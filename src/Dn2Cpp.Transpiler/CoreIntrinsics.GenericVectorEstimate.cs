using System.Reflection;
using System.Reflection.Metadata;
using SRME = System.Reflection.Metadata.Ecma335.MetadataTokens;

namespace Dn2Cpp;

internal static partial class CoreIntrinsics
{
    internal static bool IsGenericVectorEstimateDefinition(ClassInfo owner, Module module, MethodDefinitionHandle handle)
    {
        if (owner.FullName != "System.Numerics.Vector" || !IsVectorEstimateOwner(owner)
            || module != owner.Module || handle.IsNil) return false;
        var reader = module.Reader;
        var definition = reader.GetMethodDefinition(handle);
        if (definition.GetDeclaringType() != owner.Handle || !reader.StringComparer.Equals(definition.Name, "MultiplyAddEstimate")
            || definition.Attributes != (MethodAttributes.Assembly | MethodAttributes.Static | MethodAttributes.HideBySig)
            || definition.ImplAttributes != MethodImplAttributes.AggressiveInlining || definition.RelativeVirtualAddress == 0
            || definition.GetGenericParameters().Count != 1) return false;
        foreach (var parameterHandle in definition.GetGenericParameters())
        {
            var parameter = reader.GetGenericParameter(parameterHandle);
            if (parameter.Parent != handle || parameter.Index != 0 || parameter.Attributes != GenericParameterAttributes.None
                || parameter.GetConstraints().Count != 0) return false;
        }
        if (!ReadGenericVectorEstimateSignature(reader.GetBlobReader(definition.Signature), out int token)
            || (token & 3) != 0) return false;
        var vector = reader.GetTypeDefinition(SRME.TypeDefinitionHandle(token >> 2));
        return vector.GetDeclaringType().IsNil && vector.GetGenericParameters().Count == 1
            && reader.StringComparer.Equals(vector.Namespace, "System.Numerics")
            && reader.StringComparer.Equals(vector.Name, "Vector`1");
    }

    internal static bool IsGenericVectorEstimate(MethodInfo method)
    {
        if (method.Name != "MultiplyAddEstimate" || method.IsSynthetic || method.PInvoke is not null
            || method.Context.TypeArgs.Length != 0 || method.Context.MethodArgs is not [var element]
            || !IsVectorManagedValueElement(element)
            || !IsGenericVectorEstimateDefinition(method.DeclaringClass, method.Module, method.Handle)) return false;
        var definition = method.Module.Reader.GetMethodDefinition(method.Handle);
        if (method.Attributes != definition.Attributes || method.ImplAttributes != definition.ImplAttributes
            || method.Rva != definition.RelativeVirtualAddress) return false;
        var signature = method.Signature;
        if (signature.Header.RawValue != 0x10 || signature.GenericParameterCount != 1
            || signature.ParameterTypes.Length != 3 || signature.RequiredParameterCount != 3
            || signature.ReturnType is not { Kind: TypeKind.Class, IsCanonPlaceholder: false, Class: { } vector }
            || !IsVectorManagedValueClass(vector) || vector.Module != method.Module
            || !IsVectorExplicitOwner(vector) || vector.Context.TypeArgs is not [var actual]
            || actual.Kind != TypeKind.Class || actual.IsCanonPlaceholder || actual.Class != element.Class) return false;
        if (!ReadGenericVectorEstimateSignature(method.Module.Reader.GetBlobReader(definition.Signature), out int token)
            || token != SRME.GetRowNumber(vector.Handle) << 2) return false;
        foreach (var parameter in signature.ParameterTypes)
            if (parameter is not { Kind: TypeKind.Class, IsCanonPlaceholder: false, Class: { } operand } || operand != vector) return false;
        return true;
    }

    internal static bool ReadGenericVectorEstimateSignature(BlobReader blob, out int vectorToken)
    {
        vectorToken = 0;
        if (blob.RemainingBytes < 3 || blob.ReadByte() != 0x10 || blob.ReadByte() != 1 || blob.ReadByte() != 3) return false;
        for (int i = 0; i < 4; i++)
        {
            if (blob.RemainingBytes < 6 || blob.ReadByte() != 0x15 || blob.ReadByte() != 0x11
                || !blob.TryReadCompressedInteger(out int token) || token >> 2 == 0 || (token & 3) > 1
                || blob.RemainingBytes < 3 || blob.ReadByte() != 1 || blob.ReadByte() != 0x1e || blob.ReadByte() != 0
                || i != 0 && token != vectorToken) return false;
            vectorToken = token;
        }
        return blob.RemainingBytes == 0;
    }
}
