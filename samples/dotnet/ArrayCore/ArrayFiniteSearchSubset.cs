#nullable enable
using System;

namespace ArrayFiniteSearchSubset;

internal struct Selected
{
    internal int Id;
    internal static int Calls;

    public override bool Equals(object? other)
    {
        Calls = Calls * 10 + Id;
        return other is null && Id == 2;
    }

    public override int GetHashCode() => Id;
}

internal struct Alternative
{
    public override bool Equals(object? other) => other is null;
    public override int GetHashCode() => 0;
}

internal struct GenericOnly
{
    public override bool Equals(object? other) => other is null;
    public override int GetHashCode() => 0;
}

internal struct Unsearched
{
    public override bool Equals(object? other) => other is null;
    public override int GetHashCode() => 0;
}

internal static class Program
{
    private static Array? retained;

    private static void Escape(Array array) => retained = array;

    private static int GenericSearch<T>() where T : struct => Array.IndexOf((Array)new T[1], null);

    private static void JoinedSearch(bool first)
    {
        Array array = first ? new Selected[] { new() { Id = 2 } } : new Alternative[1];
        Console.WriteLine("joined=" + Array.LastIndexOf(array, null));
    }

    internal static void Run()
    {
        Console.WriteLine("== finite local array searches ==");
        Array array = new Selected[] { new() { Id = 1 }, new() { Id = 2 }, new() { Id = 3 } };
        Escape(array);
        Selected.Calls = 0;
        Console.WriteLine("forward=" + Array.IndexOf(array, null) + ":" + Selected.Calls);
        Selected.Calls = 0;
        Console.WriteLine("backward=" + Array.LastIndexOf(array, null) + ":" + Selected.Calls);
        Array nullable = new Selected?[] { new Selected { Id = 2 }, null };
        Console.WriteLine("nullable=" + Array.IndexOf(nullable, null));
        JoinedSearch(true);
        JoinedSearch(false);
        Console.WriteLine("generic=" + GenericSearch<GenericOnly>());
        // Allocation alone must not admit the unsearched element's equality body.
        Array unused = new Unsearched[3];
        Console.WriteLine("unsearched=" + unused.Length + ":" + retained!.Length);
        Console.WriteLine("finite local array searches end");
    }
}
