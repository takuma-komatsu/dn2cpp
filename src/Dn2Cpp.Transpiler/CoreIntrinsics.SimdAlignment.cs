using System.Reflection;
using System.Reflection.Metadata;

namespace Dn2Cpp;

internal static partial class CoreIntrinsics
{
    internal static bool IsVectorAlignmentManagedBody(MethodInfo method) =>
        IsVectorAlignment(method) || IsVectorAccessViolationHelper(method);

    internal static bool IsVectorAccessViolationHelper(MethodInfo method)
    {
        var owner = method.DeclaringClass;
        if (method.Name != "ThrowAccessViolationException" || owner.FullName != "System.ThrowHelper"
            || owner.Module.AssemblyName != "System.Private.CoreLib" || method.Module != owner.Module
            || owner.Handle.IsNil || method.Handle.IsNil || owner.GenericArity != 0 || owner.IsInterface || owner.IsValueType || owner.IsEnum
            || method.IsSynthetic || method.PInvoke is not null || method.Context.TypeArgs.Length != 0
            || method.Context.MethodArgs.Length != 0 || method.Rva == 0
            || method.Attributes != (MethodAttributes.Assembly | MethodAttributes.Static | MethodAttributes.HideBySig)
            || method.ImplAttributes != MethodImplAttributes.IL) return false;
        var reader = method.Module.Reader;
        var type = reader.GetTypeDefinition(owner.Handle);
        var definition = reader.GetMethodDefinition(method.Handle);
        if (type.Attributes != (TypeAttributes.NotPublic | TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit)
            || !type.GetDeclaringType().IsNil || type.GetGenericParameters().Count != 0 || type.BaseType.Kind != HandleKind.TypeDefinition
            || !reader.StringComparer.Equals(type.Namespace, "System") || !reader.StringComparer.Equals(type.Name, "ThrowHelper")
            || definition.GetDeclaringType() != owner.Handle || definition.GetGenericParameters().Count != 0
            || !reader.StringComparer.Equals(definition.Name, method.Name) || definition.Attributes != method.Attributes
            || definition.ImplAttributes != method.ImplAttributes || definition.RelativeVirtualAddress != method.Rva
            || !IsVectorAccessViolationSignature(reader.GetBlobReader(definition.Signature))) return false;
        var baseType = reader.GetTypeDefinition((TypeDefinitionHandle)type.BaseType);
        if (!reader.StringComparer.Equals(baseType.Namespace, "System") || !reader.StringComparer.Equals(baseType.Name, "Object")) return false;
        foreach (var member in type.GetMethods())
            if (reader.StringComparer.Equals(reader.GetMethodDefinition(member).Name, ".cctor")) return false;
        var signature = method.Signature;
        return signature.Header.RawValue == 0 && signature.GenericParameterCount == 0
            && signature.RequiredParameterCount == 0 && signature.ParameterTypes.Length == 0
            && signature.ReturnType is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Void, IsCanonPlaceholder: false };
    }

    internal static bool IsVectorAccessViolationSignature(BlobReader raw) =>
        raw.RemainingBytes == 3 && raw.ReadByte() == 0 && raw.ReadByte() == 0 && raw.ReadByte() == 1;
}
