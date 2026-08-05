// C# 11: raw string literals (plain and interpolated), static abstract interface
// members / generic math, generic attributes, UTF-8 string literals, list and
// slice patterns, file-local types, `required` members, auto-default structs,
// constant patterns over ReadOnlySpan<char>, `scoped`, `ref` fields, user-defined
// `checked` operators, and the unsigned right shift `>>>`.
using System;
using System.Numerics;

namespace Cs11;

// C# 11 file-local type: visible only inside this file, so the name cannot clash
// with a `Hidden` in any sibling section.
file class Hidden
{
    public static string Who()
    {
        return "Cs11.Hidden (file-local)";
    }
}

// C# 11 generic attribute.
internal sealed class MyAttr<T> : Attribute
{
}

[MyAttr<int>]
internal sealed class Tagged
{
}

// C# 11 static abstract interface members: the shape behind generic math.
internal interface IAddable<TSelf> where TSelf : IAddable<TSelf>
{
    static abstract TSelf operator +(TSelf a, TSelf b);

    static abstract TSelf Zero { get; }
}

// TSelf is a reference class, which is what the canonical-shared static-virtual
// dispatch supports (the other supported shape is an integer primitive, covered
// below by INumber<int>).
internal sealed class Vec2 : IAddable<Vec2>
{
    public Vec2(int x, int y)
    {
        X = x;
        Y = y;
    }

    public int X { get; }

    public int Y { get; }

    public static Vec2 Zero => new Vec2(0, 0);

    public static Vec2 operator +(Vec2 a, Vec2 b) => new Vec2(a.X + b.X, a.Y + b.Y);

    public override string ToString() => $"({X},{Y})";
}

// C# 11 user-defined checked operator: `checked(a + b)` binds to the `checked`
// form, everything else to the plain one.
internal struct Money
{
    public int Cents;

    public Money(int cents)
    {
        Cents = cents;
    }

    public static Money operator +(Money a, Money b) => new Money(unchecked(a.Cents + b.Cents));

    public static Money operator checked +(Money a, Money b) => new Money(checked(a.Cents + b.Cents));
}

// C# 11 auto-default struct: the one-argument constructor leaves `Unset` alone and
// the compiler zero-initializes it, where C# 10 would have rejected the constructor
// with "field must be fully assigned". The two-argument constructor exists only so
// that the field has some assignment somewhere and does not draw CS0649.
internal struct AutoDefault
{
    public int Set;

    public int Unset;

    public AutoDefault(int set)
    {
        Set = set;
    }

    public AutoDefault(int set, int unset)
    {
        Set = set;
        Unset = unset;
    }
}

internal sealed class Config
{
    public required int Port { get; init; }

    public string Host { get; init; }
}

// C# 11 `ref` field, only legal in a ref struct.
internal ref struct Counter
{
    public ref int Value;

    public Counter(ref int value)
    {
        Value = ref value;
    }

    public void Bump()
    {
        Value++;
    }
}

internal static class Program
{
    internal static void __GateEntry()
    {
        Console.WriteLine("== C# 11.0 ==");

        RawStrings();
        GenericMath();
        GenericAttributes();
        Utf8Literals();
        ListPatterns();
        FileLocalTypes();
        RequiredMembers();
        AutoDefaultStructs();
        SpanPatterns();
        ScopedAndRefFields();
        CheckedOperators();
        UnsignedRightShift();
        NestedReflectionNames();
    }

    // A compiler-generated lambda display class is a NESTED type, so
    // Assembly.GetTypes() must report it under the CLR '+' syntax
    // ("Cs11.Program+<>c"), not the bare metadata simple name "<>c". Scoped to
    // this section's namespace so the print cannot depend on which lambdas the
    // sibling sections happen to compile into display classes; this is a diff
    // gate, so the line is byte-compared against real .NET.
    private static void NestedReflectionNames()
    {
        Func<int, int> twice = static x => x * 2;
        Console.WriteLine("twice(21) => " + twice(21));

        var displayClasses = new System.Collections.Generic.List<string>();
        foreach (Type t in typeof(Program).Assembly.GetTypes())
        {
            if (t.FullName.StartsWith("Cs11.", StringComparison.Ordinal)
                && t.FullName.Contains("+<>c"))
            {
                displayClasses.Add(t.FullName);
            }
        }
        displayClasses.Sort(StringComparer.Ordinal);
        Console.WriteLine("display classes => " + string.Join(", ", displayClasses));
    }

    private static void RawStrings()
    {
        // No escaping needed for the embedded quotes.
        string json = """{"name":"dn2cpp","note":"a \"quote\" stays literal"}""";
        Console.WriteLine("raw single-line: " + json);

        // The closing delimiter's indentation is stripped from every line.
        string block = """
            line one
              indented two
            line three
            """;
        Console.WriteLine("raw multi-line: " + Flatten(block));

        // $$ raises the interpolation delimiter to {{ }}, so a single brace is literal.
        int count = 3;
        string interpolated = $$"""
            { "count": {{count}}, "literal": {braces} }
            """;
        Console.WriteLine("raw interpolated: " + Flatten(interpolated));
    }

    // Line endings inside a raw string literal come from the source file, so
    // normalize before printing: the gate must not depend on the checkout's
    // newline style.
    private static string Flatten(string s)
    {
        return s.Replace("\r\n", "\n").Replace('\n', '|');
    }

    private static void GenericMath()
    {
        Vec2 sum = SumAll(new[] { new Vec2(1, 2), new Vec2(3, 4), new Vec2(5, 6) });
        Console.WriteLine("static abstract operator +: " + sum);
        Console.WriteLine("static abstract Zero: " + Vec2.Zero);

        // The same shape against the BCL's generic math, over an integer primitive.
        Console.WriteLine("INumber<int> sum: " + SumNumbers(new[] { 1, 2, 3, 4 }));
    }

    private static T SumAll<T>(T[] xs) where T : IAddable<T>
    {
        T acc = T.Zero;
        foreach (T x in xs)
        {
            acc = acc + x;
        }

        return acc;
    }

    private static T SumNumbers<T>(T[] xs) where T : INumber<T>
    {
        T acc = T.Zero;
        foreach (T x in xs)
        {
            acc += x;
        }

        return acc;
    }

    private static void GenericAttributes()
    {
        // IsDefined, not GetCustomAttributes: no dependence on attribute order.
        Console.WriteLine("generic attribute MyAttr<int>: " + Attribute.IsDefined(typeof(Tagged), typeof(MyAttr<int>)));
        Console.WriteLine("generic attribute MyAttr<string>: " + Attribute.IsDefined(typeof(Tagged), typeof(MyAttr<string>)));
    }

    private static void Utf8Literals()
    {
        // The \u escape keeps this file pure ASCII while still producing multi-byte UTF-8.
        ReadOnlySpan<byte> bytes = "caf\u00e9"u8;
        Console.Write("utf-8 literal bytes:");
        for (int i = 0; i < bytes.Length; i++)
        {
            Console.Write(" " + bytes[i].ToString("X2"));
        }

        Console.WriteLine();
        Console.WriteLine("utf-8 literal length: " + bytes.Length);
    }

    private static void ListPatterns()
    {
        int[] arr = new[] { 1, 2, 3, 4, 5 };

        Console.WriteLine("list pattern [1, 2, ..]: " + (arr is [1, 2, ..]));
        Console.WriteLine("list pattern [1, .., 5]: " + (arr is [1, .., 5]));
        Console.WriteLine("list pattern [_, >= 3, _]: " + (new[] { 9, 4, 7 } is [_, >= 3, _]));
        Console.WriteLine("list pattern [_, >= 3, _] (no): " + (new[] { 9, 1, 7 } is [_, >= 3, _]));

        if (arr is [var head, .. var rest])
        {
            Console.WriteLine($"slice pattern: head={head} rest.Length={rest.Length} rest[0]={rest[0]} rest[^1]={rest[^1]}");
        }

        if (arr is [_, _, .. var middle, _])
        {
            Console.WriteLine("slice pattern middle: " + string.Join(",", middle));
        }

        Console.WriteLine("empty list pattern: " + (Array.Empty<int>() is []));
    }

    private static void FileLocalTypes()
    {
        Console.WriteLine("file-local type: " + Hidden.Who());
    }

    private static void RequiredMembers()
    {
        Config cfg = new Config { Port = 8080, Host = "localhost" };
        Console.WriteLine($"required member: {cfg.Host}:{cfg.Port}");
    }

    private static void AutoDefaultStructs()
    {
        AutoDefault ad = new AutoDefault(7);
        Console.WriteLine($"auto-default struct: Set={ad.Set} Unset={ad.Unset}");

        AutoDefault both = new AutoDefault(7, 9);
        Console.WriteLine($"fully assigned struct: Set={both.Set} Unset={both.Unset}");
    }

    private static void SpanPatterns()
    {
        ReadOnlySpan<char> span = "abc".AsSpan();
        Console.WriteLine("span constant pattern (abc): " + (span is "abc"));
        Console.WriteLine("span constant pattern (xyz): " + (span is "xyz"));

        string verdict = span switch
        {
            "abc" => "matched abc",
            "def" => "matched def",
            _ => "no match",
        };
        Console.WriteLine("span switch: " + verdict);
    }

    private static void ScopedAndRefFields()
    {
        ReadOnlySpan<int> nums = new[] { 10, 20, 30 };
        Console.WriteLine("scoped ReadOnlySpan param: " + SumScoped(nums));

        int cell = 5;
        BumpScoped(ref cell);
        Console.WriteLine("scoped ref param: " + cell);

        Counter counter = new Counter(ref cell);
        counter.Bump();
        counter.Bump();
        Console.WriteLine("ref field writes through: " + cell);
    }

    private static int SumScoped(scoped ReadOnlySpan<int> s)
    {
        int total = 0;
        foreach (int v in s)
        {
            total += v;
        }

        return total;
    }

    private static void BumpScoped(scoped ref int r)
    {
        r += 10;
    }

    private static void CheckedOperators()
    {
        Money big = new Money(int.MaxValue);
        Money one = new Money(1);

        Money wrapped = big + one;
        Console.WriteLine("plain operator + wraps: " + wrapped.Cents);

        try
        {
            Money boom = checked(big + one);
            Console.WriteLine("checked operator + did not throw: " + boom.Cents);
        }
        catch (OverflowException)
        {
            Console.WriteLine("checked operator + threw OverflowException");
        }
    }

    private static void UnsignedRightShift()
    {
        int neg = -8;
        Console.WriteLine($"-8 >> 1 = {neg >> 1}, -8 >>> 1 = {neg >>> 1}");

        int shifted = -16;
        shifted >>>= 2;
        Console.WriteLine("-16 >>>= 2 => " + shifted);

        long negLong = -8L;
        Console.WriteLine("-8L >>> 1 = " + (negLong >>> 1));
    }
}
