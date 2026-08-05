#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

// A hand-rolled thread pool: user threads draining a user-owned queue of COLD tasks with
// RunSynchronously(), while the submitter blocks in Wait() / .Result / GetResult(). Nothing
// on this path goes through Task.Run or Task.Start, so the in-flight counter a blocked
// drain consults stays at zero — a live user thread has to be a settling principal in its
// own right, or the wait aborts as a deadlock. The genuine-deadlock counterpart cannot live
// in a diff gate (real .NET hangs on it); it is the async-core gate's frozen
// ColdTaskDeadlock section.
namespace UserThreadTask;

static class Program
{
    // Deliberately NOT ThreadPool/Task.Run: the point is a queue and a set of threads the
    // runtime has no hook on, handed cold tasks each worker runs inline.
    sealed class Executor
    {
        private readonly Queue<Task> _queue = new Queue<Task>();
        private readonly object _lock = new object();
        private readonly Thread[] _workers;
        private bool _stop; // guarded by _lock

        public Executor(int workers)
        {
            _workers = new Thread[workers];
            for (int i = 0; i < workers; i++)
            {
                var w = new Thread(Loop);
                w.IsBackground = true;
                _workers[i] = w;
                w.Start();
            }
        }

        public void Submit(Task t)
        {
            lock (_lock)
                _queue.Enqueue(t);
        }

        // Drain-then-stop: a worker leaves only when the queue is empty AND stop is set,
        // so no submitted task is dropped.
        public void Stop()
        {
            lock (_lock)
                _stop = true;
            foreach (Thread w in _workers)
                w.Join();
        }

        private void Loop()
        {
            while (true)
            {
                Task? t = null;
                bool stop;
                lock (_lock)
                {
                    stop = _stop;
                    if (_queue.Count > 0)
                        t = _queue.Dequeue();
                }
                if (t is null)
                {
                    if (stop)
                        return;
                    Thread.Sleep(1);
                    continue;
                }
                // A faulting body settles the task FAULTED and does not throw here; the
                // blocked submitter observes it.
                t.RunSynchronously();
            }
        }
    }

    static int Triangle(int n)
    {
        int s = 0;
        for (int i = 1; i <= n; i++)
            s += i;
        return s;
    }

    public static void __GateEntry()
    {
        var ex = new Executor(2);

        // 1. A cold void Task handed to a worker, the submitter blocking in Wait(). It
        //    reaches Wait() long before a polling worker picks the task up, which is the
        //    window the deadlock verdict has to survive.
        int sideEffect = 0;
        var t1 = new Task(() => sideEffect = Triangle(100));
        ex.Submit(t1);
        t1.Wait();
        Console.WriteLine("cold void via user thread: " + sideEffect + " " + t1.IsCompleted);

        // 2. Task<T> waited through .Result, and through Wait() then .Result.
        var t2 = new Task<int>(() => 6 * 7);
        ex.Submit(t2);
        Console.WriteLine("cold result via user thread: " + t2.Result);

        var t3 = new Task<string>(() => "cold-" + Triangle(10));
        ex.Submit(t3);
        t3.Wait();
        Console.WriteLine("cold wait+result via user thread: " + t3.Result);

        // 3. GetAwaiter().GetResult() over the same hand-off, on Task<T> and on void Task.
        var t4 = new Task<long>(() => 1L << 40);
        ex.Submit(t4);
        Console.WriteLine("cold getresult via user thread: " + t4.GetAwaiter().GetResult());

        int flag = 0;
        var t5 = new Task(() => flag = 7);
        ex.Submit(t5);
        t5.GetAwaiter().GetResult();
        Console.WriteLine("cold void getresult via user thread: " + flag);

        // 4. A faulting cold task on a user thread: the fault settles on the worker
        //    (RunSynchronously never throws at the runner) and surfaces at the blocked
        //    submitter's Wait(), wrapped in an AggregateException on both sides. What this
        //    asserts is the boundary crossing, hence the unwrap to root; the wrapper's own
        //    shape is BlockingWaitWrapSubset's assert (AsyncCombinators).
        var t6 = new Task(() => throw new InvalidOperationException("boom-on-worker"));
        ex.Submit(t6);
        try
        {
            t6.Wait();
            Console.WriteLine("cold fault via user thread: NOT THROWN");
        }
        catch (Exception e)
        {
            Exception root = e is AggregateException agg ? agg.InnerException! : e;
            Console.WriteLine("cold fault via user thread: " + root.Message + " " + t6.IsFaulted);
        }

        // 5. The default-scheduler cold path (Start()) and the user-thread path in one
        //    wait sequence: both principals in flight at once.
        var poolTask = new Task<int>(() => Triangle(20));
        var userTask = new Task<int>(() => Triangle(30));
        ex.Submit(userTask);
        poolTask.Start();
        Console.WriteLine("mixed start/usersubmit: " + poolTask.Result + " " + userTask.Result);

        // 6. A batch over the two workers, waited with WaitAny then WaitAll. The winning
        //    index is nondeterministic, so only its settled-ness is printed.
        var batch = new Task[8];
        int[] results = new int[8];
        for (int i = 0; i < batch.Length; i++)
        {
            int k = i;
            batch[i] = new Task(() => results[k] = (k + 1) * (k + 1));
            ex.Submit(batch[i]);
        }
        int winner = Task.WaitAny(batch);
        Console.WriteLine("waitany winner settled: " + batch[winner].IsCompleted);
        Task.WaitAll(batch);
        int total = 0;
        foreach (int r in results)
            total += r;
        Console.WriteLine("waitall total: " + total); // 1+4+9+...+64 == 204

        // 6b. WaitAll waits for EVERY input before raising and reports each failure in one
        //     AggregateException, so two faulting members must still leave every member
        //     settled and surface both messages. InnerExceptions is read through
        //     IReadOnlyList<Exception> for the reason AggDump states.
        var failing = new Task[3];
        int okRan = 0;
        failing[0] = new Task(() => throw new InvalidOperationException("wa-boom-0"));
        failing[1] = new Task(() => okRan = 1);
        failing[2] = new Task(() => throw new InvalidOperationException("wa-boom-2"));
        foreach (Task f in failing)
            ex.Submit(f);
        try
        {
            Task.WaitAll(failing);
            Console.WriteLine("waitall fault: NOT THROWN");
        }
        catch (AggregateException ae)
        {
            IReadOnlyList<Exception> inner = ae.InnerExceptions;
            string joined = "";
            for (int i = 0; i < inner.Count; i++)
                joined += (i == 0 ? "" : "|") + inner[i].Message;
            Console.WriteLine("waitall fault: count=" + inner.Count + " " + joined
                + " ok=" + okRan + " settled=" + (failing[0].IsFaulted && failing[1].IsCompleted
                    && failing[2].IsFaulted));
        }

        // 7. The same through a promise instead of a cold task: a TaskCompletionSource
        //    completed by a user thread, with no cold work and no pool item at any point.
        var tcs = new TaskCompletionSource<int>();
        var setter = new Thread(() =>
        {
            Thread.Sleep(5);
            tcs.SetResult(1234);
        });
        setter.Start();
        Console.WriteLine("tcs from user thread: " + tcs.Task.Result);
        setter.Join();

        // 8. Nested: a worker thread is the blocked waiter, and the principal that can
        //    settle its task is the OTHER worker.
        var outer = new Task<int>(() =>
        {
            var inner = new Task<int>(() => Triangle(15));
            ex.Submit(inner);
            return inner.Result + 1;
        });
        ex.Submit(outer);
        Console.WriteLine("nested user-thread wait: " + outer.Result); // 120 + 1

        ex.Stop();
        Console.WriteLine("executor stopped");
    }
}
