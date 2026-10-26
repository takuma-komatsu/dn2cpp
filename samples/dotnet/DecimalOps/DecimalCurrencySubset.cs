using System;

namespace Dn2Cpp;

internal static class DecimalCurrencySubset
{
    public static void Run()
    {
        Console.WriteLine("== decimal OLE currency ==");
        Func<decimal, long> toCurrency = decimal.ToOACurrency;
        Func<long, decimal> fromCurrency = decimal.FromOACurrency;
        foreach (string text in new[]
        {
            "0", "1", "-1", "0.00004", "0.00005", "0.00006", "0.00015", "0.00025",
            "-0.00004", "-0.00005", "-0.00006", "-0.00015", "-0.00025",
            "1.23445", "1.23455", "-1.23445", "-1.23455",
            "0.0000500000000000000000000001", "-0.0000500000000000000000000001",
            "0.0000499999999999999999999999", "-0.0000499999999999999999999999",
            "922337203685477.5806", "922337203685477.5807", "922337203685477.58074",
            "922337203685477.58075", "922337203685477.5808", "922337203685478",
            "-922337203685477.5807", "-922337203685477.5808", "-922337203685477.58085",
            "-922337203685477.5808500000001", "-922337203685477.5809", "-922337203685478",
            "79228162514264337593543950335", "-79228162514264337593543950335"
        })
            ShowBoth(text, decimal.Parse(text), toCurrency);
        foreach (bool negative in new[] { false, true })
            for (byte scale = 0; scale <= 28; scale++)
            {
                string label = "bits:" + negative + ":" + scale;
                ShowBoth(label + ":zero", new decimal(0, 0, 0, negative, scale), toCurrency);
                ShowBoth(label + ":96", new decimal(-1, -1, -1, negative, scale), toCurrency);
            }
        foreach (long value in new[]
        {
            0L, 1, -1, 10, -10, 100, -100, 1000, -1000, 10000, -10000,
            1234500, -1234500, long.MaxValue, long.MinValue, long.MaxValue - 1, long.MinValue + 1
        })
        {
            decimal direct = decimal.FromOACurrency(value);
            decimal indirect = fromCurrency(value);
            Console.WriteLine("from direct:" + value + "=" + Words(direct) + ":" + decimal.ToOACurrency(direct));
            Console.WriteLine("from delegate:" + value + "=" + Words(indirect) + ":" + toCurrency(indirect));
        }
        Console.WriteLine("decimal OLE currency end");
    }

    private static void ShowBoth(string label, decimal value, Func<decimal, long> convert)
    {
        Show("to direct:" + label, () => decimal.ToOACurrency(value));
        Show("to delegate:" + label, () => convert(value));
    }

    private static string Words(decimal value) => string.Join(",", decimal.GetBits(value));

    private static void Show(string label, Func<long> action)
    {
        try { Console.WriteLine(label + "=" + action()); }
        catch (OverflowException exception)
        {
            Console.WriteLine(label + "=" + exception.GetType().Name + ":" + exception.HResult.ToString("X8")
                + ":" + exception.Message);
        }
    }
}
