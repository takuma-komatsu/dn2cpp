using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Cecil = Mono.Cecil;
using Cil = Mono.Cecil.Cil;

internal static class SearchValuesShuffleGuards
{
    public static bool Run(string[] args)
    {
        if (args.Length is not (5 or 6) || args[0] != "--search-values-shuffle-guards") return false;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        bool routes = args.Length == 6 && args[5] == "routes";
        string input = Path.GetFullPath(args[4]);
        if (File.Exists(input)) throw new InvalidOperationException("guard metadata output must be new");
        string corePath = Path.GetFullPath(args[3]);
        string facadePath = Path.Combine(Path.GetDirectoryName(corePath)!, "System.Runtime.Intrinsics.dll");
        string[] mutations = { "valid", "forwarded", "mixed-forwarded", "instance", "explicit-this", "vararg", "generic", "byref-return",
            "modifier-return", "array-return", "class-vector", "byref-argument", "modifier-argument", "array-argument", "wrong-element", "wrong-width", "extra", "missing",
            "foreign-vector", "foreign-return", "foreign-second", "foreign-owner", "type-context", "method-context" };
        using (var core = Cecil.AssemblyDefinition.ReadAssembly(corePath))
        using (var facade = Cecil.AssemblyDefinition.ReadAssembly(facadePath))
        using (var app = Cecil.AssemblyDefinition.CreateAssembly(new Cecil.AssemblyNameDefinition("SearchValuesShuffleGuards", new Version(1, 0)), "SearchValuesShuffleGuards", Cecil.ModuleKind.Dll))
        {
            var module = app.MainModule;
            var host = new Cecil.TypeDefinition("Fixture", "Cases", Cecil.TypeAttributes.Public, module.TypeSystem.Object);
            module.Types.Add(host); host.GenericParameters.Add(new Cecil.GenericParameter("T", host));
            var definition = core.MainModule.GetType("System.Buffers.SearchValues").Methods.Single(m => m.Name == "ShuffleNativeModified");
            foreach (string mutation in mutations)
            {
                var method = new Cecil.MethodDefinition(mutation, Cecil.MethodAttributes.Public | Cecil.MethodAttributes.Static, module.TypeSystem.Void);
                host.Methods.Add(method); method.GenericParameters.Add(new Cecil.GenericParameter("U", method));
                var target = new Cecil.MethodReference(definition.Name, module.ImportReference(definition.ReturnType), module.ImportReference(definition.DeclaringType));
                foreach (var parameter in definition.Parameters) target.Parameters.Add(new Cecil.ParameterDefinition(module.ImportReference(parameter.ParameterType)));
                Cecil.GenericInstanceType Vector(string name, Cecil.IMetadataScope scope, Cecil.TypeReference element)
                {
                    if (scope is Cecil.AssemblyNameReference assembly && !module.AssemblyReferences.Contains(assembly))
                        module.AssemblyReferences.Add(assembly);
                    var vector = new Cecil.GenericInstanceType(new Cecil.TypeReference("System.Runtime.Intrinsics", name, module, scope, true));
                    vector.GenericArguments.Add(element); return vector;
                }
                switch (mutation)
                {
                    case "forwarded":
                    case "mixed-forwarded":
                        var scope = new Cecil.AssemblyNameReference(facade.Name.Name, facade.Name.Version) { PublicKeyToken = facade.Name.PublicKeyToken, Culture = facade.Name.Culture };
                        var forwarded = Vector("Vector128`1", scope, module.TypeSystem.Byte);
                        if (mutation == "mixed-forwarded") target.Parameters[0].ParameterType = forwarded;
                        else
                        {
                            target.ReturnType = forwarded;
                            foreach (var parameter in target.Parameters) parameter.ParameterType = forwarded;
                        }
                        break;
                    case "instance": target.HasThis = true; break;
                    case "explicit-this": target.HasThis = true; target.ExplicitThis = true; break;
                    case "vararg": target.CallingConvention = Cecil.MethodCallingConvention.VarArg; break;
                    case "generic": target.GenericParameters.Add(new Cecil.GenericParameter("G", target)); break;
                    case "byref-return": target.ReturnType = new Cecil.ByReferenceType(target.ReturnType); break;
                    case "modifier-return": target.ReturnType = new Cecil.OptionalModifierType(module.TypeSystem.Int32, target.ReturnType); break;
                    case "array-return": target.ReturnType = new Cecil.ArrayType(target.ReturnType); break;
                    case "class-vector": target.ReturnType.IsValueType = false; ((Cecil.GenericInstanceType)target.ReturnType).ElementType.IsValueType = false; break;
                    case "array-argument": target.Parameters[0].ParameterType = new Cecil.ArrayType(target.Parameters[0].ParameterType); break;
                    case "byref-argument": target.Parameters[0].ParameterType = new Cecil.ByReferenceType(target.Parameters[0].ParameterType); break;
                    case "modifier-argument": target.Parameters[0].ParameterType = new Cecil.RequiredModifierType(module.TypeSystem.Int32, target.Parameters[0].ParameterType); break;
                    case "wrong-element": target.Parameters[0].ParameterType = Vector("Vector128`1", target.DeclaringType.Scope, module.TypeSystem.SByte); break;
                    case "wrong-width": target.Parameters[0].ParameterType = Vector("Vector256`1", target.DeclaringType.Scope, module.TypeSystem.Byte); break;
                    case "extra": target.Parameters.Add(new Cecil.ParameterDefinition(module.TypeSystem.Int32)); break;
                    case "missing": target.Parameters.RemoveAt(1); break;
                    case "foreign-vector":
                    case "foreign-return":
                    case "foreign-second":
                        var foreignVector = Vector("Vector128`1", new Cecil.AssemblyNameReference("ForeignVectors", new Version(1, 0)), module.TypeSystem.Byte);
                        if (mutation == "foreign-return") target.ReturnType = foreignVector;
                        else target.Parameters[mutation == "foreign-second" ? 1 : 0].ParameterType = foreignVector;
                        break;
                    case "foreign-owner":
                        var foreign = new Cecil.AssemblyNameReference("ForeignSearchValues", new Version(1, 0));
                        module.AssemblyReferences.Add(foreign);
                        target.DeclaringType = new Cecil.TypeReference("System.Buffers", "SearchValues", module, foreign); break;
                    case "type-context": target.Parameters[0].ParameterType = host.GenericParameters[0]; break;
                    case "method-context": target.Parameters[0].ParameterType = method.GenericParameters[0]; break;
                }
                // These methods hold reference rows for the resolver; they are never executed.
                var il = method.Body.GetILProcessor(); il.Emit(Cil.OpCodes.Call, target); il.Emit(Cil.OpCodes.Pop); il.Emit(Cil.OpCodes.Ret);
            }
            app.Write(input, new Cecil.WriterParameters { Timestamp = 0, DeterministicMvid = true });
        }
        var production = Assembly.LoadFrom(Path.GetFullPath(args[1]));
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
        Set(options, "Input", input); Set(options, "UseILDiet", false); Set(options, "SharedGenerics", false);
        Set(options, "AutoRef", false); Set(options, "IsaSurfaceDump", "predicate-guard-only");
        var compilation = Model("Compilation").GetMethod("Create", statics)!.Invoke(null, new object[] { new[] { input, corePath, facadePath }, options })!;
        var modules = ((IEnumerable)Get(compilation, "Modules")).Cast<object>().ToArray();
        var coreModule = modules.Single(m => (string)Get(m, "AssemblyName") == "System.Private.CoreLib");
        var appModule = modules.Single(m => (string)Get(m, "AssemblyName") == "SearchValuesShuffleGuards");
        var reader = (MetadataReader)Get(coreModule, "Reader");
        var appReader = (MetadataReader)Get(appModule, "Reader");
        var predicate = Model("CoreIntrinsics").GetMethod("IsSearchValuesShuffle", statics)!;
        var raw = Model("CoreIntrinsics").GetMethod("IsSearchValuesShuffleSignature", statics)!;
        var shape = Model("CoreIntrinsics").GetMethod("TryReadSearchValuesShuffleSignature", statics)!;
        bool Accepted(object? value) => value is not null && (bool)predicate.Invoke(null, new[] { value })!;
        unsafe bool Raw(object module, byte[] bytes)
        {
            fixed (byte* pointer = bytes) return (bool)raw.Invoke(null, new object[] { module, new BlobReader(pointer, bytes.Length) })!;
        }
        unsafe bool Shape(object module, byte[] bytes)
        {
            fixed (byte* pointer = bytes) return (bool)shape.Invoke(null,
                new object[] { module, new BlobReader(pointer, bytes.Length), default(EntityHandle), default(EntityHandle), default(EntityHandle) })!;
        }
        int checks = 0;
        void Check(bool value, string label) { if (!value) throw new InvalidOperationException("SearchValues shuffle guard: " + label); checks++; }
        var emptyContext = Model("GenericContext").GetField("Empty", statics)!.GetValue(null)!;
        var resolve = Model("Compilation").GetMethod("ResolveMemberRefMethod", instance)!;
        var function = Model("Compilation").GetMethod("ResolveFunctionMemberRef", instance)!;
        var call = Model("Compilation").GetMethod("ResolveCallTarget", instance)!;
        using var serialized = Cecil.AssemblyDefinition.ReadAssembly(input);
        var tokens = serialized.MainModule.Types.SelectMany(t => t.Methods).ToDictionary(m => m.Name,
            m => MetadataTokens.MemberReferenceHandle((int)((Cecil.MethodReference)m.Body.Instructions[0].Operand).MetadataToken.RID));
        object? Invoke(System.Reflection.MethodInfo resolver, MemberReferenceHandle token, object context) =>
            resolver.Invoke(compilation, new[] { appModule, resolver == call ? (object)(EntityHandle)token : token, context });
        bool Refused(System.Reflection.MethodInfo resolver, MemberReferenceHandle token, object context)
        {
            try { return !Accepted(Invoke(resolver, token, context)); }
            catch (TargetInvocationException error) when (error.InnerException is NotSupportedException) { return true; }
        }
        var actual = Invoke(resolve, tokens["valid"], emptyContext)!;
        Check(Accepted(actual), "original CoreLib MethodDef");
        var savedSignature = Get(actual, "_sig");
        Set(actual, "_sig", Activator.CreateInstance(savedSignature.GetType())!);
        Check(Accepted(actual) && !(bool)Get(actual, "SignatureReady"), "declaration predicate does not decode");
        Set(actual, "_sig", savedSignature);
        var methodDefinition = reader.GetMethodDefinition((MethodDefinitionHandle)Get(actual, "Handle"));
        byte[] signature = reader.GetBlobBytes(methodDefinition.Signature);
        Check(Raw(coreModule, signature), "original raw definition");
        var vector = Get(Get(Get(actual, "Signature"), "ReturnType"), "Class");
        var vectorType = Get(Get(actual, "Signature"), "ReturnType");
        var argument = Array.CreateInstance(Model("TypeDesc"), 1); argument.SetValue(vectorType, 0);
        var empty = Array.CreateInstance(Model("TypeDesc"), 0);
        object Context(bool type) => Activator.CreateInstance(Model("GenericContext"), new object[] { type ? argument : empty, type ? empty : argument })!;
        foreach (string mutation in mutations)
        {
            var reference = appReader.GetMemberReference(tokens[mutation]);
            byte[] referenceSignature = appReader.GetBlobBytes(reference.Signature);
            Check(!Raw(appModule, referenceSignature), "MemberRef is not a declaration " + mutation);
            Check(Shape(appModule, referenceSignature) == (mutation is "valid" or "forwarded" or "mixed-forwarded" or "foreign-vector" or "foreign-return" or "foreign-second" or "foreign-owner"), "raw MemberRef shape " + mutation);
            if (!routes) continue;
            var context = mutation == "type-context" ? Context(true) : mutation == "method-context" ? Context(false) : emptyContext;
            foreach (var resolver in new[] { resolve, function, call })
            {
                Check(Accepted(Invoke(resolve, tokens["valid"], emptyContext)), "valid before " + mutation);
                Check(mutation is "valid" or "forwarded" or "mixed-forwarded" ? Accepted(Invoke(resolver, tokens[mutation], context)) : Refused(resolver, tokens[mutation], context), resolver.Name + " " + mutation);
                Check(Accepted(Invoke(resolve, tokens["valid"], emptyContext)), "valid after " + mutation);
            }
        }
        foreach (byte[] bytes in new[] { Array.Empty<byte>(), signature[..1], signature[..^1], signature.Concat(new byte[] { 0 }).ToArray() })
            Check(!Raw(coreModule, bytes), "truncated or trailing raw signature");
        unsafe void RejectRawTypeSpecs()
        {
            fixed (byte* pointer = signature)
            {
                var blob = new BlobReader(pointer, signature.Length); blob.Offset = 2;
                for (int slot = 0; slot < 3; slot++)
                {
                    blob.ReadByte(); blob.ReadByte(); int start = blob.Offset;
                    blob.ReadCompressedInteger(); int end = blob.Offset;
                    foreach (byte token in new byte[] { 0, 6, 7 })
                    {
                        byte[] bytes = signature[..start].Concat(new[] { token }).Concat(signature[end..]).ToArray();
                        Check(!Shape(coreModule, bytes), "nil/TypeSpec/reserved raw token slot " + slot);
                    }
                    blob.ReadByte(); blob.ReadByte();
                }
            }
        }
        RejectRawTypeSpecs();
        foreach (int offset in Enumerable.Range(0, signature.Length))
        {
            var bytes = (byte[])signature.Clone(); bytes[offset] = 0xff;
            Check(!Raw(coreModule, bytes), "raw byte " + offset);
        }
        void Reject(object value, string field, object replacement)
        {
            var saved = Get(value, field); Set(value, field, replacement);
            try { Check(!Accepted(actual), "MethodDef " + field); }
            finally { Set(value, field, saved); }
            Check(Accepted(actual), "restored " + field);
        }
        var owner = Get(actual, "DeclaringClass");
        Reject(actual, "Handle", default(MethodDefinitionHandle)); Reject(actual, "Rva", 0); Reject(actual, "IsSynthetic", true);
        Reject(actual, "Attributes", MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig);
        Reject(actual, "ImplAttributes", MethodImplAttributes.IL); Reject(actual, "Context", Context(true)); Reject(actual, "Context", Context(false));
        Reject(owner, "Module", appModule); Reject(owner, "GenericArity", 1); Reject(owner, "Context", Context(true)); Reject(owner, "Context", Context(false));
        Reject(owner, "IsValueType", true); Reject(owner, "IsInterface", true); Reject(coreModule, "AssemblyName", "Foreign.CoreLib");
        Reject(vector, "Module", appModule); Reject(vector, "IsValueType", false); Reject(vector, "IsEnum", true);
        Reject(vector, "IsByRefLike", true); Reject(vector, "GenericArity", 2); Reject(vector, "Context", Context(false));
        Reject(vectorType, "Kind", Enum.Parse(Model("TypeKind"), "ByRef")); Reject(vectorType, "IsCanonPlaceholder", true);
        var element = ((IEnumerable)Get(Get(vector, "Context"), "TypeArgs")).Cast<object>().Single();
        Reject(element, "Primitive", PrimitiveTypeCode.SByte); Reject(element, "IsCanonPlaceholder", true);

        var diet = Assembly.LoadFrom(Path.GetFullPath(args[2]));
        var copied = diet.GetType("Dn2Cpp.AssemblyDiet", true)!.GetMethod("IsCopiedSearchValuesShuffle", statics)!;
        using (var core = Cecil.AssemblyDefinition.ReadAssembly(corePath))
        {
            var method = core.MainModule.GetType("System.Buffers.SearchValues").Methods.Single(m => m.Name == "ShuffleNativeModified");
            bool Copy() => (bool)copied.Invoke(null, new object[] { method, method.DeclaringType.FullName })!;
            Check(Copy(), "copied original body");
            var result = method.ReturnType; method.ReturnType = new Cecil.ByReferenceType(result); Check(!Copy(), "copied byref return"); method.ReturnType = result;
            var parameter = method.Parameters[0].ParameterType; method.Parameters[0].ParameterType = new Cecil.OptionalModifierType(core.MainModule.TypeSystem.Int32, parameter); Check(!Copy(), "copied modified argument"); method.Parameters[0].ParameterType = parameter;
            var flags = method.Attributes; method.IsStatic = false; Check(!Copy(), "copied instance"); method.Attributes = flags;
            var implementation = method.ImplAttributes; method.ImplAttributes = Cecil.MethodImplAttributes.IL; Check(!Copy(), "copied implementation"); method.ImplAttributes = implementation;
            method.GenericParameters.Add(new Cecil.GenericParameter("T", method)); Check(!Copy(), "copied generic"); method.GenericParameters.Clear();
            var vectorArgument = ((Cecil.GenericInstanceType)result).GenericArguments[0]; ((Cecil.GenericInstanceType)result).GenericArguments[0] = core.MainModule.TypeSystem.SByte; Check(!Copy(), "copied element"); ((Cecil.GenericInstanceType)result).GenericArguments[0] = vectorArgument;
            Check(Copy(), "copied restored");
        }
        Console.WriteLine("search-values-shuffle-guards:checks=" + checks);
        return true;
    }
}
