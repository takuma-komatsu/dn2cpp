using System;
using System.Globalization;

namespace Dn2Cpp.Gates;

internal static class Program
{
    private static void Main()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine(HotUpdatePatch.InterpretedConcatSubset.Run());
    }
}
