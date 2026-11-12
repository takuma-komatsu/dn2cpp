using System.Reflection;
using System.Reflection.Metadata;

namespace Dn2Cpp;

internal static partial class CoreIntrinsics
{
    internal static bool IsDateTimeFormatInfoGeneralTimePattern(MethodInfo method)
    {
        if (!(method.Name is "get_GeneralShortTimePattern" or "get_GeneralLongTimePattern" or "get_DateTimeOffsetPattern")
            || !IsDateTimeFormatInfoMethodOwner(method, MethodAttributes.Assembly | MethodAttributes.HideBySig | MethodAttributes.SpecialName))
            return false;
        var reader = method.Module.Reader;
        var definition = reader.GetMethodDefinition(method.Handle);
        return IsDateTimeFormatInfoGeneralTimePatternSignature(reader.GetBlobReader(definition.Signature))
            && IsDateTimeFormatInfoGeneralTimePatternDecodedSignature(method.Signature);
    }

    internal static bool IsDateTimeFormatInfoGeneralTimePatternSignature(BlobReader blob) =>
        blob.RemainingBytes == 3 && blob.ReadByte() == 0x20 && blob.ReadByte() == 0
        && blob.ReadByte() == 0x0E;

    internal static bool IsDateTimeFormatInfoGeneralTimePatternDecodedSignature(MethodSignature<TypeDesc> signature) =>
        signature.Header.RawValue == 0x20 && signature.GenericParameterCount == 0
        && signature.ParameterTypes.Length == 0
        && signature.ReturnType is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.String };
}
