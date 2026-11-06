using System;
using System.Globalization;

namespace HttpTransport;

internal static class Program
{
    private static int Main(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

        if (args.Length == 2)
        {
            string[] driverArgs = new[] { args[1] };
            if (args[0] == "get")
                return HttpReal.Program.Run(driverArgs);
            if (args[0] == "beyond")
                return HttpBeyondGet.Program.Run(driverArgs);
            if (args[0] == "tls")
                return HttpsTls.Program.Run(driverArgs);
        }

        Console.Error.WriteLine("usage: HttpTransport <get|beyond|tls> <base-url>");
        return 2;
    }
}
