#nullable enable
using System;
using System.Runtime.InteropServices;

// A [MarshalAs] on a parameter or return overrides the type+CharSet default: a string
// encoding whatever the method CharSet says, a bool width, and LPArray on a blittable
// array. Integer and enum width overrides are carved out — .NET requires the unmanaged
// width to match the managed integer's size — as are COM/BStr/SafeArray/FunctionPtr.
namespace PInvokeMarshalAsSubset;

internal static class Program
{
    // Arg encodings that beat the method CharSet, in both directions: an Ansi-default
    // method forced to UTF-16, and a Unicode method forced back to UTF-8.
    [DllImport("dn2cpptest", EntryPoint = "dn2cpptest_wstr_len")]
    private static extern int WLenForcedUtf16([MarshalAs(UnmanagedType.LPWStr)] string s);
    [DllImport("dn2cpptest", EntryPoint = "dn2cpptest_strlen", CharSet = CharSet.Unicode)]
    private static extern int ByteLenForcedUtf8([MarshalAs(UnmanagedType.LPStr)] string s);
    [DllImport("dn2cpptest", EntryPoint = "dn2cpptest_strlen")]
    private static extern int ByteLenUtf8([MarshalAs(UnmanagedType.LPUTF8Str)] string s);
    // The same both ways on the return.
    [DllImport("dn2cpptest", EntryPoint = "dn2cpptest_wstr_make")]
    [return: MarshalAs(UnmanagedType.LPWStr)]
    private static extern string MakeForcedUtf16();
    [DllImport("dn2cpptest", EntryPoint = "dn2cpptest_greeting", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.LPStr)]
    private static extern string GreetingForcedUtf8();

    // U1: a single 0/1 byte rather than the 4-byte Win32 BOOL; a non-1 truthy return
    // still normalizes to managed true.
    [DllImport("dn2cpptest", EntryPoint = "dn2cpptest_u1_and")]
    [return: MarshalAs(UnmanagedType.U1)]
    private static extern bool U1And([MarshalAs(UnmanagedType.U1)] bool a, [MarshalAs(UnmanagedType.U1)] bool b);
    [DllImport("dn2cpptest", EntryPoint = "dn2cpptest_u1_truthy")]
    [return: MarshalAs(UnmanagedType.U1)]
    private static extern bool U1Truthy();

    // LPArray on a blittable array is the default array marshalling.
    [DllImport("dn2cpptest", EntryPoint = "dn2cpptest_iarr_sum")]
    private static extern int LpArraySum([MarshalAs(UnmanagedType.LPArray)] int[] a, int n);

    internal static void __GateEntry()
    {
        Console.WriteLine(WLenForcedUtf16("café"));    // 4 (UTF-16 code units)
        Console.WriteLine(WLenForcedUtf16("A☺"));       // 2
        Console.WriteLine(ByteLenForcedUtf8("café"));   // 5 (é = 2 UTF-8 bytes)
        Console.WriteLine(ByteLenUtf8("héllo"));        // 6

        string u16 = MakeForcedUtf16();                 // "Hi" + U+263A
        Console.WriteLine(u16.Length);                  // 3
        Console.WriteLine((int)u16[2]);                 // 9786
        Console.WriteLine(GreetingForcedUtf8());        // dn2cpp says hi

        Console.WriteLine(U1And(true, true));           // True
        Console.WriteLine(U1And(true, false));          // False
        Console.WriteLine(U1Truthy());                  // True (7 normalized)

        int[] xs = { 4, 5, 6, 7 };
        Console.WriteLine(LpArraySum(xs, xs.Length));   // 22
    }
}
