using System;

namespace EnumParseSpanSubset;

internal enum Color
{
    Red = 1,
    Green = 2,
    Blue = 3,
}

[Flags]
internal enum Perm : byte
{
    None = 0,
    Read = 1,
    Write = 2,
    Exec = 4,
}

internal enum Wide : long
{
    Small = 1,
    Big = 1L << 40,
}

// Span overloads must parse only the active slice. The no-bool TryParse
// form is the call shape used by System.Text.Json's enum converter.
internal static class Program
{
    private static void TryColor(string text, bool ignoreCase)
    {
        ReadOnlySpan<char> span = text.AsSpan();
        bool ok = Enum.TryParse<Color>(span, ignoreCase, out Color value);
        Console.WriteLine($"TryParse<Color>(\"{text}\", {ignoreCase}) = {ok} {value}");
    }

    private static void TryPerm(string text, bool ignoreCase)
    {
        ReadOnlySpan<char> span = text.AsSpan();
        bool ok = Enum.TryParse<Perm>(span, ignoreCase, out Perm value);
        Console.WriteLine($"TryParse<Perm>(\"{text}\", {ignoreCase}) = {ok} {value}");
    }

    private static void TryWide(string text, bool ignoreCase)
    {
        ReadOnlySpan<char> span = text.AsSpan();
        bool ok = Enum.TryParse<Wide>(span, ignoreCase, out Wide value);
        Console.WriteLine($"TryParse<Wide>(\"{text}\", {ignoreCase}) = {ok} {value}");
    }

    private static void ParseColor(string text)
    {
        ReadOnlySpan<char> span = text.AsSpan();
        try
        {
            Console.WriteLine($"Parse<Color>(\"{text}\") = {Enum.Parse<Color>(span)}");
        }
        catch (ArgumentException)
        {
            Console.WriteLine($"Parse<Color>(\"{text}\") threw ArgumentException");
        }
    }

    internal static void __GateEntry()
    {
        TryColor("Green", false);
        TryColor("green", false);
        TryColor("green", true);
        TryColor("2", false);
        TryColor("7", false);
        TryColor("Purple", true);
        TryPerm("Read, Exec", false);
        TryPerm("read,write", true);
        TryPerm("6", false);
        TryWide("Big", false);
        TryWide("1099511627776", false);
        TryColor("", false);
        ParseColor("Blue");
        ParseColor("blue");

        ReadOnlySpan<char> write = "Write".AsSpan();
        Console.WriteLine($"Parse<Perm>(\"Write\") = {Enum.Parse<Perm>(write)}");
        ReadOnlySpan<char> small = "Small".AsSpan();
        Console.WriteLine($"Parse<Wide>(\"Small\") = {Enum.Parse<Wide>(small)}");

        // Both bool-bearing Parse and no-bool TryParse must dispatch by
        // their actual signatures, not by another overload's parameter count.
        ReadOnlySpan<char> lower = "blue".AsSpan();
        Console.WriteLine($"Parse<Color>(\"blue\", true) = {Enum.Parse<Color>(lower, true)}");
        ReadOnlySpan<char> sliced = "xxBluexx".AsSpan(2, 4);
        bool ok = Enum.TryParse<Color>(sliced, out Color fromSlice);
        Console.WriteLine(ok + " " + fromSlice);
    }
}
