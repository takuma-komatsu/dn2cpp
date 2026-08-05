using System;
using System.Runtime.InteropServices;

// MemoryMarshal.Read<T> / Write<T> over a byte span: multi-field struct, wide
// primitives, and a sub-word value whose int32-promote must sign/zero-extend.
// The transpiler memcpys at the payload's storage width with a length check.
namespace MarshalRwSubset;

struct Vec3 { public double X; public double Y; public double Z; }

class Program
{
    internal static void __GateEntry()
    {
        Span<byte> buf = stackalloc byte[32];
        Vec3 v = new Vec3 { X = 1.5, Y = 2.5, Z = 3.5 };
        MemoryMarshal.Write(buf, in v);
        Vec3 r = MemoryMarshal.Read<Vec3>(buf);
        Console.WriteLine($"{r.X},{r.Y},{r.Z}");              // 1.5,2.5,3.5

        // Heap byte[] backing.
        byte[] h = new byte[16];
        Span<byte> hs = h;
        MemoryMarshal.Write(hs, 9876543210L);
        Console.WriteLine(MemoryMarshal.Read<long>(hs));      // 9876543210
        MemoryMarshal.Write(hs, -1.25);
        Console.WriteLine(MemoryMarshal.Read<double>(hs));    // -1.25

        // The same bits promoted signed and unsigned.
        byte[] sb = { 0xFE, 0xFF, 0, 0 };
        Console.WriteLine(MemoryMarshal.Read<short>(sb));     // -2
        Console.WriteLine(MemoryMarshal.Read<ushort>(sb));    // 65534
    }
}
