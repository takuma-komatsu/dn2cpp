using System;

// `constrained. !T; callvirt object::Equals(object)` — the Object-rooted overload,
// whose argument is already a boxed reference. The receiver is NOT: it arrives as a
// managed pointer to the raw value, and dn2cpp_object_equals reads its first word as
// a type-info, so a primitive receiver that is not boxed at the call site has its own
// bits dereferenced as a Dn2CppTypeInfo* (0.003f => 0x3B449BA6) and the process dies.
// Every T shape that can reach the prefix is here, because the fault is per-kind:
// struct and reference receivers were already covered and are the controls.
namespace ConstrainedObjectEqualsSubset;

internal enum Hue : byte { Red = 1, Green = 2 }

internal struct Coord
{
    public int X;
    public int Y;
    public Coord(int x, int y) { X = x; Y = y; }
    public override bool Equals(object o) => o is Coord c && c.X == X && c.Y == Y;
    public override int GetHashCode() => X * 31 + Y;
}

internal sealed class Box<T>
{
    public T Value;
    public Box(T v) { Value = v; }

    // The field spelling (ldflda + constrained.) — the shape a settings wrapper
    // comparing its own stored value reaches.
    public bool EqField(Box<T> o) => Value.Equals((object)o.Value);

    // The same prefix over a local (ldloca).
    public bool EqLocal(T v)
    {
        T local = Value;
        return local.Equals((object)v);
    }

    // A boxed argument of a foreign runtime type, and null: both must answer False
    // rather than fault.
    public bool EqForeign(object o) => Value.Equals(o);
}

internal static class Program
{
    private static void One<T>(string tag, T a, T b)
    {
        var x = new Box<T>(a);
        Console.WriteLine("coe " + tag
            + " same=" + x.EqField(new Box<T>(a))
            + " diff=" + x.EqField(new Box<T>(b))
            + " local=" + x.EqLocal(a)
            + " localDiff=" + x.EqLocal(b)
            + " null=" + x.EqForeign(null)
            + " foreign=" + x.EqForeign("nope"));
    }

    internal static void Run()
    {
        One("float", 0.003f, 0.004f);
        One("double", 0.003d, 0.004d);
        One("int", 7, 8);
        One("uint", 7u, 8u);
        One("long", 7L, 8L);
        One("ulong", 7UL, 8UL);
        One("short", (short)7, (short)8);
        One("byte", (byte)7, (byte)8);
        One("sbyte", (sbyte)-7, (sbyte)8);
        One("char", 'a', 'b');
        One("bool", true, false);
        One("nint", (IntPtr)7, (IntPtr)8);
        One("enum", Hue.Red, Hue.Green);
        One("struct", new Coord(1, 2), new Coord(3, 4));
        One("decimal", 1.5m, 2.5m);
        One("datetime", new DateTime(2020, 1, 2), new DateTime(2021, 3, 4));
        One("string", "hi", "there");
        One("object", new object(), new object());
    }
}
