using System.Reflection;
using System.Reflection.Metadata;
using SRME = System.Reflection.Metadata.Ecma335.MetadataTokens;

namespace Dn2Cpp;

internal static partial class CoreIntrinsics
{
    // Scalar RuntimeType/UInt64 lookup keeps its callable, declaration-checked body.
    private static bool IsEnumPublicGetNameSignature(MethodSignature<TypeDesc> signature) =>
        signature.Header.RawValue == 0 && signature.GenericParameterCount == 0
        && signature.RequiredParameterCount == 2 && signature.ReturnType.IsString
        && signature.ParameterTypes is [{ Kind: TypeKind.Class, Class.FullName: "System.Type" },
            { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Object }];

    internal static bool IsEnumRuntimeLeafMethod(MethodInfo method)
    {
        var owner = method.DeclaringClass;
        if (!CoreIntrinsicTypeMaps.IsEnumRuntimeLeafDeclaration(method.Module.AssemblyName, owner.FullName,
                method.Name, method.IsStatic, method.Context.MethodArgs.Length, (int)method.Attributes, (int)method.ImplAttributes)
            || method.Module != owner.Module || method.IsSynthetic || method.Rva == 0 || method.PInvoke is not null
            || method.Handle.IsNil || owner.Handle.IsNil
            || SRME.GetRowNumber(method.Handle) > method.Module.Reader.MethodDefinitions.Count
            || SRME.GetRowNumber(owner.Handle) > method.Module.Reader.TypeDefinitions.Count
            || owner.IsValueType || owner.IsEnum || owner.IsInterface || owner.IsByRefLike || owner.GenericArity != 0
            || owner.Context.TypeArgs.Length != 0 || owner.Context.MethodArgs.Length != 0
            || method.Context.TypeArgs.Length != 0)
            return false;
        var reader = method.Module.Reader;
        var type = reader.GetTypeDefinition(owner.Handle);
        var definition = reader.GetMethodDefinition(method.Handle);
        if (!type.GetDeclaringType().IsNil || type.GetGenericParameters().Count != 0
            || !reader.StringComparer.Equals(type.Namespace, "System") || !reader.StringComparer.Equals(type.Name, "Enum")
            || (type.Attributes & (TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.Interface)) != TypeAttributes.Abstract
            || definition.GetDeclaringType() != owner.Handle || definition.GetGenericParameters().Count != 0
            || !reader.StringComparer.Equals(definition.Name, method.Name)
            || definition.Attributes != method.Attributes || definition.ImplAttributes != method.ImplAttributes
            || definition.RelativeVirtualAddress != method.Rva
            || !IsEnumRuntimeLeafSignature(reader, method.Name, method.IsStatic, reader.GetBlobReader(definition.Signature)))
            return false;
        if (!method.SignatureReady) return true;
        var signature = method.Signature;
        var parameters = signature.ParameterTypes;
        int count = method.IsStatic ? method.Name == "GetName" ? 2 : 1 : 0;
        if (signature.Header.RawValue != (method.IsStatic ? 0 : 0x20) || signature.GenericParameterCount != 0
            || signature.RequiredParameterCount != count || parameters.Length != count)
            return false;
        bool scalar = method.Name == "GetName";
        if (scalar ? signature.ReturnType is not { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.String, IsCanonPlaceholder: false }
            : !IsEnumRuntimeLeafDecodedType(method.Module, signature.ReturnType, method.Name == "InternalGetCorElementType"))
            return false;
        return (count == 0 || IsEnumRuntimeLeafDecodedType(method.Module, parameters[0], element: false))
            && (!scalar || parameters[1] is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.UInt64, IsCanonPlaceholder: false });
    }

    internal static bool IsEnumRuntimeLeafSignature(MetadataReader reader, string name, bool isStatic, BlobReader blob)
    {
        int count = isStatic ? name == "GetName" ? 2 : 1 : 0;
        if (blob.RemainingBytes < 3 || blob.ReadByte() != (isStatic ? 0 : 0x20) || blob.ReadByte() != count)
            return false;
        if (name == "GetName")
        {
            if (blob.ReadByte() != (byte)SignatureTypeCode.String) return false;
        }
        else if (!ReadEnumRuntimeLeafType(reader, ref blob, name == "InternalGetCorElementType"))
            return false;
        if (count > 0 && !ReadEnumRuntimeLeafType(reader, ref blob, element: false)) return false;
        if (count == 2 && (blob.RemainingBytes == 0 || blob.ReadByte() != (byte)SignatureTypeCode.UInt64)) return false;
        return blob.RemainingBytes == 0;
    }

    private static bool ReadEnumRuntimeLeafType(MetadataReader reader, ref BlobReader blob, bool element)
    {
        if (blob.RemainingBytes < 2 || blob.ReadByte() != (element ? 0x11 : 0x12)
            || !blob.TryReadCompressedInteger(out int token) || (token & 3) != 0
            || token >> 2 == 0 || token >> 2 > reader.TypeDefinitions.Count)
            return false;
        var handle = SRME.TypeDefinitionHandle(token >> 2);
        return element ? IsEnumCorElementType(reader, handle) : IsEnumStaticFormatRuntimeType(reader, handle);
    }

    private static bool IsEnumRuntimeLeafDecodedType(Module module, TypeDesc type, bool element)
    {
        if (type is not { Kind: TypeKind.Class, Class: { } cls, IsCanonPlaceholder: false }
            || cls.Module != module || cls.IsInterface || cls.IsByRefLike || cls.GenericArity != 0
            || cls.Context.TypeArgs.Length != 0 || cls.Context.MethodArgs.Length != 0)
            return false;
        return element ? cls.IsEnum && cls.EnumUnderlying == PrimitiveTypeCode.Byte && IsEnumCorElementType(module.Reader, cls.Handle)
            : !cls.IsValueType && !cls.IsEnum && IsEnumStaticFormatRuntimeType(module.Reader, cls.Handle);
    }

    private static bool IsEnumCorElementType(MetadataReader reader, TypeDefinitionHandle handle)
    {
        if (handle.IsNil || SRME.GetRowNumber(handle) > reader.TypeDefinitions.Count) return false;
        var type = reader.GetTypeDefinition(handle);
        if (!type.GetDeclaringType().IsNil || type.GetGenericParameters().Count != 0
            || !reader.StringComparer.Equals(type.Namespace, "System.Reflection") || !reader.StringComparer.Equals(type.Name, "CorElementType")
            || (type.Attributes & (TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.Interface)) != TypeAttributes.Sealed
            || type.BaseType.Kind != HandleKind.TypeDefinition)
            return false;
        var parent = reader.GetTypeDefinition((TypeDefinitionHandle)type.BaseType);
        if (!parent.GetDeclaringType().IsNil || !reader.StringComparer.Equals(parent.Namespace, "System")
            || !reader.StringComparer.Equals(parent.Name, "Enum")) return false;
        bool found = false;
        foreach (var fieldHandle in type.GetFields())
        {
            var field = reader.GetFieldDefinition(fieldHandle);
            if ((field.Attributes & FieldAttributes.Static) != 0) continue;
            var blob = reader.GetBlobReader(field.Signature);
            if (found || !reader.StringComparer.Equals(field.Name, "value__") || blob.RemainingBytes != 2
                || blob.ReadByte() != 0x06 || blob.ReadByte() != (byte)SignatureTypeCode.Byte)
                return false;
            found = true;
        }
        return found;
    }
}
