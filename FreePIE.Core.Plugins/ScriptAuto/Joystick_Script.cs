using static FreePIE.CommonTools.GlobalTools;

namespace FreePIE.Core.Plugins.ScriptAuto
{
    public class ScriptJoystick
    {
        private readonly JoystickPlugin plugin;
        public ScriptJoystick(JoystickPlugin plugin) => this.plugin = plugin;
        public void Joystick()
        {
            int numstick = cmdes[0][0][1] - '0';
            if (vr == null || (int)vr.NUMSTICK != numstick)
            {
                RefreshParser("", "DOWN,UP", "AXE,TEST,BUTTONS");
                vr.NUMSTICK = numstick;
            }

            if (vr.DOWN)
            {
                foreach (string bt in vr.BUTTONS)
                {
                    if (!plugin.devices[numstick].IsDown(int.Parse(bt)))
                        return;
                }
                vr.DOWN = false;
            }
            if (vr.UP)
            {
                foreach (string bt in vr.BUTTONS)
                {
                    if (plugin.devices[numstick].IsDown(int.Parse(bt)))
                        return;
                }
            }

            if (vr.AXE != null && vr.TEST != null)
            {
                int numAxis = int.Parse(vr.AXE[0]);
                int valueAxis = 0;
                switch (numAxis)
                {
                    case 0:
                        valueAxis = plugin.devices[numstick].State.X;
                        break;
                    case 1:
                        valueAxis = plugin.devices[numstick].State.Y;
                        break;
                    case 2:
                        valueAxis = plugin.devices[numstick].State.Z;
                        break;
                    case 3:
                        valueAxis = plugin.devices[numstick].State.RotationX;
                        break;
                    case 4:
                        valueAxis = plugin.devices[numstick].State.RotationY;
                        break;
                    case 5:
                        valueAxis = plugin.devices[numstick].State.RotationZ;
                        break;
                    case 6:
                        valueAxis = plugin.devices[numstick].State.Sliders[0];
                        break;
                    case 7:
                        valueAxis = plugin.devices[numstick].State.Sliders[1];
                        break;
                    default:
                        NextAction();
                        return;
                }

                if (!Compare(valueAxis, (string)vr.TEST[0], int.Parse(vr.TEST[1]), ((string[])(vr.TEST)).Length > 2 ? int.Parse(vr.TEST[2]) : 0))
                    return;
            }

            NextAction();
        }
    }
}


