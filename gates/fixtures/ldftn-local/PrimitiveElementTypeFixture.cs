using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class PrimitiveElementTypeFixture
{
    public static bool Run(string[] args)
    {
        if (args.Length != 3 || args[0] is not ("--primitive-element" or "--primitive-element-reflection"
            or "--primitive-element-oracle" or "--primitive-element-reflection-oracle")) return false;
        using var core = AssemblyDefinition.ReadAssembly(args[2]);
        if (core.Name.Name != "System.Private.CoreLib") throw new InvalidOperationException("Expected input CoreLib");
        var owner = core.MainModule.GetType("System.Runtime.CompilerServices.RuntimeHelpers");
        var elementType = core.MainModule.GetType("System.Reflection.CorElementType");
        var method = owner.Methods.Single(m => m.Name == "IsPrimitiveType");
        if (method.Attributes != (MethodAttributes.Assembly | MethodAttributes.Static | MethodAttributes.HideBySig)
            || method.ImplAttributes != MethodImplAttributes.IL || !method.HasBody || method.HasThis || method.ExplicitThis
            || method.HasGenericParameters || method.CallingConvention != MethodCallingConvention.Default
            || method.ReturnType.MetadataType != MetadataType.Boolean || method.Parameters.Count != 1
            || method.Parameters[0].ParameterType != elementType || !elementType.IsEnum
            || elementType.Fields.Single(f => f.Name == "value__").FieldType.MetadataType != MetadataType.Byte)
            throw new InvalidOperationException("Primitive element predicate declaration changed");
        Code[] codes = { Code.Ldc_I4_1, Code.Ldarg_0, Code.Ldc_I4_S, Code.And, Code.Shl,
            Code.Ldc_I4, Code.And, Code.Ldc_I4_0, Code.Cgt_Un, Code.Ret };
        var source = method.Body.Instructions;
        if (source.Count != codes.Length || source.Where((instruction, i) => instruction.OpCode.Code != codes[i]).Any()
            || (sbyte)source[2].Operand != 31 || (int)source[5].Operand != 0x03003FFC
            || method.Body.HasVariables || method.Body.HasExceptionHandlers)
            throw new InvalidOperationException("Primitive element token-free IL changed");
        string path = Path.GetFullPath(args[1]);
        using var app = AssemblyDefinition.ReadAssembly(path, new ReaderParameters { InMemory = true });
        var module = app.MainModule;
        var program = module.GetType("PrimitiveElementTypeSubset.Program");
        bool oracle = args[0].EndsWith("-oracle", StringComparison.Ordinal);
        bool reflectionOnly = args[0].Contains("-reflection", StringComparison.Ordinal);
        var copy = program.Methods.Single(m => m.Name == "CopiedPredicate");
        copy.Body = new MethodBody(copy);
        foreach (var instruction in source)
            copy.Body.Instructions.Add(instruction.Operand switch
            {
                null => Instruction.Create(instruction.OpCode),
                sbyte value => Instruction.Create(instruction.OpCode, value),
                int value => Instruction.Create(instruction.OpCode, value),
                _ => throw new InvalidOperationException("Predicate copy is not token-free"),
            });
        var target = oracle ? copy : module.ImportReference(method);
        foreach (string route in reflectionOnly ? new[] { "Reflection" } : new[] { "Direct", "Pointer", "DirectStack", "Reflection" })
        {
            if (route == "Reflection" && !oracle) continue;
            var stub = program.Methods.Single(m => m.Name == route);
            stub.Body = new MethodBody(stub);
            var il = stub.Body.GetILProcessor();
            il.Emit(OpCodes.Ldarg_0);
            if (route == "Pointer")
            {
                il.Emit(OpCodes.Ldftn, target);
                var site = new CallSite(target.ReturnType);
                site.Parameters.Add(new ParameterDefinition(target.Parameters[0].ParameterType));
                il.Emit(OpCodes.Calli, site);
            }
            else il.Emit(OpCodes.Call, target);
            il.Emit(OpCodes.Ret);
        }
        module.EntryPoint = program.Methods.Single(m => m.Name == (reflectionOnly ? "RunReflectionArtifact" : "RunArtifact"));
        app.Write(path, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        Console.WriteLine("primitive-element-sites=" + (reflectionOnly ? "reflection-only" : "direct,calli,stack,reflection")
            + (oracle ? ",token-free-IL-copy,CLR-oracle" : ",exact-CoreLib-signature,native-only"));
        return true;
    }
}
