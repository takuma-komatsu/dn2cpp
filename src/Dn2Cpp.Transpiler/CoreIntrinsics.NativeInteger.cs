using System.Reflection.Metadata;

namespace Dn2Cpp;

internal static partial class CoreIntrinsics
{
    internal static bool IsNativeIntegerEndian(MethodInfo method) =>
        BinaryIntegerEndianContract.IsNativeOwner(method.DeclaringClass.FullName)
        && IsBinaryIntegerEndianBody(method);

    internal static bool IsNativeIntegerParseCandidate(string owner, string name) =>
        PrimitiveParseContract.IsNativeOwner(owner) && PrimitiveParseContract.IsSlot(name);

    internal static bool IsNativeIntegerParse(MethodInfo method)
    {
        var owner = method.DeclaringClass;
        if (!IsNativeIntegerParseCandidate(owner.FullName, method.Name)
            || !IsScalarNumericLeafOwner(owner) || !ParseDefinitionMatchesModel(method)
            || method.Signature.ParameterTypes.Length == 0) return false;
        var signature = method.Signature;
        var input = PrimitiveParseInput(signature.ParameterTypes[0]);
        bool isTry = method.Name == "TryParse";
        int count = signature.ParameterTypes.Length - (isTry ? 1 : 0);
        bool styles = count > 1 && ParseNamed(signature.ParameterTypes[1], "System.Globalization.NumberStyles");
        bool provider = count > 1 && ParseNamed(signature.ParameterTypes[count - 1], "System.IFormatProvider");
        return input != PrimitiveParseContract.Input.None
            && IsPrimitiveParseSignature(signature, isTry, PrimitiveParseContract.Primitive(owner.FullName), input, styles, provider)
            && PrimitiveParseContract.IsTargetDefinition(method.Module.Reader, method.Handle, owner.FullName,
                isTry, input, styles, provider);
    }

    internal static bool IsNativeIntegerReference(MethodInfo method, MethodSignature<TypeDesc> signature, BlobReader blob)
    {
        if (!(IsNativeIntegerEndian(method) || IsNativeIntegerParse(method))
            || signature.Header.RawValue != 0 || signature.GenericParameterCount != 0) return false;
        var actual = method.Signature;
        if (signature.ParameterTypes.Length != actual.ParameterTypes.Length
            || signature.RequiredParameterCount != actual.ParameterTypes.Length
            || !SameNativeIntegerType(signature.ReturnType, actual.ReturnType)
            || !NativeSignatureByte(ref blob, 0)
            || !NativeSignatureByte(ref blob, (byte)actual.ParameterTypes.Length)
            || !NativeReferenceType(ref blob, actual.ReturnType)) return false;
        for (int i = 0; i < actual.ParameterTypes.Length; i++)
            if (!SameNativeIntegerType(signature.ParameterTypes[i], actual.ParameterTypes[i])
                || !NativeReferenceType(ref blob, actual.ParameterTypes[i])) return false;
        return blob.RemainingBytes == 0;
    }

    private static bool SameNativeIntegerType(TypeDesc left, TypeDesc right) =>
        !left.IsCanonPlaceholder && !right.IsCanonPlaceholder && left.Kind == right.Kind
        && (left.Kind == TypeKind.Primitive ? left.Primitive == right.Primitive
            : left.Kind == TypeKind.Class ? left.Class == right.Class
            : left.Kind == TypeKind.ByRef && left.Element is { } le && right.Element is { } re
                && SameNativeIntegerType(le, re));

    private static bool NativeSignatureByte(ref BlobReader blob, byte expected) =>
        blob.RemainingBytes > 0 && blob.ReadByte() == expected;

    private static bool NativeNamedReference(ref BlobReader blob, byte kind) =>
        NativeSignatureByte(ref blob, kind) && blob.TryReadCompressedInteger(out int coded)
        && coded > 0 && coded >> 2 != 0 && (coded & 3) is 0 or 1;

    private static bool NativeReferenceType(ref BlobReader blob, TypeDesc type)
    {
        if (type.Kind == TypeKind.Primitive) return NativeSignatureByte(ref blob, (byte)type.Primitive);
        if (type is { Kind: TypeKind.ByRef, Element: { } element })
            return NativeSignatureByte(ref blob, 0x10) && NativeReferenceType(ref blob, element);
        if (type is not { Kind: TypeKind.Class, Class: { } owner }) return false;
        if (owner.Module.Owner.GenericDefFullName(owner) == "System.ReadOnlySpan"
            && owner.Context.TypeArgs is [var spanElement])
            return NativeSignatureByte(ref blob, 0x15) && NativeNamedReference(ref blob, 0x11)
                && NativeSignatureByte(ref blob, 1) && NativeReferenceType(ref blob, spanElement);
        return owner.FullName switch
        {
            "System.Globalization.NumberStyles" => NativeNamedReference(ref blob, 0x11),
            "System.IFormatProvider" => NativeNamedReference(ref blob, 0x12),
            _ => false,
        };
    }
}
