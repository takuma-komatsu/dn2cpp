#nullable enable
using System;
using System.Runtime.InteropServices;

// Under the default CharSet a string marshals to and from a NUL-terminated UTF-8 buffer
// on Unix, and a string return is decoded back with its native buffer freed.
namespace PInvokeStringSubset;

internal static class Program
{
    [DllImport("dn2cpptest")]
    private static extern int dn2cpptest_strlen(string s);

    [DllImport("dn2cpptest")]
    private static extern string dn2cpptest_greeting();

    internal static void __GateEntry()
    {
        Console.WriteLine(dn2cpptest_strlen("hello"));     // 5
        // é is 2 UTF-8 bytes, so the byte length beats the 4-code-unit managed length.
        Console.WriteLine(dn2cpptest_strlen("café")); // 5

        string g = dn2cpptest_greeting();
        Console.WriteLine(g);                  // dn2cpp says hi
        Console.WriteLine(g.Length);           // 14
        Console.WriteLine(dn2cpptest_strlen(g)); // 14 (round trip back to native)
    }
}
