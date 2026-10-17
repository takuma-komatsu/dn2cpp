using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Runtime.Loader;

static partial class GraphArrayValidation
{
    private static bool SelectedLayoutMode(string mode) => mode.StartsWith("selected-", StringComparison.Ordinal);
    private static bool SelectedConservativePublication(string mode) => mode is "selected-cs-data" or "selected-cs-instance" or "selected-cs-upgrade";
    private static bool SelectedDispatch(string mode) => mode.StartsWith("selected-layout-", StringComparison.Ordinal);
    private static bool SelectedNamed(string mode) => mode.StartsWith("selected-named-", StringComparison.Ordinal);
    private static bool SelectedIndependent(string mode) => SelectedNamed(mode) && !mode.EndsWith("-identity-passive", StringComparison.Ordinal);
    private static bool SelectedNestedIdentity(string mode) => mode.Contains("-identity-", StringComparison.Ordinal);
    private static bool SelectedKnownOwner(string mode) => mode.Contains("-known-", StringComparison.Ordinal);
    private static bool SelectedPreserved(string mode) => mode.Contains("-preserved-", StringComparison.Ordinal);
    private static bool SelectedConstruct(string mode) => SelectedPreserved(mode) || SelectedKnownOwner(mode)
        && (mode.EndsWith("-instance", StringComparison.Ordinal) || mode.EndsWith("-upgrade", StringComparison.Ordinal));
    private static bool SelectedQuery(string mode) => !mode.EndsWith("-noquery", StringComparison.Ordinal);
    private static bool SelectedMatch(string mode) => SelectedQuery(mode) && !mode.EndsWith("-unmatched", StringComparison.Ordinal);
    private static bool SelectedConditional(string mode) => mode.EndsWith("-identity-carried", StringComparison.Ordinal)
        || !SelectedDispatch(mode) && !SelectedNamed(mode) && (SelectedKnownOwner(mode) || SelectedPreserved(mode) || SelectedConservativePublication(mode)) && SelectedMatch(mode) && !mode.EndsWith("-inactive", StringComparison.Ordinal);
    private static bool SelectedDispatchActive(string mode) => SelectedDispatch(mode) && !mode.EndsWith("-nocap", StringComparison.Ordinal)
        && !mode.EndsWith("-nolayout", StringComparison.Ordinal) && !mode.EndsWith("-nofactory", StringComparison.Ordinal);
    private static readonly string[] SelectedEffects = { "Dispatch", "Ctor", "Setter", "SecondSetter", "Getter", "Sibling", "Ordinary", "Conditional" };

    private static void CreateSelectedLayout(string directory, string corelib, string mode)
    {
        Directory.CreateDirectory(directory);
        using var core = AssemblyDefinition.ReadAssembly(corelib);
        using var app = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition("GraphApp", new Version(1, 0)), "GraphApp", ModuleKind.Console);
        using var library = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition("GraphLib", new Version(1, 0)), "GraphLib", ModuleKind.Dll);
        using var facade = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition("System.ArrayFacade", new Version(1, 0)), "System.ArrayFacade", ModuleKind.Dll);
        using var leaf = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition("System.ArrayLeaf", new Version(1, 0)), "System.ArrayLeaf", ModuleKind.Dll);
        using var shim = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition("DnZlib", new Version(1, 0)), "DnZlib", ModuleKind.Dll);
        TypeReference Core(ModuleDefinition module, string name) => module.ImportReference(core.MainModule.GetType("System." + name));
        MethodReference Api(ModuleDefinition module, string owner, string name, params string[] parameters) => module.ImportReference(core.MainModule.GetType("System." + owner).Methods.Single(m => m.Name == name && !m.HasGenericParameters
            && m.Parameters.Select(p => p.ParameterType.FullName).SequenceEqual(parameters)));
        TypeDefinition Type(ModuleDefinition module, string name, bool value = false)
        {
            var type = new TypeDefinition("Graph", name, TypeAttributes.Public | (value ? TypeAttributes.Sealed | TypeAttributes.SequentialLayout : TypeAttributes.Class), Core(module, value ? "ValueType" : "Object"));
            module.Types.Add(type);
            return type;
        }
        MethodDefinition Method(TypeDefinition owner, string name, bool instance = false, string result = "Void")
        {
            var method = new MethodDefinition(name, MethodAttributes.Public | MethodAttributes.HideBySig | (instance ? 0 : MethodAttributes.Static), Core(owner.Module, result));
            if (name == ".ctor") method.Attributes |= MethodAttributes.SpecialName | MethodAttributes.RTSpecialName;
            owner.Methods.Add(method);
            return method;
        }
        void Base(MethodDefinition method, MethodReference constructor)
        {
            method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            method.Body.Instructions.Add(Instruction.Create(OpCodes.Call, method.Module.ImportReference(constructor)));
        }
        void Effect(MethodDefinition method, string name)
        {
            method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldstr, "Graph.Selected" + name + "Target, GraphApp"));
            method.Body.Instructions.Add(Instruction.Create(OpCodes.Call, Api(method.Module, "Type", "GetType", "System.String")));
            method.Body.Instructions.Add(Instruction.Create(OpCodes.Call, Api(method.Module, "Activator", "CreateInstance", "System.Type")));
            method.Body.Instructions.Add(Instruction.Create(OpCodes.Pop));
        }
        var entry = Type(app.MainModule, "SelectedEntry");
        foreach (string name in SelectedEffects)
        {
            var target = Type(app.MainModule, "Selected" + name + "Target");
            var ctor = Method(target, ".ctor", true);
            Base(ctor, Api(app.MainModule, "Object", ".ctor"));
            var dependency = Method(Type(library.MainModule, "Selected" + name + "Dependency"), "Touch");
            dependency.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            ctor.Body.Instructions.Add(Instruction.Create(OpCodes.Call, app.MainModule.ImportReference(dependency)));
            ctor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            entry.Fields.Add(new FieldDefinition(name, FieldAttributes.Public | FieldAttributes.Static, target));
        }
        var main = Method(entry, "Main");
        app.EntryPoint = main;
        foreach (string setter in new[] { "set_CurrentCulture", "set_CurrentUICulture" })
        {
            main.Body.Instructions.Add(Instruction.Create(OpCodes.Call, Api(app.MainModule, "Globalization.CultureInfo", "get_InvariantCulture")));
            main.Body.Instructions.Add(Instruction.Create(OpCodes.Call, Api(app.MainModule, "Globalization.CultureInfo", setter, "System.Globalization.CultureInfo")));
        }
        if (SelectedDispatch(mode))
        {
            bool box = mode.Contains("-box-", StringComparison.Ordinal);
            var payload = Type(shim.MainModule, "SelectedPayload", true);
            payload.Fields.Add(new FieldDefinition("Number", FieldAttributes.Public, Core(shim.MainModule, "Int32")));
            var dispatch = Method(payload, box ? "ToString" : "Equals", true, box ? "String" : "Boolean");
            dispatch.Attributes |= MethodAttributes.Virtual;
            if (!box) dispatch.Parameters.Add(new ParameterDefinition(Core(shim.MainModule, "Object")));
            Effect(dispatch, "Dispatch");
            dispatch.Body.Instructions.Add(box ? Instruction.Create(OpCodes.Ldstr, "selected-payload") : Instruction.Create(OpCodes.Ldc_I4_0));
            dispatch.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            var ordinary = Method(payload, "Ordinary", true);
            Effect(ordinary, "Ordinary"); ordinary.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            var holder = Type(app.MainModule, "SelectedHolder");
            if (!mode.EndsWith("-nolayout", StringComparison.Ordinal))
                holder.Fields.Add(new FieldDefinition("Payload", FieldAttributes.Public | FieldAttributes.Static, app.MainModule.ImportReference(payload)));
            var touch = Method(holder, "Touch"); touch.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            var observe = Method(entry, "Observe");
            observe.Body.Instructions.Add(Instruction.Create(OpCodes.Call, touch));
            observe.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            var capability = Method(entry, "Capability");
            if (!mode.EndsWith("-nocap", StringComparison.Ordinal))
            {
                if (box)
                {
                    capability.Body.InitLocals = true;
                    capability.Body.Variables.Add(new VariableDefinition(Core(app.MainModule, "Byte")));
                    capability.Body.Instructions.Add(Instruction.Create(OpCodes.Ldloca, capability.Body.Variables[0]));
                    capability.Body.Instructions.Add(Instruction.Create(OpCodes.Ldtoken, Core(app.MainModule, "Byte")));
                    capability.Body.Instructions.Add(Instruction.Create(OpCodes.Call, Api(app.MainModule, "Runtime.CompilerServices.RuntimeHelpers", "Box", "System.Byte&", "System.RuntimeTypeHandle")));
                }
                else
                {
                    if (mode.EndsWith("-nofactory", StringComparison.Ordinal))
                    {
                        capability.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_1));
                        capability.Body.Instructions.Add(Instruction.Create(OpCodes.Newarr, Core(app.MainModule, "Int32")));
                    }
                    else
                    {
                        capability.Body.Instructions.Add(Instruction.Create(OpCodes.Ldtoken, Core(app.MainModule, "Int32")));
                        capability.Body.Instructions.Add(Instruction.Create(OpCodes.Call, Api(app.MainModule, "Type", "GetTypeFromHandle", "System.RuntimeTypeHandle")));
                        capability.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_1));
                        capability.Body.Instructions.Add(Instruction.Create(OpCodes.Call, Api(app.MainModule, "Array", "CreateInstance", "System.Type", "System.Int32")));
                    }
                    capability.Body.Instructions.Add(Instruction.Create(OpCodes.Ldnull));
                    capability.Body.Instructions.Add(Instruction.Create(OpCodes.Call, Api(app.MainModule, "Array", "IndexOf", "System.Array", "System.Object")));
                }
                capability.Body.Instructions.Add(Instruction.Create(OpCodes.Pop));
            }
            capability.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            bool late = mode.EndsWith("-late", StringComparison.Ordinal);
            main.Body.Instructions.Add(Instruction.Create(OpCodes.Call, late ? capability : observe));
            main.Body.Instructions.Add(Instruction.Create(OpCodes.Call, late ? observe : capability));
        }
        else
        {
            bool named = SelectedNamed(mode), inherited = mode.Contains("-inherited", StringComparison.Ordinal), field = mode.Contains("-field", StringComparison.Ordinal);
            string ownership = named ? "ss" : mode.Split('-')[1];
            var attributeModule = ownership[1] == 'c' ? shim.MainModule : library.MainModule;
            var providerModule = ownership[0] == 'c' ? shim.MainModule : library.MainModule;
            var attribute = Type(attributeModule, "SelectedSpecialAttribute");
            attribute.BaseType = Core(attributeModule, "Attribute");
            MethodReference baseCtor = Api(attributeModule, "Attribute", ".ctor");
            var propertyOwner = attribute;
            if (inherited)
            {
                propertyOwner = Type(attributeModule, "SelectedBaseAttribute");
                propertyOwner.BaseType = Core(attributeModule, "Attribute");
                var parentCtor = Method(propertyOwner, ".ctor", true); Base(parentCtor, baseCtor); parentCtor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
                attribute.BaseType = propertyOwner; baseCtor = parentCtor;
            }
            var constructor = Method(attribute, ".ctor", true);
            if (!named) constructor.Parameters.Add(new ParameterDefinition(Core(attributeModule, "Type")));
            Base(constructor, baseCtor); Effect(constructor, "Ctor"); constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            void Property(TypeDefinition owner, string name, string effect)
            {
                var setter = Method(owner, "set_" + name, true); setter.Attributes |= MethodAttributes.SpecialName;
                setter.Parameters.Add(new ParameterDefinition(Core(attributeModule, "Object")));
                Effect(setter, effect); setter.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
                var getter = Method(owner, "get_" + name, true, "Object"); getter.Attributes |= MethodAttributes.SpecialName;
                Effect(getter, "Getter"); getter.Body.Instructions.Add(Instruction.Create(OpCodes.Ldnull)); getter.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
                owner.Properties.Add(new PropertyDefinition(name, PropertyAttributes.None, Core(attributeModule, "Object")) { GetMethod = getter, SetMethod = setter });
            }
            if (field) propertyOwner.Fields.Add(new FieldDefinition("Value", FieldAttributes.Public, Core(attributeModule, "Object")));
            else Property(propertyOwner, "Value", "Setter");
            if (named) Property(attribute, "Second", "SecondSetter");
            MethodDefinition Attribute(string name, string effect)
            {
                var type = Type(attributeModule, name); type.BaseType = Core(attributeModule, "Attribute");
                var ctor = Method(type, ".ctor", true); Base(ctor, Api(attributeModule, "Attribute", ".ctor")); Effect(ctor, effect); ctor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
                return ctor;
            }
            var sibling = Attribute("SelectedSiblingAttribute", "Sibling");
            if (SelectedNestedIdentity(mode)) sibling.Parameters.Add(new ParameterDefinition(Core(attributeModule, "Type")));
            var unmatched = Attribute("SelectedUnmatchedAttribute", "Sibling");
            if (mode.EndsWith("-nested", StringComparison.Ordinal))
                foreach (var owner in new[] { propertyOwner, attribute }.Distinct())
                    foreach (var property in owner.Properties)
                    {
                        property.CustomAttributes.Add(new CustomAttribute(sibling));
                        foreach (var accessor in new[] { property.GetMethod, property.SetMethod })
                        {
                            accessor.CustomAttributes.Add(new CustomAttribute(sibling));
                            accessor.MethodReturnType.CustomAttributes.Add(new CustomAttribute(sibling));
                            foreach (var parameter in accessor.Parameters) parameter.CustomAttributes.Add(new CustomAttribute(sibling));
                        }
                    }
            var provider = Type(providerModule, "SelectedProvider");
            var row = new CustomAttribute(providerModule.ImportReference(constructor));
            if (!named || SelectedNestedIdentity(mode))
            {
                var conditional = Type(shim.MainModule, "SelectedConditional");
                var activate = Method(conditional, "Activate"); Effect(activate, "Conditional"); activate.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
                var inert = Type(shim.MainModule, "SelectedInert");
                if (!named) row.ConstructorArguments.Add(new CustomAttributeArgument(Core(providerModule, "Type"), providerModule.ImportReference(mode.EndsWith("-inactive", StringComparison.Ordinal) ? inert : conditional)));
                else
                {
                    var nested = new CustomAttribute(sibling);
                    nested.ConstructorArguments.Add(new CustomAttributeArgument(Core(attributeModule, "Type"), attributeModule.ImportReference(conditional)));
                    propertyOwner.Properties.Single(p => p.Name == "Value").CustomAttributes.Add(nested);
                }
                File.WriteAllText(Path.Combine(directory, "link.xml"), "<linker><assembly fullname=\"DnZlib\" preserve=\"nothing\"><type fullname=\"Graph.SelectedConditional\" preserve=\"nothing\" required=\"0\"><method name=\"Activate\" /></type></assembly></linker>\n");
            }
            if (SelectedPreserved(mode))
            {
                string path = Path.Combine(directory, "link.xml");
                string rules = "<assembly fullname=\"GraphLib\" preserve=\"nothing\"><type fullname=\"Graph.SelectedSpecialAttribute\" preserve=\"nothing\"><method name=\".ctor\" /><method name=\"set_Value\" /></type></assembly>";
                File.WriteAllText(path, File.ReadAllText(path).Replace("</linker>", rules + "</linker>", StringComparison.Ordinal));
            }
            if (named) row.Properties.Add(new CustomAttributeNamedArgument("Second", new CustomAttributeArgument(Core(providerModule, "Object"), new CustomAttributeArgument(Core(providerModule, "String"), "second"))));
            var value = new CustomAttributeNamedArgument("Value", new CustomAttributeArgument(Core(providerModule, "Object"), new CustomAttributeArgument(Core(providerModule, "Int32"), 7)));
            if (field) row.Fields.Add(value); else row.Properties.Add(value);
            provider.CustomAttributes.Add(row);
            if (!named) provider.CustomAttributes.Add(new CustomAttribute(providerModule.ImportReference(sibling)));
            var read = Method(entry, "Read"); read.Parameters.Add(new ParameterDefinition(Core(app.MainModule, "Reflection.MemberInfo")));
            void Read(bool construct)
            {
                if (SelectedKnownOwner(mode))
                {
                    read.Body.Instructions.Add(Instruction.Create(OpCodes.Ldtoken, app.MainModule.ImportReference(provider)));
                    read.Body.Instructions.Add(Instruction.Create(OpCodes.Call, Api(app.MainModule, "Type", "GetTypeFromHandle", "System.RuntimeTypeHandle")));
                }
                else read.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
                read.Body.Instructions.Add(Instruction.Create(OpCodes.Ldtoken, app.MainModule.ImportReference(mode.EndsWith("-unmatched", StringComparison.Ordinal) ? unmatched.DeclaringType : attribute)));
                read.Body.Instructions.Add(Instruction.Create(OpCodes.Call, Api(app.MainModule, "Type", "GetTypeFromHandle", "System.RuntimeTypeHandle")));
                read.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_0));
                read.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, Api(app.MainModule, "Reflection.MemberInfo", construct ? "GetCustomAttributes" : "IsDefined", "System.Type", "System.Boolean")));
                read.Body.Instructions.Add(Instruction.Create(OpCodes.Pop));
            }
            if (SelectedQuery(mode)) Read(mode.EndsWith("-instance", StringComparison.Ordinal));
            if (mode.EndsWith("-upgrade", StringComparison.Ordinal)) Read(true);
            read.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            if (SelectedIndependent(mode))
            {
                var construct = Method(entry, "Construct"); construct.Body.Instructions.Add(Instruction.Create(OpCodes.Newobj, app.MainModule.ImportReference(constructor)));
                construct.Body.Instructions.Add(Instruction.Create(OpCodes.Pop)); construct.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
                main.Body.Instructions.Add(Instruction.Create(OpCodes.Call, construct));
            }
            main.Body.Instructions.Add(Instruction.Create(OpCodes.Ldtoken, entry));
            main.Body.Instructions.Add(Instruction.Create(OpCodes.Call, Api(app.MainModule, "Type", "GetTypeFromHandle", "System.RuntimeTypeHandle")));
            main.Body.Instructions.Add(Instruction.Create(OpCodes.Call, read));
        }
        main.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        foreach (var assembly in new[] { app, library, facade, leaf, shim }) assembly.Write(Path.Combine(directory, assembly.Name.Name + ".dll"), new WriterParameters { Timestamp = 0, DeterministicMvid = true });
    }

    private static void CheckSelectedLayout(string directory, string mode, string state)
    {
        Require(state is "original" or "published" or "observed-before", "unknown selected fixture state");
        using var app = AssemblyDefinition.ReadAssembly(Path.Combine(directory, "GraphApp.dll"));
        using var library = AssemblyDefinition.ReadAssembly(Path.Combine(directory, "GraphLib.dll"));
        using var shim = AssemblyDefinition.ReadAssembly(Path.Combine(directory, "DnZlib.dll"));
        bool original = state == "original", observed = state == "observed-before", layout = SelectedDispatch(mode), named = SelectedNamed(mode);
        bool conservative = SelectedConservativePublication(mode);
        // Known owner tokens retain ordinary attribute IL. Explicit constructor
        // preservation reaches sibling rows but leaves the selected getter unrooted.
        bool physicalGetter = conservative || SelectedKnownOwner(mode);
        bool physicalSibling = conservative || !named && (SelectedKnownOwner(mode) || SelectedPreserved(mode));
        bool Body(MethodDefinition? method) => method is { HasBody: true } && !Stub(method);
        bool ExactStub(MethodDefinition? method) => method is not null && Stub(method) && method.Body.Instructions[0].Operand is int marker && marker == 0x494C4454;
        var effects = new SortedDictionary<string, bool>();
        foreach (string name in SelectedEffects)
        {
            var target = app.MainModule.GetType("Graph.Selected" + name + "Target");
            var ctor = target?.Methods.SingleOrDefault(m => m.IsConstructor);
            Require(ctor is not null && target!.BaseType.FullName == "System.Object", "selected effect target lost reference metadata");
            bool body = Body(ctor), dependency = Body(library.MainModule.GetType("Graph.Selected" + name + "Dependency")?.Methods.SingleOrDefault());
            Require(body == dependency, "selected effect retained only half its unique dependency: " + name);
            if (!body) Require(ExactStub(ctor) && library.MainModule.GetType("Graph.Selected" + name + "Dependency") is null, "selected effect missing exact stub/absent dependency: " + name);
            bool expected = original || (name switch
            {
                "Dispatch" => SelectedDispatchActive(mode),
                "Ctor" => conservative || !layout && (SelectedIndependent(mode) || SelectedConstruct(mode) && SelectedMatch(mode)),
                "Setter" => conservative || !layout && SelectedConstruct(mode) && SelectedMatch(mode) && !mode.Contains("-field", StringComparison.Ordinal),
                "SecondSetter" => named && SelectedConstruct(mode) && SelectedMatch(mode),
                "Conditional" => SelectedConditional(mode),
                "Getter" => physicalGetter, "Sibling" => physicalSibling,
                _ => false,
            });
            Require(observed || body == expected, "selected effect execution differs: " + state + ":" + mode + ":" + name);
            effects[name] = body;
        }
        bool metadata = true;
        string? identity = null;
        if (layout)
        {
            Require(shim.MainModule.AssemblyReferences.All(r => r.Name == "System.Private.CoreLib"), "field-only copied payload gained a stripped reference");
            Require(!File.Exists(Path.Combine(directory, "link.xml")), "field-only payload gained preservation roots");
            if (original) CheckSelectedLayoutInput(app, shim, mode);
        }
        else
        {
            string ownership = named ? "ss" : mode.Split('-')[1];
            var attributeModule = ownership[1] == 'c' ? shim.MainModule : library.MainModule;
            var providerModule = ownership[0] == 'c' ? shim.MainModule : library.MainModule;
            var provider = providerModule.GetType("Graph.SelectedProvider");
            bool rowRequired = original || SelectedKnownOwner(mode) || SelectedIndependent(mode) || SelectedPreserved(mode) || ownership[0] == 'c';
            if (rowRequired)
            {
                Require(provider is not null, "selected attribute provider row disappeared");
                var row = provider!.CustomAttributes.Single(a => a.AttributeType.FullName == "Graph.SelectedSpecialAttribute");
                Require(provider.CustomAttributes.Count == (named ? 1 : 2), "selected provider lost an unmatched sibling row");
                var attribute = attributeModule.GetType("Graph.SelectedSpecialAttribute");
                var constructor = attribute?.Methods.SingleOrDefault(m => m.IsConstructor);
                metadata = constructor is not null && row.Constructor.Parameters.Count == (named ? 0 : 1);
                bool inherited = mode.Contains("-inherited", StringComparison.Ordinal), field = mode.Contains("-field", StringComparison.Ordinal);
                var owner = attributeModule.GetType("Graph." + (inherited ? "SelectedBaseAttribute" : "SelectedSpecialAttribute"));
                if (field) metadata &= owner?.Fields.Any(f => f.Name == "Value" && f.FieldType.FullName == "System.Object") == true;
                else metadata &= SelectedPropertyMetadata(owner, "Value");
                if (named) metadata &= SelectedPropertyMetadata(attribute, "Second");
                if (mode.EndsWith("-nested", StringComparison.Ordinal))
                    foreach (var propertyOwner in new[] { owner, attribute }.Where(t => t is not null).Distinct())
                        foreach (var property in propertyOwner!.Properties)
                            metadata &= property.CustomAttributes.Count == 1 && property.GetMethod is { } getter && property.SetMethod is { } setter
                                && new[] { getter, setter }.All(m => m.CustomAttributes.Count == 1 && m.MethodReturnType.CustomAttributes.Count == 1
                                    && m.Parameters.All(p => p.CustomAttributes.Count == 1));
                if (SelectedNestedIdentity(mode))
                {
                    var nestedProperty = owner?.Properties.SingleOrDefault(p => p.Name == "Value");
                    metadata &= nestedProperty?.CustomAttributes.Count == 1
                        && nestedProperty.CustomAttributes[0].AttributeType.FullName == "Graph.SelectedSiblingAttribute"
                        && nestedProperty.CustomAttributes[0].ConstructorArguments.Count == 1
                        && nestedProperty.CustomAttributes[0].ConstructorArguments[0].Value is TypeReference conditionalIdentity
                        && conditionalIdentity.FullName == "Graph.SelectedConditional" && conditionalIdentity.Scope.Name == "DnZlib";
                }
                Require(observed || metadata, "selected named property/accessor metadata disappeared");
                if (!original && ownership[1] == 's' && !observed)
                {
                    bool constructs = conservative || SelectedIndependent(mode) || SelectedConstruct(mode) && SelectedMatch(mode);
                    Require(constructs ? Body(constructor) : ExactStub(constructor), "selected stripped constructor metadata/body state changed");
                    foreach (var propertyOwner in new[] { owner, attribute }.Where(t => t is not null).Distinct())
                        foreach (var property in propertyOwner!.Properties)
                        {
                            bool set = conservative || SelectedConstruct(mode) && SelectedMatch(mode);
                            Require(set ? Body(property.SetMethod) : ExactStub(property.SetMethod), "selected setter body state changed");
                            Require(physicalGetter ? Body(property.GetMethod) : ExactStub(property.GetMethod), "selected getter publication state changed");
                        }
                    if (!named || mode.EndsWith("-nested", StringComparison.Ordinal) || SelectedNestedIdentity(mode))
                    {
                        var siblingConstructor = attributeModule.GetType("Graph.SelectedSiblingAttribute")?.Methods.SingleOrDefault(m => m.IsConstructor);
                        Require(physicalSibling ? Body(siblingConstructor) : ExactStub(siblingConstructor), "selected sibling publication state changed");
                    }
                }
                Require(row.Properties.Select(p => p.Name).SequenceEqual(named ? (field ? new[] { "Second" } : new[] { "Second", "Value" }) : new[] { "Value" })
                    && row.Fields.Select(f => f.Name).SequenceEqual(field ? new[] { "Value" } : Array.Empty<string>()), "selected named argument order or kind changed");
                var value = field ? row.Fields.Single().Argument : row.Properties.Single(p => p.Name == "Value").Argument;
                Require(value.Type.FullName == "System.Object" && value.Value is CustomAttributeArgument boxed && boxed.Type.FullName == "System.Int32" && boxed.Value is int number && number == 7,
                    "selected object-typed named argument lost its declared/value types");
                if (!observed && metadata) identity = CheckSelectedClrMetadata(directory, providerModule.Assembly.Name.Name, named, field, inherited);
            }
            if (original) CheckSelectedAttributeInput(app, library, shim, mode);
        }
        if (observed) Console.WriteLine("graph-selected-observed=" + System.Text.Json.JsonSerializer.Serialize(new { effects, metadata }));
        Console.WriteLine("graph-copied-array=" + state + ":" + mode + ":selected-metadata,execution-boundaries,exact-stubs,unique-dependencies");
        if (!observed) Console.WriteLine("graph-selected-identity=" + (identity ?? "null"));
    }

    private static bool SelectedPropertyMetadata(TypeDefinition? owner, string name)
    {
        var property = owner?.Properties.SingleOrDefault(p => p.Name == name);
        return property is not null && property.PropertyType.FullName == "System.Object"
            && property.GetMethod is { } getter && getter.ReturnType.FullName == "System.Object"
            && property.SetMethod is { } setter && setter.Parameters.Count == 1 && setter.Parameters[0].ParameterType.FullName == "System.Object";
    }

    private static string CheckSelectedClrMetadata(string directory, string assemblyName, bool named, bool field, bool inherited)
    {
        var context = new AssemblyLoadContext("selected-metadata", isCollectible: true);
        context.Resolving += (_, name) => File.Exists(Path.Combine(directory, name.Name + ".dll")) ? context.LoadFromAssemblyPath(Path.GetFullPath(Path.Combine(directory, name.Name + ".dll"))) : null;
        try
        {
            var assembly = context.LoadFromAssemblyPath(Path.GetFullPath(Path.Combine(directory, assemblyName + ".dll")));
            var provider = assembly.GetType("Graph.SelectedProvider", throwOnError: true)!;
            var row = provider.GetCustomAttributesData().Single(a => a.AttributeType.FullName == "Graph.SelectedSpecialAttribute");
            Require(row.ConstructorArguments.Count == (named ? 0 : 1), "CLR could not decode selected constructor metadata");
            var value = row.NamedArguments.Single(a => a.MemberName == "Value");
            Require(value.IsField == field && value.TypedValue.ArgumentType == typeof(int) && value.TypedValue.Value is int number && number == 7,
                "CLR could not decode selected named value");
            Require(value.MemberInfo.DeclaringType!.FullName == "Graph." + (inherited ? "SelectedBaseAttribute" : "SelectedSpecialAttribute"), "CLR selected the wrong inherited named owner");
            if (named) Require(row.NamedArguments.Single(a => a.MemberName == "Second").TypedValue.Value is string text && text == "second", "CLR lost the sibling named argument");
            return System.Text.Json.JsonSerializer.Serialize(new
            {
                attribute = row.AttributeType.FullName,
                constructor = row.Constructor.ToString(),
                arguments = row.ConstructorArguments.Select(a => a.ToString()).ToArray(),
                namedArguments = row.NamedArguments.Select(a => new
                {
                    name = a.MemberName, field = a.IsField, owner = a.MemberInfo.DeclaringType!.FullName,
                    declaredType = a.MemberInfo is System.Reflection.PropertyInfo property ? property.PropertyType.FullName : ((System.Reflection.FieldInfo)a.MemberInfo).FieldType.FullName,
                    value = a.TypedValue.ToString(),
                }).ToArray(),
            });
        }
        finally { context.Unload(); }
    }

    private static void CheckSelectedCommonInput(AssemblyDefinition app, string mode)
    {
        var main = app.EntryPoint;
        Require(main.DeclaringType.FullName == "Graph.SelectedEntry" && main.Name == "Main", "selected entry changed");
        Require(main.Body.Instructions.Take(4).All(i => i.OpCode == OpCodes.Call)
            && main.Body.Instructions.Take(4).Select(i => ((MethodReference)i.Operand).Name).SequenceEqual(new[] { "get_InvariantCulture", "set_CurrentCulture", "get_InvariantCulture", "set_CurrentUICulture" }), "selected entry did not pin both cultures");
        foreach (var method in app.MainModule.GetTypes().SelectMany(t => t.Methods).Where(m => m.HasBody))
            foreach (var instruction in method.Body.Instructions)
            {
                Require(instruction.OpCode != OpCodes.Box && instruction.OpCode != OpCodes.Initobj, "selected fixture gained an executable value root");
                if (instruction.OpCode == OpCodes.Newobj)
                    Require(SelectedIndependent(mode) && method.Name == "Construct" && instruction.Operand is MethodReference constructor
                        && constructor.DeclaringType.FullName == "Graph.SelectedSpecialAttribute" && constructor.Parameters.Count == 0,
                        "selected fixture gained a direct target construction root");
                if (instruction.OpCode == OpCodes.Call && instruction.Operand is MethodReference called && called.Name == ".ctor")
                    Require(method.IsConstructor && called.DeclaringType.FullName == "System.Object" && called.Parameters.Count == 0,
                        "selected fixture directly called a target constructor");
            }
        Require(app.MainModule.GetTypes().All(t => !t.HasCustomAttributes), "selected fixture gained an application attribute root");
    }

    private static void CheckSelectedLayoutInput(AssemblyDefinition app, AssemblyDefinition shim, string mode)
    {
        CheckSelectedCommonInput(app, mode);
        bool box = mode.Contains("-box-", StringComparison.Ordinal), noCapability = mode.EndsWith("-nocap", StringComparison.Ordinal), noFactory = mode.EndsWith("-nofactory", StringComparison.Ordinal);
        var entry = app.MainModule.GetType("Graph.SelectedEntry");
        var holder = app.MainModule.GetType("Graph.SelectedHolder");
        Require(holder.Fields.Count == (mode.EndsWith("-nolayout", StringComparison.Ordinal) ? 0 : 1), "selected field-only layout control changed");
        if (holder.Fields.Count != 0) Require(holder.Fields[0].FieldType.FullName == "Graph.SelectedPayload" && holder.Fields[0].FieldType.Scope.Name == "DnZlib", "selected field lost copied value layout");
        var code = app.MainModule.GetTypes().SelectMany(t => t.Methods).Where(m => m.HasBody).SelectMany(m => m.Body.Instructions).ToArray();
        Require(code.All(i => i.Operand is not TypeReference t || t.FullName != "Graph.SelectedPayload")
            && code.All(i => i.Operand is not MethodReference m || m.DeclaringType.FullName != "Graph.SelectedPayload"), "selected copied payload gained an executable operand");
        var touch = holder.Methods.Single();
        Require(touch.Name == "Touch" && touch.Parameters.Count == 0 && touch.ReturnType.FullName == "System.Void"
            && touch.Body.Instructions.Select(i => i.OpCode.Code).SequenceEqual(new[] { Code.Ret }), "selected layout holder helper changed");
        var observe = entry.Methods.Single(m => m.Name == "Observe");
        Require(observe.Parameters.Count == 0 && observe.ReturnType.FullName == "System.Void"
            && observe.Body.Instructions.Select(i => i.OpCode.Code).SequenceEqual(new[] { Code.Call, Code.Ret })
            && observe.Body.Instructions[0].Operand is MethodReference observation && observation.DeclaringType.FullName == holder.FullName
            && observation.Name == "Touch", "selected layout must first name its holder inside the observation helper");
        var main = app.EntryPoint.Body.Instructions.Skip(4).ToArray();
        Require(main.Select(i => i.OpCode.Code).SequenceEqual(new[] { Code.Call, Code.Call, Code.Ret })
            && main.Take(2).All(i => ((MethodReference)i.Operand).DeclaringType.FullName == entry.FullName)
            && main.Take(2).Select(i => ((MethodReference)i.Operand).Name).SequenceEqual(mode.EndsWith("-late", StringComparison.Ordinal) ? new[] { "Capability", "Observe" } : new[] { "Observe", "Capability" }), "selected layout capability order changed");
        var capability = entry.Methods.Single(m => m.Name == "Capability").Body.Instructions.ToArray();
        Code[] expected = noCapability ? new[] { Code.Ret } : box ? new[] { Code.Ldloca, Code.Ldtoken, Code.Call, Code.Pop, Code.Ret }
            : noFactory ? new[] { Code.Ldc_I4_1, Code.Newarr, Code.Ldnull, Code.Call, Code.Pop, Code.Ret }
            : new[] { Code.Ldtoken, Code.Call, Code.Ldc_I4_1, Code.Call, Code.Ldnull, Code.Call, Code.Pop, Code.Ret };
        Require(capability.Select(i => i.OpCode.Code).SequenceEqual(expected), "selected layout capability instruction shape changed");
        if (!noCapability)
        {
            var calls = capability.Where(i => i.OpCode == OpCodes.Call).Select(i => (MethodReference)i.Operand).ToArray();
            Require(calls.Select(m => m.Name).SequenceEqual(box ? new[] { "Box" } : noFactory ? new[] { "IndexOf" } : new[] { "GetTypeFromHandle", "CreateInstance", "IndexOf" }), "selected layout capability API changed");
            if (box) Require(calls[0].DeclaringType.FullName == "System.Runtime.CompilerServices.RuntimeHelpers"
                && calls[0].Parameters.Select(p => p.ParameterType.FullName).SequenceEqual(new[] { "System.Byte&", "System.RuntimeTypeHandle" })
                && capability[1].Operand is TypeReference byteType && byteType.FullName == "System.Byte", "selected handle box changed its unrelated payload");
            else Require(calls[^1].DeclaringType.FullName == "System.Array" && calls[^1].Parameters.Select(p => p.ParameterType.FullName).SequenceEqual(new[] { "System.Array", "System.Object" }), "selected equality lost nongeneric search");
        }
        var payload = shim.MainModule.GetType("Graph.SelectedPayload");
        Require(payload.IsValueType && payload.Fields.Single().FieldType.FullName == "System.Int32" && payload.Methods.Count == 2, "selected copied layout definition changed");
        var dispatch = payload.Methods.Single(m => m.Name == (box ? "ToString" : "Equals"));
        Require(dispatch.IsVirtual && !dispatch.IsStatic && !dispatch.IsNewSlot && dispatch.Parameters.Count == (box ? 0 : 1), "selected payload lost its effective override");
        CheckSelectedEffectBody(dispatch, "Dispatch", 0, box ? Code.Ldstr : Code.Ldc_I4_0);
        CheckSelectedEffectBody(payload.Methods.Single(m => m.Name == "Ordinary"), "Ordinary", 0);
    }

    private static void CheckSelectedAttributeInput(AssemblyDefinition app, AssemblyDefinition library, AssemblyDefinition shim, string mode)
    {
        CheckSelectedCommonInput(app, mode);
        bool named = SelectedNamed(mode), field = mode.Contains("-field", StringComparison.Ordinal), inherited = mode.Contains("-inherited", StringComparison.Ordinal);
        string ownership = named ? "ss" : mode.Split('-')[1];
        var attributeModule = ownership[1] == 'c' ? shim.MainModule : library.MainModule;
        var providerModule = ownership[0] == 'c' ? shim.MainModule : library.MainModule;
        var provider = providerModule.GetType("Graph.SelectedProvider");
        Require(provider.Methods.Count == 0 && provider.Fields.Count == 0 && !provider.HasGenericParameters, "selected provider gained an independent execution route");
        var appCode = app.MainModule.GetTypes().SelectMany(t => t.Methods).Where(m => m.HasBody).SelectMany(m => m.Body.Instructions).ToArray();
        Require(appCode.Any(i => i.Operand is TypeReference type && type.FullName == provider.FullName) == SelectedKnownOwner(mode)
            && !app.MainModule.GetTypes().SelectMany(t => t.Fields).Any(f => f.FieldType.FullName == provider.FullName), "selected provider became a known executable owner");
        var entry = app.MainModule.GetType("Graph.SelectedEntry");
        var read = entry.Methods.Single(m => m.Name == "Read");
        Require(read.Parameters.Count == 1 && read.Parameters[0].ParameterType.FullName == "System.Reflection.MemberInfo", "selected query lost unknown owner summary");
        int reads = !SelectedQuery(mode) ? 0 : mode.EndsWith("-upgrade", StringComparison.Ordinal) ? 2 : 1;
        var instructions = read.Body.Instructions.ToArray();
        int width = SelectedKnownOwner(mode) ? 7 : 6;
        Require(instructions.Length == width * reads + 1 && instructions[^1].OpCode == OpCodes.Ret, "selected query count changed");
        for (int i = 0; i < reads; i++)
        {
            var block = instructions.Skip(i * width).Take(width).ToArray();
            int offset = SelectedKnownOwner(mode) ? 1 : 0;
            if (SelectedKnownOwner(mode))
                Require(block[0].OpCode == OpCodes.Ldtoken && block[0].Operand is TypeReference known && known.FullName == provider.FullName
                    && block[1].Operand is MethodReference fromHandle && fromHandle.Name == "GetTypeFromHandle", "selected query lost its explicit known owner");
            Require(block.Select(n => n.OpCode.Code).SequenceEqual(SelectedKnownOwner(mode)
                    ? new[] { Code.Ldtoken, Code.Call, Code.Ldtoken, Code.Call, Code.Ldc_I4_0, Code.Callvirt, Code.Pop }
                    : new[] { Code.Ldarg_0, Code.Ldtoken, Code.Call, Code.Ldc_I4_0, Code.Callvirt, Code.Pop })
                && block[1 + offset].Operand is TypeReference filter && filter.FullName == "Graph." + (mode.EndsWith("-unmatched", StringComparison.Ordinal) ? "SelectedUnmatchedAttribute" : "SelectedSpecialAttribute")
                && filter.Scope.Name == attributeModule.Assembly.Name.Name
                && block[4 + offset].Operand is MethodReference query && query.DeclaringType.FullName == "System.Reflection.MemberInfo"
                && query.Name == (mode.EndsWith("-instance", StringComparison.Ordinal) || i == 1 ? "GetCustomAttributes" : "IsDefined")
                && query.Parameters.Select(p => p.ParameterType.FullName).SequenceEqual(new[] { "System.Type", "System.Boolean" }), "selected query/filter semantics changed");
        }
        var attribute = attributeModule.GetType("Graph.SelectedSpecialAttribute");
        CheckSelectedEffectBody(attribute.Methods.Single(m => m.IsConstructor), "Ctor", 2);
        foreach (var owner in new[] { attribute, inherited ? attributeModule.GetType("Graph.SelectedBaseAttribute") : attribute }.Distinct())
            foreach (var property in owner.Properties)
            {
                CheckSelectedEffectBody(property.SetMethod, property.Name == "Second" ? "SecondSetter" : "Setter", 0);
                CheckSelectedEffectBody(property.GetMethod, "Getter", 0, Code.Ldnull);
            }
        if (named)
        {
            Require(SelectedNestedIdentity(mode) || !File.Exists(Path.Combine(Path.GetDirectoryName(app.MainModule.FileName) ?? "", "link.xml")), "named member witness gained preservation roots");
            if (SelectedIndependent(mode))
            {
                var construct = entry.Methods.Single(m => m.Name == "Construct");
                Require(construct.Body.Instructions.Select(i => i.OpCode.Code).SequenceEqual(new[] { Code.Newobj, Code.Pop, Code.Ret })
                    && construct.Body.Instructions[0].Operand is MethodReference ctor && ctor.DeclaringType.FullName == attribute.FullName && ctor.Parameters.Count == 0,
                    "named metadata witness lost its independent constructor");
            }
            else Require(entry.Methods.All(m => m.Name != "Construct") && !appCode.Any(i => i.OpCode == OpCodes.Newobj), "passive named owner gained independent construction");
        }
        else
        {
            var row = provider.CustomAttributes.Single(a => a.AttributeType.FullName == attribute.FullName);
            Require(row.ConstructorArguments.Count == 1 && row.ConstructorArguments[0].Value is TypeReference identity
                && identity.FullName == "Graph." + (mode.EndsWith("-inactive", StringComparison.Ordinal) ? "SelectedInert" : "SelectedConditional")
                && identity.Scope.Name == "DnZlib", "selected metadata lost its conditional type identity");
        }
    }

    private static void CheckSelectedEffectBody(MethodDefinition method, string effect, int prefix, Code? result = null)
    {
        var code = method.Body.Instructions.ToArray();
        var expected = new List<Code>();
        if (prefix == 2) expected.AddRange(new[] { Code.Ldarg_0, Code.Call });
        expected.AddRange(new[] { Code.Ldstr, Code.Call, Code.Call, Code.Pop });
        if (result is { } opcode) expected.Add(opcode);
        expected.Add(Code.Ret);
        Require(code.Select(i => i.OpCode.Code).SequenceEqual(expected) && (string)code[prefix].Operand == "Graph.Selected" + effect + "Target, GraphApp"
            && code[prefix + 1].Operand is MethodReference lookup && lookup.DeclaringType.FullName == "System.Type" && lookup.Name == "GetType"
            && code[prefix + 2].Operand is MethodReference activation && activation.DeclaringType.FullName == "System.Activator" && activation.Name == "CreateInstance"
            && activation.Parameters.Select(p => p.ParameterType.FullName).SequenceEqual(new[] { "System.Type" }), "selected copied/attribute effect changed its indirect target");
    }
}
