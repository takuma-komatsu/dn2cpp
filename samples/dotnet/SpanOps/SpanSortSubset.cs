#nullable enable
using System;
using System.Collections.Generic;

// A concrete IComparer<int> (passed by its concrete type to span.Sort<T,TComparer>).
namespace SpanSortSubset;

sealed class DescInt : IComparer<int>
{
    public int Compare(int a, int b) => b - a;
}

sealed class Person
{
    public string Name;
    public int Age;
    public Person(string name, int age) { Name = name; Age = age; }
    public override string ToString() => Name + ":" + Age;
}

// A concrete IComparer<Person>: the reference-element path.
sealed class ByAge : IComparer<Person>
{
    public int Compare(Person? a, Person? b) => a!.Age - b!.Age;
}

static class Program
{internal static void __GateEntry()
    {
        // Comparerless natural order — int / long / double / string (ordinal).
        int[] ai = { 3, 1, 2, 5, 4 };
        ((Span<int>)ai).Sort();
        Console.WriteLine(string.Join(",", ai));

        long[] al = { 30L, 10L, 20L, -5L };
        ((Span<long>)al).Sort();
        Console.WriteLine(string.Join(",", al));

        double[] ad = { 3.5, 1.25, 2.0, -1.0 };
        ((Span<double>)ad).Sort();
        Console.WriteLine(string.Join(",", ad));

        // All-lowercase distinct initials, so ordinal — dn2cpp has no culture —
        // agrees with .NET's culture-aware Comparer<string>.Default here.
        string[] astr = { "banana", "apple", "date", "cherry" };
        ((Span<string>)astr).Sort();
        Console.WriteLine(string.Join(",", astr));

        // Comparison<int> — descending via a lambda delegate.
        int[] bc = { 3, 1, 2, 5, 4 };
        ((Span<int>)bc).Sort((x, y) => y - x);
        Console.WriteLine(string.Join(",", bc));

        // IComparer<int> — descending via a concrete comparer class.
        int[] cc = { 3, 1, 2, 5, 4 };
        ((Span<int>)cc).Sort(new DescInt());
        Console.WriteLine(string.Join(",", cc));

        // Range sort over a sub-span (sort the middle three, leave the ends).
        int[] mid = { 9, 3, 1, 2, 8 };
        mid.AsSpan(1, 3).Sort();
        Console.WriteLine(string.Join(",", mid));

        // Reference elements via a Comparison delegate (by name length, then ordinal).
        Person[] pc = { new("Zoe", 40), new("Al", 22), new("Mae", 31) };
        ((Span<Person>)pc).Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
        Console.WriteLine(string.Join(",", (object[])pc));

        // Reference elements via a concrete IComparer (by Age ascending).
        Person[] pa = { new("Zoe", 40), new("Al", 22), new("Mae", 31) };
        ((Span<Person>)pa).Sort(new ByAge());
        Console.WriteLine(string.Join(",", (object[])pa));

        // Edge cases: empty span and a single element.
        int[] empty = Array.Empty<int>();
        ((Span<int>)empty).Sort();
        Console.WriteLine("empty:" + empty.Length);

        int[] one = { 42 };
        ((Span<int>)one).Sort();
        ((Span<int>)one).Sort(new DescInt());
        Console.WriteLine("one:" + one[0]);

        // Parallel key+value Sort. ValueTuple keys paired with reference values,
        // comparerless, so Comparer<(int,int)>.Default orders lexicographically.
        // Keys are distinct, so the result is fully determined.
        var kvKeys = new (int, int)[] { (2, 0), (0, 1), (2, 1), (-1, 0), (0, 0) };
        var kvVals = new string[] { "two-a", "zero-b", "two-b", "neg", "zero-a" };
        ((Span<(int, int)>)kvKeys).Sort((Span<string>)kvVals);
        var kvKeyStrs = new string[kvKeys.Length];
        for (int i = 0; i < kvKeys.Length; i++)
            kvKeyStrs[i] = kvKeys[i].Item1 + "/" + kvKeys[i].Item2;
        Console.WriteLine(string.Join(",", kvKeyStrs));
        Console.WriteLine(string.Join(",", kvVals));

        // Primitive keys paired with reference values: the primitive-key thunk.
        int[] pk = { 5, 1, 3, 2, 4 };
        string[] pv = { "e", "a", "c", "b", "d" };
        ((Span<int>)pk).Sort((Span<string>)pv);
        Console.WriteLine(string.Join(",", pk) + " | " + string.Join(",", pv));

        // Value keys paired with value (int) items — both buffers permuted in lockstep.
        long[] lk = { 30L, 10L, 20L };
        int[] lv = { 300, 100, 200 };
        ((Span<long>)lk).Sort((Span<int>)lv);
        Console.WriteLine(string.Join(",", lk) + " | " + string.Join(",", lv));
    }
}
