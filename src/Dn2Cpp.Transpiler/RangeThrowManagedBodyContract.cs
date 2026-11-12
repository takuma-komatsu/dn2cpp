using System.Reflection;
using System.Reflection.Metadata;

namespace Dn2Cpp;

// The range helper constructs its diagnostic in ordinary IL; a sink that drops
// arguments loses ParamName, ActualValue and the enum-aware range message.
internal static class RangeThrowManagedBodyContract
{
    internal static bool IsName(string? owner, string name) =>
        owner == "System.ThrowHelper" && name == "ThrowArgumentOutOfRange_Range";

    internal static bool IsSignature(BlobReader blob) =>
        blob.RemainingBytes == 11 && blob.ReadByte() == 0x10 && blob.ReadByte() == 1
        && blob.ReadByte() == 4 && blob.ReadByte() == 1 && blob.ReadByte() == 0x0E
        && blob.ReadByte() == 0x1E && blob.ReadByte() == 0
        && blob.ReadByte() == 0x1E && blob.ReadByte() == 0
        && blob.ReadByte() == 0x1E && blob.ReadByte() == 0;

    internal static bool IsDefinition(MetadataReader reader, MethodDefinitionHandle handle)
    {
        if (handle.IsNil || !reader.IsAssembly
            || !reader.StringComparer.Equals(reader.GetAssemblyDefinition().Name, "System.Private.CoreLib"))
            return false;
        var method = reader.GetMethodDefinition(handle);
        var owner = reader.GetTypeDefinition(method.GetDeclaringType());
        if (!reader.StringComparer.Equals(method.Name, "ThrowArgumentOutOfRange_Range")
            || method.Attributes != (MethodAttributes.Assembly | MethodAttributes.Static | MethodAttributes.HideBySig)
            || method.ImplAttributes != MethodImplAttributes.IL || method.RelativeVirtualAddress == 0
            || !owner.GetDeclaringType().IsNil || owner.GetGenericParameters().Count != 0
            || !reader.StringComparer.Equals(owner.Namespace, "System") || !reader.StringComparer.Equals(owner.Name, "ThrowHelper")
            || (owner.Attributes & (TypeAttributes.Interface | TypeAttributes.Abstract | TypeAttributes.Sealed))
                != (TypeAttributes.Abstract | TypeAttributes.Sealed)
            || method.GetGenericParameters().Count != 1 || !IsSignature(reader.GetBlobReader(method.Signature)))
            return false;
        foreach (var parameterHandle in method.GetGenericParameters())
        {
            var parameter = reader.GetGenericParameter(parameterHandle);
            if (parameter.Parent != handle || parameter.Index != 0
                || parameter.Attributes != GenericParameterAttributes.None || parameter.GetConstraints().Count != 0)
                return false;
        }
        return true;
    }
}
