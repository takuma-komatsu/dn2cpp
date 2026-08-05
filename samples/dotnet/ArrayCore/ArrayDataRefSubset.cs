#nullable disable
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ArrayDataRefSubset
{
    // MemoryMarshal.GetArrayDataReference<T>(T[]) returns a ref to element 0.
    // Previously emitted as an infinite self-recursive call (stack overflow).
    internal static class Program
    {
        internal static void Run()
        {
            int[] a = { 5, 6, 7, 8 };
            ref int r0 = ref MemoryMarshal.GetArrayDataReference(a);
            Console.WriteLine(r0);                       // 5
            r0 = 99;                                     // write through the ref
            Console.WriteLine(a[0]);                     // 99
            ref int r2 = ref Unsafe.Add(ref r0, 2);
            Console.WriteLine(r2);                       // 7
            r2 = 42;
            Console.WriteLine(a[2]);                     // 42

            string[] s = { "x", "y", "z" };
            ref string sr = ref MemoryMarshal.GetArrayDataReference(s);
            Console.WriteLine(sr);                       // x
            Console.WriteLine(Unsafe.Add(ref sr, 2));    // z
        }
    }
}
