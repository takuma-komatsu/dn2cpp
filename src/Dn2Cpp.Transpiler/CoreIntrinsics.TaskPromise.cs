using System.Reflection;
using System.Reflection.Metadata;

namespace Dn2Cpp;

internal static partial class CoreIntrinsics
{
    internal static bool IsTaskCompletionSourceOwner(ClassInfo owner) =>
        owner.Module.AssemblyName == "System.Private.CoreLib"
        && (owner.FullName == "System.Threading.Tasks.TaskCompletionSource"
            || owner.IntrinsicCppName == "Dn2CppTaskCompletionSource*");

    internal static bool IsTaskCompletionTransfer(MethodInfo method) =>
        method.Name is ("SetFromTask" or "TrySetFromTask") && IsTaskCompletionSourceOwner(method.DeclaringClass);

    internal static bool IsTaskDefaultResultField(ClassInfo owner, string name)
    {
        if (name != "s_defaultResultTask" || owner.IntrinsicCppName != "Dn2CppTask*"
            || owner.Module.AssemblyName != "System.Private.CoreLib"
            || owner.GenericArity != 1 || owner.Handle.IsNil)
            return false;
        var reader = owner.Module.Reader;
        var definition = reader.GetTypeDefinition(owner.Handle);
        return definition.GetDeclaringType().IsNil && definition.GetGenericParameters().Count == 1
            && reader.StringComparer.Equals(definition.Namespace, "System.Threading.Tasks")
            && reader.StringComparer.Equals(definition.Name, "Task`1");
    }

    // FIELD CLASS Task<!0>. A MemberRef may refer to the same Task definition
    // through a TypeRef; its resolved field type must also match the closed owner.
    internal static bool IsTaskDefaultResultSignature(BlobReader blob, EntityHandle definition = default)
    {
        if (blob.RemainingBytes < 7 || blob.ReadByte() != 0x06
            || blob.ReadByte() != (byte)SignatureTypeCode.GenericTypeInstance || blob.ReadByte() != 0x12)
            return false;
        var type = blob.ReadTypeHandle();
        return !type.IsNil && (definition.IsNil || type == definition)
            && blob.RemainingBytes == 3 && blob.ReadByte() == 1
            && blob.ReadByte() == (byte)SignatureTypeCode.GenericTypeParameter && blob.ReadByte() == 0;
    }

    internal static bool IsTaskLifecycleOwner(ClassInfo owner)
    {
        if (owner.Handle.IsNil) return false;
        var reader = owner.Module.Reader;
        var definition = reader.GetTypeDefinition(owner.Handle);
        return reader.StringComparer.Equals(definition.Namespace, "System.Threading.Tasks")
            && (reader.StringComparer.Equals(definition.Name, "Task")
                || reader.StringComparer.Equals(definition.Name, "Task`1"));
    }

    // Public delegate constructors are independent of the internal promise family.
    internal static bool IsTaskLifecycleCandidate(string name, BlobReader blob)
    {
        if (name is "TrySetResult" or "MarkStarted" or "AddException" or "Finish") return true;
        if (name != ".ctor" || blob.RemainingBytes < 3) return false;
        var header = blob.ReadSignatureHeader();
        if (header.IsGeneric && !blob.TryReadCompressedInteger(out _)) return false;
        if (!blob.TryReadCompressedInteger(out int count)) return false;
        if (count == 0) return true;
        if (count is not (2 or 3) || blob.RemainingBytes < 2) return false;
        blob.ReadByte(); // return kind is validated by the exact contract
        return blob.ReadByte() == (byte)SignatureTypeCode.Object;
    }

    internal static void ValidateTaskLifecycle(MethodInfo method)
    {
        if (IsTaskSchedulerMemberName(method.DeclaringClass.FullName, method.Name)
            && !IsTaskSchedulerMember(method))
            throw new NotSupportedException("TaskScheduler operation requires its exact CoreLib declaration");
        if (IsTaskLifecycleOwner(method.DeclaringClass) && !method.Handle.IsNil
            && IsTaskLifecycleCandidate(method.Name, method.Module.Reader.GetBlobReader(
                method.Module.Reader.GetMethodDefinition(method.Handle).Signature))
            && !IsTaskLifecycle(method))
            throw new NotSupportedException("Task lifecycle member requires its exact CoreLib declaration");
    }

    internal static bool IsTaskLifecycle(MethodInfo method)
    {
        if (IsTaskSchedulerOperation(method)) return true;
        var owner = method.DeclaringClass;
        if (method.Name is not (".ctor" or "TrySetResult" or "MarkStarted" or "AddException" or "Finish") || !IsTaskLifecycleOwner(owner)
            || method.Module.AssemblyName != "System.Private.CoreLib" || owner.Module != method.Module
            || method.Handle.IsNil || method.IsSynthetic || owner.IsValueType || owner.IsInterface
            || owner.IsEnum || owner.IsByRefLike || owner.Context.MethodArgs.Length != 0
            || method.Context.MethodArgs.Length != 0 || method.Rva == 0 || method.PInvoke is not null
            || method.ImplAttributes != MethodImplAttributes.IL
            || method.Attributes != (MethodAttributes.Assembly | MethodAttributes.HideBySig
                | (method.Name == ".ctor" ? MethodAttributes.SpecialName | MethodAttributes.RTSpecialName : 0)))
            return false;
        var reader = method.Module.Reader;
        var type = reader.GetTypeDefinition(owner.Handle);
        var definition = reader.GetMethodDefinition(method.Handle);
        bool generic = reader.StringComparer.Equals(type.Name, "Task`1");
        int arity = generic ? 1 : 0;
        return type.GetDeclaringType().IsNil && type.GetGenericParameters().Count == arity
            && owner.GenericArity == arity && owner.Context.TypeArgs.Length == arity
            && method.Context.TypeArgs.Length == arity
            && definition.GetDeclaringType() == owner.Handle && definition.GetGenericParameters().Count == 0
            && reader.StringComparer.Equals(definition.Name, method.Name)
            && definition.Attributes == method.Attributes && definition.ImplAttributes == method.ImplAttributes
            && definition.RelativeVirtualAddress == method.Rva
            && IsTaskLifecycleSignature(method.Name, generic, reader.GetBlobReader(definition.Signature))
            && (!method.SignatureReady || IsTaskLifecycleDecodedSignature(method, method.Signature));
    }

    internal static bool IsManagedTaskEventName(string name) =>
        name == "PublishUnobservedTaskException";

    private static bool IsUnobservedTaskEventArgs(TypeDesc t) =>
        t is { Kind: TypeKind.Class, Class: { FullName: "System.Threading.Tasks.UnobservedTaskExceptionEventArgs" } c }
        && c.Module.AssemblyName == "System.Private.CoreLib";

    internal static bool IsUnobservedTaskEventHandler(TypeDesc t) =>
        t is { Kind: TypeKind.Class, Class: { } c }
        && c.Module.AssemblyName == "System.Private.CoreLib"
        && c.Module.Reader.StringComparer.Equals(c.Module.Reader.GetTypeDefinition(c.Handle).Name, "EventHandler`1")
        && c.Module.Reader.StringComparer.Equals(c.Module.Reader.GetTypeDefinition(c.Handle).Namespace, "System")
        && c.Context.TypeArgs is [{ } a] && IsUnobservedTaskEventArgs(a);

    internal static bool IsManagedTaskEvent(MethodInfo m)
    {
        if (!IsManagedTaskEventName(m.Name) || m.DeclaringClass.FullName != "System.Threading.Tasks.TaskScheduler"
            || m.Module.AssemblyName != "System.Private.CoreLib" || m.Handle.IsNil || m.Rva == 0
            || m.ImplAttributes != MethodImplAttributes.IL) return false;
        if (m.Attributes != (MethodAttributes.Static | MethodAttributes.HideBySig | MethodAttributes.Assembly)) return false;
        var s = m.Signature;
        return s.Header.RawValue == 0
            && s.ReturnType is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Void }
            && s.ParameterTypes is [{ } sender, { } args] && sender.IsObject && IsUnobservedTaskEventArgs(args);
    }

    internal static bool IsTaskFinish(MethodInfo method) =>
        method.Name == "Finish" && IsTaskLifecycle(method);

    internal static bool IsTaskAddException(MethodInfo method) =>
        method.Name == "AddException" && IsTaskLifecycle(method);

    internal static bool IsTaskAddExceptionForwarder(MethodInfo method) =>
        IsTaskAddException(method)
        && method.Module.Reader.GetBlobReader(method.Module.Reader.GetMethodDefinition(method.Handle).Signature).Length == 4;

    internal static bool IsTaskManagedForwarder(MethodInfo method) =>
        IsManagedTaskEvent(method) || IsTaskSchedulerId(method) || IsTaskSchedulerDequeue(method) || IsTaskSchedulerExecute(method) ||
        method.Name == ".ctor" && method.DeclaringClass.GenericArity == 1 && IsTaskLifecycle(method)
        || IsTaskAddExceptionForwarder(method);

    internal static bool IsTaskLifecycleNative(MethodInfo method) =>
        (method.Name == "TrySetResult" || method.DeclaringClass.GenericArity == 0)
        && IsTaskLifecycle(method) && !IsTaskAddExceptionForwarder(method) && !IsTaskSchedulerId(method);

    internal static bool IsTaskLifecycleSignature(string name, bool generic, BlobReader blob)
    {
        if (blob.RemainingBytes < 3 || blob.ReadByte() != 0x20
            || !blob.TryReadCompressedInteger(out int count)) return false;
        if (name == "Finish")
            return !generic && count == 1 && blob.RemainingBytes == 2
                && blob.ReadByte() == (byte)SignatureTypeCode.Void
                && blob.ReadByte() == (byte)SignatureTypeCode.Boolean;
        if (name == "AddException")
            return !generic && count is (1 or 2) && blob.RemainingBytes == count + 1
                && blob.ReadByte() == (byte)SignatureTypeCode.Void
                && blob.ReadByte() == (byte)SignatureTypeCode.Object
                && (count == 1 || blob.ReadByte() == (byte)SignatureTypeCode.Boolean);
        if (name is "TrySetResult" or "MarkStarted")
            return (!generic || name == "TrySetResult") && blob.RemainingBytes != 0
                && blob.ReadByte() == (byte)SignatureTypeCode.Boolean && count == (generic ? 1 : 0)
                && (!generic || blob.RemainingBytes == 2 && blob.ReadByte() == (byte)SignatureTypeCode.GenericTypeParameter
                    && blob.ReadByte() == 0) && blob.RemainingBytes == 0;
        if (name != ".ctor" || blob.ReadByte() != (byte)SignatureTypeCode.Void) return false;
        if (count == 0) return blob.RemainingBytes == 0;
        return count == (generic ? 2 : 3) && blob.RemainingBytes >= 3
            && blob.ReadByte() == (byte)SignatureTypeCode.Object && blob.ReadByte() == 0x11
            && blob.TryReadCompressedInteger(out int token) && (token >> 2) != 0 && (token & 3) <= 1
            && (generic || blob.RemainingBytes == 1 && blob.ReadByte() == (byte)SignatureTypeCode.Boolean)
            && blob.RemainingBytes == 0;
    }

    internal static bool IsTaskLifecycleDecodedSignature(MethodInfo method, MethodSignature<TypeDesc> signature)
    {
        bool generic = method.DeclaringClass.GenericArity == 1;
        if (signature.Header.RawValue != 0x20 || signature.GenericParameterCount != 0
            || signature.RequiredParameterCount != signature.ParameterTypes.Length) return false;
        if (method.Name == "Finish")
            return !generic && signature.ReturnType is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Void, IsCanonPlaceholder: false }
                && signature.ParameterTypes is [{ Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Boolean, IsCanonPlaceholder: false }];
        if (method.Name == "AddException")
            return !generic && signature.ReturnType is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Void, IsCanonPlaceholder: false }
                && signature.ParameterTypes.Length is (1 or 2)
                && signature.ParameterTypes[0] is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Object, IsCanonPlaceholder: false }
                && (signature.ParameterTypes.Length == 1 || signature.ParameterTypes[1] is {
                    Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Boolean, IsCanonPlaceholder: false });
        if (method.Name is "TrySetResult" or "MarkStarted")
            return (!generic || method.Name == "TrySetResult")
                && signature.ReturnType is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Boolean, IsCanonPlaceholder: false }
                && signature.ParameterTypes.Length == (generic ? 1 : 0)
                && (!generic || ReferenceEquals(signature.ParameterTypes[0], method.Context.TypeArgs[0]));
        if (signature.ReturnType is not { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Void, IsCanonPlaceholder: false })
            return false;
        if (signature.ParameterTypes.Length == 0) return true;
        if (signature.ParameterTypes.Length != (generic ? 2 : 3)
            || signature.ParameterTypes[0] is not { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Object, IsCanonPlaceholder: false }
            || signature.ParameterTypes[1] is not { Kind: TypeKind.Class, IsCanonPlaceholder: false, Class: { } options }
            || options.Module != method.Module || options.FullName != "System.Threading.Tasks.TaskCreationOptions"
            || !options.IsEnum || options.EnumUnderlying != PrimitiveTypeCode.Int32 || options.GenericArity != 0
            || options.Context.TypeArgs.Length != 0 || options.Handle.IsNil)
            return false;
        var definition = options.Module.Reader.GetTypeDefinition(options.Handle);
        return definition.GetDeclaringType().IsNil && definition.GetGenericParameters().Count == 0
            && (generic || signature.ParameterTypes[2] is {
                Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Boolean, IsCanonPlaceholder: false });
    }
}
