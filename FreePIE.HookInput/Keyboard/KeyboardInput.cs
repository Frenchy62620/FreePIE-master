using FreePIE.HookInput.API;
using System;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;

namespace FreePIE.HookInput.Keyboard
{
    public class KeyboardInput
    {
        private WindowsHookAPI.HookDelegate keyBoardDelegate;
        private IntPtr keyBoardHandle;

        private MemoryMappedViewAccessor accessor;
        // Hook global keyboard
        private const Int32 WH_KEYBOARD_LL          = 13;
        // flags bits of lParam hookstruct
        private const Int32 LLKHF_EXTENDED          = 0b00000001;
        private const Int32 LLKHF_LOWER_IL_INJECTED = 0b00000010;
        private const Int32 LLKHF_INJECTED          = 0b00010000;
        private const Int32 LLKHF_ALTDOWN           = 0b00100000;
        private const Int32 LLKHF_UP                = 0b10000000;
        // value of wParam
        private const Int32 WM_KEYUP                = 0x0101;
        private const Int32 WM_KEYDOWN              = 0x0100;

        private const string authorizedkeys = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789+-.;";

        private bool hooked = false;
        private readonly AllInputsCommander aic; 

        public KeyboardInput(AllInputsCommander allInputsCommander)
        {
            keyBoardDelegate = KeyboardHookDelegate;
            aic = allInputsCommander;
            accessor = aic.accessor;
        }
        public bool Buffered => aic.readBit(aic.BUFFERED);
        public bool DataClear => accessor.ReadByte(Data_ADR) == 0;
        public int Data_ADR => aic.DATA >> 3;

        public void setHook(bool on)
        {
            if (hooked == on) return;
            if (on)
            {
                keyBoardHandle = WindowsHookAPI.SetWindowsHookEx(WH_KEYBOARD_LL, keyBoardDelegate, IntPtr.Zero, 0);
                if (keyBoardHandle != IntPtr.Zero) hooked = true;
            }
            else
            {
                WindowsHookAPI.UnhookWindowsHookEx(keyBoardHandle);
                hooked = false;
            }
        }



        private IntPtr KeyboardHookDelegate(Int32 Code, IntPtr wParam, IntPtr lParam)
        {

            hookStruct param = (hookStruct)Marshal.PtrToStructure(lParam, typeof(hookStruct));
            if (Code < 0 || (param.flags & LLKHF_INJECTED) != 0)
            {
                return WindowsHookAPI.CallNextHookEx(keyBoardHandle, Code, wParam, lParam);
            }
            int scanCode = param.scanCode;

            if ((param.flags & LLKHF_EXTENDED) != 0)
            {
                // NumberPadEnter, PageUp, PageDown, End, Home, LeftArrow, UpArrow, rightArrow, DownArrow, Insert, Delete, NumberPadstar
                if (param.vkCode == 13 || (param.vkCode >= 33 && param.vkCode <= 40) || param.vkCode == 45 || param.vkCode == 46 || param.vkCode == 111)
                    scanCode = param.scanCode + 128;
            }

            if ((int)wParam == WM_KEYDOWN)
            {

                aic.writeBit(scanCode, true); // set bit key
                if (Buffered && DataClear)
                {
                    var chr = (char)WindowsHookAPI.MapVirtualKey((uint)param.vkCode, 2);
                    if (authorizedkeys.Contains(chr.ToString()))
                        accessor.Write(position: aic.DATA >> 3, value: (byte)chr);
                }
#if DEBUG
                Console.WriteLine($"Vk: {param.vkCode}  Scancode: {param.scanCode} flags: {param.flags} wparam: {(int)wParam}  otherscancode: {scanCode}");
                Console.WriteLine($"Aa: {WindowsHookAPI.MapVirtualKey((uint)param.vkCode, 2)},{(char)WindowsHookAPI.MapVirtualKey((uint)param.vkCode, 2)}");
                Console.WriteLine($"Vk: {param.vkCode}  Scancode: {param.scanCode} flags: {param.flags} wparam: {(int)wParam}, scancal: {WindowsHookAPI.MapVirtualKey((uint)param.vkCode, 0)}");
#endif
            }
            else
                aic.writeBit(scanCode, false); ; // raz bit key

            if (aic.readBit(scanCode + 256) || aic.readBit(aic.BUFFERED)) // Key hooked?
            {
                return (IntPtr)1;
            }

            return WindowsHookAPI.CallNextHookEx(keyBoardHandle, Code, wParam, lParam);
        }


        ~KeyboardInput()
        {

        }

        public struct hookStruct
        {
            public int vkCode;
            public int scanCode;
            public int flags;
            public int time;
            public int dwExtraInfo;
        }
    }
}
