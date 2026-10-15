using System;
using System.Globalization;

namespace CharText;

internal static class StringHashComparisonSubset
{
    internal static void Run()
    {
        Console.WriteLine("== String comparison hashes ==");
        // Hash values are randomized on CLR; compare equalities within each process.
        foreach (string culture in new[] { "en-US", "tr-TR" })
        {
            CultureInfo.CurrentCulture = new CultureInfo(culture);
            for (int i = 0; i < 6; i++)
            {
                var comparison = (StringComparison)i;
                string value = "AbC";
                int hash = value.GetHashCode(comparison);
                Console.WriteLine("hash " + culture + ":" + i + "="
                    + (hash == value.GetHashCode(comparison)) + ":"
                    + (hash == string.GetHashCode(value.AsSpan(), comparison)) + ":"
                    + ((i & 1) == 0 || hash == "aBc".GetHashCode(comparison)) + ":"
                    + ("".GetHashCode(comparison) == string.GetHashCode("".AsSpan(), comparison)));
            }
        }
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        Console.WriteLine("unicode ordinal ignore case=" +
            ("éσ".GetHashCode(StringComparison.OrdinalIgnoreCase) == "ÉΣ".GetHashCode(StringComparison.OrdinalIgnoreCase)));
        foreach (int invalid in new[] { -1, 6, int.MinValue, int.MaxValue })
        {
            Fault("abc", invalid);
            Fault("", invalid);
        }
        Fault(null!, 4);
        Fault(null!, 6);
        Console.WriteLine("String comparison hashes end");
    }

    private static void Fault(string value, int comparison)
    {
        try
        {
            _ = value.GetHashCode((StringComparison)comparison);
            Console.WriteLine("hash fault missing");
        }
        catch (ArgumentException exception)
        {
            Console.WriteLine("hash fault=" + (value.Length == 0 ? "empty:" : "text:") + comparison + ":" + exception.GetType().Name + ":" + exception.ParamName);
        }
        catch (NullReferenceException)
        {
            Console.WriteLine("hash null=" + comparison);
        }
    }
}
