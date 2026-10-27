using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class RuntimeTypeInstantiationFixture
{
    public static bool Run(string[] args)
    {
        if (args.Length != 3 || args[0] is not ("--type-instantiation" or "--type-instantiation-reflection")) return false;
        using var core = AssemblyDefinition.ReadAssembly(args[2]);
        if (core.Name.Name != "System.Private.CoreLib") throw new InvalidOperationException("Expected input CoreLib");
        var owner = core.MainModule.GetType("System.RuntimeTypeHandle");
        var runtimeType = core.MainModule.GetType("System.RuntimeType");
        string path = Path.GetFullPath(args[1]);
        using var app = AssemblyDefinition.ReadAssembly(path, new ReaderParameters { InMemory = true });
        var module = app.MainModule;
        var program = module.GetType("RuntimeTypeInstantiationSubset.Program");
        foreach (string kind in new[] { "Internal", "Public" })
        {
            var method = owner.Methods.Single(m => m.Name == "GetInstantiation" + kind);
            string resultElement = kind == "Internal" ? "System.RuntimeType" : "System.Type";
            if (method.Attributes != (MethodAttributes.Assembly | MethodAttributes.HideBySig)
                || !method.HasThis || method.ExplicitThis || method.HasGenericParameters
                || method.CallingConvention != MethodCallingConvention.Default || !method.HasBody
                || !method.IsIL || !method.IsManaged || method.IsInternalCall || method.IsPInvokeImpl
                || method.Parameters.Count != 0 || method.ReturnType is not ArrayType { IsVector: true } array
                || array.ElementType.FullName != resultElement || array.ElementType.Resolve().Module != core.MainModule)
                throw new InvalidOperationException("Runtime type instantiation declaration changed: " + kind);
            foreach (string route in new[] { "Direct", "Pointer", "Address", "Reflection" })
            {
                var stub = program.Methods.Single(m => m.Name == kind + route);
                stub.Body = new MethodBody(stub);
                var il = stub.Body.GetILProcessor();
                var target = module.ImportReference(method);
                if (route == "Reflection")
                {
                    var getType = typeof(Type).GetMethod("GetTypeFromHandle")!;
                    var lookup = typeof(Type).GetMethod("GetMethod", new[] { typeof(string), typeof(System.Reflection.BindingFlags), typeof(Type[]) })!;
                    var invoke = typeof(System.Reflection.MethodBase).GetMethod("Invoke", new[] { typeof(object), typeof(object[]) })!;
                    il.Emit(OpCodes.Ldtoken, module.ImportReference(owner));
                    il.Emit(OpCodes.Call, module.ImportReference(getType));
                    il.Emit(OpCodes.Ldstr, method.Name);
                    il.Emit(OpCodes.Ldc_I4, (int)(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic));
                    il.Emit(OpCodes.Ldc_I4_0);
                    il.Emit(OpCodes.Newarr, module.ImportReference(typeof(Type)));
                    il.Emit(OpCodes.Callvirt, module.ImportReference(lookup));
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Box, module.ImportReference(owner));
                    il.Emit(OpCodes.Ldnull);
                    il.Emit(OpCodes.Callvirt, module.ImportReference(invoke));
                    il.Emit(OpCodes.Castclass, stub.ReturnType);
                }
                else if (route == "Address")
                {
                    var constructor = new MethodReference(".ctor", module.TypeSystem.Void, stub.ReturnType) { HasThis = true };
                    constructor.Parameters.Add(new ParameterDefinition(module.TypeSystem.Object));
                    constructor.Parameters.Add(new ParameterDefinition(module.TypeSystem.IntPtr));
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Box, module.ImportReference(owner));
                    il.Emit(OpCodes.Ldftn, target);
                    il.Emit(OpCodes.Newobj, constructor);
                }
                else
                {
                    il.Emit(OpCodes.Ldarga_S, stub.Parameters[0]);
                    if (route == "Pointer")
                    {
                        il.Emit(OpCodes.Ldftn, target);
                        var site = new CallSite(target.ReturnType);
                        site.Parameters.Add(new ParameterDefinition(new ByReferenceType(target.DeclaringType)));
                        il.Emit(OpCodes.Calli, site);
                    }
                    else il.Emit(OpCodes.Call, target);
                }
                il.Emit(OpCodes.Ret);
            }
            var getter = runtimeType.Methods.Single(m => m.Name == (kind == "Internal" ? "GetGenericArgumentsInternal" : "GetGenericArguments"));
            var getterStub = program.Methods.Single(m => m.Name == kind + "Getter");
            getterStub.Body = new MethodBody(getterStub);
            var getterIl = getterStub.Body.GetILProcessor();
            getterIl.Emit(OpCodes.Ldarg_0);
            getterIl.Emit(OpCodes.Castclass, module.ImportReference(runtimeType));
            getterIl.Emit(OpCodes.Call, module.ImportReference(getter));
            getterIl.Emit(OpCodes.Ret);
        }
        var generic = program.Methods.Single(m => m.Name == "Generic");
        var genericCall = generic.Body.Instructions.Single(i => i.OpCode == OpCodes.Call && i.Operand is MethodReference m && m.Name == "PublicHandle");
        var local = new VariableDefinition(module.ImportReference(owner));
        generic.Body.Variables.Add(local);
        generic.Body.InitLocals = true;
        var gil = generic.Body.GetILProcessor();
        gil.InsertBefore(genericCall, gil.Create(OpCodes.Stloc, local));
        gil.InsertBefore(genericCall, gil.Create(OpCodes.Ldloca, local));
        genericCall.Operand = module.ImportReference(owner.Methods.Single(m => m.Name == "GetInstantiationPublic"));
        var isArray = program.Methods.Single(m => m.Name == "IsRuntimeArray");
        isArray.Body = new MethodBody(isArray);
        var ail = isArray.Body.GetILProcessor();
        ail.Emit(OpCodes.Ldarg_0);
        ail.Emit(OpCodes.Isinst, new ArrayType(module.ImportReference(runtimeType)));
        ail.Emit(OpCodes.Ldnull);
        ail.Emit(OpCodes.Cgt_Un);
        ail.Emit(OpCodes.Ret);
        module.EntryPoint = program.Methods.Single(m => m.Name == (args[0].EndsWith("-reflection", StringComparison.Ordinal) ? "RunReflectionArtifact" : "RunArtifact"));
        app.Write(path, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        Console.WriteLine("type-instantiation-sites=two-managed-siblings,direct,pointer,address,reflection,getter,generic,exact-array,native-only");
        return true;
    }
}
