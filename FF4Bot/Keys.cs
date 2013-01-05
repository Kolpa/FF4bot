using System;
using System.Collections.Generic;
using WindowsInput;

namespace FF4Bot
{
    internal class Keys
    {
        public static Dictionary<Int32, VirtualKeyCode> Vb2Vk()
        {
            return new Dictionary<int, VirtualKeyCode>
                       {
                           {30, VirtualKeyCode.VK_A},
                           {48, VirtualKeyCode.VK_B},
                           {46, VirtualKeyCode.VK_C},
                           {32, VirtualKeyCode.VK_D},
                           {18, VirtualKeyCode.VK_E},
                           {33, VirtualKeyCode.VK_F},
                           {34, VirtualKeyCode.VK_G},
                           {35, VirtualKeyCode.VK_H},
                           {23, VirtualKeyCode.VK_I},
                           {36, VirtualKeyCode.VK_J},
                           {37, VirtualKeyCode.VK_K},
                           {38, VirtualKeyCode.VK_L},
                           {50, VirtualKeyCode.VK_M},
                           {49, VirtualKeyCode.VK_N},
                           {24, VirtualKeyCode.VK_O},
                           {25, VirtualKeyCode.VK_P},
                           {16, VirtualKeyCode.VK_Q},
                           {19, VirtualKeyCode.VK_R},
                           {31, VirtualKeyCode.VK_S},
                           {20, VirtualKeyCode.VK_T},
                           {22, VirtualKeyCode.VK_U},
                           {47, VirtualKeyCode.VK_V},
                           {17, VirtualKeyCode.VK_W},
                           {45, VirtualKeyCode.VK_X},
                           {44, VirtualKeyCode.VK_Y},
                           {21, VirtualKeyCode.VK_Z},
                       };
        }
    }
}