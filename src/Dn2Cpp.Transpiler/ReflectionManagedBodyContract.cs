using System.Reflection;
using System.Reflection.Metadata;

namespace Dn2Cpp;

internal static class ReflectionManagedBodyContract
{
    internal static bool IsName(string? owner, string name) =>
        owner == "System.Reflection.MemberInfo" && name == ".ctor"
        || owner == "System.Type" && name is "get_IsGenericTypeParameter" or "get_IsGenericMethodParameter"
        || owner == "System.Reflection.Module" && name is ".ctor" or "GetCustomAttributesData" or "get_CustomAttributes"
        || owner == "System.Reflection.RuntimeModule" && name == "GetCustomAttributesData"
        || owner == "System.Reflection.CustomAttributeData" && name is ".ctor" or "GetCustomAttributes" or "get_ConstructorArguments"
        || owner == "System.Reflection.RuntimeCustomAttributeData" && name == "get_ConstructorArguments";

    internal static bool IsRuntimeDataReaderName(string? owner, string name) =>
        owner == "System.Reflection.RuntimeCustomAttributeData" && name is "GetCustomAttributesInternal" or "get_ConstructorArguments";

    private static bool ReadClass(MetadataReader reader, ref BlobReader signature, string ns, string name, byte typeCode = 0x12)
    {
        if (signature.RemainingBytes < 2 || signature.ReadByte() != typeCode
            || !signature.TryReadCompressedInteger(out int token) || (token & 3) != 0
            || token >> 2 == 0 || token >> 2 > reader.TypeDefinitions.Count)
            return false;
        var type = reader.GetTypeDefinition(System.Reflection.Metadata.Ecma335.MetadataTokens.TypeDefinitionHandle(token >> 2));
        return type.GetDeclaringType().IsNil && reader.StringComparer.Equals(type.Namespace, ns)
            && reader.StringComparer.Equals(type.Name, name);
    }

    internal static bool IsDefinition(MetadataReader reader, MethodDefinitionHandle handle, string owner, string name)
    {
        if ((!IsName(owner, name) && !IsRuntimeDataReaderName(owner, name)) || handle.IsNil || !reader.IsAssembly
            || !reader.StringComparer.Equals(reader.GetAssemblyDefinition().Name, "System.Private.CoreLib"))
            return false;
        var method = reader.GetMethodDefinition(handle);
        var type = reader.GetTypeDefinition(method.GetDeclaringType());
        bool typePredicate = owner == "System.Type";
        string typeName = owner switch
        {
            "System.Type" => "Type",
            "System.Reflection.MemberInfo" => "MemberInfo",
            "System.Reflection.Module" => "Module",
            "System.Reflection.RuntimeModule" => "RuntimeModule",
            "System.Reflection.CustomAttributeData" => "CustomAttributeData",
            _ => "RuntimeCustomAttributeData",
        };
        if (!type.GetDeclaringType().IsNil || type.GetGenericParameters().Count != 0
            || !reader.StringComparer.Equals(type.Namespace, typePredicate ? "System" : "System.Reflection")
            || !reader.StringComparer.Equals(type.Name, typeName) || !reader.StringComparer.Equals(method.Name, name)
            || method.GetGenericParameters().Count != 0 || method.RelativeVirtualAddress == 0
            || method.ImplAttributes != MethodImplAttributes.IL)
            return false;
        var signature = reader.GetBlobReader(method.Signature);
        const MethodAttributes newVirtual = MethodAttributes.Public | MethodAttributes.Virtual
            | MethodAttributes.NewSlot | MethodAttributes.HideBySig;
        if (name == ".ctor" || typePredicate)
            return method.Attributes == (typePredicate ? newVirtual | MethodAttributes.SpecialName
                    : MethodAttributes.Family | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName)
                && signature.RemainingBytes == 3 && signature.ReadByte() == 0x20 && signature.ReadByte() == 0
                && signature.ReadByte() == (typePredicate ? 0x02 : 0x01);
        if (name == "get_ConstructorArguments")
        {
            var expected = owner == "System.Reflection.CustomAttributeData" ? newVirtual : newVirtual & ~MethodAttributes.NewSlot;
            return method.Attributes == (expected | MethodAttributes.SpecialName) && signature.RemainingBytes >= 8
                && signature.ReadByte() == 0x20 && signature.ReadByte() == 0 && signature.ReadByte() == 0x15
                && ReadClass(reader, ref signature, "System.Collections.Generic", "IList`1")
                && signature.RemainingBytes >= 3 && signature.ReadByte() == 1
                && ReadClass(reader, ref signature, "System.Reflection", "CustomAttributeTypedArgument", 0x11)
                && signature.RemainingBytes == 0;
        }
        bool readerLeaf = IsRuntimeDataReaderName(owner, name);
        bool forwarding = owner == "System.Reflection.CustomAttributeData";
        bool property = name == "get_CustomAttributes";
        var attributes = readerLeaf || forwarding
            ? MethodAttributes.Static | MethodAttributes.HideBySig | (readerLeaf ? MethodAttributes.Assembly : MethodAttributes.Public)
            : (owner == "System.Reflection.RuntimeModule" ? newVirtual & ~MethodAttributes.NewSlot : newVirtual)
                | (property ? MethodAttributes.SpecialName : 0);
        return method.Attributes == attributes && signature.RemainingBytes >= 7
            && signature.ReadByte() == (readerLeaf || forwarding ? 0 : 0x20)
            && signature.ReadByte() == (readerLeaf || forwarding ? 1 : 0) && signature.ReadByte() == 0x15
            && ReadClass(reader, ref signature, "System.Collections.Generic", property ? "IEnumerable`1" : "IList`1")
            && signature.RemainingBytes >= 3 && signature.ReadByte() == 1
            && ReadClass(reader, ref signature, "System.Reflection", "CustomAttributeData")
            && (!readerLeaf && !forwarding || ReadClass(reader, ref signature, "System.Reflection", readerLeaf ? "RuntimeModule" : "Module"))
            && signature.RemainingBytes == 0;
    }
}
