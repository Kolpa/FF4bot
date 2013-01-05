using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WindowsInput;

namespace FF4Bot
{
    class Keys
    {
       public static Dictionary<Int32, VirtualKeyCode> vb2vk()
       {
           Dictionary<Int32, VirtualKeyCode> tmp = new Dictionary<int, VirtualKeyCode>();
           tmp.Add(16, VirtualKeyCode.VK_Q);
           tmp.Add(17, VirtualKeyCode.VK_W);
           tmp.Add(18, VirtualKeyCode.VK_E);
           tmp.Add(19, VirtualKeyCode.VK_R);
           tmp.Add(20, VirtualKeyCode.VK_T);
           tmp.Add(21, VirtualKeyCode.VK_Z);
           tmp.Add(22, VirtualKeyCode.VK_U);
           tmp.Add(23, VirtualKeyCode.VK_I);
           tmp.Add(24, VirtualKeyCode.VK_O);
           tmp.Add(25, VirtualKeyCode.VK_P);

           tmp.Add(30, VirtualKeyCode.VK_A);
           tmp.Add(31, VirtualKeyCode.VK_S);
           tmp.Add(32, VirtualKeyCode.VK_D);
           tmp.Add(33, VirtualKeyCode.VK_F);
           tmp.Add(34, VirtualKeyCode.VK_G);
           tmp.Add(35, VirtualKeyCode.VK_H);
           tmp.Add(36, VirtualKeyCode.VK_J);
           tmp.Add(37, VirtualKeyCode.VK_K);
           tmp.Add(38, VirtualKeyCode.VK_L);

           tmp.Add(44, VirtualKeyCode.VK_Y);
           tmp.Add(45, VirtualKeyCode.VK_X);
           tmp.Add(46, VirtualKeyCode.VK_C);
           tmp.Add(47, VirtualKeyCode.VK_V);
           tmp.Add(48, VirtualKeyCode.VK_B);
           tmp.Add(49, VirtualKeyCode.VK_N);
           tmp.Add(50, VirtualKeyCode.VK_M);
           return tmp;
       }
    }
}
