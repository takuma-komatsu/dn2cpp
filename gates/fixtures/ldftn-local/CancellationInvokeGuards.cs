using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Cecil = Mono.Cecil;
using Cil = Mono.Cecil.Cil;

internal static class CancellationInvokeGuards
{
    public static bool Run(string[] args)
    {
        if (args.Length is not (5 or 6) || args[0] != "--cancellation-invoke-guards") return false;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        bool routes = args.Length == 6 && args[5] == "routes";
        string input = Path.GetFullPath(args[4]), corePath = Path.GetFullPath(args[3]);
        if (File.Exists(input)) throw new InvalidOperationException("guard metadata output must be new");
        string facadePath = Path.Combine(Path.GetDirectoryName(corePath)!, "System.Runtime.dll");
        string[] mutations = { "valid", "forwarded", "forwarded-callback", "forwarded-source", "instance", "explicit-this", "vararg", "generic",
            "wrong-return", "modified-return", "byref-callback", "modified-callback", "array-callback", "value-callback", "wrong-callback",
            "byref-state", "wrong-state", "byref-source", "modified-source", "array-source", "value-source", "wrong-source",
            "foreign-callback", "foreign-source", "foreign-owner", "extra", "missing", "type-context", "method-context" };
        using (var core = Cecil.AssemblyDefinition.ReadAssembly(corePath))
        using (var facade = Cecil.AssemblyDefinition.ReadAssembly(facadePath))
        using (var app = Cecil.AssemblyDefinition.CreateAssembly(new Cecil.AssemblyNameDefinition("CancellationInvokeGuards", new Version(1, 0)), "CancellationInvokeGuards", Cecil.ModuleKind.Dll))
        {
            var module = app.MainModule;
            var host = new Cecil.TypeDefinition("Fixture", "Cases", Cecil.TypeAttributes.Public, module.TypeSystem.Object);
            module.Types.Add(host); host.GenericParameters.Add(new Cecil.GenericParameter("T", host));
            var definition = core.MainModule.GetType("System.Threading.CancellationTokenSource").Methods.Single(m => m.Name == "Invoke");
            Cecil.TypeReference Named(string space, string name, Cecil.AssemblyNameReference scope)
            {
                if (!module.AssemblyReferences.Contains(scope)) module.AssemblyReferences.Add(scope);
                return new Cecil.TypeReference(space, name, module, scope);
            }
            foreach (string mutation in mutations)
            {
                var method = new Cecil.MethodDefinition(mutation, Cecil.MethodAttributes.Public | Cecil.MethodAttributes.Static, module.TypeSystem.Void);
                host.Methods.Add(method); method.GenericParameters.Add(new Cecil.GenericParameter("U", method));
                var target = new Cecil.MethodReference(definition.Name, module.TypeSystem.Void, module.ImportReference(definition.DeclaringType));
                foreach (var parameter in definition.Parameters) target.Parameters.Add(new Cecil.ParameterDefinition(module.ImportReference(parameter.ParameterType)));
                switch (mutation)
                {
                    case "forwarded":
                    case "forwarded-callback":
                    case "forwarded-source":
                        var scope = new Cecil.AssemblyNameReference(facade.Name.Name, facade.Name.Version) { PublicKeyToken = facade.Name.PublicKeyToken, Culture = facade.Name.Culture };
                        if (mutation != "forwarded-source") target.Parameters[0].ParameterType = Named("System", "Delegate", scope);
                        if (mutation != "forwarded-callback") target.Parameters[2].ParameterType = Named("System.Threading", "CancellationTokenSource", scope);
                        break;
                    case "instance": target.HasThis = true; break;
                    case "explicit-this": target.HasThis = true; target.ExplicitThis = true; break;
                    case "vararg": target.CallingConvention = Cecil.MethodCallingConvention.VarArg; break;
                    case "generic": target.GenericParameters.Add(new Cecil.GenericParameter("G", target)); break;
                    case "wrong-return": target.ReturnType = module.TypeSystem.Int32; break;
                    case "modified-return": target.ReturnType = new Cecil.OptionalModifierType(module.TypeSystem.Int32, target.ReturnType); break;
                    case "byref-callback": target.Parameters[0].ParameterType = new Cecil.ByReferenceType(target.Parameters[0].ParameterType); break;
                    case "modified-callback": target.Parameters[0].ParameterType = new Cecil.RequiredModifierType(module.TypeSystem.Int32, target.Parameters[0].ParameterType); break;
                    case "array-callback": target.Parameters[0].ParameterType = new Cecil.ArrayType(target.Parameters[0].ParameterType); break;
                    case "value-callback": target.Parameters[0].ParameterType.IsValueType = true; break;
                    case "wrong-callback": target.Parameters[0].ParameterType = module.ImportReference(core.MainModule.GetType("System.MulticastDelegate")); break;
                    case "byref-state": target.Parameters[1].ParameterType = new Cecil.ByReferenceType(module.TypeSystem.Object); break;
                    case "wrong-state": target.Parameters[1].ParameterType = module.TypeSystem.String; break;
                    case "byref-source": target.Parameters[2].ParameterType = new Cecil.ByReferenceType(target.Parameters[2].ParameterType); break;
                    case "modified-source": target.Parameters[2].ParameterType = new Cecil.OptionalModifierType(module.TypeSystem.Int32, target.Parameters[2].ParameterType); break;
                    case "array-source": target.Parameters[2].ParameterType = new Cecil.ArrayType(target.Parameters[2].ParameterType); break;
                    case "value-source": target.Parameters[2].ParameterType.IsValueType = true; break;
                    case "wrong-source": target.Parameters[2].ParameterType = module.ImportReference(core.MainModule.GetType("System.Threading.CancellationToken")); break;
                    case "foreign-callback": target.Parameters[0].ParameterType = Named("System", "Delegate", new Cecil.AssemblyNameReference("ForeignDelegate", new Version(1, 0))); break;
                    case "foreign-source": target.Parameters[2].ParameterType = Named("System.Threading", "CancellationTokenSource", new Cecil.AssemblyNameReference("ForeignCancellation", new Version(1, 0))); break;
                    case "foreign-owner": target.DeclaringType = Named("System.Threading", "CancellationTokenSource", new Cecil.AssemblyNameReference("ForeignOwner", new Version(1, 0))); break;
                    case "extra": target.Parameters.Add(new Cecil.ParameterDefinition(module.TypeSystem.Int32)); break;
                    case "missing": target.Parameters.RemoveAt(2); break;
                    case "type-context": target.Parameters[0].ParameterType = host.GenericParameters[0]; break;
                    case "method-context": target.Parameters[0].ParameterType = method.GenericParameters[0]; break;
                }
                // Reference rows only: none of these metadata-holder bodies is executed.
                var il = method.Body.GetILProcessor(); il.Emit(Cil.OpCodes.Call, target); il.Emit(Cil.OpCodes.Ret);
            }
            foreach (var constructor in core.MainModule.GetType("System.Threading.CancellationToken").Methods.Where(m => m.IsConstructor && !m.IsStatic))
            {
                string name = constructor.Parameters[0].ParameterType.MetadataType == Cecil.MetadataType.Boolean ? "ctor-bool" : "ctor-source";
                var method = new Cecil.MethodDefinition(name, Cecil.MethodAttributes.Public | Cecil.MethodAttributes.Static, module.TypeSystem.Void);
                host.Methods.Add(method); var il = method.Body.GetILProcessor(); il.Emit(Cil.OpCodes.Call, module.ImportReference(constructor)); il.Emit(Cil.OpCodes.Ret);
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
        var appModule = modules.Single(m => (string)Get(m, "AssemblyName") == "CancellationInvokeGuards");
        var reader = (MetadataReader)Get(coreModule, "Reader"); var appReader = (MetadataReader)Get(appModule, "Reader");
        var predicate = Model("CoreIntrinsics").GetMethod("IsCancellationInvoke", statics)!;
        var raw = Model("CoreIntrinsics").GetMethod("IsCancellationInvokeSignature", statics)!;
        var shape = Model("CoreIntrinsics").GetMethod("TryReadCancellationInvokeSignature", statics)!;
        var ctorKind = Model("CoreIntrinsics").GetMethod("CancellationTokenConstructorKind", statics)!;
        bool Accepted(object? value) => value is not null && (bool)predicate.Invoke(null, new[] { value })!;
        unsafe bool Raw(object module, byte[] bytes, bool declaration)
        {
            fixed (byte* pointer = bytes)
                return (bool)(declaration ? raw : shape).Invoke(null, declaration
                    ? new object[] { module, new BlobReader(pointer, bytes.Length) }
                    : new object[] { module, new BlobReader(pointer, bytes.Length), default(EntityHandle), default(EntityHandle) })!;
        }
        int checks = 0;
        void Check(bool value, string label) { if (!value) throw new InvalidOperationException("cancellation invoke guard: " + label); checks++; }
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
        var actual = Invoke(resolve, tokens["valid"], emptyContext)!; Check(Accepted(actual), "genuine declaration");
        var savedSignature = Get(actual, "_sig"); Set(actual, "_sig", Activator.CreateInstance(savedSignature.GetType())!);
        Check(Accepted(actual) && !(bool)Get(actual, "SignatureReady"), "cold predicate does not decode"); Set(actual, "_sig", savedSignature);
        var definitionSignature = reader.GetBlobBytes(reader.GetMethodDefinition((MethodDefinitionHandle)Get(actual, "Handle")).Signature);
        Check(Raw(coreModule, definitionSignature, true), "raw genuine declaration");
        var parameters = ((IEnumerable)Get(Get(actual, "Signature"), "ParameterTypes")).Cast<object>().ToArray();
        var argument = Array.CreateInstance(Model("TypeDesc"), 1); argument.SetValue(parameters[0], 0);
        var empty = Array.CreateInstance(Model("TypeDesc"), 0);
        object Context(bool type) => Activator.CreateInstance(Model("GenericContext"), new object[] { type ? argument : empty, type ? empty : argument })!;
        foreach (string mutation in mutations)
        {
            var reference = appReader.GetMemberReference(tokens[mutation]); byte[] bytes = appReader.GetBlobBytes(reference.Signature);
            Check(!Raw(appModule, bytes, true), "reference not declaration " + mutation);
            Check(Raw(appModule, bytes, false) == (mutation is "valid" or "forwarded" or "forwarded-callback" or "forwarded-source"
                or "foreign-callback" or "foreign-source" or "foreign-owner"), "raw reference " + mutation);
            if (!routes) continue;
            var context = mutation == "type-context" ? Context(true) : mutation == "method-context" ? Context(false) : emptyContext;
            foreach (var resolver in new[] { resolve, function, call })
            {
                Check(Accepted(Invoke(resolve, tokens["valid"], emptyContext)), "valid before " + mutation);
                Check(mutation is "valid" or "forwarded" or "forwarded-callback" or "forwarded-source"
                    ? Accepted(Invoke(resolver, tokens[mutation], context)) : Refused(resolver, tokens[mutation], context), resolver.Name + " " + mutation);
                Check(Accepted(Invoke(resolve, tokens["valid"], emptyContext)), "valid after " + mutation);
            }
        }
        foreach (byte[] bytes in new[] { Array.Empty<byte>(), definitionSignature[..1], definitionSignature[..^1], definitionSignature.Concat(new byte[] { 0 }).ToArray() })
            Check(!Raw(coreModule, bytes, true), "truncated or trailing signature");
        foreach (int offset in Enumerable.Range(0, definitionSignature.Length))
        {
            var bytes = (byte[])definitionSignature.Clone(); bytes[offset] = 0xff;
            Check(!Raw(coreModule, bytes, true), "raw byte " + offset);
        }
        void Reject(object value, string field, object replacement)
        {
            var saved = Get(value, field); Set(value, field, replacement);
            try { Check(!Accepted(actual), "declaration " + field); }
            finally { Set(value, field, saved); }
            Check(Accepted(actual), "restored " + field);
        }
        var owner = Get(actual, "DeclaringClass");
        Reject(actual, "Handle", default(MethodDefinitionHandle)); Reject(actual, "Rva", 0); Reject(actual, "IsSynthetic", true);
        Reject(actual, "Attributes", MethodAttributes.Assembly | MethodAttributes.Static | MethodAttributes.HideBySig);
        Reject(actual, "ImplAttributes", MethodImplAttributes.AggressiveInlining); Reject(actual, "Context", Context(true)); Reject(actual, "Context", Context(false));
        Reject(owner, "Module", appModule); Reject(owner, "GenericArity", 1); Reject(owner, "Context", Context(true)); Reject(owner, "IsValueType", true);
        Reject(coreModule, "AssemblyName", "Foreign.CoreLib");
        foreach (int slot in new[] { 0, 2 })
        {
            var cls = Get(parameters[slot], "Class");
            Reject(cls, "Module", appModule); Reject(cls, "IsValueType", true); Reject(cls, "IsInterface", true); Reject(cls, "IsByRefLike", true);
            Reject(cls, "Context", Context(false)); Reject(parameters[slot], "Kind", Enum.Parse(Model("TypeKind"), "ByRef"));
            Reject(parameters[slot], "IsCanonPlaceholder", true);
        }
        int Kind(object signature) => (int)ctorKind.Invoke(null, new[] { signature })!;
        foreach (string name in new[] { "ctor-bool", "ctor-source" })
        {
            var method = Invoke(resolve, tokens[name], emptyContext)!; var signature = Get(method, "Signature");
            int expected = name == "ctor-bool" ? 1 : 2; Check(Kind(signature) == expected, "constructor " + name);
            object Changed(byte header, int required, int generic) => Activator.CreateInstance(signature.GetType(), new object[]
                { new SignatureHeader(header), Get(signature, "ReturnType"), required, generic, Get(signature, "ParameterTypes") })!;
            foreach (var changed in new[] { Changed(0, 1, 0), Changed(0x60, 1, 0), Changed(0x25, 1, 0), Changed(0x20, 0, 0), Changed(0x20, 1, 1) })
                Check(Kind(changed) == 0, "constructor header/count " + name);
            var parameter = ((IEnumerable)Get(signature, "ParameterTypes")).Cast<object>().Single();
            void Wrong(object value, string field, object replacement)
            {
                var saved = Get(value, field); Set(value, field, replacement);
                try { Check(Kind(signature) == 0, "constructor " + field); }
                finally { Set(value, field, saved); }
                Check(Kind(signature) == expected, "constructor restored " + field);
            }
            Wrong(parameter, "IsCanonPlaceholder", true); Wrong(parameter, "Kind", Enum.Parse(Model("TypeKind"), "ByRef"));
            if (name == "ctor-bool") Wrong(parameter, "Primitive", PrimitiveTypeCode.Int32);
            else
            {
                var cls = Get(parameter, "Class");
                Wrong(cls, "Module", appModule); Wrong(cls, "Handle", default(TypeDefinitionHandle)); Wrong(cls, "IsValueType", true);
                Wrong(cls, "IsInterface", true); Wrong(cls, "Context", Context(true)); Wrong(cls, "GenericArity", 1);
            }
        }
        var diet = Assembly.LoadFrom(Path.GetFullPath(args[2]));
        var copied = diet.GetType("Dn2Cpp.AssemblyDiet", true)!.GetMethod("IsCopiedCancellationInvoke", statics)!;
        using (var core = Cecil.AssemblyDefinition.ReadAssembly(corePath))
        {
            var method = core.MainModule.GetType("System.Threading.CancellationTokenSource").Methods.Single(m => m.Name == "Invoke");
            bool Copy() => (bool)copied.Invoke(null, new object[] { method, method.DeclaringType.FullName })!;
            Check(Copy(), "copied genuine");
            var result = method.ReturnType; method.ReturnType = core.MainModule.TypeSystem.Int32; Check(!Copy(), "copied return"); method.ReturnType = result;
            foreach (int slot in new[] { 0, 2 })
            {
                var parameter = method.Parameters[slot].ParameterType; method.Parameters[slot].ParameterType = new Cecil.ByReferenceType(parameter);
                Check(!Copy(), "copied byref " + slot); method.Parameters[slot].ParameterType = new Cecil.OptionalModifierType(core.MainModule.TypeSystem.Int32, parameter);
                Check(!Copy(), "copied modified " + slot); method.Parameters[slot].ParameterType = parameter;
            }
            var attributes = method.Attributes; method.IsStatic = false; Check(!Copy(), "copied instance"); method.Attributes = attributes;
            method.GenericParameters.Add(new Cecil.GenericParameter("T", method)); Check(!Copy(), "copied generic"); method.GenericParameters.Clear();
            Check(Copy(), "copied restored");
        }
        Console.WriteLine("cancellation-invoke-guards:checks=" + checks);
        return true;
    }
}
