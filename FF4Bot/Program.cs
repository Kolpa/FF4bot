using System;
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

        #endregion

        private static readonly Timer Timer = new Timer(150);
        private static Rectangle _bounds;
        private static IntPtr _activeWindowHandle;
        private static Bitmap _bitmap;
        private static readonly Object LockObject = new object();

        #region ColorCoord Maps

        private static readonly Dictionary<Point, Color> ColorCoordsMenu = new Dictionary<Point, Color>
                                                                               {
                                                                                   {new Point(617, 600), Color.FromArgb(248, 248, 248)},
                                                                                   {new Point(622, 600), Color.FromArgb(200, 200, 200)},
                                                                                   {new Point(625, 600), Color.FromArgb(32, 32, 32)},
                                                                                   {new Point(630, 600), Color.FromArgb(32, 80, 136)}
                                                                               };

        private static readonly Dictionary<Point, Color> ColorCoordsMenuMagicSelected = new Dictionary<Point, Color>
                                                                                            {
                                                                                                {new Point(666, 130), Color.FromArgb(248, 248, 248)},
                                                                                                {new Point(670, 130), Color.FromArgb(240, 240, 240)},
                                                                                                {new Point(674, 130), Color.FromArgb(184, 184, 184)},
                                                                                                {new Point(678, 130), Color.FromArgb(0, 0, 0)}
                                                                                            };

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
                                                                                     {new Point(40, 505), Color.FromArgb(200, 200, 200)},
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

        private static readonly Dictionary<Point, Color> ColorCoordsBattleLootScreen = new Dictionary<Point, Color>
                                                                                           {
                                                                                               {new Point(100, 355), Color.FromArgb(32, 80, 136)},
                                                                                               {new Point(100, 360), Color.FromArgb(248, 248, 248)},
                                                                                               {new Point(100, 365), Color.FromArgb(200, 200, 200)},
                                                                                               {new Point(100, 367), Color.FromArgb(32, 32, 32)},
                                                                                               {new Point(100, 370), Color.FromArgb(248, 248, 248)},
                                                                                               {new Point(100, 375), Color.FromArgb(200, 200, 200)},
                                                                                               {new Point(100, 380), Color.FromArgb(32, 32, 32)},
                                                                                               {new Point(100, 385), Color.FromArgb(32, 80, 136)},
                                                                                           };

        private static readonly Dictionary<Point, Color> ColorCoordsBattleChar3HunderterStelle1 = new Dictionary<Point, Color>
                                                                                                      {
                                                                                                          {new Point(695, 588), Color.FromArgb(248, 248, 248)},
                                                                                                          {new Point(699, 582), Color.FromArgb(248, 248, 248)},
                                                                                                          {new Point(699, 585), Color.FromArgb(248, 248, 248)},
                                                                                                          {new Point(699, 590), Color.FromArgb(248, 248, 248)},
                                                                                                          {new Point(699, 595), Color.FromArgb(248, 248, 248)},
                                                                                                          {new Point(699, 599), Color.FromArgb(248, 248, 248)},
                                                                                                          {new Point(699, 601), Color.FromArgb(248, 248, 248)},
                                                                                                      };

        private static readonly Dictionary<Point, Color> ColorCoordsBattleChar3HunderterStelle2 = new Dictionary<Point, Color>
                                                                                                      {
                                                                                                          {new Point(688, 600), Color.FromArgb(248, 248, 248)},
                                                                                                          {new Point(690, 600), Color.FromArgb(248, 248, 248)},
                                                                                                          {new Point(693, 600), Color.FromArgb(248, 248, 248)},
                                                                                                          {new Point(696, 600), Color.FromArgb(248, 248, 248)},
                                                                                                          {new Point(700, 600), Color.FromArgb(248, 248, 248)},
                                                                                                          {new Point(703, 600), Color.FromArgb(248, 248, 248)},
                                                                                                          {new Point(695, 595), Color.FromArgb(248, 248, 248)},
                                                                                                          {new Point(697, 592), Color.FromArgb(248, 248, 248)},
                                                                                                          {new Point(700, 588), Color.FromArgb(248, 248, 248)},
                                                                                                          {new Point(692, 585), Color.FromArgb(248, 248, 248)},
                                                                                                          {new Point(695, 585), Color.FromArgb(248, 248, 248)},
                                                                                                          {new Point(699, 585), Color.FromArgb(248, 248, 248)},
                                                                                                          {new Point(690, 588), Color.FromArgb(248, 248, 248)},
                                                                                                      };

        private static readonly Dictionary<Point, Color> ColorCoordsBattleChar3HunderterStelle3 = new Dictionary<Point, Color>
                                                                                                      {
                                                                                                          {new Point(688, 605), Color.FromArgb(248, 248, 248)},
                                                                                                          {new Point(695, 605), Color.FromArgb(248, 248, 248)},
                                                                                                          {new Point(699, 600), Color.FromArgb(248, 248, 248)},
                                                                                                          {new Point(700, 595), Color.FromArgb(248, 248, 248)},
                                                                                                          {new Point(699, 590), Color.FromArgb(248, 248, 248)},
                                                                                                          {new Point(690, 588), Color.FromArgb(248, 248, 248)},
                                                                                                          {new Point(700, 588), Color.FromArgb(248, 248, 248)},
                                                                                                          {new Point(692, 585), Color.FromArgb(248, 248, 248)},
                                                                                                          {new Point(695, 585), Color.FromArgb(248, 248, 248)},
                                                                                                          {new Point(699, 585), Color.FromArgb(248, 248, 248)},
                                                                                                      };

        private static readonly Dictionary<Point, Color> ColorCoordsBattleChar3HunderterStelle4 = new Dictionary<Point, Color>
                                                                                                      {
                                                                                                          {new Point(699, 582), Color.FromArgb(248, 248, 248)},
                                                                                                          {new Point(699, 585), Color.FromArgb(248, 248, 248)},
                                                                                                          {new Point(699, 590), Color.FromArgb(248, 248, 248)},
                                                                                                          {new Point(699, 595), Color.FromArgb(248, 248, 248)},
                                                                                                          {new Point(699, 600), Color.FromArgb(248, 248, 248)},
                                                                                                          {new Point(699, 605), Color.FromArgb(248, 248, 248)},
                                                                                                          {new Point(688, 595), Color.FromArgb(248, 248, 248)},
                                                                                                          {new Point(693, 595), Color.FromArgb(248, 248, 248)},
                                                                                                          {new Point(703, 595), Color.FromArgb(248, 248, 248)},
                                                                                                          {new Point(690, 590), Color.FromArgb(248, 248, 248)},
                                                                                                          {new Point(695, 588), Color.FromArgb(248, 248, 248)},
                                                                                                      };

        private static readonly Dictionary<Point, Color> ColorCoordsBattleChar3HunderterStelle5 = new Dictionary<Point, Color>
                                                                                                      {
                                                                                                          {new Point(690, 585), Color.FromArgb(248, 248, 248)},
                                                                                                          {new Point(695, 585), Color.FromArgb(248, 248, 248)},
                                                                                                          {new Point(700, 585), Color.FromArgb(248, 248, 248)},
                                                                                                          {new Point(690, 590), Color.FromArgb(248, 248, 248)},
                                                                                                          {new Point(695, 590), Color.FromArgb(248, 248, 248)},
                                                                                                          {new Point(699, 590), Color.FromArgb(248, 248, 248)},
                                                                                                          {new Point(700, 595), Color.FromArgb(248, 248, 248)},
                                                                                                          {new Point(697, 600), Color.FromArgb(248, 248, 248)},
                                                                                                          {new Point(690, 605), Color.FromArgb(248, 248, 248)},
                                                                                                          {new Point(695, 605), Color.FromArgb(248, 248, 248)},
                                                                                                      };

        private static readonly Dictionary<Point, Color> ColorCoordsBattleChar3HunderterStelle6 = new Dictionary<Point, Color>
                                                                                                      {
                                                                                                          {new Point(690, 590), Color.FromArgb(248, 248, 248)},
                                                                                                          {new Point(695, 600), Color.FromArgb(248, 248, 248)},
                                                                                                          {new Point(700, 590), Color.FromArgb(248, 248, 248)},
                                                                                                          {new Point(695, 585), Color.FromArgb(248, 248, 248)},
                                                                                                          {new Point(700, 580), Color.FromArgb(248, 248, 248)},
                                                                                                      };

        private static readonly Dictionary<Point, Color> ColorCoordsBattleChar3HunderterStelle7 = new Dictionary<Point, Color>
                                                                                                      {
                                                                                                          {new Point(688, 585), Color.FromArgb(248, 248, 248)},
                                                                                                          {new Point(690, 585), Color.FromArgb(248, 248, 248)},
                                                                                                          {new Point(693, 585), Color.FromArgb(248, 248, 248)},
                                                                                                          {new Point(696, 585), Color.FromArgb(248, 248, 248)},
                                                                                                          {new Point(700, 585), Color.FromArgb(248, 248, 248)},
                                                                                                          {new Point(703, 585), Color.FromArgb(248, 248, 248)},
                                                                                                          {new Point(699, 590), Color.FromArgb(248, 248, 248)},
                                                                                                          {new Point(699, 594), Color.FromArgb(248, 248, 248)},
                                                                                                          {new Point(699, 597), Color.FromArgb(248, 248, 248)},
                                                                                                          {new Point(695, 598), Color.FromArgb(248, 248, 248)},
                                                                                                          {new Point(695, 602), Color.FromArgb(248, 248, 248)},
                                                                                                          {new Point(695, 605), Color.FromArgb(248, 248, 248)},
                                                                                                      };

        private static readonly Dictionary<Point, Color> ColorCoordsBattleChar3HunderterStelle8 = new Dictionary<Point, Color>
                                                                                                      {
                                                                                                          {new Point(666, 666), Color.FromArgb(1, 2, 3)},
                                                                                                      };

        private static readonly Dictionary<Point, Color> ColorCoordsBattleChar3HunderterStelle9 = new Dictionary<Point, Color>
                                                                                                      {
                                                                                                          {new Point(666, 666), Color.FromArgb(1, 2, 3)},
                                                                                                      };

        #endregion

        private static int _lastKnownHPChar3 = 900;
        private const int HealThreshold = 300;

        private static void Main()
        {
            Dictionary<Int32, VirtualKeyCode> keys = Keys.Vb2Vk();
            Dictionary<String, String> config = GetConfig();

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
            Console.Out.WriteLine("HP: {0}", _lastKnownHPChar3);

            HoldTurboButton();

            if (_lastKnownHPChar3 < HealThreshold && (InWorldMapFacingEast() || InWorldMapFacingNorth() || InWorldMapFacingSouth() || InWorldMapFacingWest()))
            {
                OpenMenu();
                return;
            }

            if (InMenu())
            {
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

                PressB();
            }

            if (InWorldMapFacingEast() || InWorldMapFacingNorth() || InWorldMapFacingSouth())
            {
                DirectionLeft();
                return;
            }

            if (InWorldMapFacingWest())
            {
                DirectionRight();
                return;
            }

            if (InBattle())
            {
                PressA();
                ReadChar3HP();
                return;
            }

            if (InBattleLootScreen())
            {
                PressA();
                return;
            }
        }


        private static void ReadChar3HP()
        {
            if (Char3HunderterStelle9())
                _lastKnownHPChar3 = 900;
            else if (Char3HunderterStelle8())
                _lastKnownHPChar3 = 800;
            else if (Char3HunderterStelle7())
                _lastKnownHPChar3 = 700;
            else if (Char3HunderterStelle6())
                _lastKnownHPChar3 = 600;
            else if (Char3HunderterStelle5())
                _lastKnownHPChar3 = 500;
            else if (Char3HunderterStelle4())
                _lastKnownHPChar3 = 400;
            else if (Char3HunderterStelle3())
                _lastKnownHPChar3 = 300;
            else if (Char3HunderterStelle2())
                _lastKnownHPChar3 = 200;
            else if (Char3HunderterStelle1())
                _lastKnownHPChar3 = 100;
            else
                _lastKnownHPChar3 = 1;
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
            return ColorCoordsMenu.All(colorPoint => _bitmap.GetPixel(colorPoint.Key.X, colorPoint.Key.Y) == colorPoint.Value);
        }

        private static bool InMenuMagicSelected()
        {
            return ColorCoordsMenuMagicSelected.All(colorPoint => _bitmap.GetPixel(colorPoint.Key.X, colorPoint.Key.Y) == colorPoint.Value);
        }

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

        private static bool InBattleLootScreen()
        {
            return ColorCoordsBattleLootScreen.All(colorPoint => _bitmap.GetPixel(colorPoint.Key.X, colorPoint.Key.Y) == colorPoint.Value);
        }

        private static bool Char3HunderterStelle1()
        {
            return ColorCoordsBattleChar3HunderterStelle1.All(colorPoint => _bitmap.GetPixel(colorPoint.Key.X, colorPoint.Key.Y) == colorPoint.Value);
        }

        private static bool Char3HunderterStelle2()
        {
            return ColorCoordsBattleChar3HunderterStelle2.All(colorPoint => _bitmap.GetPixel(colorPoint.Key.X, colorPoint.Key.Y) == colorPoint.Value);
        }

        private static bool Char3HunderterStelle3()
        {
            return ColorCoordsBattleChar3HunderterStelle3.All(colorPoint => _bitmap.GetPixel(colorPoint.Key.X, colorPoint.Key.Y) == colorPoint.Value);
        }

        private static bool Char3HunderterStelle4()
        {
            return ColorCoordsBattleChar3HunderterStelle4.All(colorPoint => _bitmap.GetPixel(colorPoint.Key.X, colorPoint.Key.Y) == colorPoint.Value);
        }

        private static bool Char3HunderterStelle5()
        {
            return ColorCoordsBattleChar3HunderterStelle5.All(colorPoint => _bitmap.GetPixel(colorPoint.Key.X, colorPoint.Key.Y) == colorPoint.Value);
        }

        private static bool Char3HunderterStelle6()
        {
            return ColorCoordsBattleChar3HunderterStelle6.All(colorPoint => _bitmap.GetPixel(colorPoint.Key.X, colorPoint.Key.Y) == colorPoint.Value);
        }

        private static bool Char3HunderterStelle7()
        {
            return ColorCoordsBattleChar3HunderterStelle7.All(colorPoint => _bitmap.GetPixel(colorPoint.Key.X, colorPoint.Key.Y) == colorPoint.Value);
        }

        private static bool Char3HunderterStelle8()
        {
            return ColorCoordsBattleChar3HunderterStelle8.All(colorPoint => _bitmap.GetPixel(colorPoint.Key.X, colorPoint.Key.Y) == colorPoint.Value);
        }

        private static bool Char3HunderterStelle9()
        {
            return ColorCoordsBattleChar3HunderterStelle9.All(colorPoint => _bitmap.GetPixel(colorPoint.Key.X, colorPoint.Key.Y) == colorPoint.Value);
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