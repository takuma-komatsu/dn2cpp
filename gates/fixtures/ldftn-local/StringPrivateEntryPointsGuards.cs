using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Reflection.Metadata;
using Mono.Cecil;
using Assembly = System.Reflection.Assembly;
using AssemblyDefinition = Mono.Cecil.AssemblyDefinition;
using MethodAttributes = System.Reflection.MethodAttributes;
using MethodImplAttributes = System.Reflection.MethodImplAttributes;

internal static class StringPrivateEntryPointsGuards
{
    private const BindingFlags Static = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    private const BindingFlags Instance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private delegate bool Raw(string name, BlobReader signature);

    private static object ChangedSignature<T>(object boxed, string change)
    {
        var s = (MethodSignature<T>)boxed;
        return new MethodSignature<T>(change == "header" ? new SignatureHeader(0x25) : s.Header, s.ReturnType,
            change == "required" ? s.RequiredParameterCount + 1 : s.RequiredParameterCount,
            change == "generic" ? 1 : s.GenericParameterCount, s.ParameterTypes);
    }

    public static unsafe bool Run(string[] args)
    {
        if (args.Length != 5 || args[0] != "--string-private-entrypoints-guards") return false;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        var production = Assembly.LoadFrom(Path.GetFullPath(args[1]));
        Type Model(string name) => production.GetType("Dn2Cpp." + name, true)!;
        object? Get(object value, string name) => value.GetType().GetField(name, Instance) is { } field
            ? field.GetValue(value) : value.GetType().GetProperty(name, Instance)!.GetValue(value);
        void Set(object value, string name, object? next)
        {
            if (value.GetType().GetField(name, Instance) is { } field) field.SetValue(value, next);
            else value.GetType().GetProperty(name, Instance)!.SetValue(value, next);
        }
        int checks = 0;
        void Check(bool value, string label)
        {
            if (!value) throw new InvalidOperationException("String private guard: " + label);
            checks++;
        }
        object options = Activator.CreateInstance(Model("TranspileOptions"))!;
        string corelib = Path.GetFullPath(args[2]), input = Path.GetFullPath(args[4]);
        Set(options, "Input", input); Set(options, "UseILDiet", false); Set(options, "AutoRef", false);
        Set(options, "SharedGenerics", false); Set(options, "IsaSurfaceDump", "string-private-metadata-only");
        object compilation;
        try { compilation = Model("Compilation").GetMethod("Create", Static)!.Invoke(null, new object[] { new[] { input, corelib }, options })!; }
        catch (TargetInvocationException e) when (e.InnerException is not null) { throw e.InnerException; }
        var modules = ((IEnumerable)Get(compilation, "Modules")!).Cast<object>().ToArray();
        var module = modules.Single(m => (string)Get(m, "AssemblyName")! == "System.Private.CoreLib");
        var reader = (MetadataReader)Get(module, "Reader")!;
        var handle = reader.TypeDefinitions.Single(h => reader.GetString(reader.GetTypeDefinition(h).Namespace) == "System"
            && reader.GetString(reader.GetTypeDefinition(h).Name) == "String");
        var owner = ((IDictionary)Get(module, "ClassMap")!)[handle]!;
        var coreType = Model("CoreIntrinsics");
        var classify = coreType.GetMethod("IsStringPrivateEntryPoint", Static)!;
        var isLength = coreType.GetMethod("IsStringNullTerminatedLength", Static)!;
        var row = coreType.GetField("BrStringRawByteReference", Static)!.GetValue(null)!;
        var matches = row.GetType().GetMethod("Matches")!;
        Check(Get(row, "CutKind")!.ToString() == "BodyReplace", "replacement-row-kind");
        var raw = coreType.GetMethod("IsStringPrivateEntryPointSignature", Static)!.CreateDelegate<Raw>();
        (int, int, int, int) Cold()
        {
            var classes = ((IEnumerable)Get(compilation, "Classes")!).Cast<object>().ToArray();
            int signatures = modules.Sum(m => ((IDictionary)Get(m, "MethodMap")!).Values.Cast<object>().Count(v => (bool)Get(v, "SignatureReady")!));
            int fields = classes.Sum(c => ((IEnumerable)Get(c, "Fields")!).Cast<object>().Count(f => (bool)Get(f, "TypeReady")!));
            return (classes.Length, signatures, fields, classes.Count(c => (bool)Get(c, "MembersReady")!));
        }
        int positives = 0, adjacent = 0;
        var ownedHandles = new List<MethodDefinitionHandle>();
        foreach (var mh in reader.GetTypeDefinition(handle).GetMethods())
        {
            var definition = reader.GetMethodDefinition(mh); string name = reader.GetString(definition.Name);
            bool wanted = name is "GetRawStringDataAsUInt8" or "wcslen";
            if (!wanted && name is not ("GetRawStringData" or "GetPinnableReference" or "CreateStringFromEncoding")) continue;
            object? method = ((IDictionary)Get(module, "MethodMap")!)[mh];
            if (method is null)
            {
                method = Activator.CreateInstance(Model("MethodInfo"))!;
                Set(method, "DeclaringClass", owner); Set(method, "Module", module); Set(method, "Name", name); Set(method, "Handle", mh);
                Set(method, "Attributes", definition.Attributes); Set(method, "ImplAttributes", definition.ImplAttributes); Set(method, "Rva", definition.RelativeVirtualAddress);
            }
            bool Accept() => (bool)classify.Invoke(null, new[] { method })!;
            var cold = Cold(); bool ready = (bool)Get(method, "SignatureReady")!;
            Check(Accept() == wanted, "cold-first:" + name); Check(Accept() == wanted, "cold-second:" + name);
            Check(Cold() == cold && (bool)Get(method, "SignatureReady")! == ready, "cold-purity:" + name);
            Check((bool)matches.Invoke(row, new[] { method })! == (name == "GetRawStringDataAsUInt8"), "raw-row:" + name);
            Check((bool)isLength.Invoke(null, new[] { method })! == (name == "wcslen"), "real-body:" + name);
            if (!wanted) { adjacent++; continue; }
            positives++; ownedHandles.Add(mh);
            byte[] bytes = reader.GetBlobBytes(definition.Signature);
            void RawCheck(byte[] data, bool expected, string label)
            {
                fixed (byte* p = data) Check(raw(name, new BlobReader(p, data.Length)) == expected, name + ":" + label);
            }
            RawCheck(bytes, true, "raw-original");
            for (int n = 0; n < bytes.Length; n++) RawCheck(bytes[..n], false, "truncated");
            RawCheck(bytes.Concat(new byte[] { 0 }).ToArray(), false, "trailing");
            for (int index = 0; index < bytes.Length; index++)
            {
                byte[] changed = (byte[])bytes.Clone(); changed[index] ^= 0x40;
                RawCheck(changed, false, "mutated-byte-" + index);
            }
            foreach (byte header in new byte[] { 0x10, 0x25, 0x60, 0x30 })
            {
                byte[] changed = (byte[])bytes.Clone(); changed[0] = header;
                RawCheck(changed, false, "header");
            }
            method.GetType().GetMethod("EnsureSignature", Instance)!.Invoke(method, null);
            Check(Accept(), "decoded:" + name);
            void Reject(object target, string field, object? replacement)
            {
                object? saved = Get(target, field); Set(target, field, replacement);
                try { Check(!Accept(), name + ":" + field); }
                finally { Set(target, field, saved); }
            }
            Reject(module, "AssemblyName", "Application"); Reject(method, "Name", "OtherStringMember");
            object application = modules.Single(m => (string)Get(m, "AssemblyName")! == "StringCore");
            Reject(method, "Module", application); Reject(owner, "Module", application);
            Reject(owner, "_fullName", "Application.String"); Reject(owner, "IsEnum", true);
            Reject(owner, "Handle", reader.TypeDefinitions.First(h => h != handle));
            Reject(method, "Handle", reader.GetTypeDefinition(handle).GetMethods().First(h => h != mh));
            Reject(owner, "Handle", System.Reflection.Metadata.Ecma335.MetadataTokens.TypeDefinitionHandle(reader.TypeDefinitions.Count + 1));
            Reject(method, "Handle", System.Reflection.Metadata.Ecma335.MetadataTokens.MethodDefinitionHandle(reader.MethodDefinitions.Count + 1));
            Reject(method, "Attributes", definition.Attributes ^ MethodAttributes.Static);
            Reject(method, "Attributes", definition.Attributes | MethodAttributes.Virtual);
            Reject(method, "ImplAttributes", MethodImplAttributes.InternalCall);
            Reject(method, "ImplAttributes", MethodImplAttributes.AggressiveInlining);
            Reject(method, "Rva", 0); Reject(method, "Rva", definition.RelativeVirtualAddress + 1); Reject(method, "IsSynthetic", true);
            Reject(owner, "IsValueType", true); Reject(owner, "IsInterface", true); Reject(owner, "IsByRefLike", true);
            Reject(owner, "IsSealed", false); Reject(owner, "IsAbstract", true); Reject(owner, "GenericArity", 1);
            Reject(owner, "Handle", default(TypeDefinitionHandle)); Reject(method, "Handle", default(MethodDefinitionHandle));
            object context = Get(method, "Context")!; Type argument = Model("TypeDesc");
            Array one = Array.CreateInstance(argument, 1), empty = Array.CreateInstance(argument, 0);
            one.SetValue(argument.GetMethod("MakePrimitive", Static)!.Invoke(null, new object[] { PrimitiveTypeCode.Byte }), 0);
            Reject(method, "Context", Activator.CreateInstance(context.GetType(), new object[] { empty, one }));
            Reject(method, "Context", Activator.CreateInstance(context.GetType(), new object[] { one, empty }));
            Reject(owner, "Context", Activator.CreateInstance(context.GetType(), new object[] { one, empty }));
            Reject(owner, "Context", Activator.CreateInstance(context.GetType(), new object[] { empty, one }));
            object signature = Get(method, "Signature")!;
            foreach (string change in new[] { "header", "generic", "required" })
                Reject(method, "Signature", typeof(StringPrivateEntryPointsGuards).GetMethod(nameof(ChangedSignature), Static)!
                    .MakeGenericMethod(argument).Invoke(null, new object[] { signature, change }));
            object result = Get(signature, "ReturnType")!;
            Reject(result, "IsCanonPlaceholder", true);
            if (name == "GetRawStringDataAsUInt8")
            {
                Reject(Get(result, "Element")!, "Primitive", PrimitiveTypeCode.Char);
                Reject(Get(result, "Element")!, "IsCanonPlaceholder", true);
            }
            else
            {
                object parameter = ((IEnumerable)Get(signature, "ParameterTypes")!).Cast<object>().Single();
                Reject(Get(parameter, "Element")!, "Primitive", PrimitiveTypeCode.Byte);
                Reject(parameter, "IsCanonPlaceholder", true);
            }
        }
        Check(positives == 2 && adjacent == 3, "exact-two-with-three-neighbors");
        owner.GetType().GetMethod("EnsureMembers", Instance)!.Invoke(owner, null);
        var resolve = Model("Compilation").GetMethod("ResolveCallTarget", Instance)!;
        foreach (var mh in ownedHandles)
        {
            EntityHandle entity = mh;
            var target = resolve.Invoke(compilation, new[] { module, (object)entity, Get(owner, "Context")! });
            Check(target is not null && (MethodDefinitionHandle)Get(target, "Handle")! == mh, "MethodDef-route");
        }
        coreType.GetMethod("VerifyInterceptRegistry", Static)!.Invoke(null, null);
        var diet = Assembly.LoadFrom(Path.GetFullPath(args[3]));
        Type dietType = diet.GetType("Dn2Cpp.AssemblyDiet", true)!;
        var copied = dietType.GetMethod("IsCopiedStringPrivateEntryPoint", Static)!;
        var request = Activator.CreateInstance(diet.GetType("Dn2Cpp.DietRequest", true)!)!;
        using var traversal = (IDisposable)Activator.CreateInstance(dietType, Instance, null, new[] { request }, null)!;
        dietType.GetMethod("InitializeBudget", Instance)!.Invoke(traversal, null);
        var traverse = dietType.GetMethod("TraverseCopiedBody", Instance)!;
        using var core = AssemblyDefinition.ReadAssembly(corelib);
        foreach (var method in core.MainModule.GetType("System.String").Methods)
        {
            bool wanted = method.Name is "GetRawStringDataAsUInt8" or "wcslen";
            bool Accept() => (bool)copied.Invoke(null, new object[] { method, "System.String" })!;
            Check(Accept() == wanted, "Cecil-all-String-members:" + method.Name);
            if (!wanted) continue;
            Check((bool)traverse.Invoke(traversal, new object[] { method })! == (method.Name == "wcslen"), "Cecil-body-route:" + method.Name);
            var attributes = method.Attributes; method.IsStatic = !method.IsStatic;
            Check(!Accept(), "Cecil-static"); method.Attributes = attributes;
            bool hasThis = method.HasThis; method.HasThis = !hasThis;
            Check(!Accept(), "Cecil-has-this"); method.HasThis = hasThis;
            var result = method.ReturnType; method.ReturnType = core.MainModule.TypeSystem.Int64;
            Check(!Accept(), "Cecil-return"); method.ReturnType = result;
            var generic = new Mono.Cecil.GenericParameter("T", method); method.GenericParameters.Add(generic);
            Check(!Accept(), "Cecil-generic"); method.GenericParameters.Remove(generic);
            if (method.Parameters.Count != 0)
            {
                var parameter = method.Parameters[0].ParameterType;
                method.Parameters[0].ParameterType = new PointerType(core.MainModule.TypeSystem.Byte);
                Check(!Accept(), "Cecil-pointee"); method.Parameters[0].ParameterType = parameter;
            }
        }
        Console.WriteLine($"string-private-guards={checks}:two-owned,cold-purity,raw-decoded,MethodDef,adjacent-negative,Cecil-copy");
        return true;
    }
}
