#nullable enable
using System;
using System.Runtime.InteropServices;

// Under CharSet.Unicode a char[] is already the packed 2-byte storage the native wants, so
// it is passed by data pointer like a blittable array and both directions hit the managed
// buffer. The Ansi form takes its own encode/decode path — PInvokeAnsiCharArraySubset.
namespace PInvokeWideCharArraySubset;

internal static class Program
{
    [DllImport("dn2cpptest", CharSet = CharSet.Unicode)]
    private static extern int dn2cpptest_warr_elem(char[] a, int i);
    [DllImport("dn2cpptest", CharSet = CharSet.Unicode)]
    private static extern int dn2cpptest_warr_sum(char[]? a, int n);
    [DllImport("dn2cpptest", CharSet = CharSet.Unicode)]
    private static extern void dn2cpptest_warr_upper(char[] a, int n);

    internal static void __GateEntry()
    {
        // Each element is one UTF-16 code unit.
        char[] a = { 'A', '☺', 'z' };
        Console.WriteLine(dn2cpptest_warr_elem(a, 0));   // 65
        Console.WriteLine(dn2cpptest_warr_elem(a, 1));   // 9786
        Console.WriteLine(dn2cpptest_warr_elem(a, 2));   // 122
        Console.WriteLine(dn2cpptest_warr_sum(a, 3));    // 9973

        // The native upper-cases ASCII in place, and ☺ must survive intact.
        char[] b = { 'h', '☺', 'z' };
        dn2cpptest_warr_upper(b, 3);
        Console.WriteLine((int)b[0]);                    // 72 (H)
        Console.WriteLine((int)b[1]);                    // 9786 (☺ unchanged)
        Console.WriteLine((int)b[2]);                    // 90 (Z)

        // A null array is a null pointer; an empty one a valid non-faulting pointer.
        Console.WriteLine(dn2cpptest_warr_sum(null, 0));      // 0
        Console.WriteLine(dn2cpptest_warr_sum(new char[0], 0)); // 0
    }
}
