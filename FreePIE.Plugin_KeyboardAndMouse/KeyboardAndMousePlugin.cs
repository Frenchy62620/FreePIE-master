using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using FreePIE.Core.Contracts;
using FreePIE.Plugin_KeyboardAndMouse.ScriptAuto;
using SharpDX.DirectInput;
using static FreePIE.CommonTools.GlobalTools;
using FreePIE.CommonStrategy;
using SharpDX;
using Key = FreePIE.CommonEnum.Key;
using Mouse = FreePIE.CommonEnum.Mouse;

//using SlimDX.DirectInput;
//using AutoIt;
// ajouter free.core  dans refererence pour NeedIndexer
//using FreePIE.Core.ScriptEngine.Globals.ScriptHelpers;   

namespace FreePIE.Plugin_KeyboardAndMouse
{

    [GlobalType(Type = typeof(KeyboardAndMouseGlobal))]
    public class KeyboardAndMousePlugin : Plugin
    {

        private HashSet<int> extendedKeyMap = new HashSet<int>() { 121, 125, 144, 146, 149, 153, 156, 157, 160, 161, 162, 164, 174, 176, 178, 181, 183, 184, 197, 199, 200, 201, 203, 205, 207, 208, 209, 210, 211, 219, 220, 221, 222, 223, 227, 229, 230, 231, 232, 233, 234, 235, 236, 237 };

        private ScriptKeyboardAndMouse SF;

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
            return new KeyboardAndMouseGlobal(this);
        }

        public override string FriendlyName
        {
            get { return "KeyboardAndMouse"; }
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
                    SF = new ScriptKeyboardAndMouse(this);
                }
                SF.KeyboardAndMouse();
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

        // ------------------------------ Test Leds ------------------------------------------------------------------------
        public bool isLedScrollLockOn => System.Windows.Forms.Control.IsKeyLocked(System.Windows.Forms.Keys.Scroll);
        public bool isLedCapsLockOn => System.Windows.Forms.Control.IsKeyLocked(System.Windows.Forms.Keys.CapsLock);
        public bool isLedNumLockOn => System.Windows.Forms.Control.IsKeyLocked(System.Windows.Forms.Keys.NumLock);
        // -----------------------------------------------------------------------------------------------------------------
        public bool IsClicked(int code, bool doubleclicked) => doubleclicked ? getPressedStrategy.IsDoubleClicked(code) : getPressedStrategy.IsSingleClicked(code);
        public int HeldDown(int code, int nbvalue, int lapse) => getPressedStrategy.HeldDown(code, IsDown(code), nbvalue, lapse);
        public void HeldDownStop(int code) => getPressedStrategy.HeldDownStop(code);

        public void KeepDown(bool trigger, int code, int lapse)
        {
            if (trigger)
                PressAndRelease(code, lapse);
        }
        public bool IsDown(int code, bool value = false)
        {
            if (code <= (int)Key.LastKey)
                return KeyState.IsPressed((SharpDX.DirectInput.Key)code) || MyKeyDown[code];

            return CurrentMouseState.Buttons[code - (int)Mouse.Left] || MyKeyDown[code];
        }
        public bool IsUp(int code) => !IsDown(code);

        public bool IsPressed(int code) => getPressedStrategy.IsPressed(code);
        public bool IsReleased(int code) => getPressedStrategy.IsReleased(code);


        public int Keyspressed => KeyState.PressedKeys.Count;
        public List<Key> ListKeyspressed => KeyState.PressedKeys.Cast<Key>().ToList();
        public List<int> ListIntspressed => KeyState.PressedKeys.Cast<int>().ToList();

        public void PressAndRelease(int code ,int duration = -1) => setPressedStrategy.Add(code, duration);
        public void PressAndRelease(int code, bool state, int duration = -1) => setPressedStrategy.Add(code, state, duration);


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

    }

    [Global(Name = "input")]
    public class KeyboardAndMouseGlobal
    {
        //private bool azerty_t, azerty_s;
        private readonly KeyboardAndMousePlugin plugin;
        private readonly _Keyboard keyboard;
        private readonly _Mouse mouse;
        private long starttimer;
        public KeyboardAndMouseGlobal(KeyboardAndMousePlugin plugin)
        {
            this.plugin = plugin;
            keyboard = new _Keyboard(this.plugin);
            mouse = new _Mouse(this.plugin);
        }
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
        public void sendBatch(string name, string section = null, int priority = 0)
        {
            name.LoadBatchFile(section, priority);
        }
        public void sendCommand(string cmde, int priority = 0)
        {
            cmde.AddNewCommand(priority);
        }
        public _Keyboard Keyboard => keyboard;
        public _Mouse Mouse => mouse;


        //----------------------- button/key actions ----------------------------------------------------
        public bool getClicked<T>(T code, bool dblclick = false) => plugin.IsClicked(Convert.ToInt32(code), dblclick);
        public bool getPressed<T>(T code) => plugin.IsPressed(Convert.ToInt32(code));
        public bool getReleased<T>(T code) => plugin.IsReleased(Convert.ToInt32(code));
        public bool getDown<T>(T code) => plugin.IsDown(Convert.ToInt32(code));
        //-----------------------------------------------------------------------------------------------
        public void keepDown<T>(bool trigger, T code, int duration) => plugin.KeepDown(trigger, Convert.ToInt32(code), duration);
        //----------------------- button/key Helddown ---------------------------------------------------
        public int getHeldDown<T>(T code, int nbvalue, int duration) => plugin.HeldDown(Convert.ToInt32(code), nbvalue, duration);
        public void getHeldDownStop<T>(T code) => plugin.HeldDownStop(Convert.ToInt32(code));
        //-----------------------------------------------------------------------------------------------
        //public bool getDown<T>(IList<T> codes)
        //{
        //    foreach (var code in codes)
        //        if (!plugin.IsDown(Convert.ToInt32(code))) return false;
        //    return true;
        //}
        public bool getUp<T>(T key) => plugin.IsUp(Convert.ToInt32(key));


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
        public void setPressed<T>(T code, int delay = -1) where T : struct => plugin.PressAndRelease(Convert.ToInt32(code), delay);
        // Key(s) to press one time only
        public void setPressed<T>(T key, bool state, int delay = -1) where T : struct => plugin.PressAndRelease((Convert.ToInt32(key)), state, delay);


        public void setPressed<T>(IList<T> codes, bool state, int delay = -1)
        {
            foreach (var code in codes)
                plugin.PressAndRelease(Convert.ToInt32(code), state, delay);
        }



        // list of Keys to press
        public void setPressed<T>(IList<T> codes, int delay = -1)
        {
            foreach (var code in codes)
                plugin.PressAndRelease(Convert.ToInt32(code), delay);
        }
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

        
    }

    public class _Keyboard
    {
        private readonly KeyboardAndMousePlugin plugin;
        public _Keyboard(KeyboardAndMousePlugin plugin)
        {
            this.plugin = plugin;
        }

        //----------------------- special key -----------------------------------------------------------
        public bool xShift => plugin.IsDown((int)Key.LeftShift) || plugin.IsDown((int)Key.RightShift);
        public bool xControl => plugin.IsDown((int)Key.LeftControl) || plugin.IsDown((int)Key.RightControl);
        public bool xAlt => plugin.IsDown((int)Key.LeftAlt) || plugin.IsDown((int)Key.RightAlt);
        public bool xWin => plugin.IsDown((int)Key.LeftWindowsKey) || plugin.IsDown((int)Key.RightWindowsKey);
        //------------------------------------------------------------------------------------------------

        public bool isScrollLockLedOn => plugin.isLedScrollLockOn;
        public bool isCapsLockLedOn => plugin.isLedCapsLockOn;
        public bool isNumLockLedOn => plugin.isLedNumLockOn;
        public int getNbrKeysDown => plugin.Keyspressed;
        public List<Key> getListOfKeysDown => plugin.ListKeyspressed;
        public string getBuffer() => buffer;
        public Key intTOkey(int key) => (Key)key;

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
        private readonly KeyboardAndMousePlugin plugin;
        public _Mouse(KeyboardAndMousePlugin plugin)
        {
            this.plugin = plugin;
        }

        public bool setPointerPrecision(int state = -1 /* 0 = disable, 1 = enable, -1 toogle */) => plugin.SetEnhancePointerPrecision(state);

        public int X => plugin.getmousePos.X;
        public int Y => plugin.getmousePos.Y;

        public void setXY(int x, int y) => plugin.setmousePos(x, y);

        private int wheelMax => KeyboardAndMousePlugin.WheelMax;

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
