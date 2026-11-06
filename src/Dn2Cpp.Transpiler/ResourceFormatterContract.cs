using System.Reflection.Metadata;

namespace Dn2Cpp;

// These externs enter a permanently unsupported resource deserializer, not a target loader.
internal static class ResourceFormatterContract
{
    private const string TargetIdentity = "System.Runtime.Serialization.Formatters.Binary.BinaryFormatter, System.Runtime.Serialization.Formatters";

    internal static bool IsAccessorSignature(MetadataReader reader, BlobReader blob, bool constructor, bool allowTypeReference = true)
    {
        try
        {
            if (blob.ReadByte() != 0 || blob.ReadByte() != (constructor ? 0 : 2)
                || blob.ReadByte() != 0x1c) return false;
            if (!constructor)
            {
                if (blob.ReadByte() != 0x1c || blob.ReadByte() != 0x12) return false;
                var stream = blob.ReadTypeHandle();
                if (stream.Kind == HandleKind.TypeDefinition)
                {
                    if (!IsType(reader, (TypeDefinitionHandle)stream, "System.IO", "Stream")
                        || reader.GetTypeDefinition((TypeDefinitionHandle)stream).GetGenericParameters().Count != 0) return false;
                }
                else if (allowTypeReference && stream.Kind == HandleKind.TypeReference)
                {
                    var type = reader.GetTypeReference((TypeReferenceHandle)stream);
                    if (!reader.StringComparer.Equals(type.Namespace, "System.IO")
                        || !reader.StringComparer.Equals(type.Name, "Stream")) return false;
                }
                else return false;
            }
            return blob.RemainingBytes == 0;
        }
        catch (BadImageFormatException) { return false; }
    }

    internal static bool IsAccessor(MetadataReader reader, MethodDefinitionHandle handle)
    {
        try
        {
            if (handle.IsNil || !reader.IsAssembly
                || !reader.StringComparer.Equals(reader.GetAssemblyDefinition().Name, "System.Private.CoreLib")) return false;
            var method = reader.GetMethodDefinition(handle);
            bool constructor = reader.StringComparer.Equals(method.Name, "CreateBinaryFormatter");
            if (!constructor && !reader.StringComparer.Equals(method.Name, "Deserialize")) return false;
            if ((int)method.Attributes != 0x91 || (int)method.ImplAttributes != 0 || method.RelativeVirtualAddress != 0
                || method.GetGenericParameters().Count != 0
                || !IsType(reader, method.GetDeclaringType(), "System.Resources", "ResourceReader")
                || reader.GetTypeDefinition(method.GetDeclaringType()).GetGenericParameters().Count != 0
                || !IsAccessorSignature(reader, reader.GetBlobReader(method.Signature), constructor, allowTypeReference: false)) return false;
            int accessor = 0;
            foreach (var h in method.GetCustomAttributes())
            {
                var attribute = reader.GetCustomAttribute(h);
                if (!IsAttribute(reader, attribute, "UnsafeAccessorAttribute", 1)) continue;
                var blob = reader.GetBlobReader(attribute.Value);
                if (blob.ReadUInt16() != 1 || blob.ReadInt32() != (constructor ? 0 : 1)
                    || blob.ReadUInt16() != (constructor ? 0 : 1)) return false;
                if (!constructor && (blob.ReadByte() != 0x54 || blob.ReadByte() != 0x0e
                    || !ReadString(ref blob, "Name") || !ReadString(ref blob, "Deserialize"))) return false;
                if (blob.RemainingBytes != 0) return false;
                accessor++;
            }
            if (accessor != 1) return false;
            int marker = 0, streamParameter = 0;
            foreach (var h in method.GetParameters())
            {
                var parameter = reader.GetParameter(h);
                bool annotated = parameter.SequenceNumber == (constructor ? 0 : 1);
                if (!annotated && (constructor || parameter.SequenceNumber != 2)) return false;
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
                if (target != (annotated ? 1 : 0)) return false;
                if (annotated) marker++; else streamParameter++;
            }
            return marker == 1 && streamParameter == (constructor ? 0 : 1);
        }
        catch (BadImageFormatException) { return false; }
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
            || !IsType(reader, ctor.GetDeclaringType(), "System.Runtime.CompilerServices", name)
            || reader.GetTypeDefinition(ctor.GetDeclaringType()).GetGenericParameters().Count != 0) return false;
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
