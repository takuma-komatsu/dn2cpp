using System;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;

namespace CustomAsyncTaskLib;

public interface IStateMachineRunnerPromise
{
    CustomTask Task { get; }
    Action MoveNext { get; }
    void SetResult();
    void SetException(Exception ex);
}

public interface IStateMachineRunnerPromise<T>
{
    CustomTask<T> Task { get; }
    Action MoveNext { get; }
    void SetResult(T result);
    void SetException(Exception ex);
}

/// <summary>A promise generic over the STATE MACHINE, which is
/// also its own pool node (<c>ITaskPoolNode&lt;Runner&lt;TStateMachine&gt;&gt;</c>). Without
/// adoption, the transpiler must close this — plus <c>TaskPool&lt;Runner&lt;TSM&gt;&gt;</c>,
/// plus the source interfaces — once per compiler-generated state machine in the program.</summary>
internal sealed class Runner<TStateMachine> : IStateMachineRunnerPromise, ICustomTaskSource,
                                              ITaskPoolNode<Runner<TStateMachine>>
    where TStateMachine : IAsyncStateMachine
{
    private readonly object _lock = new object();

    private Runner<TStateMachine> _nextNode;
    public ref Runner<TStateMachine> NextNode => ref _nextNode;

    private TStateMachine _stateMachine;
    private readonly Action _moveNext;
    private Action<object> _continuation;
    private object _continuationState;
    private CustomTaskStatus _status;
    private Exception _error;

    private Runner() => _moveNext = Run;

    public static void SetStateMachine(ref TStateMachine stateMachine,
                                       ref IStateMachineRunnerPromise runnerPromiseFieldRef)
    {
        if (!TaskPool<Runner<TStateMachine>>.TryPop(out var runner))
            runner = new Runner<TStateMachine>();
        CustomTaskScheduler.NoteRent(typeof(TStateMachine));
        runner._status = CustomTaskStatus.Pending;
        // Order matters. runnerPromiseFieldRef is a ref to the builder's field, and the
        // builder lives INSIDE the state machine — so the runner has to be published there
        // BEFORE the struct is copied onto the heap, or the copy resumes with a null promise,
        // rents a second runner on its next await, and the first one never completes.
        runnerPromiseFieldRef = runner;
        runner._stateMachine = stateMachine;
    }

    private void Run() => _stateMachine.MoveNext();

    public Action MoveNext => _moveNext;
    public CustomTask Task => new CustomTask(this, 0);

    public void SetResult() => Complete(CustomTaskStatus.Succeeded, null);

    public void SetException(Exception ex) => Complete(CustomTaskStatus.Faulted, ex);

    // Completing and registering must not race. The awaiter checks IsCompleted and only then
    // registers, so a promise that completes on the thread pool in between would otherwise
    // fire a null continuation and drop the caller's for good — a deadlock, and an
    // intermittent one, which is worse than a broken one.
    private void Complete(CustomTaskStatus status, Exception error)
    {
        Action<object> c;
        object s;
        lock (_lock)
        {
            _error = error;
            _status = status;
            c = _continuation;
            s = _continuationState;
            _continuation = null;
            _continuationState = null;
        }
        c?.Invoke(s);
    }

    public CustomTaskStatus GetStatus(short token)
    {
        lock (_lock)
            return _status;
    }

    public void OnCompleted(Action<object> continuation, object state, short token)
    {
        bool alreadyDone;
        lock (_lock)
        {
            alreadyDone = _status != CustomTaskStatus.Pending;
            if (!alreadyDone)
            {
                _continuation = continuation;
                _continuationState = state;
            }
        }
        if (alreadyDone)
            continuation(state);
    }

    public void GetResult(short token)
    {
        Exception error;
        CustomTaskStatus status;
        lock (_lock)
        {
            error = _error;
            status = _status;
            _stateMachine = default;
            _error = null;
            _status = CustomTaskStatus.Pending; // reset before the runner goes back on the free list
        }
        TaskPool<Runner<TStateMachine>>.TryPush(this);
        if (status == CustomTaskStatus.Faulted)
            ExceptionDispatchInfo.Capture(error).Throw();
    }
}

internal sealed class Runner<TStateMachine, T> : IStateMachineRunnerPromise<T>, ICustomTaskSource<T>,
                                                 ITaskPoolNode<Runner<TStateMachine, T>>
    where TStateMachine : IAsyncStateMachine
{
    private readonly object _lock = new object();

    private Runner<TStateMachine, T> _nextNode;
    public ref Runner<TStateMachine, T> NextNode => ref _nextNode;

    private TStateMachine _stateMachine;
    private readonly Action _moveNext;
    private Action<object> _continuation;
    private object _continuationState;
    private CustomTaskStatus _status;
    private Exception _error;
    private T _result;

    private Runner() => _moveNext = Run;

    public static void SetStateMachine(ref TStateMachine stateMachine,
                                       ref IStateMachineRunnerPromise<T> runnerPromiseFieldRef)
    {
        if (!TaskPool<Runner<TStateMachine, T>>.TryPop(out var runner))
            runner = new Runner<TStateMachine, T>();
        CustomTaskScheduler.NoteRent(typeof(TStateMachine));
        runner._status = CustomTaskStatus.Pending;
        runnerPromiseFieldRef = runner; // before the copy — see Runner<TStateMachine>
        runner._stateMachine = stateMachine;
    }

    private void Run() => _stateMachine.MoveNext();

    public Action MoveNext => _moveNext;
    public CustomTask<T> Task => new CustomTask<T>(this, 0);

    public void SetResult(T result)
    {
        lock (_lock)
            _result = result;
        Complete(CustomTaskStatus.Succeeded, null);
    }

    public void SetException(Exception ex) => Complete(CustomTaskStatus.Faulted, ex);

    // Completing and registering must not race. The awaiter checks IsCompleted and only then
    // registers, so a promise that completes on the thread pool in between would otherwise
    // fire a null continuation and drop the caller's for good — a deadlock, and an
    // intermittent one, which is worse than a broken one.
    private void Complete(CustomTaskStatus status, Exception error)
    {
        Action<object> c;
        object s;
        lock (_lock)
        {
            _error = error;
            _status = status;
            c = _continuation;
            s = _continuationState;
            _continuation = null;
            _continuationState = null;
        }
        c?.Invoke(s);
    }

    public CustomTaskStatus GetStatus(short token)
    {
        lock (_lock)
            return _status;
    }

    public void OnCompleted(Action<object> continuation, object state, short token)
    {
        bool alreadyDone;
        lock (_lock)
        {
            alreadyDone = _status != CustomTaskStatus.Pending;
            if (!alreadyDone)
            {
                _continuation = continuation;
                _continuationState = state;
            }
        }
        if (alreadyDone)
            continuation(state);
    }

    void ICustomTaskSource.GetResult(short token) => GetResult(token);

    public T GetResult(short token)
    {
        Exception error;
        CustomTaskStatus status;
        T result;
        lock (_lock)
        {
            error = _error;
            status = _status;
            result = _result;
            _stateMachine = default;
            _error = null;
            _result = default;
            _status = CustomTaskStatus.Pending; // reset before the runner goes back on the free list
        }
        TaskPool<Runner<TStateMachine, T>>.TryPush(this);
        if (status == CustomTaskStatus.Faulted)
            ExceptionDispatchInfo.Capture(error).Throw();
        return result;
    }
}
