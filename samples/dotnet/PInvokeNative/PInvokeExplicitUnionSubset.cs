#nullable enable
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// Overlapping [FieldOffset] arms crossing the boundary by value, by return, and byref.
// Every check writes one arm on one side and reads a DIFFERENT one on the other, so the
// diff pins the byte-punning rather than a field round-trip.
//
// Then the SIZE of an explicit layout, which is not a marshalling question: the emitted
// union fixes its own total, so a total decided by a pointer field is wrong on a 32-bit
// target and right everywhere the transpile runs. Asserted as a relation to IntPtr.Size,
// which reads identically on both axes — this file is built for wasm32 too
// (build-and-run-pinvoke-wasm.sh), and that is the only build where it can go red.
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

    // The pointer arm decides the total: one pointer wide on every target. (Godot's
    // godot_dictionary is exactly this shape, over an engine type that is one pointer.)
    [StructLayout(LayoutKind.Explicit)]
    private struct PtrOverlay
    {
        [FieldOffset(0)] public byte Tag;
        [FieldOffset(0)] public IntPtr Handle;
    }

    // Pointer-bearing, yet a constant offset past it decides the total: the same 16 bytes
    // on every target, so this one must NOT pick up the pointer width.
    [StructLayout(LayoutKind.Explicit)]
    private struct PtrPlusId
    {
        [FieldOffset(0)] public IntPtr Handle;
        [FieldOffset(8)] public ulong Id;
    }

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

        Console.WriteLine(Unsafe.SizeOf<PtrOverlay>() == IntPtr.Size);   // True
        Console.WriteLine(Unsafe.SizeOf<PtrPlusId>() == 16);             // True
    }
}
