#nullable enable
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace RegistrationRejectionSubset;

// A continuation registration that throws. Called explicitly (awaiter.OnCompleted, AsTask)
// it throws to its caller. At an await suspension the BCL builders catch it and re-raise it
// through Task.ThrowAsync instead: the suspended method's own catch never runs, its task
// stays pending, and the process ends as for an unhandled ThreadPool exception. So each
// suspension mode is the last thing its process does, and nothing is written between the
// suspension and the wait, which the re-raise races.
internal static class Program
{
    private sealed class RejectingSource : IValueTaskSource<int>, IValueTaskSource
    {
        public int Registrations;

        public ValueTaskSourceStatus GetStatus(short token) => ValueTaskSourceStatus.Pending;

        public int GetResult(short token) => throw new InvalidOperationException("not complete");

        void IValueTaskSource.GetResult(short token) => throw new InvalidOperationException("not complete");

        public void OnCompleted(Action<object?> continuation, object? state, short token,
            ValueTaskSourceOnCompletedFlags flags)
        {
            Registrations++;
            Console.WriteLine("source OnCompleted #" + Registrations);
            throw new InvalidOperationException("source rejected the continuation");
        }
    }

    private struct RejectingAwaiter : ICriticalNotifyCompletion
    {
        public bool IsCompleted => false;

        public int GetResult() => 0;

        public void OnCompleted(Action continuation)
        {
            Console.WriteLine("awaiter OnCompleted");
            throw new InvalidOperationException("awaiter rejected the continuation");
        }

        public void UnsafeOnCompleted(Action continuation)
        {
            Console.WriteLine("awaiter UnsafeOnCompleted");
            throw new InvalidOperationException("awaiter rejected the continuation");
        }
    }

    private struct RejectingAwaitable
    {
        public RejectingAwaiter GetAwaiter() => default;
    }

    private static readonly RejectingSource s_source = new RejectingSource();

    private static string Describe(Action register)
    {
        try
        {
            register();
            return "returned";
        }
        catch (Exception e)
        {
            return e.GetType().Name + ":" + e.Message;
        }
    }

    internal static void __GateEntry()
    {
        Console.WriteLine("== continuation registration rejection ==");
        Console.WriteLine("OnCompleted " + Describe(() =>
            new ValueTask<int>(s_source, 0).GetAwaiter().OnCompleted(() => Console.WriteLine("BUG: ran"))));
        Console.WriteLine("UnsafeOnCompleted " + Describe(() =>
            new ValueTask<int>(s_source, 0).GetAwaiter().UnsafeOnCompleted(() => Console.WriteLine("BUG: ran"))));
        Console.WriteLine("configured OnCompleted " + Describe(() =>
            new ValueTask<int>(s_source, 0).ConfigureAwait(false).GetAwaiter()
                .OnCompleted(() => Console.WriteLine("BUG: ran"))));
        Console.WriteLine("non-generic OnCompleted " + Describe(() =>
            new ValueTask(s_source, 0).GetAwaiter().OnCompleted(() => Console.WriteLine("BUG: ran"))));
        Console.WriteLine("AsTask " + Describe(() => new ValueTask<int>(s_source, 0).AsTask()));
        Console.WriteLine("registrations=" + s_source.Registrations);
        Console.WriteLine("continuation registration rejection end");
    }

    private static async Task<int> AwaitSource()
    {
        try
        {
            return await new ValueTask<int>(s_source, 0);
        }
        catch (Exception e)
        {
            Console.WriteLine("BUG: the suspended method caught " + e.GetType().Name);
            return -1;
        }
    }

    private static async Task<int> AwaitCustom()
    {
        try
        {
            return await new RejectingAwaitable();
        }
        catch (Exception e)
        {
            Console.WriteLine("BUG: the suspended method caught " + e.GetType().Name);
            return -1;
        }
    }

    private static async void AwaitSourceVoid()
    {
        try
        {
            await new ValueTask(s_source, 0);
        }
        catch (Exception e)
        {
            Console.WriteLine("BUG: the suspended method caught " + e.GetType().Name);
        }
    }

    // On .NET the wait outlives the process.
    internal static void Suspend(string mode)
    {
        Console.WriteLine("== builder suspension rejection: " + mode + " ==");
        Task<int> task;
        switch (mode)
        {
            case "value-task-source":
                task = AwaitSource();
                break;
            case "custom-awaiter":
                task = AwaitCustom();
                break;
            case "async-void":
                AwaitSourceVoid();
                task = new TaskCompletionSource<int>().Task;
                break;
            default:
                throw new ArgumentException("unknown suspension mode " + mode);
        }
        task.Wait();
        Console.WriteLine("BUG: the suspended method completed with " + task.Result);
    }
}
