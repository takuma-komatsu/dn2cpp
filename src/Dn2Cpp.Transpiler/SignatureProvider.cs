using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Metadata;

namespace Dn2Cpp;

/// <summary>
/// Decodes metadata signatures into <see cref="TypeDesc"/>. The genericContext
/// argument (a <see cref="GenericContext"/> or null) makes generic parameters
/// resolve to concrete arguments inline, so closed generic instantiations are
/// materialized eagerly during decoding.
/// </summary>
internal sealed class SignatureProvider : ISignatureTypeProvider<TypeDesc, object?>
{
    private readonly Compilation _compilation;

    public SignatureProvider(Compilation compilation) => _compilation = compilation;

    public TypeDesc GetPrimitiveType(PrimitiveTypeCode typeCode) => TypeDesc.MakePrimitive(typeCode);

    public TypeDesc GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind) =>
        _compilation.GetTypeDescForDefinition(_compilation.ModuleOf(reader), handle);

    public TypeDesc GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind)
    {
        // Prefer a real cross-assembly definition; otherwise treat as external.
        if (_compilation.ResolveTypeRef(_compilation.ModuleOf(reader), handle) is { } resolved)
            return resolved;
        var tr = reader.GetTypeReference(handle);
        string ns = reader.GetString(tr.Namespace);
        string name = reader.GetString(tr.Name);
        string full = string.IsNullOrEmpty(ns) ? name : ns + "." + name;
        return TypeDesc.MakeExternal(full);
    }

    public TypeDesc GetSZArrayType(TypeDesc elementType) => TypeDesc.MakeSZArray(elementType);

    public TypeDesc GetArrayType(TypeDesc elementType, ArrayShape shape) =>
        TypeDesc.MakeMDArray(elementType, shape.Rank);

    public TypeDesc GetByReferenceType(TypeDesc elementType) => TypeDesc.MakeByRef(elementType);

    // Pointers and function pointers pervade real CoreLib signatures (Span,
    // ref-struct internals, InternalCall thunks). We decode them to an opaque
    // Pointer placeholder rather than throwing, so loading a real BCL via -r
    // does not abort during shell building; tree-shaking keeps the members that
    // actually use them out of the output unless they are reached.
    public TypeDesc GetPointerType(TypeDesc elementType) => TypeDesc.MakePointer(elementType);

    // A function pointer keeps the same opaque pointer shape (the decoded signature is
    // dropped) but stays distinguishable from void*: [MarshalAs(FunctionPtr)] is valid on
    // it alone, so folding the two refuses a descriptor real .NET accepts.
    public TypeDesc GetFunctionPointerType(MethodSignature<TypeDesc> signature) =>
        TypeDesc.MakeFunctionPointer();

    public TypeDesc GetGenericInstantiation(TypeDesc genericType, ImmutableArray<TypeDesc> typeArguments)
    {
        if (genericType.Kind == TypeKind.Template)
            return TypeDesc.MakeClass(_compilation.Instantiate(genericType.TemplateModule!, genericType.TemplateHandle, typeArguments.ToArray()));
        // A closed instantiation of a base-image generic type: the hot-update
        // patch converter resolves it to its mangled registry name against the
        // base ABI manifest (or rejects it as absent from the base image). The
        // normal pipeline has no resolver and cannot emit a type it never loaded.
        if (genericType.Kind == TypeKind.External && _compilation.ExternalGenericResolver is { } resolve)
        {
            var args = typeArguments.ToArray();
            return resolve(genericType.ExternalName!, args)
                ?? throw new NotSupportedException(
                    $"emit-patch: the generic instantiation {genericType.ExternalName}<{string.Join(", ", args.Select(a => a.ToString()))}> is not present in the base image (pre-reference it in hotupdate-refs.txt, the AOT-instantiation boundary)");
        }
        throw new NotSupportedException($"Generic instantiation of {genericType} is not supported yet (external generic types)");
    }

    public TypeDesc GetGenericMethodParameter(object? genericContext, int index)
    {
        if (genericContext is GenericContext { } ctx && index < ctx.MethodArgs.Length)
            return ctx.MethodArgs[index];
        return TypeDesc.MakeGenericVar(index, isMethod: true);
    }

    public TypeDesc GetGenericTypeParameter(object? genericContext, int index)
    {
        if (genericContext is GenericContext { } ctx && index < ctx.TypeArgs.Length)
            return ctx.TypeArgs[index];
        return TypeDesc.MakeGenericVar(index, isMethod: false);
    }

    public TypeDesc GetModifiedType(TypeDesc modifier, TypeDesc unmodifiedType, bool isRequired) => unmodifiedType;

    public TypeDesc GetPinnedType(TypeDesc elementType) => elementType;

    public TypeDesc GetTypeFromSpecification(MetadataReader reader, object? genericContext, TypeSpecificationHandle handle, byte rawTypeKind) =>
        reader.GetTypeSpecification(handle).DecodeSignature(this, genericContext);
}

/// <summary>
/// Decodes custom-attribute value blobs (ECMA-335 II.23.3) into <see cref="TypeDesc"/>
/// arguments via <c>CustomAttribute.DecodeValue</c>. Reuses the
/// <see cref="SignatureProvider"/> for the signature-shaped pieces and adds the four
/// attribute-specific hooks: the System.Type sentinel, serialized type-name resolution
/// (for <c>typeof(T)</c>-valued and enum-typed args), and the enum underlying type.
/// </summary>
internal sealed class CustomAttributeTypeProvider : ICustomAttributeTypeProvider<TypeDesc>
{
    private readonly Compilation _compilation;
    private readonly SignatureProvider _sig;

    public CustomAttributeTypeProvider(Compilation compilation)
    {
        _compilation = compilation;
        _sig = compilation.SigProvider;
    }

    public TypeDesc GetPrimitiveType(PrimitiveTypeCode typeCode) => _sig.GetPrimitiveType(typeCode);

    public TypeDesc GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind) =>
        _sig.GetTypeFromDefinition(reader, handle, rawTypeKind);

    public TypeDesc GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind) =>
        _sig.GetTypeFromReference(reader, handle, rawTypeKind);

    public TypeDesc GetSZArrayType(TypeDesc elementType) => _sig.GetSZArrayType(elementType);

    public TypeDesc GetSystemType() => TypeDesc.MakeExternal("System.Type");

    public bool IsSystemType(TypeDesc type) =>
        (type.Kind == TypeKind.External && type.ExternalName == "System.Type")
        || (type.Kind == TypeKind.Class && type.Class!.FullName == "System.Type");

    public TypeDesc GetTypeFromSerializedName(string name)
    {
        // The blob carries an assembly-qualified name ("Ns.Type, Assembly, …"); only the
        // type-name prefix matters. Resolve it to an emitted ClassInfo when possible, else
        // an External placeholder (CppEmitter then treats the attribute as unsupported).
        int comma = name.IndexOf(',');
        string full = (comma >= 0 ? name[..comma] : name).Trim();
        return _compilation.FindClassByFullName(full) is { } cls
            ? TypeDesc.MakeClass(cls)
            : TypeDesc.MakeExternal(full);
    }

    public PrimitiveTypeCode GetUnderlyingEnumType(TypeDesc type) =>
        type.Kind == TypeKind.Class && type.Class!.IsEnum
            ? type.Class!.EnumUnderlying
            : PrimitiveTypeCode.Int32;
}
