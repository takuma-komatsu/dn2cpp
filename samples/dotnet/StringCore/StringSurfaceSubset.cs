#nullable disable
using System;

// EndsWith(char) / StartsWith(char), EndsWith/StartsWith(string, StringComparison)
// under Ordinal and the exact BMP OrdinalIgnoreCase fold, and LastIndexOf(char).
namespace StringSurfaceSubset;

internal static class Program
{internal static int Run()
    {
        // char overloads (false for empty string).
        Console.WriteLine($"endChar={"abc".EndsWith('c')}/{"abc".EndsWith('x')}/{"".EndsWith('x')}");
        Console.WriteLine($"startChar={"abc".StartsWith('a')}/{"abc".StartsWith('x')}/{"".StartsWith('x')}");

        // (string, StringComparison) — Ordinal vs OrdinalIgnoreCase, incl. empty suffix.
        Console.WriteLine($"endOrd={"file.DLL".EndsWith(".dll", StringComparison.Ordinal)}");
        Console.WriteLine($"endIC={"file.DLL".EndsWith(".dll", StringComparison.OrdinalIgnoreCase)}");
        Console.WriteLine($"endEmpty={"abc".EndsWith("", StringComparison.Ordinal)}");
        Console.WriteLine($"endLong={"ab".EndsWith("xab", StringComparison.Ordinal)}");
        Console.WriteLine($"startOrd={"__Foo".StartsWith("__", StringComparison.Ordinal)}/{"Foo".StartsWith("__", StringComparison.Ordinal)}");
        Console.WriteLine($"startIC={"FOO_bar".StartsWith("foo", StringComparison.OrdinalIgnoreCase)}");

        // Non-ASCII OrdinalIgnoreCase (exact BMP fold).
        Console.WriteLine($"startNonAscii={"Ärger".StartsWith("ärg", StringComparison.OrdinalIgnoreCase)}");
        Console.WriteLine($"endNonAscii={"file.ÄPP".EndsWith(".äpp", StringComparison.OrdinalIgnoreCase)}");

        // LastIndexOf(char).
        Console.WriteLine($"lastIdx={"a/b/c".LastIndexOf('/')}/{"abcabc".LastIndexOf('b')}/{"abc".LastIndexOf('z')}/{"".LastIndexOf('z')}");
        Console.WriteLine($"lastParen={"Foo(int)".LastIndexOf(')')}");

        return 0;
    }
}
