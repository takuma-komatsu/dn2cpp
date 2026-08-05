using System;
using System.Runtime.CompilerServices;
using System.Threading;

// [MethodImpl(Synchronized)] bodies run under a per-object monitor — instance = this,
// static = the declaring type's interned Type object, so a lock(typeof(X)) site and the
// method mutually exclude. The monitor is recursive and releases on every return path
// and on exceptional unwind. The inlining hints only steer codegen, so they are here
// for link/run coverage.
namespace MethodImplSubset;

internal static class Program
{
    private const int Threads = 8;
    private const int Iterations = 20000;

    private sealed class SyncCounter
    {
        private int _n;

        // Deliberately non-atomic read-modify-write: without the Synchronized monitor,
        // the concurrent increments would lose updates.
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Add()
        {
            _n = _n + 1;
        }

        public int Value => _n;

        [MethodImpl(MethodImplOptions.Synchronized)]
        public int AddRange(int depth)
        {
            _n = _n + 1;
            if (depth > 1)
                return 1 + AddRange(depth - 1); // recursive re-entry under the lock
            return 1;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void AddTwice()
        {
            Add(); // same-receiver cross-call re-enters the same monitor
            Add();
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void AddOrThrow(bool fail)
        {
            _n = _n + 1;
            if (fail)
                throw new InvalidOperationException("sync-throw");
        }
    }

    private static int s_n;
    private static int s_typeLocked;

    [MethodImpl(MethodImplOptions.Synchronized)]
    private static void StaticAdd()
    {
        s_n = s_n + 1;
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    private static void TypeLockedAdd()
    {
        s_typeLocked = s_typeLocked + 1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int NoInlineSum(int a, int b)
    {
        return a + b;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int InlineSum(int a, int b)
    {
        return a + b;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int InlineFib(int n)
    {
        // `inline` is only a linkage hint in the C++ output, so a recursive
        // AggressiveInlining method must still compile and run.
        return n < 2 ? n : InlineFib(n - 1) + InlineFib(n - 2);
    }

    internal static void Run()
    {
        // (a) Instance Synchronized: threads hammer a non-atomic increment.
        var counter = new SyncCounter();
        var adders = new Thread[Threads];
        for (int i = 0; i < Threads; i++)
        {
            adders[i] = new Thread(() =>
            {
                for (int k = 0; k < Iterations; k++)
                    counter.Add();
            });
            adders[i].Start();
        }
        for (int i = 0; i < Threads; i++) adders[i].Join();
        Console.WriteLine(counter.Value);         // 160000

        // (b) Static Synchronized: the monitor key is the declaring type.
        var staticAdders = new Thread[Threads];
        for (int i = 0; i < Threads; i++)
        {
            staticAdders[i] = new Thread(() =>
            {
                for (int k = 0; k < Iterations; k++)
                    StaticAdd();
            });
            staticAdders[i].Start();
        }
        for (int i = 0; i < Threads; i++) staticAdders[i].Join();
        Console.WriteLine(s_n);                   // 160000

        // (c) Recursive Synchronized re-entry (recursive monitor).
        var rec = new SyncCounter();
        Console.WriteLine(rec.AddRange(5));       // 5
        Console.WriteLine(rec.Value);             // 5

        // (e) Same-receiver cross-call between two Synchronized methods.
        rec.AddTwice();
        Console.WriteLine(rec.Value);             // 7

        // (d) Exceptional unwind releases the monitor: a second thread must still be
        // able to enter, since a leaked lock would hang this Join forever.
        var thrower = new SyncCounter();
        try
        {
            thrower.AddOrThrow(true);
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine(ex.Message);        // sync-throw
        }
        var after = new Thread(() => thrower.AddOrThrow(false));
        after.Start();
        after.Join();
        Console.WriteLine(thrower.Value);         // 2

        // (f) Inlining hints: behavioral no-ops, so this is link/run coverage.
        Console.WriteLine(NoInlineSum(20, 22));   // 42
        Console.WriteLine(InlineSum(19, 23));     // 42
        Console.WriteLine(InlineFib(10));         // 55

        // (g) lock(typeof(X)): the interned Type object gives every lock site the same
        // identity, so racing non-atomic increments stay exact.
        var typeofLockers = new Thread[Threads];
        int typeofLocked = 0;
        for (int i = 0; i < Threads; i++)
        {
            typeofLockers[i] = new Thread(() =>
            {
                for (int k = 0; k < Iterations; k++)
                {
                    lock (typeof(SyncCounter))
                    {
                        typeofLocked = typeofLocked + 1;
                    }
                }
            });
            typeofLockers[i].Start();
        }
        for (int i = 0; i < Threads; i++) typeofLockers[i].Join();
        Console.WriteLine(typeofLocked);          // 160000

        // (h) A static Synchronized method locks the same interned Type object that
        // lock(typeof(Program)) takes, so the two kinds of site mutually exclude.
        var mixed = new Thread[Threads];
        for (int i = 0; i < Threads; i++)
        {
            bool viaMethod = i % 2 == 0;
            mixed[i] = new Thread(() =>
            {
                for (int k = 0; k < Iterations; k++)
                {
                    if (viaMethod)
                    {
                        TypeLockedAdd();
                    }
                    else
                    {
                        lock (typeof(Program))
                        {
                            s_typeLocked = s_typeLocked + 1;
                        }
                    }
                }
            });
            mixed[i].Start();
        }
        for (int i = 0; i < Threads; i++) mixed[i].Join();
        Console.WriteLine(s_typeLocked);          // 160000
    }
}
