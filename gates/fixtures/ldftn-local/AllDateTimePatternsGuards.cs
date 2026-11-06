using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;

internal static class AllDateTimePatternsGuards
{
    private delegate bool RawPredicate(BlobReader blob);

    public static bool Run(string[] args)
    {
        if (args.Length != 4 || args[0] != "--all-datetime-patterns-guards") return false;
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
        using (var empty = Mono.Cecil.AssemblyDefinition.CreateAssembly(new Mono.Cecil.AssemblyNameDefinition("AllDateTimePatternsGuardInput", new Version(1, 0)), "AllDateTimePatternsGuardInput", Mono.Cecil.ModuleKind.Dll))
        using (var core = Mono.Cecil.AssemblyDefinition.ReadAssembly(args[2]))
        {
            var owner = core.MainModule.GetType("System.Globalization.DateTimeFormatInfo");
            var host = new Mono.Cecil.TypeDefinition("Guard", "References", Mono.Cecil.TypeAttributes.Public, empty.MainModule.TypeSystem.Object);
            empty.MainModule.Types.Add(host);
            foreach (var definition in owner.Methods.Where(m => m.Name == "GetAllDateTimePatterns" || m.Name == "get_DateSeparator"))
            {
                var target = empty.MainModule.ImportReference(definition);
                var stub = new Mono.Cecil.MethodDefinition(target.Name + target.Parameters.Count, Mono.Cecil.MethodAttributes.Public | Mono.Cecil.MethodAttributes.Static, empty.MainModule.TypeSystem.Void);
                host.Methods.Add(stub);
                var il = stub.Body.GetILProcessor();
                il.Emit(Mono.Cecil.Cil.OpCodes.Ldnull);
                if (target.Parameters.Count != 0) il.Emit(Mono.Cecil.Cil.OpCodes.Ldc_I4, (int)'s');
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
        var targetHandle = reader.GetTypeDefinition(ownerHandle).GetMethods().Single(h => reader.GetString(reader.GetMethodDefinition(h).Name) == "GetAllDateTimePatterns" && reader.GetBlobBytes(reader.GetMethodDefinition(h).Signature)[1] == 1);
        var actual = ((IDictionary)Get(module, "MethodMap"))[targetHandle]!;
        var ownerClass = Get(actual, "DeclaringClass");
        var predicate = Model("CoreIntrinsics").GetMethod("IsDateTimeFormatInfoAllDateTimePatterns", statics)!;
        var raw = Model("CoreIntrinsics").GetMethod("IsDateTimeFormatInfoAllDateTimePatternsSignature", statics)!.CreateDelegate<RawPredicate>();
        int checks = 0;
        void Check(bool result, string label)
        {
            if (!result) throw new InvalidOperationException("DTFI patterns guard: " + label);
            checks++;
        }
        Check((bool)predicate.Invoke(null, new[] { actual })!, "actual-CoreLib");
        foreach (var h in reader.GetTypeDefinition(ownerHandle).GetMethods().Where(h => (reader.GetString(reader.GetMethodDefinition(h).Name) == "GetAllDateTimePatterns" && h != targetHandle) || reader.GetString(reader.GetMethodDefinition(h).Name) == "get_DateSeparator"))
        {
            var candidate = ((IDictionary)Get(module, "MethodMap"))[h]!;
            string name = reader.GetString(reader.GetMethodDefinition(h).Name);
            Check((bool)predicate.Invoke(null, new[] { candidate })! == false, "actual-MethodDef-policy-" + name);
        }
        Console.WriteLine("all-datetime-patterns-actual:" + MetadataTokens.GetToken(targetHandle).ToString("X8", CultureInfo.InvariantCulture));
        var appModule = ((IEnumerable)Get(loaded, "Modules")).Cast<object>().Single(m => (string)Get(m, "AssemblyName") == "AllDateTimePatternsGuardInput");
        var appReader = (MetadataReader)Get(appModule, "Reader");
        var context = Model("GenericContext").GetField("Empty", statics)!.GetValue(null)!;
        int imports = 0;
        foreach (var h in appReader.MemberReferences)
        {
            string name = appReader.GetString(appReader.GetMemberReference(h).Name);
            if (name is not ("GetAllDateTimePatterns" or "get_DateSeparator")) continue;
            int count = appReader.GetBlobBytes(appReader.GetMemberReference(h).Signature)[1];
            var resolved = Model("Compilation").GetMethod("ResolveMemberRefMethod", instance)!.Invoke(loaded, new[] { appModule, (object)h, context })!;
            var expected = reader.GetTypeDefinition(ownerHandle).GetMethods().Single(x => reader.GetString(reader.GetMethodDefinition(x).Name) == name && reader.GetBlobBytes(reader.GetMethodDefinition(x).Signature)[1] == count);
            Check(ReferenceEquals(Get(resolved, "Module"), module) && (MethodDefinitionHandle)Get(resolved, "Handle") == expected, "resolved-memberref-" + name);
            Check((bool)predicate.Invoke(null, new[] { resolved })! == (name == "GetAllDateTimePatterns" && count == 1), "exact-neighbor-policy-" + name);
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
            attrs | MethodAttributes.PinvokeImpl, attrs | MethodAttributes.SpecialName, MethodAttributes.Private | MethodAttributes.HideBySig })
            Reject(actual, "Attributes", flags);
        foreach (var impl in new[] { MethodImplAttributes.InternalCall, MethodImplAttributes.NoInlining,
            MethodImplAttributes.Synchronized, MethodImplAttributes.PreserveSig })
            Reject(actual, "ImplAttributes", impl);
        var returned = Get(Get(actual, "Signature"), "ReturnType");
        Reject(returned, "Kind", Enum.Parse(Model("TypeKind"), "MDArray"));
        Reject(returned, "Kind", Enum.Parse(Model("TypeKind"), "ByRef"));
        var element = Get(returned, "Element");
        Reject(element, "Kind", Enum.Parse(Model("TypeKind"), "Class"));
        Reject(element, "Primitive", PrimitiveTypeCode.Object);
        var parameter = ((IEnumerable)Get(Get(actual, "Signature"), "ParameterTypes")).Cast<object>().Single();
        Reject(parameter, "Kind", Enum.Parse(Model("TypeKind"), "Class"));
        Reject(parameter, "Kind", Enum.Parse(Model("TypeKind"), "ByRef"));
        Reject(parameter, "Primitive", PrimitiveTypeCode.Int32);
        Reject(parameter, "Primitive", PrimitiveTypeCode.UInt16);
        var vectors = new (string Hex, bool Expected)[]
        {
            ("20011D0E03", true), ("00011D0E03", false), ("3001011D0E03", false),
            ("20001D0E", false), ("20021D0E0303", false), ("20011D0E0300", false),
            ("20011D0E08", false), ("20011D0E07", false), ("20011D0E1105", false),
            ("20011D0E1003", false), ("20011D0E200503", false), ("20011D120503", false),
            ("20011D1C03", false), ("2001101D0E03", false), ("2001140E02000003", false),
            ("20011C03", false), ("20011D0E", false), ("", false)
        };
        foreach (var vector in vectors)
        {
            var metadata = new MetadataBuilder();
            metadata.AddModule(0, metadata.GetOrAddString("CalendarRaw"), metadata.GetOrAddGuid(Guid.Empty), default, default);
            var blob = metadata.GetOrAddBlob(Convert.FromHexString(vector.Hex));
            var image = new BlobBuilder();
            new MetadataRootBuilder(metadata).Serialize(image, 0, 0);
            using var source = MetadataReaderProvider.FromMetadataImage(image.ToImmutableArray());
            Check(raw(source.GetMetadataReader().GetBlobReader(blob)) == vector.Expected, "raw-" + vector.Hex);
        }
        Check((bool)predicate.Invoke(null, new[] { actual })!, "restored-actual");
        Console.WriteLine("all-datetime-patterns-guards:" + checks);
        return true;
    }
}
