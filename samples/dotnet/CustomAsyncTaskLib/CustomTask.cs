using System;
using System.Runtime.CompilerServices;

namespace CustomAsyncTaskLib;

public enum CustomTaskStatus : byte
{
    Pending = 0,
    Succeeded = 1,
    Faulted = 2,
}

/// <summary>The backing promise an unfinished task reads through. Carries a default interface
/// method, mirroring GDTask's <c>IGDTaskSource&lt;T&gt; : IValueTaskSource&lt;T&gt;</c>
/// bridge — one more thing an unshielded transpile would have to close per state machine.</summary>
public interface ICustomTaskSource
{
    CustomTaskStatus GetStatus(short token);
    void OnCompleted(Action<object> continuation, object state, short token);
    void GetResult(short token);

    CustomTaskStatus UnsafeGetStatus() => GetStatus(0); // default interface method
}

public interface ICustomTaskSource<out T> : ICustomTaskSource
{
    new T GetResult(short token);
}

/// <summary>A task-like type in the GDTask/UniTask mould: a `readonly struct` whose
/// <c>[AsyncMethodBuilder]</c> names a builder of its own, so `async CustomTask` compiles.
/// A default(CustomTask) — a null source — is an ALREADY-COMPLETED task, which is what makes
/// the struct form free; dn2cpp models it on ValueTask for exactly that reason.</summary>
[AsyncMethodBuilder(typeof(AsyncCustomTaskMethodBuilder))]
public readonly struct CustomTask
{
    private readonly ICustomTaskSource _source;
    private readonly short _token;

    internal CustomTask(ICustomTaskSource source, short token)
    {
        _source = source;
        _token = token;
    }

    /// <summary>Deliberately a static FIELD, not a property — that is the shape GDTask uses
    /// (the BCL spells its counterpart as a property), and an intrinsic-mapped type has no
    /// static storage, so this is what exercises the ldsfld fold.</summary>
    public static readonly CustomTask CompletedTask = default;

    public Awaiter GetAwaiter() => new Awaiter(this);

    /// <summary>Nested, like <c>GDTask&lt;T&gt;.Awaiter</c>: its decoded FullName is the bare
    /// simple name "Awaiter", which is why the adoption registry cannot be keyed on a name.</summary>
    public readonly struct Awaiter : ICriticalNotifyCompletion
    {
        private readonly CustomTask _task;

        internal Awaiter(CustomTask task) => _task = task;

        public bool IsCompleted =>
            _task._source is null || _task._source.GetStatus(_task._token) != CustomTaskStatus.Pending;

        public void GetResult() => _task._source?.GetResult(_task._token);

        public void OnCompleted(Action continuation) => UnsafeOnCompleted(continuation);

        public void UnsafeOnCompleted(Action continuation)
        {
            if (_task._source is null)
            {
                continuation();
                return;
            }
            _task._source.OnCompleted(static s => ((Action)s)(), continuation, _task._token);
        }
    }
}

/// <summary>The generic form. Its result rides the promise, so dn2cpp's Dn2CppTask result
/// slot carries it natively once the type is adopted.</summary>
[AsyncMethodBuilder(typeof(AsyncCustomTaskMethodBuilder<>))]
public readonly struct CustomTask<T>
{
    private readonly ICustomTaskSource<T> _source;
    private readonly T _result;
    private readonly short _token;

    internal CustomTask(ICustomTaskSource<T> source, short token)
    {
        _source = source;
        _result = default;
        _token = token;
    }

    public Awaiter GetAwaiter() => new Awaiter(this);

    public readonly struct Awaiter : ICriticalNotifyCompletion
    {
        private readonly CustomTask<T> _task;

        internal Awaiter(CustomTask<T> task) => _task = task;

        public bool IsCompleted =>
            _task._source is null || _task._source.GetStatus(_task._token) != CustomTaskStatus.Pending;

        public T GetResult() =>
            _task._source is null ? _task._result : _task._source.GetResult(_task._token);

        public void OnCompleted(Action continuation) => UnsafeOnCompleted(continuation);

        public void UnsafeOnCompleted(Action continuation)
        {
            if (_task._source is null)
            {
                continuation();
                return;
            }
            _task._source.OnCompleted(static s => ((Action)s)(), continuation, _task._token);
        }
    }
}
