using System.Reflection;
using System.Reflection.Metadata;

namespace Dn2Cpp;

internal static partial class CoreIntrinsics
{
    internal static bool IsDateTimeFormatInfoDayArrayGetter(MethodInfo method) =>
        method.Name is "get_AbbreviatedDayNames" or "get_DayNames" or "get_ShortestDayNames"
        && IsDateTimeFormatInfoDayArrayMethod(method,
            MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName);

    internal static bool IsDateTimeFormatInfoDayArray(MethodInfo method) =>
        method.Name is "InternalGetAbbreviatedDayOfWeekNames" or "InternalGetDayOfWeekNames" or "InternalGetSuperShortDayNames"
        && IsDateTimeFormatInfoDayArrayMethod(method, MethodAttributes.Private | MethodAttributes.HideBySig);

    private static bool IsDateTimeFormatInfoDayArrayMethod(MethodInfo method, MethodAttributes attributes) =>
        IsDateTimeFormatInfoMethodOwner(method, attributes)
        && IsDateTimeFormatInfoMonthArraySignature(method.Module.Reader.GetBlobReader(
            method.Module.Reader.GetMethodDefinition(method.Handle).Signature), hasBoolean: false)
        && IsDateTimeFormatInfoMonthArrayDecodedSignature(method.Signature, hasBoolean: false);

    internal static bool IsDateTimeFormatInfoDayName(MethodInfo method)
    {
        if (!(method.Name is "GetAbbreviatedDayName" or "GetDayName" or "GetShortestDayName")
            || !IsDateTimeFormatInfoMethodOwner(method, MethodAttributes.Public | MethodAttributes.HideBySig))
            return false;
        var reader = method.Module.Reader;
        var definition = reader.GetMethodDefinition(method.Handle);
        return IsDateTimeFormatInfoDayNameSignature(reader.GetBlobReader(definition.Signature))
            && IsDateTimeFormatInfoDayNameDecodedSignature(method.Signature, method.Module);
    }

    internal static bool IsDateTimeFormatInfoDayNameSignature(BlobReader blob) =>
        blob.RemainingBytes >= 5 && blob.ReadByte() == 0x20 && blob.ReadByte() == 1
        && blob.ReadByte() == (byte)SignatureTypeCode.String && blob.ReadByte() == 0x11
        && blob.TryReadCompressedInteger(out int token) && token > 0 && (token >> 2) != 0
        && (token & 3) != 3 && blob.RemainingBytes == 0;

    internal static bool IsDateTimeFormatInfoDayNameDecodedSignature(MethodSignature<TypeDesc> signature, Module module)
    {
        if (signature.Header.RawValue != 0x20 || signature.GenericParameterCount != 0
            || signature.ReturnType is not { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.String }
            || signature.ParameterTypes is not [{ Kind: TypeKind.Class, Class: { FullName: "System.DayOfWeek",
                IsEnum: true, EnumUnderlying: PrimitiveTypeCode.Int32 } day }]
            || day.Module != module || day.Handle.IsNil || day.GenericArity != 0 || day.Context.TypeArgs.Length != 0)
            return false;
        var definition = module.Reader.GetTypeDefinition(day.Handle);
        return definition.GetDeclaringType().IsNil && definition.GetGenericParameters().Count == 0
            && module.Reader.StringComparer.Equals(definition.Namespace, "System")
            && module.Reader.StringComparer.Equals(definition.Name, "DayOfWeek");
    }
}
