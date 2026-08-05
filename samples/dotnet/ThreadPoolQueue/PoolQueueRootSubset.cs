#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;

// Queued work must stay GC-reachable while it waits for a worker: for a fire-and-forget
// Task.Run nothing else references the task, the delegate or its captured state. Every
// pool worker is saturated with a blocker so the work sits queued across forced
// collections, and only then is the pool released and every item asserted to have run.
namespace PoolQueueRoot;

static class Program
{
    static int s_ran;
    static int s_sum;
    static object? s_occupy; // keeps the post-collect allocation storm reachable

    // Lands in the same small pointer-containing allocation class as the delegates,
    // tasks and display classes, so the storm below actually recycles a freed block.
    sealed class Blob
    {
        public object? A;
        public object? B;
    }

    // Overwrites the stack region the queuing loop used, so conservative stack scanning
    // cannot keep the queued items alive by accident through a stale slot.
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
        int workers = Environment.ProcessorCount;
        if (workers < 1)
            workers = 1;
        var release = new SemaphoreSlim(0);
        var blockersReady = new CountdownEvent(workers);
        var blockers = new Task[workers];
        for (int i = 0; i < workers; i++)
        {
            blockers[i] = Task.Run(() =>
            {
                blockersReady.Signal();
                release.Wait();
            });
        }
        blockersReady.Wait(); // every pool worker is now parked in a blocker

        const int N = 200;
        var done = new CountdownEvent(N);
        for (int i = 1; i <= N; i++)
        {
            int k = i;
            _ = Task.Run(() => // fire-and-forget: the returned Task is deliberately dropped
            {
                Interlocked.Increment(ref s_ran);
                Interlocked.Add(ref s_sum, k);
                done.Signal();
            });
        }

        long sink = Stomp(128);
        for (int i = 0; i < 4; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            // Overwrite any small block the collection freed by mistake, before a
            // worker gets to consume it.
            for (int j = 0; j < 8192; j++)
            {
                var b = new Blob();
                b.A = s_occupy;
                b.B = done;
                s_occupy = b;
            }
            sink += i + 1;
        }

        release.Release(workers);
        done.Wait();
        foreach (var b in blockers)
            b.Wait();

        Console.WriteLine("ff ran=" + s_ran);   // 200
        Console.WriteLine("ff sum=" + s_sum);   // 20100
        Console.WriteLine("ff sink=" + sink);   // deterministic
    }
}
