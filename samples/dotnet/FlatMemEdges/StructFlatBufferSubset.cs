#nullable enable
using System;
using System.Runtime.InteropServices;

// Round-tripping a struct through a flat byte buffer: a whole-struct store/load
// through a raw (Rec*) cast at a non-zero offset, a byte poked in the buffer and
// observed through the struct view, the individual fields reinterpreted out of the
// flat bytes, and a MemoryMarshal.Write/Read/Cast round-trip at an offset into a
// Span<byte>. Rec uses only word-or-larger fields, so its C++ and .NET layouts are
// byte-identical. CoreLib only; diffed exact vs real .NET.
namespace StructFlatBufferSubset;

struct Rec { public int Id; public double Val; public long Extra; }   // 24 bytes

unsafe class Program
{
    internal static void __GateEntry()
    {
        byte* buf = stackalloc byte[64];
        for (int i = 0; i < 64; i++) buf[i] = 0;

        Rec r = new Rec { Id = 0x01020304, Val = 3.5, Extra = 1234567890123L };

        // whole-struct store/load through a raw pointer cast at offset 8.
        *(Rec*)(buf + 8) = r;
        Rec r2 = *(Rec*)(buf + 8);
        Console.WriteLine($"{r2.Id},{r2.Val},{r2.Extra}");

        Console.WriteLine(sizeof(Rec));                           // 24

        // poke the low byte of Id in the flat buffer, observe through the struct.
        buf[8] = 0xFF;
        Console.WriteLine((*(Rec*)(buf + 8)).Id);

        // reinterpret the individual fields out of the flat bytes (word offsets).
        Console.WriteLine(*(int*)(buf + 8));                      // Id
        Console.WriteLine(*(double*)(buf + 16));                  // Val = 3.5
        Console.WriteLine(*(long*)(buf + 24));                    // Extra

        // MemoryMarshal round-trip at a non-zero offset into a Span<byte>.
        byte[] heap = new byte[64];
        Span<byte> sp = heap;
        MemoryMarshal.Write(sp.Slice(40), in r);
        Rec r3 = MemoryMarshal.Read<Rec>(sp.Slice(40));
        Console.WriteLine($"{r3.Id},{r3.Val},{r3.Extra}");

        // reinterpret the struct span as Rec and read a field back.
        Span<Rec> rs = MemoryMarshal.Cast<byte, Rec>(sp.Slice(40, 24));
        Console.WriteLine(rs.Length);                             // 1
        Console.WriteLine(rs[0].Extra);                           // 1234567890123
    }
}
