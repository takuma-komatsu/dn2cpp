using System;
using System.Globalization;
using System.Reflection;

namespace AppContextExitSubset;

internal static class Program
{

    internal static void Run()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("== AppContext public neighbor ==");
        Console.WriteLine("appcontext-public-base-directory:" + !string.IsNullOrEmpty(AppContext.BaseDirectory));
        Console.WriteLine("AppContext public neighbor end");
    }
}
