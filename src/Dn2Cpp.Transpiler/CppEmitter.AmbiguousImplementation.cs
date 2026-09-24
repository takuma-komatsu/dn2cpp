using System.Collections.Immutable;
using System.Reflection.Metadata;

namespace Dn2Cpp;

internal sealed partial class CppEmitter
{
    /// <summary>The message .NET's AmbiguousImplementationException carries when
    /// <paramref name="receiver"/> is called through <paramref name="slot"/> of
    /// <paramref name="itf"/> and sibling interface overrides leave no most
    /// specific body. The CLR builds it from native resources: the method in its
    /// shared-code signature spelling, the interface and receiver type names,
    /// and the receiver's assembly display name.
    /// <para>For a generic method on a generic interface the CLR spells the
    /// declaring type with the method's own type parameters; that spelling is
    /// not reproduced.</para></summary>
    internal static string AmbiguousImplementationMessage(ClassInfo receiver, ClassInfo itf, MethodInfo slot)
    {
        string method = ClrTypeName(itf, canonical: true) + "." + slot.Name
            + "(" + string.Join(", ", ClrSignatureParameters(itf, slot)) + ")";
        return "Could not call method '" + method + "' on interface '" + ClrTypeName(itf, canonical: false)
            + "' with type '" + ClrTypeName(receiver, canonical: false) + "' from assembly '"
            + AssemblyDisplayName(receiver.Module)
            + "' because there are multiple incompatible interface methods overriding this method.";
    }

    /// <summary><c>Name, Version=v, Culture=c, PublicKeyToken=t</c>, as
    /// Assembly.FullName displays it.</summary>
    private static string AssemblyDisplayName(Module module)
    {
        try
        {
            var def = module.Reader.GetAssemblyDefinition();
            var v = def.Version;
            string culture = module.Reader.GetString(def.Culture);
            return module.AssemblyName + ", Version=" + v.Major + "." + v.Minor + "." + v.Build + "." + v.Revision
                + ", Culture=" + (culture.Length == 0 ? "neutral" : culture)
                + ", PublicKeyToken=" + (PublicKeyTokenOf(module.Reader, def) ?? "null");
        }
        catch (Exception e) when (!Compilation.IsMustEscape(e))
        {
            return module.AssemblyName;
        }
    }

    /// <summary>The CLR type-name spelling of <paramref name="cls"/>:
    /// <c>Ns.Outer+Inner`1[System.Int32]</c>. <paramref name="canonical"/> spells a
    /// reference-type argument as the shared-code placeholder
    /// <c>System.__Canon</c>.</summary>
    private static string ClrTypeName(ClassInfo cls, bool canonical)
    {
        if (cls.Handle.IsNil)
            return cls.FullName;
        string name;
        try
        {
            name = RawSignatureProvider.TypeDefinitionName(cls.Module.Reader, cls.Handle);
        }
        catch (Exception e) when (!Compilation.IsMustEscape(e))
        {
            return cls.FullName;
        }
        var args = cls.Context.TypeArgs;
        if (args.Length == 0)
            return name;
        var parts = new string[args.Length];
        for (int i = 0; i < args.Length; i++)
            parts[i] = ClrTypeName(args[i], canonical);
        return name + "[" + string.Join(",", parts) + "]";
    }

    private static string ClrTypeName(TypeDesc t, bool canonical)
    {
        if (canonical && IsSharedReferenceArgument(t))
            return "System.__Canon";
        return t.Kind switch
        {
            TypeKind.Primitive => PrimitiveClrName(t.Primitive),
            TypeKind.Class => ClrTypeName(t.Class!, canonical),
            TypeKind.External => t.ExternalName!,
            TypeKind.SZArray => ClrTypeName(t.Element!, false) + "[]",
            TypeKind.MDArray => ClrTypeName(t.Element!, false) + "[" + new string(',', t.Rank - 1) + "]",
            TypeKind.ByRef => ClrTypeName(t.Element!, false) + "&",
            TypeKind.Pointer when t.Element is not null => ClrTypeName(t.Element, false) + "*",
            _ => t.ToString(),
        };
    }

    // Shared code runs every reference-type instantiation through one body, so
    // the CLR names such an argument by the placeholder.
    private static bool IsSharedReferenceArgument(TypeDesc t) => t.Kind switch
    {
        TypeKind.Primitive => t.Primitive is PrimitiveTypeCode.String or PrimitiveTypeCode.Object,
        TypeKind.Class => !t.Class!.IsValueType,
        TypeKind.External or TypeKind.SZArray or TypeKind.MDArray => true,
        _ => false,
    };

    private static string Qualify(string ns, string name) => ns.Length == 0 ? name : ns + "." + name;

    /// <summary><paramref name="slot"/>'s parameter types as the CLR's signature
    /// formatter spells them, a type parameter of <paramref name="itf"/> taking
    /// its shared-code argument.</summary>
    private static string[] ClrSignatureParameters(ClassInfo itf, MethodInfo slot)
    {
        var args = itf.Context.TypeArgs;
        var context = new string[args.Length];
        for (int i = 0; i < args.Length; i++)
            context[i] = SigFormatName(args[i]);
        try
        {
            var sig = slot.Module.Reader.GetMethodDefinition(slot.Handle)
                .DecodeSignature(SigFormatProvider.Instance, context);
            return sig.ParameterTypes.ToArray();
        }
        catch (Exception e) when (!Compilation.IsMustEscape(e))
        {
            return [];
        }
    }

    /// <summary>A closed type argument in the signature formatter's spelling,
    /// canonicalized as shared code sees it.</summary>
    private static string SigFormatName(TypeDesc t)
    {
        if (IsSharedReferenceArgument(t))
            return "System.__Canon";
        if (t.Kind == TypeKind.Primitive)
            return SigFormatProvider.Instance.GetPrimitiveType(t.Primitive);
        if (t.Kind != TypeKind.Class || t.Class!.Handle.IsNil)
            return ClrTypeName(t, canonical: true);
        var cls = t.Class;
        string name;
        try
        {
            name = SigFormatProvider.Instance.GetTypeFromDefinition(cls.Module.Reader, cls.Handle, 0);
        }
        catch (Exception e) when (!Compilation.IsMustEscape(e))
        {
            return cls.FullName;
        }
        var args = cls.Context.TypeArgs;
        if (args.Length == 0)
            return name;
        var parts = new string[args.Length];
        for (int i = 0; i < args.Length; i++)
            parts[i] = SigFormatName(args[i]);
        return name + "<" + string.Join(",", parts) + ">";
    }

    /// <summary>Decodes a signature into the CLR signature formatter's type
    /// spellings: primitives by bare name, other types namespace-qualified, a
    /// nested type by its own name, generic arguments in angle brackets, a
    /// method type parameter as <c>!!n</c>. Reads metadata only, so decoding
    /// never creates a class.</summary>
    private sealed class SigFormatProvider : ISignatureTypeProvider<string, string[]>
    {
        internal static readonly SigFormatProvider Instance = new();

        public string GetPrimitiveType(PrimitiveTypeCode typeCode) => typeCode switch
        {
            PrimitiveTypeCode.String => "System.String",
            PrimitiveTypeCode.Object => "System.Object",
            PrimitiveTypeCode.Void => "Void",
            PrimitiveTypeCode.TypedReference => "TypedReference",
            _ => PrimitiveClrName(typeCode).Substring("System.".Length),
        };

        public string GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind)
        {
            var td = reader.GetTypeDefinition(handle);
            string name = reader.GetString(td.Name);
            return td.GetDeclaringType().IsNil ? Qualify(reader.GetString(td.Namespace), name) : name;
        }

        public string GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind)
        {
            var tr = reader.GetTypeReference(handle);
            string name = reader.GetString(tr.Name);
            return tr.ResolutionScope.Kind == HandleKind.TypeReference
                ? name
                : Qualify(reader.GetString(tr.Namespace), name);
        }

        public string GetTypeFromSpecification(MetadataReader reader, string[] genericContext,
            TypeSpecificationHandle handle, byte rawTypeKind) =>
            reader.GetTypeSpecification(handle).DecodeSignature(this, genericContext);

        public string GetGenericInstantiation(string genericType, ImmutableArray<string> typeArguments)
        {
            var parts = new string[typeArguments.Length];
            for (int i = 0; i < parts.Length; i++)
                parts[i] = typeArguments[i];
            return genericType + "<" + string.Join(",", parts) + ">";
        }

        public string GetSZArrayType(string elementType) => elementType + "[]";
        public string GetArrayType(string elementType, ArrayShape shape) =>
            elementType + "[" + new string(',', shape.Rank - 1) + "]";
        public string GetByReferenceType(string elementType) => elementType + " ByRef";
        public string GetPointerType(string elementType) => elementType + "*";
        public string GetFunctionPointerType(MethodSignature<string> signature) => "FNPTR";
        public string GetGenericTypeParameter(string[] genericContext, int index) =>
            index < genericContext.Length ? genericContext[index] : "!" + index;
        public string GetGenericMethodParameter(string[] genericContext, int index) => "!!" + index;
        public string GetModifiedType(string modifier, string unmodifiedType, bool isRequired) => unmodifiedType;
        public string GetPinnedType(string elementType) => elementType;
    }
}
