using System;
using System.Numerics;
using System.Runtime.Intrinsics;

namespace VectorProbe;

internal static unsafe class BlockRepetition
{
    private static int checks;
    private static int productions;

    public static void Run()
    {
        Console.WriteLine("-- vector block repetition and narrowing --");
        CheckRepeats<byte>();
        CheckRepeats<float>();
        CheckRepeats<nint>();
        productions = 0;
        Vector512<byte> once = Vector512.Create(Produce());
        Check((byte*)&once, 64, 16);
        if (productions != 1) throw new Exception("block source evaluated twice");
        Vector128<byte> source = Seed128<byte>();
        Vector512<byte> copy = Vector512.Create(source);
        ((byte*)&source)[0] ^= 255;
        Check((byte*)&copy, 64, 16);
        source = Seed128<byte>();
        copy = Vector512.Create(source);
        ((byte*)&copy)[1] ^= 255;
        Check((byte*)&source, 16, 16);
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
    private static Vector128<byte> Produce() { productions++; return Seed128<byte>(); }

    private static Vector64<T> Seed64<T>() where T : unmanaged
    {
        Vector64<T> value = default;
        for (int i = 0; i < 8; i++) ((byte*)&value)[i] = Pattern(i);
        return value;
    }
    private static Vector128<T> Seed128<T>() where T : unmanaged
    {
        Vector128<T> value = default;
        for (int i = 0; i < 16; i++) ((byte*)&value)[i] = Pattern(i);
        return value;
    }
    private static Vector256<T> Seed256<T>() where T : unmanaged
    {
        Vector256<T> value = default;
        for (int i = 0; i < 32; i++) ((byte*)&value)[i] = Pattern(i);
        return value;
    }
    private static void CheckRepeats<T>() where T : unmanaged
    {
        Vector64<T> small = Seed64<T>();
        Vector128<T> medium = Seed128<T>();
        Vector256<T> large = Seed256<T>();
        Vector128<T> r128 = Vector128.Create(small);
        Check((byte*)&r128, 16, 8);
        Vector256<T> r256 = Vector256.Create(small);
        Check((byte*)&r256, 32, 8);
        r256 = Vector256.Create(medium);
        Check((byte*)&r256, 32, 16);
        Vector512<T> r512 = Vector512.Create(small);
        Check((byte*)&r512, 64, 8);
        r512 = Vector512.Create(medium);
        Check((byte*)&r512, 64, 16);
        r512 = Vector512.Create(large);
        Check((byte*)&r512, 64, 32);
    }
    private static void Check(byte* bytes, int length, int block)
    {
        for (int i = 0; i < length; i++)
            if (bytes[i] != Pattern(i % block)) throw new Exception("vector block byte mismatch at " + i);
        checks++;
    }
}
