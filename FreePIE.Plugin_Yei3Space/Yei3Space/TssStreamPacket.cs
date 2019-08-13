using System.Runtime.InteropServices;

namespace FreePIE.Plugin_Yei3Space.Yei3Space
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct TssStreamPacketQuat
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] quat;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct TssStreamPacketQuatButton
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] quat;
        public byte button_state;
    }
}
