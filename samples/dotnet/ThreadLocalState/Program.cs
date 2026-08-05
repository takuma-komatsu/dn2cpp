using System;
using System.Globalization;
using System.Threading;

// ThreadLocal<T>: per-instance, per-thread storage with an optional lazy valueFactory.
// Cross-thread results are accumulated with Interlocked and read only AFTER Join, so a
// bleed, or a factory that ran the wrong number of times, breaks the fixed totals.
static class Program
{
    const int N = 8;

    sealed class Box
    {
        public int V;
        public Box(int v) { V = v; }
    }

    // Shared by every worker: each thread must see only its own value.
    static readonly ThreadLocal<int> s_tlInt = new ThreadLocal<int>();
    static readonly ThreadLocal<Box?> s_tlBox = new ThreadLocal<Box?>(); // reference-type T

    // Lazy factory: runs once per thread on first access, numbering threads 1..N.
    static int s_factoryCounter;
    static readonly ThreadLocal<int> s_tlFactory =
        new ThreadLocal<int>(() => Interlocked.Increment(ref s_factoryCounter));

    static int s_readbackSum;  // sum of per-thread int read-backs
    static int s_boxSum;       // sum of per-thread Box.V read-backs
    static long s_factorySum;  // sum of per-thread factory values
    static int s_arrived;      // spin-barrier counter

    static void Worker(object? arg)
    {
        int id = (int)arg!;
        s_tlInt.Value = id * 100;
        s_tlBox.Value = new Box(id);

        // Barrier: nobody reads back until every thread has written, so the read-back
        // genuinely proves isolation.
        Interlocked.Increment(ref s_arrived);
        while (Volatile.Read(ref s_arrived) < N)
            Thread.Yield();

        Interlocked.Add(ref s_readbackSum, s_tlInt.Value);
        Interlocked.Add(ref s_boxSum, s_tlBox.Value!.V);

        int fv = s_tlFactory.Value;   // first access on this thread: the factory runs once
        Interlocked.Add(ref s_factorySum, fv);
    }

    static void Main()
    {
        // Pin both cultures first: gate output must not depend on the host locale (see AGENTS.md).
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

        var threads = new Thread[N];
        for (int i = 0; i < N; i++)
            threads[i] = new Thread(Worker);
        for (int i = 0; i < N; i++)
            threads[i].Start(i);
        for (int i = 0; i < N; i++)
            threads[i].Join();

        Console.WriteLine(s_readbackSum);   // 100 * (0+1+...+7) = 2800
        Console.WriteLine(s_boxSum);        // 0+1+...+7 = 28

        // The factory ran exactly once per thread: values 1..N, counter == N.
        Console.WriteLine(s_factoryCounter); // 8
        Console.WriteLine(s_factorySum);     // 1+2+...+8 = 36

        // default(T) + IsValueCreated on the main thread.
        var tlDef = new ThreadLocal<int>();
        Console.WriteLine(tlDef.IsValueCreated); // False (no access yet)
        Console.WriteLine(tlDef.Value);          // 0 (default, created on first read)
        Console.WriteLine(tlDef.IsValueCreated); // True
        tlDef.Value = 99;
        Console.WriteLine(tlDef.Value);          // 99

        var tlRef = new ThreadLocal<Box?>();
        Console.WriteLine(tlRef.IsValueCreated); // False
        Console.WriteLine(tlRef.Value is null);  // True (default(Box) is null)
        Console.WriteLine(tlRef.IsValueCreated); // True

        // The factory caches per thread: the second read reuses the value.
        int mainCounter = 0;
        var tlFac = new ThreadLocal<int>(() => Interlocked.Increment(ref mainCounter));
        Console.WriteLine(tlFac.IsValueCreated); // False
        Console.WriteLine(tlFac.Value);          // 1 (factory runs)
        Console.WriteLine(tlFac.Value);          // 1 (cached; factory not re-run)
        Console.WriteLine(mainCounter);          // 1

        // Other value kinds through the factory + boxed slot.
        var tlLong = new ThreadLocal<long>(() => 9_000_000_000L);
        Console.WriteLine(tlLong.Value);         // 9000000000
        var tlDouble = new ThreadLocal<double>(() => 3.5);
        Console.WriteLine(tlDouble.Value);       // 3.5
        var tlFloat = new ThreadLocal<float>(() => 1.5f);
        Console.WriteLine(tlFloat.Value);        // 1.5

        // Dispose is a no-op (per-thread storage lives for the program); do not read after.
        var tlDisp = new ThreadLocal<int>();
        Console.WriteLine(tlDisp.Value);         // 0
        tlDisp.Dispose();

        // The IDisposable INTERFACE mouth. ThreadLocal<T> is intrinsic — no per-class
        // emitted type-info — so `using`, an interface-typed local and isinst/castclass all
        // depend on the init prologue installing an interface-dispatch map on its
        // runtime type-info, which the direct Dispose() above does not need.
        using (var tlUsing = new ThreadLocal<int>(() => 42))
        {
            Console.WriteLine(tlUsing.Value);    // 42
        }
        var tlItf = new ThreadLocal<string>(() => "tl-itf");
        Console.WriteLine(tlItf.Value);          // tl-itf
        IDisposable tlD = tlItf;
        tlD.Dispose();
        var tlObj = new ThreadLocal<int>(() => 7);
        Console.WriteLine(tlObj.Value);          // 7
        object tlBoxed = tlObj;
        Console.WriteLine(tlBoxed is IDisposable); // True
        ((IDisposable)tlBoxed).Dispose();

        // [ThreadStatic] fields as per-thread GC roots (worker + main).
        ThreadStaticRoot.Program.__GateEntry();

        // A valueFactory returning a VALUE type — struct, IntPtr, narrow enum.
        ThreadLocalStructFactory.Program.__GateEntry();
    }
}
