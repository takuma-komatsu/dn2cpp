using System.Reflection.Metadata;

namespace Dn2Cpp;

// One CoreLib accessor names a managed target which has no AssemblyRef edge.
internal static class DefaultPrincipalContract
{
    internal const string AccessorName = "<GetThreadPrincipal>g__GetDefaultPrincipal|107_0";
    internal const string TargetAssembly = "System.Security.Claims";
    internal const string TargetType = "System.Security.Principal.GenericPrincipal";
    internal const string TargetMethod = "GetDefaultInstance";
    private const string TargetIdentity = TargetType + ", " + TargetAssembly;

    internal static bool IsAccessorSignature(BlobReader blob) => blob.Length == 4
        && blob.ReadByte() == 0 && blob.ReadByte() == 1
        && blob.ReadByte() == 0x1c && blob.ReadByte() == 0x1c;

    internal static bool IsAccessor(MetadataReader reader, MethodDefinitionHandle handle)
    {
        if (handle.IsNil || !reader.IsAssembly
            || !reader.StringComparer.Equals(reader.GetAssemblyDefinition().Name, "System.Private.CoreLib")) return false;
        var method = reader.GetMethodDefinition(handle);
        if (!reader.StringComparer.Equals(method.Name, AccessorName)
            || (int)method.Attributes != 0x93 || (int)method.ImplAttributes != 0 || method.RelativeVirtualAddress != 0
            || method.GetGenericParameters().Count != 0
            || !IsType(reader, method.GetDeclaringType(), "System", "AppDomain")
            || reader.GetTypeDefinition(method.GetDeclaringType()).GetGenericParameters().Count != 0
            || !IsAccessorSignature(reader.GetBlobReader(method.Signature))) return false;
        try
        {
            int generated = 0, accessor = 0;
            foreach (var h in method.GetCustomAttributes())
            {
                var attribute = reader.GetCustomAttribute(h);
                if (IsAttribute(reader, attribute, "CompilerGeneratedAttribute", 0))
                {
                    var blob = reader.GetBlobReader(attribute.Value);
                    if (blob.Length != 4 || blob.ReadUInt16() != 1 || blob.ReadUInt16() != 0) return false;
                    generated++;
                }
                if (IsAttribute(reader, attribute, "UnsafeAccessorAttribute", 1))
                {
                    var blob = reader.GetBlobReader(attribute.Value);
                    if (blob.ReadUInt16() != 1 || blob.ReadInt32() != 2 || blob.ReadUInt16() != 1
                        || blob.ReadByte() != 0x54 || blob.ReadByte() != 0x0e
                        || !ReadString(ref blob, "Name") || !ReadString(ref blob, TargetMethod)
                        || blob.RemainingBytes != 0) return false;
                    accessor++;
                }
            }
            if (generated != 1 || accessor != 1) return false;
            int result = 0, marker = 0;
            foreach (var h in method.GetParameters())
            {
                var parameter = reader.GetParameter(h);
                if (parameter.SequenceNumber is not (0 or 1)) return false;
                int target = 0;
                foreach (var ah in parameter.GetCustomAttributes())
                {
                    var attribute = reader.GetCustomAttribute(ah);
                    if (!IsAttribute(reader, attribute, "UnsafeAccessorTypeAttribute", 2)) continue;
                    var blob = reader.GetBlobReader(attribute.Value);
                    if (blob.ReadUInt16() != 1 || !ReadString(ref blob, TargetIdentity)
                        || blob.ReadUInt16() != 0 || blob.RemainingBytes != 0) return false;
                    target++;
                }
                if (target != 1) return false;
                if (parameter.SequenceNumber == 0) result++; else marker++;
            }
            return result == 1 && marker == 1;
        }
        catch (BadImageFormatException) { return false; }
    }

    internal static bool HasAccessor(MetadataReader reader)
    {
        if (!reader.IsAssembly || !reader.StringComparer.Equals(reader.GetAssemblyDefinition().Name, "System.Private.CoreLib")) return false;
        foreach (var type in reader.TypeDefinitions)
            if (IsType(reader, type, "System", "AppDomain"))
                foreach (var method in reader.GetTypeDefinition(type).GetMethods())
                    if (IsAccessor(reader, method)) return true;
        return false;
    }

    internal static MethodDefinitionHandle FindTarget(MetadataReader reader)
    {
        if (!reader.IsAssembly || !reader.StringComparer.Equals(reader.GetAssemblyDefinition().Name, TargetAssembly)) return default;
        MethodDefinitionHandle found = default;
        foreach (var type in reader.TypeDefinitions)
        {
            if (!IsType(reader, type, "System.Security.Principal", "GenericPrincipal")) continue;
            if (reader.GetTypeDefinition(type).GetGenericParameters().Count != 0) return default;
            foreach (var handle in reader.GetTypeDefinition(type).GetMethods())
            {
                var method = reader.GetMethodDefinition(handle);
                if (!reader.StringComparer.Equals(method.Name, TargetMethod)) continue;
                if (!found.IsNil || (int)method.Attributes != 0x91 || (int)method.ImplAttributes != 0
                    || method.RelativeVirtualAddress == 0 || method.GetGenericParameters().Count != 0) return default;
                try
                {
                    var blob = reader.GetBlobReader(method.Signature);
                    if (blob.ReadByte() != 0 || blob.ReadCompressedInteger() != 0 || blob.ReadByte() != 0x12
                        || blob.ReadTypeHandle() != type || blob.RemainingBytes != 0) return default;
                }
                catch (BadImageFormatException) { return default; }
                found = handle;
            }
        }
        return found;
    }

    private static bool IsType(MetadataReader reader, TypeDefinitionHandle handle, string ns, string name)
    {
        var type = reader.GetTypeDefinition(handle);
        return type.GetDeclaringType().IsNil && reader.StringComparer.Equals(type.Namespace, ns)
            && reader.StringComparer.Equals(type.Name, name);
    }

    private static bool ReadString(ref BlobReader blob, string expected)
    {
        if (blob.ReadCompressedInteger() != expected.Length) return false;
        for (int i = 0; i < expected.Length; i++)
            if (blob.ReadByte() != expected[i]) return false;
        return true;
    }

    private static bool IsInt32Enum(MetadataReader reader, TypeDefinitionHandle handle)
    {
        var type = reader.GetTypeDefinition(handle);
        if (type.BaseType.Kind != HandleKind.TypeDefinition
            || !IsType(reader, (TypeDefinitionHandle)type.BaseType, "System", "Enum")
            || type.GetGenericParameters().Count != 0) return false;
        int fields = 0;
        foreach (var fh in type.GetFields())
        {
            var field = reader.GetFieldDefinition(fh);
            if ((field.Attributes & System.Reflection.FieldAttributes.Static) != 0) continue;
            var blob = reader.GetBlobReader(field.Signature);
            if (!reader.StringComparer.Equals(field.Name, "value__") || (int)field.Attributes != 0x606
                || blob.Length != 2 || blob.ReadByte() != 6 || blob.ReadByte() != 8) return false;
            fields++;
        }
        return fields == 1;
    }

    // Attribute constructors must belong to this CoreLib, not a same-named facade.
    private static bool IsAttribute(MetadataReader reader, CustomAttribute attribute, string name, int shape)
    {
        if (attribute.Constructor.Kind != HandleKind.MethodDefinition) return false;
        var ctor = reader.GetMethodDefinition((MethodDefinitionHandle)attribute.Constructor);
        if ((int)ctor.Attributes != 0x1886 || (int)ctor.ImplAttributes != 0
            || ctor.RelativeVirtualAddress == 0 || ctor.GetGenericParameters().Count != 0
            || !reader.StringComparer.Equals(ctor.Name, ".ctor")
            || !IsType(reader, ctor.GetDeclaringType(), "System.Runtime.CompilerServices", name)) return false;
        var blob = reader.GetBlobReader(ctor.Signature);
        if (blob.ReadByte() != 0x20 || blob.ReadCompressedInteger() != (shape == 0 ? 0 : 1)
            || blob.ReadByte() != 1) return false;
        if (shape == 1)
        {
            if (blob.ReadByte() != 0x11) return false;
            var kind = blob.ReadTypeHandle();
            if (kind.Kind != HandleKind.TypeDefinition
                || !IsType(reader, (TypeDefinitionHandle)kind, "System.Runtime.CompilerServices", "UnsafeAccessorKind")
                || !IsInt32Enum(reader, (TypeDefinitionHandle)kind)) return false;
        }
        else if (shape == 2 && blob.ReadByte() != 0x0e) return false;
        return blob.RemainingBytes == 0;
    }
}
