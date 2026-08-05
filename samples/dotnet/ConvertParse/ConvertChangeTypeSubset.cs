#nullable enable
// (practical reflection): Convert.ChangeType(object, Type) — the runtime-Type-driven
// conversion a deserializer / data-binding layer leans on, where the target type is only
// known as a System.Type. Covers string<->primitive, numeric<->numeric (banker's
// rounding), x->string, same-type passthrough, and a deserialize-style loop that converts
// each cell to its column's target type. Diffed exact vs real.NET.
using System;

namespace ConvertChangeTypeSubset;

public static class Program
{
    static void Show(object v) => Console.WriteLine($"{v} :: {v.GetType().Name}");

    internal static void __GateEntry()
    {
        // string -> primitives
        Show(Convert.ChangeType("42", typeof(int)));
        Show(Convert.ChangeType("3.5", typeof(double)));
        Show(Convert.ChangeType("true", typeof(bool)));
        Show(Convert.ChangeType("100", typeof(long)));
        Show(Convert.ChangeType("7", typeof(byte)));

        // numeric -> numeric (banker's rounding on narrowing)
        Show(Convert.ChangeType(3.9, typeof(int)));
        Show(Convert.ChangeType(2.5, typeof(int)));
        Show(Convert.ChangeType(65, typeof(double)));
        Show(Convert.ChangeType(1, typeof(bool)));
        Show(Convert.ChangeType(0, typeof(bool)));

        // anything -> string
        Show(Convert.ChangeType(123, typeof(string)));
        Show(Convert.ChangeType(2.5, typeof(string)));
        Show(Convert.ChangeType(true, typeof(string)));

        // same-type passthrough
        Show(Convert.ChangeType(99, typeof(int)));

        // null passthrough
        object? n = Convert.ChangeType(null!, typeof(string));
        Console.WriteLine("null -> " + (n is null ? "null" : "?"));

        // deserialize-style: convert each cell into its column's target type
        object[] cells = { "10", "2.0", "false", "255" };
        Type[] types = { typeof(int), typeof(double), typeof(bool), typeof(byte) };
        for (int i = 0; i < cells.Length; i++)
            Show(Convert.ChangeType(cells[i], types[i]));
    }
}
