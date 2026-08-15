using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Godot;

// The scripted node of the exported game. Kept small on purpose: the deep
// mono-module surface is covered by the DotnetSample gates, so what this one
// proves is the *export pipeline* — a game the forked editor transpiled and
// packaged runs inside a real exported .app with no .NET runtime beside it.
public partial class ExportProbe : Node
{
    // Overridden by the scene: reaching the C# instance through the engine's Set
    // bridge proves the script instance is tied to the native object.
    [Export]
    public int Answer { get; set; } = -1;

    // The GC churn is spread over frames so none allocates a spike, and the total
    // is kept modest to stay friendly to the browser tab the Web export runs in.
    private const int GcAllocFrames = 8;
    private const int GcArraysPerFrame = 8;
    private const int GcFinalizablesPerFrame = 4;

    private int _frames;
    private bool _gcReported;
    private bool _signalResumed;

    // Read at _Ready and again frames later. Both public MemberReferences resolve to real
    // CoreLib bodies and reach separate libSystem.Native monotonic-clock P/Invokes. A wasm
    // side module turns a missing PAL symbol into an import that fails only on the first
    // CALL, so both clocks have to be CALLED here. `System.` is not optional:
    // Godot.Environment is the WorldEnvironment resource, so under `using Godot` the bare
    // name is ambiguous and game code has to qualify it.
    private long _tickAtReady;
    private long _stampAtReady;

    public override void _Ready()
    {
        // The markers go through plain stdout: on iOS GD.Print reaches only the
        // platform logger, which the simulator launch pty cannot observe.
        Console.WriteLine($"DN2CPP_EXPORT_READY name={Name} answer={Answer}");
        // One engine-logger line stays behind so the macOS gate still proves the
        // GD.Print path end to end.
        GD.Print("DN2CPP_EXPORT_GDPRINT engine logger path ok");
        _tickAtReady = System.Environment.TickCount64;
        _stampAtReady = Stopwatch.GetTimestamp();

        ProbeInterop();
        _ = ProbeSignalAsync();
    }

    // Both calls below return the engine's `Error` — int-width in C++, `: long`
    // in C# because the bindings generator widens every core enum. wasm32 checks
    // the indirect call's signature, so the width has to be the engine's. Only
    // bools are printed: an enum name lookup throws under --trim-reflection.
    private void ProbeInterop()
    {
        var arr = new Godot.Collections.Array();
        Error resized = arr.Resize(3);
        Console.WriteLine($"DN2CPP_EXPORT_INTEROP resizeOk={resized == Error.Ok} sized={arr.Count == 3}");
    }

    private async Task ProbeSignalAsync()
    {
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        _signalResumed = true;
        Console.WriteLine("DN2CPP_EXPORT_SIGNAL awaited=True");
    }

    public override void _Process(double delta)
    {
        // A distinct latch, so a broken _Process dispatch shows up as a missing or
        // repeated marker rather than as a hang.
        if (_frames == 0)
            Console.WriteLine($"DN2CPP_EXPORT_PROCESS class={GetClass()} inTree={IsInsideTree()} deltaOk={delta > 0.0}");

        // Churn: 1 MB arrays and finalizable objects, all dropped on the floor.
        if (_frames < GcAllocFrames)
        {
            for (int i = 0; i < GcArraysPerFrame; i++)
            {
                var junk = new byte[1024 * 1024];
                junk[0] = (byte)_frames; // touch it so the allocation can't be elided
            }
            for (int i = 0; i < GcFinalizablesPerFrame; i++)
                _ = new FinalizableProbe();
            _frames++;
            return;
        }

        if (_gcReported)
            return;
        // The awaited continuation resumes on a ProcessFrame the churn above
        // already spent, so holding the quit behind it costs nothing and stops a
        // continuation that never resumes from passing as a race with the quit.
        if (!_signalResumed)
            return;
        _gcReported = true;

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        // finalized is THE oracle: a finalizer cannot run without a real collection,
        // which the calloc fallback never performs. A conservative collector may pin
        // a few probes, so the test is count > 0, never an exact count. bounded is
        // supporting evidence only — GC.GetTotalMemory reads small on calloc too.
        bool finalized = FinalizableProbe.FinalizedCount > 0;
        long thrownAwayBytes = (long)GcAllocFrames * GcArraysPerFrame * 1024 * 1024;
        bool bounded = GC.GetTotalMemory(true) < thrownAwayBytes / 4;
        Console.WriteLine($"DN2CPP_EXPORT_GC finalized={finalized} bounded={bounded}");

        // Booleans, never the readings: the values are non-deterministic and the
        // frequency is the host's (1e9 on the POSIX PAL, the QPC rate on Windows).
        bool ticks = System.Environment.TickCount64 >= _tickAtReady;
        bool elapsed = Stopwatch.GetTimestamp() >= _stampAtReady && Stopwatch.Frequency > 0;
        Console.WriteLine($"DN2CPP_EXPORT_CLOCK ticks={ticks} elapsed={elapsed}");

        Console.WriteLine("DN2CPP_EXPORT_DONE");
        GetTree().Quit();
    }

    // Witnesses a real collection: finalizers do not run on the calloc fallback, so
    // a non-zero count after Collect + WaitForPendingFinalizers cannot happen there.
    private sealed class FinalizableProbe
    {
        public static int FinalizedCount;

        ~FinalizableProbe() => Interlocked.Increment(ref FinalizedCount);
    }
}
