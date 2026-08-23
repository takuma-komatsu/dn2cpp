// The generic form, over several result types. Each T closes its own Runner<TSM, T> +
// TaskPool<Runner<TSM, T>> + ITaskPoolNode<Runner<TSM, T>> tower in the library — the
// per-state-machine fan-out that made an unshielded transpile combinatorial. Adopted, none
// of it is reached at all.

using System;
using System.Threading.Tasks;
using CustomAsyncTaskLib;

namespace CustomTaskGenericSubset;

internal struct Point
{
    public int X;
    public int Y;

    public override string ToString() => $"({X},{Y})";
}

internal static class Program
{
    // A generic async CustomTask<T> declared in the APP assembly.
    private static async CustomTask<T> Twice<T>(T value, Func<T, T> f)
    {
        await Task.Yield();
        return f(f(value));
    }

    private static async Task Run()
    {
        Console.WriteLine($"generic: int {await CustomTaskApi.IdentityInt(7)}");
        Console.WriteLine($"generic: string {await CustomTaskApi.IdentityString("seven")}");
        Console.WriteLine($"generic: twice-int {await Twice(3, static x => x * 2)}");
        Console.WriteLine($"generic: twice-string {await Twice("a", static s => s + "!")}");

        var p = await Twice(new Point { X = 1, Y = 2 },
            static q => new Point { X = q.X + 10, Y = q.Y + 10 });
        Console.WriteLine($"generic: struct {p}");

        object builderTask = Twice(5, static x => x).AsTask();
        Console.WriteLine("generic: builder task type "
            + (typeof(Task<int>).IsAssignableFrom(builderTask.GetType())
                && builderTask is Task<int> && builderTask is not Task<string>));
        object completedTask = CustomTask<int>.CompletedTask.AsTask();
        Console.WriteLine("generic: completed task type "
            + (typeof(Task<int>).IsAssignableFrom(completedTask.GetType())
                && completedTask is Task<int> && completedTask is not Task<string>));

        Console.WriteLine("generic: done");
    }

    internal static void __GateEntry()
    {
        Run().GetAwaiter().GetResult();
    }
}
