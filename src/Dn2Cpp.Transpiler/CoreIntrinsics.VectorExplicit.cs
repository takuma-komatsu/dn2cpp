using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace Dn2Cpp;

internal static partial class CoreIntrinsics
{
    internal static bool IsVectorNumericElement(TypeDesc element) => element is
    {
        Kind: TypeKind.Primitive, IsCanonPlaceholder: false,
        Primitive: PrimitiveTypeCode.Byte or PrimitiveTypeCode.SByte
            or PrimitiveTypeCode.Int16 or PrimitiveTypeCode.UInt16
            or PrimitiveTypeCode.Int32 or PrimitiveTypeCode.UInt32
            or PrimitiveTypeCode.Int64 or PrimitiveTypeCode.UInt64
            or PrimitiveTypeCode.IntPtr or PrimitiveTypeCode.UIntPtr
            or PrimitiveTypeCode.Single or PrimitiveTypeCode.Double,
    };

    internal static bool IsVectorExplicitOwner(ClassInfo owner)
    {
        if (owner.Handle.IsNil || owner.Module is not { AssemblyName: "System.Private.CoreLib" })
            return false;
        var reader = owner.Module.Reader;
        var definition = reader.GetTypeDefinition(owner.Handle);
        return definition.GetDeclaringType().IsNil && definition.GetGenericParameters().Count == 1
            && reader.StringComparer.Equals(definition.Namespace, "System.Numerics")
            && reader.StringComparer.Equals(definition.Name, "Vector`1");
    }

    internal static bool IsVectorExplicit(MethodInfo method)
    {
        var owner = method.DeclaringClass;
        if (method.Name != "op_Explicit" || owner.IntrinsicCppName != "Dn2CppVectorT"
            || owner.Module.AssemblyName != "System.Private.CoreLib" || method.Module != owner.Module
            || owner.Handle.IsNil || method.Handle.IsNil || !owner.IsValueType || owner.IsEnum || owner.IsInterface
            || owner.GenericArity != 1 || owner.Context.TypeArgs is not [var source] || !IsVectorNumericElement(source)
            || method.IsSynthetic || method.Context.MethodArgs.Length != 0
            || method.Attributes != (MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig | MethodAttributes.SpecialName)
            || method.ImplAttributes != MethodImplAttributes.IL || method.Rva == 0 || method.PInvoke is not null)
            return false;
        var reader = method.Module.Reader;
        var type = reader.GetTypeDefinition(owner.Handle);
        var definition = reader.GetMethodDefinition(method.Handle);
        if (!type.GetDeclaringType().IsNil || type.GetGenericParameters().Count != 1
            || !reader.StringComparer.Equals(type.Namespace, "System.Numerics")
            || !reader.StringComparer.Equals(type.Name, "Vector`1")
            || definition.GetDeclaringType() != owner.Handle || definition.GetGenericParameters().Count != 0
            || !IsVectorExplicitSignature(method.Signature, owner))
            return false;
        var blob = reader.GetBlobReader(definition.Signature);
        int token = MetadataTokens.GetRowNumber(owner.Handle) << 2;
        return ReadVectorExplicitPrefix(ref blob, token, out int result)
            && result == (int)method.Signature.ReturnType.Class!.Context.TypeArgs[0].Primitive
            && ReadVectorTypePrefix(ref blob, token)
            && blob.RemainingBytes == 2 && blob.ReadByte() == 0x13 && blob.ReadByte() == 0;
    }

    internal static bool IsVectorExplicitSignature(MethodSignature<TypeDesc> signature, ClassInfo owner) =>
        signature.Header.RawValue == 0 && signature.GenericParameterCount == 0
        && signature.ParameterTypes is [{ Kind: TypeKind.Class, Class: { } source }] && source == owner
        && signature.ReturnType is { Kind: TypeKind.Class, Class: { } target }
        && target.Module == owner.Module && target.Handle == owner.Handle
        && target.IntrinsicCppName == "Dn2CppVectorT" && target.IsValueType && !target.IsEnum && !target.IsInterface
        && target.GenericArity == 1 && target.Context.TypeArgs is [var element] && IsVectorNumericElement(element);

    // MemberRef tokens are module-local. Decoded identities supply their authority;
    // the raw shape rejects modifiers and calling conventions erased by decoding.
    internal static bool IsVectorExplicitReference(BlobReader blob)
    {
        if (!ReadVectorExplicitPrefix(ref blob, 0, out _) || !ReadVectorTypePrefix(ref blob, 0)
            || blob.RemainingBytes == 0)
            return false;
        int element = blob.ReadByte();
        if (element == 0x13)
            return blob.RemainingBytes == 1 && blob.ReadByte() == 0;
        return blob.RemainingBytes == 0 && IsVectorNumericCode(element);
    }

    private static bool ReadVectorExplicitPrefix(ref BlobReader blob, int token, out int result)
    {
        result = 0;
        if (blob.RemainingBytes < 2 || blob.ReadByte() != 0 || blob.ReadByte() != 1
            || !ReadVectorTypePrefix(ref blob, token) || blob.RemainingBytes == 0)
            return false;
        result = blob.ReadByte();
        return IsVectorNumericCode(result);
    }

    private static bool ReadVectorTypePrefix(ref BlobReader blob, int token) =>
        blob.RemainingBytes >= 4 && blob.ReadByte() == 0x15 && blob.ReadByte() == 0x11
        && blob.TryReadCompressedInteger(out int actual) && actual > 0 && (actual >> 2) != 0 && (actual & 3) <= 1
        && (token == 0 || actual == token) && blob.RemainingBytes >= 1 && blob.ReadByte() == 1;

    private static bool IsVectorNumericCode(int code) => code is 4 or 5 or 6 or 7 or 8 or 9 or 10 or 11 or 12 or 13 or 24 or 25;
}
