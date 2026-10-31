using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace Dn2Cpp;

internal static partial class CoreIntrinsics
{
    internal static bool IsConcreteVectorElementName(string name) => name is "GetElement" or "WithElement";

    internal static bool IsConcreteVectorElementReferenceCandidate(MethodSignature<TypeDesc> signature)
    {
        if (signature.GenericParameterCount == 0) return true;
        if (IsConcreteVectorElementOperand(signature.ReturnType)) return true;
        foreach (var parameter in signature.ParameterTypes)
            if (IsConcreteVectorElementOperand(parameter)) return true;
        return false;
    }

    private static bool IsConcreteVectorElementOperand(TypeDesc type)
    {
        while (type.Element is { } element) type = element;
        return type.Class?.FullName is "System.Numerics.Vector2" or "System.Numerics.Vector3" or "System.Numerics.Vector4";
    }

    internal static void ValidateConcreteVectorElement(MethodInfo method)
    {
        if (method.DeclaringClass.FullName == "System.Numerics.Vector" && IsConcreteVectorElementName(method.Name)
            && !method.Handle.IsNil && method.Module.Reader.GetMethodDefinition(method.Handle).GetGenericParameters().Count == 0
            && !IsConcreteVectorElement(method))
            throw new NotSupportedException("Concrete vector element access requires its exact CoreLib declaration");
    }

    internal static bool IsConcreteVectorElementOwner(ClassInfo owner)
    {
        if (owner.Module.AssemblyName != "System.Private.CoreLib" || owner.Handle.IsNil
            || owner.FullName != "System.Numerics.Vector" || owner.GenericArity != 0
            || owner.Context.TypeArgs.Length != 0 || !owner.IsValueType || owner.IsInterface
            || owner.IntrinsicCppName != "Dn2CppVectorT") return false;
        var reader = owner.Module.Reader;
        var definition = reader.GetTypeDefinition(owner.Handle);
        // The static facade is modeled as a value because its native identity is a POD.
        return (definition.Attributes & (TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.Interface))
                == (TypeAttributes.Abstract | TypeAttributes.Sealed)
            && definition.GetDeclaringType().IsNil && definition.GetGenericParameters().Count == 0
            && reader.StringComparer.Equals(definition.Namespace, "System.Numerics")
            && reader.StringComparer.Equals(definition.Name, "Vector");
    }

    internal static int ConcreteVectorElementCount(ClassInfo vector, Module module)
    {
        if (vector.Module != module || module.AssemblyName != "System.Private.CoreLib" || vector.Handle.IsNil
            || !vector.IsValueType || vector.IsEnum || vector.IsInterface || vector.IsByRefLike
            || vector.GenericArity != 0 || vector.Context.TypeArgs.Length != 0 || vector.IntrinsicCppName is not null)
            return 0;
        int count = vector.FullName switch { "System.Numerics.Vector2" => 2, "System.Numerics.Vector3" => 3, "System.Numerics.Vector4" => 4, _ => 0 };
        if (count == 0) return 0;
        var reader = module.Reader;
        var definition = reader.GetTypeDefinition(vector.Handle);
        if (!definition.GetDeclaringType().IsNil || definition.GetGenericParameters().Count != 0
            || !reader.StringComparer.Equals(definition.Namespace, "System.Numerics")
            || !reader.StringComparer.Equals(definition.Name, count == 2 ? "Vector2" : count == 3 ? "Vector3" : "Vector4")) return 0;
        int fields = 0;
        foreach (var handle in definition.GetFields())
        {
            var field = reader.GetFieldDefinition(handle);
            if ((field.Attributes & FieldAttributes.Static) != 0) continue;
            if (field.Attributes != FieldAttributes.Public) return 0;
            var blob = reader.GetBlobReader(field.Signature);
            if (blob.RemainingBytes != 2 || blob.ReadByte() != 6 || blob.ReadByte() != 0x0c) return 0;
            int bit = reader.StringComparer.Equals(field.Name, "X") ? 1 : reader.StringComparer.Equals(field.Name, "Y") ? 2
                : reader.StringComparer.Equals(field.Name, "Z") ? 4 : reader.StringComparer.Equals(field.Name, "W") ? 8 : 0;
            if (bit == 0 || (fields & bit) != 0) return 0;
            fields |= bit;
        }
        return fields == (1 << count) - 1 ? count : 0;
    }

    internal static bool IsConcreteVectorElement(MethodInfo method)
    {
        var owner = method.DeclaringClass;
        if (!IsConcreteVectorElementName(method.Name) || !IsConcreteVectorElementOwner(owner)
            || method.Module != owner.Module || method.Handle.IsNil || method.IsSynthetic
            || method.Context.MethodArgs.Length != 0 || method.Rva == 0 || method.PInvoke is not null
            || method.Attributes != (MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig)
            || method.ImplAttributes != MethodImplAttributes.AggressiveInlining) return false;
        var reader = method.Module.Reader;
        var definition = reader.GetMethodDefinition(method.Handle);
        if (definition.GetDeclaringType() != owner.Handle || definition.GetGenericParameters().Count != 0
            || !reader.StringComparer.Equals(definition.Name, method.Name)
            || !IsConcreteVectorElementSignature(method.Name, method.Signature, owner.Module)) return false;
        var vector = method.Signature.ParameterTypes[0].Class!;
        return IsConcreteVectorElementRawSignature(method.Name, reader.GetBlobReader(definition.Signature), MetadataTokens.GetRowNumber(vector.Handle) << 2);
    }

    internal static bool IsConcreteVectorElementSignature(string name, MethodSignature<TypeDesc> signature, Module module)
    {
        bool with = name == "WithElement";
        if (!IsConcreteVectorElementName(name) || signature.Header.RawValue != 0 || signature.GenericParameterCount != 0
            || signature.ParameterTypes.Length != (with ? 3 : 2)
            || signature.ParameterTypes[0] is not { Kind: TypeKind.Class, IsCanonPlaceholder: false, Class: { } vector }
            || ConcreteVectorElementCount(vector, module) == 0
            || signature.ParameterTypes[1] is not { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int32, IsCanonPlaceholder: false }) return false;
        return with ? signature.ReturnType is { Kind: TypeKind.Class, IsCanonPlaceholder: false, Class: { } result } && result == vector
                && signature.ParameterTypes[2] is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Single, IsCanonPlaceholder: false }
            : signature.ReturnType is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Single, IsCanonPlaceholder: false };
    }

    // Definition tokens are exact; reference tokens are local to the referring module.
    internal static bool IsConcreteVectorElementRawSignature(string name, BlobReader blob, int expectedToken = 0)
    {
        bool with = name == "WithElement";
        if (!IsConcreteVectorElementName(name) || blob.RemainingBytes < 5 || blob.ReadByte() != 0
            || blob.ReadByte() != (with ? 3 : 2)) return false;
        int result = 0;
        if (with) { if (!ReadConcreteVectorToken(ref blob, out result)) return false; }
        else if (blob.ReadByte() != 0x0c) return false;
        if (!ReadConcreteVectorToken(ref blob, out int vector) || expectedToken != 0 && vector != expectedToken
            || with && result != vector || blob.RemainingBytes != (with ? 2 : 1) || blob.ReadByte() != 8) return false;
        return !with || blob.ReadByte() == 0x0c;
    }

    private static bool ReadConcreteVectorToken(ref BlobReader blob, out int token)
    {
        token = 0;
        return blob.RemainingBytes >= 2 && blob.ReadByte() == 0x11 && blob.TryReadCompressedInteger(out token)
            && (token >> 2) != 0 && (token & 3) <= 1;
    }
}
