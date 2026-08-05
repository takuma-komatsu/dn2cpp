#nullable enable
// (practical reflection, follow-up): Convert.ChangeType non-primitive targets /
// sources the matrix carved out — Decimal targets and sources, a boxed-enum source
// (read as its underlying, or its member name for a string target), and the
// ChangeType(object, TypeCode) overload. Decimal narrowing to integer uses banker's
// rounding (2.5m -> 2, 3.5m -> 4), matching Convert.ToInt*. Enum *target* still throws in
// real.NET (DefaultToType rejects it), so that stays unsupported. Diffed exact vs real.NET.
using System;

namespace ConvertChangeTypeExtSubset;

public enum Color : int { Red = 0, Green = 1, Blue = 2 }

public static class Program
{
    static void Show(object v) => Console.WriteLine($"{v} :: {v.GetType().Name}");

    internal static void __GateEntry()
    {
        // -> Decimal target (string / int / double / bool sources)
        Show(Convert.ChangeType("3.14", typeof(decimal)));
        Show(Convert.ChangeType(42, typeof(decimal)));
        Show(Convert.ChangeType(2.5, typeof(decimal)));
        Show(Convert.ChangeType(true, typeof(decimal)));

        // Decimal source -> numeric / string / bool (banker's rounding on narrowing)
        Show(Convert.ChangeType(3.14m, typeof(int)));
        Show(Convert.ChangeType(2.5m, typeof(int)));
        Show(Convert.ChangeType(3.5m, typeof(int)));
        Show(Convert.ChangeType(3.14m, typeof(double)));
        Show(Convert.ChangeType(9.99m, typeof(string)));
        Show(Convert.ChangeType(3.14m, typeof(bool)));
        Show(Convert.ChangeType(0m, typeof(bool)));

        // boxed enum source -> underlying integer, or member name for a string target
        Show(Convert.ChangeType(Color.Blue, typeof(int)));
        Show(Convert.ChangeType(Color.Green, typeof(long)));
        Show(Convert.ChangeType(Color.Blue, typeof(string)));

        // ChangeType(object, TypeCode) overload — target named by a TypeCode
        Show(Convert.ChangeType("123", TypeCode.Int32));
        Show(Convert.ChangeType(123, TypeCode.Double));
        Show(Convert.ChangeType("9.9", TypeCode.Decimal));
        Show(Convert.ChangeType(255, TypeCode.Byte));
        Show(Convert.ChangeType(65, TypeCode.Char));
        Show(Convert.ChangeType(1, TypeCode.Boolean));
        Show(Convert.ChangeType(3.14m, TypeCode.Int32));
        Show(Convert.ChangeType(42, TypeCode.String));
    }
}
