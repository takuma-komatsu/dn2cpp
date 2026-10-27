using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class RuntimeTypeHandleConstructorFixture
{
    public static bool Run(string[] args)
    {
        if (args.Length != 3 || args[0] is not ("--runtime-type-handle-constructor" or "--runtime-type-handle-constructor-reflection")) return false;
        using var core = AssemblyDefinition.ReadAssembly(args[2]);
        if (core.Name.Name != "System.Private.CoreLib") throw new InvalidOperationException("Expected input CoreLib");
        var owner = core.MainModule.GetType("System.RuntimeTypeHandle");
        var method = owner.Methods.Single(m => m.IsConstructor && !m.IsStatic);
        if (!method.IsAssembly || !method.IsSpecialName || !method.IsRuntimeSpecialName
            || !method.HasThis || method.ExplicitThis || method.HasGenericParameters
            || method.CallingConvention != MethodCallingConvention.Default || !method.HasBody
            || !method.IsIL || !method.IsManaged || method.IsInternalCall || method.IsPInvokeImpl
            || method.ReturnType.FullName != "System.Void" || method.Parameters.Count != 1
            || method.Parameters[0].ParameterType.FullName != "System.RuntimeType"
            || method.Parameters[0].ParameterType.Resolve().Module != core.MainModule)
            throw new InvalidOperationException("Runtime type handle constructor declaration changed");
        string path = Path.GetFullPath(args[1]);
        using var app = AssemblyDefinition.ReadAssembly(path, new ReaderParameters { InMemory = true });
        var module = app.MainModule;
        var program = module.GetType("RuntimeTypeHandleConstructorSubset.Program");
        bool reflectionOnly = args[0] == "--runtime-type-handle-constructor-reflection";
        if (!reflectionOnly)
        {
            var target = module.ImportReference(method);
            foreach (string route in new[] { "Direct", "Pointer", "Newobj" })
            {
                var stub = program.Methods.Single(m => m.Name == (route == "Newobj" ? "CreateNewobj" : "Initialize" + route));
                stub.Body = new MethodBody(stub);
                var il = stub.Body.GetILProcessor();
                if (route != "Newobj") il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg, stub.Parameters[route == "Newobj" ? 0 : 1]);
                il.Emit(OpCodes.Castclass, target.Parameters[0].ParameterType);
                if (route == "Pointer")
                {
                    il.Emit(OpCodes.Ldftn, target);
                    var site = new CallSite(target.ReturnType);
                    site.Parameters.Add(new ParameterDefinition(new ByReferenceType(target.DeclaringType)));
                    site.Parameters.Add(new ParameterDefinition(target.Parameters[0].ParameterType));
                    il.Emit(OpCodes.Calli, site);
                }
                else il.Emit(route == "Newobj" ? OpCodes.Newobj : OpCodes.Call, target);
                il.Emit(OpCodes.Ret);
            }
            var direct = program.Methods.Single(m => m.Name == "CreateDirect");
            direct.Body = new MethodBody(direct) { InitLocals = true };
            var local = new VariableDefinition(target.DeclaringType);
            direct.Body.Variables.Add(local);
            var directIl = direct.Body.GetILProcessor();
            directIl.Emit(OpCodes.Ldloca, local);
            directIl.Emit(OpCodes.Ldarg_0);
            directIl.Emit(OpCodes.Castclass, target.Parameters[0].ParameterType);
            directIl.Emit(OpCodes.Call, target);
            directIl.Emit(OpCodes.Ldloc, local);
            directIl.Emit(OpCodes.Ret);
        }
        module.EntryPoint = program.Methods.Single(m => m.Name == (reflectionOnly ? "RunReflectionChecks" : "RunArtifactChecks"));
        app.Write(path, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        Console.WriteLine("handle-ctor-sites=" + (reflectionOnly ? "reflection-only" : "direct,newobj,address,reflection") + ",exact-CoreLib-signature");
        return true;
    }
}
