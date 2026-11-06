using System.Reflection;
using System.Reflection.Metadata;
using SRME = System.Reflection.Metadata.Ecma335.MetadataTokens;

namespace Dn2Cpp;

internal static partial class CoreIntrinsics
{
    internal static bool IsPrimitiveBigMul(MethodInfo method)
    {
        var cls = method.DeclaringClass;
        if (method.Name != "BigMul" || !CoreIntrinsicTypeMaps.IsPrimitiveBigMulOwner(cls.FullName)
            || cls.Module.AssemblyName != "System.Private.CoreLib" || method.Module != cls.Module
            || cls.Handle.IsNil || method.Handle.IsNil || !cls.IsValueType || cls.IsEnum || cls.IsInterface
            || cls.GenericArity != 0 || cls.Context.TypeArgs.Length != 0 || method.Context.MethodArgs.Length != 0
            || method.Rva == 0 || method.PInvoke is not null
            || method.Attributes != (MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig)
            || method.ImplAttributes != MethodImplAttributes.IL) return false;
        var reader = method.Module.Reader;
        var owner = reader.GetTypeDefinition(cls.Handle);
        var definition = reader.GetMethodDefinition(method.Handle);
        if (!owner.GetDeclaringType().IsNil || owner.GetGenericParameters().Count != 0
            || !reader.StringComparer.Equals(owner.Namespace, "System")
            || !reader.StringComparer.Equals(owner.Name, cls.FullName switch {
                "System.Int32" => "Int32", "System.UInt32" => "UInt32", "System.Int64" => "Int64",
                "System.UInt64" => "UInt64", "System.IntPtr" => "IntPtr", _ => "UIntPtr" })
            || definition.GetDeclaringType() != cls.Handle || definition.GetGenericParameters().Count != 0) return false;
        var code = CoreIntrinsicTypeMaps.WellKnownPrimitiveCode(cls.FullName)!.Value;
        bool native = code is PrimitiveTypeCode.IntPtr or PrimitiveTypeCode.UIntPtr;
        var blob = reader.GetBlobReader(definition.Signature);
        if (!Int128CheckedByte(ref blob, 0) || !Int128CheckedByte(ref blob, native ? (byte)3 : (byte)2)) return false;
        if (code is PrimitiveTypeCode.Int64 or PrimitiveTypeCode.UInt64)
        {
            if (!Int128CheckedByte(ref blob, 0x11) || !blob.TryReadCompressedInteger(out int token)
                || token <= 0 || (token & 3) != 0 || (token >> 2) > reader.TypeDefinitions.Count
                || !HasBigMulResultFields(reader, SRME.TypeDefinitionHandle(token >> 2),
                    code == PrimitiveTypeCode.Int64 ? "Int128" : "UInt128")) return false;
        }
        else if (!Int128CheckedByte(ref blob, (byte)(native ? code : code == PrimitiveTypeCode.Int32
            ? PrimitiveTypeCode.Int64 : PrimitiveTypeCode.UInt64))) return false;
        if (!Int128CheckedByte(ref blob, (byte)code) || !Int128CheckedByte(ref blob, (byte)code)) return false;
        if (native)
        {
            if (!Int128CheckedByte(ref blob, 0x10) || !Int128CheckedByte(ref blob, (byte)code)) return false;
            bool output = false;
            foreach (var parameterHandle in definition.GetParameters())
            {
                var parameter = reader.GetParameter(parameterHandle);
                if (parameter.SequenceNumber == 3) output = parameter.Attributes == ParameterAttributes.Out;
            }
            if (!output) return false;
        }
        return blob.RemainingBytes == 0;
    }

    internal static bool HasBigMulResultFields(MetadataReader reader, TypeDefinitionHandle handle, string name)
    {
        var definition = reader.GetTypeDefinition(handle);
        if (!definition.GetDeclaringType().IsNil || definition.GetGenericParameters().Count != 0
            || (definition.Attributes & TypeAttributes.Interface) != 0 || definition.BaseType.Kind != HandleKind.TypeDefinition
            || (definition.Attributes & TypeAttributes.LayoutMask) != TypeAttributes.SequentialLayout
            || !reader.StringComparer.Equals(definition.Namespace, "System")
            || !reader.StringComparer.Equals(definition.Name, name)) return false;
        var baseType = reader.GetTypeDefinition((TypeDefinitionHandle)definition.BaseType);
        if (!baseType.GetDeclaringType().IsNil || !reader.StringComparer.Equals(baseType.Namespace, "System")
            || !reader.StringComparer.Equals(baseType.Name, "ValueType")) return false;
        int lower = 0, upper = 0;
        foreach (var fieldHandle in definition.GetFields())
        {
            var field = reader.GetFieldDefinition(fieldHandle);
            if ((field.Attributes & FieldAttributes.Static) != 0) continue;
            if (field.Attributes != (FieldAttributes.Private | FieldAttributes.InitOnly)) return false;
            var blob = reader.GetBlobReader(field.Signature);
            if (!Int128CheckedByte(ref blob, 6) || !Int128CheckedByte(ref blob, 11) || blob.RemainingBytes != 0) return false;
            if (reader.StringComparer.Equals(field.Name, "_lower")) lower++;
            else if (reader.StringComparer.Equals(field.Name, "_upper")) upper++;
            else return false;
        }
        return lower == 1 && upper == 1;
    }

    internal static bool IsPrimitiveBigMulReference(MethodInfo method, MethodSignature<TypeDesc> signature, BlobReader blob)
    {
        if (!IsPrimitiveBigMul(method) || signature.Header.RawValue != 0 || signature.GenericParameterCount != 0) return false;
        var code = CoreIntrinsicTypeMaps.WellKnownPrimitiveCode(method.DeclaringClass.FullName)!.Value;
        bool native = code is PrimitiveTypeCode.IntPtr or PrimitiveTypeCode.UIntPtr;
        if (!Int128CheckedByte(ref blob, 0) || !Int128CheckedByte(ref blob, native ? (byte)3 : (byte)2)) return false;
        if (code is PrimitiveTypeCode.Int64 or PrimitiveTypeCode.UInt64)
        {
            if (!Int128CheckedByte(ref blob, 0x11) || !blob.TryReadCompressedInteger(out int token)
                || token <= 0 || (token >> 2) == 0 || (token & 3) == 3) return false;
        }
        else if (!Int128CheckedByte(ref blob, (byte)(native ? code : code == PrimitiveTypeCode.Int32
            ? PrimitiveTypeCode.Int64 : PrimitiveTypeCode.UInt64))) return false;
        if (!Int128CheckedByte(ref blob, (byte)code) || !Int128CheckedByte(ref blob, (byte)code)
            || native && (!Int128CheckedByte(ref blob, 0x10) || !Int128CheckedByte(ref blob, (byte)code))
            || blob.RemainingBytes != 0) return false;
        var actual = method.Signature;
        if (signature.ParameterTypes.Length != actual.ParameterTypes.Length
            || !SameBigMulType(signature.ReturnType, actual.ReturnType)) return false;
        for (int i = 0; i < signature.ParameterTypes.Length; i++)
            if (!SameBigMulType(signature.ParameterTypes[i], actual.ParameterTypes[i])) return false;
        return true;
    }

    private static bool SameBigMulType(TypeDesc left, TypeDesc right) =>
        !left.IsCanonPlaceholder && !right.IsCanonPlaceholder && left.Kind == right.Kind
        && (left.Kind == TypeKind.Primitive ? left.Primitive == right.Primitive
            : left.Kind == TypeKind.Class ? left.Class == right.Class
            : left.Kind == TypeKind.ByRef && left.Element is { } le && right.Element is { } re && SameBigMulType(le, re));
}
