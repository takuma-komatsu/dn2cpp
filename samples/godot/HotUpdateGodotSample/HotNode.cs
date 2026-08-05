using System;
using Dn2Cpp.Runtime;

namespace HotUpdateGodotSample;

// A scene-tree node driving BPI deployment from inside a running engine: _Ready
// applies the patch directory (safe here — the GDExtension init already ran the
// runtime init and cctors on this thread), and _Process reads the hook virtual each
// frame, so the override the patch installed shows up in the frame output.
public class HotNode : Godot.Node
{
    private int _frame;

    public override void _Ready()
    {
        Console.WriteLine("HotNode._Ready: start");
        Console.WriteLine(string.Concat("hot: before ", TickHook.Current.Describe()));

        // Reachability anchors for the engine APIs the interpreted patch code calls: a
        // skipped shim method gets its bindable fnPtr/invoker pair only when the base image
        // reaches it. IsInGroup is instead rooted through hotupdate-refs.txt, the
        // file-driven alternative for methods the base never calls.
        AddToGroup("base-group");
        Console.WriteLine(GetChildCount() == 0
            ? "hot: base child count ok"
            : "hot: base child count unexpected");
        Console.WriteLine(Godot.FileAccess.FileExists("res://project.godot")
            ? "hot: base sees project file"
            : "hot: base missing project file");
        // Mathf is a placeholder-bodied shim: this AOT call lowers inline and works, but the
        // reached method's placeholder body is deliberately un-invokable in a hot-update
        // base image — a patch importing it must fail at load, not return a default.
        Console.WriteLine(Godot.Mathf.Sqrt(9.0) == 3.0
            ? "hot: base mathf ok"
            : "hot: base mathf unexpected");

        // The deployment directory is an OS filesystem path handed in by the launcher;
        // res:// and user:// are not resolved here. Unset = fresh install, nothing to load.
        string? dir = Environment.GetEnvironmentVariable("DN2CPP_HOTPATCH_DIR");
        if (dir is null)
        {
            Console.WriteLine("hot: no patch dir");
        }
        else
        {
            try
            {
                int n = HotUpdate.LoadDirectory(dir);
                Console.WriteLine(string.Concat("hot: loaded:", n.ToString()));
            }
            catch (Exception e)
            {
                    // A patch that fails to bind fails loudly at load; surface the diagnostic and keep
                    // running unpatched instead of crashing the frame loop.
                Console.WriteLine(string.Concat("hot: load failed: ", e.Message));
            }
        }

        // A patch entry may have handed back a freshly constructed node. Put it in the tree
        // so the ENGINE drives its virtuals — the vtable-routed dispatch under test.
        Godot.Node? spawned = Holder.Pending;
        if (spawned is not null)
        {
            AddChild(spawned);
            spawned.SetProcess(true);
            Console.WriteLine("hot: spawned pending node");
        }

        SetProcess(true);
    }

    public override void _Process(double delta)
    {
        // Bounded frame prints keep the captured output deterministic under --quit-after.
        if (_frame < 3)
            Console.WriteLine(string.Concat("frame ", _frame.ToString(), ": ", TickHook.Current.Describe()));
        _frame++;
    }
}
