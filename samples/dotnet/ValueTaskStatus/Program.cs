using System;
using System.Threading;
using System.Threading.Tasks;

namespace ValueTaskStatus
{
    // Gate driver: the status properties of a completed ValueTask / ValueTask<T> —
    // IsCompleted, IsCompletedSuccessfully, IsFaulted, IsCanceled — over the three
    // terminal states. IsFaulted/IsCanceled are what a WhenAll-style combinator reads
    // before deciding whether to await, so a missing intrinsic blocks any library that
    // fans out ValueTasks (a message router publishing to several subscribers).
    internal static class Program
    {
        private static void Print(string label, ValueTask task)
        {
            Console.WriteLine($"{label}: completed={task.IsCompleted} ok={task.IsCompletedSuccessfully} faulted={task.IsFaulted} canceled={task.IsCanceled}");
        }

        private static void Print<T>(string label, ValueTask<T> task)
        {
            Console.WriteLine($"{label}: completed={task.IsCompleted} ok={task.IsCompletedSuccessfully} faulted={task.IsFaulted} canceled={task.IsCanceled}");
        }

        private static async ValueTask Throws()
        {
            await Task.CompletedTask;
            throw new InvalidOperationException("boom");
        }

        private static void Main()
        {
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            Print("default", default(ValueTask));
            Print("completed", ValueTask.CompletedTask);
            Print("fromResult", ValueTask.FromResult(3));
            Print("faulted", ValueTask.FromException(new InvalidOperationException("x")));
            Print("faultedT", ValueTask.FromException<int>(new InvalidOperationException("x")));
            Print("canceled", ValueTask.FromCanceled(cts.Token));
            Print("canceledT", ValueTask.FromCanceled<int>(cts.Token));
            Print("wrappedFaultedTask", new ValueTask(Task.FromException(new InvalidOperationException("y"))));
            Print("wrappedCanceledTask", new ValueTask<int>(Task.FromCanceled<int>(cts.Token)));

            ValueTask thrown = Throws();
            Print("asyncThrows", thrown);
            try
            {
                thrown.GetAwaiter().GetResult();
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"observed: {ex.Message}");
            }
        }
    }
}
