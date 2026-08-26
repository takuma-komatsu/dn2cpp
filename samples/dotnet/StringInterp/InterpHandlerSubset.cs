#nullable enable
// The DefaultInterpolatedStringHandler 3/4-arg constructors and the
// `string.Create(IFormatProvider?, Span<char>, ref handler)` factory Roslyn emits for an
// interpolated string over a stack-allocated buffer — the shape `KeyValuePair.ToString`
// takes. The handler is the growable Dn2CppISB, so provider/initialBuffer are discarded.
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
namespace InterpHandlerSubset;


class Program
{
    internal static void __GateEntry()
    {
        // KeyValuePair.ToString over a spread of key/value type shapes.
        Console.WriteLine(new KeyValuePair<int, string>(7, "hi"));
        Console.WriteLine(new KeyValuePair<string, int>("k", 42));
        Console.WriteLine(new KeyValuePair<double, bool>(3.5, true));

        // Direct ToString, and the same pair through a statically-typed hole.
        var kvp = new KeyValuePair<int, int>(1, 2);
        Console.WriteLine(kvp.ToString());
        Console.WriteLine($"pair={kvp}");

        // KeyValuePair arriving from Dictionary enumeration.
        var d = new Dictionary<string, int> { ["a"] = 1, ["b"] = 2 };
        foreach (var e in d)
            Console.WriteLine(e);

        // Nested KeyValuePair (inner ToString runs through the same path).
        Console.WriteLine(new KeyValuePair<int, KeyValuePair<int, int>>(9, new KeyValuePair<int, int>(3, 4)));

        NonGenericAppendFormatted();
    }

    internal static void __ToStringGate()
    {
        var handler = new DefaultInterpolatedStringHandler(8, 0);
        handler.AppendLiteral("kept");
        Console.WriteLine("handler tostring 1=" + handler.ToString());
        Console.WriteLine("handler tostring 2=" + handler.ToString());
        Console.WriteLine("handler clear=" + handler.ToStringAndClear());
        Console.WriteLine("handler after clear='" + handler.ToString() + "'");
    }

    // The handler's five NON-GENERIC AppendFormatted overloads: the C# compiler picks
    // one by the hole's static type, and the ReadOnlySpan<char> and object ones are
    // exercised here. This bucket's snapshot is frozen because other sections diverge
    // from real .NET, so these lines were checked against it by compiling standalone.
    private static void NonGenericAppendFormatted()
    {
        ReadOnlySpan<char> span = "spanhole".AsSpan();
        ReadOnlySpan<char> slice = "0123456789".AsSpan(2, 4); // "2345"

        // AppendFormatted(ReadOnlySpan<char>)
        Console.WriteLine($"span=[{span}]");
        Console.WriteLine($"slice=[{slice}]");
        Console.WriteLine($"empty=[{ReadOnlySpan<char>.Empty}]");

        // AppendFormatted(ReadOnlySpan<char>, int alignment, string format). An alignment
        // narrower than the value does not truncate: padding is a minimum, not a width.
        Console.WriteLine($"[{span,12}]");
        Console.WriteLine($"[{span,-12}]");
        Console.WriteLine($"[{span,3}]");
        Console.WriteLine($"[{slice,8}]|[{slice,-8}]");

        // A span hole and a string hole of the same text pad identically — the property
        // that makes sharing one aligned-append correct.
        Console.WriteLine($"[{"spanhole",12}]" == $"[{span,12}]");

        // The hole's static type is object, so the compiler binds the non-generic object
        // overload rather than AppendFormatted<T>.
        object o = "boxedstring";
        object oi = 42;
        object onull = null;
        Console.WriteLine($"obj=[{o}]");
        Console.WriteLine($"obj-int=[{oi}]");
        Console.WriteLine($"obj-null=[{onull}]");
        Console.WriteLine($"obj-align=[{o,15}]|[{o,-15}]");
        // An object hole dispatches the runtime type's ToString override.
        object ov = new Overrider();
        Console.WriteLine($"obj-override=[{ov}]");
    }

    private sealed class Overrider
    {
        public override string ToString() => "overridden!";
    }
}
