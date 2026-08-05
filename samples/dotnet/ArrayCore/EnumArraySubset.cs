#nullable enable
using System;

namespace EnumArraySubset
{
    // Non-int underlying types: their arrays pack at the underlying width.
    enum ByteE : byte { A = 1, B = 100, C = 200 }
    enum ShortE : short { X = -5, Y = 1000, Z = 30000 }
    enum LongE : long { L1 = 1, L2 = 2, L3 = 3 }
    enum IntE { I0, I1, I2 } // int underlying: the unchanged i4 path (sanity)

    static class Program
    {internal static void Run()
        {
            // Direct `new T[]` + element set/read — the shape that mis-read before:
            // newarr allocated an i4 array but ldelem.u1 read it as a packed buffer.
            Console.WriteLine("== byte array ==");
            ByteE[] ba = new ByteE[3];
            ba[0] = ByteE.A;
            ba[1] = ByteE.B;
            ba[2] = ByteE.C;
            foreach (ByteE e in ba)
                Console.WriteLine((int)e);     // 1 100 200
            foreach (ByteE e in ba)
                Console.WriteLine(e);          // A B C (boxed enum name)

            // Array-initializer form over a short-backed enum.
            Console.WriteLine("== short array ==");
            ShortE[] sa = { ShortE.X, ShortE.Y, ShortE.Z };
            foreach (ShortE e in sa)
                Console.WriteLine((int)e);     // -5 1000 30000

            // A long-backed enum (small in-range values).
            Console.WriteLine("== long array ==");
            LongE[] la = new LongE[] { LongE.L1, LongE.L2, LongE.L3 };
            foreach (LongE e in la)
                Console.WriteLine((long)e);    // 1 2 3

            // Enum.GetValues<T> now builds the array in the matching representation.
            Console.WriteLine("== GetValues byte ==");
            foreach (ByteE e in Enum.GetValues<ByteE>())
                Console.WriteLine((int)e);     // 1 100 200
            Console.WriteLine("== GetValues short ==");
            foreach (ShortE e in Enum.GetValues<ShortE>())
                Console.WriteLine((int)e);     // -5 1000 30000
            Console.WriteLine("== GetValues long ==");
            foreach (LongE e in Enum.GetValues<LongE>())
                Console.WriteLine((long)e);    // 1 2 3
            Console.WriteLine("== GetValues int ==");
            foreach (IntE e in Enum.GetValues<IntE>())
                Console.WriteLine((int)e);     // 0 1 2
        }
    }
}
