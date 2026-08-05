using System;
using System.Threading;

// The IDisposable INTERFACE mouth on the intrinsic threading primitives. They carry no
// per-class emitted type-info, so `using`, an interface-typed local and
// `is`/`(IDisposable)` all depend on the init prologue installing an interface-dispatch
// map — while the DIRECT Dispose() call works regardless, which is what hides a hole.
// Every value is read BEFORE disposal: real .NET's Dispose tears the primitive down
// where dn2cpp's is a no-op.
namespace DisposableIntrinsics;

internal static class Program
{
    // The interface mouth with no static receiver clue: the argument arrives as `object`,
    // so `is` is a real isinst against the type's interface rows, the cast a real
    // castclass, and Dispose can only dispatch through the installed map.
    private static void DisposeThroughObject(object o, string label)
    {
        Console.WriteLine(label + " is IDisposable: " + (o is IDisposable));
        var d = (IDisposable)o;
        d.Dispose();
    }

    public static void __GateEntry()
    {
        // --- CountdownEvent: `using`, the callvirt IDisposable::Dispose route ---
        using (var cde = new CountdownEvent(2))
        {
            cde.Signal();
            cde.Signal();
            cde.Wait();
            Console.WriteLine("cde signalled: " + cde.IsSet + " " + cde.CurrentCount);
        }

        // --- CountdownEvent: an interface-typed local, isolated from `using` sugar ---
        var cde2 = new CountdownEvent(1);
        cde2.Signal();
        Console.WriteLine("cde2 set: " + cde2.IsSet);
        IDisposable cdeD = cde2;
        cdeD.Dispose();

        // --- CountdownEvent: isinst + castclass off `object` ---
        var cde3 = new CountdownEvent(1);
        Console.WriteLine("cde3 count: " + cde3.CurrentCount);
        DisposeThroughObject(cde3, "cde3");

        // --- Barrier: one participant, so SignalAndWait completes the phase alone ---
        using (var bar = new Barrier(1))
        {
            bar.SignalAndWait();
            bar.SignalAndWait();
            Console.WriteLine("barrier phase: " + bar.CurrentPhaseNumber + " " + bar.ParticipantCount);
        }

        var bar2 = new Barrier(1);
        bar2.SignalAndWait();
        Console.WriteLine("barrier2 phase: " + bar2.CurrentPhaseNumber);
        IDisposable barD = bar2;
        barD.Dispose();

        var bar3 = new Barrier(1);
        Console.WriteLine("barrier3 participants: " + bar3.ParticipantCount);
        DisposeThroughObject(bar3, "barrier3");

        // --- ReaderWriterLockSlim: its Dispose is already a no-op at the intrinsic call
        //     site, so only the interface mouth is at risk. All three shapes of it. ---
        using (var rw = new ReaderWriterLockSlim())
        {
            rw.EnterReadLock();
            Console.WriteLine("rw read held: " + rw.CurrentReadCount);
            rw.ExitReadLock();
        }

        var rw2 = new ReaderWriterLockSlim();
        rw2.EnterWriteLock();
        rw2.ExitWriteLock();
        Console.WriteLine("rw2 write cycled: " + rw2.CurrentReadCount);
        IDisposable rwD = rw2;
        rwD.Dispose();

        var rw3 = new ReaderWriterLockSlim();
        Console.WriteLine("rw3 readers: " + rw3.CurrentReadCount);
        DisposeThroughObject(rw3, "rw3");
    }
}
