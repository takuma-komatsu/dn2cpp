using System;
using System.Globalization;

namespace FloatParseSubset
{
    // double/float Parse and TryParse (invariant: optional surrounding
    // whitespace, optional sign, decimal/exponent). The single-string overloads
    // are intercepted as strtod-based intrinsics, avoiding the BCL's
    // globalization/NumberFormatInfo closure that the real bodies pull in.
    // Printed values stay in a range where "%g" and.NET agree on formatting;
    // larger magnitudes are asserted by equality instead.
    internal static class Program
    {
        internal static void __GateEntry()
        {
            Console.WriteLine(double.Parse("2.5"));        // 2.5
            Console.WriteLine(double.Parse("-0.75"));      // -0.75
            Console.WriteLine(double.Parse("  100.25  ")); // 100.25 (whitespace)
            Console.WriteLine(double.Parse("+12.5"));      // 12.5 (leading sign)
            Console.WriteLine(double.Parse("3"));          // 3 (integer-valued)

            // Exact-value assertions (formatting-independent).
            Console.WriteLine(double.Parse("123456.789") == 123456.789); // True
            Console.WriteLine(double.Parse("1.5e3") == 1500.0);          // True

            Console.WriteLine(float.Parse("1.5"));         // 1.5
            Console.WriteLine(float.Parse("-2.25"));       // -2.25

            double d;
            Console.WriteLine(double.TryParse("42.5", out d)); // True
            Console.WriteLine(d);                              // 42.5
            Console.WriteLine(double.TryParse("abc", out d));  // False
            Console.WriteLine(d);                              // 0 (default-on-failure)
            Console.WriteLine(double.TryParse("12x", out d));  // False (trailing garbage)
            Console.WriteLine(d);                              // 0

            float f;
            Console.WriteLine(float.TryParse("7.5", out f));   // True
            Console.WriteLine(f);                              // 7.5
            Console.WriteLine(float.TryParse("", out f));      // False
            Console.WriteLine(f);                              // 0

            NumberStylesSection();
        }

        private static void Show<T>(string tag, Func<T> fn)
        {
            try { Console.WriteLine(tag + " = " + fn()); }
            catch (Exception ex) { Console.WriteLine(tag + " ! " + ex.GetType().Name); }
        }

        // NumberStyles-honoring float/double parsing: exponent/decimal-point/
        // whitespace/sign gating, thousands stripping, invalid style
        // combinations, and the styles-blind Infinity/NaN symbol fallback.
        private static void NumberStylesSection()
        {
            var inv = CultureInfo.InvariantCulture;
            Console.WriteLine("-- float/double NumberStyles --");
            Console.WriteLine(double.Parse("1e5", NumberStyles.Float, inv));       // 100000
            Show("dbl[1e5 no-exp]", () => double.Parse("1e5", NumberStyles.AllowDecimalPoint, inv)); // FormatException
            Console.WriteLine(double.Parse("1,234.5", NumberStyles.Number, inv));  // 1234.5 (Number includes thousands)
            Show("dbl[1,234.5 Float]", () => double.Parse("1,234.5", NumberStyles.Float, inv));      // FormatException
            // Provider-less ON PURPOSE: the single-string overload is the strtod-based
            // intrinsic, and its implied NumberStyles.Float|AllowThousands is what this
            // asserts — a provider would move the call to the three-argument overload.
            // The driver's invariant pin makes the CurrentCulture read behind it
            // deterministic.
            Console.WriteLine(double.Parse("1,234.5"));                             // 1234.5 (plain Parse allows thousands)
            Console.WriteLine(double.Parse("15", NumberStyles.None, inv));          // 15
            Show("dbl[ 15 None]", () => double.Parse(" 15", NumberStyles.None, inv));   // FormatException
            Show("dbl[+15 None]", () => double.Parse("+15", NumberStyles.None, inv));   // FormatException
            Show("dbl[1.5 None]", () => double.Parse("1.5", NumberStyles.None, inv));   // FormatException
            Show("dbl[hex-style]", () => double.Parse("1F", NumberStyles.AllowHexSpecifier, inv));   // ArgumentException
            Show("flt[hex-style]", () => { float q; return float.TryParse("1F", NumberStyles.AllowHexSpecifier, inv, out q); }); // ArgumentException
            Console.WriteLine(double.Parse("123.", NumberStyles.Float, inv));       // 123
            Console.WriteLine(double.Parse(".5", NumberStyles.Float, inv));         // 0.5
            Console.WriteLine(double.Parse("123-", NumberStyles.Float | NumberStyles.AllowTrailingSign, inv)); // -123
            Console.WriteLine(double.Parse("(2.5)", NumberStyles.Float | NumberStyles.AllowParentheses, inv)); // -2.5
            Show("dbl[1e Float]", () => double.Parse("1e", NumberStyles.Float, inv));   // FormatException
            Show("dbl[1e+ Float]", () => double.Parse("1e+", NumberStyles.Float, inv)); // FormatException
            Console.WriteLine(double.Parse("1.5e-3", NumberStyles.Float, inv));     // 0.0015
            Console.WriteLine(float.Parse("2.5e2", NumberStyles.Float, inv));       // 250
            Console.WriteLine(double.Parse("2,5", NumberStyles.Float, new CultureInfo("de-DE")));    // 2.5 (de decimal comma)
            Console.WriteLine(double.Parse("1.234,5", NumberStyles.Number, new CultureInfo("de-DE"))); // 1234.5
            Console.WriteLine(double.Parse("3.25".AsSpan(), NumberStyles.Float, inv)); // 3.25 (span input)
            double sd;
            Console.WriteLine(double.TryParse("6.5".AsSpan(), NumberStyles.Float, inv, out sd) + " " + sd); // True 6.5
            Console.WriteLine(double.TryParse("1e5", NumberStyles.None, inv, out sd) + " " + sd);           // False 0

            Console.WriteLine("-- Infinity/NaN symbols (styles-blind) --");
            Console.WriteLine(double.Parse("Infinity", NumberStyles.None, inv) == double.PositiveInfinity);   // True
            Console.WriteLine(double.Parse(" Infinity ", NumberStyles.None, inv) == double.PositiveInfinity); // True (trim ignores styles)
            Console.WriteLine(double.Parse("-Infinity", NumberStyles.None, inv) == double.NegativeInfinity);  // True
            Console.WriteLine(double.Parse("+Infinity", NumberStyles.None, inv) == double.PositiveInfinity);  // True
            Console.WriteLine(double.IsNaN(double.Parse("NaN", NumberStyles.Float, inv)));       // True
            Console.WriteLine(double.IsNaN(double.Parse("-NaN", NumberStyles.Float, inv)));      // True
            Console.WriteLine(double.Parse("INFINITY", NumberStyles.Float, inv) == double.PositiveInfinity);  // True (case-insensitive)
            Console.WriteLine(double.IsNaN(double.Parse("nan", NumberStyles.Float, inv)));       // True
            Console.WriteLine(float.Parse("-Infinity", NumberStyles.Float, inv) == float.NegativeInfinity);   // True
            Show("dbl[Infinity5]", () => double.Parse("Infinity5", NumberStyles.Float, inv));    // FormatException

            Console.WriteLine("-- overflow/underflow to infinity/zero --");
            Console.WriteLine(double.Parse("2e400", NumberStyles.Float, inv) == double.PositiveInfinity);  // True
            Console.WriteLine(double.Parse("-2e400", NumberStyles.Float, inv) == double.NegativeInfinity); // True
            Console.WriteLine(float.Parse("3.5e38", NumberStyles.Float, inv) == float.PositiveInfinity);   // True
            Console.WriteLine(float.Parse("1e-50", NumberStyles.Float, inv));  // 0
            Console.WriteLine(double.Parse("1e-400", NumberStyles.Float, inv)); // 0
        }
    }
}
