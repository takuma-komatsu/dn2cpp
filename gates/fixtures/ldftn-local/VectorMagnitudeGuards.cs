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
        if (args.Length != 4 || args[0] != "--vector-magnitude-guards") return false;
        foreach (string operation in VectorMagnitudeFixture.Operations)
        foreach (int width in new[] { 0, 64, 128, 256, 512 }) RunCase(args, operation, width);
        return true;
    }

    private static void RunCase(string[] args, string operation, int width)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        const BindingFlags instance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        const BindingFlags statics = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        var cases = new List<(string Element, string Route, string Mutation)>();
        foreach (var element in new[] { "float", "double" })
        foreach (var route in new[] { "direct", "address", "calli" })
        foreach (var mutation in new[] { "valid", "valid-mvar", "valid-var" }.Concat(VectorMagnitudeFixture.Mutations))
            cases.Add((element, route, mutation));
        foreach (var element in VectorMagnitudeFixture.Elements.Where(e => e is not ("float" or "double")))
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
        var resolve = Model("Compilation").GetMethod("TryResolveVectorMagnitudeSpec", instance)!;
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
        bool Accepted(object? value) => value is not null && (bool)predicate.Invoke(null, new[] { value })!;
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
            var item = cases[i]; if (item.Mutation != "valid" || item.Route != "direct" || item.Element is not ("float" or "double")) continue;
            var token = Token(methods[i]);
            var method = Invoke(resolve, app, token, empty)!;
            Check(Accepted(method), "good-initial-" + item.Element);
            var argument = ((Array)Get(Get(method, "Context"), "MethodArgs")).GetValue(0)!;
            good.Add(item.Element, (token, method, argument));
        }
        for (int i = 0; i < cases.Count; i++)
        {
            var item = cases[i]; var token = Token(methods[i]); var control = good[item.Element == "double" ? "double" : "float"];
            var contexts = new[] { empty, Context(control.Element, false), Context(control.Element, true) };
            foreach (var context in contexts)
            foreach (var resolver in token.Kind == HandleKind.MethodSpecification ? new[] { resolve, resolveSpec, resolveCall } : new[] { resolveReference, resolveFunction, resolveCall })
            {
                Check(ReferenceEquals(Invoke(resolve, app, control.Token, empty), control.Method), "good-before");
                bool expected = item.Element is "float" or "double" && (item.Mutation == "valid" || item.Mutation == "valid-mvar" && ReferenceEquals(context, contexts[1]) || item.Mutation == "valid-var" && ReferenceEquals(context, contexts[2]));
                if (expected) Check(Accepted(Invoke(resolver, app, token, context)), item.Route + ":" + item.Mutation + ":accept");
                else Check(Refused(resolver, app, token, context), item.Route + ":" + item.Mutation + ":reject");
                Check(ReferenceEquals(Invoke(resolve, app, control.Token, empty), control.Method) && Accepted(control.Method), "good-after");
            }
            if (token.Kind == HandleKind.MethodSpecification)
            {
                var spec = reader.GetMethodSpecification((MethodSpecificationHandle)token);
                Console.WriteLine("vector-magnitude-serialized:" + item.Element + ":" + item.Route + ":" + item.Mutation + ":" + MetadataTokens.GetToken(token).ToString("X8") + ":raw=" + Convert.ToHexString(reader.GetBlobBytes(spec.Signature)));
            }
        }
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
            var owner = Get(method, "DeclaringClass");
            Poison(owner, "Module", app); Poison(owner, "Handle", default(TypeDefinitionHandle));
            Poison(owner, "_fullName", "Application.Vector"); Poison(owner, "IsValueType", false);
            Poison(owner, "IsEnum", true); Poison(owner, "IsInterface", true); Poison(owner, "GenericArity", 1);
            Poison(method, "Name", "Max");
            var vector = Get(Get(Get(method, "Signature"), "ReturnType"), "Class");
            Poison(vector, "Module", app); Poison(vector, "Handle", default(TypeDefinitionHandle));
            Poison(vector, "IsValueType", false); Poison(vector, "IsEnum", true); Poison(vector, "IsInterface", true);
            Poison(vector, "GenericArity", 0); Poison(vector, "Context", empty);
            Poison(control.Element, "Primitive", PrimitiveTypeCode.Int32);
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
        Console.WriteLine("vector-magnitude-guards:" + operation + ":width=" + width + ":checks=" + checks.ToString(CultureInfo.InvariantCulture));
    }
}
