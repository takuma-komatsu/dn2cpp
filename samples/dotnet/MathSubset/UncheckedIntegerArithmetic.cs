using System;
using System.Runtime.CompilerServices;

namespace UncheckedIntegerArithmetic;

internal static class Program
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int AddI4(int a, int b) => unchecked(a + b);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int SubI4(int a, int b) => unchecked(a - b);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int MulI4(int a, int b) => unchecked(a * b);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static uint MulU4(uint a, uint b) => unchecked(a * b);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long AddI8(long a, long b) => unchecked(a + b);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long SubI8(long a, long b) => unchecked(a - b);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long MulI8(long a, long b) => unchecked(a * b);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong MulU8(ulong a, ulong b) => unchecked(a * b);

    internal static void __GateEntry()
    {
        Console.WriteLine("== unchecked integer arithmetic ==");
        Console.WriteLine("i4 add=" + AddI4(int.MaxValue, 1));
        Console.WriteLine("i4 sub=" + SubI4(int.MinValue, 1));
        Console.WriteLine("i4 mul=" + MulI4(int.MaxValue, 3));
        Console.WriteLine("u4 hash mul=" + MulU4(2166136261u, 16777619u));
        Console.WriteLine("i8 add=" + AddI8(long.MaxValue, 1));
        Console.WriteLine("i8 sub=" + SubI8(long.MinValue, 1));
        Console.WriteLine("i8 mul=" + MulI8(long.MaxValue, 3));
        Console.WriteLine("u8 mul=" + MulU8(ulong.MaxValue, 3));
        Console.WriteLine("unchecked integer arithmetic end");
    }
}
