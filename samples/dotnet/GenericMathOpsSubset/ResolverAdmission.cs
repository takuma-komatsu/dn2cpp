using System;
using System.Globalization;
using System.Numerics;
using System.Text;

namespace GenericMathResolverAdmission;

// The integer-primitive constrained static-virtual resolver admission: TSelf
// closed to any of the eight integer widths resolves against its CoreLib struct
// like double/float/decimal do, instead of the former parse-only rescue. Covers
// the shapes that resolution unlocks — the INumberBase<T> Parse/TryParse
// NumberStyles/span forms (previously the loud fail-close), the IParsable /
// ISpanParsable route across every width, TryFormat via ISpanFormattable and
// IUtf8SpanFormattable generic helpers — plus the regression net for the new
// routing split: members carrying a default interface body (Clamp/CopySign)
// where a word-size TSelf keeps the transpiled default and a sub-word TSelf now
// direct-calls its real plain-named body, the CopySign(MinValue) overflow trap
// those real bodies raise, and the T.Log2 negative-signed-input
// ArgumentOutOfRangeException with the BitOperations.Log2 no-throw contrast.
// Invariant culture throughout; output diffs exact vs real .NET.
internal static class ResolverAdmission
{
    // IParsable<T> / ISpanParsable<T> — the plain provider shapes.
    static T ParseIt<T>(string s) where T : IParsable<T> => T.Parse(s, CultureInfo.InvariantCulture);
    static T SpanParseIt<T>(ReadOnlySpan<char> s) where T : ISpanParsable<T>
        => T.Parse(s, CultureInfo.InvariantCulture);

    // INumberBase<T> — the NumberStyles shapes (static abstract, no default
    // body, no generic-math table arm: exactly the members only the resolver
    // admission can land on the NumberStyles-engine intrinsics).
    static T NumParse<T>(string s, NumberStyles st) where T : INumberBase<T>
        => T.Parse(s, st, CultureInfo.InvariantCulture);
    static T NumParseSpan<T>(ReadOnlySpan<char> s, NumberStyles st) where T : INumberBase<T>
        => T.Parse(s, st, CultureInfo.InvariantCulture);
    static (bool Ok, T Value) NumTryParse<T>(string s, NumberStyles st) where T : INumberBase<T>
    {
        bool ok = T.TryParse(s, st, CultureInfo.InvariantCulture, out T v);
        return (ok, v);
    }
    static (bool Ok, T Value) NumTryParseSpan<T>(ReadOnlySpan<char> s, NumberStyles st) where T : INumberBase<T>
    {
        bool ok = T.TryParse(s, st, CultureInfo.InvariantCulture, out T v);
        return (ok, v);
    }

    // Default-interface-body members (INumber<T>.Clamp/CopySign): a word-size
    // TSelf transpiles the interface default (the resolver skips a default-bodied
    // member on an intrinsic-mapped struct), a sub-word TSelf resolves the real
    // plain-named body and direct-calls it — both must match real .NET, including
    // the CopySign(MinValue) two's-complement overflow trap the real bodies raise.
    static T GClamp<T>(T v, T lo, T hi) where T : INumber<T> => T.Clamp(v, lo, hi);
    static T GCopySign<T>(T v, T s) where T : INumber<T> => T.CopySign(v, s);

    // INumberBase<T>.CreateChecked with a non-primitive TOther (decimal): the
    // transpiled default body reaches TSelf.TryConvertFromChecked.
    static T FromDec<T>(decimal d) where T : INumberBase<T> => T.CreateChecked(d);

    // ISpanFormattable / IUtf8SpanFormattable via constrained callvirt.
    static string FmtSpan<T>(T v, string fmt) where T : ISpanFormattable
    {
        Span<char> buf = stackalloc char[64];
        return v.TryFormat(buf, out int w, fmt, CultureInfo.InvariantCulture)
            ? new string(buf[..w])
            : "fail";
    }
    static string FmtUtf8<T>(T v, string fmt) where T : IUtf8SpanFormattable
    {
        Span<byte> buf = stackalloc byte[64];
        return v.TryFormat(buf, out int w, fmt, CultureInfo.InvariantCulture)
            ? Encoding.UTF8.GetString(buf[..w])
            : "fail";
    }

    // IBinaryNumber<T>.Log2 — signed negative input throws, unsigned never.
    static T GLog2<T>(T v) where T : IBinaryNumber<T> => T.Log2(v);
    static string Log2OrThrow<T>(T v) where T : IBinaryNumber<T>
    {
        try
        {
            return GLog2(v).ToString();
        }
        catch (ArgumentOutOfRangeException)
        {
            return "ArgumentOutOfRangeException";
        }
    }

    internal static void __GateEntry()
    {
        Console.WriteLine("== Admission: IParsable / ISpanParsable, all widths ==");
        Console.WriteLine($"{ParseIt<byte>("200")} {ParseIt<sbyte>("-100")} {ParseIt<short>("-30000")} {ParseIt<ushort>("60000")}");
        Console.WriteLine($"{ParseIt<int>("-2000000000")} {ParseIt<uint>("4000000000")} {ParseIt<long>("-9000000000000000000")} {ParseIt<ulong>("18000000000000000000")}");
        Console.WriteLine($"{SpanParseIt<byte>("255".AsSpan())} {SpanParseIt<sbyte>("-128".AsSpan())} {SpanParseIt<short>("32767".AsSpan())} {SpanParseIt<ushort>("65535".AsSpan())}");
        Console.WriteLine($"{SpanParseIt<int>("-1".AsSpan())} {SpanParseIt<uint>("1".AsSpan())} {SpanParseIt<long>("-2".AsSpan())} {SpanParseIt<ulong>("2".AsSpan())}");

        Console.WriteLine("== Admission: INumberBase Parse/TryParse styles, all widths ==");
        Console.WriteLine($"{NumParse<byte>("7f", NumberStyles.HexNumber)} {NumParse<sbyte>("f0", NumberStyles.HexNumber)} {NumParse<short>("7fff", NumberStyles.HexNumber)} {NumParse<ushort>("ffff", NumberStyles.HexNumber)}");
        Console.WriteLine($"{NumParse<int>("deadbeef", NumberStyles.HexNumber)} {NumParse<uint>("deadbeef", NumberStyles.HexNumber)} {NumParse<long>("7fffffffffffffff", NumberStyles.HexNumber)} {NumParse<ulong>("ffffffffffffffff", NumberStyles.HexNumber)}");
        Console.WriteLine($"{NumParse<int>(" 42 ", NumberStyles.Integer)} {NumParse<long>("1,234,567", NumberStyles.Integer | NumberStyles.AllowThousands)}");
        Console.WriteLine($"{NumParseSpan<byte>("ff".AsSpan(), NumberStyles.HexNumber)} {NumParseSpan<short>("-42".AsSpan(), NumberStyles.Integer)} {NumParseSpan<uint>("ffffffff".AsSpan(), NumberStyles.HexNumber)} {NumParseSpan<long>("-9000000000".AsSpan(), NumberStyles.Integer)}");
        var (b1, bv) = NumTryParse<byte>("1ff", NumberStyles.HexNumber);
        var (b2, sv) = NumTryParse<sbyte>("-12", NumberStyles.Integer);
        var (b3, uv) = NumTryParse<ulong>("zz", NumberStyles.HexNumber);
        Console.WriteLine($"{b1} {bv} {b2} {sv} {b3} {uv}");
        var (t1, w1) = NumTryParseSpan<short>("-42".AsSpan(), NumberStyles.Integer);
        var (t2, w2) = NumTryParseSpan<ushort>("fffff".AsSpan(), NumberStyles.HexNumber);
        var (t3, w3) = NumTryParseSpan<int>("cafe".AsSpan(), NumberStyles.HexNumber);
        var (t4, w4) = NumTryParseSpan<ulong>("10".AsSpan(), NumberStyles.HexNumber);
        Console.WriteLine($"{t1} {w1} {t2} {w2} {t3} {w3} {t4} {w4}");

        Console.WriteLine("== Admission: default-body Clamp/CopySign, word vs sub-word ==");
        Console.WriteLine($"{GClamp((byte)200, (byte)10, (byte)20)} {GClamp((sbyte)-100, (sbyte)-5, (sbyte)5)} {GClamp((short)-30000, (short)-100, (short)100)} {GClamp((ushort)60000, (ushort)1, (ushort)9)}");
        Console.WriteLine($"{GClamp(5, 1, 3)} {GClamp(9u, 1u, 6u)} {GClamp(-9000000000L, -100L, 100L)} {GClamp(18446744073709551615UL, 1UL, 9UL)}");
        Console.WriteLine($"{GCopySign((sbyte)8, (sbyte)-1)} {GCopySign((short)-100, (short)3)} {GCopySign(7, -2)} {GCopySign(-9000000000L, 1L)}");
        try
        {
            // Computed into a local: WriteLine(<always-throwing expr>) inside a
            // try block trips a real-.NET JIT InvalidProgramException (net10).
            short cs = GCopySign(short.MinValue, (short)1);
            Console.WriteLine(cs);
        }
        catch (OverflowException)
        {
            Console.WriteLine("OverflowException");
        }
        Console.WriteLine($"{FromDec<int>(123m)} {FromDec<byte>(255m)} {FromDec<long>(-5m)} {FromDec<ushort>(65535m)}");

        Console.WriteLine("== Admission: ISpanFormattable TryFormat, all widths ==");
        Console.WriteLine($"{FmtSpan((byte)200, "D4")} {FmtSpan((sbyte)-100, "X2")} {FmtSpan((short)-30000, "N0")} {FmtSpan((ushort)60000, "X4")}");
        Console.WriteLine($"{FmtSpan(-123456789, "N2")} {FmtSpan(4000000000u, "X8")} {FmtSpan(-9000000000L, "D")} {FmtSpan(18000000000000000000UL, "X16")}");

        Console.WriteLine("== Admission: IUtf8SpanFormattable TryFormat, all widths ==");
        Console.WriteLine($"{FmtUtf8((byte)255, "D3")} {FmtUtf8((sbyte)-128, "D")} {FmtUtf8((short)32767, "X4")} {FmtUtf8((ushort)65535, "D5")}");
        Console.WriteLine($"{FmtUtf8(-2000000000, "N0")} {FmtUtf8(4294967295u, "D")} {FmtUtf8(long.MinValue, "D")} {FmtUtf8(ulong.MaxValue, "X")}");

        Console.WriteLine("== Admission: Log2 negative-signed throw / BitOperations contrast ==");
        Console.WriteLine($"{Log2OrThrow((sbyte)-1)} {Log2OrThrow((short)-5)} {Log2OrThrow(-1)} {Log2OrThrow(long.MinValue)}");
        Console.WriteLine($"{Log2OrThrow((sbyte)64)} {Log2OrThrow((short)16384)} {Log2OrThrow(0)} {Log2OrThrow(1099511627776L)}");
        Console.WriteLine($"{Log2OrThrow((byte)0)} {Log2OrThrow((ushort)65535)} {Log2OrThrow(4294967295u)} {Log2OrThrow(18446744073709551615UL)}");
        Console.WriteLine($"int.Log2(0)={int.Log2(0)} BitOperations.Log2(0u)={BitOperations.Log2(0u)} BitOperations.Log2(0ul)={BitOperations.Log2(0ul)}");
        // Throw inputs arrive via Parse into locals: a constant always-throwing
        // int.Log2/long.Log2 call inside a try block trips a real-.NET (net10)
        // JIT InvalidProgramException, so keep the operands runtime-opaque.
        try
        {
            int m1 = int.Parse("-1", CultureInfo.InvariantCulture);
            int lr = int.Log2(m1);
            Console.WriteLine(lr);
        }
        catch (ArgumentOutOfRangeException)
        {
            Console.WriteLine("int.Log2(-1): ArgumentOutOfRangeException");
        }
        try
        {
            long lmin = long.Parse("-9223372036854775808", CultureInfo.InvariantCulture);
            long lr = long.Log2(lmin);
            Console.WriteLine(lr);
        }
        catch (ArgumentOutOfRangeException)
        {
            Console.WriteLine("long.Log2(min): ArgumentOutOfRangeException");
        }
    }
}
