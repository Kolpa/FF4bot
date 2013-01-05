using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Timers;
using Timer = System.Timers.Timer;
using WindowsInput;
using System.IO;

namespace FF4Bot
{
    class Program
    {
        private string test = "konfliktprovokation!";
        
        private const string EmulatorProcessName = "vba-v24m-svn461";

        //private const string EmulatorFolder = "C:\\Users\\Kolpa\\Desktop\\vba";
        private const string EmulatorFolder = @"D:\Spiele\Emulatoren\Emus\GB+C+A";

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out Rectangle rect);

        private static readonly Timer Timer = new Timer(100);
        private static Rectangle _bounds;
        private static IntPtr _activeWindowHandle;
        private static Bitmap _bitmap;

        #region ColorCoord Maps
        private static readonly Dictionary<Point, Color> ColorCoordsWorldMapFacingSouth = new Dictionary<Point, Color>
                                                    {
                                                        {new Point(475, 355), Color.FromArgb(72, 8, 72)},
                                                        {new Point(480, 355), Color.FromArgb(72, 8, 72)},
                                                        {new Point(495, 355), Color.FromArgb(72, 8, 72)},
                                                        {new Point(500, 355), Color.FromArgb(72, 8, 72)}
                                                    };
        
        private static readonly Dictionary<Point, Color> ColorCoordsWorldMapFacingEast = new Dictionary<Point, Color>
                                                    {
                                                        {new Point(477, 355), Color.FromArgb(72, 8, 72)},
                                                        {new Point(480, 355), Color.FromArgb(72, 8, 72)},
                                                        {new Point(485, 355), Color.FromArgb(72, 8, 72)},
                                                        {new Point(500, 355), Color.FromArgb(72, 8, 72)}
                                                    };
        
        private static readonly Dictionary<Point, Color> ColorCoordsWorldMapFacingWest = new Dictionary<Point, Color>
                                                    {
                                                        {new Point(475, 355), Color.FromArgb(72, 8, 72)},
                                                        {new Point(490, 355), Color.FromArgb(72, 8, 72)},
                                                        {new Point(495, 355), Color.FromArgb(72, 8, 72)},
                                                        {new Point(499, 355), Color.FromArgb(72, 8, 72)}
                                                    };
        
        private static readonly Dictionary<Point, Color> ColorCoordsWorldMapFacingNorth = new Dictionary<Point, Color>
                                                    {
                                                        {new Point(465, 348), Color.FromArgb(72, 8, 72)},
                                                        {new Point(465, 355), Color.FromArgb(72, 8, 72)},
                                                        {new Point(477, 363), Color.FromArgb(72, 8, 72)},
                                                        {new Point(480, 366), Color.FromArgb(72, 8, 72)}
                                                    };

        private static readonly Dictionary<Point, Color> ColorCoordsBattle = new Dictionary<Point, Color>
                                                    {
                                                        {new Point( 40, 505), Color.FromArgb(200, 200, 200)},
                                                        {new Point(100, 505), Color.FromArgb(200, 200, 200)},
                                                        {new Point(200, 505), Color.FromArgb(200, 200, 200)},
                                                        {new Point(300, 505), Color.FromArgb(200, 200, 200)},
                                                        {new Point(505, 505), Color.FromArgb(200, 200, 200)},
                                                        {new Point(600, 505), Color.FromArgb(200, 200, 200)},
                                                        {new Point(700, 505), Color.FromArgb(200, 200, 200)},
                                                        {new Point(800, 505), Color.FromArgb(200, 200, 200)},
                                                        {new Point(900, 505), Color.FromArgb(200, 200, 200)},
                                                        {new Point(950, 505), Color.FromArgb(200, 200, 200)},
                                                    };
        #endregion                                                              

        private static bool _holdingActionButton;

        static void Main()
        {   
            Timer.AutoReset = true;
            Timer.Elapsed += TimerOnElapsed;
            Timer.Start();

            foreach(var i in GetConfig())
            {
                Console.WriteLine("{0}, {1}", i.Key, i.Value);
            }

            while (Timer.Enabled)
            {
            }
        }

        private static void TimerOnElapsed(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            MainLoop();
        }

        private static void MainLoop()
        {
            _activeWindowHandle = GetForegroundWindow();

            if (!EmulatorHasFocus())
            {
                Console.Out.Write("Der Emulator hat nicht den Fokus. Bitte den Emulator anklicken.\n");
                return;
            }

            TakeScreenshot();

            InterpretScreenshot();
        }

        private static void InterpretScreenshot()
        {
            HoldTurboButton();
            
            if (!InBattle() && _holdingActionButton)
            {
                ReleaseActionButton();
                _holdingActionButton = false;
            }
            
            if (InWorldMapFacingEast() || InWorldMapFacingNorth() || InWorldMapFacingSouth())
            {
                WalkWest();
            }
            else if (InWorldMapFacingWest())
            {
                WalkEast();
            }
            else if (InBattle())
            {
                LongPressKey(VirtualKeyCode.VK_C);
            }
        }

        private static Dictionary<String, String> GetConfig()
        {
            return File.ReadAllLines(EmulatorFolder + "\\vba.ini").Where(row => row.Contains("=")).ToDictionary(row => row.Split('=')[0], row => row.Split('=')[1]);
        }

        private static void TakeScreenshot()
        {
            GetWindowRect(_activeWindowHandle, out _bounds);
            _bounds.Width -= _bounds.Left;
            _bounds.Height -= _bounds.Top;
            _bitmap = new Bitmap(_bounds.Width, _bounds.Height);

            using (Graphics g = Graphics.FromImage(_bitmap))
            {
                g.CopyFromScreen(new Point(_bounds.Left, _bounds.Top), Point.Empty, _bounds.Size);
            }
        }

        private static bool EmulatorHasFocus()
        {
            return GetFocusProcess().ProcessName == EmulatorProcessName;
        }

        private static Process GetFocusProcess()
        {
            int iFocusWindowProcessID;
            GetWindowThreadProcessId(_activeWindowHandle, out iFocusWindowProcessID);
            Process focusProcess = Process.GetProcessById(iFocusWindowProcessID);
            return focusProcess;
        }

        #region CoordChecks

        private static bool InWorldMapFacingEast()
        {
            return ColorCoordsWorldMapFacingEast.All(colorPoint => _bitmap.GetPixel(colorPoint.Key.X, colorPoint.Key.Y) == colorPoint.Value);
        }

        private static bool InWorldMapFacingWest()
        {
            return ColorCoordsWorldMapFacingWest.All(colorPoint => _bitmap.GetPixel(colorPoint.Key.X, colorPoint.Key.Y) == colorPoint.Value);
        }

        private static bool InWorldMapFacingNorth()
        {
            return ColorCoordsWorldMapFacingNorth.All(colorPoint => _bitmap.GetPixel(colorPoint.Key.X, colorPoint.Key.Y) == colorPoint.Value);
        }

        private static bool InWorldMapFacingSouth()
        {
            return ColorCoordsWorldMapFacingSouth.All(colorPoint => _bitmap.GetPixel(colorPoint.Key.X, colorPoint.Key.Y) == colorPoint.Value);
        }

        private static bool InBattle()
        {
            return ColorCoordsBattle.All(colorPoint => _bitmap.GetPixel(colorPoint.Key.X, colorPoint.Key.Y) == colorPoint.Value);
        }

        #endregion

        #region KeyboardInput

        private static void ReleaseActionButton()
        {
            Console.Out.WriteLine("Lasse Action-Button los");
            InputSimulator.SimulateKeyUp(VirtualKeyCode.VK_C);
        }

        private static void HoldTurboButton()
        {
            InputSimulator.SimulateKeyDown(VirtualKeyCode.VK_N);
        }

        private static void WalkEast()
        {
            LongPressKey(VirtualKeyCode.VK_K);
        }

        private static void WalkWest()
        {
            LongPressKey(VirtualKeyCode.VK_H);
        }

        private static void OpenMainMenu()
        {
            LongPressKey(VirtualKeyCode.VK_B);
        }

        private static void Back()
        {
            LongPressKey(VirtualKeyCode.VK_X);
        }

        private static void LongPressKey(VirtualKeyCode code)
        {
            Console.Out.Write("Drücke Taste: " + code + "\n");
            InputSimulator.SimulateKeyDown(code);
            Thread.Sleep(20);
            InputSimulator.SimulateKeyUp(code);
        }

        #endregion
    }
}
