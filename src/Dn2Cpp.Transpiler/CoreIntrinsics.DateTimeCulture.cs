namespace Dn2Cpp;

internal static partial class CoreIntrinsics
{
    internal static bool IsCultureInfoBaseConstructor(MethodInfo method)
    {
        if (method.Name != ".ctor" || method.DeclaringClass.FullName != "System.Globalization.CultureInfo"
            || method.Module.AssemblyName != "System.Private.CoreLib" || method.DeclaringClass.Module != method.Module
            || method.Handle.IsNil || method.IsSynthetic || method.PInvoke is not null
            || method.Context.TypeArgs.Length != 0 || method.Context.MethodArgs.Length != 0
            || method.Attributes != (System.Reflection.MethodAttributes.Public | System.Reflection.MethodAttributes.HideBySig
                | System.Reflection.MethodAttributes.SpecialName | System.Reflection.MethodAttributes.RTSpecialName))
            return false;
        var signature = method.Signature;
        var parameters = signature.ParameterTypes;
        return signature.Header.RawValue == 0x20 && signature.GenericParameterCount == 0
            && signature.ReturnType.IsVoid && parameters.Length is 1 or 2
            && (parameters[0].IsString || parameters[0] is { Kind: TypeKind.Primitive,
                Primitive: System.Reflection.Metadata.PrimitiveTypeCode.Int32 })
            && (parameters.Length == 1 || parameters[1] is { Kind: TypeKind.Primitive,
                Primitive: System.Reflection.Metadata.PrimitiveTypeCode.Boolean });
    }

    internal static bool IsDateTimeCultureMemberName(string? owner, string name) =>
        owner == "System.Globalization.CultureInfo" && name == "VerifyCultureName"
        || owner == "System.Globalization.DateTimeFormatInfo" && IsDateTimeFormatInfoSupportedName(name)
        || IsDateTimeLeapSecondSupportName(owner, name)
        || owner is not null && IsDateTimeManagedBodyName(owner, name);

    internal static bool IsDateTimeCultureManagedBody(MethodInfo method) =>
        IsRangeThrowManagedBody(method)
        || IsDateTimeManagedBody(method)
        || IsCultureInfoNameValidator(method)
        || IsDateTimeFormatInfoManagedBody(method);
}
