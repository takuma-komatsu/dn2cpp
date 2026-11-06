using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;

internal static class Gen2CallbackGuards
{
    private delegate bool RawPredicate(BlobReader blob, string name);

    public static bool Run(string[] args)
    {
        if (args.Length != 4 || args[0] != "--gen2-callback-guards") return false;
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
        string input = Path.GetFullPath(args[3]);
        if (File.Exists(input)) throw new InvalidOperationException("model app must be new");
        using (var empty = Mono.Cecil.AssemblyDefinition.CreateAssembly(new Mono.Cecil.AssemblyNameDefinition("Gen2CallbackGuardInput", new Version(1, 0)), "Gen2CallbackGuardInput", Mono.Cecil.ModuleKind.Dll))
            empty.Write(input, new Mono.Cecil.WriterParameters { Timestamp = 0, DeterministicMvid = true });
        var options = Activator.CreateInstance(Model("TranspileOptions"))!;
        Set(options, "Input", input); Set(options, "UseILDiet", false); Set(options, "SharedGenerics", false);
        Set(options, "AutoRef", false); Set(options, "IsaSurfaceDump", "predicate-guard-only");
        object loaded;
        try { loaded = Model("Compilation").GetMethod("Create", statics)!.Invoke(null, new object[] { new[] { input, Path.GetFullPath(args[2]) }, options })!; }
        catch (TargetInvocationException error) when (error.InnerException is not null) { throw error.InnerException; }
        var module = ((IEnumerable)Get(loaded, "Modules")).Cast<object>().Single(m => (string)Get(m, "AssemblyName") == "System.Private.CoreLib");
        var reader = (MetadataReader)Get(module, "Reader");
        var ownerHandle = reader.TypeDefinitions.Single(h =>
        {
            var type = reader.GetTypeDefinition(h);
            return reader.GetString(type.Name) == "Gen2GcCallback" && reader.GetString(type.Namespace) == "System" && type.GetDeclaringType().IsNil;
        });
        var predicate = Model("CoreIntrinsics").GetMethod("IsGen2GcCallbackRefusal", statics)!;
        var registerPredicate = Model("CoreIntrinsics").GetMethod("IsGen2GcCallbackRegister", statics)!;
        var typePredicate = Model("CoreIntrinsics").GetMethod("IsGen2GcCallbackType", statics)!;
        var raw = Model("CoreIntrinsics").GetMethod("IsGen2GcCallbackSignature", statics)!.CreateDelegate<RawPredicate>();
        int checks = 0;
        void Check(bool result, string label)
        {
            if (!result) throw new InvalidOperationException("Gen2 callback guard: " + label);
            checks++;
        }
        var methods = reader.GetTypeDefinition(ownerHandle).GetMethods().Select(h => ((IDictionary)Get(module, "MethodMap"))[h]!).ToArray();
        var selected = methods.Where(m => (string)Get(m, "Name") is ".ctor" or "Finalize").ToArray();
        Check(selected.Length == 3, "installed-three-members");
        foreach (object actual in selected)
        {
            string name = (string)Get(actual, "Name");
            var owner = Get(actual, "DeclaringClass");
            Check((bool)predicate.Invoke(null, new[] { actual })!, "actual-CoreLib-" + name);
            Check((bool)typePredicate.Invoke(null, new[] { owner })!, "actual-type");
            Console.WriteLine("gen2-callback-actual-model:" + MetadataTokens.GetToken((MethodDefinitionHandle)Get(actual, "Handle")).ToString("X8", CultureInfo.InvariantCulture));
            void Reject(object value, string field, object replacement, string label)
            {
                object saved = Get(value, field);
                Set(value, field, replacement);
                try { Check(!(bool)predicate.Invoke(null, new[] { actual })!, label); }
                finally { Set(value, field, saved); }
            }
            Reject(module, "AssemblyName", "User.CoreLib", "assembly-lookalike");
            Reject(owner, "_fullName", "User.Gen2GcCallback", "name-lookalike");
            Reject(owner, "Handle", default(TypeDefinitionHandle), "nil-owner");
            Reject(owner, "Handle", reader.TypeDefinitions.First(h => h != ownerHandle), "foreign-owner-row");
            Reject(owner, "IsSealed", false, "unsealed");
            Reject(owner, "IsAbstract", true, "abstract");
            Reject(owner, "IsInterface", true, "interface");
            Reject(owner, "IsValueType", true, "value-type");
            Reject(owner, "IsByRefLike", true, "byref-like");
            Reject(owner, "GenericArity", 1, "generic-owner");
            Reject(Get(owner, "BaseClass"), "_fullName", "System.Object", "wrong-base");
            Reject(actual, "Handle", default(MethodDefinitionHandle), "nil-method");
            Reject(actual, "Rva", 0, "no-managed-body");
            Reject(actual, "Name", "Register", "wrong-name");
            var attributes = (MethodAttributes)Get(actual, "Attributes");
            Reject(actual, "Attributes", attributes | MethodAttributes.Static, "static");
            Reject(actual, "Attributes", attributes | MethodAttributes.PinvokeImpl, "pinvoke");
            Reject(actual, "Attributes", (attributes & ~MethodAttributes.MemberAccessMask) | MethodAttributes.Public, "public");
            foreach (var implementation in new[] { MethodImplAttributes.InternalCall, MethodImplAttributes.NoInlining, MethodImplAttributes.Synchronized, MethodImplAttributes.PreserveSig })
                Reject(actual, "ImplAttributes", implementation, "impl-" + implementation);
            if (name == ".ctor")
            {
                var parameters = ((IEnumerable)Get(Get(actual, "Signature"), "ParameterTypes")).Cast<object>().ToArray();
                var function = Get(parameters[0], "Class");
                Reject(function, "IsDelegate", false, "not-delegate");
                Reject(function, "IsValueType", true, "value-Func");
                Reject(function, "Handle", default(TypeDefinitionHandle), "nil-Func");
                Reject(function, "Handle", ownerHandle, "wrong-Func-template");
                Reject(parameters[0], "Kind", Enum.Parse(Model("TypeKind"), "ByRef"), "byref-Func");
                var typeArgs = ((IEnumerable)Get(Get(function, "Context"), "TypeArgs")).Cast<object>().ToArray();
                Reject(typeArgs[^1], "Primitive", PrimitiveTypeCode.Int32, "Func-not-bool");
                if (parameters.Length == 2)
                    Reject(parameters[1], "Primitive", PrimitiveTypeCode.Int32, "marker-not-object");
            }
            Check((bool)predicate.Invoke(null, new[] { actual })!, "restored-actual");
        }
        var registerMethods = methods.Where(m => (string)Get(m, "Name") == "Register").ToArray();
        Check(registerMethods.Length == 2, "two-installed-Register-controls");
        foreach (var method in registerMethods)
        {
            Check(!(bool)predicate.Invoke(null, new[] { method })!, "Register-not-refusal");
            Check((bool)registerPredicate.Invoke(null, new[] { method })!, "actual-Register-positive");
            var attrs = (MethodAttributes)Get(method, "Attributes");
            foreach (var changed in new[] { attrs & ~MethodAttributes.Static, attrs | MethodAttributes.PinvokeImpl,
                (attrs & ~MethodAttributes.MemberAccessMask) | MethodAttributes.Private })
            {
                Set(method, "Attributes", changed);
                try { Check(!(bool)registerPredicate.Invoke(null, new[] { method })!, "Register-attrs-negative"); }
                finally { Set(method, "Attributes", attrs); }
            }
            object savedModuleName = Get(module, "AssemblyName");
            Set(module, "AssemblyName", "User.CoreLib");
            try { Check(!(bool)registerPredicate.Invoke(null, new[] { method })!, "Register-lookalike-negative"); }
            finally { Set(module, "AssemblyName", savedModuleName); }
            Check((bool)registerPredicate.Invoke(null, new[] { method })!, "Register-restored");
        }
        var vectors = new (string Name, string Hex, bool Expected)[]
        {
            ("Finalize", "200001", true), ("Finalize", "20000100", false),
            ("Finalize", "000001", false), ("Finalize", "30010001", false),
            ("Finalize", "200002", false), ("Finalize", "20010102", false),
            (".ctor", "200101150D0102", false), // missing CLASS marker
            (".ctor", "2001011512050102", true),
            (".ctor", "2001011512080102", true), // independently valid TypeDef token
            (".ctor", "200201151205021C021C", true),
            (".ctor", "2001011511050102", false), // VALUETYPE Func
            (".ctor", "2001011512010102", false), // zero RID
            (".ctor", "2001011512070102", false), // invalid token tag
            (".ctor", "2001011512050108", false),
            (".ctor", "2001011512050202", false),
            (".ctor", "200101151205010200", false),
            (".ctor", "200201151205021C0208", false),
            (".ctor", "2002011512050208021C", false),
            (".ctor", "200101101512050102", false), // byref Func
            (".ctor", "20010120051512050102", false), // modifier
            (".ctor", "0001011512050102", false),
            (".ctor", "300101011512050102", false),
            (".ctor", "20010115120501", false),
            (".ctor", "200001", false), (".ctor", "", false),
            ("Register", "0001011512050102", true),
            ("Register", "000201151205021C021C", true),
            ("Register", "2001011512050102", false),
            ("Register", "0001011511050102", false),
            ("Register", "0001011512050108", false),
            ("Register", "000201151205021C0208", false),
            ("Register", "00010120051512050102", false),
            ("Register", "000101151205010200", false),
            ("Register", "200001", false)
        };
        foreach (var vector in vectors)
        {
            var metadata = new MetadataBuilder();
            metadata.AddModule(0, metadata.GetOrAddString("Gen2Raw"), metadata.GetOrAddGuid(Guid.Empty), default, default);
            var signature = metadata.GetOrAddBlob(Convert.FromHexString(vector.Hex));
            var image = new BlobBuilder();
            new MetadataRootBuilder(metadata).Serialize(image, 0, 0);
            using var source = MetadataReaderProvider.FromMetadataImage(image.ToImmutableArray());
            Check(raw(source.GetMetadataReader().GetBlobReader(signature), vector.Name) == vector.Expected, "raw-" + vector.Name + "-" + vector.Hex);
        }
        Console.WriteLine("gen2-callback-guards:" + checks);
        return true;
    }
}
