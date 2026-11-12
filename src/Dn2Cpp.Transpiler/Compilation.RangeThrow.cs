using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace Dn2Cpp;

internal sealed partial class Compilation
{
    internal MethodInfo? TryResolveRangeThrowManagedSpec(Module module, MethodSpecificationHandle handle, GenericContext context)
    {
        var specification = module.Reader.GetMethodSpecification(handle);
        if (!RangeThrowManagedBodyContract.IsName(MethodSpecParentTypeName(module, specification),
                MethodSpecMethodName(module, specification)))
            return null;
        return ResolveMethodSpec(module, handle, context);
    }

    private void ValidateRangeThrowManagedSpec(Module module, MethodSpecification specification,
        MethodInfo target, GenericContext context)
    {
        if (!RangeThrowManagedBodyContract.IsName(target.DeclaringClass.FullName, target.Name))
            return;
        if (!CoreIntrinsics.IsRangeThrowManagedBody(target))
            throw RangeThrowManagedUnsupported();
        var reader = module.Reader;
        var blob = reader.GetBlobReader(specification.Signature);
        if (blob.RemainingBytes < 3 || blob.ReadByte() != 0x0A || blob.ReadByte() != 1)
            throw RangeThrowManagedUnsupported();
        var decoder = new SignatureDecoder<TypeDesc, object?>(SigProvider, reader, context);
        var argument = decoder.DecodeType(ref blob);
        if (blob.RemainingBytes != 0 || !CoreIntrinsics.SameRangeThrowArgument(argument, target.Context.MethodArgs[0]))
            throw RangeThrowManagedUnsupported();
        if (specification.Method.Kind == HandleKind.MemberReference)
        {
            var reference = reader.GetMemberReference((MemberReferenceHandle)specification.Method);
            if (reference.Parent.Kind is not (HandleKind.TypeDefinition or HandleKind.TypeReference)
                || TypeDefinitionOf(module, reference.Parent) is not { } identity
                || identity.Module != target.Module || identity.Handle != target.DeclaringClass.Handle
                || !RangeThrowManagedBodyContract.IsSignature(reader.GetBlobReader(reference.Signature))
                || !CoreIntrinsics.IsRangeThrowManagedSignature(reference.DecodeMethodSignature(SigProvider,
                    new GenericContext([], target.Context.MethodArgs)), target.Context.MethodArgs[0]))
                throw RangeThrowManagedUnsupported();
        }
        else if (specification.Method.Kind != HandleKind.MethodDefinition || module != target.Module
            || specification.Method != target.Handle)
            throw RangeThrowManagedUnsupported();
    }

    private static NotSupportedException RangeThrowManagedUnsupported() =>
        new("ThrowArgumentOutOfRange_Range requires its exact CoreLib declaration and MethodSpec signature");
}
