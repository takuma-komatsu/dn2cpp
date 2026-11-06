using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;

internal static class KnownConstantGuards
{
    public static bool Run(string[] args)
    {
        if (args.Length != 2 || args[0] != "--known-constant-guards") return false;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        var model = new KnownConstantGuardModel(args[1]);
        int checks = 0;
        void Check(bool value, string label)
        {
            if (!value) throw new InvalidOperationException("Known constant declaration guard: " + label);
            checks++;
        }
        // Inspect private CoreLib metadata only; never invoke these CLR methods.
        using (var pe = new PEReader(File.OpenRead(typeof(object).Assembly.Location)))
        {
            var reader = pe.GetMetadataReader();
            var owner = reader.TypeDefinitions.Single(h => reader.GetString(reader.GetTypeDefinition(h).Namespace)
                == "System.Runtime.CompilerServices" && reader.GetString(reader.GetTypeDefinition(h).Name) == "RuntimeHelpers");
            var methods = reader.GetTypeDefinition(owner).GetMethods().Where(h =>
                reader.GetString(reader.GetMethodDefinition(h).Name) == "IsKnownConstant").ToArray();
            Check(methods.Length == 4, "pinned exact four definitions");
            foreach (var handle in methods)
            {
                var definition = reader.GetMethodDefinition(handle);
                var module = model.Module(reader);
                var cls = model.Class(module, owner, "System.Runtime.CompilerServices.RuntimeHelpers");
                var method = model.Method(module, cls, handle, reader);
                Check(model.Accepts(method), "real CoreLib definition " + MetadataTokens.GetToken(handle));
                Check(pe.GetMethodBody(definition.RelativeVirtualAddress).GetILBytes() is [0x16, 0x2a],
                    "token-free false body");
            }
        }
        foreach (var signature in new[] { new byte[] { 0, 1, 2, 3 }, new byte[] { 0, 1, 2, 14 },
            new byte[] { 0, 1, 2, 18, 8 }, KnownConstantGuardImage.GenericSignature })
        {
            using var image = new KnownConstantGuardImage(model, signature);
            Check(model.Accepts(image.Method), "exact declaration " + Convert.ToHexString(signature));
            Check(model.Callable(image.Method) == (signature[0] != 0x10), "callable versus open declaration");
            void Negative(string label, object target, string member, object? value)
            {
                var old = model.Get(target, member);
                model.Set(target, member, value);
                Check(!model.Accepts(image.Method), label);
                Check(model.Callable(image.Method) == (label is "owner name" or "name"), "callable " + label);
                model.Set(target, member, old);
            }
            Negative("foreign assembly", image.Module, "AssemblyName", "Lookalike");
            Negative("assembly case", image.Module, "AssemblyName", "system.private.corelib");
            Negative("owner name", image.Owner, "_fullName", "Other.RuntimeHelpers");
            Negative("owner identity", image.Owner, "Handle", MetadataTokens.TypeDefinitionHandle(2));
            Negative("foreign method module", image.Method, "Module", model.Module(image.Reader));
            Negative("foreign owner module", image.Owner, "Module", model.Module(image.Reader));
            Negative("name", image.Method, "Name", "Other");
            Negative("no handle", image.Method, "Handle", default(MethodDefinitionHandle));
            Negative("no body", image.Method, "Rva", 0);
            Negative("PInvoke", image.Method, "PInvoke", model.Make("PInvokeInfo"));
            for (int bit = 0; bit < 16; bit++)
                Negative("attribute bit " + bit, image.Method, "Attributes", KnownConstantGuardImage.Attributes ^ (MethodAttributes)(1 << bit));
            foreach (var flag in new[] { MethodImplAttributes.Native, MethodImplAttributes.Runtime,
                MethodImplAttributes.Unmanaged, MethodImplAttributes.InternalCall })
                Negative("implementation " + flag, image.Method, "ImplAttributes", flag);
        }
        foreach (var item in KnownConstantRawGuards.Signatures())
        {
            using var image = new KnownConstantGuardImage(model, item.Signature);
            Check(model.Accepts(image.Method) == item.Expected, "raw declaration " + item.Label);
        }
        foreach (var flags in new[] { GenericParameterAttributes.None, GenericParameterAttributes.NotNullableValueTypeConstraint,
            GenericParameterAttributes.DefaultConstructorConstraint, GenericParameterAttributes.ReferenceTypeConstraint,
            KnownConstantGuardImage.StructFlags | (GenericParameterAttributes)0x20,
            KnownConstantGuardImage.StructFlags | GenericParameterAttributes.Covariant })
        {
            using var image = new KnownConstantGuardImage(model, KnownConstantGuardImage.GenericSignature, flags: flags);
            Check(!model.Accepts(image.Method), "exact generic flags " + flags);
        }
        foreach (int constraint in new[] { 0, 2, 4, -1 })
        {
            using var image = new KnownConstantGuardImage(model, KnownConstantGuardImage.GenericSignature, constraint: constraint);
            Check(!model.Accepts(image.Method), "ValueType constraint " + constraint);
        }
        foreach (int index in new[] { 1, 2 })
        {
            using var image = new KnownConstantGuardImage(model, KnownConstantGuardImage.GenericSignature, index: index);
            Check(!model.Accepts(image.Method), "generic parameter index " + index);
        }
        using (var image = new KnownConstantGuardImage(model, KnownConstantGuardImage.GenericSignature))
        {
            Check(model.Accepts(image.Method), "open declaration retained without closed argument");
            foreach (var code in Enum.GetValues<PrimitiveTypeCode>())
            {
                var argument = model.Primitive(code);
                model.Set(image.Method, "Context", model.Context(argument));
                bool expected = code is PrimitiveTypeCode.Boolean or PrimitiveTypeCode.Char or PrimitiveTypeCode.SByte
                    or PrimitiveTypeCode.Byte or PrimitiveTypeCode.Int16 or PrimitiveTypeCode.UInt16 or PrimitiveTypeCode.Int32
                    or PrimitiveTypeCode.UInt32 or PrimitiveTypeCode.Int64 or PrimitiveTypeCode.UInt64 or PrimitiveTypeCode.IntPtr
                    or PrimitiveTypeCode.UIntPtr or PrimitiveTypeCode.Single or PrimitiveTypeCode.Double;
                Check(model.Accepts(image.Method) == expected, "closed primitive " + code);
                Check(model.Callable(image.Method) == expected, "callable primitive " + code);
            }
            foreach (var item in new[] { (5, "System.Nullable`1", true, false, false),
                (6, "Fixture.ManagedStruct", true, false, true), (7, "Fixture.Enum", false, false, true),
                (8, "Fixture.RefStruct", true, true, false), (9, "Fixture.Reference", false, false, false) })
            {
                var cls = model.Class(image.Module, MetadataTokens.TypeDefinitionHandle(item.Item1), item.Item2);
                model.Set(cls, "IsValueType", item.Item3);
                model.Set(cls, "IsEnum", item.Item1 == 7);
                model.Set(cls, "IsByRefLike", item.Item4);
                model.Set(image.Method, "Context", model.Context(model.ClassType(cls)));
                Check(model.Accepts(image.Method) == item.Item5, "closed " + item.Item2);
                Check(model.Callable(image.Method) == item.Item5, "callable " + item.Item2);
            }
            foreach (string kind in new[] { "ByRef", "Pointer", "SZArray", "GenericVar", "Template", "External" })
            {
                var argument = model.Primitive(PrimitiveTypeCode.Int32);
                model.Set(argument, "Kind", Enum.Parse(model.Type("TypeKind"), kind));
                model.Set(image.Method, "Context", model.Context(argument));
                Check(!model.Accepts(image.Method), "closed " + kind);
                Check(!model.Callable(image.Method), "callable " + kind);
            }
            object Construct(int row, params object[] arguments)
            {
                var cls = model.Class(image.Module, MetadataTokens.TypeDefinitionHandle(row),
                    row == 10 ? "Fixture.Box`1" : row == 11 ? "Fixture.ReferenceBox`1" : row == 12 ? "Fixture.Pair`2" : "Fixture.ManagedStruct");
                model.Set(cls, "IsValueType", row != 11);
                model.Set(cls, "Context", model.TypeContext(arguments));
                return model.ClassType(cls);
            }
            object Wrapped(string kind, object element)
            {
                var argument = model.Make("TypeDesc");
                model.Set(argument, "Kind", Enum.Parse(model.Type("TypeKind"), kind));
                model.Set(argument, "Element", element);
                if (kind == "MDArray") model.Set(argument, "Rank", 2);
                return argument;
            }
            var text = model.Primitive(PrimitiveTypeCode.String);
            var integer = model.Primitive(PrimitiveTypeCode.Int32);
            var variable = model.Make("TypeDesc");
            model.Set(variable, "Kind", Enum.Parse(model.Type("TypeKind"), "GenericVar"));
            var template = model.Make("TypeDesc");
            model.Set(template, "Kind", Enum.Parse(model.Type("TypeKind"), "Template"));
            model.Set(template, "TemplateModule", image.Module);
            model.Set(template, "TemplateHandle", MetadataTokens.TypeDefinitionHandle(10));
            foreach (var item in new (string Label, object Argument, bool Expected)[] {
                ("Box<string>", Construct(10, text), true),
                ("Box<int[]>", Construct(10, Wrapped("SZArray", integer)), true),
                ("Box<string[,]>", Construct(10, Wrapped("MDArray", text)), true),
                ("Box<Box<string>>", Construct(10, Construct(10, text)), true),
                ("Box<ReferenceBox<string>>", Construct(10, Construct(11, text)), true),
                ("Pair<int,string>", Construct(12, integer, text), true),
                ("Box<VAR>", Construct(10, variable), false),
                ("Box<Template>", Construct(10, template), false),
                ("Box<Box<VAR>>", Construct(10, Construct(10, variable)), false),
                ("Box<ReferenceBox<Template>>", Construct(10, Construct(11, template)), false),
                ("Box<VAR[]>", Construct(10, Wrapped("SZArray", variable)), false),
                ("Box<Template[,]>", Construct(10, Wrapped("MDArray", template)), false),
                ("Box missing argument", Construct(10), false),
                ("Box extra argument", Construct(10, integer, text), false),
                ("Pair missing all arguments", Construct(12), false),
                ("Pair missing one argument", Construct(12, integer), false),
                ("Pair extra argument", Construct(12, integer, text, integer), false),
                ("nested Box missing argument", Construct(10, Construct(10)), false),
                ("nested ReferenceBox missing argument", Construct(10, Construct(11)), false),
                ("nested ReferenceBox extra argument", Construct(10, Construct(11, integer, text)), false),
                ("nongeneric struct extra argument", Construct(6, integer), false) })
            {
                model.Set(image.Method, "Context", model.Context(item.Argument));
                Check(model.Accepts(image.Method) == item.Expected, "generic closure " + item.Label);
                Check(model.Callable(image.Method) == item.Expected, "generic callable closure " + item.Label);
            }
            model.Set(image.Method, "Context", model.Context(model.Primitive(PrimitiveTypeCode.Int32), model.Primitive(PrimitiveTypeCode.Char)));
            Check(!model.Accepts(image.Method), "two closed arguments");
        }
        Console.WriteLine("known-constant-guards:True");
        Console.WriteLine("known-constant-guard-checks:" + checks);
        return true;
    }
}

internal sealed class KnownConstantGuardModel
{
    internal const BindingFlags Members = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
    private readonly Assembly assembly;
    internal KnownConstantGuardModel(string path) => assembly = Assembly.LoadFrom(Path.GetFullPath(path));
    internal Type Type(string name) => assembly.GetType("Dn2Cpp." + name, true)!;
    internal object Make(string name) => RuntimeHelpers.GetUninitializedObject(Type(name));
    internal object? Get(object target, string name) => target.GetType().GetField(name, Members) is { } field
        ? field.GetValue(target) : target.GetType().GetProperty(name, Members)!.GetValue(target);
    internal void Set(object target, string name, object? value)
    {
        if (target.GetType().GetField(name, Members) is { } field) field.SetValue(target, value);
        else if (target.GetType().GetField("<" + name + ">k__BackingField", Members) is { } backing) backing.SetValue(target, value);
        else target.GetType().GetProperty(name, Members)!.SetValue(target, value);
    }
    internal object Context(params object[] arguments)
    {
        var array = Array.CreateInstance(Type("TypeDesc"), arguments.Length);
        for (int i = 0; i < arguments.Length; i++) array.SetValue(arguments[i], i);
        return Activator.CreateInstance(Type("GenericContext"), new object[] { Array.CreateInstance(Type("TypeDesc"), 0), array })!;
    }
    internal object TypeContext(params object[] arguments)
    {
        var methodContext = Context(arguments);
        return Activator.CreateInstance(Type("GenericContext"), new object[] { Get(methodContext, "MethodArgs")!, Array.CreateInstance(Type("TypeDesc"), 0) })!;
    }
    internal object Primitive(PrimitiveTypeCode code)
    {
        var value = Make("TypeDesc");
        Set(value, "Kind", Enum.Parse(Type("TypeKind"), "Primitive")); Set(value, "Primitive", code);
        return value;
    }
    internal object ClassType(object cls)
    {
        var value = Make("TypeDesc"); Set(value, "Kind", Enum.Parse(Type("TypeKind"), "Class")); Set(value, "Class", cls);
        return value;
    }
    internal object Module(MetadataReader reader)
    {
        var module = Make("Module"); Set(module, "Reader", reader); Set(module, "AssemblyName", "System.Private.CoreLib");
        Set(module, "ClassMap", Activator.CreateInstance(Type("Module").GetField("ClassMap", Members)!.FieldType));
        return module;
    }
    internal object Class(object module, TypeDefinitionHandle handle, string fullName)
    {
        var cls = Make("ClassInfo"); Set(cls, "Module", module); Set(cls, "Handle", handle); Set(cls, "_fullName", fullName);
        Set(cls, "Context", Context());
        ((IDictionary)Get(module, "ClassMap")!)[handle] = cls;
        return cls;
    }
    internal object Method(object module, object owner, MethodDefinitionHandle handle, MetadataReader reader)
    {
        var definition = reader.GetMethodDefinition(handle); var method = Make("MethodInfo");
        Set(method, "Module", module); Set(method, "DeclaringClass", owner); Set(method, "Handle", handle);
        Set(method, "Name", reader.GetString(definition.Name)); Set(method, "Attributes", definition.Attributes);
        Set(method, "ImplAttributes", definition.ImplAttributes); Set(method, "Rva", definition.RelativeVirtualAddress);
        Set(method, "Context", Context());
        return method;
    }
    internal bool Callable(object method)
    {
        try
        {
            return ReferenceEquals(method, Type("CoreIntrinsics").GetMethod("ValidateKnownConstantCallable", Members)!.Invoke(null, new[] { method }));
        }
        catch (TargetInvocationException error) when (error.InnerException is NotSupportedException)
        {
            return false;
        }
    }
    internal bool Accepts(object method) => (bool)Type("CoreIntrinsics").GetMethod("IsKnownConstant", Members)!.Invoke(null, new[] { method })!;
}

// Invalid rows are consumed only by the compiler's metadata guards, never loaded by CLR.
internal sealed class KnownConstantGuardImage : IDisposable
{
    internal static readonly byte[] GenericSignature = { 0x10, 1, 1, 2, 0x1e, 0 };
    internal const MethodAttributes Attributes = MethodAttributes.Assembly | MethodAttributes.Static | MethodAttributes.HideBySig;
    internal const GenericParameterAttributes StructFlags = GenericParameterAttributes.NotNullableValueTypeConstraint | GenericParameterAttributes.DefaultConstructorConstraint;
    private readonly MetadataReaderProvider source;
    internal MetadataReader Reader { get; }
    internal object Module { get; }
    internal object Owner { get; }
    internal object Method { get; }
    internal MemberReferenceHandle Reference { get; }
    internal MethodSpecificationHandle Specification { get; }
    internal KnownConstantGuardImage(KnownConstantGuardModel model, byte[] signature,
        GenericParameterAttributes flags = StructFlags, int constraint = 3, int index = 0,
        byte[]? reference = null, byte[]? specification = null, bool memberParent = false, int ownerMode = 0)
    {
        var metadata = new MetadataBuilder();
        metadata.AddModule(0, metadata.GetOrAddString("Guard.dll"), metadata.GetOrAddGuid(Guid.Empty), default, default);
        metadata.AddAssembly(metadata.GetOrAddString("System.Private.CoreLib"), new Version(10, 0), default, default, default, default);
        var firstMethod = MetadataTokens.MethodDefinitionHandle(1);
        var firstField = MetadataTokens.FieldDefinitionHandle(1);
        string[] names = { "<Module>", "Type", "ValueType", "RuntimeHelpers", "Nullable`1", "ManagedStruct", "Enum", "RefStruct", "Reference", "Box`1", "ReferenceBox`1", "Pair`2" };
        for (int i = 0; i < names.Length; i++)
            metadata.AddTypeDefinition(TypeAttributes.Public, metadata.GetOrAddString(i == 3 ? "System.Runtime.CompilerServices" : i < 5 ? "System" : "Fixture"),
                metadata.GetOrAddString(names[i]), default, i <= 5 ? firstField : MetadataTokens.FieldDefinitionHandle(2), i <= 3 ? firstMethod : MetadataTokens.MethodDefinitionHandle(2));
        metadata.AddFieldDefinition(FieldAttributes.Public, metadata.GetOrAddString("Reference"), metadata.GetOrAddBlob(new byte[] { 6, 0x1c }));
        var handle = metadata.AddMethodDefinition(Attributes, MethodImplAttributes.IL, metadata.GetOrAddString("IsKnownConstant"),
            metadata.GetOrAddBlob(signature), 1, MetadataTokens.ParameterHandle(1));
        if (signature.Length > 0 && (signature[0] & 0x10) != 0)
        {
            var variable = metadata.AddGenericParameter(handle, flags, metadata.GetOrAddString("T"), index);
            if (constraint != 0) metadata.AddGenericParameterConstraint(variable, MetadataTokens.TypeDefinitionHandle(constraint < 0 ? 3 : constraint));
            if (constraint < 0) metadata.AddGenericParameterConstraint(variable, MetadataTokens.TypeDefinitionHandle(2));
        }
        metadata.AddGenericParameter(MetadataTokens.TypeDefinitionHandle(10), GenericParameterAttributes.None, metadata.GetOrAddString("T"), 0);
        metadata.AddGenericParameter(MetadataTokens.TypeDefinitionHandle(11), GenericParameterAttributes.None, metadata.GetOrAddString("T"), 0);
        metadata.AddGenericParameter(MetadataTokens.TypeDefinitionHandle(12), GenericParameterAttributes.None, metadata.GetOrAddString("T"), 0);
        metadata.AddGenericParameter(MetadataTokens.TypeDefinitionHandle(12), GenericParameterAttributes.None, metadata.GetOrAddString("U"), 1);
        EntityHandle referenceOwner = MetadataTokens.TypeDefinitionHandle(4);
        if (ownerMode != 0)
        {
            var assemblyRef = metadata.AddAssemblyReference(metadata.GetOrAddString(ownerMode == 2 ? "Lookalike" : "System.Private.CoreLib"),
                new Version(ownerMode == 3 ? 9 : 10, 0), ownerMode == 4 ? metadata.GetOrAddString("en") : default,
                metadata.GetOrAddBlob(ownerMode == 5 ? new byte[8] : new byte[] { 0x7c, 0xec, 0x85, 0xd7, 0xbe, 0xa7, 0x79, 0x8e }),
                ownerMode == 6 ? AssemblyFlags.PublicKey : default, default);
            referenceOwner = metadata.AddTypeReference(assemblyRef, metadata.GetOrAddString("System.Runtime.CompilerServices"),
                metadata.GetOrAddString(ownerMode == 7 ? "Other" : "RuntimeHelpers"));
            if (ownerMode == 8) referenceOwner = metadata.AddTypeReference(referenceOwner,
                metadata.GetOrAddString("System.Runtime.CompilerServices"), metadata.GetOrAddString("RuntimeHelpers"));
        }
        Reference = metadata.AddMemberReference(referenceOwner, metadata.GetOrAddString("IsKnownConstant"), metadata.GetOrAddBlob(reference ?? signature));
        Specification = metadata.AddMethodSpecification(memberParent ? Reference : handle, metadata.GetOrAddBlob(specification ?? new byte[] { 0x0a, 1, 8 }));
        var blob = new BlobBuilder(); new MetadataRootBuilder(metadata).Serialize(blob, 0, 0);
        source = MetadataReaderProvider.FromMetadataImage(blob.ToImmutableArray()); Reader = source.GetMetadataReader();
        Module = model.Module(Reader); Owner = model.Class(Module, MetadataTokens.TypeDefinitionHandle(4), "System.Runtime.CompilerServices.RuntimeHelpers");
        model.Class(Module, MetadataTokens.TypeDefinitionHandle(2), "System.Type");
        model.Class(Module, MetadataTokens.TypeDefinitionHandle(3), "System.ValueType");
        Method = model.Method(Module, Owner, handle, Reader);
    }
    public void Dispose() => source.Dispose();
}
