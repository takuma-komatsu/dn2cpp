using System;
using System.Collections.Generic;
using MessagePack;

namespace MessagePackUnionSubset;

[Union(3, typeof(Circle))]
[Union(7, typeof(Rectangle))]
public interface IShape
{
    string Describe();
}

[MessagePackObject]
public sealed class Circle : IShape
{
    [Key(0)]
    public int Radius { get; set; }

    public string Describe() => "circle:" + Radius;
}

[MessagePackObject]
public sealed class Rectangle : IShape
{
    [Key(0)]
    public int Width { get; set; }

    [Key(1)]
    public int Height { get; set; }

    public string Describe() => "rect:" + Width + "x" + Height;
}

[MessagePackObject]
public sealed class Drawing
{
    [Key(0)]
    public IShape Main { get; set; }

    [Key(1)]
    public List<IShape> More { get; set; }
}

internal static class Program
{
    internal static void __GateEntry(MessagePackSerializerOptions options)
    {
        var drawing = new Drawing
        {
            Main = new Circle { Radius = 5 },
            More = new List<IShape>
            {
                new Rectangle { Width = 2, Height = 9 },
                new Circle { Radius = 1 },
            },
        };
        byte[] bytes = MessagePackSerializer.Serialize(drawing, options);
        Drawing back = MessagePackSerializer.Deserialize<Drawing>(bytes, options);
        Console.WriteLine("[union] hex=" + Convert.ToHexString(bytes)
            + " back=" + back.Main.Describe() + "," + back.More[0].Describe()
            + "," + back.More[1].Describe());
    }
}
