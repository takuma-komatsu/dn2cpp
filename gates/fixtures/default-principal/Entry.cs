using System;
using System.Runtime.InteropServices;

internal static class Entry
{
    // Unrewritten, the probe takes a typed GCHandle member's address, which the
    // transpiler refuses. The principal rewrite moves the entry point away.
    private static void Main()
    {
        Func<GCHandle<object>, IntPtr> address = GCHandle<object>.ToIntPtr;
        GC.KeepAlive(address);
        throw new InvalidOperationException("native-only rewritten fixture required");
    }
}
