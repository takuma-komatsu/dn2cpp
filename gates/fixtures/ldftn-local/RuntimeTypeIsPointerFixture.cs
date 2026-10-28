using System.Globalization;
using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class RuntimeTypeIsPointerFixture
{
    public static bool Run(string[] args)
    {
        if (args.Length != 3 || (!args[0].StartsWith("--runtime-type-is-pointer-negative-", StringComparison.Ordinal) && args[0] is not ("--runtime-type-is-pointer" or "--runtime-type-is-pointer-reflection" or "--runtime-type-is-pointer-public" or "--runtime-type-is-pointer-oracle" or "--runtime-type-is-pointer-oracle-reflection"))) return false;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        using var core = AssemblyDefinition.ReadAssembly(args[2]);
        if (core.Name.Name != "System.Private.CoreLib") throw new InvalidOperationException("Expected input CoreLib");
        var runtimeType = core.MainModule.GetType("System.RuntimeType");
        var owner = core.MainModule.GetType("System.RuntimeTypeHandle");
        var helper = owner.Methods.Single(m => m.Name == "IsPointer");
        var getter = runtimeType.Methods.Single(m => m.Name == "IsPointerImpl");
        Validate(helper, getter, runtimeType);
        string path = Path.GetFullPath(args[1]);
        using var app = AssemblyDefinition.ReadAssembly(path, new ReaderParameters { InMemory = true });
        var module = app.MainModule;
        var checks = module.GetType("RuntimeTypeIsPointerSubset.IsPointerChecks");
        bool publicOnly = args[0] == "--runtime-type-is-pointer-public";
        bool reflectionOnly = args[0].EndsWith("-reflection", StringComparison.Ordinal);
        bool oracle = args[0].Contains("-oracle", StringComparison.Ordinal);
        string[] routes = publicOnly || oracle ? Array.Empty<string>() : reflectionOnly
            ? new[] { "Reflection", "ReflectionMethod" }
            : new[] { "Direct", "Pointer", "Getter", "GetterPointer", "VirtualGetter", "ConstrainedGetter", "Address", "GetterAddress", "FlowGetterAddress", "Reflection", "ReflectionMethod" };
        foreach (string route in routes)
        {
            var stub = checks.Methods.Single(m => m.Name == route);
            stub.Body = new MethodBody(stub);
            var il = stub.Body.GetILProcessor();
            var target = module.ImportReference(route is "Getter" or "GetterPointer" or "VirtualGetter" or "ConstrainedGetter" or "GetterAddress" or "FlowGetterAddress"
                ? getter : helper);
            if (target.MetadataToken.TokenType != TokenType.MemberRef)
                throw new InvalidOperationException("Expected a cross-assembly MemberRef target");
            if (route is "Reflection" or "ReflectionMethod")
            {
                il.Emit(OpCodes.Ldtoken, module.ImportReference(owner));
                il.Emit(OpCodes.Call, module.ImportReference(typeof(Type).GetMethod("GetTypeFromHandle")!));
                il.Emit(OpCodes.Ldstr, helper.Name);
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
                if (route == "Reflection")
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
                    il.Emit(OpCodes.Unbox_Any, module.TypeSystem.Boolean);
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
            else if (route == "FlowGetterAddress")
            {
                stub.Body.InitLocals = true;
                var receiver = new VariableDefinition(module.ImportReference(runtimeType));
                var address = new VariableDefinition(module.TypeSystem.IntPtr);
                stub.Body.Variables.Add(receiver);
                stub.Body.Variables.Add(address);
                var direct = Instruction.Create(OpCodes.Ldftn, target);
                var done = Instruction.Create(OpCodes.Ldloc, receiver);
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Castclass, module.ImportReference(runtimeType));
                il.Emit(OpCodes.Stloc, receiver);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Brfalse, direct);
                il.Emit(OpCodes.Ldloc, receiver);
                il.Emit(OpCodes.Ldvirtftn, target);
                il.Emit(OpCodes.Stloc, address);
                il.Emit(OpCodes.Br, done);
                il.Append(direct);
                il.Emit(OpCodes.Stloc, address);
                il.Append(done);
                il.Emit(OpCodes.Ldloc, address);
                EmitDelegate(il, module, stub.ReturnType);
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
                if (route is "Pointer" or "GetterPointer")
                {
                    if (route == "GetterPointer") il.Emit(OpCodes.Dup);
                    il.Emit(route == "GetterPointer" ? OpCodes.Ldvirtftn : OpCodes.Ldftn, target);
                    var site = new CallSite(target.ReturnType);
                    site.Parameters.Add(new ParameterDefinition(module.ImportReference(runtimeType)));
                    il.Emit(OpCodes.Calli, site);
                }
                else if (route == "GetterAddress")
                {
                    il.Emit(OpCodes.Dup);
                    il.Emit(OpCodes.Ldvirtftn, target);
                    EmitDelegate(il, module, stub.ReturnType);
                }
                else il.Emit(route == "VirtualGetter" ? OpCodes.Callvirt : OpCodes.Call, target);
            }
            il.Emit(OpCodes.Ret);
        }
        const string negativePrefix = "--runtime-type-is-pointer-negative-";
        if (args[0].StartsWith(negativePrefix, StringComparison.Ordinal))
        {
            string shape = args[0][negativePrefix.Length..];
            bool address = shape.StartsWith("address-", StringComparison.Ordinal);
            if (address) shape = shape["address-".Length..];
            bool instance = shape.StartsWith("override-", StringComparison.Ordinal);
            if (instance) shape = shape["override-".Length..];
            var stub = checks.Methods.Single(m => m.Name == (instance ? (address ? "GetterAddress" : "Getter") : (address ? "Address" : "Direct")));
            var call = stub.Body.Instructions.Single(i => i.OpCode == (address ? (instance ? OpCodes.Ldvirtftn : OpCodes.Ldftn) : OpCodes.Call));
            var definition = instance ? getter : helper;
            var bad = new MethodReference(definition.Name, module.ImportReference(definition.ReturnType), module.ImportReference(definition.DeclaringType)) { HasThis = instance };
            if (!instance) bad.Parameters.Add(new ParameterDefinition(module.ImportReference(runtimeType)));
            switch (shape)
            {
                case "return": bad.ReturnType = module.TypeSystem.Int32; break;
                case "parameter": bad.Parameters[0].ParameterType = module.TypeSystem.Object; break;
                case "hasthis": bad.HasThis = true; break;
                case "static": bad.HasThis = false; break;
                case "vararg": bad.CallingConvention = MethodCallingConvention.VarArg; break;
                case "return-class":
                    bad.ReturnType = new TypeReference("System", "Boolean", module,
                        module.ImportReference(runtimeType).Scope, false);
                    break;
                default: throw new InvalidOperationException("Unknown negative pointer reference");
            }
            call.Operand = bad;
        }
        module.EntryPoint = checks.Methods.Single(m => m.Name == (publicOnly ? "Run" : reflectionOnly ? "RunReflectionArtifact" : "RunArtifact"));
        app.Write(path, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        Console.WriteLine("runtime-type-is-pointer-sites=" + (publicOnly ? "public-only" : reflectionOnly ? "reflection-only" : "direct,calli,getter,callvirt,constrained,ldftn,ldvirtftn,flowing,reflection")
            + ",exact-CoreLib-declarations,managed-wrapper,retained-getter-IL," + (publicOnly || oracle ? "public-CLR-oracle" : "native-only"));
        return true;
    }

    private static void Validate(MethodDefinition helper, MethodDefinition getter, TypeDefinition runtimeType)
    {
        if (!runtimeType.IsSealed
            || helper.Attributes != (MethodAttributes.Assembly | MethodAttributes.Static | MethodAttributes.HideBySig)
            || helper.HasThis || helper.ExplicitThis || helper.HasGenericParameters
            || helper.CallingConvention != MethodCallingConvention.Default || helper.IsPInvokeImpl
            || helper.ReturnType.MetadataType != MetadataType.Boolean || helper.Parameters.Count != 1
            || helper.Parameters[0].ParameterType.Resolve() != runtimeType
            || !helper.HasBody || helper.RVA == 0 || helper.ImplAttributes != MethodImplAttributes.IL
            || getter.Attributes != (MethodAttributes.Family | MethodAttributes.Virtual | MethodAttributes.HideBySig)
            || !getter.HasThis || getter.ExplicitThis || getter.HasGenericParameters
            || getter.CallingConvention != MethodCallingConvention.Default || !getter.HasBody || getter.RVA == 0
            || getter.ImplAttributes != MethodImplAttributes.IL || getter.IsPInvokeImpl || getter.Parameters.Count != 0
            || getter.ReturnType.MetadataType != MetadataType.Boolean || getter.Body.HasExceptionHandlers || getter.Body.HasVariables
            || getter.Body.Instructions.Count != 3 || getter.Body.Instructions[0].OpCode != OpCodes.Ldarg_0
            || getter.Body.Instructions[1].OpCode != OpCodes.Call
            || getter.Body.Instructions[1].Operand is not MethodReference target || target.Resolve() != helper
            || getter.Body.Instructions[2].OpCode != OpCodes.Ret)
            throw new InvalidOperationException("Runtime pointer declarations changed");
    }

    private static void EmitDelegate(ILProcessor il, ModuleDefinition module, TypeReference type)
    {
        var constructor = new MethodReference(".ctor", module.TypeSystem.Void, type) { HasThis = true };
        constructor.Parameters.Add(new ParameterDefinition(module.TypeSystem.Object));
        constructor.Parameters.Add(new ParameterDefinition(module.TypeSystem.IntPtr));
        il.Emit(OpCodes.Newobj, constructor);
    }
}
