#nullable enable
using System;
using System.Runtime.InteropServices;

// char[] under the default/Ansi CharSet crosses as the WHOLE array encoded, embedded NULs
// included, so its byte length differs from the element count. Real .NET sizes that buffer
// at the element count times the code page's max bytes per char and adds NO terminator of
// its own — on a single-byte ANSI code page the content fills it exactly — so only a byte
// index below the element count is guaranteed to be marshalled content. The default
// direction is [In]; only [In,Out]/[Out] decode back, up to the array length.
namespace PInvokeAnsiCharArraySubset;

internal static class Program
{
    [DllImport("dn2cpptest")] // default CharSet (Ansi = UTF-8 on Unix)
    private static extern int dn2cpptest_aarr_len(char[] a);
    [DllImport("dn2cpptest")]
    private static extern int dn2cpptest_aarr_byte(char[] a, int i);
    [DllImport("dn2cpptest")]
    private static extern int dn2cpptest_aarr_isnull(char[]? a);
    [DllImport("dn2cpptest")]
    private static extern void dn2cpptest_aarr_upper(char[] buf, int cap);
    [DllImport("dn2cpptest", EntryPoint = "dn2cpptest_aarr_upper")]
    private static extern void dn2cpptest_aarr_upper_inout([In, Out] char[] buf, int cap);
    [DllImport("dn2cpptest", EntryPoint = "dn2cpptest_aarr_cafe")]
    private static extern void dn2cpptest_aarr_cafe_out([Out] char[] buf, int cap);

    internal static void __GateEntry()
    {
        // No aarr_len here: a strlen means something only over an array carrying its own
        // NUL (emb below), and otherwise runs past the content into allocator garbage.
        // Every char encodes to at least one byte, so byte index i < Length is content.
        char[] ascii = { 'h', 'e', 'l', 'l', 'o' };
        Console.WriteLine(dn2cpptest_aarr_byte(ascii, 0));   // 'h' = 104
        Console.WriteLine(dn2cpptest_aarr_byte(ascii, 1));   // 'e' = 101
        Console.WriteLine(dn2cpptest_aarr_byte(ascii, 2));   // 'l' = 108
        Console.WriteLine(dn2cpptest_aarr_byte(ascii, 3));   // 'l' = 108
        Console.WriteLine(dn2cpptest_aarr_byte(ascii, 4));   // 'o' = 111

        // A char with no single-byte Ansi mapping is platform-defined: UTF-8 on Unix
        // (multi-byte, so the encoded run is longer than the array), best-fit-off CP_ACP
        // on Windows (unmappable -> the one default char '?'). Indices 0-2 sit inside both
        // platforms' content by the rule above; past it the buffer is scratch.
        char[] mb = { 'A', '☺', 'z' };
        Console.WriteLine(dn2cpptest_aarr_byte(mb, 0));   // 'A' = 65
        Console.WriteLine(dn2cpptest_aarr_byte(mb, 1));   // '?' on Windows; UTF-8 lead byte on Unix
        Console.WriteLine(dn2cpptest_aarr_byte(mb, 2));   // 'z' on Windows; UTF-8 continuation byte on Unix

        // The whole array is encoded, so the run is 0x41 0x00 0x42 and the native strlen
        // stops at the array's OWN NUL — real content, not padding past it.
        char[] emb = { 'A', '\0', 'B' };
        Console.WriteLine(dn2cpptest_aarr_len(emb));      // 1

        // null -> null pointer; empty array -> a valid non-null pointer.
        Console.WriteLine(dn2cpptest_aarr_isnull(null));        // 1
        Console.WriteLine(dn2cpptest_aarr_isnull(new char[0])); // 0
        // No aarr_len over an empty array either: with no content at all there is nothing
        // a strlen can stop at.

        // Default direction is [In]: the native uppercases its copy, not the array.
        char[] def = { 'a', 'b', 'c' };
        dn2cpptest_aarr_upper(def, def.Length);
        Console.WriteLine($"{(int)def[0]} {(int)def[1]} {(int)def[2]}"); // 97 98 99 (unchanged)

        // [In,Out], length-preserving, so the array decodes back.
        char[] io = { 'a', 'b', 'c' };
        dn2cpptest_aarr_upper_inout(io, io.Length);
        Console.WriteLine($"{(int)io[0]} {(int)io[1]} {(int)io[2]}"); // 65 66 67 (ABC)

        // [Out] multi-byte: nothing is copied in, and the write-back decodes the native's
        // raw UTF-8 bytes through the host's own narrow decoder, so the chars are
        // platform-defined. Only indices 0-4 are inspected; past them the two sides differ
        // on how far the decode ran, which is buffer sizing, not marshalled content.
        char[] outBuf = new char[8];
        for (int i = 0; i < outBuf.Length; i++) outBuf[i] = 'Z';
        dn2cpptest_aarr_cafe_out(outBuf, outBuf.Length);
        Console.WriteLine($"{(int)outBuf[0]} {(int)outBuf[1]} {(int)outBuf[2]} {(int)outBuf[3]} {(int)outBuf[4]}");
        // c a f é \0 -> 99 97 102 233 0 on Unix; on Windows the raw C3 A9 decodes through
        // the ANSI code page, so the last two chars are that code page's.
        Console.WriteLine(new string(outBuf, 0, 4)); // "café" on Unix; code-page-defined otherwise
    }
}
