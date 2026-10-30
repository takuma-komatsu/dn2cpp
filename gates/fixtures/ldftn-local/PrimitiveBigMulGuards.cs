using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

internal static class PrimitiveBigMulGuards
{

    public static bool Run(string[] args)
    {
        if (args.Length != 4 || args[0] != "--primitive-bigmul-guards") return false;
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
        Set(options, "Input", Path.GetFullPath(args[3])); Set(options, "UseILDiet", false);
        Set(options, "SharedGenerics", false); Set(options, "AutoRef", false); Set(options, "IsaSurfaceDump", "predicate-guard-only");
        object loaded = Model("Compilation").GetMethod("Create", statics)!.Invoke(null,
            new object[] { new[] { Path.GetFullPath(args[3]), Path.GetFullPath(args[2]) }, options })!;
        var modules = ((IEnumerable)Get(loaded, "Modules")).Cast<object>().ToArray();
        var module = modules.Single(m => (string)Get(m, "AssemblyName") == "System.Private.CoreLib");
        var appModule = modules.Single(m => !ReferenceEquals(m, module));
        var reader = (MetadataReader)Get(module, "Reader");
        var predicate = Model("CoreIntrinsics").GetMethod("IsPrimitiveBigMul", statics)!;
        var descriptor = Model("CoreIntrinsics").GetField("BrPrimitiveBigMul", statics)!.GetValue(null)!;
        var matches = descriptor.GetType().GetMethod("Matches", instance)!;
        var empty = Array.CreateInstance(Model("TypeDesc"), 0);
        var instantiate = Model("Compilation").GetMethod("InstantiateMethodOnClass", instance)!;
        byte[] original = File.ReadAllBytes(args[2]);
        using var originalPe = new PEReader(new MemoryStream(original));
        int checks = 0;
        void Check(bool good, string label) { if (!good) throw new InvalidOperationException("Primitive BigMul guard: " + label); checks++; }
        foreach (string name in new[] { "Int32", "UInt32", "Int64", "UInt64", "IntPtr", "UIntPtr", "Int128", "UInt128" })
        {
            var ownerHandle = reader.TypeDefinitions.Single(h => reader.GetString(reader.GetTypeDefinition(h).Name) == name);
            var owner = ((IDictionary)Get(module, "ClassMap"))[ownerHandle]!;
            var handle = reader.GetTypeDefinition(ownerHandle).GetMethods().Single(h => reader.GetString(reader.GetMethodDefinition(h).Name) == "BigMul");
            var target = instantiate.Invoke(loaded, new object[] { owner, module, handle, empty })!;
            bool Accepted() => (bool)predicate.Invoke(null, new[] { target })!;
            bool expected = name is not ("Int128" or "UInt128");
            Check(Accepted() == expected, "actual " + name);
            Check((bool)matches.Invoke(descriptor, new[] { target })! == expected, "descriptor " + name);
            if (!expected) continue;
            void Reject(object obj, string field, object? next)
            {
                object previous = Get(obj, field);
                try { Set(obj, field, next); Check(!Accepted(), name + " " + field); }
                finally { Set(obj, field, previous); }
            }
            Reject(module, "AssemblyName", "Lookalike"); Reject(owner, "Module", appModule);
            Reject(owner, "IsValueType", false); Reject(owner, "IsEnum", true); Reject(owner, "IsInterface", true);
            Reject(owner, "GenericArity", 1); Reject(owner, "Handle", default(TypeDefinitionHandle));
            Reject(target, "Module", appModule); Reject(target, "Handle", default(MethodDefinitionHandle));
            Reject(target, "Name", "Multiply"); Reject(target, "Rva", 0);
            Reject(target, "Attributes", MethodAttributes.Public | MethodAttributes.HideBySig);
            Reject(target, "ImplAttributes", MethodImplAttributes.InternalCall);
            var signature = reader.GetMethodDefinition(handle).Signature;
            int blobOffset = originalPe.PEHeaders.MetadataStartOffset + reader.GetHeapMetadataOffset(HeapIndex.Blob) + MetadataTokens.GetHeapOffset(signature);
            int dataOffset = blobOffset + (original[blobOffset] < 0x80 ? 1 : original[blobOffset] < 0xc0 ? 2 : 4);
            var raw = reader.GetBlobBytes(signature);
            foreach (string mutation in new[] { "instance", "vararg", "generic", "arity", "result", "second", "tail" })
            {
                byte[] changed = (byte[])original.Clone();
                switch (mutation)
                {
                    case "instance": changed[dataOffset] = 0x20; break;
                    case "vararg": changed[dataOffset] = 5; break;
                    case "generic": changed[dataOffset] = 0x10; break;
                    case "arity": changed[dataOffset + 1] = 1; break;
                    case "result": changed[dataOffset + 2] = 8; break;
                    case "second": changed[dataOffset + raw.Length - (name.EndsWith("Ptr", StringComparison.Ordinal) ? 3 : 1)] ^= 1; break;
                    case "tail": changed[dataOffset + raw.Length - 1] = 0; break;
                }
                using var pe = new PEReader(new MemoryStream(changed));
                try { Set(module, "Reader", pe.GetMetadataReader()); Check(!Accepted(), name + " raw " + mutation); }
                finally { Set(module, "Reader", reader); }
            }
            if (name is "Int64" or "UInt64")
            {
                var resultHandle = reader.TypeDefinitions.Single(h => reader.GetString(reader.GetTypeDefinition(h).Name) == (name == "Int64" ? "Int128" : "UInt128"));
                foreach (var fieldHandle in reader.GetTypeDefinition(resultHandle).GetFields())
                {
                    var field = reader.GetFieldDefinition(fieldHandle);
                    if ((field.Attributes & FieldAttributes.Static) != 0) continue;
                    var blob = field.Signature;
                    int offset = originalPe.PEHeaders.MetadataStartOffset + reader.GetHeapMetadataOffset(HeapIndex.Blob) + MetadataTokens.GetHeapOffset(blob);
                    byte[] changed = (byte[])original.Clone(); changed[offset + 2] = 10;
                    using var pe = new PEReader(new MemoryStream(changed));
                    try { Set(module, "Reader", pe.GetMetadataReader()); Check(!Accepted(), name + " signed result field"); }
                    finally { Set(module, "Reader", reader); }
                }
            }
            Check(Accepted(), "restored " + name);
        }
        Console.WriteLine("primitive-bigmul-guards:" + checks);
        return true;
    }
}
