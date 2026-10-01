using System;
using System.Threading.Tasks;

namespace TaskDurationValidationSubset;

internal static class Program
{
    internal static void __GateEntry()
    {
        Console.WriteLine("== delay arguments ==");
        var canceled = new System.Threading.CancellationToken(true);
        Arg("delay(-2)", () => Task.Delay(-2));
        Arg("delay(-2, canceled)", () => Task.Delay(-2, canceled));
        Arg("delay(-2ms)", () => Task.Delay(TimeSpan.FromMilliseconds(-2)));
        Arg("delay(past max timer)", () => Task.Delay(TimeSpan.FromMilliseconds(4294967295.0)));
        Arg("delay(0) completed", () => Task.Delay(0).IsCompleted);
        Arg("delay(zero span) completed", () => Task.Delay(TimeSpan.Zero).IsCompleted);
        Arg("delay(0, canceled)", () => Task.Delay(0, canceled).Status);
        var forever = Task.Delay(-1);
        var foreverSpan = Task.Delay(System.Threading.Timeout.InfiniteTimeSpan);
        using var cts = new System.Threading.CancellationTokenSource();
        var untilCanceled = Task.Delay(-1, cts.Token);
        Task.Delay(5).Wait();
        Console.WriteLine("delay(-1) after a later delay: " + forever.IsCompleted + ","
            + foreverSpan.IsCompleted + "," + untilCanceled.IsCompleted);
        cts.Cancel();
        Console.WriteLine("delay(-1, token) after cancel: " + untilCanceled.Status);
        Arg("wait(-2ms)", () => Task.CompletedTask.Wait(TimeSpan.FromMilliseconds(-2)));
        Arg("wait(past Int32)", () => Task.CompletedTask.Wait(TimeSpan.FromMilliseconds(int.MaxValue + 1.0)));
        Arg("wait(-1.5ms)", () => Task.CompletedTask.Wait(TimeSpan.FromMilliseconds(-1.5)));
        long[] ticks = { -20000, -19999, -15000, -10000, -9999, 0, 9999,
            42949672940000, 42949672949999, 42949672950000, long.MinValue, long.MaxValue };
        for (int i = 0; i < ticks.Length; i++)
        {
            long value = ticks[i];
            Arg("delay ticks " + value, () => Task.Delay(TimeSpan.FromTicks(value), canceled).Status);
        }
        long[] waitTicks = { -20000, -19999, -15000, -9999, 0,
            21474836470000, 21474836479999, 21474836480000, long.MinValue, long.MaxValue };
        for (int i = 0; i < waitTicks.Length; i++)
        {
            long value = waitTicks[i];
            Arg("wait ticks " + value, () => Task.CompletedTask.Wait(TimeSpan.FromTicks(value)));
        }
        Console.WriteLine("delay arguments end");
    }

    private static void Arg(string tag, Func<object> f)
    {
        try { Console.WriteLine(tag + ": " + f()); }
        catch (ArgumentException ex)
        {
            GC.Collect();
            string actual = ex is ArgumentOutOfRangeException range
                ? (range.ActualValue is null ? "null" : range.ActualValue.GetType().Name)
                : "null";
            Console.WriteLine(tag + ": " + ex.GetType().Name + ": " + ex.Message
                + " [param=" + ex.ParamName + ", actual=" + actual + "]");
        }
    }
}
