#nullable enable
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// The SIZE of a [StructLayout(Sequential, Size = N)] value type whose fields end on a
// pointer — nothing to do with P/Invoke. The emitted body reaches N with a trailing pad,
// so a pad measured at one pointer width makes sizeof (and the instanceSize stamped from
// it) wrong on a 32-bit target, exactly as the explicit-layout union's total did. Every
// line states a relation that reads the same at both widths, so it survives the diff
// against 64-bit .NET while the wasm32 build (build-and-run-pinvoke-wasm.sh) is the only
// one that can go red. Each struct is also written and read back: the total is fixed by
// the emitted body's shape, so a shape that sizes correctly and misplaces a field would
// otherwise pass.
namespace SequentialSizeSubset;

internal static class Program
{
    // The declared size IS the field end at 64 bits and 8 bytes past it at 32: the pad is
    // 0 on one target and positive on the other.
    [StructLayout(LayoutKind.Sequential, Size = 16)]
    private struct TwoPtrExact
    {
        public IntPtr X;
        public IntPtr Y;
    }

    // The same, with the pad starting at an unaligned field end (9 bytes at 64, 5 at 32).
    [StructLayout(LayoutKind.Sequential, Size = 16)]
    private struct PtrByteExact
    {
        public IntPtr P;
        public byte B;
    }

    // The declared size dominates at both widths, but the pad reaching it does not: 12
    // bytes at 64, 16 at 32.
    [StructLayout(LayoutKind.Sequential, Size = 24)]
    private struct PtrIntPadded
    {
        public IntPtr P;
        public int I;
    }

    // Declared below the field end at 64 bits and above it at 32, so the TOTAL — not just
    // the pad — moves with the pointer width.
    [StructLayout(LayoutKind.Sequential, Size = 12)]
    private struct TwoPtrUnderSized
    {
        public IntPtr X;
        public IntPtr Y;
    }

    // The declared size dominates at both widths AND is expressible at both, so the total
    // is one literal — the representable neighbour of the refusal LayoutSizeBadNarrow
    // asserts, where the same fields under Size=12 have no C++ form at 64 bits.
    [StructLayout(LayoutKind.Sequential, Size = 16)]
    private struct PtrIntExact
    {
        public IntPtr P;
        public int I;
    }

    // The control: no pointer anywhere, so nothing here may move with the width.
    [StructLayout(LayoutKind.Sequential, Size = 12)]
    private struct IntPadded
    {
        public int I;
    }

    internal static void __GateEntry()
    {
        var a = default(TwoPtrExact);
        a.X = new IntPtr(11);
        a.Y = new IntPtr(22);
        Console.WriteLine($"{Unsafe.SizeOf<TwoPtrExact>()} {a.X.ToInt64()} {a.Y.ToInt64()}");   // 16 11 22

        var b = default(PtrByteExact);
        b.P = new IntPtr(33);
        b.B = 200;
        Console.WriteLine($"{Unsafe.SizeOf<PtrByteExact>()} {b.P.ToInt64()} {b.B}");            // 16 33 200

        var c = default(PtrIntPadded);
        c.P = new IntPtr(44);
        c.I = -3;
        Console.WriteLine($"{Unsafe.SizeOf<PtrIntPadded>()} {c.P.ToInt64()} {c.I}");            // 24 44 -3

        var d = default(TwoPtrUnderSized);
        d.X = new IntPtr(55);
        d.Y = new IntPtr(66);
        Console.WriteLine($"{Unsafe.SizeOf<TwoPtrUnderSized>() == 8 + IntPtr.Size} {d.X.ToInt64()} {d.Y.ToInt64()}");  // True 55 66

        var e = default(IntPadded);
        e.I = 7;
        Console.WriteLine($"{Unsafe.SizeOf<IntPadded>()} {e.I}");                               // 12 7

        var f = default(PtrIntExact);
        f.P = new IntPtr(77);
        f.I = -9;
        Console.WriteLine($"{Unsafe.SizeOf<PtrIntExact>()} {f.P.ToInt64()} {f.I}");             // 16 77 -9
    }
}
