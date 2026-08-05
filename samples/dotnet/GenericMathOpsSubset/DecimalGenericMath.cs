using System;
using System.Numerics;

namespace GenericMathDecimal;

// The INumberBase<decimal> members beyond the operator/conversion core:
// IsCanonical (representation-sensitive — scale 0, or a mantissa without a
// trailing decimal zero), the integer-parity pair (an integer VALUE whatever
// its scale — 2.00m is even, 2.5m is neither even nor odd — with the parity
// taken from the truncated 96-bit mantissa), and the magnitude selectors
// (|x| vs |y| with the BCL tie-break: Max prefers the non-negative operand,
// Min the negative one, so equal-magnitude ties are representation-observable
// via ToString). Driven both as direct decimal statics and through
// constrained generic helpers (the static-abstract dispatch route); output
// diffs exact vs real .NET.
internal static class DecimalGenericMath
{
    static bool Even<T>(T v) where T : INumber<T> => T.IsEvenInteger(v);
    static bool Odd<T>(T v) where T : INumber<T> => T.IsOddInteger(v);
    static bool Canon<T>(T v) where T : INumber<T> => T.IsCanonical(v);
    static T MaxMag<T>(T x, T y) where T : INumber<T> => T.MaxMagnitude(x, y);
    static T MinMag<T>(T x, T y) where T : INumber<T> => T.MinMagnitude(x, y);
    static T MaxMagN<T>(T x, T y) where T : INumber<T> => T.MaxMagnitudeNumber(x, y);
    static T MinMagN<T>(T x, T y) where T : INumber<T> => T.MinMagnitudeNumber(x, y);

    internal static void __GateEntry()
    {
        Console.WriteLine("== Decimal generic-math statics (direct) ==");
        Console.WriteLine($"canonical 1.1 {decimal.IsCanonical(1.1m)} 1.100 {decimal.IsCanonical(1.100m)} default {decimal.IsCanonical(default)}");
        Console.WriteLine($"canonical 100 {decimal.IsCanonical(100m)} 0.0 {decimal.IsCanonical(0.0m)} 0.05 {decimal.IsCanonical(0.05m)} 0.50 {decimal.IsCanonical(0.50m)}");
        Console.WriteLine($"canonical max {decimal.IsCanonical(decimal.MaxValue)} tiny {decimal.IsCanonical(0.0000000000000000000000000001m)}");
        Console.WriteLine($"even 2 {decimal.IsEvenInteger(2m)} 3 {decimal.IsEvenInteger(3m)} 2.00 {decimal.IsEvenInteger(2.00m)} 2.5 {decimal.IsEvenInteger(2.5m)} -4 {decimal.IsEvenInteger(-4m)} 0.00 {decimal.IsEvenInteger(0.00m)}");
        Console.WriteLine($"odd 3 {decimal.IsOddInteger(3m)} 2 {decimal.IsOddInteger(2m)} 3.00 {decimal.IsOddInteger(3.00m)} 2.5 {decimal.IsOddInteger(2.5m)} -3 {decimal.IsOddInteger(-3m)} 1.5 {decimal.IsOddInteger(1.5m)}");
        Console.WriteLine($"even big {decimal.IsEvenInteger(79228162514264337593543950334m)} {decimal.IsEvenInteger(79228162514264337593543950335m)} {decimal.IsEvenInteger(18446744073709551616m)}");
        Console.WriteLine($"odd big {decimal.IsOddInteger(79228162514264337593543950335m)} {decimal.IsOddInteger(18446744073709551617m)} {decimal.IsOddInteger(18446744073709551616m)}");
        Console.WriteLine($"eps {decimal.IsEvenInteger(2.000000000000000000000000001m)} {decimal.IsOddInteger(2.000000000000000000000000001m)}");
        Console.WriteLine($"maxmag {decimal.MaxMagnitude(-5m, 5m)} {decimal.MaxMagnitude(-2m, 1m)} {decimal.MaxMagnitude(1.0m, 1.00m)} {decimal.MaxMagnitude(-1.0m, -1.00m)}");
        Console.WriteLine($"minmag {decimal.MinMagnitude(-5m, 5m)} {decimal.MinMagnitude(-2m, 1m)} {decimal.MinMagnitude(1.0m, 1.00m)} {decimal.MinMagnitude(-1.0m, -1.00m)}");

        Console.WriteLine("== Decimal generic-math statics (constrained) ==");
        Console.WriteLine($"canonical {Canon(1.1m)} {Canon(1.100m)} {Canon(0.0m)} {Canon(7m)}");
        Console.WriteLine($"even/odd {Even(2m)} {Even(2.00m)} {Even(2.5m)} {Odd(3m)} {Odd(-3.00m)} {Odd(1.5m)}");
        Console.WriteLine($"maxmag {MaxMag(-5m, 5m)} {MaxMag(1.0m, 1.00m)} {MaxMagN(-5m, 5m)} {MaxMagN(-1.0m, -1.00m)}");
        Console.WriteLine($"minmag {MinMag(-5m, 5m)} {MinMag(1.0m, 1.00m)} {MinMagN(-5m, 5m)} {MinMagN(-1.0m, -1.00m)}");
        // The primitive instantiations keep riding the integer magnitude rows.
        Console.WriteLine($"int {Even(6)} {Odd(7)} {Canon(42)} {MaxMag(-8, 3)} {MinMag(-8, 3)} {MaxMagN(-8, 8)} {MinMagN(-8, 8)}");
    }
}
