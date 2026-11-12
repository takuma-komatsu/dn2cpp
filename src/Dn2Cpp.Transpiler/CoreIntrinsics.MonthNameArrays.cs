using System.Reflection;
using System.Reflection.Metadata;

namespace Dn2Cpp;

internal static partial class CoreIntrinsics
{
    internal static bool IsDateTimeFormatInfoMonthArrayName(string name) => name is
        "InternalGetAbbreviatedMonthNames" or "InternalGetMonthNames"
        or "InternalGetGenitiveMonthNames" or "InternalGetLeapYearMonthNames";

    internal static bool IsDateTimeFormatInfoMonthArrayGetter(MethodInfo method) =>
        method.Name is "get_AbbreviatedMonthNames" or "get_MonthNames"
            or "get_AbbreviatedMonthGenitiveNames" or "get_MonthGenitiveNames"
        && IsDateTimeFormatInfoMonthArrayMethod(method,
            MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName);

    internal static bool IsDateTimeFormatInfoMonthArray(MethodInfo method) =>
        IsDateTimeFormatInfoMonthArrayName(method.Name)
        && IsDateTimeFormatInfoMonthArrayMethod(method, MethodAttributes.HideBySig
            | (method.Name is "InternalGetAbbreviatedMonthNames" or "InternalGetMonthNames"
                ? MethodAttributes.Private : MethodAttributes.Assembly));

    private static bool IsDateTimeFormatInfoMonthArrayMethod(MethodInfo method, MethodAttributes attributes)
    {
        if (!IsDateTimeFormatInfoMethodOwner(method, attributes))
            return false;
        bool hasBoolean = method.Name == "InternalGetGenitiveMonthNames";
        return IsDateTimeFormatInfoMonthArraySignature(method.Module.Reader.GetBlobReader(
                method.Module.Reader.GetMethodDefinition(method.Handle).Signature), hasBoolean)
            && IsDateTimeFormatInfoMonthArrayDecodedSignature(method.Signature, hasBoolean);
    }

    internal static bool IsDateTimeFormatInfoMonthArraySignature(BlobReader blob, bool hasBoolean) =>
        blob.RemainingBytes == (hasBoolean ? 5 : 4) && blob.ReadByte() == 0x20
        && blob.ReadByte() == (hasBoolean ? 1 : 0)
        && blob.ReadByte() == 0x1D && blob.ReadByte() == 0x0E
        && (!hasBoolean || blob.ReadByte() == 0x02);

    internal static bool IsDateTimeFormatInfoMonthArrayDecodedSignature(MethodSignature<TypeDesc> signature, bool hasBoolean) =>
        signature.Header.RawValue == 0x20 && signature.GenericParameterCount == 0
        && signature.ReturnType is { Kind: TypeKind.SZArray,
            Element: { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.String } }
        && (hasBoolean
            ? signature.ParameterTypes is [{ Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Boolean }]
            : signature.ParameterTypes.Length == 0);
}
