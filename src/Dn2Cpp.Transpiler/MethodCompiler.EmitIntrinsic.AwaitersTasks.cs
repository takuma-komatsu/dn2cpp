using System.Reflection.Metadata;
using System.Text;
using SRME = System.Reflection.Metadata.Ecma335.MetadataTokens;

namespace Dn2Cpp;

internal sealed partial class MethodCompiler
{
    private string TaskTypeInfo(TypeDesc managedType)
    {
        if (managedType is not { Kind: TypeKind.Class, Class.IntrinsicCppName: "Dn2CppTask*" })
            throw new InvalidOperationException(
                $"{Method.DeclaringClass.FullName}.{Method.Name}: cannot stamp non-Task type {managedType}");
        return TypeInfoExpr(managedType)
            ?? throw new InvalidOperationException(
                $"{Method.DeclaringClass.FullName}.{Method.Name}: Task type {managedType} has no runtime identity");
    }

    private string StampTask(string expression, TypeDesc managedType) =>
        $"dn2cpp_task_stamp({expression}, {TaskTypeInfo(managedType)})";

    private void PushStampedTask(string expression, TypeDesc managedType) =>
        Push(StackKind.Ref, "Dn2CppTask*", StampTask(expression, managedType));

    private TypeDesc TaskTypeForResult(TypeDesc resultType)
    {
        TaintIfCanonical(resultType, "task typeinfo");
        var task = Comp.FindGenericInstantiation("System.Threading.Tasks.Task", [resultType])
            ?? throw new InvalidOperationException(
                $"{Method.DeclaringClass.FullName}.{Method.Name}: Task<{resultType}> is not in the completed image");
        return TypeDesc.MakeClass(task);
    }

    private TypeDesc? TaskBackingType(TypeDesc taskLikeType)
    {
        if (taskLikeType is not { Kind: TypeKind.Class, Class: { } cls })
            return null;
        if (cls.IntrinsicCppName == "Dn2CppTask*")
            return taskLikeType;
        if (Comp.GenericDefFullName(cls) == "System.Threading.Tasks.ValueTask"
            || Comp.AdoptedAsyncKey(cls) == "System.Threading.Tasks.ValueTask")
            return cls.Context.TypeArgs.Length == 0
                ? Comp.FindClassByFullName("System.Threading.Tasks.Task") is { } task
                    ? TypeDesc.MakeClass(task) : null
                : TaskTypeForResult(cls.Context.TypeArgs[0]);
        return null;
    }

    /// <summary>Read the result out of an awaited task (<c>taskExpr</c> must already be
    /// normalized through <c>dn2cpp_vtask</c> for the ValueTask family): block until it
    /// leaves the pending state, re-raise its fault, then push the result unless the
    /// awaiter is the void one.
    ///
    /// <para>The block is what makes the sync-over-async idiom correct: without it a
    /// still-PENDING task's unset result slot is read as the answer, so an
    /// <c>async ValueTask&lt;T&gt;</c> method that suspends silently returns default(T).
    /// <c>dn2cpp_vts_block</c> returns immediately on a completed task, so the
    /// await-resume path is unchanged. It blocks for a builder- or Task-backed ValueTask,
    /// as the CLR does, and for a still-pending <c>IValueTaskSource</c>-backed one reads
    /// the source's own GetResult instead — the read the CLR refuses, refused by the
    /// source itself so the exception is the one real .NET raises.</para>
    ///
    /// <para><c>taskExpr</c> is re-evaluated per statement rather than spilled to a temp:
    /// it is a pure read off an already-spilled stack temp, and minting a temp here would
    /// renumber every later temp in the method.</para>
    /// </summary>
    private void EmitAwaitedTaskResult(string taskExpr, TypeDesc returnType)
    {
        Emit($"dn2cpp_vts_block({taskExpr});");
        Emit($"dn2cpp_task_throw_if_faulted({taskExpr});");
        if (!returnType.IsVoid) // the ValueTask<T> / ValueTaskAwaiter<T> forms
            PushTaskResult(taskExpr, returnType);
    }

    private bool TryEmitAwaitersTasksIntrinsic(string declType, string name, MethodSignature<TypeDesc> sig)
    {
        switch (declType, name)
        {

            case ("System.Runtime.CompilerServices.TaskAwaiter", "get_IsCompleted"):
            {
                // A task is "completed" once it leaves the pending state (success
                // or fault); a pending task drives MoveNext down the suspend path.
                var a = Pop();
                Push(StackKind.I4, "int32_t",
                    $"(((Dn2CppTaskAwaiter*)({a.Expr}))->task->status != DN2CPP_TASK_PENDING ? 1 : 0)");
                return true;
            }
            case ("System.Runtime.CompilerServices.TaskAwaiter", "GetResult"):
            {
                var a = Pop();
                // The sync-over-async idiom: on a still-PENDING task this must block
                // (drain the scheduler, like Task.Result), not read the unset result
                // slot. dn2cpp_task_block returns immediately on a completed task, so
                // the await-resume path is unchanged.
                Emit($"dn2cpp_task_block(((Dn2CppTaskAwaiter*)({a.Expr}))->task);");
                Emit($"dn2cpp_task_throw_if_faulted(((Dn2CppTaskAwaiter*)({a.Expr}))->task);");
                if (!sig.ReturnType.IsVoid) // TaskAwaiter<T>.GetResult
                    PushTaskResult($"(((Dn2CppTaskAwaiter*)({a.Expr}))->task)", sig.ReturnType);
                return true;
            }
            // Direct awaiter.OnCompleted(Action) — the delegate-continuation form. A
            // custom suspending awaitable that wraps a Task forwards its continuation
            // here (e.g. `_t.GetAwaiter.OnCompleted(cont)`); register the Action to
            // run when the task completes.
            case ("System.Runtime.CompilerServices.TaskAwaiter", "OnCompleted"):
            case ("System.Runtime.CompilerServices.TaskAwaiter", "UnsafeOnCompleted"):
            {
                var cont = Pop(); // Action continuation
                var a = Pop();    // this (ref Dn2CppTaskAwaiter)
                Emit($"dn2cpp_task_on_completed_action(((Dn2CppTaskAwaiter*)({a.Expr}))->task, " +
                     $"{Cast(cont, "Dn2CppObject*")});");
                return true;
            }

            // task.ConfigureAwait(bool) -> ConfiguredTaskAwaitable wrapping the same
            // task (a no-op on the single-threaded model). The awaitable and its nested
            // ConfiguredTaskAwaiter both lower to Dn2CppTaskAwaiter, so GetAwaiter is an
            // identity copy and the await-suspend path matches the Task case.
            case ("System.Threading.Tasks.Task", "ConfigureAwait"):
            {
                Pop(); // bool continueOnCapturedContext (unmodeled)
                var t = Pop();
                Push(StackKind.Struct, "Dn2CppTaskAwaiter", $"Dn2CppTaskAwaiter{{ (Dn2CppTask*)({t.Expr}) }}");
                return true;
            }
            case ("System.Runtime.CompilerServices.ConfiguredTaskAwaitable", "GetAwaiter"):
            {
                var a = Pop(); // ref awaitable (already a Dn2CppTaskAwaiter)
                Push(StackKind.Struct, "Dn2CppTaskAwaiter", $"(*(Dn2CppTaskAwaiter*)({a.Expr}))");
                return true;
            }
            case ("ConfiguredTaskAwaiter", "get_IsCompleted"):
            {
                var a = Pop();
                Push(StackKind.I4, "int32_t",
                    $"(((Dn2CppTaskAwaiter*)({a.Expr}))->task->status != DN2CPP_TASK_PENDING ? 1 : 0)");
                return true;
            }
            case ("ConfiguredTaskAwaiter", "GetResult"):
            {
                var a = Pop();
                // Blocks on a still-pending task, exactly like TaskAwaiter.GetResult
                // above — `t.ConfigureAwait(false).GetAwaiter().GetResult()` is the same
                // sync-over-async idiom.
                Emit($"dn2cpp_task_block(((Dn2CppTaskAwaiter*)({a.Expr}))->task);");
                Emit($"dn2cpp_task_throw_if_faulted(((Dn2CppTaskAwaiter*)({a.Expr}))->task);");
                if (!sig.ReturnType.IsVoid) // ConfiguredTaskAwaiter<T>.GetResult
                    PushTaskResult($"(((Dn2CppTaskAwaiter*)({a.Expr}))->task)", sig.ReturnType);
                return true;
            }
            // Same delegate-continuation form as TaskAwaiter above: a custom
            // awaitable (e.g. a third-party async task type's builder) forwards
            // its Action continuation through the configured awaiter.
            case ("ConfiguredTaskAwaiter", "OnCompleted"):
            case ("ConfiguredTaskAwaiter", "UnsafeOnCompleted"):
            {
                var cont = Pop(); // Action continuation
                var a = Pop();    // this (ref Dn2CppTaskAwaiter)
                Emit($"dn2cpp_task_on_completed_action(((Dn2CppTaskAwaiter*)({a.Expr}))->task, " +
                     $"{Cast(cont, "Dn2CppObject*")});");
                return true;
            }

            // valueTask.ConfigureAwait(bool) -> ConfiguredValueTaskAwaitable(<T>)
            // wrapping the same task — the exact mirror of the Task case above.
            // ValueTask is a struct, so the receiver is a ref to the {task} struct.
            case ("System.Threading.Tasks.ValueTask", "ConfigureAwait"):
            {
                Pop(); // bool continueOnCapturedContext (unmodeled)
                var v = Pop(); // ref this (ValueTask = {task} struct)
                Push(StackKind.Struct, "Dn2CppTaskAwaiter",
                    $"Dn2CppTaskAwaiter{{ ((Dn2CppTaskAwaiter*)({v.Expr}))->task }}");
                return true;
            }
            case ("System.Runtime.CompilerServices.ConfiguredValueTaskAwaitable", "GetAwaiter"):
            {
                var a = Pop(); // ref awaitable (already a Dn2CppTaskAwaiter)
                Push(StackKind.Struct, "Dn2CppTaskAwaiter", $"(*(Dn2CppTaskAwaiter*)({a.Expr}))");
                return true;
            }
            // A ValueTask-derived awaiter may carry a NULL task — default(ValueTask)
            // is the BCL's "completed successfully" encoding (see dn2cpp_vtask in
            // dn2cpp.h) — so its readers normalize through dn2cpp_vtask.
            case ("ConfiguredValueTaskAwaiter", "get_IsCompleted"):
            {
                var a = Pop();
                Push(StackKind.I4, "int32_t",
                    $"(dn2cpp_vtask(((Dn2CppTaskAwaiter*)({a.Expr}))->task)->status != DN2CPP_TASK_PENDING ? 1 : 0)");
                return true;
            }
            case ("ConfiguredValueTaskAwaiter", "GetResult"):
            {
                var a = Pop();
                EmitAwaitedTaskResult($"dn2cpp_vtask(((Dn2CppTaskAwaiter*)({a.Expr}))->task)", sig.ReturnType);
                return true;
            }
            // Delegate-continuation form; the NULL-task ("completed") encoding
            // normalizes through dn2cpp_vtask like the readers above.
            case ("ConfiguredValueTaskAwaiter", "OnCompleted"):
            case ("ConfiguredValueTaskAwaiter", "UnsafeOnCompleted"):
            {
                var cont = Pop(); // Action continuation
                var a = Pop();    // this (ref Dn2CppTaskAwaiter)
                Emit($"dn2cpp_task_on_completed_action(dn2cpp_vtask(((Dn2CppTaskAwaiter*)({a.Expr}))->task), " +
                     $"{Cast(cont, "Dn2CppObject*")});");
                return true;
            }

            // ---- AsyncIteratorMethodBuilder (`async IAsyncEnumerable<T>`) ----
            // Five members: Create, Complete, and the generic MoveNext / AwaitOnCompleted /
            // AwaitUnsafeOnCompleted drivers (TranslateAsyncGenericIntrinsic). Nothing ever
            // reads a task back out of it — an async iterator's results and faults flow
            // through the state machine's OWN ManualResetValueTaskSourceCore promise.
            case ("System.Runtime.CompilerServices.AsyncIteratorMethodBuilder", "Create"):
                Push(StackKind.Struct, "Dn2CppAsyncBuilder", "Dn2CppAsyncBuilder{ dn2cpp_task_alloc() }");
                return true;
            case ("System.Runtime.CompilerServices.AsyncIteratorMethodBuilder", "Complete"):
                // Dropped: completing the builder's unobservable task would hand the
                // scheduler an in-flight-accounting event for a task nobody awaits. The
                // enumerator's terminal state is already published by the promise
                // (SetResult(false) / SetException) the IL runs right next to this call.
                Pop(); // builder receiver
                return true;

            // ---- ValueTask / ValueTask<T> ----
            // A ValueTask is the same {task} struct as a TaskAwaiter (always backed by a
            // Dn2CppTask) and its builder reuses Dn2CppAsyncBuilder, so the whole
            // await/suspend/resume + fault path is the Task machinery.
            // PoolingAsyncValueTaskMethodBuilder(<T>) shares every case: pooling is a CLR
            // state-machine-box allocation optimization this task model has no equivalent
            // of, so it lowers exactly like the non-pooling builder.
            case ("System.Runtime.CompilerServices.AsyncValueTaskMethodBuilder", "Create"):
            case ("System.Runtime.CompilerServices.PoolingAsyncValueTaskMethodBuilder", "Create"):
                Push(StackKind.Struct, "Dn2CppAsyncBuilder",
                    "Dn2CppAsyncBuilder{ dn2cpp_task_alloc() }");
                return true;
            case ("System.Runtime.CompilerServices.AsyncValueTaskMethodBuilder", "get_Task"):
            case ("System.Runtime.CompilerServices.PoolingAsyncValueTaskMethodBuilder", "get_Task"):
            {
                // get_Task returns a ValueTask<T> — the {task} struct over the builder's task.
                var b = Pop();
                string task = $"((Dn2CppAsyncBuilder*)({b.Expr}))->task";
                if (TaskBackingType(sig.ReturnType) is { } taskType)
                    task = StampTask(task, taskType);
                Push(StackKind.Struct, "Dn2CppTaskAwaiter", $"Dn2CppTaskAwaiter{{ {task} }}");
                return true;
            }
            case ("System.Runtime.CompilerServices.AsyncValueTaskMethodBuilder", "SetResult"):
            case ("System.Runtime.CompilerServices.PoolingAsyncValueTaskMethodBuilder", "SetResult"):
            {
                if (sig.ParameterTypes.Length == 1)
                {
                    var v = Pop();
                    var b = Pop();
                    string store = EmitTaskResultStore(v);
                    Emit($"dn2cpp_task_set_result(((Dn2CppAsyncBuilder*)({b.Expr}))->task, {store});");
                }
                else
                {
                    var b = Pop();
                    Emit($"dn2cpp_task_set_result(((Dn2CppAsyncBuilder*)({b.Expr}))->task, 0);");
                }
                return true;
            }
            case ("System.Runtime.CompilerServices.AsyncValueTaskMethodBuilder", "SetException"):
            case ("System.Runtime.CompilerServices.PoolingAsyncValueTaskMethodBuilder", "SetException"):
            {
                var e = Pop();
                var b = Pop();
                Emit($"dn2cpp_task_set_exception(((Dn2CppAsyncBuilder*)({b.Expr}))->task, (Dn2CppObject*)({e.Expr}));");
                return true;
            }
            case ("System.Runtime.CompilerServices.AsyncValueTaskMethodBuilder", "SetStateMachine"):
            case ("System.Runtime.CompilerServices.PoolingAsyncValueTaskMethodBuilder", "SetStateMachine"):
                Pop(); // arg
                Pop(); // this
                return true;

            // ---- AsyncVoidMethodBuilder (the `async void` driver) ----
            // Same Dn2CppAsyncBuilder over a task and the same state-machine drive path
            // (Start / AwaitOnCompleted / AwaitUnsafeOnCompleted route through
            // TranslateAsyncGenericIntrinsic). Only the ends differ: an async void exposes
            // no Task, so there is no get_Task, SetResult takes no result value, and
            // SetException re-raises off the synchronous stack instead of faulting an
            // awaitable Task. The allocated task is never observed, so an uncompleted one
            // is simply GC'd.
            case ("System.Runtime.CompilerServices.AsyncVoidMethodBuilder", "Create"):
                Push(StackKind.Struct, "Dn2CppAsyncBuilder", "Dn2CppAsyncBuilder{ dn2cpp_task_alloc() }");
                return true;
            case ("System.Runtime.CompilerServices.AsyncVoidMethodBuilder", "SetResult"):
            {
                // No result value — completing the unobserved task fires no awaiters (an
                // async void has none), it just settles the uniform struct's task.
                var b = Pop();
                Emit($"dn2cpp_task_set_result(((Dn2CppAsyncBuilder*)({b.Expr}))->task, 0);");
                return true;
            }
            case ("System.Runtime.CompilerServices.AsyncVoidMethodBuilder", "SetException"):
            {
                // Real .NET surfaces an async void fault as an unhandled exception on the
                // captured SynchronizationContext (or the ThreadPool), crashing the process.
                // dn2cpp_task_throw_async models that, so the fault is re-raised rather than
                // swallowed onto the builder's unobserved task. The SynchronizationContext is
                // not honored, matching the dropped-context treatment across the threading
                // surface.
                var e = Pop();
                var b = Pop(); // builder receiver — the fault does not flow through its task
                Emit($"dn2cpp_task_throw_async((Dn2CppObject*)({e.Expr}), nullptr);");
                return true;
            }
            case ("System.Runtime.CompilerServices.AsyncVoidMethodBuilder", "SetStateMachine"):
                Pop(); // arg
                Pop(); // this
                return true;

            // The `call instance.ctor` form (`var vt = new ValueTask<T>(x)` lowers to
            // ldloca + call, not newobj): write the {task} struct through the address.
            // (The newobj expression form is handled in TranslateNewobj.)
            case ("System.Threading.Tasks.ValueTask", ".ctor"):
            {
                // The source-backed 2-arg ctor (IValueTaskSource(<T>), short) — bridge the
                // source onto a pending runtime task, exactly as the newobj form does.
                // It MUST be matched on the parameter shape before the 1-arg arms: those
                // pop a single argument, so letting a 2-arg ctor fall into them would
                // silently leave the token on the IL stack and shift every later operand.
                if (sig.ParameterTypes is [{ Kind: TypeKind.Class, Class: { IsInterface: true } vtsItf },
                                            { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int16 }])
                {
                    string awaiter = ValueTaskSourceAwaiter(vtsItf);
                    var vtsSelf = Pop(); // ref this (address of the ValueTask being initialized)
                    Emit($"*(Dn2CppTaskAwaiter*)({vtsSelf.Expr}) = {awaiter};");
                    return true;
                }
                if (sig.ParameterTypes.Length != 1)
                    throw new NotSupportedException(
                        $"{Method.DeclaringClass.FullName}.{Method.Name}: ValueTask ctor shape " +
                        $"({sig.ParameterTypes.Length} args) has no intrinsic mapping");
                var arg = Pop();
                var self = Pop(); // ref this (address of the ValueTask being initialized)
                bool fromTask = sig.ParameterTypes is [{ Kind: TypeKind.Class, Class: { } pcl }]
                    && Comp.GenericDefFullName(pcl) == "System.Threading.Tasks.Task";
                string task = fromTask
                    ? $"(Dn2CppTask*)({arg.Expr})"
                    : StampTask($"dn2cpp_task_from_result({EmitTaskResultStore(arg)})",
                        TaskTypeForResult(sig.ParameterTypes[0]));
                Emit($"*(Dn2CppTaskAwaiter*)({self.Expr}) = Dn2CppTaskAwaiter{{ {task} }};");
                return true;
            }
            case ("System.Threading.Tasks.ValueTask", "GetAwaiter"):
            {
                var v = Pop(); // ref ValueTask (a {task} struct) -> ValueTaskAwaiter (same struct)
                Push(StackKind.Struct, "Dn2CppTaskAwaiter", $"(*(Dn2CppTaskAwaiter*)({v.Expr}))");
                return true;
            }
            // The ValueTask read paths normalize a possibly-NULL task through
            // dn2cpp_vtask: default(ValueTask) is the BCL's "completed successfully"
            // encoding (`_obj == null`), produced by e.g. MemoryStream.WriteAsync's
            // synchronous-completion `return default`.
            case ("System.Threading.Tasks.ValueTask", "AsTask"):
            {
                var v = Pop();
                Push(StackKind.Ref, "Dn2CppTask*",
                    $"dn2cpp_vtask_as_task(((Dn2CppTaskAwaiter*)({v.Expr}))->task, "
                    + $"{TaskTypeInfo(sig.ReturnType)})");
                return true;
            }
            case ("System.Threading.Tasks.ValueTask", "get_IsCompleted"):
            {
                var v = Pop();
                Push(StackKind.I4, "int32_t",
                    $"(dn2cpp_vtask(((Dn2CppTaskAwaiter*)({v.Expr}))->task)->status != DN2CPP_TASK_PENDING ? 1 : 0)");
                return true;
            }
            case ("System.Threading.Tasks.ValueTask", "get_Result"):
            {
                var v = Pop();
                EmitAwaitedTaskResult($"dn2cpp_vtask(((Dn2CppTaskAwaiter*)({v.Expr}))->task)", sig.ReturnType);
                return true;
            }
            // ValueTask.FromCanceled(CancellationToken) -> a pre-completed CANCELED
            // ValueTask (the {task} struct over a canceled task; awaiting it throws
            // OperationCanceledException, like an awaited canceled Task.Delay). The
            // generic FromCanceled<TResult> binds in TranslateGenericIntrinsic. Real
            // .NET throws ArgumentOutOfRangeException when the token is NOT canceled;
            // that validation is not modeled (the guard callers only take the branch
            // on an already-canceled token).
            case ("System.Threading.Tasks.ValueTask", "FromCanceled"):
            {
                Pop(); // CancellationToken (its canceled state is not re-validated)
                EmitCanceledExcRegistration();
                string task = TaskBackingType(sig.ReturnType) is { } taskType
                    ? StampTask("dn2cpp_task_from_canceled()", taskType)
                    : "dn2cpp_task_from_canceled()";
                Push(StackKind.Struct, "Dn2CppTaskAwaiter", $"Dn2CppTaskAwaiter{{ {task} }}");
                return true;
            }
            // ValueTask.FromException(Exception) -> a pre-completed faulted ValueTask;
            // the mirror of Task.FromException, wrapped in the {task} struct.
            case ("System.Threading.Tasks.ValueTask", "FromException") when sig.ParameterTypes.Length == 1:
            {
                var ex = Pop();
                string task = $"dn2cpp_task_from_exception({Cast(ex, "Dn2CppObject*")})";
                if (TaskBackingType(sig.ReturnType) is { } taskType)
                    task = StampTask(task, taskType);
                Push(StackKind.Struct, "Dn2CppTaskAwaiter",
                    $"Dn2CppTaskAwaiter{{ {task} }}");
                return true;
            }
            // ValueTask.CompletedTask -> a pre-completed successful ValueTask; the
            // mirror of Task.CompletedTask wrapped in the {task} struct. Reached as
            // OSFileStreamStrategy.DisposeAsync's nothing-to-flush arm.
            case ("System.Threading.Tasks.ValueTask", "get_CompletedTask"):
            {
                string task = TaskBackingType(sig.ReturnType) is { } taskType
                    ? StampTask("dn2cpp_task_completed()", taskType)
                    : "dn2cpp_task_completed()";
                Push(StackKind.Struct, "Dn2CppTaskAwaiter", $"Dn2CppTaskAwaiter{{ {task} }}");
                return true;
            }
            case ("System.Threading.Tasks.ValueTask", "get_IsCompletedSuccessfully"):
            {
                var v = Pop();
                Push(StackKind.I4, "int32_t",
                    $"(dn2cpp_vtask(((Dn2CppTaskAwaiter*)({v.Expr}))->task)->status == DN2CPP_TASK_SUCCEEDED ? 1 : 0)");
                return true;
            }
            case ("System.Runtime.CompilerServices.ValueTaskAwaiter", "get_IsCompleted"):
            {
                var a = Pop();
                Push(StackKind.I4, "int32_t",
                    $"(dn2cpp_vtask(((Dn2CppTaskAwaiter*)({a.Expr}))->task)->status != DN2CPP_TASK_PENDING ? 1 : 0)");
                return true;
            }
            case ("System.Runtime.CompilerServices.ValueTaskAwaiter", "GetResult"):
            {
                var a = Pop();
                EmitAwaitedTaskResult($"dn2cpp_vtask(((Dn2CppTaskAwaiter*)({a.Expr}))->task)", sig.ReturnType);
                return true;
            }
            // Delegate-continuation form (mirrors TaskAwaiter.OnCompleted): a
            // custom async task type's builder awaiting a ValueTask forwards its
            // Action continuation here. dn2cpp_vtask normalizes the NULL-task
            // ("completed") encoding to a real completed task first.
            case ("System.Runtime.CompilerServices.ValueTaskAwaiter", "OnCompleted"):
            case ("System.Runtime.CompilerServices.ValueTaskAwaiter", "UnsafeOnCompleted"):
            {
                var cont = Pop(); // Action continuation
                var a = Pop();    // this (ref Dn2CppTaskAwaiter)
                Emit($"dn2cpp_task_on_completed_action(dn2cpp_vtask(((Dn2CppTaskAwaiter*)({a.Expr}))->task), " +
                     $"{Cast(cont, "Dn2CppObject*")});");
                return true;
            }

            // ---- cancellation ----
            case ("System.Threading.CancellationTokenSource", "Cancel"):
            {
                for (int i = 1; i < sig.ParameterTypes.Length; i++)
                    Pop(); // throwOnFirstException (bool) — unmodeled
                var s = Pop();
                Emit($"dn2cpp_cts_cancel((Dn2CppCancelSource*)({s.Expr}));");
                return true;
            }
            case ("System.Threading.CancellationTokenSource", "get_Token"):
            {
                var s = Pop();
                Push(StackKind.Struct, "Dn2CppCancelToken", $"Dn2CppCancelToken{{ (Dn2CppCancelSource*)({s.Expr}) }}");
                return true;
            }
            case ("System.Threading.CancellationTokenSource", "get_IsCancellationRequested"):
            {
                var s = Pop();
                Push(StackKind.I4, "int32_t", $"dn2cpp_cts_is_cancelled((Dn2CppCancelSource*)({s.Expr}))");
                return true;
            }
            // CancelAfter(TimeSpan) / CancelAfter(int): schedule the source to cancel after a
            // delay, on a dedicated one-shot timer thread. HttpClient.Timeout (default 100 s)
            // reaches here through PrepareCancellationTokenSource, so both overloads must map.
            case ("System.Threading.CancellationTokenSource", "CancelAfter"):
            {
                var arg = Pop();  // TimeSpan delay or int millisecondsDelay
                var s = Pop();    // this
                string ms = sig.ParameterTypes is [{ } p] && IsTimeSpan(p)
                    ? $"(int64_t)dn2cpp_timespan_total({TSVal(arg)}, 10000LL)" // TimeSpan -> total ms
                    : $"(int64_t)({arg.Expr})";                                // millisecondsDelay
                Emit($"dn2cpp_cts_cancel_after((Dn2CppCancelSource*)({s.Expr}), {ms});");
                return true;
            }
            // Dispose(): the source is GC-managed, so nothing is released — except the
            // pending CancelAfter, which real .NET's Dispose stops. Most disposals do NOT
            // arrive here: `using (var cts = ...)` lowers to `callvirt IDisposable::Dispose`
            // and is devirtualized in MethodCompiler.TranslateCall, which must stay in step
            // with this arm.
            case ("System.Threading.CancellationTokenSource", "Dispose"):
            {
                var d = Pop(); // this
                Emit($"dn2cpp_cts_dispose((Dn2CppCancelSource*)({d.Expr}));");
                return true;
            }
            // CancellationTokenSource.CreateLinkedTokenSource(...): a source that cancels
            // when any of its tokens does. All three overloads together — the type is
            // intrinsic, so a sibling left unmapped is not a fallback to real IL, it is a
            // loud transpile failure the first time some BCL path takes it.
            case ("System.Threading.CancellationTokenSource", "CreateLinkedTokenSource"):
            {
                // The array overload (params CancellationToken[]) vs the 1-/2-token ones.
                if (sig.ParameterTypes is [{ Kind: TypeKind.SZArray }])
                {
                    var arr = Pop();
                    Push(StackKind.Ref, "Dn2CppCancelSource*",
                        $"dn2cpp_cts_link_array((Dn2CppArrayN*)({arr.Expr}))");
                    return true;
                }
                // 2-token: link both. 1-token: link it against CancellationToken.None,
                // whose null source links nothing — so the result cancels with the one
                // token, which is the overload's meaning.
                string second = "nullptr";
                if (sig.ParameterTypes.Length == 2)
                    second = $"((Dn2CppCancelToken)({Pop().Expr})).source";
                string first = $"((Dn2CppCancelToken)({Pop().Expr})).source";
                Push(StackKind.Ref, "Dn2CppCancelSource*", $"dn2cpp_cts_link2({first}, {second})");
                return true;
            }

            // CancellationToken value struct: { Dn2CppCancelSource* source }.
            case ("System.Threading.CancellationToken", ".ctor"):
            {
                var arg = Pop();  // bool canceled
                var self = Pop(); // ref this
                EmitCanceledExcRegistration();
                Emit($"*(Dn2CppCancelToken*)({self.Expr}) = Dn2CppCancelToken{{ ({arg.Expr}) ? dn2cpp_cts_canceled() : nullptr }};");
                return true;
            }
            case ("System.Threading.CancellationToken", "get_None"):
                Push(StackKind.Struct, "Dn2CppCancelToken", "Dn2CppCancelToken{ nullptr }");
                return true;
            case ("System.Threading.CancellationToken", "get_IsCancellationRequested"):
            {
                var t = Pop();
                Push(StackKind.I4, "int32_t", $"dn2cpp_cts_is_cancelled(((Dn2CppCancelToken*)({t.Expr}))->source)");
                return true;
            }
            case ("System.Threading.CancellationToken", "get_CanBeCanceled"):
            {
                var t = Pop();
                Push(StackKind.I4, "int32_t", $"(((Dn2CppCancelToken*)({t.Expr}))->source != nullptr ? 1 : 0)");
                return true;
            }
            // CancellationToken equality — .NET compares the underlying source by
            // reference (token1 == token2 iff they share a source, or both are None).
            // The two by-value struct arguments carry the source pointer directly.
            // Reached e.g. from AsyncOverSyncWithIoCancellation.InvokeAsync (the
            // sync-over-async file I/O wrapper) comparing its token against None.
            case ("System.Threading.CancellationToken", "op_Equality"):
            {
                var r = Pop();
                var l = Pop();
                Push(StackKind.I4, "int32_t",
                    $"((({l.Expr}).source) == (({r.Expr}).source) ? 1 : 0)");
                return true;
            }
            case ("System.Threading.CancellationToken", "op_Inequality"):
            {
                var r = Pop();
                var l = Pop();
                Push(StackKind.I4, "int32_t",
                    $"((({l.Expr}).source) != (({r.Expr}).source) ? 1 : 0)");
                return true;
            }
            // The named siblings of the operators above — same rule (identity of the
            // underlying source). The type is intrinsic, so a sibling left unmapped does
            // not fall back to real IL, it is a loud transpile failure.
            //
            // The receiver is a value type, so `this` arrives as a Dn2CppCancelToken*
            // while the operand is by value. The guard names the TYPED overload by the
            // operand's mapped C++ type; the boxing Equals(object) sibling is deliberately
            // not claimed — a loud "no intrinsic mapping" beats an unbox guessed at from a
            // type nobody checked.
            case ("System.Threading.CancellationToken", "Equals")
                when sig.ParameterTypes is [{ } eqp] && CppTypes.Of(eqp) == "Dn2CppCancelToken":
            {
                var other = Pop();
                var self = Pop();
                Push(StackKind.I4, "int32_t",
                    $"((((Dn2CppCancelToken*)({self.Expr}))->source) == (({other.Expr}).source) ? 1 : 0)");
                return true;
            }
            case ("System.Threading.CancellationToken", "GetHashCode"):
            {
                var t = Pop();
                // Hash the identity the equality compares — the source pointer — so two
                // tokens the equality calls equal cannot hash apart. None (a null source)
                // hashes to 0.
                Push(StackKind.I4, "int32_t",
                    $"(int32_t)(uintptr_t)(((Dn2CppCancelToken*)({t.Expr}))->source)");
                return true;
            }
            case ("System.Threading.CancellationToken", "ThrowIfCancellationRequested"):
            {
                var t = Pop();
                EmitCanceledExcRegistration();
                Emit($"if (dn2cpp_cts_is_cancelled(((Dn2CppCancelToken*)({t.Expr}))->source)) dn2cpp_throw_canceled();");
                return true;
            }
            // ---- The Register / UnsafeRegister family ----
            // Every arm is keyed on the DELEGATE SHAPE and answers BOTH spellings of the
            // name: the two differ only in the ExecutionContext capture Register performs,
            // and ExecutionContext.Capture() already lowers to the null "nothing to flow"
            // encoding here (CoreIntrinsics.MdExecutionContextCapture). One arm per shape
            // keeps the LIFO/already-canceled discipline in one place
            // (the sibling-overload rule, docs/ARCHITECTURE.md §4-B).
            //
            // Register(Action) / Register(Action, bool): run the Action when the
            // token's source cancels (LIFO), or immediately if already canceled. The
            // bool overload's useSynchronizationContext flag is irrelevant here. Push
            // materializes the call into a temp, so the registration runs even when the
            // returned CancellationTokenRegistration is discarded.
            case ("System.Threading.CancellationToken", "Register")
                when DelegateParamName(sig.ParameterTypes[0]) == "System.Action":
            {
                for (int i = 1; i < sig.ParameterTypes.Length; i++)
                    Pop(); // useSynchronizationContext (bool) — unmodeled
                var cb = Pop();   // Action callback
                var t = Pop();    // ref this (CancellationToken value struct)
                Push(StackKind.Ref, "Dn2CppCancelReg*",
                    $"dn2cpp_cts_register(((Dn2CppCancelToken*)({t.Expr}))->source, (Dn2CppObject*)({cb.Expr}))");
                return true;
            }
            // Register / UnsafeRegister(Action<object> callback, object state [, bool]): the
            // state-carrying form — the callback runs as callback(state) on cancel. A
            // trailing useSynchronizationContext bool is dropped as in the Action form above.
            case ("System.Threading.CancellationToken", "Register" or "UnsafeRegister")
                when DelegateParamName(sig.ParameterTypes[0]) == "System.Action_Object":
            {
                for (int i = 2; i < sig.ParameterTypes.Length; i++)
                    Pop(); // useSynchronizationContext (bool) — unmodeled
                var state = Pop(); // object state
                var cb = Pop();    // Action<object> callback
                var t = Pop();     // ref this (CancellationToken value struct)
                Push(StackKind.Ref, "Dn2CppCancelReg*",
                    $"dn2cpp_cts_register_state(((Dn2CppCancelToken*)({t.Expr}))->source, " +
                    $"(Dn2CppObject*)({cb.Expr}), (Dn2CppObject*)({state.Expr}))");
                return true;
            }
            // Register / UnsafeRegister(Action<object, CancellationToken> callback, object
            // state): the state-AND-token form. The callback runs as callback(state, token)
            // with the CANCELLING token — this source's own, which is what .NET hands it, so
            // a callback that re-reads IsCancellationRequested or ThrowIfCancellationRequested
            // sees the cancellation that invoked it.
            case ("System.Threading.CancellationToken", "Register" or "UnsafeRegister")
                when DelegateParamName(sig.ParameterTypes[0]) == "System.Action_Object_System_Threading_CancellationToken":
            {
                var tkState = Pop(); // object state
                var tkCb = Pop();    // Action<object, CancellationToken> callback
                var tkTok = Pop();   // ref this (CancellationToken value struct)
                Push(StackKind.Ref, "Dn2CppCancelReg*",
                    $"dn2cpp_cts_register_state_token(((Dn2CppCancelToken*)({tkTok.Expr}))->source, " +
                    $"(Dn2CppObject*)({tkCb.Expr}), (Dn2CppObject*)({tkState.Expr}))");
                return true;
            }
            // CancellationTokenRegistration.Dispose: detach the callback if it has not
            // fired yet (the handle is the reg-node pointer; `this` is a ref to it).
            case ("System.Threading.CancellationTokenRegistration", "Dispose"):
            {
                var t = Pop(); // ref this (Dn2CppCancelReg* value)
                Emit($"dn2cpp_cts_unregister(*((Dn2CppCancelReg**)({t.Expr})));");
                return true;
            }

            case ("System.Threading.CancellationTokenRegistration", "get_Token"):
            {
                var t = Pop(); // ref this (Dn2CppCancelReg* value)
                Push(StackKind.Struct, "Dn2CppCancelToken",
                    $"dn2cpp_ctr_token(*((Dn2CppCancelReg**)({t.Expr})))");
                return true;
            }
            case ("System.Threading.CancellationTokenRegistration", "Unregister"):
            {
                var t = Pop(); // ref this (Dn2CppCancelReg* value)
                Push(StackKind.I4, "int32_t",
                    $"dn2cpp_ctr_unregister(*((Dn2CppCancelReg**)({t.Expr})))");
                return true;
            }

            // ---- Task.Yield ----
            // Task.Yield returns a YieldAwaitable; GetAwaiter a YieldAwaiter.
            // Both are the stateless Dn2CppYieldAwaiter. IsCompleted is always
            // false, so awaiting always takes the suspend path, which posts the
            // resumption onto the run queue (TranslateAsyncGenericIntrinsic) — i.e.
            // the method yields its turn and resumes on the scheduler's next pass.
            case ("System.Threading.Tasks.Task", "Yield"):
                Push(StackKind.Struct, "Dn2CppYieldAwaiter", "Dn2CppYieldAwaiter{}");
                return true;
            case ("System.Runtime.CompilerServices.YieldAwaitable", "GetAwaiter"):
                Pop(); // ref awaitable (stateless)
                Push(StackKind.Struct, "Dn2CppYieldAwaiter", "Dn2CppYieldAwaiter{}");
                return true;
            case ("YieldAwaiter", "get_IsCompleted"):
                Pop(); // ref awaiter
                Push(StackKind.I4, "int32_t", "0"); // never completed -> always suspends
                return true;
            case ("YieldAwaiter", "GetResult"):
                Pop(); // ref awaiter — no result
                return true;
            // Delegate-continuation form: a custom async task type's builder
            // awaiting Task.Yield() forwards its Action continuation here. Post
            // it on this thread's run queue — the Action form of what the
            // builder's AwaitUnsafeOnCompleted lowering does for YieldAwaiter.
            case ("YieldAwaiter", "OnCompleted"):
            case ("YieldAwaiter", "UnsafeOnCompleted"):
            {
                var cont = Pop(); // Action continuation
                Pop();            // this (stateless)
                Emit($"dn2cpp_sched_post_action({Cast(cont, "Dn2CppObject*")});");
                return true;
            }

            default:
                return false;
        }
    }
}
