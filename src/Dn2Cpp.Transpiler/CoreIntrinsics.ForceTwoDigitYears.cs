using System.Reflection;
using System.Reflection.Metadata;

namespace Dn2Cpp;

internal static partial class CoreIntrinsics
{
    internal static bool IsDateTimeFormatInfoHasForceTwoDigitYears(MethodInfo method)
    {
        if (!(method.Name == "get_HasForceTwoDigitYears")
            || !IsDateTimeFormatInfoMethodOwner(method, MethodAttributes.Assembly | MethodAttributes.HideBySig | MethodAttributes.SpecialName))
            return false;
        var reader = method.Module.Reader;
        var definition = reader.GetMethodDefinition(method.Handle);
        return IsDateTimeFormatInfoHasForceTwoDigitYearsSignature(reader.GetBlobReader(definition.Signature))
            && IsDateTimeFormatInfoHasForceTwoDigitYearsDecodedSignature(method.Signature);
    }

    internal static bool IsDateTimeFormatInfoHasForceTwoDigitYearsSignature(BlobReader blob) =>
        blob.RemainingBytes == 3 && blob.ReadByte() == 0x20 && blob.ReadByte() == 0
        && blob.ReadByte() == 0x02;

    internal static bool IsDateTimeFormatInfoHasForceTwoDigitYearsDecodedSignature(MethodSignature<TypeDesc> signature) =>
        signature.Header.RawValue == 0x20 && signature.GenericParameterCount == 0
        && signature.ParameterTypes.Length == 0
        && signature.ReturnType is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Boolean };
}
