using System;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace GenericMathWideConversions;

// Deriving from INumberBase makes its protected source slots legal C# calls.
internal interface IWideSource : INumberBase<byte>
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Checked<TFrom, TTo>(TFrom value, out TTo result)
        where TFrom : INumberBase<TFrom> where TTo : INumberBase<TTo>
        => TFrom.TryConvertToChecked(value, out result);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Truncating<TFrom, TTo>(TFrom value, out TTo result)
        where TFrom : INumberBase<TFrom> where TTo : INumberBase<TTo>
        => TFrom.TryConvertToTruncating(value, out result);
}

internal struct WideTriple<T>
{
    internal T Before;
    internal T Value;
    internal T After;
}

internal static class WideConversions
{
    private static int _checks;
    private static string _order = "";
    private static int _throwAt;
    private static Int128 _ordered;
    private sealed class FactoryFailure : Exception { }

    private static void Check(string label, bool condition)
    {
        if (!condition) throw new InvalidOperationException("wide." + label);
        _checks++;
        Console.WriteLine("wide." + label + "=ok");
    }

    private static bool Words<T>(T value, ulong low, ulong high)
    {
        if (typeof(T) == typeof(Int128))
        {
            Int128 actual = (Int128)(object)value;
            return unchecked((ulong)actual) == low && unchecked((ulong)(actual >> 64)) == high;
        }
        UInt128 unsigned = (UInt128)(object)value;
        return unchecked((ulong)unsigned) == low && unchecked((ulong)(unsigned >> 64)) == high;
    }

    private static void BigCase(string label, long value, string expected)
    {
        BigInteger result = new BigInteger(4294967296L);
        bool ok = IWideSource.Checked<long, BigInteger>(value, out result);
        Check("big." + label + ".source", !ok && result.ToString(CultureInfo.InvariantCulture) == "0");
        Check("big." + label + ".create", BigInteger.CreateChecked(value).ToString(CultureInfo.InvariantCulture) == expected);
    }

    private static void TruncCase<TFrom, TTo>(string label, TFrom value, TTo seed, bool sourceOk, ulong low, ulong high)
        where TFrom : INumberBase<TFrom> where TTo : INumberBase<TTo>
    {
        TTo result = seed;
        bool ok = IWideSource.Truncating<TFrom, TTo>(value, out result);
        Check(label + ".source", ok == sourceOk && Words(result, sourceOk ? low : 0, sourceOk ? high : 0));
        Check(label + ".create", Words(TTo.CreateTruncating(value), low, high));
    }

    private static void Storage<TFrom, TTo>(string label, TFrom value, TTo before, TTo after,
        bool sourceOk, ulong low, ulong high)
        where TFrom : INumberBase<TFrom> where TTo : INumberBase<TTo>
    {
        TTo[] array = new TTo[] { before, after, after };
        bool ok = IWideSource.Truncating<TFrom, TTo>(value, out array[1]);
        Check(label + ".array", ok == sourceOk && Words(array[1], low, high)
            && Words(array[0], 0x1122334455667788UL, 0x8877665544332211UL)
            && Words(array[2], 0x1020304050607080UL, 0x8070605040302010UL));
        var fields = new WideTriple<TTo> { Before = before, Value = after, After = after };
        ok = IWideSource.Truncating<TFrom, TTo>(value, out fields.Value);
        Check(label + ".field", ok == sourceOk && Words(fields.Value, low, high)
            && Words(fields.Before, 0x1122334455667788UL, 0x8877665544332211UL)
            && Words(fields.After, 0x1020304050607080UL, 0x8070605040302010UL));
        fields.Value = after;
        ref TTo alias = ref fields.Value;
        ok = IWideSource.Truncating<TFrom, TTo>(value, out alias);
        Check(label + ".alias", ok == sourceOk && Words(alias, low, high) && Words(fields.Value, low, high)
            && Words(fields.Before, 0x1122334455667788UL, 0x8877665544332211UL)
            && Words(fields.After, 0x1020304050607080UL, 0x8070605040302010UL));
    }

    private static void BigStorage()
    {
        BigInteger before = new BigInteger(-4294967296L), after = new BigInteger(4294967296L);
        BigInteger[] array = new BigInteger[] { before, after, after };
        bool ok = IWideSource.Checked<long, BigInteger>(long.MinValue, out array[1]);
        Check("big.array", !ok && array[0].ToString() == "-4294967296" && array[1].ToString() == "0" && array[2].ToString() == "4294967296");
        var fields = new WideTriple<BigInteger> { Before = before, Value = after, After = after };
        ok = IWideSource.Checked<long, BigInteger>(long.MaxValue, out fields.Value);
        Check("big.field", !ok && fields.Before.ToString() == "-4294967296" && fields.Value.ToString() == "0" && fields.After.ToString() == "4294967296");
        fields.Value = after;
        ref BigInteger alias = ref fields.Value;
        ok = IWideSource.Checked<long, BigInteger>(-1, out alias);
        Check("big.alias", !ok && alias.ToString() == "0" && fields.Value.ToString() == "0"
            && fields.Before.ToString() == "-4294967296" && fields.After.ToString() == "4294967296");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static uint ValueUInt()
    {
        _order += "V";
        if (_throwAt == 1) throw new FactoryFailure();
        return uint.MaxValue;
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int ValueInt() { _order += "V"; return int.MinValue; }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ref Int128 Output()
    {
        _order += "O";
        if (_throwAt == 2) throw new FactoryFailure();
        _ordered = new Int128(9, 7);
        return ref _ordered;
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int BadIndex() { _order += "I"; return 2; }

    private static void Ordering()
    {
        _order = ""; _throwAt = 0;
        bool ok = IWideSource.Truncating<uint, Int128>(ValueUInt(), out Output());
        Check("order.true", ok && _order == "VO" && Words(_ordered, 0xffffffffUL, 0));
        _order = "";
        ok = IWideSource.Truncating<int, Int128>(ValueInt(), out Output());
        Check("order.false", !ok && _order == "VO" && Words(_ordered, 0, 0));
        _ordered = new Int128(34, 12);
        _order = "";
        bool caught = false;
        Int128[] missing = null;
        try { IWideSource.Truncating<uint, Int128>(ValueUInt(), out missing[0]); }
        catch (NullReferenceException) { caught = true; }
        Check("guard.null", caught && _order == "V" && Words(_ordered, 12, 34));
        var array = new Int128[] { new Int128(34, 12), new Int128(78, 56) };
        _order = ""; caught = false;
        try { IWideSource.Truncating<uint, Int128>(ValueUInt(), out array[BadIndex()]); }
        catch (IndexOutOfRangeException) { caught = true; }
        Check("guard.index", caught && _order == "VI" && Words(array[0], 12, 34) && Words(array[1], 56, 78));
        for (int stage = 1; stage <= 2; stage++)
        {
            _throwAt = stage; _order = ""; _ordered = new Int128(34, 12); caught = false;
            try { IWideSource.Truncating<uint, Int128>(ValueUInt(), out Output()); }
            catch (FactoryFailure) { caught = true; }
            Check("guard.factory" + stage, caught && _order == (stage == 1 ? "V" : "VO") && Words(_ordered, 12, 34));
        }
        _throwAt = 0;
    }

    internal static void __GateEntry()
    {
        Console.WriteLine("== Finite primitive source conversions to wide values ==");
        Check("width64", IntPtr.Size == 8);
        BigCase("v0", long.MinValue, "-9223372036854775808");
        BigCase("v1", -4294967297L, "-4294967297");
        BigCase("v2", -4294967296L, "-4294967296");
        BigCase("v3", -2147483649L, "-2147483649");
        BigCase("v4", -2147483648L, "-2147483648");
        BigCase("v5", -1L, "-1");
        BigCase("v6", 0L, "0");
        BigCase("v7", 1L, "1");
        BigCase("v8", 2147483647L, "2147483647");
        BigCase("v9", 2147483648L, "2147483648");
        BigCase("v10", 4294967295L, "4294967295");
        BigCase("v11", 4294967296L, "4294967296");
        BigCase("v12", long.MaxValue, "9223372036854775807");
        Int128 iSeed = new Int128(0x8877665544332211UL, 0x1122334455667788UL);
        UInt128 uSeed = new UInt128(0x8877665544332211UL, 0x1122334455667788UL);
        TruncCase<int, Int128>("int.Int128.v0", int.MinValue, iSeed, false, 0xffffffff80000000UL, 0xffffffffffffffffUL);
        TruncCase<int, Int128>("int.Int128.v1", -2147483647, iSeed, false, 0xffffffff80000001UL, 0xffffffffffffffffUL);
        TruncCase<int, Int128>("int.Int128.v2", -65536, iSeed, false, 0xffffffffffff0000UL, 0xffffffffffffffffUL);
        TruncCase<int, Int128>("int.Int128.v3", -1, iSeed, false, 0xffffffffffffffffUL, 0xffffffffffffffffUL);
        TruncCase<int, Int128>("int.Int128.v4", 0, iSeed, false, 0x0000000000000000UL, 0x0000000000000000UL);
        TruncCase<int, Int128>("int.Int128.v5", 1, iSeed, false, 0x0000000000000001UL, 0x0000000000000000UL);
        TruncCase<int, Int128>("int.Int128.v6", 65535, iSeed, false, 0x000000000000ffffUL, 0x0000000000000000UL);
        TruncCase<int, Int128>("int.Int128.v7", 65536, iSeed, false, 0x0000000000010000UL, 0x0000000000000000UL);
        TruncCase<int, Int128>("int.Int128.v8", int.MaxValue, iSeed, false, 0x000000007fffffffUL, 0x0000000000000000UL);
        TruncCase<uint, Int128>("uint.Int128.v0", 0x00000000u, iSeed, true, 0x0000000000000000UL, 0x0000000000000000UL);
        TruncCase<uint, Int128>("uint.Int128.v1", 0x00000001u, iSeed, true, 0x0000000000000001UL, 0x0000000000000000UL);
        TruncCase<uint, Int128>("uint.Int128.v2", 0x7fffffffu, iSeed, true, 0x000000007fffffffUL, 0x0000000000000000UL);
        TruncCase<uint, Int128>("uint.Int128.v3", 0x80000000u, iSeed, true, 0x0000000080000000UL, 0x0000000000000000UL);
        TruncCase<uint, Int128>("uint.Int128.v4", 0xffffffffu, iSeed, true, 0x00000000ffffffffUL, 0x0000000000000000UL);
        TruncCase<int, UInt128>("int.UInt128.v0", int.MinValue, uSeed, true, 0xffffffff80000000UL, 0xffffffffffffffffUL);
        TruncCase<int, UInt128>("int.UInt128.v1", -2147483647, uSeed, true, 0xffffffff80000001UL, 0xffffffffffffffffUL);
        TruncCase<int, UInt128>("int.UInt128.v2", -65536, uSeed, true, 0xffffffffffff0000UL, 0xffffffffffffffffUL);
        TruncCase<int, UInt128>("int.UInt128.v3", -1, uSeed, true, 0xffffffffffffffffUL, 0xffffffffffffffffUL);
        TruncCase<int, UInt128>("int.UInt128.v4", 0, uSeed, true, 0x0000000000000000UL, 0x0000000000000000UL);
        TruncCase<int, UInt128>("int.UInt128.v5", 1, uSeed, true, 0x0000000000000001UL, 0x0000000000000000UL);
        TruncCase<int, UInt128>("int.UInt128.v6", 65535, uSeed, true, 0x000000000000ffffUL, 0x0000000000000000UL);
        TruncCase<int, UInt128>("int.UInt128.v7", 65536, uSeed, true, 0x0000000000010000UL, 0x0000000000000000UL);
        TruncCase<int, UInt128>("int.UInt128.v8", int.MaxValue, uSeed, true, 0x000000007fffffffUL, 0x0000000000000000UL);
        TruncCase<uint, UInt128>("uint.UInt128.v0", 0x00000000u, uSeed, false, 0x0000000000000000UL, 0x0000000000000000UL);
        TruncCase<uint, UInt128>("uint.UInt128.v1", 0x00000001u, uSeed, false, 0x0000000000000001UL, 0x0000000000000000UL);
        TruncCase<uint, UInt128>("uint.UInt128.v2", 0x7fffffffu, uSeed, false, 0x000000007fffffffUL, 0x0000000000000000UL);
        TruncCase<uint, UInt128>("uint.UInt128.v3", 0x80000000u, uSeed, false, 0x0000000080000000UL, 0x0000000000000000UL);
        TruncCase<uint, UInt128>("uint.UInt128.v4", 0xffffffffu, uSeed, false, 0x00000000ffffffffUL, 0x0000000000000000UL);
        Int128 iAfter = new Int128(0x8070605040302010UL, 0x1020304050607080UL);
        UInt128 uAfter = new UInt128(0x8070605040302010UL, 0x1020304050607080UL);
        Storage<int, Int128>("int.Int128", int.MinValue, iSeed, iAfter, false, 0, 0);
        Storage<uint, Int128>("uint.Int128", uint.MaxValue, iSeed, iAfter, true, 0xffffffffUL, 0);
        Storage<int, UInt128>("int.UInt128", int.MinValue, uSeed, uAfter, true, 0xffffffff80000000UL, 0xffffffffffffffffUL);
        Storage<uint, UInt128>("uint.UInt128", uint.MaxValue, uSeed, uAfter, false, 0, 0);
        BigStorage();
        Ordering();
        if (_checks != 104) throw new InvalidOperationException("wide.check-count");
        Console.WriteLine("wide.checks=" + _checks);
        Console.WriteLine("wide.done");
    }
}
