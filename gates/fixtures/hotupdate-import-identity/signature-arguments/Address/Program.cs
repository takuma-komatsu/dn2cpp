using System;
using System.Globalization;
using System.Threading;

namespace SignatureArgumentImports;

internal static class AddressProgram
{
    private static void Main()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine(Interlocked.Increment(ref Cell<int>.Value));
    }
}
