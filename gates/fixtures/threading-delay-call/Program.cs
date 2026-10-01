using System.Globalization;
using System.Runtime.CompilerServices;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
foreach (int due in new[] { -2, -1, 0 })
    foreach (int period in new[] { -2, -1, 0 })
    {
        Show("timer direct int:" + due + ":" + period, () => Calls.TimerInt(null!, due, period).ToString());
        Show("timer virtual int:" + due + ":" + period, () => ((Timer)null!).Change(due, period).ToString());
    }
foreach (long due in new[] { -2L, -1, 4294967295 })
    foreach (long period in new[] { -2L, -1, 4294967295 })
    {
        Show("timer direct long:" + due + ":" + period, () => Calls.TimerLong(null!, due, period).ToString());
        Show("timer virtual long:" + due + ":" + period, () => ((Timer)null!).Change(due, period).ToString());
    }
foreach (TimeSpan due in new[] { TimeSpan.MinValue, Timeout.InfiniteTimeSpan, TimeSpan.MaxValue })
    foreach (TimeSpan period in new[] { TimeSpan.MinValue, Timeout.InfiniteTimeSpan, TimeSpan.MaxValue })
    {
        Show("timer direct span:" + due.Ticks + ":" + period.Ticks, () => Calls.TimerSpan(null!, due, period).ToString());
        Show("timer virtual span:" + due.Ticks + ":" + period.Ticks, () => ((Timer)null!).Change(due, period).ToString());
    }
foreach (int delay in new[] { -2, -1, 0, int.MaxValue })
{
    Show("cts direct int:" + delay, () => { Calls.CtsInt(null!, delay); return "armed"; });
    Show("cts virtual int:" + delay, () => { ((CancellationTokenSource)null!).CancelAfter(delay); return "armed"; });
}
foreach (TimeSpan delay in new[] { TimeSpan.MinValue, Timeout.InfiniteTimeSpan, TimeSpan.Zero, TimeSpan.MaxValue })
{
    Show("cts direct span:" + delay.Ticks, () => { Calls.CtsSpan(null!, delay); return "armed"; });
    Show("cts virtual span:" + delay.Ticks, () => { ((CancellationTokenSource)null!).CancelAfter(delay); return "armed"; });
}
foreach (int degree in new[] { int.MinValue, -2, -1, 0, 1, int.MaxValue })
{
    Show("parallel direct:" + degree, () => { Calls.Parallel(null!, degree); return "stored"; });
    Show("parallel virtual:" + degree, () => { ((System.Threading.Tasks.ParallelOptions)null!).MaxDegreeOfParallelism = degree; return "stored"; });
}
Console.WriteLine("Threading delay call faults end");

static void Show(string label, Func<string> action)
{
    try
    {
        Console.WriteLine(label + " success=" + action());
    }
    catch (Exception ex)
    {
        Console.WriteLine(label + " type=" + ex.GetType().Name);
        Console.WriteLine(label + " param=" + (ex is ArgumentException a ? a.ParamName : null));
        Console.WriteLine(label + " message=" + ex.Message.Replace("\r", "").Replace("\n", "|"));
        object? value = ex is ArgumentOutOfRangeException range ? range.ActualValue : null;
        Console.WriteLine(label + " actual=" + (value is null ? "null" : value.GetType().Name + ":" + value));
    }
}

static class Calls
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static bool TimerInt(Timer receiver, int due, int period) => receiver.Change(due, period);
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static bool TimerLong(Timer receiver, long due, long period) => receiver.Change(due, period);
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static bool TimerSpan(Timer receiver, TimeSpan due, TimeSpan period) => receiver.Change(due, period);
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void CtsInt(CancellationTokenSource receiver, int delay) => receiver.CancelAfter(delay);
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void CtsSpan(CancellationTokenSource receiver, TimeSpan delay) => receiver.CancelAfter(delay);
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void Parallel(System.Threading.Tasks.ParallelOptions receiver, int degree) => receiver.MaxDegreeOfParallelism = degree;
}
