#nullable enable
using System;
using System.Runtime.InteropServices;

// char[] under the default/Ansi CharSet is encoded as a NUL-terminated UTF-8 buffer
// holding the WHOLE array, embedded NULs included, so its byte length differs from the
// element count. The default direction is [In]; only [In,Out]/[Out] decode back, up to the
// array length, NUL-terminating after the content and leaving the rest unchanged.
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
        char[] ascii = { 'h', 'e', 'l', 'l', 'o' };
        Console.WriteLine(dn2cpptest_aarr_len(ascii));   // 5

        // Multi-byte: 0x41, 0xE2 0x98 0xBA, 0x7A = 5 bytes.
        char[] mb = { 'A', '☺', 'z' };
        Console.WriteLine(dn2cpptest_aarr_len(mb));       // 5
        Console.WriteLine(dn2cpptest_aarr_byte(mb, 0));   // 0x41 = 65
        Console.WriteLine(dn2cpptest_aarr_byte(mb, 1));   // 0xE2 = 226
        Console.WriteLine(dn2cpptest_aarr_byte(mb, 2));   // 0x98 = 152
        Console.WriteLine(dn2cpptest_aarr_byte(mb, 3));   // 0xBA = 186
        Console.WriteLine(dn2cpptest_aarr_byte(mb, 4));   // 0x7A = 122

        // The whole array is encoded, so the buffer is 0x41 0x00 0x42 0x00 and the
        // native strlen stops at the embedded NUL.
        char[] emb = { 'A', '\0', 'B' };
        Console.WriteLine(dn2cpptest_aarr_len(emb));      // 1

        // null -> null pointer; empty array -> a valid non-null pointer.
        Console.WriteLine(dn2cpptest_aarr_isnull(null));        // 1
        Console.WriteLine(dn2cpptest_aarr_isnull(new char[0])); // 0
        // No aarr_len over an empty array: real .NET's Ansi marshaller does not guarantee
        // a NUL terminator for a zero-length buffer on Windows, so strlen would read
        // uninitialized heap.

        // Default direction is [In]: the native uppercases its copy, not the array.
        char[] def = { 'a', 'b', 'c' };
        dn2cpptest_aarr_upper(def, def.Length);
        Console.WriteLine($"{(int)def[0]} {(int)def[1]} {(int)def[2]}"); // 97 98 99 (unchanged)

        // [In,Out], length-preserving, so the array decodes back.
        char[] io = { 'a', 'b', 'c' };
        dn2cpptest_aarr_upper_inout(io, io.Length);
        Console.WriteLine($"{(int)io[0]} {(int)io[1]} {(int)io[2]}"); // 65 66 67 (ABC)

        // [Out] multi-byte: nothing is copied in, and the write-back decodes and
        // NUL-terminates. Only the content and its terminator are inspected — the bytes
        // past the NUL are an artifact of the marshaller's internal buffer sizing.
        char[] outBuf = new char[8];
        for (int i = 0; i < outBuf.Length; i++) outBuf[i] = 'Z';
        dn2cpptest_aarr_cafe_out(outBuf, outBuf.Length);
        Console.WriteLine($"{(int)outBuf[0]} {(int)outBuf[1]} {(int)outBuf[2]} {(int)outBuf[3]} {(int)outBuf[4]}");
        // c a f é \0  -> 99 97 102 233 0
        Console.WriteLine(new string(outBuf, 0, 4)); // "café"
    }
}
