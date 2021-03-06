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
        private static VirtualKeyCode _kl;
        private static VirtualKeyCode _kr;
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

        private static void Close(IntPtr handle)
        {
            CloseHandle(handle);
        }

        private static int Read(IntPtr process, IntPtr adress, int iBytesToRead = 2)
        {
            byte[] bytes = new byte[24];
            uint rw = 0;
            ReadProcessMemory(process, adress, bytes, (UIntPtr) iBytesToRead, ref rw);
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

        #region Private Properties

        private static readonly Timer Timer = new Timer(100);
        private static Rectangle _bounds;
        private static IntPtr _activeWindowHandle;
        private static Bitmap _bitmap;
        private static readonly Object LockObject = new object();
        private static readonly Bitmap Spritesheet = new Bitmap("spritesheet.png");

        #endregion

        #region Enums

        private enum Direction
        {
            Up,
            Right,
            Down,
            Left
        }

        private enum StartMenuOption
        {
            Item,
            Magic
        }

        #endregion

        #region Spritesheet definitions

        private enum SpritesheetSprite
        {
            BattleLootScreen,
            SelectionHand,
            SelectionHandFaded,
            StartMenuMagicMenu,
            StartMenuChoosingMagicTarget
        }

        private static readonly Dictionary<SpritesheetSprite, Rectangle> SpritesheetSpriteRectangles = new Dictionary<SpritesheetSprite, Rectangle>
                                                                                                           {
                                                                                                               {SpritesheetSprite.BattleLootScreen, new Rectangle(8, 13, 6, 5)},
                                                                                                               {SpritesheetSprite.SelectionHand, new Rectangle(1, 19, 6, 5)},
                                                                                                               {SpritesheetSprite.SelectionHandFaded, new Rectangle(8, 19, 6, 5)},
                                                                                                               {SpritesheetSprite.StartMenuMagicMenu, new Rectangle(8, 7, 6, 5)},
                                                                                                               {SpritesheetSprite.StartMenuChoosingMagicTarget, new Rectangle(15, 7, 6, 5)},
                                                                                                           };

        #endregion

        #region Pointers

        private static IntPtr _process;
        private static IntPtr _ptrGameMainModuleBaseAddress;
        private static IntPtr _ptrChar3MaxHP;
        private static IntPtr _ptrChar3FieldHP;
        private static IntPtr _ptrChar3FieldMP;
        private static IntPtr _ptrChar3BattleHP;
        private static IntPtr _ptrChar3BattleMP;
        private static IntPtr _ptrFieldDisplayedCharIndex;
        private static IntPtr _ptrBattleFlag;
        private static IntPtr _ptrWorldMapStartMenuFlag;
        private static IntPtr _ptrStartMenuItemMenuFlag;
        private static IntPtr _ptrStartMenuOptionCursorPosition;
        private static IntPtr _ptrStartMenuMagicTargetCursorPosition;
        private static IntPtr _ptrItemSlot0ItemType;
        private static IntPtr _ptrStartMenuItemMenuCursorRow;
        private static IntPtr _ptrStartMenuItemMenuCursorCol;
        private static IntPtr _ptrLookDirection;

        #endregion

        #region Fixed Codes and Offsets

        private const int ItemCodeTent = 0xE2;

        #endregion

        private static void Main()
        {
            // ReSharper disable RedundantNameQualifier Menschen verstehen es, VS versteht es, Travis versteht es nicht -_-
            Dictionary<Int32, VirtualKeyCode> keys = FF4Bot.Keys.Vb2Vk();
            // ReSharper restore RedundantNameQualifier
            Dictionary<String, String> config = GetConfig();

            Process game = Process.GetProcessesByName(EmulatorProcessName)[0];
            _process = Open(game.Id);
            _ptrGameMainModuleBaseAddress = game.MainModule.BaseAddress;

            _ptrChar3MaxHP = GetAdress(_process, _ptrGameMainModuleBaseAddress + 0x41E380, 0x607A);
            _ptrChar3FieldHP = GetAdress(_process, _ptrGameMainModuleBaseAddress + 0x41E380, 0x6078);
            _ptrChar3FieldMP = GetAdress(_process, _ptrGameMainModuleBaseAddress + 0x41E380, 0x607C);
            _ptrChar3BattleHP = GetAdress(_process, _ptrGameMainModuleBaseAddress + 0x4EB8F8, 0x242C8);
            _ptrChar3BattleMP = GetAdress(_process, _ptrGameMainModuleBaseAddress + 0x4EB8F8, 0x242CC);
            _ptrFieldDisplayedCharIndex = GetAdress(_process, _ptrGameMainModuleBaseAddress + 0x41E380, 0x6440);
            _ptrBattleFlag = GetAdress(_process, _ptrGameMainModuleBaseAddress + 0x41E380, 0x2E);
            _ptrWorldMapStartMenuFlag = GetAdress(_process, _ptrGameMainModuleBaseAddress + 0x41E380, 0xFF52);
            _ptrStartMenuItemMenuFlag = GetAdress(_process, _ptrGameMainModuleBaseAddress + 0x41E3A0, 0x59FE);
            _ptrStartMenuOptionCursorPosition = GetAdress(_process, _ptrGameMainModuleBaseAddress + 0x41E380, 0x22C6A);
            _ptrStartMenuMagicTargetCursorPosition = GetAdress(_process, _ptrGameMainModuleBaseAddress + 0x41E380, 0x300A0);
            _ptrItemSlot0ItemType = GetAdress(_process, _ptrGameMainModuleBaseAddress + 0x41E380, 0x6564);
            _ptrStartMenuItemMenuCursorRow = GetAdress(_process, _ptrGameMainModuleBaseAddress + 0x41E380, 0x3009E);
            _ptrStartMenuItemMenuCursorCol = GetAdress(_process, _ptrGameMainModuleBaseAddress + 0x41E380, 0x3009C);
            _ptrLookDirection = GetAdress(_process, _ptrGameMainModuleBaseAddress + 0x41E380, 0xDA90);

            GetCodes(keys, config);
            Timer.AutoReset = true;
            Timer.Elapsed += TimerOnElapsed;
            Timer.Start();

            while (Timer.Enabled)
            {
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
            Console.Out.WriteLine("Starte Interpretation der Situation.");

            HoldTurboButton();

            #region Im Menü

            if (InMenu() || StartMenuItemMenuFlagSet())
            {
                Console.Out.WriteLine("Im Menü.");

                #region MP niedrig

                if (StartMenuItemMenuFlagSet())
                {
                    Console.Out.WriteLine("Bin im Item-Menü.");

                    bool bTentsFound = false;
                    int iTentItemSlot = 0;

                    for (int i = 0; i < 48; i++)
                    {
                        if (ReadItemCodeInItemSlot(i) != ItemCodeTent) continue;
                        bTentsFound = true;
                        iTentItemSlot = i;
                        break;
                    }

                    if (!bTentsFound)
                    {
                        Console.Out.WriteLine("Keine Zelte gefunden. Ich speichere und geh dann sterben.");
                        QuicksaveGame();
                        Close(_process);
                        Timer.Stop();
                        return;
                    }
                    Console.Out.WriteLine("Ich hab noch Zelte!.");

                    if (ReadItemCursorPosition() == iTentItemSlot)
                    {
                        Console.Out.WriteLine("Cursor ist auf Zelten! Benutze eins.");
                        PressA();
                        return;
                    }

                    if ((ReadItemMenuCursorCol() == 0 && iTentItemSlot%2 != 0) || (ReadItemMenuCursorCol() == 1 && iTentItemSlot%2 == 0))
                    {
                        if (ReadItemMenuCursorRow() == 23) DirectionLeft();
                        else DirectionRight();
                        Console.Out.WriteLine("Cursor ist in der falschen Spalte! Wechsle Spalte.");
                        return;
                    }

                    if (ReadItemMenuCursorRow() < iTentItemSlot/2)
                    {
                        Console.Out.WriteLine("Cursor ist in der falschen Zeile! Gehe nach unten.");
                        DirectionDown();
                        return;
                    }

                    if (ReadItemMenuCursorRow() > iTentItemSlot/2)
                    {
                        Console.Out.WriteLine("Cursor ist in der falschen Zeile! Gehe nach oben.");
                        DirectionUp();
                        return;
                    }
                }

                if (BelowMinimumMP(2))
                {
                    Console.Out.WriteLine("Bin OOM.");
                    if (GetSelectedStartMenuOption() != StartMenuOption.Item)
                    {
                        Console.Out.WriteLine("Bewege Cursor auf 'Item' im Menü .");
                        DirectionDown();
                        return;
                    }

                    Console.Out.WriteLine("Wähle 'Item' im Menü aus.");
                    PressA();
                    return;
                }

                #endregion

                #region HP niedrig

                if (!AboveSafeHP(2))
                {
                    Console.Out.WriteLine("Sollte heilen.");

                    #region Cursor nicht auf Menüpunkt "Magie"

                    if (GetSelectedStartMenuOption() != StartMenuOption.Magic && !InMenuMagicSelectedChoosingCharacter() && !InMenuChoosingMagicTarget())
                    {
                        Console.Out.WriteLine("Bewege Cursor auf 'Magie'");
                        DirectionDown();
                        return;
                    }

                    #endregion

                    #region Cursor auf Menüpunkt "Magie", aber noch nicht angeklickt

                    if (GetSelectedStartMenuOption() == StartMenuOption.Magic && !InMenuMagicSelectedChoosingCharacter() && !InMenuChoosingMagicTarget())
                    {
                        Console.Out.WriteLine("Cursor ist auf 'Magie', drücke A.");
                        PressA();
                        return;
                    }

                    #endregion

                    #region Menüpunkt "Magie" ausgewählt, noch kein Caster ausgewählt

                    if (!InMenuMagicSelectedChar3Selected() && !InMenuChoosingMagicTarget())
                    {
                        Console.Out.WriteLine("Wähle Char3 als Caster aus.");
                        DirectionDown();
                        return;
                    }

                    #endregion

                    #region Char3 als Caster markiert

                    if (InMenuMagicSelectedChar3Selected() && !InMenuChoosingMagicTarget())
                    {
                        Console.Out.WriteLine("Bestätige Char3 als Caster.");
                        PressA();
                        return;
                    }

                    #endregion

                    #region Wähle Zauber-Ziel

                    if (InMenuChoosingMagicTarget())
                    {
                        Console.Out.WriteLine("Wähle Zauber-Ziel aus.");
                        if (!InMenuMagicSelectedChar3Selected())
                        {
                            Console.Out.WriteLine("Wähle Char3 als Zauberziel aus.");
                            DirectionDown();
                            return;
                        }

                        if (!AboveSafeHP(2) && !BelowMinimumMP(2))
                        {
                            Console.Out.WriteLine("Heile Char3.");
                            PressA();
                            ReadChar3HP();
                            return;
                        }
                    }

                    #endregion
                }

                #endregion

                Console.Out.WriteLine("Verlasse das Menü.");
                PressB();
                return;
            }

            #endregion

            #region Im Magie-Menü

            if (InMenuMagicMenu())
            {
                Console.Out.WriteLine("Bin im Magie-Menü.");
                if (AboveSafeHP(2) || BelowMinimumMP(2))
                {
                    Console.Out.WriteLine("Verlasse das Magie-Menü.");
                    PressB();
                }
                else
                {
                    Console.Out.WriteLine("Drücke im Magie-Menü A.");
                    PressA();
                }

                return;
            }

            #endregion

            #region Im Kampf

            if (BattleFlagSet())
            {
                if (BelowMininumHP(2))
                {
                    Console.Out.WriteLine("Fliehe aus dem Kampf: {0} HP", ReadChar3HP());
                    HoldLR();
                    return;
                }

                PressA();
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

            #region Niedrige HP und auf der Weltkarte

            if (BelowMininumHP(2) && !InMenu())
            {
                Console.Out.WriteLine("Bin auf der Weltkarte und hab wenig Leben. Öffne Menü.");
                OpenMenu();
                return;
            }

            #endregion

            #region Ab hier nach Ausschlussprinzip Weltkarte

            StopHoldingLR();

            #endregion

            #region Weltkarte, aber anderer char als Cecil sichtbar

            if (ReadFieldDisplayedCharIndex() != 2)
            {
                PressR();
                return;
            }

            #endregion

            #region Hin und her laufen

            if (GetLookDirection() == Direction.Right) DirectionLeft();
            else DirectionRight();

            #endregion
        }

        #region Unsorted Methods

        private static void QuicksaveGame()
        {
            InputSimulator.SimulateKeyDown(VirtualKeyCode.LSHIFT);
            Thread.Sleep(100);
            LongPressKey(VirtualKeyCode.F10);
            InputSimulator.SimulateKeyUp(VirtualKeyCode.LSHIFT);
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

        private static void GetCodes(IDictionary<int, VirtualKeyCode> keys, IDictionary<string, string> config)
        {
            Int32 tup = Convert.ToInt32(config["Joy1_Up"]);
            Int32 tdown = Convert.ToInt32(config["Joy1_Down"]);
            Int32 tleft = Convert.ToInt32(config["Joy1_Left"]);
            Int32 tright = Convert.ToInt32(config["Joy1_Right"]);
            Int32 ta = Convert.ToInt32(config["Joy1_A"]);
            Int32 tb = Convert.ToInt32(config["Joy1_B"]);
            Int32 tl = Convert.ToInt32(config["Joy1_L"]);
            Int32 tr = Convert.ToInt32(config["Joy1_R"]);
            Int32 tsta = Convert.ToInt32(config["Joy1_Start"]);
            Int32 tspeed = Convert.ToInt32(config["Joy1_Speed"]);

            _kup = keys[tup];
            _kdown = keys[tdown];
            _kleft = keys[tleft];
            _kright = keys[tright];
            _ka = keys[ta];
            _kb = keys[tb];
            _kl = keys[tl];
            _kr = keys[tr];
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
                Console.Out.WriteLine("===========================================");
                MainLoop();
            }
        }

        #endregion

        #region Reading RAM

        private static int ReadChar3MaxHP()
        {
            return Read(_process, _ptrChar3MaxHP);
        }

        private static int ReadChar3HP()
        {
            return Read(_process, BattleFlagSet() ? _ptrChar3BattleHP : _ptrChar3FieldHP);
        }

        private static int ReadChar3MP()
        {
            return Read(_process, BattleFlagSet() ? _ptrChar3BattleMP : _ptrChar3FieldMP);
        }

        private static int ReadFieldDisplayedCharIndex()
        {
            return Read(_process, _ptrFieldDisplayedCharIndex, 1);
        }

        private static int ReadBattleFlag()
        {
            return Read(_process, _ptrBattleFlag, 1);
        }

        private static int ReadWorldMapStartMenuFlag()
        {
            return Read(_process, _ptrWorldMapStartMenuFlag, 1);
        }

        private static int ReadStartMenuItemMenuFlag()
        {
            return Read(_process, _ptrStartMenuItemMenuFlag, 1);
        }

        private static int ReadStartMenuOptionCursorPosition()
        {
            return Read(_process, _ptrStartMenuOptionCursorPosition, 1);
        }

        private static int ReadStartMenuMagicTargetCursorPosition()
        {
            return Read(_process, _ptrStartMenuMagicTargetCursorPosition, 1);
        }

        private static int ReadItemCodeInItemSlot(int iSlot)
        {
            return Read(_process, _ptrItemSlot0ItemType + (iSlot*0x4), 1);
        }

        private static int ReadItemMenuCursorRow()
        {
            return Read(_process, _ptrStartMenuItemMenuCursorRow, 1);
        }

        private static int ReadItemMenuCursorCol()
        {
            return Read(_process, _ptrStartMenuItemMenuCursorCol, 1);
        }

        private static int ReadItemCursorPosition()
        {
            return (2*ReadItemMenuCursorRow()) + ReadItemMenuCursorCol();
        }

        private static int ReadLookDirection()
        {
            return Read(_process, _ptrLookDirection, 1);
        }

        #endregion

        #region Infos from Reading RAM

        private static bool BelowMininumHP(int iChar)
        {
            switch (iChar)
            {
                case 2:
                    return ReadChar3HP() < (ReadChar3MaxHP()*0.2);
            }

            return false;
        }

        private static bool BelowMinimumMP(int iChar)
        {
            switch (iChar)
            {
                case 2:
                    return ReadChar3MP() < 3;
            }

            return false;
        }

        private static bool AboveSafeHP(int iChar)
        {
            switch (iChar)
            {
                case 2:
                    return ReadChar3HP() > (ReadChar3MaxHP()*0.9);
            }

            return false;
        }

        private static bool BattleFlagSet()
        {
            int iBattleFlag = ReadBattleFlag();
            return iBattleFlag == 255;
        }

        private static bool WorldMapStartMenuFlagSet()
        {
            return ReadWorldMapStartMenuFlag() != 0;
        }

        private static bool StartMenuFlagSet()
        {
            return WorldMapStartMenuFlagSet();
        }

        private static bool InMenu()
        {
            return StartMenuFlagSet();
        }

        private static bool StartMenuItemMenuFlagSet()
        {
            return ReadStartMenuItemMenuFlag() == 255;
        }

        private static bool InMenuMagicSelectedChar3Selected()
        {
            return ReadStartMenuMagicTargetCursorPosition() == 2;
        }

        private static StartMenuOption? GetSelectedStartMenuOption()
        {
            switch (ReadStartMenuOptionCursorPosition())
            {
                case 0:
                    return StartMenuOption.Item;
                case 1:
                    return StartMenuOption.Magic;
                default:
                    return null;
            }
        }

        private static Direction GetLookDirection()
        {
            int iDirectionCode = ReadLookDirection();
            switch (iDirectionCode)
            {
                default:
                    return Direction.Up;
                case 2:
                    return Direction.Right;
                case 4:
                    return Direction.Down;
                case 6:
                    return Direction.Left;
            }
        }

        #endregion

        #region CoordChecks

        private static bool InMenuMagicSelectedChoosingCharacter()
        {
            return SpritesheetSpriteIsInScreenAtPosition(SpritesheetSprite.SelectionHandFaded, 164, 67);
        }

        private static bool InMenuMagicMenu()
        {
            return SpritesheetSpriteIsInScreenAtPosition(SpritesheetSprite.StartMenuMagicMenu, 85, 101);
        }

        private static bool InMenuChoosingMagicTarget()
        {
            return SpritesheetSpriteIsInScreenAtPosition(SpritesheetSprite.StartMenuChoosingMagicTarget, 170, 178);
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

        private static void HoldLR()
        {
            InputSimulator.SimulateKeyDown(_kl);
            InputSimulator.SimulateKeyDown(_kr);
        }

        private static void StopHoldingLR()
        {
            InputSimulator.SimulateKeyUp(_kl);
            InputSimulator.SimulateKeyUp(_kr);
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

        private static void PressR()
        {
            LongPressKey(_kr);
        }

        private static void LongPressKey(VirtualKeyCode code)
        {
            //Console.Out.Write("Drücke Taste: " + code + "\n");
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