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
        Check(MetadataTokens.GetToken((FieldDefinitionHandle)Get(nameField, "Handle")) == 0x0400085c, "name-field-token");
        Check(MetadataTokens.GetToken((FieldDefinitionHandle)Get(sortField, "Handle")) == 0x0400085e, "sort-field-token");
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
        return true;
    }
}
