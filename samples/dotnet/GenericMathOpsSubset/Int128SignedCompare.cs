using System;

namespace GenericMathInt128Sign;

// Signed ordered comparison over an operand whose C++ storage type is unsigned.
// Int128 is an ordinary transpiled BCL struct (two ulong fields {_lower, _upper},
// little-endian); its op_LessThan/op_GreaterThan and Sign/IsNegative reduce to a
// SIGNED IL compare of the high word `(long)left._upper < (long)right._upper` — an
// `ulong` field reinterpreted signed. Emit `a < b` verbatim and C++ sees two
// `uint64_t` operands and picks an UNSIGNED comparison, so a negative Int128 compares
// and formats as its unsigned reading (IsNegative(-1) == false, ToString of -1 == the
// 39-digit 2^128-1 value) — a wrong answer no other diff gate walks. Everything here
// diffs exact vs real .NET.
internal static class Int128SignedCompare
{
    // A generic ordered compare so the call monomorphizes to Int128.op_LessThan etc.
    static int CmpAll<T>(T a, T b) where T : IComparable<T>, IEquatable<T>
    {
        int r = 0;
        r |= (a.CompareTo(b) < 0) ? 1 : 0;
        return r;
    }

    // The raw BCL idiom, written out: a value stored unsigned, reinterpreted signed,
    // then compared with a signed IL opcode. This is exactly Int128.op_LessThan's
    // `(long)left._upper < (long)right._upper` with nothing generic around it.
    static bool SignedReinterpretLess(ulong lo, ulong hi) => (long)lo < (long)hi;

    internal static void __GateEntry()
    {
        Int128 neg1 = Int128.CreateTruncating(-1);
        Int128 negBig = Int128.CreateTruncating(-12345L);
        Int128 min = Int128.MinValue;
        Int128 zero = Int128.Zero;
        Int128 pos = Int128.CreateTruncating(12345L);
        Int128 max = Int128.MaxValue;

        Console.WriteLine("== Int128 negative ToString ==");
        Console.WriteLine($"-1        {neg1}");
        Console.WriteLine($"-12345    {negBig}");
        Console.WriteLine($"MinValue  {min}");
        Console.WriteLine($"MaxValue  {max}");
        Console.WriteLine($"0         {zero}");
        Console.WriteLine($"12345     {pos}");

        Console.WriteLine("== Int128.IsNegative / Sign ==");
        Console.WriteLine($"IsNeg(-1)={Int128.IsNegative(neg1)} IsNeg(0)={Int128.IsNegative(zero)} IsNeg(12345)={Int128.IsNegative(pos)}");
        Console.WriteLine($"IsNeg(Min)={Int128.IsNegative(min)} IsNeg(Max)={Int128.IsNegative(max)}");
        Console.WriteLine($"Sign(-1)={Int128.Sign(neg1)} Sign(0)={Int128.Sign(zero)} Sign(12345)={Int128.Sign(pos)}");

        Console.WriteLine("== Int128 ordered compare (< > <= >=), crossing zero ==");
        Console.WriteLine($"-1 <  0  = {neg1 < zero}");
        Console.WriteLine($"-1 >  0  = {neg1 > zero}");
        Console.WriteLine($"-1 <= 0  = {neg1 <= zero}");
        Console.WriteLine($"-1 >= 0  = {neg1 >= zero}");
        Console.WriteLine($"Min <  -1 = {min < neg1}");
        Console.WriteLine($"Min <  Max = {min < max}");
        Console.WriteLine($"Max >  Min = {max > min}");
        Console.WriteLine($"-12345 < -1 = {negBig < neg1}");
        Console.WriteLine($"12345 > -1 = {pos > neg1}");
        Console.WriteLine($"-1 == -1 = {neg1 == neg1}");     // equality stays sign-independent
        Console.WriteLine($"-1 != 0  = {neg1 != zero}");

        Console.WriteLine("== Int128 CompareTo through a generic ==");
        Console.WriteLine($"cmp(-1,0)={CmpAll(neg1, zero)} cmp(0,-1)={CmpAll(zero, neg1)} cmp(Min,Max)={CmpAll(min, max)}");

        Console.WriteLine("== UInt128 large ToString (unsigned side unaffected) ==");
        Console.WriteLine($"u(-1)  {UInt128.CreateTruncating(-1)}");                       // 2^128-1
        Console.WriteLine($"u(max64) {UInt128.CreateTruncating(0xFFFFFFFFFFFFFFFFUL)}");    // 18446744073709551615
        Console.WriteLine($"u(cmp) {(UInt128.CreateTruncating(0xFFFFFFFFFFFFFFFFUL) < UInt128.MaxValue)}");

        Console.WriteLine("== ulong reinterpreted signed then compared (BCL idiom) ==");
        Console.WriteLine($"(long)0xFF..FF < (long)0 = {SignedReinterpretLess(0xFFFFFFFFFFFFFFFFUL, 0UL)}"); // -1 < 0 = True
        Console.WriteLine($"(long)0 < (long)0xFF..FF = {SignedReinterpretLess(0UL, 0xFFFFFFFFFFFFFFFFUL)}"); // 0 < -1 = False
        Console.WriteLine($"(long)5 < (long)9        = {SignedReinterpretLess(5UL, 9UL)}");                  // 5 < 9 = True
    }
}
