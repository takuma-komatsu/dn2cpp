using System.Reflection;
using System.Reflection.Metadata;

namespace Dn2Cpp;

internal static partial class CoreIntrinsics
{
    internal static bool IsDateTimeFormatInfoParserFlagGetterName(string name) =>
        name is "get_HasSpacesInMonthNames" or "get_HasSpacesInDayNames" or "get_HasYearMonthAdjustment";

    internal static bool IsDateTimeFormatInfoParserFlagGetter(MethodInfo method) =>
        IsDateTimeFormatInfoAdjustmentOwner(method, getter: true)
        && IsDateTimeFormatInfoParserFlagGetterSignature(method.Module.Reader.GetBlobReader(
            method.Module.Reader.GetMethodDefinition(method.Handle).Signature))
        && IsDateTimeFormatInfoParserFlagGetterDecodedSignature(method.Signature);

    internal static bool IsDateTimeFormatInfoYearMonthAdjustment(MethodInfo method) =>
        IsDateTimeFormatInfoAdjustmentOwner(method, getter: false)
        && IsDateTimeFormatInfoYearMonthAdjustmentSignature(method.Module.Reader.GetBlobReader(
            method.Module.Reader.GetMethodDefinition(method.Handle).Signature))
        && IsDateTimeFormatInfoYearMonthAdjustmentDecodedSignature(method.Signature);

    private static bool IsDateTimeFormatInfoAdjustmentOwner(MethodInfo method, bool getter)
    {
        return (getter ? IsDateTimeFormatInfoParserFlagGetterName(method.Name) : method.Name == "YearMonthAdjustment")
            && IsDateTimeFormatInfoMethodOwner(method, MethodAttributes.Assembly | MethodAttributes.HideBySig
                | (getter ? MethodAttributes.SpecialName : 0));
    }

    internal static bool IsDateTimeFormatInfoParserFlagGetterSignature(BlobReader blob) =>
        blob.RemainingBytes == 3 && blob.ReadByte() == 0x20 && blob.ReadByte() == 0
        && blob.ReadByte() == 0x02;

    internal static bool IsDateTimeFormatInfoParserFlagGetterDecodedSignature(MethodSignature<TypeDesc> signature) =>
        signature.Header.RawValue == 0x20 && signature.GenericParameterCount == 0
        && signature.ParameterTypes.Length == 0
        && signature.ReturnType is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Boolean };
    internal static bool IsDateTimeFormatInfoYearMonthAdjustmentSignature(BlobReader blob) =>
        blob.RemainingBytes == 8 && blob.ReadByte() == 0x20 && blob.ReadByte() == 3
        && blob.ReadByte() == 0x02 && blob.ReadByte() == 0x10 && blob.ReadByte() == 0x08
        && blob.ReadByte() == 0x10 && blob.ReadByte() == 0x08 && blob.ReadByte() == 0x02;

    internal static bool IsDateTimeFormatInfoYearMonthAdjustmentDecodedSignature(MethodSignature<TypeDesc> signature) =>
        signature.Header.RawValue == 0x20 && signature.GenericParameterCount == 0
        && signature.ReturnType is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Boolean }
        && signature.ParameterTypes is [
            { Kind: TypeKind.ByRef, Element: { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int32 } },
            { Kind: TypeKind.ByRef, Element: { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int32 } },
            { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Boolean }];
}
