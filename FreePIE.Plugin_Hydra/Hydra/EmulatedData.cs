namespace FreePIE.Plugin_Hydra.Hydra
{
    internal struct EmulatedData
    {
        public float Yaw, Pitch, Roll;
        public float X, Y, Z;
        public float JoystickX;
        public float JoystickY;
        public float Trigger;
        public int Buttons;
        public int Enabled;
        public int ControllerIndex;
        public byte IsDocked;
        public byte WhichHand;
    }
}