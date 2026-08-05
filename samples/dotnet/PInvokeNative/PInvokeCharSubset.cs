#nullable enable
using System;
using System.Runtime.InteropServices;

// The default/Ansi CharSet is UTF-8 on Unix, so a managed char crosses as the FIRST byte
// of its UTF-8 encoding — lossy for non-ASCII — and a native byte decodes back as UTF-8,
// turning any 0x80-0xFF byte into U+FFFD. CharSet.Unicode is a lossless UTF-16 passthrough.
namespace PInvokeCharSubset;

internal static class Program
{
    // The two arg paths; the native echoes the received value as int32.
    [DllImport("dn2cpptest", CharSet = CharSet.Ansi)]
    private static extern int dn2cpptest_char_ansi_byte(char c);
    [DllImport("dn2cpptest", CharSet = CharSet.Unicode)]
    private static extern int dn2cpptest_char_uni_code(char c);
    // The two return paths.
    [DllImport("dn2cpptest", CharSet = CharSet.Ansi)]
    private static extern char dn2cpptest_char_ansi_make(int v);
    [DllImport("dn2cpptest", CharSet = CharSet.Unicode)]
    private static extern char dn2cpptest_char_uni_make(int v);

    internal static void __GateEntry()
    {
        // ASCII is identity; a non-ASCII char gives the first UTF-8 byte.
        Console.WriteLine(dn2cpptest_char_ansi_byte('A'));        // 65
        Console.WriteLine(dn2cpptest_char_ansi_byte('~'));        // 126
        Console.WriteLine(dn2cpptest_char_ansi_byte('é'));   // 195 (é)
        Console.WriteLine(dn2cpptest_char_ansi_byte('☺'));   // 226 (☺)

        Console.WriteLine(dn2cpptest_char_uni_code('A'));         // 65
        Console.WriteLine(dn2cpptest_char_uni_code('é'));    // 233 (é)
        Console.WriteLine(dn2cpptest_char_uni_code('☺'));    // 9786 (☺)

        // A lone 0x80-0xFF byte is an incomplete UTF-8 sequence.
        Console.WriteLine(dn2cpptest_char_ansi_make(66));         // B
        Console.WriteLine((int)dn2cpptest_char_ansi_make(226));   // 65533

        Console.WriteLine((int)dn2cpptest_char_uni_make(0x263A)); // 9786
    }
}
