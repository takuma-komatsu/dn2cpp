// Default double/float ToString is the shortest round-trippable representation,
// matching .NET's invariant "G" formatting — both the shortest-digit selection and
// the fixed/scientific switch (scientific when the decimal exponent is < -4 or >= 17
// for double / >= 9 for float; exponent printed E±dd). Culture-aware / IFormatProvider
// formatting and explicit format specifiers are out of scope; this covers the default
// ToString / Console / StringBuilder / concat path only. No explicit CultureInfo is
// used (it pulls unsupported culture IL); dn2cpp's default path is invariant, so plain
// ToString diffs clean vs real .NET.

using System;
using System.Text;

namespace FloatRoundtripSubset
{
    internal static class Program
    {
        internal static void __GateEntry()
        {
            // Doubles: shortest digits, fixed/scientific switch, signed zero, extremes.
            double[] ds =
            {
                0.0, 1.0, -1.0, 1.5, 0.1, 1.0 / 3.0, 2.3333333333333335,
                123.456, 123456789.0, 12345678901234567.0,
                1e15, 1e16, 1e17, 1e-4, 1e-5, 1e21, 5e-324, -0.0,
                double.MaxValue, double.MinValue, double.Epsilon,
                3.14159265358979,
            };
            foreach (double d in ds)
                Console.WriteLine(d.ToString());

            Console.WriteLine("--floats--");
            // Floats: round-trip at float precision (1f/3f is "0.33333334", not the
            // double-widened value), and the float scientific threshold (>= 1e9).
            float[] fs =
            {
                0f, 1.5f, 0.1f, 1f / 3f, 123456.79f,
                1e8f, 1e9f, 1e-4f, 1e-5f, 1e20f, 3.14159f,
            };
            foreach (float f in fs)
                Console.WriteLine(f.ToString());

            // (NaN / ±Infinity are deliberately omitted: their symbols are
            // culture-sensitive in real .NET — the default ToString uses CurrentCulture,
            // which renders "∞" on a non-invariant machine — whereas dn2cpp is
            // invariant-only by design and emits NaN / Infinity / -Infinity.)

            Console.WriteLine("--console direct (R8/R4 split)--");
            Console.WriteLine(1.0 / 3.0);   // double overload
            Console.WriteLine(1f / 3f);     // float overload -> float precision
            Console.WriteLine(2.5);
            Console.WriteLine(100000.0f);

            Console.WriteLine("--stringbuilder + concat--");
            StringBuilder sb = new StringBuilder();
            sb.Append(1.0 / 3.0).Append('|').Append(1f / 3f);
            Console.WriteLine(sb.ToString());
            Console.WriteLine("d=" + (1.0 / 3.0) + " f=" + (1f / 3f));
        }
    }
}
