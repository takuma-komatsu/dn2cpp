using System;
using System.Threading;
using System.Threading.Tasks;

namespace TaskReceiverValidationSubset;

internal static class Program
{
    private static Task s_task = null;
    private static Task<int> s_intTask = null;
    private static TaskCompletionSource<int> s_source = null;
    private static TaskCompletionSource s_voidSource = null;

    private struct Pair
    {
        public int X;
    }

    private static void Fault(string label, Func<object> run)
    {
        string text;
        try
        {
            object result = run();
            text = result is null ? "null" : result.ToString();
        }
        catch (Exception ex)
        {
            string param = ex is ArgumentException arg ? " [" + arg.ParamName + "]" : "";
            text = ex.GetType().Name + ": " + ex.Message + param;
        }
        Console.WriteLine(label + " -> " + text);
    }

    private static void Run(string label, Action run) => Fault(label, () =>
    {
        run();
        return "returned";
    });

    internal static void __GateEntry()
    {
        Console.WriteLine("== task receivers ==");
        TimeSpan m2 = TimeSpan.FromMilliseconds(-2);
        TimeSpan big = TimeSpan.FromMilliseconds(int.MaxValue + 1.0);
        CancellationToken none = CancellationToken.None;

        Run("Task.Wait()", () => s_task.Wait());
        Fault("Task.Wait(-2ms)", () => s_task.Wait(m2));
        Fault("Task.Wait(past Int32)", () => s_task.Wait(big));
        Fault("Task<int>.Result", () => s_intTask.Result);
        Run("Task.GetAwaiter()", () => s_task.GetAwaiter());
        Run("Task.ConfigureAwait(false)", () => s_task.ConfigureAwait(false));
        Fault("Task.IsCompleted", () => s_task.IsCompleted);
        Fault("Task.IsCompletedSuccessfully", () => s_task.IsCompletedSuccessfully);
        Fault("Task.IsFaulted", () => s_task.IsFaulted);
        Fault("Task.IsCanceled", () => s_task.IsCanceled);
        Fault("Task.Id", () => s_task.Id);
        Fault("Task.Exception", () => s_task.Exception);
        Fault("Task.Status", () => s_task.Status);
        Run("Task.Start()", () => s_task.Start());
        Run("Task.Start(null)", () => s_task.Start(null));
        Run("Task.RunSynchronously()", () => s_task.RunSynchronously());
        Run("Task.RunSynchronously(null)", () => s_task.RunSynchronously(null));
        Fault("Task.ContinueWith(action)", () => s_task.ContinueWith(_ => { }));
        Fault("Task.ContinueWith(null)", () => s_task.ContinueWith((Action<Task>)null));
        Fault("Task.ContinueWith(action, state)", () => s_task.ContinueWith((_, _) => { }, 1));
        Fault("Task.ContinueWith<int>(function)", () => s_task.ContinueWith(_ => 1));
        Fault("Task.ContinueWith<Pair>(function)", () => s_task.ContinueWith(_ => new Pair { X = 1 }));
        Fault("Task.WaitAsync(token)", () => s_task.WaitAsync(none));
        Run("Task.Dispose()", () => s_task.Dispose());

        Fault("TaskCompletionSource<int>.Task", () => s_source.Task);
        Run("TaskCompletionSource<int>.SetResult(1)", () => s_source.SetResult(1));
        Fault("TaskCompletionSource<int>.TrySetResult(1)", () => s_source.TrySetResult(1));
        Run("TaskCompletionSource<int>.SetException(null)", () => s_source.SetException((Exception)null));
        Fault("TaskCompletionSource<int>.TrySetException(null)",
            () => s_source.TrySetException((Exception)null));
        Run("TaskCompletionSource<int>.SetCanceled()", () => s_source.SetCanceled());
        Fault("TaskCompletionSource<int>.TrySetCanceled()", () => s_source.TrySetCanceled());
        Fault("TaskCompletionSource.Task", () => s_voidSource.Task);
        Run("TaskCompletionSource.SetResult()", () => s_voidSource.SetResult());

        Task done = Task.CompletedTask;
        Fault("ContinueWith(null)", () => done.ContinueWith((Action<Task>)null));
        Fault("ContinueWith(null, state)", () => done.ContinueWith((Action<Task, object>)null, 1));
        Fault("ContinueWith<int>(null)", () => done.ContinueWith((Func<Task, int>)null));
        Fault("ContinueWith<Pair>(null)", () => done.ContinueWith((Func<Task, Pair>)null));
        Fault("Task<int>.ContinueWith(null)", () => Task.FromResult(1).ContinueWith((Action<Task<int>>)null));
        Fault("ContinueWith(null, null scheduler)",
            () => done.ContinueWith((Action<Task>)null, (TaskScheduler)null));
        Fault("ContinueWith<int>(null, null scheduler)",
            () => done.ContinueWith((Func<Task, int>)null, (TaskScheduler)null));
        var source = new TaskCompletionSource<int>();
        Run("SetException(null)", () => source.SetException((Exception)null));
        Fault("TrySetException(null)", () => source.TrySetException((Exception)null));
        var voidSource = new TaskCompletionSource();
        Run("void SetException(null)", () => voidSource.SetException((Exception)null));
        Console.WriteLine("sources still pending: " + source.Task.Status + " " + voidSource.Task.Status);
        Console.WriteLine("task receivers end");
    }
}
