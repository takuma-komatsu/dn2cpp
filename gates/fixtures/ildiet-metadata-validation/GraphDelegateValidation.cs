using Mono.Cecil;
using Mono.Cecil.Cil;

static class GraphDelegateValidation
{
    static readonly string[] Arms = { "dynamic-return", "dynamic-ref", "reflection-return", "reflection-ref", "bind-return", "bind-ref", "none-return", "none-ref" };

    internal static bool Run(string[] args)
    {
        if (args.Length == 4 && args[0] == "--create-delegate-box-fixtures")
        {
            Require(Arms.Contains(args[3]), "unknown delegate boxing arm");
            Create(args[1], args[2], args[3]);
            Console.WriteLine("fixture-created=delegate-boxes arm=" + args[3]);
            return true;
        }
        if ((args.Length is 3 or 4) && args[0] == "--check-delegate-box-fixtures")
        {
            Require(Arms.Contains(args[2]), "unknown delegate boxing arm");
            string order = args.Length == 4 ? args[3] : "all";
            Require(order is "all" or "First" or "Late" or "Unallocated", "unknown delegate discovery order");
            Check(args[1], args[2], order);
            return true;
        }
        return false;
    }

    static string Prefix(string arm, string order) => "DelegateBox" + string.Concat(arm.Split('-').Select(s => char.ToUpperInvariant(s[0]) + s[1..])) + order;

    static void Create(string directory, string corelib, string arm)
    {
        Directory.CreateDirectory(directory);
        using var core = AssemblyDefinition.ReadAssembly(corelib);
        using var library = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition("GraphLib", new Version(1, 0)), "GraphLib", ModuleKind.Dll);
        using var app = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition("GraphApp", new Version(1, 0)), "GraphApp", ModuleKind.Console);
        var module = library.MainModule;
        TypeReference Core(string name) => module.ImportReference(core.MainModule.GetType("System." + name));
        var objectType = Core("Object"); var voidType = Core("Void");
        bool byReference = arm.EndsWith("-ref", StringComparison.Ordinal);
        string capability = arm[..arm.LastIndexOf('-')];
        TypeDefinition Type(string name, TypeReference? parent = null, TypeAttributes flags = TypeAttributes.Public | TypeAttributes.Class)
        {
            var type = new TypeDefinition("Graph", name, flags, parent ?? objectType); module.Types.Add(type); return type;
        }
        MethodDefinition Method(TypeDefinition owner, string name, TypeReference? result = null, MethodAttributes flags = MethodAttributes.Public | MethodAttributes.Static)
        {
            var method = new MethodDefinition(name, flags, result ?? voidType); owner.Methods.Add(method); method.Body.Instructions.Add(Instruction.Create(OpCodes.Ret)); return method;
        }
        MethodReference Api(string owner, string name, params string[] parameters) => module.ImportReference(core.MainModule.GetType("System." + owner).Methods.Single(m => m.IsPublic && m.Name == name
            && m.Parameters.Select(p => p.ParameterType.FullName).SequenceEqual(parameters)));
        var fromHandle = Api("Type", "GetTypeFromHandle", "System.RuntimeTypeHandle");
        void TypeValue(MethodDefinition method, TypeReference type)
        {
            method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldtoken, type)); method.Body.Instructions.Add(Instruction.Create(OpCodes.Call, fromHandle));
        }
        (TypeDefinition Type, MethodDefinition Constructor) Delegate(string name, TypeReference result, TypeReference? parameter = null)
        {
            var type = Type(name, Core("MulticastDelegate"), TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Sealed);
            var ctor = new MethodDefinition(".ctor", MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName | MethodAttributes.HideBySig, voidType)
                { ImplAttributes = MethodImplAttributes.Runtime | MethodImplAttributes.Managed };
            ctor.Parameters.Add(new ParameterDefinition(objectType)); ctor.Parameters.Add(new ParameterDefinition(Core("IntPtr"))); type.Methods.Add(ctor);
            var invoke = new MethodDefinition("Invoke", MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.NewSlot | MethodAttributes.HideBySig, result)
                { ImplAttributes = MethodImplAttributes.Runtime | MethodImplAttributes.Managed };
            if (parameter is not null) invoke.Parameters.Add(new ParameterDefinition(parameter)); type.Methods.Add(invoke); return (type, ctor);
        }
        (TypeDefinition Payload, MethodDefinition Read, MethodDefinition Constructor) Pair(string order)
        {
            string prefix = Prefix(arm, order);
            var payload = Type(prefix + "Payload", Core("ValueType"), TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.SequentialLayout);
            var bodyDependency = Type(prefix + "BodyDependency"); var bodyTouch = Method(bodyDependency, "Touch");
            var leaf = Method(payload, "ToStringLeaf", flags: MethodAttributes.Private | MethodAttributes.Static);
            var text = Method(payload, "ToString", Core("String"), MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig);
            text.Body.Instructions.Clear(); text.Body.Instructions.Add(Instruction.Create(OpCodes.Call, bodyTouch)); text.Body.Instructions.Add(Instruction.Create(OpCodes.Call, leaf));
            text.Body.Instructions.Add(Instruction.Create(OpCodes.Ldstr, order)); text.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            var unused = Type(prefix + "UnselectedDependency"); Method(payload, "Unselected").Body.Instructions.Insert(0, Instruction.Create(OpCodes.Call, Method(unused, "Touch")));
            var owner = Type(prefix + "Owner"); var read = Method(owner, "Read", byReference ? voidType : payload);
            if (byReference) read.Parameters.Add(new ParameterDefinition(new ByReferenceType(payload)));
            else
            {
                var field = new FieldDefinition("Value", FieldAttributes.Public | FieldAttributes.Static, payload); owner.Fields.Add(field);
                read.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Ldsfld, field));
            }
            var signature = Delegate(prefix + "Delegate", byReference ? voidType : payload, byReference ? new ByReferenceType(payload) : null);
            return (payload, read, signature.Constructor);
        }
        var first = Pair("First"); var late = Pair("Late"); var unallocated = Pair("Unallocated");
        var driver = Type("DelegateBoxDriver"); driver.Fields.Add(new FieldDefinition("Unallocated", FieldAttributes.Public | FieldAttributes.Static, unallocated.Constructor.DeclaringType));
        var use = Method(driver, "UseCapability"); use.Body.Instructions.Clear();
        use.Parameters.Add(new ParameterDefinition(Core("Delegate"))); use.Parameters.Add(new ParameterDefinition(new ArrayType(objectType)));
        if (capability == "dynamic")
        {
            use.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0)); use.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_1));
            use.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, Api("Delegate", "DynamicInvoke", "System.Object[]"))); use.Body.Instructions.Add(Instruction.Create(OpCodes.Pop));
        }
        else if (capability == "reflection")
        {
            use.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0)); use.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, Api("Object", "GetType")));
            use.Body.Instructions.Add(Instruction.Create(OpCodes.Ldstr, "Invoke")); use.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, Api("Type", "GetMethod", "System.String")));
            use.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0)); use.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_1));
            use.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, Api("Reflection.MethodBase", "Invoke", "System.Object", "System.Object[]"))); use.Body.Instructions.Add(Instruction.Create(OpCodes.Pop));
        }
        else if (capability == "bind")
        {
            var unrelated = Type("UnrelatedBindingOwner"); Method(unrelated, "Noop");
            var unrelatedDelegate = Delegate("UnrelatedBindingDelegate", voidType);
            TypeValue(use, unrelatedDelegate.Type); TypeValue(use, unrelated); use.Body.Instructions.Add(Instruction.Create(OpCodes.Ldstr, "Noop"));
            use.Body.Instructions.Add(Instruction.Create(OpCodes.Call, Api("Delegate", "CreateDelegate", "System.Type", "System.Type", "System.String"))); use.Body.Instructions.Add(Instruction.Create(OpCodes.Pop));
        }
        use.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        void Allocate(MethodDefinition method, MethodDefinition read, MethodDefinition ctor)
        {
            method.Body.Instructions.Clear(); method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldnull)); method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldftn, read));
            method.Body.Instructions.Add(Instruction.Create(OpCodes.Newobj, ctor)); method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4, byReference ? 1 : 0));
            method.Body.Instructions.Add(Instruction.Create(OpCodes.Newarr, objectType)); method.Body.Instructions.Add(Instruction.Create(OpCodes.Call, use)); method.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        }
        var start = Method(driver, "Start"); Allocate(start, first.Read, first.Constructor);
        var bindLate = Method(driver, "BindLate"); Allocate(bindLate, late.Read, late.Constructor);
        var schedule = Method(driver, "Schedule"); schedule.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Call, bindLate));
        var entry = new TypeDefinition("Graph", "Entry", TypeAttributes.Public | TypeAttributes.Class, app.MainModule.ImportReference(objectType)); app.MainModule.Types.Add(entry);
        var main = new MethodDefinition("Main", MethodAttributes.Public | MethodAttributes.Static, app.MainModule.ImportReference(voidType)); entry.Methods.Add(main); app.EntryPoint = main;
        main.Body.Instructions.Add(Instruction.Create(OpCodes.Call, app.MainModule.ImportReference(start))); main.Body.Instructions.Add(Instruction.Create(OpCodes.Call, app.MainModule.ImportReference(schedule))); main.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        foreach (var payload in new[] { first.Payload, late.Payload, unallocated.Payload })
            foreach (var method in module.GetTypes().SelectMany(t => t.Methods).Where(m => m.HasBody))
            {
                Require(!method.Body.Variables.Any(v => v.VariableType.GetElementType().FullName == payload.FullName), "delegate fixture contains a payload local");
                foreach (var instruction in method.Body.Instructions)
                    Require(!(instruction.Operand is TypeReference type && type.GetElementType().FullName == payload.FullName)
                        && !(instruction.OpCode == OpCodes.Newobj && instruction.Operand is MethodReference ctor && ctor.DeclaringType.GetElementType().FullName == payload.FullName), "delegate fixture contains an executable payload root");
            }
        library.Write(Path.Combine(directory, "GraphLib.dll"), new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        app.Write(Path.Combine(directory, "GraphApp.dll"), new WriterParameters { Timestamp = 0, DeterministicMvid = true });
    }

    static void Check(string directory, string arm, string order)
    {
        using var library = AssemblyDefinition.ReadAssembly(Path.Combine(directory, "GraphLib.dll"));
        var module = library.MainModule;
        bool enabled = !arm.StartsWith("none-", StringComparison.Ordinal);
        MethodDefinition? Find(string owner, string name) => module.GetType("Graph." + owner)?.Methods.SingleOrDefault(m => m.Name == name);
        void Body(string owner, string name) => Require(Find(owner, name) is { HasBody: true } method && method.Body.Instructions.Count != 0 && !Stub(method), "delegate boxing target body was lost: " + owner + "::" + name);
        void Unexecuted(string owner, string name) => Require(Find(owner, name) is not { } method || method.HasBody && Stub(method), "delegate boxing opened an unrelated body: " + owner + "::" + name);
        foreach (string selected in new[] { "First", "Late", "Unallocated" }.Where(s => order == "all" || s == order))
        {
            string prefix = Prefix(arm, selected);
            Require(module.GetType("Graph." + prefix + "Delegate") is { HasGenericParameters: false }, "fixture lost its nongeneric delegate identity");
            if (selected != "Unallocated") Body(prefix + "Owner", "Read");
            if (enabled && selected != "Unallocated")
            {
                Body(prefix + "Payload", "ToString"); Body(prefix + "Payload", "ToStringLeaf"); Body(prefix + "BodyDependency", "Touch");
            }
            else
            {
                Require(Find(prefix + "Payload", "ToString") is { HasBody: true } method && Stub(method), "unarmed or unallocated delegate boxed its payload: " + prefix);
                Unexecuted(prefix + "BodyDependency", "Touch");
                if (!enabled) Require(module.GetType("Graph." + prefix + "BodyDependency") is null, "unarmed delegate retained its payload body dependency");
            }
            Unexecuted(prefix + "Payload", "Unselected"); Unexecuted(prefix + "UnselectedDependency", "Touch");
            Console.WriteLine("graph-delegate-box=" + arm + ":" + selected + (enabled && selected != "Unallocated" ? ":body" : ":stub"));
        }
    }

    static bool Stub(MethodDefinition method) => method.Body.Instructions.Select(i => i.OpCode.Code).SequenceEqual(new[] { Code.Ldc_I4, Code.Pop, Code.Ldnull, Code.Throw });
    static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
