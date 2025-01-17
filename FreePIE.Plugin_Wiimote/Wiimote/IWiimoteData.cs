﻿namespace FreePIE.Plugin_Wiimote.Wiimote
{
    public interface IWiimoteData
    {
        byte WiimoteNumber { get; }
        bool IsButtonPressed(WiimoteButtons b);
        CalibratedValue<Acceleration> Acceleration { get; }
        CalibratedValue<Gyro> MotionPlus { get; }
        EulerAngles MotionPlusEulerAngles { get; }
        Nunchuck Nunchuck { get; }
        ClassicController ClassicController { get; }
        Guitar Guitar { get; }
        BalanceBoard BalanceBoard { get; }
        bool IsDataValid(WiimoteDataValid valid);
        bool IsNunchuckButtonPressed(NunchuckButtons nunchuckButtons);
        bool IsClassicControllerButtonPressed(ClassicControllerButtons classicControllerButtons);
        bool IsGuitarButtonPressed(GuitarButtons guitarButtons);
        WiimoteCapabilities EnabledCapabilities { get; set; }
        WiimoteCapabilities AvailableCapabilities { get; set; }
        WiimoteExtensions ExtensionType { get; set; }
        int BatteryPercentage { get; set; }
        int LEDStatus { get; set; }
        ulong ExtensionID { get; set; }
    }

    public class Gyro
    {
        public Gyro(double x, double y, double z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public double x { get; private set; }
        public double y { get; private set; }
        public double z { get; private set; }
    }
}
