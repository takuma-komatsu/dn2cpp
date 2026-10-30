using System.Reflection;
using System.Reflection.Metadata;

namespace Dn2Cpp;

internal static partial class CoreIntrinsics
{
    internal static bool IsDateOnlyDeconstruct(MethodInfo method)
    {
        var cls = method.DeclaringClass;
        if (method.Name != "Deconstruct" || cls.FullName != "System.DateOnly"
            || method.Module.AssemblyName != "System.Private.CoreLib" || cls.Module != method.Module
            || cls.Handle.IsNil || method.Handle.IsNil || !cls.IsValueType || cls.IsEnum || cls.IsInterface
            || cls.GenericArity != 0 || cls.Context.TypeArgs.Length != 0
            || method.Attributes != (MethodAttributes.Public | MethodAttributes.HideBySig)
            || method.ImplAttributes != MethodImplAttributes.IL || method.Rva == 0 || method.PInvoke is not null)
            return false;
        var reader = method.Module.Reader;
        var owner = reader.GetTypeDefinition(cls.Handle);
        var definition = reader.GetMethodDefinition(method.Handle);
        return owner.GetDeclaringType().IsNil && owner.GetGenericParameters().Count == 0
            && definition.GetGenericParameters().Count == 0
            && reader.StringComparer.Equals(owner.Namespace, "System")
            && reader.StringComparer.Equals(owner.Name, "DateOnly")
            && IsDateOnlyDeconstructSignature(reader.GetBlobReader(definition.Signature))
            && IsDateOnlyDeconstructDecodedSignature(method.Signature);
    }

    internal static bool IsDateOnlyDeconstructSignature(BlobReader blob) =>
        blob.RemainingBytes == 9 && blob.ReadByte() == 0x20 && blob.ReadByte() == 3
        && blob.ReadByte() == (byte)SignatureTypeCode.Void
        && blob.ReadByte() == (byte)SignatureTypeCode.ByReference && blob.ReadByte() == (byte)SignatureTypeCode.Int32
        && blob.ReadByte() == (byte)SignatureTypeCode.ByReference && blob.ReadByte() == (byte)SignatureTypeCode.Int32
        && blob.ReadByte() == (byte)SignatureTypeCode.ByReference && blob.ReadByte() == (byte)SignatureTypeCode.Int32;

    internal static bool IsDateOnlyDeconstructDecodedSignature(MethodSignature<TypeDesc> signature) =>
        signature.Header.RawValue == 0x20 && signature.GenericParameterCount == 0
        && signature.ReturnType is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Void }
        && signature.ParameterTypes is [
            { Kind: TypeKind.ByRef, Element: { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int32 } },
            { Kind: TypeKind.ByRef, Element: { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int32 } },
            { Kind: TypeKind.ByRef, Element: { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int32 } }];
}
