using System.Globalization;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using Mono.Cecil;
using AssemblyDefinition = Mono.Cecil.AssemblyDefinition;

internal static class DefaultPrincipalGuards
{
    private delegate bool Raw(BlobReader blob);
    private const BindingFlags Static = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    private const string Accessor = "<GetThreadPrincipal>g__GetDefaultPrincipal|107_0";
    public static bool Run(string[] args)
    {
        if (args.Length != 5 || args[0] != "--default-principal-guards") return false;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        var production = Assembly.LoadFrom(Path.GetFullPath(args[1]));
        var contract = production.GetType("Dn2Cpp.DefaultPrincipalContract", true)!;
        var isAccessor = contract.GetMethod("IsAccessor", Static)!;
        var hasAccessor = contract.GetMethod("HasAccessor", Static)!;
        var findTarget = contract.GetMethod("FindTarget", Static)!;
        var raw = contract.GetMethod("IsAccessorSignature", Static)!.CreateDelegate<Raw>();
        int checks = 0;
        void Check(bool value, string label)
        {
            if (!value) throw new InvalidOperationException("default-principal guard: " + label);
            checks++;
        }
        void Inspect(Stream image, bool expected, bool target, string label)
        {
            using var pe = new PEReader(image, PEStreamOptions.LeaveOpen);
            var reader = pe.GetMetadataReader();
            if (target)
            {
                var handle = (MethodDefinitionHandle)findTarget.Invoke(null, new object[] { reader })!;
                Check(!handle.IsNil == expected, label);
            }
            else
            {
                var handle = reader.MethodDefinitions.Single(h => reader.GetString(reader.GetMethodDefinition(h).Name) == Accessor);
                Check((bool)isAccessor.Invoke(null, new object[] { reader, handle })! == expected, label);
                Check((bool)hasAccessor.Invoke(null, new object[] { reader })! == expected, label + "-scan");
            }
        }
        using (var core = File.OpenRead(args[2])) Inspect(core, true, false, "actual-installed-accessor");
        using (var claims = File.OpenRead(args[3])) Inspect(claims, true, true, "actual-installed-target");

        const BindingFlags Instance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        Type Model(string name) => production.GetType("Dn2Cpp." + name, true)!;
        object Get(object value, string name) => value.GetType().GetField(name, Instance) is { } field ? field.GetValue(value)! : value.GetType().GetProperty(name, Instance)!.GetValue(value)!;
        void Set(object value, string name, object? fieldValue)
        {
            if (value.GetType().GetField(name, Instance) is { } field) field.SetValue(value, fieldValue);
            else value.GetType().GetProperty(name, Instance)!.SetValue(value, fieldValue);
        }
        string modelInput = Path.GetFullPath(args[4]);
        if (File.Exists(modelInput)) throw new InvalidOperationException("model input must be new");
        using (var empty = Mono.Cecil.AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition("DefaultPrincipalGuardInput", new Version(1, 0)), "DefaultPrincipalGuardInput", ModuleKind.Dll))
            empty.Write(modelInput, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        var options = Activator.CreateInstance(Model("TranspileOptions"))!;
        Set(options, "Input", modelInput); Set(options, "UseILDiet", false); Set(options, "SharedGenerics", false);
        Set(options, "AutoRef", false); Set(options, "IsaSurfaceDump", "predicate-guard-only");
        object loaded;
        try { loaded = Model("Compilation").GetMethod("Create", Static)!.Invoke(null, new object[] { new[] { modelInput, Path.GetFullPath(args[2]), Path.GetFullPath(args[3]) }, options })!; }
        catch (TargetInvocationException error) when (error.InnerException is not null) { throw error.InnerException; }
        var module = ((System.Collections.IEnumerable)Get(loaded, "Modules")).Cast<object>().Single(m => (string)Get(m, "AssemblyName") == "System.Private.CoreLib");
        var reader = (MetadataReader)Get(module, "Reader");
        var accessorHandle = reader.MethodDefinitions.Single(h => reader.GetString(reader.GetMethodDefinition(h).Name) == Accessor);
        var actual = ((System.Collections.IDictionary)Get(module, "MethodMap"))[accessorHandle]!;
        var modelPredicate = Model("CoreIntrinsics").GetMethod("IsDefaultPrincipalAccessor", Static)!;
        Check((bool)modelPredicate.Invoke(null, new[] { actual })!, "actual-loaded-MethodInfo-predicate");
        Console.WriteLine("default-principal-actual-model:" + MetadataTokens.GetToken(accessorHandle).ToString("X8", CultureInfo.InvariantCulture));
        foreach (var field in new[] { "Attributes", "ImplAttributes", "Rva" })
        {
            object saved = Get(actual, field);
            object invalid = field == "Attributes" ? (object)(System.Reflection.MethodAttributes.Public | System.Reflection.MethodAttributes.Static | System.Reflection.MethodAttributes.HideBySig)
                : field == "ImplAttributes" ? (object)System.Reflection.MethodImplAttributes.InternalCall : 1;
            Set(actual, field, invalid);
            try { Check(!(bool)modelPredicate.Invoke(null, new[] { actual })!, "actual-model-invalid-" + field); }
            finally { Set(actual, field, saved); }
        }

        // These full-module copies are inspected as metadata only, never executed.
        void Mutation(bool target, string label, Action<AssemblyDefinition, Mono.Cecil.MethodDefinition> edit, bool expected = false)
        {
            using var assembly = AssemblyDefinition.ReadAssembly(target ? args[3] : args[2], new ReaderParameters { InMemory = true });
            var type = assembly.MainModule.GetType(target ? "System.Security.Principal.GenericPrincipal" : "System.AppDomain");
            var method = type.Methods.Single(m => m.Name == (target ? "GetDefaultInstance" : Accessor));
            edit(assembly, method);
            assembly.MainModule.Attributes |= ModuleAttributes.ILOnly;
            assembly.MainModule.Attributes &= ~ModuleAttributes.StrongNameSigned;
            using var memory = new MemoryStream();
            assembly.Write(memory, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
            memory.Position = 0;
            Inspect(memory, expected, target, label);
        }
        Mutation(false, "unchanged-CoreLib-rewrite-positive", (_, _) => { }, true);
        Mutation(true, "unchanged-Claims-rewrite-positive", (_, _) => { }, true);
        Mutation(false, "assembly-name", (a, _) => a.Name.Name = "NotCoreLib");
        Mutation(false, "owner-name", (_, m) => m.DeclaringType.Name = "OtherAppDomain");
        Mutation(false, "public-not-private", (_, m) => m.Attributes = Mono.Cecil.MethodAttributes.Public | Mono.Cecil.MethodAttributes.Static | Mono.Cecil.MethodAttributes.HideBySig);
        Mutation(false, "internalcall", (_, m) => m.ImplAttributes = Mono.Cecil.MethodImplAttributes.InternalCall);
        Mutation(false, "has-body", (_, m) => { m.Body = new Mono.Cecil.Cil.MethodBody(m); m.Body.Instructions.Add(Mono.Cecil.Cil.Instruction.Create(Mono.Cecil.Cil.OpCodes.Ldnull)); m.Body.Instructions.Add(Mono.Cecil.Cil.Instruction.Create(Mono.Cecil.Cil.OpCodes.Ret)); });
        Mutation(false, "wrong-return", (a, m) => m.ReturnType = a.MainModule.TypeSystem.String);
        Mutation(false, "wrong-marker", (a, m) => m.Parameters[0].ParameterType = a.MainModule.TypeSystem.String);
        Mono.Cecil.CustomAttribute Attr(Mono.Cecil.ICustomAttributeProvider provider, string name) => provider.CustomAttributes.Single(a => a.AttributeType.Name == name);
        Mutation(false, "missing-generated", (_, m) => m.CustomAttributes.Remove(Attr(m, "CompilerGeneratedAttribute")));
        Mutation(false, "wrong-accessor-kind", (_, m) => { var a = Attr(m, "UnsafeAccessorAttribute"); a.ConstructorArguments[0] = new CustomAttributeArgument(a.ConstructorArguments[0].Type, 1); });
        Mutation(false, "wrong-accessor-name", (a, m) => { var c = Attr(m, "UnsafeAccessorAttribute"); c.Properties[0] = new Mono.Cecil.CustomAttributeNamedArgument("Name", new CustomAttributeArgument(a.MainModule.TypeSystem.String, "OtherFactory")); });
        foreach (bool result in new[] { false, true })
        {
            Mutation(false, "missing-target-" + result, (_, m) => { Mono.Cecil.ICustomAttributeProvider p = result ? m.MethodReturnType : m.Parameters[0]; p.CustomAttributes.Remove(Attr(p, "UnsafeAccessorTypeAttribute")); });
            Mutation(false, "wrong-target-" + result, (a, m) => { var c = Attr(result ? m.MethodReturnType : m.Parameters[0], "UnsafeAccessorTypeAttribute"); c.ConstructorArguments[0] = new CustomAttributeArgument(a.MainModule.TypeSystem.String, "System.Security.Principal.GenericPrincipal, OtherAssembly"); });
        }
        Mutation(false, "foreign-attribute-constructor", (a, m) =>
        {
            var old = Attr(m.Parameters[0], "UnsafeAccessorTypeAttribute");
            var scope = new AssemblyNameReference("AttributeLookalike", new Version(1, 0)); a.MainModule.AssemblyReferences.Add(scope);
            var type = new Mono.Cecil.TypeReference("System.Runtime.CompilerServices", "UnsafeAccessorTypeAttribute", a.MainModule, scope);
            var ctor = new MethodReference(".ctor", a.MainModule.TypeSystem.Void, type) { HasThis = true };
            ctor.Parameters.Add(new ParameterDefinition(a.MainModule.TypeSystem.String));
            var replacement = new Mono.Cecil.CustomAttribute(ctor); replacement.ConstructorArguments.Add(old.ConstructorArguments[0]);
            m.Parameters[0].CustomAttributes.Remove(old); m.Parameters[0].CustomAttributes.Add(replacement);
        });
        Mutation(false, "attribute-constructor-flags", (_, m) =>
        {
            var ctor = Attr(m, "UnsafeAccessorAttribute").Constructor.Resolve();
            ctor.Attributes = Mono.Cecil.MethodAttributes.Private | Mono.Cecil.MethodAttributes.HideBySig | Mono.Cecil.MethodAttributes.SpecialName | Mono.Cecil.MethodAttributes.RTSpecialName;
        });
        Mutation(false, "attribute-constructor-impl", (_, m) => Attr(m, "UnsafeAccessorAttribute").Constructor.Resolve().ImplAttributes = Mono.Cecil.MethodImplAttributes.NoInlining);
        Mutation(false, "accessor-kind-not-enum", (a, _) => a.MainModule.GetType("System.Runtime.CompilerServices.UnsafeAccessorKind").BaseType = a.MainModule.TypeSystem.Object);
        Mutation(false, "accessor-kind-int64", (a, _) => a.MainModule.GetType("System.Runtime.CompilerServices.UnsafeAccessorKind").Fields.Single(f => f.Name == "value__").FieldType = a.MainModule.TypeSystem.Int64);
        Mutation(true, "target-wrong-assembly", (a, _) => a.Name.Name = "TargetLookalike");
        Mutation(true, "target-wrong-return", (a, m) => m.ReturnType = a.MainModule.TypeSystem.Object);
        Mutation(true, "target-public", (_, m) => m.Attributes = Mono.Cecil.MethodAttributes.Public | Mono.Cecil.MethodAttributes.Static | Mono.Cecil.MethodAttributes.HideBySig);
        Mutation(true, "target-duplicate", (_, m) =>
        {
            var duplicate = new Mono.Cecil.MethodDefinition(m.Name, m.Attributes, m.ReturnType);
            duplicate.Body.Instructions.Add(Mono.Cecil.Cil.Instruction.Create(Mono.Cecil.Cil.OpCodes.Ldnull));
            duplicate.Body.Instructions.Add(Mono.Cecil.Cil.Instruction.Create(Mono.Cecil.Cil.OpCodes.Ret));
            m.DeclaringType.Methods.Add(duplicate);
        });
        foreach (string hex in new[] { "00011C1C", "20011C1C", "00001C", "00011C0E", "00010E1C", "1001011C1C", "0001101C1C", "00011C101C", "00011C1C00", "000112041C" })
        {
            var builder = new MetadataBuilder(); builder.AddModule(0, builder.GetOrAddString("Raw"), builder.GetOrAddGuid(Guid.Empty), default, default);
            var blob = builder.GetOrAddBlob(Convert.FromHexString(hex)); var bytes = new BlobBuilder(); new MetadataRootBuilder(builder).Serialize(bytes, 0, 0);
            using var provider = MetadataReaderProvider.FromMetadataImage(bytes.ToImmutableArray());
            Check(raw(provider.GetMetadataReader().GetBlobReader(blob)) == (hex == "00011C1C"), "raw-" + hex);
        }
        Console.WriteLine("default-principal-contract-guards:" + checks);
        return true;
    }
}
