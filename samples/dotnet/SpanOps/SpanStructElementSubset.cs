#nullable enable
using System;

// MemoryExtensions over element types a `==`-based scalar loop cannot serve. The
// signatures say `where T : IEquatable<T>`, and Double.Equals is not `==`: a
// Span<double> holding NaN must answer Contains(NaN) with true. Every scan uses the
// same devirtualized EqualityComparer<T>.Default.Equals as the array and dictionary
// paths. Diffed exact vs .NET.
namespace SpanStructElementSubset;

internal struct Vec2 : IEquatable<Vec2>
{
    internal int X;
    internal int Y;

    internal Vec2(int x, int y)
    {
        X = x;
        Y = y;
    }

    public bool Equals(Vec2 other) => X == other.X && Y == other.Y;

    public override bool Equals(object? o) => o is Vec2 v && Equals(v);

    public override int GetHashCode() => X * 31 + Y;
}

// Equals(object) only — the Object-virtual fallback arm.
internal struct Slot
{
    internal int Id;

    internal Slot(int id)
    {
        Id = id;
    }

    public override bool Equals(object? o) => o is Slot s && s.Id == Id;

    public override int GetHashCode() => Id;
}

internal enum Kind : byte
{
    A = 1,
    B = 2,
    C = 3,
}

class Program
{
    internal static void __GateEntry()
    {
        // --- struct elements: (span, value) Contains / IndexOf / LastIndexOf ---
        Vec2[] vs = { new Vec2(1, 2), new Vec2(3, 4), new Vec2(1, 2) };
        Span<Vec2> sv = vs;
        Console.WriteLine(sv.Contains(new Vec2(3, 4)));       // True
        Console.WriteLine(sv.Contains(new Vec2(9, 9)));       // False
        Console.WriteLine(sv.IndexOf(new Vec2(1, 2)));        // 0
        Console.WriteLine(sv.LastIndexOf(new Vec2(1, 2)));    // 2
        Console.WriteLine(sv.IndexOf(new Vec2(9, 9)));        // -1

        // (span, span): SequenceEqual / StartsWith / EndsWith, and the
        // subsequence IndexOf.
        Vec2[] tail = { new Vec2(1, 2) };
        Vec2[] head = { new Vec2(1, 2), new Vec2(3, 4) };
        Vec2[] same = { new Vec2(1, 2), new Vec2(3, 4), new Vec2(1, 2) };
        Console.WriteLine(sv.SequenceEqual(same));            // True
        Console.WriteLine(sv.SequenceEqual(head));            // False (length)
        Console.WriteLine(sv.StartsWith(head));               // True
        Console.WriteLine(sv.EndsWith(tail));                 // True
        Console.WriteLine(sv.IndexOf((ReadOnlySpan<Vec2>)head));   // 0
        Console.WriteLine(sv.LastIndexOf((ReadOnlySpan<Vec2>)tail)); // 2

        // (span, v0, v1): the fixed-arity membership scan.
        Console.WriteLine(sv.IndexOfAny(new Vec2(9, 9), new Vec2(3, 4)));       // 1
        Console.WriteLine(sv.IndexOfAnyExcept(new Vec2(1, 2)));                 // 1
        Console.WriteLine(sv.LastIndexOfAnyExcept(new Vec2(1, 2)));             // 1

        // (span, values span): the set membership scan.
        Vec2[] wanted = { new Vec2(3, 4), new Vec2(7, 8) };
        Console.WriteLine(sv.IndexOfAny((ReadOnlySpan<Vec2>)wanted));           // 1
        Console.WriteLine(sv.LastIndexOfAny((ReadOnlySpan<Vec2>)wanted));       // 1
        Console.WriteLine(sv.IndexOfAnyExcept((ReadOnlySpan<Vec2>)wanted));     // 0

        // --- a struct with only the Object-virtual Equals ---
        Slot[] slots = { new Slot(1), new Slot(2), new Slot(3) };
        Span<Slot> ss = slots;
        Console.WriteLine(ss.Contains(new Slot(2)));          // True
        Console.WriteLine(ss.IndexOf(new Slot(3)));           // 2
        Console.WriteLine(ss.Contains(new Slot(7)));          // False

        // --- float/double: Equals, not `==`. NaN equals NaN; +0 equals -0. ---
        double[] ds = { 1.5, double.NaN, -0.0, 2.5 };
        Span<double> sd = ds;
        Console.WriteLine(sd.Contains(double.NaN));           // True
        Console.WriteLine(sd.IndexOf(double.NaN));            // 1
        Console.WriteLine(sd.IndexOf(0.0));                   // 2 (+0 equals -0)
        Console.WriteLine(sd.Contains(2.5));                  // True
        Console.WriteLine(sd.Contains(9.5));                  // False
        Console.WriteLine(sd.LastIndexOf(1.5));               // 0

        float[] fs = { 1f, float.NaN, 3f };
        Span<float> sf = fs;
        Console.WriteLine(sf.Contains(float.NaN));            // True
        Console.WriteLine(sf.IndexOf(3f));                    // 2

        double[] nanOnly = { double.NaN };
        Console.WriteLine(((ReadOnlySpan<double>)ds).StartsWith((ReadOnlySpan<double>)new double[] { 1.5 })); // True
        Console.WriteLine(((ReadOnlySpan<double>)nanOnly).SequenceEqual(new double[] { double.NaN }));        // True

        // --- enum elements ---
        Kind[] ks = { Kind.A, Kind.B, Kind.C, Kind.B };
        Span<Kind> sk = ks;
        Console.WriteLine(sk.Contains(Kind.B));               // True
        Console.WriteLine(sk.IndexOf(Kind.C));                // 2
        Console.WriteLine(sk.LastIndexOf(Kind.B));            // 3
        Console.WriteLine(sk.IndexOfAny(Kind.C, Kind.B));     // 1

        // --- an empty span still answers ---
        Span<Vec2> none = Array.Empty<Vec2>();
        Console.WriteLine(none.Contains(new Vec2(0, 0)));     // False
        Console.WriteLine(none.IndexOf(new Vec2(0, 0)));      // -1
    }
}
