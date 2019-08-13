using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreePIE.HookInput
{
    class Program
    {
        public static Dictionary<string, int> dico = new Dictionary<string, int>();

        static void Main(string[] args)
        {
            var hookstatus = int.Parse(args[0]);

            AllInputsCommander AIC = new AllInputsCommander(hookstatus);
            AIC.StartLoop();
        }
    }
}
