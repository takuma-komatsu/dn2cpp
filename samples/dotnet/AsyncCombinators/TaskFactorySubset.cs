#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;

namespace TaskFactorySubset
{
    // Task.Factory.StartNew + TaskScheduler.Default. Task.get_Factory and
    // TaskScheduler.get_Default are opaque sentinels; every StartNew form
    // dispatches to the same worker pool as Task.Run, and the trailing
    // CancellationToken / TaskCreationOptions / TaskScheduler arguments are
    // scheduling hints only. Covers the Action form, the Action<object>+state
    // form, the generic Func<TResult> form, the fully-hinted shape the base
    // Stream.FlushAsync compiles, the no-unwrap rule (StartNew(async ...)
    // returns a nested task, unlike Task.Run) with async bodies that really
    // suspend and one that faults after suspending, and a faulted delegate
    // whose fault is observed through Wait(). Prints happen only on the main
    // async flow after Wait()/await/.Result — never from the pool thread — so
    // the output is deterministic.
    internal static class Program
    {
        private static int s_plain;
        private static int s_hinted;
        private static int s_nested2;

        private static async Task<string> Run()
        {
            // StartNew(Action): the side effect is read only after Wait().
            Task plain = Task.Factory.StartNew(() => { s_plain = 11; });
            plain.Wait();
            int a = s_plain;

            // StartNew(Action<object>, state): the state round-trips into the body.
            int fromState = 0;
            Task stateful = Task.Factory.StartNew(s => { fromState = (int)s! + 1; }, 21);
            stateful.Wait();
            int b = fromState;

            // StartNew<int>(Func<int>): the result comes back through .Result.
            Task<int> generic = Task.Factory.StartNew(() => 30 + 3);
            int c = generic.Result;

            // The base Stream.FlushAsync shape: delegate + token + options +
            // scheduler (the three trailing arguments are hints; the delegate
            // still runs on the pool).
            Task hinted = Task.Factory.StartNew(
                () => { s_hinted = 44; },
                CancellationToken.None,
                TaskCreationOptions.DenyChildAttach,
                TaskScheduler.Default);
            hinted.Wait();
            int d = s_hinted;

            // StartNew never unwraps an async delegate (unlike Task.Run): the
            // result is a nested Task<Task<int>>, so the value needs two awaits.
            // The async body may really suspend: the suspension parks the inner
            // task's timers/continuations on the pool worker's own scheduler,
            // and that worker keeps draining it until the inner settles — so
            // awaiting the inner from here is safe.
            Task<Task<int>> nested = Task.Factory.StartNew(async () =>
            {
                await Task.Delay(20);
                await Task.Yield();
                return 55;
            });
            Task<int> inner = await nested;
            int e = await inner;

            // The non-generic inner shape: StartNew(async () => { ... }) is
            // StartNew<Task>(Func<Task>) -> Task<Task>. The side effect is read
            // only after both awaits complete on the main flow.
            Task<Task> nested2 = Task.Factory.StartNew(async () =>
            {
                await Task.Delay(10);
                s_nested2 = 66;
            });
            Task inner2 = await nested2;
            await inner2;
            int f = s_nested2;

            // A fault AFTER a suspension lands on the inner task, not the outer:
            // awaiting the outer still yields the inner task, and awaiting the
            // inner re-raises the original exception (both real .NET and the
            // transpiled runtime throw the bare exception from await).
            string innerFault = "none";
            Task<Task<int>> nestedFault = Task.Factory.StartNew<Task<int>>(async () =>
            {
                await Task.Delay(5);
                throw new InvalidOperationException("inner-boom");
            });
            Task<int> innerFaultTask = await nestedFault;
            try
            {
                innerFault = "no-throw:" + await innerFaultTask;
            }
            catch (InvalidOperationException ex)
            {
                innerFault = ex.GetType().Name + ":" + ex.Message;
            }

            // Faulted delegate: Wait() surfaces the fault, which MUST be
            // observed (an unobserved faulted task would be nondeterministic).
            // Both sides wrap it in an AggregateException; the unwrap-to-root line stays
            // because this section asserts the fault SURFACING, while the wrapper's own
            // shape belongs to BlockingWaitWrapSubset.
            string fault = "none";
            Task faulted = Task.Factory.StartNew(() => throw new InvalidOperationException("boom"));
            try
            {
                faulted.Wait();
            }
            catch (Exception ex)
            {
                Exception root = ex is AggregateException agg ? agg.InnerException! : ex;
                fault = root.GetType().Name + ":" + root.Message;
            }

            return a + "," + b + "," + c + "," + d + "," + e + "," + f + "," + innerFault + "," + fault;
        }

        internal static void __GateEntry()
        {
            Console.WriteLine(Run().Result); // 11,22,33,44,55,66,InvalidOperationException:inner-boom,InvalidOperationException:boom
        }
    }
}
