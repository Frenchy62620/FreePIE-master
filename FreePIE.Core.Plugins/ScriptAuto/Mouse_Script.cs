using static FreePIE.CommonTools.GlobalTools;

namespace FreePIE.Core.Plugins.ScriptAuto
{
    public class ScriptMouse
    {
        private readonly MousePlugin plugin;
        public ScriptMouse(MousePlugin plugin)
        {
            this.plugin = plugin;
            //Gx.AddListOfFct(GetType()); 
        }

        public void Mouse()
        {
            string[] attrib;

            if (ExtractAttribute("enhp:", out attrib))
                plugin.SetEnhancePointerPrecision(int.Parse(attrib[0]));


            bool down = ExtractAttribute("+down", out attrib);
            bool up = ExtractAttribute("+up", out attrib);
            bool rel = ExtractAttribute("+rel", out attrib);
            bool act = ExtractAttribute("+act", out attrib);

            // act only with down,up alone , test status in other case defaut +act = +down +up (SetPressed)

            bool but =ExtractAttribute("but:", out attrib);

            // =============== BEG ACT =============================
            if (act)
            {
                if (ExtractAttribute("xy:", out attrib))    // M xy:x,y
                {
                    plugin.setmouse(int.Parse(attrib[0]), int.Parse(attrib[1]));
                    NextAction();
                    return;
                }

                if (down != up)                             // M but:0,1,.. (+down or +up)
                {
                    foreach (var bt in attrib)
                        plugin.SetButtonPressed(int.Parse(bt), down);

                    NextAction();
                    return;
                }

                foreach (var bt in attrib)                  // M but:0,1,.. (+down +up or nothing)
                    plugin.PressAndRelease(int.Parse(bt));

                NextAction();
                return;
            }
            // =============== END ACT =============================

            var b = int.Parse(attrib[0]);
            var xy = ExtractAttribute("xy:", out attrib);
            // =============== WAIT XY AND BUT =============================
            if (xy)
            {
                var _xy = attrib;
                var l = _xy.Length;
                if (l == 2)
                {
                    // M [but:1] xy:x0,x1 -> wait x0 < x < x1
                    xy = Compare(plugin.getmouse().X, "><", int.Parse(_xy[0]), int.Parse(_xy[1]));
                }
                else
                {
                    if (l == 4)
                    {
                        if (string.IsNullOrEmpty(_xy[0]))
                        {
                            // M [but:1] xy:,,y0,y1 -> wait y0 < y < y1
                            xy = Compare(plugin.getmouse().Y, "><", int.Parse(_xy[2]), int.Parse(_xy[3]));
                        }
                        else
                        {
                            // M [but:1] xy:x0,x1,y0,y1 -> wait x0 < x < x1 &&  y0 < y < y1
                            xy = Compare(plugin.getmouse().X, "><", int.Parse(_xy[0]), int.Parse(_xy[1])) &&
                               Compare(plugin.getmouse().Y, "><", int.Parse(_xy[2]), int.Parse(_xy[3]));
                        }
                    }
                }

                if (!xy) return;

                if (but)   // button test with XY test?
                {
                    if (plugin.IsDown(b))
                        NextAction();
                    return;
                }

                NextAction();   // xy is true go next action
            }
            // =============== END  XY AND BUT =============================

            // =============== WAIT release or pressed BUT =================
            if (rel)
            {
                if (plugin.IsReleased(b))
                    NextAction();
                return;
            }

            if (plugin.IsPressed(b))    // default test: pressed?
                NextAction();
        }

    }
}

