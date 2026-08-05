using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// The same reinterpret over the three memory backings the BCL intrinsics must
// support uniformly — stackalloc, heap array, struct field — each written back
// through to confirm the view aliases the original storage.
namespace BackingsSubset;

struct Quad { public int A; public int B; public int C; public int D; }
struct Box { public Quad Inner; }

class Program
{
    internal static unsafe void __GateEntry()
    {
        // Stackalloc, via MemoryMarshal.Cast.
        Span<int> stack = stackalloc int[2];
        stack[0] = 0x04030201; stack[1] = 0x08070605;
        Span<byte> sb = MemoryMarshal.Cast<int, byte>(stack);
        Console.WriteLine($"{sb.Length}:{sb[0]},{sb[4]}");          // 8:1,5 (LE)

        // Heap struct array, via Unsafe.As.
        Quad[] q = { new Quad { A = 1, B = 2, C = 3, D = 4 } };
        ref int first = ref Unsafe.As<Quad, int>(ref q[0]);
        Console.WriteLine(first);                                   // 1
        first = 100;                                                // write-through
        Console.WriteLine(q[0].A);                                  // 100

        // Struct field, via CreateSpan.
        Box box = default;
        box.Inner = new Quad { A = 5, B = 6, C = 7, D = 8 };
        ref int innerRef = ref Unsafe.As<Quad, int>(ref box.Inner);
        Span<int> view = MemoryMarshal.CreateSpan(ref innerRef, 4);
        Console.WriteLine($"{view[0]},{view[1]},{view[2]},{view[3]}"); // 5,6,7,8
        view[3] = 88;
        Console.WriteLine(box.Inner.D);                            // 88
    }
}
