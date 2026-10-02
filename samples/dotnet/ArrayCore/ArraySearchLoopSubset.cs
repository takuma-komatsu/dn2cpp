#nullable enable
using System;
using System.Runtime.InteropServices;

namespace ArraySearchLoopSubset;

internal struct LoopFirstMatch
{
    internal static int Calls;
    public override bool Equals(object? other) { Calls++; return other is null; }
    public override int GetHashCode() => 0;
}

internal struct LoopSecondMatch
{
    internal static int Calls;
    public override bool Equals(object? other) { Calls++; return other is null; }
    public override int GetHashCode() => 0;
}

internal struct LoopTypeMatch
{
    internal static int Calls;
    public override bool Equals(object? other) { Calls++; return other is null; }
    public override int GetHashCode() => 0;
}

internal struct LoopUnsearchedMatch
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
    public int[] Values;
    public LoopUnsearchedMatch() { Values = Array.Empty<int>(); }
}

internal static class Program
{
    private static string LocalSearch()
    {
        Array values = new LoopFirstMatch[1];
        LoopFirstMatch.Calls = LoopSecondMatch.Calls = 0;
        int result = 0;
        int count = 0;
        while (count++ < 2)
        {
            result += Array.IndexOf(values, null);
            values = new LoopSecondMatch[1];
        }
        return result + ":" + LoopFirstMatch.Calls + ":" + LoopSecondMatch.Calls;
    }

    private static string ArgumentSearch(Array values)
    {
        LoopFirstMatch.Calls = LoopSecondMatch.Calls = 0;
        int result = 0;
        for (int count = 0; count < 2; count++)
        {
            result += Array.LastIndexOf(values, null);
            values = new LoopSecondMatch[1];
        }
        return result + ":" + LoopFirstMatch.Calls + ":" + LoopSecondMatch.Calls;
    }

    private static int ConstructTypeSearch()
    {
        // MakeArrayType needs its leaf array shape in the AOT image.
        _ = new int[0];
        Type type = typeof(int);
        int count = 2;
        do { type = type.MakeArrayType(); } while (--count > 0);
        return Array.IndexOf(Array.CreateInstance(type, 1), null);
    }

    private static string ConstructAndUnwrapSearch()
    {
        Type type = typeof(LoopTypeMatch);
        int count = 8;
        do
        {
            type = Array.CreateInstance(type, 1).GetType();
            type = Array.CreateInstance(type, 1).GetType();
        } while (--count > 0);
        type = type.GetElementType()!.GetElementType()!.GetElementType()!.GetElementType()!
            .GetElementType()!.GetElementType()!.GetElementType()!.GetElementType()!
            .GetElementType()!.GetElementType()!.GetElementType()!.GetElementType()!
            .GetElementType()!.GetElementType()!.GetElementType()!.GetElementType()!;
        LoopTypeMatch.Calls = 0;
        int result = Array.IndexOf(Array.CreateInstance(type, 1), null);
        return result + ":" + LoopTypeMatch.Calls;
    }

    private static int UnsearchedLeafSearch()
    {
        _ = new LoopUnsearchedMatch[0];
        Type type = typeof(LoopUnsearchedMatch);
        int count = 2;
        do { type = type.MakeArrayType(); } while (--count > 0);
        return Array.IndexOf(Array.CreateInstance(type, 1), null);
    }

    internal static void Run()
    {
        Console.WriteLine("== array search loop provenance ==");
        Console.WriteLine("loop-local=" + LocalSearch());
        Console.WriteLine("loop-argument=" + ArgumentSearch(new LoopFirstMatch[1]));
#if !ARRAY_SEARCH_LOOP_BACKWARD_ONLY
        Console.WriteLine("loop-type=" + ConstructTypeSearch());
        Console.WriteLine("loop-type-unwrapped=" + ConstructAndUnwrapSearch());
        Console.WriteLine("loop-unsearched-leaf=" + UnsearchedLeafSearch());
#endif
        Console.WriteLine("array search loop provenance end");
    }
}
