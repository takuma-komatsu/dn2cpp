#nullable disable
using System;
using System.Collections.Generic;

namespace ConcatJoinListSubset
{
    // string.Join / string.Concat over a List<T>. The transpiler materializes only the
    // live prefix of the backing array (_items iterated to _size), so capacity beyond
    // Count is never emitted.
    internal static class Program
    {
        internal static void __GateEntry()
        {
            // Capacity (16) deliberately exceeds Count (3): Join must emit 3 elements,
            // not the zero-filled tail of the backing array.
            var ints = new List<int>(16) { 1, 2, 3 };
            Console.WriteLine(string.Join(",", ints));   // 1,2,3

            var longs = new List<long> { 10L, 20L };
            Console.WriteLine(string.Join("-", longs));  // 10-20

            var dbls = new List<double> { 0.5, 1.5 };
            Console.WriteLine(string.Join("|", dbls));   // 0.5|1.5

            var empty = new List<int>();
            Console.WriteLine(string.Join(",", empty));  // (empty line — kept mid-output)

            var strs = new List<string> { "a", "b", "c" };
            Console.WriteLine(string.Join(", ", strs));  // a, b, c
            Console.WriteLine(string.Join('/', strs));   // a/b/c
            Console.WriteLine(string.Concat(strs));      // abc
        }
    }
}
