using System;
using System.Collections;
using System.Globalization;
using System.Runtime.CompilerServices;

internal static class ArrayComparerTypeOnlyProgram
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool Matches(object value) => value is Comparer;

    private static void Main()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("opaque-comparer/is-null:" + Matches(null));
        Console.WriteLine("opaque-comparer/is-string:" + Matches("value"));
        Console.WriteLine("opaque comparer type checks end");
    }
}
