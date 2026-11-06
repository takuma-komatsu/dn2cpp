using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Reflection.Metadata;
using Mono.Cecil;
using Assembly = System.Reflection.Assembly;
using AssemblyDefinition = Mono.Cecil.AssemblyDefinition;
using MethodAttributes = System.Reflection.MethodAttributes;

internal static class EnumStaticIntegerFormatGuards
{
    private const BindingFlags Static = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    private const BindingFlags Instance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private delegate bool Raw(MetadataReader reader, BlobReader blob);

    internal static unsafe bool Run(string[] args)
    {
        if (args.Length != 5 || args[0] != "--enum-static-integer-guards") return false;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        var assembly = Assembly.LoadFrom(Path.GetFullPath(args[1]));
        Type Model(string name) => assembly.GetType("Dn2Cpp." + name, true)!;
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
            if (!value) throw new InvalidOperationException("static enum guard: " + label);
            checks++;
        }
        var options = Activator.CreateInstance(Model("TranspileOptions"))!;
        string input = Path.GetFullPath(args[4]), corelib = Path.GetFullPath(args[2]);
        Set(options, "Input", input); Set(options, "UseILDiet", false); Set(options, "AutoRef", false);
        Set(options, "SharedGenerics", true); Set(options, "IsaSurfaceDump", "static-enum-metadata-only");
        var compilation = Model("Compilation").GetMethod("Create", Static)!.Invoke(null, new object[] { new[] { input, corelib }, options })!;
        var modules = ((IEnumerable)Get(compilation, "Modules")!).Cast<object>().ToArray();
        var module = modules.Single(m => (string)Get(m, "AssemblyName")! == "System.Private.CoreLib");
        var reader = (MetadataReader)Get(module, "Reader")!;
        var handle = reader.TypeDefinitions.Single(h => reader.GetString(reader.GetTypeDefinition(h).Namespace) == "System"
            && reader.GetString(reader.GetTypeDefinition(h).Name) == "Enum");
        var owner = ((IDictionary)Get(module, "ClassMap")!)[handle]!;
        var intrinsic = Model("CoreIntrinsics");
        var row = intrinsic.GetField("BrEnumStaticFormat", Static)!.GetValue(null)!;
        var matches = row.GetType().GetMethod("Matches")!;
        var raw = intrinsic.GetMethod("IsEnumStaticFormatSignature", Static)!.CreateDelegate<Raw>();
        var instantiate = Model("Compilation").GetMethod("InstantiateMethodOnClass", Instance)!;
        var link = Model("Compilation").GetMethod("LinkCanonicalMethodInstance", Instance)!;
        var type = Model("TypeDesc");
        object Primitive(PrimitiveTypeCode p) => type.GetMethod("MakePrimitive", Static)!.Invoke(null, new object[] { p })!;
        Array Types(params PrimitiveTypeCode[] codes)
        {
            var result = Array.CreateInstance(type, codes.Length);
            for (int i = 0; i < codes.Length; i++) result.SetValue(Primitive(codes[i]), i);
            return result;
        }
        (int, int, int, int) Cold()
        {
            var classes = ((IEnumerable)Get(compilation, "Classes")!).Cast<object>().ToArray();
            return (classes.Length, modules.Sum(m => ((IDictionary)Get(m, "MethodMap")!).Values.Cast<object>().Count(v => (bool)Get(v, "SignatureReady")!)),
                classes.Sum(c => ((IEnumerable)Get(c, "Fields")!).Cast<object>().Count(f => (bool)Get(f, "TypeReady")!)),
                classes.Count(c => (bool)Get(c, "MembersReady")!));
        }
        var pairs = new[] { (PrimitiveTypeCode.SByte, PrimitiveTypeCode.Byte), (PrimitiveTypeCode.Byte, PrimitiveTypeCode.Byte),
            (PrimitiveTypeCode.Int16, PrimitiveTypeCode.UInt16), (PrimitiveTypeCode.UInt16, PrimitiveTypeCode.UInt16),
            (PrimitiveTypeCode.Int32, PrimitiveTypeCode.UInt32), (PrimitiveTypeCode.UInt32, PrimitiveTypeCode.UInt32),
            (PrimitiveTypeCode.Int64, PrimitiveTypeCode.UInt64), (PrimitiveTypeCode.UInt64, PrimitiveTypeCode.UInt64) };
        var negativePairs = new[] { (PrimitiveTypeCode.Single, PrimitiveTypeCode.Single), (PrimitiveTypeCode.Double, PrimitiveTypeCode.Double),
            (PrimitiveTypeCode.IntPtr, PrimitiveTypeCode.UIntPtr), (PrimitiveTypeCode.UIntPtr, PrimitiveTypeCode.UIntPtr),
            (PrimitiveTypeCode.Char, PrimitiveTypeCode.Char), (PrimitiveTypeCode.Boolean, PrimitiveTypeCode.Byte),
            (PrimitiveTypeCode.Int64, PrimitiveTypeCode.UInt32), (PrimitiveTypeCode.Byte, PrimitiveTypeCode.SByte) };
        int definitions = 0, positive = 0, sharedNeighbor = 0;
        foreach (var methodHandle in reader.GetTypeDefinition(handle).GetMethods())
        {
            var definition = reader.GetMethodDefinition(methodHandle);
            string name = reader.GetString(definition.Name);
            if (name is not ("ToString" or "ToStringInlined") || (definition.Attributes & MethodAttributes.Static) == 0) continue;
            if (name == "ToStringInlined")
            {
                object neighbor = instantiate.Invoke(compilation, new object[] { owner, module, methodHandle, Types(PrimitiveTypeCode.Byte, PrimitiveTypeCode.Byte) })!;
                Check(!(bool)matches.Invoke(row, new[] { neighbor })!, "neighbor-not-intercepted");
                link.Invoke(compilation, new[] { neighbor });
                Check(Get(neighbor, "SharedOwner") is not null, "neighbor-sharing-retained"); sharedNeighbor++;
                continue;
            }
            definitions++;
            byte[] bytes = reader.GetBlobBytes(definition.Signature);
            void RawCheck(byte[] data, bool expected, string label)
            {
                fixed (byte* p = data) Check(raw(reader, new BlobReader(p, data.Length)) == expected, label);
            }
            RawCheck(bytes, true, "raw-authentic");
            for (int size = 0; size < bytes.Length; size++) RawCheck(bytes[..size], false, "raw-truncated");
            RawCheck(bytes.Concat(new byte[] { 0 }).ToArray(), false, "raw-trailing");
            foreach (int index in new[] { 0, 1, 2, 3, 4, bytes.Length - 2, bytes.Length - 1 })
            {
                byte[] changed = (byte[])bytes.Clone(); changed[index] = 0;
                RawCheck(changed, false, "raw-shape");
            }
            foreach (var (underlying, storage) in pairs.Concat(negativePairs))
            {
                bool expected = pairs.Contains((underlying, storage));
                object method = instantiate.Invoke(compilation, new object[] { owner, module, methodHandle, Types(underlying, storage) })!;
                bool Accept() => (bool)matches.Invoke(row, new[] { method })!;
                var cold = Cold(); bool ready = (bool)Get(method, "SignatureReady")!;
                Check(Accept() == expected, "cold-first"); Check(Accept() == expected, "cold-second");
                Check(Cold() == cold && (bool)Get(method, "SignatureReady")! == ready, "cold-purity");
                if (!expected) continue;
                positive++;
                method.GetType().GetMethod("EnsureSignature", Instance)!.Invoke(method, null);
                Check(Accept(), "decoded"); link.Invoke(compilation, new[] { method });
                Check(Get(method, "SharedOwner") is null, "closed-body-not-shared");
                void Reject(object target, string field, object? replacement)
                {
                    object? saved = Get(target, field); Set(target, field, replacement);
                    try { Check(!Accept(), field); } finally { Set(target, field, saved); }
                }
                Reject(method, "Name", "ToStringInlined"); Reject(module, "AssemblyName", "Application");
                Reject(method, "Attributes", definition.Attributes & ~MethodAttributes.Static);
                Reject(method, "ImplAttributes", System.Reflection.MethodImplAttributes.InternalCall);
                Reject(method, "Rva", 0); Reject(owner, "IsValueType", true); Reject(owner, "GenericArity", 1);
                var context = Get(method, "Context")!;
                Reject(method, "Context", Activator.CreateInstance(context.GetType(), new object[] { Types(), Types() })!);
                var methodArgs = (Array)Get(context, "MethodArgs")!;
                Reject(methodArgs.GetValue(0)!, "IsCanonPlaceholder", true);
                object signature = Get(method, "Signature")!;
                Reject(Get(signature, "ReturnType")!, "IsCanonPlaceholder", true);
            }
        }
        Check(definitions == 2 && positive == 16 && sharedNeighbor == 2, "exact-closed-and-neighbor-counts");
        intrinsic.GetMethod("VerifyInterceptRegistry", Static)!.Invoke(null, null);
        var diet = Assembly.LoadFrom(Path.GetFullPath(args[3])); var dietType = diet.GetType("Dn2Cpp.AssemblyDiet", true)!;
        var request = Activator.CreateInstance(diet.GetType("Dn2Cpp.DietRequest", true)!)!;
        using var traversal = (IDisposable)Activator.CreateInstance(dietType, Instance, null, new[] { request }, null)!;
        dietType.GetMethod("InitializeBudget", Instance)!.Invoke(traversal, null);
        var traverse = dietType.GetMethod("TraverseCopiedBody", Instance)!;
        using var core = AssemblyDefinition.ReadAssembly(corelib);
        foreach (var method in core.MainModule.GetType("System.Enum").Methods.Where(m => m.Name == "ToString" && m.IsStatic))
            Check((bool)traverse.Invoke(traversal, new object[] { method })!, "generic-definition-traversal-retained");
        var allocTokens = (string[])Model("Compilation").GetField("HotAllocTokens", Static)!.GetValue(null)!;
        Check(allocTokens.Contains("dn2cpp_enum_format_raw_integer("), "allocation-accounted");
        var appModule = modules.Single(m => (string)Get(m, "AssemblyName")! == "EnumOps");
        var appReader = (MetadataReader)Get(appModule, "Reader")!;
        var compiler = Model("MethodCompiler");
        var losslessReader = compiler.GetMethod("EnumMetadataMembersModel", Static)!;
        var switchReader = compiler.GetMethod("EnumMembersModel", Static)!;
        var sortedReader = Model("CppEmitter").GetMethod("SortedEnumMembers", Static)!;
        (long value, string name)[] Members(System.Reflection.MethodInfo source, object enumClass) =>
            ((IEnumerable)source.Invoke(null, new[] { enumClass })!).Cast<object>()
                .Select(v => ((long)Get(v, "Item1")!, (string)Get(v, "Item2")!)).ToArray();
        int enumFamilies = 0;
        foreach (string enumName in new[] { "I1", "U1", "I2", "U2", "I4", "U4", "I8", "U8", "Plain" })
        {
            var enumHandle = appReader.TypeDefinitions.Single(h =>
                appReader.GetString(appReader.GetTypeDefinition(h).Namespace) == "EnumStaticIntegerFormatSubset"
                && appReader.GetString(appReader.GetTypeDefinition(h).Name) == enumName);
            var enumClass = ((IDictionary)Get(appModule, "ClassMap")!)[enumHandle]!;
            var primitive = (PrimitiveTypeCode)Get(enumClass, "EnumUnderlying")!;
            bool wide = primitive is PrimitiveTypeCode.Int64 or PrimitiveTypeCode.UInt64;
            var declared = new List<(long value, string name)>();
            foreach (var fieldHandle in appReader.GetTypeDefinition(enumHandle).GetFields())
            {
                var field = appReader.GetFieldDefinition(fieldHandle);
                if ((field.Attributes & System.Reflection.FieldAttributes.Literal) == 0) continue;
                var constant = appReader.GetConstant(field.GetDefaultValue());
                var blob = appReader.GetBlobReader(constant.Value);
                long value = constant.TypeCode switch
                {
                    ConstantTypeCode.SByte => blob.ReadSByte(), ConstantTypeCode.Byte => blob.ReadByte(),
                    ConstantTypeCode.Int16 => blob.ReadInt16(), ConstantTypeCode.UInt16 => blob.ReadUInt16(),
                    ConstantTypeCode.Int32 => blob.ReadInt32(), ConstantTypeCode.UInt32 => blob.ReadUInt32(),
                    ConstantTypeCode.Int64 => blob.ReadInt64(), ConstantTypeCode.UInt64 => unchecked((long)blob.ReadUInt64()),
                    _ => throw new InvalidOperationException("Unexpected enum literal")
                };
                declared.Add((wide ? value : unchecked((int)value), appReader.GetString(field.Name)));
            }
            var all = Members(losslessReader, enumClass);
            var distinct = Members(switchReader, enumClass);
            var sorted = Members(sortedReader, enumClass);
            int bits = primitive switch
            {
                PrimitiveTypeCode.SByte or PrimitiveTypeCode.Byte => 8,
                PrimitiveTypeCode.Int16 or PrimitiveTypeCode.UInt16 => 16,
                PrimitiveTypeCode.Int32 or PrimitiveTypeCode.UInt32 => 32,
                _ => 64
            };
            ulong mask = bits == 64 ? ulong.MaxValue : (1UL << bits) - 1;
            Check(all.SequenceEqual(declared), "lossless-field-order-and-width-" + enumName);
            Check(distinct.SequenceEqual(declared.GroupBy(v => v.value).Select(g => g.First())), "switch-first-value-dedupe-" + enumName);
            Check(sorted.SequenceEqual(declared.OrderBy(v => unchecked((ulong)v.value) & mask)), "metadata-stable-unsigned-order-" + enumName);
            Check(all.Length == (enumName == "Plain" ? 4 : 6), "all-alias-count-" + enumName);
            Check(distinct.Length == all.Length - 1, "switch-distinct-count-" + enumName);
            Check(all.Any(v => v.name == "Alias"), "metadata-keeps-alias-" + enumName);
            Check(!distinct.Any(v => v.name == "Alias"), "switch-preserves-first-name-" + enumName);
            enumFamilies++;
        }
        Check(enumFamilies == 9, "exact-eight-widths-and-plain-metadata");
        Console.WriteLine($"enum-static-integer-guards={checks}:sixteen-closed,cold-purity,rare-negative,own-bodies,neighbor-sharing,copied-traversal,noalloc,lossless-nine,switch-dedupe");
        return true;
    }
}
