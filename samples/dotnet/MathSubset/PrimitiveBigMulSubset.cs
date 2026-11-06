using System;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Dn2Cpp;

public static unsafe class PrimitiveBigMulSubset
{
    delegate long Signed32(int left, int right);
    delegate ulong Unsigned32(uint left, uint right);
    delegate Int128 Signed64(long left, long right);
    delegate UInt128 Unsigned64(ulong left, ulong right);
    delegate nint SignedNative(nint left, nint right, out nint lower);
    delegate nuint UnsignedNative(nuint left, nuint right, out nuint lower);
    static int evaluations;
    static void Check(bool value, string label)
    {
        if (!value) throw new Exception("primitive BigMul: " + label);
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    static nint Signed(nint value) { evaluations++; return value; }
    [MethodImpl(MethodImplOptions.NoInlining)]
    static nuint Unsigned(nuint value) { evaluations++; return value; }

    public static void Run()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("== primitive BigMul ==");
        RunDirect();
        RunAddress();
        Console.WriteLine("primitive BigMul end");
    }

    public static void RunDirect()
    {
        Check(int.BigMul(0, int.MinValue) == 0 && int.BigMul(-3, 7) == -21, "int zero/sign");
        Check(int.BigMul(int.MinValue, int.MinValue) == 4611686018427387904L, "int min");
        Check(int.BigMul(int.MaxValue, int.MinValue) == -4611686016279904256L, "int mixed");
        Check(checked(int.BigMul(int.MaxValue, int.MaxValue)) == 4611686014132420609L, "int checked");
        Check(uint.BigMul(uint.MaxValue, uint.MaxValue) == 18446744065119617025UL, "uint max");
        Check(uint.BigMul(0x80000000, 2) == 4294967296UL && uint.BigMul(uint.MaxValue, 0) == 0
            && uint.BigMul(uint.MaxValue, 1) == uint.MaxValue, "uint high/zero/one");
        SignedWide(long.BigMul(long.MinValue, long.MinValue), 0x4000000000000000, 0);
        SignedWide(long.BigMul(long.MinValue, -1), 0, 0x8000000000000000);
        SignedWide(checked(long.BigMul(long.MaxValue, -2)), ulong.MaxValue, 2);
        SignedWide(long.BigMul(-3, 7), ulong.MaxValue, unchecked((ulong)-21L));
        SignedWide(long.BigMul(0, long.MinValue), 0, 0);
        UnsignedWide(ulong.BigMul(ulong.MaxValue, ulong.MaxValue), ulong.MaxValue - 1, 1);
        UnsignedWide(checked(ulong.BigMul(1UL << 63, 4)), 2, 0);
        UnsignedWide(ulong.BigMul(0x100000001UL, 0xffffffffUL), 0, ulong.MaxValue);
        UnsignedWide(ulong.BigMul(ulong.MaxValue, 1), 0, ulong.MaxValue);
        UnsignedWide(ulong.BigMul(ulong.MaxValue, 0), 0, 0);
        Console.WriteLine("primitive fixed direct=True");
        NativeDirect();
    }

    static void SignedWide(Int128 value, ulong upper, ulong lower)
    {
        Check(unchecked((ulong)(value >> 64)) == upper && unchecked((ulong)value) == lower, "signed wide halves");
    }
    static void UnsignedWide(UInt128 value, ulong upper, ulong lower)
    {
        Check((ulong)(value >> 64) == upper && unchecked((ulong)value) == lower, "unsigned wide halves");
    }

    static void NativeDirect()
    {
        nint min = unchecked((nint)((nuint)1 << (IntPtr.Size * 8 - 1)));
        nint max = ~min;
        nint low;
        Check(nint.BigMul(min, min, out low) == (nint)((nuint)1 << (IntPtr.Size * 8 - 2)) && low == 0, "native min");
        Check(nint.BigMul(min, -1, out low) == 0 && low == min, "native negation");
        Check(checked(nint.BigMul(max, -2, out low)) == -1 && low == 2, "native max negative");
        Check(nint.BigMul(-3, 7, out low) == -1 && low == -21, "native sign");
        Check(nint.BigMul(0, min, out low) == 0 && low == 0, "native zero");
        nuint umax = unchecked((nuint)(nint)(-1)), ulo;
        Check(nuint.BigMul(umax, umax, out ulo) == umax - 1 && ulo == 1, "unative max");
        Check(nuint.BigMul((nuint)min, 2, out ulo) == 1 && ulo == 0, "unative high bit");
        Check(checked(nuint.BigMul(unchecked((nuint)min), 4, out ulo)) == 2 && ulo == 0, "unative checked");
        Check(nuint.BigMul(umax, 1, out ulo) == 0 && ulo == umax, "unative one");
        Check(nuint.BigMul(umax, 0, out ulo) == 0 && ulo == 0, "unative zero");
        nint* slots = stackalloc nint[3]; slots[0] = 1234567; slots[2] = -7654321;
        Check(nint.BigMul(min, min, out slots[1]) == (nint)((nuint)1 << (IntPtr.Size * 8 - 2))
            && slots[1] == 0 && slots[0] == 1234567 && slots[2] == -7654321, "signed canary");
        Check(nuint.BigMul(umax, umax, out ((nuint*)slots)[1]) == umax - 1 && slots[1] == 1
            && slots[0] == 1234567 && slots[2] == -7654321, "unsigned canary");
        low = max;
        Check(nint.BigMul(low, -2, out low) == -1 && low == 2, "signed alias");
        ulo = umax;
        Check(nuint.BigMul(ulo, ulo, out ulo) == umax - 1 && ulo == 1, "unsigned alias");
        evaluations = 0;
        Check(nint.BigMul(Signed(-3), Signed(7), out low) == -1 && low == -21 && evaluations == 2, "signed evaluation");
        evaluations = 0;
        Check(nuint.BigMul(Unsigned(umax), Unsigned(umax), out ulo) == umax - 1 && ulo == 1 && evaluations == 2, "unsigned evaluation");
        Console.WriteLine("primitive native direct/canary/alias/evaluation=True");
    }

    public static void RunReflection()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        var signed = typeof(nint).GetMethod("BigMul", new[] { typeof(nint), typeof(nint), typeof(nint).MakeByRefType() });
        object[] arguments = { (nint)(-3), (nint)7, (nint)0 };
        Check((nint)signed.Invoke(null, arguments) == -1 && (nint)arguments[2] == -21, "native reflection");
        var unsigned = typeof(nuint).GetMethod("BigMul", new[] { typeof(nuint), typeof(nuint), typeof(nuint).MakeByRefType() });
        nuint max = unchecked((nuint)(nint)(-1));
        object[] values = { max, max, (nuint)0 };
        Check((nuint)unsigned.Invoke(null, values) == max - 1 && (nuint)values[2] == 1, "unative reflection");
        Console.WriteLine("primitive native reflection=True");
    }

    // This entrypoint has no direct BigMul calls; the fixture can root it alone.
    public static void RunAddress()
    {
        delegate* managed<int, int, long> i4 = &int.BigMul;
        delegate* managed<uint, uint, ulong> u4 = &uint.BigMul;
        delegate* managed<long, long, Int128> i8 = &long.BigMul;
        delegate* managed<ulong, ulong, UInt128> u8 = &ulong.BigMul;
        delegate* managed<nint, nint, out nint, nint> ni = &nint.BigMul;
        delegate* managed<nuint, nuint, out nuint, nuint> nu = &nuint.BigMul;
        Check(i4(int.MinValue, int.MinValue) == 4611686018427387904L, "address int");
        Check(u4(uint.MaxValue, uint.MaxValue) == 18446744065119617025UL, "address uint");
        SignedWide(i8(long.MaxValue, -2), ulong.MaxValue, 2);
        UnsignedWide(u8(ulong.MaxValue, ulong.MaxValue), ulong.MaxValue - 1, 1);
        nint* slots = stackalloc nint[3]; slots[0] = 1234567; slots[2] = -7654321;
        nint min = unchecked((nint)((nuint)1 << (IntPtr.Size * 8 - 1)));
        Check(ni(min, min, out slots[1]) == (nint)((nuint)1 << (IntPtr.Size * 8 - 2)) && slots[1] == 0, "address native");
        nuint max = unchecked((nuint)(nint)(-1));
        Check(nu(max, max, out ((nuint*)slots)[1]) == max - 1 && slots[1] == 1, "address unative");
        Check(slots[0] == 1234567 && slots[2] == -7654321, "address canaries");
        Console.WriteLine("primitive all-six address=True");
        RunDelegate();
    }
    static void RunDelegate()
    {
        Signed32 i4 = int.BigMul; Unsigned32 u4 = uint.BigMul;
        Signed64 i8 = long.BigMul; Unsigned64 u8 = ulong.BigMul;
        SignedNative ni = nint.BigMul; UnsignedNative nu = nuint.BigMul;
        Check(i4(-3, 7) == -21 && u4(uint.MaxValue, uint.MaxValue) == 18446744065119617025UL, "delegate fixed32");
        SignedWide(i8(long.MinValue, -1), 0, 0x8000000000000000);
        UnsignedWide(u8(ulong.MaxValue, ulong.MaxValue), ulong.MaxValue - 1, 1);
        Check(ni(-3, 7, out nint low) == -1 && low == -21, "delegate native");
        nuint max = unchecked((nuint)(nint)(-1));
        Check(nu(max, max, out nuint lower) == max - 1 && lower == 1, "delegate unative");
        Console.WriteLine("primitive all-six delegate=True");
    }

}
