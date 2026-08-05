using System;
using System.Runtime.CompilerServices;

namespace CustomAsyncTaskLib;

/// <summary>The compiler builder contract, in full, for `async CustomTask`. Every member here
/// is one the C# compiler REQUIRES of an async method builder — which is exactly why dn2cpp
/// can adopt a third-party builder by name: the names are the BCL's because the compiler
/// mandates them, not by coincidence.
///
/// <para><c>Start&lt;TStateMachine&gt;</c>'s body is a `constrained. !!TStateMachine callvirt
/// IAsyncStateMachine::MoveNext` — the very call an unadopted builder dies on. Adoption means
/// this body is never transpiled at all: Start lowers to a direct call to the state machine's
/// MoveNext, exactly as the BCL builder's does.</para></summary>
public struct AsyncCustomTaskMethodBuilder
{
    private IStateMachineRunnerPromise _runnerPromise;
    private Exception _error;

    public static AsyncCustomTaskMethodBuilder Create() => default;

    public CustomTask Task
    {
        get
        {
            if (_runnerPromise is not null)
                return _runnerPromise.Task;
            if (_error is not null)
                return FaultedSource.Create(_error);
            return CustomTask.CompletedTask;
        }
    }

    public void SetResult()
    {
        if (_runnerPromise is not null)
            _runnerPromise.SetResult();
    }

    public void SetException(Exception exception)
    {
        if (_runnerPromise is null)
            _error = exception;
        else
            _runnerPromise.SetException(exception);
    }

    public void SetStateMachine(IAsyncStateMachine stateMachine)
    {
        // The machine is started directly, so there is nothing to hand back.
    }

    public void Start<TStateMachine>(ref TStateMachine stateMachine)
        where TStateMachine : IAsyncStateMachine
        => stateMachine.MoveNext();

    public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
        where TAwaiter : INotifyCompletion
        where TStateMachine : IAsyncStateMachine
    {
        if (_runnerPromise is null)
            Runner<TStateMachine>.SetStateMachine(ref stateMachine, ref _runnerPromise);
        awaiter.OnCompleted(_runnerPromise.MoveNext);
    }

    public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
        where TAwaiter : ICriticalNotifyCompletion
        where TStateMachine : IAsyncStateMachine
    {
        if (_runnerPromise is null)
            Runner<TStateMachine>.SetStateMachine(ref stateMachine, ref _runnerPromise);
        awaiter.UnsafeOnCompleted(_runnerPromise.MoveNext);
    }
}

/// <summary>The same contract for `async CustomTask&lt;T&gt;`.</summary>
public struct AsyncCustomTaskMethodBuilder<T>
{
    private IStateMachineRunnerPromise<T> _runnerPromise;
    private Exception _error;
    private T _result;

    public static AsyncCustomTaskMethodBuilder<T> Create() => default;

    public CustomTask<T> Task
    {
        get
        {
            if (_runnerPromise is not null)
                return _runnerPromise.Task;
            if (_error is not null)
                return FaultedSource<T>.Create(_error);
            return CompletedSource<T>.Create(_result);
        }
    }

    public void SetResult(T result)
    {
        if (_runnerPromise is null)
            _result = result;
        else
            _runnerPromise.SetResult(result);
    }

    public void SetException(Exception exception)
    {
        if (_runnerPromise is null)
            _error = exception;
        else
            _runnerPromise.SetException(exception);
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
        if (_runnerPromise is null)
            Runner<TStateMachine, T>.SetStateMachine(ref stateMachine, ref _runnerPromise);
        awaiter.OnCompleted(_runnerPromise.MoveNext);
    }

    public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
        where TAwaiter : ICriticalNotifyCompletion
        where TStateMachine : IAsyncStateMachine
    {
        if (_runnerPromise is null)
            Runner<TStateMachine, T>.SetStateMachine(ref stateMachine, ref _runnerPromise);
        awaiter.UnsafeOnCompleted(_runnerPromise.MoveNext);
    }
}
