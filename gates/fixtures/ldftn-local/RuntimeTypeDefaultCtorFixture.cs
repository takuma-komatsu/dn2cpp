using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class RuntimeTypeDefaultCtorFixture
{
    public static bool Run(string[] args)
    {
        if (args.Length is not (3 or 4) || !args[0].StartsWith("--runtime-type-default-ctor", StringComparison.Ordinal)) return false;
        System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
        System.Globalization.CultureInfo.CurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;
        using var core = AssemblyDefinition.ReadAssembly(args[2]);
        var owner = core.MainModule.GetType("System.RuntimeType");
        var definition = owner.Methods.Single(m => m.Name == "CreateInstanceDefaultCtor");
        if (core.Name.Name != "System.Private.CoreLib" || definition.Attributes != (MethodAttributes.Assembly | MethodAttributes.HideBySig)
            || !definition.HasThis || definition.ExplicitThis || definition.HasGenericParameters || !definition.HasBody
            || definition.CallingConvention != MethodCallingConvention.Default || definition.ReturnType.MetadataType != MetadataType.Object
            || definition.Parameters.Count != 2 || definition.Parameters.Any(p => p.ParameterType.MetadataType != MetadataType.Boolean)
            || definition.ImplAttributes != MethodImplAttributes.IL
            || definition.Body.Instructions[0].OpCode != OpCodes.Ldarg_0
            || definition.Body.Instructions[1].Operand is not MethodReference first || first.Name != "GetOrCreateCacheEntry")
            throw new InvalidOperationException("RuntimeType default constructor declaration or receiver-first IL changed");
        using var app = AssemblyDefinition.ReadAssembly(args[1], new ReaderParameters { InMemory = true });
        var module = app.MainModule;
        if (args[0] == "--runtime-type-default-ctor-inspect-planner")
        {
            var dietChecks = module.GetType("RuntimeTypeDefaultCtorSubset.DefaultCtorChecks");
            static bool Executable(MethodDefinition? method)
            {
                if (method is null || !method.HasBody) return false;
                var il = method.Body.Instructions;
                bool stub = il.Count == 4 && il[0].OpCode == OpCodes.Ldc_I4
                    && il[0].Operand is int marker && marker == 0x494C4454
                    && il[1].OpCode == OpCodes.Pop && il[2].OpCode == OpCodes.Ldnull && il[3].OpCode == OpCodes.Throw;
                return !stub;
            }
            foreach (string name in new[] { "PlannerPrivateOnly", "PlannerUnknownOnly", "GenericOnly" })
            {
                var type = dietChecks.NestedTypes.Single(t => t.Name == name);
                if (!Executable(type.Methods.SingleOrDefault(m => m.IsConstructor && !m.IsStatic && m.Parameters.Count == 0)))
                    throw new InvalidOperationException("Required private default constructor lost: " + name);
            }
            var publicOnlyType = dietChecks.NestedTypes.Single(t => t.Name == "PlannerPublicOnly");
            if (publicOnlyType.Methods.Any(m => m.IsConstructor && !m.IsStatic && Executable(m)))
                throw new InvalidOperationException("Public-only receiver retained a private constructor body");
            var genericType = dietChecks.NestedTypes.Single(t => t.Name == "GenericOnly");
            if (genericType.Methods.Any(m => m.IsConstructor && m.Parameters.Count != 0 && Executable(m)))
                throw new InvalidOperationException("Generic default construction retained an unrelated constructor body");
            Console.WriteLine("runtime-type-default-ctor-planner-diet: executable default ctors only; omitted private/argument ctors absent or known ILDiet stub");
            return true;
        }
        var checks = module.GetType("RuntimeTypeDefaultCtorSubset.DefaultCtorChecks");
        bool oracle = args[0].Contains("oracle", StringComparison.Ordinal);
        bool reflection = args[0].EndsWith("reflection", StringComparison.Ordinal);
        bool plannerOnly = args[0].EndsWith("planner", StringComparison.Ordinal);
        bool publicOnly = args[0] == "--runtime-type-default-ctor-public";
        bool negative = args[0] == "--runtime-type-default-ctor-negative";
        MethodReference target = module.ImportReference(definition);
        if (negative)
        {
            target = new MethodReference(target.Name, target.ReturnType, target.DeclaringType) { HasThis = true };
            foreach (var parameter in definition.Parameters) target.Parameters.Add(new ParameterDefinition(module.ImportReference(parameter.ParameterType)));
            switch (args[3].Replace("address-", "", StringComparison.Ordinal))
            {
                case "return-int": target.ReturnType = module.TypeSystem.Int32; break;
                case "return-modifier": target.ReturnType = new OptionalModifierType(module.TypeSystem.Object, module.TypeSystem.Object); break;
                case "static": target.HasThis = false; break;
                case "explicit": target.ExplicitThis = true; break;
                case "vararg": target.CallingConvention = MethodCallingConvention.VarArg; break;
                case "named-bool": target.Parameters[0].ParameterType = new TypeReference("System", "Boolean", module, module.TypeSystem.CoreLibrary, true); break;
                case "bool-modifier": target.Parameters[1].ParameterType = new OptionalModifierType(module.TypeSystem.Object, module.TypeSystem.Boolean); break;
                default: throw new InvalidOperationException("Unknown malformed RuntimeType default constructor signature");
            }
        }
        if (!oracle && !publicOnly)
        {
            string[] routes = negative ? new[] { args[3].StartsWith("address-", StringComparison.Ordinal) ? "Pointer" : "Direct" }
                : reflection ? new[] { "ReflectionLeaf" }
                : plannerOnly ? new[] { "PlannerPrivate", "PlannerPublic", "PlannerUnknown", "PlannerValue", "GenericPlanner" }
                : new[] { "Direct", "Virtual", "Pointer", "Address", "ReflectionLeaf", "PlannerPrivate", "PlannerPublic", "PlannerUnknown", "PlannerValue", "GenericPlanner" };
            foreach (string route in routes)
            {
                var method = checks.Methods.Single(m => m.Name == route);
                method.Body = new MethodBody(method);
                var il = method.Body.GetILProcessor();
                if (route.StartsWith("Planner", StringComparison.Ordinal) || route == "GenericPlanner")
                {
                    string typeName = route switch { "PlannerPrivate" => "PlannerPrivateOnly", "PlannerPublic" => "PlannerPublicOnly",
                        "PlannerUnknown" => "PlannerUnknownOnly", _ => "PlannerValueOnly" };
                    il.Emit(OpCodes.Ldtoken, route == "GenericPlanner" ? (TypeReference)method.GenericParameters[0]
                        : checks.NestedTypes.Single(t => t.Name == typeName));
                    var type = module.ImportReference(typeof(Type));
                    var getType = new MethodReference("GetTypeFromHandle", type, type);
                    getType.Parameters.Add(new ParameterDefinition(module.ImportReference(typeof(RuntimeTypeHandle))));
                    il.Emit(OpCodes.Call, getType);
                    il.Emit(OpCodes.Castclass, module.ImportReference(owner));
                    il.Emit(route == "PlannerUnknown" ? OpCodes.Ldarg_0 : route is "PlannerPrivate" or "GenericPlanner" ? OpCodes.Ldc_I4_0 : OpCodes.Ldc_I4_1);
                    il.Emit(OpCodes.Ldc_I4_1);
                    il.Emit(OpCodes.Call, target);
                }
                else
                {
                    il.Emit(OpCodes.Ldarg_0); il.Emit(OpCodes.Castclass, module.ImportReference(owner));
                    if (route == "Address")
                    {
                        il.Emit(OpCodes.Ldftn, target);
                        var constructor = new MethodReference(".ctor", module.TypeSystem.Void, method.ReturnType) { HasThis = true };
                        constructor.Parameters.Add(new ParameterDefinition(module.TypeSystem.Object));
                        constructor.Parameters.Add(new ParameterDefinition(module.TypeSystem.IntPtr));
                        il.Emit(OpCodes.Newobj, constructor);
                    }
                    else
                    {
                        il.Emit(OpCodes.Ldarg_1); il.Emit(OpCodes.Ldarg_2);
                        if (route == "Pointer")
                        {
                            il.Emit(OpCodes.Ldftn, target);
                            var site = new CallSite(module.TypeSystem.Object);
                            site.Parameters.Add(new ParameterDefinition(module.ImportReference(owner)));
                            foreach (var parameter in definition.Parameters) site.Parameters.Add(new ParameterDefinition(module.ImportReference(parameter.ParameterType)));
                            il.Emit(OpCodes.Calli, site);
                        }
                        else il.Emit(route == "Virtual" ? OpCodes.Callvirt : OpCodes.Call, target);
                    }
                }
                il.Emit(OpCodes.Ret);
            }
            if (!negative && !reflection)
            {
                foreach (var type in checks.NestedTypes.Where(t => t.Name.StartsWith("Planner", StringComparison.Ordinal)))
                {
                    int tokens = 0;
                    foreach (var method in module.Types.SelectMany(AllTypes).SelectMany(t => t.Methods))
                    {
                        if (!method.HasBody) continue;
                        foreach (var instruction in method.Body.Instructions)
                        {
                            if (instruction.Operand is MethodReference member && member.DeclaringType.FullName == type.FullName)
                                throw new InvalidOperationException("Planner constructor has an independent method root");
                            if (instruction.OpCode == OpCodes.Ldtoken && instruction.Operand is TypeReference operand && operand.FullName == type.FullName)
                            {
                                if (!method.Name.StartsWith("Planner", StringComparison.Ordinal)
                                    || !method.Body.Instructions.Any(i => i.OpCode == OpCodes.Call && ReferenceEquals(i.Operand, target)))
                                    throw new InvalidOperationException("Planner Type token outside exact factory call");
                                tokens++;
                            }
                        }
                    }
                    if (tokens != 1 || type.HasCustomAttributes) throw new InvalidOperationException("Planner Type requires a sole literal receiver root");
                }
                var generic = checks.Methods.Single(m => m.Name == "GenericPlanner");
                var genericType = checks.NestedTypes.Single(t => t.Name == "GenericOnly");
                if (generic.GenericParameters.Count != 1 || genericType.HasCustomAttributes
                    || generic.Body.Instructions.Count(i => i.OpCode == OpCodes.Ldtoken && i.Operand is GenericParameter) != 1
                    || !generic.Body.Instructions.Any(i => i.OpCode == OpCodes.Call && ReferenceEquals(i.Operand, target)))
                    throw new InvalidOperationException("Generic planner must use a sole symbolic receiver and exact factory call");
                foreach (var method in module.Types.SelectMany(AllTypes).SelectMany(t => t.Methods))
                    if (method.HasBody && method.Body.Instructions.Any(i => i.Operand is MethodReference member
                        && member.DeclaringType.FullName == genericType.FullName))
                        throw new InvalidOperationException("Generic planner constructor has an independent root");
                Console.WriteLine("runtime-type-default-ctor-planner: literal and symbolic receiver roots, no constructor references or type annotations");
            }
        }
        if (args[0] == "--runtime-type-default-ctor-noalloc") MarkNoAlloc(module, checks.Methods.Single(m => m.Name == "Direct"));
        if (negative)
        {
            var entry = new MethodDefinition("MalformedDefaultCtor", MethodAttributes.Static | MethodAttributes.Private, module.TypeSystem.Void);
            checks.Methods.Add(entry);
            var il = entry.Body.GetILProcessor();
            il.Emit(OpCodes.Ldnull); il.Emit(OpCodes.Ldc_I4_0); il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Call, checks.Methods.Single(m => m.Name == (args[3].StartsWith("address-", StringComparison.Ordinal) ? "Pointer" : "Direct")));
            il.Emit(OpCodes.Pop); il.Emit(OpCodes.Ret); module.EntryPoint = entry;
        }
        else module.EntryPoint = checks.Methods.Single(m => m.Name == (publicOnly ? "Run" : plannerOnly ? "PlannerChecks" : reflection ? "RunReflectionArtifact" : "RunArtifact"));
        app.Write(args[1], new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        Console.WriteLine("runtime-type-default-ctor: " + args[0] + "; exact instance bool,bool -> object; public oracle only");
        return true;
    }

    private static IEnumerable<TypeDefinition> AllTypes(TypeDefinition type)
    {
        yield return type;
        foreach (var nested in type.NestedTypes.SelectMany(AllTypes)) yield return nested;
    }

    private static void MarkNoAlloc(ModuleDefinition module, MethodDefinition method)
    {
        var attribute = new TypeDefinition("Dn2Cpp.Runtime", "HotPathAttribute", TypeAttributes.Class | TypeAttributes.Sealed,
            module.ImportReference(typeof(Attribute)));
        var constructor = new MethodDefinition(".ctor", MethodAttributes.Public | MethodAttributes.HideBySig
            | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName, module.TypeSystem.Void);
        var il = constructor.Body.GetILProcessor();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Call, new MethodReference(".ctor", module.TypeSystem.Void, module.ImportReference(typeof(Attribute))) { HasThis = true });
        il.Emit(OpCodes.Ret); attribute.Methods.Add(constructor);
        var setter = new MethodDefinition("set_NoAlloc", MethodAttributes.Public | MethodAttributes.HideBySig
            | MethodAttributes.SpecialName, module.TypeSystem.Void) { HasThis = true };
        setter.Parameters.Add(new ParameterDefinition(module.TypeSystem.Boolean)); setter.Body.GetILProcessor().Emit(OpCodes.Ret);
        attribute.Methods.Add(setter);
        attribute.Properties.Add(new PropertyDefinition("NoAlloc", PropertyAttributes.None, module.TypeSystem.Boolean) { SetMethod = setter });
        module.Types.Add(attribute);
        var mark = new CustomAttribute(constructor);
        mark.Properties.Add(new CustomAttributeNamedArgument("NoAlloc", new CustomAttributeArgument(module.TypeSystem.Boolean, true)));
        method.CustomAttributes.Add(mark);
    }
}
