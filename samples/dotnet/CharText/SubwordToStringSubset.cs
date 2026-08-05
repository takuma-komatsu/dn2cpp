#nullable disable
using System;

// Byte/SByte/Int16/UInt16 ToString.
// dn2cpp itself (CppEmitter.EmitBlobs) does `b.ToString("x2")` over a byte; the
// sub-word primitives are NOT intrinsic types, so without an emit interception
// + reachability cut their real bodies route through System.Number.Format* — the
// biggest remaining console-self-host cascade. This sample exercises the four
// sub-word types' parameterless ToString and ToString(format) across the format
// specifiers own-code and tests use (hex with width-correct two's-complement
// masking, decimal padding, grouping, general). Output diffs exact vs real.NET.
namespace SubwordToStringSubset;

internal static class Program
{
    // Mirrors the own-code shape: byte -> two-hex-digit string, joined.
    private static string Hex2(byte[] bs)
    {
        string r = "";
        for (int i = 0; i < bs.Length; i++)
            r += bs[i].ToString("x2");
        return r;
    }

    // Generic over a value type: `value.ToString` lowers to a `constrained. T
    // callvirt object::ToString`, exercising the constrained dispatch path
    // (IsToStringablePrimitive) for the same four sub-word primitives.
    private static string Gen<T>(T value) => value.ToString();

    internal static int __GateEntry()
    {
        // The own-code idiom: lowercase 2-digit hex over a blob.
        byte[] blob = { 0x00, 0x0f, 0x7f, 0x80, 0xc8, 0xff, 0x12, 0x34 };
        Console.WriteLine("blob=" + Hex2(blob));

        byte[] bvals = { 0, 1, 15, 127, 128, 200, 255 };
        foreach (byte v in bvals)
            Console.WriteLine($"byte {v}: [{v.ToString()}] x2=[{v.ToString("x2")}] X4=[{v.ToString("X4")}] D3=[{v.ToString("D3")}] N0=[{v.ToString("N0")}] G=[{v.ToString("G")}] x=[{v.ToString("x")}] X=[{v.ToString("X")}] e=[{v.ToString("")}]");

        sbyte[] sbvals = { 0, 1, 15, 127, -1, -128, -100 };
        foreach (sbyte v in sbvals)
            Console.WriteLine($"sbyte {v}: [{v.ToString()}] x2=[{v.ToString("x2")}] X4=[{v.ToString("X4")}] D3=[{v.ToString("D3")}] N0=[{v.ToString("N0")}] G=[{v.ToString("G")}] x=[{v.ToString("x")}] X=[{v.ToString("X")}] e=[{v.ToString("")}]");

        short[] shvals = { 0, 1, 255, 4660, 32767, -1, -32768, -100 };
        foreach (short v in shvals)
            Console.WriteLine($"short {v}: [{v.ToString()}] x2=[{v.ToString("x2")}] X4=[{v.ToString("X4")}] D3=[{v.ToString("D3")}] N0=[{v.ToString("N0")}] G=[{v.ToString("G")}] x=[{v.ToString("x")}] X=[{v.ToString("X")}] e=[{v.ToString("")}]");

        ushort[] usvals = { 0, 1, 255, 4660, 32768, 65535 };
        foreach (ushort v in usvals)
            Console.WriteLine($"ushort {v}: [{v.ToString()}] x2=[{v.ToString("x2")}] X4=[{v.ToString("X4")}] D3=[{v.ToString("D3")}] N0=[{v.ToString("N0")}] G=[{v.ToString("G")}] x=[{v.ToString("x")}] X=[{v.ToString("X")}] e=[{v.ToString("")}]");

        // Constrained `T.ToString` dispatch over each sub-word primitive.
        Console.WriteLine($"gen=[{Gen<byte>(200)}]/[{Gen<sbyte>(-100)}]/[{Gen<short>(-32768)}]/[{Gen<ushort>(65535)}]");

        return 0;
    }
}
