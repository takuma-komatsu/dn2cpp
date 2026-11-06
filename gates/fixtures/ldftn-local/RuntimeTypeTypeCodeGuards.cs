using System.Collections.Immutable;
using System.Globalization;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;

internal static class RuntimeTypeTypeCodeGuards
{
    public static bool Run(string[] args)
    {
        if (args.Length != 2 || args[0] != "--runtime-type-type-code-guards") return false;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        var production = Assembly.LoadFrom(Path.GetFullPath(args[1]));
        const BindingFlags fields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        const BindingFlags statics = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        Type Model(string name) => production.GetType("Dn2Cpp." + name, throwOnError: true)!;
        object Make(string name) => RuntimeHelpers.GetUninitializedObject(Model(name));
        void Set(object value, string name, object? fieldValue)
        {
            if (value.GetType().GetField(name, fields) is { } field) field.SetValue(value, fieldValue);
            else (value.GetType().GetProperty(name, fields)
                ?? throw new InvalidOperationException("Missing model member " + name)).SetValue(value, fieldValue);
        }

        using var stream = File.OpenRead(typeof(object).Assembly.Location);
        using var pe = new PEReader(stream);
        var reader = pe.GetMetadataReader();
        object Module(string assembly = "System.Private.CoreLib")
        {
            var value = Make("Module");
            Set(value, "Reader", reader);
            Set(value, "AssemblyName", assembly);
            return value;
        }
        object Class(string fullName, object module)
        {
            var value = Make("ClassInfo");
            Set(value, "_fullName", fullName);
            Set(value, "Module", module);
            return value;
        }
        object ClassType(object? cls)
        {
            var value = Make("TypeDesc");
            Set(value, "Kind", Enum.Parse(Model("TypeKind"), "Class"));
            Set(value, "Class", cls);
            return value;
        }
        object Primitive(PrimitiveTypeCode code)
        {
            var value = Make("TypeDesc");
            Set(value, "Kind", Enum.Parse(Model("TypeKind"), "Primitive"));
            Set(value, "Primitive", code);
            return value;
        }
        object Wrapped(string kind, object element)
        {
            var value = Make("TypeDesc");
            Set(value, "Kind", Enum.Parse(Model("TypeKind"), kind));
            Set(value, "Element", element);
            return value;
        }
        var immutableRange = typeof(ImmutableArray).GetMethods().Single(m => m.Name == "CreateRange"
            && m.IsGenericMethodDefinition && m.GetParameters().Length == 1
            && m.GetParameters()[0].ParameterType.IsGenericType
            && m.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            .MakeGenericMethod(Model("TypeDesc"));
        var signatureType = typeof(MethodSignature<>).MakeGenericType(Model("TypeDesc"));
        object Signature(object result, object[] parameters, int generic, byte header)
        {
            var array = Array.CreateInstance(Model("TypeDesc"), parameters.Length);
            for (int i = 0; i < parameters.Length; i++) array.SetValue(parameters[i], i);
            return Activator.CreateInstance(signatureType, new[] { new SignatureHeader(header), result,
                parameters.Length, generic, immutableRange.Invoke(null, new object[] { array }) })!;
        }

        var intrinsics = Model("CoreIntrinsics");
        var predicate = intrinsics.GetMethod("IsRuntimeTypeTypeCode", statics)
            ?? throw new InvalidOperationException("Missing RuntimeType type-code predicate");
        var row = intrinsics.GetField("MdRuntimePrimitive", statics)!.GetValue(null)!;
        var rowMatches = row.GetType().GetMethod("Matches", new[] { Model("MethodInfo") })!;
        int checks = 0;
        void Check(bool success, string label)
        {
            if (!success) throw new InvalidOperationException("Type-code declaration guard: " + label);
            checks++;
        }
        Check(row.GetType().GetField("CutKind")!.GetValue(row)!.ToString() == "Cut"
            && row.GetType().GetField("EmitArm")!.GetValue(row)!.ToString() == "RuntimePrimitive",
            "descriptor owns cut and lowering");
        var runtimeType = typeof(object).Assembly.GetType("System.RuntimeType", throwOnError: true)!;
        var actual = runtimeType.GetMethod("GetTypeCodeImpl", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)!;
        var token = MetadataTokens.MethodDefinitionHandle(actual.MetadataToken & 0xffffff);
        var metadata = reader.GetMethodDefinition(token);
        const MethodImplAttributes mask = MethodImplAttributes.CodeTypeMask
            | MethodImplAttributes.ManagedMask | MethodImplAttributes.InternalCall;
        Check(runtimeType.IsSealed && actual.Attributes == (MethodAttributes.Family | MethodAttributes.Virtual | MethodAttributes.HideBySig)
            && actual.ReturnType == typeof(TypeCode) && actual.GetParameters().Length == 0 && !actual.IsGenericMethod
            && metadata.RelativeVirtualAddress != 0 && actual.GetMethodBody() is not null
            && (actual.GetMethodImplementationFlags() & mask) == MethodImplAttributes.IL
            && reader.GetBlobBytes(metadata.Signature).SequenceEqual(new byte[] { 0x20, 0, 0x11, 0x87, 0x04 }),
            "real CoreLib type-code override");

        object module = null!, owner = null!, method = null!, resultClass = null!;
        void Sig(object? result = null, object[]? parameters = null, int generic = 0, byte header = 0x20) =>
            Set(method, "Signature", Signature(result ?? ClassType(resultClass),
                parameters ?? Array.Empty<object>(), generic, header));
        void Reset()
        {
            module = Module();
            owner = Class("System.RuntimeType", module);
            resultClass = Class("System.TypeCode", module);
            Set(resultClass, "IsEnum", true);
            Set(resultClass, "EnumUnderlying", PrimitiveTypeCode.Int32);
            Set(owner, "IsSealed", true);
            method = Make("MethodInfo");
            Set(method, "Module", module);
            Set(method, "DeclaringClass", owner);
            Set(method, "Name", actual.Name);
            Set(method, "Attributes", actual.Attributes);
            Set(method, "ImplAttributes", actual.GetMethodImplementationFlags());
            Set(method, "Handle", token);
            Set(method, "Rva", metadata.RelativeVirtualAddress);
            Sig();
        }
        void Matches(bool expected, string label)
        {
            Check((bool)predicate.Invoke(null, new[] { method })! == expected, "predicate " + label);
            Check((bool)rowMatches.Invoke(row, new[] { method })! == expected, "descriptor " + label);
        }
        void Negative(string label, Action mutate)
        {
            Reset(); mutate(); Matches(false, label);
        }
        Reset(); Matches(true, "metadata-derived declaration");
        Negative("foreign assembly", () => Set(module, "AssemblyName", "Lookalike"));
        Negative("assembly case", () => Set(module, "AssemblyName", "system.private.corelib"));
        Negative("method foreign module", () => Set(method, "Module", Module("Lookalike")));
        Negative("method other CoreLib module", () => Set(method, "Module", Module()));
        Negative("owner foreign module", () => Set(owner, "Module", Module("Lookalike")));
        Negative("owner other CoreLib module", () => Set(owner, "Module", Module()));
        Negative("Type base", () => Set(owner, "_fullName", "System.Type"));
        Negative("TypeDelegator", () => Set(owner, "_fullName", "System.Reflection.TypeDelegator"));
        Negative("private raw owner", () => Set(owner, "_fullName", "System.RuntimeTypeHandle"));
        Negative("owner case", () => Set(owner, "_fullName", "system.runtimetype"));
        Negative("unsealed owner", () => Set(owner, "IsSealed", false));
        Negative("name", () => Set(method, "Name", "GetRuntimeTypeCode"));
        Negative("nil MethodDef", () => Set(method, "Handle", default(MethodDefinitionHandle)));
        Negative("return enum name", () => Set(resultClass, "_fullName", "System.Reflection.TypeAttributes"));
        Negative("return enum module", () => Set(resultClass, "Module", Module()));
        Negative("return not enum", () => Set(resultClass, "IsEnum", false));
        Negative("return enum width", () => Set(resultClass, "EnumUnderlying", PrimitiveTypeCode.Byte));
        Negative("name case", () => Set(method, "Name", "gettypecodeimpl"));
        Negative("missing body", () => Set(method, "Rva", 0));
        Negative("InternalCall", () => Set(method, "ImplAttributes", MethodImplAttributes.InternalCall));
        Negative("PInvoke descriptor", () => Set(method, "PInvoke", Make("PInvokeInfo")));
        for (int bit = 0; bit < 16; bit++)
        {
            var flag = (MethodAttributes)(1 << bit);
            Negative("attribute " + flag, () => Set(method, "Attributes", actual.Attributes ^ flag));
        }
        foreach (var impl in new[] { MethodImplAttributes.Native, MethodImplAttributes.Runtime, MethodImplAttributes.OPTIL, MethodImplAttributes.Unmanaged })
            Negative("implementation " + impl, () => Set(method, "ImplAttributes", impl));
        foreach (byte header in new byte[] { 0, 1, 2, 3, 4, 5, 0x10, 0x40, 0x60 })
            Negative("header " + header, () => Sig(header: header));
        Negative("generic", () => Sig(generic: 1));
        Negative("parameter", () => Sig(parameters: new[] { ClassType(owner) }));
        Negative("Int32 class", () => Sig(result: ClassType(Class("System.Int32", module))));
        Negative("missing class", () => Sig(result: ClassType(null)));
        foreach (var primitive in Enum.GetValues<PrimitiveTypeCode>())
            Negative("return primitive " + primitive, () => Sig(result: Primitive(primitive)));
        foreach (string kind in new[] { "ByRef", "Pointer", "SZArray", "MDArray" })
            Negative("wrapped return " + kind, () => Sig(result: Wrapped(kind, Primitive(PrimitiveTypeCode.Int32))));
        foreach (var hint in new[] { MethodImplAttributes.NoInlining, MethodImplAttributes.AggressiveInlining, MethodImplAttributes.NoOptimization })
        {
            Reset(); Set(method, "ImplAttributes", hint); Matches(true, "optimization hint " + hint);
        }
        Console.WriteLine("runtime-type-type-code-guards:True");
        Console.WriteLine("runtime-type-type-code-guard-checks:" + checks);
        return true;
    }
}
