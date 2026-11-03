using System.Collections;
using System.Collections.Immutable;
using System.Globalization;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using Cecil = Mono.Cecil;
using Cil = Mono.Cecil.Cil;

internal static class ArrayNativeLengthGuards
{
    private delegate bool RawPredicate(BlobReader blob);
    private const bool ArrayLeaf = true;
    private const string OwnerName = "System.Array";
    private const string MethodName = "get_NativeLength";
    private const string ModelName = "ArrayNativeLengthGuardInput";
    private const string Label = "array-native-length";
    private static readonly byte[] ExactSignature = Convert.FromHexString("200019");

    public static bool Run(string[] args)
    {
        if (args.Length != 4 || args[0] != "--array-native-length-guards") return false;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        string input = Path.GetFullPath(args[3]), corePath = Path.GetFullPath(args[2]);
        if (File.Exists(input)) throw new InvalidOperationException("guard metadata output must be new");
        WriteReferences(corePath, input);
        var production = Assembly.LoadFrom(Path.GetFullPath(args[1]));
        const BindingFlags instance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        const BindingFlags statics = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        Type Model(string name) => production.GetType("Dn2Cpp." + name, true)!;
        object Get(object value, string name) => value.GetType().GetField(name, instance) is { } field
            ? field.GetValue(value)! : value.GetType().GetProperty(name, instance)!.GetValue(value)!;
        void Set(object value, string name, object? next)
        {
            if (value.GetType().GetField(name, instance) is { } field) field.SetValue(value, next);
            else value.GetType().GetProperty(name, instance)!.SetValue(value, next);
        }
        object Invoke(System.Reflection.MethodInfo method, object? target, params object?[] values)
        {
            try { return method.Invoke(target, values)!; }
            catch (TargetInvocationException error) when (error.InnerException is not null) { throw error.InnerException; }
        }
        object NewCompilation()
        {
            var options = Activator.CreateInstance(Model("TranspileOptions"))!;
            Set(options, "Input", input); Set(options, "UseILDiet", false); Set(options, "SharedGenerics", false);
            Set(options, "AutoRef", false); Set(options, "IsaSurfaceDump", "predicate-guard-only");
            return Invoke(Model("Compilation").GetMethod("Create", statics)!, null, new[] { input, corePath }, options);
        }
        object Module(object compilation, string name) => ((IEnumerable)Get(compilation, "Modules")).Cast<object>()
            .Single(module => (string)Get(module, "AssemblyName") == name);
        TypeDefinitionHandle TypeHandle(MetadataReader reader, string name) => reader.TypeDefinitions.Single(handle =>
        {
            var definition = reader.GetTypeDefinition(handle);
            string fullName = reader.GetString(definition.Namespace) + "." + reader.GetString(definition.Name);
            return fullName == name && definition.GetDeclaringType().IsNil;
        });
        object Target(object core)
        {
            var metadata = (MetadataReader)Get(core, "Reader");
            var handle = metadata.GetTypeDefinition(TypeHandle(metadata, OwnerName)).GetMethods().Single(candidate =>
                metadata.GetString(metadata.GetMethodDefinition(candidate).Name) == MethodName);
            return ((IDictionary)Get(core, "MethodMap"))[handle]!;
        }
        int Classes(object compilation) => ((IEnumerable)Get(compilation, "Classes")).Cast<object>().Count();
        bool Has(object compilation, string field, object method) => ((IEnumerable)Get(compilation, field))
            .Cast<object>().Any(candidate => ReferenceEquals(candidate, method));
        var compilation = NewCompilation();
        var core = Module(compilation, "System.Private.CoreLib");
        var app = Module(compilation, ModelName);
        var reader = (MetadataReader)Get(core, "Reader");
        var actual = Target(core);
        var owner = Get(actual, "DeclaringClass");
        var handle = (MethodDefinitionHandle)Get(actual, "Handle");
        var ownerHandle = (TypeDefinitionHandle)Get(owner, "Handle");
        var definition = reader.GetMethodDefinition(handle);
        var intrinsic = Model("CoreIntrinsics");
        var predicate = intrinsic.GetMethod("IsArrayNativeLength", statics)!;
        var raw = intrinsic.GetMethod("IsArrayNativeLengthSignature", statics)!.CreateDelegate<RawPredicate>();
        var decoded = intrinsic.GetMethod("IsArrayNativeLengthDecodedSignature", statics)!;
        var row = intrinsic.GetField("BrArrayNativeLength", statics)!.GetValue(null)!;
        bool Accepted(object method) => (bool)Invoke(predicate, null, method);
        bool Matches(object method) => (bool)Invoke(row.GetType().GetMethod("Matches", instance)!, row, method);
        int checks = 0;
        void Check(bool condition, string label)
        {
            if (!condition) throw new InvalidOperationException(Label + " guard: " + label);
            checks++;
        }
        Check(Get(row, "CutKind").ToString() == "BodyReplace" && Get(row, "EmitArm").ToString() == "ArrayNativeLengthBody",
            "body-replacement-row");
        Check(definition.Attributes == (MethodAttributes)0x0883 && definition.ImplAttributes == (MethodImplAttributes)0x0000
            && definition.RelativeVirtualAddress != 0 && definition.GetDeclaringType() == ownerHandle
            && definition.GetGenericParameters().Count == 0, "genuine-declaration-flags-owner-rva");
        Check(reader.GetBlobBytes(definition.Signature).SequenceEqual(ExactSignature)
            && raw(reader.GetBlobReader(definition.Signature)), "genuine-raw-ABI");
        Console.WriteLine(Label + "-actual-model:" + MetadataTokens.GetToken(handle).ToString("X8", CultureInfo.InvariantCulture)
            + ":signature=" + Convert.ToHexString(ExactSignature));

        var signatureType = Model("MethodInfo").GetProperty("Signature", instance)!.PropertyType;
        Set(actual, "_sig", Activator.CreateInstance(signatureType));
        int coldClasses = Classes(compilation);
        for (int pass = 0; pass < 2; pass++)
        {
            Check(!(bool)Get(actual, "SignatureReady"), "cold-before-" + pass);
            Check(Accepted(actual) && Matches(actual), "cold-exact-match-" + pass);
            Check(!(bool)Get(actual, "SignatureReady") && Classes(compilation) == coldClasses, "cold-pure-" + pass);
        }
        object Primitive(PrimitiveTypeCode code) => Invoke(Model("TypeDesc").GetMethod("MakePrimitive", statics)!, null, code);
        var argument = Array.CreateInstance(Model("TypeDesc"), 1);
        argument.SetValue(Primitive(ArrayLeaf ? PrimitiveTypeCode.UIntPtr : PrimitiveTypeCode.IntPtr), 0);
        var empty = Array.CreateInstance(Model("TypeDesc"), 0);
        object Context(bool type) => Activator.CreateInstance(Model("GenericContext"),
            new object[] { type ? argument : empty, type ? empty : argument })!;
        void Reject(object value, string field, object? replacement, string label)
        {
            object saved = Get(value, field); bool ready = (bool)Get(actual, "SignatureReady");
            int before = Classes(compilation); Set(value, field, replacement);
            try
            {
                Check(!Accepted(actual) && !Matches(actual), "reject-" + label);
                Check((bool)Get(actual, "SignatureReady") == ready && Classes(compilation) == before, "pure-" + label);
            }
            finally { Set(value, field, saved); }
            Check(Accepted(actual) && Matches(actual) && (bool)Get(actual, "SignatureReady") == ready, "restored-" + label);
        }
        foreach (string state in new[] { "cold", "warm" })
        {
            if (state == "warm") Check((bool)Invoke(decoded, null, Get(actual, "Signature")), "decoded-genuine");
            Reject(core, "AssemblyName", "Foreign.CoreLib", state + "-assembly-name");
            Reject(owner, "Module", app, state + "-owner-module");
            Reject(owner, "_fullName", "Application.Lookalike", state + "-owner-name");
            Reject(owner, "Handle", TypeHandle(reader, "System.Object"), state + "-owner-definition");
            Reject(owner, "Handle", default(TypeDefinitionHandle), state + "-owner-nil");
            Reject(owner, "Handle", MetadataTokens.TypeDefinitionHandle(reader.TypeDefinitions.Count + 1), state + "-owner-out-of-range");
            Reject(owner, "IsValueType", ArrayLeaf, state + "-owner-kind");
            Reject(owner, "IsEnum", true, state + "-owner-enum");
            Reject(owner, "IsInterface", true, state + "-owner-interface");
            Reject(owner, "IsByRefLike", true, state + "-owner-byreflike");
            Reject(owner, "GenericArity", 1, state + "-owner-arity");
            Reject(owner, "Context", Context(true), state + "-owner-type-context");
            Reject(owner, "Context", Context(false), state + "-owner-method-context");
            Reject(actual, "Module", app, state + "-method-module");
            Reject(actual, "Name", MethodName + "Other", state + "-method-name");
            Reject(actual, "Handle", default(MethodDefinitionHandle), state + "-method-nil");
            Reject(actual, "Handle", MetadataTokens.MethodDefinitionHandle(reader.MethodDefinitions.Count + 1), state + "-method-out-of-range");
            var neighbor = reader.GetTypeDefinition(ownerHandle).GetMethods().First(candidate => candidate != handle);
            Reject(actual, "Handle", neighbor, state + "-raw-other-definition");
            Reject(actual, "Rva", 0, state + "-bodyless");
            Reject(actual, "Rva", definition.RelativeVirtualAddress + 1, state + "-raw-model-rva-disagreement");
            Reject(actual, "IsSynthetic", true, state + "-synthetic");
            Reject(actual, "Context", Context(true), state + "-type-context");
            Reject(actual, "Context", Context(false), state + "-method-context");
            Reject(actual, "PInvoke", RuntimeHelpers.GetUninitializedObject(Model("PInvokeInfo")), state + "-pinvoke");
            foreach (var mask in new[] { MethodAttributes.Static, MethodAttributes.Public, MethodAttributes.Virtual,
                MethodAttributes.Abstract, MethodAttributes.PinvokeImpl, MethodAttributes.SpecialName, MethodAttributes.HideBySig })
                Reject(actual, "Attributes", definition.Attributes ^ mask, state + "-attributes-" + mask);
            foreach (var mask in new[] { MethodImplAttributes.InternalCall, MethodImplAttributes.Runtime, MethodImplAttributes.NoInlining,
                MethodImplAttributes.Synchronized, MethodImplAttributes.PreserveSig, MethodImplAttributes.AggressiveInlining })
                Reject(actual, "ImplAttributes", definition.ImplAttributes ^ mask, state + "-implementation-" + mask);
        }

        bool Raw(byte[] bytes)
        {
            var metadata = new MetadataBuilder();
            metadata.AddModule(0, metadata.GetOrAddString(Label + "Raw"), metadata.GetOrAddGuid(Guid.Empty), default, default);
            var blob = metadata.GetOrAddBlob(bytes);
            var image = new BlobBuilder(); new MetadataRootBuilder(metadata).Serialize(image, 0, 0);
            using var provider = MetadataReaderProvider.FromMetadataImage(image.ToImmutableArray());
            return raw(provider.GetMetadataReader().GetBlobReader(blob));
        }
        int rawClasses = Classes(compilation);
        Check(Raw(ExactSignature), "raw-exact");
        for (int count = 0; count < ExactSignature.Length; count++)
            Check(!Raw(ExactSignature[..count]), "raw-truncated-" + count);
        for (int offset = 0; offset < ExactSignature.Length; offset++)
        for (int value = 0; value <= byte.MaxValue; value++)
        {
            if (value == ExactSignature[offset]) continue;
            var bytes = (byte[])ExactSignature.Clone(); bytes[offset] = (byte)value;
            Check(!Raw(bytes), "raw-single-byte-" + offset + "-" + value);
        }
        foreach (byte tail in new byte[] { 0, 0x41, 0xff })
            Check(!Raw(ExactSignature.Concat(new[] { tail }).ToArray()), "raw-trailing-" + tail);
        foreach (string hex in new[] { "20001019", "20000F19", "20001D19", "20001F0519", "2000200519", "20001205", "20800019", "30000019", "20011E00" })
            Check(!Raw(Convert.FromHexString(hex)), "raw-structured-near-miss-" + hex);
        Check(Classes(compilation) == rawClasses, "raw-no-model-growth");

        var signature = Get(actual, "Signature");
        var createArray = typeof(ImmutableArray).GetMethods().Single(method => method.Name == "Create"
            && method.IsGenericMethodDefinition && method.GetParameters().Length == 1 && method.GetParameters()[0].ParameterType.IsArray)
            .MakeGenericMethod(Model("TypeDesc"));
        object Signature(byte header, object result, int required, int generic, params object[] parameters)
        {
            var array = Array.CreateInstance(Model("TypeDesc"), parameters.Length);
            for (int i = 0; i < parameters.Length; i++) array.SetValue(parameters[i], i);
            return Activator.CreateInstance(signature.GetType(), new object[] { new SignatureHeader(header), result,
                required, generic, Invoke(createArray, null, array) })!;
        }
        byte exactHeader = ArrayLeaf ? (byte)0x20 : (byte)0;
        int exactCount = ArrayLeaf ? 0 : 1;
        var resultType = Primitive(ArrayLeaf ? PrimitiveTypeCode.UIntPtr : PrimitiveTypeCode.Void);
        object[] parameterTypes = ArrayLeaf ? Array.Empty<object>() : new[] { Primitive(PrimitiveTypeCode.IntPtr) };
        void WrongSignature(object next, string label)
        {
            Set(actual, "_sig", next); int before = Classes(compilation);
            try
            {
                Check((bool)Get(actual, "SignatureReady") && !(bool)Invoke(decoded, null, next), "decoded-reject-" + label);
                Check(!Accepted(actual) && !Matches(actual) && Classes(compilation) == before, "warm-disagreement-" + label);
            }
            finally { Set(actual, "_sig", signature); }
            Check(Accepted(actual) && Matches(actual), "warm-restored-" + label);
        }
        foreach (byte header in new byte[] { (byte)(exactHeader ^ 0x20), 0x60, 0x05, 0x10, 0x30 })
            WrongSignature(Signature(header, resultType, exactCount, 0, parameterTypes), "header-" + header);
        WrongSignature(Signature(exactHeader, resultType, exactCount + 1, 0, parameterTypes), "required-count");
        WrongSignature(Signature(exactHeader, resultType, exactCount, 1, parameterTypes), "generic-count");
        WrongSignature(Signature(exactHeader, Primitive(PrimitiveTypeCode.Int64), exactCount, 0, parameterTypes), "return-type");
        WrongSignature(Signature(exactHeader, resultType, exactCount + 1, 0,
            parameterTypes.Concat(new[] { Primitive(PrimitiveTypeCode.Int32) }).ToArray()), "extra-parameter");
        if (!ArrayLeaf)
        {
            WrongSignature(Signature(exactHeader, resultType, 0, 0), "missing-parameter");
            WrongSignature(Signature(exactHeader, resultType, 1, 0, Primitive(PrimitiveTypeCode.UIntPtr)), "unsigned-token");
        }
        for (int slot = 0; slot <= parameterTypes.Length; slot++)
        foreach (string kind in new[] { "ByRef", "Pointer", "SZArray", "Class", "External", "Canon" })
        {
            var changed = RuntimeHelpers.GetUninitializedObject(Model("TypeDesc"));
            var original = slot == 0 ? resultType : parameterTypes[slot - 1];
            Set(changed, "Kind", Enum.Parse(Model("TypeKind"), kind == "Canon" ? "Primitive" : kind));
            Set(changed, "Primitive", Get(original, "Primitive")); Set(changed, "Element", original);
            Set(changed, "IsCanonPlaceholder", kind == "Canon");
            if (kind is "Class" or "External")
            {
                string fullName = slot == 0 ? (ArrayLeaf ? "System.UIntPtr" : "System.Void") : "System.IntPtr";
                Set(changed, "Class", ((IDictionary)Get(core, "ClassMap"))[TypeHandle(reader, fullName)]);
                Set(changed, "ExternalName", fullName); Set(changed, "ExternalModule", core);
                Set(changed, "ExternalAssembly", "System.Private.CoreLib");
            }
            var parameters = (object[])parameterTypes.Clone(); if (slot != 0) parameters[slot - 1] = changed;
            WrongSignature(Signature(exactHeader, slot == 0 ? changed : resultType, exactCount, 0, parameters), "slot-" + slot + "-" + kind);
        }

        var emptyContext = Model("GenericContext").GetField("Empty", statics)!.GetValue(null)!;
        var call = Model("Compilation").GetMethod("ResolveCallTarget", instance)!;
        var member = Model("Compilation").GetMethod("ResolveMemberRefMethod", instance, null,
            new[] { Model("Module"), typeof(MemberReferenceHandle), Model("GenericContext") }, null)!;
        var tryMember = Model("Compilation").GetMethod("TryResolveMemberRefMethod", instance)!;
        var function = Model("Compilation").GetMethod("ResolveFunctionMemberRef", instance)!;
        Check(ReferenceEquals(Invoke(call, compilation, core, (EntityHandle)handle, emptyContext), actual), "MethodDef-exact-body-route");
        using (var references = Cecil.AssemblyDefinition.ReadAssembly(input))
        {
            foreach (var holder in references.MainModule.GetType("Fixture.References`1").Methods)
            {
                var reference = (Cecil.MethodReference)holder.Body.Instructions.Single(instruction => instruction.OpCode == Cil.OpCodes.Ldftn).Operand;
                Check(reference.MetadataToken.TokenType == Cecil.TokenType.MemberRef, "MemberRef-token-" + holder.Name);
                var token = MetadataTokens.MemberReferenceHandle((int)reference.MetadataToken.RID);
                var context = holder.Name == "type-context" ? Context(true) : holder.Name == "method-context" ? Context(false) : emptyContext;
                foreach (var resolver in new[] { call, member, tryMember, function })
                {
                    object? resolved = null;
                    try { resolved = Invoke(resolver, compilation, app, resolver == call ? (EntityHandle)token : (object)token, context); }
                    catch (NotSupportedException) when (holder.Name != "exact") { }
                    catch (BadImageFormatException) when (holder.Name != "exact") { }
                    bool admitted = resolved is not null && Accepted(resolved) && Matches(resolved);
                    Check(admitted == (holder.Name == "exact"), resolver.Name + "-" + holder.Name);
                    if (admitted) Check(ReferenceEquals(resolved, actual) && ReferenceEquals(Get(resolved!, "Module"), core)
                        && !(bool)Get(resolved!, "IsSynthetic"), "original-MethodDef-identity-" + resolver.Name);
                    Check(Accepted(actual) && Matches(actual), "cache-not-poisoned-" + resolver.Name + "-" + holder.Name);
                }
            }
        }

        // Each route starts in a new, stopped-before-reachability model. No direct
        // call or address root can rescue the reflection-only target-root test.
        foreach (string route in new[] { "reflection-only", "direct", "address-calli" })
        {
            var fresh = NewCompilation(); var freshCore = Module(fresh, "System.Private.CoreLib");
            var target = Target(freshCore);
            Check(!Has(fresh, "Reachable", target) && !Has(fresh, "IntrinsicFtnTargets", target), route + "-unrooted");
            if (!ArrayLeaf)
                Check(Get(Get(target, "DeclaringClass"), "IntrinsicCppName") is not null, route + "-mapped-owner");
            else
                Check((string)Get(Get(target, "DeclaringClass"), "FullName") == OwnerName
                    && Accepted(target) && Matches(target), route + "-exact-owned-array-descriptor");
            var runtimeSymbol = intrinsic.GetMethod("RuntimeTypeInfoSymbol", statics, null, new[] { Model("ClassInfo") }, null)!;
            Check(Invoke(runtimeSymbol, null, Get(target, "DeclaringClass")) is null, route + "-generated-typeinfo-owner");
            if (route == "reflection-only")
            {
                var freshApp = Module(fresh, ModelName); var appReader = (MetadataReader)Get(freshApp, "Reader");
                var callerHandle = appReader.MethodDefinitions.Single(candidate =>
                    appReader.GetString(appReader.GetMethodDefinition(candidate).Name) == "ReflectionOnlyCaller");
                var caller = ((IDictionary)Get(freshApp, "MethodMap"))[callerHandle]!;
                Invoke(Model("Compilation").GetMethod("InvokeTarget", instance)!, fresh, target, caller);
            }
            else if (route == "direct") Invoke(Model("Compilation").GetMethod("Reach", instance)!, fresh, target);
            else
            {
                var note = Model("Compilation").GetMethod("NoteIntrinsicFtnTarget", instance)!;
                Invoke(note, fresh, target); Invoke(note, fresh, target);
            }
            Check(Has(fresh, "Reachable", target) && !Has(fresh, "IntrinsicFtnTargets", target)
                && !Has(fresh, "InterceptFtnTargets", target), route + "-replacement-not-synthetic-wrapper");
            Check(!Has(fresh, "_toScan", target) && !Has(fresh, "_scanned", target), route + "-no-original-IL-scan");
            var metadataType = Model("CppEmitter").GetNestedType("TypeMetadataEmitter", BindingFlags.NonPublic)!;
            var metadata = RuntimeHelpers.GetUninitializedObject(metadataType); Set(metadata, "_c", fresh);
            var retained = metadataType.GetMethod("IsRetainedOpaqueMemberRow", instance)!;
            Check((bool)Invoke(retained, metadata, target), route + "-retained-row-eligibility");
            var saved = Get(target, "Attributes"); Set(target, "Attributes", (MethodAttributes)saved ^ MethodAttributes.Virtual);
            try { Check(!(bool)Invoke(retained, metadata, target), route + "-near-miss-row-ineligible"); }
            finally { Set(target, "Attributes", saved); }
        }
        Console.WriteLine(Label + "-routes:MethodDef;MemberRef(scanner,direct,try,function);address-calli;reflection-only-target-root;retained-row-eligibility");
        Console.WriteLine(Label + "-scope:named-generated-row-attachment-requires-emitted-and-runtime-checks;no-native-pass-claim");
        Console.WriteLine(Label + "-guards:checks=" + checks);
        return true;
    }

    private static void WriteReferences(string corePath, string output)
    {
        using var core = Cecil.AssemblyDefinition.ReadAssembly(corePath);
        if (core.Name.Name != "System.Private.CoreLib") throw new InvalidOperationException("Expected original CoreLib PE");
        using var app = Cecil.AssemblyDefinition.CreateAssembly(new Cecil.AssemblyNameDefinition(ModelName, new Version(1, 0)), ModelName, Cecil.ModuleKind.Dll);
        var module = app.MainModule;
        var host = new Cecil.TypeDefinition("Fixture", "References`1", Cecil.TypeAttributes.Public, module.TypeSystem.Object);
        module.Types.Add(host); host.GenericParameters.Add(new Cecil.GenericParameter("T", host));
        var original = core.MainModule.GetType(OwnerName).Methods.Single(method => method.Name == MethodName);
        var mutations = new List<string> { "exact", "this", "explicit-this", "vararg", "generic", "wrong-return", "extra",
            "modified-return", "foreign-owner", "wrong-owner", "wrong-name", "type-context", "method-context" };
        if (ArrayLeaf) mutations.AddRange(new[] { "byref-return", "pointer-return", "array-return" });
        else mutations.AddRange(new[] { "missing", "wrong-parameter", "byref-parameter", "pointer-parameter", "array-parameter", "modified-parameter" });
        foreach (string mutation in mutations)
        {
            var holder = new Cecil.MethodDefinition(mutation, Cecil.MethodAttributes.Public | Cecil.MethodAttributes.Static, module.TypeSystem.Void);
            host.Methods.Add(holder); holder.GenericParameters.Add(new Cecil.GenericParameter("U", holder));
            var target = new Cecil.MethodReference(MethodName, module.ImportReference(original.ReturnType), module.ImportReference(original.DeclaringType))
                { HasThis = ArrayLeaf };
            foreach (var parameter in original.Parameters) target.Parameters.Add(new Cecil.ParameterDefinition(module.ImportReference(parameter.ParameterType)));
            switch (mutation)
            {
                case "this": target.HasThis = !ArrayLeaf; break;
                case "explicit-this": target.HasThis = true; target.ExplicitThis = true; break;
                case "vararg": target.CallingConvention = Cecil.MethodCallingConvention.VarArg; break;
                case "generic": target.GenericParameters.Add(new Cecil.GenericParameter("G", target)); break;
                case "wrong-return": target.ReturnType = module.TypeSystem.Int64; break;
                case "extra": target.Parameters.Add(new Cecil.ParameterDefinition(module.TypeSystem.Int32)); break;
                case "modified-return": target.ReturnType = new Cecil.OptionalModifierType(module.TypeSystem.Int32, target.ReturnType); break;
                case "byref-return": target.ReturnType = new Cecil.ByReferenceType(target.ReturnType); break;
                case "pointer-return": target.ReturnType = new Cecil.PointerType(target.ReturnType); break;
                case "array-return": target.ReturnType = new Cecil.ArrayType(target.ReturnType); break;
                case "missing": target.Parameters.Clear(); break;
                case "wrong-parameter": target.Parameters[0].ParameterType = module.TypeSystem.UIntPtr; break;
                case "byref-parameter": target.Parameters[0].ParameterType = new Cecil.ByReferenceType(target.Parameters[0].ParameterType); break;
                case "pointer-parameter": target.Parameters[0].ParameterType = new Cecil.PointerType(target.Parameters[0].ParameterType); break;
                case "array-parameter": target.Parameters[0].ParameterType = new Cecil.ArrayType(target.Parameters[0].ParameterType); break;
                case "modified-parameter": target.Parameters[0].ParameterType = new Cecil.RequiredModifierType(module.TypeSystem.Int32, target.Parameters[0].ParameterType); break;
                case "type-context":
                case "method-context":
                    var variable = mutation == "type-context" ? host.GenericParameters[0] : holder.GenericParameters[0];
                    if (ArrayLeaf) target.ReturnType = variable; else target.Parameters[0].ParameterType = variable;
                    break;
                case "foreign-owner":
                    var scope = new Cecil.AssemblyNameReference("Foreign.Leaf", new Version(1, 0)); module.AssemblyReferences.Add(scope);
                    target.DeclaringType = new Cecil.TypeReference(original.DeclaringType.Namespace, original.DeclaringType.Name, module, scope, !ArrayLeaf);
                    break;
                case "wrong-owner": target.DeclaringType = module.ImportReference(core.MainModule.GetType("System.Object")); break;
                case "wrong-name": target.Name += "Other"; break;
            }
            // Metadata-only address holders; malformed references are never executed.
            var il = holder.Body.GetILProcessor(); il.Emit(Cil.OpCodes.Ldftn, target); il.Emit(Cil.OpCodes.Pop); il.Emit(Cil.OpCodes.Ret);
        }
        var roots = new Cecil.TypeDefinition("Fixture", "Roots", Cecil.TypeAttributes.Public, module.TypeSystem.Object); module.Types.Add(roots);
        var caller = new Cecil.MethodDefinition("ReflectionOnlyCaller", Cecil.MethodAttributes.Public | Cecil.MethodAttributes.Static, module.TypeSystem.Void);
        roots.Methods.Add(caller); caller.Body.GetILProcessor().Emit(Cil.OpCodes.Ret);
        app.Write(output, new Cecil.WriterParameters { Timestamp = 0, DeterministicMvid = true });
    }
}
