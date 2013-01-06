﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Timers;
using System.Windows.Forms;
using Timer = System.Timers.Timer;
using WindowsInput;
using System.IO;

namespace FF4Bot
{
    internal class Program
    {
        private const string EmulatorProcessName = "vba-v24m-svn461";

        private static readonly string EmulatorFolder = SystemInformation.ComputerName == "PSYCHO" ? @"D:\Spiele\Emulatoren\Emus\GB+C+A" : SystemInformation.ComputerName == "KOLPA" ? "C:\\Users\\Kolpa\\Desktop\\vba" : Path.GetDirectoryName(Application.StartupPath);

        #region TastenCodes

        private static VirtualKeyCode _kup;
        private static VirtualKeyCode _kdown;
        private static VirtualKeyCode _kleft;
        private static VirtualKeyCode _kright;
        private static VirtualKeyCode _ka;
        private static VirtualKeyCode _kb;
        private static VirtualKeyCode _kstart;
        private static VirtualKeyCode _kspeed;

        #endregion

        #region User32.dll Importe

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out Rectangle rect);

        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll")]
        private static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, UIntPtr nSize, ref uint lpNumberOfBytesWritten);

        [DllImport("kernel32.dll")]
        private static extern bool CloseHandle(IntPtr hObject);

        #endregion

        #region Dll Wrapper

        private static IntPtr Open(int id)
        {
            return OpenProcess(0x1F0FFF, true, id);
        }

        private static bool Close(IntPtr handle)
        {
            return CloseHandle(handle);
        }

        private static int Read(IntPtr process, IntPtr adress)
        {
            byte[] bytes = new byte[24];
            uint rw = 0;
            ReadProcessMemory(process, adress, bytes, (UIntPtr) sizeof (int), ref rw);
            int result = BitConverter.ToInt32(bytes, 0);
            return result;
        }

        private static IntPtr GetAdress(IntPtr process, IntPtr pointer, uint offset)
        {
            byte[] bytes = new byte[24];
            uint rw = 0;
            ReadProcessMemory(process, pointer, bytes, (UIntPtr) sizeof (int), ref rw);
            uint pt = BitConverter.ToUInt32(bytes, 0);
            IntPtr var = (IntPtr) (pt + offset);
            return var;
        }

        #endregion

        private static readonly Timer Timer = new Timer(150);
        private static Rectangle _bounds;
        private static IntPtr _activeWindowHandle;
        private static Bitmap _bitmap;
        private static readonly Object LockObject = new object();
        private static readonly Bitmap Spritesheet = new Bitmap("spritesheet.png");

        #region Spritesheet definitions

        private enum SpritesheetSprite
        {
            WorldmapCecilSouth,
            WorldmapCecilEast,
            WorldmapCecilWest,
            WorldmapCecilNorth,
            StartMenu,
            BattleScreen,
            BattleLootScreen,
            SelectionHand,
            SelectionHandFaded
        }

        private static readonly Dictionary<SpritesheetSprite, Rectangle> SpritesheetSpriteRectangles = new Dictionary<SpritesheetSprite, Rectangle>
                                                                                                           {
                                                                                                               {SpritesheetSprite.WorldmapCecilSouth, new Rectangle(1, 1, 6, 5)},
                                                                                                               {SpritesheetSprite.WorldmapCecilEast, new Rectangle(8, 1, 6, 5)},
                                                                                                               {SpritesheetSprite.WorldmapCecilWest, new Rectangle(15, 1, 6, 5)},
                                                                                                               {SpritesheetSprite.WorldmapCecilNorth, new Rectangle(22, 1, 6, 5)},
                                                                                                               {SpritesheetSprite.StartMenu, new Rectangle(1, 7, 6, 5)},
                                                                                                               {SpritesheetSprite.BattleScreen, new Rectangle(1, 13, 6, 5)},
                                                                                                               {SpritesheetSprite.BattleLootScreen, new Rectangle(8, 13, 6, 5)},
                                                                                                               {SpritesheetSprite.SelectionHand, new Rectangle(1, 19, 6, 5)},
                                                                                                               {SpritesheetSprite.SelectionHandFaded, new Rectangle(8, 19, 6, 5)},
                                                                                                           };

        #endregion

        private static int _lastKnownHPChar3 = 900;
        private const int HealThreshold = 300;

        private static IntPtr _process;
        private static IntPtr _pointer2;

        private static void Main()
        {
            // ReSharper disable RedundantNameQualifier Menschen verstehen es, VS versteht es, Travis versteht es nicht -_-
            Dictionary<Int32, VirtualKeyCode> keys = FF4Bot.Keys.Vb2Vk();
            // ReSharper restore RedundantNameQualifier
            Dictionary<String, String> config = GetConfig();


            Process game = Process.GetProcessesByName("vba-v24m-svn461")[0];
            _process = Open(game.Id);
            IntPtr pointer1 = game.MainModule.BaseAddress + 0x4EB8F8;
            _pointer2 = GetAdress(_process, pointer1, 0x242C8);

            GetCodes(keys, config);
            Timer.AutoReset = true;
            Timer.Elapsed += TimerOnElapsed;
            Timer.Start();

            while (Timer.Enabled)
            {
            }
        }

        private static void GetCodes(IDictionary<int, VirtualKeyCode> keys, IDictionary<string, string> config)
        {
            Int32 tup = Convert.ToInt32(config["Joy1_Up"]);
            Int32 tdown = Convert.ToInt32(config["Joy1_Down"]);
            Int32 tleft = Convert.ToInt32(config["Joy1_Left"]);
            Int32 tright = Convert.ToInt32(config["Joy1_Right"]);
            Int32 ta = Convert.ToInt32(config["Joy1_A"]);
            Int32 tb = Convert.ToInt32(config["Joy1_B"]);
            Int32 tsta = Convert.ToInt32(config["Joy1_Start"]);
            Int32 tspeed = Convert.ToInt32(config["Joy1_Speed"]);

            _kup = keys[tup];
            _kdown = keys[tdown];
            _kleft = keys[tleft];
            _kright = keys[tright];
            _ka = keys[ta];
            _kb = keys[tb];
            _kstart = keys[tsta];
            _kspeed = keys[tspeed];
        }

        private static Bitmap GetSpritesheetSprite(SpritesheetSprite sprite)
        {
            return Spritesheet.Clone(SpritesheetSpriteRectangles[sprite], Spritesheet.PixelFormat);
        }

        private static void TimerOnElapsed(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            lock (LockObject)
            {
                MainLoop();
            }
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

            #region Niedrige HP und auf der Weltkarte

            if (_lastKnownHPChar3 < HealThreshold && (InWorldMapFacingEast() || InWorldMapFacingNorth() || InWorldMapFacingSouth() || InWorldMapFacingWest()))
            {
                OpenMenu();
                return;
            }

            #endregion

            #region Im Menü

            if (InMenu())
            {
                #region HP niedrig

                if (_lastKnownHPChar3 < HealThreshold)
                {
                    if (!InMenuMagicSelected())
                    {
                        DirectionDown();
                        return;
                    }

                    PressA();
                    return;
                }

                #endregion

                PressB();
            }

            #endregion

            #region Auf der Weltkarte, Blick Richtung Ost Nord oder Süd

            if (InWorldMapFacingEast() || InWorldMapFacingNorth() || InWorldMapFacingSouth())
            {
                DirectionLeft();
                return;
            }

            #endregion

            #region Auf der Weltkarte, Blick Richtung West

            if (InWorldMapFacingWest())
            {
                DirectionRight();
                return;
            }

            #endregion

            #region Im Kampf

            if (InBattle())
            {
                PressA();
                ReadChar3HP();
                return;
            }

            #endregion

            #region Nach dem Kampf im Loot-Bildschirm

            if (InBattleLootScreen())
            {
                PressA();
                return;
            }

            #endregion
        }


        private static void ReadChar3HP()
        {
            if (InBattle()) _lastKnownHPChar3 = Read(_process, _pointer2);
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

        private static bool InMenu()
        {
            return SpritesheetSpriteIsInScreenAtPosition(SpritesheetSprite.StartMenu, 160, 166);
        }

        private static bool InMenuMagicSelected()
        {
            return SpritesheetSpriteIsInScreenAtPosition(SpritesheetSprite.SelectionHand, 164, 67);
        }

        private static bool InMenuMagicSelectedChoosingCharacter()
        {
            if (SpritesheetSpriteIsInScreenAtPosition(SpritesheetSprite.SelectionHandFaded, 164, 67))
                return true;

            return false;
        }

        private static bool InWorldMapFacingEast()
        {
            return SpritesheetSpriteIsInScreenAtPosition(SpritesheetSprite.WorldmapCecilEast, 128, 124);
        }

        private static bool InWorldMapFacingWest()
        {
            return SpritesheetSpriteIsInScreenAtPosition(SpritesheetSprite.WorldmapCecilWest, 122, 124);
        }

        private static bool InWorldMapFacingNorth()
        {
            return SpritesheetSpriteIsInScreenAtPosition(SpritesheetSprite.WorldmapCecilNorth, 125, 122);
        }

        private static bool InWorldMapFacingSouth()
        {
            return SpritesheetSpriteIsInScreenAtPosition(SpritesheetSprite.WorldmapCecilSouth, 125, 125);
        }

        private static bool SpritesheetSpriteIsInScreenAtPosition(SpritesheetSprite sprite, int x, int y)
        {
            Bitmap target = GetSpritesheetSprite(sprite);
            Bitmap crop = _bitmap.Clone(new Rectangle(new Point(x, y), target.Size), target.PixelFormat);
            return BitmapsAreIdentical(target, crop);
        }

        private static bool BitmapsAreIdentical(Bitmap image1, Bitmap image2)
        {
            if (image1.Size != image2.Size)
                return false;

            for (int x = 0; x < image1.Width; x++)
            {
                for (int y = 0; y < image1.Height; y++)
                {
                    if (image1.GetPixel(x, y) != image2.GetPixel(x, y))
                        return false;
                }
            }

            return true;
        }

        private static bool InBattle()
        {
            return SpritesheetSpriteIsInScreenAtPosition(SpritesheetSprite.BattleScreen, 241, 163);
        }

        private static bool InBattleLootScreen()
        {
            return SpritesheetSpriteIsInScreenAtPosition(SpritesheetSprite.BattleLootScreen, 89, 126);
        }

        #endregion

        #region KeyboardInput

        private static void HoldTurboButton()
        {
            InputSimulator.SimulateKeyDown(_kspeed);
        }

        private static void DirectionRight()
        {
            LongPressKey(_kright);
        }

        private static void DirectionLeft()
        {
            LongPressKey(_kleft);
        }

        private static void DirectionDown()
        {
            LongPressKey(_kdown);
        }

        private static void DirectionUp()
        {
            LongPressKey(_kup);
        }

        private static void PressB()
        {
            LongPressKey(_kb);
        }

        private static void PressA()
        {
            LongPressKey(_ka);
        }

        private static void LongPressKey(VirtualKeyCode code)
        {
            Console.Out.Write("Drücke Taste: " + code + "\n");
            InputSimulator.SimulateKeyDown(code);
            Thread.Sleep(20);
            InputSimulator.SimulateKeyUp(code);
        }

        private static void OpenMenu()
        {
            LongPressKey(_kstart);
        }

        #endregion
    }
}