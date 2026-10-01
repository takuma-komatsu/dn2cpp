using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace TaskOriginSubset;

internal static class Program
{
    private static void Observe(string label, Task task)
    {
        try
        {
            task.RunSynchronously(TaskScheduler.Default);
            Console.WriteLine(label + " -> returned");
        }
        catch (InvalidOperationException error)
        {
            Console.WriteLine(label + " -> " + error.Message);
        }
    }

    private static async Task InlineSuccess() => await Task.CompletedTask;

    private static async Task InlineFault()
    {
        await Task.CompletedTask;
        throw new InvalidOperationException("inline marker");
    }

    private static async Task Suspended(Task input) => await input;

    private static async Task<int> InlineResult()
    {
        await Task.CompletedTask;
        return 17;
    }

    private static void Builders()
    {
        var before = AsyncTaskMethodBuilder.Create();
        before.SetResult();
        Observe("builder result before getter", before.Task);
        var after = AsyncTaskMethodBuilder.Create();
        Task exposed = after.Task;
        after.SetResult();
        Observe("builder result after getter", exposed);
        var canceled = AsyncTaskMethodBuilder.Create();
        canceled.SetException(new OperationCanceledException());
        Observe("builder cancel before getter", canceled.Task);
        var genericBefore = AsyncTaskMethodBuilder<int>.Create();
        genericBefore.SetResult(17);
        Observe("generic builder before getter", genericBefore.Task);
        var genericAfter = AsyncTaskMethodBuilder<int>.Create();
        Task<int> genericExposed = genericAfter.Task;
        genericAfter.SetResult(23);
        Observe("generic builder after getter", genericExposed);
    }

    private static void Joins()
    {
        var input = Task.FromResult(17);
        Task single = Task.WhenAll(new Task[] { input });
        Console.WriteLine("singleton identity/type: " + ReferenceEquals(single, input)
            + "," + (single.GetType() == typeof(Task<int>)));
        Observe("singleton result", single);
        Task covariant = Task.WhenAll(new Task<int>[] { input } as IEnumerable<Task>);
        Console.WriteLine("covariant singleton identity: " + ReferenceEquals(covariant, input));
        var pending = new TaskCompletionSource();
        Task singlePending = Task.WhenAll(new Task[] { pending.Task });
        Console.WriteLine("pending singleton identity: " + ReferenceEquals(singlePending, pending.Task));
        Observe("singleton pending", singlePending);
        pending.SetResult();
        Observe("singleton settled promise", singlePending);
        int body = 0;
        var cold = new Task(() => body++);
        Task singleCold = Task.WhenAll(new Task[] { cold });
        Console.WriteLine("cold singleton identity: " + ReferenceEquals(singleCold, cold));
        Observe("singleton cold", singleCold);
        Console.WriteLine("singleton cold body: " + body);
        Observe("all empty", Task.WhenAll(Array.Empty<Task>()));
        Observe("generic all empty", Task.WhenAll(Array.Empty<Task<int>>()));
        Observe("all pair", Task.WhenAll(new Task[] { Task.CompletedTask, input }));
        Observe("generic all singleton", Task.WhenAll(new[] { input }));
        Observe("any array pair", Task.WhenAny(new Task[] { Task.CompletedTask, input }));
        Observe("any list pair", Task.WhenAny((IEnumerable<Task>)new List<Task> { Task.CompletedTask, input }));
        Observe("any queue pair", Task.WhenAny((IEnumerable<Task>)new Queue<Task>(new Task[] { Task.CompletedTask, input })));
        Observe("any singleton", Task.WhenAny(new Task[] { input }));
        Observe("any triple", Task.WhenAny(new Task[] { input, input, input }));
        Observe("generic any pair", Task.WhenAny(new[] { input, input }));
        var first = new TaskCompletionSource();
        var second = new TaskCompletionSource();
        Task<Task> anyPending = Task.WhenAny(new[] { first.Task, second.Task });
        Observe("any pending pair", anyPending);
        first.SetResult();
        anyPending.GetAwaiter().GetResult();
        Observe("any settled pending pair", anyPending);
        second.SetResult();
    }

    internal static void __GateEntry()
    {
        Console.WriteLine("== task origin ==");
        Observe("completed", Task.CompletedTask);
        Observe("result", Task.FromResult(17));
        var canceled = new CancellationToken(true);
        Observe("canceled", Task.FromCanceled(canceled));
        Observe("faulted", Task.FromException(new InvalidOperationException("fault marker")));
        var result = new TaskCompletionSource<int>();
        result.SetResult(17);
        Observe("promise result", result.Task);
        var cancel = new TaskCompletionSource<int>();
        cancel.SetCanceled();
        Observe("promise canceled", cancel.Task);
        var fault = new TaskCompletionSource<int>();
        fault.SetException(new InvalidOperationException("promise marker"));
        Observe("promise faulted", fault.Task);
        Observe("inline async", InlineSuccess());
        Observe("inline async result", InlineResult());
        Observe("inline async fault", InlineFault());
        var release = new TaskCompletionSource();
        Task suspended = Suspended(release.Task);
        Observe("suspended async", suspended);
        release.SetResult();
        suspended.GetAwaiter().GetResult();
        Observe("settled suspended async", suspended);
        Observe("default value task", default(ValueTask).AsTask());
        Observe("result value task", new ValueTask<int>(17).AsTask());
        Observe("canceled value task", ValueTask.FromCanceled(canceled).AsTask());
        Observe("faulted value task", ValueTask.FromException(new InvalidOperationException("value marker")).AsTask());
        Observe("zero delay", Task.Delay(0));
        Observe("canceled zero delay", Task.Delay(0, canceled));
        Observe("canceled infinite delay", Task.Delay(-1, canceled));
        using var stop = new CancellationTokenSource();
        Task delay = Task.Delay(-1, stop.Token);
        Observe("pending delay", delay);
        stop.Cancel();
        Observe("later canceled delay", delay);
        Builders();
        Joins();
        Console.WriteLine("task origin end");
    }
}
