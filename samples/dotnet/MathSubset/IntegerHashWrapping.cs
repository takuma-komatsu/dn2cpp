using System;
using System.Runtime.CompilerServices;

namespace IntegerHashWrapping;

internal static class Program
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int ReadChar(string text, int index) => text[index] | 0x10000;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static uint CharHash(string text)
    {
        uint hash = 2166136261u;
        for (int i = 0; i < text.Length; i++)
        {
            // Keep the char store: its range must not make wrapping multiply undefined.
            char value = unchecked((char)ReadChar(text, i));
            hash = unchecked((hash ^ value) * 16777619u);
        }
        return hash;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static uint ByteHash(byte[] data)
    {
        uint hash = 2166136261u;
        foreach (byte value in data)
            hash = unchecked((hash ^ value) * 16777619u);
        return hash;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int MulI4(int a, int b) => unchecked(a * b);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static uint MulU4(uint a, uint b) => unchecked(a * b);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long MulI8(long a, long b) => unchecked(a * b);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong MulU8(ulong a, ulong b) => unchecked(a * b);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static nint MulNative(nint a, int b) => unchecked(a * b);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static nuint MulUNative(nuint a, nuint b) => unchecked(a * b);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static float MulR4(float a, float b) => a * b;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static double MulR8(double a, double b) => a * b;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int CheckedI4(int a, int b) => checked(a * b);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static uint CheckedU4(uint a, uint b) => checked(a * b);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long CheckedI8(long a, long b) => checked(a * b);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong CheckedU8(ulong a, ulong b) => checked(a * b);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static nint CheckedNative(nint a, nint b) => checked(a * b);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static nuint CheckedUNative(nuint a, nuint b) => checked(a * b);

    private static void ExpectOverflow(string label, Action action)
    {
        try
        {
            action();
            Console.WriteLine(label + "=no exception");
        }
        catch (OverflowException)
        {
            Console.WriteLine(label + "=OverflowException");
        }
    }

    internal static void __GateEntry()
    {
        Console.WriteLine("== integer hash wrapping ==");
        Console.WriteLine("char hash empty=" + CharHash("").ToString("x8"));
        Console.WriteLine("char hash shape=" + CharHash("int32_t|intptr_t").ToString("x8"));
        Console.WriteLine("char hash nul=" + CharHash("int32_t|intptr_t\0").ToString("x8"));
        Console.WriteLine("char hash wide=" + CharHash("\u0080\uffff\ud800\udfff").ToString("x8"));
        Console.WriteLine("byte hash=" + ByteHash(new byte[] { 0, 127, 128, 255 }).ToString("x8"));
        Console.WriteLine("i4 min neg=" + MulI4(int.MinValue, -1));
        Console.WriteLine("i8 min neg=" + MulI8(long.MinValue, -1));
        Console.WriteLine("u4 max squared=" + MulU4(uint.MaxValue, uint.MaxValue));
        Console.WriteLine("u8 max squared=" + MulU8(ulong.MaxValue, ulong.MaxValue));
        Console.WriteLine("native min neg=" + (MulNative(nint.MinValue, -1) == nint.MinValue));
        Console.WriteLine("native mixed wrap=" + (MulNative(nint.MaxValue, 3) == nint.MaxValue - 2));
        Console.WriteLine("unative wrap=" + (MulUNative(nuint.MaxValue, 3) == nuint.MaxValue - 2));
        Console.WriteLine("r4 multiply=" + (MulR4(1.5f, -2f) == -3f));
        Console.WriteLine("r8 multiply=" + (MulR8(1.5, -2) == -3));
        Console.WriteLine("r4 infinity=" + float.IsPositiveInfinity(MulR4(float.MaxValue, 2f)));
        Console.WriteLine("r8 nan=" + double.IsNaN(MulR8(double.PositiveInfinity, 0)));
        ExpectOverflow("checked i4", () => { CheckedI4(int.MinValue, -1); });
        ExpectOverflow("checked u4", () => { CheckedU4(uint.MaxValue, 2); });
        ExpectOverflow("checked i8", () => { CheckedI8(long.MinValue, -1); });
        ExpectOverflow("checked u8", () => { CheckedU8(ulong.MaxValue, 2); });
        ExpectOverflow("checked native", () => { CheckedNative(nint.MinValue, -1); });
        ExpectOverflow("checked unative", () => { CheckedUNative(nuint.MaxValue, 2); });
        Console.WriteLine("checked nonoverflow=" + (CheckedI4(-7, 6) == -42
            && CheckedU4(7, 6) == 42 && CheckedI8(-7, 6) == -42
            && CheckedU8(7, 6) == 42 && CheckedNative(-7, 6) == -42
            && CheckedUNative(7, 6) == 42));
        Console.WriteLine("integer hash wrapping end");
    }
}
