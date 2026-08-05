using System;
using System.Globalization;

namespace GenericMathInt128Parse;

// The CONSTRAINED static-virtual mouth of Int128/UInt128.CreateTruncating — the exact
// Thrive path. Int128.Parse / UInt128.Parse route through
// System.Number.ParseBinaryInteger<TInteger>, whose body does
// `constrained. Int128 call INumberBase<TSelf>::CreateTruncating<TOther>` (over uint/int
// digit sources) — a static-abstract dispatch resolved to Int128.CreateTruncating. That
// impl's real body branches to TOther.TryConvertToTruncating (an InternalCall with no IL),
// so the constrained asker pair cuts it (Compilation.Reachability's static-virtual arm)
// and routes it to the same TranslateGenericIntrinsic widening the call-site pair uses.
// Without the constrained-pair wiring, Int128.Parse alone drags the InternalCall cascade.
//
// Negatives are validated word-level ((ulong)v / (ulong)(v>>64)) because dn2cpp formats a
// sign-negative Int128 as its unsigned reading today (a pre-existing formatting limitation,
// see Int128Conversions); positives and all UInt128 values round-trip through ToString.
internal static class Int128ParseConstrained
{
    static string W(Int128 v) => $"lo={(ulong)v} hi={(ulong)(v >> 64)}";
    static string W(UInt128 v) => $"lo={(ulong)v} hi={(ulong)(v >> 64)}";

    internal static void __GateEntry()
    {
        Console.WriteLine("== Int128.Parse (-> ParseBinaryInteger -> constrained CreateTruncating) ==");
        Console.WriteLine($"i128.Parse(0)          {W(Int128.Parse("0", CultureInfo.InvariantCulture))}");
        Console.WriteLine($"i128.Parse(123)        {W(Int128.Parse("123", CultureInfo.InvariantCulture))}");
        Console.WriteLine($"i128.Parse(-1)         {W(Int128.Parse("-1", CultureInfo.InvariantCulture))}");
        Console.WriteLine($"i128.Parse(-1234567890123) {W(Int128.Parse("-1234567890123", CultureInfo.InvariantCulture))}");
        // Int128.MaxValue and MinValue exactly.
        Console.WriteLine($"i128.Parse(max)        {W(Int128.Parse("170141183460469231731687303715884105727", CultureInfo.InvariantCulture))}");
        Console.WriteLine($"i128.Parse(min)        {W(Int128.Parse("-170141183460469231731687303715884105728", CultureInfo.InvariantCulture))}");
        // Positive round-trips through ToString (exact).
        Console.WriteLine($"i128.Parse(999...).str = {Int128.Parse("1234567890123456789", CultureInfo.InvariantCulture)}");

        Console.WriteLine("== UInt128.Parse (-> ParseBinaryInteger -> constrained CreateTruncating) ==");
        Console.WriteLine($"u128.Parse(0)          {W(UInt128.Parse("0", CultureInfo.InvariantCulture))}");
        Console.WriteLine($"u128.Parse(255)        {W(UInt128.Parse("255", CultureInfo.InvariantCulture))}");
        Console.WriteLine($"u128.Parse(max)        {W(UInt128.Parse("340282366920938463463374607431768211455", CultureInfo.InvariantCulture))}");
        // Full round-trip through ToString (unsigned formatting is exact).
        Console.WriteLine($"u128.Parse(max).str  = {UInt128.Parse("340282366920938463463374607431768211455", CultureInfo.InvariantCulture)}");
        Console.WriteLine($"u128.Parse(18446744073709551616).str = {UInt128.Parse("18446744073709551616", CultureInfo.InvariantCulture)}"); // 2^64
    }
}
