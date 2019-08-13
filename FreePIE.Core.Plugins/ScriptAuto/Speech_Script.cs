using static FreePIE.CommonTools.GlobalTools;

namespace FreePIE.Core.Plugins.ScriptAuto
{
    public class ScriptSpeech
    {
        private readonly SpeechPlugin plugin;
        public ScriptSpeech(SpeechPlugin plugin)
        {
            this.plugin = plugin;
        }

        public void Speech() //speech.Say -> SY;hello S say:"..."
        {
            // say:,said:,reco:,lbl:,+idle,+end

            if (vr == null)
            {
                RefreshParser("", "IDLE,END,NOIDLE,NOEND", "SAY,SAID,RECO");
                if (vr.SAY != null && vr.RECO == null)
                {
                    vr.IDLE = !vr.NOIDLE;
                    vr.END = !vr.NOEND;
                }
            }


            if (vr.SAID !=  null)
            {
                float conf = vr.CONF[0] == null ? 0.9f : int.Parse(vr.CONF[0]) / 100.0f;
                if (plugin.Said(vr.SAID[0], conf))
                    NextAction();
                return;
            }


            if (vr.SAY != null)
            {
                string say = vr.SAY[0];
                if (vr.IDLE || vr.END)
                {
                    $"{(vr.IDLE ? "S +IDLE!" : "")}S SAY:\"{say}\" RECO:2{(vr.END ? "!S +IDLE" : "")}".ReplaceCurrentCommand();
                    return;
                }
                plugin.Say(say);
                NextAction();
                return;
            }

            if (vr.IDLE && !plugin.Speaking) NextAction();
        }
    }
}


