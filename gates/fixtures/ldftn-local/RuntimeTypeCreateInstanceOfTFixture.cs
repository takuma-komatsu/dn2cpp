using System.Globalization;
using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class RuntimeTypeCreateInstanceOfTFixture
{
    public static bool Run(string[] args)
    {
        if (args.Length is not (3 or 4) || !args[0].StartsWith("--runtime-type-create-instance-of-t", StringComparison.Ordinal)) return false;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        using var core = AssemblyDefinition.ReadAssembly(args[2]);
        var owner = core.MainModule.GetType("System.RuntimeType");
        var definition = owner.Methods.Single(m => m.Name == "CreateInstanceOfT");
        if (core.Name.Name != "System.Private.CoreLib" || definition.Attributes != (MethodAttributes.Assembly | MethodAttributes.HideBySig)
            || !definition.HasThis || definition.ExplicitThis || definition.HasGenericParameters || !definition.HasBody
            || definition.CallingConvention != MethodCallingConvention.Default || definition.ReturnType.MetadataType != MetadataType.Object
            || definition.Parameters.Count != 0 || definition.ImplAttributes != MethodImplAttributes.IL)
            throw new InvalidOperationException("RuntimeType.CreateInstanceOfT declaration changed");
        using var app = AssemblyDefinition.ReadAssembly(args[1], new ReaderParameters { InMemory = true });
        var module = app.MainModule;
        var checks = module.GetType("RuntimeTypeDefaultCtorSubset.DefaultCtorChecks");
        if (args[0] == "--runtime-type-create-instance-of-t-inspect-planner")
        {
            foreach (var type in checks.NestedTypes.Where(t => t.Name.StartsWith("OfTPlan", StringComparison.Ordinal)))
                foreach (var ctor in type.Methods.Where(m => m.IsConstructor && !m.IsStatic))
                {
                    bool expected = ctor.Parameters.Count == 0 && ctor.IsPublic;
                    if (RuntimeTypeDefaultCtorFixture.HasExecutableBody(ctor) != expected)
                        throw new InvalidOperationException("OfT planner retained wrong constructor body: " + ctor.FullName);
                }
            foreach (string name in new[] { "OfTPlanPublic", "OfTPlanGeneric" })
                if (!checks.NestedTypes.Single(t => t.Name == name).Methods.Any(m => m.IsConstructor && m.IsPublic
                    && m.Parameters.Count == 0 && RuntimeTypeDefaultCtorFixture.HasExecutableBody(m)))
                    throw new InvalidOperationException("OfT planner lost public default constructor: " + name);
            Console.WriteLine("runtime-type-oft-planner-diet: public default constructors only");
            return true;
        }
        bool oracle = args[0].Contains("oracle", StringComparison.Ordinal);
        bool reflection = args[0].EndsWith("reflection", StringComparison.Ordinal);
        bool planner = args[0].EndsWith("planner", StringComparison.Ordinal);
        bool publicOnly = args[0] == "--runtime-type-create-instance-of-t-public";
        bool negative = args[0] == "--runtime-type-create-instance-of-t-negative";
        MethodReference target = module.ImportReference(definition);
        if (negative)
        {
            target = new MethodReference(target.Name, target.ReturnType, target.DeclaringType) { HasThis = true };
            switch (args[3].Replace("address-", "", StringComparison.Ordinal))
            {
                case "return-int": target.ReturnType = module.TypeSystem.Int32; break;
                case "return-modifier": target.ReturnType = new OptionalModifierType(module.TypeSystem.Object, module.TypeSystem.Object); break;
                case "static": target.HasThis = false; break;
                case "explicit": target.ExplicitThis = true; break;
                case "vararg": target.CallingConvention = MethodCallingConvention.VarArg; break;
                case "generic": target.GenericParameters.Add(new GenericParameter("T", target)); break;
                case "extra-argument": target.Parameters.Add(new ParameterDefinition(module.TypeSystem.Boolean)); break;
                default: throw new InvalidOperationException("Unknown malformed CreateInstanceOfT signature");
            }
        }
        string[] planners = { "OfTPlannerPublic", "OfTPlannerPrivate", "OfTGenericPlanner" };
        if (!oracle && !publicOnly)
        {
            string[] routes = negative ? new[] { args[3].StartsWith("address-", StringComparison.Ordinal) ? "OfTPointer" : "OfTDirect" }
                : reflection ? new[] { "OfTReflectionLeaf" } : planner ? planners
                : new[] { "OfTDirect", "OfTVirtual", "OfTPointer", "OfTAddress", "OfTReflectionLeaf" }.Concat(planners).ToArray();
            foreach (string route in routes)
            {
                var method = checks.Methods.Single(m => m.Name == route);
                method.Body = new MethodBody(method);
                var il = method.Body.GetILProcessor();
                if (planners.Contains(route))
                {
                    TypeReference receiver = route == "OfTGenericPlanner" ? method.GenericParameters[0]
                        : checks.NestedTypes.Single(t => t.Name == (route == "OfTPlannerPublic" ? "OfTPlanPublic" : "OfTPlanPrivate"));
                    il.Emit(OpCodes.Ldtoken, receiver);
                    var type = module.ImportReference(typeof(Type));
                    var getType = new MethodReference("GetTypeFromHandle", type, type);
                    getType.Parameters.Add(new ParameterDefinition(module.ImportReference(typeof(RuntimeTypeHandle))));
                    il.Emit(OpCodes.Call, getType);
                }
                else il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Castclass, module.ImportReference(owner));
                if (route is "OfTPointer" or "OfTAddress")
                {
                    il.Emit(OpCodes.Ldftn, target);
                    if (route == "OfTPointer")
                    {
                        var site = new CallSite(module.TypeSystem.Object);
                        site.Parameters.Add(new ParameterDefinition(module.ImportReference(owner)));
                        il.Emit(OpCodes.Calli, site);
                    }
                    else
                    {
                        var constructor = new MethodReference(".ctor", module.TypeSystem.Void, method.ReturnType) { HasThis = true };
                        constructor.Parameters.Add(new ParameterDefinition(module.TypeSystem.Object));
                        constructor.Parameters.Add(new ParameterDefinition(module.TypeSystem.IntPtr));
                        il.Emit(OpCodes.Newobj, constructor);
                    }
                }
                else il.Emit(route == "OfTVirtual" ? OpCodes.Callvirt : OpCodes.Call, target);
                il.Emit(OpCodes.Ret);
            }
            if (!negative && !reflection)
                foreach (var type in checks.NestedTypes.Where(t => t.Name.StartsWith("OfTPlan", StringComparison.Ordinal)))
                {
                    if (type.HasCustomAttributes) throw new InvalidOperationException("OfT planner type must have no annotation root");
                    foreach (var method in module.Types.SelectMany(AllTypes).SelectMany(t => t.Methods).Where(m => m.HasBody))
                        foreach (var instruction in method.Body.Instructions)
                        {
                            if (instruction.Operand is MethodReference member && member.DeclaringType.FullName == type.FullName)
                                throw new InvalidOperationException("OfT planner constructor has an independent root");
                            if (instruction.OpCode == OpCodes.Ldtoken && instruction.Operand is TypeReference token && token.FullName == type.FullName
                                && !planners.Contains(method.Name))
                                throw new InvalidOperationException("OfT planner has an alternate Type token root");
                        }
                }
        }
        if (args[0] == "--runtime-type-create-instance-of-t-noalloc")
            RuntimeTypeDefaultCtorFixture.MarkNoAlloc(module, checks.Methods.Single(m => m.Name == "OfTDirect"));
        if (negative)
        {
            var entry = new MethodDefinition("MalformedOfT", MethodAttributes.Static | MethodAttributes.Private, module.TypeSystem.Void);
            checks.Methods.Add(entry);
            var il = entry.Body.GetILProcessor();
            il.Emit(OpCodes.Ldnull);
            il.Emit(OpCodes.Call, checks.Methods.Single(m => m.Name == (args[3].StartsWith("address-", StringComparison.Ordinal) ? "OfTPointer" : "OfTDirect")));
            il.Emit(OpCodes.Pop); il.Emit(OpCodes.Ret); module.EntryPoint = entry;
        }
        else module.EntryPoint = checks.Methods.Single(m => m.Name == (publicOnly ? "RunOfT" : planner ? "OfTPlannerChecks"
            : reflection ? "RunOfTReflectionArtifact" : "RunOfTArtifact"));
        app.Write(args[1], new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        Console.WriteLine("runtime-type-oft: " + args[0] + "; nongeneric instance object(); public reference-type oracle only");
        return true;
    }
    private static IEnumerable<TypeDefinition> AllTypes(TypeDefinition type)
    {
        yield return type;
        foreach (var nested in type.NestedTypes.SelectMany(AllTypes)) yield return nested;
    }
}
