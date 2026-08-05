#nullable enable
using System;
namespace CreateSpanSubset;


static class Program
{
    static int SumI(ReadOnlySpan<int> s)
    {
        int t = 0;
        foreach (int x in s) t += x;
        return t;
    }

    static long SumL(ReadOnlySpan<long> s)
    {
        long t = 0;
        foreach (long x in s) t += x;
        return t;
    }

    static double SumD(ReadOnlySpan<double> s)
    {
        double t = 0;
        foreach (double x in s) t += x;
        return t;
    }

    static int SumB(ReadOnlySpan<byte> s)
    {
        int t = 0;
        foreach (byte x in s) t += x;
        return t;
    }internal static void __GateEntry()
    {
        // Inline int array literal as a ReadOnlySpan<int> argument -> CreateSpan over an
        // RVA blob. Sum + length + indexed read.
        Console.WriteLine(SumI(new[] { 1, 2, 3, 4, 5 }));

        // long / double blobs (8-byte elements).
        Console.WriteLine(SumL(new[] { 10L, 20L, 30L }));
        Console.WriteLine(SumD(new[] { 1.5, 2.25, 0.25 }));

        // byte blob (sub-word element packing).
        Console.WriteLine(SumB(new byte[] { 1, 2, 3, 250 }));

        // Collection expression targeting ReadOnlySpan<int> (also lowers to CreateSpan
        // over an RVA blob for constant elements).
        ReadOnlySpan<int> ce = [7, 8, 9];
        Console.WriteLine(ce.Length + ":" + ce[0] + "," + ce[2]);
        Console.WriteLine(SumI(ce));

        // A normal array literal indexed directly still works (no span/CreateSpan).
        Console.WriteLine(new[] { 100, 200, 300 }[1]);
    }
}
