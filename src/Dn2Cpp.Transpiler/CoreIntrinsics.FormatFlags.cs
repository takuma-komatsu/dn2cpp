using System.Reflection;
using System.Reflection.Metadata;

namespace Dn2Cpp;

internal static partial class CoreIntrinsics
{
    internal static bool IsDateTimeFormatInfoFormatFlags(MethodInfo method)
    {
        if (!(method.Name == "get_FormatFlags")
            || !IsDateTimeFormatInfoMethodOwner(method, MethodAttributes.Assembly | MethodAttributes.HideBySig | MethodAttributes.SpecialName))
            return false;
        var reader = method.Module.Reader;
        var definition = reader.GetMethodDefinition(method.Handle);
        return IsDateTimeFormatInfoFormatFlagsSignature(reader.GetBlobReader(definition.Signature))
            && IsDateTimeFormatInfoFormatFlagsDecodedSignature(method.Signature, method.Module);
    }

    internal static bool IsDateTimeFormatInfoFormatFlagsSignature(BlobReader blob) =>
        blob.RemainingBytes >= 4 && blob.ReadByte() == 0x20 && blob.ReadByte() == 0
        && blob.ReadByte() == 0x11 && blob.TryReadCompressedInteger(out int token)
        && token > 0 && (token >> 2) != 0 && (token & 3) != 3 && blob.RemainingBytes == 0;

    internal static bool IsDateTimeFormatInfoFormatFlagsDecodedSignature(MethodSignature<TypeDesc> signature, Module module)
    {
        if (signature.Header.RawValue != 0x20 || signature.GenericParameterCount != 0
            || signature.ParameterTypes.Length != 0
            || signature.ReturnType is not { Kind: TypeKind.Class, Class: { FullName: "System.Globalization.DateTimeFormatFlags",
                IsEnum: true, EnumUnderlying: PrimitiveTypeCode.Int32 } flags }
            || flags.Module != module || flags.Handle.IsNil || flags.GenericArity != 0
            || flags.Context.TypeArgs.Length != 0)
            return false;
        var definition = module.Reader.GetTypeDefinition(flags.Handle);
        return definition.GetDeclaringType().IsNil && definition.GetGenericParameters().Count == 0
            && module.Reader.StringComparer.Equals(definition.Namespace, "System.Globalization")
            && module.Reader.StringComparer.Equals(definition.Name, "DateTimeFormatFlags");
    }
}
