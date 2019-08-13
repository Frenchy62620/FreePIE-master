using System.Runtime.InteropServices;

namespace FreePIE.Plugin_Oculus.OculusVR
{
    [StructLayout(LayoutKind.Sequential)]
    public struct OculusVr6Dof
    {
        public float Yaw;
        public float Pitch;
        public float Roll;
        public float X;
        public float Y;
        public float Z;
    }
}
