using Mono.Cecil;
using Mono.Cecil.Cil;

static class GraphDelegateBindingValidation
{
    internal static readonly string[] Sections = { "bind-name-return", "bind-name-ref", "bind-name-query" };

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
        var driver = Type("NamedDelegateDriver"); var calls = new List<MethodReference>();
        var protectedTypes = new HashSet<string>(StringComparer.Ordinal);
        foreach (string selection in new[] { "Return", "Ref", "Query" })
        {
            string prefix = "NamedDelegate" + selection; bool byReference = selection == "Ref";
            var payload = Type(prefix + "Payload", Core("ValueType"), TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.SequentialLayout);
            var dependency = Method(Type(prefix + "BodyDependency"), "Touch");
            var leaf = Method(payload, "ToStringLeaf", flags: MethodAttributes.Private | MethodAttributes.Static);
            var text = Method(payload, "ToString", stringType, MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig);
            text.Body.Instructions.Clear(); text.Body.Instructions.Add(Instruction.Create(OpCodes.Call, dependency)); text.Body.Instructions.Add(Instruction.Create(OpCodes.Call, leaf));
            text.Body.Instructions.Add(Instruction.Create(OpCodes.Ldstr, selection)); text.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            Method(payload, "Unselected").Body.Instructions.Insert(0, Instruction.Create(OpCodes.Call, Method(Type(prefix + "UnselectedDependency"), "Touch")));
            var delegateType = Type(prefix + "Delegate", Core("MulticastDelegate"), TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Sealed);
            var ctor = new MethodDefinition(".ctor", MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName | MethodAttributes.HideBySig, voidType)
                { ImplAttributes = MethodImplAttributes.Runtime | MethodImplAttributes.Managed };
            ctor.Parameters.Add(new ParameterDefinition(objectType)); ctor.Parameters.Add(new ParameterDefinition(Core("IntPtr"))); delegateType.Methods.Add(ctor);
            var invoke = new MethodDefinition("Invoke", MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.NewSlot | MethodAttributes.HideBySig, byReference ? voidType : payload)
                { ImplAttributes = MethodImplAttributes.Runtime | MethodImplAttributes.Managed };
            if (byReference) invoke.Parameters.Add(new ParameterDefinition(new ByReferenceType(payload))); delegateType.Methods.Add(invoke);
            protectedTypes.Add(payload.FullName); protectedTypes.Add(delegateType.FullName);
            void NameValue(MethodDefinition method)
            {
                method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldstr, delegateType.FullName + ", GraphLib"));
                method.Body.Instructions.Add(Instruction.Create(OpCodes.Call, Api("Type", "GetType", "System.String")));
            }
            var helper = Method(driver, prefix + "Bind"); helper.Body.Instructions.Clear();
            if (selection == "Query")
            {
                NameValue(helper); helper.Body.Instructions.Add(Instruction.Create(OpCodes.Pop)); helper.Body.Instructions.Add(Instruction.Create(OpCodes.Ret)); calls.Add(helper); continue;
            }
            var owner = Type(prefix + "Owner"); var target = Method(owner, byReference ? "Noop" : "Read", byReference ? voidType : payload);
            if (byReference) target.Parameters.Add(new ParameterDefinition(new ByReferenceType(payload)));
            else
            {
                var value = new FieldDefinition("Value", FieldAttributes.Public | FieldAttributes.Static, payload); owner.Fields.Add(value);
                target.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Ldsfld, value));
            }
            helper.Parameters.Add(new ParameterDefinition(Core(byReference ? "Type" : "Reflection.MethodInfo")));
            if (byReference)
            {
                NameValue(helper); helper.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0)); helper.Body.Instructions.Add(Instruction.Create(OpCodes.Ldstr, "Noop"));
                helper.Body.Instructions.Add(Instruction.Create(OpCodes.Call, Api("Delegate", "CreateDelegate", "System.Type", "System.Type", "System.String")));
                helper.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_1)); helper.Body.Instructions.Add(Instruction.Create(OpCodes.Newarr, objectType));
            }
            else
            {
                helper.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0)); NameValue(helper);
                helper.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, Api("Reflection.MethodInfo", "CreateDelegate", "System.Type")));
                helper.Body.Instructions.Add(Instruction.Create(OpCodes.Ldnull));
            }
            helper.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, Api("Delegate", "DynamicInvoke", "System.Object[]")));
            helper.Body.Instructions.Add(Instruction.Create(OpCodes.Pop)); helper.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            var start = Method(driver, prefix + "Start"); start.Body.Instructions.Clear();
            start.Body.Instructions.Add(Instruction.Create(byReference ? OpCodes.Ldftn : OpCodes.Call, target)); start.Body.Instructions.Add(Instruction.Create(OpCodes.Pop));
            TypeValue(start, owner);
            if (!byReference)
            {
                start.Body.Instructions.Add(Instruction.Create(OpCodes.Ldstr, "Read")); start.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, Api("Type", "GetMethod", "System.String")));
            }
            start.Body.Instructions.Add(Instruction.Create(OpCodes.Call, helper)); start.Body.Instructions.Add(Instruction.Create(OpCodes.Ret)); calls.Add(start);
        }
        foreach (var method in module.GetTypes().SelectMany(t => t.Methods).Where(m => m.HasBody))
        {
            Require(!method.Body.Variables.Any(v => protectedTypes.Contains(v.VariableType.GetElementType().FullName)), "name-bound delegate fixture contains a payload or delegate local");
            foreach (var instruction in method.Body.Instructions)
                Require(!(instruction.Operand is TypeReference type && protectedTypes.Contains(type.GetElementType().FullName))
                    && !(instruction.OpCode == OpCodes.Newobj && instruction.Operand is MethodReference ctor && protectedTypes.Contains(ctor.DeclaringType.GetElementType().FullName)), "name-bound delegate fixture contains an executable payload or delegate root");
        }
        return calls.ToArray();
    }

    internal static void Check(ModuleDefinition module, string section)
    {
        string selection = section == "bind-name-return" ? "Return" : section == "bind-name-ref" ? "Ref" : "Query";
        string prefix = "NamedDelegate" + selection;
        MethodDefinition? Find(string owner, string name) => module.GetType("Graph." + owner)?.Methods.SingleOrDefault(m => m.Name == name);
        void Body(string owner, string name) => Require(Find(owner, name) is { HasBody: true } method && method.Body.Instructions.Count != 0 && !Stub(method), "name-bound delegate target body was lost: " + owner + "::" + name);
        void Unexecuted(string owner, string name) => Require(Find(owner, name) is not { } method || method.HasBody && Stub(method), "name-bound delegate opened an unrelated body: " + owner + "::" + name);
        var delegateType = module.GetType("Graph." + prefix + "Delegate"); var invoke = delegateType?.Methods.SingleOrDefault(m => m.Name == "Invoke");
        Require(delegateType is { HasGenericParameters: false } && invoke is { IsRuntime: true, HasBody: false, IsVirtual: true }, "name-bound delegate lost its original Invoke metadata");
        Require(invoke!.ReturnType.FullName == (selection == "Ref" ? "System.Void" : "Graph." + prefix + "Payload")
            && invoke.Parameters.Select(p => p.ParameterType.FullName).SequenceEqual(selection == "Ref" ? new[] { "Graph." + prefix + "Payload&" } : Array.Empty<string>()), "name-bound delegate changed its Invoke signature");
        if (selection == "Query")
        {
            foreach (string name in new[] { "ToString", "ToStringLeaf" }) Unexecuted(prefix + "Payload", name);
            Require(Find(prefix + "Payload", "ToString") is { HasBody: true } method && Stub(method), "delegate name query boxed its payload");
            Require(module.GetType("Graph." + prefix + "BodyDependency") is null, "delegate name query retained its payload body dependency");
        }
        else
        {
            Body(prefix + "Owner", selection == "Ref" ? "Noop" : "Read");
            Body(prefix + "Payload", "ToString"); Body(prefix + "Payload", "ToStringLeaf"); Body(prefix + "BodyDependency", "Touch");
        }
        Unexecuted(prefix + "Payload", "Unselected"); Require(module.GetType("Graph." + prefix + "UnselectedDependency") is null, "name-bound delegate opened an unrelated ordinary body");
    }

    static bool Stub(MethodDefinition method) => method.Body.Instructions.Select(i => i.OpCode.Code).SequenceEqual(new[] { Code.Ldc_I4, Code.Pop, Code.Ldnull, Code.Throw });
    static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
