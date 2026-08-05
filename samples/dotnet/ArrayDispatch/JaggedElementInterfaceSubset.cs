#nullable disable
using System;
using System.Linq;

// A jagged array's ELEMENT flowing straight into a collection-interface
// parameter: `data[0].Select(...)` loads the inner float[] with ldelem.ref,
// whose pushed entry must keep the element's static type — that static type is
// what the call-arg coercion boundary reads to wire the inner array's SZArray
// interface-dispatch map. Losing it compiled and linked fine and aborted at
// run time on the first IEnumerable<T> dispatch inside the LINQ iterator
// (CriWare's WaveView PCM waveform was the real-world site). Both shapes:
// the plain local jagged array, and the closure-field variant the real site
// used (the jagged array captured by a lambda, read via ldfld).

namespace JaggedElementInterfaceSubset
{
    internal struct Pt
    {
        public float X, Y;

        public Pt(float x, float y)
        {
            X = x;
            Y = y;
        }
    }

    internal static class Program
    {
        internal static void Run()
        {
            // ldloc (jagged) -> ldelem.ref -> call-arg IEnumerable<float>.
            float[][] data = new float[1][];
            data[0] = new float[] { 0.1f, 0.2f, 0.3f };
            var pts = data[0].Select((value, index) => new Pt(index, value)).ToArray();
            Console.WriteLine(pts.Length);
            Console.WriteLine(pts[2].X);
            Console.WriteLine(pts[2].Y);

            // The closure-field twin: ldfld (display class) -> ldelem.ref ->
            // call-arg, with a captured scale so the lambda is a real closure.
            double[][] wave = null;
            float scale = 2f;
            Action apply = () =>
            {
                var wpts = wave[0].Select((value, index) => new Pt(index * scale, (float)value)).ToArray();
                Console.WriteLine(wpts.Length);
                Console.WriteLine(wpts[1].X);
                Console.WriteLine(wpts[1].Y);
            };
            wave = new double[1][];
            wave[0] = new double[] { 0.5, 0.25 };
            apply();
        }
    }
}
