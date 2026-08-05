#nullable enable
using System;
using System.Runtime.InteropServices;

// A raw pointer in integer arithmetic. The `void*`->`nuint` cast emits no IL
// `conv` (pointer and native int share the stack representation), so the pointer
// reaches binary-op codegen directly: add/sub stay byte-addressed pointer
// arithmetic, every other op takes the uintptr_t address value. Output is
// address-independent because a 64-aligned block has its low 6 bits zero.
namespace PointerArithSubset;

unsafe class Program
{
    internal static void __GateEntry()
    {
        void* p = NativeMemory.AlignedAlloc(64, 64);
        byte* bp = (byte*)p;

        // The two canonical alignment idioms, inline with no local.
        Console.WriteLine((nuint)p % 64 == 0);              // True
        Console.WriteLine((nuint)p % 16 == 0);              // True
        Console.WriteLine(((nuint)p & 63) == 0);            // True
        Console.WriteLine(((nuint)p & 15) == 0);            // True

        // A known byte offset surfaces in the low bits.
        Console.WriteLine((nuint)(bp + 5) % 4);             // 1
        Console.WriteLine((nuint)(bp + 10) & 7);            // 2

        Console.WriteLine((nuint)p / 64 * 64 == (nuint)p);  // True
        Console.WriteLine(((nuint)p | 7) % 8);              // 7
        Console.WriteLine(((nuint)p ^ 5) % 8);              // 5

        // The local round-trip: stloc coerces the pointer into a nuint local.
        nuint addr = (nuint)p;
        Console.WriteLine(addr % 32 == 0);                  // True

        NativeMemory.AlignedFree(p);
    }
}
