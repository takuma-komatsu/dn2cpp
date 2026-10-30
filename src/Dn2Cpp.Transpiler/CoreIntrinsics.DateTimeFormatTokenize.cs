using System.Reflection;
using System.Reflection.Metadata;

namespace Dn2Cpp;

internal static partial class CoreIntrinsics
{
    internal static bool IsDateTimeFormatInfoTokenize(MethodInfo method)
    {
        var cls = method.DeclaringClass;
        if (method.Name != "Tokenize" || cls.FullName != "System.Globalization.DateTimeFormatInfo"
            || method.Module.AssemblyName != "System.Private.CoreLib" || cls.Module != method.Module
            || cls.Handle.IsNil || method.Handle.IsNil || cls.IsValueType || cls.IsInterface
            || cls.GenericArity != 0 || cls.Context.TypeArgs.Length != 0 || method.Context.MethodArgs.Length != 0
            || method.Attributes != (MethodAttributes.Assembly | MethodAttributes.HideBySig)
            || method.ImplAttributes != MethodImplAttributes.IL || method.Rva == 0 || method.PInvoke is not null)
            return false;
        var reader = method.Module.Reader;
        var owner = reader.GetTypeDefinition(cls.Handle);
        return owner.GetDeclaringType().IsNil && owner.GetGenericParameters().Count == 0
            && reader.StringComparer.Equals(owner.Namespace, "System.Globalization")
            && reader.StringComparer.Equals(owner.Name, "DateTimeFormatInfo")
            && IsDateTimeFormatInfoTokenizeSignature(reader.GetBlobReader(reader.GetMethodDefinition(method.Handle).Signature))
            && IsDateTimeFormatInfoTokenizeDecodedSignature(method.Signature, method.Module);
    }

    internal static bool IsDateTimeFormatInfoTokenizeSignature(BlobReader blob)
    {
        static bool Byte(ref BlobReader b, byte value) => b.RemainingBytes != 0 && b.ReadByte() == value;
        static bool Value(ref BlobReader b) => Byte(ref b, 0x11)
            && b.TryReadCompressedInteger(out int token) && token > 0
            && (token >> 2) != 0 && (token & 3) != 3;
        return Byte(ref blob, 0x20) && Byte(ref blob, 4) && Byte(ref blob, 0x02)
            && Value(ref blob) && Byte(ref blob, 0x10) && Value(ref blob)
            && Byte(ref blob, 0x10) && Byte(ref blob, 0x08)
            && Byte(ref blob, 0x10) && Value(ref blob) && blob.RemainingBytes == 0;
    }

    internal static bool IsDateTimeFormatInfoTokenizeDecodedSignature(MethodSignature<TypeDesc> signature, Module module)
    {
        if (signature.Header.RawValue != 0x20 || signature.GenericParameterCount != 0
            || signature.ReturnType is not { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Boolean }
            || signature.ParameterTypes is not [var mask,
                { Kind: TypeKind.ByRef, Element: { } token },
                { Kind: TypeKind.ByRef, Element: { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int32 } },
                { Kind: TypeKind.ByRef, Element: { } parser }])
            return false;
        return ExactValue(mask, "TokenType", true) && ExactValue(token, "TokenType", true)
            && ExactValue(parser, "__DTString", false);

        bool ExactValue(TypeDesc type, string name, bool isEnum)
        {
            if (type is not { Kind: TypeKind.Class, Class: { } cls }
                || cls.Module != module || cls.Handle.IsNil || (!isEnum && !cls.IsValueType) || cls.IsInterface
                || cls.IsEnum != isEnum || (isEnum && cls.EnumUnderlying != PrimitiveTypeCode.Int32)
                || cls.GenericArity != 0 || cls.Context.TypeArgs.Length != 0)
                return false;
            var definition = module.Reader.GetTypeDefinition(cls.Handle);
            return definition.GetDeclaringType().IsNil && definition.GetGenericParameters().Count == 0
                && module.Reader.StringComparer.Equals(definition.Namespace, "System")
                && module.Reader.StringComparer.Equals(definition.Name, name);
        }
    }
}
