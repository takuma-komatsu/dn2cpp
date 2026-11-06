using System;
using System.Globalization;
using System.Numerics;

namespace UInt128CheckedSubset;
internal static class Program
{
    private static UInt128 Direct<T>(T value) where T : INumberBase<T> => UInt128.CreateChecked(value);
    private static TSelf ViaConstraint<TSelf, T>(T value) where TSelf : INumberBase<TSelf> where T : INumberBase<T> => TSelf.CreateChecked(value);
    private static UInt128 Constrained<T>(T value) where T : INumberBase<T> => ViaConstraint<UInt128, T>(value);
    private static unsafe UInt128 Pointer<T>(T value) where T : INumberBase<T>
    {
        delegate*<T, UInt128> pointer = &UInt128.CreateChecked<T>;
        return pointer(value);
    }
    private static Func<T, UInt128> UInt128Address<T>() where T : INumberBase<T> => UInt128.CreateChecked<T>;
    private static Func<T, UInt128> UInt128Selected<T>() where T : INumberBase<T> =>
#if UINT128_CONSTRAINED
        Constrained<T>;
#elif UINT128_ADDRESS
        UInt128Address<T>();
#elif UINT128_CALLI
        Pointer<T>;
#else
        Direct<T>;
#endif
    private static Func<T, UInt128> UInt128Route<T>(int route) where T : INumberBase<T> => route switch
    {
        0 => Direct<T>, 1 => Constrained<T>, 2 => UInt128Address<T>(), 3 => Pointer<T>,
        _ => throw new InvalidOperationException("route")
    };
    private static void Check(string label, bool ok)
    {
        if (!ok) throw new InvalidOperationException(label);
        Console.WriteLine("uint128-checked-" + label + ":True");
    }
    private static void Words(string label, UInt128 value, ulong low, ulong high) =>
        Check(label, unchecked((ulong)value) == low && unchecked((ulong)(value >> 64)) == high);
    private static void Cases<T>(string label, T low, T high, ulong lowBits, ulong lowHigh, ulong highBits, ulong highHigh,
        Func<T, UInt128> convert) where T : INumberBase<T>
    {
        Words(label + "-min", convert(low), lowBits, lowHigh);
        Words(label + "-max", convert(high), highBits, highHigh);
        Words(label + "-zero", convert(default(T)), 0, 0);
    }
    private static void Signed<T>(string label, T min, T negative, T one, T max, ulong maxBits, Func<T, UInt128> convert) where T : INumberBase<T>
    {
        Words(label + "-one", convert(one), 1, 0);
        foreach (T value in new[] { min, negative })
        {
            bool overflow = false;
            try { _ = convert(value); } catch (OverflowException) { overflow = true; }
            Check(label + "-negative", overflow);
        }
        Words(label + "-zero", convert(default(T)), 0, 0);
        Words(label + "-max", convert(max), maxBits, 0);
    }
    private static unsafe void Exercise(int route, bool selected)
    {
#if UINT128_ISOLATED
        Func<T, UInt128> Pick<T>() where T : INumberBase<T> => UInt128Selected<T>();
#else
        Func<T, UInt128> Pick<T>() where T : INumberBase<T> => selected ? UInt128Selected<T>() : UInt128Route<T>(route);
#endif
        string p = selected ? "selected" : "route" + route;
        Signed(p + "-sbyte", sbyte.MinValue, (sbyte)(-1), (sbyte)1, sbyte.MaxValue, 127, Pick<sbyte>());
        Cases(p + "-byte", byte.MinValue, byte.MaxValue, 0, 0, 255, 0, Pick<byte>());
        Signed(p + "-short", short.MinValue, (short)(-1), (short)1, short.MaxValue, 32767, Pick<short>());
        Cases(p + "-ushort", ushort.MinValue, ushort.MaxValue, 0, 0, 65535, 0, Pick<ushort>());
        Signed(p + "-int", int.MinValue, (int)(-1), (int)1, int.MaxValue, int.MaxValue, Pick<int>());
        Cases(p + "-uint", uint.MinValue, uint.MaxValue, 0, 0, uint.MaxValue, 0, Pick<uint>());
        Signed(p + "-long", long.MinValue, (long)(-1), (long)1, long.MaxValue, long.MaxValue, Pick<long>());
        Cases(p + "-ulong", ulong.MinValue, ulong.MaxValue, 0, 0, ulong.MaxValue, 0, Pick<ulong>());
        Cases(p + "-char", char.MinValue, char.MaxValue, 0, 0, 65535, 0, Pick<char>());
        Words(p + "-byte-one", Pick<byte>()(1), 1, 0);
        Words(p + "-ushort-one", Pick<ushort>()(1), 1, 0);
        Words(p + "-uint-one", Pick<uint>()(1), 1, 0);
        Words(p + "-ulong-one", Pick<ulong>()(1), 1, 0);
        Words(p + "-char-one", Pick<char>()((char)1), 1, 0);
        Words(p + "-nuint-one", Pick<nuint>()((nuint)1), 1, 0);
        Words(p + "-byte-highbit", Pick<byte>()(0x80), 0x80, 0);
        Words(p + "-ushort-highbit", Pick<ushort>()(0x8000), 0x8000, 0);
        Words(p + "-uint-highbit", Pick<uint>()(0x80000000), 0x80000000, 0);
        Words(p + "-ulong-highbit", Pick<ulong>()(0x8000000000000000), 0x8000000000000000, 0);
        int nativeBytes = sizeof(nint);
        Check(p + "-target-native-width", nativeBytes == IntPtr.Size && nativeBytes == sizeof(nuint));
        ulong nmin = nativeBytes == 8 ? 0x8000000000000000 : 0xffffffff80000000;
        ulong nmax = nativeBytes == 8 ? 0x7fffffffffffffffUL : 0x7fffffffUL;
        ulong umax = nativeBytes == 8 ? ulong.MaxValue : uint.MaxValue;
        // Bounds follow the target pointer width, not the input CoreLib's architecture.
        nint nativeMin = unchecked((nint)nmin), nativeMax = (nint)nmax;
        nuint nativeUnsignedMax = unchecked((nuint)umax);
        Check(p + "-native-inputs", (long)nativeMin == (nativeBytes == 4 ? int.MinValue : long.MinValue)
            && (long)nativeMax == (nativeBytes == 4 ? int.MaxValue : long.MaxValue)
            && (nativeBytes == 4 ? (uint)nativeUnsignedMax == uint.MaxValue : (ulong)nativeUnsignedMax == ulong.MaxValue));
        Signed(p + "-nint", nativeMin, (nint)(-1), (nint)1, nativeMax, nmax, Pick<nint>());
        Cases(p + "-nuint", (nuint)0, nativeUnsignedMax, 0, 0, umax, 0, Pick<nuint>());
        ulong nativeHigh = nativeBytes == 4 ? 0x80000000UL : 0x8000000000000000UL;
        Words(p + "-nuint-highbit", Pick<nuint>()(unchecked((nuint)nativeHigh)), nativeHigh, 0);
        var identity = Pick<UInt128>();
        Words(p + "-identity-min", identity(UInt128.MinValue), 0, 0);
        Words(p + "-identity-max", identity(UInt128.MaxValue), ulong.MaxValue, ulong.MaxValue);
        Words(p + "-identity-highbit", identity((UInt128)1 << 127), 0, 0x8000000000000000);
        UInt128 mixed = ((UInt128)0x0123456789abcdefUL << 64) | 0xfedcba9876543210UL;
        Words(p + "-identity-mixed", identity(mixed), 0xfedcba9876543210, 0x0123456789abcdef);
        Producers(p, Pick<long>());
    }
    private static int calls;
    private static long Produce(bool throws)
    {
        calls++;
        if (throws) throw new FormatException("checked producer");
        return 17;
    }
    private static void Producers(string prefix, Func<long, UInt128> convert)
    {
        calls = 0;
        UInt128 value = convert(Produce(false));
        Check(prefix + "-producer-once", calls == 1 && value == (UInt128)17);
        calls = 0; bool overflow = false;
        try { _ = convert(-Produce(false)); } catch (OverflowException) { overflow = true; }
        Check(prefix + "-negative-once", calls == 1 && overflow);
        Words(prefix + "-after-overflow", convert(1), 1, 0);
        calls = 0; bool threw = false;
        try { _ = convert(Produce(true)); }
        catch (FormatException error) { threw = error.Message == "checked producer"; }
        Check(prefix + "-producer-throw", calls == 1 && threw);
    }
    private static class Ordinary<T>
    {
        internal static U Keep<U>(U value) => value;
    }
    private static void Neighbors()
    {
        Int128 signed = Int128.CreateChecked(long.MinValue);
        Check("signed-checked-extension", unchecked((ulong)signed) == 0x8000000000000000
            && unchecked((ulong)(signed >> 64)) == ulong.MaxValue);
        Check("signed-checked-identity", Int128.CreateChecked(Int128.MinValue) == Int128.MinValue);
        Check("ordinary-generic-signatures", Ordinary<int>.Keep<long>(37) == 37
            && Ordinary<string>.Keep<char>('X') == 'X');
        Check("truncating-uint128", UInt128.CreateTruncating(-1L) == UInt128.MaxValue);
        Check("truncating-int128", Int128.CreateTruncating(-1L) == (Int128)(-1));
        bool overflow = false;
        try { long source = long.MaxValue; _ = checked((int)source); }
        catch (OverflowException) { overflow = true; }
        Check("primitive-overflow", overflow);
    }
    public static void RunSelected()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Exercise(0, true);
    }
    public static void __GateEntry()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("== UInt128 checked widening ==");
        for (int route = 0; route < 4; route++) Exercise(route, false);
        Neighbors();
        Console.WriteLine("UInt128 checked widening end");
    }
}
