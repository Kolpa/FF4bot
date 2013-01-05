using System;
using System.Collections.Generic;
using WindowsInput;

namespace FF4Bot
{
    class Keys
    {
       public static Dictionary<Int32, VirtualKeyCode> Vb2Vk()
       {
           Dictionary<Int32, VirtualKeyCode> tmp = new Dictionary<int, VirtualKeyCode>
                                                       {
                                                           {16, VirtualKeyCode.VK_Q},
                                                           {17, VirtualKeyCode.VK_W},
                                                           {18, VirtualKeyCode.VK_E},
                                                           {19, VirtualKeyCode.VK_R},
                                                           {20, VirtualKeyCode.VK_T},
                                                           {21, VirtualKeyCode.VK_Z},
                                                           {22, VirtualKeyCode.VK_U},
                                                           {23, VirtualKeyCode.VK_I},
                                                           {24, VirtualKeyCode.VK_O},
                                                           {25, VirtualKeyCode.VK_P},
                                                           {30, VirtualKeyCode.VK_A},
                                                           {31, VirtualKeyCode.VK_S},
                                                           {32, VirtualKeyCode.VK_D},
                                                           {33, VirtualKeyCode.VK_F},
                                                           {34, VirtualKeyCode.VK_G},
                                                           {35, VirtualKeyCode.VK_H},
                                                           {36, VirtualKeyCode.VK_J},
                                                           {37, VirtualKeyCode.VK_K},
                                                           {38, VirtualKeyCode.VK_L},
                                                           {44, VirtualKeyCode.VK_Y},
                                                           {45, VirtualKeyCode.VK_X},
                                                           {46, VirtualKeyCode.VK_C},
                                                           {47, VirtualKeyCode.VK_V},
                                                           {48, VirtualKeyCode.VK_B},
                                                           {49, VirtualKeyCode.VK_N},
                                                           {50, VirtualKeyCode.VK_M}
                                                       };

           return tmp;
       }
    }
}
