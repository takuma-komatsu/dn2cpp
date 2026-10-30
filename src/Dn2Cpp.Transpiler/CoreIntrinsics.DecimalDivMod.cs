using System.Reflection;
using System.Reflection.Metadata;

namespace Dn2Cpp;

internal static partial class CoreIntrinsics
{
    internal static bool IsDecimalDivMod(MethodInfo method)
    {
        var cls = method.DeclaringClass;
        if (method.Name != "DecDivMod1E9" || cls.FullName != "System.Decimal"
            || cls.Module.AssemblyName != "System.Private.CoreLib" || method.Module != cls.Module
            || cls.Handle.IsNil || method.Handle.IsNil || !cls.IsValueType || cls.IsEnum || cls.IsInterface
            || cls.GenericArity != 0 || cls.Context.TypeArgs.Length != 0 || method.Context.MethodArgs.Length != 0
            || method.Attributes != (MethodAttributes.Assembly | MethodAttributes.Static | MethodAttributes.HideBySig)
            || method.ImplAttributes != MethodImplAttributes.IL || method.Rva == 0 || method.PInvoke is not null)
            return false;
        var reader = method.Module.Reader;
        var owner = reader.GetTypeDefinition(cls.Handle);
        var definition = reader.GetMethodDefinition(method.Handle);
        return owner.GetDeclaringType().IsNil && owner.GetGenericParameters().Count == 0
            && reader.StringComparer.Equals(owner.Namespace, "System") && reader.StringComparer.Equals(owner.Name, "Decimal")
            && definition.GetDeclaringType() == cls.Handle && definition.GetGenericParameters().Count == 0
            && IsDecimalDivModSignature(reader.GetBlobReader(definition.Signature))
            && IsDecimalDivModDecodedSignature(method.Signature, cls);
    }

    internal static bool IsDecimalDivModSignature(BlobReader blob) =>
        blob.RemainingBytes >= 6 && blob.ReadByte() == 0 && blob.ReadByte() == 1
            && blob.ReadByte() == 9 && blob.ReadByte() == 0x10 && blob.ReadByte() == 0x11
            && blob.TryReadCompressedInteger(out int token) && token > 0 && (token >> 2) != 0
            && (token & 3) <= 1 && blob.RemainingBytes == 0;

    internal static bool IsDecimalDivModDecodedSignature(MethodSignature<TypeDesc> signature, ClassInfo owner) =>
        signature.Header.RawValue == 0 && signature.GenericParameterCount == 0
            && signature.ReturnType is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.UInt32 }
            && signature.ParameterTypes is [{ Kind: TypeKind.ByRef, Element: { Kind: TypeKind.Class, Class: { } element } }]
            && element == owner;
}
