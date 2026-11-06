using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace VectorProbe;

internal static unsafe class ExplicitReinterprets
{
    private static int checks;
    private static int productions;

    public static void Run()
    {
        Console.WriteLine("-- vector explicit reinterprets --");
        if (Vector<byte>.Count != 16) throw new Exception("raw oracle requires a sixteen-byte CLR vector");
        byte[][] patterns = {
            new byte[] { 0x03,0x17,0x29,0x3b,0x4d,0x5f,0x61,0x73,0x85,0x97,0xa9,0xbb,0xcd,0xdf,0xe1,0xf3 },
            new byte[16],
            new byte[] { 255,255,255,255,255,255,255,255,255,255,255,255,255,255,255,255 },
            // Single: negative zero, payload NaN, infinity, minimum subnormal.
            new byte[] { 0,0,0,128,0x45,0x23,0xc1,0xff,0,0,128,127,1,0,0,0 },
            // Double: negative zero and signed payload NaN.
            new byte[] { 0,0,0,0,0,0,0,128,0x89,0x67,0x45,0x23,0x01,0,0xf8,0xff },
            // Double: infinity and minimum subnormal.
            new byte[] { 0,0,0,0,0,0,0xf0,0x7f,1,0,0,0,0,0,0,0 },
            // Signaling payloads transported without floating arithmetic.
            new byte[] { 0x45,0x23,0x81,0x7f,0,0,0,0,0x89,0x67,0x45,0x23,0x01,0,0xf0,0x7f }
        };
        foreach (byte[] pattern in patterns)
        {
            Source0(pattern);
            Source1(pattern);
            Source2(pattern);
            Source3(pattern);
            Source4(pattern);
            Source5(pattern);
            Source6(pattern);
            Source7(pattern);
            Source8(pattern);
            Source9(pattern);
            Source10(pattern);
            Source11(pattern);
        }
        byte[] bits = patterns[0];
        productions = 0;
        Check((Vector<byte>)Produce(bits), bits);
        if (productions != 1) throw new Exception("source evaluated more than once");
        Vector<uint> source = Seed<uint>(bits);
        Vector<byte> copy = (Vector<byte>)source;
        ((byte*)&source)[0] ^= 255;
        Check(copy, bits);
        source = Seed<uint>(bits);
        copy = (Vector<byte>)source;
        ((byte*)&copy)[1] ^= 255;
        Check(source, bits);
        delegate*<Vector<uint>, Vector<byte>> address = &Address;
        source = Seed<uint>(bits);
        copy = address(source);
        ((byte*)&source)[2] ^= 255;
        Check(copy, bits);
        source = Seed<uint>(bits);
        copy = address(source);
        ((byte*)&copy)[3] ^= 255;
        Check(source, bits);
        Console.WriteLine("explicit byte checks=" + checks);
        BlockRepetition.Run();
        RetainedFills();
        Console.WriteLine("vector explicit end");
    }

    private static Vector<uint> Produce(byte[] bytes) { productions++; return Seed<uint>(bytes); }
    private static Vector<byte> Address(Vector<uint> value) => (Vector<byte>)value;
    // The fixture replaces identity calls with the actual same-type operator.
    private static Vector<T> Identity<T>(Vector<T> value) where T : unmanaged => value;

    private static Vector<T> Seed<T>(byte[] bytes) where T : unmanaged
    {
        Vector<T> value = default;
        byte* data = (byte*)&value;
        for (int i = 0; i < 16; i++) data[i] = bytes[i];
        return value;
    }

    private static void Check<T>(Vector<T> value, byte[] bytes) where T : unmanaged
    {
        byte* data = (byte*)&value;
        for (int i = 0; i < 16; i++)
            if (data[i] != bytes[i]) throw new Exception("explicit byte mismatch at " + i);
        checks++;
    }

    private static void Source0(byte[] pattern)
    {
        Vector<byte> value = Seed<byte>(pattern);
        Check(Identity(value), pattern);
        Check((Vector<sbyte>)value, pattern);
        Check((Vector<short>)value, pattern);
        Check((Vector<ushort>)value, pattern);
        Check((Vector<int>)value, pattern);
        Check((Vector<uint>)value, pattern);
        Check((Vector<long>)value, pattern);
        Check((Vector<ulong>)value, pattern);
        Check((Vector<float>)value, pattern);
        Check((Vector<double>)value, pattern);
        Check((Vector<nint>)value, pattern);
        Check((Vector<nuint>)value, pattern);
    }

    private static void Source1(byte[] pattern)
    {
        Vector<sbyte> value = Seed<sbyte>(pattern);
        Check((Vector<byte>)value, pattern);
        Check(Identity(value), pattern);
        Check((Vector<short>)value, pattern);
        Check((Vector<ushort>)value, pattern);
        Check((Vector<int>)value, pattern);
        Check((Vector<uint>)value, pattern);
        Check((Vector<long>)value, pattern);
        Check((Vector<ulong>)value, pattern);
        Check((Vector<float>)value, pattern);
        Check((Vector<double>)value, pattern);
        Check((Vector<nint>)value, pattern);
        Check((Vector<nuint>)value, pattern);
    }

    private static void Source2(byte[] pattern)
    {
        Vector<short> value = Seed<short>(pattern);
        Check((Vector<byte>)value, pattern);
        Check((Vector<sbyte>)value, pattern);
        Check(Identity(value), pattern);
        Check((Vector<ushort>)value, pattern);
        Check((Vector<int>)value, pattern);
        Check((Vector<uint>)value, pattern);
        Check((Vector<long>)value, pattern);
        Check((Vector<ulong>)value, pattern);
        Check((Vector<float>)value, pattern);
        Check((Vector<double>)value, pattern);
        Check((Vector<nint>)value, pattern);
        Check((Vector<nuint>)value, pattern);
    }

    private static void Source3(byte[] pattern)
    {
        Vector<ushort> value = Seed<ushort>(pattern);
        Check((Vector<byte>)value, pattern);
        Check((Vector<sbyte>)value, pattern);
        Check((Vector<short>)value, pattern);
        Check(Identity(value), pattern);
        Check((Vector<int>)value, pattern);
        Check((Vector<uint>)value, pattern);
        Check((Vector<long>)value, pattern);
        Check((Vector<ulong>)value, pattern);
        Check((Vector<float>)value, pattern);
        Check((Vector<double>)value, pattern);
        Check((Vector<nint>)value, pattern);
        Check((Vector<nuint>)value, pattern);
    }

    private static void Source4(byte[] pattern)
    {
        Vector<int> value = Seed<int>(pattern);
        Check((Vector<byte>)value, pattern);
        Check((Vector<sbyte>)value, pattern);
        Check((Vector<short>)value, pattern);
        Check((Vector<ushort>)value, pattern);
        Check(Identity(value), pattern);
        Check((Vector<uint>)value, pattern);
        Check((Vector<long>)value, pattern);
        Check((Vector<ulong>)value, pattern);
        Check((Vector<float>)value, pattern);
        Check((Vector<double>)value, pattern);
        Check((Vector<nint>)value, pattern);
        Check((Vector<nuint>)value, pattern);
    }

    private static void Source5(byte[] pattern)
    {
        Vector<uint> value = Seed<uint>(pattern);
        Check((Vector<byte>)value, pattern);
        Check((Vector<sbyte>)value, pattern);
        Check((Vector<short>)value, pattern);
        Check((Vector<ushort>)value, pattern);
        Check((Vector<int>)value, pattern);
        Check(Identity(value), pattern);
        Check((Vector<long>)value, pattern);
        Check((Vector<ulong>)value, pattern);
        Check((Vector<float>)value, pattern);
        Check((Vector<double>)value, pattern);
        Check((Vector<nint>)value, pattern);
        Check((Vector<nuint>)value, pattern);
    }

    private static void Source6(byte[] pattern)
    {
        Vector<long> value = Seed<long>(pattern);
        Check((Vector<byte>)value, pattern);
        Check((Vector<sbyte>)value, pattern);
        Check((Vector<short>)value, pattern);
        Check((Vector<ushort>)value, pattern);
        Check((Vector<int>)value, pattern);
        Check((Vector<uint>)value, pattern);
        Check(Identity(value), pattern);
        Check((Vector<ulong>)value, pattern);
        Check((Vector<float>)value, pattern);
        Check((Vector<double>)value, pattern);
        Check((Vector<nint>)value, pattern);
        Check((Vector<nuint>)value, pattern);
    }

    private static void Source7(byte[] pattern)
    {
        Vector<ulong> value = Seed<ulong>(pattern);
        Check((Vector<byte>)value, pattern);
        Check((Vector<sbyte>)value, pattern);
        Check((Vector<short>)value, pattern);
        Check((Vector<ushort>)value, pattern);
        Check((Vector<int>)value, pattern);
        Check((Vector<uint>)value, pattern);
        Check((Vector<long>)value, pattern);
        Check(Identity(value), pattern);
        Check((Vector<float>)value, pattern);
        Check((Vector<double>)value, pattern);
        Check((Vector<nint>)value, pattern);
        Check((Vector<nuint>)value, pattern);
    }

    private static void Source8(byte[] pattern)
    {
        Vector<float> value = Seed<float>(pattern);
        Check((Vector<byte>)value, pattern);
        Check((Vector<sbyte>)value, pattern);
        Check((Vector<short>)value, pattern);
        Check((Vector<ushort>)value, pattern);
        Check((Vector<int>)value, pattern);
        Check((Vector<uint>)value, pattern);
        Check((Vector<long>)value, pattern);
        Check((Vector<ulong>)value, pattern);
        Check(Identity(value), pattern);
        Check((Vector<double>)value, pattern);
        Check((Vector<nint>)value, pattern);
        Check((Vector<nuint>)value, pattern);
    }

    private static void Source9(byte[] pattern)
    {
        Vector<double> value = Seed<double>(pattern);
        Check((Vector<byte>)value, pattern);
        Check((Vector<sbyte>)value, pattern);
        Check((Vector<short>)value, pattern);
        Check((Vector<ushort>)value, pattern);
        Check((Vector<int>)value, pattern);
        Check((Vector<uint>)value, pattern);
        Check((Vector<long>)value, pattern);
        Check((Vector<ulong>)value, pattern);
        Check((Vector<float>)value, pattern);
        Check(Identity(value), pattern);
        Check((Vector<nint>)value, pattern);
        Check((Vector<nuint>)value, pattern);
    }

    private static void Source10(byte[] pattern)
    {
        Vector<nint> value = Seed<nint>(pattern);
        Check((Vector<byte>)value, pattern);
        Check((Vector<sbyte>)value, pattern);
        Check((Vector<short>)value, pattern);
        Check((Vector<ushort>)value, pattern);
        Check((Vector<int>)value, pattern);
        Check((Vector<uint>)value, pattern);
        Check((Vector<long>)value, pattern);
        Check((Vector<ulong>)value, pattern);
        Check((Vector<float>)value, pattern);
        Check((Vector<double>)value, pattern);
        Check(Identity(value), pattern);
        Check((Vector<nuint>)value, pattern);
    }

    private static void Source11(byte[] pattern)
    {
        Vector<nuint> value = Seed<nuint>(pattern);
        Check((Vector<byte>)value, pattern);
        Check((Vector<sbyte>)value, pattern);
        Check((Vector<short>)value, pattern);
        Check((Vector<ushort>)value, pattern);
        Check((Vector<int>)value, pattern);
        Check((Vector<uint>)value, pattern);
        Check((Vector<long>)value, pattern);
        Check((Vector<ulong>)value, pattern);
        Check((Vector<float>)value, pattern);
        Check((Vector<double>)value, pattern);
        Check((Vector<nint>)value, pattern);
        Check(Identity(value), pattern);
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct Three { public byte A, B, C; }
    private struct Sixteen { public ulong A, B; }
    private struct WithReference { public object Value; public int Number; }

    private static void RetainedFills()
    {
        FillCases(true, &FillBool); FillCases(false, &FillBool);
        FillCases((ushort)0x8123, &FillU16);
        FillCases(0x89172345u, &FillU32);
        FillCases(0xfedcba9876543210ul, &FillU64);
        FillCases(BitConverter.Int32BitsToSingle(unchecked((int)0xffc12345)), &FillSingle);
        FillCases(BitConverter.Int32BitsToSingle(unchecked((int)0x80000000)), &FillSingle);
        FillCases(BitConverter.Int64BitsToDouble(unchecked((long)0xfff8000123456789)), &FillDouble);
        FillCases(BitConverter.Int64BitsToDouble(unchecked((long)0x8000000000000000)), &FillDouble);
        FillCases(new Three { A = 0x12, B = 0x89, C = 0xef }, &FillThree);
        FillCases(new Sixteen { A = 0x0123456789abcdef, B = 0xfedcba9876543210 }, &FillSixteen);
        object token = new object();
        object sentinel = new object();
        object[] objects = new object[69];
        objects[0] = objects[68] = sentinel;
        Span<object> refs = objects.AsSpan(1, 67);
        delegate*<ref Span<object>, object, void> fillObject = &FillObject;
        fillObject(ref refs, token);
        for (int i = 1; i < 68; i++) if (!ReferenceEquals(objects[i], token)) throw new Exception("reference fill identity");
        if (!ReferenceEquals(objects[0], sentinel) || !ReferenceEquals(objects[68], sentinel)) throw new Exception("reference first canary");
        fillObject(ref refs, null);
        for (int i = 1; i < 68; i++) if (objects[i] != null) throw new Exception("reference null fill");
        if (!ReferenceEquals(objects[0], sentinel) || !ReferenceEquals(objects[68], sentinel)) throw new Exception("reference canary");
        WithReference[] mixed = new WithReference[69];
        mixed[0].Number = mixed[68].Number = 71;
        Span<WithReference> mixedSpan = mixed.AsSpan(1, 67);
        delegate*<ref Span<WithReference>, WithReference, void> fillMixed = &FillMixed;
        fillMixed(ref mixedSpan, new WithReference { Value = token, Number = 19 });
        for (int i = 1; i < 68; i++) if (!ReferenceEquals(mixed[i].Value, token) || mixed[i].Number != 19) throw new Exception("mixed reference fill");
        if (mixed[0].Number != 71 || mixed[68].Number != 71 || mixed[0].Value != null || mixed[68].Value != null) throw new Exception("mixed canary");
        Console.WriteLine("retained fill byte and reference canaries passed");
    }

    private static void FillCases<T>(T value, delegate*<ref Span<T>, T, void> fill) where T : unmanaged
    {
        int threshold = 16 / sizeof(T);
        int[] lengths = { 0, 1, threshold - 1, threshold, threshold + 1, threshold * 2, threshold * 3 + 3, 67 };
        Span<T> empty = default;
        fill(ref empty, value);
        foreach (int length in lengths)
        {
            T[] actual = new T[length + 4];
            T[] expected = new T[length + 4];
            for (int i = 0; i < actual.Length; i++)
            {
                T canary = default;
                for (int b = 0; b < sizeof(T); b++) ((byte*)&canary)[b] = (byte)(0x5b + i + b);
                actual[i] = canary; expected[i] = canary;
            }
            for (int i = 2; i < length + 2; i++) expected[i] = value;
            Span<T> span = actual.AsSpan(2, length);
            fill(ref span, value);
            for (int i = 0; i < actual.Length; i++)
            {
                T a = actual[i], e = expected[i];
                for (int b = 0; b < sizeof(T); b++)
                    if (((byte*)&a)[b] != ((byte*)&e)[b]) throw new Exception("retained fill bytes/canary");
            }
        }
    }
    private static void FillBool(ref Span<bool> span, bool value) => span.Fill(value);
    private static void FillU16(ref Span<ushort> span, ushort value) => span.Fill(value);
    private static void FillU32(ref Span<uint> span, uint value) => span.Fill(value);
    private static void FillU64(ref Span<ulong> span, ulong value) => span.Fill(value);
    private static void FillSingle(ref Span<float> span, float value) => span.Fill(value);
    private static void FillDouble(ref Span<double> span, double value) => span.Fill(value);
    private static void FillThree(ref Span<Three> span, Three value) => span.Fill(value);
    private static void FillSixteen(ref Span<Sixteen> span, Sixteen value) => span.Fill(value);
    private static void FillObject(ref Span<object> span, object value) => span.Fill(value);
    private static void FillMixed(ref Span<WithReference> span, WithReference value) => span.Fill(value);
}
