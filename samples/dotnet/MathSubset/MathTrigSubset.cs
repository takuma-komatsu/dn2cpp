using System;

// The remaining clean Math functions as inline std mappings — Cbrt, the
// inverse/hyperbolic trig (Asin/Acos/Atan, Sinh/Cosh/Tanh), Log2 and the
// two-argument Log(value, newBase) = log(value)/log(newBase). Results asserted
// by equality (std matches the BCL for these values, including the
// FP-sensitive Log ratios).
namespace MathTrigSubset;

static class Program
{
    internal static void __GateEntry()
    {
        Console.WriteLine(Math.Cbrt(27.0) == 3.0);         // True
        Console.WriteLine(Math.Cbrt(8.0) == 2.0);          // True
        Console.WriteLine(Math.Log(8.0, 2.0) == 3.0);      // True
        Console.WriteLine(Math.Log(100.0, 10.0) == 2.0);   // True
        Console.WriteLine(Math.Log2(8.0) == 3.0);          // True
        Console.WriteLine(Math.Asin(1.0) == Math.PI / 2);  // True
        Console.WriteLine(Math.Acos(1.0) == 0.0);          // True
        Console.WriteLine(Math.Atan(0.0) == 0.0);          // True
        Console.WriteLine(Math.Sinh(0.0) == 0.0);          // True
        Console.WriteLine(Math.Cosh(0.0) == 1.0);          // True
        Console.WriteLine(Math.Tanh(0.0) == 0.0);          // True
    }
}
