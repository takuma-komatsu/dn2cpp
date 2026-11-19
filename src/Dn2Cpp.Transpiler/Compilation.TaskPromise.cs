using System.Reflection.Metadata;

namespace Dn2Cpp;

internal sealed partial class Compilation
{
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
                ? !CoreIntrinsics.IsTaskSchedulerOperationName(owner.FullName, name)
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

    internal MethodInfo? TryResolveTaskLifecycleReference(Module module, MemberReferenceHandle handle, GenericContext context)
    {
        var reader = module.Reader;
        var reference = reader.GetMemberReference(handle);
        string name = reader.GetString(reference.Name);
        if (name is ("TryRunInline" or "InternalQueueTask") && reference.Parent.Kind == HandleKind.TypeSpecification)
        {
            var schedulerOwner = reader.GetTypeSpecification((TypeSpecificationHandle)reference.Parent)
                .DecodeSignature(SigProvider, context).Class;
            if (CoreIntrinsics.IsTaskSchedulerOperationName(schedulerOwner?.FullName, name))
                throw new NotSupportedException("TaskScheduler operation requires an unmodified CoreLib owner");
        }
        if (CoreIntrinsics.IsTaskSchedulerOperationName(MemberRefParentTypeName(module, handle), name))
        {
            var target = ResolveMemberRefMethod(module, handle, context);
            if (!CoreIntrinsics.IsTaskSchedulerOperation(target)
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
