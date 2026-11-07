using System;
using System.Runtime.CompilerServices;

namespace LocalCallArgumentSubset;

internal static class Program
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int[] Make(int value) => new int[] { value };

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int[] CollectThenMake(int value)
    {
        GC.Collect();
        return Make(value);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Check(int unused, int[] first, int[] second)
    {
        Console.WriteLine($"local pending call: first={first[0]} second={second[0]} same={ReferenceEquals(first, second)}");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void __GateEntry()
    {
        var first = Make(1);
        var second = CollectThenMake(2);
        // The leading argument keeps first in an IL local instead of the evaluation stack.
        Check(0, first, second);
    }
}
