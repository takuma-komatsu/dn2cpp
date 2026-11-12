using System.Reflection;
using System.Reflection.Metadata;

namespace Dn2Cpp;

internal static partial class CoreIntrinsics
{
    // Keep the algorithms above the frozen DTFI storage boundary in CoreLib IL.
    internal static bool IsDateTimeFormatInfoManagedBody(MethodInfo method) =>
        DateTimeFormatInfoManagedBodyContract.IsName(method.DeclaringClass.FullName, method.Name)
        && !method.Handle.IsNil
        && DateTimeFormatInfoManagedBodyContract.IsDefinition(method.Module.Reader, method.Handle,
            method.DeclaringClass.FullName, method.Name)
        && (IsDateTimeFormatInfoDayName(method)
        || IsDateTimeFormatInfoDayArrayGetter(method)
        || IsDateTimeFormatInfoMonthName(method)
        || IsDateTimeFormatInfoMonthArrayGetter(method)
        || IsDateTimeFormatInfoInternalMonthName(method)
        || IsDateTimeFormatInfoAllDateTimePatterns(method)
        || IsDateTimeFormatInfoPatternCombinator(method)
        || IsDateTimeFormatInfoAllPatternGetter(method)
        || IsDateTimeFormatInfoParserFlagGetter(method)
        || IsDateTimeFormatInfoYearMonthAdjustment(method));

    internal static bool IsDateTimeFormatInfoBoundary(MethodInfo method) =>
        IsDateTimeFormatInfoDayArray(method)
        || IsDateTimeFormatInfoMonthArray(method)
        || IsDateTimeFormatInfoUnclonedPatternGetter(method)
        || IsDateTimeFormatInfoEraName(method)
        || IsDateTimeFormatInfoFormatFlags(method)
        || IsDateTimeFormatInfoHasForceTwoDigitYears(method)
        || IsDateTimeFormatInfoGeneralTimePattern(method)
        || IsDateTimeFormatInfoSpan(method)
        || IsDateTimeFormatInfoCalendar(method)
        || IsDateTimeFormatInfoCalendarSetter(method)
        || IsDateTimeFormatInfoTokenize(method);

    internal static bool IsDateTimeFormatInfoSupportedName(string name) => name is
        "GetAbbreviatedDayName" or "GetDayName" or "GetShortestDayName"
        or "get_AbbreviatedDayNames" or "get_DayNames" or "get_ShortestDayNames"
        or "InternalGetAbbreviatedDayOfWeekNames" or "InternalGetDayOfWeekNames" or "InternalGetSuperShortDayNames"
        or "GetAbbreviatedMonthName" or "GetMonthName" or "InternalGetMonthName"
        or "get_AbbreviatedMonthNames" or "get_MonthNames" or "get_AbbreviatedMonthGenitiveNames" or "get_MonthGenitiveNames"
        or "InternalGetAbbreviatedMonthNames" or "InternalGetMonthNames"
        or "InternalGetGenitiveMonthNames" or "InternalGetLeapYearMonthNames"
        or "GetAllDateTimePatterns" or "GetCombinedPatterns" or "GetMergedPatterns"
        or "get_AllYearMonthPatterns" or "get_AllShortDatePatterns" or "get_AllLongDatePatterns"
        or "get_AllShortTimePatterns" or "get_AllLongTimePatterns"
        or "get_UnclonedYearMonthPatterns" or "get_UnclonedShortDatePatterns" or "get_UnclonedLongDatePatterns"
        or "get_UnclonedShortTimePatterns" or "get_UnclonedLongTimePatterns"
        or "GetEraName" or "GetAbbreviatedEraName" or "get_FormatFlags" or "get_HasForceTwoDigitYears"
        or "get_GeneralShortTimePattern" or "get_GeneralLongTimePattern" or "get_DateTimeOffsetPattern"
        or "get_Calendar" or "set_Calendar" or "Tokenize" or "YearMonthAdjustment"
        || IsDateTimeFormatInfoSpanName(name) || IsDateTimeFormatInfoParserFlagGetterName(name);

    // A reference must preserve the declaration's raw calling convention and
    // decoded types before the intrinsic-mapped owner's normal fallback runs.
    internal static bool IsDateTimeFormatInfoReference(MethodInfo method,
        MethodSignature<TypeDesc> signature, BlobReader blob)
    {
        if ((!IsDateTimeFormatInfoManagedBody(method) && !IsDateTimeFormatInfoBoundary(method))
            || signature.Header.RawValue != method.Signature.Header.RawValue
            || signature.GenericParameterCount != method.Signature.GenericParameterCount
            || signature.ParameterTypes.Length != method.Signature.ParameterTypes.Length
            || signature.RequiredParameterCount != signature.ParameterTypes.Length)
            return false;
        string name = method.Name;
        if (name is "GetAbbreviatedDayName" or "GetDayName" or "GetShortestDayName")
            return IsDateTimeFormatInfoDayNameSignature(blob)
                && IsDateTimeFormatInfoDayNameDecodedSignature(signature, method.Module);
        if (name is "GetAbbreviatedMonthName" or "GetMonthName")
            return IsDateTimeFormatInfoMonthNameSignature(blob)
                && IsDateTimeFormatInfoMonthNameDecodedSignature(signature, method.Module);
        if (name is "GetEraName" or "GetAbbreviatedEraName")
            return IsDateTimeFormatInfoEraNameSignature(blob)
                && IsDateTimeFormatInfoEraNameDecodedSignature(signature, method.Module);
        if (name == "InternalGetMonthName")
            return IsDateTimeFormatInfoInternalMonthNameSignature(blob)
                && IsDateTimeFormatInfoInternalMonthNameDecodedSignature(signature, method.Module);
        if (name == "GetAllDateTimePatterns")
            return IsDateTimeFormatInfoAllDateTimePatternsSignature(blob)
                && IsDateTimeFormatInfoAllDateTimePatternsDecodedSignature(signature);
        if (name is "GetCombinedPatterns" or "GetMergedPatterns")
            return IsDateTimeFormatInfoPatternCombinatorSignature(blob, name == "GetCombinedPatterns")
                && IsDateTimeFormatInfoPatternCombinatorDecodedSignature(signature, name == "GetCombinedPatterns");
        if (name == "get_FormatFlags")
            return IsDateTimeFormatInfoFormatFlagsSignature(blob)
                && IsDateTimeFormatInfoFormatFlagsDecodedSignature(signature, method.Module);
        if (name == "YearMonthAdjustment")
            return IsDateTimeFormatInfoYearMonthAdjustmentSignature(blob)
                && IsDateTimeFormatInfoYearMonthAdjustmentDecodedSignature(signature);
        if (name == "get_HasForceTwoDigitYears" || IsDateTimeFormatInfoParserFlagGetterName(name))
            return IsDateTimeFormatInfoParserFlagGetterSignature(blob)
                && IsDateTimeFormatInfoParserFlagGetterDecodedSignature(signature);
        if (name is "get_GeneralShortTimePattern" or "get_GeneralLongTimePattern" or "get_DateTimeOffsetPattern")
            return IsDateTimeFormatInfoGeneralTimePatternSignature(blob)
                && IsDateTimeFormatInfoGeneralTimePatternDecodedSignature(signature);
        if (IsDateTimeFormatInfoSpanName(name))
            return IsDateTimeFormatInfoSpanSignature(blob)
                && IsDateTimeFormatInfoSpanDecodedSignature(signature, method.Module);
        if (name == "get_Calendar")
            return IsDateTimeFormatInfoCalendarSignature(blob)
                && IsDateTimeFormatInfoCalendarDecodedSignature(signature, method.Module);
        if (name == "set_Calendar")
            return IsDateTimeFormatInfoCalendarSetterSignature(blob)
                && IsDateTimeFormatInfoCalendarSetterDecodedSignature(signature, method.Module);
        if (name == "Tokenize")
            return IsDateTimeFormatInfoTokenizeSignature(blob)
                && IsDateTimeFormatInfoTokenizeDecodedSignature(signature, method.Module);
        bool hasBoolean = name == "InternalGetGenitiveMonthNames";
        return IsDateTimeFormatInfoMonthArraySignature(blob, hasBoolean)
            && IsDateTimeFormatInfoMonthArrayDecodedSignature(signature, hasBoolean);
    }

    private static bool IsDateTimeFormatInfoMethodOwner(MethodInfo method, MethodAttributes attributes,
        int genericArity = 0)
    {
        var cls = method.DeclaringClass;
        if (cls.FullName != "System.Globalization.DateTimeFormatInfo"
            || method.Module.AssemblyName != "System.Private.CoreLib" || cls.Module != method.Module
            || cls.Handle.IsNil || method.Handle.IsNil || method.IsSynthetic || cls.IsValueType || cls.IsInterface
            || cls.GenericArity != 0 || cls.Context.TypeArgs.Length != 0 || cls.Context.MethodArgs.Length != 0
            || method.Context.TypeArgs.Length != 0
            || (genericArity == 0 && method.Context.MethodArgs.Length != 0)
            || method.Attributes != attributes || method.ImplAttributes != MethodImplAttributes.IL
            || method.Rva == 0 || method.PInvoke is not null)
            return false;
        var reader = method.Module.Reader;
        var owner = reader.GetTypeDefinition(cls.Handle);
        var definition = reader.GetMethodDefinition(method.Handle);
        return owner.GetDeclaringType().IsNil && owner.GetGenericParameters().Count == 0
            && reader.StringComparer.Equals(owner.Namespace, "System.Globalization")
            && reader.StringComparer.Equals(owner.Name, "DateTimeFormatInfo")
            && definition.GetDeclaringType() == cls.Handle
            && reader.StringComparer.Equals(definition.Name, method.Name)
            && definition.GetGenericParameters().Count == genericArity
            && definition.Attributes == method.Attributes && definition.ImplAttributes == method.ImplAttributes
            && definition.RelativeVirtualAddress == method.Rva;
    }
}
