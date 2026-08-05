using System;

// The System.Double / System.Single static generic-math surface: the members
// shared with Math/MathF (double.Sqrt == Math.Sqrt) plus the Double/Single-only
// names — the exp/log compositions (ExpM1/Exp2/Exp10/LogP1/... are compositions
// of the base libm functions in .NET, so the native output bit-matches and raw
// prints are safe — verified value-by-value against real .NET on this host),
// the *Pi angle family, DegreesToRadians/RadiansToDegrees, Lerp and
// MultiplyAddEstimate (fused fma on arm64 .NET), Ieee754Remainder, the
// NaN-dropping *Number min-max variants, the platform-defined *Native variants
// (only unambiguous finite results asserted — real arm64 .NET answers
// fmax/fmin for these, which NaN-propagates where a plain ternary does not),
// the IFloatingPointConstants (Pi/E/Tau) and the full IsXxx predicate set.
// NaN/Infinity RESULTS are asserted with predicates so the output is symbol-
// independent. Sign-of-NaN tests pin the sign with CopySign first: .NET's bare
// double.NaN constant carries the sign bit while the native NAN macro the
// transpiler maps that constant to does not, and CopySign makes both sides
// agree; sign-insensitive predicates (IsNaN/IsFinite/...) use the bare
// constant freely.
namespace DoubleSingleStatics;

static class Program
{
    internal static void __GateEntry()
    {
        // ---- Pass-through members (same map as Math/MathF) ----
        Console.WriteLine(double.Sqrt(2.0) == Math.Sqrt(2.0));  // True
        Console.WriteLine(double.Sqrt(2.25));                   // 1.5
        Console.WriteLine(float.Sqrt(2.25f));                   // 1.5
        Console.WriteLine(double.Abs(-3.25));                   // 3.25
        Console.WriteLine(float.Abs(-2.5f));                    // 2.5
        Console.WriteLine(double.Cbrt(27.0));                   // 3
        Console.WriteLine(double.Exp(1.0) == Math.Exp(1.0));    // True
        Console.WriteLine(double.Log(8.0, 2.0));                // 3
        Console.WriteLine(double.Sin(0.5) == Math.Sin(0.5));    // True
        Console.WriteLine(float.Atan2(1.0f, 1.0f) == MathF.Atan2(1.0f, 1.0f)); // True
        Console.WriteLine(double.Floor(-1.5));                  // -2
        Console.WriteLine(double.Round(2.5));                   // 2
        Console.WriteLine(double.Round(2.5, 0, MidpointRounding.AwayFromZero)); // 3
        Console.WriteLine(double.Round(-2.5, 0, MidpointRounding.AwayFromZero)); // -3
        Console.WriteLine(float.Round(2.5f, 0, MidpointRounding.AwayFromZero)); // 3
        Console.WriteLine(double.Truncate(-7.9));               // -7
        Console.WriteLine(double.Sign(-5.5));                   // -1
        Console.WriteLine(double.Clamp(5.0, 0.0, 2.0));         // 2
        Console.WriteLine(float.Clamp(-1.0f, 0.0f, 2.0f));      // 0
        Console.WriteLine(double.CopySign(3.0, -1.0));          // -3
        Console.WriteLine(double.FusedMultiplyAdd(2.0, 3.0, 1.0)); // 7
        var (s, c) = double.SinCos(0.5);
        Console.WriteLine(s == Math.Sin(0.5) && c == Math.Cos(0.5)); // True

        // ILogB — the BCL's pinned special values survive the double./float. route.
        Console.WriteLine(double.ILogB(1024.0));                // 10
        Console.WriteLine(double.ILogB(0.0));                   // -2147483648
        Console.WriteLine(double.ILogB(double.NaN));            // 2147483647
        Console.WriteLine(double.ILogB(double.Epsilon));        // -1074
        Console.WriteLine(float.ILogB(0.0f));                   // -2147483648
        Console.WriteLine(float.ILogB(-6.0f));                  // 2

        // Min/Max — IEEE 754:2019 minimum/maximum: NaN propagates, -0 < +0.
        Console.WriteLine(double.IsNaN(double.Min(double.NaN, 1.0)));  // True
        Console.WriteLine(double.IsNaN(double.Max(1.0, double.NaN)));  // True
        Console.WriteLine(float.IsNaN(float.Min(float.NaN, 1.0f)));    // True
        Console.WriteLine(float.IsNaN(float.Max(1.0f, float.NaN)));    // True
        Console.WriteLine(double.IsNegativeInfinity(1.0 / double.Min(-0.0, 0.0)));  // True
        Console.WriteLine(double.IsPositiveInfinity(1.0 / double.Max(0.0, -0.0)));  // True
        Console.WriteLine(float.IsNegativeInfinity(1.0f / float.Min(0.0f, -0.0f))); // True
        Console.WriteLine(float.IsPositiveInfinity(1.0f / float.Max(-0.0f, 0.0f))); // True
        Console.WriteLine(double.Min(2.0, 3.0));  // 2
        Console.WriteLine(float.Max(2.0f, 3.0f)); // 3
        Console.WriteLine(double.MaxMagnitude(2.0, -3.0));  // -3
        Console.WriteLine(float.MinMagnitude(-4.0f, 3.0f)); // 3

        // ---- IFloatingPointConstants ----
        Console.WriteLine(double.Pi);    // 3.141592653589793
        Console.WriteLine(double.E);     // 2.718281828459045
        Console.WriteLine(double.Tau);   // 6.283185307179586
        Console.WriteLine(float.Pi);     // 3.1415927
        Console.WriteLine(float.E);      // 2.7182817
        Console.WriteLine(float.Tau);    // 6.2831855

        // ---- The exp/log compositions (exact — same libm, same composition) ----
        Console.WriteLine(double.ExpM1(0.5));
        Console.WriteLine(double.ExpM1(-0.9));
        Console.WriteLine(float.ExpM1(0.5f));
        Console.WriteLine(float.ExpM1(2.3f));
        Console.WriteLine(double.Exp2(0.5));
        Console.WriteLine(double.Exp2(10.0));    // 1024
        Console.WriteLine(float.Exp2(10.0f));    // 1024
        Console.WriteLine(double.Exp2M1(3.0));   // 7
        Console.WriteLine(double.Exp2M1(0.5));
        Console.WriteLine(double.Exp10(2.0));    // 100
        Console.WriteLine(double.Exp10(0.5));
        Console.WriteLine(float.Exp10(0.1f));
        Console.WriteLine(double.Exp10M1(2.0));  // 99
        Console.WriteLine(double.Exp10M1(0.5));
        Console.WriteLine(float.Exp2M1(2.0f));   // 3
        Console.WriteLine(float.Exp10M1(1.0f));  // 9
        Console.WriteLine(double.LogP1(0.0));    // 0
        Console.WriteLine(double.LogP1(0.5));
        Console.WriteLine(double.LogP1(2.3));
        Console.WriteLine(double.IsNegativeInfinity(double.LogP1(-1.0))); // True
        Console.WriteLine(double.Log2P1(3.0));   // 2
        Console.WriteLine(double.Log2P1(0.5));
        Console.WriteLine(double.Log10P1(99.0)); // 2
        Console.WriteLine(double.Log10P1(-0.9)); // -1
        Console.WriteLine(float.LogP1(1.25f));
        Console.WriteLine(float.Log2P1(1.25f));
        Console.WriteLine(float.Log10P1(9.0f));  // 1

        // ---- The *Pi angle family (libm result / Pi, in-precision) ----
        Console.WriteLine(double.AsinPi(0.5));           // 0.16666666666666666
        Console.WriteLine(double.AsinPi(1.0));           // 0.5
        Console.WriteLine(double.AcosPi(-0.5));          // 0.6666666666666667
        Console.WriteLine(double.AcosPi(1.0));           // 0
        Console.WriteLine(double.AtanPi(1.0));           // 0.25
        Console.WriteLine(double.AtanPi(-0.9));
        Console.WriteLine(double.Atan2Pi(1.0, 2.0));
        Console.WriteLine(double.Atan2Pi(-3.0, 0.7));
        Console.WriteLine(float.AsinPi(0.5f));           // 0.16666667
        Console.WriteLine(float.AcosPi(0.5f));           // 0.33333334
        Console.WriteLine(float.AtanPi(2.3f));
        Console.WriteLine(float.Atan2Pi(1.0f, 2.0f));

        // ---- Degrees/radians (multiply first, then divide — the BCL's order) ----
        Console.WriteLine(double.DegreesToRadians(180.0));  // 3.141592653589793
        Console.WriteLine(double.DegreesToRadians(90.0));   // 1.5707963267948966
        Console.WriteLine(double.DegreesToRadians(12.34));
        Console.WriteLine(double.RadiansToDegrees(Math.PI)); // 180
        Console.WriteLine(double.RadiansToDegrees(2.3));
        Console.WriteLine(float.DegreesToRadians(0.5f));
        Console.WriteLine(float.RadiansToDegrees(2.3f));

        // ---- Lerp / MultiplyAddEstimate (fused fma, deterministic here) ----
        Console.WriteLine(double.Lerp(2.0, 10.0, 0.5));     // 6
        Console.WriteLine(double.Lerp(2.0, 10.0, 0.0));     // 2
        Console.WriteLine(double.Lerp(2.0, 10.0, 1.0));     // 10
        Console.WriteLine(double.Lerp(1.25, 7.75, 0.3));    // 3.1999999999999997
        Console.WriteLine(float.Lerp(1.25f, 7.75f, 0.3f));  // 3.2
        Console.WriteLine(double.MultiplyAddEstimate(1.1, 2.3, 4.5));    // 7.03
        Console.WriteLine(float.MultiplyAddEstimate(1.1f, 2.3f, 4.5f));  // 7.03
        double fa = 1.0 + Math.ScaleB(1.0, -27);
        Console.WriteLine(double.MultiplyAddEstimate(fa, fa, -(fa * fa)) == Math.ScaleB(1.0, -54)); // True (fused)

        // ---- Ieee754Remainder (= Math.IEEERemainder) ----
        Console.WriteLine(double.Ieee754Remainder(10.1, 3.0));  // 1.0999999999999996
        Console.WriteLine(double.Ieee754Remainder(3.0, 2.0));   // -1
        Console.WriteLine(float.Ieee754Remainder(10.0f, 3.0f)); // 1
        Console.WriteLine(double.IsNaN(double.Ieee754Remainder(1.0, 0.0))); // True

        // ---- The *Number variants: a NaN operand is DROPPED ----
        Console.WriteLine(double.MaxNumber(double.NaN, 1.0));   // 1
        Console.WriteLine(double.MaxNumber(1.0, double.NaN));   // 1
        Console.WriteLine(double.MinNumber(double.NaN, 2.5));   // 2.5
        Console.WriteLine(double.MinNumber(2.5, double.NaN));   // 2.5
        Console.WriteLine(double.IsNaN(double.MaxNumber(double.NaN, double.NaN))); // True
        Console.WriteLine(double.MaxNumber(2.0, 3.0));          // 3
        Console.WriteLine(double.MinNumber(-2.0, 3.0));         // -2
        Console.WriteLine(double.IsPositiveInfinity(1.0 / double.MaxNumber(-0.0, 0.0))); // True
        Console.WriteLine(double.IsNegativeInfinity(1.0 / double.MinNumber(-0.0, 0.0))); // True
        Console.WriteLine(double.MaxMagnitudeNumber(2.0, -3.0));         // -3
        Console.WriteLine(double.MaxMagnitudeNumber(double.NaN, 1.0));   // 1
        Console.WriteLine(double.MinMagnitudeNumber(-2.0, 2.0));         // -2
        Console.WriteLine(double.MinMagnitudeNumber(1.0, double.NaN));   // 1
        Console.WriteLine(float.MaxNumber(float.NaN, 1.5f));    // 1.5
        Console.WriteLine(float.MinNumber(1.5f, float.NaN));    // 1.5
        Console.WriteLine(float.MaxMagnitudeNumber(-4.0f, 3.0f)); // -4
        Console.WriteLine(float.MinMagnitudeNumber(-4.0f, 3.0f)); // 3

        // ---- The *Native variants: NaN and ±0 are platform-defined in .NET,
        // so only the unambiguous finite results are asserted. ----
        Console.WriteLine(double.MaxNative(2.0, 3.0));     // 3
        Console.WriteLine(double.MinNative(-5.0, -7.0));   // -7
        Console.WriteLine(float.MaxNative(2.0f, 3.0f));    // 3
        Console.WriteLine(float.MinNative(2.0f, 3.0f));    // 2
        Console.WriteLine(double.ClampNative(5.0, 0.0, 2.0));   // 2
        Console.WriteLine(double.ClampNative(-1.0, 0.0, 2.0));  // 0
        Console.WriteLine(double.ClampNative(1.0, 0.0, 2.0));   // 1
        Console.WriteLine(float.ClampNative(5.0f, 0.0f, 2.0f)); // 2
        try
        {
            Console.WriteLine(double.ClampNative(1.0, 3.0, 2.0));
        }
        catch (ArgumentException)
        {
            Console.WriteLine("ClampNative min>max throws");
        }

        // ---- The IsXxx predicate truth table ----
        double nnan = double.CopySign(double.NaN, -1.0);  // sign-bit NaN
        double pnan = double.CopySign(double.NaN, 1.0);   // positive NaN
        double two52 = 4503599627370496.0;                // 2^52
        double odd53 = 9007199254740991.0;                // 2^53 - 1

        Console.WriteLine(double.IsFinite(1.0));                     // True
        Console.WriteLine(double.IsFinite(double.MaxValue));         // True
        Console.WriteLine(double.IsFinite(double.PositiveInfinity)); // False
        Console.WriteLine(double.IsFinite(double.NaN));              // False
        Console.WriteLine(double.IsFinite(double.Epsilon));          // True

        Console.WriteLine(double.IsInfinity(double.PositiveInfinity)); // True
        Console.WriteLine(double.IsInfinity(double.NegativeInfinity)); // True
        Console.WriteLine(double.IsInfinity(double.NaN));               // False
        Console.WriteLine(double.IsInfinity(double.MaxValue));          // False

        Console.WriteLine(double.IsNegative(-0.0));   // True
        Console.WriteLine(double.IsNegative(0.0));    // False
        Console.WriteLine(double.IsNegative(-2.0));   // True
        Console.WriteLine(double.IsNegative(double.NegativeInfinity)); // True
        Console.WriteLine(double.IsNegative(nnan));   // True (sign bit, not comparison)
        Console.WriteLine(double.IsNegative(pnan));   // False
        Console.WriteLine(double.IsPositive(0.0));    // True
        Console.WriteLine(double.IsPositive(-0.0));   // False
        Console.WriteLine(double.IsPositive(pnan));   // True (sign bit, not comparison)
        Console.WriteLine(double.IsPositive(nnan));   // False

        Console.WriteLine(double.IsNormal(1.0));              // True
        Console.WriteLine(double.IsNormal(0.0));              // False
        Console.WriteLine(double.IsNormal(double.Epsilon));   // False
        Console.WriteLine(double.IsNormal(double.MaxValue));  // True
        Console.WriteLine(double.IsNormal(double.PositiveInfinity)); // False
        Console.WriteLine(double.IsNormal(double.NaN));       // False

        Console.WriteLine(double.IsSubnormal(double.Epsilon));   // True
        Console.WriteLine(double.IsSubnormal(-double.Epsilon));  // True
        Console.WriteLine(double.IsSubnormal(0.0));              // False
        Console.WriteLine(double.IsSubnormal(1.0));              // False
        Console.WriteLine(double.IsSubnormal(double.NaN));       // False

        Console.WriteLine(double.IsInteger(2.0));     // True
        Console.WriteLine(double.IsInteger(-3.0));    // True
        Console.WriteLine(double.IsInteger(0.5));     // False
        Console.WriteLine(double.IsInteger(two52));   // True
        Console.WriteLine(double.IsInteger(double.PositiveInfinity)); // False
        Console.WriteLine(double.IsInteger(double.NaN));              // False

        Console.WriteLine(double.IsEvenInteger(2.0));    // True
        Console.WriteLine(double.IsEvenInteger(3.0));    // False
        Console.WriteLine(double.IsEvenInteger(-4.0));   // True
        Console.WriteLine(double.IsEvenInteger(0.0));    // True
        Console.WriteLine(double.IsEvenInteger(-0.0));   // True
        Console.WriteLine(double.IsEvenInteger(0.5));    // False
        Console.WriteLine(double.IsEvenInteger(two52));  // True
        Console.WriteLine(double.IsEvenInteger(odd53));  // False
        Console.WriteLine(double.IsOddInteger(3.0));     // True
        Console.WriteLine(double.IsOddInteger(-3.0));    // True
        Console.WriteLine(double.IsOddInteger(2.0));     // False
        Console.WriteLine(double.IsOddInteger(0.5));     // False
        Console.WriteLine(double.IsOddInteger(odd53));   // True
        Console.WriteLine(double.IsOddInteger(double.PositiveInfinity)); // False

        Console.WriteLine(double.IsRealNumber(1.0));                     // True
        Console.WriteLine(double.IsRealNumber(double.PositiveInfinity)); // True
        Console.WriteLine(double.IsRealNumber(double.NaN));              // False

        Console.WriteLine(double.IsPow2(4.0));            // True
        Console.WriteLine(double.IsPow2(1.0));            // True
        Console.WriteLine(double.IsPow2(0.5));            // True
        Console.WriteLine(double.IsPow2(3.0));            // False
        Console.WriteLine(double.IsPow2(-4.0));           // False
        Console.WriteLine(double.IsPow2(0.0));            // False
        Console.WriteLine(double.IsPow2(double.Epsilon)); // True (single subnormal bit)
        Console.WriteLine(double.IsPow2(double.Epsilon * 3));         // False
        Console.WriteLine(double.IsPow2(double.PositiveInfinity));    // False
        Console.WriteLine(double.IsPow2(double.NaN));                 // False

        // float mirrors of the same predicates.
        float fnnan = float.CopySign(float.NaN, -1.0f);
        Console.WriteLine(float.IsFinite(1.0f));                    // True
        Console.WriteLine(float.IsFinite(float.NaN));               // False
        Console.WriteLine(float.IsInfinity(float.NegativeInfinity)); // True
        Console.WriteLine(float.IsNegative(-0.0f));                 // True
        Console.WriteLine(float.IsNegative(fnnan));                 // True
        Console.WriteLine(float.IsPositive(0.0f));                  // True
        Console.WriteLine(float.IsNormal(1.0f));                    // True
        Console.WriteLine(float.IsNormal(float.Epsilon));           // False
        Console.WriteLine(float.IsSubnormal(float.Epsilon));        // True
        Console.WriteLine(float.IsSubnormal(1.0f));                 // False
        Console.WriteLine(float.IsInteger(2.0f));                   // True
        Console.WriteLine(float.IsInteger(0.5f));                   // False
        Console.WriteLine(float.IsEvenInteger(2.0f));               // True
        Console.WriteLine(float.IsEvenInteger(16777215.0f));        // False (2^24 - 1)
        Console.WriteLine(float.IsOddInteger(16777215.0f));         // True
        Console.WriteLine(float.IsOddInteger(2.0f));                // False
        Console.WriteLine(float.IsRealNumber(float.NaN));           // False
        Console.WriteLine(float.IsPow2(8.0f));                      // True
        Console.WriteLine(float.IsPow2(float.Epsilon));             // True
        Console.WriteLine(float.IsPow2(float.Epsilon * 3));         // False
        Console.WriteLine(float.IsPow2(-8.0f));                     // False
    }
}
