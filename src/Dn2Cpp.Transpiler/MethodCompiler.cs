using System.Reflection.Metadata;
using System.Text;
using SRME = System.Reflection.Metadata.Ecma335.MetadataTokens;

namespace Dn2Cpp;

// <c>TypeToken</c> carries the static type a runtime type handle stands for
// (set by `ldtoken <type>` and preserved through Type.GetTypeFromHandle) so
// `typeof(T).IsValueType` can be constant-folded at the call site.
// <c>StaticType</c> carries the operand's declared CLR type (set by ldloc/ldarg)
// — distinct from TypeToken's typeof-referent semantics — so an intrinsic can
// recover a concrete generic like List&lt;T&gt; from the call site.
// <c>BlobLen</c> is the byte length of the RVA blob an <c>ldtoken &lt;field&gt;</c>
// pushed, so the immediately-following <c>RuntimeHelpers.CreateSpan&lt;T&gt;</c> can
// size the span.
// <c>StrLiteral</c> carries the value of an <c>ldstr</c> so an intrinsic that needs
// a compile-time string argument can recover it from the stack (the symbol is
// spilled to a temp by Push) — e.g. the field name of <c>Marshal.OffsetOf</c>.
// <c>KnownNull</c> records that the operand is the null constant — an <c>ldnull</c>,
// or an intrinsic whose value IS the nullptr sentinel (<c>EqualityComparer&lt;T&gt;
// .Default</c>). Mirror of <c>NonNull</c>, and like it a pure optimization: losing it
// (Push spills, and a block boundary re-spills) can only cost back a run-time null
// test, never an answer.
/// <summary>One evaluation-stack slot. <c>Expr</c> is the C++ text that reads it —
/// almost always a single-assignment temp, since <see cref="MethodCompiler.Push"/>
/// materializes one for every push, so the ORIGIN of a value is not recoverable
/// from its text and any consumer that needs the origin must carry it here.
/// <c>ArgSlot</c> is that for <c>ldarga</c>: the IL argument number whose address
/// this entry holds (null for every other entry, including a <c>ldloca</c> one,
/// which sets <c>SlotAddr</c> alone).</summary>
internal sealed record StackEntry(string Expr, StackKind Kind, string CppType, TypeDesc? TypeToken = null, TypeDesc? StaticType = null, int? BlobLen = null, string? StrLiteral = null, bool NonNull = false, bool SlotAddr = false, bool KnownNull = false, int? ArgSlot = null);

/// <summary>
/// Translates one IL method body into a C++ function. The evaluation stack is
/// modeled with single-assignment temporaries; at basic-block boundaries the
/// stack is spilled to canonical per-depth variables so control-flow joins agree.
///
/// The intrinsic-table partials reach this class only through
/// <see cref="IEvalStack"/>; everything below the fold — the entry stacks, the
/// exception-region reconstruction, the branch liveness, the declaration list — is
/// theirs to leave alone.
/// </summary>
internal sealed partial class MethodCompiler : IEvalStack
{
    private readonly Compilation _c;
    private readonly MethodInfo _method;
    private readonly Module _module;
    private readonly System.Reflection.Metadata.MetadataReader _reader;
    private readonly LiteralPool _literals;
    private readonly ICallIntrinsics? _intrinsics;

    // The IEvalStack view of the six fields above: the intrinsic tables name these
    // rather than the fields, so the surface they depend on is the interface's and
    // not whatever happens to be in scope inside a partial of this class.
    public Compilation Comp => _c;
    public MethodInfo Method => _method;
    public Module Module => _module;
    public System.Reflection.Metadata.MetadataReader Reader => _reader;
    public LiteralPool Literals => _literals;

    private readonly IEmitBackend? _backend;

    private readonly StringBuilder _body = new();
    private readonly List<(string Type, string Name)> _decls = new();
    private readonly HashSet<string> _declared = new();
    private readonly Dictionary<int, List<StackEntry>> _entryStacks = new();
    private readonly HashSet<int> _labels = new();
    // Branch/switch targets named by an instruction at or after the target
    // itself (backward or self edges). Lets the label handler tell a loop head
    // (an edge from a not-yet-processed later instruction may still arrive)
    // from dead code (every edge already went by — and, if no entry stack got
    // recorded, every one of them was statically elided). See step 5 in
    // Compile.
    private readonly HashSet<int> _backwardBranchTargets = new();
    /// <summary>ldftn offsets whose result feeds a delegate <c>newobj</c>, mapped to
    /// the delegate class being constructed (null when its token would not resolve).
    /// A static method's ldftn behind a delegate needs the target-slot adapter, while
    /// the same ldftn used as a C# function pointer (invoked by <c>calli</c>) needs the
    /// raw address; they share the opcode, so the consumer disambiguates. See the
    /// classification pass in <c>Compile</c>.</summary>
    private readonly Dictionary<int, ClassInfo?> _ftnDelegateUse = new();

    private List<StackEntry> _stack = new();
    private List<(string Name, string CppType, StackKind Kind, TypeDesc? Type)> _args = new();
    private List<(string Name, string CppType, StackKind Kind, TypeDesc? Type)> _locals = new();
    // [HotPath(NoAlias)] span parameters whose element pointer is hoisted into a
    // __restrict prologue local, in parameter order (the order the prologue
    // declares them). Populated by SetupNoAliasSpanLocals; a name enters
    // _noAliasSpansUsed the first time the SkipBoundsChecks indexer route
    // addresses it, and only used entries are declared — an unused hoist would be
    // dead text differing from the unmarked emission for no gain.
    private readonly List<(int ArgSlot, string ArgName, string LocalName, string ElemStorage)> _noAliasSpans = new();
    private readonly HashSet<int> _noAliasSpansUsed = new();
    // Structured exception handling. Regions are reconstructed into nested
    // C++ try/catch blocks; multiple catch clauses share one C++ catch that
    // dispatches by managed type.
    private sealed class CatchGroup
    {
        public int Index;
        public int TryStart;
        public int TryEnd;       // == first clause handler start
        public int End;          // end of the last clause body
        // Ti: the runtime type-info to isinst against (null = catch-all). FilterStart:
        // the IL offset of the clause's filter block (`catch... when`), or -1 for a
        // plain typed clause whose match is the isinst alone.
        public List<(int Start, int End, string? Ti, int FilterStart)> Clauses = new();
    }
    private sealed record FinallyRegion(int Index, int TryStart, int TryEnd, int HandlerStart, int HandlerEnd)
    {
        public Dictionary<int, int> LeaveTargets { get; } = new();
    }
    // A fault region's handler runs only when an exception propagates out of the
    // try (never on normal completion or `leave`), then re-raises. Unlike a
    // finally it has no leave routing: valid IL ends the try with `leave`, which
    // jumps past the handler, so the handler is reached only by falling through
    // the C++ catch that captured the exception.
    private sealed record FaultRegion(int Index, int TryStart, int TryEnd, int HandlerStart, int HandlerEnd);
    private readonly List<(int TryStart, int TryEnd)> _tryOpens = new();
    private readonly List<CatchGroup> _catchGroups = new();
    private readonly List<FinallyRegion> _finallyRegions = new();
    private readonly List<FaultRegion> _faultRegions = new();
    private readonly Dictionary<int, CatchGroup> _catchDispatchAt = new();   // tryEnd -> group
    private readonly Dictionary<int, List<(CatchGroup G, int Clause)>> _clauseLabelAt = new();
    private readonly Dictionary<int, (CatchGroup G, int Clause)> _filterBlockAt = new(); // filterOffset -> clause
    private readonly Dictionary<int, FinallyRegion> _finallyHandlerStarts = new();
    private readonly Dictionary<int, FaultRegion> _faultHandlerStarts = new();
    // Offset-keyed views of the two remaining per-instruction region scans in
    // Compile (close catch groups ending here / open trys starting here),
    // precomputed at the end of BuildExceptionRegions like the handler-start
    // dictionaries above — so an EH-free method pays one failed TryGetValue per
    // instruction instead of a Where+OrderByDescending pass. Each value list
    // keeps exactly the order the per-instruction query produced (see the
    // build site), because that order is the nesting order the emitted
    // braces close/open in.
    private readonly Dictionary<int, List<CatchGroup>> _catchGroupEndsAt = new();
    private readonly Dictionary<int, List<(int TryStart, int TryEnd)>> _tryOpensAt = new();
    private int _tempCounter;
    private bool _unreachable;
    // Folded-guard branch liveness for this body (null: everything live). The
    // reachability scanner computed the identical pass, so the offsets skipped
    // here were never scanned — emission and reachability stay in lockstep.
    private BranchLiveness? _liveness;
    private TypeDesc? _constrained; // pending constrained. prefix for the next callvirt

    /// <summary>Armed while compiling a canonical-owner-world method (a shared
    /// body candidate under <c>--shared-generics</c>): every
    /// instantiation-dependent site throws <see cref="SharedBodyTaintException"/>,
    /// which the planning pass catches to mark the method unshareable (each
    /// grouped user then compiles its own body). Never set for real methods, so
    /// the checks cost nothing with sharing off.</summary>
    public bool SharedTrial { get; set; }
    /// <summary>When non-null (planning pass, canonical method), collects the
    /// canonical methods this body binds by symbol — direct calls, value-type
    /// ctor calls, devirtualized struct-override calls — the edges the
    /// unshareability cascade and the retention closure walk, each flagged with
    /// whether the site could pass a runtime generic context on.</summary>
    internal List<(MethodInfo Callee, bool RgctxPassable)>? SharedDirectCallees;
    // Whether this shared body read the runtime generic context (loaded a slot
    // or forwarded it) — drives the lazy __rgctx prologue.
    private bool _usedRgctx;
    // The raw IL token of the call/newobj instruction currently translating —
    // the site key rgctx slots re-resolve per real instantiation. Consumed only
    // after verifying it still resolves to the entity at hand, so a stale token
    // (an intrinsic lowering calling a different method than the site named)
    // can never mis-key a slot.
    private int _callSiteToken;

    /// <summary>Whether the call instruction currently being translated is a
    /// <c>callvirt</c> (set at <see cref="TranslateCall"/> entry). Read by the
    /// System.Exception::get_Message intrinsic to distinguish a virtual `ex.Message`
    /// (which must dispatch a derived override — dn2cpp_exception_message) from a
    /// non-virtual `base.Message` inside such an override (which must read the stored
    /// message directly — dn2cpp_exception_message_stored — or it recurses forever).</summary>
    private bool _callIsVirtual;

    public bool CallIsVirtual => _callIsVirtual;

    private void ThrowSharedTaint(string kind, object? site) =>
        throw new SharedBodyTaintException(kind,
            $"{_method.DeclaringClass.FullName}.{_method.Name}: instantiation-dependent {kind} site ({site})");

    private void TaintIfCanonical(TypeDesc t, string kind)
    {
        if (SharedTrial && Compilation.ContainsCanonPlaceholder(t))
            ThrowSharedTaint(kind, t);
    }

    private void TaintIfCanonical(ClassInfo c, string kind)
    {
        if (SharedTrial && Compilation.ContainsCanonPlaceholder(c))
            ThrowSharedTaint(kind, c.FullName);
    }

    // ---- runtime generic context (rgctx) in shared bodies ----

    /// <summary>Allocates (or reuses) an rgctx slot for the compiled canonical
    /// body and returns the shared body's load expression for it. A
    /// generic-method instantiation keys the slot in its own method registry
    /// (every slot, even a class-argument-dependent one — the fill resolves
    /// under the user's full context, covering both dimensions); a plain
    /// canonical-class body keys it on the declaring class. Taints when the
    /// slot is known unresolvable for some group member.</summary>
    private string RgctxSlotAccess(RgctxSlotKind kind, int token, string taintKind, object? site)
    {
        if (!SharedTrial || token == 0)
            ThrowSharedTaint(taintKind, site);
        int i;
        if (_method.NameSuffix != "")
        {
            if (!Compilation.IsCanonicalMethod(_method)
                || _c.RgctxMethodSlotKnownBad(_method, kind, token))
                ThrowSharedTaint(taintKind, site);
            i = _c.RgctxMethodSlotIndex(_method, kind, token);
            if (SharedDirectCallees is not null)
            {
                _c.SharedRgctxRoots.Add(_method);
                if (!_c.SharedMethodSlotKeys.TryGetValue(_method, out var keys))
                    _c.SharedMethodSlotKeys[_method] = keys = new List<(MethodInfo, RgctxSlot)>();
                keys.Add((_method, new RgctxSlot(kind, token)));
            }
        }
        else
        {
            var owner = _method.DeclaringClass;
            if (!Compilation.ContainsCanonPlaceholder(owner)
                || _c.RgctxSlotKnownBad(owner, kind, token))
                ThrowSharedTaint(taintKind, site);
            i = _c.RgctxSlotIndex(owner, kind, token);
            if (SharedDirectCallees is not null)
            {
                _c.SharedRgctxRoots.Add(_method);
                if (!_c.SharedSlotKeys.TryGetValue(_method, out var keys))
                    _c.SharedSlotKeys[_method] = keys = new List<(ClassInfo, RgctxSlot)>();
                keys.Add((owner, new RgctxSlot(kind, token)));
            }
        }
        // CctorEnsureFn is the one slot kind whose VALUE is invoked — the emitted
        // ((void(*)())__rgctx[i])() names no callee symbol (no NoteNamedBodySymbol
        // edge) and spells no runtime token the [HotPath(NoAlloc)] body scan could
        // see, so what the ensure wrapper (and the cctor behind it) allocates would
        // be invisible to the closure walk. Treat it like calli: a statically
        // unprovable indirect call, folded into this body's dispatch verdict. The
        // data kinds stay unmarked — a ti_/sf_ slot read is not a call, and the
        // rgctx-table kinds ride a direct call whose edge IS recorded. No-op unless
        // recording is armed (planning included: arming happens at emission).
        if (kind == RgctxSlotKind.CctorEnsureFn)
            _c.NoteHotIndirectCall(_method);
        _usedRgctx = true;
        return $"__rgctx[{i}]";
    }

    /// <summary>Whether the current call-site token verifiably re-resolves to
    /// <paramref name="target"/> under this body's own context — the safety
    /// check before keying an rgctx slot on it.</summary>
    private bool CallTokenResolvesTo(MethodInfo target)
    {
        if (_callSiteToken == 0)
            return false;
        var handle = SRME.EntityHandle(_callSiteToken);
        if (handle.Kind is not (HandleKind.MethodDefinition or HandleKind.MemberReference
                                or HandleKind.MethodSpecification))
            return false;
        try
        {
            // Under this (canonical) body's own context the token can resolve
            // to a partially-canonical satellite whose donor is the target —
            // the same mapping the call binding applied.
            var resolved = _c.ResolveMethodHandle(_module, handle, _method.Context, _method.DeclaringClass);
            return resolved is not null && ReferenceEquals(_c.SharedDonor(resolved), target);
        }
        catch (Exception e) when (!Compilation.IsMustEscape(e))
        {
            return false;
        }
    }

    /// <summary>Records (planning pass) that this trial body dispatches through
    /// a canonical interface handle — resolved on real receivers via the alias
    /// rows. When two real interfaces of one allocated class collapse onto the
    /// same canonical form, that canonical handle is ambiguous (first row wins)
    /// and every body dispatching through it is tainted by
    /// <c>Compilation.FinalizeSharedGenerics</c> instead of sharing.</summary>
    private void NoteCanonicalItfDispatch(ClassInfo itf)
    {
        if (SharedDirectCallees is null || !Compilation.ContainsCanonPlaceholder(itf))
            return;
        if (!_c.SharedItfDispatches.TryGetValue(_method, out var set))
            _c.SharedItfDispatches[_method] = set = new HashSet<ClassInfo>();
        set.Add(ItfDispatchTi(itf));
    }

    /// <summary>The interface identity a dispatch site probes with
    /// <c>dn2cpp_resolve_interface</c>. A placeholder-bearing handle inside a shared
    /// canonical body must probe the FULLY canonical form: the alias rows on real
    /// receivers carry <see cref="Compilation.CanonicalInterfaceOf"/> of the real
    /// interface, which flattens a constructed reference argument
    /// (IEnumerable&lt;SymbolicRegexNode&lt;CnRef&gt;&gt; →
    /// IEnumerable&lt;CnRef&gt;) — a partially-canonical handle would match no row
    /// and trap. A real (placeholder-free) handle probes itself. The flattened
    /// form is noted for (opaque) emission so its ti_ symbol exists.</summary>
    private ClassInfo ItfDispatchTi(ClassInfo itf)
    {
        if (!Compilation.ContainsCanonPlaceholder(itf))
            return itf;
        var flat = _c.CanonicalInterfaceOf(itf) ?? itf;
        if (!ReferenceEquals(flat, itf) && flat.IntrinsicCppName is null)
            NoteReferencedType(flat);
        return flat;
    }

    /// <summary>The emitted symbol for a direct (non-virtual) call/address use of
    /// <paramref name="target"/> — the shared canonical implementation when one
    /// is assigned, else the method itself — recording the cascade edge when a
    /// shared-body candidate binds another canonical method by symbol.</summary>
    private string DirectCallSym(MethodInfo target)
    {
        var impl = _c.SharedDonor(target).Emittable;
        if (SharedDirectCallees is not null && Compilation.IsCanonicalMethod(impl))
            SharedDirectCallees.Add((impl, RgctxEdgePassable(impl)));
        // This body is about to spell impl's symbol out; some body must define it.
        _c.NoteNamedBodySymbol(_method, impl);
        return impl.CppName;
    }

    /// <summary>Whether this body could hand <paramref name="impl"/> a runtime
    /// generic context if the callee turns out to need the hidden parameter:
    /// receiver-derivable callees never need one, self-recursion forwards this
    /// body's own <c>__rgctx</c> unchanged, a same-class callee of a
    /// same-class-context caller gets that <c>__rgctx</c> too (a generic-method
    /// caller's own table is a METHOD table — never handed to a class-context
    /// callee, and vice versa), and any other needy callee — class- or
    /// method-context — needs a call-site token that verifiably names it (the
    /// table-forwarding slot's key).</summary>
    private bool RgctxEdgePassable(MethodInfo impl) =>
        !_c.WouldNeedRgctxParam(impl)
        || ReferenceEquals(impl, _method)
        || (impl.NameSuffix == "" && _method.NameSuffix == ""
            && ReferenceEquals(impl.DeclaringClass, _method.DeclaringClass))
        || CallTokenResolvesTo(impl);

    /// <summary>The full direct-call expression binding <paramref name="target"/>'s
    /// donated body: the shared canonical symbol when one is assigned, with a
    /// C-style pointer cast on every argument whose erased type in the donor's
    /// signature (a reference argument collapsed to <c>Dn2CppObject*</c>)
    /// differs from the call site's static type, plus the hidden rgctx argument
    /// when due. <paramref name="args"/> holds the receiver first for instance
    /// methods/ctors; the receiver needs no cast (grouped struct names redirect
    /// to the owner's). Value-type and scalar parameters never differ (layout
    /// identity is the grouping invariant), so only pointer spellings cast.</summary>
    private string DirectCall(MethodInfo target, IReadOnlyList<string> args)
    {
        var impl = _c.SharedDonor(target).Emittable;
        var text = args as List<string> ?? args.ToList();
        if (!ReferenceEquals(impl, target))
        {
            var real = target.Signature.ParameterTypes;
            var shared = impl.Signature.ParameterTypes;
            int off = text.Count - real.Length;
            for (int i = 0; i < real.Length && i < shared.Length; i++)
            {
                string s = CppTypes.Of(shared[i]);
                if (s.EndsWith("*", StringComparison.Ordinal) && s != CppTypes.Of(real[i]))
                    // A headerless argument (the NFI trio, Assembly/Module) does
                    // not pun into the erased Dn2CppObject* parameter: the canonical
                    // body treats it as a real managed object (equality, ToString,
                    // stores into erased slots), so it gets the interned wrapper —
                    // the same conversion every other headerless→object boundary
                    // makes. One funnel decides (ErasedBoundaryCast → Cast), shared
                    // with RgctxForwarderBody so the verdict and its spelling are
                    // never maintained in two places.
                    text[off + i] = ErasedBoundaryCast(text[off + i], CppTypes.Of(real[i]), real[i], s);
            }
        }
        return $"{DirectCallSym(target)}({ArgsWithRgctx(string.Join(", ", text), target)})";
    }

    /// <summary>Joins already-built argument text with the callee's hidden
    /// rgctx argument when one is due (see <see cref="RgctxCallSuffix"/>).</summary>
    private string ArgsWithRgctx(string args, MethodInfo target)
    {
        string sfx = RgctxCallSuffix(target);
        if (sfx.Length == 0)
            return args;
        return args.Length == 0 ? sfx[2..] : args + sfx;
    }

    /// <summary>The trailing argument list a direct call to
    /// <paramref name="target"/> needs: the callee's hidden rgctx parameter when
    /// the shared callee takes one — this body's own context for self-recursion
    /// or a same-class-context callee, else the callee's table (its class's for
    /// a class-context callee, the real instantiation's per-method one for a
    /// generic-method callee) read out of a forwarding slot. Empty for every
    /// real method and during planning (whose text is discarded; the
    /// passability was recorded on the edge instead).</summary>
    private string RgctxCallSuffix(MethodInfo target)
    {
        if (!SharedTrial || SharedDirectCallees is not null)
            return "";
        var impl = _c.SharedDonor(target).Emittable;
        if (!Compilation.IsCanonicalMethod(impl) || !impl.RgctxParam)
            return "";
        _usedRgctx = true;
        if (ReferenceEquals(impl, _method)
            || (impl.NameSuffix == "" && _method.NameSuffix == ""
                && ReferenceEquals(impl.DeclaringClass, _method.DeclaringClass)))
            return ", __rgctx";
        if (!CallTokenResolvesTo(impl))
            throw new InvalidOperationException(
                $"{_method.DeclaringClass.FullName}.{_method.Name}: rgctx pass to {impl.CppName} "
                + "has no verifiable call-site token (the planning pass should have tainted this body)");
        string slot = RgctxSlotAccess(
            impl.NameSuffix != "" ? RgctxSlotKind.MethodRgctxTable : RgctxSlotKind.RgctxTable,
            _callSiteToken, "rgctx-pass", impl.CppName);
        return $", (const void* const*){slot}";
    }

    public MethodCompiler(Compilation c, MethodInfo method, LiteralPool literals, IEmitBackend? backend = null)
    {
        _c = c;
        _method = method;
        _module = method.Module;
        _reader = _module.Reader;
        _literals = literals;
        _backend = backend;
        _intrinsics = backend?.CallIntrinsics;
    }

    /// <summary>Marks <paramref name="cls"/> and its base chain as referenced so
    /// their type-infos are emitted. Used by call intrinsics that materialize a
    /// managed instance of a type which may not otherwise appear in a reachable
    /// signature (e.g. the Godot bridge wrapping an engine object return).</summary>
    public void NoteReferencedType(ClassInfo cls)
    {
        for (ClassInfo? c = cls; c is not null; c = c.BaseClass)
            _c.NoteReferencedType(c);
    }

    /// <summary>Every type a dispatch function-pointer signature (<see cref="FnPtrType"/>) spells
    /// as a bare <c>t_</c> — the declaring class (the receiver cast), the return, and each
    /// parameter, each looked through a byref/pointer wrapper — must have its layout emitted, or
    /// the cast names an undeclared struct. A method reached only through a dispatch slot puts
    /// its signature's types through no other emit edge: the slot lives on the RECEIVER's
    /// interface/vtable, so the interface/virtual method declaration itself is not necessarily
    /// reachable, and its signature types (a returned interface, a by-value struct parameter)
    /// are never met by <see cref="CppEmitter.ComputeEmitted"/>'s reachable-signature closure.
    /// <c>IDesignerHost.GetDesigner(IComponent) -&gt; IDesigner</c>, dispatched off an
    /// <c>IServiceProvider</c>, is exactly that — neither <c>IDesigner</c> nor <c>IComponent</c>
    /// appears anywhere else. So note each for emission (a reference/interface type its opaque
    /// forward decl + minimal type-info; a value type its full by-value layout) and record the
    /// naming for the named-struct backstop.</summary>
    private void NoteDispatchSignatureTypes(MethodInfo callee)
    {
        NoteDispatchSignatureType(TypeDesc.MakeClass(callee.DeclaringClass));
        NoteDispatchSignatureType(callee.Signature.ReturnType);
        foreach (var p in callee.Signature.ParameterTypes)
            NoteDispatchSignatureType(p);
    }

    private void NoteDispatchSignatureType(TypeDesc t)
    {
        while (t is { Element: { } el, Kind: TypeKind.ByRef or TypeKind.Pointer })
            t = el;
        if (t is not { Kind: TypeKind.Class, Class: { IsEnum: false } cls } || cls.IntrinsicCppName is not null)
            return;
        _c.NoteNamedStructSymbol(_method, cls.CppStructName);
        if (cls.IsValueType)
            _c.NoteForceEmit(cls);
        else
            NoteReferencedType(cls);
    }

    /// <summary>Records <paramref name="element"/>[] so its precise per-element
    /// array type-info (<c>ti_arr_*</c>) is emitted. Used by call intrinsics that
    /// materialize a managed array outside the normal newarr path (e.g. the Godot
    /// bridge copying a packed array out into a managed array).</summary>
    public void NoteArrayElementType(TypeDesc element) => _c.NoteArrayElementType(element);

    /// <summary>An instance-field access (ldfld/ldflda/stfld) on a by-value struct
    /// reads/writes a real member of its C++ layout, so that struct must be emitted
    /// with its real fields — opaque (the dead-branch-cast layout) is not enough.
    /// A struct used only inside a transpiled BCL body (System.Threading.Volatile's
    /// nested VolatileObject/VolatileUIntPtr in Volatile.Read&lt;T&gt) enters the
    /// signature-driven emit set through no edge, so force its full layout. A
    /// reference type needs the same when it is reached only via a base-typed
    /// castclass + field read and never newobj'd in the reachable closure (e.g.
    /// UnicodeEncoding.Decoder, cast from a DecoderNLS parameter inside
    /// UnicodeEncoding.GetChars to read lastByte/lastChar): without this it would
    /// stay an opaque shell (ReferencedTypes) and the field would be undefined in
    /// the native build. Intrinsic owners are filtered out by ComputeEmitted.Add.</summary>
    private void NoteFieldOwnerLayout(ClassInfo cls)
        => _c.NoteForceEmit(cls);

    /// <summary>Records a value-struct an intrinsic names by VALUE — as a by-value temp, a
    /// <c>sizeof</c>, or a deref — for FULL-layout emission. Such a struct must be a complete
    /// C++ type at the point the body names it, but an intrinsic reaches it through none of
    /// the edges ComputeEmitted's closure follows: not a reachable call's signature (the
    /// intrinsic replaced the call), not a field, not a local. The type's ONLY mouth is the
    /// intrinsic itself, so if the intrinsic stays quiet nothing declares the struct — and
    /// <b>the transpile reports success while the C++ compile fails</b> on an undeclared
    /// <c>t_&lt;Name&gt;</c>, in a toolchain that knows nothing about IL or reachability.
    /// Two shapes:
    ///
    /// <list type="bullet">
    /// <item><b>A reinterpret's output type</b> (<c>Unsafe.BitCast&lt;TFrom,TTo&gt;</c>, the
    /// MemoryMarshal span-shapers Cast/AsBytes/CreateSpan/CreateReadOnlySpan): TTo is minted
    /// out of bits, so it need appear nowhere else — and the mint can sit in a branch that is
    /// dead FOR THE EMITTED INSTANTIATION and still name the struct (a BitCast guarded by
    /// <c>if (typeof(TChar) != typeof(char))</c> names the byte span from the char
    /// instantiation).</item>
    /// <item><b>A width/deref over an arbitrary T</b> (<c>Unsafe.SizeOf&lt;T&gt;</c>,
    /// <c>Read</c>/<c>ReadUnaligned</c>/<c>Write</c>/<c>WriteUnaligned</c>): the result is an
    /// int, or is minted from a bare pointer, so T itself flows nowhere the closure looks.
    /// </item>
    /// </list>
    ///
    /// Only a NON-ENUM VALUE TYPE is recorded, because only that spelling demands a complete
    /// type: <see cref="CppTypes.Of"/> lowers an enum to <c>int32_t</c>/<c>int64_t</c> and a
    /// reference type to a pointer, so recording those would force-emit layouts the output
    /// does not want. Same seam as <see cref="NoteFieldOwnerLayout"/>, and idempotent — a
    /// no-op when the type is already reached, so any program that already compiled emits
    /// byte-identically.</summary>
    private void NoteBuiltStructLayout(TypeDesc t)
    {
        if (t is { Kind: TypeKind.Class, Class: { IsValueType: true, IsEnum: false } cls })
            _c.NoteForceEmit(cls);
    }

    /// <summary>A <c>newarr</c> / <c>Array.Empty&lt;T&gt;</c> lowering emits
    /// <c>sizeof(t_&lt;T&gt;)</c> over a value element's storage struct, so force that struct's
    /// full layout (same seam as <see cref="NoteBuiltStructLayout"/>) and record the naming for
    /// <see cref="CppEmitter.AssertNamedStructsDefined"/>. <c>Array.Empty&lt;T&gt;</c> is the
    /// mouth that matters: it never instantiates T, so a value struct reached through no other
    /// edge (a BCL type appearing only behind <c>Array.Empty</c>, e.g.
    /// <c>System.Reflection.ParameterModifier</c>) would otherwise leave its <c>t_</c>
    /// undeclared at the C++ compile.</summary>
    private void NoteArraySizeofStruct(TypeDesc element)
    {
        NoteBuiltStructLayout(element);
        // Record the SPELLING the sizeof actually emits (CppTypes.StorageOf), never the bare
        // CppStructName: an intrinsic value type (Decimal/DateTime/TimeSpan) sizes as its
        // runtime struct (Dn2CppDecimal), which NoteNamedStructSymbol's t_ filter drops — so
        // recording CppStructName here would witness a t_ the body never names, a false hole.
        _c.NoteNamedStructSymbol(_method, CppTypes.StorageOf(element));
    }

    /// <summary>Whether this method's body is emitted as an <c>inline</c>
    /// definition in the shared header instead of a .cpp TU: an inlining hint
    /// can only take effect across TU boundaries when the call site sees the
    /// definition (bodies otherwise have external linkage in one TU, and the
    /// native build has no LTO). Promoted: <c>[MethodImpl(AggressiveInlining)]</c>
    /// bodies of at most <see cref="MethodInfo.IsSmallIlBody"/>'s IL-size cap
    /// (an uncapped promotion lets a handful of giant attributed generic bodies
    /// dominate the header and every TU's parse time — see the constant's doc),
    /// and tiny IL bodies (field accessors, one-op operator wrappers —
    /// see <see cref="MethodInfo.IsTinyIlBody"/>) whose out-of-line form costs a
    /// real call per use in hot loops (e.g. Span.get_Length as a LINQ loop
    /// condition). NoInlining wins when both flags are set; an
    /// <c>[UnmanagedCallersOnly]</c> root is called from native code only, so
    /// there is no managed call site to inline into. A <c>[HotPath]</c> body is
    /// excluded too: promotion would compile it under every including TU's plain
    /// flags instead of the hot TU's (see <see cref="MethodInfo.IsHotPath"/>).</summary>
    public static bool EmitsInline(MethodInfo m)
        => (m.IsAggressiveInlining && m.IsSmallIlBody || m.IsTinyIlBody)
            && !m.IsNoInlining && !m.IsUnmanagedCallersOnly && !m.IsHotPath;

    public static string Signature(MethodInfo m)
    {
        var ps = new List<string>();
        int i = 0;
        if (!m.IsStatic)
            // A transpiled String interface impl takes the runtime string as its
            // receiver, not the opaque t_System_String shell; every other declaring
            // class keeps its struct pointer.
            ps.Add(m.DeclaringClass.FullName == "System.String"
                ? $"Dn2CppString* a{i++}"
                : $"{m.DeclaringClass.CppStructName}* a{i++}");
        foreach (var p in m.Signature.ParameterTypes)
            ps.Add($"{CppTypes.Of(p)}{(NoAliasParam(m, p) ? " __restrict" : "")} a{i++}");
        // A shared canonical body with no receiver-derivable context source
        // takes its runtime generic context as a hidden trailing parameter;
        // per-instantiation forwarders append their class's table.
        if (m.RgctxParam)
            ps.Add("const void* const* __rgctx");
        string ret = m.Signature.ReturnType.IsVoid ? "void" : CppTypes.Of(m.Signature.ReturnType);
        // MethodImpl inlining hints: the qualifier rides on prototype and
        // definition alike (every consumer is a declaration context). NoInlining
        // goes through the DN2CPP_NOINLINE macro (dn2cpp.h), not a bare
        // [[gnu::noinline]]: real MSVC (cl.exe) ignores the C++ attribute and
        // would inline the body anyway, which breaks methods that are
        // non-inlinable for conservative-GC correctness, not just for size.
        string qual = m.IsNoInlining ? "DN2CPP_NOINLINE " : EmitsInline(m) ? "inline " : "";
        return $"{qual}{ret} {m.CppName}({string.Join(", ", ps)})";
    }

    /// <summary><c>[HotPath(NoAlias = true)]</c>: whether this parameter carries
    /// <c>__restrict</c> — the caller's promise that the memory reached through it
    /// is reached through no other parameter of the call. Nothing verifies it;
    /// overlapping arguments are UB, exactly as an out-of-range index is under
    /// <c>SkipBoundsChecks</c>.
    /// <para>Qualification is decided by the parameter's <see cref="TypeDesc"/>
    /// KIND, never by its rendered C++ text ending in <c>*</c>: a class-typed
    /// parameter is a C++ pointer too, and two object references may legitimately
    /// alias — <c>__restrict</c> there would be a correctness lie, not a hint. The
    /// four kinds that qualify are the ones whose pointer addresses DATA: an array
    /// reference (the element block hangs off it, so the qualifier propagates to
    /// every <c>-&gt;data</c> access based on it), a multidimensional array, a byref,
    /// and an unmanaged pointer.</para>
    /// <para>The receiver (<c>a0</c>) never qualifies and is not routed through
    /// here: <c>this</c> aliasing a parameter is ordinary, not a bug.</para>
    /// <para>Emitted from the one shared signature builder, so prototype, definition,
    /// wrapper, rgctx forwarder and trap stub agree by construction.</para></summary>
    private static bool NoAliasParam(MethodInfo m, TypeDesc p)
        => m.NoAlias
            && p.Kind is TypeKind.SZArray or TypeKind.MDArray or TypeKind.ByRef or TypeKind.Pointer;

    public string Compile()
    {
        // Decode the HotPath bits for every reachable body here. The planning pass compiles
        // all of them before the emission phase arms its recording, so this guarantees
        // Compilation.NoAllocMethods (populated by IsHotPath's decode) is complete at
        // BeginCallSymbolAudit — the arming decision reads its count.
        _ = _method.IsHotPath;
        // A [HotPath] body never donates itself as a shared canonical body — its
        // whole point is per-instantiation optimization — so the trial taints
        // immediately and every grouped user compiles its own monomorphic copy,
        // exactly like any other taint-driven fallback.
        if (SharedTrial && _method.IsHotPath)
            throw new SharedBodyTaintException("hotpath",
                $"{_method.DeclaringClass.FullName}.{_method.Name}: [HotPath] bodies compile per instantiation");
        var bodyBlock = _module.PE.GetMethodBody(_method.Rva);
        List<Instruction> insns;
        try
        {
            insns = ILDecoder.Decode(bodyBlock.GetILBytes()!.ToImmutableArrayCompat());
        }
        catch (NotSupportedException ex)
        {
            // Name the (often BCL) method so unsupported-IL failures are
            // actionable when a real CoreLib body is reached.
            throw new NotSupportedException($"{_method.DeclaringClass.FullName}.{_method.Name}: {ex.Message}");
        }

        BuildExceptionRegions(bodyBlock);
        BuildArgsAndLocals(bodyBlock);
        SetupNoAliasSpanLocals(insns);

        // The reachability scanner computed the identical pass with the same
        // Compilation-backed callbacks (Compilation.ScanBodyForGenerics), so
        // the folds and elisions here mirror the reachable set exactly.
        // Memoized per method: the reachability scan fills, both the planning and
        // the emission compile hit — soundness argument at
        // BranchLiveness.ComputeCached.
        _liveness = BranchLiveness.ComputeCached(_method, insns, bodyBlock,
            tok => _c.ConstFoldedCallTarget(_module, tok),
            tok => _c.ClassifyTypeIdentityCall(_module, tok),
            (tokA, tokB) => _c.TypeEqualityVerdict(_module, tokA, tokB, _method.Context));

        foreach (var insn in insns)
        {
            // Dead branch instructions register no labels, and a folded branch
            // registers only its live edge — so a dead arm's join target never
            // emits a dangling (unused) C++ label.
            if (_liveness is not null && !_liveness.LiveAt(insn.Offset))
                continue;
            if (ILDecoder.IsBranch(insn.OpCode))
            {
                if (_liveness?.VerdictAt(insn.Offset) != false)
                {
                    _labels.Add((int)insn.Operand);
                    NoteBranchSource((int)insn.Operand, insn.Offset);
                }
            }
            if (insn.SwitchTargets is not null)
            {
                foreach (int t in insn.SwitchTargets)
                {
                    _labels.Add(t);
                    NoteBranchSource(t, insn.Offset);
                }
            }
        }

        // Classify each ldftn as a delegate creation (its result feeds a delegate
        // `newobj`) vs a function-pointer use (everything else — stored in a
        // delegate*<...>, passed as an arg, invoked by calli). Only the delegate
        // case needs the target-slot adapter for a static target. The delegate CLASS
        // is recorded with it (null when the token would not resolve): its `Invoke`
        // arity is what tells an open static delegate from a closed one, and reading
        // it here — rather than at the ldftn — would decode a signature for every
        // instance-target site too.
        for (int i = 0; i + 1 < insns.Count; i++)
            if (insns[i].OpCode == ILOpCode.Ldftn
                && insns[i + 1].OpCode == ILOpCode.Newobj
                && NewobjTargetIsDelegate(insns[i + 1], out var dgClass))
                _ftnDelegateUse[insns[i].Offset] = dgClass;

        foreach (var insn in insns)
        {
            // 1. Close catch blocks ending here (innermost first).
            if (_catchGroupEndsAt.TryGetValue(insn.Offset, out var closingGroups))
                foreach (var g in closingGroups)
                    _body.AppendLine("    }");

            // 2. Finally transition: close try, capture exception, fall into body.
            if (_finallyHandlerStarts.TryGetValue(insn.Offset, out var finRegion))
            {
                _body.AppendLine($"    }} catch (...) {{ __eptr{finRegion.Index} = std::current_exception(); }}");
                // The label is the join target for `leave`s routed through this
                // finally. By here every such leave (direct, and chained from inner
                // finallys) is already emitted, so skip the label when there are
                // none — the body is then reached only by the exceptional fall-
                // through above, and an unused label would warn.
                if (finRegion.LeaveTargets.Count > 0)
                    _body.AppendLine($"__fin{finRegion.Index}:;");
                _stack = new List<StackEntry>();
                _unreachable = false;
            }

            // 2b. Fault transition: close try, capture the exception. The normal
            // path leaves the try with a `leave` that jumps past this handler, so
            // only the exceptional path falls through the catch into the body;
            // `endfault` re-raises the captured exception.
            if (_faultHandlerStarts.TryGetValue(insn.Offset, out var fltRegion))
            {
                _body.AppendLine($"    }} catch (...) {{ __feptr{fltRegion.Index} = std::current_exception(); }}");
                _stack = new List<StackEntry>();
                _unreachable = false;
            }

            // 3. Catch dispatch: close try, open one C++ catch, route to the first
            // handler in IL order (typed isinst tests; a filter clause hands off to
            // its filter block, which resumes the chain at endfilter).
            if (_catchDispatchAt.TryGetValue(insn.Offset, out var grp))
            {
                _body.AppendLine($"    }} catch (Dn2CppException& __ex{grp.Index}) {{");
                EmitDispatchFrom(grp, 0);
                _unreachable = true;
            }

            // 3b. Filter block start: bind the exception as entry stack so the
            // filter IL (which ends in `endfilter`) can test it.
            if (_filterBlockAt.TryGetValue(insn.Offset, out var fb))
            {
                _body.AppendLine($"F{fb.G.Index}_{fb.Clause}:;");
                if (_declared.Add("bs0_Ref"))
                    _decls.Add(("Dn2CppObject*", "bs0_Ref"));
                _body.AppendLine($"    bs0_Ref = __ex{fb.G.Index}.obj;");
                _stack = new List<StackEntry> { new("bs0_Ref", StackKind.Ref, "Dn2CppObject*") };
                _unreachable = false;
            }

            // 4. Catch clause labels: bind the exception object as entry stack.
            if (_clauseLabelAt.TryGetValue(insn.Offset, out var clauses))
            {
                foreach (var (g, ci) in clauses)
                {
                    _body.AppendLine($"H{g.Index}_{ci}:;");
                    if (_declared.Add("bs0_Ref"))
                        _decls.Add(("Dn2CppObject*", "bs0_Ref"));
                    _body.AppendLine($"    bs0_Ref = __ex{g.Index}.obj;");
                }
                _stack = new List<StackEntry> { new("bs0_Ref", StackKind.Ref, "Dn2CppObject*") };
                _unreachable = false;
            }

            // 5. Regular branch labels. `_labels` is a purely structural pre-scan
            // (every branch instruction's operand, decoded once up front) — it
            // does not know that a brtrue/brfalse on a statically NonNull operand
            // (see the `box` of a non-nullable value type, above) never actually
            // takes its edge and skips calling BranchTo for it. So being in
            // `_labels` does not by itself mean this offset is reachable. A label
            // with no recorded entry stack while `_unreachable` is one of two
            // very different things, told apart by `_backwardBranchTargets`:
            //  - A loop head: some branch naming it sits at a LATER offset, not
            //    processed yet (the canonical `br COND; BODY: …; COND: blt BODY`
            //    shape). Resume with the empty stack — the C# compiler branches
            //    to a loop head only at a statement boundary, where the stack is
            //    empty, which is what the later backward BranchTo then records.
            //  - Dead code: every branch naming it was already processed, yet
            //    none recorded an entry stack — each such edge was statically
            //    elided (the NonNull fold). Resuming there with a fabricated
            //    empty stack corrupts the NEXT real merge point this dead code
            //    falls through into, since the live predecessor's recorded entry
            //    stack then mismatches the phantom one. Stay unreachable instead;
            //    `if (_unreachable) continue;` below keeps skipping until an
            //    offset with a recorded entry stack.
            // 4b. Folded-guard dead arm (BranchLiveness): not emitted, exactly
            // as it was not scanned for reachability. Placed after the region
            // bookkeeping above (a catch-group close can land on any offset)
            // and before the label handler (a dead offset's labels were never
            // registered; nothing may resurrect emission inside the arm).
            if (_liveness is not null && !_liveness.LiveAt(insn.Offset))
                continue;

            if (_labels.Contains(insn.Offset))
            {
                if (!_unreachable)
                    BranchTo(insn.Offset, emitGoto: false);
                bool hasEntry = _entryStacks.TryGetValue(insn.Offset, out var entry);
                bool laterEdgeMayArrive = !hasEntry && _backwardBranchTargets.Contains(insn.Offset);
                _body.AppendLine($"IL_{insn.Offset:X4}:;");
                _stack = hasEntry ? new List<StackEntry>(entry!) : new List<StackEntry>();
                _unreachable = !hasEntry && !laterEdgeMayArrive;
            }

            // 6. Open try blocks starting here (outermost first).
            if (_tryOpensAt.TryGetValue(insn.Offset, out var openingTries))
                foreach (var (_, _) in openingTries)
                    _body.AppendLine("    try {");

            if (_unreachable)
                continue;
            // A folded type-identity window (ldtoken..op_Equality) is
            // elided wholesale — nothing is pushed, no type-info is referenced,
            // and the closing branch (not elided; handled by its VerdictAt arm)
            // emits the single live edge popping nothing. The window is
            // stack-neutral, so the entry stack recorded at its start is
            // exactly the stack the branch continues with.
            if (_liveness?.ElidedAt(insn.Offset) == true)
                continue;
            Translate(insn);
        }

        var sb = new StringBuilder();
        sb.AppendLine($"// {_method.DeclaringClass.FullName}::{_method.Name}");
        // External linkage (no `static`): the body may live in a different translation unit
        // than its callers once the output is split across files; the header carries the
        // forward declaration. Unused external functions don't warn, so no [[maybe_unused]].
        sb.AppendLine($"{Signature(_method)}");
        sb.AppendLine("{");
        // An [UnmanagedCallersOnly] method can be invoked from a thread the
        // collector has never seen (a native host's own thread pool); the prologue
        // — inert unless the host enables it — registers the thread before any
        // allocation or stack scan the body can trigger.
        if (_method.IsUnmanagedCallersOnly)
            sb.AppendLine("    dn2cpp_native_callback_prologue();");
        // Opt-in shadow stack (--shadow-stack): an RAII frame guard first among the
        // guards, so it is constructed before the monitor guard and destroyed after
        // it — the frame brackets the whole body on every return path and on unwind.
        // The name is a raw C string literal, NOT a LiteralPool str_N entry: the pool
        // is numbered during the planning pass, and pooling the name would perturb
        // that numbering. Emitted identically in both passes (no pass branch).
        if (_c.ShadowStackEnabled)
            sb.AppendLine($"    Dn2CppShadowFrame __shadowFrame(\"{ShadowFrameName()}\");");
        // [MethodImpl(MethodImplOptions.Synchronized)]: the whole body runs under
        // the identity-keyed monitor — lock(this) for an instance method, lock on
        // the declaring type's interned Type object for a static one (the same
        // object typeof(X) yields, so user lock(typeof(X)) sites mutually exclude
        // with the method, as in .NET). The RAII guard is the first local, so it
        // releases after every return path and on exceptional unwind. A value-type
        // instance method is skipped (the CLR locks a throwaway per-call box
        // there — no mutual exclusion to preserve).
        if (_method.IsSynchronized && !(_method.DeclaringClass.IsValueType && !_method.IsStatic))
        {
            if (_method.IsStatic)
            {
                // A canonical shared body has no ti_ of its own; taint so each
                // real instantiation compiles its own body locking its own type.
                TaintIfCanonical(_method.DeclaringClass, "synchronized");
                sb.AppendLine("    Dn2CppMonitorGuard __syncGuard((Dn2CppObject*)"
                    + $"dn2cpp_get_type_from_handle(&{_method.DeclaringClass.CppTypeInfoName}));");
            }
            else
            {
                sb.AppendLine("    Dn2CppMonitorGuard __syncGuard((Dn2CppObject*)a0);");
            }
        }
        // Shared canonical body that read the runtime generic context without
        // taking the hidden parameter: derive it from the receiver's dynamic
        // type at this class's declaring level. Emitted only when a slot was
        // actually used (the lazy prologue), and only in the emission pass
        // (planning text is discarded, and its flags are not final yet).
        if (_usedRgctx && !_method.RgctxParam && SharedDirectCallees is null)
        {
            if (!_method.RgctxUses)
                throw new InvalidOperationException(
                    $"{_method.DeclaringClass.FullName}.{_method.Name}: body uses rgctx but the "
                    + "planning pass did not record it");
            string anchor = _c.RgctxAnchorSym(_method.DeclaringClass)
                ?? throw new InvalidOperationException(
                    $"{_method.DeclaringClass.FullName}.{_method.Name}: rgctx use with no anchor "
                    + "and no hidden parameter");
            sb.AppendLine("    const void* const* __rgctx = "
                + $"dn2cpp_rgctx(((const Dn2CppObject*)a0)->type, &{anchor});");
        }
        // [HotPath(NoAlias)] span parameters: the element pointer hoisted once,
        // qualified. Only the entries an indexer route actually addressed are
        // declared (see SetupNoAliasSpanLocals), so a body that never indexes a
        // span parameter emits exactly what it emitted before the knob existed.
        foreach (var (argSlot, argName, localName, elemSt) in _noAliasSpans)
            if (_noAliasSpansUsed.Contains(argSlot))
                sb.AppendLine($"    {elemSt}* __restrict {localName} = ({elemSt}*){argName}.f__reference;");
        foreach (var l in _locals)
            sb.AppendLine($"    {l.CppType} {l.Name} = {CppTypes.ZeroInit(l.CppType)};");
        foreach (var d in _decls)
            sb.AppendLine($"    [[maybe_unused]] {d.Type} {d.Name};");
        sb.Append(_body);
        sb.AppendLine("}");
        return sb.ToString();
    }

    /// <summary>The frame name the shadow-stack prologue bakes into this body's
    /// guard: the full rendered frame line minus its "   at " prefix,
    /// <c>Ns.Type.Method()</c> — matching what <c>dn2cpp_exc_trace_render</c>
    /// prints for a kind-0 (PC) frame, so the two trace kinds read alike. The
    /// type name is the reflection name (<see cref="Compilation.ReflectionTypeName"/>,
    /// what <c>Dn2CppTypeInfo.name</c> carries; decode-free, so this mints no
    /// closed generic), the method name the simple name (<c>.ctor</c>/<c>.cctor</c>
    /// as-is — <c>Ns.Type..ctor()</c>; a generic-method instantiation keeps its
    /// simple name, no NameSuffix mangling), and <c>()</c> is baked with no
    /// parameter list — the same declared degrade as the throw-time trace render.
    /// <para>A canonical shared body serves every instantiation in its group and
    /// cannot name its placeholder class in a user-facing line: it names the
    /// Ordinal-least <see cref="ClassInfo.SharedUsers"/> reflection name instead —
    /// deterministic whatever order grouping ran in — with the kind-0 render's
    /// " [shared generic]" mark. No users yet falls back to the placeholder
    /// <see cref="ClassInfo.FullName"/>, still deterministic; a method-dimension
    /// canonical on a real class keeps the real class name, mark included.</para>
    /// Escaped for a raw C string literal (the CppEmitter trap-body idiom).</summary>
    private string ShadowFrameName()
    {
        var cls = _method.DeclaringClass;
        string typeName;
        string mark = "";
        if (Compilation.IsCanonicalMethod(_method))
        {
            mark = " [shared generic]";
            if (Compilation.ContainsCanonPlaceholder(cls))
            {
                string? best = null;
                foreach (var user in cls.SharedUsers)
                {
                    string name = Compilation.ReflectionTypeName(user);
                    if (best is null || string.CompareOrdinal(name, best) < 0)
                        best = name;
                }
                typeName = best ?? cls.FullName;
            }
            else
            {
                typeName = Compilation.ReflectionTypeName(cls);
            }
        }
        else
        {
            typeName = Compilation.ReflectionTypeName(cls);
        }
        return (typeName + "." + _method.Name + "()" + mark)
            .Replace("\\", "\\\\").Replace("\"", "\\\"");
    }

    /// <summary>Compiles a synthesized body for a method whose managed body is
    /// a skipped placeholder (an engine shim whose calls the intrinsics replace
    /// inline): the method's own parameters are staged as the evaluation stack —
    /// exactly the operand stack an AOT call site would present — and the call
    /// intrinsic lowers a call to the method itself into the body, so the
    /// marshalling semantics are identical to an AOT call site. The function is
    /// emitted under the method's normal name and signature, so the standard
    /// header declaration, reflection fnPtr wiring and invoker-thunk generation
    /// apply unchanged (the hot-update interpreter binds patch method imports
    /// through that fnPtr/invoker pair). Returns null when the intrinsics cannot
    /// lower the call — the method then stays bodyless, the pre-existing
    /// missing-AOT boundary.</summary>
    internal string? CompileIntrinsicCallWrapper()
    {
        if (_intrinsics is not { } intrinsics)
            return null;
        return SynthesizeWrapperBody(
            _method.DeclaringClass.CppStructName + "*",
            () => intrinsics.TryEmitCall(this, _method, isCallvirt: false),
            "synthesized engine-call body");
    }

    /// <summary>The stage/lower/return/balance/assemble skeleton shared by the two
    /// wrapper synthesizers above and below: the method's own parameters are staged as
    /// the evaluation stack — exactly the operand stack an AOT call site would present —
    /// <paramref name="lower"/> lowers a call to the method itself into the body, and the
    /// result is rendered under the method's normal name and signature
    /// (<see cref="RenderWrapperBody"/>). Null when the lowering declines, throws
    /// NotSupportedException, or leaves the stack unbalanced — the method then stays
    /// bodyless.</summary>
    private string? SynthesizeWrapperBody(string recvCppType, Func<bool> lower, string tag)
    {
        int i = 0;
        if (!_method.IsStatic)
        {
            _stack.Add(new StackEntry($"a{i++}",
                _method.DeclaringClass.IsValueType ? StackKind.Ptr : StackKind.Ref,
                recvCppType));
        }
        foreach (var p in _method.Signature.ParameterTypes)
        {
            _stack.Add(new StackEntry($"a{i}", CppTypes.KindOf(p), CppTypes.Of(p), StaticType: p));
            i++;
        }
        try
        {
            if (!lower())
                return null;
        }
        catch (NotSupportedException)
        {
            return null; // a shape the lowering does not model stays bodyless
        }
        var rt = _method.Signature.ReturnType;
        if (!rt.IsVoid)
            Emit($"return {CoerceTo(Pop(), rt, CppTypes.Of(rt))};");
        if (_stack.Count != 0)
            return null; // defensive: an unbalanced lowering is not a valid body
        return RenderWrapperBody(tag);
    }

    /// <summary>Renders a synthesized body under the method's normal name and signature:
    /// the leading comment (<paramref name="tag"/> is its parenthetical), the standard
    /// header declaration, the hoisted temp declarations, and the emitted body. Shared by
    /// every synthesized-body path (the wrapper synthesizers here, the ValueEquality field
    /// walks, the HTTP shim).</summary>
    private string RenderWrapperBody(string tag)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"// {_method.DeclaringClass.FullName}::{_method.Name} ({tag})");
        sb.AppendLine($"{Signature(_method)}");
        sb.AppendLine("{");
        foreach (var d in _decls)
            sb.AppendLine($"    [[maybe_unused]] {d.Type} {d.Name};");
        sb.Append(_body);
        sb.AppendLine("}");
        return sb.ToString();
    }

    /// <summary>Synthesizes a standalone function body for a method of an
    /// intrinsic-mapped type whose ADDRESS is taken (ldftn / delegate method group,
    /// e.g. <c>Char.IsDigit</c>): seeds the stack with the parameters and applies
    /// the same core intrinsic lowering a call site would
    /// (<see cref="EmitIntrinsic"/>), so the emitted function computes exactly what
    /// the interception computes. A closed generic instantiation
    /// (<c>Array.Empty&lt;int&gt;</c>) instead replays the generic intrinsic's
    /// token-free lowering (<see cref="TryTokenFreeGenericIntrinsic"/>) from the
    /// instantiation's own type arguments. The counterpart of
    /// <see cref="CompileIntrinsicCallWrapper"/> for core (non-backend) intrinsics.
    /// Null when the method has no intrinsic lowering to synthesize from.</summary>
    internal string? CompileCoreIntrinsicWrapper()
    {
        string dispatch = CoreIntrinsics.IsIntrinsicType(_method.DeclaringClass.FullName)
            ? _method.DeclaringClass.FullName
            : IntrinsicDispatchName(_method.DeclaringClass);
        string? body = SynthesizeWrapperBody(
            CppTypes.Of(TypeDesc.MakeClass(_method.DeclaringClass)),
            () =>
            {
                if (_method.Context.MethodArgs is { Length: > 0 } targs)
                {
                    // A closed generic instantiation on an intrinsic-mapped type
                    // (Array.Empty<int>): replay the generic intrinsic's lowering from
                    // the resolved instantiation's type arguments — there is no
                    // call-site MethodSpec token here, and the non-generic EmitIntrinsic
                    // tables must not see it (a name collision with a non-generic
                    // overload would lower the wrong shape).
                    return TryTokenFreeGenericIntrinsic(dispatch, _method.Name, targs);
                }
                EmitIntrinsic(dispatch, _method.Name, _method.Signature);
                return true;
            },
            "synthesized from the call intrinsic's lowering");
        if (body is null)
            return null; // no lowering to synthesize from
        // A delegate-to-the-real-body lowering (e.g. String.CompareTo →
        // ReachStringMethod) names the method itself: as this method's own body it
        // would be infinite recursion. The signature line accounts for one
        // occurrence — any further one is a self-call. Fall back to the real IL.
        if (CountOf(body, _method.Emittable.CppName) > 1)
            return null;
        return body;
    }

    /// <summary>Synthesizes a standalone forwarder body for a bodyless P/Invoke method
    /// whose ADDRESS is taken (ldftn / delegate method group over a [DllImport], noted
    /// by <see cref="Compilation.NotePInvokeFtnTarget"/>): seeds the stack with the
    /// parameters and applies the same P/Invoke lowering a call site gets
    /// (<see cref="EmitPInvokeCall"/>), so invoking the delegate / function pointer
    /// marshals identically to a direct call. Null when the import's shape is not one
    /// the marshaller models — the emitter's compile arm then fails the transpile
    /// loudly naming the import (its address is planted in delegate state, so a
    /// missing body would otherwise surface as a bare C++ link error).</summary>
    internal string? CompilePInvokeWrapper()
    {
        if (_method.PInvoke is not { } pinv)
            return null;
        return SynthesizeWrapperBody(
            _method.DeclaringClass.CppStructName + "*",
            () =>
            {
                EmitPInvokeCall(_method, pinv);
                return true;
            },
            "synthesized P/Invoke forwarder");
    }

    private static int CountOf(string haystack, string needle)
    {
        int n = 0;
        for (int at = haystack.IndexOf(needle, StringComparison.Ordinal); at >= 0;
             at = haystack.IndexOf(needle, at + needle.Length, StringComparison.Ordinal))
            n++;
        return n;
    }

    private void BuildExceptionRegions(System.Reflection.Metadata.MethodBodyBlock bodyBlock)
    {
        var regions = bodyBlock.ExceptionRegions;
        int idx = 0;

        // Finally / fault regions.
        foreach (var r in regions)
        {
            if (r.Kind == ExceptionRegionKind.Finally)
            {
                var fr = new FinallyRegion(idx, r.TryOffset, r.TryOffset + r.TryLength,
                    r.HandlerOffset, r.HandlerOffset + r.HandlerLength);
                _finallyRegions.Add(fr);
                _finallyHandlerStarts[r.HandlerOffset] = fr;
                _tryOpens.Add((r.TryOffset, r.TryOffset + r.TryLength));
                _decls.Add(("std::exception_ptr", $"__eptr{idx}"));
                _decls.Add(("int32_t", $"__leave{idx}"));
                _declared.Add($"__eptr{idx}");
                _declared.Add($"__leave{idx}");
                idx++;
            }
            else if (r.Kind == ExceptionRegionKind.Fault)
            {
                var fr = new FaultRegion(idx, r.TryOffset, r.TryOffset + r.TryLength,
                    r.HandlerOffset, r.HandlerOffset + r.HandlerLength);
                _faultRegions.Add(fr);
                _faultHandlerStarts[r.HandlerOffset] = fr;
                _tryOpens.Add((r.TryOffset, r.TryOffset + r.TryLength));
                _decls.Add(("std::exception_ptr", $"__feptr{idx}"));
                _declared.Add($"__feptr{idx}");
                idx++;
            }
            else if (r.Kind != ExceptionRegionKind.Catch && r.Kind != ExceptionRegionKind.Filter)
            {
                // Catch and Filter handlers are built in the dedicated loops below.
                throw new NotSupportedException(
                    $"{_method.DeclaringClass.FullName}.{_method.Name}: {r.Kind} regions are not supported yet");
            }
        }

        // Catch regions grouped by their shared try range (multiple clauses).
        foreach (var r in regions.Where(r => r.Kind == ExceptionRegionKind.Catch))
        {
            string? ti = null;
            if (!r.CatchType.IsNil)
            {
                var catchType = ResolveCastTarget(System.Reflection.Metadata.Ecma335.MetadataTokens.GetToken(r.CatchType));
                // catch (Exception)/(object) catches every managed exception. The
                // runtime exception hierarchy is flat (each trap type has no base
                // chain), and a transpiled corelib emits its own `ti_System_Exception`
                // distinct from the runtime `dn2cpp_exception_type`, so an isinst
                // against the System.Exception base never matches a derived exception.
                // Treat the root type as catch-all (null ti) instead — this is what
                // the compiler-generated async builder `catch (Exception)` relies on,
                // so an exception faults the awaiting task rather than escaping the
                // scheduler. A bare C# `catch {}` compiles to `catch (object)`, whose
                // System.Object token decodes to the PRIMITIVE Object TypeDesc (not a
                // Class/External) — it must be catch-all too, or the emitted
                // `dn2cpp_isinst(ex, &dn2cpp_object_type)` never matches an exception
                // ti and the clause silently rethrows (the managed-vs-native
                // self-host divergence).
                bool catchAll = catchType is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Object }
                    || (catchType is { Kind: TypeKind.Class }
                        && CoreIntrinsics.IsIntrinsicType(catchType.Class!.FullName))
                    || catchType is { Kind: TypeKind.Class, Class.FullName: "System.Exception" }
                    || catchType is { Kind: TypeKind.External, ExternalName: "System.Exception" or "System.Object" };
                ti = catchAll ? null : TypeInfoExpr(catchType);
            }
            var group = _catchGroups.FirstOrDefault(g => g.TryStart == r.TryOffset && g.TryEnd == r.TryOffset + r.TryLength);
            if (group is null)
            {
                group = new CatchGroup { Index = idx++, TryStart = r.TryOffset, TryEnd = r.TryOffset + r.TryLength };
                _catchGroups.Add(group);
                _tryOpens.Add((group.TryStart, group.TryEnd));
            }
            group.Clauses.Add((r.HandlerOffset, r.HandlerOffset + r.HandlerLength, ti, -1));
        }

        // Filter regions (`catch... when`): another handler on the same try range.
        // The match is decided by running the filter IL block (which ends in
        // `endfilter` yielding 0/1), not an isinst — so Ti is null and FilterStart
        // points at the filter block. Clauses dispatch in IL order.
        foreach (var r in regions.Where(r => r.Kind == ExceptionRegionKind.Filter))
        {
            var group = _catchGroups.FirstOrDefault(g => g.TryStart == r.TryOffset && g.TryEnd == r.TryOffset + r.TryLength);
            if (group is null)
            {
                group = new CatchGroup { Index = idx++, TryStart = r.TryOffset, TryEnd = r.TryOffset + r.TryLength };
                _catchGroups.Add(group);
                _tryOpens.Add((group.TryStart, group.TryEnd));
            }
            group.Clauses.Add((r.HandlerOffset, r.HandlerOffset + r.HandlerLength, null, r.FilterOffset));
        }

        foreach (var g in _catchGroups)
        {
            g.Clauses.Sort((a, b) => a.Start.CompareTo(b.Start));
            g.End = g.Clauses[^1].End;
            _catchDispatchAt[g.TryEnd] = g;
            for (int i = 0; i < g.Clauses.Count; i++)
            {
                int s = g.Clauses[i].Start;
                if (!_clauseLabelAt.TryGetValue(s, out var list))
                    _clauseLabelAt[s] = list = new();
                list.Add((g, i));
                if (g.Clauses[i].FilterStart >= 0)
                    _filterBlockAt[g.Clauses[i].FilterStart] = (g, i);
            }
        }

        // The offset-keyed views Compile's per-instruction loop probes. Built last,
        // after every group's End is known. Each list's order is the nesting order
        // the emitted braces close/open in: GroupBy keeps the source list's order
        // within a group and OrderByDescending is a stable sort, so ties keep it too.
        foreach (var byEnd in _catchGroups.GroupBy(g => g.End))
            _catchGroupEndsAt[byEnd.Key] = byEnd.OrderByDescending(g => g.TryStart).ToList();
        foreach (var byStart in _tryOpens.GroupBy(t => t.TryStart))
            _tryOpensAt[byStart.Key] = byStart.OrderByDescending(t => t.TryEnd).ToList();
    }

    /// <summary>Emits the ordered handler dispatch for a catch group starting at
    /// clause <paramref name="from"/>: each typed clause is an isinst test that
    /// jumps to its handler; a filter clause hands off to its filter block (which
    /// resumes the chain at its <c>endfilter</c>). If the run reaches the end with
    /// no filter to hand off to, the exception is re-raised (continue search).</summary>
    private void EmitDispatchFrom(CatchGroup g, int from)
    {
        for (int j = from; j < g.Clauses.Count; j++)
        {
            if (g.Clauses[j].FilterStart >= 0)
            {
                Emit($"goto F{g.Index}_{j};");
                return;
            }
            string cond = g.Clauses[j].Ti is { } ti
                ? $"dn2cpp_isinst(__ex{g.Index}.obj, {ti}) != nullptr"
                : "true";
            Emit($"if ({cond}) goto H{g.Index}_{j};");
        }
        Emit("throw;");
    }

    /// <summary><c>[HotPath(NoAlias = true)]</c>: pick the
    /// <c>Span&lt;T&gt;</c>/<c>ReadOnlySpan&lt;T&gt;</c> parameters whose element
    /// pointer the prologue hoists into a <c>__restrict</c> local. A span is a
    /// by-value struct, so its parameter cannot carry the qualifier the way an
    /// array or byref parameter does — the aliasing claim has to sit on a pointer,
    /// and the only pointer in sight is the <c>f__reference</c> field the indexer
    /// re-reads at every access. Hoisting it once gives the qualifier somewhere to
    /// live and makes the base loop-invariant by construction.
    /// <para>The hoist is only correct while the parameter denotes the same span
    /// for the whole body, so a parameter the body RESEATS is excluded here:
    /// <c>starg</c> is the shape the C# compiler emits for <c>s = s.Slice(…)</c>.
    /// Reseating through a <c>ref Span&lt;T&gt;</c> callee is not visible without
    /// decoding every callee signature, and is part of the knob's declared
    /// contract (see <c>HotPathAttribute.NoAlias</c>).</para>
    /// <para>Nothing is emitted unless the body also sets
    /// <see cref="MethodInfo.SkipBoundsChecks"/>: the checked <c>get_Item</c> call
    /// is the only other reader of a span parameter, and it takes the span's
    /// address rather than its element pointer. NoAlias alone therefore leaves
    /// every entry unused, hence undeclared.</para></summary>
    private void SetupNoAliasSpanLocals(List<Instruction> insns)
    {
        if (!_method.NoAlias)
            return;
        var reseated = new HashSet<int>();
        foreach (var insn in insns)
            if (insn.OpCode is ILOpCode.Starg or ILOpCode.Starg_s)
                reseated.Add((int)insn.Operand);
        // _args is indexed by IL argument number (receiver first), which is what
        // ldarga/starg operands name.
        for (int i = 0; i < _args.Count; i++)
        {
            if (reseated.Contains(i)
                || _args[i].Type is not { Kind: TypeKind.Class, Class: { } sc }
                || _c.GenericDefFullName(sc) is not ("System.Span" or "System.ReadOnlySpan"))
                continue;
            _noAliasSpans.Add((i, _args[i].Name, $"__na{i}", CppTypes.StorageOf(sc.Context.TypeArgs[0])));
        }
    }

    /// <summary>The <c>__restrict</c> prologue local standing in for a popped span
    /// receiver, or null when this receiver is not a NoAlias-hoisted parameter.
    /// The match is on <see cref="StackEntry.ArgSlot"/>, the IL argument number
    /// <c>ldarga</c> recorded — not on the entry's C++ text, which by then is the
    /// anonymous temp <c>Push</c> materialized and names no parameter at all. A
    /// receiver that reached the call site any other way (a by-value span spilled
    /// to a temp, a span read out of a field or local) carries no slot and
    /// declines, keeping the ordinary <c>f__reference</c> read — whose base really
    /// is a different object.</summary>
    private string? NoAliasSpanBase(StackEntry e)
    {
        if (e is not { Kind: StackKind.Ptr, SlotAddr: true, ArgSlot: { } slot })
            return null;
        foreach (var (argSlot, _, localName, _) in _noAliasSpans)
        {
            if (argSlot != slot)
                continue;
            _noAliasSpansUsed.Add(slot);
            return localName;
        }
        return null;
    }

    private void BuildArgsAndLocals(System.Reflection.Metadata.MethodBodyBlock bodyBlock)
    {
        int i = 0;
        if (!_method.IsStatic)
        {
            // `this` is a managed pointer for value types, object ref otherwise.
            // A transpiled String interface impl receives the runtime string
            // (matching Signature), not the opaque t_System_String shell.
            string t = _method.DeclaringClass.FullName == "System.String"
                ? "Dn2CppString*"
                : _method.DeclaringClass.CppStructName + "*";
            _args.Add(($"a{i++}", t, _method.DeclaringClass.IsValueType ? StackKind.Ptr : StackKind.Ref, null));
        }
        foreach (var p in _method.Signature.ParameterTypes)
            _args.Add(($"a{i++}", CppTypes.Of(p), CppTypes.KindOf(p), p));

        if (!bodyBlock.LocalSignature.IsNil)
        {
            var sig = _reader.GetStandaloneSignature(bodyBlock.LocalSignature);
            var types = sig.DecodeLocalSignature(_c.SigProvider, _method.Context);
            for (int li = 0; li < types.Length; li++)
            {
                // A local may be typed by a reference class live only in a dead
                // branch (e.g. Dictionary's IEqualityComparer<string> /
                // RandomizedStringEqualityComparer slots in the value-type-key
                // path); ensure its layout is at least declared opaquely.
                NoteReferenceClass(types[li]);
                // A by-value struct local reserves real storage (`t_<Name> locN;`)
                // and may be used solely by address — its pointer handed to a
                // P/Invoke that fills it (Interop.NtDll.IO_STATUS_BLOCK, whose
                // address goes to NtCreateFile/NtQueryDirectoryFile) — so no
                // field-access or signature edge ever pulls its layout into the
                // emit closure. Force its full layout here; ComputeEmitted walks
                // its field-type closure (the nested LayoutKind.Explicit IO_STATUS
                // union). Intrinsic/primitive value types are filtered by
                // ComputeEmitted.Add (IntrinsicCppName) and enums emit separately.
                NoteLocalValueLayout(types[li]);
                _locals.Add(($"loc{li}", CppTypes.Of(types[li]), CppTypes.KindOf(types[li]), types[li]));
            }
        }
    }

    /// <summary>The C++ expression for a <c>static readonly string</c> constant declared on
    /// an INTRINSIC-mapped type, or null when the field is not one of them. Both static-field
    /// opcodes ask this — <c>ldsfld</c> pushes the expression, <c>ldsflda</c> materializes it
    /// into a temp and pushes the temp's address — so the member set has one spelling instead
    /// of one per opcode.
    ///
    /// <para><b>Why a fold is the only available answer.</b> An intrinsic-mapped type's
    /// members are emitted inline and its statics get NO storage, so the ordinary
    /// <c>sf_&lt;Type&gt;_&lt;Field&gt;</c> reference is a symbol nothing defines: the
    /// transpile succeeds and the C++ compile fails on an undeclared identifier. The values
    /// are not approximations — .NET documents <c>Boolean.TrueString</c> as "True",
    /// <c>FalseString</c> as "False" (culture-invariantly) and <c>String.Empty</c> as "",
    /// each exactly the constant its .cctor stores.</para>
    ///
    /// <para>Folding also drops the <c>EnsureCctorBefore</c> the read would otherwise emit
    /// for the intrinsic type's .cctor, which is right: that .cctor's whole observable effect
    /// is the constant now spelled at the site.</para></summary>
    private static string? IntrinsicStaticStringConstant(FieldInfo fld) =>
        (fld.DeclaringClass.FullName, fld.Name) switch
        {
            ("System.String", "Empty") => "dn2cpp_string_literal(u\"\", 0)",
            ("System.Boolean", "TrueString") => "dn2cpp_string_literal(u\"True\", 4)",
            ("System.Boolean", "FalseString") => "dn2cpp_string_literal(u\"False\", 5)",
            _ => null,
        };

    /// <summary>Records a reference (non-value) class inside a type — directly or
    /// as an array element — so its layout is emitted (opaquely if it is otherwise
    /// unreached). Skips intrinsic-mapped types, which the runtime provides.</summary>
    private void NoteReferenceClass(TypeDesc t)
    {
        switch (t.Kind)
        {
            case TypeKind.Class when t.Class is { IsValueType: false, IntrinsicCppName: null } cls:
                _c.NoteReferencedType(cls);
                break;
            case TypeKind.SZArray or TypeKind.MDArray or TypeKind.ByRef or TypeKind.Pointer:
                NoteReferenceClass(t.Element!);
                break;
        }
    }

    /// <summary>Records a by-value struct that appears — directly or as a
    /// pointer/byref/array element — as a method local OR AS A TEMP a lowering mints, so
    /// its full C++ layout is
    /// emitted even when it enters the emit set through no other edge (no reachable
    /// signature type, no by-field access): a struct a body uses only by address,
    /// its pointer handed to a P/Invoke that fills it (Interop.NtDll.IO_STATUS_BLOCK
    /// passed to NtCreateFile). Enums and intrinsic/primitive value types are left
    /// alone — enums emit separately and ComputeEmitted.Add drops IntrinsicCppName
    /// owners — so this only ever adds genuine, otherwise-missing struct layouts.
    ///
    /// <para><b>A CUT call site mints such a temp too, and that is the second caller.</b>
    /// The neutralize/trap arms in <c>EmitManagedCall</c> push a DEFAULT of the callee's
    /// return type, which <c>Push</c> spills into a `t_&lt;T&gt; tN;` temp — so a bounded
    /// method returning a STRUCT names a layout the cut just made sure nothing else pulls in.
    /// The failure is invisible: the transpile succeeds (the named-struct backstop records at
    /// FIELD accesses and signatures, not at a temp's declared type) and the C++ compile fails
    /// on an unknown type name. Every arm that pushes a defaulted return therefore asks both
    /// this and its reference-side sibling <see cref="NoteReferenceClass"/>.</para></summary>
    private void NoteLocalValueLayout(TypeDesc t)
    {
        switch (t.Kind)
        {
            case TypeKind.Class when t.Class is { IsValueType: true, IsEnum: false, IntrinsicCppName: null } cls:
                _c.NoteForceEmit(cls);
                break;
            case TypeKind.SZArray or TypeKind.MDArray or TypeKind.ByRef or TypeKind.Pointer:
                NoteLocalValueLayout(t.Element!);
                break;
        }
    }

    /// <summary>Pre-scan bookkeeping for <see cref="_backwardBranchTargets"/>:
    /// record <paramref name="target"/> when the branch/switch instruction naming
    /// it sits at or after it (a backward or self edge).</summary>
    private void NoteBranchSource(int target, int source)
    {
        if (target <= source)
            _backwardBranchTargets.Add(target);
    }

    // ---- stack helpers ----

    // Internal so call intrinsics (e.g. the Godot backend) can drive the
    // evaluation stack from another assembly.
    public void Push(StackKind kind, string cppType, string expr, TypeDesc? typeToken = null)
    {
        string t = NewTemp(cppType);
        Emit($"{t} = {expr};");
        _stack.Add(new StackEntry(t, kind, cppType, typeToken));
    }

    // The unspilled push: the caller either already materialized the value into a temp
    // of its own or is carrying metadata Push's four parameters cannot express.
    public void PushEntry(StackEntry entry) => _stack.Add(entry);

    /// <summary><c>ldc.i4 v</c> — Push, plus a note of which temp now holds the constant, so
    /// an intercept can read the LITERAL back (a ThrowHelper sink's ExceptionResource
    /// is an operand, and Push has already spilled it into a temp by the time the intercept
    /// pops it). A four-slot ring, not a map: these operands sit immediately before their
    /// call, and a per-method map would cost an entry per ldc in every body compiled.</summary>
    private void PushI4Const(int v)
    {
        Push(StackKind.I4, "int32_t", v.ToString());
        _constTemps[_constTempCursor] = (Top.Expr, v);
        _constTempCursor = (_constTempCursor + 1) % _constTemps.Length;
    }

    private readonly (string Temp, int Value)[] _constTemps = new (string, int)[4];
    private int _constTempCursor;

    /// <summary>The int32 constant <paramref name="e"/> was pushed with, when it is still in
    /// the ring. Best-effort by construction: a miss reads as "not a constant", and every
    /// caller has a fallback.</summary>
    internal int? ConstIntOf(StackEntry e)
    {
        foreach (var (temp, value) in _constTemps)
            if (temp == e.Expr)
                return value;
        return null;
    }

    public int StackDepth => _stack.Count;

    public StackEntry Top
    {
        get => _stack[^1];
        set => _stack[^1] = value;
    }

    public StackEntry Peek(int fromTop) => _stack[^fromTop];

    public StackEntry Pop()
    {
        if (_stack.Count == 0)
            throw new InvalidOperationException($"{_method.Name}: IL stack underflow");
        var e = _stack[^1];
        _stack.RemoveAt(_stack.Count - 1);
        return e;
    }

    public string NewTemp(string cppType)
    {
        string name = $"t{_tempCounter++}";
        _decls.Add((cppType, name));
        return name;
    }

    // Hoisted, unique scalar/array locals for call intrinsics in other assemblies
    // (e.g. the Godot ptrcall path). Declaring at function top — like every other
    // temp here — keeps them out of inner brace scopes and safe from goto bypass.
    internal string NewLocal(string cppType) => NewTemp(cppType);

    public string NewLocalArray(string elemCppType, int count)
    {
        string name = $"t{_tempCounter++}";
        _decls.Add((elemCppType, $"{name}[{count}]"));
        return name;
    }

    public void Emit(string line) => _body.AppendLine("    " + line);

    /// <summary>Run <paramref name="cls"/>'s static constructor on first use of one of
    /// its static fields. .NET initializes a type lazily — its .cctor runs before the
    /// first read of any of its statics — so a cctor that reads a static another type's
    /// .cctor sets is guaranteed to see the set value. dn2cpp instead runs every .cctor
    /// eagerly at startup in reach order, which breaks that ordering when the producing
    /// cctor is ordered after the consuming one (the consumer then reads a null
    /// singleton). Emitting the target's idempotent <c>__ensure</c> wrapper at
    /// the static-field access reproduces .NET's first-use ordering for the dependency.
    /// Only the cctor's *own* body skips the guard (it is the initialization); every
    /// other method guards even its own class's statics — a static helper can be the
    /// type's first-use entry when called out of another type's cctor during the eager
    /// startup pass, before its own class's cctor was reached. Cross-type
    /// cycles are broken inside the wrapper's slow path, which recognizes a re-entrant
    /// access from the thread already running that cctor and returns to the partial
    /// state (.NET's recursive-initialization semantics). An initializer that THREW is
    /// not a cycle and not a completion: the guard re-raises the remembered failure at
    /// every later use, so this call site never falls through to unassigned statics.</summary>
    private void EnsureCctorBefore(ClassInfo? cls)
    {
        if (cls is null || cls.IntrinsicCppName is not null)
            return; // intrinsic types have no emitted cctor / no real static storage
        if (ReferenceEquals(cls, _method.DeclaringClass) && _method.Name == ".cctor")
            return; // the initializing cctor's own statics: currently initializing
        var cc = cls.StaticCctor;
        if (cc is null)
            return; // no static constructor — nothing to order
        _c.NoteCctorEnsure(cc);
        Emit($"{cc.CppName}__ensure();");
    }

    /// <summary>A shared body's address expression for a placeholder-bearing
    /// class's static field: the per-instantiation storage address out of an
    /// rgctx slot keyed on the field token, preceded by the instantiation's
    /// first-use cctor guard (the <see cref="EnsureCctorBefore"/> ordering,
    /// routed through a CctorEnsureFn slot on the same key). Thread-statics
    /// have no stable address to table, so they taint.</summary>
    private string RgctxStaticAddr(FieldInfo fld, int token)
    {
        if (fld.IsThreadStatic)
            ThrowSharedTaint("statics", $"{fld.DeclaringClass.FullName}.{fld.Name} (thread-static)");
        var cc = fld.DeclaringClass.StaticCctor;
        if (cc is not null
            && !(ReferenceEquals(fld.DeclaringClass, _method.DeclaringClass) && _method.Name == ".cctor"))
            Emit($"((void(*)()){RgctxSlotAccess(RgctxSlotKind.CctorEnsureFn, token, "statics", fld.Name)})();");
        string addr = RgctxSlotAccess(RgctxSlotKind.StaticFieldAddr, token, "statics", fld.Name);
        // The rgctx slot is a void* to the real sf_ storage, so the cast must name the
        // slot's own width (CppTypes.FieldOf), not the int32-promoted stack model —
        // a sub-word static is a uint8_t/int16_t/char16_t object.
        return $"(({CppTypes.FieldOf(fld)}*){addr})";
    }

    /// <summary>Spill the current stack into canonical per-depth variables and
    /// record/verify the entry stack of the given target block.</summary>
    private void BranchTo(int targetOffset, bool emitGoto, string? condition = null)
    {
        var canonical = new List<StackEntry>();
        for (int i = 0; i < _stack.Count; i++)
        {
            var e = _stack[i];
            // Struct/Ptr entries need exact-type canonical slots; other kinds
            // share one slot per (depth, kind).
            bool typed = e.Kind is StackKind.Struct or StackKind.Ptr;
            string type = typed ? e.CppType : CppTypes.DefaultForKind(e.Kind);
            string name = typed ? $"bs{i}_{CppNaming.Sanitize(type)}" : $"bs{i}_{e.Kind}";
            if (_declared.Add(name))
                _decls.Add((type, name));
            if (e.Expr != name)
                Emit($"{name} = {Cast(e, type)};");
            // Carry the operand's declared CLR type across the spill so a use after a
            // branch (e.g. `arr.Where(lambda)` — the cached-lambda null check always
            // spills the array first) can still recognise it as `T[]`. The
            // canonical slot's C++ type may be widened, but StaticType still describes
            // the value.
            canonical.Add(new StackEntry(name, e.Kind, type, StaticType: e.StaticType));
        }

        if (_entryStacks.TryGetValue(targetOffset, out var existing))
        {
            if (existing.Count != canonical.Count)
            {
                string Fmt(IEnumerable<StackEntry> es) => string.Join(", ", es.Select(e => $"{e.Kind}:{e.CppType}"));
                throw new NotSupportedException(
                    $"{_method.DeclaringClass.FullName}.{_method.Name}: inconsistent stack at IL_{targetOffset:X4} ([{Fmt(existing)}] vs [{Fmt(canonical)}])");
            }
            for (int i = 0; i < existing.Count; i++)
            {
                var ex = existing[i];
                var can = canonical[i];
                if (ex.Kind == can.Kind && ex.CppType == can.CppType) continue;
                // A `cond ? ptr : null` ternary joins a Ptr branch (byte*/void*) with a
                // native-int branch (Roslyn emits `null` in an unmanaged pointer context as
                // `ldc.i4.0; conv.u` -> I8). Reconcile by writing this branch's value into the
                // existing slot with a cast — the earlier branch already wrote its own value
                // there. A ptr <-> intptr round-trip is bit-exact (pointer stored as intptr_t
                // and cast back at the consumer), so no value is lost when Ptr flows through
                // an I8 slot; the target reads existing's slot and later casts to whatever
                // the use site demands. The current branch's own-kind slot write above stays
                // dead but harmless. IsPointerSpellingMerge reconciles the same way, for
                // the same reason, over the other pointer-shaped disagreements — except
                // where both kinds are slot-typed and landed on the SAME slot, which the
                // spill above has already written.
                if (IsPtrIntMerge(ex.Kind, can.Kind) || IsPointerSpellingMerge(ex, can))
                {
                    if (ex.Expr != can.Expr)
                        Emit($"{ex.Expr} = {Cast(_stack[i], ex.CppType)};");
                    continue;
                }
                string Fmt(IEnumerable<StackEntry> es) => string.Join(", ", es.Select(e => $"{e.Kind}:{e.CppType}"));
                throw new NotSupportedException(
                    $"{_method.DeclaringClass.FullName}.{_method.Name}: inconsistent stack at IL_{targetOffset:X4} ([{Fmt(existing)}] vs [{Fmt(canonical)}])");
            }
            // Reconcile StaticType across join predecessors: when two paths spill
            // different declared types into the same slot, drop it to null so a
            // downstream coercion stays conservative (no array wrapping on an
            // ambiguous merge) — the block reads this stored entry stack.
            for (int i = 0; i < existing.Count; i++)
                if (!Equals(existing[i].StaticType, canonical[i].StaticType))
                    existing[i] = existing[i] with { StaticType = null };
            // Continue below with the recorded entry stack (existing wins), so the
            // fallthrough path stays consistent with what the target block reads.
            canonical = existing;
        }
        else
        {
            _entryStacks[targetOffset] = canonical;
        }

        if (emitGoto)
        {
            Emit(condition is null
                ? $"goto IL_{targetOffset:X4};"
                : $"if ({condition}) goto IL_{targetOffset:X4};");
        }

        // Continue (fallthrough / not-taken path) with the canonical names.
        _stack = new List<StackEntry>(canonical);
    }

    internal static string Cast(StackEntry e, string targetType)
    {
        if (e.CppType == targetType)
            return e.Expr;
        // CultureInfo/NumberFormatInfo/TextInfo/IFormatProvider lower to the
        // HEADERLESS `const Dn2CppNumberFormatInfo*`, so a conversion to/from
        // the managed object representation cannot be a plain pointer cast: an
        // object-generic consumer (ToString dispatch, GetType, equality, an
        // object[] element) reads a type header the struct does not have — it
        // misreads the first field (a string pointer) as the TypeInfo and jumps
        // through literal-pool data. The escape allocates the interned runtime
        // wrapper instead, and the reverse conversion unwraps it (tolerantly: a
        // raw pointer that flowed through an erased context passes through
        // unchanged). The known-null constant keeps the plain cast: null
        // converts to null either way, and the folds keyed on KnownNull must
        // keep seeing the literal.
        if (!e.KnownNull)
        {
            if (targetType == "Dn2CppObject*" && IsNfiCppType(e.CppType))
                return NfiWrapExpr(e);
            if (IsNfiCppType(targetType) && e.CppType == "Dn2CppObject*")
                return $"(({targetType})dn2cpp_nfi_unwrap({e.Expr}))";
            if (targetType == "Dn2CppObject*" && IsAsmCppType(e.CppType))
                return AsmWrapExpr(e);
            if (IsAsmCppType(targetType) && e.CppType == "Dn2CppObject*")
                return $"(({targetType})dn2cpp_asm_unwrap({e.Expr}))";
            if (targetType == "Dn2CppObject*" && UnwrappableHeaderlessCpp(e.CppType) is { } opaque)
                return $"dn2cpp_headerless_escape(\"{opaque}\")";
        }
        return $"(({targetType}){e.Expr})";
    }

    /// <summary>The managed type name of a headerless intrinsic representation that
    /// CANNOT be given a managed wrapper, or null. One remains:
    /// <c>SearchValues&lt;T&gt;</c>, the raw membership set. It fails the same misread as
    /// the NFI trio at an <c>object</c> boundary — silently, with
    /// <c>is SearchValues&lt;char&gt;</c> answering false and ToString answering
    /// "System.Object" — but it cannot take the wrapper treatment, because the identity
    /// .NET reports is a PRIVATE per-shape subclass (RangeCharSearchValues&lt;T&gt;,
    /// ProbabilisticCharSearchValues, …) this model cannot even enumerate. Answering
    /// there is not "less wrong" than throwing, it is differently wrong with no
    /// diagnostic, so the escape traps loudly instead (<c>dn2cpp_headerless_escape</c>).
    /// The REVERSE direction stays a plain cast: an object-typed value flowing back into
    /// this position is a raw handle that never escaped, exactly as before.
    /// <para>Assembly/Module (<c>const char*</c>) do NOT trap here: their private
    /// identity is a FIXED pair (RuntimeAssembly/RuntimeModule), which is exactly what a
    /// hand-written runtime type-info can carry, so they take the interned-wrapper arms
    /// above (<see cref="AsmWrapExpr"/>) instead.</para></summary>
    private static string? UnwrappableHeaderlessCpp(string cpp) => cpp switch
    {
        "Dn2CppSearchValues*" => "System.Buffers.SearchValues<T>",
        _ => null,
    };

    /// <summary>True when a C++ type string is the headerless intrinsic
    /// representation of CultureInfo/NumberFormatInfo/TextInfo/IFormatProvider
    /// (see the NFI arms in <see cref="Cast"/>).</summary>
    internal static bool IsNfiCppType(string? t) =>
        t is "const Dn2CppNumberFormatInfo*" or "Dn2CppNumberFormatInfo*";

    /// <summary>The DN2CPP_NFI_KIND_* macro the escape site's static managed
    /// type resolves to; PROVIDER when the static type is IFormatProvider or
    /// was lost on a merge — the runtime then recovers the identity from the
    /// instance's isNfi tag (dn2cpp_nfi_wrap).</summary>
    internal static string NfiKindOf(TypeDesc? t)
    {
        string? n = t?.Kind switch
        {
            TypeKind.Class => t.Class!.FullName,
            TypeKind.External => t.ExternalName,
            _ => null,
        };
        return n switch
        {
            "System.Globalization.CultureInfo" => "DN2CPP_NFI_KIND_CULTURE",
            "System.Globalization.NumberFormatInfo" => "DN2CPP_NFI_KIND_NFI",
            "System.Globalization.TextInfo" => "DN2CPP_NFI_KIND_TEXTINFO",
            _ => "DN2CPP_NFI_KIND_PROVIDER",
        };
    }

    /// <summary>The interned-wrapper allocation for an NFI-typed value escaping
    /// into an `object` context (see <see cref="Cast"/>).</summary>
    internal static string NfiWrapExpr(StackEntry e) =>
        $"dn2cpp_nfi_wrap({e.Expr}, {NfiKindOf(e.StaticType)})";

    /// <summary>True when a C++ type string is the headerless intrinsic
    /// representation of Assembly/Module — the defining assembly's simple-name
    /// <c>const char*</c> (see the asm arms in <see cref="Cast"/>).
    /// <c>const char*</c> is unambiguous on the eval stack: every producer of that
    /// spelling is an Assembly/Module reflection intrinsic
    /// (<c>MethodCompiler.EmitIntrinsic.Reflection.cs</c>); the interop decoders
    /// build their <c>const char*</c> as a local temp and push a
    /// <c>Dn2CppString*</c>.</summary>
    internal static bool IsAsmCppType(string? t) => t is "const char*";

    /// <summary>The DN2CPP_ASM_KIND_* macro the escape site's static managed type
    /// resolves to; ASSEMBLY when the static type was lost — the same default the
    /// Object::ToString arm on a <c>const char*</c> receiver uses (Assembly handles
    /// are the overwhelmingly common case; a Module handle's producers all stamp
    /// the Module static type on the pushed entry).</summary>
    internal static string AsmKindOf(TypeDesc? t) => t is
            { Kind: TypeKind.Class, Class.FullName: "System.Reflection.Module" }
            or { Kind: TypeKind.External, ExternalName: "System.Reflection.Module" }
        ? "DN2CPP_ASM_KIND_MODULE"
        : "DN2CPP_ASM_KIND_ASSEMBLY";

    /// <summary>The interned-wrapper allocation for an Assembly/Module handle
    /// escaping into an `object` context (see <see cref="Cast"/>).</summary>
    internal static string AsmWrapExpr(StackEntry e) =>
        $"dn2cpp_asm_wrap({e.Expr}, {AsmKindOf(e.StaticType)})";

    /// <summary>True when a C++ type string is a headerless intrinsic representation
    /// that has a managed WRAPPER (the NFI trio's <c>const Dn2CppNumberFormatInfo*</c>,
    /// Assembly/Module's <c>const char*</c>) — i.e. the spellings the erased-ABI
    /// boundaries convert instead of C-casting. SearchValues is deliberately not here:
    /// its escape traps (<see cref="UnwrappableHeaderlessCpp"/>), so an erased boundary
    /// keeps the plain cast and the trap stays at the object conversion that means
    /// it.</summary>
    internal static bool IsHeaderlessWrapCpp(string? t) => IsNfiCppType(t) || IsAsmCppType(t);

    /// <summary>The wrap expression carrying <paramref name="expr"/> (spelled
    /// <paramref name="realCpp"/>, its headerless C++ type) into a
    /// <c>Dn2CppObject*</c> position — the emitter-side twin of the <see cref="Cast"/>
    /// wrap arms, for mouths that hold an expression string rather than a
    /// <see cref="StackEntry"/>. <paramref name="realType"/> decides the wrapper's
    /// identity kind. Only valid when <see cref="IsHeaderlessWrapCpp"/> holds for
    /// <paramref name="realCpp"/>.</summary>
    internal static string HeaderlessWrapExpr(string expr, string realCpp, TypeDesc? realType) =>
        IsNfiCppType(realCpp)
            ? $"dn2cpp_nfi_wrap({expr}, {NfiKindOf(realType)})"
            : $"dn2cpp_asm_wrap({expr}, {AsmKindOf(realType)})";

    /// <summary>The ONE conversion an expression takes when its spelling crosses an
    /// erased-signature boundary — a real signature's position against a shared
    /// canonical implementation's erased spelling, in either direction. It is a thin
    /// veneer over <see cref="Cast"/>, so the wrap/unwrap/trap verdict and its
    /// spelling live in exactly one place: a headerless value entering a
    /// <c>Dn2CppObject*</c> position wraps, an object-typed value entering a
    /// headerless position unwraps tolerantly, everything else keeps the C cast the
    /// erased boundary always had. Shared by the direct-call binder
    /// (<see cref="DirectCall"/>) and the per-instantiation rgctx forwarder
    /// (<c>CppEmitter.RgctxForwarderBody</c>) — the two mouths that used to each
    /// spell their own verdict — a configuration whose drift was a SIGSEGV.</summary>
    internal static string ErasedBoundaryCast(string expr, string fromCpp, TypeDesc? staticType, string toCpp) =>
        Cast(new StackEntry(expr, StackKind.Ref, fromCpp, StaticType: staticType), toCpp);

    /// <summary>The tolerant unwrap expression recovering a headerless spelling
    /// <paramref name="realCpp"/> from an object-typed <paramref name="expr"/> — the
    /// emitter-side twin of the <see cref="Cast"/> unwrap arms. Only valid when
    /// <see cref="IsHeaderlessWrapCpp"/> holds for <paramref name="realCpp"/>.</summary>
    internal static string HeaderlessUnwrapExpr(string expr, string realCpp) =>
        IsNfiCppType(realCpp)
            ? $"(({realCpp})dn2cpp_nfi_unwrap((Dn2CppObject*)({expr})))"
            : $"(({realCpp})dn2cpp_asm_unwrap((Dn2CppObject*)({expr})))";

    // Only the pointer-width int qualifies: reconciling a Ptr into an I4 slot
    // would truncate the pointer to 32 bits on a 64-bit target.
    private static bool IsPtrIntMerge(StackKind a, StackKind b) =>
        (a == StackKind.Ptr && b == StackKind.I8)
        || (b == StackKind.Ptr && a == StackKind.I8);

    /// <summary>Whether a Struct/Ref or Struct/Ptr join is two spellings of ONE pointer
    /// value — what `cond ? tok.Register(cb) : default` meets. An intrinsic
    /// value type modeled as a raw pointer is Struct-kinded by
    /// <see cref="CppTypes.KindOf"/>, which is what the <c>initobj</c>'d local reads as,
    /// while the other arm carries the same value under its producer's kind: Ref where a
    /// runtime helper hands the handle back (CancellationTokenRegistration), Ptr where
    /// KindOf's own External arm answers (RuntimeTypeHandle). Struct with a pointer C++
    /// type IS that case and nothing else — a non-intrinsic value type spells a by-value
    /// <c>t_*</c> aggregate — and ECMA-335 never joins a genuine object or byref with a
    /// genuine struct. Every slot involved holds a pointer, so the round trip through
    /// either is bit-exact, as in <see cref="IsPtrIntMerge"/>.</summary>
    private static bool IsPointerSpellingMerge(StackEntry a, StackEntry b) =>
        IsPointerStruct(a) ? b.Kind is StackKind.Ref or StackKind.Ptr
        : IsPointerStruct(b) && a.Kind is StackKind.Ref or StackKind.Ptr;

    private static bool IsPointerStruct(StackEntry e) =>
        e.Kind == StackKind.Struct && e.CppType.EndsWith('*');

    /// <summary>Coerce a stack value to a target parameter/slot/return type. Identical to
    /// <see cref="Cast"/> except a raw <c>T[]</c> flowing into a closed SZArray collection-
    /// interface position (<c>IEnumerable&lt;T&gt;</c>/<c>ICollection&lt;T&gt;</c>/
    /// <c>IList&lt;T&gt;</c>/<c>IReadOnly{List,Collection}&lt;T&gt;</c>) — which IL allows
    /// without a <c>castclass</c> (array interface covariance) — additionally notes the
    /// array element so its precise <c>ti_arr_&lt;T&gt;</c> handle carries the real SZArray
    /// interface-dispatch map. The value passed is the <em>raw array</em>: the
    /// consumer's dispatch resolves on the array itself, so <c>arr.GetType</c> stays
    /// exact and no compile-time <c>SZArrayEnumerable&lt;T&gt;</c> wrap is needed (this
    /// replaces the boundary wrap, whose wrapped value had the wrong runtime type).</summary>
    /// <summary>Shared-body candidate: a poisoned runtime-type value (typeof
    /// over a placeholder-bearing type — a null handle with the static token
    /// riding along) escaping fold-land into a local/arg/field/array/return/
    /// call argument would surface as a null Type at runtime; taint instead.
    /// The whitelisted static folds never store the handle itself.</summary>
    private void TaintPoisonedTypeEscape(StackEntry e)
    {
        if (SharedTrial && e.TypeToken is { } tk
            && e.CppType is "Dn2CppType*" or "const Dn2CppTypeInfo*"
            && Compilation.ContainsCanonPlaceholder(tk))
            ThrowSharedTaint("type-identity", $"typeof({tk}) escapes");
    }

    private string CoerceTo(StackEntry e, TypeDesc? targetType, string targetCppType)
    {
        TaintPoisonedTypeEscape(e);
        if (e.StaticType is { Kind: TypeKind.SZArray, Element: { } elem }
            && targetType is { Kind: TypeKind.Class, Class: { IsInterface: true } ic }
            && _c.GenericDefFullName(ic) is "System.Collections.Generic.IEnumerable"
                or "System.Collections.Generic.IReadOnlyList"
                or "System.Collections.Generic.IReadOnlyCollection"
                or "System.Collections.Generic.ICollection"
                or "System.Collections.Generic.IList"
            && ic.Context.TypeArgs.Length == 1)
            _c.NoteArrayEnumerableElement(elem);
        // The same boundary for the NON-generic collection trio (IEnumerable/ICollection/
        // IList): a raw array flows into an IList / ICollection / IEnumerable slot without a
        // castclass (it statically implements them), so `((IList)arr).Count` / indexer /
        // GetEnumerator has no explicit-cast site to wire the map — this is that site. The
        // type TEST is map-independent (DN2CPP_TF_ARRAY_ITF); this keeps the
        // matching CALL sound whenever the array is statically visible. Noting the element
        // wires the array's full SZArray map, whose non-generic trio (BuildArrayItfDispatches)
        // the callvirt then dispatches through. A dynamically-typed (object) operand has no
        // such boundary, and its call is answered without one: a reference-element array by
        // the runtime's shared fallback table (Compilation.WireObjectArrayFallbackMap), a
        // value-element array by the per-element map every noted value element now gets
        // eagerly (the value loop in Compilation.ExpandArrayEnumerableMaps).
        if (e.StaticType is { Kind: TypeKind.SZArray, Element: { } nelem }
            && targetType is { Kind: TypeKind.Class, Class: { IsInterface: true } nic }
            && nic.FullName is "System.Collections.IEnumerable"
                or "System.Collections.ICollection"
                or "System.Collections.IList")
            _c.NoteArrayEnumerableElement(nelem);
        // Same boundary for a string: IL lets it flow into IEnumerable<char>/
        // IComparable/… positions (String statically implements them) without a
        // castclass — LINQ over a string is exactly this shape, including the
        // interface-callvirt receiver slot PopArgs coerces. Wire String's runtime
        // dispatch map so the consumer's dispatch resolves on the string itself.
        if (e.StaticType is { IsString: true }
            && targetType is { Kind: TypeKind.Class, Class: { IsInterface: true } sic }
            && _c.IsStringDispatchInterface(sic))
            _c.NoteStringInterfaces();
        // Same boundary for a boxed enum: IL lets an enum (or System.Enum-typed) value
        // flow into an IComparable/IFormattable/IConvertible/ISpanFormattable position
        // without a castclass — every enum statically implements them via System.Enum —
        // and the interface-callvirt receiver slot PopArgs coerces is exactly the
        // ((IConvertible)e).ToInt32(null) shape. Wire the shared System.Enum dispatch
        // map (installed onto dn2cpp_enum_type; the resolve walk finds it through the
        // boxed enum's base chain) so the consumer's dispatch resolves on the box.
        if (e.StaticType is { Kind: TypeKind.Class, Class: { } eenc }
            && (eenc.IsEnum || eenc.FullName == "System.Enum")
            && targetType is { Kind: TypeKind.Class, Class: { IsInterface: true } eic }
            && Compilation.IsEnumDispatchInterface(eic))
            _c.NoteEnumInterfaces();
        return Cast(e, targetCppType);
    }

    // ---- translation ----

    private void Translate(Instruction insn)
    {
        var op = insn.OpCode;
        switch (op)
        {
            case ILOpCode.Nop:
                break;

            case ILOpCode.Ldc_i4_m1: case ILOpCode.Ldc_i4_0: case ILOpCode.Ldc_i4_1:
            case ILOpCode.Ldc_i4_2: case ILOpCode.Ldc_i4_3: case ILOpCode.Ldc_i4_4:
            case ILOpCode.Ldc_i4_5: case ILOpCode.Ldc_i4_6: case ILOpCode.Ldc_i4_7:
            case ILOpCode.Ldc_i4_8:
                PushI4Const((int)op - (int)ILOpCode.Ldc_i4_0);
                break;
            case ILOpCode.Ldc_i4_s:
            case ILOpCode.Ldc_i4:
                PushI4Const((int)insn.Operand);
                break;
            case ILOpCode.Ldc_i8:
                Push(StackKind.I8, "int64_t", $"INT64_C({insn.Operand})");
                break;
            case ILOpCode.Ldc_r4:
            case ILOpCode.Ldc_r8:
            {
                // Shared with the custom-attribute float-argument path (CppEmitter):
                // maps the special values to C++ tokens and forces a floating literal
                // on an integral value. See CppTypes.FloatLiteral.
                Push(StackKind.R8, "double", CppTypes.FloatLiteral(insn.FloatOperand));
                break;
            }
            case ILOpCode.Ldnull:
                Push(StackKind.Ref, "Dn2CppObject*", "nullptr");
                // The null constant, recorded: an optional reference argument the C#
                // compiler omitted arrives as exactly this, and a lowering that would
                // otherwise have to test it at run time (the span scans' trailing
                // IEqualityComparer<T>) can settle it here instead.
                _stack[^1] = _stack[^1] with { KnownNull = true };
                break;
            case ILOpCode.Ldstr:
            {
                string value = _reader.GetUserString(SRME.UserStringHandle(insn.Token));
                Push(StackKind.Ref, "Dn2CppString*", _literals.GetOrAdd(value));
                // Carry the literal value so an intrinsic taking a compile-time string
                // (e.g. Marshal.OffsetOf's field name) can recover it — Push spilled the
                // interned symbol into a temp, so the StackEntry.Expr is no longer str_N.
                _stack[^1] = _stack[^1] with { StrLiteral = value };
                break;
            }

            case ILOpCode.Ldarg_0: case ILOpCode.Ldarg_1: case ILOpCode.Ldarg_2: case ILOpCode.Ldarg_3:
                PushVar(_args[(int)op - (int)ILOpCode.Ldarg_0]);
                break;
            case ILOpCode.Ldarg_s: case ILOpCode.Ldarg:
                PushVar(_args[(int)insn.Operand]);
                break;
            case ILOpCode.Starg_s: case ILOpCode.Starg:
            {
                var dst = _args[(int)insn.Operand];
                Emit($"{dst.Name} = {CoerceTo(Pop(), dst.Type, dst.CppType)};");
                break;
            }
            case ILOpCode.Ldloc_0: case ILOpCode.Ldloc_1: case ILOpCode.Ldloc_2: case ILOpCode.Ldloc_3:
                PushVar(_locals[(int)op - (int)ILOpCode.Ldloc_0]);
                break;
            case ILOpCode.Ldloc_s: case ILOpCode.Ldloc:
                PushVar(_locals[(int)insn.Operand]);
                break;
            case ILOpCode.Ldloca_s: case ILOpCode.Ldloca:
            {
                var v = _locals[(int)insn.Operand];
                Push(StackKind.Ptr, v.CppType + "*", $"&{v.Name}");
                // Mark the entry as a direct local/arg slot address: the byref
                // sub-word out-arg fixup (NoteByRefSlotFixup) must only rewrite a
                // full-width promoted slot, never a reinterpreted buffer pointer
                // that happens to carry the same C++ pointer type.
                _stack[^1] = _stack[^1] with { SlotAddr = true };
                break;
            }
            case ILOpCode.Ldarga_s: case ILOpCode.Ldarga:
            {
                var v = _args[(int)insn.Operand];
                Push(StackKind.Ptr, v.CppType + "*", $"&{v.Name}");
                // ArgSlot as well as SlotAddr: which parameter this is the
                // address OF cannot be read back off the temp Push materialized,
                // and the NoAlias span route needs exactly that (see
                // NoAliasSpanBase). The address of a parameter slot is constant
                // for the whole body, so the mark stays true wherever the entry
                // is later restored from a recorded branch-entry stack.
                _stack[^1] = _stack[^1] with { SlotAddr = true, ArgSlot = (int)insn.Operand };
                break;
            }
            case ILOpCode.Stloc_0: case ILOpCode.Stloc_1: case ILOpCode.Stloc_2: case ILOpCode.Stloc_3:
            {
                var dst = _locals[(int)op - (int)ILOpCode.Stloc_0];
                Emit($"{dst.Name} = {CoerceTo(Pop(), dst.Type, dst.CppType)};");
                break;
            }
            case ILOpCode.Stloc_s: case ILOpCode.Stloc:
            {
                var dst = _locals[(int)insn.Operand];
                Emit($"{dst.Name} = {CoerceTo(Pop(), dst.Type, dst.CppType)};");
                break;
            }

            case ILOpCode.Dup:
            {
                var e = Pop();
                _stack.Add(e);
                _stack.Add(e);
                break;
            }
            case ILOpCode.Pop:
                Pop();
                break;

            case ILOpCode.Add: Binary("+"); break;
            case ILOpCode.Sub: Binary("-"); break;
            case ILOpCode.Mul: Binary("*"); break;
            // div/rem do NOT go through Binary: they are the two arithmetic
            // opcodes with a divisor value the hardware cannot answer for, and
            // they carry the zero / MinValue-by-minus-one guards.
            case ILOpCode.Div: BinaryDivRem(rem: false); break;
            case ILOpCode.Rem: BinaryDivRem(rem: true); break;
            case ILOpCode.Div_un: BinaryUnsignedDivRem(rem: false); break;
            case ILOpCode.Rem_un: BinaryUnsignedDivRem(rem: true); break;
            case ILOpCode.And: Binary("&"); break;
            case ILOpCode.Or: Binary("|"); break;
            case ILOpCode.Xor: Binary("^"); break;
            case ILOpCode.Shl: Shift(right: false, arithmetic: false); break;
            case ILOpCode.Shr: Shift(right: true, arithmetic: true); break;
            case ILOpCode.Shr_un: Shift(right: true, arithmetic: false); break;
            case ILOpCode.Neg:
            {
                var a = Pop();
                Push(a.Kind, a.CppType, $"-({a.Expr})");
                break;
            }
            case ILOpCode.Not:
            {
                var a = Pop();
                Push(a.Kind, a.CppType, $"~({a.Expr})");
                break;
            }

            case ILOpCode.Ceq: Compare("==", unsigned: false); break;
            case ILOpCode.Cgt: Compare(">", unsigned: false); break;
            case ILOpCode.Cgt_un: Compare(">", unsigned: true); break;
            case ILOpCode.Clt: Compare("<", unsigned: false); break;
            case ILOpCode.Clt_un: Compare("<", unsigned: true); break;

            case ILOpCode.Conv_i1: Convert("int32_t", StackKind.I4, "(int8_t)"); break;
            case ILOpCode.Conv_u1: Convert("int32_t", StackKind.I4, "(uint8_t)"); break;
            case ILOpCode.Conv_i2: Convert("int32_t", StackKind.I4, "(int16_t)"); break;
            case ILOpCode.Conv_u2: Convert("int32_t", StackKind.I4, "(uint16_t)"); break;
            case ILOpCode.Conv_i4: Convert("int32_t", StackKind.I4, "(int32_t)"); break;
            case ILOpCode.Conv_u4: Convert("int32_t", StackKind.I4, "(uint32_t)"); break;
            case ILOpCode.Conv_i8: ConvertWiden(unsigned: false); break;
            case ILOpCode.Conv_u8: ConvertWiden(unsigned: true); break;
            case ILOpCode.Conv_r4: Convert("double", StackKind.R8, "(float)"); break;
            case ILOpCode.Conv_r8: Convert("double", StackKind.R8, "(double)"); break;
            case ILOpCode.Conv_i: ConvertWiden(unsigned: false); break;
            case ILOpCode.Conv_u: ConvertWiden(unsigned: true); break;
            case ILOpCode.Conv_r_un:
                Convert("double", StackKind.R8, "(double)(uint64_t)");
                break;

            case ILOpCode.Add_ovf: CheckedBinary("add", signed: true); break;
            case ILOpCode.Add_ovf_un: CheckedBinary("add", signed: false); break;
            case ILOpCode.Sub_ovf: CheckedBinary("sub", signed: true); break;
            case ILOpCode.Sub_ovf_un: CheckedBinary("sub", signed: false); break;
            case ILOpCode.Mul_ovf: CheckedBinary("mul", signed: true); break;
            case ILOpCode.Mul_ovf_un: CheckedBinary("mul", signed: false); break;

            case ILOpCode.Conv_ovf_i1: case ILOpCode.Conv_ovf_i1_un: CheckedConv("int32_t", StackKind.I4, "int8_t", "-128", "127"); break;
            case ILOpCode.Conv_ovf_u1: case ILOpCode.Conv_ovf_u1_un: CheckedConv("int32_t", StackKind.I4, "uint8_t", "0", "255"); break;
            case ILOpCode.Conv_ovf_i2: case ILOpCode.Conv_ovf_i2_un: CheckedConv("int32_t", StackKind.I4, "int16_t", "-32768", "32767"); break;
            case ILOpCode.Conv_ovf_u2: case ILOpCode.Conv_ovf_u2_un: CheckedConv("int32_t", StackKind.I4, "uint16_t", "0", "65535"); break;
            // int32 min is emitted as (-2147483647 - 1), computed in `int`, NOT the
            // bare literal -2147483648: `-(2147483648)` types the sub-literal
            // 2147483648 as unsigned (it overflows a 32-bit `long` on MSVC/LLP64),
            // so the negation wraps to +2147483648 and the surrounding
            // (int64_t) widen carries that wrong positive bound into dn2cpp_conv_ovf
            // — making every checked (int) conversion trap. The subtraction form is
            // INT_MIN on every compiler.
            case ILOpCode.Conv_ovf_i4: case ILOpCode.Conv_ovf_i4_un: CheckedConv("int32_t", StackKind.I4, "int32_t", "-2147483647 - 1", "2147483647"); break;
            case ILOpCode.Conv_ovf_u4: case ILOpCode.Conv_ovf_u4_un: CheckedConv("int32_t", StackKind.I4, "uint32_t", "0", "4294967295"); break;
            case ILOpCode.Conv_ovf_i8: case ILOpCode.Conv_ovf_i8_un:
            case ILOpCode.Conv_ovf_i: case ILOpCode.Conv_ovf_i_un:
                Convert("int64_t", StackKind.I8, "(int64_t)"); break;
            case ILOpCode.Conv_ovf_u8: case ILOpCode.Conv_ovf_u8_un:
            case ILOpCode.Conv_ovf_u: case ILOpCode.Conv_ovf_u_un:
                CheckedConv("int64_t", StackKind.I8, "uint64_t", "0", "9223372036854775807"); break;

            case ILOpCode.Ckfinite:
            {
                var a = Pop();
                Emit($"if (!std::isfinite({a.Expr})) dn2cpp_fail(\"ArithmeticException (NaN/Inf)\");");
                Push(a.Kind, a.CppType, a.Expr);
                break;
            }

            case ILOpCode.Br: case ILOpCode.Br_s:
                BranchTo((int)insn.Operand, emitGoto: true);
                _unreachable = true;
                break;
            case ILOpCode.Brtrue: case ILOpCode.Brtrue_s:
            {
                // A folded-guard branch (BranchLiveness): the operand is a
                // const-folded getter's constant or a folded typeof comparison,
                // so emit only the live edge — taken becomes an unconditional
                // goto with a dead fall-through, not-taken falls through
                // recording no edge to the (dead) target. A folded getter's
                // constant WAS pushed and comes off the stack; a folded
                // type-identity window was elided wholesale and pushed
                // nothing, so its branch pops nothing.
                if (_liveness?.VerdictAt(insn.Offset) is bool taken)
                {
                    if (!_liveness.FoldedWindowBranch(insn.Offset))
                        Pop();
                    if (taken)
                    {
                        BranchTo((int)insn.Operand, emitGoto: true);
                        _unreachable = true;
                    }
                    break;
                }
                var a = Pop();
                // Statically non-null operand (a non-nullable value-type box): the
                // branch is always taken. Emitting it unconditionally and marking the
                // fall-through dead lets the never-executed reference-type path drop
                // out — that path otherwise merges a value-local address against a
                // span byref at the comparand and trips the stack-consistency check.
                if (a.NonNull)
                {
                    BranchTo((int)insn.Operand, emitGoto: true);
                    _unreachable = true;
                    break;
                }
                string cond = a.Kind is StackKind.Ref or StackKind.Ptr ? $"{a.Expr} != nullptr" : $"{a.Expr} != 0";
                BranchTo((int)insn.Operand, emitGoto: true, cond);
                break;
            }
            case ILOpCode.Brfalse: case ILOpCode.Brfalse_s:
            {
                // Folded-guard branch: same single-live-edge emission (and the
                // same pop rule) as brtrue.
                if (_liveness?.VerdictAt(insn.Offset) is bool takenF)
                {
                    if (!_liveness.FoldedWindowBranch(insn.Offset))
                        Pop();
                    if (takenF)
                    {
                        BranchTo((int)insn.Operand, emitGoto: true);
                        _unreachable = true;
                    }
                    break;
                }
                var a = Pop();
                // Statically non-null operand: brfalse is never taken — fall through
                // and do not record the target as reachable from here.
                if (a.NonNull)
                    break;
                string cond = a.Kind is StackKind.Ref or StackKind.Ptr ? $"{a.Expr} == nullptr" : $"{a.Expr} == 0";
                BranchTo((int)insn.Operand, emitGoto: true, cond);
                break;
            }
            case ILOpCode.Beq: case ILOpCode.Beq_s: CondBranch(insn, "==", false); break;
            case ILOpCode.Bne_un: case ILOpCode.Bne_un_s: CondBranch(insn, "!=", false); break;
            case ILOpCode.Bge: case ILOpCode.Bge_s: CondBranch(insn, ">=", false); break;
            case ILOpCode.Bgt: case ILOpCode.Bgt_s: CondBranch(insn, ">", false); break;
            case ILOpCode.Ble: case ILOpCode.Ble_s: CondBranch(insn, "<=", false); break;
            case ILOpCode.Blt: case ILOpCode.Blt_s: CondBranch(insn, "<", false); break;
            case ILOpCode.Bge_un: case ILOpCode.Bge_un_s: CondBranch(insn, ">=", true); break;
            case ILOpCode.Bgt_un: case ILOpCode.Bgt_un_s: CondBranch(insn, ">", true); break;
            case ILOpCode.Ble_un: case ILOpCode.Ble_un_s: CondBranch(insn, "<=", true); break;
            case ILOpCode.Blt_un: case ILOpCode.Blt_un_s: CondBranch(insn, "<", true); break;

            case ILOpCode.Ret:
                if (_method.Signature.ReturnType.IsVoid)
                {
                    Emit("return;");
                }
                else
                {
                    var rt = _method.Signature.ReturnType;
                    // CoerceTo notes a raw T[] returned as a collection interface so the
                    // array's own SZArray map serves the caller's dispatch.
                    Emit($"return {CoerceTo(Pop(), rt, CppTypes.Of(rt))};");
                }
                _unreachable = true;
                break;

            case ILOpCode.Throw:
            {
                var e = Pop();
                Emit($"dn2cpp_throw({Cast(e, "Dn2CppObject*")});");
                _unreachable = true;
                break;
            }
            case ILOpCode.Rethrow:
                Emit("throw;");
                _unreachable = true;
                break;
            case ILOpCode.Leave: case ILOpCode.Leave_s:
            {
                // Leave empties the evaluation stack. Inside a try guarded by
                // a finally it routes through the finally body first.
                _stack.Clear();
                int target = (int)insn.Operand;
                // A leave that exits a catch handler clause is that handler's
                // (only) normal completion: drop the caught exception's
                // in-flight GC root here. The root persists through the whole
                // handler body so `rethrow` (which skips this leave) keeps the
                // object rooted across its next unwind, and an unbound
                // `catch (X)` cannot lose the object once the bs0 temp is
                // reused. A handler abandoned by a new throw leaks its node to
                // the runtime's capped list instead.
                foreach (var (eg, _) in _catchGroups
                             .SelectMany(g => g.Clauses.Select(c => (g, c)))
                             .Where(t => insn.Offset >= t.c.Start && insn.Offset < t.c.End
                                         && !(target >= t.c.Start && target < t.c.End)))
                    Emit($"dn2cpp_exc_inflight_pop(__ex{eg.Index}.obj);");
                // Route through the innermost finally we are leaving (whose
                // protected range contains us but not the branch target).
                var fr = _finallyRegions
                    .Where(f => insn.Offset >= f.TryStart && insn.Offset < f.TryEnd
                                && !(target >= f.TryStart && target < f.TryEnd))
                    .OrderBy(f => f.TryEnd - f.TryStart)
                    .FirstOrDefault();
                if (fr is not null)
                {
                    if (!fr.LeaveTargets.TryGetValue(target, out int id))
                    {
                        id = fr.LeaveTargets.Count;
                        fr.LeaveTargets[target] = id;
                    }
                    _entryStacks.TryAdd(target, new List<StackEntry>());
                    Emit($"__leave{fr.Index} = {id};");
                    Emit($"goto __fin{fr.Index};");
                }
                else
                {
                    BranchTo(target, emitGoto: true);
                }
                _unreachable = true;
                break;
            }
            case ILOpCode.Endfinally:
            {
                // The same opcode (0xDC) ends a fault handler (`endfault`). A fault
                // is only entered on the exceptional path, so re-raise the captured
                // exception and propagate — no leave dispatch.
                var flt = _faultRegions.FirstOrDefault(f =>
                    insn.Offset >= f.HandlerStart && insn.Offset < f.HandlerEnd);
                if (flt is not null)
                {
                    Emit($"{{ auto __e = __feptr{flt.Index}; __feptr{flt.Index} = nullptr; std::rethrow_exception(__e); }}");
                    _unreachable = true;
                    break;
                }
                var fr = _finallyRegions.FirstOrDefault(f =>
                    insn.Offset >= f.HandlerStart && insn.Offset < f.HandlerEnd)
                    ?? throw new NotSupportedException($"{_method.Name}: endfinally outside a finally region");
                Emit($"if (__eptr{fr.Index}) {{ auto __e = __eptr{fr.Index}; __eptr{fr.Index} = nullptr; std::rethrow_exception(__e); }}");
                Emit($"switch (__leave{fr.Index})");
                Emit("{");
                foreach (var (target, id) in fr.LeaveTargets)
                {
                    // A `leave` that exits several nested finallys must run each in
                    // turn. After this finally, route to the innermost *enclosing*
                    // finally we are still leaving (its try contains this one but not
                    // the target); only when none remain do we land on the target
                    //. Registering the target on that finally is safe here
                    // because its own handler is emitted later in the walk.
                    var next = _finallyRegions
                        .Where(f => f != fr
                            && f.TryStart <= fr.TryStart && fr.TryEnd <= f.TryEnd
                            && !(target >= f.TryStart && target < f.TryEnd))
                        .OrderBy(f => f.TryEnd - f.TryStart)
                        .FirstOrDefault();
                    if (next is not null)
                    {
                        if (!next.LeaveTargets.TryGetValue(target, out int nid))
                        {
                            nid = next.LeaveTargets.Count;
                            next.LeaveTargets[target] = nid;
                        }
                        Emit($"    case {id}: __leave{next.Index} = {nid}; goto __fin{next.Index};");
                    }
                    else
                    {
                        Emit($"    case {id}: goto IL_{target:X4};");
                    }
                }
                Emit("    default: dn2cpp_fail(\"corrupt finally dispatch\");");
                Emit("}");
                _unreachable = true;
                break;
            }

            case ILOpCode.Endfilter:
            {
                // The filter block leaves an int on the stack: nonzero means
                // "execute this handler", zero means "continue the search". Resume
                // the ordered dispatch — run the handler if matched, otherwise try
                // the remaining clauses / re-raise.
                var result = Pop();
                (CatchGroup G, int Clause)? found = null;
                foreach (var g in _catchGroups)
                    for (int j = 0; j < g.Clauses.Count; j++)
                        if (g.Clauses[j].FilterStart is int fs && fs >= 0
                            && insn.Offset >= fs && insn.Offset < g.Clauses[j].Start)
                            found = (g, j);
                if (found is not { } fc)
                    throw new NotSupportedException($"{_method.Name}: endfilter outside a filter region");
                Emit($"if (({result.Expr}) != 0) goto H{fc.G.Index}_{fc.Clause};");
                EmitDispatchFrom(fc.G, fc.Clause + 1);
                _unreachable = true;
                break;
            }

            case ILOpCode.Newarr:
            {
                var newarrElem = ResolveTypeToken(insn.Token);
                var newarrLen = Pop();
                EmitNewarr(newarrElem, newarrLen.Expr, insn.Token);
                break;
            }
            case ILOpCode.Ldelem:
                EmitLdelem(ResolveTypeToken(insn.Token));
                break;
            case ILOpCode.Stelem:
                EmitStelem(ResolveTypeToken(insn.Token));
                break;
            case ILOpCode.Ldelema:
                EmitLdelema(ResolveTypeToken(insn.Token));
                break;

            case ILOpCode.Ldelem_ref:
            {
                var idx = Pop();
                var arr = Pop();
                Push(StackKind.Ref, "Dn2CppObject*", LdelemRef(arr, idx));
                // Keep the element's STATIC type on the loaded entry: a jagged
                // array's element (float[][] -> float[]) flowing straight into a
                // collection-interface parameter (data[0].Select(...)) is noted at
                // the CoerceTo boundary, which wires the element array's SZArray
                // interface-dispatch map — and that boundary can only see the
                // shape through StaticType. Without it the map is never wired and
                // the consumer's interface dispatch aborts at run time.
                if (arr.StaticType is { Kind: TypeKind.SZArray, Element: { } jaggedElem })
                    _stack[^1] = _stack[^1] with { StaticType = jaggedElem };
                break;
            }
            case ILOpCode.Stelem_ref:
            {
                var val = Pop();
                var idx = Pop();
                var arr = Pop();
                Emit(StelemRef(arr, idx, Cast(val, "Dn2CppObject*")));
                break;
            }
            case ILOpCode.Ldelem_i1: EmitLdelemPrim("int8_t", StackKind.I4, "int32_t"); break;
            case ILOpCode.Ldelem_u1: EmitLdelemPrim("uint8_t", StackKind.I4, "int32_t"); break;
            case ILOpCode.Ldelem_i2: EmitLdelemPrim("int16_t", StackKind.I4, "int32_t"); break;
            case ILOpCode.Ldelem_u2: EmitLdelemPrim("uint16_t", StackKind.I4, "int32_t"); break;
            case ILOpCode.Ldelem_i8: EmitLdelemPrim("int64_t", StackKind.I8, "int64_t"); break;
            case ILOpCode.Ldelem_i: EmitLdelemPrim("intptr_t", StackKind.I8, "int64_t"); break;
            case ILOpCode.Ldelem_r4: EmitLdelemPrim("float", StackKind.R8, "double"); break;
            case ILOpCode.Ldelem_r8: EmitLdelemPrim("double", StackKind.R8, "double"); break;
            case ILOpCode.Stelem_i1: EmitStelemPrim("int8_t"); break;
            case ILOpCode.Stelem_i2: EmitStelemPrim("int16_t"); break;
            case ILOpCode.Stelem_i8: EmitStelemPrim("int64_t"); break;
            case ILOpCode.Stelem_i: EmitStelemPrim("intptr_t"); break;
            case ILOpCode.Stelem_r4: EmitStelemPrim("float"); break;
            case ILOpCode.Stelem_r8: EmitStelemPrim("double"); break;

            case ILOpCode.Ldlen:
            {
                var arr = Pop();
                // The header load IS the dereference, so reading it inline
                // (`((Dn2CppArray*)a)->length`) faults the emitted body on a null
                // array — a SIGSEGV where .NET throws a catchable
                // NullReferenceException. dn2cpp_array_length is inline and its
                // check is one compare on a pointer the read dereferences anyway;
                // the invariant and the cost argument are at its definition.
                Push(StackKind.I4, "int32_t", $"dn2cpp_array_length((Dn2CppArray*){arr.Expr})");
                break;
            }
            // ldelem.u4 shares the i4 path: uint[] (and 4-byte-underlying enum[])
            // arrays use the Dn2CppArrayI4 rep — the packed Dn2CppArrayN cast the
            // other sized ldelem variants use would read at the wrong data offset.
            // The zero- vs sign-extension difference is unobservable on the 32-bit
            // stack slot (identical bit pattern). Reached by HuffmanTree's
            // uint[] canonical-code table in the managed Deflate64 inflater.
            case ILOpCode.Ldelem_i4:
            case ILOpCode.Ldelem_u4:
            {
                var idx = Pop();
                var arr = Pop();
                Push(StackKind.I4, "int32_t", LdelemI4(arr, idx));
                break;
            }
            case ILOpCode.Stelem_i4:
            {
                var val = Pop();
                var idx = Pop();
                var arr = Pop();
                Emit(StelemI4(arr, idx, val.Expr));
                break;
            }

            case ILOpCode.Ldfld:
            {
                // Vector256/512<T>._lower/_upper: the BCL's SIMD search paths read the
                // vector halves as private fields. Route to the get_lower/get_upper op.
                if (TryEmitVectorHalfField(insn.Token, wantAddress: false))
                    break;
                var (cls, fld) = ResolveField(insn.Token);
                // A field declared on System.Exception (an intrinsic type with no emitted
                // struct) maps onto the Dn2CppExceptionObject prefix — a get_Message
                // override reading base state (FileNotFoundException reads _message).
                if (IsExceptionBaseField(cls))
                {
                    var exObj = Pop();
                    string ect = ExceptionFieldCppType(fld);
                    Push(StackKind.Ref, ect, ExceptionFieldLValue(fld, exObj));
                    break;
                }
                NoteFieldOwnerLayout(cls);
                var obj = Pop();
                // A shared-layout member is declared with the owner's erased
                // spelling; cast the load back to this site's real spelling.
                string fldExpr = FieldAccess(cls, fld, obj);
                if (LayoutFieldType(cls, fld) != CppTypes.FieldOf(fld))
                    fldExpr = $"(({CppTypes.Of(fld.Type)})({fldExpr}))";
                Push(CppTypes.KindOf(fld.Type), CppTypes.Of(fld.Type), fldExpr);
                // Thread the field's declared type as StaticType so a List<T> read
                // from a field is recognised by TryListBacking, the same as a
                // List<T> local (string.Join/Concat over a field). SZArray is
                // included so a raw `T[]` read from a field (e.g. ImmutableArray<T>'s
                // backing `array`, which ImmutableArrayExtensions.Where hands to the
                // shim Enumerable.Where as IEnumerable<T>) carries its SZArray static
                // type — otherwise CoerceTo can't see the array→IEnumerable<T> flow and
                // never wires the array's SZArray interface-dispatch map, so MoveNext's
                // callvirt IEnumerable<T>.GetEnumerator traps in dn2cpp_resolve_interface
                //. Locals/args already thread it (PushVar).
                if (fld.Type is { Kind: TypeKind.Class or TypeKind.SZArray })
                    _stack[^1] = _stack[^1] with { StaticType = fld.Type };
                break;
            }
            case ILOpCode.Stfld:
            {
                var (cls, fld) = ResolveField(insn.Token);
                if (IsExceptionBaseField(cls))
                {
                    var exVal = Pop();
                    var exObj = Pop();
                    Emit($"{ExceptionFieldLValue(fld, exObj)} = {Cast(exVal, ExceptionFieldCppType(fld))};");
                    Emit($"dn2cpp_gc_write_barrier((void*)({exObj.Expr}));");
                    break;
                }
                NoteFieldOwnerLayout(cls);
                var val = Pop();
                TaintPoisonedTypeEscape(val);
                var obj = Pop();
                // Store with the member's declared spelling — the owner's erased
                // type when the declaring class shares its struct layout.
                string storeType = LayoutFieldType(cls, fld);
                if (obj.Kind == StackKind.Struct)
                {
                    // stfld into a field of a by-value struct: write through the
                    // struct's addressable temp so a dup'd sibling (the object-
                    // initializer / readonly defensive-copy pattern that leaves the
                    // mutated value on the stack) observes the store.
                    Emit($"({StructLValue(obj)}).{fld.CppName} = {Cast(val, storeType)};");
                    break;
                }
                Emit($"{FieldAccess(cls, fld, obj)} = {Cast(val, storeType)};");
                if (fld.Type.ContainsGcReferences())
                {
                    string barrier = obj.Kind == StackKind.Ref
                        ? "dn2cpp_gc_write_barrier"
                        : "dn2cpp_gc_write_barrier_if_heap";
                    Emit($"{barrier}((void*)({obj.Expr}));");
                }
                break;
            }
            case ILOpCode.Ldflda:
            {
                if (TryEmitVectorHalfField(insn.Token, wantAddress: true))
                    break;
                var (cls, fld) = ResolveField(insn.Token);
                if (IsExceptionBaseField(cls))
                {
                    var exObj = Pop();
                    string ect = ExceptionFieldCppType(fld);
                    Push(StackKind.Ptr, ect + "*", $"&({ExceptionFieldLValue(fld, exObj)})");
                    break;
                }
                NoteFieldOwnerLayout(cls);
                var obj = Pop();
                // The address of a shared-layout member has the owner's erased
                // pointer type; cast it to this site's real spelling (same
                // object width — the canonicalization is layout-preserving).
                string wantPtr = CppTypes.FieldOf(fld) + "*";
                bool erasedMember = LayoutFieldType(cls, fld) != CppTypes.FieldOf(fld);
                // ldflda of an [InlineArray] buffer's element field: the field is
                // emitted as a C array, so decay to the first element's address —
                // `&arr` would be a whole-array pointer (T(*)[N]), not the T* the
                // span-window call sites expect.
                string elemDecay = cls.InlineArrayLength > 0 ? "[0]" : "";
                if (obj.Kind == StackKind.Struct)
                {
                    // ldflda of a field of a by-value struct: take the address of the
                    // field inside the struct's addressable temp. The pointer type
                    // mirrors the struct body's field layout (CppTypes.FieldOf), so it
                    // stays consistent with the existing managed-pointer path below.
                    string saddr = $"&(({StructLValue(obj)}).{fld.CppName}{elemDecay})";
                    Push(StackKind.Ptr, wantPtr, erasedMember ? $"(({wantPtr}){saddr})" : saddr);
                    break;
                }
                string addr = $"&({FieldAccess(cls, fld, obj)}{elemDecay})";
                Push(StackKind.Ptr, wantPtr, erasedMember ? $"(({wantPtr}){addr})" : addr);
                break;
            }
            case ILOpCode.Ldsflda:
            {
                // ldsflda of an RVA-backed static field — a <PrivateImplementationDetails>
                // blob accessed directly (e.g. BitOperations.Log2's 32-byte De Bruijn
                // table, read as a ReadOnlySpan<byte>), rather than via ldtoken +
                // RuntimeHelpers.InitializeArray. The field symbol is never emitted, so
                // materialize the bytes as a static blob and push its address.
                if (SRME.EntityHandle(insn.Token) is { Kind: HandleKind.FieldDefinition } fh
                    && _reader.GetFieldDefinition((FieldDefinitionHandle)fh).GetRelativeVirtualAddress() != 0)
                {
                    string sym = _literals.AddBlob(FieldRvaBlob((FieldDefinitionHandle)fh));
                    Push(StackKind.Ptr, "const uint8_t*", sym);
                    break;
                }
                var (_, fld) = ResolveField(insn.Token);
                // ldsflda of an intrinsic value-type constant (e.g. TimeSpan.Zero.
                // ToString) — materialize the value into a temp and push its address
                // so the following value-type instance call has a receiver.
                if (TryIntrinsicStaticField(fld, wantAddress: true))
                    break;
                // A string constant on an INTRINSIC type, by address — materialize the
                // interned literal into a temp and push its address (an intrinsic type has
                // no emitted static storage).
                if (IntrinsicStaticStringConstant(fld) is { } esLit)
                {
                    string es = NewTemp("Dn2CppString*");
                    Emit($"{es} = {esLit};");
                    Push(StackKind.Ptr, "Dn2CppString**", $"&{es}");
                    break;
                }
                // The managed pointer names the slot's real storage width
                // (CppTypes.FieldOf), matching Ldflda and Of's ByRef arm: a
                // `ref ushort` into a `static ushort` must be a uint16_t* addressing
                // the whole slot, or a 2-byte store through it leaves the rest of a
                // widened slot carrying the previous value's sign bytes.
                if (SharedTrial && Compilation.ContainsCanonPlaceholder(fld.DeclaringClass))
                {
                    Push(StackKind.Ptr, CppTypes.FieldOf(fld) + "*", RgctxStaticAddr(fld, insn.Token));
                    break;
                }
                EnsureCctorBefore(fld.DeclaringClass);
                _c.NoteForceEmit(fld.DeclaringClass);
                Push(StackKind.Ptr, CppTypes.FieldOf(fld) + "*", $"&{fld.CppStaticAccess}");
                break;
            }
            case ILOpCode.Ldsfld:
            {
                var (_, fld) = ResolveField(insn.Token);
                // Decimal / TimeSpan / DateTime are intrinsic value types; their
                // static-field constants (Decimal.Zero/One/MinusOne/Max/Min, TimeSpan.
                // Zero/Min/Max, DateTime.Min/Max) have no emitted storage — fold them to
                // the runtime constructor. DateTime.Now/UtcNow/Today are
                // static *properties* (get_*) ->.
                if (TryIntrinsicStaticField(fld, wantAddress: false))
                    break;
                // System.Type is intrinsic, so its EmptyTypes static field (the canonical
                // empty Type[], commonly passed to GetConstructor/GetMethod) has no
                // emitted storage — fold it to the cached empty TYPED array (the declared
                // Type[] element identity, like real .NET's Array.Empty<Type>()), never
                // the untyped object[] mint: an object[]-tagged result refuses
                // IEnumerable<Type> interface dispatch and fails LINQ's `is Type[]` test.
                if (fld.DeclaringClass.FullName == "System.Type" && fld.Name == "EmptyTypes")
                {
                    if (fld.Type is { Kind: TypeKind.SZArray, Element: { } tyElem })
                    {
                        EmitEmptyArray(tyElem);
                        break;
                    }
                    Push(StackKind.Ref, "Dn2CppArrayRef*", "dn2cpp_newarr_ref(0)");
                    break;
                }
                // A string constant on an INTRINSIC type (String.Empty, Boolean.TrueString/
                // FalseString) — folded to the interned literal rather than left as a
                // dangling sf_ symbol. See IntrinsicStaticStringConstant.
                if (IntrinsicStaticStringConstant(fld) is { } sLit)
                {
                    Push(StackKind.Ref, "Dn2CppString*", sLit);
                    break;
                }
                // A framework EventSource-derived provider's static singleton
                // (ArrayPoolEventSource.Log, CDSCollectionETWBCLProvider.Log, ...). Never
                // instantiated (its .cctor is skipped in ReachCctor so EventSource's
                // manifest/finalizer cascade stays out of the tree), so its storage is
                // null — and the provider type itself is never emitted. Push a null
                // dummy: the only uses are the `if (Log.IsEnabled()) Log.Event(…)`
                // tracing guards, both lowered inline (see TryEmitEventSourceNoOp). A
                // non-reference static on a provider would be silently mis-folded, so it
                // fails loudly instead.
                if (Compilation.IsFrameworkEventSourceProvider(fld.DeclaringClass))
                {
                    if (CppTypes.KindOf(fld.Type) != StackKind.Ref)
                        throw new NotSupportedException(
                            $"{_method.DeclaringClass.FullName}.{_method.Name}: static field "
                            + $"{fld.DeclaringClass.FullName}.{fld.Name} on a no-op EventSource "
                            + "provider is not a reference type (unsupported provider shape)");
                    Push(StackKind.Ref, "Dn2CppObject*", "(Dn2CppObject*)nullptr");
                    break;
                }
                // A static-field access on a class that enters the emit set through
                // no other edge (the compiler-generated <>O method-group cache: only
                // Func<…> static fields, no.cctor, never instantiated) leaves
                // fld.CppStaticName undeclared. Force its full layout (= its static
                // fields) into the emit set. Intrinsic owners are filtered by
                // ComputeEmitted.Add (and their fields are handled above).
                if (SharedTrial && Compilation.ContainsCanonPlaceholder(fld.DeclaringClass))
                {
                    Push(CppTypes.KindOf(fld.Type), CppTypes.Of(fld.Type),
                        $"(*{RgctxStaticAddr(fld, insn.Token)})");
                    break;
                }
                EnsureCctorBefore(fld.DeclaringClass);
                _c.NoteForceEmit(fld.DeclaringClass);
                Push(CppTypes.KindOf(fld.Type), CppTypes.Of(fld.Type), fld.CppStaticAccess);
                break;
            }
            case ILOpCode.Stsfld:
            {
                var (_, fld) = ResolveField(insn.Token);
                if (_stack.Count > 0)
                    TaintPoisonedTypeEscape(_stack[^1]);
                // Force the owner's static-field declarations into the emit set (the
                // <>O method-group cache writes its delegate field here).
                // Store at the slot's declared width (CppTypes.FieldOf), like Stfld:
                // the cast IS the truncation ECMA-335 specifies for stsfld into a
                // sub-int32 field, which the int32-promoted slot used to skip.
                if (SharedTrial && Compilation.ContainsCanonPlaceholder(fld.DeclaringClass))
                {
                    string sfAddr = RgctxStaticAddr(fld, insn.Token);
                    Emit($"*{sfAddr} = {Cast(Pop(), CppTypes.FieldOf(fld))};");
                    break;
                }
                EnsureCctorBefore(fld.DeclaringClass);
                _c.NoteForceEmit(fld.DeclaringClass);
                Emit($"{fld.CppStaticAccess} = {Cast(Pop(), CppTypes.FieldOf(fld))};");
                if (fld.IsGcRootedThreadStatic)
                    Emit($"dn2cpp_gc_write_barrier((void*)&({fld.CppStaticAccess}));");
                break;
            }

            // Initobj/Ldobj/Stobj dereference at the type's REAL storage width
            // (StorageOf), widening/truncating to the int32-promoted stack model
            // like ldind/stind: a byref can point into packed storage (a string's
            // chars, an array, an exact-layout struct), where dereferencing a
            // sub-int32 primitive at the promoted width reads/clobbers the
            // neighboring element. Hit in practice by canonical shared-generics
            // bodies over TChar=char (Guid.TryParseGuid's `ldobj !!TChar` through
            // ReadOnlySpan<char>.get_Item's ref into the string buffer).
            case ILOpCode.Initobj:
            {
                string mt = CppTypes.StorageOf(ResolveTypeToken(insn.Token));
                var p = Pop();
                string zero = CppTypes.ZeroInitExpr(mt);
                Emit($"*(({mt}*)({p.Expr})) = {zero};");
                break;
            }
            case ILOpCode.Ldobj:
            {
                var t = ResolveTypeToken(insn.Token);
                string ct = CppTypes.Of(t);
                string mt = CppTypes.StorageOf(t);
                var p = Pop();
                Push(CppTypes.KindOf(t), ct,
                    mt == ct ? $"*(({ct}*)({p.Expr}))" : $"({ct})(*(({mt}*)({p.Expr})))");
                break;
            }
            case ILOpCode.Stobj:
            {
                var t = ResolveTypeToken(insn.Token);
                string ct = CppTypes.Of(t);
                string mt = CppTypes.StorageOf(t);
                var val = Pop();
                var p = Pop();
                Emit(mt == ct
                    ? $"*(({ct}*)({p.Expr})) = {Cast(val, ct)};"
                    : $"*(({mt}*)({p.Expr})) = ({mt})({Cast(val, ct)});");
                if (t.ContainsGcReferences())
                    Emit($"dn2cpp_gc_write_barrier_if_heap((void*)({p.Expr}));");
                break;
            }

            case ILOpCode.Ldind_i1: LoadIndirect("int8_t", StackKind.I4, "int32_t"); break;
            case ILOpCode.Ldind_u1: LoadIndirect("uint8_t", StackKind.I4, "int32_t"); break;
            case ILOpCode.Ldind_i2: LoadIndirect("int16_t", StackKind.I4, "int32_t"); break;
            case ILOpCode.Ldind_u2: LoadIndirect("uint16_t", StackKind.I4, "int32_t"); break;
            case ILOpCode.Ldind_i4: LoadIndirect("int32_t", StackKind.I4, "int32_t"); break;
            case ILOpCode.Ldind_u4: LoadIndirect("uint32_t", StackKind.I4, "int32_t"); break;
            case ILOpCode.Ldind_i8: LoadIndirect("int64_t", StackKind.I8, "int64_t"); break;
            case ILOpCode.Ldind_i: LoadIndirect("intptr_t", StackKind.I8, "int64_t"); break;
            case ILOpCode.Ldind_r4: LoadIndirect("float", StackKind.R8, "double"); break;
            case ILOpCode.Ldind_r8: LoadIndirect("double", StackKind.R8, "double"); break;
            case ILOpCode.Ldind_ref: LoadIndirectRef(); break;

            case ILOpCode.Stind_i1: StoreIndirect("int8_t"); break;
            case ILOpCode.Stind_i2: StoreIndirect("int16_t"); break;
            case ILOpCode.Stind_i4: StoreIndirect("int32_t"); break;
            case ILOpCode.Stind_i8: StoreIndirect("int64_t"); break;
            case ILOpCode.Stind_i: StoreIndirect("intptr_t"); break;
            case ILOpCode.Stind_r4: StoreIndirect("float"); break;
            case ILOpCode.Stind_r8: StoreIndirect("double"); break;
            case ILOpCode.Stind_ref: StoreIndirectRef(); break;

            case ILOpCode.Localloc:
            {
                // stackalloc: carve memory out of the current frame; its lifetime
                // is the whole method (matching ECMA-335 localloc). localsinit
                // (on by default) zeroes it.
                var size = Pop();
                string sz = NewTemp("size_t");
                Emit($"{sz} = (size_t)({size.Expr});");
                string buf = NewTemp("int8_t*");
                Emit($"{buf} = (int8_t*)__builtin_alloca({sz});");
                Emit($"std::memset({buf}, 0, {sz});");
                _stack.Add(new StackEntry(buf, StackKind.Ptr, "int8_t*"));
                break;
            }
            case ILOpCode.Cpblk:
            {
                // cpblk: copy <size> bytes from srcaddr to destaddr (ECMA-335
                // III.3.30, stack:..., destaddr, srcaddr, size). C# emits this for
                // `stackalloc T[] {... }` blob-init, and BCL bodies use it directly.
                // memmove is the safe superset: identical to memcpy for the
                // non-overlapping contract callers rely on, and correct under overlap.
                var size = Pop();
                var src = Pop();
                var dest = Pop();
                Emit($"std::memmove((void*)({dest.Expr}), (void*)({src.Expr}), (size_t)({size.Expr}));");
                Emit($"dn2cpp_gc_write_barrier_if_heap((void*)({dest.Expr}));");
                break;
            }
            case ILOpCode.Initblk:
            {
                // initblk: set <size> bytes at addr to the byte <value> (ECMA-335
                // III.3.36, stack:..., addr, value, size). memset reads value as
                // unsigned char, matching the IL's "byte" semantics.
                var size = Pop();
                var val = Pop();
                var addr = Pop();
                Emit($"std::memset((void*)({addr.Expr}), (int)({val.Expr}), (size_t)({size.Expr}));");
                break;
            }
            case ILOpCode.Sizeof:
            {
                // Report the element's storage width (sizeof(char)==2, not the int32
                // stack width) so it stays consistent with packed sub-word arrays /
                // Span<T> pointer arithmetic. StorageOf == Of for every
                // non-sub-word type, so other sizes are unchanged.
                string ct = CppTypes.StorageOf(ResolveTypeToken(insn.Token));
                Push(StackKind.I4, "int32_t", $"(int32_t)sizeof({ct})");
                break;
            }

            case ILOpCode.Switch:
            {
                var v = Pop();
                for (int t = 0; t < insn.SwitchTargets!.Length; t++)
                    BranchTo(insn.SwitchTargets[t], emitGoto: true, $"{v.Expr} == {t}");
                break;
            }

            case ILOpCode.Ldtoken:
            {
                // ldtoken <field>: the RVA blob of a static array initializer
                // (`RuntimeHelpers.InitializeArray`). Materialize the field's raw
                // bytes as a static blob and push a pointer to it.
                if (SRME.EntityHandle(insn.Token).Kind == HandleKind.FieldDefinition)
                {
                    byte[] blob = FieldRvaBlob((FieldDefinitionHandle)SRME.EntityHandle(insn.Token));
                    Push(StackKind.Ptr, "const uint8_t*", _literals.AddBlob(blob));
                    // Carry the blob byte length so a directly-following CreateSpan can
                    // size the span (the Push spilled the symbol to a temp).
                    _stack[^1] = _stack[^1] with { BlobLen = blob.Length };
                    break;
                }
                // ResolveCastTarget, not ResolveTypeToken: a cross-assembly
                // reference TypeRef must promote to its loaded ClassInfo here too,
                // or typeof(T) over such a type pushes a null handle and a
                // GetType()==typeof(T) comparison silently misreports (surfaced by
                // [GeneratedRegex]'s cached-fallback check comparing against
                // typeof(Regex) from a user assembly). Runtime-raised exception
                // types stay External and keep their shared runtime handle.
                var target = ResolveCastTarget(insn.Token);
                // Shared-body candidate: typeof over a placeholder-bearing type is
                // instantiation-dependent only when the Type VALUE is consumed at
                // runtime — the pervasive typeof(T).IsValueType / GetTypeCode
                // folds read the static token alone, and for an enum vs its
                // underlying placeholder those folds agree. Push a null handle
                // (dead once folded, a loud null deref if a missed consumer runs
                // it) with the token riding along; the runtime-consuming Type
                // intrinsics taint on a poisoned token instead.
                if (SharedTrial && Compilation.ContainsCanonPlaceholder(target))
                {
                    Push(StackKind.Ptr, "const Dn2CppTypeInfo*", "nullptr", target);
                    break;
                }
                // typeof(value type) names its ti_ even when the struct is never used as a
                // value — e.g. typeof(int?) -> Nullable<Int32>, or typeof(Half) appearing
                // only in a reflection type-equality chain. Such a type is never pulled
                // into the value-emit set, so its const would be undeclared. Note it
                // for (opaque) emission; the type-info loop still emits its
                // genericDef/genericArgs, which Nullable.GetUnderlyingType / IsGenericType read.
                // System.Void is excluded: TypeInfoExprOf routes it to the shared
                // &dn2cpp_void_type, so noting it would only emit an orphan ti_.
                if (target is { Kind: TypeKind.Class, Class: { IsValueType: true, IntrinsicCppName: null } tc }
                    && tc.FullName != "System.Void")
                    _c.NoteReferencedType(tc);
                // typeof(T[]): record the element and push the precise per-element array
                // type-info, so typeof(T[]) and a freshly-allocated T[]'s
                // GetType are the same handle — the Span array-covariance guard
                // (array.GetType != typeof(T[])) then matches exactly and reflection on
                // typeof(T[]) reports the element type. Resolvable via GetType("T[]") too.
                if (target.Kind == TypeKind.SZArray
                    && target.Element is { Kind: TypeKind.Primitive or TypeKind.Class or TypeKind.External or TypeKind.SZArray or TypeKind.MDArray })
                {
                    _c.NoteArrayElementType(target.Element!);
                    Push(StackKind.Ptr, "const Dn2CppTypeInfo*", PreciseArrayTypeInfoExpr(target.Element!), target);
                    break;
                }
                // typeof(T[,]): the interned (element, rank) identity, the SAME handle a
                // `new T[,]` of that shape stamps and the same one CastTargetTypeInfoExpr
                // already builds for an isinst/castclass target. Falling through to the
                // nullptr fold below would make typeof(int[,]) a null Type, so
                // GetInterfaces()/Name/BaseType on it would throw NullReferenceException.
                // The fold's remaining subjects — a pointer, a byref, a function pointer —
                // genuinely have no metadata to name; an MD array does, and the interner
                // (dn2cpp_array_ti) is where it lives.
                if (target is { Kind: TypeKind.MDArray, Element: { } mdElem, Rank: var mdRank })
                {
                    _c.NoteMdArrayUse();
                    if (mdElem.Kind is TypeKind.Class && mdElem.Class is { } mdCls)
                        NoteReferencedType(mdCls);
                    string mdElemTi = MdSzElementTypeInfoExpr(mdElem)
                        ?? TypeInfoExpr(mdElem, insn.Token) ?? "nullptr";
                    Push(StackKind.Ptr, "const Dn2CppTypeInfo*",
                        $"dn2cpp_mdarr_ti({mdElemTi}, {mdRank})", target);
                    break;
                }
                // typeof(<open generic definition>), e.g. typeof(IDictionary<,>) /
                // typeof(List<>). A bare open definition names no ClassInfo, so
                // TypeInfoExprOf below would fold it to nullptr and typeof would return
                // null. Route it to the shared
                // gendef_<sym> open-definition type-info handle (DN2CPP_TF_GENERICDEF) — the
                // SAME symbol a closed instantiation's genericDef points at, so
                // GetGenericTypeDefinition() == typeof(Def<>) holds by handle identity and
                // IsGenericTypeDefinition/FullName answer.
                if (OpenGenericDefTypeInfoExpr(target) is { } gdExpr)
                {
                    Push(StackKind.Ptr, "const Dn2CppTypeInfo*", gdExpr, target);
                    break;
                }
                string? ti = TypeInfoExpr(target);
                // A `typeof` whose operand names a type from an assembly no `-r` supplied
                // must not FAIL OPEN. Such a TypeRef degrades to an External that
                // TypeInfoExprOf has no arm for, so the `?? "nullptr"` below would leave the
                // transpile green and the run-time answer a NULL Type — from which every
                // consumer throws, in the one place a diagnostic reaches nobody. Refuse at
                // transpile time instead, naming the type and the assembly to add, the same
                // posture box/unbox.any take when an External is used as a VALUE.
                //
                // Only the External arm throws. The other null answers here are real shapes
                // with no metadata to name — a pointer, a byref, a function pointer — and
                // they keep the nullptr fold. (An MD array is not one of them: it has an
                // interned identity, and its arm is above.) The shared-body placeholder fold
                // and the open-generic-definition route are taken above.
                if (ti is null && target.Kind == TypeKind.External)
                    throw new NotSupportedException(
                        $"typeof({target.ExternalName}) names a type whose assembly is not loaded"
                        + $"{ExternalTokenAssemblyNote(insn.Token)} — pass it with -r, or the "
                        + "typeof would answer a null Type at run time");
                Push(StackKind.Ptr, "const Dn2CppTypeInfo*", ti ?? "nullptr", target);
                break;
            }

            case ILOpCode.Castclass:
            {
                var target = ResolveCastTarget(insn.Token);
                var obj = Pop();
                // (IComparable<T>)box where T is a primitive/enum/string: the boxed
                // value's intrinsic type-info carries no IComparable<T> interface map,
                // so a normal castclass against the interface throws InvalidCastException.
                // The JIT treats this cast as valid for the matching boxed primitive;
                // verify against T's own concrete type-info instead (which the box
                // carries), keeping the boxed reference for the CompareTo callvirt to
                // devirtualize.
                if (ComparablePrimitiveArg(target) is { } cmpT && TypeArg0TypeInfoExpr(cmpT, insn.Token) is { } pti)
                {
                    Push(StackKind.Ref, "Dn2CppObject*",
                        $"(Dn2CppObject*)dn2cpp_castclass((Dn2CppObject*){obj.Expr}, {pti})");
                    break;
                }
                // An NFI-mapped target (CultureInfo/NumberFormatInfo/TextInfo/
                // IFormatProvider — the headerless `const Dn2CppNumberFormatInfo*`
                // lowering): an object-typed source may hold the interned wrapper an
                // escape minted, so the cast must verify AND unwrap
                // (dn2cpp_nfi_castclass). An NFI-typed source stays the erased
                // identity cast it always was — feeding it to a header-reading cast
                // would misread the struct.
                if (target.Kind != TypeKind.External && IsNfiCppType(CppTypes.Of(target)))
                {
                    string nct = CppTypes.Of(target);
                    if (IsNfiCppType(obj.CppType))
                    {
                        Push(StackKind.Ref, nct, Cast(obj, nct));
                        break;
                    }
                    string? nti = CastTargetTypeInfoExpr(target, insn.Token);
                    Push(StackKind.Ref, nct,
                        $"(({nct})dn2cpp_nfi_castclass({Cast(obj, "Dn2CppObject*")}, {NfiKindOf(target)}, {nti ?? "nullptr"}))");
                    break;
                }
                // An Assembly/Module target (the headerless `const char*` lowering):
                // same wrapper-aware split as the NFI arm — an object-typed source may
                // hold the interned wrapper an escape minted, so the cast must verify
                // AND unwrap (dn2cpp_asm_castclass). An asm-typed source stays the
                // erased identity cast it always was.
                if (target.Kind != TypeKind.External && IsAsmCppType(CppTypes.Of(target)))
                {
                    if (IsAsmCppType(obj.CppType))
                    {
                        Push(StackKind.Ref, "const char*", Cast(obj, "const char*"));
                        break;
                    }
                    string? ati = CastTargetTypeInfoExpr(target, insn.Token);
                    Push(StackKind.Ref, "const char*",
                        $"dn2cpp_asm_castclass({Cast(obj, "Dn2CppObject*")}, {AsmKindOf(target)}, {ati ?? "nullptr"})");
                    break;
                }
                // An External reference target (a runtime-canonical exception handle, or
                // a ref type with no loaded definition) has no emitted C++ struct — the
                // verified reference is typed as the generic object pointer.
                // The OPERAND goes through Cast, not a C cast: `x is T` over a
                // headerless intrinsic source is an object boundary the IL does not
                // spell — Roslyn elides the `object` local, so `object o = asm; return
                // o is Assembly;` arrives here as an isinst straight off the
                // const char* — and dn2cpp_isinst reads a type header off it.
                string cppType = target.Kind == TypeKind.External ? "Dn2CppObject*" : CppTypes.Of(target);
                string? ti = CastTargetTypeInfoExpr(target, insn.Token);
                Push(StackKind.Ref, cppType, ti is null
                    ? Cast(obj, cppType) // System.Object etc.: statically safe
                    : $"({cppType})dn2cpp_castclass({Cast(obj, "Dn2CppObject*")}, {ti})");
                break;
            }
            case ILOpCode.Isinst:
            {
                var target = ResolveCastTarget(insn.Token);
                var obj = Pop();
                // Only when the target genuinely carries no runtime type-info (System.
                // Object, or a reference type with no loaded definition AND no runtime
                // handle) does the cast degrade to an identity match (the managed
                // reference itself). ResolveCastTarget has already promoted a loadable
                // reference TypeRef to its ClassInfo and the runtime-raised exception set
                // maps to a shared handle, so a real `dn2cpp_isinst` runs below for them
                // — `o is T` no longer folds to "always true".
                // NFI-mapped target: same wrapper-aware split as the Castclass arm
                // above (verify and unwrap; an NFI-typed source keeps the erased
                // identity — non-null iff the reference is, which is what the
                // pattern's null test reads).
                if (target.Kind != TypeKind.External && IsNfiCppType(CppTypes.Of(target)))
                {
                    string nct = CppTypes.Of(target);
                    if (IsNfiCppType(obj.CppType))
                    {
                        Push(StackKind.Ref, nct, Cast(obj, nct));
                        break;
                    }
                    string? nti = CastTargetTypeInfoExpr(target, insn.Token);
                    Push(StackKind.Ref, nct,
                        $"(({nct})dn2cpp_nfi_isinst({Cast(obj, "Dn2CppObject*")}, {NfiKindOf(target)}, {nti ?? "nullptr"}))");
                    break;
                }
                // Assembly/Module target: same wrapper-aware split as the Castclass
                // arm above (verify and unwrap; an asm-typed source keeps the erased
                // identity — non-null iff the reference is, which is what the
                // pattern's null test reads).
                if (target.Kind != TypeKind.External && IsAsmCppType(CppTypes.Of(target)))
                {
                    if (IsAsmCppType(obj.CppType))
                    {
                        Push(StackKind.Ref, "const char*", Cast(obj, "const char*"));
                        break;
                    }
                    string? ati = CastTargetTypeInfoExpr(target, insn.Token);
                    Push(StackKind.Ref, "const char*",
                        $"dn2cpp_asm_isinst({Cast(obj, "Dn2CppObject*")}, {AsmKindOf(target)}, {ati ?? "nullptr"})");
                    break;
                }
                if (CastTargetTypeInfoExpr(target, insn.Token) is not { } ti)
                {
                    Push(StackKind.Ref, "Dn2CppObject*", Cast(obj, "Dn2CppObject*"));
                    break;
                }
                // isinst always yields a managed reference — the object cast to the
                // target type, or null. For a value-type target that reference is the
                // BOXED form (a subsequent unbox.any recovers the value), so the stack
                // type is Dn2CppObject*, not the unboxed primitive — casting the
                // pointer to int32_t truncated it and broke the null test in `o is
                // int`/`o switch { int n => … }`.
                string cppType = target.Kind != TypeKind.External && CppTypes.KindOf(target) == StackKind.Ref
                    ? CppTypes.Of(target) : "Dn2CppObject*";
                Push(StackKind.Ref, cppType, $"({cppType})dn2cpp_isinst({Cast(obj, "Dn2CppObject*")}, {ti})");
                break;
            }
            case ILOpCode.Box:
            {
                var target = ResolveTypeToken(insn.Token);
                if (CppTypes.KindOf(target) == StackKind.Ref)
                    break; // boxing a reference type is a no-op
                // Nullable<T>: the CLR's `box` yields the boxed *underlying* value when
                // HasValue, else a null reference — so GetType / `is T` / null-compare
                // on the result match real.NET (a plain struct box would wrongly report
                // Nullable`1 and never be null). Read the real Nullable`1 layout's
                // hasValue/value fields directly.
                if (NullableLayout(target) is (var uT, var hvF, var valF))
                {
                    string uti = TypeArg0TypeInfoExpr(uT, insn.Token)
                        ?? throw new NotSupportedException($"box of {target} is not supported yet");
                    string uct = CppTypes.Of(uT);
                    string nct = CppTypes.Of(target);
                    var nval = Pop();
                    string ntmp = NewTemp(nct);
                    Emit($"{ntmp} = {Cast(nval, nct)};");
                    // Copy the value member into an Of-width temp and box THAT: the
                    // Nullable layout's `value` field is at its real storage width
                    // (1 byte for Nullable<byte>), so boxing &field with sizeof(Of)
                    // would over-read past the field. The box payload convention is
                    // Of-width. (Reading the field when hasValue is 0 just copies
                    // the zeroed default — harmless.)
                    string vtmp = NewTemp(uct);
                    Emit($"{vtmp} = ({uct})({ntmp}.{valF});");
                    Push(StackKind.Ref, "Dn2CppObject*",
                        $"({ntmp}.{hvF} ? dn2cpp_box({uti}, &{vtmp}, sizeof({uct})) : (Dn2CppObject*)nullptr)");
                    break;
                }
                string ti = TypeInfoExpr(target, insn.Token)
                    ?? throw new NotSupportedException($"box of {target} is not supported yet");
                string ct = CppTypes.Of(target);
                var val = Pop();
                // Ensure an addressable lvalue of the exact unboxed layout.
                string tmp = NewTemp(ct);
                Emit($"{tmp} = {Cast(val, ct)};");
                Push(StackKind.Ref, "Dn2CppObject*", $"dn2cpp_box({ti}, &{tmp}, sizeof({ct}))");
                // A box of a non-nullable value type is statically non-null (only the
                // Nullable<T> path above can produce null). Recording that lets a
                // following brtrue/brfalse fold the constant branch — the JIT does the
                // same — which drops the dead reference-type comparand path the C#
                // compiler emits for generic `default(T) == null ? … : x.CompareTo(y)`.
                _stack[^1] = _stack[^1] with { NonNull = true };
                break;
            }
            case ILOpCode.Unbox:
            {
                // A managed pointer INTO the box, not a copy out of it (that is unbox.any):
                // the payload sits one header past the object, which is precisely what
                // dn2cpp_unbox returns once it has verified the type. Roslyn emits this for
                // `((T)obj).Field` — read a field of a boxed struct without copying the
                // struct — and that is the body of nearly every hand-written
                // `Equals(object)` on a value type.
                var ubTarget = ResolveTypeToken(insn.Token);
                var ubObj = Pop();
                string ubTi = TypeInfoExpr(ubTarget, insn.Token)
                    ?? throw new NotSupportedException(
                        $"{_method.DeclaringClass.FullName}.{_method.Name}: unbox of {ubTarget} has no emitted type-info");
                string ubCt = CppTypes.Of(ubTarget);
                Push(StackKind.Ptr, ubCt + "*",
                    $"(({ubCt}*)dn2cpp_unbox({Cast(ubObj, "Dn2CppObject*")}, {ubTi}))");
                break;
            }
            case ILOpCode.Unbox_any:
            {
                var target = ResolveTypeToken(insn.Token);
                var obj = Pop();
                if (CppTypes.KindOf(target) == StackKind.Ref)
                {
                    // unbox.any on a reference type behaves like castclass.
                    string cppType = CppTypes.Of(target);
                    string? cti = TypeInfoExpr(target, insn.Token);
                    Push(StackKind.Ref, cppType, cti is null
                        ? Cast(obj, cppType)
                        : $"({cppType})dn2cpp_castclass((Dn2CppObject*){obj.Expr}, {cti})");
                    break;
                }
                // Nullable<T>: the inverse of the special box above — a null reference
                // unboxes to default(Nullable<T>) (HasValue=false), a boxed T to a
                // Nullable<T> carrying it (HasValue=true).
                if (NullableLayout(target) is (var uT2, var hvF2, var valF2))
                {
                    string uti2 = TypeArg0TypeInfoExpr(uT2, insn.Token)
                        ?? throw new NotSupportedException($"unbox.any of {target} is not supported yet");
                    string uct2 = CppTypes.Of(uT2);
                    string nct2 = CppTypes.Of(target);
                    string otmp = NewTemp("Dn2CppObject*");
                    Emit($"{otmp} = (Dn2CppObject*){obj.Expr};");
                    string ntmp2 = NewTemp(nct2);
                    Emit($"{ntmp2} = {{}};");
                    Emit($"if ({otmp}) {{ {ntmp2}.{hvF2} = 1; {ntmp2}.{valF2} = *(({uct2}*)dn2cpp_unbox({otmp}, {uti2})); }}");
                    Push(CppTypes.KindOf(target), nct2, ntmp2);
                    break;
                }
                string ti = TypeInfoExpr(target, insn.Token)
                    ?? throw new NotSupportedException($"unbox.any of {target} is not supported yet");
                string ct = CppTypes.Of(target);
                Push(CppTypes.KindOf(target), ct, $"*(({ct}*)dn2cpp_unbox((Dn2CppObject*){obj.Expr}, {ti}))");
                break;
            }

            case ILOpCode.Ldftn:
            {
                // A generic-instantiated method (MethodSpecification) or a method on a
                // generic / cross-module type (MemberReference) is resolved the same way
                // a call site resolves its target, then fed through the same delegate-
                // adapter path below — so `ldftn` of a closed predicate-combiner or
                // comparison helper is supported, not just a plain MethodDefinition.
                var m = ResolveMethodHandle(SRME.EntityHandle(insn.Token), "ldftn");
                // --trim-godot-classes, the emit-side ROUTE: a deferred registry
                // lambda's body is never emitted, so its symbol must never be named
                // — its address is the nearest released ancestor's lambda instead
                // (Compilation.TrimLdftnRedirect; table frozen before emission).
                if (_c.TrimLdftnRedirect(m) is { } trimRedirect)
                    m = trimRedirect;
                // A shared body taking the address of a canonical-world method
                // would bake an owner-group function identity into delegate/
                // function-pointer state observable per instantiation.
                if (SharedTrial && Compilation.IsCanonicalMethod(m))
                    ThrowSharedTaint("ldftn", m.DeclaringClass.FullName + "." + m.Name);
                bool ftnBounded = _c.IsBoundedMethod(m.DeclaringClass.FullName, m.Name);
                // A bodyless P/Invoke whose call sites lower to a direct native call (a
                // delegate method group over the [DllImport] itself): taking the address
                // needs a function, and no real body ever exists. Note it so the emitter
                // synthesizes a forwarder from the same P/Invoke lowering a call site gets
                // (CompilePInvokeWrapper); the delegate adapter or raw function pointer
                // below then wraps/names that symbol.
                //
                // NOT when the import is BOUNDED. This note is a route, not a record: it
                // adds the method to Reachable, makes CppEmitter synthesize a forwarder that
                // names the native symbol, and puts the import's module into
                // pinvoke-libs.txt — while reachability has just deleted the edge to it and
                // the stub below is the substitute. So a `--cut` P/Invoke taken as a method
                // group would transpile green and fail at link, asking for exactly the
                // module the cut exists to remove: AGENTS.md's `cut ⟹ route` in its
                // NATIVE-symbol dimension, which AssertCalledBodiesEmitted cannot see (it
                // diffs managed symbols, and a P/Invoke has no emitted body).
                if (!ftnBounded && m.Emittable is { Rva: 0, PInvoke: not null } fimpl
                    && _c.LowersToDirectNativeCall(fimpl))
                    _c.NotePInvokeFtnTarget(fimpl);
                string expr;
                if (ftnBounded)
                {
                    // A bounded method's body is cut at reachability, so its symbol
                    // never exists — taking its address (a C# function pointer planted
                    // in a backend's native callback table) gets a stub with the shape
                    // its consumer will call through instead (FtnStubShape).
                    // Mirrors the call-site neutralization: default result, args ignored.
                    // Including its report: the second mouth of the bound, so a
                    // bounded import reached ONLY as a delegate method group is named too.
                    expr = BoundedFtnStub(m, insn.Offset, receiverSlot: false);
                }
                else if (_c.IsDynamicCodegenMember(m.DeclaringClass, m.Name))
                {
                    // The dynamic-code-generation surface: same shape-preserving
                    // stub as a bounded method, but invoking it throws the
                    // catchable PlatformNotSupportedException (mirroring the
                    // call-site lowering). The [[noreturn]] throw satisfies the
                    // non-void return path.
                    expr = DynamicCodegenFtnStub(m, insn.Offset, receiverSlot: false);
                }
                else if (_c.IsAbsentNetworkPalMember(m.DeclaringClass, m.Name))
                {
                    // The absent socket / name-resolution PAL: the same shape-preserving
                    // stub, throwing the same sentence the call site does.
                    expr = AbsentNetworkPalFtnStub(m, insn.Offset, receiverSlot: false);
                }
                else if (m.IsStatic && _ftnDelegateUse.TryGetValue(insn.Offset, out var dgClass))
                {
                    // Delegates invoke with a target slot; static targets get an
                    // adapter that either ignores it (open) or takes the static's
                    // bound first argument out of it (closed). The adapter wraps the
                    // symbol that carries the body (the canonical shared impl when one
                    // is assigned). An intrinsic-mapped type's method has no
                    // transpiled body to wrap — synthesize one from its call
                    // intrinsic's lowering so the adapter's callee exists.
                    var impl = m.Emittable;
                    if (CoreIntrinsics.IsIntrinsicType(impl.DeclaringClass.FullName))
                        _c.NoteIntrinsicFtnTarget(impl);
                    var adapter = new DelegateAdapter(impl, IsClosedStaticDelegate(dgClass, impl),
                        NeedsNfiErasedAdapter(impl));
                    if (!_c.DelegateAdapters.Contains(adapter))
                        _c.DelegateAdapters.Add(adapter);
                    // The adapter names impl's symbol in its own one-line body, so
                    // impl needs a body just as a direct call to it would.
                    _c.NoteNamedBodySymbol(_method, impl);
                    expr = $"(void*)&{adapter.CppName}";
                }
                else if (!m.IsStatic && _ftnDelegateUse.ContainsKey(insn.Offset)
                         && NeedsNfiErasedAdapter(m.Emittable))
                {
                    // An INSTANCE target normally goes into f_method as its own
                    // symbol — the invoker's cast passes the receiver through the
                    // target slot and every other position renders alike. A
                    // headerless intrinsic position does not render alike, so the
                    // method gets the NFI-erasing adapter: the one shape for which an
                    // instance target has an adapter at all.
                    if (CoreIntrinsics.IsIntrinsicType(m.Emittable.DeclaringClass.FullName))
                        _c.NoteIntrinsicFtnTarget(m.Emittable);
                    var adapter = new DelegateAdapter(m.Emittable, false, NfiErased: true);
                    if (!_c.DelegateAdapters.Contains(adapter))
                        _c.DelegateAdapters.Add(adapter);
                    _c.NoteNamedBodySymbol(_method, m.Emittable);
                    expr = $"(void*)&{adapter.CppName}";
                }
                else
                {
                    // Instance method, or a C# function pointer (delegate*<...> via
                    // &Method): the raw method address. A function pointer is invoked
                    // by calli with no target slot, so it must point at the method
                    // itself, not the delegate adapter. Same synthesized-body rule
                    // for an intrinsic-mapped type's method.
                    if (CoreIntrinsics.IsIntrinsicType(m.Emittable.DeclaringClass.FullName))
                        _c.NoteIntrinsicFtnTarget(m.Emittable);
                    _c.NoteNamedBodySymbol(_method, m.Emittable);
                    expr = $"(void*)&{m.Emittable.CppName}";
                }
                Push(StackKind.Ptr, "void*", expr);
                break;
            }
            case ILOpCode.Ldvirtftn:
            {
                // As with ldftn, a method on a closed generic type (MemberReference) or a
                // generic-instantiated method (MethodSpecification) is resolved like a
                // call site — so a virtual method group on a generic type bound to a
                // delegate (e.g. the EnumerableSorter<T> comparison helpers) resolves to
                // its vtable slot, rather than only a plain MethodDefinition.
                var m = ResolveMethodHandle(SRME.EntityHandle(insn.Token), "ldvirtftn");
                if (SharedTrial && Compilation.IsCanonicalMethod(m))
                    ThrowSharedTaint("ldftn", m.DeclaringClass.FullName + "." + m.Name);
                if (SharedTrial && Compilation.IsGvmCall(m))
                    ThrowSharedTaint("gvm", m.DeclaringClass.FullName + "." + m.Name);
                var obj = Pop();
                // An INTERFACE method has no vtable slot; its implementation is found in
                // the receiver type's own interface table, exactly as a callvirt on an
                // interface does (see TranslateCall). Same bookkeeping too: an interface
                // specialization reached only through this site would otherwise never be
                // emitted, so note it, and register the canonical dispatch. This is the
                // interface twin of the vtable lookup below — reachability already treats
                // ldvirtftn like callvirt (ReachUsedVirtual covers both), so every
                // implementation the delegate can bind to is in the tree.
                // Checked after the GVM case: an interface-declared generic virtual has no
                // interface-table slot either (its VtableSlot is unassigned), and its
                // dispatcher is the right target.
                string expr;
                // The substitute arms, the `ldvirtftn` twin of the `ldftn` ones above. A
                // bounded / dynamic-codegen method's body is cut at reachability, so the
                // vtable (or interface-table) slot the arms below would read is NOT a body —
                // CppEmitter fills an unreached slot with dn2cpp_vcall_unimplemented /
                // dn2cpp_itf_slot_missing, which links fine and aborts on the delegate's
                // first invoke. That is the wrong ANSWER (the call-site mouth neutralizes a
                // bounded call to its default and throws a *catchable* exception for the
                // dynamic-codegen surface) and the wrong ABI (the trap symbols take
                // (Dn2CppObject*) and return void, while the delegate invoker calls the slot
                // as (Dn2CppObject*, <Invoke params>) -> <Invoke return> — on wasm a
                // `call_indirect` type-immediate trap naming nothing).
                //
                // A VIRTUAL-DISPATCH bound (CoreIntrinsics.BdVirtualDispatch — today
                // SynchronizationContext.Post) falls through to the ordinary arms, exactly
                // as its callvirt sites do in MethodCompiler.Call.cs: only the base BODY is
                // cut there, the overrides stay reachable, and taking the address must hand
                // back the receiver's real slot. `ldvirtftn` IS the virtual form, so the
                // `isCallvirt` half of the call-site test is satisfied by construction.
                bool vftnBounded = _c.IsBoundedMethod(m.DeclaringClass.FullName, m.Name)
                    && !(m.IsVirtual
                         && CoreIntrinsics.BdVirtualDispatch.Matches(m.DeclaringClass.FullName, m.Name));
                if (vftnBounded)
                {
                    expr = BoundedFtnStub(m, insn.Offset, receiverSlot: true);
                }
                else if (_c.IsDynamicCodegenMember(m.DeclaringClass, m.Name))
                {
                    expr = DynamicCodegenFtnStub(m, insn.Offset, receiverSlot: true);
                }
                else if (_c.IsAbsentNetworkPalMember(m.DeclaringClass, m.Name))
                {
                    expr = AbsentNetworkPalFtnStub(m, insn.Offset, receiverSlot: true);
                }
                else if (Compilation.IsGvmCall(m))
                {
                    // A generic virtual method has no vtable slot; its type-switch
                    // dispatcher (registered for ldvirtftn too) has the same
                    // (receiver, args) shape, so a delegate binds to it directly.
                    expr = $"(void*)&{Compilation.GvmDispatchName(m)}";
                }
                else if (m.DeclaringClass.IsInterface)
                {
                    if (m.DeclaringClass.IntrinsicCppName is null)
                        NoteReferencedType(m.DeclaringClass);
                    NoteCanonicalItfDispatch(m.DeclaringClass);
                    expr = $"(void*)(dn2cpp_resolve_interface(((Dn2CppObject*)dn2cpp_null_check({obj.Expr}))->type, "
                        + $"&{ItfDispatchTi(m.DeclaringClass).CppTypeInfoName})[{m.VtableSlot}])";
                }
                else
                {
                    // A vtable slot names no symbol; a non-virtual target's address is
                    // the symbol itself, exactly as in the ldftn arm above.
                    if (!m.IsVirtual)
                        _c.NoteNamedBodySymbol(_method, m.Emittable);
                    // The two slot loads take the receiver null guard, like the
                    // callvirt arms they mirror: `ldvirtftn` on a null receiver is
                    // an NRE in .NET, raised where the delegate is BOUND rather
                    // than where it is later invoked, and this is the load that
                    // would otherwise fault. The non-virtual arm takes none — it
                    // names a symbol and reads nothing off the receiver, which is
                    // also why .NET does not check there.
                    expr = m.IsVirtual
                        ? $"(void*)(((Dn2CppObject*)dn2cpp_null_check({obj.Expr}))->type->vtable[{m.VtableSlot}])"
                        : $"(void*)&{m.Emittable.CppName}";
                }
                Push(StackKind.Ptr, "void*", expr);
                break;
            }

            case ILOpCode.Constrained:
                _constrained = ResolveTypeToken(insn.Token);
                break;

            // volatile. — a real seq_cst fence before the following load/store (the
            // multithread model). For a volatile store this gives release ordering
            // (fence-then-store); for a volatile load it forces a re-read and prevents
            // hoisting (fence-then-load). Conservative but correct.
            case ILOpCode.Volatile:
                Emit("__atomic_thread_fence(__ATOMIC_SEQ_CST);");
                break;
            // Prefixes with no effect in our alignment-agnostic model. They precede a
            // real instruction, which is emitted next.
            case ILOpCode.Readonly:
            case ILOpCode.Tail:
            case ILOpCode.Unaligned:
                break;

            case ILOpCode.Call:
            case ILOpCode.Callvirt:
                TranslateCall(insn, isCallvirt: op == ILOpCode.Callvirt);
                _constrained = null;
                break;
            case ILOpCode.Calli:
                TranslateCalli(insn);
                break;
            case ILOpCode.Newobj:
                TranslateNewobj(insn);
                break;

            default:
                throw new NotSupportedException(
                    $"{_method.DeclaringClass.FullName}.{_method.Name}: opcode {op} is not supported yet");
        }
    }

    /// <summary>The C++ ABI a synthesized <c>ldftn</c> stub must be written in — the one
    /// the two shape-preserving arms (a bounded method, the dynamic-codegen surface) share,
    /// because <b>the CONSUMER of the address decides the shape, not the callee's own
    /// signature</b>.
    ///
    /// <para>An <c>ldftn</c> result reaches exactly two consumers. A <b>delegate</b> parks
    /// it in <c>f_method</c>, and <c>CppEmitter.EmitDelegateInvokers</c> calls that slot as
    /// <c>(Dn2CppObject* target, &lt;the delegate Invoke's parameters&gt;)</c> — the target
    /// slot every <c>f_method</c> carries, which is exactly why a static target otherwise
    /// gets a <see cref="DelegateAdapter"/>. A <b>function pointer</b> is invoked by
    /// <c>calli</c>, which passes no target slot and (ECMA-335 aside) cannot carry a
    /// HasThis signature at all — <c>TranslateCalli</c> rejects those — so there the
    /// callee's own parameter list is the shape.</para>
    ///
    /// <para>Writing the callee's parameter list unconditionally would leave a bounded method
    /// taken as a delegate method group one parameter SHORT of the pointer type its invoker
    /// casts to. On a flat native ABI that is invisible — the extra leading argument lands in
    /// a register the stub ignores — so no gate can catch it; on <b>wasm</b> it is fatal and
    /// immediate, since <c>call_indirect</c> carries a type immediate and traps on a
    /// mismatch, naming nothing.</para>
    ///
    /// <para>Reading the delegate's own <c>Invoke</c> — rather than prepending a pointer to
    /// the callee's list — is what also gets a CLOSED static delegate right: there the
    /// delegate's arity is the callee's minus the bound first argument
    /// (<see cref="IsClosedStaticDelegate"/>), and the invoker casts to the delegate's
    /// shape. When the delegate class did not resolve
    /// (<see cref="NewobjTargetIsDelegate"/> reports an unknown class conservatively) the
    /// callee's list plus the target slot is the open-delegate answer.</para>
    ///
    /// <para>The bounded arm's <see cref="BoundedVerdict.Loud"/> stub is built from this same
    /// shape and differs only in its body, which is load-bearing for coverage: no Loud row is
    /// addressable, so the silent stub a gate CAN reach is the only witness this arm's ABI
    /// gets. Keep them built by one call.</para></summary>
    /// <param name="receiverSlot">The address is being taken by <c>ldvirtftn</c>,
    /// where the receiver slot is not a delegate-only convention but the ABI of the vtable /
    /// interface-table entry the arm replaces: every slot is called as
    /// <c>(receiver, &lt;the method's parameters&gt;)</c>. So the slot is unconditional here
    /// and <see cref="_ftnDelegateUse"/> — which is populated for <c>ldftn</c> only — is not
    /// consulted at all. That costs nothing: an instance method bound to a delegate has
    /// Invoke parameters equal to its own (the receiver goes in the target slot, not the
    /// parameter list), and the variance a delegate binding does permit is
    /// reference-to-reference in both directions, which renders to a pointer on either side
    /// and to the same wasm type.</param>
    private (string Params, string RetArrow, bool ReturnsVoid) FtnStubShape(
        MethodInfo m, int ilOffset, bool receiverSlot)
    {
        // Whatever shape the stub takes below, it SPELLS OUT its parameter and return
        // types, so a by-value struct among them names a `t_<T>` the cut behind this stub
        // has just made sure nothing else pulls into the emit set — the same asymmetry the
        // call-site arms ask NoteLocalValueLayout about, reached by the address road. Asked
        // here, at the one funnel all three stub builders (bounded / dynamic-codegen /
        // absent-network-PAL) go through, rather than three times in their callers.
        NoteLocalValueLayout(m.Signature.ReturnType);
        foreach (var sp in m.Signature.ParameterTypes)
            NoteLocalValueLayout(sp);
        if (receiverSlot)
        {
            var rr = m.Signature.ReturnType;
            return ("Dn2CppObject*" + string.Concat(m.Signature.ParameterTypes.Select(p => ", " + CppTypes.Of(p))),
                rr.IsVoid ? "" : $" -> {CppTypes.Of(rr)}", rr.IsVoid);
        }
        if (_ftnDelegateUse.TryGetValue(ilOffset, out var dgClass))
        {
            var invoke = dgClass?.Methods.FirstOrDefault(x => x.Name == "Invoke");
            var dgPs = invoke is not null ? invoke.Signature.ParameterTypes : m.Signature.ParameterTypes;
            var dgRet = invoke is not null ? invoke.Signature.ReturnType : m.Signature.ReturnType;
            string dgParams = "Dn2CppObject*" + string.Concat(dgPs.Select(p => ", " + CppTypes.Of(p)));
            return (dgParams, dgRet.IsVoid ? "" : $" -> {CppTypes.Of(dgRet)}", dgRet.IsVoid);
        }
        var ret = m.Signature.ReturnType;
        return (string.Join(", ", m.Signature.ParameterTypes.Select(CppTypes.Of)),
            ret.IsVoid ? "" : $" -> {CppTypes.Of(ret)}", ret.IsVoid);
    }

    /// <summary>The substitute a BOUNDED method's ADDRESS gets: a stub in the shape its
    /// consumer will call through (<see cref="FtnStubShape"/>), whose body mirrors the
    /// call-site neutralization — the default result, arguments ignored.
    ///
    /// <para><b>One builder, both mouths</b> (<c>ldftn</c> and <c>ldvirtftn</c>).
    /// AGENTS.md's rule for an intercept is that every asker that can reach the member
    /// references the same thing rather than a second copy of it. The two mouths differ in
    /// exactly one argument, the receiver slot.</para>
    ///
    /// <para>The loudness half, mouth 2 of 2 (the other is the call-site neutralization in
    /// MethodCompiler.Call.cs). A Loud import's stub keeps the same ABI as the silent one —
    /// they differ only in their BODY — but invoking it throws the same catchable
    /// PlatformNotSupportedException the call site would, worded by the same helper so the
    /// two cannot drift. The <c>[[noreturn]]</c> throw satisfies the non-void return path.
    /// </para>
    ///
    /// <para>From the <c>ldvirtftn</c> mouth the import half is inert: a <c>[DllImport]</c>
    /// is static and <c>ldvirtftn</c> pops a receiver, so
    /// <see cref="Compilation.TryBoundedImport"/> cannot answer true there. It is still ASKED
    /// — the invariant is asked at every mouth, not assumed, which is what makes this one
    /// builder rather than two.</para></summary>
    private string BoundedFtnStub(MethodInfo m, int ilOffset, bool receiverSlot)
    {
        // The second mouth of the bound's report, so a bounded import reached ONLY
        // as a delegate method group is named too.
        _c.NoteBoundedImport(m);
        var shape = FtnStubShape(m, ilOffset, receiverSlot);
        if (_c.TryBoundedImport(m, out var bimp) && bimp.Verdict == BoundedVerdict.Loud)
        {
            return $"(void*)+[]({shape.Params}){shape.RetArrow} "
                + "{ dn2cpp_throw_platform_not_supported(\""
                + Compilation.BoundedImportThrowMessage(bimp) + "\"); }";
        }
        return shape.ReturnsVoid
            ? $"(void*)+[]({shape.Params}) {{ }}"
            : $"(void*)+[]({shape.Params}){shape.RetArrow} {{ return {{}}; }}";
    }

    /// <summary>The substitute the DYNAMIC-CODEGEN surface's address gets: the same
    /// shape-preserving stub <see cref="BoundedFtnStub"/> builds, whose body is the same
    /// catchable PlatformNotSupportedException the call-site lowering throws. One builder for
    /// both mouths, for the reason stated there.</summary>
    private string DynamicCodegenFtnStub(MethodInfo m, int ilOffset, bool receiverSlot)
    {
        var shape = FtnStubShape(m, ilOffset, receiverSlot);
        return $"(void*)+[]({shape.Params}){shape.RetArrow} "
            + "{ dn2cpp_throw_platform_not_supported("
            + $"\"{_c.GenericDefFullName(m.DeclaringClass)}.{m.Name} requires dynamic code generation\"); }}";
    }

    /// <summary>The substitute the ABSENT-SOCKET-PAL surface's address gets — the same
    /// shape-preserving stub as its two siblings above, throwing the same sentence its call
    /// site does (<see cref="Compilation.AbsentNetworkPalThrowMessage"/>). Both address
    /// mouths need it: a socket method reached only as a method group (a
    /// <c>ConnectCallback</c>, a completion callback) has no body either, and a stub that
    /// returned a default there would put the cut back on the silent side.</summary>
    private string AbsentNetworkPalFtnStub(MethodInfo m, int ilOffset, bool receiverSlot)
    {
        var shape = FtnStubShape(m, ilOffset, receiverSlot);
        return $"(void*)+[]({shape.Params}){shape.RetArrow} "
            + "{ dn2cpp_throw_platform_not_supported(\""
            + Compilation.AbsentNetworkPalThrowMessage(
                _c.GenericDefFullName(m.DeclaringClass), m.Name) + "\"); }";
    }
}

internal static class ImmutableArrayExtensions
{
    public static System.Collections.Immutable.ImmutableArray<byte> ToImmutableArrayCompat(this byte[] bytes) =>
        System.Collections.Immutable.ImmutableArray.Create(bytes);
}
