using System.Reflection;
using System.Reflection.Metadata;

namespace Dn2Cpp;

internal static partial class CoreIntrinsics
{
    internal static bool IsDateTimeFormatInfoCalendarSetter(MethodInfo method)
    {
        if (!(method.Name == "set_Calendar")
            || !IsDateTimeFormatInfoMethodOwner(method, MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName))
            return false;
        var reader = method.Module.Reader;
        var definition = reader.GetMethodDefinition(method.Handle);
        return IsDateTimeFormatInfoCalendarSetterSignature(reader.GetBlobReader(definition.Signature))
            && IsDateTimeFormatInfoCalendarSetterDecodedSignature(method.Signature, method.Module);
    }

    internal static bool IsDateTimeFormatInfoCalendarSetterSignature(BlobReader blob) =>
        blob.RemainingBytes >= 5 && blob.ReadByte() == 0x20 && blob.ReadByte() == 1
        && blob.ReadByte() == 0x01 && blob.ReadByte() == 0x12 && blob.TryReadCompressedInteger(out int token)
        && token > 0 && (token >> 2) != 0 && (token & 3) != 3 && blob.RemainingBytes == 0;

    internal static bool IsDateTimeFormatInfoCalendarSetterDecodedSignature(MethodSignature<TypeDesc> signature, Module module) =>
        signature.Header.RawValue == 0x20 && signature.GenericParameterCount == 0
        && signature.ReturnType is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Void }
        && signature.ParameterTypes.Length == 1
        && IsDateTimeFormatInfoCalendarType(signature.ParameterTypes[0], module);
}
