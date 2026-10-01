using System;
using System.Threading;
using System.Threading.Tasks;

namespace ColdTaskScheduleSubset;

internal static class Program
{
    private static void Fault(string label, Action action)
    {
        try
        {
            action();
            Console.WriteLine(label + " -> returned");
        }
        catch (Exception ex)
        {
            string param = ex is ArgumentException arg ? " [" + arg.ParamName + "]" : "";
            Console.WriteLine(label + " -> " + ex.GetType().Name + ": " + ex.Message + param);
        }
    }

    internal static void __GateEntry()
    {
        Console.WriteLine("== cold task scheduler ==");
        Console.WriteLine("scheduler: " + (TaskScheduler.Default is not null) + "," +
            (TaskScheduler.Current is not null) + "," +
            ReferenceEquals(TaskScheduler.Default, TaskScheduler.Current));

        var cold = new Task(() => { });
        Fault("cold Start(null)", () => cold.Start(null!));
        Fault("cold RunSynchronously(null)", () => cold.RunSynchronously(null!));
        cold.RunSynchronously();
        Fault("completed Start(null)", () => cold.Start(null!));
        Fault("completed RunSynchronously(null)", () => cold.RunSynchronously(null!));

        var promise = new TaskCompletionSource();
        Fault("promise Start(null)", () => promise.Task.Start(null!));
        Fault("promise Start(default)", () => promise.Task.Start(TaskScheduler.Default));
        Fault("promise RunSynchronously(default)", () => promise.Task.RunSynchronously(TaskScheduler.Default));

        var continuation = promise.Task.ContinueWith(_ => { });
        Fault("continuation Start(default)", () => continuation.Start(TaskScheduler.Default));
        Fault("continuation RunSynchronously(default)", () => continuation.RunSynchronously(TaskScheduler.Default));
        promise.SetResult();
        continuation.Wait();

        var pending = new TaskCompletionSource();
        Task started = Task.Run(() => pending.Task.Wait());
        Fault("started Start(default)", () => started.Start(TaskScheduler.Default));
        Fault("started RunSynchronously(default)", () => started.RunSynchronously(TaskScheduler.Default));
        pending.SetResult();
        started.Wait();

        var inner = new TaskCompletionSource();
        Task unwrap = Task.Run((Func<Task>)(() => inner.Task));
        Fault("unwrap Start(default)", () => unwrap.Start(TaskScheduler.Default));
        Fault("unwrap RunSynchronously(default)", () => unwrap.RunSynchronously(TaskScheduler.Default));
        inner.SetResult();
        unwrap.Wait();

        Fault("StartNew(action, null)", () => Task.Factory.StartNew(
            () => { }, CancellationToken.None, TaskCreationOptions.None, null!));
        Fault("StartNew(null action, null)", () => Task.Factory.StartNew(
            (Action)null!, CancellationToken.None, TaskCreationOptions.None, null!));
        Fault("StartNew<int>(null)", () => Task.Factory.StartNew(
            () => 1, CancellationToken.None, TaskCreationOptions.None, null!));
        Fault("StartNew<int>(null function, null scheduler)", () => Task.Factory.StartNew<int>(
            (Func<int>)null!, CancellationToken.None, TaskCreationOptions.None, null!));
        Fault("StartNew<int>(null function, state, null scheduler)", () => Task.Factory.StartNew<int>(
            (Func<object?, int>)null!, null, CancellationToken.None, TaskCreationOptions.None, null!));
        Fault("ContinueWith(action, null)", () => Task.CompletedTask.ContinueWith(
            _ => { }, CancellationToken.None, TaskContinuationOptions.None, null!));
        Fault("ContinueWith(null action, null)", () => Task.CompletedTask.ContinueWith(
            (Action<Task>)null!, CancellationToken.None, TaskContinuationOptions.None, null!));
        Fault("ContinueWith<int>(function, null)", () => Task.CompletedTask.ContinueWith(
            _ => 1, CancellationToken.None, TaskContinuationOptions.None, null!));
        Console.WriteLine("cold task scheduler end");
    }
}
