using FreePIE.Plugin_Warthog;
using System;
using static FreePIE.CommonTools.GlobalTools;

namespace FreePIE.Plugin_Warthogs.ScriptAuto
{
    public class ScriptWarthog
    {
        private readonly WarthogPlugin plugin;
        public ScriptWarthog(WarthogPlugin plugin)
        {
            this.plugin = plugin;
            //Gx.AddListOfFct(GetType());
        }

        public void Warthog()
        {
            //W led:led0,... +on +off +blink

            if (vr == null)
                RefreshParser("SET,TEST", "ON,FLASH", "LED");

            if (vr.LED == null)
                NextAction();


                foreach (string led in vr.LED)
                {
                    if (vr.SET)
                    {
                        if (vr.FLASH)
                            plugin.SetLedFlashing(int.Parse(led), vr.ON);
                        else
                            plugin.SetLed(int.Parse(led), vr.ON);
                    }
                    else
                    {
                        if (vr.FLASH)
                            if (!plugin.IsLedFlashing(int.Parse(led))) return;
                        else
                            if (plugin.IsLedOn(int.Parse(led)) != vr.ON) return;
                    }
                }

            NextAction();
        }
    }
}

