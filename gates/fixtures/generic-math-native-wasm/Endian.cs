using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace GenericMathNativeWasm;

internal static partial class Program
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool TryEndian<T>(ReadOnlySpan<byte> source, bool unsigned, bool little, out T value)
        where T : IBinaryInteger<T> => little
        ? T.TryReadLittleEndian(source, unsigned, out value)
        : T.TryReadBigEndian(source, unsigned, out value);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static T ReadEndian<T>(byte[] source, int start, bool unsigned, bool little, int route)
        where T : IBinaryInteger<T>
    {
        if (route == 0) return little ? T.ReadLittleEndian(source, unsigned) : T.ReadBigEndian(source, unsigned);
        if (route == 1) return little ? T.ReadLittleEndian(new ReadOnlySpan<byte>(source), unsigned)
            : T.ReadBigEndian(new ReadOnlySpan<byte>(source), unsigned);
        return little ? T.ReadLittleEndian(source, start, unsigned) : T.ReadBigEndian(source, start, unsigned);
    }

    private static void EndianCase<T>(string label, byte[] big, bool unsigned, bool success,
        T expected, T before, T after) where T : unmanaged, IBinaryInteger<T>
    {
        for (int endian = 0; endian < 2; endian++)
        {
            bool little = endian != 0;
            byte[] bytes = (byte[])big.Clone();
            if (little) Array.Reverse(bytes);
            string key = label + (little ? ".le" : ".be");
            T value = before;
            bool ok = TryEndian<T>(bytes, unsigned, little, out value);
            Check(key + ".try-local", ok == success && value == expected);
            T[] array = { before, after, after };
            ok = TryEndian<T>(bytes, unsigned, little, out array[1]);
            Check(key + ".try-array", ok == success && array[1] == expected && array[0] == before && array[2] == after);
            Fields<T> fields = new Fields<T> { Before = before, Value = after, After = after };
            ok = TryEndian<T>(bytes, unsigned, little, out fields.Value);
            Check(key + ".try-field", ok == success && fields.Value == expected && fields.Before == before && fields.After == after);
            byte[] padded = new byte[bytes.Length + 1];
            padded[0] = 0x69;
            Array.Copy(bytes, 0, padded, 1, bytes.Length);
            for (int route = 0; route < 3; route++)
            {
                bool overflow = false;
                value = before;
                try { value = ReadEndian<T>(route == 2 ? padded : bytes, 1, unsigned, little, route); }
                catch (OverflowException) { overflow = true; }
                Check(key + ".read" + route, overflow == !success && (overflow || value == expected));
            }
        }
        return;
    }

    private static void EndianBounds<T>(string label) where T : IBinaryInteger<T>
    {
        for (int endian = 0; endian < 2; endian++)
        for (int sign = 0; sign < 2; sign++)
        {
            bool little = endian != 0, unsigned = sign != 0;
            string key = label + ".bounds" + endian + sign;
            for (int route = 0; route < 3; route++)
                Check(key + ".null" + route, ReadEndian<T>(null, 0, unsigned, little, route) == T.Zero);
            Check(key + ".end", ReadEndian<T>(new byte[] { 0xff }, 1, unsigned, little, 2) == T.Zero);
            for (int shape = 0; shape < 4; shape++)
            {
                bool range = false;
                try { ReadEndian<T>(shape < 2 ? null : new byte[] { 0x12 }, (shape & 1) == 0 ? -1 : 2,
                    unsigned, little, 2); }
                catch (ArgumentOutOfRangeException error) { range = error.ParamName is null; }
                Check(key + ".invalid" + shape, range);
            }
            for (int source = 0; source < 3; source++)
            {
                bool fault = false;
                byte[] bytes = source == 0 ? new byte[] { 1 } : source == 1 ? new byte[] { 1, 1, 1, 1, 1 } : null;
                try { TryEndian<T>(bytes, unsigned, little, out Unsafe.NullRef<T>()); }
                catch (NullReferenceException) { fault = true; }
                Check(key + ".null-out" + source, fault);
            }
        }
        return;
    }

    private static void Endian<T>(string label, bool signed, T min, T max, T high, T negativeOne,
        T signedMax, T before, T after) where T : unmanaged, IBinaryInteger<T>
    {
        for (int sign = 0; sign < 2; sign++)
        {
            bool unsigned = sign != 0;
            string key = label + ".endian." + (unsigned ? "unsigned-source" : "signed-source");
            EndianCase(key + ".empty", new byte[] { }, unsigned, true, T.Zero, before, after);
            EndianCase(key + ".zero", new byte[] { 0 }, unsigned, true, T.Zero, before, after);
            EndianCase(key + ".one", new byte[] { 1 }, unsigned, true, T.One, before, after);
            EndianCase(key + ".signed-max", new byte[] { 0x7f, 0xff, 0xff, 0xff }, unsigned, true, signedMax, before, after);
            bool fitsHigh = signed != unsigned;
            EndianCase(key + ".high-bit", new byte[] { 0x80, 0, 0, 0 }, unsigned, fitsHigh,
                fitsHigh ? high : T.Zero, before, after);
            bool fitsOnes = signed != unsigned;
            EndianCase(key + ".ones", new byte[] { 0xff, 0xff, 0xff, 0xff }, unsigned, fitsOnes,
                fitsOnes ? signed ? negativeOne : max : T.Zero, before, after);
            EndianCase(key + ".overflow", new byte[] { 1, 0, 0, 0, 0 }, unsigned, false, T.Zero, before, after);
            EndianCase(key + ".negative-overflow", new byte[] { 0xff, 0x7f, 0xff, 0xff, 0xff }, unsigned, false, T.Zero, before, after);
            EndianCase(key + ".zero-padding", new byte[] { 0, 0, 0, 0, 0, 0, 0, 1 }, unsigned, true, T.One, before, after);
            EndianCase(key + ".zero-padded-max", new byte[] { 0, 0, 0, 0, 0x7f, 0xff, 0xff, 0xff }, unsigned, true, signedMax, before, after);
            EndianCase(key + ".zero-padded-high", new byte[] { 0, 0, 0, 0, 0x80, 0, 0, 0 }, unsigned, !signed,
                signed ? T.Zero : high, before, after);
            EndianCase(key + ".zero-padded-ones", new byte[] { 0, 0, 0, 0, 0xff, 0xff, 0xff, 0xff }, unsigned, !signed,
                signed ? T.Zero : max, before, after);
            bool fitsNegative = signed && !unsigned;
            EndianCase(key + ".sign-padded-min", new byte[] { 0xff, 0xff, 0xff, 0xff, 0x80, 0, 0, 0 }, unsigned, fitsNegative,
                fitsNegative ? min : T.Zero, before, after);
            EndianCase(key + ".sign-padded-minus-one", new byte[] { 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff }, unsigned, fitsNegative,
                fitsNegative ? negativeOne : T.Zero, before, after);
            EndianCase(key + ".short-minus-one", new byte[] { 0xff }, unsigned, fitsNegative || unsigned,
                unsigned ? T.CreateChecked(255) : fitsNegative ? negativeOne : T.Zero, before, after);
        }
        EndianBounds<T>(label);
        Console.WriteLine(label + " endian native32 end");
        return;
    }
}
