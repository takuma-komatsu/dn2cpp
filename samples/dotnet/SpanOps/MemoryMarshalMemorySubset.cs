#nullable disable
using System;
using System.Runtime.InteropServices;

namespace MemoryMarshalMemorySubset
{
    // MemoryMarshal helpers over Memory<T>/ReadOnlyMemory<T> backing objects —
    // TryGetArray, AsMemory and TryGetString — all transpiled from the real CoreLib
    // IL, since the bodies reduce to Unsafe.As object inspection plus ArraySegment
    // construction and need no JIT intrinsic the transpiler does not already fold.
    internal static class Program
    {
        internal static void __GateEntry()
        {
            int[] arr = { 1, 2, 3, 4, 5 };

            // --- TryGetArray over an array-backed ReadOnlyMemory ---
            ReadOnlyMemory<int> rom = arr;
            bool got = MemoryMarshal.TryGetArray(rom, out ArraySegment<int> seg);
            Console.WriteLine(got);                              // True
            Console.WriteLine(ReferenceEquals(seg.Array, arr));  // True
            Console.WriteLine($"{seg.Offset}:{seg.Count}");      // 0:5

            // --- sliced memory: the segment carries the slice offset ---
            MemoryMarshal.TryGetArray(rom.Slice(1, 3), out ArraySegment<int> sseg);
            Console.WriteLine($"{sseg.Offset}:{sseg.Count}");    // 1:3

            // --- AsMemory: ReadOnlyMemory -> Memory, writes reach the array ---
            Memory<int> mem = MemoryMarshal.AsMemory(rom);
            mem.Span[0] = 42;
            Console.WriteLine(arr[0]);                           // 42

            // --- TryGetString over "hello".AsMemory() ---
            ReadOnlyMemory<char> sm = "hello".AsMemory();
            bool gs = MemoryMarshal.TryGetString(sm, out string text, out int start, out int length);
            Console.WriteLine(gs);                               // True
            Console.WriteLine($"{text}:{start}:{length}");       // hello:0:5

            // string slices keep the original string + start
            MemoryMarshal.TryGetString(sm.Slice(1, 3), out string stext, out int sstart, out int slength);
            Console.WriteLine($"{stext}:{sstart}:{slength}");    // hello:1:3

            // --- the same string-backed memory is NOT array-backed ---
            Console.WriteLine(MemoryMarshal.TryGetArray(sm, out ArraySegment<char> cseg)); // False
            Console.WriteLine(cseg.Array is null);               // True

            // --- string-backed .Span read ---
            ReadOnlySpan<char> sp = sm.Span;
            Console.WriteLine($"{sp.Length}:{sp[0]}{sp[4]}");    // 5:ho
        }
    }
}
