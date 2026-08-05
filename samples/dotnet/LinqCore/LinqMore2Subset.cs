#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
namespace LinqMore2Subset;


class Program
{internal static void Run()
    {
        // Sources are List<T> (operators take IEnumerable<T>; a raw T[] dispatched
        // as IEnumerable<T> is the separate gap, so the existing LINQ gates
        // feed List<T> too).

        // Zip without a result selector pairs into value tuples,
        // stopping at the shorter sequence.
        List<int> a = new List<int> { 1, 2, 3 };
        List<string> b = new List<string> { "a", "b", "c", "d" };
        List<string> zipped = new List<string>();
        foreach ((int n, string s) in a.Zip(b))
            zipped.Add(n.ToString() + s);
        Console.WriteLine(string.Join(" ", zipped));

        // GroupBy(keySelector, elementSelector, resultSelector). The
        // resultSelector aggregates the per-group element sequence (Sum over an
        // IEnumerable works; string.Join over one would not). A string
        // first-letter key keeps the resultSelector concatenating strings only
        // (char.ToString inside a concat hits a separate, pre-existing core gap:
        // Roslyn's span concat lowering for a char).
        List<string> words = new List<string> { "apple", "avocado", "banana", "cherry", "blueberry" };
        List<string> byFirst = new List<string>();
        foreach (string r in words.GroupBy(
            w => w.Substring(0, 1), w => w.Length, (key, lens) => key + ":" + lens.Sum().ToString()))
            byFirst.Add(r);
        Console.WriteLine(string.Join(",", byFirst));

        // GroupBy(keySelector, resultSelector).
        List<int> nums = new List<int> { 1, 2, 3, 4, 5, 6 };
        List<string> parity = new List<string>();
        foreach (string r in nums.GroupBy(
            n => n % 2, (key, g) => key.ToString() + ":n" + g.Count().ToString() + ":s" + g.Sum().ToString()))
            parity.Add(r);
        Console.WriteLine(string.Join(",", parity));

        // numeric Sum/Average overloads (integer-valued so formatting is exact).
        List<long> longs = new List<long> { 10L, 20L, 30L };
        Console.WriteLine(longs.Sum().ToString() + " " + longs.Average().ToString());
        List<float> floats = new List<float> { 2f, 4f, 6f };
        Console.WriteLine(floats.Sum().ToString() + " " + floats.Average().ToString());

        // selector Sum/Average over int/long/double/float projections.
        List<string> items = new List<string> { "x", "yy", "zzz" };
        Console.WriteLine(items.Sum(w => w.Length).ToString() + " " + items.Average(w => w.Length).ToString());
        Console.WriteLine(items.Sum(w => (long)w.Length).ToString() + " " + items.Average(w => (long)w.Length).ToString());
        Console.WriteLine(items.Sum(w => (double)w.Length).ToString() + " " + items.Average(w => (double)w.Length).ToString());
        Console.WriteLine(items.Sum(w => (float)w.Length).ToString() + " " + items.Average(w => (float)w.Length).ToString());
    }
}
