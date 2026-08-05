using System;
using Godot;

// Drives the RefCounted lifetime battery synchronously in _Ready, printing one
// exactly-once marker per scenario for the gate to assert. Every Variant temp
// holding an Object is disposed explicitly: an undisposed one owns an engine
// reference until its Disposer finalizes, which would make the strong/weak swap
// points nondeterministic.
public partial class LifetimeProbe : Node
{
    private const int StressCount = 1000;
    private const int SwapCount = 64;

    // The two population sections own DISJOINT id ranges in one ledger array, so
    // a straggler finalizing during the later section cannot inflate its tally.
    private const int SwapIdBase = 0;
    private const int StressIdBase = SwapCount;

    // Ledger slot for the engine-release section's one-off object, past both
    // population ranges. That section asks whether ONE named wrapper finalized;
    // the class-wide MyResource.Finalized cannot tell it from an earlier
    // section's straggler, so only a per-id slot can answer.
    private const int ReleasedId = StressIdBase + StressCount;
    private const int LedgerSize = ReleasedId + 1;

    // How many of a population a conservative stack scan may pin through a stale
    // word. A tolerance for collector luck, not for a leak: the regressions these
    // sections catch (a handle stuck strong, a swap that never weakens) release
    // ZERO, not "a few fewer".
    private const int SwapPinAllowance = 8;
    private const int StressPinAllowance = 24;

    // Holds the stress population alive until the drop pass: releasing the
    // objects must not hinge on stale stack words vanishing, which a
    // conservative collector does not guarantee.
    private static MyResource[] _stress;

    public override void _Ready()
    {
        MyResource.Ran = new bool[LedgerSize];
        SwapIdentity();
        SwapReleasePopulation();
        EngineReleaseFinalizes();
        BindingRoundtrip();
        DisposeFlows();
        StressLoop();
        GD.Print("DN2CPP_DM_RC_DONE");
    }

    // Counts the ids of one population whose finalizer ran, out of the ledger
    // rather than a counter, so the marker cannot report more than it can vouch for.
    private static int LedgerRan(int idBase, int count)
    {
        int n = 0;
        for (int i = 0; i < count; i++)
        {
            if (MyResource.Ran[idBase + i])
            {
                n++;
            }
        }
        return n;
    }

    // Overwrites a deep swath of the stack so no stale copy of a dropped
    // wrapper's pointer survives where a conservative stack scan would see it.
    private static long Stomp(int depth)
    {
        long a = depth, b = a + 3, c = b + 5, d = c + 7;
        if (depth <= 0)
        {
            return a ^ b ^ c ^ d;
        }
        return Stomp(depth - 1) ^ a ^ b ^ c ^ d;
    }

    private static void CollectAndDrain()
    {
        Stomp(128);
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }

    // Bounded collect-and-drain retry until ONE named id's finalizer has run; a
    // stuck wrapper leaves its slot false. Double-finalization of this id is
    // covered by the class-wide DoubleFinalized in the RC_STRESS marker.
    private static bool DrainUntilRan(int id)
    {
        for (int i = 0; i < 64 && !MyResource.Ran[id]; i++)
        {
            CollectAndDrain();
        }
        return MyResource.Ran[id];
    }

    // Same bounded retry for a population; falling short of the target means the
    // collector pinned some, and only the ledger can tell which.
    private static void Drain(int target)
    {
        for (int i = 0; i < 64 && MyResource.Finalized < target; i++)
        {
            CollectAndDrain();
        }
    }

    // One full swap cycle, in its own short-lived frame so the wrapper reference
    // dies with the call. From the last RemoveMeta the object is garbage.
    private void SwapReleaseOne(int id)
    {
        var res = new MyResource();
        res.Id = id;
        res.Tag = 1234;
        using (Variant v = res)
        {
            SetMeta("swap_pop", v); // refcount 1 -> 2: weak->strong swap
        }
        MyResource fetched;
        using (Variant stored = GetMeta("swap_pop"))
        {
            fetched = (MyResource)stored.AsGodotObject();
        }
        fetched.Tag = 5678;
        RemoveMeta("swap_pop"); // refcount 2 -> 1: strong->weak swap
        using (Variant v = fetched)
        {
            SetMeta("swap_pop", v); // refcount 1 -> 2: weak->strong swap
        }
        RemoveMeta("swap_pop"); // strong->weak: releasable from here
    }

    // The release half of the swap story, asked of a POPULATION: no conservative
    // collector promises to collect any ONE object, so the single-object form is
    // host-dependent. The bug is still caught whole — a swap that leaves the
    // handle strong releases none of the 64.
    private void SwapReleasePopulation()
    {
        int finalizedBefore = MyResource.Finalized;
        for (int i = 0; i < SwapCount; i++)
        {
            SwapReleaseOne(SwapIdBase + i);
        }
        Drain(finalizedBefore + SwapCount);
        int ran = LedgerRan(SwapIdBase, SwapCount);
        GD.Print($"DN2CPP_DM_RC_SWAP_RELEASED enough={ran >= SwapCount - SwapPinAllowance} ledgerOk={MyResource.DoubleFinalized == 0}");
    }

    // Kept out of the caller so the local reference dies with this frame. Storing
    // into metadata references the object (refcount 1 -> 2), which must flip the
    // initially-weak script-instance handle to strong.
    private void CreateAndStoreSwap()
    {
        var res = new MyResource();
        res.Tag = 1234;
        using Variant v = res;
        SetMeta("swap_res", v);
    }

    // Fetches the stored instance back, mutates managed-only state, then walks the
    // handle strong->weak (local still holds the wrapper) and back to strong.
    private bool SwapCycleKeepsTag()
    {
        MyResource fetched;
        using (Variant stored = GetMeta("swap_res"))
        {
            fetched = (MyResource)stored.AsGodotObject();
        }
        bool tagOk = fetched.Tag == 1234;
        fetched.Tag = 5678;
        RemoveMeta("swap_res"); // refcount 2 -> 1: strong->weak swap
        using (Variant v = fetched) // refcount 1 -> 2: weak->strong swap
        {
            SetMeta("swap_res", v);
        }
        return tagOk;
    }

    private void SwapIdentity()
    {
        CreateAndStoreSwap();
        CollectAndDrain(); // must NOT collect: the engine ref keeps the handle strong
        bool first = SwapCycleKeepsTag();
        CollectAndDrain(); // must survive the weak->strong swap round too
        MyResource again;
        using (Variant stored = GetMeta("swap_res"))
        {
            again = (MyResource)stored.AsGodotObject();
        }
        GD.Print($"DN2CPP_DM_RC_SWAP first={first} tag={again.Tag} valid={IsInstanceValid(again)}");
        RemoveMeta("swap_res"); // strong->weak: from here only this frame's locals keep it alive
    }

    private ulong CreateEngineHeld()
    {
        var res = new MyResource();
        res.Id = ReleasedId;
        using Variant v = res;
        SetMeta("released_res", v);
        return res.GetInstanceId();
    }

    private void EngineReleaseFinalizes()
    {
        ulong id = CreateEngineHeld();
        CollectAndDrain();
        // Engine-held only: the wrapper must have survived the GC and the native
        // object must still be alive. Both halves are asked of THIS object — its
        // instance id and its own ledger slot — never of a class-wide count.
        bool heldAlive = IsInstanceIdValid(id) && !MyResource.Ran[ReleasedId];
        RemoveMeta("released_res"); // refcount 2 -> 1: strong->weak swap, wrapper is now garbage
        bool finalized = DrainUntilRan(ReleasedId);
        bool nativeFreed = !IsInstanceIdValid(id);
        GD.Print($"DN2CPP_DM_RC_RELEASED heldAlive={heldAlive} finalized={finalized} nativeFreed={nativeFreed}");
    }

    // Engine-created RefCounted (no script) surfacing through the instance-binding
    // path; disposing the Variants leaves the metadata entry plus the binding
    // wrapper's unsafe reference as the only owners.
    private ulong CreateBindingHeld()
    {
        using Variant v = ClassDB.Instantiate("RefCounted");
        var rc = (RefCounted)v.AsGodotObject();
        using Variant store = rc;
        SetMeta("binding_res", store);
        return rc.GetInstanceId();
    }

    private bool BindingIdentity()
    {
        RefCounted a;
        RefCounted b;
        using (Variant va = GetMeta("binding_res"))
        {
            a = (RefCounted)va.AsGodotObject();
        }
        using (Variant vb = GetMeta("binding_res"))
        {
            b = (RefCounted)vb.AsGodotObject();
        }
        return ReferenceEquals(a, b);
    }

    private void BindingRoundtrip()
    {
        ulong id = CreateBindingHeld();
        CollectAndDrain(); // binding wrapper must survive: engine still references the native side
        bool alive = IsInstanceIdValid(id);
        bool identity = BindingIdentity();
        RemoveMeta("binding_res");
        bool freed = false;
        for (int i = 0; i < 64 && !freed; i++)
        {
            CollectAndDrain(); // wrapper dies -> finalizer drops the unsafe ref -> native freed
            freed = !IsInstanceIdValid(id);
        }
        GD.Print($"DN2CPP_DM_RC_BINDING alive={alive} identity={identity} freed={freed}");
    }

    private void DisposeFlows()
    {
        // Explicit Dispose() on a C#-only RefCounted: the managed side holds
        // the only reference, so disposing must delete the native object.
        var res = new MyResource();
        ulong resId = res.GetInstanceId();
        res.Dispose();
        bool resFreed = !IsInstanceIdValid(resId);
        res.Dispose(); // double dispose: must stay a silent no-op
        bool resPtrCleared = res.NativeInstance == IntPtr.Zero;

        // Free() on a Node: the engine deletes the native object and the
        // binding-free callback zeroes the wrapper's native pointer, so a
        // later Dispose (or the finalizer) must not double-free.
        var node = new Node();
        ulong nodeId = node.GetInstanceId();
        node.Free();
        bool nodeFreed = !IsInstanceIdValid(nodeId);
        bool nodePtrCleared = node.NativeInstance == IntPtr.Zero;
        node.Dispose();

        GD.Print($"DN2CPP_DM_RC_DISPOSE resFreed={resFreed} resPtrCleared={resPtrCleared} nodeFreed={nodeFreed} nodePtrCleared={nodePtrCleared}");
    }

    // One create/store/drop cycle per call: a short-lived frame (rather than a
    // loop local) lets the next call overwrite the slots the previous iteration's
    // pointer lived in, leaving no stale word for the conservative scan.
    private void StressOne(int i)
    {
        var r = new MyResource();
        r.Id = StressIdBase + i;
        r.Tag = i;
        using (Variant v = r) // refcount 1 -> 2: weak->strong swap
        {
            SetMeta("stress_res", v);
        }
        RemoveMeta("stress_res"); // refcount 2 -> 1: strong->weak swap
        _stress[i] = r;
    }

    private void StressAllocate()
    {
        _stress = new MyResource[StressCount];
        for (int i = 0; i < StressCount; i++)
        {
            StressOne(i);
        }
    }

    private void StressLoop()
    {
        int createdBefore = MyResource.Created;
        int finalizedBefore = MyResource.Finalized;
        StressAllocate();
        int created = MyResource.Created - createdBefore;
        for (int i = 0; i < StressCount; i++)
        {
            _stress[i] = null;
        }
        _stress = null;
        Drain(finalizedBefore + created);
        int ran = LedgerRan(StressIdBase, StressCount);
        // Both directions of refcount drift: "enough" catches a wrapper stuck
        // alive (a native-side leak shows up as the gate's "ObjectDB instances
        // leaked" grep), "ledgerOk" restores the exactness the tolerance costs.
        GD.Print($"DN2CPP_DM_RC_STRESS created={created == StressCount} enough={ran >= StressCount - StressPinAllowance} ledgerOk={MyResource.DoubleFinalized == 0}");
    }
}
