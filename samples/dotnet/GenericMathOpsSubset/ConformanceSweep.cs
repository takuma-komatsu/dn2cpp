using System;
using System.Globalization;
using System.Numerics;

namespace GenericMathConformance;

// The conformance sweep over the remaining INumber<T>-family static-abstract
// surface, dispatched through constrained generic helpers so every member takes
// the static-virtual interface route (never the concrete type's plain static):
// INumber<T> (Clamp/CopySign/Max/Min/MaxNumber/MinNumber/Sign), INumberBase<T>
// (Abs, the magnitude selectors, Radix, and the category predicates not gated in
// FloatingPointMath), IMinMaxValue<T> / the identity interfaces (sub-word and
// unsigned bounds included — the storage-width MinValue/MaxValue), IBinaryNumber
// (IsPow2 / integer Log2 / AllBitsSet), IBinaryInteger (the bit counts, rotates
// and tuple-returning DivRem), IBitwiseOperators' ~, and the IParsable<T> /
// ISpanParsable<T> constrained parse route (invariant culture — culture-dependent
// parsing is out of scope). Types span the integer widths and signednesses plus
// double/float, with decimal and Half spot-checks where the interface is
// implemented. Integer members lower through the generic-math intrinsic table
// (or the interface default bodies it feeds); double/float resolve to the
// CoreLib Double/Single struct surface; Half transpiles its real IL. NaN and
// negative-zero edges pin the IEEE semantics the plain-vs-*Number split differs
// on (plain propagates NaN, *Number drops it). Output diffs exact vs real .NET.
internal static class ConformanceSweep
{
    // INumber<T>
    static T GClamp<T>(T v, T lo, T hi) where T : INumber<T> => T.Clamp(v, lo, hi);
    static T GCopySign<T>(T v, T s) where T : INumber<T> => T.CopySign(v, s);
    static T GMax<T>(T x, T y) where T : INumber<T> => T.Max(x, y);
    static T GMin<T>(T x, T y) where T : INumber<T> => T.Min(x, y);
    static T GMaxNumber<T>(T x, T y) where T : INumber<T> => T.MaxNumber(x, y);
    static T GMinNumber<T>(T x, T y) where T : INumber<T> => T.MinNumber(x, y);
    static int GSign<T>(T v) where T : INumber<T> => T.Sign(v);

    // INumberBase<T>
    static T GAbs<T>(T v) where T : INumberBase<T> => T.Abs(v);
    static T GMaxMagnitude<T>(T x, T y) where T : INumberBase<T> => T.MaxMagnitude(x, y);
    static T GMinMagnitude<T>(T x, T y) where T : INumberBase<T> => T.MinMagnitude(x, y);
    static T GMaxMagnitudeNumber<T>(T x, T y) where T : INumberBase<T> => T.MaxMagnitudeNumber(x, y);
    static T GMinMagnitudeNumber<T>(T x, T y) where T : INumberBase<T> => T.MinMagnitudeNumber(x, y);
    static int GRadix<T>() where T : INumberBase<T> => T.Radix;
    static bool GIsCanonical<T>(T v) where T : INumberBase<T> => T.IsCanonical(v);
    static bool GIsComplexNumber<T>(T v) where T : INumberBase<T> => T.IsComplexNumber(v);
    static bool GIsImaginaryNumber<T>(T v) where T : INumberBase<T> => T.IsImaginaryNumber(v);
    static bool GIsRealNumber<T>(T v) where T : INumberBase<T> => T.IsRealNumber(v);
    static bool GIsEvenInteger<T>(T v) where T : INumberBase<T> => T.IsEvenInteger(v);
    static bool GIsOddInteger<T>(T v) where T : INumberBase<T> => T.IsOddInteger(v);
    static bool GIsNormal<T>(T v) where T : INumberBase<T> => T.IsNormal(v);
    static bool GIsZero<T>(T v) where T : INumberBase<T> => T.IsZero(v);
    static bool GIsInteger<T>(T v) where T : INumberBase<T> => T.IsInteger(v);
    static bool GIsFinite<T>(T v) where T : INumberBase<T> => T.IsFinite(v);

    // IMinMaxValue<T> / the identity interfaces
    static T GMinValue<T>() where T : IMinMaxValue<T> => T.MinValue;
    static T GMaxValue<T>() where T : IMinMaxValue<T> => T.MaxValue;
    static T GAdditiveIdentity<T>() where T : IAdditiveIdentity<T, T> => T.AdditiveIdentity;
    static T GMultiplicativeIdentity<T>() where T : IMultiplicativeIdentity<T, T> => T.MultiplicativeIdentity;

    // IBinaryNumber<T>
    static bool GIsPow2<T>(T v) where T : IBinaryNumber<T> => T.IsPow2(v);
    static T GLog2<T>(T v) where T : IBinaryNumber<T> => T.Log2(v);
    static T GAllBitsSet<T>() where T : IBinaryNumber<T> => T.AllBitsSet;

    // IBinaryInteger<T> / IBitwiseOperators
    static T GLzc<T>(T v) where T : IBinaryInteger<T> => T.LeadingZeroCount(v);
    static T GTzc<T>(T v) where T : IBinaryInteger<T> => T.TrailingZeroCount(v);
    static T GPopCount<T>(T v) where T : IBinaryInteger<T> => T.PopCount(v);
    static T GRotL<T>(T v, int n) where T : IBinaryInteger<T> => T.RotateLeft(v, n);
    static T GRotR<T>(T v, int n) where T : IBinaryInteger<T> => T.RotateRight(v, n);
    static (T Quotient, T Remainder) GDivRem<T>(T a, T b) where T : IBinaryInteger<T> => T.DivRem(a, b);
    static T GNot<T>(T v) where T : IBitwiseOperators<T, T, T> => ~v;

    // IParsable<T> / ISpanParsable<T> — invariant culture throughout.
    static T GParse<T>(string s) where T : IParsable<T> => T.Parse(s, CultureInfo.InvariantCulture);
    static (bool Ok, T Value) GTryParse<T>(string s) where T : IParsable<T>
    {
        bool ok = T.TryParse(s, CultureInfo.InvariantCulture, out T v);
        return (ok, v);
    }
    static T GParseSpan<T>(ReadOnlySpan<char> s) where T : ISpanParsable<T> => T.Parse(s, CultureInfo.InvariantCulture);
    static (bool Ok, T Value) GTryParseSpan<T>(ReadOnlySpan<char> s) where T : ISpanParsable<T>
    {
        bool ok = T.TryParse(s, CultureInfo.InvariantCulture, out T v);
        return (ok, v);
    }

    internal static void __GateEntry()
    {
        Console.WriteLine("== Sweep: INumber Clamp / CopySign ==");
        Console.WriteLine($"int    {GClamp(5, 1, 3)} {GClamp(-5, -3, 3)} {GClamp(0, -3, 3)}");
        Console.WriteLine($"uint   {GClamp(9u, 1u, 6u)} {GClamp(4294967295u, 0u, 10u)}");
        Console.WriteLine($"long   {GClamp(-9000000000L, -100L, 100L)} {GClamp(50L, -100L, 100L)}");
        Console.WriteLine($"ulong  {GClamp(18446744073709551615UL, 1UL, 9UL)}");
        Console.WriteLine($"short  {GClamp((short)-30000, (short)-100, (short)100)}");
        Console.WriteLine($"byte   {GClamp((byte)200, (byte)10, (byte)20)}");
        Console.WriteLine($"sbyte  {GClamp((sbyte)-100, (sbyte)-5, (sbyte)5)}");
        Console.WriteLine($"double {GClamp(2.5, 0.0, 1.0)} {GClamp(double.NaN, 0.0, 1.0)}");
        Console.WriteLine($"float  {GClamp(-2.5f, -1.0f, 1.0f)}");
        Console.WriteLine($"int    {GCopySign(5, -1)} {GCopySign(-5, 10)} {GCopySign(7, 0)}");
        Console.WriteLine($"long   {GCopySign(-9000000000L, 1L)} {GCopySign(9000000000L, -1L)}");
        Console.WriteLine($"short  {GCopySign((short)100, (short)-3)}");
        Console.WriteLine($"sbyte  {GCopySign((sbyte)-8, (sbyte)1)}");
        Console.WriteLine($"uint   {GCopySign(123u, 456u)}");
        Console.WriteLine($"double {GCopySign(3.5, -0.0)} {GCopySign(-3.5, 0.0)}");
        Console.WriteLine($"float  {GCopySign(2.5f, -1.0f)}");
        // CopySign(-0.0, +1) is +0: pin the sign via the 1/x infinity test.
        Console.WriteLine(double.IsPositiveInfinity(1.0 / GCopySign(-0.0, 5.0)));

        Console.WriteLine("== Sweep: INumber Max / Min / MaxNumber / MinNumber ==");
        Console.WriteLine($"int    {GMax(3, -7)} {GMin(3, -7)} {GMaxNumber(3, -7)} {GMinNumber(3, -7)}");
        Console.WriteLine($"uint   {GMax(2147483648u, 7u)} {GMin(2147483648u, 7u)}");
        Console.WriteLine($"long   {GMax(-9000000000L, 1L)} {GMin(-9000000000L, 1L)}");
        Console.WriteLine($"ulong  {GMax(18446744073709551615UL, 1UL)} {GMin(18446744073709551615UL, 1UL)}");
        Console.WriteLine($"short  {GMax((short)-2, (short)-3)} {GMin((short)-2, (short)-3)}");
        Console.WriteLine($"byte   {GMax((byte)200, (byte)100)} {GMin((byte)200, (byte)100)}");
        Console.WriteLine($"sbyte  {GMax((sbyte)-2, (sbyte)3)} {GMin((sbyte)-2, (sbyte)3)}");
        Console.WriteLine($"double {GMax(2.5, -3.5)} {GMin(2.5, -3.5)} {GMaxNumber(2.5, -3.5)} {GMinNumber(2.5, -3.5)}");
        Console.WriteLine($"float  {GMax(2.5f, -3.5f)} {GMin(2.5f, -3.5f)}");
        // NaN split: plain Max/Min propagate NaN, the *Number forms drop it.
        Console.WriteLine($"{GMax(double.NaN, 1.0)} {GMin(double.NaN, 1.0)} {GMaxNumber(double.NaN, 1.0)} {GMinNumber(double.NaN, 1.0)}");
        Console.WriteLine($"{GMax(float.NaN, 1.0f)} {GMaxNumber(float.NaN, 1.0f)} {GMinNumber(1.0f, float.NaN)}");
        // Signed-zero ordering: -0 sorts below +0 for Max/Min.
        Console.WriteLine(double.IsNegativeInfinity(1.0 / GMin(0.0, -0.0)));
        Console.WriteLine(double.IsPositiveInfinity(1.0 / GMax(-0.0, 0.0)));

        Console.WriteLine("== Sweep: INumber Sign ==");
        Console.WriteLine($"int    {GSign(-7)} {GSign(0)} {GSign(7)}");
        Console.WriteLine($"long   {GSign(-9000000000L)} {GSign(9000000000L)}");
        Console.WriteLine($"sbyte  {GSign((sbyte)-1)} {GSign((sbyte)0)}");
        Console.WriteLine($"ulong  {GSign(0UL)} {GSign(5UL)}");
        Console.WriteLine($"double {GSign(-2.5)} {GSign(0.0)} {GSign(2.5)}");
        Console.WriteLine($"float  {GSign(-0.5f)} {GSign(0.5f)}");
        // Sign(NaN) raises ArithmeticException (the concrete Double.Sign, not the
        // interface default body's sign-bit answer).
        try
        {
            Console.WriteLine(GSign(double.NaN));
        }
        catch (ArithmeticException)
        {
            Console.WriteLine("ArithmeticException");
        }

        Console.WriteLine("== Sweep: INumberBase Abs / magnitude selectors ==");
        Console.WriteLine($"int    {GAbs(-7)} {GAbs(7)}");
        Console.WriteLine($"long   {GAbs(-9000000000L)}");
        Console.WriteLine($"short  {GAbs((short)-30000)}");
        Console.WriteLine($"sbyte  {GAbs((sbyte)-7)}");
        Console.WriteLine($"uint   {GAbs(4294967295u)} byte {GAbs((byte)200)} ulong {GAbs(9UL)}");
        Console.WriteLine($"double {GAbs(-2.5)} float {GAbs(-1.5f)}");
        Console.WriteLine($"int    {GMaxMagnitude(2, -3)} {GMinMagnitude(-4, 3)} {GMaxMagnitude(-3, 3)} {GMinMagnitude(-3, 3)}");
        Console.WriteLine($"int    {GMaxMagnitude(int.MinValue, 5)} {GMinMagnitude(int.MinValue, 5)}");
        Console.WriteLine($"int    {GMaxMagnitudeNumber(2, -3)} {GMinMagnitudeNumber(-4, 3)}");
        Console.WriteLine($"long   {GMaxMagnitude(-9000000000L, 2L)} {GMinMagnitude(long.MinValue, -1L)}");
        Console.WriteLine($"short  {GMaxMagnitude((short)-100, (short)99)} {GMinMagnitude((short)-100, (short)99)}");
        Console.WriteLine($"sbyte  {GMaxMagnitude((sbyte)-128, (sbyte)1)} {GMinMagnitude((sbyte)-128, (sbyte)1)}");
        Console.WriteLine($"uint   {GMaxMagnitude(2147483648u, 7u)} {GMinMagnitude(2147483648u, 7u)}");
        Console.WriteLine($"ulong  {GMaxMagnitude(1UL, 2UL)} byte {GMinMagnitude((byte)9, (byte)3)}");
        Console.WriteLine($"double {GMaxMagnitude(2.0, -3.0)} {GMinMagnitude(-4.0, 3.0)} {GMaxMagnitudeNumber(2.0, -3.0)} {GMinMagnitudeNumber(-4.0, 3.0)}");
        // NaN split on the magnitude selectors: plain propagates, *Number drops.
        Console.WriteLine($"{GMaxMagnitude(double.NaN, 3.0)} {GMinMagnitude(double.NaN, 3.0)} {GMaxMagnitudeNumber(double.NaN, 3.0)} {GMinMagnitudeNumber(double.NaN, 3.0)}");
        Console.WriteLine($"{GMinMagnitudeNumber(float.NaN, -3.0f)} {GMaxMagnitudeNumber(float.NaN, -3.0f)}");
        // Equal-magnitude tie on floats prefers the negative for Min, positive for Max.
        Console.WriteLine($"{GMaxMagnitude(-3.0, 3.0)} {GMinMagnitude(-3.0, 3.0)}");

        Console.WriteLine("== Sweep: INumberBase Radix / category predicates ==");
        Console.WriteLine($"{GRadix<int>()} {GRadix<byte>()} {GRadix<ulong>()} {GRadix<double>()} {GRadix<float>()} {GRadix<decimal>()} {GRadix<Half>()}");
        Console.WriteLine($"int    {GIsCanonical(5)} {GIsComplexNumber(5)} {GIsImaginaryNumber(5)} {GIsRealNumber(5)}");
        Console.WriteLine($"int    {GIsInteger(5)} {GIsFinite(5)} {GIsZero(0)} {GIsZero(5)}");
        Console.WriteLine($"int    {GIsEvenInteger(4)} {GIsEvenInteger(7)} {GIsOddInteger(7)} {GIsOddInteger(-3)}");
        Console.WriteLine($"int    {GIsNormal(0)} {GIsNormal(5)} {GIsNormal(-5)}");
        Console.WriteLine($"sbyte  {GIsEvenInteger((sbyte)-4)} {GIsOddInteger((sbyte)-7)}");
        Console.WriteLine($"byte   {GIsEvenInteger((byte)200)} {GIsOddInteger((byte)201)}");
        Console.WriteLine($"long   {GIsEvenInteger(-9000000000L)} {GIsOddInteger(9000000001L)}");
        Console.WriteLine($"ulong  {GIsEvenInteger(18446744073709551615UL)} {GIsOddInteger(18446744073709551615UL)}");
        Console.WriteLine($"short  {GIsNormal((short)0)} {GIsCanonical((short)-3)} ushort {GIsEvenInteger((ushort)65534)}");
        Console.WriteLine($"double {GIsCanonical(2.5)} {GIsComplexNumber(2.5)} {GIsImaginaryNumber(2.5)}");
        Console.WriteLine($"double {GIsRealNumber(2.5)} {GIsRealNumber(double.NaN)}");
        Console.WriteLine($"double {GIsEvenInteger(4.0)} {GIsEvenInteger(4.5)} {GIsOddInteger(5.0)} {GIsOddInteger(5.5)}");
        Console.WriteLine($"double {GIsNormal(1.5)} {GIsNormal(double.Epsilon)} {GIsNormal(0.0)}");
        Console.WriteLine($"double {GIsZero(0.0)} {GIsZero(-0.0)} {GIsZero(1.0)}");
        Console.WriteLine($"float  {GIsCanonical(2.5f)} {GIsRealNumber(float.NaN)} {GIsEvenInteger(6.0f)} {GIsOddInteger(3.0f)} {GIsNormal(1.5f)} {GIsZero(-0.0f)}");

        Console.WriteLine("== Sweep: IMinMaxValue / identities (storage widths) ==");
        Console.WriteLine($"byte   {GMinValue<byte>()} {GMaxValue<byte>()}");
        Console.WriteLine($"sbyte  {GMinValue<sbyte>()} {GMaxValue<sbyte>()}");
        Console.WriteLine($"short  {GMinValue<short>()} {GMaxValue<short>()}");
        Console.WriteLine($"ushort {GMinValue<ushort>()} {GMaxValue<ushort>()}");
        Console.WriteLine($"int    {GMinValue<int>()} {GMaxValue<int>()}");
        Console.WriteLine($"uint   {GMinValue<uint>()} {GMaxValue<uint>()}");
        Console.WriteLine($"long   {GMinValue<long>()} {GMaxValue<long>()}");
        Console.WriteLine($"ulong  {GMinValue<ulong>()} {GMaxValue<ulong>()}");
        Console.WriteLine($"float  {GMinValue<float>()} {GMaxValue<float>()}");
        Console.WriteLine($"double {GMinValue<double>()} {GMaxValue<double>()}");
        Console.WriteLine($"{GAdditiveIdentity<int>()} {GAdditiveIdentity<byte>()} {GAdditiveIdentity<double>()}");
        Console.WriteLine($"{GMultiplicativeIdentity<int>()} {GMultiplicativeIdentity<ulong>()} {GMultiplicativeIdentity<double>()}");

        Console.WriteLine("== Sweep: IBinaryNumber IsPow2 / Log2 / AllBitsSet ==");
        Console.WriteLine($"int    {GIsPow2(0)} {GIsPow2(1)} {GIsPow2(64)} {GIsPow2(63)} {GIsPow2(-4)}");
        Console.WriteLine($"uint   {GIsPow2(2147483648u)} {GIsPow2(3u)}");
        Console.WriteLine($"long   {GIsPow2(4611686018427387904L)} {GIsPow2(-2L)}");
        Console.WriteLine($"ulong  {GIsPow2(9223372036854775808UL)} {GIsPow2(0UL)}");
        Console.WriteLine($"byte   {GIsPow2((byte)128)} {GIsPow2((byte)0)}");
        Console.WriteLine($"sbyte  {GIsPow2((sbyte)64)} {GIsPow2((sbyte)-64)}");
        Console.WriteLine($"short  {GIsPow2((short)16384)} {GIsPow2((short)-16384)}");
        Console.WriteLine($"double {GIsPow2(8.0)} {GIsPow2(6.0)} float {GIsPow2(0.25f)}");
        Console.WriteLine($"int    {GLog2(1)} {GLog2(1024)} {GLog2(0)}");
        Console.WriteLine($"uint   {GLog2(2147483648u)} long {GLog2(1099511627776L)} ulong {GLog2(18446744073709551615UL)}");
        Console.WriteLine($"byte   {GLog2((byte)255)} short {GLog2((short)16384)} sbyte {GLog2((sbyte)64)} ushort {GLog2((ushort)65535)}");
        Console.WriteLine($"{GAllBitsSet<int>()} {GAllBitsSet<uint>()} {GAllBitsSet<sbyte>()} {GAllBitsSet<ushort>()} {GAllBitsSet<long>()} {GAllBitsSet<ulong>()}");

        Console.WriteLine("== Sweep: IBinaryInteger bit counts ==");
        Console.WriteLine($"lzc  int {GLzc(1)} {GLzc(0)} uint {GLzc(2147483648u)} long {GLzc(1L)} ulong {GLzc(0UL)}");
        Console.WriteLine($"lzc  byte {GLzc((byte)1)} {GLzc((byte)0)} sbyte {GLzc((sbyte)-1)} short {GLzc((short)16384)} ushort {GLzc((ushort)0)}");
        Console.WriteLine($"tzc  int {GTzc(8)} {GTzc(0)} uint {GTzc(2147483648u)} long {GTzc(1099511627776L)} ulong {GTzc(0UL)}");
        Console.WriteLine($"tzc  byte {GTzc((byte)0)} {GTzc((byte)128)} sbyte {GTzc((sbyte)-128)} short {GTzc((short)0)} ushort {GTzc((ushort)512)}");
        Console.WriteLine($"pop  int {GPopCount(-1)} {GPopCount(0x0F0F)} uint {GPopCount(4294967295u)} long {GPopCount(-1L)} ulong {GPopCount(255UL)}");
        Console.WriteLine($"pop  byte {GPopCount((byte)255)} sbyte {GPopCount((sbyte)-1)} short {GPopCount((short)-1)} ushort {GPopCount((ushort)0xF0F0)}");

        Console.WriteLine("== Sweep: IBinaryInteger rotates / DivRem / ones-complement ==");
        Console.WriteLine($"int    {GRotL(0x12345678, 8):X} {GRotR(0x12345678, 8):X} {GRotL(1, 32)} {GRotL(-2, 1)}");
        Console.WriteLine($"uint   {GRotL(0x80000001u, 1)} {GRotR(0x80000001u, 1)}");
        Console.WriteLine($"long   {GRotL(1L, 64)} {GRotL(long.MinValue, 1)} ulong {GRotR(0x8000000000000001UL, 4)}");
        Console.WriteLine($"byte   {GRotL((byte)0x81, 1)} {GRotR((byte)0x81, 1)} {GRotL((byte)1, 8)} {GRotL((byte)1, 11)}");
        Console.WriteLine($"sbyte  {GRotL((sbyte)-8, 1)} {GRotR((sbyte)-8, 1)}");
        Console.WriteLine($"short  {GRotL((short)0x4001, 2)} {GRotR((short)0x4001, 2)} ushort {GRotL((ushort)0x8001, 4)}");
        var (qi, ri) = GDivRem(7, 3);
        var (qn, rn) = GDivRem(-7, 3);
        Console.WriteLine($"int    {qi} {ri} {qn} {rn}");
        var (ql, rl) = GDivRem(9000000000L, 7L);
        Console.WriteLine($"long   {ql} {rl}");
        var (qb, rb) = GDivRem((byte)200, (byte)7);
        var (qs, rs) = GDivRem((short)-100, (short)7);
        var (qu, ru) = GDivRem(18446744073709551615UL, 10UL);
        Console.WriteLine($"byte   {qb} {rb} short {qs} {rs} ulong {qu} {ru}");
        Console.WriteLine($"not    {GNot(5)} {GNot(0u)} {GNot((byte)0x0F)} {GNot((sbyte)0)} {GNot((ushort)0)} {GNot(0L)} {GNot(0UL)} {GNot((short)-1)}");

        Console.WriteLine("== Sweep: IParsable / ISpanParsable (invariant) ==");
        Console.WriteLine($"int    {GParse<int>("123")} {GParse<int>("-456")}");
        Console.WriteLine($"long   {GParse<long>("9000000000")} {GParse<long>("-42")}");
        Console.WriteLine($"double {GParse<double>("1.5")} {GParse<double>("-0.25")} {GParse<double>("1e3")}");
        Console.WriteLine($"float  {GParse<float>("2.5")}");
        Console.WriteLine($"byte   {GParse<byte>("200")} short {GParse<short>("-1234")} sbyte {GParse<sbyte>("-12")}");
        var (i1, v1) = GTryParse<int>("789");
        var (i2, v2) = GTryParse<int>("abc");
        Console.WriteLine($"int    {i1} {v1} {i2} {v2}");
        var (l1, w1) = GTryParse<long>("-9000000000");
        Console.WriteLine($"long   {l1} {w1}");
        var (d1, x1) = GTryParse<double>("3.25");
        var (d2, x2) = GTryParse<double>("zz");
        Console.WriteLine($"double {d1} {x1} {d2} {x2}");
        var (b1, y1) = GTryParse<byte>("999");
        Console.WriteLine($"byte   {b1} {y1}");
        Console.WriteLine($"span   {GParseSpan<int>("321".AsSpan())} {GParseSpan<long>("-77".AsSpan())} {GParseSpan<double>("0.5".AsSpan())}");
        var (s1, z1) = GTryParseSpan<int>("654".AsSpan());
        var (s2, z2) = GTryParseSpan<int>("zz".AsSpan());
        var (s3, z3) = GTryParseSpan<double>("-1.25".AsSpan());
        Console.WriteLine($"span   {s1} {z1} {s2} {z2} {s3} {z3}");

        Console.WriteLine("== Sweep: decimal / Half spot-checks ==");
        Console.WriteLine($"dec    {GClamp(5m, 1m, 3m)} {GAbs(-1.5m)} {GMax(2m, 3m)} {GMin(2m, 3m)}");
        Console.WriteLine($"dec    {GSign(-2m)} {GSign(0m)} {GSign(3m)}");
        Console.WriteLine($"dec    {GMinValue<decimal>()} {GMaxValue<decimal>()}");
        Console.WriteLine($"dec    {GParse<decimal>("123.45")} {GIsZero(0m)} {GIsInteger(3m)}");
        Console.WriteLine($"half   {GClamp((Half)2.5f, (Half)0f, (Half)1f)} {GAbs((Half)(-1.5f))} {GSign((Half)(-2f))}");
        Console.WriteLine($"half   {GMinValue<Half>()} {GMaxValue<Half>()} {GIsPow2((Half)4f)}");
        Console.WriteLine($"half   {GParse<Half>("1.5")}");
    }
}
