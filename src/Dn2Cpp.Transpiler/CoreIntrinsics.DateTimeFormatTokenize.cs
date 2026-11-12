using System.Reflection;
using System.Reflection.Metadata;

namespace Dn2Cpp;

internal static partial class CoreIntrinsics
{
    internal static bool IsDateTimeFormatInfoTokenize(MethodInfo method)
    {
        if (!(method.Name == "Tokenize")
            || !IsDateTimeFormatInfoMethodOwner(method, MethodAttributes.Assembly | MethodAttributes.HideBySig))
            return false;
        var reader = method.Module.Reader;
        var definition = reader.GetMethodDefinition(method.Handle);
        return IsDateTimeFormatInfoTokenizeSignature(reader.GetBlobReader(definition.Signature))
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
