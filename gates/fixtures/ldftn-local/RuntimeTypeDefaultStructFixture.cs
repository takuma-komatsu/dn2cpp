using System.Globalization;
using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class RuntimeTypeDefaultStructFixture
{
    public static bool Run(string[] args)
    {
        if (args.Length is not (3 or 4) || !args[0].StartsWith("--runtime-type-default-struct", StringComparison.Ordinal)) return false;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture; CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        using var core = AssemblyDefinition.ReadAssembly(args[2]);
        var owner = core.MainModule.GetType("System.RuntimeType");
        var definition = owner.Methods.Single(m => m.Name == "CallDefaultStructConstructor");
        if (core.Name.Name != "System.Private.CoreLib" || definition.Attributes != (MethodAttributes.Assembly | MethodAttributes.HideBySig)
            || !definition.HasThis || definition.ExplicitThis || definition.HasGenericParameters || !definition.HasBody
            || definition.CallingConvention != MethodCallingConvention.Default || definition.ReturnType.MetadataType != MetadataType.Void
            || definition.Parameters.Count != 1 || definition.Parameters[0].ParameterType is not ByReferenceType { ElementType.MetadataType: MetadataType.Byte }
            || definition.ImplAttributes != MethodImplAttributes.IL)
            throw new InvalidOperationException("CallDefaultStructConstructor declaration changed");
        using var app = AssemblyDefinition.ReadAssembly(args[1], new ReaderParameters { InMemory = true });
        var module = app.MainModule;
        var checks = module.GetType("RuntimeTypeDefaultStructSubset.StructChecks");
        if (args[0] == "--runtime-type-default-struct-inspect-planner")
        {
            foreach (string name in new[] { "PlanPublic", "PlanGeneric" })
            {
                var type = checks.NestedTypes.Single(t => t.Name == name);
                if (!type.Methods.Any(m => m.IsConstructor && !m.IsStatic && m.IsPublic && m.Parameters.Count == 0
                    && RuntimeTypeDefaultCtorFixture.HasExecutableBody(m))) throw new InvalidOperationException("Lost default ctor " + name);
                if (!type.Methods.Any(m => m.IsConstructor && m.IsStatic && RuntimeTypeDefaultCtorFixture.HasExecutableBody(m)))
                    throw new InvalidOperationException("Lost cctor " + name);
                if (type.Methods.Any(m => m.IsConstructor && !m.IsStatic && m.Parameters.Count != 0
                    && RuntimeTypeDefaultCtorFixture.HasExecutableBody(m))) throw new InvalidOperationException("Extra ctor " + name);
            }
            if (checks.NestedTypes.Single(t => t.Name == "PlanPrivate").Methods.Any(m => m.IsConstructor && !m.IsStatic
                && RuntimeTypeDefaultCtorFixture.HasExecutableBody(m))) throw new InvalidOperationException("Private ctor retained");
            using var dietLibrary = AssemblyDefinition.ReadAssembly(Path.Combine(Path.GetDirectoryName(args[1])!, "DefaultStructCtorLib.dll"));
            var hiddenCtor = dietLibrary.MainModule.GetType("DefaultStructCtorLib.ExternalPrivate").Methods.SingleOrDefault(m => m.IsConstructor
                && !m.IsStatic && m.Parameters.Count == 0);
            if (hiddenCtor is null || !hiddenCtor.IsPrivate || RuntimeTypeDefaultCtorFixture.HasExecutableBody(hiddenCtor))
                throw new InvalidOperationException("External private0ctor must retain its row without an executable body");
            Console.WriteLine("default-struct-planner: public default bodies and cctors, no private/argument bodies");
            return true;
        }
        // A legal metadata access-bit edit lets the PUBLIC generic factory oracle cover this CLR rule.
        foreach (string name in new[] { "PrivateCtor", "PlanPrivate" })
        {
            var ctor = checks.NestedTypes.Single(t => t.Name == name).Methods.Single(m => m.IsConstructor && !m.IsStatic && m.Parameters.Count == 0);
            if (!ctor.IsPublic) throw new InvalidOperationException("Expected unmodified public struct ctor");
            ctor.Attributes = (ctor.Attributes & ~MethodAttributes.MemberAccessMask) | MethodAttributes.Private;
        }
        string libraryPath = Path.Combine(Path.GetDirectoryName(args[1])!, "DefaultStructCtorLib.dll");
        using (var library = AssemblyDefinition.ReadAssembly(libraryPath, new ReaderParameters { InMemory = true }))
        {
            var ctor = library.MainModule.GetType("DefaultStructCtorLib.ExternalPrivate").Methods.Single(m => m.IsConstructor
                && !m.IsStatic && m.Parameters.Count == 0);
            if (!ctor.IsPublic) throw new InvalidOperationException("Expected unmodified external public0ctor");
            ctor.Attributes = (ctor.Attributes & ~MethodAttributes.MemberAccessMask) | MethodAttributes.Private;
            library.Write(libraryPath, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        }
        bool oracle = args[0].Contains("oracle", StringComparison.Ordinal);
        bool reflection = args[0].EndsWith("reflection", StringComparison.Ordinal);
        bool planner = args[0].EndsWith("planner", StringComparison.Ordinal);
        bool publicOnly = args[0] == "--runtime-type-default-struct-public";
        bool negative = args[0] == "--runtime-type-default-struct-negative";
        MethodReference target = module.ImportReference(definition);
        if (negative)
        {
            target = new MethodReference(target.Name, target.ReturnType, target.DeclaringType) { HasThis = true };
            target.Parameters.Add(new ParameterDefinition(new ByReferenceType(module.TypeSystem.Byte)));
            switch (args[3].Replace("address-", "", StringComparison.Ordinal))
            {
                case "return-object": target.ReturnType = module.TypeSystem.Object; break;
                case "static": target.HasThis = false; break;
                case "explicit": target.ExplicitThis = true; break;
                case "vararg": target.CallingConvention = MethodCallingConvention.VarArg; break;
                case "value-byte": target.Parameters[0].ParameterType = module.TypeSystem.Byte; break;
                case "byref-int": target.Parameters[0].ParameterType = new ByReferenceType(module.TypeSystem.Int32); break;
                case "modifier": target.Parameters[0].ParameterType = new OptionalModifierType(module.TypeSystem.Object, target.Parameters[0].ParameterType); break;
                default: throw new InvalidOperationException("Unknown malformed struct ctor signature");
            }
        }
        string[] planners = { "PlannerPublic", "PlannerPrivate", "PlannerImplicit", "GenericPlanner" };
        if (!oracle && !publicOnly)
        {
            string[] routes = negative ? new[] { args[3].StartsWith("address-", StringComparison.Ordinal) ? "Pointer" : "Direct" }
                : reflection ? new[] { "ReflectionLeaf" } : planner ? planners
                : new[] { "Direct", "Virtual", "Pointer", "Address", "ReflectionLeaf" }.Concat(planners).ToArray();
            foreach (string route in routes)
            {
                var method = checks.Methods.Single(m => m.Name == route);
                method.Body = new MethodBody(method); var il = method.Body.GetILProcessor();
                if (planners.Contains(route))
                {
                    TypeReference type = route == "GenericPlanner" ? method.GenericParameters[0]
                        : checks.NestedTypes.Single(t => t.Name == (route == "PlannerPublic" ? "PlanPublic" : route == "PlannerImplicit" ? "PlanImplicit" : "PlanPrivate"));
                    il.Emit(OpCodes.Ldtoken, type);
                    var publicType = module.ImportReference(typeof(Type));
                    var getType = new MethodReference("GetTypeFromHandle", publicType, publicType);
                    getType.Parameters.Add(new ParameterDefinition(module.ImportReference(typeof(RuntimeTypeHandle))));
                    il.Emit(OpCodes.Call, getType);
                }
                else il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Castclass, module.ImportReference(owner));
                if (route == "Address")
                {
                    il.Emit(OpCodes.Ldftn, target);
                    var ctor = new MethodReference(".ctor", module.TypeSystem.Void, method.ReturnType) { HasThis = true };
                    ctor.Parameters.Add(new ParameterDefinition(module.TypeSystem.Object)); ctor.Parameters.Add(new ParameterDefinition(module.TypeSystem.IntPtr));
                    il.Emit(OpCodes.Newobj, ctor);
                }
                else
                {
                    il.Emit(planners.Contains(route) ? OpCodes.Ldarg_0 : OpCodes.Ldarg_1);
                    if (route == "Pointer")
                    {
                        il.Emit(OpCodes.Ldftn, target); var site = new CallSite(module.TypeSystem.Void);
                        site.Parameters.Add(new ParameterDefinition(module.ImportReference(owner)));
                        site.Parameters.Add(new ParameterDefinition(new ByReferenceType(module.TypeSystem.Byte)));
                        il.Emit(OpCodes.Calli, site);
                    }
                    else il.Emit(route == "Virtual" ? OpCodes.Callvirt : OpCodes.Call, target);
                }
                il.Emit(OpCodes.Ret);
            }
            if (!negative && !reflection)
                foreach (var type in checks.NestedTypes.Where(t => t.Name is "PlanPublic" or "PlanGeneric" or "PlanPrivate" or "PlanImplicit"))
                    foreach (var method in module.Types.SelectMany(AllTypes).SelectMany(t => t.Methods).Where(m => m.HasBody))
                        foreach (var instruction in method.Body.Instructions)
                        {
                            if (instruction.Operand is MethodReference member && member.DeclaringType.FullName == type.FullName)
                                throw new InvalidOperationException("Planner has an independent ctor/cctor root");
                            if (instruction.OpCode == OpCodes.Ldtoken && instruction.Operand is TypeReference token && token.FullName == type.FullName
                                && !planners.Contains(method.Name)) throw new InvalidOperationException("Planner has alternate Type root");
                        }
        }
        if (args[0] == "--runtime-type-default-struct-noalloc") RuntimeTypeDefaultCtorFixture.MarkNoAlloc(module, checks.Methods.Single(m => m.Name == "Direct"));
        if (negative)
        {
            var entry = new MethodDefinition("MalformedStructCtor", MethodAttributes.Private | MethodAttributes.Static, module.TypeSystem.Void);
            checks.Methods.Add(entry); entry.Body.InitLocals = true; var local = new VariableDefinition(module.TypeSystem.Byte); entry.Body.Variables.Add(local);
            var il = entry.Body.GetILProcessor(); il.Emit(OpCodes.Ldnull); il.Emit(OpCodes.Ldloca, local);
            il.Emit(OpCodes.Call, checks.Methods.Single(m => m.Name == (args[3].StartsWith("address-", StringComparison.Ordinal) ? "Pointer" : "Direct")));
            il.Emit(OpCodes.Ret); module.EntryPoint = entry;
        }
        else module.EntryPoint = checks.Methods.Single(m => m.Name == (publicOnly ? "Run" : planner ? "PlannerChecks" : reflection ? "RunReflectionArtifact" : "RunArtifact"));
        app.Write(args[1], new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        Console.WriteLine("default-struct: " + args[0] + "; exact instance void(byte&); public oracle only");
        return true;
    }
    private static IEnumerable<TypeDefinition> AllTypes(TypeDefinition type)
    {
        yield return type;
        foreach (var nested in type.NestedTypes.SelectMany(AllTypes)) yield return nested;
    }
}
