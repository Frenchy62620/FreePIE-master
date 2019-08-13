using System;

namespace FreePIE.Plugin_Wiimote.Wiimote
{
    public class Calibratable : Subscribable
    {
        public Calibratable(out Action triggerUpdate, out Action triggerCalibrated) : base(out triggerUpdate)
        {
            triggerCalibrated = OnCalibrated;
        }

        private void OnCalibrated()
        {
            if (calibrated != null)
                calibrated();
        }

        public event Action calibrated;
    }
}