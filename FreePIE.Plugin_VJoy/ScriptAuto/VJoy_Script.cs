using System;
using static FreePIE.CommonTools.GlobalTools;

namespace FreePIE.Plugin_VJoy.ScriptAuto
{
    public class ScriptVJoy
    {
        private readonly VJoyPlugin plugin;
        public ScriptVJoy(VJoyPlugin plugin)
        {
            this.plugin = plugin;
        }

        public void Vjoy()
        {
            int numvjoy = cmdes[0][0][1] - '0';

            if (vr == null || (int)vr.NUMSTICK != numvjoy)
            {
                RefreshParser("", "DOWN,UP", "AXE,BUTTONS");
                vr.NUMSTICK = numvjoy;
            }
            if (vr.AXE != null && ((string[])vr.AXE).Length > 1)
            {
                if (Enum.TryParse("HID_USAGE_" + vr.AXE[0], true, out HID_USAGES usage))
                    plugin.holders[numvjoy].SetAxis((int)vr.AXE[1], usage);
            }

            if (vr.BUTTONS != null)
            {

                if (vr.DOWN != vr.UP)
                {
                    foreach (string bt in vr.BUTTONS)
                        plugin.holders[numvjoy].SetButton(int.Parse(bt), (bool)vr.DOWN);

                    NextAction();
                    return;
                }

                if (vr.T != null)
                {
                    string[] buttons = string.Join(",", vr.BUTTONS);
                    string newcmd = $"V{numvjoy} BUTTONS:{buttons} +DOWN!T {vr.T[0]}!V{numvjoy} BUTTONS:{buttons} +UP";
                    newcmd.ReplaceCurrentCommand();
                    return;
                }

                foreach (string bt in vr.BUTTONS)
                    plugin.holders[numvjoy].PressAndRelease(int.Parse(bt));
            }
            NextAction();
        }
    }
}

