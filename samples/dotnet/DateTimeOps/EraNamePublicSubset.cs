using System;
using System.Globalization;

namespace EraNamePublicSubset;

internal static class Program
{
    public static void Run()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("== Invariant era public neighbor ==");
        for (int era = 0; era <= 1; era++)
        {
            bool correct = DateTimeFormatInfo.InvariantInfo.GetEraName(era) == "A.D.";
            Console.WriteLine("era-name-public-" + era + ":" + correct);
            if (!correct) throw new InvalidOperationException("invariant era name");
        }
        Console.WriteLine("Invariant era public neighbor end");
    }
}
