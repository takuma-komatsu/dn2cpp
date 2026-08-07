#nullable disable
using System;
using System.Collections.Generic;
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
    }
}
