#nullable disable
using System;
using System.Collections.Generic;

namespace ListSortSubset
{
    // List<T>.Sort (default, natural/ordinal order) over concrete element
    // types now works. The blocker was a transpiler robustness bug: compiling a
    // body could instantiate a generic the discovery scan never saw (here the
    // ArraySortHelper<T>/Comparer<T> closure Sort reaches), which mutated the
    // class list mid-enumeration ("Collection was modified"). Emission now drives
    // to a fixpoint. Custom Comparison<T>/IComparer<T> ordering is not honored yet
    // (Array.Sort ignores the comparer — separate future work).
    internal static class Program
    {
        internal static void __GateEntry()
        {
            var ints = new List<int> { 5, 3, 1, 4, 2 };
            ints.Sort();
            foreach (var x in ints) Console.Write(x + " ");
            Console.WriteLine();                                // 1 2 3 4 5

            var strs = new List<string> { "pear", "apple", "fig", "kiwi" };
            strs.Sort();
            foreach (var s in strs) Console.Write(s + " ");
            Console.WriteLine();                                // apple fig kiwi pear

            var dbls = new List<double> { 2.5, 1.1, 3.3, 0.4 };
            dbls.Sort();
            foreach (var d in dbls) Console.Write(d + " ");
            Console.WriteLine();                                // 0.4 1.1 2.5 3.3

            var longs = new List<long> { 30, 10, 20 };
            longs.Sort();
            foreach (var l in longs) Console.Write(l + " ");
            Console.WriteLine();                                // 10 20 30

            // Sort then Reverse, and sort again after a mutation.
            ints.Reverse();
            foreach (var x in ints) Console.Write(x + " ");
            Console.WriteLine();                                // 5 4 3 2 1
            ints.Add(0);
            ints.Sort();
            foreach (var x in ints) Console.Write(x + " ");
            Console.WriteLine();                                // 0 1 2 3 4 5
        }
    }
}
