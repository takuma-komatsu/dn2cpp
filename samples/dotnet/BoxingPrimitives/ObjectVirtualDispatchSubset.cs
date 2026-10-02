using System;
using System.Runtime.CompilerServices;

// Base calls to the Object and ValueType virtuals from inside an override. A base
// call is non-virtual, so it runs the base body instead of dispatching back into the
// override: Object's type name, reference equality and identity hash, and ValueType's
// type name and field-by-field equality and hash. The identity hash is one function:
// RuntimeHelpers.GetHashCode agrees with the default Object.GetHashCode and is
// non-negative. A boxed struct formats through its ToString override wherever the
// box is formatted, and a method group over an Object virtual runs what a callvirt
// runs on a class that does not override it, a boxed value, a string and an array.
//
// RunEqualsOverrideArguments: a virtual call of Equals(object) runs the override for
// every argument, the receiver itself and null included, through a class, a boxed
// struct, a method group and Attribute.Match, while static Object.Equals answers
// those two without calling it. A box runs its struct's Equals and GetHashCode
// overrides when nothing else calls them.
//
// RunFrameworkBoxEquality: so does a box of a framework struct — tuples, nested ones
// included, Index, Range, Int128, Half, Rune, Vector3, ArraySegment<T> and Memory<T> —
// and the field walk of a struct that overrides neither runs a reference field's own
// Equals for an identical or null operand too, a null field equalling only null.
namespace ObjectVirtualDispatchSubset;

internal struct RefHolder
{
    public object Ref;

    public RefHolder(object reference)
    {
        Ref = reference;
    }
}

internal sealed class BaseCalls
{
    public override string ToString() => "base-calls:" + base.ToString();

    public override bool Equals(object obj) => base.Equals(obj);

    public override int GetHashCode() => base.GetHashCode();
}

internal sealed class Plain
{
}

internal sealed class Named
{
    public override string ToString() => "named";
}

// Each struct below is boxed where no formatting call follows the box.
internal struct Stashed
{
    public int X;

    public Stashed(int x)
    {
        X = x;
    }

    public override string ToString() => "stashed:" + X;
}

internal struct Digit
{
    public int X;

    public Digit(int x)
    {
        X = x;
    }

    public override string ToString() => X.ToString();
}

internal struct Grouped
{
    public int X;

    public Grouped(int x)
    {
        X = x;
    }

    public override string ToString() => "grouped:" + X;
}

internal interface ILabel
{
    string Label();
}

// Boxed only through Nullable<T>, whose box is a box of the struct itself.
internal struct Wrapped : ILabel
{
    public int X;

    public Wrapped(int x)
    {
        X = x;
    }

    public string Label() => "label:" + X;

    public override string ToString() => "wrapped:" + X;
}

internal struct Pair
{
    public int A;
    public int B;

    public Pair(int a, int b)
    {
        A = a;
        B = b;
    }
}

internal enum Tone
{
    Low,
    High,
}

// The string field keeps .NET off its bitwise fast path, so the base Equals compares
// field by field.
internal struct StructBaseCalls
{
    public int X;
    public string S;

    public StructBaseCalls(int x, string s)
    {
        X = x;
        S = s;
    }

    public override string ToString() => "struct:" + base.ToString();

    public override bool Equals(object obj) => base.Equals(obj);

    public override int GetHashCode() => base.GetHashCode();
}

internal sealed class AlwaysEqual
{
    public override bool Equals(object obj) => true;

    public override int GetHashCode() => 1;
}

internal sealed class NeverEqual
{
    public override bool Equals(object obj) => false;

    public override int GetHashCode() => 2;
}

internal sealed class CountingEqual
{
    public static int Calls;

    public override bool Equals(object obj)
    {
        Calls++;
        return ReferenceEquals(this, obj);
    }

    public override int GetHashCode() => 3;
}

internal sealed class StrictEqual
{
    private readonly int _value = 4;

    public override bool Equals(object obj) => ((StrictEqual)obj)._value == _value;

    public override int GetHashCode() => _value;
}

internal struct AlwaysEqualStruct
{
    public override bool Equals(object obj) => true;

    public override int GetHashCode() => 5;
}

[AttributeUsage(AttributeTargets.All)]
internal sealed class AlwaysEqualAttribute : Attribute
{
    public override bool Equals(object obj) => true;

    public override int GetHashCode() => 6;
}

internal static class Program
{
    internal static void Run()
    {
        Console.WriteLine("== object virtual dispatch ==");
        var a = new BaseCalls();
        var b = new BaseCalls();
        Console.WriteLine("class base ToString: " + a.ToString());
        Console.WriteLine("class base Equals: " + a.Equals(a) + "/" + a.Equals(b) + "/" + a.Equals(null));
        Console.WriteLine("class base GetHashCode: " + (a.GetHashCode() == RuntimeHelpers.GetHashCode(a)));
        var p = new Plain();
        Console.WriteLine("identity hash: " + (RuntimeHelpers.GetHashCode(p) == p.GetHashCode())
            + "/" + (RuntimeHelpers.GetHashCode(p) >= 0) + "/" + (RuntimeHelpers.GetHashCode(null) == 0));
        var s1 = new StructBaseCalls(1, "x");
        var s2 = new StructBaseCalls(1, "x");
        var s3 = new StructBaseCalls(2, "x");
        Console.WriteLine("struct base ToString: " + s1.ToString());
        Console.WriteLine("struct base Equals: " + s1.Equals(s2) + "/" + s1.Equals(s3)
            + "/" + s1.Equals(null) + "/" + s1.Equals(1));
        Console.WriteLine("struct base GetHashCode: " + (s1.GetHashCode() == s2.GetHashCode()));
        object stashed = new Stashed(4);
        Console.WriteLine("boxed struct ToString: " + stashed.ToString());
        object[] digits = { new Digit(5), new Digit(6) };
        Console.WriteLine("boxed struct join: joined:" + string.Join(",", digits));
        Wrapped? maybe = new Wrapped(7);
        object wrapped = maybe;
        Console.WriteLine("boxed nullable struct: " + wrapped.ToString() + "/" + ((ILabel)wrapped).Label());
        object plain = new Plain();
        object named = new Named();
        object five = 5;
        object grouped = new Grouped(3);
        object pair = new Pair(1, 2);
        object text = "text";
        object numbers = new int[2];
        object tone = Tone.High;
        Func<string>[] texts =
        {
            plain.ToString, named.ToString, five.ToString, grouped.ToString,
            pair.ToString, text.ToString, numbers.ToString, tone.ToString,
        };
        var shown = new string[texts.Length];
        for (int i = 0; i < texts.Length; i++)
            shown[i] = texts[i]();
        Console.WriteLine("method group ToString: " + string.Join("/", shown));
        Func<object, bool> plainEquals = plain.Equals;
        Func<object, bool> pairEquals = pair.Equals;
        Func<object, bool> textEquals = text.Equals;
        Func<int> plainHash = plain.GetHashCode;
        object eleven = 11L;
        Func<int> elevenHash = eleven.GetHashCode;
        Func<int> fiveHash = five.GetHashCode;
        Console.WriteLine("method group Equals/GetHashCode: " + plainEquals(plain) + "/" + plainEquals(new Plain())
            + "/" + pairEquals(new Pair(1, 2)) + "/" + pairEquals(new Pair(2, 1))
            + "/" + (plainHash() == RuntimeHelpers.GetHashCode(plain)) + "/" + elevenHash() + "/" + fiveHash()
            + "/" + textEquals(string.Concat("te", "xt")) + "/" + textEquals(numbers));
    }

    private static string Try(Func<bool> equals)
    {
        try
        {
            return equals().ToString();
        }
        catch (Exception ex)
        {
            return ex.GetType().Name;
        }
    }

    internal static void RunEqualsOverrideArguments()
    {
        Console.WriteLine("== Equals override arguments ==");
        var always = new AlwaysEqual();
        object boxedAlways = always;
        Console.WriteLine("override, null argument: " + always.Equals(null) + " " + boxedAlways.Equals(null));
        var never = new NeverEqual();
        object boxedNever = never;
        Console.WriteLine("override, itself: " + never.Equals(never) + " " + boxedNever.Equals(boxedNever));
        var counting = new CountingEqual();
        bool self = counting.Equals(counting);
        bool none = counting.Equals(null);
        Console.WriteLine("override, calls: " + self + " " + none + " " + CountingEqual.Calls);
        var strict = new StrictEqual();
        Console.WriteLine("override, faulting on null: " + Try(() => strict.Equals(null))
            + " " + Try(() => strict.Equals(new StrictEqual())));
        ValueType value = new AlwaysEqualStruct();
        object boxedValue = new AlwaysEqualStruct();
        Console.WriteLine("struct override through a box: " + value.Equals(null) + " " + boxedValue.Equals(null)
            + " " + boxedValue.GetHashCode());
        Func<object, bool> group = boxedAlways.Equals;
        Console.WriteLine("method group, null argument: " + group(null));
        Console.WriteLine("Attribute.Match, null argument: " + new AlwaysEqualAttribute().Match(null));
        Console.WriteLine("static Object.Equals: " + Equals(always, null) + " " + Equals(never, never));
        Console.WriteLine("Equals override arguments end");
    }

    private static readonly int[] Shared = { 1, 2, 3 };

    private static object[] FrameworkBoxes() => new object[]
    {
        new Index(3, fromEnd: true), 1..^2, (Int128)5, (Half)1.5, new System.Text.Rune('r'),
        new System.Numerics.Vector3(1, 2, 3), new ArraySegment<int>(Shared, 1, 2), new Memory<int>(Shared),
    };

    internal static void RunFrameworkBoxEquality()
    {
        Console.WriteLine("== framework box equality ==");
        object pair = (1, 2), samePair = (1, 2), otherPair = (1, 3);
        Console.WriteLine("tuple: " + pair.Equals(samePair) + " " + pair.Equals(otherPair)
            + " " + (pair.GetHashCode() == samePair.GetHashCode()) + " " + Equals(pair, samePair));
        object nested = (1, ("a", 2.5)), sameNested = (1, ("a", 2.5));
        Console.WriteLine("nested tuple: " + nested.Equals(sameNested) + " " + (nested.GetHashCode() == sameNested.GetHashCode()));
        object[] left = FrameworkBoxes(), right = FrameworkBoxes();
        var rows = new string[left.Length];
        for (int i = 0; i < left.Length; i++)
            rows[i] = left[i].Equals(right[i]) + ":" + (left[i].GetHashCode() == right[i].GetHashCode());
        Console.WriteLine("framework structs: " + string.Join(" ", rows));
        int before = CountingEqual.Calls;
        var counting = new CountingEqual();
        object holder = new RefHolder(counting), sameHolder = new RefHolder(counting);
        Console.WriteLine("field walk, identical reference: " + holder.Equals(sameHolder) + " " + (CountingEqual.Calls - before));
        object always = new RefHolder(new AlwaysEqual()), empty = new RefHolder(null);
        Console.WriteLine("field walk, null field: " + always.Equals(empty) + " " + empty.Equals(always)
            + " " + empty.Equals(new RefHolder(null)));
        Console.WriteLine("framework box equality end");
    }
}
