using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class EnumRuntimeContractFixture
{
    internal static void Rewrite(ModuleDefinition module, string corePath)
    {
        using var core = ModuleDefinition.ReadModule(corePath);
        var owner = module.GetType("EnumPrivateContractsSubset.Program");
        var enumType = core.GetType("System.Enum");
        var runtimeType = core.GetType("System.RuntimeType");
        foreach (var stub in owner.Methods.Where(method => method.Name is "Format" or "Default" or "FormatAddress"
            or "Name" or "NameAddress" or "Element" or "Underlying" or "RuntimeName" or "RuntimeDefined"
            or "BindName" or "BindDefined"))
        {
            bool bind = stub.Name.StartsWith("Bind", StringComparison.Ordinal);
            bool query = bind || stub.Name.StartsWith("Runtime", StringComparison.Ordinal);
            bool generic = stub.HasGenericParameters;
            string targetName = stub.Name switch
            {
                "Format" or "Default" or "FormatAddress" => "ToString",
                "Name" or "NameAddress" => "GetName",
                "Element" => "InternalGetCorElementType",
                "Underlying" => "InternalGetUnderlyingType",
                "RuntimeName" or "BindName" => "GetEnumName",
                _ => "IsEnumDefined",
            };
            var definition = (query ? runtimeType : enumType).Methods.Single(method => method.Name == targetName
                && method.IsStatic != query && method.GenericParameters.Count == stub.GenericParameters.Count
                && method.Parameters.Count == (query ? 1 : stub.Parameters.Count)
                && (query || method.Parameters[0].ParameterType.FullName == "System.RuntimeType"));
            MethodReference target = module.ImportReference(definition);
            if (generic)
            {
                var instance = new GenericInstanceMethod(target);
                foreach (var parameter in stub.GenericParameters) instance.GenericArguments.Add(parameter);
                target = instance;
            }
            stub.Body = new MethodBody(stub);
            var il = stub.Body.GetILProcessor();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Castclass, module.ImportReference(runtimeType));
            if (bind)
            {
                il.Emit(OpCodes.Dup);
                il.Emit(OpCodes.Ldvirtftn, target);
                var ctor = new MethodReference(".ctor", module.TypeSystem.Void, stub.ReturnType) { HasThis = true };
                ctor.Parameters.Add(new ParameterDefinition(module.TypeSystem.Object));
                ctor.Parameters.Add(new ParameterDefinition(module.TypeSystem.IntPtr));
                il.Emit(OpCodes.Newobj, ctor);
            }
            else
            {
                foreach (var parameter in stub.Parameters.Skip(1)) il.Emit(OpCodes.Ldarg, parameter);
                if (stub.Name.EndsWith("Address", StringComparison.Ordinal))
                {
                    il.Emit(OpCodes.Ldftn, target);
                    var site = new CallSite(module.ImportReference(definition.ReturnType));
                    foreach (var parameter in definition.Parameters)
                        site.Parameters.Add(new ParameterDefinition(module.ImportReference(parameter.ParameterType)));
                    il.Emit(OpCodes.Calli, site);
                }
                else il.Emit(query ? OpCodes.Callvirt : OpCodes.Call, target);
            }
            il.Emit(OpCodes.Ret);
            Console.WriteLine("enum-contract-fixture:" + stub.Name + ":" + target.FullName);
        }
        var bypass = new TypeDefinition("System.Runtime.CompilerServices", "IgnoresAccessChecksToAttribute",
            TypeAttributes.Public | TypeAttributes.Sealed, module.ImportReference(core.GetType("System.Attribute")));
        module.Types.Add(bypass);
        var constructor = new MethodDefinition(".ctor", MethodAttributes.Public | MethodAttributes.HideBySig
            | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName, module.TypeSystem.Void);
        constructor.Parameters.Add(new ParameterDefinition(module.TypeSystem.String));
        bypass.Methods.Add(constructor);
        var ctorIl = constructor.Body.GetILProcessor();
        ctorIl.Emit(OpCodes.Ldarg_0);
        ctorIl.Emit(OpCodes.Call, module.ImportReference(core.GetType("System.Attribute").Methods.Single(method => method.IsConstructor)));
        ctorIl.Emit(OpCodes.Ret);
        var attribute = new CustomAttribute(constructor);
        attribute.ConstructorArguments.Add(new CustomAttributeArgument(module.TypeSystem.String, "System.Private.CoreLib"));
        module.Assembly.CustomAttributes.Add(attribute);
        var entryIl = module.EntryPoint.Body.GetILProcessor();
        entryIl.InsertBefore(module.EntryPoint.Body.Instructions.Last(),
            entryIl.Create(OpCodes.Call, owner.Methods.Single(method => method.Name == "Run")));
    }
}
