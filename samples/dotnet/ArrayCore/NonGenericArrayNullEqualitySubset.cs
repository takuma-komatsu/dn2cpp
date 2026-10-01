#nullable enable
using System;

namespace NonGenericArrayNullEqualitySubset;

internal struct NullMatch
{
    internal static int Calls;
    internal int Id;

    internal NullMatch(int id) => Id = id;

    public override bool Equals(object? other)
    {
        Calls = Calls * 10 + Id;
        return other is null ? Id == 2 : other is NullMatch m && m.Id == Id;
    }

    public override int GetHashCode() => Id;
}

internal struct DirectOnlyNullMatch
{
    internal static int Calls;
    internal int Id;

    internal DirectOnlyNullMatch(int id) => Id = id;

    public override bool Equals(object? other)
    {
        Calls = Calls * 10 + Id;
        return other is null && Id == 2;
    }

    public override int GetHashCode() => Id;
}

internal struct BoxOnlyNullMatch
{
    internal static int Calls;
    internal int Id;

    internal BoxOnlyNullMatch(int id) => Id = id;

    public override bool Equals(object? other)
    {
        Calls = Calls * 10 + Id;
        return other is BoxOnlyNullMatch m && m.Id == Id;
    }

    public override int GetHashCode() => Id;
}

internal struct GenericArrayOnlyNullMatch
{
    internal static int Calls;

    public override bool Equals(object? other)
    {
        Calls++;
        return other is null;
    }

    public override int GetHashCode() => 0;
}

internal sealed class NullClass
{
    internal static int Calls;

    public override bool Equals(object? other)
    {
        Calls++;
        return other is NullClass;
    }

    public override int GetHashCode() => 0;
}

// A jagged array's immediate elements are references, even when its leaves
// are structs. The leaf's Equals body must stay unreachable.
internal struct DeadNullMatch
{
    public override bool Equals(object? other) => other is null;
    public override int GetHashCode() => 0;
}

internal static class Program
{
    private static Array MakeArray<T>() => new T[1];

    private static void RunJagged()
    {
        Array direct = new DeadNullMatch[1][];
        Array created = Array.CreateInstance(typeof(DeadNullMatch[]), 1);
        Console.WriteLine("jagged-direct=" + Array.IndexOf(direct, null));
        Console.WriteLine("jagged-created=" + Array.IndexOf(created, null));
    }

    internal static void Run()
    {
        Console.WriteLine("== non-generic null search equality ==");
        Array values = new DirectOnlyNullMatch[] { new(1), new(2), new(3) };
        DirectOnlyNullMatch.Calls = 0;
        Console.WriteLine("value-forward=" + Array.IndexOf(values, null) + ":" + DirectOnlyNullMatch.Calls);
        DirectOnlyNullMatch.Calls = 0;
        Console.WriteLine("value-backward=" + Array.LastIndexOf(values, null) + ":" + DirectOnlyNullMatch.Calls);
        Func<Array, object?, int> delegatedSearch = Array.IndexOf;
        DirectOnlyNullMatch.Calls = 0;
        Console.WriteLine("delegate-forward=" + delegatedSearch(values, null) + ":" + DirectOnlyNullMatch.Calls);

        Array classes = new NullClass[] { new(), new() };
        NullClass.Calls = 0;
        Console.WriteLine("class-forward=" + Array.IndexOf(classes, null) + ":" + NullClass.Calls);
        Console.WriteLine("class-backward=" + Array.LastIndexOf(classes, null) + ":" + NullClass.Calls);
        Array objects = new object?[] { new NullClass(), null };
        NullClass.Calls = 0;
        Console.WriteLine("object-forward=" + Array.IndexOf(objects, null) + ":" + NullClass.Calls);

        Array boxed = new object[] { new BoxOnlyNullMatch(1), new BoxOnlyNullMatch(2) };
        BoxOnlyNullMatch.Calls = 0;
        Console.WriteLine("boxed-forward=" + Array.IndexOf(boxed, new BoxOnlyNullMatch(2)) + ":" + BoxOnlyNullMatch.Calls);

        Array generic = MakeArray<GenericArrayOnlyNullMatch>();
        GenericArrayOnlyNullMatch.Calls = 0;
        Console.WriteLine("generic-value-forward=" + Array.IndexOf(generic, null) + ":" + GenericArrayOnlyNullMatch.Calls);

        Array nullable = new NullMatch?[] { new NullMatch(1), null, new NullMatch(2) };
        NullMatch.Calls = 0;
        Console.WriteLine("nullable-forward=" + Array.IndexOf(nullable, null) + ":" + NullMatch.Calls);
        NullMatch.Calls = 0;
        Console.WriteLine("nullable-backward=" + Array.LastIndexOf(nullable, null) + ":" + NullMatch.Calls);

        Console.WriteLine("integer-null=" + Array.IndexOf((Array)new int[] { 1, 2 }, null));
        Console.WriteLine("enum-null=" + Array.LastIndexOf((Array)new DayOfWeek[] { DayOfWeek.Monday }, null));
        RunJagged();
    }
}
