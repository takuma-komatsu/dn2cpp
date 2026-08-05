using System;
using System.Numerics;

namespace GenericMathChecked;

// Checked operators and range-honoring conversions through the generic-math
// static-abstract interfaces. A `checked` context on a constrained operator
// binds to op_CheckedAddition / op_CheckedIncrement / …, so every call here
// routes through IAdditionOperators<T,T,T> etc. — the interface lowering, not
// the add.ovf opcode a checked op on concrete ints would emit. Integer overflow
// must trap with OverflowException exactly like real .NET (checked negation of
// a signed T traps only at T.MinValue; of an unsigned T for any nonzero value);
// float/double checked equals unchecked (infinity, no trap). CreateChecked /
// CreateSaturating / CreateTruncating on a type parameter reach INumberBase<T>'s
// default impl and its TryConvertFrom*/TryConvertTo* leaves: Checked throws out
// of range (NaN included), Saturating clamps to [T.MinValue, T.MaxValue]
// (NaN -> 0), Truncating wraps integer sources and clamps float sources.
internal static class CheckedGenericMath
{
    static T AddC<T>(T a, T b) where T : IAdditionOperators<T, T, T> => checked(a + b);
    static T SubC<T>(T a, T b) where T : ISubtractionOperators<T, T, T> => checked(a - b);
    static T MulC<T>(T a, T b) where T : IMultiplyOperators<T, T, T> => checked(a * b);
    static T IncC<T>(T a) where T : IIncrementOperators<T> { checked { a++; } return a; }
    static T DecC<T>(T a) where T : IDecrementOperators<T> { checked { a--; } return a; }
    static T NegC<T>(T a) where T : IUnaryNegationOperators<T, T> => checked(-a);

    // The overflow arm prints the exception's runtime type name (the trap object
    // does not model .NET's message text, so the message stays out of the diff).
    // Success formats through an interpolation hole (AppendFormatted<T> renders
    // the closed primitive with its declared sign), not constrained ToString.
    static string AddS<T>(T a, T b) where T : IAdditionOperators<T, T, T>
    {
        try { return $"{AddC(a, b)}"; }
        catch (OverflowException ex) { return ex.GetType().Name; }
    }

    static string SubS<T>(T a, T b) where T : ISubtractionOperators<T, T, T>
    {
        try { return $"{SubC(a, b)}"; }
        catch (OverflowException ex) { return ex.GetType().Name; }
    }

    static string MulS<T>(T a, T b) where T : IMultiplyOperators<T, T, T>
    {
        try { return $"{MulC(a, b)}"; }
        catch (OverflowException ex) { return ex.GetType().Name; }
    }

    static string IncS<T>(T a) where T : IIncrementOperators<T>
    {
        try { return $"{IncC(a)}"; }
        catch (OverflowException ex) { return ex.GetType().Name; }
    }

    static string DecS<T>(T a) where T : IDecrementOperators<T>
    {
        try { return $"{DecC(a)}"; }
        catch (OverflowException ex) { return ex.GetType().Name; }
    }

    static string NegS<T>(T a) where T : IUnaryNegationOperators<T, T>
    {
        try { return $"{NegC(a)}"; }
        catch (OverflowException ex) { return ex.GetType().Name; }
    }

    static TTo CreateC<TTo, TFrom>(TFrom v) where TTo : INumberBase<TTo> where TFrom : INumberBase<TFrom>
        => TTo.CreateChecked(v);
    static TTo CreateS<TTo, TFrom>(TFrom v) where TTo : INumberBase<TTo> where TFrom : INumberBase<TFrom>
        => TTo.CreateSaturating(v);

    static string ChkTo<TTo, TFrom>(TFrom v) where TTo : INumberBase<TTo> where TFrom : INumberBase<TFrom>
    {
        try { return $"{TTo.CreateChecked(v)}"; }
        catch (OverflowException ex) { return ex.GetType().Name; }
    }

    static string SatTo<TTo, TFrom>(TFrom v) where TTo : INumberBase<TTo> where TFrom : INumberBase<TFrom>
    {
        try { return $"{TTo.CreateSaturating(v)}"; }
        catch (OverflowException ex) { return ex.GetType().Name; }
    }

    static string TruncTo<TTo, TFrom>(TFrom v) where TTo : INumberBase<TTo> where TFrom : INumberBase<TFrom>
    {
        try { return $"{TTo.CreateTruncating(v)}"; }
        catch (OverflowException ex) { return ex.GetType().Name; }
    }

    internal static void __GateEntry()
    {
        Console.WriteLine("== checked add/sub/mul (operator interfaces) ==");
        Console.WriteLine($"int    {AddS(int.MaxValue - 5, 5)} {AddS(int.MaxValue, 1)} {SubS(int.MinValue, 1)} {MulS(46340, 46340)} {MulS(46341, 46341)} {MulS(int.MinValue, -1)}");
        Console.WriteLine($"long   {AddS(long.MaxValue - 9L, 9L)} {AddS(long.MaxValue, 1L)} {SubS(long.MinValue, 1L)} {MulS(3037000499L, 3037000499L)} {MulS(3037000500L, 3037000500L)} {MulS(long.MinValue, -1L)}");
        Console.WriteLine($"uint   {AddS(4294967290u, 5u)} {AddS(uint.MaxValue, 1u)} {SubS(0u, 1u)} {MulS(65535u, 65537u)} {MulS(65536u, 65536u)}");
        Console.WriteLine($"ulong  {AddS(ulong.MaxValue - 1UL, 1UL)} {AddS(ulong.MaxValue, 1UL)} {SubS(0UL, 1UL)} {MulS(4294967295UL, 4294967297UL)} {MulS(4294967296UL, 4294967296UL)}");
        Console.WriteLine($"nint   {AddS((nint)5, (nint)7)} {AddS(nint.MaxValue, (nint)1)}");
        Console.WriteLine($"sbyte  {AddS((sbyte)120, (sbyte)7)} {AddS((sbyte)120, (sbyte)8)} {SubS(sbyte.MinValue, (sbyte)1)} {MulS((sbyte)11, (sbyte)11)} {MulS((sbyte)12, (sbyte)12)}");
        Console.WriteLine($"byte   {AddS((byte)250, (byte)5)} {AddS((byte)250, (byte)6)} {SubS((byte)0, (byte)1)} {MulS((byte)15, (byte)17)} {MulS((byte)16, (byte)16)}");
        Console.WriteLine($"short  {AddS((short)32760, (short)7)} {AddS((short)32760, (short)8)} {SubS(short.MinValue, (short)1)} {MulS((short)181, (short)181)} {MulS((short)182, (short)182)}");
        Console.WriteLine($"ushort {AddS((ushort)65530, (ushort)5)} {AddS((ushort)65530, (ushort)6)} {SubS((ushort)0, (ushort)1)} {MulS((ushort)255, (ushort)257)} {MulS((ushort)256, (ushort)256)}");

        Console.WriteLine("== checked inc/dec/neg (operator interfaces) ==");
        Console.WriteLine($"int    {IncS(int.MaxValue - 1)} {IncS(int.MaxValue)} {DecS(int.MinValue + 1)} {DecS(int.MinValue)} {NegS(int.MaxValue)} {NegS(int.MinValue)}");
        Console.WriteLine($"long   {IncS(long.MaxValue)} {DecS(long.MinValue)} {NegS(long.MinValue)}");
        Console.WriteLine($"uint   {IncS(uint.MaxValue)} {DecS(0u)} {NegS(0u)} {NegS(5u)}");
        Console.WriteLine($"ulong  {IncS(ulong.MaxValue)} {DecS(0UL)} {NegS(0UL)} {NegS(9UL)}");
        Console.WriteLine($"sbyte  {IncS(sbyte.MaxValue)} {DecS(sbyte.MinValue)} {NegS(sbyte.MinValue)} {NegS((sbyte)(-127))}");
        Console.WriteLine($"byte   {IncS(byte.MaxValue)} {DecS((byte)0)} {NegS((byte)0)} {NegS((byte)1)}");
        Console.WriteLine($"short  {IncS(short.MaxValue)} {DecS(short.MinValue)} {NegS(short.MinValue)}");
        Console.WriteLine($"ushort {IncS(ushort.MaxValue)} {DecS((ushort)0)} {NegS((ushort)3)}");

        Console.WriteLine("== float/double checked ops do not trap ==");
        float fInf = AddC(float.MaxValue, float.MaxValue);
        double dInf = MulC(double.MaxValue, 2.0);
        Console.WriteLine($"float  addC(max,max) posInf={fInf == float.PositiveInfinity}");
        Console.WriteLine($"double mulC(max,2) posInf={dInf == double.PositiveInfinity}");
        Console.WriteLine($"float  subC(-max,max) negInf={SubC(float.MaxValue * -1f, float.MaxValue) == float.NegativeInfinity}");
        Console.WriteLine($"double negC(1.5)={NegC(1.5)} incC(2.25)={IncC(2.25)} decC(0.5)={DecC(0.5)}");

        Console.WriteLine("== CreateChecked/Saturating/Truncating (interface TryConvert path) ==");
        Console.WriteLine($"byte<-int(300)     chk={ChkTo<byte, int>(300)} sat={SatTo<byte, int>(300)} trunc={TruncTo<byte, int>(300)}");
        Console.WriteLine($"byte<-int(-5)      chk={ChkTo<byte, int>(-5)} sat={SatTo<byte, int>(-5)} trunc={TruncTo<byte, int>(-5)}");
        Console.WriteLine($"sbyte<-int(-200)   chk={ChkTo<sbyte, int>(-200)} sat={SatTo<sbyte, int>(-200)} trunc={TruncTo<sbyte, int>(-200)}");
        Console.WriteLine($"short<-int(70000)  chk={ChkTo<short, int>(70000)} sat={SatTo<short, int>(70000)} trunc={TruncTo<short, int>(70000)}");
        Console.WriteLine($"uint<-int(-1)      chk={ChkTo<uint, int>(-1)} sat={SatTo<uint, int>(-1)} trunc={TruncTo<uint, int>(-1)}");
        Console.WriteLine($"int<-uint(maxu)    chk={ChkTo<int, uint>(uint.MaxValue)} sat={SatTo<int, uint>(uint.MaxValue)} trunc={TruncTo<int, uint>(uint.MaxValue)}");
        Console.WriteLine($"long<-ulong(maxul) chk={ChkTo<long, ulong>(ulong.MaxValue)} sat={SatTo<long, ulong>(ulong.MaxValue)} trunc={TruncTo<long, ulong>(ulong.MaxValue)}");
        Console.WriteLine($"ulong<-long(-3)    chk={ChkTo<ulong, long>(-3L)} sat={SatTo<ulong, long>(-3L)} trunc={TruncTo<ulong, long>(-3L)}");
        Console.WriteLine($"int<-long(12345)   chk={ChkTo<int, long>(12345L)} sat={SatTo<int, long>(12345L)} trunc={TruncTo<int, long>(12345L)}");
        Console.WriteLine($"char<-int(65)      chk={ChkTo<char, int>(65)} char<-int(70000) chk={ChkTo<char, int>(70000)}");

        Console.WriteLine("== float->integer conversions (interface TryConvert path) ==");
        Console.WriteLine($"int<-double(1e10)   chk={ChkTo<int, double>(1e10)} sat={SatTo<int, double>(1e10)} trunc={TruncTo<int, double>(1e10)}");
        Console.WriteLine($"int<-double(-1e10)  chk={ChkTo<int, double>(-1e10)} sat={SatTo<int, double>(-1e10)} trunc={TruncTo<int, double>(-1e10)}");
        Console.WriteLine($"int<-double(NaN)    chk={ChkTo<int, double>(double.NaN)} sat={SatTo<int, double>(double.NaN)} trunc={TruncTo<int, double>(double.NaN)}");
        Console.WriteLine($"int<-double(-2147483648.9) chk={ChkTo<int, double>(-2147483648.9)}");
        Console.WriteLine($"int<-double(2147483647.9)  chk={ChkTo<int, double>(2147483647.9)}");
        Console.WriteLine($"int<-double(-1.9)   chk={ChkTo<int, double>(-1.9)} trunc={TruncTo<int, double>(-1.9)}");
        Console.WriteLine($"byte<-double(255.9) chk={ChkTo<byte, double>(255.9)} byte<-double(256.0) chk={ChkTo<byte, double>(256.0)}");
        Console.WriteLine($"ulong<-double(-0.5) chk={ChkTo<ulong, double>(-0.5)} ulong<-double(-1.0) chk={ChkTo<ulong, double>(-1.0)}");
        Console.WriteLine($"long<-float(NaN)    sat={SatTo<long, float>(float.NaN)} uint<-float(1e12f) sat={SatTo<uint, float>(1e12f)}");
        Console.WriteLine($"long<-double(2^63)  chk={ChkTo<long, double>(9223372036854775808.0)} sat={SatTo<long, double>(9223372036854775808.0)}");
        Console.WriteLine($"long<-double(-2^63) chk={ChkTo<long, double>(-9223372036854775808.0)} sat={SatTo<long, double>(-9223372036854775808.0)}");

        Console.WriteLine("== float-target conversions (always representable) ==");
        Console.WriteLine($"float<-double(max)  chk posInf={CreateC<float, double>(double.MaxValue) == float.PositiveInfinity}");
        Console.WriteLine($"float<-double(max)  sat posInf={CreateS<float, double>(double.MaxValue) == float.PositiveInfinity}");
        // Printed through a double hole: the value is what matters here, and a raw
        // float hole would pull shortest-roundtrip float digit counts into the diff.
        Console.WriteLine($"float<-long(maxl)   chk={(double)CreateC<float, long>(long.MaxValue)}");
        Console.WriteLine($"double<-ulong(maxul) sat={CreateS<double, ulong>(ulong.MaxValue)}");
    }
}
