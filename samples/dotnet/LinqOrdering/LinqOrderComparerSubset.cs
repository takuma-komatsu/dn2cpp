#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

// LINQ ordering / Min / Max with an explicit IComparer<T>. The comparer is
// dispatched through (ComparerCriterion for ordering, direct Compare for Min/Max),
// not the natural-order CompareKeys. Sources are List<T>.

// A value-type-arg comparer: orders ints in reverse (exercises IComparer<int>
// interface dispatch with value-type args).
namespace LinqOrderComparerSubset;

class DescInt : IComparer<int>
{
    public int Compare(int x, int y) => y < x ? -1 : (y > x ? 1 : 0);
}

// A reference-type-arg comparer: orders strings by length.
class ByLen : IComparer<string>
{
    public int Compare(string? x, string? y)
    {
        int lx = x is null ? 0 : x.Length;
        int ly = y is null ? 0 : y.Length;
        return lx < ly ? -1 : (lx > ly ? 1 : 0);
    }
}

// Ordinal string comparer for ThenBy tie-breaks.
class Ordinal : IComparer<string>
{
    public int Compare(string? x, string? y) => string.CompareOrdinal(x, y);
}

class Program
{internal static void Run()
    {
        List<int> nums = new List<int> { 3, 1, 4, 1, 5, 9, 2, 6 };

        // OrderBy with a descending comparer -> descending order.
        List<int> desc = nums.OrderBy(x => x, new DescInt()).ToList();
        Console.WriteLine(string.Join(",", desc));

        // OrderByDescending with the same comparer -> double reverse = ascending.
        List<int> asc = nums.OrderByDescending(x => x, new DescInt()).ToList();
        Console.WriteLine(string.Join(",", asc));

        List<string> words = new List<string> { "bb", "a", "ccc", "dd", "e", "fff" };

        // OrderBy length, ThenBy ordinal (ascending tie-break).
        List<string> byLenAsc = words.OrderBy(w => w, new ByLen()).ThenBy(w => w, new Ordinal()).ToList();
        Console.WriteLine(string.Join(",", byLenAsc));

        // OrderBy length, ThenByDescending ordinal (descending tie-break).
        List<string> byLenDesc = words.OrderBy(w => w, new ByLen()).ThenByDescending(w => w, new Ordinal()).ToList();
        Console.WriteLine(string.Join(",", byLenDesc));

        // Min/Max with a reference-type comparer (by length): shortest / longest,
        // first-encountered on ties.
        string? shortest = words.Min(new ByLen());
        string? longest = words.Max(new ByLen());
        Console.WriteLine(shortest + " " + longest);

        // MinBy/MaxBy with a value-type-arg comparer over the int length key.
        // DescInt makes larger lengths "smaller", so MinBy picks the longest word
        // and MaxBy the shortest (both first-encountered).
        string? minBy = words.MinBy(w => w.Length, new DescInt());
        string? maxBy = words.MaxBy(w => w.Length, new DescInt());
        Console.WriteLine(minBy + " " + maxBy);
    }
}
