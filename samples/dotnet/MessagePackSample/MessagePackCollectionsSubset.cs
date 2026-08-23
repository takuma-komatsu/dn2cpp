using System;
using System.Collections.Generic;
using MessagePack;

namespace MessagePackCollectionsSubset;

[MessagePackObject]
public sealed class Bag
{
    [Key(0)]
    public int[] Values { get; set; }

    [Key(1)]
    public List<string> Names { get; set; }

    [Key(2)]
    public Dictionary<string, int> Counts { get; set; }

    [Key(3)]
    public int? Maybe { get; set; }
}

internal static class Program
{
    internal static void __GateEntry(MessagePackSerializerOptions options)
    {
        var bag = new Bag
        {
            Values = new int[] { 1, -2, 300 },
            Names = new List<string> { "a", "bb", "" },
            Counts = new Dictionary<string, int> { ["red"] = 2, ["blue"] = 5 },
            Maybe = 42,
        };
        byte[] bytes = MessagePackSerializer.Serialize(bag, options);
        Bag back = MessagePackSerializer.Deserialize<Bag>(bytes, options);
        Console.WriteLine("[collections] hex=" + Convert.ToHexString(bytes));
        Console.WriteLine("[collections-back] values=" + string.Join(",", back.Values)
            + " names=" + string.Join("|", back.Names)
            + " counts=" + back.Counts["red"] + ":" + back.Counts["blue"]
            + " maybe=" + back.Maybe);

        byte[] scalar = MessagePackSerializer.Serialize(new List<int> { 0, 127, 128, -33 }, options);
        Console.WriteLine("[list] hex=" + Convert.ToHexString(scalar)
            + " back=" + string.Join(",", MessagePackSerializer.Deserialize<List<int>>(scalar, options)));
    }
}
