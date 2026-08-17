using System;
using Dn2Cpp.Scripting;

[assembly: Preserve]

namespace PreserveAssemblyLib;

public sealed class AssemblyOnlyTarget
{
    public AssemblyOnlyTarget()
    {
        Console.WriteLine("assembly-default-constructor");
    }

    private static void AssemblyMethodNotKept() => Console.WriteLine("assembly-method-drop");
    private static void IgnoreIfUnreferencedMethod() => Console.WriteLine("ignore-unreferenced");
}
