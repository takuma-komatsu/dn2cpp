using System.Reflection;
using System.Reflection.Metadata;

namespace Dn2Cpp;

internal static partial class CoreIntrinsics
{
    internal static bool IsDateTimeFormatInfoMonthArrayName(string name) => name is
        "InternalGetGenitiveMonthNames" or "InternalGetLeapYearMonthNames"
        or "get_AbbreviatedMonthNames" or "get_AbbreviatedMonthGenitiveNames";

    internal static bool IsDateTimeFormatInfoMonthArray(MethodInfo method)
    {
        var cls = method.DeclaringClass;
        bool publicGetter = method.Name.StartsWith("get_", StringComparison.Ordinal);
        var attributes = publicGetter
            ? MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName
            : MethodAttributes.Assembly | MethodAttributes.HideBySig;
        if (!IsDateTimeFormatInfoMonthArrayName(method.Name)
            || cls.FullName != "System.Globalization.DateTimeFormatInfo"
            || method.Module.AssemblyName != "System.Private.CoreLib" || cls.Module != method.Module
            || cls.Handle.IsNil || method.Handle.IsNil || cls.IsValueType || cls.IsInterface
            || cls.GenericArity != 0 || cls.Context.TypeArgs.Length != 0
            || method.Attributes != attributes || method.ImplAttributes != MethodImplAttributes.IL
            || method.Rva == 0 || method.PInvoke is not null)
            return false;
        var reader = method.Module.Reader;
        var owner = reader.GetTypeDefinition(cls.Handle);
        bool hasBoolean = method.Name == "InternalGetGenitiveMonthNames";
        return owner.GetDeclaringType().IsNil && owner.GetGenericParameters().Count == 0
            && reader.StringComparer.Equals(owner.Namespace, "System.Globalization")
            && reader.StringComparer.Equals(owner.Name, "DateTimeFormatInfo")
            && IsDateTimeFormatInfoMonthArraySignature(reader.GetBlobReader(reader.GetMethodDefinition(method.Handle).Signature), hasBoolean)
            && IsDateTimeFormatInfoMonthArrayDecodedSignature(method.Signature, hasBoolean);
    }

    internal static bool IsDateTimeFormatInfoMonthArraySignature(BlobReader blob, bool hasBoolean) =>
        blob.RemainingBytes == (hasBoolean ? 5 : 4) && blob.ReadByte() == 0x20
        && blob.ReadByte() == (hasBoolean ? 1 : 0)
        && blob.ReadByte() == 0x1D && blob.ReadByte() == 0x0E
        && (!hasBoolean || blob.ReadByte() == 0x02);

    internal static bool IsDateTimeFormatInfoMonthArrayDecodedSignature(MethodSignature<TypeDesc> signature, bool hasBoolean) =>
        signature.Header.RawValue == 0x20 && signature.GenericParameterCount == 0
        && signature.ReturnType is { Kind: TypeKind.SZArray,
            Element: { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.String } }
        && (hasBoolean
            ? signature.ParameterTypes is [{ Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Boolean }]
            : signature.ParameterTypes.Length == 0);
}
