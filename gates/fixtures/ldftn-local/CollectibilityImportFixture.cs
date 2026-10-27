using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class CollectibilityImportFixture
{
    public static bool Run(string[] args)
    {
        if (args.Length != 3 || args[0] is not ("--collectibility-import" or "--collectibility-import-shim" or "--collectibility-import-shim-pointer")) return false;
        using var core = AssemblyDefinition.ReadAssembly(args[2]);
        if (core.Name.Name != "System.Private.CoreLib") throw new InvalidOperationException("Expected input CoreLib");
        var method = core.MainModule.GetType("System.RuntimeTypeHandle").Methods.Single(m => m.Name == "IsCollectible");
        bool CoreType(TypeReference type, string name) => type.FullName == name && type.Resolve().Module == core.MainModule;
        if (!method.IsAssembly || !method.IsStatic || method.HasThis || method.ExplicitThis || method.HasGenericParameters
            || method.CallingConvention != MethodCallingConvention.Default || method.HasBody
            || !method.IsIL || !method.IsManaged || method.IsInternalCall || !method.IsPInvokeImpl
            || !method.IsPreserveSig || method.IsVirtual || method.IsAbstract
            || method.PInvokeInfo is not { Module.Name: "QCall", EntryPoint: "RuntimeTypeHandle_IsCollectible" }
            || !CoreType(method.ReturnType, "Interop/BOOL") || !method.ReturnType.Resolve().IsEnum
            || method.ReturnType.Resolve().Fields.Single(f => f.Name == "value__").FieldType.MetadataType != MetadataType.Int32
            || method.Parameters.Count != 1 || !CoreType(method.Parameters[0].ParameterType, "System.Runtime.CompilerServices.QCallTypeHandle")
            || !method.Parameters[0].ParameterType.Resolve().IsValueType
            || !method.Parameters[0].ParameterType.Resolve().CustomAttributes.Any(a => a.AttributeType.FullName == "System.Runtime.CompilerServices.IsByRefLikeAttribute"))
            throw new InvalidOperationException("CoreCLR collectibility import declaration changed");
        var handle = method.Parameters[0].ParameterType.Resolve();
        var raw = handle.Fields.Single(f => f.Name == "_handle");
        var pointer = handle.Fields.Single(f => f.Name == "_ptr");
        if (raw.FieldType.MetadataType != MetadataType.IntPtr || pointer.FieldType is not PointerType { ElementType.MetadataType: MetadataType.Void })
            throw new InvalidOperationException("CoreCLR QCallTypeHandle fields changed");
        string path = Path.GetFullPath(args[1]);
        using var app = AssemblyDefinition.ReadAssembly(path, new ReaderParameters { InMemory = true });
        var module = app.MainModule;
        var program = module.GetType("CollectibilityImportRoutes.Program");
        var target = module.ImportReference(method);
        foreach (string route in new[] { "Direct", "Pointer" })
        {
            var stub = program.Methods.Single(m => m.Name == route);
            stub.Body = new MethodBody(stub) { InitLocals = true };
            var local = new VariableDefinition(target.Parameters[0].ParameterType);
            stub.Body.Variables.Add(local);
            var il = stub.Body.GetILProcessor();
            il.Emit(OpCodes.Ldloca, local);
            il.Emit(OpCodes.Initobj, local.VariableType);
            il.Emit(OpCodes.Ldloca, local);
            il.Emit(OpCodes.Ldc_I4, 29);
            il.Emit(OpCodes.Conv_I);
            il.Emit(OpCodes.Stfld, module.ImportReference(raw));
            il.Emit(OpCodes.Ldloc, local);
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
        if (args[0] is "--collectibility-import-shim" or "--collectibility-import-shim-pointer")
        {
            var implementation = program.Methods.Single(m => m.Name == "Managed");
            implementation.Parameters[0].ParameterType = target.Parameters[0].ParameterType;
            implementation.ReturnType = target.ReturnType;
            var attribute = module.GetType("Dn2Cpp.Runtime.NativeImplementationAttribute");
            var binding = new CustomAttribute(attribute.Methods.Single(m => m.IsConstructor));
            binding.ConstructorArguments.Add(new CustomAttributeArgument(module.TypeSystem.String, "QCall"));
            binding.ConstructorArguments.Add(new CustomAttributeArgument(module.TypeSystem.String, "RuntimeTypeHandle_IsCollectible"));
            implementation.CustomAttributes.Add(binding);
            if (args[0] == "--collectibility-import-shim-pointer")
            {
                var direct = program.Methods.Single(m => m.Name == "Direct");
                direct.Body = new MethodBody(direct);
                var il = direct.Body.GetILProcessor();
                il.Emit(OpCodes.Ldc_I4_0);
                il.Emit(OpCodes.Ret);
            }
        }
        module.EntryPoint = program.Methods.Single(m => m.Name == "RunArtifactChecks");
        app.Write(path, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        Console.WriteLine("collectibility-import-sites=MemberRef-direct,address,calli,exact-CoreLib-signature,native-only");
        return true;
    }
}
