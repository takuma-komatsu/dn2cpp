using System;
using System.Threading.Tasks;

namespace CustomAsyncTaskLib;

/// <summary>The library's own internal async surface. Each of these is a compiler-generated
/// state machine of its own, and each therefore closes its own
/// <c>Runner&lt;TSM&gt;</c> + <c>TaskPool&lt;Runner&lt;TSM&gt;&gt;</c> +
/// <c>ITaskPoolNode&lt;Runner&lt;TSM&gt;&gt;</c> tower. This is the surface reachability has
/// to sweep, and the reason a real library like GDTask (which ships dozens of these) made the
/// unshielded transpile combinatorial before it even reached the runaway.</summary>
internal static class CustomTaskOps
{
    internal static async CustomTask NopCore() => await CustomTask.CompletedTask;

    internal static async CustomTask YieldCore() => await Task.Yield();

    internal static async CustomTask SeqCore(int a, int b)
    {
        await Task.Yield();
        Console.WriteLine($"seq {a}");
        await Task.Yield();
        Console.WriteLine($"seq {b}");
    }

    internal static async CustomTask<int> SumCore(int a, int b)
    {
        await Task.Yield();
        return a + b;
    }

    internal static async CustomTask<string> TagCore(string s)
    {
        await Task.Yield();
        return "<" + s + ">";
    }

    internal static async CustomTask ThrowCore(string message)
    {
        await Task.Yield();
        throw new InvalidOperationException(message);
    }

    internal static async CustomTask<int> ThrowResultCore(string message)
    {
        await Task.Yield();
        throw new InvalidOperationException(message);
    }

    /// <summary>A GENERIC async method: one state-machine template, one Runner tower per T.</summary>
    internal static async CustomTask<T> IdentityCore<T>(T value)
    {
        await Task.Yield();
        return value;
    }

    /// <summary>An `async CustomTask` awaiting ValueTask/ValueTask&lt;T&gt; — the cross-await
    /// direction whose suspension registers the builder's continuation through
    /// ValueTaskAwaiter.OnCompleted, plus the already-completed `default(ValueTask)`
    /// (the NULL-task encoding, which must not suspend). The ConfigureAwait forms route
    /// the same awaits through the Configured*Awaiter shapes.</summary>
    internal static async CustomTask<int> ValueTaskCore(int v)
    {
        int doubled = await PendingValueTask(v);                              // ValueTaskAwaiter<int>
        await default(ValueTask);                                             // completed, no suspend
        int viaTask = await PendingTask(doubled).ConfigureAwait(false);       // ConfiguredTaskAwaiter
        int viaVTask = await PendingValueTask(viaTask).ConfigureAwait(false); // ConfiguredValueTaskAwaiter
        return viaVTask;
    }

    private static async ValueTask<int> PendingValueTask(int v)
    {
        await Task.Yield();
        return v * 2;
    }

    private static async Task<int> PendingTask(int v)
    {
        await Task.Yield();
        return v + 1;
    }
}

/// <summary>The only surface the driver touches. Every entry is an `async CustomTask`, so the
/// driver reaches the library exclusively through the compiler's builder contract — the
/// contract dn2cpp adopts. (A hand-written factory on the task type would have no BCL
/// counterpart and would fail loud, correctly, at emit.)</summary>
public static class CustomTaskApi
{
    public static async CustomTask Nop() => await CustomTaskOps.NopCore();

    public static async CustomTask Yield() => await CustomTaskOps.YieldCore();

    public static async CustomTask Seq(int a, int b) => await CustomTaskOps.SeqCore(a, b);

    public static async CustomTask<int> Sum(int a, int b) => await CustomTaskOps.SumCore(a, b);

    public static async CustomTask<string> Tag(string s) => await CustomTaskOps.TagCore(s);

    public static async CustomTask Throw(string message) => await CustomTaskOps.ThrowCore(message);

    public static async CustomTask<int> ThrowResult(string message) =>
        await CustomTaskOps.ThrowResultCore(message);

    public static async CustomTask<int> IdentityInt(int value) =>
        await CustomTaskOps.IdentityCore(value);

    public static async CustomTask<string> IdentityString(string value) =>
        await CustomTaskOps.IdentityCore(value);

    /// <summary>Awaiting one custom task from inside another: the awaiter is the adopted
    /// CustomTask.Awaiter, so the suspension goes through the adopted resume path rather
    /// than the Task/Yield ones.</summary>
    public static async CustomTask<int> SumOfSums(int a, int b, int c, int d)
    {
        int left = await CustomTaskOps.SumCore(a, b);
        int right = await CustomTaskOps.SumCore(c, d);
        return left + right;
    }

    public static async CustomTask<int> ValueTaskRoundTrip(int v) =>
        await CustomTaskOps.ValueTaskCore(v);
}
