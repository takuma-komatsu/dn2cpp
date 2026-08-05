using Godot;

// Provokes a managed fault at each of the two engine<->managed boundaries — the
// script-call boundary (guarded by real GodotSharp's CSharpInstanceBridge.Call)
// and the frame-callback boundary (a raw C function pointer, guarded by dn2cpp) —
// and keeps running; the assertion is that the engine is still there afterwards.
// Both faults are unhandled by design: a try/catch here would assert nothing,
// since the exception would never reach a native frame. CctorFaultProbe below
// rides along for the startup static-constructor pass, which shares the channel.
public partial class FaultProbe : Node
{
    // Distinctive enough that the gate's "no unexpected engine error" sweep can
    // subtract exactly these lines instead of being switched off.
    private const string Token = "DN2CPP_DM_FAULT_INJECTED";

    private static bool s_postedCallbackRan;
    private bool _methodFaulted;
    private bool _done;

    public override void _Ready()
    {
        // Frame-callback boundary. Dispatcher.SynchronizationContext is the native
        // main-thread singleton, so unlike SynchronizationContext.Current it cannot
        // be null this early. The callback runs on a later frame, from inside the
        // frame callback's drain stage — a native C frame.
        Dispatcher.SynchronizationContext.Post(
            _ =>
            {
                // Set BEFORE throwing: a guard that silently stopped running the
                // queue then shows up as posted=False, not as a missing line.
                s_postedCallbackRan = true;
                throw new System.InvalidOperationException(
                    Token + " raised from a SynchronizationContext callback");
            },
            null);
        GD.Print("DN2CPP_DM_FAULTPOSTED");
    }

    public override void _Process(double delta)
    {
        if (_done)
        {
            return;
        }
        if (!_methodFaulted)
        {
            // Script-call boundary, on the first tick. The print is not decoration:
            // an unregistered script class produces no fault and no error at all,
            // which a gate looking only for the error line would read as a pass.
            _methodFaulted = true;
            GD.Print("DN2CPP_DM_FAULTCALL");
            throw new System.InvalidOperationException(
                Token + " raised from an engine-invoked _Process");
        }
        if (!s_postedCallbackRan)
        {
            // Wait for the drain rather than assuming an ordering between
            // SceneTree::process and CSharpLanguage::frame within one iteration.
            return;
        }
        _done = true;
        // Position is load-bearing: this branch runs only after the drain, a later
        // stage of the same frame callback whose FIRST stage is the startup-cctor
        // pass — so the pass has provably run and this observes what it left behind.
        // The catch IS the assertion: a failed initializer must be re-raised by
        // every later first-use guard, not replaced by a zeroed static or a rerun.
        string cctorFault = "";
        try
        {
            cctorFault = CctorFaultProbe.Value;
        }
        catch (System.Exception e)
        {
            // The simple NAME, not FullName: that is the surface this lane's
            // trimmed run keeps, and the trim gate transpiles this same file.
            cctorFault = e.GetType().Name;
        }
        GD.Print($"DN2CPP_DM_CCTORPROBE reraised={cctorFault}");
        // Reaching this line at all is the result: _Process was called again after
        // throwing out of it, on a later frame than the one whose drain faulted.
        // The two flags tell a failure apart from a fault that never happened.
        GD.Print($"DN2CPP_DM_FAULTPROBE posted={s_postedCallbackRan} method={_methodFaulted}");
    }
}

// A type whose startup static constructor fails on demand. The failure is gated
// on an environment variable because four gates transpile this assembly and only
// one asks for it; an always-failing initializer would put a permanent error in
// the other three logs, whose assertion is that a clean init produces none. The
// env read does not perturb the emitted C++, so the self-host fixpoint holds.
internal static class CctorFaultProbe
{
    // A static field initializer, so the throw is inside the .cctor and the TYPE
    // ends up unusable — a method that merely threw would assert nothing.
    internal static readonly string Value = Init();

    private static string Init()
    {
        if (System.Environment.GetEnvironmentVariable("DN2CPP_DM_SAMPLE_CCTOR_FAULT") is not null)
        {
            throw new System.InvalidOperationException(
                "DN2CPP_DM_FAULT_INJECTED raised from a startup static constructor");
        }
        return "ok";
    }
}
