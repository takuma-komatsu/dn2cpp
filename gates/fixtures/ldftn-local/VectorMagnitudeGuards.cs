using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

internal static class VectorMagnitudeGuards
{
    private delegate bool RawPredicate(BlobReader signature, out int token);
    public static bool Run(string[] args)
    {
        if (FixedVectorNumberGuards.BuildDependency(args)) return true;
        if (args.Length != 4 || args[0] is not ("--vector-magnitude-guards" or "--vector-number-guards" or "--vector-number-shared-authority" or "--vector-number-shared-audit")) return false;
        bool number = args[0] is "--vector-number-guards" or "--vector-number-shared-authority" or "--vector-number-shared-audit";
        foreach (string operation in number ? VectorMagnitudeFixture.NumberOperations : VectorMagnitudeFixture.Operations)
        foreach (int width in args[0] is "--vector-number-shared-authority" or "--vector-number-shared-audit" ? new[] { 0 } : new[] { 0, 64, 128, 256, 512 }) RunCase(args, operation, width, number);
        return true;
    }

    private static void RunCase(string[] args, string operation, int width, bool number)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        const BindingFlags instance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        const BindingFlags statics = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        var cases = new List<(string Element, string Route, string Mutation)>();
        var admitted = new[] { "float", "double", "decimal", "enum", "struct", "refs", "int128", "uint128" };
        string prefix = number ? "vector-number" : "vector-magnitude";
        foreach (var element in admitted)
        foreach (var route in new[] { "direct", "address", "calli" })
        foreach (var mutation in new[] { "valid", "valid-mvar", "valid-var" }.Concat(VectorMagnitudeFixture.Mutations))
            cases.Add((element, route, mutation));
        foreach (var element in VectorMagnitudeFixture.Elements.Where(e => !admitted.Contains(e)))
        foreach (var route in new[] { "direct", "address", "calli" }) cases.Add((element, route, "valid"));
        string input = Path.GetFullPath(args[3]) + "." + operation + "." + width + ".dll";
        VectorMagnitudeFixture.Build(input, args[2], cases, width, operation);
        var production = Assembly.LoadFrom(Path.GetFullPath(args[1]));
        Type Model(string name) => production.GetType("Dn2Cpp." + name, true)!;
        object Get(object value, string name) => value.GetType().GetField(name, instance) is { } field ? field.GetValue(value)! : value.GetType().GetProperty(name, instance)!.GetValue(value)!;
        void Set(object value, string name, object? next)
        {
            if (value.GetType().GetField(name, instance) is { } field) field.SetValue(value, next);
            else value.GetType().GetProperty(name, instance)!.SetValue(value, next);
        }
        var options = Activator.CreateInstance(Model("TranspileOptions"))!;
        Set(options, "Input", input); Set(options, "UseILDiet", false); Set(options, "SharedGenerics", false);
        Set(options, "AutoRef", false); Set(options, "IsaSurfaceDump", "predicate-guard-only");
        object compilation;
        try { compilation = Model("Compilation").GetMethod("Create", statics)!.Invoke(null, new object[] { new[] { input, Path.GetFullPath(args[2]) }, options })!; }
        catch (TargetInvocationException error) when (error.InnerException is not null) { throw error.InnerException; }
        var modules = ((IEnumerable)Get(compilation, "Modules")).Cast<object>().ToArray();
        var app = modules.Single(m => (string)Get(m, "AssemblyName") == "VectorMagnitude");
        var core = modules.Single(m => (string)Get(m, "AssemblyName") == "System.Private.CoreLib");
        var reader = (MetadataReader)Get(app, "Reader");
        var coreReader = (MetadataReader)Get(core, "Reader");
        var predicate = Model("CoreIntrinsics").GetMethod("IsVectorMagnitude", statics)!;
        var managedPredicate = Model("CoreIntrinsics").GetMethod("IsVectorMagnitudeManagedGuard", statics)!;
        var resolve = Model("Compilation").GetMethod("TryResolveVectorMagnitudeSpec", instance)!;
        var scan = Model("Compilation").GetMethod("TryResolveVectorMagnitudeSpecForScan", instance)!;
        var instantiate = Model("Compilation").GetMethod("InstantiateMethodOnClass", instance)!;
        var resolveSpec = Model("Compilation").GetMethod("ResolveMethodSpec", instance)!;
        var resolveCall = Model("Compilation").GetMethod("ResolveCallTarget", instance)!;
        var resolveReference = Model("Compilation").GetMethod("ResolveMemberRefMethod", instance)!;
        var resolveFunction = Model("Compilation").GetMethod("ResolveFunctionMemberRef", instance)!;
        var empty = Model("GenericContext").GetField("Empty", statics)!.GetValue(null)!;
        var emptyArguments = Array.CreateInstance(Model("TypeDesc"), 0);
        object Context(object argument, bool type)
        {
            var values = Array.CreateInstance(Model("TypeDesc"), 1); values.SetValue(argument, 0);
            return Activator.CreateInstance(Model("GenericContext"), new object[] { type ? values : emptyArguments, type ? emptyArguments : values })!;
        }
        int checks = 0;
        void Check(bool result, string label)
        {
            if (!result) throw new InvalidOperationException("vector magnitude guard: " + label);
            checks++;
        }
        bool Accepted(object? value)
        {
            if (value is null) return false;
            bool numeric = (bool)predicate.Invoke(null, new[] { value })!;
            bool managed = (bool)managedPredicate.Invoke(null, new[] { value })!;
            Check(!(numeric && managed), "disjoint-routes");
            return numeric || managed;
        }
        object? Invoke(System.Reflection.MethodInfo method, object module, EntityHandle handle, object context)
        {
            object typedHandle = method == resolveCall ? handle : handle.Kind == HandleKind.MethodSpecification ? (object)(MethodSpecificationHandle)handle : (MemberReferenceHandle)handle;
            return method.Invoke(compilation, new[] { module, typedHandle, context });
        }
        bool Refused(System.Reflection.MethodInfo method, object module, EntityHandle handle, object context)
        {
            try { return Invoke(method, module, handle, context) is null; }
            catch (TargetInvocationException error) when (error.InnerException is NotSupportedException) { return true; }
        }
        using var serialized = Mono.Cecil.AssemblyDefinition.ReadAssembly(input);
        var methods = serialized.MainModule.GetType("Fixture.Program").Methods.Where(m => m.Name.StartsWith("Case", StringComparison.Ordinal)).ToArray();
        Check(methods.Length == cases.Count, "serialized-case-count");
        EntityHandle Token(Mono.Cecil.MethodDefinition method)
        {
            var target = method.Body.Instructions.Select(i => i.Operand).OfType<Mono.Cecil.MethodReference>().Single(m => m.Name == operation);
            return MetadataTokens.EntityHandle(target.MetadataToken.ToInt32());
        }
        var good = new Dictionary<string, (EntityHandle Token, object Method, object Element)>();
        for (int i = 0; i < cases.Count; i++)
        {
            var item = cases[i]; if (item.Mutation != "valid" || item.Route != "direct" || !admitted.Contains(item.Element)) continue;
            var token = Token(methods[i]);
            var method = Invoke(resolve, app, token, empty)!;
            Check(Accepted(method), "good-initial-" + item.Element);
            var argument = ((Array)Get(Get(method, "Context"), "MethodArgs")).GetValue(0)!;
            good.Add(item.Element, (token, method, argument));
        }
        foreach (var pair in good)
        {
            bool managed = pair.Key is not ("float" or "double");
            Check((bool)managedPredicate.Invoke(null, new[] { pair.Value.Method })! == managed, "route-kind-" + pair.Key);
            Check((bool)predicate.Invoke(null, new[] { pair.Value.Method })! != managed, "numeric-route-kind-" + pair.Key);
        }
        if (args[0] is "--vector-number-shared-authority" or "--vector-number-shared-audit")
        {
            FixedVectorNumberGuards.CheckShared(compilation, core, args[2], operation,
                good.Where(p => p.Key is not ("float" or "double")).Select(p => p.Value.Method).ToArray(), Check, args[0] == "--vector-number-shared-audit");
            return;
        }
        if (number && width == 0)
            GenericVectorNumberGuards.Check(compilation, core, args[2], operation,
                good.Where(p => p.Key is not ("float" or "double")).Select(p => p.Value.Method).ToArray(), Check);
        if (number && width != 0)
            FixedVectorNumberGuards.Check(compilation, core, args[2], operation, width,
                good.Where(p => p.Key is not ("float" or "double")).Select(p => p.Value.Method).ToArray(), Check);
        if (!number && width == 0)
            GenericVectorMagnitudeGuards.Check(compilation, core, args[2], operation,
                good.Where(p => p.Key is not ("float" or "double")).Select(p => p.Value.Method).ToArray(), Check);
        if (!number && width != 0)
            FixedVectorMagnitudeGuards.Check(compilation, core, args[2], operation, width,
                good.Where(p => p.Key is not ("float" or "double")).Select(p => p.Value.Method).ToArray(), Check);
        for (int i = 0; i < cases.Count; i++)
        {
            var item = cases[i]; var token = Token(methods[i]); var control = good[admitted.Contains(item.Element) ? item.Element : "float"];
            var contexts = new[] { empty, Context(control.Element, false), Context(control.Element, true) };
            foreach (var context in contexts)
            foreach (var resolver in token.Kind == HandleKind.MethodSpecification ? new[] { resolve, resolveSpec, resolveCall } : new[] { resolveReference, resolveFunction, resolveCall })
            {
                Check(ReferenceEquals(Invoke(resolve, app, control.Token, empty), control.Method), "good-before");
                bool expected = admitted.Contains(item.Element) && (item.Mutation == "valid" || item.Mutation == "valid-mvar" && ReferenceEquals(context, contexts[1]) || item.Mutation == "valid-var" && ReferenceEquals(context, contexts[2]));
                if (expected) Check(Accepted(Invoke(resolver, app, token, context)), item.Route + ":" + item.Mutation + ":accept");
                else Check(Refused(resolver, app, token, context), item.Route + ":" + item.Mutation + ":reject");
                Check(ReferenceEquals(Invoke(resolve, app, control.Token, empty), control.Method) && Accepted(control.Method), "good-after");
            }
            if (token.Kind == HandleKind.MethodSpecification)
            {
                var spec = reader.GetMethodSpecification((MethodSpecificationHandle)token);
                Console.WriteLine(prefix + "-serialized:" + item.Element + ":" + item.Route + ":" + item.Mutation + ":" + MetadataTokens.GetToken(token).ToString("X8") + ":raw=" + Convert.ToHexString(reader.GetBlobBytes(spec.Signature)));
            }
        }
        var canonical = Activator.CreateInstance(Model("TypeDesc"), true)!;
        Set(canonical, "Kind", Get(good["float"].Element, "Kind"));
        Set(canonical, "Primitive", Get(good["float"].Element, "Primitive"));
        Set(canonical, "IsCanonPlaceholder", true); Set(canonical, "CanonAnyIndex", -1);
        try
        {
            for (int i = 0; i < cases.Count; i++)
            {
                var item = cases[i];
                if (item.Element != "float" || item.Route != "direct") continue;
                var token = Token(methods[i]);
                if (token.Kind != HandleKind.MethodSpecification) continue;
                if (item.Mutation is "valid-mvar" or "valid-var")
                {
                    var context = Context(canonical, item.Mutation == "valid-var");
                    Check(Refused(resolve, app, token, context), "canonical-strict-reject-" + item.Mutation);
                    Check(Invoke(scan, app, token, context) is null, "canonical-scan-defer-" + item.Mutation);
                    Check(Refused(scan, app, token, empty), "canonical-empty-context-" + item.Mutation);
                    var wrongContext = Context(canonical, item.Mutation != "valid-var");
                    Check(Refused(scan, app, token, wrongContext), "canonical-wrong-context-" + item.Mutation);
                    Set(canonical, "CanonAnyIndex", 0);
                    try { Check(Refused(scan, app, token, context), "canonical-any-index-reject-" + item.Mutation); }
                    finally { Set(canonical, "CanonAnyIndex", -1); }
                }
                else if (item.Mutation == "valid")
                    Check(ReferenceEquals(Invoke(scan, app, token, Context(canonical, false)), good["float"].Method), "canonical-literal-not-deferred");
                else
                    Check(Refused(scan, app, token, Context(canonical, false)), "canonical-malformed-" + item.Mutation);
            }
        }
        finally { Set(canonical, "IsCanonPlaceholder", false); }
        Check(ReferenceEquals(Invoke(resolve, app, good["float"].Token, empty), good["float"].Method), "canonical-restored-concrete-cache");
        foreach (var control in good.Values)
        {
            var method = control.Method;
            var arguments = Array.CreateInstance(Model("TypeDesc"), 1); arguments.SetValue(control.Element, 0);
            object Instantiate(Array values) => instantiate.Invoke(compilation, new[] { Get(method, "DeclaringClass"), core, Get(method, "Handle"), values })!;
            foreach (int arity in new[] { 0, 2 })
            {
                Check(ReferenceEquals(Instantiate(arguments), method), "reflection-good-before");
                var invalid = Array.CreateInstance(Model("TypeDesc"), arity);
                for (int a = 0; a < arity; a++) invalid.SetValue(control.Element, a);
                bool refused = false;
                try { Instantiate(invalid); }
                catch (TargetInvocationException error) when (error.InnerException is NotSupportedException) { refused = true; }
                Check(refused, "reflection-invalid-arity");
                Check(ReferenceEquals(Instantiate(arguments), method) && Accepted(method), "reflection-good-after");
            }
            void Poison(object value, string field, object replacement)
            {
                var saved = Get(value, field); Set(value, field, replacement);
                try
                {
                    Check(!Accepted(method), "predicate-poison-" + field);
                    Check(Refused(resolve, app, control.Token, empty), "cache-poison-" + field);
                }
                finally { Set(value, field, saved); }
                Check(ReferenceEquals(Invoke(resolve, app, control.Token, empty), method) && Accepted(method), "cache-restored-" + field);
            }
            Poison(method, "Rva", 0); Poison(method, "Rva", 1); Poison(method, "IsSynthetic", true);
            Poison(method, "Module", app); Poison(method, "Handle", default(MethodDefinitionHandle));
            Poison(method, "Attributes", MethodAttributes.Private | MethodAttributes.Static | MethodAttributes.HideBySig);
            Poison(method, "ImplAttributes", MethodImplAttributes.NoInlining);
            Poison(method, "Context", empty);
            foreach (var other in good.Values.Where(other => !ReferenceEquals(other.Element, control.Element)))
                Poison(method, "Context", Context(other.Element, false));
            var owner = Get(method, "DeclaringClass");
            Poison(owner, "Module", app); Poison(owner, "Handle", default(TypeDefinitionHandle));
            Poison(owner, "_fullName", "Application.Vector"); Poison(owner, "IsValueType", false);
            Poison(owner, "IsEnum", true); Poison(owner, "IsInterface", true); Poison(owner, "GenericArity", 1);
            Poison(method, "Name", "Max");
            var vector = Get(Get(Get(method, "Signature"), "ReturnType"), "Class");
            Poison(vector, "Module", app); Poison(vector, "Handle", default(TypeDefinitionHandle));
            Poison(vector, "IsValueType", false); Poison(vector, "IsEnum", true); Poison(vector, "IsInterface", true);
            Poison(vector, "GenericArity", 0); Poison(vector, "Context", empty);
            if ((bool)predicate.Invoke(null, new[] { method })!)
                Poison(control.Element, "Primitive", PrimitiveTypeCode.Int32);
            else
            {
                var elementClass = Get(control.Element, "Class");
                Poison(elementClass, "IsInterface", true);
                Poison(elementClass, "IsByRefLike", true);
                Poison(elementClass, "GenericArity", 1);
                Poison(elementClass, "Handle", default(TypeDefinitionHandle));
            }
            Poison(control.Element, "IsCanonPlaceholder", true);
        }
        using (var originalCore = Mono.Cecil.AssemblyDefinition.ReadAssembly(args[2]))
        foreach (string concrete in new[] { "Vector2", "Vector3", "Vector4" })
        {
            var wrapper = originalCore.MainModule.GetType("System.Numerics." + concrete).Methods.Single(m => m.Name == operation);
            var call = wrapper.Body.Instructions.Select(i => i.Operand).OfType<Mono.Cecil.GenericInstanceMethod>().Single(m => m.Name == operation);
            var token = MetadataTokens.EntityHandle(call.MetadataToken.ToInt32());
            Check(token.Kind == HandleKind.MethodSpecification && coreReader.GetMethodSpecification((MethodSpecificationHandle)token).Method.Kind == HandleKind.MethodDefinition, "retained-wrapper-methoddef-spec");
            foreach (var resolver in new[] { resolve, resolveSpec, resolveCall })
                Check(Accepted(Invoke(resolver, core, token, empty)), "retained-wrapper-" + concrete);
        }
        var raw = Model("CoreIntrinsics").GetMethod("ReadVectorMagnitudeSignature", statics)!.CreateDelegate<RawPredicate>();
        const string vectorShape = "151104011E00";
        string signatureHex = "100102" + vectorShape + vectorShape + vectorShape;
        var rawCases = new List<(string Hex, bool Expected)> { (signatureHex, true) };
        for (int length = 0; length < signatureHex.Length; length += 2) rawCases.Add((signatureHex[..length], false));
        rawCases.Add((signatureHex + "00", false));
        foreach (string header in new[] { "0002", "2002", "300102", "150102", "100002", "100202", "100101", "100103" })
            rawCases.Add((header + vectorShape + vectorShape + vectorShape, false));
        foreach (string bad in new[] { "151105011E00", "151108011E00", "151204011E00", "151104021E001E00", "151104011E01", "151104011300", "151104010C", "10151104011E00", "0F151104011E00", "45151104011E00", "1F04151104011E00", "2004151104011E00", "0C" })
        for (int position = 0; position < 3; position++)
        {
            var shapes = Enumerable.Repeat(vectorShape, 3).ToArray(); shapes[position] = bad;
            rawCases.Add(("100102" + string.Concat(shapes), false));
        }
        foreach (var item in rawCases)
        {
            var builder = new MetadataBuilder();
            builder.AddModule(0, builder.GetOrAddString("VectorMagnitudeRaw"), builder.GetOrAddGuid(Guid.Empty), default, default);
            var blob = builder.GetOrAddBlob(Convert.FromHexString(item.Hex));
            var image = new BlobBuilder(); new MetadataRootBuilder(builder).Serialize(image, 0, 0);
            using var source = MetadataReaderProvider.FromMetadataImage(image.ToImmutableArray());
            Check(raw(source.GetMetadataReader().GetBlobReader(blob), out int token) == item.Expected, "raw-shape-" + item.Hex);
            if (item.Expected) Check(token == 4, "raw-vector-token");
        }
        Console.WriteLine(prefix + "-guards:" + operation + ":width=" + width + ":checks=" + checks.ToString(CultureInfo.InvariantCulture));
    }
}
