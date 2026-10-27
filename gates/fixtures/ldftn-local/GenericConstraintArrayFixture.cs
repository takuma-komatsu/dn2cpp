using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class GenericConstraintArrayFixture
{
    public static bool Run(string[] args)
    {
        if (args.Length != 3 || args[0] is not ("--constraint-array-handle" or "--constraint-array-reflection")) return false;
        using var core = AssemblyDefinition.ReadAssembly(args[2]);
        if (core.Name.Name != "System.Private.CoreLib") throw new InvalidOperationException("Expected input CoreLib");
        var runtimeType = core.MainModule.GetType("System.RuntimeType");
        var owner = core.MainModule.GetType("System.RuntimeTypeHandle");
        var getter = runtimeType.Methods.Single(m => m.Name == "GetGenericParameterConstraints");
        var wrapper = owner.Methods.Single(m => m.Name == "GetConstraints" && m.HasThis);
        bool Managed(MethodDefinition method) => method.HasThis && !method.ExplicitThis && !method.HasGenericParameters
            && method.CallingConvention == MethodCallingConvention.Default && method.HasBody && method.IsIL
            && method.IsManaged && !method.IsInternalCall && !method.IsPInvokeImpl
            && method.Parameters.Count == 0 && method.ReturnType is ArrayType array && array.IsVector
            && array.ElementType.Resolve() == core.MainModule.GetType("System.Type");
        if (!runtimeType.IsSealed || !Managed(getter)
            || getter.Attributes != (MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig)
            || !Managed(wrapper) || wrapper.Attributes != (MethodAttributes.Assembly | MethodAttributes.HideBySig))
            throw new InvalidOperationException("GetConstraints managed declaration changed");
        var raw = owner.Methods.Single(m => m.Name == "GetConstraints" && m.IsStatic);
        if (raw.Attributes != (MethodAttributes.Private | MethodAttributes.Static | MethodAttributes.HideBySig | MethodAttributes.PInvokeImpl)
            || raw.ImplAttributes != MethodImplAttributes.PreserveSig || raw.HasBody || raw.HasThis
            || raw.ExplicitThis || raw.HasGenericParameters || !raw.IsPInvokeImpl
            || raw.CallingConvention != MethodCallingConvention.Default || raw.ReturnType.MetadataType != MetadataType.Void
            || raw.Parameters.Count != 2
            || raw.Parameters[0].ParameterType.Resolve() != core.MainModule.GetType("System.Runtime.CompilerServices.QCallTypeHandle")
            || raw.Parameters[1].ParameterType.Resolve() != core.MainModule.GetType("System.Runtime.CompilerServices.ObjectHandleOnStack")
            || raw.PInvokeInfo.Module.Name != "QCall" || raw.PInvokeInfo.EntryPoint != "RuntimeTypeHandle_GetConstraints"
            || !wrapper.Body.Instructions.Any(i => i.OpCode == OpCodes.Call && i.Operand is MethodReference m && m.Resolve() == raw))
            throw new InvalidOperationException("GetConstraints QCall declaration changed");
        string path = Path.GetFullPath(args[1]);
        using var app = AssemblyDefinition.ReadAssembly(path, new ReaderParameters { InMemory = true });
        var module = app.MainModule;
        var program = module.GetType("GenericConstraintArraySubset.Program");
        bool reflectionOnly = args[0] == "--constraint-array-reflection";
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
                il.Emit(OpCodes.Castclass, module.ImportReference(wrapper.ReturnType));
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
        Console.WriteLine("constraint-array-sites=" + args[0] + ",exact-managed-wrapper,retained-getter-IL,raw-QCall-unchanged");
        return true;
    }
}
