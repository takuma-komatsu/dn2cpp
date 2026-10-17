using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Text.Json;
using System.Xml.Linq;

static partial class GraphArrayValidation
{
    private static readonly string[] ModelAuthorityModes =
    {
        "authority-model-only", "authority-ordinary-late", "authority-array",
        "authority-policy", "authority-policy-unformed", "authority-attribute", "authority-attribute-passive",
    };

    private static bool ModelAuthorityMode(string mode) => ModelAuthorityModes.Contains(mode);
    private static bool ModelAuthorityPayload(string mode) => mode is "authority-ordinary-late" or "authority-array"
        or "authority-policy";

    private static void CreateModelAuthority(string directory, string corelib, string mode)
    {
        Require(ModelAuthorityMode(mode), "unknown model authority mode");
        Directory.CreateDirectory(directory);
        using var core = AssemblyDefinition.ReadAssembly(corelib);
        using var app = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition("GraphApp", new Version(1, 0)), "GraphApp", ModuleKind.Console);
        using var lib = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition("GraphLib", new Version(1, 0)), "GraphLib", ModuleKind.Dll);
        using var facade = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition("System.ArrayFacade", new Version(1, 0)), "System.ArrayFacade", ModuleKind.Dll);
        using var leaf = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition("System.ArrayLeaf", new Version(1, 0)), "System.ArrayLeaf", ModuleKind.Dll);
        using var shim = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition("DnZlib", new Version(1, 0)), "DnZlib", ModuleKind.Dll);
        TypeReference Core(ModuleDefinition module, string name) => module.ImportReference(core.MainModule.GetType("System." + name));
        MethodReference Api(ModuleDefinition module, string owner, string name, params string[] parameters) => module.ImportReference(
            core.MainModule.GetType("System." + owner).Methods.Single(m => m.Name == name && !m.HasGenericParameters
                && m.Parameters.Select(p => p.ParameterType.FullName).SequenceEqual(parameters)));
        TypeDefinition Type(ModuleDefinition module, string name, bool generic = false, bool value = false)
        {
            var type = new TypeDefinition("Graph", name, TypeAttributes.Public | TypeAttributes.BeforeFieldInit
                | (value ? TypeAttributes.Sealed | TypeAttributes.SequentialLayout : TypeAttributes.Class),
                Core(module, value ? "ValueType" : "Object"));
            module.Types.Add(type);
            if (generic) type.GenericParameters.Add(new GenericParameter("T", type));
            return type;
        }
        MethodDefinition Method(TypeDefinition owner, string name, bool instance = false)
        {
            var method = new MethodDefinition(name, MethodAttributes.Public | MethodAttributes.HideBySig
                | (instance ? 0 : MethodAttributes.Static), Core(owner.Module, "Void"));
            if (name == ".ctor") method.Attributes |= MethodAttributes.SpecialName | MethodAttributes.RTSpecialName;
            owner.Methods.Add(method);
            return method;
        }
        GenericInstanceType Closed(ModuleDefinition module, TypeReference owner, TypeReference argument)
        {
            var type = new GenericInstanceType(module.ImportReference(owner));
            type.GenericArguments.Add(argument);
            return type;
        }
        void CallClosed(MethodDefinition caller, MethodDefinition target, TypeReference argument)
        {
            var owner = Closed(caller.Module, target.DeclaringType, argument);
            var reference = new MethodReference(target.Name, Core(caller.Module, "Void"), owner);
            if (target.Parameters.Count != 0)
            {
                // The MemberRef owns a distinct formal row; the original TypeDef
                // demand and this later closed owner must share ordinary authority.
                reference.Parameters.Add(new ParameterDefinition(owner.ElementType.GenericParameters[0]));
                if (argument.IsValueType)
                {
                    caller.Body.InitLocals = true;
                    var local = new VariableDefinition(argument);
                    caller.Body.Variables.Add(local);
                    caller.Body.Instructions.Add(Instruction.Create(OpCodes.Ldloca, local));
                    caller.Body.Instructions.Add(Instruction.Create(OpCodes.Initobj, argument));
                    caller.Body.Instructions.Add(Instruction.Create(OpCodes.Ldloc, local));
                }
                else caller.Body.Instructions.Add(Instruction.Create(OpCodes.Ldnull));
            }
            caller.Body.Instructions.Add(Instruction.Create(OpCodes.Call, reference));
        }
        MethodDefinition Dependency(string name)
        {
            var method = Method(Type(lib.MainModule, name), "Touch");
            method.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            return method;
        }
        void Touch(MethodDefinition method, string name) => method.Body.Instructions.Add(
            Instruction.Create(OpCodes.Call, method.Module.ImportReference(Dependency(name))));
        void ObjectBase(MethodDefinition ctor)
        {
            ctor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            ctor.Body.Instructions.Add(Instruction.Create(OpCodes.Call, Api(ctor.Module, "Object", ".ctor")));
        }
        TypeDefinition Payload(string name, bool value = false, string fieldPrimitive = "Int32")
        {
            var type = Type(lib.MainModule, name, value: value);
            var ctor = Method(type, ".ctor", true);
            if (value)
            {
                var field = new FieldDefinition("Number", FieldAttributes.Public, Core(lib.MainModule, fieldPrimitive));
                type.Fields.Add(field);
                ctor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
                ctor.Body.Instructions.Add(fieldPrimitive == "Int64" ? Instruction.Create(OpCodes.Ldc_I8, 1L) : Instruction.Create(OpCodes.Ldc_I4_1));
                ctor.Body.Instructions.Add(Instruction.Create(OpCodes.Stfld, field));
            }
            else ObjectBase(ctor);
            Touch(ctor, name + "Dependency");
            ctor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            var unused = Method(type, "Unused");
            Touch(unused, name + "UnusedDependency");
            unused.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            return type;
        }
        var safe = Payload("AuthoritySafe", value: true);
        var payload = Payload("AuthorityPayload");
        var dormant = Payload("AuthorityDormant");
        var attributePayload = mode.StartsWith("authority-attribute", StringComparison.Ordinal)
            ? Payload("AuthorityAttributePayload", value: true, fieldPrimitive: "Int64") : null;
        MethodDefinition Factory(string name, bool parameter)
        {
            var owner = Type(lib.MainModule, name + "`1", generic: true);
            owner.GenericParameters[0].Attributes = GenericParameterAttributes.DefaultConstructorConstraint;
            var method = Method(owner, "Run");
            if (parameter) method.Parameters.Add(new ParameterDefinition(owner.GenericParameters[0]));
            var activator = new GenericInstanceMethod(lib.MainModule.ImportReference(core.MainModule.GetType("System.Activator")
                .Methods.Single(m => m.Name == "CreateInstance" && m.IsStatic && m.GenericParameters.Count == 1 && m.Parameters.Count == 0)));
            activator.GenericArguments.Add(owner.GenericParameters[0]);
            method.Body.Instructions.Add(Instruction.Create(OpCodes.Call, activator));
            method.Body.Instructions.Add(Instruction.Create(OpCodes.Pop));
            method.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            return method;
        }
        var probe = Factory("AuthorityProbe", parameter: true);
        var helper = Factory("AuthorityHelper", parameter: false);
        var relay = Factory("AuthorityRelay", parameter: false);
        var wrapper = Type(lib.MainModule, "AuthorityWrapper`1", generic: true);
        wrapper.GenericParameters[0].Attributes = GenericParameterAttributes.DefaultConstructorConstraint;
        var wrapperCtor = Method(wrapper, ".ctor", true);
        ObjectBase(wrapperCtor);
        CallClosed(wrapperCtor, helper, wrapper.GenericParameters[0]);
        wrapperCtor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        void Consequence(MethodDefinition method)
        {
            CallClosed(method, relay, Closed(method.Module, wrapper, method.DeclaringType.GenericParameters[0]));
            method.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        }
        var cell = Type(lib.MainModule, "AuthorityCell`1", generic: true, value: true);
        cell.GenericParameters[0].Attributes = GenericParameterAttributes.DefaultConstructorConstraint;
        cell.Fields.Add(new FieldDefinition("Value", FieldAttributes.Public, cell.GenericParameters[0]));
        Consequence(Method(cell, ".ctor", true));
        var policy = Type(lib.MainModule, "AuthorityPolicy`1", generic: true);
        policy.GenericParameters[0].Attributes = GenericParameterAttributes.DefaultConstructorConstraint;
        Consequence(Method(policy, "Activate"));
        var effect = Type(lib.MainModule, "AuthorityEffect`1", generic: true);
        effect.GenericParameters[0].Attributes = GenericParameterAttributes.DefaultConstructorConstraint;
        Consequence(Method(effect, "Activate"));
        var attribute = Type(lib.MainModule, "AuthorityDataAttribute");
        attribute.BaseType = Core(lib.MainModule, "Attribute");
        var attributeCtor = Method(attribute, ".ctor", true);
        attributeCtor.Parameters.Add(new ParameterDefinition(Core(lib.MainModule, "Type")));
        attributeCtor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
        attributeCtor.Body.Instructions.Add(Instruction.Create(OpCodes.Call, Api(lib.MainModule, "Attribute", ".ctor")));
        attributeCtor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        var provider = Type(lib.MainModule, "AuthorityProvider`1", generic: true);
        provider.GenericParameters[0].Attributes = GenericParameterAttributes.DefaultConstructorConstraint;
        provider.BaseType = Closed(lib.MainModule, probe.DeclaringType, provider.GenericParameters[0]);
        if (mode.StartsWith("authority-attribute", StringComparison.Ordinal))
        {
            var row = new CustomAttribute(attributeCtor);
            row.ConstructorArguments.Add(new CustomAttributeArgument(Core(lib.MainModule, "Type"), Closed(lib.MainModule, effect, attributePayload!)));
            provider.CustomAttributes.Add(row);
        }
        var marker = Type(lib.MainModule, "AuthorityMarker`1", generic: true);
        marker.Attributes = TypeAttributes.Public | TypeAttributes.Interface | TypeAttributes.Abstract;
        marker.BaseType = null;
        var anchor = Type(lib.MainModule, "AuthorityAnchor");
        var formation = Closed(lib.MainModule, mode.StartsWith("authority-attribute", StringComparison.Ordinal)
            ? provider : probe.DeclaringType, payload);
        anchor.Interfaces.Add(new InterfaceImplementation(Closed(lib.MainModule, marker, formation)));
        if (mode is "authority-array" or "authority-policy")
            anchor.Interfaces.Add(new InterfaceImplementation(Closed(lib.MainModule, marker,
                Closed(lib.MainModule, mode == "authority-array" ? cell : policy, payload))));
        var anchorCtor = Method(anchor, ".ctor", true);
        ObjectBase(anchorCtor);
        Touch(anchorCtor, "AuthorityAnchorDependency");
        anchorCtor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        anchor.Fields.Add(new FieldDefinition("Lazy", FieldAttributes.Public, Closed(lib.MainModule, probe.DeclaringType, dormant)));
        var lazy = Method(anchor, "Unused");
        lazy.Parameters.Add(new ParameterDefinition(Closed(lib.MainModule, helper.DeclaringType, dormant)));
        Touch(lazy, "AuthorityLazyDependency");
        lazy.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));

        var leafArm = Method(Type(leaf.MainModule, "AuthorityLeaf"), "Arm");
        leafArm.Parameters.Add(new ParameterDefinition(Core(leaf.MainModule, "Array")));
        leafArm.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
        leafArm.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, Api(leaf.MainModule, "Array", "Initialize")));
        leafArm.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        var bridge = Method(Type(facade.MainModule, "AuthorityBridge"), "Arm");
        bridge.Parameters.Add(new ParameterDefinition(Core(facade.MainModule, "Array")));
        bridge.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
        bridge.Body.Instructions.Add(Instruction.Create(OpCodes.Call, facade.MainModule.ImportReference(leafArm)));
        bridge.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        var entry = Type(app.MainModule, "AuthorityEntry");
        var late = Method(entry, "Late");
        if (mode == "authority-ordinary-late") CallClosed(late, probe, app.MainModule.ImportReference(payload));
        late.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        var schedule = Method(entry, "Schedule");
        schedule.Body.Instructions.Add(Instruction.Create(OpCodes.Call, late));
        schedule.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        var trigger = Method(entry, "Trigger");
        if (mode == "authority-array")
        {
            trigger.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_1));
            trigger.Body.Instructions.Add(Instruction.Create(OpCodes.Newarr, Core(app.MainModule, "Int32")));
            trigger.Body.Instructions.Add(Instruction.Create(OpCodes.Call, app.MainModule.ImportReference(bridge)));
        }
        if (mode == "authority-attribute")
        {
            trigger.Body.Instructions.Add(Instruction.Create(OpCodes.Ldnull));
            trigger.Body.Instructions.Add(Instruction.Create(OpCodes.Newobj, app.MainModule.ImportReference(attributeCtor)));
            trigger.Body.Instructions.Add(Instruction.Create(OpCodes.Pop));
        }
        trigger.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        var main = Method(entry, "Main");
        app.EntryPoint = main;
        foreach (string setter in new[] { "set_CurrentCulture", "set_CurrentUICulture" })
        {
            main.Body.Instructions.Add(Instruction.Create(OpCodes.Call, Api(app.MainModule, "Globalization.CultureInfo", "get_InvariantCulture")));
            main.Body.Instructions.Add(Instruction.Create(OpCodes.Call, Api(app.MainModule, "Globalization.CultureInfo", setter, "System.Globalization.CultureInfo")));
        }
        CallClosed(main, probe, app.MainModule.ImportReference(safe));
        CallClosed(main, helper, app.MainModule.ImportReference(safe));
        main.Body.Instructions.Add(Instruction.Create(OpCodes.Call, schedule));
        main.Body.Instructions.Add(Instruction.Create(OpCodes.Call, trigger));
        main.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        var rules = new XElement("assembly", new XAttribute("fullname", "GraphLib"), new XAttribute("preserve", "nothing"));
        if (mode.StartsWith("authority-policy", StringComparison.Ordinal))
            rules.Add(new XElement("type", new XAttribute("fullname", policy.FullName), new XAttribute("preserve", "nothing"),
                new XElement("method", new XAttribute("name", "Activate"))));
        if (mode.StartsWith("authority-attribute", StringComparison.Ordinal))
            rules.Add(new XElement("type", new XAttribute("fullname", effect.FullName), new XAttribute("preserve", "nothing"),
                new XAttribute("required", "0"), new XElement("method", new XAttribute("name", "Activate"))));
        File.WriteAllText(Path.Combine(directory, "link.xml"), new XElement("linker", rules.HasElements ? rules : null) + "\n");
        foreach (var assembly in new[] { app, lib, facade, leaf, shim })
            assembly.Write(Path.Combine(directory, assembly.Name.Name + ".dll"), new WriterParameters { Timestamp = 0, DeterministicMvid = true });
    }

    private static void CheckModelAuthority(string directory, string mode, string state)
    {
        Require(ModelAuthorityMode(mode) && (state is "original" or "published" or "observed-before"), "unknown authority fixture state");
        using var app = AssemblyDefinition.ReadAssembly(Path.Combine(directory, "GraphApp.dll"));
        using var lib = AssemblyDefinition.ReadAssembly(Path.Combine(directory, "GraphLib.dll"));
        bool Body(string owner, string name) => lib.MainModule.GetType("Graph." + owner)?.Methods.SingleOrDefault(m => m.Name == name)
            is { HasBody: true } method && !Stub(method);
        var actual = new Dictionary<string, bool>
        {
            ["safe"] = Body("AuthoritySafe", ".ctor"), ["payload"] = Body("AuthorityPayload", ".ctor"),
            ["cell"] = Body("AuthorityCell`1", ".ctor"), ["policy"] = Body("AuthorityPolicy`1", "Activate"),
            ["effect"] = Body("AuthorityEffect`1", "Activate"), ["wrapper"] = Body("AuthorityWrapper`1", ".ctor"),
            ["attribute"] = Body("AuthorityDataAttribute", ".ctor"),
        };
        if (mode.StartsWith("authority-attribute", StringComparison.Ordinal)) actual["attributePayload"] = Body("AuthorityAttributePayload", ".ctor");
        foreach (string name in new[] { "AuthoritySafe", "AuthorityPayload", "AuthorityDormant", "AuthorityAttributePayload" })
            Require(Body(name, ".ctor") == Body(name + "Dependency", "Touch"), "authority constructor dependency disagrees: " + name);
        string[] negatives = { "AuthorityDormantDependency", "AuthoritySafeUnusedDependency", "AuthorityPayloadUnusedDependency",
            "AuthorityDormantUnusedDependency", "AuthorityAnchorDependency", "AuthorityLazyDependency" };
        if (mode.StartsWith("authority-attribute", StringComparison.Ordinal)) negatives = negatives.Append("AuthorityAttributePayloadUnusedDependency").ToArray();
        if (state == "original") Require(actual.Values.All(x => x) && negatives.All(n => Body(n, "Touch")), "authority original body inventory changed");
        if (state == "published")
        {
            Require(actual["safe"] && actual["payload"] == ModelAuthorityPayload(mode), "authority generic argument body admission changed");
            if (mode.StartsWith("authority-attribute", StringComparison.Ordinal))
            {
                bool active = mode == "authority-attribute";
                Require(actual["attributePayload"] == active, "conditional effect value argument body admission changed");
                Require(actual["attribute"] == active && actual["effect"] == active && actual["wrapper"] == active,
                    "passive attribute evidence acquired consequential bodies");
            }
            Require(negatives.All(n => !Body(n, "Touch")), "model-only or passive evidence executed an unrelated body");
            if (mode == "authority-array") Require(actual["cell"] && actual["wrapper"], "array consequence lost generic helper arguments");
            if (mode == "authority-policy") Require(actual["policy"] && actual["wrapper"], "effective generic policy lost helper arguments");
            if (mode == "authority-attribute") Require(actual["effect"] && actual["wrapper"] && actual["attribute"], "admitted attribute identity lost conditional effect arguments");
        }
        var entry = app.MainModule.GetType("Graph.AuthorityEntry");
        var main = entry.Methods.Single(m => m.Name == "Main");
        var calls = main.Body.Instructions.Where(i => i.Operand is MethodReference).Select(i => (MethodReference)i.Operand).ToArray();
        Require(calls.Take(4).Select(m => m.Name).SequenceEqual(new[] { "get_InvariantCulture", "set_CurrentCulture", "get_InvariantCulture", "set_CurrentUICulture" }),
            "authority driver cultures changed");
        Require(calls.Skip(4).Select(m => m.Name).SequenceEqual(new[] { "Run", "Run", "Schedule", "Trigger" }), "normal demand installation order changed");
        Require(calls[4].DeclaringType.FullName == "Graph.AuthorityProbe`1<Graph.AuthoritySafe>"
            && calls[5].DeclaringType.FullName == "Graph.AuthorityHelper`1<Graph.AuthoritySafe>", "ordinary safe argument changed");
        var schedule = entry.Methods.Single(m => m.Name == "Schedule");
        Require(schedule.Body.Instructions.Select(i => i.OpCode.Code).SequenceEqual(new[] { Code.Call, Code.Ret })
            && schedule.Body.Instructions[0].Operand is MethodReference scheduled && scheduled.Name == "Late", "late FIFO call chain changed");
        var late = entry.Methods.Single(m => m.Name == "Late");
        var lateCalls = late.Body.Instructions.Where(i => i.Operand is MethodReference).Select(i => (MethodReference)i.Operand).ToArray();
        Require(lateCalls.Length == (mode == "authority-ordinary-late" ? 1 : 0), "late concrete occurrence changed");
        var probe = lib.MainModule.GetType("Graph.AuthorityProbe`1");
        var payloadType = lib.MainModule.GetType("Graph.AuthorityPayload");
        Require(lib.MainModule.GetType("Graph.AuthoritySafe").IsValueType
            && (payloadType is not null ? !payloadType.IsValueType : state != "original"),
            "safe and model-only arguments entered the same reference sharing group");
        foreach (var reference in new[] { calls[4] }.Concat(lateCalls))
            Require(reference.MetadataToken.TokenType == TokenType.MemberRef && reference.DeclaringType is GenericInstanceType
                && reference.Parameters.Single().ParameterType is GenericParameter { Position: 0, Type: GenericParameterType.Type } alias
                && !ReferenceEquals(alias, probe.GenericParameters[0]) && !ReferenceEquals(alias.Owner, probe),
                "MemberRef formal alias collapsed into the definition parameter");
        if (lateCalls.Length != 0) Require(lateCalls[0].DeclaringType.FullName == "Graph.AuthorityProbe`1<Graph.AuthorityPayload>", "late argument changed");
        var provider = lib.MainModule.GetType("Graph.AuthorityProvider`1");
        string? attributeTarget = provider?.CustomAttributes.SingleOrDefault()?.ConstructorArguments.Single().Value is TypeReference target ? target.FullName : null;
        if (state == "original" && mode.StartsWith("authority-attribute", StringComparison.Ordinal) || state == "published" && mode == "authority-attribute")
            Require(attributeTarget == "Graph.AuthorityEffect`1<Graph.AuthorityAttributePayload>", "formed provider closed attribute identity changed");
        if (mode.StartsWith("authority-attribute", StringComparison.Ordinal) && (state == "original" || state == "published" && mode == "authority-attribute"))
        {
            var argument = lib.MainModule.GetType("Graph.AuthorityAttributePayload");
            Require(argument is { IsValueType: true } && argument.Fields.Single(f => f.Name == "Number").FieldType.FullName == "System.Int64"
                && lib.MainModule.GetType("Graph.AuthoritySafe").Fields.Single(f => f.Name == "Number").FieldType.FullName == "System.Int32",
                "attribute effect argument lost its distinct value layout");
            Require(provider?.BaseType is GenericInstanceType relation && relation.ElementType.FullName == "Graph.AuthorityProbe`1"
                && relation.GenericArguments.Single() is GenericParameter { Position: 0, Type: GenericParameterType.Type },
                "attribute provider stopped carrying its original passive formal relation");
        }
        using var facade = AssemblyDefinition.ReadAssembly(Path.Combine(directory, "System.ArrayFacade.dll"));
        using var leaf = AssemblyDefinition.ReadAssembly(Path.Combine(directory, "System.ArrayLeaf.dll"));
        Require(new[] { facade, leaf }.All(a => !a.MainModule.AssemblyReferences.Any(r => r.Name == "GraphLib")),
            "copied bridge gained a direct dependency that would hide ordinary binding loss");
        Console.WriteLine("model-authority-observed=" + JsonSerializer.Serialize(new { bodies = actual,
            negatives = negatives.ToDictionary(n => n, n => Body(n, "Touch")), attributeTarget,
            ordinaryOwner = calls[4].DeclaringType.FullName, lateOwner = lateCalls.SingleOrDefault()?.DeclaringType.FullName,
            distinctMemberRefAlias = true }));
        Console.WriteLine("graph-model-authority=" + state + ":" + mode + ":model-only,ordinary-replay,consequential-generic-arguments");
    }
}
