#nullable enable
// Custom numeric format strings — "pos;neg;zero" sections, "%"/permille scaling,
// escapes and quoted literals, custom E/e exponentials — plus the ToString(string)
// overload, which routes through the same runtime formatters as a "{0:fmt}" hole.
// Everything is InvariantCulture; an IFormatProvider would pull unsupported culture IL.
// Both entry points are exercised; output is bracketed so trailing spaces stay visible.

using System;
namespace CustomFormatSubset;


static class Program
{
    static void F(double v, string fmt) => Console.WriteLine("[" + string.Format("{0:" + fmt + "}", v) + "]");

    internal static void __GateEntry()
    {
        // Sections: positive ; negative ; zero.
        F(12.5, "0.0;(0.0);zero");
        F(-12.5, "0.0;(0.0);zero");
        F(0, "0.0;(0.0);zero");
        F(12.5, "0.0;(0.0)");    // two sections (zero uses the positive one)
        F(-12.5, "0.0;(0.0)");
        F(-5, "0.0");            // single section -> negatives get '-'

        // Percent / per-mille scaling.
        F(0.1234, "0.0%");
        F(0.1234, "0.00%");
        F(0.1234, "0.0‰");

        // Literals and escapes.
        F(5, "\\#0.0");
        F(5, "'x'0.0");
        F(12.3, "0.0 'units'");
        F(7, "0.0°C");

        // Custom exponential.
        F(1234.5, "0.00E+00");
        F(0.001234, "0.0e0");
        F(1234.5, "0.00E0");

        // Engineering exponential: >1 integer placeholder makes the exponent a multiple
        // of the integer-digit count.
        F(1234.5, "00.0E+0");
        F(1234.5, "##0.0E+0");
        F(1234.5, "##0.00E+00");
        F(0.0001234, "##0.0E+0");
        F(5, "##0.0E+0");
        F(0, "##0.0E+0");
        F(-1234.5, "##0.0E+0");
        F(987654321, "0.0##E0");

        // Grouping / '#' suppression.
        F(1234.567, "#,##0.00");
        F(0.5, "#.##");
        F(0, "#.##");

        // The ToString(string) overload on each numeric type.
        Console.WriteLine("--ToString(string)--");
        Console.WriteLine(255.ToString("X"));
        Console.WriteLine(7.ToString("D3"));
        Console.WriteLine(1234567.ToString("N0"));
        Console.WriteLine(1234567.ToString("F1"));
        Console.WriteLine(42u.ToString("D5"));
        Console.WriteLine((255L).ToString("X"));
        Console.WriteLine((123456789012L).ToString("N0"));
        Console.WriteLine((3.14159).ToString("F2"));
        Console.WriteLine((0.1234).ToString("0.0%"));
        Console.WriteLine((-12.5).ToString("0.0;(0.0);zero"));
        Console.WriteLine((5.0).ToString("0.0 'kg'"));
        Console.WriteLine((1f / 3f).ToString("G"));   // float G -> float precision
        Console.WriteLine((1f / 3f).ToString("F4"));
        Console.WriteLine((1234.5f).ToString("0.00E+00"));
    }
}
