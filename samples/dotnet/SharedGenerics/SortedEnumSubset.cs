#nullable enable
// SortedDictionary/SortedSet over enum keys: Comparer<TEnum>.Default is stamped
// with the closed enum's identity, so the comparer-touching bodies fall back per
// instantiation and the ordering must still match real .NET exactly. Real
// System.Private.CoreLib (-r), run vs .NET.
using System;
using System.Collections.Generic;
namespace SortedEnumSubset;

enum Rank { Gold = 1, Silver = 2, Bronze = 3, None = 99 }
enum Tier { High = 5, Mid = 3, Low = 1 }

class Program
{
    internal static void __GateEntry()
    {
        var sd = new SortedDictionary<Rank, string>();
        sd[Rank.Bronze] = "b";
        sd[Rank.None] = "n";
        sd[Rank.Gold] = "g";
        sd[Rank.Silver] = "s";
        foreach (var kv in sd)
            Console.WriteLine("sorted " + kv.Key + "=" + kv.Value);
        Console.WriteLine("sd count=" + sd.Count + " gold=" + sd[Rank.Gold]);
        Console.WriteLine("remove silver=" + sd.Remove(Rank.Silver));
        foreach (var kv in sd)
            Console.WriteLine("after " + kv.Key);

        var ss = new SortedSet<Tier> { Tier.Mid, Tier.Low, Tier.High };
        foreach (var t in ss)
            Console.WriteLine("tier " + t);
        Console.WriteLine("min=" + ss.Min + " max=" + ss.Max);
    }
}
