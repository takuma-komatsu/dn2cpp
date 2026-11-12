using System.Reflection.Metadata;

namespace Dn2Cpp;

internal static partial class CoreIntrinsics
{
    internal static bool IsRangeThrowManagedBody(MethodInfo method)
    {
        var owner = method.DeclaringClass;
        if (!RangeThrowManagedBodyContract.IsName(owner.FullName, method.Name)
            || method.Module != owner.Module || owner.Handle.IsNil || method.Handle.IsNil
            || method.IsSynthetic || method.PInvoke is not null || owner.IsValueType || owner.IsInterface
            || owner.GenericArity != 0 || owner.Context.TypeArgs.Length != 0 || owner.Context.MethodArgs.Length != 0
            || method.Context.TypeArgs.Length != 0 || method.Context.MethodArgs is not [var argument]
            || !IsRangeThrowManagedArgument(argument)
            || !RangeThrowManagedBodyContract.IsDefinition(method.Module.Reader, method.Handle))
            return false;
        var definition = method.Module.Reader.GetMethodDefinition(method.Handle);
        return definition.GetDeclaringType() == owner.Handle && definition.Attributes == method.Attributes
            && definition.ImplAttributes == method.ImplAttributes && definition.RelativeVirtualAddress == method.Rva
            && IsRangeThrowManagedSignature(method.Signature, argument);
    }

    internal static bool IsRangeThrowManagedSignature(MethodSignature<TypeDesc> signature, TypeDesc argument) =>
        signature.Header.RawValue == 0x10 && signature.GenericParameterCount == 1
        && signature.RequiredParameterCount == 4 && signature.ParameterTypes.Length == 4
        && signature.ReturnType is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Void }
        && signature.ParameterTypes[0] is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.String }
        && SameRangeThrowArgument(signature.ParameterTypes[1], argument)
        && SameRangeThrowArgument(signature.ParameterTypes[2], argument)
        && SameRangeThrowArgument(signature.ParameterTypes[3], argument);

    internal static bool IsRangeThrowManagedArgument(TypeDesc argument) => argument.Kind switch
    {
        TypeKind.Primitive => argument.Primitive is not (PrimitiveTypeCode.Void or PrimitiveTypeCode.TypedReference),
        TypeKind.Class => argument.Class is { IsByRefLike: false },
        TypeKind.GenericVar => true,
        TypeKind.SZArray or TypeKind.MDArray => argument.Element is { } element && IsRangeThrowManagedArgument(element),
        _ => false,
    };

    internal static bool SameRangeThrowArgument(TypeDesc left, TypeDesc right)
    {
        if (ReferenceEquals(left, right))
            return true;
        if (left.Kind != right.Kind || left.IsCanonPlaceholder != right.IsCanonPlaceholder
            || left.CanonAnyIndex != right.CanonAnyIndex)
            return false;
        return left.Kind switch
        {
            TypeKind.Primitive => left.Primitive == right.Primitive,
            TypeKind.Class => left.Class == right.Class,
            TypeKind.GenericVar => left.GenVarIndex == right.GenVarIndex && left.GenVarIsMethod == right.GenVarIsMethod,
            TypeKind.SZArray or TypeKind.MDArray => left.Rank == right.Rank
                && left.Element is { } le && right.Element is { } re && SameRangeThrowArgument(le, re),
            _ => false,
        };
    }
}
