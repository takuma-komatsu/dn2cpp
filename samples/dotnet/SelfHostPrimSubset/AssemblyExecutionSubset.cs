using System;
using System.Globalization;
using System.Reflection;

namespace AssemblyExecutionSubset;

internal static class Program
{

    internal static void Run()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("== Assembly execution public neighbor ==");
        Assembly linked = typeof(Program).Assembly;
        Console.WriteLine("assembly-execution-public-linked:" + (Assembly.Load(linked.GetName().Name) == linked));
        bool nullName = false;
        try { Assembly.Load((string)null); }
        catch (ArgumentNullException) { nullName = true; }
        Console.WriteLine("assembly-execution-public-null-name:" + nullName);
        Console.WriteLine("Assembly execution public neighbor end");
    }
}
