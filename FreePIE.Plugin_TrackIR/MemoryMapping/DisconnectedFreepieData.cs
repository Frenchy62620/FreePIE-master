using System.Runtime.InteropServices;
using FreePIE.Plugin_TrackIR.TrackIR;

namespace FreePIE.Plugin_TrackIR.MemoryMapping
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct DisconnectedFreepieData
    {
        public readonly TrackIRData TrackIRData;
        public const string SharedMemoryName = "FreePIEDisconnectedData";
    }
}
