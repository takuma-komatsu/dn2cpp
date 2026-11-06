using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class FormalDeclaringMethodFixture
{
    public static bool Run(string[] args)
    {
        if (args.Length != 3 || args[0] is not ("--formal-declaring-public" or "--formal-declaring-private")) return false;
        using var core = AssemblyDefinition.ReadAssembly(args[2]);
        if (core.Name.Name != "System.Private.CoreLib") throw new InvalidOperationException("Expected input CoreLib");
        var runtimeType = core.MainModule.GetType("System.RuntimeType");
        var getter = runtimeType.Methods.Single(m => m.Name == "get_DeclaringMethod");
        var handle = core.MainModule.GetType("System.RuntimeTypeHandle");
        var bridge = handle.Methods.Single(m => m.Name == "GetDeclaringMethodForGenericParameter" && m.IsAssembly);
        bool CoreType(TypeReference type, string name) => type.FullName == name && type.Resolve().Module == core.MainModule;
        bool Managed(MethodDefinition method) => method.HasBody && method.IsIL && method.IsManaged
            && !method.IsInternalCall && !method.IsPInvokeImpl && !method.HasGenericParameters
            && !method.ExplicitThis && !method.IsAbstract && method.CallingConvention == MethodCallingConvention.Default;
        if (!Managed(getter) || !getter.IsPublic || !getter.IsVirtual || getter.IsFinal || getter.IsStatic
            || !getter.HasThis || !getter.IsSpecialName || getter.Parameters.Count != 0
            || !CoreType(getter.ReturnType, "System.Reflection.MethodBase")
            || !Managed(bridge) || !bridge.IsStatic || bridge.HasThis || bridge.IsVirtual
            || bridge.Parameters.Count != 1 || !CoreType(bridge.Parameters[0].ParameterType, "System.RuntimeType")
            || !CoreType(bridge.ReturnType, "System.IRuntimeMethodInfo"))
            throw new InvalidOperationException("Formal declaring method declarations changed");
        var import = handle.Methods.Single(m => m.Name == bridge.Name && m.IsPInvokeImpl);
        if (!import.IsPrivate || !import.IsStatic || import.HasBody || !import.IsPreserveSig
            || import.ReturnType.MetadataType != MetadataType.Void
            || string.Join(",", import.Parameters.Select(p => p.ParameterType.FullName)) != "System.Runtime.CompilerServices.QCallTypeHandle,System.Runtime.CompilerServices.ObjectHandleOnStack"
            || import.PInvokeInfo.Module.Name != "QCall" || import.PInvokeInfo.EntryPoint != "RuntimeTypeHandle_GetDeclaringMethodForGenericParameter"
            || !bridge.Body.Instructions.Any(i => i.OpCode == OpCodes.Call && i.Operand is MethodReference m && m.Resolve() == import))
            throw new InvalidOperationException("Formal declaring method QCall binding changed");
        string path = Path.GetFullPath(args[1]);
        using var app = AssemblyDefinition.ReadAssembly(path, new ReaderParameters { InMemory = true });
        var module = app.MainModule;
        var program = module.GetType("FormalDeclaringMethodSubset.Program");
        bool publicGetter = args[0] == "--formal-declaring-public";
        var target = module.ImportReference(publicGetter ? getter : bridge);
        foreach (string route in publicGetter ? new[] { "Direct", "Pointer", "Address", "VirtualAddress", "FlowingVirtualAddress" } : new[] { "PrivateDirect", "PrivatePointer" })
        {
            var stub = program.Methods.Single(m => m.Name == route);
            stub.Body = new MethodBody(stub);
            var il = stub.Body.GetILProcessor();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Castclass, module.ImportReference(runtimeType));
            if (route is "Address" or "VirtualAddress" or "FlowingVirtualAddress")
            {
                var constructor = new MethodReference(".ctor", module.TypeSystem.Void, stub.ReturnType) { HasThis = true };
                constructor.Parameters.Add(new ParameterDefinition(module.TypeSystem.Object));
                constructor.Parameters.Add(new ParameterDefinition(module.TypeSystem.IntPtr));
                if (route != "Address") il.Emit(OpCodes.Dup);
                il.Emit(route == "Address" ? OpCodes.Ldftn : OpCodes.Ldvirtftn, target);
                if (route == "FlowingVirtualAddress")
                {
                    var pointer = new VariableDefinition(module.TypeSystem.IntPtr);
                    stub.Body.Variables.Add(pointer);
                    il.Emit(OpCodes.Stloc, pointer);
                    il.Emit(OpCodes.Ldloc, pointer);
                }
                il.Emit(OpCodes.Newobj, constructor);
            }
            else if (route.EndsWith("Pointer", StringComparison.Ordinal))
            {
                il.Emit(OpCodes.Ldftn, target);
                var site = new CallSite(target.ReturnType);
                if (publicGetter) site.Parameters.Add(new ParameterDefinition(target.DeclaringType));
                else site.Parameters.Add(new ParameterDefinition(target.Parameters[0].ParameterType));
                il.Emit(OpCodes.Calli, site);
            }
            else il.Emit(OpCodes.Call, target);
            if (!publicGetter) il.Emit(OpCodes.Pop);
            il.Emit(OpCodes.Ret);
        }
        module.EntryPoint = program.Methods.Single(m => m.Name == (publicGetter ? "RunPublicArtifact" : "RunPrivateArtifact"));
        app.Write(path, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        Console.WriteLine("formal-declaring-sites=" + (publicGetter ? "direct,pointer,address,virtual-address,public-getter" : "direct,pointer,reflection,native-only-private-bridge") + ",exact-CoreLib-signatures,QCall-binding-unchanged");
        return true;
    }
}
