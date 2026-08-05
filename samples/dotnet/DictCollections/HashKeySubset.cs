#nullable enable
// reference-type `object` virtual dispatch — GetHashCode() / Equals(object).
// `obj.GetHashCode()` / `obj.Equals(o)` on a reference type whose declared callee
// is System.Object dispatch the type's override via type-info fn-ptr slots
// (wired like the ToString slot), falling back to identity hash /
// reference equality. This lets a record / class with value equality work as a
// HashSet/Dictionary key: the reference-key path (EqualityComparer<T> over a ref
// key) routes through dn2cpp_object_gethashcode/_equals. A record's hash also
// folds in its EqualityContract (a System.Type) — Type hashes by its type
// identity, consistent with its equality.
//
// Then float/double keys, whose equality and hash the transpiler emits INLINE
// (EqualityEqualsExpr / PrimitiveHashExpr) rather than dispatching: Double.Equals
// is not `==` (NaN equals NaN) and the hash is the bits, not the number.
//
// Real System.Private.CoreLib (-r), run vs .NET.
using System;
using System.Collections.Generic;
namespace HashKeySubset;


record Point(int X, int Y);

class Boxy
{
    public int Id;
    public Boxy(int id) { Id = id; }
    public override int GetHashCode() => Id * 31;
    public override bool Equals(object? o) => o is Boxy b && b.Id == Id;
}

class Plain
{
    public int V;
    public Plain(int v) { V = v; }
}

class Program
{internal static void __GateEntry()
    {
        var a = new Point(1, 2);
        var b = new Point(1, 2);
        Console.WriteLine("hash eq=" + (a.GetHashCode() == b.GetHashCode()));
        Console.WriteLine("equals=" + a.Equals(b));
        Console.WriteLine("equals3=" + a.Equals(new Point(3, 4)));

        var set = new HashSet<Point>();
        set.Add(new Point(1, 2));
        set.Add(new Point(1, 2));
        set.Add(new Point(3, 4));
        Console.WriteLine("setcount=" + set.Count);
        Console.WriteLine("contains=" + set.Contains(new Point(3, 4)));

        var dict = new Dictionary<Point, string>();
        dict[new Point(1, 2)] = "first";
        dict[new Point(1, 2)] = "second";
        dict[new Point(5, 6)] = "third";
        Console.WriteLine("dictcount=" + dict.Count);
        Console.WriteLine("lookup=" + dict[new Point(1, 2)]);

        var x = new Boxy(7);
        Console.WriteLine("boxyhash=" + (x.GetHashCode() == new Boxy(7).GetHashCode()));
        Console.WriteLine("boxyeq=" + x.Equals(new Boxy(7)));
        var bset = new HashSet<Boxy>();
        bset.Add(new Boxy(7)); bset.Add(new Boxy(7)); bset.Add(new Boxy(8));
        Console.WriteLine("boxysetcount=" + bset.Count);

        var p1 = new Plain(1);
        var p2 = new Plain(1);
        Console.WriteLine("plaineq=" + p1.Equals(p2));
        Console.WriteLine("plainself=" + p1.Equals(p1));
        var pset = new HashSet<Plain>();
        pset.Add(p1); pset.Add(p1); pset.Add(p2);
        Console.WriteLine("plainsetcount=" + pset.Count);

        FloatKeys();
    }

    // float/double keys. Equality here is Double.Equals, NOT `==`: NaN equals NaN,
    // and +0.0 equals -0.0. The hash is the BITS, not the number, with every NaN and
    // both zeros normalized so the two pairs Equals and `==` disagree about still
    // land in one bucket. Nothing here enumerates a set/dict — the order is
    // hash-dependent, and Comparer<double>.Default sorts NaN below every number, so
    // even an OrderBy would be asserting the ordering model rather than this one.
    private static void FloatKeys()
    {
        // `==` says a NaN is not even itself. That is the operator; a KEY uses
        // Double.Equals, which says it is — so the dictionary below finds it.
        double nan = double.NaN, nan2 = double.NaN;
        Console.WriteLine("d nan==nan=" + (nan == nan2));
        double zero = 0.0, negZero = -0.0;
        Console.WriteLine("d 0==-0=" + (zero == negZero));

        var d = new Dictionary<double, string>
        {
            [1.5] = "one-point-five",
            [double.NaN] = "nan",
            [0.0] = "zero",
            [double.PositiveInfinity] = "+inf",
            [double.NegativeInfinity] = "-inf",
        };
        Console.WriteLine("d count=" + d.Count);
        Console.WriteLine("d has 1.5=" + d.ContainsKey(1.5));
        Console.WriteLine("d has nan=" + d.ContainsKey(double.NaN));
        Console.WriteLine("d has -0.0=" + d.ContainsKey(-0.0));
        Console.WriteLine("d has 1.7=" + d.ContainsKey(1.7));
        Console.WriteLine("d get nan=" + d[double.NaN]);
        Console.WriteLine("d get -0.0=" + d[-0.0]);
        Console.WriteLine("d get +inf=" + d[double.PositiveInfinity]);
        Console.WriteLine("d get -inf=" + d[double.NegativeInfinity]);
        // -0.0 is the SAME key as +0.0 — an overwrite, never a second entry.
        d[-0.0] = "still-zero";
        Console.WriteLine("d count after -0.0=" + d.Count);
        Console.WriteLine("d get 0.0=" + d[0.0]);
        // Two values with the same integer part must not collide into one key
        // (what truncating the hash to int32 would have done).
        d[1.7] = "one-point-seven";
        Console.WriteLine("d count after 1.7=" + d.Count);
        Console.WriteLine("d get 1.5 still=" + d[1.5]);

        var fs = new HashSet<float> { 2.5f, float.NaN, 0.0f };
        Console.WriteLine("f count=" + fs.Count);
        Console.WriteLine("f has nan=" + fs.Contains(float.NaN));
        Console.WriteLine("f has -0.0=" + fs.Contains(-0.0f));
        Console.WriteLine("f has 2.5=" + fs.Contains(2.5f));
        // A NaN key is already there; adding it again is a no-op, not a duplicate.
        Console.WriteLine("f add nan again=" + fs.Add(float.NaN));
        Console.WriteLine("f add -0.0=" + fs.Add(-0.0f));
        Console.WriteLine("f count after=" + fs.Count);

        // The same two questions asked DIRECTLY of the value, rather than through a
        // collection: `d.GetHashCode()` / `d.Equals(other)`. Three sites must agree —
        // the inline key emit above, the boxed dispatch through dn2cpp_object_*, and
        // this direct call — because a program is free to mix them, and a key that
        // hashed differently depending on how it was asked would sit in two buckets.
        // The raw hash VALUE is printed here: it is the IEEE bit fold, so it is a
        // stable number, not a runtime-seeded one (unlike a string's).
        double h1 = 1.5, h2 = 1.5;
        Console.WriteLine("dir d hash=" + h1.GetHashCode());
        Console.WriteLine("dir d hash eq=" + (h1.GetHashCode() == h2.GetHashCode()));
        Console.WriteLine("dir d nan hash=" + double.NaN.GetHashCode());
        Console.WriteLine("dir d 0 hash=" + (0.0).GetHashCode());
        Console.WriteLine("dir d -0 hash=" + (-0.0).GetHashCode());
        Console.WriteLine("dir d eq=" + h1.Equals(h2));
        Console.WriteLine("dir d eq 1.7=" + h1.Equals(1.7));
        Console.WriteLine("dir d nan eq nan=" + double.NaN.Equals(double.NaN));
        Console.WriteLine("dir d 0 eq -0=" + (0.0).Equals(-0.0));
        Console.WriteLine("dir d cmp=" + h1.CompareTo(1.7));

        float g1 = 2.5f;
        Console.WriteLine("dir f hash=" + g1.GetHashCode());
        Console.WriteLine("dir f nan hash=" + float.NaN.GetHashCode());
        Console.WriteLine("dir f -0 hash=" + (-0.0f).GetHashCode());
        Console.WriteLine("dir f eq=" + g1.Equals(2.5f));
        Console.WriteLine("dir f nan eq nan=" + float.NaN.Equals(float.NaN));
        Console.WriteLine("dir f 0 eq -0=" + (0.0f).Equals(-0.0f));

        // The inline / boxed / direct triple, on one value.
        object boxed = 1.5;
        Console.WriteLine("triple hash=" + (h1.GetHashCode() == boxed.GetHashCode()));
        Console.WriteLine("triple eq=" + boxed.Equals(1.5));

        // Double.Equals(object) — `obj is double d && Equals(d)`. A boxed float is a
        // different type and never equals a boxed double, even at the same value.
        object boxedF = 1.5f;
        object boxedStr = "1.5";
        object? boxedNull = null;
        Console.WriteLine("obj d eq d=" + h1.Equals(boxed));
        Console.WriteLine("obj d eq f=" + h1.Equals(boxedF));
        Console.WriteLine("obj d eq str=" + h1.Equals(boxedStr));
        Console.WriteLine("obj d eq null=" + h1.Equals(boxedNull!));
        Console.WriteLine("obj d nan eq nan=" + double.NaN.Equals((object)double.NaN));
        Console.WriteLine("obj f eq f=" + g1.Equals((object)2.5f));
        Console.WriteLine("obj f eq d=" + g1.Equals((object)2.5));
    }
}
