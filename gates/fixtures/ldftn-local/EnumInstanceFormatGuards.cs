using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Reflection.Metadata;
using Mono.Cecil;
using Assembly = System.Reflection.Assembly;
using AssemblyDefinition = Mono.Cecil.AssemblyDefinition;
using MethodAttributes = System.Reflection.MethodAttributes;
using MethodImplAttributes = System.Reflection.MethodImplAttributes;

internal static class EnumInstanceFormatGuards
{
    private const BindingFlags Static = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    private const BindingFlags Instance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private delegate bool Raw(MetadataReader reader, string name, MethodAttributes attributes, BlobReader blob);

    private static object GenericSignature<T>(object boxed)
    {
        var signature = (MethodSignature<T>)boxed;
        return new MethodSignature<T>(signature.Header, signature.ReturnType,
            signature.RequiredParameterCount, 1, signature.ParameterTypes);
    }

    public static unsafe bool Run(string[] args)
    {
        bool old = args.Length == 4 && args[0] == "--enum-instance-old-descriptor";
        if (!old && (args.Length != 5 || args[0] != "--enum-instance-format-guards")) return false;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        var production = Assembly.LoadFrom(Path.GetFullPath(args[1]));
        Type Model(string name) => production.GetType("Dn2Cpp." + name, true)!;
        object Get(object value, string name) => value.GetType().GetField(name, Instance) is { } field
            ? field.GetValue(value)! : value.GetType().GetProperty(name, Instance)!.GetValue(value)!;
        void Set(object value, string name, object? next)
        {
            if (value.GetType().GetField(name, Instance) is { } field) field.SetValue(value, next);
            else value.GetType().GetProperty(name, Instance)!.SetValue(value, next);
        }
        int checks = 0;
        void Check(bool success, string label)
        {
            if (!success) throw new InvalidOperationException("enum instance guard: " + label);
            checks++;
        }
        object options = Activator.CreateInstance(Model("TranspileOptions"))!;
        string corelib = Path.GetFullPath(args[2]), input = Path.GetFullPath(args[old ? 3 : 4]);
        Set(options, "Input", input); Set(options, "UseILDiet", false); Set(options, "AutoRef", false);
        Set(options, "SharedGenerics", false); Set(options, "IsaSurfaceDump", "enum-metadata-only");
        object compilation;
        try { compilation = Model("Compilation").GetMethod("Create", Static)!.Invoke(null, new object[] { new[] { input, corelib }, options })!; }
        catch (TargetInvocationException error) when (error.InnerException is not null) { throw error.InnerException; }
        var modules = ((IEnumerable)Get(compilation, "Modules")).Cast<object>().ToArray();
        var module = modules.Single(m => (string)Get(m, "AssemblyName") == "System.Private.CoreLib");
        var reader = (MetadataReader)Get(module, "Reader");
        var owner = reader.TypeDefinitions.Single(h => reader.GetString(reader.GetTypeDefinition(h).Namespace) == "System"
            && reader.GetString(reader.GetTypeDefinition(h).Name) == "Enum");
        var ownerClass = ((IDictionary)Get(module, "ClassMap"))[owner]!;
        var row = Model("CoreIntrinsics").GetField("BrEnumInstanceFormat", Static)!.GetValue(null)!;
        var matches = row.GetType().GetMethod("Matches")!;
        (int, int, int, int) Cold()
        {
            var classes = ((IEnumerable)Get(compilation, "Classes")).Cast<object>().ToArray();
            int signatures = modules.Sum(m => ((IDictionary)Get(m, "MethodMap")).Values.Cast<object>().Count(v => (bool)Get(v, "SignatureReady")));
            int fields = classes.Sum(c => ((IEnumerable)Get(c, "Fields")).Cast<object>().Count(f => (bool)Get(f, "TypeReady")));
            return (classes.Length, signatures, fields, classes.Count(c => (bool)Get(c, "MembersReady")));
        }
        int positives = 0, statics = 0;
        var raw = old ? null : Model("CoreIntrinsics").GetMethod("IsEnumInstanceFormatSignature", Static)!.CreateDelegate<Raw>();
        foreach (var handle in reader.GetTypeDefinition(owner).GetMethods())
        {
            var definition = reader.GetMethodDefinition(handle);
            string name = reader.GetString(definition.Name);
            if (name is not ("ToString" or "GetValue" or "Equals" or "GetHashCode" or "CompareTo" or "GetTypeCode")) continue;
            bool instance = (definition.Attributes & MethodAttributes.Static) == 0;
            if (old)
            {
                Check((bool)matches.Invoke(row, new object[] { "System.Enum", name })!, "legacy-name-match");
                if (!instance) statics++;
                continue;
            }
            object? method = ((IDictionary)Get(module, "MethodMap"))[handle];
            if (method is null)
            {
                method = Activator.CreateInstance(Model("MethodInfo"))!;
                Set(method, "DeclaringClass", ownerClass); Set(method, "Name", name); Set(method, "Handle", handle);
                Set(method, "Module", module); Set(method, "Attributes", definition.Attributes);
                Set(method, "ImplAttributes", definition.ImplAttributes); Set(method, "Rva", definition.RelativeVirtualAddress);
            }
            bool Accept() => (bool)matches.Invoke(row, new[] { method })!;
            var cold = Cold(); bool ready = (bool)Get(method, "SignatureReady");
            Check(Accept() == instance, "first-cold:" + name);
            Check(Accept() == instance, "second-cold:" + name);
            Check(Cold() == cold && (bool)Get(method, "SignatureReady") == ready, "cold-purity:" + name);
            byte[] bytes = reader.GetBlobBytes(definition.Signature);
            void RawCheck(byte[] data, bool expected, string label)
            {
                fixed (byte* pointer = data)
                    Check(raw!(reader, name, definition.Attributes, new BlobReader(pointer, data.Length)) == expected, label);
            }
            RawCheck(bytes, instance, "raw-actual:" + name);
            if (!instance) { statics++; continue; }
            positives++;
            foreach (byte header in new byte[] { 0, 0x10, 0x25, 0x60 })
            {
                byte[] changed = (byte[])bytes.Clone(); changed[0] = header;
                RawCheck(changed, false, "raw-header:" + name);
            }
            for (int length = 0; length < bytes.Length; length++) RawCheck(bytes[..length], false, "raw-truncated:" + name);
            RawCheck(bytes.Concat(new byte[] { 0 }).ToArray(), false, "raw-trailing:" + name);
            method.GetType().GetMethod("EnsureSignature", Instance)!.Invoke(method, null);
            Check(Accept(), "decoded:" + name);
            void Reject(object target, string field, object replacement)
            {
                object saved = Get(target, field); Set(target, field, replacement);
                try { Check(!Accept(), name + ":" + field); }
                finally { Set(target, field, saved); }
            }
            Reject(module, "AssemblyName", "Application");
            Reject(method, "Name", "NotEnumFormat");
            Reject(method, "Attributes", definition.Attributes | MethodAttributes.Static);
            Reject(method, "ImplAttributes", MethodImplAttributes.InternalCall);
            Reject(method, "Rva", 0);
            Reject(ownerClass, "IsValueType", true);
            Reject(ownerClass, "GenericArity", 1);
            object context = Get(method, "Context");
            Type argument = Model("TypeDesc");
            Array one = Array.CreateInstance(argument, 1), empty = Array.CreateInstance(argument, 0);
            one.SetValue(argument.GetMethod("MakePrimitive", Static)!.Invoke(null, new object[] { PrimitiveTypeCode.Byte }), 0);
            object generic = Activator.CreateInstance(context.GetType(), new object[] { empty, one })!;
            Reject(method, "Context", generic);
            object signature = Get(method, "Signature"), returnType = Get(signature, "ReturnType");
            object changedSignature = typeof(EnumInstanceFormatGuards).GetMethod(nameof(GenericSignature), Static)!
                .MakeGenericMethod(argument).Invoke(null, new[] { signature })!;
            Reject(method, "Signature", changedSignature);
            Reject(returnType, "IsCanonPlaceholder", true);
            if (Get(returnType, "Class") is { } resultClass)
                Reject(resultClass, "EnumUnderlying", PrimitiveTypeCode.UInt32);
        }
        if (old)
        {
            Check(statics == 2, "legacy-two-authentic-static-false-positives");
            Console.WriteLine("enum-instance-old-descriptor=two-authentic-static-generic-false-positives");
            return true;
        }
        Check(positives == 9 && statics == 2, "exact-nine-instances-two-statics");
        Model("CoreIntrinsics").GetMethod("VerifyInterceptRegistry", Static)!.Invoke(null, null);
        var diet = Assembly.LoadFrom(Path.GetFullPath(args[3]));
        var dietType = diet.GetType("Dn2Cpp.AssemblyDiet", true)!;
        var copied = dietType.GetMethod("IsCopiedEnumInstanceFormat", Static)!;
        var request = Activator.CreateInstance(diet.GetType("Dn2Cpp.DietRequest", true)!)!;
        using var traversal = (IDisposable)Activator.CreateInstance(dietType, Instance, null, new[] { request }, null)!;
        dietType.GetMethod("InitializeBudget", Instance)!.Invoke(traversal, null);
        var traverse = dietType.GetMethod("TraverseCopiedBody", Instance)!;
        using var core = AssemblyDefinition.ReadAssembly(corelib);
        int copiedPositives = 0, copiedStatics = 0;
        foreach (var method in core.MainModule.GetType("System.Enum").Methods)
        {
            bool named = method.Name is "ToString" or "GetValue" or "Equals" or "GetHashCode" or "CompareTo" or "GetTypeCode";
            bool expected = named && !method.IsStatic;
            bool Accept() => (bool)copied.Invoke(null, new object[] { method, "System.Enum" })!;
            Check(Accept() == expected, "copied:" + method.Name);
            if (named) Check((bool)traverse.Invoke(traversal, new object[] { method })! == method.IsStatic,
                "copied-real-body-traversal:" + method.Name);
            if (named && method.IsStatic) copiedStatics++;
            if (!expected) continue;
            copiedPositives++;
            var attributes = method.Attributes; method.IsStatic = true;
            Check(!Accept(), "copied-static"); method.Attributes = attributes;
            var result = method.ReturnType; method.ReturnType = new ByReferenceType(result);
            Check(!Accept(), "copied-return-byref"); method.ReturnType = result;
            var generic = new Mono.Cecil.GenericParameter("T", method); method.GenericParameters.Add(generic);
            Check(!Accept(), "copied-generic"); method.GenericParameters.Remove(generic);
            if (method.Parameters.Count > 0)
            {
                var first = method.Parameters[0].ParameterType;
                method.Parameters[0].ParameterType = new Mono.Cecil.TypeReference("System", "IFormatProvider", core.MainModule,
                    new AssemblyNameReference("Application", new Version(1, 0)));
                Check(!Accept(), "copied-foreign-provider"); method.Parameters[0].ParameterType = first;
            }
        }
        Check(copiedPositives == 9 && copiedStatics == 2, "copied-exact-family");
        Console.WriteLine($"enum-instance-guards={checks}:nine-instances,two-static-negatives,cold-purity,owned-shapes,copied-parity");
        return true;
    }
}
