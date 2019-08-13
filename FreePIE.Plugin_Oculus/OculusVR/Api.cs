using System.Runtime.InteropServices;

namespace FreePIE.Plugin_Oculus.OculusVR
{
    public static class Api
    {
        [DllImport("OVRFreePIE.dll", CallingConvention = CallingConvention.Cdecl)]
        private extern static int ovr_freepie_init();
        [DllImport("OVRFreePIE.dll", CallingConvention = CallingConvention.Cdecl)]
        private extern static int ovr_freepie_read(out OculusVr6Dof output);
        [DllImport("OVRFreePIE.dll")]
        private extern static int ovr_freepie_destroy();
        [DllImport("OVRFreePIE.dll")]
        private extern static int ovr_freepie_reset_orientation();

        public static bool Init()
        {
            return ovr_freepie_init() == 0;
        }

        public static OculusVr6Dof Read()
        {
            OculusVr6Dof output;
            ovr_freepie_read(out output);
            return output;
        }

        public static bool Dispose()
        {
            return ovr_freepie_destroy() == 0;
        }

        public static bool Center()
        {
            return ovr_freepie_reset_orientation() == 0;
        }
    }
}
