#nullable enable
// Structural equality/hash of a value type that overrides NEITHER Equals(object) nor
// GetHashCode — what System.ValueType answers for. Its real IL is a QCall extern over the
// runtime's MethodTable, so the transpiler synthesizes the field walk instead (the same
// thing the C# compiler does for a record).
//
// Covered here:
//   * KeyValuePair<K,V> — the BCL's own override-less struct — as a HashSet/Dictionary key,
//     and a plain user struct likewise (the key path: EqualityComparer<T>.Default with no
//     IEquatable<T> to devirtualize to);
//   * a float/double field, where a memcmp would be WRONG and real .NET also walks fields:
//     NaN equals NaN, and +0.0 equals -0.0 despite differing bits;
//   * a string/reference field, compared through the Object virtual (so a string field
//     compares by CONTENT, and a class field by its own override — or by reference when it
//     has none), which is what boxing each field and calling Object.Equals means;
//   * a nested struct field whose own type overrides nothing (the walk recurses) and one
//     whose type implements ONLY IEquatable<T> — ValueType.Equals does NOT call that typed
//     override, it walks the inner struct's fields, and the struct here is built so the two
//     answers disagree;
//   * a Nullable<U> field — the one type whose box is NOT a box of itself (the CLR turns it
//     into null or a box of U), so the walk compares two null-or-U boxes;
//   * a field whose own Equals(object) reads the box through `unbox; ldfld` (the
//     pre-pattern-matching style the BCL is written in), plus that override called directly;
//   * boxed dispatch: `object o = kvp; o.Equals(other)`. A null equals slot means reference
//     equality, and two boxes of one value are two allocations — so this silently answered
//     false before the slot carried the walk.
//
// No raw hash VALUE is printed: real .NET's ValueType hash is not a specified number (and
// dn2cpp's is Roslyn's record fold), so only the contract is asserted — equal values hash
// equal. Dictionary/HashSet enumeration order is hash-dependent, so only Count/Contains/
// TryGetValue are printed.
//
// Real System.Private.CoreLib (-r), run vs .NET.
using System;
using System.Collections.Generic;
using System.Threading;
namespace ValueStructKeySubset;

// Nothing overridden: every comparison of one of these is ValueType.Equals.
struct Plain
{
    public int A;
    public string? S;
    public Plain(int a, string? s) { A = a; S = s; }
}

// A float field: real .NET's memcmp fast path declines a type with one, precisely because
// bits and value disagree on NaN and on -0.0.
struct Measured
{
    public double D;
    public float F;
    public Measured(double d, float f) { D = d; F = f; }
}

// Implements IEquatable<T> and nothing else — and says every instance equals every other.
// ValueType.Equals must NOT call this: boxing a field calls the OBJECT virtual, which for
// this type is ValueType.Equals again, i.e. a walk of V. The lie is the point.
struct Loose : IEquatable<Loose>
{
    public int V;
    public Loose(int v) { V = v; }
    public bool Equals(Loose other) => true;
}

// Nested: one field walks, one field is the liar above.
struct Nest
{
    public Plain Inner;
    public Loose Loose;
    public Nest(Plain inner, Loose loose) { Inner = inner; Loose = loose; }
}

// A class field with a real override, and one with none (reference equality).
sealed class Named
{
    public readonly string N;
    public Named(string n) { N = n; }
    public override bool Equals(object? o) => o is Named x && x.N == N;
    public override int GetHashCode() => N.Length;
}

sealed class Opaque
{
    public int Q;
}

struct Holder
{
    public Named? Named;
    public Opaque? Opaque;
    public Holder(Named? n, Opaque? o) { Named = n; Opaque = o; }
}

// A hand-written Equals(object) in the pre-pattern-matching style the BCL is full of
// (System.Reflection.Metadata's handle structs, for instance): `((T)obj).Field` reads a
// field of the box WITHOUT copying it out, which Roslyn lowers to `unbox; ldfld` — the raw
// unbox opcode, not unbox.any. A field walk that dispatches this override is what first
// reaches such a body.
struct Row
{
    public int Id;
    public Row(int id) { Id = id; }
    public override bool Equals(object? o) => o is Row && ((Row)o).Id == Id;
    public override int GetHashCode() => Id;
}

// Its walk boxes the Row field and calls the override above.
struct Keyed
{
    public Row Row;
    public string? Tag;
    public Keyed(Row r, string? t) { Row = r; Tag = t; }
}

// Nullable fields over the intrinsic value types (Decimal/TimeSpan/DateTime/…). The walk
// dispatches Nullable<T>.Equals(object), whose body hands `other` on as an OBJECT rather
// than unwrapping it — so the value type's Object-virtual Equals(object) runs, not its
// typed Equals(T). Fixed values only: DateTime.Now is not deterministic.
struct Stamped
{
    public DateTime? When;
    public decimal? Amount;
    public TimeSpan? Span;
    public Stamped(DateTime? w, decimal? a, TimeSpan? s) { When = w; Amount = a; Span = s; }
}

// Mirrors library option structs whose compiler-provided ValueType.GetHashCode walks a
// CancellationToken field. The token's emitted payload is intrinsic even though the outer
// struct is ordinary managed layout.
struct CancellationStamped
{
    public int Iterations;
    public CancellationToken CancellationToken;
    public CancellationStamped(int iterations, CancellationToken cancellationToken)
    {
        Iterations = iterations;
        CancellationToken = cancellationToken;
    }
}

internal static class Program
{
    internal static void __GateEntry()
    {
        // Build the strings at run time so the comparison is content, not interning.
        string ab = string.Concat("a", "b");
        string ab2 = new string(new[] { 'a', 'b' });

        // ---- KeyValuePair<K,V>: the BCL's override-less struct, as a key ----
        var kvSet = new HashSet<KeyValuePair<int, string>>
        {
            new KeyValuePair<int, string>(1, ab),
            new KeyValuePair<int, string>(1, ab2),   // equal by value: not added twice
            new KeyValuePair<int, string>(2, "z"),
        };
        Console.WriteLine("kv setcount=" + kvSet.Count);
        Console.WriteLine("kv contains=" + kvSet.Contains(new KeyValuePair<int, string>(1, "ab")));
        Console.WriteLine("kv missing=" + kvSet.Contains(new KeyValuePair<int, string>(1, "zz")));

        var kvDict = new Dictionary<KeyValuePair<int, string>, int>();
        kvDict[new KeyValuePair<int, string>(7, ab)] = 70;
        kvDict[new KeyValuePair<int, string>(7, ab2)] = 71;   // same key: overwrites
        Console.WriteLine("kv dictcount=" + kvDict.Count);
        Console.WriteLine("kv lookup=" + kvDict[new KeyValuePair<int, string>(7, "ab")]);

        var kvA = new KeyValuePair<int, string>(3, ab);
        var kvB = new KeyValuePair<int, string>(3, ab2);
        Console.WriteLine("kv eq=" + kvA.Equals(kvB));
        Console.WriteLine("kv hash eq=" + (kvA.GetHashCode() == kvB.GetHashCode()));

        // ---- a plain user struct ----
        var pSet = new HashSet<Plain> { new Plain(1, ab), new Plain(1, ab2), new Plain(2, null) };
        Console.WriteLine("plain setcount=" + pSet.Count);
        Console.WriteLine("plain contains=" + pSet.Contains(new Plain(2, null)));
        Console.WriteLine("plain eq=" + new Plain(1, ab).Equals(new Plain(1, ab2)));
        Console.WriteLine("plain ne=" + new Plain(1, ab).Equals(new Plain(1, "ac")));
        Console.WriteLine("plain null eq=" + new Plain(2, null).Equals(new Plain(2, null)));
        Console.WriteLine("plain null ne=" + new Plain(2, null).Equals(new Plain(2, ab)));
        Console.WriteLine("plain hash eq=" + (new Plain(1, ab).GetHashCode() == new Plain(1, ab2).GetHashCode()));

        var pList = new List<Plain> { new Plain(4, "d"), new Plain(5, "e") };
        Console.WriteLine("plain list contains=" + pList.Contains(new Plain(5, "e")));
        Console.WriteLine("plain list indexof=" + pList.IndexOf(new Plain(5, "e")));

        // The third Object-rooted virtual: ValueType.ToString() IS the type's full name,
        // which is exactly what an unwired tostring slot already answers — so it needs no
        // synthesis, only the box-and-dispatch the constrained call now does.
        Console.WriteLine("plain tostring=" + new Plain(1, ab).ToString());
        Console.WriteLine("plain boxed tostring=" + ((object)new Plain(1, ab)).ToString());

        // ---- float/double fields: value semantics, not bit semantics ----
        var nan = new Measured(double.NaN, float.NaN);
        Console.WriteLine("nan self eq=" + nan.Equals(new Measured(double.NaN, float.NaN)));
        Console.WriteLine("zero eq=" + new Measured(0.0, 0.0f).Equals(new Measured(-0.0, -0.0f)));
        Console.WriteLine("zero hash eq="
            + (new Measured(0.0, 0.0f).GetHashCode() == new Measured(-0.0, -0.0f).GetHashCode()));
        Console.WriteLine("meas ne=" + new Measured(1.5, 2.5f).Equals(new Measured(1.5, 2.6f)));
        var mSet = new HashSet<Measured> { new Measured(double.NaN, 1f), new Measured(double.NaN, 1f) };
        Console.WriteLine("nan setcount=" + mSet.Count);
        Console.WriteLine("nan setcontains=" + mSet.Contains(new Measured(double.NaN, 1f)));

        // ---- nested, and the IEquatable liar ----
        var n1 = new Nest(new Plain(1, ab), new Loose(10));
        var n2 = new Nest(new Plain(1, ab2), new Loose(10));
        var n3 = new Nest(new Plain(1, ab), new Loose(11));
        Console.WriteLine("nest eq=" + n1.Equals(n2));
        // The typed Loose.Equals says true for ANY pair. ValueType.Equals ignores it and
        // walks V, so 10 != 11.
        Console.WriteLine("nest liar ne=" + n1.Equals(n3));
        Console.WriteLine("nest hash eq=" + (n1.GetHashCode() == n2.GetHashCode()));
        // ...while the typed override IS what a direct Loose-to-Loose compare uses.
        Console.WriteLine("loose typed=" + new Loose(10).Equals(new Loose(11)));

        // ---- reference fields: the field's own Object virtual, or reference equality ----
        var op = new Opaque();
        Console.WriteLine("holder named eq="
            + new Holder(new Named("x"), op).Equals(new Holder(new Named("x"), op)));
        Console.WriteLine("holder named ne="
            + new Holder(new Named("x"), op).Equals(new Holder(new Named("y"), op)));
        Console.WriteLine("holder opaque ne="
            + new Holder(new Named("x"), op).Equals(new Holder(new Named("x"), new Opaque())));
        Console.WriteLine("holder null eq=" + new Holder(null, null).Equals(new Holder(null, null)));

        // ---- a field whose override reads the box through `unbox; ldfld` ----
        Console.WriteLine("row eq=" + new Row(3).Equals((object)new Row(3)));
        Console.WriteLine("row ne=" + new Row(3).Equals((object)new Row(4)));
        Console.WriteLine("row vs other=" + new Row(3).Equals((object)"3"));
        Console.WriteLine("row vs null=" + new Row(3).Equals(null!));
        Console.WriteLine("row hash eq=" + (new Row(3).GetHashCode() == new Row(3).GetHashCode()));
        Console.WriteLine("keyed eq=" + new Keyed(new Row(3), ab).Equals(new Keyed(new Row(3), ab2)));
        Console.WriteLine("keyed ne=" + new Keyed(new Row(3), ab).Equals(new Keyed(new Row(4), ab)));
        var kSet = new HashSet<Keyed> { new Keyed(new Row(1), ab), new Keyed(new Row(1), ab2) };
        Console.WriteLine("keyed setcount=" + kSet.Count);

        // ---- Nullable fields over intrinsic value types ----
        var d1 = new DateTime(2024, 3, 1, 12, 0, 0);
        var d2 = new DateTime(2024, 3, 1, 12, 0, 0);
        var s1 = new Stamped(d1, 1.5m, TimeSpan.FromMinutes(3));
        var s2 = new Stamped(d2, 1.5m, TimeSpan.FromMinutes(3));
        var s3 = new Stamped(d1, 1.6m, TimeSpan.FromMinutes(3));
        var s4 = new Stamped(null, null, null);
        Console.WriteLine("stamped eq=" + s1.Equals(s2));
        Console.WriteLine("stamped ne=" + s1.Equals(s3));
        Console.WriteLine("stamped null eq=" + s4.Equals(new Stamped(null, null, null)));
        Console.WriteLine("stamped null ne=" + s4.Equals(s1));
        Console.WriteLine("stamped hash eq=" + (s1.GetHashCode() == s2.GetHashCode()));
        var stSet = new HashSet<Stamped> { s1, s2, s3 };
        Console.WriteLine("stamped setcount=" + stSet.Count);
        // The Object-virtual Equals(object) of the intrinsic value types themselves — the
        // overload Nullable<T> reaches. A box of another type is never equal.
        Console.WriteLine("dt obj eq=" + d1.Equals((object)d2));
        Console.WriteLine("dt obj ne=" + d1.Equals((object)d1.AddDays(1)));
        Console.WriteLine("dt obj vs str=" + d1.Equals((object)"2024"));
        Console.WriteLine("dec obj eq=" + (1.5m).Equals((object)1.5m));
        Console.WriteLine("dec obj ne=" + (1.5m).Equals((object)1.6m));
        Console.WriteLine("dec obj vs str=" + (1.5m).Equals((object)"1.5"));
        Console.WriteLine("ts obj eq=" + TimeSpan.FromMinutes(3).Equals((object)TimeSpan.FromMinutes(3)));
        Console.WriteLine("dto obj eq="
            + new DateTimeOffset(d1, TimeSpan.Zero).Equals((object)new DateTimeOffset(d2, TimeSpan.Zero)));

        // ...and the route that reaches them: Nullable<X>.Equals(object) forwards `other` on
        // as an object rather than unwrapping it.
        DateTime? q1 = d1, q2 = d2, q3 = null;
        Console.WriteLine("nullable dt eq=" + q1.Equals(q2));
        Console.WriteLine("nullable dt ne=" + q1.Equals(d1.AddDays(1)));
        Console.WriteLine("nullable null eq=" + q3.Equals(null));
        Console.WriteLine("nullable null ne=" + q3.Equals(q1));
        decimal? m1 = 1.5m;
        Console.WriteLine("nullable dec eq=" + m1.Equals(1.5m));

        // ---- boxed dispatch: the type-info equals/gethashcode slots ----
        object boxed = kvA;
        Console.WriteLine("boxed eq=" + boxed.Equals(kvB));
        Console.WriteLine("boxed ne=" + boxed.Equals(new KeyValuePair<int, string>(4, ab)));
        Console.WriteLine("boxed vs other type=" + boxed.Equals("nope"));
        Console.WriteLine("boxed vs null=" + boxed.Equals(null!));
        Console.WriteLine("boxed hash eq=" + (boxed.GetHashCode() == kvB.GetHashCode()));

        object bp1 = new Plain(9, ab);
        object bp2 = new Plain(9, ab2);
        Console.WriteLine("boxed plain eq=" + bp1.Equals(bp2));
        Console.WriteLine("boxed plain hash eq=" + (bp1.GetHashCode() == bp2.GetHashCode()));
        // An object-keyed collection: the boxes flow through dn2cpp_object_equals.
        var objSet = new HashSet<object> { bp1, bp2, new Plain(8, ab) };
        Console.WriteLine("objset count=" + objSet.Count);
        Console.WriteLine("objset contains=" + objSet.Contains(new Plain(8, ab2)));

        // APPENDED LAST: direct GetHashCode reaches the synthesized outer field walk.
        var cancellationSource = new CancellationTokenSource();
        var otherCancellationSource = new CancellationTokenSource();
        var cancellationStamped = new CancellationStamped(3, cancellationSource.Token);
        var cancellationStampedCopy = cancellationStamped;
        var otherCancellationStamped = new CancellationStamped(3, otherCancellationSource.Token);
        Console.WriteLine("cancellation field="
            + $"{cancellationStamped.Equals(cancellationStampedCopy)}"
            + $":{!cancellationStamped.Equals(otherCancellationStamped)}"
            + $":{cancellationStamped.GetHashCode() == cancellationStampedCopy.GetHashCode()}");
    }
}
