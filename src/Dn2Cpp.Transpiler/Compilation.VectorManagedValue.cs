using System.Reflection.Metadata;

namespace Dn2Cpp;

internal sealed partial class Compilation
{
    internal MethodInfo? TryResolveVectorManagedThrowSpec(Module module, MethodSpecificationHandle handle, GenericContext context)
    {
        var specification = module.Reader.GetMethodSpecification(handle);
        if (MethodSpecMethodName(module, specification) is not ("ThrowForUnsupportedNumericsVectorBaseType"
                or "ThrowForUnsupportedIntrinsicsVector64BaseType" or "ThrowForUnsupportedIntrinsicsVector128BaseType")
            || MethodSpecParentTypeName(module, specification) != "System.ThrowHelper") return null;
        var raw = module.Reader.GetBlobReader(specification.Signature);
        if (raw.RemainingBytes < 3 || raw.ReadByte() != 0x0a || raw.ReadByte() != 1
            || raw.ReadByte() is 0x01 or 0x0f or 0x10 or 0x16 or 0x1b or 0x1f or 0x20 or 0x45)
            throw VectorManagedThrowUnsupported();
        var arguments = specification.DecodeSignature(SigProvider, context);
        if (arguments is not [var argument]) throw VectorManagedThrowUnsupported();
        if (argument.Class is { } element) EnsureShape(element);
        if (!CoreIntrinsics.IsVectorManagedValueElement(argument)) return null;
        return ResolveMethodSpec(module, handle, context);
    }

    private void ValidateVectorManagedThrowSpec(Module module, MethodSpecification specification,
        MethodInfo target, GenericContext context)
    {
        if (target.Name is not ("ThrowForUnsupportedNumericsVectorBaseType"
                or "ThrowForUnsupportedIntrinsicsVector64BaseType" or "ThrowForUnsupportedIntrinsicsVector128BaseType")
            || target.DeclaringClass.FullName != "System.ThrowHelper"
            || target.Context.MethodArgs is not [var argument]) return;
        if (argument.Class is { } element) EnsureShape(element);
        if (!CoreIntrinsics.IsVectorManagedValueElement(argument)) return;
        if (!CoreIntrinsics.IsVectorManagedThrowHelper(target)) throw VectorManagedThrowUnsupported();
        var reader = module.Reader;
        var blob = reader.GetBlobReader(specification.Signature);
        if (blob.RemainingBytes < 3 || blob.ReadByte() != 0x0a || blob.ReadByte() != 1
            || !ReadVectorValueElement(module, ref blob, context, argument) || blob.RemainingBytes != 0)
            throw VectorManagedThrowUnsupported();
        if (specification.Method.Kind == HandleKind.MemberReference)
        {
            var reference = reader.GetMemberReference((MemberReferenceHandle)specification.Method);
            if (reference.Parent.Kind is not (HandleKind.TypeDefinition or HandleKind.TypeReference)
                || TypeDefinitionOf(module, reference.Parent) is not { } identity
                || identity.Module != target.Module || identity.Handle != target.DeclaringClass.Handle
                || !CoreIntrinsics.IsVectorManagedThrowSignature(reader.GetBlobReader(reference.Signature)))
                throw VectorManagedThrowUnsupported();
        }
        else if (specification.Method.Kind != HandleKind.MethodDefinition || module != target.Module
            || specification.Method != target.Handle)
            throw VectorManagedThrowUnsupported();
    }

    private static NotSupportedException VectorManagedThrowUnsupported() =>
        new("Vector unsupported-type guard requires its exact closed CoreLib declaration and unmodified MethodSpec");
}
