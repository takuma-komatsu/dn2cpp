using System;
using System.Numerics;
using System.Threading;

namespace GenericMathNativeWasm;

internal static partial class Program
{
    // A native integer element is 4 bytes here; an 8-byte atomic would also
    // rewrite its live neighbour.
    private static void Atomics<T>(string label, T before, T after) where T : unmanaged, IBinaryInteger<T>
    {
        T zero = T.Zero;
        T[] array = { before, after, after };
        T old = Interlocked.And<T>(ref array[0], zero);
        Check(label + ".and", old == before && array[0] == zero && array[1] == after && array[2] == after);
        array[0] = before;
        old = Interlocked.Or<T>(ref array[0], after);
        Check(label + ".or", old == before && array[0] == (before | after) && array[1] == after);
        array[0] = before;
        old = Interlocked.Exchange<T>(ref array[0], zero);
        Check(label + ".exchange", old == before && array[0] == zero && array[1] == after);
        old = Interlocked.CompareExchange<T>(ref array[0], before, zero);
        Check(label + ".cmpxchg", old == zero && array[0] == before && array[1] == after && array[2] == after);
        Console.WriteLine(label + " atomics native32 end");
        return;
    }
}
