#nullable enable
// Enumerable.OfType<TResult> over a non-generic IEnumerable.
//
// Exercises the three source shapes that drive the non-generic
// System.Collections.IEnumerable::GetEnumerator direct-callvirt dispatch:
//   (a) object[]  array
//   (b) List<object>  managed collection
//   (c) LINQ iterator  xs.Select(...).OfType<T>()  (the real self-host shape)
// plus boxed value-type exact matching, null filtering, order/dup preservation.
//
// Compiled against the framework System.Linq, which the gate also passes via
// -r, so OfType transpiles from the real BCL IL.

using System;
using System.Collections.Generic;
using System.Linq;
namespace LinqOfTypeSubset;


internal static class Program
{
    internal static void Run()
    {
        // (a) object[] array source: strings, boxed ints, null, other types.
        object?[] arr = new object?[] { "a", 1, null, "b", 2, 3.5, "c", (byte)1, 1L, 1 };
        Console.WriteLine("-- (a) object[] OfType<string> --");
        foreach (string s in arr.OfType<string>())
            Console.WriteLine(s);
        Console.WriteLine("-- (a) object[] OfType<int> (boxed int exact only) --");
        foreach (int i in arr.OfType<int>())
            Console.WriteLine(i);

        // (b) List<object> managed collection source.
        List<object?> list = new List<object?> { "x", null, 10, "y", (object)20, "z" };
        Console.WriteLine("-- (b) List<object> OfType<string> --");
        foreach (string s in list.OfType<string>())
            Console.WriteLine(s);

        // (c) LINQ iterator source: Select(...).OfType<T>() — the self-host shape.
        IEnumerable<object?> proj = new object?[] { 1, 2, 3, 4 }
            .Select<object?, object?>(x => (int)x! % 2 == 0 ? "even" + x : (object?)x);
        Console.WriteLine("-- (c) Select(...).OfType<string> --");
        foreach (string s in proj.OfType<string>())
            Console.WriteLine(s);

        // boxed value-type exact match: byte picks only boxed byte (not int/long).
        object?[] mix = new object?[] { (byte)1, 1, (byte)2, 2L, (byte)3 };
        Console.WriteLine("-- boxed-byte exact OfType<byte> --");
        foreach (byte b in mix.OfType<byte>())
            Console.WriteLine(b);

        // null filtering + order + duplicate preservation.
        object?[] dups = new object?[] { "p", "p", null, "q", "p" };
        Console.WriteLine("-- null filter + order + dup OfType<string> --");
        foreach (string s in dups.OfType<string>())
            Console.WriteLine(s);

        // Count via the deferred query (re-enumerates; proves lazy semantics).
        Console.WriteLine("count strings in (a): " + arr.OfType<string>().Count());
    }
}
