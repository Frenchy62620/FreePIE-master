using System;
using System.Linq;
using System.Text;
using static FreePIE.CommonTools.GlobalTools;

namespace FreePIE.Plugin_DCSPlugin.ScriptAuto
{
    public class ScriptDCS
    {
        private readonly DCSPlugin plugin;
        int default_tempo = 300;

        public ScriptDCS(DCSPlugin plugin)
        {
            this.plugin = plugin;
        }

        public void DCS()
        {
            if (vr is null)
            {
                RefreshParser("BUFFER", "ON,OFF", "T,RANGE,LBL,TEST,ADR,CMD,ROT,DEFAULT_T");
            }

            if (vr.ADR != null)
            {
                vr.ADR = ((string[])vr.ADR).Select(v => v = HexToDec(v)).ToArray();
            }

            // Default tempo to modify
            if (vr.DEFAULT_T != null)
                default_tempo = int.Parse(vr.DEFAULT_T[0]);

            // command alone
            if (vr.ROT == null && vr.TEST == null && !vr.ON && !vr.OFF && ((string[])vr.CMD).Length == 1)
            {
                plugin.SendDCSCommand(vr.CMD[0]);
                NextAction();
                return;
            }

            // data to concatenate into buffer
            if (vr.BUFFER && vr.ADR != null && vr.TEST == null)
            {
                var len = ((string[])vr.ADR).Length;
                if (len == 2)
                    buffer += plugin.GetData(uint.Parse(vr.ADR[0]), int.Parse(vr.ADR[1]));
                else if (len == 3)
                    buffer += plugin.GetData(uint.Parse(vr.ADR[0]), int.Parse(vr.ADR[1]), int.Parse(vr.ADR[2]));

                NextAction();
                return;
            }


            StringBuilder sb = new StringBuilder(128);
            string wait = vr.T != null ? $"T {vr.T[0]}" : $"T {default_tempo}";

            if (vr.TEST != null)
            {
                string operande = vr.TEST[0];
                bool result;
                if (operande.StartsWith("@"))   // string test
                {
                    //COND:@I,pos.lg,string   COND:@C,string   COND:@L,>=,string_int ADR: or buffer

                    string val0 = vr.ADR == null ? buffer : plugin.GetData(uint.Parse(vr.ADR[0]), int.Parse(vr.ADR[1]));
                    result = Compare(val0, operande, vr.TEST[1], ((string[])vr.TEST).Length > 2 ? vr.TEST[2] : null);
                }
                else                            // numeric test
                {
                    //COND:==,string_int   COND:><,string_int1,string_int2
                    int val0 = vr.ADR == null ? int.Parse(buffer) : plugin.GetData(uint.Parse(vr.ADR[0]), int.Parse(vr.ADR[1]), int.Parse(vr.ADR[2]));
                    result = Compare(val0, operande, int.Parse(vr.TEST[1]), ((string[])vr.TEST).Length > 2 ? int.Parse(vr.TEST[2]) : 0);
                }


                //if (vr.CMD != null)
                //{
                //    if (!result)
                //    {
                //        sb.Append($"D CMD:\"{vr.CMD[0]}\"!{wait}!");
                //    }
                //}

                GoToLblOrNxtAction(result);
                return;
            }



            if (vr.ROT != null)      //rot:4,0.2
            {
                int len = int.Parse(((string)vr.ROT[0]));
                if (string.IsNullOrWhiteSpace(buffer) || buffer.Length < len - 1)
                {
                    NextAction();
                    return;
                }

                if (buffer.Length < len)
                    buffer = buffer.PadLeft(len);

                int pos = int.Parse(((string)vr.ROT[1]).Split('.')[0]);
                int lg = int.Parse(((string)vr.ROT[1]).Split('.')[1]);
                int newvalue = 0;

                string[] rge = vr.RANGE;
                if (vr.ADR == null)
                {
                    if (rge[0].Contains('-'))
                    {  
                        var bornes = rge[0].Split('-').Select(c => int.Parse(c)).ToArray();
                        newvalue = int.Parse(buffer.Substring(pos, lg));
                        if (newvalue >= bornes[0] && newvalue <= bornes[1])
                            newvalue -= bornes[0];
                        else
                        {
                            NextAction();
                            return;
                        }
                    }
                    else
                    {
                        for (int i = 0; i < rge.Length; i++)
                        {
                            if (buffer.Substring(pos, lg).Equals(rge[i], StringComparison.CurrentCultureIgnoreCase))
                            {
                                newvalue = i;
                                break;
                            }
                        }
                    }
                    sb.Append($"D CMD:\"{vr.CMD[0]} {newvalue}\"!{wait}!");
                }
                else        //we have ADR
                {
                    int diff = 0;
                    int nowpos = plugin.GetData(uint.Parse(vr.ADR[0]), int.Parse(vr.ADR[1]), int.Parse(vr.ADR[2]));
                    if (rge[0].Contains('-'))
                    {
                        var bornes = rge[0].Split('-').Select(c => int.Parse(c)).ToArray();
                        newvalue = int.Parse(buffer.Substring(pos, lg));
                        if (newvalue >= bornes[0] && newvalue <= bornes[1])
                            newvalue -= bornes[0];
                        else
                            newvalue = nowpos;

                        diff = newvalue - nowpos;
                    }
                    else        // not rge with -
                    {
                        for (newvalue = 0; newvalue < rge.Length; newvalue++)
                            if (buffer.Substring(pos, lg).Equals(rge[newvalue], StringComparison.CurrentCultureIgnoreCase))
                            {
                                diff = newvalue - nowpos;
                                break;
                            }
                    }

                    if (diff == 0)
                    {
                        NextAction();
                        return;
                    }

                    var dec = diff < 0 ? "DEC" : "INC";
                    for (int i = 0; i < Math.Abs(diff); i++)
                        sb.Append($"D CMD:\"{vr.CMD[0]} {dec}\"!{wait}!");
                }
            }
            else            // no ROT
            {
                if (vr.CMD == null) // JUST D DEFAULT_TEMPO:
                {
                    NextAction();
                    return;
                }
                foreach (var cm in vr.CMD)
                {
                    if (vr.ON && vr.OFF)
                        sb.Append($"D CMD:\"{cm} 1\"!{wait}!D CMD:\"{cm} 0\"!T {default_tempo}!");
                    else if (vr.ON || vr.OFF)
                        sb.Append($"D CMD:\"{cm} {(vr.ON ? 1 : 0)}\"!{wait}!");
                    else
                        sb.Append($"D CMD:\"{cm}\"!{wait}!");
                }
            }

            sb.Length--;
            sb.ToString().ReplaceCurrentCommand();
            return;
        }

        private string HexToDec(string v)
        {
            if (v.ToUpper().StartsWith("0X"))
                return Convert.ToInt32(v, 16).ToString();
            else
                return v;
        }
    }
}

