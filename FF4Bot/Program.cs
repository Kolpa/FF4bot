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

        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll")]
        static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, UIntPtr nSize, ref uint lpNumberOfBytesWritten);

        [DllImport("kernel32.dll")]
        static extern bool CloseHandle(IntPtr hObject);

        #endregion

        #region Dll Wrapper

        private static IntPtr Open(int id)
        {
            return OpenProcess(0x1F0FFF, true, id);
        }

        private static bool Close(IntPtr Handle)
        {
            return CloseHandle(Handle);
        }

        private static int Read(IntPtr Process, IntPtr Adress)
        {
            byte[] bytes = new byte[24];
            uint rw = 0;
            ReadProcessMemory(Process, Adress, bytes, (UIntPtr)sizeof(int), ref rw);
            int result = BitConverter.ToInt32(bytes, 0);
            return result;
        }

        private static IntPtr getAdress(IntPtr Process, IntPtr pointer, uint offset)
        {
            byte[] bytes = new byte[24];
            uint rw = 0;
            ReadProcessMemory(Process, pointer, bytes, (UIntPtr)sizeof(int), ref rw);
            uint pt = BitConverter.ToUInt32(bytes, 0);
            IntPtr var = (IntPtr)(pt + offset);
            return var;
        }

        #endregion

        private static readonly Timer Timer = new Timer(150);
        private static Rectangle _bounds;
        private static IntPtr _activeWindowHandle;
        private static Bitmap _bitmap;
        private static readonly Object LockObject = new object();
        private static readonly Bitmap Spritesheet = new Bitmap("spritesheet.png");

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

        #region Spritesheet definitions

        private enum SpritesheetSprite
        {
            WorldmapCecilSouth,
            WorldmapCecilEast,
            WorldmapCecilWest,
            WorldmapCecilNorth,
        }

        private static readonly Dictionary<SpritesheetSprite, Rectangle> SpritesheetSpriteRectangles = new Dictionary<SpritesheetSprite, Rectangle>
                                                                                                           {
                                                                                                               {SpritesheetSprite.WorldmapCecilSouth, new Rectangle(1, 1, 6, 5)},
                                                                                                               {SpritesheetSprite.WorldmapCecilEast, new Rectangle(8, 1, 6, 5)},
                                                                                                               {SpritesheetSprite.WorldmapCecilWest, new Rectangle(15, 1, 6, 5)},
                                                                                                               {SpritesheetSprite.WorldmapCecilNorth, new Rectangle(22, 1, 6, 5)},
                                                                                                           };

        #endregion

        private static int _lastKnownHPChar3 = 900;
        private const int HealThreshold = 300;

        private static IntPtr process;
        private static IntPtr pointer2;

        private static void Main()
        {
            // ReSharper disable RedundantNameQualifier Menschen verstehen es, VS versteht es, Travis versteht es nicht -_-
            Dictionary<Int32, VirtualKeyCode> keys = FF4Bot.Keys.Vb2Vk();
            // ReSharper restore RedundantNameQualifier
            Dictionary<String, String> config = GetConfig();

            
            Process game = Process.GetProcessesByName("vba-v24m-svn461")[0];
            process = Open(game.Id);
            IntPtr pointer1 = game.MainModule.BaseAddress + 0x4EB8F8;
            pointer2 = getAdress(process, pointer1, 0x242C8);

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
            Console.Out.WriteLine("HP: {0}", _lastKnownHPChar3);

            HoldTurboButton();

            if (_lastKnownHPChar3 < HealThreshold && (InWorldMapFacingEast() || InWorldMapFacingNorth() || InWorldMapFacingSouth() || InWorldMapFacingWest()))
            {
                OpenMenu();
                return;
            }

            if (InWorldMapFacingNorth()) Console.Out.WriteLine("NORTH");
            if (InWorldMapFacingWest()) Console.Out.WriteLine("WEST");
            if (InWorldMapFacingEast()) Console.Out.WriteLine("EAST");
            if (InWorldMapFacingSouth()) Console.Out.WriteLine("SOUTH");

            return;

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
            int Hp = Read(process, pointer2);
            if ((int)Math.Floor(Math.Log10(Hp)) + 1 > 2)
            {
                _lastKnownHPChar3 = Convert.ToInt32(Read(process, pointer2).ToString().Substring(0, 1));
            }
            else
            {
                _lastKnownHPChar3 = 0;
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