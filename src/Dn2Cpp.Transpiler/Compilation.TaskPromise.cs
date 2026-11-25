using System.Reflection;
using System.Reflection.Metadata;

namespace Dn2Cpp;

internal sealed partial class Compilation
{
    internal bool IsTaskExceptionDelegateArgument(TypeDesc argument) => IsReferenceArg(argument);

    internal bool IsTaskSchedulerDelegateArgument(TypeDesc argument, ClassInfo parameter)
    {
        if (argument is not { Kind: TypeKind.Class, IsCanonPlaceholder: false,
            Class: { IsValueType: false, IsInterface: false } actual }) return false;
        var visited = new HashSet<ClassInfo>();
        for (var current = actual; current is not null && visited.Add(current);)
        {
            if (current == parameter) return true;
            var next = EnsureShape(current).BaseClass;
            // Runtime-owned generic shells omit BaseClass, not their metadata ancestry.
            if (next is null && current.IntrinsicCppName is not null && !current.Handle.IsNil)
            {
                var baseType = current.Module.Reader.GetTypeDefinition(current.Handle).BaseType;
                if (!baseType.IsNil)
                    next = ResolveTypeTokenForScan(current.Module, baseType, current.Context)?.Class;
            }
            current = next;
        }
        return false;
    }

    // Task<T> keeps its runtime-owned layout. Lifecycle method shells decode no TPL
    // fields or unrelated signatures and bodies.
    private void CompleteTaskLifecycleMembers(ClassInfo owner)
    {
        var reader = owner.Module.Reader;
        foreach (var handle in reader.GetTypeDefinition(owner.Handle).GetMethods())
        {
            var definition = reader.GetMethodDefinition(handle);
            string name = reader.GetString(definition.Name);
            if (owner.FullName == "System.Threading.Tasks.TaskScheduler"
                ? !(CoreIntrinsics.IsTaskSchedulerMemberName(owner.FullName, name) || CoreIntrinsics.IsManagedTaskEventName(name))
                : !CoreIntrinsics.IsTaskLifecycleCandidate(name, reader.GetBlobReader(definition.Signature))) continue;
            var method = new MethodInfo
            {
                DeclaringClass = owner, Name = name, Handle = handle, Module = owner.Module,
                Context = owner.Context, Attributes = definition.Attributes,
                ImplAttributes = definition.ImplAttributes, Rva = definition.RelativeVirtualAddress,
                PInvoke = ReadPInvoke(owner.Module, definition),
            };
            owner.Methods.Add(method);
            owner.MethodByTemplate.Add(handle, method);
        }
    }

    private void NoteUnobservedTaskEventStorage(MethodInfo method)
    {
        var field = method.DeclaringClass.Fields.SingleOrDefault(f =>
            f.Name == "UnobservedTaskException" && f.Attributes == (FieldAttributes.Private | FieldAttributes.Static)
            && CoreIntrinsics.IsUnobservedTaskEventHandler(f.Type))
            ?? throw new NotSupportedException("No exact CoreLib UnobservedTaskException backing field");
        if (IntrinsicManagedStaticFields.Add(field))
            NoteReferencedType(field.Type.Class!);
    }

    internal MethodInfo? TryResolveTaskLifecycleReference(Module module, MemberReferenceHandle handle, GenericContext context)
    {
        var reader = module.Reader;
        var reference = reader.GetMemberReference(handle);
        string name = reader.GetString(reference.Name);
        if (CoreIntrinsics.IsManagedTaskEventName(name))
        {
            if (reference.Parent.Kind == HandleKind.TypeSpecification
                && reader.GetTypeSpecification((TypeSpecificationHandle)reference.Parent)
                    .DecodeSignature(SigProvider, context).Class?.FullName == "System.Threading.Tasks.TaskScheduler")
                throw new NotSupportedException("UnobservedTaskException requires an unmodified CoreLib owner");
            if (MemberRefParentTypeName(module, handle) == "System.Threading.Tasks.TaskScheduler")
            {
                if (reference.Parent.Kind is not (HandleKind.TypeReference or HandleKind.TypeDefinition))
                    throw new NotSupportedException("UnobservedTaskException requires its exact CoreLib owner");
                var target = ResolveMemberRefMethod(module, handle, context);
                var signature = reference.DecodeMethodSignature(SigProvider, context);
                var raw = reader.GetBlobReader(reference.Signature);
                if (!CoreIntrinsics.IsManagedTaskEvent(target)
                    || raw.RemainingBytes < 5 || raw.ReadByte() != 0
                    || !raw.TryReadCompressedInteger(out int count) || count != 2
                    || raw.ReadByte() != (byte)SignatureTypeCode.Void
                    || raw.ReadByte() != (byte)SignatureTypeCode.Object
                    || raw.ReadByte() != 0x12
                    || !raw.TryReadCompressedInteger(out _) || raw.RemainingBytes != 0
                    || signature.Header.RawValue != 0 || signature.GenericParameterCount != 0
                    || signature.RequiredParameterCount != 2
                    || signature.ReturnType != target.Signature.ReturnType
                    || !signature.ParameterTypes.SequenceEqual(target.Signature.ParameterTypes))
                    throw new NotSupportedException("UnobservedTaskException requires its exact CoreLib reference and declaration");
                return target;
            }
        }
        if (name is ("TryRunInline" or "InternalQueueTask" or "get_Id" or "TryDequeue" or "TryExecuteTask") && reference.Parent.Kind == HandleKind.TypeSpecification)
        {
            var schedulerOwner = reader.GetTypeSpecification((TypeSpecificationHandle)reference.Parent)
                .DecodeSignature(SigProvider, context).Class;
            if (CoreIntrinsics.IsTaskSchedulerMemberName(schedulerOwner?.FullName, name))
                throw new NotSupportedException("TaskScheduler operation requires an unmodified CoreLib owner");
        }
        if (CoreIntrinsics.IsTaskSchedulerMemberName(MemberRefParentTypeName(module, handle), name))
        {
            var target = ResolveMemberRefMethod(module, handle, context);
            if (!CoreIntrinsics.IsTaskSchedulerMember(target)
                || !CoreIntrinsics.IsTaskSchedulerOperationSignature(name, reader.GetBlobReader(reference.Signature))
                || !CoreIntrinsics.IsTaskSchedulerOperationDecodedSignature(target,
                    reference.DecodeMethodSignature(SigProvider, target.DeclaringClass.Context)))
                throw new NotSupportedException("TaskScheduler operation requires its exact CoreLib reference and declaration");
            return target;
        }
        if (!CoreIntrinsics.IsTaskLifecycleCandidate(name, reader.GetBlobReader(reference.Signature))) return null;
        ClassInfo? owner;
        if (reference.Parent.Kind == HandleKind.TypeSpecification)
        {
            var blob = reader.GetBlobReader(reader.GetTypeSpecification((TypeSpecificationHandle)reference.Parent).Signature);
            var type = reader.GetTypeSpecification((TypeSpecificationHandle)reference.Parent).DecodeSignature(SigProvider, context);
            owner = type.Class;
            if (owner is null || !CoreIntrinsics.IsTaskLifecycleOwner(owner)) return null;
            if (blob.RemainingBytes < 2 || blob.ReadByte() != 0x15 || blob.ReadByte() != 0x12)
                throw new NotSupportedException("Task lifecycle member requires an unmodified CoreLib owner");
        }
        else
        {
            string? full = MemberRefParentTypeName(module, handle);
            if (full is not ("System.Threading.Tasks.Task" or "System.Threading.Tasks.Task`1")) return null;
            owner = reference.Parent.Kind == HandleKind.TypeReference
                ? ResolveTypeRef(module, (TypeReferenceHandle)reference.Parent)?.Class
                : reference.Parent.Kind == HandleKind.TypeDefinition
                    ? ResolveTypeDefMemberRefClass(module, (TypeDefinitionHandle)reference.Parent) : null;
        }
        if (owner is null) throw new NotSupportedException("Task lifecycle member requires its loaded CoreLib declaration");
        var method = ResolveMemberRefMethod(module, handle, context);
        if (!CoreIntrinsics.IsTaskLifecycle(method)
            || !CoreIntrinsics.IsTaskLifecycleSignature(name, owner.GenericArity == 1, reader.GetBlobReader(reference.Signature))
            || !CoreIntrinsics.IsTaskLifecycleDecodedSignature(method, reference.DecodeMethodSignature(SigProvider, owner.Context)))
            throw new NotSupportedException("Task lifecycle member requires its exact CoreLib reference and declaration");
        return method;
    }
}
