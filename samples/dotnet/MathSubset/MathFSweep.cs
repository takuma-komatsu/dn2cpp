using System;

// Systematic System.MathF sweep — the first dedicated single-precision pass
// over the whole MathF surface. The focus is float-precision discipline:
// every arm must compute in float end to end, and the divergence traps below
// print values where a compute-in-double-then-narrow shortcut yields a
// DIFFERENT float than real .NET's float path (Round(2.675f, 2) scales to
// exactly 267.5f in float but 267.4999... in double; FusedMultiplyAdd
// recovers product bits a separate mul+add rounds away). IEEE-exact members
// print raw (shortest-roundtrip float formatting, like .NET); transcendentals
// are asserted only through boolean anchors at exact points and tolerance
// relations (libm and the BCL may differ in the last ulp). Complements the
// MathF asserts scattered through the other sections — Min/Max NaN and
// signed-zero ordering (MathMinMaxSemantics), Sign NaN/zero
// (MathExceptionSemantics), Round digits/modes and their throws
// (MathRoundDigits), the CopySign/IEEERemainder/FMA/ScaleB/BitIncrement/
// ILogB/MinMagnitude basics (MathIeeeBits), the inverse hyperbolics
// (MathHyperbolicInverse) and the estimates (MathEstimates) — without
// duplicating them. MathF has no Clamp or DivRem; the float Clamp overload
// lives on Math and is swept here.
namespace MathFSweep;

static class Program
{
    internal static void __GateEntry()
    {
        // The constants — compiler-inlined float literals, printed exactly.
        Console.WriteLine(MathF.E);    // 2.7182817
        Console.WriteLine(MathF.PI);   // 3.1415927
        Console.WriteLine(MathF.Tau);  // 6.2831855

        // Abs — exact; -0 flips to +0 (observed via the reciprocal's sign).
        Console.WriteLine(MathF.Abs(-3.75f));                                  // 3.75
        Console.WriteLine(MathF.Abs(3.75f));                                   // 3.75
        Console.WriteLine(float.IsPositiveInfinity(1f / MathF.Abs(-0.0f)));    // True
        Console.WriteLine(float.IsPositiveInfinity(MathF.Abs(float.NegativeInfinity))); // True
        Console.WriteLine(float.IsNaN(MathF.Abs(float.NaN)));                  // True

        // Sign — the regular paths MathExceptionSemantics doesn't cover.
        Console.WriteLine(MathF.Sign(5.5f));                     // 1
        Console.WriteLine(MathF.Sign(-0.0f));                    // 0
        Console.WriteLine(MathF.Sign(float.PositiveInfinity));   // 1
        Console.WriteLine(MathF.Sign(float.NegativeInfinity));   // -1

        // Min/Max — exact finite prints (the NaN/signed-zero semantics live in
        // MathMinMaxSemantics); subnormals order like any other magnitude.
        Console.WriteLine(MathF.Min(1.25f, -7.5f));                    // -7.5
        Console.WriteLine(MathF.Max(1.25f, -7.5f));                    // 1.25
        Console.WriteLine(MathF.Max(float.Epsilon, 0f) == float.Epsilon);  // True

        // MinMagnitude/MaxMagnitude — NaN propagation and the equal-magnitude
        // tie (Max prefers positive, Min negative), float-side.
        Console.WriteLine(MathF.MaxMagnitude(-2f, 2f));   // 2
        Console.WriteLine(MathF.MinMagnitude(-2f, 2f));   // -2
        Console.WriteLine(float.IsPositiveInfinity(1f / MathF.MaxMagnitude(-0.0f, 0.0f)));  // True
        Console.WriteLine(float.IsNegativeInfinity(1f / MathF.MinMagnitude(-0.0f, 0.0f)));  // True
        Console.WriteLine(float.IsNaN(MathF.MaxMagnitude(float.NaN, 1f)));  // True
        Console.WriteLine(float.IsNaN(MathF.MinMagnitude(1f, float.NaN)));  // True

        // CopySign — the special-value transfers MathIeeeBits doesn't cover.
        Console.WriteLine(float.IsNaN(MathF.CopySign(float.NaN, -1f)));  // True
        Console.WriteLine(float.IsNegativeInfinity(MathF.CopySign(float.PositiveInfinity, -1f)));  // True
        Console.WriteLine(float.IsNegativeInfinity(1f / MathF.CopySign(0f, -1f)));  // True

        // ScaleB — the overflow/underflow ends and the subnormal range (a
        // subnormal prints shortest-roundtrip like any float).
        Console.WriteLine(MathF.ScaleB(1.5f, -140));                            // 1.076E-42
        Console.WriteLine(float.IsPositiveInfinity(MathF.ScaleB(1f, 200)));     // True
        Console.WriteLine(float.IsPositiveInfinity(MathF.ScaleB(float.MaxValue, 1)));  // True
        Console.WriteLine(MathF.ScaleB(1f, -200) == 0f);                        // True
        Console.WriteLine(float.IsNegativeInfinity(1f / MathF.ScaleB(-1f, -200)));  // True
        Console.WriteLine(float.IsNegativeInfinity(1f / MathF.ScaleB(-0.0f, 5)));   // True

        // ILogB — the float range ends and the normal/subnormal boundary.
        Console.WriteLine(MathF.ILogB(float.MaxValue));            // 127
        Console.WriteLine(MathF.ILogB(1024f));                     // 10
        Console.WriteLine(MathF.ILogB(MathF.BitDecrement(MathF.ScaleB(1f, -126))));  // -127
        Console.WriteLine(MathF.ILogB(float.NegativeInfinity));    // 2147483647

        // BitIncrement/BitDecrement — the float.Epsilon boundaries around
        // zero and the infinity ends (the basics live in MathIeeeBits).
        Console.WriteLine(MathF.BitIncrement(-0.0f) == float.Epsilon);   // True
        Console.WriteLine(MathF.BitDecrement(0.0f) == -float.Epsilon);   // True
        Console.WriteLine(MathF.BitDecrement(-0.0f) == -float.Epsilon);  // True
        Console.WriteLine(MathF.BitIncrement(-float.Epsilon) == 0f);     // True
        Console.WriteLine(float.IsNegativeInfinity(1f / MathF.BitIncrement(-float.Epsilon)));  // True (-Epsilon steps to -0)
        Console.WriteLine(MathF.BitIncrement(1f) == 1f + MathF.ScaleB(1f, -23));  // True
        Console.WriteLine(MathF.BitDecrement(float.PositiveInfinity) == float.MaxValue);  // True
        Console.WriteLine(float.IsPositiveInfinity(MathF.BitIncrement(float.MaxValue)));  // True
        Console.WriteLine(float.IsNaN(MathF.BitIncrement(float.NaN)));   // True

        // FusedMultiplyAdd — single-rounding traps. 1e8f² is exactly 1e16,
        // so the fused form recovers 1e16 - (float)1e16 while the separate
        // float mul+add cancels to 0; 4097f² = 16785409 keeps its low bit
        // only when fused.
        Console.WriteLine(MathF.FusedMultiplyAdd(1e8f, 1e8f, -1e16f));   // -272564220
        Console.WriteLine(1e8f * 1e8f - 1e16f);                          // 0
        Console.WriteLine(MathF.FusedMultiplyAdd(4097f, 4097f, -16777216f));  // 8193
        Console.WriteLine(4097f * 4097f - 16777216f);                    // 8192

        // Sqrt — correctly rounded by IEEE, so even non-exact inputs print
        // exactly; -0 passes through with its sign, negatives are NaN.
        Console.WriteLine(MathF.Sqrt(9f));                // 3
        Console.WriteLine(MathF.Sqrt(1024f));             // 32
        Console.WriteLine(MathF.Sqrt(2f));                // 1.4142135
        Console.WriteLine(MathF.Sqrt(0.5f));              // 0.70710677
        Console.WriteLine(MathF.Sqrt(float.MaxValue));    // 1.8446743E+19
        Console.WriteLine(float.IsNaN(MathF.Sqrt(-1f)));  // True
        Console.WriteLine(float.IsNegativeInfinity(1f / MathF.Sqrt(-0.0f)));  // True

        // Floor/Ceiling/Truncate/Round(x) — exact, including the signed-zero
        // results a negative fraction collapses to.
        Console.WriteLine(MathF.Floor(2.9f));      // 2
        Console.WriteLine(MathF.Floor(-2.1f));     // -3
        Console.WriteLine(MathF.Ceiling(2.1f));    // 3
        Console.WriteLine(MathF.Ceiling(-2.9f));   // -2
        Console.WriteLine(MathF.Truncate(3.99f));  // 3
        Console.WriteLine(MathF.Truncate(-3.99f)); // -3
        Console.WriteLine(MathF.Round(2.5f));      // 2 (ties to even)
        Console.WriteLine(MathF.Round(3.5f));      // 4
        Console.WriteLine(MathF.Round(-2.5f));     // -2
        Console.WriteLine(float.IsNegativeInfinity(1f / MathF.Round(-0.4f)));     // True
        Console.WriteLine(float.IsNegativeInfinity(1f / MathF.Truncate(-0.9f)));  // True
        Console.WriteLine(float.IsNegativeInfinity(1f / MathF.Ceiling(-0.5f)));   // True

        // Round(x, mode) — the digitless mode overload across all five modes.
        Console.WriteLine(MathF.Round(2.5f, MidpointRounding.ToEven));              // 2
        Console.WriteLine(MathF.Round(2.5f, MidpointRounding.AwayFromZero));        // 3
        Console.WriteLine(MathF.Round(-2.5f, MidpointRounding.AwayFromZero));       // -3
        Console.WriteLine(MathF.Round(0.5f, MidpointRounding.ToZero));              // 0
        Console.WriteLine(MathF.Round(-1.5f, MidpointRounding.ToNegativeInfinity)); // -2
        Console.WriteLine(MathF.Round(1.5f, MidpointRounding.ToPositiveInfinity));  // 2

        // Round(x, digits) divergence traps — the scaled product rounds
        // differently in float than in double: 2.675f * 100f is exactly
        // 267.5f (double says 267.4999...), 0.045f * 100f is exactly 4.5f
        // (double says 4.4999...). A double-routed arm would print 2.67 /
        // -2.67 / 0.05 / 0.04 here.
        Console.WriteLine(MathF.Round(2.675f, 2));    // 2.68
        Console.WriteLine(MathF.Round(-2.675f, 2));   // -2.68
        Console.WriteLine(MathF.Round(0.045f, 2));    // 0.04 (4.5f ties to even)
        Console.WriteLine(MathF.Round(0.045f, 2, MidpointRounding.AwayFromZero));  // 0.05

        // IEEERemainder — the sign-of-zero rule and the special operands
        // MathIeeeBits leaves out on the float side.
        Console.WriteLine(MathF.IEEERemainder(3f, 2f));   // -1
        Console.WriteLine(float.IsNegativeInfinity(1f / MathF.IEEERemainder(-4f, 2f)));  // True
        Console.WriteLine(float.IsNaN(MathF.IEEERemainder(1f, 0f)));                     // True
        Console.WriteLine(float.IsNaN(MathF.IEEERemainder(float.PositiveInfinity, 2f))); // True
        Console.WriteLine(MathF.IEEERemainder(4f, float.PositiveInfinity));              // 4

        // Math.Clamp(float, ...) — the single-precision Clamp (MathF has
        // none): normal clamping and the NaN-value passthrough.
        Console.WriteLine(Math.Clamp(2.5f, 0f, 2f));    // 2
        Console.WriteLine(Math.Clamp(-0.5f, 0f, 2f));   // 0
        Console.WriteLine(Math.Clamp(1.5f, 0f, 2f));    // 1.5
        Console.WriteLine(float.IsNaN(Math.Clamp(float.NaN, 0f, 1f)));  // True

        // Transcendentals — boolean anchors at exact points only (never
        // printed raw: libm and the BCL may differ in the last ulp).
        Console.WriteLine(MathF.Sin(0f) == 0f);     // True
        Console.WriteLine(float.IsNegativeInfinity(1f / MathF.Sin(-0.0f)));  // True
        Console.WriteLine(MathF.Cos(0f) == 1f);     // True
        Console.WriteLine(MathF.Tan(0f) == 0f);     // True
        Console.WriteLine(float.IsNegativeInfinity(1f / MathF.Tan(-0.0f)));  // True
        Console.WriteLine(MathF.Asin(0f) == 0f);    // True
        Console.WriteLine(MathF.Acos(1f) == 0f);    // True
        Console.WriteLine(MathF.Acos(0f) == MathF.PI / 2f);  // True
        Console.WriteLine(MathF.Atan(0f) == 0f);    // True
        Console.WriteLine(MathF.Atan(1f) == MathF.PI / 4f);  // True
        Console.WriteLine(MathF.Atan2(0f, 1f) == 0f);        // True
        Console.WriteLine(MathF.Exp(0f) == 1f);     // True
        Console.WriteLine(MathF.Exp(float.NegativeInfinity) == 0f);              // True
        Console.WriteLine(float.IsPositiveInfinity(MathF.Exp(float.PositiveInfinity)));  // True
        Console.WriteLine(MathF.Log(1f) == 0f);     // True
        Console.WriteLine(float.IsNegativeInfinity(MathF.Log(0f)));   // True
        Console.WriteLine(float.IsNaN(MathF.Log(-1f)));               // True
        Console.WriteLine(MathF.Log2(8f) == 3f);       // True
        Console.WriteLine(MathF.Log2(1024f) == 10f);   // True
        Console.WriteLine(MathF.Log10(100f) == 2f);    // True
        Console.WriteLine(MathF.Log10(1f) == 0f);      // True
        Console.WriteLine(MathF.Log(8f, 2f) == 3f);    // True (float log ratio)
        Console.WriteLine(MathF.Pow(2f, 10f) == 1024f);   // True
        Console.WriteLine(MathF.Pow(3f, 2f) == 9f);       // True
        Console.WriteLine(MathF.Pow(2f, -2f) == 0.25f);   // True
        Console.WriteLine(MathF.Pow(0f, 0f) == 1f);       // True
        Console.WriteLine(MathF.Pow(float.NaN, 0f) == 1f);   // True
        Console.WriteLine(MathF.Pow(1f, float.NaN) == 1f);   // True
        Console.WriteLine(float.IsPositiveInfinity(MathF.Pow(0f, -1f)));  // True
        Console.WriteLine(float.IsNaN(MathF.Pow(-1f, 0.5f)));             // True
        Console.WriteLine(MathF.Cbrt(27f) == 3f);   // True
        Console.WriteLine(MathF.Cbrt(-8f) == -2f);  // True
        Console.WriteLine(MathF.Cbrt(0f) == 0f);    // True
        Console.WriteLine(MathF.Sinh(0f) == 0f);    // True
        Console.WriteLine(MathF.Cosh(0f) == 1f);    // True
        Console.WriteLine(MathF.Tanh(0f) == 0f);    // True
        Console.WriteLine(MathF.Tanh(float.PositiveInfinity) == 1f);   // True
        Console.WriteLine(MathF.Tanh(float.NegativeInfinity) == -1f);  // True
        Console.WriteLine(float.IsPositiveInfinity(MathF.Cosh(float.PositiveInfinity)));  // True

        // Tolerance relations — hold on both implementations with slack far
        // above any ulp-level difference.
        Console.WriteLine(MathF.Abs(MathF.Sin(1f) - 0.84147096f) < 1e-6f);   // True
        Console.WriteLine(MathF.Abs(MathF.Cos(1f) - 0.5403023f) < 1e-6f);    // True
        Console.WriteLine(MathF.Abs(MathF.Tan(1f) - 1.5574077f) < 1e-6f);    // True
        Console.WriteLine(MathF.Abs(MathF.Asin(0.5f) - MathF.PI / 6f) < 1e-6f);   // True
        Console.WriteLine(MathF.Abs(MathF.Acos(0.5f) - MathF.PI / 3f) < 1e-6f);   // True
        Console.WriteLine(MathF.Abs(MathF.Atan2(1f, 1f) - MathF.PI / 4f) < 1e-6f); // True
        Console.WriteLine(MathF.Abs(MathF.Sinh(1f) - 1.1752012f) < 1e-6f);   // True
        Console.WriteLine(MathF.Abs(MathF.Cosh(1f) - 1.5430807f) < 1e-6f);   // True
        Console.WriteLine(MathF.Abs(MathF.Tanh(1f) - 0.7615942f) < 1e-6f);   // True
        Console.WriteLine(MathF.Abs(MathF.Exp(1f) - MathF.E) < 1e-6f);       // True
        Console.WriteLine(MathF.Abs(MathF.Log(10f) - 2.3025851f) < 1e-6f);   // True
        Console.WriteLine(MathF.Abs(MathF.Log2(10f) - 3.321928f) < 1e-6f);   // True
        Console.WriteLine(MathF.Abs(MathF.Log10(2f) - 0.30103f) < 1e-6f);    // True
        Console.WriteLine(MathF.Abs(MathF.Cbrt(2f) - 1.2599211f) < 1e-6f);   // True
        Console.WriteLine(MathF.Abs(MathF.Pow(2f, 0.5f) - MathF.Sqrt(2f)) < 1e-6f);  // True

        // SinCos — the tuple matches the individual Sin/Cos of the same
        // implementation exactly, and the Pythagorean identity holds.
        var (s0, c0) = MathF.SinCos(0f);
        Console.WriteLine(s0 == 0f && c0 == 1f);  // True
        var (s1, c1) = MathF.SinCos(1.25f);
        Console.WriteLine(s1 == MathF.Sin(1.25f) && c1 == MathF.Cos(1.25f));  // True
        Console.WriteLine(MathF.Abs(s1 * s1 + c1 * c1 - 1f) < 1e-6f);         // True

        // Exception paths not already asserted elsewhere (Sign(NaN),
        // Round(1f, 7) and Round(1f, 2, (MidpointRounding)9) live in
        // MathExceptionSemantics / MathRoundDigits): negative digits, an
        // invalid digitless mode, and the float Clamp's min > max.
        try { Console.WriteLine(MathF.Round(1.5f, -1)); }
        catch (Exception ex) { Console.WriteLine(ex.GetType().Name); }  // ArgumentOutOfRangeException
        try { Console.WriteLine(MathF.Round(1.5f, (MidpointRounding)9)); }
        catch (Exception ex) { Console.WriteLine(ex.GetType().Name); }  // ArgumentException
        try { Console.WriteLine(Math.Clamp(1f, 5f, 2f)); }
        catch (Exception ex) { Console.WriteLine(ex.GetType().Name); }  // ArgumentException
    }
}
