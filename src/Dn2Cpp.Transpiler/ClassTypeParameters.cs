using System.Collections.Immutable;
using System.Reflection.Metadata;

namespace Dn2Cpp;

/// <summary>Which class type parameters a metadata token's instantiation mentions, read
/// off its blobs alone: one bit per parameter index, the last bit standing for every
/// index past it. A member reference answers its declaring type's, a method
/// specification its declaring type's and its method arguments'.</summary>
internal static class ClassTypeParameters
{
    internal static ulong Of(MetadataReader reader, EntityHandle token)
    {
        switch (token.Kind)
        {
            case HandleKind.TypeSpecification:
                return reader.GetTypeSpecification((TypeSpecificationHandle)token).DecodeSignature(Mask.Instance, null);
            case HandleKind.MemberReference:
                var parent = reader.GetMemberReference((MemberReferenceHandle)token).Parent;
                return parent.Kind == HandleKind.TypeSpecification ? Of(reader, parent) : 0;
            case HandleKind.MethodSpecification:
                var spec = reader.GetMethodSpecification((MethodSpecificationHandle)token);
                ulong mask = Of(reader, spec.Method);
                foreach (ulong arg in spec.DecodeSignature(Mask.Instance, null))
                    mask |= arg;
                return mask;
            default:
                return 0;
        }
    }

    /// <summary>Whether a type token is a class type parameter itself.</summary>
    internal static bool IsBare(MetadataReader reader, EntityHandle type) => BareIndex(reader, type) is not null;

    /// <summary>The index of the class type parameter a type token is itself, or null.</summary>
    internal static int? BareIndex(MetadataReader reader, EntityHandle type)
    {
        if (type.Kind != HandleKind.TypeSpecification)
            return null;
        var blob = reader.GetBlobReader(reader.GetTypeSpecification((TypeSpecificationHandle)type).Signature);
        return blob.ReadSignatureTypeCode() == SignatureTypeCode.GenericTypeParameter
            ? blob.ReadCompressedInteger()
            : null;
    }

    /// <summary>Whether a type token is an instantiation of a generic value type, which
    /// its blob states whatever the arguments.</summary>
    internal static bool IsValueTypeInstance(MetadataReader reader, EntityHandle type)
    {
        if (type.Kind != HandleKind.TypeSpecification)
            return false;
        var blob = reader.GetBlobReader(reader.GetTypeSpecification((TypeSpecificationHandle)type).Signature);
        return blob.ReadSignatureTypeCode() == SignatureTypeCode.GenericTypeInstance
            && blob.ReadByte() == (byte)SignatureTypeKind.ValueType;
    }

    private sealed class Mask : ISignatureTypeProvider<ulong, object?>
    {
        internal static readonly Mask Instance = new();

        public ulong GetArrayType(ulong elementType, ArrayShape shape) => elementType;
        public ulong GetByReferenceType(ulong elementType) => elementType;
        public ulong GetFunctionPointerType(MethodSignature<ulong> signature)
        {
            ulong mask = signature.ReturnType;
            foreach (ulong p in signature.ParameterTypes)
                mask |= p;
            return mask;
        }
        public ulong GetGenericInstantiation(ulong genericType, ImmutableArray<ulong> typeArguments)
        {
            ulong mask = genericType;
            foreach (ulong a in typeArguments)
                mask |= a;
            return mask;
        }
        public ulong GetGenericMethodParameter(object? genericContext, int index) => 0;
        public ulong GetGenericTypeParameter(object? genericContext, int index) => 1UL << Math.Min(index, 63);
        public ulong GetModifiedType(ulong modifier, ulong unmodifiedType, bool isRequired) => unmodifiedType;
        public ulong GetPinnedType(ulong elementType) => elementType;
        public ulong GetPointerType(ulong elementType) => elementType;
        public ulong GetPrimitiveType(PrimitiveTypeCode typeCode) => 0;
        public ulong GetSZArrayType(ulong elementType) => elementType;
        public ulong GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind) => 0;
        public ulong GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind) => 0;
        public ulong GetTypeFromSpecification(MetadataReader reader, object? genericContext,
            TypeSpecificationHandle handle, byte rawTypeKind) =>
            reader.GetTypeSpecification(handle).DecodeSignature(this, genericContext);
    }
}
