using System.Reflection.Metadata;

namespace Dn2Cpp;

internal static partial class CoreIntrinsics
{
    internal static int VectorAliasElementCode(string name) => name switch
    {
        "AsVectorSByte" => 4, "AsVectorByte" => 5,
        "AsVectorInt16" => 6, "AsVectorUInt16" => 7,
        "AsVectorInt32" => 8, "AsVectorUInt32" => 9,
        "AsVectorInt64" => 10, "AsVectorUInt64" => 11,
        "AsVectorSingle" => 12, "AsVectorDouble" => 13,
        "AsVectorNInt" => 24, "AsVectorNUInt" => 25,
        _ => 0,
    };

    internal static bool IsVectorAsFamilyCallable(MethodInfo method) => IsVectorAs(method) || IsVectorAlias(method);

    internal static bool IsVectorAlias(MethodInfo method)
    {
        int code = VectorAliasElementCode(method.Name);
        if (code == 0 || method.IsSynthetic || method.PInvoke is not null
            || method.Context.TypeArgs.Length != 0 || method.Context.MethodArgs is not [var source]
            || !IsVectorAsElement(source)
            || !IsVectorAsFamilyDefinition(method.DeclaringClass, method.Module, method.Handle)) return false;
        var definition = method.Module.Reader.GetMethodDefinition(method.Handle);
        if (!method.Module.Reader.StringComparer.Equals(definition.Name, method.Name)
            || method.Attributes != definition.Attributes || method.ImplAttributes != definition.ImplAttributes
            || method.Rva != definition.RelativeVirtualAddress) return false;
        var signature = method.Signature;
        return signature.Header.RawValue == 0x10 && signature.GenericParameterCount == 1
            && signature.RequiredParameterCount == 1 && signature.ParameterTypes is [var argument]
            && ReadVectorAsFamilySignature(method.Module.Reader.GetBlobReader(definition.Signature), method.Name, out int token)
            && IsVectorAsClosure(argument, source, method.Module, token)
            && IsVectorAsClosure(signature.ReturnType, TypeDesc.MakePrimitive((PrimitiveTypeCode)code), method.Module, token);
    }

    // These declarations keep their original IL. Only the numeric fast paths
    // substitute a body; unsupported sources forward through the existing As guard.
    internal static bool IsVectorManagedForwarder(MethodInfo method) =>
        method.Name == "op_Explicit"
            ? IsNumericsVectorManagedValueClass(method.DeclaringClass) && IsVectorExplicitDeclaration(method)
            : IsVectorAlias(method) && IsVectorManagedValueElement(method.Context.MethodArgs[0]);

    internal static bool ReadVectorAsFamilySignature(BlobReader blob, string name, out int vectorToken)
    {
        if (name == "As") return ReadVectorAsSignature(blob, out vectorToken);
        vectorToken = 0;
        int code = VectorAliasElementCode(name);
        if (code == 0 || blob.RemainingBytes < 7 || blob.ReadByte() != 0x10
            || blob.ReadByte() != 1 || blob.ReadByte() != 1
            || blob.ReadByte() != 0x15 || blob.ReadByte() != 0x11
            || !blob.TryReadCompressedInteger(out vectorToken) || vectorToken >> 2 == 0 || (vectorToken & 3) > 1
            || blob.RemainingBytes < 2 || blob.ReadByte() != 1 || blob.ReadByte() != code
            || !ReadVectorTypePrefix(ref blob, vectorToken)) return false;
        return blob.RemainingBytes == 2 && blob.ReadByte() == 0x1e && blob.ReadByte() == 0;
    }
}
