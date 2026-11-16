using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class ThreadingPrivateFixture
{
    public static bool Run(string[] args)
    {
        if (args.Length != 4 || args[0] != "--threading-private")
            return false;
        using var core = AssemblyDefinition.ReadAssembly(args[2]);
        string path = Path.GetFullPath(args[1]);
        using var app = AssemblyDefinition.ReadAssembly(path, new ReaderParameters { InMemory = true });
        var module = app.MainModule;
        var target = core.MainModule.GetType("System.Threading.Thread").Methods.Single(method => method.Name == "ThreadNameChanged");
        if (target.Attributes != (MethodAttributes.Private | MethodAttributes.HideBySig)
            || target.ImplAttributes != MethodImplAttributes.IL || !target.HasBody || target.HasGenericParameters
            || target.ReturnType.MetadataType != MetadataType.Void || target.Parameters.Count != 1
            || target.Parameters[0].ParameterType.MetadataType != MetadataType.String)
            throw new InvalidOperationException("ThreadNameChanged declaration changed");
        var owner = module.GetType("ThreadingPrivateSubset");
        foreach (string name in new[] { "Direct", "Address" })
        {
            var stub = owner.Methods.Single(method => method.Name == name);
            var reference = module.ImportReference(target);
            stub.Body = new MethodBody(stub);
            var il = stub.Body.GetILProcessor();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            if (name == "Address")
            {
                il.Emit(OpCodes.Ldftn, reference);
                var site = new CallSite(module.TypeSystem.Void);
                foreach (var parameter in stub.Parameters)
                    site.Parameters.Add(new ParameterDefinition(parameter.ParameterType));
                il.Emit(OpCodes.Calli, site);
            }
            else
            {
                switch (args[3])
                {
                    case "valid":
                    case "lock-return": break;
                    case "return": reference.ReturnType = module.TypeSystem.Int32; break;
                    case "parameter": reference.Parameters[0].ParameterType = module.TypeSystem.Object; break;
                    case "header": reference.ExplicitThis = true; break;
                    default: throw new ArgumentException("Unknown thread name fixture mode");
                }
                il.Emit(OpCodes.Call, reference);
            }
            il.Emit(OpCodes.Ret);
        }
        var lockType = core.MainModule.GetType("System.Threading.Lock");
        foreach (var pair in new[] { ("Lock", "get_ShouldStopPreemptingWaiters"),
            ("LockReset", "ResetWaiterStartTime"), ("LockRecord", "RecordWaiterStartTime") })
        {
            var waiter = lockType.Methods.Single(method => method.Name == pair.Item2);
            bool query = pair.Item1 == "Lock";
            if (waiter.Attributes != (MethodAttributes.Private | MethodAttributes.HideBySig | (query ? MethodAttributes.SpecialName : 0))
                || waiter.ImplAttributes != (pair.Item1 == "LockReset" ? MethodImplAttributes.IL : MethodImplAttributes.AggressiveInlining)
                || !waiter.HasBody || waiter.HasGenericParameters
                || waiter.ReturnType.MetadataType != (query ? MetadataType.Boolean : MetadataType.Void) || waiter.Parameters.Count != 0)
                throw new InvalidOperationException("Lock waiter declaration changed: " + waiter.Name);
            foreach (string suffix in new[] { "Direct", "Address" })
            {
                var stub = owner.Methods.Single(method => method.Name == pair.Item1 + suffix);
                var reference = module.ImportReference(waiter);
                stub.Body = new MethodBody(stub);
                var il = stub.Body.GetILProcessor();
                il.Emit(OpCodes.Ldarg_0);
                if (suffix == "Address")
                {
                    il.Emit(OpCodes.Ldftn, reference);
                    var site = new CallSite(stub.ReturnType);
                    site.Parameters.Add(new ParameterDefinition(stub.Parameters[0].ParameterType));
                    il.Emit(OpCodes.Calli, site);
                }
                else
                {
                    if (query && args[3] == "lock-return") reference.ReturnType = module.TypeSystem.Int32;
                    il.Emit(OpCodes.Call, reference);
                }
                il.Emit(OpCodes.Ret);
            }
        }
        if (!module.Types.Any(type => type.Name == "IgnoresAccessChecksToAttribute"))
        {
            var bypass = new TypeDefinition("System.Runtime.CompilerServices", "IgnoresAccessChecksToAttribute",
                TypeAttributes.Public | TypeAttributes.Sealed, module.ImportReference(core.MainModule.GetType("System.Attribute")));
            module.Types.Add(bypass);
            var ctor = new MethodDefinition(".ctor", MethodAttributes.Public | MethodAttributes.HideBySig
                | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName, module.TypeSystem.Void);
            ctor.Parameters.Add(new ParameterDefinition(module.TypeSystem.String));
            bypass.Methods.Add(ctor);
            var il = ctor.Body.GetILProcessor();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Call, module.ImportReference(core.MainModule.GetType("System.Attribute").Methods.Single(method => method.IsConstructor)));
            il.Emit(OpCodes.Ret);
            var attribute = new CustomAttribute(ctor);
            attribute.ConstructorArguments.Add(new CustomAttributeArgument(module.TypeSystem.String, "System.Private.CoreLib"));
            app.CustomAttributes.Add(attribute);
        }
        app.Write(path, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        using var check = AssemblyDefinition.ReadAssembly(path);
        var bodies = check.MainModule.GetType("ThreadingPrivateSubset").Methods;
        var direct = bodies.Single(method => method.Name == "Direct").Body.Instructions.Single(instruction => instruction.OpCode == OpCodes.Call);
        var address = bodies.Single(method => method.Name == "Address").Body.Instructions.Single(instruction => instruction.OpCode == OpCodes.Ldftn);
        if (((MethodReference)address.Operand).FullName != target.FullName
            || !bodies.Single(method => method.Name == "Address").Body.Instructions.Any(instruction => instruction.OpCode == OpCodes.Calli))
            throw new InvalidOperationException("ThreadNameChanged address fixture changed");
        Console.WriteLine("thread-name fixture=" + args[3] + " direct=" + direct.Operand + " address=ldftn+calli:" + address.Operand);
        foreach (var pair in new[] { ("Lock", "get_ShouldStopPreemptingWaiters"),
            ("LockReset", "ResetWaiterStartTime"), ("LockRecord", "RecordWaiterStartTime") })
        {
            var body = bodies.Single(method => method.Name == pair.Item1 + "Address").Body;
            var addressLoad = body.Instructions.Single(instruction => instruction.OpCode == OpCodes.Ldftn);
            if (((MethodReference)addressLoad.Operand).FullName != lockType.Methods.Single(method => method.Name == pair.Item2).FullName
                || !body.Instructions.Any(instruction => instruction.OpCode == OpCodes.Calli))
                throw new InvalidOperationException("Lock waiter address fixture changed");
            Console.WriteLine("lock-waiter fixture=" + args[3] + " address=ldftn+calli:" + addressLoad.Operand);
        }
        return true;
    }
}
