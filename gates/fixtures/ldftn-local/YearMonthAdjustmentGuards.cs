using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;

internal static class YearMonthAdjustmentGuards
{
    private delegate bool RawPredicate(BlobReader blob);

    public static bool Run(string[] args)
    {
        if (args.Length != 4 || args[0] != "--year-month-adjustment-guards") return false;
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
        using (var empty = Mono.Cecil.AssemblyDefinition.CreateAssembly(new Mono.Cecil.AssemblyNameDefinition("YearMonthAdjustmentGuardInput", new Version(1, 0)), "YearMonthAdjustmentGuardInput", Mono.Cecil.ModuleKind.Dll))
        using (var core = Mono.Cecil.AssemblyDefinition.ReadAssembly(args[2]))
        {
            var owner = core.MainModule.GetType("System.Globalization.DateTimeFormatInfo");
            var host = new Mono.Cecil.TypeDefinition("Guard", "References", Mono.Cecil.TypeAttributes.Public, empty.MainModule.TypeSystem.Object);
            empty.MainModule.Types.Add(host);
            foreach (string name in new[] { "YearMonthAdjustment", "get_HasYearMonthAdjustment" })
            {
                var target = empty.MainModule.ImportReference(owner.Methods.Single(m => m.Name == name));
                var stub = new Mono.Cecil.MethodDefinition(name, Mono.Cecil.MethodAttributes.Public | Mono.Cecil.MethodAttributes.Static, empty.MainModule.TypeSystem.Void);
                host.Methods.Add(stub);
                stub.Body.InitLocals = true;
                var il = stub.Body.GetILProcessor();
                il.Emit(Mono.Cecil.Cil.OpCodes.Ldnull);
                foreach (var parameter in target.Parameters)
                {
                    if (parameter.ParameterType is Mono.Cecil.ByReferenceType)
                    {
                        var output = new Mono.Cecil.Cil.VariableDefinition(empty.MainModule.TypeSystem.Int32);
                        stub.Body.Variables.Add(output);
                        il.Emit(Mono.Cecil.Cil.OpCodes.Ldloca, output);
                    }
                    else il.Emit(Mono.Cecil.Cil.OpCodes.Ldc_I4_0);
                }
                il.Emit(Mono.Cecil.Cil.OpCodes.Call, target);
                if (target.ReturnType.MetadataType != Mono.Cecil.MetadataType.Void) il.Emit(Mono.Cecil.Cil.OpCodes.Pop);
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
        var module = ((IEnumerable)Get(loaded, "Modules")).Cast<object>().Single(m => (string)Get(m, "AssemblyName") == "System.Private.CoreLib");
        var reader = (MetadataReader)Get(module, "Reader");
        var ownerHandle = reader.TypeDefinitions.Single(h =>
        {
            var type = reader.GetTypeDefinition(h);
            return reader.GetString(type.Name) == "DateTimeFormatInfo" && reader.GetString(type.Namespace) == "System.Globalization" && type.GetDeclaringType().IsNil;
        });
        var targetHandle = reader.GetTypeDefinition(ownerHandle).GetMethods().Single(h => reader.GetString(reader.GetMethodDefinition(h).Name) == "YearMonthAdjustment");
        var actual = ((IDictionary)Get(module, "MethodMap"))[targetHandle]!;
        var ownerClass = Get(actual, "DeclaringClass");
        var predicate = Model("CoreIntrinsics").GetMethod("IsDateTimeFormatInfoYearMonthAdjustment", statics)!;
        var raw = Model("CoreIntrinsics").GetMethod("IsDateTimeFormatInfoYearMonthAdjustmentSignature", statics)!.CreateDelegate<RawPredicate>();
        int checks = 0;
        void Check(bool result, string label)
        {
            if (!result) throw new InvalidOperationException("year/month adjustment guard: " + label);
            checks++;
        }
        Check((bool)predicate.Invoke(null, new[] { actual })!, "actual-CoreLib");
        var hasPredicate = Model("CoreIntrinsics").GetMethod("IsDateTimeFormatInfoParserFlagGetter", statics)!;
        var hasRaw = Model("CoreIntrinsics").GetMethod("IsDateTimeFormatInfoParserFlagGetterSignature", statics)!.CreateDelegate<RawPredicate>();
        var hasHandle = reader.GetTypeDefinition(ownerHandle).GetMethods().Single(h => reader.GetString(reader.GetMethodDefinition(h).Name) == "get_HasYearMonthAdjustment");
        var hasActual = ((IDictionary)Get(module, "MethodMap"))[hasHandle]!;
        Check((bool)hasPredicate.Invoke(null, new[] { hasActual })!, "actual-Has");
        Check(!(bool)predicate.Invoke(null, new[] { hasActual })!, "Has-not-adjustment");
        Check(!(bool)hasPredicate.Invoke(null, new[] { actual })!, "adjustment-not-Has");
        Check(hasRaw(reader.GetBlobReader(reader.GetMethodDefinition(hasHandle).Signature)), "actual-Has-raw");
        Console.WriteLine("year-month-adjustment-actual:" + MetadataTokens.GetToken(targetHandle).ToString("X8", CultureInfo.InvariantCulture));
        var appModule = ((IEnumerable)Get(loaded, "Modules")).Cast<object>().Single(m => (string)Get(m, "AssemblyName") == "YearMonthAdjustmentGuardInput");
        var appReader = (MetadataReader)Get(appModule, "Reader");
        var context = Model("GenericContext").GetField("Empty", statics)!.GetValue(null)!;
        int imports = 0;
        foreach (var h in appReader.MemberReferences)
        {
            string name = appReader.GetString(appReader.GetMemberReference(h).Name);
            if (name is not ("YearMonthAdjustment" or "get_HasYearMonthAdjustment")) continue;
            var resolved = Model("Compilation").GetMethod("ResolveMemberRefMethod", instance)!.Invoke(loaded, new[] { appModule, (object)h, context })!;
            var expected = reader.GetTypeDefinition(ownerHandle).GetMethods().Single(x => reader.GetString(reader.GetMethodDefinition(x).Name) == name);
            Check(ReferenceEquals(Get(resolved, "Module"), module) && (MethodDefinitionHandle)Get(resolved, "Handle") == expected, "resolved-memberref-" + name);
            Check((bool)predicate.Invoke(null, new[] { resolved })! == (name == "YearMonthAdjustment"), "exact-neighbor-policy-" + name);
            imports++;
        }
        Check(imports == 2, "all-memberrefs");
        void Reject(object value, string field, object next)
        {
            object saved = Get(value, field);
            Set(value, field, next);
            try { Check(!(bool)predicate.Invoke(null, new[] { actual })!, "wrong-" + field); }
            finally { Set(value, field, saved); }
        }
        Reject(module, "AssemblyName", "Application.CoreLib");
        Reject(ownerClass, "_fullName", "Application.DateTimeFormatInfo");
        Reject(ownerClass, "Handle", default(TypeDefinitionHandle));
        Reject(ownerClass, "IsValueType", true);
        Reject(ownerClass, "IsInterface", true);
        Reject(ownerClass, "GenericArity", 1);
        Reject(actual, "Handle", default(MethodDefinitionHandle));
        Reject(actual, "Rva", 0);
        Reject(actual, "Name", "get_HasYearMonthAdjustment");
        var attrs = (MethodAttributes)Get(actual, "Attributes");
        foreach (var flags in new[] { attrs | MethodAttributes.Static, attrs | MethodAttributes.Virtual,
            attrs | MethodAttributes.PinvokeImpl, attrs | MethodAttributes.SpecialName, MethodAttributes.Public | MethodAttributes.HideBySig })
            Reject(actual, "Attributes", flags);
        foreach (var impl in new[] { MethodImplAttributes.InternalCall, MethodImplAttributes.NoInlining,
            MethodImplAttributes.Synchronized, MethodImplAttributes.PreserveSig })
            Reject(actual, "ImplAttributes", impl);
        var returned = Get(Get(actual, "Signature"), "ReturnType");
        Reject(returned, "Primitive", PrimitiveTypeCode.Int32);
        Reject(returned, "Kind", Enum.Parse(Model("TypeKind"), "Class"));
        var parameters = ((IEnumerable)Get(Get(actual, "Signature"), "ParameterTypes")).Cast<object>().ToArray();
        for (int index = 0; index < 2; index++)
        {
            Reject(parameters[index], "Kind", Enum.Parse(Model("TypeKind"), "Primitive"));
            Reject(Get(parameters[index], "Element"), "Primitive", PrimitiveTypeCode.Int64);
        }
        Reject(parameters[2], "Primitive", PrimitiveTypeCode.Int32);
        var vectors = new (string Hex, bool Expected)[]
        {
            ("2003021008100802", true), ("0003021008100802", false),
            ("300103021008100802", false), ("2003081008100802", false),
            ("20030208100802", false), ("20030210080802", false),
            ("200302100A100802", false), ("2003021008100A02", false),
            ("2003021008100808", false), ("200302100810081002", false),
            ("200302101205100802", false), ("20030220051008100802", false),
            ("20020210081008", false), ("200302100810080200", false),
            ("20030210081008", false), ("", false)
        };
        foreach (var vector in vectors)
        {
            var metadata = new MetadataBuilder();
            metadata.AddModule(0, metadata.GetOrAddString("AdjustmentRaw"), metadata.GetOrAddGuid(Guid.Empty), default, default);
            var blob = metadata.GetOrAddBlob(Convert.FromHexString(vector.Hex));
            var image = new BlobBuilder();
            new MetadataRootBuilder(metadata).Serialize(image, 0, 0);
            using var source = MetadataReaderProvider.FromMetadataImage(image.ToImmutableArray());
            Check(raw(source.GetMetadataReader().GetBlobReader(blob)) == vector.Expected, "raw-" + vector.Hex);
        }
        foreach (string hex in new[] { "200002", "000002", "200008", "20001205", "20001002", "2000200502", "20010202", "30010002", "20000200", "2000", "" })
        {
            var metadata = new MetadataBuilder();
            metadata.AddModule(0, metadata.GetOrAddString("HasAdjustmentRaw"), metadata.GetOrAddGuid(Guid.Empty), default, default);
            var blob = metadata.GetOrAddBlob(Convert.FromHexString(hex));
            var image = new BlobBuilder();
            new MetadataRootBuilder(metadata).Serialize(image, 0, 0);
            using var source = MetadataReaderProvider.FromMetadataImage(image.ToImmutableArray());
            Check(hasRaw(source.GetMetadataReader().GetBlobReader(blob)) == (hex == "200002"), "Has-raw-" + hex);
        }
        Check((bool)predicate.Invoke(null, new[] { actual })!, "restored-actual");
        Console.WriteLine("year-month-adjustment-guards:" + checks);
        return true;
    }
}
