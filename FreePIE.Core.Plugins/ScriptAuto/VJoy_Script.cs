using static FreePIE.CommonTools.GlobalTools;

namespace FreePIE.Core.Plugins.ScriptAuto
{
    public class ScriptVJoy
    {
        private readonly VJoyPlugin plugin;
        public ScriptVJoy(VJoyPlugin plugin)
        {
            this.plugin = plugin;
            //Gx.AddListOfFct(GetType());
        }

        public void Vjoy()       //speech.Say -> SY;hello S say:"..."
        {
            int numstick = cmdes[0][0][1] - 30;
            string[] attrib;
            bool down = ExtractAttribute("+down", out attrib);
            bool up = ExtractAttribute("+up", out attrib);

            ExtractAttribute("but:", out attrib);


            if (down != up)
            {
                foreach (var bt in attrib)
                    plugin.holders[numstick].SetButton(int.Parse(bt), down);

                NextAction();
                return;
            }

            foreach (var bt in attrib)
                plugin.holders[numstick].PressAndRelease(int.Parse(bt));

            NextAction();
        }

    }
}

