using System.Reflection;
using System.Reflection.Metadata;

namespace Dn2Cpp;

internal static partial class CoreIntrinsics
{
    internal static bool IsDateTimeLeapSecondSupportName(string? owner, string name) =>
        owner == "System.DateTime" && name == "get_SystemSupportsLeapSeconds";

    internal static bool IsDateTimeLeapSecondSupportDefinition(MetadataReader reader, MethodDefinitionHandle handle)
    {
        if (handle.IsNil || !reader.IsAssembly
            || !reader.StringComparer.Equals(reader.GetAssemblyDefinition().Name, "System.Private.CoreLib")) return false;
        var method = reader.GetMethodDefinition(handle);
        if (!reader.StringComparer.Equals(method.Name, "get_SystemSupportsLeapSeconds")
            || method.Attributes != (MethodAttributes.Assembly | MethodAttributes.Static
                | MethodAttributes.HideBySig | MethodAttributes.SpecialName)
            || method.ImplAttributes != MethodImplAttributes.IL || method.RelativeVirtualAddress == 0
            || method.GetGenericParameters().Count != 0
            || !IsDateTimeLeapSecondSupportSignature(reader.GetBlobReader(method.Signature))) return false;
        var owner = reader.GetTypeDefinition(method.GetDeclaringType());
        if (!owner.GetDeclaringType().IsNil || owner.GetGenericParameters().Count != 0
            || !reader.StringComparer.Equals(owner.Namespace, "System") || !reader.StringComparer.Equals(owner.Name, "DateTime")
            || (owner.Attributes & (TypeAttributes.Interface | TypeAttributes.Abstract | TypeAttributes.Sealed)) != TypeAttributes.Sealed
            || owner.BaseType.Kind != HandleKind.TypeDefinition) return false;
        var baseType = reader.GetTypeDefinition((TypeDefinitionHandle)owner.BaseType);
        return baseType.GetDeclaringType().IsNil && reader.StringComparer.Equals(baseType.Namespace, "System")
            && reader.StringComparer.Equals(baseType.Name, "ValueType");
    }

    internal static bool IsDateTimeLeapSecondSupport(MethodInfo method)
    {
        var owner = method.DeclaringClass;
        if (!IsDateTimeLeapSecondSupportName(owner.FullName, method.Name)
            || method.Module.AssemblyName != "System.Private.CoreLib" || method.Module != owner.Module
            || owner.Handle.IsNil || method.Handle.IsNil || method.IsSynthetic || method.PInvoke is not null
            || !owner.IsValueType || owner.IsEnum || owner.IsInterface || owner.IsByRefLike || owner.GenericArity != 0
            || owner.Context.TypeArgs.Length != 0 || owner.Context.MethodArgs.Length != 0
            || method.Context.TypeArgs.Length != 0 || method.Context.MethodArgs.Length != 0)
            return false;
        var definition = method.Module.Reader.GetMethodDefinition(method.Handle);
        return definition.GetDeclaringType() == owner.Handle && definition.RelativeVirtualAddress == method.Rva
            && definition.Attributes == method.Attributes && definition.ImplAttributes == method.ImplAttributes
            && IsDateTimeLeapSecondSupportDefinition(method.Module.Reader, method.Handle);
    }

    private static bool IsDateTimeLeapSecondSupportSignature(BlobReader blob) =>
        blob.RemainingBytes == 3 && blob.ReadByte() == 0 && blob.ReadByte() == 0
        && blob.ReadByte() == (byte)SignatureTypeCode.Boolean;

    internal static bool IsDateTimeLeapSecondSupportReference(MethodInfo method,
        MethodSignature<TypeDesc> signature, BlobReader blob) =>
        IsDateTimeLeapSecondSupport(method) && IsDateTimeLeapSecondSupportSignature(blob)
        && signature.Header.RawValue == 0 && signature.GenericParameterCount == 0
        && signature.RequiredParameterCount == 0 && signature.ParameterTypes.Length == 0
        && signature.ReturnType is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Boolean, IsCanonPlaceholder: false };
}
