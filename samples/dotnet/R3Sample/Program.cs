using System.Globalization;

namespace R3Sample
{
    internal static class Program
    {
        private static void Main()
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

            R3CoreSubset.Program.__GateEntry();
        }
    }
}
