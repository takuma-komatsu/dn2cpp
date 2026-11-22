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
    // the AggregateException's Message composes the inner messages.
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

        internal static void RunTimedWaits()
        {
            using var cancellation = new CancellationTokenSource();
            Task later = Task.Delay(TimeSpan.FromDays(1), cancellation.Token);
            Console.WriteLine("wait before delay: " + later.Wait(TimeSpan.FromMilliseconds(1)));
            cancellation.Cancel();
            Console.WriteLine("wait through delay: " + Task.Delay(1).Wait(TimeSpan.FromSeconds(8)));

            Task pending = new TaskCompletionSource<int>().Task;
            Console.WriteLine("wait pending zero: " + pending.Wait(TimeSpan.Zero));
            Console.WriteLine("wait pending finite: " + pending.Wait(TimeSpan.FromMilliseconds(1)));

            PollDelayedTasks(waitForCallbacks: true);

            using var signal = new ManualResetEventSlim();
            Task<bool> yielded = WaitForSignalAfterYield(signal);
            bool completed = yielded.Wait(TimeSpan.FromMilliseconds(10));
            signal.Set();
            Console.WriteLine("wait before signal: " + completed + "/" + yielded.GetAwaiter().GetResult());
        }

        private static void PrepareTimerPoll(bool waitForCallbacks, params Task[] tasks)
        {
            // CLR timer callbacks may still be queued after their deadlines. The native
            // deadline probe leaves its scheduler idle until the zero-time wait.
            if (waitForCallbacks)
                Task.WaitAll(tasks);
            else
                Thread.Sleep(125);
        }

        internal static void PollDelayedTasks(bool waitForCallbacks)
        {
            Task early = Task.Delay(10);
            Task late = Task.Delay(30);
            PrepareTimerPoll(waitForCallbacks, early, late);
            Task<Task> winner = Task.WhenAny(early, late);
            bool polled = late.Wait(TimeSpan.Zero);
            if (!winner.IsCompletedSuccessfully || !ReferenceEquals(winner.Result, early))
                throw new InvalidOperationException("Polling the later delay reversed the timer order.");
            Console.WriteLine("wait after idle: " + polled + "/" + early.Wait(TimeSpan.FromMilliseconds(5)));
        }

        internal static void PollDelayedWhenAll(bool waitForCallbacks)
        {
            Task join = Task.WhenAll(Task.Delay(10), Task.Delay(30));
            PrepareTimerPoll(waitForCallbacks, join);
            Console.WriteLine("overdue WhenAll poll: " + join.Wait(TimeSpan.Zero));
        }

        private static async Task<bool> WaitForSignalAfterYield(ManualResetEventSlim signal)
        {
            await Task.Yield();
            return signal.Wait(TimeSpan.FromSeconds(8));
        }

        internal static void RunDelayDeadlines()
        {
            var guard = TimeSpan.FromSeconds(8);
            var fresh = new TaskCompletionSource<Task>();
            var later = new TaskCompletionSource<Task>();
            using var startRace = new ManualResetEventSlim();
            using var release = new ManualResetEventSlim();
            bool released = false;
            var producer = new Thread(() =>
            {
                fresh.SetResult(Task.Delay(50));
                if (!startRace.Wait(guard))
                    return;
                later.SetResult(Task.Delay(180));
                released = release.Wait(guard);
            });
            producer.Start();
            bool completed;
            bool laterCompleted;
            bool timedOut = false;
            try
            {
                if (!fresh.Task.Wait(guard))
                    throw new TimeoutException("The producer did not arm its delay.");
                completed = fresh.Task.Result.Wait(TimeSpan.FromSeconds(2));
                startRace.Set();
                if (!later.Task.Wait(guard))
                    throw new TimeoutException("The producer did not arm its second delay.");
                Task original = later.Task.Result;
                Task limited = original.WaitAsync(TimeSpan.FromMilliseconds(10));
                Thread.Sleep(230);
                laterCompleted = original.Wait(TimeSpan.FromSeconds(2));
                if (limited.IsCompleted)
                {
                    try { limited.GetAwaiter().GetResult(); }
                    catch (TimeoutException) { timedOut = true; }
                }
            }
            finally
            {
                startRace.Set();
                release.Set();
                if (!producer.Join(guard))
                    throw new TimeoutException("The producer did not exit after release.");
            }
            Console.WriteLine("parked owner delay: completed=" + completed + " owner released=" + released);

            PollDelayedWhenAll(waitForCallbacks: true);
            Console.WriteLine("parked owner race: completed=" + laterCompleted + " earlier timeout=" + timedOut);
        }
    }
}
