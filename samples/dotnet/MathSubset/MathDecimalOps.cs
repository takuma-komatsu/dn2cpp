using System;

// The decimal Math members: Round across all five MidpointRounding modes and
// Clamp. Rounding is scale-aware — the directed modes look at every dropped
// digit, not just the first (2.5 vs 2.345 at 2 digits), decimal.Round trims
// the scale to `digits` but no further (trailing zeros inside the kept places
// survive), and an invalid mode throws even when no digit would be dropped.
// Clamp returns the nearest bound and throws when min > max. Exceptions print
// by runtime type name like the MathExceptionSemantics section.
namespace MathDecimalOps;

static class Program
{
    internal static void __GateEntry()
    {
        // The midpoint 2.5 under every mode, both signs.
        Console.WriteLine(Math.Round(2.5m, MidpointRounding.ToEven));              // 2
        Console.WriteLine(Math.Round(-2.5m, MidpointRounding.ToEven));             // -2
        Console.WriteLine(Math.Round(2.5m, MidpointRounding.AwayFromZero));        // 3
        Console.WriteLine(Math.Round(-2.5m, MidpointRounding.AwayFromZero));       // -3
        Console.WriteLine(Math.Round(2.5m, MidpointRounding.ToZero));              // 2
        Console.WriteLine(Math.Round(-2.5m, MidpointRounding.ToZero));             // -2
        Console.WriteLine(Math.Round(2.5m, MidpointRounding.ToNegativeInfinity));  // 2
        Console.WriteLine(Math.Round(-2.5m, MidpointRounding.ToNegativeInfinity)); // -3
        Console.WriteLine(Math.Round(2.5m, MidpointRounding.ToPositiveInfinity));  // 3
        Console.WriteLine(Math.Round(-2.5m, MidpointRounding.ToPositiveInfinity)); // -2

        // digits + mode: the directed modes see every dropped digit.
        Console.WriteLine(Math.Round(2.345m, 2, MidpointRounding.ToZero));              // 2.34
        Console.WriteLine(Math.Round(2.345m, 2, MidpointRounding.ToPositiveInfinity));  // 2.35
        Console.WriteLine(Math.Round(-2.345m, 2, MidpointRounding.ToNegativeInfinity)); // -2.35
        Console.WriteLine(Math.Round(-2.345m, 2, MidpointRounding.ToPositiveInfinity)); // -2.34
        Console.WriteLine(Math.Round(2.001m, 0, MidpointRounding.ToPositiveInfinity));  // 3
        Console.WriteLine(Math.Round(-2.001m, 0, MidpointRounding.ToNegativeInfinity)); // -3

        // Scale behavior: rounding trims to `digits`, dropping nothing keeps
        // the value's own scale, and trailing zeros inside the kept places stay.
        Console.WriteLine(Math.Round(2.500m, 1, MidpointRounding.ToZero));    // 2.5
        Console.WriteLine(Math.Round(2.5000m, 3, MidpointRounding.ToEven));   // 2.500
        Console.WriteLine(Math.Round(2.5m, 3, MidpointRounding.ToZero));      // 2.5
        Console.WriteLine(Math.Round(2.0m, MidpointRounding.ToPositiveInfinity)); // 2
        Console.WriteLine(decimal.Round(-2.6m, MidpointRounding.ToZero));     // -2

        // An invalid mode throws — even when no digit would be dropped.
        try { Console.WriteLine(Math.Round(1.5m, (MidpointRounding)5)); }
        catch (Exception ex) { Console.WriteLine(ex.GetType().Name); }  // ArgumentException
        try { Console.WriteLine(Math.Round(1.5m, 0, (MidpointRounding)(-1))); }
        catch (Exception ex) { Console.WriteLine(ex.GetType().Name); }  // ArgumentException
        try { Console.WriteLine(Math.Round(2m, (MidpointRounding)7)); }
        catch (Exception ex) { Console.WriteLine(ex.GetType().Name); }  // ArgumentException

        // Clamp: in range, at the bounds, and min > max.
        Console.WriteLine(Math.Clamp(2m, 1m, 3m));       // 2
        Console.WriteLine(Math.Clamp(5m, 1m, 3m));       // 3
        Console.WriteLine(Math.Clamp(0.5m, 1.25m, 3m));  // 1.25
        Console.WriteLine(Math.Clamp(7m, 7m, 7m));       // 7
        try { Console.WriteLine(Math.Clamp(1m, 3m, 2m)); }
        catch (Exception ex) { Console.WriteLine(ex.GetType().Name); }  // ArgumentException
    }
}
