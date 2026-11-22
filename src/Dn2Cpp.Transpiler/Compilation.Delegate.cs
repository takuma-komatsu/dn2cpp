using System.Reflection.Metadata;

namespace Dn2Cpp;

internal sealed partial class Compilation
{
    // Validate before intrinsic-owner fallback and before the MemberRef cache.
    internal MethodInfo? TryResolveDelegateCallableReference(Module module, MemberReferenceHandle handle, GenericContext context)
    {
        var reader = module.Reader;
        var reference = reader.GetMemberReference(handle);
        string name = reader.GetString(reference.Name);
        if (!DelegateCallableBodyContract.IsName("System.Delegate", name)) return null;
        string? owner = MemberRefParentTypeName(module, handle);
        if (owner is null && reference.Parent.Kind == HandleKind.TypeSpecification)
            owner = reader.GetTypeSpecification((TypeSpecificationHandle)reference.Parent).DecodeSignature(SigProvider, context).Class?.FullName;
        if (owner is null || !DelegateCallableBodyContract.IsName(owner, name)) return null;
        if (reference.Parent.Kind is not (HandleKind.TypeReference or HandleKind.TypeDefinition))
            throw new NotSupportedException("Delegate callable member requires an unmodified CoreLib owner");
        // A load set without CoreLib has no declaration to validate; the public
        // members keep their call-site projections there.
        if (reference.Parent.Kind == HandleKind.TypeReference
            && ResolveTypeRef(module, (TypeReferenceHandle)reference.Parent)?.Class is null) return null;
        var method = ResolveMemberRefMethod(module, handle, context);
        var signature = reference.DecodeMethodSignature(SigProvider, context);
        if (!CoreIntrinsics.IsDelegateCallableReference(method, signature, reader.GetBlobReader(reference.Signature)))
            throw new NotSupportedException("Delegate callable member requires its exact CoreLib reference and declaration");
        return method;
    }
}
