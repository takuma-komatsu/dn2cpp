using System.Globalization;

namespace ZLinqSample
{
    internal static class Program
    {
        private static void Main()
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

            ZLinqCoreSubset.Program.__GateEntry();
            ZLinqAdvancedSubset.Program.__GateEntry();
        }
    }
}
