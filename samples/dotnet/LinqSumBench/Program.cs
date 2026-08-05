using System;
using System.Linq;

namespace LinqSumBench;

// A heavy LINQ-reduction workload used to compare the transpiled binary against real
// .NET (whose Enumerable.Sum/Min/Max run a Vector<T> SIMD core over the span they pull
// out of an array via TryGetSpan). This is a PERF measurement target, not a correctness
// gate: it prints deterministic checksums (so the measure script can confirm every
// build agrees) and runs long enough that per-element costs dominate startup noise.
// One array element mutates every pass so the optimizer cannot hoist the reduction.
internal static class Program
{
    private const int K = 8192;          // elements per pass
    private const int SumPasses = 100000; // headline int Sum workload
    private const int SidePasses = 20000; // secondary reductions

    private static void Main()
    {
        Console.WriteLine("sumInt=" + SumIntWork());
        Console.WriteLine("sumLong=" + SumLongWork());
        Console.WriteLine("sumFloat=" + SumFloatWork());
        Console.WriteLine("maxInt=" + MaxIntWork());
    }

    // The headline: Enumerable.Sum over int[]. Values stay <= 1023 so a pass sum is
    // far from int overflow and the checked accumulation never traps.
    private static long SumIntWork()
    {
        int[] a = new int[K];
        for (int i = 0; i < K; i++) a[i] = i & 1023;

        long checksum = 0;
        for (int pass = 0; pass < SumPasses; pass++)
        {
            checksum += a.Sum();
            a[pass % K] = (a[pass % K] + 1) & 1023;
        }
        return checksum;
    }

    private static long SumLongWork()
    {
        long[] a = new long[K];
        for (int i = 0; i < K; i++) a[i] = i & 1023;

        long checksum = 0;
        for (int pass = 0; pass < SidePasses; pass++)
        {
            checksum += a.Sum();
            a[pass % K] = (a[pass % K] + 1) & 1023;
        }
        return checksum;
    }

    // Whole-valued floats <= 63 over 8192 elements: every partial sum is exactly
    // representable, so the checksum is order-independent (scalar vs SIMD lanes).
    private static long SumFloatWork()
    {
        float[] a = new float[K];
        for (int i = 0; i < K; i++) a[i] = i & 63;

        long checksum = 0;
        for (int pass = 0; pass < SidePasses; pass++)
        {
            checksum += (long)a.Sum();
            a[pass % K] = (a[pass % K] + 1) % 64;
        }
        return checksum;
    }

    private static long MaxIntWork()
    {
        int[] a = new int[K];
        for (int i = 0; i < K; i++) a[i] = (i * 7) & 4095;

        long checksum = 0;
        for (int pass = 0; pass < SidePasses; pass++)
        {
            checksum += a.Max();
            a[pass % K] = (a[pass % K] + 1) & 4095;
        }
        return checksum;
    }
}
