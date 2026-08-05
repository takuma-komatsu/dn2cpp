#nullable enable
using System;
using System.Runtime.InteropServices;

// Overlapping [FieldOffset] arms crossing the boundary by value, by return, and byref.
// Every check writes one arm on one side and reads a DIFFERENT one on the other, so the
// diff pins the byte-punning rather than a field round-trip.
namespace PInvokeExplicitUnionSubset;

internal static class Program
{
    [StructLayout(LayoutKind.Explicit)]
    private struct Overlay
    {
        [FieldOffset(0)] public long Bits;
        [FieldOffset(0)] public double F64;
        [FieldOffset(0)] public int Lo;
        [FieldOffset(4)] public int Hi;
    }

    [DllImport("dn2cpptest")]
    private static extern long dn2cpptest_union_f64_scaled(Overlay u);
    [DllImport("dn2cpptest")]
    private static extern Overlay dn2cpptest_union_make(int lo, int hi);
    [DllImport("dn2cpptest")]
    private static extern void dn2cpptest_union_swap(ref Overlay u);

    internal static void __GateEntry()
    {
        // By value in: C# writes the double arm, the native reads it.
        var u = default(Overlay);
        u.F64 = 2.75;
        Console.WriteLine(dn2cpptest_union_f64_scaled(u));

        // By value return: the native writes the halves, C# reads the int64 arm.
        Overlay m = dn2cpptest_union_make(0x11223344, 0x55667788);
        Console.WriteLine(m.Bits.ToString("X16"));      // 5566778811223344

        // hi = 0x3FF00000 is the bit pattern of 1.0.
        Overlay one = dn2cpptest_union_make(0, 0x3FF00000);
        Console.WriteLine(one.F64);                     // 1

        // Byref: the native swaps the halves through the pinned storage.
        m.Lo = 100;
        m.Hi = -7;
        dn2cpptest_union_swap(ref m);
        Console.WriteLine($"{m.Lo} {m.Hi}");            // -7 100
        Console.WriteLine(m.Bits.ToString("X16"));      // 00000064FFFFFFF9
    }
}
