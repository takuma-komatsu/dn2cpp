using System.Reflection;
using System.Reflection.Metadata;
using SRME = System.Reflection.Metadata.Ecma335.MetadataTokens;
using System.Reflection.PortableExecutable;

namespace Dn2Cpp;

// Shared with ILDiet: only these scalar bodies have an admitted managed closure.
internal static class ScalarNumericLeafContract
{
    internal static bool IsName(string name) => name is "Sign" or "get_Exponent" or "get_Significand"
        or "get_BiasedExponent" or "get_TrailingSignificand"
        or "ExtractBiasedExponentFromBits" or "ExtractTrailingSignificandFromBits";

    internal static bool IsCandidate(string? owner, string name) => owner switch
    {
        "System.Int32" => name == "Sign",
        "System.Decimal" => name == "get_Exponent",
        "System.Single" or "System.Double" => IsName(name) && name != "Sign",
        _ => false,
    };

    internal static bool IsInstance(string name) => name.StartsWith("get_", StringComparison.Ordinal);

    internal static MethodAttributes Attributes(string owner, string name) =>
        IsInstance(name)
            ? (owner == "System.Decimal" ? MethodAttributes.Private : MethodAttributes.Assembly)
                | MethodAttributes.HideBySig | MethodAttributes.SpecialName
            : (name == "Sign" ? MethodAttributes.Public : MethodAttributes.Assembly)
                | MethodAttributes.Static | MethodAttributes.HideBySig;

    internal static PrimitiveTypeCode Result(string owner, string name) => name switch
    {
        "Sign" => PrimitiveTypeCode.Int32,
        "get_Exponent" => owner == "System.Double" ? PrimitiveTypeCode.Int16 : PrimitiveTypeCode.SByte,
        "get_BiasedExponent" or "ExtractBiasedExponentFromBits" =>
            owner == "System.Single" ? PrimitiveTypeCode.Byte : PrimitiveTypeCode.UInt16,
        _ => owner == "System.Single" ? PrimitiveTypeCode.UInt32 : PrimitiveTypeCode.UInt64,
    };

    internal static PrimitiveTypeCode Parameter(string owner) => owner switch
    {
        "System.Int32" => PrimitiveTypeCode.Int32,
        "System.Single" => PrimitiveTypeCode.UInt32,
        _ => PrimitiveTypeCode.UInt64,
    };

    internal static bool IsSignature(string owner, string name, BlobReader blob)
    {
        if (!IsCandidate(owner, name)) return false;
        bool instance = IsInstance(name);
        return blob.RemainingBytes == (instance ? 3 : 4)
            && blob.ReadByte() == (instance ? 0x20 : 0)
            && blob.ReadByte() == (instance ? 0 : 1)
            && blob.ReadByte() == (byte)Result(owner, name)
            && (instance || blob.ReadByte() == (byte)Parameter(owner));
    }

    internal static bool IsDefinition(MetadataReader reader, MethodDefinitionHandle handle, string owner, string name)
    {
        if (!IsCandidate(owner, name) || handle.IsNil || !reader.IsAssembly
            || !reader.StringComparer.Equals(reader.GetAssemblyDefinition().Name, "System.Private.CoreLib")) return false;
        var method = reader.GetMethodDefinition(handle);
        var type = reader.GetTypeDefinition(method.GetDeclaringType());
        string simple = owner switch { "System.Int32" => "Int32", "System.Single" => "Single", "System.Double" => "Double", _ => "Decimal" };
        if (!reader.StringComparer.Equals(type.Namespace, "System") || !reader.StringComparer.Equals(type.Name, simple)
            || !type.GetDeclaringType().IsNil || type.GetGenericParameters().Count != 0
            || (type.Attributes & (TypeAttributes.Interface | TypeAttributes.Abstract | TypeAttributes.Sealed)) != TypeAttributes.Sealed
            || type.BaseType.Kind != HandleKind.TypeDefinition) return false;
        var baseType = reader.GetTypeDefinition((TypeDefinitionHandle)type.BaseType);
        return baseType.GetDeclaringType().IsNil && reader.StringComparer.Equals(baseType.Namespace, "System")
            && reader.StringComparer.Equals(baseType.Name, "ValueType")
            && reader.StringComparer.Equals(method.Name, name) && method.GetGenericParameters().Count == 0
            && method.Attributes == Attributes(owner, name) && method.ImplAttributes == MethodImplAttributes.IL
            && method.RelativeVirtualAddress != 0 && IsSignature(owner, name, reader.GetBlobReader(method.Signature));
    }

    internal static bool RequiresSingleStorageLoad(string owner, string name) => owner == "System.Single"
        && name is "get_BiasedExponent" or "get_TrailingSignificand";

    internal static bool IsBody(MetadataReader reader, PEReader pe, MethodDefinitionHandle handle, string owner, string name) =>
        !RequiresSingleStorageLoad(owner, name) || TrySingleStorageTargets(reader, pe, handle, name, out _, out _);

    internal static bool TrySingleStorageTargets(MetadataReader reader, PEReader pe, MethodDefinitionHandle handle,
        string name, out MethodDefinitionHandle bitConverter, out MethodDefinitionHandle extract)
    {
        bitConverter = default; extract = default;
        var method = reader.GetMethodDefinition(handle);
        if (method.RelativeVirtualAddress == 0) return false;
        var body = pe.GetMethodBody(method.RelativeVirtualAddress);
        var il = body.GetILReader();
        // A representation read must not widen a signaling NaN before BitConverter.
        if (!body.LocalSignature.IsNil || body.ExceptionRegions.Length != 0 || il.Length != 13
            || il.ReadByte() != 0x02 || il.ReadByte() != 0x4e || il.ReadByte() != 0x28) return false;
        int bitToken = il.ReadInt32();
        if (il.ReadByte() != 0x28) return false;
        int extractToken = il.ReadInt32();
        if (il.ReadByte() != 0x2a || !ReadMethodToken(reader, bitToken, out bitConverter)
            || !ReadMethodToken(reader, extractToken, out extract)) return false;
        string target = name == "get_BiasedExponent" ? "ExtractBiasedExponentFromBits" : "ExtractTrailingSignificandFromBits";
        return IsSingleToUInt32BitsDefinition(reader, bitConverter)
            && reader.GetMethodDefinition(extract).GetDeclaringType() == method.GetDeclaringType()
            && IsDefinition(reader, extract, "System.Single", target);
    }

    private static bool ReadMethodToken(MetadataReader reader, int token, out MethodDefinitionHandle handle)
    {
        handle = default;
        if ((uint)token >> 24 != 0x06 || (token & 0x00ffffff) == 0
            || (token & 0x00ffffff) > reader.MethodDefinitions.Count) return false;
        handle = (MethodDefinitionHandle)SRME.EntityHandle(token);
        return true;
    }

    internal static bool IsSingleToUInt32BitsDefinition(MetadataReader reader, MethodDefinitionHandle handle)
    {
        var method = reader.GetMethodDefinition(handle);
        var owner = reader.GetTypeDefinition(method.GetDeclaringType());
        var signature = reader.GetBlobReader(method.Signature);
        return owner.GetDeclaringType().IsNil && owner.GetGenericParameters().Count == 0
            && (owner.Attributes & (TypeAttributes.Interface | TypeAttributes.Abstract | TypeAttributes.Sealed))
                == (TypeAttributes.Abstract | TypeAttributes.Sealed)
            && reader.StringComparer.Equals(owner.Namespace, "System") && reader.StringComparer.Equals(owner.Name, "BitConverter")
            && reader.StringComparer.Equals(method.Name, "SingleToUInt32Bits")
            && method.Attributes == (MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig)
            && method.ImplAttributes == MethodImplAttributes.IL && method.RelativeVirtualAddress != 0
            && method.GetGenericParameters().Count == 0 && signature.RemainingBytes == 4
            && signature.ReadByte() == 0 && signature.ReadByte() == 1
            && signature.ReadByte() == (byte)PrimitiveTypeCode.UInt32 && signature.ReadByte() == (byte)PrimitiveTypeCode.Single;
    }

}
