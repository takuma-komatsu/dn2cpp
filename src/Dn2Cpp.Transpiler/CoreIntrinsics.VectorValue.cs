using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace Dn2Cpp;

internal static partial class CoreIntrinsics
{
    internal static bool IsVectorValueType(TypeDesc type) =>
        type is { Kind: TypeKind.Class, IsCanonPlaceholder: false, Class: { } owner }
        && IsVectorValueClass(owner);

    internal static bool IsVectorValueClass(ClassInfo owner) =>
        ((IsVectorExplicitOwner(owner) && owner.IntrinsicCppName == "Dn2CppVectorT")
            || IsFixedVectorValueStorageClass(owner))
        && owner.IsValueType && !owner.IsEnum && !owner.IsInterface && owner.GenericArity == 1
        && owner.Context.TypeArgs is [var element] && IsVectorNumericElement(element);

    // This intentionally differs from admission: malformed candidates must not
    // escape into the older name-based vector dispatch.
    internal static bool IsVectorValueCandidate(ClassInfo owner, string name) =>
        name is "Equals" or "GetHashCode"
        && (IsVectorExplicitOwner(owner) || IsFixedVectorValueOwner(owner)
            || owner.IntrinsicCppName is "Dn2CppVectorT" or "Dn2CppVector64" or "Dn2CppVector128"
                or "Dn2CppVector256" or "Dn2CppVector512");

    internal static bool IsVectorValueSignature(MethodSignature<TypeDesc> signature, string name, ClassInfo owner)
    {
        if (signature.Header.RawValue != 0x20 || signature.GenericParameterCount != 0
            || signature.RequiredParameterCount != signature.ParameterTypes.Length)
            return false;
        if (name == "GetHashCode")
            return signature.ParameterTypes.Length == 0
                && signature.ReturnType is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int32, IsCanonPlaceholder: false };
        return name == "Equals"
            && signature.ReturnType is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Boolean, IsCanonPlaceholder: false }
            && signature.ParameterTypes is [var parameter]
            && (parameter is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Object, IsCanonPlaceholder: false }
                || parameter is { Kind: TypeKind.Class, IsCanonPlaceholder: false, Class: { } argument } && argument == owner);
    }

    internal static bool IsVectorValueMethod(MethodInfo method) =>
        IsVectorValueClass(method.DeclaringClass) && IsVectorValueDeclarationMethod(method);

    internal static bool IsVectorValueCallableMethod(MethodInfo method) =>
        IsVectorValueCallableClass(method.DeclaringClass) && IsVectorValueDeclarationMethod(method);

    private static bool IsVectorValueDeclarationMethod(MethodInfo method)
    {
        var owner = method.DeclaringClass;
        if (!IsVectorValueCandidate(owner, method.Name)
            || !VectorValueDeclaration(method, owner) || !IsVectorValueSignature(method.Signature, method.Name, owner))
            return false;
        bool typed = method.Name == "Equals" && method.Signature.ParameterTypes[0].Kind == TypeKind.Class;
        var attributes = MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual;
        if (typed) attributes |= MethodAttributes.Final | MethodAttributes.NewSlot;
        if (method.Attributes != attributes
            || method.ImplAttributes != (typed ? MethodImplAttributes.AggressiveInlining : MethodImplAttributes.IL)
            || method.Rva == 0)
            return false;
        var blob = method.Module.Reader.GetBlobReader(method.Module.Reader.GetMethodDefinition(method.Handle).Signature);
        if (!VectorValueHeader(ref blob, method.Name)) return false;
        if (method.Name == "GetHashCode") return blob.RemainingBytes == 0;
        if (!typed) return blob.RemainingBytes == 1 && blob.ReadByte() == 0x1c;
        return ReadVectorTypePrefix(ref blob, MetadataTokens.GetRowNumber(owner.Handle) << 2)
            && blob.RemainingBytes == 2 && blob.ReadByte() == 0x13 && blob.ReadByte() == 0;
    }

    private static bool VectorValueDeclaration(MethodInfo method, ClassInfo owner)
    {
        if (method.IsSynthetic || method.Handle.IsNil || method.Module != owner.Module
            || method.Context.MethodArgs.Length != 0 || method.PInvoke is not null
            || method.Context.TypeArgs.Length != owner.Context.TypeArgs.Length)
            return false;
        for (int i = 0; i < owner.Context.TypeArgs.Length; i++)
            if (method.Context.TypeArgs[i] != owner.Context.TypeArgs[i]) return false;
        var definition = method.Module.Reader.GetMethodDefinition(method.Handle);
        return definition.GetDeclaringType() == owner.Handle && definition.GetGenericParameters().Count == 0
            && method.Module.Reader.StringComparer.Equals(definition.Name, method.Name)
            && definition.Attributes == method.Attributes && definition.ImplAttributes == method.ImplAttributes
            && definition.RelativeVirtualAddress == method.Rva;
    }

    internal static bool VectorValueHeader(ref BlobReader blob, string name) =>
        blob.RemainingBytes >= 3 && blob.ReadByte() == 0x20
        && blob.ReadByte() == (name == "Equals" ? 1 : 0)
        && blob.ReadByte() == (name == "Equals" ? 0x02 : 0x08)
        && name is "Equals" or "GetHashCode";

    internal static bool IsVectorValueEquatable(ClassInfo itf, ClassInfo vector) =>
        IsVectorValueClass(vector) && IsVectorValueEquatableDeclaration(itf, vector);

    internal static bool IsVectorValueCallableEquatable(ClassInfo itf, ClassInfo vector) =>
        IsVectorValueCallableClass(vector) && IsVectorValueEquatableDeclaration(itf, vector)
        && (IsVectorValueClass(vector) || IsVectorManagedEquatableConstraints(itf));

    private static bool IsVectorValueEquatableDeclaration(ClassInfo itf, ClassInfo vector)
    {
        if (itf.Module != vector.Module || itf.Handle.IsNil
            || !itf.IsInterface || itf.IsValueType || itf.IsEnum || itf.GenericArity != 1
            || itf.Context.TypeArgs is not [{ Kind: TypeKind.Class, IsCanonPlaceholder: false, Class: { } argument }]
            || argument != vector)
            return false;
        var reader = itf.Module.Reader;
        var definition = reader.GetTypeDefinition(itf.Handle);
        return definition.GetDeclaringType().IsNil && definition.GetGenericParameters().Count == 1
            && reader.StringComparer.Equals(definition.Namespace, "System")
            && reader.StringComparer.Equals(definition.Name, "IEquatable`1")
            && (!IsFixedVectorValueOwner(vector) || IsVectorManagedEquatableConstraints(itf));
    }

    internal static bool IsVectorValueEquatableMethod(MethodInfo method) =>
        IsVectorValueEquatableDeclarationMethod(method, false);

    internal static bool IsVectorValueCallableEquatableMethod(MethodInfo method) =>
        IsVectorValueEquatableDeclarationMethod(method, true);

    private static bool IsVectorValueEquatableDeclarationMethod(MethodInfo method, bool managed)
    {
        var owner = method.DeclaringClass;
        if (owner.Context.TypeArgs is not [{ Class: { } vector }]
            || !(managed ? IsVectorValueCallableEquatable(owner, vector) : IsVectorValueEquatable(owner, vector))
            || method.Name != "Equals"
            || !VectorValueDeclaration(method, owner) || method.Rva != 0
            || method.Attributes != (MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual
                | MethodAttributes.NewSlot | MethodAttributes.Abstract)
            || method.ImplAttributes != MethodImplAttributes.IL
            || !IsVectorValueSignature(method.Signature, method.Name, vector)
            || method.Signature.ParameterTypes[0].Class != vector)
            return false;
        var blob = method.Module.Reader.GetBlobReader(method.Module.Reader.GetMethodDefinition(method.Handle).Signature);
        return VectorValueHeader(ref blob, "Equals") && blob.RemainingBytes == 2
            && blob.ReadByte() == 0x13 && blob.ReadByte() == 0;
    }
}
