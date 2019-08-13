using System;
using System.Collections;
using FreePIE.Core.Contracts;
using static FreePIE.CommonTools.GlobalTools;
using HidSharp;
using FreePIE.Plugin_Warthogs.ScriptAuto;

// see http://members.aon.at/mfranz/warthog.html to learn to use HID with warthog

namespace FreePIE.Plugin_Warthog
{
    [GlobalType(Type = typeof (WarthogGlobal))]
    public class WarthogPlugin : Plugin
    {
        const int VID_WARTHOG_THROTTLE = 0x44F;
        const int PID_WARTHOG_THROTTLE = 0x404;
        private HidDeviceLoader loader;
        private HidDevice HidDevice;
        private HidStream stream;
        private blinking bk;
        private readonly byte[] b = new byte[] { 0x04, 0x02, 0x10, 0x01, 0x40, 0x08 };
        private byte[] buffer;

        private ScriptWarthog SF;

        public class blinking
        {
            public BitArray stateleds;
            public int firstledtoflash;
            public long timer;
            public bool OneLedblinking;
            public blinking()
            {
                // 00-05 current state, 06-11 state saved (before blinking)
                // 12-17 blinking, 18 if true -> at less one update, 19 unused
                stateleds = new BitArray(20, false);    
                timer = -1;
                firstledtoflash = -1;
            }
        }
        public int Brightness
        {
            get { return buffer[3]; }
            set { buffer[3] =(byte) value; }
        } 
        public long duration { get; set; } = 800;

        public override object CreateGlobal() => new WarthogGlobal(this);
        public override string FriendlyName => "warthog";

        public override Action Start()
        {
            bk = new blinking();
            loader = new HidDeviceLoader();
            HidDevice = loader.GetDeviceOrDefault(VID_WARTHOG_THROTTLE, PID_WARTHOG_THROTTLE);
            if (HidDevice == null)
                throw new Exception("The Warthog Trottle seems to be not connected");
            buffer = new byte[HidDevice.MaxInputReportLength];
            buffer[0] = 1; buffer[1] = 6; buffer[2] = 0; buffer[3] = 5;

            if (!HidDevice.TryOpen(out stream))
                throw new Exception("Failed to open the device Warthog");
            Brightness = 5;
            SetAllLeds(false);
           // OnStarted(this, new EventArgs());
            return null;
        }

        public override void Stop()
        {
            SF = null;
            SetAllLeds(false);
            stream.Close();
            buffer = null;
            HidDevice = null;
            loader = null;
        }

        public override void DoBeforeNextExecute()
        {
            CheckScriptTimer();
            var lapse = bk.timer.GetLapse();
            if (lapse > duration)
            {
                for (int led = 0; led < 6; led++)
                {
                    if (!bk.stateleds[led + 12]) continue;
                    UpdateBufferLed(led, !bk.stateleds[led]);
                }
                bk.timer = ReStartTimer();
            }
            if (bk.stateleds[18])
                stream.Write(buffer, 0, buffer.Length);
            bk.stateleds[18] = false;

            if (cmd == 'W')
            {
                if (SF == null)
                {
                    SF = new ScriptWarthog(this);
                }
                SF.Warthog();
            }
        }

        public void SetAllLeds(bool on)
         {
             buffer[2] = (byte)(on ? 0x5F : 0);
             bk.stateleds[18] = true;
            for (int led = 0; led < 6; led++)
            {
                bk.stateleds[led] = on;
                bk.stateleds[led + 6] = false;
                bk.stateleds[led + 12] = false;
            }
         }
        private void StartLoop(int led)
        {
            if (bk.stateleds[led + 12])
                return;

            bk.stateleds[led + 12] = true;
            bk.stateleds[led + 6] = bk.stateleds[led];

            if (!bk.OneLedblinking)
            {
                bk.firstledtoflash = led;
                bk.timer = StartTimer();
                bk.OneLedblinking = true;
            }
            UpdateBufferLed(led, bk.stateleds[bk.firstledtoflash]);
        }

        private void StopLoop(int led)
         {
            if (!bk.stateleds[led + 12]) return; // already stop?
            bk.stateleds[led + 12] = false; // blinking off
            UpdateBufferLed(led, bk.stateleds[led + 6]); // restore previous status of led
            bk.stateleds[led + 6] = false;
            if (!IsOneLedFlashing())
            {
                bk.timer = StopTimer();
                bk.OneLedblinking = false;
                bk.firstledtoflash = -1;
            }
        }
        private bool IsOneLedFlashing()
        {
            for (int i = 0; i < 6; i++)
                if (IsLedFlashing(i)) return true;

            return false;
        }
        public void SetLedFlashing(int led, bool on)
        {
            if (on)
                StartLoop(led);
            else
                StopLoop(led);
        }
        public void SetLed(int led, bool on)
        {
            if (IsLedFlashing(led)) StopLoop(led);
            UpdateBufferLed(led, on);
        }
        public bool IsLedFlashing(int led) => bk.stateleds[led + 12];
        public bool IsLedOn(int led)=> (buffer[2] & b[led]) != 0;
        public void ToggleLed(int led) => UpdateBufferLed(led, !IsLedOn(led));

        private void UpdateBufferLed(int led, bool on)
        {
            if (bk.stateleds[led] == on) return;
            bk.stateleds[led] = on;

            if (on)
                buffer[2] |= b[led];
            else
                buffer[2] &= (byte)~b[led];
            bk.stateleds[18] = true;
        }
 

    
    }
    // led1, led2, led3, led4, led5 and backlight -> 0 to 4 and 5
    [Global(Name = "warthog")]
    public class WarthogGlobal 
    {
        private readonly WarthogPlugin plugin;
        public WarthogGlobal(WarthogPlugin plugin)
        {
            this.plugin = plugin;
        }
        public void setLeds(bool on)
        {
            plugin.SetAllLeds(on);
        }
        public void setLed(bool on, params int[] leds)
        {
            foreach (var numled in leds)
                plugin.SetLed(numled, on);
        }
        public void toggleLed(params int[] leds)
        {
            foreach (var numled in leds)
                plugin.ToggleLed(numled);
        }

        public void setLedFlashing(bool on, params int[] leds)
        {
            foreach (var numled in leds)
                plugin.SetLedFlashing(numled, on);
        }
 
        public int brigthness
        {
            set { plugin.Brightness =(brigthness > 5 || brigthness < 0) ? 5 : brigthness; }
            get { return plugin.Brightness; }
        }
        public int duration
        {
            set { plugin.duration = value; }
            get { return (int)plugin.duration; }
        }
        public bool isLedOn(int led)
        {
            return plugin.IsLedOn(led);
        }
        public bool isLedFlashing(int led)
        {
            return plugin.IsLedFlashing(led);
        }
    }
}

