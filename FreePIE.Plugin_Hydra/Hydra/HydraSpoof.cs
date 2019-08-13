using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;

namespace FreePIE.Plugin_Hydra.Hydra
{
    internal class HydraSpoof
    {
        private readonly int controllerCount;
        private readonly MemoryMappedFile memoryMappedFile;
        private readonly MemoryMappedViewAccessor accessor;

        public HydraSpoof(int controllerCount)
        {
            this.controllerCount = controllerCount;
            memoryMappedFile = MemoryMappedFile.CreateOrOpen("SixenseEmulatedData", Marshal.SizeOf(typeof(EmulatedData)) * controllerCount);
            accessor = memoryMappedFile.CreateViewAccessor();
        }

        public void Write(EmulatedData[] data)
        {
            for (int i = 0; i < controllerCount; i++)
                data[i].ControllerIndex = i;

            accessor.WriteArray(0, data, 0, controllerCount);
        }
    }
}
