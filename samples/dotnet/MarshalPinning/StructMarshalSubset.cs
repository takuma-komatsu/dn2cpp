using System;
using System.Runtime.InteropServices;

// Blittable struct <-> native memory marshalling, the native-heap follow-up.
// Marshal.{SizeOf,PtrToStructure,StructureToPtr}<T>:
// * SizeOf<T> -> the C++ `sizeof` of T's storage type. dn2cpp emits a value-
// type struct as a plain standard-layout C++ struct, so for a blittable
// sequential type its sizeof matches the .NET marshalled layout exactly.
// * StructureToPtr<T>(T, IntPtr, bool) -> writes the struct value to native
// memory (a raw value copy; fDeleteOld is irrelevant for a blittable struct).
// * PtrToStructure<T>(IntPtr) -> reads a struct value back out (a raw copy).
// Round-tripped through a Marshal.AllocHGlobal native buffer, then mutated
// through the buffer to prove it is a real memcpy (not an aliased reference to
// the source value). Carve-outs: the non-generic Type-based overloads +
// Marshal.OffsetOf (the StructMarshalOffsetOfSubset section), non-blittable /
// explicit [StructLayout(Pack|Explicit)] types, DestroyStructure.
// Diffed exact vs real .NET.
namespace StructMarshalSubset;

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
        // SizeOf<T>: a blittable sequential struct + the primitive sizes.
        Console.WriteLine(Marshal.SizeOf<Point3>());          // 16
        Console.WriteLine(Marshal.SizeOf<int>());             // 4
        Console.WriteLine(Marshal.SizeOf<byte>());            // 1
        Console.WriteLine(Marshal.SizeOf<double>());          // 8

        // StructureToPtr -> PtrToStructure round trip through native memory.
        Point3 src = new Point3 { X = 7, Y = 11, Z = 2.5 };
        IntPtr buf = Marshal.AllocHGlobal(Marshal.SizeOf<Point3>());
        Marshal.StructureToPtr(src, buf, false);
        Point3 dst = Marshal.PtrToStructure<Point3>(buf);
        Console.WriteLine(dst.X);                             // 7
        Console.WriteLine(dst.Y);                             // 11
        Console.WriteLine(dst.Z);                             // 2.5
        Console.WriteLine(dst.X + dst.Y);                     // 18

        // Mutate through the native buffer and re-read: confirms a real memcpy
        // (not an aliased reference to the source value).
        Point3* p = (Point3*)buf;
        p->X = 100;
        Point3 dst2 = Marshal.PtrToStructure<Point3>(buf);
        Console.WriteLine(dst2.X);                            // 100
        Console.WriteLine(dst2.Y);                            // 11 (untouched)
        Console.WriteLine(src.X);                             // 7 (source unchanged)
        Marshal.FreeHGlobal(buf);
    }
}
