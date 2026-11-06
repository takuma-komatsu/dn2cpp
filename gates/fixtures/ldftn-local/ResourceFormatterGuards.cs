using System.Globalization;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Reflection.Metadata.Ecma335;
using Mono.Cecil;
using AssemblyDefinition = Mono.Cecil.AssemblyDefinition;

internal static class ResourceFormatterGuards
{
    private delegate bool Raw(MetadataReader reader, BlobReader blob, bool constructor, bool allowTypeReference);
    private const BindingFlags Static = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    public static bool Run(string[] args)
    {
        if (args.Length != 4 || args[0] != "--resource-formatter-guards") return false;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        var production = Assembly.LoadFrom(Path.GetFullPath(args[1]));
        var predicate = production.GetType("Dn2Cpp.ResourceFormatterContract", true)!.GetMethod("IsAccessor", Static)!;
        int checks = 0;
        void Inspect(Stream image, string name, bool expected, string label)
        {
            using var pe = new PEReader(image, PEStreamOptions.LeaveOpen);
            var reader = pe.GetMetadataReader();
            var owner = reader.TypeDefinitions.Single(h => reader.GetString(reader.GetTypeDefinition(h).Name) is "ResourceReader" or "OtherReader");
            var handle = reader.GetTypeDefinition(owner).GetMethods().Single(h => reader.GetString(reader.GetMethodDefinition(h).Name) == name);
            if ((bool)predicate.Invoke(null, new object[] { reader, handle })! != expected)
                throw new InvalidOperationException("resource-formatter guard: " + name + ":" + label);
            checks++;
        }
        Mono.Cecil.CustomAttribute Attr(Mono.Cecil.ICustomAttributeProvider p, string name) => p.CustomAttributes.Single(a => a.AttributeType.Name == name);
        foreach (string name in new[] { "CreateBinaryFormatter", "Deserialize" })
        {
            using (var image = File.OpenRead(args[2])) Inspect(image, name, true, "actual-installed");
            void Mutation(string label, Action<AssemblyDefinition, Mono.Cecil.MethodDefinition> edit, bool expected = false)
            {
                // Mutated CoreLib images are inspected in memory only, never loaded or executed.
                using var assembly = AssemblyDefinition.ReadAssembly(args[2], new ReaderParameters { InMemory = true });
                var method = assembly.MainModule.GetType("System.Resources.ResourceReader").Methods.Single(m => m.Name == name);
                edit(assembly, method);
                assembly.MainModule.Attributes |= ModuleAttributes.ILOnly;
                assembly.MainModule.Attributes &= ~ModuleAttributes.StrongNameSigned;
                using var memory = new MemoryStream();
                assembly.Write(memory, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
                memory.Position = 0;
                Inspect(memory, name, expected, label);
            }
            Mono.Cecil.ICustomAttributeProvider Target(Mono.Cecil.MethodDefinition m) => name == "CreateBinaryFormatter" ? m.MethodReturnType : m.Parameters[0];
            Mutation("unchanged-rewrite", (_, _) => { }, true);
            Mutation("assembly", (a, _) => a.Name.Name = "NotCoreLib");
            Mutation("owner", (_, m) => m.DeclaringType.Name = "OtherReader");
            Mutation("namespace", (_, m) => m.DeclaringType.Namespace = "Lookalike");
            Mutation("public", (_, m) => m.IsPublic = true);
            Mutation("instance", (_, m) => { m.IsStatic = false; m.HasThis = true; });
            Mutation("internalcall", (_, m) => m.ImplAttributes = Mono.Cecil.MethodImplAttributes.InternalCall);
            Mutation("generic", (_, m) => m.GenericParameters.Add(new Mono.Cecil.GenericParameter("T", m)));
            Mutation("generic-owner", (_, m) => m.DeclaringType.GenericParameters.Add(new Mono.Cecil.GenericParameter("T", m.DeclaringType)));
            Mutation("varargs", (_, m) => m.CallingConvention = MethodCallingConvention.VarArg);
            Mutation("has-body", (_, m) => { m.Body = new Mono.Cecil.Cil.MethodBody(m); m.Body.Instructions.Add(Mono.Cecil.Cil.Instruction.Create(Mono.Cecil.Cil.OpCodes.Ldnull)); m.Body.Instructions.Add(Mono.Cecil.Cil.Instruction.Create(Mono.Cecil.Cil.OpCodes.Ret)); });
            Mutation("return", (a, m) => m.ReturnType = a.MainModule.TypeSystem.String);
            Mutation("return-byref", (_, m) => m.ReturnType = new ByReferenceType(m.ReturnType));
            Mutation("return-modifier", (a, m) => m.ReturnType = new OptionalModifierType(a.MainModule.TypeSystem.Object, m.ReturnType));
            Mutation("arity", (a, m) => m.Parameters.Add(new ParameterDefinition(a.MainModule.TypeSystem.Object)));
            Mutation("missing-accessor", (_, m) => m.CustomAttributes.Remove(Attr(m, "UnsafeAccessorAttribute")));
            Mutation("duplicate-accessor", (_, m) => m.CustomAttributes.Add(Attr(m, "UnsafeAccessorAttribute")));
            Mutation("wrong-kind", (_, m) => { var a = Attr(m, "UnsafeAccessorAttribute"); a.ConstructorArguments[0] = new CustomAttributeArgument(a.ConstructorArguments[0].Type, 2); });
            Mutation("wrong-name", (a, m) => { var c = Attr(m, "UnsafeAccessorAttribute"); c.Properties.Clear(); c.Properties.Add(new Mono.Cecil.CustomAttributeNamedArgument("Name", new CustomAttributeArgument(a.MainModule.TypeSystem.String, "Other"))); });
            Mutation("missing-target", (_, m) => Target(m).CustomAttributes.Remove(Attr(Target(m), "UnsafeAccessorTypeAttribute")));
            Mutation("duplicate-target", (_, m) => Target(m).CustomAttributes.Add(Attr(Target(m), "UnsafeAccessorTypeAttribute")));
            Mutation("wrong-target", (a, m) => Attr(Target(m), "UnsafeAccessorTypeAttribute").ConstructorArguments[0] = new CustomAttributeArgument(a.MainModule.TypeSystem.String, "System.Runtime.Serialization.Formatters.Binary.BinaryFormatter, OtherAssembly"));
            Mutation("foreign-attribute", (a, m) =>
            {
                var old = Attr(Target(m), "UnsafeAccessorTypeAttribute");
                var scope = new AssemblyNameReference("Lookalike", new Version(1, 0)); a.MainModule.AssemblyReferences.Add(scope);
                var type = new Mono.Cecil.TypeReference("System.Runtime.CompilerServices", "UnsafeAccessorTypeAttribute", a.MainModule, scope);
                var ctor = new MethodReference(".ctor", a.MainModule.TypeSystem.Void, type) { HasThis = true };
                ctor.Parameters.Add(new ParameterDefinition(a.MainModule.TypeSystem.String));
                var replacement = new Mono.Cecil.CustomAttribute(ctor); replacement.ConstructorArguments.Add(old.ConstructorArguments[0]);
                Target(m).CustomAttributes.Remove(old); Target(m).CustomAttributes.Add(replacement);
            });
            Mutation("generic-accessor-attribute", (_, m) => { var t = Attr(m, "UnsafeAccessorAttribute").Constructor.Resolve().DeclaringType; t.GenericParameters.Add(new Mono.Cecil.GenericParameter("T", t)); });
            Mutation("generic-target-attribute", (_, m) => { var t = Attr(Target(m), "UnsafeAccessorTypeAttribute").Constructor.Resolve().DeclaringType; t.GenericParameters.Add(new Mono.Cecil.GenericParameter("T", t)); });
            Mutation("constructor-flags", (_, m) => Attr(m, "UnsafeAccessorAttribute").Constructor.Resolve().IsPrivate = true);
            Mutation("constructor-impl", (_, m) => Attr(m, "UnsafeAccessorAttribute").Constructor.Resolve().ImplAttributes = Mono.Cecil.MethodImplAttributes.NoInlining);
            Mutation("kind-not-enum", (a, _) => a.MainModule.GetType("System.Runtime.CompilerServices.UnsafeAccessorKind").BaseType = a.MainModule.TypeSystem.Object);
            Mutation("kind-int64", (a, _) => a.MainModule.GetType("System.Runtime.CompilerServices.UnsafeAccessorKind").Fields.Single(f => f.Name == "value__").FieldType = a.MainModule.TypeSystem.Int64);
            foreach (byte[] blob in new[] { Array.Empty<byte>(), new byte[] { 1, 0 }, new byte[] { 1, 0, 0, 0, 0, 0, 0, 0, 0 } })
                Mutation("malformed-attribute-" + blob.Length, (_, m) => { var old = Attr(m, "UnsafeAccessorAttribute"); m.CustomAttributes.Remove(old); m.CustomAttributes.Add(new Mono.Cecil.CustomAttribute(old.Constructor, blob)); });
            if (name == "Deserialize")
            {
                Mutation("formatter-byref", (_, m) => m.Parameters[0].ParameterType = new ByReferenceType(m.Parameters[0].ParameterType));
                Mutation("stream-byref", (_, m) => m.Parameters[1].ParameterType = new ByReferenceType(m.Parameters[1].ParameterType));
                Mutation("stream-modifier", (a, m) => m.Parameters[1].ParameterType = new RequiredModifierType(a.MainModule.TypeSystem.Object, m.Parameters[1].ParameterType));
                Mutation("generic-stream", (a, _) => { var t = a.MainModule.GetType("System.IO.Stream"); t.GenericParameters.Add(new Mono.Cecil.GenericParameter("T", t)); });
                Mutation("foreign-stream", (a, m) => { var scope = new AssemblyNameReference("ForeignStream", new Version(1, 0)); a.MainModule.AssemblyReferences.Add(scope); m.Parameters[1].ParameterType = new Mono.Cecil.TypeReference("System.IO", "Stream", a.MainModule, scope); });
                Mutation("stream-object", (a, m) => m.Parameters[1].ParameterType = a.MainModule.TypeSystem.Object);
                Mutation("misplaced-target", (_, m) => { var a = Attr(Target(m), "UnsafeAccessorTypeAttribute"); Target(m).CustomAttributes.Remove(a); m.Parameters[1].CustomAttributes.Add(a); });
            }
        }
        const BindingFlags instance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        Type Model(string name) => production.GetType("Dn2Cpp." + name, true)!;
        object Get(object value, string name) => value.GetType().GetField(name, instance) is { } field ? field.GetValue(value)! : value.GetType().GetProperty(name, instance)!.GetValue(value)!;
        void Set(object value, string name, object? next)
        {
            if (value.GetType().GetField(name, instance) is { } field) field.SetValue(value, next);
            else value.GetType().GetProperty(name, instance)!.SetValue(value, next);
        }
        string input = Path.GetFullPath(args[3]);
        if (File.Exists(input)) throw new InvalidOperationException("model input must be fresh");
        using (var empty = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition("ResourceFormatterGuardInput", new Version(1, 0)), "ResourceFormatterGuardInput", ModuleKind.Dll))
            empty.Write(input, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        var options = Activator.CreateInstance(Model("TranspileOptions"))!;
        Set(options, "Input", input); Set(options, "UseILDiet", false); Set(options, "SharedGenerics", false);
        Set(options, "AutoRef", false); Set(options, "IsaSurfaceDump", "predicate-guard-only");
        object loaded;
        try { loaded = Model("Compilation").GetMethod("Create", Static)!.Invoke(null, new object[] { new[] { input, Path.GetFullPath(args[2]) }, options })!; }
        catch (TargetInvocationException error) when (error.InnerException is not null) { throw error.InnerException; }
        var module = ((System.Collections.IEnumerable)Get(loaded, "Modules")).Cast<object>().Single(m => (string)Get(m, "AssemblyName") == "System.Private.CoreLib");
        var modelReader = (MetadataReader)Get(module, "Reader");
        var ownerHandle = modelReader.TypeDefinitions.Single(h => modelReader.GetString(modelReader.GetTypeDefinition(h).Name) == "ResourceReader");
        var modelPredicate = Model("CoreIntrinsics").GetMethod("IsResourceFormatterAccessor", Static)!;
        foreach (string name in new[] { "CreateBinaryFormatter", "Deserialize", "DeserializeObject", "<DeserializeObject>g__InitializeBinaryFormatter|4_0" })
        {
            var handle = modelReader.GetTypeDefinition(ownerHandle).GetMethods().Single(h => modelReader.GetString(modelReader.GetMethodDefinition(h).Name) == name);
            var actual = ((System.Collections.IDictionary)Get(module, "MethodMap"))[handle]!;
            bool expected = name is "CreateBinaryFormatter" or "Deserialize";
            if ((bool)modelPredicate.Invoke(null, new[] { actual })! != expected) throw new InvalidOperationException("resource formatter actual model: " + name);
            checks++;
            if (!expected) continue;
            void Reject(object value, string field, object next)
            {
                object saved = Get(value, field); Set(value, field, next);
                try
                {
                    if ((bool)modelPredicate.Invoke(null, new[] { actual })!) throw new InvalidOperationException("resource formatter model mutation: " + name + ":" + field);
                    checks++;
                }
                finally { Set(value, field, saved); }
            }
            Reject(module, "AssemblyName", "Lookalike.CoreLib");
            Reject(Get(actual, "DeclaringClass"), "_fullName", "Lookalike.ResourceReader");
            Reject(actual, "Name", "OtherAccessor");
            Reject(actual, "Handle", default(MethodDefinitionHandle));
            Reject(actual, "Rva", 1);
            Reject(actual, "Attributes", System.Reflection.MethodAttributes.Public | System.Reflection.MethodAttributes.Static | System.Reflection.MethodAttributes.HideBySig);
            Reject(actual, "ImplAttributes", System.Reflection.MethodImplAttributes.InternalCall);
            var result = Get(Get(actual, "Signature"), "ReturnType");
            Reject(result, "Kind", Enum.Parse(Model("TypeKind"), "ByRef"));
            Reject(result, "Primitive", PrimitiveTypeCode.String);
        }
        var raw = production.GetType("Dn2Cpp.ResourceFormatterContract", true)!.GetMethod("IsAccessorSignature", Static)!.CreateDelegate<Raw>();
        foreach (bool constructor in new[] { true, false })
            foreach (string hex in constructor
                ? new[] { "00001C", "", "00", "0000", "20001C", "05001C", "1000001C", "00001C00", "0000101C", "000020041C", "00011C1C", "0080001C" }
                : new[] { "00021C1C1204", "", "00021C1C12", "20021C1C1204", "05021C1C1204", "00021C1C120400", "00021C101C1204", "00021C1C101204", "00021C1C1F041204", "00021C1C1104", "00021C1C1200", "00021C1C12FF", "00021C1C0E" })
            {
                var builder = new MetadataBuilder();
                builder.AddModule(0, builder.GetOrAddString("Raw"), builder.GetOrAddGuid(Guid.Empty), default, default);
                builder.AddTypeDefinition(System.Reflection.TypeAttributes.Public, builder.GetOrAddString("System.IO"), builder.GetOrAddString("Stream"), default, default, default);
                var signature = builder.GetOrAddBlob(Convert.FromHexString(hex));
                var bytes = new BlobBuilder(); new MetadataRootBuilder(builder).Serialize(bytes, 0, 0);
                using var provider = MetadataReaderProvider.FromMetadataImage(bytes.ToImmutableArray());
                var reader = provider.GetMetadataReader();
                bool expected = hex == (constructor ? "00001C" : "00021C1C1204");
                if (raw(reader, reader.GetBlobReader(signature), constructor, false) != expected)
                    throw new InvalidOperationException("resource-formatter raw: " + hex);
                checks++;
            }
        Console.WriteLine("resource-formatter-contract-guards:" + checks);
        return true;
    }
}
