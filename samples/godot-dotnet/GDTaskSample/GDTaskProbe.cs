using System;
using System.Threading;
using Godot;
using GodotTask;

// Drives the real GDTask 3.1.0 tier-2 surface — its combinators, player-loop
// scheduler, object pool and cancellation model — all transpiled from GDTask's
// own shipped IL (see docs/ARCHITECTURE.md section 4-B).
//
// Every marker is a PREDICATE, never a raw frame count or elapsed time, so the
// gate can exact-diff the native run against the same project on real .NET:
// completion order is a property of the program, timing is one of the machine.
public partial class GDTaskProbe : Node
{
    public override void _Ready()
    {
        GD.Print("DN2CPP_GDTASK_READY");
        // Forget(): fire-and-forget extension on the GDTaskVoid builder (a C# 14
        // extension block in 3.1.0). Tier-2 surface — unmapped under tier 1.
        RunAsync().Forget();
    }

    // async GDTaskVoid — the third [AsyncMethodBuilder] type.
    private async GDTaskVoid RunAsync()
    {
        // 1. NextFrame — the player loop must actually pump; an undriven GDTask
        //    scheduler never resumes this await and the gate's watchdog fires.
        ulong f0 = Engine.GetProcessFrames();
        await GDTask.NextFrame(PlayerLoopTiming.Process);
        ulong f1 = Engine.GetProcessFrames();
        GD.Print($"DN2CPP_GDTASK_NEXTFRAME advanced={f1 > f0}");

        // 2. DelayFrame(5) — frames really advance, and by at least the amount
        //    asked for (the frame-counting timer, not merely "resumed").
        ulong f2 = Engine.GetProcessFrames();
        await GDTask.DelayFrame(5, PlayerLoopTiming.Process);
        ulong f3 = Engine.GetProcessFrames();
        GD.Print($"DN2CPP_GDTASK_DELAYFRAME advanced={f3 - f2 >= 5}");

        // 3. The second player-loop queue.
        await GDTask.NextFrame(PlayerLoopTiming.PhysicsProcess);
        GD.Print("DN2CPP_GDTASK_PHYSICS_TIMING ok");

        // 4. async GDTask<T> returning a value (AsyncGDTaskMethodBuilder<T>).
        int v = await ComputeAsync(20);
        GD.Print($"DN2CPP_GDTASK_VALUE {v}");

        // 5. WhenAll, all three arms: void, array (GDTask<T>[] -> GDTask<T[]>), and
        //    the fixed-arity tuple one, the deep-ValueTuple monomorphization
        //    pressure point.
        await GDTask.WhenAll(DelayFramesAsync(2), DelayFramesAsync(3));
        int[] all = await GDTask.WhenAll(new[] { ComputeAsync(1), ComputeAsync(2) });
        (int t1, int t2, int t3) = await GDTask.WhenAll(
            ComputeAsync(3), ComputeAsync(4), ComputeAsync(5));
        GD.Print($"DN2CPP_GDTASK_WHENALL void=ok array={all[0]},{all[1]} tuple={t1},{t2},{t3}");

        // 6. WhenAny, array form. The loser NEVER completes, so a correct WhenAny
        //    must not wait for it.
        (int winnerIndex, int winnerResult) = await GDTask.WhenAny(
            new[] { ComputeAsync(7), NeverEndingAsync() });
        GD.Print($"DN2CPP_GDTASK_WHENANY index={winnerIndex} result={winnerResult}");

        // 7. Delay — the real-time timer path (ValueStopwatch / DelayType).
        ulong t0 = Time.GetTicksMsec();
        await GDTask.Delay(60, PlayerLoopTiming.Process);
        GD.Print($"DN2CPP_GDTASK_DELAY elapsedOk={Time.GetTicksMsec() - t0 >= 50}");

        // 8. A CancellationToken-taking overload, cancellation observed as a
        //    THROW: GDTask's cancellation model is OperationCanceledException.
        var cts = new CancellationTokenSource();
        cts.Cancel();
        bool threw = false;
        try
        {
            await GDTask.Delay(5000, PlayerLoopTiming.Process, cts.Token);
        }
        catch (OperationCanceledException)
        {
            threw = true;
        }
        GD.Print($"DN2CPP_GDTASK_CANCEL_THROW {threw}");

        // 9. SuppressCancellationThrow() on the NON-generic GDTask -> GDTask<bool>.
        var cts2 = new CancellationTokenSource();
        cts2.Cancel();
        bool canceled = await GDTask.Delay(5000, PlayerLoopTiming.Process, cts2.Token)
            .SuppressCancellationThrow();
        GD.Print($"DN2CPP_GDTASK_SUPPRESS canceled={canceled}");

        // 10. SuppressCancellationThrow() on GDTask<T> -> GDTask<(bool, T)>: a
        //     signature naming a DEEPER instantiation of its own declaring type,
        //     which has no natural fixpoint under monomorphization. Awaiting it
        //     asserts the transpiler instantiates exactly this shape and stops.
        var cts3 = new CancellationTokenSource();
        cts3.Cancel();
        (bool isCanceled, int result) = await CancelableComputeAsync(cts3.Token)
            .SuppressCancellationThrow();
        GD.Print($"DN2CPP_GDTASK_SUPPRESS_T canceled={isCanceled} result={result}");

        // 11. A token that is NOT cancelled: the overload must complete normally,
        //     with its registration disposed and nothing left armed.
        var cts4 = new CancellationTokenSource();
        int okValue = await CancelableComputeAsync(cts4.Token);
        GD.Print($"DN2CPP_GDTASK_CANCEL_NOOP value={okValue}");

        // 12. RunOnThreadPool leaves the main thread and the continuation comes
        //     BACK to it (configureAwait: true).
        int mainId = System.Environment.CurrentManagedThreadId;
        int poolId = await GDTask.RunOnThreadPool(
            () => System.Environment.CurrentManagedThreadId, configureAwait: true);
        int afterId = System.Environment.CurrentManagedThreadId;
        GD.Print($"DN2CPP_GDTASK_THREADPOOL offMain={poolId != mainId} backOnMain={afterId == mainId}");

        // 13. SwitchToThreadPool / SwitchToMainThread — the explicit hop pair.
        await GDTask.SwitchToThreadPool();
        int onPool = System.Environment.CurrentManagedThreadId;
        await GDTask.SwitchToMainThread(PlayerLoopTiming.Process);
        int backMain = System.Environment.CurrentManagedThreadId;
        GD.Print($"DN2CPP_GDTASK_SWITCH offMain={onPool != mainId} backOnMain={backMain == mainId}");

        // 14. GDTaskCompletionSource<T> — the manual producer side, completed
        //     from a frame callback a few frames later.
        var tcs = new GDTaskCompletionSource<string>();
        CompleteLaterAsync(tcs).Forget();
        string fromTcs = await tcs.Task;
        GD.Print($"DN2CPP_GDTASK_TCS {fromTcs}");

        // 15. WaitUntil — the polling primitive driven by the player loop.
        int counter = 0;
        TickAsync().Forget();
        await GDTask.WaitUntil(() => counter >= 3, PlayerLoopTiming.Process);
        GD.Print($"DN2CPP_GDTASK_WAITUNTIL reached={counter >= 3}");

        // 16. An exception inside an async GDTask propagates to the awaiter (the
        //     promise's fault path, not the tracker's).
        bool caught = false;
        try
        {
            await ThrowingAsync();
        }
        catch (InvalidOperationException e) when (e.Message == "gdtask-boom")
        {
            caught = true;
        }
        GD.Print($"DN2CPP_GDTASK_THROW caught={caught}");

        GD.Print("DN2CPP_GDTASK_DONE");
        GetTree().Quit();

        // Bumps `counter` from the player loop so WaitUntil has something to
        // observe — a closure over the enclosing state machine's locals.
        async GDTaskVoid TickAsync()
        {
            for (int i = 0; i < 3; i++)
            {
                await GDTask.NextFrame(PlayerLoopTiming.Process);
                counter++;
            }
        }
    }

    private static async GDTask<int> ComputeAsync(int seed)
    {
        await GDTask.NextFrame(PlayerLoopTiming.Process);
        return seed * 2;
    }

    private static async GDTask DelayFramesAsync(int frames)
    {
        await GDTask.DelayFrame(frames, PlayerLoopTiming.Process);
    }

    private static async GDTask<int> CancelableComputeAsync(CancellationToken ct)
    {
        await GDTask.DelayFrame(2, PlayerLoopTiming.Process, ct);
        return 99;
    }

    private static async GDTask<int> NeverEndingAsync()
    {
        await GDTask.Delay(600000, PlayerLoopTiming.Process);
        return -1;
    }

    private static async GDTaskVoid CompleteLaterAsync(GDTaskCompletionSource<string> tcs)
    {
        await GDTask.DelayFrame(2, PlayerLoopTiming.Process);
        tcs.TrySetResult("hello-from-tcs");
    }

    private static async GDTask ThrowingAsync()
    {
        await GDTask.NextFrame(PlayerLoopTiming.Process);
        throw new InvalidOperationException("gdtask-boom");
    }
}
