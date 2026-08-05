using System;
using System.Threading;
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

    public override void _Ready()
    {
        // The markers go through plain stdout: on iOS GD.Print reaches only the
        // platform logger, which the simulator launch pty cannot observe.
        Console.WriteLine($"DN2CPP_EXPORT_READY name={Name} answer={Answer}");
        // One engine-logger line stays behind so the macOS gate still proves the
        // GD.Print path end to end.
        GD.Print("DN2CPP_EXPORT_GDPRINT engine logger path ok");
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
