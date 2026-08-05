using System;
using System.Numerics;

namespace GenericMathShiftModulus;

// The remainder, shift and checked-division operators through the generic-math
// static-abstract interfaces: IModulusOperators<T,T,T> (a % b — truncated-
// division remainder for integers, fmod semantics for float/double, the intrinsic
// decimal remainder for decimal), IShiftOperators<T,int,T> (<< / >> / >>>), and
// IDivisionOperators' op_CheckedDivision (a `checked` context binds a constrained
// `/` to the checked operator; it divides identically — its only traps come from
// the div instruction, a standing carve-out). The shift sections pin the BCL
// operator bodies' storage-width semantics, which differ from C#'s int-promoted
// shifts on the same values: the amount masks to the operand's real width (&7
// for sbyte/byte, &15 for short/ushort — so byte << 8 == byte, unlike the
// int-promoted (byte)(x << 8) == 0 — and &31 / &63 for the word sizes:
// 1 << 32 == 1, 1L << 64 == 1L), and >>> zero-extends a sub-word operand from
// its storage width before the logical shift, so sbyte -8 >>> 1 is 124
// (0xF8 >> 1), not the int-promoted -4. All integer results are exact; float
// modulus prints exactly-representable results directly and asserts the rest
// against the same-type non-generic `%`.
internal static class ShiftModulusOps
{
    static T Mod<T>(T a, T b) where T : IModulusOperators<T, T, T> => a % b;

    static T Shl<T>(T v, int n) where T : IShiftOperators<T, int, T> => v << n;
    static T Shr<T>(T v, int n) where T : IShiftOperators<T, int, T> => v >> n;
    static T Shru<T>(T v, int n) where T : IShiftOperators<T, int, T> => v >>> n;

    // Inside `checked { }` the constrained `/` binds to op_CheckedDivision (the
    // same selection rule the checked add/sub/mul helpers rely on).
    static T DivC<T>(T a, T b) where T : IDivisionOperators<T, T, T>
    {
        checked
        {
            return a / b;
        }
    }

    internal static void __GateEntry()
    {
        Console.WriteLine("== Modulus (op_Modulus): integers ==");
        Console.WriteLine($"int    {Mod(7, 3)} {Mod(-7, 3)} {Mod(7, -3)}");
        Console.WriteLine($"uint   {Mod(7u, 3u)} {Mod(4294967295u, 10u)}");
        Console.WriteLine($"long   {Mod(9000000000L, 7L)} {Mod(-9000000000L, 7L)}");
        Console.WriteLine($"ulong  {Mod(18446744073709551615UL, 10UL)}");
        Console.WriteLine($"short  {Mod((short)7, (short)3)} {Mod((short)-7, (short)3)}");
        Console.WriteLine($"byte   {Mod((byte)7, (byte)3)} {Mod((byte)200, (byte)7)}");

        Console.WriteLine("== Modulus (op_Modulus): float / double / decimal ==");
        Console.WriteLine(Mod(5.25, 1.5));                      // 0.75 (exact)
        Console.WriteLine(Mod(-5.25, 1.5));                     // -0.75 (exact)
        Console.WriteLine(Mod(7.7, 2.3) == 7.7 % 2.3);          // True
        Console.WriteLine(Mod(5.25f, 1.5f));                    // 0.75 (exact)
        Console.WriteLine(Mod(-5.25f, 1.5f));                   // -0.75 (exact)
        Console.WriteLine(Mod(7.7f, 2.3f) == 7.7f % 2.3f);      // True
        Console.WriteLine(Mod(10m, 3m));                        // 1
        Console.WriteLine(Mod(7.25m, 2.5m));                    // 2.25
        Console.WriteLine(Mod(-7.5m, 2m));                      // -1.5

        Console.WriteLine("== Shifts: int / uint ==");
        Console.WriteLine($"{Shl(1, 1)} {Shl(1, 31)} {Shl(1, 32)} {Shl(-2, 1)}");
        Console.WriteLine($"{Shr(-8, 1)} {Shr(int.MinValue, 31)} {Shr(1024, 10)}");
        Console.WriteLine($"{Shru(-8, 1)} {Shru(-1, 32)} {Shru(1024, 10)}");
        Console.WriteLine($"{Shl(1u, 31)} {Shl(2147483648u, 1)} {Shr(2147483648u, 31)} {Shru(2147483648u, 4)}");

        Console.WriteLine("== Shifts: long / ulong ==");
        Console.WriteLine($"{Shl(1L, 63)} {Shl(1L, 64)} {Shl(-2L, 1)}");
        Console.WriteLine($"{Shr(-8L, 1)} {Shr(long.MinValue, 63)}");
        Console.WriteLine($"{Shru(-8L, 1)} {Shru(-1L, 64)}");
        Console.WriteLine($"{Shl(1UL, 63)} {Shr(ulong.MaxValue, 63)} {Shru(1UL, 64)}");

        Console.WriteLine("== Shifts: sub-word (storage-width masking) ==");
        Console.WriteLine($"sbyte  {Shl((sbyte)1, 7)} {Shl((sbyte)1, 8)} {Shl((sbyte)1, 33)}");
        Console.WriteLine($"sbyte  {Shr((sbyte)-8, 1)} {Shru((sbyte)-8, 1)} {Shru((sbyte)-8, 4)}");
        Console.WriteLine($"byte   {Shl((byte)0x80, 1)} {Shl((byte)1, 8)} {Shl((byte)1, 33)} {Shr((byte)0x80, 3)}");
        Console.WriteLine($"short  {Shl((short)1, 15)} {Shl((short)1, 16)} {Shl((short)1, 33)}");
        Console.WriteLine($"short  {Shr((short)-8, 1)} {Shru((short)-8, 1)}");
        Console.WriteLine($"ushort {Shl((ushort)0x8000, 1)} {Shl((ushort)1, 33)} {Shr((ushort)0x8000, 15)} {Shru((ushort)0x8000, 4)}");

        Console.WriteLine("== Checked division (op_CheckedDivision) ==");
        Console.WriteLine($"int    {DivC(100, 7)}");
        Console.WriteLine($"long   {DivC(9000000000L, 4L)}");
        Console.WriteLine($"uint   {DivC(100u, 7u)}");
    }
}
