using System.Reflection;
using System.Reflection.Metadata;

namespace Dn2Cpp;

internal static partial class CoreIntrinsics
{
    // This field-free guard uses strings and spans, not ResourceManager's intrinsic layout.
    internal static bool IsResourceManagerDefaultType(MethodInfo method)
    {
        var cls = method.DeclaringClass;
        if (method.Name != "IsDefaultType" || cls.FullName != "System.Resources.ResourceManager"
            || method.Module.AssemblyName != "System.Private.CoreLib" || cls.Module != method.Module
            || cls.Handle.IsNil || method.Handle.IsNil || cls.IsValueType || cls.IsEnum || cls.IsInterface
            || cls.GenericArity != 0 || cls.Context.TypeArgs.Length != 0 || method.Context.MethodArgs.Length != 0
            || method.Attributes != (MethodAttributes.Assembly | MethodAttributes.Static | MethodAttributes.HideBySig)
            || method.ImplAttributes != MethodImplAttributes.IL || method.Rva == 0 || method.PInvoke is not null)
            return false;
        var reader = method.Module.Reader;
        var owner = reader.GetTypeDefinition(cls.Handle);
        var definition = reader.GetMethodDefinition(method.Handle);
        return owner.GetDeclaringType().IsNil && owner.GetGenericParameters().Count == 0
            && reader.StringComparer.Equals(owner.Namespace, "System.Resources")
            && reader.StringComparer.Equals(owner.Name, "ResourceManager")
            && definition.GetDeclaringType() == cls.Handle && definition.GetGenericParameters().Count == 0
            && IsResourceManagerDefaultTypeSignature(reader.GetBlobReader(definition.Signature))
            && IsResourceManagerDefaultTypeDecodedSignature(method.Signature);
    }

    internal static bool IsResourceManagerDefaultTypeSignature(BlobReader blob) =>
        blob.RemainingBytes == 5 && blob.ReadByte() == 0 && blob.ReadByte() == 2
        && blob.ReadByte() == 2 && blob.ReadByte() == 0x0e && blob.ReadByte() == 0x0e;

    internal static bool IsResourceManagerDefaultTypeDecodedSignature(MethodSignature<TypeDesc> signature) =>
        signature.Header.RawValue == 0 && signature.GenericParameterCount == 0
        && signature.ReturnType is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Boolean }
        && signature.ParameterTypes is [
            { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.String },
            { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.String }];
}
