using System.Globalization;

namespace PreserveFixture;

internal static class Program
{
    private static void Main()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
    }
}

public sealed class Indexed
{
    public int this[int index] => index;
}
