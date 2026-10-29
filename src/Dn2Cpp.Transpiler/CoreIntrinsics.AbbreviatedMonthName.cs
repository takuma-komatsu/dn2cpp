using System.Reflection;
using System.Reflection.Metadata;

namespace Dn2Cpp;

internal static partial class CoreIntrinsics
{
    internal static bool IsDateTimeFormatInfoAbbreviatedMonthName(MethodInfo method)
    {
        var cls = method.DeclaringClass;
        if (method.Name != "GetAbbreviatedMonthName" || cls.FullName != "System.Globalization.DateTimeFormatInfo"
            || method.Module.AssemblyName != "System.Private.CoreLib" || cls.Module != method.Module
            || cls.Handle.IsNil || method.Handle.IsNil || cls.IsValueType || cls.IsInterface
            || cls.GenericArity != 0 || cls.Context.TypeArgs.Length != 0
            || method.Attributes != (MethodAttributes.Public | MethodAttributes.HideBySig)
            || method.ImplAttributes != MethodImplAttributes.IL || method.Rva == 0 || method.PInvoke is not null)
            return false;
        var reader = method.Module.Reader;
        var owner = reader.GetTypeDefinition(cls.Handle);
        return owner.GetDeclaringType().IsNil && owner.GetGenericParameters().Count == 0
            && reader.StringComparer.Equals(owner.Namespace, "System.Globalization")
            && reader.StringComparer.Equals(owner.Name, "DateTimeFormatInfo")
            && IsDateTimeFormatInfoAbbreviatedMonthNameSignature(reader.GetBlobReader(reader.GetMethodDefinition(method.Handle).Signature))
            && IsDateTimeFormatInfoAbbreviatedMonthNameDecodedSignature(method.Signature, method.Module);
    }

    internal static bool IsDateTimeFormatInfoAbbreviatedMonthNameSignature(BlobReader blob) =>
        blob.RemainingBytes == 4 && blob.ReadByte() == 0x20 && blob.ReadByte() == 1
        && blob.ReadByte() == (byte)SignatureTypeCode.String
        && blob.ReadByte() == (byte)SignatureTypeCode.Int32;

    internal static bool IsDateTimeFormatInfoAbbreviatedMonthNameDecodedSignature(MethodSignature<TypeDesc> signature, Module module) =>
        signature.Header.RawValue == 0x20 && signature.GenericParameterCount == 0
        && signature.ReturnType is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.String }
        && signature.ParameterTypes is [{ Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int32 }];
}
