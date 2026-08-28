using System;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32.SafeHandles;

// Type identity across the four CLR event types the runtime serves with one Dn2CppEvent.
// Each needs its own type-info: they stand in a real inheritance relation (MRE/ARE are
// sealed siblings under EventWaitHandle, ManualResetEventSlim is not a WaitHandle at all)
// that one shared handle would flatten into wrong `is` answers. SUBJECT: a reflection and
// casting invariant, not a synchronization one — do not prune this section by theme.
namespace EventTypeIdentity;

internal static class Program
{
    private sealed class AttachedWaitHandle : WaitHandle
    {
        internal AttachedWaitHandle(SafeWaitHandle handle)
        {
            SafeWaitHandle = handle;
        }

        protected override void Dispose(bool explicitDisposing)
        {
            if (explicitDisposing)
                SafeWaitHandle = null;
            base.Dispose(explicitDisposing);
        }
    }

    private static void Probe(string label, object o)
    {
        Console.WriteLine($"{label} type={o.GetType()}");
        Console.WriteLine($"{label} is  mre={o is ManualResetEvent} are={o is AutoResetEvent} "
            + $"ewh={o is EventWaitHandle} slim={o is ManualResetEventSlim} wh={o is WaitHandle}");
        Console.WriteLine($"{label} eq  mre={o.GetType() == typeof(ManualResetEvent)} "
            + $"are={o.GetType() == typeof(AutoResetEvent)} "
            + $"ewh={o.GetType() == typeof(EventWaitHandle)} "
            + $"slim={o.GetType() == typeof(ManualResetEventSlim)}");
        Console.WriteLine($"{label} base={o.GetType().BaseType}");
    }

    internal static void __GateEntry()
    {
        Console.WriteLine("== event type identity ==");

        Probe("mre ", new ManualResetEvent(false));
        Probe("are ", new AutoResetEvent(false));
        // Both reset modes of the BASE type: one CLR type (the mode is a ctor argument,
        // not a subclass), so the reset mode cannot stand in for the type-info.
        Probe("ewhM", new EventWaitHandle(false, EventResetMode.ManualReset));
        Probe("ewhA", new EventWaitHandle(false, EventResetMode.AutoReset));
        Probe("slim", new ManualResetEventSlim(false));

        // The static relations, so a wrong BASE pointer is named rather than merely implied.
        Console.WriteLine("rel mre.base=" + typeof(ManualResetEvent).BaseType);
        Console.WriteLine("rel are.base=" + typeof(AutoResetEvent).BaseType);
        Console.WriteLine("rel ewh.base=" + typeof(EventWaitHandle).BaseType);
        Console.WriteLine("rel slim.base=" + typeof(ManualResetEventSlim).BaseType);
        Console.WriteLine("rel ewh<-mre=" + typeof(EventWaitHandle).IsAssignableFrom(typeof(ManualResetEvent)));
        Console.WriteLine("rel ewh<-are=" + typeof(EventWaitHandle).IsAssignableFrom(typeof(AutoResetEvent)));
        Console.WriteLine("rel mre<-ewh=" + typeof(ManualResetEvent).IsAssignableFrom(typeof(EventWaitHandle)));
        Console.WriteLine("rel wh<-slim=" + typeof(WaitHandle).IsAssignableFrom(typeof(ManualResetEventSlim)));

        // Signalling must still work: the type-info and the event state are one memory
        // write apart in dn2cpp_event_new.
        var m = new ManualResetEvent(false);
        Console.WriteLine("work mre initial=" + m.WaitOne(0));
        m.Set();
        Console.WriteLine("work mre after-set=" + m.WaitOne(0) + " again=" + m.WaitOne(0));
        m.Reset();
        Console.WriteLine("work mre after-reset=" + m.WaitOne(0));

        var a = new AutoResetEvent(false);
        Console.WriteLine("work are initial=" + a.WaitOne(0));
        a.Set();
        // Auto-reset: the first wait consumes the signal, the second sees it gone.
        Console.WriteLine("work are after-set=" + a.WaitOne(0) + " again=" + a.WaitOne(0));

        var e = new EventWaitHandle(false, EventResetMode.AutoReset);
        e.Set();
        Console.WriteLine("work ewh auto=" + e.WaitOne(0) + " again=" + e.WaitOne(0));
        var e2 = new EventWaitHandle(false, EventResetMode.ManualReset);
        e2.Set();
        Console.WriteLine("work ewh manual=" + e2.WaitOne(0) + " again=" + e2.WaitOne(0));

        var s = new ManualResetEventSlim(false);
        Console.WriteLine("work slim initial=" + s.IsSet);
        s.Set();
        Console.WriteLine("work slim after-set=" + s.IsSet);

        // SafeWaitHandle is a distinct object even when it aliases this runtime's
        // condition-variable event. Reattaching that alias must redirect the target
        // WaitHandle to the donor's signal, as the OS-handle property does on .NET.
        SafeWaitHandle safe = m.SafeWaitHandle;
        Console.WriteLine("safe type=" + safe.GetType()
            + " invalid=" + safe.IsInvalid + " closed=" + safe.IsClosed);
        Console.WriteLine("safe stable=" + ReferenceEquals(safe, m.SafeWaitHandle)
            + " raw=" + (safe.DangerousGetHandle() != IntPtr.Zero));
        SafeWaitHandle emptySafe = Activator.CreateInstance<SafeWaitHandle>();
        Console.WriteLine("safe activator invalid=" + emptySafe.IsInvalid);
        var donor = new ManualResetEvent(true);
        var attached = new ManualResetEvent(false) { SafeWaitHandle = donor.SafeWaitHandle };
        Console.WriteLine("safe attached=" + attached.WaitOne(0));

        // Put the donor beside its alias. The runtime must resolve the alias before
        // scanning the donor itself: returning index 1 proves WaitAny read the target
        // object's stale local event instead of its attached SafeWaitHandle.
        var anyDonor = new ManualResetEvent(true);
        var anyAttached = new ManualResetEvent(false) { SafeWaitHandle = anyDonor.SafeWaitHandle };
        Console.WriteLine("safe waitany attached="
            + WaitHandle.WaitAny(new WaitHandle[] { anyAttached, anyDonor }));

        // The nullable setter clears the association. Its getter lazily supplies an
        // invalid wrapper rather than returning null, and that wrapper remains a normal
        // SafeHandle for close/ref-count operations.
        anyAttached.SafeWaitHandle = null!;
        SafeWaitHandle cleared = anyAttached.SafeWaitHandle!;
        Console.WriteLine("safe cleared nonnull=" + (cleared is not null)
            + " invalid=" + cleared!.IsInvalid + " closed=" + cleared.IsClosed);
        bool addRef = false;
        cleared.DangerousAddRef(ref addRef);
        cleared.DangerousRelease();
        Console.WriteLine("safe addref=" + addRef);
        cleared.Close();
        Console.WriteLine("safe close invalid=" + cleared.IsInvalid
            + " closed=" + cleared.IsClosed);

        // WaitHandle.Dispose owns the attached SafeWaitHandle lifetime. This is
        // observable without waiting on a disposed primitive (which would throw).
        var disposed = new ManualResetEvent(false);
        SafeWaitHandle disposedSafe = disposed.SafeWaitHandle;
        disposed.Dispose();
        Console.WriteLine("safe wait dispose closed=" + disposedSafe.IsClosed);

        // A managed WaitHandle subclass borrows the donor's managed-event alias.
        // Disposing the borrower detaches it; the donor remains independently usable.
        var subclassDonor = new ManualResetEvent(true);
        var subclass = new AttachedWaitHandle(subclassDonor.SafeWaitHandle);
        subclass.Dispose();
        Console.WriteLine("safe subclass dispose keeps donor=" + subclassDonor.WaitOne(0));

        object boxedSafe = donor.SafeWaitHandle;
        Console.WriteLine("safe boxed is base=" + (boxedSafe is SafeHandle));
        Console.WriteLine("safe boxed is zero-minus="
            + (boxedSafe is SafeHandleZeroOrMinusOneIsInvalid));
        Console.WriteLine("safe runtime base=" + donor.SafeWaitHandle.GetType().BaseType);
        SafeHandle baseSafe = donor.SafeWaitHandle;
        Console.WriteLine("safe base raw="
            + (baseSafe.DangerousGetHandle() == donor.SafeWaitHandle.DangerousGetHandle()));
        bool baseAddRef = false;
        baseSafe.DangerousAddRef(ref baseAddRef);
        baseSafe.DangerousRelease();
        Console.WriteLine("safe base addref=" + baseAddRef);

        var closedRaw = new SafeWaitHandle((IntPtr)123, ownsHandle: false);
        SafeHandle closedRawBase = closedRaw;
        closedRawBase.Close();
        Console.WriteLine("safe base close raw=" + closedRawBase.DangerousGetHandle().ToInt64()
            + " closed=" + closedRawBase.IsClosed + " invalid=" + closedRawBase.IsInvalid);

        var invalidated = new SafeWaitHandle((IntPtr)124, ownsHandle: false);
        SafeHandle invalidatedBase = invalidated;
        bool invalidatedAddRef = false;
        invalidatedBase.DangerousAddRef(ref invalidatedAddRef);
        invalidatedBase.SetHandleAsInvalid();
        invalidatedBase.DangerousRelease();
        Console.WriteLine("safe base invalidated raw="
            + invalidatedBase.DangerousGetHandle().ToInt64()
            + " closed=" + invalidatedBase.IsClosed
            + " invalid=" + invalidatedBase.IsInvalid
            + " addref=" + invalidatedAddRef);
    }
}
