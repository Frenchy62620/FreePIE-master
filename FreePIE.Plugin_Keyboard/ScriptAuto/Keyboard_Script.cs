using Fclp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static FreePIE.CommonTools.GlobalTools;

namespace FreePIE.Plugin_Keyboard.ScriptAuto
{
    public class Test
    {
        public List<string> keys { get; set; }
        public bool down { get; set; }
    }
    public class Set:Test
    {

    }
    public class ScriptKeyboard
    {
        private Dictionary<int, int> Azerty = new Dictionary<int, int>()
        {
            { (int)Key.A, (int)Key.Q},
            { (int)Key.Q, (int)Key.A},
            { (int)Key.Z, (int)Key.W},
            { (int)Key.W, (int)Key.Z},
            { (int)Key.Semicolon, (int)Key.M},
            { (int)Key.Comma, (int)Key.Semicolon},
            { (int)Key.M, (int)Key.Comma}
        };
        private readonly KeyboardPlugin plugin;
        public ScriptKeyboard(KeyboardPlugin plugin, ref int[] scan, ref SharpDX.DirectInput.KeyboardState keystate)
        {
            this.plugin = plugin;
            this.scan = scan;
            dico = new Dictionary<int, bool>();
            this.keystate = keystate;
        }
        private Dictionary<int, bool> dico;
        private SharpDX.DirectInput.Key[] ks;
        private int[] scan;
        SharpDX.DirectInput.KeyboardState keystate;

        private bool isKeyDown(SharpDX.DirectInput.Key key)
        {
            return keystate.IsPressed(key);
            //return plugin.KeyState.IsPressed((SharpDX.DirectInput.Key)Enum.Parse(typeof(SharpDX.DirectInput.Key), key, true));
        }
        //private int GetValueOf(string enumName, string enumConst)
        //{
        //    Type enumType = Type.GetType(enumName);
        //    if (enumType == null)
        //    {
        //        throw new ArgumentException("Specified enum type could not be found", "enumName");
        //    }

        //    object value = Enum.Parse(enumType, enumConst);
        //    return Convert.ToInt32(value);
        //}

        //public void K()       //KKx;                wait key from keyboard
        //{
        //    var flagazerty = (Gx.subcmd2 & 1) != 0;     // azerty                               1
        //    var flagsemicolon = (Gx.subcmd2 & 2) != 0;  //rajoute un ; en fin de chaine         2
        //    var flagalpha = (Gx.subcmd2 & 4) != 0;      // utilise [A-Z]    en plus des nombres 4

        //    for (int k = (int)Key.A; k <= (int)Key.Semicolon; k++)
        //    {
        //        if (k == 36)
        //            k = (int)Key.Comma;
        //        else if (k == 52)
        //            k = (int)Key.NumberPad0;
        //        else if (k == 107)
        //            k = (int)Key.Semicolon;

        //        if (flagalpha && ((k >= (int)Key.A && k <= (int)Key.Z) || k == (int)Key.Comma || k == (int)Key.Semicolon))
        //        {
        //            var x = azerty.ContainsKey(k) ? azerty[k] : k;
        //            var y = flagazerty ? x : k;

        //            if (plugin.IsPressed(k))
        //            {
        //                switch ((Key)y)
        //                {
        //                    case Key.Comma:   //
        //                        //true.Beep(1000, 600);
        //                        Gx.keystosay = flagazerty ? "virgule" : "comma";
        //                        Gx.keystyped += ",";
        //                        return;
        //                    case Key.Semicolon:   //
        //                        //true.Beep(1000, 600);
        //                        Gx.keystyped += ";";
        //                        return;
        //                    default:
        //                        Gx.keystosay = ((char)(y + 55)).ToString();
        //                        Gx.keystyped += Gx.keystosay;
        //                        return;
        //                }
        //            }
        //        }

        //        if ((k >= (int)Key.NumberPad0 && k <= (int)Key.NumberPadStar)
        //            || k == (int)Key.Delete
        //            || k == (int)Key.End)
        //            if (plugin.IsPressed(k))
        //            {
        //                switch ((Key)k)
        //                {
        //                    case Key.NumberPadEnter:   //numpad Enter = validate
        //                        //BeepPlugin.BackgroundBeep.Beep(1000, 600);
        //                        //true.PlaySound(1000, 600);
        //                        if (flagsemicolon) Gx.keystyped += ';';
        //                        NextAction();
        //                        return;
        //                    case Key.NumberPadMinus:   //numpad - = signe * (code ascii 45)
        //                                               //true.PlaySound(600, 1000);
        //                        Gx.keystosay = flagazerty ? "moins" : "minus";
        //                        Gx.keystyped += "-";
        //                        return;
        //                    case Key.NumberPadPlus:   //numpad + = ; (code ascii 59)
        //                        //true.Beep(300, 300);
        //                        Gx.keystyped += ";";
        //                        return;
        //                    case Key.NumberPadPeriod:   //numpad . = ; (code ascii 59)
        //                        Gx.keystosay = flagazerty ? "point" : "dot";
        //                        Gx.keystyped += ".";
        //                        return;
        //                    case Key.Delete:   //delete = correction
        //                        Gx.keystosay = "correction";
        //                        if (Gx.keystyped.Length > 0)
        //                            Gx.keystyped = Gx.keystyped.Substring(0, Gx.keystyped.Length - 1);
        //                        return;
        //                    case Key.End:   //End = raz
        //                                    // true.PlaySound(100, 600);
        //                        Gx.keystosay = flagazerty ? "effacer" : "clear";
        //                        Gx.keystyped = "";
        //                        return;
        //                    case Key.NumberPadStar:   //numpad * = *
        //                        Gx.keystosay = "*";
        //                        Gx.keystyped += "*";
        //                        return;
        //                    case Key.NumberPadSlash:   //numpad / = sayall
        //                        Gx.keystosay = Gx.keystyped;
        //                        return;
        //                    default:
        //                        Gx.keystosay = ((char)(k - 41)).ToString();
        //                        Gx.keystyped += Gx.keystosay;
        //                        return;
        //                }
        //            }
        //    }
        //}

        public void Keyboard()
        {
 
            //  1   1   1   1   1    1   1    1    1
            // cmd,act,adr,rge:,wat,ope,+on,+off,+tst
            // var list = "key:,act:,adr:rge:,+act:,ope:,+down,+up.Split(',');
            string[] attrib;
            bool azerty = ExtractAttribute("+azerty", out attrib);
            if (!ExtractAttribute("+keepbuf", out attrib))
                buffer = "";
            if (ExtractAttribute("+wkp", out attrib))       //K +wkp keys:Lshift,11 => 
            {
                if (plugin.IsDown((int)Key.NumberPadEnter))
                {
                    NextAction();
                    return;
                }

                if (ExtractAttribute("key:", out attrib))
                {
                    //ks = attrib.Select(key => (SharpDX.DirectInput.Key)Enum.Parse(typeof(SharpDX.DirectInput.Key), key, true)).ToArray();

                    ks = attrib.Select(key => (SharpDX.DirectInput.Key)scan[(int)Enum.Parse(typeof(Key), key, true)]).ToArray();
                    int index;
                    for (index = 0; index < cmdes[0].Length; index++)
                    {
                        if (cmdes[0][index].Contains("key:"))
                            break;
                    }

                    StringBuilder sb = new StringBuilder("K +down ", capacity: 64);
                    sb.Append(cmdes[0][index]);
                    sb.Append("!K +wkp!K +up +rev "); sb.Append(cmdes[0][index]);
                    sb.Append("!K +down "); sb.Append(cmdes[0][index]);
                    //sb.ToString().DecodelineOfCommand(section: null, priority: 1);
                    return;
                }

                //var result = keystate.PressedKeys.Except(ks).Select(t => (int)t).ToArray();
                var result = keystate.PressedKeys.Except(ks).Select(t => (int)Enum.Parse(typeof(Key), t.ToString(),true)).ToArray();
                if (result.Count() == 0) return;


                if (((result[0] >= (int)Key.A && result[0] <= (int)Key.Z) || result[0] == (int)Key.Semicolon) && plugin.IsDown(result[0]))
                {
                    string s;
                    if (azerty && Azerty.ContainsKey(result[0]))
                        s = Azerty[result[0]].ToString();
                    else
                        s = result[0].ToString();

                    buffer += s;
                    bufferTosay = s;
                    return;
                }

                if (result[0] >= (int)Key.NumberPad0 && result[0] <= (int)Key.NumberPad9)
                {
                    var s = result[0].ToString().Last().ToString();
                    buffer += s;
                    bufferTosay = s;
                    return;
                }

                if (result[0] == (int)Key.Comma)
                {
                    buffer += ",";
                    bufferTosay = azerty ? "virgule" : "comma";
                }

                return;
            }

            bool down = ExtractAttribute("+down", out attrib);
            bool up = ExtractAttribute("+up", out attrib) && !down;

            bool act = ExtractAttribute("+act", out attrib);
            bool rev = ExtractAttribute("+rev", out attrib);

            // act only with down,up alone , test status in other case defaut +act = +down +up (SetPressed)
            ExtractAttribute("key:", out attrib);

            if (act)
            {
                if (down != up)
                {
                    if (up && rev) attrib = attrib.Reverse<string>().ToArray();
                    foreach (var ky in attrib)
                    {
                        if (down)
                        {
                            plugin.KeyDown(int.Parse(ky));
                            continue;
                        }
                        plugin.KeyUp(int.Parse(ky));
                    }
                    NextAction();
                    return;
                }

                foreach (var ky in attrib)
                    plugin.PressAndRelease(int.Parse(ky));

                NextAction();
                return;
            }

            if (down)
            {
                foreach (var ki in attrib)
                {
                    if (plugin.IsUp(Convert.ToInt32(ki)))
                        return;
                }
                NextAction();
            }

            if (up)
            {
                foreach (var ki in attrib)
                {
                    if (plugin.IsDown(Convert.ToInt32(ki)))
                        return;
                }
                NextAction();
            }

            var k = int.Parse(attrib[0]);
            bool rel = ExtractAttribute("+rel", out attrib);

            if (rel)
            {
                if (plugin.IsReleased(k))
                    NextAction();
                return;
            }
            if (plugin.IsPressed(k))
                NextAction();
        }

        private void Pressed(Set obj)
        {
            throw new NotImplementedException();
        }

        private void TestIfPressed(Test arg)
        {
            arg.down = false;
        }
    }
}


