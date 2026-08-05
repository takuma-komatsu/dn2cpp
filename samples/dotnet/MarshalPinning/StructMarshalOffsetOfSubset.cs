using System;
using System.Runtime.InteropServices;

// The layout-dependent struct-marshalling overloads carved out
// of the generic SizeOf<T>/PtrToStructure<T>/StructureToPtr<T>:
//   * Marshal.OffsetOf<T>(string) / OffsetOf(Type, string) -> IntPtr. A value-type
//     struct is a standard-layout POD, so the field's byte offset folds at transpile
//     time to offsetof(t_<Name>, f_<field>). The field name must be a string literal
//     (a nameof() compiles to the same ldstr); a runtime field name is carved out.
//   * the non-generic Type/object overloads of SizeOf / PtrToStructure / StructureToPtr,
//     which read the runtime type-info's instanceSize (== the C++ struct sizeof for a
//     blittable sequential value type) instead of a compile-time sizeof.
// Carve-outs: runtime (non-literal) field names, SizeOf(object), non-blittable /
// explicit StructLayout types. Diffed exact vs real .NET.
namespace StructMarshalOffsetOfSubset;

struct Point3
{
    public int X;
    public int Y;
    public double Z;
}

unsafe class Program
{
    internal static void __GateEntry()
    {
        // OffsetOf<T>(string): the field byte offsets of a blittable sequential struct.
        Console.WriteLine((long)Marshal.OffsetOf<Point3>("X"));        // 0
        Console.WriteLine((long)Marshal.OffsetOf<Point3>("Y"));        // 4
        Console.WriteLine((long)Marshal.OffsetOf<Point3>("Z"));        // 8
        // nameof lowers to the same ldstr literal as a quoted name.
        Console.WriteLine((long)Marshal.OffsetOf<Point3>(nameof(Point3.Y))); // 4

        // OffsetOf(Type, string): the non-generic form, type via typeof().
        Console.WriteLine((long)Marshal.OffsetOf(typeof(Point3), "Z")); // 8

        // SizeOf(Type): the non-generic form reads instanceSize at runtime.
        Console.WriteLine(Marshal.SizeOf(typeof(Point3)));             // 16
        Console.WriteLine(Marshal.SizeOf(typeof(int)));               // 4
        Console.WriteLine(Marshal.SizeOf(typeof(double)));            // 8

        // Non-generic PtrToStructure(IntPtr, Type) / StructureToPtr(object, IntPtr, bool):
        // round trip a boxed struct through a native buffer.
        Point3 src = new Point3 { X = 7, Y = 11, Z = 2.5 };
        IntPtr buf = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(Point3)));
        Marshal.StructureToPtr((object)src, buf, false);
        object boxed = Marshal.PtrToStructure(buf, typeof(Point3));
        Point3 dst = (Point3)boxed;
        Console.WriteLine(dst.X);                                     // 7
        Console.WriteLine(dst.Y);                                     // 11
        Console.WriteLine(dst.Z);                                     // 2.5

        // Mutate through the native buffer and re-read: confirms a real memcpy.
        Point3* p = (Point3*)buf;
        p->X = 100;
        Point3 dst2 = (Point3)Marshal.PtrToStructure(buf, typeof(Point3));
        Console.WriteLine(dst2.X);                                    // 100
        Console.WriteLine(dst2.Y);                                    // 11 (untouched)
        Console.WriteLine(src.X);                                     // 7  (source unchanged)
        Marshal.FreeHGlobal(buf);
    }
}
