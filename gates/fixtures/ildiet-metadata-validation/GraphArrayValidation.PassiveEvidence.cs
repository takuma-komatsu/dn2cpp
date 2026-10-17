using System.Globalization;
using System.Reflection.PortableExecutable;
using System.Text.Json;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MetadataTokens = System.Reflection.Metadata.Ecma335.MetadataTokens;

static class PassiveEvidenceInput
{
    private static void Require(bool value, string reason)
    {
        if (!value) throw new InvalidOperationException(reason);
    }

    public static void Create(string variant, string baseline, string hostCore, string output)
    {
        Require(!Directory.Exists(output) && !File.Exists(output), "Output already exists");
        Directory.CreateDirectory(output);
        foreach (var path in Directory.GetFiles(baseline).OrderBy(p => p, StringComparer.Ordinal))
            if (Path.GetFileName(path) is not ("GraphApp.dll" or "System.Private.CoreLib.dll"))
                File.Copy(path, Path.Combine(output, Path.GetFileName(path)), false);

        using var host = AssemblyDefinition.ReadAssembly(hostCore);
        using var core = AssemblyDefinition.ReadAssembly(Path.Combine(baseline, "System.Private.CoreLib.dll"));
        Require(core.Name.FullName == host.Name.FullName && core.Name.PublicKey.SequenceEqual(host.Name.PublicKey),
            "Tiny core must retain the full original core assembly identity");
        var cm = core.MainModule;
        Require(cm.Types.Count == 11 && cm.AssemblyReferences.Count == 0, "Unexpected baseline tiny core");
        TypeDefinition Shell(string name)
        {
            var original = host.MainModule.GetType(name);
            Require(original is not null && !original.HasGenericParameters, "Missing real core type: " + name);
            var shell = new TypeDefinition(original!.Namespace, original.Name, original.Attributes,
                cm.GetType(original.BaseType.FullName));
            shell.PackingSize = original.PackingSize;
            shell.ClassSize = original.ClassSize;
            cm.Types.Add(shell);
            return shell;
        }
        var typed = Shell("System.TypedReference");
        var handle = Shell("System.RuntimeTypeHandle");
        var byrefAttribute = Shell("System.Runtime.CompilerServices.IsByRefLikeAttribute");
        var attributeCtor = new MethodDefinition(".ctor", MethodAttributes.Public | MethodAttributes.HideBySig
            | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName, cm.TypeSystem.Void);
        byrefAttribute.Methods.Add(attributeCtor);
        attributeCtor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
        attributeCtor.Body.Instructions.Add(Instruction.Create(OpCodes.Call,
            cm.GetType("System.Attribute").Methods.Single(m => m.Name == ".ctor")));
        attributeCtor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        Require(host.MainModule.GetType("System.TypedReference").CustomAttributes.Any(a =>
            a.AttributeType.FullName == byrefAttribute.FullName), "Real TypedReference lost byref-like identity");
        typed.CustomAttributes.Add(new CustomAttribute(attributeCtor));
        var realGet = host.MainModule.GetType("System.Type").Methods.Single(m => m.Name == "GetTypeFromHandle"
            && m.IsStatic && m.Parameters.Count == 1 && m.Parameters[0].ParameterType.FullName == handle.FullName);
        Require(realGet.ReturnType.FullName == "System.Type" && !realGet.HasGenericParameters,
            "Real Type.GetTypeFromHandle signature changed");
        var get = new MethodDefinition(realGet.Name, realGet.Attributes, cm.GetType("System.Type"));
        get.Parameters.Add(new ParameterDefinition(handle));
        cm.GetType("System.Type").Methods.Add(get);
        get.Body.Instructions.Add(Instruction.Create(OpCodes.Ldnull));
        get.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        Require(get.FullName == realGet.FullName && cm.AssemblyReferences.Count == 0,
            "Tiny core API introduced a host dependency");
        core.Write(Path.Combine(output, "System.Private.CoreLib.dll"),
            new WriterParameters { Timestamp = 0, DeterministicMvid = true });

        using var app = AssemblyDefinition.ReadAssembly(Path.Combine(baseline, "GraphApp.dll"));
        var module = app.MainModule;
        var owner = module.GetType("Graph.EagerDeadCross");
        var carrier = owner.Methods.Single(m => m.Name == "Decline");
        Require(carrier.IsPrivate && carrier.IsStatic && !carrier.IsVirtual && !carrier.HasGenericParameters
            && !carrier.HasOverrides && carrier.Parameters.Count == 0 && carrier.Body.Instructions.Count == 4,
            "Baseline mode32 carrier changed");
        var witness = (MethodReference)carrier.Body.Instructions.Single(i => i.OpCode == OpCodes.Call).Operand;
        Require(witness.Name == "get_Task" && witness.DeclaringType.FullName == "Graph.EagerBuilder`1<System.Int64>"
            && witness.ReturnType.FullName == "Graph.EagerDeclinedTask`1<!0>", "Wrong original async MemberRef");
        var importedGet = module.ImportReference(get);
        MethodDefinition reflected;
        if (variant == "true-carrier") reflected = carrier;
        else
        {
            reflected = new MethodDefinition("PrefilterOnly", MethodAttributes.Private | MethodAttributes.Static
                | MethodAttributes.HideBySig, module.TypeSystem.Void);
            owner.Methods.Insert(owner.Methods.IndexOf(carrier), reflected);
            reflected.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4, 0));
            reflected.Body.Instructions.Add(Instruction.Create(OpCodes.Pop));
            reflected.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        }
        reflected.Parameters.Add(new ParameterDefinition("unused", ParameterAttributes.None, module.TypeSystem.TypedReference));
        Instruction[] prefix = { Instruction.Create(OpCodes.Ldarg_0), Instruction.Create(OpCodes.Refanytype),
            Instruction.Create(OpCodes.Call, importedGet), Instruction.Create(OpCodes.Pop) };
        for (int i = prefix.Length - 1; i >= 0; i--) reflected.Body.Instructions.Insert(0, prefix[i]);
        using var staged = new MemoryStream();
        app.Write(staged, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        byte[] bytes = staged.ToArray();
        // Patch only the scalar immediate after final metadata assignment. Rewriting
        // the PE again could renumber the MemberRef whose bytes are the prefilter bait.
        if (variant == "false-prefilter")
        {
            using var emitted = AssemblyDefinition.ReadAssembly(new MemoryStream(bytes));
            var methods = emitted.MainModule.GetType(owner.FullName).Methods;
            var genuine = methods.Single(m => m.Name == "Decline");
            int token = ((MethodReference)genuine.Body.Instructions.Single(i => i.OpCode == OpCodes.Call).Operand)
                .MetadataToken.ToInt32();
            var falseMethod = methods.Single(m => m.Name == "PrefilterOnly");
            var scalar = falseMethod.Body.Instructions.Single(i => i.OpCode == OpCodes.Ldc_I4);
            using var pe = new PEReader(new MemoryStream(bytes));
            var section = pe.PEHeaders.SectionHeaders.Single(s => falseMethod.RVA >= s.VirtualAddress
                && falseMethod.RVA < s.VirtualAddress + s.SizeOfRawData);
            int start = section.PointerToRawData + falseMethod.RVA - section.VirtualAddress;
            int header = (bytes[start] & 3) == 2 ? 1 : (bytes[start + 1] >> 4) * 4;
            Require((bytes[start] & 3) is 2 or 3 && header >= 1, "Unsupported method header");
            int offset = start + header + scalar.Offset + 1;
            Require(bytes[offset - 1] == 0x20, "False hit must be ldc.i4 immediate bytes");
            for (int i = 0; i < 4; i++) bytes[offset + i] = (byte)(token >> (i * 8));
        }
        using (var file = new FileStream(Path.Combine(output, "GraphApp.dll"), FileMode.CreateNew)) file.Write(bytes);
        Audit(variant, output, core.Name.FullName, realGet.FullName);
    }

    private static void Audit(string variant, string output, string coreIdentity, string realGet)
    {
        using var app = AssemblyDefinition.ReadAssembly(Path.Combine(output, "GraphApp.dll"));
        using var core = AssemblyDefinition.ReadAssembly(Path.Combine(output, "System.Private.CoreLib.dll"));
        using var stream = File.OpenRead(Path.Combine(output, "GraphApp.dll"));
        using var pe = new PEReader(stream);
        var reader = System.Reflection.Metadata.PEReaderExtensions.GetMetadataReader(pe);
        var owner = app.MainModule.GetType("Graph.EagerDeadCross");
        var carrier = owner.Methods.Single(m => m.Name == "Decline");
        var witnessInstruction = carrier.Body.Instructions.Single(i => i.Operand is MethodReference m && m.Name == "get_Task");
        var witness = (MethodReference)witnessInstruction.Operand;
        int token = witness.MetadataToken.ToInt32();
        Require((uint)token >> 24 == 0x0a, "Async evidence must be a genuine MemberRef");
        var rawMember = reader.GetMemberReference(MetadataTokens.MemberReferenceHandle(token & 0xffffff));
        Require(reader.GetString(rawMember.Name) == "get_Task", "Raw metadata does not name the builder getter");
        var allMethods = app.MainModule.GetTypes().SelectMany(t => t.Methods).ToArray();
        var wanted = variant == "true-carrier" ? carrier : owner.Methods.Single(m => m.Name == "PrefilterOnly");
        Require(!allMethods.Any(m => m.Overrides.Any(r => r.MetadataToken.ToInt32() == token)), "Override bypass carrier");
        Require(!reader.CustomAttributes.Any(h => MetadataTokens.GetToken(reader.GetCustomAttribute(h).Constructor) == token),
            "Attribute bypass carrier");
        var hits = new List<object>();
        var rawHitNames = new List<string>();
        foreach (var method in allMethods.Where(m => m.HasBody).OrderBy(m => m.MetadataToken.ToInt32()))
        {
            var il = System.Reflection.Metadata.PEReaderExtensions.GetMethodBody(pe, method.RVA).GetILBytes()!;
            var offsets = Enumerable.Range(0, Math.Max(0, il.Length - 3)).Where(i =>
                Enumerable.Range(0, 4).All(j => il[i + j] == (byte)(token >> (j * 8)))).ToArray();
            var matches = method.Body.Instructions.Where(i => i.Operand is MemberReference r
                && r.MetadataToken.ToInt32() == token).Select(i => i.Offset).ToArray();
            if (offsets.Length == 0) continue;
            rawHitNames.Add(method.Name);
            hits.Add(new { method = method.FullName, methodToken = method.MetadataToken.ToInt32(),
                rawOffsets = offsets, actualOperandOffsets = matches, il = Convert.ToHexString(il) });
            if (method == wanted && variant == "false-prefilter")
            {
                var scalar = method.Body.Instructions.Single(i => i.OpCode == OpCodes.Ldc_I4);
                Require((int)scalar.Operand == token && matches.Length == 0
                    && offsets.SequenceEqual(new[] { scalar.Offset + 1 }), "False hit is not exclusively scalar bytes");
            }
            else Require(method == carrier && matches.SequenceEqual(new[] { witnessInstruction.Offset }),
                "Unexpected actual or accidental witness carrier");
        }
        Require(rawHitNames.SequenceEqual(variant == "true-carrier" ? new[] { "Decline" } : new[] { "PrefilterOnly", "Decline" }),
            "Scanner must meet the selected dead method before any other witness carrier");
        foreach (var dead in owner.Methods)
        {
            Require(dead.IsPrivate && dead.IsStatic && !dead.IsVirtual && !dead.HasGenericParameters && !dead.HasOverrides,
                "Carrier gained an executable shape request");
            Require(!allMethods.SelectMany(m => m.HasBody ? m.Body.Instructions : Enumerable.Empty<Instruction>())
                .Any(i => i.Operand is MethodReference r && r.DeclaringType.FullName == owner.FullName), "Carrier has a caller");
        }
        Require(wanted.Parameters.Count == 1 && wanted.Parameters[0].ParameterType.MetadataType == MetadataType.TypedByReference,
            "refanytype input must be a real typed-reference signature");
        Require(wanted.Body.Instructions.Take(4).Select(i => i.OpCode.Code).SequenceEqual(
            new[] { Code.Ldarg_0, Code.Refanytype, Code.Call, Code.Pop }), "Unsupported sequence changed");
        var get = (MethodReference)wanted.Body.Instructions[2].Operand;
        Require(get.FullName == realGet && get.DeclaringType.Scope.Name == "System.Private.CoreLib"
            && get.Parameters[0].ParameterType.IsValueType, "RuntimeTypeHandle API identity changed");
        Require(core.Name.FullName == coreIdentity && core.MainModule.AssemblyReferences.Count == 0
            && core.MainModule.Types.Count == 14, "Tiny core load set changed");
        foreach (var file in Directory.GetFiles(output, "*.dll"))
        {
            using var assembly = AssemblyDefinition.ReadAssembly(file);
            foreach (var reference in assembly.MainModule.AssemblyReferences)
            {
                string target = Path.Combine(output, reference.Name + ".dll");
                Require(File.Exists(target), "Input resolution would escape isolated directory: " + reference.FullName);
                using var resolved = AssemblyDefinition.ReadAssembly(target);
                Require(resolved.Name.FullName == reference.FullName, "Explicit assembly identity mismatch");
            }
        }
        var audit = new { variant, coreIdentity, api = realGet, witnessToken = token, witness = witness.FullName,
            witnessOwnerScope = witness.DeclaringType.Scope.Name, requiredOperand = witnessInstruction.Offset,
            failingCarrier = wanted.FullName, rawHitNames, hits, attributeOrOverrideCarrier = false,
            carriersCalled = false, carrierIsNongenericNonvirtualPrivate = true,
            primarySupported = (bool?)null, companionFailureReproduced = (bool?)null };
        using var destination = new FileStream(Path.Combine(output, "operand-audit.json"), FileMode.CreateNew);
        JsonSerializer.Serialize(destination, audit, new JsonSerializerOptions { WriteIndented = true });
    }
}

static partial class GraphArrayValidation
{
    private static readonly string[] PassiveEvidenceModes = { "passive-async-true-carrier", "passive-async-false-prefilter" };
    private static bool PassiveEvidenceMode(string mode) => PassiveEvidenceModes.Contains(mode);

    private static void CreatePassiveEvidence(string directory, string corelib, string mode)
    {
        Require(PassiveEvidenceMode(mode), "unknown passive evidence mode");
        string baseline = directory + ".baseline";
        Require(!Directory.Exists(directory) && !Directory.Exists(baseline), "passive evidence output exists");
        CreateEagerModel(baseline, corelib, "eager-array-async-option-sensitive");
        PassiveEvidenceInput.Create(mode["passive-async-".Length..], baseline, corelib, directory);
    }

    private static void CheckPassiveEvidence(string directory, string mode, string state)
    {
        Require(PassiveEvidenceMode(mode) && (state is "original" or "published"), "unknown passive evidence state");
        bool original = state == "original";
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
        if (!original) Require(falseCarrier is null, "scalar token bait acquired a retained MethodDef");
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
        Console.WriteLine("passive-evidence-observed=" + JsonSerializer.Serialize(new { bodies, negatives, physical, contracts, order,
            falseCarrierBody = Body(falseCarrier), falseCarrierPresent = falseCarrier is not null, carriersCalled = false }));
        Console.WriteLine("graph-passive-evidence=" + state + ":" + mode + ":physical-only-memberref,ordinary-body-boundary");
    }
}
