using System;
using System.Globalization;
using System.Runtime.InteropServices;

namespace ConvertParse;

internal static class NumericTailSubset
{
    private static void Show<T>(string tag, Func<T> fn)
    {
        try { Console.WriteLine(tag + " = " + fn()); }
        catch (Exception ex) { Console.WriteLine(tag + " ! " + ex.GetType().Name); }
    }

    internal static void RunFloatFormat()
    {
        var inv = CultureInfo.InvariantCulture;
        Console.WriteLine("== float hex and format ==");
        // Hexadecimal floating point: exact values, a subnormal, ties at the last
        // retained bit, a required exponent, and the "X" format that writes it.
        foreach (string hx in new[] { "0x1.8p+1", "-0x1p-2", "0x0.0000000000001p-1022", "0x1.00000000000008p0", "0x1.00000000000018p0", "0x1.fffffffffffff8p1023" })
            Show("dbl[" + hx + " HexFloat]", () => double.Parse(hx, NumberStyles.HexFloat, inv).ToString("R", inv));
        foreach (string hx in new[] { "0x1.000001p0", "0x1.000003p0", "0x0.000002p-126", "0x1p128" })
            Show("flt[" + hx + " HexFloat]", () => float.Parse(hx, NumberStyles.HexFloat, inv).ToString("R", inv));
        Show("dbl[0x1.8 no-exp]", () => double.Parse("0x1.8", NumberStyles.HexFloat, inv));          // FormatException
        Show("flt[0x1.8 no-exp]", () => { float q; return float.TryParse("0x1.8", NumberStyles.HexFloat, inv, out q); }); // False
        foreach (double xv in new[] { 1d, -0.5, 1.1, 0d, double.Epsilon })
            Console.WriteLine(xv.ToString("X", inv) + " " + xv.ToString("x3", inv) + " " + xv.ToString("X0", inv));
        Console.WriteLine((1.1f).ToString("X", inv) + " " + (1.1f).ToString("x2", inv) + " " + (-float.Epsilon).ToString("X8", inv));
        Show("dbl[X1000000000]", () => 1d.ToString("X1000000000", inv));    // FormatException
        Show("flt[F1000000000]", () => 1f.ToString("F1000000000", inv));    // FormatException
        // The digit accumulator must not wrap (4294967297 wraps to 1), a
        // trailing character does not hide the oversize precision, leading
        // zeros are free, and NaN/Infinity ignore the format entirely.
        Show("dbl[X4294967297]", () => 1d.ToString("X4294967297", inv));    // FormatException
        Show("dbl[X1000000000!]", () => 1d.ToString("X1000000000!", inv));  // FormatException
        Show("dbl[X0000000000001]", () => 1d.ToString("X0000000000001", inv));
        Show("int[D0000000000003]", () => 5.ToString("D0000000000003", inv));
        Show("dbl[NaN X1000000000]", () => double.NaN.ToString("X1000000000", inv));
        Show("flt[NaN E4294967297]", () => float.NaN.ToString("E4294967297", inv));
        Show("dbl[Inf Q5]", () => double.PositiveInfinity.ToString("Q5", inv));
        try { 1d.ToString("X1000000000", inv); }
        catch (FormatException ex) { Console.WriteLine("dbl[X1000000000] message = " + ex.Message); }
        Console.WriteLine("float hex and format end");
    }

    internal static void RunParseRows()
    {
        Console.WriteLine("== currency separators and Base64 tails ==");
        var nfi = (NumberFormatInfo)NumberFormatInfo.InvariantInfo.Clone();
        nfi.CurrencySymbol = " ";
        nfi.CurrencyGroupSeparator = " ";
        nfi.NumberGroupSeparator = " ";
        Show("currency group same text", () => int.Parse("1 234", NumberStyles.Currency, nfi));
        nfi.CurrencyGroupSeparator = "\u00A0";
        nfi.NumberGroupSeparator = "\u00A0";
        Show("currency group folded text", () => int.Parse("1\u00A0234", NumberStyles.Currency, nfi));
        Show("fbs pad bits 2", () => Convert.FromBase64String("AB=="));
        Show("fbs pad bits 1", () => Convert.FromBase64String("AAB="));
        Show("tfbc alias count", () =>
        {
            char[] input = " QUJD".ToCharArray();
            ref int count = ref MemoryMarshal.Cast<char, int>(input.AsSpan(2, 2))[0];
            byte[] d = new byte[3];
            bool ok = Convert.TryFromBase64Chars(input, d, out count);
            return ok + ":" + count + ":" + BitConverter.ToString(d);
        });
        Console.WriteLine("currency separators and Base64 tails end");
    }
}
