using System;

namespace FreePIE.Plugin_Wiimote.Wiimote
{
    public class Subscribable
    {
        public Subscribable(out Action trigger)
        {
            trigger = OnUpdate;
        }

        private void OnUpdate()
        {
            if (update != null)
                update();
        }

        public event Action update;
    }
}
