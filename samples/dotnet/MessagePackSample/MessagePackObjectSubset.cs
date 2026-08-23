using System;
using MessagePack;

namespace MessagePackObjectSubset;

[MessagePackObject]
public sealed class Person
{
    [Key(0)]
    public int Id { get; set; }

    [Key(1)]
    public string Name { get; set; }

    [Key(2)]
    public int Score { get; set; }

    [IgnoreMember]
    public int Ignored { get; set; }
}

[MessagePackObject(true)]
public sealed class Named
{
    public int Count { get; set; }

    public string Label { get; set; }
}

[MessagePackObject]
public readonly struct Point
{
    [Key(0)]
    public int X { get; }

    [Key(1)]
    public int Y { get; }

    [SerializationConstructor]
    public Point(int x, int y)
    {
        X = x;
        Y = y;
    }
}

internal static class Program
{
    internal static void __GateEntry(MessagePackSerializerOptions options)
    {
        var person = new Person { Id = 7, Name = "alice", Score = -19, Ignored = 99 };
        byte[] bytes = MessagePackSerializer.Serialize(person, options);
        Person back = MessagePackSerializer.Deserialize<Person>(bytes, options);
        Console.WriteLine("[object] hex=" + Convert.ToHexString(bytes)
            + " back=" + back.Id + ":" + back.Name + ":" + back.Score
            + " ignored=" + back.Ignored);

        byte[] namedBytes = MessagePackSerializer.Serialize(new Named { Count = 3, Label = "map" }, options);
        Named named = MessagePackSerializer.Deserialize<Named>(namedBytes, options);
        Console.WriteLine("[map] hex=" + Convert.ToHexString(namedBytes)
            + " back=" + named.Count + ":" + named.Label);

        byte[] pointBytes = MessagePackSerializer.Serialize(new Point(-4, 12), options);
        Point point = MessagePackSerializer.Deserialize<Point>(pointBytes, options);
        Console.WriteLine("[ctor] hex=" + Convert.ToHexString(pointBytes)
            + " back=" + point.X + ":" + point.Y);

        byte[] nil = MessagePackSerializer.Serialize<Person>(null, options);
        Console.WriteLine("[null] hex=" + Convert.ToHexString(nil)
            + " back=" + (MessagePackSerializer.Deserialize<Person>(nil, options) is null));
    }
}
