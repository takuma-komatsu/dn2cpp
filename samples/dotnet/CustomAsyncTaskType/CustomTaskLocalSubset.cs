// A SECOND custom async task type, declared in the APP assembly rather than a library.
//
// This section is not optional. A MethodSpec's parent resolves down a different arm for a
// MethodDef/TypeDef (same assembly) than for a MemberRef/TypeRef (cross assembly), and the
// builder's Start<TSM> / AwaitUnsafeOnCompleted<TAwaiter,TSM> arrive as MethodSpecs. A fix
// that handled only the cross-assembly arm would pass a gate that only tested the other, and
// vice versa — so both are here.
//
// Deliberately different from the library's in two ways that must both work:
//   * its CompletedTask is a PROPERTY (the library's is a static field, GDTask's shape);
//   * it has no pool at all — a task-like type does not need one to be adopted.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;

namespace CustomTaskLocalSubset;

[AsyncMethodBuilder(typeof(LocalTaskBuilder))]
internal readonly struct LocalTask
{
    private readonly LocalPromise _promise;

    internal LocalTask(LocalPromise promise) => _promise = promise;

    internal static LocalTask CompletedTask => default;

    internal Awaiter GetAwaiter() => new Awaiter(_promise);

    internal readonly struct Awaiter : ICriticalNotifyCompletion
    {
        private readonly LocalPromise _promise;

        internal Awaiter(LocalPromise promise) => _promise = promise;

        public bool IsCompleted => _promise is null || _promise.IsCompleted;

        public void GetResult() => _promise?.GetResult();

        public void OnCompleted(Action continuation) => UnsafeOnCompleted(continuation);

        public void UnsafeOnCompleted(Action continuation)
        {
            if (_promise is null)
                continuation();
            else
                _promise.OnCompleted(continuation);
        }
    }
}

internal sealed class LocalPromise
{
    private readonly object _lock = new object();

    private Action _continuation;
    private Exception _error;
    private bool _completed;

    internal Action MoveNext { get; set; }

    internal bool IsCompleted
    {
        get { lock (_lock) return _completed; }
    }

    internal void SetResult() => Complete(null);

    internal void SetException(Exception error) => Complete(error);

    // Completing and registering must not race: the awaiter checks IsCompleted and only then
    // registers, so a promise that completes on the thread pool in between would otherwise
    // fire a null continuation and drop the caller's for good.
    private void Complete(Exception error)
    {
        Action c;
        lock (_lock)
        {
            _error = error;
            _completed = true;
            c = _continuation;
            _continuation = null;
        }
        c?.Invoke();
    }

    internal void OnCompleted(Action continuation)
    {
        bool alreadyDone;
        lock (_lock)
        {
            alreadyDone = _completed;
            if (!alreadyDone)
                _continuation = continuation;
        }
        if (alreadyDone)
            continuation();
    }

    internal void GetResult()
    {
        Exception error;
        lock (_lock)
            error = _error;
        if (error is not null)
            ExceptionDispatchInfo.Capture(error).Throw();
    }
}

internal struct LocalTaskBuilder
{
    private LocalPromise _promise;

    public static LocalTaskBuilder Create() => default;

    public LocalTask Task => _promise is null ? LocalTask.CompletedTask : new LocalTask(_promise);

    public void SetResult() => _promise?.SetResult();

    public void SetException(Exception exception)
    {
        _promise ??= new LocalPromise();
        _promise.SetException(exception);
    }

    public void SetStateMachine(IAsyncStateMachine stateMachine)
    {
    }

    public void Start<TStateMachine>(ref TStateMachine stateMachine)
        where TStateMachine : IAsyncStateMachine
        => stateMachine.MoveNext();

    public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
        where TAwaiter : INotifyCompletion
        where TStateMachine : IAsyncStateMachine
    {
        var box = Box(ref stateMachine);
        awaiter.OnCompleted(box);
    }

    public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
        where TAwaiter : ICriticalNotifyCompletion
        where TStateMachine : IAsyncStateMachine
    {
        var box = Box(ref stateMachine);
        awaiter.UnsafeOnCompleted(box);
    }

    private Action Box<TStateMachine>(ref TStateMachine stateMachine)
        where TStateMachine : IAsyncStateMachine
    {
        _promise ??= new LocalPromise();
        if (_promise.MoveNext is null)
        {
            IAsyncStateMachine boxed = stateMachine; // heap-box once, on first suspension
            _promise.MoveNext = boxed.MoveNext;
        }
        return _promise.MoveNext;
    }
}

internal static class Program
{
    private static async LocalTask NopLocal()
    {
        await LocalTask.CompletedTask;
        Console.WriteLine("local: awaited a completed app-declared custom task");
    }

    private static async LocalTask SuspendLocal(int n)
    {
        await Task.Yield();
        Console.WriteLine($"local: resumed after yield ({n})");
    }

    private static async LocalTask ThrowLocal(string message)
    {
        await Task.Yield();
        throw new InvalidOperationException(message);
    }

    private static async Task Run()
    {
        await NopLocal();
        await SuspendLocal(1);
        await SuspendLocal(2);

        try
        {
            await ThrowLocal("local-boom");
        }
        catch (InvalidOperationException e)
        {
            Console.WriteLine($"local: caught {e.Message}");
        }

        Console.WriteLine("local: done");
    }

    internal static void __GateEntry()
    {
        Run().GetAwaiter().GetResult();
    }
}
