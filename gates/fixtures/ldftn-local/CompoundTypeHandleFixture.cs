using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class CompoundTypeHandleFixture
{
    public static bool Run(string[] args)
    {
        if (args.Length != 3 || args[0] is not ("--compound-handle" or "--compound-reflection")) return false;
        using var core = AssemblyDefinition.ReadAssembly(args[2]);
        if (core.Name.Name != "System.Private.CoreLib") throw new InvalidOperationException("Expected input CoreLib");
        var runtimeType = core.MainModule.GetType("System.RuntimeType");
        var owner = core.MainModule.GetType("System.RuntimeTypeHandle");
        string path = Path.GetFullPath(args[1]);
        using var app = AssemblyDefinition.ReadAssembly(path, new ReaderParameters { InMemory = true });
        var module = app.MainModule;
        var program = module.GetType("CompoundTypeHandleSubset.Program");
        bool reflectionOnly = args[0] == "--compound-reflection";
        foreach (string kind in new[] { "Pointer", "ByRef" })
        {
            var factory = runtimeType.Methods.Single(m => m.Name == "Make" + kind + "Type");
            var wrapper = owner.Methods.Single(m => m.Name == "Make" + kind && m.HasThis);
            bool Managed(MethodDefinition method, TypeDefinition result) => method.HasThis && !method.ExplicitThis && !method.HasGenericParameters
                && method.CallingConvention == MethodCallingConvention.Default && method.HasBody && method.IsIL
                && method.IsManaged && !method.IsInternalCall && !method.IsPInvokeImpl
                && method.Parameters.Count == 0 && method.ReturnType.Resolve() == result;
            if (!runtimeType.IsSealed || !Managed(factory, core.MainModule.GetType("System.Type"))
                || factory.Attributes != (MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig)
                || !Managed(wrapper, runtimeType) || wrapper.Attributes != (MethodAttributes.Assembly | MethodAttributes.HideBySig)
                || !factory.Body.Instructions.Any(i => i.OpCode == OpCodes.Call && i.Operand is MethodReference m && m.Resolve() == wrapper))
                throw new InvalidOperationException("Compound type managed declaration changed: " + kind);
            var raw = owner.Methods.Single(m => m.Name == wrapper.Name && m.IsStatic);
            if (raw.Attributes != (MethodAttributes.Private | MethodAttributes.Static | MethodAttributes.HideBySig | MethodAttributes.PInvokeImpl)
                || raw.ImplAttributes != MethodImplAttributes.PreserveSig || raw.HasBody || raw.HasThis
                || raw.ExplicitThis || raw.HasGenericParameters || !raw.IsPInvokeImpl
                || raw.CallingConvention != MethodCallingConvention.Default || raw.ReturnType.MetadataType != MetadataType.Void
                || raw.Parameters.Count != 2
                || raw.Parameters[0].ParameterType.Resolve() != core.MainModule.GetType("System.Runtime.CompilerServices.QCallTypeHandle")
                || raw.Parameters[1].ParameterType.Resolve() != core.MainModule.GetType("System.Runtime.CompilerServices.ObjectHandleOnStack")
                || raw.PInvokeInfo.Module.Name != "QCall" || raw.PInvokeInfo.EntryPoint != "RuntimeTypeHandle_Make" + kind
                || !wrapper.Body.Instructions.Any(i => i.OpCode == OpCodes.Call && i.Operand is MethodReference m && m.Resolve() == raw))
                throw new InvalidOperationException("Compound type QCall declaration changed: " + kind);
            if (!reflectionOnly)
            {
                foreach (string route in new[] { "Retained", "Virtual" })
                {
                    var stub = program.Methods.Single(m => m.Name == route + kind);
                    stub.Body = new MethodBody(stub);
                    var il = stub.Body.GetILProcessor();
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Castclass, module.ImportReference(runtimeType));
                    il.Emit(route == "Retained" ? OpCodes.Call : OpCodes.Callvirt, module.ImportReference(factory));
                    il.Emit(OpCodes.Ret);
                }
            }
            if (!reflectionOnly)
            {
                var publicTarget = module.ImportReference(factory);
                var runtimeReference = module.ImportReference(runtimeType);
                foreach (string route in new[] { "PublicAddress", "VirtualAddress", "FlowAddress" })
                {
                    var stub = program.Methods.Single(m => m.Name == route + kind);
                    stub.Body = new MethodBody(stub) { InitLocals = true };
                    var il = stub.Body.GetILProcessor();
                    var constructor = new MethodReference(".ctor", module.TypeSystem.Void, stub.ReturnType) { HasThis = true };
                    constructor.Parameters.Add(new ParameterDefinition(module.TypeSystem.Object));
                    constructor.Parameters.Add(new ParameterDefinition(module.TypeSystem.IntPtr));
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Castclass, runtimeReference);
                    if (route == "FlowAddress")
                    {
                        var receiver = new VariableDefinition(runtimeReference);
                        var address = new VariableDefinition(module.TypeSystem.IntPtr);
                        stub.Body.Variables.Add(receiver);
                        stub.Body.Variables.Add(address);
                        var direct = Instruction.Create(OpCodes.Ldftn, publicTarget);
                        var done = Instruction.Create(OpCodes.Ldloc, receiver);
                        il.Emit(OpCodes.Stloc, receiver);
                        il.Emit(OpCodes.Ldarg_1);
                        il.Emit(OpCodes.Brfalse, direct);
                        il.Emit(OpCodes.Ldloc, receiver);
                        il.Emit(OpCodes.Ldvirtftn, publicTarget);
                        il.Emit(OpCodes.Stloc, address);
                        il.Emit(OpCodes.Br, done);
                        il.Append(direct);
                        il.Emit(OpCodes.Stloc, address);
                        il.Append(done);
                        il.Emit(OpCodes.Ldloc, address);
                    }
                    else
                    {
                        if (route == "VirtualAddress") il.Emit(OpCodes.Dup);
                        il.Emit(route == "VirtualAddress" ? OpCodes.Ldvirtftn : OpCodes.Ldftn, publicTarget);
                    }
                    il.Emit(OpCodes.Newobj, constructor);
                    il.Emit(OpCodes.Ret);
                }
                var constrained = program.Methods.Single(m => m.Name == "Constrained" + kind);
                constrained.Body = new MethodBody(constrained) { InitLocals = true };
                var local = new VariableDefinition(runtimeReference);
                constrained.Body.Variables.Add(local);
                var cil = constrained.Body.GetILProcessor();
                cil.Emit(OpCodes.Ldarg_0);
                cil.Emit(OpCodes.Castclass, runtimeReference);
                cil.Emit(OpCodes.Stloc, local);
                cil.Emit(OpCodes.Ldloca, local);
                cil.Emit(OpCodes.Constrained, runtimeReference);
                cil.Emit(OpCodes.Callvirt, publicTarget);
                cil.Emit(OpCodes.Ret);
            }
            var target = module.ImportReference(wrapper);
            foreach (string route in new[] { "Direct", "Calli", "Address", "Reflection" })
            {
                if (reflectionOnly && route != "Reflection") continue;
                var stub = program.Methods.Single(m => m.Name == kind + route);
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
                    if (route == "Calli")
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
        }
        module.EntryPoint = program.Methods.Single(m => m.Name == (reflectionOnly ? "RunReflectionArtifact" : "RunHandleArtifact"));
        app.Write(path, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        Console.WriteLine("compound-handle-sites=" + args[0] + ",exact-managed-siblings,retained-public-IL,raw-QCalls-unchanged");
        return true;
    }
}
