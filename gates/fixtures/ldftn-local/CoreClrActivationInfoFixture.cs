using System.Globalization;
using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class CoreClrActivationInfoFixture
{
    public static bool Run(string[] args)
    {
        if (args.Length is not (3 or 4) || !args[0].StartsWith("--coreclr-activation-info", StringComparison.Ordinal)) return false;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        using var core = AssemblyDefinition.ReadAssembly(args[2]);
        var owner = core.MainModule.GetType("System.RuntimeTypeHandle");
        var definition = owner.Methods.Single(m => m.Name == "GetActivationInfo" && m.HasBody);
        bool Fn(TypeReference type, MetadataType result, Func<TypeReference, bool> parameter)
            => type is ByReferenceType { ElementType: FunctionPointerType f } && !f.HasThis && !f.ExplicitThis
                && f.CallingConvention == MethodCallingConvention.Default && f.ReturnType.MetadataType == result
                && f.Parameters.Count == 1 && parameter(f.Parameters[0].ParameterType);
        if (core.Name.Name != "System.Private.CoreLib" || definition.Attributes != (MethodAttributes.Assembly | MethodAttributes.Static | MethodAttributes.HideBySig)
            || definition.ImplAttributes != MethodImplAttributes.IL || definition.HasGenericParameters || definition.HasThis || definition.ExplicitThis
            || definition.CallingConvention != MethodCallingConvention.Default || definition.ReturnType.MetadataType != MetadataType.Void || definition.Parameters.Count != 6
            || definition.Parameters[0].ParameterType.FullName != "System.RuntimeType"
            || !Fn(definition.Parameters[1].ParameterType, MetadataType.Object, t => t is PointerType { ElementType.MetadataType: MetadataType.Void })
            || definition.Parameters[2].ParameterType is not ByReferenceType { ElementType: PointerType { ElementType.MetadataType: MetadataType.Void } }
            || !Fn(definition.Parameters[3].ParameterType, MetadataType.Void, t => t.MetadataType == MetadataType.Object)
            || !Fn(definition.Parameters[4].ParameterType, MetadataType.Void, t => t is ByReferenceType { ElementType.MetadataType: MetadataType.Byte })
            || definition.Parameters[5].ParameterType is not ByReferenceType { ElementType.MetadataType: MetadataType.Boolean })
            throw new InvalidOperationException("Managed activation-info signature changed");
        using var app = AssemblyDefinition.ReadAssembly(args[1], new ReaderParameters { InMemory = true });
        var module = app.MainModule;
        var checks = module.GetType("CoreClrActivationInfoSubset.ActivationChecks");
        if (args[0] == "--coreclr-activation-info-oracle")
        {
            module.EntryPoint = checks.Methods.Single(m => m.Name == "Run");
            app.Write(args[1], new WriterParameters { Timestamp = 0, DeterministicMvid = true });
            return true;
        }
        MethodReference target = module.ImportReference(definition);
        bool negative = args[0] == "--coreclr-activation-info-negative";
        if (negative)
        {
            target = new MethodReference(target.Name, target.ReturnType, target.DeclaringType);
            foreach (var p in definition.Parameters) target.Parameters.Add(new ParameterDefinition(module.ImportReference(p.ParameterType)));
            string mode = args[3].Replace("address-", "", StringComparison.Ordinal);
            switch (mode)
            {
                case "allocator-return": ((FunctionPointerType)((ByReferenceType)target.Parameters[1].ParameterType).ElementType).ReturnType = module.TypeSystem.IntPtr; break;
                case "allocator-unmanaged": ((FunctionPointerType)((ByReferenceType)target.Parameters[1].ParameterType).ElementType).CallingConvention = MethodCallingConvention.C; break;
                case "value-byte": ((FunctionPointerType)((ByReferenceType)target.Parameters[4].ParameterType).ElementType).Parameters[0].ParameterType = module.TypeSystem.Byte; break;
                case "flat-pointer": target.Parameters[1].ParameterType = new ByReferenceType(new PointerType(module.TypeSystem.Void)); break;
                case "bool-value": target.Parameters[5].ParameterType = module.TypeSystem.Boolean; break;
                case "modified": target.Parameters[5].ParameterType = new OptionalModifierType(module.ImportReference(typeof(System.Runtime.CompilerServices.IsConst)), target.Parameters[5].ParameterType); break;
                default: throw new InvalidOperationException("Unknown negative " + mode);
            }
        }
        foreach (string name in new[] { "Direct", "Pointer", "Address" })
        {
            var method = checks.Methods.Single(m => m.Name == name);
            method.Body = new MethodBody(method); var il = method.Body.GetILProcessor();
            il.Emit(OpCodes.Ldarg_0); il.Emit(OpCodes.Castclass, module.ImportReference(definition.Parameters[0].ParameterType));
            if (name == "Address")
            {
                il.Emit(OpCodes.Ldftn, target);
                var ctor = new MethodReference(".ctor", module.TypeSystem.Void, method.ReturnType) { HasThis = true };
                ctor.Parameters.Add(new ParameterDefinition(module.TypeSystem.Object)); ctor.Parameters.Add(new ParameterDefinition(module.TypeSystem.IntPtr));
                il.Emit(OpCodes.Newobj, ctor);
            }
            else
            {
                for (int index = 1; index < 6; index++) il.Emit(OpCodes.Ldarg, method.Parameters[index]);
                if (name == "Pointer")
                {
                    il.Emit(OpCodes.Ldftn, target); var site = new CallSite(module.TypeSystem.Void);
                    foreach (var parameter in target.Parameters) site.Parameters.Add(new ParameterDefinition(parameter.ParameterType));
                    il.Emit(OpCodes.Calli, site);
                }
                else il.Emit(OpCodes.Call, target);
            }
            il.Emit(OpCodes.Ret);
        }
        if (args[0] == "--coreclr-activation-info-noalloc") RuntimeTypeDefaultCtorFixture.MarkNoAlloc(module, checks.Methods.Single(m => m.Name == "Direct"));
        if (negative)
        {
            var entry = new MethodDefinition("MalformedActivation", MethodAttributes.Private | MethodAttributes.Static, module.TypeSystem.Void);
            checks.Methods.Add(entry); var il = entry.Body.GetILProcessor();
            il.Emit(OpCodes.Ldnull);
            for (int index = 0; index < 5; index++) { il.Emit(OpCodes.Ldc_I4_0); il.Emit(OpCodes.Conv_U); }
            il.Emit(OpCodes.Call, checks.Methods.Single(m => m.Name == (args[3].StartsWith("address-", StringComparison.Ordinal) ? "Pointer" : "Direct")));
            il.Emit(OpCodes.Ret); module.EntryPoint = entry;
        }
        else module.EntryPoint = checks.Methods.Single(m => m.Name == (args[0] == "--coreclr-activation-info-reflection" ? "RunReflectionArtifact" : "RunArtifact"));
        app.Write(args[1], new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        Console.WriteLine("activation-info: exact managed CoreLib bridge; native-only fixture");
        return true;
    }
}
