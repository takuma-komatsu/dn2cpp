#nullable enable
using System;

// Raw-pointer reinterpret at non-zero byte offsets and mixed-width overlapping
// reads/writes that the typed Unsafe.Read/Write gate does not cover: a value read
// or written through a raw (int*)/(uint*)/(short*)/(long*)/(sbyte*) cast at an
// arbitrary offset into a flat stackalloc buffer, the same bytes viewed at several
// widths at once, and a write at one width observed at another. Reinterpret byte
// order is host-endian; the gate diffs the transpiled binary against real .NET on
// the same machine, so they agree. CoreLib only; diffed exact vs real .NET.
namespace ReinterpretOffsetSubset;

unsafe class Program
{
    internal static void __GateEntry()
    {
        byte* buf = stackalloc byte[32];
        for (int i = 0; i < 32; i++) buf[i] = (byte)(i + 1);     // 1,2,3,...,32

        // read an int through a raw int* at aligned offsets 0 and 4.
        Console.WriteLine(*(int*)(buf + 0));                      // 0x04030201
        Console.WriteLine(*(int*)(buf + 4));                      // 0x08070605
        // overlapping unaligned read at offset 2 as uint.
        Console.WriteLine(*(uint*)(buf + 2));                     // 0x06050403

        // mixed-width overlapping reads of the same 8 bytes at offset 8.
        long L = *(long*)(buf + 8);
        int lo = *(int*)(buf + 8);
        int hi = *(int*)(buf + 12);
        ushort w0 = *(ushort*)(buf + 8);
        ushort w1 = *(ushort*)(buf + 10);
        Console.WriteLine(L);
        Console.WriteLine($"{lo},{hi}");
        Console.WriteLine($"{w0},{w1}");
        Console.WriteLine(L == ((long)hi << 32 | (uint)lo));      // True
        Console.WriteLine(lo == (w1 << 16 | w0));                 // True

        // write through int* at offset 16, observe the individual bytes (LE order).
        *(int*)(buf + 16) = 0x11223344;
        Console.WriteLine($"{buf[16]},{buf[17]},{buf[18]},{buf[19]}");

        // overwrite the low half with a short, read the whole int back.
        *(short*)(buf + 16) = 0x5566;
        Console.WriteLine((*(int*)(buf + 16)).ToString("X8"));    // 11225566

        // signed vs unsigned sub-word reinterpret of the same bytes.
        *(short*)(buf + 24) = -2;
        Console.WriteLine(*(short*)(buf + 24));                   // -2
        Console.WriteLine(*(ushort*)(buf + 24));                  // 65534
        *(sbyte*)(buf + 26) = -5;
        Console.WriteLine(*(sbyte*)(buf + 26));                   // -5
        Console.WriteLine(*(byte*)(buf + 26));                    // 251
    }
}
