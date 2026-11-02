using System.Reflection;
using System.Reflection.Metadata;

namespace Dn2Cpp;

internal static partial class CoreIntrinsics
{
    internal static bool IsBinaryIntegerEndianSlotCandidate(MethodInfo method) =>
        BinaryIntegerEndianContract.IsSlot(method.Name)
        && method.Module.Owner.GenericDefFullName(method.DeclaringClass) == "System.Numerics.IBinaryInteger";

    private static bool IsEndianPrimitive(TypeDesc type, PrimitiveTypeCode primitive) =>
        type is { Kind: TypeKind.Primitive, IsCanonPlaceholder: false } && type.Primitive == primitive;

    private static bool IsEndianByteSpan(TypeDesc type) =>
        type is { Kind: TypeKind.Class, IsCanonPlaceholder: false, Class: { } span }
        && span.Module.AssemblyName == "System.Private.CoreLib"
        && span.Module.Owner.GenericDefFullName(span) == "System.ReadOnlySpan"
        && span.Context.TypeArgs is [var element] && IsEndianPrimitive(element, PrimitiveTypeCode.Byte);

    internal static bool IsBinaryIntegerEndianTrySignature(MethodSignature<TypeDesc> signature, PrimitiveTypeCode primitive) =>
        signature.Header.RawValue == 0 && signature.GenericParameterCount == 0
        && signature.RequiredParameterCount == 3 && signature.ParameterTypes is [var span, var flag, var output]
        && IsEndianPrimitive(signature.ReturnType, PrimitiveTypeCode.Boolean) && IsEndianByteSpan(span)
        && IsEndianPrimitive(flag, PrimitiveTypeCode.Boolean)
        && output is { Kind: TypeKind.ByRef, IsCanonPlaceholder: false, Element: { } element }
        && IsEndianPrimitive(element, primitive);

    internal static bool IsBinaryIntegerEndianBody(MethodInfo method)
    {
        if (IsBinaryIntegerOverflowCandidate(method)) return IsBinaryIntegerOverflowBody(method);
        var owner = method.DeclaringClass;
        if (!BinaryIntegerEndianContract.IsLeafCandidate(owner.FullName, method.Name)
            || !IsScalarNumericLeafOwner(owner) || method.Module != owner.Module || method.Handle.IsNil
            || method.IsSynthetic || method.PInvoke is not null || method.Context.TypeArgs.Length != 0
            || method.Context.MethodArgs.Length != 0
            || !IsBinaryIntegerEndianTrySignature(method.Signature, BinaryIntegerEndianContract.Primitive(owner.FullName))) return false;
        var definition = method.Module.Reader.GetMethodDefinition(method.Handle);
        return definition.GetDeclaringType() == owner.Handle && definition.RelativeVirtualAddress == method.Rva
            && definition.Attributes == method.Attributes && definition.ImplAttributes == method.ImplAttributes
            && BinaryIntegerEndianContract.IsLeafDefinition(method.Module.Reader, method.Handle, owner.FullName, method.Name);
    }

    internal static void ValidateBinaryIntegerEndianBody(MethodInfo method)
    {
        if ((BinaryIntegerEndianContract.IsLeafCandidate(method.DeclaringClass.FullName, method.Name)
                || IsBinaryIntegerOverflowCandidate(method)) && !IsBinaryIntegerEndianBody(method))
            throw new NotSupportedException("Binary integer endian leaf requires its concrete CoreLib declaration");
    }

    private static bool IsBinaryIntegerOverflowCandidate(MethodInfo method) =>
        method.Module.AssemblyName == "System.Private.CoreLib"
        && method.DeclaringClass.FullName == "System.ThrowHelper" && method.Name == "ThrowOverflowException";

    private static bool IsBinaryIntegerOverflowBody(MethodInfo method)
    {
        var owner = method.DeclaringClass;
        var signature = method.Signature;
        if (method.Module != owner.Module || owner.Handle.IsNil || method.Handle.IsNil || method.IsSynthetic
            || method.PInvoke is not null || owner.IsValueType || owner.IsInterface || owner.GenericArity != 0
            || owner.Context.TypeArgs.Length != 0 || owner.Context.MethodArgs.Length != 0
            || method.Context.TypeArgs.Length != 0 || method.Context.MethodArgs.Length != 0
            || signature.Header.RawValue != 0 || signature.GenericParameterCount != 0
            || signature.RequiredParameterCount != 0 || signature.ParameterTypes.Length != 0
            || !IsEndianPrimitive(signature.ReturnType, PrimitiveTypeCode.Void)) return false;
        var definition = method.Module.Reader.GetMethodDefinition(method.Handle);
        return definition.GetDeclaringType() == owner.Handle && definition.RelativeVirtualAddress == method.Rva
            && definition.Attributes == method.Attributes && definition.ImplAttributes == method.ImplAttributes
            && BinaryIntegerEndianContract.IsOverflowHelperDefinition(method.Module.Reader, method.Module.PE, method.Handle);
    }

    internal static bool IsBinaryIntegerEndianDefault(MethodInfo method)
    {
        if (!BinaryIntegerEndianContract.IsDefault(method.Name)
            || !IsBinaryIntegerEndianContractMethod(method, out var primitive)) return false;
        var signature = method.Signature;
        var parameters = signature.ParameterTypes;
        bool source = parameters.Length is 2 or 3 && (IsEndianByteSpan(parameters[0]) && parameters.Length == 2
            || parameters[0] is { Kind: TypeKind.SZArray, IsCanonPlaceholder: false, Element: { } element }
                && IsEndianPrimitive(element, PrimitiveTypeCode.Byte));
        return method.Attributes == (MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.Virtual | MethodAttributes.HideBySig)
            && method.Rva != 0 && signature.Header.RawValue == 0 && signature.GenericParameterCount == 0
            && signature.RequiredParameterCount == parameters.Length && source
            && IsEndianPrimitive(signature.ReturnType, primitive)
            && IsEndianPrimitive(parameters[^1], PrimitiveTypeCode.Boolean)
            && (parameters.Length == 2 || IsEndianPrimitive(parameters[1], PrimitiveTypeCode.Int32))
            && BinaryIntegerEndianContract.IsDefaultSignature(method.Module.Reader,
                method.Module.Reader.GetBlobReader(method.Module.Reader.GetMethodDefinition(method.Handle).Signature));
    }

    internal static bool IsBinaryIntegerEndianContractMethod(MethodInfo method, out PrimitiveTypeCode primitive)
    {
        primitive = PrimitiveTypeCode.Void;
        var owner = method.DeclaringClass;
        if (owner.Module.AssemblyName != "System.Private.CoreLib" || method.Module != owner.Module
            || owner.Handle.IsNil || method.Handle.IsNil || !owner.IsInterface || owner.IsValueType
            || owner.Context.TypeArgs is not [{ Kind: TypeKind.Primitive, IsCanonPlaceholder: false } self]
            || BinaryIntegerEndianContract.Owner(self.Primitive) is null
            || method.Context.TypeArgs is not [var methodSelf] || !IsEndianPrimitive(methodSelf, self.Primitive)
            || method.Context.MethodArgs.Length != 0 || method.IsSynthetic || method.PInvoke is not null) return false;
        var reader = method.Module.Reader;
        var definition = reader.GetMethodDefinition(method.Handle);
        if (!BinaryIntegerEndianContract.IsNamedType(reader, owner.Handle, "System.Numerics", "IBinaryInteger`1")
            || reader.GetTypeDefinition(owner.Handle).GetGenericParameters().Count != 1
            || definition.GetDeclaringType() != owner.Handle || !reader.StringComparer.Equals(definition.Name, method.Name)
            || definition.GetGenericParameters().Count != 0 || definition.Attributes != method.Attributes
            || definition.ImplAttributes != MethodImplAttributes.IL || method.ImplAttributes != definition.ImplAttributes
            || definition.RelativeVirtualAddress != method.Rva) return false;
        primitive = self.Primitive;
        return true;
    }
}
