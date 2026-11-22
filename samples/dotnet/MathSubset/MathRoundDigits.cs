using System;

// Round(value, digits) and Round(value, digits, MidpointRounding) for both
// Math and MathF — the value's exact decimal expansion is rounded at `digits`
// and converted back, so results are printed exactly. Covers every
// MidpointRounding mode with digits, negative values, digit counts beyond the
// type's precision, the integral-magnitude passthrough (2^52 for double, 2^23
// for float; the mode is still validated there), and the digits/mode
// exception paths.
namespace MathRoundDigits;

static class Program
{
    internal static void __GateEntry()
    {
        // Round(value, digits) — ties of the exact value go to even.
        Console.WriteLine(Math.Round(3.14159, 4));    // 3.1416
        Console.WriteLine(Math.Round(2.34567, 2));    // 2.35
        Console.WriteLine(Math.Round(2.5, 0));        // 2
        Console.WriteLine(Math.Round(1.005, 2));      // 1 (the double nearest 1.005 is below it)
        Console.WriteLine(Math.Round(-3.14159, 3));   // -3.142
        Console.WriteLine(Math.Round(0.125, 2));      // 0.12 (exact tie, to even)
        Console.WriteLine(Math.Round(0.135, 2));      // 0.14

        // All five modes at an exact tie (2.125 is representable).
        Console.WriteLine(Math.Round(2.125, 2, MidpointRounding.ToEven));              // 2.12
        Console.WriteLine(Math.Round(2.125, 2, MidpointRounding.AwayFromZero));        // 2.13
        Console.WriteLine(Math.Round(2.125, 2, MidpointRounding.ToZero));              // 2.12
        Console.WriteLine(Math.Round(2.125, 2, MidpointRounding.ToNegativeInfinity));  // 2.12
        Console.WriteLine(Math.Round(2.125, 2, MidpointRounding.ToPositiveInfinity));  // 2.13
        Console.WriteLine(Math.Round(-2.125, 2, MidpointRounding.ToEven));             // -2.12
        Console.WriteLine(Math.Round(-2.125, 2, MidpointRounding.AwayFromZero));       // -2.13
        Console.WriteLine(Math.Round(-2.125, 2, MidpointRounding.ToZero));             // -2.12
        Console.WriteLine(Math.Round(-2.125, 2, MidpointRounding.ToNegativeInfinity)); // -2.13
        Console.WriteLine(Math.Round(-2.125, 2, MidpointRounding.ToPositiveInfinity)); // -2.12
        Console.WriteLine(Math.Round(2.7, 0, MidpointRounding.ToZero));                // 2
        Console.WriteLine(Math.Round(-2.3, 0, MidpointRounding.ToNegativeInfinity));   // -3

        // Full-precision digits and the integral-magnitude passthrough, which
        // still rejects an undefined mode.
        Console.WriteLine(Math.Round(Math.PI, 15));                              // 3.141592653589793
        Console.WriteLine(Math.Round(1.23e16, 2));                               // 1.23E+16
        Console.WriteLine(Math.Round(-5.67e16, 0, MidpointRounding.AwayFromZero)); // -5.67E+16
        try { Console.WriteLine(Math.Round(1e17, 2, (MidpointRounding)9)); }
        catch (ArgumentException ex) { Console.WriteLine(ex.GetType().Name + ":" + ex.ParamName); }  // ArgumentException:mode

        // MathF — the same rounding of the float's exact value.
        Console.WriteLine(MathF.Round(3.14159f, 2));    // 3.14
        Console.WriteLine(MathF.Round(2.5f, 0));        // 2
        Console.WriteLine(MathF.Round(2.34567f, 3));    // 2.346
        Console.WriteLine(MathF.Round(-2.5f, 0, MidpointRounding.AwayFromZero));      // -3
        Console.WriteLine(MathF.Round(2.125f, 2, MidpointRounding.ToEven));           // 2.12
        Console.WriteLine(MathF.Round(2.125f, 2, MidpointRounding.ToPositiveInfinity)); // 2.13
        Console.WriteLine(MathF.Round(-0.7f, 0, MidpointRounding.ToZero));            // -0
        Console.WriteLine(MathF.Round(1.2345678f, 6));  // 1.234568
        Console.WriteLine(MathF.Round(2.5e8f, 1));      // 2.5E+08
        try { Console.WriteLine(MathF.Round(3.5e8f, 0, (MidpointRounding)9)); }
        catch (ArgumentException ex) { Console.WriteLine(ex.GetType().Name + ":" + ex.ParamName); }  // ArgumentException:mode

        // Digits have no upper bound; negative digits ->
        // ArgumentOutOfRangeException; an undefined mode -> ArgumentException
        // (also via the digitless Round(value, mode) overload).
        try { Console.WriteLine(Math.Round(1.5, 16)); }
        catch (Exception ex) { Console.WriteLine(ex.GetType().Name); }  // 1.5
        try { Console.WriteLine(Math.Round(1.5, -1)); }
        catch (Exception ex) { Console.WriteLine(ex.GetType().Name); }  // ArgumentOutOfRangeException
        try { Console.WriteLine(MathF.Round(1.5f, 7)); }
        catch (Exception ex) { Console.WriteLine(ex.GetType().Name); }  // 1.5
        try { Console.WriteLine(Math.Round(1.5, 2, (MidpointRounding)9)); }
        catch (Exception ex) { Console.WriteLine(ex.GetType().Name); }  // ArgumentException
        try { Console.WriteLine(MathF.Round(1.5f, 2, (MidpointRounding)9)); }
        catch (Exception ex) { Console.WriteLine(ex.GetType().Name); }  // ArgumentException
        try { Console.WriteLine(Math.Round(1.5, (MidpointRounding)9)); }
        catch (Exception ex) { Console.WriteLine(ex.GetType().Name); }  // ArgumentException
    }
}
