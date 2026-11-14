using System.Reflection;
using System.Reflection.Metadata;
using SRME = System.Reflection.Metadata.Ecma335.MetadataTokens;

namespace Dn2Cpp;

internal static partial class CoreIntrinsics
{
    // This helper only dispatches delegates; it never reads the managed CTS fields.
    internal static bool IsCancellationInvoke(MethodInfo method)
    {
        var owner = method.DeclaringClass;
        if (method.Name != "Invoke" || owner.FullName != "System.Threading.CancellationTokenSource"
            || method.Module.AssemblyName != "System.Private.CoreLib" || method.Module != owner.Module
            || method.IsSynthetic || method.Handle.IsNil || owner.Handle.IsNil
            || owner.IsValueType || owner.IsInterface || owner.IsEnum || owner.IsByRefLike || owner.GenericArity != 0
            || owner.Context.TypeArgs.Length != 0 || owner.Context.MethodArgs.Length != 0
            || method.Context.TypeArgs.Length != 0 || method.Context.MethodArgs.Length != 0
            || method.Rva == 0 || method.PInvoke is not null)
            return false;
        var reader = method.Module.Reader;
        var definition = reader.GetMethodDefinition(method.Handle);
        return CoreIntrinsicTypeMaps.IsCancellationInvokeMethod(method.Module.AssemblyName, owner.FullName, method.Name,
            IsCancellationClassDefinition(method.Module, owner.Handle, "System.Threading", "CancellationTokenSource")
                && definition.GetDeclaringType() == owner.Handle && definition.GetGenericParameters().Count == 0
                && reader.StringComparer.Equals(definition.Name, method.Name)
                && method.Attributes == (MethodAttributes.Private | MethodAttributes.Static | MethodAttributes.HideBySig)
                && method.ImplAttributes == MethodImplAttributes.IL
                && definition.Attributes == method.Attributes && definition.ImplAttributes == method.ImplAttributes
                && definition.RelativeVirtualAddress == method.Rva,
            IsCancellationInvokeSignature(method.Module, reader.GetBlobReader(definition.Signature))
                && (!method.SignatureReady || IsCancellationInvokeDecodedSignature(method.Signature, method.Module)));
    }

    internal static bool IsCancellationInvokeSignature(Module module, BlobReader blob) =>
        TryReadCancellationInvokeSignature(module, blob, out var callback, out var source)
        && callback.Kind == HandleKind.TypeDefinition && source.Kind == HandleKind.TypeDefinition
        && IsCancellationClassDefinition(module, (TypeDefinitionHandle)callback, "System", "Delegate")
        && IsCancellationClassDefinition(module, (TypeDefinitionHandle)source, "System.Threading", "CancellationTokenSource");

    // The consumer authenticates reference handles through the normal resolver.
    internal static bool TryReadCancellationInvokeSignature(Module module, BlobReader blob,
        out EntityHandle callback, out EntityHandle source)
    {
        callback = source = default;
        return blob.RemainingBytes >= 3 && blob.ReadByte() == 0 && blob.ReadByte() == 3 && blob.ReadByte() == 1
            && TryReadCancellationClass(module, ref blob, "System", "Delegate", out callback)
            && blob.RemainingBytes != 0 && blob.ReadByte() == 0x1c
            && TryReadCancellationClass(module, ref blob, "System.Threading", "CancellationTokenSource", out source)
            && blob.RemainingBytes == 0;
    }

    private static bool TryReadCancellationClass(Module module, ref BlobReader blob, string space, string name, out EntityHandle handle)
    {
        handle = default;
        if (blob.RemainingBytes < 2 || blob.ReadByte() != 0x12
            || !blob.TryReadCompressedInteger(out int token) || token >> 2 == 0)
            return false;
        var reader = module.Reader;
        StringHandle actualSpace, actualName;
        if ((token & 3) == 0)
        {
            if (token >> 2 > reader.TypeDefinitions.Count) return false;
            var definitionHandle = SRME.TypeDefinitionHandle(token >> 2);
            var definition = reader.GetTypeDefinition(definitionHandle);
            if (!definition.GetDeclaringType().IsNil) return false;
            handle = definitionHandle; actualSpace = definition.Namespace; actualName = definition.Name;
        }
        else if ((token & 3) == 1)
        {
            if (token >> 2 > reader.TypeReferences.Count) return false;
            var referenceHandle = SRME.TypeReferenceHandle(token >> 2);
            var reference = reader.GetTypeReference(referenceHandle);
            if (reference.ResolutionScope.Kind != HandleKind.AssemblyReference || reference.ResolutionScope.IsNil
                || SRME.GetRowNumber(reference.ResolutionScope) > reader.AssemblyReferences.Count)
                return false;
            handle = referenceHandle; actualSpace = reference.Namespace; actualName = reference.Name;
        }
        else return false;
        return reader.StringComparer.Equals(actualSpace, space) && reader.StringComparer.Equals(actualName, name);
    }

    private static bool IsCancellationClassDefinition(Module module, TypeDefinitionHandle handle, string space, string name)
    {
        if (module.AssemblyName != "System.Private.CoreLib" || handle.IsNil) return false;
        var reader = module.Reader;
        var definition = reader.GetTypeDefinition(handle);
        return definition.GetDeclaringType().IsNil && definition.GetGenericParameters().Count == 0
            && (definition.Attributes & TypeAttributes.Interface) == 0
            && reader.StringComparer.Equals(definition.Namespace, space) && reader.StringComparer.Equals(definition.Name, name);
    }

    private static bool IsCancellationClass(TypeDesc type, Module module, string space, string name) =>
        type is { Kind: TypeKind.Class, IsCanonPlaceholder: false, Class: { } cls }
        && cls.Module == module && !cls.IsValueType && !cls.IsEnum && !cls.IsInterface && !cls.IsByRefLike
        && cls.GenericArity == 0 && cls.Context.TypeArgs.Length == 0 && cls.Context.MethodArgs.Length == 0
        && IsCancellationClassDefinition(module, cls.Handle, space, name);

    internal static bool IsCancellationInvokeDecodedSignature(MethodSignature<TypeDesc> signature, Module module) =>
        signature.Header.RawValue == 0 && signature.GenericParameterCount == 0 && signature.RequiredParameterCount == 3
        && signature.ReturnType is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Void, IsCanonPlaceholder: false }
        && signature.ParameterTypes is [var callback, { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Object, IsCanonPlaceholder: false }, var source]
        && IsCancellationClass(callback, module, "System", "Delegate")
        && IsCancellationClass(source, module, "System.Threading", "CancellationTokenSource");

    internal static int CancellationTokenConstructorKind(MethodSignature<TypeDesc> signature)
    {
        if (signature.Header.RawValue != 0x20 || signature.GenericParameterCount != 0 || signature.RequiredParameterCount != 1
            || signature.ReturnType is not { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Void, IsCanonPlaceholder: false }
            || signature.ParameterTypes is not [var parameter])
            return 0;
        if (parameter is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Boolean, IsCanonPlaceholder: false })
            return 1;
        return parameter.Class is { } source && IsCancellationClass(parameter, source.Module, "System.Threading", "CancellationTokenSource") ? 2 : 0;
    }
}
