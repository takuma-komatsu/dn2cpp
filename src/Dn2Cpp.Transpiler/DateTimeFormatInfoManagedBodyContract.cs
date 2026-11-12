using System.Reflection;
using System.Reflection.Metadata;
using SRME = System.Reflection.Metadata.Ecma335.MetadataTokens;

namespace Dn2Cpp;

// Shared with ILDiet so copied CoreLib traversal follows the same field-free bodies.
internal static class DateTimeFormatInfoManagedBodyContract
{
    internal static bool IsName(string owner, string name) => owner == "System.Globalization.DateTimeFormatInfo"
        && name is "GetAbbreviatedDayName" or "GetDayName" or "GetShortestDayName"
            or "get_AbbreviatedDayNames" or "get_DayNames" or "get_ShortestDayNames"
            or "GetAbbreviatedMonthName" or "GetMonthName" or "InternalGetMonthName"
            or "get_AbbreviatedMonthNames" or "get_MonthNames" or "get_AbbreviatedMonthGenitiveNames" or "get_MonthGenitiveNames"
            or "GetAllDateTimePatterns" or "GetCombinedPatterns" or "GetMergedPatterns"
            or "get_AllYearMonthPatterns" or "get_AllShortDatePatterns" or "get_AllLongDatePatterns"
            or "get_AllShortTimePatterns" or "get_AllLongTimePatterns"
            or "get_HasSpacesInMonthNames" or "get_HasSpacesInDayNames" or "get_HasYearMonthAdjustment"
            or "YearMonthAdjustment";

    private static bool Byte(ref BlobReader blob, byte value) =>
        blob.RemainingBytes > 0 && blob.ReadByte() == value;

    private static bool Header(ref BlobReader blob, bool instance, int count) =>
        Byte(ref blob, instance ? (byte)0x20 : (byte)0) && Byte(ref blob, (byte)count);

    private static bool StringArray(ref BlobReader blob) => Byte(ref blob, 0x1D) && Byte(ref blob, 0x0E);

    private static bool Int32Ref(ref BlobReader blob) => Byte(ref blob, 0x10) && Byte(ref blob, 0x08);

    private static bool NamedEnum(ref BlobReader blob, MetadataReader reader, string ns, string name)
    {
        if (!Byte(ref blob, 0x11) || !blob.TryReadCompressedInteger(out int token)
            || (token & 3) != 0 || (token >> 2) == 0 || (token >> 2) > reader.TypeDefinitions.Count)
            return false;
        var type = reader.GetTypeDefinition(SRME.TypeDefinitionHandle(token >> 2));
        if (!type.GetDeclaringType().IsNil || type.GetGenericParameters().Count != 0
            || !reader.StringComparer.Equals(type.Namespace, ns) || !reader.StringComparer.Equals(type.Name, name)
            || (type.Attributes & TypeAttributes.Interface) != 0 || type.BaseType.Kind != HandleKind.TypeDefinition)
            return false;
        var baseType = reader.GetTypeDefinition((TypeDefinitionHandle)type.BaseType);
        if (!baseType.GetDeclaringType().IsNil || !reader.StringComparer.Equals(baseType.Namespace, "System")
            || !reader.StringComparer.Equals(baseType.Name, "Enum"))
            return false;
        int instanceFields = 0;
        foreach (var handle in type.GetFields())
        {
            var field = reader.GetFieldDefinition(handle);
            if ((field.Attributes & FieldAttributes.Static) != 0)
                continue;
            var signature = reader.GetBlobReader(field.Signature);
            if (++instanceFields != 1 || !reader.StringComparer.Equals(field.Name, "value__")
                || signature.RemainingBytes != 2 || signature.ReadByte() != 0x06 || signature.ReadByte() != 0x08)
                return false;
        }
        return instanceFields == 1;
    }

    private static bool Signature(MetadataReader reader, string name, BlobReader blob, out MethodAttributes attributes)
    {
        attributes = MethodAttributes.Public | MethodAttributes.HideBySig;
        bool matched;
        switch (name)
        {
            case "GetAbbreviatedDayName" or "GetDayName" or "GetShortestDayName":
                matched = Header(ref blob, true, 1) && Byte(ref blob, 0x0E)
                    && NamedEnum(ref blob, reader, "System", "DayOfWeek");
                break;
            case "GetAbbreviatedMonthName" or "GetMonthName":
                matched = Header(ref blob, true, 1) && Byte(ref blob, 0x0E) && Byte(ref blob, 0x08);
                break;
            case "InternalGetMonthName":
                attributes = MethodAttributes.Assembly | MethodAttributes.HideBySig;
                matched = Header(ref blob, true, 3) && Byte(ref blob, 0x0E) && Byte(ref blob, 0x08)
                    && NamedEnum(ref blob, reader, "System.Globalization", "MonthNameStyles") && Byte(ref blob, 0x02);
                break;
            case "GetAllDateTimePatterns":
            {
                int count = blob.RemainingBytes - 4;
                matched = count is 0 or 1 && Header(ref blob, true, count) && StringArray(ref blob)
                    && (count == 0 || Byte(ref blob, 0x03));
                break;
            }
            case "GetCombinedPatterns" or "GetMergedPatterns":
            {
                attributes = MethodAttributes.Private | MethodAttributes.Static | MethodAttributes.HideBySig;
                bool combined = name == "GetCombinedPatterns";
                matched = Header(ref blob, false, combined ? 3 : 2) && StringArray(ref blob) && StringArray(ref blob)
                    && (!combined || StringArray(ref blob)) && Byte(ref blob, 0x0E);
                break;
            }
            case "get_HasSpacesInMonthNames" or "get_HasSpacesInDayNames" or "get_HasYearMonthAdjustment":
                attributes = MethodAttributes.Assembly | MethodAttributes.HideBySig | MethodAttributes.SpecialName;
                matched = Header(ref blob, true, 0) && Byte(ref blob, 0x02);
                break;
            case "YearMonthAdjustment":
                attributes = MethodAttributes.Assembly | MethodAttributes.HideBySig;
                matched = Header(ref blob, true, 3) && Byte(ref blob, 0x02)
                    && Int32Ref(ref blob) && Int32Ref(ref blob) && Byte(ref blob, 0x02);
                break;
            default:
                bool privateGetter = name is "get_AllYearMonthPatterns" or "get_AllShortDatePatterns" or "get_AllLongDatePatterns"
                    or "get_AllShortTimePatterns" or "get_AllLongTimePatterns";
                attributes = (privateGetter ? MethodAttributes.Private : MethodAttributes.Public)
                    | MethodAttributes.HideBySig | MethodAttributes.SpecialName;
                matched = Header(ref blob, true, 0) && StringArray(ref blob);
                break;
        }
        return matched && blob.RemainingBytes == 0;
    }

    internal static bool IsDefinition(MetadataReader reader, MethodDefinitionHandle handle, string owner, string name)
    {
        if (!IsName(owner, name) || handle.IsNil || !reader.IsAssembly
            || !reader.StringComparer.Equals(reader.GetAssemblyDefinition().Name, "System.Private.CoreLib"))
            return false;
        var method = reader.GetMethodDefinition(handle);
        var type = reader.GetTypeDefinition(method.GetDeclaringType());
        return reader.StringComparer.Equals(type.Namespace, "System.Globalization")
            && reader.StringComparer.Equals(type.Name, "DateTimeFormatInfo")
            && type.GetDeclaringType().IsNil && type.GetGenericParameters().Count == 0
            && (type.Attributes & (TypeAttributes.Interface | TypeAttributes.Abstract | TypeAttributes.Sealed)) == TypeAttributes.Sealed
            && reader.StringComparer.Equals(method.Name, name) && method.GetGenericParameters().Count == 0
            && method.RelativeVirtualAddress != 0 && method.ImplAttributes == MethodImplAttributes.IL
            && Signature(reader, name, reader.GetBlobReader(method.Signature), out var attributes)
            && method.Attributes == attributes;
    }
}
