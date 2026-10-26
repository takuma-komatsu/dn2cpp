using System;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Dn2Cpp;

internal static class MathMixedBigMulSubset
{
    private delegate ulong UInt32First(uint a, ulong b, out ulong low);
    private delegate ulong UInt64First(ulong a, uint b, out ulong low);

    // The fixture redirects these call sites to CoreLib's internal overloads.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong Multiply32x64(uint a, ulong b, out ulong low) => Math.BigMul((ulong)a, b, out low);
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong Multiply64x32(ulong a, uint b, out ulong low) => Math.BigMul(a, (ulong)b, out low);

    private static void Print(string tag, ulong high, ulong low) => Console.WriteLine($"{tag}={high}:{low}");

    internal static void Run()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("== mixed-width BigMul ==");
        ulong low;
        Print("direct32-max", Multiply32x64(uint.MaxValue, ulong.MaxValue, out low), low);
        Print("direct32-high", Multiply32x64(0x80000000u, 0x8000000000000001UL, out low), low);
        Print("direct32-zero", Multiply32x64(0, ulong.MaxValue, out low), low);
        Print("direct32-carry", Multiply32x64(2, 0x8000000000000000UL, out low), low);
        Print("direct64-max", Multiply64x32(ulong.MaxValue, uint.MaxValue, out low), low);
        Print("direct64-high", Multiply64x32(0x8000000000000001UL, 0x80000000u, out low), low);
        Print("direct64-zero", Multiply64x32(ulong.MaxValue, 0, out low), low);
        Print("direct64-carry", Multiply64x32(0x8000000000000000UL, 2, out low), low);
        UInt32First first = Multiply32x64;
        UInt64First second = Multiply64x32;
        Print("address32-max", first(uint.MaxValue, ulong.MaxValue, out low), low);
        Print("address32-high", first(0x80000000u, 0x8000000000000001UL, out low), low);
        Print("address64-max", second(ulong.MaxValue, uint.MaxValue, out low), low);
        Print("address64-high", second(0x8000000000000001UL, 0x80000000u, out low), low);
        Console.WriteLine("mixed-width BigMul end");
    }
}
