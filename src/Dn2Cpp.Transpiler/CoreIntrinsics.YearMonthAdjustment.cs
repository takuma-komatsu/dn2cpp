using System.Reflection;
using System.Reflection.Metadata;

namespace Dn2Cpp;

internal static partial class CoreIntrinsics
{
    internal static bool IsDateTimeFormatInfoHasYearMonthAdjustment(MethodInfo method) =>
        IsDateTimeFormatInfoAdjustmentOwner(method, getter: true)
        && IsDateTimeFormatInfoHasYearMonthAdjustmentSignature(method.Module.Reader.GetBlobReader(
            method.Module.Reader.GetMethodDefinition(method.Handle).Signature))
        && IsDateTimeFormatInfoHasYearMonthAdjustmentDecodedSignature(method.Signature);

    internal static bool IsDateTimeFormatInfoYearMonthAdjustment(MethodInfo method) =>
        IsDateTimeFormatInfoAdjustmentOwner(method, getter: false)
        && IsDateTimeFormatInfoYearMonthAdjustmentSignature(method.Module.Reader.GetBlobReader(
            method.Module.Reader.GetMethodDefinition(method.Handle).Signature))
        && IsDateTimeFormatInfoYearMonthAdjustmentDecodedSignature(method.Signature);

    private static bool IsDateTimeFormatInfoAdjustmentOwner(MethodInfo method, bool getter)
    {
        var cls = method.DeclaringClass;
        if (method.Name != (getter ? "get_HasYearMonthAdjustment" : "YearMonthAdjustment") || cls.FullName != "System.Globalization.DateTimeFormatInfo"
            || method.Module.AssemblyName != "System.Private.CoreLib" || cls.Module != method.Module
            || cls.Handle.IsNil || method.Handle.IsNil || cls.IsValueType || cls.IsInterface
            || cls.GenericArity != 0 || cls.Context.TypeArgs.Length != 0
            || method.Attributes != (MethodAttributes.Assembly | MethodAttributes.HideBySig | (getter ? MethodAttributes.SpecialName : 0))
            || method.ImplAttributes != MethodImplAttributes.IL || method.Rva == 0 || method.PInvoke is not null)
            return false;
        var reader = method.Module.Reader;
        var owner = reader.GetTypeDefinition(cls.Handle);
        return owner.GetDeclaringType().IsNil && owner.GetGenericParameters().Count == 0
            && reader.StringComparer.Equals(owner.Namespace, "System.Globalization")
            && reader.StringComparer.Equals(owner.Name, "DateTimeFormatInfo");
    }

    internal static bool IsDateTimeFormatInfoHasYearMonthAdjustmentSignature(BlobReader blob) =>
        blob.RemainingBytes == 3 && blob.ReadByte() == 0x20 && blob.ReadByte() == 0
        && blob.ReadByte() == 0x02;

    internal static bool IsDateTimeFormatInfoHasYearMonthAdjustmentDecodedSignature(MethodSignature<TypeDesc> signature) =>
        signature.Header.RawValue == 0x20 && signature.GenericParameterCount == 0
        && signature.ParameterTypes.Length == 0
        && signature.ReturnType is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Boolean };
    internal static bool IsDateTimeFormatInfoYearMonthAdjustmentSignature(BlobReader blob) =>
        blob.RemainingBytes == 8 && blob.ReadByte() == 0x20 && blob.ReadByte() == 3
        && blob.ReadByte() == 0x02 && blob.ReadByte() == 0x10 && blob.ReadByte() == 0x08
        && blob.ReadByte() == 0x10 && blob.ReadByte() == 0x08 && blob.ReadByte() == 0x02;

    internal static bool IsDateTimeFormatInfoYearMonthAdjustmentDecodedSignature(MethodSignature<TypeDesc> signature) =>
        signature.Header.RawValue == 0x20 && signature.GenericParameterCount == 0
        && signature.ReturnType is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Boolean }
        && signature.ParameterTypes is [
            { Kind: TypeKind.ByRef, Element: { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int32 } },
            { Kind: TypeKind.ByRef, Element: { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int32 } },
            { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Boolean }];
}
