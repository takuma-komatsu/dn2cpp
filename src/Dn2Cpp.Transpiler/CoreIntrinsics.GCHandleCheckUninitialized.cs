using System.Reflection;
using System.Reflection.Metadata;
using SRME = System.Reflection.Metadata.Ecma335.MetadataTokens;

namespace Dn2Cpp;

internal static partial class CoreIntrinsics
{
    internal static bool IsGCHandleCheckUninitialized(MethodInfo method)
    {
        var owner = method.DeclaringClass;
        if (method.Name != "CheckUninitialized" || owner.FullName != "System.Runtime.InteropServices.GCHandle"
            || method.Module.AssemblyName != "System.Private.CoreLib" || owner.Module != method.Module
            || method.Handle.IsNil || owner.Handle.IsNil || method.IsSynthetic
            || method.Context.TypeArgs.Length != 0 || method.Context.MethodArgs.Length != 0
            || owner.Context.TypeArgs.Length != 0 || owner.Context.MethodArgs.Length != 0
            || owner.GenericArity != 0 || !owner.IsValueType || owner.IsEnum || owner.IsInterface || owner.IsByRefLike
            || method.Attributes != (MethodAttributes.Assembly | MethodAttributes.Static | MethodAttributes.HideBySig)
            || method.ImplAttributes != MethodImplAttributes.AggressiveInlining || method.Rva == 0 || method.PInvoke is not null)
            return false;
        var reader = method.Module.Reader;
        if (!reader.IsAssembly || !reader.StringComparer.Equals(reader.GetAssemblyDefinition().Name, "System.Private.CoreLib")
            || SRME.GetRowNumber(method.Handle) > reader.MethodDefinitions.Count
            || SRME.GetRowNumber(owner.Handle) > reader.TypeDefinitions.Count)
            return false;
        var definition = reader.GetMethodDefinition(method.Handle);
        var type = reader.GetTypeDefinition(owner.Handle);
        if (definition.GetDeclaringType() != owner.Handle || !reader.StringComparer.Equals(definition.Name, method.Name)
            || definition.Attributes != method.Attributes || definition.ImplAttributes != method.ImplAttributes
            || definition.RelativeVirtualAddress != method.Rva || definition.GetGenericParameters().Count != 0
            || !type.GetDeclaringType().IsNil || type.GetGenericParameters().Count != 0
            || (type.Attributes & TypeAttributes.Interface) != 0
            || !reader.StringComparer.Equals(type.Namespace, "System.Runtime.InteropServices") || !reader.StringComparer.Equals(type.Name, "GCHandle")
            || type.BaseType.Kind != HandleKind.TypeDefinition || type.BaseType.IsNil
            || SRME.GetRowNumber(type.BaseType) > reader.TypeDefinitions.Count)
            return false;
        var parent = reader.GetTypeDefinition((TypeDefinitionHandle)type.BaseType);
        return parent.GetDeclaringType().IsNil && parent.GetGenericParameters().Count == 0
            && reader.StringComparer.Equals(parent.Namespace, "System")
            && reader.StringComparer.Equals(parent.Name, "ValueType")
            && IsGCHandleCheckUninitializedSignature(reader.GetBlobReader(definition.Signature))
            && (!method.SignatureReady || IsGCHandleCheckUninitializedDecodedSignature(method.Signature));
    }

    internal static bool IsGCHandleCheckUninitializedSignature(BlobReader blob) =>
        blob.RemainingBytes == 4 && blob.ReadByte() == 0x00 && blob.ReadByte() == 0x01 && blob.ReadByte() == 0x01 && blob.ReadByte() == 0x18;

    internal static bool IsGCHandleCheckUninitializedDecodedSignature(MethodSignature<TypeDesc> signature) =>
        signature.Header.RawValue == 0x00 && signature.GenericParameterCount == 0
        && signature.RequiredParameterCount == 1
        && signature.ReturnType is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Void, IsCanonPlaceholder: false }
        && signature.ParameterTypes is [{ Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.IntPtr, IsCanonPlaceholder: false }];
}
