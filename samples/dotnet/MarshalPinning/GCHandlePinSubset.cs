// System.Runtime.InteropServices.GCHandle model. dn2cpp's managed heap is non-moving
// (Boehm mark-sweep / calloc fallback), so Normal/Pinned pinning is identity — the
// intrinsic value type Dn2CppGCHandle is one pointer to a shared uncollectable cell
// (the moral equivalent of real .NET's handle-table slot): AddrOfPinnedObject returns
// the pinned data address, Target/IsAllocated read the cell, Free clears the cell (so
// invalidation propagates to every copy of the struct), and ToIntPtr returns the
// stable cell address, which roots the target even when it is the handle's only
// surviving representation. Weak/WeakTrackResurrection instead delegate to the
// low-level weak table (a real Boehm disappearing/long link), so a weak handle is
// really weak. Sections 1-9 cover the pinning path; 10 covers Free invalidation; 11 &
// 13 cover the weak path (deterministic round-trip + an aggregate collection check);
// 12 covers ToIntPtr/FromIntPtr round-tripping; 14-15 cover Free propagation through
// copies / round-trips; 16 covers ToIntPtr stability; 17 covers native-side-only
// retention (the token as a plain integer is the only root); 18 covers the measured
// exception behaviors; 19 covers the Target setter; 20 covers GetHashCode (the raw
// handle hash: copy/round-trip equal, distinct handles apart, default = 0).
//
// This is exactly the path dn2cpp itself executes at runtime: SRM's VirtualHeap
// pins an [InlineArray]-attribute blob (byte[]) via GCHandle.Alloc(byte[], Pinned)
// and reads it through AddrOfPinnedObject, then GCHandle.Free on release.
//
// Raw pointer ADDRESSES are non-deterministic, so the sample never prints an
// address — it verifies the CONTENT read/written through the pin (and the pin's
// stability across reads), which is a deterministic exact diff vs real .NET.
//
// GC TIMING, and why this section is LAST in the driver. Section 13 asserts an
// aggregate fact about the heap (50,000 weak-handle payloads must not all
// survive a forced collection) and section 17 storms the allocator to force
// reuse of any wrongly-freed block, so both read process-wide state rather than
// their own. Running last means no later section inherits the ~200 MB of churn
// or the retained Blob chain; running after the Marshal sections is safe in the
// other direction because none of them leaves a live set anywhere near section
// 13's 51.2 MB threshold (50,000 x 4096 / 4). Do not move it up the driver, and
// do not append a heap-holding section after it.
using System;
using System.Runtime.InteropServices;

namespace GCHandlePinSubset;

unsafe class Program
{
    internal static void __GateEntry()
    {
        // 1. Pin a byte[] (the VirtualHeap shape), read it back through the pointer.
        byte[] arr = new byte[8];
        for (int i = 0; i < arr.Length; i++)
            arr[i] = (byte)(i * 7 + 3);
        GCHandle h = GCHandle.Alloc(arr, GCHandleType.Pinned);
        Console.WriteLine("IsAllocated=" + h.IsAllocated);
        byte* bp = (byte*)h.AddrOfPinnedObject();
        Console.Write("read-back:");
        for (int i = 0; i < arr.Length; i++)
            Console.Write(" " + bp[i]);
        Console.WriteLine();

        // 2. Pin address is stable across repeated AddrOfPinnedObject (non-moving).
        byte* bp2 = (byte*)h.AddrOfPinnedObject();
        Console.WriteLine("stable=" + (bp == bp2));

        // 3. Write THROUGH the pin, observe in the managed array (same backing store).
        bp[2] = 200;
        bp[5] = 111;
        Console.WriteLine("arr[2]=" + arr[2] + " arr[5]=" + arr[5]);

        // 4. Write the managed array, observe through the pin (round trip the other way).
        arr[0] = 42;
        Console.WriteLine("pin[0]=" + bp[0]);

        // 5. get_Target round-trips the same object.
        byte[] back = (byte[])h.Target;
        Console.WriteLine("target-same=" + ReferenceEquals(back, arr) + " len=" + back.Length);

        // 6. Free — identity pinning, releases nothing; the array is still usable.
        h.Free();
        Console.WriteLine("after-free arr[2]=" + arr[2]);

        // 7. int[] (Dn2CppArrayI4 rep) pin: element 0 address reads/writes the int.
        int[] ints = new int[4] { 1000, 2000, 3000, 4000 };
        GCHandle hi = GCHandle.Alloc(ints, GCHandleType.Pinned);
        int* ip = (int*)hi.AddrOfPinnedObject();
        Console.WriteLine("int pin[0]=" + ip[0] + " pin[3]=" + ip[3]);
        ip[1] = 9999;
        Console.WriteLine("ints[1]=" + ints[1]);
        hi.Free();

        // 8. double[] (Dn2CppArrayN rep, 8-byte elements) pin.
        double[] ds = new double[3] { 1.5, 2.5, 3.5 };
        GCHandle hd = GCHandle.Alloc(ds, GCHandleType.Pinned);
        double* dp = (double*)hd.AddrOfPinnedObject();
        Console.WriteLine("double pin[0]=" + dp[0] + " pin[2]=" + dp[2]);
        hd.Free();

        // 9. Default (type-less) Alloc is GCHandleType.Normal: a strong handle whose
        //    Target round-trips the object and IsAllocated is true.
        object o = "hello";
        GCHandle hw = GCHandle.Alloc(o);
        Console.WriteLine("normal-target=" + (string)hw.Target + " alloc=" + hw.IsAllocated);
        hw.Free();

        // 10. Free really invalidates the handle (matches real .NET, no longer a no-op):
        //     IsAllocated goes false, Target throws InvalidOperationException, and a
        //     second Free throws too. The receiver is a local, so Free writes back
        //     through its address and the invalidation is observable here.
        byte[] fa = new byte[4];
        GCHandle fh = GCHandle.Alloc(fa, GCHandleType.Pinned);
        fh.Free();
        Console.WriteLine("after-free IsAllocated=" + fh.IsAllocated);
        try { object _ = fh.Target; Console.WriteLine("target-after-free=NOTHROW"); }
        catch (InvalidOperationException) { Console.WriteLine("target-after-free=InvalidOperationException"); }
        try { fh.Free(); Console.WriteLine("double-free=NOTHROW"); }
        catch (InvalidOperationException) { Console.WriteLine("double-free=InvalidOperationException"); }

        // 11. Weak / WeakTrackResurrection now really route through the weak table (the
        //     kind is no longer advisory). While a strong reference keeps the target
        //     alive, Target round-trips it deterministically; after Free the slot is gone.
        object wobj = new int[3] { 5, 6, 7 };
        GCHandle wh = GCHandle.Alloc(wobj, GCHandleType.Weak);
        Console.WriteLine("weak IsAllocated=" + wh.IsAllocated
            + " target-same=" + ReferenceEquals(wh.Target, wobj));
        GC.KeepAlive(wobj);
        wh.Free();
        Console.WriteLine("weak after-free IsAllocated=" + wh.IsAllocated);

        object wrobj = new int[2] { 8, 9 };
        GCHandle wrh = GCHandle.Alloc(wrobj, GCHandleType.WeakTrackResurrection);
        Console.WriteLine("weaktrack target-same=" + ReferenceEquals(wrh.Target, wrobj));
        GC.KeepAlive(wrobj);
        wrh.Free();

        // 12. ToIntPtr / FromIntPtr (and the equivalent explicit cast operators) round-trip
        //     a Pinned handle through an opaque IntPtr. The token value is
        //     non-deterministic (an address), so it is never printed — only that the
        //     round-tripped handle reads back the same target, pin address, and
        //     allocation state (section 16 asserts the token's stability).
        int[] rt = new int[3] { 100, 200, 300 };
        GCHandle rh = GCHandle.Alloc(rt, GCHandleType.Pinned);
        IntPtr token = GCHandle.ToIntPtr(rh);
        GCHandle rh2 = GCHandle.FromIntPtr(token);
        Console.WriteLine("roundtrip target-same=" + ReferenceEquals(rh2.Target, rt)
            + " addr-same=" + (rh.AddrOfPinnedObject() == rh2.AddrOfPinnedObject())
            + " alloc=" + rh2.IsAllocated);
        IntPtr token2 = (IntPtr)rh;         // op_Explicit(GCHandle) -> IntPtr
        GCHandle rh3 = (GCHandle)token2;    // op_Explicit(IntPtr) -> GCHandle
        Console.WriteLine("cast target-same=" + ReferenceEquals(rh3.Target, rt));
        rh.Free();
        GC.KeepAlive(rt);

        // 13. Aggregate weakness check — the deterministic per-object sections above can't
        //     observe a single target's COLLECTION (a conservative GC may retain any one),
        //     so mirror WeakReferenceMemorySubset: allocate many weak handles whose targets
        //     have no strong reference, force a full collection, and confirm the heap stays
        //     far below "every target survived". If weak handles wrongly retained their
        //     targets (the earlier "handle is a scanned cell" behavior), the payloads would
        //     all survive and this would print false; real .NET prints true, so must dn2cpp.
        GCHandle[] handles = MakeWeakHandles();
        long used = GC.GetTotalMemory(true);
        long everythingSurvivedThreshold = (long)WeakCount * WeakPayloadBytes / 4; // 25% margin
        Console.WriteLine("heap bounded after " + WeakCount + " weak GCHandle allocations: "
            + (used < everythingSurvivedThreshold));
        foreach (GCHandle wkh in handles)
            wkh.Free(); // release the handle slots (raw GCHandles are not GC-managed)
        GC.KeepAlive(handles);

        // 14. Free propagates through struct copies via the shared slot (real .NET
        //     invalidates the handle-table slot, so every copy sees it). The copy Free
        //     was called on zeroes itself; the OTHER copy still reads IsAllocated=true
        //     (its word is non-zero, like real .NET's stale _handle), but its Target is
        //     null without throwing and its Free is a no-op; re-Free of the zeroed copy
        //     throws. All measured on real .NET (net10.0).
        object cobj = new int[3] { 11, 12, 13 };
        GCHandle ch = GCHandle.Alloc(cobj);
        GCHandle ch2 = ch; // struct copy of the live handle
        ch.Free();
        Console.WriteLine("copy-free h=" + ch.IsAllocated + " copy=" + ch2.IsAllocated
            + " copy-target-null=" + (ch2.Target == null));
        try { ch2.Free(); Console.WriteLine("copy-refree=NOTHROW"); }
        catch (InvalidOperationException) { Console.WriteLine("copy-refree=InvalidOperationException"); }
        try { ch.Free(); Console.WriteLine("zeroed-refree=NOTHROW"); }
        catch (InvalidOperationException) { Console.WriteLine("zeroed-refree=InvalidOperationException"); }
        GC.KeepAlive(cobj);

        // 15. Same propagation through a ToIntPtr/FromIntPtr round trip: freeing the
        //     round-tripped handle invalidates the original (they share the slot).
        object fobj = new int[2] { 21, 22 };
        GCHandle fh1 = GCHandle.Alloc(fobj);
        GCHandle fh2 = GCHandle.FromIntPtr(GCHandle.ToIntPtr(fh1));
        fh2.Free();
        Console.WriteLine("intptr-free orig-alloc=" + fh1.IsAllocated
            + " orig-target-null=" + (fh1.Target == null));
        GC.KeepAlive(fobj);

        // 16. ToIntPtr is stable — the same token across repeated calls and across a
        //     collection (real .NET returns the handle-table slot; dn2cpp the cell
        //     address). The values themselves are never printed.
        int[] sarr = new int[2] { 31, 32 };
        GCHandle sh = GCHandle.Alloc(sarr, GCHandleType.Pinned);
        IntPtr st1 = GCHandle.ToIntPtr(sh);
        GC.Collect();
        IntPtr st2 = GCHandle.ToIntPtr(sh);
        Console.WriteLine("tointptr-stable=" + (st1 == st2));
        sh.Free();
        GC.KeepAlive(sarr);

        // 17. Native-side-only retention: a Normal handle allocated in a helper whose
        //     struct (and every managed reference to the target) is dead — the ONLY
        //     surviving representation is the ToIntPtr value, stored bit-complemented in
        //     a static long so conservative scanning cannot root anything through it
        //     (real interop parks the token in native memory the same way). Stomp the
        //     stack, force collections, and storm small pointer-containing allocations
        //     so any wrongly-freed block is actually reused before the token is
        //     rehydrated; the handle must still reach a live, intact target.
        MakeNativeOnlyHandle();
        long nsink = Stomp(128);
        for (int i = 0; i < 4; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            for (int j = 0; j < 8192; j++)
            {
                var blob = new Blob();
                blob.A = s_occupy;
                blob.B = cobj;
                s_occupy = blob;
            }
        }
        GCHandle nh = GCHandle.FromIntPtr((IntPtr)(~s_hiddenToken));
        int[] nt = (int[])nh.Target;
        Console.WriteLine("native-only alive="
            + (nt[0] == 41 && nt[1] == 42 && nt[2] == 43 && nt[3] == 44)
            + " sink=" + (nsink > 0));
        nh.Free();

        // 18. Exception behaviors measured on real .NET: FromIntPtr(0) and every
        //     Target/Free on the zeroed receiver throw InvalidOperationException
        //     ("Handle is not initialized."), AddrOfPinnedObject on a live non-Pinned
        //     handle throws InvalidOperationException ("Handle is not pinned."), and
        //     ToIntPtr on a zeroed handle returns 0 WITHOUT throwing.
        try { GCHandle.FromIntPtr(IntPtr.Zero); Console.WriteLine("fromzero=NOTHROW"); }
        catch (InvalidOperationException e) { Console.WriteLine("fromzero=InvalidOperationException:" + e.Message); }
        byte[] narr = new byte[2];
        GCHandle hnorm = GCHandle.Alloc(narr); // Normal: not pinned
        try { hnorm.AddrOfPinnedObject(); Console.WriteLine("addr-normal=NOTHROW"); }
        catch (InvalidOperationException e) { Console.WriteLine("addr-normal=InvalidOperationException:" + e.Message); }
        hnorm.Free();
        GCHandle hwk = GCHandle.Alloc(narr, GCHandleType.Weak);
        try { hwk.AddrOfPinnedObject(); Console.WriteLine("addr-weak=NOTHROW"); }
        catch (InvalidOperationException e) { Console.WriteLine("addr-weak=InvalidOperationException:" + e.Message); }
        hwk.Free();
        GCHandle zh = GCHandle.Alloc(narr, GCHandleType.Pinned);
        zh.Free();
        try { zh.AddrOfPinnedObject(); Console.WriteLine("addr-freed=NOTHROW"); }
        catch (InvalidOperationException e) { Console.WriteLine("addr-freed=InvalidOperationException:" + e.Message); }
        try { zh.Target = "x"; Console.WriteLine("settarget-freed=NOTHROW"); }
        catch (InvalidOperationException e) { Console.WriteLine("settarget-freed=InvalidOperationException:" + e.Message); }
        Console.WriteLine("freed-tointptr-zero=" + (GCHandle.ToIntPtr(zh) == IntPtr.Zero));
        GC.KeepAlive(narr);

        // 19. Target setter: writes the shared slot (visible through every copy) and on
        //     a Pinned handle re-pins — AddrOfPinnedObject then reports the NEW
        //     referent's data (measured: real .NET allows the set on Pinned).
        object ta = "alpha";
        object tb = "beta";
        GCHandle th = GCHandle.Alloc(ta);
        GCHandle th2 = th;
        th.Target = tb;
        Console.WriteLine("settarget-propagates=" + ReferenceEquals(th2.Target, tb));
        th.Free();
        byte[] pa = new byte[3] { 1, 2, 3 };
        byte[] pb = new byte[3] { 9, 8, 7 };
        GCHandle ph = GCHandle.Alloc(pa, GCHandleType.Pinned);
        ph.Target = pb;
        byte* rp = (byte*)ph.AddrOfPinnedObject();
        Console.WriteLine("repin=" + rp[0] + "," + rp[1] + "," + rp[2]);
        ph.Free();
        GC.KeepAlive(pa);
        GC.KeepAlive(pb);

        // 20. GetHashCode: hashes the raw handle value (real .NET: the IntPtr slot),
        //     so a copy of a handle hashes like the original, a ToIntPtr/FromIntPtr
        //     round-trip preserves the hash, distinct live handles hash apart, and
        //     default(GCHandle) hashes to 0. Raw hash VALUES are addresses (non-
        //     deterministic), so only these relations are printed.
        object ga = new object();
        object gb = new object();
        GCHandle gh1 = GCHandle.Alloc(ga);
        GCHandle gh2 = GCHandle.Alloc(gb);
        GCHandle gh1Copy = gh1;
        Console.WriteLine("hash-nonzero=" + (gh1.GetHashCode() != 0));
        Console.WriteLine("hash-copy-equal=" + (gh1.GetHashCode() == gh1Copy.GetHashCode()));
        Console.WriteLine("hash-roundtrip-equal="
            + (GCHandle.FromIntPtr(GCHandle.ToIntPtr(gh1)).GetHashCode() == gh1.GetHashCode()));
        Console.WriteLine("hash-distinct=" + (gh1.GetHashCode() != gh2.GetHashCode()));
        Console.WriteLine("hash-default-zero=" + (default(GCHandle).GetHashCode() == 0));
        gh1.Free();
        gh2.Free();
        GC.KeepAlive(ga);
        GC.KeepAlive(gb);

        GCHandle empty = default;
        Console.WriteLine($"gchandle tostring={empty.ToString()}|{(object)empty}|{empty}");
    }

    static long s_hiddenToken; // ~ToIntPtr of section 17's handle: not a pointer bit
                               // pattern, so it roots nothing under conservative scanning
    static object s_occupy;    // keeps the post-collect allocation storm reachable

    // Small object with reference fields: it lands in the same (small,
    // pointer-containing) allocation classes as section 17's handle bookkeeping and
    // payload, so the storm actually recycles any block a collection freed by mistake.
    sealed class Blob
    {
        public object A;
        public object B;
    }

    // Deep recursion whose frames overwrite the stack region used by the allocating
    // code above, so conservative stack scanning cannot keep dead values alive by
    // accident through stale slots.
    static long Stomp(int depth)
    {
        if (depth <= 0)
            return 1;
        long a = depth;
        long b = depth * 2;
        long c = depth * 3;
        long d = depth * 4;
        return a + b + c + d + Stomp(depth - 1);
    }

    // Built in a helper (not Main) so neither the GCHandle struct nor any reference to
    // the payload survives on Main's frame — after this returns, the complemented
    // token in s_hiddenToken is the handle's only surviving representation.
    static void MakeNativeOnlyHandle()
    {
        var payload = new int[4] { 41, 42, 43, 44 };
        GCHandle h = GCHandle.Alloc(payload); // Normal: must root payload by itself
        s_hiddenToken = ~(long)GCHandle.ToIntPtr(h);
    }

    private const int WeakCount = 50_000;
    private const int WeakPayloadBytes = 4096;

    // Built in a helper (not Main) so no strong reference to any target byte[] lingers on
    // Main's native stack across the GC.GetTotalMemory(true) below — same conservative-GC
    // reasoning as WeakReferenceMemorySubset. Each handle stores only the weak cell (no
    // strong target), so the payloads are reachable solely through the weak links.
    private static GCHandle[] MakeWeakHandles()
    {
        var handles = new GCHandle[WeakCount];
        for (int i = 0; i < WeakCount; i++)
            handles[i] = GCHandle.Alloc(new byte[WeakPayloadBytes], GCHandleType.Weak);
        return handles;
    }
}
