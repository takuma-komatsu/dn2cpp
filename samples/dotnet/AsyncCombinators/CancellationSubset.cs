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

        private static string FaultKind(Exception? error)
            => error is AggregateException aggregate
                ? "AggregateException/" + aggregate.InnerExceptions.Count
                : error?.GetType().Name ?? "returned";

        private static void FaultPolicy(string label, int policy)
        {
            using var source = new CancellationTokenSource();
            CancellationToken token = source.Token;
            var older = new ArgumentException("older fault");
            var newer = new InvalidOperationException("newer fault");
            string trace = "";
            token.Register(() => trace += "remaining;");
            token.Register(() => { trace += "older;"; throw older; });
            token.Register(() => { trace += "newer;"; throw newer; });
            Exception? error = null;
            try
            {
                if (policy == 0)
                    source.Cancel();
                else
                    source.Cancel(policy == 2);
            }
            catch (Exception e) { error = e; }
            bool identity = error is AggregateException aggregate
                ? ReferenceEquals(aggregate.InnerExceptions[0], newer)
                    && ReferenceEquals(aggregate.InnerExceptions[1], older)
                : ReferenceEquals(error, newer);
            source.Cancel(false);
            Console.WriteLine("cancel " + label + ": " + trace + "|" + FaultKind(error)
                + "|identity=" + identity + "|requested=" + source.IsCancellationRequested
                + "/" + token.IsCancellationRequested);
        }

        private static void StateTokenFaults(bool first)
        {
            using var source = new CancellationTokenSource();
            CancellationToken token = source.Token;
            object state = new object();
            var older = new ArgumentException("state fault");
            var newer = new InvalidOperationException("token fault");
            string trace = "";
            bool stateIdentity = false;
            bool tokenIdentity = false;
            token.Register(() => trace += "remaining;");
            token.Register(s =>
            {
                trace += "state;";
                stateIdentity = ReferenceEquals(s, state);
                throw older;
            }, state);
            token.UnsafeRegister((s, t) =>
            {
                trace += "token;";
                tokenIdentity = ReferenceEquals(s, state) && t == token && t.IsCancellationRequested;
                throw newer;
            }, state);
            Exception? error = null;
            try { source.Cancel(first); }
            catch (Exception e) { error = e; }
            bool identity = error is AggregateException aggregate
                ? ReferenceEquals(aggregate.InnerExceptions[0], newer)
                    && ReferenceEquals(aggregate.InnerExceptions[1], older)
                : ReferenceEquals(error, newer);
            Console.WriteLine("cancel state/token " + first + ": " + trace + "|" + FaultKind(error)
                + "|identity=" + identity + "/" + stateIdentity + "/" + tokenIdentity);
        }

        private static void LinkedFaults(bool first)
        {
            using var parent = new CancellationTokenSource();
            var parentFault = new ArgumentException("parent fault");
            var older = new ArgumentException("child older");
            var newer = new InvalidOperationException("child newer");
            string trace = "";
            parent.Token.Register(() => { trace += "parent;"; throw parentFault; });
            using var child = CancellationTokenSource.CreateLinkedTokenSource(parent.Token);
            child.Token.Register(() => { trace += "child-older;"; throw older; });
            child.Token.Register(() => { trace += "child-newer;"; throw newer; });
            AggregateException? error = null;
            try { parent.Cancel(first); }
            catch (AggregateException e) { error = e; }
            AggregateException? childError = first ? error : error?.InnerExceptions[0] as AggregateException;
            bool identity = childError is not null && childError.InnerExceptions.Count == 2
                && ReferenceEquals(childError.InnerExceptions[0], newer)
                && ReferenceEquals(childError.InnerExceptions[1], older)
                && (first || ReferenceEquals(error!.InnerExceptions[1], parentFault));
            Console.WriteLine("cancel linked " + first + ": " + trace + "|" + FaultKind(error)
                + "|nested=" + (error?.InnerException is AggregateException)
                + "|identity=" + identity + "|requested=" + parent.IsCancellationRequested
                + "/" + child.IsCancellationRequested);
        }

        private static void ReentrantFaults()
        {
            using var source = new CancellationTokenSource();
            var fault = new InvalidOperationException("registered while canceling");
            string trace = "";
            source.Token.Register(() => trace += "remaining;");
            source.Token.Register(() =>
            {
                trace += "outer;";
                source.Cancel(true);
                source.Token.Register(() => { trace += "immediate;"; throw fault; });
            });
            AggregateException? error = null;
            try { source.Cancel(); }
            catch (AggregateException e) { error = e; }
            Console.WriteLine("cancel reentrant: " + trace + "|" + FaultKind(error)
                + "|identity=" + ReferenceEquals(error?.InnerException, fault));
            Exception? direct = null;
            try { source.Token.Register(() => throw fault); }
            catch (Exception e) { direct = e; }
            Console.WriteLine("cancel already requested register: " + FaultKind(direct)
                + "|identity=" + ReferenceEquals(direct, fault));
        }

        private static void CollectedFaults()
        {
            using var source = new CancellationTokenSource();
            string trace = "";
            source.Token.Register(() => trace += "remaining;");
            source.Token.Register(() =>
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                trace += "older;";
                throw new ArgumentException("collected older");
            });
            source.Token.Register(() => { trace += "newer;"; throw new InvalidOperationException("collected newer"); });
            try { source.Cancel(false); }
            catch (AggregateException error)
            {
                Console.WriteLine("cancel collected: " + trace + "|" + error.InnerExceptions.Count
                    + "|" + error.InnerExceptions[0].Message + "," + error.InnerExceptions[1].Message);
            }
        }

        internal static void RunFaults()
        {
            Console.WriteLine("== cancellation callback faults ==");
            FaultPolicy("default", 0);
            FaultPolicy("false", 1);
            FaultPolicy("true", 2);
            StateTokenFaults(false);
            StateTokenFaults(true);
            LinkedFaults(false);
            LinkedFaults(true);
            ReentrantFaults();
            CollectedFaults();
            Console.WriteLine("cancellation callback faults end");
        }
    }
}
