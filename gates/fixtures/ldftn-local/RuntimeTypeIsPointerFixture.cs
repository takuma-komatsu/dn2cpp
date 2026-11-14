using System.Globalization;
using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class RuntimeTypeIsPointerFixture
{
    public static bool Run(string[] args)
    {
        if (args.Length != 3 || args[0] is not ("--runtime-type-is-pointer" or "--runtime-type-is-pointer-oracle"
            or "--runtime-type-is-pointer-aggregate"
            or "--runtime-type-is-pointer-negative-address-return" or "--runtime-type-is-pointer-negative-call-class-return")) return false;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        using var core = AssemblyDefinition.ReadAssembly(args[2]);
        if (core.Name.Name != "System.Private.CoreLib") throw new InvalidOperationException("Expected input CoreLib");
        var runtimeType = core.MainModule.GetType("System.RuntimeType");
        var owner = core.MainModule.GetType("System.RuntimeTypeHandle");
        var helper = owner.Methods.Single(m => m.Name == "IsPointer");
        var getter = runtimeType.Methods.Single(m => m.Name == "IsPointerImpl");
        string path = Path.GetFullPath(args[1]);
        using var app = AssemblyDefinition.ReadAssembly(path, new ReaderParameters { InMemory = true });
        var module = app.MainModule;
        bool aggregate = args[0] == "--runtime-type-is-pointer-aggregate";
        if (aggregate && (module.EntryPoint is null || module.EntryPoint.DeclaringType.FullName != "Program"
            || module.EntryPoint.Name != "Main"))
            throw new InvalidOperationException("Expected the aggregate Program.Main entry point");
        var checks = module.GetType("RuntimeTypeIsPointerSubset.IsPointerChecks");
        // The oracle keeps the managed wrappers, which reach the same body through public APIs on the CLR.
        string[] routes = args[0] == "--runtime-type-is-pointer-oracle" ? Array.Empty<string>()
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
        if (args[0] is "--runtime-type-is-pointer-negative-address-return" or "--runtime-type-is-pointer-negative-call-class-return")
        {
            // An ldftn of the pointer query whose MemberRef returns Int32 instead of Boolean, or a
            // call whose MemberRef encodes Boolean as a class. ILDiet rewrites the class encoding.
            bool address = args[0] == "--runtime-type-is-pointer-negative-address-return";
            var site = checks.Methods.Single(m => m.Name == (address ? "Address" : "Direct")).Body.Instructions
                .Single(i => i.OpCode == (address ? OpCodes.Ldftn : OpCodes.Call));
            var bad = new MethodReference(helper.Name, address ? module.TypeSystem.Int32
                : new TypeReference("System", "Boolean", module, module.TypeSystem.CoreLibrary, false), module.ImportReference(owner));
            bad.Parameters.Add(new ParameterDefinition(module.ImportReference(runtimeType)));
            site.Operand = bad;
        }
        if (!aggregate)
            module.EntryPoint = checks.Methods.Single(m => m.Name == "RunArtifact");
        app.Write(path, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        if (aggregate)
            Console.WriteLine("runtime-type-is-pointer-entry:Program.Main");
        return true;
    }

    private static void EmitDelegate(ILProcessor il, ModuleDefinition module, TypeReference type)
    {
        var constructor = new MethodReference(".ctor", module.TypeSystem.Void, type) { HasThis = true };
        constructor.Parameters.Add(new ParameterDefinition(module.TypeSystem.Object));
        constructor.Parameters.Add(new ParameterDefinition(module.TypeSystem.IntPtr));
        il.Emit(OpCodes.Newobj, constructor);
    }
}
