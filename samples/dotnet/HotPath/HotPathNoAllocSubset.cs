#nullable disable
using System;
using Dn2Cpp.Runtime;

namespace HotPathNoAllocSubset
{
    // [HotPath(NoAlloc = true)]: the transpiler proves each marked kernel's
    // direct-call closure allocates nothing and dispatches nothing dynamically.
    // The knob is inert under real .NET, so results stay exact-diff-identical —
    // the kernels here are integer-only, so the diff is culture- and rounding-
    // proof. The gate script grep-asserts the marked bodies land in
    // generated_hot.cpp; that a transpile completes at all is the verifier's own
    // pass (a violation would fail it with error: / exit 2, covered by the
    // HotPathNoAlloc*Bad negative mini-projects).
    internal static class Program
    {
        // A value type folded by value — struct arithmetic reached through a
        // depth-2 static helper. Passing and returning it is a stack copy, so the
        // closure allocates nothing.
        private struct Acc
        {
            public long Total;
        }

        private static Acc Fold(Acc acc, int v)
        {
            acc.Total += (long)v * v;
            return acc;
        }

        [HotPath(NoAlloc = true)]
        private static long SumOfSquares(int[] values)
        {
            Acc acc = default;
            for (int i = 0; i < values.Length; i++)
                acc = Fold(acc, values[i]);
            return acc.Total;
        }

        // A span kernel: NoAlloc pairs with SkipBoundsChecks so the raw indexer
        // replaces the checked Span get_Item body, keeping the closure trivially
        // allocation-free.
        [HotPath(NoAlloc = true, SkipBoundsChecks = true)]
        private static long SumSpan(ReadOnlySpan<int> values)
        {
            long sum = 0;
            for (int i = 0; i < values.Length; i++)
                sum += values[i];
            return sum;
        }

        internal static void __GateEntry()
        {
            var ints = new int[64];
            for (int i = 0; i < ints.Length; i++)
                ints[i] = i - 20;
            Console.WriteLine(SumOfSquares(ints));
            Console.WriteLine(SumSpan(new ReadOnlySpan<int>(ints, 4, 50)));
        }
    }
}
