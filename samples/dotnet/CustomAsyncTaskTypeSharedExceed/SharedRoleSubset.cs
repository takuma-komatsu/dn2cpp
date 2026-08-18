// Two task types in different assemblies share CustomTask.Awaiter. An out-of-contract
// reference to that role must decline both owners; otherwise the later candidate re-adopts
// the handle and routes Probe through the TaskAwaiter intrinsic table.

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using CustomAsyncTaskLib;

namespace SharedRoleSubset;

[AsyncMethodBuilder(typeof(SharedAwaiterTaskBuilder))]
internal readonly struct SharedAwaiterTask
{
    private readonly CustomTask _task;

    internal SharedAwaiterTask(CustomTask task) => _task = task;

    internal CustomTask.Awaiter GetAwaiter() => _task.GetAwaiter();
}

internal struct SharedAwaiterTaskBuilder
{
    private AsyncCustomTaskMethodBuilder _builder;

    public static SharedAwaiterTaskBuilder Create() => new()
    {
        _builder = AsyncCustomTaskMethodBuilder.Create(),
    };

    public SharedAwaiterTask Task => new(_builder.Task);

    public void SetResult() => _builder.SetResult();

    public void SetException(Exception exception) => _builder.SetException(exception);

    public void SetStateMachine(IAsyncStateMachine stateMachine) =>
        _builder.SetStateMachine(stateMachine);

    public void Start<TStateMachine>(ref TStateMachine stateMachine)
        where TStateMachine : IAsyncStateMachine => _builder.Start(ref stateMachine);

    public void AwaitOnCompleted<TAwaiter, TStateMachine>(
        ref TAwaiter awaiter, ref TStateMachine stateMachine)
        where TAwaiter : INotifyCompletion
        where TStateMachine : IAsyncStateMachine =>
        _builder.AwaitOnCompleted(ref awaiter, ref stateMachine);

    public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(
        ref TAwaiter awaiter, ref TStateMachine stateMachine)
        where TAwaiter : ICriticalNotifyCompletion
        where TStateMachine : IAsyncStateMachine =>
        _builder.AwaitUnsafeOnCompleted(ref awaiter, ref stateMachine);
}

internal static class Program
{
    private static async SharedAwaiterTask Shared()
    {
        await Task.Yield();
    }

    private static async Task Run()
    {
        SharedAwaiterTask task = Shared();
        int probe = default(CustomTask).GetAwaiter().Probe();
        Console.WriteLine($"shared: probe {probe}");
        await task;
        Console.WriteLine("shared: done");
    }

    internal static void __GateEntry()
    {
        Run().GetAwaiter().GetResult();
    }
}
