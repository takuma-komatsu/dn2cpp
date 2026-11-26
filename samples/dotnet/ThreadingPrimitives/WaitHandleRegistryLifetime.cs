using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace WaitHandleRegistryLifetime;

internal static class Program
{
    private sealed class AttachedWaitHandle : WaitHandle
    {
        internal int Sentinel;

        internal AttachedWaitHandle()
        {
        }

        internal AttachedWaitHandle(SafeWaitHandle safeHandle)
        {
            SafeWaitHandle = safeHandle;
        }
    }

    private sealed class DerivedEventWaitHandle : EventWaitHandle
    {
        internal int Sentinel;

        private DerivedEventWaitHandle()
            : base(false, EventResetMode.ManualReset)
        {
            Sentinel = 42;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void DropAttached(SafeWaitHandle donorSafe)
    {
        var attached = new AttachedWaitHandle(donorSafe);
        GC.SuppressFinalize(attached);
        GC.KeepAlive(attached);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static DerivedEventWaitHandle ConstructEvent(Type type) =>
        (DerivedEventWaitHandle)Activator.CreateInstance(type, nonPublic: true)!;

    internal static void RunConstructedEvent()
    {
        var derived = ConstructEvent(typeof(DerivedEventWaitHandle));
        var safe = derived.SafeWaitHandle;
        bool initial = derived.WaitOne(0);
        derived.Set();
        bool signaled = derived.WaitOne(0) && derived.WaitOne(0);
        derived.Reset();
        bool reset = derived.WaitOne(0);
        using var donor = new ManualResetEvent(true);
        derived.SafeWaitHandle = donor.SafeWaitHandle;
        bool reassigned = derived.WaitOne(0);
        derived.SafeWaitHandle = safe;
        derived.Close();
        Console.WriteLine("constructed event field=" + derived.Sentinel
            + " initial=" + initial + " signaled=" + signaled + " after reset=" + reset
            + " reassigned=" + reassigned + " closed=" + safe.IsClosed
            + " donor closed=" + donor.SafeWaitHandle.IsClosed);
    }

    internal static void RunDisposedEvent()
    {
        var targets = new WeakReference[2];
        // A joined thread leaves no stale event or wrapper on a scanned stack.
        var creator = new Thread(() =>
        {
            using var handle = new EventWaitHandle(false, EventResetMode.AutoReset);
            targets[0] = new WeakReference(handle);
            targets[1] = new WeakReference(handle.SafeWaitHandle);
        });
        creator.Start();
        creator.Join();
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        Console.WriteLine("disposed event collected=" + !targets[0].IsAlive
            + " safe=" + !targets[1].IsAlive);
    }

    internal static void __GateEntry()
    {
        Console.WriteLine("== wait handle key lifetime ==");

        var donor = new ManualResetEvent(false);
        SafeWaitHandle donorSafe = donor.SafeWaitHandle;
        DropAttached(donorSafe);
        for (int i = 0; i < 8; i++)
            GC.Collect();

        bool isolated = true;
        bool freshInvalid = true;
        bool freshOpen = true;
        bool directLayout = true;
        for (int i = 0; i < 4096; i++)
        {
            var fresh = new AttachedWaitHandle();
            fresh.Sentinel = 42;
            SafeWaitHandle freshSafe = fresh.SafeWaitHandle;
            isolated &= !ReferenceEquals(freshSafe, donorSafe);
            freshInvalid &= freshSafe.IsInvalid;
            freshOpen &= !freshSafe.IsClosed;
            directLayout &= fresh.Sentinel == 42;
        }

        Console.WriteLine("fresh isolated=" + isolated
            + " invalid=" + freshInvalid + " open=" + freshOpen);

        var derived = (DerivedEventWaitHandle)RuntimeHelpers.GetUninitializedObject(
            typeof(DerivedEventWaitHandle));
        derived.Sentinel = 42;
        SafeWaitHandle derivedSafe = derived.SafeWaitHandle;
        Console.WriteLine("layout direct=" + directLayout
            + " event-derived=" + (derived.Sentinel == 42)
            + " invalid=" + derivedSafe.IsInvalid + " open=" + !derivedSafe.IsClosed);
        GC.KeepAlive(donor);
    }
}
