using System.Globalization;
using System.Collections;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;

internal static class CoreClrActivationInfoGuards
{
    private delegate bool RawPredicate(BlobReader blob);
    public static bool Run(string[] args)
    {
        if (args.Length is not (2 or 3) || args[0] != "--coreclr-activation-info-guards") return false;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        var production = Assembly.LoadFrom(Path.GetFullPath(args[1]));
        const BindingFlags fields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        const BindingFlags statics = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        Type Model(string name) => production.GetType("Dn2Cpp." + name, true)!;
        object Make(string name) => RuntimeHelpers.GetUninitializedObject(Model(name));
        void Set(object target, string name, object? value)
        {
            if (target.GetType().GetField(name, fields) is { } field) field.SetValue(target, value);
            else target.GetType().GetProperty(name, fields)!.SetValue(target, value);
        }
        object Get(object target, string name) => target.GetType().GetField(name, fields) is { } field
            ? field.GetValue(target)! : target.GetType().GetProperty(name, fields)!.GetValue(target)!;
        object Map(string owner, string field) => Activator.CreateInstance(Model(owner).GetField(field, fields)!.FieldType)!;
        var predicate = Model("CoreIntrinsics").GetMethod("IsCoreClrActivationInfo", statics)!;
        var rawPredicate = Model("CoreIntrinsics").GetMethod("IsCoreClrActivationInfoSignature", statics)!.CreateDelegate<RawPredicate>();
        var decode = typeof(MethodDefinition).GetMethods().Single(m => m.Name == "DecodeSignature").MakeGenericMethod(Model("TypeDesc"), typeof(object));
        int checks = 0;
        void Check(bool condition, string label) { if (!condition) throw new InvalidOperationException("Activation raw guard " + label); checks++; }
        string exact = "0006011208101B00011C0F01100F01101B0001011C101B00010110051002";
        var vectors = new List<(string Label, string Hex, bool Expected)> { ("exact", exact, true) };
        void Mutation(string label, string from, string to) => vectors.Add((label, exact.Replace(from, to, StringComparison.Ordinal), false));
        Mutation("allocator-unmanaged", "1B00011C", "1B01011C");
        Mutation("allocator-instance", "1B00011C", "1B20011C");
        Mutation("allocator-vararg", "1B00011C", "1B05011C");
        Mutation("allocator-generic", "1B00011C", "1B1001011C");
        Mutation("allocator-return", "1B00011C", "1B000118");
        Mutation("allocator-arg", "1B00011C0F01", "1B00011C18");
        Mutation("allocator-flat", "101B00011C0F01", "100F01");
        Mutation("allocator-pointer", "101B00011C0F01", "0F1B00011C0F01");
        Mutation("reference-arg", "1B0001011C", "1B00010118");
        Mutation("value-arg", "1B0001011005", "1B00010105");
        Mutation("value-ref-int", "1B0001011005", "1B0001011008");
        Mutation("value-modifier", "1B0001011005", "1B00010110200805");
        Mutation("bool-value", "1002", "02");
        Mutation("runtime-value", "1208", "1108");
        Mutation("runtime-modifier", "1208", "20081208");
        vectors.Add(("instance", "20" + exact.Substring(2), false));
        vectors.Add(("vararg", "05" + exact.Substring(2), false));
        vectors.Add(("trailing", exact + "02", false));
        // Parser-only invalid coded tokens must be rejected without metadata resolution.
        foreach (byte token in new byte[] { 0, 1, 2, 3, 7 })
        {
            var metadata = new MetadataBuilder();
            metadata.AddModule(0, metadata.GetOrAddString("RawGuard"), metadata.GetOrAddGuid(Guid.Empty), default, default);
            byte[] bytes = Convert.FromHexString(exact); bytes[4] = token;
            var blob = metadata.GetOrAddBlob(bytes); var image = new BlobBuilder(); new MetadataRootBuilder(metadata).Serialize(image, 0, 0);
            using var source = MetadataReaderProvider.FromMetadataImage(image.ToImmutableArray());
            Check(!rawPredicate(source.GetMetadataReader().GetBlobReader(blob)), "invalid-type-token-" + token);
        }
        if (args.Length == 3)
        {
            using var stream = File.OpenRead(args[2]);
            using var pe = new System.Reflection.PortableExecutable.PEReader(stream);
            var reader = pe.GetMetadataReader();
            var rth = reader.TypeDefinitions.Single(h => reader.GetString(reader.GetTypeDefinition(h).Namespace) == "System"
                && reader.GetString(reader.GetTypeDefinition(h).Name) == "RuntimeTypeHandle");
            var handle = reader.GetTypeDefinition(rth).GetMethods().Single(h => reader.GetString(reader.GetMethodDefinition(h).Name) == "GetActivationInfo"
                && (reader.GetMethodDefinition(h).Attributes & MethodAttributes.PinvokeImpl) != 0);
            var definition = reader.GetMethodDefinition(handle);
            var module = Make("Module"); Set(module,"Reader",reader); Set(module,"AssemblyName","System.Private.CoreLib");
            var owner = Make("ClassInfo"); Set(owner,"Module",module); Set(owner,"_fullName","System.RuntimeTypeHandle");
            var method = Make("MethodInfo"); Set(method,"Module",module); Set(method,"DeclaringClass",owner); Set(method,"Handle",handle);
            Set(method,"Name","GetActivationInfo"); Set(method,"Attributes",definition.Attributes); Set(method,"ImplAttributes",definition.ImplAttributes); Set(method,"Rva",definition.RelativeVirtualAddress);
            Check(!rawPredicate(reader.GetBlobReader(definition.Signature)),"actual-raw-import-signature");
            Check(!(bool)predicate.Invoke(null,new[]{method})!,"actual-raw-import-definition");
            var import=definition.GetImport();
            Check(reader.GetString(import.Name)=="RuntimeTypeHandle_GetActivationInfo"
                && reader.GetString(reader.GetModuleReference(import.Module).Name)=="QCall", "actual-raw-import-binding");
        }
        foreach (var vector in vectors)
        {
            var metadata = new MetadataBuilder();
            metadata.AddModule(0, metadata.GetOrAddString("Guard.dll"), metadata.GetOrAddGuid(Guid.Empty), default, default);
            metadata.AddAssembly(metadata.GetOrAddString("System.Private.CoreLib"), new Version(10,0,0,0), default, default, default, default);
            var fieldStart = MetadataTokens.FieldDefinitionHandle(1); var methodStart = MetadataTokens.MethodDefinitionHandle(1);
            metadata.AddTypeDefinition(TypeAttributes.NotPublic, default, metadata.GetOrAddString("<Module>"), default, fieldStart, methodStart);
            var rt = metadata.AddTypeDefinition(TypeAttributes.NotPublic | TypeAttributes.Sealed, metadata.GetOrAddString("System"), metadata.GetOrAddString("RuntimeType"), default, fieldStart, methodStart);
            var rth = metadata.AddTypeDefinition(TypeAttributes.Public | TypeAttributes.Sealed, metadata.GetOrAddString("System"), metadata.GetOrAddString("RuntimeTypeHandle"), default, fieldStart, methodStart);
            var handle = metadata.AddMethodDefinition(MethodAttributes.Assembly | MethodAttributes.Static | MethodAttributes.HideBySig, MethodImplAttributes.IL,
                metadata.GetOrAddString("GetActivationInfo"), metadata.GetOrAddBlob(Convert.FromHexString(vector.Hex)), 1, MetadataTokens.ParameterHandle(1));
            var image = new BlobBuilder(); new MetadataRootBuilder(metadata).Serialize(image, 0, 0);
            using var source = MetadataReaderProvider.FromMetadataImage(image.ToImmutableArray()); var reader = source.GetMetadataReader();
            var compilation = Make("Compilation"); var module = Make("Module");
            Set(module, "Reader", reader); Set(module, "AssemblyName", "System.Private.CoreLib"); Set(module, "Owner", compilation);
            var classes = (IDictionary)Map("Module", "ClassMap"); Set(module, "ClassMap", classes);
            object Class(TypeDefinitionHandle h, string name)
            {
                var cls = Make("ClassInfo"); Set(cls,"Handle",h); Set(cls,"Module",module); Set(cls,"_fullName",name); Set(cls,"IsSealed",true); classes.Add(h,cls); return cls;
            }
            Class(rt,"System.RuntimeType"); var owner = Class(rth,"System.RuntimeTypeHandle");
            var readers = (IDictionary)Map("Compilation", "_byReader"); readers.Add(reader,module); Set(compilation,"_byReader",readers);
            var provider = Activator.CreateInstance(Model("SignatureProvider"), fields, null, new[] {compilation}, null)!;
            var definition = reader.GetMethodDefinition(handle);
            var signature = decode.Invoke(definition,new object?[]{provider,null})!;
            var method = Make("MethodInfo"); Set(method,"DeclaringClass",owner); Set(method,"Module",module); Set(method,"Handle",handle);
            Set(method,"Name","GetActivationInfo"); Set(method,"Attributes",definition.Attributes); Set(method,"ImplAttributes",definition.ImplAttributes);
            Set(method,"Rva",1); Set(method,"Signature",signature);
            Check((bool)predicate.Invoke(null,new[]{method})! == vector.Expected, vector.Label);
            if (vector.Expected)
            {
                void Reject(string field, object value) { object old=Get(method,field); Set(method,field,value); Check(!(bool)predicate.Invoke(null,new[]{method})!,field+value); Set(method,field,old); }
                Reject("Attributes",MethodAttributes.Private|MethodAttributes.Static|MethodAttributes.HideBySig);
                Reject("Attributes",MethodAttributes.Assembly|MethodAttributes.HideBySig);
                Reject("Attributes",MethodAttributes.Private|MethodAttributes.Static|MethodAttributes.HideBySig|MethodAttributes.PinvokeImpl);
                Reject("ImplAttributes",MethodImplAttributes.InternalCall); Reject("ImplAttributes",MethodImplAttributes.PreserveSig);
                Reject("ImplAttributes",MethodImplAttributes.Synchronized); Reject("ImplAttributes",MethodImplAttributes.NoInlining);
                Reject("ImplAttributes",MethodImplAttributes.AggressiveInlining); Reject("Rva",0); Reject("Name","GetActivationOther");
                Set(module,"AssemblyName","Application"); Check(!(bool)predicate.Invoke(null,new[]{method})!,"foreign"); Set(module,"AssemblyName","System.Private.CoreLib");
            }
            Console.WriteLine(vector.Label + ":accepted=" + vector.Expected);
        }
        var hotCompilation=Make("Compilation"); var hotMethod=Make("MethodInfo"); Set(hotMethod,"_cppName","ActivationGuard");
        var facts=(IDictionary)Map("Compilation","HotBodyFacts"); Set(hotCompilation,"HotBodyFacts",facts);
        Model("Compilation").GetMethod("RecordHotBodyFacts",fields)!.Invoke(hotCompilation,new[]{hotMethod,"dn2cpp_throw_platform_not_supported(\"activation\");"});
        var fact=facts["ActivationGuard"]!;
        Check(!(bool)Get(fact,"Allocates") && !(bool)Get(fact,"Dispatches"),"existing-throw-policy");
        Console.WriteLine("activation-info-raw-guards="+checks);
        return true;
    }
}
