using System;
using System.Runtime.CompilerServices;

// Unsafe.As / AsRef reinterpretation over structs, references, `in` parameters
// and raw addresses; each lowers to a C++ pointer reinterpret.
namespace UnsafeAsSubset;

struct Pair { public int A; public int B; }

class Program
{
    internal static unsafe void __GateEntry()
    {
        // Write through the long view clears both ints.
        Pair p = new Pair { A = 0x11111111, B = 0x22222222 };
        ref long asLong = ref Unsafe.As<Pair, long>(ref p);
        Console.WriteLine(asLong);                 // host-endian bit pattern
        asLong = 0;
        Console.WriteLine($"{p.A},{p.B}");         // 0,0

        double d = 2.0;
        ref long bits = ref Unsafe.As<double, long>(ref d);
        Console.WriteLine(bits);                   // 4611686018427387904

        // As<T>(object): an unchecked downcast.
        object o = "general";
        string s = Unsafe.As<string>(o);
        Console.WriteLine(s.Length);               // 7

        // AsRef<T>(in T): a writable ref out of a readonly in-parameter.
        int local = 5;
        ref int w = ref Unsafe.AsRef(in local);
        w = 17;
        Console.WriteLine(local);                  // 17

        // AsRef<T>(void*): a writable ref over a raw address.
        long store = 123;
        ref long lr = ref Unsafe.AsRef<long>(&store);
        lr = 456;
        Console.WriteLine(store);                  // 456
    }
}
