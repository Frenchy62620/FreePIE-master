using System;
using System.Runtime.InteropServices;
using static FreePIE.HookInput.Keyboard.KeyboardInput;

namespace FreePIE.HookInput.API
{
    public class WindowsHookAPI
    {
        //public delegate IntPtr HookDelegate(
        //    Int32 Code, IntPtr wParam, IntPtr lParam);
        public delegate IntPtr HookDelegate(Int32 Code, IntPtr wParam, IntPtr lParam);

        [DllImport("User32.dll")]
        public static extern IntPtr CallNextHookEx(IntPtr hHook, Int32 nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("User32.dll")]
        public static extern IntPtr UnhookWindowsHookEx(IntPtr hHook);


        [DllImport("User32.dll")]
        public static extern IntPtr SetWindowsHookEx(Int32 idHook, HookDelegate lpfn, IntPtr hmod, Int32 dwThreadId);


        /*
        MAPVK_VK_TO_CHAR 2
        uCode is a virtual-key code and is translated into an unshifted character value in the low-order word of the return value.
            Dead keys(diacritics) are indicated by setting the top bit of the return value.

        MAPVK_VK_TO_VSC 0
        uCode is a virtual-key code and is translated into a scan code.
            If it is a virtual-key code that does not distinguish between left- and right-hand keys, the left-hand scan code is returned.

        MAPVK_VSC_TO_VK 1
        uCode is a scan code and is translated into a virtual-key code that does not distinguish between left- and right-hand keys.

        MAPVK_VSC_TO_VK_EX 3
        uCode is a scan code and is translated into a virtual-key code that distinguishes between left- and right-hand keys.
            
        If there is no translation, the function returns 0.
        */
        [DllImport("user32.dll")]
        public static extern int MapVirtualKey(uint uCode, uint uMapType);
    }
}
