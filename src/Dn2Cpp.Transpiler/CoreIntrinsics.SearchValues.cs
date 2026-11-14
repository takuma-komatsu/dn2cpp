using System.Reflection;
using System.Reflection.Metadata;
using SRME = System.Reflection.Metadata.Ecma335.MetadataTokens;

namespace Dn2Cpp;

internal static partial class CoreIntrinsics
{
    // The real body preserves the different SSSE3 and portable out-of-range rules.
    internal static bool IsSearchValuesShuffle(MethodInfo method)
    {
        var owner = method.DeclaringClass;
        if (method.Module.AssemblyName != "System.Private.CoreLib" || method.Module != owner.Module
            || owner.FullName != "System.Buffers.SearchValues" || method.Name != "ShuffleNativeModified"
            || method.IsSynthetic || owner.Handle.IsNil || method.Handle.IsNil
            || owner.IsValueType || owner.IsInterface || owner.IsEnum || owner.IsByRefLike
            || owner.GenericArity != 0 || owner.Context.TypeArgs.Length != 0 || owner.Context.MethodArgs.Length != 0
            || method.Context.TypeArgs.Length != 0 || method.Context.MethodArgs.Length != 0
            || method.Rva == 0 || method.PInvoke is not null)
            return false;
        var reader = method.Module.Reader;
        var definition = reader.GetMethodDefinition(method.Handle);
        var type = reader.GetTypeDefinition(owner.Handle);
        return CoreIntrinsicTypeMaps.IsSearchValuesShuffleMethod(method.Module.AssemblyName, owner.FullName, method.Name,
            type.GetDeclaringType().IsNil && type.GetGenericParameters().Count == 0
                && (type.Attributes & (TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.Interface))
                    == (TypeAttributes.Abstract | TypeAttributes.Sealed)
                && reader.StringComparer.Equals(type.Namespace, "System.Buffers")
                && reader.StringComparer.Equals(type.Name, "SearchValues")
                && definition.GetDeclaringType() == owner.Handle && definition.GetGenericParameters().Count == 0
                && reader.StringComparer.Equals(definition.Name, method.Name)
                && method.Attributes == (MethodAttributes.Assembly | MethodAttributes.Static | MethodAttributes.HideBySig)
                && method.ImplAttributes == MethodImplAttributes.AggressiveInlining
                && definition.Attributes == method.Attributes && definition.ImplAttributes == method.ImplAttributes
                && definition.RelativeVirtualAddress == method.Rva,
            IsSearchValuesShuffleSignature(method.Module, reader.GetBlobReader(definition.Signature))
                && (!method.SignatureReady || IsSearchValuesShuffleDecodedSignature(method.Signature, method.Module)));
    }

    internal static bool IsSearchValuesShuffleSignature(Module module, BlobReader blob) =>
        TryReadSearchValuesShuffleSignature(module, blob, out var result, out var first, out var second)
        && result.Kind == HandleKind.TypeDefinition && result == first && result == second
        && IsSearchValuesShuffleVectorDefinition(module, (TypeDefinitionHandle)result);

    // Reference authority is resolved by Compilation, never by this predicate.
    internal static bool TryReadSearchValuesShuffleSignature(Module module, BlobReader blob,
        out EntityHandle result, out EntityHandle first, out EntityHandle second)
    {
        result = first = second = default;
        return blob.RemainingBytes >= 2 && blob.ReadByte() == 0 && blob.ReadByte() == 2
            && TryReadSearchValuesShuffleVector(module, ref blob, out result)
            && TryReadSearchValuesShuffleVector(module, ref blob, out first)
            && TryReadSearchValuesShuffleVector(module, ref blob, out second)
            && blob.RemainingBytes == 0;
    }

    private static bool TryReadSearchValuesShuffleVector(Module module, ref BlobReader blob, out EntityHandle handle)
    {
        handle = default;
        if (blob.RemainingBytes < 5 || blob.ReadByte() != 0x15 || blob.ReadByte() != 0x11
            || !blob.TryReadCompressedInteger(out int token) || token >> 2 == 0
            || blob.RemainingBytes < 2 || blob.ReadByte() != 1 || blob.ReadByte() != 0x05)
            return false;
        var reader = module.Reader;
        StringHandle space, name;
        if ((token & 3) == 0)
        {
            if (token >> 2 > reader.TypeDefinitions.Count) return false;
            var definitionHandle = SRME.TypeDefinitionHandle(token >> 2);
            var definition = reader.GetTypeDefinition(definitionHandle);
            if (!definition.GetDeclaringType().IsNil) return false;
            handle = definitionHandle;
            space = definition.Namespace; name = definition.Name;
        }
        else if ((token & 3) == 1)
        {
            if (token >> 2 > reader.TypeReferences.Count) return false;
            var referenceHandle = SRME.TypeReferenceHandle(token >> 2);
            var reference = reader.GetTypeReference(referenceHandle);
            if (reference.ResolutionScope.Kind != HandleKind.AssemblyReference || reference.ResolutionScope.IsNil
                || SRME.GetRowNumber(reference.ResolutionScope) > reader.AssemblyReferences.Count)
                return false;
            handle = referenceHandle;
            space = reference.Namespace; name = reference.Name;
        }
        else return false;
        return reader.StringComparer.Equals(space, "System.Runtime.Intrinsics")
            && reader.StringComparer.Equals(name, "Vector128`1");
    }

    private static bool IsSearchValuesShuffleVectorDefinition(Module module, TypeDefinitionHandle handle)
    {
        if (module.AssemblyName != "System.Private.CoreLib" || handle.IsNil) return false;
        var reader = module.Reader;
        var definition = reader.GetTypeDefinition(handle);
        if (!definition.GetDeclaringType().IsNil || definition.GetGenericParameters().Count != 1
            || !reader.StringComparer.Equals(definition.Namespace, "System.Runtime.Intrinsics")
            || !reader.StringComparer.Equals(definition.Name, "Vector128`1")) return false;
        foreach (var parameterHandle in definition.GetGenericParameters())
        {
            var parameter = reader.GetGenericParameter(parameterHandle);
            if (parameter.Parent != handle || parameter.Index != 0 || parameter.Attributes != GenericParameterAttributes.None
                || parameter.GetConstraints().Count != 0) return false;
        }
        return true;
    }

    internal static bool IsSearchValuesShuffleDecodedSignature(MethodSignature<TypeDesc> signature, Module module) =>
        signature.Header.RawValue == 0 && signature.GenericParameterCount == 0 && signature.RequiredParameterCount == 2
        && signature.ParameterTypes is [var first, var second]
        && IsSearchValuesShuffleVector(signature.ReturnType, module)
        && IsSearchValuesShuffleVector(first, module) && IsSearchValuesShuffleVector(second, module);

    private static bool IsSearchValuesShuffleVector(TypeDesc type, Module module) =>
        type is { Kind: TypeKind.Class, IsCanonPlaceholder: false, Class: { } vector }
        && vector.Module == module && vector.IntrinsicCppName == "Dn2CppVector128"
        && vector.IsValueType && !vector.IsEnum && !vector.IsInterface && !vector.IsByRefLike && vector.GenericArity == 1
        && vector.Context.MethodArgs.Length == 0
        && vector.Context.TypeArgs is [{ Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Byte, IsCanonPlaceholder: false }]
        && IsSearchValuesShuffleVectorDefinition(module, vector.Handle);
}
