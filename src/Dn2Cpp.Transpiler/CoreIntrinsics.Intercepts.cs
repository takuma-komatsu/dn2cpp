using System.Reflection.Metadata;

namespace Dn2Cpp;

// THE INTERCEPT DESCRIPTOR REGISTRY — what it guarantees, and what it does not.
//
// Every intercepted BCL member set below is written down here once, as a row: a pure
// predicate plus the cut kind, the emit arm, and (for the scan) the continuation kind.
// Each asker references the row it needs AT ITS OWN CHAIN POSITION — there is no shared
// walk, and unifying the orders would change which line answers a token, and with it the
// emitted bytes. What that buys is one spelling per member set, so two copies cannot
// diverge silently until the C++ linker reports an undefined symbol.
//
// What it does NOT buy:
//
//   * A row does not FORCE both of its askers to reference it. Wiring a new row into
//     only the resolver still compiles and still passes the self-check, re-creating the
//     cut-without-route failure. The registry makes that visible — each row's doc
//     comment names its askers — not impossible.
//   * VerifyInterceptRegistry sees only the tables' own structure. WHICH CALL SITES
//     REFERENCE WHICH ROW lives in Compilation.Resolve.cs, Compilation.Reachability.cs
//     and MethodCompiler.Call.cs, and no walk over an array of rows can decide it.
//   * Not every intercept is a row: the non-pure predicates, the receiver-side
//     route-without-cut tests, the argument-rewriting intercepts, the per-instance
//     halves of the bounded and dynamic-codegen sets, and the reflection-usage marks
//     ScanNeedsParentTypeName carries are each written out at their site with a comment
//     naming their counterpart. The rule for telling them apart is in AGENTS.md and
//     docs/ARCHITECTURE.md §4-B.
//
// What actually CATCHES a cut ⟹ route violation is CppEmitter.AssertCalledBodiesEmitted,
// at transpile time, and only if the corpus walks the orphaned call — a net, not a proof.

/// <summary>What the reachability asker does with a call token a descriptor row
/// matches. The row carries this beside the emit arm so the one-directional
/// invariant between the two askers — cut ⟹ route (AGENTS.md) — is a property of
/// the ROW rather than of two lists kept in step by hand: a member set cannot be
/// cut without the same row naming the arm that lowers its orphaned calls.</summary>
internal enum InterceptCutKind
{
    /// <summary>No reachability action — the row only routes the call at emit; the
    /// real body stays in the tree.
    ///
    /// <para><b>A resolver must NEVER reference a CutKind.None row.</b> Route-without-cut
    /// is mere bloat (a body transpiled that nothing calls); cutting a row that says None is
    /// a SILENT MISCOMPILE, because what these rows route is precisely the lowering whose
    /// real body must survive. <see cref="CoreIntrinsics.MrGcKeepAlive"/> is the case to
    /// hold in mind: cut its body and the liveness barrier GC.KeepAlive exists to provide
    /// disappears — an empty transpiled body is inlinable and -O2 erases it. Nothing fails;
    /// the C++ compiles, links, and collects an object that was supposed to be
    /// pinned.</para></summary>
    None,
    /// <summary>The edge to the real body is deleted (<c>ResolveCallTarget</c>
    /// answers null), so the emit arm must lower every call the cut orphans.</summary>
    Cut,
    /// <summary>The method stays reachable — its vtable slot stays a real
    /// pointer — but its IL is never scanned; the emitter synthesizes the body
    /// in its place.</summary>
    BodyReplace,
    /// <summary>The subtree behind the body is cut and the call site is neutralized or
    /// trapped. Every asker goes through <c>Compilation.IsBoundedMethod</c> — the merge
    /// point, which unions the core row with the backend's
    /// <c>IEmitBackend.AdditionalBoundedMethods</c> and the CLI's <c>--cut</c> specs.
    /// Deliberately NOT <see cref="CoreIntrinsics.IsBoundedMethod"/>: that is the static
    /// core half only, and reading it as the whole bounded set silently under-reports a
    /// backend's cuts.</summary>
    Bounded,
}

/// <summary>What the reachability SCAN (<c>Compilation.ScanBodyForGenerics</c>) does with
/// a call token a row matches, and how its loop continues afterwards. The scan is not the
/// resolver: it walks a body's tokens and, for a handful of members, records a use or
/// reaches something the IL does not name BEFORE deciding whether to ask
/// <c>ResolveCallTarget</c> at all. The two continuations are not interchangeable — one
/// keeps the ordinary call edge, the other replaces it — so the row spells out which rather
/// than leaving it implied by where the <c>continue</c> sits.</summary>
internal enum InterceptScanEffect
{
    /// <summary>The row has no reachability-scan action at all (every row outside
    /// <see cref="CoreIntrinsics.ScanIntercepts"/>).</summary>
    None,
    /// <summary>The scan records the use and then RESOLVES the token as it would
    /// have anyway — the ordinary call edge is untouched. The mark is pure
    /// addition, so a row carrying this continuation never cuts
    /// (<see cref="InterceptCutKind.None"/>).</summary>
    MarkThenResolve,
    /// <summary>The scan performs its effect and SKIPS resolution entirely —
    /// <c>constrained = null; continue;</c>, the scan-side shape of a cut. The
    /// effect may touch Compilation state (reach a type, mark a slot), which is
    /// exactly why the effect BODY stays at the call site: a row is pure, and a
    /// predicate the innermost scan loop asks must stay that way.</summary>
    EffectThenSkipResolve,
}

/// <summary>Which emit arm lowers a matched call. One member per route. The two
/// dispatch switches over this enum — <c>MethodCompiler.TryEmitMethodDefIntercept</c>
/// and <c>MethodCompiler.TryEmitMemberRefIntercept</c> — each carry a case
/// for the arms whose mouth IS that funnel and end in a THROWING DEFAULT, so a row
/// minted without an arm, or routed through the funnel that does not own its arm,
/// fails the first transpile that matches it rather than falling through to
/// <c>EmitManagedCall</c> and naming a body the cut deleted.
///
/// <para>Neither switch is exhaustive over this enum, by design: an arm with no case in
/// either funnel is DOCUMENTARY — it names a mouth outside both (a whole synthesized body,
/// a call-site arm deep inside <c>EmitManagedCall</c>, a position that is itself semantics,
/// a lowering that lives in the C++ runtime) — and the throwing default is what enforces
/// that. Each is marked below.</para>
///
/// <para><see cref="Unspecified"/> is the zero value on purpose. A row constructed without
/// naming an arm must not silently acquire whichever member happens to sit first;
/// <see cref="CoreIntrinsics.VerifyInterceptRegistry"/> rejects it and both funnels'
/// throwing default catches it.</para></summary>
internal enum InterceptEmitArm
{
    /// <summary>No arm named — never valid on a row, and the zero value for exactly
    /// that reason: a default-initialized arm must be REJECTED rather than aliasing a
    /// real route. Asserted by <see cref="CoreIntrinsics.VerifyInterceptRegistry"/>.</summary>
    Unspecified,
    /// <summary><c>MethodCompiler.EmitRuntimePrimitive</c> — total over
    /// <see cref="CoreIntrinsics.LoweredRuntimePrimitive"/>: an unmodeled shape
    /// leaves through its throw, never through <c>EmitManagedCall</c>.</summary>
    RuntimePrimitive,
    /// <summary><c>MethodCompiler.TryEmitEventSourceNoOp</c> — EventSource.IsEnabled
    /// folds to false (the provider-member fold behind the same method stays a
    /// separate, non-pure route: it needs the ClassInfo).</summary>
    EventSourceNoOp,
    /// <summary><c>MethodCompiler.TryEmitRegexCompileFold</c> — a dead null
    /// RegexRunnerFactory.</summary>
    RegexCompileFold,
    /// <summary><c>MethodCompiler.TryEmitConstFoldedGetter</c> — the call site
    /// pushes the folded constant.</summary>
    ConstFoldedGetter,
    /// <summary><c>MethodCompiler.TryEmitConstFoldedStringCall</c> — the call site
    /// discards the arguments and pushes the folded string literal
    /// (<see cref="CoreIntrinsics.ConstFoldedStringCall"/>). Distinct from
    /// <see cref="ConstFoldedGetter"/> because that one pushes an int32 and pops
    /// nothing: these are not zero-argument getters.</summary>
    ConstFoldedStringCall,
    /// <summary>SerializationInfo.ThrowIfDeserializationInProgress — discard the
    /// arguments; no BinaryFormatter deserialization ever runs here.</summary>
    DeserializationGuardNoOp,
    /// <summary>The System.Environment surface — <c>EmitIntrinsic</c> under the
    /// literal type key.</summary>
    EnvIntrinsic,
    /// <summary>The sub-word integer members of
    /// <see cref="CoreIntrinsics.IsInlineLoweredPrimitiveMember"/> —
    /// <c>EmitIntrinsic</c> under the declaring type.</summary>
    InlinePrimitive,
    /// <summary>System.Collections.Comparer.Compare — <c>EmitIntrinsic</c> (lowers
    /// to dn2cpp_object_compare). TWO mouths share this arm: the call site (the
    /// MethodDef/MemberRef funnels) and the BODY, which <c>CppEmitter</c>
    /// synthesizes from the same intrinsic lowering (<c>CompileCoreIntrinsicWrapper</c>)
    /// so the IComparer.Compare interface slot points at the boxed-object order.
    /// One arm because it is one lowering, reached twice.</summary>
    ComparerCompare,
    /// <summary>ExecutionContext.Capture() — push the null "nothing to flow"
    /// encoding every consumer already handles.</summary>
    ExecutionContextCaptureNull,
    /// <summary>SynchronizationContext's thread-slot statics —
    /// <c>EmitIntrinsic</c> under the literal type key (the per-thread
    /// dn2cpp_sync_ctx_get/set slot).</summary>
    SyncContextSlot,
    /// <summary>A member of an intrinsic-mapped type
    /// (<see cref="CoreIntrinsics.IsIntrinsicType"/>) — <c>EmitIntrinsic</c>
    /// under the declaring type.</summary>
    IntrinsicDispatch,
    /// <summary>MemoryExtensions.ToUpperInvariant/ToLowerInvariant over char
    /// spans (<see cref="CoreIntrinsics.LoweredSpanCaseFold"/>) — the runtime
    /// BMP invariant fold (dn2cpp_span_case_invariant), built inline at the
    /// call site.</summary>
    SpanCaseFold,
    /// <summary>The System.IO.Path / System.IO.File overloads a dn2cpp_path_* /
    /// dn2cpp_file_* helper models (<see cref="CoreIntrinsics.LoweredIoMember"/>)
    /// — <c>EmitIoIntrinsic</c>. The row is boolean; WHICH lowering to emit is
    /// re-asked from the predicate by the arm, whose second call reads the
    /// caller's memoized signature thunk, not a second decode.</summary>
    IoIntrinsic,
    /// <summary>The WIDE integer primitives' (Int32/UInt32/Int64/UInt64)
    /// TryFormat belt — <c>EmitIntrinsic</c> under the declaring type.</summary>
    WideTryFormat,
    /// <summary><c>EmitIntrinsic(declType, name, Sig())</c> — the many BCL
    /// members lowered inline to the intrinsic table under their own declaring
    /// type (SynchronizationContext slot statics, NativeMemory, Marshal, the
    /// non-generic MemoryMarshal.GetArrayDataReference, Buffer.BlockCopy,
    /// AppContext.BaseDirectory, Directory). Their emit is byte-identical, so
    /// one arm serves them all.</summary>
    IntrinsicUnderDeclType,
    /// <summary><c>EmitIntrinsic(declType, name, mr.DecodeMethodSignature(
    /// SigProvider, null))</c> — the Enum reflection statics and
    /// Nullable.GetUnderlyingType, which decode their signature in the NULL
    /// generic context (not the caller's memoized thunk), exactly as the
    /// pre-descriptor call sites did.</summary>
    IntrinsicUnderDeclTypeNullCtx,
    /// <summary>The System.GC surface lowered inline to the dn2cpp_gc_* /
    /// dn2cpp_keep_alive helpers — the arm dispatches by member name+shape over
    /// the whole family (SuppressFinalize / ReRegisterForFinalize / Collect /
    /// WaitForPendingFinalizers / GetTotalMemory / GetAllocatedBytesForCurrentThread /
    /// GetTotalAllocatedBytes / KeepAlive). Shared by the cut
    /// row (<see cref="CoreIntrinsics.MrGc"/>) and the emit-only KeepAlive row
    /// (<see cref="CoreIntrinsics.MrGcKeepAlive"/> — route-without-cut).</summary>
    GcFamily,
    /// <summary>Object.Finalize() — the base call a C# destructor's finally block
    /// emits; a pure no-op, pop the receiver and discard.</summary>
    ObjectFinalizeNoOp,
    /// <summary>Encoding.GetString — <c>TryEmitEncodingGetString</c>, which is
    /// TOTAL over the modeled decoder shapes (an unmodeled shape leaves through
    /// its own throw, never a fall-through), so the arm cannot decline.</summary>
    EncodingGetString,
    /// <summary>A generic method lowered inline —
    /// <c>MethodCompiler.TranslateGenericIntrinsic(parentTypeName, methodName,
    /// specHandle)</c>. Every MethodSpecification-arm row routes here; the arm
    /// discriminates on the same (parent type, method name) pair the row matched
    /// on, so one arm serves them all.
    ///
    /// <para>DOCUMENTARY: the MethodSpecification mouth is its own arm of
    /// <c>MethodCompiler.TranslateCall</c>'s token-shape switch, not one of the two
    /// descriptor funnels, so this arm has no case in either and routing a row
    /// through one hits its throwing default.</para></summary>
    GenericIntrinsic,
    /// <summary>An integer primitive's TryFormat reached through a constrained or
    /// boxed dispatch — <c>MethodCompiler.TryEmitValueConstrained</c>, which routes
    /// it back to the concrete integer type's <c>EmitIntrinsic</c> TryFormat case.
    /// TOTAL over <see cref="CoreIntrinsics.LoweredIntegerTryFormat"/>: the reach
    /// cut is by NAME over all eight widths, so an unmodeled overload SHAPE leaves
    /// through the arm's throw, never through the ordinary constrained call.
    ///
    /// <para>DOCUMENTARY: <c>TryEmitValueConstrained</c> runs ahead of the token-shape
    /// switch (it must consume the pending <c>constrained.</c> prefix), so this arm has
    /// no case in either descriptor funnel.</para></summary>
    ConstrainedTryFormat,
    /// <summary>A boxed sub-word integer's ToString slot. There is no transpiler-side route
    /// and there cannot be one: the value is formatted by the C++ runtime's
    /// <c>dn2cpp_object_tostring</c> straight from the type-info, which is why the
    /// handwritten <c>dn2cpp_*_type</c> carry a nullptr tostring slot.
    ///
    /// <para>DOCUMENTARY: the lowering is in the C++ runtime, not in the transpiler at
    /// all.</para></summary>
    BoxedToStringSlot,
    /// <summary>An enum's <c>ISpanFormattable.TryFormat</c> reached through a constrained
    /// or boxed dispatch. The BOXED mouth (an interface slot on a boxed enum —
    /// <c>EnumConverter.ConvertTo</c>) is a plain-name cut: the impl is
    /// <c>System.Enum.System.ISpanFormattable.TryFormat</c>, so cutting its reach edge
    /// leaves the enum's ISpanFormattable itable slot a trap stub (unreached impls degrade
    /// to <c>dn2cpp_itf_slot_missing[_named]</c> — CppEmitter.TypeMetadataEmitter), never a
    /// dangling symbol, exactly as the integer boxed cut does. The CONSTRAINED mouth
    /// (<c>{enum}</c> interpolation lowered through DefaultInterpolatedStringHandler —
    /// <c>constrained. &lt;enum&gt; callvirt ISpanFormattable::TryFormat</c>) IS routed:
    /// <c>MethodCompiler.TryEmitValueConstrained</c> boxes the enum and formats it via
    /// <c>dn2cpp_enum_format</c> + <c>dn2cpp_string_try_copy_to_span</c>. The reach body
    /// pulls in Enum.TryFormatPrimitiveDefault → GetEnumInfo → GetEnumValuesAndNames (an
    /// InternalCall with no IL), so it must not enter the tree on either mouth.
    ///
    /// <para>DOCUMENTARY: like <see cref="ConstrainedTryFormat"/>, the constrained route
    /// lives in <c>TryEmitValueConstrained</c> ahead of the token-shape switch, and the
    /// boxed mouth has no transpiler route at all (the itable trap stub is its degrade), so
    /// this arm has no case in either descriptor funnel.</para></summary>
    ConstrainedEnumTryFormat,

    // The three arms below belong to the SCAN rows. Each names a mouth deliberately NOT
    // routed through the registry, so the arm is documentary — it records where the
    // lowering lives, which keeps a scan-side cut from reading as a cut with nothing
    // behind it.

    /// <summary>Enum.HasFlag — lowered inline to a bit test at the TOP of
    /// <c>MethodCompiler.TranslateCall</c>, ahead of the constrained handling and
    /// carrying a <c>_constrained = null</c> side effect. That POSITION is
    /// semantics — the arm must run before anything else can consume the pending
    /// prefix — so the emit mouth stays written out there rather than becoming a
    /// registry reference; this arm names it.</summary>
    EnumHasFlagBitTest,
    /// <summary>CultureInfo.get_CompareInfo — a synthesized zero-initialized
    /// CompareInfo (<c>MethodCompiler.EmitIntrinsic</c>, the Numbers arm). The scan
    /// row's job is the other half: marking that CompareInfo allocated, because the
    /// IL names no ctor to reach (the real one reads fields of the intrinsic-mapped
    /// CultureInfo).</summary>
    CultureCompareInfoSynth,
    /// <summary>An Object/ValueType-rooted Equals/GetHashCode — lowered to
    /// <c>dn2cpp_object_equals</c> / <c>dn2cpp_object_gethashcode</c>, which answer
    /// from the type-info slots. The scan row marks the dispatch used
    /// (<c>NoteObjectEqualityDispatch</c>): this pair's declaring type is intrinsic,
    /// so it owns no vtable slot to mark instead. The row's non-generic
    /// <c>System.Collections.IEqualityComparer</c> arm names a second mouth: the
    /// interface dispatch in <c>MethodCompiler.EmitManagedCall</c> answers a null
    /// receiver (the <c>EqualityComparer&lt;T&gt;.Default</c> sentinel escaped as an
    /// object — Tuple's structural trio) with these same helpers.</summary>
    ObjectEqualityHelper,

    // The arms below belong to the BodyReplace/Bounded rows. None is a case in the two
    // emit funnels, deliberately: each lowers at a site the funnels never reach — a whole
    // synthesized BODY, or a call-site arm sitting in EmitManagedCall well past the
    // token-shape switch. Routing one of these rows through a funnel hits its throwing
    // default, as intended.

    /// <summary>A synthesized System.Net.Http body —
    /// <c>MethodCompiler.CompileHttpShimBody</c>, emitted under the intercepted
    /// method's OWN symbol so a virtual slot (<c>CppEmitter.RenderVtable</c>) points
    /// at a real body. Which body (the DnHttp transport forward, the
    /// client-certificate refusal, or its constant-Manual getter twin) is decided by
    /// re-asking the same two pure predicates the row's own <c>Extra</c> is built
    /// from — never a separate list.</summary>
    HttpShimBody,
    /// <summary>A synthesized System.Enum instance ToString/GetTypeCode body —
    /// <c>MethodCompiler.CompileEnumInstanceFormatBody</c>, emitted under the
    /// intercepted method's OWN symbol so EVERY mouth resolves to it: a direct
    /// <c>callvirt System.Enum::ToString()</c> on a System.Enum-typed receiver, a
    /// <c>System.IFormattable/IConvertible</c> itable slot on a boxed enum
    /// (<c>CppEmitter.RenderInterfaceTable</c>), and a boxed constrained dispatch.
    /// BodyReplace, not Cut, precisely because the itable slot points at the real body's
    /// symbol — a Cut would leave it a trap stub and the interface-cast form would fail. The
    /// real IL (ToStringInlined -> GetEnumInfo -> GetEnumValuesAndNames, an InternalCall,
    /// plus the RuntimeType-cache cascade) is never scanned. The body itself calls only
    /// runtime helpers
    /// (<c>dn2cpp_object_tostring</c> / <c>dn2cpp_enum_format</c> /
    /// <c>dn2cpp_type_get_type_code</c>), which dispatch on the boxed enum's runtime
    /// type, so one synthesized body serves every enum.</summary>
    EnumInstanceFormatBody,
    /// <summary>A bounded method's call site: drop the arguments and push the
    /// default result (null / zero / nothing-for-void) —
    /// <c>MethodCompiler.EmitManagedCall</c>. The ldftn mouth
    /// (<c>MethodCompiler</c>'s address-taken arm) mirrors it with a
    /// shape-preserving no-op stub, because a cut body has no symbol to
    /// address.</summary>
    BoundedNeutralize,
    /// <summary>A bounded VIRTUAL whose callvirt sites are NOT neutralized: only the
    /// base body is cut, and the call falls through to the ordinary virtual-dispatch
    /// emission so a user override still runs. Not a second route so much as the
    /// absence of one — which is exactly why it is a row: the carve-out is a
    /// property of the intercepted member set, and stating it beside
    /// <see cref="BoundedNeutralize"/> is what keeps the two from being read as one
    /// rule.</summary>
    BoundedVirtualDispatch,
    /// <summary>The base Stream async funnels: bounded body, but the call site is
    /// REWRITTEN rather than neutralized —
    /// <c>MethodCompiler.TryEmitStreamSyncOverAsyncFunnel</c> dispatches
    /// synchronously through the <c>Read</c>/<c>Write</c> slot and hands back a
    /// completed Task. Its reach half is a second edge, not a cut: the slot the
    /// rewrite dispatches through has to be REACHED, and in a program that only ever
    /// awaits its stream nothing else names it.</summary>
    StreamSyncOverAsyncRewrite,
    /// <summary>The dynamic-code-generation surface: the call site emits
    /// <c>dn2cpp_throw_platform_not_supported</c> naming the member (the NativeAOT
    /// posture), and the ldftn mouth a stub that throws the same. Unlike
    /// <see cref="BoundedNeutralize"/> this must fail LOUDLY — reaching it at run
    /// time is a real unsupported-feature error, not runtime-dead plumbing.</summary>
    DynamicCodegenThrow,
    /// <summary>The absent socket / name-resolution platform layer: every call /
    /// ldftn / ldvirtftn / newobj site emits
    /// <c>dn2cpp_throw_platform_not_supported</c> naming the member
    /// (<c>Compilation.AbsentNetworkPalThrowMessage</c>). Loud for the same reason
    /// <see cref="DynamicCodegenThrow"/> is — a caller that wants a connected socket
    /// cannot proceed without one, so the zero <see cref="BoundedNeutralize"/> would
    /// push is a lie it would then dereference — and loud for one reason more: the
    /// alternative to cutting this surface is not a wrong answer but an UNDEFINED
    /// SYMBOL at C++ link time, because <c>libSystem.Native</c> is a runtime-provided
    /// P/Invoke module and nothing implements its socket half.
    ///
    /// <para>DOCUMENTARY, like the five arms above it: the lowering is a call-site arm
    /// in <c>MethodCompiler.EmitManagedCall</c> plus the three address/allocation
    /// mouths, none of which is one of the two descriptor funnels.</para></summary>
    AbsentNetworkPalThrow,
}

/// <summary>One MethodDefinition-arm intercept: the single truth source both askers
/// consult about one intercepted member set. The reachability cut
/// (<c>Compilation.ResolveCallTarget</c>) and the emit route
/// (<c>MethodCompiler.TranslateCall</c>) each test <see cref="Matches"/> on the SAME
/// row, so the member set cannot drift between them — the "ask ONE pure predicate from
/// both askers" rule (AGENTS.md) as a struct instead of a discipline.
///
/// <para>A row is referenced by each asker AT ITS OWN CHAIN POSITION; there is no shared
/// walk order. The two chains order their tests differently and the order is load-bearing —
/// the reach chain's intrinsic-type test preempts rows the emit chain must still ask — so
/// hoisting both askers onto one ordered walk would change which line answers a token, and
/// with it the decode trace and the emitted bytes.</para>
///
/// <para>Gate discipline: <see cref="TypeGate"/>/<see cref="NameGate"/> are plain strings
/// compared BEFORE <see cref="Extra"/> is invoked, because the askers sit in the innermost
/// per-call-token loops — a row must cost string compares, not a delegate call, for the
/// overwhelming majority of tokens. <see cref="Extra"/> must be a static lambda, so the
/// delegate is allocated once at class init and a row is stateless; a per-call closure over
/// the <c>MethodInfo</c> ARGUMENT (a signature thunk) is fine, since it is confined to
/// tokens the gates already admitted. <see cref="Extra"/> may read <c>mi.Signature</c> only
/// behind those gates: a signature read is a decode, and a decode mints the closed generics
/// the signature names (AGENTS.md).</para>
///
/// <para>MemberReference-arm rows get a context type of their own — (declaring-type name,
/// member name, per-asker signature thunk): that arm has no MethodInfo, and its two askers
/// must keep their distinct memoized thunks (reach decodes under
/// <c>GenericContext.Empty</c>, emit under <c>_method.Context</c>).</para></summary>
internal readonly struct MethodDefIntercept
{
    /// <summary>What the reachability asker does with a matched token.</summary>
    public readonly InterceptCutKind CutKind;

    /// <summary>The emit arm that lowers a matched call.</summary>
    public readonly InterceptEmitArm EmitArm;

    /// <summary>Declaring-type full name, or null when the member set spans types
    /// (then <see cref="Extra"/> carries the whole test, e.g. a shared
    /// name-keyed predicate).</summary>
    public readonly string? TypeGate;

    /// <summary>Method name, or null when the set has several (then
    /// <see cref="Extra"/> tests the names).</summary>
    public readonly string? NameGate;

    /// <summary>The rest of the pure predicate — a static lambda, invoked only
    /// after both gates pass. Null when the gates ARE the whole test.</summary>
    public readonly Func<MethodInfo, bool>? Extra;

    public MethodDefIntercept(InterceptCutKind cutKind, InterceptEmitArm emitArm,
        string? typeGate = null, string? nameGate = null, Func<MethodInfo, bool>? extra = null)
    {
        CutKind = cutKind;
        EmitArm = emitArm;
        TypeGate = typeGate;
        NameGate = nameGate;
        Extra = extra;
    }

    /// <summary>The one question both askers ask. Pure: reads names and flags off
    /// the MethodInfo (and, behind the gates, its signature) — never Compilation
    /// state, which is what keeps it askable from the reachability scan.</summary>
    public bool Matches(MethodInfo mi)
    {
        if (TypeGate is not null && mi.DeclaringClass.FullName != TypeGate)
            return false;
        if (NameGate is not null && mi.Name != NameGate)
            return false;
        return Extra is null || Extra(mi);
    }
}

/// <summary>One MemberReference-arm intercept: the single truth source both askers
/// consult about one intercepted member set, exactly like
/// <see cref="MethodDefIntercept"/> — but the MemberReference arm has no
/// <c>MethodInfo</c>, so its context is (declaring-type full name, member name,
/// signature thunk). The thunk is each asker's own per-token memoized thunk, passed
/// through untouched: reach decodes under <c>GenericContext.Empty</c>, emit under
/// <c>_method.Context</c>, and the two must stay distinct — a signature read is a
/// decode, and a decode mints the closed generics the signature names (AGENTS.md),
/// so unifying the thunks would move decodes between the askers.
///
/// <para>Rows are referenced by each asker AT ITS OWN CHAIN POSITION, never via a
/// shared walk — the same contract as the MethodDefinition arm (see
/// <see cref="MethodDefIntercept"/> on why the chain orders must not be
/// unified).</para>
///
/// <para>Gate discipline as on the MethodDefinition rows:
/// <see cref="TypeGate"/>/<see cref="NameGate"/> are plain strings compared BEFORE
/// <see cref="Extra"/> is invoked, <see cref="Extra"/> must be a static lambda, and
/// it may call the signature thunk only behind a name test (its own or the
/// gates').</para></summary>
internal readonly struct MemberRefIntercept
{
    /// <summary>What the reachability asker does with a matched token.
    /// <see cref="InterceptCutKind.None"/> marks a route-without-cut row — the
    /// emitter lowers the call but the real body stays in the tree — which the
    /// resolver must NOT reference.</summary>
    public readonly InterceptCutKind CutKind;

    /// <summary>The emit arm that lowers a matched call.</summary>
    public readonly InterceptEmitArm EmitArm;

    /// <summary>Declaring-type full name, or null when the member set spans
    /// types (then <see cref="Extra"/> carries the type test).</summary>
    public readonly string? TypeGate;

    /// <summary>Member name, or null when the set has several (then
    /// <see cref="Extra"/> tests the names).</summary>
    public readonly string? NameGate;

    /// <summary>The rest of the pure predicate — a static lambda over
    /// (declaring-type name, member name, signature thunk), invoked only after
    /// both gates pass. Null when the gates ARE the whole test.</summary>
    public readonly Func<string?, string, Func<MethodSignature<TypeDesc>>, bool>? Extra;

    public MemberRefIntercept(InterceptCutKind cutKind, InterceptEmitArm emitArm,
        string? typeGate = null, string? nameGate = null,
        Func<string?, string, Func<MethodSignature<TypeDesc>>, bool>? extra = null)
    {
        CutKind = cutKind;
        EmitArm = emitArm;
        TypeGate = typeGate;
        NameGate = nameGate;
        Extra = extra;
    }

    /// <summary>The one question both askers ask. Pure — names and, behind the
    /// gates, the thunked signature; never Compilation state.</summary>
    public bool Matches(string? declType, string name, Func<MethodSignature<TypeDesc>> sig)
    {
        if (TypeGate is not null && declType != TypeGate)
            return false;
        if (NameGate is not null && name != NameGate)
            return false;
        return Extra is null || Extra(declType, name, sig);
    }
}

/// <summary>One intercept whose whole question is a (declaring-type name, member
/// name) pair — the third descriptor shape, serving the askers that never
/// hold a <c>MethodInfo</c> and never need a signature: the MethodSpecification arm,
/// the constrained/virtual arm, and the reachability scan. Same contract as
/// <see cref="MethodDefIntercept"/>/<see cref="MemberRefIntercept"/>: both askers
/// test the SAME row, so the intercepted member set cannot drift between the
/// reachability cut and the emit route.
///
/// <para><b>What the pair MEANS differs per table, and the difference is
/// load-bearing.</b> In <see cref="CoreIntrinsics.MethodSpecIntercepts"/> it is read off
/// the TOKEN — the MethodSpec's parent type name and method name, before any resolution. In
/// <see cref="CoreIntrinsics.ConstrainedVirtualIntercepts"/> it is read off the RESOLVED
/// IMPL, because those askers only have a question once the slot has been bound. In
/// <see cref="CoreIntrinsics.ScanIntercepts"/> it is read off the RAW TOKEN under both
/// handle kinds a scanned body can name these members with (MemberReference and
/// MethodDefinition), before resolution, because for two of its three rows the whole point
/// is that resolution never happens. A row therefore belongs to exactly one table; asking a
/// MethodSpec row about a resolved impl is a category error even when the strings line
/// up.</para>
///
/// <para>Gate discipline as on the other two shapes: <see cref="TypeGate"/> and
/// <see cref="NameGate"/> are plain strings compared BEFORE <see cref="Extra"/> is
/// invoked, and <see cref="Extra"/> must be a static lambda. There is no signature
/// thunk here at all — nothing on either arm may decode from a row, which is what
/// keeps these rows askable from the innermost reachability scan.</para></summary>
internal readonly struct NameKeyedIntercept
{
    /// <summary>What the reachability asker does with a matched pair.</summary>
    public readonly InterceptCutKind CutKind;

    /// <summary>The emit arm that lowers a matched call.</summary>
    public readonly InterceptEmitArm EmitArm;

    /// <summary>What the reachability SCAN does with a matched token, and how its
    /// loop continues afterwards. <see cref="InterceptScanEffect.None"/> on every
    /// row outside <see cref="CoreIntrinsics.ScanIntercepts"/> — the scan is a third
    /// asker, not a rename of the cut.</summary>
    public readonly InterceptScanEffect ScanEffect;

    /// <summary>Declaring-type full name, or null when the member set spans types
    /// (then <see cref="Extra"/> carries the type test).</summary>
    public readonly string? TypeGate;

    /// <summary>Member name, or null when the set has several (then
    /// <see cref="Extra"/> tests the names).</summary>
    public readonly string? NameGate;

    /// <summary>The rest of the pure predicate — a static lambda over
    /// (declaring-type name, member name), invoked only after both gates pass.
    /// Null when the gates ARE the whole test.</summary>
    public readonly Func<string?, string, bool>? Extra;

    public NameKeyedIntercept(InterceptCutKind cutKind, InterceptEmitArm emitArm,
        string? typeGate = null, string? nameGate = null,
        Func<string?, string, bool>? extra = null,
        InterceptScanEffect scanEffect = InterceptScanEffect.None)
    {
        CutKind = cutKind;
        EmitArm = emitArm;
        TypeGate = typeGate;
        NameGate = nameGate;
        Extra = extra;
        ScanEffect = scanEffect;
    }

    /// <summary>The one question both askers ask. Pure — two names and, behind the
    /// gates, whatever static table <see cref="Extra"/> consults; never Compilation
    /// state and never a decode.</summary>
    public bool Matches(string? declType, string name)
    {
        if (TypeGate is not null && declType != TypeGate)
            return false;
        if (NameGate is not null && name != NameGate)
            return false;
        return Extra is null || Extra(declType, name);
    }
}

internal static partial class CoreIntrinsics
{
    /// <summary>The intra-CoreLib runtime primitives
    /// (<see cref="LoweredRuntimePrimitive"/>) — cut by NAME over whole overload
    /// families; the arm is total over the predicate, so an unmodeled shape
    /// throws instead of naming a body the cut deleted.</summary>
    public static readonly MethodDefIntercept MdRuntimePrimitive = new(
        InterceptCutKind.Cut, InterceptEmitArm.RuntimePrimitive,
        extra: static mi => LoweredRuntimePrimitive(mi.DeclaringClass.FullName, mi.Name));

    /// <summary>EventSource.IsEnabled() folds to false, so the framework's
    /// `if (Log.IsEnabled()) Log.Event(…)` guards go dead and the EventSource
    /// manifest/finalizer cascade stays out of the tree. Only the bool-returning
    /// overloads: the sig read sits behind the gates.</summary>
    public static readonly MethodDefIntercept MdEventSourceIsEnabled = new(
        InterceptCutKind.Cut, InterceptEmitArm.EventSourceNoOp,
        typeGate: "System.Diagnostics.Tracing.EventSource", nameGate: "IsEnabled",
        extra: static mi => mi.Signature.ReturnType is
            { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Boolean });

    /// <summary>Regex.Compile folds to a dead null RegexRunnerFactory (its
    /// dynamic-code guard is const-folded false — the IL2CPP/NativeAOT posture),
    /// keeping RegexCompiler and the Reflection.Emit closure out of the tree.</summary>
    public static readonly MethodDefIntercept MdRegexCompileFold = new(
        InterceptCutKind.Cut, InterceptEmitArm.RegexCompileFold,
        typeGate: "System.Text.RegularExpressions.Regex", nameGate: "Compile",
        extra: static mi => mi.IsStatic);

    /// <summary>Const-folded capability getters (<see cref="ConstFoldedGetter"/>):
    /// the call site pushes the constant, so the edge must not exist.</summary>
    public static readonly MethodDefIntercept MdConstFoldedGetter = new(
        InterceptCutKind.Cut, InterceptEmitArm.ConstFoldedGetter,
        extra: static mi => ConstFoldedGetter(mi.DeclaringClass.FullName, mi.Name) is not null);

    /// <summary>Const-folded string-returning calls
    /// (<see cref="ConstFoldedStringCall"/>): the call site pushes the constant, so the
    /// edge must not exist. Both members are Windows-CoreLib TimeZoneInfo internals whose
    /// real bodies walk into User32.LoadString and CultureInfo.Parent — the globalization
    /// carve-out — and neither can be closed by admitting the module: the fold IS what
    /// invariant-globalization .NET answers. No TypeGate, matching MdConstFoldedGetter:
    /// the member set lives in the table, and a gate naming one type would become a lie
    /// the moment the table holds two.</summary>
    public static readonly MethodDefIntercept MdConstFoldedStringCall = new(
        InterceptCutKind.Cut, InterceptEmitArm.ConstFoldedStringCall,
        extra: static mi => mi.IsStatic
            && IsConstFoldedStringCallName(mi.Name)
            && ConstFoldedStringCall(mi.DeclaringClass.FullName, mi.Name) is not null);

    /// <summary>SerializationInfo.ThrowIfDeserializationInProgress is a no-op —
    /// no BinaryFormatter deserialization ever runs in a dn2cpp binary, and the
    /// real body pulls AsyncLocal&lt;bool&gt; → ExecutionContext.GetLocalValue →
    /// Thread internals.</summary>
    public static readonly MethodDefIntercept MdDeserializationGuard = new(
        InterceptCutKind.Cut, InterceptEmitArm.DeserializationGuardNoOp,
        typeGate: "System.Runtime.Serialization.SerializationInfo",
        nameGate: "ThrowIfDeserializationInProgress");

    /// <summary>The System.Environment overloads a dn2cpp_env_* helper models
    /// (<see cref="LoweredEnvMember"/>). The TypeGate duplicates the predicate's
    /// TYPE test on purpose: it keeps every non-Environment token from paying for
    /// the sig-thunk closure, and a duplicated type name cannot drift — a
    /// duplicated MEMBER SET is what can.</summary>
    public static readonly MethodDefIntercept MdEnvMember = new(
        InterceptCutKind.Cut, InterceptEmitArm.EnvIntrinsic,
        typeGate: "System.Environment",
        extra: static mi => LoweredEnvMember(mi.DeclaringClass.FullName, mi.Name, () => mi.Signature));

    /// <summary>The sub-word integers' ToString/Parse/TryParse/TryFormat
    /// (<see cref="IsInlineLoweredPrimitiveMember"/>), lowered inline at every
    /// call site so the System.Number format/parse cascade stays out of the
    /// tree.</summary>
    public static readonly MethodDefIntercept MdInlinePrimitive = new(
        InterceptCutKind.Cut, InterceptEmitArm.InlinePrimitive,
        extra: static mi => IsInlineLoweredPrimitiveMember(mi.DeclaringClass.FullName, mi.Name));

    /// <summary>System.Collections.Comparer.Compare(object, object)
    /// (<see cref="LoweredComparerCompare"/>) — body-intercepted to
    /// dn2cpp_object_compare.</summary>
    public static readonly MethodDefIntercept MdComparerCompare = new(
        InterceptCutKind.Cut, InterceptEmitArm.ComparerCompare,
        extra: static mi => LoweredComparerCompare(mi.DeclaringClass.FullName, mi.Name));

    /// <summary>ExecutionContext.Capture() lowers to the null "nothing to flow"
    /// encoding; its real body reads Thread._executionContext on the intrinsic
    /// Thread model.</summary>
    public static readonly MethodDefIntercept MdExecutionContextCapture = new(
        InterceptCutKind.Cut, InterceptEmitArm.ExecutionContextCaptureNull,
        typeGate: "System.Threading.ExecutionContext", nameGate: "Capture",
        extra: static mi => mi.Signature.ParameterTypes.Length == 0);

    /// <summary>SynchronizationContext's thread-slot statics — get_Current /
    /// SetSynchronizationContext read/write the per-thread runtime slot; the
    /// real bodies touch Thread._synchronizationContext on the intrinsic Thread
    /// model.</summary>
    public static readonly MethodDefIntercept MdSyncContextSlot = new(
        InterceptCutKind.Cut, InterceptEmitArm.SyncContextSlot,
        typeGate: "System.Threading.SynchronizationContext",
        extra: static mi => mi.Name is "get_Current" or "SetSynchronizationContext");

    /// <summary>The non-generic MemoryMarshal.GetArrayDataReference(Array) reached
    /// INTRA-CoreLib — Marshal.UnsafeAddrOfPinnedArrayElement and the ArrayMarshaller
    /// pinning shims call it this way, and its own body is RuntimeHelpers.GetMethodTable
    /// BaseSize pointer math with no mapping, so an uncut edge fails the transpile loudly.
    /// Same member set and lowering as <see cref="MrMemoryMarshalArrayData"/>; two rows
    /// because the two askers hold different context (a MethodInfo here, a name triple
    /// there). MemoryMarshal is not an intrinsic-mapped type, so
    /// <see cref="MdIntrinsicType"/> does not answer this token.</summary>
    public static readonly MethodDefIntercept MdMemoryMarshalArrayData = new(
        InterceptCutKind.Cut, InterceptEmitArm.IntrinsicUnderDeclType,
        typeGate: "System.Runtime.InteropServices.MemoryMarshal", nameGate: "GetArrayDataReference",
        extra: static mi => mi.Signature.GenericParameterCount == 0);

    /// <summary>A member of an intrinsic-mapped type
    /// (<see cref="IsIntrinsicType"/>): emitted inline, never transpiled, so an
    /// intra-assembly call into one is not a reachability edge.</summary>
    public static readonly MethodDefIntercept MdIntrinsicType = new(
        InterceptCutKind.Cut, InterceptEmitArm.IntrinsicDispatch,
        extra: static mi => IsIntrinsicType(mi.DeclaringClass.FullName));

    /// <summary>Every MethodDefinition-arm row — a REGISTRY, not a chain: the
    /// order here carries no meaning, because each asker references the rows it
    /// needs at its own chain position (see <see cref="MethodDefIntercept"/> on
    /// why the two chain orders must not be unified).</summary>
    public static readonly MethodDefIntercept[] MethodDefIntercepts =
    [
        MdRuntimePrimitive,
        MdEventSourceIsEnabled,
        MdRegexCompileFold,
        MdConstFoldedGetter,
        MdConstFoldedStringCall,
        MdDeserializationGuard,
        MdEnvMember,
        MdInlinePrimitive,
        MdComparerCompare,
        MdExecutionContextCapture,
        MdSyncContextSlot,
        MdMemoryMarshalArrayData,
        MdIntrinsicType,
    ];

    /// <summary>First row in <paramref name="rows"/> matching
    /// <paramref name="mi"/>, or -1 — for an asker whose chain segment is a
    /// contiguous run of rows in one order.</summary>
    public static int FirstMethodDefMatch(MethodDefIntercept[] rows, MethodInfo mi)
    {
        for (int i = 0; i < rows.Length; i++)
            if (rows[i].Matches(mi))
                return i;
        return -1;
    }

    // ---- MemberReference-arm rows ----

    /// <summary>MemoryExtensions.ToUpperInvariant/ToLowerInvariant over char
    /// spans (<see cref="LoweredSpanCaseFold"/> — shape-keyed; any other overload
    /// falls through and transpiles from its real body). The TypeGate duplicates
    /// the predicate's TYPE test on purpose, as on <see cref="MdEnvMember"/>: a
    /// duplicated type name cannot drift, a duplicated MEMBER SET is what
    /// can.</summary>
    public static readonly MemberRefIntercept MrSpanCaseFold = new(
        InterceptCutKind.Cut, InterceptEmitArm.SpanCaseFold,
        typeGate: "System.MemoryExtensions",
        extra: static (dt, n, sig) => LoweredSpanCaseFold(dt, n, sig));

    /// <summary>The System.IO.Path / System.IO.File overloads a dn2cpp_path_* /
    /// dn2cpp_file_* helper models (<see cref="LoweredIoMember"/> — shape-keyed;
    /// every declined overload falls through and transpiles from its real BCL
    /// body). Boolean row; the emit arm re-asks the predicate for WHICH
    /// lowering.</summary>
    public static readonly MemberRefIntercept MrIoMember = new(
        InterceptCutKind.Cut, InterceptEmitArm.IoIntrinsic,
        extra: static (dt, n, sig) => LoweredIoMember(dt, n, sig) is not null);

    /// <summary>The System.Environment overloads a dn2cpp_env_* helper models
    /// (<see cref="LoweredEnvMember"/>) — the MemberRef twin of
    /// <see cref="MdEnvMember"/>, sharing its predicate and its TypeGate
    /// rationale.</summary>
    public static readonly MemberRefIntercept MrEnvMember = new(
        InterceptCutKind.Cut, InterceptEmitArm.EnvIntrinsic,
        typeGate: "System.Environment",
        extra: static (dt, n, sig) => LoweredEnvMember(dt, n, sig));

    /// <summary>The sub-word integers' ToString/Parse/TryParse/TryFormat
    /// (<see cref="IsInlineLoweredPrimitiveMember"/>) — the MemberRef twin of
    /// <see cref="MdInlinePrimitive"/>.</summary>
    public static readonly MemberRefIntercept MrInlinePrimitive = new(
        InterceptCutKind.Cut, InterceptEmitArm.InlinePrimitive,
        extra: static (dt, n, _) => IsInlineLoweredPrimitiveMember(dt, n));

    /// <summary>System.Collections.Comparer.Compare(object, object)
    /// (<see cref="LoweredComparerCompare"/>) — the MemberRef twin of
    /// <see cref="MdComparerCompare"/>.</summary>
    public static readonly MemberRefIntercept MrComparerCompare = new(
        InterceptCutKind.Cut, InterceptEmitArm.ComparerCompare,
        extra: static (dt, n, _) => LoweredComparerCompare(dt, n));

    /// <summary>The WIDE integer primitives' TryFormat (the span-write formatter) — the
    /// belt to the intrinsic-type braces, keeping the System.Number.TryFormat* cascade out
    /// of the tree.
    ///
    /// <para>Its four names are the eight of <see cref="LoweredIntegerTryFormat"/> minus the
    /// four sub-words of <see cref="MrInlinePrimitive"/>, written out rather than derived as
    /// that difference on purpose: the derivation would only hold while the sub-word row is
    /// asked FIRST on both askers, which would make a chain ORDER load-bearing for
    /// correctness — exactly the coupling the registry removes by letting each asker
    /// reference rows at its own position. Nothing can drift either way, since this is
    /// already one row both askers share.</para></summary>
    public static readonly MemberRefIntercept MrWideIntTryFormat = new(
        InterceptCutKind.Cut, InterceptEmitArm.WideTryFormat,
        nameGate: "TryFormat",
        extra: static (dt, _, _) =>
            dt is "System.Int32" or "System.UInt32" or "System.Int64" or "System.UInt64");

    /// <summary>Const-folded capability getters referenced cross-assembly — the
    /// MemberRef twin of <see cref="MdConstFoldedGetter"/>, same predicate
    /// (<see cref="ConstFoldedGetter"/>) behind both. Only the CUT asks this row
    /// (the resolver has no MethodInfo); the emit mouth is the post-resolution
    /// <see cref="MdConstFoldedGetter"/> reference in
    /// <c>MethodCompiler.TranslateCall</c>, which shares the arm id — so this row
    /// deliberately has no case in the MemberRef emit funnel.</summary>
    public static readonly MemberRefIntercept MrConstFoldedGetter = new(
        InterceptCutKind.Cut, InterceptEmitArm.ConstFoldedGetter,
        extra: static (dt, n, _) =>
            IsConstFoldedGetterName(n) && dt is not null && ConstFoldedGetter(dt, n) is not null);

    // ---- MemberReference rows whose whole test is an inline name gate ----

    /// <summary>The non-generic Enum reflection statics taking a runtime Type
    /// (GetNames/GetName/IsDefined/Parse/TryParse/Format/GetUnderlyingType/GetValues/
    /// GetValuesAsUnderlyingType/ToObject) — lowered via the per-enum runtime table; the
    /// generic Enum.*&lt;T&gt; forms are a MethodSpecification, cut elsewhere. Name-only
    /// predicate (no signature decode on either asker); the emit arm decodes in the NULL
    /// context.
    ///
    /// <para>The emit arm (TryEmitEnumArrayIntrinsic) classifies Parse/TryParse and ToObject
    /// by argument TYPE rather than arity, so no wrong-shape route is possible: the span
    /// Parse overloads realize the span into a Dn2CppString and reuse the string path, and
    /// ToObject covers .NET's whole overload set (the eight integral forms + object).
    /// Cutting Format and ToObject deletes the GetEnumInfo/GetEnumValuesAndNames +
    /// RuntimeType-cache reflection cascade and the last transpiled caller of
    /// Enum.ValidateRuntimeType, whose `enumType is RuntimeType` test is structurally false
    /// for a dn2cpp Type object (every Type carries the shared &amp;dn2cpp_type_type header,
    /// and RuntimeType derives from Type, so the isinst can never match) and threw
    /// Arg_MustBeType at run time.</para>
    ///
    /// <para>These are STATIC methods, so only the call-site pair reaches them: the
    /// constrained/virtual pair (a slot dispatch) and the scan pair (the enum type is a
    /// runtime Type argument, not a generic in the token) have nothing to wire.</para></summary>
    public static readonly MemberRefIntercept MrEnumStatics = new(
        InterceptCutKind.Cut, InterceptEmitArm.IntrinsicUnderDeclTypeNullCtx,
        typeGate: "System.Enum",
        extra: static (_, n, _) => n is "GetNames" or "GetName" or "IsDefined"
            or "Parse" or "TryParse" or "Format" or "GetUnderlyingType" or "GetValues"
            or "GetValuesAsUnderlyingType" or "ToObject");

    /// <summary>Nullable.GetUnderlyingType(Type) — the real reflection body is not
    /// an edge; lowered to the runtime helper (NULL-context decode, like
    /// <see cref="MrEnumStatics"/>).</summary>
    public static readonly MemberRefIntercept MrNullableGetUnderlyingType = new(
        InterceptCutKind.Cut, InterceptEmitArm.IntrinsicUnderDeclTypeNullCtx,
        typeGate: "System.Nullable", nameGate: "GetUnderlyingType");

    /// <summary>SynchronizationContext.get_Current / SetSynchronizationContext —
    /// the per-thread runtime slot (dn2cpp_sync_ctx_get/set). Instance members
    /// (Post/Send) transpile.</summary>
    public static readonly MemberRefIntercept MrSyncContextSlot = new(
        InterceptCutKind.Cut, InterceptEmitArm.IntrinsicUnderDeclType,
        typeGate: "System.Threading.SynchronizationContext",
        extra: static (_, n, _) => n is "get_Current" or "SetSynchronizationContext");

    /// <summary>NativeMemory's C-heap wrappers (Alloc/AllocZeroed/Free/Realloc,
    /// the Aligned* trio, the bulk Clear/Copy/Fill) → dn2cpp_native_* / memset /
    /// memmove.</summary>
    public static readonly MemberRefIntercept MrNativeMemory = new(
        InterceptCutKind.Cut, InterceptEmitArm.IntrinsicUnderDeclType,
        typeGate: "System.Runtime.InteropServices.NativeMemory",
        extra: static (_, n, _) => n is "Alloc" or "AllocZeroed" or "Free" or "Realloc"
            or "AlignedAlloc" or "AlignedFree" or "AlignedRealloc"
            or "Clear" or "Copy" or "Fill");

    /// <summary>The Marshal surface lowered inline (the 37-name family — heap
    /// alloc/free/copy, SizeOf/PtrToStructure/StructureToPtr/OffsetOf, the cached
    /// last-error slot, the CoTaskMem reallocators, the PtrToString / StringTo /
    /// ZeroFree decoders/encoders, the typed Read*/Write* accessors). One row, both
    /// askers — there is no second copy of this 37-name set anywhere.</summary>
    public static readonly MemberRefIntercept MrMarshalInterop = new(
        InterceptCutKind.Cut, InterceptEmitArm.IntrinsicUnderDeclType,
        typeGate: "System.Runtime.InteropServices.Marshal",
        extra: static (_, n, _) => n is "AllocHGlobal" or "FreeHGlobal" or "Copy"
            or "SizeOf" or "PtrToStructure" or "StructureToPtr" or "OffsetOf"
            or "GetLastWin32Error" or "GetLastPInvokeError" or "SetLastPInvokeError"
            or "ReAllocHGlobal" or "AllocCoTaskMem" or "ReAllocCoTaskMem" or "FreeCoTaskMem"
            or "PtrToStringUTF8" or "PtrToStringAnsi" or "PtrToStringUni"
            or "StringToHGlobalAnsi" or "StringToHGlobalUni"
            or "StringToCoTaskMemAnsi" or "StringToCoTaskMemUni" or "StringToCoTaskMemUTF8"
            or "ZeroFreeGlobalAllocAnsi" or "ZeroFreeGlobalAllocUnicode"
            or "ZeroFreeCoTaskMemAnsi" or "ZeroFreeCoTaskMemUnicode" or "ZeroFreeCoTaskMemUTF8"
            or "ReadByte" or "ReadInt16" or "ReadInt32" or "ReadInt64" or "ReadIntPtr"
            or "WriteByte" or "WriteInt16" or "WriteInt32" or "WriteInt64" or "WriteIntPtr");

    /// <summary>The non-generic MemoryMarshal.GetArrayDataReference(Array) overload
    /// only (GenericParameterCount == 0) → dn2cpp_pinned_data_addr; the generic
    /// GetArrayDataReference&lt;T&gt;(T[]) is a MethodSpecification, cut elsewhere.
    /// The signature thunk is called behind the name gate. The same member reached
    /// intra-CoreLib is <see cref="MdMemoryMarshalArrayData"/> — a different asker pair,
    /// hence a second row rather than a second list.</summary>
    public static readonly MemberRefIntercept MrMemoryMarshalArrayData = new(
        InterceptCutKind.Cut, InterceptEmitArm.IntrinsicUnderDeclType,
        typeGate: "System.Runtime.InteropServices.MemoryMarshal", nameGate: "GetArrayDataReference",
        extra: static (_, _, sig) => sig().GenericParameterCount == 0);

    /// <summary>The System.GC CUT family (shape-gated): SuppressFinalize /
    /// ReRegisterForFinalize / GetTotalMemory / GetTotalAllocatedBytes take one
    /// argument, Collect / WaitForPendingFinalizers /
    /// GetAllocatedBytesForCurrentThread take none. KeepAlive is deliberately NOT here —
    /// it is route-without-cut (<see cref="MrGcKeepAlive"/>), because an empty
    /// transpiled body is inlinable and -O2 would erase the liveness barrier it
    /// exists to provide. The shape gate lives inside the predicate; sig() is
    /// touched only for the named members, never for KeepAlive or an
    /// unrelated GC member.</summary>
    public static readonly MemberRefIntercept MrGc = new(
        InterceptCutKind.Cut, InterceptEmitArm.GcFamily,
        typeGate: "System.GC",
        extra: static (_, n, sig) => n switch
        {
            "SuppressFinalize" or "ReRegisterForFinalize" or "GetTotalMemory"
                or "GetTotalAllocatedBytes" => sig().ParameterTypes.Length == 1,
            "Collect" or "WaitForPendingFinalizers"
                or "GetAllocatedBytesForCurrentThread" => sig().ParameterTypes.Length == 0,
            _ => false,
        });

    /// <summary>GC.KeepAlive(object) — EMIT-ONLY (route-without-cut): the emitter
    /// lowers it to the opaque dn2cpp_keep_alive barrier, but the real body stays
    /// in the tree (it must NOT be cut — an empty body the optimizer erases is the
    /// one thing KeepAlive exists to prevent). CutKind.None; the reachability
    /// resolver must never reference this row.</summary>
    public static readonly MemberRefIntercept MrGcKeepAlive = new(
        InterceptCutKind.None, InterceptEmitArm.GcFamily,
        typeGate: "System.GC", nameGate: "KeepAlive",
        extra: static (_, _, sig) => sig().ParameterTypes.Length == 1);

    /// <summary>Object.Finalize() — the compiler-generated base call in a C#
    /// destructor's finally; a pure no-op (pop the receiver).</summary>
    public static readonly MemberRefIntercept MrObjectFinalize = new(
        InterceptCutKind.Cut, InterceptEmitArm.ObjectFinalizeNoOp,
        typeGate: "System.Object", nameGate: "Finalize",
        extra: static (_, _, sig) => sig().ParameterTypes.Length == 0);

    /// <summary>SpanHelpers.Memmove → std::memmove. REACH-ONLY here: SpanHelpers
    /// is an internal CoreLib type, so a cross-assembly MemberRef to it does not
    /// occur; the MemberRef cut is the belt to the MethodDefinition
    /// RuntimePrimitive route (<see cref="LoweredRuntimePrimitive"/> names
    /// "System.SpanHelpers" => "Memmove"), which is where the in-CoreLib form is
    /// lowered. There is no MemberRef emit funnel case — the emitter never sees
    /// this shape — so the EmitArm is nominal (RuntimePrimitive documents the real
    /// mouth).</summary>
    public static readonly MemberRefIntercept MrSpanHelpersMemmove = new(
        InterceptCutKind.Cut, InterceptEmitArm.RuntimePrimitive,
        typeGate: "System.SpanHelpers", nameGate: "Memmove");

    /// <summary>Buffer's byte-granular surface over the one extent: BlockCopy →
    /// dn2cpp_buffer_blockcopy, ByteLength → dn2cpp_buffer_bytelength, Get/SetByte →
    /// dn2cpp_buffer_get/setbyte. BlockCopy's and ByteLength's real bodies reach the
    /// Array.NativeLength / GetCorElementTypeOfElementType / GetElementSize InternalCalls;
    /// Get/SetByte's reach the non-generic MemoryMarshal.GetArrayDataReference(Array),
    /// whose own body is RuntimeHelpers.GetMethodTable pointer math. Lowering them here
    /// rather than through that reference keeps the ELEMENT verdict at the emit site:
    /// Buffer must refuse a non-primitive element, and no header read answers that for an
    /// imprecise array handle. The refusal precedes every extent.</summary>
    public static readonly MemberRefIntercept MrBufferExtent = new(
        InterceptCutKind.Cut, InterceptEmitArm.IntrinsicUnderDeclType,
        typeGate: "System.Buffer",
        extra: static (_, n, _) => n is "BlockCopy" or "ByteLength" or "GetByte" or "SetByte");

    /// <summary>Encoding.GetString (Encoding / ASCIIEncoding / UTF8Encoding / UnicodeEncoding) —
    /// the core of the UTF-8-decode + SIMD cascade collapse. Symmetric and sound:
    /// reach = name cut, emit = name gate + shape-total-throw
    /// (<c>TryEmitEncodingGetString</c> has no false path). Name-only predicate;
    /// the shape check lives in the emit arm.</summary>
    public static readonly MemberRefIntercept MrEncodingGetString = new(
        InterceptCutKind.Cut, InterceptEmitArm.EncodingGetString,
        nameGate: "GetString",
        extra: static (dt, _, _) =>
            dt is "System.Text.Encoding" or "System.Text.ASCIIEncoding"
                or "System.Text.UTF8Encoding" or "System.Text.UnicodeEncoding");

    /// <summary>AppContext.BaseDirectory → the running executable's directory.
    /// Only this member; the type's switch/data accessors transpile as real
    /// IL.</summary>
    public static readonly MemberRefIntercept MrAppContextBaseDir = new(
        InterceptCutKind.Cut, InterceptEmitArm.IntrinsicUnderDeclType,
        typeGate: "System.AppContext", nameGate: "get_BaseDirectory");

    /// <summary>Directory.Exists / GetCurrentDirectory / SetCurrentDirectory —
    /// lowered via the runtime helpers; CreateDirectory is deliberately NOT here
    /// (callers deref the returned DirectoryInfo, so its real body transpiles).</summary>
    public static readonly MemberRefIntercept MrDirectory = new(
        InterceptCutKind.Cut, InterceptEmitArm.IntrinsicUnderDeclType,
        typeGate: "System.IO.Directory",
        extra: static (_, n, _) => n is "Exists" or "GetCurrentDirectory" or "SetCurrentDirectory");

    /// <summary>Every MemberReference-arm row — a REGISTRY, not a chain: order
    /// carries no meaning, each asker references the rows it needs at its own
    /// chain position (same contract as <see cref="MethodDefIntercepts"/>).</summary>
    public static readonly MemberRefIntercept[] MemberRefIntercepts =
    [
        MrSpanCaseFold,
        MrIoMember,
        MrEnvMember,
        MrInlinePrimitive,
        MrComparerCompare,
        MrWideIntTryFormat,
        MrConstFoldedGetter,
        MrEnumStatics,
        MrNullableGetUnderlyingType,
        MrSyncContextSlot,
        MrNativeMemory,
        MrMarshalInterop,
        MrMemoryMarshalArrayData,
        MrGc,
        MrGcKeepAlive,
        MrObjectFinalize,
        MrSpanHelpersMemmove,
        MrBufferExtent,
        MrEncodingGetString,
        MrAppContextBaseDir,
        MrDirectory,
    ];

    // ---- MethodSpecification-arm rows ----
    //
    // The rows below are the member sets BOTH MethodSpec chains ask. Three member sets
    // are deliberately NOT rows, because their two sides are asymmetric — each stays
    // written out at its call site naming its counterpart:
    //   * VectorMath.Min/Max            — reach-only cut; the route is EmitManagedCall's
    //                                     dead-zero-vector fold, not TranslateGenericIntrinsic.
    //   * MemoryMarshal span helpers    — emit-only route; the cut rides IsIntrinsicType.
    //   * CustomAttributeExtensions
    //       .GetCustomAttributes        — emit-only route (route-without-cut = bloat, never
    //                                     a link error).
    // A row here is (parent TYPE NAME of the MethodSpec token, its METHOD NAME) — read
    // off the token, before resolution. See NameKeyedIntercept on why that differs from
    // the ConstrainedVirtualIntercepts table below.

    /// <summary>The integer <c>PrimitiveTypeCode</c>s — the ten widths a generic-math
    /// conversion may target. The single source for both askers' Create* rows and for
    /// <c>MethodCompiler.IsIntegerPrimitive</c>, which forwards here: the reach chain
    /// used to spell the ten codes out while the emit chain called its own helper, two
    /// copies of one set with nothing making them agree.</summary>
    public static bool IsIntegerPrimitiveCode(PrimitiveTypeCode p) => p is
        PrimitiveTypeCode.SByte or PrimitiveTypeCode.Byte or
        PrimitiveTypeCode.Int16 or PrimitiveTypeCode.UInt16 or
        PrimitiveTypeCode.Int32 or PrimitiveTypeCode.UInt32 or
        PrimitiveTypeCode.Int64 or PrimitiveTypeCode.UInt64 or
        PrimitiveTypeCode.IntPtr or PrimitiveTypeCode.UIntPtr;

    /// <summary>MemoryExtensions' scalar-lowered span scans: their real bodies
    /// vectorize through Unsafe.BitCast / IsBitwiseEquatable, untranspilable, so the
    /// call site emits a scalar loop and the real body must NOT be an edge.</summary>
    public static readonly NameKeyedIntercept MsMemoryExtScan = new(
        InterceptCutKind.Cut, InterceptEmitArm.GenericIntrinsic,
        typeGate: "System.MemoryExtensions",
        extra: static (_, n) => n is "Contains" or "IndexOf" or "LastIndexOf"
            or "SequenceEqual" or "StartsWith" or "EndsWith" or "IndexOfAny"
            or "LastIndexOfAny" or "IndexOfAnyExcept" or "LastIndexOfAnyExcept"
            or "Reverse" or "Sort");

    /// <summary>MemoryExtensions.ContainsAny/ContainsAnyExcept — lowered inline ONLY in
    /// their SearchValues&lt;T&gt; 2-arg shape (a runtime set scan). This row owns the
    /// NAME set; the shape test stays at both call sites as the one shared call to
    /// <c>Compilation.IsMemoryExtSearchValuesForm</c> — it needs the module, the spec and
    /// the asker's own generic context, so it is not pure and cannot live in a row. That
    /// is a shared PREDICATE CALL at two sites, not a second list: what drifts is a
    /// duplicated member set, and the member set is here.</summary>
    public static readonly NameKeyedIntercept MsMemoryExtSearchValues = new(
        InterceptCutKind.Cut, InterceptEmitArm.GenericIntrinsic,
        typeGate: "System.MemoryExtensions",
        extra: static (_, n) => n is "ContainsAny" or "ContainsAnyExcept");

    /// <summary>Enum's generic reflection statics — lowered inline from the per-enum
    /// (name, value) table; their real bodies route through the reflection metadata
    /// stack (ArrayPool/EventSource/Calli). The non-generic overloads taking a runtime
    /// Type are the MemberReference twin (<see cref="MrEnumStatics"/>) — note the sets
    /// differ (GetUnderlyingType/GetValues are non-generic-only), which is exactly why
    /// they are two rows and not one. Parse and TryParse appear in BOTH: the generic
    /// forms here, the runtime-Type forms there.</summary>
    public static readonly NameKeyedIntercept MsEnumStatics = new(
        InterceptCutKind.Cut, InterceptEmitArm.GenericIntrinsic,
        typeGate: "System.Enum",
        extra: static (_, n) => n is "GetValues" or "GetNames" or "GetName"
            or "Parse" or "TryParse" or "IsDefined");

    /// <summary>Marshal's generic blittable-marshalling statics: SizeOf/PtrToStructure/
    /// StructureToPtr/OffsetOf lower to sizeof / a value copy / offsetof, and the
    /// delegate/function-pointer pair to the generated per-delegate-type thunk-pool
    /// helpers. Their real bodies reflect or QCall into the native marshaller.</summary>
    public static readonly NameKeyedIntercept MsMarshalGenerics = new(
        InterceptCutKind.Cut, InterceptEmitArm.GenericIntrinsic,
        typeGate: "System.Runtime.InteropServices.Marshal",
        extra: static (_, n) => n is "SizeOf" or "PtrToStructure"
            or "StructureToPtr" or "OffsetOf"
            or "GetFunctionPointerForDelegate" or "GetDelegateForFunctionPointer");

    /// <summary>Activator.CreateInstance&lt;T&gt;() → <c>new T()</c>; its real body
    /// reflects, so it is not an edge — the scan reaches T's ctor instead.</summary>
    public static readonly NameKeyedIntercept MsActivatorCreateInstance = new(
        InterceptCutKind.Cut, InterceptEmitArm.GenericIntrinsic,
        typeGate: "System.Activator", nameGate: "CreateInstance");

    /// <summary>GC.AllocateUninitializedArray&lt;T&gt;(length[, pinned]) →
    /// <c>new T[length]</c> ("uninitialized" is only a zero-fill optimization, so a
    /// zero-init array is always a valid result). The real worker reflects on
    /// typeof(T).TypeHandle, an InternalCall.</summary>
    public static readonly NameKeyedIntercept MsGcAllocateUninitializedArray = new(
        InterceptCutKind.Cut, InterceptEmitArm.GenericIntrinsic,
        typeGate: "System.GC", nameGate: "AllocateUninitializedArray");

    /// <summary>StringBuilder.AppendInterpolatedStringHandler.AppendFormatted&lt;T&gt;
    /// (and its AppendFormattedWithTempSpace&lt;T&gt; helper) — the handler is modeled
    /// as the underlying StringBuilder and this formats T straight into it. Its real
    /// body reaches Enum.TryFormatUnconstrained → the RuntimeType/EnumInfo/Number-format
    /// cascade; cutting this edge is the core of that cascade's collapse. The bare
    /// nested name is what MethodSpecParentTypeName returns here (the TypeRef carries no
    /// namespace).</summary>
    public static readonly NameKeyedIntercept MsAppendInterpolatedFormatted = new(
        InterceptCutKind.Cut, InterceptEmitArm.GenericIntrinsic,
        typeGate: "AppendInterpolatedStringHandler",
        extra: static (_, n) => n is "AppendFormatted" or "AppendFormattedWithTempSpace");

    /// <summary>INumberBase&lt;T&gt;.CreateTruncating/CreateChecked/CreateSaturating
    /// &lt;TOther&gt; on a concrete integer primitive: a pure numeric cast. The real
    /// bodies route through the INumberBase TryConvert* InternalCalls. The 32/64-bit
    /// targets already qualify via <see cref="IsIntrinsicType"/>; this row is what makes
    /// the sub-word targets (Byte/SByte/Int16/UInt16), which are NOT intrinsic types,
    /// dispatch too. Name-first so the type switch behind
    /// <c>Compilation.WellKnownPrimitive</c> only runs for the three names.</summary>
    public static readonly NameKeyedIntercept MsCreateConversion = new(
        InterceptCutKind.Cut, InterceptEmitArm.GenericIntrinsic,
        extra: static (dt, n) =>
            n is "CreateTruncating" or "CreateChecked" or "CreateSaturating"
            && dt is not null
            && Compilation.WellKnownPrimitive(dt) is { Kind: TypeKind.Primitive } cm
            && IsIntegerPrimitiveCode(cm.Primitive));

    /// <summary>Int128/UInt128.CreateTruncating&lt;TOther&gt; over an integer-primitive
    /// source. Int128/UInt128 are ordinary transpiled BCL structs (two ulong fields,
    /// {_lower, _upper} in little-endian declaration order), NOT primitives, so
    /// <see cref="MsCreateConversion"/>'s <c>WellKnownPrimitive</c> gate never matches
    /// them and their real bodies stay in the tree — where CreateTruncating branches to
    /// <c>TOther.TryConvertToTruncating</c>, an InternalCall with no IL (the
    /// <c>IBinaryInteger_Int32::TryConvertToTruncating</c> gap the Int128Converter/
    /// UInt128Converter parse paths reach via Newtonsoft/TypeDescriptor). Cut it; the
    /// route (TranslateGenericIntrinsic) lowers the widening — a sign/zero extension into
    /// {_lower, _upper} — inline for an integer-primitive source and loudly declines any
    /// other TOther (no general Int128 generic-math engine). CreateChecked/CreateSaturating
    /// are NOT cut: they are unreached, and widening into 128 bits never overflows anyway.
    /// A STATIC method, so only the call-site pair reaches it.</summary>
    public static readonly NameKeyedIntercept MsInt128CreateConversion = new(
        InterceptCutKind.Cut, InterceptEmitArm.GenericIntrinsic,
        extra: static (dt, n) =>
            n == "CreateTruncating" && dt is "System.Int128" or "System.UInt128");

    /// <summary>Every MethodSpecification-arm row — a REGISTRY, not a chain: order
    /// carries no meaning, each asker references the rows it needs at its own chain
    /// position (same contract as <see cref="MethodDefIntercepts"/>).</summary>
    public static readonly NameKeyedIntercept[] MethodSpecIntercepts =
    [
        MsMemoryExtScan,
        MsMemoryExtSearchValues,
        MsEnumStatics,
        MsMarshalGenerics,
        MsActivatorCreateInstance,
        MsGcAllocateUninitializedArray,
        MsAppendInterpolatedFormatted,
        MsCreateConversion,
        MsInt128CreateConversion,
    ];

    // ---- The constrained/virtual asker pair ----
    //
    // A SECOND asker pair, distinct from the call-site pair above: that one asks about a
    // CALL TOKEN, this one about a RESOLVED IMPL — a slot dispatch has already bound. It
    // has two mouths on each side:
    //
    //   reach: Compilation.ReachConstrainedImpl  (the `constrained.` prefix's target)
    //          Compilation.ReachVirtualImpl      (an interface slot on a boxed value)
    //   emit:  MethodCompiler.TryEmitValueConstrained (the interpolation hole's
    //          ISpanFormattable::TryFormat)
    //          the runtime's dn2cpp_object_tostring, which formats a boxed sub-word
    //          integer from its type-info because the handwritten dn2cpp_*_type carry
    //          a nullptr tostring slot — there is no transpiler-side route at all
    //
    // Every one of those mouths asks the SAME two predicates below — 8-wide TryFormat
    // (Byte..UInt64) and 4-wide ToString (the sub-words). None restates a name set, and
    // none may: a cut on one mouth paired with a route that lost a name on another fails as
    // an undefined symbol at C++ LINK time. Both predicates DERIVE their type set from
    // PrimitiveIntegerFullName / IsInlineLoweredPrimitiveMember, so there is no further
    // list to drift either.

    /// <summary>The <c>System.*</c> name of an integer primitive (the eight types whose
    /// TryFormat is lowered inline), or null for any non-integer primitive.
    /// Used to route a `constrained. callvirt ISpanFormattable::TryFormat` back to the
    /// concrete integer type's EmitIntrinsic case, by the static-virtual admission
    /// (<c>MethodCompiler.ConstrainedStaticVirtualSelf</c>) and by its reach-side twin —
    /// and it is the SOURCE OF THE EIGHT-TYPE SET that
    /// <see cref="LoweredIntegerTryFormat"/> tests against.
    ///
    /// <para>It lives here rather than on MethodCompiler because the reachability scan
    /// asks it too: a predicate both askers consult cannot sit inside one of
    /// them.</para></summary>
    public static string? PrimitiveIntegerFullName(PrimitiveTypeCode p) => p switch
    {
        PrimitiveTypeCode.Byte => "System.Byte",
        PrimitiveTypeCode.SByte => "System.SByte",
        PrimitiveTypeCode.Int16 => "System.Int16",
        PrimitiveTypeCode.UInt16 => "System.UInt16",
        PrimitiveTypeCode.Int32 => "System.Int32",
        PrimitiveTypeCode.UInt32 => "System.UInt32",
        PrimitiveTypeCode.Int64 => "System.Int64",
        PrimitiveTypeCode.UInt64 => "System.UInt64",
        _ => null,
    };

    /// <summary>An integer primitive's <c>TryFormat</c> — the span-write formatter, all
    /// EIGHT widths (Byte..UInt64). Reached as a `constrained. &lt;int&gt; callvirt
    /// ISpanFormattable::TryFormat` out of a `{value:fmt}` interpolation hole (Roslyn
    /// lowers those through DefaultInterpolatedStringHandler), or as an interface slot on
    /// a boxed integer. Lowered inline to dn2cpp_try_format_int|uint, so the real body must
    /// not be reached: it pulls in the whole System.Number.TryFormat* subtree.
    ///
    /// <para>EIGHT-wide, which is why <see cref="IsInlineLoweredPrimitiveMember"/> cannot
    /// serve: that predicate is FOUR-wide by construction — the sub-words are the ones whose
    /// whole ToString/Parse/TryParse/TryFormat family lowers inline, because Int32/Int64 and
    /// their unsigned twins are intrinsic types whose members never transpile anyway.</para>
    ///
    /// <para>The type set is DERIVED, not restated: a name is one of the eight exactly
    /// when <c>Compilation.WellKnownPrimitive</c> maps it to a code that
    /// <see cref="PrimitiveIntegerFullName"/> maps back. The name test sits behind the
    /// method-name gate, so the type switch runs only for a TryFormat.</para></summary>
    public static bool LoweredIntegerTryFormat(string? declType, string name) =>
        name == "TryFormat"
        && declType is not null
        && Compilation.WellKnownPrimitive(declType) is { Kind: TypeKind.Primitive } wk
        && PrimitiveIntegerFullName(wk.Primitive) is not null;

    /// <summary>A sub-word integer's <c>ToString</c> reached through a BOXED value —
    /// Object::ToString or IFormattable::ToString on a boxed Byte/SByte/Int16/UInt16.
    /// Never needed: dn2cpp_object_tostring formats such a value straight from its
    /// type-info (the handwritten dn2cpp_*_type carry a nullptr tostring slot), so the
    /// real Byte.ToString body is dead. The direct-call edge is cut on the call-site
    /// asker pair (<see cref="MrInlinePrimitive"/> / <see cref="MdInlinePrimitive"/>);
    /// this is the boxed-dispatch twin, without which a boxed sub-word plus a used
    /// Object.ToString virtual slot re-roots the whole Number-format subtree.
    ///
    /// <para><b>The name gate is this predicate's own and must stay here.</b>
    /// <see cref="IsInlineLoweredPrimitiveMember"/> answers true for the whole
    /// ToString/Parse/TryParse/TryFormat family, so delegating to it WITHOUT testing the
    /// name first would cut Parse/TryParse/TryFormat off this dispatch path as well —
    /// slots that are genuinely reached and whose bodies nothing here lowers.</para></summary>
    public static bool LoweredBoxedSubWordToString(string? declType, string name) =>
        name == "ToString" && IsInlineLoweredPrimitiveMember(declType, name);

    /// <summary>An integer primitive's ISpanFormattable/IUtf8SpanFormattable
    /// <c>TryFormat</c>, all eight widths (<see cref="LoweredIntegerTryFormat"/>).</summary>
    public static readonly NameKeyedIntercept CvIntegerTryFormat = new(
        InterceptCutKind.Cut, InterceptEmitArm.ConstrainedTryFormat,
        nameGate: "TryFormat",
        extra: static (dt, n) => LoweredIntegerTryFormat(dt, n));

    /// <summary>A boxed sub-word integer's Object/IFormattable <c>ToString</c>
    /// (<see cref="LoweredBoxedSubWordToString"/>).</summary>
    public static readonly NameKeyedIntercept CvBoxedSubWordToString = new(
        InterceptCutKind.Cut, InterceptEmitArm.BoxedToStringSlot,
        nameGate: "ToString",
        extra: static (dt, n) => LoweredBoxedSubWordToString(dt, n));

    /// <summary>An enum's ISpanFormattable::TryFormat impl reached as an interface slot on
    /// a BOXED enum (EnumConverter.ConvertTo). The resolved impl is inherited from
    /// System.Enum, so it is name-keyable — <c>System.Enum.System.ISpanFormattable.TryFormat</c>.
    /// This row is the boxed mouth only; the CONSTRAINED mouth (the enum reached through a
    /// `constrained. &lt;enum&gt; callvirt ISpanFormattable::TryFormat`, whose token names the
    /// interface method "TryFormat" over an arbitrary enum type name) cannot share this row —
    /// its predicate is non-pure (needs <c>ClassInfo.IsEnum</c>), so it is written out inline
    /// at <c>ReachConstrainedImpl</c> and <c>TryEmitValueConstrained</c> with a comment naming
    /// this counterpart. See <see cref="InterceptEmitArm.ConstrainedEnumTryFormat"/>.</summary>
    public static readonly NameKeyedIntercept CvEnumTryFormat = new(
        InterceptCutKind.Cut, InterceptEmitArm.ConstrainedEnumTryFormat,
        typeGate: "System.Enum",
        nameGate: "System.ISpanFormattable.TryFormat");

    /// <summary>Every constrained/virtual-arm row. A REGISTRY, not a chain, like the two
    /// tables above — but note what the (declaring type, member name) pair MEANS here:
    /// it is read off the RESOLVED IMPL, not off a call token. These askers have no
    /// question until dispatch has bound the slot, so a row is asked with the concrete
    /// type the constrained receiver or the boxed value landed on. Rows are therefore
    /// NOT interchangeable with <see cref="MethodSpecIntercepts"/>, whose pair is a
    /// token's parent-type and method name before any resolution.</summary>
    public static readonly NameKeyedIntercept[] ConstrainedVirtualIntercepts =
    [
        CvIntegerTryFormat,
        CvBoxedSubWordToString,
        CvEnumTryFormat,
    ];

    // ---- The reachability SCAN's rows ----
    //
    // A THIRD asker, and it is not the resolver. Compilation.ScanBodyForGenerics walks a
    // reachable body's tokens and, for a handful of members, acts BEFORE deciding whether
    // to call ResolveCallTarget at all — either recording a use the IL does not otherwise
    // express, or reaching something the IL does not name and then skipping resolution.
    // Its emit counterparts are all deliberately un-migrated mouths (see the three
    // documentary arms on InterceptEmitArm), so what these rows buy is not a second
    // asker's agreement but the member sets written down once, beside the sets the other
    // askers share, rather than inline in a switch arm that connects them to nothing.
    //
    // The EFFECT of a matched row stays at the call site — it touches Compilation state,
    // and a predicate the innermost per-token loop asks must stay pure. The row carries the
    // member set, the continuation kind (InterceptScanEffect), and the arm that makes the
    // skip sound.

    /// <summary>The two Object-rooted equality members. Shared by
    /// <see cref="ScObjectEqualityDispatch"/>'s predicate and by the scan's
    /// MethodDefinition mouth, which must test the NAME before it composes the
    /// declaring type's full name — two more raw metadata reads and a concat that
    /// every call token in every reachable body would otherwise pay. That is a shared
    /// PREDICATE CALL at two sites, not a second list: the member set is here.</summary>
    public static bool IsObjectEqualityMemberName(string name) => name is "Equals" or "GetHashCode";

    /// <summary>An <c>Object</c>/<c>ValueType</c>-rooted Equals/GetHashCode call site:
    /// mark the object-equality dispatch used, then resolve the token as usual. The
    /// mark is the ONLY used-slot record there can be for this pair — their declaring
    /// type is intrinsic-mapped, so it owns no vtable slot — and it is what wires a
    /// boxed struct's structural equality into the type-info slots
    /// <c>dn2cpp_object_equals</c> answers from.
    ///
    /// <para>The NON-GENERIC <c>System.Collections.IEqualityComparer</c> carries the same
    /// two member names and is in the set for the same reason: its emitted dispatch
    /// (MethodCompiler.EmitManagedCall's interface arm) answers a null receiver — the
    /// <c>EqualityComparer&lt;T&gt;.Default</c> nullptr sentinel, escaped into Tuple's
    /// structural trio as an object — with these very helpers, so a body holding such a
    /// call site can hand them a boxed element whose type-info slots must be wired.</para>
    ///
    /// <para>Cut kind None and continuation <see cref="InterceptScanEffect.MarkThenResolve"/>:
    /// this row adds, it does not replace. Whatever cutting happens to these tokens is
    /// <see cref="MdIntrinsicType"/>'s doing at the resolver, one asker over.</para>
    ///
    /// <para>The scan reaches this row through TWO mouths and both are needed: a
    /// MemberReference (an app or cross-assembly body naming Object::Equals) and a
    /// MethodDefinition (a body of the loaded CoreLib naming it in its own module). The
    /// second is guarded by a run-once flag at its call site — a scan-side optimisation,
    /// not part of the predicate.</para></summary>
    public static readonly NameKeyedIntercept ScObjectEqualityDispatch = new(
        InterceptCutKind.None, InterceptEmitArm.ObjectEqualityHelper,
        extra: static (dt, n) => IsObjectEqualityMemberName(n)
            && dt is "System.Object" or "System.ValueType"
                or "System.Collections.IEqualityComparer",
        scanEffect: InterceptScanEffect.MarkThenResolve);

    /// <summary>The Enum.HasFlag member name. Shared by <see cref="ScEnumHasFlag"/>'s
    /// predicate and by the two mouths that must test the NAME before composing the token's
    /// parent-type name — a string build every call token of every reachable body would
    /// otherwise pay. A shared PREDICATE CALL at three sites, not a second list. It is a
    /// predicate rather than a bare <c>NameGate</c> read for the same reason
    /// <see cref="IsObjectEqualityMemberName"/> is: an asker comparing a gate FIELD as a
    /// value silently stops matching the moment the row moves that name into
    /// <see cref="NameKeyedIntercept.Extra"/> — a legitimate widening that would kill the
    /// arm with no diagnostic.</summary>
    public static bool IsEnumHasFlagMemberName(string name) => name == "HasFlag";

    /// <summary>Enum.HasFlag — emitted inline as a bit test, so the real body must not
    /// be reached: it pulls in GetMethodTable and an InternalCall. Continuation
    /// <see cref="InterceptScanEffect.EffectThenSkipResolve"/> with no effect at all —
    /// the skip IS the whole action, the scan-side shape of a cut.
    ///
    /// <para>The emit mouth's POSITION is not a registry reference and must not become
    /// one: it sits at the top of <c>MethodCompiler.TranslateCall</c>, ahead of the
    /// constrained handling and with a <c>_constrained = null</c> side effect
    /// (<see cref="InterceptEmitArm.EnumHasFlagBitTest"/>). But the member SET it tests
    /// is this row — the mouth asks <see cref="NameKeyedIntercept.Matches"/> where it
    /// stands. Position stays hand-written; the question does not.</para></summary>
    public static readonly NameKeyedIntercept ScEnumHasFlag = new(
        InterceptCutKind.Cut, InterceptEmitArm.EnumHasFlagBitTest,
        typeGate: "System.Enum",
        extra: static (_, n) => IsEnumHasFlagMemberName(n),
        scanEffect: InterceptScanEffect.EffectThenSkipResolve);

    /// <summary>The CultureInfo.get_CompareInfo member name — the same shared-predicate
    /// shape as <see cref="IsObjectEqualityMemberName"/> and
    /// <see cref="IsEnumHasFlagMemberName"/>, and for the same reason.</summary>
    public static bool IsCultureCompareInfoMemberName(string name) => name == "get_CompareInfo";

    /// <summary>CultureInfo.get_CompareInfo — the emit synthesizes a zero-initialized
    /// CompareInfo, so the scan marks that transpiled CompareInfo allocated and skips
    /// resolution. There is no ctor to reach (the real one reads fields of the
    /// intrinsic-mapped CultureInfo) and the invariant arms of its reachable members
    /// read no instance state.
    ///
    /// <para>The cut here is belt-and-braces rather than load-bearing: CultureInfo is
    /// intrinsic-mapped, so <c>ResolveCallTarget</c> answers null for both call forms
    /// anyway (<see cref="MdIntrinsicType"/>). The row is CutKind.Cut because that is
    /// what the scan does with the token — the reason it is also redundant belongs in a
    /// comment, not in a weaker cut kind.</para>
    ///
    /// <para>Reached through both a MemberReference and a MethodDefinition mouth. The NAME
    /// is the cheap half and the call site asks it FIRST — through
    /// <see cref="IsCultureCompareInfoMemberName"/>, never by reading a gate field —
    /// because obtaining the declaring-type name on the MethodDefinition mouth builds a
    /// string.</para></summary>
    public static readonly NameKeyedIntercept ScCultureCompareInfo = new(
        InterceptCutKind.Cut, InterceptEmitArm.CultureCompareInfoSynth,
        typeGate: "System.Globalization.CultureInfo",
        extra: static (_, n) => IsCultureCompareInfoMemberName(n),
        scanEffect: InterceptScanEffect.EffectThenSkipResolve);

    /// <summary>Every scan-arm row. A REGISTRY, not a chain, like the four tables above
    /// (<see cref="MethodDefIntercepts"/>, <see cref="MemberRefIntercepts"/>,
    /// <see cref="MethodSpecIntercepts"/>, <see cref="ConstrainedVirtualIntercepts"/>)
    /// — each row is referenced by the scan at its own position in
    /// <c>ScanBodyForGenerics</c>.
    ///
    /// <para>The HasFlag row's <c>continue</c> sits AFTER the reflection-usage marks, which
    /// is an optimisation rather than an ordering constraint: it reuses the name/parent
    /// strings those marks already read, and the two name sets are disjoint. The real
    /// constraint lives one level up, in <see cref="ScanNeedsParentTypeName"/>: every name
    /// any of these sites can want must be in that prefilter, or the parent name is never
    /// read and the site never runs.</para></summary>
    public static readonly NameKeyedIntercept[] ScanIntercepts =
    [
        ScObjectEqualityDispatch,
        ScEnumHasFlag,
        ScCultureCompareInfo,
    ];

    /// <summary>Whether a call token's member NAME obliges the reachability scan to go
    /// on and read the token's PARENT TYPE name. A prefilter, and the reason it exists
    /// is cost: the parent read builds a string, the scan runs it per call token of
    /// every reachable body, and only this handful of names can ever want it.
    ///
    /// <para><b>INVARIANT — this set is a UNION OF THREE THINGS, and deriving it from
    /// the registry alone breaks the other two SILENTLY.</b> It is:</para>
    /// <para>(a) the names of every <see cref="ScanIntercepts"/> row — Equals and
    /// GetHashCode (<see cref="ScObjectEqualityDispatch"/>), HasFlag
    /// (<see cref="ScEnumHasFlag"/>), get_CompareInfo
    /// (<see cref="ScCultureCompareInfo"/>); plus</para>
    /// <para>(b) the hand-written residue: the names that drive the REFLECTION-USAGE
    /// marks — Invoke, GetValue, SetValue, CreateInstance, GetCustomAttributes,
    /// GetCustomAttribute, IsDefined. Those set <c>_reflectionInvokeUsed</c> /
    /// <c>_reflectionCtorUsed</c> / <c>_reflectionAttrUsed</c>, which are not intercepts
    /// at all — they do not cut an edge or route a call, they OPEN a reachability route
    /// (reach every app-module method body / ctor / attribute ctor so a reflected member
    /// is invokable). No descriptor row will ever carry them.</para>
    /// <para>(c) RunClassConstructor — the MemberRef mouth of the RuntimeHelpers.
    /// RunClassConstructor reach effect (reach the ldtoken'd type's cctor body so the
    /// __ensure wrapper the emit intrinsic lowers the call to is not a nullptr no-op).
    /// The in-CoreLib mouth is a MethodDef token and rides the resolved-target arm
    /// further down the scan; this name is what lets the user-module MemberRef mouth
    /// see its parent and fire at all.</para>
    /// <para>Drop (b) or (c) and nothing fails: the transpile is green, the C++ compiles and
    /// links, and the shipped binary throws the first time it calls
    /// <c>PropertyInfo.GetValue</c> on an accessor the reflection route was supposed to have
    /// reached — or silently skips a class initializer real .NET runs. A silently narrowed
    /// reachability route with no diagnostic anywhere in the toolchain is why the union is
    /// written out in full rather than computed from
    /// <see cref="ScanIntercepts"/>.</para></summary>
    public static bool ScanNeedsParentTypeName(string name) => name is
        // (a) ScanIntercepts row names.
        "Equals" or "GetHashCode" or "HasFlag" or "get_CompareInfo"
        // (b) the reflection-usage marks — NOT intercepts, never registry rows.
        or "Invoke" or "GetValue" or "SetValue" or "CreateInstance"
        or "GetCustomAttributes" or "GetCustomAttribute" or "IsDefined"
        // (c) the MemberRef mouth of the RunClassConstructor reach effect.
        or "RunClassConstructor";

    // ---- The BodyReplace and Bounded rows ----
    //
    // The two cut kinds the call-site rows above never use. Their askers are not the
    // token-shape chains (ResolveCallTarget / TranslateCall's MethodDef-MemberRef-
    // MethodSpec switch) but the reachability DRAIN — Compilation.Reach, which decides
    // whether a method's IL is scanned at all — paired with an emit mouth that is a
    // whole synthesized BODY (CppEmitter) or a call-site arm deep inside
    // EmitManagedCall. Same contract as every table above: ONE row, both askers, so the
    // intercepted member set cannot drift between the cut and the route.
    //
    // A row here is (declaring-type full name, member name), read off the RESOLVED
    // METHOD — the drain holds a MethodInfo and the emitter a compiled body, so unlike
    // MethodSpecIntercepts there is no token to read and unlike ConstrainedVirtual-
    // Intercepts no slot to bind first.

    /// <summary>A System.Net.Http method whose real IL the reachability cut deletes and
    /// whose body the emitter synthesizes in its place
    /// (<see cref="IsBodyReplacedHttpMethod"/> — the union of the transport-override, the
    /// client-certificate-accessor and the system-proxy intercepts).
    ///
    /// <para><see cref="InterceptCutKind.BodyReplace"/>, and the distinction from
    /// <see cref="InterceptCutKind.Cut"/> is the whole point: the method stays
    /// REACHABLE, so <c>CppEmitter.RenderVtable</c> gives its virtual slot a real
    /// pointer and the abstract HttpMessageHandler dispatch lands on an emitted body.
    /// What is cut is the SCAN — <c>Compilation.Reach</c> never enqueues the real IL, so
    /// the SetupHandlerChain → connection-pool → socket/DNS/TLS/QUIC subtree (and the
    /// certificate setter's X509Store → keychain → ASN.1 → BigInteger one) never enters
    /// the tree. cut ⟹ route holds through the body, not through the call site: the
    /// synthesized body carries the method's own symbol, so no call site is orphaned and
    /// nothing names a symbol the cut deleted.</para>
    ///
    /// <para>Unlike its four neighbours this row carries no <c>dt is not null</c> guard,
    /// and does not need one: <see cref="IsBodyReplacedHttpMethod"/> takes a
    /// <c>string?</c> and answers false for null, whereas <see cref="IsBoundedMethod"/>
    /// and friends take a non-null <c>string</c> and the guard is what satisfies
    /// them.</para></summary>
    public static readonly NameKeyedIntercept BrHttpShim = new(
        InterceptCutKind.BodyReplace, InterceptEmitArm.HttpShimBody,
        extra: static (dt, n) => IsBodyReplacedHttpMethod(dt, n));

    /// <summary>The System.Enum instance ToString/GetTypeCode family
    /// (<see cref="IsEnumInstanceFormatMethod"/>): real IL cut from the SCAN, body
    /// synthesized by <c>MethodCompiler.CompileEnumInstanceFormatBody</c> under the
    /// method's own symbol.
    ///
    /// <para><see cref="InterceptCutKind.BodyReplace"/>, not <see cref="InterceptCutKind.Cut"/>,
    /// and the distinction is load-bearing: an enum implements IFormattable/IConvertible, and
    /// its itable slot (<c>CppEmitter.RenderInterfaceTable</c>) points at the real body's
    /// symbol. A Cut would leave that slot a trap stub and
    /// <c>((IFormattable)someEnum).ToString(...)</c> would fail; keeping the method reachable
    /// gives the slot a real synthesized body. The SCAN is what is cut — <c>Compilation.Reach</c>
    /// never enqueues the real IL — so the GetEnumInfo/GetEnumValuesAndNames (InternalCall) +
    /// RuntimeType-cache cascade never enters the tree. cut ⟹ route holds through the
    /// body: every mouth (direct <c>callvirt System.Enum::ToString()</c>, the itable slot, a
    /// boxed constrained dispatch) resolves to that one symbol, so nothing names a deleted
    /// body. The synthesized body calls only runtime helpers that dispatch on the boxed enum's
    /// runtime type, so it reaches no managed edge — unlike <see cref="BrHttpShim"/> there is
    /// no <c>ReachXxxEdges</c> companion.</para></summary>
    public static readonly NameKeyedIntercept BrEnumInstanceFormat = new(
        InterceptCutKind.BodyReplace, InterceptEmitArm.EnumInstanceFormatBody,
        extra: static (dt, n) => IsEnumInstanceFormatMethod(dt, n));

    /// <summary>The core BCL bounded set (<see cref="IsBoundedMethod"/>): body cut at
    /// reachability, call site neutralized to the default result.
    ///
    /// <para><b>This row is the CORE set only, and the asker that consults it merges two
    /// more.</b> <c>Compilation.IsBoundedMethod</c> unions this row with the backend's
    /// <c>IEmitBackend.AdditionalBoundedMethods</c> and the CLI's <c>--cut</c> specs —
    /// both per-instance, neither knowable from a static table — and every asker
    /// (<c>Compilation.Reach</c>, <c>MethodCompiler.EmitManagedCall</c>, the ldftn stub)
    /// asks THAT method, not this row directly. So the registry owns what a registry can
    /// own, the member set dn2cpp itself intercepts; the instance union stays where the
    /// instance state is. Reading this row as the whole bounded set would silently
    /// under-report a backend's cuts.</para></summary>
    public static readonly NameKeyedIntercept BdCoreBounded = new(
        InterceptCutKind.Bounded, InterceptEmitArm.BoundedNeutralize,
        extra: static (dt, n) => dt is not null && IsBoundedMethod(dt, n));

    /// <summary>The bounded virtual whose callvirt sites keep dispatching
    /// (<see cref="IsVirtualDispatchBounded"/> — SynchronizationContext.Post): only the
    /// base BODY is cut. A carve-out ON <see cref="BdCoreBounded"/> rather than a row
    /// beside it — every member it names is also in the core bounded set — so the emit
    /// site asks both, in that order.</summary>
    public static readonly NameKeyedIntercept BdVirtualDispatch = new(
        InterceptCutKind.Bounded, InterceptEmitArm.BoundedVirtualDispatch,
        extra: static (dt, n) => dt is not null && IsVirtualDispatchBounded(dt, n));

    /// <summary>The base Stream async funnels
    /// (<see cref="IsStreamSyncOverAsyncFunnel"/>): bounded — their bodies are in the
    /// core set and stay cut — but rewritten at the call site instead of neutralized,
    /// because the default they would otherwise get is a NULL Task and awaiting one is a
    /// nullptr dereference. Like <see cref="BdVirtualDispatch"/> this is a carve-out on
    /// <see cref="BdCoreBounded"/>, and BOTH askers need it: the emitter to rewrite, and
    /// <c>Compilation.Reach</c> to reach the sync slot the rewrite dispatches
    /// through.</summary>
    public static readonly NameKeyedIntercept BdStreamSyncFunnel = new(
        InterceptCutKind.Bounded, InterceptEmitArm.StreamSyncOverAsyncRewrite,
        extra: static (dt, n) => dt is not null && IsStreamSyncOverAsyncFunnel(dt, n));

    /// <summary>The dynamic-code-generation surface
    /// (<see cref="IsDynamicCodegenMember"/>): body cut at reachability, every call /
    /// ldftn / newobj site lowered to a catchable PlatformNotSupportedException naming
    /// the member.
    ///
    /// <para>Core-set only, for the same reason as <see cref="BdCoreBounded"/> though a
    /// different one in kind: <c>Compilation.IsDynamicCodegenMember</c> wraps this row in
    /// a namespace prefilter and keys it on the ARITY-STRIPPED open-definition name
    /// (<c>GenericDefFullName</c>), so that CallSite&lt;T&gt; / Expression&lt;TDelegate&gt;
    /// instantiations match their one entry. Both of those need the Compilation, so the
    /// askers go through that method; the row carries the name set.</para></summary>
    public static readonly NameKeyedIntercept BdDynamicCodegen = new(
        InterceptCutKind.Bounded, InterceptEmitArm.DynamicCodegenThrow,
        extra: static (dt, n) => dt is not null && IsDynamicCodegenMember(dt, n));

    /// <summary>The absent socket / name-resolution platform layer
    /// (<see cref="IsAbsentNetworkPalMember"/>): body cut at reachability, every call /
    /// ldftn / ldvirtftn / newobj site lowered to a catchable
    /// PlatformNotSupportedException naming the member.
    ///
    /// <para>Core-set only, exactly as <see cref="BdDynamicCodegen"/> is and for the same
    /// mechanical reason: <c>Compilation.IsAbsentNetworkPalMember</c> wraps this row in a
    /// namespace prefilter and keys it on the ARITY-STRIPPED open-definition name, and both
    /// of those need the Compilation, so the askers go through that method while the row
    /// carries the member set.</para>
    ///
    /// <para><b>It sits beside the dynamic-codegen row rather than in the bounded table
    /// because its failure direction is the loud one twice over.</b> A caller that asked for
    /// a connected socket cannot proceed without one, so <see cref="BoundedNeutralize"/>'s
    /// zero would be a null Task somebody awaits or a null Socket somebody configures — and,
    /// unlike every other row here, the alternative to cutting is not a wrong ANSWER at all:
    /// <c>libSystem.Native</c> is runtime-provided, so an admitted socket import lowers to a
    /// direct native call and the failure arrives as an undefined symbol at C++ link time
    /// (see <see cref="IsAbsentNetworkPalType"/>).</para></summary>
    public static readonly NameKeyedIntercept BdAbsentNetworkPal = new(
        InterceptCutKind.Bounded, InterceptEmitArm.AbsentNetworkPalThrow,
        extra: static (dt, n) => dt is not null && IsAbsentNetworkPalMember(dt, n));

    /// <summary>Every BodyReplace/Bounded row. A REGISTRY, not a chain, like the four
    /// tables above — and, like them, two of these rows deliberately OVERLAP
    /// (<see cref="BdVirtualDispatch"/> and <see cref="BdStreamSyncFunnel"/> both name
    /// members of <see cref="BdCoreBounded"/>), because they are carve-outs on it rather
    /// than alternatives to it. There is no first-match walk to be confused by that; each
    /// asker names the rows it needs at its own site.</summary>
    public static readonly NameKeyedIntercept[] BoundedIntercepts =
    [
        BrHttpShim,
        BrEnumInstanceFormat,
        BdCoreBounded,
        BdVirtualDispatch,
        BdStreamSyncFunnel,
        BdDynamicCodegen,
        BdAbsentNetworkPal,
    ];

    // ---- The registry self-check ----
    //
    // It sees the STRUCTURE of a row: that an arm was named, that the gates are
    // well-formed, that the cut and scan kinds are ones the row's own table admits, that
    // no two rows in a table match exactly the same tokens.
    //
    // It CANNOT see whether a row is referenced by both of its askers — the invariant the
    // registry exists to serve — because that is a property of the CALL SITES in
    // Compilation.Resolve.cs, Compilation.Reachability.cs and MethodCompiler.Call.cs, not
    // of anything reachable from an array of rows. Wiring a row into only one asker still
    // compiles and still passes this check; what catches THAT is
    // CppEmitter.AssertCalledBodiesEmitted, and only if the corpus walks the orphaned call.

    /// <summary>Whether <see cref="VerifyInterceptRegistry"/> actually walks the tables.
    /// Debug arms it unconditionally; Release behind <c>DN2CPP_INTERCEPT_SELFCHECK=1</c>.
    /// Environment-driven is sound here for the reason a cap is (AGENTS.md): it can only
    /// turn a run into an abort or back, never change the C++ a successful transpile
    /// emits — every failure it reports is a malformed table, which is a transpiler bug,
    /// not bad input.</summary>
    private static bool RegistrySelfCheckEnabled
    {
        get
        {
#if DEBUG
            return true;
#else
            return EnvKnobs.BoolIsOne(EnvKnobs.InterceptSelfCheck);
#endif
        }
    }

    /// <summary>Walks every descriptor table and throws on a malformed row. Called once per
    /// transpile from <c>Compilation.Build</c>; see the block comment above for the limit on
    /// what a table walk can decide.
    ///
    /// <para>An <see cref="InvalidOperationException"/> and not a
    /// <see cref="NotSupportedException"/>, deliberately: a malformed row is an internal
    /// invariant violation, so it crashes raw with its stack rather than being rendered
    /// as a polite <c>error:</c> line (AGENTS.md, the exception contract).</para></summary>
    public static void VerifyInterceptRegistry()
    {
        if (!RegistrySelfCheckEnabled)
            return;

        for (int i = 0; i < MethodDefIntercepts.Length; i++)
        {
            var r = MethodDefIntercepts[i];
            CheckRow("MethodDefIntercepts", i, r.EmitArm, r.TypeGate, r.NameGate);
            CheckCallSiteCutKind("MethodDefIntercepts", i, r.CutKind);
            for (int j = 0; j < i; j++)
            {
                var p = MethodDefIntercepts[j];
                if (p.Extra is null && r.Extra is null
                    && p.TypeGate == r.TypeGate && p.NameGate == r.NameGate)
                    throw Indistinguishable("MethodDefIntercepts", j, i, r.TypeGate, r.NameGate);
            }
        }

        for (int i = 0; i < MemberRefIntercepts.Length; i++)
        {
            var r = MemberRefIntercepts[i];
            CheckRow("MemberRefIntercepts", i, r.EmitArm, r.TypeGate, r.NameGate);
            CheckCallSiteCutKind("MemberRefIntercepts", i, r.CutKind);
            for (int j = 0; j < i; j++)
            {
                var p = MemberRefIntercepts[j];
                if (p.Extra is null && r.Extra is null
                    && p.TypeGate == r.TypeGate && p.NameGate == r.NameGate)
                    throw Indistinguishable("MemberRefIntercepts", j, i, r.TypeGate, r.NameGate);
            }
        }

        CheckNameKeyedTable("MethodSpecIntercepts", MethodSpecIntercepts, false, false);
        CheckNameKeyedTable("ConstrainedVirtualIntercepts", ConstrainedVirtualIntercepts, false, false);
        CheckNameKeyedTable("ScanIntercepts", ScanIntercepts, true, false);
        CheckNameKeyedTable("BoundedIntercepts", BoundedIntercepts, false, true);
    }

    /// <summary>The checks every row shape shares: an arm was named, and neither gate is
    /// an empty string (a null gate means "no gate"; an empty one is a typo that would
    /// match nothing and read as a live row).</summary>
    private static void CheckRow(string table, int i, InterceptEmitArm arm,
        string? typeGate, string? nameGate)
    {
        if (arm == InterceptEmitArm.Unspecified)
            throw new InvalidOperationException(
                $"intercept registry: {table}[{i}] names no emit arm "
                + "(InterceptEmitArm.Unspecified is the reject sentinel, never a route)");
        if (typeGate is not null && typeGate.Length == 0)
            throw new InvalidOperationException(
                $"intercept registry: {table}[{i}] has an empty TypeGate "
                + "(use null for \"no gate\"; \"\" matches nothing)");
        if (nameGate is not null && nameGate.Length == 0)
            throw new InvalidOperationException(
                $"intercept registry: {table}[{i}] has an empty NameGate "
                + "(use null for \"no gate\"; \"\" matches nothing)");
    }

    /// <summary>The call-token tables answer a resolver that either deletes an edge or
    /// leaves it alone. BodyReplace and Bounded belong to the reachability DRAIN, whose
    /// rows live in <see cref="BoundedIntercepts"/>; one appearing here would be asked by
    /// a resolver that has no way to honour it.</summary>
    private static void CheckCallSiteCutKind(string table, int i, InterceptCutKind cut)
    {
        if (cut is not (InterceptCutKind.Cut or InterceptCutKind.None))
            throw new InvalidOperationException(
                $"intercept registry: {table}[{i}] carries cut kind {cut}, which belongs to "
                + "the reachability drain (BoundedIntercepts), not a call-token table");
    }

    /// <summary>The three name-keyed tables' shared walk. <paramref name="scanTable"/>
    /// demands a scan effect on every row and forbids one everywhere else — the scan is a
    /// third asker, not a rename of the cut, so a stray effect on a call-token row is a
    /// row filed in the wrong table. <paramref name="drainTable"/> flips the cut-kind
    /// admission the other way.</summary>
    private static void CheckNameKeyedTable(string table, NameKeyedIntercept[] rows,
        bool scanTable, bool drainTable)
    {
        for (int i = 0; i < rows.Length; i++)
        {
            var r = rows[i];
            CheckRow(table, i, r.EmitArm, r.TypeGate, r.NameGate);
            if (drainTable)
            {
                if (r.CutKind is not (InterceptCutKind.Bounded or InterceptCutKind.BodyReplace))
                    throw new InvalidOperationException(
                        $"intercept registry: {table}[{i}] carries cut kind {r.CutKind}; the "
                        + "drain table admits only Bounded and BodyReplace");
            }
            else
            {
                CheckCallSiteCutKind(table, i, r.CutKind);
            }
            if (scanTable != (r.ScanEffect != InterceptScanEffect.None))
                throw new InvalidOperationException(
                    $"intercept registry: {table}[{i}] has scan effect {r.ScanEffect}, which "
                    + (scanTable
                        ? "leaves a ScanIntercepts row with no continuation kind"
                        : "belongs to a ScanIntercepts row — the scan is a third asker, not "
                          + "a rename of the cut"));
            // MarkThenResolve is pure addition: it records a use and then resolves the
            // token as it would have anyway. A row that also CUT would be recording a use
            // of an edge it deletes in the same breath.
            if (r.ScanEffect == InterceptScanEffect.MarkThenResolve
                && r.CutKind != InterceptCutKind.None)
                throw new InvalidOperationException(
                    $"intercept registry: {table}[{i}] is MarkThenResolve but cuts "
                    + $"({r.CutKind}); a row that resolves the token as usual cannot also "
                    + "delete the edge");
            // Two rows deliberately OVERLAP in BoundedIntercepts (the carve-outs on
            // BdCoreBounded), so overlap is not the test — indistinguishability is. Two
            // rows with the same gates and no Extra match exactly the same tokens, which
            // makes the later one dead.
            for (int j = 0; j < i; j++)
            {
                var p = rows[j];
                if (p.Extra is null && r.Extra is null
                    && p.TypeGate == r.TypeGate && p.NameGate == r.NameGate)
                    throw Indistinguishable(table, j, i, r.TypeGate, r.NameGate);
            }
        }
    }

    private static InvalidOperationException Indistinguishable(string table, int j, int i,
        string? typeGate, string? nameGate) =>
        new($"intercept registry: {table}[{j}] and {table}[{i}] are indistinguishable "
            + $"(TypeGate={typeGate ?? "<null>"}, NameGate={nameGate ?? "<null>"}, no Extra "
            + "on either), so they match exactly the same tokens and one of them is dead");
}
