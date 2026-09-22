using System.Threading;
using System.Threading.Tasks;
using UnrealSharp;
using UnrealSharp.Attributes;
using UnrealSharp.Engine;
using UnrealSharp.Log;

namespace ManagedBaseline;

[UClass]
public partial class AAsyncProbeActor : AActor
{
    [UProperty]
    public partial int AsyncResult { get; set; }

    private bool _started;

    [UFunction(FunctionFlags.BlueprintCallable)]
    public void StartProbe()
    {
        if (_started) return;
        _started = true;
        try
        {
            UnrealSynchronizationContext game = new(NamedThread.GameThread);
            UnrealSynchronizationContext worker = new(NamedThread.AnyNormalThreadNormalTask);
            _ = RunProbe(game, worker, Environment.CurrentManagedThreadId);
        }
        catch (Exception error)
        {
            Fail(error);
        }
    }

    private async Task RunProbe(UnrealSynchronizationContext game, UnrealSynchronizationContext worker, int gameThread)
    {
        try
        {
            int taskValue = await Task.Run(() =>
            {
                Require(Environment.CurrentManagedThreadId != gameThread, "Task.Run thread");
                return 21;
            }).ConfigureAwait(false);
            int value = await new ValueTask<int>(Task.FromResult(taskValue * 2)).ConfigureAwait(false);
            Require(value == 42, "ValueTask result");

            using CancellationTokenSource cancellation = new();
            cancellation.Cancel();
            bool canceled = false;
            try
            {
                await Task.FromCanceled(cancellation.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                canceled = true;
            }
            Require(canceled, "Task cancellation");

            TaskCompletionSource<int> posted = new(TaskCreationOptions.RunContinuationsAsynchronously);
            worker.Post(_ =>
            {
                try
                {
                    Require(UnrealSynchronizationContext.CurrentThread != NamedThread.GameThread, "native worker thread");
                    int calls = 0;
                    Action callbacks = () => calls += 1;
                    callbacks += () => calls += 10;
                    callbacks();
                    posted.SetResult(calls);
                }
                catch (Exception error)
                {
                    posted.SetException(error);
                }
            }, null);
            using CancellationTokenSource timeout = new();
            Task deadline = Task.Delay(5000, timeout.Token);
            Task completed = await Task.WhenAny(posted.Task, deadline).ConfigureAwait(false);
            timeout.Cancel();
            Require(ReferenceEquals(completed, posted.Task), "native worker callback timeout");
            Require(await posted.Task.ConfigureAwait(false) == 11, "multicast callback");

            game.Post(_ =>
            {
                try
                {
                    Require(UnrealSynchronizationContext.CurrentThread == NamedThread.GameThread, "game thread return");
                    TWeakObjectPtr<AAsyncProbeActor> weak = new(this);
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    Require(weak.IsValid && ReferenceEquals(weak.Object, this), "wrapper identity after GC");
                    AsyncResult = 1023;
                    UnrealLogger.Log("Dn2CppSmoke", "async=1023");
                }
                catch (Exception error)
                {
                    Fail(error);
                }
            }, null);
        }
        catch (Exception error)
        {
            game.Post(_ => Fail(error), null);
        }
    }

    private static void Require(bool condition, string operation)
    {
        if (!condition) throw new InvalidOperationException(operation);
    }

    private void Fail(Exception error)
    {
        AsyncResult = -1;
        UnrealLogger.Log("Dn2CppSmoke", $"async-error={error.GetType().Name}:{error.Message}");
    }
}
