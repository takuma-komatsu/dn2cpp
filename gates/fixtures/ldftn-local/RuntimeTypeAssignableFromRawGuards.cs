using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;

internal static class RuntimeTypeAssignableFromRawGuards
{
    public static bool Run(string[] args)
    {
        if (args.Length != 2 || args[0] != "--runtime-type-assignable-from-raw-guards") return false;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
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
        var predicate = Model("CoreIntrinsics").GetMethod("IsRuntimeTypeAssignableFrom", statics)!;
        var shape = Model("Compilation").GetMethod("RuntimeTypeAssignableFromMemberRefShape", statics)!;
        var decode = typeof(MethodDefinition).GetMethods().Single(m => m.Name == "DecodeSignature")
            .MakeGenericMethod(Model("TypeDesc"), typeof(object));
        int checks = 0;
        void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("Assignable-from raw guard: " + message);
            checks++;
        }

        // Metadata-only declarations, never CLR-loaded or executed. The production
        // decoder erases modifiers/raw class-value distinctions and normalizes Boolean.
        // TypeDef rows 2/3/4/5 encode Boolean/RuntimeType/Type/TypeInfo as 08/0c/10/14.
        foreach (var item in new (string Label, byte[] Signature, bool Expected)[]
        {
            ("exact", new byte[] { 0x20, 1, 2, 0x12, 0x10 }, true),
            ("valuetype-parameter", new byte[] { 0x20, 1, 2, 0x11, 0x10 }, false),
            ("byref-parameter", new byte[] { 0x20, 1, 2, 0x10, 0x12, 0x10 }, false),
            ("modified-parameter", new byte[] { 0x20, 1, 2, 0x20, 0x10, 0x12, 0x10 }, false),
            ("required-modified-parameter", new byte[] { 0x20, 1, 2, 0x1f, 0x10, 0x12, 0x10 }, false),
            ("named-class-boolean", new byte[] { 0x20, 1, 0x12, 0x08, 0x12, 0x10 }, false),
            ("named-value-boolean", new byte[] { 0x20, 1, 0x11, 0x08, 0x12, 0x10 }, false),
            ("modified-boolean", new byte[] { 0x20, 1, 0x20, 0x10, 2, 0x12, 0x10 }, false),
            ("static", new byte[] { 0x00, 1, 2, 0x12, 0x10 }, false),
            ("explicit-this", new byte[] { 0x60, 1, 2, 0x12, 0x10 }, false),
            ("varargs", new byte[] { 0x25, 1, 2, 0x12, 0x10 }, false),
            ("generic", new byte[] { 0x30, 1, 1, 2, 0x12, 0x10 }, false),
            ("object-parameter", new byte[] { 0x20, 1, 2, 0x1c }, false),
            ("runtime-type-parameter", new byte[] { 0x20, 1, 2, 0x12, 0x0c }, false),
            ("typeinfo-overload", new byte[] { 0x20, 1, 2, 0x12, 0x14 }, false),
            ("missing-parameter", new byte[] { 0x20, 0, 2 }, false),
            ("extra-parameter", new byte[] { 0x20, 2, 2, 0x12, 0x10, 0x12, 0x10 }, false),
            ("trailing-token", new byte[] { 0x20, 1, 2, 0x12, 0x10, 0x10 }, false),
        })
        {
            var metadata = new MetadataBuilder();
            metadata.AddModule(0, metadata.GetOrAddString("Guard.dll"), metadata.GetOrAddGuid(Guid.Empty), default, default);
            metadata.AddAssembly(metadata.GetOrAddString("System.Private.CoreLib"), new Version(10, 0, 0, 0), default, default, default, default);
            var firstField = MetadataTokens.FieldDefinitionHandle(1);
            var firstMethod = MetadataTokens.MethodDefinitionHandle(1);
            metadata.AddTypeDefinition(TypeAttributes.NotPublic, default, metadata.GetOrAddString("<Module>"), default, firstField, firstMethod);
            var boolean = metadata.AddTypeDefinition(TypeAttributes.Public | TypeAttributes.Sealed,
                metadata.GetOrAddString("System"), metadata.GetOrAddString("Boolean"), default, firstField, firstMethod);
            var runtimeType = metadata.AddTypeDefinition(TypeAttributes.NotPublic | TypeAttributes.Sealed,
                metadata.GetOrAddString("System"), metadata.GetOrAddString("RuntimeType"), default, firstField, firstMethod);
            var nextMethod = MetadataTokens.MethodDefinitionHandle(2);
            var type = metadata.AddTypeDefinition(TypeAttributes.Public | TypeAttributes.Abstract,
                metadata.GetOrAddString("System"), metadata.GetOrAddString("Type"), default, firstField, nextMethod);
            var typeInfo = metadata.AddTypeDefinition(TypeAttributes.Public | TypeAttributes.Abstract,
                metadata.GetOrAddString("System.Reflection"), metadata.GetOrAddString("TypeInfo"), default, firstField, nextMethod);
            var handle = metadata.AddMethodDefinition(MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig,
                MethodImplAttributes.IL, metadata.GetOrAddString("IsAssignableFrom"), metadata.GetOrAddBlob(item.Signature), 1,
                MetadataTokens.ParameterHandle(1));
            metadata.AddParameter(ParameterAttributes.None, metadata.GetOrAddString("source"), 1);
            var image = new BlobBuilder();
            new MetadataRootBuilder(metadata).Serialize(image, 0, 0);
            using var source = MetadataReaderProvider.FromMetadataImage(image.ToImmutableArray());
            var reader = source.GetMetadataReader();
            var compilation = Make("Compilation");
            var module = Make("Module");
            Set(module, "Reader", reader);
            Set(module, "AssemblyName", "System.Private.CoreLib");
            Set(module, "Owner", compilation);
            var classMap = (IDictionary)NewMap(Model("Module"), "ClassMap");
            Set(module, "ClassMap", classMap);
            object MakeClass(TypeDefinitionHandle definition, string name)
            {
                var cls = Make("ClassInfo");
                Set(cls, "Handle", definition);
                Set(cls, "Module", module);
                Set(cls, "_fullName", name);
                Set(cls, "IsSealed", definition == runtimeType || definition == boolean);
                classMap.Add(definition, cls);
                return cls;
            }
            MakeClass(boolean, "System.Boolean");
            var owner = MakeClass(runtimeType, "System.RuntimeType");
            MakeClass(type, "System.Type");
            MakeClass(typeInfo, "System.Reflection.TypeInfo");
            var readers = (IDictionary)NewMap(Model("Compilation"), "_byReader");
            readers.Add(reader, module);
            Set(compilation, "_byReader", readers);
            var provider = Activator.CreateInstance(Model("SignatureProvider"), fields, null, new[] { compilation }, null)!;
            var definition = reader.GetMethodDefinition(handle);
            var signature = decode.Invoke(definition, new object?[] { provider, null })!;
            var method = Make("MethodInfo");
            Set(method, "DeclaringClass", owner);
            Set(method, "Module", module);
            Set(method, "Handle", handle);
            Set(method, "Name", reader.GetString(definition.Name));
            Set(method, "Attributes", definition.Attributes);
            Set(method, "ImplAttributes", definition.ImplAttributes);
            Set(method, "Rva", definition.RelativeVirtualAddress);
            Set(method, "Signature", signature);
            bool accepted = (bool)predicate.Invoke(null, new[] { method })!;
            var result = Get(signature, "ReturnType");
            var parameters = ((IEnumerable)Get(signature, "ParameterTypes")).Cast<object>()
                .Select(parameter => Get(parameter, "Kind") + "/" + parameter);
            Console.WriteLine($"{item.Label}:raw={Convert.ToHexString(item.Signature)};decoded-return={Get(result, "Kind")}/{result};decoded-parameters={string.Join(",", parameters)};accepted={accepted};expected={item.Expected}");
            Check(accepted == item.Expected, item.Label);
        }

        // This helper validates raw grammar only; decoded owner/Type identity and
        // same-module checks remain in ResolveMemberRefMethod after method selection.
        foreach (var item in new (string Label, byte[] Signature, bool Expected)[]
        {
            ("exact", new byte[] { 0x20, 1, 2, 0x12, 0x10 }, true),
            ("compressed-token", new byte[] { 0x20, 1, 2, 0x12, 0x82, 0x30 }, true),
            ("four-byte-token", new byte[] { 0x20, 1, 2, 0x12, 0xc0, 0, 2, 0x30 }, true),
            ("static", new byte[] { 0, 1, 2, 0x12, 0x10 }, false),
            ("explicit-this", new byte[] { 0x60, 1, 2, 0x12, 0x10 }, false),
            ("wrong-count", new byte[] { 0x20, 2, 2, 0x12, 0x10 }, false),
            ("wrong-return", new byte[] { 0x20, 1, 8, 0x12, 0x10 }, false),
            ("named-boolean", new byte[] { 0x20, 1, 0x12, 8, 0x12, 0x10 }, false),
            ("valuetype-parameter", new byte[] { 0x20, 1, 2, 0x11, 0x10 }, false),
            ("byref-parameter", new byte[] { 0x20, 1, 2, 0x10, 0x12, 0x10 }, false),
            ("modified-parameter", new byte[] { 0x20, 1, 2, 0x20, 0x10, 0x12, 0x10 }, false),
            ("trailing", new byte[] { 0x20, 1, 2, 0x12, 0x10, 0 }, false),
            ("missing-token", new byte[] { 0x20, 1, 2, 0x12 }, false),
            ("truncated-token", new byte[] { 0x20, 1, 2, 0x12, 0x80 }, false),
            ("invalid-compression", new byte[] { 0x20, 1, 2, 0x12, 0xff }, false),
        })
        {
            var metadata = new MetadataBuilder();
            metadata.AddModule(0, metadata.GetOrAddString("MemberRefGuard.dll"), metadata.GetOrAddGuid(Guid.Empty), default, default);
            var owner = metadata.AddTypeReference(default, metadata.GetOrAddString("System"), metadata.GetOrAddString("RuntimeType"));
            var member = metadata.AddMemberReference(owner, metadata.GetOrAddString("IsAssignableFrom"), metadata.GetOrAddBlob(item.Signature));
            var image = new BlobBuilder();
            new MetadataRootBuilder(metadata).Serialize(image, 0, 0);
            using var source = MetadataReaderProvider.FromMetadataImage(image.ToImmutableArray());
            var reader = source.GetMetadataReader();
            bool accepted;
            try { accepted = (bool)shape.Invoke(null, new object[] { reader, reader.GetMemberReference(member) })!; }
            catch (TargetInvocationException error) when (error.InnerException is BadImageFormatException)
            {
                accepted = false;
            }
            Check(accepted == item.Expected, "MemberRef " + item.Label);
            Console.WriteLine($"memberref-{item.Label}:accepted={accepted};expected={item.Expected}");
        }
        Console.WriteLine("runtime-type-assignable-from-raw-guards:True");
        Console.WriteLine("runtime-type-assignable-from-raw-guard-checks:" + checks);
        return true;
    }
}
