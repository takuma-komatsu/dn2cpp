using System.Reflection;
using System.Reflection.Metadata;

namespace Dn2Cpp;

internal static partial class CoreIntrinsics
{
    internal static bool IsMarshalStructureToPtr(MethodInfo method)
    {
        var cls = method.DeclaringClass;
        if (method.Module.AssemblyName != "System.Private.CoreLib" || method.Module != cls.Module
            || cls.FullName != "System.Runtime.InteropServices.Marshal" || method.Name != "StructureToPtr"
            || cls.Handle.IsNil || method.Handle.IsNil || method.IsSynthetic
            || cls.IsValueType || cls.IsEnum || cls.IsInterface || cls.IsByRefLike
            || cls.GenericArity != 0 || cls.Context.TypeArgs.Length != 0 || cls.Context.MethodArgs.Length != 0
            || method.Context.TypeArgs.Length != 0 || method.Context.MethodArgs.Length != 0)
            return false;
        var reader = method.Module.Reader;
        var definition = reader.GetMethodDefinition(method.Handle);
        var owner = reader.GetTypeDefinition(cls.Handle);
        return owner.GetDeclaringType().IsNil && owner.GetGenericParameters().Count == 0
            && reader.StringComparer.Equals(owner.Namespace, "System.Runtime.InteropServices")
            && reader.StringComparer.Equals(owner.Name, "Marshal")
            && definition.GetDeclaringType() == cls.Handle
            && reader.StringComparer.Equals(definition.Name, method.Name)
            && definition.GetGenericParameters().Count == 0
            && method.Attributes == (MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig)
            && method.ImplAttributes == MethodImplAttributes.IL && method.Rva != 0 && method.PInvoke is null
            && definition.Attributes == method.Attributes && definition.ImplAttributes == method.ImplAttributes
            && definition.RelativeVirtualAddress == method.Rva
            && IsMarshalStructureToPtrSignature(reader.GetBlobReader(definition.Signature))
            && (!method.SignatureReady || IsMarshalStructureToPtrDecodedSignature(method.Signature));
    }

    internal static bool IsMarshalStructureToPtrSignature(BlobReader blob) =>
        blob.RemainingBytes == 6 && blob.ReadByte() == 0 && blob.ReadByte() == 3
        && blob.ReadByte() == 1 && blob.ReadByte() == 0x1c && blob.ReadByte() == 0x18 && blob.ReadByte() == 2;

    internal static bool IsMarshalStructureToPtrDecodedSignature(MethodSignature<TypeDesc> signature) =>
        signature.Header.RawValue == 0 && signature.GenericParameterCount == 0 && signature.RequiredParameterCount == 3
        && signature.ReturnType is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Void, IsCanonPlaceholder: false }
        && signature.ParameterTypes is [{ Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Object, IsCanonPlaceholder: false },
            { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.IntPtr, IsCanonPlaceholder: false },
            { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Boolean, IsCanonPlaceholder: false }];
}
