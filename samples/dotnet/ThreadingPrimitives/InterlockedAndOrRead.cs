using System;
using System.Threading;

// Interlocked.And/Or over int/uint/long/ulong — they return the ORIGINAL value, unlike
// Add — plus Interlocked.Read and unsigned wraparound at the type boundaries.
namespace InterlockedAndOrRead;

internal static class Program
{
    internal static void __GateEntry()
    {
        // ---- And/Or, int ----
        int i = 0b1100;
        Console.WriteLine(Interlocked.Or(ref i, 0b0011));    // 12 (original)
        Console.WriteLine(Interlocked.And(ref i, 0b1010));   // 15 (original)
        Console.WriteLine(i);                                 // 10

        // ---- And/Or, uint (high bit set to prove unsigned handling) ----
        uint u = 0x80000001u;
        Console.WriteLine(Interlocked.Or(ref u, 0x40000000u));   // 2147483649
        Console.WriteLine(Interlocked.And(ref u, 0xC0000000u));  // 3221225473
        Console.WriteLine(u);                                     // 3221225472

        // ---- And/Or, long ----
        long l = 0x1_0000_000FL;
        Console.WriteLine(Interlocked.Or(ref l, 0xF0L));      // 4294967311
        Console.WriteLine(Interlocked.And(ref l, 0xFFL));     // 4294967551
        Console.WriteLine(l);                                  // 255

        // ---- And/Or, ulong ----
        ulong ul = 0x8000_0000_0000_0001UL;
        Console.WriteLine(Interlocked.Or(ref ul, 0x2UL));               // 9223372036854775809
        Console.WriteLine(Interlocked.And(ref ul, 0x8000_0000_0000_0002UL)); // 9223372036854775811
        Console.WriteLine(ul);                                           // 9223372036854775810

        // ---- Read(ref long/ulong): a seq_cst 64-bit load ----
        long rl = -9_000_000_000L;
        Console.WriteLine(Interlocked.Read(ref rl));           // -9000000000
        ulong rul = 0xFFFF_FFFF_FFFF_FFFFUL;
        Console.WriteLine(Interlocked.Read(ref rul));          // 18446744073709551615

        // ---- unsigned Add/Increment/Decrement wraparound ----
        uint uw = uint.MaxValue;
        Console.WriteLine(Interlocked.Increment(ref uw));      // 0 (wrapped)
        Console.WriteLine(Interlocked.Decrement(ref uw));      // 4294967295 (wrapped back)
        Console.WriteLine(Interlocked.Add(ref uw, 2u));        // 1 (wrapped past max)
        ulong ulw = ulong.MaxValue;
        Console.WriteLine(Interlocked.Increment(ref ulw));     // 0
        Console.WriteLine(Interlocked.Decrement(ref ulw));     // 18446744073709551615
        Console.WriteLine(Interlocked.Add(ref ulw, 3ul));      // 2
    }
}
