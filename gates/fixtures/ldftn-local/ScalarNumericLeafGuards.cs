using System.Collections;
using System.Globalization;
using System.Reflection.PortableExecutable;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

internal static class ScalarNumericLeafGuards
{
    private delegate bool RawPredicate(string owner, string name, BlobReader blob);
    internal static readonly string[] FloatNames = { "get_Exponent", "get_Significand", "get_BiasedExponent",
        "get_TrailingSignificand", "ExtractBiasedExponentFromBits", "ExtractTrailingSignificandFromBits" };

    public static bool Run(string[] args)
    {
        if (args.Length != 5 || args[0] != "--scalar-numeric-leaf-guards") return false;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        string input = Path.GetFullPath(args[3]);
        if (File.Exists(input)) throw new InvalidOperationException("model input must be new");
        string[] mutations = { "valid", "instance", "explicit", "vararg", "generic", "return", "byref", "modifier", "pinned", "parameter", "owner", "ownerbyref", "spec" };
        var targets = new List<(string Owner, string Name)> { ("System.Int32", "Sign"), ("System.Decimal", "get_Exponent") };
        foreach (string owner in new[] { "System.Single", "System.Double" })
            foreach (string name in FloatNames) targets.Add((owner, name));
        using (var core = Mono.Cecil.AssemblyDefinition.ReadAssembly(args[2]))
        using (var app = Mono.Cecil.AssemblyDefinition.CreateAssembly(new Mono.Cecil.AssemblyNameDefinition("ScalarLeafGuards", new Version(1, 0)), "ScalarLeafGuards", Mono.Cecil.ModuleKind.Dll))
        {
            var module = app.MainModule;
            var host = new Mono.Cecil.TypeDefinition("Fixture", "Cases", Mono.Cecil.TypeAttributes.Public, module.TypeSystem.Object); module.Types.Add(host);
            for (int index = 0; index < targets.Count; index++)
            foreach (string mutation in mutations)
            {
                var row = targets[index];
                var definition = core.MainModule.GetType(row.Owner).Methods.Single(m => m.Name == row.Name);
                var target = new Mono.Cecil.MethodReference(definition.Name, module.ImportReference(definition.ReturnType), module.ImportReference(definition.DeclaringType)) { HasThis = definition.HasThis };
                foreach (var parameter in definition.Parameters) target.Parameters.Add(new Mono.Cecil.ParameterDefinition(module.ImportReference(parameter.ParameterType)));
                switch (mutation)
                {
                    case "instance": target.HasThis = !target.HasThis; break;
                    case "explicit": target.ExplicitThis = true; break;
                    case "vararg": target.CallingConvention = Mono.Cecil.MethodCallingConvention.VarArg; break;
                    case "generic": target.GenericParameters.Add(new Mono.Cecil.GenericParameter("T", target)); break;
                    case "return": target.ReturnType = module.TypeSystem.Object; break;
                    case "byref": target.ReturnType = new Mono.Cecil.ByReferenceType(target.ReturnType); break;
                    case "modifier": target.ReturnType = new Mono.Cecil.OptionalModifierType(module.TypeSystem.Object, target.ReturnType); break;
                    case "pinned": target.ReturnType = new Mono.Cecil.PinnedType(target.ReturnType); break;
                    case "parameter": target.Parameters.Add(new Mono.Cecil.ParameterDefinition(module.TypeSystem.Int32)); break;
                    case "owner": var owner = new Mono.Cecil.GenericInstanceType(target.DeclaringType); owner.GenericArguments.Add(module.TypeSystem.Int32); target.DeclaringType = owner; break;
                    case "ownerbyref": target.DeclaringType = new Mono.Cecil.ByReferenceType(target.DeclaringType); break;
                    case "spec": var spec = new Mono.Cecil.GenericInstanceMethod(target); spec.GenericArguments.Add(module.TypeSystem.Int32); target = spec; break;
                }
                var method = new Mono.Cecil.MethodDefinition("case" + index + "_" + mutation, Mono.Cecil.MethodAttributes.Public | Mono.Cecil.MethodAttributes.Static, module.TypeSystem.Void); host.Methods.Add(method);
                var il = method.Body.GetILProcessor(); il.Emit(Mono.Cecil.Cil.OpCodes.Call, target); il.Emit(Mono.Cecil.Cil.OpCodes.Pop); il.Emit(Mono.Cecil.Cil.OpCodes.Ret);
            }
            app.Write(input, new Mono.Cecil.WriterParameters { Timestamp = 0, DeterministicMvid = true });
        }
        var production = Assembly.LoadFrom(Path.GetFullPath(args[1]));
        var diet = Assembly.LoadFrom(Path.GetFullPath(args[4]));
        const BindingFlags instance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        const BindingFlags statics = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        Type Model(string name) => production.GetType("Dn2Cpp." + name, true)!;
        object Get(object value, string name) => value.GetType().GetField(name, instance) is { } field ? field.GetValue(value)! : value.GetType().GetProperty(name, instance)!.GetValue(value)!;
        void Set(object value, string name, object next)
        {
            if (value.GetType().GetField(name, instance) is { } field) field.SetValue(value, next);
            else value.GetType().GetProperty(name, instance)!.SetValue(value, next);
        }
        var options = Activator.CreateInstance(Model("TranspileOptions"))!;
        Set(options, "Input", input); Set(options, "UseILDiet", false); Set(options, "SharedGenerics", false); Set(options, "AutoRef", false); Set(options, "IsaSurfaceDump", "predicate-guard-only");
        var compilation = Model("Compilation").GetMethod("Create", statics)!.Invoke(null, new object[] { new[] { input, Path.GetFullPath(args[2]) }, options })!;
        var modules = ((IEnumerable)Get(compilation, "Modules")).Cast<object>().ToArray();
        var coreModule = modules.Single(m => (string)Get(m, "AssemblyName") == "System.Private.CoreLib");
        var appModule = modules.Single(m => (string)Get(m, "AssemblyName") == "ScalarLeafGuards");
        var reader = (MetadataReader)Get(coreModule, "Reader");
        var predicate = Model("CoreIntrinsics").GetMethod("IsScalarNumericLeaf", statics)!;
        var raw = Model("ScalarNumericLeafContract").GetMethod("IsSignature", statics)!.CreateDelegate<RawPredicate>();
        var dietRaw = diet.GetType("Dn2Cpp.ScalarNumericLeafContract", true)!.GetMethod("IsSignature", statics)!.CreateDelegate<RawPredicate>();
        var dietDefinition = diet.GetType("Dn2Cpp.ScalarNumericLeafContract", true)!.GetMethod("IsDefinition", statics)!;
        var bodyPredicate = Model("ScalarNumericLeafContract").GetMethod("IsBody", statics)!;
        var dietBodyPredicate = diet.GetType("Dn2Cpp.ScalarNumericLeafContract", true)!.GetMethod("IsBody", statics)!;
        byte[] original = File.ReadAllBytes(args[2]);
        var originalPe = (PEReader)Get(coreModule, "PE");
        bool Accepted(object? value) => value is not null && (bool)predicate.Invoke(null, new[] { value })!;
        int checks = 0;
        void Check(bool value, string label) { if (!value) throw new InvalidOperationException("scalar leaf guard: " + label); checks++; }
        var context = Model("GenericContext").GetField("Empty", statics)!.GetValue(null)!;
        var resolve = Model("Compilation").GetMethod("ResolveMemberRefMethod", instance)!;
        var function = Model("Compilation").GetMethod("ResolveFunctionMemberRef", instance)!;
        var call = Model("Compilation").GetMethod("ResolveCallTarget", instance)!;
        var specification = Model("Compilation").GetMethod("ResolveMethodSpec", instance)!;
        object? Invoke(System.Reflection.MethodInfo resolver, EntityHandle token)
        {
            object typed = resolver == call ? token : token.Kind == HandleKind.MethodSpecification ? (object)(MethodSpecificationHandle)token : (MemberReferenceHandle)token;
            return resolver.Invoke(compilation, new[] { appModule, typed, context });
        }
        bool Refused(System.Reflection.MethodInfo resolver, EntityHandle token)
        {
            try { return Invoke(resolver, token) is null; }
            catch (TargetInvocationException error) when (error.InnerException is NotSupportedException) { return true; }
        }
        using var serialized = Mono.Cecil.AssemblyDefinition.ReadAssembly(input);
        var tokens = serialized.MainModule.Types.SelectMany(t => t.Methods).ToDictionary(m => m.Name, m => MetadataTokens.EntityHandle(((Mono.Cecil.MethodReference)m.Body.Instructions[0].Operand).MetadataToken.ToInt32()));
        for (int index = 0; index < targets.Count; index++)
        {
            var row = targets[index];
            var ownerHandle = reader.TypeDefinitions.Single(h => { var t = reader.GetTypeDefinition(h); return reader.GetString(t.Namespace) + "." + reader.GetString(t.Name) == row.Owner; });
            var handle = reader.GetTypeDefinition(ownerHandle).GetMethods().Single(h => reader.GetString(reader.GetMethodDefinition(h).Name) == row.Name);
            var actual = ((IDictionary)Get(coreModule, "MethodMap"))[handle]!;
            var ownerClass = Get(actual, "DeclaringClass");
            var valid = tokens["case" + index + "_valid"];
            Check(Accepted(actual), "actual-" + row);
            Check((bool)dietDefinition.Invoke(null, new object[] { reader, handle, row.Owner, row.Name })!, "diet-definition-" + row);
            foreach (string mutation in mutations)
            foreach (var resolver in mutation == "spec" ? new[] { specification, call } : new[] { resolve, function, call })
            {
                Check(Accepted(Invoke(resolve, valid)), "good-before");
                Check(mutation == "valid" ? Accepted(Invoke(resolver, tokens["case" + index + "_" + mutation])) : Refused(resolver, tokens["case" + index + "_" + mutation]), row + ":" + mutation + ":" + resolver.Name);
                Check(Accepted(Invoke(resolve, valid)), "good-after");
            }
            void Reject(object value, string field, object replacement, bool cache = true)
            {
                var saved = Get(value, field); Set(value, field, replacement);
                try
                {
                    Check(!Accepted(actual), "declaration-" + field);
                    if (cache) Check(Refused(resolve, valid), "cached-" + field);
                }
                finally { Set(value, field, saved); }
                Check(Accepted(actual), "restored-" + field);
            }
            Reject(coreModule, "AssemblyName", "Foreign.CoreLib");
            Reject(ownerClass, "Module", appModule); Reject(ownerClass, "_fullName", "Foreign.Scalar", false);
            Reject(ownerClass, "Handle", default(TypeDefinitionHandle)); Reject(ownerClass, "IsEnum", true);
            Reject(ownerClass, "GenericArity", 1); Reject(ownerClass, "IsInterface", true); Reject(ownerClass, "IsValueType", false); Reject(ownerClass, "IsByRefLike", true);
            Reject(actual, "Handle", default(MethodDefinitionHandle)); Reject(actual, "Rva", 0); Reject(actual, "Rva", (int)Get(actual, "Rva") + 1); Reject(actual, "IsSynthetic", true);
            Reject(actual, "Attributes", (MethodAttributes)Get(actual, "Attributes") ^ MethodAttributes.Static);
            Reject(actual, "Attributes", (MethodAttributes)Get(actual, "Attributes") | MethodAttributes.Virtual);
            Reject(actual, "ImplAttributes", MethodImplAttributes.InternalCall);
            var typeArray = Array.CreateInstance(Model("TypeDesc"), 1); typeArray.SetValue(Get(actual, "Signature").GetType().GetProperty("ReturnType")!.GetValue(Get(actual, "Signature")), 0);
            var empty = Array.CreateInstance(Model("TypeDesc"), 0);
            Reject(actual, "Context", Activator.CreateInstance(Model("GenericContext"), new object[] { empty, typeArray })!);
            Reject(actual, "Context", Activator.CreateInstance(Model("GenericContext"), new object[] { typeArray, empty })!);
            Reject(ownerClass, "Context", Activator.CreateInstance(Model("GenericContext"), new object[] { empty, typeArray })!);
            Reject(ownerClass, "Context", Activator.CreateInstance(Model("GenericContext"), new object[] { typeArray, empty })!);
            var resultType = Get(Get(actual, "Signature"), "ReturnType");
            Reject(resultType, "Primitive", PrimitiveTypeCode.IntPtr);
            Reject(resultType, "Kind", Enum.Parse(Model("TypeKind"), "ByRef"));
            if (row.Owner == "System.Single" && row.Name is "get_BiasedExponent" or "get_TrailingSignificand")
            {
                var block = originalPe.GetMethodBody(reader.GetMethodDefinition(handle).RelativeVirtualAddress);
                var il = block.GetILReader(); il.ReadBytes(3); int bitToken = il.ReadInt32(); il.ReadByte(); int extractToken = il.ReadInt32();
                var bitHandle = (MethodDefinitionHandle)MetadataTokens.EntityHandle(bitToken);
                var bitMethod = ((IDictionary)Get(coreModule, "MethodMap"))[bitHandle]!;
                var extract = ((IDictionary)Get(coreModule, "MethodMap"))[(MethodDefinitionHandle)MetadataTokens.EntityHandle(extractToken)]!;
                Reject(bitMethod, "Handle", default(MethodDefinitionHandle)); Reject(bitMethod, "Rva", 0); Reject(bitMethod, "Module", appModule);
                Reject(bitMethod, "Name", "SingleToInt32Bits"); Reject(bitMethod, "IsSynthetic", true);
                Reject(bitMethod, "Attributes", MethodAttributes.Public); Reject(bitMethod, "ImplAttributes", MethodImplAttributes.InternalCall);
                var bitOwner = Get(bitMethod, "DeclaringClass");
                Reject(bitOwner, "IsValueType", true); Reject(bitOwner, "IsInterface", true); Reject(bitOwner, "IsEnum", true); Reject(bitOwner, "IsByRefLike", true);
                Reject(bitOwner, "GenericArity", 1); Reject(bitOwner, "Module", appModule);
                Reject(extract, "Handle", default(MethodDefinitionHandle)); Reject(extract, "Rva", 0); Reject(extract, "IsSynthetic", true);
                var methodMap = (IDictionary)Get(coreModule, "MethodMap");
                var extractHandle = (MethodDefinitionHandle)MetadataTokens.EntityHandle(extractToken);
                methodMap[extractHandle] = actual;
                try { Check(!Accepted(actual) && Refused(resolve, valid), "replaced-extract-map-entry"); }
                finally { methodMap[extractHandle] = extract; }
                Check(Accepted(actual), "restored-extract-map-entry");
                var bitSignature = Get(bitMethod, "Signature");
                var bitResult = bitSignature.GetType().GetProperty("ReturnType")!.GetValue(bitSignature)!;
                Reject(bitResult, "Primitive", PrimitiveTypeCode.Int32);
                int rva = reader.GetMethodDefinition(handle).RelativeVirtualAddress;
                var section = originalPe.PEHeaders.SectionHeaders.Single(s => rva >= s.VirtualAddress && rva < s.VirtualAddress + s.SizeOfRawData);
                int bodyOffset = section.PointerToRawData + rva - section.VirtualAddress;
                Check((original[bodyOffset] & 3) == 2, "single-bit-read-tiny-header");
                int code = bodyOffset + 1;
                var mutationsBytes = new List<(string Label, int Position, byte Value)>();
                foreach (int offset in new[] { 0, 1, 2, 7, 12 }) mutationsBytes.Add(("opcode-" + offset, code + offset, 0));
                mutationsBytes.Add(("length", bodyOffset, (byte)(original[bodyOffset] - 4)));
                mutationsBytes.Add(("bit-target-kind", code + 6, 0x0a));
                mutationsBytes.Add(("extract-target-kind", code + 11, 0x0a));
                mutationsBytes.Add(("bit-target-row", code + 3, (byte)(original[code + 3] + 1)));
                mutationsBytes.Add(("extract-target-row", code + 8, (byte)(original[code + 8] + 1)));
                var bitDefinition = reader.GetMethodDefinition(bitHandle);
                int blob = originalPe.PEHeaders.MetadataStartOffset + reader.GetHeapMetadataOffset(HeapIndex.Blob) + MetadataTokens.GetHeapOffset(bitDefinition.Signature);
                Check(original[blob] == 4, "bit-converter-raw-signature-width");
                mutationsBytes.Add(("bit-signature-result", blob + 3, (byte)PrimitiveTypeCode.Int32));
                mutationsBytes.Add(("bit-signature-input", blob + 4, (byte)PrimitiveTypeCode.Double));
                foreach (var mutation in mutationsBytes)
                {
                    Check(Accepted(Invoke(resolve, valid)), "body-good-before");
                    byte[] changed = (byte[])original.Clone(); changed[mutation.Position] = mutation.Value;
                    using var pe = new PEReader(new MemoryStream(changed)); var changedReader = pe.GetMetadataReader();
                    Set(coreModule, "Reader", changedReader); Set(coreModule, "PE", pe);
                    try
                    {
                        Check(!Accepted(actual), "body-" + mutation.Label);
                        Check(Refused(resolve, valid) && Refused(function, valid) && Refused(call, valid), "body-caches-" + mutation.Label);
                        foreach (var bodyCheck in new[] { bodyPredicate, dietBodyPredicate })
                            Check(!(bool)bodyCheck.Invoke(null, new object[] { changedReader, pe, handle, row.Owner, row.Name })!, "body-shared-" + mutation.Label);
                    }
                    finally { Set(coreModule, "Reader", reader); Set(coreModule, "PE", originalPe); }
                    Check(Accepted(Invoke(resolve, valid)), "body-good-after");
                }
            }
            byte[] validBytes = reader.GetBlobBytes(reader.GetMethodDefinition(handle).Signature);
            var blobs = new List<byte[]> { validBytes, Array.Empty<byte>(), validBytes.Concat(new byte[] { 0 }).ToArray(), new byte[] { 0x45 }.Concat(validBytes).ToArray() };
            for (int position = 0; position < validBytes.Length; position++) { byte[] bad = (byte[])validBytes.Clone(); bad[position] ^= 0x10; blobs.Add(bad); }
            for (int b = 0; b < blobs.Count; b++)
            {
                var metadata = new MetadataBuilder(); metadata.AddModule(0, metadata.GetOrAddString("ScalarRaw"), metadata.GetOrAddGuid(Guid.Empty), default, default);
                var blob = metadata.GetOrAddBlob(blobs[b]); var image = new BlobBuilder(); new MetadataRootBuilder(metadata).Serialize(image, 0, 0);
                using var source = MetadataReaderProvider.FromMetadataImage(image.ToImmutableArray());
                Check(raw(row.Owner, row.Name, source.GetMetadataReader().GetBlobReader(blob)) == (b == 0), "raw-" + row + ":" + b);
                Check(dietRaw(row.Owner, row.Name, source.GetMetadataReader().GetBlobReader(blob)) == (b == 0), "diet-raw-" + row + ":" + b);
            }
        }
        Console.WriteLine("scalar-leaf-authority-checks:" + checks);
        return true;
    }
}
