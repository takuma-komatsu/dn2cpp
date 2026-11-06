using System;
using System.Globalization;
using System.Numerics;

namespace Int128CheckedSubset;
internal static class Program
{
    private static Int128 Direct<T>(T value) where T : INumberBase<T> => Int128.CreateChecked(value);
    private static TSelf ViaConstraint<TSelf, T>(T value) where TSelf : INumberBase<TSelf> where T : INumberBase<T> => TSelf.CreateChecked(value);
    private static Int128 Constrained<T>(T value) where T : INumberBase<T> => ViaConstraint<Int128, T>(value);
    private static unsafe Int128 Pointer<T>(T value) where T : INumberBase<T>
    {
        delegate*<T, Int128> pointer = &Int128.CreateChecked<T>;
        return pointer(value);
    }
    private static Func<T, Int128> Address<T>() where T : INumberBase<T> => Int128.CreateChecked<T>;
    private static Func<T, Int128> Selected<T>() where T : INumberBase<T> =>
#if INT128_CONSTRAINED
        Constrained<T>;
#elif INT128_ADDRESS
        Address<T>();
#elif INT128_CALLI
        Pointer<T>;
#else
        Direct<T>;
#endif
    private static Func<T, Int128> Route<T>(int route) where T : INumberBase<T> => route switch
    {
        0 => Direct<T>, 1 => Constrained<T>, 2 => Address<T>(), 3 => Pointer<T>,
        _ => throw new InvalidOperationException("route")
    };
    private static void Check(string label, bool ok)
    {
        if (!ok) throw new InvalidOperationException(label);
        Console.WriteLine("int128-checked-" + label + ":True");
    }
    private static void Words(string label, Int128 value, ulong low, ulong high) =>
        Check(label, unchecked((ulong)value) == low && unchecked((ulong)(value >> 64)) == high);
    private static void Cases<T>(string label, T low, T high, ulong lowBits, ulong lowHigh, ulong highBits, ulong highHigh,
        Func<T, Int128> convert) where T : INumberBase<T>
    {
        Words(label + "-min", convert(low), lowBits, lowHigh);
        Words(label + "-max", convert(high), highBits, highHigh);
        Words(label + "-zero", convert(default(T)), 0, 0);
    }
    private static unsafe void Exercise(int route, bool selected)
    {
#if INT128_ISOLATED
        Func<T, Int128> Pick<T>() where T : INumberBase<T> => Selected<T>();
#else
        Func<T, Int128> Pick<T>() where T : INumberBase<T> => selected ? Selected<T>() : Route<T>(route);
#endif
        string p = selected ? "selected" : "route" + route;
        Cases(p + "-sbyte", sbyte.MinValue, sbyte.MaxValue, unchecked((ulong)-128L), ulong.MaxValue, 127, 0, Pick<sbyte>());
        Cases(p + "-byte", byte.MinValue, byte.MaxValue, 0, 0, 255, 0, Pick<byte>());
        Cases(p + "-short", short.MinValue, short.MaxValue, unchecked((ulong)-32768L), ulong.MaxValue, 32767, 0, Pick<short>());
        Cases(p + "-ushort", ushort.MinValue, ushort.MaxValue, 0, 0, 65535, 0, Pick<ushort>());
        Cases(p + "-int", int.MinValue, int.MaxValue, unchecked((ulong)int.MinValue), ulong.MaxValue, int.MaxValue, 0, Pick<int>());
        Cases(p + "-uint", uint.MinValue, uint.MaxValue, 0, 0, uint.MaxValue, 0, Pick<uint>());
        Cases(p + "-long", long.MinValue, long.MaxValue, 0x8000000000000000, ulong.MaxValue, long.MaxValue, 0, Pick<long>());
        Cases(p + "-ulong", ulong.MinValue, ulong.MaxValue, 0, 0, ulong.MaxValue, 0, Pick<ulong>());
        Cases(p + "-char", char.MinValue, char.MaxValue, 0, 0, 65535, 0, Pick<char>());
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
        Cases(p + "-nint", nativeMin, nativeMax, nmin, ulong.MaxValue, nmax, 0, Pick<nint>());
        Cases(p + "-nuint", (nuint)0, nativeUnsignedMax, 0, 0, umax, 0, Pick<nuint>());
        ulong nativeHigh = nativeBytes == 4 ? 0x80000000UL : 0x8000000000000000UL;
        Words(p + "-nuint-highbit", Pick<nuint>()(unchecked((nuint)nativeHigh)), nativeHigh, 0);
        var identity = Pick<Int128>();
        Words(p + "-identity-min", identity(Int128.MinValue), 0, 0x8000000000000000);
        Words(p + "-identity-max", identity(Int128.MaxValue), ulong.MaxValue, 0x7fffffffffffffff);
        Int128 mixed = ((Int128)0x0123456789abcdefUL << 64) | 0xfedcba9876543210UL;
        Words(p + "-identity-mixed", identity(mixed), 0xfedcba9876543210, 0x0123456789abcdef);
        Producers(p, Pick<long>());
    }
    private static int calls;
    private static long Produce(bool throws)
    {
        calls++;
        if (throws) throw new FormatException("checked producer");
        return long.MinValue;
    }
    private static void Producers(string prefix, Func<long, Int128> convert)
    {
        calls = 0;
        Int128 value = convert(Produce(false));
        Check(prefix + "-producer-once", calls == 1 && value == (Int128)long.MinValue);
        calls = 0; bool threw = false;
        try { _ = convert(Produce(true)); }
        catch (FormatException error) { threw = error.Message == "checked producer"; }
        Check(prefix + "-producer-throw", calls == 1 && threw);
    }
    private static void Neighbors()
    {
        Check("truncating-int128", Int128.CreateTruncating(-1L) == (Int128)(-1));
        Check("truncating-uint128", UInt128.CreateTruncating(-1L) == UInt128.MaxValue);
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
        Console.WriteLine("== Int128 checked widening ==");
        for (int route = 0; route < 4; route++) Exercise(route, false);
        Neighbors();
        Console.WriteLine("Int128 checked widening end");
    }
}
