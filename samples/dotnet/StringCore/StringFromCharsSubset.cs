#nullable enable
using System;
namespace StringFromCharsSubset;


static class Program
{internal static void Run()
    {
        string s = "hello world";

        // ReadOnlySpan<char>.ToString() -> string (its body is new string(this)).
        Console.WriteLine(s.AsSpan(6).ToString());        // world
        Console.WriteLine(s.AsSpan(0, 5).ToString());     // hello

        // new string(ReadOnlySpan<char>) and from a Span<char>.
        Console.WriteLine(new string(s.AsSpan(0, 5)));    // hello
        Span<char> sc = new char[] { 'S', 'p', 'a', 'n' };
        Console.WriteLine(new string(sc));                // Span

        // new string(char[]) and new string(char[], start, length).
        char[] arr = { 'a', 'b', 'c', 'd', 'e' };
        Console.WriteLine(new string(arr));               // abcde
        Console.WriteLine(new string(arr, 1, 3));         // bcd

        // new string(char, count).
        Console.WriteLine(new string('x', 5));            // xxxxx
        Console.WriteLine(new string('-', 0));            // (empty)

        // The constructed string is independent: mutating the source array afterward
        // must not change it (proves the copy, not an alias).
        char[] m = { 'A', 'B', 'C' };
        string copy = new string(m);
        m[0] = 'Z';
        Console.WriteLine(copy + "/" + new string(m));    // ABC/ZBC

        // Round-trips compose with other string ops.
        Console.WriteLine(new string(s.AsSpan(0, 5)).ToUpper()); // HELLO
        Console.WriteLine(("[" + new string("".AsSpan()) + "]")); // []
    }
}
