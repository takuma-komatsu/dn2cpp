using System;

namespace GenericMathInt128Convert;

// Int128/UInt128.CreateTruncating<TOther> over integer-primitive sources — the shape
// System.Number's Int128Converter/UInt128Converter parse paths reach (via Newtonsoft ->
// TypeDescriptor). Int128/UInt128 are ordinary transpiled BCL structs (two ulong fields,
// {_lower, _upper} little-endian), not primitives, so the primitive CreateTruncating
// blocks decline them; the real body branches to TOther.TryConvertToTruncating (an
// InternalCall with no IL). MethodCompiler.TranslateGenericIntrinsic lowers the widening
// — a pure sign/zero extension into {_lower, _upper} — inline (cut by MsInt128Create-
// Conversion): a signed negative source sign-extends (high word all ones), an unsigned
// source zero-extends.
//
// The full 128-bit result is pinned by its two 64-bit words — (ulong)v is _lower,
// (ulong)(v >> 64) is _upper — and each is diffed exact vs real .NET. Negative-Int128
// ToString and sign-crossing ordered compares are covered by the Int128SignedCompare
// section, which covers signed compare over unsigned storage; UInt128.ToString is
// exercised directly below since unsigned 128-bit formatting is exact.
internal static class Int128Conversions
{
    static string W(Int128 v) => $"lo={(ulong)v} hi={(ulong)(v >> 64)}";
    static string W(UInt128 v) => $"lo={(ulong)v} hi={(ulong)(v >> 64)}";

    internal static void __GateEntry()
    {
        Console.WriteLine("== Int128.CreateTruncating (word-level: sign/zero extension) ==");
        Console.WriteLine($"i128<-int(-1)          {W(Int128.CreateTruncating(-1))}");
        Console.WriteLine($"i128<-int.Min          {W(Int128.CreateTruncating(int.MinValue))}");
        Console.WriteLine($"i128<-uint(0xFFFFFFFF)  {W(Int128.CreateTruncating(0xFFFFFFFFu))}");
        Console.WriteLine($"i128<-long(-1)         {W(Int128.CreateTruncating(-1L))}");
        Console.WriteLine($"i128<-ulong(max)        {W(Int128.CreateTruncating(0xFFFFFFFFFFFFFFFFUL))}");
        Console.WriteLine($"i128<-long(1234567890123) {W(Int128.CreateTruncating(1234567890123L))}");
        Console.WriteLine($"i128<-int(0)            {W(Int128.CreateTruncating(0))}");
        Console.WriteLine($"i128<-short(-2)         {W(Int128.CreateTruncating((short)-2))}");
        Console.WriteLine($"i128<-byte(200)         {W(Int128.CreateTruncating((byte)200))}");

        Console.WriteLine("== UInt128.CreateTruncating (word-level: sign/zero extension) ==");
        Console.WriteLine($"u128<-int(-1)          {W(UInt128.CreateTruncating(-1))}");
        Console.WriteLine($"u128<-int(255)          {W(UInt128.CreateTruncating(255))}");
        Console.WriteLine($"u128<-long(-1)         {W(UInt128.CreateTruncating(-1L))}");
        Console.WriteLine($"u128<-ulong(max)        {W(UInt128.CreateTruncating(0xFFFFFFFFFFFFFFFFUL))}");
        Console.WriteLine($"u128<-uint(0xDEADBEEF)   {W(UInt128.CreateTruncating(0xDEADBEEFu))}");

        // UInt128.ToString is exact (unsigned 128-bit formatting); confirm the whole
        // formatted value for a couple of cases, including the all-ones bit pattern.
        Console.WriteLine("== UInt128.ToString (unsigned formatting) ==");
        Console.WriteLine($"u128<-int(-1).str  = {UInt128.CreateTruncating(-1)}");        // 340282366920938463463374607431768211455
        Console.WriteLine($"u128<-ulong(max).str = {UInt128.CreateTruncating(0xFFFFFFFFFFFFFFFFUL)}"); // 18446744073709551615
        Console.WriteLine($"i128<-long(pos).str = {Int128.CreateTruncating(1234567890123L)}");          // 1234567890123
    }
}
