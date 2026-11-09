using System;
using System.Numerics;
using System.Runtime.Intrinsics;

namespace VectorProbe;

internal static unsafe class BlockRepetition
{
    private static int checks;

    public static void Run()
    {
        Console.WriteLine("-- vector block repetition and narrowing --");
        Vector512<byte> wide = default;
        for (int i = 0; i < 64; i++) ((byte*)&wide)[i] = Pattern(i);
        Vector<byte> narrow = Vector512.AsVector(wide);
        Check((byte*)&narrow, 16, 64);
        Vector256<byte> middle = Seed256<byte>();
        narrow = Vector256.AsVector(middle);
        Check((byte*)&narrow, 16, 32);
        ((byte*)&middle)[0] ^= 255;
        Check((byte*)&narrow, 16, 32);
        Console.WriteLine("block repeat and low-byte checks=" + checks);
    }

    private static byte Pattern(int index) => (byte)(7 + index * 29);
    private static Vector256<T> Seed256<T>() where T : unmanaged
    {
        Vector256<T> value = default;
        for (int i = 0; i < 32; i++) ((byte*)&value)[i] = Pattern(i);
        return value;
    }
    private static void Check(byte* bytes, int length, int block)
    {
        for (int i = 0; i < length; i++)
            if (bytes[i] != Pattern(i % block)) throw new Exception("vector block byte mismatch at " + i);
        checks++;
    }
}
