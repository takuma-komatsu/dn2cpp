using System.Reflection.Metadata;

namespace Dn2Cpp;

internal static partial class CoreIntrinsics
{
    // Enum value validation is shared with the native public query, not CoreCLR tables.
    internal static bool IsRuntimeTypeEnumName(MethodInfo method)
    {
        if (method.Module.AssemblyName != "System.Private.CoreLib"
            || method.DeclaringClass.Module != method.Module
            || method.DeclaringClass.FullName != "System.RuntimeType" || !method.DeclaringClass.IsSealed
            || method.Name != "GetEnumName"
            || method.Attributes != (System.Reflection.MethodAttributes.Public | System.Reflection.MethodAttributes.Virtual
                | System.Reflection.MethodAttributes.HideBySig)
            || method.Rva == 0 || method.PInvoke is not null
            || (method.ImplAttributes & (System.Reflection.MethodImplAttributes.CodeTypeMask
                | System.Reflection.MethodImplAttributes.ManagedMask | System.Reflection.MethodImplAttributes.InternalCall)) != 0)
            return false;
        var signature = method.Signature;
        var blob = method.Module.Reader.GetBlobReader(method.Module.Reader.GetMethodDefinition(method.Handle).Signature);
        return blob.RemainingBytes == 4 && blob.ReadByte() == 0x20 && blob.ReadByte() == 1
            && blob.ReadByte() == (byte)SignatureTypeCode.String && blob.ReadByte() == (byte)SignatureTypeCode.Object
            && signature.Header.RawValue == 0x20 && signature.GenericParameterCount == 0
            && signature.ReturnType is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.String }
            && signature.ParameterTypes is [{ Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Object }];
    }

    // Enum value validation is shared with the native public query, not CoreCLR tables.
    internal static bool IsRuntimeTypeEnumDefined(MethodInfo method)
    {
        if (method.Module.AssemblyName != "System.Private.CoreLib"
            || method.DeclaringClass.Module != method.Module
            || method.DeclaringClass.FullName != "System.RuntimeType" || !method.DeclaringClass.IsSealed
            || method.Name != "IsEnumDefined"
            || method.Attributes != (System.Reflection.MethodAttributes.Public | System.Reflection.MethodAttributes.Virtual
                | System.Reflection.MethodAttributes.HideBySig)
            || method.Rva == 0 || method.PInvoke is not null
            || (method.ImplAttributes & (System.Reflection.MethodImplAttributes.CodeTypeMask
                | System.Reflection.MethodImplAttributes.ManagedMask | System.Reflection.MethodImplAttributes.InternalCall)) != 0)
            return false;
        var signature = method.Signature;
        var blob = method.Module.Reader.GetBlobReader(method.Module.Reader.GetMethodDefinition(method.Handle).Signature);
        return blob.RemainingBytes == 4 && blob.ReadByte() == 0x20 && blob.ReadByte() == 1
            && blob.ReadByte() == (byte)SignatureTypeCode.Boolean && blob.ReadByte() == (byte)SignatureTypeCode.Object
            && signature.Header.RawValue == 0x20 && signature.GenericParameterCount == 0
            && signature.ReturnType is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Boolean }
            && signature.ParameterTypes is [{ Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Object }];
    }

}
