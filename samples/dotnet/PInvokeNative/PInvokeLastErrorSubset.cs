#nullable enable
using System;
using System.Runtime.InteropServices;

// Under SetLastError the marshaller captures the platform's last-error slot — errno off
// Windows — into a per-thread cached slot immediately after the call. The capture must be
// read before any later P/Invoke overwrites it, so each read snapshots into a local first.
namespace PInvokeLastErrorSubset;

internal static class Program
{
    [DllImport("dn2cpptest", SetLastError = true)]
    private static extern int dn2cpptest_set_last_error(int e);

    internal static void __GateEntry()
    {
        int r = dn2cpptest_set_last_error(7);
        int e1 = Marshal.GetLastWin32Error();      // snapshot immediately
        Console.WriteLine(r);                       // 14
        Console.WriteLine(e1);                      // 7

        dn2cpptest_set_last_error(42);
        int e2 = Marshal.GetLastPInvokeError();    // same cached slot,.NET 6+ name
        Console.WriteLine(e2);                      // 42

        dn2cpptest_set_last_error(13);
        int e3 = Marshal.GetLastWin32Error();
        Console.WriteLine(e3);                      // 13

        // The setter writes the cached slot directly, with no native call.
        Marshal.SetLastPInvokeError(99);
        Console.WriteLine(Marshal.GetLastWin32Error()); // 99
    }
}
