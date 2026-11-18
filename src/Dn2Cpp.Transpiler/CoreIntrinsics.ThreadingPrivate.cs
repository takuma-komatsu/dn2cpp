using System.Reflection;
using System.Reflection.Metadata;

namespace Dn2Cpp;

internal static partial class CoreIntrinsics
{
    internal static bool IsThreadingPrivateName(string? owner, string name) =>
        IsSemaphoreWaiterName(owner, name)
        || owner == "System.Threading.Thread" && name == "ThreadNameChanged"
        || owner == "System.Threading.Lock" && name is "get_ShouldStopPreemptingWaiters" or "ResetWaiterStartTime" or "RecordWaiterStartTime";

    internal static void ValidateThreadingPrivate(MethodInfo method)
    {
        if (IsThreadingPrivateName(method.DeclaringClass.FullName, method.Name)
            && !IsThreadingPrivate(method))
            throw new NotSupportedException($"{method.DeclaringClass.Name}.{method.Name} requires its exact CoreLib declaration");
    }

    internal static bool IsThreadingPrivate(MethodInfo method)
    {
        var owner = method.DeclaringClass;
        if (IsSemaphoreWaiterName(owner.FullName, method.Name))
            return IsSemaphoreWaiter(method);
        bool waiterMember = owner.FullName == "System.Threading.Lock";
        bool waiterQuery = method.Name == "get_ShouldStopPreemptingWaiters";
        if (!IsThreadingPrivateName(owner.FullName, method.Name)
            || method.Module.AssemblyName != "System.Private.CoreLib" || owner.Module != method.Module
            || owner.Handle.IsNil || method.Handle.IsNil || method.IsSynthetic
            || owner.IsValueType || owner.IsInterface || owner.IsEnum || owner.IsByRefLike
            || owner.GenericArity != 0 || owner.Context.TypeArgs.Length != 0 || owner.Context.MethodArgs.Length != 0
            || method.Context.TypeArgs.Length != 0 || method.Context.MethodArgs.Length != 0
            || method.Attributes != (MethodAttributes.Private | MethodAttributes.HideBySig
                | (waiterQuery ? MethodAttributes.SpecialName : 0))
            || method.ImplAttributes != (waiterQuery || method.Name == "RecordWaiterStartTime"
                ? MethodImplAttributes.AggressiveInlining : MethodImplAttributes.IL)
            || method.Rva == 0 || method.PInvoke is not null)
            return false;
        var reader = method.Module.Reader;
        var type = reader.GetTypeDefinition(owner.Handle);
        var definition = reader.GetMethodDefinition(method.Handle);
        return type.GetDeclaringType().IsNil && type.GetGenericParameters().Count == 0
            && reader.StringComparer.Equals(type.Namespace, "System.Threading")
            && reader.StringComparer.Equals(type.Name, waiterMember ? "Lock" : "Thread")
            && definition.GetDeclaringType() == owner.Handle && definition.GetGenericParameters().Count == 0
            && reader.StringComparer.Equals(definition.Name, method.Name)
            && definition.Attributes == method.Attributes && definition.ImplAttributes == method.ImplAttributes
            && definition.RelativeVirtualAddress == method.Rva
            && IsThreadingPrivateSignature(reader.GetBlobReader(definition.Signature), method.Name)
            && (!method.SignatureReady || IsThreadingPrivateDecodedSignature(method.Signature, method.Name));
    }

    internal static bool IsThreadingPrivateSignature(BlobReader blob, string name) =>
        name is "CreateAndAddAsyncWaiter" or "RemoveAsyncWaiter"
            ? IsSemaphoreWaiterSignature(blob, name)
            : name != "ThreadNameChanged"
            ? blob.RemainingBytes == 3 && blob.ReadByte() == 0x20 && blob.ReadByte() == 0
                && blob.ReadByte() == (byte)(name == "get_ShouldStopPreemptingWaiters" ? SignatureTypeCode.Boolean : SignatureTypeCode.Void)
            : blob.RemainingBytes == 4 && blob.ReadByte() == 0x20 && blob.ReadByte() == 1
                && blob.ReadByte() == (byte)SignatureTypeCode.Void && blob.ReadByte() == (byte)SignatureTypeCode.String;

    internal static bool IsThreadingPrivateDecodedSignature(MethodSignature<TypeDesc> signature, string name) =>
        name is "CreateAndAddAsyncWaiter" or "RemoveAsyncWaiter"
            ? IsSemaphoreWaiterDecodedSignature(signature, name)
            : signature.Header.RawValue == 0x20 && signature.GenericParameterCount == 0
        && (name != "ThreadNameChanged"
            ? signature.RequiredParameterCount == 0 && signature.ParameterTypes.Length == 0
                && signature.ReturnType is { Kind: TypeKind.Primitive, IsCanonPlaceholder: false }
                && signature.ReturnType.Primitive == (name == "get_ShouldStopPreemptingWaiters" ? PrimitiveTypeCode.Boolean : PrimitiveTypeCode.Void)
            : signature.RequiredParameterCount == 1
                && signature.ReturnType is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Void, IsCanonPlaceholder: false }
                && signature.ParameterTypes is [{ Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.String, IsCanonPlaceholder: false }]);
}
