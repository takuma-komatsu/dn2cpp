#nullable enable
using System;
using System.Threading.Tasks;

namespace ColdTaskDeadlockSubset
{
    // The counterpart to ColdTaskSubset: a blocking wait no principal can EVER satisfy.
    // Nothing is runnable on this thread, no pool work is in flight, and no user thread is
    // alive — so no Task.RunSynchronously, no TaskCompletionSource.SetResult and no pool
    // worker can settle the task, ever.
    //
    // THIS SECTION IS WHY THE ASYNC-CORE GATE IS FROZEN AND NOT DIFFED. Real .NET has no
    // deadlock detector: every wait below blocks forever there, so there is no oracle to
    // diff against — a `dotnet $app` run would hang the gate rather than disagree with it.
    // dn2cpp deliberately diverges and reports instead, and reports *catchably*: a
    // transpiled Godot game must be able to log one wedged frame and keep running rather
    // than take the process down (it used to abort with SIGABRT), and it must not simply
    // hang, because a freeze names nothing. The frozen lines below therefore pin three
    // things: the exception type a host boundary can catch, the diagnosis the message
    // carries, and that the program is still alive and still runs tasks afterwards.
    //
    // Every line also reaches stderr unconditionally, which is not tested here (the gate
    // diffs stdout) but is the point of the report: a caught exception leaves no trace, and
    // an abort replaced by a silent stall is a worse thing to operate than the abort was.
    //
    // What is NOT tested here, on purpose: a wait that STAYS satisfiable — one with a live
    // user thread that never exits. That thread is a principal that could still settle the
    // task, so the wait correctly sleeps forever, which is real .NET's behavior and would
    // hang this gate. The `afterexit` case below is the closest safe neighbour: it starts
    // satisfiable and becomes defeated when the last principal leaves.
    internal static class Program
    {
        private static string Defeated(Action wait)
        {
            try
            {
                wait();
                return "NOT THROWN";
            }
            catch (InvalidOperationException e)
            {
                // Only the head of the diagnosis is printed: the remedy sentence is prose
                // and would make the snapshot a copy of it.
                string m = e.Message;
                int cut = m.IndexOf(')');
                return cut < 0 ? m : m.Substring(0, cut + 1);
            }
        }

        internal static void __GateEntry()
        {
            // A promise-style task nobody will ever complete, waited three ways — the three
            // mouths that funnel into the blocking drain.
            Console.WriteLine("wait: " + Defeated(() => new TaskCompletionSource<int>().Task.Wait()));
            Console.WriteLine("result: " + Defeated(() =>
            {
                int _ = new TaskCompletionSource<int>().Task.Result;
            }));
            Console.WriteLine("getresult: " + Defeated(() =>
                new TaskCompletionSource<int>().Task.GetAwaiter().GetResult()));

            // A cold task that was never started: its work exists but no principal owns it.
            var never = new Task(() => Console.WriteLine("unreachable"));
            Console.WriteLine("coldnever: " + Defeated(() => never.Wait()));

            // The batch waits reach the same verdict, and WaitAll reaches it on a member
            // that is not the first (its first input is already settled).
            var settled = Task.FromResult(1);
            var stuck = new TaskCompletionSource<int>().Task;
            Console.WriteLine("waitall: " + Defeated(() => Task.WaitAll(new Task[] { settled, stuck })));
            Console.WriteLine("waitany: " + Defeated(() => Task.WaitAny(new Task[] { stuck })));

            // A wait that is satisfiable when it STARTS and becomes defeated while it
            // sleeps. This is the case the whole wake mechanism exists for: the waiter
            // parks because a user thread is alive, that thread then exits without
            // settling anything, and nothing about a thread exiting touches the task being
            // waited on — so there is no completion to ride and the last principal out has
            // to wake the waiters itself. A regression here does not print a wrong line, it
            // HANGS forever; the gate's bounded run is the assertion.
            var orphaned = new TaskCompletionSource<int>();
            var quitter = new System.Threading.Thread(() => System.Threading.Thread.Sleep(50));
            quitter.Start();
            Console.WriteLine("afterexit: " + Defeated(() => orphaned.Task.Wait()));
            quitter.Join();

            // The same wake, for the two principals that are NOT tasks and NOT user
            // threads. Both are counted precisely so a wait they could settle parks instead
            // of reporting; the assertion here is the other half of that bargain — the count
            // comes back DOWN, so the report re-arms. A leaked increment does not print a
            // wrong line, it disarms the detector for the rest of the process: every wait
            // below it hangs forever, silently, which is the failure the counting was
            // introduced to avoid in the first place. The gate's bounded run is the
            // assertion, and the delay is the point — the waiter has to park first.
            var afterFf = new TaskCompletionSource<int>();
            System.Threading.ThreadPool.QueueUserWorkItem(_ => System.Threading.Thread.Sleep(50));
            Console.WriteLine("afterff: " + Defeated(() => afterFf.Task.Wait()));

            // The CancelAfter timer thread is a principal for exactly as long as its delay:
            // this wait parks because the timer is armed, and is answered when the timer
            // fires and the thread leaves. Nothing registers a callback, so the cancel
            // settles nothing — the only thing that ends the wait is the departure.
            var afterTimer = new TaskCompletionSource<int>();
            var timed = new System.Threading.CancellationTokenSource();
            timed.CancelAfter(50);
            Console.WriteLine("aftertimer: " + Defeated(() => afterTimer.Task.Wait()));

            // A System.Threading.Timer is a principal only while it is ARMED (a due time
            // is pending, or a callback is running) — never for the mere lifetime of its
            // undisposed thread. The failure these lines guard against prints no wrong
            // line: a lifetime-counted timer disarms the deadlock detector until Dispose,
            // so every wait below would HANG forever. The gate's bounded run is the assert.

            // Born idle (the 1-arg ctor, no due time): counts for nothing from the start.
            var idleTimer = new System.Threading.Timer(_ => { });
            var idleWait = new TaskCompletionSource<int>();
            Console.WriteLine("timeridle: " + Defeated(() => idleWait.Task.Wait()));
            idleTimer.Dispose();

            // A fired one-shot goes idle, and the report re-arms on that transition:
            // the wait parks while the due time is pending (the timer could still settle
            // it), and is answered when the empty callback returns and the timer goes
            // idle — undisposed, which is exactly the state a lifetime-counting fix
            // would wrongly treat as an armed principal.
            var spent = new System.Threading.Timer(_ => { }, null, 50, System.Threading.Timeout.Infinite);
            var spentWait = new TaskCompletionSource<int>();
            Console.WriteLine("timerspent: " + Defeated(() => spentWait.Task.Wait()));
            spent.Dispose();

            // Change to Infinite is a DISARM transition: the pending due is retired and
            // the principal leaves at the Change, not at Dispose.
            var disarmed = new System.Threading.Timer(_ => { }, null, 60000, System.Threading.Timeout.Infinite);
            disarmed.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
            var disarmWait = new TaskCompletionSource<int>();
            Console.WriteLine("timerdisarm: " + Defeated(() => disarmWait.Task.Wait()));
            disarmed.Dispose();

            // The timer thread must not count as its own principal while its callback is
            // the one blocking: a TimerCallback waiting on a task nothing will ever
            // settle has to get the report rather than hang on its own armed state. The
            // outer wait is satisfiable (the armed timer is its principal); the inner one
            // is not, and the callback carries the verdict out.
            string onTimer = "NOT RUN";
            var timerDone = new TaskCompletionSource<int>();
            var reporting = new System.Threading.Timer(_ =>
            {
                onTimer = Defeated(() => new TaskCompletionSource<int>().Task.Wait());
                timerDone.SetResult(1);
            }, null, 30, System.Threading.Timeout.Infinite);
            timerDone.Task.Wait();
            reporting.Dispose();
            Console.WriteLine("ontimer: " + onTimer);

            // The waiter must not count as its own principal. A LONE user thread blocking
            // on a task nothing will ever settle has to get the report rather than hang on
            // its own liveness — so the verdict discounts the current thread. A regression
            // here does not print a wrong line, it HANGS (the gate's bounded run is what
            // catches that), which is why this case is worth its own thread. The catch has
            // to be inside the body: an exception escaping a thread body terminates the
            // process, as in .NET.
            string onThread = "NOT RUN";
            var lone = new System.Threading.Thread(() =>
                onThread = Defeated(() => new TaskCompletionSource<int>().Task.Wait()));
            lone.Start();
            lone.Join();
            Console.WriteLine("onthread: " + onThread);

            // Catchable means catchable: the runtime is still usable after the report.
            Console.WriteLine("alive: " + Task.Run(() => 6 * 7).Result);
        }
    }
}
