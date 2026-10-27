using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class RuntimeTypeMakeGenericFixture
{
    public static bool Run(string[] args)
    {
        if (args.Length != 3 || args[0] is not ("--make-generic-public" or "--make-generic-private" or "--make-generic-reflection")) return false;
        using var core = AssemblyDefinition.ReadAssembly(args[2]);
        if (core.Name.Name != "System.Private.CoreLib") throw new InvalidOperationException("Expected input CoreLib");
        var runtimeType = core.MainModule.GetType("System.RuntimeType");
        var owner = core.MainModule.GetType("System.RuntimeTypeHandle");
        var make = runtimeType.Methods.Single(m => m.Name == "MakeGenericType");
        var scalar = owner.Methods.Single(m => m.Name == "Instantiate" && m.Parameters.Count == 1 && m.Parameters[0].ParameterType.FullName == "System.RuntimeType");
        var array = owner.Methods.Single(m => m.Name == "Instantiate" && m.Parameters.Count == 1 && m.Parameters[0].ParameterType.FullName == "System.Type[]");
        bool CoreType(TypeReference type, string name) => type.FullName == name && type.Resolve().Module == core.MainModule;
        bool Managed(MethodDefinition method) => method.HasThis && !method.ExplicitThis && !method.HasGenericParameters
            && method.CallingConvention == MethodCallingConvention.Default && method.HasBody && method.IsIL
            && method.IsManaged && !method.IsInternalCall && !method.IsPInvokeImpl;
        if (!runtimeType.IsSealed || !Managed(make)
            || make.Attributes != (MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig)
            || !CoreType(make.ReturnType, "System.Type") || make.Parameters.Count != 1
            || make.Parameters[0].ParameterType is not ArrayType { IsVector: true } ma || !CoreType(ma.ElementType, "System.Type"))
            throw new InvalidOperationException("RuntimeType.MakeGenericType declaration changed");
        foreach (var method in new[] { scalar, array })
            if (!Managed(method) || method.Attributes != (MethodAttributes.Assembly | MethodAttributes.HideBySig)
                || !CoreType(method.ReturnType, "System.RuntimeType") || method.Parameters.Count != 1
                || (method == scalar ? !CoreType(method.Parameters[0].ParameterType, "System.RuntimeType")
                    : method.Parameters[0].ParameterType is not ArrayType { IsVector: true } a || !CoreType(a.ElementType, "System.Type")))
                throw new InvalidOperationException("RuntimeTypeHandle.Instantiate declaration changed");
        var raw = owner.Methods.Single(m => m.Name == "Instantiate" && m.IsPInvokeImpl);
        if (raw.Attributes != (MethodAttributes.Private | MethodAttributes.Static | MethodAttributes.HideBySig | MethodAttributes.PInvokeImpl)
            || raw.HasBody || !raw.IsPreserveSig || raw.ReturnType.MetadataType != MetadataType.Void
            || string.Join(",", raw.Parameters.Select(p => p.ParameterType.FullName)) != "System.Runtime.CompilerServices.QCallTypeHandle,System.IntPtr*,System.Int32,System.Runtime.CompilerServices.ObjectHandleOnStack"
            || raw.PInvokeInfo.Module.Name != "QCall" || raw.PInvokeInfo.EntryPoint != "RuntimeTypeHandle_Instantiate"
            || !new[] { scalar, array }.All(m => m.Body.Instructions.Any(i => i.OpCode == OpCodes.Call && i.Operand is MethodReference mr && mr.Resolve() == raw)))
            throw new InvalidOperationException("RuntimeTypeHandle.Instantiate QCall binding changed");
        string path = Path.GetFullPath(args[1]);
        using var app = AssemblyDefinition.ReadAssembly(path, new ReaderParameters { InMemory = true });
        var module = app.MainModule;
        var program = module.GetType("RuntimeTypeMakeGenericSubset.Program");
        bool publicMode = args[0] == "--make-generic-public";
        if (publicMode)
        {
            var target = module.ImportReference(make);
            foreach (string name in new[] { "Planned", "Forwarded" })
            {
                var planned = program.Methods.Single(m => m.Name == name);
                var definition = planned.Body.Instructions.First(i => i.OpCode == OpCodes.Call
                    && i.Operand is MethodReference m && m.Name == "GetTypeFromHandle");
                planned.Body.GetILProcessor().InsertAfter(definition, Instruction.Create(OpCodes.Castclass, module.ImportReference(runtimeType)));
                var construction = planned.Body.Instructions.Single(i => i.OpCode == OpCodes.Callvirt
                    && i.Operand is MethodReference m && m.Name == "MakeGenericType");
                construction.OpCode = OpCodes.Call;
                if (name == "Planned") construction.Operand = target;
                else
                {
                    // Forwarded Type identity follows assembly-name casing, not type-name aliases.
                    var original = (MethodReference)construction.Operand;
                    if (original.ReturnType.Scope is not AssemblyNameReference { Name: "System.Runtime" } scope)
                        throw new InvalidOperationException("Expected System.Runtime-forwarded Type signature");
                    var lowerScope = new AssemblyNameReference("system.runtime", scope.Version)
                        { PublicKeyToken = scope.PublicKeyToken, Culture = scope.Culture };
                    module.AssemblyReferences.Add(lowerScope);
                    var forwardedType = new TypeReference("System", "Type", module, lowerScope);
                    var forwarded = new MethodReference(original.Name, forwardedType, target.DeclaringType) { HasThis = true };
                    forwarded.Parameters.Add(new ParameterDefinition(new ArrayType(forwardedType)));
                    construction.Operand = forwarded;
                }
            }
            foreach (string route in new[] { "Direct", "Virtual", "Pointer", "Address", "VirtualAddress", "FlowingAddress" })
            {
                var stub = program.Methods.Single(m => m.Name == route);
                stub.Body = new MethodBody(stub);
                var il = stub.Body.GetILProcessor();
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Castclass, module.ImportReference(runtimeType));
                if (route.EndsWith("Address", StringComparison.Ordinal))
                {
                    if (route != "Address") il.Emit(OpCodes.Dup);
                    il.Emit(route == "Address" ? OpCodes.Ldftn : OpCodes.Ldvirtftn, target);
                    if (route == "FlowingAddress")
                    {
                        var pointer = new VariableDefinition(module.TypeSystem.IntPtr);
                        stub.Body.Variables.Add(pointer);
                        il.Emit(OpCodes.Stloc, pointer);
                        il.Emit(OpCodes.Ldloc, pointer);
                    }
                    var constructor = new MethodReference(".ctor", module.TypeSystem.Void, stub.ReturnType) { HasThis = true };
                    constructor.Parameters.Add(new ParameterDefinition(module.TypeSystem.Object));
                    constructor.Parameters.Add(new ParameterDefinition(module.TypeSystem.IntPtr));
                    il.Emit(OpCodes.Newobj, constructor);
                }
                else
                {
                    il.Emit(OpCodes.Ldarg_1);
                    if (route == "Pointer")
                    {
                        il.Emit(OpCodes.Ldftn, target);
                        var site = new CallSite(target.ReturnType);
                        site.Parameters.Add(new ParameterDefinition(target.DeclaringType));
                        site.Parameters.Add(new ParameterDefinition(target.Parameters[0].ParameterType));
                        il.Emit(OpCodes.Calli, site);
                    }
                    else il.Emit(route == "Virtual" ? OpCodes.Callvirt : OpCodes.Call, target);
                }
                il.Emit(OpCodes.Ret);
            }
        }
        else if (args[0] == "--make-generic-private")
        {
            // The delegate's actual parameter is RuntimeType, not a wider Type argument.
            var delegateType = program.NestedTypes.Single(t => t.Name == "MakeScalarInvoker");
            foreach (var method in delegateType.Methods.Where(m => m.Name is "Invoke" or "BeginInvoke"))
                method.Parameters[0].ParameterType = module.ImportReference(runtimeType);
            var adapter = program.Methods.Single(m => m.Name == "ScalarDelegate");
            var invocation = adapter.Body.Instructions.Single(i => i.OpCode == OpCodes.Callvirt && i.Operand is MethodReference m && m.Name == "Invoke");
            adapter.Body.GetILProcessor().InsertBefore(invocation, Instruction.Create(OpCodes.Castclass, module.ImportReference(runtimeType)));
            foreach (var pair in new[] { (Name: "Scalar", Method: scalar), (Name: "Array", Method: array) })
            {
                var target = module.ImportReference(pair.Method);
                foreach (string route in new[] { "Direct", "Pointer", "Address" })
                {
                    var stub = program.Methods.Single(m => m.Name == pair.Name + route);
                    stub.Body = new MethodBody(stub);
                    var il = stub.Body.GetILProcessor();
                    if (route == "Address")
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
                        il.Emit(OpCodes.Ldarg_1);
                        if (pair.Name == "Scalar") il.Emit(OpCodes.Castclass, module.ImportReference(runtimeType));
                        if (route == "Pointer")
                        {
                            il.Emit(OpCodes.Ldftn, target);
                            var site = new CallSite(target.ReturnType);
                            site.Parameters.Add(new ParameterDefinition(new ByReferenceType(target.DeclaringType)));
                            site.Parameters.Add(new ParameterDefinition(target.Parameters[0].ParameterType));
                            il.Emit(OpCodes.Calli, site);
                        }
                        else il.Emit(OpCodes.Call, target);
                    }
                    il.Emit(OpCodes.Ret);
                }
            }
        }
        module.EntryPoint = program.Methods.Single(m => m.Name == (publicMode ? "RunPublicArtifact"
            : args[0] == "--make-generic-private" ? "RunPrivateArtifact" : "RunReflectionArtifact"));
        app.Write(path, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        Console.WriteLine("make-generic-sites=" + args[0] + ",exact-CoreLib-signatures,QCall-binding-unchanged");
        return true;
    }
}
