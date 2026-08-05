#nullable enable
using System;

// Array.IndexOf<T> / Array.LastIndexOf<T> over EVERY element type, not just the
// int/string/reference-equatable ones the intrinsic used to accept: the element
// comparison is the same devirtualized EqualityComparer<T>.Default.Equals the
// Dictionary key path uses, so a struct compares by its IEquatable<T>.Equals and a
// reference element by its Equals(object) override — a Base[] holding a Derived that
// overrides Equals now answers the way .NET does, where a pointer compare said "not
// found". Covers the argument defaults of all three overloads of each (the backward
// scan's start/count derivation is where an empty array bites). Diffed exact vs .NET.
namespace ArrayElementEqualitySubset;

internal struct Point : IEquatable<Point>
{
    internal int X;
    internal int Y;

    internal Point(int x, int y)
    {
        X = x;
        Y = y;
    }

    public bool Equals(Point other) => X == other.X && Y == other.Y;

    public override bool Equals(object? o) => o is Point p && Equals(p);

    public override int GetHashCode() => X * 31 + Y;

    public override string ToString() => "(" + X + "," + Y + ")";
}

// No IEquatable<T>: the Object-virtual Equals(object) is the one that dispatches.
internal struct Tag
{
    internal int Id;

    internal Tag(int id)
    {
        Id = id;
    }

    public override bool Equals(object? o) => o is Tag t && t.Id == Id;

    public override int GetHashCode() => Id;
}

internal enum Color : byte
{
    Red = 1,
    Green = 2,
    Blue = 3,
}

internal enum Big : long
{
    Small = 1L,
    Huge = 0x1_0000_0001L,   // low 32 bits collide with Small
}

internal class Item
{
    internal int Id;

    internal Item(int id)
    {
        Id = id;
    }

    public override bool Equals(object? o) => o is Item i && i.Id == Id;

    public override int GetHashCode() => Id;
}

internal class Plain
{
    // Overrides nothing: reference equality, like every default class.
}

internal class Derived : Item
{
    internal Derived(int id) : base(id)
    {
    }
}

internal class Program
{
    internal static void Run()
    {
        // --- element widths the old intrinsic rejected outright ---
        double[] d = { 1.5, 2.5, 3.5, 2.5 };
        Console.WriteLine(Array.IndexOf(d, 2.5));       // 1
        Console.WriteLine(Array.LastIndexOf(d, 2.5));   // 3
        Console.WriteLine(Array.IndexOf(d, 9.5));       // -1

        byte[] b = { 7, 8, 9, 8 };
        Console.WriteLine(Array.IndexOf(b, (byte)8));       // 1
        Console.WriteLine(Array.LastIndexOf(b, (byte)8));   // 3

        char[] c = { 'a', 'b', 'c', 'b' };
        Console.WriteLine(Array.IndexOf(c, 'b'));       // 1
        Console.WriteLine(Array.LastIndexOf(c, 'b'));   // 3

        bool[] bl = { false, true, false };
        Console.WriteLine(Array.IndexOf(bl, true));     // 1

        long[] l = { 10L, 20L, 30L, 20L };
        Console.WriteLine(Array.IndexOf(l, 20L));       // 1
        Console.WriteLine(Array.LastIndexOf(l, 20L));   // 3

        short[] sh = { 1, 2, 3 };
        Console.WriteLine(Array.IndexOf(sh, (short)3)); // 2

        uint[] u = { 1u, 4000000000u, 3u };
        Console.WriteLine(Array.IndexOf(u, 4000000000u)); // 1

        float[] f = { 1.0f, 2.0f, 3.0f };
        Console.WriteLine(Array.IndexOf(f, 2.0f));      // 1

        // --- enums: a byte-backed one packs sub-word, a 64-bit one must not be
        //     compared through a truncated int32 (Small and Huge share their low half) ---
        Color[] cols = { Color.Red, Color.Green, Color.Blue, Color.Green };
        Console.WriteLine(Array.IndexOf(cols, Color.Green));      // 1
        Console.WriteLine(Array.LastIndexOf(cols, Color.Green));  // 3
        Console.WriteLine(Array.IndexOf(cols, Color.Blue));       // 2

        Big[] bigs = { Big.Huge, Big.Small };
        Console.WriteLine(Array.IndexOf(bigs, Big.Small));        // 1
        Console.WriteLine(Array.IndexOf(bigs, Big.Huge));         // 0

        // --- structs: typed IEquatable<T>.Equals, and the Object-virtual fallback ---
        Point[] pts = { new Point(1, 2), new Point(3, 4), new Point(1, 2) };
        Console.WriteLine(Array.IndexOf(pts, new Point(3, 4)));       // 1
        Console.WriteLine(Array.IndexOf(pts, new Point(1, 2)));       // 0
        Console.WriteLine(Array.LastIndexOf(pts, new Point(1, 2)));   // 2
        Console.WriteLine(Array.IndexOf(pts, new Point(9, 9)));       // -1

        Tag[] tags = { new Tag(1), new Tag(2), new Tag(3) };
        Console.WriteLine(Array.IndexOf(tags, new Tag(2)));           // 1
        Console.WriteLine(Array.IndexOf(tags, new Tag(7)));           // -1

        // --- reference elements: the override is dispatched, not pointer-compared ---
        Item[] items = { new Item(1), new Item(2), new Item(3) };
        Console.WriteLine(Array.IndexOf(items, new Item(2)));         // 1 (a DIFFERENT object)
        Console.WriteLine(Array.IndexOf(items, new Item(9)));         // -1

        // The soundness case: a Base[] holding a Derived whose inherited Equals is
        // value equality. A pointer compare would say -1.
        Item[] mixed = { new Item(1), new Derived(2) };
        Console.WriteLine(Array.IndexOf(mixed, new Item(2)));         // 1
        Console.WriteLine(Array.IndexOf(mixed, new Derived(1)));      // 0

        // A class that overrides nothing keeps reference equality.
        Plain p0 = new Plain();
        Plain p1 = new Plain();
        Plain[] plains = { p0, p1 };
        Console.WriteLine(Array.IndexOf(plains, p1));                 // 1
        Console.WriteLine(Array.IndexOf(plains, new Plain()));        // -1

        // null is an element like any other.
        string?[] withNull = { "a", null, "b" };
        Console.WriteLine(Array.IndexOf(withNull, null));             // 1
        Console.WriteLine(Array.LastIndexOf(withNull, "b"));          // 2

        // --- overload argument defaults ---
        int[] xs = { 5, 6, 7, 6, 5 };
        Console.WriteLine(Array.IndexOf(xs, 6));           // 1
        Console.WriteLine(Array.IndexOf(xs, 6, 2));        // 3
        Console.WriteLine(Array.IndexOf(xs, 5, 1, 3));     // -1 (scans 6,7,6)
        Console.WriteLine(Array.IndexOf(xs, 7, 1, 3));     // 2
        Console.WriteLine(Array.LastIndexOf(xs, 6));       // 3
        Console.WriteLine(Array.LastIndexOf(xs, 6, 2));    // 1 (scans 7,6,5 backwards)
        Console.WriteLine(Array.LastIndexOf(xs, 5, 3, 2)); // -1 (scans 6,7)
        Console.WriteLine(Array.LastIndexOf(xs, 5, 4, 2)); // 4

        // Empty arrays: the backward scan's default startIndex is -1, and .NET
        // returns -1 without ever looking at the buffer.
        int[] empty = Array.Empty<int>();
        Console.WriteLine(Array.IndexOf(empty, 1));        // -1
        Console.WriteLine(Array.LastIndexOf(empty, 1));    // -1
        Point[] emptyPts = Array.Empty<Point>();
        Console.WriteLine(Array.LastIndexOf(emptyPts, new Point(0, 0))); // -1

        // Single element, backward: startIndex = 0, count = 1.
        Console.WriteLine(Array.LastIndexOf(new int[] { 42 }, 42));  // 0

        // --- the non-generic (untyped) form: element boxed, Object.Equals dispatched ---
        Array untyped = xs;
        Console.WriteLine(Array.IndexOf(untyped, 7));            // 2
        Console.WriteLine(Array.LastIndexOf(untyped, 6));        // 3
        Console.WriteLine(Array.IndexOf(untyped, 6, 2));         // 3
        Console.WriteLine(Array.IndexOf(untyped, 99));           // -1
        Array untypedStr = new string[] { "x", "y", "x" };
        Console.WriteLine(Array.IndexOf(untypedStr, "x"));       // 0
        Console.WriteLine(Array.LastIndexOf(untypedStr, "x"));   // 2
    }
}
