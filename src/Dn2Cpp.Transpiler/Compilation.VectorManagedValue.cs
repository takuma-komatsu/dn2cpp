using System.Reflection.Metadata;

namespace Dn2Cpp;

internal sealed partial class Compilation
{
    internal MethodInfo? TryResolveVectorManagedThrowSpec(Module module, MethodSpecificationHandle handle, GenericContext context)
    {
        var specification = module.Reader.GetMethodSpecification(handle);
        if (MethodSpecMethodName(module, specification) != "ThrowForUnsupportedNumericsVectorBaseType"
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
        if (target.Name != "ThrowForUnsupportedNumericsVectorBaseType"
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

    // Canonical layouts may share enum scalars, but not a managed Vector<enum>
    // field with its intrinsic-mapped Vector<CnInt32> counterpart.
    private bool SameVectorSignatureStorage(TypeDesc user, TypeDesc owner)
    {
        while (user.Kind == TypeKind.ByRef && owner.Kind == TypeKind.ByRef)
        {
            user = user.Element!;
            owner = owner.Element!;
        }
        if (user is not { Kind: TypeKind.Class, Class: { } actual }
            || owner is not { Kind: TypeKind.Class, Class: { } canonical }) return true;
        EnsureShape(actual);
        EnsureShape(canonical);
        return !actual.IsValueType || !canonical.IsValueType || SameVectorFieldStorage(actual, canonical);
    }

    internal bool SameVectorFieldStorage(ClassInfo user, ClassInfo owner) =>
        SameVectorFieldStorage(user, owner, new HashSet<(ClassInfo, ClassInfo)>());

    private bool SameVectorFieldStorage(ClassInfo user, ClassInfo owner,
        HashSet<(ClassInfo, ClassInfo)> visited)
    {
        if (user == owner || !visited.Add((user, owner))) return true;
        if (user.Module != owner.Module || user.Handle != owner.Handle) return true;
        if (CoreIntrinsics.IsVectorExplicitOwner(user) && CoreIntrinsics.IsVectorExplicitOwner(owner))
            return (user.IntrinsicCppName is null) == (owner.IntrinsicCppName is null);
        if (user.IntrinsicCppName is not null || owner.IntrinsicCppName is not null) return true;
        EnsureShape(user);
        EnsureShape(owner);
        if (user.BaseClass is { } userBase && owner.BaseClass is { } ownerBase
            && !SameVectorFieldStorage(userBase, ownerBase, visited)) return false;
        var userFields = user.Fields.ToArray();
        var ownerFields = owner.Fields.ToArray();
        foreach (var field in userFields)
        {
            if (field.IsStatic || field.IsLiteral) continue;
            foreach (var counterpart in ownerFields)
            {
                if (counterpart.Handle != field.Handle || counterpart.DeclaringClass.Module != field.DeclaringClass.Module
                    || counterpart.IsStatic || counterpart.IsLiteral) continue;
                var actual = field.Type;
                var canonical = counterpart.Type;
                if (actual is not { Kind: TypeKind.Class, Class: { } actualClass }
                    || canonical is not { Kind: TypeKind.Class, Class: { } canonicalClass }) break;
                EnsureShape(actualClass);
                EnsureShape(canonicalClass);
                if (actualClass.IsValueType && canonicalClass.IsValueType
                    && !SameVectorFieldStorage(actualClass, canonicalClass, visited)) return false;
                break;
            }
        }
        return true;
    }

    private static NotSupportedException VectorManagedThrowUnsupported() =>
        new("Vector unsupported-type guard requires its exact closed CoreLib declaration and unmodified MethodSpec");
}
