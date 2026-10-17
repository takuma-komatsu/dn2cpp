using Mono.Cecil;
using Mono.Cecil.Cil;

static partial class GraphArrayValidation
{
    internal static bool Run(string[] args)
    {
        if (args.Length == 4 && args[0] == "--create-array-layout-fixtures")
        {
            CreateLayout(args[1], args[2], args[3]);
            Console.WriteLine("fixture-created=array-layout mode=" + args[3]);
            return true;
        }
        if (args.Length == 4 && args[0] == "--check-array-layout-fixtures")
        {
            CheckLayout(args[1], args[2], args[3]);
            return true;
        }
        return false;
    }

    private static void CreateLayout(string directory, string corelib, string mode)
    {
        Require(mode is "initialize-first" or "layout-first" or "unarmed", "unknown array layout mode");
        Directory.CreateDirectory(directory);
        using var core = AssemblyDefinition.ReadAssembly(corelib);
        using var library = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition("GraphLib", new Version(1, 0)), "GraphLib", ModuleKind.Dll);
        using var app = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition("GraphApp", new Version(1, 0)), "GraphApp", ModuleKind.Console);
        var module = app.MainModule;
        TypeReference Core(ModuleDefinition target, string name) => target.ImportReference(core.MainModule.GetType("System." + name));
        TypeDefinition Type(ModuleDefinition target, string name, bool value = false, bool generic = false)
        {
            var type = new TypeDefinition("Graph", name, value
                ? TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.SequentialLayout | TypeAttributes.BeforeFieldInit
                : TypeAttributes.Public | TypeAttributes.Class, Core(target, value ? "ValueType" : "Object"));
            target.Types.Add(type);
            if (generic) type.GenericParameters.Add(new GenericParameter("T", type));
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
            var type = Type(library.MainModule, name);
            var touch = Method(type, "Touch");
            touch.ReturnType = Core(library.MainModule, "Int32");
            touch.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4, 7));
            touch.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            return module.ImportReference(touch);
        }
        MethodDefinition Constructor(TypeDefinition type, string dependency, bool overload = false)
        {
            var ctor = Method(type, ".ctor", true);
            if (overload) ctor.Parameters.Add(new ParameterDefinition(Core(module, "Int32")));
            if (!type.IsValueType)
            {
                ctor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
                ctor.Body.Instructions.Add(Instruction.Create(OpCodes.Call, module.ImportReference(core.MainModule.GetType("System.Object")
                    .Methods.Single(m => m.IsConstructor && !m.IsStatic && m.Parameters.Count == 0))));
            }
            if (type.IsValueType) ctor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            ctor.Body.Instructions.Add(Instruction.Create(OpCodes.Call, Dependency(dependency)));
            ctor.Body.Instructions.Add(type.IsValueType ? Instruction.Create(OpCodes.Stfld, type.Fields.Single()) : Instruction.Create(OpCodes.Pop));
            ctor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            return ctor;
        }
        TypeDefinition Value(string name, string dependency, bool generic)
        {
            var type = Type(module, name, true, generic);
            type.Fields.Add(new FieldDefinition("Value", FieldAttributes.Public, Core(module, "Int32")));
            Constructor(type, dependency);
            return type;
        }
        GenericInstanceType Closed(TypeReference type, TypeReference argument)
        {
            var instance = new GenericInstanceType(type);
            instance.GenericArguments.Add(argument);
            return instance;
        }
        var cell = Value("ArrayLayoutCell`1", "ArrayLayoutCellDependency", true);
        Constructor(cell, "ArrayLayoutOverloadDependency", true);
        var ordinary = Method(cell, "Ordinary");
        ordinary.Body.Instructions.Add(Instruction.Create(OpCodes.Call, Dependency("ArrayLayoutOrdinaryDependency")));
        ordinary.Body.Instructions.Add(Instruction.Create(OpCodes.Pop));
        ordinary.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        Value("ArrayLayoutShell", "ArrayLayoutShellDependency", false);
        Value("ArrayLayoutMadeCell`1", "ArrayLayoutMadeDependency", true);
        Value("ArrayLayoutUnobserved`1", "ArrayLayoutUnobservedDependency", true);
        var outer = Type(module, "ArrayLayoutOuter`1", generic: true);
        var nested = Value("ArrayLayoutNested", "ArrayLayoutNestedDependency", true);
        module.Types.Remove(nested);
        nested.Namespace = "";
        nested.Attributes = (nested.Attributes & ~TypeAttributes.VisibilityMask) | TypeAttributes.NestedPublic;
        outer.NestedTypes.Add(nested);
        var reference = Type(module, "ArrayLayoutReferencePayload");
        Constructor(reference, "ArrayLayoutReferenceDependency");
        var provider = Method(Type(library.MainModule, "ArrayLayoutUnusedProvider"), "Read");
        provider.Body.Instructions.Add(Instruction.Create(OpCodes.Call, library.MainModule.GetType("Graph." + "ArrayLayoutReferenceDependency").Methods.Single()));
        provider.Body.Instructions.Add(Instruction.Create(OpCodes.Pop));
        provider.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        var holder = Type(module, "ArrayLayoutHolder`1", generic: true);
        var holderArgument = holder.GenericParameters.Single();
        var f = new FieldDefinition("F", FieldAttributes.Public, Closed(cell, holderArgument));
        var g = new FieldDefinition("G", FieldAttributes.Public, Closed(cell, holderArgument));
        holder.Fields.Add(f); holder.Fields.Add(g);
        var nestedField = new FieldDefinition("Nested", FieldAttributes.Public, Closed(nested, holderArgument));
        var nestedCopy = new FieldDefinition("NestedCopy", FieldAttributes.Public, Closed(nested, holderArgument));
        holder.Fields.Add(nestedField); holder.Fields.Add(nestedCopy);
        holder.Fields.Add(new FieldDefinition("Reference", FieldAttributes.Public, reference));
        var callback = new FunctionPointerType { ReturnType = Closed(cell, holderArgument) };
        callback.Parameters.Add(new ParameterDefinition(Closed(nested, holderArgument)));
        holder.Fields.Add(new FieldDefinition("Callback", FieldAttributes.Public, callback));
        var holderCtor = Method(holder, ".ctor", true);
        holderCtor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
        holderCtor.Body.Instructions.Add(Instruction.Create(OpCodes.Call, module.ImportReference(core.MainModule.GetType("System.Object")
            .Methods.Single(m => m.IsConstructor && !m.IsStatic && m.Parameters.Count == 0))));
        holderCtor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        var entry = Type(module, "ArrayLayoutEntry");
        var move = Method(entry, "Move");
        move.GenericParameters.Add(new GenericParameter("T", move));
        var moveOwner = Closed(holder, move.GenericParameters.Single());
        move.Parameters.Add(new ParameterDefinition(moveOwner));
        foreach (var (source, destination) in new[] { (f, g), (nestedField, nestedCopy) })
        {
            move.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            move.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            move.Body.Instructions.Add(Instruction.Create(OpCodes.Ldfld, new FieldReference(source.Name, source.FieldType, moveOwner)));
            move.Body.Instructions.Add(Instruction.Create(OpCodes.Stfld, new FieldReference(destination.Name, destination.FieldType, moveOwner)));
        }
        move.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        var initialize = Method(entry, "Initialize");
        initialize.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_2));
        initialize.Body.Instructions.Add(Instruction.Create(OpCodes.Newarr, Core(module, "Int32")));
        initialize.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, module.ImportReference(core.MainModule.GetType("System.Array")
            .Methods.Single(m => m.Name == "Initialize" && !m.IsStatic && m.Parameters.Count == 0))));
        initialize.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        var form = Method(entry, "Form");
        var typeType = core.MainModule.GetType("System.Type");
        form.Body.Instructions.Add(Instruction.Create(OpCodes.Ldstr, "Graph.ArrayLayoutMadeCell`1, GraphApp"));
        form.Body.Instructions.Add(Instruction.Create(OpCodes.Call, module.ImportReference(typeType.Methods.Single(m => m.Name == "GetType"
            && m.IsStatic && m.Parameters.Count == 1 && m.Parameters[0].ParameterType.FullName == "System.String"))));
        form.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_1));
        form.Body.Instructions.Add(Instruction.Create(OpCodes.Newarr, module.ImportReference(typeType)));
        form.Body.Instructions.Add(Instruction.Create(OpCodes.Dup));
        form.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_0));
        form.Body.Instructions.Add(Instruction.Create(OpCodes.Ldtoken, Core(module, "Int32")));
        form.Body.Instructions.Add(Instruction.Create(OpCodes.Call, module.ImportReference(typeType.Methods.Single(m => m.Name == "GetTypeFromHandle"))));
        form.Body.Instructions.Add(Instruction.Create(OpCodes.Stelem_Ref));
        form.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, module.ImportReference(typeType.Methods.Single(m => m.Name == "MakeGenericType"))));
        form.Body.Instructions.Add(Instruction.Create(OpCodes.Pop));
        form.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        var main = Method(entry, "Main"); app.EntryPoint = main;
        var culture = core.MainModule.GetType("System.Globalization.CultureInfo");
        foreach (string setter in new[] { "set_CurrentCulture", "set_CurrentUICulture" })
        {
            main.Body.Instructions.Add(Instruction.Create(OpCodes.Call, module.ImportReference(culture.Methods.Single(m => m.Name == "get_InvariantCulture"))));
            main.Body.Instructions.Add(Instruction.Create(OpCodes.Call, module.ImportReference(culture.Methods.Single(m => m.Name == setter))));
        }
        if (mode == "initialize-first") main.Body.Instructions.Add(Instruction.Create(OpCodes.Call, initialize));
        var closedHolder = Closed(holder, Core(module, "Int32"));
        main.Body.Instructions.Add(Instruction.Create(OpCodes.Newobj, new MethodReference(".ctor", Core(module, "Void"), closedHolder) { HasThis = true }));
        var closedMove = new GenericInstanceMethod(move); closedMove.GenericArguments.Add(Core(module, "Int32"));
        main.Body.Instructions.Add(Instruction.Create(OpCodes.Call, closedMove));
        main.Body.Instructions.Add(Instruction.Create(OpCodes.Call, form));
        if (mode == "layout-first") main.Body.Instructions.Add(Instruction.Create(OpCodes.Call, initialize));
        main.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        library.Write(Path.Combine(directory, "GraphLib.dll"), new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        app.Write(Path.Combine(directory, "GraphApp.dll"), new WriterParameters { Timestamp = 0, DeterministicMvid = true });
    }

    private static void CheckLayout(string directory, string mode, string state)
    {
        Require(mode is "initialize-first" or "layout-first" or "unarmed", "unknown array layout mode");
        Require(state is "original" or "before" or "published", "unknown array layout state");
        using var app = AssemblyDefinition.ReadAssembly(Path.Combine(directory, "GraphApp.dll"));
        using var library = AssemblyDefinition.ReadAssembly(Path.Combine(directory, "GraphLib.dll"));
        var holder = app.MainModule.GetType("Graph.ArrayLayoutHolder`1");
        Require(holder is not null && holder.Fields.Single(f => f.Name == "Callback").FieldType is FunctionPointerType pointer
            && !pointer.HasThis && !pointer.ExplicitThis && pointer.CallingConvention == MethodCallingConvention.Default
            && pointer.Parameters.Count == 1
            && pointer.ReturnType is GenericInstanceType result && result.ElementType.FullName == "Graph.ArrayLayoutCell`1"
            && pointer.Parameters[0].ParameterType is GenericInstanceType parameter
            && parameter.ElementType.FullName == "Graph.ArrayLayoutOuter`1/ArrayLayoutNested"
            && result.GenericArguments.Count == 1 && parameter.GenericArguments.Count == 1
            && result.GenericArguments[0] is GenericParameter resultArgument && resultArgument.Owner == holder
            && resultArgument.Type == GenericParameterType.Type && resultArgument.Position == 0
            && parameter.GenericArguments[0] is GenericParameter parameterArgument && parameterArgument.Owner == holder
            && parameterArgument.Type == GenericParameterType.Type && parameterArgument.Position == 0,
            "function-pointer signature lost its nested layout or declaring-owner binding");
        bool executes = state == "original" || state == "published" && mode != "unarmed";
        bool Body(MethodDefinition? method) => method is { HasBody: true } && !Stub(method);
        bool ExactStub(MethodDefinition method) => Stub(method) && method.Body.Instructions[0].Operand is int marker && marker == 0x494C4454;
        foreach (var (typeName, dependency) in new[] { ("ArrayLayoutCell`1", "ArrayLayoutCellDependency"),
            ("ArrayLayoutShell", "ArrayLayoutShellDependency"), ("ArrayLayoutMadeCell`1", "ArrayLayoutMadeDependency"),
            ("ArrayLayoutOuter`1/ArrayLayoutNested", "ArrayLayoutNestedDependency") })
        {
            var type = app.MainModule.GetType("Graph." + typeName);
            var ctor = type?.Methods.Single(m => m.IsConstructor && !m.IsStatic && m.Parameters.Count == 0);
            Require(Body(ctor) == executes, "array layout default constructor state: " + typeName + ":" + state);
            var target = library.MainModule.GetType("Graph." + dependency)?.Methods.SingleOrDefault();
            Require(Body(target) == executes, "array layout unique dependency state: " + dependency + ":" + state);
        }
        if (state == "original")
        {
            var code = app.MainModule.GetTypes().SelectMany(t => t.Methods).Where(m => m.HasBody).SelectMany(m => m.Body.Instructions).ToArray();
            Require(code.Where(i => i.OpCode == OpCodes.Newobj).All(i => i.Operand is MethodReference method
                && method.DeclaringType.FullName.StartsWith("Graph.ArrayLayoutHolder`1", StringComparison.Ordinal)), "layout fixture directly constructs a value payload");
            Require(code.All(i => i.OpCode != OpCodes.Initobj && i.OpCode != OpCodes.Box), "layout fixture gained an independent value runtime root");
            var move = app.MainModule.GetType("Graph.ArrayLayoutEntry").Methods.Single(m => m.Name == "Move");
            Require(move.Body.Instructions.Where(i => i.OpCode == OpCodes.Ldfld).Select(i => ((FieldReference)i.Operand).Name)
                    .SequenceEqual(new[] { "F", "Nested" })
                && move.Body.Instructions.Where(i => i.OpCode == OpCodes.Stfld).Select(i => ((FieldReference)i.Operand).Name)
                    .SequenceEqual(new[] { "G", "NestedCopy" }), "layout fixture lost its ordinary and nested value field copies");
            Require(code.Count(i => i.Operand is MethodReference method && method.Name == "MakeGenericType") == 1
                && code.Single(i => i.Operand is MethodReference method && method.Name == "MakeGenericType").Next.OpCode == OpCodes.Pop,
                "layout fixture lost its discarded generic formation");
            Require(app.MainModule.GetTypes().All(t => t.CustomAttributes.Count == 0), "layout fixture introduced preservation attributes");
        }
        else
        {
            var cell = app.MainModule.GetType("Graph.ArrayLayoutCell`1");
            Require(cell is not null && ExactStub(cell.Methods.Single(m => m.IsConstructor && m.Parameters.Count == 1))
                && ExactStub(cell.Methods.Single(m => m.Name == "Ordinary")), "Array.Initialize opened ordinary or overload bodies");
            var reference = app.MainModule.GetType("Graph.ArrayLayoutReferencePayload");
            Require(reference is not null && ExactStub(reference.Methods.Single(m => m.IsConstructor)), "reference field metadata opened a constructor");
            Require(app.MainModule.GetType("Graph.ArrayLayoutUnobserved`1") is null, "Array.Initialize opened an unobserved generic definition");
            foreach (string name in new[] { "ArrayLayoutOverloadDependency", "ArrayLayoutOrdinaryDependency", "ArrayLayoutReferenceDependency",
                "ArrayLayoutUnobservedDependency", "ArrayLayoutUnusedProvider" })
                Require(library.MainModule.GetType("Graph." + name) is null, "array layout opened a negative provider/body: " + name);
        }
        Console.WriteLine("graph-array-layout=" + state + ":" + mode + ":default-constructors,unique-external-dependencies,metadata-negatives");
    }
}
