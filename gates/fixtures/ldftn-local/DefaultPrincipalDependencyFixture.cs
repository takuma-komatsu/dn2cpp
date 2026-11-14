using System.Globalization;
using Mono.Cecil;

// Copies System.Security.Claims with GenericPrincipal.GetDefaultInstance
// returning object, for a transpile-only factory refusal.
internal static class DefaultPrincipalDependencyFixture
{
    public static bool Run(string[] args)
    {
        if (args.Length != 4 || args[0] != "--default-principal-target-copy" || args[3] != "wrong-return") return false;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        if (File.Exists(args[2])) throw new InvalidOperationException("target copy output must be new");
        using var assembly = AssemblyDefinition.ReadAssembly(args[1], new ReaderParameters { InMemory = true });
        var method = assembly.MainModule.GetType("System.Security.Principal.GenericPrincipal").Methods
            .Single(m => m.Name == "GetDefaultInstance");
        method.ReturnType = assembly.MainModule.TypeSystem.Object;
        assembly.MainModule.Attributes |= ModuleAttributes.ILOnly;
        assembly.MainModule.Attributes &= ~ModuleAttributes.StrongNameSigned;
        assembly.Write(args[2], new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        return true;
    }
}
