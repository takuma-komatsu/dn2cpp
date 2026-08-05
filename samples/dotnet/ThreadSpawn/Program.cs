using System;
using System.Threading;

// Real OS threads exercising the synchronization primitives under genuine parallelism.
// Every result is read only AFTER joining, so the totals are deterministic — a racy
// implementation would not reproduce N*K reliably.
static class Program
{
    static int s_counter;        // Interlocked.Increment across threads
    static long s_lockSum;       // lock-protected accumulate across threads
    static readonly object s_lock = new object();
    static int s_flag;           // Volatile handoff

    const int N = 8;
    const int K = 10000;

    static void Worker()
    {
        for (int i = 0; i < K; i++)
            Interlocked.Increment(ref s_counter);
        for (int i = 0; i < K; i++)
            lock (s_lock) { s_lockSum += 2; }
    }

    // Cross-thread sub-word atomics. Sentinel lives in the byte next to the
    // Exchange-hammered Hot, so an atomic wider than the declared width stomps it.
    struct HotPack { public byte Hot; public byte Sentinel; }
    static byte s_byteCounter;
    static HotPack s_pack;

    static void ByteWorker()
    {
        for (int i = 0; i < 50; i++)
        {
            while (true)
            {
                byte cur = Volatile.Read(ref s_byteCounter);
                byte next = (byte)(cur + 1);
                if (Interlocked.CompareExchange(ref s_byteCounter, next, cur) == cur)
                    break;
            }
            Interlocked.Exchange(ref s_pack.Hot, (byte)(i + 1));
        }
    }

    // An explicit static constructor (not beforefieldinit) so every static access is
    // guarded, doing enough work that a broken guard's init window is wide. Inits must
    // read 1, and Sum must never be observed half-initialized by a racing reader.
    static class LazyInit
    {
        public static int Inits;
        public static readonly long Sum;

        static LazyInit()
        {
            var table = new long[4096];
            for (int i = 0; i < table.Length; i++)
                table[i] = i * 3L;
            long s = 0;
            for (int i = 0; i < table.Length; i++)
                s += table[i];
            Sum = s; // 3 * 4095 * 4096 / 2 = 25159680
            Inits++;
        }
    }

    static void Main()
    {
        var threads = new Thread[N];
        for (int i = 0; i < N; i++)
            threads[i] = new Thread(Worker);
        for (int i = 0; i < N; i++)
            threads[i].Start();
        for (int i = 0; i < N; i++)
            threads[i].Join();

        Console.WriteLine(s_counter);                              // 80000
        Console.WriteLine(s_lockSum);                              // 160000
        Console.WriteLine(Thread.CurrentThread.ManagedThreadId > 0); // True (main)

        // ParameterizedThreadStart: the boxed argument flows to the worker.
        int captured = 0;
        var pt = new Thread(o => { captured = (int)o!; });
        pt.Start(42);
        pt.Join();
        Console.WriteLine(captured);                               // 42

        // Volatile handoff: the producer writes data then sets the flag, and Join
        // establishes the happens-before the consumer reads under.
        int shared = 0;
        var producer = new Thread(() => { shared = 7; Volatile.Write(ref s_flag, 1); });
        producer.Start();
        producer.Join();
        Console.WriteLine(Volatile.Read(ref s_flag) == 1 ? shared : -1); // 7

        var w = new Thread(() => Thread.Sleep(1));
        Console.WriteLine(w.IsAlive);                              // False (not started)
        w.Start();
        w.Join();
        Console.WriteLine(w.IsAlive);                              // False (finished)

        // 4 threads x 50 CAS-increments on a shared byte: exactly 200, no wrap, and the
        // adjacent sentinel byte keeps its initial value.
        s_pack.Sentinel = 0x5A;
        var byteThreads = new Thread[4];
        for (int i = 0; i < 4; i++)
            byteThreads[i] = new Thread(ByteWorker);
        for (int i = 0; i < 4; i++)
            byteThreads[i].Start();
        for (int i = 0; i < 4; i++)
            byteThreads[i].Join();
        Console.WriteLine(s_byteCounter);                          // 200
        Console.WriteLine(s_pack.Hot);                             // 50 (every worker's last write)
        Console.WriteLine(s_pack.Sentinel);                        // 90 (0x5A intact)

        // N threads race the FIRST USE of a lazily-initialized type: the cctor must run
        // exactly once and every racing reader must see fully-initialized statics. A guard
        // without a happens-before edge, or one two threads can enter, shows a 0 sum or
        // Inits > 1.
        var initSeen = new long[N];
        var initThreads = new Thread[N];
        for (int i = 0; i < N; i++)
        {
            int idx = i;
            initThreads[i] = new Thread(() => { initSeen[idx] = LazyInit.Sum; });
        }
        for (int i = 0; i < N; i++)
            initThreads[i].Start();
        for (int i = 0; i < N; i++)
            initThreads[i].Join();
        long initTotal = 0;
        for (int i = 0; i < N; i++)
            initTotal += initSeen[i];
        Console.WriteLine(LazyInit.Inits);                         // 1 (cctor ran exactly once)
        Console.WriteLine(initTotal);                              // 201277440 (N * 25159680)
    }
}
