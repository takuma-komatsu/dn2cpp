using System.Globalization;
using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class DateTimeTrioFixture
{
    public static bool Run(string[] args)
    {
        bool negative = args.Length == 4 && args[0] == "--datetime-trio-negative";
        if (!negative && (args.Length != 3 || !new[] { "--datetime-trio", "--datetime-trio-reflection", "--datetime-trio-minimal", "--datetime-trio-oracle", "--datetime-trio-lookup" }.Contains(args[0]))) return false;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        using var core = AssemblyDefinition.ReadAssembly(args[2]);
        var owner = core.MainModule.GetType("System.DateTime");
        var addDefinition = owner.Methods.Single(method => method.Name == "TryAddTicks");
        var createDefinition = owner.Methods.Single(method => method.Name == "TryCreate");
        var ctorDefinition = owner.Methods.Single(method => method.IsConstructor && method.Parameters.Count == 3
            && method.Parameters[0].ParameterType.MetadataType == MetadataType.Int64
            && method.Parameters[1].ParameterType.FullName == "System.DateTimeKind" && method.Parameters[2].ParameterType.MetadataType == MetadataType.Boolean);
        if ((int)addDefinition.Attributes != 0x83 || (int)createDefinition.Attributes != 0x93 || (int)ctorDefinition.Attributes != 0x1883
            || new[] { addDefinition, createDefinition, ctorDefinition }.Any(method => method.ImplAttributes != 0 || method.RVA == 0 || method.HasGenericParameters)
            || addDefinition.MetadataToken.ToInt32() != 0x06000fb6 || createDefinition.MetadataToken.ToInt32() != 0x0600102b || ctorDefinition.MetadataToken.ToInt32() != 0x06000f94)
            throw new InvalidOperationException("installed DateTime trio changed");
        string path = Path.GetFullPath(args[1]);
        using var app = AssemblyDefinition.ReadAssembly(path, new ReaderParameters { InMemory = true });
        var module = app.MainModule;
        var program = module.GetType("DateTimeTrioSubset.Program");
        MethodDefinition Method(string name) => program.Methods.Single(method => method.Name == name);
        ILProcessor Body(string name)
        {
            var method = Method(name);
            method.Body = new MethodBody(method);
            return method.Body.GetILProcessor();
        }
        var add = module.ImportReference(addDefinition);
        var create = module.ImportReference(createDefinition);
        var ctor = module.ImportReference(ctorDefinition);
        if (args[0] != "--datetime-trio-oracle")
        {
            foreach (var pair in new[] { (Prefix: "Add", Target: add), (Prefix: "Create", Target: create), (Prefix: "Ctor", Target: ctor) })
            {
                foreach (string route in new[] { "Direct", "Pointer" })
                {
                    string name = pair.Prefix + route;
                    var il = Body(name);
                    foreach (var parameter in Method(name).Parameters) il.Emit(OpCodes.Ldarg, parameter);
                    if (route == "Direct") il.Emit(OpCodes.Call, pair.Target);
                    else
                    {
                        il.Emit(OpCodes.Ldftn, pair.Target);
                        var site = new CallSite(pair.Target.ReturnType);
                        foreach (var parameter in Method(name).Parameters) site.Parameters.Add(new ParameterDefinition(parameter.ParameterType));
                        il.Emit(OpCodes.Calli, site);
                    }
                    il.Emit(OpCodes.Ret);
                }
                RuntimeTypeDefaultCtorFixture.MarkNoAlloc(module, Method(pair.Prefix + "Direct"));
            }
            foreach (var row in new[] { (Name: "AddAddress", Delegate: "ClosedAdd", Target: add), (Name: "CreateAddress", Delegate: "CreateQuery", Target: create), (Name: "CtorAddress", Delegate: "ClosedCtor", Target: ctor) })
            {
                var il = Body(row.Name);
                if (row.Name == "CreateAddress") il.Emit(OpCodes.Ldnull);
                else
                {
                    il.Emit(OpCodes.Ldarg_0);
                    if (row.Name == "AddAddress") il.Emit(OpCodes.Box, module.ImportReference(owner));
                }
                il.Emit(OpCodes.Ldftn, row.Target);
                il.Emit(OpCodes.Newobj, program.NestedTypes.Single(type => type.Name == row.Delegate).Methods.Single(method => method.IsConstructor));
                il.Emit(OpCodes.Ret);
            }
            var newobj = Body("CtorNew");
            foreach (var parameter in Method("CtorNew").Parameters) newobj.Emit(OpCodes.Ldarg, parameter);
            newobj.Emit(OpCodes.Newobj, ctor); newobj.Emit(OpCodes.Ret);
            // Keep the actual producer sequence and replace only its final wrapper call.
            foreach (var row in new[] { (Name: "AddProducer", Target: add, Wrapper: "AddDirect"), (Name: "CreateProducer", Target: create, Wrapper: "CreateDirect"), (Name: "CtorProducer", Target: ctor, Wrapper: "CtorDirect") })
                Method(row.Name).Body.Instructions.Single(instruction => instruction.OpCode == OpCodes.Call && instruction.Operand is MethodReference method && method.Name == row.Wrapper).Operand = row.Target;
            if (negative)
            {
                string mode = args[3];
                bool pointer = mode.StartsWith("address-", StringComparison.Ordinal);
                if (pointer) mode = mode.Substring(8);
                bool allocation = mode.StartsWith("newobj-", StringComparison.Ordinal);
                if (allocation) mode = mode.Substring(7);
                int dash = mode.IndexOf('-');
                string family = mode.Substring(0, dash), mutation = mode.Substring(dash + 1);
                var target = family switch { "add" => add, "create" => create, "ctor" => ctor, _ => throw new InvalidOperationException("unknown negative family") };
                if (allocation && family != "ctor") throw new InvalidOperationException("newobj requires ctor");
                var bad = new MethodReference(target.Name, target.ReturnType, target.DeclaringType) { HasThis = target.HasThis };
                foreach (var parameter in target.Parameters) bad.Parameters.Add(new ParameterDefinition(parameter.ParameterType));
                switch (mutation)
                {
                    case "return": bad.ReturnType = module.TypeSystem.Int32; break;
                    case "static": bad.HasThis = !bad.HasThis; break;
                    case "generic": bad.GenericParameters.Add(new GenericParameter("T", bad)); break;
                    case "arity" when family != "ctor": bad.Parameters.RemoveAt(bad.Parameters.Count - 1); break;
                    case "out-value" when family != "ctor": bad.Parameters[^1].ParameterType = module.ImportReference(owner); break;
                    case "out-other" when family != "ctor": bad.Parameters[^1].ParameterType = new ByReferenceType(module.TypeSystem.Int64); break;
                    case "modifier": bad.Parameters[0].ParameterType = new OptionalModifierType(module.TypeSystem.Object, bad.Parameters[0].ParameterType); break;
                    case "first-int" when family is "add" or "ctor": bad.Parameters[0].ParameterType = module.TypeSystem.Int32; break;
                    case "first-long" when family == "create": bad.Parameters[0].ParameterType = module.TypeSystem.Int64; break;
                    case "kind-int" when family == "ctor": bad.Parameters[1].ParameterType = module.TypeSystem.Int32; break;
                    case "bool-int" when family == "ctor": bad.Parameters[2].ParameterType = module.TypeSystem.Int32; break;
                    default: throw new InvalidOperationException("unknown DateTime trio negative");
                }
                string prefix = family == "add" ? "Add" : family == "create" ? "Create" : "Ctor";
                string methodName = allocation ? "CtorNew" : prefix + (pointer ? "Pointer" : "Direct");
                var opcode = allocation ? OpCodes.Newobj : pointer ? OpCodes.Ldftn : OpCodes.Call;
                Method(methodName).Body.Instructions.Single(instruction => instruction.OpCode == opcode).Operand = bad;
            }
        }
        module.EntryPoint = Method(args[0] switch
        {
            "--datetime-trio-oracle" => "RunPublicOracle",
            "--datetime-trio-reflection" => "RunReflectionChecks",
            "--datetime-trio-lookup" => "RunLookupChecks",
            "--datetime-trio-minimal" => "RunMinimal",
            _ => "Run"
        });
        app.Write(path, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        return true;
    }
}
