#nullable enable
using System;
using PInvokeRefLib;

// Every import this section reaches is declared in the referenced PInvokeRefLib assembly,
// not the app module, so it needs `--pinvoke-module dn2cpptest` to admit that module into
// the direct-native-call lowering. The gate's negative arm asserts the refusal without it.
namespace PInvokeCrossAsmSubset;

internal static class Program
{
    internal static void __GateEntry()
    {
        Console.WriteLine(NativeBridge.Add(20, 22));            // 42
        Console.WriteLine(NativeBridge.Mul(3_000_000L, 4000L)); // 12000000000
        Console.WriteLine(NativeBridge.Scale(2.5, 4));          // 10
        Console.WriteLine(NativeBridge.Utf8Length("héllo")); // 6 (é = 2 UTF-8 bytes)
    }
}
