#nullable enable
using System;
using System.Runtime.InteropServices;

// LPWStr is a NUL-terminated UTF-16 buffer, which is already Dn2CppString's internal
// representation, so nothing transcodes in either direction. 𝄞 (U+1D11E) is here because
// it is a surrogate pair, i.e. two code units.
namespace PInvokeWideStringSubset;

internal static class Program
{
    [DllImport("dn2cpptest", CharSet = CharSet.Unicode)]
    private static extern int dn2cpptest_wstr_len(string s);
    [DllImport("dn2cpptest", CharSet = CharSet.Unicode)]
    private static extern int dn2cpptest_wstr_first(string s);
    [DllImport("dn2cpptest", CharSet = CharSet.Unicode)]
    private static extern string dn2cpptest_wstr_make();
    [DllImport("dn2cpptest", CharSet = CharSet.Unicode)]
    private static extern string dn2cpptest_wstr_echo(string s);

    internal static void __GateEntry()
    {
        // Code-unit lengths.
        Console.WriteLine(dn2cpptest_wstr_len("café"));    // 4
        Console.WriteLine(dn2cpptest_wstr_len("A☺"));      // 2
        Console.WriteLine(dn2cpptest_wstr_len("𝄞"));       // 2
        Console.WriteLine(dn2cpptest_wstr_len(""));         // 0
        // The first code unit is 9786, not a UTF-8 lead byte.
        Console.WriteLine(dn2cpptest_wstr_first("☺abc"));   // 9786

        // The returned native buffer is decoded and freed.
        string r = dn2cpptest_wstr_make();                  // "Hi" + U+263A
        Console.WriteLine(r.Length);                        // 3
        Console.WriteLine((int)r[0]);                       // 72
        Console.WriteLine((int)r[2]);                       // 9786

        // A full round trip must preserve the value.
        string e = dn2cpptest_wstr_echo("café☺");
        Console.WriteLine(e.Length);                        // 5
        Console.WriteLine(e == "café☺");                    // True
    }
}
