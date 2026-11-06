using System.Reflection;
using System.Reflection.Metadata;
using SRME = System.Reflection.Metadata.Ecma335.MetadataTokens;

namespace Dn2Cpp;

// The source-side fallback remains CoreLib IL, including false plus default(out).
// ILDiet traverses these definitions without admitting additional closed pairs.
internal static class PrimitiveWideConversionContract
{
    internal static string? Owner(PrimitiveTypeCode source) => source switch
    {
        PrimitiveTypeCode.Int32 => "System.Int32",
        PrimitiveTypeCode.UInt32 => "System.UInt32",
        PrimitiveTypeCode.Int64 => "System.Int64",
        _ => null,
    };

    internal static PrimitiveTypeCode Source(string owner) => owner switch
    {
        "System.Int32" => PrimitiveTypeCode.Int32,
        "System.UInt32" => PrimitiveTypeCode.UInt32,
        "System.Int64" => PrimitiveTypeCode.Int64,
        _ => PrimitiveTypeCode.Void,
    };

    internal static string Slot(PrimitiveTypeCode source) => source == PrimitiveTypeCode.Int64
        ? "TryConvertToChecked" : "TryConvertToTruncating";

    internal static bool IsPair(PrimitiveTypeCode source, string slot, string target) =>
        source == PrimitiveTypeCode.Int64 && slot == "TryConvertToChecked" && target == "System.Numerics.BigInteger"
        || source is PrimitiveTypeCode.Int32 or PrimitiveTypeCode.UInt32 && slot == "TryConvertToTruncating"
            && target is "System.Int128" or "System.UInt128";

    internal static string? BodyName(string owner) => owner switch
    {
        "System.Int32" => "System.Numerics.INumberBase<System.Int32>.TryConvertToTruncating",
        "System.UInt32" => "System.Numerics.INumberBase<System.UInt32>.TryConvertToTruncating",
        "System.Int64" => "System.Numerics.INumberBase<System.Int64>.TryConvertToChecked",
        _ => null,
    };

    internal static bool IsBodyName(string owner, string name) => BodyName(owner) is { } expected && name == expected;

    internal static bool IsSignature(BlobReader blob, PrimitiveTypeCode source)
    {
        if (!Byte(ref blob, 0x10) || !Byte(ref blob, 1) || !Byte(ref blob, 2) || !Byte(ref blob, 2)) return false;
        if (source == PrimitiveTypeCode.Void)
        {
            if (!Byte(ref blob, 0x13) || !Byte(ref blob, 0)) return false;
        }
        else if (!Byte(ref blob, (byte)source)) return false;
        return Byte(ref blob, 0x10) && Byte(ref blob, 0x1e) && Byte(ref blob, 0) && blob.RemainingBytes == 0;
    }

    internal static bool HasConstraint(MetadataReader reader, MethodDefinition method)
    {
        var parameters = method.GetGenericParameters();
        if (parameters.Count != 1) return false;
        var parameter = reader.GetGenericParameter(parameters.First());
        var constraints = parameter.GetConstraints();
        if (parameter.Index != 0 || parameter.Attributes != GenericParameterAttributes.None || constraints.Count != 1) return false;
        var constraint = reader.GetGenericParameterConstraint(constraints.First()).Type;
        if (constraint.Kind != HandleKind.TypeSpecification) return false;
        var blob = reader.GetBlobReader(reader.GetTypeSpecification((TypeSpecificationHandle)constraint).Signature);
        return Byte(ref blob, 0x15) && Byte(ref blob, 0x12)
            && blob.TryReadCompressedInteger(out int coded) && (coded & 3) == 0 && coded >> 2 != 0
            && coded >> 2 <= reader.TypeDefinitions.Count
            && BinaryIntegerEndianContract.IsNamedType(reader, SRME.TypeDefinitionHandle(coded >> 2), "System.Numerics", "INumberBase`1")
            && Byte(ref blob, 1) && Byte(ref blob, 0x1e) && Byte(ref blob, 0) && blob.RemainingBytes == 0;
    }

    internal static bool IsBodyDefinition(MetadataReader reader, MethodDefinitionHandle handle, string owner, string name)
    {
        if (!IsBodyName(owner, name) || handle.IsNil || !reader.IsAssembly
            || !reader.StringComparer.Equals(reader.GetAssemblyDefinition().Name, "System.Private.CoreLib")) return false;
        var method = reader.GetMethodDefinition(handle);
        var type = reader.GetTypeDefinition(method.GetDeclaringType());
        return BinaryIntegerEndianContract.IsNamedType(reader, method.GetDeclaringType(), "System", owner.Substring(7))
            && type.GetGenericParameters().Count == 0
            && (type.Attributes & (TypeAttributes.Interface | TypeAttributes.Abstract | TypeAttributes.Sealed)) == TypeAttributes.Sealed
            && type.BaseType.Kind == HandleKind.TypeDefinition
            && BinaryIntegerEndianContract.IsNamedType(reader, (TypeDefinitionHandle)type.BaseType, "System", "ValueType")
            && reader.StringComparer.Equals(method.Name, name)
            && method.Attributes == (MethodAttributes.Private | MethodAttributes.Static | MethodAttributes.HideBySig)
            && method.ImplAttributes == MethodImplAttributes.AggressiveInlining && method.RelativeVirtualAddress != 0
            && HasConstraint(reader, method) && IsSignature(reader.GetBlobReader(method.Signature), Source(owner));
    }

    private static bool Byte(ref BlobReader blob, byte value) => blob.RemainingBytes != 0 && blob.ReadByte() == value;
}
