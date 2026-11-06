using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;

internal static class DtfINameArraysGuards
{
    private delegate bool RawPredicate(BlobReader blob, bool hasBoolean);

    public static bool Run(string[] args)
    {
        if (args.Length != 4 || args[0] != "--dtfi-name-arrays-guards") return false;
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
        string[] accepted = { "InternalGetGenitiveMonthNames", "InternalGetLeapYearMonthNames", "get_AbbreviatedMonthNames", "get_AbbreviatedMonthGenitiveNames" };
        string[] all = accepted.Concat(new[] { "get_MonthNames", "get_MonthGenitiveNames", "InternalGetAbbreviatedDayOfWeekNames" }).ToArray();
        string input = Path.GetFullPath(args[3]);
        if (File.Exists(input)) throw new InvalidOperationException("model app must be new");
        using (var empty = Mono.Cecil.AssemblyDefinition.CreateAssembly(new Mono.Cecil.AssemblyNameDefinition("DtfINameArraysGuardInput", new Version(1, 0)), "DtfINameArraysGuardInput", Mono.Cecil.ModuleKind.Dll))
        using (var core = Mono.Cecil.AssemblyDefinition.ReadAssembly(args[2]))
        {
            var owner = core.MainModule.GetType("System.Globalization.DateTimeFormatInfo");
            var host = new Mono.Cecil.TypeDefinition("Guard", "References", Mono.Cecil.TypeAttributes.Public, empty.MainModule.TypeSystem.Object);
            empty.MainModule.Types.Add(host);
            foreach (string name in all)
            {
                var target = empty.MainModule.ImportReference(owner.Methods.Single(m => m.Name == name));
                var stub = new Mono.Cecil.MethodDefinition(name, Mono.Cecil.MethodAttributes.Public | Mono.Cecil.MethodAttributes.Static, empty.MainModule.TypeSystem.Void);
                host.Methods.Add(stub);
                var il = stub.Body.GetILProcessor();
                il.Emit(Mono.Cecil.Cil.OpCodes.Ldnull);
                if (target.Parameters.Count != 0) il.Emit(Mono.Cecil.Cil.OpCodes.Ldc_I4_0);
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
        var targetHandle = reader.GetTypeDefinition(ownerHandle).GetMethods().Single(h => reader.GetString(reader.GetMethodDefinition(h).Name) == "InternalGetGenitiveMonthNames");
        var actual = ((IDictionary)Get(module, "MethodMap"))[targetHandle]!;
        var ownerClass = Get(actual, "DeclaringClass");
        var predicate = Model("CoreIntrinsics").GetMethod("IsDateTimeFormatInfoMonthArray", statics)!;
        var raw = Model("CoreIntrinsics").GetMethod("IsDateTimeFormatInfoMonthArraySignature", statics)!.CreateDelegate<RawPredicate>();
        int checks = 0;
        void Check(bool result, string label)
        {
            if (!result) throw new InvalidOperationException("DTFI name arrays guard: " + label);
            checks++;
        }
        Check((bool)predicate.Invoke(null, new[] { actual })!, "actual-CoreLib");
        foreach (var h in reader.GetTypeDefinition(ownerHandle).GetMethods().Where(h => all.Contains(reader.GetString(reader.GetMethodDefinition(h).Name))))
        {
            var candidate = ((IDictionary)Get(module, "MethodMap"))[h]!;
            string name = reader.GetString(reader.GetMethodDefinition(h).Name);
            Check((bool)predicate.Invoke(null, new[] { candidate })! == accepted.Contains(name), "actual-MethodDef-policy-" + name);
            if (accepted.Contains(name))
            {
                int expectedAttributes = name.StartsWith("get_", StringComparison.Ordinal) ? 0x886 : 0x83;
                Check((int)reader.GetMethodDefinition(h).Attributes == expectedAttributes, "installed-attributes-" + name);
                Check(raw(reader.GetBlobReader(reader.GetMethodDefinition(h).Signature), name == "InternalGetGenitiveMonthNames"), "actual-raw-" + name);
                var decoded = Model("CoreIntrinsics").GetMethod("IsDateTimeFormatInfoMonthArrayDecodedSignature", statics)!;
                Check((bool)decoded.Invoke(null, new[] { Get(candidate, "Signature"), (object)(name == "InternalGetGenitiveMonthNames") })!, "actual-decoded-" + name);
                Console.WriteLine("dtfi-name-arrays-installed:" + name + ":" + MetadataTokens.GetToken(h).ToString("X8", CultureInfo.InvariantCulture));
            }
        }
        Console.WriteLine("dtfi-name-arrays-actual:" + MetadataTokens.GetToken(targetHandle).ToString("X8", CultureInfo.InvariantCulture));
        var appModule = ((IEnumerable)Get(loaded, "Modules")).Cast<object>().Single(m => (string)Get(m, "AssemblyName") == "DtfINameArraysGuardInput");
        var appReader = (MetadataReader)Get(appModule, "Reader");
        var context = Model("GenericContext").GetField("Empty", statics)!.GetValue(null)!;
        int imports = 0;
        foreach (var h in appReader.MemberReferences)
        {
            string name = appReader.GetString(appReader.GetMemberReference(h).Name);
            if (!all.Contains(name)) continue;
            var resolved = Model("Compilation").GetMethod("ResolveMemberRefMethod", instance)!.Invoke(loaded, new[] { appModule, (object)h, context })!;
            var expected = reader.GetTypeDefinition(ownerHandle).GetMethods().Single(x => reader.GetString(reader.GetMethodDefinition(x).Name) == name);
            Check(ReferenceEquals(Get(resolved, "Module"), module) && (MethodDefinitionHandle)Get(resolved, "Handle") == expected, "resolved-memberref-" + name);
            Check((bool)predicate.Invoke(null, new[] { resolved })! == accepted.Contains(name), "exact-neighbor-policy-" + name);
            imports++;
        }
        Check(imports == 7, "all-memberrefs");
        void Reject(object value, string field, object next)
        {
            object saved = Get(value, field);
            Set(value, field, next);
            try { Check(!(bool)predicate.Invoke(null, new[] { actual })!, "wrong-" + field); }
            finally { Set(value, field, saved); }
        }
        Reject(module, "AssemblyName", "Application.CoreLib");
        Reject(ownerClass, "Module", appModule);
        Reject(ownerClass, "_fullName", "Application.DateTimeFormatInfo");
        Reject(ownerClass, "Handle", default(TypeDefinitionHandle));
        Reject(ownerClass, "IsValueType", true);
        Reject(ownerClass, "IsInterface", true);
        Reject(ownerClass, "GenericArity", 1);
        Reject(actual, "Handle", default(MethodDefinitionHandle));
        Reject(actual, "Rva", 0);
        Reject(actual, "Name", "get_Calendar");
        var attrs = (MethodAttributes)Get(actual, "Attributes");
        foreach (var flags in new[] { attrs | MethodAttributes.Static, attrs | MethodAttributes.Virtual,
            attrs | MethodAttributes.PinvokeImpl, MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName, attrs | MethodAttributes.SpecialName, MethodAttributes.Private | MethodAttributes.HideBySig })
            Reject(actual, "Attributes", flags);
        foreach (var impl in new[] { MethodImplAttributes.InternalCall, MethodImplAttributes.NoInlining,
            MethodImplAttributes.Synchronized, MethodImplAttributes.PreserveSig })
            Reject(actual, "ImplAttributes", impl);
        var result = Get(Get(actual, "Signature"), "ReturnType");
        Reject(result, "Kind", Enum.Parse(Model("TypeKind"), "Class"));
        Reject(result, "Kind", Enum.Parse(Model("TypeKind"), "ByRef"));
        Reject(Get(result, "Element"), "Primitive", PrimitiveTypeCode.Object);
        Reject(Get(result, "Element"), "Kind", Enum.Parse(Model("TypeKind"), "Class"));
        var parameters = (IEnumerable)Get(Get(actual, "Signature"), "ParameterTypes");
        var boolean = parameters.Cast<object>().Single();
        Reject(boolean, "Primitive", PrimitiveTypeCode.Int32);
        Reject(boolean, "Kind", Enum.Parse(Model("TypeKind"), "Class"));
        var vectors = new (string Hex, bool HasBoolean, bool Expected)[]
        {
            ("20011D0E02", true, true), ("20001D0E", false, true),
            ("20011D0E02", false, false), ("20001D0E", true, false),
            ("20011D1C02", true, false), ("2001101D0E02", true, false),
            ("20011D0E08", true, false), ("20011D0E1205", true, false),
            ("20011D120502", true, false), ("20011D110502", true, false),
            ("20011D20050E02", true, false), ("00011D0E02", true, false),
            ("3001011D0E02", true, false), ("20021D0E0202", true, false),
            ("20011D0E0200", true, false), ("20001D1C", false, false),
            ("2000101D0E", false, false), ("20001D1205", false, false),
            ("00001D0E", false, false), ("20001D0E00", false, false),
            ("20011D0E", true, false), ("", false, false)
        };
        foreach (var vector in vectors)
        {
            var metadata = new MetadataBuilder();
            metadata.AddModule(0, metadata.GetOrAddString("NameArraysRaw"), metadata.GetOrAddGuid(Guid.Empty), default, default);
            var blob = metadata.GetOrAddBlob(Convert.FromHexString(vector.Hex));
            var image = new BlobBuilder();
            new MetadataRootBuilder(metadata).Serialize(image, 0, 0);
            using var source = MetadataReaderProvider.FromMetadataImage(image.ToImmutableArray());
            Check(raw(source.GetMetadataReader().GetBlobReader(blob), vector.HasBoolean) == vector.Expected, "raw-" + vector.Hex);
        }
        Check((bool)predicate.Invoke(null, new[] { actual })!, "restored-actual");
        Console.WriteLine("dtfi-name-arrays-guards:" + checks);
        return true;
    }
}
