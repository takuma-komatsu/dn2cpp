#nullable enable
// System.Tuple's structural equality/order/format trio — the reference Tuple, not
// ValueTuple. Its BCL bodies pass `EqualityComparer<object>.Default` (dn2cpp: the
// opaque identity) into `IStructuralEquatable.GetHashCode/Equals(…, IEqualityComparer)`
// as an OBJECT, and the inner bodies dispatch the comparer through the NON-GENERIC
// System.Collections.IEqualityComparer interface. That dispatch used to read
// nullptr->type and crash — Thrive's PatchMap.Verify (`HashSet<Tuple<Patch,Patch>>.Add`
// → virtual GetHashCode) was the boot blocker. The emitted dispatch now answers a null
// comparer with the boxed-object default ops (dn2cpp_object_gethashcode/_equals) —
// exactly the ObjectEqualityComparer semantics the real Default carries.
//
// Covered here, each diffed exactly vs real .NET:
//   * Tuple.GetHashCode() — only RELATIONSHIPS are printed (equal tuples hash equal),
//     never raw hash values (.NET string hashes are run-seeded);
//   * HashSet<Tuple<R,R>> dedup over a reference element type (the Thrive shape);
//   * Tuple.Equals(object) — reference elements (identity) and value elements (boxed
//     value equality), plus null;
//   * the EXPLICIT IStructuralEquatable calls with EqualityComparer<object>.Default —
//     the sentinel passed as a visible argument;
//   * IComparable.CompareTo via Comparer<object>.Default — NOT the sentinel (a real
//     GenericComparer<object> whose Compare lowers to dn2cpp_object_compare); asserted
//     here so the whole trio stays covered by one section;
//   * Tuple.ToString(), including the arity-8 Rest nesting;
//   * a Dictionary keyed on Tuple (hash + equality through the same comparer path).
using System;
using System.Collections;
using System.Collections.Generic;

namespace TupleStructuralSubset;

// A reference element with NO overrides: equality must be identity, like real .NET's
// ObjectEqualityComparer over a plain class (the Thrive Patch shape).
class Patch
{
    public string Name;
    public Patch(string n) { Name = n; }
    public override string ToString() => "Patch(" + Name + ")";
}

class Program
{
    internal static void __GateEntry()
    {
        // ---- Tuple.GetHashCode: virtual dispatch -> sentinel -> non-generic comparer ----
        var t = new Tuple<string, string>("a", "b");
        Console.WriteLine("tup hash-eq=" + (t.GetHashCode() == new Tuple<string, string>("a", "b").GetHashCode()));
        var ti = new Tuple<int, int>(1, 2);
        Console.WriteLine("tup hash-eq-int=" + (ti.GetHashCode() == new Tuple<int, int>(1, 2).GetHashCode()));

        // ---- HashSet<Tuple<Patch,Patch>> dedup (the Thrive PatchMap.Verify shape) ----
        var p1 = new Patch("p1");
        var p2 = new Patch("p2");
        var set = new HashSet<Tuple<Patch, Patch>>();
        Console.WriteLine("tup add1=" + set.Add(Tuple.Create(p1, p2)));
        Console.WriteLine("tup add2=" + set.Add(Tuple.Create(p1, p2)));   // dup: same references
        Console.WriteLine("tup add3=" + set.Add(Tuple.Create(p2, p1)));   // swapped: distinct
        Console.WriteLine("tup count=" + set.Count);
        Console.WriteLine("tup contains=" + set.Contains(Tuple.Create(p1, p2)));

        // ---- Tuple.Equals(object) ----
        Console.WriteLine("tup eq1=" + t.Equals(new Tuple<string, string>("a", "b")));
        Console.WriteLine("tup eq2=" + t.Equals(new Tuple<string, string>("a", "c")));
        Console.WriteLine("tup eq3=" + t.Equals(null));
        Console.WriteLine("tup eq4=" + ti.Equals(new Tuple<int, int>(1, 2)));

        // ---- explicit IStructuralEquatable with EqualityComparer<object>.Default ----
        Console.WriteLine("tup se=" + ((IStructuralEquatable)t).Equals(
            new Tuple<string, string>("a", "b"), EqualityComparer<object>.Default));
        Console.WriteLine("tup sh-eq=" + (((IStructuralEquatable)t).GetHashCode(EqualityComparer<object>.Default)
            == t.GetHashCode()));

        // ---- IComparable.CompareTo via Comparer<object>.Default ----
        var c1 = new Tuple<int, string>(1, "a");
        var c2 = new Tuple<int, string>(1, "b");
        var c3 = new Tuple<int, string>(2, "a");
        Console.WriteLine("tup cmp1=" + Math.Sign(((IComparable)c1).CompareTo(c2)));
        Console.WriteLine("tup cmp2=" + Math.Sign(((IComparable)c3).CompareTo(c1)));
        Console.WriteLine("tup cmp3=" + Math.Sign(((IComparable)c1).CompareTo(c1)));
        Console.WriteLine("tup ocmp1=" + Math.Sign(Comparer<object>.Default.Compare(1, 2)));
        Console.WriteLine("tup ocmp2=" + Math.Sign(Comparer<object>.Default.Compare("b", "a")));

        // ---- Tuple.ToString, including the arity-8 Rest nesting ----
        Console.WriteLine("tup ts=" + t);
        Console.WriteLine("tup ts2=" + Tuple.Create(1, "x", 2.5).ToString());
        var big = Tuple.Create(1, 2, 3, 4, 5, 6, 7, 8);
        Console.WriteLine("tup big=" + big);
        Console.WriteLine("tup big-hash-eq=" + (big.GetHashCode() == Tuple.Create(1, 2, 3, 4, 5, 6, 7, 8).GetHashCode()));

        // ---- Dictionary keyed on Tuple ----
        var d = new Dictionary<Tuple<int, int>, string>();
        d[Tuple.Create(1, 2)] = "v";
        Console.WriteLine("tup dict=" + d[Tuple.Create(1, 2)]);
    }
}
