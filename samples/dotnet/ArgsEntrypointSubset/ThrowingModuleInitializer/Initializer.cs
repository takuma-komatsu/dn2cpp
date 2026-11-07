using System;
using System.Runtime.CompilerServices;

namespace ThrowingModuleInitializer;

// A library passed to the transpiler with -r that the app never touches. .NET loads no
// such assembly, so its module initializer never runs; the startup pass runs every
// non-framework module initializer, so dn2cpp runs this one, and it must report the
// failure and carry on rather than let it escape ahead of the entry point's handler.
internal static class Initializer
{
    [ModuleInitializer]
    internal static void Init() => throw new InvalidOperationException("module initializer failed on purpose");
}
