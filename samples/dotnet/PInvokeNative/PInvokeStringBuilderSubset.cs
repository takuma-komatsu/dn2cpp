#nullable enable
using System;
using System.Runtime.InteropServices;
using System.Text;

// A StringBuilder argument is a caller-allocated bidirectional buffer — the Win32
// GetWindowText idiom: the content is copied in NUL-terminated and the write-back replaces
// the builder. The native buffer is sized for sb.Capacity, so this section also pins
// dn2cpp's Capacity model against real .NET.
namespace PInvokeStringBuilderSubset;

internal static class Program
{
    [DllImport("dn2cpptest", CharSet = CharSet.Ansi)]
    private static extern int dn2cpptest_sb_inlen(StringBuilder sb);
    [DllImport("dn2cpptest", CharSet = CharSet.Ansi)]
    private static extern int dn2cpptest_sb_fill(StringBuilder sb, int cap);
    [DllImport("dn2cpptest", CharSet = CharSet.Ansi)]
    private static extern void dn2cpptest_sb_cafe(StringBuilder sb, int cap);
    [DllImport("dn2cpptest", CharSet = CharSet.Unicode)]
    private static extern int dn2cpptest_sb_winlen(StringBuilder sb);
    [DllImport("dn2cpptest", CharSet = CharSet.Unicode)]
    private static extern int dn2cpptest_sb_wfill(StringBuilder sb, int cap);

    internal static void __GateEntry()
    {
        // The ctor-seeded capacity IS the native buffer length, so it must match exactly.
        Console.WriteLine(new StringBuilder().Capacity);          // 16
        Console.WriteLine(new StringBuilder(0).Capacity);         // 16
        Console.WriteLine(new StringBuilder(8).Capacity);         // 8
        Console.WriteLine(new StringBuilder(100).Capacity);       // 100
        Console.WriteLine(new StringBuilder("abc").Capacity);     // 16
        Console.WriteLine(new StringBuilder("abc", 32).Capacity); // 32

        // Ansi is UTF-8 on Unix, so the native reads a byte length.
        var sb = new StringBuilder(64);
        sb.Append("hello");
        Console.WriteLine(dn2cpptest_sb_inlen(sb));               // 5

        var sb2 = new StringBuilder(64);
        sb2.Append("hi");
        int r = dn2cpptest_sb_fill(sb2, sb2.Capacity);
        Console.WriteLine(r);                                     // 2 (input length)
        Console.WriteLine(sb2.ToString());                       // HI+2
        Console.WriteLine(sb2.Length);                           // 4
        Console.WriteLine(sb2.Capacity);                         // 64 (unchanged)

        // The native writes multi-byte UTF-8, which must decode to 4 chars.
        var sb3 = new StringBuilder(64);
        dn2cpptest_sb_cafe(sb3, sb3.Capacity);
        Console.WriteLine(sb3.ToString());                       // café
        Console.WriteLine(sb3.Length);                           // 4
        Console.WriteLine((int)sb3.ToString()[3]);               // 233 (é = U+00E9)

        // Unicode counts UTF-16 code units, so ☺ counts as one.
        var sb4 = new StringBuilder(64);
        sb4.Append("ab☺");
        Console.WriteLine(dn2cpptest_sb_winlen(sb4));            // 3

        // The native upper-cases ASCII, and ☺ must survive intact.
        var sb5 = new StringBuilder(64);
        sb5.Append("hi☺");
        int r5 = dn2cpptest_sb_wfill(sb5, sb5.Capacity);
        Console.WriteLine(r5);                                   // 3 (input length)
        Console.WriteLine(sb5.ToString());                      // WHI☺
        Console.WriteLine((int)sb5.ToString()[3]);              // 9786 (☺ unchanged)
        Console.WriteLine(sb5.Length);                          // 4
    }
}
