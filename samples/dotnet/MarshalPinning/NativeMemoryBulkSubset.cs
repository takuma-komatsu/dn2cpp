#nullable enable
using System;
using System.Runtime.InteropServices;

// NativeMemory bulk byte operations over a raw Alloc'd block: Fill stamps a byte
// value across a range (read back at the edges and middle), Copy is memmove-style
// overlap-safe in BOTH directions (forward and backward overlapping ranges over an
// asymmetric ramp pattern), and Clear zeroes a range without touching neighbours.
// No raw addresses are printed. CoreLib only; diffed exact vs real .NET.
namespace NativeMemoryBulkSubset;

unsafe class Program
{
    internal static void __GateEntry()
    {
        byte* p = (byte*)NativeMemory.Alloc(64);

        // Fill: stamp 0x5A across the whole block, check edges + middle.
        NativeMemory.Fill(p, 64, 0x5A);
        Console.WriteLine(p[0]);   // 90
        Console.WriteLine(p[31]);  // 90
        Console.WriteLine(p[63]);  // 90

        // Asymmetric ramp so a shifted copy is detectable: p[i] = i.
        for (int i = 0; i < 64; i++)
            p[i] = (byte)i;

        // Overlapping Copy, forward (source below destination): [8..24) -> [16..32).
        // A naive ascending byte loop would smear; memmove semantics must hold.
        NativeMemory.Copy(p + 8, p + 16, 16);
        Console.WriteLine(p[16]);  // 8
        Console.WriteLine(p[23]);  // 15
        Console.WriteLine(p[31]);  // 23
        Console.WriteLine(p[32]);  // 32 (just past the copy, untouched)

        // Re-ramp, then overlapping Copy backward (source above destination):
        // [16..32) -> [8..24).
        for (int i = 0; i < 64; i++)
            p[i] = (byte)i;
        NativeMemory.Copy(p + 16, p + 8, 16);
        Console.WriteLine(p[8]);   // 16
        Console.WriteLine(p[23]);  // 31
        Console.WriteLine(p[7]);   // 7 (just below the copy, untouched)
        Console.WriteLine(p[24]);  // 24 (source tail keeps its own value)

        // Clear: zero the middle 16 bytes only; the neighbours survive.
        NativeMemory.Clear(p + 24, 16);
        Console.WriteLine(p[24]);  // 0
        Console.WriteLine(p[39]);  // 0
        Console.WriteLine(p[23]);  // 31
        Console.WriteLine(p[40]);  // 40

        NativeMemory.Free(p);
    }
}
