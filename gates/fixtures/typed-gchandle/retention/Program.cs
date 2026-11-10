using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

internal static class Program
{
    private sealed class Payload { internal int Value; }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static IntPtr RootOnlyToken() => GCHandle<Payload>.ToIntPtr(new GCHandle<Payload>(new Payload { Value = 37 }));

    private static int Main()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

        var empty = default(GCHandle<Payload>);
        empty.Dispose();
        empty.Dispose();
        var zero = GCHandle<Payload>.FromIntPtr(IntPtr.Zero);
        if (empty.IsAllocated || zero.IsAllocated || !zero.Equals(empty) || zero.GetHashCode() != 0)
            return 1;
        if (zero.Equals((object)default(GCHandle<object>)))
            return 2;

        var target = new Payload { Value = 11 };
        var handle = new GCHandle<Payload>(target);
        var copy = handle;
        if (!handle.IsAllocated || handle.Target != target || !handle.Equals(copy)
            || !handle.Equals((object)copy) || handle.GetHashCode() != copy.GetHashCode())
            return 3;
        handle.Target = new Payload { Value = 23 };
        if (copy.Target.Value != 23)
            return 4;
        var token = GCHandle<Payload>.ToIntPtr(handle);
        var roundtrip = GCHandle<Payload>.FromIntPtr(token);
        if (!roundtrip.Equals(handle) || roundtrip.Target.Value != 23)
            return 5;
        var otherType = GCHandle<object>.FromIntPtr(token);
        if (handle.Equals((object)otherType))
            return 6;

        object boxed = handle;
        if (!boxed.Equals((object)copy) || boxed.GetHashCode() != handle.GetHashCode()
            || !((IEquatable<GCHandle<Payload>>)boxed).Equals(copy)
            || boxed is IEquatable<GCHandle<object>>)
            return 7;
        ((IDisposable)boxed).Dispose();
        ((IDisposable)boxed).Dispose();
        var disposed = (GCHandle<Payload>)boxed;
        if (disposed.IsAllocated || !disposed.Equals(empty) || disposed.GetHashCode() != 0
            || GCHandle<Payload>.ToIntPtr(disposed) != IntPtr.Zero || !handle.IsAllocated
            || !((IEquatable<GCHandle<Payload>>)boxed).Equals(empty))
            return 8;
        // The original copy now holds a released token and must not be used again.

        var nullHandle = new GCHandle<Payload>(null);
        if (!nullHandle.IsAllocated || nullHandle.Target != null)
            return 9;
        nullHandle.Target = target;
        if (nullHandle.Target != target)
            return 10;
        nullHandle.Dispose();
        nullHandle.Dispose();
        if (nullHandle.IsAllocated)
            return 11;

        var onlyToken = RootOnlyToken();
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var rooted = GCHandle<Payload>.FromIntPtr(onlyToken);
        if (rooted.Target.Value != 37)
            return 12;
        rooted.Dispose();
        Console.WriteLine("Typed GC handle retention end");
        return 0;
    }
}
