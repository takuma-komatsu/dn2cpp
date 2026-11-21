using System;
using System.Globalization;

namespace ConvertParse;

internal static class IntegerUtf8ValidationSubset
{
    internal static void Run()
    {
        Console.WriteLine("== integer UTF8 validation ==");
        var format = new NumberFormatInfo { NegativeSign = "\uFFFD" };
        byte[] valid = [0xEF, 0xBF, 0xBD, 0x31];
        Console.WriteLine("valid replacement sign=" + int.TryParse(valid, NumberStyles.Integer, format, out int validValue)
            + ":" + validValue);
        Check("illegal lead", [0xFF, 0x31], format);
        Check("continuation", [0x80, 0x31], format);
        Check("truncated", [0xE2, 0x31], format);
        Check("overlong", [0xC0, 0xAF, 0x31], format);
        Check("surrogate", [0xED, 0xA0, 0x80, 0x31], format);
        Check("out of range", [0xF4, 0x90, 0x80, 0x80, 0x31], format);
        try
        {
            int.TryParse(new byte[] { 0xFF }, (NumberStyles)0x8000, format, out _);
            Console.WriteLine("invalid style=missing fault");
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine("invalid style=" + ex.GetType().Name + ":" + ex.ParamName);
        }
        Console.WriteLine("integer UTF8 validation end");
    }

    // The float entries are byte-domain too: invalid bytes never match a
    // separator, a special-value symbol must match in as many bytes as it
    // occupies, whatever its case variant's UTF-16 length, and an unpaired
    // surrogate in a separator is the U+FFFD its UTF-8 encoding holds.
    internal static void RunFloat()
    {
        Console.WriteLine("== float UTF8 ==");
        var sep = new NumberFormatInfo { NumberDecimalSeparator = "\uFFFD" };
        FloatCheck("hex invalid separator", [0x30, 0x78, 0x31, 0xFF, 0x38, 0x70, 0x30], NumberStyles.HexFloat, sep);
        FloatCheck("hex valid separator", [0x30, 0x78, 0x31, 0xEF, 0xBF, 0xBD, 0x38, 0x70, 0x30], NumberStyles.HexFloat, sep);
        FloatCheck("decimal invalid separator", [0x31, 0xFF, 0x35], NumberStyles.Float, sep);
        FloatCheck("invalid symbol", [0xFF], NumberStyles.Float, new NumberFormatInfo { NaNSymbol = "\uFFFD" });
        string wide = "\u2C65", narrow = "\u023A";
        FloatSymbol("nan shorter variant", new NumberFormatInfo { NaNSymbol = wide }, narrow);
        FloatSymbol("nan longer variant", new NumberFormatInfo { NaNSymbol = narrow }, wide);
        FloatSymbol("signed infinity shorter variant", new NumberFormatInfo { PositiveInfinitySymbol = wide }, "+" + narrow);
        FloatSymbol("negative infinity shorter variant", new NumberFormatInfo { NegativeInfinitySymbol = wide }, narrow + " ");
        FloatSymbol("same-length variant", new NumberFormatInfo { NaNSymbol = "\u00FF" }, "\u0178");
        FloatSymbol("equal total length", new NumberFormatInfo { NaNSymbol = narrow + wide }, wide + narrow);
        FloatSymbol("signed same-length variant", new NumberFormatInfo { PositiveInfinitySymbol = "\u0178" }, "+\u00FF");
        FloatSymbol("kelvin sign", new NumberFormatInfo { NaNSymbol = "\u212A" }, "k");
        var unpaired = new NumberFormatInfo { NumberDecimalSeparator = "\uD800" };
        FloatCheck("unpaired surrogate separator", [0x31, 0xEF, 0xBF, 0xBD, 0x35], NumberStyles.Float, unpaired);
        FloatCheck("hex unpaired surrogate separator", [0x30, 0x78, 0x31, 0xEF, 0xBF, 0xBD, 0x38, 0x70, 0x30],
            NumberStyles.HexFloat, unpaired);
        DecimalCheck("decimal type invalid byte", [0x31, 0xFF, 0x35], unpaired);
    }

    private static void DecimalCheck(string name, byte[] bytes, NumberFormatInfo format)
    {
        Console.WriteLine(name + " try=" + decimal.TryParse(bytes, NumberStyles.Float, format, out decimal value) + ":" + value);
        try
        {
            Console.WriteLine(name + " parse=" + decimal.Parse(bytes, NumberStyles.Float, format));
        }
        catch (FormatException ex)
        {
            Console.WriteLine(name + " parse=" + ex.GetType().Name + ":" + ex.Message);
        }
    }

    private static void FloatSymbol(string name, NumberFormatInfo format, string input)
    {
        FloatCheck(name, System.Text.Encoding.UTF8.GetBytes(input), NumberStyles.Float, format);
        Console.WriteLine(name + " string=" + double.TryParse(input, NumberStyles.Float, format, out double value) + ":" + value);
    }

    private static void FloatCheck(string name, byte[] bytes, NumberStyles styles, NumberFormatInfo format)
    {
        Console.WriteLine(name + " try=" + double.TryParse(bytes, styles, format, out double value) + ":" + value
            + " single=" + float.TryParse(bytes, styles, format, out float single) + ":" + single);
        try
        {
            Console.WriteLine(name + " parse=" + double.Parse(bytes, styles, format));
        }
        catch (FormatException ex)
        {
            Console.WriteLine(name + " parse=" + ex.GetType().Name + ":" + ex.Message);
        }
    }

    private static void Check(string name, byte[] bytes, NumberFormatInfo format)
    {
        Console.WriteLine(name + " try=" + int.TryParse(bytes, NumberStyles.Integer, format, out int value) + ":" + value);
        try
        {
            Console.WriteLine(name + " parse=" + int.Parse(bytes, NumberStyles.Integer, format));
        }
        catch (FormatException ex)
        {
            Console.WriteLine(name + " parse=" + ex.GetType().Name + ":" + ex.Message);
        }
    }
}
