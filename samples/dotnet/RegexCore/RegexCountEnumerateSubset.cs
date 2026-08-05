#nullable enable
using System;
using System.Text.RegularExpressions;

// SUBJECT: Regex.Count, the allocation-free EnumerateMatches (ref-struct
// ValueMatch enumerator over a ReadOnlySpan<char>), and span-input IsMatch.
namespace RegexCountEnumerateSubset;

static class Program
{
    internal static void __GateEntry()
    {
        Console.WriteLine("-- Count --");
        Console.WriteLine(Regex.Count("a1 b2 c3", @"\d"));
        Console.WriteLine(new Regex("[aeiou]").Count("sequential"));
        Console.WriteLine(Regex.Count("none", @"\d"));

        Console.WriteLine("-- EnumerateMatches --");
        ReadOnlySpan<char> span = "cat cow dog crow".AsSpan();
        foreach (ValueMatch vm in Regex.EnumerateMatches(span, @"c\w+"))
            Console.WriteLine(vm.Index + ":" + vm.Length);

        Console.WriteLine("-- span IsMatch --");
        Console.WriteLine(Regex.IsMatch("abc 123".AsSpan(), @"\d+"));
        Console.WriteLine(new Regex(@"\d+").IsMatch("no digits".AsSpan()));
    }
}
