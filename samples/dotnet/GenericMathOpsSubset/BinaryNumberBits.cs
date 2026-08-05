using System;
using System.Numerics;

namespace GenericMathBinaryBits;

// IBinaryNumber<TSelf>'s bit-pattern members on the IEEE floats, where `~` is
// not expressible in plain C# and the exact bits are the contract:
// op_OnesComplement is a NOT of the raw IEEE representation and AllBitsSet is
// the all-ones pattern (a negative quiet NaN with a full payload), both
// observable bit-exactly through BitConverter — plus AllBitsSet for every
// integer width (all-ones at the REAL storage width: byte 255, sbyte -1) and
// the integer `~` through the same constraint. Output diffs exact vs real .NET.
internal static class BinaryNumberBits
{
    static T Not<T>(T v) where T : IBinaryNumber<T> => ~v;
    static T AllBits<T>() where T : IBinaryNumber<T> => T.AllBitsSet;

    internal static void __GateEntry()
    {
        Console.WriteLine("== IBinaryNumber bits (op_OnesComplement / AllBitsSet) ==");
        Console.WriteLine($"~0.0 {BitConverter.DoubleToInt64Bits(Not(0.0))}");
        Console.WriteLine($"~1.5 {BitConverter.DoubleToInt64Bits(Not(1.5))}");
        Console.WriteLine($"~-2.25 {BitConverter.DoubleToInt64Bits(Not(-2.25))}");
        Console.WriteLine($"~0f {BitConverter.SingleToInt32Bits(Not(0.0f))}");
        Console.WriteLine($"~1.5f {BitConverter.SingleToInt32Bits(Not(1.5f))}");
        Console.WriteLine($"~-2.25f {BitConverter.SingleToInt32Bits(Not(-2.25f))}");
        Console.WriteLine($"roundtrip {Not(Not(1.5))} {Not(Not(-2.25f))}");
        Console.WriteLine($"allbits d {BitConverter.DoubleToInt64Bits(AllBits<double>())}");
        Console.WriteLine($"allbits f {BitConverter.SingleToInt32Bits(AllBits<float>())}");
        Console.WriteLine($"allbits i8 {AllBits<sbyte>()} {AllBits<byte>()}");
        Console.WriteLine($"allbits i16 {AllBits<short>()} {AllBits<ushort>()}");
        Console.WriteLine($"allbits i32 {AllBits<int>()} {AllBits<uint>()}");
        Console.WriteLine($"allbits i64 {AllBits<long>()} {AllBits<ulong>()}");
        Console.WriteLine($"allbits char {(int)AllBits<char>()}");
        Console.WriteLine($"~ints {Not((sbyte)5)} {Not((byte)5)} {Not((short)5)} {Not((ushort)5)} {Not(5)} {Not(5u)} {Not(5L)} {Not(5ul)}");
    }
}
