using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using FreePIE.Core.Persistence.Paths;
using FreePIE.Core.Common;
using System.Reflection;
using static CommonVarAndTools.GlobalTools;

namespace FreePIE.Core.Plugins.Extensions
{
    using System.Text.RegularExpressions;
    using Gx = GlobalExtensionMethods;
    public static class GlobalExtensionMethods
    {
        //private static readonly IPortable portable = new Portable();
        //private static readonly PortablePaths portablepaths = new PortablePaths();
        //private static readonly UacCompliantPaths uacPaths = new UacCompliantPaths(new FileSystem());

        private static readonly string applicationDataSubPath = @"FreePie\files_freepie\";
        private static readonly string applicationDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), applicationDataSubPath);

        private static List<string> commandes;
        public static List<string[]> cmdes;

        // Timer gestion
        private static Stopwatch globaltimer = new Stopwatch();
        private static int Ctime;
        private static long timer;
        private static int endtime;

        public static int lapse_singleclick;
        public static char cmd;
        public static char subcmd1;
        public static int subcmd2;
        public static string[] wd;
        public static string keystyped;
        public static string keystosay;
        public static Dictionary<char, List<MethodInfo>> dico_mi;

        public static string buffer;

        static GlobalExtensionMethods()
        {
            lapse_singleclick = 300;
            wd = new string[2];
            cmd = '\0';
            keystosay = keystyped = "";
            Ctime = 0;
            timer = -1;
        }
        // --------------------- Extension Executing Auto function -----------------------------------
        public static void AddListOfFct(Type t)
        {
            if (dico_mi == null)
                dico_mi = new Dictionary<char, List<MethodInfo>>();

            if (!dico_mi.ContainsKey(cmd))
            {
                var list_mi = t.GetMethods().Where(m => m.Name.Length == 1);
                dico_mi[cmd] = new List<MethodInfo>(list_mi);
            }
        }
        public static void InvokeMethodinfo<T>(ref T instance)
        {
            if (subcmd1 == '9')
            {
                instance = default(T);
                dico_mi.Remove(cmd);
                if (dico_mi.Count == 0)
                    dico_mi = null;
                NextAction();
                return;
            }
            List<MethodInfo> mi;
            dico_mi.TryGetValue(cmd, out mi);
            var method = mi.FirstOrDefault(m => m.Name.Equals(new string(subcmd1, 1)));
            if (method != null)
                method.Invoke(instance, null);
            else
                NextAction();
        }


        internal static void Testfunction(Func<int, bool, bool> f)
        {
            var val = wd[1].Split(':');
            if (f(Convert.ToInt32(val[0]), false)) // True
            {
                if (val.Length > 1)         // Goto Etiq true
                    $"G0;{val[1]}".DecodelineOfCommand(section: null, priority: 1);
                else
                    NextAction();
            }
            else                            // False
            {
                if (val.Length > 2)         // Goto Etiq False
                    $"G0;{val[2]}".DecodelineOfCommand(section: null, priority: 1);
            }
        }

        // ----------- END ------- Extension Executing Auto function --------------------------

        public static SpeechPlugin speechplugin {get; set; }

        public static int SearchPos(string str, string substr, int lg)
        {
            if (str.Contains('-'))
            {
                var s = Convert.ToInt32((substr));
                var bornes = str.Split('-').Select(c => Convert.ToInt32(c)).ToArray();
                if (s < bornes[0] || s > bornes[1])
                    return -1;
                return s - bornes[0];
            }
            var tab = (str.Split(',').Select(s =>
            {
                var st = new string('0', lg) + s;
                return st.Substring(st.Length - lg, lg);
            })).ToArray();
            for (int indice = 0; indice < tab.Count(); indice++)
            {
                if (tab[indice].Equals(substr))
                    return indice;
            }
            return -1;
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
        //public static long GetLapse(this long time)
        //{
        //    if (time < 0)
        //        return -1;
        //    return globaltimer.ElapsedMilliseconds - time;
        //}
        //public static void CheckScriptTimer()
        //{
        //    if (timer >= 0 && timer.GetLapse() >= endtime)
        //    {
        //        timer = StopTimer();
        //        NextAction();
        //    }
        //}
        // ------ END ---------- Extension Global Timer -----------------------------------
        public static string ConvertByteToString(this byte[] source)
        {
            return source != null ? System.Text.Encoding.UTF8.GetString(source) : null;
        }
        //public static string FreePiePath(this string file)
        //{
        //    return Environment.CurrentDirectory.StartsWith(@"C:\Program Files (x86)\FreePIE") ? applicationDataPath : $@"files_freepie\{file}";
        //    //return string.Format("{0}\\{1}\\{2}", portable.IsPortable ? portablepaths.Data : uacPaths.Data, "files_freepie", file);
        //}
        public static string getDataTyped(string indice = null)
        {
            if (indice == null) return keystyped;
            var i = Convert.ToInt32(indice);
            var k = keystyped.Split(';');
            return k.Length > i ? k[i] : "";
        }

        //public static bool Compare<T>(string op, T buf, T val) where T : IComparable
        //{
        //    // @C:val   ou @C,adress,lg:val ou @C,adress1,lg,adress2
        //    // @I,indice:val ou @I,indice.......
        //    // @L,lg:val ou @
        //    //==:val ou == address,mask,shift:val

        //    switch (op.Substring(0, op.Length > 1 ? 2 : 1))
        //    {
        //        case "@C": return buf.ToString().Contains(val.ToString());
        //        case "@D": return !buf.ToString().Contains(val.ToString());
        //        case "@I":
        //            var indice = Convert.ToInt32(op.Substring(2));
        //            return buf.ToString()[indice] == val.ToString()[0];
        //        case "@L":
        //            return Compare(op.Split(',')[1], buf.ToString().Length, Convert.ToInt32(val));
        //        case "==": return buf.CompareTo(val) == 0;
        //        case "<>": return buf.CompareTo(val) != 0;
        //        case ">": return buf.CompareTo(val) > 0;
        //        case ">=": return buf.CompareTo(val) >= 0;
        //        case "<": return buf.CompareTo(val) < 0;
        //        case "<=": return buf.CompareTo(val) <= 0;
        //    }

        //    return false;
        //}

        internal static bool Compare<T>(T buforadr, string op, T val1, T val2) where T : IComparable
        {
            // @C:val   ou @C,adress,lg:val ou @C,adress1,lg,adress2
            // @Iindice:val ou @I,indice.......@Ixxzzz
            // @Llg:val ou @
            //==:val ou == address,mask,shift:val

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

                case "C": return buforadr.ToString().Contains(val1.ToString());
                case "D": return !buforadr.ToString().Contains(val1.ToString());
                case "I":
                    var nbr = Convert.ToInt32(op.Substring(1));
                    var pos = nbr / 100;
                    var lg = nbr - pos * 100;
                    return buforadr.ToString().Substring(pos, lg).Equals(val1.ToString());
                case "L":
                    return Compare(buforadr.ToString().Length, val2.ToString(), Convert.ToInt32(val1), 0);


            }
            return false;
        }
        public static void NextAction()
        {
            cmd = '\0';
            cmdes.RemoveAt(0);
            LoadCommand();
        }


        public static void DecodelineOfCommand1(this string command, string section=null, int priority = 0)
        {
            if (command.Contains(";") || command.Contains("C0"))     // commande unitaire ou Clear Buffer C0?
            {
                AddCommand1(command, priority);
            }
            else
            {
                using (StreamReader file = new StreamReader((@"command\" + command).FreePiePath(), System.Text.Encoding.Default))
                {
                    StringBuilder linesb = new StringBuilder(64);
                    string line;
                    bool startsection = !String.IsNullOrWhiteSpace(section);
                    bool endsection = startsection;
                    int id;

                    while ((line = file.ReadLine()) != null)
                    {
                        if (startsection && (!line.Contains(section) || line.IndexOf('#') != 0)) continue;
                        if (endsection && !startsection && line.Contains(section) && line.IndexOf('#') == 0) break;
                        startsection = false;

                        if ((id = line.IndexOf('#')) == 0)
                        {
                            if (line.Contains("END_SCRIPT")) break;
                            continue;
                        }
                        linesb.Clear().Append(line);
                        //if (line.IndexOf('{') != -1)
                        //    linesb.AppendFormat(line, getDataTyped().Split(';'));
                        //else
                        //    linesb.Append(line);
                        
                        //id = id > 0 ? id - (line.Length - linesb.Length)  - 1 : linesb.Length - 1;
                        id = id > 0 ? id - 1 : linesb.Length - 1;

                        for (; id >= 0; id--)
                        {
                            var c = linesb[id];
                            switch (c)
                            {
                                case ' ':
                                case '\t':
                                    continue;
                                default:
                                    break;
                            }
                            break;
                        }
                        if (id != -1)
                            AddCommand1(linesb.ToString(0, id + 1), priority);
                    }
                }
            }
            LoadCommand1();
        }
        private static void AddCommand1(string commande, int priority = 0)
        {
            if (commandes == null)
                commandes = new List<string>();

            switch (priority)
            {
                case 0: // Add new command in last position and execute only if no active command
                    foreach (var line in commande.Split('!'))
                        commandes.Add(line);
                    return;
                case 1: // Add new command in first position but after the active command if exist
                    break;
                //case 2:
                //    // Insert new file of command
                //    cmd = '\0';
                //    break;
                //case 3: // insert new command or group of command in first position and overwrite the active command (NO command file)
                //    cmd = '\0';
                //    break;
                default:
                    cmd = '\0';
                    break;
            }
            var st = commande.Split('!').Reverse();
            foreach (var line in st)
            {
                if (priority == 2)
                    commandes.Insert(subcmd2, line);
                else
                    commandes.Insert(0, line);
            }
            subcmd2 += st.Count();
        }
        private static void LoadCommand1()
        {
            if (commandes == null || commandes.Count == 0 || cmd != '\0') return;
            if (commandes[0].IndexOf('{') != -1)
                commandes[0] = string.Format(commandes[0], getDataTyped().Split(';'));
            wd = commandes[0].Split(new char[] { ';' }, 2);
            cmd = wd[0][0];
            if (wd[0].Length > 1) subcmd1 = wd[0][1];
            if (wd[0].Length > 2 && cmd != '*') subcmd2 = Convert.ToInt32(wd[0].Substring(2));
            Console.WriteLine($"cde: {commandes[0]}");
            commandes.RemoveAt(0);
            if (commandes.Count == 0)
                commandes = null;
            switch (cmd)
            {
                case '*':
                case '0':    // 0 = nop
                    NextAction();
                    break;
                case 'T':
                    timer = StartTimer();
                    break;
                case 'B':
                    //BeepPlugin.BackgroundBeep.Beep(Convert.ToInt32(wd[1]));
                    NextAction();
                    break;
                case 'G':    // Goto etiq
                    if (string.IsNullOrEmpty(wd[1])) NextAction();
                    while(!commandes[0].Contains(wd[1]) || commandes[0][0] != '*')
                        commandes.RemoveAt(0);
                    NextAction();
                    break;
                case 'L':
                    subcmd2 = 0;
                    var st = wd[1].Split(',');
                    if (st.Length == 2)
                        st[0].DecodelineOfCommand1(st[1], 2);
                    else
                        st[0].DecodelineOfCommand1(null, 2);
                    break;
                case 'C':   // Clear Buffer or dico or  Put in buffer
                    if (subcmd1 == '9') // Clear Dico of Method script function
                    {
                        dico_mi.Clear();
                        dico_mi = null;
                    }
                    else    // Clear Buffer if C0 else Put in buffer
                    {
                        keystosay = "";
                        keystyped = (subcmd1 == '1') ? wd[1] : "";
                    }                       
                    NextAction();
                    break;
                case '9':   // Clear Dico of Method script function
                    dico_mi.Clear();
                    dico_mi = null;
                    break;
            }
            //if (commandes.Count == 0)
            //    commandes = null;
        }
       
        //public static bool PlaySound(this bool value, string audio)
        //{
        //    if (value)
        //    {
        //        //if (!audio.Contains('\\'))
        //        //    audio = audio.FreePiePath();
        //        //NAudioPlugin.Instance.PlaySound(audio);
        //    }
        //    return value;
        //}
        //public static bool PlaySound(this bool value, int idcache)
        //{
        //    //if (value)
        //    //    NAudioPlugin.Instance.PlaySound(idcache);
        //    return value;
        //}
        //public static bool PlaySound(this bool value, int frequency, int duration = 300)
        //{
        //    if (value)
        //        BeepPlugin.BackgroundBeep.Beep(frequency, duration);
        //    return value;
        //}
        //public static void PlaySound(this bool value, IList<int> bip1 = null, IList<int> bip2 = null)
        //{
        //    if (value)
        //    {
        //        if (bip1 != null)
        //        {
        //            if (bip1.Count < 2)
        //                //BeepPlugin.BackgroundBeep.Beep(1000, 300);
        //            else
        //                //BeepPlugin.BackgroundBeep.Beep(bip1[0], bip1[1]);
        //        }
        //    }
        //    else
        //    {
        //        if (bip2 != null)
        //        {
        //            if (bip2.Count < 2)
        //                //BeepPlugin.BackgroundBeep.Beep(200, 300);
        //            else
        //                //BeepPlugin.BackgroundBeep.Beep(bip2[0], bip2[1]);
        //        }
        //    }
        //}

        // test new look

        //public static void DecodelineOfCommand(this string command, string section = null, int priority = 0)
        //{
        //    if (command.Contains(":"))     // commande unitaire cde cat:xxxx,yyyy +oo # cde unitaire
        //    {
        //        AddCommand(command, priority);
        //    }
        //    else
        //    {
        //        using (StreamReader file = new StreamReader((@"command\" + command).FreePiePath(), System.Text.Encoding.Default))
        //        {
        //            StringBuilder linesb = new StringBuilder(64);
        //            string line;
        //            bool startsection = !String.IsNullOrWhiteSpace(section);
        //            bool endsection = startsection;
        //            int id;

        //            while ((line = file.ReadLine()) != null)
        //            {
        //                if (line.Contains("END_SCRIPT")) break;
        //                if (startsection && (!line.Contains(section) || line.IndexOf('#') != 0)) continue;
        //                if (endsection && !startsection && line.Contains(section) && line.IndexOf('#') == 0) break;
        //                startsection = false;

        //                if (line.IndexOf('#') >= 0) line = Regex.Replace(line, @"\s +#.+$", "");
        //                if (string.IsNullOrEmpty(line)) continue;

        //                linesb.Clear().Append(line);
        //                AddCommand(linesb.ToString(), priority);
        //            }
        //        }
        //    }
        //    LoadCommand();
        //}
        private static void AddCommand(string commande, int priority = 0)
        {
            if (cmdes == null)
                cmdes = new List<string[]>();
            Regex regex = new Regex(@"^\*?\w+(?:[\*.]\w+)?|\w+:(?:""[^""]+""|[^\s]+)|\+\w+");

            switch (priority)
            {
                case 0: // Add new command in last position, loading inital from file
                    foreach (var line in commande.Split('!'))
                    {
                        var list = regex.Matches(line).Cast<Match>().Select(match => match.Value.Replace("\"", "")).ToArray();
                        cmdes.Add(list);
                    }
                    return;
                case 1: // insert new command or group of command in first position and overwrite the active command (with regex)
                    cmdes.RemoveAt(0);
                    subcmd2 = 0;
                    foreach (var line in commande.Split('!'))
                    {
                        var list = regex.Matches(line).Cast<Match>().Select(match => match.Value.Replace("\"", "")).ToArray();
                        cmdes.Insert(subcmd2++, list);
                    }

                    break;
                case 2: // Insert new file of command (L)
                    foreach (var line in commande.Split('!'))
                    {
                        var list = regex.Matches(line).Cast<Match>().Select(match => match.Value.Replace("\"", "")).ToArray();
                        cmdes.Insert(subcmd2++, list);
                    }
                    break;
                case 3: // insert new command or group of command in first position no regex
                    cmdes.RemoveAt(0);
                    subcmd2 = 0;
                    foreach (var line in commande.Split('!'))
                    {
                        cmdes.Insert(subcmd2++, new string[] { line });
                    }
                    break;
                default:
                    cmd = '\0';
                    break;
            }
        }
        private static void Testspecialfunctions()
        {
            switch (cmd)
            {
                case '*':
                case '0':    // 0 = nop
                    break;
                case 'T':   // T3000
                    endtime = int.Parse(cmdes[0][0].Substring(1));
                    timer = StartTimer();
                    break;
                case 'B':   // Bfrequency[.duration]
                    int id = cmdes[0][0].IndexOf('.');
                    //if (id < 0)
                    //    BeepPlugin.BackgroundBeep.Beep(int.Parse(cmdes[0][0].Substring(1)));
                    //else
                    //    BeepPlugin.BackgroundBeep.Beep(int.Parse(cmdes[0][0].Substring(1, id - 1)), int.Parse(cmdes[0][0].Substring(id + 1)));
                    break;
                case 'G':   // Goto etiq -> G nam:
                    int result;
                    if (cmdes[0][0].Length < 2 || cmdes[0][0][1] != '*' || (result = cmdes.FindIndex(startIndex: 1, match: l => l[0].StartsWith(cmdes[0][0].Substring(1)))) < 0)
                    {
                        return;
                    }
                    cmdes.RemoveRange(0, result - 1);
                    return;
                case 'L':   // L nam:nameoffile[,_nameofSECTION]
                    string[] attrib;
                    ExtractAttribute("nam:", out attrib);
                    attrib[0].DecodelineOfCommand1(attrib.Length == 2 ? attrib[1] : null, 2);
                    break;
                case 'C':   // Clear Buffer or dico or  Put in buffer
                    if (subcmd1 == '9') // Clear Dico of Method script function
                    {
                        dico_mi.Clear();
                        dico_mi = null;
                    }
                    else    // Clear Buffer if C0 else Put in buffer
                    {
                        keystosay = "";
                        keystyped = (subcmd1 == '1') ? wd[1] : "";
                    }
                    NextAction();
                    break;
                case '9':   // Clear Dico of Method script function
                    dico_mi.Clear();
                    dico_mi = null;
                    break;
            }
        }
        private static void LoadCommand()
        {
            if (cmdes == null || cmdes.Count == 0 || cmd != '\0') return;

            // gestion du {0] dasn un string
            //if (cmdes[0].IndexOf('{') != -1)
            //    cmdes[0] = string.Format(commandes[0], getDataTyped().Split(';'));


            //wd = cmdes[0];
            
            cmd = cmdes[0][0][0];


            Console.Write("cde:");
            foreach (var line in cmdes[0][0])
                Console.WriteLine($" {line}");

            //Testspecialfunctions();
            switch (cmd)
            {
                case '*':
                case '0':    // 0 = nop
                    NextAction();
                    break;
                case 'T':   // T3000
                    endtime = int.Parse(cmdes[0][0].Substring(1));
                    timer = StartTimer();
                    break;
                case 'B':   // Bfrequency[.duration]
                    //int id = cmdes[0][0].IndexOf('.');
                    //if (id < 0)
                    //    BeepPlugin.BackgroundBeep.Beep(int.Parse(cmdes[0][0].Substring(1)));
                    //else
                    //    BeepPlugin.BackgroundBeep.Beep(int.Parse(cmdes[0][0].Substring(1, id - 1)), int.Parse(cmdes[0][0].Substring(id + 1)));
                    NextAction();
                    break;
                case 'G':   // Goto etiq -> G nam:
                    int result;
                    if (cmdes[0][0].Length < 2 || cmdes[0][0][1] != '*' || (result = cmdes.FindIndex(startIndex: 1, match: l => l[0].StartsWith(cmdes[0][0].Substring(1)))) < 0)
                    {
                        return;
                    }
                    cmdes.RemoveRange(0, result - 1);
                    NextAction();
                    break;
                case 'L':   // L nam:nameoffile[,_nameofSECTION]
                    string[] attrib;
                    ExtractAttribute("nam:", out attrib);
                    subcmd2 = 0;
                    attrib[0].DecodelineOfCommand(attrib.Length == 2 ? attrib[1] : null, 2);
                    break;
                case 'C':   // Clear Buffer or dico or  Put in buffer
                    if (subcmd1 == '9') // Clear Dico of Method script function
                    {
                        dico_mi.Clear();
                        dico_mi = null;
                    }
                    else    // Clear Buffer if C0 else Put in buffer
                    {
                        keystosay = "";
                        keystyped = (subcmd1 == '1') ? wd[1] : "";
                    }
                    NextAction();
                    break;
                case '9':   // Clear Dico of Method script function
                    dico_mi.Clear();
                    dico_mi = null;
                    NextAction();
                    break;
            }

        }

        internal static void GoToLblOrNxtAction()
        {
            string[] attrib;
            var lbl = ExtractAttribute("lbl:", out attrib) ? attrib[0] : "";
            if (string.IsNullOrEmpty(lbl))
                NextAction();
            else
                $"G {lbl}".DecodelineOfCommand(section: null, priority: 1);
        }
        internal static void ModifyCMD(string v, string[] act)
        {
            string[] cmd;
            for (int i = 0; i < cmdes[0].Length; i++)
                if (cmdes[0][i].Contains(v))
                {
                    if (v[0] == '+')    // For +idle 
                    {
                        cmdes[0][i] = v.Substring(1);
                        return;
                    }
                    cmd = (cmdes[0][i].Substring(v.Length)).Split(',');
                    StringBuilder sb = new StringBuilder(v, 128);
                    foreach (var a in act)
                        sb.Append($"{cmd[0]}{a},");
                    sb.Length--;
                    cmdes[0][i] = sb.ToString();
                    break;
                }
        }
        internal static bool ExtractAttribute(string v, out string[] attrib)
        {
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

        //    var captured = matchList
        //        // linq-ify into list
        //        .Cast<Match>()
        //        // flatten to single list
        //        .SelectMany(o =>
        //            // linq-ify
        //            o.Groups.Cast<Capture>()
        //                // don't need the pattern
        //                .Skip(1).SkipWhile(c => string.IsNullOrEmpty(c.Value))
        //                .TakeWhile(c => !string.IsNullOrEmpty(c.Value))
        //                // select what you wanted
        //                .Select(c => c.Value)).ToArray();
        //}


        //private static void ExtractAttributes()
        //{
        //    var cmde = captured[0][0];
        //    var subcmde = captured[0].Length == 2 ? captured[0][1] : '\0';
        //    for (int i = 0; i < captured.Length; i++)
        //    {
        //        if (captured[i].Contains(':'))
        //        {
        //            var a = captured[i + 1].Split(',');
        //            var b = a.Length;
        //        }
        //    }
        //}
        //if (commandes.Count == 0)
        //    commandes = null;


        //public static IEnumerable<Type> GetLoadableTypes(this Assembly assembly)
        //{
        //    if (assembly == null) throw new ArgumentNullException(nameof(assembly));
        //    try
        //    {
        //        return assembly.GetTypes();
        //    }
        //    catch (ReflectionTypeLoadException e)
        //    {
        //        return e.Types.Where(t => t != null);
        //    }
        //}

    }
}
