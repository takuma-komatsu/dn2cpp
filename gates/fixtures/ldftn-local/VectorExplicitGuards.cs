using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

internal static class VectorExplicitGuards
{
    private delegate bool RawPredicate(BlobReader blob);

    public static bool Run(string[] args)
    {
        if (args.Length != 4 || args[0] != "--vector-explicit-guards") return false;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        var production = Assembly.LoadFrom(Path.GetFullPath(args[1]));
        const BindingFlags instance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        const BindingFlags statics = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        Type Model(string name) => production.GetType("Dn2Cpp." + name, true)!;
        object Get(object value, string name) => value.GetType().GetField(name, instance) is { } field ? field.GetValue(value)! : value.GetType().GetProperty(name, instance)!.GetValue(value)!;
        void Set(object value, string name, object? next)
        {
            if (value.GetType().GetField(name, instance) is { } field) field.SetValue(value, next);
            else value.GetType().GetProperty(name, instance)!.SetValue(value, next);
        }
        var options = Activator.CreateInstance(Model("TranspileOptions"))!;
        Set(options, "Input", Path.GetFullPath(args[3])); Set(options, "UseILDiet", false); Set(options, "SharedGenerics", false);
        Set(options, "AutoRef", true); Set(options, "IsaSurfaceDump", "predicate-guard-only");
        object loaded;
        try { loaded = Model("Compilation").GetMethod("Create", statics)!.Invoke(null, new object[] { new[] { Path.GetFullPath(args[3]), Path.GetFullPath(args[2]) }, options })!; }
        catch (TargetInvocationException error) when (error.InnerException is not null) { throw error.InnerException; }
        var modules = ((IEnumerable)Get(loaded, "Modules")).Cast<object>().ToArray();
        var module = modules.Single(m => (string)Get(m, "AssemblyName") == "System.Private.CoreLib");
        var app = modules.Single(m => (string)Get(m, "AssemblyName") == "VectorProbe");
        var reader = (MetadataReader)Get(app, "Reader");
        var context = Model("GenericContext").GetField("Empty", statics)!.GetValue(null)!;
        var predicate = Model("CoreIntrinsics").GetMethod("IsVectorExplicit", statics)!;
        var raw = Model("CoreIntrinsics").GetMethod("IsVectorExplicitReference", statics)!.CreateDelegate<RawPredicate>();
        int checks = 0;
        void Check(bool result, string label)
        {
            if (!result) throw new InvalidOperationException("vector explicit guard: " + label);
            checks++;
        }
        var pairs = new HashSet<string>();
        object? actual = null;
        foreach (var handle in reader.MemberReferences)
        {
            var reference = reader.GetMemberReference(handle);
            if (!reader.StringComparer.Equals(reference.Name, "op_Explicit") || reference.Parent.Kind != HandleKind.TypeSpecification) continue;
            object resolved;
            try { resolved = Model("Compilation").GetMethod("ResolveMemberRefMethod", instance)!.Invoke(loaded, new[] { app, (object)handle, context })!; }
            catch (TargetInvocationException error) when (error.InnerException is not null) { throw new InvalidOperationException("reference " + MetadataTokens.GetToken(handle).ToString("X8") + " raw=" + Convert.ToHexString(reader.GetBlobBytes(reference.Signature)), error.InnerException); }
            Check((bool)predicate.Invoke(null, new[] { resolved })!, "resolved-CoreLib");
            Check(raw(reader.GetBlobReader(reference.Signature)), "reference-shape");
            var owner = Get(resolved, "DeclaringClass");
            var source = ((Array)Get(Get(owner, "Context"), "TypeArgs")).GetValue(0)!;
            var target = Get(Get(Get(resolved, "Signature"), "ReturnType"), "Class");
            var destination = ((Array)Get(Get(target, "Context"), "TypeArgs")).GetValue(0)!;
            string pair = Get(source, "Primitive") + ":" + Get(destination, "Primitive");
            pairs.Add(pair);
            if (pair == "UInt32:Byte") actual = resolved;
        }
        Check(pairs.Count == 144 && actual is not null, "all-twelve-by-twelve");
        object selected = actual!;
        var ownerClass = Get(selected, "DeclaringClass");
        var signature = Get(selected, "Signature");
        var sourceElement = ((Array)Get(Get(ownerClass, "Context"), "TypeArgs")).GetValue(0)!;
        var result = Get(signature, "ReturnType");
        var resultClass = Get(result, "Class");
        var resultElement = ((Array)Get(Get(resultClass, "Context"), "TypeArgs")).GetValue(0)!;
        var input = ((IEnumerable)Get(signature, "ParameterTypes")).Cast<object>().Single();
        void Reject(object value, string field, object next)
        {
            object saved = Get(value, field);
            Set(value, field, next);
            try { Check(!(bool)predicate.Invoke(null, new[] { selected })!, "wrong-" + field + ":" + next); }
            finally { Set(value, field, saved); }
        }
        Reject(module, "AssemblyName", "Application.CoreLib");
        Reject(ownerClass, "Module", app);
        Reject(ownerClass, "Handle", default(TypeDefinitionHandle));
        Reject(ownerClass, "IntrinsicCppName", "Dn2CppVector128");
        Reject(ownerClass, "IsValueType", false);
        Reject(ownerClass, "IsEnum", true);
        Reject(ownerClass, "IsInterface", true);
        Reject(ownerClass, "GenericArity", 0);
        Reject(selected, "Handle", default(MethodDefinitionHandle));
        Reject(selected, "Module", app);
        Reject(selected, "Rva", 0);
        Reject(selected, "Name", "op_Implicit");
        Reject(selected, "IsSynthetic", true);
        var attrs = (MethodAttributes)Get(selected, "Attributes");
        foreach (var flags in new[] { attrs & ~MethodAttributes.Static, attrs | MethodAttributes.Virtual, attrs | MethodAttributes.PinvokeImpl,
            attrs & ~MethodAttributes.SpecialName, attrs & ~MethodAttributes.Public }) Reject(selected, "Attributes", flags);
        Reject(selected, "ImplAttributes", MethodImplAttributes.InternalCall);
        foreach (var code in new[] { PrimitiveTypeCode.Boolean, PrimitiveTypeCode.Char, PrimitiveTypeCode.Object, PrimitiveTypeCode.String, PrimitiveTypeCode.Void })
        {
            Reject(sourceElement, "Primitive", code); Reject(resultElement, "Primitive", code);
        }
        foreach (string kind in new[] { "Class", "GenericVar", "Template", "ByRef", "Pointer" })
        {
            Reject(sourceElement, "Kind", Enum.Parse(Model("TypeKind"), kind));
            Reject(resultElement, "Kind", Enum.Parse(Model("TypeKind"), kind));
        }
        Reject(sourceElement, "IsCanonPlaceholder", true);
        Reject(resultElement, "IsCanonPlaceholder", true);
        Reject(input, "Class", resultClass);
        Reject(input, "Kind", Enum.Parse(Model("TypeKind"), "ByRef"));
        Reject(result, "Kind", Enum.Parse(Model("TypeKind"), "ByRef"));
        Reject(resultClass, "Module", app);
        Reject(resultClass, "Handle", default(TypeDefinitionHandle));
        var typeArray = Array.CreateInstance(Model("TypeDesc"), 1); typeArray.SetValue(sourceElement, 0);
        var emptyArray = Array.CreateInstance(Model("TypeDesc"), 0);
        object genericMethod = Activator.CreateInstance(Model("GenericContext"), new object[] { typeArray, typeArray })!;
        object openOwner = Activator.CreateInstance(Model("GenericContext"), new object[] { emptyArray, emptyArray })!;
        Reject(selected, "Context", genericMethod);
        Reject(ownerClass, "Context", openOwner);
        var vectors = new (string Hex, bool Expected)[]
        {
            ("00011511040105151104011300", true), ("000115110501181511050109", true),
            ("20011511040105151104011300", false), ("05011511040105151104011300", false),
            ("1000011511040105151104011300", false), ("00001511040105", false),
            ("00011511040102151104011300", false), ("00011511040103151104011300", false),
            ("0001101511040105151104011300", false), ("0001151104010510151104011300", false),
            ("00011511040105151104011301", false), ("00011511040105151104011e00", false),
            ("00011511000105151104011300", false), ("00011511060105151104011300", false),
            ("0001151104010515110401130000", false), ("000115110401051511040102", false),
            ("000115110401051511040103", false), ("0001151104010515110401", false), ("", false)
        };
        foreach (var vector in vectors)
        {
            var metadata = new MetadataBuilder();
            metadata.AddModule(0, metadata.GetOrAddString("VectorExplicitRaw"), metadata.GetOrAddGuid(Guid.Empty), default, default);
            var blob = metadata.GetOrAddBlob(Convert.FromHexString(vector.Hex));
            var builder = new BlobBuilder(); new MetadataRootBuilder(metadata).Serialize(builder, 0, 0);
            using var provider = MetadataReaderProvider.FromMetadataImage(builder.ToImmutableArray());
            Check(raw(provider.GetMetadataReader().GetBlobReader(blob)) == vector.Expected, "raw:" + vector.Hex);
        }
        Check((bool)predicate.Invoke(null, new[] { selected })!, "restored");
        Console.WriteLine("vector explicit guards passed pairs=" + pairs.Count + " checks=" + checks);
        return true;
    }
}
