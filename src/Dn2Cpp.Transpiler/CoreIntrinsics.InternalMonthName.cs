using System.Reflection;
using System.Reflection.Metadata;

namespace Dn2Cpp;

internal static partial class CoreIntrinsics
{
    internal static bool IsDateTimeFormatInfoInternalMonthName(MethodInfo method)
    {
        var cls = method.DeclaringClass;
        if (method.Name != "InternalGetMonthName" || cls.FullName != "System.Globalization.DateTimeFormatInfo"
            || method.Module.AssemblyName != "System.Private.CoreLib" || cls.Module != method.Module
            || cls.Handle.IsNil || method.Handle.IsNil || cls.IsValueType || cls.IsInterface
            || cls.GenericArity != 0 || cls.Context.TypeArgs.Length != 0
            || method.Attributes != (MethodAttributes.Assembly | MethodAttributes.HideBySig)
            || method.ImplAttributes != MethodImplAttributes.IL || method.Rva == 0 || method.PInvoke is not null)
            return false;
        var reader = method.Module.Reader;
        var owner = reader.GetTypeDefinition(cls.Handle);
        return owner.GetDeclaringType().IsNil && owner.GetGenericParameters().Count == 0
            && reader.StringComparer.Equals(owner.Namespace, "System.Globalization")
            && reader.StringComparer.Equals(owner.Name, "DateTimeFormatInfo")
            && IsDateTimeFormatInfoInternalMonthNameSignature(reader.GetBlobReader(reader.GetMethodDefinition(method.Handle).Signature))
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
