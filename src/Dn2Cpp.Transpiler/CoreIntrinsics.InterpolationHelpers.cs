using System.Reflection;
using System.Reflection.Metadata;

namespace Dn2Cpp;

internal static partial class CoreIntrinsics
{
    internal static bool IsInterpolationStaticHelperName(string name) =>
        name is "HasCustomFormatter" or "GetDefaultLength";

    // These field-free static bodies do not use the intrinsic handler's layout.
    internal static bool IsInterpolationStaticHelper(MethodInfo method)
    {
        var cls = method.DeclaringClass;
        if (!IsInterpolationStaticHelperName(method.Name)
            || cls.FullName != "System.Runtime.CompilerServices.DefaultInterpolatedStringHandler"
            || method.Module.AssemblyName != "System.Private.CoreLib" || cls.Module != method.Module
            || cls.Handle.IsNil || method.Handle.IsNil || !cls.IsValueType || cls.IsEnum || cls.IsInterface
            || cls.GenericArity != 0 || cls.Context.TypeArgs.Length != 0 || method.Context.MethodArgs.Length != 0
            || method.Attributes != (MethodAttributes.Assembly | MethodAttributes.Static | MethodAttributes.HideBySig)
            || method.ImplAttributes != MethodImplAttributes.AggressiveInlining || method.Rva == 0 || method.PInvoke is not null)
            return false;
        var reader = method.Module.Reader;
        var owner = reader.GetTypeDefinition(cls.Handle);
        var definition = reader.GetMethodDefinition(method.Handle);
        return owner.GetDeclaringType().IsNil && owner.GetGenericParameters().Count == 0
            && reader.StringComparer.Equals(owner.Namespace, "System.Runtime.CompilerServices")
            && reader.StringComparer.Equals(owner.Name, "DefaultInterpolatedStringHandler")
            && definition.GetDeclaringType() == cls.Handle && definition.GetGenericParameters().Count == 0
            && IsInterpolationStaticHelperSignature(method.Name, reader.GetBlobReader(definition.Signature))
            && IsInterpolationStaticHelperDecodedSignature(method.Name, method.Signature, method.Module);
    }

    internal static bool IsInterpolationStaticHelperSignature(string name, BlobReader blob)
    {
        if (name == "GetDefaultLength")
            return blob.RemainingBytes == 5 && blob.ReadByte() == 0 && blob.ReadByte() == 2
                && blob.ReadByte() == 8 && blob.ReadByte() == 8 && blob.ReadByte() == 8;
        return name == "HasCustomFormatter" && blob.RemainingBytes >= 5
            && blob.ReadByte() == 0 && blob.ReadByte() == 1 && blob.ReadByte() == 2
            && blob.ReadByte() == 0x12 && blob.TryReadCompressedInteger(out int token)
            && token > 0 && (token >> 2) != 0 && (token & 3) <= 1 && blob.RemainingBytes == 0;
    }

    internal static bool IsInterpolationStaticHelperDecodedSignature(string name, MethodSignature<TypeDesc> signature, Module module)
    {
        if (signature.Header.RawValue != 0 || signature.GenericParameterCount != 0) return false;
        if (name == "GetDefaultLength")
            return signature.ReturnType is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int32 }
                && signature.ParameterTypes is [
                    { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int32 },
                    { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int32 }];
        if (name != "HasCustomFormatter"
            || signature.ReturnType is not { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Boolean }
            || signature.ParameterTypes.Length != 1
            || signature.ParameterTypes[0] is not { Kind: TypeKind.Class, Class: { } provider }
            || provider.Module != module || provider.FullName != "System.IFormatProvider"
            || !provider.IsInterface || provider.IsValueType || provider.Handle.IsNil
            || provider.GenericArity != 0 || provider.Context.TypeArgs.Length != 0) return false;
        var definition = module.Reader.GetTypeDefinition(provider.Handle);
        return definition.GetDeclaringType().IsNil && definition.GetGenericParameters().Count == 0
            && module.Reader.StringComparer.Equals(definition.Namespace, "System")
            && module.Reader.StringComparer.Equals(definition.Name, "IFormatProvider");
    }
}
