using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class AmbiguousMatchFactoryFixture
{
    public static bool Run(string[] args)
    {
        if (args.Length != 3 || args[0] is not ("--ambiguous-match-factory" or "--ambiguous-match-factory-reflection")) return false;
        using var core = AssemblyDefinition.ReadAssembly(args[2]);
        if (core.Name.Name != "System.Private.CoreLib") throw new InvalidOperationException("Expected input CoreLib");
        var owner = core.MainModule.GetType("System.ThrowHelper");
        string path = Path.GetFullPath(args[1]);
        using var app = AssemblyDefinition.ReadAssembly(path, new ReaderParameters { InMemory = true });
        var module = app.MainModule;
        var program = module.GetType("AmbiguousMatchFactorySubset.Program");
        bool reflectionOnly = args[0] == "--ambiguous-match-factory-reflection";
        foreach (string kind in new[] { "Member", "Attribute" })
        {
            string parameter = kind == "Member" ? "System.Reflection.MemberInfo" : "System.Attribute";
            var method = owner.Methods.Single(m => m.Name == "GetAmbiguousMatchException"
                && m.Parameters.Count == 1 && m.Parameters[0].ParameterType.FullName == parameter);
            bool CoreType(TypeReference type, string fullName) => type.FullName == fullName && type.Resolve().Module == core.MainModule;
            if (!method.IsAssembly || !method.IsStatic || method.HasThis || method.ExplicitThis || method.HasGenericParameters
                || method.IsVirtual || method.IsAbstract || method.CallingConvention != MethodCallingConvention.Default
                || !method.HasBody || !method.IsIL || !method.IsManaged || method.IsInternalCall || method.IsPInvokeImpl
                || !CoreType(method.ReturnType, "System.Reflection.AmbiguousMatchException")
                || !CoreType(method.Parameters[0].ParameterType, parameter)
                || !method.Body.Instructions.Any(i => i.OpCode == OpCodes.Call && i.Operand is MethodReference target
                    && target.DeclaringType.FullName == "System.SR" && target.Name == "Format"
                    && target.Parameters.Count == (kind == "Member" ? 3 : 2))
                || !method.Body.Instructions.Any(i => i.OpCode == OpCodes.Call && i.Operand is MethodReference target
                    && target.DeclaringType.FullName == "System.SR"
                    && target.Name == "get_Arg_AmbiguousMatchException_" + (kind == "Member" ? "MemberInfo" : "Attribute"))
                || kind == "Member" && !method.Body.Instructions.Any(i => i.OpCode == OpCodes.Callvirt
                    && i.Operand is MethodReference target && target.DeclaringType.FullName == "System.Reflection.MemberInfo"
                    && target.Name == "get_DeclaringType")
                || !method.Body.Instructions.Any(i => i.OpCode == OpCodes.Newobj && i.Operand is MethodReference target
                    && target.DeclaringType.FullName == "System.Reflection.AmbiguousMatchException")
                || method.Body.Instructions.Any(i => i.OpCode == OpCodes.Throw || i.OpCode == OpCodes.Rethrow))
                throw new InvalidOperationException("Ambiguous-match factory declaration changed: " + kind);
            if (reflectionOnly) continue;
            foreach (string route in new[] { "Direct", "Pointer" })
            {
                var stub = program.Methods.Single(m => m.Name == kind + route);
                stub.Body = new MethodBody(stub);
                var il = stub.Body.GetILProcessor();
                var target = module.ImportReference(method);
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
        }
        module.EntryPoint = program.Methods.Single(m => m.Name == (reflectionOnly ? "RunReflectionChecks" : "RunArtifactChecks"));
        app.Write(path, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        Console.WriteLine("ambiguous-factory-sites=" + (reflectionOnly ? "reflection-only" : "direct,address,reflection")
            + ",exact-CoreLib-signatures,native-only");
        return true;
    }
}
