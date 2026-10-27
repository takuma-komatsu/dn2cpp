using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class RuntimeFieldHandleFixture
{
    public static bool Run(string[] args)
    {
        if (args.Length != 3 || args[0] != "--runtime-field-handle") return false;
        using var core = AssemblyDefinition.ReadAssembly(args[2]);
        if (core.Name.Name != "System.Private.CoreLib") throw new InvalidOperationException("Expected input CoreLib");
        var owner = core.MainModule.GetType("System.RuntimeFieldHandle");
        var shapes = new (string Name, string Label, string Return, string Parameters, bool Private, string? Import)[]
        {
            ("GetUtf8NameInternal", "GetUtf8NameInternal", "System.Void*", "System.RuntimeFieldHandleInternal", true, null),
            ("GetAttributes", "GetAttributes", "System.Reflection.FieldAttributes", "System.RuntimeFieldHandleInternal", false, null),
            ("GetApproxDeclaringMethodTable", "GetApproxDeclaringMethodTable", "System.Runtime.CompilerServices.MethodTable*", "System.RuntimeFieldHandleInternal", true, null),
            ("IsFastPathSupported", "IsFastPathSupported", "System.Boolean", "System.Reflection.RtFieldInfo", false, null),
            ("GetInstanceFieldOffset", "GetInstanceFieldOffset", "System.Int32", "System.Reflection.RtFieldInfo", false, null),
            ("GetStaticFieldAddress", "GetStaticFieldAddress", "System.IntPtr", "System.Reflection.RtFieldInfo", false, null),
            ("GetFieldDataReference", "GetFieldDataReference", "System.Void", "System.IntPtr,System.Runtime.CompilerServices.ObjectHandleOnStack,System.Runtime.CompilerServices.ByteRefOnStack", true, "RuntimeFieldHandle_GetFieldDataReference"),
            ("GetToken", "GetToken", "System.Int32", "System.IntPtr", false, null),
            ("GetValueDirect", "GetValueDirect", "System.Void", "System.IntPtr,System.Void*,System.Runtime.CompilerServices.QCallTypeHandle,System.Runtime.CompilerServices.QCallTypeHandle,System.Runtime.CompilerServices.ObjectHandleOnStack", true, "RuntimeFieldHandle_GetValueDirect"),
            ("SetValueDirect", "SetValueDirect", "System.Void", "System.IntPtr,System.Void*,System.Runtime.CompilerServices.ObjectHandleOnStack,System.Runtime.CompilerServices.QCallTypeHandle,System.Runtime.CompilerServices.QCallTypeHandle", true, "RuntimeFieldHandle_SetValueDirect"),
            ("GetStaticFieldForGenericType", "GetStaticFieldForGenericType", "System.RuntimeFieldHandleInternal", "System.RuntimeFieldHandleInternal,System.Runtime.CompilerServices.MethodTable*", true, null),
            ("AcquiresContextFromThis", "AcquiresContextFromThis", "System.Boolean", "System.RuntimeFieldHandleInternal", false, null),
            ("GetLoaderAllocatorInternal", "GetLoaderAllocatorInternal", "System.Reflection.LoaderAllocator", "System.RuntimeFieldHandleInternal", true, null),
            ("GetEnCFieldAddr", "GetEnCFieldAddr", "System.Void*", "System.Runtime.CompilerServices.ObjectHandleOnStack,System.Void*", true, "RuntimeFieldHandle_GetEnCFieldAddr"),
            ("<GetRVAFieldInfo>g____PInvoke|24_0", "GetRVAFieldInfo", "System.Int32", "System.RuntimeFieldHandleInternal,System.Void**,System.UInt32*", false, "RuntimeFieldHandle_GetRVAFieldInfo"),
            ("<GetValue>g____PInvoke|30_0", "GetValue", "System.Void", "System.IntPtr,System.Runtime.CompilerServices.ObjectHandleOnStack,System.Runtime.CompilerServices.QCallTypeHandle,System.Runtime.CompilerServices.QCallTypeHandle,System.Int32*,System.Runtime.CompilerServices.ObjectHandleOnStack", false, "RuntimeFieldHandle_GetValue"),
            ("<SetValue>g____PInvoke|34_0", "SetValue", "System.Void", "System.IntPtr,System.Runtime.CompilerServices.ObjectHandleOnStack,System.Runtime.CompilerServices.ObjectHandleOnStack,System.Runtime.CompilerServices.QCallTypeHandle,System.Runtime.CompilerServices.QCallTypeHandle,System.Int32*", false, "RuntimeFieldHandle_SetValue"),
        };
        string path = Path.GetFullPath(args[1]);
        using var app = AssemblyDefinition.ReadAssembly(path, new ReaderParameters { InMemory = true });
        var module = app.MainModule;
        var program = module.GetType("RuntimeFieldHandleSubset.Program");
        foreach (var shape in shapes)
        {
            var method = owner.Methods.Single(m => m.Name == shape.Name && !m.HasBody);
            bool importMatches = shape.Import is null
                ? method.IsInternalCall && !method.IsPInvokeImpl
                : method.IsPInvokeImpl && !method.IsInternalCall && method.IsPreserveSig
                    && method.PInvokeInfo.Module.Name == "QCall" && method.PInvokeInfo.EntryPoint == shape.Import;
            if (!method.IsStatic || method.HasThis || method.HasGenericParameters || method.HasBody || !importMatches
                || (shape.Private ? !method.IsPrivate : !method.IsAssembly)
                || method.ReturnType.FullName != shape.Return
                || string.Join(",", method.Parameters.Select(p => p.ParameterType.FullName)) != shape.Parameters)
                throw new InvalidOperationException("CoreCLR FieldDesc declaration changed: " + shape.Name);
            foreach (string route in new[] { "Direct", "Pointer" })
            {
                var stub = program.Methods.Single(m => m.Name == shape.Label + route);
                stub.Body = new MethodBody(stub) { InitLocals = true };
                var il = stub.Body.GetILProcessor();
                var target = module.ImportReference(method);
                foreach (var parameter in target.Parameters)
                {
                    string? sentinel = parameter.ParameterType.FullName switch
                    {
                        "System.Void**" => "RvaAddress",
                        "System.UInt32*" => "RvaLength",
                        "System.Int32*" => "Initialized",
                        _ => null,
                    };
                    if (sentinel is not null)
                    {
                        il.Emit(OpCodes.Ldsflda, program.Fields.Single(f => f.Name == sentinel));
                        il.Emit(OpCodes.Conv_U);
                    }
                    else if (parameter.ParameterType.IsPointer)
                    {
                        il.Emit(OpCodes.Ldc_I4_0);
                        il.Emit(OpCodes.Conv_U);
                    }
                    else if (parameter.ParameterType.IsValueType)
                    {
                        var local = new VariableDefinition(parameter.ParameterType);
                        stub.Body.Variables.Add(local);
                        il.Emit(OpCodes.Ldloca, local);
                        il.Emit(OpCodes.Initobj, local.VariableType);
                        il.Emit(OpCodes.Ldloc, local);
                    }
                    else il.Emit(OpCodes.Ldnull);
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
                if (target.ReturnType.MetadataType != MetadataType.Void) il.Emit(OpCodes.Pop);
                il.Emit(OpCodes.Ret);
            }
        }
        module.EntryPoint = program.Methods.Single(m => m.Name == "RunArtifactChecks");
        app.Write(path, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        Console.WriteLine("field-handle-sites=17-direct,17-address,exact-CoreLib-signatures");
        return true;
    }
}
