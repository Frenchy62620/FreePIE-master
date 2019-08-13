using System;
using System.Threading; // bip
using System.Collections.Generic;
using FreePIE.Core.Contracts;
using static FreePIE.CommonTools.GlobalTools;

namespace FreePIE.Core.Plugins
{
    [GlobalType(Type = typeof (BeepGlobal))]
    public class BeepPlugin : Plugin
    {
        private bool running;
        //public readonly Thread _beepThread;
        public readonly AutoResetEvent _signalBeep = new AutoResetEvent(false);
        private int frequency;
        private int duration;
        public override object CreateGlobal()
        {
            return new BeepGlobal(this);
        }
        public override Action Start()
        {
            running = true;
            return ThreadAction;
        }

        private void ThreadAction()
        {
            OnStarted(this, new EventArgs());

            while (running)
            {
                try
                {
                    _signalBeep.WaitOne();
                    Console.Beep(frequency, duration);
                }
                catch (Exception)
                {
                    if (running)
                        throw;

                    break;
                }
            }
        }
        public void Beep(int f, int d = 300)
        {
            if (d == 0) return;
            duration = d;
            frequency = f;
            _signalBeep.Set();
        }
        public void Beep(IList<int> bip)
        {
            if (bip == null) return;
            duration = bip[1];
            frequency = bip[0];
            _signalBeep.Set();
        }
        public override string FriendlyName => "Beep";

        public override void DoBeforeNextExecute()
        {
            CheckScriptTimer();
            if (cmd == 'B')
            {
                int id = cmdes[0][1].IndexOf(',');
                Beep(int.Parse(cmdes[0][1].Substring(0, id)), int.Parse(cmdes[0][1].Substring(id + 1)));
                NextAction();
            }
        }
        public override void Stop()
        {
            running = false;
            duration = 300;
            frequency =1000;
            _signalBeep.Set();
        }
    }

    [Global(Name = "beep")]
    public class BeepGlobal
    {
        private readonly BeepPlugin plugin;
        public BeepGlobal(BeepPlugin plugin)
        {
            this.plugin = plugin;
            BeepPlg = plugin;
        }

        public void play(int frequency, int duration = 300) => plugin.Beep(frequency, duration);
        public void play(bool value, IList<int> bip1 = null, IList<int> bip2 = null)
        {
            if (value)
                plugin.Beep(bip1);
            else
                plugin.Beep(bip2);
        }
    }
}