using Mono.Cecil;
using Mono.Cecil.Cil;

static partial class GraphLookupValidation
{
    static readonly string[] SelectorModes = { "symbolic-first", "symbolic-late", "literal-complete", "null-mixed", "null-literal-complete" };

    internal static bool Run(string[] args)
    {
        if (args.Length == 4 && args[0] == "--create-selector-fixtures")
        {
            Require(SelectorModes.Contains(args[3]), "unknown selector fixture mode"); CreateSelector(args[1], args[2], args[3]);
            Console.WriteLine("fixture-created=selector-vectors mode=" + args[3]); return true;
        }
        if (args.Length == 4 && args[0] == "--check-selector-fixtures")
        {
            Require(SelectorModes.Contains(args[2]) && (args[3] is "original" or "published"), "unknown selector fixture mode or state");
            CheckSelector(args[1], args[2], args[3]); return true;
        }
        return false;
    }

    static void CreateSelector(string directory, string corelib, string mode)
    {
        Directory.CreateDirectory(directory);
        using var core = AssemblyDefinition.ReadAssembly(corelib);
        using var app = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition("GraphApp", new Version(1, 0)), "GraphApp", ModuleKind.Console);
        using var library = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition("GraphLib", new Version(1, 0)), "GraphLib", ModuleKind.Dll);
        var module = app.MainModule; var lib = library.MainModule;
        TypeReference Core(ModuleDefinition target, string name) => target.ImportReference(core.MainModule.GetType("System." + name));
        MethodReference Api(string owner, string name, params string[] parameters) => module.ImportReference(core.MainModule.GetType("System." + owner).Methods.Single(m => m.IsPublic && m.Name == name
            && m.Parameters.Select(p => p.ParameterType.FullName).SequenceEqual(parameters)));
        TypeDefinition Type(ModuleDefinition target, string name)
        {
            var type = new TypeDefinition("Graph", name, TypeAttributes.Public | TypeAttributes.Class, Core(target, "Object")); target.Types.Add(type); return type;
        }
        MethodDefinition Method(TypeDefinition owner, string name)
        {
            var method = new MethodDefinition(name, MethodAttributes.Public | MethodAttributes.Static, Core(owner.Module, "Void")); owner.Methods.Add(method); method.Body.Instructions.Add(Instruction.Create(OpCodes.Ret)); return method;
        }
        void TypeValue(MethodDefinition method, TypeReference type)
        {
            method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldtoken, type)); method.Body.Instructions.Add(Instruction.Create(OpCodes.Call, Api("Type", "GetTypeFromHandle", "System.RuntimeTypeHandle")));
        }
        var targetOwner = Type(lib, "SelectorOwner"); bool nullCase = mode.StartsWith("null-", StringComparison.Ordinal);
        foreach (int count in nullCase ? new[] { 0 } : new[] { 1, 3 })
        {
            string name = count == 0 ? "Zero" : count == 1 ? "One" : "Three";
            var target = Method(targetOwner, "Run");
            for (int i = 0; i < count; i++) target.Parameters.Add(new ParameterDefinition(Core(lib, "Int32")));
            target.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Call, Method(Type(lib, "Selector" + name + "Dependency"), "Touch")));
        }
        Method(targetOwner, "Unselected").Body.Instructions.Insert(0, Instruction.Create(OpCodes.Call, Method(Type(lib, "SelectorUnselectedDependency"), "Touch")));
        var driver = Type(module, "SelectorDriver"); var read = Method(driver, "Read"); read.Body.Instructions.Clear();
        if (!nullCase) read.GenericParameters.Add(new GenericParameter("T", read));
        read.Parameters.Add(new ParameterDefinition(Core(module, "Boolean"))); read.Parameters.Add(new ParameterDefinition(new ArrayType(Core(module, "Object"))));
        read.Body.InitLocals = true; read.Body.Variables.Add(new VariableDefinition(new ArrayType(Core(module, "Type"))));
        void LiteralVector()
        {
            read.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_1)); read.Body.Instructions.Add(Instruction.Create(OpCodes.Newarr, Core(module, "Type")));
            read.Body.Instructions.Add(Instruction.Create(OpCodes.Dup)); read.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_0)); TypeValue(read, Core(module, "Int32")); read.Body.Instructions.Add(Instruction.Create(OpCodes.Stelem_Ref));
        }
        bool mixed = mode is "symbolic-first" or "symbolic-late" or "null-mixed";
        if (mixed)
        {
            var literal = Instruction.Create(OpCodes.Nop); var join = Instruction.Create(OpCodes.Nop);
            read.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0)); read.Body.Instructions.Add(Instruction.Create(OpCodes.Brtrue, literal));
            if (nullCase) read.Body.Instructions.Add(Instruction.Create(OpCodes.Ldnull));
            else
            {
                TypeValue(read, read.GenericParameters[0]); read.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, Api("Type", "GetGenericArguments")));
            }
            read.Body.Instructions.Add(Instruction.Create(OpCodes.Stloc_0)); read.Body.Instructions.Add(Instruction.Create(OpCodes.Br, join));
            read.Body.Instructions.Add(literal); LiteralVector(); read.Body.Instructions.Add(Instruction.Create(OpCodes.Stloc_0));
            read.Body.Instructions.Add(join);
        }
        else { LiteralVector(); read.Body.Instructions.Add(Instruction.Create(OpCodes.Stloc_0)); }
        TypeValue(read, module.ImportReference(targetOwner)); read.Body.Instructions.Add(Instruction.Create(OpCodes.Ldstr, "Run")); read.Body.Instructions.Add(Instruction.Create(OpCodes.Ldloc_0));
        read.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, Api("Type", "GetMethod", "System.String", "System.Type[]")));
        read.Body.Instructions.Add(Instruction.Create(OpCodes.Ldnull)); read.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_1));
        read.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, Api("Reflection.MethodBase", "Invoke", "System.Object", "System.Object[]")));
        read.Body.Instructions.Add(Instruction.Create(OpCodes.Pop)); read.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        TypeReference Closed(int arity)
        {
            var definition = Type(module, arity == 1 ? "SelectorSingle`1" : "SelectorTriple`3");
            for (int i = 0; i < arity; i++) definition.GenericParameters.Add(new GenericParameter("T" + i, definition));
            var instance = new GenericInstanceType(definition); for (int i = 0; i < arity; i++) instance.GenericArguments.Add(Core(module, "Int32")); return instance;
        }
        void CallRead(MethodDefinition caller, int arity)
        {
            caller.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_0)); caller.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4, arity)); caller.Body.Instructions.Add(Instruction.Create(OpCodes.Newarr, Core(module, "Object")));
            for (int i = 0; i < arity; i++)
            {
                caller.Body.Instructions.Add(Instruction.Create(OpCodes.Dup)); caller.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4, i)); caller.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_0));
                caller.Body.Instructions.Add(Instruction.Create(OpCodes.Box, Core(module, "Int32"))); caller.Body.Instructions.Add(Instruction.Create(OpCodes.Stelem_Ref));
            }
            MethodReference call = read;
            if (!nullCase)
            {
                var instance = new GenericInstanceMethod(read); instance.GenericArguments.Add(Closed(mode == "literal-complete" ? 3 : arity)); call = instance;
            }
            caller.Body.Instructions.Add(Instruction.Create(OpCodes.Call, call));
        }
        var start = Method(driver, "Start"); start.Body.Instructions.Clear(); CallRead(start, nullCase ? 0 : mode == "symbolic-first" ? 3 : 1);
        if (mode == "symbolic-late")
        {
            var schedule = Method(driver, "Schedule"); schedule.Body.Instructions.Clear(); CallRead(schedule, 3); schedule.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            start.Body.Instructions.Add(Instruction.Create(OpCodes.Call, schedule));
        }
        start.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        var main = Method(Type(module, "Entry"), "Main"); app.EntryPoint = main; main.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Call, start));
        app.Write(Path.Combine(directory, "GraphApp.dll"), new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        library.Write(Path.Combine(directory, "GraphLib.dll"), new WriterParameters { Timestamp = 0, DeterministicMvid = true });
    }

    static void CheckSelector(string directory, string mode, string state)
    {
        using var app = AssemblyDefinition.ReadAssembly(Path.Combine(directory, "GraphApp.dll"));
        using var library = AssemblyDefinition.ReadAssembly(Path.Combine(directory, "GraphLib.dll"));
        var own = app.MainModule; var lib = library.MainModule; var driver = own.GetType("Graph.SelectorDriver"); var read = driver.Methods.Single(m => m.Name == "Read");
        bool nullCase = mode.StartsWith("null-", StringComparison.Ordinal); bool mixed = mode is "symbolic-first" or "symbolic-late" or "null-mixed";
        var code = read.Body.Instructions;
        Require(read.IsStatic && read.GenericParameters.Count == (nullCase ? 0 : 1) && read.Parameters.Count == 2
            && read.Parameters[0].ParameterType.FullName == "System.Boolean" && read.Parameters[1].ParameterType.FullName == "System.Object[]"
            && read.Body.Variables.Single().VariableType.FullName == "System.Type[]", "selector fixture lost its original typed-array local and caller inputs");
        Require(code.Count(i => i.OpCode == OpCodes.Brtrue) == (mixed ? 1 : 0) && code.Count(i => i.OpCode == OpCodes.Newarr && i.Operand is TypeReference type && type.FullName == "System.Type") == 1
            && code.Single(i => i.OpCode == OpCodes.Newarr).Previous.OpCode == OpCodes.Ldc_I4_1
            && code.Count(i => i.OpCode == OpCodes.Stelem_Ref) == 1 && code.Count(i => i.OpCode == OpCodes.Stloc_0) == (mixed ? 2 : 1), "selector fixture changed its same-category branch or exact literal vector");
        var typeArguments = code.Where(i => i.Operand is MethodReference method && method.Name == "GetGenericArguments").ToArray();
        Require(typeArguments.Length == (!nullCase && mixed ? 1 : 0)
            && typeArguments.All(i => i.Previous.Previous is { OpCode.Code: Code.Ldtoken, Operand: GenericParameter parameter } && ReferenceEquals(parameter.Owner, read)), "selector fixture lost its symbolic sequence producer");
        Require(code.Count(i => i.OpCode == OpCodes.Ldnull) == (mode == "null-mixed" ? 2 : 1)
            && code.Count(i => i.Operand is MethodReference method && method.DeclaringType.FullName == "System.Type" && method.Name == "GetMethod"
                && method.Parameters.Select(p => p.ParameterType.FullName).SequenceEqual(new[] { "System.String", "System.Type[]" })) == 1
            && code.Any(i => i.Operand is MethodReference method && method.DeclaringType.FullName == "System.Reflection.MethodBase" && method.Name == "Invoke")
            && code.Any(i => i.OpCode == OpCodes.Ldstr && (string)i.Operand == "Run"), "selector fixture changed its null/vector shape or recognized lookup/invoke route");
        var start = driver.Methods.Single(m => m.Name == "Start"); var schedule = driver.Methods.SingleOrDefault(m => m.Name == "Schedule");
        var bindings = new List<int>();
        foreach (var method in new[] { start, schedule }.Where(m => m is not null))
            foreach (var call in method!.Body.Instructions.Select(i => i.Operand).OfType<GenericInstanceMethod>())
            {
                Require(call.ElementMethod.FullName == read.FullName && call.GenericArguments.Single() is GenericInstanceType type
                    && type.GenericArguments.All(a => a.FullName == "System.Int32") && type.GenericArguments.Count is 1 or 3
                    && type.ElementType.FullName == (type.GenericArguments.Count == 1 ? "Graph.SelectorSingle`1" : "Graph.SelectorTriple`3"), "selector fixture lost an exact closed original MethodSpec");
                bindings.Add(((GenericInstanceType)call.GenericArguments[0]).GenericArguments.Count);
            }
        Require(bindings.SequenceEqual(nullCase ? Array.Empty<int>() : mode == "symbolic-late" ? new[] { 1, 3 } : new[] { 3 })
            && (schedule is not null) == (mode == "symbolic-late"), "selector fixture changed its first or later generic binding order");
        if (schedule is not null)
            Require(start.Body.Instructions.Where(i => i.OpCode == OpCodes.Call).Select(i => ((MethodReference)i.Operand).Name).SequenceEqual(new[] { "Read", "Schedule" }), "selector fixture no longer discovers the second binding after the helper body");
        foreach (var caller in new[] { start, schedule }.Where(m => m is not null))
        {
            var instructions = caller!.Body.Instructions;
            var allocation = instructions.Single(i => i.OpCode == OpCodes.Newarr);
            int count = nullCase ? 0 : caller == schedule || mode == "symbolic-first" ? 3 : 1;
            Require(allocation.Operand is TypeReference element && element.FullName == "System.Object"
                && allocation.Previous.OpCode == OpCodes.Ldc_I4 && (int)allocation.Previous.Operand == count
                && allocation.Previous.Previous.OpCode == OpCodes.Ldc_I4_0
                && instructions.Count(i => i.OpCode == OpCodes.Stelem_Ref) == count
                && instructions.Count(i => i.OpCode == OpCodes.Box && i.Operand is TypeReference box && box.FullName == "System.Int32") == count,
                "selector caller lost its false branch and correctly sized boxed-Int32 argument vector");
        }
        Require(own.EntryPoint.Body.Instructions.Any(i => i.OpCode == OpCodes.Call && i.Operand is MethodReference method && method.FullName == start.FullName)
            && !own.GetTypes().SelectMany(t => t.Methods).Where(m => m.HasBody).SelectMany(m => m.Body.Instructions)
                .Any(i => i.Operand is MethodReference method && (method.DeclaringType.FullName == "Graph.SelectorOwner" || method.DeclaringType.Name.EndsWith("Dependency", StringComparison.Ordinal))), "selector fixture directly rooted a library body");
        var owner = lib.GetType("Graph.SelectorOwner");
        MethodDefinition? Find(int arity) => owner?.Methods.SingleOrDefault(m => m.Name == "Run" && m.Parameters.Count == arity);
        void Body(int arity) => Require(Find(arity) is { HasBody: true } method && !Stub(method), "selector target body was lost: Run/" + arity);
        void Leaf(string prefix) => Require(lib.GetType("Graph.Selector" + prefix + "Dependency")?.Methods.SingleOrDefault(m => m.Name == "Touch") is { HasBody: true } method && !Stub(method), "selector target dependency body was lost: " + prefix);
        if (state == "original")
        {
            Require(owner is { HasGenericParameters: false, Fields.Count: 0, CustomAttributes.Count: 0 }
                && owner.Methods.Count(m => m.Name == "Run") == (nullCase ? 1 : 2)
                && owner.Methods.All(m => m.IsStatic && m.IsPublic && !m.IsVirtual && !m.HasGenericParameters && m.Parameters.All(p => p.ParameterType.FullName == "System.Int32")), "selector fixture changed its original target surface");
            foreach (var method in owner!.Methods)
            {
                string dependency = method.Name == "Unselected" ? "Unselected" : method.Parameters.Count == 0 ? "Zero" : method.Parameters.Count == 1 ? "One" : "Three";
                Require(method.Body.Instructions.Select(i => i.OpCode.Code).SequenceEqual(new[] { Code.Call, Code.Ret })
                    && method.Body.Instructions[0].Operand is MethodReference target && target.Name == "Touch"
                    && target.DeclaringType.FullName == "Graph.Selector" + dependency + "Dependency", "selector fixture changed its unique external dependency body");
            }
            Console.WriteLine("graph-selector=original:" + mode + ":sequence-shapes,closed-bindings,no-body-roots"); return;
        }
        if (mode is "symbolic-first" or "symbolic-late") { Body(1); Leaf("One"); Body(3); Leaf("Three"); }
        else if (mode == "literal-complete")
        {
            Body(1); Leaf("One"); Require(Find(3) is not { } method || Stub(method), "complete vector opened the three-parameter overload");
            Require(lib.GetType("Graph.SelectorThreeDependency") is null, "complete vector retained the three-parameter body dependency");
        }
        else if (mode == "null-mixed") { Body(0); Leaf("Zero"); }
        else
        {
            Require(Find(0) is not { } method || Stub(method), "complete vector opened the parameterless overload");
            Require(lib.GetType("Graph.SelectorZeroDependency") is null, "complete vector retained the parameterless body dependency");
        }
        Require(owner?.Methods.SingleOrDefault(m => m.Name == "Unselected") is not { } unrelated || Stub(unrelated), "selector lookup opened an unrelated name");
        Require(lib.GetType("Graph.SelectorUnselectedDependency") is null, "selector lookup retained an unrelated body dependency");
        Console.WriteLine("graph-selector=published:" + mode + ":selected-bodies,complete-controls,unselected-negative");
    }
}
