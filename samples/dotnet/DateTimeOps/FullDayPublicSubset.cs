using System;
using System.Globalization;

namespace FullDayPublicSubset;

internal static class Program
{
    public static void Run()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("== Full invariant day public neighbor ==");
        string[] expected = { "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" };
        for (int day = 0; day < expected.Length; day++)
        {
            bool success = DateTimeFormatInfo.InvariantInfo.GetDayName((DayOfWeek)day) == expected[day];
            Console.WriteLine("full-day-public-" + day + ":" + success);
            if (!success) throw new InvalidOperationException("full day public value");
        }
        Console.WriteLine("Full invariant day public neighbor end");
    }
}
