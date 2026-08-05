#nullable enable
using System;
using System.Runtime.InteropServices;

// A single-pointer-field struct crossing BY VALUE in both directions: the singleton
// aggregate is ABI-flattened to one pointer register, and the managed side must see a
// one-IntPtr-field struct. The pointed-at buffer is a static literal, never freed.
namespace PInvokeSingleFieldStructSubset;

internal static class Program
{
    [StructLayout(LayoutKind.Sequential)]
    private struct NativeString { public IntPtr P; }

    [DllImport("dn2cpptest")]
    private static extern NativeString dn2cpptest_nstr_version();
    [DllImport("dn2cpptest")]
    private static extern int dn2cpptest_nstr_len(NativeString s);

    internal static void __GateEntry()
    {
        NativeString v = dn2cpptest_nstr_version();
        Console.WriteLine(Marshal.PtrToStringUTF8(v.P));    // dn2cpptest 1.2.3

        // The pointer the native handed out rides back in.
        Console.WriteLine(dn2cpptest_nstr_len(v));          // 16

        // A default struct carries a null pointer.
        Console.WriteLine(dn2cpptest_nstr_len(default));    // -1
    }
}
