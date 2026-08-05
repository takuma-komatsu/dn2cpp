#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

// Task.Delay is settled by NEITHER a pool worker nor a user thread but by its own thread's
// virtual clock, which advances only when that thread pumps or blocks — so a blocking
// wait's own clock-advance (dn2cpp_task_drain's advance_timers) is load-bearing. Both
// orders are here because they exercise opposite halves: delays owned by the WAITING
// thread, and delays owned by a user thread the waiter must sleep behind. The per-frame
// driver is where a clock that advances once and then stops shows up as a stall.
namespace UserThreadDelay;

static class Program
{
    // Workers that never exit: a task handed over is run inline on one of them.
    sealed class Executor
    {
        private readonly Queue<Task> queue = new Queue<Task>();
        private readonly object gate = new object();
        private readonly List<Thread> threads = new List<Thread>();
        private bool stop;

        public Executor(int n)
        {
            for (int i = 0; i < n; i++)
            {
                var t = new Thread(Loop);
                t.IsBackground = true;
                threads.Add(t);
                t.Start();
            }
        }

        public void Submit(Task t)
        {
            lock (gate)
                queue.Enqueue(t);
        }

        public void Stop()
        {
            lock (gate)
                stop = true;
            foreach (var t in threads)
                t.Join();
        }

        private void Loop()
        {
            while (true)
            {
                Task? t = null;
                bool quit;
                lock (gate)
                {
                    quit = stop;
                    if (queue.Count > 0)
                        t = queue.Dequeue();
                }
                if (t is null)
                {
                    if (quit)
                        return;
                    Thread.Sleep(1);
                    continue;
                }
                t.RunSynchronously();
            }
        }
    }

    // Whichever thread starts this owns the delays, so a thread that later blocks on the
    // returned task must advance that clock, or sleep while the owner does.
    static async Task<int> DelayChain(int steps, int ms)
    {
        int acc = 0;
        for (int i = 1; i <= steps; i++)
        {
            await Task.Delay(ms);
            acc += i;
        }

        return acc;
    }

    // One task's completion depending on a user thread AND on a virtual clock.
    static async Task<int> DelayThenHandOff(Executor ex, int ms)
    {
        await Task.Delay(ms);
        var handed = new Task<int>(() => 7);
        ex.Submit(handed);
        int v = await handed;
        await Task.Delay(ms);
        return v + 1;
    }

    public static void __GateEntry()
    {
        var ex = new Executor(2);

        // 1. Delays owned by the WAITING thread: the blocking drain's own clock-advance is
        //    the only thing that can make this finish.
        Task<int> own = DelayChain(5, 10);
        Console.WriteLine("delay own-thread: " + own.Result); // 15

        // 2. Delays owned by a USER thread: the waiter's own clock is irrelevant, it must
        //    sleep and be woken.
        int fromWorker = 0;
        var tcs = new TaskCompletionSource<int>();
        var runner = new Thread(() =>
        {
            fromWorker = DelayChain(4, 10).Result; // 10
            tcs.SetResult(fromWorker);
        });
        runner.Start();
        Console.WriteLine("delay user-thread: " + tcs.Task.Result);
        runner.Join();

        // 3. The delays belong to the executor worker, the wait to main.
        var cold = new Task<int>(() => DelayChain(3, 10).Result); // 6
        ex.Submit(cold);
        Console.WriteLine("delay cold-on-worker: " + cold.Result);

        // 4. A delay, a hand-off to a worker, another delay: two principals plus a clock.
        Console.WriteLine("delay handoff: " + DelayThenHandOff(ex, 10).Result); // 8

        // 5. The per-frame driver: a fresh delay-driven chain started and blocked on each
        //    frame, so a clock that advances once and then stops shows up as a stall.
        int total = 0;
        for (int frame = 0; frame < 40; frame++)
        {
            total += DelayChain(2, 5).Result; // 3 per frame
            if ((frame & 7) == 0)
            {
                var handed = new Task<int>(() => 1);
                ex.Submit(handed);
                total += handed.Result;
            }
        }

        Console.WriteLine("delay frames: " + total); // 40*3 + 5 == 125

        ex.Stop();
        Console.WriteLine("delay executor stopped");
    }
}
