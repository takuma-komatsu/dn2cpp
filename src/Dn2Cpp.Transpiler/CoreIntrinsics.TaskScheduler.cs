using System.Reflection;
using System.Reflection.Metadata;

namespace Dn2Cpp;

internal static partial class CoreIntrinsics
{
    internal static bool IsTaskSchedulerInlineName(string? owner, string name) =>
        owner == "System.Threading.Tasks.TaskScheduler" && name == "TryRunInline";

    internal static bool IsTaskSchedulerInline(MethodInfo method)
    {
        var owner = method.DeclaringClass;
        if (!IsTaskSchedulerInlineName(owner.FullName, method.Name)
            || method.Module.AssemblyName != "System.Private.CoreLib" || owner.Module != method.Module
            || owner.Handle.IsNil || method.Handle.IsNil || method.IsSynthetic
            || owner.IsValueType || owner.IsInterface || owner.IsEnum || owner.IsByRefLike
            || owner.GenericArity != 0 || owner.Context.TypeArgs.Length != 0 || owner.Context.MethodArgs.Length != 0
            || method.Context.TypeArgs.Length != 0 || method.Context.MethodArgs.Length != 0
            || method.Attributes != (MethodAttributes.Assembly | MethodAttributes.HideBySig)
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
            && IsTaskSchedulerInlineSignature(reader.GetBlobReader(definition.Signature))
            && (!method.SignatureReady || IsTaskSchedulerInlineDecodedSignature(method, method.Signature));
    }

    internal static bool IsTaskSchedulerInlineSignature(BlobReader blob) =>
        blob.RemainingBytes >= 6 && blob.ReadByte() == 0x20 && blob.ReadByte() == 2
        && blob.ReadByte() == (byte)SignatureTypeCode.Boolean && blob.ReadByte() == 0x12
        && blob.TryReadCompressedInteger(out int token) && (token >> 2) != 0 && (token & 3) <= 1
        && blob.RemainingBytes == 1 && blob.ReadByte() == (byte)SignatureTypeCode.Boolean;

    internal static bool IsTaskSchedulerInlineDecodedSignature(MethodInfo method, MethodSignature<TypeDesc> signature)
    {
        if (signature.Header.RawValue != 0x20 || signature.GenericParameterCount != 0
            || signature.RequiredParameterCount != 2
            || signature.ReturnType is not { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Boolean, IsCanonPlaceholder: false }
            || signature.ParameterTypes is not [
                { Kind: TypeKind.Class, IsCanonPlaceholder: false, Class: { } task },
                { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Boolean, IsCanonPlaceholder: false }]
            || task.Module != method.Module || task.FullName != "System.Threading.Tasks.Task"
            || task.Handle.IsNil || task.GenericArity != 0 || task.Context.TypeArgs.Length != 0
            || task.Context.MethodArgs.Length != 0 || task.IsValueType || task.IsInterface)
            return false;
        var definition = task.Module.Reader.GetTypeDefinition(task.Handle);
        return definition.GetDeclaringType().IsNil && definition.GetGenericParameters().Count == 0;
    }
}
