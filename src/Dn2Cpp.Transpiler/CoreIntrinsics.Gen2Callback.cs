using System.Reflection;
using System.Reflection.Metadata;

namespace Dn2Cpp;

internal static partial class CoreIntrinsics
{
    // The permanent allocation boundary belongs to this sealed CoreLib type, not
    // to arbitrary finalizable classes or user types with the same display name.
    internal static bool IsGen2GcCallbackType(ClassInfo cls)
    {
        if (cls.Module.AssemblyName != "System.Private.CoreLib" || cls.Handle.IsNil
            || cls.FullName != "System.Gen2GcCallback" || cls.IsValueType || cls.IsInterface
            || cls.IsAbstract || !cls.IsSealed || cls.IsByRefLike || cls.GenericArity != 0
            || cls.Context.TypeArgs.Length != 0
            || cls.BaseClass is not { FullName: "System.Runtime.ConstrainedExecution.CriticalFinalizerObject" } parent
            || parent.Module != cls.Module)
            return false;
        var reader = cls.Module.Reader;
        var owner = reader.GetTypeDefinition(cls.Handle);
        return owner.GetDeclaringType().IsNil && owner.GetGenericParameters().Count == 0
            && owner.Attributes == (TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit)
            && reader.StringComparer.Equals(owner.Namespace, "System")
            && reader.StringComparer.Equals(owner.Name, "Gen2GcCallback");
    }

    internal static bool IsGen2GcCallbackRefusal(MethodInfo method) =>
        method.Name is ".ctor" or "Finalize" && IsGen2GcCallbackMember(method);

    internal static bool IsGen2GcCallbackRegister(MethodInfo method) =>
        method.Name == "Register" && IsGen2GcCallbackMember(method);

    private static bool IsGen2GcCallbackMember(MethodInfo method)
    {
        if (!IsGen2GcCallbackType(method.DeclaringClass) || method.Module != method.DeclaringClass.Module
            || method.Handle.IsNil || method.Rva == 0 || method.PInvoke is not null
            || method.ImplAttributes != MethodImplAttributes.IL)
            return false;
        var attributes = method.Name switch
        {
            ".ctor" => MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
            "Finalize" => MethodAttributes.Family | MethodAttributes.HideBySig | MethodAttributes.Virtual,
            "Register" => MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static,
            _ => (MethodAttributes)(-1),
        };
        return method.Attributes == attributes
            && IsGen2GcCallbackSignature(method.Module.Reader.GetBlobReader(
                method.Module.Reader.GetMethodDefinition(method.Handle).Signature), method.Name)
            && IsGen2GcCallbackDecodedSignature(method.Signature, method.Module, method.Name);
    }

    internal static bool IsGen2GcCallbackDecodedSignature(MethodSignature<TypeDesc> signature, Module module, string name)
    {
        if (signature.Header.RawValue != (name == "Register" ? 0 : 0x20) || signature.GenericParameterCount != 0 || !signature.ReturnType.IsVoid)
            return false;
        var parameters = signature.ParameterTypes;
        if (name == "Finalize") return parameters.Length == 0;
        if (name is not (".ctor" or "Register") || parameters.Length is not (1 or 2)
            || parameters[0] is not { Kind: TypeKind.Class, Class: { IsDelegate: true, IsValueType: false } function }
            || function.Module != module || function.Handle.IsNil
            || function.Context.TypeArgs.Length != parameters.Length)
            return false;
        var definition = module.Reader.GetTypeDefinition(function.Handle);
        if (!definition.GetDeclaringType().IsNil
            || !module.Reader.StringComparer.Equals(definition.Namespace, "System")
            || !module.Reader.StringComparer.Equals(definition.Name, parameters.Length == 1 ? "Func`1" : "Func`2"))
            return false;
        var args = function.Context.TypeArgs;
        return parameters.Length == 1
            ? args[0] is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Boolean }
            : args[0] is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Object }
                && args[1] is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Boolean }
                && parameters[1] is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Object };
    }

    internal static bool IsGen2GcCallbackSignature(BlobReader blob, string name)
    {
        if (blob.RemainingBytes < 3 || blob.ReadByte() != (name == "Register" ? 0 : 0x20))
            return false;
        int count = blob.ReadByte();
        if (blob.ReadByte() != (byte)SignatureTypeCode.Void)
            return false;
        if (name == "Finalize") return count == 0 && blob.RemainingBytes == 0;
        if (name is not (".ctor" or "Register") || count is not (1 or 2) || blob.RemainingBytes < 4
            || blob.ReadByte() != (byte)SignatureTypeCode.GenericTypeInstance || blob.ReadByte() != 0x12
            || !blob.TryReadCompressedInteger(out int token) || token <= 0 || (token >> 2) == 0 || (token & 3) == 3
            || !blob.TryReadCompressedInteger(out int arguments) || arguments != count)
            return false;
        return count == 1
            ? blob.RemainingBytes == 1 && blob.ReadByte() == (byte)SignatureTypeCode.Boolean
            : blob.RemainingBytes == 3 && blob.ReadByte() == (byte)SignatureTypeCode.Object
                && blob.ReadByte() == (byte)SignatureTypeCode.Boolean && blob.ReadByte() == (byte)SignatureTypeCode.Object;
    }
}
