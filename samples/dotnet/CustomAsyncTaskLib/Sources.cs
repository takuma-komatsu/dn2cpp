using System;
using System.Runtime.ExceptionServices;

namespace CustomAsyncTaskLib;

/// <summary>The promise a method that threw BEFORE its first suspension hands back — the
/// builder has no runner yet, so there is nothing pooled to fault.</summary>
internal sealed class FaultedSource : ICustomTaskSource
{
    private readonly Exception _error;

    private FaultedSource(Exception error) => _error = error;

    internal static CustomTask Create(Exception error) => new CustomTask(new FaultedSource(error), 0);

    public CustomTaskStatus GetStatus(short token) => CustomTaskStatus.Faulted;

    public void OnCompleted(Action<object> continuation, object state, short token) => continuation(state);

    public void GetResult(short token) => ExceptionDispatchInfo.Capture(_error).Throw();
}

internal sealed class FaultedSource<T> : ICustomTaskSource<T>
{
    private readonly Exception _error;

    private FaultedSource(Exception error) => _error = error;

    internal static CustomTask<T> Create(Exception error) =>
        new CustomTask<T>(new FaultedSource<T>(error), 0);

    public CustomTaskStatus GetStatus(short token) => CustomTaskStatus.Faulted;

    public void OnCompleted(Action<object> continuation, object state, short token) => continuation(state);

    void ICustomTaskSource.GetResult(short token) => GetResult(token);

    public T GetResult(short token)
    {
        ExceptionDispatchInfo.Capture(_error).Throw();
        return default;
    }
}

/// <summary>The promise a method that ran to completion synchronously hands back.</summary>
internal sealed class CompletedSource<T> : ICustomTaskSource<T>
{
    private readonly T _result;

    private CompletedSource(T result) => _result = result;

    internal static CustomTask<T> Create(T result) =>
        new CustomTask<T>(new CompletedSource<T>(result), 0);

    public CustomTaskStatus GetStatus(short token) => CustomTaskStatus.Succeeded;

    public void OnCompleted(Action<object> continuation, object state, short token) => continuation(state);

    void ICustomTaskSource.GetResult(short token) { }

    public T GetResult(short token) => _result;
}
