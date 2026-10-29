using System.Globalization;
using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class DefaultPrincipalDependencyFixture
{
    public static bool Run(string[] args)
    {
        if (args.Length != 4 || args[0] != "--default-principal-target-copy") return false;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        if (File.Exists(args[2])) throw new InvalidOperationException("target copy output must be new");
        using var assembly = AssemblyDefinition.ReadAssembly(args[1], new ReaderParameters { InMemory = true });
        var type = assembly.MainModule.GetType("System.Security.Principal.GenericPrincipal");
        var method = type.Methods.Single(m => m.Name == "GetDefaultInstance");
        switch (args[3])
        {
            case "unchanged": break;
            case "lookalike-assembly": assembly.Name.Name = "ClaimsLookalike"; break;
            case "wrong-return": method.ReturnType = assembly.MainModule.TypeSystem.Object; break;
            case "wrong-name": method.Name = "NotGetDefaultInstance"; break;
            case "duplicate":
                var duplicate = new MethodDefinition(method.Name, method.Attributes, method.ReturnType);
                duplicate.Body.Instructions.Add(Instruction.Create(OpCodes.Ldnull)); duplicate.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
                type.Methods.Add(duplicate);
                break;
            default: throw new ArgumentException("unknown target mutation");
        }
        assembly.MainModule.Attributes |= ModuleAttributes.ILOnly;
        assembly.MainModule.Attributes &= ~ModuleAttributes.StrongNameSigned;
        assembly.Write(args[2], new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        Console.WriteLine("default-principal-target-copy:" + args[3] + ";transpile-only");
        return true;
    }
}
