#nullable enable
using System;

// Pointer arithmetic edges over stackalloc, fixed heap arrays, and struct element
// strides: ptr + i / ptr - ptr (element distance), byte distance via a byte* cast,
// walking a buffer by increment/decrement, and the element stride of a struct
// pointer. ptr - ptr yields the element count, deterministic regardless of the
// actual base address. CoreLib only; diffed exact vs real .NET.
namespace PtrArithEdgesSubset;

struct Vec3 { public int X; public int Y; public int Z; }   // 12 bytes

unsafe class Program
{
    internal static void __GateEntry()
    {
        // --- stackalloc int buffer ---
        int* ip = stackalloc int[8];
        for (int i = 0; i < 8; i++) ip[i] = i * i;            // 0,1,4,9,16,25,36,49

        int* a = ip + 2;
        int* b = ip + 6;
        Console.WriteLine(b - a);                              // 4 (element distance)
        Console.WriteLine(*(a + 1));                           // ip[3] = 9
        Console.WriteLine(*(b - 2));                           // ip[4] = 16
        Console.WriteLine((int)((byte*)b - (byte*)a));         // 16 (byte distance)

        int sum = 0;
        for (int* q = ip; q < ip + 8; q++) sum += *q;
        Console.WriteLine(sum);                                // 140

        // --- fixed over a heap array ---
        int[] heap = { 10, 20, 30, 40, 50 };
        fixed (int* hp = heap)
        {
            int* mid = hp + 2;
            Console.WriteLine(mid - hp);                       // 2
            Console.WriteLine(*mid);                           // 30
            Console.WriteLine(*(mid - 1) + *(mid + 1));        // 60
            int* end = hp + heap.Length;
            Console.WriteLine(end - hp);                       // 5
            long acc = 0;
            for (int* q = end - 1; q >= hp; q--) acc = acc * 100 + *q;
            Console.WriteLine(acc);                            // 5040302010
        }

        // --- struct element stride ---
        Vec3* vp = stackalloc Vec3[3];
        for (int i = 0; i < 3; i++) { vp[i].X = i; vp[i].Y = i * 10; vp[i].Z = i * 100; }
        Console.WriteLine((vp + 2) - vp);                      // 2 (element distance)
        Console.WriteLine((int)((byte*)(vp + 1) - (byte*)vp)); // 12 (sizeof Vec3)
        Console.WriteLine(sizeof(Vec3));                       // 12
        Vec3* v2 = vp + 2;
        Console.WriteLine($"{v2->X},{v2->Y},{v2->Z}");         // 2,20,200
    }
}
