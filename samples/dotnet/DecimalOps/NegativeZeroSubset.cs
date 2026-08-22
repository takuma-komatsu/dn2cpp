using System;
using System.Globalization;

namespace NegativeZeroSubset;

internal static class Program
{
    // Parsed at run time: Roslyn folds constant decimal arithmetic — to +0 for
    // -1m + 1m, diverging from the runtime's own DecCalc — so literal operands
    // would never reach the runtime operators this section pins.
    private static decimal P(string s) => decimal.Parse(s, CultureInfo.InvariantCulture);

    internal static void Run()
    {
        Show("ctor", new decimal(0, 0, 0, true, 3));
        Show("parse", decimal.Parse("-0.000", CultureInfo.InvariantCulture));
        Show("negate", decimal.Negate(0.000m));
        Show("cancel", P("-1") + P("1"));
        Show("cancel-scales", P("1.50") - P("1.5"));
        Show("zero-mix", P("0") + new decimal(0, 0, 0, true, 3));
        Show("mul", P("-1") * P("0"));
        Show("div", P("0") / P("-1"));
        Show("rem", P("-3") % P("3"));
    }

    private static void Show(string name, decimal value)
    {
        int[] bits = decimal.GetBits(value);
        Console.WriteLine(name + ": " + bits[3].ToString("X8", CultureInfo.InvariantCulture)
            + ", " + decimal.IsNegative(value));
    }
}
