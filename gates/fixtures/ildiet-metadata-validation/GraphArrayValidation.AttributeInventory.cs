using Mono.Cecil;
using Mono.Cecil.Cil;

static partial class GraphArrayValidation
{
    private static bool AttributeInventoryMode(string mode) => mode.StartsWith("inventory-", StringComparison.Ordinal);
    private static bool InventoryClass(string mode) => mode.StartsWith("inventory-class-", StringComparison.Ordinal);
    private static bool InventoryUntouched(string mode) => mode.StartsWith("inventory-untouched-", StringComparison.Ordinal);
    private static bool InventoryConstruct(string mode) => InventoryClass(mode) ? !mode.EndsWith("-no-carrier", StringComparison.Ordinal)
        : InventoryUntouched(mode) && !mode.EndsWith("-unreached", StringComparison.Ordinal);
    private static bool InventoryActive(string mode) => !mode.EndsWith("-no-carrier", StringComparison.Ordinal)
        && !mode.EndsWith("-unreached", StringComparison.Ordinal) && !mode.EndsWith("-unnamed", StringComparison.Ordinal)
        && !mode.Contains("-excluded-", StringComparison.Ordinal);
    private static readonly string[] InventoryEffects = { "Ctor", "Setter", "Getter", "Sibling", "Ordinary" };

    private static void CreateAttributeInventory(string directory, string corelib, string mode)
    {
        Directory.CreateDirectory(directory);
        using var core = AssemblyDefinition.ReadAssembly(corelib);
        using var app = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition("GraphApp", new Version(1, 0)), "GraphApp", ModuleKind.Console);
        using var lib = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition("GraphLib", new Version(1, 0)), "GraphLib", ModuleKind.Dll);
        using var shim = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition("DnZlib", new Version(1, 0)), "DnZlib", ModuleKind.Dll);
        using var facade = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition("System.ArrayFacade", new Version(1, 0)), "System.ArrayFacade", ModuleKind.Dll);
        using var leaf = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition("System.ArrayLeaf", new Version(1, 0)), "System.ArrayLeaf", ModuleKind.Dll);
        TypeReference Core(ModuleDefinition m, string name) => m.ImportReference(core.MainModule.GetType("System." + name));
        MethodReference Api(ModuleDefinition m, string owner, string name, params string[] parameters) => m.ImportReference(core.MainModule.GetType("System." + owner).Methods.Single(x => x.Name == name && !x.HasGenericParameters && x.Parameters.Select(p => p.ParameterType.FullName).SequenceEqual(parameters)));
        TypeDefinition Type(ModuleDefinition m, string name)
        {
            var t = new TypeDefinition("Graph", name, TypeAttributes.Public | TypeAttributes.Class, Core(m, "Object")); m.Types.Add(t);
            return t;
        }
        MethodDefinition Method(TypeDefinition t, string name, bool instance = false, string result = "Void")
        {
            var m = new MethodDefinition(name, MethodAttributes.Public | MethodAttributes.HideBySig | (instance ? 0 : MethodAttributes.Static), Core(t.Module, result));
            if (name == ".ctor") m.Attributes |= MethodAttributes.SpecialName | MethodAttributes.RTSpecialName;
            t.Methods.Add(m);
            return m;
        }
        void Base(MethodDefinition m, string owner)
        {
            m.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0)); m.Body.Instructions.Add(Instruction.Create(OpCodes.Call, Api(m.Module, owner, ".ctor")));
        }
        void Effect(MethodDefinition m, string name)
        {
            m.Body.Instructions.Add(Instruction.Create(OpCodes.Ldstr, "Graph.Inventory" + name + "Target, GraphApp"));
            m.Body.Instructions.Add(Instruction.Create(OpCodes.Call, Api(m.Module, "Type", "GetType", "System.String")));
            m.Body.Instructions.Add(Instruction.Create(OpCodes.Call, Api(m.Module, "Activator", "CreateInstance", "System.Type")));
            m.Body.Instructions.Add(Instruction.Create(OpCodes.Pop));
        }
        PropertyDefinition Property(TypeDefinition t, string name, string effect)
        {
            var get = Method(t, "get_" + name, true, "Int32"); get.Attributes |= MethodAttributes.SpecialName;
            Effect(get, effect == "Setter" ? "Getter" : effect); get.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_0)); get.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            var set = Method(t, "set_" + name, true); set.Attributes |= MethodAttributes.SpecialName; set.Parameters.Add(new ParameterDefinition(Core(t.Module, "Int32")));
            Effect(set, effect); set.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            var p = new PropertyDefinition(name, PropertyAttributes.None, Core(t.Module, "Int32")) { GetMethod = get, SetMethod = set }; t.Properties.Add(p); return p;
        }
        var entry = Type(app.MainModule, "InventoryEntry");
        var empty = Type(app.MainModule, "InventoryEmpty");
        foreach (string name in InventoryEffects)
        {
            var t = Type(app.MainModule, "Inventory" + name + "Target"); var ctor = Method(t, ".ctor", true); Base(ctor, "Object");
            var dependency = Method(Type(lib.MainModule, "Inventory" + name + "Dependency"), "Touch"); dependency.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            ctor.Body.Instructions.Add(Instruction.Create(OpCodes.Call, app.MainModule.ImportReference(dependency))); ctor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            entry.Fields.Add(new FieldDefinition(name, FieldAttributes.Public | FieldAttributes.Static, t));
        }
        bool classIdentity = InventoryClass(mode), untouched = InventoryUntouched(mode), removed = mode.StartsWith("inventory-removed-", StringComparison.Ordinal);
        bool framework = mode.StartsWith("inventory-framework-", StringComparison.Ordinal);
        var attrModule = classIdentity || untouched ? lib.MainModule : framework ? facade.MainModule : shim.MainModule;
        var attr = Type(attrModule, classIdentity ? "InventoryOuterAttribute" : "InventoryDataAttribute"); attr.BaseType = Core(attrModule, "Attribute");
        var attrCtor = Method(attr, ".ctor", true); if (!classIdentity) attrCtor.Parameters.Add(new ParameterDefinition(Core(attrModule, "Type")));
        Base(attrCtor, "Attribute"); Effect(attrCtor, "Ctor"); attrCtor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        var value = Property(attr, "Value", "Setter");
        TypeDefinition conditional;
        ICustomAttributeProvider provider;
        TypeDefinition? owner = null;
        if (classIdentity)
        {
            conditional = Type(lib.MainModule, "InventorySiblingAttribute"); conditional.BaseType = Core(lib.MainModule, "Attribute");
            var siblingCtor = Method(conditional, ".ctor", true); Base(siblingCtor, "Attribute"); Effect(siblingCtor, "Sibling"); siblingCtor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            value.CustomAttributes.Add(new CustomAttribute(siblingCtor));
            owner = Type(lib.MainModule, "InventorySelectedProvider");
            var outerRow = new CustomAttribute(attrCtor); outerRow.Properties.Add(new CustomAttributeNamedArgument("Value", new CustomAttributeArgument(Core(lib.MainModule, "Int32"), 7)));
            owner.CustomAttributes.Add(outerRow); provider = value;
        }
        else
        {
            conditional = Type(lib.MainModule, "InventoryConditional");
            var providerModule = removed || untouched ? lib.MainModule : shim.MainModule;
            owner = Type(providerModule, "InventoryCarrier");
            var ping = Method(owner, "Ping"); ping.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            var unused = Method(owner, "Unused", false, "Int32"); unused.Parameters.Add(new ParameterDefinition(Core(providerModule, "Int32")));
            Effect(unused, "Ordinary"); unused.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_0)); unused.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            provider = owner;
            if (mode.Contains("-cctor", StringComparison.Ordinal))
            {
                var initializer = Method(owner, ".cctor"); initializer.Attributes |= MethodAttributes.SpecialName | MethodAttributes.RTSpecialName;
                Effect(initializer, "Ordinary"); initializer.Body.Instructions.Add(Instruction.Create(OpCodes.Ret)); provider = initializer;
            }
            else if (mode.Contains("-assembly", StringComparison.Ordinal)) provider = shim;
            else if (mode.Contains("-module", StringComparison.Ordinal)) provider = shim.MainModule;
            else if (mode.Contains("-method", StringComparison.Ordinal)) provider = unused;
            else if (mode.Contains("-return", StringComparison.Ordinal)) provider = unused.MethodReturnType;
            else if (mode.Contains("-parameter", StringComparison.Ordinal) && !mode.Contains("-generic-", StringComparison.Ordinal)) provider = unused.Parameters[0];
            else if (mode.Contains("-property", StringComparison.Ordinal)) provider = Property(owner, "UnusedProperty", "Ordinary");
            else if (mode.Contains("-field", StringComparison.Ordinal))
            {
                var field = new FieldDefinition("UnusedField", FieldAttributes.Public, Core(providerModule, "Int32")); owner.Fields.Add(field); provider = field;
            }
            else if (mode.Contains("-excluded-event", StringComparison.Ordinal))
            {
                var add = Method(owner, "add_UnusedEvent", true); add.Attributes |= MethodAttributes.SpecialName; add.Parameters.Add(new ParameterDefinition(Core(providerModule, "Action"))); add.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
                var remove = Method(owner, "remove_UnusedEvent", true); remove.Attributes |= MethodAttributes.SpecialName; remove.Parameters.Add(new ParameterDefinition(Core(providerModule, "Action"))); remove.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
                var e = new EventDefinition("UnusedEvent", EventAttributes.None, Core(providerModule, "Action")) { AddMethod = add, RemoveMethod = remove }; owner.Events.Add(e); provider = e;
            }
            else if (mode.Contains("-excluded-generic-parameter", StringComparison.Ordinal))
            {
                var generic = Method(owner, "UnusedGeneric"); generic.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
                var parameter = new GenericParameter("T", generic); generic.GenericParameters.Add(parameter); provider = parameter;
            }
            // Serialized Type arguments do not require an AssemblyRef to their target.
            using var stream = new MemoryStream(); using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, leaveOpen: true))
            {
                void Text(string text) { var bytes = System.Text.Encoding.UTF8.GetBytes(text); Require(bytes.Length < 128, "inventory SerString grew"); writer.Write((byte)bytes.Length); writer.Write(bytes); }
                writer.Write((ushort)1); Text("Graph.InventoryConditional, GraphLib"); writer.Write((ushort)1);
                writer.Write((byte)0x54); writer.Write((byte)0x08); Text("Value"); writer.Write(7);
            }
            provider.CustomAttributes.Add(new CustomAttribute(providerModule.ImportReference(attrCtor), stream.ToArray()));
        }
        var activate = Method(conditional, "Activate");
        var unique = Method(Type(lib.MainModule, "InventoryConditionalDependency"), "Touch"); unique.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        activate.Body.Instructions.Add(Instruction.Create(OpCodes.Call, unique)); activate.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        File.WriteAllText(Path.Combine(directory, "link.xml"), "<linker><assembly fullname=\"GraphLib\" preserve=\"nothing\"><type fullname=\"" + conditional.FullName + "\" preserve=\"nothing\" required=\"0\"><method name=\"Activate\" /></type></assembly></linker>\n");
        var carry = Method(entry, "Carry");
        if (InventoryConstruct(mode))
        {
            if (!classIdentity) carry.Body.Instructions.Add(Instruction.Create(OpCodes.Ldnull));
            carry.Body.Instructions.Add(Instruction.Create(OpCodes.Newobj, app.MainModule.ImportReference(attrCtor))); carry.Body.Instructions.Add(Instruction.Create(OpCodes.Pop));
        }
        else if (!classIdentity && !untouched && !mode.EndsWith("-no-carrier", StringComparison.Ordinal)
            && !mode.Contains("-assembly", StringComparison.Ordinal) && !mode.Contains("-module", StringComparison.Ordinal))
            carry.Body.Instructions.Add(Instruction.Create(OpCodes.Call, app.MainModule.ImportReference(owner!.Methods.Single(m => m.Name == "Ping"))));
        carry.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        var read = Method(entry, "Read");
        if (classIdentity && !mode.EndsWith("-no-read", StringComparison.Ordinal))
        {
            read.Parameters.Add(new ParameterDefinition(Core(app.MainModule, "Reflection.MemberInfo")));
            read.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            read.Body.Instructions.Add(Instruction.Create(OpCodes.Ldtoken, app.MainModule.ImportReference(attr)));
            read.Body.Instructions.Add(Instruction.Create(OpCodes.Call, Api(app.MainModule, "Type", "GetTypeFromHandle", "System.RuntimeTypeHandle")));
            read.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_0));
            read.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, Api(app.MainModule, "Reflection.MemberInfo", "IsDefined", "System.Type", "System.Boolean")));
            read.Body.Instructions.Add(Instruction.Create(OpCodes.Pop));
        }
        else if (!mode.EndsWith("-no-read", StringComparison.Ordinal))
        {
            read.Body.Instructions.Add(Instruction.Create(OpCodes.Ldtoken, empty));
            read.Body.Instructions.Add(Instruction.Create(OpCodes.Call, Api(app.MainModule, "Type", "GetTypeFromHandle", "System.RuntimeTypeHandle")));
            read.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, Api(app.MainModule, "Reflection.MemberInfo", "GetCustomAttributesData")));
            read.Body.Instructions.Add(Instruction.Create(OpCodes.Pop));
        }
        if (framework && mode.EndsWith("-named", StringComparison.Ordinal))
        {
            read.Body.Instructions.Add(Instruction.Create(OpCodes.Ldtoken, app.MainModule.ImportReference(attr)));
            read.Body.Instructions.Add(Instruction.Create(OpCodes.Call, Api(app.MainModule, "Type", "GetTypeFromHandle", "System.RuntimeTypeHandle")));
            read.Body.Instructions.Add(Instruction.Create(OpCodes.Pop));
        }
        read.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        var main = Method(entry, "Main"); app.EntryPoint = main;
        foreach (string setter in new[] { "set_CurrentCulture", "set_CurrentUICulture" })
        {
            main.Body.Instructions.Add(Instruction.Create(OpCodes.Call, Api(app.MainModule, "Globalization.CultureInfo", "get_InvariantCulture")));
            main.Body.Instructions.Add(Instruction.Create(OpCodes.Call, Api(app.MainModule, "Globalization.CultureInfo", setter, "System.Globalization.CultureInfo")));
        }
        void Read()
        {
            if (classIdentity && !mode.EndsWith("-no-read", StringComparison.Ordinal))
            {
                main.Body.Instructions.Add(Instruction.Create(OpCodes.Ldtoken, empty));
                main.Body.Instructions.Add(Instruction.Create(OpCodes.Call, Api(app.MainModule, "Type", "GetTypeFromHandle", "System.RuntimeTypeHandle")));
            }
            main.Body.Instructions.Add(Instruction.Create(OpCodes.Call, read));
        }
        bool late = mode.EndsWith("-late", StringComparison.Ordinal);
        if (late) Read();
        main.Body.Instructions.Add(Instruction.Create(OpCodes.Call, carry));
        if (!late) Read();
        main.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        foreach (var assembly in new[] { app, lib, shim, facade, leaf }) assembly.Write(Path.Combine(directory, assembly.Name.Name + ".dll"), new WriterParameters { Timestamp = 0, DeterministicMvid = true });
    }

    private static ICustomAttributeProvider? InventoryProvider(AssemblyDefinition lib, AssemblyDefinition shim, string mode)
    {
        if (InventoryClass(mode)) return lib.MainModule.GetType("Graph.InventoryOuterAttribute")?.Properties.SingleOrDefault(p => p.Name == "Value");
        if (mode.Contains("-assembly", StringComparison.Ordinal)) return shim;
        if (mode.Contains("-module", StringComparison.Ordinal)) return shim.MainModule;
        var module = InventoryUntouched(mode) || mode.StartsWith("inventory-removed-", StringComparison.Ordinal) ? lib.MainModule : shim.MainModule;
        var owner = module.GetType("Graph.InventoryCarrier");
        if (owner is null) return null;
        var method = owner.Methods.SingleOrDefault(m => m.Name == "Unused");
        if (mode.Contains("-cctor", StringComparison.Ordinal)) return owner.Methods.SingleOrDefault(m => m.Name == ".cctor");
        if (mode.Contains("-method", StringComparison.Ordinal)) return method;
        if (mode.Contains("-return", StringComparison.Ordinal)) return method?.MethodReturnType;
        if (mode.Contains("-parameter", StringComparison.Ordinal) && !mode.Contains("-generic-", StringComparison.Ordinal)) return method?.Parameters.SingleOrDefault();
        if (mode.Contains("-property", StringComparison.Ordinal)) return owner.Properties.SingleOrDefault(p => p.Name == "UnusedProperty");
        if (mode.Contains("-field", StringComparison.Ordinal)) return owner.Fields.SingleOrDefault(f => f.Name == "UnusedField");
        if (mode.Contains("-excluded-event", StringComparison.Ordinal)) return owner.Events.SingleOrDefault();
        if (mode.Contains("-excluded-generic-parameter", StringComparison.Ordinal)) return owner.Methods.SingleOrDefault(m => m.Name == "UnusedGeneric")?.GenericParameters.SingleOrDefault();
        return owner;
    }

    private static void CheckAttributeInventory(string directory, string mode, string state)
    {
        Require(state is "original" or "published" or "observed-before", "unknown inventory fixture state");
        using var app = AssemblyDefinition.ReadAssembly(Path.Combine(directory, "GraphApp.dll"));
        using var lib = AssemblyDefinition.ReadAssembly(Path.Combine(directory, "GraphLib.dll"));
        using var shim = AssemblyDefinition.ReadAssembly(Path.Combine(directory, "DnZlib.dll"));
        using var facade = AssemblyDefinition.ReadAssembly(Path.Combine(directory, "System.ArrayFacade.dll"));
        bool original = state == "original", observed = state == "observed-before", classIdentity = InventoryClass(mode);
        bool Body(MethodDefinition? m) => m is { HasBody: true } && !Stub(m);
        bool ExactStub(MethodDefinition? m) => m is not null && Stub(m) && m.Body.Instructions[0].Operand is int marker && marker == 0x494C4454;
        foreach (var copied in new[] { shim, facade })
            Require(copied.MainModule.AssemblyReferences.All(r => r.Name is "System.Private.CoreLib" or "System.ArrayFacade"), "inventory copied input gained a stripped assembly reference");
        var effects = new SortedDictionary<string, bool>();
        foreach (string name in InventoryEffects)
        {
            var ctor = app.MainModule.GetType("Graph.Inventory" + name + "Target")?.Methods.SingleOrDefault(m => m.IsConstructor);
            Require(ctor is not null, "inventory effect metadata disappeared");
            bool body = Body(ctor), dependency = Body(lib.MainModule.GetType("Graph.Inventory" + name + "Dependency")?.Methods.SingleOrDefault());
            Require(body == dependency, "inventory effect and unique dependency disagree: " + name);
            if (!body) Require(ExactStub(ctor) && lib.MainModule.GetType("Graph.Inventory" + name + "Dependency") is null, "inventory effect lost exact stub/absent dependency: " + name);
            Require(original || observed || body == (name == "Ctor" && InventoryConstruct(mode)), "inventory constructed incidental attribute/accessor: " + name);
            effects[name] = body;
        }
        var target = lib.MainModule.GetType("Graph." + (classIdentity ? "InventorySiblingAttribute" : "InventoryConditional"));
        bool active = Body(target?.Methods.SingleOrDefault(m => m.Name == "Activate"));
        Require(active == Body(lib.MainModule.GetType("Graph.InventoryConditionalDependency")?.Methods.SingleOrDefault()), "inventory conditional body lost its dependency");
        Require(original || observed || active == InventoryActive(mode), "inventory conditional admission changed");
        var provider = InventoryProvider(lib, shim, mode);
        string attributeName = "Graph." + (classIdentity ? "InventorySiblingAttribute" : "InventoryDataAttribute");
        var row = provider?.CustomAttributes.SingleOrDefault(a => a.AttributeType.FullName == attributeName);
        bool metadata = row is not null;
        if (metadata)
        {
            if (classIdentity)
                Require(row!.ConstructorArguments.Count == 0 && row.Properties.Count == 0 && row.Fields.Count == 0, "attribute-class identity gained an argument target");
            else
            {
                Require(row!.ConstructorArguments.Count == 1 && row.ConstructorArguments[0].Value is TypeReference t
                    && t.FullName == "Graph.InventoryConditional" && t.Scope.Name == "GraphLib", "serialized inventory Type identity changed");
                Require(row.Properties.Count == 1 && row.Properties[0].Name == "Value" && row.Properties[0].Argument.Type.FullName == "System.Int32"
                    && row.Properties[0].Argument.Value is int number && number == 7, "inventory named row changed");
            }
        }
        var attrModule = classIdentity || InventoryUntouched(mode) ? lib.MainModule : mode.StartsWith("inventory-framework-", StringComparison.Ordinal) ? facade.MainModule : shim.MainModule;
        var attr = attrModule.GetType("Graph." + (classIdentity ? "InventoryOuterAttribute" : "InventoryDataAttribute"));
        var attrCtor = attr?.Methods.SingleOrDefault(m => m.IsConstructor);
        bool attributesPresent = attrCtor is not null && attr?.Properties.SingleOrDefault(p => p.Name == "Value") is { GetMethod: not null, SetMethod: not null };
        if (classIdentity) attributesPresent &= target?.Methods.SingleOrDefault(m => m.IsConstructor) is not null;
        if (original || !observed && InventoryActive(mode)) Require(metadata && attributesPresent, "inventory lost original provider/member/attribute evidence");
        if (!original && !observed && mode == "inventory-class-no-carrier")
        {
            Require(attr is { Methods.Count: 0, Properties.Count: 0 }, "inventory filter-only type lost identity or retained members");
            Require(lib.MainModule.GetType("Graph.InventorySelectedProvider") is null && target is null,
                "inventory filter-only type retained an unused provider or sibling attribute");
        }
        else if (!original && !observed && attrModule == lib.MainModule && attr is not null)
        {
            Require(InventoryConstruct(mode) ? Body(attrCtor) : ExactStub(attrCtor), "inventory attribute constructor execution changed");
            foreach (var p in attr.Properties) Require(ExactStub(p.GetMethod) && ExactStub(p.SetMethod), "inventory attribute accessor executed");
            if (classIdentity && target is not null) Require(ExactStub(target.Methods.SingleOrDefault(m => m.IsConstructor)), "zero-argument sibling constructor executed");
        }
        if (!original && !observed && mode.StartsWith("inventory-removed-", StringComparison.Ordinal))
        {
            var owner = lib.MainModule.GetType("Graph.InventoryCarrier");
            Require(owner is not null && Body(owner.Methods.SingleOrDefault(m => m.Name == "Ping")), "inventory carried owner disappeared");
            foreach (var m in owner!.Methods.Where(m => m.Name != "Ping")) Require(ExactStub(m), "inventory metadata evidence acquired a member body");
            if (mode.Contains("-method", StringComparison.Ordinal) || mode.Contains("-return", StringComparison.Ordinal) || mode.Contains("-parameter", StringComparison.Ordinal))
            {
                var method = owner.Methods.SingleOrDefault(m => m.Name == "Unused");
                Require(method is not null && method.ReturnType.FullName == "System.Int32" && method.Parameters.Count == 1 && method.Parameters[0].ParameterType.FullName == "System.Int32", "inventory method/return/parameter signature changed");
            }
            if (provider is PropertyDefinition property) Require(property.PropertyType.FullName == "System.Int32" && property.GetMethod is not null && property.SetMethod is not null, "inventory property accessor identity disappeared");
        }
        if (!original && !observed && mode.Contains("-cctor", StringComparison.Ordinal) && InventoryActive(mode))
            Require(provider is MethodDefinition initializer && initializer.IsConstructor && initializer.IsStatic && ExactStub(initializer), "admitted initializer row needs metadata without execution");
        if (original) CheckInventoryInput(app, lib, shim, mode);
        Console.WriteLine("graph-inventory-observed=" + System.Text.Json.JsonSerializer.Serialize(new { effects, active, metadata, attributesPresent }));
        Console.WriteLine("graph-copied-array=" + state + ":" + mode + ":original-provider-evidence,conditional-body,unique-dependency,attribute-nonexecution");
    }

    private static void CheckInventoryInput(AssemblyDefinition app, AssemblyDefinition lib, AssemblyDefinition shim, string mode)
    {
        bool cls = InventoryClass(mode), untouched = InventoryUntouched(mode);
        var entry = app.MainModule.GetType("Graph.InventoryEntry");
        var main = app.EntryPoint;
        Require(main.DeclaringType == entry && main.Name == "Main", "inventory entry changed");
        Require(main.Body.Instructions.Take(4).All(i => i.OpCode == OpCodes.Call)
            && main.Body.Instructions.Take(4).Select(i => ((MethodReference)i.Operand).Name).SequenceEqual(new[] { "get_InvariantCulture", "set_CurrentCulture", "get_InvariantCulture", "set_CurrentUICulture" }), "inventory cultures are not pinned");
        var helpers = main.Body.Instructions.Skip(4).Where(i => i.OpCode == OpCodes.Call && i.Operand is MethodReference m && m.DeclaringType.FullName == entry.FullName).Select(i => ((MethodReference)i.Operand).Name);
        Require(helpers.SequenceEqual(mode.EndsWith("-late", StringComparison.Ordinal) ? new[] { "Read", "Carry" } : new[] { "Carry", "Read" }), "inventory helper evidence order changed");
        var carry = entry.Methods.Single(m => m.Name == "Carry");
        var expectedCarry = InventoryConstruct(mode) ? (cls ? new[] { Code.Newobj, Code.Pop, Code.Ret } : new[] { Code.Ldnull, Code.Newobj, Code.Pop, Code.Ret })
            : cls || untouched || mode.EndsWith("-no-carrier", StringComparison.Ordinal) || mode.Contains("-assembly", StringComparison.Ordinal) || mode.Contains("-module", StringComparison.Ordinal)
                ? new[] { Code.Ret } : new[] { Code.Call, Code.Ret };
        Require(carry.Body.Instructions.Select(i => i.OpCode.Code).SequenceEqual(expectedCarry), "inventory carrier acquired an extra root");
        var read = entry.Methods.Single(m => m.Name == "Read");
        var readCalls = read.Body.Instructions.Where(i => i.Operand is MethodReference).Select(i => ((MethodReference)i.Operand).Name).ToArray();
        var expectedRead = mode.EndsWith("-no-read", StringComparison.Ordinal) ? Array.Empty<string>() : cls ? new[] { "GetTypeFromHandle", "IsDefined" }
            : mode == "inventory-framework-named" ? new[] { "GetTypeFromHandle", "GetCustomAttributesData", "GetTypeFromHandle" } : new[] { "GetTypeFromHandle", "GetCustomAttributesData" };
        Require(readCalls.SequenceEqual(expectedRead), "inventory attribute capability API changed");
        if (!cls)
        {
            Require(read.Parameters.Count == 0, "inventory read acquired an unknown owner");
            foreach (var token in read.Body.Instructions.Where(i => i.OpCode == OpCodes.Ldtoken))
                Require(token.Operand is TypeReference type && (type.FullName == "Graph.InventoryEmpty" || mode == "inventory-framework-named" && type.FullName == "Graph.InventoryDataAttribute"), "inventory query selected its witness owner");
        }
        foreach (var m in app.MainModule.GetTypes().SelectMany(t => t.Methods).Where(m => m.HasBody))
            foreach (var i in m.Body.Instructions)
            {
                Require(i.OpCode != OpCodes.Box && i.OpCode != OpCodes.Initobj, "inventory gained a value construction root");
                if (i.OpCode == OpCodes.Newobj) Require(m == carry && InventoryConstruct(mode) && i.Operand is MethodReference ctor
                    && ctor.DeclaringType.FullName == "Graph." + (cls ? "InventoryOuterAttribute" : "InventoryDataAttribute"), "inventory gained a direct construction root");
                if (i.Operand is MethodReference called)
                    Require(called.Name != "Activate" && called.Name != "Unused" && !called.Name.StartsWith("get_Value", StringComparison.Ordinal)
                        && !called.Name.StartsWith("set_Value", StringComparison.Ordinal), "inventory directly reached a witness method");
                if (i.Operand is TypeReference t) Require(t.FullName != "Graph.InventoryConditional" && t.FullName != "Graph.InventorySiblingAttribute" && t.FullName != "Graph.InventoryCarrier", "inventory gained a witness type token");
            }
        Require(app.MainModule.GetTypes().All(t => !t.HasCustomAttributes), "inventory gained an application attribute root");
        var conditional = lib.MainModule.GetType("Graph." + (cls ? "InventorySiblingAttribute" : "InventoryConditional"));
        var activation = conditional.Methods.Single(m => m.Name == "Activate");
        Require(activation.IsStatic && activation.Parameters.Count == 0 && activation.ReturnType.FullName == "System.Void"
            && activation.Body.Instructions.Select(i => i.OpCode.Code).SequenceEqual(new[] { Code.Call, Code.Ret })
            && activation.Body.Instructions[0].Operand is MethodReference dependency
            && dependency.DeclaringType.FullName == "Graph.InventoryConditionalDependency" && dependency.Name == "Touch", "inventory conditional method acquired another root or lost its unique dependency");
        if (!cls)
        {
            var ownerModule = untouched || mode.StartsWith("inventory-removed-", StringComparison.Ordinal) ? lib.MainModule : shim.MainModule;
            var ping = ownerModule.GetType("Graph.InventoryCarrier").Methods.Single(m => m.Name == "Ping");
            Require(ping.Body.Instructions.Select(i => i.OpCode.Code).SequenceEqual(new[] { Code.Ret }), "inventory carrier method gained effects");
        }
        if (untouched)
        {
            Require(app.MainModule.GetTypeReferences().All(t => t.FullName != "Graph.InventoryCarrier"), "untouched owner gained an application TypeRef");
            Require(lib.MainModule.GetType("Graph.InventoryCarrier").GenericParameters.Count == 0, "untouched owner stopped being originally modeled");
            Require(app.MainModule.GetTypes().SelectMany(t => t.Fields).All(f => f.FieldType.FullName != "Graph.InventoryCarrier"), "untouched owner gained field metadata");
            Require(app.MainModule.GetTypes().SelectMany(t => t.Methods).All(m => m.ReturnType.FullName != "Graph.InventoryCarrier" && m.Parameters.All(p => p.ParameterType.FullName != "Graph.InventoryCarrier")
                && (!m.HasBody || m.Body.Instructions.All(i => i.Operand is not MethodReference mr || mr.DeclaringType.FullName != "Graph.InventoryCarrier"))), "untouched owner gained a member/signature root");
        }
    }
}
