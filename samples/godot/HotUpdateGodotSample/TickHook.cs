namespace HotUpdateGodotSample;

// The hook seam a hot-update patch replaces: a plain (non-Godot) class held
// in a base static slot. AOT frame code dispatches Describe() virtually
// through the receiver's vtable, so a patch subclass instance installed into
// Current (a reference-typed static store, the proven patch entry surface)
// takes effect on the very next dispatch — no engine-side rebinding involved.
public class TickHook
{
    public static TickHook Current = new TickHook();

    public virtual string Describe()
    {
        return "base tick";
    }
}
