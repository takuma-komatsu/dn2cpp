using System.Reflection;
using System.Reflection.Metadata;

namespace Dn2Cpp;

internal static class ReflectionManagedBodyContract
{
    internal static bool IsName(string? owner, string name) =>
        owner == "System.Reflection.MemberInfo" && name == ".ctor"
        || owner == "System.Type" && name is "get_IsGenericTypeParameter" or "get_IsGenericMethodParameter";

    internal static bool IsDefinition(MetadataReader reader, MethodDefinitionHandle handle, string owner, string name)
    {
        if (!IsName(owner, name) || handle.IsNil || !reader.IsAssembly
            || !reader.StringComparer.Equals(reader.GetAssemblyDefinition().Name, "System.Private.CoreLib"))
            return false;
        var method = reader.GetMethodDefinition(handle);
        var type = reader.GetTypeDefinition(method.GetDeclaringType());
        bool constructor = name == ".ctor";
        var attributes = MethodAttributes.HideBySig | MethodAttributes.SpecialName
            | (constructor ? MethodAttributes.Family | MethodAttributes.RTSpecialName
                : MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.NewSlot);
        var signature = reader.GetBlobReader(method.Signature);
        return type.GetDeclaringType().IsNil && type.GetGenericParameters().Count == 0
            && reader.StringComparer.Equals(type.Namespace, constructor ? "System.Reflection" : "System")
            && reader.StringComparer.Equals(type.Name, constructor ? "MemberInfo" : "Type")
            && reader.StringComparer.Equals(method.Name, name) && method.GetGenericParameters().Count == 0
            && method.RelativeVirtualAddress != 0 && method.ImplAttributes == MethodImplAttributes.IL
            && method.Attributes == attributes && signature.RemainingBytes == 3
            && signature.ReadByte() == 0x20 && signature.ReadByte() == 0
            && signature.ReadByte() == (constructor ? 0x01 : 0x02);
    }
}
