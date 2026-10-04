using System;
using System.Collections.Generic;

namespace NullableEqualitySubset;

internal struct EquatableKey : IEquatable<EquatableKey>
{
    internal static int Calls;
    internal int Value;
    internal int Ignored;

    public bool Equals(EquatableKey other) => false;

    bool IEquatable<EquatableKey>.Equals(EquatableKey other)
    {
        Calls++;
        return Value == other.Value;
    }

    public override bool Equals(object other) => false;

    public override int GetHashCode() => Value;
}

internal struct PlainKey
{
    internal int Value;
    internal string Tag;
}

internal struct ObjectKey
{
    internal int Value;
    internal int Ignored;

    public bool Equals(ObjectKey other) => false;

    public override bool Equals(object other) => other is ObjectKey key && Value == key.Value;

    public override int GetHashCode() => Value;
}

internal static class Program
{
    private static void GenericCases<T>(string name, T first, T equal, T different) where T : struct
    {
        var comparer = EqualityComparer<T?>.Default;
        Console.WriteLine(name + ": " + comparer.Equals(null, null) + " "
            + comparer.Equals(first, null) + " " + comparer.Equals(null, first) + " "
            + comparer.Equals(first, equal) + " " + comparer.Equals(first, different));
    }

    internal static void __GateEntry()
    {
        Console.WriteLine("== nullable default equality ==");
        var ints = EqualityComparer<int?>.Default;
        Console.WriteLine("typed int: " + ints.Equals(null, null) + " " + ints.Equals(7, null)
            + " " + ints.Equals(null, 7) + " " + ints.Equals(7, 7) + " " + ints.Equals(7, 8));
        GenericCases("generic int", 7, 7, 8);
        GenericCases("generic byte", (byte)255, (byte)255, (byte)0);
        GenericCases("generic char", 'a', 'a', 'b');
        GenericCases("generic NaN", double.NaN, double.NaN, 1.0);
        GenericCases("generic signed zero", 0.0f, -0.0f, 1.0f);
        GenericCases("generic decimal", 1.0m, 1.00m, 2.0m);

        var first = new EquatableKey { Value = 3, Ignored = 1 };
        var equal = new EquatableKey { Value = 3, Ignored = 2 };
        var different = new EquatableKey { Value = 4, Ignored = 1 };
        var keys = EqualityComparer<EquatableKey?>.Default;
        EquatableKey.Calls = 0;
        Console.WriteLine("typed equatable: " + keys.Equals(null, null) + " " + keys.Equals(first, null)
            + " " + keys.Equals(null, first) + " " + keys.Equals(first, equal)
            + " " + keys.Equals(first, different) + " " + EquatableKey.Calls);
        EquatableKey.Calls = 0;
        GenericCases("generic equatable", first, equal, different);
        Console.WriteLine("generic equatable calls: " + EquatableKey.Calls);
        GenericCases("generic plain", new PlainKey { Value = 3, Tag = "ab" },
            new PlainKey { Value = 3, Tag = new string(new[] { 'a', 'b' }) },
            new PlainKey { Value = 3, Tag = "ac" });
        GenericCases("generic object override", new ObjectKey { Value = 3, Ignored = 1 },
            new ObjectKey { Value = 3, Ignored = 2 }, new ObjectKey { Value = 4, Ignored = 1 });

        var set = new HashSet<int?> { null, null, 7, 7, 8 };
        var list = new List<int?> { null, 7, 8 };
        Console.WriteLine("nullable int collections: " + set.Count + " " + set.Contains(null)
            + " " + set.Contains(7) + " " + set.Contains(9) + " " + list.IndexOf(null)
            + " " + list.IndexOf(7) + " " + list.IndexOf(9));
        var keySet = new HashSet<EquatableKey?> { null, null, first, equal, different };
        var keyList = new List<EquatableKey?> { null, first };
        var keyArray = new EquatableKey?[] { null, first };
        Console.WriteLine("nullable equatable collections: " + keySet.Count + " " + keySet.Contains(equal)
            + " " + keyList.Contains(equal) + " " + keyList.IndexOf(equal)
            + " " + Array.IndexOf(keyArray, null) + " " + Array.IndexOf(keyArray, equal)
            + " " + Array.IndexOf(keyArray, different));
        Console.WriteLine("nullable default equality end");
    }
}
