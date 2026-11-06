using System.Globalization;
using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class RuntimeTypeTypeCodeFixture
{
    public static bool Run(string[] args)
    {
        if (args.Length is not (3 or 4) || args[0] is not ("--runtime-type-type-code" or "--runtime-type-type-code-reflection"
            or "--runtime-type-type-code-negative" or "--runtime-type-type-code-public-scope" or "--runtime-type-type-code-public" or "--runtime-type-type-code-oracle" or "--runtime-type-type-code-oracle-reflection")) return false;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        using var core = AssemblyDefinition.ReadAssembly(args[2]);
        if (core.Name.Name != "System.Private.CoreLib") throw new InvalidOperationException("Expected CoreLib");
        var runtimeType = core.MainModule.GetType("System.RuntimeType");
        var targetDefinition = runtimeType.Methods.Single(m => m.Name == "GetTypeCodeImpl");
        if (!runtimeType.IsSealed
            || targetDefinition.Attributes != (MethodAttributes.Family | MethodAttributes.Virtual | MethodAttributes.HideBySig)
            || !targetDefinition.HasThis || targetDefinition.ExplicitThis || targetDefinition.HasGenericParameters
            || targetDefinition.CallingConvention != MethodCallingConvention.Default || targetDefinition.Parameters.Count != 0
            || targetDefinition.ReturnType.FullName != "System.TypeCode" || !targetDefinition.ReturnType.IsValueType || !targetDefinition.HasBody
            || targetDefinition.RVA == 0 || targetDefinition.IsPInvokeImpl || targetDefinition.ImplAttributes != MethodImplAttributes.IL
            || !targetDefinition.Body.Instructions.Any(i => i.OpCode == OpCodes.Call
                && i.Operand is MethodReference m && m.DeclaringType.FullName == "System.RuntimeType" && m.Name == "get_Cache")
            || !targetDefinition.Body.Instructions.Any(i => i.OpCode == OpCodes.Call
                && i.Operand is MethodReference m && m.DeclaringType.FullName == "System.Type" && m.Name == "GetRuntimeTypeCode"))
            throw new InvalidOperationException("RuntimeType.GetTypeCodeImpl declaration changed");
        // The raw-null oracle is justified by managed field access, not a private CLR call.
        var cache = runtimeType.Methods.Single(m => m.Name == "get_Cache");
        var body = targetDefinition.Body.Instructions;
        if (body.Count < 2 || body[0].OpCode != OpCodes.Ldarg_0 || body[1].OpCode != OpCodes.Call
            || body[1].Operand is not MethodReference first || first.FullName != cache.FullName
            || !cache.HasBody || cache.Body.Instructions.Count < 2
            || cache.Body.Instructions[0].OpCode != OpCodes.Ldarg_0
            || cache.Body.Instructions[1].OpCode != OpCodes.Ldfld
            || cache.Body.Instructions[1].Operand is not FieldReference field
            || field.DeclaringType.FullName != "System.RuntimeType" || field.Name != "m_cache")
            throw new InvalidOperationException("RuntimeType.GetTypeCodeImpl managed null validation changed");
        string path = Path.GetFullPath(args[1]);
        using var app = AssemblyDefinition.ReadAssembly(path, new ReaderParameters { InMemory = true });
        var module = app.MainModule;
        var checks = module.GetType("RuntimeTypeTypeCodeSubset.TypeCodeChecks");
        bool publicScope = args[0] == "--runtime-type-type-code-public-scope";
        bool publicOnly = publicScope || args[0] == "--runtime-type-type-code-public";
        bool reflectionOnly = args[0].EndsWith("-reflection", StringComparison.Ordinal);
        bool oracle = args[0].Contains("-oracle", StringComparison.Ordinal);
        string[] routes = publicOnly || oracle ? Array.Empty<string>() : reflectionOnly
            ? new[] { "ReflectionLeaf" }
            : new[] { "Direct", "Pointer", "Virtual", "VirtualPointer", "Constrained", "Address", "VirtualAddress", "FlowAddress", "ReflectionLeaf" };
        var target = module.ImportReference(targetDefinition);
        bool negative = args[0] == "--runtime-type-type-code-negative";
        if (negative)
        {
            if (args.Length != 4) throw new InvalidOperationException("Missing negative signature");
            var bad = new MethodReference(target.Name, target.ReturnType, target.DeclaringType)
                { HasThis = true, CallingConvention = MethodCallingConvention.Default };
            switch (args[3])
            {
                case "return-int": case "address-return-int": bad.ReturnType = module.TypeSystem.Int32; break;
                case "return-object": bad.ReturnType = module.TypeSystem.Object; break;
                case "return-enum": bad.ReturnType = module.ImportReference(core.MainModule.GetType("System.Reflection.TypeAttributes")); break;
                case "return-class": bad.ReturnType = new TypeReference("System", "TypeCode", module, target.ReturnType.Scope, false); break;
                case "return-byref": bad.ReturnType = new ByReferenceType(target.ReturnType); break;
                case "static": bad.HasThis = false; break;
                case "explicit": bad.ExplicitThis = true; break;
                case "vararg": bad.CallingConvention = MethodCallingConvention.VarArg; break;
                case "return-modifier": bad.ReturnType = new OptionalModifierType(module.TypeSystem.Object, target.ReturnType); break;
                default: throw new InvalidOperationException("Unknown negative signature");
            }
            target = bad;
            routes = new[] { args[3].StartsWith("address-", StringComparison.Ordinal) ? "Pointer" : "Direct" };
        }
        if (!negative && target.MetadataToken.TokenType != TokenType.MemberRef)
            throw new InvalidOperationException("Expected imported override MemberRef");
        foreach (string route in routes)
        {
            var stub = checks.Methods.Single(m => m.Name == route);
            stub.Body = new MethodBody(stub);
            var il = stub.Body.GetILProcessor();
            if (route == "Constrained")
            {
                stub.Body.InitLocals = true;
                var receiver = new VariableDefinition(module.ImportReference(runtimeType));
                stub.Body.Variables.Add(receiver);
                LoadReceiver(il, module, runtimeType);
                il.Emit(OpCodes.Stloc, receiver);
                il.Emit(OpCodes.Ldloca, receiver);
                il.Emit(OpCodes.Constrained, module.ImportReference(runtimeType));
                il.Emit(OpCodes.Callvirt, target);
            }
            else if (route == "FlowAddress")
            {
                stub.Body.InitLocals = true;
                var receiver = new VariableDefinition(module.ImportReference(runtimeType));
                var pointer = new VariableDefinition(module.TypeSystem.IntPtr);
                stub.Body.Variables.Add(receiver);
                stub.Body.Variables.Add(pointer);
                var direct = Instruction.Create(OpCodes.Ldftn, target);
                var bind = Instruction.Create(OpCodes.Ldloc, receiver);
                LoadReceiver(il, module, runtimeType);
                il.Emit(OpCodes.Stloc, receiver);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Brfalse, direct);
                il.Emit(OpCodes.Ldloc, receiver);
                il.Emit(OpCodes.Ldvirtftn, target);
                il.Emit(OpCodes.Stloc, pointer);
                il.Emit(OpCodes.Br, bind);
                il.Append(direct);
                il.Emit(OpCodes.Stloc, pointer);
                il.Append(bind);
                il.Emit(OpCodes.Ldloc, pointer);
                EmitDelegate(il, module, stub.ReturnType);
            }
            else
            {
                LoadReceiver(il, module, runtimeType);
                if (route is "Pointer" or "VirtualPointer")
                {
                    if (route == "VirtualPointer") il.Emit(OpCodes.Dup);
                    il.Emit(route == "VirtualPointer" ? OpCodes.Ldvirtftn : OpCodes.Ldftn, target);
                    var site = new CallSite(module.ImportReference(targetDefinition.ReturnType));
                    site.Parameters.Add(new ParameterDefinition(module.ImportReference(runtimeType)));
                    il.Emit(OpCodes.Calli, site);
                }
                else if (route is "Address" or "VirtualAddress")
                {
                    if (route == "VirtualAddress") il.Emit(OpCodes.Dup);
                    il.Emit(route == "VirtualAddress" ? OpCodes.Ldvirtftn : OpCodes.Ldftn, target);
                    EmitDelegate(il, module, stub.ReturnType);
                }
                else il.Emit(route == "Virtual" ? OpCodes.Callvirt : OpCodes.Call, target);
            }
            il.Emit(OpCodes.Ret);
        }
        module.EntryPoint = checks.Methods.Single(m => m.Name == (publicScope ? "RunPublicScope" : publicOnly ? "Run" : reflectionOnly ? "RunReflectionArtifact" : "RunArtifact"));
        app.Write(path, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        Console.WriteLine("runtime-type-type-code-sites=" + (publicOnly ? "public-only" : reflectionOnly ? "reflection-only-local-wrapper"
            : "direct,calli,callvirt,constrained,ldftn,ldvirtftn,flowing,reflection-wrapper")
            + ",exact-CoreLib-override,MemberRef," + (publicOnly || oracle ? "public-CLR-oracle" : "native-only"));
        return true;
    }

    private static void LoadReceiver(ILProcessor il, ModuleDefinition module, TypeDefinition runtimeType)
    {
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Castclass, module.ImportReference(runtimeType));
    }

    private static void EmitDelegate(ILProcessor il, ModuleDefinition module, TypeReference type)
    {
        var constructor = new MethodReference(".ctor", module.TypeSystem.Void, type) { HasThis = true };
        constructor.Parameters.Add(new ParameterDefinition(module.TypeSystem.Object));
        constructor.Parameters.Add(new ParameterDefinition(module.TypeSystem.IntPtr));
        il.Emit(OpCodes.Newobj, constructor);
    }
}
