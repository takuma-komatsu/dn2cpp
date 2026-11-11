using System;
using System.Numerics;
using System.Runtime.Intrinsics;

namespace VectorProbe;

internal static partial class Program
{
    private static void VectorCopyBounds()
    {
        // Count is under test: derive storage and the short span from the
        // target pointer size so an incorrect Count cannot bless an overrun.
        int lanes = 16 / IntPtr.Size;
        VectorValueAssert(Vector<nint>.Count == lanes && Vector<nuint>.Count == lanes, "native-int numerics counts");
        VectorValueAssert(Vector64<nint>.Count == 8 / IntPtr.Size
            && Vector64<nuint>.Count == 8 / IntPtr.Size, "native-int 64 counts");
        VectorValueAssert(Vector128<nint>.Count == lanes
            && Vector128<nuint>.Count == lanes, "native-int 128 counts");
        VectorValueAssert(Vector256<nint>.Count == 32 / IntPtr.Size
            && Vector256<nuint>.Count == 32 / IntPtr.Size, "native-int 256 counts");
        VectorValueAssert(Vector512<nint>.Count == 64 / IntPtr.Size
            && Vector512<nuint>.Count == 64 / IntPtr.Size, "native-int 512 counts");
        Vector128<nint> sequence = Vector128.CreateSequence<nint>(3, 2);
        for (int i = 0; i < lanes; i++)
            VectorValueAssert(sequence.GetElement(i) == 3 + 2 * i, "native-int sequence covers every lane");
        Console.WriteLine("native-int counts and sequence lanes passed");
        nint[] guarded = new nint[lanes + 2];
        for (int i = 0; i < guarded.Length; i++) guarded[i] = -91;
        Vector<nint> source = new Vector<nint>(17);
        VectorValueAssert(!source.TryCopyTo(guarded.AsSpan(1, lanes - 1)), "short native-int span must refuse");
        for (int i = 0; i < guarded.Length; i++)
            VectorValueAssert(guarded[i] == -91, "short native-int copy must not write");
        VectorValueAssert(source.TryCopyTo(guarded.AsSpan(1, lanes)), "exact native-int span must copy");
        VectorValueAssert(guarded[0] == -91 && guarded[lanes + 1] == -91, "native-int copy canaries");
        for (int i = 1; i <= lanes; i++)
            VectorValueAssert(guarded[i] == 17, "native-int copy covers every lane");
        Console.WriteLine("native-int short/exact span and canaries passed");

        // Raw-byte CopyTo checks bytes, not the source's element count.
        byte[] bytes = new byte[18];
        for (int i = 0; i < bytes.Length; i++) bytes[i] = 0xa5;
        Vector<int> words = new Vector<int>(0x12345678);
        bool refused = false;
        try { words.CopyTo(bytes.AsSpan(1, 15)); }
        catch (ArgumentException) { refused = true; }
        VectorValueAssert(refused, "short byte span must refuse");
        for (int i = 0; i < bytes.Length; i++)
            VectorValueAssert(bytes[i] == 0xa5, "short byte copy must not write");
        words.CopyTo(bytes.AsSpan(1, 16));
        VectorValueAssert(bytes[0] == 0xa5 && bytes[17] == 0xa5, "byte copy canaries");
        for (int i = 0; i < 16; i++)
            VectorValueAssert(bytes[i + 1] == (byte)(0x12345678U >> ((i % 4) * 8)), "byte copy payload");
        Console.WriteLine("vector byte-span short/exact copy and canaries passed");
    }
}
