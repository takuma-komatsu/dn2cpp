#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BlockingWaitWrapSubset
{
    // The blocking-wait wrap contract: Task.Wait()/Wait(timeout)/Task<T>.Result
    // wrap a fault or cancellation in an AggregateException — the wrap belongs to the
    // blocking wait, not to the task — while GetAwaiter().GetResult() re-raises the
    // stored exception unwrapped. A canceled task carries a TaskCanceledException
    // (which IS an OperationCanceledException), with real .NET's default message, and
    // the AggregateException's Message composes the inner messages. All tasks here are
    // pre-settled, so every line is deterministic and diffed exact vs real .NET.
    internal static class Program
    {
        private static void Show(string label, Action a)
        {
            try
            {
                a();
                Console.WriteLine(label + ": no-throw");
            }
            catch (Exception e)
            {
                Console.WriteLine(label + ": " + e.GetType().Name + " | " + e.Message);
            }
        }

        internal static void __GateEntry()
        {
            var cts = new CancellationTokenSource();
            cts.Cancel();
            Task canceled = Task.FromCanceled(cts.Token);
            Task<int> canceledInt = Task.FromCanceled<int>(cts.Token);
            Console.WriteLine("canceled status: " + canceled.IsCanceled + " "
                + canceled.IsFaulted + " " + canceled.IsCompleted);

            // The three blocking mouths wrap.
            Show("canceled wait", () => canceled.Wait());
            Show("canceled wait(ts)", () => canceled.Wait(TimeSpan.FromSeconds(5)));
            Show("canceled result", () => { int _ = canceledInt.Result; });
            // The awaiter mouth does not.
            Show("canceled getresult", () => canceled.GetAwaiter().GetResult());
            Show("canceledInt getresult", () => { int _ = canceledInt.GetAwaiter().GetResult(); });

            // The wrapped shape in detail: one inner, a TaskCanceledException that is
            // also an OperationCanceledException, carrying .NET's default message.
            try
            {
                canceled.Wait();
            }
            catch (AggregateException ae)
            {
                IReadOnlyList<Exception> inner = ae.InnerExceptions;
                Console.WriteLine("canceled inner: count=" + inner.Count
                    + " " + inner[0].GetType().Name
                    + " isOce=" + (inner[0] is OperationCanceledException)
                    + " | " + inner[0].Message);
                Console.WriteLine("canceled same: " + ReferenceEquals(ae.InnerException, inner[0]));
            }

            // A faulted task through the same funnel.
            Task<int> faulted = Task.FromException<int>(new InvalidOperationException("boom"));
            Show("faulted wait", () => ((Task)faulted).Wait());
            Show("faulted wait(ts)", () => ((Task)faulted).Wait(TimeSpan.FromSeconds(5)));
            Show("faulted result", () => { int _ = faulted.Result; });
            Show("faulted getresult", () => { int _ = faulted.GetAwaiter().GetResult(); });
            try
            {
                ((Task)faulted).Wait();
            }
            catch (AggregateException ae)
            {
                Console.WriteLine("faulted inner: " + ae.InnerException!.GetType().Name
                    + " | " + ae.InnerException!.Message);
            }

            // A successful task keeps every mouth quiet — the wrap must not
            // touch the completed path.
            Task<int> done = Task.FromResult(41);
            Show("done wait", () => ((Task)done).Wait());
            Console.WriteLine("done result: " + done.Result
                + " " + done.GetAwaiter().GetResult());
        }
    }
}
