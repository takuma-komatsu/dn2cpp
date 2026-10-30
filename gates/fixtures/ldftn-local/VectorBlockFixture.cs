using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class VectorBlockFixture
{
    public static bool Run(string[] args)
    {
        if (args.Length != 4 || args[0] != "--vector-block-negative") return false;
        using var core = AssemblyDefinition.ReadAssembly(args[2]);
        using var app = AssemblyDefinition.ReadAssembly(args[1], new ReaderParameters { InMemory = true });
        var module = app.MainModule;
        var run = module.GetType("VectorProbe.BlockRepetition").Methods.Single(m => m.Name == "Run");
        var instruction = run.Body.Instructions.First(i => i.Operand is GenericInstanceMethod m
            && m.Name == "Create" && m.DeclaringType.FullName == "System.Runtime.Intrinsics.Vector512");
        GenericInstanceType Vector(string name, TypeReference element)
        {
            var result = new GenericInstanceType(module.ImportReference(core.MainModule.GetType(name)));
            result.GenericArguments.Add(element); return result;
        }
        var original = (GenericInstanceMethod)instruction.Operand;
        var method = new MethodReference("Create", Vector("System.Runtime.Intrinsics.Vector512`1", module.TypeSystem.Byte), original.DeclaringType);
        method.GenericParameters.Add(new GenericParameter("T", method));
        method.Parameters.Add(new ParameterDefinition(Vector("System.Runtime.Intrinsics.Vector128`1", module.TypeSystem.Byte)));
        var instance = new GenericInstanceMethod(method); instance.GenericArguments.Add(module.TypeSystem.Byte);
        switch (args[3])
        {
            case "cross-element": method.Parameters[0].ParameterType = Vector("System.Runtime.Intrinsics.Vector128`1", module.TypeSystem.Int32); break;
            case "equal-width": method.Parameters[0].ParameterType = Vector("System.Runtime.Intrinsics.Vector512`1", module.TypeSystem.Byte); break;
            case "portable-source": method.Parameters[0].ParameterType = Vector("System.Numerics.Vector`1", module.TypeSystem.Byte); break;
            case "portable-result": method.ReturnType = Vector("System.Numerics.Vector`1", module.TypeSystem.Byte); break;
            case "method-element": instance.GenericArguments[0] = module.TypeSystem.Int32; break;
            case "instance": method.HasThis = true; break;
            case "bool":
                method.Parameters[0].ParameterType = Vector("System.Runtime.Intrinsics.Vector128`1", module.TypeSystem.Boolean);
                method.ReturnType = Vector("System.Runtime.Intrinsics.Vector512`1", module.TypeSystem.Boolean);
                instance.GenericArguments[0] = module.TypeSystem.Boolean; break;
            default: throw new ArgumentException("unknown block negative " + args[3]);
        }
        instruction.Operand = instance;
        app.Write(args[1]);
        Console.WriteLine("vector block malformed shape " + args[3]);
        return true;
    }
}
