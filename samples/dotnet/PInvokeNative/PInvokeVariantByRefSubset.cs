#nullable enable
using System;
using System.Runtime.InteropServices;

// A godot_variant-shaped blittable ref struct passed byref: a Pack=8 sequential outer
// whose payload is a nested Explicit ref-struct union. A ref struct is not heap-storable,
// but the marshal is a byref copy of its bytes rather than a box, so it is blittable
// exactly when no arm of its field graph holds a managed reference. The storage is pinned
// and the native edits the arms in place, so the diff pins the nested layout at its
// non-zero offset.
namespace PInvokeVariantByRefSubset;

internal static unsafe class Program
{
    [StructLayout(LayoutKind.Sequential)]
    private struct Vec4
    {
        public float X, Y, Z, W;
    }

    [StructLayout(LayoutKind.Explicit)]
    private ref struct VData
    {
        [FieldOffset(0)] public long AsInt;
        [FieldOffset(0)] public double AsFloat;
        [FieldOffset(0)] public IntPtr AsPtr;
        [FieldOffset(0)] public Vec4 AsVec;
    }

    // A 4-byte tag, 4 bytes of Pack=8 padding, then the union at offset 8.
    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    private ref struct V
    {
        public int Type;
        public VData Data;
    }

    [DllImport("dn2cpptest")]
    private static extern void dn2cpptest_variant_negate(ref V v);
    [DllImport("dn2cpptest")]
    private static extern void dn2cpptest_variant_bump_via_ptr(ref V v);

    internal static void __GateEntry()
    {
        // The int arm.
        V v = default;
        v.Type = 1;
        v.Data.AsInt = 42;
        dn2cpptest_variant_negate(ref v);
        Console.WriteLine(v.Data.AsInt);                    // -42

        // The double arm, overlapping the same offset-8 payload.
        v = default;
        v.Type = 2;
        v.Data.AsFloat = 1.5;
        dn2cpptest_variant_negate(ref v);
        Console.WriteLine(v.Data.AsFloat);                  // -1.5

        // The 16-byte inline value-struct arm, which sizes the union.
        v = default;
        v.Type = 3;
        v.Data.AsVec = new Vec4 { X = 1, Y = 2, Z = 3, W = 4 };
        dn2cpptest_variant_negate(ref v);
        Vec4 r = v.Data.AsVec;
        Console.WriteLine($"{r.X} {r.Y} {r.Z} {r.W}");      // -1 -2 -3 -4

        // The unmanaged-pointer arm: the native bumps the pointed-at cell.
        int cell = 100;
        V p = default;
        p.Type = 4;
        p.Data.AsPtr = (IntPtr)(&cell);
        dn2cpptest_variant_bump_via_ptr(ref p);
        Console.WriteLine(cell);                            // 101
    }
}
