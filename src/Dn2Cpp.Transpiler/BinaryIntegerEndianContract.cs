using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using SRME = System.Reflection.Metadata.Ecma335.MetadataTokens;

namespace Dn2Cpp;

// The admitted leaves keep CoreLib's range, signedness and byte-order logic.
// Shared with ILDiet so copied-body traversal follows the same finite closure.
internal static class BinaryIntegerEndianContract
{
    internal static string? Owner(PrimitiveTypeCode primitive) => primitive switch
    {
        PrimitiveTypeCode.Char => "System.Char",
        PrimitiveTypeCode.Int32 => "System.Int32",
        PrimitiveTypeCode.Int64 => "System.Int64",
        PrimitiveTypeCode.IntPtr => "System.IntPtr",
        PrimitiveTypeCode.UInt32 => "System.UInt32",
        PrimitiveTypeCode.UInt64 => "System.UInt64",
        PrimitiveTypeCode.UIntPtr => "System.UIntPtr",
        _ => null,
    };

    internal static PrimitiveTypeCode Primitive(string owner) => owner switch
    {
        "System.Char" => PrimitiveTypeCode.Char,
        "System.Int32" => PrimitiveTypeCode.Int32,
        "System.Int64" => PrimitiveTypeCode.Int64,
        "System.IntPtr" => PrimitiveTypeCode.IntPtr,
        "System.UInt32" => PrimitiveTypeCode.UInt32,
        "System.UInt64" => PrimitiveTypeCode.UInt64,
        "System.UIntPtr" => PrimitiveTypeCode.UIntPtr,
        _ => PrimitiveTypeCode.Void,
    };

    internal static bool IsSlot(string name) => name is "TryReadBigEndian" or "TryReadLittleEndian";
    internal static bool IsDefault(string name) => name is "ReadBigEndian" or "ReadLittleEndian";

    internal static string LeafName(string owner, string slot) =>
        "System.Numerics.IBinaryInteger<" + (owner switch
        {
            "System.IntPtr" => "nint", "System.UIntPtr" => "nuint", _ => owner,
        }) + ">." + slot;

    internal static bool IsLeafCandidate(string owner, string name) => Primitive(owner) != PrimitiveTypeCode.Void
        && (name == LeafName(owner, "TryReadBigEndian") || name == LeafName(owner, "TryReadLittleEndian"));

    internal static bool IsNamedType(MetadataReader reader, TypeDefinitionHandle handle, string ns, string name) =>
        !handle.IsNil && reader.GetTypeDefinition(handle) is var type && type.GetDeclaringType().IsNil
        && reader.StringComparer.Equals(type.Namespace, ns) && reader.StringComparer.Equals(type.Name, name);

    private static bool ReadSpan(MetadataReader reader, ref BlobReader signature)
    {
        if (signature.RemainingBytes < 5 || signature.ReadByte() != 0x15 || signature.ReadByte() != 0x11
            || !signature.TryReadCompressedInteger(out int coded) || (coded & 3) != 0 || coded >> 2 == 0
            || coded >> 2 > reader.TypeDefinitions.Count
            || !IsNamedType(reader, SRME.TypeDefinitionHandle(coded >> 2), "System", "ReadOnlySpan`1")) return false;
        return signature.RemainingBytes >= 2 && signature.ReadByte() == 1 && signature.ReadByte() == 5;
    }

    internal static bool IsTrySignature(MetadataReader reader, BlobReader signature, PrimitiveTypeCode primitive)
    {
        if (signature.RemainingBytes < 3 || signature.ReadByte() != 0 || signature.ReadByte() != 3
            || signature.ReadByte() != 2 || !ReadSpan(reader, ref signature)
            || signature.RemainingBytes < 3 || signature.ReadByte() != 2 || signature.ReadByte() != 0x10) return false;
        return primitive == PrimitiveTypeCode.Void
            ? signature.RemainingBytes == 2 && signature.ReadByte() == 0x13 && signature.ReadByte() == 0
            : signature.RemainingBytes == 1 && signature.ReadByte() == (byte)primitive;
    }

    internal static bool IsDefaultSignature(MetadataReader reader, BlobReader signature)
    {
        if (signature.RemainingBytes < 5 || signature.ReadByte() != 0) return false;
        int count = signature.ReadByte();
        if (count is not (2 or 3) || signature.ReadByte() != 0x13 || signature.ReadByte() != 0) return false;
        if (signature.ReadByte() == 0x1d)
        {
            if (signature.RemainingBytes != count || signature.ReadByte() != 5
                || count == 3 && signature.ReadByte() != 8) return false;
        }
        else
        {
            signature.Offset--;
            if (count != 2 || !ReadSpan(reader, ref signature)) return false;
        }
        return signature.RemainingBytes == 1 && signature.ReadByte() == 2;
    }

    internal static bool IsLeafDefinition(MetadataReader reader, MethodDefinitionHandle handle, string owner, string name)
    {
        if (!IsLeafCandidate(owner, name) || handle.IsNil || !reader.IsAssembly
            || !reader.StringComparer.Equals(reader.GetAssemblyDefinition().Name, "System.Private.CoreLib")) return false;
        var method = reader.GetMethodDefinition(handle);
        var type = reader.GetTypeDefinition(method.GetDeclaringType());
        return IsNamedType(reader, method.GetDeclaringType(), "System", owner.Substring(7))
            && type.GetGenericParameters().Count == 0
            && (type.Attributes & (TypeAttributes.Interface | TypeAttributes.Abstract | TypeAttributes.Sealed)) == TypeAttributes.Sealed
            && type.BaseType.Kind == HandleKind.TypeDefinition
            && IsNamedType(reader, (TypeDefinitionHandle)type.BaseType, "System", "ValueType")
            && reader.StringComparer.Equals(method.Name, name) && method.GetGenericParameters().Count == 0
            && method.Attributes == (MethodAttributes.Private | MethodAttributes.Static | MethodAttributes.HideBySig)
            && method.ImplAttributes == MethodImplAttributes.IL && method.RelativeVirtualAddress != 0
            && IsTrySignature(reader, reader.GetBlobReader(method.Signature), Primitive(owner));
    }

    internal static bool IsImplementationParent(MetadataReader reader, EntityHandle handle,
        TypeDefinitionHandle contract, PrimitiveTypeCode primitive)
    {
        if (handle.Kind != HandleKind.TypeSpecification) return false;
        var signature = reader.GetBlobReader(reader.GetTypeSpecification((TypeSpecificationHandle)handle).Signature);
        return signature.RemainingBytes >= 5 && signature.ReadByte() == 0x15 && signature.ReadByte() == 0x12
            && signature.TryReadCompressedInteger(out int coded) && (coded & 3) == 0
            && coded >> 2 == SRME.GetRowNumber(contract)
            && signature.RemainingBytes == 2 && signature.ReadByte() == 1 && signature.ReadByte() == (byte)primitive;
    }

    internal static bool IsOverflowHelperDefinition(MetadataReader reader, PEReader pe, MethodDefinitionHandle handle)
    {
        if (handle.IsNil || !reader.IsAssembly
            || !reader.StringComparer.Equals(reader.GetAssemblyDefinition().Name, "System.Private.CoreLib")) return false;
        var method = reader.GetMethodDefinition(handle);
        var owner = reader.GetTypeDefinition(method.GetDeclaringType());
        var signature = reader.GetBlobReader(method.Signature);
        if (!IsNamedType(reader, method.GetDeclaringType(), "System", "ThrowHelper")
            || owner.GetGenericParameters().Count != 0
            || !reader.StringComparer.Equals(method.Name, "ThrowOverflowException") || method.GetGenericParameters().Count != 0
            || method.Attributes != (MethodAttributes.Assembly | MethodAttributes.Static | MethodAttributes.HideBySig)
            || method.ImplAttributes != MethodImplAttributes.IL || method.RelativeVirtualAddress == 0
            || signature.RemainingBytes != 3 || signature.ReadByte() != 0 || signature.ReadByte() != 0 || signature.ReadByte() != 1)
            return false;
        var body = pe.GetMethodBody(method.RelativeVirtualAddress);
        var il = body.GetILReader();
        if (!body.LocalSignature.IsNil || body.ExceptionRegions.Length != 0 || il.Length != 6 || il.ReadByte() != 0x73)
            return false;
        int token = il.ReadInt32();
        if (il.ReadByte() != 0x7a || (uint)token >> 24 != 6 || (token & 0xffffff) == 0
            || (token & 0xffffff) > reader.MethodDefinitions.Count) return false;
        var ctor = reader.GetMethodDefinition(SRME.MethodDefinitionHandle(token & 0xffffff));
        var ctorSignature = reader.GetBlobReader(ctor.Signature);
        return IsNamedType(reader, ctor.GetDeclaringType(), "System", "OverflowException")
            && reader.GetTypeDefinition(ctor.GetDeclaringType()).GetGenericParameters().Count == 0
            && reader.StringComparer.Equals(ctor.Name, ".ctor") && ctor.GetGenericParameters().Count == 0
            && ctor.Attributes == (MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName)
            && ctor.ImplAttributes == MethodImplAttributes.IL && ctor.RelativeVirtualAddress != 0
            && ctorSignature.RemainingBytes == 3 && ctorSignature.ReadByte() == 0x20
            && ctorSignature.ReadByte() == 0 && ctorSignature.ReadByte() == 1;
    }
}
