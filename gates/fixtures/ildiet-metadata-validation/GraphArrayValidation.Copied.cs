using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Xml.Linq;

static partial class GraphArrayValidation
{
    private static bool CopiedRootAttributeMode(string mode) => mode is "copied-type-attribute" or "copied-type-attribute-unnamed"
        or "copied-type-attribute-noquery" or "copied-type-attribute-data" or "copied-type-attribute-setter"
        or "copied-method-attribute" or "copied-assembly-attribute"
        or "copied-parameter-attribute" or "copied-property-attribute-data" or "copied-event-attribute-data";

    private static bool CopiedNamedSetterMode(string mode) => mode is "copied-type-attribute-setter"
        or "copied-type-attribute-unnamed" or "copied-type-attribute-noquery" or "copied-type-attribute-data"
        or "copied-property-attribute-data" or "copied-event-attribute-data";

    private static bool CopiedPreservationMode(string mode) => mode is "copied-preserve-method" or "copied-preserve-conditional" or "copied-preserve-inactive" or "copied-preserve-signature" or "copied-preserve-named";

    private static bool CopiedDemandMode(string mode) => mode is "copied-demand-default" or "copied-demand-construct"
        or "copied-demand-method" or "copied-demand-property" or "copied-demand-event" or "copied-demand-field"
        or "copied-demand-method-lookup" or "copied-demand-property-lookup" or "copied-demand-event-lookup" or "copied-demand-field-lookup"
        or "copied-demand-formed" or "copied-demand-event-attribute";

    private static bool CopiedDemandNegativeMode(string mode) => mode.EndsWith("-lookup", StringComparison.Ordinal)
        || mode is "copied-demand-formed" or "copied-demand-event-attribute";

    internal static bool RunCopied(string[] args)
    {
        if (args.Length == 5 && args[0] == "--check-selected-cut-fixtures")
        {
            CheckSelectedCut(args[1], args[2], args[3], args[4]);
            return true;
        }
        if (args.Length == 4 && args[0] == "--check-cut-passive-fixtures")
        {
            CheckCutPassive(args[1], args[2], args[3]);
            return true;
        }
        if (args.Length == 3 && args[0] == "--check-cut-backend-fixture")
        {
            CheckCutBackend(args[1], args[2]);
            return true;
        }
        if (args.Length == 4 && args[0] == "--create-copied-array-fixtures")
        {
            CreateCopied(args[1], args[2], args[3]);
            Console.WriteLine("fixture-created=copied-array mode=" + args[3]);
            return true;
        }
        if (args.Length == 4 && args[0] == "--check-copied-array-fixtures")
        {
            CheckCopied(args[1], args[2], args[3]);
            return true;
        }
        return false;
    }

    private static void CreateCopied(string directory, string corelib, string mode)
    {
        if (PassiveEvidenceMode(mode)) { CreatePassiveEvidence(directory, corelib, mode); return; }
        if (ModelAuthorityMode(mode)) { CreateModelAuthority(directory, corelib, mode); return; }
        if (ByRefTokenMode(mode)) { CreateByRefTokens(directory, corelib, mode); return; }
        if (EagerModelMode(mode)) { CreateEagerModel(directory, corelib, mode); return; }
        if (AttributeInventoryMode(mode)) { CreateAttributeInventory(directory, corelib, mode); return; }
        if (SelectedLayoutMode(mode)) { CreateSelectedLayout(directory, corelib, mode); return; }
        if (CopiedLateMode(mode)) { CreateCopiedLate(directory, corelib, mode); return; }
        Require(mode is "direct" or "copied-to-copied" or "cctor" or "ldftn-delegate" or "unarmed" or "module-initializer" or "framework-module-unarmed"
            or "framework-attribute-named" or "framework-attribute-unarmed" or "framework-attribute-same-name"
            || CopiedRootAttributeMode(mode) || CopiedPreservationMode(mode) || CopiedDemandMode(mode), "unknown copied array mode");
        Directory.CreateDirectory(directory);
        using var core = AssemblyDefinition.ReadAssembly(corelib);
        using var app = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition("GraphApp", new Version(1, 0)), "GraphApp", ModuleKind.Console);
        using var library = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition("GraphLib", new Version(1, 0)), "GraphLib", ModuleKind.Dll);
        using var facade = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition("System.ArrayFacade", new Version(1, 0)), "System.ArrayFacade", ModuleKind.Dll);
        using var leaf = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition("System.ArrayLeaf", new Version(1, 0)), "System.ArrayLeaf", ModuleKind.Dll);
        using var shim = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition("DnZlib", new Version(1, 0)), "DnZlib", ModuleKind.Dll);
        TypeReference Core(ModuleDefinition target, string name) => target.ImportReference(core.MainModule.GetType("System." + name));
        TypeDefinition Type(ModuleDefinition target, string name, bool value = false)
        {
            var type = new TypeDefinition("Graph", name, value
                ? TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.SequentialLayout | TypeAttributes.BeforeFieldInit
                : TypeAttributes.Public | TypeAttributes.Class, Core(target, value ? "ValueType" : "Object"));
            target.Types.Add(type);
            return type;
        }
        MethodDefinition Method(TypeDefinition type, string name, bool instance = false)
        {
            var method = new MethodDefinition(name, MethodAttributes.Public | (instance
                ? MethodAttributes.SpecialName | MethodAttributes.RTSpecialName : MethodAttributes.Static), Core(type.Module, "Void"));
            type.Methods.Add(method);
            return method;
        }
        MethodReference Dependency(string name)
        {
            var method = Method(Type(library.MainModule, name), "Touch");
            method.ReturnType = Core(library.MainModule, "Int32");
            method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4, 7));
            method.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            return app.MainModule.ImportReference(method);
        }
        MethodDefinition Constructor(TypeDefinition type, string dependency, bool overload = false)
        {
            var ctor = Method(type, ".ctor", true);
            if (overload) ctor.Parameters.Add(new ParameterDefinition(Core(type.Module, "Int32")));
            if (!type.IsValueType)
            {
                ctor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
                ctor.Body.Instructions.Add(Instruction.Create(OpCodes.Call, type.Module.ImportReference(core.MainModule.GetType("System.Object")
                    .Methods.Single(m => m.IsConstructor && !m.IsStatic && m.Parameters.Count == 0))));
            }
            if (type.IsValueType) ctor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            ctor.Body.Instructions.Add(Instruction.Create(OpCodes.Call, Dependency(dependency)));
            ctor.Body.Instructions.Add(type.IsValueType ? Instruction.Create(OpCodes.Stfld, type.Fields.Single()) : Instruction.Create(OpCodes.Pop));
            ctor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            return ctor;
        }
        var value = Type(app.MainModule, "CopiedArrayValue", true);
        value.Fields.Add(new FieldDefinition("Value", FieldAttributes.Public, Core(app.MainModule, "Int32")));
        Constructor(value, "CopiedArrayDependency");
        Constructor(value, "CopiedArrayOverloadDependency", true);
        var ordinary = Method(value, "Ordinary");
        ordinary.Body.Instructions.Add(Instruction.Create(OpCodes.Call, Dependency("CopiedArrayOrdinaryDependency")));
        ordinary.Body.Instructions.Add(Instruction.Create(OpCodes.Pop));
        ordinary.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        var reference = Type(app.MainModule, "CopiedArrayReference");
        Constructor(reference, "CopiedArrayReferenceDependency");
        var provider = Method(Type(library.MainModule, "CopiedArrayUnusedProvider"), "Read");
        provider.Body.Instructions.Add(Instruction.Create(OpCodes.Call, library.MainModule.GetType("Graph.CopiedArrayReferenceDependency").Methods.Single()));
        provider.Body.Instructions.Add(Instruction.Create(OpCodes.Pop));
        provider.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));

        MethodDefinition Initializer(ModuleDefinition target, string name)
        {
            var initialize = Method(Type(target, name), "Initialize");
            initialize.Parameters.Add(new ParameterDefinition(Core(target, "Array")));
            initialize.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            initialize.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, target.ImportReference(core.MainModule.GetType("System.Array")
                .Methods.Single(m => m.Name == "Initialize" && !m.IsStatic && m.Parameters.Count == 0))));
            initialize.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            return initialize;
        }
        var initialize = Initializer(facade.MainModule, "CopiedArrayFacade");
        var leafInitialize = Initializer(leaf.MainModule, "CopiedArrayLeaf");
        var forward = Method(initialize.DeclaringType, "Forward");
        forward.Parameters.Add(new ParameterDefinition(Core(facade.MainModule, "Array")));
        forward.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
        forward.Body.Instructions.Add(Instruction.Create(OpCodes.Call, facade.MainModule.ImportReference(leafInitialize)));
        forward.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        var noop = Method(initialize.DeclaringType, "Noop");
        noop.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        MethodDefinition? inactiveNoop = null;
        if (CopiedPreservationMode(mode))
        {
            inactiveNoop = Method(leafInitialize.DeclaringType, "Noop");
            inactiveNoop.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            string conditional = mode == "copied-preserve-method" ? "" : " required=\"false\"";
            string signatureRule = mode == "copied-preserve-signature" ? "  <assembly fullname=\"GraphApp\" preserve=\"nothing\">\n"
                + "    <type fullname=\"Graph.CopiedArrayEntry\" preserve=\"nothing\">\n      <method name=\"PreservedSignature\" />\n    </type>\n  </assembly>\n" : "";
            File.WriteAllText(Path.Combine(directory, "link.xml"), "<linker>\n" + signatureRule + "  <assembly fullname=\"System.ArrayFacade\" preserve=\"nothing\">\n"
                + "    <type fullname=\"Graph.CopiedArrayFacade\" preserve=\"nothing\"" + conditional + ">\n"
                + "      <method name=\"Initialize\" />\n    </type>\n  </assembly>\n</linker>\n");
        }
        var construct = Method(initialize.DeclaringType, "Construct");
        construct.ReturnType = Core(facade.MainModule, "Object");
        construct.Parameters.Add(new ParameterDefinition(Core(facade.MainModule, "Type")));
        construct.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
        construct.Body.Instructions.Add(Instruction.Create(OpCodes.Call, facade.MainModule.ImportReference(core.MainModule.GetType("System.Activator")
            .Methods.Single(m => m.Name == "CreateInstance" && !m.HasGenericParameters && m.Parameters.Count == 1
                && m.Parameters[0].ParameterType.FullName == "System.Type"))));
        construct.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        var bridge = Type(facade.MainModule, "CopiedArrayCctor");
        var cctor = Method(bridge, ".cctor");
        cctor.Attributes = MethodAttributes.Private | MethodAttributes.Static | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName;
        cctor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_1));
        cctor.Body.Instructions.Add(Instruction.Create(OpCodes.Newarr, Core(facade.MainModule, "Int32")));
        cctor.Body.Instructions.Add(Instruction.Create(OpCodes.Call, initialize));
        cctor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        var enter = Method(bridge, "Enter");
        enter.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        if (mode is "module-initializer" or "framework-module-unarmed")
        {
            var owner = mode == "module-initializer" ? shim.MainModule : facade.MainModule;
            var initializer = Method(owner.GetType("<Module>"), ".cctor");
            initializer.Attributes = MethodAttributes.Private | MethodAttributes.Static | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName;
            initializer.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_1));
            initializer.Body.Instructions.Add(Instruction.Create(OpCodes.Newarr, Core(owner, "Int32")));
            initializer.Body.Instructions.Add(Instruction.Create(OpCodes.Call, owner.ImportReference(initialize)));
            initializer.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        }

        MethodDefinition? boundaryCtor = null;
        TypeDefinition? namedBoundary = null;
        if (mode is "framework-attribute-named" or "framework-attribute-unarmed" or "framework-attribute-same-name" || CopiedRootAttributeMode(mode))
        {
            var boundary = Type(facade.MainModule, "CopiedBoundaryAttribute");
            boundary.BaseType = Core(facade.MainModule, "Attribute");
            boundaryCtor = Method(boundary, ".ctor", true);
            boundaryCtor.Parameters.Add(new ParameterDefinition(Core(facade.MainModule, "Int32")));
            boundaryCtor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            boundaryCtor.Body.Instructions.Add(Instruction.Create(OpCodes.Call, facade.MainModule.ImportReference(core.MainModule.GetType("System.Attribute")
                .Methods.Single(m => m.IsConstructor && !m.IsStatic && m.Parameters.Count == 0))));
            if (mode != "copied-type-attribute-setter")
            {
                boundaryCtor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_1));
                boundaryCtor.Body.Instructions.Add(Instruction.Create(OpCodes.Newarr, Core(facade.MainModule, "Int32")));
                boundaryCtor.Body.Instructions.Add(Instruction.Create(OpCodes.Call, initialize));
            }
            boundaryCtor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            if (CopiedNamedSetterMode(mode))
            {
                var setter = Method(boundary, "set_Arm", true);
                setter.Attributes = MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig;
                setter.Parameters.Add(new ParameterDefinition(Core(facade.MainModule, "Int32")));
                setter.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_1));
                setter.Body.Instructions.Add(Instruction.Create(OpCodes.Newarr, Core(facade.MainModule, "Int32")));
                setter.Body.Instructions.Add(Instruction.Create(OpCodes.Call, initialize));
                setter.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
                var getter = Method(boundary, "get_Arm", true);
                getter.Attributes = MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig;
                getter.ReturnType = Core(facade.MainModule, "Int32");
                getter.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_7));
                getter.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
                boundary.Properties.Add(new PropertyDefinition("Arm", PropertyAttributes.None, Core(facade.MainModule, "Int32")) { GetMethod = getter, SetMethod = setter });
            }
            namedBoundary = boundary;
            if (mode == "framework-attribute-same-name")
            {
                namedBoundary = Type(leaf.MainModule, "CopiedBoundaryAttribute");
                namedBoundary.BaseType = Core(leaf.MainModule, "Attribute");
                var alternateCtor = Method(namedBoundary, ".ctor", true);
                alternateCtor.Parameters.Add(new ParameterDefinition(Core(leaf.MainModule, "Int32")));
                alternateCtor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
                alternateCtor.Body.Instructions.Add(Instruction.Create(OpCodes.Call, leaf.MainModule.ImportReference(core.MainModule.GetType("System.Attribute")
                    .Methods.Single(m => m.IsConstructor && !m.IsStatic && m.Parameters.Count == 0))));
                alternateCtor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            }
        }
        var entry = Type(app.MainModule, "CopiedArrayEntry");
        if (boundaryCtor is not null && !CopiedRootAttributeMode(mode))
        {
            var attribute = new CustomAttribute(app.MainModule.ImportReference(boundaryCtor));
            attribute.ConstructorArguments.Add(new CustomAttributeArgument(Core(app.MainModule, "Int32"), 7));
            entry.CustomAttributes.Add(attribute);
        }
        if (CopiedRootAttributeMode(mode))
        {
            var attribute = new CustomAttribute(boundaryCtor!);
            attribute.ConstructorArguments.Add(new CustomAttributeArgument(Core(facade.MainModule, "Int32"), 7));
            if (CopiedNamedSetterMode(mode))
                attribute.Properties.Add(new CustomAttributeNamedArgument("Arm", new CustomAttributeArgument(Core(facade.MainModule, "Int32"), 7)));
            if (mode == "copied-assembly-attribute") facade.CustomAttributes.Add(attribute);
            else if (mode is "copied-method-attribute" or "copied-parameter-attribute")
            {
                var owner = Method(initialize.DeclaringType, "AttributedMember");
                owner.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
                if (mode == "copied-parameter-attribute")
                {
                    var parameter = new ParameterDefinition(Core(facade.MainModule, "Int32"));
                    owner.Parameters.Add(parameter);
                    parameter.CustomAttributes.Add(attribute);
                }
                else owner.CustomAttributes.Add(attribute);
            }
            else if (mode == "copied-property-attribute-data")
            {
                var getter = Method(initialize.DeclaringType, "get_AttributedProperty");
                getter.Attributes |= MethodAttributes.SpecialName | MethodAttributes.HideBySig;
                getter.ReturnType = Core(facade.MainModule, "Int32");
                getter.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_0));
                getter.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
                var property = new PropertyDefinition("AttributedProperty", PropertyAttributes.None, Core(facade.MainModule, "Int32")) { GetMethod = getter };
                property.CustomAttributes.Add(attribute);
                initialize.DeclaringType.Properties.Add(property);
            }
            else if (mode == "copied-event-attribute-data")
            {
                var item = new EventDefinition("AttributedEvent", EventAttributes.None, Core(facade.MainModule, "Action"));
                foreach (string prefix in new[] { "add_", "remove_" })
                {
                    var accessor = Method(initialize.DeclaringType, prefix + "AttributedEvent");
                    accessor.Attributes |= MethodAttributes.SpecialName | MethodAttributes.HideBySig;
                    accessor.Parameters.Add(new ParameterDefinition(Core(facade.MainModule, "Action")));
                    accessor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
                    if (prefix == "add_") item.AddMethod = accessor;
                    else item.RemoveMethod = accessor;
                }
                item.CustomAttributes.Add(attribute);
                initialize.DeclaringType.Events.Add(item);
            }
            else initialize.DeclaringType.CustomAttributes.Add(attribute);
        }
        entry.Fields.Add(new FieldDefinition("Layout", FieldAttributes.Public | FieldAttributes.Static, value));
        entry.Fields.Add(new FieldDefinition("Reference", FieldAttributes.Public | FieldAttributes.Static, reference));
        if (mode == "copied-preserve-signature")
        {
            var signature = Method(entry, "PreservedSignature");
            signature.ReturnType = app.MainModule.ImportReference(initialize.DeclaringType);
            signature.Body.Instructions.Add(Instruction.Create(OpCodes.Ldnull));
            signature.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        }
        var main = Method(entry, "Main"); app.EntryPoint = main;
        var culture = core.MainModule.GetType("System.Globalization.CultureInfo");
        foreach (string setter in new[] { "set_CurrentCulture", "set_CurrentUICulture" })
        {
            main.Body.Instructions.Add(Instruction.Create(OpCodes.Call, app.MainModule.ImportReference(culture.Methods.Single(m => m.Name == "get_InvariantCulture"))));
            main.Body.Instructions.Add(Instruction.Create(OpCodes.Call, app.MainModule.ImportReference(culture.Methods.Single(m => m.Name == setter))));
        }
        if (CopiedDemandMode(mode))
        {
            var demandOwner = Type(facade.MainModule, "CopiedDemandOwner");
            void Arm(MethodDefinition method)
            {
                method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_1));
                method.Body.Instructions.Add(Instruction.Create(OpCodes.Newarr, Core(facade.MainModule, "Int32")));
                method.Body.Instructions.Add(Instruction.Create(OpCodes.Call, initialize));
            }
            var constructor = Method(demandOwner, ".ctor", true);
            constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Call, facade.MainModule.ImportReference(core.MainModule.GetType("System.Object")
                .Methods.Single(m => m.IsConstructor && !m.IsStatic && m.Parameters.Count == 0))));
            bool constructDemand = mode is "copied-demand-default" or "copied-demand-construct" or "copied-demand-formed";
            if (constructDemand) Arm(constructor);
            constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            if (mode is "copied-demand-method" or "copied-demand-method-lookup")
            {
                var run = Method(demandOwner, "Run");
                Arm(run);
                run.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            }
            if (mode is "copied-demand-property" or "copied-demand-property-lookup")
            {
                var getter = Method(demandOwner, "get_Value");
                getter.Attributes |= MethodAttributes.SpecialName | MethodAttributes.HideBySig;
                getter.ReturnType = Core(facade.MainModule, "Int32");
                Arm(getter);
                getter.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_7));
                getter.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
                demandOwner.Properties.Add(new PropertyDefinition("Value", PropertyAttributes.None, Core(facade.MainModule, "Int32")) { GetMethod = getter });
            }
            if (mode is "copied-demand-event" or "copied-demand-event-lookup" or "copied-demand-event-attribute")
            {
                var item = new EventDefinition("Changed", EventAttributes.None, Core(facade.MainModule, "Action"));
                foreach (string prefix in new[] { "add_", "remove_" })
                {
                    var accessor = Method(demandOwner, prefix + "Changed");
                    accessor.Attributes |= MethodAttributes.SpecialName | MethodAttributes.HideBySig;
                    accessor.Parameters.Add(new ParameterDefinition(Core(facade.MainModule, "Action")));
                    if (prefix == "add_" && mode != "copied-demand-event-attribute") Arm(accessor);
                    accessor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
                    if (prefix == "add_") item.AddMethod = accessor;
                    else item.RemoveMethod = accessor;
                }
                demandOwner.Events.Add(item);
                if (mode == "copied-demand-event-attribute")
                {
                    var attribute = Type(facade.MainModule, "CopiedDemandAttribute");
                    attribute.BaseType = Core(facade.MainModule, "Attribute");
                    var attributeCtor = Method(attribute, ".ctor", true);
                    attributeCtor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
                    attributeCtor.Body.Instructions.Add(Instruction.Create(OpCodes.Call, facade.MainModule.ImportReference(core.MainModule.GetType("System.Attribute")
                        .Methods.Single(m => m.IsConstructor && !m.IsStatic && m.Parameters.Count == 0))));
                    Arm(attributeCtor);
                    attributeCtor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
                    var setter = Method(attribute, "set_Arm", true);
                    setter.Attributes = MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig;
                    setter.Parameters.Add(new ParameterDefinition(Core(facade.MainModule, "Int32")));
                    Arm(setter);
                    setter.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
                    var getter = Method(attribute, "get_Arm", true);
                    getter.Attributes = MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig;
                    getter.ReturnType = Core(facade.MainModule, "Int32");
                    getter.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_7));
                    getter.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
                    attribute.Properties.Add(new PropertyDefinition("Arm", PropertyAttributes.None, Core(facade.MainModule, "Int32")) { GetMethod = getter, SetMethod = setter });
                    var row = new CustomAttribute(attributeCtor);
                    row.Properties.Add(new CustomAttributeNamedArgument("Arm", new CustomAttributeArgument(Core(facade.MainModule, "Int32"), 7)));
                    item.CustomAttributes.Add(row);
                    main.Body.Instructions.Add(Instruction.Create(OpCodes.Ldtoken, app.MainModule.ImportReference(attribute)));
                    main.Body.Instructions.Add(Instruction.Create(OpCodes.Call, app.MainModule.ImportReference(core.MainModule.GetType("System.Type")
                        .Methods.Single(m => m.Name == "GetTypeFromHandle"))));
                    main.Body.Instructions.Add(Instruction.Create(OpCodes.Pop));
                }
            }
            if (mode is "copied-demand-field" or "copied-demand-field-lookup")
            {
                // Reflection boxes admit user-module payloads; this shim is copied unchanged.
                var fieldValue = Type(shim.MainModule, "CopiedDemandFieldValue", true);
                fieldValue.Fields.Add(new FieldDefinition("Number", FieldAttributes.Public, Core(shim.MainModule, "Int32")));
                var stringify = Method(fieldValue, "ToString", true);
                stringify.Attributes = MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig;
                stringify.ReturnType = Core(shim.MainModule, "String");
                stringify.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_1));
                stringify.Body.Instructions.Add(Instruction.Create(OpCodes.Newarr, Core(shim.MainModule, "Int32")));
                stringify.Body.Instructions.Add(Instruction.Create(OpCodes.Call, shim.MainModule.ImportReference(initialize)));
                stringify.Body.Instructions.Add(Instruction.Create(OpCodes.Ldstr, "field"));
                stringify.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
                demandOwner.Fields.Add(new FieldDefinition("Value", FieldAttributes.Public | FieldAttributes.Static, facade.MainModule.ImportReference(fieldValue)));
            }
            var helper = Method(entry, "Demand");
            var parameter = new GenericParameter("T", helper);
            helper.GenericParameters.Add(parameter);
            if (mode is "copied-demand-default" or "copied-demand-formed")
            {
                var activate = new GenericInstanceMethod(app.MainModule.ImportReference(core.MainModule.GetType("System.Activator")
                    .Methods.Single(m => m.Name == "CreateInstance" && m.HasGenericParameters && m.Parameters.Count == 0)));
                activate.GenericArguments.Add(parameter);
                helper.Body.Instructions.Add(Instruction.Create(OpCodes.Call, activate));
            }
            else
            {
                helper.Body.Instructions.Add(Instruction.Create(OpCodes.Ldtoken, parameter));
                helper.Body.Instructions.Add(Instruction.Create(OpCodes.Call, app.MainModule.ImportReference(core.MainModule.GetType("System.Type")
                    .Methods.Single(m => m.Name == "GetTypeFromHandle"))));
                if (mode == "copied-demand-construct")
                    helper.Body.Instructions.Add(Instruction.Create(OpCodes.Call, app.MainModule.ImportReference(core.MainModule.GetType("System.Activator")
                        .Methods.Single(m => m.Name == "CreateInstance" && !m.HasGenericParameters && m.Parameters.Count == 1
                            && m.Parameters[0].ParameterType.FullName == "System.Type"))));
                else
                {
                    string kind = mode.Contains("-method", StringComparison.Ordinal) ? "Method"
                        : mode.Contains("-property", StringComparison.Ordinal) ? "Property"
                        : mode.Contains("-event", StringComparison.Ordinal) ? "Event" : "Field";
                    helper.Body.Instructions.Add(Instruction.Create(OpCodes.Ldstr, kind == "Method" ? "Run" : kind == "Event" ? "Changed" : "Value"));
                    helper.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, app.MainModule.ImportReference(core.MainModule.GetType("System.Type")
                        .Methods.Single(m => m.Name == "Get" + kind && m.Parameters.Count == 1 && m.Parameters[0].ParameterType.FullName == "System.String"))));
                    if (!mode.EndsWith("-lookup", StringComparison.Ordinal))
                    {
                        helper.Body.Instructions.Add(Instruction.Create(OpCodes.Ldnull));
                        string apiOwner = kind == "Method" ? "MethodBase" : kind + "Info";
                        string apiName = kind == "Method" ? "Invoke" : kind == "Event" ? "AddEventHandler" : "GetValue";
                        if (kind is "Method" or "Event") helper.Body.Instructions.Add(Instruction.Create(OpCodes.Ldnull));
                        helper.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, app.MainModule.ImportReference(core.MainModule.GetType("System.Reflection." + apiOwner)
                            .Methods.Single(m => m.Name == apiName && m.Parameters.Count == (kind is "Method" or "Event" ? 2 : 1)))));
                    }
                }
            }
            bool eventCall = mode is "copied-demand-event" or "copied-demand-event-attribute";
            if (!eventCall) helper.Body.Instructions.Add(Instruction.Create(OpCodes.Pop));
            helper.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            main.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_1));
            main.Body.Instructions.Add(Instruction.Create(OpCodes.Newarr, value));
            main.Body.Instructions.Add(Instruction.Create(OpCodes.Pop));
            var closedHelper = new GenericInstanceMethod(helper);
            closedHelper.GenericArguments.Add(app.MainModule.ImportReference(demandOwner));
            main.Body.Instructions.Add(Instruction.Create(mode == "copied-demand-formed" ? OpCodes.Ldtoken : OpCodes.Call, closedHelper));
            if (mode == "copied-demand-formed") main.Body.Instructions.Add(Instruction.Create(OpCodes.Pop));
        }
        if (mode == "copied-preserve-named")
        {
            main.Body.Instructions.Add(Instruction.Create(OpCodes.Ldstr, "Graph.CopiedArrayFacade, System.ArrayFacade"));
            main.Body.Instructions.Add(Instruction.Create(OpCodes.Call, app.MainModule.ImportReference(core.MainModule.GetType("System.Type")
                .Methods.Single(m => m.Name == "GetType" && m.IsStatic && m.Parameters.Count == 1
                    && m.Parameters[0].ParameterType.FullName == "System.String"))));
            main.Body.Instructions.Add(Instruction.Create(OpCodes.Pop));
        }
        if (mode is "framework-attribute-named" or "framework-attribute-same-name")
        {
            main.Body.Instructions.Add(Instruction.Create(OpCodes.Ldtoken, app.MainModule.ImportReference(namedBoundary!)));
            main.Body.Instructions.Add(Instruction.Create(OpCodes.Call, app.MainModule.ImportReference(core.MainModule.GetType("System.Type")
                .Methods.Single(m => m.Name == "GetTypeFromHandle"))));
            main.Body.Instructions.Add(Instruction.Create(OpCodes.Pop));
        }
        if (boundaryCtor is not null && !CopiedRootAttributeMode(mode))
        {
            main.Body.Instructions.Add(Instruction.Create(OpCodes.Ldtoken, entry));
            main.Body.Instructions.Add(Instruction.Create(OpCodes.Call, app.MainModule.ImportReference(core.MainModule.GetType("System.Type")
                .Methods.Single(m => m.Name == "GetTypeFromHandle"))));
            main.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_0));
            main.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, app.MainModule.ImportReference(core.MainModule.GetType("System.Reflection.MemberInfo")
                .Methods.Single(m => m.Name == "GetCustomAttributes" && m.Parameters.Count == 1
                    && m.Parameters[0].ParameterType.FullName == "System.Boolean"))));
            main.Body.Instructions.Add(Instruction.Create(OpCodes.Pop));
        }
        if (CopiedRootAttributeMode(mode))
        {
            if (mode != "copied-type-attribute-unnamed")
            {
                main.Body.Instructions.Add(Instruction.Create(OpCodes.Ldtoken, app.MainModule.ImportReference(namedBoundary!)));
                main.Body.Instructions.Add(Instruction.Create(OpCodes.Call, app.MainModule.ImportReference(core.MainModule.GetType("System.Type")
                    .Methods.Single(m => m.Name == "GetTypeFromHandle"))));
                main.Body.Instructions.Add(Instruction.Create(OpCodes.Pop));
            }
            if (mode != "copied-type-attribute-noquery")
            {
                main.Body.Instructions.Add(Instruction.Create(OpCodes.Ldtoken, app.MainModule.ImportReference(initialize.DeclaringType)));
                main.Body.Instructions.Add(Instruction.Create(OpCodes.Call, app.MainModule.ImportReference(core.MainModule.GetType("System.Type")
                    .Methods.Single(m => m.Name == "GetTypeFromHandle"))));
                if (mode is "copied-method-attribute" or "copied-parameter-attribute" or "copied-property-attribute-data" or "copied-event-attribute-data")
                {
                    string lookup = mode == "copied-property-attribute-data" ? "GetProperty" : mode == "copied-event-attribute-data" ? "GetEvent" : "GetMethod";
                    string member = mode == "copied-property-attribute-data" ? "AttributedProperty" : mode == "copied-event-attribute-data" ? "AttributedEvent" : "AttributedMember";
                    main.Body.Instructions.Add(Instruction.Create(OpCodes.Ldstr, member));
                    main.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, app.MainModule.ImportReference(core.MainModule.GetType("System.Type")
                        .Methods.Single(m => m.Name == lookup && m.Parameters.Count == 1 && m.Parameters[0].ParameterType.FullName == "System.String"))));
                }
                if (mode == "copied-parameter-attribute")
                {
                    main.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, app.MainModule.ImportReference(core.MainModule.GetType("System.Reflection.MethodBase")
                        .Methods.Single(m => m.Name == "GetParameters" && m.Parameters.Count == 0))));
                    main.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_0));
                    main.Body.Instructions.Add(Instruction.Create(OpCodes.Ldelem_Ref));
                }
                if (mode == "copied-assembly-attribute")
                    main.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, app.MainModule.ImportReference(core.MainModule.GetType("System.Type")
                        .Methods.Single(m => m.Name == "get_Assembly"))));
                if (mode is "copied-type-attribute-data" or "copied-property-attribute-data" or "copied-event-attribute-data")
                    main.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, app.MainModule.ImportReference(core.MainModule.GetType("System.Reflection.MemberInfo")
                        .Methods.Single(m => m.Name == "GetCustomAttributesData" && !m.IsStatic && m.Parameters.Count == 0))));
                else
                {
                    main.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_0));
                    string owner = mode == "copied-assembly-attribute" ? "System.Reflection.Assembly"
                        : mode == "copied-parameter-attribute" ? "System.Reflection.ParameterInfo" : "System.Reflection.MemberInfo";
                    main.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, app.MainModule.ImportReference(core.MainModule.GetType(owner)
                        .Methods.Single(m => m.Name == "GetCustomAttributes" && m.Parameters.Count == 1
                            && m.Parameters[0].ParameterType.FullName == "System.Boolean"))));
                }
                main.Body.Instructions.Add(Instruction.Create(OpCodes.Pop));
            }
        }
        if (mode == "ldftn-delegate")
        {
            var action = new GenericInstanceType(Core(app.MainModule, "Action`1"));
            action.GenericArguments.Add(Core(app.MainModule, "Array"));
            var actionCtor = new MethodReference(".ctor", Core(app.MainModule, "Void"), action) { HasThis = true };
            actionCtor.Parameters.Add(new ParameterDefinition(Core(app.MainModule, "Object")));
            actionCtor.Parameters.Add(new ParameterDefinition(Core(app.MainModule, "IntPtr")));
            main.Body.Instructions.Add(Instruction.Create(OpCodes.Ldnull));
            main.Body.Instructions.Add(Instruction.Create(OpCodes.Ldftn, app.MainModule.ImportReference(initialize)));
            main.Body.Instructions.Add(Instruction.Create(OpCodes.Newobj, actionCtor));
            main.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_1));
            main.Body.Instructions.Add(Instruction.Create(OpCodes.Newarr, Core(app.MainModule, "Int32")));
            var invoke = new MethodReference("Invoke", Core(app.MainModule, "Void"), action) { HasThis = true };
            invoke.Parameters.Add(new ParameterDefinition(action.ElementType.GenericParameters.Single()));
            main.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, invoke));
        }
        else
        {
            if (mode is "direct" or "copied-to-copied")
            {
                main.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_1));
                main.Body.Instructions.Add(Instruction.Create(OpCodes.Newarr, Core(app.MainModule, "Int32")));
            }
            var target = mode switch { "direct" => initialize, "copied-to-copied" => forward, "cctor" => enter,
                "copied-preserve-inactive" or "copied-preserve-signature" or "copied-preserve-named" => inactiveNoop!, _ => noop };
            main.Body.Instructions.Add(Instruction.Create(OpCodes.Call, app.MainModule.ImportReference(target)));
        }
        main.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        foreach (var assembly in new[] { app, library, facade, leaf, shim })
            assembly.Write(Path.Combine(directory, assembly.Name.Name + ".dll"), new WriterParameters { Timestamp = 0, DeterministicMvid = true });
    }

    private static void CheckCopied(string directory, string mode, string state)
    {
        if (PassiveEvidenceMode(mode)) { CheckPassiveEvidence(directory, mode, state); return; }
        if (ModelAuthorityMode(mode)) { CheckModelAuthority(directory, mode, state); return; }
        if (ByRefTokenMode(mode)) { CheckByRefTokens(directory, mode, state); return; }
        if (EagerModelMode(mode)) { CheckEagerModel(directory, mode, state); return; }
        if (AttributeInventoryMode(mode)) { CheckAttributeInventory(directory, mode, state); return; }
        if (SelectedLayoutMode(mode)) { CheckSelectedLayout(directory, mode, state); return; }
        if (CopiedLateMode(mode)) { CheckCopiedLate(directory, mode, state); return; }
        Require(mode is "direct" or "copied-to-copied" or "cctor" or "ldftn-delegate" or "unarmed" or "module-initializer" or "framework-module-unarmed"
            or "framework-attribute-named" or "framework-attribute-unarmed" or "framework-attribute-same-name"
            || CopiedRootAttributeMode(mode) || CopiedPreservationMode(mode) || CopiedDemandMode(mode), "unknown copied array mode");
        Require(state is "original" or "before" or "published" or "observed-before", "unknown copied array state");
        bool observedBefore = state == "observed-before";
        Require(!observedBefore || mode is "copied-property-attribute-data" or "copied-event-attribute-data" || CopiedDemandMode(mode), "observed old state is limited to copied accessor metadata controls");
        using var app = AssemblyDefinition.ReadAssembly(Path.Combine(directory, "GraphApp.dll"));
        using var library = AssemblyDefinition.ReadAssembly(Path.Combine(directory, "GraphLib.dll"));
        foreach (string name in new[] { "System.ArrayFacade", "System.ArrayLeaf", "DnZlib" })
        {
            using var copied = AssemblyDefinition.ReadAssembly(Path.Combine(directory, name + ".dll"));
            Require(copied.MainModule.AssemblyReferences.All(r => r.Name is "System.Private.CoreLib" or "System.ArrayLeaf" or "System.ArrayFacade"
                || name == "System.ArrayFacade" && mode is ("copied-demand-field" or "copied-demand-field-lookup") && r.Name == "DnZlib"),
                "copied fixture gained a stripped assembly reference");
            if (mode == "framework-attribute-same-name" && name != "DnZlib")
            {
                var attribute = copied.MainModule.GetType("Graph.CopiedBoundaryAttribute");
                Require(attribute is not null && attribute.BaseType.FullName == "System.Attribute", "copied fixture lost a same-named framework attribute definition");
                var attributeCtor = attribute!.Methods.Single(m => m.IsConstructor && !m.IsStatic);
                Require(attributeCtor.Parameters.Count == 1 && attributeCtor.Parameters[0].ParameterType.FullName == "System.Int32", "copied fixture changed a same-named attribute signature");
                var calls = attributeCtor.Body.Instructions.Where(i => i.Operand is MethodReference).Select(i => (MethodReference)i.Operand).ToArray();
                Require(calls.Length == (name == "System.ArrayFacade" ? 2 : 1)
                    && calls[0].DeclaringType.FullName == "System.Attribute" && calls[0].Name == ".ctor"
                    && (name != "System.ArrayFacade" || calls[1].DeclaringType.FullName == "Graph.CopiedArrayFacade" && calls[1].Name == "Initialize"),
                    "same-named copied attribute constructor bodies lost their distinct capabilities");
                Require(attributeCtor.Body.Instructions.Select(i => i.OpCode.Code).SequenceEqual(name == "System.ArrayFacade"
                    ? new[] { Code.Ldarg_0, Code.Call, Code.Ldc_I4_1, Code.Newarr, Code.Call, Code.Ret }
                    : new[] { Code.Ldarg_0, Code.Call, Code.Ret }), "same-named copied attribute constructors changed their original IL");
            }
            var moduleInitializers = copied.MainModule.GetType("<Module>").Methods.Where(m => m.IsConstructor && m.IsStatic).ToArray();
            bool moduleInitializes = name == "DnZlib" && mode == "module-initializer"
                || name == "System.ArrayFacade" && mode == "framework-module-unarmed";
            Require(moduleInitializers.Length == (moduleInitializes ? 1 : 0), "copied fixture changed its module initializer boundary");
            if (moduleInitializes)
                Require(moduleInitializers[0].Body.Instructions.Any(i => i.Operand is MethodReference method
                    && method.DeclaringType.FullName == "Graph.CopiedArrayFacade" && method.Name == "Initialize"), "copied module initializer lost its capability call");
            if (name == "System.ArrayFacade")
            {
                var construct = copied.MainModule.GetType("Graph.CopiedArrayFacade").Methods.Single(m => m.Name == "Construct");
                Require(construct.HasBody && construct.Body.Instructions.Any(i => i.Operand is MethodReference method
                    && method.DeclaringType.FullName == "System.Activator" && method.Name == "CreateInstance"), "copied fixture lost its unused construction-provider discriminator");
                var bridge = copied.MainModule.GetType("Graph.CopiedArrayCctor");
                Require(!bridge.IsBeforeFieldInit && bridge.Methods.Single(m => m.IsConstructor && m.IsStatic).Body.Instructions.Any(i => i.Operand is MethodReference method
                    && method.DeclaringType.FullName == "Graph.CopiedArrayFacade" && method.Name == "Initialize"), "copied fixture lost its precise static-constructor path");
            }
        }
        var value = app.MainModule.GetType("Graph.CopiedArrayValue");
        var ctor = value?.Methods.SingleOrDefault(m => m.IsConstructor && !m.IsStatic && m.Parameters.Count == 0);
        bool Body(MethodDefinition? method) => method is { HasBody: true } && !Stub(method);
        bool ExactStub(MethodDefinition method) => Stub(method) && method.Body.Instructions[0].Operand is int marker && marker == 0x494C4454;
        bool executes = state == "original" || state == "published" && mode is not ("unarmed" or "framework-module-unarmed" or "framework-attribute-unarmed"
            or "copied-type-attribute-unnamed" or "copied-type-attribute-noquery" or "copied-type-attribute-data" or "copied-preserve-inactive"
            or "copied-property-attribute-data" or "copied-event-attribute-data")
            && (!CopiedDemandMode(mode) || !CopiedDemandNegativeMode(mode));
        Require(observedBefore || Body(ctor) == executes, "copied array default constructor state: " + state + ":" + mode);
        Require(observedBefore || Body(library.MainModule.GetType("Graph.CopiedArrayDependency")?.Methods.SingleOrDefault()) == executes,
            "copied array unique dependency state: " + state + ":" + mode);
        Require(value is not null && ctor is not null, "copied array lost its modeled value definition or constructor signature");
        if (!executes && (!observedBefore || !Body(ctor))) Require(ExactStub(ctor!), "missing copied array body is not an exact marked stub");
        if (observedBefore && CopiedDemandMode(mode) && !Body(ctor))
            Require(library.MainModule.GetType("Graph.CopiedArrayDependency") is null, "old copied demand kept the allegedly missing unique dependency");
        if (observedBefore)
            Console.WriteLine("graph-copied-array-observed-before=" + mode + ":constructor-body=" + Body(ctor)
                + ":dependency-body=" + Body(library.MainModule.GetType("Graph.CopiedArrayDependency")?.Methods.SingleOrDefault()));
        if (state == "original")
        {
            var code = app.MainModule.GetTypes().SelectMany(t => t.Methods).Where(m => m.HasBody).SelectMany(m => m.Body.Instructions).ToArray();
            Require(code.All(i => i.OpCode != OpCodes.Initobj && i.OpCode != OpCodes.Box), "copied fixture gained an independent value runtime root");
            Require(code.Where(i => i.OpCode == OpCodes.Newobj).All(i => i.Operand is MethodReference m && m.DeclaringType.FullName.StartsWith("System.Action`1", StringComparison.Ordinal)), "copied fixture directly constructs a user payload");
            Require(code.All(i => i.Operand is not MethodReference m || m.DeclaringType.FullName != "Graph.CopiedArrayValue" || m.Name != ".ctor"), "copied fixture directly calls a value constructor");
            Require(code.All(i => i.Operand is not MethodReference m || m.DeclaringType.FullName != "System.Array" || m.Name != "Initialize"), "copied fixture bypassed its copied boundary");
            var entry = app.MainModule.GetType("Graph.CopiedArrayEntry");
            bool attributed = mode is "framework-attribute-named" or "framework-attribute-unarmed" or "framework-attribute-same-name";
            Require(app.MainModule.GetTypes().Where(t => t != entry).All(t => t.CustomAttributes.Count == 0)
                && entry.CustomAttributes.Count == (attributed ? 1 : 0), "copied fixture introduced an unrelated or preservation attribute");
            if (attributed)
            {
                var attribute = entry.CustomAttributes.Single();
                Require(attribute.AttributeType.FullName == "Graph.CopiedBoundaryAttribute"
                    && attribute.AttributeType.Scope is AssemblyNameReference scope && scope.Name == "System.ArrayFacade"
                    && attribute.ConstructorArguments.Count == 1 && attribute.ConstructorArguments[0].Type.FullName == "System.Int32"
                    && attribute.ConstructorArguments[0].Value is int argument && argument == 7
                    && attribute.Fields.Count == 0 && attribute.Properties.Count == 0, "copied fixture changed its framework attribute contract");
            }
            Require(entry.Fields.Single(f => f.Name == "Layout").FieldType.FullName == "Graph.CopiedArrayValue", "copied fixture lost its value-layout root");
            var main = entry.Methods.Single(m => m.Name == "Main");
            var named = main.Body.Instructions.Where(i => i.OpCode == OpCodes.Ldtoken && i.Operand is TypeReference type
                && type.FullName == "Graph.CopiedBoundaryAttribute").ToArray();
            bool rootNamed = CopiedRootAttributeMode(mode) && mode != "copied-type-attribute-unnamed";
            Require(named.Length == (mode is "framework-attribute-named" or "framework-attribute-same-name" || rootNamed ? 1 : 0), "copied fixture changed its framework attribute naming root");
            if (named.Length != 0)
                Require(named[0].Operand is TypeReference namedType && namedType.Scope is AssemblyNameReference namedScope
                    && namedScope.Name == (mode == "framework-attribute-same-name" ? "System.ArrayLeaf" : "System.ArrayFacade")
                    && named[0].Next.Operand is MethodReference fromHandle && fromHandle.DeclaringType.FullName == "System.Type"
                    && fromHandle.Name == "GetTypeFromHandle" && named[0].Next.Next.OpCode == OpCodes.Pop, "copied fixture changed its discarded attribute type token");
            var queries = main.Body.Instructions.Where(i => i.OpCode == OpCodes.Ldtoken && i.Operand is TypeReference type
                && type.FullName == "Graph.CopiedArrayEntry").ToArray();
            Require(queries.Length == (attributed ? 1 : 0), "copied fixture changed its attribute query receiver");
            if (queries.Length != 0)
            {
                var call = queries[0].Next.Next.Next;
                Require(queries[0].Next.Operand is MethodReference entryFromHandle && entryFromHandle.DeclaringType.FullName == "System.Type"
                    && entryFromHandle.Name == "GetTypeFromHandle" && queries[0].Next.Next.OpCode == OpCodes.Ldc_I4_0
                    && call.OpCode == OpCodes.Callvirt && call.Operand is MethodReference query
                    && query.DeclaringType.FullName == "System.Reflection.MemberInfo" && query.Name == "GetCustomAttributes"
                    && query.Parameters.Count == 1 && query.Parameters[0].ParameterType.FullName == "System.Boolean"
                    && call.Next.OpCode == OpCodes.Pop, "copied fixture changed its executable attribute-construction capability");
            }
            string target = mode switch { "direct" or "ldftn-delegate" => "Initialize", "copied-to-copied" => "Forward", "cctor" => "Enter", _ => "Noop" };
            var roots = main.Body.Instructions.Where(i => i.Operand is MethodReference method && method.DeclaringType.Namespace == "Graph").ToArray();
            if (CopiedDemandMode(mode)) CheckCopiedDemandInput(directory, mode, app);
            else Require(roots.Length == 1 && roots[0].Operand is MethodReference root && root.Name == target
                && root.DeclaringType.FullName == (mode is "copied-preserve-inactive" or "copied-preserve-signature" or "copied-preserve-named" ? "Graph.CopiedArrayLeaf" : mode == "cctor" ? "Graph.CopiedArrayCctor" : "Graph.CopiedArrayFacade")
                && roots[0].OpCode == (mode == "ldftn-delegate" ? OpCodes.Ldftn : OpCodes.Call), "copied fixture lost its actual copied-method root");
            if (CopiedRootAttributeMode(mode) || CopiedPreservationMode(mode)) CheckCopiedRootInput(directory, mode, app);
        }
        else
        {
            Require(ExactStub(value!.Methods.Single(m => m.IsConstructor && m.Parameters.Count == 1))
                && ExactStub(value.Methods.Single(m => m.Name == "Ordinary")), "copied array opened overload or ordinary bodies");
            var reference = app.MainModule.GetType("Graph.CopiedArrayReference");
            Require(reference is not null && reference.Methods.All(m => !Body(m) && (!m.HasBody || ExactStub(m))), "copied array opened reference metadata bodies");
            foreach (string name in new[] { "CopiedArrayOverloadDependency", "CopiedArrayOrdinaryDependency", "CopiedArrayReferenceDependency", "CopiedArrayUnusedProvider" })
                Require(library.MainModule.GetType("Graph." + name) is null, "copied array opened a negative provider/body: " + name);
        }
        Console.WriteLine("graph-copied-array=" + state + ":" + mode + ":default-constructor,unique-external-dependency,copied-boundary,metadata-negatives");
    }

    private static void CheckCopiedDemandInput(string directory, string mode, AssemblyDefinition app)
    {
        using var facade = AssemblyDefinition.ReadAssembly(Path.Combine(directory, "System.ArrayFacade.dll"));
        var owner = facade.MainModule.GetType("Graph.CopiedDemandOwner");
        Require(owner is not null && owner.BaseType.FullName == "System.Object" && !owner.HasGenericParameters,
            "copied demand lost its ordinary copied owner");
        var entry = app.MainModule.GetType("Graph.CopiedArrayEntry");
        var main = entry.Methods.Single(m => m.Name == "Main");
        var helper = entry.Methods.Single(m => m.Name == "Demand");
        Require(helper.IsStatic && helper.GenericParameters.Count == 1 && helper.Parameters.Count == 0
            && helper.ReturnType.FullName == "System.Void", "copied demand lost its generic helper");
        var roots = main.Body.Instructions.Where(i => i.Operand is MethodReference m && m.DeclaringType.Namespace == "Graph").ToArray();
        Require(roots.Length == 2 && roots[0].Operand is GenericInstanceMethod closed && closed.ElementMethod.Name == "Demand"
            && closed.ElementMethod.DeclaringType.FullName == "Graph.CopiedArrayEntry" && closed.GenericArguments.Count == 1
            && closed.GenericArguments[0].FullName == owner!.FullName && closed.GenericArguments[0].Scope is AssemblyNameReference scope
            && scope.Name == "System.ArrayFacade" && roots[0].OpCode == (mode == "copied-demand-formed" ? OpCodes.Ldtoken : OpCodes.Call)
            && (mode != "copied-demand-formed" || roots[0].Next.OpCode == OpCodes.Pop)
            && roots[1].OpCode == OpCodes.Call && roots[1].Operand is MethodReference noop
            && noop.DeclaringType.FullName == "Graph.CopiedArrayFacade" && noop.Name == "Noop",
            "copied demand changed its executable or formed-only binding");
        var arrays = main.Body.Instructions.Where(i => i.OpCode == OpCodes.Newarr).ToArray();
        Require(arrays.Length == 1 && arrays[0].Operand is TypeReference value && value.FullName == "Graph.CopiedArrayValue"
            && arrays[0].Previous.OpCode == OpCodes.Ldc_I4_1 && arrays[0].Next.OpCode == OpCodes.Pop,
            "copied demand lost its nonconstructing application array layout");
        var tokens = main.Body.Instructions.Where(i => i.OpCode == OpCodes.Ldtoken && i.Operand is TypeReference).ToArray();
        Require(tokens.Length == (mode == "copied-demand-event-attribute" ? 1 : 0), "copied demand gained a concrete typeof root");
        if (tokens.Length != 0)
            Require(tokens[0].Operand is TypeReference attributeType && attributeType.FullName == "Graph.CopiedDemandAttribute"
                && attributeType.Scope is AssemblyNameReference attributeScope && attributeScope.Name == "System.ArrayFacade"
                && tokens[0].Next.Operand is MethodReference namedFromHandle && namedFromHandle.Name == "GetTypeFromHandle"
                && tokens[0].Next.Next.OpCode == OpCodes.Pop, "copied event attribute lost its explicit naming control");
        var code = helper.Body.Instructions.ToArray();
        if (mode is "copied-demand-default" or "copied-demand-formed")
            Require(code.Select(i => i.OpCode.Code).SequenceEqual(new[] { Code.Call, Code.Pop, Code.Ret })
                && code[0].Operand is GenericInstanceMethod genericActivate && genericActivate.DeclaringType.FullName == "System.Activator"
                && genericActivate.Name == "CreateInstance" && genericActivate.Parameters.Count == 0 && genericActivate.GenericArguments.Count == 1
                && genericActivate.GenericArguments[0] == helper.GenericParameters[0], "copied default-constructor demand changed its symbolic argument");
        else
        {
            Require(code[0].OpCode == OpCodes.Ldtoken && code[0].Operand == helper.GenericParameters[0]
                && code[1].OpCode == OpCodes.Call && code[1].Operand is MethodReference symbolicFromHandle
                && symbolicFromHandle.DeclaringType.FullName == "System.Type" && symbolicFromHandle.Name == "GetTypeFromHandle",
                "copied reflection demand lost its symbolic receiver");
            if (mode == "copied-demand-construct")
                Require(code.Select(i => i.OpCode.Code).SequenceEqual(new[] { Code.Ldtoken, Code.Call, Code.Call, Code.Pop, Code.Ret })
                    && code[2].Operand is MethodReference activate && activate.DeclaringType.FullName == "System.Activator"
                    && activate.Name == "CreateInstance" && activate.Parameters.Count == 1 && activate.Parameters[0].ParameterType.FullName == "System.Type",
                    "copied constructor demand changed its non-generic activation API");
            else
            {
                string kind = mode.Contains("-method", StringComparison.Ordinal) ? "Method"
                    : mode.Contains("-property", StringComparison.Ordinal) ? "Property"
                    : mode.Contains("-event", StringComparison.Ordinal) ? "Event" : "Field";
                bool lookupOnly = mode.EndsWith("-lookup", StringComparison.Ordinal);
                var expected = new List<Code> { Code.Ldtoken, Code.Call, Code.Ldstr, Code.Callvirt };
                if (!lookupOnly)
                {
                    expected.Add(Code.Ldnull);
                    if (kind is "Method" or "Event") expected.Add(Code.Ldnull);
                    expected.Add(Code.Callvirt);
                }
                if (lookupOnly || kind != "Event") expected.Add(Code.Pop);
                expected.Add(Code.Ret);
                Require(code.Select(i => i.OpCode.Code).SequenceEqual(expected)
                    && (string)code[2].Operand == (kind == "Method" ? "Run" : kind == "Event" ? "Changed" : "Value")
                    && code[3].Operand is MethodReference lookup && lookup.DeclaringType.FullName == "System.Type"
                    && lookup.Name == "Get" + kind && lookup.Parameters.Count == 1 && lookup.Parameters[0].ParameterType.FullName == "System.String",
                    "copied reflection demand changed its lookup or executable IL");
                if (!lookupOnly)
                {
                    var call = (MethodReference)code[kind is "Method" or "Event" ? 6 : 5].Operand;
                    Require(call.DeclaringType.FullName == "System.Reflection." + (kind == "Method" ? "MethodBase" : kind + "Info")
                        && call.Name == (kind == "Method" ? "Invoke" : kind == "Event" ? "AddEventHandler" : "GetValue")
                        && call.Parameters.Select(p => p.ParameterType.FullName).SequenceEqual(kind == "Method"
                            ? new[] { "System.Object", "System.Object[]" } : kind == "Event"
                            ? new[] { "System.Object", "System.Delegate" } : new[] { "System.Object" }),
                        "copied reflection demand changed its supported execution overload");
                }
            }
        }
        bool CallsInitialize(MethodDefinition method) => method.Body.Instructions.Any(i => i.Operand is MethodReference call
            && call.DeclaringType.FullName == "Graph.CopiedArrayFacade" && call.Name == "Initialize");
        var capability = owner!.Methods.Where(CallsInitialize).Select(m => m.Name).ToArray();
        string? selected = mode is "copied-demand-default" or "copied-demand-construct" or "copied-demand-formed" ? ".ctor"
            : mode is "copied-demand-method" or "copied-demand-method-lookup" ? "Run"
            : mode is "copied-demand-property" or "copied-demand-property-lookup" ? "get_Value"
            : mode is "copied-demand-event" or "copied-demand-event-lookup" ? "add_Changed" : null;
        Require(capability.SequenceEqual(selected is null ? Array.Empty<string>() : new[] { selected })
            && owner.Methods.All(m => !m.IsConstructor || !m.IsStatic), "copied demand gained an independent initializer capability");
        if (mode is "copied-demand-field" or "copied-demand-field-lookup")
        {
            var field = owner.Fields.Single();
            using var shim = AssemblyDefinition.ReadAssembly(Path.Combine(directory, "DnZlib.dll"));
            var payload = shim.MainModule.GetType("Graph.CopiedDemandFieldValue");
            Require(field.IsStatic && !field.IsLiteral && field.FieldType.FullName == payload.FullName && payload.IsValueType
                && field.FieldType.Scope is AssemblyNameReference payloadScope && payloadScope.Name == "DnZlib"
                && facade.MainModule.GetType("Graph.CopiedDemandFieldValue") is null
                && payload.Methods.Count == 1 && payload.Methods[0].Name == "ToString" && payload.Methods[0].IsVirtual
                && !payload.Methods[0].IsNewSlot && CallsInitialize(payload.Methods[0])
                && payload.Methods[0].Body.Instructions.Select(i => i.OpCode.Code).SequenceEqual(new[] { Code.Ldc_I4_1, Code.Newarr, Code.Call, Code.Ldstr, Code.Ret })
                && payload.Methods[0].Body.Instructions[2].Operand is MethodReference initialize
                && initialize.DeclaringType.Scope is AssemblyNameReference initializerScope && initializerScope.Name == "System.ArrayFacade",
                "copied field demand lost its boxed virtual-body discriminator");
        }
        if (mode == "copied-demand-event-attribute")
        {
            var item = owner.Events.Single();
            var attribute = facade.MainModule.GetType("Graph.CopiedDemandAttribute");
            Require(item.AddMethod.Body.Instructions.Select(i => i.OpCode.Code).SequenceEqual(new[] { Code.Ret })
                && item.RemoveMethod.Body.Instructions.Select(i => i.OpCode.Code).SequenceEqual(new[] { Code.Ret })
                && item.CustomAttributes.Count == 1 && item.CustomAttributes[0].AttributeType.FullName == attribute.FullName
                && item.CustomAttributes[0].ConstructorArguments.Count == 0 && item.CustomAttributes[0].Fields.Count == 0
                && item.CustomAttributes[0].Properties.Count == 1 && item.CustomAttributes[0].Properties[0].Name == "Arm"
                && item.CustomAttributes[0].Properties[0].Argument.Value is int argument && argument == 7
                && CallsInitialize(attribute.Methods.Single(m => m.Name == ".ctor"))
                && CallsInitialize(attribute.Methods.Single(m => m.Name == "set_Arm"))
                && attribute.Methods.Single(m => m.Name == "get_Arm").Body.Instructions.Select(i => i.OpCode.Code).SequenceEqual(new[] { Code.Ldc_I4_7, Code.Ret }),
                "copied event execution lost its real nonexecuted constructor/setter/getter controls");
        }
    }

    private static void CheckCopiedRootInput(string directory, string mode, AssemblyDefinition app)
    {
        using var facade = AssemblyDefinition.ReadAssembly(Path.Combine(directory, "System.ArrayFacade.dll"));
        using var leaf = AssemblyDefinition.ReadAssembly(Path.Combine(directory, "System.ArrayLeaf.dll"));
        var owner = facade.MainModule.GetType("Graph.CopiedArrayFacade");
        var entry = app.MainModule.GetType("Graph.CopiedArrayEntry");
        var main = entry.Methods.Single(m => m.Name == "Main");
        var code = main.Body.Instructions.ToArray();
        if (CopiedPreservationMode(mode))
        {
            var descriptor = XDocument.Load(Path.Combine(directory, "link.xml")).Root!;
            Require(descriptor.Name == "linker" && !descriptor.HasAttributes && descriptor.Elements().Count() == (mode == "copied-preserve-signature" ? 2 : 1), "copied preservation changed its descriptor root");
            var assembly = descriptor.Elements().Last();
            var type = assembly.Elements().Single();
            var method = type.Elements().Single();
            Require(assembly.Name == "assembly" && assembly.Attributes().Count() == 2 && (string?)assembly.Attribute("fullname") == "System.ArrayFacade"
                && (string?)assembly.Attribute("preserve") == "nothing" && assembly.Elements().Count() == 1
                && type.Name == "type" && (string?)type.Attribute("fullname") == "Graph.CopiedArrayFacade"
                && (string?)type.Attribute("preserve") == "nothing" && type.Elements().Count() == 1
                && type.Attributes().Count() == (mode == "copied-preserve-method" ? 2 : 3)
                && (string?)type.Attribute("required") == (mode == "copied-preserve-method" ? null : "false")
                && method.Name == "method" && method.Attributes().Count() == 1 && (string?)method.Attribute("name") == "Initialize"
                && !method.HasElements, "copied preservation changed its selected method or conditional activation");
            Require(code.All(i => i.OpCode != OpCodes.Ldtoken), "copied preservation gained an independent type token");
            var namedLookups = code.Where(i => i.Operand is MethodReference call && call.DeclaringType.FullName == "System.Type"
                && call.Name == "GetType").ToArray();
            Require(namedLookups.Length == (mode == "copied-preserve-named" ? 1 : 0), "copied preservation changed its named type identity roots");
            if (mode == "copied-preserve-named")
                Require(namedLookups[0].OpCode == OpCodes.Call && namedLookups[0].Operand is MethodReference namedLookup
                    && namedLookup.Parameters.Count == 1 && namedLookup.Parameters[0].ParameterType.FullName == "System.String"
                    && namedLookups[0].Previous.OpCode == OpCodes.Ldstr
                    && (string)namedLookups[0].Previous.Operand == "Graph.CopiedArrayFacade, System.ArrayFacade"
                    && namedLookups[0].Next.OpCode == OpCodes.Pop,
                    "copied conditional activation lost its discarded named type identity");
            Require(leaf.MainModule.GetType("Graph.CopiedArrayLeaf").Methods.Single(m => m.Name == "Noop").Body.Instructions
                .Select(i => i.OpCode.Code).SequenceEqual(new[] { Code.Ret }), "inactive preservation gained a facade execution edge");
            if (mode is "copied-preserve-inactive" or "copied-preserve-named")
                Require(app.MainModule.AssemblyReferences.All(r => r.Name != "System.ArrayFacade"), "inactive preservation gained a facade metadata use");
            if (mode == "copied-preserve-signature")
            {
                var signatureAssembly = descriptor.Elements().First();
                var signatureType = signatureAssembly.Elements().Single();
                var signatureMethod = signatureType.Elements().Single();
                Require(signatureAssembly.Name == "assembly" && signatureAssembly.Attributes().Count() == 2
                    && (string?)signatureAssembly.Attribute("fullname") == "GraphApp" && (string?)signatureAssembly.Attribute("preserve") == "nothing"
                    && signatureType.Name == "type" && signatureType.Attributes().Count() == 2
                    && (string?)signatureType.Attribute("fullname") == "Graph.CopiedArrayEntry" && (string?)signatureType.Attribute("preserve") == "nothing"
                    && signatureMethod.Name == "method" && signatureMethod.Attributes().Count() == 1
                    && (string?)signatureMethod.Attribute("name") == "PreservedSignature" && !signatureMethod.HasElements,
                    "copied conditional activation lost its preserved-signature policy");
                var signature = entry.Methods.Single(m => m.Name == "PreservedSignature");
                Require(signature.ReturnType.FullName == "Graph.CopiedArrayFacade" && signature.ReturnType.Scope is AssemblyNameReference signatureScope
                    && signatureScope.Name == "System.ArrayFacade" && signature.Parameters.Count == 0
                    && signature.Body.Instructions.Select(i => i.OpCode.Code).SequenceEqual(new[] { Code.Ldnull, Code.Ret }),
                    "copied conditional activation changed its uncalled null-returning signature witness");
            }
            return;
        }
        var providers = new List<ICustomAttributeProvider> { facade, facade.MainModule };
        foreach (var type in facade.MainModule.GetTypes())
        {
            providers.Add(type);
            providers.AddRange(type.Fields);
            providers.AddRange(type.Properties);
            providers.AddRange(type.Events);
            foreach (var method in type.Methods)
            {
                providers.Add(method);
                providers.Add(method.MethodReturnType);
                providers.AddRange(method.Parameters);
            }
        }
        ICustomAttributeProvider selected = mode == "copied-assembly-attribute" ? facade
            : mode == "copied-method-attribute" ? owner.Methods.Single(m => m.Name == "AttributedMember")
            : mode == "copied-parameter-attribute" ? owner.Methods.Single(m => m.Name == "AttributedMember").Parameters.Single()
            : mode == "copied-property-attribute-data" ? owner.Properties.Single(p => p.Name == "AttributedProperty")
            : mode == "copied-event-attribute-data" ? owner.Events.Single(e => e.Name == "AttributedEvent") : owner;
        Require(selected.CustomAttributes.Count == 1 && providers.Sum(p => p.CustomAttributes.Count) == 1, "copied attribute row moved to an unrelated provider");
        var row = selected.CustomAttributes.Single();
        Require(row.AttributeType.FullName == "Graph.CopiedBoundaryAttribute" && row.AttributeType.Scope is ModuleDefinition scope
            && scope.Assembly.Name.Name == "System.ArrayFacade"
            && row.ConstructorArguments.Count == 1 && row.ConstructorArguments[0].Type.FullName == "System.Int32"
            && row.ConstructorArguments[0].Value is int argument && argument == 7 && row.Fields.Count == 0
            && row.Properties.Count == (CopiedNamedSetterMode(mode) ? 1 : 0), "copied owner attribute lost its original scoped row");
        var boundary = facade.MainModule.GetType("Graph.CopiedBoundaryAttribute");
        var ctor = boundary.Methods.Single(m => m.IsConstructor && !m.IsStatic);
        Require(ctor.Body.Instructions.Select(i => i.OpCode.Code).SequenceEqual(mode == "copied-type-attribute-setter"
            ? new[] { Code.Ldarg_0, Code.Call, Code.Ret }
            : new[] { Code.Ldarg_0, Code.Call, Code.Ldc_I4_1, Code.Newarr, Code.Call, Code.Ret }), "copied owner attribute changed its constructor capability");
        if (CopiedNamedSetterMode(mode))
        {
            var property = boundary.Properties.Single();
            Require(property.Name == "Arm" && property.GetMethod is { } getter
                && getter.Body.Instructions.Select(i => i.OpCode.Code).SequenceEqual(new[] { Code.Ldc_I4_7, Code.Ret })
                && property.SetMethod is { } setter
                && setter.Body.Instructions.Select(i => i.OpCode.Code).SequenceEqual(new[] { Code.Ldc_I4_1, Code.Newarr, Code.Call, Code.Ret })
                && setter.Body.Instructions.Any(i => i.Operand is MethodReference call && call.Name == "Initialize"
                    && call.DeclaringType.FullName == "Graph.CopiedArrayFacade")
                && row.Properties.Single().Name == "Arm" && row.Properties.Single().Argument.Type.FullName == "System.Int32"
                && row.Properties.Single().Argument.Value is int value && value == 7, "copied attribute lost its named setter edge or uncalled getter control");
        }
        if (mode is "copied-method-attribute" or "copied-parameter-attribute")
            Require(owner.Methods.Single(m => m.Name == "AttributedMember").Body.Instructions.Select(i => i.OpCode.Code)
                .SequenceEqual(new[] { Code.Ret }), "the attribute-owning method gained an independent capability edge");
        if (mode == "copied-parameter-attribute")
            Require(owner.Methods.Single(m => m.Name == "AttributedMember").CustomAttributes.Count == 0
                && selected is ParameterDefinition parameter && parameter.ParameterType.FullName == "System.Int32",
                "copied parameter attribute moved from its parameter row to its method or parameter type");
        if (mode == "copied-property-attribute-data")
            Require(owner.Properties.Single().GetMethod.Body.Instructions.Select(i => i.OpCode.Code).SequenceEqual(new[] { Code.Ldc_I4_0, Code.Ret }),
                "copied property metadata gained an accessor execution edge");
        if (mode == "copied-event-attribute-data")
            Require(owner.Events.Single().AddMethod.Body.Instructions.Select(i => i.OpCode.Code).SequenceEqual(new[] { Code.Ret })
                && owner.Events.Single().RemoveMethod.Body.Instructions.Select(i => i.OpCode.Code).SequenceEqual(new[] { Code.Ret }),
                "copied event metadata gained an accessor execution edge");
        var receiver = code.Where(i => i.OpCode == OpCodes.Ldtoken && i.Operand is TypeReference type
            && type.FullName == "Graph.CopiedArrayFacade").ToArray();
        Require(receiver.Length == (mode == "copied-type-attribute-noquery" ? 0 : 1), "copied attribute query lost its known receiver");
        if (receiver.Length != 0)
            Require(receiver[0].Operand is TypeReference receiverType && receiverType.Scope is AssemblyNameReference receiverScope
                && receiverScope.Name == "System.ArrayFacade" && receiver[0].Next.Operand is MethodReference fromHandle
                && fromHandle.DeclaringType.FullName == "System.Type" && fromHandle.Name == "GetTypeFromHandle", "copied attribute receiver changed its defining scope");
        var queries = code.Where(i => i.Operand is MethodReference call && call.Name is "GetCustomAttributes" or "GetCustomAttributesData").ToArray();
        Require(queries.Length == (mode == "copied-type-attribute-noquery" ? 0 : 1), "copied attribute query execution block did not run");
        if (queries.Length != 0)
        {
            bool dataOnly = mode is "copied-type-attribute-data" or "copied-property-attribute-data" or "copied-event-attribute-data";
            string api = dataOnly ? "System.Reflection.MemberInfo"
                : mode == "copied-assembly-attribute" ? "System.Reflection.Assembly"
                : mode == "copied-parameter-attribute" ? "System.Reflection.ParameterInfo" : "System.Reflection.MemberInfo";
            var call = (MethodReference)queries[0].Operand;
            Require(call.DeclaringType.FullName == api && call.HasThis && queries[0].OpCode == OpCodes.Callvirt
                && queries[0].Next.OpCode == OpCodes.Pop
                && (dataOnly
                    ? call.Name == "GetCustomAttributesData" && call.Parameters.Count == 0
                    : call.Name == "GetCustomAttributes" && call.Parameters.Count == 1
                        && queries[0].Previous.OpCode == OpCodes.Ldc_I4_0
                        && call.Parameters[0].ParameterType.FullName == "System.Boolean"), "copied attribute read changed its construction or metadata-only contract");
            if (mode is "copied-method-attribute" or "copied-parameter-attribute")
                Require(code.Any(i => i.Operand is MethodReference lookup && lookup.Name == "GetMethod"
                    && i.Previous.OpCode == OpCodes.Ldstr && (string)i.Previous.Operand == "AttributedMember"), "copied member attribute lost its exact uncalled owner");
            if (mode == "copied-parameter-attribute")
                Require(code.Any(i => i.Operand is MethodReference lookup && lookup.Name == "GetParameters"
                    && lookup.DeclaringType.FullName == "System.Reflection.MethodBase" && i.Next.OpCode == OpCodes.Ldc_I4_0
                    && i.Next.Next.OpCode == OpCodes.Ldelem_Ref), "copied parameter attribute lost its selected parameter row");
            if (mode is "copied-property-attribute-data" or "copied-event-attribute-data")
            {
                string lookupName = mode == "copied-property-attribute-data" ? "GetProperty" : "GetEvent";
                string memberName = mode == "copied-property-attribute-data" ? "AttributedProperty" : "AttributedEvent";
                Require(code.Any(i => i.Operand is MethodReference lookup && lookup.Name == lookupName
                    && i.Previous.OpCode == OpCodes.Ldstr && (string)i.Previous.Operand == memberName), "copied accessor metadata lost its exact attributed row");
            }
            if (mode == "copied-assembly-attribute")
                Require(code.Any(i => i.Operand is MethodReference lookup && lookup.Name == "get_Assembly"), "copied assembly attribute lost its known module receiver");
            if (mode != "copied-type-attribute-unnamed")
            {
                int named = Array.FindIndex(code, i => i.OpCode == OpCodes.Ldtoken && i.Operand is TypeReference type
                    && type.FullName == "Graph.CopiedBoundaryAttribute");
                Require(named >= 0 && named < Array.IndexOf(code, queries[0]), "copied attribute changed its early naming route");
            }
        }
    }
}
