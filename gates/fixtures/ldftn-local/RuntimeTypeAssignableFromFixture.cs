using System.Globalization;
using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class RuntimeTypeAssignableFromFixture
{
    public static bool Run(string[] args)
    {
        if (args.Length is not (3 or 4) || args[0] is not ("--runtime-type-assignable-from" or "--runtime-type-assignable-from-reflection"
            or "--runtime-type-assignable-from-public" or "--runtime-type-assignable-from-oracle" or "--runtime-type-assignable-from-oracle-reflection" or "--runtime-type-assignable-from-negative")) return false;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        using var core = AssemblyDefinition.ReadAssembly(args[2]);
        if (core.Name.Name != "System.Private.CoreLib") throw new InvalidOperationException("Expected CoreLib");
        var runtimeType = core.MainModule.GetType("System.RuntimeType");
        var targetDefinition = runtimeType.Methods.Single(m => m.Name == "IsAssignableFrom" && m.Parameters.Count == 1
            && m.Parameters[0].ParameterType.FullName == "System.Type");
        if (!runtimeType.IsSealed
            || targetDefinition.Attributes != (MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig)
            || !targetDefinition.HasThis || targetDefinition.ExplicitThis || targetDefinition.HasGenericParameters
            || targetDefinition.CallingConvention != MethodCallingConvention.Default || targetDefinition.Parameters.Count != 1
            || targetDefinition.Parameters[0].ParameterType.FullName != "System.Type"
            || targetDefinition.Parameters[0].ParameterType.IsValueType
            || targetDefinition.ReturnType.MetadataType != MetadataType.Boolean || !targetDefinition.HasBody
            || targetDefinition.RVA == 0 || targetDefinition.IsPInvokeImpl || targetDefinition.ImplAttributes != MethodImplAttributes.IL
            || !targetDefinition.Body.Instructions.Any(i => i.OpCode == OpCodes.Call
                && i.Operand is MethodReference m && m.DeclaringType.FullName == "System.RuntimeTypeHandle" && m.Name == "CanCastTo"))
            throw new InvalidOperationException("RuntimeType.IsAssignableFrom(Type) declaration changed");
        // The oracle calls only this public managed override, never its private helpers.
        var body = targetDefinition.Body.Instructions;
        if (body.Count < 5 || body[0].OpCode != OpCodes.Ldarg_1
            || body[1].OpCode.FlowControl != FlowControl.Cond_Branch
            || body[2].OpCode != OpCodes.Ldc_I4_0 || body[3].OpCode != OpCodes.Ret)
            throw new InvalidOperationException("Managed assignability null-source-first validation changed");
        string path = Path.GetFullPath(args[1]);
        using var app = AssemblyDefinition.ReadAssembly(path, new ReaderParameters { InMemory = true });
        var module = app.MainModule;
        var checks = module.GetType("RuntimeTypeAssignableFromSubset.AssignableFromChecks");
        bool publicOnly = args[0] == "--runtime-type-assignable-from-public";
        bool reflectionOnly = args[0].EndsWith("-reflection", StringComparison.Ordinal);
        bool oracle = args[0].Contains("-oracle", StringComparison.Ordinal);
        string[] routes = publicOnly || oracle ? Array.Empty<string>() : reflectionOnly
            ? new[] { "ReflectionLeaf" }
            : new[] { "Direct", "Pointer", "Virtual", "VirtualPointer", "Constrained", "Address", "VirtualAddress", "FlowAddress", "ReflectionLeaf" };
        var target = module.ImportReference(targetDefinition);
        var sourceType = module.ImportReference(core.MainModule.GetType("System.Type"));
        bool negative = args[0] == "--runtime-type-assignable-from-negative";
        if (negative)
        {
            if (args.Length != 4) throw new InvalidOperationException("Missing negative signature");
            var bad = new MethodReference(target.Name, target.ReturnType, target.DeclaringType)
                { HasThis = true, CallingConvention = MethodCallingConvention.Default };
            bad.Parameters.Add(new ParameterDefinition(sourceType));
            switch (args[3])
            {
                case "return-int": case "address-return-int": bad.ReturnType = module.TypeSystem.Int32; break;
                case "return-object": bad.ReturnType = module.TypeSystem.Object; break;
                case "parameter-string": bad.Parameters[0].ParameterType = module.TypeSystem.String; break;
                case "parameter-byref": bad.Parameters[0].ParameterType = new ByReferenceType(sourceType); break;
                case "parameter-object": bad.Parameters[0].ParameterType = module.TypeSystem.Object; break;
                case "parameter-typeinfo": bad.Parameters[0].ParameterType = module.ImportReference(core.MainModule.GetType("System.Reflection.TypeInfo")); break;
                case "parameter-modifier": bad.Parameters[0].ParameterType = new OptionalModifierType(module.TypeSystem.Object, sourceType); break;
                case "static": bad.HasThis = false; break;
                case "explicit": bad.ExplicitThis = true; break;
                case "vararg": bad.CallingConvention = MethodCallingConvention.VarArg; break;
                case "return-modifier": bad.ReturnType = new OptionalModifierType(module.TypeSystem.Object, module.TypeSystem.Boolean); break;
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
                il.Emit(OpCodes.Ldarg_1);
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
                    // Keep receiver and source beneath the independent address load.
                    il.Emit(OpCodes.Ldarg_1);
                    if (route == "VirtualPointer") LoadReceiver(il, module, runtimeType);
                    il.Emit(route == "VirtualPointer" ? OpCodes.Ldvirtftn : OpCodes.Ldftn, target);
                    var site = new CallSite(module.TypeSystem.Boolean);
                    site.Parameters.Add(new ParameterDefinition(module.ImportReference(runtimeType)));
                    site.Parameters.Add(new ParameterDefinition(sourceType));
                    il.Emit(OpCodes.Calli, site);
                }
                else if (route is "Address" or "VirtualAddress")
                {
                    if (route == "VirtualAddress") il.Emit(OpCodes.Dup);
                    il.Emit(route == "VirtualAddress" ? OpCodes.Ldvirtftn : OpCodes.Ldftn, target);
                    EmitDelegate(il, module, stub.ReturnType);
                }
                else
                {
                    il.Emit(OpCodes.Ldarg_1);
                    il.Emit(route == "Virtual" ? OpCodes.Callvirt : OpCodes.Call, target);
                }
            }
            il.Emit(OpCodes.Ret);
        }
        if (negative)
        {
            // Only the malformed site is reachable; oracle-only routes must not mask it.
            var entry = new MethodDefinition("MalformedAssignableFrom", MethodAttributes.Private | MethodAttributes.Static,
                module.TypeSystem.Void);
            entry.Parameters.Add(new ParameterDefinition(new ArrayType(module.TypeSystem.String)));
            checks.Methods.Add(entry);
            var il = entry.Body.GetILProcessor();
            il.Emit(OpCodes.Ldnull);
            il.Emit(OpCodes.Ldnull);
            il.Emit(OpCodes.Call, checks.Methods.Single(m => m.Name == routes[0]));
            il.Emit(OpCodes.Pop);
            il.Emit(OpCodes.Ret);
            module.EntryPoint = entry;
        }
        else
        {
            module.EntryPoint = checks.Methods.Single(m => m.Name == (publicOnly ? "Run" : reflectionOnly ? "RunReflectionArtifact" : "RunArtifact"));
        }
        app.Write(path, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        Console.WriteLine("runtime-type-assignable-from-sites=" + (publicOnly ? "public-only" : reflectionOnly ? "reflection-only-local-wrapper"
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
