#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

// (nullable slice): the LINQ Sum/Average nullable overloads. Compiled against
// the real System.Linq, which the gate also passes via -r so every operator
// transpiles from the real BCL IL. Null elements are skipped; Sum
// over an empty/all-null sequence is 0 (never null), Average over one is null. The
// overloads ride dn2cpp's generic-struct Nullable<T> support (no core change).
// Results are stringified through HasValue/Value (not boxed) to keep formatting
// deterministic across.NET and the transpiled output.
namespace LinqNullableSubset;


static class Program
{
    static string S(int? v) => v.HasValue ? v.Value.ToString() : "null";
    static string S(long? v) => v.HasValue ? v.Value.ToString() : "null";
    static string S(double? v) => v.HasValue ? v.Value.ToString() : "null";
    static string S(float? v) => v.HasValue ? v.Value.ToString() : "null";internal static void Run()
    {
        var ints = new List<int?> { 1, null, 3, null, 5 };       // non-null: 1,3,5
        var longs = new List<long?> { 10L, null, 20L };           // non-null: 10,20
        var doubles = new List<double?> { 1.5, null, 2.5 };       // non-null: 1.5,2.5
        var floats = new List<float?> { 1.5f, null, 0.5f };       // non-null: 1.5,0.5
        var allNull = new List<int?> { null, null };
        var empty = new List<int?>();

        // Sum (no selector): nulls skipped, empty/all-null -> 0.
        Console.WriteLine(S(ints.Sum()));        // 9
        Console.WriteLine(S(longs.Sum()));       // 30
        Console.WriteLine(S(doubles.Sum()));     // 4
        Console.WriteLine(S(floats.Sum()));      // 2
        Console.WriteLine(S(allNull.Sum()));     // 0
        Console.WriteLine(S(empty.Sum()));       // 0

        // Average (no selector): nulls skipped, empty/all-null -> null.
        Console.WriteLine(S(ints.Average()));    // 3
        Console.WriteLine(S(longs.Average()));   // 15
        Console.WriteLine(S(doubles.Average())); // 2
        Console.WriteLine(S(floats.Average()));  // 1
        Console.WriteLine(S(allNull.Average())); // null
        Console.WriteLine(S(empty.Average()));   // null

        // Selector forms over a projected element (string -> nullable number). The
        // empty word maps to null and is skipped; lengths are chosen so every average
        // terminates exactly (a repeating decimal would exercise float :G round-trip,
        // which is, not this ticket). Non-null lengths: 1, 3, 2 (sum 6, count 3).
        var words = new List<string> { "a", "", "bbb", "cc" };
        Func<string, int?> lenN = s => s.Length == 0 ? (int?)null : s.Length;
        Console.WriteLine(S(words.Sum(lenN)));        // 6
        Console.WriteLine(S(words.Average(lenN)));    // 6/3 = 2
        Console.WriteLine(S(words.Sum(s => s.Length == 0 ? (long?)null : (long)s.Length)));        // 6
        Console.WriteLine(S(words.Average(s => s.Length == 0 ? (double?)null : s.Length * 0.5)));   // (0.5+1.5+1.0)/3 = 1
        Console.WriteLine(S(words.Sum(s => s.Length == 0 ? (float?)null : s.Length * 1.0f)));       // 6
        Console.WriteLine(S(words.Average(s => s.Length == 0 ? (float?)null : (float)s.Length)));   // 6/3 = 2
    }
}
