using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class ActivatorDefaultFixture
{
    public static bool Run(string[] args)
    {
        if (args.Length is not (3 or 4) || !args[0].StartsWith("--activator-default", StringComparison.Ordinal)) return false;
        using var core = AssemblyDefinition.ReadAssembly(args[2]);
        var owner = core.MainModule.Types.Single(t => t.FullName == "System.Activator");
        var definition = owner.Methods.Single(m => m.Name == "CreateInstance" && m.IsAssembly && m.IsStatic
            && m.Parameters.Count == 3 && m.Parameters[0].ParameterType.FullName == "System.Type"
            && m.Parameters[1].ParameterType.MetadataType == MetadataType.Boolean
            && m.Parameters[2].ParameterType.MetadataType == MetadataType.Boolean);
        using var app = AssemblyDefinition.ReadAssembly(args[1], new ReaderParameters { InMemory = true });
        var module = app.MainModule;
        var checks = module.Types.Single(t => t.FullName == "ActivatorDefaultSubset.ActivatorChecks");
        bool oracle = args[0].Contains("oracle", StringComparison.Ordinal), reflection = args[0].EndsWith("reflection", StringComparison.Ordinal);
        bool publicOnly = args[0] == "--activator-default-public", negative = args[0] == "--activator-default-negative";
        MethodReference target = module.ImportReference(definition);
        if (negative)
        {
            target = new MethodReference(target.Name, target.ReturnType, target.DeclaringType);
            foreach (var parameter in definition.Parameters) target.Parameters.Add(new ParameterDefinition(module.ImportReference(parameter.ParameterType)));
            switch (args[3].Replace("address-", "", StringComparison.Ordinal))
            {
                case "return-int": target.ReturnType = module.TypeSystem.Int32; break;
                case "return-modifier": target.ReturnType = new OptionalModifierType(module.TypeSystem.Object, module.TypeSystem.Object); break;
                case "instance": target.HasThis = true; break;
                case "explicit": target.HasThis = true; target.ExplicitThis = true; break;
                case "vararg": target.CallingConvention = MethodCallingConvention.VarArg; break;
                case "type-valuetype": target.Parameters[0].ParameterType = new TypeReference("System", "Type", module, module.TypeSystem.CoreLibrary, true); break;
                case "named-bool": target.Parameters[1].ParameterType = new TypeReference("System", "Boolean", module, module.TypeSystem.CoreLibrary, true); break;
                case "bool-modifier": target.Parameters[2].ParameterType = new OptionalModifierType(module.TypeSystem.Object, module.TypeSystem.Boolean); break;
                default: throw new InvalidOperationException("Unknown malformed Activator signature");
            }
        }
        if (!oracle && !publicOnly)
        {
            foreach (string route in new[] { "Direct", "Pointer", "Address", "ReflectionLeaf", "Planner" })
            {
                var method = checks.Methods.Single(m => m.Name == route);
                method.Body = new MethodBody(method);
                var il = method.Body.GetILProcessor();
                if (route == "Address")
                {
                    il.Emit(OpCodes.Ldnull); il.Emit(OpCodes.Ldftn, target);
                    var constructor = new MethodReference(".ctor", module.TypeSystem.Void, method.ReturnType) { HasThis = true };
                    constructor.Parameters.Add(new ParameterDefinition(module.TypeSystem.Object));
                    constructor.Parameters.Add(new ParameterDefinition(module.TypeSystem.IntPtr));
                    il.Emit(OpCodes.Newobj, constructor);
                }
                else
                {
                    if (route == "Planner")
                    {
                        il.Emit(OpCodes.Ldtoken, checks.NestedTypes.Single(t => t.Name == "PlannerOnly"));
                        var getType = new MethodReference("GetTypeFromHandle", target.Parameters[0].ParameterType,
                            target.Parameters[0].ParameterType);
                        getType.Parameters.Add(new ParameterDefinition(module.ImportReference(typeof(RuntimeTypeHandle))));
                        il.Emit(OpCodes.Call, getType); il.Emit(OpCodes.Ldarg_0); il.Emit(OpCodes.Ldarg_1);
                    }
                    else { il.Emit(OpCodes.Ldarg_0); il.Emit(OpCodes.Ldarg_1); il.Emit(OpCodes.Ldarg_2); }
                    if (route == "Pointer")
                    {
                        il.Emit(OpCodes.Ldftn, target);
                        var site = new CallSite(module.TypeSystem.Object);
                        foreach (var parameter in definition.Parameters) site.Parameters.Add(new ParameterDefinition(module.ImportReference(parameter.ParameterType)));
                        il.Emit(OpCodes.Calli, site);
                    }
                    else il.Emit(OpCodes.Call, target);
                }
                il.Emit(OpCodes.Ret);
            }
        }
        if (!oracle && !publicOnly && !negative)
        {
            var plannerType = checks.NestedTypes.Single(t => t.Name == "PlannerOnly");
            var planner = checks.Methods.Single(m => m.Name == "Planner");
            int typeTokens = 0;
            foreach (var method in module.Types.SelectMany(AllTypes).SelectMany(t => t.Methods))
            {
                if (!method.HasBody) continue;
                foreach (var instruction in method.Body.Instructions)
                {
                    if (instruction.Operand is MethodReference member && member.DeclaringType.FullName == plannerType.FullName)
                        throw new InvalidOperationException("PlannerOnly constructor has an independent method root");
                    if (instruction.OpCode == OpCodes.Ldtoken && instruction.Operand is TypeReference type
                        && type.FullName == plannerType.FullName)
                    {
                        if (method != planner) throw new InvalidOperationException("PlannerOnly has an alternate Type token root");
                        typeTokens++;
                    }
                }
            }
            if (typeTokens != 1 || plannerType.HasCustomAttributes
                || !planner.Body.Instructions.Any(i => i.OpCode == OpCodes.Call && ReferenceEquals(i.Operand, target)))
                throw new InvalidOperationException("PlannerOnly must be rooted solely by the exact literal-type factory");
            Console.WriteLine("activator-default-planner: sole literal Type root, no constructor references or type annotations");
        }
        if (args[0] == "--activator-default-nocore")
        {
            // The full bucket has unrelated external generic declarations. Isolate the
            // public intrinsic control so no unrelated missing reference masks this route.
            using var control = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition(app.Name.Name, app.Name.Version),
                module.Name, ModuleKind.Console);
            var cm = control.MainModule;
            var type = new TypeDefinition("", "PublicActivatorControl", TypeAttributes.Class, cm.TypeSystem.Object);
            cm.Types.Add(type);
            var entry = new MethodDefinition("Main", MethodAttributes.Static | MethodAttributes.Public, cm.TypeSystem.Void);
            type.Methods.Add(entry);
            var factory = cm.ImportReference(owner.Methods.Single(m => m.Name == "CreateInstance" && m.IsPublic
                && m.Parameters.Count == 1 && m.Parameters[0].ParameterType.FullName == "System.Type"));
            var getType = new MethodReference("GetTypeFromHandle", factory.Parameters[0].ParameterType, factory.Parameters[0].ParameterType);
            getType.Parameters.Add(new ParameterDefinition(cm.ImportReference(typeof(RuntimeTypeHandle))));
            var il = entry.Body.GetILProcessor();
            il.Emit(OpCodes.Ldtoken, cm.TypeSystem.Int32); il.Emit(OpCodes.Call, getType);
            il.Emit(OpCodes.Call, factory); il.Emit(OpCodes.Pop); il.Emit(OpCodes.Ret);
            cm.EntryPoint = entry;
            control.Write(args[1], new WriterParameters { Timestamp = 0, DeterministicMvid = true });
            Console.WriteLine("activator-default: isolated public one-argument intrinsic without CoreLib");
            return true;
        }
        if (args[0] == "--activator-default-noalloc") MarkNoAlloc(module, checks.Methods.Single(m => m.Name == "Direct"));
        if (negative)
        {
            var entry = new MethodDefinition("MalformedActivator", MethodAttributes.Static | MethodAttributes.Private, module.TypeSystem.Void);
            checks.Methods.Add(entry);
            var il = entry.Body.GetILProcessor();
            il.Emit(OpCodes.Ldnull); il.Emit(OpCodes.Ldc_I4_0); il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Call, checks.Methods.Single(m => m.Name == (args[3].StartsWith("address-", StringComparison.Ordinal) ? "Pointer" : "Direct")));
            il.Emit(OpCodes.Pop); il.Emit(OpCodes.Ret); module.EntryPoint = entry;
        }
        else module.EntryPoint = checks.Methods.Single(m => m.Name == (publicOnly ? "Run" : reflection ? "RunReflectionArtifact" : "RunArtifact"));
        app.Write(args[1], new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        Console.WriteLine("activator-default: " + args[0] + "; exact static Type,bool,bool -> object; public oracle only");
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
