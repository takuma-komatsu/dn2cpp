using System;
using HotUpdateNoCtor;

namespace HotUpdateNoCtorPatch;

internal static class NoCtorImportProbe
{
    internal static void Run()
    {
        Console.WriteLine("== noctor generic import identity ==");
        Console.WriteLine(GenericImportSurface.TypeName<int>());
        Console.WriteLine(GenericImportSurface.TypeName<string>());
        Console.WriteLine(GenericImportSurface.Echo<int>(7));
        Console.WriteLine(GenericImportSurface.Echo<string>("echo"));
        Console.WriteLine(LegacyImportSurface.Name());
        Console.WriteLine(LegacyImportSurface.Pair<int>());
        Console.WriteLine("== noctor generic import identity end ==");
    }
}
