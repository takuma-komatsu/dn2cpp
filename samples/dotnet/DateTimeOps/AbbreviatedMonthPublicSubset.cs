using System;
using System.Globalization;

namespace AbbreviatedMonthPublicSubset;

internal static class Program
{
    public static void Run()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("== Abbreviated invariant month public neighbor ==");
        string[] expected = { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec", "" };
        for (int month = 1; month <= 13; month++)
        {
            bool correct = DateTimeFormatInfo.InvariantInfo.GetAbbreviatedMonthName(month) == expected[month - 1];
            Console.WriteLine("abbreviated-month-public-" + month + ":" + correct);
            if (!correct) throw new InvalidOperationException("invariant month name");
        }
        bool empty = DateTimeFormatInfo.InvariantInfo.GetAbbreviatedMonthName(13).Length == 0;
        Console.WriteLine("abbreviated-month-public-empty:" + empty);
        if (!empty) throw new InvalidOperationException("thirteenth month must be empty");
        Console.WriteLine("Abbreviated invariant month public neighbor end");
    }
}
