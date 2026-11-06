using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class ArrayTypeHandleFixture
{
    public static bool Run(string[] args)
    {
        if (args.Length != 3 || args[0] is not ("--array-type-handle" or "--array-type-reflection")) return false;
        using var core = AssemblyDefinition.ReadAssembly(args[2]);
        if (core.Name.Name != "System.Private.CoreLib") throw new InvalidOperationException("Expected input CoreLib");
        var runtimeType = core.MainModule.GetType("System.RuntimeType");
        var owner = core.MainModule.GetType("System.RuntimeTypeHandle");
        string path = Path.GetFullPath(args[1]);
        using var app = AssemblyDefinition.ReadAssembly(path, new ReaderParameters { InMemory = true });
        var module = app.MainModule;
        var program = module.GetType("ArrayTypeHandleSubset.Program");
        bool reflectionOnly = args[0] == "--array-type-reflection";
        foreach (bool sz in new[] { true, false })
        {
            string kind = sz ? "Sz" : "Rank";
            int arity = sz ? 0 : 1;
            var factory = runtimeType.Methods.Single(m => m.Name == "MakeArrayType" && m.Parameters.Count == arity);
            var wrapper = owner.Methods.Single(m => m.Name == (sz ? "MakeSZArray" : "MakeArray") && m.HasThis);
            bool Managed(MethodDefinition method, TypeDefinition result) => method.HasThis && !method.ExplicitThis && !method.HasGenericParameters
                && method.CallingConvention == MethodCallingConvention.Default && method.HasBody && method.IsIL
                && method.IsManaged && !method.IsInternalCall && !method.IsPInvokeImpl
                && method.Parameters.Count == arity && (sz || method.Parameters[0].ParameterType.MetadataType == MetadataType.Int32)
                && method.ReturnType.Resolve() == result;
            if (!runtimeType.IsSealed || !Managed(factory, core.MainModule.GetType("System.Type"))
                || factory.Attributes != (MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig)
                || !Managed(wrapper, runtimeType) || wrapper.Attributes != (MethodAttributes.Assembly | MethodAttributes.HideBySig)
                || !factory.Body.Instructions.Any(i => i.OpCode == OpCodes.Call && i.Operand is MethodReference m && m.Resolve() == wrapper))
                throw new InvalidOperationException("Array type managed declaration changed: " + kind);
            if (!sz)
            {
                var instructions = factory.Body.Instructions;
                if (instructions[0].OpCode != OpCodes.Ldarg_1 || instructions[1].OpCode != OpCodes.Ldc_I4_0
                    || instructions[2].OpCode.Code is not (Code.Bgt or Code.Bgt_S) || instructions[2].Operand != instructions[5]
                    || instructions[3].OpCode != OpCodes.Newobj || instructions[3].Operand is not MethodReference exception
                    || exception.DeclaringType.FullName != "System.IndexOutOfRangeException" || instructions[4].OpCode != OpCodes.Throw)
                    throw new InvalidOperationException("Public array rank validation changed");
            }
            var raw = owner.Methods.Single(m => m.Name == wrapper.Name && m.IsStatic);
            if (raw.Attributes != (MethodAttributes.Private | MethodAttributes.Static | MethodAttributes.HideBySig | MethodAttributes.PInvokeImpl)
                || raw.ImplAttributes != MethodImplAttributes.PreserveSig || raw.HasBody || raw.HasThis
                || raw.ExplicitThis || raw.HasGenericParameters || !raw.IsPInvokeImpl
                || raw.CallingConvention != MethodCallingConvention.Default || raw.ReturnType.MetadataType != MetadataType.Void
                || raw.Parameters.Count != arity + 2
                || raw.Parameters[0].ParameterType.Resolve() != core.MainModule.GetType("System.Runtime.CompilerServices.QCallTypeHandle")
                || !sz && raw.Parameters[1].ParameterType.MetadataType != MetadataType.Int32
                || raw.Parameters[arity + 1].ParameterType.Resolve() != core.MainModule.GetType("System.Runtime.CompilerServices.ObjectHandleOnStack")
                || raw.PInvokeInfo.Module.Name != "QCall" || raw.PInvokeInfo.EntryPoint != "RuntimeTypeHandle_" + wrapper.Name
                || !wrapper.Body.Instructions.Any(i => i.OpCode == OpCodes.Call && i.Operand is MethodReference m && m.Resolve() == raw)
                || !wrapper.Body.Instructions.Any(i => i.OpCode == OpCodes.Call && i.Operand is MethodReference m
                    && m.DeclaringType.FullName == "System.RuntimeTypeHandle" && m.Name == "GetNativeHandle"))
                throw new InvalidOperationException("Array type QCall declaration changed: " + kind);
            if (!reflectionOnly)
            {
                foreach (string route in new[] { "Retained", "Virtual" })
                {
                    foreach (string suffix in sz ? new[] { "" } : new[] { "", "Unsigned" })
                    {
                        var stub = program.Methods.Single(m => m.Name == route + kind + suffix);
                        stub.Body = new MethodBody(stub);
                        var il = stub.Body.GetILProcessor();
                        il.Emit(OpCodes.Ldarg_0);
                        il.Emit(OpCodes.Castclass, module.ImportReference(runtimeType));
                        if (!sz) il.Emit(OpCodes.Ldarg_1);
                        il.Emit(route == "Retained" ? OpCodes.Call : OpCodes.Callvirt, module.ImportReference(factory));
                        il.Emit(OpCodes.Ret);
                    }
                }
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
                if (!sz) cil.Emit(OpCodes.Ldarg_1);
                cil.Emit(OpCodes.Constrained, runtimeReference);
                cil.Emit(OpCodes.Callvirt, publicTarget);
                cil.Emit(OpCodes.Ret);
            }
            var target = module.ImportReference(wrapper);
            foreach (string route in new[] { "Direct", "Calli", "Address", "Reflection", "Unsigned" })
            {
                if (reflectionOnly && route != "Reflection" || sz && route == "Unsigned") continue;
                var stub = program.Methods.Single(m => m.Name == kind + route);
                stub.Body = new MethodBody(stub);
                var il = stub.Body.GetILProcessor();
                if (route == "Reflection")
                {
                    il.Emit(OpCodes.Ldtoken, module.ImportReference(owner));
                    il.Emit(OpCodes.Call, module.ImportReference(typeof(Type).GetMethod("GetTypeFromHandle")!));
                    il.Emit(OpCodes.Ldstr, wrapper.Name);
                    il.Emit(OpCodes.Ldc_I4, (int)(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic));
                    il.Emit(OpCodes.Ldc_I4, arity);
                    il.Emit(OpCodes.Newarr, module.ImportReference(typeof(Type)));
                    if (!sz)
                    {
                        il.Emit(OpCodes.Dup);
                        il.Emit(OpCodes.Ldc_I4_0);
                        il.Emit(OpCodes.Ldtoken, module.TypeSystem.Int32);
                        il.Emit(OpCodes.Call, module.ImportReference(typeof(Type).GetMethod("GetTypeFromHandle")!));
                        il.Emit(OpCodes.Stelem_Ref);
                    }
                    il.Emit(OpCodes.Callvirt, module.ImportReference(typeof(Type).GetMethod("GetMethod", new[] { typeof(string), typeof(System.Reflection.BindingFlags), typeof(Type[]) })!));
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Box, module.ImportReference(owner));
                    if (sz) il.Emit(OpCodes.Ldnull);
                    else
                    {
                        il.Emit(OpCodes.Ldc_I4_1);
                        il.Emit(OpCodes.Newarr, module.TypeSystem.Object);
                        il.Emit(OpCodes.Dup);
                        il.Emit(OpCodes.Ldc_I4_0);
                        il.Emit(OpCodes.Ldarg_1);
                        il.Emit(OpCodes.Box, module.TypeSystem.Int32);
                        il.Emit(OpCodes.Stelem_Ref);
                    }
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
                    if (!sz) il.Emit(OpCodes.Ldarg_1);
                    if (route == "Calli")
                    {
                        il.Emit(OpCodes.Ldftn, target);
                        var site = new CallSite(target.ReturnType);
                        site.Parameters.Add(new ParameterDefinition(new ByReferenceType(target.DeclaringType)));
                        if (!sz) site.Parameters.Add(new ParameterDefinition(module.TypeSystem.Int32));
                        il.Emit(OpCodes.Calli, site);
                    }
                    else il.Emit(OpCodes.Call, target);
                }
                il.Emit(OpCodes.Ret);
            }
        }
        module.EntryPoint = program.Methods.Single(m => m.Name == (reflectionOnly ? "RunReflectionArtifact" : "RunHandleArtifact"));
        app.Write(path, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        Console.WriteLine("array-type-sites=" + args[0] + ",exact-managed-siblings,retained-public-rank-guard,unsigned-argument,raw-QCalls-unchanged");
        return true;
    }
}
