using System;

// The inverse hyperbolic trio — Acosh, Asinh, Atanh — for both Math and
// MathF. Transcendental results are never printed raw (libm and the BCL may
// differ in the last ulp): the assertions are boolean anchors at exact points
// (Acosh(1) == 0, the IEEE-mandated poles and NaN domains, the -0 sign
// preservation) plus round-trip tolerance relations.
namespace MathHyperbolicInverse;

static class Program
{
    internal static void __GateEntry()
    {
        // Exact anchors.
        Console.WriteLine(Math.Acosh(1.0) == 0.0);   // True
        Console.WriteLine(Math.Asinh(0.0) == 0.0);   // True
        Console.WriteLine(Math.Atanh(0.0) == 0.0);   // True

        // Round-trips within tolerance (never printed raw).
        Console.WriteLine(Math.Abs(Math.Acosh(Math.Cosh(1.5)) - 1.5) < 1e-12);   // True
        Console.WriteLine(Math.Abs(Math.Asinh(Math.Sinh(2.0)) - 2.0) < 1e-12);   // True
        Console.WriteLine(Math.Abs(Math.Atanh(Math.Tanh(0.5)) - 0.5) < 1e-12);   // True

        // IEEE-mandated poles, domain edges, and NaN domains.
        Console.WriteLine(double.IsPositiveInfinity(Math.Atanh(1.0)));    // True
        Console.WriteLine(double.IsNegativeInfinity(Math.Atanh(-1.0)));   // True
        Console.WriteLine(double.IsNaN(Math.Atanh(2.0)));                 // True
        Console.WriteLine(double.IsNaN(Math.Acosh(0.5)));                 // True
        Console.WriteLine(double.IsPositiveInfinity(Math.Acosh(double.PositiveInfinity))); // True
        Console.WriteLine(double.IsNegativeInfinity(Math.Asinh(double.NegativeInfinity))); // True

        // Asinh/Atanh preserve the sign of zero (odd functions).
        Console.WriteLine(double.IsNegativeInfinity(1.0 / Math.Asinh(-0.0)));   // True
        Console.WriteLine(double.IsNegativeInfinity(1.0 / Math.Atanh(-0.0)));   // True

        // MathF — the same anchors in single precision.
        Console.WriteLine(MathF.Acosh(1.0f) == 0.0f);   // True
        Console.WriteLine(MathF.Asinh(0.0f) == 0.0f);   // True
        Console.WriteLine(MathF.Atanh(0.0f) == 0.0f);   // True
        Console.WriteLine(MathF.Abs(MathF.Asinh(MathF.Sinh(1.5f)) - 1.5f) < 1e-5f);   // True
        Console.WriteLine(MathF.Abs(MathF.Acosh(MathF.Cosh(1.5f)) - 1.5f) < 1e-5f);   // True
        Console.WriteLine(float.IsPositiveInfinity(MathF.Atanh(1.0f)));   // True
        Console.WriteLine(float.IsNaN(MathF.Acosh(0.5f)));                // True
    }
}
