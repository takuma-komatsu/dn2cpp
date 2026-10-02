#nullable enable
using System;

namespace FieldAliasProvenanceSubset;

internal struct OpaqueHolderMatch
{
    internal static int Calls;

    public override bool Equals(object? other)
    {
        Calls++;
        return other is null;
    }

    public override int GetHashCode() => 0;
}

internal struct ArrayGetHolderMatch
{
    internal static int Calls;

    public override bool Equals(object? other)
    {
        Calls++;
        return other is null;
    }

    public override int GetHashCode() => 0;
}

internal struct StaticHolderSelectedMatch
{
    internal static int Calls;

    public override bool Equals(object? other)
    {
        Calls++;
        return other is null;
    }

    public override int GetHashCode() => 0;
}

internal struct StaticHolderOverwrittenMatch
{
    public override bool Equals(object? other) => other is null;
    public override int GetHashCode() => 0;
}

internal sealed class Holder
{
    public Array? Values;
}

internal static class Program
{
    private static readonly object[] SharedOwners = { new Holder() };
    private static readonly Holder SharedHolder = new();

    private static Holder MakeHolder() => (Holder)Activator.CreateInstance(typeof(Holder))!;

    private static void Store(Holder holder) => holder.Values = new OpaqueHolderMatch[1];

    private static Array Load(Holder holder) => holder.Values!;

    private static Holder GetSharedHolder() => (Holder)SharedOwners.GetValue(0)!;

    private static void StoreShared() =>
        GetSharedHolder().Values = new ArrayGetHolderMatch[1];

    private static Array LoadShared() => GetSharedHolder().Values!;

    private static string StaticHolderOverwrite()
    {
        SharedHolder.Values = new StaticHolderOverwrittenMatch[1];
        SharedHolder.Values = new StaticHolderSelectedMatch[1];
        StaticHolderSelectedMatch.Calls = 0;
        return Array.IndexOf(SharedHolder.Values, null) + ":"
            + StaticHolderSelectedMatch.Calls;
    }

    internal static void Run()
    {
        Console.WriteLine("== array search field aliases ==");
        var holder = MakeHolder();
        Store(holder);
        OpaqueHolderMatch.Calls = 0;
        Console.WriteLine("opaque-holder=" + Array.IndexOf(Load(holder), null)
            + ":" + OpaqueHolderMatch.Calls);

        StoreShared();
        ArrayGetHolderMatch.Calls = 0;
        Console.WriteLine("array-get-holder=" + Array.IndexOf(LoadShared(), null)
            + ":" + ArrayGetHolderMatch.Calls);
        Console.WriteLine("static-holder-overwrite=" + StaticHolderOverwrite());
        Console.WriteLine("array search field aliases end");
    }
}
