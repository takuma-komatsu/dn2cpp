using System;
using System.Threading;

internal static class CancellationDelayValidationSubset
{
    public static void __GateEntry()
    {
        Console.WriteLine("== Cancellation delay fields ==");
        int[] ints = { int.MinValue, -2, -1, 0, 1, int.MaxValue };
        long[] ticks = { long.MinValue, -20000, -19999, -10000, -9999, -1, 0, 1, 10000, 42949672940000L, 42949672950000L, 9223372036854769999L, -9223372036854769999L, long.MaxValue };
        foreach (int delay in ints)
        {
            O("cts int:" + delay, () => { using var s = new CancellationTokenSource(delay); return "created"; });
            O("cts null int:" + delay, () => { ((CancellationTokenSource)null!).CancelAfter(delay); return "cancelled"; });
            using var s = new CancellationTokenSource();
            O("cts after int:" + delay, () => { s.CancelAfter(delay); return "armed"; });
        }
        foreach (long tick in ticks)
        {
            TimeSpan delay = new TimeSpan(tick);
            O("cts span:" + tick, () => { using var s = new CancellationTokenSource(delay); return "created"; });
            O("cts null span:" + tick, () => { ((CancellationTokenSource)null!).CancelAfter(delay); return "cancelled"; });
            using var s = new CancellationTokenSource();
            O("cts after span:" + tick, () => { s.CancelAfter(delay); return "armed"; });
        }
        for (int state = 0; state < 2; state++)
        {
            using var source = new CancellationTokenSource();
            if (state == 0)
                source.Dispose();
            else
                source.Cancel();
            foreach (int delay in new[] { -2, -1, 0 })
                O("cts state int:" + state + ":" + delay, () => { source.CancelAfter(delay); return "armed"; });
            foreach (TimeSpan delay in new[] { TimeSpan.MinValue, Timeout.InfiniteTimeSpan, TimeSpan.Zero, TimeSpan.MaxValue })
                O("cts state span:" + state + ":" + delay.Ticks, () => { source.CancelAfter(delay); return "armed"; });
        }
        Exception? saved = null;
        try { new CancellationTokenSource(-2); } catch (Exception ex) { saved = ex; }
        GC.Collect(); GC.WaitForPendingFinalizers();
        Fault("cancellation delay fields GC", saved!);
        Console.WriteLine("Cancellation delay fields end");
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
