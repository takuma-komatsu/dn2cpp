using System;
using System.Runtime.CompilerServices;

// Unsafe.Unbox<T>(object) hands back a ref that ALIASES the box's payload, not a
// copy: each arm mutates through the ref and reads back with a plain (T)obj cast
// on the same box. Known divergence, not exercised here: .NET allows Unbox<int>
// on a boxed enum via underlying-type identity, dn2cpp requires the exact type.
namespace UnsafeUnboxSubset;

struct Pair { public long A; public double B; }   // blittable, 16 bytes

class Program
{
    internal static void __GateEntry()
    {
        object oi = 41;
        ref int ri = ref Unsafe.Unbox<int>(oi);
        ri++;
        Console.WriteLine((int)oi);            // 42

        object ol = 1234567890123456789L;
        ref long rl = ref Unsafe.Unbox<long>(ol);
        Console.WriteLine(rl);                 // 1234567890123456789
        rl = -1;
        Console.WriteLine((long)ol);           // -1

        // In-place arithmetic on the boxed payload.
        object od = 1.5;
        Unsafe.Unbox<double>(od) *= 4.0;
        Console.WriteLine((double)od);         // 6

        // One field mutated through the ref; the other survives.
        object op = new Pair { A = 7, B = 2.25 };
        ref Pair rp = ref Unsafe.Unbox<Pair>(op);
        rp.A = 99;
        Pair back = (Pair)op;
        Console.WriteLine($"{back.A}:{back.B}"); // 99:2.25
    }
}
