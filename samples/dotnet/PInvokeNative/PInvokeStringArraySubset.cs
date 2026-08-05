#nullable enable
using System;
using System.Runtime.InteropServices;

// A string[] marshals as one NUL-terminated buffer pointer per element — char** under the
// default CharSet, char16_t** under Unicode — with the count carried by a separate
// argument. A null element or a null array is a null pointer; an empty array is non-null.
// The default direction is [In]; [In,Out] decodes each slot back and [Out] skips the input
// encoding, freeing each slot's native pointer to match .NET's ownership.
namespace PInvokeStringArraySubset;

internal static class Program
{
    [DllImport("dn2cpptest")] private static extern int dn2cpptest_sa_sumlen(string[] a, int n);
    [DllImport("dn2cpptest")] private static extern int dn2cpptest_sa_byte(string[] a, int i, int j);
    [DllImport("dn2cpptest")] private static extern int dn2cpptest_sa_isnull(string[]? a);
    [DllImport("dn2cpptest")] private static extern int dn2cpptest_sa_elem_isnull(string[] a, int i);
    [DllImport("dn2cpptest", CharSet = CharSet.Unicode)] private static extern int dn2cpptest_wsa_sumlen(string[] a, int n);
    [DllImport("dn2cpptest", CharSet = CharSet.Unicode)] private static extern int dn2cpptest_wsa_first(string[] a, int i);

    // One upcase function bound under two directions, so the pair pins the copy semantics.
    [DllImport("dn2cpptest", EntryPoint = "dn2cpptest_strarr_upcase_inout")]
    private static extern void sa_upcase_default(string[] a, int n);
    [DllImport("dn2cpptest", EntryPoint = "dn2cpptest_strarr_upcase_inout")]
    private static extern void sa_upcase_inout([In, Out] string[] a, int n);
    [DllImport("dn2cpptest")] private static extern void dn2cpptest_strarr_fill_out([Out] string[] a, int n);
    [DllImport("dn2cpptest", CharSet = CharSet.Unicode, EntryPoint = "dn2cpptest_wstrarr_upcase_inout")]
    private static extern void wsa_upcase_inout([In, Out] string[] a, int n);
    [DllImport("dn2cpptest", CharSet = CharSet.Unicode, EntryPoint = "dn2cpptest_wstrarr_fill_out")]
    private static extern void wsa_fill_out([Out] string[] a, int n);
    // The native replaces only even-index slots; the odd ones keep the marshalled IN
    // buffer, which in the transpiled build is a GC pointer the write-back must NOT free.
    [DllImport("dn2cpptest", EntryPoint = "dn2cpptest_strarr_upcase_even_inout")]
    private static extern void sa_upcase_even_inout([In, Out] string[] a, int n);
    [DllImport("dn2cpptest", CharSet = CharSet.Unicode, EntryPoint = "dn2cpptest_wstrarr_upcase_even_inout")]
    private static extern void wsa_upcase_even_inout([In, Out] string[] a, int n);

    internal static void __GateEntry()
    {
        string[] a = { "foo", "hello", "x" };
        Console.WriteLine(dn2cpptest_sa_sumlen(a, 3)); // 9

        // Multi-byte: "A☺" is 4 UTF-8 bytes.
        string[] b = { "A☺", "z" };
        Console.WriteLine(dn2cpptest_sa_sumlen(b, 2));  // 5
        Console.WriteLine(dn2cpptest_sa_byte(b, 0, 1)); // 0xE2 = 226

        Console.WriteLine(dn2cpptest_sa_isnull(null));        // 1
        Console.WriteLine(dn2cpptest_sa_isnull(new string[0])); // 0
        Console.WriteLine(dn2cpptest_sa_sumlen(new string[0], 0)); // 0

        string?[] c = { "p", null, "q" };
        Console.WriteLine(dn2cpptest_sa_sumlen(c!, 3));        // 1 + (-1) + 1 = 1
        Console.WriteLine(dn2cpptest_sa_elem_isnull(c!, 1));   // 1

        // CharSet.Unicode gives code-unit lengths over raw UTF-16.
        Console.WriteLine(dn2cpptest_wsa_sumlen(b, 2));  // 3
        Console.WriteLine(dn2cpptest_wsa_first(b, 0));   // 65

        // The default [In] copies in rather than pinning, so nothing writes back.
        string[] din = { "foo", "bar" };
        sa_upcase_default(din, din.Length);
        Console.WriteLine($"{din[0]} {din[1]}");        // foo bar (unchanged)

        // A null element stays a null pointer the native leaves alone, decoding back to null.
        string?[] io = { "foo", "Hi", null, "baz" };
        sa_upcase_inout(io!, io.Length);
        Console.WriteLine($"{io[0]} {io[1]} {(io[2] is null ? "<null>" : io[2])} {io[3]}"); // FOO HI <null> BAZ

        // [Out]: the native sees zeroed slots.
        string[] o = new string[3];
        dn2cpptest_strarr_fill_out(o, o.Length);
        Console.WriteLine($"{o[0]} {o[1]} {o[2]}");     // out0 out1 out2

        // The same two directions under CharSet.Unicode.
        string?[] wio = { "abc", "MiX", null };
        wsa_upcase_inout(wio!, wio.Length);
        Console.WriteLine($"{wio[0]} {wio[1]} {(wio[2] is null ? "<null>" : wio[2])}"); // ABC MIX <null>

        string[] wo = new string[2];
        wsa_fill_out(wo, wo.Length);
        Console.WriteLine($"{wo[0]} {wo[1]}");          // out0 out1

        string[] ev = { "aa", "bb", "cc", "dd" };
        sa_upcase_even_inout(ev, ev.Length);
        Console.WriteLine($"{ev[0]} {ev[1]} {ev[2]} {ev[3]}"); // AA bb CC dd

        string[] wev = { "wx", "yz", "pq" };
        wsa_upcase_even_inout(wev, wev.Length);
        Console.WriteLine($"{wev[0]} {wev[1]} {wev[2]}");      // WX yz PQ
    }
}
