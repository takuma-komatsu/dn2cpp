using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;

internal static class AbbreviatedDayGuards
{
    private delegate bool RawPredicate(BlobReader blob);

    public static bool Run(string[] args)
    {
        if (args.Length != 4 || args[0] != "--abbreviated-day-guards") return false;
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
        using (var empty = Mono.Cecil.AssemblyDefinition.CreateAssembly(new Mono.Cecil.AssemblyNameDefinition("AbbreviatedDayGuardInput", new Version(1, 0)), "AbbreviatedDayGuardInput", Mono.Cecil.ModuleKind.Dll))
        using (var core = Mono.Cecil.AssemblyDefinition.ReadAssembly(args[2]))
        {
            var owner = core.MainModule.GetType("System.Globalization.DateTimeFormatInfo");
            var host = new Mono.Cecil.TypeDefinition("Guard", "References", Mono.Cecil.TypeAttributes.Public, empty.MainModule.TypeSystem.Object);
            empty.MainModule.Types.Add(host);
            foreach (string name in new[] { "GetAbbreviatedDayName", "GetDayName", "get_AbbreviatedDayNames" })
            {
                var target = empty.MainModule.ImportReference(owner.Methods.Single(m => m.Name == name));
                var stub = new Mono.Cecil.MethodDefinition(name, Mono.Cecil.MethodAttributes.Public | Mono.Cecil.MethodAttributes.Static, empty.MainModule.TypeSystem.Void);
                host.Methods.Add(stub);
                var il = stub.Body.GetILProcessor();
                il.Emit(Mono.Cecil.Cil.OpCodes.Ldnull);
                if (target.Parameters.Count != 0) il.Emit(Mono.Cecil.Cil.OpCodes.Ldc_I4_0);
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
        var module = ((IEnumerable)Get(loaded, "Modules")).Cast<object>().Single(m => (string)Get(m, "AssemblyName") == "System.Private.CoreLib");
        var reader = (MetadataReader)Get(module, "Reader");
        var ownerHandle = reader.TypeDefinitions.Single(h =>
        {
            var type = reader.GetTypeDefinition(h);
            return reader.GetString(type.Name) == "DateTimeFormatInfo" && reader.GetString(type.Namespace) == "System.Globalization" && type.GetDeclaringType().IsNil;
        });
        var targetHandle = reader.GetTypeDefinition(ownerHandle).GetMethods().Single(h => reader.GetString(reader.GetMethodDefinition(h).Name) == "GetAbbreviatedDayName");
        var actual = ((IDictionary)Get(module, "MethodMap"))[targetHandle]!;
        var ownerClass = Get(actual, "DeclaringClass");
        var predicate = Model("CoreIntrinsics").GetMethod("IsDateTimeFormatInfoAbbreviatedDayName", statics)!;
        var raw = Model("CoreIntrinsics").GetMethod("IsDateTimeFormatInfoAbbreviatedDayNameSignature", statics)!.CreateDelegate<RawPredicate>();
        int checks = 0;
        void Check(bool result, string label)
        {
            if (!result) throw new InvalidOperationException("abbreviated day guard: " + label);
            checks++;
        }
        Check((bool)predicate.Invoke(null, new[] { actual })!, "actual-CoreLib");
        Console.WriteLine("abbreviated-day-actual:" + MetadataTokens.GetToken(targetHandle).ToString("X8", CultureInfo.InvariantCulture));
        var appModule = ((IEnumerable)Get(loaded, "Modules")).Cast<object>().Single(m => (string)Get(m, "AssemblyName") == "AbbreviatedDayGuardInput");
        var appReader = (MetadataReader)Get(appModule, "Reader");
        var context = Model("GenericContext").GetField("Empty", statics)!.GetValue(null)!;
        int imports = 0;
        foreach (var h in appReader.MemberReferences)
        {
            string name = appReader.GetString(appReader.GetMemberReference(h).Name);
            if (name is not ("GetAbbreviatedDayName" or "GetDayName" or "get_AbbreviatedDayNames")) continue;
            var resolved = Model("Compilation").GetMethod("ResolveMemberRefMethod", instance)!.Invoke(loaded, new[] { appModule, (object)h, context })!;
            var expected = reader.GetTypeDefinition(ownerHandle).GetMethods().Single(x => reader.GetString(reader.GetMethodDefinition(x).Name) == name);
            Check(ReferenceEquals(Get(resolved, "Module"), module) && (MethodDefinitionHandle)Get(resolved, "Handle") == expected, "resolved-memberref-" + name);
            Check((bool)predicate.Invoke(null, new[] { resolved })! == (name == "GetAbbreviatedDayName"), "exact-neighbor-policy-" + name);
            imports++;
        }
        Check(imports == 3, "all-memberrefs");
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
        Reject(actual, "Name", "GetDayName");
        var attrs = (MethodAttributes)Get(actual, "Attributes");
        foreach (var flags in new[] { attrs | MethodAttributes.Static, attrs | MethodAttributes.Virtual,
            attrs | MethodAttributes.PinvokeImpl, MethodAttributes.Private | MethodAttributes.HideBySig })
            Reject(actual, "Attributes", flags);
        foreach (var impl in new[] { MethodImplAttributes.InternalCall, MethodImplAttributes.NoInlining,
            MethodImplAttributes.Synchronized, MethodImplAttributes.PreserveSig })
            Reject(actual, "ImplAttributes", impl);
        var parameter = ((IEnumerable)Get(Get(actual, "Signature"), "ParameterTypes")).Cast<object>().Single();
        var day = Get(parameter, "Class");
        Reject(day, "IsEnum", false);
        Reject(day, "EnumUnderlying", PrimitiveTypeCode.Int64);
        Reject(day, "_fullName", "System.DateTimeKind");
        Reject(day, "Handle", ownerHandle);
        Reject(day, "Handle", default(TypeDefinitionHandle));
        Reject(day, "GenericArity", 1);
        Reject(day, "Module", appModule);
        Reject(parameter, "Kind", Enum.Parse(Model("TypeKind"), "ByRef"));
        var vectors = new (string Hex, bool Expected)[]
        {
            ("20010E1105", true), ("20010E1108", true), ("20010E118354", true),
            ("20010E1205", false), ("00010E1105", false), ("3001010E1105", false),
            ("20010E08", false), ("20010E101105", false), ("20010E20051105", false),
            ("20011C1105", false), ("20020E11051105", false), ("20010E110500", false),
            ("20010E1101", false), ("20010E1107", false), ("20010E11", false), ("", false)
        };
        foreach (var vector in vectors)
        {
            var metadata = new MetadataBuilder();
            metadata.AddModule(0, metadata.GetOrAddString("DayRaw"), metadata.GetOrAddGuid(Guid.Empty), default, default);
            var blob = metadata.GetOrAddBlob(Convert.FromHexString(vector.Hex));
            var image = new BlobBuilder();
            new MetadataRootBuilder(metadata).Serialize(image, 0, 0);
            using var source = MetadataReaderProvider.FromMetadataImage(image.ToImmutableArray());
            Check(raw(source.GetMetadataReader().GetBlobReader(blob)) == vector.Expected, "raw-" + vector.Hex);
        }
        Check((bool)predicate.Invoke(null, new[] { actual })!, "restored-actual");
        Console.WriteLine("abbreviated-day-guards:" + checks);
        return true;
    }
}
