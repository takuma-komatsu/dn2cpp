namespace Dn2Cpp;

internal static partial class CoreIntrinsics
{
    // Storage identity must survive even when its element has no numeric lowering.
    internal static bool IsVectorStorageOwner(ClassInfo owner)
    {
        if (owner.Handle.IsNil || owner.Module.AssemblyName != "System.Private.CoreLib")
            return false;
        var reader = owner.Module.Reader;
        var definition = reader.GetTypeDefinition(owner.Handle);
        if (!definition.GetDeclaringType().IsNil || definition.GetGenericParameters().Count != 1)
            return false;
        string ns = reader.GetString(definition.Namespace), name = reader.GetString(definition.Name);
        return ns == "System.Numerics" && name == "Vector`1"
            || ns == "System.Runtime.Intrinsics" && name is "Vector64`1" or "Vector128`1" or "Vector256`1" or "Vector512`1";
    }

    internal static bool IsMappedVectorStorage(ClassInfo owner) => IsVectorStorageOwner(owner)
        && owner.IntrinsicCppName is "Dn2CppVectorT" or "Dn2CppVector64" or "Dn2CppVector128"
            or "Dn2CppVector256" or "Dn2CppVector512";
}
