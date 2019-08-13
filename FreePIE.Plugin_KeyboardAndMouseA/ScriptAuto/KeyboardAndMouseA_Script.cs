using System;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using FreePIE.CommonEnum;
using static FreePIE.CommonTools.GlobalTools;

namespace FreePIE.Plugin_KeyboardAndMouseA.ScriptAuto
{
    public class ScriptKeyboardAndMouseA
    {
        //private readonly HashSet<int> autorisedKeys = new HashSet<int>()
        //{
        //    (int)Key.Q, (int)Key.W, (int)Key.E, (int)Key.R, (int)Key.T, (int)Key.Y, (int)Key.U, (int)Key.I, (int)Key.O, (int)Key.P,
        //    (int)Key.A, (int)Key.S, (int)Key.D, (int)Key.F, (int)Key.G, (int)Key.H, (int)Key.J, (int)Key.K, (int)Key.L, (int)Key.Semicolon,
        //    (int)Key.Z, (int)Key.X, (int)Key.C, (int)Key.V, (int)Key.B, (int)Key.N, (int)Key.Comma, (int)Key.Period,
        //    (int)Key.NumberPadStar, (int)Key.Space,
        //    (int)Key.NumberPad7, (int)Key.NumberPad8, (int)Key.NumberPad9, (int)Key.NumberPadMinus,
        //    (int)Key.NumberPad4, (int)Key.NumberPad5, (int)Key.NumberPad6, (int)Key.NumberPadPlus,
        //    (int)Key.NumberPad1, (int)Key.NumberPad2, (int)Key.NumberPad3, (int)Key.NumberPad0,
        //    (int)Key.NumberPadPeriod, (int)Key.NumberPadEnter, (int)Key.NumberPadComma, (int)Key.NumberPadSlash,
        //    (int)Key.Home, ( int)Key.End, (int)Key.Insert, (int)Key.Delete
        //};
        //private readonly HashSet<int> autorisedAlphaKeys = new HashSet<int>()
        //{
        //    (int)Key.Q, (int)Key.W, (int)Key.E, (int)Key.R, (int)Key.T, (int)Key.Y, (int)Key.U, (int)Key.I, (int)Key.O, (int)Key.P,
        //    (int)Key.A, (int)Key.S, (int)Key.D, (int)Key.F, (int)Key.G, (int)Key.H, (int)Key.J, (int)Key.K, (int)Key.L, (int)Key.Semicolon,
        //    (int)Key.Z, (int)Key.X, (int)Key.C, (int)Key.V, (int)Key.B, (int)Key.N
        //};
        private const string authorizedkeys = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789+-.;";
        private string [] Translate;
        private readonly KeyboardAndMousePluginA plugin;
        private readonly MemoryMappedViewAccessor accessor;


        private const int ADR_DATA = (int)HOOK.DATA >> 3;
        private const int NEW_DATA = 247;
        public ScriptKeyboardAndMouseA(KeyboardAndMousePluginA plugin)
        {
            this.plugin = plugin;
            this.accessor = plugin.accessor;
        }

        private bool HaveNewData() => accessor.ReadByte(ADR_DATA) != 0;
        private T ToEnum<T>(string value) where T : struct
        {
            //if (string.IsNullOrEmpty(value))
            //{
            //    return default(T);
            //}

            return Enum.TryParse(value: value, ignoreCase: true, result: out T result) ? result : default(T);
        }
        public void KeyboardAndMouseA()
        {
            if (vr is null)
            {
                RefreshParser("TEST,GET", "DOWN,UP,OFF,HOOK", "LAUNCH,T,HOOK,SWALLOW,XY,HPTR,CHOICE,INPUTS,TRANSLATE");

                if (vr.TRANSLATE != null || vr.LAUNCH != null)
                {
                    Translate = vr.TRANSLATE;

                    if (vr.LAUNCH != null)      //I LAUNCH:KEYBOARD or MOUSE or BOTH [T:time]
                    {
                        plugin.setHookProgram((int)ToEnum<HOOK>(vr.LAUNCH[0]));
                        if (vr.T != null)
                            $"T {vr.T[0]}".ReplaceCurrentCommand();
                        return;
                    }
                    NextAction();
                    return;
                }

                if (vr.GET)
                {
                    vr.validatekeydown = false;
                    vr.atleastonekeydown = false;
                }

                if (vr.INPUTS != null)
                    vr.INPUTS = Toint();
            }

            if (vr.TEST)
            {
                if (vr.XY != null)
                {
                    bool xy = false;
                    int l = vr.XY.Length;
                    if (l == 2)
                    {
                        // I [INPUTS:1] xy:x0,x1 -> wait x0 < x < x1
                        xy = Compare(plugin.getmousePos.X, "><", int.Parse(vr.XY[0]), int.Parse(vr.XY[1]));
                    }
                    else
                    {
                        if (l == 4)
                        {
                            if (string.IsNullOrEmpty(vr.XY[0]))
                            {
                                // I [INPUTS:1] xy:,,y0,y1 -> wait y0 < y < y1
                                xy = Compare(plugin.getmousePos.Y, "><", int.Parse(vr.XY[2]), int.Parse(vr.XY[3]));
                            }
                            else
                            {
                                // I [INPUTS:1] xy:x0,x1,y0,y1 -> wait x0 < x < x1 &&  y0 < y < y1
                                xy = Compare(plugin.getmousePos.X, "><", int.Parse(vr.XY[0]), int.Parse(vr.XY[1])) &&
                                     Compare(plugin.getmousePos.Y, "><", int.Parse(vr.XY[2]), int.Parse(vr.XY[3]));
                            }
                        }
                    }

                    if (!xy) return;

                    NextAction();   // xy is true go next action
                    return;
                }

                if (vr.DOWN)
                {
                    foreach (int ky in vr.INPUTS)
                    {
                        if (plugin.IsUp(ky, (bool)vr.HOOK))
                            return;
                    }
                    vr.DOWN = false;
                }
                if (vr.UP)
                {
                    Array.Reverse(vr.INPUTS);
                    foreach (int ky in vr.INPUTS)
                    {
                        if (plugin.IsDown(ky, (bool)vr.HOOK))
                            return;
                    }
                }

                NextAction();
                return;
            }

            if (vr.GET)
            {
                if (vr.validatekeydown && plugin.IsUp((int)Key.NumberPadEnter, true))
                {
                    NextAction();
                    return;
                }
                if (plugin.IsDown((int)Key.NumberPadEnter))
                {
                    if (vr.CHOICE != null)
                    {
                        if (buffer.Length != 1 || !char.IsDigit(buffer, 0) || int.Parse(vr.CHOICE[0]) < int.Parse(buffer))
                        {
                            buffer = "";
                            Beep(150, 600);
                            return;
                        }
                    }
                    vr.validatekeydown = true;
                    return;
                }

                if (plugin.KeysPressed.Count != 1)
                {
                    if (plugin.KeysPressed.Count == 0)
                        vr.atleastonekeydown = false;
                    return;
                }

                if (!vr.atleastonekeydown)
                {
                    vr.atleastonekeydown = true;

                    switch (plugin.KeysPressed[0])
                    {
                        case (int)Key.Insert:
                            bufferTosay = Translate == null ? "point virgule" : Translate[0];
                            buffer += ";";
                            break;
                        case (int)Key.NumberPadPeriod:
                            bufferTosay = Translate == null ? "point" : Translate[1];
                            buffer += ".";
                            break;
                        case (int)Key.NumberPadPlus:
                            bufferTosay = Translate == null ? "plus" : Translate[2];
                            buffer += "+";
                            break;
                        case (int)Key.NumberPadMinus:
                            bufferTosay = Translate == null ? "moins" : Translate[3];
                            buffer += "-";
                            break;
                        case (int)Key.Delete:           // correction
                            bufferTosay = Translate == null ? "correction" : Translate[4];
                            if (buffer.Length > 0)
                                buffer = buffer.Substring(0, buffer.Length - 1);
                            break;
                        case (int)Key.Home:             // raz
                            bufferTosay = Translate == null ? "tout effacer" : Translate[5];
                            buffer = "";
                            break;
                        case (int)Key.NumberPadStar:    // Read Buffer
                            bufferTosay = buffer;
                            break;

                        default:
                            if (HaveNewData())
                            {
                                bufferTosay = ((char)accessor.ReadByte(ADR_DATA)).ToString();
                                accessor.Write(ADR_DATA, (byte)0);
                                if (vr.CHOICE != null)
                                    buffer = bufferTosay;
                                else
                                    buffer += bufferTosay;
                            }
                            break;
                    }
                }
                return;
            }

            if (vr.HOOK != null)
                    plugin.setHook((int)plugin.ToEnum<HOOK>(vr.HOOK[0]), !vr.OFF);

            if (vr.SWALLOW != null)       //swalow:"+$alpha,$num$pad"
            {
                foreach (string sw in vr.SWALLOW)
                    plugin.swallow(sw);
                NextAction();
            }

            if (vr.HPTR != null)
                plugin.SetEnhancePointerPrecision(int.Parse(vr.HPTR[0]));

            if (vr.XY != null)
                plugin.setmousePos(int.Parse(vr.XY[0]), int.Parse(vr.XY[1]));

            if (vr.DOWN != vr.UP)
            {
                foreach (int ky in vr.INPUTS)
                {
                    if (vr.DOWN)
                        plugin.KeyOrButtonDown(ky);
                    else
                        plugin.KeyOrButtonUp(ky);
                }
                NextAction();
                return;
            }

            foreach (int ky in vr.INPUTS)
                plugin.PressAndRelease(ky);

            NextAction();


            int[] Toint()
            {
                List<int> num = new List<int>();
                foreach (string v in vr.INPUTS)
                {
                    int a = 0;
                    if (char.IsDigit(v, 0))
                        a = int.Parse(v);
                    else
                        a = (int)ToEnum<Key>(v) + (int)ToEnum<Mouse>(v);

                    if (a > 0) num.Add(a);
                }
                return num.ToArray();
            }
        }
    }
}


