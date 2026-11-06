using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class RuntimeTypeModuleFixture
{
    public static bool Run(string[] args)
    {
        if (args.Length != 3 || (!args[0].StartsWith("--runtime-type-module-negative-", StringComparison.Ordinal) && args[0] is not ("--runtime-type-module" or "--runtime-type-module-reflection" or "--runtime-type-module-public" or "--runtime-type-module-oracle" or "--runtime-type-module-oracle-reflection"))) return false;
        using var core = AssemblyDefinition.ReadAssembly(args[2]);
        if (core.Name.Name != "System.Private.CoreLib") throw new InvalidOperationException("Expected input CoreLib");
        var runtimeType = core.MainModule.GetType("System.RuntimeType");
        var runtimeModule = core.MainModule.GetType("System.Reflection.RuntimeModule");
        var moduleType = core.MainModule.GetType("System.Reflection.Module");
        var owner = core.MainModule.GetType("System.RuntimeTypeHandle");
        var helper = owner.Methods.Single(m => m.Name == "GetModule");
        var getter = runtimeType.Methods.Single(m => m.Name == "get_Module");
        var runtimeGetter = runtimeType.Methods.Single(m => m.Name == "GetRuntimeModule");
        Validate(helper, getter, runtimeGetter, runtimeType, runtimeModule, moduleType);
        string path = Path.GetFullPath(args[1]);
        using var app = AssemblyDefinition.ReadAssembly(path, new ReaderParameters { InMemory = true });
        var module = app.MainModule;
        var checks = module.GetType("RuntimeTypeModuleSubset.RuntimeModuleChecks");
        bool publicOnly = args[0] == "--runtime-type-module-public";
        bool reflectionOnly = args[0].EndsWith("-reflection", StringComparison.Ordinal);
        bool oracle = args[0].Contains("-oracle", StringComparison.Ordinal);
        string[] routes = publicOnly || oracle ? Array.Empty<string>() : reflectionOnly
            ? new[] { "Reflection", "ReflectionMethod", "ObjectReflection" }
            : new[] { "Direct", "Pointer", "RuntimeGetter", "RuntimeGetterPointer", "ObjectRuntimeGetter", "Getter", "GetterPointer", "VirtualGetter", "ConstrainedGetter", "Address", "GetterAddress", "GetterAddressLocal",
                "ObjectDirect", "ObjectPointer", "ObjectLocal", "Reflection", "ReflectionMethod", "ObjectReflection" };
        foreach (string route in routes)
        {
            var stub = checks.Methods.Single(m => m.Name == route);
            stub.Body = new MethodBody(stub);
            var il = stub.Body.GetILProcessor();
            var target = module.ImportReference(route is "Getter" or "GetterPointer" or "VirtualGetter" or "ConstrainedGetter"
                or "GetterAddress" or "GetterAddressLocal"
                ? getter : route is "RuntimeGetter" or "RuntimeGetterPointer" or "ObjectRuntimeGetter" ? runtimeGetter : helper);
            if (target.MetadataToken.TokenType != TokenType.MemberRef)
                throw new InvalidOperationException("Expected imported MemberRef target");
            if (route is "Reflection" or "ReflectionMethod" or "ObjectReflection")
            {
                il.Emit(OpCodes.Ldtoken, module.ImportReference(owner));
                il.Emit(OpCodes.Call, module.ImportReference(typeof(Type).GetMethod("GetTypeFromHandle")!));
                il.Emit(OpCodes.Ldstr, target.Name);
                il.Emit(OpCodes.Ldc_I4, (int)(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static));
                il.Emit(OpCodes.Ldc_I4_1);
                il.Emit(OpCodes.Newarr, module.ImportReference(typeof(Type)));
                il.Emit(OpCodes.Dup);
                il.Emit(OpCodes.Ldc_I4_0);
                il.Emit(OpCodes.Ldtoken, module.ImportReference(runtimeType));
                il.Emit(OpCodes.Call, module.ImportReference(typeof(Type).GetMethod("GetTypeFromHandle")!));
                il.Emit(OpCodes.Stelem_Ref);
                il.Emit(OpCodes.Callvirt, module.ImportReference(typeof(Type).GetMethod("GetMethod",
                    new[] { typeof(string), typeof(System.Reflection.BindingFlags), typeof(Type[]) })!));
                if (route != "ReflectionMethod")
                {
                    il.Emit(OpCodes.Ldnull);
                    il.Emit(OpCodes.Ldc_I4_1);
                    il.Emit(OpCodes.Newarr, module.TypeSystem.Object);
                    il.Emit(OpCodes.Dup);
                    il.Emit(OpCodes.Ldc_I4_0);
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Stelem_Ref);
                    il.Emit(OpCodes.Callvirt, module.ImportReference(typeof(System.Reflection.MethodBase)
                        .GetMethod("Invoke", new[] { typeof(object), typeof(object[]) })!));
                    if (route == "Reflection")
                        il.Emit(OpCodes.Castclass, module.ImportReference(moduleType));
                }
            }
            else if (route == "ConstrainedGetter")
            {
                stub.Body.InitLocals = true;
                var local = new VariableDefinition(module.ImportReference(runtimeType));
                stub.Body.Variables.Add(local);
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Castclass, module.ImportReference(runtimeType));
                il.Emit(OpCodes.Stloc, local);
                il.Emit(OpCodes.Ldloca, local);
                il.Emit(OpCodes.Constrained, module.ImportReference(runtimeType));
                il.Emit(OpCodes.Callvirt, target);
            }
            else if (route == "Address")
            {
                il.Emit(OpCodes.Ldnull);
                il.Emit(OpCodes.Ldftn, target);
                EmitDelegate(il, module, stub.ReturnType);
            }
            else
            {
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Castclass, module.ImportReference(runtimeType));
                if (route is "Pointer" or "ObjectPointer" or "GetterPointer" or "RuntimeGetterPointer")
                {
                    if (route == "GetterPointer") il.Emit(OpCodes.Dup);
                    il.Emit(route == "GetterPointer" ? OpCodes.Ldvirtftn : OpCodes.Ldftn, target);
                    var site = new CallSite(target.ReturnType);
                    site.Parameters.Add(new ParameterDefinition(module.ImportReference(runtimeType)));
                    il.Emit(OpCodes.Calli, site);
                }
                else if (route is "GetterAddress" or "GetterAddressLocal")
                {
                    il.Emit(OpCodes.Dup);
                    il.Emit(OpCodes.Ldvirtftn, target);
                    if (route == "GetterAddressLocal")
                    {
                        stub.Body.InitLocals = true;
                        var address = new VariableDefinition(module.TypeSystem.IntPtr);
                        stub.Body.Variables.Add(address);
                        il.Emit(OpCodes.Stloc, address);
                        il.Emit(OpCodes.Ldloc, address);
                    }
                    EmitDelegate(il, module, stub.ReturnType);
                }
                else
                {
                    il.Emit(route == "VirtualGetter" ? OpCodes.Callvirt : OpCodes.Call, target);
                    if (route == "ObjectLocal")
                    {
                        stub.Body.InitLocals = true;
                        var local = new VariableDefinition(module.ImportReference(runtimeModule));
                        stub.Body.Variables.Add(local);
                        il.Emit(OpCodes.Stloc, local);
                        il.Emit(OpCodes.Ldloc, local);
                    }
                }
            }
            il.Emit(OpCodes.Ret);
        }
        const string negativePrefix = "--runtime-type-module-negative-";
        if (args[0].StartsWith(negativePrefix, StringComparison.Ordinal))
        {
            string shape = args[0][negativePrefix.Length..];
            bool address = shape.StartsWith("address-", StringComparison.Ordinal);
            if (address) shape = shape["address-".Length..];
            bool internalBody = shape.StartsWith("internal-", StringComparison.Ordinal);
            if (internalBody) shape = shape["internal-".Length..];
            bool instance = shape.StartsWith("override-", StringComparison.Ordinal);
            if (instance) shape = shape["override-".Length..];
            var stub = checks.Methods.Single(m => m.Name == (internalBody ? "RuntimeGetter" : instance ? (address ? "GetterAddress" : "Getter") : (address ? "Address" : "Direct")));
            var call = stub.Body.Instructions.Single(i => i.OpCode == (address ? (instance ? OpCodes.Ldvirtftn : OpCodes.Ldftn) : OpCodes.Call));
            var definition = internalBody ? runtimeGetter : instance ? getter : helper;
            instance |= internalBody;
            var bad = new MethodReference(definition.Name, module.ImportReference(definition.ReturnType), module.ImportReference(definition.DeclaringType)) { HasThis = instance };
            if (!instance) bad.Parameters.Add(new ParameterDefinition(module.ImportReference(runtimeType)));
            switch (shape)
            {
                case "return": bad.ReturnType = module.TypeSystem.Int32; break;
                case "parameter": bad.Parameters[0].ParameterType = module.TypeSystem.Object; break;
                case "hasthis": bad.HasThis = true; break;
                case "static": bad.HasThis = false; break;
                case "vararg": bad.CallingConvention = MethodCallingConvention.VarArg; break;
                case "return-valuetype":
                    bad.ReturnType = new TypeReference(definition.ReturnType.Namespace, definition.ReturnType.Name, module,
                        module.ImportReference(definition.ReturnType).Scope, true);
                    break;
                default: throw new InvalidOperationException("Unknown negative module reference");
            }
            call.Operand = bad;
        }
        module.EntryPoint = checks.Methods.Single(m => m.Name == (publicOnly ? "Run" : reflectionOnly ? "RunReflectionArtifact" : "RunArtifact"));
        app.Write(path, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        Console.WriteLine("runtime-type-module-sites=" + (publicOnly ? "public-only" : reflectionOnly ? "reflection-only" : "direct,calli,getter,callvirt,constrained,ldftn,ldvirtftn,object,reflection")
            + ",exact-CoreLib-declarations,RuntimeModule-return,retained-getter-IL," + (publicOnly || oracle ? "public-CLR-oracle" : "native-only") + "");
        return true;
    }

    private static void Validate(MethodDefinition helper, MethodDefinition getter, MethodDefinition runtimeGetter, TypeDefinition runtimeType,
        TypeDefinition runtimeModule, TypeDefinition moduleType)
    {
        if (!runtimeType.IsSealed || runtimeModule.BaseType.Resolve() != moduleType
            || helper.Attributes != (MethodAttributes.Assembly | MethodAttributes.Static | MethodAttributes.HideBySig)
            || helper.HasThis || helper.ExplicitThis || helper.HasGenericParameters
            || helper.CallingConvention != MethodCallingConvention.Default || helper.IsPInvokeImpl
            || helper.ReturnType.Resolve() != runtimeModule || helper.Parameters.Count != 1
            || helper.Parameters[0].ParameterType.Resolve() != runtimeType
            || !helper.HasBody || helper.RVA == 0 || helper.ImplAttributes != MethodImplAttributes.IL
            || getter.Attributes != (MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig | MethodAttributes.SpecialName)
            || !getter.HasThis || getter.ExplicitThis || getter.HasGenericParameters
            || getter.CallingConvention != MethodCallingConvention.Default || !getter.HasBody || getter.RVA == 0
            || getter.ImplAttributes != MethodImplAttributes.IL || getter.IsPInvokeImpl || getter.Parameters.Count != 0
            || getter.ReturnType.Resolve() != moduleType || getter.Body.HasExceptionHandlers || getter.Body.HasVariables
            || getter.Body.Instructions.Count != 3 || getter.Body.Instructions[0].OpCode != OpCodes.Ldarg_0
            || getter.Body.Instructions[1].OpCode != OpCodes.Call
            || getter.Body.Instructions[1].Operand is not MethodReference target || target.Resolve() != runtimeGetter
            || getter.Body.Instructions[2].OpCode != OpCodes.Ret)
            throw new InvalidOperationException("Runtime type assembly declarations changed");
        if (runtimeGetter.Attributes != (MethodAttributes.Assembly | MethodAttributes.HideBySig)
            || !runtimeGetter.HasThis || runtimeGetter.ExplicitThis || runtimeGetter.HasGenericParameters
            || runtimeGetter.CallingConvention != MethodCallingConvention.Default || runtimeGetter.IsPInvokeImpl
            || runtimeGetter.ReturnType.Resolve() != runtimeModule || runtimeGetter.Parameters.Count != 0
            || !runtimeGetter.HasBody || runtimeGetter.RVA == 0 || runtimeGetter.ImplAttributes != MethodImplAttributes.IL
            || runtimeGetter.Body.HasExceptionHandlers || runtimeGetter.Body.HasVariables
            || runtimeGetter.Body.Instructions.Count != 3 || runtimeGetter.Body.Instructions[0].OpCode != OpCodes.Ldarg_0
            || runtimeGetter.Body.Instructions[1].OpCode != OpCodes.Call
            || runtimeGetter.Body.Instructions[1].Operand is not MethodReference internalTarget || internalTarget.Resolve() != helper
            || runtimeGetter.Body.Instructions[2].OpCode != OpCodes.Ret)
            throw new InvalidOperationException("Runtime module internal accessor changed");
        var body = helper.Body.Instructions;
        if (helper.Body.HasExceptionHandlers || helper.Body.HasVariables || body.Count != 8
            || body[0].OpCode != OpCodes.Ldarg_0 || body[1].OpCode != OpCodes.Call
            || body[1].Operand is not MethodReference cached || cached.Resolve() is not { Name: "GetModuleIfExists", IsInternalCall: true } cachedDefinition
            || cachedDefinition.DeclaringType != helper.DeclaringType || cachedDefinition.ReturnType.Resolve() != runtimeModule
            || body[2].OpCode != OpCodes.Dup || body[3].OpCode != OpCodes.Brtrue_S || body[3].Operand != body[7]
            || body[4].OpCode != OpCodes.Pop || body[5].OpCode != OpCodes.Ldarg_0 || body[6].OpCode != OpCodes.Call
            || body[6].Operand is not MethodReference worker || worker.Resolve().DeclaringType != helper.DeclaringType
            || worker.ReturnType.Resolve() != runtimeModule || body[7].OpCode != OpCodes.Ret)
            throw new InvalidOperationException("Managed assembly adapter IL changed");
    }

    private static void EmitDelegate(ILProcessor il, ModuleDefinition module, TypeReference type)
    {
        var constructor = new MethodReference(".ctor", module.TypeSystem.Void, type) { HasThis = true };
        constructor.Parameters.Add(new ParameterDefinition(module.TypeSystem.Object));
        constructor.Parameters.Add(new ParameterDefinition(module.TypeSystem.IntPtr));
        il.Emit(OpCodes.Newobj, constructor);
    }
}
