using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Xml.Linq;

static partial class GraphArrayValidation
{
    private static bool CopiedLateMode(string mode) => mode is "copied-late-layout" or "copied-early-layout"
        or "copied-layout-unarmed" or "copied-layout-unmodeled" or "copied-layout-framework"
        or "copied-filter-instance" or "copied-filter-setter" or "copied-filter-unmatched"
        or "copied-filter-noquery" or "copied-filter-framework"
        or "copied-filter-data" or "copied-filter-data-noquery" or "copied-filter-data-unmatched"
        or "copied-filter-data-inactive" or "copied-filter-data-upgrade";

    private static bool CopiedLateLayout(string mode) => mode.Contains("layout", StringComparison.Ordinal);
    private static bool CopiedLateData(string mode) => mode.StartsWith("copied-filter-data", StringComparison.Ordinal);
    private static bool CopiedLatePositive(string mode) => mode is "copied-late-layout" or "copied-early-layout";

    private static void CreateCopiedLate(string directory, string corelib, string mode)
    {
        Directory.CreateDirectory(directory);
        using var core = AssemblyDefinition.ReadAssembly(corelib);
        using var app = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition("GraphApp", new Version(1, 0)), "GraphApp", ModuleKind.Console);
        using var library = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition("GraphLib", new Version(1, 0)), "GraphLib", ModuleKind.Dll);
        using var facade = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition("System.ArrayFacade", new Version(1, 0)), "System.ArrayFacade", ModuleKind.Dll);
        using var leaf = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition("System.ArrayLeaf", new Version(1, 0)), "System.ArrayLeaf", ModuleKind.Dll);
        using var shim = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition("DnZlib", new Version(1, 0)), "DnZlib", ModuleKind.Dll);
        bool layout = CopiedLateLayout(mode), data = CopiedLateData(mode);
        TypeReference Core(ModuleDefinition module, string name) => module.ImportReference(core.MainModule.GetType("System." + name));
        MethodReference Api(ModuleDefinition module, string type, string name, params string[] parameters) => module.ImportReference(core.MainModule.GetType(type).Methods.Single(m => m.Name == name
            && m.Parameters.Select(p => p.ParameterType.FullName).SequenceEqual(parameters)));
        TypeDefinition Type(ModuleDefinition module, string name, bool value = false)
        {
            var type = new TypeDefinition("Graph", name, TypeAttributes.Public | (value
                ? TypeAttributes.Sealed | TypeAttributes.SequentialLayout | TypeAttributes.BeforeFieldInit : TypeAttributes.Class), Core(module, value ? "ValueType" : "Object"));
            module.Types.Add(type);
            return type;
        }
        MethodDefinition Method(TypeDefinition owner, string name, bool instance = false)
        {
            var method = new MethodDefinition(name, MethodAttributes.Public | (instance ? MethodAttributes.HideBySig : MethodAttributes.Static), Core(owner.Module, "Void"));
            if (name == ".ctor") method.Attributes |= MethodAttributes.SpecialName | MethodAttributes.RTSpecialName;
            owner.Methods.Add(method);
            return method;
        }
        void Dependency(MethodDefinition method, string name)
        {
            var dependency = Method(Type(library.MainModule, name), "Touch");
            dependency.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            method.Body.Instructions.Add(Instruction.Create(OpCodes.Call, method.Module.ImportReference(dependency)));
        }
        MethodDefinition Constructor(TypeDefinition type, string dependency, bool overload = false)
        {
            var constructor = Method(type, ".ctor", true);
            if (overload) constructor.Parameters.Add(new ParameterDefinition(Core(type.Module, "Int32")));
            if (!type.IsValueType)
            {
                constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
                constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Call, Api(type.Module, "System.Object", ".ctor")));
            }
            Dependency(constructor, dependency);
            constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            return constructor;
        }
        void Initialize(MethodDefinition method)
        {
            method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_1));
            method.Body.Instructions.Add(Instruction.Create(OpCodes.Newarr, Core(method.Module, "Int32")));
            method.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, Api(method.Module, "System.Array", "Initialize")));
        }
        void ActivateNamed(MethodDefinition method, string name)
        {
            method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldstr, "Graph." + name + ", GraphApp"));
            method.Body.Instructions.Add(Instruction.Create(OpCodes.Call, Api(method.Module, "System.Type", "GetType", "System.String")));
            method.Body.Instructions.Add(Instruction.Create(OpCodes.Call, Api(method.Module, "System.Activator", "CreateInstance", "System.Type")));
            method.Body.Instructions.Add(Instruction.Create(OpCodes.Pop));
        }
        var entry = Type(app.MainModule, "LateEntry");
        var target = Type(app.MainModule, layout ? "LateTarget" : "LateValue", !layout);
        if (!layout) target.Fields.Add(new FieldDefinition("Number", FieldAttributes.Public, Core(app.MainModule, "Int32")));
        Constructor(target, "LateDependency");
        if (!layout) Constructor(target, "LateOverloadDependency", true);
        var ordinary = Method(target, "Ordinary");
        Dependency(ordinary, "LateOrdinaryDependency");
        ordinary.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        entry.Fields.Add(new FieldDefinition("Layout", FieldAttributes.Public | FieldAttributes.Static, app.MainModule.ImportReference(target)));
        var reference = Type(app.MainModule, "LateReference");
        Constructor(reference, "LateReferenceDependency");
        entry.Fields.Add(new FieldDefinition("Reference", FieldAttributes.Public | FieldAttributes.Static, app.MainModule.ImportReference(reference)));
        foreach (string name in new[] { "LateAttributeCtorTarget", "LateAttributeSetterTarget", "LateAttributeGetterTarget" })
        {
            var forbidden = Type(app.MainModule, name);
            Constructor(forbidden, name + "Dependency");
            entry.Fields.Add(new FieldDefinition(name, FieldAttributes.Public | FieldAttributes.Static, app.MainModule.ImportReference(forbidden)));
        }
        var main = Method(entry, "Main");
        app.EntryPoint = main;
        foreach (string setter in new[] { "set_CurrentCulture", "set_CurrentUICulture" })
        {
            main.Body.Instructions.Add(Instruction.Create(OpCodes.Call, Api(app.MainModule, "System.Globalization.CultureInfo", "get_InvariantCulture")));
            main.Body.Instructions.Add(Instruction.Create(OpCodes.Call, Api(app.MainModule, "System.Globalization.CultureInfo", setter, "System.Globalization.CultureInfo")));
        }
        if (layout)
        {
            var overloadTarget = Type(app.MainModule, "LateCellOverloadTarget");
            Constructor(overloadTarget, "LateOverloadDependency");
            entry.Fields.Add(new FieldDefinition("CellOverload", FieldAttributes.Public | FieldAttributes.Static, overloadTarget));
            var cellModule = mode == "copied-layout-framework" ? facade.MainModule : shim.MainModule;
            var cell = Type(cellModule, "LateCell`1", true);
            var parameter = new GenericParameter("T", cell);
            cell.GenericParameters.Add(parameter);
            cell.Fields.Add(new FieldDefinition("Value", FieldAttributes.Public, parameter));
            var constructor = Method(cell, ".ctor", true);
            ActivateNamed(constructor, "LateTarget");
            constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            var overloadConstructor = Method(cell, ".ctor", true);
            overloadConstructor.Parameters.Add(new ParameterDefinition(Core(cellModule, "Int32")));
            ActivateNamed(overloadConstructor, "LateCellOverloadTarget");
            overloadConstructor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            var arm = Method(entry, "Arm");
            Initialize(arm);
            arm.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            var model = Method(entry, "Model");
            var closed = new GenericInstanceType(app.MainModule.ImportReference(cell));
            closed.GenericArguments.Add(Core(app.MainModule, "Int32"));
            model.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_1));
            model.Body.Instructions.Add(Instruction.Create(OpCodes.Newarr, closed));
            model.Body.Instructions.Add(Instruction.Create(OpCodes.Pop));
            model.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            if (mode == "copied-early-layout") main.Body.Instructions.Add(Instruction.Create(OpCodes.Call, model));
            if (mode != "copied-layout-unarmed") main.Body.Instructions.Add(Instruction.Create(OpCodes.Call, arm));
            if (mode is not ("copied-early-layout" or "copied-layout-unmodeled")) main.Body.Instructions.Add(Instruction.Create(OpCodes.Call, model));
        }
        else
        {
            var attributeModule = mode == "copied-filter-framework" ? facade.MainModule : shim.MainModule;
            var attribute = Type(attributeModule, "LateSpecialAttribute");
            attribute.BaseType = Core(attributeModule, "Attribute");
            var attributeCtor = Method(attribute, ".ctor", true);
            if (data) attributeCtor.Parameters.Add(new ParameterDefinition(Core(attributeModule, "Type")));
            attributeCtor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            attributeCtor.Body.Instructions.Add(Instruction.Create(OpCodes.Call, Api(attributeModule, "System.Attribute", ".ctor")));
            if (data) ActivateNamed(attributeCtor, "LateAttributeCtorTarget");
            else if (mode != "copied-filter-setter") Initialize(attributeCtor);
            attributeCtor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            var setter = Method(attribute, "set_Arm", true);
            setter.Attributes |= MethodAttributes.SpecialName;
            setter.Parameters.Add(new ParameterDefinition(Core(attributeModule, "Int32")));
            if (data) ActivateNamed(setter, "LateAttributeSetterTarget");
            else Initialize(setter);
            setter.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            var getter = Method(attribute, "get_Arm", true);
            getter.Attributes |= MethodAttributes.SpecialName;
            getter.ReturnType = Core(attributeModule, "Int32");
            ActivateNamed(getter, "LateAttributeGetterTarget");
            getter.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_7));
            getter.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            attribute.Properties.Add(new PropertyDefinition("Arm", PropertyAttributes.None, Core(attributeModule, "Int32")) { GetMethod = getter, SetMethod = setter });
            var unmatched = Type(attributeModule, "LateOtherAttribute");
            unmatched.BaseType = Core(attributeModule, "Attribute");
            var unmatchedCtor = Method(unmatched, ".ctor", true);
            unmatchedCtor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            unmatchedCtor.Body.Instructions.Add(Instruction.Create(OpCodes.Call, Api(attributeModule, "System.Attribute", ".ctor")));
            unmatchedCtor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            var rowModule = data ? library.MainModule : attributeModule;
            var row = new CustomAttribute(rowModule.ImportReference(attributeCtor));
            row.Properties.Add(new CustomAttributeNamedArgument("Arm", new CustomAttributeArgument(Core(rowModule, "Int32"), 7)));
            if (data)
            {
                var conditional = Type(shim.MainModule, "LateConditionalFactory");
                var initialize = Method(conditional, "Initialize");
                Initialize(initialize);
                initialize.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
                var inert = Type(shim.MainModule, "LateUnrelatedIdentity");
                row.ConstructorArguments.Add(new CustomAttributeArgument(Core(rowModule, "Type"), rowModule.ImportReference(mode == "copied-filter-data-inactive" ? inert : conditional)));
                Type(library.MainModule, "LateUnusedProvider").CustomAttributes.Add(row);
                File.WriteAllText(Path.Combine(directory, "link.xml"), "<linker>\n  <assembly fullname=\"DnZlib\" preserve=\"nothing\">\n"
                    + "    <type fullname=\"Graph.LateConditionalFactory\" preserve=\"nothing\" required=\"0\">\n"
                    + "      <method name=\"Initialize\" />\n    </type>\n  </assembly>\n</linker>\n");
            }
            else attributeModule.Assembly.CustomAttributes.Add(row);
            var read = Method(entry, "Read");
            string ownerName = data ? "System.Reflection.MemberInfo" : "System.Reflection.Assembly";
            read.Parameters.Add(new ParameterDefinition(app.MainModule.ImportReference(core.MainModule.GetType(ownerName))));
            void ReadAttributes(bool construct)
            {
                read.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
                read.Body.Instructions.Add(Instruction.Create(OpCodes.Ldtoken, app.MainModule.ImportReference(mode.EndsWith("-unmatched", StringComparison.Ordinal) ? unmatched : attribute)));
                read.Body.Instructions.Add(Instruction.Create(OpCodes.Call, Api(app.MainModule, "System.Type", "GetTypeFromHandle", "System.RuntimeTypeHandle")));
                read.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_0));
                read.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, Api(app.MainModule, ownerName, construct ? "GetCustomAttributes" : "IsDefined", "System.Type", "System.Boolean")));
                read.Body.Instructions.Add(Instruction.Create(OpCodes.Pop));
            }
            if (!mode.EndsWith("-noquery", StringComparison.Ordinal)) ReadAttributes(!data);
            if (mode == "copied-filter-data-upgrade") ReadAttributes(true);
            read.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            main.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_1));
            main.Body.Instructions.Add(Instruction.Create(OpCodes.Newarr, target));
            main.Body.Instructions.Add(Instruction.Create(OpCodes.Pop));
            main.Body.Instructions.Add(Instruction.Create(OpCodes.Ldtoken, entry));
            main.Body.Instructions.Add(Instruction.Create(OpCodes.Call, Api(app.MainModule, "System.Type", "GetTypeFromHandle", "System.RuntimeTypeHandle")));
            if (!data) main.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, Api(app.MainModule, "System.Type", "get_Assembly")));
            main.Body.Instructions.Add(Instruction.Create(OpCodes.Call, read));
        }
        main.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        foreach (var assembly in new[] { app, library, facade, leaf, shim })
            assembly.Write(Path.Combine(directory, assembly.Name.Name + ".dll"), new WriterParameters { Timestamp = 0, DeterministicMvid = true });
    }

    private static void CheckCopiedLate(string directory, string mode, string state)
    {
        Require(state is "original" or "published" or "observed-before", "unknown late copied fixture state");
        using var app = AssemblyDefinition.ReadAssembly(Path.Combine(directory, "GraphApp.dll"));
        using var library = AssemblyDefinition.ReadAssembly(Path.Combine(directory, "GraphLib.dll"));
        using var facade = AssemblyDefinition.ReadAssembly(Path.Combine(directory, "System.ArrayFacade.dll"));
        using var leaf = AssemblyDefinition.ReadAssembly(Path.Combine(directory, "System.ArrayLeaf.dll"));
        using var shim = AssemblyDefinition.ReadAssembly(Path.Combine(directory, "DnZlib.dll"));
        foreach (var copied in new[] { facade, leaf, shim })
        {
            Require(copied.MainModule.AssemblyReferences.All(r => r.Name is "System.Private.CoreLib" or "System.ArrayFacade" or "System.ArrayLeaf" or "DnZlib"),
                "late copied fixture gained a direct stripped reference");
            Require(copied.MainModule.GetTypes().All(t => t.Methods.All(m => !m.IsConstructor || !m.IsStatic)), "late copied fixture gained a static initializer");
        }
        bool layout = CopiedLateLayout(mode), data = CopiedLateData(mode), original = state == "original", observed = state == "observed-before";
        var target = app.MainModule.GetType("Graph." + (layout ? "LateTarget" : "LateValue"));
        var constructor = target?.Methods.SingleOrDefault(m => m.IsConstructor && !m.IsStatic && m.Parameters.Count == 0);
        bool Body(MethodDefinition? method) => method is { HasBody: true } && !Stub(method);
        bool ExactStub(MethodDefinition? method) => method is not null && Stub(method)
            && method.Body.Instructions[0].Operand is int marker && marker == 0x494C4454;
        Require(target is not null && constructor is not null, "late copied fixture lost modeled target metadata");
        bool actual = Body(constructor), expected = original || CopiedLatePositive(mode);
        bool dependency = Body(library.MainModule.GetType("Graph.LateDependency")?.Methods.SingleOrDefault());
        Require(observed || actual == expected, "late copied target constructor state: " + state + ":" + mode);
        Require(observed || dependency == expected, "late copied unique dependency state: " + state + ":" + mode);
        if (!actual)
        {
            Require(ExactStub(constructor), "late copied target missing an exact marked stub");
            Require(library.MainModule.GetType("Graph.LateDependency") is null, "late copied missing-body witness kept its unique dependency");
        }
        if (observed)
            Console.WriteLine("graph-copied-late-observed-before=" + mode + ":constructor-body=" + actual + ":dependency-body=" + dependency);
        foreach (string name in new[] { "LateAttributeCtorTarget", "LateAttributeSetterTarget", "LateAttributeGetterTarget" })
        {
            var type = app.MainModule.GetType("Graph." + name);
            var ctor = type?.Methods.SingleOrDefault(m => m.IsConstructor && m.Parameters.Count == 0);
            bool runs = original;
            if (!observed)
            {
                Require(Body(ctor) == runs && (runs || ExactStub(ctor)), "late attribute target execution: " + state + ":" + name);
                Require(Body(library.MainModule.GetType("Graph." + name + "Dependency")?.Methods.SingleOrDefault()) == runs,
                    "late attribute target dependency execution: " + state + ":" + name);
                if (!runs) Require(library.MainModule.GetType("Graph." + name + "Dependency") is null, "late attribute retained a forbidden dependency");
            }
            else Console.WriteLine("graph-copied-late-observed-forbidden=" + mode + ":" + name + ":constructor-body=" + Body(ctor));
        }
        if (!original)
        {
            Require(ExactStub(target!.Methods.Single(m => m.Name == "Ordinary")), "late copied demand opened an ordinary target body");
            if (layout)
                Require(ExactStub(app.MainModule.GetType("Graph.LateCellOverloadTarget")?.Methods.SingleOrDefault()),
                    "late copied initializer opened the copied constructor overload target");
            else
                Require(ExactStub(target.Methods.Single(m => m.IsConstructor && m.Parameters.Count == 1)),
                    "late copied initializer opened the value constructor overload");
            Require(ExactStub(app.MainModule.GetType("Graph.LateReference")?.Methods.SingleOrDefault()), "late copied demand opened a reference metadata constructor");
            foreach (string name in new[] { "LateOverloadDependency", "LateOrdinaryDependency", "LateReferenceDependency" })
                Require(library.MainModule.GetType("Graph." + name) is null, "late copied demand opened an unrelated dependency");
        }
        else CheckCopiedLateOriginal(directory, mode, app, library, facade, shim);
        Console.WriteLine("graph-copied-array=" + state + ":" + mode + ":original-boundary,target-constructor,unique-dependency,strict-negatives");
    }

    private static void CheckCopiedLateOriginal(string directory, string mode, AssemblyDefinition app, AssemblyDefinition library,
        AssemblyDefinition facade, AssemblyDefinition shim)
    {
        bool layout = CopiedLateLayout(mode), data = CopiedLateData(mode);
        var entry = app.MainModule.GetType("Graph.LateEntry");
        var main = app.EntryPoint;
        Require(main.DeclaringType == entry && main.Name == "Main" && main.Parameters.Count == 0, "late copied entry changed");
        var mainCode = main.Body.Instructions.ToArray();
        Require(mainCode.Take(4).All(i => i.OpCode == OpCodes.Call)
            && mainCode.Take(4).Select(i => ((MethodReference)i.Operand).Name).SequenceEqual(new[] { "get_InvariantCulture", "set_CurrentCulture", "get_InvariantCulture", "set_CurrentUICulture" }),
            "late copied entry did not pin cultures first");
        var appCode = app.MainModule.GetTypes().SelectMany(t => t.Methods).Where(m => m.HasBody).SelectMany(m => m.Body.Instructions).ToArray();
        Require(appCode.All(i => i.OpCode != OpCodes.Newobj && i.OpCode != OpCodes.Initobj && i.OpCode != OpCodes.Box), "late copied fixture gained an artificial construction root");
        foreach (var method in app.MainModule.GetTypes().SelectMany(t => t.Methods).Where(m => m.HasBody))
        {
            var constructorCalls = method.Body.Instructions.Where(i => i.Operand is MethodReference called && called.Name == ".ctor").ToArray();
            bool referenceConstructor = method.IsConstructor && !method.IsStatic && !method.DeclaringType.IsValueType;
            Require(constructorCalls.Length == (referenceConstructor ? 1 : 0), "late copied fixture directly calls an application constructor");
            if (referenceConstructor)
                Require(method.Body.Instructions[0].OpCode == OpCodes.Ldarg_0 && method.Body.Instructions[1] == constructorCalls[0]
                    && constructorCalls[0].OpCode == OpCodes.Call && constructorCalls[0].Operand is MethodReference baseConstructor
                    && baseConstructor.DeclaringType.FullName == "System.Object" && baseConstructor.Name == ".ctor"
                    && baseConstructor.Parameters.Count == 0 && baseConstructor.DeclaringType.Scope.Name == "System.Private.CoreLib",
                    "reference target lost its base-only constructor prefix");
        }
        Require(app.MainModule.GetTypes().All(t => t.CustomAttributes.Count == 0), "late copied fixture gained an application attribute root");
        Require(entry.Fields.Single(f => f.Name == "Layout").FieldType.FullName == "Graph." + (layout ? "LateTarget" : "LateValue"), "late copied target lost its metadata layout");
        if (layout)
        {
            Require(!File.Exists(Path.Combine(directory, "link.xml")), "late layout gained preservation roots");
            var arm = entry.Methods.Single(m => m.Name == "Arm");
            var model = entry.Methods.Single(m => m.Name == "Model");
            Require(arm.Parameters.Count == 0 && model.Parameters.Count == 0 && arm.ReturnType.FullName == "System.Void" && model.ReturnType.FullName == "System.Void", "late layout leaked through helper signatures");
            Require(arm.Body.Instructions.Select(i => i.OpCode.Code).SequenceEqual(new[] { Code.Ldc_I4_1, Code.Newarr, Code.Callvirt, Code.Ret })
                && arm.Body.Instructions[1].Operand is TypeReference integer && integer.FullName == "System.Int32"
                && arm.Body.Instructions[2].Operand is MethodReference init && init.DeclaringType.FullName == "System.Array" && init.Name == "Initialize",
                "late layout lost its independent array initializer helper");
            Require(model.Body.Instructions.Select(i => i.OpCode.Code).SequenceEqual(new[] { Code.Ldc_I4_1, Code.Newarr, Code.Pop, Code.Ret })
                && model.Body.Instructions[1].Operand is GenericInstanceType closed && closed.ElementType.FullName == "Graph.LateCell`1"
                && closed.GenericArguments.Count == 1 && closed.GenericArguments[0].FullName == "System.Int32"
                && closed.ElementType.Scope is AssemblyNameReference scope && scope.Name == (mode == "copied-layout-framework" ? "System.ArrayFacade" : "DnZlib"),
                "late layout lost its first concrete copied generic layout");
            var calls = mainCode.Where(i => i.Operand is MethodReference m && m.DeclaringType == entry).ToArray();
            string[] expected = mode == "copied-early-layout" ? new[] { "Model", "Arm" }
                : mode == "copied-layout-unarmed" ? new[] { "Model" }
                : mode == "copied-layout-unmodeled" ? new[] { "Arm" } : new[] { "Arm", "Model" };
            Require(mainCode.Length == 5 + expected.Length && calls.All(i => i.OpCode == OpCodes.Call)
                && calls.Select(i => ((MethodReference)i.Operand).Name).SequenceEqual(expected), "late layout helper order or independence changed");
            var cell = (mode == "copied-layout-framework" ? facade : shim).MainModule.GetType("Graph.LateCell`1");
            Require(cell.IsValueType && cell.GenericParameters.Count == 1 && cell.Fields.Single().FieldType == cell.GenericParameters[0], "late layout lost its generic copied value definition");
            Require(cell.Methods.Count == 2 && cell.Methods.All(m => m.IsConstructor && !m.IsStatic), "late copied cell constructor set changed");
            var ctor = cell.Methods.Single(m => m.Parameters.Count == 0);
            var overload = cell.Methods.Single(m => m.Parameters.Count == 1);
            Require(overload.Parameters[0].ParameterType.FullName == "System.Int32", "late copied constructor overload signature changed");
            CheckLateNamedActivation(ctor, "LateTarget", false);
            CheckLateNamedActivation(overload, "LateCellOverloadTarget", false);
            var overloadTarget = app.MainModule.GetType("Graph.LateCellOverloadTarget");
            Require(overloadTarget.BaseType.FullName == "System.Object"
                && entry.Fields.Single(f => f.Name == "CellOverload").FieldType.FullName == overloadTarget.FullName,
                "late copied overload target lost reference metadata pinning");
            var overloadTargetCode = overloadTarget.Methods.Single().Body.Instructions.ToArray();
            Require(overloadTargetCode.Select(i => i.OpCode.Code).SequenceEqual(new[] { Code.Ldarg_0, Code.Call, Code.Call, Code.Ret })
                && overloadTargetCode[2].Operand is MethodReference overloadDependency
                && overloadDependency.DeclaringType.FullName == "Graph.LateOverloadDependency" && overloadDependency.Name == "Touch"
                && overloadDependency.DeclaringType.Scope.Name == "GraphLib", "late copied overload lost its unique stripped dependency");
            Require(app.MainModule.GetType("Graph.LateTarget").Methods.Count(m => m.IsConstructor) == 1,
                "literal Activator target gained a conservative constructor overload");
            Require(app.MainModule.GetType("Graph.LateTarget").BaseType.FullName == "System.Object", "late layout target must be a reference type");
        }
        else
        {
            Require(appCode.All(i => i.Operand is not MethodReference method || method.DeclaringType.FullName != "System.Array" || method.Name != "Initialize"), "attribute filter bypassed its copied capability");
            var read = entry.Methods.Single(m => m.Name == "Read");
            string owner = data ? "System.Reflection.MemberInfo" : "System.Reflection.Assembly";
            Require(read.Parameters.Count == 1 && read.Parameters[0].ParameterType.FullName == owner, "attribute filter lost its unknown argument owner");
            var code = read.Body.Instructions.ToArray();
            int reads = mode.EndsWith("-noquery", StringComparison.Ordinal) ? 0 : mode == "copied-filter-data-upgrade" ? 2 : 1;
            Require(code.Length == reads * 6 + 1 && code[^1].OpCode == OpCodes.Ret, "attribute filter read count changed");
            for (int i = 0; i < reads; i++)
            {
                var block = code.Skip(i * 6).Take(6).ToArray();
                Require(block.Select(n => n.OpCode.Code).SequenceEqual(new[] { Code.Ldarg_0, Code.Ldtoken, Code.Call, Code.Ldc_I4_0, Code.Callvirt, Code.Pop })
                    && block[1].Operand is TypeReference filter && filter.FullName == "Graph." + (mode.EndsWith("-unmatched", StringComparison.Ordinal) ? "LateOtherAttribute" : "LateSpecialAttribute")
                    && filter.Scope is AssemblyNameReference filterScope && filterScope.Name == (mode == "copied-filter-framework" ? "System.ArrayFacade" : "DnZlib")
                    && block[2].Operand is MethodReference fromHandle && fromHandle.DeclaringType.FullName == "System.Type" && fromHandle.Name == "GetTypeFromHandle"
                    && block[4].Operand is MethodReference query && query.DeclaringType.FullName == owner
                    && query.Name == (data && i == 0 ? "IsDefined" : "GetCustomAttributes")
                    && query.Parameters.Select(p => p.ParameterType.FullName).SequenceEqual(new[] { "System.Type", "System.Boolean" }),
                    "attribute filter changed its unknown owner, exact filter or metadata/construction operation");
            }
            Require(mainCode.Where(i => i.OpCode == OpCodes.Newarr).Single().Operand is TypeReference value && value.FullName == "Graph.LateValue"
                && mainCode.Where(i => i.Operand is MethodReference m && m.DeclaringType == entry).Single().Operand == read,
                "attribute filter lost its modeled value or helper call");
            var attributeModule = (mode == "copied-filter-framework" ? facade : shim).MainModule;
            var attribute = attributeModule.GetType("Graph.LateSpecialAttribute");
            CustomAttribute row;
            if (data)
            {
                var provider = library.MainModule.GetType("Graph.LateUnusedProvider");
                Require(provider is not null && !provider.HasGenericParameters && provider.Methods.Count == 0 && provider.Fields.Count == 0
                    && !app.MainModule.GetTypes().SelectMany(t => t.Fields).Any(f => f.FieldType.FullName == provider.FullName)
                    && !appCode.Any(i => i.Operand is TypeReference type && type.FullName == provider.FullName), "data filter provider gained an independent user reference");
                row = provider!.CustomAttributes.Single();
                Require(row.ConstructorArguments.Count == 1 && row.ConstructorArguments[0].Type.FullName == "System.Type"
                    && row.ConstructorArguments[0].Value is TypeReference identity && identity.FullName == "Graph." + (mode == "copied-filter-data-inactive" ? "LateUnrelatedIdentity" : "LateConditionalFactory")
                    && identity.Scope is AssemblyNameReference identityScope && identityScope.Name == "DnZlib", "data filter lost its isolated conditional identity argument");
                CheckLateInitialize(shim.MainModule.GetType("Graph.LateConditionalFactory").Methods.Single().Body.Instructions.ToArray(), true);
                Require(shim.CustomAttributes.Count == 0 && library.CustomAttributes.Count == 0
                    && library.MainModule.GetTypes().Sum(t => t.CustomAttributes.Count) == 1,
                    "data filter gained another attribute provider root");
                var descriptor = XDocument.Load(Path.Combine(directory, "link.xml"));
                var assembly = descriptor.Root!.Elements().Single();
                var descriptorType = assembly.Elements().Single();
                var descriptorMethod = descriptorType.Elements().Single();
                Require(descriptor.Root.Name == "linker" && !descriptor.Root.HasAttributes
                    && assembly.Name == "assembly" && assembly.Attributes().Count() == 2 && (string?)assembly.Attribute("fullname") == "DnZlib" && (string?)assembly.Attribute("preserve") == "nothing"
                    && descriptorType.Name == "type" && descriptorType.Attributes().Count() == 3 && (string?)descriptorType.Attribute("fullname") == "Graph.LateConditionalFactory"
                    && (string?)descriptorType.Attribute("preserve") == "nothing" && (string?)descriptorType.Attribute("required") == "0"
                    && descriptorMethod.Name == "method" && descriptorMethod.Attributes().Count() == 1 && (string?)descriptorMethod.Attribute("name") == "Initialize" && !descriptorMethod.HasElements,
                    "data filter changed its conditional preservation contract");
                CheckLateNamedActivation(attribute.Methods.Single(m => m.IsConstructor), "LateAttributeCtorTarget", true);
                CheckLateNamedActivation(attribute.Methods.Single(m => m.Name == "set_Arm"), "LateAttributeSetterTarget", false);
            }
            else
            {
                Require(!File.Exists(Path.Combine(directory, "link.xml")), "instance filter gained preservation roots");
                row = attributeModule.Assembly.CustomAttributes.Single();
                Require(library.CustomAttributes.Count == 0 && library.MainModule.GetTypes().All(t => t.CustomAttributes.Count == 0)
                    && attributeModule.GetTypes().All(t => t.CustomAttributes.Count == 0), "instance filter gained another attribute provider");
                Require(row.ConstructorArguments.Count == 0, "instance filter changed its attribute arguments");
                var constructorCode = attribute.Methods.Single(m => m.IsConstructor).Body.Instructions.ToArray();
                Require(constructorCode[0].OpCode == OpCodes.Ldarg_0 && constructorCode[1].OpCode == OpCodes.Call
                    && constructorCode[1].Operand is MethodReference baseConstructor && baseConstructor.DeclaringType.FullName == "System.Attribute"
                    && baseConstructor.Name == ".ctor", "instance attribute lost its base constructor");
                CheckLateInitialize(constructorCode.Skip(2).ToArray(), mode != "copied-filter-setter");
                CheckLateInitialize(attribute.Methods.Single(m => m.Name == "set_Arm").Body.Instructions.ToArray(), true);
            }
            Require(row.AttributeType.FullName == attribute.FullName && row.Fields.Count == 0 && row.Properties.Count == 1
                && row.Properties[0].Name == "Arm" && row.Properties[0].Argument.Value is int arm && arm == 7,
                "attribute filter lost its actual named setter row");
            CheckLateNamedActivation(attribute.Methods.Single(m => m.Name == "get_Arm"), "LateAttributeGetterTarget", false, true);
        }
    }

    private static void CheckLateInitialize(Instruction[] code, bool initializes)
    {
        Require(code.Select(i => i.OpCode.Code).SequenceEqual(initializes
            ? new[] { Code.Ldc_I4_1, Code.Newarr, Code.Callvirt, Code.Ret } : new[] { Code.Ret }), "attribute initializer body changed");
        if (initializes)
            Require(code[1].Operand is TypeReference integer && integer.FullName == "System.Int32"
                && code[2].Operand is MethodReference method && method.DeclaringType.FullName == "System.Array"
                && method.Name == "Initialize" && method.Parameters.Count == 0, "attribute initializer route changed");
    }

    private static void CheckLateNamedActivation(MethodDefinition method, string name, bool baseCall, bool returnsInteger = false)
    {
        var code = method.Body.Instructions.ToArray();
        var expected = new List<Code>();
        if (baseCall) { expected.Add(Code.Ldarg_0); expected.Add(Code.Call); }
        expected.AddRange(new[] { Code.Ldstr, Code.Call, Code.Call, Code.Pop });
        if (returnsInteger) expected.Add(Code.Ldc_I4_7);
        expected.Add(Code.Ret);
        int start = baseCall ? 2 : 0;
        Require(!baseCall || code[1].Operand is MethodReference baseConstructor
            && baseConstructor.DeclaringType.FullName == "System.Attribute" && baseConstructor.Name == ".ctor"
            && baseConstructor.Parameters.Count == 0, "attribute activation lost its base-only prefix");
        Require(code.Select(i => i.OpCode.Code).SequenceEqual(expected) && (string)code[start].Operand == "Graph." + name + ", GraphApp"
            && code[start + 1].Operand is MethodReference lookup && lookup.DeclaringType.FullName == "System.Type" && lookup.Name == "GetType"
            && lookup.Parameters.Select(p => p.ParameterType.FullName).SequenceEqual(new[] { "System.String" })
            && code[start + 2].Operand is MethodReference construct && construct.DeclaringType.FullName == "System.Activator" && construct.Name == "CreateInstance"
            && construct.Parameters.Select(p => p.ParameterType.FullName).SequenceEqual(new[] { "System.Type" }), "copied literal activation changed its indirect target");
    }
}
