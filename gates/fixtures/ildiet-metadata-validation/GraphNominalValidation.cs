using Mono.Cecil;
using Mono.Cecil.Cil;

static class GraphNominalValidation
{
    internal static bool Run(string[] args)
    {
        if (args.Length == 3 && args[0] == "--create-scope-method-fixtures")
        {
            CreateMethods(args[1], args[2]);
            Console.WriteLine("fixture-created=scope-method-overloads");
            return true;
        }
        if (args.Length == 3 && args[0] == "--check-scope-method-fixtures")
        {
            Require(args[2] is "original" or "published", "unknown scope method fixture state");
            CheckMethods(args[1], args[2]);
            return true;
        }
        if (args.Length == 3 && args[0] == "--create-nominal-fixtures")
        {
            Create(args[1], args[2]);
            Console.WriteLine("fixture-created=nominal-module-identity");
            return true;
        }
        if ((args.Length is 3 or 4) && args[0] == "--check-nominal-fixtures")
        {
            Require(args[2] is "original" or "published", "unknown nominal fixture state");
            string family = args.Length == 4 ? args[3] : "all";
            Require(family is "all" or "top" or "nested", "unknown nominal fixture family");
            Check(args[1], args[2], family);
            return true;
        }
        return false;
    }

    static void CreateMethods(string directory, string corelib)
    {
        Directory.CreateDirectory(directory);
        using var core = AssemblyDefinition.ReadAssembly(corelib);
        using var app = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition("GraphApp", new Version(1, 0)), "shared.dll", ModuleKind.Console);
        using var library = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition("GraphLib", new Version(1, 0)), "shared.dll", ModuleKind.Dll);
        TypeReference Core(ModuleDefinition module, string name) => module.ImportReference(core.MainModule.GetType("System." + name));
        TypeDefinition Type(ModuleDefinition module, string name)
        {
            var type = new TypeDefinition("Graph", name, TypeAttributes.Public | TypeAttributes.Class, Core(module, "Object"));
            module.Types.Add(type);
            return type;
        }
        MethodDefinition Method(TypeDefinition owner, string name)
        {
            var method = new MethodDefinition(name, MethodAttributes.Public | MethodAttributes.Static, Core(owner.Module, "Int32"));
            owner.Methods.Add(method);
            return method;
        }
        var appPayload = Type(app.MainModule, "ScopePayload");
        var libPayload = Type(library.MainModule, "ScopePayload");
        var unusedPayload = Type(app.MainModule, "ScopeUnusedPayload");
        var owner = Type(app.MainModule, "ScopeMethods`1");
        owner.GenericParameters.Add(new GenericParameter("TOwner", owner));
        var closedOwner = new GenericInstanceType(owner);
        closedOwner.GenericArguments.Add(Core(app.MainModule, "Int32"));
        var main = Method(Type(app.MainModule, "Entry"), "Main");
        main.ReturnType = Core(app.MainModule, "Void");
        app.EntryPoint = main;
        foreach (string name in new[] { "Read", "Generic" })
            foreach (var payload in new[] { appPayload, libPayload, unusedPayload })
            {
                string origin = payload == appPayload ? "Application" : payload == libPayload ? "Library" : "Unused";
                // Each selected overload opens a distinct dependency in the other assembly.
                var dependencyModule = payload == appPayload ? library.MainModule : app.MainModule;
                var dependency = Method(Type(dependencyModule, "Scope" + name + origin + "Dependency"), "Touch");
                dependency.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4, origin == "Application" ? 11 : 22));
                dependency.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
                var method = Method(owner, name);
                method.Parameters.Add(new ParameterDefinition(app.MainModule.ImportReference(payload)));
                if (name == "Generic") method.GenericParameters.Add(new GenericParameter("TMethod", method));
                method.Body.Instructions.Add(Instruction.Create(OpCodes.Call, app.MainModule.ImportReference(dependency)));
                method.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
                if (payload == unusedPayload) continue;
                var reference = new MethodReference(name, method.ReturnType, closedOwner);
                reference.Parameters.Add(new ParameterDefinition(method.Parameters[0].ParameterType));
                if (name == "Generic") reference.GenericParameters.Add(new GenericParameter("TMethod", reference));
                MethodReference target = reference;
                if (name == "Generic")
                {
                    var instance = new GenericInstanceMethod(reference);
                    instance.GenericArguments.Add(Core(app.MainModule, "String"));
                    target = instance;
                }
                main.Body.Instructions.Add(Instruction.Create(OpCodes.Ldnull));
                main.Body.Instructions.Add(Instruction.Create(OpCodes.Call, target));
                main.Body.Instructions.Add(Instruction.Create(OpCodes.Pop));
            }
        var unusedProvider = Method(Type(library.MainModule, "ScopeUnusedProvider"), "Read");
        unusedProvider.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_0));
        unusedProvider.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        main.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        app.Write(Path.Combine(directory, "GraphApp.dll"), new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        library.Write(Path.Combine(directory, "GraphLib.dll"), new WriterParameters { Timestamp = 0, DeterministicMvid = true });
    }

    static void CheckMethods(string directory, string state)
    {
        using var app = AssemblyDefinition.ReadAssembly(Path.Combine(directory, "GraphApp.dll"));
        using var library = AssemblyDefinition.ReadAssembly(Path.Combine(directory, "GraphLib.dll"));
        var modules = new[] { app.MainModule, library.MainModule };
        Require(modules.All(m => m.Name == "shared.dll") && modules[0].Mvid != modules[1].Mvid,
            "scope method fixture lost equal module names or distinct module identities");
        var calls = app.EntryPoint.Body.Instructions.Where(i => i.OpCode == OpCodes.Call).Select(i => (MethodReference)i.Operand).ToArray();
        Require(calls.Length == 4 && calls.Count(m => m is GenericInstanceMethod) == 2
            && calls.All(m => m.DeclaringType is GenericInstanceType owner && owner.GenericArguments.Single().FullName == "System.Int32"),
            "scope method fixture lost its original MemberRef/MethodSpec closed owner contexts");
        var owner = app.MainModule.GetType("Graph.ScopeMethods`1");
        foreach (string name in new[] { "Read", "Generic" })
            foreach (bool application in new[] { true, false })
            {
                string assembly = application ? "GraphApp" : "GraphLib";
                bool Scope(TypeReference type) => type.Scope is ModuleDefinition module ? module.Assembly.Name.Name == assembly
                    : type.Scope is AssemblyNameReference reference && reference.Name == assembly;
                var method = owner.Methods.SingleOrDefault(m => m.Name == name && m.Parameters.Count == 1
                    && m.Parameters[0].ParameterType.FullName == "Graph.ScopePayload" && Scope(m.Parameters[0].ParameterType));
                Require(method is { HasBody: true } && !Stub(method), "scope overload body was lost: " + name + ":" + assembly);
                Require(calls.Count(m => m.Name == name && Scope(m.Parameters[0].ParameterType)) == 1,
                    "scope overload call lost its parameter assembly: " + name + ":" + assembly);
                string dependency = "Graph.Scope" + name + (application ? "Application" : "Library") + "Dependency";
                var dependencyModule = application ? library.MainModule : app.MainModule;
                Body(dependencyModule, dependency, "Touch");
                Require(method!.Body.Instructions.Any(i => i.OpCode == OpCodes.Call && i.Operand is MethodReference target
                    && target.DeclaringType.FullName == dependency), "scope overload lost its unique dependency operand");
            }
        if (state == "published")
        {
            var unselected = owner.Methods.Where(m => m.Parameters.Any(p => p.ParameterType.FullName == "Graph.ScopeUnusedPayload")).ToArray();
            Require(unselected.Select(m => m.Name).OrderBy(n => n, StringComparer.Ordinal).SequenceEqual(new[] { "Generic", "Read" })
                && unselected.All(m => m.HasBody && Stub(m)), "scope method resolution executed an unselected overload");
            Require(app.MainModule.GetType("Graph.ScopeUnusedPayload") is { } unusedPayload && unusedPayload.Methods.Count == 0,
                "scope method resolution changed the application signature-only payload");
            foreach (string name in new[] { "ScopeUnusedProvider", "ScopeReadUnusedDependency", "ScopeGenericUnusedDependency" })
                Require(modules.All(m => m.GetType("Graph." + name) is null), "scope method resolution retained an unused provider: " + name);
        }
        Console.WriteLine("graph-scope-methods=" + state + ":memberref,methodspec,closed-owner,both-assemblies,unique-external-dependencies,unselected-controls");
    }

    static void Create(string directory, string corelib)
    {
        Directory.CreateDirectory(directory);
        using var core = AssemblyDefinition.ReadAssembly(corelib);
        using var app = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition("GraphApp", new Version(1, 0)), "shared.dll", ModuleKind.Console);
        using var library = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition("GraphLib", new Version(1, 0)), "shared.dll", ModuleKind.Dll);
        var module = library.MainModule;
        TypeReference Core(ModuleDefinition target, string name) => target.ImportReference(core.MainModule.GetType("System." + name));
        MethodReference Api(string owner, string name, params string[] parameters) => module.ImportReference(core.MainModule.GetType("System." + owner).Methods.Single(m => m.IsPublic && m.Name == name
            && m.Parameters.Select(p => p.ParameterType.FullName).SequenceEqual(parameters)));
        TypeDefinition Type(ModuleDefinition target, string name, TypeAttributes flags = TypeAttributes.Public | TypeAttributes.Class)
        {
            var type = new TypeDefinition("Graph", name, flags, Core(target, "Object")); target.Types.Add(type); return type;
        }
        MethodDefinition Method(TypeDefinition owner, string name, TypeReference? result = null, MethodAttributes flags = MethodAttributes.Public | MethodAttributes.Static)
        {
            var method = new MethodDefinition(name, flags, result ?? Core(owner.Module, "Void")); owner.Methods.Add(method); method.Body.Instructions.Add(Instruction.Create(OpCodes.Ret)); return method;
        }
        foreach (var target in new[] { app.MainModule, library.MainModule })
            foreach (bool nested in new[] { false, true })
            {
                string family = nested ? "Nested" : "Top";
                string origin = target == app.MainModule ? "Application" : "Library";
                TypeDefinition payload;
                if (nested)
                {
                    var container = Type(target, "NominalContainer");
                    payload = new TypeDefinition("", "Payload", TypeAttributes.NestedPublic | TypeAttributes.Class, Core(target, "Object")); container.NestedTypes.Add(payload);
                }
                else payload = Type(target, "NominalTopPayload");
                var dependency = Method(Type(target, "Nominal" + family + origin + "CtorDependency"), "Touch");
                var leaf = Method(payload, "ConstructorLeaf", flags: MethodAttributes.Private | MethodAttributes.Static);
                var ctor = Method(payload, ".ctor", flags: MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);
                ctor.Body.Instructions.Clear(); ctor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
                ctor.Body.Instructions.Add(Instruction.Create(OpCodes.Call, target.ImportReference(core.MainModule.GetType("System.Object").Methods.Single(m => m.IsConstructor && !m.IsStatic && m.Parameters.Count == 0))));
                ctor.Body.Instructions.Add(Instruction.Create(OpCodes.Call, dependency)); ctor.Body.Instructions.Add(Instruction.Create(OpCodes.Call, leaf)); ctor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
                Method(payload, "Unselected").Body.Instructions.Insert(0, Instruction.Create(OpCodes.Call, Method(Type(target, "Nominal" + family + origin + "UnselectedDependency"), "Touch")));
            }
        var driver = Type(module, "NominalDriver");
        var genericActivator = module.ImportReference(core.MainModule.GetType("System.Activator").Methods.Single(m => m.IsPublic && m.Name == "CreateInstance" && m.GenericParameters.Count == 1 && m.Parameters.Count == 0));
        var firstBindings = new List<MethodDefinition>(); var lateBindings = new List<MethodDefinition>();
        foreach (bool nested in new[] { false, true })
        {
            string family = nested ? "Nested" : "Top";
            var factory = Type(module, "Nominal" + family + "Factory`1");
            var parameter = new GenericParameter("T", factory) { Attributes = GenericParameterAttributes.DefaultConstructorConstraint }; factory.GenericParameters.Add(parameter);
            var create = Method(factory, "Create", Core(module, "Object")); create.Body.Instructions.Clear();
            var activate = new GenericInstanceMethod(genericActivator); activate.GenericArguments.Add(parameter);
            create.Body.Instructions.Add(Instruction.Create(OpCodes.Call, activate)); create.Body.Instructions.Add(Instruction.Create(OpCodes.Box, parameter)); create.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            foreach (bool first in new[] { true, false })
            {
                string assembly = nested == first ? "GraphLib" : "GraphApp";
                var bind = Method(driver, (first ? "First" : "Late") + family); bind.Body.Instructions.Clear();
                bind.Body.Instructions.Add(Instruction.Create(OpCodes.Ldtoken, factory)); bind.Body.Instructions.Add(Instruction.Create(OpCodes.Call, Api("Type", "GetTypeFromHandle", "System.RuntimeTypeHandle")));
                bind.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_1)); bind.Body.Instructions.Add(Instruction.Create(OpCodes.Newarr, Core(module, "Type")));
                bind.Body.Instructions.Add(Instruction.Create(OpCodes.Dup)); bind.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_0));
                bind.Body.Instructions.Add(Instruction.Create(OpCodes.Ldstr, (nested ? "Graph.NominalContainer+Payload" : "Graph.NominalTopPayload") + ", " + assembly));
                bind.Body.Instructions.Add(Instruction.Create(OpCodes.Call, Api("Type", "GetType", "System.String"))); bind.Body.Instructions.Add(Instruction.Create(OpCodes.Stelem_Ref));
                bind.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, Api("Type", "MakeGenericType", "System.Type[]")));
                bind.Body.Instructions.Add(Instruction.Create(OpCodes.Ldstr, "Create")); bind.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, Api("Type", "GetMethod", "System.String")));
                bind.Body.Instructions.Add(Instruction.Create(OpCodes.Ldnull)); bind.Body.Instructions.Add(Instruction.Create(OpCodes.Ldnull));
                bind.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, Api("Reflection.MethodBase", "Invoke", "System.Object", "System.Object[]")));
                bind.Body.Instructions.Add(Instruction.Create(OpCodes.Pop)); bind.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
                (first ? firstBindings : lateBindings).Add(bind);
            }
        }
        void Calls(MethodDefinition method, params MethodDefinition[] targets)
        {
            method.Body.Instructions.Clear();
            foreach (var target in targets) method.Body.Instructions.Add(Instruction.Create(OpCodes.Call, target));
            method.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        }
        var start = Method(driver, "Start"); Calls(start, firstBindings.ToArray());
        var lateBinding = Method(driver, "LateBinding"); Calls(lateBinding, lateBindings.ToArray());
        // Delay the second bindings until the first factory bodies enqueue demands.
        var queueBinding = Method(driver, "QueueBinding"); Calls(queueBinding, lateBinding);
        var delayBinding = Method(driver, "DelayBinding"); Calls(delayBinding, queueBinding);
        var schedule = Method(driver, "Schedule"); Calls(schedule, delayBinding);
        var entry = Type(app.MainModule, "Entry"); var main = Method(entry, "Main"); app.EntryPoint = main; main.Body.Instructions.Clear();
        main.Body.Instructions.Add(Instruction.Create(OpCodes.Call, app.MainModule.ImportReference(start))); main.Body.Instructions.Add(Instruction.Create(OpCodes.Call, app.MainModule.ImportReference(schedule))); main.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        foreach (var target in new[] { app.MainModule, module })
            foreach (var method in target.GetTypes().SelectMany(t => t.Methods).Where(m => m.HasBody))
            {
                bool Payload(TypeReference type) => type.GetElementType().FullName is "Graph.NominalTopPayload" or "Graph.NominalContainer/Payload";
                Require(!method.Body.Variables.Any(v => Payload(v.VariableType)), "nominal fixture contains a payload local");
                foreach (var instruction in method.Body.Instructions)
                    Require(!(instruction.Operand is TypeReference type && Payload(type))
                        && !(instruction.OpCode == OpCodes.Newobj && instruction.Operand is MethodReference ctor && Payload(ctor.DeclaringType)), "nominal fixture contains an executable payload root");
            }
        app.Write(Path.Combine(directory, "GraphApp.dll"), new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        library.Write(Path.Combine(directory, "GraphLib.dll"), new WriterParameters { Timestamp = 0, DeterministicMvid = true });
    }

    static void Check(string directory, string state, string section)
    {
        using var app = AssemblyDefinition.ReadAssembly(Path.Combine(directory, "GraphApp.dll"));
        using var library = AssemblyDefinition.ReadAssembly(Path.Combine(directory, "GraphLib.dll"));
        Require(app.Name.Name == "GraphApp" && library.Name.Name == "GraphLib" && app.Name.FullName != library.Name.FullName, "nominal fixture lost its distinct assembly identities");
        Require(app.MainModule.Name == "shared.dll" && library.MainModule.Name == "shared.dll" && app.MainModule.Mvid != library.MainModule.Mvid, "nominal fixture lost its equal module names or distinct MVIDs");
        MethodDefinition Driver(string name) => library.MainModule.GetType("Graph.NominalDriver").Methods.Single(m => m.Name == name);
        void Calls(MethodDefinition method, params string[] names) => Require(method.Body.Instructions.Where(i => i.OpCode == OpCodes.Call && i.Operand is MethodReference).Select(i => ((MethodReference)i.Operand).Name).SequenceEqual(names), "nominal fixture changed its first/late call structure: " + method.Name);
        Calls(app.EntryPoint, "Start", "Schedule"); Calls(Driver("Start"), "FirstTop", "FirstNested"); Calls(Driver("Schedule"), "DelayBinding");
        Calls(Driver("DelayBinding"), "QueueBinding"); Calls(Driver("QueueBinding"), "LateBinding"); Calls(Driver("LateBinding"), "LateTop", "LateNested");
        var topFactory = library.MainModule.GetType("Graph.NominalTopFactory`1"); var nestedFactory = library.MainModule.GetType("Graph.NominalNestedFactory`1");
        Require(topFactory.GenericParameters.Count == 1 && nestedFactory.GenericParameters.Count == 1
            && !ReferenceEquals(topFactory.GenericParameters[0], nestedFactory.GenericParameters[0]), "nominal fixture merged its distinct factory formals");
        foreach (var factory in new[] { topFactory, nestedFactory })
        {
            var parameter = factory.GenericParameters[0]; var create = factory.Methods.Single(m => m.Name == "Create");
            Require(parameter.HasDefaultConstructorConstraint && create.Body.Instructions.Any(i => i.OpCode == OpCodes.Call
                && i.Operand is GenericInstanceMethod target && target.DeclaringType.FullName == "System.Activator" && target.Name == "CreateInstance"
                && target.GenericArguments.Count == 1 && target.GenericArguments[0] is GenericParameter argument && ReferenceEquals(argument.Owner, factory)), "nominal factory lost its original formal constructor demand");
        }
        foreach (bool nested in new[] { false, true })
        {
            string family = nested ? "Nested" : "Top";
            string name = nested ? "Graph.NominalContainer/Payload" : "Graph.NominalTopPayload";
            Require(app.MainModule.GetType(name) is { } appPayload && library.MainModule.GetType(name) is { } libPayload && appPayload.FullName == libPayload.FullName, "nominal fixture lost its colliding type names");
            foreach (bool first in new[] { true, false })
            {
                string assembly = nested == first ? "GraphLib" : "GraphApp";
                var bind = Driver((first ? "First" : "Late") + family);
                var literals = bind.Body.Instructions.Where(i => i.OpCode == OpCodes.Ldstr).Select(i => (string)i.Operand).ToArray();
                Require(literals.SequenceEqual(new[] { name.Replace('/', '+') + ", " + assembly, "Create" }), "nominal binding lost its assembly-qualified literal");
            }
        }
        if (state == "original")
        {
            Console.WriteLine("graph-nominal=original:shared-module,distinct-assemblies,distinct-mvids,top-and-nested,first-and-late-call-structure");
            return;
        }
        foreach (bool nested in new[] { false, true }.Where(value => section == "all" || section == (value ? "nested" : "top")))
        {
            string family = nested ? "Nested" : "Top"; string payload = nested ? "Graph.NominalContainer/Payload" : "Graph.NominalTopPayload";
            Body(library.MainModule, "Graph.Nominal" + family + "Factory`1", "Create");
            foreach (bool first in new[] { true, false })
            {
                bool fromLibrary = nested == first; var module = fromLibrary ? library.MainModule : app.MainModule;
                string origin = fromLibrary ? "Library" : "Application";
                foreach (string name in new[] { ".ctor", "ConstructorLeaf" }) Body(module, payload, name);
                Body(module, "Graph.Nominal" + family + origin + "CtorDependency", "Touch");
                var unused = module.GetType(payload)?.Methods.SingleOrDefault(m => m.Name == "Unselected");
                Require(unused is null || unused.HasBody && Stub(unused), "nominal binding opened an unrelated ordinary body");
                Require(module.GetType("Graph.Nominal" + family + origin + "UnselectedDependency") is null, "nominal binding retained an unrelated body dependency");
                Console.WriteLine("graph-nominal-binding=" + family.ToLowerInvariant() + (first ? ":first:" : ":late:") + module.Assembly.Name.Name + ":body");
            }
        }
        Console.WriteLine("graph-nominal=published:" + section);
    }

    static void Body(ModuleDefinition module, string owner, string name)
    {
        var method = module.GetType(owner)?.Methods.SingleOrDefault(m => m.Name == name);
        Require(method is { HasBody: true } && method.Body.Instructions.Count != 0 && !Stub(method), "nominal binding target body was lost: " + module.Assembly.Name.Name + ":" + owner + "::" + name);
    }

    static bool Stub(MethodDefinition method) => method.Body.Instructions.Select(i => i.OpCode.Code).SequenceEqual(new[] { Code.Ldc_I4, Code.Pop, Code.Ldnull, Code.Throw });
    static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
