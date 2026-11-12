using System.Reflection;
using System.Reflection.Metadata;

namespace Dn2Cpp;

internal static partial class CoreIntrinsics
{
    internal static bool IsDateTimeFormatInfoCalendar(MethodInfo method)
    {
        if (!(method.Name == "get_Calendar")
            || !IsDateTimeFormatInfoMethodOwner(method, MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName))
            return false;
        var reader = method.Module.Reader;
        var definition = reader.GetMethodDefinition(method.Handle);
        return IsDateTimeFormatInfoCalendarSignature(reader.GetBlobReader(definition.Signature))
            && IsDateTimeFormatInfoCalendarDecodedSignature(method.Signature, method.Module);
    }

    internal static bool IsDateTimeFormatInfoCalendarSignature(BlobReader blob) =>
        blob.RemainingBytes >= 4 && blob.ReadByte() == 0x20 && blob.ReadByte() == 0
        && blob.ReadByte() == 0x12 && blob.TryReadCompressedInteger(out int token)
        && token > 0 && (token >> 2) != 0 && (token & 3) != 3 && blob.RemainingBytes == 0;

    internal static bool IsDateTimeFormatInfoCalendarDecodedSignature(MethodSignature<TypeDesc> signature, Module module)
        => signature.Header.RawValue == 0x20 && signature.GenericParameterCount == 0
            && signature.ParameterTypes.Length == 0
            && IsDateTimeFormatInfoCalendarType(signature.ReturnType, module);

    private static bool IsDateTimeFormatInfoCalendarType(TypeDesc type, Module module)
    {
        if (type is not { Kind: TypeKind.Class, Class: { FullName: "System.Globalization.Calendar",
                IsValueType: false, IsEnum: false, IsInterface: false } calendar }
            || calendar.Module != module || calendar.Handle.IsNil || calendar.GenericArity != 0
            || calendar.Context.TypeArgs.Length != 0)
            return false;
        var definition = module.Reader.GetTypeDefinition(calendar.Handle);
        return definition.GetDeclaringType().IsNil && definition.GetGenericParameters().Count == 0
            && module.Reader.StringComparer.Equals(definition.Namespace, "System.Globalization")
            && module.Reader.StringComparer.Equals(definition.Name, "Calendar");
    }
}
