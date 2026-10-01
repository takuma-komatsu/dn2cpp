using System;
using System.Collections;
using System.Collections.Generic;

// EqualityComparer<T>.Default and its generic collection paths use IEquatable<T>
// for a reference T that implements it, otherwise Equals(object). Non-generic
// Array.IndexOf uses Equals(object). Neither receives a null operand. A boxed
// framework struct keys an object-keyed set or dictionary by value. A struct key whose
// IEquatable<T> is implemented explicitly beside a public Equals(T) runs the explicit
// body, and one declaring Equals(T) without implementing IEquatable<T> runs its
// Equals(object) override.
namespace DefaultEqualityMouthsSubset;

internal sealed class Counting
{
    public int Calls;

    public override bool Equals(object obj)
    {
        Calls++;
        return ReferenceEquals(this, obj);
    }

    public override int GetHashCode() => 7;
}

internal sealed class NullLover
{
    public int Calls;

    public override bool Equals(object obj)
    {
        Calls++;
        return obj is null || ReferenceEquals(obj, this);
    }

    public override int GetHashCode() => 8;
}

internal struct ExplicitKey : IEquatable<ExplicitKey>
{
    public static string Trace = "";
    public int Value;

    public bool Equals(ExplicitKey other)
    {
        Trace += "P";
        return Value == other.Value;
    }

    bool IEquatable<ExplicitKey>.Equals(ExplicitKey other)
    {
        Trace += "X";
        return Value == other.Value;
    }

    public override bool Equals(object obj)
    {
        Trace += "O";
        return obj is ExplicitKey other && other.Value == Value;
    }

    public override int GetHashCode() => Value;
}

internal struct TypedOnlyKey
{
    public static string Trace = "";
    public int Value;

    public bool Equals(TypedOnlyKey other)
    {
        Trace += "T";
        return Value == other.Value;
    }

    public override bool Equals(object obj)
    {
        Trace += "O";
        return obj is TypedOnlyKey other && other.Value == Value;
    }

    public override int GetHashCode() => Value;
}

internal sealed class EquatableReference : IEquatable<EquatableReference>, IEquatable<object>
{
    internal readonly int Value;
    internal int Calls;

    internal EquatableReference(int value) => Value = value;

    public bool Equals(EquatableReference other) => false;

    bool IEquatable<EquatableReference>.Equals(EquatableReference other)
    {
        Calls++;
        return other is not null && Value == other.Value;
    }

    bool IEquatable<object>.Equals(object other) => false;

    public override int GetHashCode() => Value;
}

internal sealed class CollectionOnlyReference : IEquatable<CollectionOnlyReference>
{
    internal readonly int Value;

    internal CollectionOnlyReference(int value) => Value = value;

    public bool Equals(CollectionOnlyReference other) => other is not null && Value == other.Value;

    public override int GetHashCode() => Value;
}

internal class VariantBase : IEquatable<VariantBase>
{
    internal readonly int Value;

    internal VariantBase(int value) => Value = value;

    public bool Equals(VariantBase other) => other is not null && Value == other.Value;

    public override int GetHashCode() => Value;
}

internal sealed class VariantDerived : VariantBase
{
    internal VariantDerived(int value) : base(value) { }
}

internal static class Program
{
    private static string Seen(ref string trace)
    {
        string seen = trace.Length == 0 ? "-" : trace.Substring(0, 1);
        trace = "";
        return seen;
    }

    internal static void __GateEntry()
    {
        Console.WriteLine("== default equality mouths ==");
        var counting = new Counting();
        Console.WriteLine("identical key: " + EqualityComparer<object>.Default.Equals(counting, counting) + " "
            + EqualityComparer<Counting>.Default.Equals(counting, counting) + " " + counting.Calls);
        var dict = new Dictionary<object, int> { [counting] = 1 };
        var set = new HashSet<object> { counting };
        var list = new List<Counting> { counting };
        int before = counting.Calls;
        Console.WriteLine("identical key, collections: " + dict[counting] + " " + set.Contains(counting) + " "
            + list.IndexOf(counting) + " " + Array.IndexOf(new object[] { counting }, counting) + " "
            + Array.IndexOf((Array)new object[] { counting }, (object)counting) + " " + (counting.Calls - before));
        var lover = new NullLover();
        Console.WriteLine("null key: " + EqualityComparer<object>.Default.Equals(lover, null) + " "
            + EqualityComparer<object>.Default.Equals(null, lover) + " " + EqualityComparer<object>.Default.Equals(null, null)
            + " " + new List<object> { lover }.Contains(null) + " " + Array.IndexOf((Array)new object[] { lover }, (object)null)
            + " " + lover.Calls);
        var boxes = new HashSet<object>
        {
            (1, 2), (1, 2), (2, 1), ("x", 1), ("x", 1), new Index(3), new Index(3), 1..2, 1..2,
            (Int128)5, (Int128)5, (Half)1, (Half)1, new System.Text.Rune('a'), new System.Text.Rune('a'),
        };
        var byTuple = new Dictionary<object, string>();
        byTuple[(1, "a")] = "first";
        byTuple[(1, "a")] = "second";
        Console.WriteLine("boxed framework keys: " + boxes.Count + " " + byTuple.Count + " " + byTuple[(1, "a")]
            + " " + boxes.Contains((2, 1)) + " " + boxes.Contains(1..3));
        var key = new ExplicitKey { Value = 4 };
        Console.WriteLine("explicit IEquatable key: " + EqualityComparer<ExplicitKey>.Default.Equals(key, key) + Seen(ref ExplicitKey.Trace)
            + " " + new List<ExplicitKey> { key }.Contains(key) + Seen(ref ExplicitKey.Trace)
            + " " + Array.IndexOf(new[] { key }, key) + Seen(ref ExplicitKey.Trace)
            + " " + new Dictionary<ExplicitKey, int> { [key] = 1 }.ContainsKey(key) + Seen(ref ExplicitKey.Trace));
        var typed = new TypedOnlyKey { Value = 5 };
        Console.WriteLine("typed Equals without IEquatable: " + EqualityComparer<TypedOnlyKey>.Default.Equals(typed, typed) + Seen(ref TypedOnlyKey.Trace)
            + " " + new List<TypedOnlyKey> { typed }.Contains(typed) + Seen(ref TypedOnlyKey.Trace)
            + " " + new Dictionary<TypedOnlyKey, int> { [typed] = 1 }.ContainsKey(typed) + Seen(ref TypedOnlyKey.Trace)
            + " " + new HashSet<TypedOnlyKey> { typed, typed }.Count + Seen(ref TypedOnlyKey.Trace));
        var first = new EquatableReference(17);
        var same = new EquatableReference(17);
        var other = new EquatableReference(18);
        var referenceComparer = EqualityComparer<EquatableReference>.Default;
        Console.WriteLine("reference IEquatable comparer: " + referenceComparer.Equals(first, same) + " "
            + referenceComparer.Equals(first, first) + " " + referenceComparer.Equals(first, other) + " "
            + referenceComparer.Equals(first, null) + " " + referenceComparer.Equals(null, null) + " "
            + EqualityComparer<object>.Default.Equals(first, same) + " " + first.Calls);
        var refDict = new Dictionary<EquatableReference, int> { [first] = 7 };
        var refSet = new HashSet<EquatableReference> { first };
        var refList = new List<EquatableReference> { first };
        Console.WriteLine("reference IEquatable collections: " + refDict.ContainsKey(same) + " "
            + refSet.Contains(same) + " " + refList.Contains(same) + " "
            + refList.IndexOf(same) + " " + refList.IndexOf(other) + " " + first.Calls);
        Console.WriteLine("reference IEquatable arrays: " + Array.IndexOf(new[] { first }, same)
            + " " + Array.IndexOf((Array)new[] { first }, (object)same));
        var nongenericReferenceComparer = (System.Collections.IEqualityComparer)referenceComparer;
        Console.WriteLine("reference IEquatable nongeneric: " + nongenericReferenceComparer.Equals(first, same) + " "
            + nongenericReferenceComparer.Equals(first, first) + " "
            + nongenericReferenceComparer.Equals(first, null) + " " + first.Calls);
        string wrongType = "-";
        try { nongenericReferenceComparer.Equals(first, new Counting()); }
        catch (ArgumentException e) { wrongType = e.GetType().Name; }
        Console.WriteLine("reference IEquatable nongeneric type: " + wrongType);
        try { _ = nongenericReferenceComparer.Equals(first, new object()); }
        catch (ArgumentException ex)
        {
            Console.WriteLine("reference IEquatable nongeneric message: " + ex.Message);
            Console.WriteLine("reference IEquatable nongeneric param: " + (ex.ParamName ?? "null"));
        }
        var lone = new CollectionOnlyReference(23);
        var loneEqual = new CollectionOnlyReference(23);
        Console.WriteLine("collection-only IEquatable: "
            + new Dictionary<CollectionOnlyReference, int> { [lone] = 1 }.ContainsKey(loneEqual) + " "
            + new HashSet<CollectionOnlyReference> { lone }.Contains(loneEqual) + " "
            + new List<CollectionOnlyReference> { lone }.IndexOf(loneEqual));
        var derived = new VariantDerived(29);
        var derivedEqual = new VariantDerived(29);
        Console.WriteLine("variant IEquatable: " + EqualityComparer<VariantDerived>.Default.Equals(derived, derivedEqual)
            + " " + new HashSet<VariantDerived> { derived }.Contains(derivedEqual));
        int[] vector = { 1, 2 };
        int[] vectorEqual = { 1, 2 };
        int[,] matrix = { { 1, 2 } };
        int[,] matrixEqual = { { 1, 2 } };
        Console.WriteLine("array reference equality: " + EqualityComparer<int[]>.Default.Equals(vector, vector)
            + " " + EqualityComparer<int[]>.Default.Equals(vector, vectorEqual)
            + " " + new HashSet<int[]> { vector }.Contains(vectorEqual)
            + " " + EqualityComparer<int[,]>.Default.Equals(matrix, matrix)
            + " " + EqualityComparer<int[,]>.Default.Equals(matrix, matrixEqual)
            + " " + new List<int[,]> { matrix }.Contains(matrixEqual));
        Console.WriteLine("default equality mouths end");
    }
}
