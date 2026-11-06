using System.Collections;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

internal static class ConcreteVectorReductionGuards
{
    private delegate bool RawPredicate(string name, BlobReader blob, int expectedToken);
    public static bool Run(string[] args)
    {
        if (args.Length != 4 || args[0] != "--concrete-vector-reduction-guards") return false;
        string input = Path.GetFullPath(args[3]);
        string foreignPath = Path.ChangeExtension(input, ".foreign.dll");
        if (File.Exists(input) || File.Exists(foreignPath)) throw new InvalidOperationException("model app and foreign dependency must be new");
        string[] mutations = { "valid", "instance", "vararg", "generic", "return", "byref-return", "modifier-return", "byref-operand", "modifier-operand", "parameter", "scalar", "owner", "spec", "type-context", "method-context", "vector4", "foreign-operand" };
        using (var core = Mono.Cecil.AssemblyDefinition.ReadAssembly(args[2]))
        using (var foreign = Mono.Cecil.AssemblyDefinition.CreateAssembly(new Mono.Cecil.AssemblyNameDefinition("ForeignConcreteVectors", new Version(1, 0)), "ForeignConcreteVectors", Mono.Cecil.ModuleKind.Dll))
        using (var app = Mono.Cecil.AssemblyDefinition.CreateAssembly(new Mono.Cecil.AssemblyNameDefinition("ConcreteReductionGuards", new Version(1, 0)), "ConcreteReductionGuards", Mono.Cecil.ModuleKind.Dll))
        {
            var module = app.MainModule;
            var host = new Mono.Cecil.TypeDefinition("Fixture", "Cases", Mono.Cecil.TypeAttributes.Public, module.TypeSystem.Object); module.Types.Add(host);
            host.GenericParameters.Add(new Mono.Cecil.GenericParameter("T", host));
            foreach (int width in new[] { 2, 3 }) foreign.MainModule.Types.Add(new Mono.Cecil.TypeDefinition("System.Numerics", "Vector" + width,
                Mono.Cecil.TypeAttributes.Public | Mono.Cecil.TypeAttributes.Sealed | Mono.Cecil.TypeAttributes.SequentialLayout, foreign.MainModule.ImportReference(typeof(ValueType))));
            foreach (int width in new[] { 2, 3 })
            foreach (string name in ConcreteVectorReductionFixture.Names)
            foreach (string mutation in mutations)
            {
                var definition = core.MainModule.GetType("System.Runtime.Intrinsics.Vector128").Methods.Single(m => m.Name == name && !m.HasGenericParameters && m.Parameters.Count == (ConcreteVectorReductionFixture.Bits(name) ? 1 : 2) && m.Parameters[0].ParameterType.FullName == "System.Numerics.Vector" + width);
                var method = new Mono.Cecil.MethodDefinition(Key(width, name, mutation), Mono.Cecil.MethodAttributes.Public | Mono.Cecil.MethodAttributes.Static, module.TypeSystem.Void); host.Methods.Add(method);
                method.GenericParameters.Add(new Mono.Cecil.GenericParameter("U", method));
                var target = new Mono.Cecil.MethodReference(definition.Name, module.ImportReference(definition.ReturnType), module.ImportReference(definition.DeclaringType));
                foreach (var parameter in definition.Parameters) target.Parameters.Add(new Mono.Cecil.ParameterDefinition(module.ImportReference(parameter.ParameterType)));
                switch (mutation)
                {
                    case "instance": target.HasThis = true; break;
                    case "vararg": target.CallingConvention = Mono.Cecil.MethodCallingConvention.VarArg; break;
                    case "generic": target.GenericParameters.Add(new Mono.Cecil.GenericParameter("T", target)); break;
                    case "return": target.ReturnType = module.TypeSystem.UInt32; break;
                    case "byref-return": target.ReturnType = new Mono.Cecil.ByReferenceType(target.ReturnType); break;
                    case "modifier-return": target.ReturnType = new Mono.Cecil.OptionalModifierType(module.TypeSystem.Object, target.ReturnType); break;
                    case "byref-operand": target.Parameters[0].ParameterType = new Mono.Cecil.ByReferenceType(target.Parameters[0].ParameterType); break;
                    case "modifier-operand": target.Parameters[0].ParameterType = new Mono.Cecil.OptionalModifierType(module.TypeSystem.Object, target.Parameters[0].ParameterType); break;
                    case "parameter": target.Parameters.Add(new Mono.Cecil.ParameterDefinition(module.TypeSystem.Int32)); break;
                    case "scalar": if (target.Parameters.Count == 2) target.Parameters[1].ParameterType = module.TypeSystem.Double; else target.Parameters.Add(new Mono.Cecil.ParameterDefinition(module.TypeSystem.Single)); break;
                    case "owner": var owner = new Mono.Cecil.GenericInstanceType(target.DeclaringType); owner.GenericArguments.Add(module.TypeSystem.Int32); target.DeclaringType = owner; break;
                    case "spec": var spec = new Mono.Cecil.GenericInstanceMethod(target); spec.GenericArguments.Add(module.TypeSystem.Int32); target = spec; break;
                    case "vector4": target.Parameters[0].ParameterType = module.ImportReference(core.MainModule.GetType("System.Numerics.Vector4")); break;
                    case "foreign-operand": target.Parameters[0].ParameterType = module.ImportReference(foreign.MainModule.GetType("System.Numerics.Vector" + width)); break;
                    case "type-context": target.Parameters[0].ParameterType = host.GenericParameters[0]; break;
                    case "method-context": target.Parameters[0].ParameterType = method.GenericParameters[0]; break;
                }
                var il = method.Body.GetILProcessor(); il.Emit(Mono.Cecil.Cil.OpCodes.Call, target); il.Emit(Mono.Cecil.Cil.OpCodes.Pop); il.Emit(Mono.Cecil.Cil.OpCodes.Ret);
            }
            foreign.Write(foreignPath, new Mono.Cecil.WriterParameters { Timestamp = 0, DeterministicMvid = true });
            app.Write(input, new Mono.Cecil.WriterParameters { Timestamp = 0, DeterministicMvid = true });
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
        Set(options, "Input", input); Set(options, "UseILDiet", false); Set(options, "SharedGenerics", false); Set(options, "AutoRef", false); Set(options, "IsaSurfaceDump", "predicate-guard-only");
        var compilation = Model("Compilation").GetMethod("Create", statics)!.Invoke(null, new object[] { new[] { input, Path.GetFullPath(args[2]), foreignPath }, options })!;
        var modules = ((IEnumerable)Get(compilation, "Modules")).Cast<object>().ToArray();
        var coreModule = modules.Single(m => (string)Get(m, "AssemblyName") == "System.Private.CoreLib");
        var appModule = modules.Single(m => (string)Get(m, "AssemblyName") == "ConcreteReductionGuards");
        var reader = (MetadataReader)Get(coreModule, "Reader");
        var predicate = Model("CoreIntrinsics").GetMethod("IsConcreteVectorReduction", statics)!;
        var raw = Model("CoreIntrinsics").GetMethod("IsConcreteVectorReductionRawSignature", statics)!.CreateDelegate<RawPredicate>();
        bool Accepted(object? value) => value is not null && (bool)predicate.Invoke(null, new[] { value })!;
        int checks = 0;
        void Check(bool value, string label) { if (!value) throw new InvalidOperationException("concrete vector reduction guard: " + label); checks++; }
        var emptyContext = Model("GenericContext").GetField("Empty", statics)!.GetValue(null)!;
        var resolve = Model("Compilation").GetMethod("ResolveMemberRefMethod", instance)!;
        var function = Model("Compilation").GetMethod("ResolveFunctionMemberRef", instance)!;
        var call = Model("Compilation").GetMethod("ResolveCallTarget", instance)!;
        var specification = Model("Compilation").GetMethod("ResolveMethodSpec", instance)!;
        object? Invoke(System.Reflection.MethodInfo resolver, EntityHandle token, object context)
        {
            object typed = resolver == call ? token : token.Kind == HandleKind.MethodSpecification ? (object)(MethodSpecificationHandle)token : (MemberReferenceHandle)token;
            return resolver.Invoke(compilation, new[] { appModule, typed, context });
        }
        bool Refused(System.Reflection.MethodInfo resolver, EntityHandle token, object context)
        {
            try { return !Accepted(Invoke(resolver, token, context)); }
            catch (TargetInvocationException error) when (error.InnerException is NotSupportedException) { return true; }
        }
        using var serialized = Mono.Cecil.AssemblyDefinition.ReadAssembly(input);
        var tokens = serialized.MainModule.Types.SelectMany(t => t.Methods).ToDictionary(m => m.Name, m => MetadataTokens.EntityHandle(((Mono.Cecil.MethodReference)m.Body.Instructions[0].Operand).MetadataToken.ToInt32()));
        foreach (int width in new[] { 2, 3 })
        foreach (string name in ConcreteVectorReductionFixture.Names)
        {
            var valid = tokens[Key(width, name, "valid")];
            var actual = Invoke(resolve, valid, emptyContext)!; Check(Accepted(actual), "actual-" + width + "-" + name);
            var owner = Get(actual, "DeclaringClass");
            var definition = reader.GetMethodDefinition((MethodDefinitionHandle)Get(actual, "Handle"));
            byte[] signature = reader.GetBlobBytes(definition.Signature);
            var rawCases = new List<byte[]> { signature, Array.Empty<byte>(), signature[..1], signature[..^1], signature.Concat(new byte[] { 0 }).ToArray() };
            foreach (int offset in new[] { 0, 1, 2, 3 })
            {
                var changed = (byte[])signature.Clone(); changed[offset] = offset switch { 0 => 0x20, 1 => 0, 2 => 9, _ => 0x10 }; rawCases.Add(changed);
            }
            foreach (var mutation in new[] { (Offset: 0, Code: (byte)0x40), (Offset: 3, Code: (byte)0x45), (Offset: 3, Code: (byte)0x41) })
            {
                var changed = (byte[])signature.Clone(); changed[mutation.Offset] = mutation.Code; rawCases.Add(changed);
            }
            rawCases.Add(signature.Concat(new byte[] { 0x41 }).ToArray());
            rawCases.Add(new byte[] { 0, signature[1], signature[2], 0x11, 0x80 });
            foreach (byte[] bytes in rawCases)
            {
                var metadata = new MetadataBuilder(); metadata.AddModule(0, metadata.GetOrAddString("ConcreteRaw"), metadata.GetOrAddGuid(Guid.Empty), default, default);
                var blob = metadata.GetOrAddBlob(bytes); var image = new BlobBuilder(); new MetadataRootBuilder(metadata).Serialize(image, 0, 0);
                using var source = MetadataReaderProvider.FromMetadataImage(image.ToImmutableArray());
                bool expected = ReferenceEquals(bytes, signature);
                Check(raw(name, source.GetMetadataReader().GetBlobReader(blob), 0) == expected, "raw-" + width + "-" + name + "-" + Convert.ToHexString(bytes));
                Check(!raw("Unknown", source.GetMetadataReader().GetBlobReader(blob), 0), "raw-name");
                Check(!raw(name, source.GetMetadataReader().GetBlobReader(blob), 4), "raw-token");
            }
            var parameters = ((IEnumerable)Get(Get(actual, "Signature"), "ParameterTypes")).Cast<object>().ToArray();
            var argument = Array.CreateInstance(Model("TypeDesc"), 1); argument.SetValue(parameters[0], 0);
            var empty = Array.CreateInstance(Model("TypeDesc"), 0);
            object Context(bool type) => Activator.CreateInstance(Model("GenericContext"), new object[] { type ? argument : empty, type ? empty : argument })!;
            foreach (string mutation in mutations)
            foreach (var resolver in mutation == "spec" ? new[] { specification, call } : new[] { resolve, function, call })
            {
                Check(Accepted(Invoke(resolve, valid, emptyContext)), "good-before");
                var context = mutation == "type-context" ? Context(true) : mutation == "method-context" ? Context(false) : emptyContext;
                Check(mutation == "valid" ? Accepted(Invoke(resolver, tokens[Key(width, name, mutation)], context)) : Refused(resolver, tokens[Key(width, name, mutation)], context), "reference-" + width + "-" + name + "-" + mutation + "-" + resolver.Name);
                Check(Accepted(Invoke(resolve, valid, emptyContext)), "good-after");
            }
            void Reject(object value, string field, object replacement, bool cache = true)
            {
                var saved = Get(value, field); Set(value, field, replacement);
                try { Check(!Accepted(actual), "declaration-" + field); if (cache) Check(Refused(resolve, valid, emptyContext), "cached-" + field); }
                finally { Set(value, field, saved); }
                Check(Accepted(actual), "restored-" + field);
            }
            Reject(actual, "Handle", default(MethodDefinitionHandle)); Reject(actual, "Rva", 0); Reject(actual, "IsSynthetic", true);
            Reject(actual, "Attributes", MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig);
            Reject(actual, "Attributes", MethodAttributes.Assembly | MethodAttributes.HideBySig);
            Reject(actual, "ImplAttributes", MethodImplAttributes.InternalCall);
            Reject(actual, "Context", Context(true)); Reject(actual, "Context", Context(false));
            Reject(coreModule, "AssemblyName", "Foreign.CoreLib"); Reject(owner, "Module", appModule);
            Reject(owner, "GenericArity", 1); Reject(owner, "IsInterface", true); Reject(owner, "IsValueType", false);
            var result = Get(Get(actual, "Signature"), "ReturnType");
            Reject(result, "Primitive", PrimitiveTypeCode.UInt32); Reject(result, "Kind", Enum.Parse(Model("TypeKind"), "ByRef"));
            Reject(parameters[0], "Kind", Enum.Parse(Model("TypeKind"), "ByRef"));
            var operand = Get(parameters[0], "Class");
            Reject(operand, "Module", appModule); Reject(operand, "GenericArity", 1);
            Reject(operand, "_fullName", "System.Numerics.Vector4");
        }
        Console.WriteLine("concrete-vector-reduction-guards:checks=" + checks);
        return true;
    }
    static string Key(int width, string name, string mutation) => width + "_" + name + "_" + mutation;
}
