using System;
using System.Globalization;

namespace Dn2Cpp;

internal static class DecimalFloatSubset
{
    internal static void Run()
    {
        var ci = CultureInfo.InvariantCulture;
        Console.WriteLine("== decimal exact float conversions ==");
        // Both conversions round the exact value once, directly to the destination format.
        foreach (string text in new[] { "1000000000000000.1", "16777217.000000001", "9007199254740995", "16777219", "-0.00", "-123456789.123456789012345678", "79228162514264337593543950335", "0.0000000000000000000000000001" })
        {
            decimal d = decimal.Parse(text, ci);
            Console.WriteLine($"{text}: {BitConverter.DoubleToInt64Bits((double)d):X16} {BitConverter.SingleToInt32Bits((float)d):X8}");
        }
        // A boxed decimal reaches Single through the same single rounding.
        object boxed = decimal.Parse("16777217.000000001", ci);
        Console.WriteLine($"boxed: {BitConverter.SingleToInt32Bits(Convert.ToSingle(boxed)):X8} "
            + $"{BitConverter.SingleToInt32Bits((float)Convert.ChangeType(boxed, typeof(float), ci)):X8} "
            + $"{BitConverter.SingleToInt32Bits(((IConvertible)boxed).ToSingle(ci)):X8}");
        Console.WriteLine("decimal exact float conversions end");
    }
}
