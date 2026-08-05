#nullable disable
using System;
using System.Runtime.InteropServices;

// Two runtime primitives the console self-host reaches.
//
// GC.SuppressFinalize is a no-op here — dn2cpp never runs finalizers — but its real body
// calls RuntimeHelpers.GetMethodTable, so the Dispose pattern is the reachable caller and
// must run without throwing.
//
// Marshal.GetLastPInvokeError reads the per-thread cached last-error slot, snapshotted
// right after the call because a later P/Invoke would overwrite it. The self-host reaches
// it only on the compile surface (SafeHandle.InternalRelease), so the app-facing form is
// exercised here for runtime exactness.
namespace RuntimePrimSubset;

internal static class Program
{
    [DllImport("dn2cpptest", SetLastError = true)]
    private static extern int dn2cpptest_set_last_error(int e);

    // Two IDisposable types, so SuppressFinalize is reached on more than one receiver.
    private sealed class Handle : IDisposable
    {
        public bool Disposed;
        public void Dispose()
        {
            Disposed = true;
            GC.SuppressFinalize(this);
        }
    }

    private sealed class Resource : IDisposable
    {
        private readonly int _id;
        public Resource(int id) => _id = id;
        public int Id => _id;
        public void Dispose() => GC.SuppressFinalize(this);
    }

    internal static void __GateEntry()
    {
        var h = new Handle();
        h.Dispose();
        Console.WriteLine("dispose1 " + h.Disposed);

        using (var r = new Resource(5))
            Console.WriteLine("dispose2 " + r.Id);

        // Twice on one object is still a no-op.
        var h2 = new Handle();
        GC.SuppressFinalize(h2);
        GC.SuppressFinalize(h2);
        Console.WriteLine("dispose3 ok");

        int r0 = dn2cpptest_set_last_error(7);
        int e0 = Marshal.GetLastPInvokeError();   // snapshot immediately
        Console.WriteLine(r0);                      // 14
        Console.WriteLine(e0);                      // 7

        dn2cpptest_set_last_error(42);
        int e1 = Marshal.GetLastPInvokeError();
        Console.WriteLine(e1);                      // 42
    }
}
