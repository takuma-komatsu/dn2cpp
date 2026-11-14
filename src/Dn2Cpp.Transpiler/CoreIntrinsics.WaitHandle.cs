using System.Reflection;
using System.Reflection.Metadata;
using SRME = System.Reflection.Metadata.Ecma335.MetadataTokens;

namespace Dn2Cpp;

internal static partial class CoreIntrinsics
{
    // These field-free wrappers preserve the existing native-import capability boundary.
    internal static bool IsWaitHandleRawWaitWrapper(MethodInfo method)
    {
        if (!IsWaitHandleRawWaitOwner(method) || method.Rva == 0 || method.PInvoke is not null)
            return false;
        var reader = method.Module.Reader;
        var definition = reader.GetMethodDefinition(method.Handle);
        bool assembly = method.Attributes == (MethodAttributes.Assembly | MethodAttributes.Static | MethodAttributes.HideBySig);
        bool privateMethod = method.Attributes == (MethodAttributes.Private | MethodAttributes.Static | MethodAttributes.HideBySig);
        if (!TryReadWaitHandleRawWaitWrapperSignature(method.Name, reader.GetBlobReader(definition.Signature),
                out var span, out int parameters))
            return false;
        return CoreIntrinsicTypeMaps.IsWaitHandleRawWaitWrapperMethod(method.Module.AssemblyName,
            method.DeclaringClass.FullName, method.Name,
            method.ImplAttributes == MethodImplAttributes.IL
                && (method.Name == "WaitMultipleIgnoringSyncContextCore" || parameters == 4 ? privateMethod : assembly),
            parameters, span.Kind == HandleKind.TypeDefinition
                && IsWaitHandleRawWaitSpanDefinition(method.Module, (TypeDefinitionHandle)span)
                && (!method.SignatureReady || IsWaitHandleRawWaitWrapperDecodedSignature(
                    method.Signature, method.Module, method.Name)));
    }

    internal static bool IsWaitHandleRawWaitImport(MethodInfo method)
    {
        if (!IsWaitHandleRawWaitOwner(method) || method.Rva != 0
            || method.Name != "<WaitMultipleIgnoringSyncContext>g____PInvoke|2_0"
            || method.Attributes != (MethodAttributes.Assembly | MethodAttributes.Static
                | MethodAttributes.HideBySig | MethodAttributes.PinvokeImpl)
            || method.ImplAttributes != MethodImplAttributes.PreserveSig
            || method.PInvoke is not { ModuleName: "QCall", RawEntryPoint: "WaitHandle_WaitMultipleIgnoringSyncContext" })
            return false;
        var reader = method.Module.Reader;
        var definition = reader.GetMethodDefinition(method.Handle);
        var import = definition.GetImport();
        return !import.Module.IsNil
            && reader.StringComparer.Equals(reader.GetModuleReference(import.Module).Name, "QCall")
            && reader.StringComparer.Equals(import.Name, "WaitHandle_WaitMultipleIgnoringSyncContext")
            && import.Attributes == (MethodImportAttributes.CallingConventionWinApi | MethodImportAttributes.ExactSpelling)
            && method.PInvoke.ImportAttributes == import.Attributes
            && IsWaitHandleRawWaitImportSignature(reader.GetBlobReader(definition.Signature))
            && (!method.SignatureReady || IsWaitHandleRawWaitImportDecodedSignature(method.Signature));
    }

    private static bool IsWaitHandleRawWaitOwner(MethodInfo method)
    {
        var owner = method.DeclaringClass;
        if (method.Module.AssemblyName != "System.Private.CoreLib" || method.Module != owner.Module
            || owner.FullName != "System.Threading.WaitHandle" || method.IsSynthetic
            || owner.Handle.IsNil || method.Handle.IsNil || owner.IsValueType || owner.IsInterface
            || owner.IsEnum || owner.IsByRefLike || owner.GenericArity != 0
            || owner.Context.TypeArgs.Length != 0 || owner.Context.MethodArgs.Length != 0
            || method.Context.TypeArgs.Length != 0 || method.Context.MethodArgs.Length != 0)
            return false;
        var reader = method.Module.Reader;
        var type = reader.GetTypeDefinition(owner.Handle);
        var definition = reader.GetMethodDefinition(method.Handle);
        return type.GetDeclaringType().IsNil && type.GetGenericParameters().Count == 0
            && reader.StringComparer.Equals(type.Namespace, "System.Threading")
            && reader.StringComparer.Equals(type.Name, "WaitHandle")
            && (type.Attributes & (TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.Interface)) == TypeAttributes.Abstract
            && definition.GetDeclaringType() == owner.Handle && definition.GetGenericParameters().Count == 0
            && reader.StringComparer.Equals(definition.Name, method.Name)
            && definition.Attributes == method.Attributes && definition.ImplAttributes == method.ImplAttributes
            && definition.RelativeVirtualAddress == method.Rva;
    }

    internal static bool IsWaitHandleRawWaitWrapperSignature(Module module, string name, BlobReader blob) =>
        TryReadWaitHandleRawWaitWrapperSignature(name, blob, out var span, out _)
        && span.Kind == HandleKind.TypeDefinition
        && IsWaitHandleRawWaitSpanDefinition(module, (TypeDefinitionHandle)span);

    // References resolve the returned type handle in their own module before granting authority.
    internal static bool TryReadWaitHandleRawWaitWrapperSignature(string name, BlobReader blob,
        out EntityHandle spanDefinition, out int parameterCount)
    {
        spanDefinition = default;
        parameterCount = 0;
        if (name is not ("WaitMultipleIgnoringSyncContext" or "WaitMultipleIgnoringSyncContextCore")
            || blob.RemainingBytes < 3 || blob.ReadByte() != 0)
            return false;
        parameterCount = blob.ReadByte();
        if ((parameterCount != 3 && (name != "WaitMultipleIgnoringSyncContext" || parameterCount != 4))
            || blob.ReadByte() != (byte)SignatureTypeCode.Int32
            || blob.RemainingBytes < 5 || blob.ReadByte() != 0x15 || blob.ReadByte() != 0x11
            || !blob.TryReadCompressedInteger(out int token) || token >> 2 == 0
            || (token & 3) > 1 || blob.RemainingBytes < 2 || blob.ReadByte() != 1
            || blob.ReadByte() != (byte)SignatureTypeCode.IntPtr)
            return false;
        spanDefinition = (token & 3) == 0 ? SRME.TypeDefinitionHandle(token >> 2) : SRME.TypeReferenceHandle(token >> 2);
        if (parameterCount == 4 && (blob.RemainingBytes < 1 || blob.ReadByte() != (byte)SignatureTypeCode.Int32))
            return false;
        return blob.RemainingBytes == 2 && blob.ReadByte() == (byte)SignatureTypeCode.Boolean
            && blob.ReadByte() == (byte)SignatureTypeCode.Int32;
    }

    private static bool IsWaitHandleRawWaitSpanDefinition(Module module, TypeDefinitionHandle handle)
    {
        if (module.AssemblyName != "System.Private.CoreLib" || handle.IsNil
            || SRME.GetRowNumber(handle) > module.Reader.TypeDefinitions.Count)
            return false;
        var reader = module.Reader;
        var definition = reader.GetTypeDefinition(handle);
        return definition.GetDeclaringType().IsNil && definition.GetGenericParameters().Count == 1
            && reader.StringComparer.Equals(definition.Namespace, "System")
            && reader.StringComparer.Equals(definition.Name, "ReadOnlySpan`1");
    }

    internal static bool IsWaitHandleRawWaitWrapperDecodedSignature(
        MethodSignature<TypeDesc> signature, Module module, string name)
    {
        var parameters = signature.ParameterTypes;
        if (name is not ("WaitMultipleIgnoringSyncContext" or "WaitMultipleIgnoringSyncContextCore")
            || signature.Header.RawValue != 0 || signature.GenericParameterCount != 0
            || signature.RequiredParameterCount != parameters.Length
            || (parameters.Length != 3 && (name != "WaitMultipleIgnoringSyncContext" || parameters.Length != 4))
            || !IsRawWaitPrimitive(signature.ReturnType, PrimitiveTypeCode.Int32)
            || parameters[0] is not { Kind: TypeKind.Class, IsCanonPlaceholder: false, Class: { } span }
            || span.Module != module || !span.IsValueType || !span.IsByRefLike || span.IsEnum || span.IsInterface
            || span.GenericArity != 1 || span.Context.MethodArgs.Length != 0
            || span.Context.TypeArgs is not [{ } element] || !IsRawWaitPrimitive(element, PrimitiveTypeCode.IntPtr)
            || !IsWaitHandleRawWaitSpanDefinition(module, span.Handle))
            return false;
        return (parameters.Length == 3 || IsRawWaitPrimitive(parameters[1], PrimitiveTypeCode.Int32))
            && IsRawWaitPrimitive(parameters[^2], PrimitiveTypeCode.Boolean)
            && IsRawWaitPrimitive(parameters[^1], PrimitiveTypeCode.Int32);
    }

    internal static bool IsWaitHandleRawWaitImportSignature(BlobReader blob) =>
        blob.RemainingBytes == 8 && blob.ReadByte() == 0 && blob.ReadByte() == 4
        && blob.ReadByte() == (byte)SignatureTypeCode.Int32 && blob.ReadByte() == (byte)SignatureTypeCode.Pointer
        && blob.ReadByte() == (byte)SignatureTypeCode.IntPtr && blob.ReadByte() == (byte)SignatureTypeCode.Int32
        && blob.ReadByte() == (byte)SignatureTypeCode.Int32 && blob.ReadByte() == (byte)SignatureTypeCode.Int32;

    private static bool IsWaitHandleRawWaitImportDecodedSignature(MethodSignature<TypeDesc> signature) =>
        signature.Header.RawValue == 0 && signature.GenericParameterCount == 0 && signature.RequiredParameterCount == 4
        && IsRawWaitPrimitive(signature.ReturnType, PrimitiveTypeCode.Int32)
        && signature.ParameterTypes is [{ Kind: TypeKind.Pointer, Element: { } element }, var count, var all, var timeout]
        && IsRawWaitPrimitive(element, PrimitiveTypeCode.IntPtr) && IsRawWaitPrimitive(count, PrimitiveTypeCode.Int32)
        && IsRawWaitPrimitive(all, PrimitiveTypeCode.Int32) && IsRawWaitPrimitive(timeout, PrimitiveTypeCode.Int32);

    private static bool IsRawWaitPrimitive(TypeDesc type, PrimitiveTypeCode code) =>
        type.Kind == TypeKind.Primitive && !type.IsCanonPlaceholder && type.Primitive == code;
}
