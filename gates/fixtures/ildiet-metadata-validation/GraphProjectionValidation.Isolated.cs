using Mono.Cecil;
using Mono.Cecil.Cil;

static partial class GraphProjectionValidation
{
    static readonly string[] InstantiationModes = { "method-first", "method-only", "type-first", "type-late", "type-only", "method-known", "method-mixed", "method-static" };

    internal static bool Run(string[] args)
    {
        if (args.Length == 4 && args[0] == "--create-instantiation-fixtures")
        {
            Require(InstantiationModes.Contains(args[3]), "unknown instantiation fixture mode");
            CreateInstantiation(args[1], args[2], args[3]);
            Console.WriteLine("fixture-created=discarded-instantiation mode=" + args[3]);
            return true;
        }
        if (args.Length == 4 && args[0] == "--check-instantiation-fixtures")
        {
            Require(InstantiationModes.Contains(args[2]) && (args[3] is "original" or "published"), "unknown instantiation fixture mode or state");
            CheckInstantiation(args[1], args[2], args[3]);
            return true;
        }
        return false;
    }

    static void CreateInstantiation(string directory, string corelib, string mode)
    {
        Directory.CreateDirectory(directory);
        using var core = AssemblyDefinition.ReadAssembly(corelib);
        using var app = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition("GraphApp", new Version(1, 0)), "GraphApp", ModuleKind.Console);
        using var library = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition("GraphLib", new Version(1, 0)), "GraphLib", ModuleKind.Dll);
        var module = app.MainModule;
        TypeReference Core(ModuleDefinition target, string name) => target.ImportReference(core.MainModule.GetType("System." + name));
        MethodReference Api(string owner, string name, params string[] parameters) => module.ImportReference(core.MainModule.GetType("System." + owner).Methods.Single(m => m.IsPublic && m.Name == name
            && m.Parameters.Select(p => p.ParameterType.FullName).SequenceEqual(parameters)));
        TypeDefinition Type(ModuleDefinition target, string name)
        {
            var type = new TypeDefinition("Graph", name, TypeAttributes.Public | TypeAttributes.Class, Core(target, "Object"));
            target.Types.Add(type);
            return type;
        }
        MethodDefinition Method(TypeDefinition owner, string name, TypeReference? result = null)
        {
            var method = new MethodDefinition(name, MethodAttributes.Public | MethodAttributes.Static, result ?? Core(owner.Module, "Void"));
            owner.Methods.Add(method);
            return method;
        }
        MethodReference Dependency(string name)
        {
            var touch = Method(Type(library.MainModule, name), "Touch");
            touch.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            return module.ImportReference(touch);
        }
        var objectCtor = module.ImportReference(core.MainModule.GetType("System.Object").Methods.Single(m => m.IsConstructor && !m.IsStatic && m.Parameters.Count == 0));
        TypeDefinition Payload(string name, string dependency)
        {
            var type = Type(module, name);
            var ctor = new MethodDefinition(".ctor", MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName, Core(module, "Void"));
            type.Methods.Add(ctor);
            ctor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            ctor.Body.Instructions.Add(Instruction.Create(OpCodes.Call, objectCtor));
            ctor.Body.Instructions.Add(Instruction.Create(OpCodes.Call, Dependency(dependency)));
            ctor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            return type;
        }
        var payload = Payload("InstantiationPayload", "InstantiationCtorDependency");
        var dormantPayload = Payload("InstantiationDormantPayload", "InstantiationDormantCtorDependency");
        var activator = module.ImportReference(core.MainModule.GetType("System.Activator").Methods.Single(m => m.IsPublic && m.IsStatic && m.Name == "CreateInstance"
            && m.GenericParameters.Count == 1 && m.Parameters.Count == 0));
        MethodDefinition Factory(TypeDefinition owner, bool typeParameter, bool dormant = false)
        {
            var create = Method(owner, "Create", Core(module, "Object"));
            var parameter = new GenericParameter("T", typeParameter ? (IGenericParameterProvider)owner : create)
            {
                Attributes = GenericParameterAttributes.DefaultConstructorConstraint,
            };
            if (typeParameter) owner.GenericParameters.Add(parameter);
            else create.GenericParameters.Add(parameter);
            if (dormant) create.Body.Instructions.Add(Instruction.Create(OpCodes.Call, Dependency("InstantiationDormantBodyDependency")));
            var construct = new GenericInstanceMethod(activator);
            construct.GenericArguments.Add(parameter);
            create.Body.Instructions.Add(Instruction.Create(OpCodes.Call, construct));
            create.Body.Instructions.Add(Instruction.Create(OpCodes.Box, parameter));
            create.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            return create;
        }
        bool typeCase = mode.StartsWith("type-", StringComparison.Ordinal);
        bool executes = !mode.EndsWith("-only", StringComparison.Ordinal);
        bool late = mode.EndsWith("-late", StringComparison.Ordinal);
        bool known = mode == "method-known", mixed = mode == "method-mixed", direct = mode == "method-static";
        var owner = Type(module, typeCase ? "InstantiationTypeFactory`1" : "InstantiationMethodFactory");
        var create = Factory(owner, typeCase);
        var unselected = Method(owner, "Unselected");
        unselected.Body.Instructions.Add(Instruction.Create(OpCodes.Call, Dependency("InstantiationUnselectedDependency")));
        unselected.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        var dormantOwner = Type(module, "InstantiationDormantFactory");
        Factory(dormantOwner, false, dormant: true);
        Dependency("InstantiationProviderDependency");
        var provider = Method(Type(library.MainModule, "InstantiationUnusedProvider"), "Read");
        provider.Body.Instructions.Add(Instruction.Create(OpCodes.Call, library.MainModule.GetType("Graph.InstantiationProviderDependency").Methods.Single()));
        provider.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        var driver = Type(module, "InstantiationDriver");
        void TypeValue(MethodDefinition method, TypeReference type)
        {
            method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldtoken, type));
            method.Body.Instructions.Add(Instruction.Create(OpCodes.Call, Api("Type", "GetTypeFromHandle", "System.RuntimeTypeHandle")));
        }
        void Vector(MethodDefinition method, TypeReference argument)
        {
            method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_1));
            method.Body.Instructions.Add(Instruction.Create(OpCodes.Newarr, Core(module, "Type")));
            method.Body.Instructions.Add(Instruction.Create(OpCodes.Dup));
            method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_0));
            TypeValue(method, argument);
            method.Body.Instructions.Add(Instruction.Create(OpCodes.Stelem_Ref));
        }
        void Receiver(MethodDefinition method, TypeReference target, bool genericType)
        {
            TypeValue(method, target);
            if (!genericType)
            {
                method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldstr, "Create"));
                method.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, Api("Type", "GetMethod", "System.String")));
            }
        }
        var form = Method(driver, "FormKnown");
        Receiver(form, owner, typeCase);
        Vector(form, payload);
        form.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, typeCase ? Api("Type", "MakeGenericType", "System.Type[]")
            : Api("Reflection.MethodInfo", "MakeGenericMethod", "System.Type[]")));
        form.Body.Instructions.Add(Instruction.Create(OpCodes.Pop));
        Receiver(form, dormantOwner, false);
        Vector(form, dormantPayload);
        form.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, Api("Reflection.MethodInfo", "MakeGenericMethod", "System.Type[]")));
        form.Body.Instructions.Add(Instruction.Create(OpCodes.Pop));
        form.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        var invoke = Method(driver, "InvokeUnknown");
        invoke.Parameters.Add(new ParameterDefinition(new ArrayType(Core(module, "Type"))));
        Receiver(invoke, owner, typeCase);
        if (known) Vector(invoke, payload);
        else if (mixed)
        {
            var unknown = Instruction.Create(OpCodes.Ldarg_0);
            var join = Instruction.Create(OpCodes.Nop);
            invoke.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            invoke.Body.Instructions.Add(Instruction.Create(OpCodes.Brtrue, unknown));
            Vector(invoke, payload);
            invoke.Body.Instructions.Add(Instruction.Create(OpCodes.Br, join));
            invoke.Body.Instructions.Add(unknown);
            invoke.Body.Instructions.Add(join);
        }
        else invoke.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
        invoke.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, typeCase ? Api("Type", "MakeGenericType", "System.Type[]")
            : Api("Reflection.MethodInfo", "MakeGenericMethod", "System.Type[]")));
        if (typeCase)
        {
            invoke.Body.Instructions.Add(Instruction.Create(OpCodes.Ldstr, "Create"));
            invoke.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, Api("Type", "GetMethod", "System.String")));
        }
        invoke.Body.Instructions.Add(Instruction.Create(OpCodes.Ldnull));
        invoke.Body.Instructions.Add(Instruction.Create(OpCodes.Ldnull));
        invoke.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, Api("Reflection.MethodBase", "Invoke", "System.Object", "System.Object[]")));
        invoke.Body.Instructions.Add(Instruction.Create(OpCodes.Pop));
        invoke.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        var main = Method(Type(module, "Entry"), "Main");
        app.EntryPoint = main;
        if (!late) main.Body.Instructions.Add(Instruction.Create(OpCodes.Call, form));
        if (executes)
        {
            Vector(main, payload);
            main.Body.Instructions.Add(Instruction.Create(OpCodes.Call, invoke));
        }
        if (late)
        {
            var schedule = Method(driver, "Schedule");
            schedule.Body.Instructions.Add(Instruction.Create(OpCodes.Call, form));
            schedule.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            main.Body.Instructions.Add(Instruction.Create(OpCodes.Call, schedule));
        }
        if (direct)
        {
            var closed = new GenericInstanceMethod(create);
            closed.GenericArguments.Add(payload);
            main.Body.Instructions.Add(Instruction.Create(OpCodes.Call, closed));
            main.Body.Instructions.Add(Instruction.Create(OpCodes.Pop));
        }
        TypeValue(main, module.ImportReference(Type(library.MainModule, "InstantiationAnchor")));
        main.Body.Instructions.Add(Instruction.Create(OpCodes.Pop));
        main.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        app.Write(Path.Combine(directory, "GraphApp.dll"), new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        library.Write(Path.Combine(directory, "GraphLib.dll"), new WriterParameters { Timestamp = 0, DeterministicMvid = true });
    }

    static void CheckInstantiation(string directory, string mode, string state)
    {
        using var app = AssemblyDefinition.ReadAssembly(Path.Combine(directory, "GraphApp.dll"));
        using var library = AssemblyDefinition.ReadAssembly(Path.Combine(directory, "GraphLib.dll"));
        var module = app.MainModule;
        var lib = library.MainModule;
        bool typeCase = mode.StartsWith("type-", StringComparison.Ordinal);
        bool executes = !mode.EndsWith("-only", StringComparison.Ordinal);
        bool late = mode.EndsWith("-late", StringComparison.Ordinal);
        bool known = mode == "method-known", mixed = mode == "method-mixed", direct = mode == "method-static";
        string ownerName = typeCase ? "InstantiationTypeFactory`1" : "InstantiationMethodFactory";
        var owner = module.GetType("Graph." + ownerName);
        var create = owner.Methods.Single(m => m.Name == "Create");
        Require(create.IsPublic && create.IsStatic && !create.IsVirtual && create.Parameters.Count == 0 && create.ReturnType.FullName == "System.Object"
            && owner.GenericParameters.Count == (typeCase ? 1 : 0) && create.GenericParameters.Count == (typeCase ? 0 : 1)
            && (typeCase ? owner.GenericParameters[0] : create.GenericParameters[0]).HasDefaultConstructorConstraint
            && owner.CustomAttributes.Count == 0 && !owner.HasInterfaces && owner.Methods.All(m => !m.IsConstructor), "instantiation fixture changed its factory definition or rooting surface");
        var driver = module.GetType("Graph.InstantiationDriver");
        var form = driver.Methods.Single(m => m.Name == "FormKnown");
        var code = form.Body.Instructions;
        Require(code.Count(i => i.OpCode == OpCodes.Pop) == 2
            && code.Count(i => i.OpCode == OpCodes.Newarr && i.Operand is TypeReference type && type.FullName == "System.Type") == 2
            && code.Count(i => i.OpCode == OpCodes.Stelem_Ref) == 2
            && code.Count(i => i.Operand is MethodReference method && method.Name == "MakeGenericMethod") == (typeCase ? 1 : 2)
            && code.Count(i => i.Operand is MethodReference method && method.Name == "MakeGenericType") == (typeCase ? 1 : 0)
            && code.Where(i => i.Operand is MethodReference method && method.Name is "MakeGenericMethod" or "MakeGenericType").All(i => i.Next.OpCode == OpCodes.Pop), "instantiation fixture lost a discarded exact formation");
        Require(code.Where(i => i.OpCode == OpCodes.Ldtoken).Select(i => ((TypeReference)i.Operand).FullName).SequenceEqual(new[]
            { "Graph." + ownerName, "Graph.InstantiationPayload", "Graph.InstantiationDormantFactory", "Graph.InstantiationDormantPayload" }), "instantiation fixture changed the two exact formation tuples");
        var main = app.EntryPoint;
        var calls = main.Body.Instructions.Where(i => i.OpCode == OpCodes.Call && i.Operand is MethodReference method && method.DeclaringType.FullName == driver.FullName)
            .Select(i => ((MethodReference)i.Operand).Name).ToArray();
        Require(calls.SequenceEqual(late ? new[] { "InvokeUnknown", "Schedule" } : executes ? new[] { "FormKnown", "InvokeUnknown" } : new[] { "FormKnown" }), "instantiation fixture changed its first or late binding schedule");
        var directCalls = main.Body.Instructions.Where(i => i.OpCode == OpCodes.Call && i.Operand is GenericInstanceMethod instance
            && instance.ElementMethod.FullName == create.FullName).ToArray();
        Require(directCalls.Length == (direct ? 1 : 0) && directCalls.All(i => ((GenericInstanceMethod)i.Operand).GenericArguments.Single().FullName == "Graph.InstantiationPayload"),
            "instantiation static control changed its independently named closed target");
        var schedule = driver.Methods.SingleOrDefault(m => m.Name == "Schedule");
        Require((schedule is not null) == late && (!late || schedule!.Body.Instructions.Select(i => i.OpCode.Code).SequenceEqual(new[] { Code.Call, Code.Ret })
            && schedule.Body.Instructions[0].Operand is MethodReference scheduled && scheduled.FullName == form.FullName), "late instantiation no longer binds after the unknown helper body");
        var invoke = driver.Methods.Single(m => m.Name == "InvokeUnknown");
        Require(invoke.Parameters.Count == 1 && invoke.Parameters[0].ParameterType.FullName == "System.Type[]", "instantiation helper lost its unknown vector parameter");
        if (state == "original" || executes)
        {
            var formation = invoke.Body.Instructions.Single(i => i.Operand is MethodReference method && method.Name == (typeCase ? "MakeGenericType" : "MakeGenericMethod"));
            Require(formation.Previous.OpCode == (known ? OpCodes.Stelem_Ref : mixed ? OpCodes.Nop : OpCodes.Ldarg_0)
                && invoke.Body.Instructions.Count(i => i.Operand is MethodReference method && method.Name == "Invoke" && method.DeclaringType.FullName == "System.Reflection.MethodBase") == 1
                && invoke.Body.Instructions.Count(i => i.OpCode == OpCodes.Newarr) == (known || mixed ? 1 : 0)
                && invoke.Body.Instructions.Count(i => i.OpCode == OpCodes.Brtrue) == (mixed ? 1 : 0), "instantiation execution no longer uses the unknown helper vector");
        }
        var payload = module.GetType("Graph.InstantiationPayload");
        var ctor = payload.Methods.Single(m => m.IsConstructor);
        Require(ctor.IsPublic && !ctor.IsStatic && !ctor.IsVirtual && ctor.Parameters.Count == 0 && payload.CustomAttributes.Count == 0, "instantiation payload gained an independent executable root");
        Require(lib.GetType("Graph.InstantiationAnchor") is { Methods.Count: 0 }, "instantiation fixture lost its metadata-only library anchor");
        if (state == "original")
        {
            Require(module.GetTypes().SelectMany(t => t.Methods).Where(m => m.HasBody).SelectMany(m => m.Body.Instructions).All(i => i.OpCode != OpCodes.Newobj
                && (i.Operand is not GenericInstanceMethod instance || instance.GenericArguments.All(a => a is GenericParameter)
                    || direct && instance.ElementMethod.FullName == create.FullName && instance.GenericArguments.Single().FullName == payload.FullName)
                && i.Operand is not GenericInstanceType), "instantiation fixture directly names a closed body or constructs a payload");
            Require(create.Body.Instructions.Any(i => i.Operand is GenericInstanceMethod instance && instance.Name == "CreateInstance"
                && instance.DeclaringType.FullName == "System.Activator" && instance.GenericArguments.Single() is GenericParameter), "instantiation factory lost its generic constructor demand");
            foreach (string name in new[] { "InstantiationCtorDependency", "InstantiationDormantCtorDependency", "InstantiationDormantBodyDependency", "InstantiationUnselectedDependency", "InstantiationProviderDependency", "InstantiationUnusedProvider" })
                Require(lib.GetType("Graph." + name) is not null, "instantiation fixture lost an original unique dependency: " + name);
            Console.WriteLine("graph-instantiation=original:" + mode + (known || mixed || direct ? ":discarded-known,finite-or-static-control" : ":discarded-known,unknown-vector,no-closed-body-roots"));
            return;
        }
        bool Original(MethodDefinition? method) => method is { HasBody: true } && !InstantiationStub(method);
        bool selected = known || mixed || direct;
        Require(Original(create) == selected, "instantiation selected factory body state differed: " + mode);
        Require(Original(ctor) == selected, "instantiation selected payload constructor body state differed: " + mode);
        Require((lib.GetType("Graph.InstantiationCtorDependency") is not null) == selected, "instantiation selected constructor dependency state differed: " + mode);
        if (selected) Body(lib, "InstantiationCtorDependency", "Touch");
        Unexecuted(module, ownerName, "Unselected");
        Unexecuted(module, "InstantiationDormantFactory", "Create");
        Unexecuted(module, "InstantiationDormantPayload", ".ctor");
        foreach (string name in new[] { "InstantiationDormantCtorDependency", "InstantiationDormantBodyDependency", "InstantiationUnselectedDependency", "InstantiationProviderDependency", "InstantiationUnusedProvider" })
            Require(lib.GetType("Graph." + name) is null, "instantiation formation executed an unrelated body: " + name);
        Console.WriteLine("graph-instantiation=published:" + mode + (executes ? ":finite-or-static-execution,unknown-no-body,dormant-and-provider-negative"
            : ":selected-constructor-dependency,formation-only,dormant-and-provider-negative"));
    }

    static bool InstantiationStub(MethodDefinition method) => method.Body.Instructions.Select(i => i.OpCode.Code)
        .SequenceEqual(new[] { Code.Ldc_I4, Code.Pop, Code.Ldnull, Code.Throw });
}
