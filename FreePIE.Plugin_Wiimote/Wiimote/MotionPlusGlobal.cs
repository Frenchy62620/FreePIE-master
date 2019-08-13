using System;

namespace FreePIE.Plugin_Wiimote.Wiimote
{
    public class MotionPlusGlobal : Calibratable
    {
        private IWiimoteData data;

        public MotionPlusGlobal(IWiimoteData data, out Action trigger, out Action calibrated) : base(out trigger, out calibrated)
        {
            this.data = data;
        }

        public double yaw_down { get { return data.MotionPlus.Value.x; } }
        public double pitch_left { get { return data.MotionPlus.Value.y; } }
        public double roll_left { get { return data.MotionPlus.Value.z; } }
    }
}
