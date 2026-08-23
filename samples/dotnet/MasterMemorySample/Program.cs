using System;
using System.Globalization;
using System.Linq;
using MasterMemory;
using MessagePack;
using MessagePack.Resolvers;

namespace MasterMemorySample;

public enum Kind
{
    Warrior,
    Mage,
}

[MemoryTable("characters"), MessagePackObject(true)]
public sealed record Character
{
    [PrimaryKey]
    public int Id { get; init; }

    [SecondaryKey(0), NonUnique]
    public Kind Kind { get; init; }

    [SecondaryKey(1), NonUnique]
    public int Level { get; init; }

    [SecondaryKey(2, keyOrder: 1), NonUnique]
    public int Rank { get; init; }

    [SecondaryKey(2, keyOrder: 0), NonUnique]
    public Kind RankKind { get; init; }

    public string Name { get; init; }
}

internal static class Program
{
    private static void Main()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

        StaticCompositeResolver.Instance.Register(
            MasterMemoryResolver.Instance,
            MessagePack.GeneratedMessagePackResolver.Instance,
            BuiltinResolver.Instance);
        IFormatterResolver resolver = StaticCompositeResolver.Instance;

        var builder = new DatabaseBuilder(resolver);
        builder.Append(CreateRows());
        byte[] bytes = builder.Build();
        var db = new MemoryDatabase(bytes, formatterResolver: resolver);

        Console.WriteLine("[build] bytes=" + bytes.Length + " count=" + db.CharacterTable.Count);
        Console.WriteLine("[primary] " + db.CharacterTable.FindById(3));
        Console.WriteLine("[try] hit=" + db.CharacterTable.TryFindById(5, out Character found)
            + " name=" + found.Name + " miss=" + db.CharacterTable.TryFindById(99, out _));

        Console.WriteLine("[secondary] mage=" + Join(db.CharacterTable.FindByKind(Kind.Mage)));
        Console.WriteLine("[composite] " + Join(db.CharacterTable.FindByRankKindAndRank((Kind.Mage, 2))));
        Console.WriteLine("[range] " + Join(db.CharacterTable.FindRangeByLevel(15, 30)));
        Console.WriteLine("[closest-low] " + Join(db.CharacterTable.FindClosestByLevel(26)));
        Console.WriteLine("[closest-high] " + Join(db.CharacterTable.FindClosestByLevel(26, false)));
        Console.WriteLine("[all] " + Join(db.CharacterTable.All));
        Console.WriteLine("[reverse] " + Join(db.CharacterTable.AllReverse));
    }

    private static Character[] CreateRows()
    {
        return new Character[]
        {
            new Character { Id = 1, Kind = Kind.Warrior, Level = 10, RankKind = Kind.Warrior, Rank = 1, Name = "Alice" },
            new Character { Id = 2, Kind = Kind.Mage, Level = 20, RankKind = Kind.Mage, Rank = 2, Name = "Bob" },
            new Character { Id = 3, Kind = Kind.Warrior, Level = 20, RankKind = Kind.Warrior, Rank = 2, Name = "Carol" },
            new Character { Id = 4, Kind = Kind.Mage, Level = 30, RankKind = Kind.Mage, Rank = 2, Name = "Dave" },
            new Character { Id = 5, Kind = Kind.Mage, Level = 40, RankKind = Kind.Mage, Rank = 3, Name = "Eve" },
        };
    }

    private static string Join(RangeView<Character> rows)
    {
        return string.Join(",", rows.Select(x => x.Id + ":" + x.Name));
    }
}
