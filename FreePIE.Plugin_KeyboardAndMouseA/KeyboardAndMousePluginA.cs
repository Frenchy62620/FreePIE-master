using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using FreePIE.Core.Contracts;
using FreePIE.Plugin_KeyboardAndMouseA.ScriptAuto;
using SharpDX.DirectInput;
using static FreePIE.CommonTools.GlobalTools;
using FreePIE.CommonStrategy;
using SharpDX;
using System.IO.MemoryMappedFiles;
using System.Threading;
using FreePIE.CommonEnum;
using Key = FreePIE.CommonEnum.Key;
using Mouse = FreePIE.CommonEnum.Mouse;
//using SlimDX.DirectInput;
//using AutoIt;
// ajouter free.core  dans refererence pour NeedIndexer
//using FreePIE.Core.ScriptEngine.Globals.ScriptHelpers;   

namespace FreePIE.Plugin_KeyboardAndMouseA
{
    //[GlobalEnum]
    //public enum HOOK
    //{
    //    STOP_PROCESS = 0,
    //    KEYBOARD = 245,
    //    BUFFERED = 246,
    //    GETDATAFLAG = 247,
    //    DATA = 248,
    //    MOUSE = 256
    //}

    [GlobalType(Type = typeof(KeyboardAndMouseAGlobal))]
    public class KeyboardAndMousePluginA : Plugin
    {

        private HashSet<int> extendedKeyMap = new HashSet<int>() { 121, 125, 144, 146, 149, 153, 156, 157, 160, 161, 162, 164, 174, 176, 178, 181, 183, 184, 197, 199, 200, 201, 203, 205, 207, 208, 209, 210, 211, 219, 220, 221, 222, 223, 227, 229, 230, 231, 232, 233, 234, 235, 236, 237 };

        private EventWaitHandle ewh;
        private MemoryMappedFile memory;
        public MemoryMappedViewAccessor accessor;

        public List<int> KeysPressed;
        private ScriptKeyboardAndMouseA SF;

        private DirectInput DirectInputInstance = new DirectInput();

        private SharpDX.DirectInput.Keyboard KeyboardDevice;
        private KeyboardState KeyState = new KeyboardState();
        private bool[] MyKeyDown = new bool[256];

        private SharpDX.DirectInput.Mouse mouseDevice;
        private MouseState currentMouseState;
        private double deltaXOut;
        private double deltaYOut;

        public int WheelValue { get; internal set; }
        public int Wheelturn = 0;
        public int[] Codes;
        public long[] Tempo;
        private int wheel;
        public const int WheelMax = 120;
        private Point point;

        //public readonly Dictionary<int, int> ToAzerty = new Dictionary<int, int>()
        //{
        //    { (int)Key.A, (int)Key.Q},
        //    { (int)Key.Q, (int)Key.A},
        //    { (int)Key.Z, (int)Key.W},
        //    { (int)Key.W, (int)Key.Z},
        //    { (int)Key.Semicolon, (int)Key.M},
        //    { (int)Key.Comma, (int)Key.Semicolon},
        //    { (int)Key.M, (int)Key.Comma}
        //};

        public SetPressedStrategy<int> setPressedStrategy;
        public GetPressedStrategy<int> getPressedStrategy;

        public override object CreateGlobal()
        {
            return new KeyboardAndMouseAGlobal(this);
        }

        public override string FriendlyName
        {
            get { return "KeyboardAndMouseA"; }
        }

        public override Action Start()
        {

            IntPtr handle = Process.GetCurrentProcess().MainWindowHandle;

            KeyboardDevice = new SharpDX.DirectInput.Keyboard(DirectInputInstance);
            if (KeyboardDevice == null)
                throw new Exception("Failed to create keyboard device");

            KeyboardDevice.SetCooperativeLevel(handle, CooperativeLevel.Background | CooperativeLevel.NonExclusive);
            KeyboardDevice.Acquire();

            KeyboardDevice.GetCurrentState(ref KeyState);




            mouseDevice = new SharpDX.DirectInput.Mouse(DirectInputInstance);
            if (mouseDevice == null)
                throw new Exception("Failed to create mouse device");

            mouseDevice.SetCooperativeLevel(handle, CooperativeLevel.Background | CooperativeLevel.NonExclusive);
            mouseDevice.Properties.AxisMode = DeviceAxisMode.Relative;   // Get delta values
            mouseDevice.Acquire();



            getPressedStrategy = new GetPressedStrategy<int>(IsDown);
            setPressedStrategy = new SetPressedStrategy<int>(KeyOrButtonDown, KeyOrButtonUp);

            OnStarted(this, new EventArgs());
            return null;
        }

        public void KeyOrButtonDown(int code)
        {
            if (code <= (int)Key.LastKey)
                SetKeyPressed(code, true);
            else
                SetButtonPressed(code, true);
        }
        public void KeyOrButtonUp(int code)
        {
            if (code <= (int)Key.LastKey)
                SetKeyPressed(code, false);
            else
                SetButtonPressed(code, false);
        }

        public override void Stop()
        {
            SF = null;
            // Don't leave any keys pressed
            for (int i = 1; i < MyKeyDown.Length; i++)
                if (MyKeyDown[i]) KeyOrButtonUp(i);

            if (KeyboardDevice != null)
            {
                KeyboardDevice.Unacquire();
                KeyboardDevice.Dispose();
                KeyboardDevice = null;
            }

            if (mouseDevice != null)
            {
                mouseDevice.Unacquire();
                mouseDevice.Dispose();
                mouseDevice = null;
            }

            if (DirectInputInstance != null)
            {
                DirectInputInstance.Dispose();
                DirectInputInstance = null;
            }
        }
        public override void DoBeforeNextExecute()
        {
            KeyboardDevice.GetCurrentState(ref KeyState);

            if (memory != null)
            {
                KeysPressed.Clear();
                for (int i = 1; i < 238; i++)
                {
                    if (IsDown(i))
                        KeysPressed.Add(i);
                }
            }

            if (Wheelturn != 0)
            {
                setPressedStrategy.Add(Wheelturn < 0 ? (int)Mouse.WheelBwd : (int)Mouse.WheelFwd);
                Wheelturn = Wheelturn - Math.Sign(Wheelturn);
            }

            if (Tempo != null && Tempo[0].GetLapse() >= Tempo[1])
            {
                Tempo[0] = StopTimer();
                Tempo = null;
                foreach (var code in Codes.Reverse())
                    KeyOrButtonUp(code);
                Codes = null;
            }

            setPressedStrategy.Do();
            CheckScriptTimer();

            if ((cmd == 'I') && setPressedStrategy.IsListEmpty())
            {
                if (SF == null)
                {
                    SF = new ScriptKeyboardAndMouseA(this);
                }
                SF.KeyboardAndMouseA();
            }

            // If a mouse command was given in the script, issue it all at once right here
            if ((int)deltaXOut != 0 || (int)deltaYOut != 0 || wheel != 0)
            {
                var input = new MouseKeyIO.INPUT[1];
                input[0].type = MouseKeyIO.INPUT_MOUSE;
                input[0].mi = MouseInput((int)deltaXOut, (int)deltaYOut, (uint)wheel, 0, MouseKeyIO.MOUSEEVENTF_MOVE | MouseKeyIO.MOUSEEVENTF_WHEEL);

                MouseKeyIO.NativeMethods.SendInput(1, input, Marshal.SizeOf(input[0].GetType()));

                // Reset the mouse values
                if ((int)deltaXOut != 0)
                {
                    deltaXOut = deltaXOut - (int)deltaXOut;
                }
                if ((int)deltaYOut != 0)
                {
                    deltaYOut = deltaYOut - (int)deltaYOut;
                }

                wheel = 0;
            }
            currentMouseState = null;  // flush the mouse state
        }

        // Mouse Activity --------------------------------------------------------------------------------------------------
        private MouseState CurrentMouseState => currentMouseState ?? (currentMouseState = mouseDevice.GetCurrentState());

        public Point getmousePos
        {
            get
            {
                MouseKeyIO.NativeMethods.GetCursorPos(out point);
                return point;
            }
        }

        public void setmousePos(int x, int y) => MouseKeyIO.NativeMethods.SetCursorPos(x, y);

        public bool SetEnhancePointerPrecision(int state = -1)
        {
            int[] mouseParams = new int[3];//necessary for enhancePointer
            if (state < 0)
            {
                // toggle value, Get the current values.
                MouseKeyIO.NativeMethods.SystemParametersInfoGet(MouseKeyIO.NativeMethods.SPI_GETMOUSE, 
                                                                    0, 
                                                                    GCHandle.Alloc(mouseParams, GCHandleType.Pinned).AddrOfPinnedObject(), 0);
                mouseParams[2] = 1 - mouseParams[2];
            }
            else
                mouseParams[2] = state;
            // Update the system setting.
            return MouseKeyIO.NativeMethods.SystemParametersInfoSet(MouseKeyIO.NativeMethods.SPI_SETMOUSE,
                                                                    0, 
                                                                    GCHandle.Alloc(mouseParams, GCHandleType.Pinned).AddrOfPinnedObject(), MouseKeyIO.SPIF.SPIF_SENDCHANGE);
        }

        public double DeltaX
        {
            set { deltaXOut = deltaXOut + value; }
            get { return CurrentMouseState.X; }
        }

        public double DeltaY
        {
            set { deltaYOut = deltaYOut + value; }
            get { return CurrentMouseState.Y; }
        }

        public int Wheel
        {
            get { return CurrentMouseState.Z; }
            set { wheel = value; }
        }

        // -----------------------------------------------------------------------------------------------------------------
        public void setBit(int keycode, bool on)
        {
            if (accessor == null) return;
            var numIntinByte = (keycode / 32) * 4;
            var numBit = keycode % 32;
            var value = accessor.ReadInt32(numIntinByte);
            if (on)
                accessor.Write(numIntinByte, value | (1 << numBit));   // set bit key
            else
                accessor.Write(numIntinByte, value & ~(1 << numBit));  // raz bit key
        }

        public bool getBit(int keycode)
        {
            if (accessor == null) return false;
            var numIntinByte = (keycode / 32) * 4;
            var numBit = keycode % 32;
            return (accessor.ReadInt32(numIntinByte) & (1 << numBit)) != 0;  // bit on?
        }
        public bool isLedScrollLockOn() => System.Windows.Forms.Control.IsKeyLocked(System.Windows.Forms.Keys.Scroll);

        public bool IsClicked(int code, bool doubleclicked, bool hook)
        {
            return doubleclicked ? getPressedStrategy.IsDoubleClicked(code, hook) : getPressedStrategy.IsSingleClicked(code, hook);
        }

        public int HeldDown(int code, int nbvalue, int lapse, bool hook) => getPressedStrategy.HeldDown(code, IsDown(code, hook), nbvalue, lapse);
        public void HeldDownStop(int code) => getPressedStrategy.HeldDownStop(code);

        public bool IsDown(int code, bool hook = false)
        {
            if (hook && memory != null)
                return getBit(code) || MyKeyDown[code];

            if (code <= (int)Key.LastKey)
                return KeyState.IsPressed((SharpDX.DirectInput.Key)code) || MyKeyDown[code];

            return CurrentMouseState.Buttons[code - (int)Mouse.Left] || MyKeyDown[code];
        }
        public bool IsUp(int code, bool hook) => !IsDown(code, hook);

        public bool IsPressed(int code, bool hook = false) => getPressedStrategy.IsPressed(code, hook);
        public bool IsReleased(int code, bool hook = false) => getPressedStrategy.IsReleased(code, hook);


        public int Keyspressed => KeyState.PressedKeys.Count;
        public List<Key> ListKeyspressed => KeyState.PressedKeys.Cast<Key>().ToList();
        public List<int> ListIntspressed => KeyState.PressedKeys.Cast<int>().ToList();

        public void PressAndRelease(int code) => setPressedStrategy.Add(code);
        public void PressAndRelease(int code, bool state) => setPressedStrategy.Add(code, state);


        // ButtonDown and ButtonUp
        public void SetButtonPressed(int button, bool pressed)
        {
            if (MyKeyDown[button] == pressed) return;
            MyKeyDown[button] = pressed;

            uint btn_flag;
            int mousedata = 0;
            var reelbutton = button - (int)Mouse.Left;
            var up = !pressed ? 2 : 1;
            if (reelbutton == 3 || reelbutton == 4)
                mousedata = reelbutton - 3 + (int)MouseKeyIO.XBUTTON1;

            btn_flag = (mousedata > 0 ? MouseKeyIO.MOUSEEVENTF_XDOWN : MouseKeyIO.MOUSEEVENTF_LEFTDOWN << (reelbutton * 2)) * (uint)up;

            var input = new MouseKeyIO.INPUT[1];
            input[0].type = MouseKeyIO.INPUT_MOUSE;
            input[0].mi = MouseInput(0, 0, (uint)mousedata, 0, btn_flag);
            MouseKeyIO.NativeMethods.SendInput(1, input, Marshal.SizeOf(input[0].GetType()));
        }
        private MouseKeyIO.MOUSEINPUT MouseInput(int x, int y, uint data, uint t, uint flag)
        {
            return new MouseKeyIO.MOUSEINPUT { dx = x, dy = y, mouseData = data, time = t, dwFlags = flag };
        }
        // KeyDown and KeyUp


        public void SetKeyPressed(int code, bool pressed)
        {
            if (MyKeyDown[code] == pressed || code < 1 || code > 255) return;
            MyKeyDown[code] = pressed;
            var input = new MouseKeyIO.INPUT[1];
            input[0].type = MouseKeyIO.INPUT_KEYBOARD;

            if (pressed)
            {
                input[0].ki = KeyInput((ushort)code, extendedKeyMap.Contains(code) ? MouseKeyIO.KEYEVENTF_EXTENDEDKEY : 0);
            }
            else
            {
                if (extendedKeyMap.Contains(code))
                    input[0].ki = KeyInput((ushort)code,
                        MouseKeyIO.KEYEVENTF_EXTENDEDKEY | MouseKeyIO.KEYEVENTF_KEYUP);
                else
                    input[0].ki = KeyInput((ushort)code, MouseKeyIO.KEYEVENTF_KEYUP);
            }
            MouseKeyIO.NativeMethods.SendInput(1, input, Marshal.SizeOf(input[0].GetType()));
        }
        private MouseKeyIO.KEYBDINPUT KeyInput(ushort code, uint flag)
        {
            return new MouseKeyIO.KEYBDINPUT { wVk = 0, wScan = code, time = 0, dwExtraInfo = IntPtr.Zero, dwFlags = flag | MouseKeyIO.KEYEVENTF_SCANCODE };
        }

        private void Hook(int keyorbutton, bool on) => setBit(keyorbutton + 256, on);
        public void swallow(string key)
        {
            bool toblock = true;
            if (key[0].Equals('+') || key[0].Equals('-'))
            {
                toblock = !key[0].Equals('-');
                key = key.Substring(1);
            }
            if (key.Contains("$dcs"))
                key = key.Replace("$dcs", "$alpha,$num,NumberPadSlash,NumberPadStar,NumberPadPlus,NumberPadMinus,NumberPadEnter,NumberPadPeriod,Insert,Delete,End,Home");
            var kys = key.Split(',');

            foreach (var ky in kys)
            {
                if (ky.Contains("$allkeys"))
                {
                    for (int k = 1; k <= (int)Key.LastKey; k++)
                        Hook(k, toblock);
                }
                else if (ky.Contains("$allbuttons"))
                {
                    var minvalue = (int)Mouse.Left;
                    var maxvalue = (int)Mouse.LastButton;
                    for (int k = minvalue; k <= maxvalue; k++)
                        Hook(k, toblock);
                }
                else if (ky.Contains("$alpha"))
                {
                    for (char k = 'A'; k <= 'Z'; k++)
                        Hook((int)(ToEnum<Key>(k.ToString())), toblock);

                    Hook((int)Key.Semicolon, toblock);
                    Hook((int)Key.Comma, toblock);
                }
                else if (ky.Contains("$num"))
                {
                    for (char k = '0'; k <= '9'; k++)
                        Hook((int)(ToEnum<Key>($"NumberPad{k}")), toblock);
                }
                else if (ky.Contains("$left"))
                {
                    Hook((int)Mouse.Left, toblock);
                }
                else if (ky.Contains("$right"))
                {
                    Hook((int)Mouse.Right, toblock);
                }
                else if (ky.Contains("$middle"))
                {
                    Hook((int)Mouse.Middle, toblock);
                }
                else if (ky.Contains("$x1"))
                {
                    Hook((int)Mouse.X1, toblock);
                }
                else if (ky.Contains("$x2"))
                {
                    Hook((int)Mouse.X2, toblock);
                }
                else if (ky.Contains("$wheel"))
                {
                    Hook((int)Mouse.WheelFwd, toblock);
                }
                else if (ky.Contains("$char:"))
                {
                    var ke = ky.Split(':');
                    foreach (var ch in ke[1])
                        Hook((int)ToEnum<Key>(ch.ToString()), toblock);
                }
                else if (ky.Contains("$int:"))
                {
                    var num = ky.Split(':')[1].Split(';');
                    for (int k = 1; k < num.Count(); k++)
                        Hook(Convert.ToInt32(num[k]), toblock);
                }
                else
                {
                    Hook((int)ToEnum<Key>(ky), toblock);
                }
            }
        }

        public void setHook(int adr, bool on)
        {
            if (adr == (int)HOOK.BOTH)
            {
                setBit((int)HOOK.KEYBOARD, on);
                setBit((int)HOOK.MOUSE, on);
            }
            else
                setBit(adr, on);

            ewh.Set();
        }
        public void setHookProgram(int hookdevice = 0) //1 = Keyboard, 2 = Mouse, 3 = all
        {
            if (hookdevice == (int)HOOK.KEYBOARD)
                hookdevice = 1;
            else if (hookdevice == (int)HOOK.MOUSE)
                hookdevice = 2;
            else if (hookdevice == (int)HOOK.BOTH)
                hookdevice = 3;
            else
            {
                setHook((int)HOOK.STOP_PROCESS, true);
                accessor.Dispose();
                memory.Dispose();
                ewh.Dispose();
                return;
            }

            if (memory == null) return;

            ewh = new EventWaitHandle(false, EventResetMode.AutoReset, "ewhFreePIE");
            memory = MemoryMappedFile.CreateOrOpen("hookFreePIE", 68, MemoryMappedFileAccess.ReadWrite);
            accessor = memory.CreateViewAccessor();
            Process.Start("FreePIE.HookInput.exe", hookdevice.ToString());
        }

        public T ToEnum<T>(string value) where T : struct
        {
            return Enum.TryParse(value: value, ignoreCase: true, result: out T result) ? result : default(T);
        }
    }

    [Global(Name = "inputA")]
    public class KeyboardAndMouseAGlobal
    {
        //private bool azerty_t, azerty_s;
        private readonly KeyboardAndMousePluginA plugin;

        public _Keyboard Keyboard { get; }
        public _Mouse Mouse { get; }


        public KeyboardAndMouseAGlobal(KeyboardAndMousePluginA plugin)
        {
            this.plugin = plugin;
            Keyboard = new _Keyboard(this.plugin);
            Mouse = new _Mouse(this.plugin);
        }

        private long starttimer;
        public long startTimer => starttimer = StartTimer();
        public long stopTimer
        {
            get
            {
                var lapse = starttimer.GetLapse();
                StopTimer();
                return lapse;
            }
        }
        //public async void sendBatchAsync(string name, string section = null, int priority = 0)
        //{
        //    await Task.Run(() => { name.LoadBatchFile(section, priority); });
        //}
        public void sendBatch(string name, string section = null, int priority = 0) => name.LoadBatchFile(section, priority);
        public void sendCommand(string cmde, int priority = 0) => cmde.AddNewCommand(priority);



        //----------------------- button/key actions ----------------------------------------------------
        public bool getClicked<T>(T code, bool dblclick = false, bool hook = false) => plugin.IsClicked(Convert.ToInt32(code), dblclick, hook);
        public bool getPressed<T>(T code, bool hook = false) => plugin.IsPressed(Convert.ToInt32(code), hook);
        public bool getReleased<T>(T code, bool hook = false) => plugin.IsReleased(Convert.ToInt32(code), hook);
        public bool getDown<T>(T code, bool hook = false) => plugin.IsDown(Convert.ToInt32(code), hook);
        //-----------------------------------------------------------------------------------------------

        //----------------------- button/key Helddown ---------------------------------------------------
        public int getHeldDown<T>(T code, int nbvalue, int duration, bool hook) => plugin.HeldDown(Convert.ToInt32(code), nbvalue, duration, hook);
        public void getHeldDownStop<T>(T code) => plugin.HeldDownStop(Convert.ToInt32(code));
        //-----------------------------------------------------------------------------------------------
        //public bool getDown<T>(IList<T> codes)
        //{
        //    foreach (var code in codes)
        //        if (!plugin.IsDown(Convert.ToInt32(code))) return false;
        //    return true;
        //}
        public bool getUp<T>(T key, bool hook = false) => plugin.IsUp(Convert.ToInt32(key), hook);


        //public string getNamekey(int i) => Enum.GetName(typeof(Key), i);
        //public Key getNamekey1(string k)
        //{
        //    //Enum.TryParse(k, out Key mykey);
        //    return (Key)Enum.Parse(typeof(Key), k, true);
        //}

        public void set<T>(T code, bool down)
        {
            if (down)
                plugin.KeyOrButtonDown(Convert.ToInt32(code));
            else
                plugin.KeyOrButtonUp(Convert.ToInt32(code));
        }

        public void set(IList<Key> codes, bool down, bool reverse = false)
        {
            if (reverse)
                foreach (var code in codes.Reverse())
                    set(code, down);
            else
                foreach (var code in codes)
                    set(code, down);
        }

        // One Key to press
        public void setPressed<T>(T code) where T : struct => plugin.PressAndRelease(Convert.ToInt32(code));

        // Key(s) to press one time only
        public void setPressed<T>(IList<T> codes, bool state)
        {
            foreach (var code in codes)
                plugin.PressAndRelease(Convert.ToInt32(code), state);
        }

        public void setPressed<T>(T key, bool state) where T : struct => plugin.PressAndRelease((Convert.ToInt32(key)), state);

        // list of Keys to press

        public void setPressed(int[] directionXY, params IList<Key>[] keys)
        {
            if (keys.Length < 4) return;
            for (int i = 0; i < 4; i++)
                if (i == directionXY[0] || i == directionXY[1]) setPressed(keys[i], true);
        }

        public void setPressedBip(IList<Key> keys, int frequency, int duration = 300)
        {
            foreach (var k in keys)
                plugin.PressAndRelease((int) k);
            Beep(frequency, duration);
        }

        public void HOOK_launchProgram(HOOK devices = HOOK.BOTH) => plugin.setHookProgram((int)devices);
        public void HOOK_stopProgram() => plugin.setHookProgram();
        public void HOOK_setDeviceState(HOOK devices = HOOK.BOTH, bool state = true) => plugin.setHook((int)devices, state);

        public void HOOK_swallow(IList<string> codes)
        {
            foreach (var c in codes)
                plugin.swallow(c);
        }

    }

    public class _Keyboard
    {
        private readonly KeyboardAndMousePluginA plugin;
        public _Keyboard(KeyboardAndMousePluginA plugin)
        {
            this.plugin = plugin;
        }

        //----------------------- special key -----------------------------------------------------------
        public bool xShift => plugin.IsDown((int)Key.LeftShift) || plugin.IsDown((int)Key.RightShift);
        public bool xControl => plugin.IsDown((int)Key.LeftControl) || plugin.IsDown((int)Key.RightControl);
        public bool xAlt => plugin.IsDown((int)Key.LeftAlt) || plugin.IsDown((int)Key.RightAlt);
        public bool xWin => plugin.IsDown((int)Key.LeftWindowsKey) || plugin.IsDown((int)Key.RightWindowsKey);
        //------------------------------------------------------------------------------------------------

        public bool isScrollLedOn() => plugin.isLedScrollLockOn();
        public int getNbrKeysDown => plugin.Keyspressed;
        public List<Key> getListOfKeysDown => plugin.ListKeyspressed;
        public string getBuffer() => buffer;
        public Key intTOkey(int key)
        {
            //string e = ((Key)key).ToString();
            //string t = Enum.GetName(typeof(Key), key);
            return (Key)key;
        }

        // direction Pov to list of key(s)
        public List<Key> getKeyFromPov(int direction, params IList<Key>[] keys)
        {
            if (direction < 0 || direction > 3) return null;

            List<Key> keycursor = new List<Key>();

            switch (keys.Length)
            {
                case 1:
                    keycursor.Add(keys[0][direction]);
                    break;
                case 4:
                    foreach (var ky in keys[direction])
                        keycursor.Add(ky);
                    break;
                default:
                    throw new Exception($"Number of list: {keys.Length}. Just only 1 or 4 lists of Keys.");
            }
            return keycursor;
        }
        public string getNamekey(int i) => Enum.GetName(typeof(Key), i);
        public Key getNamekey1(string k)
        {
            //Enum.TryParse(k, out Key mykey);
            return (Key)Enum.Parse(typeof(Key), k, true);
        }
    }

    public class _Mouse
    {
        private readonly KeyboardAndMousePluginA plugin;
        public _Mouse(KeyboardAndMousePluginA plugin)
        {
            this.plugin = plugin;
        }

        public bool setPointerPrecision(int state = -1 /* 0 = disable, 1 = enable, -1 toogle */) => plugin.SetEnhancePointerPrecision(state);

        public int X => plugin.getmousePos.X;
        public int Y => plugin.getmousePos.Y;

        public void setXY(int x, int y) => plugin.setmousePos(x, y);

        private int wheelMax => KeyboardAndMousePluginA.WheelMax;

        public double deltaX
        {
            get { return plugin.DeltaX; }
            set { plugin.DeltaX = value; }
        }
        public double deltaY
        {
            get { return plugin.DeltaY; }
            set { plugin.DeltaY = value; }
        }

        public int wheel
        {
            get { return plugin.Wheel; }
            set { plugin.Wheel = value; }
        }

        public bool wheelUp
        {
            get { return plugin.Wheel == wheelMax; }
            set { plugin.Wheel = value ? wheelMax : 0; }
        }

        public bool wheelDown
        {
            get { return plugin.Wheel == -wheelMax; }
            set { plugin.Wheel = value ? -wheelMax : 0; }
        }

        public int wheelvalue
        {
            get { return plugin.WheelValue; }
            set { plugin.WheelValue = value; }
        }
        public string selectWithWheel(int nbstep, IList<string> list)
        {
            var nbvalue = list.Count;
            var data = nbvalue * nbstep * wheelMax;
            wheelvalue += wheel;
            if (wheelvalue < 0) wheelvalue += data;
            var index = (wheelvalue % data) / (nbstep * wheelMax);
            if (plugin.IsPressed((int)Mouse.Middle))
            {
                return list[index];
            }
            return "";
        }
    }
}
