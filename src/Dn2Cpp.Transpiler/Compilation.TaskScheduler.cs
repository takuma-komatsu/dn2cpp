using System.Reflection.Metadata;
using SRME = System.Reflection.Metadata.Ecma335.MetadataTokens;

namespace Dn2Cpp;

internal sealed partial class Compilation
{
    internal sealed record TaskSchedulerQueueMembers(MethodInfo Queue, MethodInfo Dequeue, MethodInfo ExceptionConstructor, MethodInfo? Inline);
    internal TaskSchedulerQueueMembers? TaskSchedulerQueue { get; private set; }
    private ClassInfo? _explicitScheduler;
    private bool _needsSchedulerInline;

    // An implicit operation can inherit a generated scheduler only after an explicit
    // dispatch made one current. Keep virtual slots out of pool-only images.
    private void NoteTaskSchedulerDispatch(MethodInfo caller, EntityHandle handle, List<Instruction> instructions,
        int index, MethodBodyBlock body, ref HashSet<int>? entries)
    {
        if (_explicitScheduler is not null && _needsSchedulerInline) return;
        var module = caller.Module;
        string? name = CallTargetMethodName(module, handle);
        bool wait = name is "Wait" or "get_Result" or "GetResult" or "HandleAsyncEntryPoint";
        if (!(wait || name is "Start" or "StartNew" or "ContinueWith" or "RunSynchronously")
            || DeclaringDefinitionOf(module, handle) is not { } owner || owner.Module.AssemblyName != "System.Private.CoreLib")
            return;
        var reader = owner.Module.Reader;
        var definition = reader.GetTypeDefinition(owner.Handle);
        if (wait)
        {
            if (_needsSchedulerInline || !IsSchedulerWaitOwner(name, RawSignatureProvider.TypeDefinitionName(reader, owner.Handle)))
                return;
        }
        else if (!reader.StringComparer.Equals(definition.Namespace, "System.Threading.Tasks")
            || !(name == "StartNew" ? reader.StringComparer.Equals(definition.Name, "TaskFactory")
                : reader.StringComparer.Equals(definition.Name, "Task")
                    || name == "ContinueWith" && reader.StringComparer.Equals(definition.Name, "Task`1"))) return;
        var member = handle.Kind == HandleKind.MethodSpecification
            ? module.Reader.GetMethodSpecification((MethodSpecificationHandle)handle).Method : handle;
        var signature = member.Kind == HandleKind.MemberReference
            ? module.Reader.GetMemberReference((MemberReferenceHandle)member).DecodeMethodSignature(SigProvider, caller.Context)
            : module.Reader.GetMethodDefinition((MethodDefinitionHandle)member).DecodeSignature(SigProvider, caller.Context);
        var parameters = signature.ParameterTypes;
        if (wait)
        {
            if (name == "HandleAsyncEntryPoint")
            {
                if (signature.Header.IsInstance || parameters.Length != 1) return;
            }
            else if (!signature.Header.IsInstance) return;
            else if (parameters.Length != 0)
            {
                if (name != "Wait" || parameters is not [{ Class: { FullName: "System.TimeSpan" } timeout }]
                    || timeout.Module != owner.Module) return;
                if (instructions[index].OpCode is ILOpCode.Call or ILOpCode.Callvirt)
                {
                    entries ??= BranchLiveness.NonFallthroughEntries(instructions, body);
                    if (KnownFiniteWait(caller, instructions, index, timeout, body.LocalVariablesInitialized, entries))
                        return;
                }
            }
            NoteTaskSchedulerRequirement(null, true);
            return;
        }
        if (!signature.Header.IsInstance) return;
        var scheduler = parameters.Length > 0 && parameters[^1] is { Kind: TypeKind.Class, Class: { FullName: "System.Threading.Tasks.TaskScheduler" } last }
            && last.Module == owner.Module ? last : null;
        if (name is "Start" or "RunSynchronously" && (!signature.ReturnType.IsVoid || parameters.Length != (scheduler is null ? 0 : 1)))
            return;
        bool inline = name == "RunSynchronously" || name == "ContinueWith"
            && parameters.Any(p => p.Class is { FullName: "System.Threading.Tasks.TaskContinuationOptions" } options && options.Module == owner.Module);
        if ((_explicitScheduler is not null || scheduler is null) && (!inline || _needsSchedulerInline)) return;
        if (instructions[index].OpCode is ILOpCode.Call or ILOpCode.Callvirt)
        {
            entries ??= BranchLiveness.NonFallthroughEntries(instructions, body);
            if (scheduler is not null && DefaultOrNullScheduler(caller, instructions, index, scheduler, body.LocalVariablesInitialized, entries))
                return;
            if (name == "ContinueWith" && inline)
                inline = ContinuationMayInline(instructions, index, scheduler is not null, entries);
        }
        if ((_explicitScheduler is not null || scheduler is null) && (!inline || _needsSchedulerInline)) return;
        NoteTaskSchedulerRequirement(scheduler, inline);
    }

    private void NoteTaskSchedulerRequirement(ClassInfo? scheduler, bool inline)
    {
        _explicitScheduler ??= scheduler;
        _needsSchedulerInline |= inline;
        if (_explicitScheduler is not null)
            foreach (var allocated in _allocatedTypeOrder.ToList())
                NoteAllocatedQueueScheduler(allocated);
    }

    private static bool IsSchedulerWaitOwner(string? name, string owner)
    {
        const string compiler = "System.Runtime.CompilerServices.";
        return name switch
        {
            "Wait" => owner == "System.Threading.Tasks.Task",
            "get_Result" => owner is "System.Threading.Tasks.Task`1" or "System.Threading.Tasks.ValueTask`1",
            "GetResult" => owner is compiler + "TaskAwaiter" or compiler + "TaskAwaiter`1"
                or compiler + "ValueTaskAwaiter" or compiler + "ValueTaskAwaiter`1"
                or compiler + "ConfiguredTaskAwaitable+ConfiguredTaskAwaiter"
                or compiler + "ConfiguredTaskAwaitable`1+ConfiguredTaskAwaiter"
                or compiler + "ConfiguredValueTaskAwaitable+ConfiguredValueTaskAwaiter"
                or compiler + "ConfiguredValueTaskAwaitable`1+ConfiguredValueTaskAwaiter",
            "HandleAsyncEntryPoint" => owner == MethodCompiler.RuntimeAsyncHelpersType,
            _ => false,
        };
    }

    // Wait(TimeSpan) offers inline execution only for an infinite timeout. A
    // dynamic value can still be infinite, even if this image also uses finite waits.
    private bool KnownFiniteWait(MethodInfo caller, List<Instruction> instructions, int index,
        ClassInfo timeout, bool initLocals, HashSet<int> entries)
    {
        int source = SchedulerArgumentProducer(instructions, index, initLocals, entries);
        if (source < 0) return false;
        var value = instructions[source];
        if (value.OpCode is not (ILOpCode.Ldsfld or ILOpCode.Call or ILOpCode.Newobj)) return false;
        var handle = SRME.EntityHandle(value.Token);
        if (value.OpCode == ILOpCode.Ldsfld)
        {
            var reader = caller.Module.Reader;
            var name = handle.Kind == HandleKind.FieldDefinition
                ? reader.GetFieldDefinition((FieldDefinitionHandle)handle).Name
                : reader.GetMemberReference((MemberReferenceHandle)handle).Name;
            return reader.StringComparer.Equals(name, "Zero") && ResolveFieldClass(caller, handle) == timeout;
        }
        string? factory = CallTargetMethodName(caller.Module, handle);
        if (factory is not (".ctor" or "FromDays" or "FromHours" or "FromMinutes" or "FromSeconds" or "FromMilliseconds" or "FromTicks")
            || QueueApiOwner(caller, handle, timeout.FullName, factory, out var signature) != timeout
            || signature.ParameterTypes.Length != 1) return false;
        int argument = SchedulerProducer(instructions, source, entries);
        if (argument >= 0 && instructions[argument].OpCode == ILOpCode.Conv_i8)
            argument = SchedulerProducer(instructions, argument, entries);
        if (argument < 0) return false;
        var number = instructions[argument];
        return number.OpCode switch
        {
            >= ILOpCode.Ldc_i4_0 and <= ILOpCode.Ldc_i4_8 => true,
            ILOpCode.Ldc_i4 or ILOpCode.Ldc_i4_s or ILOpCode.Ldc_i8 => number.Operand >= 0,
            ILOpCode.Ldc_r4 or ILOpCode.Ldc_r8 => number.FloatOperand >= 0,
            _ => false,
        };
    }

    private static int SchedulerProducer(List<Instruction> instructions, int consumer, HashSet<int> entries)
    {
        while (consumer > 0 && !entries.Contains(instructions[consumer].Offset))
        {
            consumer--;
            if (instructions[consumer].OpCode != ILOpCode.Nop) return consumer;
        }
        return -1;
    }

    private static bool ContinuationMayInline(List<Instruction> instructions, int index, bool explicitScheduler, HashSet<int> entries)
    {
        int producer = SchedulerProducer(instructions, index, entries);
        if (explicitScheduler && producer >= 0)
        {
            var op = instructions[producer].OpCode;
            if (op is not (ILOpCode.Ldarg or ILOpCode.Ldarg_s or ILOpCode.Ldarg_0 or ILOpCode.Ldarg_1 or ILOpCode.Ldarg_2 or ILOpCode.Ldarg_3
                or ILOpCode.Ldloc or ILOpCode.Ldloc_s or ILOpCode.Ldloc_0 or ILOpCode.Ldloc_1 or ILOpCode.Ldloc_2 or ILOpCode.Ldloc_3
                or ILOpCode.Ldsfld)) return true;
            producer = SchedulerProducer(instructions, producer, entries);
        }
        if (producer < 0) return true;
        var value = instructions[producer];
        return value.OpCode switch
        {
            >= ILOpCode.Ldc_i4_0 and <= ILOpCode.Ldc_i4_8 => false,
            ILOpCode.Ldc_i4 or ILOpCode.Ldc_i4_s => ((int)value.Operand & 0x80000) != 0,
            _ => true,
        };
    }

    private static int SchedulerArgumentProducer(List<Instruction> instructions, int index, bool initLocals, HashSet<int> entries)
    {
        int source = SchedulerProducer(instructions, index, entries);
        if (source < 0) return -1;
        var value = instructions[source];
        if (ILDecoder.LocalIndex(value) is int local)
        {
            if (!initLocals || value.OpCode is ILOpCode.Ldloca or ILOpCode.Ldloca_s || ILDecoder.IsStoreLocal(value.OpCode))
                return -1;
            // A path that bypasses the sole assignment retains the local's zero-initialized value.
            int store = -1;
            for (int i = 0; i < instructions.Count; i++)
            {
                var use = instructions[i];
                if (ILDecoder.LocalIndex(use) != local) continue;
                if (use.OpCode is ILOpCode.Ldloca or ILOpCode.Ldloca_s) return -1;
                if (!ILDecoder.IsStoreLocal(use.OpCode)) continue;
                if (store >= 0) return -1;
                store = i;
            }
            source = SchedulerProducer(instructions, store, entries);
        }
        return source;
    }

    private bool DefaultOrNullScheduler(MethodInfo caller, List<Instruction> instructions, int index,
        ClassInfo scheduler, bool initLocals, HashSet<int> entries)
    {
        int source = SchedulerArgumentProducer(instructions, index, initLocals, entries);
        if (source < 0) return false;
        var value = instructions[source];
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
        if (_explicitScheduler is not { } scheduler
            || TaskSchedulerQueue is { } existing && (!_needsSchedulerInline || existing.Inline is not null)
            || allocated.BaseClass is null || !DerivesFromOrIs(allocated.BaseClass, scheduler))
            return;
        EnsureCompleted(scheduler);
        MethodInfo? inline = null;
        if (_needsSchedulerInline)
        {
            inline = scheduler.Methods.SingleOrDefault(m => m.Name == "TryExecuteTaskInline" && m.IsAbstract
                && m.IsVirtual && m.VtableSlot >= 0 && m.Signature.Header.RawValue == 0x20
                && m.Signature.ReturnType.Primitive == PrimitiveTypeCode.Boolean
                && m.Signature.ParameterTypes is [{ Class: { FullName: "System.Threading.Tasks.Task" } task },
                    { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Boolean }] && task.Module == scheduler.Module)
                ?? throw new NotSupportedException("Task scheduling requires CoreLib's TryExecuteTaskInline slot");
            if (TaskSchedulerQueue is { } retained)
            {
                TaskSchedulerQueue = retained with { Inline = inline };
                ReachUsedVirtual(inline);
                return;
            }
        }
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
        TaskSchedulerQueue = new(queue, dequeue, ctor, inline);
        if (inline is not null) ReachUsedVirtual(inline);
        ReachUsedVirtual(queue);
        ReachUsedVirtual(dequeue);
        ReachAllocatedType(exception);
        Reach(ctor);
    }
}
