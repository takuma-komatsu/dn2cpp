using System.Reflection;
using System.Reflection.Metadata;
using SRME = System.Reflection.Metadata.Ecma335.MetadataTokens;

namespace Dn2Cpp;

internal static partial class CoreIntrinsics
{
    // Static generic formatting helpers share these names, not the boxed-instance contract.
    internal static bool IsEnumInstanceFormatMethod(MethodInfo method)
    {
        var owner = method.DeclaringClass;
        if (!CoreIntrinsicTypeMaps.IsEnumInstanceFormatMember(owner.FullName, method.Name)
            || method.Module.AssemblyName != "System.Private.CoreLib" || method.Module != owner.Module
            || method.IsSynthetic || method.IsStatic || method.Rva == 0 || method.PInvoke is not null
            || method.ImplAttributes != MethodImplAttributes.IL || method.Handle.IsNil || owner.Handle.IsNil
            || SRME.GetRowNumber(method.Handle) > method.Module.Reader.MethodDefinitions.Count
            || SRME.GetRowNumber(owner.Handle) > method.Module.Reader.TypeDefinitions.Count
            || owner.IsValueType || owner.IsEnum || owner.IsInterface || owner.IsByRefLike || owner.GenericArity != 0
            || owner.Context.TypeArgs.Length != 0 || owner.Context.MethodArgs.Length != 0
            || method.Context.TypeArgs.Length != 0 || method.Context.MethodArgs.Length != 0)
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
            || !IsEnumInstanceFormatSignature(reader, method.Name, method.Attributes, reader.GetBlobReader(definition.Signature)))
            return false;
        return !method.SignatureReady || IsEnumInstanceFormatDecodedSignature(method);
    }

    internal static bool IsEnumInstanceFormatSignature(MetadataReader reader, string name,
        MethodAttributes attributes, BlobReader blob)
    {
        if (!CoreIntrinsicTypeMaps.IsEnumInstanceFormatMember("System.Enum", name)
            || blob.RemainingBytes < 3 || blob.ReadByte() != 0x20)
            return false;
        int count = blob.ReadByte();
        if (count > 2) return false;
        string? result = ReadEnumInstanceFormatType(reader, ref blob);
        if (result is null) return false;
        string? first = count > 0 ? ReadEnumInstanceFormatType(reader, ref blob) : null;
        if (count > 0 && first is null) return false;
        string? second = count > 1 ? ReadEnumInstanceFormatType(reader, ref blob) : null;
        if (count > 1 && second is null) return false;
        return blob.RemainingBytes == 0
            && CoreIntrinsicTypeMaps.IsEnumInstanceFormatMethod("System.Private.CoreLib", "System.Enum", name,
                false, 0, (int)attributes, 0, count, result, first, second);
    }

    private static string? ReadEnumInstanceFormatType(MetadataReader reader, ref BlobReader blob)
    {
        if (blob.RemainingBytes == 0) return null;
        byte code = blob.ReadByte();
        switch (code)
        {
            case (byte)SignatureTypeCode.Boolean: return "System.Boolean";
            case (byte)SignatureTypeCode.Int32: return "System.Int32";
            case (byte)SignatureTypeCode.String: return "System.String";
            case (byte)SignatureTypeCode.Object: return "System.Object";
        }
        if (code is not (0x11 or 0x12) || !blob.TryReadCompressedInteger(out int token)
            || (token & 3) != 0 || token >> 2 == 0 || token >> 2 > reader.TypeDefinitions.Count)
            return null;
        var handle = SRME.TypeDefinitionHandle(token >> 2);
        string name = code == 0x11 ? "TypeCode" : "IFormatProvider";
        return IsEnumInstanceFormatNamedType(reader, handle, name)
            ? code == 0x11 ? "System.TypeCode" : "System.IFormatProvider" : null;
    }

    private static bool IsEnumInstanceFormatNamedType(MetadataReader reader, TypeDefinitionHandle handle, string name)
    {
        if (handle.IsNil || SRME.GetRowNumber(handle) > reader.TypeDefinitions.Count)
            return false;
        var type = reader.GetTypeDefinition(handle);
        if (!type.GetDeclaringType().IsNil || type.GetGenericParameters().Count != 0
            || !reader.StringComparer.Equals(type.Namespace, "System") || !reader.StringComparer.Equals(type.Name, name))
            return false;
        if (name == "IFormatProvider")
            return (type.Attributes & (TypeAttributes.Interface | TypeAttributes.Abstract | TypeAttributes.Sealed))
                == (TypeAttributes.Interface | TypeAttributes.Abstract);
        if (name != "TypeCode" || type.BaseType.Kind != HandleKind.TypeDefinition
            || (type.Attributes & (TypeAttributes.Interface | TypeAttributes.Abstract | TypeAttributes.Sealed)) != TypeAttributes.Sealed)
            return false;
        var parent = reader.GetTypeDefinition((TypeDefinitionHandle)type.BaseType);
        if (!parent.GetDeclaringType().IsNil || !reader.StringComparer.Equals(parent.Namespace, "System")
            || !reader.StringComparer.Equals(parent.Name, "Enum"))
            return false;
        bool found = false;
        foreach (var fieldHandle in type.GetFields())
        {
            var field = reader.GetFieldDefinition(fieldHandle);
            if ((field.Attributes & FieldAttributes.Static) != 0) continue;
            var blob = reader.GetBlobReader(field.Signature);
            if (found || !reader.StringComparer.Equals(field.Name, "value__") || blob.RemainingBytes != 2
                || blob.ReadByte() != 0x06 || blob.ReadByte() != (byte)SignatureTypeCode.Int32)
                return false;
            found = true;
        }
        return found;
    }

    private static bool IsEnumInstanceFormatDecodedSignature(MethodInfo method)
    {
        var signature = method.Signature;
        var parameters = signature.ParameterTypes;
        if (signature.Header.RawValue != 0x20 || signature.GenericParameterCount != 0
            || signature.RequiredParameterCount != parameters.Length || parameters.Length > 2)
            return false;
        string? result = EnumInstanceFormatDecodedType(method.Module, signature.ReturnType);
        string? first = parameters.Length > 0 ? EnumInstanceFormatDecodedType(method.Module, parameters[0]) : null;
        string? second = parameters.Length > 1 ? EnumInstanceFormatDecodedType(method.Module, parameters[1]) : null;
        return result is not null && (parameters.Length == 0 || first is not null) && (parameters.Length < 2 || second is not null)
            && CoreIntrinsicTypeMaps.IsEnumInstanceFormatMethod(method.Module.AssemblyName, method.DeclaringClass.FullName,
                method.Name, method.IsStatic, 0, (int)method.Attributes, (int)method.ImplAttributes,
                parameters.Length, result, first, second);
    }

    private static string? EnumInstanceFormatDecodedType(Module module, TypeDesc type)
    {
        if (type.IsCanonPlaceholder) return null;
        if (type.Kind == TypeKind.Primitive)
            return type.Primitive switch
            {
                PrimitiveTypeCode.Boolean => "System.Boolean",
                PrimitiveTypeCode.Int32 => "System.Int32",
                PrimitiveTypeCode.String => "System.String",
                PrimitiveTypeCode.Object => "System.Object",
                _ => null,
            };
        if (type is not { Kind: TypeKind.Class, Class: { } cls } || cls.Module != module || cls.GenericArity != 0
            || cls.Context.TypeArgs.Length != 0 || cls.Context.MethodArgs.Length != 0 || cls.IsByRefLike)
            return null;
        if (cls.FullName == "System.IFormatProvider" && cls.IsInterface && !cls.IsValueType && !cls.IsEnum
            && IsEnumInstanceFormatNamedType(module.Reader, cls.Handle, "IFormatProvider"))
            return "System.IFormatProvider";
        return cls.FullName == "System.TypeCode" && cls.IsEnum && !cls.IsInterface
            && cls.EnumUnderlying == PrimitiveTypeCode.Int32 && IsEnumInstanceFormatNamedType(module.Reader, cls.Handle, "TypeCode")
            ? "System.TypeCode" : null;
    }
}
