using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

internal static class ConcreteVectorElementsGuards
{
    private delegate bool RawPredicate(string name, BlobReader blob, int token);

    public static bool Run(string[] args)
    {
        if (args.Length != 4 || args[0] != "--concrete-vector-elements-guards") return false;
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
        bool Named(string name) => name is "GetElement" or "WithElement";
        string input = Path.GetFullPath(args[3]);
        if (File.Exists(input)) throw new InvalidOperationException("model app must be new");
        using (var app = Mono.Cecil.AssemblyDefinition.CreateAssembly(new Mono.Cecil.AssemblyNameDefinition("ConcreteVectorElementsGuardInput", new Version(1, 0)), "ConcreteVectorElementsGuardInput", Mono.Cecil.ModuleKind.Dll))
        using (var core = Mono.Cecil.AssemblyDefinition.ReadAssembly(args[2]))
        {
            var host = new Mono.Cecil.TypeDefinition("Guard", "References", Mono.Cecil.TypeAttributes.Public, app.MainModule.TypeSystem.Object);
            app.MainModule.Types.Add(host);
            int serial = 0;
            foreach (var method in core.MainModule.GetType("System.Numerics.Vector").Methods.Where(m => Named(m.Name) && !m.HasGenericParameters))
            {
                var target = app.MainModule.ImportReference(method);
                var stub = new Mono.Cecil.MethodDefinition("Reference" + serial++, Mono.Cecil.MethodAttributes.Public | Mono.Cecil.MethodAttributes.Static, app.MainModule.TypeSystem.Void);
                host.Methods.Add(stub);
                var il = stub.Body.GetILProcessor();
                il.Emit(Mono.Cecil.Cil.OpCodes.Ldftn, target);
                il.Emit(Mono.Cecil.Cil.OpCodes.Pop);
                il.Emit(Mono.Cecil.Cil.OpCodes.Ret);
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
        var appModule = modules.Single(m => (string)Get(m, "AssemblyName") == "ConcreteVectorElementsGuardInput");
        var reader = (MetadataReader)Get(module, "Reader");
        var ownerHandle = reader.TypeDefinitions.Single(h =>
        {
            var type = reader.GetTypeDefinition(h);
            return reader.GetString(type.Name) == "Vector" && reader.GetString(type.Namespace) == "System.Numerics" && type.GetDeclaringType().IsNil;
        });
        var predicate = Model("CoreIntrinsics").GetMethod("IsConcreteVectorElement", statics)!;
        var raw = Model("CoreIntrinsics").GetMethod("IsConcreteVectorElementRawSignature", statics)!.CreateDelegate<RawPredicate>();
        int checks = 0;
        void Check(bool result, string label)
        {
            if (!result) throw new InvalidOperationException("concrete vector elements guard: " + label);
            checks++;
        }
        bool Accepted(object method) => (bool)predicate.Invoke(null, new[] { method })!;
        var appReader = (MetadataReader)Get(appModule, "Reader");
        var context = Model("GenericContext").GetField("Empty", statics)!.GetValue(null)!;
        var resolve = Model("Compilation").GetMethod("ResolveMemberRefMethod", instance)!;
        var imported = new Dictionary<MethodDefinitionHandle, object>();
        foreach (var h in appReader.MemberReferences)
        {
            if (!Named(appReader.GetString(appReader.GetMemberReference(h).Name))) continue;
            var method = resolve.Invoke(loaded, new[] { appModule, (object)h, context })!;
            imported.Add((MethodDefinitionHandle)Get(method, "Handle"), method);
        }
        Check(imported.Count == 6, "resolved-concrete-declarations");
        var ownerClass = Get(imported.Values.First(), "DeclaringClass");
        object? actual = null;
        int concrete = 0, generic = 0;
        foreach (var h in reader.GetTypeDefinition(ownerHandle).GetMethods())
        {
            var definition = reader.GetMethodDefinition(h);
            string name = reader.GetString(definition.Name);
            if (!Named(name)) continue;
            bool expected = definition.GetGenericParameters().Count == 0;
            object candidate;
            if (expected) candidate = imported[h];
            else
            {
                // Intrinsic facade declarations need not populate Module.MethodMap.
                candidate = Activator.CreateInstance(Model("MethodInfo"))!;
                Set(candidate, "DeclaringClass", ownerClass); Set(candidate, "Module", module);
                Set(candidate, "Name", name); Set(candidate, "Handle", h);
                Set(candidate, "Context", Get(ownerClass, "Context"));
                Set(candidate, "Attributes", definition.Attributes); Set(candidate, "ImplAttributes", definition.ImplAttributes);
                Set(candidate, "Rva", definition.RelativeVirtualAddress);
            }
            if (expected)
            {
                foreach (var parameter in ((IEnumerable)Get(Get(candidate, "Signature"), "ParameterTypes")).Cast<object>())
                    if (Get(parameter, "Class") is { } vector) Model("Compilation").GetMethod("EnsureShape", instance)!.Invoke(loaded, new[] { vector });
            }
            Check(Accepted(candidate) == expected, "methoddef-" + MetadataTokens.GetToken(h).ToString("X8", CultureInfo.InvariantCulture));
            Check(raw(name, reader.GetBlobReader(definition.Signature), 0) == expected, "raw-declaration-" + name);
            if (expected) { concrete++; actual ??= candidate; }
            else generic++;
        }
        Check(concrete == 6 && generic == 2, "finite-six-and-generic-neighbors");
        var current = actual!;
        var owner = Get(current, "DeclaringClass");
        void Reject(object value, string field, object next)
        {
            Check(Accepted(current), "before-" + field);
            object saved = Get(value, field);
            Set(value, field, next);
            try { Check(!Accepted(current), "reject-" + field); }
            finally { Set(value, field, saved); }
            Check(Accepted(current), "restored-" + field);
        }
        Reject(module, "AssemblyName", "Application.CoreLib");
        Reject(owner, "Module", appModule);
        Reject(owner, "_fullName", "Application.Vector");
        Reject(owner, "Handle", default(TypeDefinitionHandle));
        Reject(owner, "IsValueType", false);
        Reject(owner, "IsInterface", true);
        Reject(owner, "GenericArity", 1);
        Reject(owner, "IntrinsicCppName", "Dn2CppVector128");
        Reject(current, "Module", appModule);
        Reject(current, "Handle", default(MethodDefinitionHandle));
        Reject(current, "Rva", 0);
        Reject(current, "Name", "ToScalar");
        var attrs = (MethodAttributes)Get(current, "Attributes");
        foreach (var flags in new[] { attrs & ~MethodAttributes.Static, attrs | MethodAttributes.Virtual, attrs | MethodAttributes.PinvokeImpl, attrs | MethodAttributes.SpecialName, MethodAttributes.Private | MethodAttributes.Static | MethodAttributes.HideBySig })
            Reject(current, "Attributes", flags);
        foreach (var impl in new[] { MethodImplAttributes.IL, MethodImplAttributes.InternalCall, MethodImplAttributes.NoInlining, MethodImplAttributes.Synchronized })
            Reject(current, "ImplAttributes", impl);
        var parameters = ((IEnumerable)Get(Get(current, "Signature"), "ParameterTypes")).Cast<object>().ToArray();
        var vectorType = parameters[0];
        var vectorClass = Get(vectorType, "Class");
        Reject(vectorType, "Kind", Enum.Parse(Model("TypeKind"), "ByRef"));
        Reject(vectorClass, "Module", appModule);
        Reject(vectorClass, "_fullName", "Application.Vector2");
        Reject(vectorClass, "IsValueType", false);
        Reject(vectorClass, "IsEnum", true);
        Reject(vectorClass, "IsByRefLike", true);
        Reject(vectorClass, "GenericArity", 1);
        Reject(vectorClass, "IntrinsicCppName", "Dn2CppVector128");
        Reject(parameters[1], "Primitive", PrimitiveTypeCode.UInt32);
        Reject(parameters[1], "Kind", Enum.Parse(Model("TypeKind"), "ByRef"));
        int imports = 0;
        foreach (var h in appReader.MemberReferences)
        {
            if (!Named(appReader.GetString(appReader.GetMemberReference(h).Name))) continue;
            object Resolve() => resolve.Invoke(loaded, new[] { appModule, (object)h, context })!;
            var first = Resolve();
            Check(Accepted(first), "reference-first");
            var saved = Get(module, "AssemblyName");
            Set(module, "AssemblyName", "Application.CoreLib");
            try
            {
                bool rejected = false;
                try { Resolve(); }
                catch (TargetInvocationException error) when (error.InnerException is NotSupportedException) { rejected = true; }
                Check(rejected, "reference-cache-bad");
            }
            finally { Set(module, "AssemblyName", saved); }
            Check(ReferenceEquals(first, Resolve()), "reference-cache-restored");
            imports++;
        }
        Check(imports == 6, "all-reference-overloads");
        var vectors = new (string Name, string Hex, int Token, bool Expected)[]
        {
            ("GetElement", "00020c110408", 4, true), ("WithElement", "000311041104080c", 4, true),
            ("GetElement", "00020c110508", 0, true), ("GetElement", "00020c110508", 4, false),
            ("GetElement", "20020c110408", 0, false), ("GetElement", "05020c110408", 0, false),
            ("GetElement", "1001020c110408", 0, false), ("GetElement", "00020d110408", 0, false),
            ("GetElement", "00020c10110408", 0, false), ("GetElement", "00020c20110408", 0, false),
            ("GetElement", "00020c120408", 0, false), ("GetElement", "00020c110008", 0, false),
            ("GetElement", "00020c110608", 0, false), ("GetElement", "00020c11040800", 0, false),
            ("GetElement", "00020c110409", 0, false), ("GetElement", "00020c11", 0, false),
            ("GetElement", "", 0, false), ("WithElement", "000311081104080c", 0, false),
            ("WithElement", "000311041104080d", 0, false), ("WithElement", "000311041104080c00", 0, false),
            ("WithElement", "00031011041104080c", 0, false), ("WithElement", "00031104110410080c", 0, false),
            ("Other", "00020c110408", 0, false), ("GetElement", "00020c110408", 4, true),
        };
        foreach (var vector in vectors)
        {
            var metadata = new MetadataBuilder();
            metadata.AddModule(0, metadata.GetOrAddString("ConcreteVectorRaw"), metadata.GetOrAddGuid(Guid.Empty), default, default);
            var blob = metadata.GetOrAddBlob(Convert.FromHexString(vector.Hex));
            var image = new BlobBuilder();
            new MetadataRootBuilder(metadata).Serialize(image, 0, 0);
            using var source = MetadataReaderProvider.FromMetadataImage(image.ToImmutableArray());
            Check(raw(vector.Name, source.GetMetadataReader().GetBlobReader(blob), vector.Token) == vector.Expected, "raw-" + vector.Name + "-" + vector.Hex);
        }
        Console.WriteLine("concrete-vector-elements-guards:" + checks);
        return true;
    }
}
