using System.Reflection.PortableExecutable;
using System.Text.Json;
using Mono.Cecil;
using Mono.Cecil.Cil;

static partial class GraphArrayValidation
{
    private static void CheckCutPassive(string originalDirectory, string directory, string mode)
    {
        Require(PassiveEvidenceMode(mode), "unknown cut passive evidence mode");
        bool original = false;
        using var app = AssemblyDefinition.ReadAssembly(Path.Combine(directory, "GraphApp.dll"));
        using var lib = AssemblyDefinition.ReadAssembly(Path.Combine(directory, "GraphLib.dll"));
        using var second = AssemblyDefinition.ReadAssembly(Path.Combine(directory, "GraphTaskB.dll"));
        bool Body(MethodDefinition? method) => method is { HasBody: true } && !Stub(method);
        bool At(string name, string member) => Body(lib.MainModule.GetType("Graph." + name)?.Methods.SingleOrDefault(m => m.Name == member));
        var bodies = new Dictionary<string, bool>
        {
            ["conditional"] = At("EagerConditional", "Activate"), ["array"] = At("EagerCell`1", ".ctor"),
            ["array_b"] = Body(second.MainModule.GetType("Graph.EagerBCell`1")?.Methods.SingleOrDefault(m => m.Name == ".ctor")),
        };
        Require(bodies.Values.All(x => x), "passive evidence lost possible option-arm consequences");
        foreach (var pair in new[] { ("conditional", "EagerConditionalDependency"), ("array", "EagerArrayDependency"), ("array_b", "EagerBArrayDependency") })
            Require(bodies[pair.Item1] == At(pair.Item2, "Touch"), "passive consequence dependency changed");
        string[] negativeNames = { "EagerLazyFieldDependency", "EagerLazyMethodDependency", "EagerLazyConditionalDependency",
            "EagerAnchorCtorDependency", "EagerAnchorCctorDependency", "EagerAnchorOrdinaryDependency" };
        var negatives = negativeNames.ToDictionary(n => n, n => At(n, "Touch"));
        Require(negatives.Values.All(v => v == original), "passive evidence opened unrelated body");
        var anchor = lib.MainModule.GetType("Graph.EagerAnchor");
        Require(anchor is not null && !anchor.HasGenericParameters && anchor.Interfaces.Count != 0, "passive formation anchor disappeared");
        if (!original) Require(anchor!.Methods.All(m => !Body(m)), "passive anchor gained executable body");
        Require(At("EagerDataAttribute", ".ctor"), "independently reached attribute ctor disappeared");
        var provider = lib.MainModule.GetType("System.Threading.Tasks.Task`1");
        var attribute = provider?.CustomAttributes.Single(a => a.AttributeType.FullName == "Graph.EagerDataAttribute");
        Require(attribute?.ConstructorArguments.Single().Value is TypeReference target && target.FullName == "Graph.EagerConditional"
            && target.Scope.Name == "GraphLib", "implicit backing conditional attribute changed");
        var tasks = new[] { lib.MainModule.GetType("Graph.EagerTask`1"), second.MainModule.GetType("Graph.EagerDeclinedTask`1") };
        var contracts = tasks.Select(t =>
        {
            var row = t.CustomAttributes.Single(a => a.AttributeType.FullName == "System.Runtime.CompilerServices.AsyncMethodBuilderAttribute");
            var builder = (TypeReference)row.ConstructorArguments.Single().Value;
            Require(builder.FullName == "Graph.EagerBuilder`1" && builder.Scope.Name == "GraphLib", "shared builder identity changed");
            return new { owner = t.FullName, builder = builder.FullName + "@" + builder.Scope.Name,
                relations = t.Interfaces.Select(i => i.InterfaceType.FullName).ToArray() };
        }).ToArray();
        var builderType = lib.MainModule.GetType("Graph.EagerBuilder`1");
        Require(builderType.Methods.Select(m => m.Name).OrderBy(n => n, StringComparer.Ordinal).SequenceEqual(
            new[] { "Create", "SetException", "SetResult", "SetStateMachine", "Start", "get_Task" }), "shared builder predicate rows changed");
        var getter = builderType.Methods.Single(m => m.Name == "get_Task");
        Require(getter.HasThis && getter.Parameters.Count == 0 && getter.ReturnType is GenericInstanceType result
            && result.ElementType.FullName == "Graph.EagerDeclinedTask`1" && result.ElementType.Scope.Name == "GraphTaskB"
            && result.GenericArguments.Single() is GenericParameter { Position: 0, Type: GenericParameterType.Type }, "shared builder return predicate changed");
        var owner = app.MainModule.GetType("Graph.EagerDeadCross");
        var carrier = owner.Methods.Single(m => m.Name == "Decline");
        Require(Body(carrier) && carrier.IsPrivate && carrier.IsStatic && !carrier.IsVirtual && !carrier.HasGenericParameters
            && !carrier.HasOverrides && carrier.Body.InitLocals && carrier.Body.ExceptionHandlers.Count == 0, "physical evidence carrier changed");
        var codes = carrier.Body.Instructions.Select(i => i.OpCode.Code).ToArray();
        Require(codes.SequenceEqual(mode.EndsWith("true-carrier", StringComparison.Ordinal)
            ? new[] { Code.Ldarg_0, Code.Refanytype, Code.Call, Code.Pop, Code.Ldloca, Code.Call, Code.Pop, Code.Ret }
            : new[] { Code.Ldloca, Code.Call, Code.Pop, Code.Ret }), "physical evidence IL changed");
        Require(carrier.Body.Variables.Single().VariableType.FullName == "Graph.EagerBuilder`1<System.Int64>", "evidence local changed");
        var witness = (MethodReference)carrier.Body.Instructions.Single(i => i.Operand is MethodReference m && m.Name == "get_Task").Operand;
        Require(witness.MetadataToken.TokenType == TokenType.MemberRef && witness.DeclaringType.FullName == "Graph.EagerBuilder`1<System.Int64>"
            && witness.DeclaringType.Scope.Name == "GraphLib" && witness.ReturnType.FullName == "Graph.EagerDeclinedTask`1<!0>", "actual evidence MemberRef changed");
        var falseCarrier = owner.Methods.SingleOrDefault(m => m.Name == "PrefilterOnly");
        Require((falseCarrier is not null) == mode.EndsWith("false-prefilter", StringComparison.Ordinal),
            "cut target MethodDef retention changed");
        var cutPhysical = CheckCutPhysical(originalDirectory, app, mode);
        Require(!app.MainModule.GetTypes().SelectMany(t => t.Methods).Where(m => m.HasBody)
            .SelectMany(m => m.Body.Instructions).Any(i => i.Operand is MethodReference m && m.DeclaringType.FullName == owner.FullName),
            "passive carrier gained a caller");
        Require(app.EntryPoint.Body.Instructions.Take(4).Select(i => (i.Operand as MethodReference)?.Name).SequenceEqual(
            new[] { "get_InvariantCulture", "set_CurrentCulture", "get_InvariantCulture", "set_CurrentUICulture" }), "passive evidence cultures changed");
        using var stream = File.OpenRead(Path.Combine(directory, "GraphApp.dll"));
        using var pe = new PEReader(stream);
        byte[] il = System.Reflection.Metadata.PEReaderExtensions.GetMethodBody(pe, carrier.RVA).GetILBytes()!;
        foreach (var instruction in carrier.Body.Instructions)
            if (instruction.Operand is MemberReference)
                Array.Clear(il, instruction.Offset + instruction.OpCode.Size, 4);
        string Operand(object? value) => value switch
        {
            MemberReference member => member.FullName + "@" + member.DeclaringType.Scope.Name,
            VariableDefinition local => "local:" + local.Index + ":" + local.VariableType.FullName,
            ParameterDefinition parameter => "parameter:" + parameter.Index + ":" + parameter.ParameterType.FullName,
            null => "", _ => Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture)!,
        };
        var physical = new { signature = carrier.FullName, attributes = (int)carrier.Attributes, implAttributes = (int)carrier.ImplAttributes,
            initLocals = carrier.Body.InitLocals, maxStack = carrier.Body.MaxStackSize,
            locals = carrier.Body.Variables.Select(v => v.VariableType.FullName).ToArray(), maskedIl = Convert.ToHexString(il),
            instructions = carrier.Body.Instructions.Select(i => new { op = i.OpCode.Code.ToString(), operand = Operand(i.Operand) }).ToArray() };
        var order = new[] { lib, second }.SelectMany(a => a.MainModule.GetTypes())
            .Where(t => t.Name is "EagerTask`1" or "EagerDeclinedTask`1" or "EagerBuilder`1")
            .Select(t => new { assembly = t.Module.Assembly.Name.Name, name = t.FullName, methods = t.Methods.Where(m => m.Name is "Create" or "SetException" or "SetResult" or "SetStateMachine" or "Start" or "get_Task" or "GetAwaiter").Select(m => m.Name).ToArray() }).ToArray();
        Console.WriteLine("cut-authority-observed=" + JsonSerializer.Serialize(new { bodies, negatives, physical, contracts, order, cutPhysical,
            falseCarrierBody = Body(falseCarrier), falseCarrierPresent = falseCarrier is not null, carriersCalled = false }));
        Console.WriteLine("graph-cut-authority=published:" + mode + ":physical-body,token-relocation,ordinary-authority-negative");
    }

    private static object CutPhysical(MethodDefinition method)
    {
        Require(method.HasBody && !Stub(method), "cut physical body became a stub: " + method.FullName);
        Require(method.Body.ExceptionHandlers.Count == 0, "cut fixture exception regions changed");
        using var stream = File.OpenRead(method.Module.FileName);
        using var pe = new PEReader(stream);
        byte[] il = System.Reflection.Metadata.PEReaderExtensions.GetMethodBody(pe, method.RVA).GetILBytes()!;
        foreach (var instruction in method.Body.Instructions)
            if (instruction.Operand is MemberReference)
                Array.Clear(il, instruction.Offset + instruction.OpCode.Size, 4);
        string Operand(object? operand) => operand switch
        {
            TypeReference type => type.FullName + "@" + type.Scope.Name,
            MemberReference member => member.FullName + "@" + member.DeclaringType.Scope.Name,
            VariableDefinition local => "local:" + local.Index + ":" + local.VariableType.FullName,
            ParameterDefinition parameter => "parameter:" + parameter.Index + ":" + parameter.ParameterType.FullName,
            null => "", _ => Convert.ToString(operand, System.Globalization.CultureInfo.InvariantCulture)!,
        };
        return new { signature = method.FullName, attributes = (int)method.Attributes,
            implAttributes = (int)method.ImplAttributes, initLocals = method.Body.InitLocals,
            maxStack = method.Body.MaxStackSize, exceptionHandlers = method.Body.ExceptionHandlers.Count,
            locals = method.Body.Variables.Select(v => v.VariableType.FullName).ToArray(),
            maskedIl = Convert.ToHexString(il), instructions = method.Body.Instructions.Select(i =>
                new { offset = i.Offset, op = i.OpCode.Code.ToString(), operand = Operand(i.Operand) }).ToArray() };
    }

    private static object[] CheckCutPhysical(string originalDirectory, AssemblyDefinition app, string mode)
    {
        using var before = AssemblyDefinition.ReadAssembly(Path.Combine(originalDirectory, "GraphApp.dll"));
        var originalOwner = before.MainModule.GetType("Graph.EagerDeadCross");
        var owner = app.MainModule.GetType(originalOwner.FullName);
        string[] names = mode.EndsWith("true-carrier", StringComparison.Ordinal)
            ? new[] { "Decline" } : new[] { "PrefilterOnly", "Decline" };
        Require(originalOwner.Methods.Select(m => m.Name).SequenceEqual(names)
            && owner.Methods.Select(m => m.Name).SequenceEqual(names), "cut method order or set changed");
        var result = new List<object>();
        foreach (string name in names)
        {
            var original = originalOwner.Methods.Single(m => m.Name == name);
            var retained = owner.Methods.Single(m => m.Name == name);
            var physical = CutPhysical(retained);
            Require(JsonSerializer.Serialize(CutPhysical(original)) == JsonSerializer.Serialize(physical),
                "cut body/signature/local/scalar changed: " + name);
            Require(retained.IsPrivate && retained.IsStatic && !retained.IsVirtual
                && !retained.HasGenericParameters && !retained.HasOverrides, "cut fixture authority shape changed");
            if (name == "PrefilterOnly")
            {
                var scalar = retained.Body.Instructions.Single(i => i.OpCode.Code == Code.Ldc_I4);
                int originalToken = ((MethodReference)originalOwner.Methods.Single(m => m.Name == "Decline")
                    .Body.Instructions.Single(i => i.Operand is MethodReference r && r.Name == "get_Task").Operand)
                    .MetadataToken.ToInt32();
                Require((int)scalar.Operand == originalToken, "scalar bait was relocated as a metadata token");
                Require(retained.Body.Instructions.All(i => i.Operand is not MethodReference r || r.Name != "get_Task"),
                    "scalar bait gained an actual witness operand");
            }
            result.Add(physical);
        }
        return result.ToArray();
    }

    private static void CheckCutBackend(string originalDirectory, string outputDirectory)
    {
        using var beforeApp = AssemblyDefinition.ReadAssembly(Path.Combine(originalDirectory, "PolicyApp.dll"));
        using var app = AssemblyDefinition.ReadAssembly(Path.Combine(outputDirectory, "PolicyApp.dll"));
        using var beforeLibrary = AssemblyDefinition.ReadAssembly(Path.Combine(originalDirectory, "PolicyLibrary.dll"));
        using var library = AssemblyDefinition.ReadAssembly(Path.Combine(outputDirectory, "PolicyLibrary.dll"));
        var unused = app.MainModule.GetType("PolicyFixture.SignatureScript");
        Require(unused is not null && unused.Methods.All(m => m.Name != ".cctor"), "signature-only registration opened an initializer");
        Require(unused!.Methods.Single(m => m.Name == "Callback").Body.Instructions.Select(i => i.OpCode.Code)
            .SequenceEqual(new[] { Code.Ldc_I4, Code.Pop, Code.Ldnull, Code.Throw }), "signature-only callback became executable");
        Require(app.MainModule.GetType("PolicyFixture.BodyOnlyDependency") is null, "unused signature opened its body dependency");
        foreach (string name in new[] { "ExplicitScript", "CodeScript", "ScalarScript" })
        {
            var original = beforeApp.MainModule.GetType("PolicyFixture." + name);
            var retained = app.MainModule.GetType("PolicyFixture." + name);
            Require(retained is not null, "runtime policy type was removed: " + name);
            foreach (var method in original.Methods)
                Require(retained!.Methods.Any(m => m.FullName == method.FullName && CutBodyText(m) == CutBodyText(method)),
                    "runtime policy body changed: " + method.FullName);
        }
        foreach (string name in new[] { "SignatureParent", "SignatureWrapper" })
        {
            var retained = library.MainModule.GetType("Policy." + name);
            Require(retained is not null, "wrapper signature was removed: " + name);
            Require(retained!.Methods.All(m => m.Name != "CallbackLeaf"), "cut metadata opened a wrapper callback dependency: " + name);
            var constructor = retained.Methods.SingleOrDefault(m => m.Name == ".ctor");
            Require(name == "SignatureWrapper" ? constructor is not null && Stub(constructor) : constructor is null,
                "physical cut constructor operand gained executable authority: " + name);
            Require(retained.Methods.Single(m => m.Name == "Callback").Body.Instructions.Select(i => i.OpCode.Code)
                .SequenceEqual(new[] { Code.Ldc_I4, Code.Pop, Code.Ldnull, Code.Throw }), "signature-only wrapper virtual body became executable: " + name);
        }
        var originalRegistry = beforeLibrary.MainModule.GetType("Policy.Registry");
        var registry = library.MainModule.GetType("Policy.Registry");
        foreach (string name in new[] { "Root", "Live", "AllocatedWrapper", "FallbackWrapper", "SignatureWrapper" })
        {
            var factory = originalRegistry.Methods.Single(m => m.Name == "Factory" + name);
            Require(registry.Methods.Any(m => m.FullName == factory.FullName && CutBodyText(m) == CutBodyText(factory)),
                "runtime or fallback factory changed: " + name);
        }
        Require(registry.Methods.All(m => m.Name != "FactorySignatureParent"), "uncut unused parent factory survived");
        foreach (string name in new[] { "FactorySignatureWrapper", "FactoryLive" })
            Require(JsonSerializer.Serialize(CutPhysical(originalRegistry.Methods.Single(m => m.Name == name)))
                == JsonSerializer.Serialize(CutPhysical(registry.Methods.Single(m => m.Name == name))),
                "cut registry factory physical body changed: " + name);
        var originalKeys = originalRegistry.Methods.Single(m => m.Name == ".cctor").Body.Instructions
            .Where(i => i.OpCode.Code == Code.Ldstr).Select(i => (string)i.Operand);
        var instructions = registry.Methods.Single(m => m.Name == ".cctor").Body.Instructions;
        Require(originalKeys.SequenceEqual(instructions.Where(i => i.OpCode.Code == Code.Ldstr).Select(i => (string)i.Operand)),
            "constructor registry keys changed");
        for (int i = 0; i + 1 < instructions.Count; i++)
            if (instructions[i].Operand is string key)
            {
                string expected = key == "SignatureParent" ? "FactoryLive" : "Factory" + key;
                Require(instructions[i + 1].OpCode.Code == Code.Ldftn
                    && instructions[i + 1].Operand is MethodReference target && target.Name == expected
                    && registry.Methods.Any(m => m.FullName == target.FullName),
                    "physical factory pointer or ancestor redirect changed: " + key);
            }
        var array = app.CustomAttributes.Single(a => a.ConstructorArguments.Single().Value is CustomAttributeArgument[]);
        var registered = ((CustomAttributeArgument[])array.ConstructorArguments.Single().Value)
            .Select(a => ((TypeReference)a.Value).FullName);
        Require(registered.SequenceEqual(new[] { "PolicyFixture.ExplicitScript", "PolicyFixture.CodeScript", "PolicyFixture.ScalarScript" }),
            "registration did not distinguish metadata and runtime roots");
        var scalar = app.CustomAttributes.Single(a => a.ConstructorArguments.Single().Value is TypeReference);
        Require(((TypeReference)scalar.ConstructorArguments.Single().Value).FullName == "PolicyFixture.ScalarScript",
            "ordinary scalar attribute root changed");
        Console.WriteLine("cut-backend-contract=physical-factories,original-pointer,ancestor-redirect,ctor-stub,callback-runtime-negative");
    }

    private static string CutBodyText(MethodDefinition method) => string.Join("|", method.Body.Instructions.Select(i =>
        i.OpCode.Code + ":" + (i.Operand is MemberReference member ? member.FullName : i.Operand?.ToString())));

    private static object SelectedCutPhysical(MethodDefinition method)
    {
        Require(method.HasBody && !Stub(method), "selected cut became a stub: " + method.FullName);
        Require(method.Body.ExceptionHandlers.Count == 0, "selected cut exception regions changed");
        using var stream = File.OpenRead(method.Module.FileName);
        using var pe = new PEReader(stream);
        byte[] il = System.Reflection.Metadata.PEReaderExtensions.GetMethodBody(pe, method.RVA).GetILBytes()!;
        // Member and user-string tokens relocate; their decoded identities must not.
        foreach (var instruction in method.Body.Instructions)
            if (instruction.Operand is MemberReference or string)
                Array.Clear(il, instruction.Offset + instruction.OpCode.Size, 4);
        string Operand(object? operand) => operand switch
        {
            TypeReference type => type.FullName + "@" + type.Scope.Name,
            MemberReference member => member.FullName + "@" + member.DeclaringType.Scope.Name,
            VariableDefinition local => "local:" + local.Index + ":" + local.VariableType.FullName,
            ParameterDefinition parameter => "parameter:" + parameter.Index + ":" + parameter.ParameterType.FullName,
            null => "", _ => Convert.ToString(operand, System.Globalization.CultureInfo.InvariantCulture)!,
        };
        return new { signature = method.FullName, attributes = (int)method.Attributes,
            implAttributes = (int)method.ImplAttributes, initLocals = method.Body.InitLocals,
            maxStack = method.Body.MaxStackSize, exceptionHandlers = method.Body.ExceptionHandlers.Count,
            locals = method.Body.Variables.Select(v => v.VariableType.FullName).ToArray(),
            maskedIl = Convert.ToHexString(il), instructions = method.Body.Instructions.Select(i =>
                new { offset = i.Offset, op = i.OpCode.Code.ToString(), operand = Operand(i.Operand) }).ToArray() };
    }

    private static void CheckSelectedCut(string originalDirectory, string directory, string mode, string variant)
    {
        Require(mode is "selected-ss-known-instance" or "selected-named-inherited-known-upgrade", "unknown selected cut fixture");
        Require(variant is "ctor" or "setter" or "both", "unknown selected cut variant");
        bool inherited = mode == "selected-named-inherited-known-upgrade";
        Require(!inherited || variant == "setter", "inherited cut fixture must select its base setter");
        bool cutConstructor = variant is "ctor" or "both", cutSetter = variant is "setter" or "both";
        CheckSelectedLayout(directory, mode, "observed-before");
        using var original = AssemblyDefinition.ReadAssembly(Path.Combine(originalDirectory, "GraphLib.dll"));
        using var library = AssemblyDefinition.ReadAssembly(Path.Combine(directory, "GraphLib.dll"));
        using var app = AssemblyDefinition.ReadAssembly(Path.Combine(directory, "GraphApp.dll"));
        var attribute = library.MainModule.GetType("Graph.SelectedSpecialAttribute");
        var setterOwner = library.MainModule.GetType("Graph." + (inherited ? "SelectedBaseAttribute" : "SelectedSpecialAttribute"));
        Require(SelectedPropertyMetadata(setterOwner, "Value") && (!inherited || SelectedPropertyMetadata(attribute, "Second")),
            "selected cut lost named property/accessor identity");
        var identity = CheckSelectedClrMetadata(directory, "GraphLib", inherited, false, inherited);
        Require(identity == CheckSelectedClrMetadata(originalDirectory, "GraphLib", inherited, false, inherited),
            "selected cut changed CLR attribute metadata");
        bool Body(MethodDefinition? method) => method is { HasBody: true } && !Stub(method);
        object OriginalPhysical(MethodDefinition method)
        {
            var before = original.MainModule.GetType(method.DeclaringType.FullName).Methods.Single(m => m.FullName == method.FullName);
            var retained = SelectedCutPhysical(method);
            Require(JsonSerializer.Serialize(SelectedCutPhysical(before)) == JsonSerializer.Serialize(retained),
                "selected cut changed physical body/signature: " + method.FullName);
            return retained;
        }
        var physical = new List<object>();
        foreach (var method in new[] { attribute.Methods.Single(m => m.IsConstructor), setterOwner.Methods.Single(m => m.Name == "set_Value") })
        {
            Require(Body(method), "selected cut or independently reached body disappeared");
            if (method.IsConstructor ? !cutConstructor : !cutSetter) continue;
            physical.Add(OriginalPhysical(method));
        }
        // The known owner token retains ordinary attribute IL; primary emission
        // must still reject getters and unmatched constructors as executable targets.
        foreach (var owner in new[] { attribute, setterOwner }.Distinct())
            foreach (var property in owner.Properties)
            {
                Require(Body(property.SetMethod), "independently selected named setter disappeared");
                Require(Body(property.GetMethod), "known-owner attribute getter physical body disappeared");
                OriginalPhysical(property.GetMethod);
            }
        if (!inherited)
            OriginalPhysical(library.MainModule.GetType("Graph.SelectedSiblingAttribute").Methods.Single(m => m.IsConstructor));
        var physicalEffects = new SortedDictionary<string, bool>();
        foreach (string effect in SelectedEffects)
        {
            bool expected = effect switch
            {
                "Ctor" => !cutConstructor, "Setter" => !cutSetter,
                "SecondSetter" => inherited, "Conditional" => !inherited,
                "Getter" => true, "Sibling" => !inherited, _ => false,
            };
            var target = app.MainModule.GetType("Graph.Selected" + effect + "Target");
            bool body = Body(target?.Methods.SingleOrDefault(m => m.IsConstructor));
            bool dependency = Body(library.MainModule.GetType("Graph.Selected" + effect + "Dependency")?.Methods.SingleOrDefault());
            Require(body == expected && dependency == expected, "selected cut changed physical effect retention: " + effect);
            physicalEffects[effect] = body;
        }
        Console.WriteLine("selected-cut-observed=" + JsonSerializer.Serialize(new { physicalEffects, physical, identity }));
        Console.WriteLine("graph-selected-cut=published:" + mode + ":" + variant + ":physical-body,metadata-identity,execution-boundaries");
    }
}
