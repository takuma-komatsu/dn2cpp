namespace GodotSample
{
    /// <summary>
    /// A registered node whose engine-invoked _Ready throws on purpose: the one boundary
    /// whose contract is to DEGRADE. The GDExtension bridge catches a managed exception
    /// escaping into an engine C frame and hands it to dn2cpp_report_boundary_exception,
    /// which reports it through the engine's error log instead of unwinding into engine
    /// frames.
    ///
    /// _Ready is the deliberate pick: it takes no arguments and returns nothing, so
    /// nothing the per-class trampoline decoded is in flight when the exception unwinds.
    ///
    /// Keep this node the FIRST child of Root in main.tscn. NOTIFICATION_READY propagates
    /// in declaration order, so being first puts the fault ahead of every marker MyNode
    /// and MyNode3D print, which is what lets those markers stand as the survival proof.
    /// </summary>
    public class MyFaultNode : Godot.Node
    {
        public override void _Ready()
        {
            // Printed BEFORE the throw and asserted on its own, because "no boundary report in
            // the output" is otherwise ambiguous between the reporter going silent — the
            // regression this catches — and the fixture never running at all. It goes out
            // through GD.Print, the engine's own channel, so it also witnesses that the engine
            // reached this body.
            Godot.GD.Print("MyFaultNode._Ready: about to throw across the engine boundary");

            // No guard: _ready fires once per instance, and suppressing a repeat would hide a
            // re-dispatch regression the gate's report count catches. The message carries a
            // token nothing else in the corpus prints.
            throw new System.InvalidOperationException(
                "MyFaultNode._Ready fault DN2CPP_GD_FAULT_INJECTED");
        }
    }
}
