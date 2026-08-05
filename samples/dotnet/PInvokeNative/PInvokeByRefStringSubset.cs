#nullable disable
using System;
using System.Runtime.InteropServices;

// `out string` / `ref string` passes a pointer-to-pointer the native fills; the marshaller
// decodes it and frees the buffer. dn2cpp passes an intermediate void* temp and decodes
// back into the ByRef slot after the call, freeing it only when the native replaced it —
// the [In] buffer is GC memory. Ansi is UTF-8 on Unix; CharSet.Unicode is UTF-16.
namespace PInvokeByRefStringSubset;

internal static class Program
{
    [DllImport("dn2cpptest", CharSet = CharSet.Ansi)]
    private static extern void dn2cpptest_out_str(out string s);
    [DllImport("dn2cpptest", CharSet = CharSet.Unicode)]
    private static extern void dn2cpptest_out_wstr(out string s);
    [DllImport("dn2cpptest", CharSet = CharSet.Ansi)]
    private static extern void dn2cpptest_out_null(out string s);
    [DllImport("dn2cpptest", CharSet = CharSet.Ansi)]
    private static extern void dn2cpptest_ref_str(ref string s);
    [DllImport("dn2cpptest", CharSet = CharSet.Ansi)]
    private static extern void dn2cpptest_ref_keep(ref string s);

    internal static void __GateEntry()
    {
        dn2cpptest_out_str(out string a);
        Console.WriteLine(a);                  // native-made

        dn2cpptest_out_wstr(out string w);
        Console.WriteLine(w);                  // Ok☺
        Console.WriteLine(w.Length);           // 3

        dn2cpptest_out_null(out string n);
        Console.WriteLine(n is null);          // True

        // ref round-trips: the native reads the value in and replaces it.
        string r = "hello";
        dn2cpptest_ref_str(ref r);
        Console.WriteLine(r);                   // [hello:5]

        // A null managed string is seen as a null pointer in.
        string r2 = null;
        dn2cpptest_ref_str(ref r2);
        Console.WriteLine(r2);                  // [(null):6]

        // Left unchanged by the native, the caller keeps its input value.
        string r3 = "keep";
        dn2cpptest_ref_keep(ref r3);
        Console.WriteLine(r3);                  // keep
    }
}
