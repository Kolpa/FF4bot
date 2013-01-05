using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WindowsInput;

namespace FF4Bot
{
    class Keys
    {
       public static readonly Dictionary<Int32, VirtualKeyCode> vb2vk()
       {
           Dictionary<Int32, VirtualKeyCode> tmp = new Dictionary<int, VirtualKeyCode>();
           tmp.Add(30, VirtualKeyCode.VK_A);
           tmp.Add(31, VirtualKeyCode.VK_S);
           tmp.Add(32, VirtualKeyCode.VK_D);
           tmp.Add(33, VirtualKeyCode.VK_F);
           tmp.Add(34, VirtualKeyCode.VK_G);
           tmp.Add(35, VirtualKeyCode.VK_H);
           tmp.Add(36, VirtualKeyCode.VK_J);
           tmp.Add(37, VirtualKeyCode.VK_K);
           tmp.Add(38, VirtualKeyCode.VK_L);
           tmp.Add(16, VirtualKeyCode.VK_Q);
           tmp.Add(17, VirtualKeyCode.VK_W);
           tmp.Add(18, VirtualKeyCode.VK_E);
           tmp.Add(19, VirtualKeyCode.VK_R);
           return tmp;
       }
       public static readonly Dictionary<String, VirtualKeyCode> vk2dir()
       {
           Dictionary<String, VirtualKeyCode> tmp = new Dictionary<String, VirtualKeyCode>();
           tmp.Add("Up", VirtualKeyCode.VK_A);
           tmp.Add("Down", VirtualKeyCode.VK_S);
           tmp.Add("Left", VirtualKeyCode.VK_D);
           tmp.Add("Right", VirtualKeyCode.VK_F);
           tmp.Add("A", VirtualKeyCode.VK_G);
           tmp.Add("B", VirtualKeyCode.VK_H);
           tmp.Add("L", VirtualKeyCode.VK_J);
           tmp.Add("R", VirtualKeyCode.VK_K);
           tmp.Add("Select", VirtualKeyCode.VK_L);
           tmp.Add("Start", VirtualKeyCode.VK_Q);
           tmp.Add("Speed", VirtualKeyCode.VK_W);
           tmp.Add("Capture", VirtualKeyCode.VK_E);
           tmp.Add("GS", VirtualKeyCode.VK_R);
           return tmp;
       }
    }
}
