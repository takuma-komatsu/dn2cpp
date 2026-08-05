using System;

// Math/MathF.ReciprocalEstimate and ReciprocalSqrtEstimate. .NET documents
// the results as platform-dependent approximations (this host's arm64
// hardware answers FRECPE/FRSQRTE, ~8-bit precision; the transpiled output
// computes the exact division), so finite results are never printed raw —
// only tolerance relations that hold under any conforming estimate. The
// IEEE special cases (±0, ±infinity, NaN, negative sqrt inputs) are exact
// under both implementations (verified against real .NET on arm64) and are
// observed through the Is* predicates so no signed zero prints raw.
namespace MathEstimates;

static class Program
{
    internal static void __GateEntry()
    {
        // ReciprocalEstimate(double): within 2% of the exact reciprocal.
        Console.WriteLine(Math.Abs(Math.ReciprocalEstimate(4.0) * 4.0 - 1.0) < 0.02);    // True
        Console.WriteLine(Math.Abs(Math.ReciprocalEstimate(3.0) * 3.0 - 1.0) < 0.02);    // True
        Console.WriteLine(Math.Abs(Math.ReciprocalEstimate(0.1) * 0.1 - 1.0) < 0.02);    // True
        Console.WriteLine(Math.Abs(Math.ReciprocalEstimate(100.0) * 100.0 - 1.0) < 0.02); // True
        Console.WriteLine(Math.Abs(Math.ReciprocalEstimate(-2.0) - -0.5) < 0.02);        // True

        // ReciprocalSqrtEstimate(double): within 2% of the exact 1/sqrt.
        Console.WriteLine(Math.Abs(Math.ReciprocalSqrtEstimate(4.0) - 0.5) < 0.02);      // True
        Console.WriteLine(Math.Abs(Math.ReciprocalSqrtEstimate(1.0) - 1.0) < 0.02);      // True
        Console.WriteLine(Math.Abs(Math.ReciprocalSqrtEstimate(0.25) - 2.0) < 0.05);     // True
        Console.WriteLine(Math.Abs(Math.ReciprocalSqrtEstimate(100.0) - 0.1) < 0.02);    // True

        // The exact special cases, shared by FRECPE/FRSQRTE and the division.
        Console.WriteLine(double.IsPositiveInfinity(Math.ReciprocalEstimate(0.0)));      // True
        Console.WriteLine(double.IsNegativeInfinity(Math.ReciprocalEstimate(-0.0)));     // True
        Console.WriteLine(Math.ReciprocalEstimate(double.PositiveInfinity) == 0.0);      // True
        Console.WriteLine(double.IsNegativeInfinity(1.0 / Math.ReciprocalEstimate(double.NegativeInfinity))); // True (-0)
        Console.WriteLine(double.IsNaN(Math.ReciprocalEstimate(double.NaN)));            // True
        Console.WriteLine(double.IsPositiveInfinity(Math.ReciprocalSqrtEstimate(0.0)));  // True
        Console.WriteLine(double.IsNegativeInfinity(Math.ReciprocalSqrtEstimate(-0.0))); // True
        Console.WriteLine(Math.ReciprocalSqrtEstimate(double.PositiveInfinity) == 0.0);  // True
        Console.WriteLine(double.IsNaN(Math.ReciprocalSqrtEstimate(double.NaN)));        // True
        Console.WriteLine(double.IsNaN(Math.ReciprocalSqrtEstimate(-1.0)));              // True

        // MathF variants: the same relations in single precision.
        Console.WriteLine(MathF.Abs(MathF.ReciprocalEstimate(4.0f) * 4.0f - 1.0f) < 0.02f);   // True
        Console.WriteLine(MathF.Abs(MathF.ReciprocalEstimate(3.0f) * 3.0f - 1.0f) < 0.02f);   // True
        Console.WriteLine(MathF.Abs(MathF.ReciprocalEstimate(-2.0f) - -0.5f) < 0.02f);        // True
        Console.WriteLine(MathF.Abs(MathF.ReciprocalSqrtEstimate(4.0f) - 0.5f) < 0.02f);      // True
        Console.WriteLine(MathF.Abs(MathF.ReciprocalSqrtEstimate(0.25f) - 2.0f) < 0.05f);     // True
        Console.WriteLine(float.IsPositiveInfinity(MathF.ReciprocalEstimate(0.0f)));          // True
        Console.WriteLine(float.IsNegativeInfinity(MathF.ReciprocalEstimate(-0.0f)));         // True
        Console.WriteLine(MathF.ReciprocalEstimate(float.PositiveInfinity) == 0.0f);          // True
        Console.WriteLine(float.IsNaN(MathF.ReciprocalEstimate(float.NaN)));                  // True
        Console.WriteLine(float.IsPositiveInfinity(MathF.ReciprocalSqrtEstimate(0.0f)));      // True
        Console.WriteLine(MathF.ReciprocalSqrtEstimate(float.PositiveInfinity) == 0.0f);      // True
        Console.WriteLine(float.IsNaN(MathF.ReciprocalSqrtEstimate(float.NaN)));              // True
        Console.WriteLine(float.IsNaN(MathF.ReciprocalSqrtEstimate(-1.0f)));                  // True
    }
}
