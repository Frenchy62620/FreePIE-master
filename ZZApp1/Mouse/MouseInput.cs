using FreePIE.HookInput.API;
using System;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;

namespace FreePIE.HookInput.Mouse
{
    public enum Mouse
    {
        Left = 238,
        Right,
        Middle,
        X1,
        X2,
        WheelFwd,
        WheelBwd,
        XY = 64
    }
    internal class MouseInput
    {
        private readonly AllInputsCommander aic;
        private const Int32 WM_MOUSEMOVE = 0x0200;

        private const Int32 WM_LBUTTONDOWN = 0x0201;
        private const Int32 WM_LBUTTONUP = 0x0202;
        private const Int32 WM_LBUTTONDBLCLK = 0x0203;
        private const Int32 WM_RBUTTONDOWN = 0x0204;
        private const Int32 WM_RBUTTONUP = 0x0205;
        private const Int32 WM_RBUTTONDBLCLK = 0x0206;
        private const Int32 WM_MBUTTONDOWN = 0x0207;
        private const Int32 WM_MBUTTONUP = 0x0208;
        private const Int32 WM_MBUTTONDBLCLK = 0x0209;

        private const Int32 WM_MOUSEWHEEL = 0x020A;

        private const Int32 WM_XBUTTONDOWN = 0x020B;
        private const Int32 WM_XBUTTONUP = 0x020C;
        private const Int32 WM_XBUTTONDBLCLK = 0x020D;

        private MemoryMappedViewAccessor accessor;

        private bool hooked = false;

        private WindowsHookAPI.HookDelegate mouseDelegate;
        private IntPtr mouseHandle;


        private const Int32 WH_MOUSE_LL = 14;

        public MouseInput(AllInputsCommander allInputsCommander)
        {
            mouseDelegate = MouseHookDelegate;
            aic = allInputsCommander;
            accessor = aic.accessor;
        }

        public void setHook(bool on)
        {
            if (hooked == on) return;
            if (on)
            {
                mouseHandle = WindowsHookAPI.SetWindowsHookEx(WH_MOUSE_LL, mouseDelegate, IntPtr.Zero, 0);
                if (mouseHandle != IntPtr.Zero) hooked = true;
            }
            else
            {
                WindowsHookAPI.UnhookWindowsHookEx(mouseHandle);
                hooked = false;
            }
        }
        private IntPtr MouseHookDelegate(Int32 Code, IntPtr wParam, IntPtr lParam)
        {
            //VK_LBUTTON    0x01 Left mouse button
            //VK_RBUTTON    0x02 Right mouse button
            //VK_MBUTTON    0x04 Middle mouse button
            //VK_XBUTTON1   0x05 X1 mouse button
            //VK_XBUTTON2   0x06 X2 mouse button
            // see https://msdn.microsoft.com/en-us/library/windows/desktop/ms644970(v=vs.85).aspx

            //mouseData:
            //If the message is WM_MOUSEWHEEL, the high-order word of this member is the wheel delta.The low-order word is reserved.
            //    A positive value indicates that the wheel was rotated forward, away from the user;
            //    a negative value indicates that the wheel was rotated backward, toward the user. 
            //    One wheel click is defined as WHEEL_DELTA, which is 120.(0x78 or 0xFF88)
            //If the message is WM_XBUTTONDOWN, WM_XBUTTONUP, WM_XBUTTONDBLCLK, WM_NCXBUTTONDOWN, WM_NCXBUTTONUP, or WM_NCXBUTTONDBLCLK, 
            //    the high - order word specifies which X button was pressed or released, 
            //    and the low - order word is reserved.This value can be one or more of the following values.Otherwise, mouseData is not used.
            //XBUTTON1  = 0x0001 The first X button was pressed or released.
            //XBUTTON2  = 0x0002  The second X button was pressed or released.

            MSLLHOOKSTRUCT lparam = (MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));
            int command = (int)wParam;
            if (Code < 0 || command == WM_LBUTTONDBLCLK || command == WM_RBUTTONDBLCLK)
                return WindowsHookAPI.CallNextHookEx(mouseHandle, Code, wParam, lParam);

            //if ((int)wParam != 512)
            //    Console.WriteLine($"x: {lparam.pt.x}  y: {lparam.pt.y} f: {lparam.flags}  moudata: {(Int16)(lparam.mouseData >> 16)}  wp: {(int)wParam}");

            if (command == WM_MOUSEMOVE)
            {
                int xy = lparam.pt.x + (lparam.pt.y << 16);
                //Console.WriteLine($"xy:{ xy} x:{lparam.pt.x} y:{lparam.pt.y} y<<16:{(lparam.pt.y)<<16}");
                accessor.Write((long)Mouse.XY, xy);
            }
            else if (command == WM_XBUTTONDOWN || command == WM_XBUTTONUP)
            {
                int numbutton = ((int)lparam.mouseData >> 16) - 1;
                aic.writeBit((int)Mouse.X1 + numbutton, command == WM_XBUTTONDOWN);
                //Console.WriteLine($"action={aic.readBit((int)Mouse.X1 + numbutton)}");
                if (IsHooked((int)Mouse.X1 + numbutton)) return (IntPtr)1;
            }
            else if (command == WM_LBUTTONDOWN || command == WM_LBUTTONUP)
            {
                aic.writeBit((int)Mouse.Left, command == WM_LBUTTONDOWN);
                if (IsHooked((int)Mouse.Left)) return (IntPtr)1;
            }
            else if (command == WM_RBUTTONDOWN || command == WM_RBUTTONUP)
            {
                aic.writeBit((int)Mouse.Right, command == WM_RBUTTONDOWN);
                if (IsHooked((int)Mouse.Right)) return (IntPtr)1;
            }
            else if (command == WM_MBUTTONDOWN || command == WM_MBUTTONUP)
            {
                aic.writeBit((int)Mouse.Middle, command == WM_MBUTTONDOWN);
                Console.WriteLine($"action={aic.readBit((int)Mouse.Middle)}");
                if (IsHooked((int)Mouse.Middle)) return (IntPtr)1;
            }
            else if (command == WM_MOUSEWHEEL)
            {
                int wheelvalue = (Int16)(lparam.mouseData >> 16) < 0 ? 1 : 0; // Forward = 0, Backward = 1
                if (!aic.readBit((int)Mouse.WheelFwd + wheelvalue))
                    aic.writeBit((int)Mouse.WheelFwd + wheelvalue, true);
                if (IsHooked((int)Mouse.WheelFwd + wheelvalue)) return (IntPtr)1;
            }


            return WindowsHookAPI.CallNextHookEx(mouseHandle, Code, wParam, lParam);
        }

        private bool IsHooked(int action) => aic.readBit(action + 256);

        //private bool IsHooked(bool commanddown, int action)
        //{
        //    if (aic.readBit(action + 256))
        //    {
        //        aic.writeBit(action, commanddown);
        //        //Console.WriteLine($"hooked:true  action={action}");
        //        return true;
        //    }
        //    //Console.WriteLine($"hooked:false  action={action}");
        //    return false;
        //}

        ~MouseInput()
        {

        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        ////Valeurs issues de Winuser.h du SDK de Microsoft.
        ///// <summary>
        ///// Windows NT/2000/XP: Installe un hook pour la souris
        ///// </summary>
        //private const int WH_MOUSE_LL = 14;
        ///// <summary>
        ///// Windows NT/2000/XP: Installe un hook pour le clavier
        ///// </summary>
        //private const int WH_KEYBOARD_LL = 13;

        //private const int WH_MOUSE = 7;

        //private const int WH_KEYBOARD = 2;

        ///// <summary>
        ///// Le message WM_MOUSEMOVE est envoyé quand la souris bouge
        ///// </summary>
        //private const int WM_MOUSEMOVE = 0x200;
        ///// <summary>
        ///// Le message WM_LBUTTONDOWN est envoyé lorsque le bouton gauche est pressé
        ///// </summary>
        //private const int WM_LBUTTONDOWN = 0x201;
        ///// <summary>
        ///// Le message WM_RBUTTONDOWN est envoyé lorsque le bouton droit est pressé
        ///// </summary>
        //private const int WM_RBUTTONDOWN = 0x204;
        ///// <summary>
        ///// Le message WM_MBUTTONDOWN est envoyé lorsque le bouton central est pressé
        ///// </summary>
        //private const int WM_MBUTTONDOWN = 0x207;
        ///// <summary>
        ///// Le message WM_LBUTTONUP est envoyé lorsque le bouton gauche est relevé
        ///// </summary>
        //private const int WM_LBUTTONUP = 0x202;
        ///// <summary>
        ///// Le message WM_RBUTTONUP est envoyé lorsque le bouton droit est relevé 
        ///// </summary>
        //private const int WM_RBUTTONUP = 0x205;

        //private const int WM_MBUTTONUP = 0x208;

        //private const int WM_LBUTTONDBLCLK = 0x203;

        //private const int WM_RBUTTONDBLCLK = 0x206;

        //private const int WM_MBUTTONDBLCLK = 0x209;

        //private const int WM_MOUSEWHEEL = 0x020A;


        //private const int WM_KEYDOWN = 0x100;

        //private const int WM_KEYUP = 0x101;

        //private const int WM_SYSKEYDOWN = 0x104;

        //private const int WM_SYSKEYUP = 0x105;

        //private const byte VK_SHIFT = 0x10;
        //private const byte VK_CAPITAL = 0x14;
        //private const byte VK_NUMLOCK = 0x90;
    }
}