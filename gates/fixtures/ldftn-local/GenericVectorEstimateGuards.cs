using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

internal static class GenericVectorEstimateGuards
{
    public static bool Run(string[] args)
    {
        if (args.Length != 4 || args[0] != "--generic-vector-estimate-guards") return false;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        const BindingFlags instance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        const BindingFlags statics = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        var cases = new List<(string Element, string Route, string Mutation)>();
        foreach (var element in GenericVectorEstimateFixture.Elements)
        foreach (var route in new[] { "direct", "address", "calli" })
        foreach (var mutation in new[] { "valid", "valid-mvar", "valid-var" }.Concat(GenericVectorEstimateFixture.Mutations))
            cases.Add((element, route, mutation));
        GenericVectorEstimateFixture.Build(args[3], args[2], cases);
        var production = Assembly.LoadFrom(Path.GetFullPath(args[1]));
        Type Model(string name) => production.GetType("Dn2Cpp." + name, true)!;
        object Get(object value, string name) => value.GetType().GetField(name, instance) is { } field ? field.GetValue(value)! : value.GetType().GetProperty(name, instance)!.GetValue(value)!;
        void Set(object value, string name, object? next)
        {
            if (value.GetType().GetField(name, instance) is { } field) field.SetValue(value, next);
            else value.GetType().GetProperty(name, instance)!.SetValue(value, next);
        }
        var options = Activator.CreateInstance(Model("TranspileOptions"))!;
        Set(options, "Input", Path.GetFullPath(args[3])); Set(options, "UseILDiet", false); Set(options, "SharedGenerics", false);
        Set(options, "AutoRef", false); Set(options, "IsaSurfaceDump", "predicate-guard-only");
        object compilation;
        try { compilation = Model("Compilation").GetMethod("Create", statics)!.Invoke(null, new object[] { new[] { Path.GetFullPath(args[3]), Path.GetFullPath(args[2]) }, options })!; }
        catch (TargetInvocationException error) when (error.InnerException is not null) { throw error.InnerException; }
        var modules = ((IEnumerable)Get(compilation, "Modules")).Cast<object>().ToArray();
        var app = modules.Single(m => (string)Get(m, "AssemblyName") == "GenericVectorEstimate");
        var core = modules.Single(m => (string)Get(m, "AssemblyName") == "System.Private.CoreLib");
        var reader = (MetadataReader)Get(app, "Reader");
        var coreReader = (MetadataReader)Get(core, "Reader");
        var predicate = Model("CoreIntrinsics").GetMethod("IsGenericVectorEstimate", statics)!;
        var resolve = Model("Compilation").GetMethod("TryResolveGenericVectorEstimateSpec", instance)!;
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
            if (!result) throw new InvalidOperationException("generic vector estimate guard: " + label);
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
        using var serialized = Mono.Cecil.AssemblyDefinition.ReadAssembly(args[3]);
        var methods = serialized.MainModule.GetType("Fixture.Program").Methods.Where(m => m.Name.StartsWith("Case", StringComparison.Ordinal)).ToArray();
        Check(methods.Length == cases.Count, "serialized-case-count");
        EntityHandle Token(Mono.Cecil.MethodDefinition method)
        {
            var target = method.Body.Instructions.Select(i => i.Operand).OfType<Mono.Cecil.MethodReference>().Single(m => m.Name == "MultiplyAddEstimate");
            return MetadataTokens.EntityHandle(target.MetadataToken.ToInt32());
        }
        var good = new Dictionary<string, (EntityHandle Token, object Method, object Element)>();
        for (int i = 0; i < cases.Count; i++)
        {
            var item = cases[i]; if (item.Mutation != "valid" || item.Route != "direct") continue;
            var token = Token(methods[i]);
            var method = Invoke(resolve, app, token, empty)!;
            Check(Accepted(method), "good-initial-" + item.Element);
            var argument = ((Array)Get(Get(method, "Context"), "MethodArgs")).GetValue(0)!;
            good.Add(item.Element, (token, method, argument));
        }
        var resolveGuard = Model("Compilation").GetMethod("ResolveGenericVectorEstimateGuard", instance)!;
        var originalPe = (System.Reflection.PortableExecutable.PEReader)Get(core, "PE");
        var originalImage = File.ReadAllBytes(args[2]);
        int BodyOffset(int rva)
        {
            var section = originalPe.PEHeaders.SectionHeaders.Single(s => rva >= s.VirtualAddress && rva < s.VirtualAddress + s.SizeOfRawData);
            int offset = section.PointerToRawData + rva - section.VirtualAddress;
            int header = (originalImage[offset] & 3) == 2 ? 1 : (originalImage[offset + 1] >> 4) * 4;
            return offset + header;
        }
        foreach (var control in good.Values)
        {
            var count = resolveGuard.Invoke(compilation, new[] { control.Method })!;
            int estimateOffset = BodyOffset((int)Get(control.Method, "Rva"));
            int countOffset = BodyOffset((int)Get(count, "Rva"));
            var countBytes = originalPe.GetMethodBody((int)Get(count, "Rva")).GetILBytes()!;
            Check(countBytes[0] == 0x28, "count-first-call");
            var helperToken = MetadataTokens.EntityHandle(BitConverter.ToInt32(countBytes, 1));
            var helper = Invoke(resolveSpec, core, helperToken, Get(count, "Context"))!;
            int helperOffset = BodyOffset((int)Get(helper, "Rva"));
            var helperBytes = originalPe.GetMethodBody((int)Get(helper, "Rva")).GetILBytes()!;
            var estimateBytes = originalPe.GetMethodBody((int)Get(control.Method, "Rva")).GetILBytes()!;
            Check(estimateBytes[55] == 0x28 && estimateBytes[56] != helperBytes[1], "estimate-count-target-mutation-changes-token");
            Check(countBytes[4] == 0x2b, "count-helper-methodspec-token");
            var mutations = new[]
            {
                (Offset: estimateOffset + 8, Value: (byte)0x2c, Name: "estimate-conditional-entry"),
                (Offset: estimateOffset + 9, Value: (byte)0, Name: "estimate-fallthrough"),
                (Offset: estimateOffset + 56, Value: helperBytes[1], Name: "estimate-count-target"),
                (Offset: countOffset + 4, Value: (byte)0x0a, Name: "count-helper-target-kind"),
                (Offset: countOffset, Value: (byte)0x6f, Name: "count-callvirt"),
                (Offset: helperOffset + 5, Value: (byte)0x2c, Name: "helper-inverted-condition"),
                (Offset: helperOffset + helperBytes.Length - 1, Value: (byte)0, Name: "helper-missing-ret"),
            };
            foreach (var mutation in mutations)
            {
                Check(ReferenceEquals(Invoke(resolve, app, control.Token, empty), control.Method), "il-good-before");
                var changed = (byte[])originalImage.Clone(); changed[mutation.Offset] = mutation.Value;
                using var modified = new System.Reflection.PortableExecutable.PEReader(new MemoryStream(changed, writable: false));
                Set(core, "PE", modified);
                try
                {
                    bool refused = false;
                    try { resolveGuard.Invoke(compilation, new[] { control.Method }); }
                    catch (TargetInvocationException error) when (error.InnerException is NotSupportedException) { refused = true; }
                    Check(refused, mutation.Name + ":guard-reject");
                    Check(Refused(resolve, app, control.Token, empty), mutation.Name + ":resolver-reject");
                }
                finally { Set(core, "PE", originalPe); }
                Check(ReferenceEquals(Invoke(resolve, app, control.Token, empty), control.Method) && Accepted(control.Method), "il-good-after");
            }
        }
        for (int i = 0; i < cases.Count; i++)
        {
            var item = cases[i]; var token = Token(methods[i]); var control = good[item.Element];
            var contexts = new[] { empty, Context(control.Element, false), Context(control.Element, true) };
            foreach (var context in contexts)
            foreach (var resolver in token.Kind == HandleKind.MethodSpecification ? new[] { resolve, resolveSpec, resolveCall } : new[] { resolveReference, resolveFunction, resolveCall })
            {
                Check(ReferenceEquals(Invoke(resolve, app, control.Token, empty), control.Method), "good-before");
                bool expected = item.Mutation == "valid" || item.Mutation == "valid-mvar" && ReferenceEquals(context, contexts[1]) || item.Mutation == "valid-var" && ReferenceEquals(context, contexts[2]);
                if (expected) Check(Accepted(Invoke(resolver, app, token, context)), item.Route + ":" + item.Mutation + ":accept");
                else Check(Refused(resolver, app, token, context), item.Route + ":" + item.Mutation + ":reject");
                Check(ReferenceEquals(Invoke(resolve, app, control.Token, empty), control.Method) && Accepted(control.Method), "good-after");
            }
            if (token.Kind == HandleKind.MethodSpecification)
            {
                var spec = reader.GetMethodSpecification((MethodSpecificationHandle)token);
                Console.WriteLine("generic-vector-estimate-serialized:" + item.Element + ":" + item.Route + ":" + item.Mutation + ":" + MetadataTokens.GetToken(token).ToString("X8") + ":raw=" + Convert.ToHexString(reader.GetBlobBytes(spec.Signature)));
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
            Poison(method, "Attributes", MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig);
            Poison(method, "ImplAttributes", MethodImplAttributes.NoInlining);
            Poison(method, "Context", empty);
            var vector = Get(Get(Get(method, "Signature"), "ReturnType"), "Class");
            var ownerContext = Get(vector, "Context");
            var ownerHandle = (TypeDefinitionHandle)Get(vector, "Handle");
            var bridge = coreReader.GetTypeDefinition(ownerHandle).GetMethods().Single(h => coreReader.GetString(coreReader.GetMethodDefinition(h).Name).EndsWith(".MultiplyAddEstimate", StringComparison.Ordinal));
            var pe = (System.Reflection.PortableExecutable.PEReader)Get(core, "PE");
            var il = pe.GetMethodBody(coreReader.GetMethodDefinition(bridge).RelativeVirtualAddress).GetILBytes()!;
            Check(il.Length == 9 && il[0] == 2 && il[1] == 3 && il[2] == 4 && il[3] == 0x28 && il[8] == 0x2a, "exact-retained-bridge-il");
            var bridgeToken = MetadataTokens.EntityHandle(BitConverter.ToInt32(il, 4));
            Check(bridgeToken.Kind == HandleKind.MethodSpecification && coreReader.GetMethodSpecification((MethodSpecificationHandle)bridgeToken).Method.Kind == HandleKind.MethodDefinition, "retained-methoddef-spec");
            foreach (var resolver in new[] { resolve, resolveSpec, resolveCall })
                Check(Accepted(Invoke(resolver, core, bridgeToken, ownerContext)), "retained-methoddef-resolver");
        }
        Console.WriteLine("generic-vector-estimate-guards:checks=" + checks.ToString(CultureInfo.InvariantCulture));
        return true;
    }
}
