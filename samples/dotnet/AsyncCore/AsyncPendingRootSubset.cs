#nullable enable
using System;
using System.Threading.Tasks;

// A suspended async state machine must survive a garbage collection while its ONLY
// reference is per-thread scheduler state: a queued resumption after `await Task.Yield()`
// or a pending Task.Delay timer entry. Several methods suspend, an interleaved collector
// method forces full collections between its own turns (after deep recursion scrubbed
// stale stack slots), and then everything is awaited and deterministic totals printed.
// Single-threaded cooperative scheduling keeps every counter exact.
namespace AsyncPendingRoot;

static class Program
{
    static int s_turns;
    static object? s_occupy; // keeps the post-collect allocation storm reachable

    // Small object with reference fields: it lands in the same (small, pointer-
    // containing) allocation classes as continuations/boxed state machines/timer
    // entries, so the storm below actually recycles any block a collection freed
    // by mistake.
    sealed class Blob
    {
        public object? A;
        public object? B;
    }

    static async Task<int> Spin(int id, int rounds)
    {
        int local = 0;
        for (int r = 0; r < rounds; r++)
        {
            await Task.Yield();
            local += id;
            s_turns++;
        }
        return local;
    }

    static async Task<long> YieldingCollector(int rounds)
    {
        long sink = 0;
        for (int r = 0; r < rounds; r++)
        {
            await Task.Yield();
            long scrub = Stomp(96); // overwrite stale stack slots left by other turns
            GC.Collect();
            // Allocation storm: reuse (and overwrite) any small block the collection
            // freed by mistake before the suspended turns resume.
            for (int j = 0; j < 2048; j++)
            {
                var b = new Blob();
                b.A = s_occupy;
                b.B = scrub;
                s_occupy = b;
            }
            sink += r + 1;
        }
        return sink;
    }

    static async Task<int> Delayed(int id)
    {
        await Task.Delay(5 * id + 5);
        return id * 100;
    }

    static long Stomp(int depth)
    {
        if (depth <= 0)
            return 1;
        long a = depth;
        long b = depth * 2;
        long c = depth * 3;
        long d = depth * 4;
        return a + b + c + d + Stomp(depth - 1);
    }

    internal static void __GateEntry()
    {
        // Run-queue roots: eight spinners sit suspended on Task.Yield while the
        // collector (interleaved on the same run queue) forces collections.
        var spinners = new Task<int>[8];
        for (int i = 0; i < spinners.Length; i++)
            spinners[i] = Spin(i + 1, 24);
        long sink = Stomp(128) + YieldingCollector(24).Result;
        int total = 0;
        foreach (var t in spinners)
            total += t.Result;
        Console.WriteLine("spin total=" + total);       // 24 * (1+..+8) = 864
        Console.WriteLine("spin turns=" + s_turns);     // 8 * 24 = 192
        Console.WriteLine("spin sink=" + (sink > 0));

        // Timer roots: six pending Task.Delay entries (and the state machines awaiting
        // them) survive collections forced before any timer is due.
        var delayed = new Task<int>[6];
        for (int i = 0; i < delayed.Length; i++)
            delayed[i] = Delayed(i + 1);
        long sink2 = Stomp(128) + YieldingCollector(4).Result;
        int dtotal = 0;
        foreach (var t in delayed)
            dtotal += t.Result;
        Console.WriteLine("delay total=" + dtotal);     // 100 * (1+..+6) = 2100
        Console.WriteLine("delay sink=" + (sink2 > 0));
    }
}
