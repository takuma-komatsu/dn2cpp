namespace Dn2Cpp;

internal sealed partial class Compilation
{
    // Physical storage, not numeric admission: canonical fields retain their width.
    internal bool ContainsFixedVectorStorage(TypeDesc type) =>
        ContainsFixedVectorStorage(type, new HashSet<ClassInfo>());

    private bool ContainsFixedVectorStorage(TypeDesc type, HashSet<ClassInfo> visited)
    {
        if (type is not { Kind: TypeKind.Class, Class: { } owner } || !visited.Add(owner)) return false;
        EnsureShape(owner);
        if (!owner.IsValueType || owner.IsEnum) return false;
        if (CoreIntrinsics.IsFixedVectorValueStorageClass(owner)) return true;
        if (owner.IntrinsicCppName is not null) return false;
        foreach (var field in owner.Fields.ToArray())
            if (!field.IsStatic && !field.IsLiteral && ContainsFixedVectorStorage(field.Type, visited)) return true;
        return false;
    }
}
