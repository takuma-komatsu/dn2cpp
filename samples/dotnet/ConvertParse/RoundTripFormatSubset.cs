using System;

// The explicit "R"/"r" round-trip specifier on double and float: always the
// shortest round-trippable form, with any precision digits accepted and IGNORED
// ("R7" == "R" — .NET's documented round-trip behavior). float must stay at
// float precision even with a precision digit present (1f/3f under "R7" is
// "0.33333334", not the double-widened 17-digit form). Covers subnormals,
// signed zero, and values needing the full 17 (double) / 9 (float) digits.
// NaN / ±Infinity stay omitted (culture-sensitive symbols in real .NET).
namespace RoundTripFormatSubset
{
    internal static class Program
    {
        internal static void __GateEntry()
        {
            Console.WriteLine("-- round-trip R/r --");
            double[] ds =
            {
                0.0, -0.0, 1.0, -1.5, 0.1, 1.0 / 3.0, 2.3333333333333335,
                123.456, 12345678901234567.0, 1e16, 1e17, 1e-4, 1e-5,
                5e-324, 1e-323, 4.9406564584124654e-310,
                double.Epsilon, double.MaxValue, double.MinValue,
                3.141592653589793, 1.7976931348623157e308,
            };
            string[] fmts = { "R", "r", "R7", "r7", "R0", "R17", "r2" };
            foreach (string f in fmts)
                foreach (double d in ds)
                    Console.WriteLine($"d [{f}] {d.ToString(f)}");

            Console.WriteLine("-- float R/r --");
            float[] fs =
            {
                0f, -0f, 1.5f, 0.1f, 1f / 3f, 123456.79f,
                1e8f, 1e9f, 1e-4f, 1e-5f, 1e-40f, 1.4e-45f,
                float.Epsilon, float.MaxValue, float.MinValue,
                16777216f, 1.00000012f, 3.14159265f,
            };
            // "G"/"g" without precision share the float shortest-round-trip path
            // (lowercase "g" also lowercases the exponent marker) — pinned here too.
            string[] ffmts = { "R", "r", "R7", "r7", "R9", "r0", "G", "g" };
            foreach (string f in ffmts)
                foreach (float v in fs)
                    Console.WriteLine($"f [{f}] {v.ToString(f)}");
        }
    }
}
