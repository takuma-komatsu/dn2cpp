using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

internal static class DateTimeTrioGuards
{
    private delegate bool RawPredicate(BlobReader blob);

    public static bool Run(string[] args)
    {
        if (args.Length != 4 || args[0] != "--datetime-trio-guards") return false;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        const BindingFlags instance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        const BindingFlags statics = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        var production = Assembly.LoadFrom(Path.GetFullPath(args[1]));
        Type Model(string name) => production.GetType("Dn2Cpp." + name, true)!;
        object Get(object value, string name) => value.GetType().GetField(name, instance) is { } field ? field.GetValue(value)! : value.GetType().GetProperty(name, instance)!.GetValue(value)!;
        void Set(object value, string name, object next)
        {
            if (value.GetType().GetField(name, instance) is { } field) field.SetValue(value, next);
            else value.GetType().GetProperty(name, instance)!.SetValue(value, next);
        }
        string input = Path.GetFullPath(args[3]);
        if (File.Exists(input)) throw new InvalidOperationException("guard input must be new");
        int[] tokens = { 0x06000fb6, 0x0600102b, 0x06000f94 };
        int[] publicCtorTokens;
        string[] names = { "TryAddTicks", "TryCreate", "LocalConstructor" };
        using (var app = Mono.Cecil.AssemblyDefinition.CreateAssembly(new Mono.Cecil.AssemblyNameDefinition("DateTimeTrioGuardInput", new Version(1, 0)), "DateTimeTrioGuardInput", Mono.Cecil.ModuleKind.Dll))
        using (var core = Mono.Cecil.AssemblyDefinition.ReadAssembly(args[2]))
        {
            var owner = core.MainModule.GetType("System.DateTime");
            var host = new Mono.Cecil.TypeDefinition("Guard", "References", Mono.Cecil.TypeAttributes.Public, app.MainModule.TypeSystem.Object);
            app.MainModule.Types.Add(host);
            publicCtorTokens = owner.Methods.Where(method => method.IsConstructor && method.IsPublic
                && ((method.Parameters.Count == 2 && method.Parameters[0].ParameterType.MetadataType == Mono.Cecil.MetadataType.Int64
                    && method.Parameters[1].ParameterType.FullName == "System.DateTimeKind")
                || (method.Parameters.Count == 3 && method.Parameters.All(parameter => parameter.ParameterType.MetadataType == Mono.Cecil.MetadataType.Int32))))
                .Select(method => method.MetadataToken.ToInt32()).ToArray();
            if (publicCtorTokens.Length != 2) throw new InvalidOperationException("public constructor controls changed");
            foreach (int token in tokens.Concat(publicCtorTokens))
            {
                var definition = owner.Methods.Single(method => method.MetadataToken.ToInt32() == token);
                var target = app.MainModule.ImportReference(definition);
                var stub = new Mono.Cecil.MethodDefinition("Target" + token.ToString("X8"), Mono.Cecil.MethodAttributes.Static | Mono.Cecil.MethodAttributes.Public, app.MainModule.TypeSystem.Void);
                host.Methods.Add(stub); stub.Body.InitLocals = true;
                var il = stub.Body.GetILProcessor();
                void PushArguments(bool receiver)
                {
                    if (receiver)
                    {
                        var local = new Mono.Cecil.Cil.VariableDefinition(app.MainModule.ImportReference(owner)); stub.Body.Variables.Add(local);
                        il.Emit(Mono.Cecil.Cil.OpCodes.Ldloca, local);
                    }
                    foreach (var parameter in target.Parameters)
                    {
                        if (parameter.ParameterType is Mono.Cecil.ByReferenceType reference)
                        {
                            var local = new Mono.Cecil.Cil.VariableDefinition(reference.ElementType);stub.Body.Variables.Add(local);
                            il.Emit(Mono.Cecil.Cil.OpCodes.Ldloca, local);
                        }
                        else if (parameter.ParameterType.MetadataType == Mono.Cecil.MetadataType.Int64) il.Emit(Mono.Cecil.Cil.OpCodes.Ldc_I8, 0L);
                        else il.Emit(Mono.Cecil.Cil.OpCodes.Ldc_I4_0);
                    }
                }
                PushArguments(target.HasThis);
                il.Emit(Mono.Cecil.Cil.OpCodes.Call, target);
                if (target.ReturnType.MetadataType != Mono.Cecil.MetadataType.Void) il.Emit(Mono.Cecil.Cil.OpCodes.Pop);
                il.Emit(Mono.Cecil.Cil.OpCodes.Ldftn, target); il.Emit(Mono.Cecil.Cil.OpCodes.Pop);
                if (definition.IsConstructor)
                {
                    PushArguments(false);il.Emit(Mono.Cecil.Cil.OpCodes.Newobj, target);il.Emit(Mono.Cecil.Cil.OpCodes.Pop);
                }
                il.Emit(Mono.Cecil.Cil.OpCodes.Ret);
            }
            app.Write(input, new Mono.Cecil.WriterParameters { Timestamp = 0, DeterministicMvid = true });
        }
        var options = Activator.CreateInstance(Model("TranspileOptions"))!;
        Set(options, "Input", input);Set(options, "AutoRef", false);Set(options, "UseILDiet", false);Set(options, "SharedGenerics", false);Set(options, "IsaSurfaceDump", "predicate-guard-only");
        object loaded;
        try { loaded = Model("Compilation").GetMethod("Create", statics)!.Invoke(null, new object[] { new[] { input, Path.GetFullPath(args[2]) }, options })!; }
        catch (TargetInvocationException error) when (error.InnerException is not null) { throw error.InnerException; }
        var modules = ((IEnumerable)Get(loaded, "Modules")).Cast<object>().ToArray();
        var module = modules.Single(value => (string)Get(value, "AssemblyName") == "System.Private.CoreLib");
        var appModule = modules.Single(value => (string)Get(value, "AssemblyName") == "DateTimeTrioGuardInput");
        var reader = (MetadataReader)Get(module, "Reader");
        var appReader = (MetadataReader)Get(appModule, "Reader");
        var context = Model("GenericContext").GetField("Empty", statics)!.GetValue(null)!;
        int checks = 0;
        void Check(bool condition, string label)
        {
            if (!condition) throw new InvalidOperationException("DateTime trio guard: " + label);
            checks++;
        }
        for (int index = 0; index < tokens.Length; index++)
        {
            var handle = MetadataTokens.MethodDefinitionHandle(tokens[index] & 0xffffff);
            var actual = ((IDictionary)Get(module, "MethodMap"))[handle]!;
            var owner = Get(actual, "DeclaringClass");
            string api = "IsDateTime" + names[index];
            var predicate = Model("CoreIntrinsics").GetMethod(api, statics)!;
            var decoded = Model("CoreIntrinsics").GetMethod(api + "DecodedSignature", statics)!;
            var raw = Model("CoreIntrinsics").GetMethod(api + "Signature", statics)!.CreateDelegate<RawPredicate>();
            bool Accepted() => (bool)predicate.Invoke(null, new[] { actual })!;
            Check(Accepted(), "actual-" + names[index]);
            Check((bool)decoded.Invoke(null, new[] { Get(actual, "Signature"), module })!, "decoded-" + names[index]);
            byte[] original = reader.GetBlobBytes(reader.GetMethodDefinition(handle).Signature);
            Check(raw(reader.GetBlobReader(reader.GetMethodDefinition(handle).Signature)), "raw-actual-" + names[index]);
            int imports = 0, publicImports = 0;
            foreach (var referenceHandle in appReader.MemberReferences)
            {
                string name = appReader.GetString(appReader.GetMemberReference(referenceHandle).Name);
                if (name != (index == 2 ? ".ctor" : names[index])) continue;
                var resolved = Model("Compilation").GetMethod("ResolveMemberRefMethod", instance)!.Invoke(loaded, new[] { appModule, (object)referenceHandle, context })!;
                var resolvedHandle = (MethodDefinitionHandle)Get(resolved, "Handle");
                if (index == 2 && publicCtorTokens.Contains(MetadataTokens.GetToken(resolvedHandle)))
                {
                    Check(ReferenceEquals(Get(resolved, "Module"), module) && ((MethodAttributes)Get(resolved, "Attributes") & MethodAttributes.MemberAccessMask) == MethodAttributes.Public,
                        "actual-public-constructor-authority");
                    Check(!(bool)predicate.Invoke(null, new[] { resolved })!, "public-constructor-not-private-adapter");
                    publicImports++;
                    continue;
                }
                Check(ReferenceEquals(Get(resolved, "Module"), module) && resolvedHandle == handle, "memberref-identity-" + names[index]);
                Check((bool)predicate.Invoke(null, new[] { resolved })!, "memberref-policy-" + names[index]);imports++;
            }
            Check(imports == 1, "one-actual-reference-" + names[index]);
            if (index == 2) Check(publicImports == 2, "both-public-constructor-MemberRefs");
            void Reject(object value, string field, object next)
            {
                object saved = Get(value, field);Set(value, field, next);
                try { Check(!Accepted(), names[index] + "-wrong-" + field); }
                finally { Set(value, field, saved); }
            }
            Reject(module, "AssemblyName", "Application.CoreLib");
            Reject(owner, "Module", appModule);
            Reject(owner, "_fullName", "Application.DateTime");
            Reject(owner, "Handle", default(TypeDefinitionHandle));
            Reject(owner, "IsValueType", false);
            Reject(owner, "IsEnum", true);
            Reject(owner, "GenericArity", 1);
            Reject(actual, "Handle", default(MethodDefinitionHandle));Reject(actual, "Rva", 0);
            Reject(actual, "Name", "WrongName");
            var attrs = (MethodAttributes)Get(actual, "Attributes");
            Reject(actual, "Attributes", attrs ^ MethodAttributes.Static);
            Reject(actual, "Attributes", attrs | MethodAttributes.Virtual);
            Reject(actual, "Attributes", attrs | MethodAttributes.PinvokeImpl);
            Reject(actual, "ImplAttributes", MethodImplAttributes.InternalCall);
            var signature = Get(actual, "Signature");
            Reject(Get(signature, "ReturnType"), "Primitive", PrimitiveTypeCode.Int32);
            var parameters = ((IEnumerable)Get(signature, "ParameterTypes")).Cast<object>().ToArray();
            if (index != 2)
            {
                var output = parameters[^1];
                Reject(output, "Kind", Enum.Parse(Model("TypeKind"), "Class"));
                Reject(Get(output, "Element"), "Kind", Enum.Parse(Model("TypeKind"), "Primitive"));
            }
            else
            {
                var kind = Get(parameters[1], "Class");
                Console.WriteLine("datetime-trio-kind-model:IsEnum=" + Get(kind, "IsEnum") + ":IsValueType=" + Get(kind, "IsValueType") + ":Underlying=" + Get(kind, "EnumUnderlying"));
                Reject(kind, "Module", appModule);
                Reject(kind, "IsEnum", false);
                Reject(kind, "EnumUnderlying", PrimitiveTypeCode.Int64);
                Reject(parameters[1], "Kind", Enum.Parse(Model("TypeKind"), "Primitive"));
                Reject(parameters[2], "Primitive", PrimitiveTypeCode.Int32);
            }
            var vectors = new List<byte[]>();
            var header = original.ToArray();header[0] ^= 0x20;vectors.Add(header);
            var returned = original.ToArray();returned[2] = 0x08;vectors.Add(returned);
            var generic = original.ToArray();generic[0] |= 0x10;vectors.Add(generic);
            vectors.Add(original.Concat(new byte[] { 0 }).ToArray());vectors.Add(original[..^1]);
            if (index == 2) { var marker = original.ToArray();marker[4] = 0x12;vectors.Add(marker); }
            else { var marker = original.ToArray();int pos = index == 0 ? 5 : 11;marker[pos] = 0x12;vectors.Add(marker); }
            foreach (byte[] bytes in vectors)
            {
                var metadata = new MetadataBuilder();metadata.AddModule(0, metadata.GetOrAddString("TrioRaw"), metadata.GetOrAddGuid(Guid.Empty), default, default);
                var blob = metadata.GetOrAddBlob(bytes);var image = new BlobBuilder();new MetadataRootBuilder(metadata).Serialize(image, 0, 0);
                using var provider = MetadataReaderProvider.FromMetadataImage(image.ToImmutableArray());
                Check(!raw(provider.GetMetadataReader().GetBlobReader(blob)), "raw-negative-" + names[index] + "-" + Convert.ToHexString(bytes));
            }
            Check(Accepted(), "restored-" + names[index]);
            Console.WriteLine("datetime-trio-actual:" + names[index] + ":" + tokens[index].ToString("X8") + ":" + Convert.ToHexString(original));
        }
        Console.WriteLine("datetime-trio-guards:" + checks);
        return true;
    }
}
