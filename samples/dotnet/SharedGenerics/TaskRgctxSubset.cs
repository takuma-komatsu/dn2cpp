#nullable enable
// Task producer identities inside generic methods. Non-async reference
// instantiations share one body through rgctx; async producers retain the same
// exact closed identities through their existing state-machine fallback.
using System;
using System.Threading.Tasks;
namespace TaskRgctxSubset;

sealed class Alpha { }
sealed class Beta { }

static class Ops
{
    public static Task<T> FromResult<T>(T value) => Task.FromResult(value);

    public static Task<T> ValueTaskResult<T>(T value) =>
        ValueTask.FromResult(value).AsTask();

    public static TaskCompletionSource<T> Source<T>() => new();

    public static async Task<T> AsyncTask<T>(T value)
    {
        await Task.Yield();
        return value;
    }

    public static async ValueTask<T> AsyncValueTask<T>(T value)
    {
        await Task.Yield();
        return value;
    }
}

static class Program
{
    internal static void __GateEntry()
    {
        Console.WriteLine("task rgctx from="
            + (Ops.FromResult(new Alpha()).GetType() == typeof(Task<Alpha>))
            + "," + (Ops.FromResult(new Beta()).GetType() == typeof(Task<Beta>)));
        Console.WriteLine("task rgctx value="
            + (Ops.ValueTaskResult(new Alpha()).GetType() == typeof(Task<Alpha>))
            + "," + (Ops.ValueTaskResult(new Beta()).GetType() == typeof(Task<Beta>)));
        var alphaSource = Ops.Source<Alpha>();
        var betaSource = Ops.Source<Beta>();
        Console.WriteLine("task rgctx source="
            + (alphaSource.GetType() == typeof(TaskCompletionSource<Alpha>)
                && alphaSource.Task.GetType() == typeof(Task<Alpha>))
            + "," + (betaSource.GetType() == typeof(TaskCompletionSource<Beta>)
                && betaSource.Task.GetType() == typeof(Task<Beta>)));
        alphaSource.SetResult(new Alpha());
        betaSource.SetResult(new Beta());
        object alphaAsync = Ops.AsyncTask(new Alpha());
        object betaAsync = Ops.AsyncTask(new Beta());
        Console.WriteLine("task rgctx async="
            + (alphaAsync is Task<Alpha> && alphaAsync is not Task<Beta>)
            + "," + (betaAsync is Task<Beta> && betaAsync is not Task<Alpha>));
        object alphaAsyncValue = Ops.AsyncValueTask(new Alpha()).AsTask();
        object betaAsyncValue = Ops.AsyncValueTask(new Beta()).AsTask();
        Console.WriteLine("task rgctx async-value="
            + (alphaAsyncValue is Task<Alpha> && alphaAsyncValue is not Task<Beta>)
            + "," + (betaAsyncValue is Task<Beta> && betaAsyncValue is not Task<Alpha>));
    }
}
