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

    // The patcher replaces these carrier bodies with the exact CoreLib calls.
    private static bool DirectObjectEquals(string receiver, object other) => throw new NotSupportedException();
    private static string DirectToString(string receiver) => throw new NotSupportedException();
    private static string DirectTrim(string receiver, ReadOnlySpan<char> set) => throw new NotSupportedException();
    private static string DirectTrimStart(string receiver, ReadOnlySpan<char> set) => throw new NotSupportedException();
    private static string DirectTrimEnd(string receiver, ReadOnlySpan<char> set) => throw new NotSupportedException();

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

    private static void Main(string[] args) => PatchStringCalls.Apply(args);

    private static void Run()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Show("direct both null valid", () => DirectEquals(null, null, StringComparison.Ordinal));
        Show("direct both null invalid", () => DirectEquals(null, null, (StringComparison)6));
        Show("direct same invalid", () => DirectEquals("a", "a", (StringComparison)6));
        Show("direct ignore case", () => DirectEquals("a", "A", StringComparison.OrdinalIgnoreCase));
        string absent = null;
        Console.WriteLine("direct String overrides=" + DirectObjectEquals("abc", (object)"abc") + ":" +
            DirectObjectEquals("abc", 1) + ":" + DirectObjectEquals(null, null) + ":" + DirectToString("abc") + ":" +
            (DirectToString(null) is null));
        ReadOnlySpan<char> set = "ab";
        Console.WriteLine("direct span trim=" + DirectTrim("abbaXba", set) + ":" +
            DirectTrimStart("abbaXba", set) + ":" + DirectTrimEnd("abbaXba", set) + ":" +
            DirectTrim(" X ", ReadOnlySpan<char>.Empty));
        Show("virtual both null valid", () => absent.Equals(null, StringComparison.Ordinal));
        Show("virtual both null invalid", () => absent.Equals(null, (StringComparison)6));
    }
}
