using System;
using System.Globalization;

namespace CanonicalLinkBound;

internal static class Program
{
    private static T Id<T>(T value) => value;

    private static void Main()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine(Id("canonical-link-bound"));
    }
}
