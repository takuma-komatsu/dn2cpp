#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;

namespace TaskStateSubset
{
    // Task state inspection (IsCompleted / IsCompletedSuccessfully /
    // IsFaulted / IsCanceled / Status) mapped to the runtime status field, plus
    // Task.FromException<T> / FromException -> a pre-completed faulted task. Also
    // shows WhenAny completing SUCCESSFULLY when its winner faulted (the fault
    // lives on the winner, not on WhenAny's own task). All assertions are
    // deterministic.
    internal static class Program
    {
        private static async Task<int> Slow(int v) { await Task.Yield(); return v; }

        private static async Task<string> Run()
        {
            Task<int> done = Task.FromResult(42);
            bool d1 = done.IsCompleted, d2 = done.IsCompletedSuccessfully,
                 d3 = done.IsFaulted, d4 = done.IsCanceled;       // T,T,F,F

            Task<int> pend = Slow(7);
            bool p1 = pend.IsCompleted;                            // F (before await)
            int pv = await pend;
            bool p2 = pend.IsCompletedSuccessfully;                // T (after await)

            Task<int> faulted = Task.FromException<int>(new InvalidOperationException("boom"));
            bool e1 = faulted.IsFaulted, e2 = faulted.IsCompleted,
                 e3 = faulted.IsCompletedSuccessfully;             // T,T,F
            Task ngFaulted = Task.FromException(new InvalidOperationException("x"));
            bool e4 = ngFaulted.IsFaulted;                         // T

            // WhenAny with a pre-completed faulted input: that input wins (index 0,
            // already complete), WhenAny's own task succeeds, the winner is faulted.
            Task<int> wAny = await Task.WhenAny(faulted, Slow(9));
            bool w1 = wAny.IsFaulted;                              // T (winner faulted)

            return d1 + "," + d2 + "," + d3 + "," + d4 + "," + p1 + "," + p2 + "," +
                   e1 + "," + e2 + "," + e3 + "," + e4 + "," + pv + "," + w1;
        }

        // Task.Status over every state a deterministic program can pin. Each case
        // below is one real .NET reports the same way on every run; the one state
        // deliberately absent is a STARTED task still in flight (Task.Run's, or a
        // cold task between Start and completion), where real .NET itself races
        // between WaitingToRun and Running and so has no fixed answer to assert.
        private static async Task<string> Statuses()
        {
            // A cold `new Task(...)` before Start: real .NET reports Created.
            var cold = new Task<int>(() => 1);
            var s0 = cold.Status;

            // Promise tasks before completion: real .NET reports WaitingForActivation
            // for an async method's task and for a TCS's task alike.
            Task<int> pend = Slow(7);
            var s1 = pend.Status;
            var tcs = new TaskCompletionSource<int>();
            var s2 = tcs.Task.Status;

            // The three settled states.
            var s3 = Task.FromResult(42).Status;
            var s4 = Task.FromException<int>(new InvalidOperationException("boom")).Status;
            var s5 = Task.FromCanceled<int>(new CancellationToken(true)).Status;

            // The same three reached by settling a TCS, plus a completed async task.
            await pend;
            var s6 = pend.Status;
            tcs.SetResult(1);
            var s7 = tcs.Task.Status;
            var tcsC = new TaskCompletionSource<int>();
            tcsC.SetCanceled();
            var s8 = tcsC.Task.Status;
            var tcsE = new TaskCompletionSource<int>();
            tcsE.SetException(new InvalidOperationException("y"));
            var s9 = tcsE.Task.Status;

            // Start the cold task only after its Created status was read, so the
            // read above stays deterministic; join before reading the settled one.
            cold.Start();
            await cold;
            var s10 = cold.Status;

            return Fmt("cold-unstarted", s0) + Fmt("async-pending", s1) + Fmt("tcs-pending", s2)
                 + Fmt("fromresult", s3) + Fmt("fromexception", s4) + Fmt("fromcanceled", s5)
                 + Fmt("async-done", s6) + Fmt("tcs-setresult", s7) + Fmt("tcs-setcanceled", s8)
                 + Fmt("tcs-setexception", s9) + Fmt("cold-done", s10);
        }

        private static string Fmt(string label, TaskStatus s) => label + "=" + s + "(" + (int)s + ") ";

        internal static void __GateEntry()
        {
            // Run never reads a faulted task's.Result, so it completes cleanly.
            Console.WriteLine(Run().Result);
            Console.WriteLine(Statuses().Result);
        }
    }
}
