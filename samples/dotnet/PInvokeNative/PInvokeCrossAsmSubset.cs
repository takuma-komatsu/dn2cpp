#nullable enable
using System;
using PInvokeRefLib;

// Every import this section reaches is declared in the referenced PInvokeRefLib assembly,
// not the app module. Referenced-assembly imports use the same lowering as app imports.
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
