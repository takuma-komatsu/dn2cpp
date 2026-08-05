#nullable enable
using System;
using System.Runtime.InteropServices;

// Unlike a primitive blittable array, which .NET pins and passes by pointer so write-backs
// are visible either way, a struct array is marshalled BY COPY with direction semantics:
// [In] copies in only, [In,Out] copies both ways, [Out] zeroes the input first. The packed
// storage stride, trailing padding included, must match the native C array layout.
namespace PInvokeStructArraySubset;

internal static class Program
{
    [StructLayout(LayoutKind.Sequential)]
    private struct Point { public int X; public int Y; }

    [StructLayout(LayoutKind.Sequential)]
    private struct Record { public int Id; public double Value; }

    [DllImport("dn2cpptest")] private static extern int dn2cpptest_ptarr_sum(Point[] a, int n);
    [DllImport("dn2cpptest")] private static extern int dn2cpptest_ptarr_x(Point[] a, int i);
    [DllImport("dn2cpptest")] private static extern int dn2cpptest_ptarr_isnull(Point[]? a);

    // One swap function under all three directions, so the trio pins the copy semantics.
    [DllImport("dn2cpptest", EntryPoint = "dn2cpptest_ptarr_swap_all")]
    private static extern void ptarr_swap_default(Point[] a, int n);
    [DllImport("dn2cpptest", EntryPoint = "dn2cpptest_ptarr_swap_all")]
    private static extern void ptarr_swap_inout([In, Out] Point[] a, int n);
    [DllImport("dn2cpptest", EntryPoint = "dn2cpptest_ptarr_swap_all")]
    private static extern void ptarr_swap_out([Out] Point[] a, int n);

    [DllImport("dn2cpptest")] private static extern double dn2cpptest_recarr_sum(Record[] a, int n);
    [DllImport("dn2cpptest", EntryPoint = "dn2cpptest_recarr_bump")]
    private static extern void recarr_bump_inout([In, Out] Record[] a, int n, double k);

    internal static void __GateEntry()
    {
        var pts = new Point[]
        {
            new Point { X = 1, Y = 2 },
            new Point { X = 3, Y = 4 },
            new Point { X = 10, Y = 20 },
        };
        Console.WriteLine(dn2cpptest_ptarr_sum(pts, pts.Length)); // 40
        Console.WriteLine(dn2cpptest_ptarr_x(pts, 2));            // 10 — indexing pins the stride

        Console.WriteLine(dn2cpptest_ptarr_isnull(null));                 // 1
        Console.WriteLine(dn2cpptest_ptarr_isnull(Array.Empty<Point>())); // 0
        Console.WriteLine(dn2cpptest_ptarr_sum(Array.Empty<Point>(), 0)); // 0

        // The default [In] copies in rather than pinning, so the mutation is invisible.
        var pd = new Point[] { new Point { X = 1, Y = 2 } };
        ptarr_swap_default(pd, pd.Length);
        Console.WriteLine($"{pd[0].X} {pd[0].Y}"); // 1 2 (unchanged)

        var pio = new Point[]
        {
            new Point { X = 1, Y = 2 },
            new Point { X = 10, Y = 20 },
        };
        ptarr_swap_inout(pio, pio.Length);
        Console.WriteLine($"{pio[0].X} {pio[0].Y} {pio[1].X} {pio[1].Y}"); // 2 1 20 10

        // [Out]: the native sees a zeroed buffer, so swapping yields zeroes.
        var po = new Point[] { new Point { X = 5, Y = 6 } };
        ptarr_swap_out(po, po.Length);
        Console.WriteLine($"{po[0].X} {po[0].Y}"); // 0 0

        // A mixed int + double struct strides by 16 with Value at offset 8, so this pins
        // the C padding against dn2cpp's layout.
        var recs = new Record[]
        {
            new Record { Id = 2, Value = 1.5 },
            new Record { Id = 3, Value = 2.0 },
        };
        Console.WriteLine(dn2cpptest_recarr_sum(recs, recs.Length)); // 8.5

        recarr_bump_inout(recs, recs.Length, 10.0);
        Console.WriteLine($"{recs[0].Id} {recs[0].Value} {recs[1].Id} {recs[1].Value}"); // 3 15 4 20
    }
}
