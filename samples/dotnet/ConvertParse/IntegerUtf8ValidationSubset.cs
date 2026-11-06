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
