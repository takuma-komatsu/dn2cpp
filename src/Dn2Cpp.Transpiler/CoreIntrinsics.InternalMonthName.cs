using System.Reflection;
using System.Reflection.Metadata;

namespace Dn2Cpp;

internal static partial class CoreIntrinsics
{
    internal static bool IsDateTimeFormatInfoInternalMonthName(MethodInfo method)
    {
        if (!(method.Name == "InternalGetMonthName")
            || !IsDateTimeFormatInfoMethodOwner(method, MethodAttributes.Assembly | MethodAttributes.HideBySig))
            return false;
        var reader = method.Module.Reader;
        var definition = reader.GetMethodDefinition(method.Handle);
        return IsDateTimeFormatInfoInternalMonthNameSignature(reader.GetBlobReader(definition.Signature))
            && IsDateTimeFormatInfoInternalMonthNameDecodedSignature(method.Signature, method.Module);
    }

    internal static bool IsDateTimeFormatInfoInternalMonthNameSignature(BlobReader blob) =>
        blob.RemainingBytes >= 7 && blob.ReadByte() == 0x20 && blob.ReadByte() == 3
        && blob.ReadByte() == (byte)SignatureTypeCode.String
        && blob.ReadByte() == (byte)SignatureTypeCode.Int32 && blob.ReadByte() == 0x11
        && blob.TryReadCompressedInteger(out int token) && token > 0 && (token >> 2) != 0
        && (token & 3) != 3 && blob.RemainingBytes == 1
        && blob.ReadByte() == (byte)SignatureTypeCode.Boolean;

    internal static bool IsDateTimeFormatInfoInternalMonthNameDecodedSignature(MethodSignature<TypeDesc> signature, Module module)
    {
        if (signature.Header.RawValue != 0x20 || signature.GenericParameterCount != 0
            || signature.ReturnType is not { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.String }
            || signature.ParameterTypes is not [
                { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int32 },
                { Kind: TypeKind.Class, Class: { FullName: "System.Globalization.MonthNameStyles",
                    IsEnum: true, EnumUnderlying: PrimitiveTypeCode.Int32 } style },
                { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Boolean }]
            || style.Module != module || style.Handle.IsNil || style.GenericArity != 0 || style.Context.TypeArgs.Length != 0)
            return false;
        var definition = module.Reader.GetTypeDefinition(style.Handle);
        return definition.GetDeclaringType().IsNil && definition.GetGenericParameters().Count == 0
            && module.Reader.StringComparer.Equals(definition.Namespace, "System.Globalization")
            && module.Reader.StringComparer.Equals(definition.Name, "MonthNameStyles");
    }
}
