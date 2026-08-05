using System;
using System.Numerics;

namespace VectorBench;

// A memory-bound Vector<T> workload comparing the scalar lane-loop emulation
// against the Highway SIMD backend. A PERF target, not a correctness gate: the
// checksums are deterministic so the measure script can confirm the two backends
// agree, and the work is loop-carried and written back so the optimizer cannot
// fold it away.
internal static class Program
{
    private const int K = 8192;        // multiple of any vector width
    private const int Passes = 160000; // sized so a run dominates startup noise

    private static void Main()
    {
        long ci = IntWork();
        long cf = FloatWork();
        Console.WriteLine("int=" + ci);
        Console.WriteLine("flt=" + cf);
    }

    private static long IntWork()
    {
        int[] a = new int[K];
        int[] b = new int[K];
        for (int i = 0; i < K; i++) { a[i] = i & 1023; b[i] = (i * 3 + 1) & 1023; }

        int w = Vector<int>.Count;
        long checksum = 0;
        for (int pass = 0; pass < Passes; pass++)
        {
            Vector<int> acc = Vector<int>.Zero;
            for (int i = 0; i + w <= K; i += w)
            {
                Vector<int> va = new Vector<int>(a, i);
                Vector<int> vb = new Vector<int>(b, i);
                acc += va * vb - va;
            }
            checksum += Vector.Sum(acc);
            a[pass % K] = (a[pass % K] + 1) & 1023;   // defeats loop hoisting
        }
        return checksum;
    }

    // Whole-valued throughout, so the checksum is a stable integer independent of
    // float formatting.
    private static long FloatWork()
    {
        float[] a = new float[K];
        float[] b = new float[K];
        for (int i = 0; i < K; i++) { a[i] = (i & 63); b[i] = ((i * 2) & 63); }

        int w = Vector<float>.Count;
        long checksum = 0;
        for (int pass = 0; pass < Passes; pass++)
        {
            Vector<float> acc = Vector<float>.Zero;
            for (int i = 0; i + w <= K; i += w)
            {
                Vector<float> va = new Vector<float>(a, i);
                Vector<float> vb = new Vector<float>(b, i);
                acc += va * vb + va;
            }
            checksum += (long)Vector.Sum(acc);
            a[pass % K] = (a[pass % K] + 1) % 64;
        }
        return checksum;
    }
}
