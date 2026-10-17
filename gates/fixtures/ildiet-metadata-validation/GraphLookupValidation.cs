using Mono.Cecil;
using Mono.Cecil.Cil;

static partial class GraphLookupValidation
{
    internal static readonly string[] Sections = { "invoke-member-event", "event-query", "lookup-flags-mixed", "lookup-flags-known", "lookup-arity-mixed", "lookup-arity-known" };

    internal static MethodReference[] Create(ModuleDefinition module, ModuleDefinition core)
    {
        TypeReference Core(string name) => module.ImportReference(core.GetType("System." + name));
        var objectType = Core("Object"); var voidType = Core("Void"); var stringType = Core("String");
        MethodReference Api(string owner, string name, params string[] parameters) => module.ImportReference(core.GetType("System." + owner).Methods.Single(m => m.IsPublic && m.Name == name
            && m.Parameters.Select(p => p.ParameterType.FullName).SequenceEqual(parameters)));
        TypeDefinition Type(string name, TypeReference? parent = null, TypeAttributes flags = TypeAttributes.Public | TypeAttributes.Class)
        {
            var type = new TypeDefinition("Graph", name, flags, parent ?? objectType); module.Types.Add(type); return type;
        }
        MethodDefinition Method(TypeDefinition owner, string name, TypeReference? result = null, MethodAttributes flags = MethodAttributes.Public | MethodAttributes.Static)
        {
            var method = new MethodDefinition(name, flags, result ?? voidType); owner.Methods.Add(method); method.Body.Instructions.Add(Instruction.Create(OpCodes.Ret)); return method;
        }
        void TypeValue(MethodDefinition method, TypeReference owner)
        {
            method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldtoken, owner));
            method.Body.Instructions.Add(Instruction.Create(OpCodes.Call, Api("Type", "GetTypeFromHandle", "System.RuntimeTypeHandle")));
        }
        MethodDefinition Dependency(string prefix) => Method(Type(prefix + "Dependency"), "Touch");
        var driver = Type("LookupAlternativeDriver"); var calls = new List<MethodReference>();
        foreach (bool execute in new[] { true, false })
        {
            string prefix = execute ? "InvokeEvent" : "QueryEvent";
            var payload = Type(prefix + "Payload", Core("ValueType"), TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.SequentialLayout);
            var text = Method(payload, "ToString", stringType, MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig);
            text.Body.Instructions.Clear(); text.Body.Instructions.Add(Instruction.Create(OpCodes.Call, Dependency(prefix + "Box")));
            text.Body.Instructions.Add(Instruction.Create(OpCodes.Ldstr, prefix)); text.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            var delegateType = Type(prefix + "Delegate", Core("MulticastDelegate"), TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Sealed);
            var ctor = new MethodDefinition(".ctor", MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName | MethodAttributes.HideBySig, voidType)
                { ImplAttributes = MethodImplAttributes.Runtime | MethodImplAttributes.Managed };
            ctor.Parameters.Add(new ParameterDefinition(objectType)); ctor.Parameters.Add(new ParameterDefinition(Core("IntPtr"))); delegateType.Methods.Add(ctor);
            var invoke = new MethodDefinition("Invoke", MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.NewSlot | MethodAttributes.HideBySig, voidType)
                { ImplAttributes = MethodImplAttributes.Runtime | MethodImplAttributes.Managed };
            invoke.Parameters.Add(new ParameterDefinition(new ByReferenceType(payload))); delegateType.Methods.Add(invoke);
            var owner = Type(prefix + "Owner");
            foreach (string eventName in new[] { "Signal", "Unselected" })
            {
                var item = new EventDefinition(eventName, EventAttributes.None, delegateType); owner.Events.Add(item);
                foreach (string kind in new[] { "add", "remove", "raise" })
                {
                    var accessor = Method(owner, kind + "_" + eventName, flags: MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.SpecialName | MethodAttributes.HideBySig);
                    accessor.Parameters.Add(new ParameterDefinition(kind == "raise" ? new ByReferenceType(payload) : delegateType));
                    if (kind != "raise") accessor.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Call, Dependency(prefix + kind + eventName)));
                    if (kind == "add") item.AddMethod = accessor;
                    else if (kind == "remove") item.RemoveMethod = accessor;
                    else item.InvokeMethod = accessor;
                }
            }
            var run = Method(driver, prefix); run.Body.Instructions.Clear(); TypeValue(run, owner); run.Body.Instructions.Add(Instruction.Create(OpCodes.Ldstr, "Signal"));
            run.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4, execute ? 280 : 24));
            if (execute)
            {
                run.Body.Instructions.Add(Instruction.Create(OpCodes.Ldnull)); run.Body.Instructions.Add(Instruction.Create(OpCodes.Ldnull));
                run.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_0)); run.Body.Instructions.Add(Instruction.Create(OpCodes.Newarr, objectType));
                run.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, Api("Type", "InvokeMember", "System.String", "System.Reflection.BindingFlags", "System.Reflection.Binder", "System.Object", "System.Object[]")));
            }
            else run.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, Api("Type", "GetEvent", "System.String", "System.Reflection.BindingFlags")));
            run.Body.Instructions.Add(Instruction.Create(OpCodes.Pop)); run.Body.Instructions.Add(Instruction.Create(OpCodes.Ret)); calls.Add(run);
        }
        foreach (bool flags in new[] { true, false })
            foreach (bool mixed in new[] { true, false })
            {
                string prefix = (flags ? "Flags" : "Arity") + (mixed ? "Mixed" : "Known");
                var owner = Type(prefix + (flags ? "Base" : "Owner"));
                string name = flags ? "Work" : "Run";
                var selected = Method(owner, name); selected.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Call, Dependency(prefix + "Selected")));
                Method(owner, "Unselected").Body.Instructions.Insert(0, Instruction.Create(OpCodes.Call, Dependency(prefix + "Unselected")));
                if (!flags)
                {
                    var generic = Method(owner, name); generic.GenericParameters.Add(new GenericParameter("T", generic));
                    generic.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Call, Dependency(prefix + "Generic")));
                }
                var target = flags ? Type(prefix + "Derived", owner) : owner;
                var run = Method(driver, prefix); run.Body.Instructions.Clear();
                if (mixed)
                {
                    run.Parameters.Add(new ParameterDefinition(Core("Boolean"))); run.Parameters.Add(new ParameterDefinition(Core(flags ? "Reflection.BindingFlags" : "Int32")));
                    run.Body.InitLocals = true; run.Body.Variables.Add(new VariableDefinition(Core(flags ? "Reflection.BindingFlags" : "Int32")));
                    var known = Instruction.Create(OpCodes.Ldc_I4, flags ? 26 : 1); var join = Instruction.Create(OpCodes.Nop);
                    run.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0)); run.Body.Instructions.Add(Instruction.Create(OpCodes.Brtrue, known));
                    run.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_1)); run.Body.Instructions.Add(Instruction.Create(OpCodes.Stloc_0)); run.Body.Instructions.Add(Instruction.Create(OpCodes.Br, join));
                    run.Body.Instructions.Add(known); run.Body.Instructions.Add(Instruction.Create(OpCodes.Stloc_0)); run.Body.Instructions.Add(join);
                }
                TypeValue(run, target); run.Body.Instructions.Add(Instruction.Create(OpCodes.Ldstr, name));
                run.Body.Instructions.Add(mixed ? Instruction.Create(OpCodes.Ldloc_0) : Instruction.Create(OpCodes.Ldc_I4, flags ? 26 : 1));
                if (!flags)
                {
                    run.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_0)); run.Body.Instructions.Add(Instruction.Create(OpCodes.Newarr, Core("Type")));
                }
                run.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, flags ? Api("Type", "GetMethod", "System.String", "System.Reflection.BindingFlags")
                    : Api("Type", "GetMethod", "System.String", "System.Int32", "System.Type[]")));
                run.Body.Instructions.Add(Instruction.Create(OpCodes.Ldnull)); run.Body.Instructions.Add(Instruction.Create(OpCodes.Ldnull));
                run.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, Api("Reflection.MethodBase", "Invoke", "System.Object", "System.Object[]")));
                run.Body.Instructions.Add(Instruction.Create(OpCodes.Pop)); run.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
                if (mixed)
                {
                    var start = Method(driver, prefix + "Start"); start.Body.Instructions.Clear(); start.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_0));
                    start.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4, flags ? 88 : 0)); start.Body.Instructions.Add(Instruction.Create(OpCodes.Call, run)); start.Body.Instructions.Add(Instruction.Create(OpCodes.Ret)); calls.Add(start);
                }
                else calls.Add(run);
            }
        foreach (var method in module.GetTypes().Where(t => t.Name.StartsWith("LookupAlternative", StringComparison.Ordinal) || t.Name.StartsWith("InvokeEvent", StringComparison.Ordinal) || t.Name.StartsWith("QueryEvent", StringComparison.Ordinal)).SelectMany(t => t.Methods).Where(m => m.HasBody))
        {
            Require(method.Body.Variables.All(v => !v.VariableType.GetElementType().Name.EndsWith("EventPayload", StringComparison.Ordinal)), "event fixture contains a payload local");
            foreach (var instruction in method.Body.Instructions)
            {
                Require(instruction.Operand is not TypeReference type || !type.GetElementType().Name.EndsWith("EventPayload", StringComparison.Ordinal), "event fixture contains an executable payload root");
                Require(instruction.OpCode != OpCodes.Newobj || instruction.Operand is not MethodReference constructor
                    || !constructor.DeclaringType.Name.EndsWith("EventDelegate", StringComparison.Ordinal), "event fixture allocated its delegate");
            }
        }
        return calls.ToArray();
    }

    internal static void Check(ModuleDefinition module, string section)
    {
        MethodDefinition? Find(string owner, string name, int arity = 0) => module.GetType("Graph." + owner)?.Methods.SingleOrDefault(m => m.Name == name && m.GenericParameters.Count == arity);
        void Body(string owner, string name, int arity = 0) => Require(Find(owner, name, arity) is { HasBody: true } method && method.Body.Instructions.Count != 0 && !Stub(method), "lookup target body was lost: " + owner + "::" + name + "/arity=" + arity);
        void Unexecuted(string owner, string name, int arity = 0) => Require(Find(owner, name, arity) is not { } method || method.HasBody && Stub(method), "lookup opened an unselected body: " + owner + "::" + name);
        if (section is "invoke-member-event" or "event-query")
        {
            bool execute = section == "invoke-member-event"; string prefix = execute ? "InvokeEvent" : "QueryEvent";
            var item = module.GetType("Graph." + prefix + "Owner")?.Events.SingleOrDefault(e => e.Name == "Signal");
            Require(item is { AddMethod: not null, RemoveMethod: not null, InvokeMethod: not null }, "event selection lost accessor metadata");
            Require(module.GetType("Graph." + prefix + "Delegate") is { HasGenericParameters: false }, "event delegate identity was lost");
            foreach (string kind in new[] { "add", "remove", "raise" })
            {
                if (execute) Body(prefix + "Owner", kind + "_Signal");
                else Require(Find(prefix + "Owner", kind + "_Signal") is { HasBody: true } method && Stub(method), "event query executed an accessor");
                Unexecuted(prefix + "Owner", kind + "_Unselected");
            }
            foreach (string kind in new[] { "add", "remove" })
            {
                if (execute) Body(prefix + kind + "SignalDependency", "Touch");
                else Require(module.GetType("Graph." + prefix + kind + "SignalDependency") is null, "event query retained an accessor dependency");
                Require(module.GetType("Graph." + prefix + kind + "UnselectedDependency") is null, "event execution opened an unrelated accessor dependency");
            }
            if (execute) { Body(prefix + "Payload", "ToString"); Body(prefix + "BoxDependency", "Touch"); }
            else
            {
                Require(Find(prefix + "Payload", "ToString") is { HasBody: true } method && Stub(method), "event metadata boxed its payload");
                Require(module.GetType("Graph." + prefix + "BoxDependency") is null, "event metadata retained a boxed dependency");
            }
        }
        else
        {
            bool flags = section.StartsWith("lookup-flags", StringComparison.Ordinal); bool mixed = section.EndsWith("mixed", StringComparison.Ordinal);
            string prefix = (flags ? "Flags" : "Arity") + (mixed ? "Mixed" : "Known"); string owner = prefix + (flags ? "Base" : "Owner"); string name = flags ? "Work" : "Run";
            if (mixed) { Body(owner, name); Body(prefix + "SelectedDependency", "Touch"); }
            else
            {
                Unexecuted(owner, name); Require(module.GetType("Graph." + prefix + "SelectedDependency") is null, "known lookup ignored its narrowing constraint");
            }
            if (!flags) { Body(owner, name, 1); Body(prefix + "GenericDependency", "Touch"); }
            Unexecuted(owner, "Unselected"); Require(module.GetType("Graph." + prefix + "UnselectedDependency") is null, "lookup opened an unrelated name");
        }
    }

    static bool Stub(MethodDefinition method) => method.Body.Instructions.Select(i => i.OpCode.Code).SequenceEqual(new[] { Code.Ldc_I4, Code.Pop, Code.Ldnull, Code.Throw });
    static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
