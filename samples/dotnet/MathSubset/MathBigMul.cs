using System;

// Math.BigMul across all six overloads: the widening 32-bit forms, the
// 64-bit (a, b, out low) forms returning the high half, and the
// Int128/UInt128-returning forms. The edge values stress the signed high
// half (the MinValue squares, MinValue * -1, mixed signs) and the
// full-range unsigned product; zeros anchor the trivial path. The 128-bit
// results print as their (high, low) ulong halves — Int128 decimal
// formatting routes through NumberFormatInfo, which does not transpile yet.
namespace MathBigMul;

static class Program
{
    internal static void __GateEntry()
    {
        // (int, int) -> long: widening — no truncation at the corners.
        Console.WriteLine(Math.BigMul(int.MinValue, int.MinValue));  // 4611686018427387904
        Console.WriteLine(Math.BigMul(int.MaxValue, int.MinValue));  // -4611686016279904256
        Console.WriteLine(Math.BigMul(int.MaxValue, int.MaxValue));  // 4611686014132420609
        Console.WriteLine(Math.BigMul(-3, 7));                       // -21
        Console.WriteLine(Math.BigMul(0, int.MinValue));             // 0

        // (uint, uint) -> ulong.
        Console.WriteLine(Math.BigMul(uint.MaxValue, uint.MaxValue));  // 18446744065119617025
        Console.WriteLine(Math.BigMul(uint.MaxValue, 2u));             // 8589934590
        Console.WriteLine(Math.BigMul(0u, uint.MaxValue));             // 0

        // (ulong, ulong, out ulong low) -> high half.
        ulong ulo;
        Console.WriteLine(Math.BigMul(ulong.MaxValue, ulong.MaxValue, out ulo));  // 18446744073709551614
        Console.WriteLine(ulo);                                                   // 1
        Console.WriteLine(Math.BigMul(ulong.MaxValue, 0UL, out ulo));             // 0
        Console.WriteLine(ulo);                                                   // 0
        Console.WriteLine(Math.BigMul(1UL << 63, 2UL, out ulo));                  // 1
        Console.WriteLine(ulo);                                                   // 0

        // (long, long, out long low) -> signed high half.
        long slo;
        Console.WriteLine(Math.BigMul(long.MinValue, -1L, out slo));           // 0
        Console.WriteLine(slo);                                                // -9223372036854775808
        Console.WriteLine(Math.BigMul(long.MinValue, long.MinValue, out slo)); // 4611686018427387904
        Console.WriteLine(slo);                                                // 0
        Console.WriteLine(Math.BigMul(long.MaxValue, -2L, out slo));           // -1
        Console.WriteLine(slo);                                                // 2
        Console.WriteLine(Math.BigMul(-3L, 7L, out slo));                      // -1
        Console.WriteLine(slo);                                                // -21
        Console.WriteLine(Math.BigMul(0L, long.MinValue, out slo));            // 0
        Console.WriteLine(slo);                                                // 0

        // (long, long) -> Int128, printed as (high, low) ulong halves.
        PrintInt128(Math.BigMul(3L, 5L));                       // 0, 15
        PrintInt128(Math.BigMul(long.MinValue, long.MinValue)); // 2^62, 0
        PrintInt128(Math.BigMul(long.MinValue, -1L));           // 0, 2^63
        PrintInt128(Math.BigMul(long.MaxValue, -2L));           // all-ones, 2
        PrintInt128(Math.BigMul(-3L, 7L));                      // all-ones, -21's bits

        // (ulong, ulong) -> UInt128.
        PrintUInt128(Math.BigMul(ulong.MaxValue, ulong.MaxValue)); // 2^64-2, 1
        PrintUInt128(Math.BigMul(0UL, ulong.MaxValue));            // 0, 0
        PrintUInt128(Math.BigMul(1UL << 63, 4UL));                 // 2, 0
    }

    private static void PrintInt128(Int128 v)
    {
        Console.WriteLine((ulong)(v >> 64));
        Console.WriteLine((ulong)v);
    }

    private static void PrintUInt128(UInt128 v)
    {
        Console.WriteLine((ulong)(v >> 64));
        Console.WriteLine((ulong)v);
    }
}
