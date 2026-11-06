using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class RuntimeMethodSlotLookupFixture
{
    public static bool Run(string[] args)
    {
        if (args.Length != 3 || args[0] is not ("--runtime-method-slot-lookup" or "--runtime-method-slot-lookup-reflection")) return false;
        using var core = AssemblyDefinition.ReadAssembly(args[2]);
        if (core.Name.Name != "System.Private.CoreLib") throw new InvalidOperationException("Expected input CoreLib");
        var owner = core.MainModule.GetType("System.RuntimeTypeHandle");
        string path = Path.GetFullPath(args[1]);
        using var app = AssemblyDefinition.ReadAssembly(path, new ReaderParameters { InMemory = true });
        var module = app.MainModule;
        var program = module.GetType("RuntimeMethodSlotLookupSubset.Program");
        bool reflectionOnly = args[0] == "--runtime-method-slot-lookup-reflection";
        foreach (string name in new[] { "GetMethodAt", "GetInterfaceMethodImplementation" })
        {
            bool at = name == "GetMethodAt";
            var method = owner.Methods.Single(m => m.Name == name && m.IsAssembly);
            bool CoreType(TypeReference type, string fullName) => type.FullName == fullName && type.Resolve().Module == core.MainModule;
            if (method.IsStatic != at || method.HasThis == at || method.ExplicitThis || method.HasGenericParameters
                || method.IsVirtual || method.IsAbstract || method.CallingConvention != MethodCallingConvention.Default
                || !method.HasBody || !method.IsIL || !method.IsManaged || method.IsInternalCall || method.IsPInvokeImpl
                || !CoreType(method.ReturnType, "System.RuntimeMethodHandleInternal") || method.Parameters.Count != 2
                || !CoreType(method.Parameters[0].ParameterType, at ? "System.RuntimeType" : "System.RuntimeTypeHandle")
                || (at ? method.Parameters[1].ParameterType.MetadataType != MetadataType.Int32
                    : !CoreType(method.Parameters[1].ParameterType, "System.RuntimeMethodHandleInternal")))
                throw new InvalidOperationException("Runtime method-slot declaration changed: " + name);
            var import = owner.Methods.Single(m => m.Name == name && m.IsPInvokeImpl);
            string nativeParameters = at ? "System.Runtime.CompilerServices.MethodTable*,System.Int32"
                : "System.Runtime.CompilerServices.QCallTypeHandle,System.Runtime.CompilerServices.QCallTypeHandle,System.RuntimeMethodHandleInternal";
            if (!import.IsPrivate || !import.IsStatic || import.HasBody || !import.IsPreserveSig
                || string.Join(",", import.Parameters.Select(p => p.ParameterType.FullName)) != nativeParameters
                || import.ReturnType.FullName != (at ? "System.IntPtr" : "System.RuntimeMethodHandleInternal")
                || import.PInvokeInfo.Module.Name != "QCall" || import.PInvokeInfo.EntryPoint != "RuntimeTypeHandle_" + name
                || !method.Body.Instructions.Any(i => i.OpCode == OpCodes.Call && i.Operand is MethodReference m && m.Resolve() == import))
                throw new InvalidOperationException("Runtime method-slot QCall binding changed: " + name);
            if (reflectionOnly) continue;
            foreach (string route in at ? new[] { "Direct", "Pointer", "UnsignedDirect", "UnsignedPointer" }
                : new[] { "Direct", "Pointer" })
            {
                var stub = program.Methods.Single(m => m.Name == (at ? "At" : "Implementation") + route);
                stub.Body = new MethodBody(stub) { InitLocals = true };
                var il = stub.Body.GetILProcessor();
                var target = module.ImportReference(method);
                if (at)
                {
                    VariableDefinition? unsignedSlot = null;
                    if (route.StartsWith("Unsigned", StringComparison.Ordinal))
                    {
                        // CLI I4 arguments may arrive from unsigned storage without conv.i4.
                        unsignedSlot = new VariableDefinition(module.TypeSystem.UInt32);
                        stub.Body.Variables.Add(unsignedSlot);
                        il.Emit(OpCodes.Ldarg_1);
                        il.Emit(OpCodes.Stloc, unsignedSlot);
                    }
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Castclass, target.Parameters[0].ParameterType);
                    if (unsignedSlot is null) il.Emit(OpCodes.Ldarg_1);
                    else il.Emit(OpCodes.Ldloc, unsignedSlot);
                }
                else
                {
                    il.Emit(OpCodes.Ldarga_S, stub.Parameters[0]);
                    il.Emit(OpCodes.Ldarg_1);
                    var handle = new VariableDefinition(target.Parameters[1].ParameterType);
                    stub.Body.Variables.Add(handle);
                    il.Emit(OpCodes.Ldloca, handle);
                    il.Emit(OpCodes.Initobj, handle.VariableType);
                    il.Emit(OpCodes.Ldloc, handle);
                }
                if (route.EndsWith("Pointer", StringComparison.Ordinal))
                {
                    il.Emit(OpCodes.Ldftn, target);
                    var site = new CallSite(target.ReturnType);
                    if (!at) site.Parameters.Add(new ParameterDefinition(new ByReferenceType(target.DeclaringType)));
                    foreach (var parameter in target.Parameters) site.Parameters.Add(new ParameterDefinition(parameter.ParameterType));
                    il.Emit(OpCodes.Calli, site);
                }
                else il.Emit(OpCodes.Call, target);
                il.Emit(OpCodes.Pop);
                il.Emit(OpCodes.Ret);
            }
        }
        module.EntryPoint = program.Methods.Single(m => m.Name == (reflectionOnly ? "RunReflectionChecks" : "RunArtifactChecks"));
        app.Write(path, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        Console.WriteLine("method-slot-sites=" + (reflectionOnly ? "reflection-only" : "direct,address,reflection") + ",exact-CoreLib-signatures,native-only");
        return true;
    }
}
