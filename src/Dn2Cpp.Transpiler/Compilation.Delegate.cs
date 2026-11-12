using System.Reflection.Metadata;

namespace Dn2Cpp;

internal sealed partial class Compilation
{
    // Generated delegates have no ordinary vtable. The base value-method bodies
    // are still callable nonvirtually, but virtual entry selects Multicast's
    // override, including when entered through a retained reflection row.
    internal MethodInfo DelegateCallableVirtualTarget(MethodInfo method)
    {
        if (method.DeclaringClass.FullName != "System.Delegate" || method.Name is not ("Equals" or "GetHashCode"))
            return method;
        var owner = FindClassByFullName("System.MulticastDelegate");
        var target = owner?.EnsureMembers().Methods.FirstOrDefault(m => m.Name == method.Name
            && CoreIntrinsics.BrDelegateCallable.Matches(m));
        if (target is null || target.Module != method.Module)
            throw new NotSupportedException("Delegate virtual value entry requires its exact CoreLib MulticastDelegate override");
        return target;
    }

    // Validate before intrinsic-owner fallback and before the MemberRef cache.
    internal MethodInfo? TryResolveDelegateCallableReference(Module module, MemberReferenceHandle handle, GenericContext context)
    {
        var reader = module.Reader;
        var reference = reader.GetMemberReference(handle);
        string name = reader.GetString(reference.Name);
        if (!DelegateCallableBodyContract.IsName("System.MulticastDelegate", name)) return null;
        string? owner = MemberRefParentTypeName(module, handle);
        if (owner is null && reference.Parent.Kind == HandleKind.TypeSpecification)
            owner = reader.GetTypeSpecification((TypeSpecificationHandle)reference.Parent).DecodeSignature(SigProvider, context).Class?.FullName;
        if (owner is null || !DelegateCallableBodyContract.IsName(owner, name)) return null;
        if (reference.Parent.Kind is not (HandleKind.TypeReference or HandleKind.TypeDefinition))
            throw new NotSupportedException("Delegate callable member requires an unmodified CoreLib owner");
        var method = ResolveMemberRefMethod(module, handle, context);
        var signature = reference.DecodeMethodSignature(SigProvider, context);
        if (!CoreIntrinsics.IsDelegateCallableReference(method, signature, reader.GetBlobReader(reference.Signature)))
            throw new NotSupportedException("Delegate callable member requires its exact CoreLib reference and declaration");
        return method;
    }
}
