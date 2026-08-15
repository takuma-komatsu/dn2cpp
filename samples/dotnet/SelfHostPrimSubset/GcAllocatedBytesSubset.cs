using System;
using System.Threading;

namespace GcAllocatedBytesSubset;

// GC.GetAllocatedBytesForCurrentThread / GetTotalAllocatedBytes over the
// runtime's thread_local allocation accounting. Raw byte counts differ between
// the runtimes (dn2cpp counts requested bytes, pinned included), so only the
// invariants both must satisfy are printed. Lives in this bucket because a
// section may start a Thread here: this gate is the bucket's only driver and
// has no wasm axis, where threads would throw.
internal static class Program
{
    // Static keeps: a dropped store would let the JIT elide the very allocation
    // the probe measures.
    private static byte[] s_keep;
    private static byte[] s_keepOther;
    private static long s_sink;

    // Allocation-free by construction; the dn2cpp side's quiet delta is exactly 0.
    private static long Spin()
    {
        long s = 0;
        for (int i = 0; i < 100000; i++)
            s += i * 3;
        return s;
    }

    internal static void __GateEntry()
    {
        long a0 = GC.GetAllocatedBytesForCurrentThread();
        Console.WriteLine($"alloc-nonneg={a0 >= 0}");

        s_keep = new byte[1 << 20];
        long a1 = GC.GetAllocatedBytesForCurrentThread();
        Console.WriteLine($"alloc-delta-covers={a1 - a0 >= (1 << 20)}");
        Console.WriteLine($"alloc-monotone={a1 >= a0}");

        // The decisive per-thread test: 8 MiB allocated on another thread must
        // not show up in this thread's counter. The window's own cost on real
        // .NET is the Thread object and Start/Join bookkeeping (hundreds of
        // bytes measured); < 1 MiB leaves three orders of magnitude of margin
        // while still convicting a process-wide approximation.
        long b0 = GC.GetAllocatedBytesForCurrentThread();
        var t = new Thread(() => { s_keepOther = new byte[8 << 20]; });
        t.Start();
        t.Join();
        long b1 = GC.GetAllocatedBytesForCurrentThread();
        Console.WriteLine($"alloc-thread-isolated={b1 - b0 < (1 << 20)}");

        // A quiet window: warm Spin up first so tiered-JIT churn on real .NET
        // lands outside it (measured 0 after warm-up); the threshold, not
        // zero, keeps the oracle side flake-proof.
        s_sink = Spin();
        long q0 = GC.GetAllocatedBytesForCurrentThread();
        s_sink += Spin();
        long q1 = GC.GetAllocatedBytesForCurrentThread();
        Console.WriteLine($"alloc-quiet-small={q1 - q0 < 4096}");

        // Approximate total: real .NET reads stale per-thread buffers, so a
        // fresh 1 MiB may not appear at all — assert only non-negative and
        // monotone, never "it grew".
        long t0 = GC.GetTotalAllocatedBytes(false);
        long t1 = GC.GetTotalAllocatedBytes(false);
        Console.WriteLine($"total-allocated-nonneg={t0 >= 0}");
        Console.WriteLine($"total-allocated-monotone={t1 >= t0}");

        // Precise total has no staleness carve-out: a 1 MiB allocation must be
        // covered on both runtimes.
        long p0 = GC.GetTotalAllocatedBytes(true);
        s_keep = new byte[1 << 20];
        long p1 = GC.GetTotalAllocatedBytes(true);
        Console.WriteLine($"total-precise-covers={p1 - p0 >= (1 << 20)}");
    }
}
