using System.Globalization;

namespace ArrayCoreSearchLoops;

internal static class Program
{
    private static void Main(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        if (args.Length > 0 && args[0] == "before-array-search-additions")
        {
            ArraySearchLoopSubset.Program.Run(false);
            return;
        }
        ArraySearchLoopSubset.Program.Run();
#if !ARRAY_SEARCH_LOOP_BACKWARD_ONLY
        ArraySearchLoopSubset.Program.RunInterfaces();
        ArraySearchLoopSubset.Program.RunSharedReadWrites();
#endif
    }
}
