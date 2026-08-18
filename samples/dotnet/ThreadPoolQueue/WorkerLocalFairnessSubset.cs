using System;
using System.Threading;
using System.Threading.Tasks;

namespace WorkerLocalFairness;

// A pool callback may leave an async-void tail on its worker's scheduler. More tails
// than there are workers must not keep a later global item from ever getting a turn.
static class Program
{
    static int s_started;
    static int s_stop;
    static CountdownEvent? s_finished;

    static async void Loop(object? _)
    {
        Interlocked.Increment(ref s_started);
        while (Volatile.Read(ref s_stop) == 0)
            await Task.Delay(1);
        s_finished!.Signal();
    }

    internal static void __GateEntry()
    {
        int count = Environment.ProcessorCount + 2;
        s_started = 0;
        s_stop = 0;
        s_finished = new CountdownEvent(count);

        for (int i = 0; i < count; i++)
        {
            if (!ThreadPool.QueueUserWorkItem(Loop))
                throw new Exception("QueueUserWorkItem returned false");
        }

        int observed = Task.Run(() =>
        {
            while (Volatile.Read(ref s_started) != count)
                Thread.Yield();
            Volatile.Write(ref s_stop, 1);
            return Volatile.Read(ref s_started);
        }).Result;

        s_finished.Wait();
        Console.WriteLine($"worker-local-fairness: {observed == count}, {s_finished.CurrentCount}");
        s_finished.Dispose();
        s_finished = null;
    }
}
