using System.Reflection;
using System.Reflection.Metadata;

namespace Dn2Cpp;

internal static partial class CoreIntrinsics
{
    internal static bool IsDateTimeFormatInfoGeneralTimePattern(MethodInfo method)
    {
        var cls = method.DeclaringClass;
        if (method.Name is not ("get_GeneralShortTimePattern" or "get_GeneralLongTimePattern" or "get_DateTimeOffsetPattern") || cls.FullName != "System.Globalization.DateTimeFormatInfo"
            || method.Module.AssemblyName != "System.Private.CoreLib" || cls.Module != method.Module
            || cls.Handle.IsNil || method.Handle.IsNil || cls.IsValueType || cls.IsInterface
            || cls.GenericArity != 0 || cls.Context.TypeArgs.Length != 0
            || method.Attributes != (MethodAttributes.Assembly | MethodAttributes.HideBySig | MethodAttributes.SpecialName)
            || method.ImplAttributes != MethodImplAttributes.IL || method.Rva == 0 || method.PInvoke is not null)
            return false;
        var reader = method.Module.Reader;
        var owner = reader.GetTypeDefinition(cls.Handle);
        return owner.GetDeclaringType().IsNil && owner.GetGenericParameters().Count == 0
            && reader.StringComparer.Equals(owner.Namespace, "System.Globalization")
            && reader.StringComparer.Equals(owner.Name, "DateTimeFormatInfo")
            && IsDateTimeFormatInfoGeneralTimePatternSignature(reader.GetBlobReader(reader.GetMethodDefinition(method.Handle).Signature))
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
