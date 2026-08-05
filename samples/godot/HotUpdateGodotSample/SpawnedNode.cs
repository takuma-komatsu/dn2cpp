using System;

namespace HotUpdateGodotSample;

// The engine-virtual seam a hot-update patch overrides: a ClassDB-registered class
// whose _Process the engine drives through the class's vtable. A patch subclass
// instance carries the interpreted override in its vtable copy, so the gate asserts
// this base body's marker never appears.
public class SpawnedNode : Godot.Node
{
    private int _frame;

    public override void _Process(double delta)
    {
        // Bounded frame prints keep the captured output deterministic under --quit-after.
        if (_frame < 3)
            Console.WriteLine("spawned base tick");
        _frame++;
    }
}

// The slot through which a patch entry hands a freshly constructed node back to base
// code. HotNode adds it to the scene tree after the load, so the engine — not
// managed code — drives its virtuals from then on.
public static class Holder
{
    public static Godot.Node? Pending;
}
