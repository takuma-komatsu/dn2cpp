using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

internal static class ResourceTypeGuardGuards
{
    private delegate bool RawPredicate(BlobReader blob);

    public static bool Run(string[] args)
    {
        if ((args.Length != 4 && args.Length != 5) || args[0] != "--resource-type-guard-guards") return false;
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
        string[] accepted = { "IsDefaultType" };
        string[] all = accepted;
        string input = Path.GetFullPath(args[3]);
        if (File.Exists(input)) throw new InvalidOperationException("model app must be new");
        using (var empty = Mono.Cecil.AssemblyDefinition.CreateAssembly(new Mono.Cecil.AssemblyNameDefinition("ResourceTypeGuardGuardInput", new Version(1, 0)), "ResourceTypeGuardGuardInput", Mono.Cecil.ModuleKind.Dll))
        using (var core = Mono.Cecil.AssemblyDefinition.ReadAssembly(args[2]))
        {
            var owner = core.MainModule.GetType("System.Resources.ResourceManager");
            var host = new Mono.Cecil.TypeDefinition("Guard", "References", Mono.Cecil.TypeAttributes.Public, empty.MainModule.TypeSystem.Object);
            empty.MainModule.Types.Add(host);
            foreach (string name in all)
            {
                var target = empty.MainModule.ImportReference(owner.Methods.Single(m => m.Name == name));
                var stub = new Mono.Cecil.MethodDefinition(name, Mono.Cecil.MethodAttributes.Public | Mono.Cecil.MethodAttributes.Static, empty.MainModule.TypeSystem.Void);
                host.Methods.Add(stub);
                var il = stub.Body.GetILProcessor();
                il.Emit(Mono.Cecil.Cil.OpCodes.Ldnull);
                if (target.Parameters.Count != 0) il.Emit(Mono.Cecil.Cil.OpCodes.Ldnull);
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
            return reader.GetString(type.Name) == "ResourceManager" && reader.GetString(type.Namespace) == "System.Resources" && type.GetDeclaringType().IsNil;
        });
        var targetHandle = reader.GetTypeDefinition(ownerHandle).GetMethods().Single(h => reader.GetString(reader.GetMethodDefinition(h).Name) == "IsDefaultType");
        var actual = ((IDictionary)Get(module, "MethodMap"))[targetHandle]!;
        var ownerClass = Get(actual, "DeclaringClass");
        var predicate = Model("CoreIntrinsics").GetMethod("IsResourceManagerDefaultType", statics)!;
        var raw = Model("CoreIntrinsics").GetMethod("IsResourceManagerDefaultTypeSignature", statics)!.CreateDelegate<RawPredicate>();
        int checks = 0;
        void Check(bool result, string label)
        {
            if (!result) throw new InvalidOperationException("resource type guard: " + label);
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
                Check(raw(reader.GetBlobReader(reader.GetMethodDefinition(h).Signature)), "actual-raw-" + name);
                var decoded = Model("CoreIntrinsics").GetMethod("IsResourceManagerDefaultTypeDecodedSignature", statics)!;
                Check((bool)decoded.Invoke(null, new[] { Get(candidate, "Signature") })!, "actual-decoded-" + name);
                Console.WriteLine("resource-type-guard-installed:" + name + ":" + MetadataTokens.GetToken(h).ToString("X8", CultureInfo.InvariantCulture));
            }
        }
        Console.WriteLine("resource-type-guard-actual:" + MetadataTokens.GetToken(targetHandle).ToString("X8", CultureInfo.InvariantCulture));
        var appModule = ((IEnumerable)Get(loaded, "Modules")).Cast<object>().Single(m => (string)Get(m, "AssemblyName") == "ResourceTypeGuardGuardInput");
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
        Check(imports == 1, "all-memberrefs");
        void Reject(object value, string field, object next)
        {
            object saved = Get(value, field);
            Set(value, field, next);
            try { Check(!(bool)predicate.Invoke(null, new[] { actual })!, "wrong-" + field); }
            finally { Set(value, field, saved); }
        }
        Reject(module, "AssemblyName", "Application.CoreLib");
        Reject(ownerClass, "Module", appModule);
        Reject(ownerClass, "_fullName", "Application.ResourceManager");
        Reject(ownerClass, "Handle", default(TypeDefinitionHandle));
        Reject(ownerClass, "IsValueType", true);
        Reject(ownerClass, "IsEnum", true);
        Reject(ownerClass, "IsInterface", true);
        Reject(ownerClass, "GenericArity", 1);
        Reject(actual, "Handle", default(MethodDefinitionHandle));
        Reject(actual, "Rva", 0);
        Reject(actual, "Name", "get_Calendar");
        var attrs = (MethodAttributes)Get(actual, "Attributes");
        foreach (var flags in new[] { attrs & ~MethodAttributes.Static, attrs | MethodAttributes.Virtual,
            attrs | MethodAttributes.PinvokeImpl, MethodAttributes.Private | MethodAttributes.Static | MethodAttributes.HideBySig, MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName, attrs | MethodAttributes.SpecialName, MethodAttributes.Private | MethodAttributes.HideBySig })
            Reject(actual, "Attributes", flags);
        foreach (var impl in new[] { MethodImplAttributes.InternalCall, MethodImplAttributes.NoInlining,
            MethodImplAttributes.Synchronized, MethodImplAttributes.PreserveSig })
            Reject(actual, "ImplAttributes", impl);
        var result = Get(Get(actual, "Signature"), "ReturnType");
        Reject(result, "Kind", Enum.Parse(Model("TypeKind"), "Class"));
        Reject(result, "Kind", Enum.Parse(Model("TypeKind"), "ByRef"));
        Reject(result, "Primitive", PrimitiveTypeCode.Int32);
        Reject(result, "Primitive", PrimitiveTypeCode.Byte);
        foreach (var parameter in ((IEnumerable)Get(Get(actual, "Signature"), "ParameterTypes")).Cast<object>())
        {
            Reject(parameter, "Kind", Enum.Parse(Model("TypeKind"), "ByRef"));
            Reject(parameter, "Primitive", PrimitiveTypeCode.Object);
        }
        var vectors = new[] { "0002020E0E", "", "00", "0002020E", "0002020E0E00", "2002020E0E",
            "4002020E0E", "0502020E0E", "100002020E0E", "0002080E0E", "000202100E0E",
            "0002020E100E", "0002021C0E", "0002020E1C", "00020220040E0E", "0002020E1F040E",
            "0001020E", "0003020E0E0E", "0002020E41", "008002020E0E" };
        foreach (string vector in vectors)
        {
            var metadata = new MetadataBuilder();
            metadata.AddModule(0, metadata.GetOrAddString("ResourceGuardRaw"), metadata.GetOrAddGuid(Guid.Empty), default, default);
            var blob = metadata.GetOrAddBlob(Convert.FromHexString(vector));
            var image = new BlobBuilder();
            new MetadataRootBuilder(metadata).Serialize(image, 0, 0);
            using var source = MetadataReaderProvider.FromMetadataImage(image.ToImmutableArray());
            Check(raw(source.GetMetadataReader().GetBlobReader(blob)) == (vector == "0002020E0E"), "raw-" + vector);
        }
        Check((bool)predicate.Invoke(null, new[] { actual })!, "restored-actual");
        if (args.Length == 5)
        {
            var diet = Assembly.LoadFrom(Path.GetFullPath(args[4]));
            var copied = diet.GetType("Dn2Cpp.AssemblyDiet", true)!.GetMethod("IsCopiedResourceManagerDefaultType", statics)!;
            using var core = Mono.Cecil.AssemblyDefinition.ReadAssembly(args[2]);
            var owner = core.MainModule.GetType("System.Resources.ResourceManager");
            bool Accepted(Mono.Cecil.MethodDefinition method) => (bool)copied.Invoke(null, new object[] { method, owner.FullName })!;
            foreach (var method in owner.Methods)
                Check(Accepted(method) == accepted.Contains(method.Name), "cecil-family-" + method.Name);
            foreach (var method in owner.Methods.Where(m => accepted.Contains(m.Name)))
            {
                var flags = method.Attributes;
                method.Attributes &= ~Mono.Cecil.MethodAttributes.Static;
                Check(!Accepted(method), "cecil-instance-" + method.Name); method.Attributes = flags;
                var impl = method.ImplAttributes;
                method.ImplAttributes = Mono.Cecil.MethodImplAttributes.InternalCall;
                Check(!Accepted(method), "cecil-impl-" + method.Name); method.ImplAttributes = impl;
                method.HasThis = true;
                Check(!Accepted(method), "cecil-hasthis-" + method.Name); method.HasThis = false;
                var convention = method.CallingConvention;
                method.CallingConvention = Mono.Cecil.MethodCallingConvention.VarArg;
                Check(!Accepted(method), "cecil-vararg-" + method.Name); method.CallingConvention = convention;
                var returnType = method.ReturnType;
                method.ReturnType = new Mono.Cecil.OptionalModifierType(core.MainModule.TypeSystem.Object, returnType);
                Check(!Accepted(method), "cecil-return-modifier-" + method.Name); method.ReturnType = returnType;
                var parameter = method.Parameters[0].ParameterType;
                method.Parameters[0].ParameterType = new Mono.Cecil.ByReferenceType(parameter);
                Check(!Accepted(method), "cecil-byref-" + method.Name);
                method.Parameters[0].ParameterType = new Mono.Cecil.RequiredModifierType(core.MainModule.TypeSystem.Object, parameter);
                Check(!Accepted(method), "cecil-param-modifier-" + method.Name);
                method.Parameters[0].ParameterType = parameter;
                Check(Accepted(method), "cecil-restored-" + method.Name);
            }
        }
        Console.WriteLine("resource-type-guard-guards:" + checks);
        return true;
    }
}
