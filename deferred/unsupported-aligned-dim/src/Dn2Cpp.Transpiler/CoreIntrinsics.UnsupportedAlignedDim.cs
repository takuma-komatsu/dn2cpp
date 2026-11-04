using System.Reflection.Metadata;

namespace Dn2Cpp;

internal static partial class CoreIntrinsics
{
    internal static bool IsUnsupportedAlignedDimCandidate(MethodInfo method)
    {
        var owner = method.DeclaringClass;
        if (owner.Module.AssemblyName != "System.Private.CoreLib" || owner.Handle.IsNil) return false;
        var reader = owner.Module.Reader;
        var definition = reader.GetTypeDefinition(owner.Handle);
        return reader.StringComparer.Equals(definition.Namespace, "System.Runtime.Intrinsics")
            && reader.StringComparer.Equals(definition.Name, "ISimdVector`2")
            && (method.Name is "LoadAligned" or "StoreAligned"
                || !method.Handle.IsNil && (method.Module.Reader.StringComparer.Equals(
                    method.Module.Reader.GetMethodDefinition(method.Handle).Name, "LoadAligned")
                    || method.Module.Reader.StringComparer.Equals(
                        method.Module.Reader.GetMethodDefinition(method.Handle).Name, "StoreAligned")));
    }

    internal static bool IsUnsupportedAlignedDimVector(ClassInfo vector) =>
        vector.Context.TypeArgs is [var element] && vector.Context.MethodArgs.Length == 0
        && (IsVectorExplicitOwner(vector) ? IsUnsupportedAlignedDimValue(element, "Decimal")
            : IsFixedVectorValueOwner(vector) && (IsUnsupportedAlignedDimChar(element)
                || IsUnsupportedAlignedDimValue(element, "Int128") || IsUnsupportedAlignedDimValue(element, "UInt128")));

    internal static bool IsUnsupportedAlignedDimChar(TypeDesc element) =>
        element is { Kind: TypeKind.Primitive, IsCanonPlaceholder: false, Primitive: PrimitiveTypeCode.Char, Class: null };

    private static bool IsUnsupportedAlignedDimValue(TypeDesc element, string name)
    {
        if (!IsVectorManagedValueElement(element) || element.Class is not { } value
            || value.Module.AssemblyName != "System.Private.CoreLib" || value.IsEnum
            || value.Context.MethodArgs.Length != 0) return false;
        var reader = value.Module.Reader;
        var definition = reader.GetTypeDefinition(value.Handle);
        return definition.GetDeclaringType().IsNil && definition.GetGenericParameters().Count == 0
            && reader.StringComparer.Equals(definition.Namespace, "System")
            && reader.StringComparer.Equals(definition.Name, name);
    }

    internal static int UnsupportedAlignedDimWidth(ClassInfo vector) =>
        IsVectorExplicitOwner(vector) ? 16 : FixedVectorValueOwnerWidth(vector);

    internal static bool IsUnsupportedAlignedDimThrowElement(string name, TypeDesc element) =>
        name == "ThrowForUnsupportedNumericsVectorBaseType" ? IsUnsupportedAlignedDimValue(element, "Decimal")
        : name is "ThrowForUnsupportedIntrinsicsVector64BaseType" or "ThrowForUnsupportedIntrinsicsVector128BaseType"
            or "ThrowForUnsupportedIntrinsicsVector256BaseType" or "ThrowForUnsupportedIntrinsicsVector512BaseType"
            && (IsUnsupportedAlignedDimChar(element) || IsUnsupportedAlignedDimValue(element, "Int128")
                || IsUnsupportedAlignedDimValue(element, "UInt128"));

    internal static bool SameUnsupportedAlignedDimElement(TypeDesc actual, TypeDesc expected) =>
        !actual.IsCanonPlaceholder && !expected.IsCanonPlaceholder && actual.Kind == expected.Kind
        && (IsUnsupportedAlignedDimChar(expected) ? IsUnsupportedAlignedDimChar(actual)
            : actual.Kind == TypeKind.Class && actual.Class is not null && actual.Class == expected.Class);
}
