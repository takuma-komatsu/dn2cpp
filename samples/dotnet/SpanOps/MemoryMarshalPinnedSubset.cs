#nullable disable
using System;
using System.Buffers;
using System.Runtime.InteropServices;

namespace MemoryMarshalPinnedSubset
{
    // MemoryMarshal.CreateFromPinnedArray, Memory<T>.Pin()/MemoryHandle.Dispose and
    // MemoryMarshal.ToEnumerable, all transpiled from the real CoreLib IL. dn2cpp's
    // GC is non-moving, so pinning is identity: the caller contract always holds and
    // Pin()'s GCHandle is a strong handle whose address stays valid regardless.
    internal static class Program
    {
        internal static void __GateEntry()
        {
            // --- CreateFromPinnedArray: Memory<T> over an array slice, write-through ---
            int[] arr = { 10, 20, 30, 40, 50, 60 };
            Memory<int> pinned = MemoryMarshal.CreateFromPinnedArray(arr, 1, 4);
            Console.WriteLine(pinned.Length);        // 4
            Span<int> ps = pinned.Span;
            Console.WriteLine(ps[0]);                // 20
            ps[3] = 99;
            Console.WriteLine(arr[4]);               // 99

            // --- Pin(): the MemoryHandle's pointer addresses the memory's element 0 ---
            unsafe
            {
                MemoryHandle h = pinned.Pin();
                int* p = (int*)h.Pointer;
                Console.WriteLine(p[0] + p[3]);      // 20 + 99 = 119
                h.Dispose();
            }
            Console.WriteLine(pinned.Span[1]);       // 30 (still valid after unpin)

            // --- ToEnumerable: array-backed, full length (returns the array itself) ---
            char[] chars = { 'a', 'b', 'c' };
            ReadOnlyMemory<char> cm = chars;
            foreach (char c in MemoryMarshal.ToEnumerable(cm)) Console.Write(c);
            Console.WriteLine();                     // abc

            // --- ToEnumerable: array-backed slice (iterator over [offset, offset+len)) ---
            int[] nums = { 1, 2, 3, 4, 5 };
            int sum = 0;
            foreach (int v in MemoryMarshal.ToEnumerable(new ReadOnlyMemory<int>(nums, 1, 3))) sum += v;
            Console.WriteLine(sum);                  // 2+3+4 = 9

            // --- ToEnumerable: string-backed slice ---
            ReadOnlyMemory<char> sm = "worlds".AsMemory().Slice(1, 4);
            foreach (char c in MemoryMarshal.ToEnumerable(sm)) Console.Write(c);
            Console.WriteLine();                     // orld

            // --- ToEnumerable: empty memory fast path ---
            int emptyCount = 0;
            foreach (int v in MemoryMarshal.ToEnumerable(ReadOnlyMemory<int>.Empty)) emptyCount++;
            Console.WriteLine(emptyCount);           // 0

            // --- ToEnumerable: string-backed, full length (returns the string itself
            // as the IEnumerable<char>; dispatch resolves on String's interface map) ---
            ReadOnlyMemory<char> fm = "hello".AsMemory();
            foreach (char c in MemoryMarshal.ToEnumerable(fm)) Console.Write(c);
            Console.WriteLine();                     // hello
        }
    }
}
