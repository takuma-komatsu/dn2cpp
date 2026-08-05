#nullable enable
using System;
using System.Runtime.CompilerServices;

// The residual Unsafe surface — BitCast, ByteOffset, SubtractByteOffset, the
// IsAddress* pair, SkipInit — each lowering to a small C++ form: a memcpy
// reinterpret, pointer arithmetic, a uintptr_t comparison, or a no-op.
namespace UnsafeSurfaceSubset;

unsafe class Program
{
    internal static void __GateEntry()
    {
        Console.WriteLine(Unsafe.BitCast<int, float>(1065353216));   // 1
        Console.WriteLine(Unsafe.BitCast<float, int>(1.5f));         // 1069547520
        Console.WriteLine(Unsafe.BitCast<uint, int>(0x80000000u));   // -2147483648
        Console.WriteLine(Unsafe.BitCast<long, double>(4607182418800017408L)); // 1
        Console.WriteLine(Unsafe.BitCast<double, long>(2.0));        // 4611686018427387904

        int[] arr = { 10, 20, 30, 40 };

        // ByteOffset is target - origin, so it must go negative when reversed.
        nint fwd = Unsafe.ByteOffset(ref arr[0], ref arr[3]);
        nint back = Unsafe.ByteOffset(ref arr[3], ref arr[0]);
        Console.WriteLine((long)fwd);   // 12
        Console.WriteLine((long)back);  // -12

        // Unsigned address ordering, both strict at equality.
        Console.WriteLine(Unsafe.IsAddressGreaterThan(ref arr[3], ref arr[0])); // True
        Console.WriteLine(Unsafe.IsAddressGreaterThan(ref arr[0], ref arr[0])); // False
        Console.WriteLine(Unsafe.IsAddressLessThan(ref arr[0], ref arr[3]));    // True
        Console.WriteLine(Unsafe.IsAddressLessThan(ref arr[3], ref arr[3]));    // False

        ref int r = ref Unsafe.SubtractByteOffset(ref arr[3], (nint)8);
        Console.WriteLine(r);  // 20
        r = 99;                // a real ref: the write must reach arr[1]
        Console.WriteLine(arr[1]); // 99

        // SkipInit: a no-op that satisfies definite assignment.
        Unsafe.SkipInit(out int x);
        x = 42;
        Console.WriteLine(x);  // 42
    }
}
