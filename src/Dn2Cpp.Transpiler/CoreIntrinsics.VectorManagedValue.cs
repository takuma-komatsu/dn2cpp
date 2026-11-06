using System.Reflection;
using System.Reflection.Metadata;

namespace Dn2Cpp;

internal static partial class CoreIntrinsics
{
    internal static bool IsVectorManagedValueMethod(MethodInfo method) =>
        IsVectorManagedValueClass(method.DeclaringClass) && IsVectorValueDeclarationMethod(method);

    internal static bool IsVectorValueCallableClass(ClassInfo owner) =>
        IsVectorValueClass(owner) || IsVectorManagedValueClass(owner);

    internal static bool IsVectorManagedValueElement(TypeDesc element) =>
        element is { Kind: TypeKind.Class, IsCanonPlaceholder: false, Class: { } value }
        && !value.Handle.IsNil && (value.IsValueType || value.IsEnum) && !value.IsInterface
        && !value.IsByRefLike && value.GenericArity == 0 && value.Context.TypeArgs.Length == 0;

    // This is a bounded managed-body support set, not the complement of SIMD support.
    internal static bool IsVectorManagedValueClass(ClassInfo owner) =>
        IsNumericsVectorManagedValueClass(owner) || IsFixedVectorManagedValueClass(owner);

    private static bool IsNumericsVectorManagedValueClass(ClassInfo owner)
    {
        if (!IsVectorExplicitOwner(owner) || owner.IntrinsicCppName is not null
            || !owner.IsValueType || owner.IsEnum || owner.IsInterface || owner.GenericArity != 1
            || owner.Context.TypeArgs is not [var element] || !IsVectorManagedValueElement(element))
            return false;
        var reader = owner.Module.Reader;
        foreach (var handle in reader.GetTypeDefinition(owner.Handle).GetGenericParameters())
        {
            var parameter = reader.GetGenericParameter(handle);
            if (parameter.Index != 0 || parameter.Parent != owner.Handle
                || parameter.Attributes != GenericParameterAttributes.None || parameter.GetConstraints().Count != 0)
                return false;
        }
        return true;
    }

    internal static bool IsVectorManagedValueCandidateClass(ClassInfo owner) =>
        owner.IntrinsicCppName is null && (IsVectorExplicitOwner(owner) || IsFixedVectorValueOwner(owner))
        && owner.Context.TypeArgs is [var element] && IsVectorManagedValueElement(element);

    internal static bool IsVectorManagedCountCandidate(ClassInfo owner, string name) =>
        name == "get_Count" && IsVectorManagedValueCandidateClass(owner);

    internal static bool IsVectorManagedEquatableCandidate(ClassInfo owner, ClassInfo vector)
    {
        if (!IsVectorManagedValueCandidateClass(vector) || owner.Module != vector.Module || owner.Handle.IsNil)
            return false;
        var reader = owner.Module.Reader;
        var definition = reader.GetTypeDefinition(owner.Handle);
        return definition.GetDeclaringType().IsNil && reader.StringComparer.Equals(definition.Namespace, "System")
            && reader.StringComparer.Equals(definition.Name, "IEquatable`1");
    }

    internal static bool IsVectorManagedCount(MethodInfo method) =>
        IsVectorManagedValueClass(method.DeclaringClass) && method.Name == "get_Count"
        && VectorValueDeclaration(method, method.DeclaringClass)
        && method.Attributes == (MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig | MethodAttributes.SpecialName)
        && method.ImplAttributes == MethodImplAttributes.IL && method.Rva != 0
        && IsVectorManagedCountSignature(method.Module.Reader.GetBlobReader(
            method.Module.Reader.GetMethodDefinition(method.Handle).Signature))
        && method.Signature.Header.RawValue == 0 && method.Signature.GenericParameterCount == 0
        && method.Signature.RequiredParameterCount == 0 && method.Signature.ParameterTypes.Length == 0
        && method.Signature.ReturnType is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int32, IsCanonPlaceholder: false };

    internal static bool IsVectorManagedCountSignature(BlobReader blob) =>
        blob.RemainingBytes == 3 && blob.ReadByte() == 0 && blob.ReadByte() == 0 && blob.ReadByte() == 8;

    internal static bool IsVectorManagedEquatableConstraints(ClassInfo owner)
    {
        var reader = owner.Module.Reader;
        foreach (var handle in reader.GetTypeDefinition(owner.Handle).GetGenericParameters())
        {
            var parameter = reader.GetGenericParameter(handle);
            if (parameter.Parent != owner.Handle || parameter.Index != 0
                || parameter.Attributes != GenericParameterAttributes.AllowByRefLike || parameter.GetConstraints().Count != 0)
                return false;
        }
        return true;
    }

    internal static bool IsVectorManagedThrowHelper(MethodInfo method) =>
        IsNumericsVectorManagedThrowHelper(method) || IsFixedVectorManagedThrowHelper(method);

    private static bool IsNumericsVectorManagedThrowHelper(MethodInfo method) =>
        method.Name == "ThrowForUnsupportedNumericsVectorBaseType"
        && IsVectorManagedThrowDeclaration(method);

    private static bool IsVectorManagedThrowDeclaration(MethodInfo method)
    {
        var owner = method.DeclaringClass;
        if (owner.FullName != "System.ThrowHelper" || owner.Module.AssemblyName != "System.Private.CoreLib"
            || method.Module != owner.Module || owner.Handle.IsNil || method.Handle.IsNil
            || method.IsSynthetic || method.PInvoke is not null || owner.GenericArity != 0
            || owner.Context.TypeArgs.Length != 0 || method.Context.TypeArgs.Length != 0
            || method.Context.MethodArgs is not [var element] || !IsVectorManagedValueElement(element)
            || method.Attributes != (MethodAttributes.Assembly | MethodAttributes.Static | MethodAttributes.HideBySig)
            || method.ImplAttributes != MethodImplAttributes.AggressiveInlining || method.Rva == 0)
            return false;
        var reader = method.Module.Reader;
        var type = reader.GetTypeDefinition(owner.Handle);
        var definition = reader.GetMethodDefinition(method.Handle);
        if (!type.GetDeclaringType().IsNil || type.GetGenericParameters().Count != 0
            || definition.GetDeclaringType() != owner.Handle || definition.Attributes != method.Attributes
            || definition.ImplAttributes != method.ImplAttributes || definition.RelativeVirtualAddress != method.Rva
            || !reader.StringComparer.Equals(definition.Name, method.Name)
            || definition.GetGenericParameters().Count != 1
            || !IsVectorManagedThrowSignature(reader.GetBlobReader(definition.Signature)))
            return false;
        foreach (var handle in definition.GetGenericParameters())
        {
            var parameter = reader.GetGenericParameter(handle);
            if (parameter.Parent != method.Handle || parameter.Index != 0
                || parameter.Attributes != GenericParameterAttributes.None || parameter.GetConstraints().Count != 0)
                return false;
        }
        var signature = method.Signature;
        return signature.Header.RawValue == 0x10 && signature.GenericParameterCount == 1
            && signature.RequiredParameterCount == 0 && signature.ParameterTypes.Length == 0
            && signature.ReturnType is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Void, IsCanonPlaceholder: false };
    }

    internal static bool IsVectorManagedThrowSignature(BlobReader blob) =>
        blob.RemainingBytes == 4 && blob.ReadByte() == 0x10 && blob.ReadByte() == 1
        && blob.ReadByte() == 0 && blob.ReadByte() == 1;

}
