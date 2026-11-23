using System.Reflection.Metadata;
using System.Text;

namespace Dn2Cpp;

/// <summary>Runtime-async (<c>MethodImplAttributes.Async</c>) support: the body lowering
/// and the <c>System.Runtime.CompilerServices.AsyncHelpers</c> call surface.
/// <c>AsyncHelpers</c> is an intrinsic type, so every call into it is routed here and none
/// of its CoreCLR continuation machinery is transpiled.
///
/// <para>A runtime-async body reads as synchronous IL whose <c>ret</c> carries the
/// unwrapped result. It is lowered onto the native task plumbing the state-machine
/// builders use: the body completes a task it allocates at entry instead of returning,
/// and a body with suspension sites becomes a resumable function <c>M__async</c> behind
/// the method's unchanged signature. A suspending activation saves its arguments, IL
/// locals and residual evaluation stack into a heap <c>M__AsyncFrame</c>, registers
/// <c>M__resume</c> on what it awaits and returns; the resumption re-enters
/// <c>M__async</c> with that frame and jumps to the site by its saved state.</para>
///
/// <para>C++ cannot jump into a try block, so a site inside try bodies is reached
/// through them: the jump lands before the outermost enclosing <c>try {</c>, and each
/// enclosing block opens by forwarding the saved state one level in. A suspension
/// leaves its try blocks by <c>goto</c>, which runs no handler. No site lies in a
/// handler, so the region bookkeeping (<c>__eptrN</c>, <c>__feptrN</c>,
/// <c>__leaveN</c>, the caught exception) is idle at every site and the fresh native
/// locals of a resumed activation already hold that state.</para></summary>
internal sealed partial class MethodCompiler
{
    internal const string RuntimeAsyncHelpersType = "System.Runtime.CompilerServices.AsyncHelpers";

    /// <summary><see cref="StackEntry.AwaitOptions"/> for a
    /// <c>ConfigureAwaitOptions</c> argument that was not a literal.</summary>
    private const int AwaitOptionsNotLiteral = -1;
    private const int AwaitSuppressThrowing = 2;
    private const int AwaitForceYielding = 4;

    /// <summary>One emitted <c>try {</c> of a runtime-async body.</summary>
    private sealed class RuntimeAsyncTry
    {
        public readonly int Start;
        public readonly int End;
        /// <summary>The <c>_body</c> offsets at which the <c>try {</c> line starts and
        /// ends.</summary>
        public readonly int OpenAt;
        public readonly int BodyAt;
        /// <summary>Where the block sends each resumed state it contains: the site's
        /// label, or the entry label of the next enclosing block inward.</summary>
        public readonly List<(int State, string Target)> Forwards = new();

        public RuntimeAsyncTry(int start, int end, int openAt, int bodyAt)
        {
            Start = start;
            End = end;
            OpenAt = openAt;
            BodyAt = bodyAt;
        }
    }

    private sealed class RuntimeAsyncBody
    {
        /// <summary><c>T</c> of a <c>Task&lt;T&gt;</c> / <c>ValueTask&lt;T&gt;</c> return; null
        /// for the non-generic forms, whose <c>ret</c> carries no value.</summary>
        public TypeDesc? ResultType;
        /// <summary>The statement registering each suspension site's resumption. Site
        /// <c>k</c> (1-based, in IL order) resumes in state <c>k</c>; state 0 is the
        /// initial entry.</summary>
        public readonly List<string> Registrations = new();
        /// <summary>The label a resumption of each site jumps to from the function's
        /// top level, parallel to <see cref="Registrations"/>.</summary>
        public readonly List<string> Entries = new();
        /// <summary>The try blocks opened so far, in emission order: of two blocks
        /// holding the same offset, the outer one comes first.</summary>
        public readonly List<RuntimeAsyncTry> Tries = new();
        /// <summary>Frame slots for evaluation-stack values live across a suspension.</summary>
        public readonly List<(string Type, string Name)> Slots = new();
        public readonly HashSet<string> SlotNames = new();
        /// <summary>Whether a value-type instance body opens by copying <c>*this</c> into
        /// a local and never touches the receiver again, so nothing after a suspension
        /// reads through it and the caller's value is never written.</summary>
        public bool CopiesThis;
        public bool Returns;
        /// <summary>The expression handing the body's task to the caller, in the
        /// method's declared return type. Reads the builder local <c>__ab</c>.</summary>
        public string Exposed = "";
    }

    /// <summary>The lowering state of the runtime-async body being compiled; null in
    /// every other body.</summary>
    private RuntimeAsyncBody? _async;

    /// <summary>Checks a runtime-async body's contract before any of its IL is
    /// translated.</summary>
    private void ValidateRuntimeAsyncBody()
    {
        string site = $"{_method.DeclaringClass.FullName}.{_method.Name}";
        // The monitor guard brackets one synchronous activation; a suspended body would
        // resume outside it.
        if (_method.IsSynchronized)
            throw new NotSupportedException(
                $"{site}: a runtime-async method cannot also be Synchronized");
        if (!IsRuntimeAsyncReturnType(_method.Signature.ReturnType))
            throw new NotSupportedException(
                $"{site}: a runtime-async method must return Task, Task<T>, ValueTask or ValueTask<T>, "
                + $"not {_method.Signature.ReturnType}");
        // A hoisted element pointer is taken once at entry and would dangle in a resumed
        // activation.
        if (_method.NoAlias)
            throw new NotSupportedException(
                $"{site}: a runtime-async method cannot be [HotPath(NoAlias = true)]");
    }

    private bool IsRuntimeAsyncReturnType(TypeDesc type) =>
        type is { Kind: TypeKind.Class, Class: { } cls }
        && Comp.GenericDefFullName(cls) is "System.Threading.Tasks.Task" or "System.Threading.Tasks.ValueTask";

    /// <summary>Activates the lowering for a validated body whose arguments and locals
    /// are modeled.</summary>
    private void BeginRuntimeAsyncBody(List<Instruction> insns)
    {
        // The frame layout and the resume trampoline are spelled per closed body.
        if (SharedTrial)
            ThrowSharedTaint("runtime-async", _method.Name);
        var returnClass = _method.Signature.ReturnType.Class!;
        _async = new RuntimeAsyncBody
        {
            ResultType = returnClass.Context.TypeArgs.Length == 1 ? returnClass.Context.TypeArgs[0] : null,
            CopiesThis = insns.Count >= 3
                && insns[0].OpCode == ILOpCode.Ldarg_0
                && insns[1].OpCode == ILOpCode.Ldobj
                && insns[2].OpCode is ILOpCode.Stloc_0 or ILOpCode.Stloc_1 or ILOpCode.Stloc_2
                    or ILOpCode.Stloc_3 or ILOpCode.Stloc_s or ILOpCode.Stloc
                && !TouchesReceiverFrom(insns, 1),
        };
    }

    /// <summary>Whether an instruction at or after <paramref name="from"/> loads, takes
    /// the address of or stores argument 0.</summary>
    private static bool TouchesReceiverFrom(List<Instruction> insns, int from)
    {
        for (int i = from; i < insns.Count; i++)
        {
            var insn = insns[i];
            if (insn.OpCode == ILOpCode.Ldarg_0
                || (insn.OpCode is ILOpCode.Ldarg_s or ILOpCode.Ldarg or ILOpCode.Ldarga_s or ILOpCode.Ldarga
                        or ILOpCode.Starg_s or ILOpCode.Starg
                    && insn.Operand == 0))
                return true;
        }
        return false;
    }

    /// <summary><c>ret</c> in a runtime-async body: complete the task with the unwrapped
    /// result, as the builders' <c>SetResult</c> does.</summary>
    private void EmitRuntimeAsyncReturn()
    {
        var body = _async!;
        if (body.ResultType is { } resultType)
        {
            string cppType = CppTypes.Of(resultType);
            Push(CppTypes.KindOf(resultType), cppType, CoerceTo(Pop(), resultType, cppType));
            var value = Pop();
            string cache = TaskCachedResultDisposal(value, resultType);
            string store = EmitTaskResultStore(value);
            Emit($"dn2cpp_async_task_set_result(__atask, {store}, {cache});");
        }
        else
        {
            Emit("dn2cpp_async_task_set_result(__atask, 0, true);");
        }
        Emit("goto __adone;");
        body.Returns = true;
    }

    /// <summary>Closes the body's guarded region: a managed exception escaping the body
    /// settles the task faulted or canceled, as the builders' <c>SetException</c> does,
    /// instead of reaching the caller.</summary>
    private void EndRuntimeAsyncBody()
    {
        var body = _async!;
        _body.AppendLine("    } catch (Dn2CppException& __aex) {");
        EmitCanceledExcRegistration();
        Emit("dn2cpp_task_set_exception_or_canceled(__atask, __aex.obj);");
        // The task roots the exception from here on.
        Emit("dn2cpp_exc_inflight_pop(__aex.obj);");
        _body.AppendLine("    }");
        var returnType = _method.Signature.ReturnType;
        var backing = TaskBackingType(returnType)
            ?? throw new InvalidOperationException(
                $"{_method.DeclaringClass.FullName}.{_method.Name}: {returnType} has no backing Task type");
        string task = StampTask("dn2cpp_asyncbuilder_task(&__ab)", backing, 0);
        body.Exposed = returnType.Class!.IntrinsicCppName == "Dn2CppTask*" ? task : $"Dn2CppTaskAwaiter{{ {task} }}";
    }

    /// <summary>Whether a slot of this type keeps its value across a suspension. A byref
    /// or byref-like value may point into the suspended activation's dead stack, so it
    /// reads as default after a resumption instead.</summary>
    private static bool SurvivesSuspension(TypeDesc? type, StackKind kind) =>
        type is null
            ? kind != StackKind.Ptr
            : type.Kind != TypeKind.ByRef && !(type.Kind == TypeKind.Class && type.Class!.IsByRefLike);

    /// <summary>Renders a lowered runtime-async body. With no suspension site it is one
    /// function under the method's signature; otherwise that signature forwards to the
    /// resumable function, next to its frame and resume trampoline. The companion names
    /// derive from the method's own symbol, which is unique across the image.</summary>
    private string RenderRuntimeAsyncBody(string signature, string? rgctxAnchor, string? entryGuard)
    {
        var body = _async!;
        bool resumable = body.Registrations.Count > 0;
        string returnCpp = CppTypes.Of(_method.Signature.ReturnType);
        string frame = _method.CppName + "__AsyncFrame";
        string resume = _method.CppName + "__resume";
        string resumableName = _method.CppName + "__async";

        // Everything the frame carries besides its own bookkeeping, in save order.
        var saved = new List<(string Type, string Name)>();
        var sb = new StringBuilder();
        sb.AppendLine($"// {_method.DeclaringClass.FullName}::{_method.Name}");
        sb.AppendLine(signature);
        sb.AppendLine("{");
        if (entryGuard is not null)
            sb.AppendLine($"    {entryGuard}();");
        if (resumable)
        {
            var parameters = new List<string> { "void* __afp" };
            var initial = new List<string> { "nullptr" };
            var resumed = new List<string> { "__f" };
            foreach (var a in _args)
            {
                parameters.Add($"{a.CppType} {a.Name}");
                initial.Add(a.Name);
                if (SurvivesSuspension(a.Type, a.Kind))
                {
                    saved.Add((a.CppType, a.Name));
                    resumed.Add($"__f->{a.Name}");
                }
                else
                {
                    resumed.Add(CppTypes.ZeroInitExpr(a.CppType));
                }
            }
            if (_method.RgctxParam)
            {
                parameters.Add("const void* const* __rgctx");
                initial.Add("__rgctx");
                saved.Add(("const void* const*", "__rgctx"));
                resumed.Add("__f->__rgctx");
            }
            for (int li = 0; li < _locals.Count; li++)
            {
                var l = _locals[li];
                if (!SurvivesSuspension(l.Type, l.Kind))
                    continue;
                saved.Add((l.CppType, l.Name));
                if (HasDelegateTagLocal(li))
                    saved.Add(("int32_t", l.Name + "_delegate_tag"));
            }
            saved.AddRange(body.Slots);

            string prototype = $"{returnCpp} {resumableName}({string.Join(", ", parameters)})";
            sb.AppendLine($"    {prototype};");
            sb.AppendLine($"    return {resumableName}({string.Join(", ", initial)});");
            sb.AppendLine("}");
            sb.AppendLine($"struct {frame}");
            sb.AppendLine("{");
            sb.AppendLine("    int32_t __state;");
            sb.AppendLine("    Dn2CppTask* __task;");
            sb.AppendLine("    Dn2CppTask* __awaited;");
            foreach (var (type, name) in saved)
                sb.AppendLine($"    {type} {name};");
            sb.AppendLine("};");
            sb.AppendLine($"{prototype};");
            sb.AppendLine($"static void {resume}(void* __afp)");
            sb.AppendLine("{");
            sb.AppendLine($"    {frame}* __f = ({frame}*)__afp;");
            sb.AppendLine($"    (void){resumableName}({string.Join(", ", resumed)});");
            sb.AppendLine("}");
            sb.AppendLine(prototype);
            sb.AppendLine("{");
        }
        if (_c.ShadowStackEnabled)
            sb.AppendLine($"    Dn2CppShadowFrame __shadowFrame(\"{ShadowFrameName()}\");");
        AppendRgctxAnchor(sb, rgctxAnchor);
        AppendLocalDeclarations(sb);
        foreach (var (type, name) in body.Slots)
            sb.AppendLine($"    {type} {name} = {CppTypes.ZeroInit(type)};");
        sb.AppendLine("    Dn2CppTask* __atask;");
        if (resumable)
        {
            sb.AppendLine($"    {returnCpp} __aret = {CppTypes.ZeroInit(returnCpp)};");
            sb.AppendLine("    Dn2CppTask* __aw = nullptr;");
            sb.AppendLine("    int32_t __astate = 0;");
            sb.AppendLine($"    {frame}* __af = ({frame}*)__afp;");
            sb.AppendLine("    const bool __ainit = __af == nullptr;");
            sb.AppendLine("    if (__af != nullptr) {");
            foreach (var (_, name) in saved)
                if (!IsArgumentName(name))
                    sb.AppendLine($"        {name} = __af->{name};");
            sb.AppendLine("        __atask = __af->__task;");
            sb.AppendLine("        __aw = __af->__awaited;");
            sb.AppendLine("        __astate = __af->__state;");
            sb.AppendLine("    } else {");
            sb.AppendLine("        __atask = dn2cpp_async_task_alloc();");
            sb.AppendLine("    }");
        }
        else
        {
            sb.AppendLine("    __atask = dn2cpp_async_task_alloc();");
        }
        sb.AppendLine("    try {");
        if (resumable)
        {
            // Every temp is assigned after the label it is read behind, so entering at a
            // site needs nothing but the restored locals and slots.
            sb.AppendLine("    switch (__astate) {");
            sb.AppendLine("    case 0: break;");
            for (int k = 1; k <= body.Registrations.Count; k++)
                sb.AppendLine($"    case {k}: goto {body.Entries[k - 1]};");
            sb.AppendLine("    default: dn2cpp_fail(\"runtime-async: resumed in an unknown state\");");
            sb.AppendLine("    }");
        }
        AppendRuntimeAsyncBodyText(sb, body);
        if (body.Returns)
            sb.AppendLine("__adone:;");
        if (!resumable)
        {
            sb.AppendLine($"    {{ Dn2CppAsyncBuilder __ab{{ __atask, nullptr }}; return {body.Exposed}; }}");
            sb.AppendLine("}");
            return sb.ToString();
        }
        // Only the initial activation has a caller to hand the task to; the origin
        // transitions run once, there.
        sb.AppendLine($"    if (__ainit) {{ Dn2CppAsyncBuilder __ab{{ __atask, nullptr }}; __aret = {body.Exposed}; }}");
        sb.AppendLine("    return __aret;");
        sb.AppendLine("__asuspend:;");
        sb.AppendLine($"    if (__af == nullptr) __af = ({frame}*)dn2cpp_alloc(sizeof({frame}));");
        sb.AppendLine("    __af->__state = __astate;");
        sb.AppendLine("    __af->__task = __atask;");
        sb.AppendLine("    __af->__awaited = __aw;");
        foreach (var (_, name) in saved)
            sb.AppendLine($"    __af->{name} = {name};");
        sb.AppendLine("    dn2cpp_gc_write_barrier((void*)__af);");
        // The resumption may run on another thread as soon as it is registered, so the
        // value returned to the caller is settled first and nothing but it is read after.
        sb.AppendLine("    if (__ainit) { dn2cpp_async_task_suspend(__atask); "
            + $"Dn2CppAsyncBuilder __ab{{ __atask, nullptr }}; __aret = {body.Exposed}; }}");
        sb.AppendLine("    switch (__astate) {");
        for (int k = 1; k <= body.Registrations.Count; k++)
            sb.AppendLine($"    case {k}: {body.Registrations[k - 1]} break;");
        sb.AppendLine("    }");
        sb.AppendLine("    return __aret;");
        sb.AppendLine("}");
        return sb.ToString();
    }

    /// <summary>Appends the translated IL, giving every try block that holds a
    /// suspension site its entry label and state forwarding. A block entered in state 0
    /// falls through the forwarding; a block without sites keeps its text.</summary>
    private void AppendRuntimeAsyncBodyText(StringBuilder sb, RuntimeAsyncBody body)
    {
        string text = _body.ToString();
        int from = 0;
        for (int i = 0; i < body.Tries.Count; i++)
        {
            var t = body.Tries[i];
            if (t.Forwards.Count == 0)
                continue;
            sb.Append(text.Substring(from, t.OpenAt - from));
            sb.AppendLine($"__atry_{i}:;");
            sb.Append(text.Substring(t.OpenAt, t.BodyAt - t.OpenAt));
            sb.AppendLine("    switch (__astate) {");
            foreach (var (state, target) in t.Forwards)
                sb.AppendLine($"    case {state}: goto {target};");
            sb.AppendLine("    default: break;");
            sb.AppendLine("    }");
            from = t.BodyAt;
        }
        sb.Append(text.Substring(from));
    }

    /// <summary>Routes a resumption of the site being emitted through the try blocks
    /// around it, innermost first, and records where the top level enters the chain.</summary>
    private void RouteRuntimeAsyncResumption(RuntimeAsyncBody body, int state)
    {
        int at = _callSiteOffset;
        string target = $"__ar_{state}";
        for (int i = body.Tries.Count - 1; i >= 0; i--)
        {
            var t = body.Tries[i];
            if (at < t.Start || at >= t.End)
                continue;
            t.Forwards.Add((state, target));
            target = $"__atry_{i}";
        }
        body.Entries.Add(target);
    }

    /// <summary>Arguments reach a resumed activation as parameters, from the frame.</summary>
    private bool IsArgumentName(string name)
    {
        if (name == "__rgctx")
            return true;
        foreach (var a in _args)
            if (a.Name == name)
                return true;
        return false;
    }

    /// <summary>The non-generic <c>AsyncHelpers</c> members. A member this declines falls
    /// through to the intrinsic funnel's unmapped-member error.</summary>
    private bool TryEmitRuntimeAsyncIntrinsic(string declType, string name, MethodSignature<TypeDesc> sig)
    {
        if (declType != RuntimeAsyncHelpersType || sig.ParameterTypes.Length != 1)
            return false;
        switch (name)
        {
            // The synchronous wrapper Roslyn emits around an async Main: block until the
            // task settles and re-raise its fault unwrapped, as GetAwaiter().GetResult().
            case "HandleAsyncEntryPoint":
            {
                var t = Pop();
                string task = $"((Dn2CppTask*)({t.Expr}))";
                if (!t.NonNull)
                    Emit($"(void)dn2cpp_null_check((Dn2CppObject*)({t.Expr}));");
                Emit($"dn2cpp_task_block({task});");
                Emit($"dn2cpp_task_throw_if_faulted({task});");
                if (!sig.ReturnType.IsVoid)
                    PushTaskResult(task, sig.ReturnType);
                return true;
            }
            case "Await":
                EmitRuntimeAsyncSuspension(name, sig);
                return true;
        }
        return false;
    }

    /// <summary>The generic <c>AsyncHelpers</c> members, reached as MethodSpecs.</summary>
    private void EmitRuntimeAsyncGenericIntrinsic(string name, MethodSpecificationHandle msh, TypeDesc[] methodArgs)
    {
        var sig = MethodSpecSig(msh, methodArgs);
        if (methodArgs.Length == 1 && sig.ParameterTypes.Length == 1
            && name is "Await" or "AwaitAwaiter" or "UnsafeAwaitAwaiter")
        {
            EmitRuntimeAsyncSuspension(name, sig);
            return;
        }
        throw new NotSupportedException(
            $"{Method.DeclaringClass.FullName}.{Method.Name}: call to {RuntimeAsyncHelpersType}::{name}"
            + $"<{methodArgs.Length}>{SigArgs(sig)} has no intrinsic mapping yet [chain: {Comp.ReachChain(Method)}]");
    }

    /// <summary>One suspension point (<c>Await</c>, <c>AwaitAwaiter</c>,
    /// <c>UnsafeAwaitAwaiter</c>). A suspension needs the resumable frame of a lowered
    /// runtime-async body around it; anywhere else — an ordinary method, or a function
    /// synthesized for the helper's own address — there is nothing to suspend.</summary>
    private void EmitRuntimeAsyncSuspension(string helper, MethodSignature<TypeDesc> sig)
    {
        string site = $"{Method.DeclaringClass.FullName}.{Method.Name}";
        if (_async is not { } body)
            throw new NotSupportedException(
                $"{site}: {RuntimeAsyncHelpersType}::{helper}{SigArgs(sig)} "
                + "suspends its caller, which is only possible inside a lowered runtime-async method body "
                + $"[chain: {Comp.ReachChain(Method)}]");
        RequireSuspensionOutsideHandlers(site, helper);
        // A resumed activation is entered with a null receiver.
        if (!_method.IsStatic && _method.DeclaringClass.IsValueType && !body.CopiesThis)
            throw new NotSupportedException(
                $"{site}: a suspending runtime-async value-type instance method must open by copying *this "
                + "and not use the receiver again");

        var awaitable = Pop();
        var awaited = sig.ParameterTypes[0];
        string? awaitedDef = awaited is { Kind: TypeKind.Class, Class: { } awaitedClass }
            ? Comp.GenericDefFullName(awaitedClass) : null;
        string resume = _method.CppName + "__resume";
        string register = $"dn2cpp_async_await_on_completed(__aw, &{resume}, __af";
        string registration = register + ");";
        // Null: the site suspends unconditionally.
        string? pending = "__aw->status == DN2CPP_TASK_PENDING";
        bool throws = true;
        bool valueTask = false;
        bool yields = false;
        switch (helper, awaitedDef)
        {
            case ("Await", "System.Threading.Tasks.Task"):
                Emit($"__aw = (Dn2CppTask*)({awaitable.Expr});");
                if (!awaitable.NonNull)
                    Emit("(void)dn2cpp_null_check((Dn2CppObject*)__aw);");
                registration = register + ", true, true);";
                break;
            case ("Await", "System.Runtime.CompilerServices.ConfiguredTaskAwaitable"):
            {
                // A resumed activation does not keep the awaitable, so whether the site
                // suspends on a settled task and whether it throws are fixed here, from
                // options that reach the await straight from ConfigureAwait.
                int options = awaitable.AwaitOptions
                    ?? throw new NotSupportedException(
                        $"{site}: a runtime-async await of a ConfiguredTaskAwaitable must directly follow "
                        + "the ConfigureAwait call that makes it");
                if (options == AwaitOptionsNotLiteral)
                    throw new NotSupportedException(
                        $"{site}: a runtime-async await needs its ConfigureAwaitOptions as a literal");
                // ConfigureAwait rejects these with ArgumentOutOfRangeException.
                if ((options & ~7) != 0 || ((options & AwaitSuppressThrowing) != 0 && !sig.ReturnType.IsVoid))
                    throw new NotSupportedException(
                        $"{site}: ConfigureAwaitOptions value {options} is not valid for this runtime-async await");
                Emit($"__aw = ({awaitable.Expr}).task;");
                Emit("(void)dn2cpp_null_check((Dn2CppObject*)__aw);");
                // Match the state-machine Task route: capture managed Post, and
                // queue forced ready work without requiring an ordinary owner's pump.
                bool forceYielding = (options & AwaitForceYielding) != 0;
                registration = $"{register}, ({awaitable.Expr}).captures_context(), true, {(forceYielding ? "true" : "false")});";
                if (forceYielding)
                    pending = null;
                throws = (options & AwaitSuppressThrowing) == 0;
                break;
            }
            case ("Await", "System.Threading.Tasks.ValueTask"):
            case ("Await", "System.Runtime.CompilerServices.ConfiguredValueTaskAwaitable"):
                Emit($"__aw = dn2cpp_vtask(({awaitable.Expr}).task);");
                if (awaitedDef == "System.Runtime.CompilerServices.ConfiguredValueTaskAwaitable")
                    registration = $"{register}, ({awaitable.Expr}).captures_context());";
                pending = "dn2cpp_vtask_status(__aw) == DN2CPP_TASK_PENDING";
                valueTask = true;
                break;
            case ("UnsafeAwaitAwaiter", _) when awaited.Class?.IntrinsicCppName == "Dn2CppYieldAwaiter":
                Emit("__aw = nullptr;");
                pending = null;
                yields = true;
                registration = $"dn2cpp_sched_post(&{resume}, __af);";
                break;
            default:
                throw new NotSupportedException(
                    $"{site}: {RuntimeAsyncHelpersType}::{helper}{SigArgs(sig)} suspends on a custom awaiter, "
                    + $"which is not lowered [chain: {Comp.ReachChain(Method)}]");
        }

        body.Registrations.Add(registration);
        int state = body.Registrations.Count;
        RouteRuntimeAsyncResumption(body, state);
        SpillStackAcrossSuspension(body, site);
        Emit(pending is null
            ? $"__astate = {state}; goto __asuspend;"
            : $"if ({pending}) {{ __astate = {state}; goto __asuspend; }}");
        _body.AppendLine($"__ar_{state}:;");
        Emit("__astate = 0;");
        if (yields)
            return;
        if (valueTask)
        {
            EmitAwaitedTaskResult("__aw", sig.ReturnType);
            return;
        }
        if (throws)
            Emit("dn2cpp_task_throw_if_faulted(__aw);");
        else
            Emit("{ const Dn2CppConfiguredTaskAwaiter __suppressed{__aw, DN2CPP_AWAIT_SUPPRESS_THROWING}; "
                + "dn2cpp_configured_task_get_result(&__suppressed); }");
        if (!sig.ReturnType.IsVoid)
            PushTaskResult("__aw", sig.ReturnType);
    }

    /// <summary>A site cannot lie in a handler or filter, a try nested in one included:
    /// C++ cannot jump into a catch block, and a suspended handler would have to carry
    /// its in-flight exception and pending leave across the suspension. The
    /// runtime-async IL contract keeps suspensions out of handlers.</summary>
    private void RequireSuspensionOutsideHandlers(string site, string helper)
    {
        int at = _callSiteOffset;
        bool inHandler = false;
        foreach (var g in _catchGroups)
            foreach (var c in g.Clauses)
                inHandler |= at >= (c.FilterStart >= 0 ? c.FilterStart : c.Start) && at < c.End;
        foreach (var f in _finallyRegions)
            inHandler |= at >= f.HandlerStart && at < f.HandlerEnd;
        foreach (var f in _faultRegions)
            inHandler |= at >= f.HandlerStart && at < f.HandlerEnd;
        if (inHandler)
            throw new NotSupportedException(
                $"{site}: {RuntimeAsyncHelpersType}::{helper} at IL_{at:X4} suspends inside an exception handler");
    }

    /// <summary>Moves what the suspension leaves on the evaluation stack into frame
    /// slots, which the frame saves and a resumption restores; the temps the entries
    /// lived in are not carried across.</summary>
    private void SpillStackAcrossSuspension(RuntimeAsyncBody body, string site)
    {
        for (int i = 0; i < _stack.Count; i++)
        {
            var e = _stack[i];
            // An address may point into the activation that is about to return.
            if (e.Kind == StackKind.Ptr)
                throw new NotSupportedException(
                    $"{site}: a pointer on the evaluation stack cannot live across a runtime-async suspension");
            if (e.DelegateTag is not null)
                throw new NotSupportedException(
                    $"{site}: a method address on the evaluation stack cannot live across a runtime-async suspension");
            string name = $"__as{i}_{CppNaming.Sanitize(e.CppType)}";
            if (body.SlotNames.Add(name))
                body.Slots.Add((e.CppType, name));
            Emit($"{name} = {Cast(e, e.CppType)};");
            _stack[i] = new StackEntry(name, e.Kind, e.CppType, e.TypeToken, e.StaticType, NonNull: e.NonNull,
                SpilledSingle: e.SpilledSingle);
        }
    }
}
