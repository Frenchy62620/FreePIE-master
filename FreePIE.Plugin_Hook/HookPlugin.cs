using FreePIE.Core.Contracts;
using System;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Threading;
using System.Diagnostics;
using FreePIE.CommonStrategy;
using FreePIE.Plugin_Hook.ScriptAuto;
using static FreePIE.CommonTools.GlobalTools;
using System.Runtime.InteropServices;
using FreePIE.CommonEnum;

namespace FreePIE.Plugin_Hook
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

    [GlobalType(Type = typeof(HookGlobal))]
    public class HookPlugin : Plugin
    {
        private HashSet<int> extendedKeyMap = new HashSet<int>() { 121, 125, 144, 146, 149, 153, 156, 157, 160, 161, 162, 164, 174, 176, 178, 181, 183, 184, 197, 199, 200, 201, 203, 205, 207, 208, 209, 210, 211, 219, 220, 221, 222, 223, 227, 229, 230, 231, 232, 233, 234, 235, 236, 237 };

        private EventWaitHandle ewh;
        private MemoryMappedFile memory;
        public MemoryMappedViewAccessor accessor;

        public List<int> KeysPressed;
        private bool[] MyKeyDown = new bool[256];
        private int hookstatus;
        private ScriptHook SF;
        public SetPressedStrategy<int> setPressedStrategy;
        public GetPressedStrategy<int> getPressedStrategy;
        private bool Debug;

        public const int WheelMax = 120;
        public int Wheelturn;
        private int MouseX;
        private int MouseY;
        public int WheelValue { get; internal set; }
        public int[] Codes;
        public long[] Tempo;
        private MouseKeyIO.POINT point;
        public override object CreateGlobal()
        {
            return new HookGlobal(this);
        }

        public override string FriendlyName
        {
            get { return "hook"; }
        }

        public override Action Start()
        {
            setPressedStrategy = new SetPressedStrategy<int>(KeyOrButtonDown, KeyOrButtonUp);
            getPressedStrategy = new GetPressedStrategy<int>(IsDown);
            KeysPressed = new List<int>();
            ewh = new EventWaitHandle(false, EventResetMode.AutoReset, "ewhFreePIE");

            memory = MemoryMappedFile.CreateOrOpen("hookFreePIE", 68, MemoryMappedFileAccess.ReadWrite);
            accessor = memory.CreateViewAccessor();
            for (byte i = 0; i < 68; i++)
                accessor.Write(i, 0);
            if (!Debug)
                Process.Start("FreePIE.HookInput.exe", hookstatus.ToString());
            return null;
        }

        public override void Stop()
        {
            setHook((int)HOOK.STOP_PROCESS, true);

            accessor.Dispose();
            memory.Dispose();
            ewh.Dispose();
        }

        public override void DoBeforeNextExecute()
        {
            if (Wheelturn != 0)
            {
                setPressedStrategy.Add(Wheelturn < 0 ? (int)Mouse.WheelBwd : (int)Mouse.WheelFwd);
                Wheelturn -=  Math.Sign(Wheelturn);
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
            KeysPressed.Clear();
            for (int i = 1; i < 238; i++)
            {
                if (IsDown(i))
                    KeysPressed.Add(i);
            }

            if ((WheelValue = (IsDown((int)Mouse.WheelFwd) ? 1 : 0) + (IsDown((int)Mouse.WheelBwd) ? -1 : 0)) != 0)
                    setBit((int)(WheelValue < 0 ? Mouse.WheelBwd : Mouse.WheelFwd), false);

            if (cmd == 'H')
            {
                if (SF == null)
                    SF = new ScriptHook(this);

                SF.Hook();
            }
        }


        public override bool GetProperty(int index, IPluginProperty property)
        {
            if (index > 2)
                return false;

            if (index == 0)
            {
                property.Name = "HookKeyboard";
                property.Caption = "Hook Keyboard";
                property.DefaultValue = true;
                property.HelpText = "if true, give the possibility to hook keyboard";
            }
            else if (index == 1)
            {
                property.Name = "HookMouse";
                property.Caption = "Hook Mouse";
                property.DefaultValue = false;
                property.HelpText = "if true, give the possibility to hook mouse";
            }
            else // (index == 2)
            {
                property.Name = "Hookdebug";
                property.Caption = "Hook debug";
                property.DefaultValue = false;
                property.HelpText = "if true, avoid to launch hooker";
            }
            return true;
        }

        public override bool SetProperties(Dictionary<string, object> properties)
        {
            var k = (bool)properties["HookKeyboard"] ? 1 : 0;
            var m = (bool)properties["HookMouse"] ? 2 : 0;
            Debug = (bool)properties["Hookdebug"];
            hookstatus = k + m;
            return true;
        }

        public void setBit(int keycode, bool on)
        {
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
            var numIntinByte = (keycode / 32) * 4;
            var numBit = keycode % 32;
            return (accessor.ReadInt32(numIntinByte) & (1 << numBit)) != 0;  // bit on?
        }

        public bool IsDown(int keycode, bool on = false) => getBit(keycode) || MyKeyDown[keycode];
        public bool IsUp(int keycode) => !IsDown(keycode);
        public bool IsClicked(int code, bool doubleclicked) => doubleclicked ? getPressedStrategy.IsDoubleClicked(code) : getPressedStrategy.IsSingleClicked(code);

        public bool IsPressed(int code) => getPressedStrategy.IsPressed(code);
        public bool IsReleased(int code) => getPressedStrategy.IsReleased(code);

        public int HeldDown(int code, int nbvalue, int lapse) => getPressedStrategy.HeldDown(code, IsDown(code), nbvalue, lapse);
        public void HeldDownStop(int code) => getPressedStrategy.HeldDownStop(code);

        public bool IsEnterPressed => IsPressed((int)Key.NumberPadEnter) || IsPressed((int)Key.Return);

        public MouseKeyIO.POINT getmousePos
        {
            get
            {
                var xy = accessor.ReadInt32((int)Mouse.XY >> 3);
                point.X = xy & 0XFFFF;
                point.Y = xy >> 16;
                return point;
            }
        }

        private int CalculateAbsoluteCoordinateX(int x)
        {
            return (int)Math.Round((x * 65535.0f) / (MouseKeyIO.NativeMethods.GetSystemMetrics(MouseKeyIO.SystemMetric.SM_CXSCREEN) - 1),0, MidpointRounding.AwayFromZero);
        }

        private int CalculateAbsoluteCoordinateY(int y)
        {
            return (int)Math.Round((y * 65535.0f) / (MouseKeyIO.NativeMethods.GetSystemMetrics(MouseKeyIO.SystemMetric.SM_CYSCREEN) - 1),0, MidpointRounding.AwayFromZero);
        }
        public void setmousePos(int x, int y)
        {
            MouseX = CalculateAbsoluteCoordinateX(x);
            MouseY = CalculateAbsoluteCoordinateY(y);
            setPressedStrategy.Add((int)Mouse.XY);
        }

        public void KeyOrButtonDown(int code)
        {
            if (code <= (int)Key.LastKey)
                SetKeyPressed(code, true);
            else
            {
                switch (code)
                {
                    case (int)Mouse.XY:
                        setXY();
                        break;
                    case (int)Mouse.WheelBwd:
                    case (int)Mouse.WheelFwd:
                        setWheel(code);
                        break;
                    default:
                        SetButtonPressed(code, true);
                        break;
                }
            }
        }

        public void KeyOrButtonUp(int code)
        {
            switch (code)
            {
                case (int)Mouse.XY:
                case (int)Mouse.WheelBwd:
                case (int)Mouse.WheelFwd:
                    break;

                default:
                    if (code <= (int)Key.LastKey)
                        SetKeyPressed(code, false);
                    else
                        SetButtonPressed(code, false);
                    break;
            }
        }

        //public double DeltaX
        //{
        //    set { deltaXOut = deltaXOut + value; }
        //    get { return CurrentMouseState.X; }
        //}

        //public double DeltaY
        //{
        //    set { deltaYOut = deltaYOut + value; }
        //    get { return CurrentMouseState.Y; }
        //}


        private void setWheel(int button)
        {
            int mousedata = WheelMax * (button == (int)Mouse.WheelBwd ? -1 : 1);
            var input = new MouseKeyIO.INPUT[1];
            input[0].type = MouseKeyIO.INPUT_MOUSE;
            input[0].mi = MouseInput(0, 0, (uint)mousedata, 0, MouseKeyIO.MOUSEEVENTF_WHEEL);
            MouseKeyIO.NativeMethods.SendInput(1, input, Marshal.SizeOf(input[0].GetType()));
        }
        private void setXY()
        {
            var input = new MouseKeyIO.INPUT[1];
            input[0].type = MouseKeyIO.INPUT_MOUSE;
            input[0].mi = MouseInput(MouseX, MouseY, 0, 0, MouseKeyIO.MOUSEEVENTF_MOVE | MouseKeyIO.MOUSEEVENTF_ABSOLUTE);
            MouseKeyIO.NativeMethods.SendInput(1, input, Marshal.SizeOf(input[0].GetType()));
        }

        public void DragAndDrop(int x, int y)
        {
            var input = new MouseKeyIO.INPUT[3];
            input[0].type = MouseKeyIO.INPUT_MOUSE;
            input[0].mi = MouseInput(0, 0, 0, 0, MouseKeyIO.MOUSEEVENTF_LEFTDOWN);
            input[1].type = MouseKeyIO.INPUT_MOUSE;
            var X = CalculateAbsoluteCoordinateX(x);
            var Y = CalculateAbsoluteCoordinateY(y);
            input[1].mi = MouseInput(X, Y, 0, 0, MouseKeyIO.MOUSEEVENTF_MOVE | MouseKeyIO.MOUSEEVENTF_ABSOLUTE);
            input[2].type = MouseKeyIO.INPUT_MOUSE;
            input[2].mi = MouseInput(0, 0, 0, 0, MouseKeyIO.MOUSEEVENTF_LEFTUP);
            MouseKeyIO.NativeMethods.SendInput(3, input, Marshal.SizeOf(input[0].GetType()));
        }
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
        private MouseKeyIO.KEYBDINPUT KeyInput(ushort code, uint flag)
        {
            return new MouseKeyIO.KEYBDINPUT { wVk = 0, wScan = code, time = 0, dwExtraInfo = IntPtr.Zero, dwFlags = flag | MouseKeyIO.KEYEVENTF_SCANCODE };
        }

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


        public void setHook(int adr, bool on)
        {
            setBit(adr, on);
            ewh.Set();
        }
        public void PressAndRelease(int code) => setPressedStrategy.Add(code);
        //public void PressAndRelease(int code, int lapse) => setPressedStrategy.Add(code, lapse);
        public void PressAndRelease(int code, bool state) => setPressedStrategy.Add(code, state);
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
            {
                var numberpad = "NumberPadSlash,NumberPadStar,NumberPadPlus,NumberPadMinus,NumberPadEnter,NumberPadPeriod";
                key = key.Replace("$dcs", $"$alpha,$num,Insert,Delete,End,Home,{numberpad}");
            }

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


        // HookKey((int)ToEnum(key.ToString(), HKey.Unknown), toblock);


        public T ToEnum<T>(string value) where T : struct
        {
            return Enum.TryParse(value: value, ignoreCase: true, result: out T result) ? result : default(T);
        }


    }

    [Global(Name = "hook")]
    public class HookGlobal
    {
        private readonly HookPlugin plugin;

        public HookGlobal(HookPlugin plugin)
        {
            this.plugin = plugin;
            Keyboard = new _Keyboard(this.plugin);
            Mouse = new _Mouse(this.plugin);
        }

        public _Keyboard Keyboard { get; }
        public _Mouse Mouse { get; }

        public void setHOOK(HOOK type, bool on) => plugin.setHook((int)type, on);

        public void swallow(IList<string> codes)
        {
            foreach (var c in codes)
                plugin.swallow(c);
        }

        //----------------------- button/key actions ----------------------------------------------------
        public bool getClicked<T>(T code, bool dblclick = false) => plugin.IsClicked(Convert.ToInt32(code), dblclick);
        public bool getPressed<T>(T code) => plugin.IsPressed(Convert.ToInt32(code));
        public bool getReleased<T>(T code) => plugin.IsReleased(Convert.ToInt32(code));
        public bool getDown<T>(T code) => plugin.IsDown(Convert.ToInt32(code));
        //-----------------------------------------------------------------------------------------------

        //----------------------- button/key Helddown ---------------------------------------------------
        public int getHeldDown<T>(T code, int nbvalue, int duration) => plugin.HeldDown(Convert.ToInt32(code), nbvalue, duration);
        public void getHeldDownStop<T>(T code) => plugin.HeldDownStop(Convert.ToInt32(code));
        //-----------------------------------------------------------------------------------------------

        public void sendBatch(string name, string section = null, int priority = 0)
        {
            name.LoadBatchFile(section, priority);
        }
        public void sendCommand(string cmde, int priority = 0)
        {
            cmde.AddNewCommand(priority);
        }
        public string Buffer => buffer;
        public bool toBuffer
        {
            get => plugin.getBit((int)HOOK.BUFFERED);
            set => plugin.setBit((int)HOOK.BUFFERED, value);
        }

        public void setDown<T>(IList<T> codes)
        {
            foreach (var code in codes)
                plugin.KeyOrButtonDown(Convert.ToInt32(code));
        }
        public void setUp<T>(IList<T> codes)
        {
            foreach (var code in codes.Reverse())
                plugin.KeyOrButtonUp(Convert.ToInt32(code));
        }
        public void set<T>(IList<T> codes, bool down)
        {
           if (down)
                setDown(codes);
            else
                setUp(codes);
        }
        public void setPressed<T>(IList<T> codes)
        {
            foreach(var code in codes)
                plugin.PressAndRelease((Convert.ToInt32(code)));
        }
        public void setPressed<T>(T code) where T : struct
        {
            plugin.PressAndRelease((Convert.ToInt32(code)));
        }
        public void setPressedWithTempo<T>(IList<T>codes, int tempo)
        {
            if (plugin.Tempo != null) return;
            set(codes, true);

            plugin.Codes = codes.Select(c => Convert.ToInt32(c)).ToArray();
            plugin.Tempo = new long[2] { StartTimer(), tempo };
        }

        //public bool wheelUp
        //{
        //    get
        //    {               
        //        var b = plugin.IsDown((int)HMouse.WheelFwd);
        //        if (b) plugin.setFlag((int)HMouse.WheelFwd, false);
        //        return b;
        //    }
        //}

        //public bool wheelDown
        //{
        //    get
        //    {
        //        var b = plugin.IsDown((int)HMouse.WheelBwd);
        //        if (b) plugin.setFlag((int)HMouse.WheelFwd, false);
        //        return b;
        //    }
        //}
    }



    public class _Keyboard
    {
        private readonly HookPlugin plugin;
        public _Keyboard(HookPlugin plugin)
        {
            this.plugin = plugin;
        }

        //----------------------- special keys -----------------------------------------------------------
        public bool xShift => plugin.IsDown((int)Key.LeftShift) || plugin.IsDown((int)Key.RightShift);
        public bool xControl => plugin.IsDown((int)Key.LeftControl) || plugin.IsDown((int)Key.RightControl);
        public bool xAlt => plugin.IsDown((int)Key.LeftAlt) || plugin.IsDown((int)Key.RightAlt);
        public bool xWin => plugin.IsDown((int)Key.LeftWindowsKey) || plugin.IsDown((int)Key.RightWindowsKey);
        //-----------------------------------------------------------------------------------------------

    }

    public class _Mouse
    {
        private readonly HookPlugin plugin;
        public _Mouse(HookPlugin plugin)
        {
            this.plugin = plugin;
        }



        public int X => plugin.getmousePos.X;
        public int Y => plugin.getmousePos.Y;

        public void setXY(int x, int y) => plugin.setmousePos(x, y);

        public void dragAnddrop(int x, int y) => plugin.DragAndDrop(x, y);
        public int wheelMax => HookPlugin.WheelMax;

        //public double deltaX
        //{
        //    get { return plugin.DeltaX; }
        //    set { plugin.DeltaX = value; }
        //}
        //public double deltaY
        //{
        //    get { return plugin.DeltaY; }
        //    set { plugin.DeltaY = value; }
        //}
        //----------------------- Wheel Gestion -----------------------------------------------------------
        public void setWheel(int times) => plugin.Wheelturn = times;
        public bool wheelUp
        {
            get { return plugin.WheelValue == 1; }
            set { plugin.WheelValue = value ? 1 : 0; }
        }

        public bool wheelDown
        {
            get { return plugin.WheelValue == -1; }
            set { plugin.WheelValue = value ? -1 : 0; }
        }

        public int wheelvalue
        {
            get { return plugin.WheelValue; }
            set { plugin.WheelValue = value; }
        }
        //-------------------------------------------------------------------------------------------------

    }
}

