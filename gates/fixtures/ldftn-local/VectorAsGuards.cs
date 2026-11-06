using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

internal static class VectorAsGuards
{
    public static bool Run(string[] args)
    {
        if (args.Length != 4 || args[0] != "--vector-as-guards") return false;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        const BindingFlags instance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        const BindingFlags statics = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        var cases = new List<(string Source, string Destination, string Route, string Mutation)>();
        var shapes = new[] { (Source: "decimal", Destination: "int"), (Source: "int", Destination: "decimal"), (Source: "decimal", Destination: "struct") };
        foreach (var shape in shapes)
        foreach (var route in new[] { "direct", "address", "calli" })
        foreach (var mutation in new[] { "valid", "valid-mvar", "valid-var" }.Concat(VectorAsFixture.Mutations))
            cases.Add((shape.Source, shape.Destination, route, mutation));
        foreach (var element in VectorAsFixture.Elements)
        foreach (var shape in new[] { (element, "double"), ("double", element), (element, element) })
        foreach (var route in new[] { "direct", "address", "calli" })
            cases.Add((shape.Item1, shape.Item2, route, "valid"));
        foreach (var source in VectorAsFixture.NumericElements)
        foreach (var destination in VectorAsFixture.NumericElements)
            cases.Add((source, destination, "direct", "valid"));
        VectorAsFixture.Build(args[3], args[2], cases);
        var production = Assembly.LoadFrom(Path.GetFullPath(args[1]));
        Type Model(string name) => production.GetType("Dn2Cpp." + name, true)!;
        object Get(object value, string name) => value.GetType().GetField(name, instance) is { } field ? field.GetValue(value)! : value.GetType().GetProperty(name, instance)!.GetValue(value)!;
        void Set(object value, string name, object? next)
        {
            if (value.GetType().GetField(name, instance) is { } field) field.SetValue(value, next);
            else value.GetType().GetProperty(name, instance)!.SetValue(value, next);
        }
        var options = Activator.CreateInstance(Model("TranspileOptions"))!;
        Set(options, "Input", Path.GetFullPath(args[3])); Set(options, "UseILDiet", false); Set(options, "SharedGenerics", false);
        Set(options, "AutoRef", false); Set(options, "IsaSurfaceDump", "predicate-guard-only");
        object compilation;
        try { compilation = Model("Compilation").GetMethod("Create", statics)!.Invoke(null, new object[] { new[] { Path.GetFullPath(args[3]), Path.GetFullPath(args[2]) }, options })!; }
        catch (TargetInvocationException error) when (error.InnerException is not null) { throw error.InnerException; }
        var modules = ((IEnumerable)Get(compilation, "Modules")).Cast<object>().ToArray();
        var core = modules.Single(m => (string)Get(m, "AssemblyName") == "System.Private.CoreLib");
        var app = modules.Single(m => !ReferenceEquals(m, core) && (string)Get(m, "Path") == Path.GetFullPath(args[3]));
        var reader = (MetadataReader)Get(app, "Reader");
        var predicate = Model("CoreIntrinsics").GetMethod("IsVectorAs", statics)!;
        var guarded = Model("CoreIntrinsics").GetMethod("IsGuardedVectorAs", statics)!;
        var resolve = Model("Compilation").GetMethod("TryResolveGuardedVectorAsSpec", instance)!;
        var resolveSpec = Model("Compilation").GetMethod("ResolveMethodSpec", instance)!;
        var resolveCall = Model("Compilation").GetMethod("ResolveCallTarget", instance)!;
        var resolveReference = Model("Compilation").GetMethod("ResolveMemberRefMethod", instance)!;
        var resolveFunction = Model("Compilation").GetMethod("ResolveFunctionMemberRef", instance)!;
        var instantiate = Model("Compilation").GetMethod("InstantiateMethodOnClass", instance)!;
        var resolveGuard = Model("Compilation").GetMethod("ResolveGuardedVectorAsGuards", instance)!;
        var empty = Model("GenericContext").GetField("Empty", statics)!.GetValue(null)!;
        var emptyArguments = Array.CreateInstance(Model("TypeDesc"), 0);
        object Context(Array values, bool type) => Activator.CreateInstance(Model("GenericContext"), new object[] { type ? values : emptyArguments, type ? emptyArguments : values })!;
        int checks = 0;
        void Check(bool result, string label)
        {
            if (!result) throw new InvalidOperationException("vector As guard: " + label);
            checks++;
        }
        bool Accepted(object? value) => value is not null && (bool)predicate.Invoke(null, new[] { value })!;
        bool Guarded(object value) => (bool)guarded.Invoke(null, new[] { value })!;
        object? Invoke(System.Reflection.MethodInfo method, object module, EntityHandle handle, object context)
        {
            object typed = method == resolveCall ? handle : handle.Kind == HandleKind.MethodSpecification
                ? (object)(MethodSpecificationHandle)handle : (MemberReferenceHandle)handle;
            return method.Invoke(compilation, new[] { module, typed, context });
        }
        bool Refused(System.Reflection.MethodInfo method, object module, EntityHandle token, object context)
        {
            try { return Invoke(method, module, token, context) is null; }
            catch (TargetInvocationException error) when (error.InnerException is NotSupportedException) { return true; }
        }
        using var serialized = Mono.Cecil.AssemblyDefinition.ReadAssembly(args[3]);
        var methods = serialized.MainModule.Types.SelectMany(t => t.Methods).Where(m => m.Name.StartsWith("Case", StringComparison.Ordinal)).ToArray();
        Check(methods.Length == cases.Count, "serialized-case-count");
        EntityHandle Token(Mono.Cecil.MethodDefinition method)
        {
            var target = method.Body.Instructions.Select(i => i.Operand).OfType<Mono.Cecil.MethodReference>().Single(m => m.Name == "As");
            return MetadataTokens.EntityHandle(target.MetadataToken.ToInt32());
        }
        var good = new Dictionary<(string Source, string Destination), (EntityHandle Token, object Method, Array Arguments)>();
        for (int i = 0; i < cases.Count; i++)
        {
            var item = cases[i]; if (item.Mutation != "valid" || item.Route != "direct") continue;
            var token = Token(methods[i]);
            var method = Invoke(resolveSpec, app, token, empty)!;
            Check(Accepted(method), "initial:" + item.Source + ":" + item.Destination);
            var arguments = (Array)Get(Get(method, "Context"), "MethodArgs");
            good.TryAdd((item.Source, item.Destination), (token, method, arguments));
            bool shouldGuard = VectorAsFixture.Elements.Contains(item.Source) || VectorAsFixture.Elements.Contains(item.Destination);
            Check(Guarded(method) == shouldGuard, "bounded-guard-pair");
            var special = Invoke(resolve, app, token, empty);
            Check(shouldGuard ? ReferenceEquals(special, method) : special is null, "numeric-pair-route-preserved");
        }
        for (int i = 0; i < cases.Count; i++)
        {
            var item = cases[i]; var token = Token(methods[i]); var control = good[(item.Source, item.Destination)];
            var contexts = new[] { empty, Context(control.Arguments, false), Context(control.Arguments, true) };
            if (!Guarded(control.Method)) continue;
            foreach (var context in contexts)
            foreach (var resolver in token.Kind == HandleKind.MethodSpecification ? new[] { resolve, resolveSpec, resolveCall } : new[] { resolveReference, resolveFunction, resolveCall })
            {
                Check(ReferenceEquals(Invoke(resolve, app, control.Token, empty), control.Method), "good-before");
                bool expected = item.Mutation == "valid" || item.Mutation == "valid-mvar" && ReferenceEquals(context, contexts[1])
                    || item.Mutation == "valid-var" && ReferenceEquals(context, contexts[2]);
                if (expected) Check(Accepted(Invoke(resolver, app, token, context)), item.Route + ":" + item.Mutation + ":accept");
                else Check(Refused(resolver, app, token, context), item.Route + ":" + item.Mutation + ":reject");
                Check(ReferenceEquals(Invoke(resolve, app, control.Token, empty), control.Method) && Accepted(control.Method), "good-after");
            }
        }
        bool ThrowsUnsupported(System.Reflection.MethodInfo resolver, EntityHandle target, object context)
        {
            try { Invoke(resolver, app, target, context); }
            catch (TargetInvocationException error) when (error.InnerException is NotSupportedException) { return true; }
            return false;
        }
        var appPe = (PEReader)Get(app, "PE");
        var appImage = File.ReadAllBytes(args[3]);
        var byReader = (IDictionary)Get(compilation, "_byReader");
        int AppBlobOffset(BlobHandle blob)
        {
            int at = appPe.PEHeaders.MetadataStartOffset + reader.GetHeapMetadataOffset(HeapIndex.Blob) + MetadataTokens.GetHeapOffset(blob);
            return at + (appImage[at] < 0x80 ? 1 : appImage[at] < 0xc0 ? 2 : 4);
        }
        foreach (var shape in shapes.Take(2))
        foreach (var route in new[] { "direct", "address", "calli" })
        foreach (var mode in new[] { "valid-var", "valid-mvar" })
        {
            var control = good[shape];
            int index = cases.FindIndex(c => c.Source == shape.Source && c.Destination == shape.Destination && c.Route == route && c.Mutation == mode);
            var target = Token(methods[index]);
            var numericIndex = shape.Source == "int" ? 0 : 1;
            var numeric = control.Arguments.GetValue(numericIndex)!;
            var canonical = Activator.CreateInstance(Model("TypeDesc"))!;
            Set(canonical, "Kind", Get(numeric, "Kind")); Set(canonical, "Primitive", Get(numeric, "Primitive"));
            Set(canonical, "IsCanonPlaceholder", true); Set(canonical, "CanonAnyIndex", -1);
            var canonicalArguments = (Array)control.Arguments.Clone(); canonicalArguments.SetValue(canonical, numericIndex);
            var context = Context(canonicalArguments, mode == "valid-var");
            int before = (int)Get(compilation, "_methodInstanceCount");
            Check(Invoke(resolveCall, app, target, context) is null, "canonical-scan-deferred:" + mode + ":" + route);
            Check((int)Get(compilation, "_methodInstanceCount") == before, "canonical-scan-no-callable");
            Check(ThrowsUnsupported(resolve, target, context), "canonical-strict-specialized-reject");
            Check(ThrowsUnsupported(resolveSpec, target, context), "canonical-strict-methodspec-reject");
            bool refused = false;
            try { instantiate.Invoke(compilation, new[] { Get(control.Method, "DeclaringClass"), core, Get(control.Method, "Handle"), canonicalArguments }); }
            catch (TargetInvocationException error) when (error.InnerException is NotSupportedException) { refused = true; }
            Check(refused, "canonical-strict-instantiation-reject");
            Set(canonical, "CanonAnyIndex", 0);
            Check(ThrowsUnsupported(resolveCall, target, context), "canonical-any-not-width-reject");
            Set(canonical, "CanonAnyIndex", -1);
            var primitive = Get(canonical, "Primitive");
            Set(canonical, "Primitive", Enum.Parse(primitive.GetType(), "Object"));
            Check(ThrowsUnsupported(resolveCall, target, context), "canonical-reference-not-width-reject");
            Set(canonical, "Primitive", primitive);
            var partner = canonicalArguments.GetValue(1 - numericIndex)!;
            var invalidPartner = Activator.CreateInstance(Model("TypeDesc"))!;
            Set(invalidPartner, "Kind", Get(numeric, "Kind"));
            Set(invalidPartner, "Primitive", Enum.Parse(primitive.GetType(), "Boolean"));
            canonicalArguments.SetValue(invalidPartner, 1 - numericIndex);
            Check(ThrowsUnsupported(resolveCall, target, context), "canonical-invalid-noncanonical-partner");
            canonicalArguments.SetValue(partner, 1 - numericIndex);
            var secondCanonical = Activator.CreateInstance(Model("TypeDesc"))!;
            Set(secondCanonical, "Kind", Get(numeric, "Kind"));
            Set(secondCanonical, "Primitive", Enum.Parse(primitive.GetType(), "Double"));
            Set(secondCanonical, "IsCanonPlaceholder", true); Set(secondCanonical, "CanonAnyIndex", -1);
            canonicalArguments.SetValue(secondCanonical, 1 - numericIndex);
            int bothBefore = (int)Get(compilation, "_methodInstanceCount");
            Check(Invoke(resolveCall, app, target, context) is null, "both-canonical-scan-deferred:" + mode + ":" + route);
            Check((int)Get(compilation, "_methodInstanceCount") == bothBefore, "both-canonical-scan-no-callable");
            Check(ThrowsUnsupported(resolve, target, context), "both-canonical-strict-specialized-reject");
            Check(ThrowsUnsupported(resolveSpec, target, context), "both-canonical-strict-methodspec-reject");
            refused = false;
            try { instantiate.Invoke(compilation, new[] { Get(control.Method, "DeclaringClass"), core, Get(control.Method, "Handle"), canonicalArguments }); }
            catch (TargetInvocationException error) when (error.InnerException is NotSupportedException) { refused = true; }
            Check(refused, "both-canonical-strict-instantiation-reject");
            canonicalArguments.SetValue(partner, 1 - numericIndex);
            var specification = reader.GetMethodSpecification((MethodSpecificationHandle)target);
            var reference = reader.GetMemberReference((MemberReferenceHandle)specification.Method);
            var referenceRaw = reader.GetBlobBytes(reference.Signature);
            var mutations = new[]
            {
                (Offset: AppBlobOffset(specification.Signature), Value: (byte)0x09),
                (Offset: AppBlobOffset(specification.Signature) + 1, Value: (byte)1),
                (Offset: AppBlobOffset(specification.Signature) + 2, Value: (byte)0x10),
                (Offset: AppBlobOffset(reference.Signature), Value: (byte)0x30),
                (Offset: AppBlobOffset(reference.Signature) + referenceRaw.Length - 1, Value: (byte)1),
            };
            foreach (var mutation in mutations)
            {
                var changed = (byte[])appImage.Clone(); changed[mutation.Offset] = mutation.Value;
                using var modified = new PEReader(new MemoryStream(changed, writable: false));
                var modifiedReader = modified.GetMetadataReader(); byReader.Add(modifiedReader, app);
                Set(app, "PE", modified); Set(app, "Reader", modifiedReader);
                try { Check(ThrowsUnsupported(resolveCall, target, context), "canonical-malformed-raw-not-deferred"); }
                finally { Set(app, "PE", appPe); Set(app, "Reader", reader); byReader.Remove(modifiedReader); }
                Check(Invoke(resolveCall, app, target, context) is null, "canonical-raw-restored-defer");
            }
            Check(ReferenceEquals(Invoke(resolve, app, control.Token, empty), control.Method), "canonical-restored-concrete-route");
        }
        foreach (var shape in shapes)
        {
            var control = good[shape]; var method = control.Method;
            object Instantiate(Array values) => instantiate.Invoke(compilation, new[] { Get(method, "DeclaringClass"), core, Get(method, "Handle"), values })!;
            foreach (int arity in new[] { 0, 1, 3 })
            {
                var invalid = Array.CreateInstance(Model("TypeDesc"), arity);
                for (int index = 0; index < arity; index++) invalid.SetValue(control.Arguments.GetValue(index % 2), index);
                Check(ReferenceEquals(Instantiate(control.Arguments), method), "instantiate-good-before");
                bool refused = false;
                try { Instantiate(invalid); } catch (TargetInvocationException error) when (error.InnerException is NotSupportedException) { refused = true; }
                Check(refused, "instantiate-arity");
                Check(ReferenceEquals(Instantiate(control.Arguments), method), "instantiate-good-after");
            }
            void Poison(string field, object replacement)
            {
                var saved = Get(method, field); Set(method, field, replacement);
                try
                {
                    Check(!Accepted(method), "predicate-poison-" + field);
                    Check(Refused(resolve, app, control.Token, empty), "cache-poison-" + field);
                }
                finally { Set(method, field, saved); }
                Check(ReferenceEquals(Invoke(resolve, app, control.Token, empty), method) && Accepted(method), "cache-restored-" + field);
            }
            Poison("Rva", 0); Poison("Rva", 1); Poison("IsSynthetic", true); Poison("Module", app);
            Poison("Handle", default(MethodDefinitionHandle)); Poison("Context", empty);
            Poison("Attributes", MethodAttributes.Assembly | MethodAttributes.Static | MethodAttributes.HideBySig);
            Poison("ImplAttributes", MethodImplAttributes.NoInlining);
            CheckBodies(compilation, core, app, args[2], args[3], control.Token, method, empty,
                (module, token, context) => Invoke(resolve, module, token, context), resolveGuard, Check);
        }
        Console.WriteLine("vector-as-guards:checks=" + checks.ToString(CultureInfo.InvariantCulture));
        return true;
    }

    private static void CheckBodies(object compilation, object core, object app, string corePath, string appPath,
        EntityHandle token, object method, object empty, Func<object, EntityHandle, object, object?> resolve,
        System.Reflection.MethodInfo guard, Action<bool, string> check)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        object Get(object value, string name) => value.GetType().GetField(name, flags) is { } field ? field.GetValue(value)! : value.GetType().GetProperty(name, flags)!.GetValue(value)!;
        void Set(object value, string name, object replacement) => value.GetType().GetField(name, flags)!.SetValue(value, replacement);
        var pe = (PEReader)Get(core, "PE"); var reader = (MetadataReader)Get(core, "Reader");
        var originalImage = File.ReadAllBytes(corePath);
        var byReader = (IDictionary)Get(compilation, "_byReader");
        int BodyOffset(int rva)
        {
            var section = pe.PEHeaders.SectionHeaders.Single(s => rva >= s.VirtualAddress && rva < s.VirtualAddress + s.SizeOfRawData);
            int offset = section.PointerToRawData + rva - section.VirtualAddress;
            return offset + ((originalImage[offset] & 3) == 2 ? 1 : (originalImage[offset + 1] >> 4) * 4);
        }
        int BlobOffset(BlobHandle blob)
        {
            int offset = pe.PEHeaders.MetadataStartOffset + reader.GetHeapMetadataOffset(HeapIndex.Blob) + MetadataTokens.GetHeapOffset(blob);
            return offset + (originalImage[offset] < 0x80 ? 1 : originalImage[offset] < 0xc0 ? 2 : 4);
        }
        int RowOffset(TableIndex table, int row) => pe.PEHeaders.MetadataStartOffset + reader.GetTableMetadataOffset(table) + (row - 1) * reader.GetTableRowSize(table);
        var definitionHandle = (MethodDefinitionHandle)Get(method, "Handle");
        var definition = reader.GetMethodDefinition(definitionHandle);
        int offset = BodyOffset(definition.RelativeVirtualAddress);
        var il = pe.GetMethodBody(definition.RelativeVirtualAddress).GetILBytes()!;
        check(il.Length == 17 && il[0] == 0x28 && il[5] == 0x28 && il[10] == 2 && il[11] == 0x28 && il[16] == 0x2a, "original-As-body");
        var fromSpec = reader.GetMethodSpecification((MethodSpecificationHandle)MetadataTokens.EntityHandle(BitConverter.ToInt32(il, 1)));
        var toSpec = reader.GetMethodSpecification((MethodSpecificationHandle)MetadataTokens.EntityHandle(BitConverter.ToInt32(il, 6)));
        check(fromSpec.Method == toSpec.Method && fromSpec.Method.Kind == HandleKind.MethodDefinition, "same-helper-source-first");
        check(Convert.ToHexString(reader.GetBlobBytes(fromSpec.Signature)) == "0A011E00"
            && Convert.ToHexString(reader.GetBlobBytes(toSpec.Signature)) == "0A011E01", "source-destination-MVAR-order");
        var helper = reader.GetMethodDefinition((MethodDefinitionHandle)fromSpec.Method);
        int helperOffset = BodyOffset(helper.RelativeVirtualAddress);
        var helperIl = pe.GetMethodBody(helper.RelativeVirtualAddress).GetILBytes()!;
        check(helperIl.Length == 15 && helperIl[5] == 0x2d && helperIl[7] == 0x1f && helperIl[8] == 0x43, "helper-original-resource");
        var mutations = new List<(int Offset, byte[] Value, string Name)>();
        void Byte(int at, byte value, string name) => mutations.Add((at, new[] { value }, name));
        Byte(offset, 0, "prefix-nop"); Byte(offset + 5, 0x6f, "second-callvirt"); Byte(offset + 10, 3, "wrong-argument");
        Byte(offset + 16, 0, "missing-return");
        mutations.Add((offset + 1, il.Skip(6).Take(4).ToArray(), "first-guard-is-destination"));
        mutations.Add((offset + 6, il.Skip(1).Take(4).ToArray(), "second-guard-is-source"));
        Byte(BlobOffset(fromSpec.Signature) + 2, 0x13, "source-typevar");
        Byte(BlobOffset(toSpec.Signature) + 3, 0, "destination-wrong-mvar");
        mutations.Add((offset + 12, il.Skip(1).Take(4).ToArray(), "wrong-BitCast-target"));
        var bitCastSpec = reader.GetMethodSpecification((MethodSpecificationHandle)MetadataTokens.EntityHandle(BitConverter.ToInt32(il, 12)));
        var bitCast = reader.GetMethodDefinition((MethodDefinitionHandle)bitCastSpec.Method);
        foreach (var parameterHandle in bitCast.GetGenericParameters())
        {
            var parameter = reader.GetGenericParameter(parameterHandle);
            check(parameter.Attributes == GenericParameterAttributes.AllowByRefLike, "BitCast-original-generic-flags");
            int row = RowOffset(TableIndex.GenericParam, MetadataTokens.GetRowNumber(parameterHandle));
            Byte(row + 2, 0, "BitCast-missing-allow-byref-like");
            Byte(row, (byte)(1 - parameter.Index), "BitCast-wrong-generic-index");
        }
        Byte(BlobOffset(bitCast.Signature) + 4, 0, "BitCast-wrong-return-MVAR");
        Byte(helperOffset + 5, 0x2c, "helper-inverted-condition"); Byte(helperOffset + 8, 0x42, "helper-wrong-resource");
        Byte(helperOffset + 14, 0, "helper-missing-ret");
        var sinkHandle = (MethodDefinitionHandle)MetadataTokens.EntityHandle(BitConverter.ToInt32(helperIl, 10));
        var sink = reader.GetMethodDefinition(sinkHandle);
        int sinkOffset = BodyOffset(sink.RelativeVirtualAddress);
        var sinkIl = pe.GetMethodBody(sink.RelativeVirtualAddress).GetILBytes()!;
        Byte(sinkOffset + sinkIl.Length - 1, 0x2a, "sink-returns-instead-of-throw");
        Byte(RowOffset(TableIndex.MethodDef, MetadataTokens.GetRowNumber(definitionHandle)) + 6, 0x93, "As-nonpublic");
        var rawSignature = reader.GetBlobBytes(definition.Signature);
        Byte(BlobOffset(definition.Signature), 0x30, "As-instance-header");
        Byte(BlobOffset(definition.Signature) + rawSignature.Length - 1, 1, "As-argument-MVAR-order");
        foreach (var parameter in definition.GetGenericParameters())
            Byte(RowOffset(TableIndex.GenericParam, MetadataTokens.GetRowNumber(parameter)) + 2, 4, "As-generic-reference-constraint");
        var getterReference = reader.GetMemberReference((MemberReferenceHandle)MetadataTokens.EntityHandle(BitConverter.ToInt32(helperIl, 1)));
        var getterName = reader.GetString(getterReference.Name);
        var vector = reader.TypeDefinitions.Single(h => reader.GetString(reader.GetTypeDefinition(h).Namespace) == "System.Numerics"
            && reader.GetString(reader.GetTypeDefinition(h).Name) == "Vector`1");
        var getter = reader.GetTypeDefinition(vector).GetMethods().Select(reader.GetMethodDefinition).Single(m => reader.GetString(m.Name) == getterName);
        var cctorName = reader.MethodDefinitions.Select(reader.GetMethodDefinition).First(m => reader.GetString(m.Name) == ".cctor").Name;
        int stringWidth = reader.GetHeapSize(HeapIndex.String) > ushort.MaxValue ? 4 : 2;
        foreach (var initializerOwner in new[] { definition.GetDeclaringType(), helper.GetDeclaringType(), vector })
        {
            var unused = reader.GetTypeDefinition(initializerOwner).GetMethods().First(h => h != definitionHandle
                && h != (MethodDefinitionHandle)fromSpec.Method && reader.GetString(reader.GetMethodDefinition(h).Name) != getterName);
            mutations.Add((RowOffset(TableIndex.MethodDef, MetadataTokens.GetRowNumber(unused)) + 8,
                BitConverter.GetBytes(MetadataTokens.GetHeapOffset(cctorName)).Take(stringWidth).ToArray(), "skipped-owner-initializer"));
        }
        int getterOffset = BodyOffset(getter.RelativeVirtualAddress);
        Byte(getterOffset, 0, "getter-whitelist-prefix");
        var getterIl = pe.GetMethodBody(getter.RelativeVirtualAddress).GetILBytes()!;
        check(getterIl[0] == 0xd0 && getterIl[5] == 0x28 && getterIl[10] == 0xd0
            && getterIl[15] == 0x28 && getterIl[20] == 0x28, "getter-original-first-whitelist-step");
        var getTypeHandle = (MethodDefinitionHandle)MetadataTokens.EntityHandle(BitConverter.ToInt32(getterIl, 6));
        var equalityHandle = (MethodDefinitionHandle)MetadataTokens.EntityHandle(BitConverter.ToInt32(getterIl, 21));
        foreach (var operationHandle in new[] { getTypeHandle, equalityHandle })
        {
            var operation = reader.GetMethodDefinition(operationHandle);
            int rawOffset = BlobOffset(operation.Signature);
            var operationRaw = reader.GetBlobBytes(operation.Signature);
            check(operationRaw[0] == 0 && operationRaw[1] == (operationHandle == getTypeHandle ? 1 : 2), "type-operation-original-raw-header");
            Byte(rawOffset, 0x20, "type-operation-instance-raw-header");
            Byte(rawOffset, 0x05, "type-operation-vararg-raw-header");
            Byte(rawOffset + 1, (byte)(operationRaw[1] - 1), "type-operation-short-count-trailing-raw");
            Byte(RowOffset(TableIndex.MethodDef, MetadataTokens.GetRowNumber(operationHandle)) + 6, 0x93, "type-operation-nonpublic-attributes");
            Byte(RowOffset(TableIndex.MethodDef, MetadataTokens.GetRowNumber(operationHandle)) + 4, 8, "type-operation-noinline-attributes");
        }
        var decimalHandle = reader.TypeDefinitions.Single(h => reader.GetString(reader.GetTypeDefinition(h).Namespace) == "System"
            && reader.GetString(reader.GetTypeDefinition(h).Name) == "Decimal");
        var doubleHandle = reader.TypeDefinitions.Single(h => reader.GetString(reader.GetTypeDefinition(h).Namespace) == "System"
            && reader.GetString(reader.GetTypeDefinition(h).Name) == "Double");
        mutations.Add((getterOffset + 11, BitConverter.GetBytes(MetadataTokens.GetToken(decimalHandle)), "whitelist-nonnumeric-type-target"));
        mutations.Add((getterOffset + 11, BitConverter.GetBytes(MetadataTokens.GetToken(doubleHandle)), "whitelist-wrong-supported-type-target"));
        var byteHandle = (TypeDefinitionHandle)MetadataTokens.EntityHandle(BitConverter.ToInt32(getterIl, 11));
        mutations.Add((RowOffset(TableIndex.TypeDef, MetadataTokens.GetRowNumber(byteHandle)) + 4,
            BitConverter.GetBytes(MetadataTokens.GetHeapOffset(reader.GetTypeDefinition(decimalHandle).Name)).Take(stringWidth).ToArray(),
            "whitelist-same-token-wrong-type-name"));
        foreach (var mutation in mutations)
        {
            check(ReferenceEquals(resolve(app, token, empty), method), mutation.Name + ":good-before");
            var changed = (byte[])originalImage.Clone(); mutation.Value.CopyTo(changed, mutation.Offset);
            using var modified = new PEReader(new MemoryStream(changed, writable: false));
            var modifiedReader = modified.GetMetadataReader(); byReader.Add(modifiedReader, core);
            Set(core, "PE", modified); Set(core, "Reader", modifiedReader);
            try
            {
                bool refused = false;
                try { guard.Invoke(compilation, new[] { method }); }
                catch (TargetInvocationException error) when (error.InnerException is NotSupportedException) { refused = true; }
                check(refused, mutation.Name + ":guard");
                refused = false;
                try { resolve(app, token, empty); }
                catch (TargetInvocationException error) when (error.InnerException is NotSupportedException) { refused = true; }
                check(refused, mutation.Name + ":precache");
            }
            finally { Set(core, "PE", pe); Set(core, "Reader", reader); byReader.Remove(modifiedReader); }
            check(ReferenceEquals(resolve(app, token, empty), method), mutation.Name + ":good-after");
        }
        foreach (var operationHandle in new[] { getTypeHandle, equalityHandle })
        {
            var operationModel = ((IDictionary)Get(core, "MethodMap"))[operationHandle]!;
            var savedSignature = Get(operationModel, "Signature");
            var replacement = Activator.CreateInstance(savedSignature.GetType(), new[]
            {
                Get(savedSignature, "Header"), Get(savedSignature, "ReturnType"), (object)0,
                Get(savedSignature, "GenericParameterCount"), Get(savedSignature, "ParameterTypes"),
            })!;
            Set(operationModel, "_sig", replacement);
            try
            {
                bool refused = false;
                try { guard.Invoke(compilation, new[] { method }); }
                catch (TargetInvocationException error) when (error.InnerException is NotSupportedException) { refused = true; }
                check(refused, "type-operation-required-parameter-count");
                refused = false;
                try { resolve(app, token, empty); }
                catch (TargetInvocationException error) when (error.InnerException is NotSupportedException) { refused = true; }
                check(refused, "type-operation-required-count-precache");
            }
            finally { Set(operationModel, "_sig", savedSignature); }
            check(ReferenceEquals(resolve(app, token, empty), method), "type-operation-required-count-restored");
        }
        var arguments = (Array)Get(Get(method, "Context"), "MethodArgs");
        var destinationElement = Get(arguments.GetValue(1)!, "Class");
        if (Get(arguments.GetValue(0)!, "Kind").ToString() == "Primitive"
            && destinationElement is not null && (string)Get(destinationElement, "FullName") == "System.Decimal")
        {
            // A concrete Decimal parent decodes to the selected closed vector but
            // no longer proves that the preceding numeric guard is a no-op.
            var originalGuards = (Array)guard.Invoke(compilation, new[] { method })!;
            var guardContext = Get(originalGuards.GetValue(0)!, "Context");
            var expectedVector = Get(Get(Get(method, "Signature"), "ReturnType"), "Class");
            var concrete = new List<byte> { 0x15, 0x11 };
            void Compressed(int value)
            {
                if (value < 0x80) concrete.Add((byte)value);
                else if (value < 0x4000) { concrete.Add((byte)(0x80 | value >> 8)); concrete.Add((byte)value); }
                else { concrete.Add((byte)(0xc0 | value >> 24)); concrete.Add((byte)(value >> 16)); concrete.Add((byte)(value >> 8)); concrete.Add((byte)value); }
            }
            Compressed(MetadataTokens.GetRowNumber(vector) << 2); concrete.Add(1); concrete.Add(0x11);
            Compressed(MetadataTokens.GetRowNumber((TypeDefinitionHandle)Get(destinationElement, "Handle")) << 2);
            BlobHandle donor = default;
            for (int row = 1; row <= reader.GetTableRowCount(TableIndex.TypeSpec); row++)
            {
                var donorSignature = reader.GetTypeSpecification(MetadataTokens.TypeSpecificationHandle(row)).Signature;
                if (reader.GetBlobBytes(donorSignature).Length == concrete.Count) { donor = donorSignature; break; }
            }
            check(!donor.IsNil, "concrete-getter-donor-blob");
            var changed = (byte[])originalImage.Clone(); concrete.ToArray().CopyTo(changed, BlobOffset(donor));
            int heapWidth = reader.GetHeapSize(HeapIndex.Blob) > ushort.MaxValue ? 4 : 2;
            BitConverter.GetBytes(MetadataTokens.GetHeapOffset(donor)).Take(heapWidth).ToArray().CopyTo(changed,
                RowOffset(TableIndex.TypeSpec, MetadataTokens.GetRowNumber((TypeSpecificationHandle)getterReference.Parent)));
            using var modified = new PEReader(new MemoryStream(changed, writable: false));
            var modifiedReader = modified.GetMetadataReader(); byReader.Add(modifiedReader, core);
            Set(core, "PE", modified); Set(core, "Reader", modifiedReader);
            try
            {
                var resolved = compilation.GetType().GetMethod("ResolveTypeTokenForScan", flags)!
                    .Invoke(compilation, new[] { core, (object)getterReference.Parent, guardContext })!;
                check(ReferenceEquals(Get(resolved, "Class"), expectedVector), "concrete-getter-still-matches-closed-Decimal");
                bool refused = false;
                try { guard.Invoke(compilation, new[] { method }); }
                catch (TargetInvocationException error) when (error.InnerException is NotSupportedException) { refused = true; }
                check(refused, "concrete-getter-parent-guard");
                refused = false;
                try { resolve(app, token, empty); }
                catch (TargetInvocationException error) when (error.InnerException is NotSupportedException) { refused = true; }
                check(refused, "concrete-getter-parent-precache");
            }
            finally { Set(core, "PE", pe); Set(core, "Reader", reader); byReader.Remove(modifiedReader); }
            check(ReferenceEquals(resolve(app, token, empty), method), "concrete-getter-parent-restored");
        }
        // Reuse an existing valid EH body while preserving the As declaration and
        // matching model RVA, so rejection tests EH policy rather than stale identity.
        int ehRva = reader.MethodDefinitions.Select(reader.GetMethodDefinition)
            .Where(m => m.RelativeVirtualAddress != 0)
            .Select(m => m.RelativeVirtualAddress).First(rva => pe.GetMethodBody(rva).ExceptionRegions.Length != 0);
        var ehImage = (byte[])originalImage.Clone();
        BitConverter.GetBytes(ehRva).CopyTo(ehImage, RowOffset(TableIndex.MethodDef, MetadataTokens.GetRowNumber(definitionHandle)));
        using (var modified = new PEReader(new MemoryStream(ehImage, writable: false)))
        {
            var modifiedReader = modified.GetMetadataReader(); byReader.Add(modifiedReader, core);
            Set(core, "PE", modified); Set(core, "Reader", modifiedReader); Set(method, "Rva", ehRva);
            try
            {
                bool refused = false;
                try { guard.Invoke(compilation, new[] { method }); }
                catch (TargetInvocationException error) when (error.InnerException is NotSupportedException) { refused = true; }
                check(refused, "As-EH-body");
                refused = false;
                try { resolve(app, token, empty); }
                catch (TargetInvocationException error) when (error.InnerException is NotSupportedException) { refused = true; }
                check(refused, "As-EH-precache");
            }
            finally { Set(method, "Rva", definition.RelativeVirtualAddress); Set(core, "PE", pe); Set(core, "Reader", reader); byReader.Remove(modifiedReader); }
        }
        check(ReferenceEquals(resolve(app, token, empty), method), "EH-restored");
        var appPe = (PEReader)Get(app, "PE"); var appReader = (MetadataReader)Get(app, "Reader");
        var appImage = File.ReadAllBytes(appPath);
        var signature = appReader.GetMethodSpecification((MethodSpecificationHandle)token).Signature;
        int blobOffset = appPe.PEHeaders.MetadataStartOffset + appReader.GetHeapMetadataOffset(HeapIndex.Blob) + MetadataTokens.GetHeapOffset(signature);
        blobOffset += appImage[blobOffset] < 0x80 ? 1 : appImage[blobOffset] < 0xc0 ? 2 : 4;
        foreach (var mutation in new[] { (Offset: 0, Value: (byte)0x09), (Offset: 1, Value: (byte)1) })
        {
            var changed = (byte[])appImage.Clone(); changed[blobOffset + mutation.Offset] = mutation.Value;
            using var modified = new PEReader(new MemoryStream(changed, writable: false));
            var modifiedReader = modified.GetMetadataReader(); byReader.Add(modifiedReader, app);
            Set(app, "PE", modified); Set(app, "Reader", modifiedReader);
            try
            {
                bool refused = false;
                try { resolve(app, token, empty); }
                catch (TargetInvocationException error) when (error.InnerException is NotSupportedException) { refused = true; }
                check(refused, "same-token-malformed-spec");
            }
            finally { Set(app, "PE", appPe); Set(app, "Reader", appReader); byReader.Remove(modifiedReader); }
            check(ReferenceEquals(resolve(app, token, empty), method), "same-token-spec-restored");
        }
    }
}
