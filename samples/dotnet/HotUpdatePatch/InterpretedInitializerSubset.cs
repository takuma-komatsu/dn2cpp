using System;
using HotUpdateBase;

namespace HotUpdatePatch;

// Each AOT type is entered first here, so the import runs the initializer that the
// entry triggers.
public static class InterpretedInitializerSubset
{
    public static string Run()
    {
        Console.WriteLine("== interpreted initializer imports ==");
        Console.WriteLine("before imports=" + InitializerStage.Report(0));
        Console.WriteLine("static method import=" + InitializerStage.Report(ImportedStaticInitializer.Touch()));
        Console.WriteLine("constructor import=" + InitializerStage.Report(new ImportedConstructedInitializer().Value()));
        Console.WriteLine("static field import=" + InitializerStage.Report(ImportedFieldInitializer.Value));
        return "interpreted initializer imports end";
    }
}
