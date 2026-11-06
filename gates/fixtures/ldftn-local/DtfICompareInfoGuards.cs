using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

internal static class DtfICompareInfoGuards
{
    private delegate bool RawPredicate(BlobReader blob);
    public static bool Run(string[] args)
    {
        if (args.Length != 4 || args[0] != "--dtfi-compare-info-guards") return false;
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
        if (File.Exists(input)) throw new InvalidOperationException("model input must be new");
        using (var empty = Mono.Cecil.AssemblyDefinition.CreateAssembly(new Mono.Cecil.AssemblyNameDefinition("DtfICompareInfoGuardInput", new Version(1, 0)), "DtfICompareInfoGuardInput", Mono.Cecil.ModuleKind.Dll))
        using (var core = Mono.Cecil.AssemblyDefinition.ReadAssembly(args[2]))
        {
            var host = new Mono.Cecil.TypeDefinition("Guard", "References", Mono.Cecil.TypeAttributes.Public, empty.MainModule.TypeSystem.Object);
            empty.MainModule.Types.Add(host);
            foreach (string owner in new[] { "DateTimeFormatInfo", "CultureInfo" })
            {
                var target = empty.MainModule.ImportReference(core.MainModule.GetType("System.Globalization." + owner).Methods.Single(m => m.Name == "get_CompareInfo"));
                var stub = new Mono.Cecil.MethodDefinition(owner, Mono.Cecil.MethodAttributes.Public | Mono.Cecil.MethodAttributes.Static, empty.MainModule.TypeSystem.Void);
                host.Methods.Add(stub);
                var il = stub.Body.GetILProcessor();
                il.Emit(Mono.Cecil.Cil.OpCodes.Ldnull);
                il.Emit(Mono.Cecil.Cil.OpCodes.Call, target);
                il.Emit(Mono.Cecil.Cil.OpCodes.Pop);
                il.Emit(Mono.Cecil.Cil.OpCodes.Ret);
            }
            foreach (string name in new[] { ".ctor", "InitSort" })
            {
                var target = empty.MainModule.ImportReference(core.MainModule.GetType("System.Globalization.CompareInfo").Methods.Single(m => m.Name == name
                    && m.Parameters.Count == 1 && m.Parameters[0].ParameterType.FullName == "System.Globalization.CultureInfo"));
                var stub = new Mono.Cecil.MethodDefinition(name == ".ctor" ? "CompareInfoCtor" : name,
                    Mono.Cecil.MethodAttributes.Public | Mono.Cecil.MethodAttributes.Static, empty.MainModule.TypeSystem.Void);
                host.Methods.Add(stub);
                var il = stub.Body.GetILProcessor();
                il.Emit(Mono.Cecil.Cil.OpCodes.Ldnull); il.Emit(Mono.Cecil.Cil.OpCodes.Ldnull);
                il.Emit(Mono.Cecil.Cil.OpCodes.Call, target); il.Emit(Mono.Cecil.Cil.OpCodes.Ret);
            }
            var validator = core.MainModule.GetType("System.Globalization.CultureInfo").Methods.Single(method => method.Name == "VerifyCultureName"
                && method.Parameters.Count == 2 && method.Parameters[0].ParameterType.MetadataType == Mono.Cecil.MetadataType.String);
            var validatorStub = new Mono.Cecil.MethodDefinition("ValidateName", Mono.Cecil.MethodAttributes.Public | Mono.Cecil.MethodAttributes.Static, empty.MainModule.TypeSystem.Void);
            host.Methods.Add(validatorStub);
            var validatorIl = validatorStub.Body.GetILProcessor();
            validatorIl.Emit(Mono.Cecil.Cil.OpCodes.Ldnull); validatorIl.Emit(Mono.Cecil.Cil.OpCodes.Ldc_I4_0);
            validatorIl.Emit(Mono.Cecil.Cil.OpCodes.Call, empty.MainModule.ImportReference(validator));
            validatorIl.Emit(Mono.Cecil.Cil.OpCodes.Pop); validatorIl.Emit(Mono.Cecil.Cil.OpCodes.Ret);
            empty.Write(input, new Mono.Cecil.WriterParameters { Timestamp = 0, DeterministicMvid = true });
        }
        var options = Activator.CreateInstance(Model("TranspileOptions"))!;
        Set(options, "Input", input); Set(options, "UseILDiet", false); Set(options, "SharedGenerics", false);
        Set(options, "AutoRef", false); Set(options, "IsaSurfaceDump", "predicate-guard-only");
        object loaded;
        try { loaded = Model("Compilation").GetMethod("Create", statics)!.Invoke(null, new object[] { new[] { input, Path.GetFullPath(args[2]) }, options })!; }
        catch (TargetInvocationException error) when (error.InnerException is not null) { throw error.InnerException; }
        var modules = ((IEnumerable)Get(loaded, "Modules")).Cast<object>().ToArray();
        var module = modules.Single(m => (string)Get(m, "AssemblyName") == "System.Private.CoreLib");
        var appModule = modules.Single(m => (string)Get(m, "AssemblyName") == "DtfICompareInfoGuardInput");
        var reader = (MetadataReader)Get(module, "Reader");
        var predicate = Model("CoreIntrinsics").GetMethod("IsInvariantCompareInfoGetter", statics)!;
        var decoded = Model("CoreIntrinsics").GetMethod("IsInvariantCompareInfoGetterDecodedSignature", statics)!;
        var raw = Model("CoreIntrinsics").GetMethod("IsInvariantCompareInfoGetterSignature", statics)!.CreateDelegate<RawPredicate>();
        var fieldPredicate = Model("CoreIntrinsics").GetMethod("TryGetInvariantCompareInfoFields", statics)!;
        int checks = 0;
        void Check(bool result, string label)
        {
            if (!result) throw new InvalidOperationException("CompareInfo guard: " + label);
            checks++;
        }
        object Actual(string owner)
        {
            var handle = reader.TypeDefinitions.Single(h => reader.GetString(reader.GetTypeDefinition(h).Name) == owner && reader.GetString(reader.GetTypeDefinition(h).Namespace) == "System.Globalization");
            var method = reader.GetTypeDefinition(handle).GetMethods().Single(h => reader.GetString(reader.GetMethodDefinition(h).Name) == "get_CompareInfo");
            return ((IDictionary)Get(module, "MethodMap"))[method]!;
        }
        var targets = new[] { Actual("DateTimeFormatInfo"), Actual("CultureInfo") };
        foreach (var actual in targets)
        {
            var handle = (MethodDefinitionHandle)Get(actual, "Handle");
            Check((bool)predicate.Invoke(null, new[] { actual })!, "actual-" + MetadataTokens.GetToken(handle).ToString("X8"));
            Check(raw(reader.GetBlobReader(reader.GetMethodDefinition(handle).Signature)), "actual-raw");
            Check((bool)decoded.Invoke(null, new[] { Get(actual, "Signature"), module })!, "actual-decoded");
            Console.WriteLine("dtfi-compare-info-installed:" + MetadataTokens.GetToken(handle).ToString("X8", CultureInfo.InvariantCulture));
        }
        var appReader = (MetadataReader)Get(appModule, "Reader");
        var context = Model("GenericContext").GetField("Empty", statics)!.GetValue(null)!;
        int imports = 0;
        foreach (var handle in appReader.MemberReferences)
        {
            if (appReader.GetString(appReader.GetMemberReference(handle).Name) != "get_CompareInfo") continue;
            var resolved = Model("Compilation").GetMethod("ResolveMemberRefMethod", instance)!.Invoke(loaded, new[] { appModule, (object)handle, context })!;
            Check(targets.Any(t => ReferenceEquals(t, resolved)), "actual-memberref-identity");
            Check((bool)predicate.Invoke(null, new[] { resolved })!, "actual-memberref-positive");
            imports++;
        }
        Check(imports == 2, "both-memberrefs");
        foreach (var actual in targets)
        {
            void Reject(object value, string field, object next)
            {
                object saved = Get(value, field);
                Set(value, field, next);
                try { Check(!(bool)predicate.Invoke(null, new[] { actual })!, "wrong-" + field); }
                finally { Set(value, field, saved); }
            }
            var owner = Get(actual, "DeclaringClass");
            Reject(owner, "Module", appModule);
            Reject(owner, "_fullName", "Application.CultureInfo");
            Reject(owner, "Handle", default(TypeDefinitionHandle));
            Reject(owner, "IsValueType", true);
            Reject(owner, "GenericArity", 1);
            Reject(actual, "Handle", default(MethodDefinitionHandle));
            Reject(actual, "Rva", 0);
            Reject(actual, "Name", "get_NumberFormat");
            var attributes = (MethodAttributes)Get(actual, "Attributes");
            Reject(actual, "Attributes", attributes | MethodAttributes.Static);
            Reject(actual, "Attributes", attributes ^ MethodAttributes.Virtual);
            Reject(actual, "ImplAttributes", MethodImplAttributes.NoInlining);
            var result = Get(Get(actual, "Signature"), "ReturnType");
            Reject(result, "Kind", Enum.Parse(Model("TypeKind"), "ByRef"));
            var resultClass = Get(result, "Class");
            Reject(resultClass, "Module", appModule);
            Reject(resultClass, "_fullName", "Application.CompareInfo");
            Reject(resultClass, "IsValueType", true);
            Reject(resultClass, "IsEnum", true);
        }
        var compareClass = Get(Get(Get(targets[0], "Signature"), "ReturnType"), "Class");
        object?[] fieldArgs = { compareClass, null, null };
        Check((bool)fieldPredicate.Invoke(null, fieldArgs)!, "actual-fields");
        var nameField = fieldArgs[1]!; var sortField = fieldArgs[2]!;
        var compareHandle = (TypeDefinitionHandle)Get(compareClass, "Handle");
        var compareDefinition = reader.GetTypeDefinition(compareHandle);
        Check(ReferenceEquals(Get(compareClass, "Module"), module)
            && reader.StringComparer.Equals(compareDefinition.Namespace, "System.Globalization")
            && reader.StringComparer.Equals(compareDefinition.Name, "CompareInfo"), "actual-field-owner");
        foreach (var row in new[] { (Field: nameField, Name: "m_name", Attributes: 0x1), (Field: sortField, Name: "_sortName", Attributes: 0x81) })
        {
            var handle = (FieldDefinitionHandle)Get(row.Field, "Handle");
            var definition = reader.GetFieldDefinition(handle);
            Check(ReferenceEquals(Get(row.Field, "DeclaringClass"), compareClass) && compareDefinition.GetFields().Contains(handle)
                && reader.StringComparer.Equals(definition.Name, row.Name) && (int)definition.Attributes == row.Attributes
                && reader.GetBlobBytes(definition.Signature).SequenceEqual(new byte[] { 0x06, 0x0e }), "actual-field-declaration-" + row.Name);
        }
        bool Fields() => (bool)fieldPredicate.Invoke(null, new object?[] { compareClass, null, null })!;
        foreach (var field in new[] { nameField, sortField })
        {
            void RejectField(string key, object next)
            {
                object saved = Get(field, key); Set(field, key, next);
                try { Check(!Fields(), "field-" + key); Check(!(bool)predicate.Invoke(null, new[] { targets[0] })!, "getter-rejects-field-" + key); }
                finally { Set(field, key, saved); }
            }
            RejectField("DeclaringClass", Get(targets[0], "DeclaringClass"));
            RejectField("Handle", default(FieldDefinitionHandle));
            RejectField("IsStatic", true);
            RejectField("IsLiteral", true);
            RejectField("ByValArraySize", 1);
            var integer = Model("TypeDesc").GetMethod("MakePrimitive", statics)!.Invoke(null, new object[] { PrimitiveTypeCode.Int32 })!;
            RejectField("Type", integer);
            RejectField("Name", "unsupported-field");
        }
        var fields = (IList)Get(compareClass, "Fields");
        fields.Add(nameField);
        try { Check(!Fields(), "duplicate-name-field"); }
        finally { fields.RemoveAt(fields.Count - 1); }
        int index = fields.IndexOf(sortField);
        fields.RemoveAt(index);
        try { Check(!Fields(), "missing-sort-field"); }
        finally { fields.Insert(index, sortField); }
        Check(Fields(), "restored-fields");
        foreach (var vector in new (string Hex, bool Expected)[] {
            ("20001205", true), ("20001209", true), ("2000128970", true),
            ("20001105", false), ("20001C", false), ("20000E", false), ("2000101205", false),
            ("200020051205", false), ("00001205", false), ("3001001205", false),
            ("2001120508", false), ("20001200", false), ("20001201", false), ("20001207", false),
            ("2000120500", false), ("200012", false), ("", false) })
        {
            var metadata = new MetadataBuilder();
            metadata.AddModule(0, metadata.GetOrAddString("CompareInfoRaw"), metadata.GetOrAddGuid(Guid.Empty), default, default);
            var blob = metadata.GetOrAddBlob(Convert.FromHexString(vector.Hex));
            var image = new BlobBuilder(); new MetadataRootBuilder(metadata).Serialize(image, 0, 0);
            using var source = MetadataReaderProvider.FromMetadataImage(image.ToImmutableArray());
            Check(raw(source.GetMetadataReader().GetBlobReader(blob)) == vector.Expected, "raw-" + vector.Hex);
        }
        Console.WriteLine("dtfi-compare-info-guards:" + checks);
        void ColdPredicate(object method, MethodInfo matcher, string label)
        {
            object saved = Get(method, "_sig");
            var classes = (IList)Get(loaded, "Classes");
            int classCount = classes.Count;
            var watched = new[] { Get(method, "DeclaringClass"), compareClass }.SelectMany(owner =>
                ((IEnumerable)Get(owner, "Fields")).Cast<object>()).Distinct().ToArray();
            var ready = watched.Select(field => (bool)Get(field, "TypeReady")).ToArray();
            Set(method, "_sig", Activator.CreateInstance(saved.GetType())!);
            try
            {
                for (int pass = 0; pass < 2; pass++)
                {
                    Check(!(bool)Get(method, "SignatureReady"), label + "-cold-signature");
                    Check((bool)matcher.Invoke(null, new[] { method })!, label + "-cold-acceptance");
                    Check(!(bool)Get(method, "SignatureReady") && classes.Count == classCount, label + "-non-decoding");
                    Check(watched.Select(field => (bool)Get(field, "TypeReady")).SequenceEqual(ready), label + "-field-readiness");
                }
            }
            finally { Set(method, "_sig", saved); }
        }
        int initializerStart = checks;
        var initializer = Model("CoreIntrinsics").GetMethod("IsInvariantCompareInfoInitializer", statics)!;
        var initializerFields = Model("CoreIntrinsics").GetMethod("TryGetInvariantCompareInfoInitializerFields", statics)!;
        var initializers = compareDefinition.GetMethods().Select(handle => ((IDictionary)Get(module, "MethodMap"))[handle]!)
            .Where(method => (string)Get(method, "Name") is ".ctor" or "InitSort")
            .Where(method => ((IEnumerable)Get(Get(method, "Signature"), "ParameterTypes")).Cast<object>().ToArray() is [var parameter]
                && Get(parameter, "Class") is { } cls && (string)Get(cls, "FullName") == "System.Globalization.CultureInfo").ToArray();
        Check(initializers.Length == 2, "initializer-pair");
        foreach (var actual in initializers)
        {
            Check((bool)initializer.Invoke(null, new[] { actual })!, "actual-initializer-" + Get(actual, "Name"));
            ColdPredicate(actual, initializer, "initializer-" + Get(actual, "Name"));
            Console.WriteLine("dtfi-compare-info-initializer-installed:" + MetadataTokens.GetToken((MethodDefinitionHandle)Get(actual, "Handle")).ToString("X8", CultureInfo.InvariantCulture));
        }
        int initializerImports = 0;
        foreach (var handle in appReader.MemberReferences)
        {
            if (appReader.GetString(appReader.GetMemberReference(handle).Name) is not (".ctor" or "InitSort")) continue;
            var resolved = Model("Compilation").GetMethod("ResolveMemberRefMethod", instance)!.Invoke(loaded, new[] { appModule, (object)handle, context })!;
            Check(initializers.Any(target => ReferenceEquals(target, resolved)), "initializer-memberref-identity");
            Check((bool)initializer.Invoke(null, new[] { resolved })!, "initializer-memberref-positive");
            initializerImports++;
        }
        Check(initializerImports == 2, "initializer-memberrefs");
        var integerType = Model("TypeDesc").GetMethod("MakePrimitive", statics)!.Invoke(null, new object[] { PrimitiveTypeCode.Int32 })!;
        var noArguments = Array.CreateInstance(Model("TypeDesc"), 0);
        var oneArgument = Array.CreateInstance(Model("TypeDesc"), 1); oneArgument.SetValue(integerType, 0);
        var typeContext = Activator.CreateInstance(Model("GenericContext"), new object[] { oneArgument, noArguments })!;
        var methodContext = Activator.CreateInstance(Model("GenericContext"), new object[] { noArguments, oneArgument })!;
        foreach (var actual in initializers)
        {
            void Reject(object value, string key, object next)
            {
                object saved = Get(value, key); Set(value, key, next);
                try { Check(!(bool)initializer.Invoke(null, new[] { actual })!, "initializer-reject-" + key); }
                finally { Set(value, key, saved); }
            }
            var signature = Get(actual, "Signature");
            var parameter = ((IEnumerable)Get(signature, "ParameterTypes")).Cast<object>().Single();
            var culture = Get(parameter, "Class");
            Reject(actual, "Module", appModule);
            Reject(actual, "Handle", default(MethodDefinitionHandle));
            Reject(actual, "Handle", Get(initializers.Single(other => !ReferenceEquals(other, actual)), "Handle"));
            Reject(actual, "Rva", 0);
            Reject(actual, "Name", "InitializeSort");
            Reject(actual, "Attributes", (MethodAttributes)Get(actual, "Attributes") | MethodAttributes.Static);
            Reject(actual, "ImplAttributes", MethodImplAttributes.NoInlining);
            foreach (var specialized in new[] { typeContext, methodContext })
            {
                Reject(actual, "Context", specialized);
                Reject(compareClass, "Context", specialized);
                Reject(culture, "Context", specialized);
            }
            Reject(compareClass, "Module", appModule);
            Reject(compareClass, "_fullName", "Application.CompareInfo");
            Reject(compareClass, "Handle", Get(culture, "Handle"));
            Reject(compareClass, "IsValueType", true);
            Reject(compareClass, "IsInterface", true);
            Reject(compareClass, "GenericArity", 1);
            Reject(culture, "Module", appModule);
            Reject(culture, "_fullName", "Application.CultureInfo");
            Reject(culture, "Handle", Get(compareClass, "Handle"));
            Reject(culture, "IsValueType", true);
            Reject(culture, "IsInterface", true);
            Reject(culture, "GenericArity", 1);
            Reject(parameter, "Kind", Enum.Parse(Model("TypeKind"), "ByRef"));
            foreach (var changed in InitializerSignatures(signature, integerType)) Reject(actual, "Signature", changed);
            Check((bool)initializer.Invoke(null, new[] { actual })!, "restored-initializer");
        }
        object?[] initializerFieldArgs = { compareClass, null, null, null };
        Check((bool)initializerFields.Invoke(null, initializerFieldArgs)!, "actual-initializer-fields");
        Check(ReferenceEquals(initializerFieldArgs[1], nameField) && ReferenceEquals(initializerFieldArgs[2], sortField), "initializer-shared-string-fields");
        var asciiField = initializerFieldArgs[3]!;
        var asciiHandle = (FieldDefinitionHandle)Get(asciiField, "Handle");
        var asciiDefinition = reader.GetFieldDefinition(asciiHandle);
        Check(ReferenceEquals(Get(asciiField, "DeclaringClass"), compareClass) && compareDefinition.GetFields().Contains(asciiHandle)
            && reader.StringComparer.Equals(asciiDefinition.Name, "_isAsciiEqualityOrdinal") && (int)asciiDefinition.Attributes == 0x81
            && reader.GetBlobBytes(asciiDefinition.Signature).SequenceEqual(new byte[] { 0x06, 0x02 }), "actual-ascii-field-declaration");
        bool InitializerFields() => (bool)initializerFields.Invoke(null, new object?[] { compareClass, null, null, null })!;
        void RejectInitializerFields(string label)
        {
            Check(!InitializerFields(), "initializer-fields-" + label);
            foreach (var actual in initializers) Check(!(bool)initializer.Invoke(null, new[] { actual })!, "initializer-rejects-fields-" + label);
        }
        foreach (var field in new[] { nameField, sortField, asciiField })
        {
            foreach (var change in new (string Key, object Next)[] {
                ("DeclaringClass", Get(targets[0], "DeclaringClass")), ("Handle", default(FieldDefinitionHandle)),
                ("IsStatic", true), ("IsLiteral", true), ("ByValArraySize", 1), ("Type", integerType), ("Name", "unsupported-field") })
            {
                object saved = Get(field, change.Key); Set(field, change.Key, change.Next);
                try { RejectInitializerFields(change.Key); }
                finally { Set(field, change.Key, saved); }
            }
        }
        fields.Add(asciiField);
        try { RejectInitializerFields("duplicate-ascii"); }
        finally { fields.RemoveAt(fields.Count - 1); }
        index = fields.IndexOf(asciiField); fields.RemoveAt(index);
        try
        {
            RejectInitializerFields("missing-ascii");
            Check((bool)predicate.Invoke(null, new[] { targets[0] })!, "getter-does-not-require-ascii-field");
        }
        finally { fields.Insert(index, asciiField); }
        Check(InitializerFields(), "restored-initializer-fields");
        Console.WriteLine("dtfi-compare-info-initializer-guards:" + (checks - initializerStart));
        int validatorStart = checks;
        var validatorPredicate = Model("CoreIntrinsics").GetMethod("IsCultureInfoNameValidator", statics)!;
        var validatorRaw = Model("CoreIntrinsics").GetMethod("IsCultureInfoNameValidatorSignature", statics)!.CreateDelegate<RawPredicate>();
        var validatorDecoded = Model("CoreIntrinsics").GetMethod("IsCultureInfoNameValidatorDecodedSignature", statics)!;
        var cultureClass = Get(targets[1], "DeclaringClass");
        var validatorHandles = reader.GetTypeDefinition((TypeDefinitionHandle)Get(cultureClass, "Handle")).GetMethods()
            .Where(handle => reader.StringComparer.Equals(reader.GetMethodDefinition(handle).Name, "VerifyCultureName")).ToArray();
        Check(validatorHandles.Length == 2, "validator-sibling-inventory");
        var validatorHandle = validatorHandles.Single(handle => reader.GetBlobBytes(reader.GetMethodDefinition(handle).Signature)
            .SequenceEqual(new byte[] { 0, 2, 2, 14, 2 }));
        var actualValidator = ((IDictionary)Get(module, "MethodMap"))[validatorHandle]!;
        ColdPredicate(actualValidator, validatorPredicate, "validator");
        foreach (var handle in validatorHandles)
            Check((bool)validatorPredicate.Invoke(null, new[] { ((IDictionary)Get(module, "MethodMap"))[handle]! })!
                == (handle == validatorHandle), "validator-only-string-sibling");
        Check(validatorRaw(reader.GetBlobReader(reader.GetMethodDefinition(validatorHandle).Signature)), "validator-raw-actual");
        Check((bool)validatorDecoded.Invoke(null, new[] { Get(actualValidator, "Signature") })!, "validator-decoded-actual");
        int validatorImports = 0;
        foreach (var handle in appReader.MemberReferences)
        {
            if (!appReader.StringComparer.Equals(appReader.GetMemberReference(handle).Name, "VerifyCultureName")) continue;
            var resolved = Model("Compilation").GetMethod("ResolveMemberRefMethod", instance)!.Invoke(loaded, new[] { appModule, (object)handle, context })!;
            Check(ReferenceEquals(resolved, actualValidator), "validator-import-identity");
            Check((bool)validatorPredicate.Invoke(null, new[] { resolved })!, "validator-import-shape");
            validatorImports++;
        }
        Check(validatorImports == 1, "validator-single-import");
        void RejectValidator(object value, string key, object next)
        {
            object saved = Get(value, key); Set(value, key, next);
            try { Check(!(bool)validatorPredicate.Invoke(null, new[] { actualValidator })!, "validator-reject-" + key); }
            finally { Set(value, key, saved); }
        }
        RejectValidator(actualValidator, "Module", appModule);
        RejectValidator(actualValidator, "Handle", default(MethodDefinitionHandle));
        RejectValidator(actualValidator, "Handle", validatorHandles.Single(handle => handle != validatorHandle));
        RejectValidator(actualValidator, "Name", "OtherValidator");
        RejectValidator(actualValidator, "Rva", 0);
        RejectValidator(actualValidator, "Rva", (int)Get(actualValidator, "Rva") + 1);
        RejectValidator(actualValidator, "IsSynthetic", true);
        foreach (var flags in new[] { MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig,
            MethodAttributes.Assembly | MethodAttributes.HideBySig }) RejectValidator(actualValidator, "Attributes", flags);
        RejectValidator(actualValidator, "ImplAttributes", MethodImplAttributes.AggressiveInlining);
        foreach (var specialized in new[] { typeContext, methodContext })
        {
            RejectValidator(actualValidator, "Context", specialized);
            RejectValidator(cultureClass, "Context", specialized);
        }
        RejectValidator(cultureClass, "Module", appModule);
        RejectValidator(cultureClass, "_fullName", "Application.CultureInfo");
        RejectValidator(cultureClass, "Handle", default(TypeDefinitionHandle));
        RejectValidator(cultureClass, "IsValueType", true);
        RejectValidator(cultureClass, "GenericArity", 1);
        foreach (var changed in ValidatorSignatures(Get(actualValidator, "Signature"), integerType))
            RejectValidator(actualValidator, "Signature", changed);
        foreach (var vector in new (string Hex, bool Expected)[] {
            ("0002020E02", true), ("", false), ("0002020E", false), ("0002020E0200", false),
            ("2002020E02", false), ("100102020E02", false), ("0002080E02", false), ("000202100E02", false),
            ("0002020E08", false), ("0003020E0202", false), ("00020220050E02", false) })
        {
            var metadata = new MetadataBuilder();
            metadata.AddModule(0, metadata.GetOrAddString("CultureValidatorRaw"), metadata.GetOrAddGuid(Guid.Empty), default, default);
            var blob = metadata.GetOrAddBlob(Convert.FromHexString(vector.Hex));
            var image = new BlobBuilder(); new MetadataRootBuilder(metadata).Serialize(image, 0, 0);
            using var source = MetadataReaderProvider.FromMetadataImage(image.ToImmutableArray());
            Check(validatorRaw(source.GetMetadataReader().GetBlobReader(blob)) == vector.Expected, "validator-raw-" + vector.Hex);
        }
        var diet = Assembly.LoadFrom(Path.Combine(Path.GetDirectoryName(Path.GetFullPath(args[1]))!, "ildiet", "ILDiet.dll"));
        var copied = diet.GetType("Dn2Cpp.AssemblyDiet", true)!.GetMethod("IsCopiedCultureInfoNameValidator", statics)!;
        using (var core = Mono.Cecil.AssemblyDefinition.ReadAssembly(args[2]))
        {
            foreach (var method in core.MainModule.GetType("System.Globalization.CultureInfo").Methods.Where(method => method.Name == "VerifyCultureName"))
            {
                bool valid = method.Parameters[0].ParameterType.MetadataType == Mono.Cecil.MetadataType.String;
                Check((bool)copied.Invoke(null, new object[] { method, method.DeclaringType.FullName })! == valid, "validator-diet-sibling");
                var flags = method.Attributes; method.Attributes |= Mono.Cecil.MethodAttributes.Virtual;
                try { Check(!(bool)copied.Invoke(null, new object[] { method, method.DeclaringType.FullName })!, "validator-diet-flags"); }
                finally { method.Attributes = flags; }
            }
        }
        Check((bool)validatorPredicate.Invoke(null, new[] { actualValidator })!, "validator-restored");
        Console.WriteLine("culture-name-validator-installed:" + MetadataTokens.GetToken(validatorHandle).ToString("X8", CultureInfo.InvariantCulture));
        Console.WriteLine("culture-name-validator-guards:" + (checks - validatorStart));

        return true;
    }

    private static IEnumerable<object> ValidatorSignatures(object signature, object integer)
    {
        object Get(string name) => signature.GetType().GetProperty(name)!.GetValue(signature)!;
        var parameters = ((IEnumerable)Get("ParameterTypes")).Cast<object>().ToArray();
        var create = typeof(System.Collections.Immutable.ImmutableArray).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(method => method.Name == "Create" && method.IsGenericMethodDefinition
                && method.GetParameters() is [var parameter] && parameter.ParameterType.IsArray).MakeGenericMethod(integer.GetType());
        var constructor = signature.GetType().GetConstructors().Single();
        foreach (string mode in new[] { "instance", "generic", "return", "arity", "first", "second", "required" })
        {
            var values = Array.CreateInstance(integer.GetType(), mode == "arity" ? 1 : 2);
            for (int i = 0; i < values.Length; i++) values.SetValue(mode == (i == 0 ? "first" : "second") ? integer : parameters[i], i);
            var immutable = create.Invoke(null, new object[] { values })!;
            yield return constructor.Invoke(constructor.GetParameters().Select(parameter => parameter.Name switch {
                "header" => mode == "instance" ? new SignatureHeader(0x20) : Get("Header"),
                "returnType" => mode == "return" ? integer : Get("ReturnType"),
                "requiredParameterCount" => mode == "required" ? 1 : values.Length,
                "genericParameterCount" => mode == "generic" ? 1 : Get("GenericParameterCount"),
                "parameterTypes" => immutable,
                _ => throw new InvalidOperationException("unknown signature constructor parameter") }).ToArray());
        }
    }

    private static IEnumerable<object> InitializerSignatures(object signature, object integer)
    {
        object Get(string name) => signature.GetType().GetProperty(name)!.GetValue(signature)!;
        var parameters = ((IEnumerable)Get("ParameterTypes")).Cast<object>().ToArray();
        var create = typeof(System.Collections.Immutable.ImmutableArray).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(method => method.Name == "Create" && method.IsGenericMethodDefinition
                && method.GetParameters() is [var parameter] && parameter.ParameterType.IsArray).MakeGenericMethod(integer.GetType());
        var constructor = signature.GetType().GetConstructors().Single();
        foreach (string mode in new[] { "static", "generic", "return", "arity", "parameter" })
        {
            var values = Array.CreateInstance(integer.GetType(), mode == "arity" ? 0 : 1);
            if (values.Length != 0) values.SetValue(mode == "parameter" ? integer : parameters[0], 0);
            var immutable = create.Invoke(null, new object[] { values })!;
            yield return constructor.Invoke(constructor.GetParameters().Select(parameter => parameter.Name switch {
                "header" => mode == "static" ? new SignatureHeader(0) : Get("Header"),
                "returnType" => mode == "return" ? integer : Get("ReturnType"),
                "requiredParameterCount" => values.Length,
                "genericParameterCount" => mode == "generic" ? 1 : Get("GenericParameterCount"),
                "parameterTypes" => immutable,
                _ => throw new InvalidOperationException("unknown signature constructor parameter") }).ToArray());
        }
    }
}
