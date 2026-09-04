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
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void DropAttached(SafeWaitHandle donorSafe)
    {
        var attached = new AttachedWaitHandle(donorSafe);
        GC.SuppressFinalize(attached);
        GC.KeepAlive(attached);
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
