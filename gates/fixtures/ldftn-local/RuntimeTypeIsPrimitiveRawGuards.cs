using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;

internal static class RuntimeTypeIsPrimitiveRawGuards
{
    public static bool Run(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        if (args.Length != 2 || args[0] != "--runtime-type-is-primitive-raw-guards") return false;
        var production = Assembly.LoadFrom(Path.GetFullPath(args[1]));
        const BindingFlags fields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        const BindingFlags statics = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        Type Model(string name) => production.GetType("Dn2Cpp." + name, throwOnError: true)!;
        object Make(string name) => RuntimeHelpers.GetUninitializedObject(Model(name));
        void Set(object target, string name, object? value)
        {
            if (target.GetType().GetField(name, fields) is { } field) field.SetValue(target, value);
            else (target.GetType().GetProperty(name, fields)
                ?? throw new InvalidOperationException("Missing member " + name)).SetValue(target, value);
        }
        object Get(object target, string name) => target.GetType().GetField(name, fields) is { } field
            ? field.GetValue(target)!
            : target.GetType().GetProperty(name, fields)!.GetValue(target)!;
        object NewMap(Type owner, string field) => Activator.CreateInstance(owner.GetField(field, fields)!.FieldType)!;

        var helperPredicate = Model("CoreIntrinsics").GetMethod("IsRuntimeTypeHandleIsPrimitive", statics)!;
        var overridePredicate = Model("CoreIntrinsics").GetMethod("IsRuntimeTypeIsPrimitiveOverride", statics)!;
        var decode = typeof(MethodDefinition).GetMethods().Single(m => m.Name == "DecodeSignature")
            .MakeGenericMethod(Model("TypeDesc"), typeof(object));
        int failures = 0;

        // These are metadata-only MethodDefs, never CLR-loaded or executed. TypeDef rows
        // 2/3 are Boolean/RuntimeType, so their TypeDefOrRef encodings are 08/0c.
        foreach (var item in new (string Label, bool Helper, byte[] Signature, bool Expected)[]
        {
            ("helper-exact", true, new byte[] { 0x00, 0x01, 0x02, 0x12, 0x0c }, true),
            ("helper-valuetype-parameter", true, new byte[] { 0x00, 0x01, 0x02, 0x11, 0x0c }, false),
            ("helper-class-boolean-return", true, new byte[] { 0x00, 0x01, 0x12, 0x08, 0x12, 0x0c }, false),
            ("helper-modified-parameter", true, new byte[] { 0x00, 0x01, 0x02, 0x20, 0x0c, 0x12, 0x0c }, false),
            ("override-exact", false, new byte[] { 0x20, 0x00, 0x02 }, true),
            ("override-class-boolean-return", false, new byte[] { 0x20, 0x00, 0x12, 0x08 }, false),
            ("override-valuetype-boolean-return", false, new byte[] { 0x20, 0x00, 0x11, 0x08 }, false),
            ("override-modified-return", false, new byte[] { 0x20, 0x00, 0x20, 0x0c, 0x02 }, false),
        })
        {
            var metadata = new MetadataBuilder();
            metadata.AddModule(0, metadata.GetOrAddString("Guard.dll"), metadata.GetOrAddGuid(Guid.Empty), default, default);
            metadata.AddAssembly(metadata.GetOrAddString("System.Private.CoreLib"), new Version(10, 0, 0, 0),
                default, default, default, default);
            var firstField = MetadataTokens.FieldDefinitionHandle(1);
            var firstMethod = MetadataTokens.MethodDefinitionHandle(1);
            metadata.AddTypeDefinition(TypeAttributes.NotPublic, default, metadata.GetOrAddString("<Module>"),
                default, firstField, firstMethod);
            var boolean = metadata.AddTypeDefinition(TypeAttributes.Public | TypeAttributes.Sealed,
                metadata.GetOrAddString("System"), metadata.GetOrAddString("Boolean"), default, firstField, firstMethod);
            var runtimeType = metadata.AddTypeDefinition(TypeAttributes.NotPublic | TypeAttributes.Sealed,
                metadata.GetOrAddString("System"), metadata.GetOrAddString("RuntimeType"), default, firstField, firstMethod);
            var runtimeHandle = metadata.AddTypeDefinition(TypeAttributes.Public | TypeAttributes.Sealed,
                metadata.GetOrAddString("System"), metadata.GetOrAddString("RuntimeTypeHandle"), default,
                firstField, MetadataTokens.MethodDefinitionHandle(2));
            var getterSignature = item.Helper ? new byte[] { 0x20, 0x00, 0x02 } : item.Signature;
            var helperSignature = item.Helper ? item.Signature : new byte[] { 0x00, 0x01, 0x02, 0x12, 0x0c };
            var getterHandle = metadata.AddMethodDefinition(MethodAttributes.Family | MethodAttributes.Virtual | MethodAttributes.HideBySig,
                MethodImplAttributes.IL, metadata.GetOrAddString("IsPrimitiveImpl"), metadata.GetOrAddBlob(getterSignature),
                1, MetadataTokens.ParameterHandle(1));
            var helperHandle = metadata.AddMethodDefinition(MethodAttributes.Assembly | MethodAttributes.Static | MethodAttributes.HideBySig,
                MethodImplAttributes.IL, metadata.GetOrAddString("IsPrimitive"), metadata.GetOrAddBlob(helperSignature),
                1, MetadataTokens.ParameterHandle(1));
            metadata.AddParameter(ParameterAttributes.None, metadata.GetOrAddString("type"), 1);
            var blob = new BlobBuilder();
            new MetadataRootBuilder(metadata).Serialize(blob, 0, 0);
            using var source = MetadataReaderProvider.FromMetadataImage(blob.ToImmutableArray());
            var reader = source.GetMetadataReader();

            var compilation = Make("Compilation");
            var module = Make("Module");
            Set(module, "Reader", reader);
            Set(module, "AssemblyName", "System.Private.CoreLib");
            Set(module, "Owner", compilation);
            var classMap = (IDictionary)NewMap(Model("Module"), "ClassMap");
            Set(module, "ClassMap", classMap);
            object MakeClass(TypeDefinitionHandle handle, string fullName)
            {
                var cls = Make("ClassInfo");
                Set(cls, "Handle", handle);
                Set(cls, "Module", module);
                Set(cls, "_fullName", fullName);
                Set(cls, "IsSealed", true);
                classMap.Add(handle, cls);
                return cls;
            }
            MakeClass(boolean, "System.Boolean");
            var runtimeClass = MakeClass(runtimeType, "System.RuntimeType");
            var handleClass = MakeClass(runtimeHandle, "System.RuntimeTypeHandle");
            var readers = (IDictionary)NewMap(Model("Compilation"), "_byReader");
            readers.Add(reader, module);
            Set(compilation, "_byReader", readers);
            var signatureProvider = Activator.CreateInstance(Model("SignatureProvider"), fields, null,
                new[] { compilation }, null)!;
            var methodHandle = item.Helper ? helperHandle : getterHandle;
            var definition = reader.GetMethodDefinition(methodHandle);
            // Crucially use the candidate's real SignatureProvider, not constructed
            // TypeDesc objects: this is where raw class/value-kind/modifiers disappear.
            var signature = decode.Invoke(definition, new object?[] { signatureProvider, null })!;
            var method = Make("MethodInfo");
            Set(method, "DeclaringClass", item.Helper ? handleClass : runtimeClass);
            Set(method, "Module", module);
            Set(method, "Handle", methodHandle);
            Set(method, "Name", reader.GetString(definition.Name));
            Set(method, "Attributes", definition.Attributes);
            Set(method, "ImplAttributes", definition.ImplAttributes);
            Set(method, "Rva", definition.RelativeVirtualAddress);
            Set(method, "Signature", signature);
            bool accepted = (bool)(item.Helper ? helperPredicate : overridePredicate).Invoke(null, new[] { method })!;
            var resultType = Get(signature, "ReturnType");
            var parameters = ((IEnumerable)Get(signature, "ParameterTypes")).Cast<object>()
                .Select(type => Get(type, "Kind") + "/" + type);
            Console.WriteLine($"{item.Label}:raw={Convert.ToHexString(item.Signature)};decoded-return={Get(resultType, "Kind")}/{resultType};decoded-parameters={string.Join(",", parameters)};accepted={accepted};expected={item.Expected}");
            if (accepted != item.Expected) failures++;
        }
        if (failures != 0) throw new InvalidOperationException("Primitive raw declaration mismatches: " + failures);
        Console.WriteLine("runtime-type-is-primitive-raw-guards:True");
        return true;
    }
}
