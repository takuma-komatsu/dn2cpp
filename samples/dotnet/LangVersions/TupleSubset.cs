// C# value tuples (System.ValueTuple<...>) transpiled over the real
// CoreLib. Tuple literals, named elements, deconstruction, tuples in
// List/Dictionary, equality and — the crash fix — ValueTuple.ToString().
//
// ValueTuple.ToString() formats each field via `Item?.ToString()`, which the
// C# compiler lowers to `constrained. <fieldType> callvirt object::ToString()`.
// The transpiler now devirtualizes that for primitive/string fields and routes
// `tuple.ToString()` itself (a `constrained. ValueTuple<..> callvirt
// object::ToString()`) to the struct's own override, instead of handing the raw
// managed pointer to dn2cpp_object_tostring (which crashed).
//
// Whole-tuple formatting where the tuple is boxed
// (Console.WriteLine(tuple) / "s" + tuple / string.Format) dispatches through
// the value type's TypeInfo `tostring` thunk, and an interpolation hole
// `$"{tuple}"` (AppendFormatted<T>, no box) calls the struct ToString directly.
using System;
using System.Collections.Generic;

namespace TupleSubset;

internal static class Program
{
    private static (int, int) MinMax(int a, int b) => a < b ? (a, b) : (b, a);

    private static (string name, int age) MakePerson() => ("Alice", 30);

    internal static void __GateEntry()
    {
        var t = (1, 2);
        Console.WriteLine(t.Item1 + "," + t.Item2);

        var (lo, hi) = MinMax(7, 3);
        Console.WriteLine(lo + ".." + hi);

        var p = MakePerson();
        Console.WriteLine(p.name + "=" + p.age);

        var list = new List<(int, string)>();
        list.Add((1, "a"));
        list.Add((2, "b"));
        foreach (var (n, s) in list)
            Console.WriteLine(n + ":" + s);

        Console.WriteLine(((1, 2) == (1, 2)).ToString());
        Console.WriteLine(t.Equals((3, 4)).ToString());

        Console.WriteLine(t.ToString());
        Console.WriteLine((42L, 3.5).ToString());
        Console.WriteLine((true, 'Z').ToString());
        Console.WriteLine(("hi", 7).ToString());
        Console.WriteLine((42L, 3.5, true, 'Z', "hi").ToString());

        var d = new Dictionary<string, (int hp, int mp)>();
        d["hero"] = (100, 50);
        Console.WriteLine(d["hero"].hp + "/" + d["hero"].mp);

        (int x, int y) = (5, 9);
        (x, y) = (y, x);
        Console.WriteLine(x + "," + y);

        // Whole-tuple formatting (the tuple is boxed or formatted as a unit).
        Console.WriteLine(t);                                       // box -> tostring thunk
        Console.WriteLine((42L, 3.5, true, 'Z', "hi"));
        Console.WriteLine($"named={p}");                            // interpolation hole
        Console.WriteLine("concat=" + (10, 20));                    // concat(object) -> box
        Console.WriteLine(string.Format("fmt {0}", (true, 'Z')));   // string.Format
        foreach (var pair in list)
            Console.WriteLine(pair);                                // whole tuple per item
    }
}
