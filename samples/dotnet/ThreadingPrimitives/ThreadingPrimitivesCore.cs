using System;
using System.Threading;

// Single-threaded correctness of the synchronization primitives (Interlocked atomics,
// Volatile, [ThreadStatic], MemoryBarrier): single-threaded semantics are identical to
// real .NET, so the diff pins the lowering. Genuine racing lives in the Thread gate.
namespace ThreadingPrimitivesCore;

internal static class Program
{
    [ThreadStatic] static int t_value;

    static int s_i4;
    static long s_i8;
    static object? s_ref;
    static int s_vol;
    static long s_volL;
    static double s_volD;
    static bool s_flag;

    internal static void __GateEntry()
    {
        // ---- Interlocked, 32-bit ----
        s_i4 = 10;
        Console.WriteLine(Interlocked.Increment(ref s_i4));                  // 11
        Console.WriteLine(Interlocked.Decrement(ref s_i4));                  // 10
        Console.WriteLine(Interlocked.Add(ref s_i4, 5));                     // 15
        Console.WriteLine(Interlocked.Exchange(ref s_i4, 100));             // 15 (old)
        Console.WriteLine(s_i4);                                            // 100
        Console.WriteLine(Interlocked.CompareExchange(ref s_i4, 200, 100)); // 100 (old) -> set 200
        Console.WriteLine(s_i4);                                            // 200
        Console.WriteLine(Interlocked.CompareExchange(ref s_i4, 999, 1));   // 200 (old) -> no change
        Console.WriteLine(s_i4);                                            // 200

        // ---- Interlocked, 64-bit ----
        s_i8 = 1_000_000_000_000L;
        Console.WriteLine(Interlocked.Increment(ref s_i8));                 // 1000000000001
        Console.WriteLine(Interlocked.Add(ref s_i8, 5));                    // 1000000000006
        Console.WriteLine(Interlocked.Exchange(ref s_i8, 42));             // 1000000000006 (old)
        Console.WriteLine(s_i8);                                            // 42

        // ---- Interlocked, reference ----
        object a = new object();
        object b = new object();
        s_ref = a;
        object? prev = Interlocked.Exchange(ref s_ref, b);
        Console.WriteLine(ReferenceEquals(prev, a));                        // True
        Console.WriteLine(ReferenceEquals(s_ref, b));                       // True
        object? cx = Interlocked.CompareExchange(ref s_ref, a, b);
        Console.WriteLine(ReferenceEquals(cx, b));                          // True (old)
        Console.WriteLine(ReferenceEquals(s_ref, a));                       // True (swapped)

        Interlocked.MemoryBarrier();

        // ---- Volatile.Read/Write ----
        Volatile.Write(ref s_vol, 7);
        Console.WriteLine(Volatile.Read(ref s_vol));                        // 7
        Volatile.Write(ref s_volL, 9_000_000_000L);
        Console.WriteLine(Volatile.Read(ref s_volL));                       // 9000000000
        Volatile.Write(ref s_volD, 3.5);
        Console.WriteLine(Volatile.Read(ref s_volD));                       // 3.5
        Volatile.Write(ref s_flag, true);
        Console.WriteLine(Volatile.Read(ref s_flag));                       // True

        // ---- [ThreadStatic] on the main thread (behaves like a normal static) ----
        Console.WriteLine(t_value);                                         // 0 (default)
        t_value = 123;
        Console.WriteLine(t_value);                                         // 123
    }
}
