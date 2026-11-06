using System;
using System.Globalization;

namespace DateTimeFamily;

internal static class Entry
{
    private static void RunAll() => throw new InvalidOperationException("family fixture was not rewritten");

    private static void Main()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        RunAll();
        Console.WriteLine("DateTime family aggregate end");
    }
}
