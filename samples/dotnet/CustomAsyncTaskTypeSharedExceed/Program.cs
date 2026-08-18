using System.Globalization;

namespace CustomAsyncTaskTypeSharedExceed;

internal static class Program
{
    private static void Main()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

        SharedRoleSubset.Program.__GateEntry();
    }
}
