using System;

namespace StringTypedComparisonSubset;

internal sealed class ComparableValue : IComparable<ComparableValue>
{
    internal int Value;

    public int CompareTo(ComparableValue other) => other is null ? 1 : Value.CompareTo(other.Value);
}

internal sealed class StringComparable : IComparable<string>
{
    int IComparable<string>.CompareTo(string other) => 17;
}

internal static class Program
{
    private static int Cast<T>(T value, T other) => ((IComparable<T>)value).CompareTo(other);

    private static int Held<T>(IComparable<T> value, T other) => value.CompareTo(other);

    private static int Constrained<T>(T value, T other) where T : IComparable<T> => value.CompareTo(other);

    private static string Try(Func<int> call)
    {
        try
        {
            return Math.Sign(call()).ToString();
        }
        catch (NullReferenceException)
        {
            return "NRE";
        }
    }

    internal static void Run()
    {
        Console.WriteLine("== string typed interface dispatch ==");
        string value = new string(new[] { 'm', 'a', 'n', 'g', 'o' });
        string none = null;
        IComparable<string> held = value;
        IComparable<string> missing = null;
        Console.WriteLine("direct string compare: " + Try(() => value.CompareTo("apple")) + " "
            + Try(() => value.CompareTo("mango")) + " " + Try(() => value.CompareTo("zebra"))
            + " " + Try(() => value.CompareTo(none)) + " " + Try(() => none.CompareTo(value)));
        Console.WriteLine("held string compare: " + Try(() => held.CompareTo("apple")) + " "
            + Try(() => held.CompareTo("mango")) + " " + Try(() => held.CompareTo("zebra"))
            + " " + Try(() => held.CompareTo(none)) + " " + Try(() => missing.CompareTo(value)));
        Console.WriteLine("generic cast string compare: " + Try(() => Cast(value, "apple")) + " "
            + Try(() => Cast(value, "mango")) + " " + Try(() => Cast(value, "zebra"))
            + " " + Try(() => Cast(value, none)) + " " + Try(() => Cast(none, value)));
        Console.WriteLine("generic held string compare: " + Try(() => Held(held, "apple")) + " "
            + Try(() => Held(held, "mango")) + " " + Try(() => Held(held, "zebra"))
            + " " + Try(() => Held(held, none)) + " " + Try(() => Held(missing, value)));
        Console.WriteLine("constrained string compare: " + Try(() => Constrained(value, "apple")) + " "
            + Try(() => Constrained(value, "mango")) + " " + Try(() => Constrained(value, "zebra"))
            + " " + Try(() => Constrained(value, none)) + " " + Try(() => Constrained(none, value)));
        var key = new ComparableValue { Value = 3 };
        var equal = new ComparableValue { Value = 3 };
        Console.WriteLine("other shared comparison: " + Cast(key, equal) + " " + Held(key, equal)
            + " " + Constrained(key, equal) + " " + Cast(key, null) + " " + Constrained(key, null));
        var custom = new StringComparable();
        Console.WriteLine("custom typed string comparison: " + Held<string>(custom, null));
        object boxed = value;
        Console.WriteLine("string comparison relations: " + (boxed is IComparable<string>) + " "
            + (boxed is IComparable<object>) + " " + (boxed is IComparable<ComparableValue>));
        Console.WriteLine("string typed interface dispatch end");
    }
}
