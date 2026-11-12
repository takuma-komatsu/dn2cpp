using System.Reflection;
using System.Reflection.Metadata;

namespace Dn2Cpp;

internal static partial class CoreIntrinsics
{
    internal static bool IsDateTimeFormatInfoAllDateTimePatterns(MethodInfo method)
    {
        if (!(method.Name == "GetAllDateTimePatterns")
            || !IsDateTimeFormatInfoMethodOwner(method, MethodAttributes.Public | MethodAttributes.HideBySig))
            return false;
        var reader = method.Module.Reader;
        var definition = reader.GetMethodDefinition(method.Handle);
        return IsDateTimeFormatInfoAllDateTimePatternsSignature(reader.GetBlobReader(definition.Signature))
            && IsDateTimeFormatInfoAllDateTimePatternsDecodedSignature(method.Signature);
    }

    internal static bool IsDateTimeFormatInfoAllDateTimePatternsSignature(BlobReader blob)
    {
        int count = blob.RemainingBytes - 4;
        return count is 0 or 1 && blob.ReadByte() == 0x20 && blob.ReadByte() == count
            && blob.ReadByte() == 0x1D && blob.ReadByte() == 0x0E
            && (count == 0 || blob.ReadByte() == 0x03);
    }

    internal static bool IsDateTimeFormatInfoAllDateTimePatternsDecodedSignature(MethodSignature<TypeDesc> signature) =>
        signature.Header.RawValue == 0x20 && signature.GenericParameterCount == 0
        && signature.ReturnType is { Kind: TypeKind.SZArray,
            Element: { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.String } }
        && (signature.ParameterTypes.Length == 0
            || signature.ParameterTypes is [{ Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Char }]);

    internal static bool IsDateTimeFormatInfoPatternCombinator(MethodInfo method)
    {
        if (method.Name is not ("GetCombinedPatterns" or "GetMergedPatterns")
            || !IsDateTimeFormatInfoMethodOwner(method,
                MethodAttributes.Private | MethodAttributes.Static | MethodAttributes.HideBySig))
            return false;
        bool combined = method.Name == "GetCombinedPatterns";
        return IsDateTimeFormatInfoPatternCombinatorSignature(method.Module.Reader.GetBlobReader(
                method.Module.Reader.GetMethodDefinition(method.Handle).Signature), combined)
            && IsDateTimeFormatInfoPatternCombinatorDecodedSignature(method.Signature, combined);
    }

    internal static bool IsDateTimeFormatInfoPatternCombinatorSignature(BlobReader blob, bool combined) =>
        blob.RemainingBytes == (combined ? 9 : 7) && blob.ReadByte() == 0
        && blob.ReadByte() == (combined ? 3 : 2)
        && blob.ReadByte() == 0x1D && blob.ReadByte() == 0x0E
        && blob.ReadByte() == 0x1D && blob.ReadByte() == 0x0E
        && (!combined || (blob.ReadByte() == 0x1D && blob.ReadByte() == 0x0E))
        && blob.ReadByte() == 0x0E;

    internal static bool IsDateTimeFormatInfoPatternCombinatorDecodedSignature(MethodSignature<TypeDesc> signature, bool combined) =>
        signature.Header.RawValue == 0 && signature.GenericParameterCount == 0
        && signature.ReturnType is { Kind: TypeKind.SZArray,
            Element: { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.String } }
        && signature.ParameterTypes.Length == (combined ? 3 : 2)
        && signature.ParameterTypes[0] is { Kind: TypeKind.SZArray,
            Element: { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.String } }
        && (!combined || signature.ParameterTypes[1] is { Kind: TypeKind.SZArray,
            Element: { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.String } })
        && signature.ParameterTypes[^1] is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.String };

    internal static bool IsDateTimeFormatInfoAllPatternGetter(MethodInfo method) =>
        method.Name is "get_AllYearMonthPatterns" or "get_AllShortDatePatterns" or "get_AllLongDatePatterns"
            or "get_AllShortTimePatterns" or "get_AllLongTimePatterns"
        && IsDateTimeFormatInfoPatternArrayGetter(method);

    internal static bool IsDateTimeFormatInfoUnclonedPatternGetter(MethodInfo method) =>
        method.Name is "get_UnclonedYearMonthPatterns" or "get_UnclonedShortDatePatterns" or "get_UnclonedLongDatePatterns"
            or "get_UnclonedShortTimePatterns" or "get_UnclonedLongTimePatterns"
        && IsDateTimeFormatInfoPatternArrayGetter(method);

    private static bool IsDateTimeFormatInfoPatternArrayGetter(MethodInfo method) =>
        IsDateTimeFormatInfoMethodOwner(method,
            MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.SpecialName)
        && IsDateTimeFormatInfoMonthArraySignature(method.Module.Reader.GetBlobReader(
            method.Module.Reader.GetMethodDefinition(method.Handle).Signature), hasBoolean: false)
        && IsDateTimeFormatInfoMonthArrayDecodedSignature(method.Signature, hasBoolean: false);
}
