using System.Reflection;
using System.Reflection.Metadata;
using SRME = System.Reflection.Metadata.Ecma335.MetadataTokens;

namespace Dn2Cpp;

internal static partial class CoreIntrinsics
{
    internal static bool IsRuntimeTypeGenericFactory(MethodInfo method)
    {
        if (method.Name != "CreateInstanceForAnotherGenericParameter"
            || method.DeclaringClass.FullName != "System.RuntimeTypeHandle"
            || method.Module.AssemblyName != "System.Private.CoreLib"
            || method.Module != method.DeclaringClass.Module || method.Handle.IsNil
            || method.DeclaringClass.Handle.IsNil || method.IsSynthetic || method.Context.MethodArgs.Length != 0
            || method.Context.TypeArgs.Length != 0 || method.DeclaringClass.Context.TypeArgs.Length != 0
            || method.DeclaringClass.Context.MethodArgs.Length != 0
            || !method.DeclaringClass.IsValueType || method.DeclaringClass.IsEnum
            || method.DeclaringClass.IsInterface || method.DeclaringClass.IsByRefLike
            || method.DeclaringClass.GenericArity != 0
            || method.Attributes != (MethodAttributes.Assembly | MethodAttributes.Static | MethodAttributes.HideBySig)
            || method.ImplAttributes != MethodImplAttributes.IL || method.Rva == 0 || method.PInvoke is not null)
            return false;
        var reader = method.Module.Reader;
        var definition = reader.GetMethodDefinition(method.Handle);
        var owner = reader.GetTypeDefinition(method.DeclaringClass.Handle);
        return definition.GetDeclaringType() == method.DeclaringClass.Handle
            && reader.StringComparer.Equals(definition.Name, method.Name)
            && definition.Attributes == method.Attributes && definition.ImplAttributes == method.ImplAttributes
            && definition.RelativeVirtualAddress == method.Rva
            && definition.GetGenericParameters().Count == 0 && owner.GetDeclaringType().IsNil
            && (owner.Attributes & TypeAttributes.Interface) == 0
            && IsGenericFactoryCoreDefinition(method.Module, owner.BaseType, "System", "ValueType")
            && owner.GetGenericParameters().Count == 0
            && reader.StringComparer.Equals(owner.Namespace, "System")
            && reader.StringComparer.Equals(owner.Name, "RuntimeTypeHandle")
            && IsRuntimeTypeGenericFactorySignature(reader.GetBlobReader(definition.Signature), out int parameters, out var type)
            && IsGenericFactoryRuntimeType(method.Module, type)
            && (!method.SignatureReady || method.Signature.ParameterTypes.Length == parameters
                && IsRuntimeTypeGenericFactoryDecodedSignature(method.Signature, method.Module));
    }

    // All operands name the same RuntimeType identity. A TypeSpec or modifier is
    // not the private runtime-object ABI, even if its decoded display name agrees.
    internal static bool IsRuntimeTypeGenericFactorySignature(BlobReader blob, out int parameters, out EntityHandle type)
    {
        parameters = 0;
        type = default;
        if (blob.RemainingBytes < 3 || blob.ReadByte() != 0) return false;
        parameters = blob.ReadByte();
        if (parameters is not (2 or 3) || blob.ReadByte() != 0x1c) return false;
        int identity = 0;
        for (int i = 0; i < parameters; i++)
        {
            if (blob.RemainingBytes == 0 || blob.ReadByte() != 0x12
                || !blob.TryReadCompressedInteger(out int token) || token <= 0 || token >> 2 == 0
                || (token & 3) > 1 || i != 0 && token != identity)
                return false;
            identity = token;
        }
        if (blob.RemainingBytes != 0) return false;
        type = (identity & 3) == 0 ? SRME.TypeDefinitionHandle(identity >> 2) : SRME.TypeReferenceHandle(identity >> 2);
        return true;
    }

    private static bool IsGenericFactoryCoreDefinition(Module module, EntityHandle handle, string space, string name)
    {
        if (module.AssemblyName != "System.Private.CoreLib" || handle.Kind != HandleKind.TypeDefinition
            || handle.IsNil || SRME.GetRowNumber(handle) > module.Reader.TypeDefinitions.Count)
            return false;
        var definition = module.Reader.GetTypeDefinition((TypeDefinitionHandle)handle);
        return definition.GetDeclaringType().IsNil && definition.GetGenericParameters().Count == 0
            && (definition.Attributes & TypeAttributes.Interface) == 0
            && module.Reader.StringComparer.Equals(definition.Namespace, space)
            && module.Reader.StringComparer.Equals(definition.Name, name);
    }

    internal static bool IsGenericFactoryRuntimeType(Module module, EntityHandle handle) =>
        IsGenericFactoryCoreDefinition(module, handle, "System", "RuntimeType")
        && IsGenericFactoryCoreDefinition(module,
            module.Reader.GetTypeDefinition((TypeDefinitionHandle)handle).BaseType, "System.Reflection", "TypeInfo");

    internal static bool IsRuntimeTypeGenericFactoryDecodedSignature(MethodSignature<TypeDesc> signature, Module module)
    {
        if (signature.Header.RawValue != 0 || signature.GenericParameterCount != 0
            || signature.RequiredParameterCount != signature.ParameterTypes.Length
            || signature.ParameterTypes.Length is not (2 or 3)
            || signature.ReturnType is not { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Object, IsCanonPlaceholder: false })
            return false;
        foreach (var parameter in signature.ParameterTypes)
            if (parameter is not { Kind: TypeKind.Class, IsCanonPlaceholder: false, Class: { } cls }
                || cls.Module != module || cls.FullName != "System.RuntimeType" || cls.GenericArity != 0
                || cls.IsValueType || cls.IsEnum || cls.IsInterface || cls.IsByRefLike
                || cls.Context.TypeArgs.Length != 0 || cls.Context.MethodArgs.Length != 0
                || !IsGenericFactoryRuntimeType(module, cls.Handle))
                return false;
        return true;
    }
}
