using System.Reflection;
using System.Reflection.Metadata;
using SRME = System.Reflection.Metadata.Ecma335.MetadataTokens;

namespace Dn2Cpp;

internal static partial class CoreIntrinsics
{
    internal static bool IsStringRawByteReference(MethodInfo method) =>
        method.Name == "GetRawStringDataAsUInt8" && IsStringPrivateEntryPoint(method);

    internal static bool IsStringNullTerminatedLength(MethodInfo method) =>
        method.Name == "wcslen" && IsStringPrivateEntryPoint(method);

    internal static bool IsStringPrivateEntryPoint(MethodInfo method)
    {
        var owner = method.DeclaringClass;
        if (method.Name is not ("GetRawStringDataAsUInt8" or "wcslen")
            || method.Module.AssemblyName != "System.Private.CoreLib" || method.Module != owner.Module
            || owner.FullName != "System.String" || method.IsSynthetic
            || owner.Handle.IsNil || method.Handle.IsNil || owner.IsValueType || owner.IsInterface
            || owner.IsEnum || owner.IsByRefLike || !owner.IsSealed || owner.IsAbstract
            || owner.GenericArity != 0 || owner.Context.TypeArgs.Length != 0 || owner.Context.MethodArgs.Length != 0
            || method.Context.TypeArgs.Length != 0 || method.Context.MethodArgs.Length != 0
            || method.Rva == 0 || method.PInvoke is not null || method.ImplAttributes != MethodImplAttributes.IL)
            return false;
        var reader = method.Module.Reader;
        if (SRME.GetRowNumber(owner.Handle) > reader.TypeDefinitions.Count
            || SRME.GetRowNumber(method.Handle) > reader.MethodDefinitions.Count)
            return false;
        var type = reader.GetTypeDefinition(owner.Handle);
        var definition = reader.GetMethodDefinition(method.Handle);
        bool isStatic = method.Name == "wcslen";
        return CoreIntrinsicTypeMaps.IsStringPrivateEntryPointMethod(method.Module.AssemblyName, owner.FullName, method.Name,
            type.GetDeclaringType().IsNil && type.GetGenericParameters().Count == 0
                && reader.StringComparer.Equals(type.Namespace, "System") && reader.StringComparer.Equals(type.Name, "String")
                && (type.Attributes & (TypeAttributes.VisibilityMask | TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.Interface))
                    == (TypeAttributes.Public | TypeAttributes.Sealed)
                && definition.GetDeclaringType() == owner.Handle && definition.GetGenericParameters().Count == 0
                && reader.StringComparer.Equals(definition.Name, method.Name)
                && method.Attributes == (MethodAttributes.Assembly | MethodAttributes.HideBySig
                    | (isStatic ? MethodAttributes.Static : 0))
                && definition.Attributes == method.Attributes && definition.ImplAttributes == method.ImplAttributes
                && definition.RelativeVirtualAddress == method.Rva,
            IsStringPrivateEntryPointSignature(method.Name, reader.GetBlobReader(definition.Signature))
                && (!method.SignatureReady || IsStringPrivateEntryPointDecodedSignature(method.Name, method.Signature)));
    }

    internal static bool IsStringPrivateEntryPointSignature(string name, BlobReader blob) => name switch
    {
        "GetRawStringDataAsUInt8" => blob.RemainingBytes == 4 && blob.ReadByte() == 0x20 && blob.ReadByte() == 0
            && blob.ReadByte() == (byte)SignatureTypeCode.ByReference && blob.ReadByte() == (byte)SignatureTypeCode.Byte,
        "wcslen" => blob.RemainingBytes == 5 && blob.ReadByte() == 0 && blob.ReadByte() == 1
            && blob.ReadByte() == (byte)SignatureTypeCode.Int32 && blob.ReadByte() == (byte)SignatureTypeCode.Pointer
            && blob.ReadByte() == (byte)SignatureTypeCode.Char,
        _ => false,
    };

    internal static bool IsStringPrivateEntryPointDecodedSignature(string name, MethodSignature<TypeDesc> signature)
    {
        if (signature.GenericParameterCount != 0 || signature.RequiredParameterCount != signature.ParameterTypes.Length)
            return false;
        return name switch
        {
            "GetRawStringDataAsUInt8" => signature.Header.RawValue == 0x20 && signature.ParameterTypes.Length == 0
                && signature.ReturnType is { Kind: TypeKind.ByRef, IsCanonPlaceholder: false, Element: { } element }
                && IsStringPrivatePrimitive(element, PrimitiveTypeCode.Byte),
            "wcslen" => signature.Header.RawValue == 0 && IsStringPrivatePrimitive(signature.ReturnType, PrimitiveTypeCode.Int32)
                && signature.ParameterTypes is [{ Kind: TypeKind.Pointer, IsCanonPlaceholder: false, Element: { } element }]
                && IsStringPrivatePrimitive(element, PrimitiveTypeCode.Char),
            _ => false,
        };
    }

    private static bool IsStringPrivatePrimitive(TypeDesc type, PrimitiveTypeCode code) =>
        type.Kind == TypeKind.Primitive && !type.IsCanonPlaceholder && type.Primitive == code;
}
