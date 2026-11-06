using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class ResourceTypeGuardFixture
{
    public static bool Run(string[] args)
    {
        if (args.Length != 4 || args[0] != "--resource-type-guard") return false;
        using var core = AssemblyDefinition.ReadAssembly(args[2]);
        using var app = AssemblyDefinition.ReadAssembly(args[1], new ReaderParameters { InMemory = true });
        var module = app.MainModule;
        var program = module.GetType("ResourceTypeGuard.Program");
        var owner = core.MainModule.GetType("System.Resources.ResourceManager");
        var definition = owner.Methods.Single(m => m.Name == "IsDefaultType");
        if (core.Name.Name != "System.Private.CoreLib"
            || definition.Attributes != (MethodAttributes.Assembly | MethodAttributes.Static | MethodAttributes.HideBySig)
            || definition.ImplAttributes != MethodImplAttributes.IL || definition.RVA == 0
            || definition.CallingConvention != MethodCallingConvention.Default || definition.ExplicitThis
            || definition.HasThis || definition.HasGenericParameters
            || !definition.HasBody || definition.ReturnType is TypeSpecification || definition.ReturnType.MetadataType != MetadataType.Boolean
            || definition.Parameters.Count != 2 || definition.Parameters.Any(p => p.ParameterType is TypeSpecification || p.ParameterType.MetadataType != MetadataType.String))
            throw new InvalidOperationException("installed resource type guard shape changed");
        MethodReference target = module.ImportReference(definition);
        bool negative = args[3].StartsWith("negative-", StringComparison.Ordinal);
        if (negative)
        {
            var bad = new MethodReference(target.Name, target.ReturnType, target.DeclaringType);
            foreach (var parameter in target.Parameters) bad.Parameters.Add(new ParameterDefinition(parameter.ParameterType));
            switch (args[3].Split('-')[2])
            {
                case "spec": var spec = new GenericInstanceMethod(target); spec.GenericArguments.Add(module.TypeSystem.Int32); bad = spec; break;
                case "instance": bad.HasThis = true; break;
                case "generic": bad.GenericParameters.Add(new GenericParameter("T", bad)); break;
                case "return": bad.ReturnType = module.TypeSystem.Int32; break;
                case "modifier": bad.ReturnType = new OptionalModifierType(module.TypeSystem.Object, bad.ReturnType); break;
                case "byref": bad.Parameters[0].ParameterType = new ByReferenceType(bad.Parameters[0].ParameterType); break;
                case "secondbyref": bad.Parameters[1].ParameterType = new ByReferenceType(bad.Parameters[1].ParameterType); break;
                case "parammodifier": bad.Parameters[1].ParameterType = new RequiredModifierType(module.TypeSystem.Object, bad.Parameters[1].ParameterType); break;
                case "arity": bad.Parameters.Add(new ParameterDefinition(module.TypeSystem.String)); break;
                case "varargs": bad.CallingConvention = MethodCallingConvention.VarArg; break;
                case "wrongtype": bad.Parameters[0].ParameterType = module.TypeSystem.Object; break;
                default: throw new InvalidOperationException("negative shape");
            }
            target = bad;
        }
        var direct = program.Methods.Single(m => m.Name == "Direct");
        direct.Body = new MethodBody(direct);
        var il = direct.Body.GetILProcessor();
        il.Emit(OpCodes.Ldarg_0); il.Emit(OpCodes.Ldarg_1); il.Emit(OpCodes.Call, target); il.Emit(OpCodes.Ret);
        var address = program.Methods.Single(m => m.Name == "Address");
        address.Body = new MethodBody(address); il = address.Body.GetILProcessor();
        il.Emit(OpCodes.Ldnull); il.Emit(OpCodes.Ldftn, target);
        il.Emit(OpCodes.Newobj, program.NestedTypes.Single(t => t.Name == "Query").Methods.Single(m => m.IsConstructor)); il.Emit(OpCodes.Ret);
        var validateDefinition = core.MainModule.GetType("System.Resources.ResourceReader").Methods.Single(m => m.Name == "ValidateReaderType");
        if (validateDefinition.Attributes != (MethodAttributes.Private | MethodAttributes.Static | MethodAttributes.HideBySig)
            || validateDefinition.ImplAttributes != MethodImplAttributes.IL || !validateDefinition.HasBody
            || validateDefinition.Parameters.Count != 1 || validateDefinition.Parameters[0].ParameterType.MetadataType != MetadataType.String
            || validateDefinition.ReturnType.MetadataType != MetadataType.Boolean)
            throw new InvalidOperationException("installed reader validator shape changed");
        var validate = program.Methods.Single(m => m.Name == "Validate");
        validate.Body = new MethodBody(validate); il = validate.Body.GetILProcessor();
        il.Emit(OpCodes.Ldarg_0); il.Emit(OpCodes.Call, module.ImportReference(validateDefinition)); il.Emit(OpCodes.Ret);
        module.EntryPoint = program.Methods.Single(m => m.Name == (negative
            ? (args[3].StartsWith("negative-address-", StringComparison.Ordinal) ? "RunAddress" : "Main")
            : args[3] switch { "direct" => "Main", "address" => "RunAddress", "reflection" => "RunReflection", "invoke" => "RunInvoke", "all" => "RunAll", "header" => "RunHeader", "validate" => "RunValidate", "validate-oracle" => "RunValidateOracle", _ => throw new InvalidOperationException("route") }));
        app.Write(args[1], new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        return true;
    }
}
