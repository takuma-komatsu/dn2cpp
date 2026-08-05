#nullable enable
using System;
using System.Runtime.InteropServices;

// System.Boolean default-marshals as a 4-byte Win32 BOOL: the caller sends exactly 0/1,
// and any non-zero return normalizes to managed true.
namespace PInvokeBoolSubset;

internal static class Program
{
    [DllImport("dn2cpptest")] private static extern bool dn2cpptest_bool_and(bool a, bool b);
    [DllImport("dn2cpptest")] private static extern bool dn2cpptest_bool_or(bool a, bool b);
    [DllImport("dn2cpptest")] private static extern bool dn2cpptest_bool_truthy();
    [DllImport("dn2cpptest")] private static extern bool dn2cpptest_bool_falsy();

    internal static void __GateEntry()
    {
        Console.WriteLine(dn2cpptest_bool_and(true, true));    // True
        Console.WriteLine(dn2cpptest_bool_and(true, false));   // False
        Console.WriteLine(dn2cpptest_bool_or(false, true));    // True

        Console.WriteLine(dn2cpptest_bool_truthy());           // True (42 normalized)
        Console.WriteLine(dn2cpptest_bool_falsy());            // False

        // A returned bool driving a managed condition.
        Console.WriteLine(dn2cpptest_bool_truthy() ? "yes" : "no"); // yes
    }
}
