namespace Dn2Cpp;

internal static partial class CoreIntrinsics
{
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
