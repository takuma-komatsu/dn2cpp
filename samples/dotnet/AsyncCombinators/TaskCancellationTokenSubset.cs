using System;
using System.Threading;
using System.Threading.Tasks;

namespace TaskCancellationTokenSubset
{
    internal static class Program
    {
        private static CancellationToken Token(Task task) => new TaskCanceledException(task).CancellationToken;
        private static void Show(string name, bool value) => Console.WriteLine(name + ": " + value);
        private static CancellationToken Caught(Task task)
        {
            try { task.GetAwaiter().GetResult(); }
            catch (OperationCanceledException error) { return error.CancellationToken; }
            return default;
        }
        private static async Task<int> AwaitTask(Task<int> task) => await task;
        private static async Task ThrowCanceled(CancellationToken token)
        {
            await Task.Yield();
            throw new OperationCanceledException(token);
        }

        private static OperationCanceledException Error(Task task)
        {
            try { task.GetAwaiter().GetResult(); }
            catch (OperationCanceledException error) { return error; }
            throw new Exception("Expected cancellation");
        }

        private static async Task<int> ThrowAfter(Task gate, Exception error)
        {
            await gate;
            throw error;
        }

        internal static void RunFreshFilter(bool generic)
        {
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(default, default);
            var token = linked.Token;
            Task filtered = generic
                ? Task.CompletedTask.ContinueWith(_ => 1, token,
                    TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Default)
                : Task.CompletedTask.ContinueWith(_ => { }, token,
                    TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Default);
            // Observe settlement after awaiting, as in the independent fresh-process
            // witness. Native continuations run on the next scheduler turn.
            var error = Error(filtered);
            Show("fresh filter", filtered.IsCanceled && error is TaskCanceledException
                && error.CancellationToken == token);
        }

        private static void CheckCaptured(string name, OperationCanceledException error, CancellationToken token)
        {
            var gate = new TaskCompletionSource<int>();
            var task = ThrowAfter(gate.Task, error);
            var wait = task.WaitAsync(token);
            var typed = Task.WhenAll(task, Task.FromResult(1));
            var untyped = Task.WhenAll(new Task[] { task, Task.CompletedTask });
            gate.SetResult(1);
            var unwrap = Task.Run(() => task);
            Show(name, ReferenceEquals(error, Error(task)) && ReferenceEquals(error, Error(task))
                && ReferenceEquals(error, Error(wait)) && ReferenceEquals(error, Error(typed))
                && ReferenceEquals(error, Error(untyped)) && ReferenceEquals(error, Error(unwrap))
                && Token(typed) == error.CancellationToken);
        }

        internal static void RunFromTask()
        {
            var value = new object();
            var result = new TaskCompletionSource<object>();
            Func<Task<object>, bool> transfer = result.TrySetFromTask;
            Show("TCS from result", transfer(Task.FromResult(value))
                && ReferenceEquals(value, result.Task.Result) && !transfer(result.Task));

            var first = new Exception("first");
            var second = new Exception("second");
            var fault = new TaskCompletionSource();
            fault.SetFromTask(Task.WhenAll(Task.FromException(first), Task.FromException(second)));
            var errors = fault.Task.Exception.InnerExceptions;
            Show("TCS from faults", errors.Count == 2 && ReferenceEquals(first, errors[0])
                && ReferenceEquals(second, errors[1]));

            var error = new OperationCanceledException(new CancellationToken(true));
            var canceled = new TaskCompletionSource<int>();
            canceled.SetFromTask(ThrowAfter(Task.CompletedTask, error));
            var synthetic = Task.FromCanceled<int>(error.CancellationToken);
            var copy = new TaskCompletionSource<int>();
            copy.SetFromTask(synthetic);
            Show("TCS from cancellation", ReferenceEquals(error, Error(canceled.Task))
                && Token(canceled.Task) == error.CancellationToken
                && ReferenceEquals(((TaskCanceledException)Error(copy.Task)).Task, copy.Task));

            try { fault.TrySetFromTask(new TaskCompletionSource().Task); }
            catch (ArgumentException ex) { Console.WriteLine("TCS from pending: " + ex.ParamName); }
        }

        internal static void Run()
        {
            Console.WriteLine("== task cancellation tokens ==");
            using var first = new CancellationTokenSource();
            using var second = new CancellationTokenSource();
            var a = first.Token;
            var b = second.Token;
            var cold = new Task(() => { }, a);
            Show("cold token", Token(cold) == a);
            cold.RunSynchronously();
            Show("successful token", Token(cold) == a);
            var fault = new Task(() => throw new Exception("fault"), b);
            fault.RunSynchronously();
            Show("faulted token", Token(fault) == b);
            Show("default token", Token(Task.CompletedTask) == CancellationToken.None);

            var source = new TaskCompletionSource<int>();
            Show("promise pending token", Token(source.Task) == CancellationToken.None);
            source.SetCanceled(a);
            Show("promise cancellation token", Token(source.Task) == a && Caught(source.Task) == a);
            Show("promise losing token", !source.TrySetCanceled(b) && Token(source.Task) == a);
            var successful = new TaskCompletionSource<int>();
            successful.SetResult(1);
            Show("promise success token", !successful.TrySetCanceled(a)
                && Token(successful.Task) == CancellationToken.None);

            var left = new TaskCompletionSource<int>();
            var right = new TaskCompletionSource<int>();
            Task untyped = Task.WhenAll(new Task[] { left.Task, right.Task });
            Task typed = Task.WhenAll(left.Task, right.Task);
            right.SetCanceled(b);
            left.SetCanceled(a);
            Show("join completion token", Token(untyped) == b && Caught(untyped) == b);
            Show("join input token", Token(typed) == a && Caught(typed) == a);

            Show("synthetic cancellation identity", !ReferenceEquals(Error(left.Task), Error(left.Task))
                && !ReferenceEquals(Error(left.Task), Error(typed))
                && !ReferenceEquals(Error(right.Task), Error(untyped)));
            var synthetic = new TaskCompletionSource<int>();
            var syntheticWait = synthetic.Task.WaitAsync(a);
            synthetic.SetCanceled(b);
            var syntheticUnwrap = Task.Run(() => synthetic.Task);
            Show("synthetic transfer identity", !ReferenceEquals(Error(synthetic.Task), Error(syntheticWait))
                && !ReferenceEquals(Error(synthetic.Task), Error(syntheticUnwrap))
                && Token(syntheticWait) == b && Token(syntheticUnwrap) == b);
            CheckCaptured("captured OCE identity", new OperationCanceledException(b), a);
            CheckCaptured("captured TCE identity", new TaskCanceledException(left.Task), b);

            var pending = new TaskCompletionSource<int>();
            var wait = pending.Task.WaitAsync(a);
            var delay = Task.Delay(Timeout.Infinite, b);
            Show("registered pending tokens", Token(wait) == CancellationToken.None
                && Token(delay) == CancellationToken.None);
            first.Cancel();
            second.Cancel();
            Show("wait token", Token(wait) == a && Caught(wait) == a);
            Show("delay token", Token(delay) == b && Caught(delay) == b);
            Show("from canceled token", Token(Task.FromCanceled<int>(a)) == a);
            Show("value task token", Token(ValueTask.FromCanceled<int>(b).AsTask()) == b);
            Show("builder token", Token(AwaitTask(source.Task)) == a);

            using var creator = new CancellationTokenSource();
            var run = Task.Run(() => 1, creator.Token);
            run.Wait();
            Show("run creator token", Token(run) == creator.Token);
            var factory = Task.Factory.StartNew(() => 2, creator.Token);
            factory.Wait();
            Show("factory creator token", Token(factory) == creator.Token);
            var continuation = Task.CompletedTask.ContinueWith(_ => 3, creator.Token);
            continuation.Wait();
            Show("continuation creator token", Token(continuation) == creator.Token);
            var filtered = Task.CompletedTask.ContinueWith(_ => { }, creator.Token,
                TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Default);
            Show("filtered token", Caught(filtered) == creator.Token && Token(filtered) == creator.Token);
            var thrown = ThrowCanceled(creator.Token);
            Show("builder thrown token", Caught(thrown) == creator.Token && Token(thrown) == creator.Token);
            var unwrap = Task.Run(() => source.Task, creator.Token);
            Show("unwrap inner token", Caught(unwrap) == a && Token(unwrap) == a);

            int calls = 0;
            var canceledCold = new Task(() => calls++, a);
            var canceledRun = Task.Run(() => { calls++; }, a);
            var canceledUnwrap = Task.Run(() => { calls++; return Task.CompletedTask; }, a);
            var canceledFactory = Task.Factory.StartNew(() => { calls++; }, a);
            Caught(canceledRun);
            Caught(canceledUnwrap);
            Caught(canceledFactory);
            Show("pre-canceled tasks", canceledCold.IsCanceled && canceledRun.IsCanceled
                && canceledUnwrap.IsCanceled && canceledFactory.IsCanceled && calls == 0);
            Show("pre-canceled tokens", Token(canceledCold) == a && Token(canceledRun) == a
                && Token(canceledUnwrap) == a && Token(canceledFactory) == a);
            try { canceledCold.Start(); }
            catch (InvalidOperationException) { Console.WriteLine("pre-canceled start: True"); }
            Console.WriteLine("task cancellation tokens end");
        }
    }
}
