#if KNOWN_NULL_JOINS_ONLY
using System.Globalization;

internal static class KnownNullJoinsOnlyProgram
{
    private static void Main()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        KnownNullJoinSubset.Program.Run();
    }
}
#endif
