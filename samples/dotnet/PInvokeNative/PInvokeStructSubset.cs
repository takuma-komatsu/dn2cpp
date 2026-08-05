#nullable enable
using System;
using System.Runtime.InteropServices;

// A blittable struct crosses the C ABI by value exactly as the equivalent C struct, with
// no marshalling.
namespace PInvokeStructSubset;

internal static class Program
{
    [StructLayout(LayoutKind.Sequential)]
    private struct Point { public int X; public int Y; }

    [StructLayout(LayoutKind.Sequential)]
    private struct Record { public int Id; public double Value; }

    [DllImport("dn2cpptest")] private static extern int dn2cpptest_point_sum(Point p);
    [DllImport("dn2cpptest")] private static extern Point dn2cpptest_point_make(int x, int y);
    [DllImport("dn2cpptest")] private static extern double dn2cpptest_record_weight(Record r);

    internal static void __GateEntry()
    {
        var p = new Point { X = 3, Y = 4 };
        Console.WriteLine(dn2cpptest_point_sum(p));         // 7

        Point q = dn2cpptest_point_make(10, 20);
        Console.WriteLine($"{q.X} {q.Y}");                  // 10 20

        // Mixed int + double fields, so the padding has to match.
        var r = new Record { Id = 5, Value = 1.5 };
        Console.WriteLine(dn2cpptest_record_weight(r));     // 7.5
    }
}
