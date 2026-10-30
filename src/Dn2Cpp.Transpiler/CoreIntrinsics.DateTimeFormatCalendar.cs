using System.Reflection;
using System.Reflection.Metadata;

namespace Dn2Cpp;

internal static partial class CoreIntrinsics
{
    internal static bool IsDateTimeFormatInfoCalendar(MethodInfo method)
    {
        var cls = method.DeclaringClass;
        if (method.Name != "get_Calendar" || cls.FullName != "System.Globalization.DateTimeFormatInfo"
            || method.Module.AssemblyName != "System.Private.CoreLib" || cls.Module != method.Module
            || cls.Handle.IsNil || method.Handle.IsNil || cls.IsValueType || cls.IsInterface
            || cls.GenericArity != 0 || cls.Context.TypeArgs.Length != 0
            || method.Attributes != (MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName)
            || method.ImplAttributes != MethodImplAttributes.IL || method.Rva == 0 || method.PInvoke is not null)
            return false;
        var reader = method.Module.Reader;
        var owner = reader.GetTypeDefinition(cls.Handle);
        return owner.GetDeclaringType().IsNil && owner.GetGenericParameters().Count == 0
            && reader.StringComparer.Equals(owner.Namespace, "System.Globalization")
            && reader.StringComparer.Equals(owner.Name, "DateTimeFormatInfo")
            && IsDateTimeFormatInfoCalendarSignature(reader.GetBlobReader(reader.GetMethodDefinition(method.Handle).Signature))
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
