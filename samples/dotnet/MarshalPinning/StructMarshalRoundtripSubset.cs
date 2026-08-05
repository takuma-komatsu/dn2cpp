#nullable enable
using System;
using System.Runtime.InteropServices;

// Native-heap struct marshalling round-trips over a blittable struct (int/long/
// double/IntPtr fields only, so the C++ and .NET marshalled layouts are identical).
// AllocHGlobal -> StructureToPtr<T> -> PtrToStructure<T> and back, then mutate the
// buffer through a raw (Rec*) pointer and re-read to prove a real memcpy (not an
// aliased reference to the source value). Also the non-generic Type-based overloads:
// SizeOf(typeof(T)), StructureToPtr((object)boxed, ptr, false), PtrToStructure(ptr,
// typeof(T)) -> a boxed Rec. CoreLib only; diffed exact vs real .NET.
namespace StructMarshalRoundtripSubset;

struct Rec { public int A; public long B; public double C; public IntPtr D; }   // 32 bytes

unsafe class Program
{
    internal static void __GateEntry()
    {
        // --- generic StructureToPtr<T> / PtrToStructure<T> round-trip ---
        Rec src = new Rec { A = 5, B = 9_000_000_000L, C = 1.25, D = (IntPtr)123 };
        IntPtr buf = Marshal.AllocHGlobal(Marshal.SizeOf<Rec>());
        Marshal.StructureToPtr(src, buf, false);
        Rec dst = Marshal.PtrToStructure<Rec>(buf);
        Console.WriteLine($"{dst.A},{dst.B},{dst.C},{(long)dst.D}");   // 5,9000000000,1.25,123

        // mutate THROUGH the native buffer, re-read: proves a real memcpy (not aliasing).
        Rec* p = (Rec*)buf;
        p->A = 77;
        p->C = 2.5;
        Rec dst2 = Marshal.PtrToStructure<Rec>(buf);
        Console.WriteLine($"{dst2.A},{dst2.B},{dst2.C}");              // 77,9000000000,2.5
        Console.WriteLine(src.A);                                      // 5 (source untouched)
        Marshal.FreeHGlobal(buf);

        // --- non-generic Type-based overloads ---
        Rec src2 = new Rec { A = 1, B = 2, C = 3.5, D = (IntPtr)4 };
        IntPtr buf2 = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(Rec)));
        Marshal.StructureToPtr((object)src2, buf2, false);
        object boxed = Marshal.PtrToStructure(buf2, typeof(Rec));
        Rec dst3 = (Rec)boxed;
        Console.WriteLine($"{dst3.A},{dst3.B},{dst3.C},{(long)dst3.D}"); // 1,2,3.5,4
        Marshal.FreeHGlobal(buf2);
    }
}
