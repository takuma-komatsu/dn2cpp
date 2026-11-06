using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class RuntimeTypeAssemblyFixture
{
    public static bool Run(string[] args)
    {
        if (args.Length != 3 || args[0] is not ("--runtime-type-assembly" or "--runtime-type-assembly-reflection" or "--runtime-type-assembly-public")) return false;
        using var core = AssemblyDefinition.ReadAssembly(args[2]);
        if (core.Name.Name != "System.Private.CoreLib") throw new InvalidOperationException("Expected input CoreLib");
        var runtimeType = core.MainModule.GetType("System.RuntimeType");
        var runtimeAssembly = core.MainModule.GetType("System.Reflection.RuntimeAssembly");
        var assembly = core.MainModule.GetType("System.Reflection.Assembly");
        var owner = core.MainModule.GetType("System.RuntimeTypeHandle");
        var helper = owner.Methods.Single(m => m.Name == "GetAssembly");
        var getter = runtimeType.Methods.Single(m => m.Name == "get_Assembly");
        Validate(helper, getter, runtimeType, runtimeAssembly, assembly);
        string path = Path.GetFullPath(args[1]);
        using var app = AssemblyDefinition.ReadAssembly(path, new ReaderParameters { InMemory = true });
        var module = app.MainModule;
        var checks = module.GetType("RuntimeTypeAssemblySubset.RuntimeAssemblyChecks");
        bool publicOnly = args[0] == "--runtime-type-assembly-public";
        bool reflectionOnly = args[0] == "--runtime-type-assembly-reflection";
        string[] routes = publicOnly ? Array.Empty<string>() : reflectionOnly
            ? new[] { "Reflection", "ReflectionMethod", "ObjectReflection" }
            : new[] { "Direct", "Pointer", "Getter", "GetterPointer", "VirtualGetter", "ConstrainedGetter", "Address", "GetterAddress", "GetterAddressLocal",
                "ObjectDirect", "ObjectPointer", "ObjectLocal", "Reflection", "ReflectionMethod", "ObjectReflection" };
        foreach (string route in routes)
        {
            var stub = checks.Methods.Single(m => m.Name == route);
            stub.Body = new MethodBody(stub);
            var il = stub.Body.GetILProcessor();
            var target = module.ImportReference(route is "Getter" or "GetterPointer" or "VirtualGetter" or "ConstrainedGetter"
                or "GetterAddress" or "GetterAddressLocal"
                ? getter : helper);
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
                        il.Emit(OpCodes.Castclass, module.ImportReference(assembly));
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
                if (route is "Pointer" or "ObjectPointer" or "GetterPointer")
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
                        var local = new VariableDefinition(module.ImportReference(runtimeAssembly));
                        stub.Body.Variables.Add(local);
                        il.Emit(OpCodes.Stloc, local);
                        il.Emit(OpCodes.Ldloc, local);
                    }
                }
            }
            il.Emit(OpCodes.Ret);
        }
        module.EntryPoint = checks.Methods.Single(m => m.Name == (publicOnly ? "Run" : reflectionOnly ? "RunReflectionArtifact" : "RunArtifact"));
        app.Write(path, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        Console.WriteLine("runtime-type-assembly-sites=" + (publicOnly ? "public-only" : reflectionOnly ? "reflection-only" : "direct,calli,getter,callvirt,constrained,ldftn,ldvirtftn,object,reflection")
            + ",exact-CoreLib-declarations,RuntimeAssembly-return,retained-getter-IL,native-only");
        return true;
    }

    private static void Validate(MethodDefinition helper, MethodDefinition getter, TypeDefinition runtimeType,
        TypeDefinition runtimeAssembly, TypeDefinition assembly)
    {
        if (!runtimeType.IsSealed || runtimeAssembly.BaseType.Resolve() != assembly
            || helper.Attributes != (MethodAttributes.Assembly | MethodAttributes.Static | MethodAttributes.HideBySig)
            || helper.HasThis || helper.ExplicitThis || helper.HasGenericParameters
            || helper.CallingConvention != MethodCallingConvention.Default || helper.IsPInvokeImpl
            || helper.ReturnType.Resolve() != runtimeAssembly || helper.Parameters.Count != 1
            || helper.Parameters[0].ParameterType.Resolve() != runtimeType
            || !helper.HasBody || helper.RVA == 0 || helper.ImplAttributes != MethodImplAttributes.IL
            || getter.Attributes != (MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig | MethodAttributes.SpecialName)
            || !getter.HasThis || getter.ExplicitThis || getter.HasGenericParameters
            || getter.CallingConvention != MethodCallingConvention.Default || !getter.HasBody || getter.RVA == 0
            || getter.ImplAttributes != MethodImplAttributes.IL || getter.IsPInvokeImpl || getter.Parameters.Count != 0
            || getter.ReturnType.Resolve() != assembly || getter.Body.HasExceptionHandlers || getter.Body.HasVariables
            || getter.Body.Instructions.Count != 3 || getter.Body.Instructions[0].OpCode != OpCodes.Ldarg_0
            || getter.Body.Instructions[1].OpCode != OpCodes.Call
            || getter.Body.Instructions[1].Operand is not MethodReference target || target.Resolve() != helper
            || getter.Body.Instructions[2].OpCode != OpCodes.Ret)
            throw new InvalidOperationException("Runtime type assembly declarations changed");
        var body = helper.Body.Instructions;
        if (helper.Body.HasExceptionHandlers || helper.Body.HasVariables || body.Count != 8
            || body[0].OpCode != OpCodes.Ldarg_0 || body[1].OpCode != OpCodes.Call
            || body[1].Operand is not MethodReference cached || cached.Resolve() is not { Name: "GetAssemblyIfExists", IsInternalCall: true } cachedDefinition
            || cachedDefinition.DeclaringType != helper.DeclaringType || cachedDefinition.ReturnType.Resolve() != runtimeAssembly
            || body[2].OpCode != OpCodes.Dup || body[3].OpCode != OpCodes.Brtrue_S || body[3].Operand != body[7]
            || body[4].OpCode != OpCodes.Pop || body[5].OpCode != OpCodes.Ldarg_0 || body[6].OpCode != OpCodes.Call
            || body[6].Operand is not MethodReference worker || worker.Resolve().DeclaringType != helper.DeclaringType
            || worker.ReturnType.Resolve() != runtimeAssembly || body[7].OpCode != OpCodes.Ret)
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
