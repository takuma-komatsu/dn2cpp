using System;

// Math rounding completion — Truncate and Log10 (unary, std mappings) and
// Round(value, MidpointRounding) (the mode enum selects std rounding at
// runtime). The 1-arg Round (ties-to-even) was already mapped. Results are
// asserted by equality so the test is independent of double formatting.
namespace MathRoundSubset;

static class Program
{
    internal static void __GateEntry()
    {
        // Truncate — toward zero.
        Console.WriteLine(Math.Truncate(2.7) == 2.0);     // True
        Console.WriteLine(Math.Truncate(-2.7) == -2.0);   // True
        Console.WriteLine(Math.Truncate(2.0) == 2.0);     // True

        // Log10.
        Console.WriteLine(Math.Log10(1000.0) == 3.0);     // True
        Console.WriteLine(Math.Log10(1.0) == 0.0);        // True

        // Round(value, MidpointRounding) — each enum mode.
        Console.WriteLine(Math.Round(2.5, MidpointRounding.ToEven) == 2.0);            // True
        Console.WriteLine(Math.Round(3.5, MidpointRounding.ToEven) == 4.0);            // True
        Console.WriteLine(Math.Round(2.5, MidpointRounding.AwayFromZero) == 3.0);      // True
        Console.WriteLine(Math.Round(-2.5, MidpointRounding.AwayFromZero) == -3.0);    // True
        Console.WriteLine(Math.Round(2.7, MidpointRounding.ToZero) == 2.0);            // True
        Console.WriteLine(Math.Round(-2.7, MidpointRounding.ToZero) == -2.0);          // True
        Console.WriteLine(Math.Round(-2.3, MidpointRounding.ToNegativeInfinity) == -3.0); // True
        Console.WriteLine(Math.Round(2.3, MidpointRounding.ToPositiveInfinity) == 3.0);   // True
    }
}
