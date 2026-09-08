#nullable enable
using System;
using System.Collections.Generic;
using GenericVarianceDispatchSubset;

namespace InterfaceClosureDispatchSubset;

internal interface IPair<out T>
{
    T First();
    T Second();
}

internal interface ILeft<out T> : IPair<T> { }
internal interface IRight<out T> : IPair<T> { }
internal interface IDiamond<out T> : ILeft<T>, IRight<T> { }

internal class PairBase<T> : IDiamond<T>
{
    private readonly T _value;

    internal PairBase(T value) { _value = value; }
    public T First()
    {
        return _value;
    }

    public T Second()
    {
        return _value;
    }
}

internal sealed class Diamond<T> : PairBase<T>
{
    internal Diamond(T value) : base(value) { }
}

internal interface IConsumer<in T>
{
    string First(T value);
    string Second(T value);
}

internal sealed class AnimalConsumer : IConsumer<Animal>
{
    public string First(Animal value)
    {
        return "first:" + value.Name;
    }

    public string Second(Animal value)
    {
        return "second:" + value.Name;
    }
}

internal class LengthComparer : EqualityComparer<string>
{
    public override bool Equals(string? x, string? y)
    {
        return x?.Length == y?.Length;
    }

    public override int GetHashCode(string value)
    {
        return value.Length;
    }
}

internal sealed class DerivedLengthComparer : LengthComparer { }

internal static class Program
{
    internal static void Run()
    {
        Console.WriteLine("-- interface closure dispatch --");
        var diamond = new Diamond<Cat>(new Cat());
        IPair<Cat> exact = diamond;
        Console.WriteLine("diamond exact: " + exact.First().Name + "/" + exact.Second().Name);

        // Both slots must survive an exact miss through the same inherited diamond.
        IPair<Animal> wide = diamond;
        Console.WriteLine("diamond variant: " + wide.First().Name + "/" + wide.Second().Name);
        ILeft<Animal> left = diamond;
        IRight<Animal> right = diamond;
        Console.WriteLine("diamond parents: " + left.First().Name + "/" + right.Second().Name);

        // The outer argument is a class whose interface closure supplies the inner match.
        object nested = new Diamond<Diamond<Cat>>(diamond);
        var nestedWide = (IPair<IPair<Animal>>)nested;
        Console.WriteLine("diamond nested first: " + nestedWide.First().First().Name
            + "/" + nestedWide.First().Second().Name);
        Console.WriteLine("diamond nested second: " + nestedWide.Second().First().Name
            + "/" + nestedWide.Second().Second().Name);
        Console.WriteLine("diamond nested reverse: "
            + ((object)new Diamond<IPair<Animal>>(wide) is IPair<IPair<Cat>>));
        Console.WriteLine("diamond value invariant: " + ((object)new Diamond<int>(7) is IPair<object>));

        var consumer = new AnimalConsumer();
        IPair<IConsumer<Cat>> consumers = new Diamond<AnimalConsumer>(consumer);
        Console.WriteLine("diamond nested consumer: " + consumers.First().First(new Cat())
            + "/" + consumers.Second().Second(new Cat()));

        // IList<T> is invariant; only the reference-element array fallback serves this.
        object array = new Diamond<Cat>[] { diamond };
        var list = (IList<IPair<Animal>>)array;
        Console.WriteLine("diamond array: " + list.Count + "/" + list[0].First().Name
            + "/" + list[0].Second().Name);
        var sequence = (IEnumerable<IPair<Animal>>)array;
        foreach (var pair in sequence)
            Console.WriteLine("diamond array enumeration: " + pair.First().Name + "/" + pair.Second().Name);

        // EqualityComparer's intrinsic base acquires its interface at allocation reach.
        // Exercise the derived closure before and after allocating the concrete base.
        var derived = new DerivedLengthComparer();
        IPair<IEqualityComparer<string>> derivedPair = new Diamond<DerivedLengthComparer>(derived);
        PrintComparer("derived comparer", derivedPair);
        IPair<IEqualityComparer<string>> basePair = new Diamond<LengthComparer>(new LengthComparer());
        PrintComparer("base comparer", basePair);
        PrintComparer("derived comparer again", derivedPair);
    }

    private static void PrintComparer(string label, IPair<IEqualityComparer<string>> pair)
    {
        Console.WriteLine(label + ": " + pair.First().Equals("cat", "dog")
            + "/" + pair.Second().Equals("cat", "lion") + "/" + pair.Second().GetHashCode("horse"));
    }
}
