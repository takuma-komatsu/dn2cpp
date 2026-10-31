using System.Collections;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

internal static class VectorUnsupportedGuards
{
    public static void Run(string compiler, string core, string input, string expected)
    {
        using var pe = new PEReader(File.OpenRead(input));
        var rawReader = pe.GetMetadataReader();
        string moduleName = rawReader.GetString(rawReader.GetModuleDefinition().Name);
        bool auxiliary = moduleName.StartsWith("VectorUnsupportedInput.", StringComparison.Ordinal);
        string mode = moduleName[(moduleName.IndexOf('.') + 1)..];
        if (auxiliary) VectorUnsupportedInputs.Verify(mode, input);
        else VectorUnsupportedValueShapeProof.Verify(mode, input);
        var production = Assembly.LoadFrom(Path.GetFullPath(compiler));
        const BindingFlags instance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        const BindingFlags statics = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        Type Model(string name) => production.GetType("Dn2Cpp." + name, true)!;
        object Get(object value, string name) => value.GetType().GetField(name, instance) is { } field ? field.GetValue(value)! : value.GetType().GetProperty(name, instance)!.GetValue(value)!;
        void Set(object value, string name, object? next)
        {
            if (value.GetType().GetField(name, instance) is { } field) field.SetValue(value, next);
            else value.GetType().GetProperty(name, instance)!.SetValue(value, next);
        }
        object Invoke(object? target, System.Reflection.MethodInfo method, params object[] values)
        {
            try { return method.Invoke(target, values)!; }
            catch (TargetInvocationException error) when (error.InnerException is not null) { throw error.InnerException; }
        }
        var options = Activator.CreateInstance(Model("TranspileOptions"))!;
        Set(options, "Input", Path.GetFullPath(input)); Set(options, "UseILDiet", false); Set(options, "SharedGenerics", false);
        Set(options, "AutoRef", true); Set(options, "IsaSurfaceDump", "predicate-guard-only");
        object loaded;
        try { loaded = Invoke(null, Model("Compilation").GetMethod("Create", statics)!, new[] { Path.GetFullPath(input), Path.GetFullPath(core) }, options); }
        catch (NotSupportedException error) when (expected == "reject" && mode.Contains("foreign", StringComparison.Ordinal)
            && error.Message.Contains("conflicts with a CoreLib/runtime type identity", StringComparison.Ordinal))
        {
            Console.WriteLine($"vector-unsupported-reference mode={mode} refused=input-identity-before-family feature-validator-executed=false");
            Console.WriteLine(error.Message); return;
        }
        var modules = ((IEnumerable)Get(loaded, "Modules")).Cast<object>().ToArray();
        var app = modules.Single(x => (string)Get(x, "AssemblyName") == (auxiliary ? "VectorUnsupportedInput" : "VectorUnsupportedValueGuardInput"));
        var reader = (MetadataReader)Get(app, "Reader");
        var context = Model("GenericContext").GetField("Empty", statics)!.GetValue(null)!;
        object Call(string name, params object[] values) => Invoke(loaded, Model("Compilation").GetMethod(name, instance)!, values);
        bool Predicate(string name, object target) => (bool)Invoke(null, Model("CoreIntrinsics").GetMethod(name, statics)!, target);
        int checks = 0;
        void Check(bool success, string label) { if (!success) throw new InvalidOperationException("vector-unsupported: " + label); checks++; }
        if (mode == "helper-context-mvar")
        {
            var token = MetadataTokens.MethodSpecificationHandle(1);
            object Element(string name)
            {
                var handle = reader.TypeReferences.Single(h => reader.StringComparer.Equals(reader.GetTypeReference(h).Namespace, "System")
                    && reader.StringComparer.Equals(reader.GetTypeReference(h).Name, name));
                var type = Call("ResolveTypeTokenForScan", app, (EntityHandle)handle, context);
                if (Get(type, "Class") is { } cls) Call("EnsureShape", cls);
                return type;
            }
            object Caller(object element)
            {
                var empty = Array.CreateInstance(Model("TypeDesc"), 0);
                var arguments = Array.CreateInstance(Model("TypeDesc"), 1); arguments.SetValue(element, 0);
                return Activator.CreateInstance(Model("GenericContext"), new object[] { empty, arguments })!;
            }
            var decimalType = Element("Decimal"); var int128Type = Element("Int128"); var stringType = Element("String");
            var good = Caller(decimalType); var other = Caller(int128Type); var excluded = Caller(stringType);
            var first = Call("TryResolveVectorManagedThrowSpec", app, token, good);
            Check(first is not null && Predicate("IsVectorManagedThrowHelper", first), "context-decimal-helper");
            Check(ReferenceEquals(first, Call("ResolveMethodSpec", app, token, good)), "context-real-resolver-identity");
            var second = Call("TryResolveVectorManagedThrowSpec", app, token, other);
            Check(second is not null && Predicate("IsVectorManagedThrowHelper", second) && !ReferenceEquals(first, second), "context-other-admitted-closure");
            Check(ReferenceEquals(((Array)Get(Get(first, "Context"), "MethodArgs")).GetValue(0), decimalType), "context-exact-decimal");
            Check(ReferenceEquals(((Array)Get(Get(second, "Context"), "MethodArgs")).GetValue(0), int128Type), "context-exact-int128");
            Check(Call("TryResolveVectorManagedThrowSpec", app, token, excluded) is null, "context-excluded-reference-not-cached");
            Check(ReferenceEquals(first, Call("TryResolveVectorManagedThrowSpec", app, token, good)), "context-good-after-excluded");
            Console.WriteLine($"vector-unsupported-helper-context same-token=true admitted-distinct=true excluded-reference=support-boundary good-other-excluded-good=true checks={checks}");
            return;
        }
        if (mode == "mvar-context")
        {
            var token = reader.MemberReferences.Single(h => reader.StringComparer.Equals(reader.GetMemberReference(h).Name, "Equals"));
            var ownerType = Call("ResolveTypeTokenForScan", app, reader.GetMemberReference(token).Parent, context);
            var owner = Get(ownerType, "Class"); Call("EnsureShape", owner);
            var element = ((Array)Get(Get(owner, "Context"), "TypeArgs")).GetValue(0)!;
            var empty = Array.CreateInstance(Model("TypeDesc"), 0);
            object Caller(object argument)
            {
                var args = Array.CreateInstance(Model("TypeDesc"), 1); args.SetValue(argument, 0);
                return Activator.CreateInstance(Model("GenericContext"), new object[] { empty, args })!;
            }
            var good = Caller(ownerType); var bad = Caller(element);
            var first = Call("ResolveMemberRefMethod", app, token, good);
            Check(Predicate("IsVectorValueCallableMethod", first) && !Predicate("IsVectorValueMethod", first), "managed-context-first");
            try { Call("ResolveMemberRefMethod", app, token, bad); throw new InvalidOperationException("Same-token wrong context admitted"); }
            catch (NotSupportedException error) when (error.Message.Contains("Vector<T> value methods", StringComparison.Ordinal)) { checks++; }
            var restored = Call("ResolveMemberRefMethod", app, token, good);
            Check(ReferenceEquals(first, restored), "same-token-restored-managed-method");
            Console.WriteLine($"vector-unsupported-context same-token=true same-owner=true good-bad-good=true checks={checks}");
            return;
        }
        var receivers = new Dictionary<int, EntityHandle>();
        foreach (var definitionHandle in reader.MethodDefinitions)
        {
            var definition = reader.GetMethodDefinition(definitionHandle);
            if (definition.RelativeVirtualAddress == 0) continue;
            var il = pe.GetMethodBody(definition.RelativeVirtualAddress).GetILBytes()!;
            foreach (int offset in VectorUnsupportedValueShapeProof.InstructionOffsets(il))
                if (offset + 10 < il.Length && il[offset] == 0xfe && il[offset + 1] == 0x16 && il[offset + 6] == 0x6f)
                    receivers[BitConverter.ToInt32(il, offset + 7)] = MetadataTokens.EntityHandle(BitConverter.ToInt32(il, offset + 2));
        }
        var specs = Enumerable.Range(1, reader.GetTableRowCount(TableIndex.MethodSpec)).Select(MetadataTokens.MethodSpecificationHandle).ToArray();
        var members = reader.MemberReferences.Where(h => reader.GetString(reader.GetMemberReference(h).Name) is "Equals" or "GetHashCode" or "get_Count").ToArray();
        Check(specs.Length + members.Length > 0, "target-present");
        if (mode is "helper-owner-class-spec" or "helper-owner-modopt-spec" or "helper-owner-modreq-spec")
        {
            Check(expected == "reject" && specs.Length == 1, "exact-owner-negative-contract");
            foreach (string resolver in new[] { "ResolveCallTarget", "ResolveMethodSpec" })
            {
                try
                {
                    if (resolver == "ResolveCallTarget") Call(resolver, app, (EntityHandle)specs[0], context);
                    else Call(resolver, app, specs[0], context);
                    throw new InvalidOperationException("Helper TypeSpec owner admitted or silently cut by " + resolver);
                }
                catch (NotSupportedException error) when (error.Message == "Vector unsupported-type guard requires its exact closed CoreLib declaration and unmodified MethodSpec")
                {
                    checks++;
                    Console.WriteLine($"vector-unsupported-reference mode={mode} route={resolver} refused=feature-specific classification=unsupported-owner-spelling");
                }
            }
            Console.WriteLine($"vector-unsupported-guards mode={mode} expected=reject checks={checks}");
            return;
        }
        foreach (EntityHandle token in specs.Length > 0 ? specs.Select(h => (EntityHandle)h) : members.Select(h => (EntityHandle)h))
        {
            try
            {
                object result;
                if (token.Kind == HandleKind.MethodSpecification)
                {
                    if (mode == "helper-owner-spec")
                        result = Call("ResolveCallTarget", app, token, context);
                    else if (mode.StartsWith("helper-", StringComparison.Ordinal))
                    {
                        result = Call("TryResolveVectorManagedThrowSpec", app, (MethodSpecificationHandle)token, context);
                        Check(result is not null, "actual-candidate-route-must-not-fall-through");
                        var direct = Call("ResolveMethodSpec", app, (MethodSpecificationHandle)token, context);
                        Check(ReferenceEquals(result, direct), "candidate-and-direct-same-managed-target");
                    }
                    else result = Call("ResolveMethodSpec", app, (MethodSpecificationHandle)token, context);
                }
                else
                {
                    var handle = (MemberReferenceHandle)token;
                    string name = reader.GetString(reader.GetMemberReference(handle).Name);
                    if (receivers.TryGetValue(MetadataTokens.GetToken(handle), out var receiverHandle))
                    {
                        var desc = Call("ResolveTypeTokenForScan", app, receiverHandle, context);
                        var receiver = Get(desc, "Class");
                        Call("EnsureShape", receiver);
                        Call("ValidateVectorValueReceiver", app, receiverHandle, context, receiver);
                        result = Call("ValidateVectorValueConstrained", app, (EntityHandle)handle, context, receiver);
                    }
                    else result = Call(name == "get_Count" ? "ResolveVectorManagedCountReference" : "ResolveMemberRefMethod", app, handle, context);
                }
                Check(expected == "accept", "unexpected-admission mode=" + mode);
                bool guard = token.Kind == HandleKind.MethodSpecification;
                string predicate = guard ? "IsVectorManagedThrowHelper" : (string)Get(result, "Name") == "get_Count" ? "IsVectorManagedCount" : "IsVectorValueCallableMethod";
                if (!guard && Predicate("IsVectorValueCallableEquatableMethod", result)) predicate = "IsVectorValueCallableEquatableMethod";
                Check(Predicate(predicate, result), "exact-callable-declaration");
                if (VectorUnsupportedFamily.Width != 0)
                {
                    if (guard) Check(Predicate("IsFixedVectorManagedThrowHelper", result), "separate-fixed-guard-family");
                    else Check(Predicate("IsFixedVectorManagedValueClass", Get(result, "DeclaringClass")), "separate-fixed-managed-owner");
                }
                Check(!Predicate("IsVectorValueMethod", result), "not-numeric-lane-method");
                var owner = Get(result, "DeclaringClass");
                if (!guard && (string)Get(owner, "FullName") == VectorUnsupportedFamily.OwnerName)
                {
                    Check(Predicate("IsVectorManagedValueClass", owner), "unmapped-owner");
                    Check(Get(owner, "IntrinsicCppName") is null, "not-intrinsic-mapped");
                }
                void RejectMutation(object value, string field, object next)
                {
                    object saved = Get(value, field); Set(value, field, next);
                    try { Check(!Predicate(predicate, result), "model-mutation-" + field); }
                    finally { Set(value, field, saved); }
                }
                if (predicate != "IsVectorValueCallableEquatableMethod") RejectMutation(result, "Rva", 0);
                RejectMutation(result, "IsSynthetic", true);
                RejectMutation(result, "Module", app);
                RejectMutation(result, "ImplAttributes", MethodImplAttributes.InternalCall);
                Check(Predicate(predicate, result), "restored-declaration");
                Console.WriteLine($"vector-unsupported-reference mode={mode} accepted={MetadataTokens.GetToken(token):X8} numeric=false");
            }
            catch (NotSupportedException error) when (expected == "reject" && mode == "helper-extra"
                && error.Message == $"System.ThrowHelper: no generic method {VectorUnsupportedFamily.HelperName}<1>/1")
            {
                checks++;
                Console.WriteLine($"vector-unsupported-reference mode={mode} refused=member-resolution-before-feature-validator feature-validator-executed=false classification=unresolved-member-signature");
            }
            catch (NotSupportedException error) when (expected == "reject" && mode == "helper-owner-spec"
                && error.Message == "Generic instantiation of System.ThrowHelper is not supported yet (external generic types)")
            {
                checks++;
                Console.WriteLine($"vector-unsupported-reference mode={mode} route=ResolveCallTarget refused=external-generic-owner-boundary feature-validator-executed=false classification=illegal-generic-owner-shape");
            }
            catch (NotSupportedException error) when (expected != "accept" &&
                (error.Message.Contains("Vector<T> value methods", StringComparison.Ordinal) || error.Message.Contains("Vector unsupported-type guard", StringComparison.Ordinal)))
            {
                checks++;
                Console.WriteLine($"vector-unsupported-reference mode={mode} refused=feature-specific classification={(expected == "scope" ? "support-boundary" : mode == "helper-foreign-owner" ? "foreign-source-identity" : "rejected-call/declaration-shape")}");
            }
        }
        Console.WriteLine($"vector-unsupported-guards mode={mode} expected={expected} checks={checks}");
    }
}
