using System.Collections;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

internal static class VectorAlignmentGuards
{
    private delegate bool RawPredicate(BlobReader blob);
    public static bool Run(string[] args)
    {
        if (args.Length != 4 || args[0] != "--vector-alignment-guards") return false;
        string input = Path.GetFullPath(args[3]);
        if (File.Exists(input)) throw new InvalidOperationException("model app must be new");
        string[] mutations = { "valid", "instance", "vararg", "generic", "return", "byref", "modifier", "parameter", "owner", "spec" };
        using (var core = Mono.Cecil.AssemblyDefinition.ReadAssembly(args[2]))
        using (var app = Mono.Cecil.AssemblyDefinition.CreateAssembly(new Mono.Cecil.AssemblyNameDefinition("AlignmentGuards", new Version(1, 0)), "AlignmentGuards", Mono.Cecil.ModuleKind.Dll))
        {
            var module = app.MainModule;
            var host = new Mono.Cecil.TypeDefinition("Fixture", "Cases", Mono.Cecil.TypeAttributes.Public, module.TypeSystem.Object); module.Types.Add(host);
            var definition = core.MainModule.GetType("System.Numerics.Vector").Methods.Single(m => m.Name == "get_Alignment");
            foreach (string mutation in mutations)
            {
                var target = new Mono.Cecil.MethodReference(definition.Name, module.TypeSystem.Int32, module.ImportReference(definition.DeclaringType));
                switch (mutation)
                {
                    case "instance": target.HasThis = true; break;
                    case "vararg": target.CallingConvention = Mono.Cecil.MethodCallingConvention.VarArg; break;
                    case "generic": target.GenericParameters.Add(new Mono.Cecil.GenericParameter("T", target)); break;
                    case "return": target.ReturnType = module.TypeSystem.UInt32; break;
                    case "byref": target.ReturnType = new Mono.Cecil.ByReferenceType(target.ReturnType); break;
                    case "modifier": target.ReturnType = new Mono.Cecil.OptionalModifierType(module.TypeSystem.Object, target.ReturnType); break;
                    case "parameter": target.Parameters.Add(new Mono.Cecil.ParameterDefinition(module.TypeSystem.Int32)); break;
                    case "owner": var owner = new Mono.Cecil.GenericInstanceType(target.DeclaringType); owner.GenericArguments.Add(module.TypeSystem.Int32); target.DeclaringType = owner; break;
                    case "spec": var spec = new Mono.Cecil.GenericInstanceMethod(target); spec.GenericArguments.Add(module.TypeSystem.Int32); target = spec; break;
                }
                var method = new Mono.Cecil.MethodDefinition(mutation, Mono.Cecil.MethodAttributes.Public | Mono.Cecil.MethodAttributes.Static, module.TypeSystem.Void); host.Methods.Add(method);
                var il = method.Body.GetILProcessor(); il.Emit(Mono.Cecil.Cil.OpCodes.Call, target); il.Emit(Mono.Cecil.Cil.OpCodes.Pop); il.Emit(Mono.Cecil.Cil.OpCodes.Ret);
            }
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
        var compilation = Model("Compilation").GetMethod("Create", statics)!.Invoke(null, new object[] { new[] { input, Path.GetFullPath(args[2]) }, options })!;
        var modules = ((IEnumerable)Get(compilation, "Modules")).Cast<object>().ToArray();
        var coreModule = modules.Single(m => (string)Get(m, "AssemblyName") == "System.Private.CoreLib");
        var appModule = modules.Single(m => (string)Get(m, "AssemblyName") == "AlignmentGuards");
        var reader = (MetadataReader)Get(coreModule, "Reader");
        var ownerHandle = reader.TypeDefinitions.Single(h => { var t = reader.GetTypeDefinition(h); return reader.GetString(t.Namespace) == "System.Numerics" && reader.GetString(t.Name) == "Vector"; });
        var handle = reader.GetTypeDefinition(ownerHandle).GetMethods().Single(h => reader.GetString(reader.GetMethodDefinition(h).Name) == "get_Alignment");
        var actual = ((IDictionary)Get(coreModule, "MethodMap"))[handle]!;
        var ownerClass = Get(actual, "DeclaringClass");
        var predicate = Model("CoreIntrinsics").GetMethod("IsVectorAlignment", statics)!;
        var raw = Model("CoreIntrinsics").GetMethod("IsVectorAlignmentSignature", statics)!.CreateDelegate<RawPredicate>();
        bool Accepted(object? value) => value is not null && (bool)predicate.Invoke(null, new[] { value })!;
        int checks = 0;
        void Check(bool value, string label) { if (!value) throw new InvalidOperationException("vector alignment guard: " + label); checks++; }
        Check(Accepted(actual), "actual");
        Check(raw(reader.GetBlobReader(reader.GetMethodDefinition(handle).Signature)), "actual-raw");
        Check((bool)Model("CoreIntrinsics").GetMethod("IsVectorAlignmentDecodedSignature", statics)!.Invoke(null, new[] { Get(actual, "Signature") })!, "actual-decoded");
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
        foreach (string mutation in mutations)
        foreach (var resolver in mutation == "spec" ? new[] { specification, call } : new[] { resolve, function, call })
        {
            Check(Accepted(Invoke(resolve, tokens["valid"])), "good-before");
            Check(mutation == "valid" ? Accepted(Invoke(resolver, tokens[mutation])) : Refused(resolver, tokens[mutation]), "reference-" + mutation + "-" + resolver.Name);
            Check(Accepted(Invoke(resolve, tokens["valid"])), "good-after");
        }
        void Reject(object value, string field, object replacement, bool cache = false)
        {
            var saved = Get(value, field); Set(value, field, replacement);
            try
            {
                Check(!Accepted(actual), "declaration-" + field);
                if (cache) Check(Refused(resolve, tokens["valid"]), "cached-" + field);
            }
            finally { Set(value, field, saved); }
            Check(Accepted(actual), "restored-" + field);
        }
        Reject(coreModule, "AssemblyName", "Foreign.CoreLib");
        Reject(ownerClass, "Module", appModule); Reject(ownerClass, "_fullName", "Foreign.Vector"); Reject(ownerClass, "GenericArity", 1); Reject(ownerClass, "IsInterface", true); Reject(ownerClass, "IsValueType", false);
        Reject(actual, "Handle", default(MethodDefinitionHandle), true); Reject(actual, "Rva", 0, true); Reject(actual, "Name", "get_Count"); Reject(actual, "IsSynthetic", true, true);
        foreach (var flags in new[] { MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig | MethodAttributes.SpecialName, MethodAttributes.Assembly | MethodAttributes.HideBySig | MethodAttributes.SpecialName, MethodAttributes.Assembly | MethodAttributes.Static | MethodAttributes.HideBySig }) Reject(actual, "Attributes", flags, true);
        Reject(actual, "ImplAttributes", MethodImplAttributes.InternalCall, true);
        var result = Get(Get(actual, "Signature"), "ReturnType");
        Reject(result, "Primitive", PrimitiveTypeCode.UInt32, true); Reject(result, "Kind", Enum.Parse(Model("TypeKind"), "ByRef"), true);
        var argument = Array.CreateInstance(Model("TypeDesc"), 1); argument.SetValue(result, 0);
        var empty = Array.CreateInstance(Model("TypeDesc"), 0);
        foreach (bool type in new[] { false, true }) Reject(actual, "Context", Activator.CreateInstance(Model("GenericContext"), new object[] { type ? argument : empty, type ? empty : argument })!, true);
        foreach (string hex in new[] { "000008", "", "00", "0000", "000009", "200008", "050008", "10010008", "00010808", "00001008", "0000200508", "00000800" })
        {
            var metadata = new MetadataBuilder(); metadata.AddModule(0, metadata.GetOrAddString("AlignmentRaw"), metadata.GetOrAddGuid(Guid.Empty), default, default);
            var blob = metadata.GetOrAddBlob(Convert.FromHexString(hex)); var image = new BlobBuilder(); new MetadataRootBuilder(metadata).Serialize(image, 0, 0);
            using var source = MetadataReaderProvider.FromMetadataImage(image.ToImmutableArray());
            Check(raw(source.GetMetadataReader().GetBlobReader(blob)) == (hex == "000008"), "raw-" + hex);
        }
        // These are pinned metadata/body witnesses, not native external-call tests.
        // The CLR oracle independently checks their element and hardware contracts.
        using (var stream = File.OpenRead(args[2]))
        using (var pe = new PEReader(stream))
        using (var core = Mono.Cecil.AssemblyDefinition.ReadAssembly(args[2]))
        {
            var pinned = new (int Width, int Token, string[] Bodies)[]
            {
                (0, 0x06002E84, new[] { "28F72C00062A", "28700F000A2A", "28F82C00062A" }),
                (64, 0x06005430, new[] { "1E2A", "286C18000A2A", "162A" }),
                (128, 0x06004EAC, new[] { "1F102A", "28C814000A2A", "28194D00062A" }),
                (256, 0x0600508C, new[] { "1F202A", "28CA14000A2A", "28214F00062A" }),
                (512, 0x0600526B, new[] { "1F402A", "28CC14000A2A", "28015100062A" })
            };
            foreach (var family in pinned)
            {
                var type = core.MainModule.GetType(VectorAlignmentFixture.Owner(family.Width) + "`1");
                for (int index = 0; index < VectorAlignmentFixture.Properties.Length; index++)
                {
                    string property = VectorAlignmentFixture.Properties[index];
                    var bridge = type.Methods.Single(m => m.Name.EndsWith(".get_" + property, StringComparison.Ordinal));
                    string label = family.Width + ":" + property;
                    Check(bridge.MetadataToken.ToInt32() == family.Token + index, "bridge-token-" + label);
                    var def = reader.GetMethodDefinition((MethodDefinitionHandle)MetadataTokens.EntityHandle(family.Token + index));
                    Check((int)def.Attributes == 0x0891 && def.ImplAttributes == MethodImplAttributes.IL, "bridge-flags-" + label);
                    Check(Convert.ToHexString(reader.GetBlobBytes(def.Signature)) == (index == 2 ? "000002" : "000008"), "bridge-signature-" + label);
                    Check(Convert.ToHexString(pe.GetMethodBody(def.RelativeVirtualAddress).GetILBytes()!) == family.Bodies[index], "bridge-body-" + label);
                    Check(bridge.Overrides.Count == 1 && bridge.Overrides[0].Name == "get_" + property
                        && bridge.Overrides[0].DeclaringType is Mono.Cecil.GenericInstanceType slot
                        && slot.ElementType.FullName == "System.Runtime.Intrinsics.ISimdVector`2"
                        && slot.GenericArguments.Count == 2
                        && slot.GenericArguments[0] is Mono.Cecil.GenericInstanceType self
                        && self.ElementType.FullName == type.FullName
                        && self.GenericArguments.Single() is Mono.Cecil.GenericParameter { Position: 0, Type: Mono.Cecil.GenericParameterType.Type }
                        && slot.GenericArguments[1] is Mono.Cecil.GenericParameter { Position: 0, Type: Mono.Cecil.GenericParameterType.Type }, "bridge-methodimpl-" + label);
                    if (index == 1)
                    {
                        var count = (Mono.Cecil.MethodReference)bridge.Body.Instructions[0].Operand;
                        Check(count.Name == "get_Count" && count.Parameters.Count == 0 && !count.HasThis
                            && count.ReturnType.MetadataType == Mono.Cecil.MetadataType.Int32
                            && count.DeclaringType is Mono.Cecil.GenericInstanceType owner
                            && owner.ElementType.FullName == type.FullName
                            && owner.GenericArguments.Single() is Mono.Cecil.GenericParameter { Position: 0, Type: Mono.Cecil.GenericParameterType.Type }, "bridge-count-dependency-" + label);
                    }
                    else if (bridge.Body.Instructions[0].Operand is Mono.Cecil.MethodReference dependency)
                        Check(dependency.DeclaringType.FullName == VectorAlignmentFixture.Owner(family.Width)
                            && dependency.Name == "get_" + property && !dependency.HasThis && dependency.Parameters.Count == 0,
                            "bridge-facade-dependency-" + label);
                }
            }
        }
        Console.WriteLine("vector-alignment-guards:checks=" + checks);
        return true;
    }
}
