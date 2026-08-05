#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;

// The third settler of the FfSettler family: a System.Threading.Timer callback, run on a
// thread the program never started. The principal count follows the timer's ARMED state —
// dueTime >= 0 and not disposed, plus the window a callback is actually running — and NOT
// the timer thread's lifetime, because a lifetime +1 would keep the principal set
// non-empty until Dispose and trade a false deadlock verdict for a silent hang. The disarm
// half cannot be diffed against real .NET (which has no detector); it is the
// timeridle/timerspent/timerdisarm/ontimer lines of the async-core gate's frozen
// ColdTaskDeadlock section. Due times are short but real: the waiter must reach the drain
// with the timer still pending.
namespace TimerSettler;

static class Program
{
    public static void __GateEntry()
    {
        // Disposal runs through BOTH routes: `using` (callvirt IDisposable::Dispose,
        // dispatched through the IDisposable row the init prologue installs onto the
        // intrinsic timer type-info) in sections 1/3/6, and the direct intrinsic
        // call-site Dispose() in 2/4/5/7/8.

        // 1. A one-shot armed at construction is the only thing that will ever settle the
        //    task the main thread blocks on.
        var tcs1 = new TaskCompletionSource<int>();
        using (var t1 = new Timer(_ => tcs1.SetResult(4321), null, 50, Timeout.Infinite))
        {
            tcs1.Task.Wait();
            Console.WriteLine("timer wait: " + tcs1.Task.Result + " " + tcs1.Task.IsCompleted);
        }

        // 2. The state object threads through, and .Result funnels into the same drain.
        var tcs2 = new TaskCompletionSource<string>();
        var t2 = new Timer(s => tcs2.SetResult("state-" + (int)s!), 7, 50, Timeout.Infinite);
        Console.WriteLine("timer result (state): " + tcs2.Task.Result);
        t2.Dispose();

        // 3. Change is an ARM transition: a timer born idle counts for nothing until
        //    Change gives it a due time, and the wait must park on that transition.
        var tcs3 = new TaskCompletionSource<int>();
        using (var t3 = new Timer(_ => tcs3.SetResult(77)))
        {
            t3.Change(50, Timeout.Infinite);
            Console.WriteLine("timer change-armed: " + tcs3.Task.Result);
        }

        // 4. A periodic timer stays armed across fires: the settling tick is not the first,
        //    so the count must survive the fire -> re-arm transition.
        var tcs4 = new TaskCompletionSource<int>();
        int ticks = 0;
        var t4 = new Timer(_ =>
        {
            if (Interlocked.Increment(ref ticks) == 3)
                tcs4.TrySetResult(3);
        }, null, 20, 20);
        Console.WriteLine("timer periodic tick: " + tcs4.Task.Result);
        t4.Dispose();

        // 5. The callback-running window is part of the armed state: a due time of 0 fires
        //    at once, so the principal may be the RUNNING callback rather than a pending
        //    due — the sleep holds it there.
        var tcs5 = new TaskCompletionSource<int>();
        var t5 = new Timer(_ =>
        {
            Thread.Sleep(50);
            tcs5.SetResult(99);
        }, null, 0, Timeout.Infinite);
        Console.WriteLine("timer in-callback: " + tcs5.Task.GetAwaiter().GetResult());
        t5.Dispose();

        // 6. The interface route with NO static receiver clue: an IDisposable-typed local,
        //    so only the runtime interface row on the intrinsic timer type-info can carry
        //    the dispatch.
        var tcs6 = new TaskCompletionSource<int>();
        IDisposable d6 = new Timer(_ => tcs6.SetResult(11), null, 50, Timeout.Infinite);
        Console.WriteLine("timer idisposable: " + tcs6.Task.Result);
        d6.Dispose();

        // 7. A Change issued while the callback is IN FLIGHT must be honored: the
        //    post-callback step must not overwrite dueMs with the re-arm/idle value, or a
        //    one-shot whose callback re-arms itself goes idle after tick 1. Born idle and
        //    armed only after the local is assigned, so the callback's read of t7 races
        //    nothing.
        var tcs7 = new TaskCompletionSource<int>();
        int ticks7 = 0;
        Timer? t7 = null;
        t7 = new Timer(_ =>
        {
            if (Interlocked.Increment(ref ticks7) < 3)
                t7!.Change(30, Timeout.Infinite);   // issued while this callback runs
            else
                tcs7.TrySetResult(ticks7);
        });
        t7.Change(30, Timeout.Infinite);
        Console.WriteLine("timer midcb rearm: " + tcs7.Task.Result);
        t7.Dispose();

        // 8. The other arm: a PERIODIC timer whose callback Changes mid-flight to "fire
        //    once more, then stop". The post-callback step must not re-read the period the
        //    Change already retired, or tick 2 never comes; honored, the count is exactly 2.
        var tcs8 = new TaskCompletionSource<int>();
        int ticks8 = 0;
        Timer? t8 = null;
        t8 = new Timer(_ =>
        {
            int n = Interlocked.Increment(ref ticks8);
            if (n == 1)
                t8!.Change(40, Timeout.Infinite);   // issued while this callback runs
            else
                tcs8.TrySetResult(n);
        });
        t8.Change(20, 20);
        Console.WriteLine("timer midcb oneshot: " + tcs8.Task.Result);
        t8.Dispose();
    }
}
