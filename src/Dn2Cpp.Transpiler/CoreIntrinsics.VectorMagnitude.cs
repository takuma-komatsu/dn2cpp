using System.Reflection;
using System.Reflection.Metadata;
using SRME = System.Reflection.Metadata.Ecma335.MetadataTokens;

namespace Dn2Cpp;

internal static partial class CoreIntrinsics
{
    internal static bool IsVectorMagnitudeName(string name) => name is
        "MaxMagnitude" or "MinMagnitude" or "MaxMagnitudeNumber" or "MinMagnitudeNumber";

    internal static bool IsVectorMagnitudeElement(TypeDesc element) => element is
        { Kind: TypeKind.Primitive, IsCanonPlaceholder: false, Primitive: PrimitiveTypeCode.Single or PrimitiveTypeCode.Double };

    internal static void ValidateVectorMagnitude(MethodInfo method)
    {
        if (IsVectorMagnitudeName(method.Name) && IsVectorEstimateOwnerName(method.DeclaringClass.FullName)
            && !IsVectorMagnitude(method))
            throw new NotSupportedException($"Vector {method.Name} requires an exact floating-point generic CoreLib declaration");
    }

    internal static bool IsVectorMagnitudeDefinition(ClassInfo owner, Module module, MethodDefinitionHandle handle)
    {
        if (!IsVectorEstimateOwner(owner)
            || module != owner.Module || handle.IsNil) return false;
        var reader = module.Reader;
        var definition = reader.GetMethodDefinition(handle);
        if (definition.GetDeclaringType() != owner.Handle || !IsVectorMagnitudeName(reader.GetString(definition.Name))
            || definition.Attributes != (MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig)
            || definition.ImplAttributes != MethodImplAttributes.AggressiveInlining || definition.RelativeVirtualAddress == 0
            || definition.GetGenericParameters().Count != 1) return false;
        foreach (var parameterHandle in definition.GetGenericParameters())
        {
            var parameter = reader.GetGenericParameter(parameterHandle);
            if (parameter.Parent != handle || parameter.Index != 0 || parameter.Attributes != GenericParameterAttributes.None
                || parameter.GetConstraints().Count != 0) return false;
        }
        if (!ReadVectorMagnitudeSignature(reader.GetBlobReader(definition.Signature), out int token)
            || (token & 3) != 0) return false;
        var vector = reader.GetTypeDefinition(SRME.TypeDefinitionHandle(token >> 2));
        return vector.GetDeclaringType().IsNil && vector.GetGenericParameters().Count == 1
            && reader.StringComparer.Equals(vector.Namespace, owner.FullName == "System.Numerics.Vector"
                ? "System.Numerics" : "System.Runtime.Intrinsics")
            && reader.StringComparer.Equals(vector.Name, VectorMagnitudeValueName(owner));
    }

    internal static bool IsVectorMagnitude(MethodInfo method)
    {
        if (!IsVectorMagnitudeName(method.Name) || method.IsSynthetic || method.PInvoke is not null
            || method.Context.TypeArgs.Length != 0 || method.Context.MethodArgs is not [var element]
            || !IsVectorMagnitudeElement(element)
            || !IsVectorMagnitudeDefinition(method.DeclaringClass, method.Module, method.Handle)) return false;
        var definition = method.Module.Reader.GetMethodDefinition(method.Handle);
        if (!method.Module.Reader.StringComparer.Equals(definition.Name, method.Name)
            || method.Attributes != definition.Attributes || method.ImplAttributes != definition.ImplAttributes
            || method.Rva != definition.RelativeVirtualAddress) return false;
        var signature = method.Signature;
        if (signature.Header.RawValue != 0x10 || signature.GenericParameterCount != 1
            || signature.ParameterTypes.Length != 2 || signature.RequiredParameterCount != 2
            || signature.ReturnType is not { Kind: TypeKind.Class, IsCanonPlaceholder: false, Class: { } vector }
            || !IsVectorValueClass(vector) || vector.IsByRefLike || vector.Module != method.Module
            || vector.IntrinsicCppName != method.DeclaringClass.IntrinsicCppName
            || vector.Context.TypeArgs is not [var actual]
            || !IsVectorMagnitudeElement(actual) || actual.Primitive != element.Primitive) return false;
        if (!ReadVectorMagnitudeSignature(method.Module.Reader.GetBlobReader(definition.Signature), out int token)
            || token != SRME.GetRowNumber(vector.Handle) << 2) return false;
        foreach (var parameter in signature.ParameterTypes)
            if (parameter is not { Kind: TypeKind.Class, IsCanonPlaceholder: false, Class: { } operand } || operand != vector) return false;
        return true;
    }

    private static string VectorMagnitudeValueName(ClassInfo owner) => owner.FullName switch
    {
        "System.Numerics.Vector" => "Vector`1",
        "System.Runtime.Intrinsics.Vector64" => "Vector64`1",
        "System.Runtime.Intrinsics.Vector128" => "Vector128`1",
        "System.Runtime.Intrinsics.Vector256" => "Vector256`1",
        "System.Runtime.Intrinsics.Vector512" => "Vector512`1",
        _ => "",
    };

    internal static bool ReadVectorMagnitudeSignature(BlobReader blob, out int vectorToken)
    {
        vectorToken = 0;
        if (blob.RemainingBytes < 3 || blob.ReadByte() != 0x10 || blob.ReadByte() != 1 || blob.ReadByte() != 2) return false;
        for (int i = 0; i < 3; i++)
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
