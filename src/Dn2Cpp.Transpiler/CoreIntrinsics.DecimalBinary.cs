using System.Reflection.Metadata;

namespace Dn2Cpp;

internal static partial class CoreIntrinsics
{
    private static bool DecimalBinaryPrimitive(TypeDesc type, PrimitiveTypeCode primitive) =>
        type is { Kind: TypeKind.Primitive, IsCanonPlaceholder: false } && type.Primitive == primitive;

    private static bool DecimalBinaryNamed(TypeDesc type, string name) =>
        type is { Kind: TypeKind.Class, IsCanonPlaceholder: false, Class: { } owner }
        && owner.Module.AssemblyName == "System.Private.CoreLib" && owner.FullName == name && !owner.Handle.IsNil;

    private static bool DecimalBinarySpan(TypeDesc type, bool readOnly) =>
        type is { Kind: TypeKind.Class, IsCanonPlaceholder: false, Class: { } owner }
        && owner.Module.AssemblyName == "System.Private.CoreLib"
        && owner.Module.Owner.GenericDefFullName(owner) == (readOnly ? "System.ReadOnlySpan" : "System.Span")
        && owner.Context.TypeArgs is [var element] && DecimalBinaryPrimitive(element, PrimitiveTypeCode.Byte);

    private static bool DecimalBinaryModel(MethodInfo method)
    {
        var owner = method.DeclaringClass;
        if (owner.Module.AssemblyName != "System.Private.CoreLib" || method.Module != owner.Module
            || owner.Handle.IsNil || owner.GenericArity != 0 || owner.IsInterface || owner.IsEnum || owner.IsByRefLike
            || owner.Context.TypeArgs.Length != 0 || owner.Context.MethodArgs.Length != 0
            || method.Handle.IsNil || method.IsSynthetic || method.PInvoke is not null
            || method.Context.TypeArgs.Length != 0 || method.Context.MethodArgs.Length != 0) return false;
        var definition = method.Module.Reader.GetMethodDefinition(method.Handle);
        return definition.GetDeclaringType() == owner.Handle && definition.RelativeVirtualAddress == method.Rva
            && definition.Attributes == method.Attributes && definition.ImplAttributes == method.ImplAttributes
            && method.Module.Reader.StringComparer.Equals(definition.Name, method.Name);
    }

    private static bool IsDecimalBinaryBodyCandidate(MethodInfo method)
    {
        int count = DecimalBinaryContract.ParameterCount(method.DeclaringClass.FullName, method.Name);
        return count >= 0 && method.Signature.ParameterTypes.Length == count;
    }

    internal static bool IsDecimalBinaryBody(MethodInfo method)
    {
        if (!IsDecimalBinaryBodyCandidate(method) || !DecimalBinaryModel(method)
            || !DecimalBinaryContract.IsBodyDefinition(method.Module.Reader, method.Module.PE, method.Handle,
                method.DeclaringClass.FullName, method.Name)) return false;
        var signature = method.Signature;
        if (signature.Header.RawValue != 0 || signature.GenericParameterCount != 0
            || signature.RequiredParameterCount != signature.ParameterTypes.Length) return false;
        if (method.DeclaringClass.FullName == "System.ThrowHelper")
            return !method.DeclaringClass.IsValueType && (method.Name == "CreateEndOfFileException"
                ? DecimalBinaryNamed(signature.ReturnType, "System.Exception") : DecimalBinaryPrimitive(signature.ReturnType, PrimitiveTypeCode.Void));
        if (!method.DeclaringClass.IsValueType) return false;
        if (method.Name == "ToDecimal")
            return DecimalBinaryNamed(signature.ReturnType, "System.Decimal") && DecimalBinarySpan(signature.ParameterTypes[0], true);
        return DecimalBinaryPrimitive(signature.ReturnType, PrimitiveTypeCode.Void)
            && signature.ParameterTypes[0] is { Kind: TypeKind.ByRef, IsCanonPlaceholder: false, Element: { } value }
            && DecimalBinaryNamed(value, "System.Decimal") && DecimalBinarySpan(signature.ParameterTypes[1], false);
    }

    internal static bool IsDecimalBinaryConstructor(MethodInfo method)
    {
        if (!DecimalBinaryModel(method) || method.DeclaringClass.FullName != "System.Decimal" || !method.DeclaringClass.IsValueType
            || !DecimalBinaryContract.IsConstructor(method.Module.Reader, method.Handle)) return false;
        var signature = method.Signature;
        return signature.Header.RawValue == 0x20 && signature.GenericParameterCount == 0 && signature.RequiredParameterCount == 4
            && DecimalBinaryPrimitive(signature.ReturnType, PrimitiveTypeCode.Void) && signature.ParameterTypes.Length == 4
            && signature.ParameterTypes.All(p => DecimalBinaryPrimitive(p, PrimitiveTypeCode.Int32));
    }

    internal static void ValidateDecimalBinaryBody(MethodInfo method)
    {
        if (IsDecimalBinaryBodyCandidate(method) && !IsDecimalBinaryBody(method))
            throw new NotSupportedException("Decimal binary conversion requires its exact CoreLib body and signature");
    }
}
