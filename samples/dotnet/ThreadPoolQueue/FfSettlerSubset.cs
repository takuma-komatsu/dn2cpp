#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;

// Two settlers that are neither a pool Task nor a live user thread, and so must still count
// as principals when a blocked Wait()/.Result drains and asks whether anything can settle
// it: a fire-and-forget ThreadPool item (which carries no Task), and the CancelAfter timer
// (a runtime-internal thread the program never started). An empty principal set is a
// deadlock verdict, so a miss here aborts a program that was about to make progress.
// Each settling item sleeps first so the waiter reaches the drain with it still
// outstanding; every value is read only after the wait returns.
namespace FfSettler;

static class Program
{
    // The IThreadPoolWorkItem mouth: UnsafeQueueUserWorkItem resolves Execute() through
    // the receiver's interface table at the enqueue site.
    sealed class SettleItem : IThreadPoolWorkItem
    {
        private readonly TaskCompletionSource<int> _tcs;
        private readonly int _value;

        public SettleItem(TaskCompletionSource<int> tcs, int value)
        {
            _tcs = tcs;
            _value = value;
        }

        public void Execute()
        {
            Thread.Sleep(5);
            _tcs.SetResult(_value);
        }
    }

    public static void __GateEntry()
    {
        // 1. The bare hand-off through the three blocking mouths. Nothing here is a
        //    Task.Run and nothing here is a thread the program started.
        var tcs1 = new TaskCompletionSource<int>();
        ThreadPool.QueueUserWorkItem(_ =>
        {
            Thread.Sleep(5);
            tcs1.SetResult(1234);
        });
        tcs1.Task.Wait();
        Console.WriteLine("ff wait: " + tcs1.Task.Result + " " + tcs1.Task.IsCompleted);

        var tcs2 = new TaskCompletionSource<string>();
        ThreadPool.QueueUserWorkItem(o =>
        {
            Thread.Sleep(5);
            tcs2.SetResult("state-" + (int)o!);
        }, 7);
        Console.WriteLine("ff result (2-arg state): " + tcs2.Task.Result);

        var tcs3 = new TaskCompletionSource<long>();
        ThreadPool.QueueUserWorkItem(_ =>
        {
            Thread.Sleep(5);
            tcs3.SetResult(1L << 33);
        });
        Console.WriteLine("ff getresult: " + tcs3.Task.GetAwaiter().GetResult());

        // 2. A fault handed across the same boundary. Both sides wrap a Wait() fault in an
        //    AggregateException; the unwrap-to-root is deliberate, since what this asserts
        //    is the hand-off (the wrapper's shape is BlockingWaitWrapSubset's assert).
        var tcs4 = new TaskCompletionSource<int>();
        ThreadPool.QueueUserWorkItem(_ =>
        {
            Thread.Sleep(5);
            tcs4.SetException(new InvalidOperationException("ff-boom"));
        });
        try
        {
            tcs4.Task.Wait();
            Console.WriteLine("ff fault: NOT THROWN");
        }
        catch (Exception e)
        {
            Exception root = e is AggregateException agg ? agg.InnerException! : e;
            Console.WriteLine("ff fault: " + root.Message + " " + tcs4.Task.IsFaulted);
        }

        // 3. The settling item is queued by another item, so the principal set is never
        //    empty but never holds the same member twice in a row.
        var tcs5 = new TaskCompletionSource<int>();
        ThreadPool.QueueUserWorkItem(_ =>
        {
            Thread.Sleep(5);
            ThreadPool.QueueUserWorkItem(__ =>
            {
                Thread.Sleep(5);
                tcs5.SetResult(99);
            });
        });
        Console.WriteLine("ff chain: " + tcs5.Task.Result);

        // 4. UnsafeQueueUserWorkItem's WaitCallback and IThreadPoolWorkItem forms: both
        //    land in the same pool arm.
        var tcs6 = new TaskCompletionSource<int>();
        ThreadPool.UnsafeQueueUserWorkItem(_ =>
        {
            Thread.Sleep(5);
            tcs6.SetResult(4242);
        }, null);
        Console.WriteLine("ff unsafe waitcallback: " + tcs6.Task.Result);

        var tcs7 = new TaskCompletionSource<int>();
        ThreadPool.UnsafeQueueUserWorkItem(new SettleItem(tcs7, 55), false);
        Console.WriteLine("ff unsafe workitem: " + tcs7.Task.Result);

        var tcsValue = new TaskCompletionSource<int>();
        ThreadPool.UnsafeQueueUserWorkItem<(TaskCompletionSource<int>, int)>(
            static s => s.Item1.SetResult(s.Item2), (tcsValue, 66), false);
        Console.WriteLine("ff unsafe value state: " + tcsValue.Task.Result);

        var tcsPrimitive = new TaskCompletionSource<int>();
        ThreadPool.QueueUserWorkItem<int>(
            s => tcsPrimitive.SetResult(s), 77, false);
        Console.WriteLine("ff primitive state: " + tcsPrimitive.Task.Result);

        var tcsContext = new TaskCompletionSource<int>();
        new SynchronizationContext().Post(
            static s => ((TaskCompletionSource<int>)s!).SetResult(88), tcsContext);
        Console.WriteLine("sync context base post: " + tcsContext.Task.Result);

        // 5. Several promises settled by several items, waited with WaitAll.
        var batch = new Task[6];
        int[] got = new int[6];
        for (int i = 0; i < batch.Length; i++)
        {
            int k = i;
            var p = new TaskCompletionSource<int>();
            batch[i] = p.Task;
            ThreadPool.QueueUserWorkItem(_ =>
            {
                Thread.Sleep(5);
                got[k] = (k + 1) * 10;
                p.SetResult(k);
            });
        }
        Task.WaitAll(batch);
        int total = 0;
        foreach (int g in got)
            total += g;
        Console.WriteLine("ff waitall total: " + total); // 10+20+...+60 == 210

        // 6. The CancelAfter timer as a principal: the ONLY thing that can settle this wait
        //    is a cancel callback on a runtime-internal timer thread — no pool item and no
        //    user thread exists at any point.
        var cts = new CancellationTokenSource();
        var tcs8 = new TaskCompletionSource<string>();
        cts.Token.Register(() => tcs8.SetResult("by-cancel-callback"));
        cts.CancelAfter(20);
        Console.WriteLine("cancelafter settles wait: " + tcs8.Task.Result
            + " " + cts.IsCancellationRequested);

        // 7. The same through .Result, registering after the timer is armed, and with the
        //    CancelAfter rescheduled before it fires so the wait outlives the reschedule.
        var cts2 = new CancellationTokenSource();
        cts2.CancelAfter(1000);
        cts2.CancelAfter(20); // reschedule onto the already-running timer thread
        var tcs9 = new TaskCompletionSource<int>();
        cts2.Token.Register(() => tcs9.SetResult(777));
        Console.WriteLine("cancelafter reschedule settles wait: " + tcs9.Task.Result);

        // 8. Both principals in one wait: an item settles one promise, a cancel callback
        //    the other.
        var cts3 = new CancellationTokenSource();
        var fromFf = new TaskCompletionSource<int>();
        var fromTimer = new TaskCompletionSource<int>();
        cts3.Token.Register(() => fromTimer.SetResult(2));
        ThreadPool.QueueUserWorkItem(_ =>
        {
            Thread.Sleep(5);
            fromFf.SetResult(1);
        });
        cts3.CancelAfter(20);
        Task.WaitAll(new Task[] { fromFf.Task, fromTimer.Task });
        Console.WriteLine("mixed ff+timer: " + fromFf.Task.Result + " " + fromTimer.Task.Result);
    }
}
