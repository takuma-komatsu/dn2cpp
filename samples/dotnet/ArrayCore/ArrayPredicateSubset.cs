using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace ArrayPredicateSubset
{
    // Array's delegate-driven generics: Find, FindLast, FindAll, the three FindIndex and
    // three FindLastIndex overloads, Exists, TrueForAll, ConvertAll, ForEach and
    // AsReadOnly. System.Array is intrinsic-mapped, so every call site is intercepted;
    // these members delegate to their real CoreLib bodies, which also call each other
    // (Exists -> FindIndex -> the ranged FindIndex). The element types span a primitive,
    // a string (the shared reference instantiation), a struct and a class. The fault rows
    // pin .NET's argument order and messages: which of array/match/startIndex/count is
    // reported first differs between FindIndex and FindLastIndex, and FindLastIndex
    // accepts startIndex -1 only on an empty array.
    internal static class Program
    {
        private struct Pt
        {
            public int X, Y;
            public Pt(int x, int y) { X = x; Y = y; }
            public override string ToString() => "(" + X + "," + Y + ")";
        }

        private sealed class Item
        {
            public readonly string Name;
            public readonly int Rank;
            public Item(string name, int rank) { Name = name; Rank = rank; }
            public override string ToString() => Name + "#" + Rank;
        }

        private static string Show(object o)
        {
            if (o is null)
                return "null";
            if (o is string s)
                return "\"" + s + "\"";
            if (o is System.Collections.IEnumerable e)
            {
                var sb = new StringBuilder("[");
                bool first = true;
                foreach (object x in e)
                {
                    if (!first)
                        sb.Append(',');
                    sb.Append(x is null ? "null" : x.ToString());
                    first = false;
                }
                return sb.Append(']').ToString();
            }
            return o.ToString();
        }

        // Struct results format through a typed ToString call, so these rows assert the
        // Array members rather than boxed-struct formatting.
        private static string Fmt(Pt[] a)
        {
            var sb = new StringBuilder("[");
            for (int i = 0; i < a.Length; i++)
                sb.Append(i == 0 ? "" : ",").Append(a[i].ToString());
            return sb.Append(']').ToString();
        }

        private static void E(string tag, Func<object> f)
        {
            try
            {
                Console.WriteLine(tag + ": " + Show(f()));
            }
            catch (Exception ex)
            {
                Console.WriteLine(tag + ": " + ex.GetType().Name + ": " + ex.Message);
            }
        }

        private static bool IsEven(int v) => v % 2 == 0;

        // Generic callers: a reference-type T reaches the members from a shared body.
        private static T FirstMatch<T>(T[] a, Predicate<T> p) => Array.Find(a, p);

        private static int CountMatches<T>(T[] a, Predicate<T> p) => Array.FindAll(a, p).Length;

        private static bool AnyVia<T>(T[] a, Predicate<T> p)
        {
            Func<T[], Predicate<T>, bool> exists = Array.Exists;
            return exists(a, p);
        }

        internal static void Run()
        {
            Console.WriteLine("== array predicate family ==");
            int[] nums = { 5, 12, 7, 20, 3, 12, 8 };
            int[] none = new int[0];
            string[] words = { "alpha", null, "beta", "gamma", "delta" };
            Pt[] pts = { new Pt(1, 2), new Pt(3, 4), new Pt(5, 6), new Pt(3, 9) };
            Item[] items = { new Item("ann", 3), new Item("bob", 1), new Item("cy", 3) };

            E("find-int", () => Array.Find(nums, v => v > 10));
            E("find-int-none", () => Array.Find(nums, v => v > 100));
            E("find-empty", () => Array.Find(none, v => true));
            E("find-string", () => Array.Find(words, w => w != null && w.StartsWith("g")));
            E("find-string-none", () => Array.Find(words, w => w == "zeta"));
            E("find-struct", () => Array.Find(pts, p => p.X == 3).ToString());
            E("find-struct-none", () => Array.Find(pts, p => p.X == 7).ToString());
            E("find-class", () => Array.Find(items, it => it.Rank == 3));
            E("findlast-int", () => Array.FindLast(nums, v => v > 10));
            E("findlast-string", () => Array.FindLast(words, w => w != null && w.Contains('a')));
            E("findlast-struct", () => Array.FindLast(pts, p => p.X == 3).ToString());
            E("findlast-class", () => Array.FindLast(items, it => it.Rank == 3));
            E("findlast-empty", () => Array.FindLast(new string[0], w => true));

            E("findall-int", () => Array.FindAll(nums, IsEven));
            E("findall-int-type", () => Array.FindAll(nums, IsEven).GetType() == typeof(int[]));
            E("findall-none", () => Array.FindAll(nums, v => v < 0).Length);
            E("findall-string", () => Array.FindAll(words, w => w is null || w.Length == 5));
            E("findall-struct", () => Fmt(Array.FindAll(pts, p => p.X == 3)));
            E("findall-class", () => Array.FindAll(items, it => it.Rank == 3));

            E("findindex", () => Array.FindIndex(nums, v => v == 12));
            E("findindex-start", () => Array.FindIndex(nums, 2, v => v == 12));
            E("findindex-range", () => Array.FindIndex(nums, 2, 3, v => v == 12));
            E("findindex-range-miss", () => Array.FindIndex(nums, 2, 2, v => v == 3));
            E("findindex-at-end", () => Array.FindIndex(nums, nums.Length, v => true));
            E("findindex-string", () => Array.FindIndex(words, w => w is null));
            E("findlastindex", () => Array.FindLastIndex(nums, v => v == 12));
            E("findlastindex-start", () => Array.FindLastIndex(nums, 4, v => v == 12));
            E("findlastindex-range", () => Array.FindLastIndex(nums, 4, 3, v => v == 12));
            E("findlastindex-range-miss", () => Array.FindLastIndex(nums, 4, 2, v => v == 5));
            E("findlastindex-empty", () => Array.FindLastIndex(none, v => true));
            E("findlastindex-empty-start", () => Array.FindLastIndex(none, -1, v => true));
            E("findlastindex-struct", () => Array.FindLastIndex(pts, p => p.X == 3));

            E("exists", () => Array.Exists(nums, v => v == 20));
            E("exists-miss", () => Array.Exists(nums, v => v == 21));
            E("exists-string", () => Array.Exists(words, w => w is null));
            E("exists-empty", () => Array.Exists(none, v => true));
            E("trueforall", () => Array.TrueForAll(nums, v => v > 0));
            E("trueforall-miss", () => Array.TrueForAll(nums, IsEven));
            E("trueforall-empty", () => Array.TrueForAll(none, v => false));
            E("trueforall-class", () => Array.TrueForAll(items, it => it.Name.Length >= 2));

            E("convertall-int-string", () => Array.ConvertAll(nums, v => "n" + v));
            E("convertall-string-int", () => Array.ConvertAll(words, w => w is null ? -1 : w.Length));
            E("convertall-struct-int", () => Array.ConvertAll(pts, p => p.X * 10 + p.Y));
            E("convertall-int-struct", () => Fmt(Array.ConvertAll(new[] { 1, 2 }, v => new Pt(v, -v))));
            E("convertall-class-string", () => Array.ConvertAll(items, it => it.Name));
            E("convertall-type", () => Array.ConvertAll(nums, v => (long)v).GetType() == typeof(long[]));
            E("convertall-empty", () => Array.ConvertAll(none, v => v.ToString()).Length);

            var trace = new StringBuilder();
            Array.ForEach(nums, v => trace.Append(v).Append(';'));
            Console.WriteLine("foreach-int: " + trace);
            trace.Clear();
            Array.ForEach(words, w => trace.Append(w ?? "<null>").Append(';'));
            Console.WriteLine("foreach-string: " + trace);
            int sum = 0;
            Array.ForEach(pts, p => sum += p.X);
            Console.WriteLine("foreach-struct: " + sum);
            Array.ForEach(none, v => sum = -1);
            Console.WriteLine("foreach-empty: " + sum);

            int[] live = { 4, 5, 6 };
            ReadOnlyCollection<int> ro = Array.AsReadOnly(live);
            live[1] = 50;
            Console.WriteLine("asreadonly-int: " + ro.Count + " " + ro[1] + " " + Show(ro));
            Console.WriteLine("asreadonly-contains: " + ro.Contains(6) + " " + ro.IndexOf(50));
            Console.WriteLine("asreadonly-isreadonly: " + ((ICollection<int>)ro).IsReadOnly);
            ReadOnlyCollection<string> ros = Array.AsReadOnly(words);
            Console.WriteLine("asreadonly-string: " + ros.Count + " " + Show(ros[0]) + " " + Show(ros[1]));
            Console.WriteLine("asreadonly-empty-shared: "
                + ReferenceEquals(Array.AsReadOnly(new int[0]), Array.AsReadOnly(new int[0])));
            E("asreadonly-add", () => { ((IList<int>)ro).Add(1); return "added"; });

            // Scanning stops at the first match, and a throwing predicate unwinds out.
            int calls = 0;
            Array.FindIndex(nums, v => { calls++; return v == 7; });
            Console.WriteLine("findindex-calls: " + calls);
            calls = 0;
            Array.FindLastIndex(nums, v => { calls++; return v == 20; });
            Console.WriteLine("findlastindex-calls: " + calls);
            calls = 0;
            E("predicate-throws", () => Array.Find(nums, v =>
            {
                calls++;
                if (v == 7)
                    throw new InvalidOperationException("stop at " + v);
                return false;
            }));
            Console.WriteLine("predicate-throws-calls: " + calls);

            // Method groups name the members' own bodies.
            Func<int[], Predicate<int>, bool> exists = Array.Exists;
            Func<string[], Predicate<string>, int> findIndex = Array.FindIndex;
            Func<string[], ReadOnlyCollection<string>> asReadOnly = Array.AsReadOnly;
            Console.WriteLine("group-exists: " + exists(nums, v => v == 3));
            Console.WriteLine("group-findindex: " + findIndex(words, w => w == "gamma"));
            Console.WriteLine("group-asreadonly: " + asReadOnly(words).Count);
            Console.WriteLine("generic-find: " + FirstMatch(words, w => w == "beta") + " "
                + FirstMatch(items, it => it.Rank == 1) + " " + FirstMatch(nums, v => v > 15));
            Console.WriteLine("generic-findall: " + CountMatches(words, w => w != null) + " "
                + CountMatches(items, it => it.Rank == 3) + " " + CountMatches(nums, IsEven));
            Console.WriteLine("generic-group: " + AnyVia(words, w => w is null) + " "
                + AnyVia(items, it => it.Rank > 5) + " " + AnyVia(nums, v => v == 8));

            Console.WriteLine("== array predicate faults ==");
            int[] nul = null;
            Predicate<int> nulMatch = null;
            E("find-null-array", () => Array.Find(nul, v => true));
            E("find-null-match", () => Array.Find(nums, nulMatch));
            E("find-null-both", () => Array.Find(nul, nulMatch));
            E("findlast-null-array", () => Array.FindLast(nul, v => true));
            E("findlast-null-match", () => Array.FindLast(nums, nulMatch));
            E("findall-null-array", () => Array.FindAll(nul, v => true));
            E("findall-null-match", () => Array.FindAll(nums, nulMatch));
            E("findindex-null-array", () => Array.FindIndex(nul, v => true));
            E("findindex-null-match", () => Array.FindIndex(nums, nulMatch));
            E("findindex2-null-array", () => Array.FindIndex(nul, 0, v => true));
            E("findindex3-null-array", () => Array.FindIndex(nul, 0, 0, v => true));
            E("findindex-neg-start", () => Array.FindIndex(nums, -1, v => true));
            E("findindex-start-past-end", () => Array.FindIndex(nums, nums.Length + 1, v => true));
            E("findindex-neg-count", () => Array.FindIndex(nums, 0, -1, v => true));
            E("findindex-count-past-end", () => Array.FindIndex(nums, 5, 3, v => true));
            E("findindex-count-overflow", () => Array.FindIndex(nums, 1, int.MaxValue, v => true));
            E("findindex-bad-start-null-match", () => Array.FindIndex(nums, -1, nulMatch));
            E("findindex-range-null-match", () => Array.FindIndex(nums, 0, 1, nulMatch));
            E("findlastindex-null-array", () => Array.FindLastIndex(nul, v => true));
            E("findlastindex-null-match", () => Array.FindLastIndex(nums, nulMatch));
            E("findlastindex2-null-array", () => Array.FindLastIndex(nul, 0, v => true));
            E("findlastindex3-null-array", () => Array.FindLastIndex(nul, 0, 0, v => true));
            E("findlastindex-bad-start-null-match", () => Array.FindLastIndex(nums, -5, nulMatch));
            E("findlastindex-start-at-length", () => Array.FindLastIndex(nums, nums.Length, v => true));
            E("findlastindex-neg-start", () => Array.FindLastIndex(nums, -1, v => true));
            E("findlastindex-empty-start0", () => Array.FindLastIndex(none, 0, v => true));
            E("findlastindex-neg-count", () => Array.FindLastIndex(nums, 3, -1, v => true));
            E("findlastindex-count-past-start", () => Array.FindLastIndex(nums, 3, 5, v => true));
            E("findlastindex-empty-count", () => Array.FindLastIndex(none, -1, 1, v => true));
            E("exists-null-array", () => Array.Exists(nul, v => true));
            E("exists-null-match", () => Array.Exists(nums, nulMatch));
            E("trueforall-null-array", () => Array.TrueForAll(nul, v => true));
            E("trueforall-null-match", () => Array.TrueForAll(nums, nulMatch));
            E("convertall-null-array", () => Array.ConvertAll(nul, v => v.ToString()));
            E("convertall-null-converter", () => Array.ConvertAll(nums, (Converter<int, string>)null));
            E("foreach-null-array", () => { Array.ForEach(nul, v => { }); return "ran"; });
            E("foreach-null-action", () => { Array.ForEach(nums, null); return "ran"; });
            E("asreadonly-null", () => Array.AsReadOnly(nul));
            E("find-string-null-array", () => Array.Find((string[])null, w => true));
        }
    }
}
