using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

internal static class DtfITokenizeGuards
{
    private delegate bool Raw(BlobReader blob);
    public static bool Run(string[] args)
    {
        if (args.Length != 4 || args[0] != "--dtfi-tokenize-guards") return false;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        var production = Assembly.LoadFrom(Path.GetFullPath(args[1]));
        const BindingFlags instance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        const BindingFlags statics = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        Type Model(string name) => production.GetType("Dn2Cpp." + name, true)!;
        object Get(object value, string name) => value.GetType().GetField(name, instance) is { } f ? f.GetValue(value)! : value.GetType().GetProperty(name, instance)!.GetValue(value)!;
        void Set(object value, string name, object? next)
        {
            if (value.GetType().GetField(name, instance) is { } f) f.SetValue(value, next);
            else value.GetType().GetProperty(name, instance)!.SetValue(value, next);
        }
        string input = Path.GetFullPath(args[3]);
        Directory.CreateDirectory(Path.GetDirectoryName(input)!);
        using (var core = Mono.Cecil.AssemblyDefinition.ReadAssembly(args[2]))
        using (var app = Mono.Cecil.AssemblyDefinition.CreateAssembly(new Mono.Cecil.AssemblyNameDefinition("TokenizeGuardInput", new Version(1, 0)), "TokenizeGuardInput", Mono.Cecil.ModuleKind.Dll))
        {
            var host = new Mono.Cecil.TypeDefinition("", "Entry", Mono.Cecil.TypeAttributes.Public, app.MainModule.TypeSystem.Object);
            app.MainModule.Types.Add(host);
            var method = new Mono.Cecil.MethodDefinition("Reference", Mono.Cecil.MethodAttributes.Public | Mono.Cecil.MethodAttributes.Static, app.MainModule.TypeSystem.Void);
            host.Methods.Add(method);
            var target = app.MainModule.ImportReference(core.MainModule.GetType("System.Globalization.DateTimeFormatInfo").Methods.Single(m => m.Name == "Tokenize"));
            var il = method.Body.GetILProcessor();
            il.Emit(Mono.Cecil.Cil.OpCodes.Ldftn, target); il.Emit(Mono.Cecil.Cil.OpCodes.Pop); il.Emit(Mono.Cecil.Cil.OpCodes.Ret);
            app.Write(input);
        }
        var options = Activator.CreateInstance(Model("TranspileOptions"))!;
        Set(options, "Input", input); Set(options, "UseILDiet", false); Set(options, "SharedGenerics", false);
        Set(options, "AutoRef", false); Set(options, "IsaSurfaceDump", "predicate-only");
        var compilation = Model("Compilation").GetMethod("Create", statics)!.Invoke(null, new object[] { new[] { input, Path.GetFullPath(args[2]) }, options })!;
        var modules = ((IEnumerable)Get(compilation, "Modules")).Cast<object>().ToArray();
        var coreModule = modules.Single(m => (string)Get(m, "AssemblyName") == "System.Private.CoreLib");
        var appModule = modules.Single(m => (string)Get(m, "AssemblyName") == "TokenizeGuardInput");
        var reader = (MetadataReader)Get(coreModule, "Reader");
        var ownerHandle = reader.TypeDefinitions.Single(h => reader.GetString(reader.GetTypeDefinition(h).Name) == "DateTimeFormatInfo");
        var handle = reader.GetTypeDefinition(ownerHandle).GetMethods().Single(h => reader.GetString(reader.GetMethodDefinition(h).Name) == "Tokenize");
        var actual = ((IDictionary)Get(coreModule, "MethodMap"))[handle]!;
        var predicate = Model("CoreIntrinsics").GetMethod("IsDateTimeFormatInfoTokenize", statics)!;
        var decoded = Model("CoreIntrinsics").GetMethod("IsDateTimeFormatInfoTokenizeDecodedSignature", statics)!;
        var raw = Model("CoreIntrinsics").GetMethod("IsDateTimeFormatInfoTokenizeSignature", statics)!.CreateDelegate<Raw>();
        int count = 0;
        void Check(bool condition, string label) { if (!condition) throw new InvalidOperationException(label); count++; }
        Check((bool)predicate.Invoke(null, new[] { actual })!, "actual installed Tokenize");
        Check(raw(reader.GetBlobReader(reader.GetMethodDefinition(handle).Signature)), "actual raw");
        var signature = Get(actual, "Signature");
        Check((bool)decoded.Invoke(null, new[] { signature, coreModule })!, "actual decoded");
        var parameters = ((IEnumerable)Get(signature, "ParameterTypes")).Cast<object>().ToArray();
        var token = Get(parameters[0], "Class");
        Console.WriteLine("tokenize-enum-model:IsEnum=" + Get(token, "IsEnum") + ",IsValueType=" + Get(token, "IsValueType"));
        var ar = (MetadataReader)Get(appModule, "Reader");
        var mr = ar.MemberReferences.Single(h => ar.GetString(ar.GetMemberReference(h).Name) == "Tokenize");
        var context = Model("GenericContext").GetField("Empty", statics)!.GetValue(null)!;
        var resolved = Model("Compilation").GetMethod("ResolveMemberRefMethod", instance)!.Invoke(compilation, new[] { appModule, (object)mr, context })!;
        Check(ReferenceEquals(actual, resolved), "actual imported MemberRef identity");
        void Reject(object value, string name, object next)
        {
            object saved = Get(value, name); Set(value, name, next);
            try { Check(!(bool)predicate.Invoke(null, new[] { actual })!, "negative " + name); }
            finally { Set(value, name, saved); }
        }
        var owner = Get(actual, "DeclaringClass");
        Reject(owner, "Module", appModule); Reject(owner, "_fullName", "User.DateTimeFormatInfo");
        Reject(owner, "Handle", default(TypeDefinitionHandle)); Reject(owner, "IsValueType", true);
        Reject(owner, "GenericArity", 1); Reject(actual, "Rva", 0);
        Reject(actual, "Attributes", MethodAttributes.Public | MethodAttributes.HideBySig);
        Reject(actual, "Attributes", MethodAttributes.Assembly | MethodAttributes.HideBySig | MethodAttributes.Static);
        Reject(actual, "ImplAttributes", MethodImplAttributes.NoInlining);
        Reject(token, "Module", appModule); Reject(token, "IsEnum", false);
        Reject(token, "EnumUnderlying", PrimitiveTypeCode.Int64); Reject(token, "GenericArity", 1);
        var parser = Get(Get(parameters[3], "Element"), "Class");
        Reject(parser, "Module", appModule); Reject(parser, "IsValueType", false); Reject(parser, "IsEnum", true);
        Reject(parser, "Handle", default(TypeDefinitionHandle));
        var bytes = reader.GetBlobBytes(reader.GetMethodDefinition(handle).Signature);
        var vectors = new List<(byte[] Bytes, bool Expected)> { (bytes, true), (new byte[] { 0x20,4,2,0x11,5,0x10,0x11,9,0x10,8,0x10,0x11,13 }, true) };
        for (int i = 0; i < bytes.Length; i++) vectors.Add((bytes.Take(i).ToArray(), false));
        vectors.Add((bytes.Concat(new byte[] { 0 }).ToArray(), false));
        foreach (var (index, replacement) in new[] { (0,0), (1,3), (2,8), (3,0x12), (6,8), (7,0x12), (10,8), (11,0x0a), (12,8), (13,0x12) })
        { var bad = (byte[])bytes.Clone(); bad[index] = (byte)replacement; vectors.Add((bad, false)); }
        foreach (var vector in vectors)
        {
            var metadata = new MetadataBuilder();
            metadata.AddModule(0, metadata.GetOrAddString("TokenizeRaw"), metadata.GetOrAddGuid(Guid.Empty), default, default);
            var blob = metadata.GetOrAddBlob(vector.Bytes);
            var image = new BlobBuilder(); new MetadataRootBuilder(metadata).Serialize(image, 0, 0);
            using var source = MetadataReaderProvider.FromMetadataImage(image.ToImmutableArray());
            Check(raw(source.GetMetadataReader().GetBlobReader(blob)) == vector.Expected, "raw exact shape");
        }
        Console.WriteLine("dtfi-tokenize-guards:" + count);
        return true;
    }
}
