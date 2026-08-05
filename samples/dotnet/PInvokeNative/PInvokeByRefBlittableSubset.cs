#nullable enable
using System;
using System.Runtime.InteropServices;

// ref/out/in of a blittable type passes the pinned address of the managed storage with no
// copy, so write-backs need no post-call decode. ref bool and ref char stay a carve-out:
// their 4-byte-BOOL / charset width differs from the managed storage.
namespace PInvokeByRefBlittableSubset;

internal static class Program
{
    private enum Color { Red = 0, Green = 1, Blue = 2 }

    [DllImport("dn2cpptest")] private static extern void dn2cpptest_ref_addone(ref int p);
    [DllImport("dn2cpptest")] private static extern void dn2cpptest_ref_dbl_scale(ref double p, double k);
    [DllImport("dn2cpptest")] private static extern void dn2cpptest_ref_long_neg(ref long p);
    [DllImport("dn2cpptest")] private static extern void dn2cpptest_ref_byte_inc(ref byte p);
    [DllImport("dn2cpptest")] private static extern int dn2cpptest_out_divmod(int a, int b, out int rem);
    [DllImport("dn2cpptest")] private static extern void dn2cpptest_ref_pt_swap(ref Point p);
    [DllImport("dn2cpptest")] private static extern void dn2cpptest_ref_enum_next(ref Color e);
    // The native mutates through the read-only ref, proving `in` lowers to the same
    // byref pointer.
    [DllImport("dn2cpptest", EntryPoint = "dn2cpptest_ref_addone")]
    private static extern void dn2cpptest_in_addone(in int p);

    [StructLayout(LayoutKind.Sequential)]
    private struct Point { public int X; public int Y; }

    internal static void __GateEntry()
    {
        int v = 41;
        dn2cpptest_ref_addone(ref v);
        Console.WriteLine(v); // 42

        // Quotient returned, remainder written through the out pointer.
        int q = dn2cpptest_out_divmod(17, 5, out int rem);
        Console.WriteLine($"{q} {rem}"); // 3 2

        double d = 2.5;
        dn2cpptest_ref_dbl_scale(ref d, 4.0);
        Console.WriteLine(d); // 10

        long L = 5;
        dn2cpptest_ref_long_neg(ref L);
        Console.WriteLine(L); // -5

        // Sub-word storage width must match uint8_t*.
        byte b = 200;
        dn2cpptest_ref_byte_inc(ref b);
        Console.WriteLine(b); // 201

        Point p = new Point { X = 1, Y = 2 };
        dn2cpptest_ref_pt_swap(ref p);
        Console.WriteLine($"{p.X} {p.Y}"); // 2 1

        Color c = Color.Red;
        dn2cpptest_ref_enum_next(ref c);
        Console.WriteLine(c); // Green

        int w = 7;
        dn2cpptest_in_addone(in w);
        Console.WriteLine(w); // 8
    }
}
