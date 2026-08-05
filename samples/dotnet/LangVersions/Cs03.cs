// C# 3.0: `var`, object/collection initializers, anonymous types (printing
// ToString() too), lambda expressions, a self-authored extension method, a
// LINQ query expression over a deterministically ordered source,
// auto-properties, and expression-tree CONSTRUCTION.
//
// `Expression<T>.Compile()` is never called here: dn2cpp cuts
// `Expression.Compile` permanently (throws `PlatformNotSupportedException`,
// see samples/dotnet/ReflectTypes/DynamicCodegenSubset.cs, a frozen-snapshot
// gate) — calling it would make this section diverge from real .NET and
// break the byte-for-byte gate this bucket runs under. Tree construction and
// structural reads (NodeType, ToString()) stay fully transpiled and are what
// this section exercises instead.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Cs03;

internal sealed class Person
{
    // auto-properties
    internal string Name { get; set; }
    internal int Age { get; set; }
}

internal static class Extensions
{
    // self-authored extension method
    internal static string Shout(this string s)
    {
        return s.ToUpperInvariant() + "!";
    }
}

internal static class Program
{
    internal static void __GateEntry()
    {
        Console.WriteLine("== C# 3.0 ==");

        // var + object initializer
        var alice = new Person { Name = "Alice", Age = 30 };
        Console.WriteLine("person=" + alice.Name + "/" + alice.Age);

        // collection initializer
        var numbers = new List<int> { 5, 3, 1, 4, 2 };
        Console.WriteLine("numbers=" + string.Join(",", numbers));

        // anonymous type
        var anon = new { X = 1, Y = "a" };
        Console.WriteLine("anon=" + anon.ToString());
        Console.WriteLine("anon.X=" + anon.X + " anon.Y=" + anon.Y);

        // lambda expression
        Func<int, int> square = x => x * x;
        Console.WriteLine("square(6)=" + square(6));

        // extension method
        Console.WriteLine("shout=" + "hi".Shout());

        // LINQ query expression, deterministic source
        var people = new List<Person>
        {
            new Person { Name = "Carol", Age = 25 },
            new Person { Name = "Bob", Age = 40 },
            new Person { Name = "Alice", Age = 30 }
        };

        var query =
            from p in people
            where p.Age >= 30
            orderby p.Name
            select p.Name + ":" + p.Age;

        foreach (string line in query)
        {
            Console.WriteLine("query: " + line);
        }

        // expression tree construction only (never Compile()d)
        Expression<Func<int, int>> expr = x => x * 2;
        Console.WriteLine("expr.Body.NodeType=" + expr.Body.NodeType);
        Console.WriteLine("expr.ToString()=" + expr.ToString());
    }
}
