using System;
using System.Globalization;
namespace OrdinaryReflectionTypeLeaves;
static class Program
{
    private static void Main(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("== ordinary reflection type leaves ==");
        AttributeTypePropertySubset.Program.Run();
        ReflectAttrBoxedSubset.Program.Run();
        ReflectRuntimeTypeParitySubset.Program.Run();
        ReflectAssemblyErrorSubset.Program.Run();
        Console.WriteLine("ordinary reflection type leaves end");
        if (args.Length > 0 && args[0] == "before-attribute-chain")
            return;
        AttributeTypePropertySubset.ChainedTypes.Run();
    }
}
