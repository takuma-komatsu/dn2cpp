using System.Reflection;
using System.Reflection.Metadata;
using SRME = System.Reflection.Metadata.Ecma335.MetadataTokens;

namespace Dn2Cpp;

internal static partial class CoreIntrinsics
{
    internal static bool IsUnsupportedAlignedName(string name) => name is "LoadAligned" or "StoreAligned";

    internal static bool IsSimdDefaultOwner(ClassInfo owner)
    {
        if (owner.Handle.IsNil) return false;
        var reader = owner.Module.Reader;
        var type = reader.GetTypeDefinition(owner.Handle);
        return reader.StringComparer.Equals(type.Namespace, "System.Runtime.Intrinsics")
            && reader.StringComparer.Equals(type.Name, "ISimdVector`2");
    }

    internal static bool IsUnsupportedAlignedElement(TypeDesc type)
    {
        if (type is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Char, IsCanonPlaceholder: false }) return true;
        if (!IsVectorManagedValueElement(type) || type.Class is not { IsEnum: false } value
            || value.Module.AssemblyName != "System.Private.CoreLib" || value.Context.MethodArgs.Length != 0) return false;
        var reader = value.Module.Reader;
        var definition = reader.GetTypeDefinition(value.Handle);
        return definition.GetDeclaringType().IsNil && definition.GetGenericParameters().Count == 0
            && reader.StringComparer.Equals(definition.Namespace, "System")
            && (reader.StringComparer.Equals(definition.Name, "Decimal") && value.FullName == "System.Decimal"
                || reader.StringComparer.Equals(definition.Name, "Int128") && value.FullName == "System.Int128"
                || reader.StringComparer.Equals(definition.Name, "UInt128") && value.FullName == "System.UInt128");
    }

    internal static bool SameUnsupportedAlignedElement(TypeDesc actual, TypeDesc expected) =>
        IsUnsupportedAlignedElement(actual) && IsUnsupportedAlignedElement(expected)
        && actual.Kind == expected.Kind && (actual.Kind == TypeKind.Primitive
            ? actual.Primitive == expected.Primitive : actual.Class == expected.Class);

    private static bool IsUnsupportedAlignedVector(ClassInfo vector, TypeDesc element)
    {
        if ((!IsVectorExplicitOwner(vector) && !IsFixedVectorValueOwner(vector))
            || !vector.IsValueType || vector.IsEnum || vector.IsInterface || vector.IsByRefLike
            || vector.GenericArity != 1) return false;
        if (element.Kind == TypeKind.Class)
            return element.Class!.Module == vector.Module && IsVectorManagedValueClass(vector);
        // Char keeps the existing intrinsic storage, without numeric lane admission.
        if (IsFixedVectorValueOwner(vector)) return IsFixedVectorValueStorageClass(vector);
        if (vector.IntrinsicCppName != "Dn2CppVectorT") return false;
        var reader = vector.Module.Reader;
        foreach (var handle in reader.GetTypeDefinition(vector.Handle).GetGenericParameters())
        {
            var parameter = reader.GetGenericParameter(handle);
            if (parameter.Parent != vector.Handle || parameter.Index != 0
                || parameter.Attributes != GenericParameterAttributes.None || parameter.GetConstraints().Count != 0) return false;
        }
        return true;
    }

    internal static bool IsUnsupportedAlignedCandidate(ClassInfo owner, string name) =>
        IsUnsupportedAlignedName(name) && IsSimdDefaultOwner(owner)
        && owner.Context.TypeArgs is [var self, var element]
        && (IsUnsupportedAlignedElement(element) || self.Class?.Context.TypeArgs is [var lane] && IsUnsupportedAlignedElement(lane));

    internal static bool IsUnsupportedAlignedDefault(MethodInfo method)
    {
        var owner = method.DeclaringClass;
        if (!IsUnsupportedAlignedCandidate(owner, method.Name) || method.Module != owner.Module
            || owner.Module.AssemblyName != "System.Private.CoreLib" || !owner.IsInterface || owner.IsValueType
            || owner.IsEnum || owner.GenericArity != 2 || owner.Context.MethodArgs.Length != 0
            || owner.Context.TypeArgs is not [{ Kind: TypeKind.Class, IsCanonPlaceholder: false, Class: { } vector }, var element]
            || !IsUnsupportedAlignedElement(element) || vector.Module != owner.Module
            || !IsUnsupportedAlignedVector(vector, element)
            || vector.Context.MethodArgs.Length != 0 || vector.Context.TypeArgs is not [var lane]
            || !SameUnsupportedAlignedElement(lane, element)
            || method.Context.MethodArgs.Length != 0 || method.Context.TypeArgs is not [var actualSelf, var actualElement]
            || actualSelf.Kind != TypeKind.Class || actualSelf.IsCanonPlaceholder || actualSelf.Class != vector
            || !SameUnsupportedAlignedElement(actualElement, element)
            || method.Handle.IsNil || method.IsSynthetic || method.Rva == 0 || method.PInvoke is not null
            || method.Attributes != (MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.Virtual | MethodAttributes.HideBySig)
            || method.ImplAttributes != MethodImplAttributes.IL)
            return false;
        var reader = owner.Module.Reader;
        var type = reader.GetTypeDefinition(owner.Handle);
        var definition = reader.GetMethodDefinition(method.Handle);
        if (type.Attributes != (TypeAttributes.NotPublic | TypeAttributes.Interface | TypeAttributes.Abstract | TypeAttributes.BeforeFieldInit)
            || !type.GetDeclaringType().IsNil || !type.BaseType.IsNil || type.GetGenericParameters().Count != 2
            || definition.GetDeclaringType() != owner.Handle || definition.GetGenericParameters().Count != 0
            || !reader.StringComparer.Equals(definition.Name, method.Name) || definition.Attributes != method.Attributes
            || definition.ImplAttributes != method.ImplAttributes || definition.RelativeVirtualAddress != method.Rva
            || !IsUnsupportedAlignedSignature(method.Name, reader.GetBlobReader(definition.Signature)))
            return false;
        foreach (var handle in type.GetGenericParameters())
        {
            var parameter = reader.GetGenericParameter(handle);
            if (parameter.Parent != owner.Handle || parameter.Index is not (0 or 1)
                || parameter.Attributes != GenericParameterAttributes.None
                || parameter.GetConstraints().Count != (parameter.Index == 0 ? 1 : 0)) return false;
            foreach (var constraintHandle in parameter.GetConstraints())
            {
                var constraint = reader.GetGenericParameterConstraint(constraintHandle);
                if (constraint.Parameter != handle || constraint.Type.Kind != HandleKind.TypeSpecification) return false;
                var raw = reader.GetBlobReader(reader.GetTypeSpecification((TypeSpecificationHandle)constraint.Type).Signature);
                if (raw.RemainingBytes < 8 || raw.ReadByte() != 0x15 || raw.ReadByte() != 0x12
                    || !raw.TryReadCompressedInteger(out int token) || token != SRME.GetRowNumber(owner.Handle) << 2
                    || raw.RemainingBytes != 5 || raw.ReadByte() != 2 || raw.ReadByte() != 0x13
                    || raw.ReadByte() != 0 || raw.ReadByte() != 0x13 || raw.ReadByte() != 1) return false;
            }
        }
        foreach (var handle in type.GetMethods())
            if (reader.StringComparer.Equals(reader.GetMethodDefinition(handle).Name, ".cctor")) return false;
        foreach (var handle in reader.GetTypeDefinition(vector.Handle).GetMethods())
            if (reader.StringComparer.Equals(reader.GetMethodDefinition(handle).Name, ".cctor")) return false;
        if (!method.SignatureReady) return true;
        var sig = method.Signature;
        bool Vector(TypeDesc value) => value.Kind == TypeKind.Class && !value.IsCanonPlaceholder && value.Class == vector;
        bool Pointer(TypeDesc value) => value.Kind == TypeKind.Pointer && !value.IsCanonPlaceholder
            && value.Element is { } pointed && SameUnsupportedAlignedElement(pointed, element);
        return sig.Header.RawValue == 0 && sig.GenericParameterCount == 0 && sig.RequiredParameterCount == sig.ParameterTypes.Length
            && (method.Name == "LoadAligned"
                ? Vector(sig.ReturnType) && sig.ParameterTypes is [var source] && Pointer(source)
                : sig.ReturnType is { Kind: TypeKind.Primitive, IsCanonPlaceholder: false, Primitive: PrimitiveTypeCode.Void }
                    && sig.ParameterTypes is [var value, var destination] && Vector(value) && Pointer(destination));
    }

    internal static bool IsUnsupportedAlignedSignature(string name, BlobReader raw)
    {
        bool load = name == "LoadAligned";
        if (!IsUnsupportedAlignedName(name) || raw.RemainingBytes != (load ? 7 : 8)
            || raw.ReadByte() != 0 || raw.ReadByte() != (load ? 1 : 2)) return false;
        if (!load && raw.ReadByte() != 1) return false;
        return raw.ReadByte() == 0x13 && raw.ReadByte() == 0 && raw.ReadByte() == 0x0f
            && raw.ReadByte() == 0x13 && raw.ReadByte() == 1;
    }

    internal static void ValidateUnsupportedAlignedDefault(MethodInfo method)
    {
        if (IsUnsupportedAlignedCandidate(method.DeclaringClass, method.Name) && !IsUnsupportedAlignedDefault(method))
            throw new NotSupportedException("Unsupported aligned defaults require their exact closed CoreLib declaration and signature");
    }
}
