using System;
using System.Threading;

internal static class TimerValidationSubset
{
    public static void __GateEntry()
    {
        Console.WriteLine("== Timer validation fields ==");
        TimerCallback cb = static _ => { };
        int[] ints = { int.MinValue, -2, -1, 0, 1, int.MaxValue };
        long[] longs = { long.MinValue, -2, -1, 0, 1, int.MaxValue, 4294967294L, 4294967295L, long.MaxValue };
        long[] ticks = { long.MinValue, -20000, -19999, -10000, -9999, -1, 0, 1, 10000, 42949672940000L, 42949672950000L, 9223372036854769999L, -9223372036854769999L, long.MaxValue };
        for (int callback = 0; callback < 2; callback++)
        {
            TimerCallback? f = callback == 0 ? null : cb;
            O("timer callback:" + callback, () => { using var t = new Timer(f!); return "created"; });
            foreach (int due in ints) foreach (int period in ints)
                O("timer int:" + callback + ":" + due + ":" + period, () => { using var t = new Timer(f!, null, due, period); return "created"; });
            foreach (long due in longs) foreach (long period in longs)
                O("timer long:" + callback + ":" + due + ":" + period, () => { using var t = new Timer(f!, null, due, period); return "created"; });
            foreach (long due in ticks) foreach (long period in ticks)
            {
                TimeSpan d = new TimeSpan(due), r = new TimeSpan(period);
                O("timer span:" + callback + ":" + due + ":" + period, () => { using var t = new Timer(f!, null, d, r); return "created"; });
                O("provider timer:" + callback + ":" + due + ":" + period, () => { using var t = TimeProvider.System.CreateTimer(f!, null, d, r); return "created"; });
            }
            foreach (uint due in new uint[] { 0, 1, 2147483648, 4294967294, uint.MaxValue }) foreach (uint period in new uint[] { 0, 1, 2147483648, 4294967294, uint.MaxValue })
                O("timer uint:" + callback + ":" + due + ":" + period, () => { using var t = new Timer(f!, null, due, period); return "created"; });
        }
        for (int state = 0; state < 3; state++)
        {
            using var live = new Timer(cb);
            Timer? timer = state == 0 ? null : live;
            if (state == 2) live.Dispose();
            foreach (int due in ints) foreach (int period in ints)
                O("change int:" + state + ":" + due + ":" + period, () => timer!.Change(due, period).ToString());
            foreach (long due in longs) foreach (long period in longs)
                O("change long:" + state + ":" + due + ":" + period, () => timer!.Change(due, period).ToString());
            foreach (long due in ticks) foreach (long period in ticks)
                O("change span:" + state + ":" + due + ":" + period, () => timer!.Change(new TimeSpan(due), new TimeSpan(period)).ToString());
            using var it = TimeProvider.System.CreateTimer(cb, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            ITimer? iface = state == 0 ? null : it;
            if (state == 2) it.Dispose();
            foreach (long due in ticks) foreach (long period in ticks)
                O("change interface:" + state + ":" + due + ":" + period, () => iface!.Change(new TimeSpan(due), new TimeSpan(period)).ToString());
        }
        Exception? saved = null;
        try { new Timer(cb, null, long.MinValue, long.MaxValue); } catch (Exception ex) { saved = ex; }
        GC.Collect(); GC.WaitForPendingFinalizers();
        Fault("timer fields GC", saved!);

        for (int i = 0; i < 8; i++)
        {
            using var timer = new Timer(_ => { });
            using var start = new Barrier(3);
            Exception? first = null, second = null;
            var a = new Thread(() => { start.SignalAndWait(); try { timer.Dispose(); } catch (Exception ex) { first = ex; } });
            var b = new Thread(() => { start.SignalAndWait(); try { timer.Dispose(); } catch (Exception ex) { second = ex; } });
            a.Start(); b.Start(); start.SignalAndWait(); a.Join(); b.Join();
            Console.WriteLine("timer racing dispose:" + i + " success=" + (first is null && second is null));
        }
        using var stopped = new ManualResetEventSlim(false);
        Timer? self = null;
        self = new Timer(_ => { self!.Dispose(); stopped.Set(); });
        self.Change(0, Timeout.Infinite);
        stopped.Wait(); self.Dispose();
        Console.WriteLine("timer self dispose success=True");
        Console.WriteLine("Timer validation fields end");
    }

    private static void Fault(string label, Exception ex)
    {
        Console.WriteLine(label + " type=" + ex.GetType().Name);
        Console.WriteLine(label + " param=" + (ex is ArgumentException a ? a.ParamName : null));
        Console.WriteLine(label + " message=" + ex.Message.Replace("\r", "").Replace("\n", "|"));
        object? value = ex is ArgumentOutOfRangeException range ? range.ActualValue : null;
        Console.WriteLine(label + " actual=" + (value is null ? "null" : value.GetType().Name + ":" + value));
    }

    private static void O(string label, Func<string> action)
    {
        try
        {
            Console.WriteLine(label + " success=" + action());
        }
        catch (Exception ex)
        {
            Fault(label, ex);
        }
    }
}
