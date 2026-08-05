#nullable disable
using System;

namespace MemorySpanSubset
{
    // ReadOnlyMemory<T>.Span / Memory<T>.Span over an array-backed buffer. The Span
    // getter calls RuntimeHelpers.ObjectHasComponentSize to pick the array-backed
    // branch over the MemoryManager one, so reading through .Span exercises that
    // intrinsic end to end.
    internal static class Program
    {
        internal static void __GateEntry()
        {
            byte[] arr = { 10, 20, 30, 40, 50, 60 };

            ReadOnlyMemory<byte> rom = arr;
            ReadOnlySpan<byte> rs = rom.Span;
            Console.WriteLine(rs.Length);        // 6
            Console.WriteLine(rs[0] + rs[5]);    // 70

            ReadOnlyMemory<byte> slice = rom.Slice(2, 3);
            ReadOnlySpan<byte> ss = slice.Span;
            Console.WriteLine(ss.Length);        // 3
            Console.WriteLine(ss[0] + ss[2]);    // 80

            Memory<byte> mem = arr;
            mem.Span[0] = 99;                    // write-through to arr
            Console.WriteLine(arr[0]);           // 99
        }
    }
}
