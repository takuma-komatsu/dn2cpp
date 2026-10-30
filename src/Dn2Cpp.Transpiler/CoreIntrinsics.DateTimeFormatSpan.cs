using System.Reflection;
using System.Reflection.Metadata;

namespace Dn2Cpp;

internal static partial class CoreIntrinsics
{
    internal static bool IsDateTimeFormatInfoSpanName(string name) => name is
        "AMDesignatorTChar" or "PMDesignatorTChar" or "DateSeparatorTChar"
        or "TimeSeparatorTChar" or "DecimalSeparatorTChar";

    internal static bool IsDateTimeFormatInfoSpan(MethodInfo method)
    {
        var cls = method.DeclaringClass;
        if (!IsDateTimeFormatInfoSpanName(method.Name) || cls.FullName != "System.Globalization.DateTimeFormatInfo"
            || method.Module.AssemblyName != "System.Private.CoreLib" || cls.Module != method.Module
            || cls.Handle.IsNil || method.Handle.IsNil || cls.IsValueType || cls.IsInterface
            || cls.GenericArity != 0 || cls.Context.TypeArgs.Length != 0
            || method.Attributes != (MethodAttributes.Assembly | MethodAttributes.HideBySig)
            || method.ImplAttributes != MethodImplAttributes.IL || method.Rva == 0 || method.PInvoke is not null)
            return false;
        var reader = method.Module.Reader;
        var owner = reader.GetTypeDefinition(cls.Handle);
        var definition = reader.GetMethodDefinition(method.Handle);
        return owner.GetDeclaringType().IsNil && owner.GetGenericParameters().Count == 0
            && reader.StringComparer.Equals(owner.Namespace, "System.Globalization")
            && reader.StringComparer.Equals(owner.Name, "DateTimeFormatInfo")
            && definition.GetGenericParameters().Count == 1
            && IsDateTimeFormatInfoSpanSignature(reader.GetBlobReader(definition.Signature))
            && IsDateTimeFormatInfoSpanDecodedSignature(method.Signature, method.Module);
    }

    internal static bool IsDateTimeFormatInfoSpanSignature(BlobReader blob) =>
        blob.RemainingBytes >= 9 && blob.ReadByte() == 0x30 && blob.ReadByte() == 1 && blob.ReadByte() == 0
        && blob.ReadByte() == (byte)SignatureTypeCode.GenericTypeInstance && blob.ReadByte() == 0x11
        && blob.TryReadCompressedInteger(out int token) && token > 0 && (token >> 2) != 0 && (token & 3) != 3
        && blob.RemainingBytes == 3 && blob.ReadByte() == 1
        && blob.ReadByte() == (byte)SignatureTypeCode.GenericMethodParameter && blob.ReadByte() == 0;

    internal static bool IsDateTimeFormatInfoSpanDecodedSignature(MethodSignature<TypeDesc> signature, Module module)
    {
        if (signature.Header.RawValue != 0x30 || signature.GenericParameterCount != 1
            || signature.ParameterTypes.Length != 0
            || signature.ReturnType is not { Kind: TypeKind.Class, Class: { IsValueType: true, IsEnum: false } span }
            || span.Module != module || span.Handle.IsNil || span.Context.TypeArgs.Length != 1)
            return false;
        var definition = module.Reader.GetTypeDefinition(span.Handle);
        return definition.GetDeclaringType().IsNil && definition.GetGenericParameters().Count == 1
            && module.Reader.StringComparer.Equals(definition.Namespace, "System")
            && module.Reader.StringComparer.Equals(definition.Name, "ReadOnlySpan`1")
            && span.Context.TypeArgs[0] is { Kind: TypeKind.GenericVar, GenVarIsMethod: true, GenVarIndex: 0 }
                or { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Char or PrimitiveTypeCode.Byte };
    }
}
