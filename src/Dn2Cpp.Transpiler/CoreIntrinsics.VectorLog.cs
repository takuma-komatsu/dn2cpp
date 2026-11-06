using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace Dn2Cpp;

internal static partial class CoreIntrinsics
{
    internal static void ValidateVectorLog(MethodInfo method)
    {
        if (method.Name is "Log" or "Log2" && IsVectorEstimateOwnerName(method.DeclaringClass.FullName)
            && !IsVectorLog(method))
            throw new NotSupportedException($"Vector {method.Name} requires its exact floating-point CoreLib declaration");
    }

    internal static bool IsVectorLog(MethodInfo method)
    {
        var owner = method.DeclaringClass;
        if (method.Name is not ("Log" or "Log2") || !IsVectorEstimateOwner(owner) || method.Module != owner.Module
            || method.Handle.IsNil || method.IsSynthetic || method.Context.MethodArgs.Length != 0
            || method.Context.TypeArgs.Length != 0 || method.Rva == 0 || method.PInvoke is not null
            || method.Attributes != (MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig)
            || method.ImplAttributes != MethodImplAttributes.AggressiveInlining
            || !IsVectorLogSignature(method.Signature, owner)) return false;
        var vector = method.Signature.ReturnType.Class!;
        var reader = method.Module.Reader;
        var definition = reader.GetMethodDefinition(method.Handle);
        return definition.Attributes == method.Attributes && definition.ImplAttributes == method.ImplAttributes
            && definition.RelativeVirtualAddress == method.Rva
            && definition.GetDeclaringType() == owner.Handle && definition.GetGenericParameters().Count == 0
            && reader.StringComparer.Equals(definition.Name, method.Name)
            && IsVectorLogRawSignature(reader.GetBlobReader(definition.Signature), MetadataTokens.GetRowNumber(vector.Handle) << 2,
                (int)vector.Context.TypeArgs[0].Primitive);
    }

    internal static bool IsVectorLogSignature(MethodSignature<TypeDesc> signature, ClassInfo owner)
    {
        if (signature.Header.RawValue != 0 || signature.GenericParameterCount != 0 || signature.ParameterTypes.Length != 1
            || signature.RequiredParameterCount != 1
            || signature.ReturnType is not { Kind: TypeKind.Class, IsCanonPlaceholder: false, Class: { } vector }
            || !IsVectorValueClass(vector) || vector.IsByRefLike || vector.Module != owner.Module
            || vector.IntrinsicCppName != owner.IntrinsicCppName
            || vector.Context.TypeArgs[0] is not { Kind: TypeKind.Primitive, IsCanonPlaceholder: false,
                Primitive: PrimitiveTypeCode.Single or PrimitiveTypeCode.Double }
            || signature.ParameterTypes[0] is not { Kind: TypeKind.Class, IsCanonPlaceholder: false, Class: { } operand }
            || operand != vector) return false;
        var reader = vector.Module.Reader;
        var definition = reader.GetTypeDefinition(vector.Handle);
        bool numerics = owner.FullName == "System.Numerics.Vector";
        string name = owner.FullName switch
        {
            "System.Numerics.Vector" => "Vector`1", "System.Runtime.Intrinsics.Vector64" => "Vector64`1",
            "System.Runtime.Intrinsics.Vector128" => "Vector128`1", "System.Runtime.Intrinsics.Vector256" => "Vector256`1", _ => "Vector512`1",
        };
        return reader.StringComparer.Equals(definition.Namespace, numerics ? "System.Numerics" : "System.Runtime.Intrinsics")
            && reader.StringComparer.Equals(definition.Name, name);
    }

    // A public logarithm takes one explicit floating-point closure, never a caller substitution.
    internal static bool IsVectorLogRawSignature(BlobReader blob, int expectedToken = 0, int expectedElement = 0)
    {
        if (blob.RemainingBytes < 2 || blob.ReadByte() != 0 || blob.ReadByte() != 1) return false;
        int token = 0, element = 0;
        for (int i = 0; i < 2; i++)
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
