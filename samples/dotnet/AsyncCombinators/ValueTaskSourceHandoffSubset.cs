#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace ValueTaskSourceHandoffSubset;

// A pooled IValueTaskSource in ThreadPoolValueTaskSource's shape: its producer reports
// completion before it reads the continuation slot, and GetResult recycles the source for
// the next operation. A continuation registered with an operation the consumer then reads
// directly is found by that late producer and run against the next operation. So an
// await that finds the operation complete reads it once and registers nothing, an await
// that suspends registers once and is read once by its continuation, and AsTask over a
// completed operation reads it synchronously.
internal static class Program
{
    private static readonly TimeSpan Bound = TimeSpan.FromSeconds(10);

    private sealed class Producer
    {
        public readonly ManualResetEventSlim Reported = new ManualResetEventSlim(false);
        public readonly ManualResetEventSlim Proceed = new ManualResetEventSlim(false);
        public readonly ManualResetEventSlim Published = new ManualResetEventSlim(false);
        public string Waited = "none";
        public string Held = "none";
    }

    private sealed class PooledSource : IValueTaskSource<int>
    {
        private readonly object _gate = new object();
        private short _version;
        private volatile bool _completed;
        private bool _published;
        private int _result;
        private Action<object?>? _continuation;
        private object? _state;

        public int GetResultCalls;
        public int Refusals;
        public int Registrations;
        public int Invocations;
        public readonly ManualResetEventSlim Registered = new ManualResetEventSlim(false);

        public short Version => _version;

        public ValueTask<int> Begin() => new ValueTask<int>(this, _version);

        // Completes the current operation on a new thread once `before` (if any) is set,
        // then waits for `p.Proceed` and runs whatever continuation the slot holds then.
        public Thread Complete(int result, ManualResetEventSlim? before, Producer p)
        {
            var worker = new Thread(() =>
            {
                if (before is not null)
                    p.Waited = before.Wait(Bound) ? "registered" : "timed out";
                _result = result;
                _completed = true;
                p.Reported.Set();
                p.Held = p.Proceed.Wait(Bound) ? "released" : "timed out";
                Action<object?>? continuation;
                object? state;
                lock (_gate)
                {
                    continuation = _continuation;
                    state = _state;
                    _continuation = null;
                    _state = null;
                    _published = true;
                }
                p.Published.Set();
                if (continuation is not null)
                {
                    Interlocked.Increment(ref Invocations);
                    continuation(state);
                }
            });
            worker.Start();
            return worker;
        }

        public ValueTaskSourceStatus GetStatus(short token)
        {
            Validate(token);
            return _completed ? ValueTaskSourceStatus.Succeeded : ValueTaskSourceStatus.Pending;
        }

        public int GetResult(short token)
        {
            Interlocked.Increment(ref GetResultCalls);
            lock (_gate)
            {
                if (token != _version || !_completed)
                {
                    Refusals++;
                    throw new InvalidOperationException(
                        token != _version ? "stale token" : "operation not completed");
                }
                int result = _result;
                _completed = false;
                _published = false;
                _continuation = null;
                _state = null;
                _version++;
                return result;
            }
        }

        public void OnCompleted(Action<object?> continuation, object? state, short token,
            ValueTaskSourceOnCompletedFlags flags)
        {
            bool runNow;
            lock (_gate)
            {
                Validate(token);
                Registrations++;
                runNow = _published;
                if (!runNow)
                {
                    _continuation = continuation;
                    _state = state;
                }
            }
            Registered.Set();
            if (runNow)
            {
                Interlocked.Increment(ref Invocations);
                continuation(state);
            }
        }

        private void Validate(short token)
        {
            if (token != _version)
                throw new InvalidOperationException("stale token");
        }
    }

    private static async Task RunAsync()
    {
        var source = new PooledSource();

        var p1 = new Producer();
        ValueTask<int> op1 = source.Begin();
        Thread w1 = source.Complete(41, null, p1);
        bool reported1 = p1.Reported.Wait(Bound);
        int value1 = await op1;
        Console.WriteLine($"op1 reported={reported1} value={value1}");

        // The next operation starts on the recycled source while op1's producer has yet
        // to read the continuation slot.
        var p2 = new Producer();
        ValueTask<int> op2 = source.Begin();
        p1.Proceed.Set();
        bool joined1 = w1.Join(Bound);
        Console.WriteLine($"op1 producer={p1.Held} joined={joined1}");
        p2.Proceed.Set();
        Thread w2 = source.Complete(42, null, p2);
        bool joined2 = w2.Join(Bound);
        string value2;
        try
        {
            value2 = (await op2).ToString();
        }
        catch (InvalidOperationException e)
        {
            value2 = "fault:" + e.Message;
        }
        Console.WriteLine($"op2 producer={p2.Held} joined={joined2} value={value2}");

        var p3 = new Producer();
        p3.Proceed.Set();
        source.Registered.Reset();
        ValueTask<int> op3 = source.Begin();
        source.Complete(43, source.Registered, p3);
        int value3 = await op3;
        Console.WriteLine($"op3 waited={p3.Waited} value={value3}");

        var p4 = new Producer();
        p4.Proceed.Set();
        ValueTask<int> op4 = source.Begin();
        Thread w4 = source.Complete(44, null, p4);
        bool published4 = p4.Published.Wait(Bound);
        Task<int> task4 = op4.AsTask();
        Console.WriteLine($"op4 published={published4} completed={task4.IsCompleted} value={task4.Result}");
        w4.Join(Bound);

        Console.WriteLine($"counts: getresult={source.GetResultCalls} refused={source.Refusals} "
            + $"registered={source.Registrations} invoked={source.Invocations} version={source.Version}");
    }

    internal static void __GateEntry()
    {
        Console.WriteLine("== value task source handoff ==");
        RunAsync().GetAwaiter().GetResult();
        Console.WriteLine("value task source handoff end");
    }

    // OnCompleted stores the continuation before it decides to throw, so a rejected
    // registration leaves a continuation the completing caller still runs. That run is
    // the orphan of the failed AsTask: it reads the source once more (a stale token makes
    // the status read throw into the caller) and must not settle the task a later read
    // produced, nor end a registration it never held, which would leave the next
    // registration's waiter without a settler.
    private sealed class RejectingSource : IValueTaskSource<int>
    {
        private readonly object _gate = new object();
        private short _version;
        private bool _completed;
        private bool _rejectNext;
        private int _result;
        private Action<object?>? _continuation;
        private object? _state;

        public int GetResultCalls;
        public int Refusals;
        public int Registrations;

        public short Version => _version;

        public ValueTask<int> Begin(bool rejectRegistration)
        {
            lock (_gate)
                _rejectNext = rejectRegistration;
            return new ValueTask<int>(this, _version);
        }

        // Completes the current operation and takes the continuation the slot holds.
        public Action<object?>? Complete(int result, out object? state)
        {
            lock (_gate)
            {
                _result = result;
                _completed = true;
                Action<object?>? continuation = _continuation;
                state = _state;
                _continuation = null;
                _state = null;
                return continuation;
            }
        }

        public ValueTaskSourceStatus GetStatus(short token)
        {
            lock (_gate)
            {
                Validate(token);
                return _completed ? ValueTaskSourceStatus.Succeeded : ValueTaskSourceStatus.Pending;
            }
        }

        public int GetResult(short token)
        {
            Interlocked.Increment(ref GetResultCalls);
            lock (_gate)
            {
                if (token != _version || !_completed)
                {
                    Refusals++;
                    throw new InvalidOperationException(
                        token != _version ? "stale token" : "operation not completed");
                }
                int result = _result;
                _completed = false;
                _continuation = null;
                _state = null;
                _version++;
                return result;
            }
        }

        public void OnCompleted(Action<object?> continuation, object? state, short token,
            ValueTaskSourceOnCompletedFlags flags)
        {
            bool reject;
            lock (_gate)
            {
                Validate(token);
                Registrations++;
                _continuation = continuation;
                _state = state;
                reject = _rejectNext;
                _rejectNext = false;
            }
            if (reject)
                throw new InvalidOperationException("registration rejected");
        }

        private void Validate(short token)
        {
            if (token != _version)
                throw new InvalidOperationException("stale token");
        }
    }

    private static string Outcome(Func<object> read)
    {
        try
        {
            return "ok:" + read();
        }
        catch (Exception e)
        {
            return e.GetType().Name + ":" + e.Message;
        }
    }

    private static string Invoke(Action<object?>? continuation, object? state)
    {
        if (continuation is null)
            return "none";
        try
        {
            continuation(state);
            return "returned";
        }
        catch (Exception e)
        {
            return e.GetType().Name + ":" + e.Message;
        }
    }

    internal static void RunRejectedRegistration()
    {
        Console.WriteLine("== value task source rejected registration ==");
        var source = new RejectingSource();

        // A direct read consumes the operation, then the orphan runs.
        ValueTask<int> op5 = source.Begin(rejectRegistration: true);
        Console.WriteLine("op5 as-task=" + Outcome(() => op5.AsTask().Status));
        Action<object?>? orphan5 = source.Complete(45, out object? state5);
        Console.WriteLine($"op5 stored={orphan5 is not null} read={Outcome(() => op5.Result)}");
        Console.WriteLine("op5 orphan=" + Invoke(orphan5, state5));

        // A thread the program started that blocks on the next operation sees its
        // registration as the only settler; the pause lets that waiter park first.
        ValueTask<int> op6 = source.Begin(rejectRegistration: false);
        var armed = new ManualResetEventSlim(false);
        string value6 = "none";
        var waiter = new Thread(() =>
        {
            Task<int> task6 = op6.AsTask();
            armed.Set();
            try
            {
                value6 = task6.Result.ToString();
            }
            catch (Exception e)
            {
                value6 = "fault:" + e.GetType().Name;
            }
        });
        waiter.Start();
        bool armed6 = armed.Wait(Bound);
        Thread.Sleep(50);
        Action<object?>? continuation6 = source.Complete(46, out object? state6);
        string ran6 = Invoke(continuation6, state6);
        bool joined6 = waiter.Join(Bound);
        Console.WriteLine($"op6 armed={armed6} continuation={ran6} joined={joined6} value={value6}");

        // AsTask over the completed operation reads it on the spot, then the orphan runs.
        ValueTask<int> op7 = source.Begin(rejectRegistration: true);
        Console.WriteLine("op7 as-task=" + Outcome(() => op7.AsTask().Status));
        Action<object?>? orphan7 = source.Complete(47, out object? state7);
        Task<int> task7 = op7.AsTask();
        Console.WriteLine("op7 orphan=" + Invoke(orphan7, state7));
        Console.WriteLine($"op7 status={task7.Status} result={Outcome(() => task7.Result)}");

        // The orphan runs first and consumes the operation, as on .NET.
        ValueTask<int> op8 = source.Begin(rejectRegistration: true);
        Console.WriteLine("op8 as-task=" + Outcome(() => op8.AsTask().Status));
        Action<object?>? orphan8 = source.Complete(48, out object? state8);
        Console.WriteLine("op8 orphan=" + Invoke(orphan8, state8));
        Console.WriteLine("op8 completed=" + Outcome(() => op8.IsCompleted));

        Console.WriteLine($"rejection counts: getresult={source.GetResultCalls} "
            + $"refused={source.Refusals} registered={source.Registrations} version={source.Version}");
        Console.WriteLine("value task source rejected registration end");
    }
}
