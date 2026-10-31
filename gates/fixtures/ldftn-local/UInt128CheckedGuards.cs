using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

internal static class UInt128CheckedGuards
{
    private delegate bool RawPredicate(BlobReader blob, bool interfaceForm);

    public static bool Run(string[] args)
    {
        if (args.Length != 4 || args[0] != "--uint128-checked-guards") return false;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        var production = Assembly.LoadFrom(Path.GetFullPath(args[1]));
        const BindingFlags instance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        const BindingFlags statics = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        Type Model(string name) => production.GetType("Dn2Cpp." + name, true)!;
        object Get(object value, string name) => value.GetType().GetField(name, instance) is { } field ? field.GetValue(value)! : value.GetType().GetProperty(name, instance)!.GetValue(value)!;
        void Set(object value, string name, object? next)
        {
            if (value.GetType().GetField(name, instance) is { } field) field.SetValue(value, next);
            else value.GetType().GetProperty(name, instance)!.SetValue(value, next);
        }
        var options = Activator.CreateInstance(Model("TranspileOptions"))!;
        Set(options, "Input", Path.GetFullPath(args[3])); Set(options, "UseILDiet", false);
        Set(options, "SharedGenerics", false); Set(options, "AutoRef", false); Set(options, "IsaSurfaceDump", "predicate-guard-only");
        object loaded = Model("Compilation").GetMethod("Create", statics)!.Invoke(null,
            new object[] { new[] { Path.GetFullPath(args[3]), Path.GetFullPath(args[2]) }, options })!;
        var modules = ((IEnumerable)Get(loaded, "Modules")).Cast<object>().ToArray();
        var module = modules.Single(m => (string)Get(m, "AssemblyName") == "System.Private.CoreLib");
        var appModule = modules.Single(m => !ReferenceEquals(m, module));
        var reader = (MetadataReader)Get(module, "Reader");
        var ownerHandle = reader.TypeDefinitions.Single(h => reader.GetString(reader.GetTypeDefinition(h).Name) == "UInt128");
        var owner = ((IDictionary)Get(module, "ClassMap"))[ownerHandle]!;
        var handle = reader.GetTypeDefinition(ownerHandle).GetMethods().Single(h => reader.GetString(reader.GetMethodDefinition(h).Name) == "CreateChecked");
        var primitive = Model("TypeDesc").GetMethod("MakePrimitive", statics)!;
        var source = primitive.Invoke(null, new object[] { PrimitiveTypeCode.Int64 })!;
        var sources = Array.CreateInstance(Model("TypeDesc"), 1); sources.SetValue(source, 0);
        var target = Model("Compilation").GetMethod("InstantiateMethodOnClass", instance)!.Invoke(loaded, new object[] { owner, module, handle, sources })!;
        var predicate = Model("CoreIntrinsics").GetMethod("IsUInt128Checked", statics)!;
        var fields = Model("CoreIntrinsics").GetMethod("HasUInt128CheckedFields", statics)!;
        var raw = Model("CoreIntrinsics").GetMethod("IsUInt128CheckedSignature", statics)!.CreateDelegate<RawPredicate>();
        int checks = 0;
        void Check(bool good, string label) { if (!good) throw new InvalidOperationException("UInt128 checked guard: " + label); checks++; }
        bool Accepted() => (bool)predicate.Invoke(null, new[] { target })!;
        Check(Accepted(), "actual closed method");
        var uint128 = Model("TypeDesc").GetMethod("MakeClass", statics)!.Invoke(null, new[] { owner })!;
        var identityArgs = Array.CreateInstance(Model("TypeDesc"), 1); identityArgs.SetValue(uint128, 0);
        var identity = Model("Compilation").GetMethod("InstantiateMethodOnClass", instance)!.Invoke(loaded, new object[] { owner, module, handle, identityArgs })!;
        Check((bool)predicate.Invoke(null, new[] { identity })!, "actual identity");
        foreach (string excluded in new[] { "Int128", "Half", "Decimal", "DayOfWeek" })
        {
            var excludedHandle = reader.TypeDefinitions.Single(h => reader.GetString(reader.GetTypeDefinition(h).Name) == excluded);
            var excludedOwner = ((IDictionary)Get(module, "ClassMap"))[excludedHandle]!;
            var excludedType = Model("TypeDesc").GetMethod("MakeClass", statics)!.Invoke(null, new[] { excludedOwner })!;
            var excludedArgs = Array.CreateInstance(Model("TypeDesc"), 1); excludedArgs.SetValue(excludedType, 0);
            var excludedMethod = Model("Compilation").GetMethod("InstantiateMethodOnClass", instance)!.Invoke(loaded,
                new object[] { owner, module, handle, excludedArgs })!;
            Check(!(bool)predicate.Invoke(null, new[] { excludedMethod })!, "excluded class " + excluded);
        }
        var interfaceHandle = reader.TypeDefinitions.Single(h => reader.GetString(reader.GetTypeDefinition(h).Name) == "INumberBase`1");
        var interfaceClass = Model("Compilation").GetMethod("Instantiate", instance)!.Invoke(loaded, new object[] { module, interfaceHandle, identityArgs })!;
        var interfaceMethod = reader.GetTypeDefinition(interfaceHandle).GetMethods().Single(h => reader.GetString(reader.GetMethodDefinition(h).Name) == "CreateChecked");
        var closedInterface = Model("Compilation").GetMethod("InstantiateMethodOnClass", instance)!.Invoke(loaded, new object[] { interfaceClass, module, interfaceMethod, sources })!;
        Check((bool)Model("CoreIntrinsics").GetMethod("IsUInt128CheckedInterface", statics)!.Invoke(null, new[] { closedInterface })!, "actual closed interface");
        var concrete = Model("Compilation").GetMethod("ResolveUInt128CheckedInterface", instance)!.Invoke(loaded, new[] { closedInterface, owner })!;
        Check(ReferenceEquals(concrete, target), "interface resolves exact concrete authority");
        Check((bool)fields.Invoke(null, new[] { owner })!, "actual fields");
        void Reject(object obj, string name, object? value)
        {
            object old = Get(obj, name);
            try { Set(obj, name, value); Check(!Accepted(), name); }
            finally { Set(obj, name, old); }
        }
        Reject(module, "AssemblyName", "UserAssembly"); Reject(owner, "Module", appModule);
        Reject(owner, "IsValueType", false); Reject(owner, "IsEnum", true); Reject(owner, "IsInterface", true);
        Reject(owner, "GenericArity", 1); Reject(owner, "Handle", default(TypeDefinitionHandle));
        Reject(target, "Module", appModule); Reject(target, "Handle", default(MethodDefinitionHandle));
        Reject(target, "Name", "CreateSaturating"); Reject(target, "Rva", 0);
        foreach (var flags in new[] { MethodAttributes.Public, MethodAttributes.Public | MethodAttributes.Static,
            MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig | MethodAttributes.PinvokeImpl })
            Reject(target, "Attributes", flags);
        foreach (var flags in new[] { MethodImplAttributes.IL, MethodImplAttributes.InternalCall, MethodImplAttributes.NoInlining })
            Reject(target, "ImplAttributes", flags);
        foreach (var code in new[] { PrimitiveTypeCode.Boolean, PrimitiveTypeCode.Single, PrimitiveTypeCode.Double, PrimitiveTypeCode.Object })
            Reject(source, "Primitive", code);
        Reject(source, "IsCanonPlaceholder", true);
        foreach (var code in new[] { PrimitiveTypeCode.SByte, PrimitiveTypeCode.Byte, PrimitiveTypeCode.Int16, PrimitiveTypeCode.UInt16,
            PrimitiveTypeCode.Int32, PrimitiveTypeCode.UInt32, PrimitiveTypeCode.Int64, PrimitiveTypeCode.UInt64,
            PrimitiveTypeCode.IntPtr, PrimitiveTypeCode.UIntPtr, PrimitiveTypeCode.Char })
        {
            Set(source, "Primitive", code); Check(Accepted(), "supported " + code);
        }
        Set(source, "Primitive", PrimitiveTypeCode.Int64);
        foreach (var vector in new (string Hex, bool Interface, bool Expected)[] {
            ("10010111081E00",false,true), ("10010111061E00",false,false), ("10010113001E00",true,true),
            ("30010111081E00",false,false), ("10000111081E00",false,false), ("10020111081E00",false,false),
            ("10010011081E00",false,false), ("10010112081E00",false,false), ("1001011011081E00",false,false),
            ("100101200811081E00",false,false), ("10010111001E00",false,false), ("10010111071E00",false,false),
            ("10010111081E01",false,false), ("10010111081300",false,false), ("10010111081E0000",false,false),
            ("10010113011E00",true,false), ("10010113001E01",true,false), ("",false,false) })
        {
            var metadata = new MetadataBuilder(); metadata.AddModule(0, metadata.GetOrAddString("Raw"), metadata.GetOrAddGuid(Guid.Empty), default, default);
            var blob = metadata.GetOrAddBlob(Convert.FromHexString(vector.Hex)); var image = new BlobBuilder();
            new MetadataRootBuilder(metadata).Serialize(image, 0, 0);
            using var provider = MetadataReaderProvider.FromMetadataImage(image.ToImmutableArray());
            Check(raw(provider.GetMetadataReader().GetBlobReader(blob), vector.Interface) == vector.Expected, "raw " + vector.Hex);
        }
        // Field tables are metadata-only negatives based on the actual positive layout.
        foreach (string mode in new[] { "valid", "lower-type", "upper-static", "lower-public", "upper-mutable", "extra", "duplicate", "explicit" })
        {
            var metadata = new MetadataBuilder();
            metadata.AddModule(0, metadata.GetOrAddString("Layout"), metadata.GetOrAddGuid(Guid.Empty), default, default);
            var layout = mode == "explicit" ? TypeAttributes.ExplicitLayout : TypeAttributes.SequentialLayout;
            var newOwner = metadata.AddTypeDefinition(TypeAttributes.Public | layout, metadata.GetOrAddString("System"), metadata.GetOrAddString("UInt128"), default,
                MetadataTokens.FieldDefinitionHandle(1), MetadataTokens.MethodDefinitionHandle(1));
            var attributes = FieldAttributes.Private | FieldAttributes.InitOnly;
            metadata.AddFieldDefinition(mode == "lower-public" ? FieldAttributes.Public | FieldAttributes.InitOnly : attributes,
                metadata.GetOrAddString("_lower"), metadata.GetOrAddBlob(new byte[] { 6, mode == "lower-type" ? (byte)10 : (byte)11 }));
            metadata.AddFieldDefinition(mode == "upper-static" ? attributes | FieldAttributes.Static : mode == "upper-mutable" ? FieldAttributes.Private : attributes,
                metadata.GetOrAddString(mode == "duplicate" ? "_lower" : "_upper"), metadata.GetOrAddBlob(new byte[] { 6, 11 }));
            if (mode == "extra") metadata.AddFieldDefinition(attributes, metadata.GetOrAddString("extra"), metadata.GetOrAddBlob(new byte[] { 6, 11 }));
            var image = new BlobBuilder(); new MetadataRootBuilder(metadata).Serialize(image, 0, 0);
            using var provider = MetadataReaderProvider.FromMetadataImage(image.ToImmutableArray());
            try
            {
                Set(module, "Reader", provider.GetMetadataReader()); Set(owner, "Handle", newOwner);
                Check((bool)fields.Invoke(null, new[] { owner })! == (mode == "valid"), "layout " + mode);
            }
            finally { Set(module, "Reader", reader); Set(owner, "Handle", ownerHandle); }
        }
        // Mutate exact loaded signature/constraint blobs without normalizing MemberRefs
        // or writing ReadyToRun method bodies through another assembly writer.
        byte[] original = File.ReadAllBytes(args[2]);
        using var originalPe = new PEReader(new MemoryStream(original));
        int BlobStart(BlobHandle blob, byte[] bytes)
        {
            int offset = originalPe.PEHeaders.MetadataStartOffset + reader.GetHeapMetadataOffset(HeapIndex.Blob) + MetadataTokens.GetHeapOffset(blob);
            return offset + (bytes[offset] < 0x80 ? 1 : bytes[offset] < 0xc0 ? 2 : 4);
        }
        var signatureBlob = reader.GetMethodDefinition(handle).Signature;
        var generic = reader.GetGenericParameter(reader.GetMethodDefinition(handle).GetGenericParameters().Single());
        var constraintHandle = (TypeSpecificationHandle)reader.GetGenericParameterConstraint(generic.GetConstraints().Single()).Type;
        var constraintBlob = reader.GetTypeSpecification(constraintHandle).Signature;
        foreach (string mode in new[] { "return-token", "mvar", "constraint" })
        {
            var changedBytes = (byte[])original.Clone();
            if (mode == "return-token") changedBytes[BlobStart(signatureBlob, changedBytes) + 5] ^= 4;
            else if (mode == "mvar") changedBytes[BlobStart(signatureBlob, changedBytes) + reader.GetBlobBytes(signatureBlob).Length - 1] = 1;
            else changedBytes[BlobStart(constraintBlob, changedBytes) + reader.GetBlobBytes(constraintBlob).Length - 1] = 1;
            using var pe = new PEReader(new MemoryStream(changedBytes));
            try { Set(module, "Reader", pe.GetMetadataReader()); Check(!Accepted(), "metadata " + mode); }
            finally { Set(module, "Reader", reader); }
        }
        Check(Accepted(), "restored actual");
        Console.WriteLine("uint128-checked-guards:" + checks);
        return true;
    }
}
