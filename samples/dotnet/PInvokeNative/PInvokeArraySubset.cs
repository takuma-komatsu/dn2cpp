#nullable enable
using System;
using System.Runtime.InteropServices;

// A blittable array marshals as a pinned pointer to its first element with no copy, so
// write-backs are visible. A null array is a null pointer; an empty array is a valid
// non-null one.
namespace PInvokeArraySubset;

internal static class Program
{
    [DllImport("dn2cpptest")] private static extern int dn2cpptest_iarr_sum(int[] a, int n);
    [DllImport("dn2cpptest")] private static extern long dn2cpptest_larr_sum(long[] a, int n);
    [DllImport("dn2cpptest")] private static extern double dn2cpptest_darr_sum(double[] a, int n);
    [DllImport("dn2cpptest")] private static extern int dn2cpptest_barr_sum(byte[] a, int n);
    [DllImport("dn2cpptest")] private static extern void dn2cpptest_iarr_scale(int[] a, int n, int k);

    internal static void __GateEntry()
    {
        int[] ia = { 10, 20, 30, 40 };
        Console.WriteLine(dn2cpptest_iarr_sum(ia, ia.Length));     // 100

        long[] la = { 1_000_000_000L, 2_000_000_000L, 3_000_000_000L };
        Console.WriteLine(dn2cpptest_larr_sum(la, la.Length));     // 6000000000

        double[] da = { 1.5, 2.25, 0.25 };
        Console.WriteLine(dn2cpptest_darr_sum(da, da.Length));     // 4

        byte[] ba = { 200, 100, 50 };
        Console.WriteLine(dn2cpptest_barr_sum(ba, ba.Length));     // 350

        int[] sc = { 1, 2, 3 };
        dn2cpptest_iarr_scale(sc, sc.Length, 5);
        Console.WriteLine($"{sc[0]} {sc[1]} {sc[2]}");             // 5 10 15

        int[]? nul = null;
        Console.WriteLine(dn2cpptest_iarr_sum(nul!, 0));           // 0

        Console.WriteLine(dn2cpptest_iarr_sum(Array.Empty<int>(), 0)); // 0
    }
}
