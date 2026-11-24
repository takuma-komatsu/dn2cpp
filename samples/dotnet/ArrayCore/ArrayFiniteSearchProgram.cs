using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace ArrayCoreFiniteSearch;

internal static class Program
{
    [InlineArray(2)]
    private struct Buffer<T>
    {
        private T _first;
    }

    private static void Main()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        ArrayFiniteSearchSubset.Program.Run();
        Array values = new KeyValuePair<int, Buffer<Vector128<byte>>?>[1];
        Console.WriteLine("boxed inline array search=" + Array.IndexOf(values, values.GetValue(0)));
    }
}
