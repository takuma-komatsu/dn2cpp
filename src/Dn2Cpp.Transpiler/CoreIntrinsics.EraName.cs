using System.Reflection;
using System.Reflection.Metadata;

namespace Dn2Cpp;

internal static partial class CoreIntrinsics
{
    internal static bool IsDateTimeFormatInfoEraName(MethodInfo method)
    {
        if (!(method.Name is "GetEraName" or "GetAbbreviatedEraName")
            || !IsDateTimeFormatInfoMethodOwner(method, MethodAttributes.Public | MethodAttributes.HideBySig))
            return false;
        var reader = method.Module.Reader;
        var definition = reader.GetMethodDefinition(method.Handle);
        return IsDateTimeFormatInfoEraNameSignature(reader.GetBlobReader(definition.Signature))
            && IsDateTimeFormatInfoEraNameDecodedSignature(method.Signature, method.Module);
    }

    internal static bool IsDateTimeFormatInfoEraNameSignature(BlobReader blob) =>
        blob.RemainingBytes == 4 && blob.ReadByte() == 0x20 && blob.ReadByte() == 1
        && blob.ReadByte() == (byte)SignatureTypeCode.String
        && blob.ReadByte() == (byte)SignatureTypeCode.Int32;

    internal static bool IsDateTimeFormatInfoEraNameDecodedSignature(MethodSignature<TypeDesc> signature, Module module) =>
        signature.Header.RawValue == 0x20 && signature.GenericParameterCount == 0
        && signature.ReturnType is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.String }
        && signature.ParameterTypes is [{ Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int32 }];
}
