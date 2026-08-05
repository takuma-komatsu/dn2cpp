// C# 2.0: generic classes/methods with constraints (`class`, `struct`,
// `new()`, an interface constraint), `Nullable<T>` (`??`, `HasValue`/`Value`,
// lifted operators), iterators (`yield return`/`yield break`, including a
// generic iterator), anonymous methods, a class split across two `partial`
// declarations in this file, a static class, delegate covariance/
// contravariance via method-group conversion, and `default(T)`.
using System;
using System.Collections.Generic;

namespace Cs02;

internal interface ITagged
{
    string Tag { get; }
}

internal sealed class Widget : ITagged
{
    public string Tag
    {
        get { return "widget"; }
    }
}

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

internal delegate Animal AnimalFactory();

internal delegate void CatHandler(Cat cat);

// two partial declarations of the same class
internal partial class Recorder
{
    private readonly List<string> _lines = new List<string>();

    internal void Add(string line)
    {
        _lines.Add(line);
    }
}

internal partial class Recorder
{
    internal int Count
    {
        get { return _lines.Count; }
    }

    internal string Join()
    {
        return string.Join("|", _lines.ToArray());
    }
}

internal static class Box<T> where T : class, new()
{
    internal static T Create()
    {
        return new T();
    }
}

internal static class GenericHelpers
{
    internal static T FirstOrDefaultValue<T>(IList<T> list) where T : struct
    {
        if (list.Count == 0)
        {
            return default(T);
        }
        return list[0];
    }

    internal static string DescribeTag<T>(T item) where T : ITagged
    {
        return "tag=" + item.Tag;
    }

    internal static IEnumerable<int> CountUp(int start, int end)
    {
        int i = start;
        while (true)
        {
            if (i > end)
            {
                yield break;
            }
            yield return i;
            i++;
        }
    }

    internal static IEnumerable<T> Repeat<T>(T value, int times)
    {
        for (int i = 0; i < times; i++)
        {
            yield return value;
        }
    }
}

internal static class Program
{
    private static Cat MakeCat()
    {
        return new Cat();
    }

    private static void HandleAnimal(Animal a)
    {
        Console.WriteLine("handled: " + a.Name);
    }

    internal static void __GateEntry()
    {
        Console.WriteLine("== C# 2.0 ==");

        // generics + constraints
        Widget w = Box<Widget>.Create();
        Console.WriteLine("boxed=" + w.Tag);

        List<int> numbers = new List<int>();
        numbers.Add(10);
        numbers.Add(20);
        Console.WriteLine("first=" + GenericHelpers.FirstOrDefaultValue(numbers));
        List<int> empty = new List<int>();
        Console.WriteLine("firstOfEmpty=" + GenericHelpers.FirstOrDefaultValue(empty));
        Console.WriteLine(GenericHelpers.DescribeTag(new Widget()));

        // Nullable<T>
        int? a = 5;
        int? b = null;
        Console.WriteLine("a.HasValue=" + a.HasValue + " a.Value=" + a.Value);
        Console.WriteLine("b??99=" + (b ?? 99));
        int? sum = a + b; // lifted operator: any-null operand yields null
        Console.WriteLine("sum=" + (sum.HasValue ? sum.Value.ToString() : "null"));
        int? sum2 = a + 1; // lifted operator with a non-null operand
        Console.WriteLine("sum2=" + sum2);

        // iterators
        List<int> counted = new List<int>();
        foreach (int i in GenericHelpers.CountUp(1, 5))
        {
            counted.Add(i);
        }
        Console.WriteLine("counted=" + string.Join(",", counted.ToArray()));

        List<string> repeated = new List<string>();
        foreach (string s in GenericHelpers.Repeat("x", 3))
        {
            repeated.Add(s);
        }
        Console.WriteLine("repeated=" + string.Join(",", repeated.ToArray()));

        // anonymous methods
        Converter<int, int> doubler = delegate(int x)
        {
            return x * 2;
        };
        Console.WriteLine("doubled=" + doubler(21));

        // delegate covariance (return type) via method-group conversion:
        // MakeCat returns Cat, assigned where an AnimalFactory (returns Animal) is expected.
        AnimalFactory factory = MakeCat;
        Animal produced = factory();
        Console.WriteLine("produced=" + produced.Name);

        // delegate contravariance (parameter type) via method-group conversion:
        // HandleAnimal accepts Animal, assigned where a CatHandler (accepts Cat) is expected.
        CatHandler handler = HandleAnimal;
        handler(MakeCat());

        // partial class
        Recorder recorder = new Recorder();
        recorder.Add("one");
        recorder.Add("two");
        Console.WriteLine("recorder=" + recorder.Join() + " count=" + recorder.Count);

        // default(T)
        Console.WriteLine("default(int)=" + default(int));
        Console.WriteLine("default(string) is null=" + (default(string) == null));
    }
}
