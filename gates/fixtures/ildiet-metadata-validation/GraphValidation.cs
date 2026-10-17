using Mono.Cecil;
using Mono.Cecil.Cil;

static class GraphValidation
{
    public static bool Run(string[] args)
    {
        if (GraphArrayValidation.Run(args)) return true;
        if (GraphArrayValidation.RunCopied(args)) return true;
        if (GraphDelegateValidation.Run(args)) return true;
        if (GraphNominalValidation.Run(args)) return true;
        if (GraphUncertaintyValidation.Run(args)) return true;
        if (GraphLookupValidation.Run(args)) return true;
        if (GraphProjectionValidation.Run(args)) return true;
        if (args.Length == 4 && args[0] == "--create-graph-fixtures")
        {
            Create(args[1], args[2], int.Parse(args[3], System.Globalization.CultureInfo.InvariantCulture));
            Console.WriteLine("fixture-created=definition-graph callers=" + args[3]);
            return true;
        }
        if (args.Length >= 2 && args[0] == "--check-token-closure")
        {
            CheckClosure(args.Skip(1).ToArray());
            return true;
        }
        if (args.Length >= 3 && args[0] == "--check-token-closure-external-corelib")
        {
            CheckClosureWithExternalCoreLib(args[1], args.Skip(2).ToArray());
            return true;
        }
        if (args.Length == 2 && args[0] == "--relations")
        {
            using var assembly = AssemblyDefinition.ReadAssembly(args[1]);
            foreach (string line in Relations(assembly).OrderBy(s => s, StringComparer.Ordinal))
                Console.WriteLine(line);
            return true;
        }
        if (args.Length == 3 && (args[0] is "--check-graph-projection" or "--check-graph-array-vector"))
        {
            using var library = AssemblyDefinition.ReadAssembly(Path.Combine(args[1], "GraphLib.dll"));
            using var application = AssemblyDefinition.ReadAssembly(Path.Combine(args[1], "GraphApp.dll"));
            if (args[0] == "--check-graph-projection") GraphProjectionValidation.Check(library.MainModule, args[2], application.MainModule);
            else GraphArrayValidation.Check(library.MainModule, args[2]);
            return true;
        }
        if (args.Length == 4 && args[0] == "--check-graph-contexts")
        {
            Require(args[3] is "original" or "published", "unknown graph context state");
            CheckContexts(args[1], int.Parse(args[2], System.Globalization.CultureInfo.InvariantCulture), args[3] == "published");
            return true;
        }
        if (args.Length == 2 && args[0] == "--check-graph-fixtures")
        {
            using var library = AssemblyDefinition.ReadAssembly(Path.Combine(args[1], "GraphLib.dll"));
            var module = library.MainModule;
            Require(module.GetType("Graph.Late")?.Methods.Single(m => m.Name == "Run").HasBody == true,
                "late metadata-to-body promotion lost its method");
            Require(module.GetType("Graph.DeadBranch")?.Methods.Any(m => m.Name == "Touch") == true,
                "a constant dead branch lost its operand dependency");
            Require(module.GetType("Graph.Growing")?.Methods.Any(m => m.Name == "Loop") == true,
                "a growing generic body was removed");
            Require(module.GetType("Graph.BodyOnlyDependency") is null,
                "a metadata-only method opened its body dependency");
            foreach (string name in new[] { "Late", "SignatureOnly" })
            {
                var method = module.GetType("Graph." + name).Methods.Single(m => m.Name == "Run");
                bool stub = method.HasBody && method.Body.Instructions.Select(i => i.OpCode.Code)
                    .SequenceEqual(new[] { Code.Ldc_I4, Code.Pop, Code.Ldnull, Code.Throw });
                Require(stub == (name == "SignatureOnly"), "wrong retained body state: " + name);
            }
            foreach (var (name, method) in new[] { ("TypePayload", "ObservedTypeMember"), ("MethodPayload", "ObservedMethodMember") })
                Require(module.GetType("Graph." + name)?.Methods.Any(m => m.Name == method) == true,
                    "inherited interface symbol demand lost its bound metadata: " + name);
            var constrained = module.GetType("Graph.Constrained`1")?.GenericParameters.Single();
            Require(constrained is not null && constrained.HasDefaultConstructorConstraint
                && constrained.HasNotNullableValueTypeConstraint, "unmanaged constraint flags changed");
            Require(constrained!.Constraints.Single().ConstraintType is RequiredModifierType unmanagedConstraint
                && unmanagedConstraint.ModifierType.FullName == "System.Runtime.InteropServices.UnmanagedType"
                && unmanagedConstraint.ElementType.FullName == "System.ValueType",
                "unmanaged constraint lost its required modifier");
            Require(constrained.CustomAttributes.Any(a => a.AttributeType.FullName
                == "System.Runtime.CompilerServices.IsUnmanagedAttribute"), "unmanaged parameter attribute was removed");
            GraphArrayValidation.Check(module);
            using var application = AssemblyDefinition.ReadAssembly(Path.Combine(args[1], "GraphApp.dll"));
            GraphProjectionValidation.Check(module, app: application.MainModule);
            GraphReflectionValidation.Check(application.MainModule, module);
            Console.WriteLine("graph-fixtures=late-promotion,dead-branch,growing-generics,signature-only,inherited-interface-demands");
            foreach (string section in GraphLookupValidation.Sections) GraphProjectionValidation.Check(module, section, application.MainModule);
            foreach (string section in GraphDelegateBindingValidation.Sections) GraphProjectionValidation.Check(module, section, application.MainModule);
            foreach (string section in GraphPropertyValidation.Sections) GraphProjectionValidation.Check(module, section, application.MainModule);
            return true;
        }
        return false;
    }

    static void Create(string directory, string corelib, int callers)
    {
        Directory.CreateDirectory(directory);
        using var core = AssemblyDefinition.ReadAssembly(corelib);
        using var library = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition("GraphLib", new Version(1, 0)),
            "GraphLib", ModuleKind.Dll);
        using var app = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition("GraphApp", new Version(1, 0)),
            "GraphApp", ModuleKind.Console);
        var lib = library.MainModule;
        var mod = app.MainModule;
        TypeReference Core(string name) => lib.ImportReference(core.MainModule.GetType("System." + name));
        var objectType = Core("Object");
        var voidType = Core("Void");
        var intType = Core("Int32");
        TypeDefinition Type(string name, TypeAttributes attributes = TypeAttributes.Public | TypeAttributes.Class,
            TypeReference? parent = null)
        {
            var type = new TypeDefinition("Graph", name, attributes, parent ?? objectType);
            lib.Types.Add(type);
            return type;
        }
        MethodDefinition Method(TypeDefinition owner, string name, MethodAttributes attributes = MethodAttributes.Public | MethodAttributes.Static,
            TypeReference? result = null)
        {
            var method = new MethodDefinition(name, attributes, result ?? voidType);
            owner.Methods.Add(method);
            if (!method.IsAbstract) method.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            return method;
        }
        var late = Type("Late");
        var lateRun = Method(late, "Run");
        var genericLate = Type("GenericLate");
        var genericLateRun = Method(genericLate, "Run");
        genericLateRun.GenericParameters.Add(new GenericParameter("TArray", genericLateRun));
        var witnessParameter = new GenericParameter("TWitness", genericLateRun); genericLateRun.GenericParameters.Add(witnessParameter);
        var observedDependency = Type("ObservedBodyDependency"); var observedTouch = Method(observedDependency, "Touch");
        var witnesses = new List<TypeDefinition>();
        for (int i = 1; i <= 40; i++)
        {
            var witness = Type("ClosedContextWitness" + i.ToString(System.Globalization.CultureInfo.InvariantCulture)); witnesses.Add(witness);
            Method(witness, "Observed").Body.Instructions.Insert(0, Instruction.Create(OpCodes.Call, observedTouch));
        }
        var dead = Type("DeadBranch");
        var deadRun = Method(dead, "Touch");
        var bodyOnly = Type("BodyOnlyDependency");
        var bodyOnlyRun = Method(bodyOnly, "Touch");
        var signature = Type("SignatureOnly");
        var signatureRun = Method(signature, "Run");
        signatureRun.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Call, bodyOnlyRun));
        var growing = Type("Growing");
        var loop = Method(growing, "Loop");
        var parameter = new GenericParameter("T", loop);
        loop.GenericParameters.Add(parameter);
        var list = new GenericInstanceType(lib.ImportReference(core.MainModule.GetType("System.Collections.Generic.List`1")));
        list.GenericArguments.Add(parameter);
        var recurse = new GenericInstanceMethod(loop);
        recurse.GenericArguments.Add(list);
        loop.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Call, recurse));

        var arrays = Type("ArrayRelations");
        var vector = new ArrayType(late);
        var rankOne = new ArrayType(late, 1);
        rankOne.Dimensions[0] = new ArrayDimension(1, null);
        var rectangular = new ArrayType(late, 2);
        var bounded = new ArrayType(late, 2);
        bounded.Dimensions[0] = new ArrayDimension(-2, 3);
        bounded.Dimensions[1] = new ArrayDimension(4, 8);
        foreach (var (name, type) in new[] { ("Vector", vector), ("RankOne", rankOne), ("Rectangular", rectangular), ("Bounded", bounded) })
            arrays.Fields.Add(new FieldDefinition(name, FieldAttributes.Public, type));
        arrays.Fields.Add(new FieldDefinition("Required", FieldAttributes.Public,
            new RequiredModifierType(Core("Runtime.CompilerServices.IsVolatile"), intType)));
        arrays.Fields.Add(new FieldDefinition("Optional", FieldAttributes.Public,
            new OptionalModifierType(Core("Runtime.CompilerServices.IsVolatile"), intType)));
        var arrayMethods = Method(arrays, "PseudoMethods");
        arrayMethods.Body.Instructions.Clear();
        var arrayCtor = new MethodReference(".ctor", voidType, rectangular) { HasThis = true };
        arrayCtor.Parameters.Add(new ParameterDefinition(intType));
        arrayCtor.Parameters.Add(new ParameterDefinition(intType));
        var get = new MethodReference("Get", late, rectangular) { HasThis = true };
        get.Parameters.Add(new ParameterDefinition(intType));
        get.Parameters.Add(new ParameterDefinition(intType));
        arrayMethods.Body.Variables.Add(new VariableDefinition(rectangular));
        arrayMethods.Body.InitLocals = true;
        foreach (var instruction in new[] { Instruction.Create(OpCodes.Ldc_I4_1), Instruction.Create(OpCodes.Ldc_I4_1),
            Instruction.Create(OpCodes.Newobj, arrayCtor), Instruction.Create(OpCodes.Stloc_0), Instruction.Create(OpCodes.Ldloc_0),
            Instruction.Create(OpCodes.Ldc_I4_0), Instruction.Create(OpCodes.Ldc_I4_0), Instruction.Create(OpCodes.Call, get),
            Instruction.Create(OpCodes.Pop), Instruction.Create(OpCodes.Ret) }) arrayMethods.Body.Instructions.Add(instruction);

        var marshal = Type("CustomMarshaler");
        arrays.Fields.Add(new FieldDefinition("Marshaled", FieldAttributes.Public, Core("String"))
        {
            MarshalInfo = new CustomMarshalInfo { ManagedType = marshal, Cookie = "graph-cookie" }
        });
        var payload = Type("TypePayload");
        Method(payload, "ObservedTypeMember");
        var methodPayload = Type("MethodPayload");
        Method(methodPayload, "ObservedMethodMember");
        var payloadCtor = Method(methodPayload, ".ctor",
            MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);
        payloadCtor.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Ldarg_0));
        payloadCtor.Body.Instructions.Insert(1, Instruction.Create(OpCodes.Call, lib.ImportReference(
            core.MainModule.GetType("System.Object").Methods.Single(m => m.IsConstructor && !m.IsStatic && m.Parameters.Count == 0))));
        var generic = Type("Constrained`1");
        var constraint = new GenericParameter("T", generic) { Attributes = GenericParameterAttributes.DefaultConstructorConstraint | GenericParameterAttributes.NotNullableValueTypeConstraint };
        constraint.Constraints.Add(new GenericParameterConstraint(new RequiredModifierType(
            Core("Runtime.InteropServices.UnmanagedType"), Core("ValueType"))));
        var unmanaged = core.MainModule.GetType("System.Runtime.CompilerServices.IsUnmanagedAttribute");
        constraint.CustomAttributes.Add(new CustomAttribute(lib.ImportReference(
            unmanaged.Methods.Single(m => m.IsConstructor && !m.IsStatic && m.Parameters.Count == 0))));
        generic.GenericParameters.Add(constraint);
        var iface = Type("IContract`1", TypeAttributes.Public | TypeAttributes.Interface | TypeAttributes.Abstract);
        iface.BaseType = null;
        var symbol = new GenericParameter("T", iface);
        iface.GenericParameters.Add(symbol);
        var slot = Method(iface, "Select", MethodAttributes.Public | MethodAttributes.Abstract | MethodAttributes.Virtual | MethodAttributes.NewSlot, symbol);
        var methodSymbol = new GenericParameter("U", slot);
        slot.GenericParameters.Add(methodSymbol);
        methodSymbol.Attributes = GenericParameterAttributes.DefaultConstructorConstraint;
        var dim = Method(iface, "Default", MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.NewSlot);
        var staticSlot = Method(iface, "Static", MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.Abstract | MethodAttributes.Virtual | MethodAttributes.NewSlot);
        var ancestor = Type("Ancestor`1");
        var ancestorParameter = new GenericParameter("T", ancestor);
        ancestor.GenericParameters.Add(ancestorParameter);
        var inherited = Method(ancestor, "Select", MethodAttributes.Public | MethodAttributes.HideBySig, ancestorParameter);
        var inheritedParameter = new GenericParameter("U", inherited);
        inherited.GenericParameters.Add(inheritedParameter);
        inheritedParameter.Attributes = GenericParameterAttributes.DefaultConstructorConstraint;
        // Both generic owners carry distinct reflection demands through inherited interface implementation.
        var runtimeType = core.MainModule.GetType("System.Type");
        var fromHandle = lib.ImportReference(runtimeType.Methods.Single(m => m.Name == "GetTypeFromHandle"));
        var getMethods = lib.ImportReference(runtimeType.Methods.Single(m => m.Name == "GetMethods" && m.Parameters.Count == 0));
        genericLateRun.Body.Instructions.Clear();
        genericLateRun.Body.Instructions.Add(Instruction.Create(OpCodes.Ldtoken, witnessParameter));
        genericLateRun.Body.Instructions.Add(Instruction.Create(OpCodes.Call, fromHandle));
        genericLateRun.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, getMethods));
        genericLateRun.Body.Instructions.Add(Instruction.Create(OpCodes.Pop)); genericLateRun.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        inherited.Body.Instructions.Clear();
        foreach (var gp in new[] { ancestorParameter, inheritedParameter })
        {
            inherited.Body.Instructions.Add(Instruction.Create(OpCodes.Ldtoken, gp));
            inherited.Body.Instructions.Add(Instruction.Create(OpCodes.Call, fromHandle));
            inherited.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, getMethods));
            inherited.Body.Instructions.Add(Instruction.Create(OpCodes.Pop));
        }
        inherited.Body.Variables.Add(new VariableDefinition(ancestorParameter));
        inherited.Body.InitLocals = true;
        inherited.Body.Instructions.Add(Instruction.Create(OpCodes.Ldloc_0));
        inherited.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        var derived = Type("Derived`1");
        var derivedParameter = new GenericParameter("T", derived);
        derived.GenericParameters.Add(derivedParameter);
        var parent = new GenericInstanceType(ancestor); parent.GenericArguments.Add(derivedParameter); derived.BaseType = parent;
        var closedInterface = new GenericInstanceType(iface); closedInterface.GenericArguments.Add(derivedParameter);
        derived.Interfaces.Add(new InterfaceImplementation(closedInterface));
        var implementation = Method(derived, "Static", MethodAttributes.Public | MethodAttributes.Static);
        implementation.Overrides.Add(new MethodReference(staticSlot.Name, voidType, closedInterface));
        var covariantBase = Type("CovariantBase");
        var baseSlot = Method(covariantBase, "Make", MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.NewSlot, objectType);
        baseSlot.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Ldnull));
        var covariant = Type("Covariant", parent: covariantBase);
        var covariantSlot = Method(covariant, "Make", MethodAttributes.Public | MethodAttributes.Virtual, Core("String"));
        covariantSlot.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Ldnull));
        covariantSlot.Overrides.Add(baseSlot);
        var explicitInterface = Type("IExplicit", TypeAttributes.Public | TypeAttributes.Interface | TypeAttributes.Abstract);
        explicitInterface.BaseType = null;
        var explicitSlot = Method(explicitInterface, "Run", MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Abstract | MethodAttributes.NewSlot);
        var explicitOwner = Type("ExplicitOwner");
        explicitOwner.Interfaces.Add(new InterfaceImplementation(explicitInterface));
        var explicitBody = Method(explicitOwner, "Graph.IExplicit.Run", MethodAttributes.Private | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.NewSlot);
        explicitBody.Overrides.Add(explicitSlot);
        var root = new TypeDefinition("Graph", "Program", TypeAttributes.Public | TypeAttributes.Class, mod.ImportReference(core.MainModule.GetType("System.Object")));
        mod.Types.Add(root);
        var main = new MethodDefinition("Main", MethodAttributes.Public | MethodAttributes.Static, mod.ImportReference(voidType));
        root.Methods.Add(main); app.EntryPoint = main;
        var signatureRoot = new MethodDefinition("Rows", MethodAttributes.Public | MethodAttributes.Static, mod.ImportReference(signature));
        root.Methods.Add(signatureRoot);
        signatureRoot.Parameters.Add(new ParameterDefinition(mod.ImportReference(late)));
        signatureRoot.Body.Instructions.Add(Instruction.Create(OpCodes.Ldnull)); signatureRoot.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        main.Body.Instructions.Add(Instruction.Create(OpCodes.Ldtoken, mod.ImportReference(signature)));
        main.Body.Instructions.Add(Instruction.Create(OpCodes.Call, mod.ImportReference(fromHandle)));
        main.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, mod.ImportReference(getMethods)));
        main.Body.Instructions.Add(Instruction.Create(OpCodes.Pop));
        var end = Instruction.Create(OpCodes.Ret);
        main.Body.Instructions.Add(Instruction.Create(OpCodes.Call, mod.ImportReference(lateRun)));
        TypeReference contextArray = mod.ImportReference(intType);
        for (int i = 0; i < callers; i++)
        {
            contextArray = new ArrayType(contextArray);
            var context = new GenericInstanceMethod(mod.ImportReference(genericLateRun));
            context.GenericArguments.Add(contextArray); context.GenericArguments.Add(mod.ImportReference(witnesses[i]));
            main.Body.Instructions.Add(Instruction.Create(OpCodes.Call, context));
        }
        main.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_0));
        main.Body.Instructions.Add(Instruction.Create(OpCodes.Brfalse, end));
        main.Body.Instructions.Add(Instruction.Create(OpCodes.Call, mod.ImportReference(deadRun)));
        var closedLoop = new GenericInstanceMethod(mod.ImportReference(loop)); closedLoop.GenericArguments.Add(mod.ImportReference(intType));
        main.Body.Instructions.Add(Instruction.Create(OpCodes.Call, closedLoop));
        main.Body.Instructions.Add(Instruction.Create(OpCodes.Call, mod.ImportReference(arrayMethods)));
        var calledInterface = new GenericInstanceType(mod.ImportReference(iface));
        calledInterface.GenericArguments.Add(mod.ImportReference(payload));
        var calledSlot = mod.ImportReference(slot);
        calledSlot.DeclaringType = calledInterface;
        var calledGeneric = new GenericInstanceMethod(calledSlot);
        calledGeneric.GenericArguments.Add(mod.ImportReference(methodPayload));
        main.Body.Instructions.Add(Instruction.Create(OpCodes.Ldnull));
        main.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, calledGeneric));
        main.Body.Instructions.Add(Instruction.Create(OpCodes.Pop));
        var closedDerived = new GenericInstanceType(mod.ImportReference(derived));
        closedDerived.GenericArguments.Add(mod.ImportReference(payload));
        main.Body.Instructions.Add(Instruction.Create(OpCodes.Ldtoken, closedDerived));
        main.Body.Instructions.Add(Instruction.Create(OpCodes.Pop));
        foreach (var type in new[] { covariant, explicitOwner })
        {
            main.Body.Instructions.Add(Instruction.Create(OpCodes.Ldtoken, mod.ImportReference(type)));
            main.Body.Instructions.Add(Instruction.Create(OpCodes.Pop));
        }
        foreach (var type in new[] { generic, derived })
        {
            var instance = new GenericInstanceType(mod.ImportReference(type)); instance.GenericArguments.Add(mod.ImportReference(intType));
            main.Body.Instructions.Add(Instruction.Create(OpCodes.Ldtoken, instance)); main.Body.Instructions.Add(Instruction.Create(OpCodes.Pop));
        }
        foreach (var method in GraphArrayValidation.Create(library, core).Cast<MethodReference>()
            .Concat(GraphReflectionValidation.Create(app, library, core))
            .Concat(GraphProjectionValidation.Create(app, library, core)))
        {
            if (method.HasThis || method.ReturnType.FullName != "System.Void"
                || method.Parameters.Any(p => p.ParameterType.FullName != "System.Int32"))
                throw new InvalidOperationException("graph driver requires static void helpers with only Int32 parameters");
            foreach (var _ in method.Parameters)
                main.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_0));
            main.Body.Instructions.Add(Instruction.Create(OpCodes.Call, mod.ImportReference(method)));
        }
        main.Body.Instructions.Add(end);
        library.Write(Path.Combine(directory, "GraphLib.dll"), new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        app.Write(Path.Combine(directory, "GraphApp.dll"), new WriterParameters { Timestamp = 0, DeterministicMvid = true });
    }

    static void CheckContexts(string directory, int callers, bool published)
    {
        using var app = AssemblyDefinition.ReadAssembly(Path.Combine(directory, "GraphApp.dll"));
        var contexts = app.EntryPoint.Body.Instructions.Select(i => i.Operand).OfType<GenericInstanceMethod>()
            .Where(m => m.Name == "Run" && m.DeclaringType.FullName == "Graph.GenericLate").ToArray();
        Require(contexts.Length == callers && contexts.Select(m => m.FullName).Distinct(StringComparer.Ordinal).Count() == callers,
            "closed generic caller contexts are not distinct");
        for (int i = 0; i < contexts.Length; i++)
        {
            Require(contexts[i].GenericArguments.Count == 2, "closed caller lost a type argument");
            TypeReference argument = contexts[i].GenericArguments[0]; int depth = 0;
            while (argument is ArrayType array && array.IsVector) { depth++; argument = array.ElementType; }
            Require(depth == i + 1 && argument.FullName == "System.Int32"
                && contexts[i].GenericArguments[1].FullName == "Graph.ClosedContextWitness" + (i + 1).ToString(System.Globalization.CultureInfo.InvariantCulture),
                "closed caller argument shape changed");
        }
        if (published)
        {
            using var library = AssemblyDefinition.ReadAssembly(Path.Combine(directory, "GraphLib.dll"));
            for (int i = 1; i <= 40; i++)
            {
                var observed = library.MainModule.GetType("Graph.ClosedContextWitness" + i.ToString(System.Globalization.CultureInfo.InvariantCulture))?.Methods.SingleOrDefault(m => m.Name == "Observed");
                if (i > callers) Require(observed is null, "an unbound closed caller retained member metadata");
                else Require(observed is { HasBody: true } && observed.Body.Instructions.Select(insn => insn.OpCode.Code)
                    .SequenceEqual(new[] { Code.Ldc_I4, Code.Pop, Code.Ldnull, Code.Throw }),
                    "a closed caller failed to apply metadata-only member demand: " + i.ToString(System.Globalization.CultureInfo.InvariantCulture));
            }
            Require(library.MainModule.GetType("Graph.ObservedBodyDependency") is null, "closed caller member demand opened observed method bodies");
        }
        Console.WriteLine("graph-contexts=" + (published ? "published" : "original") + ":closed-callers="
            + callers.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    static IEnumerable<string> Relations(AssemblyDefinition assembly)
    {
        foreach (var exported in assembly.MainModule.ExportedTypes)
            yield return "exported " + exported.FullName + " scope=" + exported.Scope.Name + " forwarder=" + exported.IsForwarder;
        foreach (var type in assembly.MainModule.GetTypes())
        {
            string owner = "type " + type.FullName;
            yield return owner + " base=" + type.BaseType?.FullName;
            foreach (var item in type.Interfaces) yield return owner + " interface=" + item.InterfaceType.FullName;
            foreach (var parameter in type.GenericParameters)
                foreach (string relation in ParameterRelations(owner, parameter)) yield return relation;
            foreach (var field in type.Fields)
            {
                yield return "field " + field.FullName;
                if (field.HasMarshalInfo) yield return "marshal " + field.FullName + " " + MarshalRelation(field.MarshalInfo);
            }
            foreach (var method in type.Methods)
            {
                string row = "method " + method.FullName;
                yield return row;
                foreach (var item in method.Overrides) yield return row + " override=" + item.FullName;
                foreach (var parameter in method.GenericParameters)
                    foreach (string relation in ParameterRelations(row, parameter)) yield return relation;
                foreach (var parameter in method.Parameters)
                    if (parameter.HasMarshalInfo) yield return row + " parameter=" + parameter.Index + " marshal=" + MarshalRelation(parameter.MarshalInfo);
                if (method.MethodReturnType.HasMarshalInfo) yield return row + " return-marshal=" + MarshalRelation(method.MethodReturnType.MarshalInfo);
            }
            foreach (var property in type.Properties) yield return "property " + property.FullName;
            foreach (var item in type.Events) yield return "event " + item.FullName;
        }
    }

    static IEnumerable<string> ParameterRelations(string owner, GenericParameter parameter)
    {
        yield return owner + " generic=" + parameter.Position + " flags=" + (int)parameter.Attributes;
        foreach (var item in parameter.Constraints) yield return owner + " generic=" + parameter.Position + " constraint=" + item.ConstraintType.FullName;
        foreach (var attribute in parameter.CustomAttributes)
            yield return owner + " generic=" + parameter.Position + " attribute=" + attribute.AttributeType.FullName;
    }

    static string MarshalRelation(MarshalInfo info) => info is CustomMarshalInfo custom
        ? "custom:" + custom.ManagedType?.FullName + ":" + custom.Cookie : info.NativeType.ToString();

    static void CheckClosure(string[] paths)
    {
        using var resolver = new DefaultAssemblyResolver();
        foreach (string directory in paths.Select(p => Path.GetDirectoryName(Path.GetFullPath(p))!).Distinct()) resolver.AddSearchDirectory(directory);
        var assemblies = paths.Select(p => AssemblyDefinition.ReadAssembly(p, new ReaderParameters { AssemblyResolver = resolver })).ToArray();
        var loaded = assemblies.Select(a => a.Name.FullName).ToHashSet(StringComparer.Ordinal);
        int operands = 0;
        try
        {
            foreach (var assembly in assemblies)
                foreach (var type in assembly.MainModule.GetTypes())
                    foreach (var method in type.Methods.Where(m => m.HasBody))
                        foreach (var instruction in method.Body.Instructions)
                        {
                            if (instruction.Operand is TypeReference tr) CheckType(tr);
                            else if (instruction.Operand is MethodReference mr)
                            {
                                if (mr.DeclaringType is ArrayType) { CheckType(mr.DeclaringType); CheckType(mr.ReturnType); foreach (var p in mr.Parameters) CheckType(p.ParameterType); }
                                else Check(mr.Resolve(), mr.FullName);
                                if (mr is GenericInstanceMethod generic) foreach (var arg in generic.GenericArguments) CheckType(arg);
                            }
                            else if (instruction.Operand is FieldReference fr) Check(fr.Resolve(), fr.FullName);
                            else if (instruction.Operand is CallSite site) { CheckType(site.ReturnType); foreach (var p in site.Parameters) CheckType(p.ParameterType); }
                            else continue;
                            operands++;
                        }
            Require(operands > 0, "token closure checked no operands");
            Console.WriteLine("token-closure=valid operands=" + operands);
        }
        finally { foreach (var assembly in assemblies) assembly.Dispose(); }
        void Check(IMemberDefinition? target, string name)
        {
            Require(target is not null, "unresolved retained body operand: " + name);
            var module = target is TypeDefinition td ? td.Module : target!.DeclaringType.Module;
            Require(loaded.Contains(module.Assembly.Name.FullName), "operand resolved outside output load set: " + name);
        }
        void CheckType(TypeReference type)
        {
            if (type is GenericParameter) return;
            if (type is FunctionPointerType function)
            {
                CheckType(function.ReturnType);
                foreach (var parameter in function.Parameters) CheckType(parameter.ParameterType);
                return;
            }
            if (type is GenericInstanceType generic) foreach (var argument in generic.GenericArguments) CheckType(argument);
            if (type is IModifierType modifier) CheckType(modifier.ModifierType);
            if (type is TypeSpecification specification) { CheckType(specification.ElementType); return; }
            Check(type.Resolve(), type.FullName);
        }
    }

    sealed class ClosureResolver : IAssemblyResolver
    {
        readonly Dictionary<string, AssemblyDefinition> _assemblies = new(StringComparer.Ordinal);
        internal void Add(AssemblyDefinition assembly) => _assemblies.Add(assembly.Name.FullName, assembly);
        public AssemblyDefinition Resolve(AssemblyNameReference name) => _assemblies.TryGetValue(name.FullName, out var assembly)
            ? assembly : throw new InvalidOperationException("operand requested an unprovided assembly: " + name.FullName);
        public AssemblyDefinition Resolve(AssemblyNameReference name, ReaderParameters parameters) => Resolve(name);
        public void Dispose() { }
    }

    static void CheckClosureWithExternalCoreLib(string corelib, string[] paths)
    {
        using var resolver = new ClosureResolver();
        using var external = AssemblyDefinition.ReadAssembly(corelib, new ReaderParameters { AssemblyResolver = resolver });
        Require(external.Name.Name == "System.Private.CoreLib", "external closure inspection accepts only the provided original CoreLib");
        resolver.Add(external);
        var assemblies = paths.Select(p => AssemblyDefinition.ReadAssembly(p, new ReaderParameters { AssemblyResolver = resolver })).ToArray();
        int operands = 0; int externalTargets = 0;
        try
        {
            Require(paths.Select(p => Path.GetDirectoryName(Path.GetFullPath(p))).Distinct(StringComparer.Ordinal).Count() == 1,
                "user closure modules must belong to one published generation");
            foreach (var assembly in assemblies) resolver.Add(assembly);
            var modules = assemblies.Select(a => a.MainModule).ToHashSet();
            Require(!modules.Any(m => m.Assembly.Name.Name == external.Name.Name), "external CoreLib must be absent from the published load set");
            foreach (var assembly in assemblies)
                foreach (var type in assembly.MainModule.GetTypes())
                    foreach (var method in type.Methods.Where(m => m.HasBody))
                        foreach (var instruction in method.Body.Instructions)
                        {
                            if (instruction.Operand is TypeReference tr) CheckType(tr);
                            else if (instruction.Operand is MethodReference mr)
                            {
                                CheckType(mr.DeclaringType); CheckType(mr.ReturnType);
                                foreach (var parameter in mr.Parameters) CheckType(parameter.ParameterType);
                                if (mr.DeclaringType is not ArrayType) Check(mr.Resolve(), mr.FullName);
                                if (mr is GenericInstanceMethod generic) foreach (var argument in generic.GenericArguments) CheckType(argument);
                            }
                            else if (instruction.Operand is FieldReference fr) { CheckType(fr.DeclaringType); CheckType(fr.FieldType); Check(fr.Resolve(), fr.FullName); }
                            else if (instruction.Operand is CallSite site) { CheckType(site.ReturnType); foreach (var parameter in site.Parameters) CheckType(parameter.ParameterType); }
                            else continue;
                            operands++;
                        }
            Require(operands > 0 && externalTargets > 0, "external closure inspection checked no CoreLib targets");
            Console.WriteLine("token-closure=valid-external-corelib operands=" + operands + " corelib-targets=" + externalTargets);

            void Check(IMemberDefinition? target, string name)
            {
                Require(target is not null, "unresolved retained body operand: " + name);
                var module = target is TypeDefinition definition ? definition.Module : target!.DeclaringType.Module;
                if (ReferenceEquals(module, external.MainModule)) externalTargets++;
                else Require(modules.Contains(module), "user operand resolved outside the published generation: " + name);
            }
            void CheckType(TypeReference type)
            {
                if (type is GenericParameter) return;
                if (type is FunctionPointerType function)
                {
                    CheckType(function.ReturnType); foreach (var parameter in function.Parameters) CheckType(parameter.ParameterType); return;
                }
                if (type is GenericInstanceType generic) foreach (var argument in generic.GenericArguments) CheckType(argument);
                if (type is IModifierType modifier) CheckType(modifier.ModifierType);
                if (type is TypeSpecification specification) { CheckType(specification.ElementType); return; }
                Check(type.Resolve(), type.FullName);
            }
        }
        finally { foreach (var assembly in assemblies) assembly.Dispose(); }
    }

    static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
