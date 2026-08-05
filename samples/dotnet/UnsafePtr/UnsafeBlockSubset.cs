#nullable enable
using System;
using System.Runtime.CompilerServices;

// Block memory ops, reached two ways: the raw `cpblk`/`initblk` opcodes that a
// `stackalloc T[] {...}` blob-init emits, and the Unsafe CopyBlock/InitBlock
// intrinsics whose real bodies are literally those opcodes. Both map to
// std::memmove / std::memset.
namespace UnsafeBlockSubset;

class Program
{
    internal static unsafe void __GateEntry()
    {
        // void* overloads over stack buffers.
        byte* buf = stackalloc byte[8];
        Unsafe.InitBlock(buf, 0xAB, 8);
        Console.WriteLine($"{buf[0]},{buf[3]},{buf[7]}"); // 171,171,171

        byte* src = stackalloc byte[4];
        for (int i = 0; i < 4; i++) src[i] = (byte)(i + 1);
        byte* dst = stackalloc byte[4];
        Unsafe.CopyBlock(dst, src, 4);
        Console.WriteLine($"{dst[0]},{dst[1]},{dst[2]},{dst[3]}"); // 1,2,3,4

        // ref-byte overloads over a managed array.
        byte[] arr = new byte[5];
        Unsafe.InitBlock(ref arr[0], 7, 5);
        Console.WriteLine($"{arr[0]},{arr[2]},{arr[4]}");  // 7,7,7

        byte[] copy = new byte[5];
        Unsafe.CopyBlock(ref copy[0], ref arr[0], 5);
        Console.WriteLine($"{copy[0]},{copy[4]}");         // 7,7

        // Unaligned variants; the alignment hint is ignored.
        byte* u = stackalloc byte[4];
        Unsafe.InitBlockUnaligned(u, 9, 4);
        byte* u2 = stackalloc byte[4];
        Unsafe.CopyBlockUnaligned(u2, u, 4);
        Console.WriteLine($"{u2[0]},{u2[1]},{u2[2]},{u2[3]}"); // 9,9,9,9

        // Raw `cpblk` from an RVA blob: a MUTABLE Span is required, or Roslyn
        // emits a CreateSpan alias over the blob instead of a copy.
        Span<byte> blob = stackalloc byte[]
            { 10, 20, 30, 40, 50, 60, 70, 80, 90, 100, 110, 120, 130, 140, 150, 160 };
        Console.WriteLine($"{blob[0]},{blob[7]},{blob[15]}"); // 10,80,160
        blob[0] = 99;                                          // and it's writable
        Console.WriteLine(blob[0]);                            // 99
    }
}
