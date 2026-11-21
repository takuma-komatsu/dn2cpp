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
    /// <c>ConfigureAwaitOptions</c> argument that was not a literal; outside the
    /// <c>int</c> range, so no literal collides with it.</summary>
    private const long AwaitOptionsNotLiteral = long.MinValue;
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
        /// <summary>What a site runs on the held frame before it is published, parallel to
        /// <see cref="Registrations"/>; empty for a site with nothing to prepare.</summary>
        public readonly List<string> Prepares = new();
        /// <summary>The label a resumption of each site jumps to from the function's
        /// top level, parallel to <see cref="Registrations"/>.</summary>
        public readonly List<string> Entries = new();
        /// <summary>The try blocks opened so far, in emission order: of two blocks
        /// holding the same offset, the outer one comes first.</summary>
        public readonly List<RuntimeAsyncTry> Tries = new();
        /// <summary>Frame slots for evaluation-stack values live across a suspension.</summary>
        public readonly List<(string Type, string Name)> Slots = new();
        public readonly HashSet<string> SlotNames = new();
        /// <summary>The slot of each (stack depth, C++ type, tag) already named.</summary>
        public readonly Dictionary<(int Depth, string CppType, bool Tag), string> SlotByKey = new();
        /// <summary>Slots left out of the frame.</summary>
        public readonly HashSet<string> UnsavedSlots = new();
        /// <summary>Whether a value-type instance body touches its receiver at most in an
        /// opening copy of <c>*this</c> that no branch re-enters, so no resumed activation
        /// reads through it and the caller's value is never written.</summary>
        public bool ReadsReceiverOnlyAtEntry;
        public bool Returns;
        /// <summary>Whether a suspension registers a custom awaiter, so the frame holds the
        /// continuation delegate.</summary>
        public bool CachesAction;
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
            ReadsReceiverOnlyAtEntry = ReadsReceiverOnlyAtEntry(insns),
        };
    }

    /// <summary>Whether the body touches argument 0 nowhere, or only in an opening
    /// <c>ldarg.0; ldobj</c> that no branch targets.</summary>
    private static bool ReadsReceiverOnlyAtEntry(List<Instruction> insns)
    {
        if (!TouchesReceiverFrom(insns, 0))
            return true;
        if (insns.Count < 2 || insns[0].OpCode != ILOpCode.Ldarg_0 || insns[1].OpCode != ILOpCode.Ldobj
            || TouchesReceiverFrom(insns, 1))
            return false;
        int entry = insns[0].Offset;
        foreach (var insn in insns)
        {
            if (ILDecoder.IsBranch(insn.OpCode) && insn.Operand == entry)
                return false;
            if (insn.SwitchTargets is not { } targets)
                continue;
            foreach (int target in targets)
                if (target == entry)
                    return false;
        }
        return true;
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
            Emit($"__alate = !dn2cpp_async_task_try_set_result(__atask, {store}, {cache});");
        }
        else
        {
            Emit("__alate = !dn2cpp_async_task_try_set_result(__atask, 0, true);");
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
        Emit("__alate = !dn2cpp_async_task_try_set_exception_or_canceled(__atask, __aex.obj);");
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
            : type.Kind != TypeKind.ByRef && !IsByRefLikeValue(type);

    /// <summary>A ref struct, or <c>TypedReference</c>, which signatures decode as a
    /// primitive rather than as its own byref-like type.</summary>
    private static bool IsByRefLikeValue(TypeDesc type) =>
        type is { Kind: TypeKind.Class, Class.IsByRefLike: true }
        || type is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.TypedReference }
        || type is { Kind: TypeKind.External, ExternalName: "System.TypedReference" };

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
            var parameters = new List<string> { "void* __afp", "int32_t __astate" };
            var initial = new List<string> { "nullptr", "0" };
            var resumed = new List<string> { "__f", "__s" };
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
            saved.AddRange(body.Slots.Where(s => !body.UnsavedSlots.Contains(s.Name)));

            string prototype = $"{returnCpp} {resumableName}({string.Join(", ", parameters)})";
            sb.AppendLine($"    {prototype};");
            sb.AppendLine($"    return {resumableName}({string.Join(", ", initial)});");
            sb.AppendLine("}");
            sb.AppendLine($"struct {frame}");
            sb.AppendLine("{");
            // A custom awaiter's continuation delegate targets the frame, so the frame
            // is a managed object: an OnCompleted may inspect Target.
            sb.AppendLine("    Dn2CppObject __object;");
            // __state is the frame's lock: negative while a thread holds the frame, which
            // only that thread reads or writes. __task is written once, before the frame
            // is first published.
            sb.AppendLine("    int32_t __state;");
            sb.AppendLine("    Dn2CppTask* __task;");
            sb.AppendLine("    Dn2CppTask* __awaited;");
            if (body.CachesAction)
                sb.AppendLine("    Dn2CppObject* __act;");
            foreach (var (type, name) in saved)
                sb.AppendLine($"    {type} {name};");
            sb.AppendLine("};");
            sb.AppendLine($"{prototype};");
            sb.AppendLine($"static void {resume}(void* __afp)");
            sb.AppendLine("{");
            sb.AppendLine($"    {frame}* __f = ({frame}*)__afp;");
            // Claiming the frame is atomic and precedes every other read of it. A negative
            // state is a running or a completed activation, neither of which has a site to
            // resume at; CoreLib's dispatch settles the task for the first and fails the
            // second transition of it for the second.
            sb.AppendLine("    const int32_t __s = dn2cpp_interlocked_exchange_i4(&__f->__state, -2);");
            sb.AppendLine("    if (__s < 0) { dn2cpp_async_resume_running(__f->__task); return; }");
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
        // The activation's context changes end with the activation, as CoreLib's
        // dispatch restores the invoking thread's context.
        sb.AppendLine("    Dn2CppObject* const __asc = dn2cpp_sync_ctx_get();");
        // Set when the task had already settled by the time the body completed.
        sb.AppendLine("    bool __alate = false;");
        if (resumable)
        {
            sb.AppendLine($"    {returnCpp} __aret = {CppTypes.ZeroInit(returnCpp)};");
            sb.AppendLine("    Dn2CppTask* __aw = nullptr;");
            if (body.CachesAction)
                sb.AppendLine("    Dn2CppObject* __acur = nullptr;");
            sb.AppendLine($"    {frame}* __af = ({frame}*)__afp;");
            sb.AppendLine("    const bool __ainit = __af == nullptr;");
            sb.AppendLine("    if (__af != nullptr) {");
            foreach (var (_, name) in saved)
                if (!IsArgumentName(name))
                    sb.AppendLine($"        {name} = __af->{name};");
            sb.AppendLine("        __atask = __af->__task;");
            sb.AppendLine("        __aw = __af->__awaited;");
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
            sb.AppendLine("    (void)__alate;");
            sb.AppendLine($"    {{ Dn2CppAsyncBuilder __ab{{ __atask, nullptr }}; {returnCpp} __ar = {body.Exposed}; "
                + "dn2cpp_sync_ctx_set(__asc); return __ar; }");
            sb.AppendLine("}");
            return sb.ToString();
        }
        // Only the initial activation has a caller to hand the task to; the origin
        // transitions run once, there.
        sb.AppendLine($"    if (__ainit) {{ Dn2CppAsyncBuilder __ab{{ __atask, nullptr }}; __aret = {body.Exposed}; }}");
        sb.AppendLine("    dn2cpp_sync_ctx_set(__asc);");
        // The frame is the continuation delegate's target, so it must not outlive the
        // activation's values: only the header stays, and its state stays negative.
        sb.AppendLine("    if (__af != nullptr) {");
        sb.AppendLine($"        std::memset((char*)&__af->__awaited, 0, sizeof({frame}) - ((char*)&__af->__awaited - (char*)__af));");
        sb.AppendLine("        dn2cpp_gc_write_barrier((void*)__af);");
        sb.AppendLine("        (void)dn2cpp_interlocked_exchange_i4(&__af->__state, -1);");
        sb.AppendLine("    }");
        // A resumed body that completed a task settled meanwhile fails like CoreLib's
        // dispatch, once the activation's own cleanup is done.
        sb.AppendLine("    if (__alate) dn2cpp_tcs_throw_settled();");
        sb.AppendLine("    return __aret;");
        sb.AppendLine("__asuspend:;");
        sb.AppendLine($"    if (__af == nullptr) {{ __af = ({frame}*)dn2cpp_alloc(sizeof({frame})); "
            + "__af->__object.type = &dn2cpp_object_type; __af->__task = __atask; }");
        sb.AppendLine("    __af->__awaited = __aw;");
        foreach (var (_, name) in saved)
            sb.AppendLine($"    __af->{name} = {name};");
        if (body.CachesAction)
        {
            sb.AppendLine("    switch (__astate) {");
            for (int k = 1; k <= body.Prepares.Count; k++)
                if (body.Prepares[k - 1].Length > 0)
                    sb.AppendLine($"    case {k}: {{ {body.Prepares[k - 1]} }} break;");
            sb.AppendLine("    default: break;");
            sb.AppendLine("    }");
        }
        sb.AppendLine("    dn2cpp_gc_write_barrier((void*)__af);");
        // Publishing hands the frame to whichever thread resumes it next.
        sb.AppendLine("    (void)dn2cpp_interlocked_exchange_i4(&__af->__state, __astate);");
        // The resumption may run on another thread as soon as it is registered, so the
        // value returned to the caller is settled first and nothing but it is read after.
        sb.AppendLine("    if (__ainit) { dn2cpp_async_task_suspend(__atask); "
            + $"Dn2CppAsyncBuilder __ab{{ __atask, nullptr }}; __aret = {body.Exposed}; }}");
        sb.AppendLine("    switch (__astate) {");
        for (int k = 1; k <= body.Registrations.Count; k++)
            sb.AppendLine($"    case {k}: {body.Registrations[k - 1]} break;");
        sb.AppendLine("    }");
        sb.AppendLine("    dn2cpp_sync_ctx_set(__asc);");
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
        if (!_method.IsStatic && _method.DeclaringClass.IsValueType && !body.ReadsReceiverOnlyAtEntry)
            throw new NotSupportedException(
                $"{site}: a suspending runtime-async value-type instance method may use its receiver only "
                + "to copy *this on entry");

        var awaitable = Pop();
        var awaited = sig.ParameterTypes[0];
        string? awaitedDef = awaited is { Kind: TypeKind.Class, Class: { } awaitedClass }
            ? Comp.GenericDefFullName(awaitedClass) : null;
        string resume = _method.CppName + "__resume";
        string prepare = "";
        string registration = $"dn2cpp_async_await_on_completed(__aw, &{resume}, __af);";
        // Null: the site suspends unconditionally.
        string? pending = "__aw->status == DN2CPP_TASK_PENDING";
        bool throws = true;
        bool valueTask = false;
        bool registersOnly = false;
        switch (helper, awaitedDef)
        {
            case ("Await", "System.Threading.Tasks.Task"):
                Emit($"__aw = (Dn2CppTask*)({awaitable.Expr});");
                if (!awaitable.NonNull)
                    Emit("(void)dn2cpp_null_check((Dn2CppObject*)__aw);");
                break;
            case ("Await", "System.Runtime.CompilerServices.ConfiguredTaskAwaitable"):
            {
                // The awaitable's value does not carry its options, so one that reaches
                // the await through a local, a call or a branch join has lost them.
                long tag = awaitable.AwaitOptions
                    ?? throw new NotSupportedException(
                        $"{site}: a runtime-async await of a ConfiguredTaskAwaitable must directly follow "
                        + "the ConfigureAwait call that makes it");
                if (tag == AwaitOptionsNotLiteral)
                    throw new NotSupportedException(
                        $"{site}: a runtime-async await needs its ConfigureAwaitOptions as a literal");
                int options = (int)tag;
                Emit($"__aw = ({awaitable.Expr}).task;");
                Emit("(void)dn2cpp_null_check((Dn2CppObject*)__aw);");
                // A settled task still suspends: its registration queues the resumption.
                if ((options & AwaitForceYielding) != 0)
                    pending = null;
                throws = (options & AwaitSuppressThrowing) == 0;
                break;
            }
            case ("Await", "System.Threading.Tasks.ValueTask"):
            case ("Await", "System.Runtime.CompilerServices.ConfiguredValueTaskAwaitable"):
                Emit($"__aw = dn2cpp_vtask(({awaitable.Expr}).task);");
                pending = "dn2cpp_vtask_status(__aw) == DN2CPP_TASK_PENDING";
                valueTask = true;
                break;
            // Roslyn tests IsCompleted and reads GetResult itself around these two, so
            // the site only suspends: it has no fast path and leaves no result.
            case ("AwaitAwaiter" or "UnsafeAwaitAwaiter", _):
            {
                pending = null;
                registersOnly = true;
                string held = NewTemp(awaitable.CppType);
                Emit($"{held} = {Cast(awaitable, awaitable.CppType)};");
                switch (awaited.Class?.IntrinsicCppName)
                {
                    case "Dn2CppYieldAwaiter":
                        Emit("__aw = nullptr;");
                        registration = $"dn2cpp_sched_post(&{resume}, __af);";
                        break;
                    // A settled task queues the resumption instead of running it inline.
                    // A default task awaiter holds no task, so its registration fails as
                    // OnCompleted over one does; a default value-task awaiter is completed.
                    case "Dn2CppTaskAwaiter" when awaitedDef is "System.Runtime.CompilerServices.TaskAwaiter"
                        or "ConfiguredTaskAwaiter":
                        Emit($"__aw = {held}.task;");
                        registration = GuardRegistration(
                            "(void)dn2cpp_null_check((Dn2CppObject*)__aw); " + registration, reraise: true);
                        break;
                    case "Dn2CppTaskAwaiter":
                        Emit($"__aw = dn2cpp_vtask({held}.task);");
                        break;
                    default:
                        Emit("__aw = nullptr;");
                        // The awaiter arrives by value, so its registration method runs on
                        // this copy and what it writes to a struct awaiter is not seen by
                        // the GetResult that follows. A throw from it is re-raised as
                        // Task.ThrowAsync does and leaves the method suspended.
                        body.CachesAction = true;
                        registration = "{ " + (CustomAwaiterResume(awaited, $"&{held}", "__af", resume,
                                preferUnsafe: helper == "UnsafeAwaitAwaiter", reraise: true,
                                completionTask: "__atask", prepare: p => prepare = p)
                            ?? throw new NotSupportedException(
                                $"{site}: {RuntimeAsyncHelpersType}::{helper}{SigArgs(sig)} suspends on a value-type "
                                + $"awaiter whose registration method has no body [chain: {Comp.ReachChain(Method)}]")) + " }";
                        break;
                }
                break;
            }
            default:
                throw new NotSupportedException(
                    $"{site}: {RuntimeAsyncHelpersType}::{helper}{SigArgs(sig)} has no intrinsic mapping yet "
                    + $"[chain: {Comp.ReachChain(Method)}]");
        }

        body.Registrations.Add(registration);
        body.Prepares.Add(prepare);
        int state = body.Registrations.Count;
        RouteRuntimeAsyncResumption(body, state);
        SpillStackAcrossSuspension(body, site);
        Emit(pending is null
            ? $"__astate = {state}; goto __asuspend;"
            : $"if ({pending}) {{ __astate = {state}; goto __asuspend; }}");
        _body.AppendLine($"__ar_{state}:;");
        Emit("__astate = 0;");
        if (registersOnly)
            return;
        if (valueTask)
        {
            EmitAwaitedTaskResult("__aw", sig.ReturnType);
            return;
        }
        // A continuation invoked while the task is pending waits for it, as CoreLib's
        // ValidateEnd does; the resumption after completion pays only the status test.
        Emit("if (__aw->status == DN2CPP_TASK_PENDING) (void)dn2cpp_task_block(__aw);");
        Emit(throws ? "dn2cpp_task_throw_if_faulted(__aw);" : "dn2cpp_task_mark_exceptions_handled(__aw);");
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

    /// <summary>The slot for a depth and C++ type: one per exact pair, and a name no other
    /// pair has taken even where sanitizing makes two spellings alike. Numbering follows
    /// emission order.</summary>
    private static string SpillSlotName(RuntimeAsyncBody body, int depth, string cppType, bool tag, out bool created)
    {
        if (body.SlotByKey.TryGetValue((depth, cppType, tag), out var existing))
        {
            created = false;
            return existing;
        }
        string stem = tag ? $"__as{depth}_tag" : $"__as{depth}_{CppNaming.Sanitize(cppType)}";
        string name = stem;
        for (int n = 2; !body.SlotNames.Add(name); n++)
            name = $"{stem}_{n}";
        body.SlotByKey[(depth, cppType, tag)] = name;
        created = true;
        return name;
    }

    private const string TypeHandleCppType = "const Dn2CppTypeInfo*";

    /// <summary>Moves what the suspension leaves on the evaluation stack into frame
    /// slots, which the frame saves and a resumption restores; the temps the entries
    /// lived in are not carried across.</summary>
    private void SpillStackAcrossSuspension(RuntimeAsyncBody body, string site)
    {
        for (int i = 0; i < _stack.Count; i++)
        {
            var e = _stack[i];
            // An address may point into the activation that is about to return. A type
            // handle is a type-info pointer, immutable static data.
            if (e.Kind == StackKind.Ptr && e.CppType != TypeHandleCppType)
                throw new NotSupportedException(
                    $"{site}: a pointer on the evaluation stack cannot live across a runtime-async suspension");
            string name = SpillSlotName(body, i, e.CppType, tag: false, out bool created);
            if (created)
            {
                body.Slots.Add((e.CppType, name));
                // A byref-like value may hold an address into the activation about to
                // return, so its slot is not saved and reads as default after a resumption.
                if (Comp.IsByRefLikeCppType(e.CppType))
                    body.UnsavedSlots.Add(name);
            }
            Emit($"{name} = {Cast(e, e.CppType)};");
            // The tag of a 64-bit value that may hold a method address travels with it.
            string? tag = e.DelegateTag;
            if (tag is not null && tag != UntrackedDelegateTag)
            {
                string tagName = SpillSlotName(body, i, "int32_t", tag: true, out bool tagCreated);
                if (tagCreated)
                    body.Slots.Add(("int32_t", tagName));
                Emit($"{tagName} = {tag};");
                tag = tagName;
            }
            _stack[i] = new StackEntry(name, e.Kind, e.CppType, e.TypeToken, e.StaticType, NonNull: e.NonNull, DelegateTag: tag);
        }
    }
}
