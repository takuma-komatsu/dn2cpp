using System;
using System.Threading;
using System.Threading.Tasks;

namespace TaskCreationOptionsSubset
{
    internal static class Program
    {
        private static void Show(string label, Task task) =>
            Console.WriteLine(label + ": " + (int)task.CreationOptions);

        private static async Task<int> AwaitSource(Task<int> source) => await source;

        private static async Task<int> WorkerYield()
        {
            await Task.Yield();
            await Task.Delay(1);
            return 31;
        }

        public static void Run()
        {
            Console.WriteLine("== task creation options ==");
            Show("completed", Task.CompletedTask);
            Show("cached result", Task.FromResult(1));
            Show("uncached result", Task.FromResult(12345));
            Show("default value task", default(ValueTask).AsTask());
            Show("value task result", new ValueTask<int>(12345).AsTask());
            Show("fault", Task.FromException(new Exception("options")));
            Show("canceled", Task.FromCanceled(new CancellationToken(true)));
            var source = new TaskCompletionSource<int>("state", TaskCreationOptions.RunContinuationsAsynchronously);
            Show("source pending", source.Task);
            source.SetResult(7);
            Show("source complete", source.Task);
            Show("source void", new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously).Task);
            var cold = new Task(() => { }, TaskCreationOptions.DenyChildAttach | TaskCreationOptions.HideScheduler);
            Show("cold", cold);
            cold.RunSynchronously();
            Show("cold started", cold);
            var coldState = new Task<int>(state => (int)state, 2, TaskCreationOptions.LongRunning);
            Show("cold state", coldState);
            coldState.RunSynchronously();
            var run = Task.Run(() => 7);
            run.Wait();
            Show("run", run);
            var unwrap = Task.Run(() => Task.FromResult(7));
            unwrap.Wait();
            Show("run unwrap", unwrap);
            var nested = Task.Run<Task<int>>(() => source.Task);
            nested.Wait();
            Show("run nested", nested);
            Console.WriteLine("run nested identity: " + ReferenceEquals(nested.Result, source.Task));
            var suspendedNested = Task.Run<Task<int>>(WorkerYield);
            Console.WriteLine("run nested worker: " + suspendedNested.Result.Result);
            Show("run nested worker options", suspendedNested);
            var factory = Task.Factory.StartNew(() => 7, CancellationToken.None,
                TaskCreationOptions.PreferFairness | TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
            factory.Wait();
            Show("factory", factory);
            var factoryState = Task.Factory.StartNew(state => { }, null, CancellationToken.None,
                TaskCreationOptions.RunContinuationsAsynchronously, TaskScheduler.Default);
            factoryState.Wait();
            Show("factory state", factoryState);
            var pending = new TaskCompletionSource<int>();
            var asyncTask = AwaitSource(pending.Task);
            Show("async pending", asyncTask);
            pending.SetResult(3);
            asyncTask.Wait();
            Show("async complete", asyncTask);
            var continuation = source.Task.ContinueWith(_ => { },
                TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.DenyChildAttach);
            continuation.Wait();
            Show("continuation", continuation);
            Show("when all empty", Task.WhenAll(Array.Empty<Task>()));
            Show("when all", Task.WhenAll(new Task[] { source.Task }));
            Show("when all generic", Task.WhenAll(source.Task));
            Show("when any", Task.WhenAny(source.Task));
            Show("delay zero", Task.Delay(0));
            using (var cancellation = new CancellationTokenSource())
            {
                var delay = Task.Delay(Timeout.Infinite, cancellation.Token);
                Show("delay pending", delay);
                cancellation.Cancel();
                Show("delay canceled", delay);
            }
            using (var cancellation = new CancellationTokenSource())
            {
                // A zero delay has synchronously finished all initialization; waiting
                // for a timed delay's status does not guarantee timer cleanup finished.
                var delay = Task.Delay(0, cancellation.Token);
                cancellation.Cancel();
                Show("delay completed then canceled", delay);
            }
            using (var semaphore = new SemaphoreSlim(0))
            {
                var waiter = semaphore.WaitAsync();
                Show("semaphore pending", waiter);
                semaphore.Release();
                waiter.Wait();
                Show("semaphore complete", waiter);
            }
            using (var cancellation = new CancellationTokenSource())
            using (var semaphore = new SemaphoreSlim(0))
            {
                var waiter = semaphore.WaitAsync(cancellation.Token);
                Show("semaphore cancelable", waiter);
                semaphore.Release();
                waiter.Wait();
            }
            using (var cancellation = new CancellationTokenSource())
            {
                var pendingWait = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
                var wrapper = pendingWait.Task.WaitAsync(cancellation.Token);
                Show("wait wrapper", wrapper);
                cancellation.Cancel();
                Show("wait canceled", wrapper);
            }
            Show("wait identity", source.Task.WaitAsync(Timeout.InfiniteTimeSpan));
            var pendingIdentity = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            Task basePending = pendingIdentity.Task;
            Task pendingForwarded = basePending.WaitAsync(CancellationToken.None);
            Console.WriteLine("wait pending identity: " + ReferenceEquals(basePending, pendingForwarded));
            Show("wait pending options", pendingForwarded);
            pendingIdentity.SetResult(1);
            Task baseCompleted = pendingIdentity.Task;
            Task completedForwarded = baseCompleted.WaitAsync(new CancellationToken(true));
            Console.WriteLine("wait completed identity: " + ReferenceEquals(baseCompleted, completedForwarded));
            Show("wait completed options", completedForwarded);
            try { Task missing = null; _ = missing.CreationOptions; }
            catch (NullReferenceException) { Console.WriteLine("options null: True"); }
            Console.WriteLine("task creation options end");
        }
    }
}
