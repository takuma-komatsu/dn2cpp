using System;
using System.Globalization;
using Dn2Cpp.Runtime;

namespace HotUpdateNoCtor;

internal static class Program
{
    private static void Main(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

        if (args.Length == 2 && args[0] == "--load-dir")
        {
            Console.WriteLine("noctor directory:" + HotUpdate.LoadDirectory(args[1]));
            return;
        }

        // Keep the ordinary unshaped fallback control's lone row in the base.
        if (LegacyImportSurface.Name() == 0)
            Console.WriteLine("unreachable");
        Console.WriteLine("noctor: start");
        try
        {
            HotUpdate.Load(args[0]);
        }
        catch (NotSupportedException e)
        {
            Console.WriteLine("noctor rejected:" + e.Message);
            return;
        }
        Console.WriteLine("noctor: done");
    }
}
