using System;
using System.Collections.Generic;
using System.Diagnostics;
using FreePIE.Core.Contracts;
using FreePIE.Core.ScriptEngine.Globals.ScriptHelpers.Strategies;
using System.Reflection;
using Microsoft.CSharp;
using System.CodeDom.Compiler;
using System.IO;

namespace FreePIE.Core.ScriptEngine.Globals.ScriptHelpers
{
    [GlobalEnum]
    public enum Units
    {
        Degrees = 1,
        Radians = 2
    }

    [Global(Name = "filters")]
    public class FilterHelper : IScriptHelper
    {
        private readonly Dictionary<string, double> deltaLastSamples;
        private readonly Dictionary<string, double> simpleLastSamples;
        private readonly Dictionary<string, ContinuesRotationStrategy> continousRotationStrategies;

        public FilterHelper()
        {
            deltaLastSamples = new Dictionary<string, double>();
            simpleLastSamples = new Dictionary<string, double>();
            continousRotationStrategies = new Dictionary<string, ContinuesRotationStrategy>();
        }

        [NeedIndexer]
        public double simple(double x, double smoothing, string indexer)
        {
            if (smoothing < 0 || smoothing > 1)
                throw new ArgumentException("Smoothing must be a value between 0 and 1");

            var lastSample = x;
            if (simpleLastSamples.ContainsKey(indexer))
                lastSample = simpleLastSamples[indexer];

            lastSample = (lastSample * smoothing) + (x * (1 - smoothing));
            simpleLastSamples[indexer] = lastSample;

            return lastSample;
        }

        [NeedIndexer]
        public double delta(double x, string indexer)
        {
            var lastSample = x;
            if (deltaLastSamples.ContainsKey(indexer))
                lastSample = deltaLastSamples[indexer];

            deltaLastSamples[indexer] = x;

            return x - lastSample;
        }

        //[Deprecated("continuousRotation")]
        //[NeedIndexer]
        //public double continousRotation(double x, string indexer)
        //{
        //    return continuousRotation(x, indexer);
        //}

        //[Deprecated("continuousRotation")]
        //[NeedIndexer]
        //public double continousRotation(double x, Units unit, string indexer)
        //{
        //    return continuousRotation(x, unit, indexer);
        //}

        [NeedIndexer]
        public double continuousRotation(double x, string indexer)
        {
            return continuousRotation(x, Units.Radians, indexer);
        }

        [NeedIndexer]
        public double continuousRotation(double x, Units unit, string indexer)
        {
            if (!continousRotationStrategies.ContainsKey(indexer))
                continousRotationStrategies[indexer] = new ContinuesRotationStrategy(unit);

            var strategy = continousRotationStrategies[indexer];
            strategy.Update(x);

            return strategy.Out;
        }
        public double deadband(double x, double deadZone, double minY, double maxY)
        {
            var scaled = ensureMapRange(x, minY, maxY, -1, 1);
            var y = 0d;

            if (Math.Abs(scaled) > deadZone)
                y = ensureMapRange(Math.Abs(scaled), deadZone, 1, 0, 1) * Math.Sign(scaled);

            return ensureMapRange(y, -1, 1, minY, maxY);
        }

        //public double deadband(double x, double deadZone)
        //{
        //    if (Math.Abs(x) >= Math.Abs(deadZone))
        //        return x;

        //    return 0;
        //}
        private double deadband(double x, double deadZoneC, double deadZoneX, double minY, double maxY, double alpha = 0)
        {
            var scaled = ensureMapRange(x, minY, maxY, -1, 1);
            var y = 0d;
            alpha = 1 - alpha / 50;
            deadZoneX = 1 - deadZoneX / 50;
            deadZoneC = deadZoneC / 100; 
            var val = Math.Abs(scaled);
            if (val > deadZoneC && val < deadZoneX)
            {
                y = ensureMapRange(Math.Abs(scaled), deadZoneC, deadZoneX, 0, alpha) * Math.Sign(scaled);
            }
            else
            {
                if (val >= deadZoneX)
                {
                    y = Math.Sign(scaled) * alpha;
                }
            }

            return ensureMapRange(y, -1, 1, minY, maxY);
        }
        public int mapRange(double x, double xMin, double xMax)
        {
            const double yMin = -16383;
            const double yMax = 16383;
            return (int) (yMin + (yMax - yMin) * (x - xMin) / (xMax - xMin));
        }
        public double mapRange(double x, double xMin, double xMax, double yMin = -16383, double yMax = 16383)
        {
            return yMin + (yMax - yMin) * (x - xMin) / (xMax - xMin);
        }
        public double ensureMapRange(double x, double xMin, double xMax, double yMin, double yMax)
        {
            var val = mapRange(x, xMin, xMax, yMin, yMax); // ((x - xMin) / (xMax - xMin))*(yMax - yMin) + yMin;
            if (yMax > yMin)
                return Math.Max(Math.Min(val, yMax), yMin);
            else
                return Math.Max(Math.Min(val, yMin), yMax);
        }
        public double CurveL(double x, double minY, double maxY, double deadZoneC, double deadZoneX, double alpha = 0)
        {
            return deadband(x, deadZoneC, deadZoneX, minY, maxY, alpha) ;
        }

        public int LinearCurve(int x, IList<int> Xpoints, IList<int> Ypoints, int param = 0)
        {
            /*  x € [-16383, 16383] => y € [-16383, 16383]
                for example, points = [0,5, 65,70, 75,70, 100,100] à transformer via VjoyRange
                for x € [0, 65], y € [5, 70]
                for x € [65, 75], y € [70, 70]  (so = 70)
                for x € [75, 100], y € [70, 100]
            */
            int nbpoints = Xpoints.Count;

            for (int i = 0; i < nbpoints; i++)
            {
                if (x == Xpoints[i]) return Ypoints[i];
                if (x < Xpoints[i])
                {
                    int deltaY = Ypoints[i] - Ypoints[i - 1], deltaX = Xpoints[i] - Xpoints[i - 1];
                    double A = deltaY / deltaX;
                    var B = Ypoints[i - 1] - Xpoints[i - 1] * A;
                    var Y = A * x + B;
                    return (int)(Y * (1 - param * 0.1));
                }
            }
            return 0;
        }

        public double LinearCurve(double x, IList<int> Xpoints, IList<int> Ypoints, int xMin, int xMax, int param = 0)
        {
            /*  x € [xMin, xMax] => y € [-16383, 16383]
                return value is a double value
                if xMin == xMax that means: no need to transform the value x, already in range [0, 100]
                if xMin != xMax that means: initially x is in range [xMin, xMax] and
                    before applying the function, we need to transform x in range [0, 100]

                Linear custom curve y = f(x), f = list of points (%)
                for example, points = [0,5, 65,70, 75,70, 100,100]
                for x € [0, 65], y € [5, 70]
                for x € [65, 75], y € [70, 70]  (so = 70)
                for x € [75, 100], y € [70, 100]
            */
            var x0 = mapRange(x, xMin, xMax);
            return LinearCurve(x0, Xpoints, Ypoints, param);
        }

        public List<int> VjoyRange(IList<int> Xpoints, int xMin = -100, int xMax = 100)
        {
            List<int> lst = new List<int>();
            foreach(var point in Xpoints)
                lst.Add(mapRange(point, xMin, xMax));
            return lst;
        }
        // f(x) = x*(param/9)+(x^5)*(9-param)/9
        public int Joy1Curve(double x, int param, int xMin = -16383, int xMax = 16383, int yMin = -16383, int yMax = 16383)
        {
            x = mapRange(x, xMin, xMax, -1, 1);
            double y = param * x / 9 + (9 - param) * (Math.Pow(x, 5)) / 9;
            return (int)mapRange(y, -1, 1, yMin, yMax);
        }
        // f(x) = x^(3-(param/4.5))
        public double Joy2Curve(double x, int param, int xMin = -16383, int xMax = 16383, int yMin = -16383, int yMax = 16383)
        {
            x = mapRange(x, xMin, xMax, -1, 1);
            double power = 3 - (param / 4.5);
            double y = Math.Pow(Math.Abs(x), power) * Math.Sign(x);
            return (int)mapRange(y, -1, 1, yMin, yMax);
        }
        private Func<double, double, double> betterFunction;

        private MethodInfo CreateAndCompileFunction(string filename)
        {
            FileInfo fi = new FileInfo(@".\files_freepie\curves\CurveAssembly.dll");
            CSharpCodeProvider provider = new CSharpCodeProvider();
            CompilerParameters compilerparams = new CompilerParameters();
            compilerparams.OutputAssembly = @".\files_freepie\curves\CurveAssembly.dll";
            CompilerResults results = provider.CompileAssemblyFromFile(compilerparams, filename);

            if (results.Errors.Count > 0)
            {
                // Display compilation errors.
                Console.WriteLine("Errors building {0} into {1}",
                    fi.FullName, results.PathToAssembly);
                foreach (CompilerError ce in results.Errors)
                {
                    Console.WriteLine("  {0}", ce.ToString());
                }
            }
            else
            {
                // Display a successful compilation message.
                Console.WriteLine("Source {0} built into {1} successfully.",
                    fi.FullName, results.PathToAssembly);
            }

            Type binaryFunction = results.CompiledAssembly.GetType("UserFunction.BinaryFunction");
            return binaryFunction.GetMethod("MyFunction");
        }
        public void compileFunction(string curvename)
        {
            MethodInfo function = CreateAndCompileFunction(@".\files_freepie\curves\" + curvename);
            betterFunction = (Func<double, double, double>)Delegate.CreateDelegate(typeof(Func<double, double, double>), function);
        }

        public double MyFunction(double x, double param)
        {
            return betterFunction(x, param);
        }

    }
}
