using System.Threading.Tasks;
using Godot;

// Partial: the Godot.SourceGenerators emit the ScriptBridge/registration halves
// of every script class into the other part.
public partial class Player : Node
{
    private bool _processLogged;

    // The scene bakes a non-default Speed (engine -> managed Set at
    // instantiation); the others keep their C# initializer defaults, so _Ready's
    // print asserts both directions.
    [Export]
    public int Speed { get; set; } = 42;

    [Export]
    public string Label { get; set; } = "default-label";

    [Export]
    public float Factor { get; set; } = 1.5f;

    [Export]
    public Vector2 Spawn { get; set; } = new Vector2(3, 4);

    // The source generator turns this into the delegate + event pair, the
    // EmitSignalScored helper, and the RaiseGodotClassSignalCallbacks override.
    [Signal]
    public delegate void ScoredEventHandler(int amount);

    // Printed from the constructor to catch double construction: the bridge
    // allocates uninitialized, ties the object to the native Object*, then
    // invokes this in place — it must run exactly once per node.
    public Player()
    {
        GD.Print("DN2CPP_DM_CTOR");
    }

    public override void _Ready()
    {
        // Name round-trips through the native object this instance is tied to,
        // proving the managed instance really wraps the scene's node.
        GD.Print($"DN2CPP_DM_READY name={Name}");

        GD.Print($"DN2CPP_DM_EXPORT speed={Speed} label={Label} factor={Factor} spawn={Spawn}");

        // The emit goes out through the engine (EmitSignal -> connection dispatch
        // -> EventSignalCallable -> RaiseGodotClassSignalCallbacks) and back in.
        Scored += OnScored;
        EmitSignalScored(7);

        // Engine-signal paths on a one-shot Timer: a delegate-backed Callable
        // connection (ManagedCallable) and an awaited ToSignal (SignalAwaiter,
        // whose continuation resumes inline in the signal callback).
        var timer = new Timer();
        timer.OneShot = true;
        timer.WaitTime = 0.05;
        AddChild(timer);
        timer.Timeout += OnTimeout;
        // A SECOND Callable from a fresh delegate over the same method: the
        // engine-side lookup must fall through pointer identity to managed
        // delegate hash/equality to find the existing connection.
        GD.Print($"DN2CPP_DM_CONNECTED {timer.IsConnected(Timer.SignalName.Timeout, Callable.From(OnTimeout))}");
        _ = AwaitTimeoutAsync(timer);
        timer.Start();
    }

    public override void _Process(double delta)
    {
        if (_processLogged)
        {
            return;
        }
        _processLogged = true;
        GD.Print("DN2CPP_DM_PROCESS");
    }

    private void OnScored(int amount)
    {
        GD.Print($"DN2CPP_DM_SIGNAL amount={amount}");
    }

    private void OnTimeout()
    {
        GD.Print("DN2CPP_DM_TIMEOUT");
    }

    private async Task AwaitTimeoutAsync(Timer timer)
    {
        await ToSignal(timer, Timer.SignalName.Timeout);
        GD.Print("DN2CPP_DM_TOSIGNAL");
        await RunAsyncInteropAsync();
    }

    // Main-thread continuation marshalling: the frame callback's scheduler pump
    // must resume both awaits below on the main thread, where the inline ToSignal
    // resume above never left it.
    private async Task RunAsyncInteropAsync()
    {
        int mainId = System.Threading.Thread.CurrentThread.ManagedThreadId;
        ulong t0 = Time.GetTicksMsec();
        int workerId = await Task.Run(() => System.Threading.Thread.CurrentThread.ManagedThreadId);
        int resumedId = System.Threading.Thread.CurrentThread.ManagedThreadId;
        await Task.Delay(60);
        int delayId = System.Threading.Thread.CurrentThread.ManagedThreadId;
        ulong elapsed = Time.GetTicksMsec() - t0;
        GD.Print($"DN2CPP_DM_ASYNC resumeOnMain={resumedId == mainId} workerDiffers={workerId != mainId} delayOnMain={delayId == mainId} delayElapsedOk={elapsed >= 50}");
        await RunSyncContextAsync();
        // All timer-dependent markers are out; hand completion to the sibling
        // ParityNode, which quits once its own markers are too, so no marker
        // races the quit.
        ParityNode.NotifyPlayerFlowDone();
    }

    // The live main-thread SynchronizationContext: Current is the Dispatcher
    // singleton on the main thread and null on a worker, and a worker-side Post
    // lands back on the main thread on a later frame. Every step is awaited, so
    // the marker is deterministic.
    private async Task RunSyncContextAsync()
    {
        int mainId = System.Threading.Thread.CurrentThread.ManagedThreadId;
        var current = System.Threading.SynchronizationContext.Current;
        bool mainHasCtx = current is not null
            && ReferenceEquals(current, Dispatcher.SynchronizationContext);
        var posted = new TaskCompletionSource<int>();
        bool workerCtxNull = await Task.Run(() =>
        {
            bool ctxNull = System.Threading.SynchronizationContext.Current is null;
            Dispatcher.SynchronizationContext.Post(
                _ => posted.SetResult(System.Threading.Thread.CurrentThread.ManagedThreadId), null);
            return ctxNull;
        });
        int postedThreadId = await posted.Task;
        GD.Print($"DN2CPP_DM_SYNCCTX mainHasCtx={mainHasCtx} workerCtxNull={workerCtxNull} postedOnMain={postedThreadId == mainId}");
    }
}
