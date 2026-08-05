using System;

// Round(value, digits) and Round(value, digits, MidpointRounding) for both
// Math and MathF — the scale/round/unscale algorithm is deterministic
// arithmetic, so results are printed exactly. Covers every MidpointRounding
// mode with digits, negative values, the digits range edges (15 for double,
// 6 for float), the >= 1e16 / 1e8f passthrough (where even an invalid mode
// goes undetected, like the BCL), and the digits/mode exception paths.
namespace MathRoundDigits;

static class Program
{
    internal static void __GateEntry()
    {
        // Round(value, digits) — ties to even after scaling.
        Console.WriteLine(Math.Round(3.14159, 4));    // 3.1416
        Console.WriteLine(Math.Round(2.34567, 2));    // 2.35
        Console.WriteLine(Math.Round(2.5, 0));        // 2
        Console.WriteLine(Math.Round(1.005, 2));      // 1 (1.005 scales to 100.49999...)
        Console.WriteLine(Math.Round(-3.14159, 3));   // -3.142
        Console.WriteLine(Math.Round(0.125, 2));      // 0.12 (exact tie, to even)
        Console.WriteLine(Math.Round(0.135, 2));      // 0.14

        // All five modes at an exact scaled tie (2.125 * 100 == 212.5).
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

        // The digits = 15 edge and the >= 1e16 passthrough (which also skips
        // the mode validation, exactly like the BCL).
        Console.WriteLine(Math.Round(Math.PI, 15));                              // 3.141592653589793
        Console.WriteLine(Math.Round(1.23e16, 2));                               // 1.23E+16
        Console.WriteLine(Math.Round(-5.67e16, 0, MidpointRounding.AwayFromZero)); // -5.67E+16
        Console.WriteLine(Math.Round(1e17, 2, (MidpointRounding)9));             // 1E+17

        // MathF — float digits 0..6, the 1e8f limit, float-precision scaling.
        Console.WriteLine(MathF.Round(3.14159f, 2));    // 3.14
        Console.WriteLine(MathF.Round(2.5f, 0));        // 2
        Console.WriteLine(MathF.Round(2.34567f, 3));    // 2.346
        Console.WriteLine(MathF.Round(-2.5f, 0, MidpointRounding.AwayFromZero));      // -3
        Console.WriteLine(MathF.Round(2.125f, 2, MidpointRounding.ToEven));           // 2.12
        Console.WriteLine(MathF.Round(2.125f, 2, MidpointRounding.ToPositiveInfinity)); // 2.13
        Console.WriteLine(MathF.Round(-0.7f, 0, MidpointRounding.ToZero));            // -0
        Console.WriteLine(MathF.Round(1.2345678f, 6));  // 1.234568
        Console.WriteLine(MathF.Round(2.5e8f, 1));      // 2.5E+08
        Console.WriteLine(MathF.Round(3.5e8f, 0, (MidpointRounding)9));  // 3.5E+08

        // Exceptions: digits out of range -> ArgumentOutOfRangeException;
        // an invalid mode below the limit -> ArgumentException (also via the
        // digitless Round(value, mode) overload).
        try { Console.WriteLine(Math.Round(1.5, 16)); }
        catch (Exception ex) { Console.WriteLine(ex.GetType().Name); }  // ArgumentOutOfRangeException
        try { Console.WriteLine(Math.Round(1.5, -1)); }
        catch (Exception ex) { Console.WriteLine(ex.GetType().Name); }  // ArgumentOutOfRangeException
        try { Console.WriteLine(MathF.Round(1.5f, 7)); }
        catch (Exception ex) { Console.WriteLine(ex.GetType().Name); }  // ArgumentOutOfRangeException
        try { Console.WriteLine(Math.Round(1.5, 2, (MidpointRounding)9)); }
        catch (Exception ex) { Console.WriteLine(ex.GetType().Name); }  // ArgumentException
        try { Console.WriteLine(MathF.Round(1.5f, 2, (MidpointRounding)9)); }
        catch (Exception ex) { Console.WriteLine(ex.GetType().Name); }  // ArgumentException
        try { Console.WriteLine(Math.Round(1.5, (MidpointRounding)9)); }
        catch (Exception ex) { Console.WriteLine(ex.GetType().Name); }  // ArgumentException
    }
}
