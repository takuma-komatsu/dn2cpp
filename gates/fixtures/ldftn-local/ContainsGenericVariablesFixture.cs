using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class ContainsGenericVariablesFixture
{
    public static bool Run(string[] args)
    {
        if (args.Length != 3 || args[0] is not ("--contains-generic-handle" or "--contains-generic-reflection")) return false;
        using var core = AssemblyDefinition.ReadAssembly(args[2]);
        if (core.Name.Name != "System.Private.CoreLib") throw new InvalidOperationException("Expected input CoreLib");
        var runtimeType = core.MainModule.GetType("System.RuntimeType");
        var owner = core.MainModule.GetType("System.RuntimeTypeHandle");
        var getter = runtimeType.Methods.Single(m => m.Name == "get_ContainsGenericParameters");
        var wrapper = owner.Methods.Single(m => m.Name == "ContainsGenericVariables" && m.HasThis);
        bool Managed(MethodDefinition method) => method.HasThis && !method.ExplicitThis && !method.HasGenericParameters
            && method.CallingConvention == MethodCallingConvention.Default && method.HasBody && method.IsIL
            && method.IsManaged && !method.IsInternalCall && !method.IsPInvokeImpl
            && method.Parameters.Count == 0 && method.ReturnType.MetadataType == MetadataType.Boolean;
        if (!runtimeType.IsSealed || !Managed(getter)
            || getter.Attributes != (MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig | MethodAttributes.SpecialName)
            || !Managed(wrapper) || wrapper.Attributes != (MethodAttributes.Assembly | MethodAttributes.HideBySig))
            throw new InvalidOperationException("ContainsGenericVariables managed declaration changed");
        var raw = owner.Methods.Single(m => m.Name == "ContainsGenericVariables" && m.IsStatic);
        if (raw.Attributes != (MethodAttributes.Assembly | MethodAttributes.Static | MethodAttributes.HideBySig)
            || raw.ImplAttributes != MethodImplAttributes.InternalCall || raw.HasBody || raw.HasThis
            || raw.ExplicitThis || raw.HasGenericParameters || raw.IsPInvokeImpl
            || raw.CallingConvention != MethodCallingConvention.Default || raw.ReturnType.MetadataType != MetadataType.Boolean
            || raw.Parameters.Count != 1 || raw.Parameters[0].ParameterType.Resolve() != runtimeType
            || !wrapper.Body.Instructions.Any(i => i.OpCode == OpCodes.Call && i.Operand is MethodReference m && m.Resolve() == raw))
            throw new InvalidOperationException("ContainsGenericVariables static InternalCall declaration changed");
        string path = Path.GetFullPath(args[1]);
        using var app = AssemblyDefinition.ReadAssembly(path, new ReaderParameters { InMemory = true });
        var module = app.MainModule;
        var program = module.GetType("ContainsGenericVariablesSubset.Program");
        bool reflectionOnly = args[0] == "--contains-generic-reflection";
        if (!reflectionOnly)
        {
            var stub = program.Methods.Single(m => m.Name == "RetainedGetter");
            stub.Body = new MethodBody(stub);
            var il = stub.Body.GetILProcessor();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Castclass, module.ImportReference(runtimeType));
            il.Emit(OpCodes.Call, module.ImportReference(getter));
            il.Emit(OpCodes.Ret);
        }
        var target = module.ImportReference(wrapper);
        foreach (string route in new[] { "Direct", "Pointer", "Address", "Reflection" })
        {
            if (reflectionOnly && route != "Reflection") continue;
            var stub = program.Methods.Single(m => m.Name == "Handle" + route);
            stub.Body = new MethodBody(stub);
            var il = stub.Body.GetILProcessor();
            if (route == "Reflection")
            {
                il.Emit(OpCodes.Ldtoken, module.ImportReference(owner));
                il.Emit(OpCodes.Call, module.ImportReference(typeof(Type).GetMethod("GetTypeFromHandle")!));
                il.Emit(OpCodes.Ldstr, wrapper.Name);
                il.Emit(OpCodes.Ldc_I4, (int)(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic));
                il.Emit(OpCodes.Ldc_I4_0);
                il.Emit(OpCodes.Newarr, module.ImportReference(typeof(Type)));
                il.Emit(OpCodes.Callvirt, module.ImportReference(typeof(Type).GetMethod("GetMethod", new[] { typeof(string), typeof(System.Reflection.BindingFlags), typeof(Type[]) })!));
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Box, module.ImportReference(owner));
                il.Emit(OpCodes.Ldnull);
                il.Emit(OpCodes.Callvirt, module.ImportReference(typeof(System.Reflection.MethodBase).GetMethod("Invoke", new[] { typeof(object), typeof(object[]) })!));
                il.Emit(OpCodes.Unbox_Any, module.TypeSystem.Boolean);
            }
            else if (route == "Address")
            {
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Box, module.ImportReference(owner));
                il.Emit(OpCodes.Ldftn, target);
                var constructor = new MethodReference(".ctor", module.TypeSystem.Void, stub.ReturnType) { HasThis = true };
                constructor.Parameters.Add(new ParameterDefinition(module.TypeSystem.Object));
                constructor.Parameters.Add(new ParameterDefinition(module.TypeSystem.IntPtr));
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
        module.EntryPoint = program.Methods.Single(m => m.Name == (reflectionOnly ? "RunReflectionArtifact" : "RunHandleArtifact"));
        app.Write(path, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        Console.WriteLine("contains-generic-sites=" + args[0] + ",exact-managed-wrapper,retained-getter-IL,raw-InternalCall-unchanged");
        return true;
    }
}
