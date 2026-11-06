using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

internal static class VectorForwardingGuards
{
    private delegate bool RawAlias(BlobReader blob, string name, out int token);

    public static bool Run(string[] args)
    {
        if (args.Length != 5 || args[0] != "--vector-forwarding-guards") return false;
        string family = args[4];
        if (family is not ("explicit" or "alias")) throw new ArgumentException(family);
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        var cases = new List<(string Source, string Destination, string Route, string Mutation)>();
        foreach (string source in VectorAsFixture.Elements)
        foreach (string destination in VectorAsFixture.NumericElements)
            cases.Add((source, destination, "direct", "valid-runtime"));
        foreach (string source in new[] { "decimal", "enum" })
        foreach (string mode in new[] { "valid-runtime-var", "valid-runtime-mvar" })
            cases.Add((source, "int", "direct", mode));
        VectorAsFixture.Build(args[3], args[2], cases, family);
        var production = Assembly.LoadFrom(Path.GetFullPath(args[1]));
        const BindingFlags instance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        const BindingFlags statics = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        Type Model(string name) => production.GetType("Dn2Cpp." + name, true)!;
        object Get(object value, string name) => value.GetType().GetField(name, instance) is { } f ? f.GetValue(value)! : value.GetType().GetProperty(name, instance)!.GetValue(value)!;
        void Set(object value, string name, object? next)
        {
            if (value.GetType().GetField(name, instance) is { } f) f.SetValue(value, next);
            else value.GetType().GetProperty(name, instance)!.SetValue(value, next);
        }
        var options = Activator.CreateInstance(Model("TranspileOptions"))!;
        Set(options, "Input", Path.GetFullPath(args[3])); Set(options, "UseILDiet", false); Set(options, "SharedGenerics", false);
        Set(options, "AutoRef", false); Set(options, "IsaSurfaceDump", "predicate-guard-only");
        object compilation;
        try { compilation = Model("Compilation").GetMethod("Create", statics)!.Invoke(null, new object[] { new[] { Path.GetFullPath(args[3]), Path.GetFullPath(args[2]) }, options })!; }
        catch (TargetInvocationException e) when (e.InnerException is not null) { throw e.InnerException; }
        var modules = ((IEnumerable)Get(compilation, "Modules")).Cast<object>().ToArray();
        var core = modules.Single(m => (string)Get(m, "AssemblyName") == "System.Private.CoreLib");
        var app = modules.Single(m => (string)Get(m, "Path") == Path.GetFullPath(args[3]));
        var reader = (MetadataReader)Get(app, "Reader");
        var empty = Model("GenericContext").GetField("Empty", statics)!.GetValue(null)!;
        var noArguments = Array.CreateInstance(Model("TypeDesc"), 0);
        object Context(object source, bool type)
        {
            var values = Array.CreateInstance(Model("TypeDesc"), 2); values.SetValue(source, 0); values.SetValue(source, 1);
            return Activator.CreateInstance(Model("GenericContext"), new object[] { type ? values : noArguments, type ? noArguments : values })!;
        }
        var predicate = Model("CoreIntrinsics").GetMethod("IsVectorManagedForwarder", statics)!;
        var numeric = Model("CoreIntrinsics").GetMethod("IsVectorExplicit", statics)!;
        var alias = Model("CoreIntrinsics").GetMethod("IsVectorAlias", statics)!;
        var resolve = Model("Compilation").GetMethod(family == "alias" ? "ResolveMethodSpec" : "ResolveMemberRefMethod", instance)!;
        var scan = Model("Compilation").GetMethod("ResolveCallTarget", instance)!;
        var special = Model("Compilation").GetMethod("TryResolveGuardedVectorAsSpec", instance)!;
        int checks = 0;
        void Check(bool condition, string label)
        {
            if (!condition) throw new InvalidOperationException("vector forwarding guard " + family + ": " + label);
            checks++;
        }
        bool Accepted(object method) => (bool)predicate.Invoke(null, new[] { method })!;
        object? Invoke(System.Reflection.MethodInfo operation, EntityHandle handle, object context)
        {
            object token = operation == scan ? handle : handle.Kind == HandleKind.MethodSpecification
                ? (object)(MethodSpecificationHandle)handle : (MemberReferenceHandle)handle;
            return operation.Invoke(compilation, new[] { app, token, context });
        }
        bool Refused(System.Reflection.MethodInfo operation, EntityHandle handle, object context)
        {
            try { return Invoke(operation, handle, context) is null; }
            catch (TargetInvocationException e) when (e.InnerException is NotSupportedException) { return true; }
        }
        using var serialized = Mono.Cecil.AssemblyDefinition.ReadAssembly(args[3]);
        var methods = serialized.MainModule.Types.SelectMany(t => t.Methods).Where(m => m.Name.StartsWith("Case", StringComparison.Ordinal))
            .OrderBy(m => int.Parse(m.Name.AsSpan(4, m.Name.IndexOf('_') - 4), CultureInfo.InvariantCulture)).ToArray();
        Check(methods.Length == cases.Count, "case-count");
        EntityHandle Token(Mono.Cecil.MethodDefinition method) => MetadataTokens.EntityHandle(method.Body.Instructions
            .Select(i => i.Operand).OfType<Mono.Cecil.MethodReference>().Single(m => family == "explicit" ? m.Name == "op_Explicit" : m.Name.StartsWith("AsVector", StringComparison.Ordinal)).MetadataToken.ToInt32());
        var controls = new Dictionary<(string, string), (EntityHandle Token, object Method, object Source)>();
        var handles = new HashSet<MethodDefinitionHandle>();
        for (int i = 0; i < 72; i++)
        {
            var token = Token(methods[i]); object method;
            try { method = Invoke(resolve, token, empty)!; }
            catch (TargetInvocationException e) when (e.InnerException is not null)
            { throw new InvalidOperationException("initial forwarding control " + i + ":" + cases[i].Source + "->" + cases[i].Destination + " token=" + MetadataTokens.GetToken(token).ToString("X8"), e.InnerException); }
            Check(Accepted(method), "managed-family");
            Check(!(bool)numeric.Invoke(null, new[] { method })!, "not-numeric-explicit");
            if (family == "alias") Check((bool)alias.Invoke(null, new[] { method })!, "alias-authority");
            var owner = Get(method, "DeclaringClass");
            var source = ((Array)Get(Get(family == "alias" ? method : owner, "Context"), family == "alias" ? "MethodArgs" : "TypeArgs")).GetValue(0)!;
            controls.Add((cases[i].Source, cases[i].Destination), (token, method, source));
            handles.Add((MethodDefinitionHandle)Get(method, "Handle"));
            Check(ReferenceEquals(Invoke(resolve, token, empty), method), "stable-cache");
        }
        Check(handles.Count == 12, "return-sensitive-twelve-definitions");
        for (int i = 72; i < cases.Count; i++)
        {
            var item = cases[i]; var control = controls[(item.Source, item.Destination)]; var token = Token(methods[i]);
            bool type = item.Mutation.EndsWith("-var", StringComparison.Ordinal);
            var context = Context(control.Source, type);
            Check(Accepted(Invoke(resolve, token, context)!), "scoped-context");
            Check(Refused(resolve, token, empty), "missing-context");
            Check(Accepted(Invoke(resolve, token, context)!), "context-good-bad-good");
            if (family == "alias")
            {
                var canonical = Activator.CreateInstance(Model("TypeDesc"))!;
                Set(canonical, "Kind", Enum.Parse(Model("TypeKind"), "Primitive"));
                Set(canonical, "Primitive", PrimitiveTypeCode.Int32); Set(canonical, "IsCanonPlaceholder", true); Set(canonical, "CanonAnyIndex", -1);
                var canonicalContext = Context(canonical, type);
                int before = (int)Get(compilation, "_methodInstanceCount");
                Check(Invoke(scan, token, canonicalContext) is null, "canonical-scan-deferred");
                Check((int)Get(compilation, "_methodInstanceCount") == before, "canonical-no-cache-insertion");
                Check(Refused(special, token, canonicalContext) && Refused(resolve, token, canonicalContext), "canonical-strict-rejection");
                Set(canonical, "CanonAnyIndex", 0);
                Check(Refused(scan, token, canonicalContext), "canonical-reference-width-refused");
                Set(canonical, "CanonAnyIndex", -1); Set(canonical, "Primitive", PrimitiveTypeCode.Boolean);
                Check(Refused(scan, token, canonicalContext), "canonical-bool-refused");
                Check(Accepted(Invoke(resolve, token, context)!), "canonical-restored-concrete");
            }
        }
        var byReader = (IDictionary)Get(compilation, "_byReader");
        void WithImage(object module, string path, Action<MetadataReader, PEReader, byte[], Action<int, byte[], Action>> action)
        {
            var oldPe = (PEReader)Get(module, "PE"); var oldReader = (MetadataReader)Get(module, "Reader"); var image = File.ReadAllBytes(path);
            void Patch(int offset, byte[] bytes, Action test)
            {
                var changed = (byte[])image.Clone(); bytes.CopyTo(changed, offset);
                using var pe = new PEReader(new MemoryStream(changed, writable: false)); var metadata = pe.GetMetadataReader();
                byReader.Add(metadata, module); Set(module, "PE", pe); Set(module, "Reader", metadata);
                try { test(); }
                finally { Set(module, "PE", oldPe); Set(module, "Reader", oldReader); byReader.Remove(metadata); }
            }
            action(oldReader, oldPe, image, Patch);
        }
        int BlobOffset(MetadataReader metadata, PEReader pe, byte[] image, BlobHandle handle)
        {
            int at = pe.PEHeaders.MetadataStartOffset + metadata.GetHeapMetadataOffset(HeapIndex.Blob) + MetadataTokens.GetHeapOffset(handle);
            return at + (image[at] < 0x80 ? 1 : image[at] < 0xc0 ? 2 : 4);
        }
        foreach (var control in controls.Values.Take(12))
        {
            var method = control.Method; var owner = Get(method, "DeclaringClass");
            void Poison(object value, string field, object? replacement)
            {
                object saved = Get(value, field); Set(value, field, replacement);
                try { Check(!Accepted(method), "predicate-" + field); Check(Refused(resolve, control.Token, empty), "cache-" + field); }
                catch (TargetInvocationException e) when (e.InnerException is not null)
                { throw new InvalidOperationException("poison " + value.GetType().Name + "." + field + "=" + replacement, e.InnerException); }
                finally { Set(value, field, saved); }
                Check(ReferenceEquals(Invoke(resolve, control.Token, empty), method) && Accepted(method), "restored-" + field);
            }
            Poison(method, "Module", app); Poison(method, "Handle", default(MethodDefinitionHandle));
            Poison(method, "Rva", 0); Poison(method, "IsSynthetic", true);
            Poison(method, "Attributes", MethodAttributes.Private | MethodAttributes.Static | MethodAttributes.HideBySig);
            Poison(method, "ImplAttributes", MethodImplAttributes.InternalCall);
            Poison(method, "Name", family == "alias" ? "AsVectorMissing" : "op_Implicit");
            Poison(owner, "Module", app); Poison(owner, "Handle", default(TypeDefinitionHandle));
            Poison(control.Source, "IsCanonPlaceholder", true);
            foreach (string kind in new[] { "ByRef", "Pointer", "GenericVar" })
                Poison(control.Source, "Kind", Enum.Parse(Model("TypeKind"), kind));
            var sourceClass = Get(control.Source, "Class");
            Poison(sourceClass, "IsValueType", false); Poison(sourceClass, "IsByRefLike", true); Poison(sourceClass, "GenericArity", 1);
            var result = Get(Get(method, "Signature"), "ReturnType");
            var other = controls.Values.First(c => !Equals(Get(c.Method, "Handle"), Get(method, "Handle")));
            Poison(result, "Class", Get(Get(Get(other.Method, "Signature"), "ReturnType"), "Class"));
            Poison(result, "Kind", Enum.Parse(Model("TypeKind"), "ByRef"));
            var parameter = ((IEnumerable)Get(Get(method, "Signature"), "ParameterTypes")).Cast<object>().Single();
            Poison(parameter, "Class", Get(result, "Class"));
            Poison(method, "Context", empty);
            WithImage(app, args[3], (metadata, pe, image, patch) =>
            {
                var referenceHandle = control.Token.Kind == HandleKind.MethodSpecification
                    ? (MemberReferenceHandle)metadata.GetMethodSpecification((MethodSpecificationHandle)control.Token).Method : (MemberReferenceHandle)control.Token;
                var reference = metadata.GetMemberReference(referenceHandle); var bytes = metadata.GetBlobBytes(reference.Signature);
                int offset = BlobOffset(metadata, pe, image, reference.Signature);
                foreach (var change in new[] { (0, (byte)(family == "alias" ? 0x30 : 0x20)), (family == "alias" ? 2 : 1, (byte)0), (bytes.Length - 1, (byte)1) })
                {
                    patch(offset + change.Item1, new[] { change.Item2 }, () => Check(Refused(resolve, control.Token, empty), "raw-reference-before-cache"));
                    Check(ReferenceEquals(Invoke(resolve, control.Token, empty), method), "raw-reference-restored");
                }
                patch(offset + bytes.Length - 2, new byte[] { 0x10, 0x08 }, () => Check(Refused(resolve, control.Token, empty), "raw-reference-byref-i4-before-cache"));
                Check(ReferenceEquals(Invoke(resolve, control.Token, empty), method), "raw-reference-byref-restored");
                if (family == "explicit")
                {
                    var parentSignature = metadata.GetTypeSpecification((TypeSpecificationHandle)reference.Parent).Signature;
                    var parentRaw = metadata.GetBlobReader(parentSignature); var parentBytes = metadata.GetBlobBytes(parentSignature);
                    Check(parentRaw.ReadByte() == 0x15 && parentRaw.ReadByte() == 0x11, "original-parent-vector-value-type");
                    parentRaw.ReadCompressedInteger(); Check(parentRaw.ReadByte() == 1, "original-parent-arity");
                    int sourceKind = parentBytes.Length - parentRaw.RemainingBytes;
                    Check(parentRaw.ReadByte() == 0x11, "original-parent-managed-value-element");
                    int parentOffset = BlobOffset(metadata, pe, image, parentSignature);
                    foreach (int changedByte in new[] { 1, sourceKind })
                    {
                        patch(parentOffset + changedByte, new byte[] { 0x12 }, () => Check(Refused(resolve, control.Token, empty), "raw-parent-erased-class-kind"));
                        Check(ReferenceEquals(Invoke(resolve, control.Token, empty), method), "raw-parent-restored");
                    }
                }
                if (family == "alias")
                {
                    var spec = metadata.GetMethodSpecification((MethodSpecificationHandle)control.Token); int at = BlobOffset(metadata, pe, image, spec.Signature);
                    foreach (var change in new[] { (0, (byte)9), (1, (byte)0), (1, (byte)2), (2, (byte)0x10) })
                    {
                        patch(at + change.Item1, new[] { change.Item2 }, () => Check(Refused(resolve, control.Token, empty), "raw-methodspec-before-cache"));
                        Check(ReferenceEquals(Invoke(resolve, control.Token, empty), method), "raw-methodspec-restored");
                    }
                    var referenceResolver = Model("Compilation").GetMethod("ResolveMemberRefMethod", instance)!;
                    Check(Refused(referenceResolver, referenceHandle, empty), "bare-generic-alias-reference");
                }
            });
            WithImage(core, args[2], (metadata, pe, image, patch) =>
            {
                var handle = (MethodDefinitionHandle)Get(method, "Handle"); var declaration = metadata.GetMethodDefinition(handle);
                int row = pe.PEHeaders.MetadataStartOffset + metadata.GetTableMetadataOffset(TableIndex.MethodDef)
                    + (MetadataTokens.GetRowNumber(handle) - 1) * metadata.GetTableRowSize(TableIndex.MethodDef);
                int at = BlobOffset(metadata, pe, image, declaration.Signature); var raw = metadata.GetBlobBytes(declaration.Signature);
                foreach (var change in new[] { (row + 6, (byte)0x93), (row + 4, (byte)0x08), (at, (byte)0x30), (at + raw.Length - 1, (byte)1), (at + raw.Length - 2, (byte)(family == "alias" ? 0x13 : 0x1e)) })
                {
                    patch(change.Item1, new[] { change.Item2 }, () => { Check(!Accepted(method), "raw-definition"); Check(Refused(resolve, control.Token, empty), "raw-definition-cache"); });
                    Check(ReferenceEquals(Invoke(resolve, control.Token, empty), method) && Accepted(method), "raw-definition-restored");
                }
                if (family == "explicit")
                {
                    var il = pe.GetMethodBody(declaration.RelativeVirtualAddress).GetILBytes()!;
                    Check(il.Length == 7 && il[0] == 2 && il[1] == 0x28 && il[6] == 0x2a, "pinned-original-forwarding-IL");
                    var specification = (MethodSpecificationHandle)MetadataTokens.EntityHandle(BitConverter.ToInt32(il, 2));
                    int specRow = pe.PEHeaders.MetadataStartOffset + metadata.GetTableMetadataOffset(TableIndex.MethodSpec)
                        + (MetadataTokens.GetRowNumber(specification) - 1) * metadata.GetTableRowSize(TableIndex.MethodSpec);
                    int width = Math.Max(metadata.MethodDefinitions.Count, metadata.MemberReferences.Count) < 32768 ? 2 : 4;
                    patch(specRow, BitConverter.GetBytes(MetadataTokens.GetRowNumber(handle) << 1).Take(width).ToArray(), () =>
                    {
                        bool refused = false;
                        try { Model("Compilation").GetMethod("ResolveMethodSpec", instance)!.Invoke(compilation, new[] { core, (object)specification, empty }); }
                        catch (TargetInvocationException e) when (e.InnerException is NotSupportedException) { refused = true; }
                        Check(refused, "nongeneric-operator-methodspec-refused");
                    });
                }
                var parameters = family == "alias" ? declaration.GetGenericParameters()
                    : metadata.GetTypeDefinition(declaration.GetDeclaringType()).GetGenericParameters();
                foreach (var generic in parameters)
                {
                    int gp = pe.PEHeaders.MetadataStartOffset + metadata.GetTableMetadataOffset(TableIndex.GenericParam)
                        + (MetadataTokens.GetRowNumber(generic) - 1) * metadata.GetTableRowSize(TableIndex.GenericParam);
                    patch(gp + 2, new byte[] { 4 }, () => Check(Refused(resolve, control.Token, empty), "generic-constraint"));
                }
            });
        }
        if (family == "alias")
        {
            var raw = Model("CoreIntrinsics").GetMethod("ReadVectorAsFamilySignature", statics)!.CreateDelegate<RawAlias>();
            foreach (var item in new[] { ("AsVectorByte", "1001011511040105151104011E00", true), ("AsVectorDouble", "100101151104010D151104011E00", true),
                ("AsVectorByte", "100101151104010D151104011E00", false), ("AsVectorByte", "1002011511040105151104011E00", false),
                ("AsVectorByte", "1001011511040105151104011300", false), ("AsVectorByte", "1001011511040105151104011E0000", false),
                ("AsVectorMissing", "1001011511040105151104011E00", false), ("AsVectorByte", "1001011511040105151105011E00", false) })
            {
                var metadata = new MetadataBuilder(); metadata.AddModule(0, metadata.GetOrAddString("ForwardingRaw"), metadata.GetOrAddGuid(Guid.Empty), default, default);
                var blob = metadata.GetOrAddBlob(Convert.FromHexString(item.Item2)); var builder = new BlobBuilder(); new MetadataRootBuilder(metadata).Serialize(builder, 0, 0);
                using var provider = MetadataReaderProvider.FromMetadataImage(builder.ToImmutableArray());
                Check(raw(provider.GetMetadataReader().GetBlobReader(blob), item.Item1, out _) == item.Item3, "raw-alias-name-return");
            }
        }
        Console.WriteLine("vector-forwarding-guards:" + family + ":checks=" + checks.ToString(CultureInfo.InvariantCulture));
        return true;
    }
}
