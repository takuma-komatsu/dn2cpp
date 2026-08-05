#nullable enable
// HashSet over enum elements beside HashSet<int>: membership, set operations, and
// the array identity of elements copied out of shared bodies — List<TEnum>.ToArray
// allocates inside a canonical-group body, so the result must still carry the real
// enum's array handle. Real System.Private.CoreLib (-r), run vs .NET.
using System;
using System.Collections.Generic;
namespace EnumHashSetSubset;

enum Fruit { Apple = 1, Pear = 2, Plum = 3, Fig = 4 }
enum Coin { One = 1, Two = 2, Five = 5 }

class Program
{
    internal static void __GateEntry()
    {
        var set = new HashSet<Fruit> { Fruit.Apple, Fruit.Pear, Fruit.Plum };
        Console.WriteLine("set count=" + set.Count);
        Console.WriteLine("has pear=" + set.Contains(Fruit.Pear));
        Console.WriteLine("has fig=" + set.Contains(Fruit.Fig));
        Console.WriteLine("add pear=" + set.Add(Fruit.Pear));
        Console.WriteLine("add fig=" + set.Add(Fruit.Fig));
        Console.WriteLine("remove plum=" + set.Remove(Fruit.Plum));
        foreach (var f in set)
            Console.WriteLine("elem " + f);

        var other = new HashSet<Fruit> { Fruit.Pear, Fruit.Fig };
        set.IntersectWith(other);
        Console.WriteLine("intersect count=" + set.Count);
        set.UnionWith(new HashSet<Fruit> { Fruit.Apple, Fruit.Pear });
        Console.WriteLine("union count=" + set.Count + " has apple=" + set.Contains(Fruit.Apple));

        var coins = new HashSet<Coin> { Coin.One, Coin.Five };
        Console.WriteLine("coins=" + coins.Count + " five=" + coins.Contains(Coin.Five));

        var ints = new HashSet<int> { 1, 2, 3 };
        ints.IntersectWith(new HashSet<int> { 2, 3, 4 });
        Console.WriteLine("ints count=" + ints.Count + " has2=" + ints.Contains(2));

        var list = new List<Fruit> { Fruit.Apple, Fruit.Fig };
        Fruit[] arr = list.ToArray();
        Console.WriteLine("arr type enum=" + (arr.GetType() == typeof(Fruit[])));
        Console.WriteLine("arr type int=" + (arr.GetType() == typeof(int[])));
        Console.WriteLine("arr elem=" + arr[1] + " len=" + arr.Length);

        var copy = new Fruit[set.Count];
        set.CopyTo(copy);
        Console.WriteLine("copy type=" + (copy.GetType() == typeof(Fruit[])));
        Console.WriteLine("copy0=" + copy[0]);
    }
}
