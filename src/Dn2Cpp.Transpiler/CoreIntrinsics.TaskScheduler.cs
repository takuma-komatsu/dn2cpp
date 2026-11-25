using System.Reflection;
using System.Reflection.Metadata;

namespace Dn2Cpp;

internal static partial class CoreIntrinsics
{
    internal static bool IsTaskSchedulerMemberName(string? owner, string name) =>
        owner == "System.Threading.Tasks.TaskScheduler" && name is "TryRunInline" or "InternalQueueTask" or "get_Id" or "TryDequeue" or "TryExecuteTask";

    internal static bool IsTaskSchedulerId(MethodInfo method) =>
        method.Name == "get_Id" && IsTaskSchedulerOperation(method);

    internal static bool IsTaskSchedulerInline(MethodInfo method) =>
        method.Name == "TryRunInline" && IsTaskSchedulerOperation(method);

    internal static bool IsTaskSchedulerDequeue(MethodInfo method) =>
        method.Name == "TryDequeue" && IsTaskSchedulerMember(method);

    internal static bool IsTaskSchedulerExecute(MethodInfo method) =>
        method.Name == "TryExecuteTask" && IsTaskSchedulerMember(method);

    internal static bool IsTaskSchedulerOperation(MethodInfo method) =>
        method.Name is not ("TryDequeue" or "TryExecuteTask") && IsTaskSchedulerMember(method);

    internal static bool IsTaskSchedulerMember(MethodInfo method)
    {
        var owner = method.DeclaringClass;
        if (!IsTaskSchedulerMemberName(owner.FullName, method.Name)
            || method.Module.AssemblyName != "System.Private.CoreLib" || owner.Module != method.Module
            || owner.Handle.IsNil || method.Handle.IsNil || method.IsSynthetic
            || owner.IsValueType || owner.IsInterface || owner.IsEnum || owner.IsByRefLike
            || owner.GenericArity != 0 || owner.Context.TypeArgs.Length != 0 || owner.Context.MethodArgs.Length != 0
            || method.Context.TypeArgs.Length != 0 || method.Context.MethodArgs.Length != 0
            || method.Attributes != (method.Name switch
            {
                "get_Id" => MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName,
                "TryExecuteTask" => MethodAttributes.Family | MethodAttributes.HideBySig,
                "TryDequeue" => MethodAttributes.FamORAssem | MethodAttributes.HideBySig | MethodAttributes.Virtual | MethodAttributes.NewSlot,
                _ => MethodAttributes.Assembly | MethodAttributes.HideBySig,
            })
            || method.ImplAttributes != MethodImplAttributes.IL || method.Rva == 0 || method.PInvoke is not null)
            return false;
        var reader = method.Module.Reader;
        var type = reader.GetTypeDefinition(owner.Handle);
        var definition = reader.GetMethodDefinition(method.Handle);
        return type.GetDeclaringType().IsNil && type.GetGenericParameters().Count == 0
            && reader.StringComparer.Equals(type.Namespace, "System.Threading.Tasks")
            && reader.StringComparer.Equals(type.Name, "TaskScheduler")
            && definition.GetDeclaringType() == owner.Handle && definition.GetGenericParameters().Count == 0
            && reader.StringComparer.Equals(definition.Name, method.Name)
            && definition.Attributes == method.Attributes && definition.ImplAttributes == method.ImplAttributes
            && definition.RelativeVirtualAddress == method.Rva
            && IsTaskSchedulerOperationSignature(method.Name, reader.GetBlobReader(definition.Signature))
            && (!method.SignatureReady || IsTaskSchedulerOperationDecodedSignature(method, method.Signature));
    }

    internal static bool IsTaskSchedulerOperationSignature(string name, BlobReader blob)
    {
        if (name == "get_Id")
            return blob.RemainingBytes == 3 && blob.ReadByte() == 0x20 && blob.ReadByte() == 0
                && blob.ReadByte() == (byte)SignatureTypeCode.Int32;
        bool inline = name == "TryRunInline";
        if (!inline && name is not ("InternalQueueTask" or "TryDequeue" or "TryExecuteTask")) return false;
        return blob.RemainingBytes >= (inline ? 6 : 5)
            && blob.ReadByte() == 0x20 && blob.ReadByte() == (inline ? 2 : 1)
            && blob.ReadByte() == (byte)(name == "InternalQueueTask" ? SignatureTypeCode.Void : SignatureTypeCode.Boolean)
            && blob.ReadByte() == 0x12
            && blob.TryReadCompressedInteger(out int token) && (token >> 2) != 0 && (token & 3) <= 1
            && (inline ? blob.RemainingBytes == 1 && blob.ReadByte() == (byte)SignatureTypeCode.Boolean
                : blob.RemainingBytes == 0);
    }

    internal static bool IsTaskSchedulerOperationDecodedSignature(MethodInfo method, MethodSignature<TypeDesc> signature)
    {
        if (method.Name == "get_Id")
            return signature.Header.RawValue == 0x20 && signature.GenericParameterCount == 0
                && signature.RequiredParameterCount == 0 && signature.ParameterTypes.Length == 0
                && signature.ReturnType is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int32, IsCanonPlaceholder: false };
        bool inline = method.Name == "TryRunInline";
        if ((!inline && method.Name is not ("InternalQueueTask" or "TryDequeue" or "TryExecuteTask"))
            || signature.Header.RawValue != 0x20 || signature.GenericParameterCount != 0
            || signature.RequiredParameterCount != (inline ? 2 : 1)
            || signature.ParameterTypes.Length != (inline ? 2 : 1)
            || (method.Name == "InternalQueueTask" ? !signature.ReturnType.IsVoid
                : signature.ReturnType is not { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Boolean, IsCanonPlaceholder: false })
            || signature.ParameterTypes[0] is not { Kind: TypeKind.Class, IsCanonPlaceholder: false, Class: { } task }
            || (inline && signature.ParameterTypes[1] is not { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Boolean, IsCanonPlaceholder: false })
            || task.Module != method.Module || task.FullName != "System.Threading.Tasks.Task"
            || task.Handle.IsNil || task.GenericArity != 0 || task.Context.TypeArgs.Length != 0
            || task.Context.MethodArgs.Length != 0 || task.IsValueType || task.IsInterface)
            return false;
        var definition = task.Module.Reader.GetTypeDefinition(task.Handle);
        return definition.GetDeclaringType().IsNil && definition.GetGenericParameters().Count == 0;
    }
}
