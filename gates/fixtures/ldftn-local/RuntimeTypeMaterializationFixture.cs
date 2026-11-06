using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class RuntimeTypeMaterializationFixture
{
    public static bool Run(string[] args)
    {
        if (args.Length != 3 || args[0] != "--runtime-type-materialization") return false;
        using var core = AssemblyDefinition.ReadAssembly(args[2]);
        if (core.Name.Name != "System.Private.CoreLib") throw new InvalidOperationException("Expected input CoreLib");
        var owner = core.MainModule.GetType("System.RuntimeTypeHandle");
        var shapes = new (string Label, string Name, string Result, string Parameters, bool Private, bool PInvoke)[]
        {
            ("MethodTable", "GetRuntimeType", "System.RuntimeType", "System.Runtime.CompilerServices.MethodTable*", false, false),
            ("Handle", "GetRuntimeTypeFromHandle", "System.RuntimeType", "System.IntPtr", false, false),
            ("Slow", "GetRuntimeTypeFromHandleSlow", "System.RuntimeType", "System.IntPtr", true, false),
            ("QCall", "GetRuntimeTypeFromHandleSlow", "System.Void", "System.IntPtr,System.Runtime.CompilerServices.ObjectHandleOnStack", true, true),
            ("MaybeNull", "GetRuntimeTypeFromHandleMaybeNull", "System.RuntimeType", "System.IntPtr", false, false),
        };
        string path = Path.GetFullPath(args[1]);
        using var app = AssemblyDefinition.ReadAssembly(path, new ReaderParameters { InMemory = true });
        var module = app.MainModule;
        var program = module.GetType("RuntimeTypeMaterializationSubset.Program");
        foreach (var shape in shapes)
        {
            var method = owner.Methods.Single(m => m.Name == shape.Name && m.IsStatic
                && string.Join(",", m.Parameters.Select(p => p.ParameterType.FullName)) == shape.Parameters);
            if (method.HasThis || method.HasGenericParameters || method.IsInternalCall
                || (shape.Private ? !method.IsPrivate : !method.IsAssembly)
                || method.ReturnType.FullName != shape.Result
                || method.IsPInvokeImpl != shape.PInvoke || method.HasBody == shape.PInvoke)
                throw new InvalidOperationException("CoreCLR type materialization declaration changed: " + shape.Label);
            foreach (string route in new[] { "Direct", "Pointer" })
            {
                var stub = program.Methods.Single(m => m.Name == shape.Label + route);
                stub.Body = new MethodBody(stub) { InitLocals = true };
                var il = stub.Body.GetILProcessor();
                var target = module.ImportReference(method);
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Conv_U);
                if (shape.PInvoke)
                {
                    var local = new VariableDefinition(target.Parameters[1].ParameterType);
                    stub.Body.Variables.Add(local);
                    il.Emit(OpCodes.Ldloca, local);
                    il.Emit(OpCodes.Initobj, local.VariableType);
                    il.Emit(OpCodes.Ldloc, local);
                }
                if (route == "Pointer")
                {
                    il.Emit(OpCodes.Ldftn, target);
                    var site = new CallSite(target.ReturnType);
                    foreach (var parameter in target.Parameters)
                        site.Parameters.Add(new ParameterDefinition(parameter.ParameterType));
                    il.Emit(OpCodes.Calli, site);
                }
                else il.Emit(OpCodes.Call, target);
                if (shape.Label == "MaybeNull")
                {
                    il.Emit(OpCodes.Ldnull);
                    il.Emit(OpCodes.Ceq);
                }
                else if (target.ReturnType.MetadataType != MetadataType.Void) il.Emit(OpCodes.Pop);
                il.Emit(OpCodes.Ret);
            }
        }
        var instance = owner.Methods.Single(m => m.Name == "GetRuntimeType" && !m.IsStatic);
        if (!instance.IsAssembly || !instance.HasBody || !instance.HasThis || instance.HasGenericParameters
            || instance.Parameters.Count != 0 || instance.ReturnType.FullName != "System.RuntimeType"
            || !instance.Body.Instructions.Any(i => i.OpCode == OpCodes.Ldfld && i.Operand is FieldReference field && field.Name == "m_type"))
            throw new InvalidOperationException("CoreCLR instance type-handle overload changed");
        var maybeNull = owner.Methods.Single(m => m.Name == "GetRuntimeTypeFromHandleMaybeNull");
        if (!maybeNull.Body.Instructions.Any(i => i.OpCode == OpCodes.Ldnull)
            || !maybeNull.Body.Instructions.Any(i => i.Operand is MethodReference method && method.Name == "GetRuntimeTypeFromHandle"))
            throw new InvalidOperationException("CoreCLR nullable type-handle wrapper changed");
        module.EntryPoint = program.Methods.Single(m => m.Name == "RunArtifactChecks");
        app.Write(path, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        Console.WriteLine("type-materialize-sites=5-direct,5-address,exact-CoreLib-signatures");
        return true;
    }
}
