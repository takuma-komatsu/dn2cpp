using System.Reflection;
using System.Reflection.Metadata;
using SRME = System.Reflection.Metadata.Ecma335.MetadataTokens;

namespace Dn2Cpp;

internal static partial class CoreIntrinsics
{
    internal static bool IsInt128CheckedType(ClassInfo cls)
    {
        if (cls.Module.AssemblyName != "System.Private.CoreLib" || cls.Handle.IsNil
            || !cls.IsValueType || cls.IsEnum || cls.IsInterface || cls.GenericArity != 0
            || cls.Context.TypeArgs.Length != 0 || cls.FullName != "System.Int128") return false;
        var reader = cls.Module.Reader;
        var definition = reader.GetTypeDefinition(cls.Handle);
        return definition.GetDeclaringType().IsNil && definition.GetGenericParameters().Count == 0
            && (definition.Attributes & TypeAttributes.LayoutMask) == TypeAttributes.SequentialLayout
            && reader.StringComparer.Equals(definition.Namespace, "System")
            && reader.StringComparer.Equals(definition.Name, "Int128");
    }

    internal static bool IsInt128CheckedSource(TypeDesc source, ClassInfo target) =>
        !source.IsCanonPlaceholder && (source.Kind == TypeKind.Primitive && source.Primitive is
            PrimitiveTypeCode.SByte or PrimitiveTypeCode.Byte or PrimitiveTypeCode.Int16 or PrimitiveTypeCode.UInt16
            or PrimitiveTypeCode.Int32 or PrimitiveTypeCode.UInt32 or PrimitiveTypeCode.Int64 or PrimitiveTypeCode.UInt64
            or PrimitiveTypeCode.IntPtr or PrimitiveTypeCode.UIntPtr or PrimitiveTypeCode.Char
        || source is { Kind: TypeKind.Class, Class: { } cls } && cls == target);

    // A closed concrete factory is the only retained body replaced by this unit.
    internal static bool IsInt128Checked(MethodInfo method) =>
        IsInt128CheckedType(method.DeclaringClass) && Int128CheckedDefinition(method, false)
        && method.Context.MethodArgs is [var source] && IsInt128CheckedSource(source, method.DeclaringClass)
        && IsInt128CheckedDecodedSignature(method.Signature, method.DeclaringClass, source);

    internal static bool IsInt128CheckedInterface(MethodInfo method)
    {
        var cls = method.DeclaringClass;
        if (cls.Module.AssemblyName != "System.Private.CoreLib" || cls.Handle.IsNil || !cls.IsInterface
            || cls.GenericArity != 1 || cls.Context.TypeArgs is not [{ Kind: TypeKind.Class, Class: { } target }]
            || !IsInt128CheckedType(target) || target.Module != cls.Module) return false;
        var definition = cls.Module.Reader.GetTypeDefinition(cls.Handle);
        return definition.GetDeclaringType().IsNil
            && cls.Module.Reader.StringComparer.Equals(definition.Namespace, "System.Numerics")
            && cls.Module.Reader.StringComparer.Equals(definition.Name, "INumberBase`1")
            && Int128CheckedDefinition(method, true)
            && method.Context.MethodArgs is [var source] && IsInt128CheckedSource(source, target)
            && IsInt128CheckedDecodedSignature(method.Signature, target, source);
    }

    private static bool Int128CheckedDefinition(MethodInfo method, bool interfaceForm)
    {
        if (method.Name != "CreateChecked" || method.Handle.IsNil || method.Module != method.DeclaringClass.Module
            || method.Rva == 0 || method.PInvoke is not null
            || method.Attributes != (MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig
                | (interfaceForm ? MethodAttributes.Virtual : 0))
            || method.ImplAttributes != MethodImplAttributes.AggressiveInlining) return false;
        var reader = method.Module.Reader;
        var definition = reader.GetMethodDefinition(method.Handle);
        if (definition.GetDeclaringType() != method.DeclaringClass.Handle) return false;
        var parameters = definition.GetGenericParameters();
        if (parameters.Count != 1 || !IsInt128CheckedSignature(reader.GetBlobReader(definition.Signature), interfaceForm)) return false;
        if (!interfaceForm)
        {
            var result = reader.GetBlobReader(definition.Signature);
            result.Offset = 4;
            if (!result.TryReadCompressedInteger(out int resultToken)
                || resultToken != (SRME.GetRowNumber(method.DeclaringClass.Handle) << 2)) return false;
        }
        var parameter = reader.GetGenericParameter(parameters.First());
        if (parameter.Index != 0 || parameter.Attributes != GenericParameterAttributes.None
            || parameter.GetConstraints().Count != 1) return false;
        var constraint = reader.GetGenericParameterConstraint(parameter.GetConstraints().First()).Type;
        if (constraint.Kind != HandleKind.TypeSpecification) return false;
        var blob = reader.GetBlobReader(reader.GetTypeSpecification((TypeSpecificationHandle)constraint).Signature);
        if (!Int128CheckedByte(ref blob, 0x15) || !Int128CheckedByte(ref blob, 0x12)
            || !blob.TryReadCompressedInteger(out int token) || (token & 3) != 0 || (token >> 2) == 0
            || (token >> 2) > reader.TypeDefinitions.Count
            || !Int128CheckedByte(ref blob, 1) || !Int128CheckedByte(ref blob, 0x1e)
            || !Int128CheckedByte(ref blob, 0) || blob.RemainingBytes != 0) return false;
        var numberBase = reader.GetTypeDefinition(SRME.TypeDefinitionHandle(token >> 2));
        return numberBase.GetDeclaringType().IsNil && numberBase.GetGenericParameters().Count == 1
            && (numberBase.Attributes & TypeAttributes.Interface) != 0
            && reader.StringComparer.Equals(numberBase.Namespace, "System.Numerics")
            && reader.StringComparer.Equals(numberBase.Name, "INumberBase`1");
    }

    private static bool Int128CheckedByte(ref BlobReader blob, byte value) =>
        blob.RemainingBytes != 0 && blob.ReadByte() == value;

    internal static bool IsInt128CheckedSignature(BlobReader blob, bool interfaceForm = false)
    {
        if (!Int128CheckedByte(ref blob, 0x10) || !Int128CheckedByte(ref blob, 1)
            || !Int128CheckedByte(ref blob, 1)) return false;
        if (interfaceForm)
        {
            if (!Int128CheckedByte(ref blob, 0x13) || !Int128CheckedByte(ref blob, 0)) return false;
        }
        else if (!Int128CheckedByte(ref blob, 0x11) || !blob.TryReadCompressedInteger(out int token)
            || token <= 0 || (token >> 2) == 0 || (token & 3) == 3) return false;
        return Int128CheckedByte(ref blob, 0x1e) && Int128CheckedByte(ref blob, 0) && blob.RemainingBytes == 0;
    }

    internal static bool IsInt128CheckedDecodedSignature(MethodSignature<TypeDesc> signature, ClassInfo target, TypeDesc source) =>
        signature.Header.RawValue == 0x10 && signature.GenericParameterCount == 1
        && signature.ReturnType is { Kind: TypeKind.Class, Class: { } result } && result == target
        && signature.ParameterTypes is [var parameter] && SameInt128CheckedSource(parameter, source);

    private static bool SameInt128CheckedSource(TypeDesc left, TypeDesc right) =>
        !left.IsCanonPlaceholder && !right.IsCanonPlaceholder && left.Kind == right.Kind
        && (left.Kind == TypeKind.Primitive ? left.Primitive == right.Primitive
            : left.Kind == TypeKind.Class && left.Class == right.Class);

    internal static bool HasInt128CheckedFields(ClassInfo cls)
    {
        if (!IsInt128CheckedType(cls)) return false;
        var reader = cls.Module.Reader;
        int lower = 0, upper = 0;
        foreach (var handle in reader.GetTypeDefinition(cls.Handle).GetFields())
        {
            var field = reader.GetFieldDefinition(handle);
            if ((field.Attributes & FieldAttributes.Static) != 0) continue;
            if (field.Attributes != (FieldAttributes.Private | FieldAttributes.InitOnly)) return false;
            var blob = reader.GetBlobReader(field.Signature);
            if (!Int128CheckedByte(ref blob, 0x06) || !Int128CheckedByte(ref blob, 0x0b) || blob.RemainingBytes != 0) return false;
            if (reader.StringComparer.Equals(field.Name, "_lower")) lower++;
            else if (reader.StringComparer.Equals(field.Name, "_upper")) upper++;
            else return false;
        }
        return lower == 1 && upper == 1;
    }
}
