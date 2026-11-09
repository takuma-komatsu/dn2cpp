using System;
using System.Runtime.CompilerServices;

namespace Dn2Cpp;

internal static class DecimalScaleSubset
{
    public static void Run()
    {
        Console.WriteLine("== decimal scale ==");
        Func<decimal, byte> readScale = ReadScale;
        foreach (bool negative in new[] { false, true })
            for (byte scale = 0; scale <= 28; scale++)
            {
                Show("zero:" + negative + ":" + scale,
                    new decimal(0, 0, 0, negative, scale), readScale);
                Show("96:" + negative + ":" + scale,
                    new decimal(-1, -1, -1, negative, scale), readScale);
            }
        foreach (decimal value in new[] { 1m, 1.0m, 1.00m, -1.000m })
            Show("equal:" + value, value, readScale);
        Console.WriteLine("decimal scale end");
    }

    private static void Show(string label, decimal value, Func<decimal, byte> readScale)
    {
        Console.WriteLine("scale:" + label + "=" + value.Scale + ":" + readScale(value));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static byte ReadScale(decimal value) => value.Scale;
}
