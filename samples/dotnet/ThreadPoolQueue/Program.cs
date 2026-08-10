using System;
using System.Globalization;
using System.Threading;

// ThreadPool.QueueUserWorkItem. Every queued item signals a CountdownEvent the main thread
// waits on before reading a result, which is what makes fire-and-forget deterministic. The
// boxed state of the 2-arg overload is reachable ONLY through the queued item and the
// queuing loop provokes a collection, so this also pins that the runtime keeps the queued
// delegate and its state rooted until a worker runs them.
static class Program
{
    static int s_count;     // bumped by the 1-arg (null-state) callbacks
    static int s_stateSum;  // sum of the int states threaded through the 2-arg overload
    static int s_sink;      // keeps the throwaway-garbage allocations from being elided

    const int N = 128;      // 1-arg items
    const int M = 256;      // 2-arg (state-carrying) items; sum 1..256 == 32896

    static void Main()
    {
        // Pin both cultures first: gate output must not depend on the host locale (see AGENTS.md).
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

        var done = new CountdownEvent(N + M);

        // 1-arg overload (null state). QueueUserWorkItem always returns true.
        for (int i = 0; i < N; i++)
        {
            bool queued = ThreadPool.QueueUserWorkItem(_ =>
            {
                Interlocked.Increment(ref s_count);
                done.Signal();
            });
            if (!queued)
                throw new Exception("QueueUserWorkItem returned false");
        }

        // 2-arg overload: each box is reachable only via the queued item, so it must stay
        // rooted until the worker reads it; the throwaway arrays provoke a GC mid-queue.
        for (int i = 1; i <= M; i++)
        {
            ThreadPool.QueueUserWorkItem(o =>
            {
                Interlocked.Add(ref s_stateSum, (int)o!);
                done.Signal();
            }, i);

            int[] junk = new int[1024];
            junk[i & 1023] = i;
            s_sink ^= junk[i & 1023];
        }

        done.Wait();

        Console.WriteLine(s_count);              // 128
        Console.WriteLine(s_stateSum);           // 32896
        Console.WriteLine(s_count + s_stateSum); // 33024
        Console.WriteLine(s_sink);               // XOR of 1..256 (deterministic)

        PoolQueueRoot.Program.__GateEntry();
        UserThreadTask.Program.__GateEntry();
        ThriveExecutor.Program.__GateEntry();
        UserThreadDelay.Program.__GateEntry();
        FfSettler.Program.__GateEntry();
        TimerSettler.Program.__GateEntry();
        FastSettler.Program.__GateEntry();
    }
}
