using System.Reflection;
using System.Reflection.Metadata;
using SRME = System.Reflection.Metadata.Ecma335.MetadataTokens;

namespace Dn2Cpp;

internal static partial class CoreIntrinsics
{
    // Only closed integer pairs have the native storage/formatting contract.
    internal static bool IsEnumStaticFormatMethod(MethodInfo method)
    {
        var owner = method.DeclaringClass;
        var args = method.Context.MethodArgs;
        if (owner.FullName != "System.Enum" || method.Name != "ToString"
            || method.Module.AssemblyName != "System.Private.CoreLib" || method.Module != owner.Module
            || method.IsSynthetic || method.Attributes != (MethodAttributes)0x0091
            || method.ImplAttributes != MethodImplAttributes.NoInlining || method.Rva == 0 || method.PInvoke is not null
            || method.Handle.IsNil || owner.Handle.IsNil
            || SRME.GetRowNumber(method.Handle) > method.Module.Reader.MethodDefinitions.Count
            || SRME.GetRowNumber(owner.Handle) > method.Module.Reader.TypeDefinitions.Count
            || owner.IsValueType || owner.IsEnum || owner.IsInterface || owner.IsByRefLike || owner.GenericArity != 0
            || owner.Context.TypeArgs.Length != 0 || owner.Context.MethodArgs.Length != 0
            || method.Context.TypeArgs.Length != 0 || args.Length != 2 || !IsEnumStaticFormatPair(args[0], args[1]))
            return false;
        var reader = method.Module.Reader;
        var type = reader.GetTypeDefinition(owner.Handle);
        var definition = reader.GetMethodDefinition(method.Handle);
        if (!type.GetDeclaringType().IsNil || type.GetGenericParameters().Count != 0
            || !reader.StringComparer.Equals(type.Namespace, "System") || !reader.StringComparer.Equals(type.Name, "Enum")
            || (type.Attributes & (TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.Interface)) != TypeAttributes.Abstract
            || definition.GetDeclaringType() != owner.Handle || definition.GetGenericParameters().Count != 2
            || !reader.StringComparer.Equals(definition.Name, method.Name)
            || definition.Attributes != method.Attributes || definition.ImplAttributes != method.ImplAttributes
            || definition.RelativeVirtualAddress != method.Rva
            || !IsEnumStaticFormatSignature(reader, reader.GetBlobReader(definition.Signature)))
            return false;
        if (!method.SignatureReady) return true;
        var signature = method.Signature;
        var ps = signature.ParameterTypes;
        if (signature.Header.RawValue != 0x10 || signature.GenericParameterCount != 2
            || signature.RequiredParameterCount != ps.Length || ps.Length is not (2 or 3)
            || !IsEnumStaticFormatPrimitive(signature.ReturnType, PrimitiveTypeCode.String)
            || ps[0] is not { Kind: TypeKind.Class, Class: { } runtime } || ps[0].IsCanonPlaceholder
            || runtime.Module != method.Module || runtime.FullName != "System.RuntimeType"
            || runtime.IsInterface || runtime.IsValueType || runtime.IsEnum || runtime.IsByRefLike || runtime.GenericArity != 0
            || runtime.Context.TypeArgs.Length != 0 || runtime.Context.MethodArgs.Length != 0
            || !IsEnumStaticFormatRuntimeType(reader, runtime.Handle)
            || ps[^1] is not { Kind: TypeKind.ByRef, Element: { } element } || ps[^1].IsCanonPlaceholder
            || !IsEnumStaticFormatPrimitive(element, PrimitiveTypeCode.Byte))
            return false;
        return ps.Length == 2 || IsEnumStaticFormatPrimitive(ps[1], PrimitiveTypeCode.Char);
    }

    private static bool IsEnumStaticFormatPrimitive(TypeDesc type, PrimitiveTypeCode code) =>
        type.Kind == TypeKind.Primitive && !type.IsCanonPlaceholder && type.Primitive == code;

    private static bool IsEnumStaticFormatPair(TypeDesc underlying, TypeDesc storage)
    {
        if (underlying.Kind != TypeKind.Primitive || storage.Kind != TypeKind.Primitive
            || underlying.IsCanonPlaceholder || storage.IsCanonPlaceholder)
            return false;
        return (underlying.Primitive, storage.Primitive) is
            (PrimitiveTypeCode.SByte or PrimitiveTypeCode.Byte, PrimitiveTypeCode.Byte)
            or (PrimitiveTypeCode.Int16 or PrimitiveTypeCode.UInt16, PrimitiveTypeCode.UInt16)
            or (PrimitiveTypeCode.Int32 or PrimitiveTypeCode.UInt32, PrimitiveTypeCode.UInt32)
            or (PrimitiveTypeCode.Int64 or PrimitiveTypeCode.UInt64, PrimitiveTypeCode.UInt64);
    }

    internal static bool IsEnumStaticFormatSignature(MetadataReader reader, BlobReader blob)
    {
        if (blob.RemainingBytes < 8 || blob.ReadByte() != 0x10 || blob.ReadByte() != 2)
            return false;
        int count = blob.ReadByte();
        if (count is not (2 or 3) || blob.ReadByte() != (byte)SignatureTypeCode.String
            || blob.ReadByte() != 0x12 || !blob.TryReadCompressedInteger(out int token)
            || (token & 3) != 0 || token >> 2 == 0 || token >> 2 > reader.TypeDefinitions.Count
            || !IsEnumStaticFormatRuntimeType(reader, SRME.TypeDefinitionHandle(token >> 2)))
            return false;
        if (count == 3 && (blob.RemainingBytes == 0 || blob.ReadByte() != (byte)SignatureTypeCode.Char))
            return false;
        return blob.RemainingBytes == 2 && blob.ReadByte() == (byte)SignatureTypeCode.ByReference
            && blob.ReadByte() == (byte)SignatureTypeCode.Byte;
    }

    private static bool IsEnumStaticFormatRuntimeType(MetadataReader reader, TypeDefinitionHandle handle)
    {
        if (handle.IsNil || SRME.GetRowNumber(handle) > reader.TypeDefinitions.Count) return false;
        var type = reader.GetTypeDefinition(handle);
        return type.GetDeclaringType().IsNil && type.GetGenericParameters().Count == 0
            && reader.StringComparer.Equals(type.Namespace, "System") && reader.StringComparer.Equals(type.Name, "RuntimeType")
            && (type.Attributes & (TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.Interface)) == TypeAttributes.Sealed;
    }
}
