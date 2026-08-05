// C# 12: primary constructors on a non-record class and struct, collection
// expressions (array / List<T> / Span<T> / ReadOnlySpan<T> targets, spread
// elements, the empty literal, and a user-defined CollectionBuilder type),
// [InlineArray] structs, default lambda parameters, `ref readonly` parameters,
// and `using` aliases for any type.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

// C# 12 lets a `using` alias name any type, not just a named one: a tuple type
// and an array type both alias here.
using Pair = (int Number, string Name);
using IntArr = int[];

namespace Cs12;

// C# 12 primary constructor on a plain class: `name` and `age` are captured, not
// turned into public members (that is what a record would do).
internal class Person(string name, int age)
{
    public string Describe() => $"{name} ({age})";

    public int NextYear => age + 1;
}

// C# 12 primary constructor on a struct.
internal struct Pt(int x, int y)
{
    public int X => x;

    public int Y => y;

    public override string ToString() => $"[{x},{y}]";
}

// C# 12 CollectionBuilder: makes a user-defined type a legal collection-expression
// target. The builder takes a ReadOnlySpan<T> of the elements.
[CollectionBuilder(typeof(MyColBuilder), nameof(MyColBuilder.Create))]
internal sealed class MyCol<T> : IEnumerable<T>
{
    private readonly T[] _items;

    public MyCol(T[] items)
    {
        _items = items;
    }

    public int Count => _items.Length;

    public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)_items).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();
}

internal static class MyColBuilder
{
    public static MyCol<T> Create<T>(ReadOnlySpan<T> items) => new MyCol<T>(items.ToArray());
}

// C# 12 inline array: one field, N elements laid out contiguously, indexable and
// spannable.
[InlineArray(4)]
internal struct Buf4
{
    private int _element0;
}

internal static class Program
{
    internal static void __GateEntry()
    {
        Console.WriteLine("== C# 12.0 ==");

        PrimaryConstructors();
        CollectionExpressions();
        CollectionBuilderTarget();
        InlineArrays();
        DefaultLambdaParameters();
        RefReadonlyParameters();
        UsingAliases();
    }

    private static void PrimaryConstructors()
    {
        Person p = new Person("ada", 36);
        Console.WriteLine("primary ctor (class): " + p.Describe() + " next=" + p.NextYear);

        Pt pt = new Pt(3, 4);
        Console.WriteLine("primary ctor (struct): " + pt + " X=" + pt.X + " Y=" + pt.Y);
        Console.WriteLine("primary ctor (struct) default: " + default(Pt));
    }

    private static void CollectionExpressions()
    {
        int[] arr = [1, 2, 3];
        Console.WriteLine("collection expr -> array: " + string.Join(",", arr));

        List<string> names = ["alpha", "beta", "gamma"];
        Console.WriteLine("collection expr -> List<T>: " + string.Join(",", names));

        Span<int> span = [4, 5, 6];
        Console.WriteLine("collection expr -> Span<T>: " + SumSpan(span));

        ReadOnlySpan<int> ros = [7, 8];
        Console.WriteLine("collection expr -> ReadOnlySpan<T>: " + SumSpan(ros));

        int[] empty = [];
        Console.WriteLine("collection expr -> empty: length=" + empty.Length);

        int[] tail = [10, 11];
        int[] spread = [.. arr, .. tail, 99];
        Console.WriteLine("collection expr spread: " + string.Join(",", spread));

        List<string> moreNames = [.. names, "delta"];
        Console.WriteLine("collection expr spread (List<T>): " + string.Join(",", moreNames));
    }

    private static int SumSpan(ReadOnlySpan<int> s)
    {
        int total = 0;
        foreach (int v in s)
        {
            total += v;
        }

        return total;
    }

    private static void CollectionBuilderTarget()
    {
        MyCol<int> mine = [1, 2, 3, 4];
        Console.WriteLine("CollectionBuilder target: Count=" + mine.Count + " items=" + string.Join(",", mine));

        MyCol<string> strs = ["x", .. new[] { "y", "z" }];
        Console.WriteLine("CollectionBuilder + spread: " + string.Join(",", strs));

        MyCol<int> none = [];
        Console.WriteLine("CollectionBuilder empty: Count=" + none.Count);
    }

    private static void InlineArrays()
    {
        Buf4 buf = default;
        for (int i = 0; i < 4; i++)
        {
            buf[i] = (i + 1) * 10;
        }

        Console.WriteLine($"inline array indexer: {buf[0]},{buf[1]},{buf[2]},{buf[3]}");

        Span<int> asSpan = buf;
        Console.WriteLine("inline array as Span<T>: length=" + asSpan.Length + " sum=" + SumSpan(asSpan));

        asSpan[2] = 99;
        Console.WriteLine("inline array write through span: " + buf[2]);
    }

    private static void DefaultLambdaParameters()
    {
        var doubler = (int x = 5) => x * 2;
        Console.WriteLine($"lambda default parameter: f()={doubler()} f(3)={doubler(3)}");

        var join = (string sep = "-", string a = "L", string b = "R") => a + sep + b;
        Console.WriteLine($"lambda defaults (several): {join()} {join("+")} {join("+", "x", "y")}");
    }

    private static void RefReadonlyParameters()
    {
        Pt pt = new Pt(3, 4);
        Console.WriteLine("ref readonly parameter: " + NormSquared(in pt));
    }

    private static int NormSquared(ref readonly Pt p)
    {
        return (p.X * p.X) + (p.Y * p.Y);
    }

    private static void UsingAliases()
    {
        Pair pair = (7, "seven");
        Console.WriteLine($"using alias (tuple): {pair.Number}={pair.Name}");

        IntArr aliased = [1, 2, 3];
        Console.WriteLine("using alias (array): " + string.Join(",", aliased));
    }
}
