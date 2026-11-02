using System.Reflection.Metadata;

namespace Dn2Cpp;

internal static partial class CoreIntrinsics
{
    internal static bool IsPrimitiveWideConversionTarget(TypeDesc type)
    {
        if (type is not { Kind: TypeKind.Class, IsCanonPlaceholder: false, Class: { } target }
            || !target.IsValueType || target.IsInterface || target.IsEnum || target.IsByRefLike
            || target.Handle.IsNil || target.GenericArity != 0 || target.Context.TypeArgs.Length != 0
            || target.Context.MethodArgs.Length != 0) return false;
        string assembly;
        string ns;
        string name;
        switch (target.FullName)
        {
            case "System.Int128": assembly = "System.Private.CoreLib"; ns = "System"; name = "Int128"; break;
            case "System.UInt128": assembly = "System.Private.CoreLib"; ns = "System"; name = "UInt128"; break;
            case "System.Numerics.BigInteger": assembly = "System.Runtime.Numerics"; ns = "System.Numerics"; name = "BigInteger"; break;
            default: return false;
        }
        var reader = target.Module.Reader;
        return target.Module.AssemblyName == assembly && reader.IsAssembly
            && reader.StringComparer.Equals(reader.GetAssemblyDefinition().Name, assembly)
            && BinaryIntegerEndianContract.IsNamedType(reader, target.Handle, ns, name)
            && reader.GetTypeDefinition(target.Handle).GetGenericParameters().Count == 0;
    }

    internal static bool IsPrimitiveWideConversionSlotFamily(MethodInfo method) =>
        method.Name is "TryConvertToChecked" or "TryConvertToTruncating"
        && method.Module.Owner.GenericDefFullName(method.DeclaringClass) == "System.Numerics.INumberBase"
        && method.DeclaringClass.Context.TypeArgs is [{ Kind: TypeKind.Primitive } source]
        && method.Context.MethodArgs is [var target] && IsPrimitiveWideConversionTarget(target)
        && PrimitiveWideConversionContract.IsPair(source.Primitive, method.Name, target.Class!.FullName);

    private static bool IsPrimitiveWideConversionBodyCandidate(MethodInfo method) =>
        PrimitiveWideConversionContract.IsBodyName(method.DeclaringClass.FullName, method.Name)
        && method.Context.MethodArgs is [var target] && IsPrimitiveWideConversionTarget(target)
        && PrimitiveWideConversionContract.IsPair(PrimitiveWideConversionContract.Source(method.DeclaringClass.FullName),
            PrimitiveWideConversionContract.Slot(PrimitiveWideConversionContract.Source(method.DeclaringClass.FullName)), target.Class!.FullName);

    internal static bool IsPrimitiveWideConversionSignature(MethodSignature<TypeDesc> signature, PrimitiveTypeCode source, TypeDesc target) =>
        signature.Header.RawValue == 0x10 && signature.GenericParameterCount == 1 && signature.RequiredParameterCount == 2
        && signature.ReturnType is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Boolean, IsCanonPlaceholder: false }
        && signature.ParameterTypes is [{ Kind: TypeKind.Primitive, IsCanonPlaceholder: false } value,
            { Kind: TypeKind.ByRef, IsCanonPlaceholder: false, Element: { Kind: TypeKind.Class, IsCanonPlaceholder: false } output }]
        && value.Primitive == source && output.Class == target.Class;

    internal static bool IsPrimitiveWideConversionBody(MethodInfo method)
    {
        if (!IsPrimitiveWideConversionBodyCandidate(method)) return false;
        var owner = method.DeclaringClass;
        if (!IsScalarNumericLeafOwner(owner) || method.Module != owner.Module || method.Handle.IsNil
            || method.IsSynthetic || method.PInvoke is not null || method.Context.TypeArgs.Length != 0
            || !IsPrimitiveWideConversionSignature(method.Signature, PrimitiveWideConversionContract.Source(owner.FullName), method.Context.MethodArgs[0])) return false;
        var definition = method.Module.Reader.GetMethodDefinition(method.Handle);
        return definition.GetDeclaringType() == owner.Handle && definition.RelativeVirtualAddress == method.Rva
            && definition.Attributes == method.Attributes && definition.ImplAttributes == method.ImplAttributes
            && PrimitiveWideConversionContract.IsBodyDefinition(method.Module.Reader, method.Handle, owner.FullName, method.Name);
    }

    internal static void ValidatePrimitiveWideConversionBody(MethodInfo method)
    {
        // Open and unrelated instantiations retain their existing ownership policy.
        if (IsPrimitiveWideConversionBodyCandidate(method) && !IsPrimitiveWideConversionBody(method))
            throw new NotSupportedException("Primitive wide conversion requires its exact closed CoreLib body");
    }
}
