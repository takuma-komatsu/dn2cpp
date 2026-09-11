using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class GodotValidation
{
    internal static bool Run(string[] args)
    {
        if (args.Length == 2 && args[0] == "--create-engine-fixtures")
        {
            Create(args[1]);
            return true;
        }
        if (args.Length == 3 && args[0] == "--check-engine-fixtures")
        {
            CheckFixture(args[1], args[2]);
            return true;
        }
        if (args.Length == 4 && args[0] == "--check-godot-sample")
        {
            CheckSample(args[1], args[2], args[3]);
            return true;
        }
        return false;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void CheckSample(string inputApp, string inputEngine, string output)
    {
        using var beforeApp = AssemblyDefinition.ReadAssembly(inputApp);
        using var afterApp = AssemblyDefinition.ReadAssembly(Path.Combine(output, "DotnetSample.dll"));
        using var beforeEngine = AssemblyDefinition.ReadAssembly(inputEngine);
        using var afterEngine = AssemblyDefinition.ReadAssembly(Path.Combine(output, "GodotSharp.dll"));
        foreach (string name in new[] { "UnusedScript", "UnusedScript/<>c", "UnusedScript/MethodName" })
        {
            Require(beforeApp.MainModule.GetType(name) is not null, "missing unused-script witness: " + name);
            Require(afterApp.MainModule.GetType(name) is null, "unused script or helper survived: " + name);
        }
        foreach (string name in new[] { "Player", "Probe", "AutoloadProbe", "SceneResource", "GenericBase`1",
            "GenericConcrete", "MyResource", "ExplicitPreservedScript", "FaultProbe" })
            Require(afterApp.MainModule.GetType(name) is not null, "used script disappeared: " + name);
        foreach (string name in new[] { "Godot.GodotObject", "Godot.RefCounted", "Godot.Node2D", "Godot.Sprite3D", "Godot.LightmapperRD" })
            Require(afterEngine.MainModule.GetType(name) is not null, "required engine wrapper disappeared: " + name);
        Require(beforeEngine.MainModule.GetType("Godot.Sprite2D") is not null, "missing unused engine witness");
        Require(afterEngine.MainModule.GetType("Godot.Sprite2D") is null, "unused engine wrapper survived ILDiet");
        var handwritten = afterApp.MainModule.GetType("HandwrittenContainer/OrdinaryNested");
        Require(handwritten is not null && handwritten.Methods.Any(m => m.Name == ".cctor")
            && handwritten.Methods.Any(m => m.Name == "ReadValue"), "ordinary handwritten nested members disappeared");
        Require(afterApp.MainModule.GetType("HandwrittenContainer") is not null,
            "live ordinary nested type lost its declaring script");
        CheckRegistration(beforeApp, afterApp, "Godot.AssemblyHasScriptsAttribute", "UnusedScript");
        CheckRegistry(beforeEngine, afterEngine, "Godot.Constructors", "Godot.Sprite2D", "Godot.Node2D");
        Console.WriteLine("godot-ildiet-metadata=unused-engine,unused-script,nested-helper,ordinary-nested,scene,autoload,resource,generic,code-created,explicit,registrations");
    }

    private static void CheckRegistration(AssemblyDefinition before, AssemblyDefinition after, string attributeName, string removed)
    {
        static IEnumerable<string> Types(CustomAttributeArgument argument)
        {
            if (argument.Value is TypeReference type) yield return type.FullName;
            if (argument.Value is CustomAttributeArgument[] array)
                foreach (var item in array)
                    foreach (string name in Types(item)) yield return name;
        }
        string[] Registered(AssemblyDefinition assembly) => assembly.CustomAttributes
            .Where(a => a.AttributeType.FullName == attributeName)
            .SelectMany(a => a.ConstructorArguments.SelectMany(Types)).ToArray();
        Require(Registered(before).Contains(removed), "registration lacks removed-type witness");
        string[] registered = Registered(after);
        Require(registered.Length != 0 && !registered.Contains(removed), "script registration array was not filtered");
        foreach (string name in registered)
            Require(after.MainModule.GetType(name) is not null, "dangling registered script: " + name);
    }

    private static void CheckRegistry(AssemblyDefinition before, AssemblyDefinition after, string registryName,
        string removed, string ancestor)
    {
        var original = before.MainModule.GetType(registryName);
        var rewritten = after.MainModule.GetType(registryName);
        Require(original is not null && rewritten is not null, "constructor registry disappeared");
        static IEnumerable<MethodDefinition> Methods(TypeDefinition type) => type.Methods
            .Concat(type.NestedTypes.SelectMany(Methods));
        static IEnumerable<string> Keys(TypeDefinition type) => Methods(type)
            .Where(m => m.Name == ".cctor" && m.HasBody).SelectMany(m => m.Body.Instructions)
            .Where(i => i.OpCode.Code == Code.Ldstr).Select(i => (string)i.Operand);
        Require(Keys(original!).Any(), "registry has no engine-name witnesses");
        Require(Keys(original!).SequenceEqual(Keys(rewritten!)), "engine registration keys changed");
        bool redirected = false;
        var outputMethods = Methods(rewritten!).ToDictionary(m => m.FullName);
        foreach (var method in Methods(original!))
        {
            if (!method.HasBody || !method.Body.Instructions.Any(i => i.OpCode.Code == Code.Newobj
                    && i.Operand is MethodReference constructor && constructor.DeclaringType.FullName == removed)) continue;
            if (!outputMethods.TryGetValue(method.FullName, out var replacement)) continue;
            redirected |= replacement.HasBody && replacement.Body.Instructions.Any(i => i.OpCode.Code == Code.Newobj
                && i.Operand is MethodReference constructor && constructor.DeclaringType.FullName == ancestor);
        }
        // Implementations may replace the ldftn operand with the ancestor's existing
        // factory, in which case the dead factory method itself is removed.
        redirected |= Methods(original!).Any(m => m.HasBody && m.Body.Instructions.Any(i => i.OpCode.Code == Code.Newobj
            && i.Operand is MethodReference constructor && constructor.DeclaringType.FullName == removed))
            && !Methods(rewritten!).Any(m => m.HasBody && m.Body.Instructions.Any(i => i.OpCode.Code == Code.Newobj
                && i.Operand is MethodReference constructor && constructor.DeclaringType.FullName == removed))
            && Methods(rewritten!).Any(m => m.HasBody && m.Body.Instructions.Any(i => i.OpCode.Code == Code.Newobj
                && i.Operand is MethodReference constructor && constructor.DeclaringType.FullName == ancestor));
        Require(redirected, "constructor factory did not route to the retained ancestor");
        foreach (var method in Methods(rewritten!))
            if (method.HasBody)
                foreach (var instruction in method.Body.Instructions)
                    if (instruction.Operand is MethodReference target && target.DeclaringType.Scope == after.MainModule)
                    {
                        var owner = after.MainModule.GetType(target.DeclaringType.FullName);
                        Require(owner is not null && owner.Methods.Any(m => m.FullName == target.FullName),
                            "dangling constructor registry reference: " + target.FullName);
                    }
    }

    private static void CheckFixture(string original, string output)
    {
        using var beforeApp = AssemblyDefinition.ReadAssembly(Path.Combine(original, "EngineApp.dll"));
        using var app = AssemblyDefinition.ReadAssembly(Path.Combine(output, "EngineApp.dll"));
        using var beforeEngine = AssemblyDefinition.ReadAssembly(Path.Combine(original, "GodotSharp.dll"));
        using var engine = AssemblyDefinition.ReadAssembly(Path.Combine(output, "GodotSharp.dll"));
        foreach (string name in new[] { "EngineFixture.UnusedScript", "EngineFixture.UnusedScript/Helper" })
            Require(app.MainModule.GetType(name) is null, "unused conditional type survived: " + name);
        foreach (string name in new[] { "EngineFixture.LiveScript", "EngineFixture.Ordinary", "EngineFixture.ScalarScript" })
        {
            var type = app.MainModule.GetType(name);
            Require(type is not null && type.Methods.Any(m => m.Name == ".cctor"), "live initializer disappeared: " + name);
            Require(type!.Methods.Any(m => m.Name == "Callback") && type.Methods.Any(m => m.Name == "CallbackLeaf"),
                "engine callback body or closure disappeared: " + name);
        }
        Require(engine.MainModule.GetType("Engine.Unused") is null, "unused engine type survived");
        Require(engine.MainModule.GetType("Engine.UnknownFactoryType") is not null, "unknown factory was not conservatively retained");
        var live = app.MainModule.GetType("EngineFixture.LiveScript");
        Require(live.Properties.Any(p => p.Name == "Value" && p.GetMethod is not null && p.SetMethod is not null), "live property disappeared");
        Require(live.Events.Any(e => e.Name == "Changed" && e.AddMethod is not null && e.RemoveMethod is not null), "live event disappeared");
        var nested = app.MainModule.GetType("EngineFixture.HandwrittenContainer/OrdinaryNested");
        Require(nested is not null && nested.Methods.Any(m => m.Name == ".cctor")
            && nested.Methods.Any(m => m.Name == "PublicMethod") && nested.Methods.Any(m => m.Name == "PrivateLeaf"),
            "ordinary handwritten nested members disappeared");
        Require(app.MainModule.GetType("EngineFixture.HandwrittenContainer") is not null,
            "live ordinary nested type lost its declaring script");
        CheckRegistration(beforeApp, app, "Engine.AssemblyHasScriptsAttribute", "EngineFixture.UnusedScript");
        Require(app.CustomAttributes.Any(a => a.AttributeType.FullName == "Engine.AssemblyHasScriptsAttribute"
            && a.ConstructorArguments.Single().Value is null), "null registration array changed");
        CheckRegistry(beforeEngine, engine, "Engine.Constructors", "Engine.Unused", "Engine.Live");
        Console.WriteLine("engine-policy-metadata=conditional-members,ordinary-members,ordinary-nested-members,attribute-array,null-array,scalar-type,ancestor-factory,unknown-factory");
    }

    private static void Create(string directory)
    {
        Directory.CreateDirectory(directory);
        using var engine = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition("GodotSharp", new Version(1, 0)), "GodotSharp", ModuleKind.Dll);
        var module = engine.MainModule;
        TypeDefinition Type(string ns, string name, TypeReference baseType)
        {
            var type = new TypeDefinition(ns, name, TypeAttributes.Public | TypeAttributes.Class, baseType);
            module.Types.Add(type);
            AddMethod(type, ".ctor", module.TypeSystem.Void, MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);
            return type;
        }
        var root = Type("Engine", "Object", module.TypeSystem.Object);
        var live = Type("Engine", "Live", root);
        var unused = Type("Engine", "Unused", live);
        var unknown = Type("Engine", "UnknownFactoryType", root);
        var registry = Type("Engine", "Constructors", module.TypeSystem.Object);
        var register = AddMethod(registry, "Register", module.TypeSystem.Void, MethodAttributes.Public | MethodAttributes.Static);
        register.Parameters.Add(new ParameterDefinition(module.TypeSystem.String));
        register.Parameters.Add(new ParameterDefinition(module.TypeSystem.IntPtr));
        var cctor = AddMethod(registry, ".cctor", module.TypeSystem.Void,
            MethodAttributes.Private | MethodAttributes.Static | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);
        cctor.Body.Instructions.Clear();
        foreach (var type in new[] { root, live, unused, unknown })
        {
            var factory = AddMethod(registry, "Factory" + type.Name, root, MethodAttributes.Private | MethodAttributes.Static);
            factory.Body.Instructions.Clear();
            factory.Body.Instructions.Add(Instruction.Create(OpCodes.Newobj, type.Methods.Single(m => m.Name == ".ctor")));
            if (type == unknown)
            {
                factory.Body.Instructions.Add(Instruction.Create(OpCodes.Dup));
                factory.Body.Instructions.Add(Instruction.Create(OpCodes.Pop));
            }
            factory.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            cctor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldstr, type.Name));
            cctor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldftn, factory));
            cctor.Body.Instructions.Add(Instruction.Create(OpCodes.Call, register));
        }
        cctor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        var attribute = Type("Engine", "AssemblyHasScriptsAttribute", module.ImportReference(typeof(Attribute)));
        var attributeCtor = attribute.Methods.Single(m => m.Name == ".ctor");
        attributeCtor.Parameters.Add(new ParameterDefinition(new ArrayType(module.ImportReference(typeof(System.Type)))));
        var scalarAttributeCtor = AddMethod(attribute, ".ctor", module.TypeSystem.Void,
            MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);
        scalarAttributeCtor.Parameters.Add(new ParameterDefinition(module.ImportReference(typeof(System.Type))));
        engine.Write(Path.Combine(directory, "GodotSharp.dll"), new WriterParameters { Timestamp = 0, DeterministicMvid = true });

        using var app = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition("EngineApp", new Version(1, 0)), "EngineApp", ModuleKind.Dll);
        TypeDefinition Script(string name, TypeReference baseType)
        {
            var type = new TypeDefinition("EngineFixture", name, TypeAttributes.Public | TypeAttributes.Class, baseType);
            app.MainModule.Types.Add(type);
            AddMethod(type, ".ctor", app.MainModule.TypeSystem.Void, MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);
            AddMethod(type, ".cctor", app.MainModule.TypeSystem.Void, MethodAttributes.Private | MethodAttributes.Static | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);
            var callback = AddMethod(type, "Callback", app.MainModule.TypeSystem.Void, MethodAttributes.Public);
            var leaf = AddMethod(type, "CallbackLeaf", app.MainModule.TypeSystem.Void, MethodAttributes.Private | MethodAttributes.Static);
            callback.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Call, leaf));
            return type;
        }
        var liveScript = Script("LiveScript", app.MainModule.ImportReference(live));
        var unusedScript = Script("UnusedScript", app.MainModule.ImportReference(live));
        var scalarScript = Script("ScalarScript", app.MainModule.ImportReference(live));
        var helper = new TypeDefinition("", "Helper", TypeAttributes.NestedPublic | TypeAttributes.Class, app.MainModule.TypeSystem.Object);
        helper.CustomAttributes.Add(new CustomAttribute(app.MainModule.ImportReference(
            typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute).GetConstructor(System.Type.EmptyTypes)!)));
        unusedScript.NestedTypes.Add(helper);
        AddMethod(helper, ".cctor", app.MainModule.TypeSystem.Void, MethodAttributes.Private | MethodAttributes.Static | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);
        AddMethod(helper, "Unused", app.MainModule.TypeSystem.Void, MethodAttributes.Public);
        var handwrittenContainer = Script("HandwrittenContainer", app.MainModule.ImportReference(live));
        var ordinaryNested = new TypeDefinition("", "OrdinaryNested", TypeAttributes.NestedPublic | TypeAttributes.Class,
            app.MainModule.TypeSystem.Object);
        handwrittenContainer.NestedTypes.Add(ordinaryNested);
        AddMethod(ordinaryNested, ".cctor", app.MainModule.TypeSystem.Void,
            MethodAttributes.Private | MethodAttributes.Static | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);
        var nestedPublic = AddMethod(ordinaryNested, "PublicMethod", app.MainModule.TypeSystem.Void, MethodAttributes.Public);
        var nestedLeaf = AddMethod(ordinaryNested, "PrivateLeaf", app.MainModule.TypeSystem.Void, MethodAttributes.Private | MethodAttributes.Static);
        nestedPublic.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Call, nestedLeaf));
        Script("Ordinary", app.MainModule.TypeSystem.Object);
        var getter = AddMethod(liveScript, "get_Value", app.MainModule.TypeSystem.Int32, MethodAttributes.Public | MethodAttributes.SpecialName);
        getter.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Ldc_I4, 7));
        var setter = AddMethod(liveScript, "set_Value", app.MainModule.TypeSystem.Void, MethodAttributes.Public | MethodAttributes.SpecialName);
        setter.Parameters.Add(new ParameterDefinition(app.MainModule.TypeSystem.Int32));
        liveScript.Properties.Add(new PropertyDefinition("Value", PropertyAttributes.None, app.MainModule.TypeSystem.Int32) { GetMethod = getter, SetMethod = setter });
        var action = app.MainModule.ImportReference(typeof(Action));
        var add = AddMethod(liveScript, "add_Changed", app.MainModule.TypeSystem.Void, MethodAttributes.Public | MethodAttributes.SpecialName);
        var remove = AddMethod(liveScript, "remove_Changed", app.MainModule.TypeSystem.Void, MethodAttributes.Public | MethodAttributes.SpecialName);
        add.Parameters.Add(new ParameterDefinition(action));
        remove.Parameters.Add(new ParameterDefinition(action));
        liveScript.Events.Add(new EventDefinition("Changed", EventAttributes.None, action) { AddMethod = add, RemoveMethod = remove });
        var scriptAttribute = new CustomAttribute(app.MainModule.ImportReference(attributeCtor));
        var systemType = app.MainModule.ImportReference(typeof(System.Type));
        scriptAttribute.ConstructorArguments.Add(new CustomAttributeArgument(new ArrayType(systemType),
            new[] { new CustomAttributeArgument(systemType, liveScript), new CustomAttributeArgument(systemType, unusedScript) }));
        app.CustomAttributes.Add(scriptAttribute);
        var nullAttribute = new CustomAttribute(app.MainModule.ImportReference(attributeCtor));
        nullAttribute.ConstructorArguments.Add(new CustomAttributeArgument(new ArrayType(systemType), null));
        app.CustomAttributes.Add(nullAttribute);
        var scalarAttribute = new CustomAttribute(app.MainModule.ImportReference(scalarAttributeCtor));
        scalarAttribute.ConstructorArguments.Add(new CustomAttributeArgument(systemType, scalarScript));
        app.CustomAttributes.Add(scalarAttribute);
        app.Write(Path.Combine(directory, "EngineApp.dll"), new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        Console.WriteLine("fixture-created=engine-policy");
    }

    private static MethodDefinition AddMethod(TypeDefinition type, string name, TypeReference result, MethodAttributes attributes)
    {
        var method = new MethodDefinition(name, attributes, result);
        type.Methods.Add(method);
        method.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        return method;
    }
}
