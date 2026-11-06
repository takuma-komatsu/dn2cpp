using System.Reflection;
using System.Reflection.Metadata;
using SRME = System.Reflection.Metadata.Ecma335.MetadataTokens;

namespace Dn2Cpp;

internal static partial class CoreIntrinsics
{
    internal static bool IsKnownConstantName(string? owner, string name) =>
        name == "IsKnownConstant" && owner == "System.Runtime.CompilerServices.RuntimeHelpers";

    // Decode the declaration, not the substituted signature: IsKnownConstant<char>
    // and IsKnownConstant(char) are different callable bodies.
    internal static bool IsKnownConstant(MethodInfo method)
    {
        if (method.Name != "IsKnownConstant" || method.DeclaringClass.FullName != "System.Runtime.CompilerServices.RuntimeHelpers"
            || method.Module.AssemblyName != "System.Private.CoreLib"
            || method.Module != method.DeclaringClass.Module || method.Handle.IsNil)
            return false;
        var reader = method.Module.Reader;
        var definition = reader.GetMethodDefinition(method.Handle);
        var owner = reader.GetTypeDefinition(method.DeclaringClass.Handle);
        if (!owner.GetDeclaringType().IsNil || owner.GetGenericParameters().Count != 0
            || definition.GetDeclaringType() != method.DeclaringClass.Handle
            || !IsKnownConstantSignature(reader.GetBlobReader(definition.Signature), out byte parameter, out var type))
            return false;
        bool generic = parameter == 0x1e;
        bool parameterIdentity = parameter != 0x12 || KnownConstantCoreType(method.Module, type, "Type");
        var parameters = definition.GetGenericParameters();
        bool constraints = parameters.Count == (generic ? 1 : 0);
        if (constraints && generic)
        {
            var variable = reader.GetGenericParameter(parameters.First());
            constraints = variable.Index == 0
                && variable.Attributes == (GenericParameterAttributes.NotNullableValueTypeConstraint
                    | GenericParameterAttributes.DefaultConstructorConstraint)
                && variable.GetConstraints().Count == 1;
            if (constraints)
                constraints = KnownConstantCoreType(method.Module,
                    reader.GetGenericParameterConstraint(variable.GetConstraints().First()).Type, "ValueType");
        }
        return CoreIntrinsicTypeMaps.IsKnownConstantMethod(method.Module.AssemblyName,
            method.DeclaringClass.FullName, method.Name,
            method.Attributes == (MethodAttributes.Assembly | MethodAttributes.Static | MethodAttributes.HideBySig),
            method.Rva != 0 && method.ImplAttributes == MethodImplAttributes.IL && method.PInvoke is null,
            parameterIdentity, constraints)
            && (!generic || method.Context.MethodArgs.Length == 0
                || method.Context.MethodArgs is [var argument] && IsKnownConstantArgument(argument));
    }

    internal static MethodInfo ValidateKnownConstantCallable(MethodInfo method)
    {
        if (method.Name == "IsKnownConstant" && method.DeclaringClass.FullName == "System.Runtime.CompilerServices.RuntimeHelpers"
            && (!IsKnownConstant(method)
                || method.Module.Reader.GetMethodDefinition(method.Handle).GetGenericParameters().Count != 0
                    && method.Context.MethodArgs.Length != 1))
            throw new NotSupportedException("RuntimeHelpers.IsKnownConstant requires an exact closed callable; generic calls need MethodSpec");
        return method;
    }

    private static bool KnownConstantCoreType(Module module, EntityHandle handle, string name)
    {
        if (handle.Kind != HandleKind.TypeDefinition || handle.IsNil) return false;
        var type = module.Reader.GetTypeDefinition((TypeDefinitionHandle)handle);
        return type.GetDeclaringType().IsNil && type.GetGenericParameters().Count == 0
            && module.Reader.StringComparer.Equals(type.Namespace, "System")
            && module.Reader.StringComparer.Equals(type.Name, name);
    }

    internal static bool IsKnownConstantArgument(TypeDesc argument) =>
        KnownConstantClosedArgument(argument, 0) && (argument.Kind == TypeKind.Primitive && argument.Primitive is
            PrimitiveTypeCode.Boolean or PrimitiveTypeCode.Char or PrimitiveTypeCode.SByte or PrimitiveTypeCode.Byte
            or PrimitiveTypeCode.Int16 or PrimitiveTypeCode.UInt16 or PrimitiveTypeCode.Int32 or PrimitiveTypeCode.UInt32
            or PrimitiveTypeCode.Int64 or PrimitiveTypeCode.UInt64 or PrimitiveTypeCode.IntPtr or PrimitiveTypeCode.UIntPtr
            or PrimitiveTypeCode.Single or PrimitiveTypeCode.Double
        || argument is { Kind: TypeKind.Class, Class: { IsByRefLike: false } cls }
            && (cls.IsValueType || cls.IsEnum) && !cls.Handle.IsNil
            && !(cls.Module.AssemblyName == "System.Private.CoreLib"
                && cls.Module.Reader.StringComparer.Equals(cls.Module.Reader.GetTypeDefinition(cls.Handle).Name, "Nullable`1")));

    // A value shell can still contain unresolved parameters or malformed generic
    // arity. Checking only its outer value-type flag would admit an open callable.
    private static bool KnownConstantClosedArgument(TypeDesc argument, int depth)
    {
        if (depth > 64) return false;
        if (argument.Kind == TypeKind.Primitive)
            return argument.Primitive is not (PrimitiveTypeCode.Void or PrimitiveTypeCode.TypedReference);
        if (argument.Kind is TypeKind.SZArray or TypeKind.MDArray)
            return argument.Element is { } element && KnownConstantClosedArgument(element, depth + 1);
        if (argument is not { Kind: TypeKind.Class, Class: { } cls } || cls.Handle.IsNil
            || cls.Context.TypeArgs.Length != cls.Module.Reader.GetTypeDefinition(cls.Handle).GetGenericParameters().Count)
            return false;
        foreach (var nested in cls.Context.TypeArgs)
            if (!KnownConstantClosedArgument(nested, depth + 1)) return false;
        return true;
    }

    internal static bool IsKnownConstantSignature(BlobReader blob, out byte parameter, out EntityHandle type)
    {
        parameter = 0;
        type = default;
        if (blob.RemainingBytes == 0) return false;
        byte header = blob.ReadByte();
        if (header is not (0 or 0x10) || header == 0x10 && !KnownConstantByte(ref blob, 1)
            || !KnownConstantByte(ref blob, 1) || !KnownConstantByte(ref blob, 2)
            || blob.RemainingBytes == 0) return false;
        parameter = blob.ReadByte();
        if (header == 0x10)
            return parameter == 0x1e && KnownConstantByte(ref blob, 0) && blob.RemainingBytes == 0;
        if (parameter is 3 or 0x0e) return blob.RemainingBytes == 0;
        if (parameter != 0x12 || !blob.TryReadCompressedInteger(out int token)
            || token <= 0 || (token >> 2) == 0 || (token & 3) > 1 || blob.RemainingBytes != 0) return false;
        type = (token & 3) == 0 ? SRME.TypeDefinitionHandle(token >> 2) : SRME.TypeReferenceHandle(token >> 2);
        return true;
    }

    internal static bool IsKnownConstantInstantiation(MetadataReader reader, BlobHandle signature)
    {
        var blob = reader.GetBlobReader(signature);
        if (!KnownConstantByte(ref blob, 0x0a) || !KnownConstantByte(ref blob, 1) || blob.RemainingBytes == 0)
            return false;
        int start = blob.Offset;
        byte kind = blob.ReadByte();
        if (kind is not (>= 2 and <= 0x0d or 0x11 or 0x13 or 0x15 or 0x18 or 0x19 or 0x1e)) return false;
        if (kind == 0x15 && (blob.RemainingBytes == 0 || blob.ReadByte() != 0x11)) return false;
        blob.Offset = start;
        return KnownConstantArgumentShape(ref blob, 0) && blob.RemainingBytes == 0;
    }

    // Decoders erase custom modifiers and pinned wrappers. Check every nested
    // argument before decoding, including reference arguments inside value types.
    private static bool KnownConstantArgumentShape(ref BlobReader blob, int depth)
    {
        if (depth > 64 || blob.RemainingBytes == 0) return false;
        byte kind = blob.ReadByte();
        if (kind is >= 2 and <= 0x0e or 0x18 or 0x19 or 0x1c) return true;
        if (kind is 0x11 or 0x12) return KnownConstantTypeToken(ref blob);
        if (kind is 0x13 or 0x1e) return blob.TryReadCompressedInteger(out _);
        if (kind == 0x1d) return KnownConstantArgumentShape(ref blob, depth + 1);
        if (kind == 0x15)
        {
            if (blob.RemainingBytes == 0 || blob.ReadByte() is not (0x11 or 0x12)
                || !KnownConstantTypeToken(ref blob) || !blob.TryReadCompressedInteger(out int count)
                || count <= 0 || count > blob.RemainingBytes) return false;
            for (int i = 0; i < count; i++)
                if (!KnownConstantArgumentShape(ref blob, depth + 1)) return false;
            return true;
        }
        if (kind == 0x14)
        {
            if (!KnownConstantArgumentShape(ref blob, depth + 1)
                || !blob.TryReadCompressedInteger(out int rank) || rank <= 0
                || !blob.TryReadCompressedInteger(out int sizes) || sizes > rank || sizes > blob.RemainingBytes)
                return false;
            for (int i = 0; i < sizes; i++)
                if (!blob.TryReadCompressedInteger(out _)) return false;
            if (!blob.TryReadCompressedInteger(out int bounds) || bounds > rank || bounds > blob.RemainingBytes)
                return false;
            for (int i = 0; i < bounds; i++)
                if (!blob.TryReadCompressedSignedInteger(out _)) return false;
            return true;
        }
        return false;
    }

    private static bool KnownConstantTypeToken(ref BlobReader blob) =>
        blob.TryReadCompressedInteger(out int token) && token > 0 && (token >> 2) != 0 && (token & 3) <= 1;

    private static bool KnownConstantByte(ref BlobReader blob, byte value) =>
        blob.RemainingBytes != 0 && blob.ReadByte() == value;
}
