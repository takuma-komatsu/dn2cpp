using System;
using System.Globalization;

namespace FullMonthPublicSubset;

internal static class Program
{
    public static void Run()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("== Full invariant month public neighbor ==");
        string[] expected = { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December", "" };
        for (int month = 1; month <= 13; month++)
        {
            bool correct = DateTimeFormatInfo.InvariantInfo.GetMonthName(month) == expected[month - 1];
            Console.WriteLine("full-month-public-" + month + ":" + correct);
            if (!correct) throw new InvalidOperationException("invariant month name");
        }
        bool empty = DateTimeFormatInfo.InvariantInfo.GetMonthName(13).Length == 0;
        Console.WriteLine("full-month-public-empty:" + empty);
        if (!empty) throw new InvalidOperationException("thirteenth month must be empty");
        Console.WriteLine("Full invariant month public neighbor end");
    }
}
