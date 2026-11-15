using System;
using System.Runtime.CompilerServices;

namespace ThrowingModuleInitializer;

// The argument cases and fatal-startup case share one compiled image.
public static class Initializer
{
    [ModuleInitializer]
    internal static void Init()
    {
        if (Environment.GetEnvironmentVariable("DN2CPP_THROW_MODULE_INITIALIZER") != "1")
            return;
        Console.WriteLine("module initializer before throw");
        throw new InvalidOperationException("module initializer failed on purpose");
    }

    public static void Touch() { }
}
