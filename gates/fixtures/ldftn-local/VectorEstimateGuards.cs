using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

internal static class VectorEstimateGuards
{
    private delegate bool RawPredicate(BlobReader blob, int token, int element);
    public static bool Run(string[] args)
    {
        if (args.Length != 4 || args[0] != "--vector-estimate-guards") return false;
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
        string[] owners = { "System.Numerics.Vector", "System.Runtime.Intrinsics.Vector64", "System.Runtime.Intrinsics.Vector128", "System.Runtime.Intrinsics.Vector256", "System.Runtime.Intrinsics.Vector512" };
        string input = Path.GetFullPath(args[3]);
        if (File.Exists(input)) throw new InvalidOperationException("model app must be new");
        using (var app = Mono.Cecil.AssemblyDefinition.CreateAssembly(new Mono.Cecil.AssemblyNameDefinition("VectorEstimateGuardInput", new Version(1, 0)), "VectorEstimateGuardInput", Mono.Cecil.ModuleKind.Dll))
        using (var core = Mono.Cecil.AssemblyDefinition.ReadAssembly(args[2]))
        {
            var host = new Mono.Cecil.TypeDefinition("Guard", "References", Mono.Cecil.TypeAttributes.Public, app.MainModule.TypeSystem.Object);
            app.MainModule.Types.Add(host);
            int serial = 0;
            foreach (var owner in owners)
            foreach (var method in core.MainModule.GetType(owner).Methods.Where(m => m.Name == "MultiplyAddEstimate" && !m.HasGenericParameters))
            {
                var target = app.MainModule.ImportReference(method);
                var stub = new Mono.Cecil.MethodDefinition("Reference" + serial++, Mono.Cecil.MethodAttributes.Public | Mono.Cecil.MethodAttributes.Static, app.MainModule.TypeSystem.Void);
                host.Methods.Add(stub);
                var il = stub.Body.GetILProcessor();
                il.Emit(Mono.Cecil.Cil.OpCodes.Ldftn, target); il.Emit(Mono.Cecil.Cil.OpCodes.Pop); il.Emit(Mono.Cecil.Cil.OpCodes.Ret);
            }
            var genuine = core.MainModule.GetType("System.Runtime.Intrinsics.Vector128").Methods.Single(m => m.Name == "MultiplyAddEstimate"
                && !m.HasGenericParameters && ((Mono.Cecil.GenericInstanceType)m.ReturnType).GenericArguments[0].MetadataType == Mono.Cecil.MetadataType.Single);
            foreach (bool genericOperand in new[] { false, true })
            {
                var caller = new Mono.Cecil.MethodDefinition(genericOperand ? "BadGenericMvarCaller" : "BadGenericConcreteCaller",
                    Mono.Cecil.MethodAttributes.Public | Mono.Cecil.MethodAttributes.Static, app.MainModule.TypeSystem.Void);
                host.Methods.Add(caller);
                var callerArgument = new Mono.Cecil.GenericParameter("T", caller); caller.GenericParameters.Add(callerArgument);
                var importedTarget = app.MainModule.ImportReference(genuine);
                Mono.Cecil.TypeReference operand = importedTarget.ReturnType;
                if (genericOperand)
                {
                    var open = new Mono.Cecil.GenericInstanceType(((Mono.Cecil.GenericInstanceType)operand).ElementType);
                    open.GenericArguments.Add(callerArgument); operand = open;
                }
                var badTarget = new Mono.Cecil.MethodReference("MultiplyAddEstimate", operand, importedTarget.DeclaringType)
                    { CallingConvention = Mono.Cecil.MethodCallingConvention.Generic };
                badTarget.GenericParameters.Add(new Mono.Cecil.GenericParameter("TMethod", badTarget));
                for (int i = 0; i < 3; i++) badTarget.Parameters.Add(new Mono.Cecil.ParameterDefinition(operand));
                var il = caller.Body.GetILProcessor();
                il.Emit(Mono.Cecil.Cil.OpCodes.Ldftn, badTarget); il.Emit(Mono.Cecil.Cil.OpCodes.Pop); il.Emit(Mono.Cecil.Cil.OpCodes.Ret);
            }
            foreach (string kind in new[] { "zero", "one", "internal" })
            {
                var targetDefinition = kind == "internal"
                    ? core.MainModule.GetType("System.Runtime.Intrinsics.Vector128").Methods.Single(m => m.Name == "MultiplyAddEstimate" && m.HasGenericParameters)
                    : genuine;
                var specification = new Mono.Cecil.GenericInstanceMethod(app.MainModule.ImportReference(targetDefinition));
                if (kind != "zero") specification.GenericArguments.Add(app.MainModule.TypeSystem.Single);
                var caller = new Mono.Cecil.MethodDefinition("MethodSpecification" + kind,
                    Mono.Cecil.MethodAttributes.Public | Mono.Cecil.MethodAttributes.Static, app.MainModule.TypeSystem.Void);
                host.Methods.Add(caller);
                var il = caller.Body.GetILProcessor();
                il.Emit(Mono.Cecil.Cil.OpCodes.Ldftn, specification); il.Emit(Mono.Cecil.Cil.OpCodes.Pop); il.Emit(Mono.Cecil.Cil.OpCodes.Ret);
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
        var appModule = modules.Single(m => (string)Get(m, "AssemblyName") == "VectorEstimateGuardInput");
        var reader = (MetadataReader)Get(module, "Reader");
        var predicate = Model("CoreIntrinsics").GetMethod("IsVectorMultiplyAddEstimate", statics)!;
        var raw = Model("CoreIntrinsics").GetMethod("IsVectorEstimateRawSignature", statics)!.CreateDelegate<RawPredicate>();
        int checks = 0;
        void Check(bool result, string label)
        {
            if (!result) throw new InvalidOperationException("vector estimate guard: " + label);
            checks++;
        }
        bool Accepted(object method) => (bool)predicate.Invoke(null, new[] { method })!;
        var appReader = (MetadataReader)Get(appModule, "Reader");
        var context = Model("GenericContext").GetField("Empty", statics)!.GetValue(null)!;
        var resolve = Model("Compilation").GetMethod("ResolveMemberRefMethod", instance)!;
        var imported = new Dictionary<MethodDefinitionHandle, object>();
        var references = new List<MemberReferenceHandle>();
        var badReferences = new List<MemberReferenceHandle>();
        foreach (var h in appReader.MemberReferences)
        {
            if (appReader.GetString(appReader.GetMemberReference(h).Name) != "MultiplyAddEstimate") continue;
            if ((appReader.GetBlobReader(appReader.GetMemberReference(h).Signature).ReadByte() & 0x10) != 0)
            {
                badReferences.Add(h);
                continue;
            }
            var method = resolve.Invoke(loaded, new[] { appModule, (object)h, context })!;
            imported.Add((MethodDefinitionHandle)Get(method, "Handle"), method); references.Add(h);
        }
        Check(imported.Count == 10, "resolved-public-declarations");
        Check(badReferences.Select(h => Convert.ToHexString(appReader.GetBlobBytes(appReader.GetMemberReference(h).Signature))).Distinct().Count() == 2,
            "serialized-generic-concrete-and-mvar");
        var noArguments = Array.CreateInstance(Model("TypeDesc"), 0);
        var callerContexts = new List<object> { context };
        foreach (var code in new[] { PrimitiveTypeCode.Single, PrimitiveTypeCode.Double })
        {
            var value = imported.Values.Select(m => Get(Get(Get(m, "Signature"), "ReturnType"), "Class"))
                .Select(v => ((Array)Get(Get(v, "Context"), "TypeArgs")).GetValue(0)!)
                .First(e => (PrimitiveTypeCode)Get(e, "Primitive") == code);
            var arguments = Array.CreateInstance(Model("TypeDesc"), 1); arguments.SetValue(value, 0);
            callerContexts.Add(Activator.CreateInstance(Model("GenericContext"), new object[] { noArguments, arguments })!);
        }
        foreach (var callerContext in callerContexts)
        foreach (var badReference in badReferences)
        foreach (var goodReference in references)
        {
            object ResolveReference(MemberReferenceHandle h) => resolve.Invoke(loaded, new[] { appModule, (object)h, callerContext })!;
            var before = ResolveReference(goodReference);
            Check(Accepted(before), "serialized-good-before");
            bool rejected = false;
            try { ResolveReference(badReference); }
            catch (TargetInvocationException error) when (error.InnerException is NotSupportedException) { rejected = true; }
            Check(rejected, "serialized-bad-generic-reference");
            var after = ResolveReference(goodReference);
            Check(ReferenceEquals(before, after) && Accepted(after), "serialized-good-after");
        }
        var rejectSpecification = Model("Compilation").GetMethod("RejectVectorEstimateMethodSpecification", instance)!;
        int specificationRows = appReader.GetTableRowCount(TableIndex.MethodSpec);
        Check(specificationRows == 3, "serialized-method-specification-count");
        var rawSpecifications = new List<string>();
        for (int row = 1; row <= specificationRows; row++)
        {
            var handle = MetadataTokens.MethodSpecificationHandle(row);
            var specification = appReader.GetMethodSpecification(handle);
            string hex = Convert.ToHexString(appReader.GetBlobBytes(specification.Signature));
            rawSpecifications.Add(hex);
            Check(specification.Method.Kind == HandleKind.MemberReference && hex is "0A00" or "0A010C", "serialized-method-specification-shape");
            Console.WriteLine("vector-estimate-guard-methodspec:" + MetadataTokens.GetToken(handle).ToString("X8")
                + ":target=" + MetadataTokens.GetToken(specification.Method).ToString("X8") + ":raw=" + hex);
            foreach (var callerContext in callerContexts)
            foreach (var goodReference in references)
            {
                object ResolveGood() => resolve.Invoke(loaded, new[] { appModule, (object)goodReference, callerContext })!;
                var before = ResolveGood(); Check(Accepted(before), "specification-good-before");
                bool rejected = false;
                try { rejectSpecification.Invoke(loaded, new[] { appModule, (object)handle, callerContext }); }
                catch (TargetInvocationException error) when (error.InnerException is NotSupportedException) { rejected = true; }
                Check(rejected, "reject-serialized-method-specification");
                Check(ReferenceEquals(before, ResolveGood()), "specification-good-after");
            }
        }
        Check(rawSpecifications.Count(s => s == "0A00") == 1 && rawSpecifications.Count(s => s == "0A010C") == 2,
            "zero-one-and-internal-specification-signatures");
        int concrete = 0, generic = 0;
        foreach (var owner in imported.Values.Select(m => Get(m, "DeclaringClass")).Distinct())
        foreach (var h in reader.GetTypeDefinition((TypeDefinitionHandle)Get(owner, "Handle")).GetMethods())
        {
            var definition = reader.GetMethodDefinition(h);
            if (reader.GetString(definition.Name) != "MultiplyAddEstimate") continue;
            bool expected = definition.GetGenericParameters().Count == 0;
            object candidate;
            if (expected) candidate = imported[h];
            else
            {
                candidate = Activator.CreateInstance(Model("MethodInfo"))!;
                Set(candidate, "DeclaringClass", owner); Set(candidate, "Module", module);
                Set(candidate, "Name", "MultiplyAddEstimate"); Set(candidate, "Handle", h);
                Set(candidate, "Context", Get(owner, "Context"));
                Set(candidate, "Attributes", definition.Attributes); Set(candidate, "ImplAttributes", definition.ImplAttributes);
                Set(candidate, "Rva", definition.RelativeVirtualAddress);
            }
            Check(Accepted(candidate) == expected, "methoddef-" + MetadataTokens.GetToken(h).ToString("X8", CultureInfo.InvariantCulture));
            Check(raw(reader.GetBlobReader(definition.Signature), 0, 0) == expected, "raw-declaration");
            if (expected) concrete++; else generic++;
        }
        Check(concrete == 10 && generic == 5, "finite-public-and-generic-neighbors");
        var emptyArray = Array.CreateInstance(Model("TypeDesc"), 0);
        foreach (var current in imported.Values)
        {
            var owner = Get(current, "DeclaringClass");
            var signature = Get(current, "Signature");
            var result = Get(signature, "ReturnType");
            var vector = Get(result, "Class");
            var element = ((Array)Get(Get(vector, "Context"), "TypeArgs")).GetValue(0)!;
            var typeArray = Array.CreateInstance(Model("TypeDesc"), 1); typeArray.SetValue(element, 0);
            object Context(Array types, Array methods) => Activator.CreateInstance(Model("GenericContext"), new object[] { types, methods })!;
            void Reject(object value, string field, object next)
            {
                Check(Accepted(current), "before-" + field);
                object saved = Get(value, field); Set(value, field, next);
                try { Check(!Accepted(current), "reject-" + field); }
                finally { Set(value, field, saved); }
                Check(Accepted(current), "restored-" + field);
            }
            Reject(module, "AssemblyName", "Application.CoreLib");
            Reject(owner, "Module", appModule); Reject(owner, "_fullName", "Application.Vector");
            Reject(owner, "Handle", default(TypeDefinitionHandle)); Reject(owner, "IsValueType", false);
            Reject(owner, "IsInterface", true); Reject(owner, "IsEnum", true); Reject(owner, "IsByRefLike", true);
            Reject(owner, "GenericArity", 1); Reject(owner, "IntrinsicCppName", "Dn2CppOtherVector");
            Reject(owner, "Context", Context(typeArray, emptyArray));
            Reject(current, "Module", appModule); Reject(current, "Handle", default(MethodDefinitionHandle));
            Reject(current, "Rva", 0); Reject(current, "Rva", 1); Reject(current, "Name", "ToScalar"); Reject(current, "IsSynthetic", true);
            Reject(current, "Context", Context(emptyArray, typeArray)); Reject(current, "Context", Context(typeArray, emptyArray));
            var sibling = imported.Values.Single(m => ReferenceEquals(Get(m, "DeclaringClass"), owner) && !ReferenceEquals(m, current));
            Reject(current, "Signature", Get(sibling, "Signature"));
            Reject(current, "Handle", Get(sibling, "Handle"));
            var attrs = (MethodAttributes)Get(current, "Attributes");
            foreach (var flags in new[] { attrs & ~MethodAttributes.Static, attrs | MethodAttributes.Virtual, attrs | MethodAttributes.PinvokeImpl,
                attrs | MethodAttributes.SpecialName, MethodAttributes.Private | MethodAttributes.Static | MethodAttributes.HideBySig }) Reject(current, "Attributes", flags);
            foreach (var impl in new[] { MethodImplAttributes.InternalCall, MethodImplAttributes.NoInlining, MethodImplAttributes.Synchronized }) Reject(current, "ImplAttributes", impl);
            Reject(vector, "Module", appModule); Reject(vector, "Handle", default(TypeDefinitionHandle));
            Reject(vector, "IsValueType", false); Reject(vector, "IsEnum", true); Reject(vector, "IsInterface", true);
            Reject(vector, "IsByRefLike", true); Reject(vector, "GenericArity", 0); Reject(vector, "IntrinsicCppName", "Dn2CppOtherVector");
            Reject(vector, "Context", Context(emptyArray, emptyArray));
            foreach (var operand in ((IEnumerable)Get(signature, "ParameterTypes")).Cast<object>().Append(result).Distinct())
            {
                Reject(operand, "Kind", Enum.Parse(Model("TypeKind"), "ByRef"));
                Reject(operand, "IsCanonPlaceholder", true);
            }
            foreach (var code in new[] { PrimitiveTypeCode.Int32, PrimitiveTypeCode.Boolean, PrimitiveTypeCode.Char, PrimitiveTypeCode.Object }) Reject(element, "Primitive", code);
            Reject(element, "Kind", Enum.Parse(Model("TypeKind"), "GenericVar")); Reject(element, "IsCanonPlaceholder", true);
        }
        foreach (var h in references)
        {
            object Resolve() => resolve.Invoke(loaded, new[] { appModule, (object)h, context })!;
            var first = Resolve(); Check(Accepted(first), "reference-first");
            void Poison(object value, string field, object bad)
            {
                var saved = Get(value, field); Set(value, field, bad);
                try
                {
                    bool rejected = false;
                    try { Resolve(); }
                    catch (TargetInvocationException error) when (error.InnerException is NotSupportedException) { rejected = true; }
                    Check(rejected, "reference-cache-" + field);
                }
                finally { Set(value, field, saved); }
                Check(ReferenceEquals(first, Resolve()), "reference-cache-restored");
            }
            Poison(module, "AssemblyName", "Application.CoreLib");
            Poison(first, "Rva", 0); Poison(first, "IsSynthetic", true);
            Poison(first, "Attributes", MethodAttributes.Private | MethodAttributes.Static | MethodAttributes.HideBySig);
        }
        var vectors = new List<(string Hex, int Token, bool Expected)>();
        foreach (string scalar in new[] { "0c", "0d" })
        {
            string operand = "15110401" + scalar;
            string valid = "0003" + string.Concat(Enumerable.Repeat(operand, 4));
            vectors.Add((valid, 4, true)); vectors.Add((valid, 8, false));
            vectors.Add((valid.Replace("151104", "151105", StringComparison.Ordinal), 0, true));
            foreach (string header in new[] { "2003", "0503", "100103", "0002", "0004" }) vectors.Add((header + valid[4..], 0, false));
            vectors.Add((valid + "00", 0, false));
            for (int size = 0; size < valid.Length; size += 2) vectors.Add((valid[..size], 0, false));
            for (int position = 0; position < 4; position++)
            foreach (string bad in new[] { "1015110401" + scalar, "0f15110401" + scalar, "4515110401" + scalar,
                "200415110401" + scalar, "1f0415110401" + scalar, "15120401" + scalar,
                "15110001" + scalar, "15110601" + scalar, "15110801" + scalar,
                "15110402" + scalar, "1511040108", "151104011300", "151104011e00",
                "15110401" + (scalar == "0c" ? "0d" : "0c") })
            {
                var parts = Enumerable.Repeat(operand, 4).ToArray(); parts[position] = bad;
                vectors.Add(("0003" + string.Concat(parts), 0, false));
            }
        }
        foreach (var vector in vectors)
        {
            var metadata = new MetadataBuilder();
            metadata.AddModule(0, metadata.GetOrAddString("VectorEstimateRaw"), metadata.GetOrAddGuid(Guid.Empty), default, default);
            var blob = metadata.GetOrAddBlob(Convert.FromHexString(vector.Hex));
            var image = new BlobBuilder(); new MetadataRootBuilder(metadata).Serialize(image, 0, 0);
            using var source = MetadataReaderProvider.FromMetadataImage(image.ToImmutableArray());
            Check(raw(source.GetMetadataReader().GetBlobReader(blob), vector.Token, 0) == vector.Expected, "raw-" + vector.Hex);
        }
        Console.WriteLine("vector-estimate-guards:" + checks + ":raw=" + vectors.Count + ":public=" + concrete + ":generic=" + generic);
        return true;
    }
}
