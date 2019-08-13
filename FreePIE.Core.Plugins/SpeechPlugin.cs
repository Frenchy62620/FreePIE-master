using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Xml;
using System.Collections.Generic;
using System.Speech.Recognition;
using System.Speech.Synthesis;
using System.Speech.Recognition.SrgsGrammar;
using FreePIE.Core.Contracts;
using FreePIE.Core.Plugins.ScriptAuto;
using static FreePIE.CommonTools.GlobalTools;

//using System.Speech.Recognition.SrgsGrammar;
namespace FreePIE.Core.Plugins
{

    [GlobalType(Type = typeof (SpeechGlobal))]
    public class SpeechPlugin : Plugin
    {
        private SpeechSynthesizer synth;
        private Prompt prompt;
        private string word;
        private ScriptSpeech SF;

        private SpeechRecognitionEngine recognitionEngine;
        private Dictionary<string, RecognitionInfo> recognizerResults;
        public string semantic;
        private readonly string folder = @"vocal\";
        private List<string> grammarName;
        public override object CreateGlobal()
        {
            //speechplugin = this;
            return new SpeechGlobal(this);
        }

        public override void DoBeforeNextExecute()
        {
            CheckScriptTimer();

            if (bufferTosay != "")
            {
                if (bufferTosay.Contains(";"))
                {
                    PromptBuilder pb = new PromptBuilder();
                    foreach (var s in bufferTosay.Split(';'))
                    {
                        //pb.StartParagraph();
                        //pb.StartSentence();
                        pb.AppendText(s);
                        pb.AppendBreak(PromptBreak.Small);
                        //pb.EndSentence();
                        //pb.EndParagraph();
                    }
                    Say(pb);
                }
                else
                    Say(bufferTosay);

                bufferTosay = "";
                return;
            }

            if (cmd == 'S')
            {
                if (SF == null)
                {
                    SF = new ScriptSpeech(this);
                }
                SF.Speech();
            }

        }

        public override void Stop()
        {
            SF = null;
            synth?.Dispose();

            if (recognitionEngine == null) return;
            recognitionEngine.RecognizeAsyncStop();
            recognitionEngine.UnloadAllGrammars();
            recognitionEngine.Dispose();
        }

        public void SelectVoice(string name)
        {
            EnsureSynthesizer();
            synth.SelectVoice(name);
        }

        public void Say(string text)
        {
            EnsureSynthesizer();

            if (prompt != null)
                synth.SpeakAsyncCancel(prompt);

            prompt = synth.SpeakAsync(text);
        }
        public void SayOne(string text)
        {
            if (Speaking || text.Equals(word))
                return;
            EnsureSynthesizer();

            if (prompt != null)
                synth.SpeakAsyncCancel(prompt);

            prompt = synth.SpeakAsync(text);
            word = text;
        }
        public void Say(PromptBuilder text)
        {
            EnsureSynthesizer();

            if (prompt != null)
                synth.SpeakAsyncCancel(prompt);

            prompt = synth.SpeakAsync(text);
        }
        public bool Said(string text, float confidence)
        {
            if(confidence < 0.0 || confidence > 1.0) throw new ArgumentException("Confidence has to be between 0.0 and 1.0");

            var init = EnsureRecognizer();

            if (!recognizerResults.ContainsKey(text))
            {
                var builder = new GrammarBuilder(text);
                recognitionEngine.LoadGrammarAsync(new Grammar(builder));
                recognizerResults[text] = new RecognitionInfo(confidence);
            }

            if (init)
            {
                recognitionEngine.SetInputToDefaultAudioDevice();
                recognitionEngine.RecognizeAsync(RecognizeMode.Multiple);
            }
            var info = recognizerResults[text];
            var result = info.Result;
            info.Result = false;

            return result;
        }


        public bool Said()
        {
            RecognitionInfo info;
            recognizerResults.TryGetValue("++", out info);
            var result = info.Result;
            info.Result = false;
            return result;
        }

        private bool EnsureRecognizer()
        {
            var result = recognitionEngine == null;

            if (recognitionEngine == null)
            {
                recognitionEngine = new SpeechRecognitionEngine();
                recognizerResults = new Dictionary<string, RecognitionInfo>();
                grammarName = new List<string>();
//                recognitionEngine.SpeechRecognized += new EventHandler<SpeechRecognizedEventArgs>(recognizer_SpeechRecognized);
//                return true;
                recognitionEngine.SpeechRecognized += (s, e) =>
                {
                    RecognitionInfo info;
                    if (recognizerResults.TryGetValue(e.Result.Text, out info))
                    {
                        if (e.Result.Confidence >= info.Confidence)
                            info.Result = true;
                    }
                    else
                    {
                        recognizerResults.TryGetValue("++", out info);
                        if (e.Result.Confidence >= info.Confidence)
                        {
                            info.Result = true;
                            semantic = (string)e.Result.Semantics["result"].Value;
                        }
                    }
                };

            }

            return result;
        }

        public bool Speaking { get; private set; }
        private void EnsureSynthesizer()
        {
            if (synth == null)
            {
                synth = new SpeechSynthesizer();
                synth.SetOutputToDefaultAudioDevice();
                synth.SpeakCompleted += (s, e) => Speaking = false;
                synth.SpeakStarted += (s, e) => Speaking = true;
            }
        }

        public override string FriendlyName => "Speech";

        private class RecognitionInfo
        {
            public bool Result { get; set; }
            public float Confidence { get; private set; }

            public RecognitionInfo(float confidence)
            {
                Confidence = confidence;
            }
        }
        public void compile(string xmlfile)
        {
            xmlfile = folder + xmlfile;
            string cfgfile = xmlfile.Replace(".xml", ".cfg");
            GrammarCompile(xmlfile.FreePiePath(), cfgfile.FreePiePath());
        }
        public void LoadGrammar(IList<string> cfgfiles)
        {
            Said("++", 0.9f);
            foreach (var cfgfile in cfgfiles)
            {
                string xmlfile = cfgfile.Replace(".cfg", ".xml");
                // Create a Grammar object and load it to the recognizer.
                Grammar g = new Grammar((folder + cfgfile).FreePiePath()) {Name = GetName(folder + xmlfile, true) };
                grammarName.Add(g.Name);
                recognitionEngine.LoadGrammar(g);
            }
            while(string.IsNullOrEmpty(recognitionEngine.Grammars[0].Name))
                recognitionEngine.UnloadGrammar(recognitionEngine.Grammars[0]);
            //if (string.IsNullOrEmpty(recognitionEngine.Grammars[0].Name)) recognitionEngine.UnloadGrammar(recognitionEngine.Grammars[0]);

            true.Beep(1000, 300);
            //BeepPlugin.BackgroundBeep.Beep(1000, 300);
            // Console.WriteLine("Starting asynchronous recognition...");
        }
        private string GetName(string file, bool add_dir = false)
        {
            if (add_dir) file = file.FreePiePath();
            XDocument xdoc = XDocument.Load(file);
            XNamespace ns = xdoc.Root.Attribute("xmlns").Value;
            return xdoc.Root.Descendants(ns + "meta").First().Attribute("name").Value;
        }

        // Write to the console the text and the semantics from the recognition result.
        //static void recognizer_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        //{
        //  Console.WriteLine("Speech recognized: " + e.Result.Text);
        //  Console.WriteLine();
        //  Console.WriteLine("Semantic results:");
        //  Console.WriteLine("  The departure city is: " + e.Result.Semantics["result"].Value);
        ////  Console.WriteLine("  The arrival city is: " + e.Result.Semantics["GoingTo"].Value);
        //}


        public void SetGrammar(IList<string> names, bool on)
        {
            if (names.Count == 1 && names[0].Equals("*"))
            {
                foreach (var g in recognitionEngine.Grammars)
                    if (!g.Name.Equals("master")) g.Enabled = on;
            }
            else
                foreach (var g in recognitionEngine.Grammars)
                    if (names.Contains(g.Name)) g.Enabled = on;

            //BeepPlugin.BackgroundBeep.Beep(on ? 1000 : 200, 300);
            true.Beep(on ? 1000 : 200, 300);
        }

        public void GrammarCompile(string xml_grammar, string cfg_grammar)
        {
            FileStream fs = new FileStream(cfg_grammar, FileMode.Create);
            XmlReader reader = XmlReader.Create(xml_grammar);
            SrgsGrammarCompiler.Compile(reader, (Stream)fs);
            fs.Close();
        }
    }

    [Global(Name = "speech")]
    public class SpeechGlobal
    {
        private readonly SpeechPlugin plugin;

        public SpeechGlobal(SpeechPlugin plugin)
        {
            this.plugin = plugin;
        }
        public void sayone(string text)
        {
            plugin.SayOne(text);
        }
        public void say(string text)
        {
            plugin.Say(text);
        }
        public void say(int select, IList<string> text)
        {
            plugin.Say(text[select]);
        }

        public void say(IList<bool> value, IList<string> text)
        {
            for (int i = 0; i < value.Count; i++)
            {
                if (value[i])
                {
                    plugin.Say(text[i]);
                    return;
                }
            }
        }

        public bool speaking => plugin.Speaking;

        public bool said(string text, float confidence = 0.9f) => plugin.Said(text, confidence);
        public int said(IList<string> text, float confidence = 0.9f)
        {
            for (int i = 0; i < text.Count(); i++)
                if (plugin.Said(text[i], confidence))
                    return i;
            return -1;

        }
        public bool saidFromfile()
        {
            return plugin.Said();
        }
        public string result => plugin.semantic;

        public void selectVoice(string name)
        {
            plugin.SelectVoice(name);
        }
        //************************* grammar
        public void loadCFG(IList<string> cfgfiles)
        {
            plugin.LoadGrammar(cfgfiles);
        }
        public void setCFG(IList<string> names, bool on)
        {
            plugin.SetGrammar(names, on);
        }

        public void compile(IList<string> xmlfiles)
        {
            foreach (var xmlfile in xmlfiles)
                plugin.compile(xmlfile);
        }
    }
}
