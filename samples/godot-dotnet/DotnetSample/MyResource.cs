using Godot;

// RefCounted-lifetime probe resource driven by LifetimeProbe. Tag deliberately
// has no [Export], so its value exists only in managed memory — reading it back
// after a round trip through engine storage plus a forced GC proves the fetched
// wrapper is the SAME managed instance, not a re-created one.
public partial class MyResource : Resource
{
    public static int Created;
    public static int Finalized;

    // Per-id ledger of the finalizers that ran. A conservative collector may pin
    // a handful of instances through a stale stack word, so "all N finalized" is
    // not assertable and the gate states a bounded shortfall; the ledger keeps
    // that exact about what did happen — N distinct ids, none of them twice.
    public static bool[] Ran = System.Array.Empty<bool>();
    public static int DoubleFinalized;

    // Ledger slot, or -1 for an instance no section tracks. The identity, binding
    // and dispose sections stay at -1: each asks the engine whether a native
    // object is gone (IsInstanceIdValid), which is per-instance already.
    public int Id { get; set; } = -1;

    public int Tag { get; set; }

    public MyResource()
    {
        Created++;
    }

    ~MyResource()
    {
        Finalized++;
        int id = Id;
        if (id >= 0 && id < Ran.Length)
        {
            if (Ran[id])
                DoubleFinalized++;
            else
                Ran[id] = true;
        }
    }
}
