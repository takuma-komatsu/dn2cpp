#nullable disable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SettledCombinatorsSubset
{
    // Task.WhenAll / Task.WhenAny over inputs that are ALREADY settled: the join must be
    // complete when the combinator RETURNS, not one turn of the scheduler later. .NET
    // registers each of its continuations ExecuteSynchronously, so a settled input is
    // consumed by the registration itself.
    //
    // Every line here is READ without waiting first, and that is the whole point: a
    // `.Wait()` ahead of the read passes whether the join finished inline or was posted,
    // so it cannot tell the two apart. The mixed rows are the other half — a single
    // pending input must still leave the join PENDING, so an over-eager inline path is
    // caught too. Diffed exactly against real .NET.
    internal static class Program
    {
        private static Task Faulted()
        {
            var tcs = new TaskCompletionSource<int>();
            tcs.SetException(new InvalidOperationException("x"));
            return tcs.Task;
        }

        private static Task Canceled()
        {
            var tcs = new TaskCompletionSource<int>();
            tcs.SetCanceled();
            return tcs.Task;
        }

        private static void WhenAll()
        {
            Task done = Task.CompletedTask;
            Task done2 = Task.FromResult(7);
            Task<int> doneT = Task.FromResult(1);
            Task<int> doneT2 = Task.FromResult(2);
            Task stuck = new TaskCompletionSource<int>().Task;
            Task<int> stuckT = new TaskCompletionSource<int>().Task;

            Console.WriteLine("wa-two-done: " + Task.WhenAll(new Task[] { done, done2 }).IsCompleted);
            Console.WriteLine("wa-one-done: " + Task.WhenAll(new Task[] { done }).IsCompleted);
            Console.WriteLine("wa-empty: " + Task.WhenAll(new Task[0]).IsCompleted);
            Console.WriteLine("wa-mixed: " + Task.WhenAll(new Task[] { done, stuck }).IsCompleted);
            Console.WriteLine("wa-mixed-rev: " + Task.WhenAll(new Task[] { stuck, done }).IsCompleted);

            Task<int[]> tj = Task.WhenAll(new Task<int>[] { doneT, doneT2 });
            Console.WriteLine("wa-T-two-done: " + tj.IsCompleted + "," + tj.Result[0] + "," + tj.Result[1]);
            Console.WriteLine("wa-T-empty: " + Task.WhenAll(new Task<int>[0]).IsCompleted);
            Console.WriteLine("wa-T-mixed: " + Task.WhenAll(new Task<int>[] { doneT, stuckT }).IsCompleted);

            Console.WriteLine("wa-enum: " + Task.WhenAll((IEnumerable<Task>)new List<Task> { done, done2 }).IsCompleted);
            Console.WriteLine("wa-T-enum: " + Task.WhenAll((IEnumerable<Task<int>>)new List<Task<int>> { doneT, doneT2 }).IsCompleted);

            // A settled input that did not succeed settles the join the same way, and the
            // outcome it carries is the input's, not "completed".
            Task f = Task.WhenAll(new Task[] { done, Faulted() });
            Console.WriteLine("wa-faulted: " + f.IsCompleted + "," + f.IsFaulted + ","
                + f.Exception.InnerException.GetType().Name);
            Task c = Task.WhenAll(new Task[] { done, Canceled() });
            Console.WriteLine("wa-canceled: " + c.IsCompleted + "," + c.IsCanceled + "," + c.IsFaulted
                + "," + (c.Exception is null));
            // A fault anywhere outranks a cancellation anywhere, in either order.
            Console.WriteLine("wa-cancel-then-fault: "
                + Task.WhenAll(new Task[] { Canceled(), Faulted() }).IsFaulted);
            Console.WriteLine("wa-fault-then-cancel: "
                + Task.WhenAll(new Task[] { Faulted(), Canceled() }).IsFaulted);
            // The canceled join re-raises like a canceled task, wrapped by the blocking
            // wait and bare through the awaiter.
            try { Task.WhenAll(new Task[] { done, Canceled() }).Wait(); Console.WriteLine("wa-canceled-wait: no-throw"); }
            catch (AggregateException ae) { Console.WriteLine("wa-canceled-wait: AggregateException|" + ae.InnerException.GetType().Name); }
            try { Task.WhenAll(new Task[] { done, Canceled() }).GetAwaiter().GetResult(); Console.WriteLine("wa-canceled-getresult: no-throw"); }
            catch (Exception ex) { Console.WriteLine("wa-canceled-getresult: " + ex.GetType().Name); }

            // An inline join is itself a settled input to the next combinator.
            Console.WriteLine("wa-of-wa: "
                + Task.WhenAll(new Task[] { Task.WhenAll(new Task[] { done, done2 }) }).IsCompleted);
        }

        private static void WhenAny()
        {
            Task done = Task.CompletedTask;
            Task done2 = Task.FromResult(7);
            Task<int> doneT = Task.FromResult(1);
            Task<int> doneT2 = Task.FromResult(2);
            Task stuck = new TaskCompletionSource<int>().Task;
            Task<int> stuckT = new TaskCompletionSource<int>().Task;

            // The winner is the first settled input in ARRAY order, which is only
            // deterministic because nothing but the registration decides it.
            Task<Task> a = Task.WhenAny(new Task[] { done, done2 });
            Console.WriteLine("wy-two-done: " + a.IsCompleted + "," + (a.Result == done));
            Task<Task> b = Task.WhenAny(new Task[] { stuck, done });
            Console.WriteLine("wy-stuck-then-done: " + b.IsCompleted + "," + (b.Result == done));
            Console.WriteLine("wy-done-then-stuck: " + Task.WhenAny(new Task[] { done, stuck }).IsCompleted);
            Console.WriteLine("wy-two-stuck: " + Task.WhenAny(new Task[] { stuck, stuckT }).IsCompleted);

            Task<Task<int>> t = Task.WhenAny(new Task<int>[] { doneT, doneT2 });
            Console.WriteLine("wy-T-two-done: " + t.IsCompleted + "," + t.Result.Result);
            Console.WriteLine("wy-enum: "
                + Task.WhenAny((IEnumerable<Task>)new List<Task> { done, done2 }).IsCompleted);

            // WhenAny never faults: a faulted winner leaves the join SUCCEEDED and the
            // fault observable only through the winner itself.
            Task<Task> g = Task.WhenAny(new Task[] { Faulted() });
            Console.WriteLine("wy-faulted: " + g.IsCompleted + "," + g.IsFaulted + "," + g.Result.IsFaulted);

            Console.WriteLine("wy-of-wa: "
                + Task.WhenAny(new Task[] { Task.WhenAll(new Task[] { done, done2 }) }).IsCompleted);
        }

        internal static void __GateEntry()
        {
            WhenAll();
            WhenAny();
        }

        private enum PendingCode : ushort { Low = 1, High = 65535 }

        private struct PendingPair
        {
            internal int Number;
            internal string Text;
            public override string ToString() => Number + "/" + Text;
        }

        private static void PendingResults<T>(string label, T first, T second)
        {
            var a = new TaskCompletionSource<T>();
            var b = new TaskCompletionSource<T>();
            var all = Task.WhenAll(new Task<T>[] { a.Task, b.Task });
            bool before = all.IsCompleted;
            a.SetResult(first);
            bool partial = all.IsCompleted;
            b.SetResult(second);
            bool final = all.IsCompleted;
            T[] values = all.Result;
            Console.WriteLine("pending " + label + ": " + before + "," + partial + "," + final
                + "|" + values.Length + "|" + values[0] + "," + values[1]);
        }

        private static void PendingNonGeneric()
        {
            var a = new TaskCompletionSource<int>();
            var b = new TaskCompletionSource<int>();
            var all = Task.WhenAll((IEnumerable<Task>)new List<Task>
                { a.Task, Task.CompletedTask, b.Task, a.Task });
            var any = Task.WhenAny((IEnumerable<Task>)new List<Task> { a.Task, b.Task });
            Console.WriteLine("pending nongeneric before: " + all.IsCompleted + "," + any.IsCompleted);
            a.SetResult(1);
            Console.WriteLine("pending nongeneric partial: " + all.IsCompleted + "," + any.IsCompleted);
            Task winner = any.Result;
            b.SetResult(2);
            Console.WriteLine("pending nongeneric final: " + all.IsCompleted + "," + any.IsCompleted
                + "|" + ReferenceEquals(winner, a.Task) + "," + ReferenceEquals(any.Result, winner));
        }

        private static void PendingOutcomes()
        {
            var fault = new TaskCompletionSource<int>();
            var cancel = new TaskCompletionSource<int>();
            var all = Task.WhenAll(fault.Task, cancel.Task);
            var any = Task.WhenAny(fault.Task, cancel.Task);
            fault.SetException(new InvalidOperationException("pending fault"));
            Console.WriteLine("pending fault partial: " + all.IsCompleted + "," + any.IsCompleted
                + "|" + any.IsFaulted + "," + ReferenceEquals(any.Result, fault.Task));
            cancel.SetCanceled();
            Console.WriteLine("pending fault final: " + all.IsCompleted + "," + all.IsFaulted
                + "," + all.IsCanceled + "|" + all.Exception.InnerExceptions.Count
                + "," + all.Exception.InnerException.Message);

            var success = new TaskCompletionSource<int>();
            var canceled = new TaskCompletionSource<int>();
            var canceledAll = Task.WhenAll(success.Task, canceled.Task);
            var canceledAny = Task.WhenAny(canceled.Task, success.Task);
            canceled.SetCanceled();
            Console.WriteLine("pending cancel partial: " + canceledAll.IsCompleted + "," + canceledAny.IsCompleted
                + "|" + canceledAny.IsCanceled + "," + canceledAny.Result.IsCanceled);
            success.SetResult(3);
            Console.WriteLine("pending cancel final: " + canceledAll.IsCompleted + "," + canceledAll.IsCanceled
                + "," + canceledAll.IsFaulted + "|" + (canceledAll.Exception is null));
        }

        private static void PendingNested()
        {
            var source = new TaskCompletionSource<int>();
            Task chain = source.Task;
            for (int i = 0; i < 1024; i++)
                chain = Task.WhenAll(new Task[] { chain, Task.CompletedTask });
            var any = Task.WhenAny(new Task[] { chain, new TaskCompletionSource<int>().Task });
            var outer = Task.WhenAll(new Task[] { chain, any });
            source.SetResult(17);
            Console.WriteLine("pending nested: " + chain.IsCompleted + "," + any.IsCompleted
                + "," + outer.IsCompleted + "|" + ReferenceEquals(any.Result, chain));
        }

        private static void PendingWorker()
        {
            var source = new TaskCompletionSource<int>();
            var all = Task.WhenAll(source.Task, Task.FromResult(5));
            var any = Task.WhenAny(source.Task, new TaskCompletionSource<int>().Task);
            bool allAtReturn = false;
            bool anyAtReturn = false;
            var worker = new Thread(() =>
            {
                source.SetResult(4);
                allAtReturn = all.IsCompleted;
                anyAtReturn = any.IsCompleted;
            });
            worker.Start();
            worker.Join();
            Console.WriteLine("pending worker: " + allAtReturn + "," + anyAtReturn
                + "|" + all.Result[0] + "," + all.Result[1] + "," + ReferenceEquals(any.Result, source.Task));
        }

        private static void PendingRaces()
        {
            int allCompleted = 0;
            int anyCompleted = 0;
            int validResults = 0;
            for (int i = 0; i < 32; i++)
            {
                var a = new TaskCompletionSource<int>();
                var b = new TaskCompletionSource<int>();
                using var start = new ManualResetEventSlim(false);
                var first = new Thread(() => { start.Wait(); a.SetResult(7); });
                var second = new Thread(() => { start.Wait(); b.SetResult(9); });
                first.Start();
                second.Start();
                if ((i & 1) != 0)
                    start.Set();
                // Alternate concurrent pending callbacks with registration/completion races.
                var all = Task.WhenAll(a.Task, b.Task);
                var any = Task.WhenAny(a.Task, b.Task);
                if ((i & 1) == 0)
                    start.Set();
                first.Join();
                second.Join();
                if (all.IsCompleted)
                    allCompleted++;
                if (any.IsCompleted)
                    anyCompleted++;
                int[] values = all.Result;
                Task<int> winner = any.Result;
                if (values[0] == 7 && values[1] == 9
                    && (ReferenceEquals(winner, a.Task) || ReferenceEquals(winner, b.Task))
                    && ReferenceEquals(any.Result, winner))
                    validResults++;
            }
            Console.WriteLine("pending races: " + allCompleted + "," + anyCompleted + "," + validResults);
        }

        private static void OrdinaryQueuedAwait()
        {
            var source = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            var callback = new TaskCompletionSource<bool>();
            int producer = Thread.CurrentThread.ManagedThreadId;
            bool setting = false;
            source.Task.GetAwaiter().OnCompleted(() => callback.SetResult(
                Thread.CurrentThread.ManagedThreadId == producer && setting));
            setting = true;
            source.SetResult(1);
            setting = false;
            Console.WriteLine("ordinary await inline: " + callback.Task.Result);
        }

        internal static void RunPending()
        {
            Console.WriteLine("== pending task joins ==");
            PendingResults("int", 7, 9);
            PendingResults("long", 4294967296L, -4294967297L);
            PendingResults("string", "left", "right");
            PendingResults<object>("object", 11, "value");
            PendingResults("enum", PendingCode.Low, PendingCode.High);
            PendingResults("struct", new PendingPair { Number = 11, Text = "left" },
                new PendingPair { Number = 12, Text = "right" });
            PendingNonGeneric();
            PendingOutcomes();
            PendingNested();
            PendingWorker();
            PendingRaces();
            OrdinaryQueuedAwait();
            Console.WriteLine("pending task joins end");
        }
    }
}
