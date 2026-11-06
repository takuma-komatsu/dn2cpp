using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;

internal static class ActivatorDefaultGuards
{
    public static bool Run(string[] args)
    {
        if (args.Length != 2 || args[0] != "--activator-default-guards") return false;
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
        var predicate = Model("CoreIntrinsics").GetMethod("IsActivatorDefault", statics)!;
        var decode = typeof(MethodDefinition).GetMethods().Single(m => m.Name == "DecodeSignature")
            .MakeGenericMethod(Model("TypeDesc"), typeof(object));
        int checks = 0;
        void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("Activator default raw guard: " + message);
            checks++;
        }

        // Actual SRM decoding deliberately normalizes class/value tags and modifiers;
        // the exact descriptor must still reject those raw declaration near-misses.
        foreach (var item in new (string Label, byte[] Signature, bool Expected)[]
        {
            ("exact", new byte[] { 0, 3, 0x1c, 0x12, 0x10, 2, 2 }, true),
            ("valuetype-type", new byte[] { 0, 3, 0x1c, 0x11, 0x10, 2, 2 }, false),
            ("byref-type", new byte[] { 0, 3, 0x1c, 0x10, 0x12, 0x10, 2, 2 }, false),
            ("modified-type", new byte[] { 0, 3, 0x1c, 0x20, 0x10, 0x12, 0x10, 2, 2 }, false),
            ("modified-return", new byte[] { 0, 3, 0x20, 0x10, 0x1c, 0x12, 0x10, 2, 2 }, false),
            ("named-bool-class", new byte[] { 0, 3, 0x1c, 0x12, 0x10, 0x12, 8, 2 }, false),
            ("named-bool-value", new byte[] { 0, 3, 0x1c, 0x12, 0x10, 2, 0x11, 8 }, false),
            ("modified-bool", new byte[] { 0, 3, 0x1c, 0x12, 0x10, 2, 0x20, 0x10, 2 }, false),
            ("instance", new byte[] { 0x20, 3, 0x1c, 0x12, 0x10, 2, 2 }, false),
            ("explicit", new byte[] { 0x60, 3, 0x1c, 0x12, 0x10, 2, 2 }, false),
            ("vararg", new byte[] { 5, 3, 0x1c, 0x12, 0x10, 2, 2 }, false),
            ("generic", new byte[] { 0x10, 1, 3, 0x1c, 0x12, 0x10, 2, 2 }, false),
            ("object-type", new byte[] { 0, 3, 0x1c, 0x1c, 2, 2 }, false),
            ("wrong-type", new byte[] { 0, 3, 0x1c, 0x12, 0x14, 2, 2 }, false),
            ("wrong-return", new byte[] { 0, 3, 8, 0x12, 0x10, 2, 2 }, false),
            ("missing-parameter", new byte[] { 0, 2, 0x1c, 0x12, 0x10, 2 }, false),
            ("extra-parameter", new byte[] { 0, 4, 0x1c, 0x12, 0x10, 2, 2, 2 }, false),
            ("trailing-token", new byte[] { 0, 3, 0x1c, 0x12, 0x10, 2, 2, 2 }, false),
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
                metadata.GetOrAddString("System"), metadata.GetOrAddString("Activator"), default, firstField, firstMethod);
            var nextMethod = MetadataTokens.MethodDefinitionHandle(2);
            var type = metadata.AddTypeDefinition(TypeAttributes.Public | TypeAttributes.Abstract,
                metadata.GetOrAddString("System"), metadata.GetOrAddString("Type"), default, firstField, nextMethod);
            var typeInfo = metadata.AddTypeDefinition(TypeAttributes.Public | TypeAttributes.Abstract,
                metadata.GetOrAddString("System.Reflection"), metadata.GetOrAddString("TypeInfo"), default, firstField, nextMethod);
            var handle = metadata.AddMethodDefinition(MethodAttributes.Assembly | MethodAttributes.Static | MethodAttributes.HideBySig,
                MethodImplAttributes.IL, metadata.GetOrAddString("CreateInstance"), metadata.GetOrAddBlob(item.Signature), 1,
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
            var owner = MakeClass(runtimeType, "System.Activator");
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
            if (item.Expected)
            {
                void Reject(string field, object bad)
                {
                    object old = Get(method, field);
                    Set(method, field, bad);
                    Check(!(bool)predicate.Invoke(null, new[] { method })!, "declaration-" + field);
                    Set(method, field, old);
                }
                Reject("Name", "CreateSomething");
                Reject("Attributes", MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig);
                Reject("ImplAttributes", MethodImplAttributes.InternalCall);
                Reject("Rva", 0);
                Set(module, "AssemblyName", "Application");
                Check(!(bool)predicate.Invoke(null, new[] { method })!, "foreign-owner");
                Set(module, "AssemblyName", "System.Private.CoreLib");
            }
        }

        var hotCompilation = Make("Compilation");
        var hotMethod = Make("MethodInfo");
        Set(hotMethod, "_cppName", "ActivatorDefaultGuard");
        var facts = (IDictionary)NewMap(Model("Compilation"), "HotBodyFacts");
        Set(hotCompilation, "HotBodyFacts", facts);
        Model("Compilation").GetMethod("RecordHotBodyFacts", fields)!.Invoke(hotCompilation,
            new[] { hotMethod, "return dn2cpp_activator_create_instance_wrapped(type, nonPublic, wrap);" });
        var fact = facts["ActivatorDefaultGuard"]!;
        Check((bool)Get(fact, "Allocates") && (bool)Get(fact, "Dispatches"), "allocation-and-dispatch-facts");
        Check((string)Get(fact, "AllocToken") == "dn2cpp_activator_create_instance"
            && (string)Get(fact, "DispatchToken") == "dn2cpp_activator_create_instance", "existing-construction-prefix");
        Console.WriteLine("activator-default-raw-guards=" + checks);
        return true;
    }
}
