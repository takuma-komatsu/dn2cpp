using System;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace StringComparisonCall;

internal static class Program
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool DirectEquals(string receiver, string other, StringComparison comparison)
    {
        return receiver.Equals(other, comparison);
    }

    private static void Show(string label, Func<bool> body)
    {
        try
        {
            Console.WriteLine(label + "=" + body());
        }
        catch (Exception ex)
        {
            Console.WriteLine(label + "=" + ex.GetType().Name + ":" + ex.Message);
        }
    }

    private static void Main()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Show("direct both null valid", () => DirectEquals(null, null, StringComparison.Ordinal));
        Show("direct both null invalid", () => DirectEquals(null, null, (StringComparison)6));
        Show("direct same invalid", () => DirectEquals("a", "a", (StringComparison)6));
        Show("direct ignore case", () => DirectEquals("a", "A", StringComparison.OrdinalIgnoreCase));
        string absent = null;
        Show("virtual both null valid", () => absent.Equals(null, StringComparison.Ordinal));
        Show("virtual both null invalid", () => absent.Equals(null, (StringComparison)6));
    }
}
