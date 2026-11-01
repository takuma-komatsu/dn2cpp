using System.Reflection;
using System.Reflection.Metadata;

namespace Dn2Cpp;

internal static partial class CoreIntrinsics
{
    // Admit the real field-free IL body. Its Vector<byte>.Count dependency uses
    // the target vector width, not the host CLR width or the caller's element.
    internal static bool IsVectorAlignment(MethodInfo method)
    {
        var owner = method.DeclaringClass;
        if (method.Name != "get_Alignment" || owner.FullName != "System.Numerics.Vector"
            || !IsVectorEstimateOwner(owner) || method.Module != owner.Module
            || method.Handle.IsNil || method.IsSynthetic || method.Context.TypeArgs.Length != 0
            || method.Context.MethodArgs.Length != 0 || method.Rva == 0 || method.PInvoke is not null
            || method.Attributes != (MethodAttributes.Assembly | MethodAttributes.Static | MethodAttributes.HideBySig | MethodAttributes.SpecialName)
            || method.ImplAttributes != MethodImplAttributes.IL
            || !IsVectorAlignmentDecodedSignature(method.Signature)) return false;
        var reader = method.Module.Reader;
        var definition = reader.GetMethodDefinition(method.Handle);
        return definition.GetDeclaringType() == owner.Handle && definition.GetGenericParameters().Count == 0
            && definition.Attributes == method.Attributes && definition.ImplAttributes == method.ImplAttributes
            && definition.RelativeVirtualAddress == method.Rva && reader.StringComparer.Equals(definition.Name, method.Name)
            && IsVectorAlignmentSignature(reader.GetBlobReader(definition.Signature));
    }

    internal static bool IsVectorAlignmentSignature(BlobReader blob) =>
        blob.RemainingBytes == 3 && blob.ReadByte() == 0 && blob.ReadByte() == 0 && blob.ReadByte() == 8;

    internal static bool IsVectorAlignmentDecodedSignature(MethodSignature<TypeDesc> signature) =>
        signature.Header.RawValue == 0 && signature.GenericParameterCount == 0
        && signature.RequiredParameterCount == 0 && signature.ParameterTypes.Length == 0
        && signature.ReturnType is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int32, IsCanonPlaceholder: false };

    internal static void ValidateVectorAlignment(MethodInfo method)
    {
        if (method.Name == "get_Alignment" && method.DeclaringClass.FullName == "System.Numerics.Vector"
            && !IsVectorAlignment(method))
            throw new NotSupportedException("Vector.get_Alignment requires its exact CoreLib declaration");
    }
}
