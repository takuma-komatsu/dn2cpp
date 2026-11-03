using System.Collections;
using System.Collections.Immutable;
using System.Globalization;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;

internal static class ArraySortFactoryGuards
{
    private delegate bool RawPredicate(BlobReader blob, out int parameters, out EntityHandle type);

    public static bool Run(string[] args)
    {
        if (args.Length != 4 || args[0] != "--array-sort-factory-guards") return false;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        string input = Path.GetFullPath(args[3]);
        if (File.Exists(input)) throw new InvalidOperationException("guard model output must be new");
        using (var schema = Mono.Cecil.AssemblyDefinition.ReadAssembly(args[2]))
        using (var empty = Mono.Cecil.AssemblyDefinition.CreateAssembly(new Mono.Cecil.AssemblyNameDefinition(
            "ArraySortFactoryGuardInput", new Version(1, 0)), "ArraySortFactoryGuardInput", Mono.Cecil.ModuleKind.Dll))
        {
            var module = empty.MainModule;
            var host = new Mono.Cecil.TypeDefinition("Fixture", "References", Mono.Cecil.TypeAttributes.Public, module.TypeSystem.Object);
            module.Types.Add(host);
            var owner = schema.MainModule.GetType("System.RuntimeTypeHandle");
            var runtimeType = schema.MainModule.GetType("System.RuntimeType");
            foreach (string mutation in new[] { "valid-two", "valid-three", "instance", "vararg", "wrong-return", "wrong-type",
                "byref", "array", "modified", "missing", "extra", "foreign-type", "foreign-owner" })
            {
                var target = new Mono.Cecil.MethodReference("CreateInstanceForAnotherGenericParameter", module.TypeSystem.Object,
                    module.ImportReference(owner));
                for (int slot = 0; slot < (mutation == "valid-three" ? 3 : 2); slot++)
                    target.Parameters.Add(new Mono.Cecil.ParameterDefinition(module.ImportReference(runtimeType)));
                switch (mutation)
                {
                    case "instance": target.HasThis = true; break;
                    case "vararg": target.CallingConvention = Mono.Cecil.MethodCallingConvention.VarArg; break;
                    case "wrong-return": target.ReturnType = module.TypeSystem.Int32; break;
                    case "wrong-type": target.Parameters[1].ParameterType = module.ImportReference(schema.MainModule.GetType("System.Type")); break;
                    case "byref": target.Parameters[1].ParameterType = new Mono.Cecil.ByReferenceType(target.Parameters[1].ParameterType); break;
                    case "array": target.Parameters[1].ParameterType = new Mono.Cecil.ArrayType(target.Parameters[1].ParameterType); break;
                    case "modified": target.Parameters[1].ParameterType = new Mono.Cecil.OptionalModifierType(module.TypeSystem.Int32, target.Parameters[1].ParameterType); break;
                    case "missing": target.Parameters.RemoveAt(1); break;
                    case "extra": target.Parameters.Add(new Mono.Cecil.ParameterDefinition(module.TypeSystem.Int32)); break;
                    case "foreign-type":
                    case "foreign-owner":
                        var scope = new Mono.Cecil.AssemblyNameReference("Foreign.Factory", new Version(1, 0));
                        module.AssemblyReferences.Add(scope);
                        if (mutation == "foreign-type") target.Parameters[1].ParameterType = new Mono.Cecil.TypeReference("System", "RuntimeType", module, scope);
                        else target.DeclaringType = new Mono.Cecil.TypeReference("System", "RuntimeTypeHandle", module, scope);
                        break;
                }
                var holder = new Mono.Cecil.MethodDefinition("Ref_" + mutation, Mono.Cecil.MethodAttributes.Public | Mono.Cecil.MethodAttributes.Static, module.TypeSystem.Void);
                host.Methods.Add(holder);
                var il = holder.Body.GetILProcessor();
                for (int slot = 0; slot < target.Parameters.Count + (target.HasThis ? 1 : 0); slot++) il.Emit(Mono.Cecil.Cil.OpCodes.Ldnull);
                il.Emit(Mono.Cecil.Cil.OpCodes.Call, target); il.Emit(Mono.Cecil.Cil.OpCodes.Pop); il.Emit(Mono.Cecil.Cil.OpCodes.Ret);
            }
            empty.Write(input, new Mono.Cecil.WriterParameters { Timestamp = 0, DeterministicMvid = true });
        }
        var production = Assembly.LoadFrom(Path.GetFullPath(args[1]));
        const BindingFlags instance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        const BindingFlags statics = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        Type Model(string name) => production.GetType("Dn2Cpp." + name, true)!;
        object Get(object value, string name) => value.GetType().GetField(name, instance) is { } field
            ? field.GetValue(value)! : value.GetType().GetProperty(name, instance)!.GetValue(value)!;
        void Set(object value, string name, object? next)
        {
            if (value.GetType().GetField(name, instance) is { } field) field.SetValue(value, next);
            else value.GetType().GetProperty(name, instance)!.SetValue(value, next);
        }
        object Invoke(System.Reflection.MethodInfo method, object? target, params object?[] values)
        {
            try { return method.Invoke(target, values)!; }
            catch (TargetInvocationException error) when (error.InnerException is not null) { throw error.InnerException; }
        }
        var options = Activator.CreateInstance(Model("TranspileOptions"))!;
        Set(options, "Input", input); Set(options, "UseILDiet", false); Set(options, "SharedGenerics", false);
        Set(options, "AutoRef", false); Set(options, "IsaSurfaceDump", "predicate-guard-only");
        var compilation = Invoke(Model("Compilation").GetMethod("Create", statics)!, null,
            new[] { input, Path.GetFullPath(args[2]) }, options);
        var modules = ((IEnumerable)Get(compilation, "Modules")).Cast<object>().ToArray();
        var core = modules.Single(module => (string)Get(module, "AssemblyName") == "System.Private.CoreLib");
        var app = modules.Single(module => (string)Get(module, "AssemblyName") == "ArraySortFactoryGuardInput");
        var reader = (MetadataReader)Get(core, "Reader");
        TypeDefinitionHandle TypeHandle(string name) => reader.TypeDefinitions.Single(handle =>
        {
            var type = reader.GetTypeDefinition(handle);
            return reader.GetString(type.Namespace) == "System" && reader.GetString(type.Name) == name && type.GetDeclaringType().IsNil;
        });
        var ownerHandle = TypeHandle("RuntimeTypeHandle");
        var runtimeTypeHandle = TypeHandle("RuntimeType");
        var intrinsic = Model("CoreIntrinsics");
        var predicate = intrinsic.GetMethod("IsRuntimeTypeGenericFactory", statics)!;
        var raw = intrinsic.GetMethod("IsRuntimeTypeGenericFactorySignature", statics)!.CreateDelegate<RawPredicate>();
        var decoded = intrinsic.GetMethod("IsRuntimeTypeGenericFactoryDecodedSignature", statics)!;
        var authority = intrinsic.GetMethod("IsGenericFactoryRuntimeType", statics)!;
        var row = intrinsic.GetField("BrRuntimeTypeGenericFactory", statics)!.GetValue(null)!;
        bool Accepted(object method) => (bool)Invoke(predicate, null, method);
        bool RowMatches(object method) => (bool)Invoke(row.GetType().GetMethod("Matches", instance)!, row, method);
        int checks = 0;
        void Check(bool condition, string label)
        {
            if (!condition) throw new InvalidOperationException("ArraySort factory guard: " + label);
            checks++;
        }
        Check(Get(row, "CutKind").ToString() == "BodyReplace"
            && Get(row, "EmitArm").ToString() == "RuntimeTypeGenericFactoryBody", "body-replacement-row");
        var handles = reader.GetTypeDefinition(ownerHandle).GetMethods().Where(handle =>
            reader.GetString(reader.GetMethodDefinition(handle).Name) == "CreateInstanceForAnotherGenericParameter").ToArray();
        Check(handles.Length == 3, "original-wrapper-import-inventory");
        var actuals = new List<object>();
        foreach (var handle in handles)
        {
            var definition = reader.GetMethodDefinition(handle);
            var actual = ((IDictionary)Get(core, "MethodMap"))[handle]!;
            bool managed = definition.RelativeVirtualAddress != 0;
            if (!managed)
            {
                Check(!Accepted(actual) && !RowMatches(actual), "private-QCall-not-managed-wrapper");
                continue;
            }
            actuals.Add(actual);
            Check(definition.Attributes == (MethodAttributes)0x0093 && definition.ImplAttributes == MethodImplAttributes.IL,
                "original-managed-flags");
            Check(raw(reader.GetBlobReader(definition.Signature), out int count, out EntityHandle type)
                && count is 2 or 3 && type == (EntityHandle)runtimeTypeHandle, "original-raw-ABI");
            Console.WriteLine("array-sort-factory-actual-model:" + MetadataTokens.GetToken(handle).ToString("X8", CultureInfo.InvariantCulture)
                + ":parameters=" + count);
            Set(actual, "_sig", Activator.CreateInstance(Model("MethodInfo").GetProperty("Signature", instance)!.PropertyType));
            int classes = ((IEnumerable)Get(compilation, "Classes")).Cast<object>().Count();
            for (int pass = 0; pass < 2; pass++)
            {
                Check(!(bool)Get(actual, "SignatureReady"), "cold-before");
                Check(Accepted(actual) && RowMatches(actual), "cold-exact-match");
                Check(!(bool)Get(actual, "SignatureReady"), "cold-after");
                Check(((IEnumerable)Get(compilation, "Classes")).Cast<object>().Count() == classes, "no-model-growth");
            }
            var owner = Get(actual, "DeclaringClass");
            object Primitive(PrimitiveTypeCode code) => Invoke(Model("TypeDesc").GetMethod("MakePrimitive", statics)!, null, code);
            var oneArgument = Array.CreateInstance(Model("TypeDesc"), 1); oneArgument.SetValue(Primitive(PrimitiveTypeCode.Int32), 0);
            var empty = Array.CreateInstance(Model("TypeDesc"), 0);
            object Context(bool typeArgument) => Activator.CreateInstance(Model("GenericContext"),
                new object[] { typeArgument ? oneArgument : empty, typeArgument ? empty : oneArgument })!;
            void Reject(object value, string field, object replacement, string label)
            {
                object saved = Get(value, field); Set(value, field, replacement);
                try { Check(!Accepted(actual) && !RowMatches(actual), label); }
                finally { Set(value, field, saved); }
                Check(Accepted(actual) && !(bool)Get(actual, "SignatureReady"), "restored-cold-" + label);
            }
            Reject(core, "AssemblyName", "Foreign.CoreLib", "foreign-assembly");
            Reject(owner, "Module", app, "foreign-owner-module");
            Reject(owner, "_fullName", "Application.RuntimeTypeHandle", "wrong-owner-name");
            Reject(owner, "Handle", TypeHandle("Object"), "wrong-owner-definition");
            Reject(owner, "Handle", default(TypeDefinitionHandle), "nil-owner-definition");
            Reject(owner, "IsValueType", false, "owner-reference-type");
            Reject(owner, "IsEnum", true, "owner-enum");
            Reject(owner, "IsInterface", true, "owner-interface");
            Reject(owner, "IsByRefLike", true, "owner-byref-like");
            Reject(owner, "GenericArity", 1, "owner-generic-arity");
            Reject(actual, "Module", app, "foreign-method-module");
            Reject(actual, "Name", "CreateInstanceForAnotherGenericParameterOther", "wrong-method-name");
            Reject(actual, "Handle", default(MethodDefinitionHandle), "nil-method-definition");
            Reject(actual, "Rva", 0, "bodyless-method");
            Reject(actual, "Rva", definition.RelativeVirtualAddress + 1, "raw-model-rva-mismatch");
            Reject(actual, "Handle", handles.Single(candidate => reader.GetMethodDefinition(candidate).RelativeVirtualAddress == 0),
                "raw-import-definition-model-flags-mismatch");
            Reject(actual, "Handle", handles.Single(candidate => candidate != handle && reader.GetMethodDefinition(candidate).RelativeVirtualAddress != 0),
                "raw-other-wrapper-definition-model-rva-mismatch");
            Reject(actual, "IsSynthetic", true, "synthetic-method");
            Reject(actual, "Context", Context(true), "type-context");
            Reject(actual, "Context", Context(false), "method-context");
            Reject(owner, "Context", Context(true), "owner-type-context");
            Reject(owner, "Context", Context(false), "owner-method-context");
            foreach (var attributes in new[] { (MethodAttributes)0x0096, (MethodAttributes)0x0013,
                (MethodAttributes)0x0083, (MethodAttributes)0x2093, (MethodAttributes)0x0893 })
                Reject(actual, "Attributes", attributes, "attributes-" + (int)attributes);
            foreach (var impl in new[] { MethodImplAttributes.InternalCall, MethodImplAttributes.Runtime,
                MethodImplAttributes.PreserveSig, MethodImplAttributes.Synchronized, MethodImplAttributes.NoInlining })
                Reject(actual, "ImplAttributes", impl, "implementation-" + impl);
            var signature = Get(actual, "Signature");
            Check((bool)Get(actual, "SignatureReady") && (bool)Invoke(decoded, null, signature, core)
                && Accepted(actual) && RowMatches(actual), "prepared-exact-ABI");
            object Changed(byte header, int required, int generic, object? parameters = null) => Activator.CreateInstance(signature.GetType(), new object[]
                { new SignatureHeader(header), Get(signature, "ReturnType"), required, generic, parameters ?? Get(signature, "ParameterTypes") })!;
            void RejectWarmSignature(object changed, bool decodedExpected, string label)
            {
                Set(actual, "_sig", changed);
                int before = ((IEnumerable)Get(compilation, "Classes")).Cast<object>().Count();
                try
                {
                    Check((bool)Get(actual, "SignatureReady"), "warm-ready-" + label);
                    Check((bool)Invoke(decoded, null, changed, core) == decodedExpected, "decoded-" + label);
                    Check(!Accepted(actual) && !RowMatches(actual), "warm-rejected-" + label);
                    Check(((IEnumerable)Get(compilation, "Classes")).Cast<object>().Count() == before, "warm-no-growth-" + label);
                }
                finally { Set(actual, "_sig", signature); }
                Check((bool)Get(actual, "SignatureReady") && Accepted(actual) && RowMatches(actual), "warm-restored-" + label);
            }
            RejectWarmSignature(Changed(0x20, count, 0), false, "instance-header");
            RejectWarmSignature(Changed(5, count, 0), false, "vararg-header");
            RejectWarmSignature(Changed(0x10, count, 1), false, "generic-header");
            RejectWarmSignature(Changed(0, count - 1, 0), false, "required-arity");
            var parameter = ((IEnumerable)Get(signature, "ParameterTypes")).Cast<object>().First();
            var parameters = Get(signature, "ParameterTypes");
            var opposite = count == 2
                ? Invoke(parameters.GetType().GetMethod("Add", new[] { Model("TypeDesc") })!, parameters, parameter)
                : Invoke(parameters.GetType().GetMethod("RemoveAt", new[] { typeof(int) })!, parameters, count - 1);
            RejectWarmSignature(Changed(0, count == 2 ? 3 : 2, 0, opposite), true, "raw-model-arity-mismatch");
            var parameterClass = Get(parameter, "Class");
            void RejectDecoded(object value, string field, object replacement)
            {
                object saved = Get(value, field); Set(value, field, replacement);
                int before = ((IEnumerable)Get(compilation, "Classes")).Cast<object>().Count();
                try
                {
                    Check((bool)Get(actual, "SignatureReady"), "warm-ready-" + field);
                    Check(!(bool)Invoke(decoded, null, signature, core), "decoded-" + field);
                    Check(!Accepted(actual) && !RowMatches(actual), "warm-rejected-" + field);
                    Check(((IEnumerable)Get(compilation, "Classes")).Cast<object>().Count() == before, "warm-no-growth-" + field);
                }
                finally { Set(value, field, saved); }
                Check((bool)Invoke(decoded, null, signature, core) && (bool)Get(actual, "SignatureReady")
                    && Accepted(actual) && RowMatches(actual), "warm-restored-" + field);
            }
            RejectDecoded(Get(signature, "ReturnType"), "Primitive", PrimitiveTypeCode.Void);
            RejectDecoded(Get(signature, "ReturnType"), "Kind", Enum.Parse(Model("TypeKind"), "ByRef"));
            RejectDecoded(Get(signature, "ReturnType"), "IsCanonPlaceholder", true);
            RejectDecoded(parameter, "IsCanonPlaceholder", true);
            RejectDecoded(parameter, "Kind", Enum.Parse(Model("TypeKind"), "ByRef"));
            RejectDecoded(parameterClass, "Module", app);
            RejectDecoded(parameterClass, "Handle", TypeHandle("Type"));
            RejectDecoded(parameterClass, "Handle", default(TypeDefinitionHandle));
            RejectDecoded(parameterClass, "IsValueType", true);
            RejectDecoded(parameterClass, "IsEnum", true);
            RejectDecoded(parameterClass, "IsInterface", true);
            RejectDecoded(parameterClass, "IsByRefLike", true);
            RejectDecoded(parameterClass, "GenericArity", 1);
            RejectDecoded(parameterClass, "Context", Context(true));
            RejectDecoded(parameterClass, "Context", Context(false));
            var context = Model("GenericContext").GetField("Empty", statics)!.GetValue(null)!;
            Check(ReferenceEquals(Invoke(Model("Compilation").GetMethod("ResolveCallTarget", instance)!, compilation,
                core, (EntityHandle)handle, context), actual), "MethodDef-call-keeps-wrapper");
            Invoke(Model("Compilation").GetMethod("Reach", instance)!, compilation, actual);
            Check(((IEnumerable)Get(compilation, "Reachable")).Cast<object>().Any(method => ReferenceEquals(method, actual)), "wrapper-body-reached");
        }
        Check(actuals.Count == 2, "two-managed-overloads");
        var producer = Model("Compilation").GetMethod("IsArraySortFactorySignature", statics)!;
        bool ProducerAccepted(BlobReader blob, int arity, out TypeDefinitionHandle contract)
        {
            object?[] arguments = { core, blob, arity, default(TypeDefinitionHandle) };
            bool accepted = (bool)Invoke(producer, null, arguments);
            contract = (TypeDefinitionHandle)arguments[3]!;
            return accepted;
        }
        foreach (int arity in new[] { 1, 2 })
        {
            TypeDefinitionHandle Family(string name) => reader.TypeDefinitions.Single(handle =>
            {
                var definition = reader.GetTypeDefinition(handle);
                return definition.GetDeclaringType().IsNil && reader.GetString(definition.Namespace) == "System.Collections.Generic"
                    && reader.GetString(definition.Name) == name;
            });
            var owner = Family("ArraySortHelper`" + arity);
            var contract = Family("IArraySortHelper`" + arity);
            var handle = reader.GetTypeDefinition(owner).GetMethods().Single(candidate =>
                reader.GetString(reader.GetMethodDefinition(candidate).Name) == "CreateArraySortHelper");
            var definition = reader.GetMethodDefinition(handle);
            int classes = ((IEnumerable)Get(compilation, "Classes")).Cast<object>().Count();
            Check(ProducerAccepted(reader.GetBlobReader(definition.Signature), arity, out var actualContract)
                && actualContract == contract, "actual-producer-raw-contract-" + arity);
            byte[] Signature(TypeDefinitionHandle family, byte header = 0, byte parameters = 0,
                byte resultKind = 0x12, byte variableKind = 0x13, bool reverseVariables = false,
                bool trailing = false, int argumentCount = -1)
            {
                var blob = new BlobBuilder();
                blob.WriteByte(header); blob.WriteByte(parameters); blob.WriteByte(0x15); blob.WriteByte(resultKind);
                blob.WriteCompressedInteger(MetadataTokens.GetRowNumber(family) << 2);
                blob.WriteByte((byte)(argumentCount < 0 ? arity : argumentCount));
                for (int i = 0; i < arity; i++)
                {
                    blob.WriteByte(variableKind);
                    blob.WriteByte((byte)(reverseVariables ? (arity == 1 ? 1 : arity - i - 1) : i));
                }
                if (trailing) blob.WriteByte(0);
                return blob.ToArray();
            }
            foreach (var vector in new[] {
                (Label: "exact", Bytes: Signature(contract), Expected: true),
                (Label: "instance-header", Bytes: Signature(contract, header: 0x20), Expected: false),
                (Label: "generic-header", Bytes: Signature(contract, header: 0x10), Expected: false),
                (Label: "argument-count", Bytes: Signature(contract, parameters: 1), Expected: false),
                (Label: "value-return", Bytes: Signature(contract, resultKind: 0x11), Expected: false),
                (Label: "class-return-family", Bytes: Signature(owner), Expected: false),
                (Label: "other-interface-family", Bytes: Signature(Family(arity == 1 ? "IComparer`1" : "IReadOnlyDictionary`2")), Expected: false),
                (Label: "other-helper-family", Bytes: Signature(Family("IArraySortHelper`" + (arity == 1 ? 2 : 1))), Expected: false),
                (Label: "type-argument-count", Bytes: Signature(contract, argumentCount: arity + 1), Expected: false),
                (Label: "VAR-order", Bytes: Signature(contract, reverseVariables: true), Expected: false),
                (Label: "MVAR", Bytes: Signature(contract, variableKind: 0x1e), Expected: false),
                (Label: "trailing", Bytes: Signature(contract, trailing: true), Expected: false) })
            {
                var metadata = new MetadataBuilder();
                metadata.AddModule(0, metadata.GetOrAddString("ArraySortFactoryProducerRaw"), metadata.GetOrAddGuid(Guid.Empty), default, default);
                var blob = metadata.GetOrAddBlob(vector.Bytes);
                var image = new BlobBuilder(); new MetadataRootBuilder(metadata).Serialize(image, 0, 0);
                using var source = MetadataReaderProvider.FromMetadataImage(image.ToImmutableArray());
                Check(ProducerAccepted(source.GetMetadataReader().GetBlobReader(blob), arity, out var found) == vector.Expected
                    && (!vector.Expected || found == contract), "producer-" + arity + "-" + vector.Label);
                Check(((IEnumerable)Get(compilation, "Classes")).Cast<object>().Count() == classes, "producer-no-model-growth-" + arity + "-" + vector.Label);
            }
            Console.WriteLine("array-sort-factory-producer:arity=" + arity + ";raw-local-contract");
        }
        Check((bool)Invoke(authority, null, core, (EntityHandle)runtimeTypeHandle), "actual-runtime-type-authority");
        foreach (var handle in new EntityHandle[] { default, TypeHandle("Type"), TypeHandle("Object"),
            MetadataTokens.TypeReferenceHandle(1), MetadataTokens.TypeSpecificationHandle(1),
            MetadataTokens.TypeDefinitionHandle(reader.TypeDefinitions.Count + 1) })
            Check(!(bool)Invoke(authority, null, core, handle), "wrong-runtime-type-authority");
        Check(!(bool)Invoke(authority, null, app, (EntityHandle)runtimeTypeHandle), "foreign-runtime-type-authority");

        foreach (var vector in new (string Hex, bool Expected)[] {
            ("00021C12041204", true), ("00031C120412041204", true), ("00021C12051205", true),
            ("20021C12041204", false), ("60021C12041204", false), ("05021C12041204", false),
            ("1001021C12041204", false), ("00011C1204", false), ("00041C1204120412041204", false),
            ("00020112041204", false), ("0002120412041204", false), ("00021C11041204", false),
            ("00021C12041104", false), ("00021C1012041204", false), ("00021C1204101204", false),
            ("00021C1D12041204", false), ("00021C0F12041204", false), ("00021C12041208", false),
            ("00021C12001200", false), ("00021C12011201", false), ("00021C12061206", false),
            ("00021C12071207", false), ("00021C1204120400", false), ("00021C1204120441", false),
            ("00021C120412", false), ("00021C1280", false), ("0002", false), ("00", false), ("", false) })
        {
            var metadata = new MetadataBuilder();
            metadata.AddModule(0, metadata.GetOrAddString("ArraySortFactoryRaw"), metadata.GetOrAddGuid(Guid.Empty), default, default);
            var blob = metadata.GetOrAddBlob(Convert.FromHexString(vector.Hex));
            var image = new BlobBuilder(); new MetadataRootBuilder(metadata).Serialize(image, 0, 0);
            using var source = MetadataReaderProvider.FromMetadataImage(image.ToImmutableArray());
            Check(raw(source.GetMetadataReader().GetBlobReader(blob), out _, out _) == vector.Expected, "raw-" + vector.Hex);
        }
        // These finite reference-holder methods are metadata only, never executed.
        using (var refs = Mono.Cecil.AssemblyDefinition.ReadAssembly(input))
        {
            var context = Model("GenericContext").GetField("Empty", statics)!.GetValue(null)!;
            var resolve = Model("Compilation").GetMethod("ResolveCallTarget", instance)!;
            foreach (var holder in refs.MainModule.GetType("Fixture.References").Methods)
            {
                var reference = (Mono.Cecil.MethodReference)holder.Body.Instructions.Single(instruction => instruction.OpCode == Mono.Cecil.Cil.OpCodes.Call).Operand;
                bool accepted = false;
                try
                {
                    var resolved = Invoke(resolve, compilation, app, MetadataTokens.EntityHandle(reference.MetadataToken.ToInt32()), context);
                    accepted = resolved is not null && Accepted(resolved) && RowMatches(resolved);
                    if (holder.Name.StartsWith("Ref_valid-", StringComparison.Ordinal))
                    {
                        Check(reference.MetadataToken.TokenType == Mono.Cecil.TokenType.MemberRef, "actual-MemberRef-input");
                        var expected = actuals.Single(method => ((IEnumerable)Get(Get(method, "Signature"), "ParameterTypes")).Cast<object>().Count()
                            == reference.Parameters.Count);
                        Check(ReferenceEquals(resolved, expected), "MemberRef-exact-wrapper-identity-" + reference.Parameters.Count);
                        Check(resolved is not null && ReferenceEquals(Get(resolved, "Module"), core) && !(bool)Get(resolved, "IsSynthetic"),
                            "MemberRef-original-CoreLib-body-" + reference.Parameters.Count);
                        Console.WriteLine("array-sort-factory-memberref:parameters=" + reference.Parameters.Count + ";original-wrapper");
                    }
                }
                catch (NotSupportedException) { }
                Check(accepted == holder.Name.StartsWith("Ref_valid-", StringComparison.Ordinal), "MemberRef-" + holder.Name);
            }
        }
        // Exercise the same token facts and closure backstop emission uses, without
        // transpiling or running a target assembly in this metadata guard.
        var facts = (IDictionary)Activator.CreateInstance(Model("Compilation").GetField("HotBodyFacts", instance)!.FieldType)!;
        Set(compilation, "HotBodyFacts", facts);
        Set(compilation, "HotCallEdges", Activator.CreateInstance(Model("Compilation").GetField("HotCallEdges", instance)!.FieldType)!);
        var roots = (IList)Get(compilation, "NoAllocMethods");
        Check(roots.Count == 0, "NoAlloc-guard-clean-roots");
        var emitter = RuntimeHelpers.GetUninitializedObject(Model("CppEmitter"));
        Set(emitter, "_c", compilation);
        var verifyNoAlloc = Model("CppEmitter").GetMethod("AssertNoAllocClosures", instance)!;
        foreach (var probe in new[] {
            (Name: "FactoryOne", Body: "return dn2cpp_runtime_type_create_generic_instance_1(representative, argument);", Reject: true),
            (Name: "FactoryTwo", Body: "return dn2cpp_runtime_type_create_generic_instance_2(representative, first, second);", Reject: true),
            (Name: "Control", Body: "return nullptr;", Reject: false) })
        {
            var method = RuntimeHelpers.GetUninitializedObject(Model("MethodInfo"));
            string symbol = "ArraySortFactoryHotGuard_" + probe.Name;
            Set(method, "Name", probe.Name); Set(method, "DeclaringClass", Get(actuals[0], "DeclaringClass"));
            Set(method, "_cppName", symbol);
            Invoke(Model("Compilation").GetMethod("RecordHotBodyFacts", instance)!, compilation, method, probe.Body);
            var fact = facts[symbol]!;
            Check((bool)Get(fact, "Allocates") == probe.Reject && (bool)Get(fact, "Dispatches") == probe.Reject,
                "hot-allocation-dispatch-facts-" + probe.Name);
            if (probe.Reject)
                Check((string)Get(fact, "AllocToken") == "dn2cpp_runtime_type_create_generic_instance_"
                    && (string)Get(fact, "DispatchToken") == "dn2cpp_runtime_type_create_generic_instance_", "hot-factory-token-" + probe.Name);
            else Check(Get(fact, "AllocToken") is null && Get(fact, "DispatchToken") is null, "hot-control-no-token");
            roots.Clear(); roots.Add(method);
            bool rejected = false;
            try { Invoke(verifyNoAlloc, emitter, roots); }
            catch (NotSupportedException error)
            {
                rejected = true;
                Check(error.Message.Contains("[HotPath(NoAlloc)]", StringComparison.Ordinal)
                    && error.Message.Contains("dn2cpp_runtime_type_create_generic_instance_", StringComparison.Ordinal), "NoAlloc-diagnostic-" + probe.Name);
            }
            Check(rejected == probe.Reject, "NoAlloc-verdict-" + probe.Name);
        }
        roots.Clear();
        Console.WriteLine("array-sort-factory-noalloc:factory-one=rejected;factory-two=rejected;control=accepted");
        Console.WriteLine("array-sort-factory-guards:checks=" + checks);
        return true;
    }
}
