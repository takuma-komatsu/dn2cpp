#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;

// LINQ over a string as IEnumerable<char>: dispatch goes through String's runtime
// interface map, since no cast instruction exists at these call sites — the
// conversion is a call-boundary coercion.
namespace StringLinqSubset;

internal static class Program
{
    internal static int Run()
    {
        string s = "Hello_World42";

        // Predicates and counting.
        Console.WriteLine($"all={s.All(c => char.IsAsciiLetterOrDigit(c) || c == '_')}");
        Console.WriteLine($"any={s.Any(char.IsWhiteSpace)}");
        Console.WriteLine($"count={s.Count()}/{s.Count(char.IsDigit)}");
        Console.WriteLine($"contains={s.Contains('W')}/{s.Contains('z')}");

        // Element access.
        Console.WriteLine($"first={s.First()}/{s.First(char.IsLower)}");
        Console.WriteLine($"last={s.Last()}/{s.Last(char.IsLetter)}");
        Console.WriteLine($"elementAt={s.ElementAt(6)}");
        Console.WriteLine($"single={"x".Single()}");

        // Projection / aggregation.
        Console.WriteLine($"sum={s.Select(c => (int)c).Sum()}");
        Console.WriteLine($"minmax={s.Min()}/{s.Max()}");
        Console.WriteLine($"aggregate={s.Aggregate(0, (acc, c) => acc * 31 + c)}");

        // Filtering / ordering / set ops, materialized back into strings.
        Console.WriteLine($"where={string.Concat(s.Where(char.IsUpper))}");
        Console.WriteLine($"reverse={string.Concat(s.Reverse())}");
        Console.WriteLine($"distinct={string.Concat("aabbccaa".Distinct())}");
        IEnumerable<char> ordered = s.OrderBy(c => c);
        Console.WriteLine($"orderBy={string.Concat(ordered)}");
        Console.WriteLine($"skipTake={string.Concat(s.Skip(6).Take(5))}");

        // Sequence comparison between two strings-as-sequences.
        Console.WriteLine($"seqEqual={"abc".SequenceEqual("abc")}/{"abc".SequenceEqual("abd")}");

        // Join over enumerated chars, and ToArray back into a string.
        Console.WriteLine($"join={string.Join("-", "abc".AsEnumerable())}");
        Console.WriteLine($"toArray={new string(s.Where(char.IsLetter).ToArray())}");

        // Method-group delegate over an intrinsic-typed static: the address-taken form,
        // synthesized from the intrinsic's lowering.
        Func<char, bool> isDigit = char.IsDigit;
        Console.WriteLine($"methodGroup={s.Where(isDigit).Count()}");
        return 0;
    }
}
