using System.Collections.Immutable;
using System.Globalization;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;

internal static class RuntimeTypeIsByRefGuards
{
    public static bool Run(string[] args)
    {
        if (args.Length != 2 || args[0] != "--runtime-type-is-by-ref-guards") return false;
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
        var helperPredicate = intrinsics.GetMethod("IsRuntimeTypeHandleIsByRef", statics)
            ?? throw new InvalidOperationException("Missing IsByRef production predicate");
        var getterPredicate = intrinsics.GetMethod("IsRuntimeTypeIsByRefOverride", statics)
            ?? throw new InvalidOperationException("Missing retained by-reference override predicate");
        var row = intrinsics.GetField("MdRuntimePrimitive", statics)!.GetValue(null)!;
        var rowMatches = row.GetType().GetMethod("Matches", new[] { Model("MethodInfo") })!;
        int checks = 0;
        void Check(bool success, string label)
        {
            if (!success) throw new InvalidOperationException("By-reference declaration guard: " + label);
            checks++;
        }
        Check(row.GetType().GetField("CutKind")!.GetValue(row)!.ToString() == "Cut"
            && row.GetType().GetField("EmitArm")!.GetValue(row)!.ToString() == "RuntimePrimitive",
            "descriptor names cut and lowering arm");

        var clrRuntimeType = typeof(object).Assembly.GetType("System.RuntimeType", throwOnError: true)!;
        var helper = typeof(RuntimeTypeHandle).GetMethod("IsByRef", statics, new[] { clrRuntimeType })!;
        var getter = clrRuntimeType.GetMethod("IsByRefImpl", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)!;
        const MethodImplAttributes implementationMask = MethodImplAttributes.CodeTypeMask
            | MethodImplAttributes.ManagedMask | MethodImplAttributes.InternalCall;
        foreach (bool publicGetter in new[] { false, true })
        {
            var actual = publicGetter ? getter : helper;
            byte expectedHeader = publicGetter ? (byte)0x20 : (byte)0;
            string expectedOwner = publicGetter ? "System.RuntimeType" : "System.RuntimeTypeHandle";
            var expectedAttributes = publicGetter
                ? MethodAttributes.Family | MethodAttributes.Virtual | MethodAttributes.HideBySig
                : MethodAttributes.Assembly | MethodAttributes.Static | MethodAttributes.HideBySig;
            var token = MetadataTokens.MethodDefinitionHandle(actual.MetadataToken & 0xffffff);
            var metadata = reader.GetMethodDefinition(token);
            Check(actual.Attributes == expectedAttributes && !actual.IsGenericMethod
                && actual.ReturnType == typeof(bool)
                && reader.GetBlobBytes(metadata.Signature)[0] == expectedHeader
                && metadata.RelativeVirtualAddress != 0
                && actual.GetMethodBody() is not null
                && (actual.GetMethodImplementationFlags() & implementationMask)
                    == MethodImplAttributes.IL
                && (publicGetter ? actual.GetParameters().Length == 0 && clrRuntimeType.IsSealed
                    : actual.GetParameters() is [{ ParameterType: var parameterType }] && parameterType == clrRuntimeType),
                actual.Name + " real CoreLib declaration");

            object module = null!, owner = null!, parameter = null!, result = null!, method = null!;
            void Sig(object? returnType = null, object[]? parameters = null, int generic = 0, byte? header = null) =>
                Set(method, "Signature", Signature(returnType ?? Primitive(PrimitiveTypeCode.Boolean),
                    parameters ?? (publicGetter ? Array.Empty<object>() : new[] { ClassType(parameter) }),
                    generic, header ?? expectedHeader));
            void Reset()
            {
                module = Module();
                owner = Class(expectedOwner, module);
                Set(owner, "IsSealed", publicGetter);
                parameter = Class("System.RuntimeType", module);
                result = Class("System.Boolean", module);
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
                var selected = publicGetter ? getterPredicate : helperPredicate;
                var other = publicGetter ? helperPredicate : getterPredicate;
                Check((bool)selected.Invoke(null, new[] { method })! == expected, actual.Name + " predicate " + label);
                Check(!(bool)other.Invoke(null, new[] { method })!, actual.Name + " other predicate " + label);
                Check((bool)rowMatches.Invoke(row, new[] { method })! == (!publicGetter && expected),
                    actual.Name + " descriptor " + label);
            }
            void Negative(string label, Action mutate)
            {
                Reset();
                mutate();
                Matches(false, label);
            }

            Reset(); Matches(true, "metadata-derived model");
            Negative("assembly", () => Set(module, "AssemblyName", "Lookalike"));
            Negative("assembly casing", () => Set(module, "AssemblyName", "system.private.corelib"));
            Negative("method foreign module", () => Set(method, "Module", Module("Lookalike")));
            Negative("method distinct CoreLib module", () => Set(method, "Module", Module()));
            Negative("owner foreign module", () => Set(owner, "Module", Module("Lookalike")));
            Negative("owner distinct CoreLib module", () => Set(owner, "Module", Module()));
            Negative("owner name", () => Set(owner, "_fullName", "System.Type"));
            Negative("owner casing", () => Set(owner, "_fullName", expectedOwner.ToLowerInvariant()));
            Negative("method name", () => Set(method, "Name", "Other"));
            Negative("method name casing", () => Set(method, "Name", actual.Name.ToLowerInvariant()));
            Negative("body availability", () => Set(method, "Rva", 0));
            Negative("InternalCall boundary", () => Set(method, "ImplAttributes",
                actual.GetMethodImplementationFlags() ^ MethodImplAttributes.InternalCall));
            Negative("PInvoke descriptor", () => Set(method, "PInvoke", Make("PInvokeInfo")));
            for (int bit = 0; bit < 16; bit++)
            {
                var flag = (MethodAttributes)(1 << bit);
                Negative("attribute " + flag, () => Set(method, "Attributes", actual.Attributes ^ flag));
            }
            foreach (var implementation in new[] { MethodImplAttributes.Native, MethodImplAttributes.Runtime,
                MethodImplAttributes.OPTIL, MethodImplAttributes.Unmanaged })
                Negative("implementation " + implementation,
                    () => Set(method, "ImplAttributes", actual.GetMethodImplementationFlags() | implementation));
            foreach (byte header in new byte[] { 0, 1, 2, 3, 4, 5, 0x10, 0x20, 0x40, 0x60 })
                if (header != expectedHeader) Negative("signature header " + header, () => Sig(header: header));
            Negative("generic arity", () => Sig(generic: 1));
            Negative("extra parameter", () => Sig(parameters: new[] { ClassType(parameter), ClassType(parameter) }));
            Negative("Boolean class instead of primitive", () => Sig(returnType: ClassType(result)));
            Negative("return class missing", () => Sig(returnType: ClassType(null)));
            foreach (var primitive in Enum.GetValues<PrimitiveTypeCode>())
                if (primitive != PrimitiveTypeCode.Boolean)
                    Negative("primitive return " + primitive, () => Sig(returnType: Primitive(primitive)));
            foreach (string kind in new[] { "ByRef", "Pointer", "SZArray", "MDArray" })
                Negative("wrapped return " + kind, () => Sig(returnType: Wrapped(kind, Primitive(PrimitiveTypeCode.Boolean))));
            foreach (string kind in new[] { "External", "ExternalGeneric", "GenericVar", "Template" })
                Negative("return kind " + kind, () =>
                {
                    var type = Primitive(PrimitiveTypeCode.Boolean);
                    Set(type, "Kind", Enum.Parse(Model("TypeKind"), kind));
                    Sig(returnType: type);
                });
            if (publicGetter)
            {
                Negative("unsealed owner", () => Set(owner, "IsSealed", false));
                Negative("one parameter", () => Sig(parameters: new[] { ClassType(parameter) }));
            }
            else
            {
                Negative("no parameter", () => Sig(parameters: Array.Empty<object>()));
                Negative("parameter Type instead of RuntimeType", () => Set(parameter, "_fullName", "System.Type"));
                Negative("parameter foreign module", () => Set(parameter, "Module", Module("Lookalike")));
                Negative("parameter distinct CoreLib module", () => Set(parameter, "Module", Module()));
                Negative("parameter class missing", () => Sig(parameters: new[] { ClassType(null) }));
                foreach (var primitive in Enum.GetValues<PrimitiveTypeCode>())
                    Negative("primitive parameter " + primitive, () => Sig(parameters: new[] { Primitive(primitive) }));
                foreach (string kind in new[] { "ByRef", "Pointer", "SZArray", "MDArray" })
                    Negative("wrapped parameter " + kind, () => Sig(parameters: new[] { Wrapped(kind, ClassType(parameter)) }));
                foreach (string name in new[] { "GetGenericVariableIndex", "ContainsGenericVariables",
                    "IsGenericTypeVariable", "IsGenericMethodVariable" })
                    Negative("unrelated private ABI " + name, () => Set(method, "Name", name));
            }
            foreach (var hint in new[] { MethodImplAttributes.NoInlining, MethodImplAttributes.AggressiveInlining,
                MethodImplAttributes.NoOptimization })
            {
                Reset();
                Set(method, "ImplAttributes", actual.GetMethodImplementationFlags() | hint);
                Matches(true, "implementation hint " + hint);
            }
        }

        var getterBody = getter.GetMethodBody()!.GetILAsByteArray()!;
        Check(getterBody is [0x02, 0x28, _, _, _, _, 0x2a]
            && getter.Module.ResolveMethod(BitConverter.ToInt32(getterBody, 2)) == helper,
            "retained getter calls the exact helper");
        Console.WriteLine("runtime-type-is-by-ref-guards:True");
        Console.WriteLine("runtime-type-is-by-ref-guard-checks:" + checks);
        return true;
    }
}
