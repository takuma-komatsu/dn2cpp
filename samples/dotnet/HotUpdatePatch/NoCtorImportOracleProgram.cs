using System.Globalization;

namespace HotUpdateNoCtorPatch;

internal static class Program
{
    private static void Main()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        NoCtorImportProbe.Run();
    }
}
