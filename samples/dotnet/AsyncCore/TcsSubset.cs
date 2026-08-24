#nullable enable
using System;
using System.Threading.Tasks;

namespace TcsSubset
{
    // TaskCompletionSource / TaskCompletionSource<T>: a manually-completed task.
    // The TCS is a source wrapper over one stable pending task; SetResult /
    // SetException / SetCanceled are exactly-once transitions (Set* throws
    // InvalidOperationException once settled, TrySet* returns false), results
    // pack like async Task<T> results (double bit-cast, reference carried), and
    // a cross-thread completion (Task.Run) wakes a blocked .Result. All
    // assertions are deterministic: every read happens after the completing
    // call (or after the completing task is awaited).
    internal static class Program
    {
        private static async Task<string> AwaitFaulted(TaskCompletionSource<int> tcs)
        {
            try
            {
                return "value " + await tcs.Task;
            }
            catch (InvalidOperationException ex)
            {
                return "caught " + ex.Message;
            }
        }

        internal static void __GateEntry()
        {
            // Generic TCS: SetResult before the read; the task is the same object
            // across get_Task calls, so state reads see the transition.
            var tcs = new TaskCompletionSource<int>();
            Console.WriteLine("pending: " + tcs.Task.IsCompleted);
            tcs.SetResult(5);
            Console.WriteLine("result: " + tcs.Task.Result + "," + tcs.Task.IsCompletedSuccessfully);

            // Exactly-once: a second transition Try-fails / throws.
            Console.WriteLine("again: " + tcs.TrySetResult(6));
            try { tcs.SetResult(7); }
            catch (InvalidOperationException) { Console.WriteLine("settled: InvalidOperationException"); }

            // Non-generic (void) TCS + TaskCreationOptions hint ctor.
            var vtcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            vtcs.SetResult();
            vtcs.Task.Wait();
            Console.WriteLine("void: " + vtcs.Task.IsCompletedSuccessfully);

            // Double result packs through the 8-byte slot (bit-cast).
            var dtcs = new TaskCompletionSource<double>();
            Console.WriteLine("trydouble: " + dtcs.TrySetResult(2.5) + "," + dtcs.Task.Result);

            // A faulted TCS re-raises the stored exception at await.
            var ftcs = new TaskCompletionSource<int>();
            ftcs.SetException(new InvalidOperationException("tcs boom"));
            Console.WriteLine(AwaitFaulted(ftcs).Result);

            // A canceled TCS: the blocking Wait() wraps the TaskCanceledException in
            // an AggregateException — the wrap belongs to the blocking wait, not the task —
            // and the inner TCE is an OperationCanceledException.
            var ctcs = new TaskCompletionSource<int>();
            Console.WriteLine("cancel: " + ctcs.TrySetCanceled() + "," + ctcs.Task.IsCanceled);
            try { ctcs.Task.Wait(); }
            catch (AggregateException ae) when (ae.InnerException is OperationCanceledException oce)
            {
                Console.WriteLine("canceled: " + ae.GetType().Name + "(" + oce.GetType().Name + ")");
            }

            // Cross-thread completion: a pool worker settles the TCS while this
            // thread blocks on .Result (the async<->thread bridge wakes it).
            var xtcs = new TaskCompletionSource<string>();
            Task runner = Task.Run(() => xtcs.TrySetResult("done"));
            Console.WriteLine("bridge: " + xtcs.Task.Result);
            runner.Wait();
        }
    }
}
