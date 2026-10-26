using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class RuntimeMethodEnumerationFixture
{
    public static bool Run(string[] args)
    {
        if (args.Length != 3 || args[0] != "--runtime-method-enumeration") return false;
        using var core = AssemblyDefinition.ReadAssembly(args[2]);
        if (core.Name.Name != "System.Private.CoreLib") throw new InvalidOperationException("Expected input CoreLib");
        var owner = core.MainModule.GetType("System.RuntimeTypeHandle");
        var first = owner.Methods.Single(m => m.Name == "GetFirstIntroducedMethod");
        var next = owner.Methods.Single(m => m.Name == "GetNextIntroducedMethod");
        foreach (var method in new[] { first, next })
            if (!method.IsPrivate || !method.IsStatic || method.HasGenericParameters || !method.IsInternalCall
                || method.HasBody || method.Parameters.Count != 1)
                throw new InvalidOperationException("CoreCLR enumeration declaration changed");
        if (first.ReturnType.FullName != "System.RuntimeMethodHandleInternal"
            || first.Parameters[0].ParameterType.FullName != "System.RuntimeType"
            || next.ReturnType.MetadataType != MetadataType.Void
            || next.Parameters[0].ParameterType.FullName != "System.RuntimeMethodHandleInternal&")
            throw new InvalidOperationException("CoreCLR enumeration signature changed");
        var enumerator = owner.NestedTypes.Single(t => t.Name == "IntroducedMethodEnumerator");
        var ctor = enumerator.Methods.Single(m => m.IsConstructor);
        var move = enumerator.Methods.Single(m => m.Name == "MoveNext");
        if (!ctor.HasBody || !move.HasBody
            || !ctor.Body.Instructions.Any(i => i.Operand is MethodReference m && m.Name == first.Name)
            || !move.Body.Instructions.Any(i => i.Operand is MethodReference m && m.Name == next.Name))
            throw new InvalidOperationException("CoreCLR enumeration IL changed");
        string path = Path.GetFullPath(args[1]);
        using var app = AssemblyDefinition.ReadAssembly(path, new ReaderParameters { InMemory = true });
        var module = app.MainModule;
        var program = module.GetType("RuntimeMethodEnumerationSubset.Program");
        var getType = module.ImportReference(core.MainModule.GetType("System.Type").Methods.Single(m => m.Name == "GetTypeFromHandle"));
        foreach (string name in new[] { "FirstDirect", "FirstPointer", "NextDirect", "NextPointer", "EnumeratorConstructor", "DefaultMoveNext" })
        {
            var stub = program.Methods.Single(m => m.Name == name);
            stub.Body = new MethodBody(stub) { InitLocals = true };
            var il = stub.Body.GetILProcessor();
            if (name == "DefaultMoveNext")
            {
                var local = new VariableDefinition(module.ImportReference(enumerator));
                stub.Body.Variables.Add(local);
                il.Emit(OpCodes.Ldloca, local);
                il.Emit(OpCodes.Initobj, local.VariableType);
                il.Emit(OpCodes.Ldloca, local);
                il.Emit(OpCodes.Call, module.ImportReference(move));
            }
            else
            {
                bool isNext = name.StartsWith("Next", StringComparison.Ordinal);
                var target = module.ImportReference(isNext ? next : first);
                if (isNext)
                {
                    var local = new VariableDefinition(module.ImportReference(first.ReturnType));
                    stub.Body.Variables.Add(local);
                    il.Emit(OpCodes.Ldloca, local);
                    il.Emit(OpCodes.Initobj, local.VariableType);
                    il.Emit(OpCodes.Ldloca, local);
                }
                else
                {
                    il.Emit(OpCodes.Ldtoken, program.NestedTypes.Single(t => t.Name == "Target"));
                    il.Emit(OpCodes.Call, getType);
                    il.Emit(OpCodes.Castclass, module.ImportReference(first.Parameters[0].ParameterType));
                }
                if (name == "EnumeratorConstructor") il.Emit(OpCodes.Newobj, module.ImportReference(ctor));
                else if (name.EndsWith("Pointer", StringComparison.Ordinal))
                {
                    il.Emit(OpCodes.Ldftn, target);
                    var site = new CallSite(target.ReturnType);
                    site.Parameters.Add(new ParameterDefinition(target.Parameters[0].ParameterType));
                    il.Emit(OpCodes.Calli, site);
                }
                else il.Emit(OpCodes.Call, target);
                if (!isNext) il.Emit(OpCodes.Pop);
            }
            il.Emit(OpCodes.Ret);
        }
        module.EntryPoint = program.Methods.Single(m => m.Name == "RunArtifactChecks");
        app.Write(path, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        Console.WriteLine("method-enum-sites=2-direct,2-address,constructor,default-MoveNext,exact-CoreLib-signatures");
        return true;
    }
}
