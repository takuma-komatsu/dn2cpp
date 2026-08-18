using System.Globalization;

namespace CustomAsyncTaskTypeShapeExceed;

internal static class Program
{
    private static void Main()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

        SignatureShapeSubset.Program.__GateEntry();
    }
}
