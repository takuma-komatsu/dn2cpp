using System.Reflection.Metadata;

namespace Dn2Cpp;

internal sealed partial class Compilation
{
    internal void ValidateNumericMemberRefOwner(Module module, MemberReferenceHandle handle, GenericContext context)
    {
        ValidatePrimitiveBigMulOwner(module, handle, context);
        ValidateNativeIntegerOwner(module, module.Reader.GetMemberReference(handle), context);
    }

    private void ValidateNativeIntegerOwner(Module module, MemberReference reference, GenericContext context)
    {
        if (reference.Parent.Kind != HandleKind.TypeSpecification) return;
        var reader = module.Reader;
        if (!reader.StringComparer.Equals(reference.Name, "Parse")
            && !reader.StringComparer.Equals(reference.Name, "TryParse")
            && !reader.StringComparer.Equals(reference.Name, "System.Numerics.IBinaryInteger<nint>.TryReadBigEndian")
            && !reader.StringComparer.Equals(reference.Name, "System.Numerics.IBinaryInteger<nint>.TryReadLittleEndian")
            && !reader.StringComparer.Equals(reference.Name, "System.Numerics.IBinaryInteger<nuint>.TryReadBigEndian")
            && !reader.StringComparer.Equals(reference.Name, "System.Numerics.IBinaryInteger<nuint>.TryReadLittleEndian")) return;
        var type = reader.GetTypeSpecification((TypeSpecificationHandle)reference.Parent)
            .DecodeSignature(SigProvider, context);
        while (type.Element is { } element) type = element;
        bool native = type.Kind == TypeKind.Primitive
            ? type.Primitive is PrimitiveTypeCode.IntPtr or PrimitiveTypeCode.UIntPtr
            : type.Class is { } owner && PrimitiveParseContract.IsNativeOwner(owner.FullName);
        if (native)
            throw new NotSupportedException("Native integer entry point requires its unmodified named CoreLib owner");
    }
}
