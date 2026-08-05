// C# 1.0: class/struct/interface/delegate/event/property/indexer/enum,
// `params` arrays, operator overloading (binary `+` and an implicit/explicit
// conversion operator pair), `ref`/`out` parameters, boxing/unboxing, a
// user-defined attribute (applied, never reflected over), explicit
// `throw`/try/catch/finally, `foreach`, `checked`/`unchecked`, an inheritance
// chain with virtual/override/abstract, and a static constructor.
using System;

namespace Cs01;

internal enum Color
{
    Red,
    Green,
    Blue
}

[AttributeUsage(AttributeTargets.Class)]
internal sealed class GateAttribute : Attribute
{
    internal GateAttribute(string note)
    {
    }
}

internal interface IShape
{
    double Area();
}

internal delegate void Announce(string message);

[Gate("cs1 sample")]
internal abstract class ShapeBase : IShape
{
    private static int _instanceCount;

    static ShapeBase()
    {
        _instanceCount = 0;
    }

    protected ShapeBase()
    {
        _instanceCount++;
    }

    internal static int InstanceCount
    {
        get { return _instanceCount; }
    }

    public abstract double Area();

    public virtual string Describe()
    {
        return "shape";
    }
}

internal sealed class Square : ShapeBase
{
    private readonly double _side;

    internal Square(double side)
    {
        _side = side;
    }

    public override double Area()
    {
        return _side * _side;
    }

    public override string Describe()
    {
        return "square(" + _side + ")";
    }
}

internal struct Point
{
    internal int X;
    internal int Y;

    internal Point(int x, int y)
    {
        X = x;
        Y = y;
    }

    public static Point operator +(Point a, Point b)
    {
        return new Point(a.X + b.X, a.Y + b.Y);
    }

    public static implicit operator string(Point p)
    {
        return "(" + p.X + "," + p.Y + ")";
    }

    public static explicit operator int(Point p)
    {
        return p.X * 1000 + p.Y;
    }
}

internal sealed class Bag
{
    private readonly object[] _items;

    internal Bag(params object[] items)
    {
        _items = items;
    }

    public event Announce OnDump;

    internal object this[int index]
    {
        get { return _items[index]; }
        set { _items[index] = value; }
    }

    internal int Count
    {
        get { return _items.Length; }
    }

    internal void Dump()
    {
        if (OnDump != null)
        {
            OnDump("dumping " + _items.Length + " items");
        }
    }
}

internal static class Program
{
    private static void AddOne(ref int x)
    {
        x = x + 1;
    }

    private static bool TryParseColor(string text, out Color color)
    {
        switch (text)
        {
            case "Red":
                color = Color.Red;
                return true;
            case "Green":
                color = Color.Green;
                return true;
            case "Blue":
                color = Color.Blue;
                return true;
            default:
                color = Color.Red;
                return false;
        }
    }

    private static void Validate(int x)
    {
        if (x < 0)
        {
            throw new ArgumentException("x must be non-negative");
        }
    }

    private static void HandleDump(string message)
    {
        Console.WriteLine("event: " + message);
    }

    internal static void __GateEntry()
    {
        Console.WriteLine("== C# 1.0 ==");

        // inheritance + virtual/override/abstract + static constructor
        IShape[] shapes = new IShape[] { new Square(2.0), new Square(3.0) };
        foreach (IShape shape in shapes)
        {
            Console.WriteLine(((ShapeBase)shape).Describe() + " area=" + shape.Area());
        }
        Console.WriteLine("instances=" + ShapeBase.InstanceCount);

        // struct, operator overloading, implicit/explicit conversion
        Point p1 = new Point(1, 2);
        Point p2 = new Point(3, 4);
        Point p3 = p1 + p2;
        string asString = p3;
        int asInt = (int)p3;
        Console.WriteLine("point=" + asString + " asInt=" + asInt);

        // ref/out
        int n = 41;
        AddOne(ref n);
        Console.WriteLine("addOne=" + n);

        Color parsed;
        bool ok = TryParseColor("Green", out parsed);
        Console.WriteLine("parsed=" + ok + " " + parsed);

        // params array, indexer, event (explicit delegate creation, the C# 1.0 way)
        Bag bag = new Bag(1, "two", 3.0);
        bag.OnDump += new Announce(HandleDump);
        bag.Dump();
        Console.WriteLine("bag[1]=" + bag[1] + " count=" + bag.Count);

        // boxing/unboxing
        object boxed = 123;
        int unboxed = (int)boxed;
        Console.WriteLine("boxed=" + boxed + " unboxed=" + unboxed);

        // checked/unchecked
        unchecked
        {
            int overflowed = int.MaxValue + 1;
            Console.WriteLine("unchecked=" + overflowed);
        }
        try
        {
            checked
            {
                int willOverflow = int.MaxValue;
                willOverflow = willOverflow + 1;
                Console.WriteLine("checked=" + willOverflow);
            }
        }
        catch (OverflowException e)
        {
            Console.WriteLine("caught: " + e.GetType().Name);
        }
        finally
        {
            Console.WriteLine("finally ran");
        }

        // explicit throw, caught by type
        try
        {
            Validate(-1);
        }
        catch (ArgumentException e)
        {
            Console.WriteLine("validate caught: " + e.Message);
        }

        // attribute presence: applied above on ShapeBase, never reflected over
        Console.WriteLine("attribute applied to ShapeBase compiles");
    }
}
