using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace Dn2Cpp;

internal static partial class CoreIntrinsics
{
    internal static bool IsVectorEstimateOwnerName(string name) => name is
        "System.Numerics.Vector" or "System.Runtime.Intrinsics.Vector64" or "System.Runtime.Intrinsics.Vector128"
        or "System.Runtime.Intrinsics.Vector256" or "System.Runtime.Intrinsics.Vector512";

    internal static bool IsVectorEstimateOwner(ClassInfo owner)
    {
        if (!IsVectorEstimateOwnerName(owner.FullName) || owner.Module.AssemblyName != "System.Private.CoreLib"
            || owner.Handle.IsNil || owner.GenericArity != 0 || owner.Context.TypeArgs.Length != 0
            || !owner.IsValueType || owner.IsInterface || owner.IsEnum || owner.IsByRefLike) return false;
        string cpp = owner.FullName switch
        {
            "System.Numerics.Vector" => "Dn2CppVectorT", "System.Runtime.Intrinsics.Vector64" => "Dn2CppVector64",
            "System.Runtime.Intrinsics.Vector128" => "Dn2CppVector128", "System.Runtime.Intrinsics.Vector256" => "Dn2CppVector256",
            _ => "Dn2CppVector512",
        };
        if (owner.IntrinsicCppName != cpp) return false;
        var reader = owner.Module.Reader;
        var definition = reader.GetTypeDefinition(owner.Handle);
        bool numerics = owner.FullName == "System.Numerics.Vector";
        string name = owner.FullName switch
        {
            "System.Numerics.Vector" => "Vector", "System.Runtime.Intrinsics.Vector64" => "Vector64",
            "System.Runtime.Intrinsics.Vector128" => "Vector128", "System.Runtime.Intrinsics.Vector256" => "Vector256", _ => "Vector512",
        };
        return definition.GetDeclaringType().IsNil && definition.GetGenericParameters().Count == 0
            && (definition.Attributes & (TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.Interface))
                == (TypeAttributes.Abstract | TypeAttributes.Sealed)
            && reader.StringComparer.Equals(definition.Namespace, numerics ? "System.Numerics" : "System.Runtime.Intrinsics")
            && reader.StringComparer.Equals(definition.Name, name);
    }

    internal static void ValidateVectorMultiplyAddEstimate(MethodInfo method)
    {
        if (method.Name == "MultiplyAddEstimate" && IsVectorEstimateOwnerName(method.DeclaringClass.FullName)
            && !method.Handle.IsNil && method.Module.Reader.GetMethodDefinition(method.Handle).GetGenericParameters().Count == 0
            && !IsVectorMultiplyAddEstimate(method))
            throw new NotSupportedException("Vector MultiplyAddEstimate requires its exact floating-point CoreLib declaration");
    }

    internal static bool IsVectorMultiplyAddEstimate(MethodInfo method)
    {
        var owner = method.DeclaringClass;
        if (method.Name != "MultiplyAddEstimate" || !IsVectorEstimateOwner(owner) || method.Module != owner.Module
            || method.Handle.IsNil || method.IsSynthetic || method.Context.MethodArgs.Length != 0
            || method.Context.TypeArgs.Length != 0 || method.Rva == 0 || method.PInvoke is not null
            || method.Attributes != (MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig)
            || !IsVectorEstimateSignature(method.Signature, owner)) return false;
        var vector = method.Signature.ReturnType.Class!;
        bool single512 = owner.FullName == "System.Runtime.Intrinsics.Vector512"
            && vector.Context.TypeArgs[0].Primitive == PrimitiveTypeCode.Single;
        if (method.ImplAttributes != (single512 ? MethodImplAttributes.IL : MethodImplAttributes.AggressiveInlining)) return false;
        var reader = method.Module.Reader;
        var definition = reader.GetMethodDefinition(method.Handle);
        return definition.Attributes == method.Attributes && definition.ImplAttributes == method.ImplAttributes
            && definition.RelativeVirtualAddress == method.Rva
            && definition.GetDeclaringType() == owner.Handle && definition.GetGenericParameters().Count == 0
            && reader.StringComparer.Equals(definition.Name, method.Name)
            && IsVectorEstimateRawSignature(reader.GetBlobReader(definition.Signature), MetadataTokens.GetRowNumber(vector.Handle) << 2,
                (int)vector.Context.TypeArgs[0].Primitive);
    }

    internal static bool IsVectorEstimateSignature(MethodSignature<TypeDesc> signature, ClassInfo owner)
    {
        if (signature.Header.RawValue != 0 || signature.GenericParameterCount != 0 || signature.ParameterTypes.Length != 3
            || signature.RequiredParameterCount != 3
            || signature.ReturnType is not { Kind: TypeKind.Class, IsCanonPlaceholder: false, Class: { } vector }
            || !IsVectorValueClass(vector) || vector.IsByRefLike || vector.Module != owner.Module
            || vector.IntrinsicCppName != owner.IntrinsicCppName
            || vector.Context.TypeArgs[0] is not { Kind: TypeKind.Primitive, IsCanonPlaceholder: false,
                Primitive: PrimitiveTypeCode.Single or PrimitiveTypeCode.Double }) return false;
        var reader = vector.Module.Reader;
        var definition = reader.GetTypeDefinition(vector.Handle);
        bool numerics = owner.FullName == "System.Numerics.Vector";
        string name = owner.FullName switch
        {
            "System.Numerics.Vector" => "Vector`1", "System.Runtime.Intrinsics.Vector64" => "Vector64`1",
            "System.Runtime.Intrinsics.Vector128" => "Vector128`1", "System.Runtime.Intrinsics.Vector256" => "Vector256`1", _ => "Vector512`1",
        };
        if (!reader.StringComparer.Equals(definition.Namespace, numerics ? "System.Numerics" : "System.Runtime.Intrinsics")
            || !reader.StringComparer.Equals(definition.Name, name)) return false;
        foreach (var parameter in signature.ParameterTypes)
            if (parameter is not { Kind: TypeKind.Class, IsCanonPlaceholder: false, Class: { } operand } || operand != vector) return false;
        return true;
    }

    // Raw operands must be explicit floating-point closures, never caller substitutions.
    internal static bool IsVectorEstimateRawSignature(BlobReader blob, int expectedToken = 0, int expectedElement = 0)
    {
        if (blob.RemainingBytes < 2 || blob.ReadByte() != 0 || blob.ReadByte() != 3) return false;
        int token = 0, element = 0;
        for (int i = 0; i < 4; i++)
        {
            if (blob.RemainingBytes < 5 || blob.ReadByte() != 0x15 || blob.ReadByte() != 0x11
                || !blob.TryReadCompressedInteger(out int current) || (current >> 2) == 0 || (current & 3) > 1
                || blob.RemainingBytes < 2 || blob.ReadByte() != 1) return false;
            int scalar = blob.ReadByte();
            if (scalar is not (0x0c or 0x0d) || expectedElement != 0 && scalar != expectedElement || expectedToken != 0 && current != expectedToken
                || i != 0 && (current != token || scalar != element)) return false;
            token = current;
            element = scalar;
        }
        return blob.RemainingBytes == 0;
    }
}
