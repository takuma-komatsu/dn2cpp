using System.Globalization;

internal static class Entry
{
    static void Main()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Dn2Cpp.PrimitiveBigMulSubset.Run();
    }
}
