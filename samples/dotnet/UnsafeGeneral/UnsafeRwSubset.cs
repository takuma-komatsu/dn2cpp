using System;
using System.Runtime.CompilerServices;

// Unsafe.Read/Write and their Unaligned forms across three memory backings. The
// aligned forms deref; the Unaligned forms must memcpy, so they are exercised at
// a deliberately misaligned offset.
namespace UnsafeRwSubset;

struct Vec3 { public double X; public double Y; public double Z; }
struct Holder { public long Header; public Vec3 Body; }

class Program
{
    internal static unsafe void __GateEntry()
    {
        Vec3 v = new Vec3 { X = 1.25, Y = -2.5, Z = 3.75 };

        // Stackalloc buffer, misaligned struct round-trip.
        byte* buf = stackalloc byte[32];
        Unsafe.WriteUnaligned(buf + 3, v);
        Vec3 r = Unsafe.ReadUnaligned<Vec3>(buf + 3);
        Console.WriteLine($"{r.X},{r.Y},{r.Z}");                     // 1.25,-2.5,3.75

        // Heap byte[], via the ref-byte overload.
        byte[] arr = new byte[16];
        Unsafe.WriteUnaligned(ref arr[0], 1234567890123456789L);
        Unsafe.WriteUnaligned(ref arr[8], 6.5);
        Console.WriteLine(Unsafe.ReadUnaligned<long>(ref arr[0]));   // 1234567890123456789
        Console.WriteLine(Unsafe.ReadUnaligned<double>(ref arr[8])); // 6.5

        // Struct field, aligned Read/Write through its address.
        Holder h = default;
        h.Header = 7;
        Vec3* pf = &h.Body;
        Unsafe.Write(pf, v);
        Vec3 back = Unsafe.Read<Vec3>(pf);
        Console.WriteLine($"{h.Header}:{back.X},{back.Y},{back.Z}"); // 7:1.25,-2.5,3.75
    }
}
