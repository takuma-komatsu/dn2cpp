using System.Reflection.Metadata;
using SRME = System.Reflection.Metadata.Ecma335.MetadataTokens;

namespace Dn2Cpp;

internal sealed partial class Compilation
{
    internal sealed record TaskSchedulerQueueMembers(MethodInfo Queue, MethodInfo Dequeue, MethodInfo ExceptionConstructor);
    internal TaskSchedulerQueueMembers? TaskSchedulerQueue { get; private set; }
    private ClassInfo? _explicitStartScheduler;

    // A custom queue is reachable only when Start(TaskScheduler) and a generated
    // scheduler instance can meet. Neither a parameterless Start nor a subclass
    // constructed only for its Id dispatches these slots.
    private void NoteTaskStartQueue(MethodInfo caller, EntityHandle handle, List<Instruction> instructions,
        int index, MethodBodyBlock body, ref HashSet<int>? entries)
    {
        if (_explicitStartScheduler is not null
            || QueueApiOwner(caller, handle, "System.Threading.Tasks.Task", "Start", out var signature) is not { } owner
            || signature.Header.RawValue != 0x20 || !signature.ReturnType.IsVoid || signature.ParameterTypes is not
                [{ Kind: TypeKind.Class, Class: { FullName: "System.Threading.Tasks.TaskScheduler" } scheduler }]
            || scheduler.Module != owner.Module)
            return;
        if (instructions[index].OpCode is ILOpCode.Call or ILOpCode.Callvirt)
        {
            entries ??= BranchLiveness.NonFallthroughEntries(instructions, body);
            if (DefaultOrNullStartArgument(caller, instructions, index, scheduler, body.LocalVariablesInitialized, entries))
                return;
        }
        _explicitStartScheduler = scheduler;
        foreach (var allocated in _allocatedTypeOrder.ToList())
            NoteAllocatedQueueScheduler(allocated);
    }

    private bool DefaultOrNullStartArgument(MethodInfo caller, List<Instruction> instructions, int index,
        ClassInfo scheduler, bool initLocals, HashSet<int> entries)
    {
        // A branch or handler entering past the producer supplies its own value.
        int Producer(int consumer)
        {
            while (consumer > 0 && !entries.Contains(instructions[consumer].Offset))
            {
                consumer--;
                if (instructions[consumer].OpCode != ILOpCode.Nop) return consumer;
            }
            return -1;
        }

        int source = Producer(index);
        if (source < 0) return false;
        var value = instructions[source];
        if (ILDecoder.LocalIndex(value) is int local)
        {
            if (!initLocals || value.OpCode is ILOpCode.Ldloca or ILOpCode.Ldloca_s || ILDecoder.IsStoreLocal(value.OpCode))
                return false;
            // If control bypasses the sole assignment, the reference retains its initial null.
            int store = -1;
            for (int i = 0; i < instructions.Count; i++)
            {
                var use = instructions[i];
                if (ILDecoder.LocalIndex(use) != local) continue;
                if (use.OpCode is ILOpCode.Ldloca or ILOpCode.Ldloca_s) return false;
                if (!ILDecoder.IsStoreLocal(use.OpCode)) continue;
                if (store >= 0) return false;
                store = i;
            }
            source = Producer(store);
            if (source < 0) return false;
            value = instructions[source];
        }
        return value.OpCode == ILOpCode.Ldnull || value.OpCode == ILOpCode.Call
            && QueueApiOwner(caller, SRME.EntityHandle(value.Token), scheduler.FullName, "get_Default", out var signature) == scheduler
            && signature.Header.RawValue == 0 && signature.ParameterTypes.Length == 0 && signature.ReturnType.Class == scheduler;
    }

    private ClassInfo? QueueApiOwner(MethodInfo caller, EntityHandle handle, string ownerName, string name,
        out MethodSignature<TypeDesc> signature)
    {
        var module = caller.Module;
        signature = default;
        if (CallTargetMethodName(module, handle) != name || CallTargetTypeName(module, handle) != ownerName)
            return null;
        if (handle.Kind == HandleKind.MemberReference)
        {
            var reference = module.Reader.GetMemberReference((MemberReferenceHandle)handle);
            var owner = reference.Parent.Kind == HandleKind.TypeReference
                ? ResolveTypeRef(module, (TypeReferenceHandle)reference.Parent)?.Class
                : reference.Parent.Kind == HandleKind.TypeDefinition
                    ? ResolveTypeDefMemberRefClass(module, (TypeDefinitionHandle)reference.Parent) : null;
            if (owner?.Module.AssemblyName != "System.Private.CoreLib") return null;
            signature = reference.DecodeMethodSignature(SigProvider, caller.Context);
            return owner;
        }
        if (handle.Kind == HandleKind.MethodDefinition && module.AssemblyName == "System.Private.CoreLib"
            && module.MethodMap.TryGetValue((MethodDefinitionHandle)handle, out var method))
        {
            signature = method.Signature;
            return method.DeclaringClass;
        }
        return null;
    }

    private void NoteAllocatedQueueScheduler(ClassInfo allocated)
    {
        if (TaskSchedulerQueue is not null || _explicitStartScheduler is not { } scheduler
            || allocated.BaseClass is null || !DerivesFromOrIs(allocated.BaseClass, scheduler))
            return;
        EnsureCompleted(scheduler);
        var queue = scheduler.Methods.SingleOrDefault(m => m.Name == "QueueTask" && m.IsAbstract
            && m.IsVirtual && m.VtableSlot >= 0 && m.Signature.Header.RawValue == 0x20
            && m.Signature.ReturnType.IsVoid && m.Signature.ParameterTypes is
                [{ Kind: TypeKind.Class, Class: { FullName: "System.Threading.Tasks.Task" } task }]
            && task.Module == scheduler.Module);
        var dequeue = scheduler.Methods.SingleOrDefault(CoreIntrinsics.IsTaskSchedulerDequeue);
        ClassInfo? exception = null;
        if (TypeIndex().TryGetValue(("System.Threading.Tasks", "TaskSchedulerException"), out var definitions))
            foreach (var (module, handle) in definitions)
                if (module == scheduler.Module)
                    module.ClassMap.TryGetValue(handle, out exception);
        if (exception is not null) EnsureCompleted(exception);
        var ctor = exception?.Methods.SingleOrDefault(m => m.Name == ".ctor" && m.Signature.Header.RawValue == 0x20
            && m.Signature.ReturnType.IsVoid && m.Signature.ParameterTypes is
            [{ Kind: TypeKind.Class, Class: { FullName: "System.Exception" } inner }] && inner.Module == scheduler.Module);
        if (queue is null || dequeue is not { VtableSlot: >= 0 } || ctor is null || exception is null)
            throw new NotSupportedException("Task.Start requires the CoreLib scheduler queue slots and TaskSchedulerException(Exception)");
        TaskSchedulerQueue = new(queue, dequeue, ctor);
        ReachUsedVirtual(queue);
        ReachUsedVirtual(dequeue);
        ReachAllocatedType(exception);
        Reach(ctor);
    }
}
