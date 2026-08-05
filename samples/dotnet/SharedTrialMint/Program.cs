using System;
using System.Collections.Generic;

namespace SharedTrialMint;

/// <summary>Mints a generic instantiation no IL token names: only the shared-generics
/// canonical body ever translates the <c>int[] -&gt; IEnumerable&lt;int&gt;</c> boundary,
/// whose collection-interface map first instantiates the emitter-invented
/// <c>SZArrayEnumerable&lt;int&gt;</c> inside <c>CppEmitter</c>'s planning-pass
/// <c>try</c> — so shrinking <c>DN2CPP_MAX_INSTANTIATIONS</c> trips the bound exactly
/// where a swallowed <c>InstantiationBoundException</c> would be invisible.</summary>
internal sealed class W<T>
{
    internal T? Value;
}

internal static class Shared
{
    private static int Total(IEnumerable<int> xs)
    {
        int n = 0;
        foreach (int x in xs)
            n += x;
        return n;
    }

    internal static int Run<T>(T a, T b) where T : class
    {
        var w = new W<T> { Value = a };
        // int[] into an IEnumerable<int> parameter: the boundary that wires the
        // array's collection-interface map.
        int[] arr = new int[3];
        arr[0] = ReferenceEquals(w.Value, b) ? 1 : 0;
        arr[1] = 2;
        arr[2] = 4;
        return Total(arr);
    }
}

internal static class Program
{
    private static void Main()
    {
        Console.WriteLine(Shared.Run("a", "b"));
        Console.WriteLine(Shared.Run(new object(), new object()));
        Console.WriteLine(Shared.Run(new int[1], new int[1]));
    }
}
