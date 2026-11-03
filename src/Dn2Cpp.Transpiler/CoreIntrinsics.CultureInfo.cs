using System.Reflection;
using System.Reflection.Metadata;

namespace Dn2Cpp;

internal static partial class CoreIntrinsics
{
    // The string validator is field-free. Its CultureInfo overload reads the
    // managed inheritance flag and cannot share the headerless native body route.
    internal static bool IsCultureInfoNameValidator(MethodInfo method)
    {
        var cls = method.DeclaringClass;
        if (method.Module.AssemblyName != "System.Private.CoreLib" || method.Module != cls.Module
            || cls.FullName != "System.Globalization.CultureInfo" || method.Name != "VerifyCultureName"
            || cls.Handle.IsNil || method.Handle.IsNil || method.IsSynthetic || cls.IsValueType || cls.IsInterface
            || cls.GenericArity != 0 || cls.Context.TypeArgs.Length != 0 || cls.Context.MethodArgs.Length != 0
            || method.Context.TypeArgs.Length != 0 || method.Context.MethodArgs.Length != 0)
            return false;
        var reader = method.Module.Reader;
        var definition = reader.GetMethodDefinition(method.Handle);
        var owner = reader.GetTypeDefinition(cls.Handle);
        return CoreIntrinsicTypeMaps.IsCultureInfoNameValidatorMethod(method.Module.AssemblyName, cls.FullName, method.Name,
            owner.GetDeclaringType().IsNil && owner.GetGenericParameters().Count == 0
                && reader.StringComparer.Equals(owner.Namespace, "System.Globalization")
                && reader.StringComparer.Equals(owner.Name, "CultureInfo")
                && definition.GetDeclaringType() == cls.Handle
                && reader.StringComparer.Equals(definition.Name, method.Name)
                && definition.GetGenericParameters().Count == 0
                && method.Attributes == (MethodAttributes.Assembly | MethodAttributes.Static | MethodAttributes.HideBySig)
                && method.ImplAttributes == MethodImplAttributes.IL && method.Rva != 0 && method.PInvoke is null
                && definition.Attributes == method.Attributes && definition.ImplAttributes == method.ImplAttributes
                && definition.RelativeVirtualAddress == method.Rva,
            IsCultureInfoNameValidatorSignature(reader.GetBlobReader(definition.Signature))
                && (!method.SignatureReady || IsCultureInfoNameValidatorDecodedSignature(method.Signature)));
    }

    internal static bool IsCultureInfoNameValidatorSignature(BlobReader blob) =>
        blob.RemainingBytes == 5 && blob.ReadByte() == 0 && blob.ReadByte() == 2
        && blob.ReadByte() == 2 && blob.ReadByte() == 0x0e && blob.ReadByte() == 2;

    internal static bool IsCultureInfoNameValidatorDecodedSignature(MethodSignature<TypeDesc> signature) =>
        signature.Header.RawValue == 0 && signature.GenericParameterCount == 0 && signature.RequiredParameterCount == 2
        && signature.ReturnType is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Boolean }
        && signature.ParameterTypes is [{ IsString: true }, { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Boolean }];
}
