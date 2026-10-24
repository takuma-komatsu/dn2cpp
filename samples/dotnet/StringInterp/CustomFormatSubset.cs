#nullable enable
// Custom numeric format strings — "pos;neg;zero" sections, "%"/permille scaling,
// escapes and quoted literals, custom E/e exponentials — plus the ToString(string)
// overload, which routes through the same runtime formatters as a "{0:fmt}" hole.
// The driver pins InvariantCulture; targeted NumberFormatInfo cases exercise symbols.
// Both entry points are exercised; output is bracketed so trailing spaces stay visible.

using System;
using System.Globalization;
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

    internal static void __CommaScalingGate()
    {
        Console.WriteLine("== Custom numeric comma scaling ==");
        Console.WriteLine("trillion=" + (100 * 1e12).ToString("0,,,,.####"));
        Console.WriteLine("fraction=" + 123456789.0.ToString("0,,.###"));
        Console.WriteLine("integer=" + 1234567L.ToString("0,"));
        Console.WriteLine("grouped=" + 1234567890L.ToString("#,##0,,"));
        Console.WriteLine("grouped fraction=" + 1234567890.0.ToString("#,##0,.00"));
        Console.WriteLine("negative=" + (-1234567.0).ToString("0,,.##;(0,,.##);'zero'"));
        Console.WriteLine("zero=" + 0.0.ToString("0,,.##;(0,,.##);'zero'"));
        Console.WriteLine("ordinary grouping=" + 1234567.0.ToString("#,##0"));
        Console.WriteLine("quoted=" + 1234567.0.ToString("0','"));
        Console.WriteLine("escaped=" + 1234567.0.ToString("0\\,"));
        Console.WriteLine("fraction commas=" + 1234.5.ToString("0.0,,"));
        Console.WriteLine("percent=" + string.Format("{0:0,,.##%}", 1234567.0));
        Console.WriteLine($"interpolation={1234000f:0,,.###}");
        // Decimal digit midpoints round away from zero after scaling.
        Console.WriteLine("midpoint long=" + 1234500L.ToString("0,"));
        Console.WriteLine("midpoint double=" + 2500.0.ToString("0,"));
        Console.WriteLine("midpoint fraction=" + 1250000.0.ToString("0,,.0"));
        Console.WriteLine("midpoint unscaled=" + 2.5.ToString("0"));
        Console.WriteLine("midpoint negative=" + (-2500.0).ToString("0,"));
        Console.WriteLine("midpoint float=" + 0.125f.ToString("0.00"));
        Console.WriteLine("midpoint percent=" + 0.00125.ToString("0.00%"));
        Console.WriteLine("midpoint exponent=" + 1.25.ToString("0.0E+0"));
        Console.WriteLine("exact long=" + long.MaxValue.ToString("#,##0"));
        Console.WriteLine("scaled ulong=" + ulong.MaxValue.ToString("0,"));
        // The sign follows rounding: integers and decimal have no negative zero,
        // and a section that prints nothing prints no sign.
        Console.WriteLine("negative int to zero=" + (-4).ToString("0,"));
        Console.WriteLine("negative int to nothing=[" + (-4).ToString("#,,") + "]");
        Console.WriteLine("negative short fraction=" + ((short)-4).ToString("0,.00"));
        Console.WriteLine($"negative interpolation={-4:0,}");
        Console.WriteLine("negative literal=" + ((sbyte)-4).ToString("'x'#,"));
        Console.WriteLine("negative double to zero=" + (-499.0).ToString("0,"));
        Console.WriteLine("negative double to nothing=[" + (-0.001).ToString("#") + "]");
        Console.WriteLine("negative double literal=" + (-0.001).ToString("'x'#"));
        Console.WriteLine("negative zero double=" + (-0.0).ToString("0"));
        Console.WriteLine("negative decimal to zero=" + (-0.001m).ToString("0.00"));
        // Decimal formats its exact digits.
        Console.WriteLine("decimal integer=" + 1234567890123456m.ToString("0"));
        Console.WriteLine("decimal fraction=" + 0.1234567890123456m.ToString("0.0000000000000000"));
        Console.WriteLine("decimal grouped=" + 12345678901234567890123.45m.ToString("#,##0.0"));
        Console.WriteLine("decimal scaled=" + 79228162514264337593543950335m.ToString("0,"));
        Console.WriteLine("decimal midpoint=" + (-1234567890123456789.5m).ToString("0"));
        Console.WriteLine("decimal optional=" + 1.50m.ToString("0.##"));
        Console.WriteLine("decimal exponent=" + 1234567890123456789012345678m.ToString("0.00E+00"));
        Console.WriteLine("Custom numeric comma scaling end");
    }

    internal static void __SectionMatrixGate()
    {
        Console.WriteLine("== Custom numeric sections and placeholders ==");
        Console.WriteLine("rounded section=" + (-4).ToString("0,;(0,)"));
        Console.WriteLine("empty negative=" + (-5).ToString("#;;z"));
        Console.WriteLine("fraction exponent=" + 0.5.ToString(".0E+0"));
        Console.WriteLine("mixed zero=" + 5.ToString("0#"));
        Console.WriteLine("integer literal=" + 1234.ToString("0x0"));
        Console.WriteLine("repeated point=" + 1.5.ToString("0..0"));
        Console.WriteLine("exponent width=" + 1234.ToString("0E+00000000000"));
        Console.WriteLine("embedded nul=[" + 5.ToString("0\u0000x") + "]");
        Console.WriteLine("nul negative section=[" + (-5).ToString("0;\u0000(0)") + "]");

        double[] values = { -1234.5, -5, -0.5, 0, 0.5, 5, 1234.5 };
        string[] formats = {
            "0,;(0,)", "#;;z", ".0E+0", "0#", "0x0", "0..0",
            "#0", "#0#", "00#", "0#0", "0'X'0", "#'X'#",
            ".#E+0", ".00E+00", "0.0.0", "0..#", "0.##;[0.##];'zero'",
            "0,000", "000,000.0", "#,##0.0E+0", "#,##0'x'0"
        };
        for (int f = 0; f < formats.Length; f++)
        {
            for (int v = 0; v < values.Length; v++)
                Console.WriteLine("matrix " + f + "/" + v + "=[" + values[v].ToString(formats[f]) + "]");
        }
        Console.WriteLine("Custom numeric sections and placeholders end");
    }

    internal static void __StandardNulGate()
    {
        Console.WriteLine("== Standard numeric NUL termination ==");
        string[] formats = {
            "\0", "\0x", "N\0", "N\02", "N2\0", "N2\0x",
            "F\0", "F1\0x", "0\0x", "0x\0", "N2x\0"
        };
        for (int f = 0; f < formats.Length; f++)
        {
            string format = formats[f];
            Console.WriteLine("matrix " + f + "/int=[" + 1234.ToString(format) + "]");
            Console.WriteLine("matrix " + f + "/double=[" + 1234.6.ToString(format) + "]");
            Console.WriteLine("matrix " + f + "/float=[" + 1234.6f.ToString(format) + "]");
            Console.WriteLine("matrix " + f + "/decimal=[" + 1234.6m.ToString(format) + "]");
        }
        Console.WriteLine("significant=[" + 1234.5.ToString("G3\0tail") + "]");
        Console.WriteLine("hex=[" + 255.ToString("X\0tail") + "]");
        Console.WriteLine("padded=[" + 5.ToString("D3\0tail") + "]");
        Console.WriteLine("decimal exact=[" + 1234567890123456789012345678m.ToString("\0tail") + "]");
        Console.WriteLine("negative decimal=[" + (-0.001m).ToString("F2\0tail") + "]");
        Console.WriteLine("float G0=[" + 0.1f.ToString("G0") + "]");
        Console.WriteLine("float G NUL=[" + 0.1f.ToString("G\0tail") + "]");
        Console.WriteLine("hex X0=[" + 0.ToString("X0") + "]");
        Console.WriteLine("hex X NUL=[" + 0.ToString("X\0tail") + "]");
        NumberFormatInfo symbols = new NumberFormatInfo { NaNSymbol = "NAN-E", NegativeSign = "NEG" };
        Console.WriteLine("float g0 NaN=[" + float.NaN.ToString("g0", symbols) + "]");
        Console.WriteLine("float g NUL NaN=[" + float.NaN.ToString("g\0tail", symbols) + "]");
        Console.WriteLine("float g0 negative=[" + (-1f).ToString("g0", symbols) + "]");
        Console.WriteLine("float g NUL negative=[" + (-1f).ToString("g\0tail", symbols) + "]");
        Console.WriteLine("float g0 exponent=[" + 1e20f.ToString("g0", symbols) + "]");
        Console.WriteLine("float g NUL exponent=[" + 1e20f.ToString("g\0tail", symbols) + "]");
        Console.WriteLine("float r NaN=[" + float.NaN.ToString("r", symbols) + "]");
        Console.WriteLine("double r NaN=[" + double.NaN.ToString("r", symbols) + "]");
        Console.WriteLine("float r negative=[" + (-1f).ToString("r", symbols) + "]");
        Console.WriteLine("double r negative=[" + (-1d).ToString("r", symbols) + "]");
        Console.WriteLine("Standard numeric NUL termination end");
        Console.Write("raw=[" + 5.ToString("N2\0tail") + "]\0END\n");
    }
}
