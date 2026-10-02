using System;
using System.Collections.Generic;

namespace KnownNullJoinSubset;

internal struct Unformatted
{
}

internal sealed class ModuloComparer : IEqualityComparer<int>
{
    public bool Equals(int x, int y) => x % 10 == y % 10;
    public int GetHashCode(int value) => value % 10;
}

internal static class Program
{
    // The IL fixture joins two null stack values instead of this direct null.
    internal static string NullJoin(bool choice) => string.Join<Unformatted>(",", (IEnumerable<Unformatted>)null);

    internal static bool MixedContains(bool choice) =>
        new[] { 3 }.AsSpan().Contains(13, choice ? new ModuloComparer() : null);

    // The IL fixture carries null on the stack until a backedge supplies a comparer.
    internal static bool BackedgeContains(bool replace) =>
        new[] { 3 }.AsSpan().Contains(13, (IEqualityComparer<int>)null);

    private static string NullResult(bool choice)
    {
        try
        {
            return NullJoin(choice);
        }
        catch (ArgumentNullException e)
        {
            return e.ParamName;
        }
    }

    internal static void Run()
    {
        Console.WriteLine("== known null stack joins ==");
        Console.WriteLine("join-null-forward=" + NullResult(false) + "/" + NullResult(true));
        Console.WriteLine("mixed-comparer=" + MixedContains(false) + "/" + MixedContains(true));
        Console.WriteLine("backedge-comparer=" + BackedgeContains(false) + "/" + BackedgeContains(true));
        Console.WriteLine("known null stack joins end");
    }
}
