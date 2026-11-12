using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class DelegateCallableFixture
{
    public static bool Run(string[] args)
    {
        if (args.Length != 3 || args[0] is not ("--delegate-callable-bodies" or "--delegate-retained-only"))
            return false;
        using var core = AssemblyDefinition.ReadAssembly(args[2]);
        string path = Path.GetFullPath(args[1]);
        using var app = AssemblyDefinition.ReadAssembly(path, new ReaderParameters { InMemory = true });
        var module = app.MainModule;
        if (args[0] == "--delegate-retained-only")
        {
            // Only the descriptor may keep a delegate body in this image.
            var entry = app.EntryPoint;
            foreach (var type in module.Types.ToArray())
                if (type.Name != "<Module>" && type != entry.DeclaringType)
                    module.Types.Remove(type);
            foreach (var method in entry.DeclaringType.Methods.ToArray())
                if (method != entry) entry.DeclaringType.Methods.Remove(method);
            entry.DeclaringType.Fields.Clear();
            entry.Body = new MethodBody(entry);
            var il = entry.Body.GetILProcessor();
            var culture = core.MainModule.GetType("System.Globalization.CultureInfo");
            foreach (string setter in new[] { "set_CurrentCulture", "set_CurrentUICulture" })
            {
                il.Emit(OpCodes.Call, module.ImportReference(culture.Methods.Single(method => method.Name == "get_InvariantCulture")));
                il.Emit(OpCodes.Call, module.ImportReference(culture.Methods.Single(method => method.Name == setter)));
            }
            il.Emit(OpCodes.Ret);
            app.Write(path, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
            using var retainedCheck = AssemblyDefinition.ReadAssembly(path);
            var calls = retainedCheck.EntryPoint.Body.Instructions.Where(item => item.Operand is MethodReference).ToArray();
            if (retainedCheck.MainModule.Types.Count != 2 || retainedCheck.EntryPoint.DeclaringType.Methods.Count != 1
                || retainedCheck.EntryPoint.DeclaringType.Fields.Count != 0 || calls.Length != 4
                || calls.Any(item => item.OpCode != OpCodes.Call
                    || ((MethodReference)item.Operand).DeclaringType.FullName != "System.Globalization.CultureInfo"))
                throw new InvalidOperationException("retained delegate fixture has a non-culture execution root");
            Console.WriteLine("delegate retained-only roots=CurrentCulture,CurrentUICulture");
            return true;
        }
        var owner = module.GetType("DelegateCallableBodies.Routes");
        var records = new List<(string Stub, string Target, bool Address, bool Virtual)>();
        foreach (string prefix in new[] { "Call", "Calli", "Callvirt", "VirtCalli" })
        {
            bool address = prefix is "Calli" or "VirtCalli";
            bool virtualDispatch = prefix is "Callvirt" or "VirtCalli";
            foreach (var (name, type, member) in new[]
            {
                ("DelegateEquals", "System.Delegate", "Equals"),
                ("DelegateHash", "System.Delegate", "GetHashCode"),
                ("MulticastEquals", "System.MulticastDelegate", "Equals"),
                ("MulticastHash", "System.MulticastDelegate", "GetHashCode"),
                ("Factory", "System.MulticastDelegate", "NewMulticastDelegate"),
                ("FactoryFlag", "System.MulticastDelegate", "NewMulticastDelegate"),
                ("Combine", "System.MulticastDelegate", "CombineImpl"),
                ("Remove", "System.MulticastDelegate", "RemoveImpl"),
                ("Target", "System.MulticastDelegate", "GetTarget"),
                ("Method", "System.MulticastDelegate", "GetMethodImpl"),
                ("List", "System.MulticastDelegate", "GetInvocationList"),
                ("Single", "System.MulticastDelegate", "get_HasSingleTarget"),
                ("At", "System.MulticastDelegate", "TryGetAt"),
                ("DelegateCtorObject", "System.Delegate", ".ctor"),
                ("DelegateCtorType", "System.Delegate", ".ctor"),
                ("MulticastCtorObject", "System.MulticastDelegate", ".ctor"),
                ("MulticastCtorType", "System.MulticastDelegate", ".ctor"),
                ("CtorClosed", "System.MulticastDelegate", "CtorClosed"),
                ("CtorClosedStatic", "System.MulticastDelegate", "CtorClosedStatic"),
                ("CtorRTClosed", "System.MulticastDelegate", "CtorRTClosed"),
                ("CtorOpened", "System.MulticastDelegate", "CtorOpened"),
                ("CtorVirtualDispatch", "System.MulticastDelegate", "CtorVirtualDispatch"),
                ("CtorCollectibleClosedStatic", "System.MulticastDelegate", "CtorCollectibleClosedStatic"),
                ("CtorCollectibleOpened", "System.MulticastDelegate", "CtorCollectibleOpened"),
                ("CtorCollectibleVirtualDispatch", "System.MulticastDelegate", "CtorCollectibleVirtualDispatch"),
            })
            {
                var signature = owner.Methods.Single(method => method.Name == "Call" + name);
                var definition = core.MainModule.GetType(type).Methods.Single(method => method.Name == member
                    && !method.IsStatic && !method.HasGenericParameters
                    && method.Parameters.Count == signature.Parameters.Count - 1
                    && method.Parameters.Select(parameter => parameter.ParameterType.FullName)
                        .SequenceEqual(signature.Parameters.Skip(1).Select(parameter => parameter.ParameterType.FullName)));
                if (virtualDispatch && !definition.IsVirtual) continue;
                var stub = owner.Methods.Single(method => method.Name == prefix + name);
                if (stub.ReturnType.FullName != definition.ReturnType.FullName)
                    throw new InvalidOperationException("delegate fixture return mismatch: " + stub.Name);
                var target = module.ImportReference(definition);
                stub.Body = new MethodBody(stub);
                var il = stub.Body.GetILProcessor();
                foreach (var parameter in stub.Parameters) il.Emit(OpCodes.Ldarg, parameter);
                if (address)
                {
                    if (virtualDispatch) il.Emit(OpCodes.Ldarg_0);
                    il.Emit(virtualDispatch ? OpCodes.Ldvirtftn : OpCodes.Ldftn, target);
                    var site = new CallSite(stub.ReturnType);
                    foreach (var parameter in stub.Parameters)
                        site.Parameters.Add(new ParameterDefinition(parameter.ParameterType));
                    il.Emit(OpCodes.Calli, site);
                }
                else il.Emit(virtualDispatch ? OpCodes.Callvirt : OpCodes.Call, target);
                il.Emit(OpCodes.Ret);
                records.Add((stub.Name, definition.FullName, address, virtualDispatch));
            }
        }

        var baseEquals = core.MainModule.GetType("System.Delegate").Methods.Single(method => method.Name == "Equals");
        var order = owner.Methods.Single(method => method.Name == "CallvirtEqualsOrder");
        order.Body = new MethodBody(order);
        var orderIl = order.Body.GetILProcessor();
        orderIl.Emit(OpCodes.Ldarg_0);
        orderIl.Emit(OpCodes.Call, module.GetType("DelegateValueSubset.Program").Methods.Single(method => method.Name == "EffectArgument"));
        orderIl.Emit(OpCodes.Callvirt, module.ImportReference(baseEquals));
        orderIl.Emit(OpCodes.Ret);
        foreach (var (stubName, type) in new[] { ("VirtualBaseAddress", "System.Delegate"), ("VirtualMulticastAddress", "System.MulticastDelegate") })
        {
            var stub = owner.Methods.Single(method => method.Name == stubName);
            stub.Body = new MethodBody(stub);
            var il = stub.Body.GetILProcessor();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldvirtftn, module.ImportReference(core.MainModule.GetType(type).Methods.Single(method => method.Name == "Equals")));
            il.Emit(OpCodes.Ret);
        }
        foreach (string name in new[] { "VirtualBoundEquals", "VirtualBoundEqualsLocal" })
        {
            var stub = owner.Methods.Single(method => method.Name == name);
            bool local = name.EndsWith("Local", StringComparison.Ordinal);
            stub.Body = new MethodBody(stub) { InitLocals = local };
            if (local) stub.Body.Variables.Add(new VariableDefinition(module.TypeSystem.IntPtr));
            var il = stub.Body.GetILProcessor();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldvirtftn, module.ImportReference(baseEquals));
            if (local)
            {
                il.Emit(OpCodes.Stloc_0);
                il.Emit(OpCodes.Ldloc_0);
            }
            var constructor = new MethodReference(".ctor", module.TypeSystem.Void, stub.ReturnType) { HasThis = true };
            constructor.Parameters.Add(new ParameterDefinition(module.TypeSystem.Object));
            constructor.Parameters.Add(new ParameterDefinition(module.TypeSystem.IntPtr));
            il.Emit(OpCodes.Newobj, constructor);
            il.Emit(OpCodes.Ret);
        }

        // Access checks belong to the CLR oracle, not the CoreLib declarations
        // the transpiler must match for these hand-authored private calls.
        var bypass = new TypeDefinition("System.Runtime.CompilerServices", "IgnoresAccessChecksToAttribute",
            TypeAttributes.Public | TypeAttributes.Sealed, module.ImportReference(core.MainModule.GetType("System.Attribute")));
        module.Types.Add(bypass);
        var ctor = new MethodDefinition(".ctor", MethodAttributes.Public | MethodAttributes.HideBySig
            | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName, module.TypeSystem.Void);
        ctor.Parameters.Add(new ParameterDefinition(module.TypeSystem.String));
        bypass.Methods.Add(ctor);
        var ctorIl = ctor.Body.GetILProcessor();
        ctorIl.Emit(OpCodes.Ldarg_0);
        ctorIl.Emit(OpCodes.Call, module.ImportReference(core.MainModule.GetType("System.Attribute").Methods.Single(method => method.IsConstructor)));
        ctorIl.Emit(OpCodes.Ret);
        var attribute = new CustomAttribute(ctor);
        attribute.ConstructorArguments.Add(new CustomAttributeArgument(module.TypeSystem.String, "System.Private.CoreLib"));
        app.CustomAttributes.Add(attribute);
        app.Write(path, new WriterParameters { Timestamp = 0, DeterministicMvid = true });

        using var check = AssemblyDefinition.ReadAssembly(path);
        foreach (var (name, target, address, virtualDispatch) in records)
        {
            var body = check.MainModule.GetType(owner.FullName).Methods.Single(method => method.Name == name).Body;
            var instruction = body.Instructions.Single(item => item.Operand is MethodReference);
            var actual = (MethodReference)instruction.Operand;
            var expected = address ? virtualDispatch ? OpCodes.Ldvirtftn : OpCodes.Ldftn
                : virtualDispatch ? OpCodes.Callvirt : OpCodes.Call;
            if (instruction.OpCode != expected || actual.FullName != target
                || actual.DeclaringType.Scope.Name != "System.Private.CoreLib"
                || body.Instructions.Count(item => item.OpCode == OpCodes.Calli) != (address ? 1 : 0)
                || body.Instructions.Any(item => item.OpCode == OpCodes.Throw))
                throw new InvalidOperationException("delegate fixture lost its selected route: " + name);
            Console.WriteLine(name + "=" + expected.Name + (address ? "+calli:" : ":") + target);
        }
        var checkedOwner = check.MainModule.GetType(owner.FullName);
        var orderCalls = checkedOwner.Methods.Single(method => method.Name == "CallvirtEqualsOrder").Body.Instructions
            .Where(item => item.Operand is MethodReference).ToArray();
        if (orderCalls.Length != 2 || orderCalls[0].OpCode != OpCodes.Call || orderCalls[1].OpCode != OpCodes.Callvirt
            || ((MethodReference)orderCalls[0].Operand).Name != "EffectArgument"
            || ((MethodReference)orderCalls[1].Operand).FullName != baseEquals.FullName)
            throw new InvalidOperationException("delegate callvirt fixture lost argument evaluation before the call");
        foreach (string name in new[] { "VirtualBaseAddress", "VirtualMulticastAddress" })
        {
            var body = checkedOwner.Methods.Single(method => method.Name == name).Body.Instructions;
            if (body.Count != 3 || body[0].OpCode != OpCodes.Ldarg_0 || body[1].OpCode != OpCodes.Ldvirtftn || body[2].OpCode != OpCodes.Ret)
                throw new InvalidOperationException("delegate address fixture lost its isolated ldvirtftn: " + name);
        }
        Console.WriteLine("delegate virtual null and argument-order fixtures verified");
        foreach (string name in new[] { "VirtualBoundEquals", "VirtualBoundEqualsLocal" })
        {
            var body = checkedOwner.Methods.Single(method => method.Name == name).Body;
            var load = body.Instructions.Single(item => item.OpCode == OpCodes.Ldvirtftn);
            var constructor = (MethodReference)body.Instructions.Single(item => item.OpCode == OpCodes.Newobj).Operand;
            bool local = name.EndsWith("Local", StringComparison.Ordinal);
            if (((MethodReference)load.Operand).FullName != baseEquals.FullName
                || body.Variables.Count != (local ? 1 : 0)
                || local && (body.Variables[0].VariableType.MetadataType != MetadataType.IntPtr
                    || load.Next.OpCode != OpCodes.Stloc_0 || load.Next.Next.OpCode != OpCodes.Ldloc_0)
                || !local && load.Next.OpCode != OpCodes.Newobj
                || constructor.DeclaringType.FullName != "System.Func`2<System.Object,System.Boolean>"
                || constructor.Parameters.Count != 2 || constructor.Parameters[0].ParameterType.MetadataType != MetadataType.Object
                || constructor.Parameters[1].ParameterType.MetadataType != MetadataType.IntPtr)
                throw new InvalidOperationException("delegate fixture lost virtual binding provenance: " + name);
            Console.WriteLine(name + "=ldvirtftn+" + (local ? "local+" : "") + "delegate:" + baseEquals.FullName);
        }
        return true;
    }
}
