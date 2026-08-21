#nullable enable
// A user-defined `class MyCmp : EqualityComparer<T>` — the way the BCL docs tell you to
// write a comparer. EqualityComparer<T> is intrinsic-mapped to an opaque Dn2CppObject*
// whose Default is an opaque identity, so a subclass needs its base .ctor mapped to be
// constructible at all; the load-bearing half is that its Equals/GetHashCode reach real
// code through three routes that must all agree with real .NET:
//
//   * a DIRECT call on the subclass type (normal vtable dispatch),
//   * an UPCAST call through the `EqualityComparer<T>` static type (the intrinsic arm,
//     which must dispatch the real receiver through its IEqualityComparer<T> interface
//     rather than silently answering with T's DEFAULT equality),
//   * a Dictionary/HashSet KEY path (the _comparer callvirt through IEqualityComparer<T>).
//
// Covered for a REFERENCE element type (string) and a VALUE element type (a struct), with
// the Default comparer used alongside to prove its intrinsic path still answers default
// equality. Every printed value is comparer-controlled or a Count/lookup, so it is
// deterministic and diffs exactly vs real .NET (no runtime-seeded string hashes).
using System;
using System.Collections.Generic;

namespace CustomComparerSubset;

// Reference element type: two strings are "equal" iff same length; hash is the length.
sealed class LengthComparer : EqualityComparer<string>
{
    public override bool Equals(string? a, string? b) => (a?.Length ?? -1) == (b?.Length ?? -1);
    public override int GetHashCode(string s) => s?.Length ?? -1;
}

// Reference element type, case-insensitive first char — a second comparer over the same T,
// to prove the dispatch keys on the receiver object, not on T.
sealed class FirstCharComparer : EqualityComparer<string>
{
    public override bool Equals(string? a, string? b) =>
        char.ToUpperInvariant((a ?? "\0")[0]) == char.ToUpperInvariant((b ?? "\0")[0]);
    public override int GetHashCode(string s) => char.ToUpperInvariant((s ?? "\0")[0]);
}

struct Cell
{
    public int Species;
    public int X;
    public int Y;
}

// Value element type: two cells are "equal" iff same Species (position ignored).
sealed class SpeciesComparer : EqualityComparer<Cell>
{
    public override bool Equals(Cell a, Cell b) => a.Species == b.Species;
    public override int GetHashCode(Cell c) => c.Species;
}

class Program
{
    // The receiver arrives as an EqualityComparer<T> ARGUMENT (an ldarg, not a newobj) —
    // the upcast path, and the one a Default passed the same way must survive too.
    private static string UpEq(EqualityComparer<string> c, string a, string b) => c.Equals(a, b).ToString();
    private static string UpHash(EqualityComparer<string> c, string s) => c.GetHashCode(s).ToString();
    private static string UpEqCell(EqualityComparer<Cell> c, Cell a, Cell b) => c.Equals(a, b).ToString();

    internal static void __GateEntry()
    {
        // ---- reference element type, direct calls on the subclass ----
        var len = new LengthComparer();
        Console.WriteLine("len abc==xyz=" + len.Equals("abc", "xyz"));   // True (same length)
        Console.WriteLine("len abc==wxyz=" + len.Equals("abc", "wxyz")); // False
        Console.WriteLine("len hash abc=" + len.GetHashCode("abc"));     // 3

        // ---- reference element type, UPCAST through EqualityComparer<string> ----
        Console.WriteLine("up len abc==xyz=" + UpEq(len, "abc", "xyz")); // True
        Console.WriteLine("up len hash=" + UpHash(len, "zzzzz"));        // 5

        // ---- the Default comparer, upcast the SAME way: intrinsic path still default ----
        var def = EqualityComparer<string>.Default;
        Console.WriteLine("up def a==a=" + UpEq(def, "a", "a"));         // True
        Console.WriteLine("up def a==b=" + UpEq(def, "a", "b"));         // False
        Console.WriteLine("def direct=" + def.Equals("abc", "abc"));     // True

        // ---- reference element type as a Dictionary key ----
        var byLen = new Dictionary<string, int>(new LengthComparer());
        byLen["abc"] = 1;         // length 3
        byLen["wxyz"] = 2;        // length 4
        Console.WriteLine("byLen[xyz]=" + byLen["xyz"]);          // 1 (same length as abc)
        Console.WriteLine("byLen has qqqq=" + byLen.ContainsKey("qqqq")); // True (length 4)
        Console.WriteLine("byLen has q=" + byLen.ContainsKey("q"));       // False (length 1)
        Console.WriteLine("byLen count=" + byLen.Count);                  // 2
        byLen["de"] = 3;          // length 2, new
        byLen["fg"] = 4;          // length 2, overwrite de's slot
        Console.WriteLine("byLen count2=" + byLen.Count);                 // 3
        Console.WriteLine("byLen[hi]=" + byLen["hi"]);                    // 4

        // ---- reference element type as a HashSet ----
        var lenSet = new HashSet<string>(new LengthComparer());
        lenSet.Add("a"); lenSet.Add("bb"); lenSet.Add("cc"); lenSet.Add("ddd");
        Console.WriteLine("lenSet count=" + lenSet.Count);       // 3 (lengths 1,2,3)
        Console.WriteLine("lenSet has ee=" + lenSet.Contains("ee")); // True (length 2)
        Console.WriteLine("lenSet has eeee=" + lenSet.Contains("eeee")); // False

        // ---- a DIFFERENT comparer over the same T, in its own dictionary ----
        var byChar = new Dictionary<string, int>(new FirstCharComparer());
        byChar["apple"] = 10;
        byChar["banana"] = 20;
        Console.WriteLine("byChar[Avocado]=" + byChar["Avocado"]); // 10 (first char A/a)
        Console.WriteLine("byChar count=" + byChar.Count);         // 2
        Console.WriteLine("up char hash=" + new FirstCharComparer().GetHashCode("apple")); // 65 ('A')

        // ---- value element type: direct, upcast, and as Dictionary/HashSet keys ----
        var sp = new SpeciesComparer();
        var c1 = new Cell { Species = 1, X = 2, Y = 3 };
        var c1b = new Cell { Species = 1, X = 9, Y = 9 };
        var c2 = new Cell { Species = 2, X = 0, Y = 0 };
        Console.WriteLine("sp c1==c1b=" + sp.Equals(c1, c1b));    // True (same species)
        Console.WriteLine("sp c1==c2=" + sp.Equals(c1, c2));      // False
        Console.WriteLine("sp hash c1=" + sp.GetHashCode(c1));    // 1
        Console.WriteLine("up sp c1==c1b=" + UpEqCell(sp, c1, c1b)); // True

        var byCell = new Dictionary<Cell, string>(new SpeciesComparer());
        byCell[c1] = "first";
        byCell[c1b] = "overwrite";      // same species -> same key
        byCell[c2] = "second";
        Console.WriteLine("byCell count=" + byCell.Count);        // 2
        Console.WriteLine("byCell[c1]=" + byCell[c1]);            // overwrite
        Console.WriteLine("byCell has species1=" + byCell.ContainsKey(new Cell { Species = 1, X = 42, Y = 42 })); // True

        var cellSet = new HashSet<Cell>(new SpeciesComparer());
        cellSet.Add(c1); cellSet.Add(c1b); cellSet.Add(c2);
        Console.WriteLine("cellSet count=" + cellSet.Count);     // 2
        Console.WriteLine("cellSet has species2=" + cellSet.Contains(new Cell { Species = 2, X = 7, Y = 7 })); // True

        // ---- the value-type Default comparer still works over a struct ----
        var cellDef = EqualityComparer<Cell>.Default;
        Console.WriteLine("cellDef c1==c1=" + cellDef.Equals(c1, c1));   // True (all fields)
        Console.WriteLine("cellDef c1==c1b=" + cellDef.Equals(c1, c1b)); // False (X/Y differ)
    }
}
