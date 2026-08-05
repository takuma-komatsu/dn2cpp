#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

// The LINQ IEqualityComparer<TKey> overloads thread a user comparer into
// the shim's backing HashSet/Dictionary ctor (custom-comparer dispatch).
// Sources are List<T> (a raw T[] dispatched as IEnumerable<T> is a known gap).

// Case-insensitive string comparer using only supported ops (manual ASCII case
// folding — StringComparer.OrdinalIgnoreCase pulls unsupported culture IL).
namespace LinqComparerSubset;

class CaseInsensitive : IEqualityComparer<string>
{
    static int Fold(char c) => (c >= 'A' && c <= 'Z') ? c + 32 : c;
    public bool Equals(string? x, string? y)
    {
        if (x is null || y is null) return ReferenceEquals(x, y);
        if (x.Length != y.Length) return false;
        for (int i = 0; i < x.Length; i++)
            if (Fold(x[i]) != Fold(y[i])) return false;
        return true;
    }
    public int GetHashCode(string s)
    {
        int h = 17;
        foreach (char c in s) h = h * 31 + Fold(c);
        return h;
    }
}

class Program
{internal static void Run()
    {
        var ci = new CaseInsensitive();

        // string.Join over a List<T> needs the list bound to a local first (the
        // call-result boundary), so each materializer lands in a local.

        // Distinct(comparer): "a"/"A" and "b"/"B"/"b" collapse; first appearance wins.
        List<string> ds = new List<string> { "a", "A", "b", "B", "b" };
        List<string> distinct = ds.Distinct(ci).ToList();
        Console.WriteLine(string.Join(",", distinct));

        // DistinctBy(keySelector, comparer): key by first letter, case-insensitively.
        List<string> words = new List<string> { "Apple", "apricot", "Banana", "blueberry", "Cherry" };
        List<string> distinctBy = words.DistinctBy(w => w.Substring(0, 1), ci).ToList();
        Console.WriteLine(string.Join(",", distinctBy));

        // GroupBy(keySelector, comparer): group by first letter (CI). Key is the
        // first-encountered form; groups in first-appearance order.
        List<string> g1 = new List<string>();
        foreach (var g in words.GroupBy(w => w.Substring(0, 1), ci))
            g1.Add(g.Key + ":" + g.Count().ToString());
        Console.WriteLine(string.Join(",", g1));

        // GroupBy(keySelector, elementSelector, comparer): element = length, sum per group.
        List<string> g2 = new List<string>();
        foreach (var g in words.GroupBy(w => w.Substring(0, 1), w => w.Length, ci))
            g2.Add(g.Key + ":" + g.Sum().ToString());
        Console.WriteLine(string.Join(",", g2));

        // GroupBy(keySelector, resultSelector, comparer).
        List<string> g3 = words.GroupBy(
            w => w.Substring(0, 1), (k, grp) => k + "=" + grp.Count().ToString(), ci).ToList();
        Console.WriteLine(string.Join(",", g3));

        // GroupBy(keySelector, elementSelector, resultSelector, comparer).
        List<string> g4 = words.GroupBy(
            w => w.Substring(0, 1), w => w.Length, (k, lens) => k + "#" + lens.Sum().ToString(), ci).ToList();
        Console.WriteLine(string.Join(",", g4));

        // ToHashSet(comparer): only distinct (case-insensitive) survive.
        var hs = new List<string> { "x", "X", "y", "Y", "z" }.ToHashSet(ci);
        Console.WriteLine(hs.Count.ToString() + " " + hs.Contains("Z").ToString() + " " + hs.Contains("q").ToString());

        // ToDictionary(keySelector, comparer): keep the word as value, CI lookup.
        var td1 = new List<string> { "one", "two", "three" }.ToDictionary(w => w, ci);
        Console.WriteLine(td1.Count.ToString() + " " + td1["ONE"] + " " + td1["Two"]);

        // ToDictionary(keySelector, elementSelector, comparer): value = length.
        var td2 = new List<string> { "one", "two", "three" }.ToDictionary(w => w, w => w.Length, ci);
        Console.WriteLine(td2.Count.ToString() + " " + td2["ONE"].ToString() + " " + td2["THREE"].ToString());

        // Default (no comparer) stays content-keyed — regression guard.
        var def = words.GroupBy(w => w.Substring(0, 1));
        Console.WriteLine(def.Count().ToString());
    }
}
