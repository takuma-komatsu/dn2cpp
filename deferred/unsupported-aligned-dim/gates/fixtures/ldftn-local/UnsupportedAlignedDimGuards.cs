using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using Mono.Cecil.Cil;
using CecilMethod = Mono.Cecil.MethodDefinition;
using CecilType = Mono.Cecil.TypeDefinition;

internal static class UnsupportedAlignedDimGuards
{
    private const BindingFlags Instance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private const BindingFlags Static = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    private static readonly (int Width, string Element)[] Pairs = new[] { (0, "Decimal") }
        .Concat(new[] { 64, 128, 256, 512 }.SelectMany(w => new[] { "Char", "Int128", "UInt128" }.Select(e => (w, e)))).ToArray();
    private static readonly string[] Operations = { "LoadAligned", "StoreAligned" };
    private static readonly string[] ReferenceMutations = { "valid", "instance", "explicit-this", "vararg", "generic", "return",
        "byref", "modifier", "parameter", "concrete", "methodvar", "var-index", "owner-modifier", "owner-self",
        "owner-element", "methodspec", "methodspec-zero" };

    public static bool Run(string[] args)
    {
        if (args.Length == 4 && args[0] == "--unsupported-aligned-dim-reference-fixture")
        {
            var mode = args[3].Split(':');
            if (mode.Length != 5 || !Operations.Contains(mode[0]) || !ReferenceMutations.Contains(mode[4])
                || mode[3] is not ("direct" or "address" or "calli")) throw new ArgumentException("operation:width:element:route:mutation");
            Build(args[1], args[2], (mode[0], int.Parse(mode[1], CultureInfo.InvariantCulture), mode[2], mode[3], mode[4]));
            Console.WriteLine("unsupported-aligned-dim-reference-fixture:" + args[3]);
            return true;
        }
        if (args.Length is not (4 or 5) || args[0] != "--unsupported-aligned-dim-guards") return false;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        string modeName = args.Length == 5 ? args[4] : "all";
        if (modeName is not ("all" or "positive" or "raw" or "warm" or "references" or "audit")) throw new ArgumentException("unknown authority mode");
        Build(args[3], args[2]);
        using var harness = new Harness(args[1], args[2], args[3], modeName == "audit");
        harness.Run(modeName);
        return true;
    }

    private static string Label(string operation, int width, string element, string mutation) => operation + "_" + width + "_" + element + "_" + mutation;

    private static void Build(string output, string corePath,
        (string Operation, int Width, string Element, string Route, string Mutation)? single = null)
    {
        output = Path.GetFullPath(output);
        if (File.Exists(output)) throw new InvalidOperationException("guard fixture output must be new");
        using var core = Mono.Cecil.AssemblyDefinition.ReadAssembly(corePath);
        using var app = Mono.Cecil.AssemblyDefinition.CreateAssembly(new Mono.Cecil.AssemblyNameDefinition("UnsupportedAlignedDimGuards", new Version(1, 0)),
            "UnsupportedAlignedDimGuards", single is null ? Mono.Cecil.ModuleKind.Dll : Mono.Cecil.ModuleKind.Console);
        var module = app.MainModule;
        var host = new CecilType("Fixture", "Cases", Mono.Cecil.TypeAttributes.Public, module.TypeSystem.Object); module.Types.Add(host);
        var value = new CecilType("Fixture", "Value", Mono.Cecil.TypeAttributes.Public | Mono.Cecil.TypeAttributes.Sealed | Mono.Cecil.TypeAttributes.SequentialLayout,
            module.ImportReference(core.MainModule.GetType("System.ValueType"))); module.Types.Add(value);
        value.Fields.Add(new Mono.Cecil.FieldDefinition("Field", Mono.Cecil.FieldAttributes.Public, module.TypeSystem.Int32));
        var enumeration = new CecilType("Fixture", "Enum", Mono.Cecil.TypeAttributes.Public | Mono.Cecil.TypeAttributes.Sealed,
            module.ImportReference(core.MainModule.GetType("System.Enum"))); module.Types.Add(enumeration);
        enumeration.Fields.Add(new Mono.Cecil.FieldDefinition("value__", Mono.Cecil.FieldAttributes.Public | Mono.Cecil.FieldAttributes.SpecialName | Mono.Cecil.FieldAttributes.RTSpecialName, module.TypeSystem.Int32));
        var genericValue = new CecilType("Fixture", "Box`1", Mono.Cecil.TypeAttributes.Public | Mono.Cecil.TypeAttributes.Sealed | Mono.Cecil.TypeAttributes.SequentialLayout,
            module.ImportReference(core.MainModule.GetType("System.ValueType"))); module.Types.Add(genericValue);
        genericValue.GenericParameters.Add(new Mono.Cecil.GenericParameter("T", genericValue));
        genericValue.Fields.Add(new Mono.Cecil.FieldDefinition("Field", Mono.Cecil.FieldAttributes.Public, genericValue.GenericParameters[0]));
        var declaration = module.ImportReference(core.MainModule.GetType("System.Runtime.Intrinsics.ISimdVector`2"));
        var cases = single is { } selected ? new[] { selected }
            : Pairs.SelectMany(p => Operations.SelectMany(o => ReferenceMutations.Select(m => (o, p.Width, p.Element, "direct", m)))).ToArray();
        foreach (var (operation, width, elementName, route, mutation) in cases)
        {
            var element = module.ImportReference(core.MainModule.GetType("System." + elementName));
            var vector = new Mono.Cecil.GenericInstanceType(module.ImportReference(core.MainModule.GetType(VectorAlignmentFixture.Owner(width) + "`1")));
            vector.GenericArguments.Add(element);
            var contract = new Mono.Cecil.GenericInstanceType(declaration);
            contract.GenericArguments.Add(mutation == "owner-self" ? module.TypeSystem.Int32 : vector);
            contract.GenericArguments.Add(mutation == "owner-element" ? module.TypeSystem.UInt16 : element);
            Mono.Cecil.MethodReference target = new(operation, operation == "LoadAligned" ? declaration.GenericParameters[0] : module.TypeSystem.Void, contract);
            if (operation == "StoreAligned") target.Parameters.Add(new Mono.Cecil.ParameterDefinition(declaration.GenericParameters[0]));
            target.Parameters.Add(new Mono.Cecil.ParameterDefinition(new Mono.Cecil.PointerType(declaration.GenericParameters[1])));
            switch (mutation)
            {
                case "instance": target.HasThis = true; break;
                case "explicit-this": target.HasThis = true; target.ExplicitThis = true; break;
                case "vararg": target.CallingConvention = Mono.Cecil.MethodCallingConvention.VarArg; break;
                case "generic": target.GenericParameters.Add(new Mono.Cecil.GenericParameter("T", target)); break;
                case "return": target.ReturnType = module.TypeSystem.Int32; break;
                case "byref": target.Parameters[^1].ParameterType = new Mono.Cecil.ByReferenceType(declaration.GenericParameters[1]); break;
                case "modifier": target.Parameters[^1].ParameterType = new Mono.Cecil.OptionalModifierType(module.TypeSystem.Object, target.Parameters[^1].ParameterType); break;
                case "parameter": target.Parameters.Add(new Mono.Cecil.ParameterDefinition(module.TypeSystem.Int32)); break;
                case "concrete":
                    target.ReturnType = operation == "LoadAligned" ? vector : module.TypeSystem.Void;
                    if (operation == "StoreAligned") target.Parameters[0].ParameterType = vector;
                    target.Parameters[^1].ParameterType = new Mono.Cecil.PointerType(element); break;
                case "methodvar":
                    var generic = new Mono.Cecil.MethodReference("Foreign", module.TypeSystem.Void, host);
                    generic.GenericParameters.Add(new Mono.Cecil.GenericParameter("T", generic));
                    target.Parameters[^1].ParameterType = new Mono.Cecil.PointerType(generic.GenericParameters[0]); break;
                case "var-index": target.Parameters[^1].ParameterType = new Mono.Cecil.PointerType(declaration.GenericParameters[0]); break;
                case "owner-modifier": target.DeclaringType = new Mono.Cecil.OptionalModifierType(module.TypeSystem.Object, contract); break;
                case "methodspec": case "methodspec-zero":
                    var spec = new Mono.Cecil.GenericInstanceMethod(target);
                    if (mutation == "methodspec") spec.GenericArguments.Add(element);
                    target = spec; break;
            }
            var method = new CecilMethod(Label(operation, width, elementName, mutation), Mono.Cecil.MethodAttributes.Public | Mono.Cecil.MethodAttributes.Static,
                module.TypeSystem.Void); host.Methods.Add(method);
            var il = method.Body.GetILProcessor();
            if (single is null)
            {
                il.Emit(OpCodes.Ldftn, target); il.Emit(OpCodes.Pop); il.Emit(OpCodes.Ret);
                continue;
            }
            if (operation == "StoreAligned")
            {
                var local = new VariableDefinition(vector); method.Body.Variables.Add(local); method.Body.InitLocals = true;
                il.Emit(OpCodes.Ldloca, local); il.Emit(OpCodes.Initobj, vector); il.Emit(OpCodes.Ldloc, local);
            }
            il.Emit(OpCodes.Ldc_I4_0); il.Emit(OpCodes.Conv_U);
            if (route == "direct") il.Emit(OpCodes.Call, target);
            else
            {
                il.Emit(OpCodes.Ldftn, target);
                if (route == "address")
                {
                    var address = new VariableDefinition(module.TypeSystem.IntPtr); method.Body.Variables.Add(address);
                    il.Emit(OpCodes.Stloc, address); il.Emit(OpCodes.Ldloc, address);
                }
                var site = new Mono.Cecil.CallSite(operation == "LoadAligned" ? vector : module.TypeSystem.Void);
                if (operation == "StoreAligned") site.Parameters.Add(new Mono.Cecil.ParameterDefinition(vector));
                site.Parameters.Add(new Mono.Cecil.ParameterDefinition(new Mono.Cecil.PointerType(element)));
                il.Emit(OpCodes.Calli, site);
            }
            if (operation == "LoadAligned") il.Emit(OpCodes.Pop);
            il.Emit(OpCodes.Ret); app.EntryPoint = method;
        }
        app.Write(output, new Mono.Cecil.WriterParameters { Timestamp = 0, DeterministicMvid = true });
    }

    private static object? Get(object value, string name) => value.GetType().GetField(name, Instance) is { } field
        ? field.GetValue(value) : value.GetType().GetProperty(name, Instance)!.GetValue(value);
    private static object Required(object value, string name) => Get(value, name) ?? throw new InvalidOperationException("missing " + name);
    private static void Set(object value, string name, object? replacement)
    {
        if (value.GetType().GetField(name, Instance) is { } field) field.SetValue(value, replacement);
        else value.GetType().GetProperty(name, Instance)!.SetValue(value, replacement);
    }
    private static object Clone(object value) => typeof(object).GetMethod("MemberwiseClone", Instance)!.Invoke(value, null)!;
    private sealed record Mutation(string Name, (int Offset, byte[] Bytes)[] Patches);

    private sealed class Harness : IDisposable
    {
        private readonly Assembly _production;
        private readonly object _compilation, _core, _app, _empty;
        private readonly PEReader _pe;
        private readonly MetadataReader _reader;
        private readonly byte[] _image;
        private readonly Mono.Cecil.AssemblyDefinition _cecil, _fixture;
        private readonly Dictionary<string, EntityHandle> _references;
        private readonly HashSet<string> _failures = new(StringComparer.Ordinal);
        private readonly bool _audit;
        private int _checks, _raw, _warm, _referenceChecks, _qualifications;

        internal Harness(string compiler, string corePath, string input, bool audit)
        {
            _audit = audit;
            _production = Assembly.LoadFrom(Path.GetFullPath(compiler));
            var options = Activator.CreateInstance(Model("TranspileOptions"))!;
            Set(options, "Input", Path.GetFullPath(input)); Set(options, "UseILDiet", false); Set(options, "SharedGenerics", false);
            Set(options, "AutoRef", false); Set(options, "IsaSurfaceDump", "predicate-guard-only");
            _compilation = Model("Compilation").GetMethod("Create", Static)!.Invoke(null,
                new object[] { new[] { Path.GetFullPath(input), Path.GetFullPath(corePath) }, options })!;
            var modules = ((IEnumerable)Required(_compilation, "Modules")).Cast<object>().ToArray();
            _core = modules.Single(m => (string)Required(m, "AssemblyName") == "System.Private.CoreLib");
            _app = modules.Single(m => (string)Required(m, "AssemblyName") == "UnsupportedAlignedDimGuards");
            _empty = Model("GenericContext").GetField("Empty", Static)!.GetValue(null)!;
            _pe = (PEReader)Required(_core, "PE"); _reader = (MetadataReader)Required(_core, "Reader"); _image = File.ReadAllBytes(corePath);
            _cecil = Mono.Cecil.AssemblyDefinition.ReadAssembly(corePath); _fixture = Mono.Cecil.AssemblyDefinition.ReadAssembly(input);
            _references = _fixture.MainModule.GetType("Fixture.Cases").Methods.ToDictionary(m => m.Name,
                m => MetadataTokens.EntityHandle(((Mono.Cecil.MethodReference)m.Body.Instructions[0].Operand).MetadataToken.ToInt32()));
        }

        private Type Model(string name) => _production.GetType("Dn2Cpp." + name, true)!;
        private object? Invoke(string name, params object?[] arguments) => Model("Compilation").GetMethod(name, Instance)!.Invoke(_compilation, arguments);
        private object StaticInvoke(string owner, string name, params object?[] arguments) => Model(owner).GetMethod(name, Static)!.Invoke(null, arguments)!;
        private Array Args(params object[] values)
        {
            var result = Array.CreateInstance(Model("TypeDesc"), values.Length);
            for (int i = 0; i < values.Length; i++) result.SetValue(values[i], i);
            return result;
        }
        private object Context(Array types, Array methods) => Activator.CreateInstance(Model("GenericContext"), new object[] { types, methods })!;
        private void Check(bool success, string label)
        {
            if (!success)
            {
                if (!_audit) throw new InvalidOperationException("unsupported aligned DIM authority: " + label);
                _failures.Add(label);
            }
            _checks++;
        }
        private bool Refused(Func<object?> action, string label)
        {
            try { return action() is null; }
            catch (TargetInvocationException error) when (error.InnerException is NotSupportedException) { return true; }
            catch (TargetInvocationException error) when (_audit && error.InnerException is not null)
            { _failures.Add(label + ":non-refusal-exception:" + error.InnerException.GetType().Name); return false; }
        }
        private CecilMethod Definition(object method) => (CecilMethod)_cecil.MainModule.LookupToken(MetadataTokens.GetToken((MethodDefinitionHandle)Required(method, "Handle")));
        private int Row(TableIndex table, int row) => _pe.PEHeaders.MetadataStartOffset + _reader.GetTableMetadataOffset(table) + (row - 1) * _reader.GetTableRowSize(table);
        private int Blob(BlobHandle handle)
        {
            int offset = _pe.PEHeaders.MetadataStartOffset + _reader.GetHeapMetadataOffset(HeapIndex.Blob) + MetadataTokens.GetHeapOffset(handle);
            return offset + (_image[offset] < 128 ? 1 : _image[offset] < 192 ? 2 : 4);
        }
        private int Rva(int rva)
        {
            var section = _pe.PEHeaders.SectionHeaders.Single(s => rva >= s.VirtualAddress && rva < s.VirtualAddress + s.SizeOfRawData);
            return section.PointerToRawData + rva - section.VirtualAddress;
        }
        private int Body(CecilMethod method)
        {
            int header = Rva(method.RVA); return header + ((_image[header] & 3) == 2 ? 1 : (_image[header + 1] >> 4) * 4);
        }
        private int StringSize => _reader.GetHeapSize(HeapIndex.String) > ushort.MaxValue ? 4 : 2;
        private int BlobSize => _reader.GetHeapSize(HeapIndex.Blob) > ushort.MaxValue ? 4 : 2;
        private int MethodSize => Math.Max(_reader.GetTableRowCount(TableIndex.MethodDef), _reader.GetTableRowCount(TableIndex.MemberRef)) < 32768 ? 2 : 4;
        private int TypeSize => _reader.GetTableRowCount(TableIndex.TypeDef) < 65536 ? 2 : 4;
        private static byte[] Index(int value, int size) => BitConverter.GetBytes(value).Take(size).ToArray();

        private sealed record Case(string Operation, int Width, string Element, EntityHandle Reference, object Root, object Proof,
            object SelfValue, EntityHandle AlignmentReceiver, EntityHandle UnsafeReceiver)
        {
            internal string Label => Operation + ":" + Width + ":" + Element;
        }

        internal void Run(string mode)
        {
            var cases = new List<Case>();
            foreach (var (width, element) in Pairs)
            foreach (string operation in Operations)
            {
                var reference = _references[Label(operation, width, element, "valid")];
                var root = Invoke("ResolveCallTarget", _app, reference, _empty) ?? throw new InvalidOperationException("missing genuine DIM root");
                var proof = Invoke("ResolveUnsupportedAlignedDimProof", root) ?? throw new InvalidOperationException("missing genuine DIM proof");
                var definition = Definition(root);
                EntityHandle ReceiverAt(string name) => MetadataTokens.EntityHandle(((Mono.Cecil.TypeReference)definition.Body.Instructions
                    .Single(i => i.Offset == (int)Required(proof, name + "Offset")).Previous.Operand).MetadataToken.ToInt32());
                var item = new Case(operation, width, element, reference, root, proof,
                    ((Array)Required(Required(root, "Context"), "TypeArgs")).GetValue(0)!, ReceiverAt("Alignment"), ReceiverAt("Unsafe"));
                Positive(item); cases.Add(item);
            }
            if (mode is "all" or "references" or "audit") Excluded(cases);
            foreach (var item in cases)
            {
                int rawBefore = _raw, warmBefore = _warm, referenceBefore = _referenceChecks, qualifiedBefore = _qualifications;
                if (mode is "all" or "references" or "audit") References(item);
                if (mode is "all" or "warm" or "audit") Warm(item);
                if (mode is "all" or "raw" or "audit") Raw(item);
                Console.WriteLine("unsupported-aligned-dim-authority:" + item.Label + ":raw=" + (_raw - rawBefore)
                    + ":warm=" + (_warm - warmBefore) + ":references=" + (_referenceChecks - referenceBefore) + ":qualified=" + (_qualifications - qualifiedBefore));
            }
            if (_failures.Count != 0)
            {
                foreach (string label in _failures.OrderBy(x => x, StringComparer.Ordinal)) Console.Error.WriteLine("unsupported-aligned-dim-audit-failure:" + label);
                throw new InvalidOperationException("unsupported aligned DIM audit found " + _failures.Count + " distinct failures");
            }
            Console.WriteLine("unsupported-aligned-dim-guards:roots=" + cases.Count + ":checks=" + _checks + ":raw=" + _raw + ":warm=" + _warm + ":references=" + _referenceChecks + ":qualified=" + _qualifications);
        }

        private object? Proof(Case item, bool reference = false)
        {
            var root = reference ? Invoke("ResolveCallTarget", _app, item.Reference, _empty)! : item.Root;
            return Invoke("ResolveUnsupportedAlignedDimProof", root);
        }

        private (object? Method, bool Terminal) Edge(object caller, int offset, int token, EntityHandle receiver, object? self)
        {
            object?[] arguments = { caller, offset, token, receiver, self, false };
            var method = Invoke("TryResolveUnsupportedAlignedDimEdge", arguments);
            return (method, (bool)arguments[^1]!);
        }

        private static EntityHandle Receiver(Case item, bool terminal) => terminal ? item.UnsafeReceiver : item.AlignmentReceiver;
        private static object Self(Case item) => item.SelfValue;

        private void Positive(Case item)
        {
            var proof = Proof(item)!;
            Check(ReferenceEquals(Required(proof, "Root"), item.Root), item.Label + ":actual-root");
            Check(ReferenceEquals(Required(item.Root, "Module"), _core), item.Label + ":corelib-root");
            var definition = Definition(item.Root);
            Check(definition.DeclaringType.FullName == "System.Runtime.Intrinsics.ISimdVector`2" && definition.Name == item.Operation,
                item.Label + ":real-dim-definition");
            Check(Convert.ToHexString(_reader.GetBlobBytes(_reader.GetMethodDefinition((MethodDefinitionHandle)Required(item.Root, "Handle")).Signature))
                == (item.Operation == "LoadAligned" ? "000113000F1301" : "00020113000F1301"), item.Label + ":raw-dim-vars");
            var vector = Required(proof, "Vector");
            Check(ReferenceEquals(Required(Self(item), "Class"), vector), item.Label + ":same-self");
            Check((string)Required(Required(proof, "Guard"), "Name") == (item.Width == 0 ? "ThrowForUnsupportedNumericsVectorBaseType"
                : "ThrowForUnsupportedIntrinsicsVector" + item.Width + "BaseType"), item.Label + ":same-width-helper");
            foreach (bool terminal in new[] { false, true })
            {
                int offset = (int)Required(proof, terminal ? "UnsafeOffset" : "AlignmentOffset");
                int token = (int)Required(proof, terminal ? "UnsafeToken" : "AlignmentToken");
                var actual = Edge(item.Root, offset, token, Receiver(item, terminal), Self(item));
                Check(ReferenceEquals(actual.Method, Required(proof, terminal ? "Guard" : "Alignment")) && actual.Terminal == terminal,
                    item.Label + ":edge:" + terminal);
                Check(offset == (terminal ? item.Operation == "LoadAligned" ? 28 : 29 : 7), item.Label + ":opcode-offset:" + terminal);
            }
            foreach (string field in new[] { "Alignment", "Guard", "Bridge", "Facade", "Getter" })
            {
                var method = Required(proof, field);
                Check(ReferenceEquals(Required(method, "Module"), _core) && (int)Required(method, "Rva") != 0 && !(bool)Required(method, "IsSynthetic"),
                    item.Label + ":real-dependency:" + field);
                Check(ReferenceEquals(method, Required(Proof(item, true)!, field)), item.Label + ":cached-dependency:" + field);
            }
            var bridge = Definition(Required(proof, "Bridge"));
            var callInstruction = bridge.Body.Instructions.Single(i => i.OpCode == OpCodes.Call);
            var spec = _reader.GetMethodSpecification((MethodSpecificationHandle)MetadataTokens.EntityHandle(((Mono.Cecil.MethodReference)callInstruction.Operand).MetadataToken.ToInt32()));
            Check(Convert.ToHexString(_reader.GetBlobBytes(spec.Signature)) == "0A011300", item.Label + ":bridge-owner-var");
            var facade = Definition(Required(proof, "Facade"));
            Check(facade.Body.Instructions[0].OpCode == OpCodes.Call, item.Label + ":guard-first");
            var helperSpec = _reader.GetMethodSpecification((MethodSpecificationHandle)MetadataTokens.EntityHandle(((Mono.Cecil.MethodReference)facade.Body.Instructions[0].Operand).MetadataToken.ToInt32()));
            Check(Convert.ToHexString(_reader.GetBlobBytes(helperSpec.Signature)) == "0A011E00", item.Label + ":facade-method-var");
            var helper = Definition(Required(proof, "Guard"));
            Check(helper.Body.Instructions[2].OpCode == OpCodes.Ldc_I4_S && Convert.ToInt32(helper.Body.Instructions[2].Operand, CultureInfo.InvariantCulture) == 0x43,
                item.Label + ":real-resource");
        }

        private void CheckDenied(Case item, string label)
        {
            Check(Refused(() => Proof(item), label), item.Label + ":proof:" + label);
            Check(Refused(() => Proof(item, true), label), item.Label + ":reference-proof:" + label);
            foreach (bool terminal in new[] { false, true })
            {
                string edge = terminal ? "Unsafe" : "Alignment";
                Check(Refused(() => Edge(item.Root, (int)Required(item.Proof, edge + "Offset"), (int)Required(item.Proof, edge + "Token"), Receiver(item, terminal), Self(item)).Method, label),
                    item.Label + ":edge:" + edge + ":" + label);
            }
        }

        private void Restored(Case item, string label)
        {
            var proof = Proof(item, true)!;
            Check(proof is not null && ReferenceEquals(Required(proof, "Alignment"), Required(item.Proof, "Alignment"))
                && ReferenceEquals(Required(proof, "Guard"), Required(item.Proof, "Guard")), item.Label + ":restored:" + label);
        }

        private void References(Case item)
        {
            foreach (string mutation in ReferenceMutations)
            {
                var handle = _references[Label(item.Operation, item.Width, item.Element, mutation)];
                foreach (string resolver in handle.Kind == HandleKind.MethodSpecification
                    ? new[] { "ResolveMethodSpec", "ResolveCallTarget" } : new[] { "ResolveMemberRefMethod", "ResolveFunctionMemberRef", "ResolveCallTarget" })
                {
                    object token = resolver == "ResolveCallTarget" ? handle : handle.Kind == HandleKind.MethodSpecification
                        ? (object)(MethodSpecificationHandle)handle : (MemberReferenceHandle)handle;
                    object? Resolve() => Invoke(resolver, _app, token, _empty);
                    Restored(item, "reference-before");
                    Check(mutation == "valid" ? ReferenceEquals(Resolve(), item.Root) : Refused(Resolve, mutation), item.Label + ":incoming:" + mutation + ":" + resolver);
                    Restored(item, "reference-after"); _referenceChecks++;
                }
            }
            var appMethod = ((IDictionary)Required(_app, "MethodMap")).Values.Cast<object>().First();
            foreach (bool terminal in new[] { false, true })
            {
                string edge = terminal ? "Unsafe" : "Alignment";
                int offset = (int)Required(item.Proof, edge + "Offset"), token = (int)Required(item.Proof, edge + "Token");
                var receiver = Receiver(item, terminal);
                foreach (int wrongOffset in new[] { -1, 0, offset - 1, offset + 1, 4096 })
                    Check(Refused(() => Edge(item.Root, wrongOffset, token, receiver, Self(item)).Method, "offset"), item.Label + ":scope-offset:" + edge + ":" + wrongOffset);
                Check(Refused(() => Edge(item.Root, offset, token ^ 1, receiver, Self(item)).Method, "token"), item.Label + ":scope-token:" + edge);
                Check(Refused(() => Edge(item.Root, offset, token, default, Self(item)).Method, "receiver-token"), item.Label + ":scope-receiver-token:" + edge);
                Check(Refused(() => Edge(item.Root, offset, token, receiver, null).Method, "receiver-null"), item.Label + ":scope-receiver-null:" + edge);
                Check(Refused(() => Edge(appMethod, offset, token, receiver, Self(item)).Method, "foreign-caller"), item.Label + ":scope-foreign-caller:" + edge);
                var copy = Clone(appMethod);
                Set(copy, "Name", item.Operation); Set(copy, "Context", Required(item.Root, "Context")); Set(copy, "Signature", Required(item.Root, "Signature"));
                Check(Refused(() => Edge(copy, offset, token, receiver, Self(item)).Method, "copied-context"), item.Label + ":scope-copied-context:" + edge);
                var alteredSelf = Clone(Self(item)); Set(alteredSelf, "Class", Clone(Required(alteredSelf, "Class")));
                Check(Refused(() => Edge(item.Root, offset, token, receiver, alteredSelf).Method, "same-mangle-receiver"), item.Label + ":scope-same-mangle-receiver:" + edge);
            }
            var alignmentToken = MetadataTokens.EntityHandle((int)Required(item.Proof, "AlignmentToken"));
            var slot = Invoke("ResolveMemberRefMethod", _core, (MemberReferenceHandle)alignmentToken, Required(item.Root, "Context"))!;
            Check(Refused(() => Invoke("TryResolveSimdAlignmentCall", _core, alignmentToken, Required(item.Root, "Context"), Receiver(item, false), Self(item), slot), "standalone-alignment"),
                item.Label + ":no-global-alignment-privilege");
            Canonical(item);
            Restored(item, "scope");
        }

        private void Canonical(Case item)
        {
            foreach (var code in new[] { PrimitiveTypeCode.Char, PrimitiveTypeCode.Object })
            {
                var element = Activator.CreateInstance(Model("TypeDesc"), true)!;
                Set(element, "Kind", Enum.Parse(Model("TypeKind"), "Primitive")); Set(element, "Primitive", code); Set(element, "IsCanonPlaceholder", true);
                var vector = Invoke("Instantiate", _core, Required(Required(item.Proof, "Vector"), "Handle"), Args(element))!;
                Invoke("EnsureCompleted", vector); var self = StaticInvoke("TypeDesc", "MakeClass", vector);
                var contract = Invoke("Instantiate", _core, Required(Required(item.Root, "DeclaringClass"), "Handle"), Args(self, element))!;
                Invoke("EnsureCompleted", contract);
                var root = Invoke("ResolveMethodHandle", _core, (EntityHandle)(MethodDefinitionHandle)Required(item.Root, "Handle"), Required(contract, "Context"))!;
                Check(Refused(() => Invoke("ResolveUnsupportedAlignedDimProof", root), "canonical-proof"), item.Label + ":canonical-proof:" + code);
                foreach (bool terminal in new[] { false, true })
                {
                    string edge = terminal ? "Unsafe" : "Alignment";
                    Check(Edge(root, (int)Required(item.Proof, edge + "Offset"), (int)Required(item.Proof, edge + "Token"), Receiver(item, terminal), self).Method is null,
                        item.Label + ":canonical-edge-deferred:" + edge + ":" + code);
                }
                var compiler = Activator.CreateInstance(Model("MethodCompiler"), Instance, null,
                    new[] { _compilation, root, Activator.CreateInstance(Model("LiteralPool"), true), null }, CultureInfo.InvariantCulture)!;
                Set(compiler, "SharedTrial", true);
                bool tainted = false;
                try { Model("MethodCompiler").GetMethod("Compile", Instance)!.Invoke(compiler, null); }
                catch (TargetInvocationException error) when (error.InnerException?.GetType().FullName == "Dn2Cpp.SharedBodyTaintException")
                { tainted = error.InnerException.Message.Contains("unsupported-aligned-dim", StringComparison.Ordinal); }
                Check(tainted, item.Label + ":canonical-root-taint-before-emission:" + code);
                _referenceChecks++;
            }
        }

        private void Excluded(List<Case> controls)
        {
            var elements = new List<(string Name, object Type)>();
            foreach (var code in new[] { PrimitiveTypeCode.Boolean, PrimitiveTypeCode.Char, PrimitiveTypeCode.UInt16, PrimitiveTypeCode.Int32,
                PrimitiveTypeCode.Single, PrimitiveTypeCode.Double, PrimitiveTypeCode.Object, PrimitiveTypeCode.String })
                elements.Add((code.ToString(), StaticInvoke("TypeDesc", "MakePrimitive", code)));
            foreach (string name in new[] { "Decimal", "Int128", "UInt128" })
            {
                var handle = _reader.TypeDefinitions.Single(h => _reader.GetString(_reader.GetTypeDefinition(h).Namespace) == "System"
                    && _reader.GetString(_reader.GetTypeDefinition(h).Name) == name);
                elements.Add((name, StaticInvoke("TypeDesc", "MakeClass", ((IDictionary)Required(_core, "ClassMap"))[handle]!)));
            }
            var appReader = (MetadataReader)Required(_app, "Reader");
            foreach (string name in new[] { "Value", "Enum", "Cases" })
            {
                var handle = appReader.TypeDefinitions.Single(h => appReader.GetString(appReader.GetTypeDefinition(h).Name) == name);
                var owner = ((IDictionary)Required(_app, "ClassMap"))[handle]!;
                elements.Add(("user-" + name, StaticInvoke("TypeDesc", "MakeClass", owner)));
                if (name == "Value")
                {
                    var byrefLike = Clone(owner); Set(byrefLike, "IsByRefLike", true);
                    elements.Add(("user-byref-like", StaticInvoke("TypeDesc", "MakeClass", byrefLike)));
                }
            }
            var primitive = elements.Single(e => e.Name == "Int32").Type;
            var nullable = _reader.TypeDefinitions.Single(h => _reader.GetString(_reader.GetTypeDefinition(h).Name) == "Nullable`1");
            var nullableClass = Invoke("Instantiate", _core, nullable, Args(primitive))!; Invoke("EnsureCompleted", nullableClass);
            elements.Add(("nullable", StaticInvoke("TypeDesc", "MakeClass", nullableClass)));
            var generic = appReader.TypeDefinitions.Single(h => appReader.GetString(appReader.GetTypeDefinition(h).Name) == "Box`1");
            var genericClass = Invoke("Instantiate", _app, generic, Args(primitive))!; Invoke("EnsureCompleted", genericClass);
            elements.Add(("generic-value", StaticInvoke("TypeDesc", "MakeClass", genericClass)));
            foreach (string kind in new[] { "ByRef", "Pointer", "SZArray", "GenericVar" })
            {
                var type = Clone(primitive); Set(type, "Kind", Enum.Parse(Model("TypeKind"), kind)); Set(type, "Element", primitive);
                elements.Add((kind, type));
            }
            var canonical = Clone(elements.Single(e => e.Name == "Char").Type); Set(canonical, "IsCanonPlaceholder", true); elements.Add(("canonical-char", canonical));
            var contractHandle = _reader.TypeDefinitions.Single(h => _reader.GetString(_reader.GetTypeDefinition(h).Name) == "ISimdVector`2");
            foreach (int width in new[] { 0, 64, 128, 256, 512 })
            foreach (var (name, element) in elements)
            {
                if (Pairs.Contains((width, name))) continue;
                foreach (string operation in Operations)
                {
                    string label = "excluded:" + width + ":" + name + ":" + operation;
                    object? Attempt()
                    {
                        string vectorName = width == 0 ? "Vector`1" : "Vector" + width + "`1";
                        var vectorHandle = _reader.TypeDefinitions.Single(h => _reader.GetString(_reader.GetTypeDefinition(h).Name) == vectorName
                            && _reader.GetString(_reader.GetTypeDefinition(h).Namespace) == (width == 0 ? "System.Numerics" : "System.Runtime.Intrinsics"));
                        var vector = Invoke("Instantiate", _core, vectorHandle, Args(element))!; Invoke("EnsureCompleted", vector);
                        var self = StaticInvoke("TypeDesc", "MakeClass", vector);
                        var contract = Invoke("Instantiate", _core, contractHandle, Args(self, element))!; Invoke("EnsureCompleted", contract);
                        var root = Invoke("ResolveMethodHandle", _core, (EntityHandle)(MethodDefinitionHandle)Required(controls.First(c => c.Operation == operation).Root, "Handle"), Required(contract, "Context"))!;
                        return Invoke("ResolveUnsupportedAlignedDimProof", root);
                    }
                    Check(Refused(Attempt, label), label); _referenceChecks++;
                }
            }
            foreach (var control in controls) Restored(control, "excluded-shapes");
        }

        private object[] Dependencies(Case item)
        {
            var methods = new HashSet<object> { item.Root };
            foreach (string field in new[] { "Alignment", "Guard", "Bridge", "Facade", "Getter" }) methods.Add(Required(item.Proof, field));
            void ResolveToken(object caller, Mono.Cecil.MethodReference operand)
            {
                var target = Invoke("ResolveMethodHandle", _core, MetadataTokens.EntityHandle(operand.MetadataToken.ToInt32()), Required(caller, "Context"));
                if (target is not null) methods.Add(target);
            }
            var rootDefinition = Definition(item.Root);
            ResolveToken(item.Root, (Mono.Cecil.MethodReference)rootDefinition.Body.Instructions.Single(i => i.Offset == 16).Operand);
            var guard = Required(item.Proof, "Guard");
            ResolveToken(guard, (Mono.Cecil.MethodReference)Definition(guard).Body.Instructions[3].Operand);
            var sink = methods.Single(m => (string)Required(m, "Name") == "ThrowNotSupportedException");
            foreach (var instruction in Definition(sink).Body.Instructions.Where(i => i.OpCode == OpCodes.Call || i.OpCode == OpCodes.Newobj))
                ResolveToken(sink, (Mono.Cecil.MethodReference)instruction.Operand);
            var access = methods.Single(m => (string)Required(m, "Name") == "ThrowAccessViolationException");
            ResolveToken(access, (Mono.Cecil.MethodReference)Definition(access).Body.Instructions[0].Operand);
            var getter = Required(item.Proof, "Getter");
            foreach (var instruction in Definition(getter).Body.Instructions.Where(i => i.OpCode == OpCodes.Call))
                ResolveToken(getter, (Mono.Cecil.MethodReference)instruction.Operand);
            if (item.Width == 0)
            {
                var alignment = Required(item.Proof, "Alignment");
                ResolveToken(alignment, (Mono.Cecil.MethodReference)Definition(alignment).Body.Instructions[0].Operand);
            }
            return methods.ToArray();
        }

        private IEnumerable<(string Label, object Type)> TypeMutations(object original)
        {
            string kind = Required(original, "Kind").ToString()!;
            var canonical = Clone(original); Set(canonical, "IsCanonPlaceholder", true); yield return ("canonical", canonical);
            var wrongKind = Clone(original); Set(wrongKind, "Kind", Enum.Parse(Required(original, "Kind").GetType(), kind == "Primitive" ? "Class" : "Primitive"));
            yield return ("kind", wrongKind);
            if (kind == "Class" && Get(original, "Class") is { } identity)
            {
                var twin = Clone(identity); var type = Clone(original); Set(type, "Class", twin);
                Check(!ReferenceEquals(twin, identity) && (string)Required(twin, "CppName") == (string)Required(identity, "CppName"), "distinct-class-identical-cpp-name");
                Check((string)StaticInvoke("Compilation", "MangleArg", original) == (string)StaticInvoke("Compilation", "MangleArg", type), "distinct-class-identical-mangle");
                yield return ("same-mangle-class", type);
            }
            if (kind == "Primitive")
            {
                var changed = Clone(original);
                var code = (PrimitiveTypeCode)Required(original, "Primitive");
                Set(changed, "Primitive", code == PrimitiveTypeCode.Char ? PrimitiveTypeCode.UInt16 : PrimitiveTypeCode.Char);
                yield return (code == PrimitiveTypeCode.Char ? "char-to-uint16-null-class" : "primitive", changed);
                if (code == PrimitiveTypeCode.Char)
                {
                    Check(Get(original, "Class") is null && Get(changed, "Class") is null, "char-uint16-both-null-class");
                    var falseClass = Clone(original);
                    var decimalHandle = _reader.TypeDefinitions.Single(h => _reader.GetString(_reader.GetTypeDefinition(h).Namespace) == "System"
                        && _reader.GetString(_reader.GetTypeDefinition(h).Name) == "Decimal");
                    Set(falseClass, "Class", ((IDictionary)Required(_core, "ClassMap"))[decimalHandle]);
                    yield return ("char-nonnull-class", falseClass);
                }
            }
            if (Get(original, "Element") is { } element)
                foreach (var nested in TypeMutations(element))
                {
                    var changed = Clone(original); Set(changed, "Element", nested.Type);
                    yield return ("element:" + nested.Label, changed);
                }
        }

        private IEnumerable<(string Label, object Context)> ContextMutations(object original)
        {
            var types = (Array)Required(original, "TypeArgs"); var methods = (Array)Required(original, "MethodArgs");
            foreach (bool type in new[] { true, false })
            {
                var values = type ? types : methods;
                string group = type ? "TypeArgs" : "MethodArgs";
                if (values.Length == 0)
                    yield return (group + ":extra", Context(type ? Args(StaticInvoke("TypeDesc", "MakePrimitive", PrimitiveTypeCode.Char)) : types,
                        type ? methods : Args(StaticInvoke("TypeDesc", "MakePrimitive", PrimitiveTypeCode.Char))));
                else
                {
                    yield return (group + ":empty", Context(type ? Args() : types, type ? methods : Args()));
                    for (int index = 0; index < values.Length; index++)
                    foreach (var mutation in TypeMutations(values.GetValue(index)!))
                    {
                        var changed = (Array)values.Clone(); changed.SetValue(mutation.Type, index);
                        yield return (group + "[" + index + "]:" + mutation.Label, Context(type ? changed : types, type ? methods : changed));
                    }
                }
            }
        }

        private IEnumerable<(string Label, object Signature)> SignatureMutations(object signature)
        {
            var parameters = ((IEnumerable)Required(signature, "ParameterTypes")).Cast<object>().ToArray();
            var result = Required(signature, "ReturnType");
            var immutable = typeof(System.Collections.Immutable.ImmutableArray).GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Single(m => m.Name == "Create" && m.IsGenericMethodDefinition && m.GetParameters() is [var p] && p.ParameterType.IsArray).MakeGenericMethod(Model("TypeDesc"));
            var constructor = signature.GetType().GetConstructors().Single();
            object Make(object returnType, object[] args, string? replace = null, object? replacement = null)
            {
                object Value(string name) => name switch
                {
                    "header" => Required(signature, "Header"), "returnType" => returnType,
                    "requiredParameterCount" => Required(signature, "RequiredParameterCount"), "genericParameterCount" => Required(signature, "GenericParameterCount"),
                    "parameterTypes" => immutable.Invoke(null, new object[] { Args(args) })!, _ => throw new InvalidOperationException("unknown signature field " + name)
                };
                return constructor.Invoke(constructor.GetParameters().Select(p => p.Name == replace ? replacement : Value(p.Name!)).ToArray());
            }
            for (int position = -1; position < parameters.Length; position++)
            foreach (var mutation in TypeMutations(position < 0 ? result : parameters[position]))
            {
                var changed = (object[])parameters.Clone(); if (position >= 0) changed[position] = mutation.Type;
                yield return ((position < 0 ? "return" : "parameter[" + position + "]") + ":" + mutation.Label,
                    Make(position < 0 ? mutation.Type : result, changed));
            }
            yield return ("generic-count", Make(result, parameters, "genericParameterCount", (int)Required(signature, "GenericParameterCount") + 1));
            yield return ("required-count", Make(result, parameters, "requiredParameterCount", (int)Required(signature, "RequiredParameterCount") + 1));
            var header = (SignatureHeader)Required(signature, "Header");
            yield return ("calling-convention", Make(result, parameters, "header", new SignatureHeader((byte)(header.RawValue ^ 0x20))));
        }

        private void Warm(Case item)
        {
            var dependencies = Dependencies(item);
            var owners = new HashSet<object>();
            var appOwner = Required(((IDictionary)Required(_app, "MethodMap")).Values.Cast<object>().First(), "DeclaringClass");
            var pinvoke = ((IDictionary)Required(_core, "MethodMap")).Values.Cast<object>().Select(m => Get(m, "PInvoke")).FirstOrDefault(p => p is not null);
            void Poison(object target, string field, object? replacement, string label)
            {
                var original = Get(target, field); Restored(item, "warm-before:" + label); Set(target, field, replacement);
                try { CheckDenied(item, label); _warm++; }
                finally { Set(target, field, original); }
                Restored(item, "warm-after:" + label);
            }
            foreach (var method in dependencies)
            {
                string label = (string)Required(method, "Name") + ":" + MetadataTokens.GetToken((MethodDefinitionHandle)Required(method, "Handle")).ToString("X8", CultureInfo.InvariantCulture);
                foreach (var mutation in new (string Field, object? Value)[]
                {
                    ("Module", _app), ("DeclaringClass", appOwner), ("Handle", default(MethodDefinitionHandle)), ("Rva", (int)Required(method, "Rva") == 0 ? 1 : 0),
                    ("Name", "Foreign"), ("Attributes", (MethodAttributes)Required(method, "Attributes") ^ MethodAttributes.Static),
                    ("ImplAttributes", MethodImplAttributes.InternalCall), ("IsSynthetic", true),
                }) Poison(method, mutation.Field, mutation.Value, label + ":" + mutation.Field);
                if (pinvoke is not null) Poison(method, "PInvoke", pinvoke, label + ":PInvoke");
                foreach (var mutation in ContextMutations(Required(method, "Context"))) Poison(method, "Context", mutation.Context, label + ":context:" + mutation.Label);
                foreach (var mutation in SignatureMutations(Required(method, "Signature"))) Poison(method, "Signature", mutation.Signature, label + ":signature:" + mutation.Label);
                owners.Add(Required(method, "DeclaringClass"));
            }
            owners.Add(Required(item.Proof, "Vector"));
            foreach (var owner in owners)
            {
                string label = "owner:" + Required(owner, "FullName");
                Poison(owner, "Module", _app, label + ":module");
                Poison(owner, "Handle", default(TypeDefinitionHandle), label + ":handle");
                Poison(owner, "_fullName", "Foreign.Owner", label + ":name");
                Poison(owner, "GenericArity", (int)Required(owner, "GenericArity") + 1, label + ":arity");
                foreach (string field in new[] { "IsValueType", "IsInterface", "IsEnum", "IsByRefLike" })
                    Poison(owner, field, !(bool)Required(owner, field), label + ":" + field);
                foreach (var mutation in ContextMutations(Required(owner, "Context"))) Poison(owner, "Context", mutation.Context, label + ":context:" + mutation.Label);
            }
            Poison(_core, "AssemblyName", "Foreign.CoreLib", "core-module-name");
        }

        private void Raw(Case item)
        {
            var mutations = new List<Mutation>();
            var qualifications = new List<Mutation>();
            var methods = Dependencies(item);
            var definitions = methods.Select(Definition).Distinct().ToArray();
            var root = Definition(item.Root);
            var bridge = Definition(Required(item.Proof, "Bridge"));
            var facade = Definition(Required(item.Proof, "Facade"));
            var guard = Definition(Required(item.Proof, "Guard"));
            var getter = Definition(Required(item.Proof, "Getter"));
            var alignment = Definition(Required(item.Proof, "Alignment"));
            var vector = bridge.DeclaringType;
            var contract = root.DeclaringType;
            void Add(string label, params (int Offset, byte[] Bytes)[] patches) => mutations.Add(new Mutation(label, patches));
            void Byte(string label, int offset, int value) => Add(label, (offset, new[] { (byte)value }));
            void Token(string label, CecilMethod method, Instruction instruction, int token) => Add(label, (Body(method) + instruction.Offset + instruction.OpCode.Size, BitConverter.GetBytes(token)));
            void Op(string label, CecilMethod method, Instruction instruction, byte opcode) => Byte(label, Body(method) + instruction.Offset, opcode);
            StringHandle Named(string name) => _reader.MethodDefinitions.Select(_reader.GetMethodDefinition).First(d => _reader.GetString(d.Name) == name).Name;
            var spare = Enumerable.Range(1, _reader.GetTableRowCount(TableIndex.TypeSpec)).Select(MetadataTokens.TypeSpecificationHandle)
                .Last(h => { int length = _reader.GetBlobBytes(_reader.GetTypeSpecification(h).Signature).Length; return length >= 80 && length < 128; });
            var spareBlob = _reader.GetTypeSpecification(spare).Signature;
            int spareAt = Blob(spareBlob);
            Check(_image[spareAt - 1] < 128, "isolated-blob-single-byte-length");
            void ReplaceBlob(string label, int indexOffset, byte[] bytes)
            {
                Check(bytes.Length <= _reader.GetBlobBytes(spareBlob).Length, "isolated-blob-capacity");
                Add(label, (indexOffset, Index(MetadataTokens.GetHeapOffset(spareBlob), BlobSize)),
                    (spareAt - 1, new[] { (byte)bytes.Length }), (spareAt, bytes));
            }
            void Signature(string label, EntityHandle handle)
            {
                BlobHandle blob; int at;
                if (handle.Kind == HandleKind.MethodDefinition)
                {
                    blob = _reader.GetMethodDefinition((MethodDefinitionHandle)handle).Signature;
                    at = Row(TableIndex.MethodDef, MetadataTokens.GetRowNumber(handle)) + 8 + StringSize;
                }
                else if (handle.Kind == HandleKind.MemberReference)
                {
                    blob = _reader.GetMemberReference((MemberReferenceHandle)handle).Signature;
                    at = Row(TableIndex.MemberRef, MetadataTokens.GetRowNumber(handle)) + _reader.GetTableRowSize(TableIndex.MemberRef) - BlobSize;
                }
                else
                {
                    blob = _reader.GetMethodSpecification((MethodSpecificationHandle)handle).Signature;
                    at = Row(TableIndex.MethodSpec, MetadataTokens.GetRowNumber(handle)) + MethodSize;
                }
                byte[] raw = _reader.GetBlobBytes(blob);
                ReplaceBlob(label + ":trailing", at, raw.Concat(new byte[] { 0 }).ToArray());
                Byte(label + ":header", Blob(blob), raw[0] ^ 0x20);
                for (int i = 0; i + 1 < raw.Length; i++)
                    if (raw[i] is 0x13 or 0x1e)
                    {
                        Byte(label + ":var-kind:" + i, Blob(blob) + i, raw[i] == 0x13 ? 0x1e : 0x13);
                        Byte(label + ":var-index:" + i, Blob(blob) + i + 1, raw[i + 1] == 0 ? 1 : 0);
                    }
                if (handle.Kind != HandleKind.MethodSpecification)
                {
                    int result = (raw[0] & 0x10) != 0 ? 3 : 2;
                    foreach (byte modifier in new byte[] { 0x1f, 0x20 })
                        ReplaceBlob(label + ":modifier:" + modifier, at, raw.Take(result).Concat(new byte[] { modifier, 4 }).Concat(raw.Skip(result)).ToArray());
                    for (int i = 0; i < raw.Length; i++)
                        if (raw[i] == 0x1f)
                        {
                            int tokenBytes = raw[i + 1] < 128 ? 1 : raw[i + 1] < 192 ? 2 : 4;
                            Byte(label + ":required-to-optional:" + i, Blob(blob) + i, 0x20);
                            ReplaceBlob(label + ":required-removed:" + i, at, raw.Take(i).Concat(raw.Skip(i + 1 + tokenBytes)).ToArray());
                            ReplaceBlob(label + ":required-wrong-owner:" + i, at, raw.Take(i + 1).Concat(new byte[] { 4 }).Concat(raw.Skip(i + 1 + tokenBytes)).ToArray());
                        }
                }
            }
            void Parent(string label, MemberReferenceHandle handle)
            {
                var reference = _reader.GetMemberReference(handle);
                if (reference.Parent.Kind != HandleKind.TypeSpecification) throw new InvalidOperationException("expected exact generic parent");
                var original = _reader.GetBlobBytes(_reader.GetTypeSpecification((TypeSpecificationHandle)reference.Parent).Signature);
                var variants = new List<(string Name, byte[] Bytes)> { ("trailing", original.Concat(new byte[] { 0 }).ToArray()),
                    ("modopt", new byte[] { 0x20, 4 }.Concat(original).ToArray()), ("modreq", new byte[] { 0x1f, 4 }.Concat(original).ToArray()) };
                var marker = (byte[])original.Clone(); marker[1] = original[1] == 0x12 ? (byte)0x11 : (byte)0x12; variants.Add(("class-value", marker));
                for (int i = 0; i + 1 < original.Length; i++)
                    if (original[i] is 0x13 or 0x1e)
                    {
                        var variable = (byte[])original.Clone(); variable[i] = original[i] == 0x13 ? (byte)0x1e : (byte)0x13; variants.Add(("var-kind:" + i, variable));
                        var index = (byte[])original.Clone(); index[i + 1] = original[i + 1] == 0 ? (byte)1 : (byte)0; variants.Add(("var-index:" + i, index));
                    }
                int parentSize = _reader.GetTableRowSize(TableIndex.MemberRef) - StringSize - BlobSize;
                foreach (var variant in variants)
                    Add(label + ":parent:" + variant.Name,
                        (Row(TableIndex.MemberRef, MetadataTokens.GetRowNumber(handle)), Index((MetadataTokens.GetRowNumber(spare) << 3) | 4, parentSize)),
                        (spareAt - 1, new[] { (byte)variant.Bytes.Length }), (spareAt, variant.Bytes));
            }
            void MethodAuthority(CecilMethod method)
            {
                string label = "method:" + method.FullName;
                int row = Row(TableIndex.MethodDef, (int)method.MetadataToken.RID);
                Byte(label + ":static", row + 6, _image[row + 6] ^ 0x10);
                Byte(label + ":pinvoke", row + 7, _image[row + 7] ^ 0x20);
                Byte(label + ":impl", row + 4, _image[row + 4] ^ 0x20);
                Add(label + ":rva", (row, BitConverter.GetBytes(method.RVA == 0 ? 1 : 0)));
                Add(label + ":name", (row + 8, Index(MetadataTokens.GetHeapOffset(Named("ToString")), StringSize)));
                Signature(label + ":signature", MetadataTokens.EntityHandle(method.MetadataToken.ToInt32()));
                var definition = _reader.GetMethodDefinition((MethodDefinitionHandle)MetadataTokens.EntityHandle(method.MetadataToken.ToInt32()));
                foreach (var generic in definition.GetGenericParameters())
                {
                    int parameter = Row(TableIndex.GenericParam, MetadataTokens.GetRowNumber(generic));
                    Byte(label + ":generic-flags", parameter + 2, _image[parameter + 2] ^ 1);
                    Byte(label + ":generic-index", parameter, _image[parameter] + 1);
                }
                foreach (var parameter in definition.GetParameters())
                {
                    int at = Row(TableIndex.Param, MetadataTokens.GetRowNumber(parameter));
                    qualifications.Add(new Mutation(label + ":param-flags:" + MetadataTokens.GetRowNumber(parameter),
                        new[] { (at, new[] { (byte)(_image[at] ^ 1) }) }));
                }
            }
            foreach (var method in definitions) MethodAuthority(method);
            void BodySections(CecilMethod method)
            {
                int at = Rva(method.RVA), body = Body(method), length = method.Body.CodeSize;
                int end = (body + length + 3) & ~3;
                byte[] section = new byte[16]; section[0] = 1; section[1] = 16; section[8] = 1; section[9] = 1; section[11] = 1;
                BitConverter.GetBytes(_cecil.MainModule.GetType("System.Exception").MetadataToken.ToInt32()).CopyTo(section, 12);
                if ((_image[at] & 3) == 3)
                {
                    Add("eh:" + method.FullName, (at, new[] { (byte)(_image[at] | 8) }), (end, section));
                    Add("locals:" + method.FullName, (at + 8, BitConverter.GetBytes(0x11000001)));
                }
                else
                {
                    // Retain the RVA and exact IL while adding a real fat-header
                    // section. A cached RVA mismatch cannot mask the EH check.
                    var bytes = new byte[12 + length]; bytes[0] = 0x0b; bytes[1] = 0x30;
                    bytes[2] = 8; BitConverter.GetBytes(length).CopyTo(bytes, 4); _image.Skip(body).Take(length).ToArray().CopyTo(bytes, 12);
                    int changedEnd = (at + bytes.Length + 3) & ~3;
                    Add("eh:" + method.FullName, (at, bytes), (changedEnd, section));
                    var localBody = (byte[])bytes.Clone(); localBody[0] = 3; BitConverter.GetBytes(0x11000001).CopyTo(localBody, 8);
                    Add("locals:" + method.FullName, (at, localBody));
                }
            }
            foreach (var method in new[] { root, bridge, facade, guard, getter, alignment }) BodySections(method);
            foreach (var owner in definitions.Select(m => m.DeclaringType).Append(vector).Distinct())
            {
                int at = Row(TableIndex.TypeDef, (int)owner.MetadataToken.RID);
                bool structural = owner == vector || owner == contract || owner == facade.DeclaringType || owner == guard.DeclaringType;
                if (structural) Byte("owner-flags:" + owner.FullName, at, _image[at] ^ 0x20);
                var ownerDefinition = _reader.GetTypeDefinition((TypeDefinitionHandle)MetadataTokens.EntityHandle(owner.MetadataToken.ToInt32()));
                foreach (var generic in ownerDefinition.GetGenericParameters())
                {
                    int parameter = Row(TableIndex.GenericParam, MetadataTokens.GetRowNumber(generic));
                    Byte("owner-generic-flags:" + owner.FullName + ":" + MetadataTokens.GetRowNumber(generic), parameter + 2, _image[parameter + 2] ^ 1);
                    Byte("owner-generic-index:" + owner.FullName + ":" + MetadataTokens.GetRowNumber(generic), parameter, _image[parameter] + 1);
                    foreach (var constraint in _reader.GetGenericParameter(generic).GetConstraints())
                    {
                        int constraintRow = Row(TableIndex.GenericParamConstraint, MetadataTokens.GetRowNumber(constraint));
                        Byte("owner-constraint-parent:" + owner.FullName, constraintRow, _image[constraintRow] ^ 1);
                        var constraintType = _reader.GetGenericParameterConstraint(constraint).Type;
                        if (constraintType.Kind == HandleKind.TypeSpecification)
                        {
                            int raw = Blob(_reader.GetTypeSpecification((TypeSpecificationHandle)constraintType).Signature);
                            Byte("owner-constraint-marker:" + owner.FullName, raw + 1, 0x11);
                        }
                    }
                }
                var extra = owner.Methods.FirstOrDefault(m => !definitions.Contains(m) && m.Name != ".cctor");
                if (structural && extra is not null) Add("cctor:" + owner.FullName, (Row(TableIndex.MethodDef, (int)extra.MetadataToken.RID) + 8,
                    Index(MetadataTokens.GetHeapOffset(Named(".cctor")), StringSize)));
            }
            foreach (var instruction in root.Body.Instructions)
            {
                if (instruction.OpCode == OpCodes.Constrained)
                {
                    var handle = (TypeSpecificationHandle)MetadataTokens.EntityHandle(((Mono.Cecil.TypeReference)instruction.Operand).MetadataToken.ToInt32());
                    var blob = _reader.GetTypeSpecification(handle).Signature;
                    Byte("dim-constrained-var:" + instruction.Offset, Blob(blob), 0x1e);
                    Byte("dim-constrained-index:" + instruction.Offset, Blob(blob) + 1, 1);
                }
                else if (instruction.OpCode == OpCodes.Call)
                {
                    var handle = MetadataTokens.EntityHandle(((Mono.Cecil.MethodReference)instruction.Operand).MetadataToken.ToInt32());
                    if (handle.Kind == HandleKind.MemberReference)
                    {
                        Signature("dim-call:" + instruction.Offset, handle); Parent("dim-call:" + instruction.Offset, (MemberReferenceHandle)handle);
                    }
                    Op("dim-callvirt:" + instruction.Offset, root, instruction, 0x6f);
                }
                else if (instruction.OpCode == OpCodes.Brfalse_S)
                {
                    Op("dim-branch-direction", root, instruction, 0x2d);
                    Byte("dim-branch-target", Body(root) + instruction.Offset + 1, 0);
                }
                else if (instruction.OpCode == OpCodes.Rem_Un) Op("dim-signed-remainder", root, instruction, 0x5d);
                else if (instruction.OpCode == OpCodes.Conv_U) Op("dim-signed-conversion", root, instruction, 0xd3);
                else if (instruction.OpCode == OpCodes.Ldarg_0 || instruction.OpCode == OpCodes.Ldarg_1)
                    Op("dim-argument:" + instruction.Offset, root, instruction, instruction.OpCode == OpCodes.Ldarg_0 ? (byte)3 : (byte)2);
                else if (instruction.OpCode == OpCodes.Ret) Op("dim-tail", root, instruction, 0x00);
            }
            int rootHeader = Rva(root.RVA);
            if ((_image[rootHeader] & 3) == 2)
                Byte("dim-extra-tail", rootHeader, _image[rootHeader] + 4);
            else
            {
                Add("dim-local-signature", (rootHeader + 8, BitConverter.GetBytes(0x11000001)));
                Add("dim-extra-tail", (rootHeader + 4, BitConverter.GetBytes(root.Body.CodeSize + 1)));
            }
            foreach (var method in new[] { bridge, guard, getter, alignment })
            {
                if (method == getter)
                {
                    foreach (var instruction in getter.Body.Instructions.Where(i => i.OpCode == OpCodes.Ldtoken))
                        Token("whitelist-type:" + instruction.Offset, getter, instruction, _cecil.MainModule.GetType("System.Char").MetadataToken.ToInt32());
                    var branch = getter.Body.Instructions.First(i => i.OpCode == OpCodes.Brtrue_S);
                    Op("whitelist-branch", getter, branch, 0x2c);
                }
                else
                    foreach (var instruction in method.Body.Instructions)
                    {
                        if (instruction.OpCode == OpCodes.Call)
                        {
                            Op("dependency-callvirt:" + method.FullName + ":" + instruction.Offset, method, instruction, 0x6f);
                            var handle = MetadataTokens.EntityHandle(((Mono.Cecil.MethodReference)instruction.Operand).MetadataToken.ToInt32());
                            if (handle.Kind is HandleKind.MemberReference or HandleKind.MethodSpecification)
                            {
                                Signature("dependency-call:" + method.FullName + ":" + instruction.Offset, handle);
                                if (handle.Kind == HandleKind.MemberReference) Parent("dependency-call:" + method.FullName + ":" + instruction.Offset, (MemberReferenceHandle)handle);
                            }
                        }
                        else if (instruction.OpCode == OpCodes.Ldarg_0 || instruction.OpCode == OpCodes.Ldarg_1)
                            Op("bridge-argument:" + method.FullName + ":" + instruction.Offset, method, instruction, instruction.OpCode == OpCodes.Ldarg_0 ? (byte)3 : (byte)2);
                        else if (instruction.OpCode == OpCodes.Brtrue_S)
                        {
                            Op("helper-branch-direction", method, instruction, 0x2c);
                            Byte("helper-branch-target", Body(method) + instruction.Offset + 1, 0);
                        }
                        else if (instruction.OpCode == OpCodes.Ldc_I4_S)
                            Byte("dependency-constant:" + method.FullName, Body(method) + instruction.Offset + 1, Convert.ToByte(instruction.Operand, CultureInfo.InvariantCulture) ^ 1);
                        else if (instruction.OpCode == OpCodes.Ldc_I4_8) Op("alignment-constant", method, instruction, 0x1d);
                        else if (instruction.OpCode == OpCodes.Ret) Op("dependency-return:" + method.FullName, method, instruction, 0x00);
                    }
            }
            var facadeCall = facade.Body.Instructions[0];
            Op("facade-guard-not-first", facade, facadeCall, 0x00);
            var facadeSpec = (MethodSpecificationHandle)MetadataTokens.EntityHandle(((Mono.Cecil.MethodReference)facadeCall.Operand).MetadataToken.ToInt32());
            Signature("facade-guard-spec", facadeSpec);
            var differentHelper = _cecil.MainModule.GetType("System.ThrowHelper").Methods.Single(m => m.Name == (item.Width == 64
                ? "ThrowForUnsupportedIntrinsicsVector128BaseType" : "ThrowForUnsupportedIntrinsicsVector64BaseType"));
            Add("facade-wrong-width-helper", (Row(TableIndex.MethodSpec, MetadataTokens.GetRowNumber(facadeSpec)), Index((int)differentHelper.MetadataToken.RID << 1, MethodSize)));
            var otherWidth = _cecil.MainModule.GetType(VectorAlignmentFixture.Owner(item.Width == 64 ? 128 : 64));
            var foreignFacade = otherWidth.Methods.Single(m => m.Name == facade.Name && m.HasGenericParameters && m.Parameters.Count == facade.Parameters.Count);
            var bridgeSpec = (MethodSpecificationHandle)MetadataTokens.EntityHandle(((Mono.Cecil.MethodReference)bridge.Body.Instructions.Single(i => i.OpCode == OpCodes.Call).Operand).MetadataToken.ToInt32());
            Add("bridge-wrong-width-facade", (Row(TableIndex.MethodSpec, MetadataTokens.GetRowNumber(bridgeSpec)), Index((int)foreignFacade.MetadataToken.RID << 1, MethodSize)));
            var vectorHandle = (TypeDefinitionHandle)MetadataTokens.EntityHandle(vector.MetadataToken.ToInt32());
            var rows = _reader.GetTypeDefinition(vectorHandle).GetMethodImplementations().ToArray();
            var noOffset = rows.Single(h => _reader.GetMethodImplementation(h).MethodBody == MetadataTokens.EntityHandle(bridge.MetadataToken.ToInt32()));
            var offsetBridge = vector.Methods.Single(m => m.Name.EndsWith("." + (item.Operation == "LoadAligned" ? "LoadUnsafe" : "StoreUnsafe"), StringComparison.Ordinal)
                && m.Parameters.Count == bridge.Parameters.Count + 1);
            qualifications.Add(new Mutation("unreachable-offset-body", new[] { (Body(offsetBridge), new byte[] { 0 }) }));
            qualifications.Add(new Mutation("unreachable-facade-suffix", new[] { (Body(facade) + facade.Body.Instructions[1].Offset, new byte[] { 0 }) }));
            var offset = rows.Single(h => _reader.GetMethodImplementation(h).MethodBody == MetadataTokens.EntityHandle(offsetBridge.MetadataToken.ToInt32()));
            var other = rows.First(h => h != noOffset && h != offset);
            int selectedAt = Row(TableIndex.MethodImpl, MetadataTokens.GetRowNumber(noOffset));
            int otherAt = Row(TableIndex.MethodImpl, MetadataTokens.GetRowNumber(other));
            int offsetAt = Row(TableIndex.MethodImpl, MetadataTokens.GetRowNumber(offset));
            int rowSize = _reader.GetTableRowSize(TableIndex.MethodImpl);
            Add("methodimpl-duplicate-memberref", (otherAt, _image.Skip(selectedAt).Take(rowSize).ToArray()));
            var implementation = _reader.GetMethodImplementation(noOffset);
            var declaration = (MemberReferenceHandle)implementation.MethodDeclaration;
            Parent("methodimpl-no-offset", declaration); Signature("methodimpl-no-offset", declaration);
            var slot = contract.Methods.Single(m => m.Name == (item.Operation == "LoadAligned" ? "LoadUnsafe" : "StoreUnsafe") && m.Parameters.Count == bridge.Parameters.Count);
            Add("methodimpl-competing-methoddef", (otherAt + TypeSize + MethodSize, Index((int)slot.MetadataToken.RID << 1, MethodSize)));
            Add("methodimpl-competing-methoddef-renamed", (otherAt + TypeSize + MethodSize, Index((int)slot.MetadataToken.RID << 1, MethodSize)),
                (Row(TableIndex.MethodDef, (int)slot.MetadataToken.RID) + 8, Index(MetadataTokens.GetHeapOffset(Named("ToString")), StringSize)));
            Add("methodimpl-competing-methoddef-signature", (otherAt + TypeSize + MethodSize, Index((int)slot.MetadataToken.RID << 1, MethodSize)),
                (Blob(_reader.GetMethodDefinition((MethodDefinitionHandle)MetadataTokens.EntityHandle(slot.MetadataToken.ToInt32())).Signature), new byte[] { 0x20 }));
            Add("methodimpl-nil", (otherAt + TypeSize + MethodSize, new byte[MethodSize]));
            Add("methodimpl-nil-body", (selectedAt + TypeSize, new byte[MethodSize]));
            Add("methodimpl-offset-body-as-no-offset", (selectedAt + TypeSize, Index((int)offsetBridge.MetadataToken.RID << 1, MethodSize)));
            Add("methodimpl-no-offset-body-as-offset", (offsetAt + TypeSize, Index((int)bridge.MetadataToken.RID << 1, MethodSize)));
            var offsetDeclaration = (MemberReferenceHandle)_reader.GetMethodImplementation(offset).MethodDeclaration;
            Parent("methodimpl-offset", offsetDeclaration); Signature("methodimpl-offset", offsetDeclaration);
            var reference = _reader.GetMemberReference(offsetDeclaration);
            Add("methodimpl-offset-unknown-name", (Row(TableIndex.MemberRef, MetadataTokens.GetRowNumber(offsetDeclaration))
                + _reader.GetTableRowSize(TableIndex.MemberRef) - StringSize - BlobSize, Index(MetadataTokens.GetHeapOffset(Named("ToString")), StringSize)));
            var resource = _cecil.MainModule.GetType("System.ExceptionResource").Fields.Single(f => f.Name == "Arg_TypeNotSupported");
            var resourceHandle = (FieldDefinitionHandle)MetadataTokens.EntityHandle(resource.MetadataToken.ToInt32());
            var constant = _reader.GetConstant(_reader.GetFieldDefinition(resourceHandle).GetDefaultValue());
            Byte("resource-value", Blob(constant.Value), 0x42);
            Byte("resource-field-flags", Row(TableIndex.Field, (int)resource.MetadataToken.RID), (byte)resource.Attributes ^ 0x10);
            var byReader = (IDictionary)Required(_compilation, "_byReader");
            foreach (var mutation in mutations)
            {
                Restored(item, "raw-before:" + mutation.Name);
                var image = (byte[])_image.Clone(); foreach (var patch in mutation.Patches) patch.Bytes.CopyTo(image, patch.Offset);
                using var pe = new PEReader(new MemoryStream(image, writable: false)); var reader = pe.GetMetadataReader();
                byReader.Add(reader, _core); Set(_core, "Reader", reader); Set(_core, "PE", pe);
                try { CheckDenied(item, mutation.Name); _raw++; }
                finally { Set(_core, "Reader", _reader); Set(_core, "PE", _pe); byReader.Remove(reader); }
                Restored(item, "raw-after:" + mutation.Name);
            }
            foreach (var mutation in qualifications)
            {
                var image = (byte[])_image.Clone(); foreach (var patch in mutation.Patches) patch.Bytes.CopyTo(image, patch.Offset);
                using var pe = new PEReader(new MemoryStream(image, writable: false)); var reader = pe.GetMetadataReader();
                byReader.Add(reader, _core); Set(_core, "Reader", reader); Set(_core, "PE", pe);
                try { Restored(item, "semantically-irrelevant:" + mutation.Name); _qualifications++; }
                finally { Set(_core, "Reader", _reader); Set(_core, "PE", _pe); byReader.Remove(reader); }
                Restored(item, "qualification-after:" + mutation.Name);
            }
        }

        public void Dispose() { _cecil.Dispose(); _fixture.Dispose(); }
    }
}
