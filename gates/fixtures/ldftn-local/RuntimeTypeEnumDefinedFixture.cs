using System.Globalization;
using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class RuntimeTypeEnumDefinedFixture
{
    public static bool Run(string[] args)
    {
        if (args.Length is not (3 or 4) || args[0] is not ("--runtime-type-enum-defined" or "--runtime-type-enum-defined-reflection"
            or "--runtime-type-enum-defined-public" or "--runtime-type-enum-defined-oracle" or "--runtime-type-enum-defined-oracle-reflection" or "--runtime-type-enum-defined-negative")) return false;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        using var core = AssemblyDefinition.ReadAssembly(args[2]);
        if (core.Name.Name != "System.Private.CoreLib") throw new InvalidOperationException("Expected CoreLib");
        var runtimeType = core.MainModule.GetType("System.RuntimeType");
        var targetDefinition = runtimeType.Methods.Single(m => m.Name == "IsEnumDefined");
        if (!runtimeType.IsSealed
            || targetDefinition.Attributes != (MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig)
            || !targetDefinition.HasThis || targetDefinition.ExplicitThis || targetDefinition.HasGenericParameters
            || targetDefinition.CallingConvention != MethodCallingConvention.Default || targetDefinition.Parameters.Count != 1
            || targetDefinition.Parameters[0].ParameterType.MetadataType != MetadataType.Object
            || targetDefinition.ReturnType.MetadataType != MetadataType.Boolean || !targetDefinition.HasBody
            || targetDefinition.RVA == 0 || targetDefinition.IsPInvokeImpl || targetDefinition.ImplAttributes != MethodImplAttributes.IL
            || !targetDefinition.Body.Instructions.Any(i => i.OpCode == OpCodes.Call
                && i.Operand is MethodReference m && m.DeclaringType.FullName == "System.Type" && m.Name == "IsIntegerType"))
            throw new InvalidOperationException("RuntimeType.IsEnumDefined declaration changed");
        // RawOracle is pinned to managed IL; no private VM entry is invoked.
        var body = targetDefinition.Body.Instructions;
        if (body.Count < 8 || body[0].OpCode != OpCodes.Ldarg_1 || body[1].OpCode != OpCodes.Ldstr
            || (string)body[1].Operand != "value" || body[2].OpCode != OpCodes.Call
            || body[2].Operand is not MethodReference nullCheck || nullCheck.DeclaringType.FullName != "System.ArgumentNullException"
            || nullCheck.Name != "ThrowIfNull" || body[3].OpCode != OpCodes.Ldarg_0
            || body[4].OpCode != OpCodes.Call || body[4].Operand is not MethodReference enumCheck
            || enumCheck.Name != "get_IsActualEnum" || enumCheck.DeclaringType.FullName != "System.RuntimeType")
            throw new InvalidOperationException("Managed enum-defined value-before-this validation changed");
        var actualEnum = runtimeType.Methods.Single(m => m.Name == "get_IsActualEnum");
        var nativeHandle = runtimeType.Methods.Single(m => m.Name == "GetNativeTypeHandle");
        if (actualEnum.Body.Instructions[0].OpCode != OpCodes.Ldarg_0
            || actualEnum.Body.Instructions[1].Operand is not MethodReference handleCall || handleCall.Name != "GetNativeTypeHandle"
            || nativeHandle.Body.Instructions[0].OpCode != OpCodes.Ldarg_0
            || nativeHandle.Body.Instructions[1].OpCode != OpCodes.Ldfld
            || nativeHandle.Body.Instructions[1].Operand is not FieldReference handleField || handleField.Name != "m_handle")
            throw new InvalidOperationException("Managed enum-defined null receiver dereference changed");
        string path = Path.GetFullPath(args[1]);
        using var app = AssemblyDefinition.ReadAssembly(path, new ReaderParameters { InMemory = true });
        var module = app.MainModule;
        var checks = module.GetType("RuntimeTypeEnumDefinedSubset.EnumDefinedChecks");
        bool publicOnly = args[0] == "--runtime-type-enum-defined-public";
        bool reflectionOnly = args[0].EndsWith("-reflection", StringComparison.Ordinal);
        bool oracle = args[0].Contains("-oracle", StringComparison.Ordinal);
        string[] routes = publicOnly || oracle ? Array.Empty<string>() : reflectionOnly
            ? new[] { "ReflectionLeaf" }
            : new[] { "Direct", "Pointer", "Virtual", "VirtualPointer", "Constrained", "Address", "VirtualAddress", "FlowAddress", "ReflectionLeaf" };
        var target = module.ImportReference(targetDefinition);
        bool negative = args[0] == "--runtime-type-enum-defined-negative";
        if (negative)
        {
            if (args.Length != 4) throw new InvalidOperationException("Missing negative signature");
            var bad = new MethodReference(target.Name, target.ReturnType, target.DeclaringType)
                { HasThis = true, CallingConvention = MethodCallingConvention.Default };
            bad.Parameters.Add(new ParameterDefinition(module.TypeSystem.Object));
            switch (args[3])
            {
                case "return-int": case "address-return-int": bad.ReturnType = module.TypeSystem.Int32; break;
                case "return-object": bad.ReturnType = module.TypeSystem.Object; break;
                case "parameter-string": bad.Parameters[0].ParameterType = module.TypeSystem.String; break;
                case "parameter-byref": bad.Parameters[0].ParameterType = new ByReferenceType(module.TypeSystem.Object); break;
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
                    // Keep receiver and value beneath the independent address load.
                    il.Emit(OpCodes.Ldarg_1);
                    if (route == "VirtualPointer") LoadReceiver(il, module, runtimeType);
                    il.Emit(route == "VirtualPointer" ? OpCodes.Ldvirtftn : OpCodes.Ldftn, target);
                    var site = new CallSite(module.TypeSystem.Boolean);
                    site.Parameters.Add(new ParameterDefinition(module.ImportReference(runtimeType)));
                    site.Parameters.Add(new ParameterDefinition(module.TypeSystem.Object));
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
        module.EntryPoint = checks.Methods.Single(m => m.Name == (publicOnly ? "Run" : reflectionOnly ? "RunReflectionArtifact" : "RunArtifact"));
        app.Write(path, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        Console.WriteLine("runtime-type-enum-defined-sites=" + (publicOnly ? "public-only" : reflectionOnly ? "reflection-only-local-wrapper"
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
