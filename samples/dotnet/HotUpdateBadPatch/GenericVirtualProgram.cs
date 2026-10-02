using System.Globalization;

namespace HotUpdateGenericVirtualBad;

internal class GenericVirtual
{
    public virtual T Echo<T>(T value) => value;
}

internal static class Program
{
    private static void Main()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        _ = new GenericVirtual();
    }
}
