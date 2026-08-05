using System;
using System.Runtime.CompilerServices;

// Unsafe pointer arithmetic and sizing over non-int element types. Add/Subtract
// must stride by the element's real storage width, in both the int and nint
// offset spellings.
namespace UnsafeArithSubset;

struct Rec { public long Id; public double Score; }

class Program
{
    internal static void __GateEntry()
    {
        Console.WriteLine(Unsafe.SizeOf<Rec>());      // 16
        Console.WriteLine(Unsafe.SizeOf<double>());   // 8
        Console.WriteLine(Unsafe.SizeOf<byte>());     // 1

        double[] xs = { 1.5, 2.5, 3.5, 4.5 };

        ref double a1 = ref Unsafe.Add(ref xs[0], 2);
        Console.WriteLine(a1);                        // 3.5
        ref double a2 = ref Unsafe.Add(ref xs[0], (nint)3);
        Console.WriteLine(a2);                        // 4.5
        ref double s1 = ref Unsafe.Subtract(ref a2, (nint)1);
        Console.WriteLine(s1);                        // 3.5
        ref double s2 = ref Unsafe.Subtract(ref a2, 3);
        Console.WriteLine(s2);                        // 1.5
        a1 = 30.5;                                    // write-through hits xs[2]
        Console.WriteLine(xs[2]);                     // 30.5

        nint off = Unsafe.ByteOffset(ref xs[0], ref xs[3]);
        Console.WriteLine((long)off);                 // 24

        Console.WriteLine(Unsafe.AreSame(ref xs[1], ref xs[1])); // True
        Console.WriteLine(Unsafe.AreSame(ref xs[0], ref xs[1])); // False
        Console.WriteLine(Unsafe.AreSame(ref Unsafe.Add(ref xs[0], 3), ref xs[3])); // True

        ref Rec nil = ref Unsafe.NullRef<Rec>();
        Console.WriteLine(Unsafe.IsNullRef(ref nil));            // True
        Rec real = default;
        Console.WriteLine(Unsafe.IsNullRef(ref real));           // False
    }
}
