using System;
using HotUpdateGodotSample;

namespace HotUpdateGodotPatch;

// A patch over the Godot base image that also exercises engine calls from
// interpreted code: a hot-update base synthesizes an invoker-compatible
// wrapper for every reachable engine-shim method (plus any rooted through
// hotupdate-refs.txt), so patch method imports against the shim surface bind
// like any base-image method. Markers go through Console.WriteLine; the group
// round-trip one builds its line with string concatenation (String.Concat on
// the intrinsic import surface).
public class PatchHook : TickHook
{
    public override string Describe()
    {
        return "patched tick";
    }
}

// A Node-derived patch class: `new PatchNode()` in the interpreter gets its
// engine object through the runtime's allocation hook (constructed under the
// nearest ClassDB-registered ancestor, SpawnedNode), and the engine's
// per-frame _process dispatch reaches this interpreted override through the
// base image's vtable-routed virtual trampoline. The first frame makes
// instance engine calls on the node itself: a string-arg/bool-return group
// round-trip (AddToGroup + IsInGroup — IsInGroup is base-unused, rooted via
// hotupdate-refs.txt) and an int-return read (GetChildCount; a fresh node has
// no children, so the value is deterministic).
public class PatchNode : SpawnedNode
{
    private int _frame;

    public override void _Process(double delta)
    {
        if (_frame == 0)
        {
            AddToGroup("patched-group");
            Console.WriteLine("patch: engine group round-trip "
                + (IsInGroup("patched-group") ? "ok" : "failed"));
            Console.WriteLine(GetChildCount() == 0
                ? "patch: engine child count ok"
                : "patch: engine child count unexpected");
        }
        if (_frame < 3)
            Console.WriteLine("spawned patched tick");
        _frame++;
    }
}

internal static class Program
{
    private static void Main()
    {
        Console.WriteLine("patch: installing hook");
        TickHook.Current = new PatchHook();
        // A static engine-shim call from the interpreter (no receiver — the
        // wrapper passes the engine's null-instance sentinel): the project
        // file is always present when the engine runs this scene.
        Console.WriteLine(Godot.FileAccess.FileExists("res://project.godot")
            ? "patch: static engine call ok"
            : "patch: static engine call failed");
        Console.WriteLine("patch: spawning node");
        Holder.Pending = new PatchNode();
    }
}
