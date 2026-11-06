namespace Dn2Cpp;

internal sealed partial class Compilation
{
    // Physical storage, not numeric admission: canonical fields retain their width.
    internal bool ContainsVectorStorage(TypeDesc type) =>
        ContainsVectorStorage(type, new HashSet<ClassInfo>());

    private bool ContainsVectorStorage(TypeDesc type, HashSet<ClassInfo> visited)
    {
        if (type is not { Kind: TypeKind.Class, Class: { } owner } || !visited.Add(owner)) return false;
        EnsureShape(owner);
        if (!owner.IsValueType || owner.IsEnum) return false;
        if (CoreIntrinsics.IsMappedVectorStorage(owner)) return true;
        if (owner.IntrinsicCppName is not null) return false;
        foreach (var field in owner.Fields.ToArray())
            if (!field.IsStatic && !field.IsLiteral && ContainsVectorStorage(field.Type, visited)) return true;
        return false;
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
        if (CoreIntrinsics.IsVectorStorageOwner(user) && CoreIntrinsics.IsVectorStorageOwner(owner))
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
                if (!MayHaveVectorFieldStorage(field, counterpart)) break;
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

    // Only Class descriptors can enter the inline-storage proof. Keep unrelated
    // field signatures lazy instead of materializing their entire type closure.
    private static bool MayHaveVectorFieldStorage(FieldInfo field, FieldInfo counterpart)
    {
        if (field.Handle.IsNil || counterpart.Handle.IsNil) return true;
        var reader = field.DeclaringClass.Module.Reader;
        var blob = reader.GetBlobReader(reader.GetFieldDefinition(field.Handle).Signature);
        if (blob.RemainingBytes < 2 || blob.ReadByte() != 0x06) return true;
        byte kind = blob.ReadByte();
        if (kind is >= 0x02 and <= 0x0e or 0x18 or 0x19 or 0x1c)
            return blob.RemainingBytes != 0;
        if (kind is 0x0f or 0x10 or 0x14 or 0x1d)
            return blob.RemainingBytes == 0;
        if (kind != 0x13 || !blob.TryReadCompressedInteger(out int index)
            || index < 0 || blob.RemainingBytes != 0) return true;
        var actual = field.DeclaringClass.Context.TypeArgs;
        var canonical = counterpart.DeclaringClass.Context.TypeArgs;
        return index >= actual.Length || index >= canonical.Length
            || (actual[index].Kind == TypeKind.Class && canonical[index].Kind == TypeKind.Class);
    }
}
