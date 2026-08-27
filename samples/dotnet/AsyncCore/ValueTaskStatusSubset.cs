using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace ValueTaskStatusSubset
{
    internal static class Program
    {
        private const short Token = unchecked((short)0x8123);

        private static void RequireToken(short token)
        {
            if (token != Token)
                throw new InvalidOperationException("value task source token");
        }

        private sealed class StatusSource : IValueTaskSource
        {
            private ValueTaskSourceStatus _status;
            private Action<object> _continuation;
            private object _state;
            private bool _signalRequested;

            public int GetResultCount { get; private set; }

            public void SetStatus(ValueTaskSourceStatus status)
            {
                _status = status;
            }

            public void Signal()
            {
                if (_continuation is null)
                {
                    _signalRequested = true;
                    return;
                }
                _continuation(_state);
            }

            public ValueTaskSourceStatus GetStatus(short token)
            {
                RequireToken(token);
                return _status;
            }

            public void GetResult(short token)
            {
                RequireToken(token);
                GetResultCount++;
                if (_status == ValueTaskSourceStatus.Faulted)
                    throw new InvalidOperationException("source fault");
                if (_status == ValueTaskSourceStatus.Canceled)
                    throw new OperationCanceledException();
            }

            public void OnCompleted(Action<object> continuation, object state,
                short token, ValueTaskSourceOnCompletedFlags flags)
            {
                RequireToken(token);
                _continuation = continuation;
                _state = state;
                if (_signalRequested)
                    continuation(state);
            }
        }

        private sealed class StatusSource<T> : IValueTaskSource<T>
        {
            private ValueTaskSourceStatus _status;
            private Action<object> _continuation;
            private object _state;
            private bool _signalRequested;

            public int GetResultCount { get; private set; }

            public void SetStatus(ValueTaskSourceStatus status)
            {
                _status = status;
            }

            public void Signal()
            {
                if (_continuation is null)
                {
                    _signalRequested = true;
                    return;
                }
                _continuation(_state);
            }

            public ValueTaskSourceStatus GetStatus(short token)
            {
                RequireToken(token);
                return _status;
            }

            public T GetResult(short token)
            {
                RequireToken(token);
                GetResultCount++;
                if (_status == ValueTaskSourceStatus.Faulted)
                    throw new InvalidOperationException("source fault");
                if (_status == ValueTaskSourceStatus.Canceled)
                    throw new OperationCanceledException();
                return default(T);
            }

            public void OnCompleted(Action<object> continuation, object state,
                short token, ValueTaskSourceOnCompletedFlags flags)
            {
                RequireToken(token);
                _continuation = continuation;
                _state = state;
                if (_signalRequested)
                    continuation(state);
            }
        }

        private static void Print(string label, ValueTask task)
        {
            Console.WriteLine($"{label}: completed={task.IsCompleted} ok={task.IsCompletedSuccessfully} faulted={task.IsFaulted} canceled={task.IsCanceled}");
        }

        private static void Print<T>(string label, ValueTask<T> task)
        {
            Console.WriteLine($"{label}: completed={task.IsCompleted} ok={task.IsCompletedSuccessfully} faulted={task.IsFaulted} canceled={task.IsCanceled}");
        }

        private static void Print(string label, Task task)
        {
            Console.WriteLine($"{label}: completed={task.IsCompleted} ok={task.IsCompletedSuccessfully} faulted={task.IsFaulted} canceled={task.IsCanceled}");
        }

        private static void Print<T>(string label, Task<T> task)
        {
            Console.WriteLine($"{label}: completed={task.IsCompleted} ok={task.IsCompletedSuccessfully} faulted={task.IsFaulted} canceled={task.IsCanceled}");
        }

        private static async ValueTask CancelAsync()
        {
            await Task.CompletedTask;
            throw new OperationCanceledException();
        }

        private static async ValueTask<int> CancelAsyncT()
        {
            await Task.CompletedTask;
            throw new OperationCanceledException();
        }

        private static async Task CancelTaskAsync()
        {
            await Task.CompletedTask;
            throw new OperationCanceledException();
        }

        private static async Task<int> CancelTaskAsyncT()
        {
            await Task.CompletedTask;
            throw new OperationCanceledException();
        }

        private static string ObserveFault(ValueTask task)
        {
            try
            {
                task.GetAwaiter().GetResult();
                return "none";
            }
            catch (InvalidOperationException)
            {
                return "fault";
            }
        }

        private static string ObserveFault<T>(ValueTask<T> task)
        {
            try
            {
                task.GetAwaiter().GetResult();
                return "none";
            }
            catch (InvalidOperationException)
            {
                return "fault";
            }
        }

        private static string ObserveCanceled(ValueTask task)
        {
            try
            {
                task.GetAwaiter().GetResult();
                return "none";
            }
            catch (OperationCanceledException)
            {
                return "canceled";
            }
        }

        private static string ObserveCanceled<T>(ValueTask<T> task)
        {
            try
            {
                task.GetAwaiter().GetResult();
                return "none";
            }
            catch (OperationCanceledException)
            {
                return "canceled";
            }
        }

        private static void PrintSourceTransitions()
        {
            var faulted = new StatusSource();
            var faultedT = new StatusSource<int>();
            var canceled = new StatusSource();
            var canceledT = new StatusSource<int>();
            var faultedTask = new ValueTask(faulted, Token);
            var faultedTaskT = new ValueTask<int>(faultedT, Token);
            var canceledTask = new ValueTask(canceled, Token);
            var canceledTaskT = new ValueTask<int>(canceledT, Token);

            Print("source-faulted-pending", faultedTask);
            Print("source-faulted-t-pending", faultedTaskT);
            faulted.SetStatus(ValueTaskSourceStatus.Faulted);
            faultedT.SetStatus(ValueTaskSourceStatus.Faulted);
            Print("source-faulted-status", faultedTask);
            Print("source-faulted-t-status", faultedTaskT);
            faulted.Signal();
            faultedT.Signal();
            Print("source-faulted-signaled", faultedTask);
            Print("source-faulted-t-signaled", faultedTaskT);

            Print("source-canceled-pending", canceledTask);
            Print("source-canceled-t-pending", canceledTaskT);
            canceled.SetStatus(ValueTaskSourceStatus.Canceled);
            canceledT.SetStatus(ValueTaskSourceStatus.Canceled);
            Print("source-canceled-status", canceledTask);
            Print("source-canceled-t-status", canceledTaskT);
            canceled.Signal();
            canceledT.Signal();
            Print("source-canceled-signaled", canceledTask);
            Print("source-canceled-t-signaled", canceledTaskT);
        }

        private static void PrintSingleConsumption()
        {
            var faulted = new StatusSource();
            var faultedT = new StatusSource<int>();
            var canceled = new StatusSource();
            var canceledT = new StatusSource<int>();
            var faultedTask = new ValueTask(faulted, Token);
            var faultedTaskT = new ValueTask<int>(faultedT, Token);
            var canceledTask = new ValueTask(canceled, Token);
            var canceledTaskT = new ValueTask<int>(canceledT, Token);

            faulted.SetStatus(ValueTaskSourceStatus.Faulted);
            faultedT.SetStatus(ValueTaskSourceStatus.Faulted);
            canceled.SetStatus(ValueTaskSourceStatus.Canceled);
            canceledT.SetStatus(ValueTaskSourceStatus.Canceled);
            string fault = ObserveFault(faultedTask);
            string faultT = ObserveFault(faultedTaskT);
            string cancel = ObserveCanceled(canceledTask);
            string cancelT = ObserveCanceled(canceledTaskT);

            // A delayed notification must not consume a result already claimed
            // through the terminal ValueTask's synchronous GetResult path.
            faulted.Signal();
            faultedT.Signal();
            canceled.Signal();
            canceledT.Signal();
            Console.WriteLine($"source-getresult: {fault},{faultT},{cancel},{cancelT} counts={faulted.GetResultCount},{faultedT.GetResultCount},{canceled.GetResultCount},{canceledT.GetResultCount}");
        }

        public static void __GateEntry()
        {
            CancellationToken canceledToken = new CancellationToken(true);

            Print("default", default(ValueTask));
            Print("default-t", default(ValueTask<int>));
            Print("completed", ValueTask.CompletedTask);
            Print("from-result", ValueTask.FromResult(3));
            Print("faulted", ValueTask.FromException(new InvalidOperationException("fault")));
            Print("faulted-t", ValueTask.FromException<int>(new InvalidOperationException("fault")));
            Print("canceled", ValueTask.FromCanceled(canceledToken));
            Print("canceled-t", ValueTask.FromCanceled<int>(canceledToken));

            var pending = new TaskCompletionSource<object>();
            var pendingT = new TaskCompletionSource<int>();
            var pendingTask = new ValueTask(pending.Task);
            var pendingTaskT = new ValueTask<int>(pendingT.Task);
            Print("task-pending", pendingTask);
            Print("task-pending-t", pendingTaskT);
            pending.SetResult(null);
            pendingT.SetResult(5);
            Print("task-completed", pendingTask);
            Print("task-completed-t", pendingTaskT);
            Print("task-faulted", new ValueTask(Task.FromException(new InvalidOperationException("fault"))));
            Print("task-faulted-t", new ValueTask<int>(Task.FromException<int>(new InvalidOperationException("fault"))));
            Print("task-canceled", new ValueTask(Task.FromCanceled(canceledToken)));
            Print("task-canceled-t", new ValueTask<int>(Task.FromCanceled<int>(canceledToken)));

            PrintSourceTransitions();
            PrintSingleConsumption();
            Print("async-canceled", CancelAsync());
            Print("async-canceled-t", CancelAsyncT());
            Print("async-task-canceled", CancelTaskAsync());
            Print("async-task-canceled-t", CancelTaskAsyncT());
            Console.WriteLine($"value-task-status token: {Token}");
            Console.WriteLine("value-task-status: ran");
        }
    }
}
