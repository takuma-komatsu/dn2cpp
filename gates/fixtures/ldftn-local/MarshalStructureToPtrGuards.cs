using System.Collections;
using System.Collections.Immutable;
using System.Globalization;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;

internal static class MarshalStructureToPtrGuards
{
    private delegate bool RawPredicate(BlobReader blob);

    public static bool Run(string[] args)
    {
        if (args.Length != 4 || args[0] != "--marshal-structure-to-ptr-guards") return false;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        string input = Path.GetFullPath(args[3]);
        if (File.Exists(input)) throw new InvalidOperationException("model app must be new");
        using (var empty = Mono.Cecil.AssemblyDefinition.CreateAssembly(
            new Mono.Cecil.AssemblyNameDefinition("MarshalStructureGuardInput", new Version(1, 0)),
            "MarshalStructureGuardInput", Mono.Cecil.ModuleKind.Dll))
            empty.Write(input, new Mono.Cecil.WriterParameters { Timestamp = 0, DeterministicMvid = true });

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
        Set(options, "Input", input); Set(options, "UseILDiet", false);
        Set(options, "SharedGenerics", false); Set(options, "AutoRef", false);
        Set(options, "IsaSurfaceDump", "predicate-guard-only");
        var compilation = Invoke(Model("Compilation").GetMethod("Create", statics)!, null,
            new[] { input, Path.GetFullPath(args[2]) }, options);
        var modules = ((IEnumerable)Get(compilation, "Modules")).Cast<object>().ToArray();
        var core = modules.Single(m => (string)Get(m, "AssemblyName") == "System.Private.CoreLib");
        var app = modules.Single(m => (string)Get(m, "AssemblyName") == "MarshalStructureGuardInput");
        var reader = (MetadataReader)Get(core, "Reader");
        var ownerHandle = reader.TypeDefinitions.Single(h =>
        {
            var type = reader.GetTypeDefinition(h);
            return reader.GetString(type.Namespace) == "System.Runtime.InteropServices"
                && reader.GetString(type.Name) == "Marshal" && type.GetDeclaringType().IsNil;
        });
        var handles = reader.GetTypeDefinition(ownerHandle).GetMethods().ToArray();
        var actualHandle = handles.Single(h => reader.GetString(reader.GetMethodDefinition(h).Name) == "StructureToPtr"
            && reader.GetMethodDefinition(h).GetGenericParameters().Count == 0);
        var genericHandle = handles.Single(h => reader.GetString(reader.GetMethodDefinition(h).Name) == "StructureToPtr"
            && reader.GetMethodDefinition(h).GetGenericParameters().Count == 1);
        var actual = ((IDictionary)Get(core, "MethodMap"))[actualHandle]!;
        var owner = Get(actual, "DeclaringClass");
        var intrinsic = Model("CoreIntrinsics");
        var predicate = intrinsic.GetMethod("IsMarshalStructureToPtr", statics)!;
        var raw = intrinsic.GetMethod("IsMarshalStructureToPtrSignature", statics)!.CreateDelegate<RawPredicate>();
        var decoded = intrinsic.GetMethod("IsMarshalStructureToPtrDecodedSignature", statics)!;
        var row = intrinsic.GetField("MdMarshalStructureToPtr", statics)!.GetValue(null)!;
        bool Accepted(object method) => (bool)Invoke(predicate, null, method);
        bool RowMatches(object method) => (bool)Invoke(row.GetType().GetMethod("Matches", instance)!, row, method);
        int checks = 0;
        void Check(bool value, string label)
        {
            if (!value) throw new InvalidOperationException("Marshal StructureToPtr guard: " + label);
            checks++;
        }

        var definition = reader.GetMethodDefinition(actualHandle);
        Check(definition.Attributes == (MethodAttributes)0x0096
            && definition.ImplAttributes == MethodImplAttributes.IL && definition.RelativeVirtualAddress != 0
            && Convert.ToHexString(reader.GetBlobBytes(definition.Signature)) == "0003011C1802", "original-CoreLib-contract");
        Console.WriteLine("marshal-structure-to-ptr-actual-model:" + MetadataTokens.GetToken(actualHandle).ToString("X8", CultureInfo.InvariantCulture));
        Set(actual, "_sig", Activator.CreateInstance(Model("MethodInfo").GetProperty("Signature", instance)!.PropertyType));
        int classCount = ((IEnumerable)Get(compilation, "Classes")).Cast<object>().Count();
        for (int pass = 0; pass < 2; pass++)
        {
            Check(!(bool)Get(actual, "SignatureReady"), "cold-before");
            Check(Accepted(actual) && RowMatches(actual), "cold-exact-match");
            Check(!(bool)Get(actual, "SignatureReady"), "cold-after");
            Check(((IEnumerable)Get(compilation, "Classes")).Cast<object>().Count() == classCount, "no-model-growth");
        }
        Check(Get(row, "CutKind").ToString() == "Cut" && Get(row, "EmitArm").ToString() == "IntrinsicUnderDeclType", "cut-route-arm");
        var find = intrinsic.GetMethod("TryFindCutRow", statics)!;
        object?[] findArgs = { actual, null };
        Check((bool)Invoke(find, null, findArgs) && row.Equals(findArgs[1]), "registered-cut-row");
        Check(!(bool)Get(actual, "SignatureReady"), "registry-stays-cold");

        // Open generic methods live in GenericMethodTemplates, not MethodMap.
        // Model that original declaration without specializing or decoding it.
        var genericDefinition = reader.GetMethodDefinition(genericHandle);
        var genericSibling = Activator.CreateInstance(Model("MethodInfo"), true)!;
        Set(genericSibling, "DeclaringClass", owner); Set(genericSibling, "Module", core);
        Set(genericSibling, "Handle", genericHandle); Set(genericSibling, "Name", reader.GetString(genericDefinition.Name));
        Set(genericSibling, "Attributes", genericDefinition.Attributes);
        Set(genericSibling, "ImplAttributes", genericDefinition.ImplAttributes);
        Set(genericSibling, "Rva", genericDefinition.RelativeVirtualAddress);
        foreach (var handle in new[] { genericHandle,
            handles.Single(h => reader.GetString(reader.GetMethodDefinition(h).Name) == "IsPinnable"),
            handles.Single(h => reader.GetString(reader.GetMethodDefinition(h).Name) == "PtrToStructureHelper") })
        {
            var sibling = handle == genericHandle ? genericSibling : ((IDictionary)Get(core, "MethodMap"))[handle]!;
            bool ready = (bool)Get(sibling, "SignatureReady");
            Check(!Accepted(sibling) && !RowMatches(sibling), "genuine-sibling-rejected");
            Check((bool)Get(sibling, "SignatureReady") == ready, "sibling-no-decode");
        }

        object Primitive(PrimitiveTypeCode code) => Invoke(Model("TypeDesc").GetMethod("MakePrimitive", statics)!, null, code);
        var argument = Array.CreateInstance(Model("TypeDesc"), 1);
        argument.SetValue(Primitive(PrimitiveTypeCode.Int32), 0);
        var emptyArguments = Array.CreateInstance(Model("TypeDesc"), 0);
        object Context(bool type) => Activator.CreateInstance(Model("GenericContext"),
            new object[] { type ? argument : emptyArguments, type ? emptyArguments : argument })!;
        void Reject(object value, string field, object replacement, string label)
        {
            object saved = Get(value, field);
            Set(value, field, replacement);
            try { Check(!Accepted(actual) && !RowMatches(actual), label); }
            finally { Set(value, field, saved); }
            Check(Accepted(actual) && !(bool)Get(actual, "SignatureReady"), "restored-cold-" + label);
        }
        Reject(core, "AssemblyName", "Application.CoreLib", "foreign-assembly");
        Reject(owner, "Module", app, "foreign-owner-module");
        Reject(owner, "_fullName", "Application.Marshal", "wrong-owner-name");
        Reject(owner, "Handle", default(TypeDefinitionHandle), "nil-owner");
        Reject(owner, "GenericArity", 1, "generic-owner");
        Reject(owner, "IsValueType", true, "value-owner");
        Reject(owner, "IsEnum", true, "enum-owner");
        Reject(owner, "IsInterface", true, "interface-owner");
        Reject(owner, "IsByRefLike", true, "byref-like-owner");
        Reject(owner, "Context", Context(true), "owner-type-context");
        Reject(owner, "Context", Context(false), "owner-method-context");
        Reject(actual, "Name", "StructureToPtrHelper", "wrong-name");
        Reject(actual, "Module", app, "foreign-method-module");
        Reject(actual, "Handle", default(MethodDefinitionHandle), "nil-method");
        Reject(actual, "Rva", 0, "bodyless-method");
        Reject(actual, "Rva", definition.RelativeVirtualAddress + 1, "wrong-body-address");
        Reject(actual, "IsSynthetic", true, "synthetic-method");
        Reject(actual, "Context", Context(true), "method-type-context");
        Reject(actual, "Context", Context(false), "method-method-context");
        foreach (var attributes in new[] { (MethodAttributes)0x0091, (MethodAttributes)0x0086,
            (MethodAttributes)0x2096, (MethodAttributes)0x0896 })
            Reject(actual, "Attributes", attributes, "attributes-" + (int)attributes);
        foreach (var impl in new[] { MethodImplAttributes.InternalCall, MethodImplAttributes.Runtime,
            MethodImplAttributes.PreserveSig, MethodImplAttributes.Synchronized, MethodImplAttributes.NoInlining })
            Reject(actual, "ImplAttributes", impl, "implementation-" + impl);

        foreach (var vector in new (string Hex, bool Expected)[] {
            ("0003011C1802", true), ("2003011C1802", false), ("6003011C1802", false),
            ("0503011C1802", false), ("100103011C1802", false), ("0002011C18", false),
            ("0004011C180208", false), ("0003021C1802", false), ("0003010E1802", false),
            ("0003011C1902", false), ("0003011C1808", false), ("000301101C1802", false),
            ("0003011C101802", false), ("0003011C181002", false), ("000310011C1802", false),
            ("00030120051C1802", false), ("0003011C20051802", false), ("0003011C18200502", false),
            ("0003011C180200", false), ("0003011C180241", false), ("0003011C18", false),
            ("0003", false), ("00", false), ("", false) })
        {
            var metadata = new MetadataBuilder();
            metadata.AddModule(0, metadata.GetOrAddString("MarshalRaw"), metadata.GetOrAddGuid(Guid.Empty), default, default);
            var blob = metadata.GetOrAddBlob(Convert.FromHexString(vector.Hex));
            var image = new BlobBuilder(); new MetadataRootBuilder(metadata).Serialize(image, 0, 0);
            using var source = MetadataReaderProvider.FromMetadataImage(image.ToImmutableArray());
            Check(raw(source.GetMetadataReader().GetBlobReader(blob)) == vector.Expected, "raw-" + vector.Hex);
        }

        var signature = Get(actual, "Signature");
        Check((bool)Invoke(decoded, null, signature) && Accepted(actual), "prepared-exact-match");
        Set(actual, "Handle", genericHandle);
        try { Check(!Accepted(actual), "decoded-cache-cannot-authorize-generic-row"); }
        finally { Set(actual, "Handle", actualHandle); }
        var createArray = typeof(ImmutableArray).GetMethods().Single(m => m.Name == "Create"
            && m.IsGenericMethodDefinition && m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType.IsArray)
            .MakeGenericMethod(Model("TypeDesc"));
        object Signature(byte header, PrimitiveTypeCode result, int required, int generic, params object[] parameters)
        {
            var array = Array.CreateInstance(Model("TypeDesc"), parameters.Length);
            for (int i = 0; i < parameters.Length; i++) array.SetValue(parameters[i], i);
            var immutable = Invoke(createArray, null, array);
            return Activator.CreateInstance(signature.GetType(), new object[] { new SignatureHeader(header),
                Primitive(result), required, generic, immutable })!;
        }
        var obj = Primitive(PrimitiveTypeCode.Object);
        var pointer = Primitive(PrimitiveTypeCode.IntPtr);
        var boolean = Primitive(PrimitiveTypeCode.Boolean);
        void RejectDecoded(object invalid, string label)
        {
            Set(actual, "Signature", invalid);
            try { Check(!(bool)Invoke(decoded, null, invalid) && !Accepted(actual), label); }
            finally { Set(actual, "Signature", signature); }
        }
        foreach (var invalid in new[] {
            Signature(0x20, PrimitiveTypeCode.Void, 3, 0, obj, pointer, boolean),
            Signature(0x05, PrimitiveTypeCode.Void, 3, 0, obj, pointer, boolean),
            Signature(0x10, PrimitiveTypeCode.Void, 3, 1, obj, pointer, boolean),
            Signature(0, PrimitiveTypeCode.Boolean, 3, 0, obj, pointer, boolean),
            Signature(0, PrimitiveTypeCode.Void, 2, 0, obj, pointer),
            Signature(0, PrimitiveTypeCode.Void, 2, 0, obj, pointer, boolean),
            Signature(0, PrimitiveTypeCode.Void, 3, 0, Primitive(PrimitiveTypeCode.String), pointer, boolean),
            Signature(0, PrimitiveTypeCode.Void, 3, 0, obj, Primitive(PrimitiveTypeCode.UIntPtr), boolean),
            Signature(0, PrimitiveTypeCode.Void, 3, 0, obj, pointer, Primitive(PrimitiveTypeCode.Int32)) })
            RejectDecoded(invalid, "decoded-shape-rejected");

        object WithTypes(object result, object[] parameters)
        {
            var array = Array.CreateInstance(Model("TypeDesc"), parameters.Length);
            for (int i = 0; i < parameters.Length; i++) array.SetValue(parameters[i], i);
            return Activator.CreateInstance(signature.GetType(), new object[] { new SignatureHeader(0),
                result, 3, 0, Invoke(createArray, null, array) })!;
        }
        var codes = new[] { PrimitiveTypeCode.Void, PrimitiveTypeCode.Object, PrimitiveTypeCode.IntPtr, PrimitiveTypeCode.Boolean };
        for (int slot = 0; slot < codes.Length; slot++)
        foreach (bool canonical in new[] { false, true })
        {
            var types = codes.Select(Primitive).ToArray();
            var changed = RuntimeHelpers.GetUninitializedObject(Model("TypeDesc"));
            Set(changed, "Kind", Enum.Parse(Model("TypeKind"), canonical ? "Primitive" : "ByRef"));
            Set(changed, "Primitive", codes[slot]);
            Set(changed, "Element", types[slot]);
            Set(changed, "IsCanonPlaceholder", canonical);
            types[slot] = changed;
            RejectDecoded(WithTypes(types[0], types[1..]), "decoded-nonconcrete-slot-" + slot);
        }
        var objectHandle = reader.TypeDefinitions.Single(h =>
        {
            var type = reader.GetTypeDefinition(h);
            return reader.GetString(type.Namespace) == "System" && reader.GetString(type.Name) == "Object";
        });
        foreach (string kind in new[] { "Class", "External" })
        {
            var alias = RuntimeHelpers.GetUninitializedObject(Model("TypeDesc"));
            Set(alias, "Kind", Enum.Parse(Model("TypeKind"), kind));
            Set(alias, "Class", ((IDictionary)Get(core, "ClassMap"))[objectHandle]);
            Set(alias, "ExternalName", "System.Object");
            Set(alias, "ExternalModule", core);
            Set(alias, "ExternalAssembly", "System.Private.CoreLib");
            RejectDecoded(WithTypes(Primitive(PrimitiveTypeCode.Void), new[] { alias, pointer, boolean }), "decoded-object-alias-" + kind);
        }

        var context = Model("GenericContext").GetField("Empty", statics)!.GetValue(null)!;
        Check(Invoke(Model("Compilation").GetMethod("ResolveCallTarget", instance)!, compilation,
            core, (EntityHandle)actualHandle, context) is null, "MethodDef-call-cut");
        var note = Model("Compilation").GetMethod("NoteInterceptFtnTarget", instance)!;
        for (int pass = 0; pass < 2; pass++)
        {
            Invoke(note, compilation, actual);
            Check(((IEnumerable)Get(compilation, "InterceptFtnTargets")).Cast<object>().Count(m => ReferenceEquals(m, actual)) == 1,
                "callable-wrapper-registration");
            Check(((IEnumerable)Get(compilation, "Reachable")).Cast<object>().Any(m => ReferenceEquals(m, actual)), "callable-reachable");
        }
        Console.WriteLine("marshal-structure-to-ptr-guards:checks=" + checks);
        return true;
    }
}
