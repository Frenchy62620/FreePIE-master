using FreePIE.HookInput.Keyboard;
using FreePIE.HookInput.Mouse;
using System;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FreePIE.HookInput
{
    public class AllInputsCommander
    {
        // creates the memory mapped file
        // memory = MemoryMappedFile.OpenExisting("hookFreePIE");
        private MemoryMappedFile memory;
        public MemoryMappedViewAccessor accessor;
        private EventWaitHandle ewh;
        private bool running;

        private KeyboardInput keyboardInputHook = null;
        private MouseInput mouseInputHook = null;

        public readonly int STOP_PROCESS = 0;     // bit onoff Hook On/Off
        public readonly int HOOK_KEYBOARD = 245;  // bit onoff Hook On/Off
        public readonly int HOOK_MOUSE = 256;     // bit onoff Hook On/Off
        public readonly int BUFFERED = 246;     // bit onoff Hook On/Off
        public readonly int GETDATAFLAG = 247;     // bit onoff Hook On/Off
        public readonly int DATA = 248;

        private int hookstatus;

        public AllInputsCommander(int hookstatus)
        {
            memory = MemoryMappedFile.CreateOrOpen("hookFreePIE", 68, MemoryMappedFileAccess.ReadWrite);
            accessor = memory.CreateViewAccessor();
            ewh = EventWaitHandle.OpenExisting("ewhFreePIE");
            this.hookstatus = hookstatus;

            if ((hookstatus & 1) != 0)
                keyboardInputHook = new KeyboardInput(this);
            if ((hookstatus & 2) != 0)
                mouseInputHook = new MouseInput(this);

            running = true;
        }

        private bool HKeyboard
        {
            get => readBit(HOOK_KEYBOARD);
            set => writeBit(HOOK_KEYBOARD, value);
        }

        private bool HMouse
        {
            get => readBit(HOOK_MOUSE);
            set => writeBit(HOOK_MOUSE, value);
        }

        private bool HStopProcess
        {
            get => readBit(STOP_PROCESS);
            set => writeBit(STOP_PROCESS, value);
        }



        public bool readBit(int pos)
        {
            var numInt = pos / 32;
            var numBit = pos % 32;
            return (accessor.ReadInt32(numInt * 4) & (1 << numBit)) != 0;
        }

        public void writeBit(int pos, bool newvalue)
        {

            var numIntinByte = (pos / 32) * 4;
            var numBit = pos % 32;
            var oldvalue = accessor.ReadInt32(numIntinByte);
            int value;
            if (newvalue) // Key blocked? 0 1-220  221,222,223||224 225-444 445,446,447 (7+7 mots de 32 bits)
                value = oldvalue | (1 << numBit);
            else
                value = oldvalue & ~(1 << numBit);

            accessor.Write(numIntinByte, value);
        }

        public void StartLoop()
        {
            if (hookstatus == 0)
                running = false;

            while (running)
            {
                ewh.WaitOne();
                if (HStopProcess)
                {
                    HKeyboard = false;
                    HMouse = false;
                    running = false;
                }

                keyboardInputHook?.setHook(HKeyboard);
                mouseInputHook?.setHook(HMouse);
            }

            accessor.Dispose();
            memory.Dispose();
            ewh.Dispose();

            ewh = null;
            accessor = null;
            memory = null;
            
            System.Windows.Application.Current.Shutdown();
        }
    }
}

