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

public sealed class AssemblyFieldTarget
{
    [Preserve]
    private static int InitializedField = Initialize();

    private static int Initialize() => 41;
}

public sealed class CollidingAttributeTarget
{
    [PreserveCollision.Duplicate]
    private static void NonDerivedMethod() => Console.WriteLine("non-derived-drop");
}
