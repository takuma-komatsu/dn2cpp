using System;
using System.Globalization;

internal static class ReflectedEventsPreservationProgram
{
    private static void Main()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("lookup-only event accessors: " + typeof(EventSubset.ExoticEventOwner).GetEvent("Exotic").GetOtherMethods(true).Length);
        Console.WriteLine("preserved event accessors");
    }
}
