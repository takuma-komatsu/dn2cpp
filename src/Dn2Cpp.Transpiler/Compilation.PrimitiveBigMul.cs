using System.Reflection.Metadata;

namespace Dn2Cpp;

internal sealed partial class Compilation
{
    private void ValidatePrimitiveBigMulOwner(Module module, MemberReferenceHandle handle, GenericContext context)
    {
        var reader = module.Reader;
        var reference = reader.GetMemberReference(handle);
        if (!reader.StringComparer.Equals(reference.Name, "BigMul")
            || reference.Parent.Kind != HandleKind.TypeSpecification)
            return;
        var owner = reader.GetTypeSpecification((TypeSpecificationHandle)reference.Parent)
            .DecodeSignature(SigProvider, context);
        while (owner.Element is { } element) owner = element;
        string? name = owner.Class?.FullName;
        if (owner.Kind == TypeKind.Primitive)
            name = owner.Primitive switch
            {
                PrimitiveTypeCode.Int32 => "System.Int32",
                PrimitiveTypeCode.UInt32 => "System.UInt32",
                PrimitiveTypeCode.Int64 => "System.Int64",
                PrimitiveTypeCode.UInt64 => "System.UInt64",
                PrimitiveTypeCode.IntPtr => "System.IntPtr",
                PrimitiveTypeCode.UIntPtr => "System.UIntPtr",
                _ => null,
            };
        if (CoreIntrinsicTypeMaps.IsPrimitiveBigMulOwner(name))
            throw new NotSupportedException("Primitive BigMul requires its unmodified named CoreLib owner");
    }
}
