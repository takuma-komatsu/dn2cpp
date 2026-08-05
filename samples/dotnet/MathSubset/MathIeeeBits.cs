using System;

// The IEEE-exact Math/MathF members: CopySign, IEEERemainder,
// FusedMultiplyAdd, ScaleB, BitIncrement/BitDecrement, ILogB and
// MaxMagnitude/MinMagnitude. Every operation here is exact (bit-defined by
// IEEE 754 or pure bit manipulation), so raw prints are safe; signed zeros
// are observed through the sign of their reciprocal.
namespace MathIeeeBits;

static class Program
{
    internal static void __GateEntry()
    {
        // CopySign — magnitude of x, sign of y; the zero signs transfer too.
        Console.WriteLine(Math.CopySign(3.0, -1.0));    // -3
        Console.WriteLine(Math.CopySign(-3.0, 1.0));    // 3
        Console.WriteLine(Math.CopySign(2.5, -0.0));    // -2.5
        Console.WriteLine(double.IsNegativeInfinity(1.0 / Math.CopySign(0.0, -1.0)));  // True
        Console.WriteLine(MathF.CopySign(2.5f, -0.0f)); // -2.5
        Console.WriteLine(MathF.CopySign(-4.0f, 1.0f)); // 4

        // IEEERemainder — x - y*n with n the nearest integer to x/y, ties to
        // even; a zero result carries x's sign.
        Console.WriteLine(Math.IEEERemainder(3.0, 2.0));    // -1
        Console.WriteLine(Math.IEEERemainder(10.0, 3.0));   // 1
        Console.WriteLine(Math.IEEERemainder(11.0, 3.0));   // -1
        Console.WriteLine(Math.IEEERemainder(2.5, 2.0));    // 0.5
        Console.WriteLine(Math.IEEERemainder(3.5, 2.0));    // -0.5
        Console.WriteLine(Math.IEEERemainder(4.0, double.PositiveInfinity)); // 4
        Console.WriteLine(double.IsNegativeInfinity(1.0 / Math.IEEERemainder(-4.0, 2.0))); // True
        Console.WriteLine(double.IsNaN(Math.IEEERemainder(1.0, 0.0)));       // True
        Console.WriteLine(MathF.IEEERemainder(10.0f, 3.0f));  // 1
        Console.WriteLine(MathF.IEEERemainder(2.5f, 2.0f));   // 0.5

        // FusedMultiplyAdd — x*y + z with a single rounding: recovers the
        // rounding error of a plain product exactly.
        Console.WriteLine(Math.FusedMultiplyAdd(2.0, 3.0, 1.0));    // 7
        Console.WriteLine(Math.FusedMultiplyAdd(-2.0, 3.0, 1.5));   // -4.5
        double a = 1.0 + Math.ScaleB(1.0, -27);
        Console.WriteLine(Math.FusedMultiplyAdd(a, a, -(a * a)) == Math.ScaleB(1.0, -54));  // True
        Console.WriteLine(MathF.FusedMultiplyAdd(2.0f, 3.0f, 1.0f)); // 7
        float b = 1.0f + MathF.ScaleB(1.0f, -12);
        Console.WriteLine(MathF.FusedMultiplyAdd(b, b, -(b * b)) == MathF.ScaleB(1.0f, -24)); // True

        // ScaleB — exact x * 2^n down into the subnormals and out to the
        // overflow/underflow ends.
        Console.WriteLine(Math.ScaleB(1.0, 10));    // 1024
        Console.WriteLine(Math.ScaleB(3.0, -2));    // 0.75
        Console.WriteLine(Math.ScaleB(1.0, -1074) == double.Epsilon);           // True
        Console.WriteLine(double.IsPositiveInfinity(Math.ScaleB(1.0, 2000)));   // True
        Console.WriteLine(Math.ScaleB(1.0, -2000) == 0.0);                      // True
        Console.WriteLine(double.IsNegativeInfinity(1.0 / Math.ScaleB(-0.0, 5))); // True
        Console.WriteLine(MathF.ScaleB(1.5f, 4));   // 24
        Console.WriteLine(MathF.ScaleB(1.0f, -149) == float.Epsilon);           // True

        // BitIncrement/BitDecrement — the adjacent representable value.
        Console.WriteLine(Math.BitIncrement(0.0) == double.Epsilon);    // True
        Console.WriteLine(Math.BitIncrement(-0.0) == double.Epsilon);   // True
        Console.WriteLine(Math.BitDecrement(0.0) == -double.Epsilon);   // True
        Console.WriteLine(Math.BitIncrement(1.0) == 1.0 + Math.ScaleB(1.0, -52));  // True
        Console.WriteLine(Math.BitDecrement(1.0) == 1.0 - Math.ScaleB(1.0, -53));  // True
        Console.WriteLine(Math.BitIncrement(double.NegativeInfinity) == double.MinValue);  // True
        Console.WriteLine(Math.BitDecrement(double.PositiveInfinity) == double.MaxValue);  // True
        Console.WriteLine(double.IsPositiveInfinity(Math.BitIncrement(double.PositiveInfinity))); // True
        Console.WriteLine(double.IsNaN(Math.BitIncrement(double.NaN)));  // True
        Console.WriteLine(MathF.BitIncrement(0.0f) == float.Epsilon);    // True
        Console.WriteLine(MathF.BitDecrement(1.0f) == 1.0f - MathF.ScaleB(1.0f, -24));  // True
        Console.WriteLine(MathF.BitIncrement(float.NegativeInfinity) == float.MinValue); // True

        // ILogB — the unbiased base-2 exponent, with the BCL's pinned special
        // values (0 -> int.MinValue, ±infinity/NaN -> int.MaxValue) and the
        // subnormal range counted below MinExponent.
        Console.WriteLine(Math.ILogB(1.0));     // 0
        Console.WriteLine(Math.ILogB(0.5));     // -1
        Console.WriteLine(Math.ILogB(3.0));     // 1
        Console.WriteLine(Math.ILogB(-8.0));    // 3
        Console.WriteLine(Math.ILogB(1024.0));  // 10
        Console.WriteLine(Math.ILogB(0.0));                        // -2147483648
        Console.WriteLine(Math.ILogB(-0.0));                       // -2147483648
        Console.WriteLine(Math.ILogB(double.NaN));                 // 2147483647
        Console.WriteLine(Math.ILogB(double.PositiveInfinity));    // 2147483647
        Console.WriteLine(Math.ILogB(double.NegativeInfinity));    // 2147483647
        Console.WriteLine(Math.ILogB(double.Epsilon));             // -1074
        Console.WriteLine(Math.ILogB(Math.ScaleB(1.0, -1050)));    // -1050
        Console.WriteLine(Math.ILogB(Math.BitDecrement(Math.ScaleB(1.0, -1022)))); // -1023
        Console.WriteLine(MathF.ILogB(1.0f));           // 0
        Console.WriteLine(MathF.ILogB(-6.0f));          // 2
        Console.WriteLine(MathF.ILogB(0.0f));           // -2147483648
        Console.WriteLine(MathF.ILogB(float.NaN));      // 2147483647
        Console.WriteLine(MathF.ILogB(float.Epsilon));  // -149
        Console.WriteLine(MathF.ILogB(MathF.ScaleB(1.0f, -130)));  // -130

        // MaxMagnitude/MinMagnitude — larger/smaller absolute value; NaN
        // propagates; on equal magnitudes Max prefers positive, Min negative.
        Console.WriteLine(Math.MaxMagnitude(2.0, -3.0));    // -3
        Console.WriteLine(Math.MinMagnitude(2.0, -3.0));    // 2
        Console.WriteLine(Math.MaxMagnitude(-2.0, 2.0));    // 2
        Console.WriteLine(Math.MinMagnitude(-2.0, 2.0));    // -2
        Console.WriteLine(double.IsPositiveInfinity(1.0 / Math.MaxMagnitude(-0.0, 0.0)));  // True
        Console.WriteLine(double.IsNegativeInfinity(1.0 / Math.MinMagnitude(-0.0, 0.0)));  // True
        Console.WriteLine(double.IsNaN(Math.MaxMagnitude(double.NaN, 1.0)));   // True
        Console.WriteLine(double.IsNaN(Math.MinMagnitude(1.0, double.NaN)));   // True
        Console.WriteLine(MathF.MaxMagnitude(-4.0f, 3.0f)); // -4
        Console.WriteLine(MathF.MinMagnitude(-4.0f, 3.0f)); // 3
    }
}
