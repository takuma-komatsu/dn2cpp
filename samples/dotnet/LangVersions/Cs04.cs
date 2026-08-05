// C# 4.0: named arguments, optional parameters (with default values), and
// generic interface variance (`out T` covariance / `in T` contravariance) on
// self-authored interfaces, plus the same covariance already present on the
// BCL's `IEnumerable<out T>`.
//
// `dynamic` is never used in this file: the DLR CallSite it lowers to is a
// permanent carve-out in dn2cpp, and that cut being loud (not a silent
// fallback) is exactly what samples/dotnet/ReflectTypes/DynamicCodegenSubset.cs
// (a frozen-snapshot gate) asserts. Using `dynamic` here would make this
// section diverge from real .NET and break the byte-for-byte gate this
// bucket runs under.
using System;
using System.Collections.Generic;

namespace Cs04;

internal class Animal
{
    internal virtual string Name
    {
        get { return "animal"; }
    }
}

internal sealed class Cat : Animal
{
    internal override string Name
    {
        get { return "cat"; }
    }
}

internal interface ICovariant<out T>
{
    T Get();
}

internal sealed class Box<T> : ICovariant<T>
{
    private readonly T _value;

    internal Box(T value)
    {
        _value = value;
    }

    public T Get()
    {
        return _value;
    }
}

internal interface IContravariant<in T>
{
    string Describe(T value);
}

internal sealed class AnimalDescriber : IContravariant<Animal>
{
    public string Describe(Animal value)
    {
        return "described:" + value.Name;
    }
}

internal static class Program
{
    private static string Greet(string name, string greeting = "Hello", int times = 1)
    {
        string result = "";
        for (int i = 0; i < times; i++)
        {
            result += greeting + ", " + name + "! ";
        }
        return result.Trim();
    }

    internal static void __GateEntry()
    {
        Console.WriteLine("== C# 4.0 ==");

        // named arguments
        Console.WriteLine(Greet(name: "World", times: 2));

        // optional/default arguments
        Console.WriteLine(Greet("Alice"));
        Console.WriteLine(Greet("Bob", greeting: "Hi"));

        // covariance: ICovariant<Cat> assignable to ICovariant<Animal>
        ICovariant<Cat> catBox = new Box<Cat>(new Cat());
        ICovariant<Animal> animalBox = catBox;
        Console.WriteLine("covariant.Get().Name=" + animalBox.Get().Name);

        // contravariance: IContravariant<Animal> assignable to IContravariant<Cat>
        IContravariant<Animal> animalDescriber = new AnimalDescriber();
        IContravariant<Cat> catDescriber = animalDescriber;
        Console.WriteLine(catDescriber.Describe(new Cat()));

        // nested variance: a variant argument that is itself a variant instantiation.
        // ICovariant<ICovariant<Cat>> assignable to ICovariant<ICovariant<Animal>>, and
        // the .Get().Get() chain dispatches through two variant rows.
        ICovariant<ICovariant<Cat>> nestedCatBox = new Box<ICovariant<Cat>>(new Box<Cat>(new Cat()));
        ICovariant<ICovariant<Animal>> nestedAnimalBox = nestedCatBox;
        Console.WriteLine("nested.Get().Get().Name=" + nestedAnimalBox.Get().Get().Name);

        // IEnumerable<out T> covariance from the BCL
        List<Cat> cats = new List<Cat> { new Cat(), new Cat() };
        IEnumerable<Animal> animals = cats;
        int count = 0;
        foreach (Animal a in animals)
        {
            count++;
        }
        Console.WriteLine("animals.count=" + count);
    }
}
