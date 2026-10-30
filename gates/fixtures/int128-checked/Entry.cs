using System;
using System.Globalization;

internal static class Entry
{
    public static void Main()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
#if INT128_DISCARD
        _ = Int128.CreateChecked<long>(-7);
#elif INT128_ISOLATED
        Int128CheckedSubset.Program.RunSelected();
#else
        Int128CheckedSubset.Program.__GateEntry();
#endif
    }
}
