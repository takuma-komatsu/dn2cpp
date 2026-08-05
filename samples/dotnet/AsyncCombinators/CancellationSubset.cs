#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CancellationSubset
{
    // CancellationToken / cancellation modeling. A CancellationTokenSource holds
    // a canceled flag and the pending Task.Delay()s bound to its token; Cancel()
    // transitions them to the (now real) CANCELED task state. Awaiting a canceled task
    // — or ThrowIfCancellationRequested on a requested token — throws an
    // OperationCanceledException catchable by a typed clause (the runtime is handed the
    // real type-info). CancellationToken.None never cancels. The virtual clock makes
    // the cancel-vs-completion ordering deterministic.
    internal static class Program
    {
        private static async Task<int> Workload(CancellationToken ct)
        {
            for (int i = 0; i < 5; i++)
            {
                await Task.Delay(10);
                ct.ThrowIfCancellationRequested();
            }
            return 99;
        }

        private static async Task<string> Run()
        {
            var cts = new CancellationTokenSource();
            CancellationToken tok = cts.Token;
            bool req0 = tok.IsCancellationRequested;          // False

            Task delayed = Task.Delay(1000, tok);             // cancellable long delay
            await Task.Delay(10);
            cts.Cancel();
            bool req1 = tok.IsCancellationRequested;          // True

            string c1 = "no";
            try { await delayed; }
            catch (OperationCanceledException) { c1 = "oce"; } // canceled delay -> OCE
            bool isCanc = delayed.IsCanceled, isFault = delayed.IsFaulted; // True, False

            int normal = await Workload(CancellationToken.None); // never cancels -> 99

            var cts2 = new CancellationTokenSource();
            Task<int> w = Workload(cts2.Token);
            await Task.Delay(25);
            cts2.Cancel();                                    // cancel mid-loop
            string loop = "?";
            try { await w; }
            catch (OperationCanceledException) { loop = "canceled"; }

            return req0 + "," + req1 + "," + c1 + "," + isCanc + "," + isFault + "," + normal + "," + loop;
        }

        internal static void __GateEntry()
        {
            Console.WriteLine(Run().Result);   // False,True,oce,True,False,99,canceled
        }
    }
}
