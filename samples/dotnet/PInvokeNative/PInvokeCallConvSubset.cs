#nullable enable
using System;
using System.Runtime.InteropServices;

// The Unix target ABIs have exactly one C calling convention, so .NET collapses
// Cdecl/StdCall/Winapi/FastCall/ThisCall to it and the transpiler lowers every
// CallingConvention identically. One cdecl native is imported under three of them.
namespace PInvokeCallConvSubset;

internal static class Program
{
    [DllImport("dn2cpptest", EntryPoint = "dn2cpptest_cc_add", CallingConvention = CallingConvention.Cdecl)]
    private static extern int AddCdecl(int a, int b);
    [DllImport("dn2cpptest", EntryPoint = "dn2cpptest_cc_add", CallingConvention = CallingConvention.StdCall)]
    private static extern int AddStdCall(int a, int b);
    [DllImport("dn2cpptest", EntryPoint = "dn2cpptest_cc_add", CallingConvention = CallingConvention.Winapi)]
    private static extern int AddWinapi(int a, int b);

    internal static void __GateEntry()
    {
        Console.WriteLine(AddCdecl(2, 3));      // 5
        Console.WriteLine(AddStdCall(10, 20));  // 30 (StdCall == cdecl on Unix)
        Console.WriteLine(AddWinapi(100, 23));  // 123 (Winapi == cdecl on Unix)
    }
}
