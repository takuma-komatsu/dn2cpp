using Mono.Cecil;
using Mono.Cecil.Cil;

static class GraphPropertyValidation
{
    internal static readonly string[] Sections = { "property-set-box", "property-invoke-box", "property-query-box" };

    internal static MethodReference[] Create(ModuleDefinition module, ModuleDefinition core)
    {
        TypeReference Core(string name) => module.ImportReference(core.GetType("System." + name));
        var objectType = Core("Object"); var voidType = Core("Void"); var intType = Core("Int32");
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
        var driver = Type("PropertySetterDriver"); var calls = new List<MethodReference>(); var payloads = new HashSet<string>(StringComparer.Ordinal);
        foreach (string selection in new[] { "SetValue", "InvokeMember", "Query" })
        {
            string prefix = "Setter" + selection;
            var payload = Type(prefix + "Payload", Core("ValueType"), TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.SequentialLayout); payloads.Add(payload.FullName);
            var dependency = Method(Type(prefix + "BodyDependency"), "Touch");
            var leaf = Method(payload, "ToStringLeaf", flags: MethodAttributes.Private | MethodAttributes.Static);
            var text = Method(payload, "ToString", Core("String"), MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig);
            text.Body.Instructions.Clear(); text.Body.Instructions.Add(Instruction.Create(OpCodes.Call, dependency)); text.Body.Instructions.Add(Instruction.Create(OpCodes.Call, leaf));
            text.Body.Instructions.Add(Instruction.Create(OpCodes.Ldstr, selection)); text.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            Method(payload, "Unselected").Body.Instructions.Insert(0, Instruction.Create(OpCodes.Call, Method(Type(prefix + "UnselectedDependency"), "Touch")));
            var owner = Type(prefix + "Owner");
            var setter = Method(owner, "set_Item", flags: MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.SpecialName | MethodAttributes.HideBySig);
            setter.Parameters.Add(new ParameterDefinition(new ByReferenceType(payload))); setter.Parameters.Add(new ParameterDefinition(intType));
            var property = new PropertyDefinition("Item", PropertyAttributes.None, intType) { SetMethod = setter };
            owner.Properties.Add(property);
            Require(property.Parameters.Select(p => p.ParameterType.FullName).SequenceEqual(new[] { payload.FullName + "&" }), "setter fixture has an invalid property index signature");
            var run = Method(driver, prefix); run.Body.Instructions.Clear();
            run.Body.Instructions.Add(Instruction.Create(OpCodes.Ldstr, owner.FullName + ", GraphLib")); run.Body.Instructions.Add(Instruction.Create(OpCodes.Call, Api("Type", "GetType", "System.String")));
            run.Body.Instructions.Add(Instruction.Create(OpCodes.Ldstr, "Item"));
            if (selection == "InvokeMember")
            {
                run.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4, 8216));
                run.Body.Instructions.Add(Instruction.Create(OpCodes.Ldnull)); run.Body.Instructions.Add(Instruction.Create(OpCodes.Ldnull));
                run.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_2)); run.Body.Instructions.Add(Instruction.Create(OpCodes.Newarr, objectType));
                run.Body.Instructions.Add(Instruction.Create(OpCodes.Dup)); run.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_1));
                run.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_1)); run.Body.Instructions.Add(Instruction.Create(OpCodes.Box, intType)); run.Body.Instructions.Add(Instruction.Create(OpCodes.Stelem_Ref));
                run.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, Api("Type", "InvokeMember", "System.String", "System.Reflection.BindingFlags", "System.Reflection.Binder", "System.Object", "System.Object[]")));
                run.Body.Instructions.Add(Instruction.Create(OpCodes.Pop));
            }
            else
            {
                run.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, Api("Type", "GetProperty", "System.String")));
                if (selection == "SetValue")
                {
                    run.Body.Instructions.Add(Instruction.Create(OpCodes.Ldnull)); run.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_1)); run.Body.Instructions.Add(Instruction.Create(OpCodes.Box, intType));
                    run.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_1)); run.Body.Instructions.Add(Instruction.Create(OpCodes.Newarr, objectType));
                    run.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, Api("Reflection.PropertyInfo", "SetValue", "System.Object", "System.Object", "System.Object[]")));
                }
                else run.Body.Instructions.Add(Instruction.Create(OpCodes.Pop));
            }
            run.Body.Instructions.Add(Instruction.Create(OpCodes.Ret)); calls.Add(run);
        }
        foreach (var method in module.GetTypes().SelectMany(t => t.Methods).Where(m => m.HasBody))
        {
            Require(!method.Body.Variables.Any(v => payloads.Contains(v.VariableType.GetElementType().FullName)), "setter fixture contains a payload local");
            foreach (var instruction in method.Body.Instructions)
                Require(!(instruction.Operand is TypeReference type && payloads.Contains(type.GetElementType().FullName))
                    && !(instruction.OpCode == OpCodes.Newobj && instruction.Operand is MethodReference ctor && payloads.Contains(ctor.DeclaringType.GetElementType().FullName)), "setter fixture contains an executable payload root");
        }
        return calls.ToArray();
    }

    internal static void Check(ModuleDefinition module, string section)
    {
        string selection = section == "property-set-box" ? "SetValue" : section == "property-invoke-box" ? "InvokeMember" : "Query";
        string prefix = "Setter" + selection;
        MethodDefinition? Find(string owner, string name) => module.GetType("Graph." + owner)?.Methods.SingleOrDefault(m => m.Name == name);
        void Body(string owner, string name) => Require(Find(owner, name) is { HasBody: true } method && method.Body.Instructions.Count != 0 && !Stub(method), "property setter boxed target body was lost: " + owner + "::" + name);
        void Unexecuted(string owner, string name) => Require(Find(owner, name) is not { } method || method.HasBody && Stub(method), "property setter opened an unrelated body: " + owner + "::" + name);
        var property = module.GetType("Graph." + prefix + "Owner")?.Properties.SingleOrDefault(p => p.Name == "Item");
        Require(property is { GetMethod: null, SetMethod: not null } && property.PropertyType.FullName == "System.Int32"
            && property.Parameters.Select(p => p.ParameterType.FullName).SequenceEqual(new[] { "Graph." + prefix + "Payload&" }), "setter fixture lost its write-only ref indexer metadata");
        Require(property!.SetMethod!.IsStatic && !property.SetMethod.IsVirtual
            && property.SetMethod.Parameters.Select(p => p.ParameterType.FullName).SequenceEqual(new[] { "Graph." + prefix + "Payload&", "System.Int32" }), "setter fixture changed its original ref parameter signature");
        if (selection == "Query")
        {
            foreach (var (owner, name) in new[] { (prefix + "Owner", "set_Item"), (prefix + "Payload", "ToString") })
                Require(Find(owner, name) is { HasBody: true } method && Stub(method), "property query executed a setter or boxed its payload: " + owner + "::" + name);
            Unexecuted(prefix + "Payload", "ToStringLeaf"); Require(module.GetType("Graph." + prefix + "BodyDependency") is null, "property query retained a boxed payload dependency");
        }
        else
        {
            Body(prefix + "Owner", "set_Item"); Body(prefix + "Payload", "ToString"); Body(prefix + "Payload", "ToStringLeaf"); Body(prefix + "BodyDependency", "Touch");
        }
        Unexecuted(prefix + "Payload", "Unselected"); Require(module.GetType("Graph." + prefix + "UnselectedDependency") is null, "property setter opened an unrelated ordinary body");
    }

    static bool Stub(MethodDefinition method) => method.Body.Instructions.Select(i => i.OpCode.Code).SequenceEqual(new[] { Code.Ldc_I4, Code.Pop, Code.Ldnull, Code.Throw });
    static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
