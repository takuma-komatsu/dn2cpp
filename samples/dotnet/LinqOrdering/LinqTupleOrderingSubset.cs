#nullable enable
using System;
using System.Linq;

namespace LinqTupleOrderingSubset;

internal static class Program
{
    internal static void Run()
    {
        Console.WriteLine("== tuple ordering generic boundaries ==");
        // The two-key iterator over a pair and the base iterator over a triple
        // must retain distinct layouts after reference arguments canonicalize.
        var pairs = new[] { ("b", "x"), ("a", "z"), ("a", "y") };
        foreach (var row in pairs.OrderBy(row => row.Item1).ThenBy(row => row.Item2))
            Console.WriteLine("pair " + row.Item1 + "/" + row.Item2);

        var triples = new[] { ("b", "x", "2"), ("a", "z", "1"), ("a", "y", "3") };
        foreach (var row in triples.OrderBy(row => row.Item1).ThenByDescending(row => row.Item2))
            Console.WriteLine("triple " + row.Item1 + "/" + row.Item2 + "/" + row.Item3);

        // The same leaves grouped at different depths need separate argument keys.
        var left = new[] { (("b", "x"), "2"), (("a", "y"), "1") };
        foreach (var row in left.OrderBy(row => row.Item1.Item1).ThenBy(row => row.Item2))
            Console.WriteLine("left " + row.Item1.Item1 + "/" + row.Item1.Item2 + "/" + row.Item2);
        var right = new[] { ("b", ("x", "2")), ("a", ("y", "1")) };
        foreach (var row in right.OrderBy(row => row.Item1).ThenBy(row => row.Item2.Item1))
            Console.WriteLine("right " + row.Item1 + "/" + row.Item2.Item1 + "/" + row.Item2.Item2);
        Console.WriteLine("tuple ordering generic boundaries end");
    }
}
