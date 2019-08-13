using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Dynamic;

namespace FreePIE.CommonTools
{
    public static class GlobalTools
    {
        // ---------------- folders for FreePie
        private static readonly string applicationDataSubPath = @"FreePie\files_freepie\";
        private static readonly string applicationDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), applicationDataSubPath);

        // Timer gestion
        private static Stopwatch globaltimer = new Stopwatch();
        private static int Ctime;
        public static long timer;
        public static int endtime;

        public static List<string[]> cmdes;

        public static int lapse_singleclick;
        public static char cmd;
        private static int subcmd2;
        private static bool debug = false;
        public static string buffer;
        public static string bufferTosay;
        public static dynamic vr;


        static GlobalTools()
        {
            vr = null;
            lapse_singleclick = 300;
            bufferTosay = buffer = "";
            cmd = '\0';
            Ctime = 0;
            timer = -1;
        }

        //public static T ToEnum<T>(this string value, T defaultValue) where T : struct
        //{
        //    if (string.IsNullOrEmpty(value))
        //    {
        //        return defaultValue;
        //    }

        //    return Enum.TryParse<T>(value, true, out T result) ? result : defaultValue;
        //}





        // --------------------- build the right folder ---------------------
        public static string FreePiePath(this string file)
        {
            return Environment.CurrentDirectory.StartsWith(@"C:\Program Files (x86)\FreePIE") ? applicationDataPath : $@"files_freepie\{file}";
            //return string.Format("{0}\\{1}\\{2}", portable.IsPortable ? portablepaths.Data : uacPaths.Data, "files_freepie", file);
        }

        // --------------------- Extension Global Timer -----------------------------------
        public static long StartTimer()
        {
            if (Ctime++ == 0)
                globaltimer.Restart();
            return globaltimer.ElapsedMilliseconds;
        }
        public static long ReStartTimer()
        {
            return globaltimer.ElapsedMilliseconds;
        }
        public static long StopTimer()
        {
            if (--Ctime <= 0)
            {
                globaltimer.Stop();
                Ctime = 0;
            }
            return -1;
        }
        public static long GetLapse(this long time)
        {
            if (time < 0)
                return -1;
            return globaltimer.ElapsedMilliseconds - time;
        }
        public static void CheckScriptTimer()
        {
            if (timer >= 0 && timer.GetLapse() >= endtime)
            {
                timer = StopTimer();
                NextAction();
            }
        }
        // ------ END ---------- Extension Global Timer -----------------------------------


        public static void NextAction(bool deletefirstcmde = true )
        {
            RemoveAllProperties();
            cmd = '\0';
            if (deletefirstcmde && cmdes.Any()) cmdes.RemoveAt(0);
            LoadNewCommand();
        }
        public static void AddNewCommand(this string command, int priority = 0)
        {
            subcmd2 = 1;
            if (priority == 0)
            {
                cmdes?.Clear();
                cmd = '\0';
            }
            AddOrInsertCommand(command, priority);
            LoadNewCommand();
        }

        public static void ReplaceCurrentCommand(this string command)
        {
            subcmd2 = 1;
            AddOrInsertCommand(command, priority: 1);
            NextAction();
        }

        public static void LoadBatchFile(this string command, string section = null, int priority = 0)
        {
            if (priority == 0)           //erase all previous cmdes
                cmdes?.Clear();

            using (StreamReader file = new StreamReader((@"command\" + command).FreePiePath(), System.Text.Encoding.Default))
            {
                StringBuilder linesb = new StringBuilder(64);
                string line;
                bool startsection = !String.IsNullOrWhiteSpace(section);
                bool endsection = startsection;
                if (startsection)
                    section = "[" + section + "]";

                while ((line = file.ReadLine()) != null)
                {
                    if (line.Contains("END_SCRIPT")) break;


                    if (startsection && (!line.Contains(section) || line.IndexOf('#') != 0)) continue;
                    if (endsection && !startsection && line.Contains(section) && line.IndexOf('#') == 0) break;
                    startsection = false

                    if (line.IndexOf('#') >= 0) line = Regex.Replace(line, @"[\t ]*#.+$", "");
                    //line = Regex.Replace(line, @"[\t ]+$", "");
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    linesb.Clear().Append(line);
                    AddOrInsertCommand(linesb.ToString(), priority);
                }
            }
            if (priority == 2 && cmd != '\0')
                return;
            NextAction(deletefirstcmde: priority == 1);
        }

        private static void LoadNewCommand()
        {
            if (cmdes == null || cmdes.Count == 0 || cmd != '\0') return;

            // gestion du {0} dans un string
            for(int i = 1; i < cmdes[0].Length; i++)
            {
                if (cmdes[0][i].Contains("{"))
                    cmdes[0][i] = string.Format(cmdes[0][i], buffer.Split(';'));
            }

            cmd = cmdes[0][0][0];

            if (debug) Console.WriteLine("cde: " + string.Join(" ", cmdes[0]));

            //Testspecialfunctions();
            switch (cmd)
            {
                case '*':
                case '0':    // 0 = nop
                    NextAction();
                    break;
                case 'T':   // T 3000
                    endtime = int.Parse(cmdes[0][1]);
                    timer = StartTimer();
                    break;
                case 'G':   // Goto etiq -> G LBL:
                    int result = 0;
                    string etiq = cmdes[0][1];
                    if (etiq[0] != '*' || etiq.Equals("*NEXT") || (result = cmdes.FindIndex(startIndex: 1, match: label => label[0].Equals(etiq))) < 0)
                    {
                        if (etiq.Equals("*END_OF_SCRIPT"))
                            cmdes.Clear();
                        NextAction();
                        return;
                    }
                    cmdes.RemoveRange(0, result - 1);
                    NextAction();
                    break;
                case 'L':   // L FILE:nameoffile[,_nameofSECTION]
                    RefreshParser("", "", "FILE");
                    string[] FILE = vr.FILE;
                    subcmd2 = 1;
                    FILE[0].LoadBatchFile(section: FILE.Length == 2 ? FILE[1] : null, priority: 1);
                    break;
                case '%':   // Clear Buffer or/and remove cmdes or put in buffer
                    RefreshParser("REPLACE", "DEBUG_ON,DEBUG_OFF,CLR_BUF", "DEL_CMD,BUFFER");

                    if (vr.REPLACE)
                    {
                        if (vr.BUFFER != null)
                        {
                            string repl = ((string[])vr.BUFFER).Length > 1 ? vr.BUFFER[1] : "";
                            buffer = buffer.Replace(vr.BUFFER[0], repl);
                        }
                        NextAction();
                        return;
                    }


                    if (vr.CLR_BUF)
                        buffer = "";

                    if (vr.BUFFER != null)
                        buffer += string.Join(";", vr.BUFFER);


                    if (vr.DEBUG_OFF != vr.DEBUG_ON)
                        debug = vr.DEBUG_ON;

                    if (vr.DEL_CMD != null && !string.IsNullOrEmpty(vr.DEL_CMD[0]))
                    {
                        string etiqdeb = vr.DEL_CMD[0];

                        if (((string[])vr.DEL_CMD).Length > 1)
                        {
                            string etiqend = vr.DEL_CMD[1];
                            int idxdeb = cmdes.FindIndex(startIndex: 1, match: label => label[0].Equals(etiqdeb));
                            int idxend = cmdes.FindIndex(startIndex: 1, match: label => label[0].Equals(etiqend));
                            if (idxdeb >= 0 && idxend >= 0)
                                cmdes.RemoveRange(idxdeb, idxend - idxdeb);
                        }
                    }
                    NextAction();
                    break;
            }

        }
        private static void AddOrInsertCommand(string commande, int priority = 0)
        {
            if (cmdes == null)
                cmdes = new List<string[]>();

            switch (priority)
            {
                case 0: // Add new command from Batch file, erasing all previous commands
                case 2: // Add new command without erasing older
                    foreach (var line in commande.Split('!'))
                    {
                        var list = line.split(true,' ');
                        cmdes.Add(list);
                    }
                    break;
                case 1: // substitute the command by another BatchFile (L) or line command
                    foreach (var line in commande.Split('!'))
                    {
                        var list = line.split(true, ' ');
                        cmdes.Insert(subcmd2++, list);
                    }
                    break;
                default:
                    cmd = '\0';
                    break;
            }
        }

        public static void RefreshParser(string commandes, string booleans, string values)
        {
            vr = new ExpandoObject();
            if (!string.IsNullOrWhiteSpace(commandes))
            {
                foreach (var c in commandes.Split(','))
                {
                    var index = Array.FindIndex(cmdes[0], item => item.Equals(c));
                    AddProperty(c, index != -1);
                }
            }
            if (!string.IsNullOrWhiteSpace(booleans))
            {
                foreach (var b in booleans.Split(','))
                {
                    var index = Array.FindIndex(cmdes[0], item => item.Equals("+" + b));
                    AddProperty(b, index != -1);
                }
            }

            if (!string.IsNullOrWhiteSpace(values))
            {
                foreach (var v in values.Split(','))
                {
                    string[] stringSeparators = new string[] { v + ":" };
                    var index = Array.FindIndex(cmdes[0], item => item.StartsWith(stringSeparators[0]));
                    AddProperty(v, index != -1 ? cmdes[0][index].Split(stringSeparators, 2, StringSplitOptions.None)[1].split(false, ',') : null);
                }
            }
        }
        public static void AddProperty(string propertyName, object propertyValue)
        {
            // ExpandoObject supports IDictionary so we can extend it like this
            var expandoDict = vr as IDictionary<string, object>;
            if (expandoDict.ContainsKey(propertyName))
                expandoDict[propertyName] = propertyValue;
            else
                expandoDict.Add(propertyName, propertyValue);
        }
        public static void RemoveProperty(string propertyName)
        {
            // ExpandoObject supports IDictionary so we can extend it like this
            var expandoDict = vr as IDictionary<string, object>;
            expandoDict.Remove(propertyName);
        }

        public static void RemoveAllProperties()
        {
            if (vr == null) return;
            var expandoDict = vr as IDictionary<string, object>;
            expandoDict.Clear();
            vr = null;
        }
        public static string[] split(this string stringToSplit, bool keepQuote, params char[] delimiters)
        {
            List<string> results = new List<string>();

            bool inQuote = false;
            StringBuilder currentToken = new StringBuilder();
            for (int index = 0; index < stringToSplit.Length; ++index)
            {
                char currentCharacter = stringToSplit[index];
                if (currentCharacter == '"')
                {
                    // When we see a ", we need to decide whether we are
                    // at the start or send of a quoted section...
                    inQuote = !inQuote;
                    if (keepQuote)
                        currentToken.Append(currentCharacter);
                }
                else if (delimiters.Contains(currentCharacter) && inQuote == false)
                {
                    // We've come to the end of a token, so we find the token,
                    // trim it and add it to the collection of results...
                    string result = currentToken.ToString();//.Trim();
                    if (result != "") results.Add(result);

                    // We start a new token...
                    currentToken.Clear();
                }
                else
                {
                    // We've got a 'normal' character, so we add it to
                    // the curent token...
                    currentToken.Append(currentCharacter);
                }
            }

            // We've come to the end of the string, so we add the last token...
            string lastResult = currentToken.ToString();
            if (lastResult != "") results.Add(lastResult);

            return results.ToArray();
        }

        public static bool ExtractAttribute(string v, out string[] attrib)
        {
            //var a = typeof(v) == typeof(string); 
            foreach (var a in cmdes[0])
            {
                if (a.Contains(v))
                {
                    if (v.Equals("say:"))
                        attrib = (a.Substring(v.Length)).Split('!');
                    else
                        attrib = (a.Substring(v.Length)).Split(',');

                    return true;
                }
            }
            attrib = null;
            return false;
        }
        public static bool Compare<T>(T buforadr, string op, T val1, T val2) where T : IComparable
        {
            // ope:@C,string
            // ope:@D,string   
            // ope:@I,pos.lg,string
            // ope:@L,op,2
            //
            //ope:==,string_int   ope:><,string_int1,string_int2

            switch (op)
            {
                case "==": return buforadr.CompareTo(val1) == 0;
                case "<>": return buforadr.CompareTo(val1) != 0;
                case ">": return buforadr.CompareTo(val1) > 0;
                case ">=":
                case "=>": return buforadr.CompareTo(val1) >= 0;
                case "<": return buforadr.CompareTo(val1) < 0;
                case "=<":
                case "<=": return buforadr.CompareTo(val1) <= 0;
                case "><": return buforadr.CompareTo(val1) > 0 && buforadr.CompareTo(val2) < 0;

                case "@C": return buforadr.ToString().Contains(val1.ToString());     // op=@C val1=string
                case "@D": return !buforadr.ToString().Contains(val1.ToString());    // op=@D val1=string
                case "@I":
                    // op=I val1=pos.lg val2=string    pos=nbr[0] lg=nbr{1]  pos = 0 to ... lg = 1 to ...
                    var nbr = val1.ToString().Split('.').Select(c => int.Parse(c)).ToArray();
                    return buforadr.ToString().Substring(nbr[0], nbr[1]).Equals(val2.ToString());
                case "@L":
                    // op=L val1=ope val2=string_int 
                    return Compare(buforadr.ToString().Length, val2.ToString(), Convert.ToInt32(val1), 0);


            }
            return false;
        }

        public static void GoToLblOrNxtAction(bool flag = true)
        {
            if (vr.LBL == null)
            {
                if (flag)
                    NextAction();
            }
            else
            {
                if (flag)
                    $"G {vr.LBL[0]}".ReplaceCurrentCommand();
                else
                    NextAction();
            }
        }
        public static dynamic BeepPlg { get; set; } = null;

        public static void Beep(int frequency, int duration = 300) => true.Beep(frequency, duration);
        public static bool Beep(this bool value, int frequency, int duration = 300)
        {
            if (value)
                BeepPlg?.Beep(frequency, duration);
            return value;
        }
        public static void Beep(this bool value, IList<int> bip1 = null, IList<int> bip2 = null)
        {
            if (value)
            {
                if (bip1 != null)
                {
                    if (bip1.Count < 2)
                        BeepPlg?.Beep(1000, 300);
                    else
                        BeepPlg?.Beep(bip1[0], bip1[1]);
                }
            }
            else
            {
                if (bip2 != null)
                {
                    if (bip2.Count < 2)
                        BeepPlg?.Beep(300, 300);
                    else
                        BeepPlg?.Beep(bip2[0], bip2[1]);
                }
            }
        }
    }
}
