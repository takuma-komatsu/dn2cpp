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
        // A boxed decimal converts as the cast does.
        Console.WriteLine($"boxed 16777217.000000001: {BitConverter.SingleToInt32Bits(Convert.ToSingle((object)16777217.000000001m)):X8}");
        // Every operator rounds a tie to even, and an inexact quotient drops trailing zeros.
        decimal fromDouble = (decimal)double.Parse("0.3", ci);
        Console.WriteLine($"(decimal)0.3d / 2m: {(fromDouble / 2m).ToString(ci)}");
        Console.WriteLine($"(decimal)0.3d * 0.5m: {(fromDouble * 0.5m).ToString(ci)}");
        decimal part = (decimal)double.Parse("2.2", ci), total = (decimal)double.Parse("4.4", ci);
        Console.WriteLine($"(decimal)2.2d / (decimal)4.4d: {(part / total).ToString(ci)}");
        decimal price = (decimal)double.Parse("-1.23", ci);
        Console.WriteLine($"(decimal)-1.23d * 0: {(price * 0).ToString(ci)} {decimal.IsNegative(price * 0)}");
        decimal max = decimal.Parse("79228162514264337593543950335", ci);
        Console.WriteLine($"decimal.MaxValue / 10m + 0.06m: {(max / 10m + 0.06m).ToString(ci)}");
        decimal residual = (decimal)double.Parse("0.01", ci) / 0.01m - 1m;
        Console.WriteLine($"residual * residual: {(residual * residual).ToString(ci)}");
        Console.WriteLine("decimal exact float conversions end");
    }

    // The program's only use of Round(value, digits, mode) takes its address.
    internal static void RunRoundDelegate()
    {
        Func<double, int, MidpointRounding, double> round = Math.Round;
        Console.WriteLine("Math.Round delegate=" + round(2.125, 2, MidpointRounding.ToEven).ToString(CultureInfo.InvariantCulture));
    }
}
