#nullable disable
using System;
using Dn2Cpp.Runtime;

namespace HotPathNoAliasSubset
{
    // [HotPath(NoAlias = true)]: the marked bodies' array, byref and pointer
    // parameters are emitted __restrict — the caller's promise that no two of them
    // reach the same memory. Nothing is verified, so every call site here passes
    // genuinely disjoint buffers; overlapping ones would be UB, exactly as an
    // out-of-range index is under SkipBoundsChecks.
    //
    // Everything is integral, so the outputs are exact-diffable against real .NET,
    // where the attribute is inert. What the gate script asserts structurally is
    // the emitted C++: __restrict on the qualifying parameters of these bodies,
    // and NOT on the class-typed parameter of CountOver (two object references may
    // legitimately alias) — a positive control that the qualifier tracks the
    // parameter kind rather than being sprayed over every pointer.
    internal static class Program
    {
        // A class-typed parameter, deliberately: it renders as a C++ pointer and
        // must nonetheless stay unqualified.
        private sealed class Threshold
        {
            public int Value;
        }

        // Two arrays in, one written: the shape __restrict exists for.
        [HotPath(NoAlias = true)]
        private static void Axpy(int[] dst, int[] src, int scale)
        {
            for (int i = 0; i < dst.Length; i++)
                dst[i] += src[i] * scale;
        }

        // Byref parameters (T*), qualified the same way; the receiver of a static
        // method does not exist, so this body is all-qualified but for `scale`.
        [HotPath(NoAlias = true)]
        private static void MinMax(int[] values, ref int lo, ref int hi)
        {
            lo = values[0];
            hi = values[0];
            for (int i = 1; i < values.Length; i++)
            {
                if (values[i] < lo)
                    lo = values[i];
                if (values[i] > hi)
                    hi = values[i];
            }
        }

        // The negative control: the array qualifies, the class does not.
        [HotPath(NoAlias = true)]
        private static int CountOver(int[] values, Threshold limit)
        {
            int n = 0;
            for (int i = 0; i < values.Length; i++)
                if (values[i] > limit.Value)
                    n++;
            return n;
        }

        // The span half of the knob, which only takes effect alongside
        // SkipBoundsChecks: each span parameter's element pointer is hoisted into
        // a __restrict prologue local that the raw indexer route then addresses,
        // instead of re-reading f__reference through the span struct at every
        // access.
        [HotPath(NoAlias = true, SkipBoundsChecks = true)]
        private static long DotSpans(ReadOnlySpan<int> a, ReadOnlySpan<int> b)
        {
            long acc = 0;
            for (int i = 0; i < a.Length; i++)
                acc += (long)a[i] * b[i];
            return acc;
        }

        // A span parameter the body RESEATS opts out of the hoist (the pointer
        // would go stale) while keeping the raw indexer — the NoAlias and
        // SkipBoundsChecks halves must stay independent, and the body still correct.
        [HotPath(NoAlias = true, SkipBoundsChecks = true)]
        private static long SumTail(ReadOnlySpan<int> values, int skip)
        {
            values = values.Slice(skip);
            long acc = 0;
            for (int i = 0; i < values.Length; i++)
                acc += values[i];
            return acc;
        }

        internal static void __GateEntry()
        {
            var dst = new int[96];
            var src = new int[96];
            for (int i = 0; i < dst.Length; i++)
            {
                dst[i] = i * 3 - 40;
                src[i] = 100 - i;
            }
            Axpy(dst, src, 7);
            long axpySum = 0;
            for (int i = 0; i < dst.Length; i++)
                axpySum += dst[i];
            Console.WriteLine(axpySum);
            Console.WriteLine(dst[0] + "," + dst[47] + "," + dst[95]);

            int lo = 0;
            int hi = 0;
            MinMax(dst, ref lo, ref hi);
            Console.WriteLine(lo + "," + hi);

            var limit = new Threshold();
            limit.Value = 300;
            Console.WriteLine(CountOver(dst, limit));

            var xs = new int[64];
            var ys = new int[64];
            for (int i = 0; i < xs.Length; i++)
            {
                xs[i] = i - 10;
                ys[i] = 2 * i + 1;
            }
            Console.WriteLine(DotSpans(new ReadOnlySpan<int>(xs), new ReadOnlySpan<int>(ys)));
            Console.WriteLine(SumTail(new ReadOnlySpan<int>(xs), 20));
        }
    }
}
