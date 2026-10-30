using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;

internal static class DateTimeTCharGuards
{
    private delegate bool RawPredicate(BlobReader blob);

    public static bool Run(string[] args)
    {
        if (args.Length != 4 || args[0] != "--datetime-tchar-guards") return false;
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
        string[] names = { "AMDesignatorTChar", "PMDesignatorTChar", "DateSeparatorTChar", "TimeSeparatorTChar", "DecimalSeparatorTChar" };
        using (var app = Mono.Cecil.AssemblyDefinition.CreateAssembly(new Mono.Cecil.AssemblyNameDefinition("DateTimeTCharGuardInput", new Version(1, 0)), "DateTimeTCharGuardInput", Mono.Cecil.ModuleKind.Dll))
        using (var core = Mono.Cecil.AssemblyDefinition.ReadAssembly(args[2]))
        {
            var owner = core.MainModule.GetType("System.Globalization.DateTimeFormatInfo");
            var host = new Mono.Cecil.TypeDefinition("Guard", "References", Mono.Cecil.TypeAttributes.Public, app.MainModule.TypeSystem.Object);
            app.MainModule.Types.Add(host);
            foreach (string name in names)
            {
                var target = app.MainModule.ImportReference(owner.Methods.Single(method => method.Name == name));
                foreach (bool bytes in new[] { false, true })
                {
                    var spec = new Mono.Cecil.GenericInstanceMethod(target);
                    spec.GenericArguments.Add(bytes ? app.MainModule.TypeSystem.Byte : app.MainModule.TypeSystem.Char);
                    var stub = new Mono.Cecil.MethodDefinition(name + (bytes ? "Byte" : "Char"), Mono.Cecil.MethodAttributes.Public | Mono.Cecil.MethodAttributes.Static, app.MainModule.TypeSystem.Void);
                    host.Methods.Add(stub);
                    var il = stub.Body.GetILProcessor();
                    // Metadata-only fixture. Both actual call and address operands resolve the same specification.
                    il.Emit(Mono.Cecil.Cil.OpCodes.Ldnull);
                    il.Emit(Mono.Cecil.Cil.OpCodes.Call, spec);
                    il.Emit(Mono.Cecil.Cil.OpCodes.Pop);
                    il.Emit(Mono.Cecil.Cil.OpCodes.Ldftn, spec);
                    il.Emit(Mono.Cecil.Cil.OpCodes.Pop);
                    il.Emit(Mono.Cecil.Cil.OpCodes.Ret);
                }
            }
            app.Write(input, new Mono.Cecil.WriterParameters { Timestamp = 0, DeterministicMvid = true });
        }
        var options = Activator.CreateInstance(Model("TranspileOptions"))!;
        Set(options, "Input", input); Set(options, "UseILDiet", false); Set(options, "SharedGenerics", false);
        Set(options, "AutoRef", false); Set(options, "IsaSurfaceDump", "predicate-guard-only");
        object loaded;
        try { loaded = Model("Compilation").GetMethod("Create", statics)!.Invoke(null, new object[] { new[] { input, Path.GetFullPath(args[2]) }, options })!; }
        catch (TargetInvocationException error) when (error.InnerException is not null) { throw error.InnerException; }
        var modules = ((IEnumerable)Get(loaded, "Modules")).Cast<object>().ToArray();
        var module = modules.Single(m => (string)Get(m, "AssemblyName") == "System.Private.CoreLib");
        var appModule = modules.Single(m => (string)Get(m, "AssemblyName") == "DateTimeTCharGuardInput");
        var reader = (MetadataReader)Get(module, "Reader");
        var appReader = (MetadataReader)Get(appModule, "Reader");
        var ownerHandle = reader.TypeDefinitions.Single(h =>
        {
            var definition = reader.GetTypeDefinition(h);
            return reader.GetString(definition.Namespace) == "System.Globalization" && reader.GetString(definition.Name) == "DateTimeFormatInfo" && definition.GetDeclaringType().IsNil;
        });
        var predicate = Model("CoreIntrinsics").GetMethod("IsDateTimeFormatInfoSpan", statics)!;
        var decoded = Model("CoreIntrinsics").GetMethod("IsDateTimeFormatInfoSpanDecodedSignature", statics)!;
        var raw = Model("CoreIntrinsics").GetMethod("IsDateTimeFormatInfoSpanSignature", statics)!.CreateDelegate<RawPredicate>();
        var context = Model("GenericContext").GetField("Empty", statics)!.GetValue(null)!;
        int checks = 0;
        void Check(bool result, string label)
        {
            if (!result) throw new InvalidOperationException("DateTime TChar guard: " + label);
            checks++;
        }
        bool Accepted(object method) => (bool)predicate.Invoke(null, new[] { method })!;
        var actuals = new List<object>();
        foreach (string name in names)
        {
            var handle = reader.GetTypeDefinition(ownerHandle).GetMethods().Single(h => reader.GetString(reader.GetMethodDefinition(h).Name) == name);
            // Generic definitions are absent from MethodMap. Use the compiler's own
            // template instantiator with its interned symbolic method variable.
            var declaringClass = ((IDictionary)Get(module, "ClassMap"))[ownerHandle]!;
            var typeArguments = Array.CreateInstance(Model("TypeDesc"), 1);
            typeArguments.SetValue(Model("TypeDesc").GetMethod("MakeGenericVar", statics)!.Invoke(null, new object[] { 0, true }), 0);
            var actual = Model("Compilation").GetMethod("InstantiateMethodOnClass", instance)!.Invoke(loaded,
                new object[] { declaringClass, module, handle, typeArguments })!;
            actuals.Add(actual);
            Check(Accepted(actual), "actual-open-" + name);
            Check(raw(reader.GetBlobReader(reader.GetMethodDefinition(handle).Signature)), "actual-raw-" + name);
            Check((bool)decoded.Invoke(null, new[] { Get(actual, "Signature"), module })!, "actual-decoded-" + name);
            Console.WriteLine("datetime-tchar-actual:" + name + ":" + MetadataTokens.GetToken(handle).ToString("X8", CultureInfo.InvariantCulture));
        }
        int specs = 0;
        foreach (var row in Enumerable.Range(1, appReader.GetTableRowCount(TableIndex.MethodSpec)))
        {
            var handle = MetadataTokens.MethodSpecificationHandle(row);
            var specification = appReader.GetMethodSpecification(handle);
            Check(specification.Method.Kind == HandleKind.MemberReference, "actual-MethodSpec-MemberRef");
            var reference = appReader.GetMemberReference((MemberReferenceHandle)specification.Method);
            string name = appReader.GetString(reference.Name);
            Check(names.Contains(name), "exact-MethodSpec-name");
            Check(raw(appReader.GetBlobReader(reference.Signature)), "actual-MemberRef-raw-" + name);
            var resolved = Model("Compilation").GetMethod("ResolveMethodSpec", instance)!.Invoke(loaded, new[] { appModule, (object)handle, context })!;
            Check(Accepted(resolved), "closed-exact-" + name);
            Check(ReferenceEquals(Get(resolved, "Module"), module), "closed-authority-" + name);
            var span = Get(Get(Get(resolved, "Signature"), "ReturnType"), "Class");
            var argument = ((IEnumerable)Get(Get(span, "Context"), "TypeArgs")).Cast<object>().Single();
            Check((PrimitiveTypeCode)Get(argument, "Primitive") is PrimitiveTypeCode.Char or PrimitiveTypeCode.Byte, "closed-width-" + name);
            specs++;
        }
        Check(specs == 10, "five-names-both-widths");
        using (var emitted = Mono.Cecil.AssemblyDefinition.ReadAssembly(input))
        {
            var bodies = emitted.MainModule.GetType("Guard.References").Methods.SelectMany(method => method.Body.Instructions).ToArray();
            Check(bodies.Count(i => i.OpCode == Mono.Cecil.Cil.OpCodes.Call && i.Operand is Mono.Cecil.GenericInstanceMethod) == 10, "direct-spec-operands");
            Check(bodies.Count(i => i.OpCode == Mono.Cecil.Cil.OpCodes.Ldftn && i.Operand is Mono.Cecil.GenericInstanceMethod) == 10, "address-spec-operands");
        }
        object selected = actuals[2];
        var ownerClass = Get(selected, "DeclaringClass");
        var returned = Get(Get(selected, "Signature"), "ReturnType");
        var returnClass = Get(returned, "Class");
        void Reject(object value, string field, object next)
        {
            object saved = Get(value, field);
            Set(value, field, next);
            try { Check(!Accepted(selected), "wrong-" + value.GetType().Name + "-" + field); }
            finally { Set(value, field, saved); }
        }
        Reject(module, "AssemblyName", "Application.CoreLib");
        Reject(ownerClass, "Module", appModule);
        Reject(ownerClass, "_fullName", "Application.DateTimeFormatInfo");
        Reject(ownerClass, "Handle", default(TypeDefinitionHandle));
        Reject(ownerClass, "IsValueType", true);
        Reject(ownerClass, "IsInterface", true);
        Reject(ownerClass, "GenericArity", 1);
        Reject(selected, "Handle", default(MethodDefinitionHandle));
        Reject(selected, "Name", "get_DateSeparator");
        Reject(selected, "Rva", 0);
        var attrs = (MethodAttributes)Get(selected, "Attributes");
        foreach (var flags in new[] { attrs | MethodAttributes.Static, attrs | MethodAttributes.Virtual, attrs | MethodAttributes.PinvokeImpl, MethodAttributes.Public | MethodAttributes.HideBySig })
            Reject(selected, "Attributes", flags);
        foreach (var impl in new[] { MethodImplAttributes.InternalCall, MethodImplAttributes.NoInlining, MethodImplAttributes.Synchronized })
            Reject(selected, "ImplAttributes", impl);
        Reject(returned, "Kind", Enum.Parse(Model("TypeKind"), "ByRef"));
        Reject(returnClass, "Module", appModule);
        Reject(returnClass, "Handle", default(TypeDefinitionHandle));
        Reject(returnClass, "Handle", ownerHandle);
        Reject(returnClass, "IsValueType", false);
        Reject(returnClass, "IsEnum", true);
        var variable = ((IEnumerable)Get(Get(returnClass, "Context"), "TypeArgs")).Cast<object>().Single();
        Reject(variable, "GenVarIsMethod", false);
        Reject(variable, "GenVarIndex", 1);
        var vectors = new (string Hex, bool Expected)[]
        {
            ("300100151105011E00", true), ("300100151109011E00", true), ("300100151108011E00", true),
            ("30010015118081011E00", true), ("200100151105011E00", false), ("100100151105011E00", false),
            ("300000151105011E00", false), ("300200151105011E00", false), ("300101151105011E0008", false),
            ("300100151205011E00", false), ("30010010151105011E00", false), ("3001002005151105011E00", false),
            ("300100151100011E00", false), ("300100151101011E00", false), ("300100151103011E00", false),
            ("300100151107011E00", false), ("300100151105001E00", false), ("300100151105021E00", false),
            ("300100151105011300", false), ("300100151105011E01", false), ("3001001511050103", false),
            ("300100151105011E0000", false), ("300100151105011E", false), ("", false)
        };
        foreach (var vector in vectors)
        {
            var metadata = new MetadataBuilder();
            metadata.AddModule(0, metadata.GetOrAddString("TCharRaw"), metadata.GetOrAddGuid(Guid.Empty), default, default);
            var blob = metadata.GetOrAddBlob(Convert.FromHexString(vector.Hex));
            var image = new BlobBuilder();
            new MetadataRootBuilder(metadata).Serialize(image, 0, 0);
            using var source = MetadataReaderProvider.FromMetadataImage(image.ToImmutableArray());
            Check(raw(source.GetMetadataReader().GetBlobReader(blob)) == vector.Expected, "raw-" + vector.Hex);
        }
        foreach (var actual in actuals) Check(Accepted(actual), "restored-actual");
        Console.WriteLine("datetime-tchar-guards:" + checks);
        return true;
    }
}
