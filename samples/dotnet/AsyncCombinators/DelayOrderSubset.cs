#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DelayOrderSubset
{
    // Timer deadlines do not order .NET's callback execution. The live oracle
    // orders appends through task dependencies; native virtual deadlines have a
    // separate frozen probe with no such dependencies.
    internal static class Program
    {
        private static readonly object s_gate = new object();
        private static string s_log = "";

        private static void Append(string id)
        {
            lock (s_gate)
            {
                s_log += id;
            }
        }

        private static async Task Work(string id, int ms)
        {
            await Task.Delay(ms);
            Append(id);
        }

        private static async Task WorkTs(string id, int ms)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(ms));
            Append(id);
        }

        private static async Task AppendAfter(string id, Task delay, Task predecessor)
        {
            await delay;
            await predecessor;
            Append(id);
        }

        private static async Task RunOrdered()
        {
            Task aDelay = Task.Delay(1600);
            Task bDelay = Task.Delay(400);
            Task cDelay = Task.Delay(1200);
            Task dDelay = Task.Delay(800);
            Task b = AppendAfter("B", bDelay, Task.CompletedTask);
            Task d = AppendAfter("D", dDelay, b);
            Task c = AppendAfter("C", cDelay, d);
            Task a = AppendAfter("A", aDelay, c);
            await Task.WhenAll(a, b, c, d);
            s_log += "|";

            Task xDelay = Task.Delay(TimeSpan.FromMilliseconds(600));
            Task yDelay = Task.Delay(TimeSpan.FromMilliseconds(200));
            Task y = AppendAfter("y", yDelay, Task.CompletedTask);
            Task x = AppendAfter("x", xDelay, y);
            await Task.WhenAll(x, y);
            s_log += "|";

            await Task.Delay(100);
            s_log += "done";
        }

        private static async Task RunNativeDeadlines()
        {
            await Task.WhenAll(Work("A", 1600), Work("B", 400), Work("C", 1200), Work("D", 800));
            s_log += "|";

            await Task.WhenAll(WorkTs("x", 600), WorkTs("y", 200));
            s_log += "|";

            await Task.Delay(100);
            s_log += "done";
        }

        internal static void __GateEntry()
        {
            RunOrdered().Wait();
            Console.WriteLine(s_log);
        }

        internal static void RunNativeDeadlineProbe()
        {
            Console.WriteLine("== native delay deadlines ==");
            RunNativeDeadlines().Wait();
            Console.WriteLine(s_log);
            Console.WriteLine("native delay deadlines end");
        }

        private static async Task<bool> ObserveCancellation(Task delay)
        {
            try
            {
                await delay;
                return false;
            }
            catch (OperationCanceledException)
            {
                return true;
            }
        }

        private static void CheckSuspension(string label, bool timeSpan)
        {
            using var source = new CancellationTokenSource();
            Task delay = timeSpan
                ? Task.Delay(Timeout.InfiniteTimeSpan, source.Token)
                : Task.Delay(Timeout.Infinite, source.Token);
            Task<bool> observer = ObserveCancellation(delay);
            bool delayPending = !delay.IsCompleted;
            bool observerPending = !observer.IsCompleted;
            source.Cancel();
            bool resumed = observer.GetAwaiter().GetResult();
            Console.WriteLine(label + " suspension: " + delayPending + "," + observerPending
                + "|" + resumed + "," + delay.IsCanceled);
        }

        internal static void RunSynchronizationChecks()
        {
            Console.WriteLine("== delay synchronization ==");
            CheckSuspension("integer", false);
            CheckSuspension("TimeSpan", true);
            s_log = "";
            var release = new TaskCompletionSource<bool>();
            Task first = AppendAfter("first", Task.CompletedTask, release.Task);
            Task second = AppendAfter("second", Task.CompletedTask, first);
            Console.WriteLine("dependency pending: " + !first.IsCompleted + "," + !second.IsCompleted
                + "|" + s_log.Length);
            release.SetResult(true);
            Task.WhenAll(first, second).GetAwaiter().GetResult();
            Console.WriteLine("dependency order: " + s_log);
            Console.WriteLine("delay synchronization end");
        }
    }
}
