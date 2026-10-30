using System.Reflection;
using System.Reflection.Metadata;

namespace Dn2Cpp;

internal static partial class CoreIntrinsics
{
    internal static bool IsDateTimeParserBoundary(MethodInfo method) =>
        IsDateTimeTryAddTicks(method) || IsDateTimeTryCreate(method) || IsDateTimeLocalConstructor(method);

    internal static bool IsDateTimeTryAddTicks(MethodInfo method) =>
        IsDateTimeParserOwner(method, "TryAddTicks", MethodAttributes.Assembly | MethodAttributes.HideBySig)
        && IsDateTimeTryAddTicksSignature(DateTimeParserBlob(method))
        && IsDateTimeTryAddTicksDecodedSignature(method.Signature, method.Module);

    internal static bool IsDateTimeTryCreate(MethodInfo method) =>
        IsDateTimeParserOwner(method, "TryCreate", MethodAttributes.Assembly | MethodAttributes.Static | MethodAttributes.HideBySig)
        && IsDateTimeTryCreateSignature(DateTimeParserBlob(method))
        && IsDateTimeTryCreateDecodedSignature(method.Signature, method.Module);

    internal static bool IsDateTimeLocalConstructor(MethodInfo method) =>
        IsDateTimeParserOwner(method, ".ctor", MethodAttributes.Assembly | MethodAttributes.HideBySig
            | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName)
        && IsDateTimeLocalConstructorSignature(DateTimeParserBlob(method))
        && IsDateTimeLocalConstructorDecodedSignature(method.Signature, method.Module);

    private static BlobReader DateTimeParserBlob(MethodInfo method) =>
        method.Module.Reader.GetBlobReader(method.Module.Reader.GetMethodDefinition(method.Handle).Signature);

    private static bool IsDateTimeParserOwner(MethodInfo method, string name, MethodAttributes attributes)
    {
        var cls = method.DeclaringClass;
        if (method.Name != name || cls.FullName != "System.DateTime"
            || method.Module.AssemblyName != "System.Private.CoreLib" || cls.Module != method.Module
            || cls.Handle.IsNil || method.Handle.IsNil || !cls.IsValueType || cls.IsEnum || cls.IsInterface
            || cls.GenericArity != 0 || cls.Context.TypeArgs.Length != 0 || method.Attributes != attributes
            || method.ImplAttributes != MethodImplAttributes.IL || method.Rva == 0 || method.PInvoke is not null)
            return false;
        var reader = method.Module.Reader;
        var owner = reader.GetTypeDefinition(cls.Handle);
        return owner.GetDeclaringType().IsNil && owner.GetGenericParameters().Count == 0
            && reader.GetMethodDefinition(method.Handle).GetGenericParameters().Count == 0
            && reader.StringComparer.Equals(owner.Namespace, "System")
            && reader.StringComparer.Equals(owner.Name, "DateTime");
    }

    private static bool DateTimeParserByte(ref BlobReader blob, byte value) =>
        blob.RemainingBytes > 0 && blob.ReadByte() == value;

    private static bool DateTimeParserValueToken(ref BlobReader blob) =>
        DateTimeParserByte(ref blob, 0x11) && blob.TryReadCompressedInteger(out int token)
        && token > 0 && (token >> 2) != 0 && (token & 3) != 3;

    internal static bool IsDateTimeTryAddTicksSignature(BlobReader blob) =>
        DateTimeParserByte(ref blob, 0x20) && DateTimeParserByte(ref blob, 2)
        && DateTimeParserByte(ref blob, 2) && DateTimeParserByte(ref blob, 0x0A)
        && DateTimeParserByte(ref blob, 0x10) && DateTimeParserValueToken(ref blob) && blob.RemainingBytes == 0;

    internal static bool IsDateTimeTryCreateSignature(BlobReader blob)
    {
        if (!DateTimeParserByte(ref blob, 0) || !DateTimeParserByte(ref blob, 8) || !DateTimeParserByte(ref blob, 2))
            return false;
        for (int i = 0; i < 7; i++) if (!DateTimeParserByte(ref blob, 8)) return false;
        return DateTimeParserByte(ref blob, 0x10) && DateTimeParserValueToken(ref blob) && blob.RemainingBytes == 0;
    }

    internal static bool IsDateTimeLocalConstructorSignature(BlobReader blob) =>
        DateTimeParserByte(ref blob, 0x20) && DateTimeParserByte(ref blob, 3)
        && DateTimeParserByte(ref blob, 1) && DateTimeParserByte(ref blob, 0x0A)
        && DateTimeParserValueToken(ref blob) && DateTimeParserByte(ref blob, 2) && blob.RemainingBytes == 0;

    private static bool IsDateTimeParserValue(TypeDesc type, Module module, bool kind = false)
    {
        if (type is not { Kind: TypeKind.Class, Class: { } cls }
            || cls.Module != module || module.AssemblyName != "System.Private.CoreLib" || cls.Handle.IsNil
            || (!kind && !cls.IsValueType) || cls.IsInterface || cls.IsEnum != kind
            || kind && cls.EnumUnderlying != PrimitiveTypeCode.Int32
            || cls.GenericArity != 0 || cls.Context.TypeArgs.Length != 0
            || cls.FullName != (kind ? "System.DateTimeKind" : "System.DateTime")) return false;
        var definition = module.Reader.GetTypeDefinition(cls.Handle);
        return definition.GetDeclaringType().IsNil && definition.GetGenericParameters().Count == 0
            && module.Reader.StringComparer.Equals(definition.Namespace, "System")
            && module.Reader.StringComparer.Equals(definition.Name, kind ? "DateTimeKind" : "DateTime");
    }

    internal static bool IsDateTimeTryAddTicksDecodedSignature(MethodSignature<TypeDesc> signature, Module module) =>
        signature.Header.RawValue == 0x20 && signature.GenericParameterCount == 0
        && signature.ReturnType is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Boolean }
        && signature.ParameterTypes is [{ Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int64 },
            { Kind: TypeKind.ByRef, Element: { } result }] && IsDateTimeParserValue(result, module);

    internal static bool IsDateTimeTryCreateDecodedSignature(MethodSignature<TypeDesc> signature, Module module)
    {
        if (signature.Header.RawValue != 0 || signature.GenericParameterCount != 0
            || signature.ReturnType is not { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Boolean }
            || signature.ParameterTypes.Length != 8) return false;
        for (int i = 0; i < 7; i++)
            if (signature.ParameterTypes[i] is not { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int32 }) return false;
        return signature.ParameterTypes[7] is { Kind: TypeKind.ByRef, Element: { } result }
            && IsDateTimeParserValue(result, module);
    }

    internal static bool IsDateTimeLocalConstructorDecodedSignature(MethodSignature<TypeDesc> signature, Module module) =>
        signature.Header.RawValue == 0x20 && signature.GenericParameterCount == 0
        && signature.ReturnType is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Void }
        && signature.ParameterTypes is [{ Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int64 }, var kind,
            { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Boolean }] && IsDateTimeParserValue(kind, module, kind: true);

    // Reject this shape before the older arity-based YMD constructor dispatch.
    internal static bool IsPotentialDateTimeLocalConstructor(MethodSignature<TypeDesc> signature) =>
        signature.ParameterTypes.Length == 3 && (signature.ParameterTypes[0].Primitive == PrimitiveTypeCode.Int64
            || signature.ParameterTypes[1].Class?.FullName == "System.DateTimeKind"
            || signature.ParameterTypes[2].Primitive == PrimitiveTypeCode.Boolean);
}
