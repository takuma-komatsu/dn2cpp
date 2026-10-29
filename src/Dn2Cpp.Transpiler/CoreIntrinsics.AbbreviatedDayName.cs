using System.Reflection;
using System.Reflection.Metadata;

namespace Dn2Cpp;

internal static partial class CoreIntrinsics
{
    internal static bool IsDateTimeFormatInfoAbbreviatedDayName(MethodInfo method)
    {
        var cls = method.DeclaringClass;
        if (method.Name != "GetAbbreviatedDayName" || cls.FullName != "System.Globalization.DateTimeFormatInfo"
            || method.Module.AssemblyName != "System.Private.CoreLib" || cls.Module != method.Module
            || cls.Handle.IsNil || method.Handle.IsNil || cls.IsValueType || cls.IsInterface
            || cls.GenericArity != 0 || cls.Context.TypeArgs.Length != 0
            || method.Attributes != (MethodAttributes.Public | MethodAttributes.HideBySig)
            || method.ImplAttributes != MethodImplAttributes.IL || method.Rva == 0 || method.PInvoke is not null)
            return false;
        var reader = method.Module.Reader;
        var owner = reader.GetTypeDefinition(cls.Handle);
        return owner.GetDeclaringType().IsNil && owner.GetGenericParameters().Count == 0
            && reader.StringComparer.Equals(owner.Namespace, "System.Globalization")
            && reader.StringComparer.Equals(owner.Name, "DateTimeFormatInfo")
            && IsDateTimeFormatInfoAbbreviatedDayNameSignature(reader.GetBlobReader(reader.GetMethodDefinition(method.Handle).Signature))
            && IsDateTimeFormatInfoAbbreviatedDayNameDecodedSignature(method.Signature, method.Module);
    }

    internal static bool IsDateTimeFormatInfoAbbreviatedDayNameSignature(BlobReader blob) =>
        blob.RemainingBytes >= 5 && blob.ReadByte() == 0x20 && blob.ReadByte() == 1
        && blob.ReadByte() == (byte)SignatureTypeCode.String && blob.ReadByte() == 0x11
        && blob.TryReadCompressedInteger(out int token) && token > 0 && (token >> 2) != 0
        && (token & 3) != 3 && blob.RemainingBytes == 0;

    internal static bool IsDateTimeFormatInfoAbbreviatedDayNameDecodedSignature(MethodSignature<TypeDesc> signature, Module module)
    {
        if (signature.Header.RawValue != 0x20 || signature.GenericParameterCount != 0
            || signature.ReturnType is not { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.String }
            || signature.ParameterTypes is not [{ Kind: TypeKind.Class, Class: { FullName: "System.DayOfWeek",
                IsEnum: true, EnumUnderlying: PrimitiveTypeCode.Int32 } day }]
            || day.Module != module || day.Handle.IsNil || day.GenericArity != 0 || day.Context.TypeArgs.Length != 0)
            return false;
        var definition = module.Reader.GetTypeDefinition(day.Handle);
        return definition.GetDeclaringType().IsNil && definition.GetGenericParameters().Count == 0
            && module.Reader.StringComparer.Equals(definition.Namespace, "System")
            && module.Reader.StringComparer.Equals(definition.Name, "DayOfWeek");
    }
}
