#nullable enable
using System;

// The `fixed` statement over every source shape. Arrays and interior pointers
// ride the non-moving GC plus ldelema/conv; strings and spans lower through
// GetPinnableReference, transpiled from the real BCL body.
namespace FixedStatementSubset;

unsafe class Program
{
    internal static void __GateEntry()
    {
        // (1) An array; the write-through must reach the array.
        int[] arr = { 10, 20, 30 };
        fixed (int* p = arr)
        {
            Console.WriteLine(p[0] + p[1] + p[2]); // 60
            p[1] = 99;
        }
        Console.WriteLine(arr[1]);                  // 99

        // (2) An interior pointer.
        fixed (int* p = &arr[2])
        {
            Console.WriteLine(*p);                  // 30
        }

        // (3) A sub-word (packed) array.
        byte[] bytes = { 1, 2, 3, 4 };
        fixed (byte* p = bytes)
        {
            Console.WriteLine(p[0] + p[3]);         // 5
        }

        // (4) A string literal.
        string s = "Hello";
        fixed (char* p = s)
        {
            int sum = 0;
            for (int i = 0; i < s.Length; i++) sum += p[i];
            Console.WriteLine(sum);                 // 500 (H+e+l+l+o)
            Console.WriteLine((int)p[0]);           // 72
        }

        // (5) A HEAP string: the NUL-terminated alloc path, not the literal one.
        string heap = string.Concat("ab", "cd");
        fixed (char* p = heap)
        {
            Console.WriteLine((int)p[0]);           // 97 'a'
            Console.WriteLine((int)p[3]);           // 100 'd'
        }

        // (6) The empty string pins to a valid ref to the terminating '\0', not
        // null: GetPinnableReference has no length-zero special case, so reading
        // p[0] must yield 0 rather than fault.
        string empty = "";
        fixed (char* p = empty)
        {
            Console.WriteLine((int)p[0]);           // 0
        }

        // (7) Span<T>.
        Span<int> sp = stackalloc int[3];
        sp[0] = 7; sp[1] = 8; sp[2] = 9;
        fixed (int* p = sp)
        {
            Console.WriteLine(p[0] + p[1] + p[2]);  // 24
            p[0] = 70;
        }
        Console.WriteLine(sp[0]);                   // 70

        // (8) ReadOnlySpan<char>.
        ReadOnlySpan<char> ros = "World";
        fixed (char* p = ros)
        {
            Console.WriteLine((int)p[0]);           // 87 'W'
            Console.WriteLine((int)p[4]);           // 100 'd'
        }
    }
}
