using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

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

    /// <summary>A same-name non-await-pattern overload. Adoption must not route it as the
    /// parameterless <see cref="GetAwaiter()"/> intrinsic.</summary>
    public Awaiter GetAwaiter(int marker) => marker == 0 ? new Awaiter(this) : default;

    /// <summary>UniTask.Yield's shape: a static member on the TASK TYPE returning a
    /// hand-rolled awaitable with no BCL counterpart. A cross-assembly caller is a MemberRef
    /// outside the mapped contract, so awaiting this from another assembly is what makes the
    /// adoption pre-scan decline this library and transpile its real IL.</summary>
    public static YieldAwaitable Yield() => new YieldAwaitable();

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

        public int Probe() => IsCompleted ? 1 : 0;

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

/// <summary>What <see cref="CustomTask.Yield"/> returns: a hand-rolled awaitable that always
/// suspends and reposts the continuation through the same <c>Task.Yield()</c> the library's
/// own cores suspend on. Not task-like (no [AsyncMethodBuilder]), so it is never an adoption
/// candidate itself — it is plain IL either way.</summary>
public readonly struct YieldAwaitable
{
    public Awaiter GetAwaiter() => new Awaiter();

    public readonly struct Awaiter : ICriticalNotifyCompletion
    {
        public bool IsCompleted => false;

        public void GetResult() { }

        public void OnCompleted(Action continuation) => UnsafeOnCompleted(continuation);

        public void UnsafeOnCompleted(Action continuation) =>
            System.Threading.Tasks.Task.Yield().GetAwaiter().UnsafeOnCompleted(continuation);
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

    public static CustomTask<T> CompletedTask => default;

    public async Task<T> AsTask() => await this;

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
