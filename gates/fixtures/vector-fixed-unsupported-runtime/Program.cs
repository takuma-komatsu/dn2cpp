using System.Globalization;

namespace VectorProbe;

internal static partial class Program
{
    private static void Main()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        FixedUnsupportedValueSemantics();
    }
}
