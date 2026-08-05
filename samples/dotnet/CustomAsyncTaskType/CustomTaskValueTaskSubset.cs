// The cross-await matrix from inside an `async CustomTask`: a pending ValueTask<T>
// (suspends through ValueTaskAwaiter), a completed default(ValueTask) (the NULL-task
// encoding — must not suspend), and both again through ConfigureAwait(false)
// (ConfiguredTaskAwaiter / ConfiguredValueTaskAwaiter). Deterministic: every await is
// sequential and clock-free (Task.Yield only).

using System;
using System.Threading.Tasks;
using CustomAsyncTaskLib;

namespace CustomTaskValueTaskSubset;

internal static class Program
{
    private static async Task Run()
    {
        // 21 -> *2 = 42 -> +1 = 43 -> *2 = 86, across four awaiter shapes.
        int r = await CustomTaskApi.ValueTaskRoundTrip(21);
        Console.WriteLine($"valuetask: {r}");

        Console.WriteLine("valuetask: done");
    }

    internal static void __GateEntry()
    {
        Run().GetAwaiter().GetResult();
    }
}
