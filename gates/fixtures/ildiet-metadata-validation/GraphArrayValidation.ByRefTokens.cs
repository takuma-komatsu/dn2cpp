using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Text.Json;
using System.Xml.Linq;

static partial class GraphArrayValidation
{
    private static readonly string[] ByRefTokenModes =
    {
        "byref-token-core", "byref-token-copied", "byref-token-rewritten",
        "byref-token-direct", "byref-token-constrained", "byref-token-interface",
        "byref-token-boxable", "byref-token-preserved",
    };

    private static bool ByRefTokenMode(string mode) => ByRefTokenModes.Contains(mode);
    private static bool ByRefTokenArmed(string mode) => mode is "byref-token-direct" or "byref-token-constrained"
        or "byref-token-interface" or "byref-token-boxable" or "byref-token-preserved";

    private static void CreateByRefTokens(string directory, string corelib, string mode)
    {
        Require(ByRefTokenMode(mode), "unknown named byref token mode");
        Directory.CreateDirectory(directory);
        using var core = AssemblyDefinition.ReadAssembly(corelib);
        using var app = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition("GraphApp", new Version(1, 0)), "GraphApp", ModuleKind.Console);
        using var lib = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition("GraphLib", new Version(1, 0)), "GraphLib", ModuleKind.Dll);
        using var shim = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition("DnZlib", new Version(1, 0)), "DnZlib", ModuleKind.Dll);
        using var facade = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition("System.ArrayFacade", new Version(1, 0)), "System.ArrayFacade", ModuleKind.Dll);
        using var leaf = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition("System.ArrayLeaf", new Version(1, 0)), "System.ArrayLeaf", ModuleKind.Dll);
        TypeReference Core(ModuleDefinition module, string name) => module.ImportReference(core.MainModule.GetType("System." + name));
        MethodReference Api(ModuleDefinition module, string owner, string name, params string[] parameters) => module.ImportReference(
            core.MainModule.GetType("System." + owner).Methods.Single(m => m.Name == name && !m.HasGenericParameters
                && m.Parameters.Select(p => p.ParameterType.FullName).SequenceEqual(parameters)));
        TypeDefinition Type(ModuleDefinition module, string name, bool value = false)
        {
            var type = new TypeDefinition("Graph", name, TypeAttributes.Public | TypeAttributes.BeforeFieldInit
                | (value ? TypeAttributes.Sealed | TypeAttributes.SequentialLayout : TypeAttributes.Class), Core(module, value ? "ValueType" : "Object"));
            module.Types.Add(type);
            return type;
        }
        MethodDefinition Method(TypeDefinition type, string name, bool instance = false, string result = "Void")
        {
            var method = new MethodDefinition(name, MethodAttributes.Public | MethodAttributes.HideBySig
                | (instance ? 0 : MethodAttributes.Static), Core(type.Module, result));
            if (name == ".ctor") method.Attributes |= MethodAttributes.SpecialName | MethodAttributes.RTSpecialName;
            type.Methods.Add(method);
            return method;
        }
        MethodDefinition Dependency(string name)
        {
            var method = Method(Type(lib.MainModule, name), "Touch");
            method.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            return method;
        }
        var value = Type(app.MainModule, "ByRefTokenValue", true);
        var field = new FieldDefinition("Number", FieldAttributes.Public, Core(app.MainModule, "Int32"));
        value.Fields.Add(field);
        foreach (bool overload in new[] { false, true })
        {
            var ctor = Method(value, ".ctor", true);
            if (overload) ctor.Parameters.Add(new ParameterDefinition(Core(app.MainModule, "Int32")));
            ctor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            ctor.Body.Instructions.Add(overload ? Instruction.Create(OpCodes.Ldarg_1) : Instruction.Create(OpCodes.Ldc_I4, 7));
            ctor.Body.Instructions.Add(Instruction.Create(OpCodes.Stfld, field));
            ctor.Body.Instructions.Add(Instruction.Create(OpCodes.Call, app.MainModule.ImportReference(
                Dependency(overload ? "ByRefTokenOverloadDependency" : "ByRefTokenDependency"))));
            ctor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        }
        var ordinary = Method(value, "Ordinary");
        ordinary.Body.Instructions.Add(Instruction.Create(OpCodes.Call, app.MainModule.ImportReference(Dependency("ByRefTokenOrdinaryDependency"))));
        ordinary.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));

        var initialize = Method(Type(leaf.MainModule, "ByRefTokenLeaf"), "Initialize");
        initialize.Parameters.Add(new ParameterDefinition(Core(leaf.MainModule, "Array")));
        initialize.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
        initialize.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, Api(leaf.MainModule, "Array", "Initialize")));
        initialize.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        var bridge = Method(Type(facade.MainModule, "ByRefTokenBridge"), "Arm");
        bridge.Parameters.Add(new ParameterDefinition(Core(facade.MainModule, "Array")));
        bridge.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
        bridge.Body.Instructions.Add(Instruction.Create(OpCodes.Call, facade.MainModule.ImportReference(initialize)));
        bridge.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));

        var ownerModule = mode == "byref-token-rewritten" ? lib.MainModule : shim.MainModule;
        var payload = Type(ownerModule, "ByRefTokenPayload", true);
        payload.Fields.Add(new FieldDefinition("Number", FieldAttributes.Public, Core(ownerModule, "Int32")));
        if (mode != "byref-token-boxable")
            payload.CustomAttributes.Add(new CustomAttribute(Api(ownerModule, "Runtime.CompilerServices.IsByRefLikeAttribute", ".ctor")));
        void Arm(MethodDefinition method, bool text)
        {
            method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_1));
            method.Body.Instructions.Add(Instruction.Create(OpCodes.Newarr, Core(method.Module, "Int32")));
            method.Body.Instructions.Add(Instruction.Create(OpCodes.Call, method.Module.ImportReference(bridge)));
            if (text) method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldstr, "named-byref"));
            method.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        }
        var stringify = Method(payload, "ToString", true, "String");
        stringify.Attributes |= MethodAttributes.Virtual;
        Arm(stringify, true);
        var contract = new TypeDefinition("Graph", "IByRefTokenSlot", TypeAttributes.Public | TypeAttributes.Interface | TypeAttributes.Abstract);
        ownerModule.Types.Add(contract);
        var slot = Method(contract, "Render", true, "String");
        slot.Attributes |= MethodAttributes.Abstract | MethodAttributes.Virtual | MethodAttributes.NewSlot;
        payload.Interfaces.Add(new InterfaceImplementation(contract));
        var explicitBody = Method(payload, "ExplicitRender", true, "String");
        explicitBody.Attributes = MethodAttributes.Private | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.NewSlot | MethodAttributes.HideBySig;
        explicitBody.Overrides.Add(slot);
        Arm(explicitBody, true);
        var activation = Method(payload, "Activate");
        Arm(activation, false);
        var unused = Method(payload, "Unused");
        unused.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));

        var main = Method(Type(app.MainModule, "ByRefTokenEntry"), "Main");
        app.EntryPoint = main;
        foreach (string setter in new[] { "set_CurrentCulture", "set_CurrentUICulture" })
        {
            main.Body.Instructions.Add(Instruction.Create(OpCodes.Call, Api(app.MainModule, "Globalization.CultureInfo", "get_InvariantCulture")));
            main.Body.Instructions.Add(Instruction.Create(OpCodes.Call, Api(app.MainModule, "Globalization.CultureInfo", setter, "System.Globalization.CultureInfo")));
        }
        main.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_1));
        main.Body.Instructions.Add(Instruction.Create(OpCodes.Newarr, value));
        main.Body.Instructions.Add(Instruction.Create(OpCodes.Pop));
        if (mode is "byref-token-core" or "byref-token-copied" or "byref-token-rewritten" or "byref-token-preserved")
        {
            TypeReference[] tokens = mode == "byref-token-core"
                ? new[] { Core(app.MainModule, "TypedReference"), Core(app.MainModule, "Void") }
                : new[] { app.MainModule.ImportReference(payload) };
            foreach (var token in tokens)
            {
                main.Body.Instructions.Add(Instruction.Create(OpCodes.Ldtoken, token));
                main.Body.Instructions.Add(Instruction.Create(OpCodes.Call, Api(app.MainModule, "Type", "GetTypeFromHandle", "System.RuntimeTypeHandle")));
                main.Body.Instructions.Add(Instruction.Create(OpCodes.Pop));
            }
        }
        else
        {
            main.Body.InitLocals = true;
            var local = new VariableDefinition(app.MainModule.ImportReference(payload));
            main.Body.Variables.Add(local);
            main.Body.Instructions.Add(Instruction.Create(OpCodes.Ldloca, local));
            main.Body.Instructions.Add(Instruction.Create(OpCodes.Initobj, local.VariableType));
            if (mode == "byref-token-boxable")
            {
                main.Body.Instructions.Add(Instruction.Create(OpCodes.Ldloc, local));
                main.Body.Instructions.Add(Instruction.Create(OpCodes.Box, local.VariableType));
                main.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, Api(app.MainModule, "Object", "ToString")));
            }
            else
            {
                main.Body.Instructions.Add(Instruction.Create(OpCodes.Ldloca, local));
                if (mode == "byref-token-direct")
                    main.Body.Instructions.Add(Instruction.Create(OpCodes.Call, app.MainModule.ImportReference(stringify)));
                else
                {
                    main.Body.Instructions.Add(Instruction.Create(OpCodes.Constrained, local.VariableType));
                    main.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, mode == "byref-token-interface"
                        ? app.MainModule.ImportReference(slot) : Api(app.MainModule, "Object", "ToString")));
                }
            }
            main.Body.Instructions.Add(Instruction.Create(OpCodes.Pop));
        }
        main.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        var linker = new XElement("linker");
        if (mode == "byref-token-preserved")
            linker.Add(new XElement("assembly", new XAttribute("fullname", "DnZlib"), new XAttribute("preserve", "nothing"),
                new XElement("type", new XAttribute("fullname", "Graph.ByRefTokenPayload"), new XAttribute("preserve", "nothing"),
                    new XAttribute("required", "0"), new XElement("method", new XAttribute("name", "Activate")))));
        File.WriteAllText(Path.Combine(directory, "link.xml"), linker + "\n");
        foreach (var assembly in new[] { app, lib, facade, leaf, shim })
            assembly.Write(Path.Combine(directory, assembly.Name.Name + ".dll"), new WriterParameters { Timestamp = 0, DeterministicMvid = true });
    }

    private static void CheckByRefTokens(string directory, string mode, string state)
    {
        Require(ByRefTokenMode(mode) && (state is "original" or "published" or "observed-before"), "unknown named byref token check");
        bool original = state == "original", observed = state == "observed-before";
        using var app = AssemblyDefinition.ReadAssembly(Path.Combine(directory, "GraphApp.dll"));
        using var lib = AssemblyDefinition.ReadAssembly(Path.Combine(directory, "GraphLib.dll"));
        using var shim = AssemblyDefinition.ReadAssembly(Path.Combine(directory, "DnZlib.dll"));
        foreach (string name in new[] { "DnZlib", "System.ArrayFacade", "System.ArrayLeaf" })
        {
            using var assembly = AssemblyDefinition.ReadAssembly(Path.Combine(directory, name + ".dll"));
            Require(assembly.MainModule.AssemblyReferences.All(r => r.Name is "System.Private.CoreLib" or "System.ArrayFacade" or "System.ArrayLeaf"),
                "named byref copied payload gained a stripped reference");
        }
        var owner = mode == "byref-token-rewritten" ? lib.MainModule : shim.MainModule;
        var payload = owner.GetType("Graph.ByRefTokenPayload");
        Require(payload is not null && payload.IsValueType && !payload.HasGenericParameters, "named byref payload identity changed");
        Require(payload!.CustomAttributes.Count(a => a.AttributeType.FullName == "System.Runtime.CompilerServices.IsByRefLikeAttribute")
            == (mode == "byref-token-boxable" ? 0 : 1), "named byref marker changed");
        var stringify = payload.Methods.Single(m => m.Name == "ToString");
        Require(stringify.IsVirtual && !stringify.IsStatic && stringify.Parameters.Count == 0 && stringify.ReturnType.FullName == "System.String",
            "named byref override shape changed");
        var explicitBody = payload.Methods.Single(m => m.Name == "ExplicitRender");
        Require(explicitBody.IsVirtual && explicitBody.IsPrivate && explicitBody.Overrides.Count == 1
            && explicitBody.Overrides[0].Name == "Render" && explicitBody.Overrides[0].DeclaringType.FullName == "Graph.IByRefTokenSlot",
            "named byref explicit interface mapping changed");
        bool rewrittenBody = stringify.HasBody && !Stub(stringify);
        if (mode == "byref-token-rewritten" && !observed)
            Require(rewrittenBody == original && (explicitBody.HasBody && !Stub(explicitBody)) == original,
                "named byref token executed a rewritten override");
        var main = app.EntryPoint;
        Require(main is not null && main.HasBody && !Stub(main), "named byref entry disappeared");
        var code = main!.Body.Instructions.ToArray();
        Require(code.Take(4).Select(i => (i.Operand as MethodReference)?.Name).SequenceEqual(
            new[] { "get_InvariantCulture", "set_CurrentCulture", "get_InvariantCulture", "set_CurrentUICulture" }), "named byref invariant culture prefix changed");
        var tokens = code.Where(i => i.OpCode == OpCodes.Ldtoken).Select(i => i.Operand as TypeReference).ToArray();
        bool tokenOnly = mode is "byref-token-core" or "byref-token-copied" or "byref-token-rewritten" or "byref-token-preserved";
        Require(tokens.Length == (mode == "byref-token-core" ? 2 : tokenOnly ? 1 : 0), "named byref token count changed");
        if (tokenOnly)
        {
            var expectedCode = new List<Code> { Code.Call, Code.Call, Code.Call, Code.Call, Code.Ldc_I4_1, Code.Newarr, Code.Pop };
            for (int i = 0; i < tokens.Length; i++) expectedCode.AddRange(new[] { Code.Ldtoken, Code.Call, Code.Pop });
            expectedCode.Add(Code.Ret);
            Require(code.Select(i => i.OpCode.Code).SequenceEqual(expectedCode), "named byref token-only body changed");
            Require(main.Body.Variables.Count == 0 && code.All(i => i.OpCode != OpCodes.Initobj && i.OpCode != OpCodes.Box && i.OpCode != OpCodes.Constrained),
                "named byref token gained an independent runtime operand");
            Require(tokens.All(t => t is not null && t is not TypeSpecification && t is not GenericParameter && !t.HasGenericParameters),
                "named byref token is no longer a plain definition reference");
            Require(tokens.Select(t => t!.FullName).SequenceEqual(mode == "byref-token-core"
                ? new[] { "System.TypedReference", "System.Void" } : new[] { "Graph.ByRefTokenPayload" }), "named byref token identity changed");
            Require(tokens.All(t => t!.Scope.Name == (mode == "byref-token-core" ? "System.Private.CoreLib"
                : mode == "byref-token-rewritten" ? "GraphLib" : "DnZlib")), "named byref token scope changed");
        }
        else
        {
            Require(main.Body.Variables.Count == 1 && main.Body.Variables[0].VariableType.FullName == "Graph.ByRefTokenPayload", "named byref receiver changed");
            Require(code.Count(i => i.OpCode == OpCodes.Constrained) == (mode is "byref-token-constrained" or "byref-token-interface" ? 1 : 0),
                "named byref constrained prefix changed");
            Require(code.Count(i => i.OpCode == OpCodes.Box) == (mode == "byref-token-boxable" ? 1 : 0), "named byref boxing control changed");
            var call = code.Single(i => i.Operand is MethodReference m && (m.Name is "ToString" or "Render"));
            var target = (MethodReference)call.Operand;
            Require(call.OpCode == (mode == "byref-token-direct" ? OpCodes.Call : OpCodes.Callvirt)
                && target.DeclaringType.FullName == (mode == "byref-token-direct" ? "Graph.ByRefTokenPayload"
                    : mode == "byref-token-interface" ? "Graph.IByRefTokenSlot" : "System.Object"), "named byref direct/slot target changed");
        }
        bool Real(TypeDefinition? type, string name, int parameters = 0) => type?.Methods.Any(m => m.Name == name
            && m.Parameters.Count == parameters && m.HasBody && !Stub(m)) == true;
        var value = app.MainModule.GetType("Graph.ByRefTokenValue");
        bool array = Real(value, ".ctor"), dependency = Real(lib.MainModule.GetType("Graph.ByRefTokenDependency"), "Touch");
        Require(array == dependency, "named byref selected constructor/dependency disagree");
        if (!observed) Require(array == (original || ByRefTokenArmed(mode)), "named byref selected constructor admission changed");
        foreach (string name in new[] { "ByRefTokenOverloadDependency", "ByRefTokenOrdinaryDependency" })
            Require(Real(lib.MainModule.GetType("Graph." + name), "Touch") == original, "named byref unrelated dependency survived");
        Require(Real(value, ".ctor", 1) == original && Real(value, "Ordinary") == original, "named byref ordinary/overload body survived");
        Console.WriteLine("byref-token-evidence=" + JsonSerializer.Serialize(new
        {
            array, dependency, rewrittenBody,
            tokens = tokens.Select(t => t!.FullName + ", " + t.Scope.Name).ToArray(),
            code = code.Select(i => i.OpCode.Code.ToString()).ToArray(),
        }));
        Console.WriteLine("graph-copied-array=" + state + ":" + mode + ":named-byref-token,direct-and-constrained-controls,unique-array-dependency");
    }
}
