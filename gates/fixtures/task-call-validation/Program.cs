using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

internal static class Program
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool DirectWait(Task task, TimeSpan timeout) => task.Wait(timeout);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Task DirectAction(Task task, Action<Task> continuation) => task.ContinueWith(continuation);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Task<int> DirectFunction(Task task, Func<Task, int> continuation) => task.ContinueWith(continuation);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void DirectException(TaskCompletionSource<int> source, Exception error) => source.SetException(error);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool DirectTryException(TaskCompletionSource<int> source, Exception error) => source.TrySetException(error);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static TaskAwaiter DirectAwaiter(Task task) => task.GetAwaiter();

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ConfiguredTaskAwaitable DirectConfigure(Task task, bool capture) => task.ConfigureAwait(capture);

    private static string Field(string? text)
    {
        if (text is null)
            return "<null>";
        return text.Replace("\r", "\\r").Replace("\n", "\\n");
    }

    private static void Observe(string tag, Func<object?> body)
    {
        try { Console.WriteLine(tag + "|ok:" + Field(body()?.ToString())); }
        catch (Exception error)
        {
            var argument = error as ArgumentException;
            var range = error as ArgumentOutOfRangeException;
            object? actual = range?.ActualValue;
            Console.WriteLine(tag + "|fault:" + error.GetType().Name
                + "|param:" + Field(argument?.ParamName)
                + "|actual:" + Field(actual?.ToString())
                + "|actualType:" + Field(actual?.GetType().Name)
                + "|message:" + Field(error.Message));
        }
    }

    private static void Main()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("== task direct-call validation ==");
        Task? task = null;
        TaskCompletionSource<int>? source = null;
        TimeSpan invalid = TimeSpan.FromTicks(-20000);
        TimeSpan pastInt32 = TimeSpan.FromTicks(21474836480000L);
        TimeSpan zero = TimeSpan.Zero;
        Action<Task>? action = null;
        Func<Task, int>? function = null;
        Exception? exception = null;

        Observe("direct-wait-negative-null", () => DirectWait(task!, invalid));
        Observe("virtual-wait-negative-null", () => task!.Wait(invalid));
        Observe("direct-wait-past-int32-null", () => DirectWait(task!, pastInt32));
        Observe("virtual-wait-past-int32-null", () => task!.Wait(pastInt32));
        Observe("direct-wait-zero-null", () => DirectWait(task!, zero));
        Observe("virtual-wait-zero-null", () => task!.Wait(zero));
        Observe("direct-action-null-null", () => DirectAction(task!, action!));
        Observe("virtual-action-null-null", () => task!.ContinueWith(action!));
        Observe("direct-function-null-null", () => DirectFunction(task!, function!));
        Observe("virtual-function-null-null", () => task!.ContinueWith(function!));
        Observe("direct-tcs-null-null", () => { DirectException(source!, exception!); return "returned"; });
        Observe("virtual-tcs-null-null", () => { source!.SetException(exception!); return "returned"; });
        Observe("direct-tcs-try-null-null", () => DirectTryException(source!, exception!));
        Observe("virtual-tcs-try-null-null", () => source!.TrySetException(exception!));
        Observe("direct-get-awaiter-null", () => { DirectAwaiter(task!); return "constructed"; });
        Observe("virtual-get-awaiter-null", () => { task!.GetAwaiter(); return "constructed"; });
        Observe("direct-configure-null", () => { DirectConfigure(task!, false); return "constructed"; });
        Observe("virtual-configure-null", () => { task!.ConfigureAwait(false); return "constructed"; });

        action = static _ => { };
        function = static _ => 17;
        exception = new InvalidOperationException("exception marker");
        Observe("direct-action-null-present", () => DirectAction(task!, action));
        Observe("virtual-action-null-present", () => task!.ContinueWith(action));
        Observe("direct-function-null-present", () => DirectFunction(task!, function));
        Observe("virtual-function-null-present", () => task!.ContinueWith(function));
        Observe("direct-tcs-null-present", () => { DirectException(source!, exception); return "returned"; });
        Observe("virtual-tcs-null-present", () => { source!.SetException(exception); return "returned"; });
        Observe("direct-tcs-try-null-present", () => DirectTryException(source!, exception));
        Observe("virtual-tcs-try-null-present", () => source!.TrySetException(exception));
        Console.WriteLine("task direct-call validation end");
    }
}
