using System;

// Math/MathF.Min/Max floating-point semantics (IEEE 754:2019 minimum/
// maximum): a NaN operand propagates (from either position) and -0.0 orders
// below +0.0 — both unlike C's fmin/fmax, which drop the NaN and treat the
// zeros as equal. NaN/Infinity results are asserted with the Is* predicates
// so the output is independent of the culture's NaN/Infinity symbols.
namespace MathMinMaxSemantics;

static class Program
{
    internal static void __GateEntry()
    {
        // NaN propagates from either operand.
        Console.WriteLine(double.IsNaN(Math.Min(double.NaN, 1.0)));  // True
        Console.WriteLine(double.IsNaN(Math.Min(1.0, double.NaN)));  // True
        Console.WriteLine(double.IsNaN(Math.Max(double.NaN, 1.0)));  // True
        Console.WriteLine(double.IsNaN(Math.Max(1.0, double.NaN)));  // True
        Console.WriteLine(float.IsNaN(MathF.Min(float.NaN, 1.0f)));  // True
        Console.WriteLine(float.IsNaN(MathF.Min(1.0f, float.NaN)));  // True
        Console.WriteLine(float.IsNaN(MathF.Max(float.NaN, 1.0f)));  // True
        Console.WriteLine(float.IsNaN(MathF.Max(1.0f, float.NaN)));  // True

        // -0.0 orders below +0.0, observable only through the sign of the
        // reciprocal — in both operand orders.
        Console.WriteLine(double.IsNegativeInfinity(1.0 / Math.Min(-0.0, 0.0)));   // True
        Console.WriteLine(double.IsNegativeInfinity(1.0 / Math.Min(0.0, -0.0)));   // True
        Console.WriteLine(double.IsPositiveInfinity(1.0 / Math.Max(-0.0, 0.0)));   // True
        Console.WriteLine(double.IsPositiveInfinity(1.0 / Math.Max(0.0, -0.0)));   // True
        Console.WriteLine(float.IsNegativeInfinity(1.0f / MathF.Min(-0.0f, 0.0f))); // True
        Console.WriteLine(float.IsNegativeInfinity(1.0f / MathF.Min(0.0f, -0.0f))); // True
        Console.WriteLine(float.IsPositiveInfinity(1.0f / MathF.Max(-0.0f, 0.0f))); // True
        Console.WriteLine(float.IsPositiveInfinity(1.0f / MathF.Max(0.0f, -0.0f))); // True

        // Regular finite cases keep working (including an infinite operand).
        Console.WriteLine(Math.Min(1.5, 2.5));                       // 1.5
        Console.WriteLine(Math.Max(1.5, 2.5));                       // 2.5
        Console.WriteLine(Math.Min(-3.0, 2.0));                      // -3
        Console.WriteLine(Math.Max(double.NegativeInfinity, 7.0));   // 7
        Console.WriteLine(Math.Min(double.PositiveInfinity, 7.0));   // 7
        Console.WriteLine(MathF.Min(1.5f, 2.5f) == 1.5f);            // True
        Console.WriteLine(MathF.Max(1.5f, 2.5f) == 2.5f);            // True
        Console.WriteLine(MathF.Min(-3.5f, -7.25f) == -7.25f);       // True
        Console.WriteLine(MathF.Max(-3.5f, -7.25f) == -3.5f);        // True
    }
}
