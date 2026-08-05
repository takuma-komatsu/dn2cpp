#nullable enable
// Dictionary over int-underlying enum keys beside Dictionary<int,V>: two distinct
// enums group under one canonical body, and everything observable must still match
// real .NET — add/lookup/remove/enumerate, a user IEqualityComparer<TEnum> honored
// through the receiver's canonical alias row (a silent fall-back to default
// equality would change the parity-collision counts below), boxed-key identity and
// member-name ToString. Real System.Private.CoreLib (-r), run vs .NET.
using System;
using System.Collections.Generic;
namespace EnumDictSubset;

enum Color { Red = 1, Green = 2, Blue = 3, Cyan = 4 }
enum Shape { Dot = 10, Line = 20, Tri = 30, Quad = 40 }

sealed class ParityComparer : IEqualityComparer<Color>
{
    public bool Equals(Color a, Color b) => ((int)a & 1) == ((int)b & 1);
    public int GetHashCode(Color c) => (int)c & 1;
}

class Program
{
    internal static void __GateEntry()
    {
        var byColor = new Dictionary<Color, string>();
        byColor.Add(Color.Red, "r");
        byColor[Color.Green] = "g";
        byColor[Color.Blue] = "b";
        Console.WriteLine("color count=" + byColor.Count);
        Console.WriteLine("green=" + byColor[Color.Green]);
        Console.WriteLine("has blue=" + byColor.ContainsKey(Color.Blue));
        Console.WriteLine("has cyan=" + byColor.ContainsKey(Color.Cyan));
        Console.WriteLine("remove red=" + byColor.Remove(Color.Red));
        Console.WriteLine("after remove=" + byColor.Count);
        byColor[Color.Cyan] = "c";
        foreach (var kv in byColor)
            Console.WriteLine("kv " + kv.Key + "=" + kv.Value);
        Console.WriteLine("try green=" + byColor.TryGetValue(Color.Green, out var g) + " " + g);
        Console.WriteLine("try red=" + byColor.TryGetValue(Color.Red, out var r) + " " + (r is null));

        var byShape = new Dictionary<Shape, string> { { Shape.Dot, "." }, { Shape.Line, "-" }, { Shape.Tri, "^" } };
        Console.WriteLine("shape count=" + byShape.Count);
        Console.WriteLine("tri=" + byShape[Shape.Tri]);
        Console.WriteLine("quad=" + byShape.ContainsKey(Shape.Quad));

        var byInt = new Dictionary<int, string> { { 1, "one" }, { 2, "two" } };
        Console.WriteLine("int count=" + byInt.Count + " one=" + byInt[1]);

        // The user comparer must be dispatched (not silently replaced by
        // default equality): parity merges the four members into two buckets.
        var parity = new Dictionary<Color, int>(new ParityComparer());
        parity[Color.Red] = 1;
        parity[Color.Green] = 2;
        parity[Color.Blue] = 3;
        parity[Color.Cyan] = 4;
        Console.WriteLine("parity count=" + parity.Count);
        Console.WriteLine("parity red=" + parity[Color.Red] + " green=" + parity[Color.Green]);
        Console.WriteLine("parity has quad-odd=" + parity.ContainsKey((Color)7));

        object boxed = Color.Blue;
        Console.WriteLine("boxed is Color=" + (boxed is Color));
        Console.WriteLine("boxed is Shape=" + (boxed is Shape));
        Console.WriteLine("boxed tostr=" + boxed.ToString());
        Color k = Color.Green;
        Console.WriteLine("key tostr=" + k.ToString());
        Console.WriteLine("dict type self=" + (byColor.GetType() == typeof(Dictionary<Color, string>)));
        Console.WriteLine("dict type int=" + (byColor.GetType() == typeof(Dictionary<int, string>)));
    }
}
