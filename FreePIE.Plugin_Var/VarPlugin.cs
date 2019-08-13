using System.Collections.Generic;
using System.Threading;
using FreePIE.CommonStrategy;
using FreePIE.Core.Contracts;
using FreePIE.Core.ScriptEngine.Globals.ScriptHelpers;
using static FreePIE.CommonTools.GlobalTools;

namespace FreePIE.Plugin_Var
{
    [GlobalType(Type = typeof(VarGlobal))]
    public class VarPlugin : Plugin
    {
       // private readonly Dictionary<string, Stopwatch> stopwatches;
        private GetPressedStrategy<string> getPressedStrategy;
        public VarPlugin()
        {
            getPressedStrategy = new GetPressedStrategy<string>(IsVarDown);
        }
        public override object CreateGlobal()
        {
            return new VarGlobal(this);
        }

        public override string FriendlyName => "var";

        public bool IsPressed(bool value, string indexer) => getPressedStrategy.IsPressed(indexer, value);
        public bool IsReleased(bool value, string indexer) => getPressedStrategy.IsReleased(indexer, value);
        public bool IsVarDown(string indexer, bool value) => value;

        //begin single and double click

        public void SetLapseSingleClick(int value)
        {
            lapse_singleclick = value;
        }

        public bool IsSingleClicked(bool value, string indexer) => getPressedStrategy.IsSingleClicked(indexer, value);
        public bool IsDoubleClicked(bool value, string indexer) => getPressedStrategy.IsDoubleClicked(indexer, value);

        public int HeldDown(bool value, int nbvalue, int lapse, string indexer) => getPressedStrategy.HeldDown(indexer, value, nbvalue, lapse);
        public void HeldDownStop(string indexer) => getPressedStrategy.HeldDownStop(indexer);
        public bool Repeat(bool value, int lapse, string indexer) => getPressedStrategy.Repeated(indexer, value, lapse);

        //   // return getVarPressedStrategy.Repeated(indexer, value, milliseconds);
        //}
        //public int Get4Direction(float value, Axis a, int XorY, int Y = 0)
        //{
        //    switch (a)
        //    {
        //        case Axis.X:
        //        case Axis.Y:
        //            return FindDirection(value, a, XorY);
        //        case Axis.XY:
        //            return Get8Direction(value, XorY, Y, true);
        //        default:
        //            return -1;
        //    }
        //}
        //public int Get8Direction(float value, int X, int Y, bool fourdir = false)
        //{
        //    int x = FindDirection(value, Axis.X, X), y = FindDirection(value, Axis.Y, Y);
        //    if (x < 0) return y;
        //    if (y < 0) return x;
        //    if (fourdir) return -1;
        //    // x , y         x , y
        //    // 1 , 0 -> 4 ;  1 , 2 -> 5
        //    // 3 , 2 -> 6 ;  3 , 0 -> 7
        //    return x == 1 ? y / 2 + 4 : 7 - y / 2;
        //}
        //private int FindDirection(float value, Axis a, int XorY)
        //{
        //    if (Math.Abs(XorY) < value) return -1;
        //    return  XorY > 0? (int)a - 1 :(int)a + 1;
        //}

        public void SendCommand(string command, int priority = 0)
        {
            command.AddNewCommand(priority);
        }
    }

    [Global(Name = "var")]
    public class VarGlobal
    {
        private readonly VarPlugin plugin;
        public VarGlobal(VarPlugin plugin)
        {
            this.plugin = plugin;
        }

        // *************** Pressed **************************************************************
        [NeedIndexer]
        public bool getPressedBip(bool value, int frequency, int duration, string indexer)
        {
            return plugin.IsPressed(value, indexer).Beep(frequency, duration);
        }

        [NeedIndexer]
        public bool getPressed(bool value, string indexer)
        {
            return plugin.IsPressed(value, indexer);
        }
        // *************** Released **************************************************************
        [NeedIndexer]
        public bool getReleasedBip(bool value, int frequency, int duration, string indexer)
        {
            return plugin.IsReleased(value, indexer).Beep(frequency, duration);
        }

        [NeedIndexer]
        public bool getReleased(bool value, string indexer)
        {
            return plugin.IsReleased(value, indexer);
        }
        [NeedIndexer]
        public List<bool> getStates(bool value, int state /* 1 down 2 Pressed, 4 Released */, string indexer)
        {
            List<bool> b = new List<bool>();
            if ((state & 0x01) != 0) b.Add(value);
            if ((state & 0x02) != 0) b.Add(plugin.IsPressed(value, indexer));
            if ((state & 0x04) != 0) b.Add(plugin.IsReleased(value, indexer));
            return b;
        }


        // *************** single ou double Clicked **************************************************************
        [NeedIndexer]
        public bool getClicked(bool value, bool doubleclick, string indexer)
        {
            return doubleclick ? plugin.IsDoubleClicked(value, indexer) : plugin.IsSingleClicked(value, indexer);
        }
        [NeedIndexer]
        public bool getClickedBip(bool value, bool doubleclick, int frequency, int duration, string indexer)
        {
            return doubleclick ? plugin.IsDoubleClicked(value, indexer).Beep(frequency, duration) : plugin.IsSingleClicked(value, indexer).Beep(frequency, duration);
        }
        // *************** heldDown **************************************************************
        [NeedIndexer]
        public int getHeldDown(bool value, int nbvalue, int duration, string indexer) => plugin.HeldDown(value, nbvalue, duration, indexer);
        [NeedIndexer]
        public void getHeldDownStop(bool value, int nbvalue, int duration, string indexer) => plugin.HeldDownStop(indexer);

        [NeedIndexer]
        public bool getRepeat(bool value, int lapse, string indexer) => plugin.Repeat(value, lapse, indexer);

        // *************** change value duration sglClick ****************************************
        public void wait(int time)
        {
            Thread.Sleep(time);
        }
        public int lapseSingleClick
        {
            set { plugin.SetLapseSingleClick(value); }
        }
        // *************** call command file ****************************************
        public void sendCommand(string command, int priority = 0)
        {
            plugin.SendCommand(command, priority);
        }

        public string DataInBuffer
        {
            get
            {
                return buffer; ;
            }
            set
            {
                buffer += value;
            }
        }
    }
}
