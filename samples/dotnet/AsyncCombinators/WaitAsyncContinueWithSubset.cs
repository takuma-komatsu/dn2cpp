#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;

namespace WaitAsyncContinueWithSubset
{
    // SUBJECT: two lowerings reached on the real Grpc.Net.Client unary path, both easy to
    // get subtly wrong.
    //
    //  (1) Task.WaitAsync(CancellationToken) — a task mirroring the antecedent's outcome
    //      that settles CANCELED as soon as the token cancels. What is asserted is the
    //      RACE, in both directions and exactly once: the antecedent winning (result and
    //      fault alike pass through), the token winning (TaskCanceledException, and the
    //      antecedent keeps running — WaitAsync bounds the WAIT, not the work), and the
    //      PRECEDENCE between the two when both are already settled. That last row is the
    //      one an intuitive "whichever fires first" implementation gets backwards: .NET's
    //      WaitAsyncCore returns `this` before it ever looks at the token, so an
    //      already-COMPLETE antecedent beats an already-canceled token.
    //
    //  (2) Task.ContinueWith with TaskContinuationOptions — the CONDITIONAL
    //      continuations. These used to be a loud transpile refusal precisely because the
    //      alternative (popping the operand and ignoring it) would have run an
    //      OnlyOnFaulted continuation after a successful antecedent, which compiles and
    //      does the wrong thing. Every filter spelling — the six NotOn*/OnlyOn* names plus
    //      the unfiltered None — is exercised against each of the three antecedent
    //      outcomes, and the assertion is on BOTH halves of
    //      the semantics: whether the delegate ran, and — the half that is easy to miss —
    //      that a continuation whose predicate excluded it is CANCELED rather than
    //      completed, so awaiting it throws.
    //
    // Every fault is observed and every wait is bounded by a settled task, so the
    // program is deterministic and diffs exact against real .NET.
    internal static class Program
    {
        // An antecedent settled in each of the three ways ContinueWith's filters
        // discriminate. The CANCELED one is built with Task.FromCanceled rather than by
        // letting an async method throw an OperationCanceledException, and that is a
        // deliberate narrowing rather than convenience: real .NET's async builder maps a
        // thrown OCE onto a CANCELED task while dn2cpp's maps it onto a FAULTED one, so an
        // OCE-throwing helper would make this section's verdict depend on that unrelated
        // divergence instead of on the filter semantics it exists to assert. (The
        // divergence is real and is worth its own ticket; it is NOT what these rows are
        // about, and asserting it here would have frozen dn2cpp's answer as correct.)
        private static Task Settled(int kind)
        {
            if (kind == 1)
            {
                return Task.FromException(new InvalidOperationException("boom"));
            }
            if (kind == 2)
            {
                var pre = new CancellationTokenSource();
                pre.Cancel();
                return Task.FromCanceled(pre.Token);
            }
            return Task.FromResult("ok");
        }

        private static string Outcome(Task t) =>
            t.IsCanceled ? "canceled" : t.IsFaulted ? "faulted" : "ran";

        private static async Task Run()
        {
            // ---- (1) WaitAsync: the antecedent wins ----
            var live = new CancellationTokenSource();
            Console.WriteLine("waitasync result: " + await Task.FromResult(7).WaitAsync(live.Token));
            try
            {
                await Task.FromException(new InvalidOperationException("faulted-through")).WaitAsync(live.Token);
            }
            catch (InvalidOperationException e)
            {
                Console.WriteLine("waitasync fault passes through: " + e.Message);
            }

            // ---- (1) WaitAsync: the token wins, and the work survives it ----
            var tcs = new TaskCompletionSource<int>();
            var late = new CancellationTokenSource();
            Task<int> bounded = tcs.Task.WaitAsync(late.Token);
            late.Cancel();
            try
            {
                await bounded;
                Console.WriteLine("waitasync: NOT REACHED");
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("waitasync canceled by token: " + Outcome(bounded));
            }
            // The antecedent was never touched: completing it afterwards still works, and
            // the already-settled wrapper does not change.
            tcs.SetResult(11);
            Console.WriteLine("waitasync antecedent kept running: " + await tcs.Task
                + " wrapper=" + Outcome(bounded));

            // ---- (1) WaitAsync: an already-canceled token beats a completed antecedent ----
            var dead = new CancellationTokenSource();
            dead.Cancel();
            Task<int> preCanceled = Task.FromResult(5).WaitAsync(dead.Token);
            try
            {
                await preCanceled;
                Console.WriteLine("waitasync pre-canceled: NOT REACHED");
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("waitasync pre-canceled token wins: " + Outcome(preCanceled));
            }

            // ---- (2) ContinueWith filters, every spelling against every outcome ----
            var filters = new (string Name, TaskContinuationOptions Opt)[]
            {
                ("None", TaskContinuationOptions.None),
                ("OnlyOnRanToCompletion", TaskContinuationOptions.OnlyOnRanToCompletion),
                ("OnlyOnFaulted", TaskContinuationOptions.OnlyOnFaulted),
                ("OnlyOnCanceled", TaskContinuationOptions.OnlyOnCanceled),
                ("NotOnRanToCompletion", TaskContinuationOptions.NotOnRanToCompletion),
                ("NotOnFaulted", TaskContinuationOptions.NotOnFaulted),
                ("NotOnCanceled", TaskContinuationOptions.NotOnCanceled),
            };
            string[] kinds = { "ran", "faulted", "canceled" };
            for (int k = 0; k < kinds.Length; k++)
            {
                foreach (var (name, opt) in filters)
                {
                    Task ante = Settled(k);
                    try
                    {
                        await ante;
                    }
                    catch (Exception)
                    {
                        // Observed on purpose: the antecedent's outcome is the input here.
                    }
                    int ran = 0;
                    Task cont = ante.ContinueWith(_ => { ran = 1; }, opt);
                    string awaited;
                    try
                    {
                        await cont;
                        awaited = "completed";
                    }
                    catch (OperationCanceledException)
                    {
                        awaited = "threw-canceled";
                    }
                    Console.WriteLine($"cw {kinds[k],-8} {name,-21} ran={ran} cont={Outcome(cont)} await={awaited}");
                }
            }

            // ---- (2) the state-carrying and result-returning forms with a filter ----
            Task okAnte = Settled(0);
            await okAnte;
            Task withState = okAnte.ContinueWith(
                static (_, s) => Console.WriteLine("cw state: " + s), "carried",
                CancellationToken.None, TaskContinuationOptions.OnlyOnRanToCompletion,
                TaskScheduler.Default);
            await withState;
            Task<int> typed = okAnte.ContinueWith(_ => 42, TaskContinuationOptions.NotOnFaulted);
            Console.WriteLine("cw typed result: " + await typed);
            Console.WriteLine("waitasync/continuewith section done");
        }

        internal static void __GateEntry() => Run().GetAwaiter().GetResult();
    }
}
