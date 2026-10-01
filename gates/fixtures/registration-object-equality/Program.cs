using System;
using System.Globalization;
using System.Threading;

internal static class Program
{
    private static void Main()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        var registration = default(CancellationTokenRegistration);
        Console.WriteLine("null: " + registration.Equals((object?)null));
        Console.WriteLine("wrong: " + registration.Equals("wrong"));
        using var source = new CancellationTokenSource();
        using var registered = source.Token.Register(() => { });
        Console.WriteLine("registered null: " + registered.Equals((object?)null));
        Console.WriteLine("registered wrong: " + registered.Equals("wrong"));
        Console.WriteLine("registration object equality end");
    }
}
