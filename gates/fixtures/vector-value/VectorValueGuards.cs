using System.Collections;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

internal static class VectorValueGuards
{
    public static void Run(string compiler, string core, string input, bool accept, bool mutations, bool contextRegression = false)
    {
        var production = Assembly.LoadFrom(Path.GetFullPath(compiler));
        const BindingFlags instance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        const BindingFlags statics = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        Type Model(string name) => production.GetType("Dn2Cpp." + name, true)!;
        object Get(object value, string name) => value.GetType().GetField(name, instance) is { } field ? field.GetValue(value)! : value.GetType().GetProperty(name, instance)!.GetValue(value)!;
        void Set(object value, string name, object? next)
        {
            if (value.GetType().GetField(name, instance) is { } field) field.SetValue(value, next);
            else value.GetType().GetProperty(name, instance)!.SetValue(value, next);
        }
        object Invoke(object? target, System.Reflection.MethodInfo method, params object[] values)
        {
            try { return method.Invoke(target, values)!; }
            catch (TargetInvocationException error) when (error.InnerException is not null) { throw error.InnerException; }
        }
        string inputMode;
        using (var source = new System.Reflection.PortableExecutable.PEReader(File.OpenRead(input)))
        {
            var sourceReader = source.GetMetadataReader();
            inputMode = sourceReader.GetString(sourceReader.GetModuleDefinition().Name).Replace("VectorValueGuardInput.", "", StringComparison.Ordinal);
        }
        // Prove the intended foreign identity independently before accepting an earlier loader refusal.
        VectorValueShapeProof.Verify(inputMode, input);
        string? conflictingType = inputMode switch
        {
            "constrained-foreign-equatable" => "System.IEquatable`1",
            "constrained-foreign-object" => "System.Object",
            "foreign-owner" => "System.Numerics.Vector`1",
            _ => null
        };
        var options = Activator.CreateInstance(Model("TranspileOptions"))!;
        Set(options, "Input", Path.GetFullPath(input)); Set(options, "UseILDiet", false); Set(options, "SharedGenerics", false);
        Set(options, "AutoRef", true); Set(options, "IsaSurfaceDump", "predicate-guard-only");
        object loaded;
        try { loaded = Invoke(null, Model("Compilation").GetMethod("Create", statics)!, new[] { Path.GetFullPath(input), Path.GetFullPath(core) }, options); }
        catch (NotSupportedException error) when (!accept && conflictingType is not null
            && error.Message == $"input type '{conflictingType}' in assembly 'VectorValueGuardInput' conflicts with a CoreLib/runtime type identity")
        {
            Console.WriteLine($"vector-value-reference mode={inputMode} refused=input-identity-before-family feature-validator-executed=false");
            Console.WriteLine(error.Message);
            return;
        }
        var modules = ((IEnumerable)Get(loaded, "Modules")).Cast<object>().ToArray();
        var app = modules.Single(x => (string)Get(x, "AssemblyName") == "VectorValueGuardInput");
        var coreModule = modules.Single(x => (string)Get(x, "AssemblyName") == "System.Private.CoreLib");
        var reader = (MetadataReader)Get(app, "Reader");
        bool scopeOnly = new[] { "bool", "char", "decimal", "int128", "uint128", "enum", "struct", "open", "fixed-owner", "foreign-owner" }.Contains(inputMode);
        var context = Model("GenericContext").GetField("Empty", statics)!.GetValue(null)!;
        var resolve = Model("Compilation").GetMethod("ResolveVectorValueReference", instance)!;
        var constrained = Model("Compilation").GetMethod("ValidateVectorValueConstrained", instance)!;
        var typeResolve = Model("Compilation").GetMethod("ResolveTypeTokenForScan", instance)!;
        var predicate = Model("CoreIntrinsics").GetMethod("IsVectorValueMethod", statics)!;
        int checks = 0;
        void Check(bool result, string label)
        { if (!result) throw new InvalidOperationException("vector-value-guard: " + label); checks++; }
        if (contextRegression && inputMode.Contains("context-spec-", StringComparison.Ordinal))
        {
            var ownerHandle = Enumerable.Range(1, reader.GetTableRowCount(TableIndex.TypeSpec)).Select(MetadataTokens.TypeSpecificationHandle)
                .Single(h => reader.GetBlobReader(reader.GetTypeSpecification(h).Signature).ReadByte() == 0x15);
            var ownerType = Invoke(loaded, typeResolve, app, (EntityHandle)ownerHandle, context);
            var owner = Get(ownerType, "Class");
            Invoke(loaded, Model("Compilation").GetMethod("EnsureShape", instance)!, owner);
            Check((bool)Invoke(null, Model("CoreIntrinsics").GetMethod("IsVectorValueClass", statics)!, owner), "context-spec-real-vector");
            var empty = Array.CreateInstance(Model("TypeDesc"), 0);
            var args = Array.CreateInstance(Model("TypeDesc"), 1); args.SetValue(ownerType, 0);
            bool methodVariable = inputMode.EndsWith("-mvar", StringComparison.Ordinal);
            var caller = Activator.CreateInstance(Model("GenericContext"), new object[] { methodVariable ? empty : args, methodVariable ? args : empty })!;
            var spec = MetadataTokens.MethodSpecificationHandle(1);
            var parent = reader.GetMemberReference((MemberReferenceHandle)reader.GetMethodSpecification(spec).Method).Parent;
            Check(ReferenceEquals(Get(Invoke(loaded, typeResolve, app, parent, caller), "Class"), owner), "context-spec-parent-resolves-exact-owner");
            try
            {
                Invoke(loaded, Model("Compilation").GetMethod("ResolveMethodSpec", instance)!, app, spec, caller);
                throw new InvalidOperationException("Contextual Vector value MethodSpec was admitted");
            }
            catch (NotSupportedException error) when (error.Message.Contains("Vector<T> value methods", StringComparison.Ordinal)) { checks++; }
            Console.WriteLine($"vector-value-context-spec mode={inputMode} zero-arity=true contextual-parent=true refused=feature-specific checks={checks}");
            return;
        }
        if (contextRegression)
        {
            var handle = reader.MemberReferences.Single(h => reader.StringComparer.Equals(reader.GetMemberReference(h).Name, "Equals"));
            var reference = reader.GetMemberReference(handle);
            var ownerType = Invoke(loaded, typeResolve, app, reference.Parent, context);
            var owner = Get(ownerType, "Class");
            Invoke(loaded, Model("Compilation").GetMethod("EnsureShape", instance)!, owner);
            var empty = Array.CreateInstance(Model("TypeDesc"), 0);
            var goodArgs = Array.CreateInstance(Model("TypeDesc"), 1); goodArgs.SetValue(ownerType, 0);
            var badArgs = Array.CreateInstance(Model("TypeDesc"), 1);
            badArgs.SetValue(((Array)Get(Get(owner, "Context"), "TypeArgs")).GetValue(0), 0);
            var good = Activator.CreateInstance(Model("GenericContext"), new object[] { empty, goodArgs })!;
            var bad = Activator.CreateInstance(Model("GenericContext"), new object[] { empty, badArgs })!;
            var actualResolver = Model("Compilation").GetMethod("ResolveMemberRefMethod", instance)!;
            var first = Invoke(loaded, actualResolver, app, handle, good);
            Check((bool)Invoke(null, predicate, first), "same-token-good-context");
            try
            {
                Invoke(loaded, actualResolver, app, handle, bad);
                throw new InvalidOperationException("Same-token cached method bypassed mismatched MethodArgs");
            }
            catch (NotSupportedException error) when (error.Message.Contains("Vector<T> value methods", StringComparison.Ordinal))
            { checks++; }
            var restored = Invoke(loaded, actualResolver, app, handle, good);
            Check(ReferenceEquals(first, restored), "same-token-owner-stable-after-rejection");
            Console.WriteLine($"vector-value-context-cache same-token=true same-owner=true good-bad-good=true checks={checks}");
            return;
        }
        var accepted = new List<object>();
        var vectors = new List<object>();
        if (accept)
        {
            for (int row = 1; row <= reader.GetTableRowCount(TableIndex.TypeSpec); row++)
            {
                var desc = Invoke(loaded, typeResolve, app, (EntityHandle)MetadataTokens.TypeSpecificationHandle(row), context);
                if (desc is null || Get(desc, "Class") is not { } cls) continue;
                Invoke(loaded, Model("Compilation").GetMethod("EnsureShape", instance)!, cls);
                if ((bool)Invoke(null, Model("CoreIntrinsics").GetMethod("IsVectorValueClass", statics)!, cls)) vectors.Add(cls);
            }
        }
        int references = 0;
        var constrainedReceivers = new Dictionary<int, EntityHandle>();
        // Read only our generated IL tokens; do not let Cecil normalize or decode malformed signatures.
        using (var pe = new System.Reflection.PortableExecutable.PEReader(File.OpenRead(input)))
        foreach (var methodHandle in reader.MethodDefinitions)
        {
            var method = reader.GetMethodDefinition(methodHandle);
            if (method.RelativeVirtualAddress == 0) continue;
            var il = pe.GetMethodBody(method.RelativeVirtualAddress).GetILBytes()!;
            foreach (int i in VectorValueShapeProof.InstructionOffsets(il))
                if (i + 10 < il.Length && il[i] == 0xfe && il[i + 1] == 0x16 && il[i + 6] == 0x6f)
                {
                    var receiverHandle = MetadataTokens.EntityHandle(BitConverter.ToInt32(il, i + 2));
                    constrainedReceivers[BitConverter.ToInt32(il, i + 7)] = receiverHandle;
                    if (accept)
                    {
                        var receiver = Get(Invoke(loaded, typeResolve, app, receiverHandle, context), "Class");
                        Invoke(loaded, Model("Compilation").GetMethod("ValidateVectorValueReceiver", instance)!, app, receiverHandle, context, receiver);
                        checks++;
                    }
                }
        }
        if (reader.GetTableRowCount(TableIndex.MethodSpec) != 0)
        {
            Check(!accept, "unexpected-positive-methodspec");
            for (int row = 1; row <= reader.GetTableRowCount(TableIndex.MethodSpec); row++)
            {
                try
                {
                    Invoke(loaded, Model("Compilation").GetMethod("RejectVectorValueMethodSpecification", instance)!, app, MetadataTokens.MethodSpecificationHandle(row), context);
                    throw new InvalidOperationException("Vector value MethodSpec was admitted");
                }
                catch (NotSupportedException error) when (error.Message.Contains("Vector<T> value methods", StringComparison.Ordinal))
                { checks++; Console.WriteLine("vector-value-methodspec refused=feature-specific"); }
            }
            Console.WriteLine($"vector-value-guards expected=reject methodspecs={reader.GetTableRowCount(TableIndex.MethodSpec)} checks={checks}");
            return;
        }
        foreach (var handle in reader.MemberReferences)
        {
            var reference = reader.GetMemberReference(handle);
            string name = reader.GetString(reference.Name);
            if (name is not ("Equals" or "GetHashCode")) continue;
            references++;
            string raw = Convert.ToHexString(reader.GetBlobBytes(reference.Signature));
            try
            {
                object result;
                if (!accept && constrainedReceivers.TryGetValue(MetadataTokens.GetToken(handle), out var receiverHandle))
                {
                    var receiverDesc = Invoke(loaded, typeResolve, app, receiverHandle, context);
                    var receiver = Get(receiverDesc, "Class");
                    Invoke(loaded, Model("Compilation").GetMethod("ValidateVectorValueReceiver", instance)!, app, receiverHandle, context, receiver);
                    result = Invoke(loaded, constrained, app, (EntityHandle)handle, context, receiver);
                    Check(false, "malformed-constrained-reference-admitted");
                    continue;
                }
                if (accept && reference.Parent.Kind != HandleKind.TypeSpecification)
                {
                    foreach (var receiver in vectors)
                    {
                        result = Invoke(loaded, constrained, app, (EntityHandle)handle, context, receiver);
                        Check((bool)Invoke(null, predicate, result), "constrained-object-" + name); accepted.Add(result);
                    }
                    continue;
                }
                if (accept)
                {
                    var desc = Invoke(loaded, typeResolve, app, reference.Parent, context);
                    var owner = Get(desc, "Class");
                    if ((string)Get(owner, "FullName") is var fullName && fullName.StartsWith("System.IEquatable", StringComparison.Ordinal))
                    {
                        var arg = ((Array)Get(Get(owner, "Context"), "TypeArgs")).GetValue(0)!;
                        result = constrainedReceivers.ContainsKey(MetadataTokens.GetToken(handle))
                            ? Invoke(loaded, constrained, app, (EntityHandle)handle, context, Get(arg, "Class"))
                            : Invoke(loaded, Model("Compilation").GetMethod("ResolveMemberRefMethod", instance)!, app, handle, context);
                    }
                    else result = Invoke(loaded, Model("Compilation").GetMethod("ResolveMemberRefMethod", instance)!, app, handle, context);
                }
                else result = Invoke(loaded, scopeOnly ? resolve : Model("Compilation").GetMethod("ResolveMemberRefMethod", instance)!, app, handle, context);
                Check(accept, "malformed-reference-admitted token=" + MetadataTokens.GetToken(handle).ToString("X8") + " raw=" + raw);
                Check((bool)Invoke(null, predicate, result)
                    || (bool)Invoke(null, Model("CoreIntrinsics").GetMethod("IsVectorValueEquatableMethod", statics)!, result), "resolved-exact-declaration"); accepted.Add(result);
                Console.WriteLine("vector-value-reference accepted=" + MetadataTokens.GetToken(handle).ToString("X8") + " raw=" + raw);
            }
            catch (NotSupportedException error) when (!accept && error.Message.Contains("Vector<T> value methods", StringComparison.Ordinal))
            { checks++; Console.WriteLine("vector-value-reference refused=feature-specific raw=" + raw); }
        }
        Check(references != 0, "references-present");
        if (accept && mutations)
        {
            Check(vectors.Distinct().Count() == 12, "twelve-numeric-closures");
            Check(accepted.Count >= 36, "three-method-family");
            foreach (object method in accepted.Distinct())
            {
                var owner = Get(method, "DeclaringClass");
                var element = ((Array)Get(Get(owner, "Context"), "TypeArgs")).GetValue(0)!;
                void Reject(object value, string field, object next)
                {
                    object saved = Get(value, field); Set(value, field, next);
                    try { Check(!(bool)Invoke(null, predicate, method), "mutation-" + field); }
                    finally { Set(value, field, saved); }
                }
                Reject(coreModule, "AssemblyName", "Foreign.CoreLib");
                Reject(owner, "Module", app); Reject(owner, "Handle", default(TypeDefinitionHandle));
                Reject(owner, "IntrinsicCppName", "Dn2CppVector128"); Reject(owner, "IsValueType", false);
                Reject(owner, "IsEnum", true); Reject(owner, "IsInterface", true); Reject(owner, "GenericArity", 0);
                Reject(method, "Module", app); Reject(method, "Handle", default(MethodDefinitionHandle));
                Reject(method, "IsSynthetic", true); Reject(method, "Rva", 0); Reject(method, "Name", "EqualsAll");
                Reject(method, "Attributes", (MethodAttributes)Get(method, "Attributes") | MethodAttributes.Static);
                Reject(method, "ImplAttributes", MethodImplAttributes.InternalCall);
                foreach (var code in new[] { PrimitiveTypeCode.Boolean, PrimitiveTypeCode.Char, PrimitiveTypeCode.Object, PrimitiveTypeCode.String, PrimitiveTypeCode.Void }) Reject(element, "Primitive", code);
                foreach (string kind in new[] { "Class", "GenericVar", "Template", "ByRef", "Pointer" }) Reject(element, "Kind", Enum.Parse(Model("TypeKind"), kind));
                Reject(element, "IsCanonPlaceholder", true);
                var empty = Array.CreateInstance(Model("TypeDesc"), 0);
                var one = Array.CreateInstance(Model("TypeDesc"), 1); one.SetValue(element, 0);
                Reject(owner, "Context", Activator.CreateInstance(Model("GenericContext"), new object[] { empty, empty })!);
                var wrongOne = Array.CreateInstance(Model("TypeDesc"), 1);
                wrongOne.SetValue(((Array)Get(Get(vectors.First(v => !ReferenceEquals(v, owner)), "Context"), "TypeArgs")).GetValue(0), 0);
                Reject(method, "Context", Activator.CreateInstance(Model("GenericContext"), new object[] { wrongOne, empty })!);
                Reject(method, "Context", Activator.CreateInstance(Model("GenericContext"), new object[] { one, one })!);
                var signature = Get(method, "Signature");
                Reject(Get(signature, "ReturnType"), "Kind", Enum.Parse(Model("TypeKind"), "ByRef"));
                foreach (object parameter in ((IEnumerable)Get(signature, "ParameterTypes"))) Reject(parameter, "Kind", Enum.Parse(Model("TypeKind"), "ByRef"));
                Check((bool)Invoke(null, predicate, method), "restored-exact-declaration");
            }
        }
        Console.WriteLine($"vector-value-guards expected={(accept ? "accept" : "reject")} references={references} checks={checks}");
    }
}
