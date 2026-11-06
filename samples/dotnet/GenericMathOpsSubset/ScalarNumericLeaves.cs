using System;
using System.Numerics;

namespace GenericMathScalarLeaves;

internal static class ScalarNumericLeaves
{
    private static int _effects;
    private static int Operand(int value) { _effects++; return value; }

    private static string Bytes(byte[] bytes)
    {
        string result = "";
        foreach (byte value in bytes) result += value.ToString("X2");
        return result;
    }

    private static void Write<T>(string label, T value) where T : IFloatingPoint<T>
    {
        IFloatingPoint<T> number = value;
        int[] lengths = { 0, 1, 2, 3, 4, 7, 8, 9, 11, 12, 13 };
        foreach (int length in lengths)
        for (int operation = 0; operation < 4; operation++)
        {
            byte[] bytes = new byte[length + 2];
            for (int i = 0; i < bytes.Length; i++) bytes[i] = 0xA5;
            Span<byte> destination = new Span<byte>(bytes, 1, length);
            int written = 91;
            bool ok = operation switch
            {
                0 => number.TryWriteExponentBigEndian(destination, out written),
                1 => number.TryWriteExponentLittleEndian(destination, out written),
                2 => number.TryWriteSignificandBigEndian(destination, out written),
                _ => number.TryWriteSignificandLittleEndian(destination, out written),
            };
            Console.WriteLine($"leaf-write:{label}:{operation}:{length}={ok}:{written}:{Bytes(bytes)}");
        }
    }

    internal static void __GateEntry()
    {
        Console.WriteLine("== scalar numeric managed leaves ==");
        uint[] singleBits = { 0, 0x80000000, 1, 0x80000001, 0x007FFFFF, 0x807FFFFF,
            0x00800000, 0x80800000, 0x3F800000, 0x40000000, 0x7F7FFFFF, 0xFF7FFFFF,
            0x7F800000, 0xFF800000, 0x7FC00001, 0xFFC12345, 0x7F800001, 0xFF812345 };
        foreach (uint bits in singleBits)
            Write("f32-" + bits.ToString("X8"), BitConverter.Int32BitsToSingle(unchecked((int)bits)));
        ulong[] doubleBits = { 0, 0x8000000000000000, 1, 0x8000000000000001,
            0x000FFFFFFFFFFFFF, 0x800FFFFFFFFFFFFF, 0x0010000000000000, 0x8010000000000000,
            0x3FF0000000000000, 0x4000000000000000, 0x7FEFFFFFFFFFFFFF, 0xFFEFFFFFFFFFFFFF,
            0x7FF0000000000000, 0xFFF0000000000000, 0x7FF8000000000001, 0xFFF8123456789ABC,
            0x7FF0000000000001, 0xFFF0123456789ABC };
        foreach (ulong bits in doubleBits)
            Write("f64-" + bits.ToString("X16"), BitConverter.Int64BitsToDouble(unchecked((long)bits)));
        for (byte scale = 0; scale <= 28; scale++)
        for (int negative = 0; negative < 2; negative++)
        {
            Write("dec-zero-" + negative + "-" + scale, new decimal(0, 0, 0, negative != 0, scale));
            Write("dec-full-" + negative + "-" + scale, new decimal(-1, -1, -1, negative != 0, scale));
        }
        Func<int, int> address = int.Sign;
        foreach (int value in new[] { int.MinValue, -1, 0, 1, int.MaxValue })
        {
            _effects = 0;
            int direct = int.Sign(Operand(value));
            int indirect = address(Operand(value));
            Console.WriteLine($"leaf-sign:{value}={direct}:{indirect}:{_effects}");
        }
        Console.WriteLine("scalar numeric managed leaves end");
    }
}
