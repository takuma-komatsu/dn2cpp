#nullable enable
using System;
using System.Collections.Generic;

// A case-insensitive string comparer using only supported ops (manual ASCII
// case folding — StringComparer.OrdinalIgnoreCase pulls unsupported culture IL).
namespace CustomComparerSubset;

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

class ByLength : IEqualityComparer<string>
{
    public bool Equals(string? x, string? y) => (x?.Length ?? -1) == (y?.Length ?? -1);
    public int GetHashCode(string s) => s.Length;
}

class Program
{internal static void __GateEntry()
    {
        // Dictionary keyed case-insensitively via a custom comparer.
        var ci = new Dictionary<string, int>(new CaseInsensitive());
        ci["Hello"] = 1;
        ci["HELLO"] = 2;            // overwrites "Hello" (case-insensitive)
        ci["world"] = 3;
        Console.WriteLine(ci.Count + " " + ci["hello"] + " " + ci["WORLD"]);

        // HashSet keyed by length: only distinct lengths survive.
        var lens = new HashSet<string>(new ByLength());
        foreach (string w in new[] { "a", "bb", "cc", "ddd", "e" })
            lens.Add(w);
        Console.WriteLine(lens.Count + " " + lens.Contains("zz") + " " + lens.Contains("zzzz"));

        // Default dictionary stays content-keyed (no comparer) — regression guard.
        var def = new Dictionary<string, int>();
        def["Hello"] = 1;
        def["HELLO"] = 2;          // distinct from "Hello"
        Console.WriteLine(def.Count + " " + def.ContainsKey("hello"));

        // Custom-comparer dictionary lookups: ContainsKey / TryGetValue.
        var byLen = new Dictionary<string, string>(new ByLength());
        byLen["ab"] = "two";
        byLen["xyz"] = "three";
        Console.WriteLine(byLen.ContainsKey("qq") + " " + byLen.TryGetValue("???", out string? v) + " " + v);
    }
}
