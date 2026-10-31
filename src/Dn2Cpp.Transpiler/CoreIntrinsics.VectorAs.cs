using System.Reflection;
using System.Reflection.Metadata;
using SRME = System.Reflection.Metadata.Ecma335.MetadataTokens;

namespace Dn2Cpp;

internal static partial class CoreIntrinsics
{
    internal static bool IsVectorAsDefinition(ClassInfo owner, Module module, MethodDefinitionHandle handle)
    {
        if (owner.FullName != "System.Numerics.Vector" || !IsVectorEstimateOwner(owner)
            || module != owner.Module || handle.IsNil) return false;
        var reader = module.Reader;
        var definition = reader.GetMethodDefinition(handle);
        if (definition.GetDeclaringType() != owner.Handle || !reader.StringComparer.Equals(definition.Name, "As")
            || definition.Attributes != (MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig)
            || definition.ImplAttributes != MethodImplAttributes.AggressiveInlining || definition.RelativeVirtualAddress == 0
            || definition.GetGenericParameters().Count != 2) return false;
        int index = 0;
        foreach (var parameterHandle in definition.GetGenericParameters())
        {
            var parameter = reader.GetGenericParameter(parameterHandle);
            if (parameter.Parent != handle || parameter.Index != index++
                || parameter.Attributes != GenericParameterAttributes.None || parameter.GetConstraints().Count != 0)
                return false;
        }
        if (!ReadVectorAsSignature(reader.GetBlobReader(definition.Signature), out int token) || (token & 3) != 0)
            return false;
        var vector = reader.GetTypeDefinition(SRME.TypeDefinitionHandle(token >> 2));
        if (!vector.GetDeclaringType().IsNil || vector.GetGenericParameters().Count != 1
            || !reader.StringComparer.Equals(vector.Namespace, "System.Numerics")
            || !reader.StringComparer.Equals(vector.Name, "Vector`1")) return false;
        foreach (var parameterHandle in vector.GetGenericParameters())
        {
            var parameter = reader.GetGenericParameter(parameterHandle);
            if (parameter.Parent != SRME.TypeDefinitionHandle(token >> 2) || parameter.Index != 0
                || parameter.Attributes != GenericParameterAttributes.None || parameter.GetConstraints().Count != 0)
                return false;
        }
        return true;
    }

    internal static bool IsVectorAs(MethodInfo method)
    {
        if (method.Name != "As" || method.IsSynthetic || method.PInvoke is not null
            || method.Context.TypeArgs.Length != 0 || method.Context.MethodArgs is not [var source, var destination]
            || !IsVectorAsElement(source) || !IsVectorAsElement(destination)
            || !IsVectorAsDefinition(method.DeclaringClass, method.Module, method.Handle)) return false;
        var definition = method.Module.Reader.GetMethodDefinition(method.Handle);
        if (method.Attributes != definition.Attributes || method.ImplAttributes != definition.ImplAttributes
            || method.Rva != definition.RelativeVirtualAddress) return false;
        var signature = method.Signature;
        if (signature.Header.RawValue != 0x10 || signature.GenericParameterCount != 2
            || signature.RequiredParameterCount != 1 || signature.ParameterTypes is not [var argument]
            || !ReadVectorAsSignature(method.Module.Reader.GetBlobReader(definition.Signature), out int token))
            return false;
        return IsVectorAsClosure(argument, source, method.Module, token)
            && IsVectorAsClosure(signature.ReturnType, destination, method.Module, token);
    }

    internal static void ValidateVectorAsCallable(MethodInfo method)
    {
        if (method.Name == "As" && method.DeclaringClass.FullName == "System.Numerics.Vector"
            && !method.Handle.IsNil && !IsVectorAs(method))
            throw new NotSupportedException("Vector.As requires its exact closed CoreLib declaration");
    }

    internal static bool IsGuardedVectorAs(MethodInfo method) => IsVectorAs(method)
        && (IsVectorManagedValueElement(method.Context.MethodArgs[0])
            || IsVectorManagedValueElement(method.Context.MethodArgs[1]));

    internal static bool IsVectorAsElement(TypeDesc element) =>
        IsVectorNumericElement(element) || IsVectorManagedValueElement(element);

    private static bool IsVectorAsClosure(TypeDesc type, TypeDesc element, Module module, int token)
    {
        if (type is not { Kind: TypeKind.Class, IsCanonPlaceholder: false, Class: { } vector }
            || vector.Module != module || vector.Handle != SRME.TypeDefinitionHandle(token >> 2)
            || vector.IsByRefLike || vector.Context.TypeArgs is not [var actual] || !IsVectorExplicitOwner(vector)
            || !IsVectorValueCallableClass(vector)) return false;
        return actual.Kind == element.Kind && !actual.IsCanonPlaceholder
            && (element.Kind == TypeKind.Primitive ? actual.Primitive == element.Primitive : actual.Class == element.Class);
    }

    internal static bool ReadVectorAsSignature(BlobReader blob, out int vectorToken)
    {
        vectorToken = 0;
        if (blob.RemainingBytes < 3 || blob.ReadByte() != 0x10 || blob.ReadByte() != 2 || blob.ReadByte() != 1)
            return false;
        for (int index = 1; index >= 0; index--)
        {
            if (blob.RemainingBytes < 6 || blob.ReadByte() != 0x15 || blob.ReadByte() != 0x11
                || !blob.TryReadCompressedInteger(out int token) || token >> 2 == 0 || (token & 3) > 1
                || blob.RemainingBytes < 3 || blob.ReadByte() != 1 || blob.ReadByte() != 0x1e
                || blob.ReadByte() != index || index == 0 && token != vectorToken) return false;
            vectorToken = token;
        }
        return blob.RemainingBytes == 0;
    }
}
