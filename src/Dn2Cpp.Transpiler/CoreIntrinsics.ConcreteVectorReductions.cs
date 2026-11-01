using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace Dn2Cpp;

internal static partial class CoreIntrinsics
{
    internal static bool IsConcreteVectorReductionName(string name) => name is
        "All" or "Any" or "None" or "Count" or "IndexOf" or "LastIndexOf"
        or "AllWhereAllBitsSet" or "AnyWhereAllBitsSet" or "NoneWhereAllBitsSet"
        or "CountWhereAllBitsSet" or "IndexOfWhereAllBitsSet" or "LastIndexOfWhereAllBitsSet";

    private static bool ConcreteVectorReductionReturnsBoolean(string name) => name is
        "All" or "Any" or "None" or "AllWhereAllBitsSet" or "AnyWhereAllBitsSet" or "NoneWhereAllBitsSet";

    internal static bool IsConcreteVectorReductionCandidate(MethodSignature<TypeDesc> signature)
    {
        if (signature.GenericParameterCount == 0) return true;
        if (IsConcreteVectorReductionOperand(signature.ReturnType)) return true;
        foreach (var operand in signature.ParameterTypes)
            if (IsConcreteVectorReductionOperand(operand)) return true;
        return false;
    }

    private static bool IsConcreteVectorReductionOperand(TypeDesc operand)
    {
        while (operand.Element is { } element) operand = element;
        return operand.Class?.FullName is "System.Numerics.Vector2" or "System.Numerics.Vector3";
    }

    internal static bool IsConcreteVectorReductionSignature(string name, MethodSignature<TypeDesc> signature, Module module)
    {
        int count = name.EndsWith("WhereAllBitsSet", StringComparison.Ordinal) ? 1 : 2;
        if (!IsConcreteVectorReductionName(name) || signature.Header.RawValue != 0 || signature.GenericParameterCount != 0
            || signature.RequiredParameterCount != count || signature.ParameterTypes.Length != count
            || signature.ParameterTypes[0] is not { Kind: TypeKind.Class, IsCanonPlaceholder: false, Class: { } vector }
            || ConcreteVectorElementCount(vector, module) is not (2 or 3)
            || signature.ReturnType is not { Kind: TypeKind.Primitive, IsCanonPlaceholder: false } result
            || result.Primitive != (ConcreteVectorReductionReturnsBoolean(name) ? PrimitiveTypeCode.Boolean : PrimitiveTypeCode.Int32)) return false;
        return count == 1 || signature.ParameterTypes[1] is
            { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Single, IsCanonPlaceholder: false };
    }

    internal static bool IsConcreteVectorReductionRawSignature(string name, BlobReader blob, int expectedToken = 0)
    {
        int count = name.EndsWith("WhereAllBitsSet", StringComparison.Ordinal) ? 1 : 2;
        if (!IsConcreteVectorReductionName(name) || blob.RemainingBytes < 5 || blob.ReadByte() != 0
            || blob.ReadByte() != count || blob.ReadByte() != (ConcreteVectorReductionReturnsBoolean(name) ? 2 : 8)
            || !ReadConcreteVectorToken(ref blob, out int token) || expectedToken != 0 && token != expectedToken
            || blob.RemainingBytes != count - 1) return false;
        return count == 1 || blob.ReadByte() == 0x0c;
    }

    // These helpers' real IL owns zero padding and all-bits-set masks; a generic
    // four-lane reduction is not a replacement for their concrete-width semantics.
    internal static bool IsConcreteVectorReduction(MethodInfo method)
    {
        var owner = method.DeclaringClass;
        if (!IsConcreteVectorReductionName(method.Name) || owner.FullName != "System.Runtime.Intrinsics.Vector128"
            || !IsVectorEstimateOwner(owner) || method.Module != owner.Module || method.Handle.IsNil || method.IsSynthetic
            || method.Context.TypeArgs.Length != 0 || method.Context.MethodArgs.Length != 0 || method.Rva == 0 || method.PInvoke is not null
            || method.Attributes != (MethodAttributes.Assembly | MethodAttributes.Static | MethodAttributes.HideBySig)
            || method.ImplAttributes != MethodImplAttributes.AggressiveInlining
            || !IsConcreteVectorReductionSignature(method.Name, method.Signature, owner.Module)) return false;
        var reader = method.Module.Reader;
        var definition = reader.GetMethodDefinition(method.Handle);
        return definition.GetDeclaringType() == owner.Handle && definition.GetGenericParameters().Count == 0
            && definition.Attributes == method.Attributes && definition.ImplAttributes == method.ImplAttributes
            && definition.RelativeVirtualAddress == method.Rva && reader.StringComparer.Equals(definition.Name, method.Name)
            && IsConcreteVectorReductionRawSignature(method.Name, reader.GetBlobReader(definition.Signature),
                MetadataTokens.GetRowNumber(method.Signature.ParameterTypes[0].Class!.Handle) << 2);
    }

    internal static void ValidateConcreteVectorReduction(MethodInfo method)
    {
        if (IsConcreteVectorReductionName(method.Name) && method.DeclaringClass.FullName == "System.Runtime.Intrinsics.Vector128"
            && IsConcreteVectorReductionCandidate(method.Signature) && !IsConcreteVectorReduction(method))
            throw new NotSupportedException("Concrete vector reduction requires its exact CoreLib declaration");
    }
}
