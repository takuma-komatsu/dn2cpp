using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using SRME = System.Reflection.Metadata.Ecma335.MetadataTokens;

namespace Dn2Cpp;

/// <summary>The kind bits of an open generic DEFINITION a body took <c>typeof</c> of, so a
/// synthetic <c>gendef_</c> handle — one for a definition no closed instantiation minted a
/// handle for — answers <c>Type.IsInterface</c>/<c>IsAbstract</c>/<c>IsValueType</c>/
/// <c>IsSealed</c> as real .NET does. A natural gendef reads these off its closed
/// instantiation instead. <see cref="Unknown"/> is the best-effort degrade for a token site
/// that could not resolve the definition's attributes: a cross-assembly <c>External</c> name
/// dn2cpp minted no ClassInfo for and cannot open.</summary>
[System.Flags]
internal enum GenericDefKind
{
    Unknown = 0,
    ValueType = 1,
    Interface = 2,
    Abstract = 4,
    Sealed = 8,
}

/// <summary>The monomorphization bound was passed
/// (<see cref="Compilation.CheckInstantiationBound"/>). A <see cref="NotSupportedException"/>,
/// so <c>TranspileDriver</c> still renders it as an <c>error:</c> line and exit code 2 — but a
/// DISTINCT type, because <c>--measure</c>'s gap-row arms swallow every other exception and
/// carry on. Swallowing this one would feed the overrun it guards: every row recorded is one
/// more allocation on a list that is already too big. It must escape, exactly as
/// <see cref="MemoryGuard"/>'s overrun does.</summary>
internal sealed class InstantiationBoundException : NotSupportedException
{
    public InstantiationBoundException(string message) : base(message) { }
}

/// <summary>DN2CPP_STRICT_COMPLETION only: a specialization's members were read without
/// anyone asking for them. Derives from Exception rather than NotSupportedException because
/// the transpiler is full of arms that catch a NotSupportedException/InvalidOperationException
/// from a speculative compile and record it as "this body cannot be shared" or "this gap is
/// measured". Swallowed there, a strict violation would not fail the run — it would quietly
/// change WHICH bodies get shared while the audit reported clean. This one has to escape
/// everything.</summary>
internal sealed class StrictCompletionException : Exception
{
    public StrictCompletionException(string message) : base(message) { }
}

/// <summary>One loaded assembly. Owns its metadata reader and the per-module
/// handle maps (handles are only unique within a single reader).</summary>
internal sealed class Module
{
    public required int Index;
    public required PEReader PE;
    public required MetadataReader Reader;
    public required string AssemblyName;
    /// <summary>The compilation this module belongs to; a <see cref="ClassInfo"/> reaches it
    /// through here to decode its own members on demand. A field rather than a static for the
    /// same reason <see cref="TemplateDescs"/> is: the transpiler builds more than one
    /// compilation in one process.</summary>
    public required Compilation Owner;

    public readonly Dictionary<TypeDefinitionHandle, ClassInfo> ClassMap = new();
    public readonly Dictionary<MethodDefinitionHandle, MethodInfo> MethodMap = new();
    public readonly HashSet<TypeDefinitionHandle> GenericTemplates = new();
    public readonly HashSet<MethodDefinitionHandle> GenericMethodTemplates = new();

    /// <summary>Interned open-generic-definition descriptors, keyed by the template's
    /// TypeDef. Filled on demand by <see cref="TypeDesc.MakeTemplate"/>; see the
    /// remarks there for why these are worth interning and why the cache lives on the
    /// module rather than in a static.</summary>
    public Dictionary<TypeDefinitionHandle, TypeDesc>? TemplateDescs;

    /// <summary>Per-module caches of <see cref="ThrowHelperResources"/>'s metadata reads
    /// (the ThrowHelper TypeDef, its sinks' decoded resource/argument sources, and the two
    /// exception enums' value -> member-name maps). Here rather than in a static for the
    /// reason <see cref="TemplateDescs"/> is, and per module because System.ThrowHelper,
    /// its enums and its SR are all per-assembly polyfills.</summary>
    public TypeDefinitionHandle? ThrowHelperType;
    public Dictionary<(string, int), (ThrowHelperResources.Source? Res, ThrowHelperResources.Source? Arg)>? ThrowHelperSinks;
    public Dictionary<string, Dictionary<int, string>>? ExceptionEnums;
}

/// <summary>Where a compilation stands in the emit pipeline. The pipeline's order is a
/// contract, not a convention: the type-layout closure must run BEFORE the planning pass
/// (so the instantiations a field-type decode mints are grouped before any shareability
/// verdict), the verdicts must be computed exactly once over the completed planning
/// fixpoint, and the registries must be reset in the one window between the verdicts and
/// the emission pass that refills them. Misordering any of these does not fail — it
/// quietly changes which bodies get shared and what the output names. The phase machine
/// makes it loud: <see cref="Compilation.EnterPhase"/> owns the legal transitions, and each
/// phase-sensitive entry point asserts the phase it is legal in, throwing
/// <see cref="InvalidOperationException"/> (a transpiler bug, never bad input). Assert-only:
/// a run that respects the protocol is byte-identical with the machine present or
/// absent.</summary>
internal enum EmitPhase
{
    /// <summary>Build: pass 2, the monomorphization/reachability fixpoint, and Build's
    /// own <c>SyncSharedGenerics</c> call. The initial phase.</summary>
    Discovery,
    /// <summary>The emit path's FIRST <c>ComputeEmitted</c> call, ahead of planning: its
    /// field-type decodes mint closed generics that the planning rounds must still get
    /// to group. (<c>--measure</c> skips this phase — its guarded closure runs last,
    /// inside Emission, where a failure is a gap row rather than a phase.)</summary>
    LayoutClosure,
    /// <summary>The shared-generics planning pass: every reachable body trial-compiled
    /// for its side effects (taint, call edges, rgctx slots), text discarded.</summary>
    Planning,
    /// <summary>Verdicts assigned (<c>FinalizeSharedGenerics</c>); the window in which
    /// the planning registries/state are reset for the emission pass.</summary>
    Finalized,
    /// <summary>The real pass — the only body compilation whose text (and named
    /// symbols) reach the output. Terminal.</summary>
    Emission,
}

/// <summary>What a bounded row's substitute DOES when the bounded method turns out to be
/// a bodyless <c>[DllImport]</c>: a column on the core bounded table
/// (<c>CoreIntrinsics.BoundedVerdictOf</c>), honoured at both substitute mouths. The question
/// a new row answers is "is a zero here an ANSWER or a SUBSTITUTE?"; neither value is safe as
/// a blanket rule, so there deliberately is no default.</summary>
internal enum BoundedVerdict
{
    /// <summary>The default is the correct answer for a dn2cpp binary, so the call site
    /// keeps answering it with nothing said: no managed debugger IS attached, no COM
    /// object DOES exist, an assembly DOES belong to no custom load context. A caller that
    /// could tell the difference would be told a lie by any other value.</summary>
    Silent,

    /// <summary>The caller asked the native world for data this build cannot obtain, and
    /// the zero is indistinguishable from a real result (an empty process name, a null
    /// module handle). The call site throws a catchable
    /// <c>PlatformNotSupportedException</c> naming the module, the entry point, the
    /// managed thunk and the remedy — the posture <c>dn2cpp_require_metadata</c> takes for
    /// a stripped type. Catchable, not fatal: a program that can carry on without the datum
    /// can only write that <c>catch</c> if the failure is visible at all.</summary>
    Loud,
}

/// <summary>One native import bounded to a substitute: the <c>[DllImport]</c> module and
/// entry point that will never be called, the managed P/Invoke thunk whose call sites were
/// answered instead, and WHAT they were answered with (<see cref="BoundedVerdict"/>).
/// Deliberately does not carry bounded <em>managed</em> methods — a managed bound substitutes
/// a modelled no-op, whereas an import stands in for a native module the build does not
/// provide.</summary>
internal readonly record struct BoundedImport(
    string Module, string EntryPoint, string Method, BoundedVerdict Verdict);

/// <summary>One delegate adapter: the static method a delegate was created over,
/// plus what the delegate's target slot MEANS for it. An <b>open</b> static delegate
/// leaves the slot unused — its <c>Invoke</c> and the static agree on their arguments;
/// a <b>closed</b> one carries the static's bound FIRST argument in it, so its
/// <c>Invoke</c> takes one argument fewer (<c>arr.Contains</c> converted to a
/// <c>Func&lt;T, bool&gt;</c> — the extension-method group conversion).
/// <para>The pair is the key, not the method: the two shapes have different C++
/// signatures, and one static reached both ways needs both bodies. Keying on the
/// method alone emits whichever shape was seen first and lets the other call it with its
/// arguments shifted by one — a silent wrong answer, or a segfault when the bound receiver
/// lands in an argument slot typed as a pointer.</para></summary>
/// <para><see cref="NfiErased"/> marks a third shape, and it answers a different question:
/// not what the target slot means but what <c>f_method</c>'s ABI IS. A wrappable HEADERLESS
/// intrinsic position (the NFI trio's <c>const Dn2CppNumberFormatInfo*</c>, Assembly/Module's
/// <c>const char*</c>) is a pointer that is not an object, so the assumption behind the raw
/// function pointer — "the variance a delegate binding permits is reference-to-reference,
/// which renders to a pointer on either side" — is false there. Variance makes it unfixable
/// at the invoke site, since one delegate instance can be invoked through two delegate types
/// (a <c>Func&lt;CultureInfo&gt;</c> assigned to a <c>Func&lt;object&gt;</c> emits no IL at
/// all). So <c>f_method</c> is given ONE canonical ABI: every headerless position erases to
/// <c>Dn2CppObject*</c>. This adapter converts the target's real signature to that ABI and
/// the delegate invoker converts back; the two sides derive the erasure independently and
/// cannot disagree, since the only spellings that can differ both erase to the same thing.
/// It is why an INSTANCE target can need an adapter at all.</para>
internal readonly record struct DelegateAdapter(MethodInfo Method, bool Closed, bool NfiErased = false)
{
    /// <summary>The adapter's C++ symbol — the single definition both mouths use (the
    /// <c>ldftn</c> site that names it and the emitter that defines it). A method's
    /// <see cref="MethodInfo.CppName"/> always starts <c>m_</c>, so neither prefix can
    /// collide with a plain adapter's name.</summary>
    public string CppName => NfiErased
        ? $"dgadapn_{(Closed ? "closed_" : "")}{Method.CppName}"
        : Closed ? $"dgadap_closed_{Method.CppName}" : $"dgadap_{Method.CppName}";
}

/// <summary>
/// Loads the input assemblies and builds the type/method/vtable model used by codegen.
/// </summary>
internal sealed partial class Compilation
{
    public List<Module> Modules { get; } = new();
    public Module AppModule => Modules[0];
    public PEReader PE => AppModule.PE;
    public MetadataReader Reader => AppModule.Reader;
    public SignatureProvider SigProvider { get; }
    public List<ClassInfo> Classes { get; } = new();
    public MethodInfo? EntryPoint { get; private set; }

    /// <summary>Hot-update patch converter hook (null in the normal pipeline).
    /// A closed generic instantiation of a base-image type cannot be modeled as
    /// a loaded <see cref="ClassInfo"/> — the converter never loads the base —
    /// so this resolves the instantiation to its mangled registry name against
    /// the base-image ABI manifest, or returns null for an instantiation the
    /// base image never emitted (the HybridCLR missing-AOT-instantiation
    /// boundary). Given the open-definition name (with its arity backtick, e.g.
    /// <c>System.Collections.Generic.List`1</c>) and the closed type arguments.</summary>
    public Func<string, TypeDesc[], TypeDesc?>? ExternalGenericResolver { get; set; }

    /// <summary>Methods reachable from the program roots; only these have
    /// their bodies decoded and emitted (tree-shaking). This is what lets us
    /// pull in large library assemblies without compiling unused code.</summary>
    public ReachableSet Reachable { get; } = new();

    /// <summary>Static methods needing a target-slot adapter because a delegate was
    /// created over them, each paired with the shape that delegate wants (populated
    /// during method compilation; see <see cref="DelegateAdapter"/>).</summary>
    public List<DelegateAdapter> DelegateAdapters { get; } = new();

    /// <summary>Every delegate class some emitted TEXT names an invoker for
    /// (<c>dginvoke_&lt;CppName&gt;</c>) — recorded at the mouths that spell the name
    /// (the Invoke call site, the sort-comparer thunk), and unioned into
    /// <c>CppEmitter.EmitDelegateInvokers</c>'s per-emitted-class walk. The union covers a
    /// delegate type that exists only as a VARIANCE view (<c>Func&lt;object&gt; fo = fs;</c>
    /// with <c>Func&lt;object&gt;</c> never constructed): it is invokable, yet the assignment
    /// emits no IL, so nothing puts the class in the emit set and its <c>dginvoke_*</c>
    /// would be a green transpile that dies in clang on an undeclared identifier. An invoker
    /// is emitted text, not a compiled body, so <c>AssertCalledBodiesEmitted</c> cannot see
    /// the miss. A planning-pass note whose final body never spells the name costs one
    /// unused invoker — bloat, never a link error.</summary>
    public HashSet<ClassInfo> DelegateInvokerUses { get; } = new();

    /// <summary>Canonical shared generics (opt-in via <c>--shared-generics</c>):
    /// group generic instantiations whose C++ layout coincides under a canonical
    /// owner instantiation (see <see cref="CanonicalGenerics"/>). In this stage
    /// the grouping is pure model linkage (<see cref="ClassInfo.SharedOwner"/> /
    /// <see cref="ClassInfo.SharedUsers"/>) — emission is unchanged until shared
    /// body emission lands.</summary>
    public bool SharedGenericsEnabled { get; }

    /// <summary>Opt-in shadow stack (<c>--shadow-stack</c>): every body
    /// compiled through <c>MethodCompiler.Compile</c> opens with a
    /// <c>Dn2CppShadowFrame</c> RAII guard baking its frame name, so
    /// <c>dn2cpp_exc_stamp_trace</c> records exact throw traces (kind 1) that
    /// survive -O2 inlining and exist on WASM. Off by default — the guard is a
    /// per-call cost and the flag changes the emitted C++, which is why it is
    /// a CLI flag and never an environment variable (self-host fixpoint).</summary>
    public bool ShadowStackEnabled { get; }

    // Generic support (shared across modules).
    private readonly Dictionary<MetadataReader, Module> _byReader = new();
    // Instantiation caches: two-level — (module, template token) outer, a
    // structural key over the argument vector inner. The inner comparers must
    // reproduce joined-mangle STRING equality EXACTLY (see TypeArgsComparer): the
    // mapping joined-string <-> (module, token, fragment vector) is a bijection,
    // because no mangle fragment can contain the join separators (',' / ':' / '<'),
    // so no equality class splits or merges. The method cache keys on
    // declClass.CppName — the STRING, not the ClassInfo reference: same-CppName
    // twins deliberately collide onto one MethodInfo, and a reference key would
    // silently mint two.
    // Neither dictionary is ever enumerated (order stays out of the output);
    // _instanceCount/_methodInstanceCount stand in for the flat .Count the
    // instantiation bound and the census line read.
    private readonly Dictionary<(int Module, int Token), Dictionary<TypeDesc[], ClassInfo>> _instances = new();
    private int _instanceCount;
    private readonly Dictionary<(int Module, int Token), Dictionary<(string Decl, TypeDesc[] Args), MethodInfo>> _methodInstances = new();
    private int _methodInstanceCount;

    private Dictionary<TypeDesc[], ClassInfo> InstancesFor(Module module, TypeDefinitionHandle template)
    {
        var key = (module.Index, SRME.GetToken(template));
        if (!_instances.TryGetValue(key, out var inner))
            _instances[key] = inner = new Dictionary<TypeDesc[], ClassInfo>(TypeArgsComparer.Instance);
        return inner;
    }

    public ClassInfo? FindGenericInstantiation(string @namespace, string name, TypeDesc[] args)
    {
        if (!TypeIndex().TryGetValue((@namespace, name), out var definitions)
            || definitions.Count == 0)
            return null;
        foreach (var (module, handle) in definitions)
        {
            if (InstancesFor(module, handle).TryGetValue(args, out var instance))
                return instance;
        }
        return null;
    }

    private Dictionary<(string Decl, TypeDesc[] Args), MethodInfo> MethodInstancesFor(Module module, MethodDefinitionHandle template)
    {
        var key = (module.Index, SRME.GetToken(template));
        if (!_methodInstances.TryGetValue(key, out var inner))
            _methodInstances[key] = inner = new Dictionary<(string, TypeDesc[]), MethodInfo>(MethodInstanceKeyComparer.Instance);
        return inner;
    }

    /// <summary>Structural equality over a generic-argument vector, reproducing
    /// <see cref="MangleArg"/> STRING equality exactly — two vectors are equal iff their
    /// joined mangles were: elementwise fragment comparison, and the fragments contain no
    /// join separator, so the per-element split is unambiguous. "Exactly" is the whole
    /// contract — this comparer must never know more, or less, than the mangle does; it owns
    /// no rule of its own. What it reproduces deliberately is the same-CppName conflation of
    /// two distinct NAMED types, which the model is built on.
    /// Hashing is plain unchecked arithmetic, not System.HashCode (whose
    /// cctor-seeded state the self-host must not consume at static-init — see the
    /// s_primitives note in Model.cs); the hash never reaches the output, since no
    /// dictionary keyed by this comparer is ever enumerated.</summary>
    private sealed class TypeArgsComparer : IEqualityComparer<TypeDesc[]>
    {
        public static readonly TypeArgsComparer Instance = new();

        /// <summary>One argument's identity under the instantiation-cache mangle — the
        /// shared element test <see cref="SameTypeArg"/> also answers with.</summary>
        internal static bool ElementEquals(TypeDesc a, TypeDesc b) =>
            ReferenceEquals(a, b) || MangleArg(a) == MangleArg(b);

        public bool Equals(TypeDesc[]? x, TypeDesc[]? y)
        {
            if (ReferenceEquals(x, y))
                return true;
            if (x is null || y is null || x.Length != y.Length)
                return false;
            for (int i = 0; i < x.Length; i++)
                if (!ElementEquals(x[i], y[i]))
                    return false;
            return true;
        }

        public int GetHashCode(TypeDesc[] args)
        {
            unchecked
            {
                int h = 17;
                foreach (var t in args)
                    h = h * 31 + MangleArg(t).GetHashCode();
                return h;
            }
        }
    }

    /// <summary>Key comparer for the generic-method instantiation cache: the declaring
    /// class's CppName (ordinal — the string, preserving the same-CppName
    /// twin-collision semantics) plus the argument vector under <see cref="TypeArgsComparer"/>.</summary>
    private sealed class MethodInstanceKeyComparer : IEqualityComparer<(string Decl, TypeDesc[] Args)>
    {
        public static readonly MethodInstanceKeyComparer Instance = new();

        public bool Equals((string Decl, TypeDesc[] Args) x, (string Decl, TypeDesc[] Args) y) =>
            string.Equals(x.Decl, y.Decl, StringComparison.Ordinal)
            && TypeArgsComparer.Instance.Equals(x.Args, y.Args);

        public int GetHashCode((string Decl, TypeDesc[] Args) k)
        {
            unchecked
            {
                return k.Decl.GetHashCode() * 31 + TypeArgsComparer.Instance.GetHashCode(k.Args);
            }
        }
    }

    // ResolveMemberRefMethod memo. A MemberRef is visited repeatedly (scan, planning
    // compile, real compile) and the candidate set is not strictly time-invariant:
    // InstantiateMethodOnClass appends generic-method instantiations under their plain
    // template name, so a later uncached visit could see — and through the wantKey
    // tie-break prefer — a same-name/same-arity instance the first visit did not. The memo
    // pins every visit to the deterministic first-visit answer, which is what byte-identity
    // is measured against. For a TypeRef parent the class is ctx-independent, so
    // (module, token) is the whole key; for a TypeSpec parent the class depends on the
    // caller's generic context, so the parent is decoded per visit and the class rides in
    // the key. Keys are decode-free and the dictionaries are never enumerated, so their
    // order cannot reach the output. Failures are not cached: a throwing resolution re-runs
    // and throws identically.
    private readonly Dictionary<(int Module, int Token), MethodInfo> _memberRefMethodsByTypeRef = new();
    private readonly Dictionary<(int Module, int Token, ClassInfo Cls), MethodInfo> _memberRefMethodsBySpec = new();
    // Generic-method instantiations in creation order, plus the cursor of the
    // incremental canonical-linking pass. This is the deterministic population
    // the method-dimension linkage and the finalize eligibility loop walk: the
    // instances sit on real and canonical classes alike, and the class-keyed
    // template map cannot address one of several instantiations sharing a
    // template handle.
    private readonly List<MethodInfo> _methodInstanceOrder = new();
    private int _methodInstanceLinkCursor;

    /// <summary>Specializations awaiting completion. Drained FIFO — or, with
    /// <c>DN2CPP_SPEC_DRAIN=lifo</c>, LIFO.
    /// <para>That knob is an <b>order probe</b>, and it is sound because both the
    /// reachable set and the specialization closure are least fixpoints of monotone
    /// operators: draining in the opposite order reaches the very same <i>set</i>,
    /// and changes only the order in which <see cref="Classes"/> grows. So a
    /// difference in the emitted bytes between the two drains is not a bug in either
    /// drain — it is proof that emission reads discovery order somewhere, and the
    /// diff says where. Once the emitter is independent of that order, the two must
    /// agree byte-for-byte, and a gate holds them to it.</para></summary>
    private static readonly bool DrainLifo = EnvKnobs.Raw(EnvKnobs.SpecDrain) == "lifo";
    private readonly Queue<ClassInfo> _pendingFifo = new();
    private readonly Stack<ClassInfo> _pendingLifo = new();

    private void EnqueuePending(ClassInfo spec)
    {
        if (DrainLifo)
            _pendingLifo.Push(spec);
        else
            _pendingFifo.Enqueue(spec);
    }

    private int PendingCount => DrainLifo ? _pendingLifo.Count : _pendingFifo.Count;

    private ClassInfo DequeuePending() => DrainLifo ? _pendingLifo.Pop() : _pendingFifo.Dequeue();

    /// <summary>Third-party async task-family types adopted into the intrinsic Task
    /// family: a type carrying <c>[AsyncMethodBuilder(typeof(B))]</c> — GDTask,
    /// UniTask, any hand-rolled task-like type — together with its builder and its
    /// awaiter. Keyed on the DEFINING <c>(module, TypeDef)</c> rather than a name: a
    /// closed specialization carries its template's handle, and a nested awaiter's
    /// <see cref="ClassInfo.FullName"/> is the bare simple name "Awaiter", which is not
    /// unique — so one probe serves the non-generic form and every specialization alike.
    /// The value is the C++ runtime struct the type lowers to, plus the BCL dispatch key
    /// its members answer to. See <see cref="AdoptCustomAsyncTaskTypes"/>.</summary>
    private readonly Dictionary<(int ModuleIndex, TypeDefinitionHandle Handle), (string Cpp, string Key)>
        _adoptedAsync = new();
    /// <summary>Assembly simple names whose [AsyncMethodBuilder] task types are NOT
    /// adopted (<c>--no-adopt-async</c>): their real IL — builder, promises, pools —
    /// transpiles through the general pipeline, making the library's whole API
    /// surface work rather than just the await plumbing. Per ASSEMBLY, not per type:
    /// a library like GDTask carries sibling task types (GDTask / GDTaskVoid) whose
    /// promises interlock, and adopting half would mix intrinsic and real promises.
    /// The opt-out lives here, at the registry's single fill site
    /// (<see cref="AdoptCustomAsyncTaskTypes"/>) — every consumer reads
    /// <see cref="_adoptedAsync"/> through AdoptedAsyncKey/AdoptedAsyncCpp/
    /// HasAdoptedAsync, so skipping a module at the fill returns every path to the
    /// general pipeline.</summary>
    private readonly IReadOnlyList<string> _noAdoptAsync;
    private readonly IEmitBackend? _backend;
    // Backend-contributed bounded methods (IEmitBackend.AdditionalBoundedMethods),
    // snapshotted at construction — merged with the core set by IsBoundedMethod.
    private readonly HashSet<(string Type, string Method)> _backendBoundedMethods;
    // CLI --cut specs (parsed "DeclType::Method" pairs) — the third set
    // IsBoundedMethod merges: same subtree-cut + call-site-neutralization
    // semantics as the backend set, as a per-run lever. Validated against the
    // loaded modules right after load (ValidateCutMethods). Matches on the
    // non-generic FullName, like the other bounded sets.
    private readonly HashSet<(string Type, string Method)> _cliCutMethods;
    /// <summary>Hot-update base build: extra closed generic instantiations to
    /// force-emit (the <c>hotupdate-refs.txt</c> roots — a patch may bind a
    /// base-image generic type the base program itself never uses, so the base
    /// must carry it, HybridCLR's AOTGenericReferences). Each is
    /// <c>OpenDef[arg,...]</c> with CLR type names, e.g.
    /// <c>GBase.Box[System.String]</c>.</summary>
    private readonly IReadOnlyList<string>? _genericRoots;

    /// <summary>Hot-update world (a <c>--hotupdate-base</c> build, or the patch
    /// converter's own model): the typeof-equality branch fold
    /// (<c>typeof(A)==typeof(B)</c>) is disabled — a patch may import an instantiation the fold would have
    /// pruned from the base image, and hot update's whole posture is "keep
    /// everything" (see the base-abi member tables). The const-folded GETTER
    /// verdicts stay on: they encode capabilities the runtime genuinely lacks
    /// either way.</summary>
    private readonly bool _hotUpdatePosture;

    /// <summary>The single construction path: every option is set on the record
    /// before the instance exists, so nothing can observe a half-configured
    /// Compilation — the "must be set before Build()" ordering in the constructor
    /// is structural, not disciplinary. <paramref name="assemblyPaths"/> is the
    /// load set the caller assembled (input + references + auto-referenced shims),
    /// not <see cref="TranspileOptions.Input"/>/References verbatim.</summary>
    public static Compilation Create(IReadOnlyList<string> assemblyPaths, TranspileOptions options) =>
        new(assemblyPaths, options);

    private Compilation(IReadOnlyList<string> assemblyPaths, TranspileOptions options)
    {
        _backend = options.Backend;
        // Must be set before Build(): the reachability drain computes the
        // typeof-equality branch folds, and a hot-update world must not prune (see the field).
        // A --hotupdate-base build IS a hot-update world, hence the OR.
        _hotUpdatePosture = options.HotUpdatePosture || options.HotupdateBase;
        _noAdoptAsync = options.NoAdoptAsync ?? Array.Empty<string>();
        _cliCutMethods = (options.CutMethods ?? Array.Empty<string>()).Select(ParseCutSpec).ToHashSet();
        _trimReflection = options.TrimReflection;
        _reflectionRoots = new HashSet<string>(options.ReflectionRoots ?? Array.Empty<string>(), StringComparer.Ordinal);
        _projectRoots = options.ProjectRoots;
        _linkFeatures = new HashSet<string>(options.LinkFeatures, StringComparer.Ordinal);
        _noManifestResources = new HashSet<string>(
            options.NoManifestResources ?? Array.Empty<string>(), StringComparer.Ordinal);
        _manifestResourceRoots = new HashSet<string>(
            options.ManifestResourceRoots ?? Array.Empty<string>(), StringComparer.Ordinal);
        _pinvokeModules = new HashSet<string>(options.PInvokeModules ?? Array.Empty<string>(), StringComparer.Ordinal);
        // Snapshot before Build(): the reachability drain consults IsBoundedMethod.
        _backendBoundedMethods = options.Backend is null
            ? new() : new(options.Backend.AdditionalBoundedMethods);
        _genericRoots = options.HotupdateRefs;
        SharedGenericsEnabled = options.SharedGenerics;
        ShadowStackEnabled = options.ShadowStack;
        // Grouped specializations name their canonical owner's struct layout
        // (ClassInfo.CppStructName) only when sharing is on; a per-process
        // switch because CppStructName has no compilation context.
        ClassInfo.ShareStructLayout = options.SharedGenerics;
        // Set before Build() so a patch signature naming a base-image generic
        // type resolves during eager signature decoding (hot-update converter).
        ExternalGenericResolver = options.ExternalGenericResolver;
        // Self-hosting feasibility harness (CppEmitter.MeasureGaps): collect
        // reachability-phase gaps instead of aborting at the first. Must be set
        // before Build() drives the reachability fixpoint.
        if (options.Measure)
            ReachabilityDiagnostics = new List<(MethodInfo? M, Exception Ex)>();
        // Phase instrumentation reports a heap number per phase; these are the
        // populations that explain it. Installed before the first LoadModule so
        // even the load phase's mark carries them.
        Timing.Populations = () =>
            $"classes {Classes.Count} reach {Reachable.Count} inst {_instanceCount}"
            + $" minst {_methodInstanceCount} sig {ModelCensus.SignaturesDecoded}"
            + $" fld {ModelCensus.FieldTypesDecoded} gdepth {MaxGenericArgDepth}";
        foreach (var path in assemblyPaths)
            LoadModule(path);
        // Transitive reference auto-resolution (opt-in): before Build(), pull in any
        // not-yet-loaded shared-framework definition assemblies the loaded modules
        // reference, so external generic instantiations resolve instead of aborting
        // signature decoding. A no-op when off (default) — the load set is then exactly
        // the passed paths and output is byte-identical.
        if (options.AutoRef)
            LoadReferenceClosure(assemblyPaths);
        // Conditional default references: the shim assemblies shipped beside the CLI,
        // each injected only when the BCL assembly it serves is already in the load set
        // (see InjectDefaultRefs for the table, the simple-name dedupe, and why an
        // unreached extra module still changes the emitted bytes). Off — and therefore
        // exactly today's output — when DefaultRefDir is null.
        //
        // THIS WINDOW IS THE ONLY ONE, in both directions:
        //  * not before the closure — System.Net.Http and System.IO.Compression are
        //    usually loaded BY the closure, not by an explicit -r. A tool-installed
        //    `dn2cpp app.dll --auto-ref` is exactly that shape, and it is the case the
        //    feature exists for; firing before the closure would leave every trigger
        //    absent and inject nothing.
        //  * not after Build() — Pass 1 mints a shell per type from Modules and Pass 2
        //    walks every module to register its [NativeImplementation] rows. A module
        //    appended afterwards is invisible to both, so its adapters would never
        //    register and its types would never exist.
        if (options.DefaultRefDir is { } defaultRefDir)
        {
            bool injected = InjectDefaultRefs(
                defaultRefDir, options.NoDefaultRefs ?? Array.Empty<string>());
            // A shim carries AssemblyRefs of its own (System.Memory, …). Left
            // unclosed, Pass 2's signature decode can abort on an external generic, so
            // re-run the closure over the grown module set. Cheap and idempotent: the
            // closure rebuilds `loaded` from Modules and seeds its queue with all of
            // Modules each time, so a second run re-walks the assembly-reference rows
            // already in memory and loads only what is genuinely new — no PE is read
            // twice.
            if (injected && options.AutoRef)
                LoadReferenceClosure(assemblyPaths);
        }
        // Runs after EVERY load path above (explicit -r, the auto-ref closure, the
        // injected default refs): a --no-manifest-resources naming an injected shim
        // or a closure-loaded BCL assembly is legitimate, so the name set to
        // validate against does not exist any earlier.
        ValidateManifestResourceFlags();
        Timing.Mark("load-modules");
        SigProvider = new SignatureProvider(this);
        Build();
    }

    /// <summary>Parses one <c>--cut</c> spec ("DeclType::Method") into the merge
    /// set. Malformed specs fail here, before any module loads.</summary>
    private static (string Type, string Method) ParseCutSpec(string spec)
    {
        int sep = spec.IndexOf("::", StringComparison.Ordinal);
        if (sep <= 0 || sep + 2 >= spec.Length)
            throw new NotSupportedException(
                $"--cut {spec}: expected \"DeclType::Method\" (e.g. \"GodotTask.TaskTracker::TrackActiveTask\")");
        return (spec[..sep], spec[(sep + 2)..]);
    }

    /// <summary>Every <c>--cut</c> spec must name a loaded type with a method of
    /// that name — a typo silently becoming a no-op cut is a footgun. Runs after
    /// Build() Pass 2 (member lists populated), before reachability consumes the
    /// cut set.</summary>
    private void ValidateCutMethods()
    {
        foreach (var (type, method) in _cliCutMethods)
        {
            var cls = FindClassByFullName(type);
            if (cls is null)
                throw new NotSupportedException(
                    $"--cut {type}::{method}: no loaded assembly declares type {type} "
                    + $"(loaded: {string.Join(", ", Modules.Select(m => m.AssemblyName))})");
            if (!cls.Methods.Any(m => m.Name == method))
                throw new NotSupportedException(
                    $"--cut {type}::{method}: type {type} has no method named {method}");
        }
    }

    /// <summary>Whether a specific method is bounded (subtree cut at reachability,
    /// call/ldftn sites neutralized): the core BCL set in
    /// <see cref="CoreIntrinsics.IsBoundedMethod"/> merged with the backend's
    /// <see cref="IEmitBackend.AdditionalBoundedMethods"/> and the CLI's
    /// <c>--cut</c> specs.
    ///
    /// <para>THIS is the merge point every asker goes through — the reachability drain,
    /// the call-site neutralization, the ldftn stub. The core half is asked as the
    /// descriptor row <see cref="CoreIntrinsics.BdCoreBounded"/>, which is where the cut
    /// kind and the emit arm are recorded; the other two sets are per-instance and cannot
    /// be static rows, so the union stays here.</para></summary>
    internal bool IsBoundedMethod(string declType, string name) =>
        CoreIntrinsics.BdCoreBounded.Matches(declType, name)
            || _backendBoundedMethods.Contains((declType, name))
            || _cliCutMethods.Contains((declType, name));

    /// <summary>Keyed by module, entry point and declaring method so two same-named
    /// overloads binding one entry point collapse and two entry points in one module do
    /// not. Insertion order is emit order, which is stable but not meaningful; every
    /// reader sorts (see <see cref="BoundedImports"/>).
    ///
    /// <para>The separator is U+0001 and not <c>'|'</c>, which makes the key's ORDINAL
    /// order identical to the (Module, EntryPoint, Method) ordinal order the report
    /// wants: U+0001 sorts below every character a module name, entry point or type
    /// name can contain, so no field's content can reach across a separator the way
    /// <c>'|'</c> (U+007C, above every letter and digit) lets it — <c>"a|z"</c> sorts
    /// after <c>"ab|a"</c> while <c>"az"</c> sorts before it. That is what lets
    /// <see cref="BoundedImports"/> sort strings instead of rows.</para></summary>
    private readonly Dictionary<string, BoundedImport> _boundedImports = new(StringComparer.Ordinal);

    /// <summary>The native imports this transpile bounded to a zero-returning default,
    /// ordered by module then entry point then declaring method — the report behind the
    /// driver's count line and the <c>--measure</c> sidecar.
    ///
    /// <para>Ordered through the KEYS rather than with <c>OrderBy(…).ThenBy(…)</c> over the
    /// rows, and that is a SELF-HOST constraint, not a preference: a LINQ ordering whose
    /// element is a value type mints <c>Enumerable.OrderedIterator&lt;BoundedImport,
    /// TKey&gt;</c>, whose ref-typed key instantiations share one canonical body — and the
    /// generic-virtual dispatcher for
    /// <c>IOrderedEnumerable&lt;T&gt;.CreateOrderedEnumerable</c> then branches on the
    /// canonical group OWNER's <c>ti_</c>, a type-info no class emits. The transpile is
    /// green and the C++ compile fails on an undeclared identifier. The key order above is
    /// defined to be the order this wants, so the two spellings are
    /// equivalent.</para></summary>
    internal IReadOnlyList<BoundedImport> BoundedImports
    {
        get
        {
            var keys = new List<string>(_boundedImports.Keys);
            keys.Sort(StringComparer.Ordinal);
            var rows = new List<BoundedImport>(keys.Count);
            foreach (string key in keys)
                rows.Add(_boundedImports[key]);
            return rows;
        }
    }

    /// <summary>Records a bounded callee that is a bodyless <c>[DllImport]</c> — an
    /// import of a native module this build does not provide, whose call site is about
    /// to be answered with a zero.
    ///
    /// <para>A bounded managed method's default is a modelled no-op some reader can still
    /// act on (a null comparer means "use the default one"); a bounded import's zero is
    /// indistinguishable from a real answer, and nothing else in the pipeline names such a
    /// row. The reachability drain's <c>m.Rva == 0</c> early-out fires <b>before</b> its
    /// bounded arm, so <c>--measure</c> records no gap; the cut keeps the import out of
    /// <c>pinvoke-libs.txt</c>, so the C++ link cannot name it either; and the transpile is
    /// green. This call site — the one that substitutes the zero — is the only place that
    /// knows.</para>
    ///
    /// <para>Recorded under <see cref="EmitPhase.Emission"/> only. The planning pass
    /// trial-compiles bodies for their side effects and discards the text, so a call site
    /// it walks may belong to no emitted body; the real pass is the set that ships. A
    /// <c>--measure</c> run reaches this too — its real pass runs under Emission as
    /// well. <b>The recording is phase-gated; the VERDICT is not</b> — see
    /// <see cref="TryBoundedImport"/>.</para></summary>
    internal void NoteBoundedImport(MethodInfo m)
    {
        if (Phase != EmitPhase.Emission || !TryBoundedImport(m, out var noted))
            return;
        string key = noted.Module + "\u0001" + noted.EntryPoint + "\u0001" + noted.Method;
        if (!_boundedImports.ContainsKey(key))
            _boundedImports.Add(key, noted);
    }

    /// <summary>Classifies a bounded callee: is it a bodyless <c>[DllImport]</c>, and if so
    /// what does its row say the substitute should do? <b>The one place both mouths and the
    /// report agree</b> — the call-site neutralization, the <c>ldftn</c> stub and
    /// <see cref="NoteBoundedImport"/> all go through it, so a row's verdict cannot be
    /// honoured at one mouth and quietly not at the other (AGENTS.md, "an intercept has
    /// askers on both sides").
    ///
    /// <para>Deliberately NOT phase-gated, unlike the recording above. The planning pass
    /// compiles bodies and throws the text away, but the text it produces has to be the text
    /// the real pass produces — that equality is what makes a planning verdict (which body
    /// can be shared) sound. Gate this on <see cref="Phase"/> and a shared canonical body
    /// could be chosen from a planning pass that emitted a zero and then ship from a real
    /// pass that emitted a throw.</para>
    ///
    /// <para>The verdict is read only after the import test, because a
    /// <see cref="BoundedVerdict.Loud"/> row with a real IL body would have no module to
    /// name; see the invariant written at <c>CoreIntrinsics.s_boundedMethods</c>. The caller
    /// has already established that the method is bounded at all — this does not re-ask
    /// <see cref="IsBoundedMethod"/>.</para></summary>
    internal bool TryBoundedImport(MethodInfo m, out BoundedImport row)
    {
        if (m.Rva != 0 || m.PInvoke is not { } pinv)
        {
            row = default;
            return false;
        }
        string declType = m.DeclaringClass.FullName;
        row = new BoundedImport(pinv.ModuleName, pinv.EntryPoint, declType + "." + m.Name,
            CoreIntrinsics.BoundedVerdictOf(declType, m.Name));
        return true;
    }

    /// <summary>The sentence a <see cref="BoundedVerdict.Loud"/> import's substitute throws,
    /// built once so the call-site mouth and the <c>ldftn</c> mouth cannot word it
    /// differently. Names the module, the entry point, the managed thunk that declared the
    /// import, and the remedy — <c>dn2cpp_require_metadata</c>'s shape, and for the same
    /// reason: whoever meets this is running a shipped binary, and a message that says only
    /// "unsupported" sends them to the wrong repository.
    ///
    /// <para>Returns the text ALREADY ESCAPED for a raw C string literal (the CppEmitter
    /// trap-body idiom, as <c>MethodCompiler.ShadowFrameName</c> uses it) — both mouths
    /// wrap it in quotes and nothing else. Every character that can occur here comes from
    /// an ECMA-335 metadata string: a module path, an entry point, or a compiler-minted
    /// thunk name carrying <c>&lt;</c>, <c>&gt;</c> and <c>|</c>. None of those is a
    /// backslash or a quote today, which is exactly why the escaping is done here once
    /// rather than trusted to stay unnecessary.</para></summary>
    internal static string BoundedImportThrowMessage(BoundedImport row) =>
        ("the native import " + row.Module + "!" + row.EntryPoint + " (" + row.Method + ") is not "
        + "available in a dn2cpp-transpiled binary: this build provides no implementation of that "
        + "module, and answering the call with a zero would be indistinguishable from a real "
        + "result. Avoid the API, or provide the module through a dn2cpp runtime shim.")
        .Replace("\\", "\\\\").Replace("\"", "\\\"");

    /// <summary>Whether a method belongs to the dynamic-code-generation surface
    /// (subtree cut at reachability, call/ldftn/newobj sites lowered to a
    /// catchable runtime PlatformNotSupportedException). Keyed on the
    /// arity-stripped open-definition name so CallSite&lt;T&gt; /
    /// Expression&lt;TDelegate&gt; instantiations (whose FullName is mangled)
    /// match their one table entry; the namespace prefilter keeps the hot
    /// reachability path from resolving definition names for unrelated types.
    /// The name set is the descriptor row <see cref="CoreIntrinsics.BdDynamicCodegen"/>
    /// (which pairs it with the throwing emit arm); the prefilter and the
    /// definition-name key need the Compilation, so — as with
    /// <see cref="IsBoundedMethod"/> — every asker goes through this method rather than
    /// the row.</summary>
    internal bool IsDynamicCodegenMember(ClassInfo cls, string name) =>
        cls.Namespace is "System.Reflection.Emit" or "System.Runtime.CompilerServices"
            or "System.Linq.Expressions"
        && !cls.IsValueType && !cls.IsEnum
        && CoreIntrinsics.BdDynamicCodegen.Matches(GenericDefFullName(cls), name);

    /// <summary>Whether a method belongs to the absent socket / name-resolution platform
    /// layer (subtree cut at reachability, call/ldftn/ldvirtftn/newobj sites lowered to a
    /// catchable runtime PlatformNotSupportedException). The name set is the descriptor row
    /// <see cref="CoreIntrinsics.BdAbsentNetworkPal"/>, which pairs it with the throwing
    /// emit arm; the namespace prefilter and the arity-stripped definition-name key need
    /// the Compilation, so — as with <see cref="IsDynamicCodegenMember"/> — every asker
    /// goes through this method rather than the row.
    ///
    /// <para>Value types and enums are excluded for the reason they are there: the
    /// namespace's structs and enums (SocketError, AddressFamily, IPPacketInformation) are
    /// data flowing through reachable signatures and must keep transpiling. The
    /// <c>System.Net</c> arm of the prefilter exists for <c>System.Net.Dns</c> alone; the
    /// row's own test is what keeps IPAddress / IPEndPoint / WebUtility out of it.</para></summary>
    internal bool IsAbsentNetworkPalMember(ClassInfo cls, string name) =>
        cls.Namespace is "System.Net.Sockets" or "System.Net"
        && !cls.IsValueType && !cls.IsEnum
        && CoreIntrinsics.BdAbsentNetworkPal.Matches(GenericDefFullName(cls), name);

    /// <summary>The sentence every absent-socket-PAL substitute throws. One builder, four
    /// mouths (the call site, the two address-taken stubs and <c>newobj</c>), so a program
    /// that meets the refusal reads the same diagnosis wherever it met it. Escaped like
    /// <see cref="BoundedImportThrowMessage"/>, because the mouths wrap it in C++ quotes
    /// and nothing else.</summary>
    internal static string AbsentNetworkPalThrowMessage(string declType, string name) =>
        (declType + "." + name + " needs the socket / name-resolution platform layer, which a "
        + "dn2cpp-transpiled binary does not provide: there is no System.Net.Sockets or DNS "
        + "resolver implementation behind libSystem.Native, and answering the call with a zero "
        + "would hand back an unconnected socket or a null task. HTTP/HTTPS is served by the "
        + "dn2cpp transport instead — use HttpClient, or configure the API not to open its own "
        + "socket.")
        .Replace("\\", "\\\\").Replace("\"", "\\\"");

    /// <summary>Loads one assembly image as the next module (index = current module
    /// count) and registers it in <see cref="Modules"/> / the reader map.
    /// Reads the whole image via the already-intercepted File.ReadAllBytes and hands the
    /// bytes to the ImmutableArray&lt;byte&gt; PEReader ctor, rather than the
    /// FileStream-owning File.OpenRead overload. Functionally identical (PEReader
    /// holds the image either way; no stream to dispose), but it keeps the
    /// self-host transpile of dn2cpp off the heavy FileStream model — and, with the
    /// File.WriteAllText switch, off the shared ArrayPool/EventSource/Tracing cascade
    /// those File I/O roots pulled in.
    /// <para>The bytes are re-typed, not re-copied: <c>ToImmutableArray()</c> would
    /// duplicate the whole image, and PEReader would then hold the copy while the original
    /// became large-object garbage at exactly the moment the heap is otherwise empty.
    /// <c>AsImmutableArray</c> hands over the array the read just produced, which
    /// nothing else references.</para></summary>
    private Module LoadModule(string path)
    {
        var pe = new PEReader(ImmutableCollectionsMarshal.AsImmutableArray(File.ReadAllBytes(path)));
        var reader = pe.GetMetadataReader();
        var module = new Module
        {
            Index = Modules.Count,
            PE = pe,
            Reader = reader,
            AssemblyName = reader.GetString(reader.GetAssemblyDefinition().Name),
            Owner = this,
        };
        Modules.Add(module);
        _byReader[reader] = module;
        // A runaway --auto-ref closure (LoadReferenceClosure walks assembly references
        // to a fixpoint) is caught here, before a byte of it is decoded. The guard
        // samples, so force this one: the loop runs once per assembly, not once per
        // type, and would otherwise almost never land on a sample.
        MemoryGuard.CheckNow("load-modules");
        return module;
    }

    /// <summary>Transitive reference closure (opt-in via <c>--auto-ref</c>): before
    /// Build(), walk every loaded module's AssemblyReferences and eagerly load any
    /// not-yet-loaded definition assembly found in the shared-framework directory (the
    /// directory holding System.Private.CoreLib.dll among the passed paths), to a
    /// fixpoint. This extends the TypeIndex so external generic instantiations the
    /// reachable code touches (<c>ReadOnlySequence&lt;T&gt;</c> in System.Memory,
    /// <c>OrderedDictionary&lt;,&gt;</c> in System.Collections, …) resolve to real
    /// templates instead of aborting signature decoding.
    /// <para>Must run before Build() — Pass 2 eagerly decodes every signature, so an
    /// on-demand load mid-decode would miss earlier-decoded references; the closure fixes
    /// the full module set up front.</para>
    /// <para>Scoped to the framework directory so user/application DLLs are never pulled
    /// in (a referenced name with no matching framework file stays external exactly as
    /// before). Closure modules append after the explicit ones, so the app module
    /// (index 0) still wins TypeIndex ties. A no-op when no framework directory is found
    /// (e.g. a self-contained app with no CoreLib reference).</para></summary>
    private void LoadReferenceClosure(IReadOnlyList<string> assemblyPaths)
    {
        // The shared-framework directory = the directory of the passed CoreLib. Only
        // assemblies sitting next to it are candidates (framework-scoped, not the whole
        // filesystem), so a user DLL with a colliding simple name is never loaded.
        string? fwDir = null;
        foreach (var p in assemblyPaths)
        {
            string? dir = Path.GetDirectoryName(Path.GetFullPath(p));
            if (dir is not null && File.Exists(Path.Combine(dir, "System.Private.CoreLib.dll")))
            {
                fwDir = dir;
                break;
            }
        }
        if (fwDir is null)
            return;

        var loaded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var m in Modules)
            loaded.Add(m.AssemblyName);

        // Fixpoint: a freshly loaded framework assembly may reference more, so process a
        // worklist (seeded with everything already loaded) until it drains. The queue
        // owns its own storage, so appending to Modules inside LoadModule is safe.
        var queue = new Queue<Module>(Modules);
        while (queue.Count > 0)
        {
            var module = queue.Dequeue();
            foreach (var arh in module.Reader.AssemblyReferences)
            {
                string name = module.Reader.GetString(module.Reader.GetAssemblyReference(arh).Name);
                if (!loaded.Add(name))
                    continue; // already loaded, or a non-framework ref we already skipped
                string candidate = Path.Combine(fwDir, name + ".dll");
                if (!File.Exists(candidate))
                    continue; // not a shared-framework assembly -> stays external, as before
                var newModule = LoadModule(candidate);
                loaded.Add(newModule.AssemblyName); // the defined name may differ from the ref name
                queue.Enqueue(newModule);
            }
        }
    }

    public Module ModuleOf(MetadataReader reader) => _byReader[reader];

    // Name index over Classes, and the cursor of the classes already indexed. Classes is
    // append-only but keeps GROWING through discovery and emission (specializations are
    // appended), so the index catches up on demand instead of being built once.
    private readonly Dictionary<string, ClassInfo> _byFullName = new();
    private int _byFullNameCursor;

    /// <summary>The (first) loaded class with the given namespace-qualified name, or
    /// null. Used to resolve a custom attribute's serialized <c>typeof(T)</c> argument
    /// to a ClassInfo, a constrained static-virtual call's receiver, and more —
    /// several of them per compiled call site.
    /// <para>Index-backed (the cursor above), not a linear scan: Classes runs to
    /// 10^5..10^6 entries on a real game, so a per-call-site scan would be
    /// quadratic.</para></summary>
    public ClassInfo? FindClassByFullName(string fullName)
    {
        for (; _byFullNameCursor < Classes.Count; _byFullNameCursor++)
        {
            var c = Classes[_byFullNameCursor];
            // Smallest SortKey wins among same-named classes (cross-module twins,
            // specializations) — a tie-break on the input, never on discovery order:
            // what a name resolves to is a *meaning* question (the ClassInfo handed
            // back drives attribute typeof(T) arguments and constrained
            // static-virtual receivers), and tying it to the walk would make the
            // meaning depend on the order Classes happened to grow.
            if (!_byFullName.TryGetValue(c.FullName, out var cur)
                || ClassInfo.CompareByOrder(c, cur) < 0)
                _byFullName[c.FullName] = c;
        }
        return _byFullName.TryGetValue(fullName, out var hit) ? hit : null;
    }

    /// <summary>The lazily-built custom-attribute value decoder.</summary>
    public CustomAttributeTypeProvider AttrProvider => _attrProvider ??= new CustomAttributeTypeProvider(this);
    private CustomAttributeTypeProvider? _attrProvider;

    public ClassInfo GetClass(Module m, TypeDefinitionHandle handle) => m.ClassMap[handle];
    public MethodInfo GetMethod(Module m, MethodDefinitionHandle handle) => m.MethodMap[handle];

    /// <summary>Ensures a generic specialization's fields/methods/vtable are
    /// populated before a caller inspects them (e.g. reading a List&lt;T&gt;'s
    /// backing-array fields from an intrinsic). Idempotent and a no-op for
    /// non-generic classes.
    ///
    /// <para>This is now the <em>only</em> door into the member tier: the drain populates
    /// shape and stops (<see cref="CompleteShape"/>), so a specialization's methods exist
    /// only because someone asked for them — here, or through the accessors on
    /// <see cref="ClassInfo"/>, which call the same thing. Forgetting to pull therefore
    /// costs memory, not correctness.</para></summary>
    public void EnsureCompleted(ClassInfo cls)
    {
        if (cls.GenericArity == 0)
            return;
        if (!cls.ShapeCompleted)
            CompleteShape(cls);
        if (!cls.MembersCompleted)
            CompleteMembers(cls);
    }

    public TypeDesc GetTypeDescForDefinition(Module m, TypeDefinitionHandle handle)
    {
        if (m.ClassMap.TryGetValue(handle, out var cls))
        {
            // A primitive value type is a TypeDef when corlib itself is the module
            // being transpiled; an IL type token for it (`newarr int`, `ldelema
            // int` inside Dictionary) must behave like the primitive the type
            // encodes, not a corlib `System.Int32` class.
            if (WellKnownPrimitive(cls.FullName) is { } prim)
                return prim;
            return TypeDesc.MakeClass(cls);
        }
        if (m.GenericTemplates.Contains(handle))
            return TypeDesc.MakeTemplate(m, handle);
        throw new NotSupportedException("Reference to an unbuilt type definition");
    }

    /// <summary>The primitive a well-known corlib type name encodes, or null. Used
    /// to normalize references to corlib's own primitive TypeDefs/TypeRefs.</summary>
    public static TypeDesc? WellKnownPrimitive(string fullName) => fullName switch
    {
        "System.Boolean" => TypeDesc.MakePrimitive(PrimitiveTypeCode.Boolean),
        "System.Char" => TypeDesc.MakePrimitive(PrimitiveTypeCode.Char),
        "System.SByte" => TypeDesc.MakePrimitive(PrimitiveTypeCode.SByte),
        "System.Byte" => TypeDesc.MakePrimitive(PrimitiveTypeCode.Byte),
        "System.Int16" => TypeDesc.MakePrimitive(PrimitiveTypeCode.Int16),
        "System.UInt16" => TypeDesc.MakePrimitive(PrimitiveTypeCode.UInt16),
        "System.Int32" => TypeDesc.MakePrimitive(PrimitiveTypeCode.Int32),
        "System.UInt32" => TypeDesc.MakePrimitive(PrimitiveTypeCode.UInt32),
        "System.Int64" => TypeDesc.MakePrimitive(PrimitiveTypeCode.Int64),
        "System.UInt64" => TypeDesc.MakePrimitive(PrimitiveTypeCode.UInt64),
        "System.IntPtr" => TypeDesc.MakePrimitive(PrimitiveTypeCode.IntPtr),
        "System.UIntPtr" => TypeDesc.MakePrimitive(PrimitiveTypeCode.UIntPtr),
        "System.Single" => TypeDesc.MakePrimitive(PrimitiveTypeCode.Single),
        "System.Double" => TypeDesc.MakePrimitive(PrimitiveTypeCode.Double),
        // String/Object have runtime metadata (dn2cpp_string_type/object_type) and
        // dedicated C++ reps; normalizing corlib's own TypeDefs to the primitive
        // keeps typeof(string)/isinst string out of the per-class emit path.
        "System.String" => TypeDesc.MakePrimitive(PrimitiveTypeCode.String),
        "System.Object" => TypeDesc.MakePrimitive(PrimitiveTypeCode.Object),
        _ => null,
    };

    /// <summary>Reference types named by an IL type token (castclass/isinst/
    /// ldtoken/…) or a bounded call's receiver, but never allocated or otherwise
    /// reached. They occur in branches the BCL leaves statically reachable but
    /// that fold away at runtime (e.g. Dictionary's string-comparer path for a
    /// value-type key). They are emitted opaquely so dead-branch C++ still
    /// compiles, never as live layouts.</summary>
    public HashSet<ClassInfo> ReferencedTypes { get; } = new();

    public void NoteReferencedType(ClassInfo cls) => ReferencedTypes.Add(cls);

    /// <summary>Notes every concrete handle that a constructed type's runtime identity
    /// points through, including generic arguments and array elements.</summary>
    internal void NoteTypeIdentityClosure(TypeDesc type)
    {
        switch (type.Kind)
        {
            case TypeKind.Class:
                NoteReferencedType(type.Class!);
                foreach (var arg in type.Class!.Context.TypeArgs)
                    NoteTypeIdentityClosure(arg);
                break;
            case TypeKind.SZArray:
                NoteArrayElementType(type.Element!);
                NoteTypeIdentityClosure(type.Element!);
                break;
            case TypeKind.MDArray:
                NoteMdArrayType(type);
                NoteTypeIdentityClosure(type.Element!);
                break;
        }
    }

    /// <summary>Open generic DEFINITIONS a body took the runtime <c>typeof</c> of —
    /// <c>typeof(IDictionary&lt;,&gt;)</c>, <c>typeof(List&lt;&gt;)</c>. Such a token decodes
    /// to an <c>External</c> TypeDesc carrying the CLR backtick name (dn2cpp mints no
    /// ClassInfo for a bare open definition), and the <c>ldtoken</c> lowering routes it to
    /// the shared <c>gendef_&lt;sym&gt;</c> open-definition type-info handle — the same one a
    /// closed instantiation's <c>genericDef</c> points at, so <c>GetGenericTypeDefinition()
    /// == typeof(Def&lt;&gt;)</c> holds and <c>IsGenericTypeDefinition</c>/<c>FullName</c>
    /// answer. The keys are the backtick full names; the emitter synthesizes a gendef for any
    /// that no emitted closed instantiation already minted, so a body naming
    /// <c>&amp;gendef_&lt;sym&gt;</c> never links against an undefined symbol (cut ⟹ route).
    /// The value carries the definition's KIND bits so the synthetic handle answers
    /// <c>IsInterface</c>/<c>IsAbstract</c>/<c>IsValueType</c>/<c>IsSealed</c> like real .NET —
    /// resolved at the token site (a natural gendef reads them off its closed instantiation
    /// instead, and wins the emit dedup, so this value is consulted only for a def no close
    /// exists of). It carries the definition's type-parameter NAMES on the same terms — the
    /// bracket group <c>Type.ToString()</c> appends. Both are facts about one TypeDef, so both
    /// degrade together and only where none is found: a cross-assembly (<c>External</c>) def
    /// resolves through the type index by name.</summary>
    public Dictionary<string, (GenericDefKind Kind, string? ParamNames)> TypeofOpenGenericDefs { get; } =
        new(System.StringComparer.Ordinal);

    /// <summary>Records that a body took <c>typeof</c> of the open generic definition whose
    /// CLR backtick full name is <paramref name="defName"/>, so the emitter guarantees a
    /// <c>gendef_&lt;sym&gt;</c> handle exists for it. <paramref name="kind"/> and
    /// <paramref name="paramNames"/> carry the definition's kind bits and type-parameter
    /// names for the synthetic handle. Idempotent; a later call carrying resolved kind bits
    /// upgrades an earlier <see cref="GenericDefKind.Unknown"/> record (the same def can be
    /// typeof'd from a site that could resolve its attributes and one that could not).</summary>
    public void NoteTypeofOpenGenericDef(string defName, GenericDefKind kind, string? paramNames)
    {
        if (!TypeofOpenGenericDefs.TryGetValue(defName, out var existing) || existing.Kind == GenericDefKind.Unknown)
            TypeofOpenGenericDefs[defName] = (kind, paramNames);
    }

    /// <summary>Reflection-metadata trimming (<c>--trim-reflection</c>): strip the field,
    /// method and property tables — and everything only they name (accessor thunks,
    /// invoker thunks, parameter/generic-argument pools, attribute factories) — of every
    /// class the program cannot plausibly reflect over. A CLI flag rather than an
    /// environment variable on purpose: it changes the C++ a *successful* transpile emits,
    /// and the self-host fixpoint requires that no environment variable can do that.</summary>
    private readonly bool _trimReflection;

    /// <summary><c>--reflection-root</c>: the escape hatch. See
    /// <see cref="ReflectionRootMatching"/> for what a root matches.</summary>
    private readonly HashSet<string> _reflectionRoots;

    /// <summary>The classes that keep their reflection member metadata, or null when
    /// trimming is off — in which case <see cref="KeepsReflectionMetadata"/> answers true
    /// for everything and the emitted C++ is byte-identical to a build without the
    /// flag.</summary>
    private HashSet<ClassInfo>? _reflectionKeep;

    /// <summary>Whether <paramref name="cls"/> keeps its reflection member metadata.
    /// True for every class unless <c>--trim-reflection</c> asked otherwise; a stripped
    /// class still carries its name, base, interfaces, layout, vtable, assembly, Type
    /// object, enum members and the ToString/Equals/GetHashCode/Finalize slots — only the
    /// <c>GetFields/GetMethods/GetProperties</c> surface goes, and reading it at run time
    /// throws (see <c>DN2CPP_TF_METADATA_STRIPPED</c>) rather than answering empty.</summary>
    public bool KeepsReflectionMetadata(ClassInfo cls) =>
        _reflectionKeep is null || _reflectionKeep.Contains(cls);

    /// <summary>Materializes the reflection keep-set. Called by the emitter immediately
    /// before <c>EmitTypeInfos</c>, and it must not run earlier: <c>ComputeEmitted</c>'s
    /// delegate-invoker pass still appends to <see cref="ReferencedTypes"/>, which is a
    /// seed. Idempotent, and a no-op when trimming is off.</summary>
    public void ComputeReflectionKeepSet()
    {
        if (!_trimReflection || _reflectionKeep is not null)
            return;
        var keep = new HashSet<ClassInfo>();
        var rootsHit = new HashSet<string>(StringComparer.Ordinal);
        // Nothing below decodes — Module/FullName/BaseClass/Interfaces are *shape*, which a
        // completed specialization populates eagerly — so Classes cannot grow underneath this
        // walk. Snapshot anyway: a seed that one day reads a signature or a field type would
        // otherwise turn a keep-set into a "collection was modified".
        foreach (var cls in Classes.ToList())
        {
            // The game's own assembly. Reflection over user types is what has to keep
            // working — [Export]/[Signal] scanning, serialization, attributes — and its
            // tables are a rounding error against the framework's.
            if (cls.Module == AppModule)
                keep.Add(cls);
            // Every delegate type, whatever module it comes from. NOT a reflection
            // concession: dn2cpp_delegate_create finds a delegate's Invoke row by scanning
            // that delegate's OWN method table (dn2cpp_delegate_invoke_row), so a stripped
            // delegate cannot be bound at all — Callable.From, GodotSharp's DelegateUtils
            // and every event subscription that goes through Delegate.CreateDelegate would
            // throw. A delegate declares no fields and exactly the four methods the CLR's
            // delegate shape mandates (.ctor, Invoke, BeginInvoke, EndInvoke), so the whole
            // family is negligible against the tables this flag exists to cut.
            else if (cls.IsDelegate)
                keep.Add(cls);
            // Named by a reachable type token: ldtoken/typeof, isinst/castclass, a bounded
            // call's receiver, a delegate invoker's signature. Deliberately broader than
            // "typeof() appears in the IL" — being *nameable* is the precondition for
            // typeof(X).GetMethods(), and over-keeping costs relocations while under-keeping
            // costs a run-time throw. The other route to a Type — x.GetType() — is not
            // statically decidable at all, which is precisely why a stripped read must fail
            // loudly instead of answering empty.
            else if (ReferencedTypes.Contains(cls))
                keep.Add(cls);
            // The escape hatch. rootsHit records the ROOT that matched, not the class, because
            // that is what the typo check below is asking about.
            if (_reflectionRoots.Count > 0 && ReflectionRootMatching(cls) is { } hit)
            {
                keep.Add(cls);
                rootsHit.Add(hit);
            }
            if (_explicitReflectionKeep.Contains(cls))
                keep.Add(cls);
        }
        // A root matching no loaded type is a hard error: a typo silently becoming a no-op
        // root would surface as a PlatformNotSupportedException in a shipped game, which is
        // the one place this diagnostic cannot reach anybody.
        List<string>? missing = null;
        foreach (string r in _reflectionRoots)
            if (!rootsHit.Contains(r))
                (missing ??= new List<string>()).Add(r);
        if (missing is not null)
        {
            missing.Sort(StringComparer.Ordinal);
            throw new NotSupportedException(
                $"--reflection-root: no loaded type is named {string.Join(", ", missing)}");
        }
        // Base-chain closure. MANDATORY, and load-bearing twice:
        //   * dn2cpp_collect_fields / dn2cpp_collect_methods walk `for (ti = type; ti;
        //     ti = ti->base)`, so a kept class over a stripped base would SILENTLY lose its
        //     inherited members (the stripped-bit test at every level turns that into a
        //     throw, but a throw on a type the program legitimately reflects over is still
        //     a broken program).
        //   * Godot.GodotObject.InternalGetClassNativeBaseName walks up to the first engine
        //     wrapper base and reads its private static NativeName field BY REFLECTION —
        //     strip that base's field table and Godot script registration gets a null native
        //     class name. The closure keeps Godot.Node/Node2D/... alive for free.
        // Interfaces ride along: an interface's own member table is the only place its
        // members live (an interface has no base chain), so a kept implementor whose
        // interface was stripped would answer a GetInterfaces()-then-GetMethods() walk with a
        // throw. They are cheap — an interface declares no fields.
        var work = new Stack<ClassInfo>(keep);
        while (work.Count > 0)
        {
            var c = work.Pop();
            if (c.BaseClass is { } b && keep.Add(b))
                work.Push(b);
            foreach (var itf in c.Interfaces)
                if (keep.Add(itf))
                    work.Push(itf);
        }
        _reflectionKeep = keep;
    }

    /// <summary>The <c>--reflection-root</c> that names <paramref name="cls"/>, or null.
    /// Matched on the exact <see cref="ClassInfo.FullName"/>, or on the arity-stripped
    /// generic-definition name (<see cref="GenericDefFullName"/> —
    /// <c>System.Collections.Generic.List</c>), so that one root covers every instantiation:
    /// a closed instantiation's own FullName has its type arguments mangled into it
    /// (<c>List_System_Int32</c>), which is not a name anyone can be expected to guess, and
    /// rooting instantiations one at a time is not what a program reflecting over
    /// <c>List&lt;T&gt;</c> wants anyway.</summary>
    private string? ReflectionRootMatching(ClassInfo cls)
    {
        if (_reflectionRoots.Contains(cls.FullName))
            return cls.FullName;
        try
        {
            // Raw metadata only (no signature or field-type decode), so this is safe inside
            // the Classes walk. A synthesized class has no type definition to read; it stays
            // rootable by its exact name.
            string def = GenericDefFullName(cls);
            return _reflectionRoots.Contains(def) ? def : null;
        }
        catch (Exception e) when (!IsMustEscape(e))
        {
            return null;
        }
    }

    /// <summary>Assembly simple names whose embedded manifest resources are dropped
    /// from the image (<c>--no-manifest-resources</c>). A flag rather than an
    /// environment variable for the <see cref="_trimReflection"/> reason: it changes
    /// the C++ a *successful* transpile emits.</summary>
    private readonly HashSet<string> _noManifestResources;

    /// <summary>Manifest resource names kept under <c>--no-manifest-resources</c>
    /// (<c>--manifest-resource-root</c>): the escape hatch, matched by exact manifest
    /// name against the dropped assemblies' embedded resources.</summary>
    private readonly HashSet<string> _manifestResourceRoots;

    /// <summary>Whether <paramref name="module"/>'s embedded manifest resources are
    /// dropped (<c>--no-manifest-resources</c> named its assembly). The emitter then
    /// keeps only the <see cref="IsManifestResourceRoot"/> rows of its table and sets
    /// the registry row's <c>resourcesDropped</c> bit, so a run-time miss throws
    /// instead of answering the null that means "no such resource".</summary>
    internal bool DropsManifestResources(Module module) =>
        _noManifestResources.Contains(module.AssemblyName);

    /// <summary>Whether a manifest resource name is kept by
    /// <c>--manifest-resource-root</c> — see <see cref="DropsManifestResources"/>.</summary>
    internal bool IsManifestResourceRoot(string name) => _manifestResourceRoots.Contains(name);

    /// <summary>Hard-errors the two manifest-resource flags' typo shapes, at load
    /// time (not from the emitter, whose resource tables are emitted only when a body
    /// reads one — a typo must be loud whether or not this program pays the blobs):
    /// every <c>--no-manifest-resources</c> must name a loaded assembly (the
    /// <c>--no-default-ref</c> contract), and every <c>--manifest-resource-root</c>
    /// must match an embedded resource of some DROPPED assembly — a root's only
    /// effect is inside one, so a root without a matching drop is a no-op and a
    /// silent no-op root is the <c>--reflection-root</c> footgun: it would surface as
    /// a PlatformNotSupportedException only in the shipped game.</summary>
    private void ValidateManifestResourceFlags()
    {
        if (_noManifestResources.Count == 0 && _manifestResourceRoots.Count == 0)
            return;
        var loaded = new HashSet<string>(Modules.Select(m => m.AssemblyName), StringComparer.Ordinal);
        foreach (string name in _noManifestResources.OrderBy(n => n, StringComparer.Ordinal))
            if (!loaded.Contains(name))
                throw new NotSupportedException(
                    $"--no-manifest-resources {name}: no loaded assembly is named {name} "
                    + $"(loaded: {string.Join(", ", Modules.Select(m => m.AssemblyName))})");
        if (_manifestResourceRoots.Count == 0)
            return;
        var rootsHit = new HashSet<string>(StringComparer.Ordinal);
        foreach (var module in Modules)
        {
            if (!DropsManifestResources(module))
                continue;
            // Names only — Locate reads the ManifestResource rows, no blob is pulled.
            foreach (var res in ResourceStrings.Locate(module.PE, module.Reader))
                if (_manifestResourceRoots.Contains(res.Name))
                    rootsHit.Add(res.Name);
        }
        List<string>? missing = null;
        foreach (string r in _manifestResourceRoots)
            if (!rootsHit.Contains(r))
                (missing ??= new List<string>()).Add(r);
        if (missing is not null)
        {
            missing.Sort(StringComparer.Ordinal);
            throw new NotSupportedException(
                "--manifest-resource-root: no assembly named by --no-manifest-resources "
                + $"carries an embedded resource named {string.Join(", ", missing)}");
        }
    }

    /// <summary>Classes whose **full** C++ layout (and, for static-field owners,
    /// their static-field declarations) a reachable method body materially uses,
    /// yet which enter the signature-driven emit set through no other edge — e.g.
    /// the compiler-generated <c>&lt;&gt;O</c> method-group cache (only static
    /// <c>Func&lt;…&gt;</c> fields, no .cctor and never instantiated, so
    /// ReachCctor is a no-op), or a by-value struct accessed by field inside a
    /// transpiled BCL body (System.Threading.Volatile's nested VolatileObject/
    /// VolatileUIntPtr in Volatile.Read&lt;T&gt;). Unlike <see cref="ReferencedTypes"/>
    /// (opaque empty layout — enough for a dead-branch cast), these need their
    /// real fields, so <see cref="CppEmitter.ComputeEmitted"/> seeds from this set
    /// as a *full*, non-opaque emit root (native build).</summary>
    public HashSet<ClassInfo> ForceEmittedClasses { get; } = new();

    public void NoteForceEmit(ClassInfo? cls)
    {
        if (cls is not null)
            ForceEmittedClasses.Add(cls);
    }

    /// <summary>Static constructors a method body injected a lazy first-use guard for
    /// (a static-field access whose declaring type runs a .cctor on first use). dn2cpp
    /// otherwise runs every .cctor eagerly at startup in reach order, which violates
    /// .NET's guarantee that a type's statics are set before anything reads them — e.g.
    /// a cctor that reads a static another (later-ordered) cctor sets observes its
    /// default value. The emitter generates an idempotent <c>__ensure</c> wrapper per
    /// target so the read site can run the target's cctor first.</summary>
    public HashSet<MethodInfo> CctorEnsureTargets { get; } = new();

    public void NoteCctorEnsure(MethodInfo cctor) => CctorEnsureTargets.Add(cctor);

    /// <summary>P/Invoke methods whose call site actually lowered to a native call.
    /// A P/Invoke method has no IL body, so it never enters the normal
    /// <see cref="Reachable"/> set (Reach cuts Rva==0); the emitter walks this set
    /// instead to declare each distinct entry-point symbol <c>extern "C"</c>.</summary>
    public HashSet<MethodInfo> PInvokeCalls { get; } = new();

    /// <summary>Managed implementations of native P/Invoke entry points, keyed by the
    /// <c>[DllImport]</c> (module, entry point) they replace: static methods carrying
    /// <c>[Dn2Cpp.Runtime.NativeImplementation(module, entryPoint)]</c> in any loaded
    /// assembly (matched by attribute full name, so a decoupled assembly can declare
    /// its own internal copy). A call to a bodyless P/Invoke with a registered
    /// implementation lowers to a direct call to it (<c>MethodCompiler
    /// .EmitNativeImplCall</c>) instead of an <c>extern "C"</c> native call, so the
    /// native symbol is never referenced or linked. Populated during pass 2, before
    /// any reachability scan.</summary>
    public Dictionary<(string Module, string Entry), MethodInfo> NativeImpls { get; } = new();

    /// <summary>Canonicalizes a P/Invoke module name for <see cref="NativeImpls"/>'s
    /// dictionary key: strips a leading <c>lib</c> (case-sensitively, the same
    /// prefix rule <see cref="LinkLibToken"/> applies) and lowercases. A [NativeImplementation]
    /// backend (DnZlib/DnBrotli) is written once against the POSIX spelling
    /// (<c>libSystem.IO.Compression.Native</c>) that <c>System.IO.Compression.dll</c> /
    /// <c>System.IO.Compression.Brotli.dll</c> — not CoreLib — declare on macOS/Linux;
    /// on win-x64 those same assemblies spell the identical native shim
    /// <c>System.IO.Compression.Native</c> (no <c>lib</c> prefix — see
    /// <see cref="IsRuntimeProvidedPInvokeModule"/>'s Windows arm). Without this, the
    /// exact-string dictionary lookup below would silently miss on Windows and every
    /// [NativeImplementation] swap would fall through to the native P/Invoke path.</summary>
    private static string CanonicalNativeImplModule(string module) =>
        (module.StartsWith("lib", StringComparison.Ordinal) ? module[3..] : module)
            .ToLowerInvariant();

    /// <summary>The registered managed implementation replacing a P/Invoke's native
    /// entry point, or null when the P/Invoke stays on its native path. The lookup
    /// sees <see cref="PInvokeInfo.EntryPoint"/> after any platform rewrite (the
    /// Darwin setattrlist reroute), so an implementation of a rewritten libc import
    /// would have to name the rewritten symbol — irrelevant for the intended
    /// BCL-backend swaps.</summary>
    public MethodInfo? NativeImplFor(PInvokeInfo pinv) =>
        NativeImpls.Count != 0
        && NativeImpls.TryGetValue((CanonicalNativeImplModule(pinv.ModuleName), pinv.EntryPoint), out var impl)
            ? impl : null;

    /// <summary>Validates and registers a <c>[NativeImplementation]</c> method. The
    /// shape constraints keep the call-site lowering a plain direct static call: an
    /// instance method would need a receiver the P/Invoke call site doesn't have, a
    /// bodyless method (itself a P/Invoke / extern) has nothing to call, and a generic
    /// method or declaring type has no single symbol to bind. Signature compatibility
    /// against the replaced import is checked per call site (where both signatures are
    /// in hand — <c>MethodCompiler.EmitNativeImplCall</c>).</summary>
    private void RegisterNativeImpl((string Module, string Entry) key, MethodInfo impl)
    {
        string Where() => $"[NativeImplementation(\"{key.Module}\", \"{key.Entry}\")] "
                          + $"{impl.DeclaringClass.FullName}.{impl.Name}";
        if (!impl.IsStatic)
            throw new NotSupportedException($"{Where()}: a native-implementation method must be static");
        if (impl.Rva == 0)
            throw new NotSupportedException(
                $"{Where()}: a native-implementation method must have an IL body (it cannot itself be extern/P-Invoke)");
        if (impl.DeclaringClass.GenericArity > 0)
            throw new NotSupportedException(
                $"{Where()}: a native-implementation method cannot be declared on a generic type");
        var canonKey = (CanonicalNativeImplModule(key.Module), key.Entry);
        if (NativeImpls.TryGetValue(canonKey, out var prior))
            throw new NotSupportedException(
                $"{Where()}: duplicate native implementation — already provided by "
                + $"{prior.DeclaringClass.FullName}.{prior.Name}");
        NativeImpls.Add(canonKey, impl);
    }

    /// <summary>Delegate types converted to/from a raw native function pointer — via the
    /// explicit <c>Marshal.GetFunctionPointerForDelegate&lt;T&gt;</c> /
    /// <c>Marshal.GetDelegateForFunctionPointer&lt;T&gt;</c> intrinsics
    /// (<c>MethodCompiler.TranslateGenericIntrinsic</c>) OR the implicit marshal of a
    /// delegate-typed P/Invoke parameter (<c>MethodCompiler.Newobj.cs</c>). The returned
    /// pointer has no bounded lifetime (the native may store it and call back later, which
    /// the transpiler cannot statically rule out), so each type gets a small pool of
    /// statically-rooted delegate slots + one C-ABI thunk per slot
    /// (<see cref="CppEmitter.EmitMarshalFnPtrThunks"/>) — the single delegate-to-fnptr
    /// path for both callers.</summary>
    public HashSet<ClassInfo> MarshalFnPtrDelegates { get; } = new();

    /// <summary>Whether any CreateDelegate intrinsic (MethodInfo.CreateDelegate /
    /// Delegate.CreateDelegate) was lowered. When set, the emitter generates the
    /// reflection delegate-bind machinery — one signature-deduplicated
    /// <c>dgrefl_*</c> trampoline per distinct delegate-Invoke ABI shape and the
    /// {delegate type-info → trampoline} registry the runtime binder scans
    /// (<see cref="CppEmitter.EmitDelegateReflBinds"/>). The registry symbols
    /// themselves emit unconditionally (a null row) so the runtime always links.</summary>
    public bool NeedsReflectionDelegateBind { get; set; }

    /// <summary>Non-blittable but marshalable value structs (string/bool/blittable/
    /// nested-blittable fields) passed to a P/Invoke by value or by ref. Each needs a
    /// generated native-layout <c>tn_&lt;Name&gt;</c> plus field-by-field marshal-in/out
    /// helpers (<see cref="CppEmitter.EmitPInvokeMarshalStructs"/>); the call site
    /// (<c>MethodCompiler.EmitPInvokeCall</c>) registers the type here when it lowers such
    /// an argument. The owning managed struct is also force-emitted (its <c>t_&lt;Name&gt;</c>
    /// layout backs the marshallers).</summary>
    public HashSet<ClassInfo> PInvokeMarshalStructs { get; } = new();

    /// <summary>Resolves a cross-assembly TypeRef to a TypeDef/Template in a
    /// loaded module, or null when the target assembly is not loaded (in which
    /// case the type is treated as an intrinsic/external reference).</summary>
    // (namespace, name) -> the TypeDefs that declare it, in module order so the
    // app module (index 0) wins ties. Built once; a real CoreLib has thousands
    // of types, so a per-TypeRef linear scan would be O(types^2).
    private Dictionary<(string, string), List<(Module Module, TypeDefinitionHandle Handle)>>? _typeIndex;

    private Dictionary<(string, string), List<(Module, TypeDefinitionHandle)>> TypeIndex()
    {
        if (_typeIndex is not null)
            return _typeIndex;
        _typeIndex = new();
        foreach (var target in Modules)
        {
            foreach (var tdh in target.Reader.TypeDefinitions)
            {
                var td = target.Reader.GetTypeDefinition(tdh);
                var key = (target.Reader.GetString(td.Namespace), target.Reader.GetString(td.Name));
                if (!_typeIndex.TryGetValue(key, out var list))
                    _typeIndex[key] = list = new();
                list.Add((target, tdh));
            }
        }
        return _typeIndex;
    }

    /// <summary>Everything a SYNTHETIC <c>gendef_</c> handle needs about the generic
    /// definition whose CLR backtick full name is <paramref name="defName"/> — its kind bits
    /// and its declared type-parameter names — read off the definition's own metadata,
    /// located through the type index (app module wins ties, as <see cref="ResolveTypeRef"/>
    /// does). Both facts ride the ONE lookup because both are facts about that one TypeDef;
    /// the degrade when no loaded module declares the name is
    /// <see cref="GenericDefKind.Unknown"/> with no bracket group.
    /// <para>The by-NAME counterpart of <see cref="ResolveOpenGenericDefKind"/> /
    /// <see cref="GenericParamNames(Module, TypeDefinitionHandle)"/>, and the arm that makes
    /// a typeof of an INTRINSIC definition answer: dn2cpp mints neither ClassInfo nor
    /// Template for one, so <c>typeof(Vector128&lt;&gt;)</c> decodes to an External name.
    /// A nested name (carrying '+') resolves to nothing and degrades, which is where the
    /// gendef mouths carve nested definitions out anyway.</para></summary>
    /// <summary>The CLR backtick full name of <paramref name="t"/> when it is an
    /// OPEN generic definition in one of the three shapes a <c>typeof(D&lt;&gt;)</c>
    /// ldtoken decodes to (the <c>OpenGenericDefTypeInfoExpr</c> shapes), else
    /// null. Nested definitions answer null, matching the gendef carve-out.</summary>
    internal static string? OpenGenericDefBacktickNameOf(TypeDesc t) => t switch
    {
        { Kind: TypeKind.External, ExternalName: { } en }
            when en.Contains('`') && !en.Contains('+') => en,
        { Kind: TypeKind.Template, TemplateModule: { } tm } =>
            MethodCompiler.OpenDefBacktickName(tm, t.TemplateHandle),
        { Kind: TypeKind.Class, Class: { GenericArity: > 0 } c } when c.Context.TypeArgs.Length == 0
            => MethodCompiler.OpenDefBacktickName(c.Module, c.Handle),
        _ => null,
    };

    public (string? Name, GenericDefKind Kind, string? ParamNames) OpenGenericDefFactsByName(string defName)
    {
        if (OpenGenericDefHandleByName(defName) is not { } d)
            return (defName, GenericDefKind.Unknown, null);
        return (defName, ResolveOpenGenericDefKind(d.Module, d.Handle), GenericParamNames(d.Module, d.Handle));
    }

    /// <summary>The (module, TypeDef) a generic definition's backtick full name points at,
    /// or null when no loaded module declares it. <see cref="TypeIndex"/> is keyed on the
    /// RAW name, arity backtick included.</summary>
    internal (Module Module, TypeDefinitionHandle Handle)? OpenGenericDefHandleByName(string defName)
    {
        int cut = defName.LastIndexOf('.');
        var key = cut < 0 ? ("", defName) : (defName[..cut], defName[(cut + 1)..]);
        return TypeIndex().TryGetValue(key, out var cands) && cands.Count > 0 ? cands[0] : null;
    }

    public TypeDesc? ResolveTypeRef(Module from, TypeReferenceHandle handle)
    {
        var tr = from.Reader.GetTypeReference(handle);
        string ns = from.Reader.GetString(tr.Namespace);
        string name = from.Reader.GetString(tr.Name);
        if (!TypeIndex().TryGetValue((ns, name), out var candidates))
            return null;

        // A nested type reference carries an empty namespace and its declaring
        // type as the ResolutionScope, so the (ns, name) key alone is ambiguous —
        // List`1.Enumerator and ArraySegment`1.Enumerator both index as
        // ("", "Enumerator"). Disambiguate by resolving the declaring type and
        // keeping only the candidate nested inside it.
        if (tr.ResolutionScope.Kind == HandleKind.TypeReference)
        {
            var (declMod, declHandle) = TemplateOrClassDef(ResolveTypeRef(from, (TypeReferenceHandle)tr.ResolutionScope));
            if (declMod is null)
                return null;
            foreach (var (target, tdh) in candidates)
                if (target == declMod
                    && target.Reader.GetTypeDefinition(tdh).GetDeclaringType() == declHandle)
                    return MakeTypeDescFor(target, tdh);
            return null;
        }

        foreach (var (target, tdh) in candidates)
            if (MakeTypeDescFor(target, tdh) is { } td)
                return td;
        return null;
    }

    /// <summary>A loaded type def's TypeDesc — concrete (ClassMap) or open
    /// template — or null if the handle is neither.</summary>
    private static TypeDesc? MakeTypeDescFor(Module target, TypeDefinitionHandle tdh)
    {
        if (target.ClassMap.TryGetValue(tdh, out var cls))
            return TypeDesc.MakeClass(cls);
        if (target.GenericTemplates.Contains(tdh))
            return TypeDesc.MakeTemplate(target, tdh);
        return null;
    }

    /// <summary>The (module, def-handle) a resolved type points at, for both the
    /// concrete-class and open-template forms.</summary>
    private static (Module?, TypeDefinitionHandle) TemplateOrClassDef(TypeDesc? t) => t switch
    {
        { Kind: TypeKind.Class, Class: { } c } => (c.Module, c.Handle),
        { Kind: TypeKind.Template, TemplateModule: { } m } => (m, t.TemplateHandle),
        _ => (null, default),
    };

    /// <summary>The enclosing-type qualifier for a nested type's C++ names — the
    /// declaring type chain (outermost first) plus its namespace, dot-separated and
    /// dot-terminated — or "" for a top-level type. Disambiguates same-named nested
    /// types across declaring types.</summary>
    private static string NestedCppPrefix(MetadataReader reader, TypeDefinition td)
    {
        var parts = new List<string>();
        for (var declHandle = td.GetDeclaringType(); !declHandle.IsNil;)
        {
            var declTd = reader.GetTypeDefinition(declHandle);
            string ns = reader.GetString(declTd.Namespace);
            string name = reader.GetString(declTd.Name);
            parts.Insert(0, string.IsNullOrEmpty(ns) ? name : ns + "." + name);
            declHandle = declTd.GetDeclaringType();
        }
        return parts.Count == 0 ? "" : string.Join(".", parts) + ".";
    }

    /// <summary>The CLR reflection name of a class — what real .NET reports as
    /// <c>Type.FullName</c>: for a nested type the declaring chain joined with
    /// '+' (<c>Ns.Outer+Mid+Leaf</c>), for everything else
    /// <see cref="ClassInfo.FullName"/> unchanged. This is what the emitter
    /// bakes into <c>Dn2CppTypeInfo.name</c> and keys the type registry on, so
    /// the two can never disagree — both are this one pure function of the
    /// metadata (a declaring-chain walk, like <see cref="NestedCppPrefix"/>;
    /// no member or signature decode, so it is strict-completion safe).
    /// <para>Two carve-outs return <see cref="ClassInfo.FullName"/> as-is: a
    /// synthetic class with no metadata row (<c>Handle.IsNil</c>), and a closed
    /// generic specialization — <see cref="Instantiate"/> already folds the
    /// declaring chain into its mangled <c>Name</c>
    /// (<c>Dictionary_Enumerator_Int32_String</c>), the documented
    /// mangled-name carve-out for generic reflection names.</para></summary>
    internal static string ReflectionTypeName(ClassInfo cls)
    {
        if (cls.Handle.IsNil || cls.Context.TypeArgs.Length > 0)
            return cls.FullName;
        try
        {
            var reader = cls.Module.Reader;
            var td = reader.GetTypeDefinition(cls.Handle);
            var declHandle = td.GetDeclaringType();
            if (declHandle.IsNil)
                return cls.FullName;
            var parts = new List<string> { cls.Name };
            while (!declHandle.IsNil)
            {
                var declTd = reader.GetTypeDefinition(declHandle);
                string ns = reader.GetString(declTd.Namespace);
                string name = reader.GetString(declTd.Name);
                parts.Insert(0, string.IsNullOrEmpty(ns) ? name : ns + "." + name);
                declHandle = declTd.GetDeclaringType();
            }
            return string.Join("+", parts);
        }
        catch (Exception e) when (!IsMustEscape(e))
        {
            // Same posture as CppEmitter.IsNestedType: unreadable metadata
            // degrades to the bare name rather than failing the transpile.
            return cls.FullName;
        }
    }

    private void Build()
    {
        // The intercept descriptor tables are static data every asker below consults, so
        // check their shape once, before anything reads a row. Armed in Debug (and behind
        // DN2CPP_INTERCEPT_SELFCHECK=1 in Release), a no-op otherwise; it can only abort a
        // run, never change the output of one that succeeds. What it can and cannot decide
        // is written out at the check itself — in particular it does NOT see whether both
        // of a row's askers reference it.
        CoreIntrinsics.VerifyInterceptRegistry();
        // Pass 1 (all modules): create shells so signature decoding can resolve
        // TypeDefs. Generic type definitions are held back as templates.
        foreach (var module in Modules)
        {
            var reader = module.Reader;
            foreach (var tdh in reader.TypeDefinitions)
            {
                var td = reader.GetTypeDefinition(tdh);
                string name = reader.GetString(td.Name);
                // The `<Module>` pseudo-type (TypeDef row 1) exists in EVERY assembly and
                // is almost always empty — every framework assembly's is — so modeling it
                // unconditionally would cost a ClassInfo per loaded module and buy nothing.
                // It is not always empty: a [ModuleInitializer] method makes the C# compiler
                // emit a `<Module>` .cctor that calls it, and that .cctor is the only thing
                // that EVER calls a module initializer — the attribute itself carries no
                // runtime behavior. Skipping the row would therefore not merely lose a
                // type, it would silently drop the initializer. Model the row exactly when
                // it carries members, so an ordinary program pays nothing and a module
                // initializer reaches its own cctor through the ordinary .cctor machinery
                // (rooted in CompleteAndDiscover, run eagerly at startup).
                if (name == "<Module>" && td.GetMethods().Count == 0 && td.GetFields().Count == 0)
                    continue;
                if (td.GetGenericParameters().Count > 0)
                {
                    module.GenericTemplates.Add(tdh);
                    continue;
                }
                ModelCensus.ClassesPass1++;
                var cls = new ClassInfo
                {
                    Namespace = reader.GetString(td.Namespace),
                    Name = name,
                    Handle = tdh,
                    Module = module,
                    IsAbstract = (td.Attributes & TypeAttributes.Abstract) != 0,
                    IsSealed = (td.Attributes & TypeAttributes.Sealed) != 0,
                    IsPublic = (td.Attributes & TypeAttributes.VisibilityMask) == TypeAttributes.Public,
                    IsInterface = (td.Attributes & TypeAttributes.ClassSemanticsMask) == TypeAttributes.Interface,
                    IsBeforeFieldInit = (td.Attributes & TypeAttributes.BeforeFieldInit) != 0,
                };
                // Non-generic async/await Task-family types lower to runtime
                // structs; mark them so their IL is never transpiled. The
                // generic forms are marked in Instantiate.
                cls.IntrinsicCppName = CoreIntrinsics.IntrinsicGenericCppType(cls.FullName);
                // An intrinsic *nested* value type (e.g. System.Threading.Lock.Scope)
                // decodes to a bare simple name, so it is matched only together with its
                // enclosing type — keying the intrinsic map on (enclosing, name).
                if (cls.IntrinsicCppName is null && td.GetDeclaringType() is { IsNil: false } dh)
                {
                    var declTd = reader.GetTypeDefinition(dh);
                    string declNs = reader.GetString(declTd.Namespace);
                    string declNm = reader.GetString(declTd.Name);
                    string declFull = string.IsNullOrEmpty(declNs) ? declNm : declNs + "." + declNm;
                    cls.IntrinsicCppName = CoreIntrinsics.IntrinsicNestedCppType(declFull, name);
                }
                // An intrinsic type whose C++ mapping is a by-value struct (no trailing
                // '*') is modeled as a value type even when its metadata is a reference
                // type — e.g. the file-backed MemoryMappedFile / view handles lowered to
                // small value structs. Pass 2's base-type scan only ever sets IsValueType
                // true (for a ValueType base), so this is never clobbered. (The
                // specialization path applies the same rule at CompleteShape.)
                if (cls.IntrinsicCppName is { } icn0 && !icn0.EndsWith('*'))
                    cls.IsValueType = true;
                // A nested type's simple name is not unique across declaring types
                // (e.g. each class's compiler-generated <Method>d__N iterator/async
                // state machine). Qualify its C++ identifiers with the enclosing
                // type chain so they do not collide.
                cls.CppNamePrefix = NestedCppPrefix(reader, td);
                module.ClassMap.Add(tdh, cls);
                Classes.Add(cls);
            }
        }

        // Disambiguate C++ identifiers that collide across modules. Two kinds of
        // type are DEFINED in more than one assembly under the same (namespace,
        // name): compiler-generated `<...>` types (e.g. <PrivateImplementationDetails>,
        // emitted into every assembly that uses array/span literals) and
        // framework-internal shared-source types (e.g. System.HexConverter lives in
        // CoreLib + System.Text.Json + System.Text.Encodings.Web; System.Text.UnicodeUtility
        // likewise). Each copy is referenced only within its own assembly (a `<...>`
        // type is module-local; an `internal` type via a TypeDefinition handle), so all
        // copies are reached and emitted — yet they mangle to one assembly-UNqualified
        // C++ identifier and redefine each other. Qualify the C++ name of every member
        // of a colliding set with its owning module; the (namespace, name) matching used
        // for resolution/intrinsics is untouched. Single-definition types keep a unique
        // identifier and are left alone. (Same-named nested types in different declaring
        // types — e.g. List`1.Enumerator vs ArraySegment`1.Enumerator — already carry a
        // distinct NestedCppPrefix, so they do not appear here.)
        var byCppName = new Dictionary<string, List<ClassInfo>>();
        foreach (var cls in Classes)
        {
            if (!byCppName.TryGetValue(cls.CppName, out var list))
                byCppName[cls.CppName] = list = new();
            list.Add(cls);
        }
        foreach (var list in byCppName.Values)
            if (list.Count > 1)
                foreach (var cls in list)
                    cls.CppNamePrefix = "m" + cls.Module.Index + "_" + cls.CppNamePrefix;
        Timing.Mark("build-pass1");

        // Pass 1.5: adopt third-party custom async task types into the intrinsic Task
        // family. This must run before Pass 2, whose eager signature decode creates the
        // first specializations: Instantiate stamps IntrinsicCppName once, at creation,
        // so every closed form is stamped from the registry and none needs back-filling.
        AdoptCustomAsyncTaskTypes();
        Timing.Mark("adopt-async");

        // Pass 2 (all modules): bases, fields, methods.
        foreach (var cls in Classes.ToList())
        {
            // Every type of every loaded assembly gets a model here, reachable or not,
            // with every signature eagerly decoded — so an oversized reference closure
            // shows up as heap growth in this loop before reachability even starts.
            MemoryGuard.Check("build-model");
            var module = cls.Module;
            var reader = module.Reader;
            var td = reader.GetTypeDefinition(cls.Handle);
            cls.IsByRefLike = IsByRefLikeType(reader, td);
            cls.InlineArrayLength = InlineArrayLength(reader, td);
            cls.IsExplicitLayout = (td.Attributes & TypeAttributes.LayoutMask) == TypeAttributes.ExplicitLayout;
            cls.IsAutoLayout = (td.Attributes & TypeAttributes.LayoutMask) == TypeAttributes.AutoLayout;
            var typeLayout = td.GetLayout();
            cls.LayoutPack = typeLayout.PackingSize;
            cls.LayoutSize = typeLayout.Size;
            cls.LayoutCharSetUnicode =
                (td.Attributes & TypeAttributes.StringFormatMask) == TypeAttributes.UnicodeClass;

            if (!td.BaseType.IsNil && td.BaseType.Kind == HandleKind.TypeDefinition)
            {
                cls.BaseClass = module.ClassMap[(TypeDefinitionHandle)td.BaseType];
                // Within an assembly (notably the real corlib) the base is a
                // TypeDef, so the kind flags must be derived from it here too —
                // not just in the cross-assembly TypeRef branch below. Otherwise
                // corlib's own value types (e.g. Span<T>) look like reference
                // types and skip value-type handling.
                switch (cls.BaseClass.FullName)
                {
                    // System.Enum itself is NOT a value type, exactly as in the CLR
                    // (ECMA-335 II.13: Enum, like ValueType, is an abstract REFERENCE
                    // type; only its derivatives are value types, and
                    // typeof(System.Enum).IsValueType is false). A System.Enum-typed
                    // slot holds a boxed reference — CppTypes.Of maps it to
                    // Dn2CppObject*, KindOf to Ref — so calling it a value type here
                    // made the box/constrained./typeof(T).IsValueType arms treat a
                    // Dn2CppObject* slot as a by-value struct: HashSet<System.Enum>
                    // passed &slot where the boxed receiver belongs.
                    case "System.ValueType" when cls.FullName != "System.Enum":
                        cls.IsValueType = true; break;
                    case "System.Enum": cls.IsEnum = true; break;
                    case "System.MulticastDelegate" or "System.Delegate": cls.IsDelegate = true; break;
                }
            }
            else if (!td.BaseType.IsNil && td.BaseType.Kind == HandleKind.TypeReference)
            {
                var baseRef = reader.GetTypeReference((TypeReferenceHandle)td.BaseType);
                string baseName = reader.GetString(baseRef.Name);
                string baseNs = reader.GetString(baseRef.Namespace);
                // Same System.Enum carve-out as the TypeDef arm above (a corelib-less
                // test corlib defining System.Enum lands here): Enum itself is a
                // reference type.
                if (baseNs == "System" && baseName == "ValueType")
                    cls.IsValueType = cls.FullName != "System.Enum";
                else if (baseNs == "System" && baseName == "Enum")
                    cls.IsEnum = true;
                else if (baseNs == "System" && baseName is "MulticastDelegate" or "Delegate")
                {
                    cls.IsDelegate = true;
                    if (ResolveTypeRef(module, (TypeReferenceHandle)td.BaseType) is { Kind: TypeKind.Class } bt)
                        cls.BaseClass = bt.Class;
                }
                else if (ResolveTypeRef(module, (TypeReferenceHandle)td.BaseType) is { Kind: TypeKind.Class } bt)
                    cls.BaseClass = bt.Class; // cross-assembly base
                else if (ResolveTypeRef(module, (TypeReferenceHandle)td.BaseType) is null
                         && !(baseNs == "System" && baseName == "Object"))
                    // A base whose declaring assembly is not loaded (a corelib-less
                    // build's `MyEx : SystemException`): keep its CLR name so
                    // InheritsFromException can recognize an External BCL exception
                    // ancestor at the chain's end.
                    cls.ExternalBaseName = TypeRefFullName(reader, (TypeReferenceHandle)td.BaseType);
                // System.Object (or other external bases) => treated as root.
            }
            else if (!td.BaseType.IsNil && td.BaseType.Kind == HandleKind.TypeSpecification)
            {
                // A non-generic type whose base is a closed generic — e.g.
                // ConverterList : ConfigurationList<JsonConverter> — encodes its
                // base as a TypeSpecification, which the TypeDef/TypeRef branches
                // above miss (leaving BaseClass null, so the interface-map walk
                // never reaches the base's interfaces). The derived type has no
                // generic parameters, so its base spec is always closed; decode
                // it with a null generic context (mirrors the generic-spec load
                // path and this pass's TypeSpec interface-impl handling below).
                if (reader.GetTypeSpecification((TypeSpecificationHandle)td.BaseType)
                          .DecodeSignature(SigProvider, null) is { Kind: TypeKind.Class } bt)
                    cls.BaseClass = bt.Class;
            }

            // A FieldInfo per field row, but NO decoded type — see FieldInfo.Type. The rows are
            // the type's shape and every reader that names the type reads them; what those rows
            // are TYPED at is a separate question, asked only by whoever lays the type out,
            // reflects over it or touches it with a body. Context is what the deferred decode
            // needs, and for a Pass-2 class it is empty.
            foreach (var fdh in td.GetFields())
            {
                var fd = reader.GetFieldDefinition(fdh);
                ModelCensus.FieldsPass2++;
                var fm = ReadFieldMarshal(reader, fd);
                cls.Fields.Add(new FieldInfo
                {
                    DeclaringClass = cls,
                    Name = reader.GetString(fd.Name),
                    Handle = fdh,
                    IsStatic = (fd.Attributes & FieldAttributes.Static) != 0,
                    IsLiteral = (fd.Attributes & FieldAttributes.Literal) != 0,
                    IsThreadStatic = HasThreadStatic(reader, fd),
                    ByValArraySize = ByValArraySizeOf(fm),
                    MarshalAs = fm.Kind,
                    MarshalSizeConst = fm.SizeConst,
                    MarshalArraySubType = fm.ArraySubType,
                    ExplicitOffset = fd.GetOffset(),
                    Attributes = fd.Attributes,
                });
            }

            // An enum's underlying integer type is the type of its single instance
            // (non-literal) field, `value__`. Captured so a packed enum array uses the
            // underlying width; defaults to Int32 when not found.
            //
            // This DOES decode — one field type, for every enum the run loads, and the only
            // decode Pass 2 still makes. It is left eager because it is the cheap end of the
            // trade twice over: the type it reads is a primitive, so it names no generic and
            // mints nothing, and deferring it would only move the read to EnumUnderlying's
            // first consumer, which is every packed enum array. The FirstOrDefault's predicate
            // reads only the attribute bits, so a non-enum decodes nothing and an enum's
            // literals are never touched.
            if (cls.IsEnum
                && cls.Fields.FirstOrDefault(f => !f.IsStatic && !f.IsLiteral) is { Type.Kind: TypeKind.Primitive } vf)
                cls.EnumUnderlying = vf.Type.Primitive;

            foreach (var iih in td.GetInterfaceImplementations())
            {
                var ii = reader.GetInterfaceImplementation(iih);
                if (ii.Interface.Kind == HandleKind.TypeDefinition)
                    cls.Interfaces.Add(module.ClassMap[(TypeDefinitionHandle)ii.Interface]);
                else if (ii.Interface.Kind == HandleKind.TypeReference
                         && ResolveTypeRef(module, (TypeReferenceHandle)ii.Interface) is { Kind: TypeKind.Class } it)
                    cls.Interfaces.Add(it.Class!);
                else if (ii.Interface.Kind == HandleKind.TypeSpecification
                         && reader.GetTypeSpecification((TypeSpecificationHandle)ii.Interface)
                                  .DecodeSignature(SigProvider, null) is { Kind: TypeKind.Class } its)
                    // Closed generic interface (e.g. a `yield` state machine
                    // implementing IEnumerable<int>/IEnumerator<int>). The TypeSpec
                    // decodes to an instantiated interface ClassInfo.
                    cls.Interfaces.Add(its.Class!);
                else if (ii.Interface.Kind == HandleKind.TypeReference
                         && ResolveTypeRef(module, (TypeReferenceHandle)ii.Interface) is null
                         && TypeRefFullName(reader, (TypeReferenceHandle)ii.Interface) is { } extName)
                    // An interface whose declaring assembly was not referenced at
                    // transpile (so ResolveTypeRef finds no loaded type): keep its
                    // CLR name for a downstream consumer (the hot-update patch
                    // converter matches it against the base-image ABI manifest).
                    // The normal emit path never reads this list.
                    cls.ExternalInterfaceNames.Add(extName);
                else if (cls.Module == AppModule)
                    throw new NotSupportedException($"{cls.FullName}: unsupported interface reference");
                // Reference assemblies (a real CoreLib) carry closed/generic
                // interface impls (IValueTaskSource<T>, ...) we don't model yet;
                // skip rather than abort so the BCL can be loaded.
            }

            // A MethodInfo per method row, but NO decoded signature. The decode is the
            // expensive half — and it is what mints closed generics — while most methods of
            // most loaded assemblies are never asked for theirs: a `-r` assembly contributes
            // every method it declares here, and a program reaches a few hundred of them.
            // What this loop does still have to get right is Context, which the deferred
            // decode reads; for a Pass-2 method it is empty.
            foreach (var mdh in td.GetMethods())
            {
                var md = reader.GetMethodDefinition(mdh);
                if (md.GetGenericParameters().Count > 0)
                {
                    module.GenericMethodTemplates.Add(mdh);
                    continue;
                }
                var (isUco, ucoEntryPoint, nativeImpl) = ReadMethodInteropAttributes(module, md);
                ModelCensus.MethodsPass2++;
                var mi = new MethodInfo
                {
                    DeclaringClass = cls,
                    Name = reader.GetString(md.Name),
                    Handle = mdh,
                    Module = module,
                    Attributes = md.Attributes,
                    ImplAttributes = md.ImplAttributes,
                    Rva = md.RelativeVirtualAddress,
                    PInvoke = ReadPInvoke(reader, md),
                    IsUnmanagedCallersOnly = isUco,
                    UnmanagedEntryPoint = ucoEntryPoint,
                };
                cls.Methods.Add(mi);
                module.MethodMap.Add(mdh, mi);
                if (nativeImpl is { } ni)
                    RegisterNativeImpl(ni, mi);
            }
        }
        Timing.Mark("build-pass2");

        // --cut specs validate against the Pass-2 member lists (a spec resolving
        // to nothing is a hard error), before reachability — the first consumer
        // of the cut set — runs.
        ValidateCutMethods();

        DiscoverPreservationPolicies();

        // Pass 2.5: populate MethodImpls for all non-generic classes. Interfaces
        // included: an interface carries .override rows when it explicitly implements
        // a base interface's static abstract member — the only way C# admits it as a
        // static-abstract-constrained type argument — and ResolveStaticVirtualImpl
        // probes that map. (Specializations already populate via CompleteMembers.)
        foreach (var cls in Classes.ToList())
        {
            if (cls.IsDelegate || cls.IsEnum || cls.GenericArity > 0)
                continue;
            var td = cls.Module.Reader.GetTypeDefinition(cls.Handle);
            PopulateMethodImpls(cls, td, cls.Module, GenericContext.Empty);
        }

        // Pass 3: vtables (bases first), then interface slots in
        // declaration order.
        BuildVtables();
        // Non-generic interfaces only. A closed generic interface numbers its own slots
        // when its members are decoded (BuildVtableForSpecialization's IsInterface arm),
        // so this pass has nothing to do for one — at this point in the build it
        // has not been completed and its method list is empty. Saying so is not a tidy-up:
        // reading .Methods on a specialization decodes it, decoding instantiates the
        // generics its signatures name, and those are appended to Classes — which is the
        // very list being walked.
        foreach (var cls in Classes.Where(c => c.IsInterface && c.GenericArity == 0))
        {
            int slot = 0;
            foreach (var m in cls.Methods)
                m.VtableSlot = slot++;
        }

        int epToken = AppModule.PE.PEHeaders.CorHeader!.EntryPointTokenOrRelativeVirtualAddress;
        if (epToken != 0)
        {
            var epHandle = (MethodDefinitionHandle)SRME.EntityHandle(epToken);
            EntryPoint = AppModule.MethodMap.TryGetValue(epHandle, out var ep) ? ep : null;
        }
        Timing.Mark("build-vtables");

        // Monomorphization: complete the generic instances reached during
        // signature decoding, then scan reachable IL for more, to a fixpoint.
        CompleteAndDiscover();
        Timing.Mark("discover");

        // Canonical shared generics (opt-in): group layout-coinciding
        // instantiations under a canonical owner and reach each grouped
        // method's owner counterpart, to a fixpoint. Runs after the discovery
        // fixpoint so every argument's enum-ness/value-ness (set during pass 2
        // / completion) is final; instantiations discovered later (during body
        // emission) are re-linked each emit round via the same entry point.
        if (SharedGenericsEnabled)
        {
            SyncSharedGenerics();
            Timing.Mark("shared-sync");
        }
        // The model is now whole — everything the discovery fixpoint will ever
        // create exists (emission adds only what a body's own tokens name). This
        // is the point the heap curve's biggest step lands on, so it is the point
        // worth counting.
        ModelCensus.Report("build", this);
    }

    // ---- generic instantiation (monomorphization) ----

    /// <summary>The deepest type-argument nesting any instantiation created so far
    /// carries (<see cref="ClassInfo.GenericDepth"/>, and the same measure over a
    /// generic method's arguments). Reported by the phase instrumentation, and the
    /// quantity <see cref="MaxInstantiationDepth"/> bounds.</summary>
    internal int MaxGenericArgDepth { get; private set; }

    /// <summary>Type-argument nesting depth of one type argument. A closed
    /// specialization carries its own precomputed depth, so this is O(1) for the
    /// common case; array/byref/pointer wrappers each add a level, and every other
    /// shape — primitive, external, generic parameter — is a leaf.
    ///
    /// <para>A wrapper MUST count a level. Were it transparent, an array-deepening
    /// self-recursion — <c>Box&lt;T&gt;.Next : Box&lt;T[]&gt;</c>, or
    /// <c>M&lt;T&gt;() =&gt; M&lt;T[]&gt;()</c> — would keep <c>Box&lt;int&gt;</c>,
    /// <c>Box&lt;int[]&gt;</c>, <c>Box&lt;int[][]&gt;</c> all at depth 1, slip past the depth
    /// bound and burn the instantiation <em>count</em> cap instead: gigabytes of shells and a
    /// wrong diagnostic. Depth is a recursion budget, not a semantic measure — counting a
    /// wrapper only moves the abort threshold, never the emitted output.</para></summary>
    private static int TypeArgDepth(TypeDesc t) => t.Kind switch
    {
        TypeKind.Class => t.Class!.GenericDepth,
        TypeKind.SZArray or TypeKind.MDArray or TypeKind.ByRef or TypeKind.Pointer =>
            1 + TypeArgDepth(t.Element!),
        // Hot-update patch converter only: a base-image generic the converter never
        // loaded, so there is no ClassInfo to carry a precomputed depth.
        TypeKind.ExternalGeneric => 1 + MaxArgDepth(t.GenericArgs!),
        _ => 0,
    };

    /// <summary>The deepest of a type-argument list (0 for an empty list).</summary>
    private static int MaxArgDepth(TypeDesc[] args)
    {
        int max = 0;
        foreach (var a in args)
        {
            int d = TypeArgDepth(a);
            if (d > max)
                max = d;
        }
        return max;
    }

    // ---- the monomorphization bound ----
    //
    // Monomorphization has no natural fixpoint. A self-nesting generic —
    // `static void M<T>() => M<List<T>>();` — asks for M<int>, M<List<int>>,
    // M<List<List<int>>>, … forever, and each instantiation is a fresh ClassInfo
    // with fresh fields, methods, a vtable and freshly decoded signatures whose
    // own generics instantiate in turn. Nothing in the discovery fixpoint stops
    // it: an unbounded transpiler simply allocates until the machine dies. Every
    // AOT compiler bounds this (IL2CPP: --maximum-recursive-generic-depth,
    // default 7).
    //
    // Depth is the complete guard: an unbounded expansion must keep producing
    // *distinct* instantiations from a finite set of generic definitions, and the
    // only way to do that is to nest the type arguments ever deeper. The count is
    // a backstop against that reasoning being wrong — a merely absurd (but finite)
    // closure still fails loudly instead of eating the machine.
    //
    // Both are env-overridable, and that is sound where TinyInlineIlBytes' must-be-
    // a-build-constant rule would not be: those constants change *which* text is
    // emitted for a program that compiles either way, so a host-dependent value
    // would break the self-host byte-for-byte fixpoint. A cap only ever turns a
    // run into an abort or back — it cannot perturb the output of a transpile that
    // succeeds. A real game that legitimately needs more gets a lever instead of a
    // fork of the compiler.

    /// <summary>Type-argument nesting ceiling. Real inputs nest single digits deep, so 32
    /// leaves ample headroom while still catching a recursive generic within a few dozen
    /// instantiations rather than a few million. Raise with
    /// <c>DN2CPP_MAX_GENERIC_DEPTH</c>.</summary>
    private static readonly int MaxInstantiationDepth =
        EnvKnobs.PositiveInt(EnvKnobs.MaxGenericDepth, 32);

    /// <summary>Ceiling on the total number of closed generic type + method
    /// instantiations — orders of magnitude above what a real corpus creates. Raise with
    /// <c>DN2CPP_MAX_INSTANTIATIONS</c>.</summary>
    private static readonly int MaxInstantiations =
        EnvKnobs.PositiveInt(EnvKnobs.MaxInstantiations, 1_000_000);

    /// <summary>The offending instantiation, spelled for a human. Truncated, because a
    /// runaway nest mangles into a name thousands of characters long that buries the
    /// rest of the message — and the reach chain the error carries localizes the site
    /// far better than the full spelling would.</summary>
    private static string Ellipsize(string s) =>
        s.Length <= 100 ? s : s[..100] + $"…({s.Length} chars)";

    /// <summary>Where the instantiation that tripped a bound came from. The two stories are
    /// very different, and which one it is decides what the caller can do about it. A member's
    /// SIGNATURE drives an instantiation whether or not a call does — decoding one instantiates
    /// the generics it names, so a member typed at a deeper instantiation of its own declaring
    /// type recurses on its own. A reachability edge, by contrast, means real code asked, and
    /// the chain names the code.
    ///
    /// <para>Both kinds of member are decoded on demand, and what differs is the demand. A
    /// METHOD's signature is read because something reached the method, so a self-nesting
    /// signature like <c>GDTask&lt;T&gt;.SuppressCancellationThrow() -&gt;
    /// GDTask&lt;(bool, T)&gt;</c> is not decoded at all in a program that never calls it. A
    /// FIELD's type is read because something needs its declaring type's LAYOUT, and <em>a
    /// layout is not a call</em>: the emit-set closure asks every emitted class for one. So a
    /// field's nest can be reached with an empty chain, and the member is then the only thing
    /// that can name the fault. Both are printed when both are known.</para></summary>
    private string InstantiationDriver()
    {
        // The OPEN definition's name, not the closed spec's: a runaway's specialization mangles
        // into thousands of characters, and the actionable fact is which member of which
        // generic — `GodotTask.GDTask.SuppressCancellationThrow`, not the nest it reached.
        string chain = _currentScan is { } m ? $"\n  [chain: {ReachChain(m)}]" : "";
        if (_decodingMember is { } d)
            return $"\n  Driven by the signature of {(d.IsField ? "field" : "method")} "
                 + $"{GenericDefFullName(d.Spec)}.{d.Member} — decoding a member's signature "
                 + "instantiates the generics that signature names, so a member typed at a deeper "
                 + "instantiation of its own declaring type recurses. "
                 + (d.IsField
                     ? "A FIELD's type is decoded when something needs its declaring type's LAYOUT, "
                       + "and a layout is not a call — the emit-set closure asks every emitted class "
                       + "for one — so any chain below names what wanted the TYPE, not the field."
                     : "A METHOD signature is decoded only when something asks for it, so this nest is "
                       + "one the program reached for.")
                 + chain;
        return chain;
    }

    /// <summary>Enforces the monomorphization bound as one instantiation is about to
    /// be created. Throws <see cref="InstantiationBoundException"/> — a
    /// <see cref="NotSupportedException"/>, so <c>TranspileDriver</c> still turns it into an
    /// actionable <c>error:</c> line and exit code 2, but a distinct type so the
    /// <c>--measure</c> gap-row arms rethrow it instead of swallowing it.</summary>
    private void CheckInstantiationBound(int depth, string what)
    {
        if (depth > MaxInstantiationDepth)
            throw new InstantiationBoundException(
                $"generic instantiation nested {depth} deep while instantiating {Ellipsize(what)}: "
                + $"past the {MaxInstantiationDepth}-level limit. A generic that instantiates "
                + "itself at an ever-deeper type argument (e.g. `void M<T>() => M<List<T>>()`) "
                + "expands forever — every AOT compiler bounds this. Rewrite the recursion, or "
                + "raise the limit with DN2CPP_MAX_GENERIC_DEPTH=<n> if the nesting is genuine."
                + InstantiationDriver());
        if (_instanceCount + _methodInstanceCount >= MaxInstantiations)
            throw new InstantiationBoundException(
                $"generic instantiation count passed the {MaxInstantiations} limit while "
                + $"instantiating {Ellipsize(what)}. The reference closure is monomorphizing far beyond any "
                + "real program: drop --auto-ref for an explicit -r set, point at a Release rather "
                + "than a Debug bin directory (a Debug one drags Roslyn / Cecil / TestPlatform in), "
                + "or raise the limit with DN2CPP_MAX_INSTANTIATIONS=<n>."
                + InstantiationDriver());
    }

    /// <summary>Whether <paramref name="e"/> is one of the two exceptions no swallowing
    /// arm may eat. <see cref="StrictCompletionException"/>: swallowed, a strict-mode
    /// violation would not fail the run, it would quietly change which bodies get shared.
    /// <see cref="InstantiationBoundException"/>: recording an overrun and carrying on
    /// would not merely defeat the monomorphization bound, it would feed it. Every broad
    /// catch is filtered with <c>when (!IsMustEscape(e))</c> so these two escape — note
    /// InstantiationBoundException IS a NotSupportedException (so TranspileDriver renders
    /// it as an <c>error:</c> line), which is why type-list filters need this test too.</summary>
    internal static bool IsMustEscape(Exception e) =>
        e is StrictCompletionException or InstantiationBoundException;

    /// <summary>The member whose signature is being decoded right now — a field, in
    /// <see cref="CompleteShape"/>'s field loop, or a method, in
    /// <see cref="DecodeSignature"/> — for <see cref="InstantiationDriver"/>. Null outside a
    /// decode: an instantiation created anywhere else came from a reachability edge, which
    /// <c>_currentScan</c> names instead.</summary>
    private (ClassInfo Spec, string Member, bool IsField)? _decodingMember;

    /// <summary>Returns (creating a shell if needed) the closed specialization
    /// of a generic type. Field/method/vtable population is deferred so it can
    /// run after the non-generic model — including base vtables — is built.</summary>
    public ClassInfo Instantiate(Module module, TypeDefinitionHandle templateHandle, TypeDesc[] args)
    {
        var reader = module.Reader;
        // Keying on the caller's args array is sound for the same reason handing it to
        // GenericContext below always was: argument vectors are never mutated after the
        // call (a mutation would corrupt the spec's Context before it corrupted this key).
        var byArgs = InstancesFor(module, templateHandle);
        if (byArgs.TryGetValue(args, out var existing))
            return existing;

        var td = reader.GetTypeDefinition(templateHandle);
        string rawName = StripArity(reader.GetString(td.Name));

        int depth = 1 + MaxArgDepth(args);
        if (depth > MaxGenericArgDepth)
            MaxGenericArgDepth = depth;
        CheckInstantiationBound(depth, rawName + "<" + string.Join(", ", args.Select(a => a.ToString())) + ">");

        // The arity-stripped open-definition full name (e.g.
        // "System.Runtime.CompilerServices.AsyncTaskMethodBuilder"), used to
        // detect generic Task-family types that lower to runtime structs.
        string ns = reader.GetString(td.Namespace);
        string defFull = string.IsNullOrEmpty(ns) ? rawName : ns + "." + rawName;

        // A nested type's simple name is not unique (List<T>.Enumerator and
        // SegmentedArray<T>.Enumerator both decode to "Enumerator"); qualify it
        // with its declaring type chain so distinct closes don't collide on the
        // mangled C++ name.
        for (var declHandle = td.GetDeclaringType(); !declHandle.IsNil;)
        {
            var declTd = reader.GetTypeDefinition(declHandle);
            rawName = StripArity(reader.GetString(declTd.Name)) + "_" + rawName;
            declHandle = declTd.GetDeclaringType();
        }
        string mangled = rawName + "_" + string.Join("_", args.Select(MangleArg));

        ModelCensus.ClassesSpec++;
        var spec = new ClassInfo
        {
            Namespace = ns,
            Name = mangled,
            Handle = templateHandle,
            Module = module,
            IsAbstract = (td.Attributes & TypeAttributes.Abstract) != 0,
            IsSealed = (td.Attributes & TypeAttributes.Sealed) != 0,
            IsPublic = (td.Attributes & TypeAttributes.VisibilityMask) == TypeAttributes.Public,
            IsInterface = (td.Attributes & TypeAttributes.ClassSemanticsMask) == TypeAttributes.Interface,
            IsBeforeFieldInit = (td.Attributes & TypeAttributes.BeforeFieldInit) != 0,
            GenericArity = args.Length,
            GenericDepth = depth,
            Context = new GenericContext(args, Array.Empty<TypeDesc>()),
            // Element-aware for the SIMD vectors: a non-primitive element (Vector<Int128>)
            // is left non-intrinsic so its real element-wise BCL IL transpiles, as before.
            // …or a third-party async task-family template adopted in Pass 1.5:
            // GDTask<T>, its builder, its awaiter. Stamped here, at creation, so a closed
            // form is intrinsic before CompleteShape ever looks at it.
            IntrinsicCppName = CoreIntrinsics.IntrinsicGenericCppType(defFull, args)
                               ?? AdoptedAsyncCpp(module, templateHandle),
        };
        byArgs.Add(args, spec);
        _instanceCount++;
        Classes.Add(spec);
        EnqueuePending(spec);
        // Intrinsic ValueTask<T> and TaskCompletionSource<T> both allocate a real
        // Task<T> backing. Create its identity while the model can still grow, even when
        // no managed signature exposes it; emission may only look it up.
        if (args.Length == 1
            && (defFull is "System.Threading.Tasks.ValueTask"
                          or "System.Threading.Tasks.TaskCompletionSource"
                || AdoptedAsyncKey(spec) == "System.Threading.Tasks.ValueTask")
            && TypeIndex().TryGetValue(("System.Threading.Tasks", "Task`1"), out var taskDefs))
            Instantiate(taskDefs[0].Item1, taskDefs[0].Item2, args);
        return spec;
    }

    /// <summary>Force-emits one hotupdate-refs.txt root — a closed generic
    /// instantiation <c>OpenDef[arg,...]</c> (CLR type names) the base image must
    /// carry so a patch that never appears in the base program can still bind it
    /// (HybridCLR's AOTGenericReferences). Resolves the open generic definition
    /// by name+arity and the type arguments, instantiates the specialization,
    /// and reaches every method with a body — the base cannot know which members
    /// the patch will call, so it emits the whole instantiation.</summary>
    private void SeedGenericRoot(string root)
    {
        // A generic-method root (DeclType::Method[arg,...]) — the `::` separates it
        // from a generic-type root (OpenDef[arg,...]).
        int dc = root.IndexOf("::", StringComparison.Ordinal);
        if (dc >= 0)
        {
            SeedGenericMethodRoot(root, dc);
            return;
        }
        int lb = root.IndexOf('[');
        if (lb < 0 || !root.EndsWith("]", StringComparison.Ordinal))
            throw new NotSupportedException($"hotupdate-refs: malformed generic instantiation '{root}' (expected OpenDef[arg,...])");
        string openDef = root.Substring(0, lb).Trim();
        string argList = root.Substring(lb + 1, root.Length - lb - 2);
        var argDescs = SplitTopLevel(argList).Select(a => ResolveRootTypeName(a.Trim())).ToArray();

        foreach (var module in Modules)
        {
            foreach (var h in module.GenericTemplates)
            {
                var td = module.Reader.GetTypeDefinition(h);
                if (td.GetGenericParameters().Count != argDescs.Length)
                    continue;
                string ns = module.Reader.GetString(td.Namespace);
                string full = string.IsNullOrEmpty(ns)
                    ? StripArity(module.Reader.GetString(td.Name))
                    : ns + "." + StripArity(module.Reader.GetString(td.Name));
                if (full != openDef)
                    continue;
                var spec = Instantiate(module, h, argDescs);
                EnsureCompleted(spec);
                foreach (var mth in spec.Methods.ToList())
                    if (mth.Rva != 0)
                        Reach(mth);
                return;
            }
        }
        throw new NotSupportedException($"hotupdate-refs: no loaded open generic type '{openDef}' with arity {argDescs.Length} for root '{root}'");
    }

    /// <summary>Force-emits one hotupdate-refs.txt generic-method root —
    /// <c>DeclType::Method[arg,...]</c> (CLR type names). The base image must carry
    /// a closed instantiation of a base-image generic method a patch calls but the
    /// base program never does, so the hot-update loader can bind it (the method
    /// analogue of the generic-type roots — a generic method emits every
    /// instantiation under one reflected name, so the loader disambiguates them by
    /// sigShape, which needs the wanted instantiation present). Resolves the
    /// declaring class by name and instantiates + reaches every generic-method
    /// template on it named <paramref name="root"/>'s method at the given
    /// method-generic arity (as with a type root, the base cannot know which
    /// overload the patch calls). The declaring type is non-generic; a generic
    /// method on a generic type combines both surfaces and is a later slice.</summary>
    private void SeedGenericMethodRoot(string root, int dc)
    {
        string decl = root.Substring(0, dc).Trim();
        string rest = root.Substring(dc + 2).Trim();
        int lb = rest.IndexOf('[');
        // No type-argument list: a plain (non-generic) method root.
        if (lb < 0 && rest.Length > 0 && !rest.Contains(']'))
        {
            SeedMethodRoot(root, decl, rest);
            return;
        }
        if (lb < 0 || !rest.EndsWith("]", StringComparison.Ordinal))
            throw new NotSupportedException($"hotupdate-refs: malformed generic method instantiation '{root}' (expected DeclType::Method[arg,...])");
        string methodName = rest.Substring(0, lb).Trim();
        string argList = rest.Substring(lb + 1, rest.Length - lb - 2);
        var methodArgs = SplitTopLevel(argList).Select(a => ResolveRootTypeName(a.Trim())).ToArray();

        var cls = FindClassByFullName(decl);
        if (cls is null || cls.GenericArity > 0)
            throw new NotSupportedException($"hotupdate-refs: no loaded non-generic type '{decl}' for generic method root '{root}'");
        EnsureCompleted(cls);

        bool any = false;
        foreach (var mh in cls.Module.Reader.GetTypeDefinition(cls.Handle).GetMethods())
        {
            var md = cls.Module.Reader.GetMethodDefinition(mh);
            if (cls.Module.Reader.GetString(md.Name) != methodName
                || md.GetGenericParameters().Count != methodArgs.Length)
                continue;
            Reach(InstantiateMethodOnClass(cls, cls.Module, mh, methodArgs));
            any = true;
        }
        if (!any)
            throw new NotSupportedException($"hotupdate-refs: no generic method '{decl}::{methodName}' with {methodArgs.Length} type argument(s) for root '{root}'");
    }

    /// <summary>Force-reaches one hotupdate-refs.txt non-generic method root —
    /// <c>DeclType::Method</c> (no type-argument list). A patch can only bind a
    /// base-image method whose reflection row carries a callable fnPtr/invoker
    /// pair, which exists only for reachable methods — so a method the base
    /// program never calls (most usefully an engine-shim method of a
    /// GDExtension hot-update base, whose synthesized wrapper is also
    /// reachability-gated) is rooted here. Reaches every same-name overload
    /// with a body (the base cannot know which one the patch calls).</summary>
    private void SeedMethodRoot(string root, string decl, string methodName)
    {
        var cls = FindClassByFullName(decl);
        if (cls is null || cls.GenericArity > 0)
            throw new NotSupportedException($"hotupdate-refs: no loaded non-generic type '{decl}' for method root '{root}'");
        EnsureCompleted(cls);
        bool any = false;
        foreach (var m in cls.Methods)
        {
            if (m.Name != methodName || m.Rva == 0)
                continue;
            Reach(m);
            any = true;
        }
        if (!any)
            throw new NotSupportedException($"hotupdate-refs: no method '{decl}::{methodName}' with a body for root '{root}'");
    }

    /// <summary>Splits a comma-separated generic-argument list at top-level
    /// commas only (a nested instantiation's own args are bracketed).</summary>
    private static List<string> SplitTopLevel(string s)
    {
        var parts = new List<string>();
        int depth = 0, start = 0;
        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
            if (c == '[') depth++;
            else if (c == ']') depth--;
            else if (c == ',' && depth == 0)
            {
                parts.Add(s.Substring(start, i - start));
                start = i + 1;
            }
        }
        parts.Add(s.Substring(start));
        return parts;
    }

    /// <summary>Resolves a hotupdate-refs type-argument name (a CLR full name, or
    /// a nested <c>OpenDef[arg,...]</c>) to a TypeDesc: a primitive/String by
    /// name, a nested closed generic recursively, a loaded class, else an
    /// external reference.</summary>
    private TypeDesc ResolveRootTypeName(string name)
    {
        int lb = name.IndexOf('[');
        if (lb >= 0 && name.EndsWith("]", StringComparison.Ordinal))
        {
            // A nested closed generic argument (e.g. List[System.Int32]): resolve
            // its open def + args the same way, so List[List[System.Int32]] works.
            string openDef = name.Substring(0, lb).Trim();
            var nestedArgs = SplitTopLevel(name.Substring(lb + 1, name.Length - lb - 2))
                .Select(a => ResolveRootTypeName(a.Trim())).ToArray();
            foreach (var module in Modules)
                foreach (var h in module.GenericTemplates)
                {
                    var td = module.Reader.GetTypeDefinition(h);
                    if (td.GetGenericParameters().Count != nestedArgs.Length)
                        continue;
                    string ns = module.Reader.GetString(td.Namespace);
                    string full = string.IsNullOrEmpty(ns)
                        ? StripArity(module.Reader.GetString(td.Name))
                        : ns + "." + StripArity(module.Reader.GetString(td.Name));
                    if (full == openDef)
                    {
                        var spec = Instantiate(module, h, nestedArgs);
                        EnsureCompleted(spec);
                        return TypeDesc.MakeClass(spec);
                    }
                }
            throw new NotSupportedException($"hotupdate-refs: no loaded open generic type '{openDef}' with arity {nestedArgs.Length}");
        }
        return name switch
        {
            "System.Boolean" => TypeDesc.MakePrimitive(PrimitiveTypeCode.Boolean),
            "System.Char" => TypeDesc.MakePrimitive(PrimitiveTypeCode.Char),
            "System.SByte" => TypeDesc.MakePrimitive(PrimitiveTypeCode.SByte),
            "System.Byte" => TypeDesc.MakePrimitive(PrimitiveTypeCode.Byte),
            "System.Int16" => TypeDesc.MakePrimitive(PrimitiveTypeCode.Int16),
            "System.UInt16" => TypeDesc.MakePrimitive(PrimitiveTypeCode.UInt16),
            "System.Int32" => TypeDesc.MakePrimitive(PrimitiveTypeCode.Int32),
            "System.UInt32" => TypeDesc.MakePrimitive(PrimitiveTypeCode.UInt32),
            "System.Int64" => TypeDesc.MakePrimitive(PrimitiveTypeCode.Int64),
            "System.UInt64" => TypeDesc.MakePrimitive(PrimitiveTypeCode.UInt64),
            "System.Single" => TypeDesc.MakePrimitive(PrimitiveTypeCode.Single),
            "System.Double" => TypeDesc.MakePrimitive(PrimitiveTypeCode.Double),
            "System.String" => TypeDesc.MakePrimitive(PrimitiveTypeCode.String),
            "System.Object" => TypeDesc.MakePrimitive(PrimitiveTypeCode.Object),
            _ => FindClassByFullName(name) is { } cls ? TypeDesc.MakeClass(cls) : TypeDesc.MakeExternal(name),
        };
    }

    /// <summary>Strips the CLR generic arity marker (e.g. "Box`1" -> "Box").</summary>
    private static string StripArity(string name)
    {
        int tick = name.IndexOf('`');
        return tick >= 0 ? name.Substring(0, tick) : name;
    }

    private static string MangleArg(TypeDesc t) =>
        // The Class arm reads the live cached ClassInfo.MangleFragment (prefix-invalidated
        // there, and zero-alloc already); every other arm computes once per descriptor
        // into TypeDesc.MangleCache — sound because descriptors are immutable
        // and the interned kinds (primitives, canon placeholders, per-class Desc) are
        // shared, so the fragment is built once per distinct type rather than per ask.
        // No canon placeholder is Class-kind (they are constructed as primitives), so
        // the bypass cannot skip the '$' arm.
        t.Kind == TypeKind.Class && !t.IsCanonPlaceholder
            ? t.Class!.MangleFragment
            : t.MangleCache ??= t switch
            {
                // Canonical placeholders mangle into their own token space: '$' can
                // never appear in a sanitized class/external mangle or a primitive
                // name, so a canonical instantiation key (Dictionary<$CnInt32,…>) can
                // never collide with a genuine one (Dictionary<Int32,…>).
                { IsCanonPlaceholder: true } => t.CanonAnyIndex >= 0 ? "$CnAny" + t.CanonAnyIndex
                    : t.IsObject ? "$CnRef" : "$Cn" + t.Primitive,
                { Kind: TypeKind.Primitive } => t.Primitive.ToString(),
                // A NAMED type's fragment goes through CppNaming.MangleFragment, which
                // escapes it out of the other spellings' token spaces — a
                // global-namespace `class Int32 {}` would otherwise read as this method's
                // primitive arm, a `class FooArr {}` as the SZArray arm's `Foo[]`,
                // and the pair would share an instantiation-cache key. The
                // Class arm above takes the same route through the per-class cache.
                // An ExternalGeneric's name is already the closed instantiation's mangled
                // registry name, so it identifies the type exactly as a plain external's does.
                { Kind: TypeKind.External or TypeKind.ExternalGeneric } =>
                    CppNaming.MangleFragment(CppNaming.Sanitize(t.ExternalName!)),
                // The type-constructor arms: base + one marker per level, read back right to
                // left (CppNaming.MangleFragment states the injectivity contract). A single
                // "T" for all of them would put Shadow<int[,]> and Shadow<double[,]> in one
                // specialization — one static, one type-info — silently.
                { Kind: TypeKind.SZArray } => MangleArg(t.Element!) + "Arr",
                { Kind: TypeKind.MDArray } => MangleArg(t.Element!) + "Md" + t.Rank,
                { Kind: TypeKind.Pointer } => MangleArg(t.Element!) + "Pointer",
                { Kind: TypeKind.ByRef } => MangleArg(t.Element!) + "Byref",
                // The two leaf bases that are not names. Degenerate as a type argument, yet
                // reached: an open Foo<!0> is minted whenever a signature decodes under an
                // empty context, and !0 sharing a key with !1 is the same silent aliasing.
                { Kind: TypeKind.GenericVar } =>
                    "Gvar" + (t.GenVarIsMethod ? "M" : "T") + t.GenVarIndex,
                { Kind: TypeKind.Template } =>
                    "Gtpl" + t.TemplateModule!.Index + "_" + SRME.GetToken(t.TemplateHandle),
                _ => throw new InvalidOperationException(
                    $"no generic-argument mangle for type kind {t.Kind} ({t}) — every kind must "
                    + "spell a fragment no other kind can, or two type arguments share one "
                    + "specialization (CppNaming.MangleFragment)"),
            };

    /// <summary>Canonical mangle for an SZArray element type — the suffix of the
    /// <c>ti_arr_&lt;…&gt;</c> symbol CppEmitter emits for the array <c>element[]</c>
    /// — shared by the collection set and the emitter so they agree.</summary>
    internal static string ArrayElemMangle(TypeDesc element) => MangleArg(element);

    /// <summary>SZArray element types reached via <c>newarr T</c> / <c>typeof(T[])</c>,
    /// keyed by <see cref="ArrayElemMangle"/> so value-equal TypeDescs dedupe
    /// (TypeDesc has reference identity). CppEmitter emits one per-element
    /// <c>ti_arr_&lt;key&gt;</c> per entry and registers its <c>"Elem[]"</c> name.</summary>
    public Dictionary<string, TypeDesc> ArrayElementTypes { get; } = new(System.StringComparer.Ordinal);

    /// <summary>Records that <paramref name="element"/><c>[]</c> can exist at runtime, so
    /// a per-element array type-info is emitted for it. A jagged element (itself
    /// an SZArray) recurses so the whole GetElementType chain has precise handles; an
    /// MDArray element additionally notes the MD type itself
    /// (<see cref="NoteMdArrayType"/>) — its <c>ti_md_</c> is the linkable constant the
    /// SZ handle's elementType names. Only concrete element kinds are tracked; an
    /// unresolved generic var/template is ignored (the reached IL it is collected from
    /// is already monomorphized).</summary>
    public void NoteArrayElementType(TypeDesc element)
    {
        if (element.Kind is not (TypeKind.Primitive or TypeKind.Class or TypeKind.External or TypeKind.SZArray or TypeKind.MDArray))
            return;
        // A canonical placeholder element can only surface while trial-compiling
        // a shared-body candidate; such a site taints the body (no shared body
        // ever allocates a placeholder array), so its synthetic ti_arr_ handle
        // must never be emitted.
        if (ContainsCanonPlaceholder(element))
            return;
        if (!ArrayElementTypes.TryAdd(ArrayElemMangle(element), element))
            return;
        // An array type-info now exists, so Type.GetInterfaces() over one can be asked
        // and needs the shared non-generic rows.
        NoteArrayItfRowsNeeded();
        if (element.Kind == TypeKind.SZArray)
            NoteArrayElementType(element.Element!);
        else if (element.Kind == TypeKind.MDArray)
            NoteMdArrayType(element);
    }

    /// <summary>MDArray types whose identity is named by emitted metadata: either
    /// as an SZArray element (<c>new int[2][,]</c>, <c>typeof(int[][,])</c>) or
    /// inside a constructed type's identity closure, keyed by <see cref="ArrayElemMangle"/>.
    /// CppEmitter emits one static <c>ti_md_&lt;key&gt;</c> per entry and registers it in
    /// the type registry, where the runtime interner (<c>dn2cpp_array_ti</c>) resolves
    /// every <c>new T[,]</c> of the shape to it — so the static handle IS the interned
    /// identity, not a second one. A bare instruction token still uses the runtime
    /// interner directly unless another emitted identity names its static handle.</summary>
    internal Dictionary<string, TypeDesc> MdArrayTypes { get; } = new(System.StringComparer.Ordinal);

    /// <summary>See <see cref="MdArrayTypes"/>. Recurses like NoteArrayElementType so
    /// the whole GetElementType chain stays linkable, notes a referenced enum element's
    /// own ti_ (the NoteReflectedArrayType precedent), and keys the shared rank&gt;=2
    /// dispatch map — the MD identity can flow through an SZ array or constructed
    /// generic metadata without a statically visible MD token.</summary>
    internal void NoteMdArrayType(TypeDesc md)
    {
        if (ContainsCanonPlaceholder(md) || !MdArrayTypes.TryAdd(ArrayElemMangle(md), md))
            return;
        NoteMdArrayUse();
        var el = md.Element!;
        if (el.Kind is TypeKind.SZArray)
            NoteArrayElementType(el.Element!);
        else if (el.Kind is TypeKind.MDArray)
            NoteMdArrayType(el);
        else if (el is { Kind: TypeKind.Class, Class: { IsEnum: true } ec })
            NoteReferencedType(ec);
    }

    /// <summary>One SZArray interface the array's runtime dispatch map carries —
    /// the closed interface type-info <paramref name="Itf"/> (e.g. <c>IList&lt;E&gt;</c>,
    /// <c>IReadOnlyList&lt;E&gt;</c>, or the non-generic <c>System.Collections.IList</c>) and,
    /// per declared method, the interface declaration plus the
    /// <c>SZArrayEnumerable&lt;E&gt;</c> wrapper's concrete implementation the emitted thunk
    /// forwards to. The declaration's vtable slot indexes the entry's slot table; the impl's
    /// reachability gates whether that slot gets a thunk (else nullptr).</summary>
    internal sealed record ArrayItfDispatch(ClassInfo Itf, IReadOnlyList<(MethodInfo Decl, MethodInfo Impl)> Methods);

    /// <summary>The resolved support an array's runtime interface-dispatch map needs:
    /// the <c>SZArrayEnumerable&lt;E&gt;</c> wrapper class + its <c>(E[])</c>
    /// ctor (the array is wrapped fresh per dispatch) and the full CLR SZArray interface
    /// set — <c>IEnumerable&lt;E&gt;</c>/<c>ICollection&lt;E&gt;</c>/<c>IList&lt;E&gt;</c>/
    /// <c>IReadOnly{List,Collection}&lt;E&gt;</c> + the non-generic <c>IEnumerable</c>/
    /// <c>ICollection</c>/<c>IList</c> — each as a <see cref="ArrayItfDispatch"/> whose
    /// methods forward to the wrapper. CppEmitter turns each into a
    /// <c>Dn2CppInterfaceEntry</c> on <c>ti_arr_&lt;E&gt;</c>.</summary>
    internal sealed record ArrayEnumerableInfo(TypeDesc Elem, ClassInfo Szae, MethodInfo Ctor, IReadOnlyList<ArrayItfDispatch> Dispatches);

    /// <summary>Element types E (keyed by <see cref="ArrayElemMangle"/>) for which an
    /// array (<c>E[]</c>) should carry a real <c>IEnumerable&lt;E&gt;</c> interface-
    /// dispatch map, so a <em>runtime</em> cast <c>(IEnumerable&lt;E&gt;)obj</c> /
    /// <c>foreach</c> resolves on the array itself — not only the statically-known
    /// boundary the wrap covers. CppEmitter wires the map onto <c>ti_arr_&lt;E&gt;</c>
    /// for each E that is also a noted array type.</summary>
    internal Dictionary<string, ArrayEnumerableInfo> ArrayEnumerableElementTypes { get; } = new(System.StringComparer.Ordinal);

    /// <summary>The element types E requested as <c>IEnumerable&lt;E&gt;</c> at a runtime
    /// array cast. The exact element E gets a map; a covariant cast
    /// <c>(IEnumerable&lt;Base&gt;)derivedArr</c> also needs every <c>Derived[]</c> array
    /// (Derived reference-assignable to E) to carry its own <c>IEnumerable&lt;Derived&gt;</c>
    /// map, wired by <see cref="ExpandArrayEnumerableMaps"/> once the noted-array set is
    /// final.</summary>
    private readonly Dictionary<string, TypeDesc> _arrayEnumerableTargets = new(System.StringComparer.Ordinal);

    /// <summary>Notes that arrays are cast to / used as a closed SZArray collection
    /// interface over <paramref name="element"/> at runtime — a
    /// <c>castclass</c>/<c>isinst</c> to it, or a covariant store/call-arg/return where a
    /// raw <c>element[]</c> flows into such a parameter (the boundary, now served by
    /// the array's own map rather than a compile-time wrap). Records the target (for the
    /// covariant expansion), guarantees the precise <c>ti_arr_&lt;element&gt;</c>
    /// handle is emitted, and wires the exact <c>element[]</c> array's full SZArray
    /// interface map (reaching the <c>SZArrayEnumerable&lt;element&gt;</c> wrapper + the
    /// interfaces). No-op when the support assembly / CoreLib interfaces aren't loaded.</summary>
    public void NoteArrayEnumerableElement(TypeDesc element)
    {
        if (element.Kind is not (TypeKind.Primitive or TypeKind.Class or TypeKind.External or TypeKind.SZArray))
            return;
        // See NoteArrayElementType: placeholder-element arrays never exist.
        if (ContainsCanonPlaceholder(element))
            return;
        element = PromoteExternalArrayElement(element);
        // The map is attached to the precise per-element array type-info in EmitArrayTypeInfos;
        // ensure that handle is emitted (an array flowing in via store/call-arg may have no
        // newarr in this compilation to have noted it).
        NoteArrayElementType(element);
        _arrayEnumerableTargets.TryAdd(ArrayElemMangle(element), element);
        WireArrayEnumerableMap(element);
    }

    /// <summary>An array element that arrived as <see cref="TypeKind.External"/> but names a
    /// type we actually loaded, promoted to that <see cref="ClassInfo"/>.
    /// <para><see cref="MethodCompiler.ResolveTypeToken"/> deliberately degrades a
    /// cross-assembly REFERENCE type token to External — a caught exception type has to keep
    /// degrading to a catch-all rather than reference a per-type metadata symbol the
    /// runtime-trap exceptions never materialize. Harmless while the element stays opaque (an
    /// array of it stores Dn2CppObject* either way), but this is the one boundary where an
    /// element becomes a generic type ARGUMENT — <c>SZArrayEnumerable&lt;E&gt;</c> — and a
    /// specialization at an External E has members that cannot be typed: <c>get_Current</c>
    /// returns E, and <c>CppTypes.Of</c> can only type the handful of External names it maps
    /// by hand, so anything else fails there pointing at the wrapper rather than at the
    /// array.</para>
    /// <para>Promotion is gated on leaving <see cref="ArrayElemMangle"/> unchanged, which
    /// makes it non-perturbing: the mangle is the <c>ti_arr_&lt;E&gt;</c> suffix the newarr
    /// site, the collection set and the emitter all key on, so an element whose two spellings
    /// would name different symbols keeps its current behaviour. For a top-level type the
    /// two agree by construction.</para></summary>
    private TypeDesc PromoteExternalArrayElement(TypeDesc element)
    {
        if (element.Kind != TypeKind.External || element.ExternalName is not { } name)
            return element;
        if (FindClassByFullName(name) is not { } cls)
            return element;
        var promoted = TypeDesc.MakeClass(cls);
        return ArrayElemMangle(promoted) == ArrayElemMangle(element) ? promoted : element;
    }

    /// <summary>Wires the <c>IEnumerable&lt;element&gt;</c> dispatch map for the
    /// <c>element[]</c> array: resolves + reaches the wrapper/enumeration support
    /// and records it for CppEmitter. Idempotent. Without a CoreLib the enumeration
    /// interfaces don't exist and there is nothing to dispatch, so that case is a
    /// no-op; a missing *support shim*, by contrast, is a hard failure — the array
    /// would silently lose its interface map and abort on the first dispatch through
    /// it. Resolve the interfaces first so the no-CoreLib case never demands the
    /// shim.</summary>
    private void WireArrayEnumerableMap(TypeDesc element)
    {
        // Promote here, not only in NoteArrayEnumerableElement: ExpandArrayEnumerableMaps
        // (the covariant fan-out) wires straight from the noted-ARRAY set, whose elements
        // come from the newarr/token site and are therefore the degraded External spelling.
        // This is the one funnel into SZArrayEnumerableFor — the single place an element
        // becomes a generic type argument — so promoting here covers every caller.
        element = PromoteExternalArrayElement(element);
        if (ArrayEnumerableElementTypes.ContainsKey(ArrayElemMangle(element)))
            return;
        if (EnumerationDispatch(element) is not { } ed)
            return;
        var (szae, ctor) = SZArrayEnumerableFor(element);
        ArrayEnumerableElementTypes[ArrayElemMangle(element)] =
            new ArrayEnumerableInfo(element, szae, ctor, BuildArrayItfDispatches(element, szae));
        // Force-reach the IEnumerable<E> enumeration triple so an array used purely as a
        // statically-known IEnumerable<E> still emits its GetEnumerator/Current/MoveNext
        // (the wrapper is the enumerator); the other SZArray interfaces' methods are reached
        // on demand by the program's own callvirts (kept tight by tree-shaking).
        ReachEnumeration(ed);
        // Drain AFTER the triple reach, not only inside SZArrayEnumerableFor: marking
        // IEnumerable<E>.GetEnumerator used-virtual crosses with every allocated
        // implementer — for E = object the variant match reaches the explicit impl on
        // every allocated List<RefT> — and a method reached here without a drain would
        // be COMPILED by the emit fixpoint's next batch with its body never SCANNED
        // (with shared generics off no later round drains), so its callees would miss
        // the reachable set and AssertCalledBodiesEmitted would fail the transpile on
        // the forwarded-to symbol (found by build-and-run-emit-order-stability).
        DrainReachability();
    }

    /// <summary>Elements whose <c>E[]</c> carries no dispatch map, each mapped to the five
    /// generic collection interfaces (<c>IList&lt;E&gt;</c> &amp;c.) closed over it.
    /// CppEmitter attaches them to <c>ti_arr_&lt;E&gt;</c> as RELATION-only rows — nullptr
    /// slot tables, the same shape as the shared non-generic set — so
    /// Type.GetInterfaces() reports the eleven rows the type TEST already answers, while
    /// every dispatch reader skips them (<c>slots != nullptr</c>) and the loud dispatch
    /// abort stays.</summary>
    internal Dictionary<string, IReadOnlyList<ClassInfo>> ArrayGenericItfRowElements { get; } =
        new(System.StringComparer.Ordinal);

    /// <summary>Resolves the five generic collection interfaces closed over
    /// <paramref name="element"/> and notes their type-infos for emission — the
    /// relation-only sibling of <see cref="WireArrayEnumerableMap"/> for the elements
    /// that loop must skip. Idempotent; no-op without a CoreLib (the interfaces do
    /// not exist and the type test has nothing to contradict).</summary>
    private bool WireArrayGenericItfRows(TypeDesc element)
    {
        string key = ArrayElemMangle(element);
        if (ArrayGenericItfRowElements.ContainsKey(key))
            return false;
        var idx = TypeIndex();
        var itfs = new List<ClassInfo>(5);
        foreach (var name in new[]
            { "IList`1", "ICollection`1", "IEnumerable`1", "IReadOnlyList`1", "IReadOnlyCollection`1" })
        {
            if (!idx.TryGetValue(("System.Collections.Generic", name), out var cands))
                return false;
            var itf = Instantiate(cands[0].Item1, cands[0].Item2, new[] { element });
            EnsureCompleted(itf);
            NoteReferencedType(itf);
            itfs.Add(itf);
        }
        ArrayGenericItfRowElements[key] = itfs;
        return true;
    }

    /// <summary>Plants those rows on every noted element whose array still has no dispatch
    /// map, once the noted set is final. The eager loop above runs at NOTING time and so
    /// cannot see an element first noted AFTER the emit fixpoint — the reflection tables'
    /// member-type pre-note (<c>TypeMetadataEmitter.NoteReflectedMemberArrayElements</c>) is
    /// exactly that, and its arrays enumerated six interfaces where .NET reports eleven.
    /// This sweep is the confirmation point, so the answer stops depending on WHEN
    /// an element was noted; a dispatch map cannot be wired here (its thunks need bodies the
    /// fixpoint has already compiled), which is the right answer anyway — such an array is
    /// merely NAMED, and the loud dispatch abort stays.
    /// <para>Returns the interfaces this call minted. Nothing else names them by now, so the
    /// caller owes each a type-info; the emit set closed before this ran.</para></summary>
    internal List<ClassInfo> PlantUnmappedArrayGenericItfRows()
    {
        var minted = new List<ClassInfo>();
        // Snapshot: Instantiate/EnsureCompleted below decode, and a decode appends to
        // Classes — never to ArrayElementTypes, but the walk must not depend on that.
        foreach (var key in ArrayElementTypes.Keys.OrderBy(k => k, System.StringComparer.Ordinal).ToList())
        {
            if (ArrayEnumerableElementTypes.ContainsKey(key) || ArrayGenericItfRowElements.ContainsKey(key))
                continue;
            if (WireArrayGenericItfRows(ArrayElementTypes[key]))
                minted.AddRange(ArrayGenericItfRowElements[key]);
        }
        return minted;
    }

    /// <summary>The CLR SZArray interface set an array's runtime dispatch map carries:
    /// the wrapper's implemented generic collection interfaces closed over the
    /// array's exact element + the non-generic <c>IEnumerable</c>/<c>ICollection</c>/
    /// <c>IList</c>. Resolved off the (already instantiated/completed)
    /// <c>SZArrayEnumerable&lt;element&gt;</c> wrapper's interface list so each
    /// <c>Itf</c>/method is the same Instantiate-deduped instance the wrapper's impls and
    /// the program's callvirts resolve against. <c>IEnumerator&lt;E&gt;</c>/<c>IDisposable</c>
    /// (the wrapper is itself an enumerator) are excluded — an array is not those.</summary>
    private IReadOnlyList<ArrayItfDispatch> BuildArrayItfDispatches(TypeDesc element, ClassInfo szae)
    {
        var result = new List<ArrayItfDispatch>();
        foreach (var itf in szae.Interfaces)
        {
            if (!IsSZArrayMapInterface(itf, element))
                continue;
            var methods = new List<(MethodInfo, MethodInfo)>();
            itf.EnsureMembers();   // the interface's declarations ARE the table's rows
            foreach (var decl in itf.Methods)
                if (ResolveItfImplOrNull(szae, decl) is { } impl)
                    methods.Add((decl, impl));
            if (methods.Count == 0)
                continue;
            result.Add(new ArrayItfDispatch(itf, methods));
            // The closed interface type-info must be emitted for the entry's `&ti` to link.
            NoteReferencedType(itf);
        }
        return result;
    }

    /// <summary>Whether <paramref name="itf"/> is one of the interfaces an SZArray of
    /// <paramref name="element"/> implements in the CLR: a non-generic
    /// <c>IEnumerable</c>/<c>ICollection</c>/<c>IList</c>, or a generic collection
    /// interface closed over the array's exact element type.</summary>
    private bool IsSZArrayMapInterface(ClassInfo itf, TypeDesc element)
    {
        if (itf.FullName is "System.Collections.IEnumerable"
            or "System.Collections.ICollection"
            or "System.Collections.IList")
            return true;
        return (GenericDefFullName(itf) is "System.Collections.Generic.IEnumerable"
                or "System.Collections.Generic.ICollection"
                or "System.Collections.Generic.IReadOnlyCollection"
                or "System.Collections.Generic.IList"
                or "System.Collections.Generic.IReadOnlyList")
            && itf.Context.TypeArgs is [{ } arg]
            && ArrayElemMangle(arg) == ArrayElemMangle(element);
    }

    /// <summary>String's runtime interface-dispatch map: the CoreLib
    /// <c>System.String</c> ClassInfo plus one <see cref="ArrayItfDispatch"/> per
    /// interface String implements in the CLR (<c>IEnumerable&lt;char&gt;</c>/
    /// <c>IEnumerable</c>/<c>IComparable</c>/<c>IComparable&lt;string&gt;</c>/
    /// <c>IEquatable&lt;string&gt;</c>/<c>ICloneable</c>/<c>IConvertible</c>), each
    /// method pairing the interface declaration with String's transpiled CoreLib
    /// implementation. CppEmitter turns the dispatches into
    /// <c>Dn2CppInterfaceEntry</c> rows that the generated init prologue installs
    /// onto the runtime-owned <c>dn2cpp_string_type</c>
    /// (<c>dn2cpp_string_set_interfaces</c>) — strings have no per-class emitted
    /// type-info to carry them.</summary>
    internal sealed record StringInterfaceInfo(ClassInfo StringClass, IReadOnlyList<ArrayItfDispatch> Dispatches);

    internal StringInterfaceInfo? StringInterfaces { get; private set; }

    /// <summary>Notes that a string is used as one of its CLR interfaces at runtime —
    /// a <c>castclass</c>/<c>isinst</c> to such an interface, a string flowing into an
    /// interface-typed parameter/receiver/slot (LINQ over a string emits no cast: the
    /// conversion is a call-boundary coercion), or an <c>IEnumerable&lt;char&gt;</c>
    /// enumeration anywhere (a string-backed <c>ReadOnlyMemory&lt;char&gt;</c> can
    /// surface the string itself as the enumerable). Wires the whole map once:
    /// entry presence is what makes <c>is</c>/<c>castclass</c> succeed, and slots fill
    /// per-method by reachability like every other interface table. Reaches the
    /// <c>char</c> enumeration triple eagerly (the map's reason to exist) and joins
    /// String into the used-virtual cross product so later dispatches reach its
    /// impls on demand. No-op without a CoreLib.</summary>
    public void NoteStringInterfaces()
    {
        if (StringInterfaces is not null)
            return;
        if (!TypeIndex().TryGetValue(("System", "String"), out var cands)
            || !cands[0].Item1.ClassMap.TryGetValue(cands[0].Item2, out var strCls))
            return;
        var dispatches = new List<ArrayItfDispatch>();
        foreach (var itf in strCls.Interfaces)
        {
            if (!IsStringDispatchInterface(itf))
                continue;
            var methods = new List<(MethodInfo, MethodInfo)>();
            itf.EnsureMembers();   // ditto
            foreach (var decl in itf.Methods)
                if (!decl.IsStatic && decl.VtableSlot >= 0
                    && ResolveItfImplOrNull(strCls, decl) is { IsStatic: false } impl)
                    methods.Add((decl, impl));
            if (methods.Count == 0)
                continue;
            dispatches.Add(new ArrayItfDispatch(itf, methods));
            // The closed interface type-info must be emitted for the entry's `&ti` to link.
            NoteReferencedType(itf);
        }
        StringInterfaces = new StringInterfaceInfo(strCls, dispatches);
        // The enumeration triple over char: GetEnumerator/get_Current/MoveNext are
        // what every consumer of a string-as-IEnumerable<char> dispatches first.
        if (EnumerationDispatch(TypeDesc.MakePrimitive(PrimitiveTypeCode.Char)) is { } ed)
            ReachEnumeration(ed);
        // Join String into the used-virtual cross product (interface slots only —
        // String is never in _allocatedRefTypes: its ToString/GetHashCode/Equals go
        // through runtime intrinsics, and eager allocation side effects don't apply).
        foreach (var decl in _usedVirtualDecls.ToList())
            ReachStringVirtualImpl(decl);
        // Wiring happens during body compilation; drain now so the emit fixpoint
        // picks up the transitive closure of the just-reached String impls.
        DrainReachability();
    }

    /// <summary>The boxed-enum runtime interface-dispatch map: the CoreLib
    /// <c>System.Enum</c> ClassInfo plus one <see cref="ArrayItfDispatch"/> per
    /// interface System.Enum implements in the CLR (<c>IComparable</c>/
    /// <c>IFormattable</c>/<c>IConvertible</c>/<c>ISpanFormattable</c>), each method
    /// pairing the interface declaration with System.Enum's own implementation.
    /// CppEmitter turns the dispatches into <c>Dn2CppInterfaceEntry</c> rows the
    /// generated init prologue installs onto the runtime-owned
    /// <c>dn2cpp_enum_type</c> (<c>dn2cpp_enum_set_interfaces</c>). ONE map serves
    /// every enum: a boxed enum's own per-enum type-info deliberately carries no
    /// rows — its <c>base</c> IS <c>dn2cpp_enum_type</c> and
    /// <c>dn2cpp_resolve_interface_walk</c> consults the base chain — so the map
    /// costs a program constant, not per-enum, bytes. The impls take the box
    /// pointer directly (System.Enum is modeled as a reference type), so
    /// no unboxing thunks are involved.</summary>
    internal sealed record EnumInterfaceInfo(ClassInfo EnumClass, IReadOnlyList<ArrayItfDispatch> Dispatches);

    internal EnumInterfaceInfo? EnumInterfaces { get; private set; }

    /// <summary>The four non-generic interfaces every enum implements via System.Enum
    /// — the same set the runtime's <c>dn2cpp_wellknown_itf_mask</c> TF_ENUM arm
    /// already answers the type TEST for. The type test is map-independent; this set
    /// gates wiring the DISPATCH map (<see cref="NoteEnumInterfaces"/>).</summary>
    internal static bool IsEnumDispatchInterface(ClassInfo itf) =>
        itf.FullName is "System.IComparable" or "System.IFormattable"
            or "System.IConvertible" or "System.ISpanFormattable";

    /// <summary>Notes that a boxed enum may be dispatched through one of System.Enum's
    /// CLR interfaces at runtime — an enum or System.Enum-typed value flowing into such
    /// an interface-typed position (the <c>((IConvertible)e).ToInt32(null)</c> receiver
    /// coercion; IL needs no castclass, every enum statically implements them), or a
    /// runtime <c>castclass</c>/<c>isinst</c> to such an interface (an enum can hide
    /// behind <c>object</c> — Comparer&lt;object&gt;.Default's <c>(IComparable)x</c>).
    /// Wires the whole map once, mirroring <see cref="NoteStringInterfaces"/>: slots
    /// fill per-method by reachability (the used-virtual cross product), so a program
    /// that only ever casts pays only for what it dispatches. No-op without a
    /// CoreLib.</summary>
    public void NoteEnumInterfaces()
    {
        if (EnumInterfaces is not null)
            return;
        if (!TypeIndex().TryGetValue(("System", "Enum"), out var ecands)
            || !ecands[0].Item1.ClassMap.TryGetValue(ecands[0].Item2, out var enCls))
            return;
        var dispatches = new List<ArrayItfDispatch>();
        foreach (var itf in enCls.Interfaces)
        {
            if (!IsEnumDispatchInterface(itf))
                continue;
            var methods = new List<(MethodInfo, MethodInfo)>();
            itf.EnsureMembers();
            foreach (var decl in itf.Methods)
                if (!decl.IsStatic && decl.VtableSlot >= 0
                    && ResolveItfImplOrNull(enCls, decl) is { IsStatic: false } impl)
                    methods.Add((decl, impl));
            if (methods.Count == 0)
                continue;
            dispatches.Add(new ArrayItfDispatch(itf, methods));
            // The closed interface type-info must be emitted for the entry's `&ti` to link.
            NoteReferencedType(itf);
        }
        EnumInterfaces = new EnumInterfaceInfo(enCls, dispatches);
        // Join System.Enum into the used-virtual cross product (interface slots only —
        // System.Enum is never in _allocatedRefTypes: nothing news it, boxes are
        // minted under the concrete enum's own ti_). Later dispatches reach its impls
        // on demand via the ReachUsedVirtual hook.
        foreach (var decl in _usedVirtualDecls.ToList())
            ReachEnumVirtualImpl(decl);
        // Wiring happens during body compilation; drain now so the emit fixpoint
        // picks up the transitive closure of the just-reached System.Enum impls.
        DrainReachability();
    }

    /// <summary>Reaches System.Enum's implementation of a dispatched interface slot —
    /// the System.Enum half of the used-virtual × allocated-type cross product
    /// (mirror of <see cref="ReachStringVirtualImpl"/>). The format/value family
    /// (ToString/GetTypeCode/CompareTo/Equals/GetHashCode/GetValue) is BodyReplace'd
    /// (BrEnumInstanceFormat) inside <see cref="Reach"/>; the IConvertible.To* family
    /// keeps its real IL (Convert.ToXxx(GetValue(), …)), which is transpilable once
    /// GetValue no longer names the cut FCall.</summary>
    private void ReachEnumVirtualImpl(MethodInfo decl)
    {
        if (EnumInterfaces is not { } ei || !decl.DeclaringClass.IsInterface)
            return;
        if (!ImplementsInterface(ei.EnumClass, decl.DeclaringClass))
            return;
        if (ResolveItfImplOrNull(ei.EnumClass, decl) is not { IsStatic: false } impl)
            return;
        // System.Enum.System.ISpanFormattable.TryFormat is cut (CvEnumTryFormat —
        // its real IL reaches GetEnumInfo -> GetEnumValuesAndNames, an InternalCall
        // with no IL). Its map slot degrades to the named dispatch trap, exactly as
        // the same row's cut in ReachVirtualImpl leaves a per-class slot.
        if (CoreIntrinsics.CvEnumTryFormat.Matches(impl.DeclaringClass.FullName, impl.Name))
            return;
        Reach(impl);
    }

    /// <summary>One row of the intrinsic-type interface-dispatch table: an intrinsic type
    /// that models a CLR type implementing
    /// <paramref name="ItfNamespace"/>.<paramref name="ItfName"/>,
    /// the runtime-owned <c>Dn2CppTypeInfo</c> the generated init prologue installs the
    /// map onto, and the C++ body of the thunk that fills the interface method's slot.
    ///
    /// <para>An intrinsic type carries no per-class emitted type-info, so nothing would
    /// otherwise give it an interface map at all and every interface-mouth dispatch —
    /// <c>using (…)</c>, which lowers to <c>callvirt IDisposable::Dispose</c>, an
    /// interface-typed local, an <c>isinst</c>/<c>castclass</c> — hits
    /// <c>dn2cpp_resolve_interface</c>'s "has no map" abort, while the *direct*
    /// <c>Dispose()</c> call goes through the intrinsic call-site route and works. That
    /// divergence between the two mouths is what makes the hole invisible until a
    /// <c>using</c> shows up.</para>
    ///
    /// <para>The thunk kind fixes both managed and C++ signatures. Row resolution validates
    /// the managed slot at mint time and records a mismatch
    /// (<see cref="IntrinsicInterfaceMismatch"/>); the emitter fails the transpile when a
    /// mismatched row's interface is in the emit set, instead of producing a wasm
    /// function-pointer signature trap.</para></summary>
    internal enum IntrinsicInterfaceThunkKind
    {
        TimerDispose,
        NoopDispose,
        TimerChange,
        TimerDisposeAsync,
    }

    internal sealed record IntrinsicInterfaceRow(
        string IntrinsicName, string TypeInfoSym, string ThunkSym,
        string ItfNamespace, string ItfName, string SlotMethod, int SlotParameterCount,
        IntrinsicInterfaceThunkKind ThunkKind);

    /// <summary>The intrinsic types given a runtime interface map, in emit order.
    /// A type may own multiple rows, and they must stay ADJACENT here: the emitter turns
    /// each run of one TypeInfoSym into a single map, and installing a map REPLACES the
    /// type-info's interface list, so a second run would drop the first one's interfaces.
    /// A split run fails the emit rather than silently dropping
    /// (<c>CppEmitter.EmitIntrinsicInterfaceMaps</c>).</summary>
    internal static readonly IntrinsicInterfaceRow[] IntrinsicInterfaceRows =
    [
        new("System.Threading.Timer", "dn2cpp_timer_type", "itfthunk_timer_dispose",
            "System", "IDisposable", "Dispose", 0, IntrinsicInterfaceThunkKind.TimerDispose),
        new("System.Threading.Timer", "dn2cpp_timer_type", "itfthunk_timer_change",
            "System.Threading", "ITimer", "Change", 2, IntrinsicInterfaceThunkKind.TimerChange),
        new("System.Threading.Timer", "dn2cpp_timer_type", "itfthunk_timer_dispose_async",
            "System", "IAsyncDisposable", "DisposeAsync", 0,
            IntrinsicInterfaceThunkKind.TimerDisposeAsync),
        new("System.TimeProvider+SystemTimeProviderTimer", "dn2cpp_timeprovider_timer_type",
            "itfthunk_timeprovider_timer_dispose", "System", "IDisposable", "Dispose", 0,
            IntrinsicInterfaceThunkKind.TimerDispose),
        new("System.TimeProvider+SystemTimeProviderTimer", "dn2cpp_timeprovider_timer_type",
            "itfthunk_timeprovider_timer_change", "System.Threading", "ITimer", "Change", 2,
            IntrinsicInterfaceThunkKind.TimerChange),
        new("System.TimeProvider+SystemTimeProviderTimer", "dn2cpp_timeprovider_timer_type",
            "itfthunk_timeprovider_timer_dispose_async", "System", "IAsyncDisposable", "DisposeAsync", 0,
            IntrinsicInterfaceThunkKind.TimerDisposeAsync),
        new("System.Threading.CountdownEvent", "dn2cpp_countdown_type", "itfthunk_countdown_dispose",
            "System", "IDisposable", "Dispose", 0, IntrinsicInterfaceThunkKind.NoopDispose),
        new("System.Threading.Barrier", "dn2cpp_barrier_type", "itfthunk_barrier_dispose",
            "System", "IDisposable", "Dispose", 0, IntrinsicInterfaceThunkKind.NoopDispose),
        new("System.Threading.ReaderWriterLockSlim", "dn2cpp_rwlock_type", "itfthunk_rwlock_dispose",
            "System", "IDisposable", "Dispose", 0, IntrinsicInterfaceThunkKind.NoopDispose),
        new("System.Threading.ThreadLocal`1", "dn2cpp_threadlocal_type", "itfthunk_threadlocal_dispose",
            "System", "IDisposable", "Dispose", 0, IntrinsicInterfaceThunkKind.NoopDispose),
        new("System.Collections.Concurrent.BlockingCollection`1", "dn2cpp_blockingcollection_type",
            "itfthunk_blockingcoll_dispose", "System", "IDisposable", "Dispose", 0,
            IntrinsicInterfaceThunkKind.NoopDispose),
    ];

    /// <summary>A row whose intrinsic is instantiated by the program and whose interface
    /// resolved against the loaded CoreLib: the row, the interface's ClassInfo and the
    /// declaration whose vtable slot the thunk fills. CppEmitter turns each into one
    /// <c>Dn2CppInterfaceEntry</c> row installed by the init prologue.</summary>
    internal sealed record IntrinsicInterfaceInfo(IntrinsicInterfaceRow Row, ClassInfo Itf, MethodInfo SlotDecl);

    /// <summary>A row whose interface resolved but whose declared slot did not match the
    /// row: the interface and the diagnostic. Recorded rather than thrown, because the
    /// mint point cannot tell whether it matters — the row is rendered only when the
    /// interface's type-info is in the emit set, and a program that mints the intrinsic
    /// without ever touching the interface installs nothing and must not abort.</summary>
    internal sealed record IntrinsicInterfaceMismatch(ClassInfo Itf, string Message);

    private readonly IntrinsicInterfaceInfo?[] _intrinsicItfs = new IntrinsicInterfaceInfo?[IntrinsicInterfaceRows.Length];
    private readonly bool[] _intrinsicItfNoted = new bool[IntrinsicInterfaceRows.Length];
    private readonly List<IntrinsicInterfaceMismatch> _intrinsicItfMismatches = [];

    /// <summary>The rows that failed shape validation, for the emitter to raise on if it
    /// reaches one whose interface is emitted.</summary>
    internal IReadOnlyList<IntrinsicInterfaceMismatch> IntrinsicInterfaceMismatches => _intrinsicItfMismatches;

    /// <summary>The resolved rows, in table order — a stable emit order independent of
    /// the order the program happened to mint the intrinsics in.</summary>
    internal IEnumerable<IntrinsicInterfaceInfo> IntrinsicInterfaces
    {
        get
        {
            foreach (var info in _intrinsicItfs)
                if (info is not null)
                    yield return info;
        }
    }

    /// <summary>Notes that the intrinsic named by <paramref name="intrinsicName"/> is
    /// minted somewhere — called from that type's newobj intercept, which is its only
    /// mint point — wiring its interface-dispatch info once. The emitter renders the row
    /// only when the program ALSO emits the interface's type-info (a <c>using</c>-lowered
    /// call site or an isinst/castclass notes it): a program that mints the intrinsic but
    /// never touches the interface installs nothing, and no dispatch can miss the absent
    /// row. No-op without a CoreLib, or when <paramref name="intrinsicName"/> names no
    /// row — the table is the whole contract, so an unlisted caller is a no-op rather
    /// than an error. A row whose slot the loaded framework spells differently leaves the
    /// row uninstalled and records an <see cref="IntrinsicInterfaceMismatch"/> for the
    /// emitter to raise on.</summary>
    public void NoteIntrinsicInterfaces(string intrinsicName)
    {
        for (int i = 0; i < IntrinsicInterfaceRows.Length; i++)
        {
            var row = IntrinsicInterfaceRows[i];
            if (!string.Equals(row.IntrinsicName, intrinsicName, StringComparison.Ordinal))
                continue;
            if (_intrinsicItfNoted[i])
                continue;
            _intrinsicItfNoted[i] = true;
            if (!TypeIndex().TryGetValue((row.ItfNamespace, row.ItfName), out var cands)
                || !cands[0].Item1.ClassMap.TryGetValue(cands[0].Item2, out var itf))
                continue;
            itf.EnsureMembers();
            var decl = itf.Methods.FirstOrDefault(m => !m.IsStatic && m.Name == row.SlotMethod
                && m.Signature.ParameterTypes.Length == row.SlotParameterCount);
            if (decl is null)
            {
                _intrinsicItfMismatches.Add(new(itf,
                    $"intrinsic interface row {row.IntrinsicName} -> {row.ItfNamespace}.{row.ItfName}." +
                    $"{row.SlotMethod} could not resolve its declared slot shape"));
                continue;
            }
            bool shapeMatches = row.ThunkKind switch
            {
                IntrinsicInterfaceThunkKind.TimerDispose or IntrinsicInterfaceThunkKind.NoopDispose => decl.Signature.ReturnType.IsVoid
                    && decl.Signature.ParameterTypes.Length == 0,
                IntrinsicInterfaceThunkKind.TimerChange =>
                    decl.Signature.ReturnType is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Boolean }
                    && decl.Signature.ParameterTypes is
                        [{ Kind: TypeKind.Class, Class.FullName: "System.TimeSpan" },
                         { Kind: TypeKind.Class, Class.FullName: "System.TimeSpan" }],
                IntrinsicInterfaceThunkKind.TimerDisposeAsync =>
                    decl.Signature.ReturnType is
                        { Kind: TypeKind.Class, Class.FullName: "System.Threading.Tasks.ValueTask" }
                    && decl.Signature.ParameterTypes.Length == 0,
                _ => false,
            };
            if (!shapeMatches)
            {
                _intrinsicItfMismatches.Add(new(itf,
                    $"intrinsic interface row {row.IntrinsicName} -> {row.ItfNamespace}.{row.ItfName}." +
                    $"{row.SlotMethod} does not match thunk kind {row.ThunkKind}"));
                continue;
            }
            _intrinsicItfs[i] = new IntrinsicInterfaceInfo(row, itf, decl);
        }
    }

    /// <summary>Resolves and reaches String's own CoreLib instance method
    /// <paramref name="name"/> (the overload picked by <paramref name="match"/> over
    /// its parameter types) so an intrinsic call site can delegate to the real
    /// transpiled body — String is an intrinsic-mapped type, so its methods are
    /// never reached through normal call edges. Null without a CoreLib or when
    /// String has no such method.</summary>
    internal MethodInfo? ReachStringMethod(string name, System.Func<TypeDesc[], bool> match)
    {
        NoteStringInterfaces();
        if (StringInterfaces is not { } si)
            return null;
        var m = si.StringClass.Methods.FirstOrDefault(m => !m.IsStatic && m.Rva != 0
            && m.Name == name && match(m.Signature.ParameterTypes.ToArray()));
        if (m is null)
            return null;
        ReachIntrinsicTypeMethod(m);
        DrainReachability();
        return m;
    }

    /// <summary>The static-method variant of <see cref="ReachStringMethod"/>:
    /// String's internal static helpers (MakeSeparatorList/MakeSeparatorListAny —
    /// the separator scanners behind MemoryExtensions.Split) delegate to their
    /// real transpiled bodies the same way.</summary>
    internal MethodInfo? ReachStringStaticMethod(string name, System.Func<TypeDesc[], bool> match)
    {
        NoteStringInterfaces();
        if (StringInterfaces is not { } si)
            return null;
        var m = si.StringClass.Methods.FirstOrDefault(m => m.IsStatic && m.Rva != 0
            && m.Name == name && match(m.Signature.ParameterTypes.ToArray()));
        if (m is null)
            return null;
        ReachIntrinsicTypeMethod(m);
        DrainReachability();
        return m;
    }

    /// <summary>Resolves and reaches an ordinary (non-intrinsic-mapped) loaded
    /// class's instance method or ctor so an intrinsic call site can allocate the
    /// object and delegate to the real transpiled body — the bridge from a
    /// handle-modeled receiver into plain managed BCL IL (Assembly.GetName() ->
    /// <c>new AssemblyName(displayName)</c>). <paramref name="allocates"/> applies
    /// the newobj side effects a scanned allocation site would have (type-info
    /// emission, the used-virtual cross product, ToString/GetHashCode/Equals
    /// wiring). Null when the type is not loaded (no CoreLib reference) or has no
    /// matching method with a body.</summary>
    internal MethodInfo? ReachManagedMethod(string typeFullName, string name,
        System.Func<TypeDesc[], bool> match, bool allocates = false)
    {
        if (FindClassByFullName(typeFullName) is not { } cls)
            return null;
        return ReachManagedMethod(cls, name, match, allocates);
    }

    /// <summary>The <see cref="ClassInfo"/>-keyed form of
    /// <see cref="ReachManagedMethod(string,string,System.Func{TypeDesc[],bool},bool)"/>,
    /// for a callee an intrinsic already holds a resolved class for.
    ///
    /// <para>It exists because the by-NAME form cannot reach a closed generic. A
    /// specialization's <see cref="ClassInfo.FullName"/> is the MANGLED instantiation name
    /// that <see cref="Instantiate"/> builds
    /// (<c>System.Collections.ObjectModel.ReadOnlyCollection_System_Exception</c>), so
    /// <see cref="FindClassByFullName"/> would have to be handed a string no source spells
    /// — and would answer only for an instantiation something else had already minted. A
    /// lowering that wants <c>ReadOnlyCollection&lt;Exception&gt;</c> instead reads it off
    /// its own callee signature, where the decode has already closed it
    /// (<c>SignatureProvider</c> calls <see cref="Instantiate"/> for a generic
    /// instantiation), and hands the ClassInfo here.</para></summary>
    internal MethodInfo? ReachManagedMethod(ClassInfo cls, string name,
        System.Func<TypeDesc[], bool> match, bool allocates = false)
    {
        var m = cls.EnsureMembers().Methods.FirstOrDefault(m => !m.IsStatic && m.Rva != 0
            && m.Name == name && match(m.Signature.ParameterTypes.ToArray()));
        if (m is null)
            return null;
        if (allocates)
        {
            NoteReferencedType(cls);
            ReachAllocatedType(cls);
        }
        Reach(m);
        // Mandatory, not cosmetic: a method reached here without a drain would be COMPILED
        // by the emit fixpoint's next batch with its body never SCANNED, so its callees
        // would miss the reachable set and AssertCalledBodiesEmitted would fail the
        // transpile on a forwarded-to symbol. (Same note at WireArrayEnumerableMap.)
        DrainReachability();
        return m;
    }

    /// <summary>Reaches String's implementation of a dispatched interface slot,
    /// exempting it from the intrinsic-type cut (the slot table needs the real
    /// transpiled function — there is no call site to intercept). The String half
    /// of the used-virtual × allocated-type cross product.</summary>
    private void ReachStringVirtualImpl(MethodInfo decl)
    {
        if (StringInterfaces is not { } si || !decl.DeclaringClass.IsInterface)
            return;
        if (!ImplementsInterface(si.StringClass, decl.DeclaringClass))
            return;
        if (ResolveItfImplOrNull(si.StringClass, decl) is { IsStatic: false } impl)
            ReachIntrinsicTypeMethod(impl);
    }

    /// <summary>Reaches an intrinsic-mapped type's method whose REAL body must be
    /// transpiled (String's interface impls / an intrinsic call site delegating to
    /// the real body): exempts it from the intrinsic-type cut in <see cref="Reach"/>
    /// and, if a prior ldftn had claimed it as a synthesized wrapper, upgrades it —
    /// the real body serves the ftn use too, but the wrapper cannot serve an
    /// interface slot.</summary>
    private void ReachIntrinsicTypeMethod(MethodInfo m)
    {
        _intrinsicTypeTranspiled.Add(m);
        if (IntrinsicFtnTargets.Remove(m))
            Reachable.Remove(m); // was added scan-less; re-add through Reach so the body is scanned
        Reach(m);
    }

    /// <summary>ldftn/delegate targets on intrinsic-mapped types (e.g.
    /// <c>s.Count(char.IsDigit)</c> — a method group over <c>Char.IsDigit</c>).
    /// Their call sites are always intercepted, so the real body never exists —
    /// but taking the address needs a function. CppEmitter synthesizes each body
    /// from the call intrinsic's own lowering
    /// (<see cref="MethodCompiler.CompileCoreIntrinsicWrapper"/>); the method joins
    /// <see cref="Reachable"/> scan-less (its real IL is never followed).</summary>
    internal HashSet<MethodInfo> IntrinsicFtnTargets { get; } = new();

    public void NoteIntrinsicFtnTarget(MethodInfo m)
    {
        if (_intrinsicTypeTranspiled.Contains(m))
            return; // real body transpiled — the symbol already exists
        if (IntrinsicFtnTargets.Add(m))
            Reachable.Add(m.EnsureSignature()); // reached => decoded, as in Reach
    }

    /// <summary>ldftn/delegate targets whose call sites an intercept descriptor row CUT
    /// (<see cref="CoreIntrinsics.TryFindCutRow"/>) — a method group over
    /// <c>ExecutionContext.Run</c> and its kind. Reachability deleted the edge to the real
    /// body, but the delegate adapter (or the raw address) NAMES the method's own symbol,
    /// so cut ⟹ route holds only if the body exists: CppEmitter synthesizes it by
    /// replaying that row's own emit arm
    /// (<see cref="MethodCompiler.CompileInterceptWrapper"/>), which is the same lowering
    /// a direct call gets. The method joins <see cref="Reachable"/> scan-less — following
    /// its IL is exactly what the cut forbids — the same protocol as
    /// <see cref="IntrinsicFtnTargets"/>, whose rows are the intrinsic-mapped TYPES this
    /// one's are not.</summary>
    internal HashSet<MethodInfo> InterceptFtnTargets { get; } = new();

    public void NoteInterceptFtnTarget(MethodInfo m)
    {
        if (InterceptFtnTargets.Add(m))
            Reachable.Add(m.EnsureSignature()); // reached => decoded, as in Reach
    }

    /// <summary>Whether <paramref name="itf"/> is one of the interfaces String
    /// implements in the CLR and dispatches through its runtime map. The static-only
    /// parse interfaces (<c>IParsable</c>/<c>ISpanParsable</c>) are excluded — they
    /// have no instance slots to dispatch.</summary>
    internal bool IsStringDispatchInterface(ClassInfo itf)
    {
        if (itf.FullName is "System.Collections.IEnumerable" or "System.ICloneable"
            or "System.IComparable" or "System.IConvertible")
            return true;
        return GenericDefFullName(itf) switch
        {
            "System.Collections.Generic.IEnumerable" =>
                itf.Context.TypeArgs is [{ Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Char }],
            "System.IComparable" or "System.IEquatable" =>
                itf.Context.TypeArgs is [{ } a] && a.IsString,
            _ => false,
        };
    }

    /// <summary>Covariant array→<c>IEnumerable</c> expansion: for every noted array
    /// element A whose <c>A[]</c> is reference-assignable to a requested
    /// <c>IEnumerable&lt;E&gt;</c> target (A == E, or A derives from / implements E), wire
    /// <c>A</c>'s own <c>IEnumerable&lt;A&gt;</c> map. The runtime then matches the array's
    /// <c>IEnumerable&lt;A&gt;</c> covariantly against the requested <c>IEnumerable&lt;E&gt;</c>
    /// (<c>dn2cpp_itf_variant_match</c>). Also wires every noted VALUE element's own map
    /// eagerly (see the loop below). Driven inside the emit fixpoint (the noted-array
    /// and target sets both grow during body compilation); returns whether a new map was
    /// wired, so the caller keeps draining until stable.</summary>
    public bool ExpandArrayEnumerableMaps()
    {
        bool added = WireObjectArrayFallbackMap();
        // The multi-dimensional sibling: one shared six-interface map for every
        // rank>=2 array, keyed on any MD array being noted at all.
        added |= WireMdArrayItfMap();
        // The ENUMERATION counterpart of both: the same six non-generic
        // interfaces as relation-only rows, so Type.GetInterfaces() over an array
        // reports them. Keyed on any array type-info existing at all.
        added |= WireArrayNongenericItfRows();
        // The gendef sibling: force-emit the argument-free ancestry every bare
        // typeof(D<>) handle's relation rows will point at.
        added |= WireTypeofOpenGenericDefAncestry();
        // Eager VALUE-element wiring: a value-element array
        // reached through an `object`-typed variable has no statically visible
        // cast/boundary to key the lazy per-element map on — the reference-element
        // fallback table cannot serve it either (value layouts differ per element, so
        // the object-keyed thunks would read garbage) — so every noted value element's
        // rank-1 array gets its own map up front. Deliberately NOT
        // NoteArrayEnumerableElement: the target set drives the covariant expansion
        // below, and a value element is invariant — registering it as a target would
        // change which reference-element maps get wired for nothing. Intrinsic value
        // elements are skipped — an intrinsic type's ti_ is never emitted, so a map
        // could not attach to it; boxing one is already a separate existing limit, and
        // its dispatch keeps the loud abort. Both intrinsic tests, per AGENTS.md: the
        // name table misses a closed generic intrinsic (Vector128<T>), which only
        // IntrinsicCppName catches. A STRUCT element must additionally have a default
        // equality the one builder pair can answer (the same chain as
        // MethodCompiler.TryEqualityEqualsLValue's struct arm): the wrapper's
        // IndexOf/Contains lower EqualityComparer<T>.Default.Equals, and wiring the map
        // allocates the wrapper, so the used-virtual crossing reaches those bodies
        // whenever the program calls IList<T>.IndexOf anywhere — for an element with no
        // equality (ConditionalWeakTable<K,V>.Entry, noted by CoreLib itself) that
        // turned every real-CoreLib transpile into the loud NotSupported at the
        // wrapper's body. Such elements keep the loud dispatch abort, as before. The
        // walk is over ArrayElementTypes (snapshotted — WireArrayEnumerableMap notes
        // new elements), not Classes, so the member/field-type decodes the equality
        // tests pull are sound here (the wire itself decodes far more).
        foreach (var v in ArrayElementTypes.Values.ToList())
        {
            if (!IsValueElement(v))
                continue;
            // The two skipped shapes get RELATION-only rows instead: the
            // type TEST answers the five generic collection interfaces by flag, so
            // enumeration must not answer six where .NET says eleven. Their
            // dispatch keeps the loud abort — the rows carry nullptr slots.
            if (v.Kind == TypeKind.Class
                && (CoreIntrinsics.IsIntrinsicType(v.Class!.FullName)
                    || v.Class.IntrinsicCppName is not null))
            {
                added |= WireArrayGenericItfRows(v);
                continue;
            }
            if (v is { Kind: TypeKind.Class, Class: { IsValueType: true, IsEnum: false } vc }
                && EffectiveTypedEquals(vc) is null && EffectiveEquals(vc) is null
                && SynthesizedValueEquals(vc) is null)
            {
                added |= WireArrayGenericItfRows(v);
                continue;
            }
            if (ArrayEnumerableElementTypes.ContainsKey(ArrayElemMangle(v)))
                continue;
            WireArrayEnumerableMap(v);
            added |= ArrayEnumerableElementTypes.ContainsKey(ArrayElemMangle(v));
        }
        // MethodBase.Invoke / PropertyInfo.GetValue on an interface-declared row is a
        // dispatch mouth with no statically visible call site: dn2cpp_invoke_mi's
        // interface arm resolves the RECEIVER's slot exactly as a callvirt would, so a
        // program that uses reflection invoke can land on any wired array map's rows.
        // Mirror that mouth here — reachability must be at least as generous as
        // dispatch: reach every wired map's impls, so their slots hold thunks
        // rather than trap stubs. Costs nothing in a program that never invokes;
        // driven each round because the flag and the map set both grow as bodies
        // compile. Snapshot the walk — Reach decodes, and a decode can note elements.
        if (_reflectionInvokeUsed)
        {
            bool reached = false;
            foreach (var aei in ArrayEnumerableElementTypes.Values.ToList())
                foreach (var d in aei.Dispatches)
                    foreach (var (_, impl) in d.Methods)
                        if (!Reachable.Contains(impl))
                        {
                            Reach(impl);
                            reached = true;
                        }
            // The MD map's rows are a dispatch mouth for the same reflection-invoke
            // arm, so they get the same generosity.
            if (MdArrayInterfaces is { } mai)
                foreach (var d in mai.Dispatches)
                    foreach (var (_, impl) in d.Methods)
                        if (!Reachable.Contains(impl))
                        {
                            Reach(impl);
                            reached = true;
                        }
            if (reached)
                DrainReachability();
        }
        if (_arrayEnumerableTargets.Count == 0)
            return added;
        foreach (var a in ArrayElementTypes.Values.ToList())
        {
            if (ArrayEnumerableElementTypes.ContainsKey(ArrayElemMangle(a)))
                continue;
            foreach (var e in _arrayEnumerableTargets.Values)
                if (IsReferenceAssignableTo(a, e))
                {
                    WireArrayEnumerableMap(a);
                    added |= ArrayEnumerableElementTypes.ContainsKey(ArrayElemMangle(a));
                    break;
                }
        }
        return added;
    }

    /// <summary>Wires the <c>object</c>-element SZArray map once any REFERENCE-element
    /// array is noted — the shared fallback dispatch table. A collection-
    /// interface call on an array reached through an <c>object</c>-typed variable (or on
    /// a runtime-materialized array) has no statically visible cast/boundary to key the
    /// lazy per-element map on, so the emitter registers this one map with the runtime
    /// (<c>dn2cpp_array_set_ref_fallback_interfaces</c>, see EmitInitCalls) and
    /// <c>dn2cpp_resolve_interface_walk</c> serves any reference-element rank-1 array
    /// from it — reference elements share one C++ layout, which is what makes the
    /// object-keyed thunks sound for all of them. Gated on a reference element being
    /// noted at all: a program whose only arrays are value-element cannot be served by
    /// the fallback anyway (value elements get their own per-element maps eagerly —
    /// the value loop in <see cref="ExpandArrayEnumerableMaps"/> — so they never need
    /// it), and a program with no arrays cannot dispatch on one. Driven each fixpoint round
    /// (the noted-array set grows as bodies compile); a pure function of the noted set,
    /// never of the environment.</summary>
    private bool WireObjectArrayFallbackMap()
    {
        var obj = TypeDesc.MakePrimitive(PrimitiveTypeCode.Object);
        string objKey = ArrayElemMangle(obj);
        if (ArrayEnumerableElementTypes.ContainsKey(objKey))
            return false;
        bool anyRef = false;
        foreach (var a in ArrayElementTypes.Values)
            if (!IsValueElement(a))
            {
                anyRef = true;
                break;
            }
        if (!anyRef)
            return false;
        // Deliberately NOT NoteArrayEnumerableElement: that would register `object` as a
        // covariant expansion target, and every reference element is assignable to it —
        // the expansion in ExpandArrayEnumerableMaps would then wire a per-element map
        // (a wrapper instantiation + thunks) for EVERY noted reference element, when the
        // fallback table is the whole point of not needing those. (Value elements are the
        // mirror image: each gets its own eager map, and the target set stays untouched
        // there too.) Note the element ti and wire the one map.
        NoteArrayElementType(obj);
        WireArrayEnumerableMap(obj);
        return ArrayEnumerableElementTypes.ContainsKey(objKey);
    }

    /// <summary>The multi-dimensional array runtime dispatch support: the
    /// non-generic <c>Dn2Cpp.Runtime.MDArrayEnumerable</c> wrapper, its
    /// <c>(System.Array)</c> ctor, and one <see cref="ArrayItfDispatch"/> per interface
    /// a CLR rank&gt;=2 array implements — exactly the six non-generic ones
    /// (<see cref="IsMdArrayMapInterface"/>). CppEmitter turns the dispatches into ONE
    /// shared <c>Dn2CppInterfaceEntry</c> table the generated init prologue registers
    /// with the runtime (<c>dn2cpp_array_set_md_fallback_interfaces</c>, see
    /// EmitInitCalls): MD type-infos are always runtime-interned with no interface
    /// rows of their own, so — unlike the per-element SZ maps — there is no type-info
    /// to attach rows to, and <c>dn2cpp_resolve_interface_walk</c> serves every
    /// rank&gt;=2 array from this table. Element-agnostic soundness: the wrapper
    /// reads/writes through the System.Array reflection surface, whose runtime helpers
    /// dispatch on the receiver's own type-info.</summary>
    internal sealed record MdArrayItfInfo(ClassInfo Wrapper, MethodInfo Ctor, IReadOnlyList<ArrayItfDispatch> Dispatches);

    internal MdArrayItfInfo? MdArrayInterfaces { get; private set; }

    private bool _mdArrayUsed;

    /// <summary>Notes that the program can materialize or see a multi-dimensional
    /// (rank&gt;=2) array: a <c>new T[,]</c> lowering, an MD accessor member
    /// (<c>Get</c>/<c>Set</c>/<c>Address</c>), an MD type token
    /// (typeof/castclass/isinst), or a fixed-rank&gt;1 <c>Array.CreateInstance</c>.
    /// Keys wiring the shared MD dispatch map (<see cref="WireMdArrayItfMap"/>, driven
    /// inside the emit fixpoint); a program that never notes wires and emits nothing,
    /// so its output stays byte-identical.</summary>
    public void NoteMdArrayUse()
    {
        _mdArrayUsed = true;
        // An MD array's interned type-info carries no rows of its own, so the shared
        // relation-only set is the ONLY thing GetInterfaces() can report for it.
        NoteArrayItfRowsNeeded();
    }

    /// <summary>The six non-generic interfaces a CLR multi-dimensional array
    /// implements — no generic collection interface is among them — the same set
    /// whose type-infos carry <c>DN2CPP_TF_ARRAY_ITF</c> for the
    /// type-TEST half (CppEmitter.TypeMetadataEmitter). This set gates the DISPATCH
    /// map's rows; the wrapper's <c>IEnumerator</c> is deliberately outside it — the
    /// cursor's dispatch is ordinary class dispatch, and an array is not an
    /// enumerator.</summary>
    internal static bool IsMdArrayMapInterface(ClassInfo itf) =>
        itf.FullName is "System.Collections.IEnumerable"
            or "System.Collections.ICollection"
            or "System.Collections.IList"
            or "System.ICloneable"
            or "System.Collections.IStructuralComparable"
            or "System.Collections.IStructuralEquatable";

    /// <summary>Wires the shared multi-dimensional array dispatch map once any MD
    /// array is noted (<see cref="NoteMdArrayUse"/>): resolves + reaches the
    /// non-generic <c>MDArrayEnumerable</c> wrapper and records the six-interface
    /// dispatch set for CppEmitter. Mirrors <see cref="WireObjectArrayFallbackMap"/> —
    /// driven each fixpoint round, a pure function of the noted flag. Without a
    /// CoreLib the collection interfaces don't exist and nothing can dispatch, so
    /// that case is a no-op (resolved FIRST, so it never demands the shim); a missing
    /// *support shim* is a hard failure — the map would silently not exist and the
    /// first MD interface dispatch would abort at run time.</summary>
    private bool WireMdArrayItfMap()
    {
        if (!_mdArrayUsed || MdArrayInterfaces is not null)
            return false;
        // Both halves of the no-CoreLib guard: without the non-generic collection
        // interfaces nothing can dispatch, and without a LOADED System.Array the
        // wrapper's (System.Array) ctor is inexpressible (its parameter degrades to
        // External and could never match) — either way the MD dispatch keeps the
        // loud abort it has today, and neither case may demand the shim.
        if (!TypeIndex().TryGetValue(("System.Collections", "IEnumerable"), out _)
            || !TypeIndex().TryGetValue(("System", "Array"), out _))
            return false;
        const string Need = "dispatching a collection interface on a multi-dimensional array";
        var (mod, tdh) = RequireShimType("Dn2Cpp.Runtime", "MDArrayEnumerable", Need)[0];
        if (!mod.ClassMap.TryGetValue(tdh, out var cls))
            throw new NotSupportedException(
                "Dn2Cpp.Runtime.MDArrayEnumerable could not be resolved from the loaded "
                + "Dn2Cpp.Runtime.dll (stale or mismatched shim). It is needed for " + Need + ".");
        var ctor = cls.EnsureMembers().Methods.FirstOrDefault(
            m => m.Name == ".ctor"
                && m.Signature.ParameterTypes is [{ Kind: TypeKind.Class } p]
                && p.Class!.FullName == "System.Array");
        if (ctor is null)
            throw new NotSupportedException(
                "Dn2Cpp.Runtime.MDArrayEnumerable has no (System.Array) constructor: the loaded "
                + "Dn2Cpp.Runtime.dll is stale or does not match this dn2cpp. It is needed for "
                + Need + ".");
        NoteReferencedType(cls);
        // ReachAllocatedType joins the wrapper into the used-virtual cross product, so
        // only the interface members the program actually dispatches get bodies — the
        // rest degrade to named trap stubs in the emitted table.
        ReachAllocatedType(cls);
        Reach(ctor);
        var dispatches = new List<ArrayItfDispatch>();
        foreach (var itf in cls.Interfaces)
        {
            if (!IsMdArrayMapInterface(itf))
                continue;
            var methods = new List<(MethodInfo, MethodInfo)>();
            itf.EnsureMembers();   // the interface's declarations ARE the table's rows
            foreach (var decl in itf.Methods)
                if (!decl.IsStatic && decl.VtableSlot >= 0
                    && ResolveItfImplOrNull(cls, decl) is { IsStatic: false } impl)
                    methods.Add((decl, impl));
            if (methods.Count == 0)
                continue;
            dispatches.Add(new ArrayItfDispatch(itf, methods));
            // The closed interface type-info must be emitted for the entry's `&ti` to link.
            NoteReferencedType(itf);
        }
        MdArrayInterfaces = new MdArrayItfInfo(cls, ctor, dispatches);
        // Force-reach the non-generic enumeration triple (GetEnumerator +
        // get_Current/MoveNext) so a bare (IEnumerable)md + foreach enumerates even
        // when the program's own callvirts sat in bodies compiled before this wiring —
        // the same eager triple WireArrayEnumerableMap reaches for IEnumerable<E>.
        ReachNonGenericArrayEnumerator();
        if (TypeIndex().TryGetValue(("System.Collections", "IEnumerator"), out var ecands)
            && ecands[0].Item1.ClassMap.TryGetValue(ecands[0].Item2, out var enumItf))
        {
            enumItf.EnsureMembers();
            foreach (var decl in enumItf.Methods)
                if (!decl.IsStatic && decl.VtableSlot >= 0
                    && decl.Name is "MoveNext" or "get_Current")
                    ReachUsedVirtual(decl);
            NoteReferencedType(enumItf);
        }
        // Wiring happens during body compilation; drain now so the emit fixpoint
        // picks up the transitive closure of the just-reached wrapper members.
        DrainReachability();
        return true;
    }

    /// <summary>The six non-generic array interfaces as CLR types, in
    /// <see cref="IsMdArrayMapInterface"/> order, or null when the load set does not carry
    /// all six. Rendered by <c>CppEmitter.EmitArrayNongenericItfRows</c> as ONE
    /// relation-only (nullptr-slot) row table and installed by the init prologue
    /// (<c>dn2cpp_array_set_nongeneric_interfaces</c>) — see that declaration in
    /// <c>runtime/core/dn2cpp_core.h</c> for why enumeration needs a table the two
    /// dispatch maps cannot provide.</summary>
    internal IReadOnlyList<ClassInfo>? ArrayNongenericInterfaces { get; private set; }

    private bool _arrayItfRowsNeeded;

    /// <summary>Notes that the program can hold an ARRAY type-info at all — every noted
    /// SZArray element type and every noted MD array — which is the trigger for the
    /// relation-only row set above. Deliberately wider than the dispatch maps' triggers:
    /// a dispatch map is needed only where a call goes through an interface, whereas
    /// <c>typeof(T[]).GetInterfaces()</c> asks about an array the program merely
    /// NAMES.</summary>
    private void NoteArrayItfRowsNeeded() => _arrayItfRowsNeeded = true;

    /// <summary>Resolves the six non-generic array interfaces and forces their type-infos
    /// to be emitted, once any array type-info is noted. Driven each fixpoint round like
    /// its two dispatch siblings, and a pure function of the noted flag.
    ///
    /// <para>The force-note is the load-bearing half and is not optional: nothing reaches
    /// <c>IStructuralComparable</c>/<c>IStructuralEquatable</c> on its own — they arrive
    /// incidentally, as rows on <c>ValueTuple</c> or in String's dispatch set — so a table
    /// naming them would fail to LINK. Forcing all six also makes the answer a fact about
    /// arrays rather than about what the program happened to reach, which is what lets a
    /// gate diff it against real .NET.</para>
    ///
    /// <para>A load set missing any of the six wires nothing and every reader keeps
    /// today's answer — the same no-CoreLib posture <see cref="WireMdArrayItfMap"/>
    /// takes, and for the same reason: there is no shim to demand here, so a partial
    /// table would be a worse answer than none.</para></summary>
    private bool WireArrayNongenericItfRows()
    {
        if (!_arrayItfRowsNeeded || ArrayNongenericInterfaces is not null)
            return false;
        var names = new (string Ns, string Name)[]
        {
            ("System.Collections", "IEnumerable"),
            ("System.Collections", "ICollection"),
            ("System.Collections", "IList"),
            ("System", "ICloneable"),
            ("System.Collections", "IStructuralComparable"),
            ("System.Collections", "IStructuralEquatable"),
        };
        var itfs = new List<ClassInfo>(names.Length);
        foreach (var key in names)
        {
            if (!TypeIndex().TryGetValue(key, out var cands)
                || !cands[0].Item1.ClassMap.TryGetValue(cands[0].Item2, out var cls)
                || !cls.IsInterface)
                return false;
            itfs.Add(cls);
        }
        foreach (var itf in itfs)
            NoteReferencedType(itf);
        ArrayNongenericInterfaces = itfs;
        return true;
    }

    private readonly HashSet<string> _gendefAncestryWired = new(System.StringComparer.Ordinal);

    /// <summary>Force-emits the type-infos a bare <c>typeof(D&lt;&gt;)</c> handle's
    /// substitution-invariant relation rows will name (<see cref="OpenGenericDefAncestry"/>):
    /// its nearest invariant ancestor and its invariant interface closure. Only these
    /// definitions need forcing — one with an emitted closed instantiation already drags its
    /// own base and interfaces into the emit set, and the shell filters its rows by the same
    /// definedness test the closed type's table uses.
    ///
    /// <para>Without it, a definition the program only ever names — <c>class Box&lt;T&gt; :
    /// IMarker</c> with no close — has no <c>ti_IMarker</c> to point at, and
    /// <c>typeof(IMarker).IsAssignableFrom(typeof(Box&lt;&gt;))</c> answers False where .NET
    /// answers True. This pass is also the one place a CLOSED invariant entry
    /// (<c>class Fixed&lt;T&gt; : ClosedBase&lt;int&gt;</c>) is MINTED, not merely looked
    /// up: the reflected-over definition observably needs that type-info, and nothing else
    /// in a typeof-only program would instantiate it. Driven in the emit fixpoint like its
    /// array siblings, since <see cref="TypeofOpenGenericDefs"/> grows as bodies
    /// compile.</para></summary>
    private bool WireTypeofOpenGenericDefAncestry()
    {
        int before = ReferencedTypes.Count;
        foreach (string defName in TypeofOpenGenericDefs.Keys
                     .OrderBy(k => k, System.StringComparer.Ordinal).ToList())
        {
            if (!_gendefAncestryWired.Add(defName))
                continue;
            var (bas, itfs) = OpenGenericDefAncestryByName(defName, materialize: true);
            if (bas is not null)
                NoteReferencedType(bas);
            foreach (var itf in itfs)
                NoteReferencedType(itf);
        }
        return ReferencedTypes.Count != before;
    }

    /// <summary>Whether <paramref name="cls"/> is the <c>SZArrayEnumerable&lt;object&gt;</c>
    /// wrapper instantiation — the class the shared reference-element fallback table's
    /// thunks wrap arrays into. Its type-info is stamped
    /// <c>DN2CPP_TF_REF_ERASED_ITF</c> (CppEmitter.TypeMetadataEmitter) so the runtime
    /// may service any all-reference-argument instantiation of its closed generic
    /// interfaces from its object-keyed rows (the enumerator it returns is dispatched at
    /// the caller's element typing, e.g. <c>IEnumerator&lt;Attribute&gt;</c>).</summary>
    internal bool IsRefErasedItfWrapper(ClassInfo cls) =>
        ArrayEnumerableElementTypes.TryGetValue(
            ArrayElemMangle(TypeDesc.MakePrimitive(PrimitiveTypeCode.Object)), out var aei)
        && ReferenceEquals(aei.Szae, cls);

    /// <summary>Whether an instance of element type <paramref name="a"/> is reference-
    /// assignable to <paramref name="b"/> — exact, every reference type to object,
    /// or a class/interface that <paramref name="a"/> derives from / implements. Value-type
    /// elements (primitives, structs, enums) are invariant: assignable only when exact.</summary>
    private bool IsReferenceAssignableTo(TypeDesc a, TypeDesc b)
    {
        if (ArrayElemMangle(a) == ArrayElemMangle(b))
            return true;
        if (IsValueElement(a) || IsValueElement(b))
            return false;
        if (b.IsObject)
            return true;
        return a.Kind == TypeKind.Class && b.Kind == TypeKind.Class
            && (DerivesFromOrIs(a.Class!, b.Class!) || ImplementsInterface(a.Class!, b.Class!));
    }

    private static bool IsValueElement(TypeDesc t) =>
        (t.Kind == TypeKind.Primitive && t.Primitive is not (PrimitiveTypeCode.String or PrimitiveTypeCode.Object))
        || (t.Kind == TypeKind.Class && (t.Class!.IsValueType || t.Class.IsEnum));

    private readonly HashSet<MethodInfo> _scanned = new();

    // Set when a reached body calls MethodInfo/MethodBase.Invoke. Triggers the
    // reflection-invoke reachability route after the initial discovery drain.
    private bool _reflectionInvokeUsed;

    // Set when a reached body calls ConstructorInfo.Invoke or the non-generic
    // Activator.CreateInstance(Type). Triggers the reflection-ctor route.
    private bool _reflectionCtorUsed;

    // One arm of the runtime-instantiation trigger: a reached body calls
    // Type.MakeGenericType through the MemberRef mouth (a user-module call
    // site; a CoreLib-internal MethodDef call is deliberately not a trigger —
    // the templates serve program code that mints closed types by hand).
    private bool _makeGenericTypeUsed;

    // The other arm: the CLR backtick full names of every OPEN generic
    // definition a reached body ldtokens (typeof(D<>)), in first-seen order.
    // Recorded unconditionally, not behind _makeGenericTypeUsed — the flag can
    // be set by a body scanned after the ldtoken site, and gating the record on
    // it would make the set scan-order-dependent (the _appMethodSpecTypeArgs
    // argument). Consumed once, after the scan closes, by the
    // runtime-instantiation template pass.
    private readonly List<string> _typeofOpenGenericDefNames = new();
    private readonly HashSet<string> _typeofOpenGenericDefSeen = new(StringComparer.Ordinal);

    // The generic-METHOD half of the reflection-ctor surface: the type arguments
    // of every MethodSpec an app-module body instantiates, recorded by
    // ScanBodyForGenerics off the resolved target's Context (already-closed
    // TypeDescs — no decode). `serializer.Deserialize<Dictionary<string,
    // FileLoadedAchievement>>(reader)` names its construction target ONLY here:
    // it is no field or property type, so the declared-member surface alone
    // cannot see it. Recorded unconditionally (not behind _reflectionCtorUsed —
    // the flag can be set by a body scanned AFTER the call site, so gating the
    // record on it would make the surface scan-order-dependent); consumed by
    // ReachUserSurfaceNamedSpecializationCtors. Duplicates are fine (the
    // consumer dedupes); the count is part of the consumer's fixpoint measure.
    private readonly List<TypeDesc> _appMethodSpecTypeArgs = new();

    // The reflection-ctor route's LIBRARY-target state, persistent across the
    // fixpoint rounds (consumed and grown by
    // ReachUserSurfaceNamedSpecializationCtors; their combined count is part of
    // the caller's fixpoint measure, so a round that opens a new target is
    // always followed by another round that collects that target's members):
    //  - _userReflConstructed: library classes the surface NAMED and this route
    //    opened as construction targets (all instance ctors + allocation);
    //  - _userReflSurface: those plus their user-module base chains — the
    //    classes whose declared member types feed the next round's surface and
    //    whose property accessors are reached (a deserializer populates an
    //    inherited property through the BASE's accessor body).
    private readonly HashSet<ClassInfo> _userReflConstructed = new();
    private readonly HashSet<ClassInfo> _userReflSurface = new();

    // Set when a reached body calls GetCustomAttributes/IsDefined. Triggers the
    // reflection-attribute route: reach each app-module attribute's ctor + named-property
    // setters and allocate the attribute types so their instances can be materialized.
    private bool _reflectionAttrUsed;

    // The full names of FRAMEWORK-module classes a reachable USER-module body names
    // with a type token (`ldtoken <type>` — typeof). Recorded by ScanBodyForGenerics
    // off the already-resolved token (no extra decode; recording every named class
    // rather than testing attribute-hood keeps the record decode-free — a base-chain
    // walk would read shapes the scan otherwise never asks for). Consumed by
    // DecodeCustomAttributes as the keep-set widening for framework-DECLARED attribute
    // types: `GetMember(...)[0].GetCustomAttributes(typeof(DescriptionAttribute),
    // false)` names the attribute type it filters by, and that naming is the one
    // statically visible evidence that the program reflects over a BCL attribute
    // (Thrive's EnumHelper over [Description] enum fields). Non-attribute members of
    // the set are inert — only an attribute ctor's declaring class is ever tested
    // against it. Recorded unconditionally (not behind _reflectionAttrUsed — the flag
    // can be set by a body scanned AFTER the token, so gating would make the set
    // scan-order-dependent); duplicates are free (a set); the count is part of the
    // reflection-attribute walk's fixpoint measure below.
    private readonly HashSet<string> _userTypeofNamedFrameworkTypes = new();

    // The reflection-invoke route's LIBRARY extension: user-LIBRARY classes a
    // reachable body names with a type token (`ldtoken <type>` — typeof), in
    // first-named order (list + set: the consumer's reach order is emit-relevant, and
    // a HashSet walk is not deterministic). The token is the one statically visible
    // evidence that the program can reflect over the class (the same keep rule
    // --trim-reflection uses): Google.Protobuf's descriptor machinery hands
    // typeof(Value) to GeneratedClrTypeInfo, then GetMethod/Invoke's accessor members
    // nothing calls statically. Recorded unconditionally (not behind
    // _reflectionInvokeUsed — the flag can be set by a body scanned AFTER the token,
    // so gating would make the set scan-order-dependent); consumed by the
    // typeof-named-library loop beside the app-module invoke route, whose fixpoint
    // measure is the list's count.
    private readonly List<ClassInfo> _typeofNamedLibraryClasses = new();
    private readonly HashSet<ClassInfo> _typeofNamedLibraryClassesSeen = new();

    /// <summary>Whether a framework-declared attribute class is reflectable because a
    /// reachable user-module body named it with typeof — the bounded widening of
    /// <see cref="DecodeCustomAttributes"/>'s non-framework keep rule.</summary>
    internal bool IsUserTypeofNamedFrameworkType(string fullName) =>
        _userTypeofNamedFrameworkTypes.Contains(fullName);

    /// <summary>Whether any emitted body lowered a manifest-resource read
    /// (<c>Assembly.GetManifestResourceStream</c> / <c>GetManifestResourceNames</c> /
    /// <c>GetManifestResourceInfo</c>) — the condition under which
    /// <see cref="CppEmitter.EmitAssemblyRegistry"/> carries every loaded module's
    /// embedded resource blobs into the image.
    ///
    /// <para>This is an EMIT-time mark, not a scan-time one, and deliberately so: the
    /// registry is emitted after every body has been compiled, so the exact set of
    /// programs that can observe a resource is already known — no over-approximation,
    /// and a program that reads none stays byte-identical (the registry rows keep the
    /// trailing 0-fill of the two new <c>Dn2CppAssemblyRegEntry</c> members). It is
    /// sticky across the shared-generics planning pass, which is the conservative
    /// direction: a canonical body that trial-compiles a resource read and is later
    /// dropped costs blobs nothing names, never a missing one.</para>
    ///
    /// <para>The blobs are carried for EVERY loaded module, framework ones included.
    /// Narrowing to the app module SILENTLY would be a lie: a missing resource and a
    /// dropped one are indistinguishable at <c>GetManifestResourceStream</c>, which
    /// answers both with null. Narrowing LOUDLY is <c>--no-manifest-resources</c>: the
    /// dropped assembly's registry row keeps a <c>resourcesDropped</c> bit, and a run-time
    /// miss throws naming the assembly and the remedy instead of answering that null (see
    /// <see cref="DropsManifestResources"/>).</para>
    ///
    /// <para>It stays ONE bit rather than a per-assembly keep-set: every reader
    /// spills to a temporary (<c>IEvalStack.Push</c>), so no site's receiver assembly
    /// is a constant and such an analysis would answer "all" anyway. See
    /// <c>docs/EDITOR-EXPORT-DESIGN.md</c> §10.</para></summary>
    internal bool ManifestResourcesUsed { get; private set; }

    /// <summary>Records that an emitted body reads manifest resources — see
    /// <see cref="ManifestResourcesUsed"/>.</summary>
    internal void NoteManifestResourceUse() => ManifestResourcesUsed = true;

    private void CompleteAndDiscover()
    {
        _toScan = new Queue<MethodInfo>();

        SeedPreservedMembers();

        // Seed with the program roots. Only the app module is rooted; reference
        // assemblies (e.g. a real CoreLib passed with -r) are pulled in solely
        // through reachability edges, so tree-shaking can keep all but the
        // handful of BCL methods the program actually uses.
        if (EntryPoint is not null)
            Reach(EntryPoint);
        foreach (var cls in Classes.ToList())
        {
            // Undecoded specializations contribute nothing, and asking would change that: this
            // runs before the first drain, so reading .Methods here would decode the class and
            // newly root every public method of every public app generic — a reachability
            // change, and an order-dependent one, since a specialization first instantiated
            // while compiling a body would never pass through here at all. Whatever IS decoded
            // by now (a generic base of a non-generic class, via BuildVtables) is still
            // seeded.
            if (cls.Module != AppModule || !cls.MembersReady)
                continue;
            foreach (var m in cls.Methods)
            {
                if (m.Rva == 0)
                    continue;
                bool isRoot = m.Name == ".cctor"
                    || (cls.IsPublic && (m.IsPublic || m.Name == ".ctor"));
                if (isRoot)
                    Reach(m);
            }
        }

        // A module initializer is a side effect with NO call edge — that is the entire
        // point of the feature — so a call-graph tree-shaker can never discover it. The
        // `<Module>` .cctor the C# compiler emits to call it is therefore a root, in every
        // loaded NON-framework module: the app module (where the loop above would already
        // have rooted it, .cctor being an unconditional app-module root — this loop is
        // idempotent and states the intent) and every user library pulled in with -r, whose
        // .cctors are otherwise not roots at all (ReachCctor pulls a reference assembly's
        // .cctor in on ALLOCATION of its type, and `<Module>` is never allocated, so a
        // library's initializer would silently never run).
        //
        // .NET runs a module initializer lazily, before the first access to anything in
        // that module; dn2cpp runs every reached .cctor eagerly at startup, so a referenced
        // library's initializer runs even when the program ends up touching nothing else in
        // it. That over-approximates .NET, and deliberately: the alternative is to under-run
        // it, and a registration hook that does not run is exactly the silent wrong answer
        // this exists to prevent. It matches what the AOT peers do (IL2CPP runs module
        // initializers for the assemblies it links).
        //
        // Framework assemblies are exempt, on the same bound as the [UnmanagedCallersOnly]
        // roots below: BCL module-level plumbing is the runtime's job, not transpiled IL's.
        // The carve-out is free today — every framework assembly's `<Module>` row is empty,
        // so Pass 1 does not even model it — and it is what keeps a future BCL initializer
        // from dragging a native-startup subtree into every program.
        foreach (var cls in Classes.ToList())
        {
            if (cls.Name != "<Module>" || IsFrameworkAssemblyName(cls.Module.AssemblyName)
                || !cls.MembersReady)
                continue;
            foreach (var m in cls.Methods)
                if (m.Name == ".cctor" && m.IsStatic && m.Rva != 0)
                    Reach(m);
        }

        // [UnmanagedCallersOnly] methods are native entry points: native code takes
        // their address (an exported symbol, or ldftn flowing out through another
        // native callback) with no managed call edge, so they are roots in every
        // loaded module — even private methods of internal types, and in reference
        // modules pulled in with -r. Framework assemblies are exempt: the BCL's own
        // UnmanagedCallersOnly methods are CoreCLR-host / PAL callbacks (ComActivator,
        // EventPipe, ConsolePal's terminal signal handler, …) wired up by native
        // runtime code dn2cpp does not implement, and rooting them drags
        // untranspilable subtrees into every program that references the assembly.
        foreach (var cls in Classes.ToList())
        {
            if (IsFrameworkAssemblyName(cls.Module.AssemblyName) || !cls.MembersReady)
                continue;   // as above: before the first drain, and an [UnmanagedCallersOnly]
                            // method cannot live on a closed generic in the first place
            // A backend that skips a method's body owns its native entry itself
            // (e.g. the mono-module backend emits godotsharp_game_main_init in its
            // epilogue), so a skipped method is neither rooted nor exported here —
            // rooting it would compile the skipped body and collide on the symbol.
            foreach (var m in cls.Methods)
                if (m.IsUnmanagedCallersOnly && m.Rva != 0
                    && _backend?.ShouldSkipMethodBody(cls, m) is not true)
                    Reach(m);
        }

        // Seed reachability for classes a backend instantiates outside the C# call
        // graph. The value-type/abstract guard is a defensive core-side invariant
        // (ReachAllocatedType assumes a concrete reference type): current backends
        // already filter these out, but Compilation must not trust the backend to.
        if (_backend is not null)
        {
            // Engine-wrapper allowlist trim (--trim-godot-classes): install the
            // backend's registry-cctor nomination BEFORE anything can be scanned —
            // the deferral is consulted while the cctor's body is scanned, and the
            // first drain below is what starts scanning.
            if (_backend.GodotClassTrim(this) is { } trimSpec)
                InstallGodotClassTrim(trimSpec);

            // Materialize both. A backend is free to hand these back as a lazy walk over
            // Classes — GodotBackend does — and reaching is what appends to Classes: it
            // resolves, resolving decodes, and a decode instantiates the generics the
            // signature names. Consuming the walk while the consumer grows the list it
            // walks is a "collection was modified" waiting for the first program that has
            // an exported class whose roots name a generic nothing else did.
            foreach (var cls in _backend.ExternallyAllocatedClasses(this).ToList())
                if (!cls.IsValueType && !cls.IsAbstract)
                    ReachAllocatedType(cls);

            // Seed methods a backend's emitted epilogue calls directly (outside the
            // C# call graph) as roots — same contract as the externally-allocated
            // classes above.
            foreach (var m in _backend.AdditionalRootMethods(this).ToList())
                Reach(m);
        }

        // Hot-update base build: force-emit the extra closed generic
        // instantiations a patch may bind against (hotupdate-refs.txt roots).
        if (_genericRoots is { Count: > 0 })
            foreach (var root in _genericRoots)
                SeedGenericRoot(root);

        DrainReachability();
        // Reflection-invoke reachability route: if the program calls
        // MethodInfo.Invoke, make every app-module (non-ctor) method body invokable by
        // reaching it — its invoker thunk + arg/return box/unbox then emit. Bounded to
        // the app module so a real CoreLib pulled in with -r is not force-reached (which
        // would drag untranspilable BCL bodies into the tree). A reflection-only method
        // of a non-app type stays stripped (IL2CPP-with-managed-stripping semantics).
        //
        // A library-DECLARED type is opened by the surface walk below instead, and only
        // when the user code's declared surface NAMES it (a generic-method type argument,
        // a member type of an already-opened target). Extending THIS loop to every user
        // module is not a cost question but a soundness one: allocating every concrete
        // class of every -r library turns on the used-slot × allocated-type cross product
        // for classes nothing constructs, which drags untranspilable BCL subtrees (a JSON
        // DataSet converter reaching System.Data → XmlSerializer) into the transpile for
        // code no program input can ever select.
        if (_reflectionInvokeUsed || _reflectionCtorUsed)
        {
            foreach (var cls in Classes.ToList())
            {
                if (cls.Module != AppModule || cls.IsInterface)
                    continue;
                // Unlike the seeding loops above, this one runs after the discovery drain:
                // reflection genuinely can invoke a closed generic's methods, so ask for
                // the members here (EnsureMembers).
                foreach (var m in cls.EnsureMembers().Methods)
                {
                    if (m.Rva == 0 || m.Name == ".cctor")
                        continue;
                    // A body the backend replaces wholesale (e.g. the source-generated
                    // GodotPlugins.Game.Main bootstrap the .NET-module backend emits in
                    // C++) is never emitted, so it cannot be reflection-invoked either —
                    // force-reaching it would only drag its dead callees into the tree.
                    if (_backend?.ShouldSkipMethodBody(cls, m) == true)
                        continue;
                    bool isCtor = m.Name == ".ctor";
                    // Reach non-ctor method bodies for MethodInfo.Invoke; reach ctor
                    // bodies (and allocate the type so its vtable/type-info emit) for
                    // ConstructorInfo.Invoke / Activator.CreateInstance(Type).
                    if (isCtor ? _reflectionCtorUsed : _reflectionInvokeUsed)
                        Reach(m);
                }
                if (_reflectionCtorUsed && !cls.IsValueType && !cls.IsAbstract)
                    ReachAllocatedType(cls);
            }
            // The app module is not the whole ctor surface a late-bound
            // Type.GetConstructor can name: a JSON deserializer constructs the
            // closed BCL generic a user field/property is DECLARED at
            // (Dictionary<string, AppType>), which lives in the framework module —
            // one a user body passes as a generic-method type argument
            // (serializer.Deserialize<Dictionary<string, AppType>>(reader)) — or a
            // type DECLARED in a referenced user library (Thrive's GameWiki).
            // Alternate with the drain to a fixpoint: the drain scans the
            // bodies the force-reach loop above just queued, and a scan can
            // record new user MethodSpec type args — and an opened library
            // target's member types widen the surface for the next round.
            if (_reflectionCtorUsed)
            {
                int seenSpecArgs = -1, seenOpened = -1;
                while (seenSpecArgs != _appMethodSpecTypeArgs.Count
                    || seenOpened != _userReflSurface.Count + _userReflConstructed.Count
                        + _typeofNamedLibraryClasses.Count)
                {
                    seenSpecArgs = _appMethodSpecTypeArgs.Count;
                    seenOpened = _userReflSurface.Count + _userReflConstructed.Count
                        + _typeofNamedLibraryClasses.Count;
                    ReachUserSurfaceNamedSpecializationCtors();
                    DrainReachability();
                }
            }
            else
                DrainReachability();
        }

        // Reflection-invoke route, LIBRARY extension: a user-library class a
        // reachable body typeof-names is a reflection target the app-module loop
        // cannot see (Google.Protobuf hands typeof(Value) to GeneratedClrTypeInfo,
        // then its accessor factories GetMethod/Invoke Has*/Clear* members nothing
        // calls statically — an unreached member has no metadata row, so GetMethod
        // answers null where real .NET answers the row). Reach the surface a
        // reflection helper can invoke without synthesizing an argument it cannot
        // know: instance property accessors and public parameterless non-generic
        // instance methods. Deliberately NOT every body — the typeof-named bound
        // keeps the IL2CPP-with-managed-stripping semantics the app loop's comment
        // states, and the accessor/niladic bound keeps a typeof-named converter
        // class from force-reaching a parameterized body's untranspilable subtree
        // (the DataSetConverter.ReadJson failure that rejected opening whole
        // libraries). The residue stays loud: GetMethod over a parameterized
        // library method answers null and the caller's own diagnostic names it.
        // Alternate with the drain: a newly reached body can typeof-name more.
        if (_reflectionInvokeUsed)
        {
            int doneNamed = 0;
            while (doneNamed < _typeofNamedLibraryClasses.Count)
            {
                int end = _typeofNamedLibraryClasses.Count;
                for (int i = doneNamed; i < end; i++)
                {
                    var cls = _typeofNamedLibraryClasses[i];
                    if (cls.IsInterface || cls.IsEnum
                        || cls.IntrinsicCppName is not null
                        || CoreIntrinsics.IsIntrinsicType(cls.FullName)
                        || ContainsCanonPlaceholder(cls)
                        || _backend?.WantsTypeofReflectionSurface(cls) == false)
                        continue;
                    foreach (var m in cls.EnsureMembers().Methods)
                    {
                        if (m.Rva == 0 || m.IsStatic || m.Name is ".ctor" or ".cctor"
                            || _backend?.ShouldSkipMethodBody(cls, m) == true)
                            continue;
                        if ((m.Attributes & MethodAttributes.SpecialName) != 0
                            && (m.Name.StartsWith("get_", StringComparison.Ordinal)
                                || m.Name.StartsWith("set_", StringComparison.Ordinal)))
                        {
                            Reach(m);
                            continue;
                        }
                        if ((m.Attributes & MethodAttributes.MemberAccessMask)
                            != MethodAttributes.Public)
                            continue;
                        try
                        {
                            if (m.Signature.GenericParameterCount == 0
                                && m.Signature.ParameterTypes.Length == 0)
                                Reach(m);
                        }
                        catch (Exception e) when (!IsMustEscape(e))
                        {
                            // A method whose signature does not decode is not
                            // reflectively invokable either.
                        }
                    }
                    if (!cls.IsAbstract)
                        ReachUserInterfaceImpls(cls);
                }
                doneNamed = end;
                DrainReachability();
            }
        }

        // Reflection-attribute reachability route: if the program calls
        // GetCustomAttributes/IsDefined, reach every reflectable attribute's ctor + named
        // property setters and allocate the attribute types so GetCustomAttributes can
        // materialize fresh instances. Elements scanned are user-module (app + referenced
        // libraries), so an attribute on a library-declared class is reachable too;
        // attribute types are bounded to non-framework modules plus the user-typeof-named
        // framework attributes (per DecodeCustomAttributes) so a real CoreLib pulled in
        // with -r is not force-reached. Framework modules are skipped rather than merely
        // bounded on the attribute side because a BCL class can only carry a BCL-declared
        // attribute (a framework assembly cannot reference user code) — reflectable now
        // only when user code typeof-names it, a read the emit-time attr tables still
        // serve (BuildAttrTable decodes per element; RenderAttrCreate drops a row whose
        // ctor was not reached, so an un-walked framework element's row degrades rather
        // than naming an unemitted symbol) — and scanning them would pay blob-decode +
        // MethodMap lookups on every reached CoreLib class for a handful of rows.
        if (_reflectionAttrUsed)
        {
            // The base filters a runtime GetCustomAttributes(Type) commonly names beside
            // a concrete attribute type: give System.Attribute (and object, noted by the
            // attr-array retag sites anyway) a ti_arr_ handle so the runtime's precise
            // attrType[] stamp (dn2cpp_find_array_ti) can answer for them too.
            if (TypeIndex().TryGetValue(("System", "Attribute"), out var attrCands)
                && attrCands[0].Item1.ClassMap.TryGetValue(attrCands[0].Item2, out var attrBase))
                NoteArrayElementType(TypeDesc.MakeClass(attrBase));
            // Alternate the walk with the drain to a fixpoint on the typeof-named
            // framework-type set: reaching an attribute ctor/setter queues bodies, the
            // drain scans them, and a scanned USER body can typeof-name a framework
            // attribute type (_userTypeofNamedFrameworkTypes) that widens what
            // DecodeCustomAttributes keeps — so the walk must run again to pick up the
            // rows the earlier round dropped. The walk itself is idempotent (Reach and
            // the note sites dedupe), so a repeat round costs blob re-decodes only.
            int seenTypeofNamed = -1;
            while (seenTypeofNamed != _userTypeofNamedFrameworkTypes.Count)
            {
                seenTypeofNamed = _userTypeofNamedFrameworkTypes.Count;
                foreach (var cls in Classes.ToList())
                {
                    if (!IsUserModule(cls.Module))
                        continue;
                    try
                    {
                        var reader = cls.Module.Reader;
                        var td = reader.GetTypeDefinition(cls.Handle);
                        ReachAttributesOf(cls.Module, td.GetCustomAttributes());
                        foreach (var fh in td.GetFields())
                            ReachAttributesOf(cls.Module, reader.GetFieldDefinition(fh).GetCustomAttributes());
                        foreach (var mh in td.GetMethods())
                        {
                            var md = reader.GetMethodDefinition(mh);
                            ReachAttributesOf(cls.Module, md.GetCustomAttributes());
                            foreach (var pph in md.GetParameters())
                                ReachAttributesOf(cls.Module, reader.GetParameter(pph).GetCustomAttributes());
                        }
                        foreach (var ph in td.GetProperties())
                            ReachAttributesOf(cls.Module, reader.GetPropertyDefinition(ph).GetCustomAttributes());
                    }
                    catch (Exception e) when (!IsMustEscape(e))
                    {
                        // A class without decodable metadata (e.g. a generic instance) just
                        // contributes no reflected attributes.
                    }
                }
                // Assembly-level custom attributes: every loaded module's assembly
                // attributes participate in the same keep-alive rules (attribute ctor +
                // named setters + typeof-arg types), so Assembly.GetCustomAttributes can
                // materialize them. DecodeCustomAttributes bounds this to attribute types
                // defined in non-framework modules (plus the user-typeof-named framework
                // ones), so a real CoreLib pulled in with -r contributes almost nothing.
                foreach (var module in Modules)
                    try
                    {
                        ReachAttributesOf(module, module.Reader.GetAssemblyDefinition().GetCustomAttributes());
                    }
                    catch (Exception e) when (!IsMustEscape(e))
                    {
                        // A module without an assembly definition contributes no attributes.
                    }
                DrainReachability();
            }
        }

        // Last, because it reads two facts the phases above are still producing — which
        // structs got boxed, and whether anything compares two objects — and because the
        // walk it starts can produce more of both (a field's Equals override is a body, and
        // a body can box). Alternate with the drain to a fixpoint, exactly as the
        // used×allocated cross product it stands in for does.
        while (ReachBoxedValueEquality())
            DrainReachability();

        // --trim-godot-classes: the allowlist may only grow while reachability can
        // still deliver the released lambdas' subtrees — freeze it here, after the
        // last drain and before ANY emission (including the emitter's planning
        // pass), and compute the ldftn redirect table (cut ⟹ route, sealed).
        TrimFreeze();
    }

    /// <summary>Reflection-ctor route, extension beyond the app-module loop: open a
    /// construction route for every type that the user code's declared surface —
    /// an app-module (or already-opened library target's) field or property type,
    /// or a generic-method type argument an app-module body instantiates
    /// (<see cref="_appMethodSpecTypeArgs"/>) — NAMES (directly, as a nested
    /// generic argument, or as an array element) and that the app-module loop
    /// cannot see. Two kinds of named target, two treatments:
    ///
    /// <list type="bullet"><item>a FRAMEWORK-module closed generic specialization
    /// (<c>Dictionary&lt;string, AppType&gt;</c>) gets its parameterless instance
    /// ctor — the overload <c>GetDefaultConstructor</c> asks for;</item>
    /// <item>a non-framework LIBRARY class (Thrive's <c>GameWiki</c> in
    /// ThriveScriptsShared.dll — any arity, non-generic included) gets the app
    /// treatment: ALL instance ctors (Newtonsoft binds a parameterized ctor when a
    /// type has no default one — <c>GameWiki.Page</c> has only a 7-argument one),
    /// allocation, and — with its user-module base chain — its property accessors,
    /// because the deserializer then populates the object through
    /// <c>PropertyInfo.SetValue</c> on accessors nothing calls statically
    /// (GameWiki's <c>{ get; set; } = null!;</c> members: an initializer stores the
    /// backing field, so the setter has no static caller and its proptab slot
    /// emitted nullptr). FieldInfo.Get/SetValue needs no reach at all (field
    /// thunks are data access, emitted with the field table). An opened target's
    /// own member types join the surface for the next round, so the closure walks
    /// exactly the deserialization tree.</item></list>
    ///
    /// <para>This is the shape a reflection deserializer walks (Newtonsoft.Json's
    /// <c>ReflectionUtils.GetDefaultConstructor</c>): the target type comes off
    /// <c>FieldInfo.FieldType</c>/<c>PropertyInfo.PropertyType</c>, so
    /// <c>Dictionary&lt;string, AppType&gt;</c> appears ONLY as a construction
    /// target — nothing constructs it statically, so its <c>.ctor</c> is never
    /// reached and its emitted ctor table reads <c>(nullptr, 0)</c>. The app-module
    /// loop above cannot see either kind: neither is an app-module class.</para>
    ///
    /// <para>The key is the NAMED surface, deliberately NOT library-module
    /// membership. Opening every class of every -r library is not a cost question but
    /// a soundness one: allocating every concrete library class turns on the
    /// used-slot × allocated-type cross product for classes nothing constructs, which
    /// drags untranspilable BCL subtrees (a JSON DataSet converter reaching
    /// System.Data → XmlSerializer) into programs that can never select them. The
    /// residue of the named bound: a library type a deserializer constructs from a
    /// NAME the static surface never carries (a <c>$type</c> string in the data, a
    /// bare <c>typeof</c> handed straight to <c>Activator</c>) stays closed and fails
    /// loudly at run time with the no-constructor diagnostic.</para>
    ///
    /// <para>A member declared at an abstract collection INTERFACE
    /// (<c>IReadOnlyList&lt;int&gt;</c>) takes one more step: the deserializer
    /// constructs the interface's canonical BCL materialization, which it mints with
    /// <c>typeof(ReadOnlyCollection&lt;&gt;).MakeGenericType(itemType)</c> — an
    /// instantiation nothing in the program names statically. The same walk
    /// therefore also seeds those wrappers
    /// (<see cref="CollectionInterfaceWrapperDefs"/>), reaching their parameterless
    /// and single-reference-argument public ctors.</para>
    ///
    /// <para>The surface is member TYPES plus creator ARGUMENTS: a deserializer
    /// binding a parameterized creator deserializes each argument at the ctor's
    /// declared parameter type, so the instance-ctor parameter types of every
    /// openable class join <c>named</c> alongside its field/property types.
    /// And a collection shape a deserializer must mutate through a wrapper of its
    /// OWN (Newtonsoft's <c>CollectionWrapper&lt;T&gt;</c> /
    /// <c>DictionaryWrapper&lt;K,V&gt;</c>, minted with <c>MakeGenericType</c>
    /// exactly like the materializations above, but living in the serializer's
    /// assembly rather than the BCL) seeds those adapters too
    /// (<see cref="CollectSerializerWrapperSeeds"/>) — resolved by (ns, name)
    /// against the loaded module set, so the whole mechanism is a no-op unless the
    /// program actually links Newtonsoft (Thrive's boot blocker: a
    /// <c>SerializedData(ICollection&lt;string&gt;)</c> /
    /// <c>VersionPatchNotes(…, List&lt;string&gt;, …)</c> creator argument dying on
    /// the missing <c>CollectionWrapper&lt;string&gt;</c>).</para>
    ///
    /// <para>An SZARRAY on either surface seeds the TEMPORARY LIST
    /// (<see cref="CollectArrayTemporaryListSeed"/>): a reflection deserializer
    /// materializing a <c>T[]</c> member never constructs the array — it builds the
    /// items into a <c>typeof(List&lt;&gt;).MakeGenericType(elementType)</c>
    /// temporary first and converts afterwards, and nothing constructs
    /// <c>List&lt;T&gt;</c> statically when the member is declared <c>T[]</c>
    /// (Thrive's boot: <c>List&lt;MusicContext&gt;</c> off a <c>MusicContext[]</c>
    /// member, minted by layout alone, ctor table <c>(nullptr, 0)</c>).</para>
    ///
    /// <para>The key is deliberately NOT "every ctor of every specialization" — that
    /// is the walk-that-filters anti-pattern, and force-reaching arbitrary BCL ctor
    /// bodies is both bloat and a termination risk. It mirrors how the user route
    /// keys on user-module membership: the user code's declared member surface is
    /// what a deserializer can name, so that closure — the same one whose emitted
    /// field tables give these types their ti_ — is what opens the route, and only
    /// the parameterless ctor is reached (the overload <c>GetDefaultConstructor</c>
    /// asks for; a parameterized BCL ctor an app calls is reached normally).</para>
    ///
    /// <para>Residues of that bound, neither with a known live trigger. An
    /// interface named ONLY as a non-ctor method parameter or return type is not member
    /// surface, so its materialization stays unseeded — fields, properties, instance-ctor
    /// parameters and app MethodSpec type args are the whole surface collected below. And
    /// nothing seeds at all unless the program calls <c>ConstructorInfo.Invoke</c> or
    /// non-generic <c>Activator.CreateInstance(Type)</c>: <c>_reflectionCtorUsed</c> gates
    /// the entire route, so a program that merely probes <c>MakeGenericType</c> gets no
    /// seeds. Either miss surfaces as <c>dn2cpp_type_make_generic</c>'s catchable
    /// NotSupportedException naming the missing instantiation, and the remedy is to widen
    /// this surface for the corpus that hits it — never to force-reach arbitrary BCL
    /// ctors, per the paragraph above.</para>
    ///
    /// <para>Decode discipline: a field-type/property-signature read is a decode and
    /// a decode appends to <see cref="Classes"/>, so the walk snapshots and collects
    /// first, then acts. The decodes land here deterministically (flag-independent
    /// per input: the route fires off the program's own IL) and are the very reads
    /// the emit set's layout closure performs later, so the model grows nothing it
    /// would not have grown — only earlier, inside the drain that shapes it.</para></summary>
    /// <summary>Reaches <paramref name="cls"/>'s implementations of USER-module
    /// interface methods — the surface a reflection helper dispatches after
    /// discovering the interface on an instance (GetInterfaces → GetMethod → Invoke,
    /// the DI-container injection shape). Parameterized impls included: the interface
    /// contract is what lets the helper synthesize the arguments a bare parameterized
    /// method denies it. Framework interfaces stay out for the reason the typeof-named
    /// loop's niladic bound exists — IEnumerable/IComparable impls over every opened
    /// class would force-reach subtrees nothing invokes.</summary>
    private void ReachUserInterfaceImpls(ClassInfo cls)
    {
        for (var c = cls; c is not null; c = c.BaseClass)
            foreach (var itf in c.Interfaces)
            {
                if (!IsUserModule(itf.Module))
                    continue;
                foreach (var im in itf.EnsureMembers().Methods)
                {
                    try
                    {
                        if (im.Signature.GenericParameterCount == 0
                            && ResolveItfImplOrNull(cls, im) is { Rva: not 0 } impl
                            && _backend?.ShouldSkipMethodBody(impl.DeclaringClass, impl) != true)
                            Reach(impl);
                    }
                    catch (Exception e) when (!IsMustEscape(e))
                    {
                        // An undecodable signature is not reflectively invokable.
                    }
                }
            }
    }

    // A reflection-constructed instance is also a lifecycle-dispatch target: a
    // Unity-style invoker calls GetMethod(name, Instance | Public | NonPublic)
    // then Invoke(target, null) for well-known niladic messages (a generated
    // view initializer, Awake/OnEnable/Update) — private methods included, and
    // an unreached method has no metadata row, so GetMethod answers null where
    // real .NET answers the row and the binding silently never happens. Reach
    // the non-generic niladic VOID instance methods along the user-module base
    // chain (GetMethod on the concrete type sees inherited non-private rows;
    // private ones only on the type itself, but reaching those too costs
    // nothing new — a niladic body names no argument type). A lifecycle message
    // is fire-and-forget, so the void bound is what keeps a value-returning
    // niladic like GDTask's AsValueTask — whose body trips the transpiler's
    // IValueTaskSource result-kind bound — out of the speculative set.
    // Parameterized and value-returning methods stay the deliberate residue,
    // exactly as in the typeof-named loop's bound. NOT
    // called from the preservation ctor arm: [Preserve] on a type keeps the
    // type and its ctors only (Unity linker parity, asserted by the
    // preserve-control gate's stripped-member list).
    private void ReachUserNiladicInstanceMethods(ClassInfo cls)
    {
        for (var c = cls; c is not null && IsUserModule(c.Module); c = c.BaseClass)
            foreach (var m in c.EnsureMembers().Methods)
            {
                if (m.Rva == 0 || m.IsStatic || m.Name == ".ctor"
                    || _backend?.ShouldSkipMethodBody(c, m) == true)
                    continue;
                try
                {
                    if (m.Signature.GenericParameterCount == 0
                        && m.Signature.ParameterTypes.Length == 0
                        && m.Signature.ReturnType.IsVoid)
                        Reach(m);
                }
                catch (Exception e) when (!IsMustEscape(e))
                {
                    // An undecodable signature is not reflectively invokable.
                }
            }
    }

    private void ReachUserSurfaceNamedSpecializationCtors()
    {
        // Collect the declared member-type surface: the app module, plus every
        // library target an earlier round OPENED (with its user-module base
        // chain) — an opened target's members are what the deserializer
        // populates next, so its member types are nameable exactly as an app
        // type's are (Thrive's GameWiki, in ThriveScriptsShared.dll, declares
        // the List<Page> properties wiki.json deserializes into). NOT every
        // library class: an un-named library type's members are no more
        // reachable by a deserializer than the type itself. Snapshot the class
        // list (reads below grow it); fields are shape but their TYPES decode on
        // read, and a property type is a signature decode off the metadata row.
        // Each entry carries its provenance: LibSurface marks a member type of an
        // OPENED library target rather than of the app module (the MATERIALIZATION
        // seeds below stay app-surface-only — see the comment at the seed check),
        // and CreatorArg marks an instance-ctor parameter type (the serializer-
        // wrapper seeds' generous arm is scoped to those).
        var named = new List<(TypeDesc Type, bool LibSurface, bool CreatorArg)>();
        foreach (var cls in Classes.ToList())
        {
            bool libSurface = cls.Module != AppModule;
            if (libSurface && !_userReflSurface.Contains(cls))
                continue;
            foreach (var f in cls.Fields)
                if (!f.IsLiteral)
                    named.Add((f.Type, libSurface, false));
            // The generic BASE instantiations' type arguments are surface too: a
            // framework base takes the derived class's collaborator types as type
            // parameters (`GlobalPage : PageBase<..., GlobalWindow>`) and its shared
            // body late-binds `new TWindow()` — a construction site of the ARGUMENT,
            // visible in no field, property or ctor-parameter signature.
            for (var b = cls.BaseClass; b is not null; b = b.BaseClass)
                foreach (var a in b.Context.TypeArgs)
                    named.Add((a, libSurface, false));
            try
            {
                var reader = cls.Module.Reader;
                var td = reader.GetTypeDefinition(cls.Handle);
                foreach (var ph in td.GetProperties())
                    named.Add((reader.GetPropertyDefinition(ph)
                        .DecodeSignature(SigProvider, cls.Context).ReturnType, libSurface, false));
            }
            catch (Exception e) when (!IsMustEscape(e))
            {
                // A class without decodable metadata contributes no property types.
            }
            // The CREATOR-ARGUMENT half of the surface: a deserializer that binds a
            // parameterized creator ([JsonConstructor], the no-default-ctor
            // fallback) deserializes each argument at the ctor's declared
            // PARAMETER type before any instance exists (Newtonsoft's
            // CreateObjectUsingCreatorWithParameters resolves the argument's
            // contract off ParameterInfo.ParameterType), so an openable class's
            // instance-ctor parameter types are nameable exactly as its member
            // types are — and a parameter with no matching property is invisible
            // to the field/property walk above. MembersReady-guarded: an unpulled
            // class was never force-reached or opened, so no ctor row of it is
            // reflectively bindable (and the guard keeps this walk decode-free
            // under DN2CPP_STRICT_COMPLETION). A reached ctor's signature is
            // already decoded; reading it again is the memoized read.
            if (cls.MembersReady)
            {
                try
                {
                    foreach (var m in cls.Methods)
                        if (m.Name == ".ctor" && !m.IsStatic && m.Rva != 0)
                            foreach (var pt in m.Signature.ParameterTypes)
                                named.Add((pt, libSurface, true));
                }
                catch (Exception e) when (!IsMustEscape(e))
                {
                    // A ctor whose signature does not decode contributes no
                    // parameter types (it is not reflectively bindable either).
                }
            }
        }

        // The generic-METHOD half of the surface: type arguments of MethodSpecs
        // app bodies instantiated, recorded by the scan (closed TypeDescs, so
        // appending them decodes nothing here). The caller alternates this
        // method with the drain until the list stops growing, so args recorded
        // by bodies the force-reach loop queued are picked up on the next round.
        foreach (var t in _appMethodSpecTypeArgs)
            named.Add((t, false, false));

        // A typeof-named LIBRARY class contributes its base-chain instantiation
        // arguments even though its member types stay closed: the class reaches
        // reflection only through its typeof registration, and its framework base
        // takes its collaborator types as type parameters and late-binds
        // `new TArg()` in a shared body — a construction site of the argument
        // that no field, property or ctor-parameter signature shows.
        foreach (var cls in _typeofNamedLibraryClasses)
            for (var b = cls.BaseClass; b is not null; b = b.BaseClass)
                foreach (var a in b.Context.TypeArgs)
                    named.Add((a, true, false));

        // Expand the named surface — nested generic arguments and array/byref
        // elements — into the closed non-app specializations it names. Dedupe into
        // a list so the act phase runs in first-named order (deterministic).
        var seen = new HashSet<ClassInfo>();
        var targets = new List<ClassInfo>();
        // Library targets staged this round (opened in the act phase below; the
        // persistent sets grow there, never during the walk).
        var userTargets = new List<ClassInfo>();
        // Closed BCL collection INTERFACES on the same surface: a reflection
        // deserializer cannot construct the interface, so it constructs the
        // interface's canonical BCL materialization instead — obtained with
        // typeof(Def<>).MakeGenericType(itemType), which resolves only against
        // the AOT-generated closed types. Collect them here; seeded after the
        // concrete targets below. Each staged seed carries the closed type
        // arguments to instantiate its definitions at (the interface's own args
        // for the materialization seeds; the matched collection interface's args
        // for the serializer-wrapper seeds below).
        var seenItfs = new HashSet<ClassInfo>();
        var wrapperSeeds = new List<(TypeDesc[] Args, (string Ns, string Name)[] Defs)>();
        // SERIALIZER-wrapper seeds (the Newtonsoft mirror): a reflection
        // deserializer that must mutate a collection through a non-IList/
        // non-IDictionary surface wraps it in the serializer's OWN adapter type,
        // minted with typeof(CollectionWrapper<>).MakeGenericType(itemType)
        // (Newtonsoft's JsonArrayContract.CreateWrapper /
        // JsonDictionaryContract.CreateWrapper) — a library type, not BCL, so the
        // (ns, name) lookup below finds it only when that library is loaded and
        // the whole mechanism is a no-op otherwise. Hoisted presence test: every
        // corpus without the library skips the closure walks entirely, so the
        // output there is byte-identical with the seed compiled out.
        bool serializerDefs = SerializerWrapperDefsLoaded();
        var seenSerializerNodes = new HashSet<ClassInfo>();
        // TEMPORARY-LIST seeds for SZArrays on the surface. Dedupe on the element
        // descriptor — interned for the common kinds (primitives by table, classes
        // via ClassInfo.Desc); a non-interned duplicate (a jagged element) merely
        // stages twice and the act loop's seenWrappers absorbs it.
        var seenArrayElems = new HashSet<TypeDesc>();
        void Visit(TypeDesc t, bool libSurface, bool creatorArg)
        {
            if (t.Kind is TypeKind.SZArray or TypeKind.MDArray or TypeKind.ByRef or TypeKind.Pointer)
            {
                // An SZArray is a deserialization target whose items a reflection
                // deserializer builds into a MakeGenericType-minted List<elem>
                // temporary before converting to the array — seed that temporary
                // (the helper's summary carries the mirror and its filters).
                if (t.Kind == TypeKind.SZArray)
                    CollectArrayTemporaryListSeed(t.Element!, seenArrayElems, wrapperSeeds);
                Visit(t.Element!, libSurface, creatorArg);
                return;
            }
            if (t.Kind != TypeKind.Class)
                return;
            var c = t.Class!;
            foreach (var a in c.Context.TypeArgs)
                Visit(a, libSurface, creatorArg);
            // MATERIALIZATION seeds fire from the APP surface only. A library's
            // internal member typed at an abstract collection interface is
            // overwhelmingly its own plumbing, not a deserialization target — and
            // the seed's ReachAllocatedType arms the used-slot cross product for
            // the minted List<T>: Godot.Collections.Dictionary's
            // `ICollection<Variant> Keys` seeded List<Godot.Variant>, whose
            // IndexOf then failed the transpile on Variant's missing equality.
            // A library member DECLARED at such an interface is the documented
            // residue: its wrapper stays unseeded and the runtime MakeGenericType
            // diagnostic names the missing instantiation.
            if (!libSurface
                && c.IsInterface && c.Context.TypeArgs.Length > 0
                && IsFrameworkAssemblyName(c.Module.AssemblyName)
                && !ContainsCanonPlaceholder(c)
                && CollectionInterfaceWrapperDefs(GenericDefFullName(c), c.Context.TypeArgs.Length)
                    is { } wrapperDefs
                && seenItfs.Add(c))
                wrapperSeeds.Add((c.Context.TypeArgs, wrapperDefs));
            // SERIALIZER-wrapper seeds fire from BOTH surfaces, unlike the
            // materialization seeds above, because the wrap happens wherever the
            // deserializer POPULATES a collection, and Thrive's observed sites are
            // one of each: an app-declared `ICollection<string>` creator argument
            // (Tutorial.AlreadySeenTutorials' SerializedData) and a library-declared
            // `List<string>` one (ThriveScriptsShared.VersionPatchNotes). The
            // Variant residue that keeps the materialization seeds app-only is
            // handled INSIDE the helper instead of by a surface gate: the wrappers
            // delegate every element operation through the collection interfaces,
            // whose Contains/Remove slot uses arm the used-slot × allocated-type
            // product, so the helper declines any element type without a buildable
            // comparison (see its CanCompareStructuralField filter and the comment
            // there naming the Variant failure).
            if (serializerDefs && !ContainsCanonPlaceholder(c))
                CollectSerializerWrapperSeeds(c, creatorArg, seenSerializerNodes, wrapperSeeds);
            if (c.Module == AppModule                 // the app-module loop reaches these
                || c.IsInterface || c.IsEnum
                // An intrinsic type's members are never transpiled — both tests, per
                // the doctrine (the name table misses a closed generic intrinsic).
                || c.IntrinsicCppName is not null
                || CoreIntrinsics.IsIntrinsicType(c.FullName)
                || ContainsCanonPlaceholder(c))
                return;
            // A named non-framework LIBRARY class — any arity, non-generic
            // included, abstract included (its accessors serve the concrete
            // subclasses the deserializer builds) — is a library target.
            if (IsUserModule(c.Module))
            {
                if (!_userReflConstructed.Contains(c) && seen.Add(c))
                    userTargets.Add(c);
                return;
            }
            if (c.Context.TypeArgs.Length == 0        // closed generic specializations only
                || c.IsAbstract)
                return;
            if (seen.Add(c))
                targets.Add(c);
        }
        foreach (var (t, libSurface, creatorArg) in named)
            Visit(t, libSurface, creatorArg);

        // Act on the collected targets: reaching decodes members and grows Classes,
        // which is why it must not share the walk above. Finding the parameterless
        // row reads each candidate ctor's signature — an intended decode, on the
        // specialization being opened to reflection.
        foreach (var cls in targets)
        {
            MethodInfo? ctor = null;
            foreach (var m in cls.EnsureMembers().Methods)
                if (m.Name == ".ctor" && !m.IsStatic && m.Rva != 0
                    && m.Signature.ParameterTypes.Length == 0)
                {
                    ctor = m;
                    break;
                }
            if (ctor is null || _backend?.ShouldSkipMethodBody(cls, ctor) == true)
                continue;
            Reach(ctor);
            if (!cls.IsValueType)
                ReachAllocatedType(cls);
        }

        // Open the staged library targets: ALL instance ctors + allocation on the
        // target itself, property accessors on the target and its user-module base
        // chain (the deserializer populates an inherited property through the
        // BASE's accessor body — the derived proptab has no row for it), and
        // _userReflSurface membership so the next round collects their member
        // types. Instance accessors only: a deserializer reads and writes object
        // state, never static members, and every method NOT opened here is the
        // deliberate residue — MethodInfo.Invoke over an arbitrary library method
        // keeps the IL2CPP-with-managed-stripping bound the app loop's comment
        // states.
        foreach (var cls in userTargets)
        {
            _userReflConstructed.Add(cls);
            if (!cls.IsAbstract)
            {
                foreach (var m in cls.EnsureMembers().Methods)
                    if (m.Name == ".ctor" && !m.IsStatic && m.Rva != 0
                        && _backend?.ShouldSkipMethodBody(cls, m) is not true)
                        Reach(m);
                if (!cls.IsValueType)
                    ReachAllocatedType(cls);
                // The reflection helper that constructed the instance dispatches its
                // user-interface surface next (a DI container's GetInterfaces →
                // GetMethod → Invoke injection pass), so those impls are part of the
                // opened surface — as are the niladic lifecycle messages a
                // Unity-style invoker GetMethod-invokes on the fresh instance.
                ReachUserInterfaceImpls(cls);
                ReachUserNiladicInstanceMethods(cls);
            }
            for (var b = cls; b is not null && IsUserModule(b.Module); b = b.BaseClass)
            {
                if (!_userReflSurface.Add(b))
                    break;      // this base chain is already open from here up
                foreach (var m in b.EnsureMembers().Methods)
                    if (m.Rva != 0 && !m.IsStatic
                        && (m.Attributes & MethodAttributes.SpecialName) != 0
                        && (m.Name.StartsWith("get_", StringComparison.Ordinal)
                            || m.Name.StartsWith("set_", StringComparison.Ordinal))
                        && _backend?.ShouldSkipMethodBody(b, m) is not true)
                        Reach(m);
            }
        }

        // Canonical-wrapper seeds. An abstract collection interface on the surface
        // is a construction target only through its canonical BCL materialization:
        // Newtonsoft's JsonArrayContract/JsonDictionaryContract compute CreatedType
        // with typeof(ReadOnlyCollection<>).MakeGenericType(itemType) (and build the
        // items into a MakeGenericType-minted List<T>/Dictionary<K,V> first), and
        // dn2cpp's MakeGenericType — like IL2CPP's — resolves only closed types the
        // AOT closure generated. Nothing constructs ReadOnlyCollection<int>
        // statically when the member is declared IReadOnlyList<int>, so without this
        // seed the deserializer dies on a NotSupportedException (Thrive's
        // AchievementsManager.PerformLoad). Reached ctors: the parameterless one and
        // the single-reference-argument ones — the copy ctor a read-only wrapper is
        // built through (ReadOnlyCollection(IList<T>)) and the collection/comparer
        // ctors a deserializer picks; a primitive-argument ctor (capacity ints) is
        // no construction route a contract resolver takes. A separate dedupe from
        // `targets` on purpose: a wrapper that is ALSO a directly-named surface type
        // still needs its one-argument ctors, which the loop above never reaches.
        var seenWrappers = new HashSet<ClassInfo>();
        foreach (var (args, wrapperDefs) in wrapperSeeds)
            foreach (var (wns, wname) in wrapperDefs)
            {
                if (!TypeIndex().TryGetValue((wns, wname), out var cands))
                    continue;
                var (mod, tdh) = cands[0];
                var spec = Instantiate(mod, tdh, args);
                if (!seenWrappers.Add(spec)
                    // Both intrinsic tests, per the doctrine (the name table misses
                    // a closed generic intrinsic).
                    || spec.IntrinsicCppName is not null
                    || CoreIntrinsics.IsIntrinsicType(spec.FullName))
                    continue;
                foreach (var m in spec.EnsureMembers().Methods)
                {
                    if (m.Name != ".ctor" || m.IsStatic || !m.IsPublic || m.Rva == 0
                        || _backend?.ShouldSkipMethodBody(spec, m) == true)
                        continue;
                    var ps = m.Signature.ParameterTypes;
                    if (ps.Length == 0 || (ps.Length == 1 && ps[0].Kind != TypeKind.Primitive))
                        Reach(m);
                }
                if (!spec.IsValueType)
                    ReachAllocatedType(spec);
            }
    }

    /// <summary>The canonical BCL materializations of an abstract generic collection
    /// interface — the (namespace, metadata-name) keys of the open definitions a
    /// late-bound constructor (Newtonsoft's contract resolver, the BCL's own
    /// collection converters) mints with <c>MakeGenericType</c> when a member is
    /// declared at the interface. Null for any other definition name/arity. The set
    /// mirrors Newtonsoft's CreatedType table: the mutable interfaces materialize as
    /// <c>List&lt;T&gt;</c> / <c>HashSet&lt;T&gt;</c> / <c>Dictionary&lt;K,V&gt;</c>;
    /// the read-only ones additionally as the <c>ReadOnlyCollection&lt;T&gt;</c> /
    /// <c>ReadOnlyDictionary&lt;K,V&gt;</c> wrapper (built FROM the mutable one, so
    /// both are seeded).
    /// <para>What the table deliberately omits: <c>IReadOnlySet&lt;T&gt;</c>,
    /// for which Newtonsoft computes no CreatedType, and every NON-collection definition
    /// a deserializer may mint (<c>Nullable&lt;&gt;</c>, a user generic, an Immutable
    /// builder) — those have no canonical materialization a mirror could name. Each miss
    /// is the runtime <c>dn2cpp_type_make_generic</c> NotSupportedException naming the
    /// instantiation, which is the remedy pointer; extend the table per corpus that hits
    /// one rather than pre-populating it.</para></summary>
    private static (string Ns, string Name)[]? CollectionInterfaceWrapperDefs(string defFullName, int arity)
        => (defFullName, arity) switch
        {
            ("System.Collections.Generic.IEnumerable", 1)
            or ("System.Collections.Generic.ICollection", 1)
            or ("System.Collections.Generic.IList", 1)
                => new[] { ("System.Collections.Generic", "List`1") },
            ("System.Collections.Generic.IReadOnlyCollection", 1)
            or ("System.Collections.Generic.IReadOnlyList", 1)
                => new[]
                {
                    ("System.Collections.Generic", "List`1"),
                    ("System.Collections.ObjectModel", "ReadOnlyCollection`1"),
                },
            ("System.Collections.Generic.ISet", 1)
                => new[] { ("System.Collections.Generic", "HashSet`1") },
            ("System.Collections.Generic.IDictionary", 2)
                => new[] { ("System.Collections.Generic", "Dictionary`2") },
            ("System.Collections.Generic.IReadOnlyDictionary", 2)
                => new[]
                {
                    ("System.Collections.Generic", "Dictionary`2"),
                    ("System.Collections.ObjectModel", "ReadOnlyDictionary`2"),
                },
            _ => null,
        };

    /// <summary>The (namespace, metadata-name) home of Newtonsoft.Json's internal
    /// collection adapters. These are LIBRARY types, not BCL: the seed resolves them
    /// by name against whatever modules the program loaded (<see cref="TypeIndex"/>),
    /// so a program without Newtonsoft — every gate corpus, the self-host — pays
    /// nothing and emits byte-identical output. The loaded module set is part of the
    /// transpiler's input, so the lookup is deterministic per input and safe for the
    /// self-host fixpoint (nothing environment-driven reaches the output).</summary>
    private const string SerializerWrapperNamespace = "Newtonsoft.Json.Utilities";

    /// <summary>Whether any loaded module defines the serializer's wrapper
    /// definitions — the hoisted no-op test for the serializer-wrapper seeds.</summary>
    private bool SerializerWrapperDefsLoaded()
        => TypeIndex().ContainsKey((SerializerWrapperNamespace, "CollectionWrapper`1"))
        || TypeIndex().ContainsKey((SerializerWrapperNamespace, "DictionaryWrapper`2"));

    /// <summary>Serializer-wrapper seeds for one surface-named type: when
    /// <paramref name="c"/> is a collection shape a reflection deserializer can wrap,
    /// stage <c>Newtonsoft.Json.Utilities.CollectionWrapper&lt;T&gt;</c> /
    /// <c>DictionaryWrapper&lt;K,V&gt;</c> (closed at the element/key/value types
    /// read off the shape's own collection interfaces) onto
    /// <paramref name="seeds"/>, for the act loop that reaches the parameterless and
    /// single-reference-argument public ctors and allocates the type — exactly the
    /// treatment the BCL materialization seeds get, because the CreateWrapper call
    /// this mirrors ends in <c>MakeGenericType(...).GetConstructor(...)</c> +
    /// invoke, and CollectionWrapper's two ctors (<c>IList</c> /
    /// <c>ICollection&lt;T&gt;</c>) and DictionaryWrapper's three (<c>IDictionary</c>
    /// / <c>IDictionary&lt;K,V&gt;</c> / <c>IReadOnlyDictionary&lt;K,V&gt;</c>) are
    /// all single-reference-argument.
    ///
    /// <para>The mirror of Newtonsoft's wrap conditions, one arm per contract side:</para>
    ///
    /// <list type="bullet"><item>the collection side (JsonArrayContract) wraps a
    /// deserialization target that implements <c>ICollection&lt;T&gt;</c> without
    /// non-generic <c>IList</c> — the interfaces <c>ICollection&lt;T&gt;</c> /
    /// <c>IList&lt;T&gt;</c> / <c>ISet&lt;T&gt;</c> (whose contract default-creates
    /// the materialization and wraps it) and every non-IList implementer
    /// (<c>HashSet&lt;T&gt;</c>, a custom collection) — plus an
    /// <c>IEnumerable&lt;T&gt;</c>-only CLASS, whose temporary is wrapped when a
    /// parameterized creator exists. An interface that carries no
    /// <c>ICollection&lt;T&gt;</c> (<c>IReadOnlyList&lt;T&gt;</c>, exactly-
    /// <c>IEnumerable&lt;T&gt;</c>) never wraps: its contract either sets no wrapper
    /// or builds through the materialization's parameterized creator;</item>
    /// <item>for a CREATOR ARGUMENT (<paramref name="creatorArg"/>) the collection
    /// arm is deliberately GENEROUS: the IList-assignable exclusion is dropped, so a
    /// <c>List&lt;T&gt;</c>-typed ctor parameter seeds <c>CollectionWrapper&lt;T&gt;</c>
    /// too. Real programs wrap such an argument even though the mirror says an
    /// IList-assignable target never does, and the asymmetry is deliberate: a seed the
    /// mirror calls unnecessary costs one small delegating type per element type,
    /// whereas one it wrongly skips is a runtime NotSupportedException in a shipped
    /// game. The generosity stays scoped to creator arguments because widening it to
    /// the whole member surface inflates the reachable set by tens of thousands of
    /// methods (the wrapper bodies' Contains/Remove slot uses arm the used-slot ×
    /// allocated-type product per element type);</item>
    /// <item>the dictionary side (JsonDictionaryContract) wraps only when the
    /// contract's CreatedType is not <c>IDictionary</c>-assignable — i.e. a custom
    /// <c>IDictionary&lt;K,V&gt;</c>/<c>IReadOnlyDictionary&lt;K,V&gt;</c> implementer
    /// that skips non-generic <c>IDictionary</c>, plus <c>ConcurrentDictionary</c>
    /// (wrapped unconditionally, issue #1582 in Newtonsoft) — and the interfaces
    /// themselves (an existing-value populate wraps through the DECLARED interface
    /// contract). <c>Dictionary&lt;K,V&gt;</c> and its IDictionary-implementing kin
    /// never wrap and no failure has ever been observed there, so the precise mirror
    /// stands on both surfaces. A dictionary-shaped type seeds no CollectionWrapper:
    /// the contract resolver classifies dictionary-first.</item></list>
    ///
    /// <para>Shape discipline: the closure walk reads <c>Interfaces</c>/<c>BaseClass</c>
    /// only — shape, never a member pull — but a specialization the surface collect
    /// just decoded may not have had its <c>CompleteShape</c> turn yet, and an empty
    /// interface list here would silently drop the seed. So the walk completes each
    /// generic node's shape on entry: the same decode the next drain would perform,
    /// only earlier — the model grows nothing it would not have grown (and only on a
    /// corpus that loads Newtonsoft; see the hoisted presence test).</para></summary>
    private void CollectSerializerWrapperSeeds(ClassInfo c, bool creatorArg,
        HashSet<ClassInfo> seenNodes,
        List<(TypeDesc[] Args, (string Ns, string Name)[] Defs)> seeds)
    {
        // String implements IEnumerable<char> but resolves to a string contract,
        // never a collection one — and it is on virtually every surface.
        if (c.FullName == "System.String")
            return;
        // Transitive interface closure of c (c itself included when it IS an
        // interface), plus the non-generic-IList/IDictionary bits the mirror tests.
        // Deterministic walk order (list order, base chain order) so the staged
        // seed order — and with it the reach order — is a pure function of the
        // input.
        var closure = new List<ClassInfo>();
        var visited = new HashSet<ClassInfo>();
        var stack = new Stack<ClassInfo>();
        stack.Push(c);
        bool hasNonGenericIDictionary = false;
        bool hasNonGenericIList = false;
        while (stack.Count > 0)
        {
            var node = stack.Pop();
            if (!visited.Add(node))
                continue;
            if (node.GenericArity != 0 && !node.ShapeCompleted)
                CompleteShape(node);
            if (node.IsInterface)
            {
                closure.Add(node);
                if (node.FullName == "System.Collections.IDictionary")
                    hasNonGenericIDictionary = true;
                if (node.FullName == "System.Collections.IList")
                    hasNonGenericIList = true;
            }
            for (var b = node; b is not null; b = b.BaseClass)
                foreach (var i in b.Interfaces)
                    stack.Push(i);
        }
        var dictNodes = new List<ClassInfo>();
        var enumNodes = new List<ClassInfo>();
        bool hasGenericICollection = false;
        foreach (var itf in closure)
        {
            if (itf.Context.TypeArgs.Length == 0 || ContainsCanonPlaceholder(itf)
                || !IsFrameworkAssemblyName(itf.Module.AssemblyName))
                continue;
            // Every element/key/value type must have a buildable comparison
            // (CanCompareStructuralField — the same pure test the synthesized
            // value-equality walk uses). The wrappers themselves synthesize no
            // equality, but their bodies USE the collection interfaces'
            // Contains/Remove slots, and a used slot demands a compilable body
            // from every allocated implementer: seeding
            // CollectionWrapper<Godot.Variant> off GodotSharp's `ICollection<Variant>
            // Keys` armed ICollection<Variant>.Contains, whose
            // Dictionary<StringName,Variant>.ValueCollection implementation calls
            // ContainsValue — Variant equality, a hard transpile failure. An
            // equality-less element keeps the loud runtime MakeGenericType
            // diagnostic instead — and no JSON creator argument legitimately
            // carries one.
            bool comparable = true;
            foreach (var a in itf.Context.TypeArgs)
                comparable &= CanCompareStructuralField(a, hash: false, new HashSet<ClassInfo>());
            if (!comparable)
                continue;
            switch (GenericDefFullName(itf))
            {
                case "System.Collections.Generic.IDictionary" when itf.Context.TypeArgs.Length == 2:
                case "System.Collections.Generic.IReadOnlyDictionary" when itf.Context.TypeArgs.Length == 2:
                    dictNodes.Add(itf);
                    break;
                case "System.Collections.Generic.ICollection" when itf.Context.TypeArgs.Length == 1:
                    hasGenericICollection = true;
                    break;
                case "System.Collections.Generic.IEnumerable" when itf.Context.TypeArgs.Length == 1:
                    enumNodes.Add(itf);
                    break;
            }
        }
        if (dictNodes.Count > 0)
        {
            bool wraps = c.IsInterface
                || !hasNonGenericIDictionary
                || GenericDefFullName(c) == "System.Collections.Concurrent.ConcurrentDictionary";
            if (!wraps)
                return;
            foreach (var n in dictNodes)
                if (seenNodes.Add(n))
                    seeds.Add((n.Context.TypeArgs,
                        new[] { (SerializerWrapperNamespace, "DictionaryWrapper`2") }));
            return;
        }
        // The collection arm's wrap test (see the summary): a creator argument is
        // generous; a member-surface shape wraps per the contract mirror — never
        // through non-generic IList, and an interface only with ICollection<T> in
        // its closure.
        if (!creatorArg
            && (hasNonGenericIList || (c.IsInterface && !hasGenericICollection)))
            return;
        foreach (var n in enumNodes)
            if (seenNodes.Add(n))
                seeds.Add((n.Context.TypeArgs,
                    new[] { (SerializerWrapperNamespace, "CollectionWrapper`1") }));
    }

    /// <summary>Temporary-list seed for one SZArray on the named surface: stage
    /// <c>List&lt;elem&gt;</c> for the act loop that reaches its parameterless and
    /// single-reference-argument public ctors and allocates it. A reflection
    /// deserializer materializing a <c>T[]</c> member never constructs the array —
    /// it builds the items into a <c>typeof(List&lt;&gt;).MakeGenericType(elementType)</c>
    /// temporary first (Newtonsoft's <c>JsonArrayContract.CreateTemporaryCollection</c>,
    /// constructed through <c>LateBoundReflectionDelegateFactory.CreateDefaultConstructor</c>
    /// → <c>GetConstructors()</c> → parameterless <c>ConstructorInfo.Invoke</c>) and
    /// converts with <c>Array.CreateInstance</c> + <c>CopyTo</c> afterwards. Nothing
    /// constructs <c>List&lt;T&gt;</c> statically when the member is declared
    /// <c>T[]</c> — the specialization is minted by the layout closure alone — so
    /// without the seed the temporary's emitted ctor table reads <c>(nullptr, 0)</c>
    /// and the deserializer dies on "Unable to find default constructor for
    /// System.Collections.Generic.List_…" (Thrive's SimulationParameters.LoadRegistry,
    /// <c>List&lt;MusicContext&gt;</c> off a <c>MusicContext[]</c> member).
    ///
    /// <para>This arm is the necessary pair of the precise reflected array member
    /// types (<c>CppEmitter.FieldTypeInfoExpr</c>'s SZArray arm +
    /// <c>TypeMetadataEmitter.NoteReflectedMemberArrayElements</c>): a precise
    /// <c>T[]</c> member type is what routes the contract resolver onto the ARRAY
    /// contract — and therefore onto this temporary — where the old
    /// <c>System.Object</c> degrade fell into the untyped/Linq contract and never
    /// asked for it. Every reflection-visible array member now takes this path.</para>
    ///
    /// <para>Fires from BOTH surfaces, like the serializer-wrapper seeds (an opened
    /// library target's <c>T[]</c> member is deserialized exactly as an app one),
    /// and is guarded the same way: every element must have a buildable comparison
    /// (<see cref="CanCompareStructuralField"/> — the seeded List's allocation arms
    /// the used-slot × allocated-type product for its Contains/IndexOf/Remove
    /// bodies, and a <c>List&lt;Godot.Variant&gt;</c> seed would demand Variant
    /// equality, a hard transpile failure). A declined element keeps the loud
    /// runtime diagnostic. A BYTE element never seeds:
    /// Newtonsoft classifies <c>byte[]</c> as a base64 PRIMITIVE contract — no
    /// temporary list is ever minted for it — and <c>byte[]</c> is on virtually
    /// every surface. Intrinsic elements are declined with both tests, per the
    /// doctrine (the name table misses a closed generic intrinsic).</para></summary>
    private void CollectArrayTemporaryListSeed(TypeDesc elem, HashSet<TypeDesc> seenElems,
        List<(TypeDesc[] Args, (string Ns, string Name)[] Defs)> seeds)
    {
        // Concrete, generic-arg-legal element kinds only: a byref/pointer element
        // cannot close List<T> at all, and an External element resolves to no
        // modeled class, so a seed could open nothing (the runtime MakeGenericType
        // diagnostic stays for both).
        if (elem.Kind is not (TypeKind.Primitive or TypeKind.Class or TypeKind.SZArray or TypeKind.MDArray))
            return;
        if (elem is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Byte }
            || ContainsCanonPlaceholder(elem))
            return;
        if (elem.Class is { } ec
            && (ec.IntrinsicCppName is not null || CoreIntrinsics.IsIntrinsicType(ec.FullName)))
            return;
        if (!CanCompareStructuralField(elem, hash: false, new HashSet<ClassInfo>()))
            return;
        if (seenElems.Add(elem))
            seeds.Add((new[] { elem },
                new[] { ("System.Collections.Generic", "List`1") }));
    }

    /// <summary>Whether an assembly simple name identifies a .NET framework/BCL
    /// assembly (used to bound the [UnmanagedCallersOnly] rooting to user/library
    /// modules — a non-framework library's native callbacks are rooted, while
    /// the BCL's own host/PAL callbacks stay tree-shaken).
    ///
    /// <para><c>Dn2Cpp.Runtime</c> — dn2cpp's own managed support shim, auto-loaded
    /// beside the executable — is the platform's half, not user code: its members
    /// exist to be pulled by the transpiler's dedicated routes (RequireShimType),
    /// and several are transpilable ONLY through them (Dn2CppConsoleWriter's ctor
    /// names the intrinsic-mapped TextWriter base ctor). The reflection-ctor
    /// route's library reach is what makes the classification load-bearing:
    /// treating the shim as a user library opened that ctor as a construction
    /// target and failed the transpile.</para>
    ///
    /// <para><b>This list is for assemblies whose transpile FAILS when they are treated
    /// as user libraries — a name does not belong here merely because dn2cpp ships
    /// it.</b> The conditional default references (<c>DnZlib</c>, <c>DnBrotli</c>,
    /// <c>DnHttp</c> — see <see cref="InjectDefaultRefs"/>) are shipped and are
    /// deliberately absent: none of them has a type of the kind that broke
    /// Dn2Cpp.Runtime (<c>DnHttp.DnHttpBackend</c> is an <c>internal static class</c>
    /// with no ctor at all, the two codec shims are ordinary IL on ordinary types, and
    /// their <c>[NativeImplementation]</c> adapters already satisfy
    /// <see cref="RegisterNativeImpl"/>'s shape constraints). Adding them would take
    /// them out of <see cref="IsUserModule"/>, drop their custom-attribute metadata,
    /// and move the output of the existing <c>-r DnZlib</c> gates — with not one
    /// motivating defect behind it.</para></summary>
    internal static bool IsFrameworkAssemblyName(string name) =>
        name is "mscorlib" or "netstandard" or "Dn2Cpp.Runtime"
        || name.StartsWith("System.", StringComparison.Ordinal)
        || name.StartsWith("Microsoft.", StringComparison.Ordinal);

    /// <summary>Whether a module is user/library code rather than a framework/BCL
    /// assembly — the complement of <see cref="IsFrameworkAssemblyName"/>. Used to
    /// gate the custom-attribute reflection tables, which must cover a class declared
    /// in a referenced (non-framework) library, not just the app module. Pre-existing
    /// quirk inherited from the name test: a user library literally named
    /// <c>System.*</c>/<c>Microsoft.*</c> reads as framework (its attributes go
    /// unemitted) — accepted, as no real user assembly claims a BCL name.</summary>
    internal bool IsUserModule(Module m) => !IsFrameworkAssemblyName(m.AssemblyName);

    /// <summary>Reaches an attribute's ctor, named-property setters, and allocated type
    /// for every reflectable custom attribute in the collection, and notes every
    /// typeof()-valued argument's class (single or array element) as a referenced type so
    /// its type-info is emitted and the argument stays renderable.</summary>
    private void ReachAttributesOf(Module module, CustomAttributeHandleCollection handles)
    {
        foreach (var da in DecodeCustomAttributes(module, handles))
        {
            Reach(da.Ctor);
            // GetCustomAttributes(attrType[, bool]) returns an array whose RUNTIME type
            // is attrType[] in .NET; the runtime helper stamps that identity via
            // dn2cpp_find_array_ti, which only answers for elements the image emitted a
            // ti_arr_ handle for. The filter is a runtime Type value, so no call site can
            // note it — note every reflectable attribute type here instead (the bounded
            // set a filter can usefully name). The ti alone suffices: the stamped array's
            // interface DISPATCH rides the shared reference-element fallback.
            NoteArrayElementType(TypeDesc.MakeClass(da.AttrClass));
            if (!da.AttrClass.IsValueType && !da.AttrClass.IsAbstract)
                ReachAllocatedType(da.AttrClass);
            foreach (var fa in da.Fixed)
            {
                NoteAttrArgTypes(fa.Value);
                NoteAttrArgArrayType(fa.Type);
            }
            foreach (var na in da.Named)
            {
                NoteAttrArgTypes(na.Value);
                NoteAttrArgArrayType(na.Type);
                // The setter may be declared on an attribute BASE class — walk the
                // chain, paired with the emit-side
                // lookup in CppEmitter.RenderNamedArg: a setter emit finds but
                // reach never reached fails its Reachable test and silently drops the
                // whole attribute row. Field-kind named args need no reach pairing —
                // their storage rides the struct layout.
                if (na.Kind == CustomAttributeNamedArgumentKind.Property && na.Name is { } pn
                    && da.AttrClass.InstanceMethodOnBaseChain("set_" + pn) is { } sm)
                    Reach(sm);
            }
        }
    }

    /// <summary>Notes the element type of an SZArray-typed attribute ARGUMENT so its
    /// precise <c>ti_arr_&lt;elem&gt;</c> handle is emitted (and header-declared — the
    /// declaration loop CppEmitter.ArrayTypeInfoDeclared answers from runs before any
    /// attribute table renders).
    /// The emit-side pairing is CppEmitter.RenderAttrArray, whose typed allocation
    /// names that handle: an attribute-built array left on the shared object[] handle
    /// has no interface-dispatch map, so the first LINQ over it inside the attribute
    /// ctor (e.g. <c>inputNames.Count(...)</c>) aborts loudly.
    /// An enum element also needs its own referenced ti_, exactly as
    /// TypeMetadataEmitter.NoteReflectedArrayType notes for member types.</summary>
    private void NoteAttrArgArrayType(TypeDesc t)
    {
        if (t is not { Kind: TypeKind.SZArray, Element: { Kind: TypeKind.Primitive or TypeKind.Class or TypeKind.External or TypeKind.SZArray } el })
            return;
        NoteArrayElementType(el);
        if (el is { Kind: TypeKind.Class, Class: { IsEnum: true } ec })
            NoteReferencedType(ec);
    }

    /// <summary>Notes the class behind a typeof()-valued attribute argument (a decoded
    /// <see cref="TypeDesc"/>, or an array of typed arguments carrying them) so the
    /// type-info survives tree-shaking for the attribute factory to reference.</summary>
    private void NoteAttrArgTypes(object? value)
    {
        if (value is TypeDesc { Kind: TypeKind.Class, Class: { } cls })
            NoteReferencedType(cls);
        else if (value is ImmutableArray<CustomAttributeTypedArgument<TypeDesc>> items)
            foreach (var item in items)
                NoteAttrArgTypes(item.Value);
    }

    /// <summary>Drives the reachability/discovery fixpoint to quiescence: complete
    /// pending specializations and scan every newly-reached body for its callees and
    /// generic instantiations. Run during the initial discovery and again whenever a
    /// later phase reaches new methods (e.g. the emit-time array wrapper reach),
    /// so an emit-time-reached method's transitive callees (its own helpers, thrown
    /// exception types, EqualityComparer&lt;T&gt;) are reached and emitted too.</summary>
    /// <summary>Self-hosting feasibility harness (CppEmitter.MeasureGaps): when
    /// non-null, <em>any</em> exception thrown while completing a specialization or
    /// scanning a method body for reachability is recorded here (by type) and that work
    /// item skipped, instead of aborting the whole run — so one pass enumerates every
    /// reachability-phase gap, including an unexpected throw that would otherwise stop
    /// the measurement early. Null in the normal path, where such errors propagate as
    /// before.</summary>
    internal List<(MethodInfo? M, Exception Ex)>? ReachabilityDiagnostics;

    internal void DrainReachability()
    {
        bool progress = true;
        while (progress)
        {
            progress = false;

            while (PendingCount > 0)
            {
                // OUTSIDE the measure-mode try below, deliberately: that arm swallows
                // every exception into a gap row and keeps draining, which would not
                // just defeat the guard but feed it — each swallowed overrun would add
                // another gap row to the list that is already too big. The guard must
                // propagate.
                MemoryGuard.Check("specialize");
                var spec = DequeuePending();
                // Shape only. The queue exists so that a type NAMED anywhere can be laid out
                // and spelled correctly; it is not evidence that anything USES the type, and
                // it never was. Members are decoded by whoever actually needs them
                // (EnsureCompleted) — which is what stops the drain from being a transitive
                // closure over every signature in every assembly on the reference list.
                if (ReachabilityDiagnostics is null)
                    CompleteShape(spec);
                else
                    // Measure mode only: record any exception and keep draining — except
                    // the must-escape pair (IsMustEscape): the monomorphization bound must
                    // escape for the same reason MemoryGuard.Check is placed outside this
                    // arm (above): recording an overrun and draining on would not just
                    // defeat the guard, it would feed it.
                    try { CompleteShape(spec); }
                    catch (Exception ex) when (!IsMustEscape(ex))
                    { ReachabilityDiagnostics.Add((null, ex)); }
                progress = true;
            }

            while (_toScan.Count > 0)
            {
                MemoryGuard.Check("reach");  // outside the try, as above
                var m = _toScan.Dequeue();
                if (!_scanned.Add(m))
                    continue;
                if (ReachabilityDiagnostics is null)
                    ScanBodyForGenerics(m);
                else
                    // Measure mode only: record any exception and keep scanning — the
                    // must-escape pair excepted, as above.
                    try { ScanBodyForGenerics(m); }
                    catch (Exception ex) when (!IsMustEscape(ex))
                    { ReachabilityDiagnostics.Add((m, ex)); }
                progress = true;
            }
            if (ActivateConditionalPreservationPolicies())
                progress = true;
        }
    }

    private Queue<MethodInfo> _toScan = new();
    private MethodInfo? _currentScan;
    private readonly Dictionary<MethodInfo, MethodInfo?> _predTrace = new();

    // Used-slot virtual reachability (reference types). Rather than reaching the
    // *entire* dispatchable surface of every allocated reference type (which, for
    // a real-CoreLib type like List<T>, drags in IndexOf/Contains/LastIndexOf ->
    // Array.* -> SIMD SpanHelpers that are never called), we reach a virtual or
    // interface slot's implementation only when (a) the type is allocated and
    // (b) some callvirt/ldvirtftn actually targets that slot. The two facts arrive
    // in any order, so we keep both sets and take their cross-product to a fixpoint.
    private readonly HashSet<ClassInfo> _allocatedRefTypes = new();
    private readonly HashSet<MethodInfo> _usedVirtualDecls = new();

    /// <summary>True if the type's boxed form is allocated somewhere (a reference
    /// type via newobj, or a value type via box). A boxed value type needs an
    /// interface dispatch table in its type-info, unlike one only ever dispatched
    /// via direct constrained calls.</summary>
    public bool IsAllocated(ClassInfo c) => _allocatedRefTypes.Contains(c);

    /// <summary>The recorded reachability path to <paramref name="m"/> (newest
    /// first), for actionable diagnostics when a reached method cannot be
    /// emitted. Empty for roots.</summary>
    public string ReachChain(MethodInfo m)
    {
        var chain = new List<string>();
        var seen = new HashSet<MethodInfo>();
        for (var p = m; p is not null && seen.Add(p); _predTrace.TryGetValue(p, out p))
            chain.Add(p.DeclaringClass.FullName + "." + p.Name);
        return string.Join(" <- ", chain);
    }

    // ---- Engine-wrapper allowlist trim (--trim-godot-classes) ----------------
    //
    // The backend-nominated registry cctor (GodotSharp's Godot.Constructors..cctor)
    // registers one `native class name -> ptr => new X(ptr)` lambda per engine class,
    // and those ldftn edges are the single root of the whole engine-wrapper
    // surface: both reach roots (the ManagedCallbacks.Create ldftn table and the
    // [UnmanagedCallersOnly] rule) converge on this one cctor. So the CUT is one
    // place — the cctor's ldftn edges are deferred instead of reached — and the
    // ROUTE is one place — an undeferred lambda's ldftn at emit is redirected to the
    // nearest released ancestor's lambda (MethodCompiler's Ldftn case), so the
    // registry keeps every key, the dictionary's loud missing-name throw never
    // fires, and an unreleased class's engine object is wrapped as its nearest
    // released ancestor. cut ⟹ route holds structurally: a deferred lambda's symbol
    // is only ever named through the redirect, and the redirect table is frozen
    // after the reachability fixpoint — a release requested after the freeze that
    // would have changed the table fails the transpile (TrimRelease), with
    // AssertCalledBodiesEmitted as the unconditional backstop behind that.
    //
    // A lambda is RELEASED (reached normally) when user code — anything outside the
    // registry's own module; counting GodotSharp-internal mentions cascades back to
    // the full set — names its wrapper class: an allocation (new Sprite2D()), a
    // typeof/ldtoken, a castclass/isinst, a generic argument (GetNode<Sprite2D>),
    // a reached user method's signature (_Input(InputEvent)), a user class's base
    // chain (Player : CharacterBody2D), a --godot-class-root, or the unconditional
    // floor (GodotObject, RefCounted — the redirect terminators). Correctness of
    // the ancestor fallback rests on exactly that rule: every type test the program
    // can express names its target type, so the target is released, and a wrapper
    // allocated as the nearest released ancestor answers every expressible
    // cast/is identically to the true class. The observable residue is
    // GetType().Name-style string reflection over never-named wrappers — the same
    // documented-constraint bucket as --trim-reflection.

    private GodotClassTrimSpec? _godotClassTrim;
    private Module? _trimRegistryModule;

    internal bool GodotClassTrimEnabled => _godotClassTrim is not null;

    /// <summary>The allowlist: eligible classes user code has named. Grows only
    /// until <see cref="TrimFreeze"/>.</summary>
    private readonly HashSet<ClassInfo> _trimReleased = new();
    /// <summary>Wrapper class → its registry allocate-lambda, filled as the registry
    /// cctor's scan peeks each ldftn target (released and pending alike) — the map
    /// the redirect walk resolves ancestors through.</summary>
    private readonly Dictionary<ClassInfo, MethodInfo> _trimClassLambda = new();
    /// <summary>Deferred (cut) lambdas → the wrapper class each allocates; drained
    /// by a release, redirected by the freeze.</summary>
    private readonly Dictionary<MethodInfo, ClassInfo> _trimPendingLambdas = new();
    /// <summary>The frozen route: deferred lambda → nearest released ancestor's
    /// lambda. Consulted by MethodCompiler's Ldftn case.</summary>
    private readonly Dictionary<MethodInfo, MethodInfo> _trimLdftnRedirect = new();
    private bool _trimFrozen;

    /// <summary>Headline numbers for the transpile log: lambdas seen in the registry
    /// cctor, classes released, lambdas redirected to an ancestor.</summary>
    internal (int Registered, int Released, int Redirected) GodotClassTrimStats
        => (_trimClassLambda.Count, _trimReleased.Count, _trimLdftnRedirect.Count);

    /// <summary>Installs the backend's <see cref="IEmitBackend.GodotClassTrim"/>
    /// nomination. Runs before the first reachability drain — the registry cctor's
    /// scan consults the nomination, so it must be armed before anything is scanned
    /// — and releases the floor classes immediately.</summary>
    private void InstallGodotClassTrim(GodotClassTrimSpec spec)
    {
        _godotClassTrim = spec;
        _trimRegistryModule = spec.RegistryCctor.DeclaringClass.Module;
        foreach (var cls in spec.Floor)
            TrimRelease(cls);
    }

    /// <summary>Whether a class is within the trim's reach at all: declared in the
    /// registry's module and on the wrapper root's derivation tree (an engine
    /// wrapper). Everything else — app classes, BCL types, GodotSharp's own
    /// non-wrapper machinery — is never touched.</summary>
    private bool TrimEligibleClass(ClassInfo cls)
    {
        if (_godotClassTrim is not { } spec)
            return false;
        if (cls.Module != _trimRegistryModule || cls.IsValueType || cls.IsInterface)
            return false;
        for (var b = cls; b is not null; b = b.BaseClass)
            if (ReferenceEquals(b, spec.WrapperRoot))
                return true;
        return false;
    }

    /// <summary>Admits one class to the allowlist, reaching its deferred registry
    /// lambda if the cctor scan already pended it (the scan-side decline handles the
    /// other order). Post-freeze the allowlist is immutable: a release that would
    /// have changed the redirect table fails the transpile — that is the cut ⟹ route
    /// guard turning a would-be C++ link error into a transpiler error — while a
    /// release that changes nothing (no lambda, or its lambda already reached) is
    /// tolerated as the no-op it is.</summary>
    private void TrimRelease(ClassInfo cls)
    {
        if (_trimFrozen)
        {
            if (!_trimReleased.Contains(cls)
                && _trimClassLambda.TryGetValue(cls, out var frozen)
                && _trimLdftnRedirect.ContainsKey(frozen))
                throw new NotSupportedException(
                    $"--trim-godot-classes: engine wrapper {cls.FullName} was first named after "
                    + "the allowlist froze (post-reachability), but its registry lambda was "
                    + $"already redirected. Pass --godot-class-root {cls.FullName} to keep it.");
            return;
        }
        if (!_trimReleased.Add(cls))
            return;
        if (_trimClassLambda.TryGetValue(cls, out var lambda) && _trimPendingLambdas.Remove(lambda))
            Reach(lambda);
    }

    /// <summary>The scan-side CUT: called for every ldftn edge of the registry
    /// cctor's body. True defers the lambda (the caller skips the Reach edge);
    /// false means "reach it normally" — because the trim is off, the scan is not
    /// the registry cctor, the lambda's wrapper class is already released, or the
    /// lambda body is not the expected allocate shape (the safe fallback: an
    /// unrecognized entry is kept, never cut).</summary>
    private bool TrimTryDeferRegistryLambda(MethodInfo scanMethod, MethodInfo lambda)
    {
        if (_godotClassTrim is not { } spec || !ReferenceEquals(scanMethod, spec.RegistryCctor))
            return false;
        // Post-freeze the redirect table is sealed, so a fresh deferral could never
        // be routed: if the registry cctor were first scanned only by an
        // emitter-driven drain (DrainReachability under SyncSharedGenerics or an
        // intrinsic bridge), a pending entry minted here would be a dangling ldftn.
        // Reach every lambda normally instead — correct, merely untrimmed.
        if (_trimFrozen)
            return false;
        if (TrimLambdaAllocatedClass(lambda) is not { } cls)
            return false;
        _trimClassLambda[cls] = lambda;
        if (_trimReleased.Contains(cls))
            return false;
        _trimPendingLambdas[lambda] = cls;
        return true;
    }

    /// <summary>Peeks a registry lambda's body for the engine wrapper it allocates.
    /// The generated shape is exactly <c>ldarg.1; newobj Godot.X..ctor(nint); ret</c>
    /// (an instance method on the compiler's <c>&lt;&gt;c</c> closure singleton);
    /// nop and ldarg.0 are tolerated for Debug-shaped IL. Anything else — or a
    /// newobj target that is not an eligible engine wrapper — answers null, and the
    /// caller reaches the lambda normally instead of cutting it.</summary>
    private ClassInfo? TrimLambdaAllocatedClass(MethodInfo lambda)
    {
        if (lambda.Rva == 0 || lambda.Module != _trimRegistryModule)
            return null;
        ClassInfo? cls = null;
        foreach (var insn in ILDecoder.Decode(
                     lambda.Module.PE.GetMethodBody(lambda.Rva).GetILBytes()!.ToImmutableArrayCompat()))
        {
            switch (insn.OpCode)
            {
                case ILOpCode.Nop:
                case ILOpCode.Ldarg_0:
                case ILOpCode.Ldarg_1:
                case ILOpCode.Ret:
                    continue;
                case ILOpCode.Newobj when cls is null:
                    if (ResolveCallTarget(lambda.Module, SRME.EntityHandle(insn.Token), lambda.Context)
                        is not { } ctor)
                        return null;
                    cls = ctor.DeclaringClass;
                    continue;
                default:
                    return null;
            }
        }
        return cls is not null && TrimEligibleClass(cls) ? cls : null;
    }

    /// <summary>Release triggers ① (allocation) and ⑥ (a user class's engine base
    /// chain), hooked into <see cref="ReachAllocatedType"/> ahead of its dedup —
    /// a wrapper first allocated GodotSharp-internally and later from user code
    /// must still release on the user allocation.</summary>
    private void TrimNoteAllocated(ClassInfo c)
    {
        // ① an allocation named from user code (`new Sprite2D()` — the scan's
        // newobj/box edge lands here) releases the wrapper; a GodotSharp-internal
        // allocation does not. _currentScan is the naming site; a null/stale scan
        // context (a synthesis path outside any scan) errs toward releasing,
        // which only keeps more — never breaks.
        if (_currentScan is null || _currentScan.Module != _trimRegistryModule)
            TrimNoteNamedClass(c);
        // ⑥ a user class's base chain (`Player : CharacterBody2D`) releases each
        // engine ancestor, whatever context allocated it — the engine instantiates
        // script classes without any scannable naming site.
        if (c.Module != _trimRegistryModule)
            for (var b = c.BaseClass; b is not null; b = b.BaseClass)
                if (TrimEligibleClass(b))
                    TrimRelease(b);
    }

    /// <summary>Notes one class (and, recursively, its generic arguments — naming
    /// <c>Godot.Collections.Array&lt;Sprite2D&gt;</c> names Sprite2D) as named by
    /// user code, releasing every eligible engine wrapper among them.</summary>
    private void TrimNoteNamedClass(ClassInfo cls)
    {
        if (TrimEligibleClass(cls))
            TrimRelease(cls);
        foreach (var arg in cls.Context.TypeArgs)
            TrimNoteNamedType(arg);
    }

    /// <summary>The <see cref="TypeDesc"/> form of <see cref="TrimNoteNamedClass"/>:
    /// walks through array/byref/pointer wrappers to the classes a named type
    /// mentions. Null-tolerant so unresolved scan tokens stay a no-op.</summary>
    private void TrimNoteNamedType(TypeDesc? t)
    {
        if (t is { Kind: TypeKind.Class, Class: { } cls })
            TrimNoteNamedClass(cls);
        else if (t is { Element: { } el })
            TrimNoteNamedType(el);
    }

    /// <summary>Release trigger ④ at a scanned call/ldftn site: a generic method
    /// instantiation's type arguments (<c>GetNode&lt;Sprite2D&gt;</c>) and the type
    /// arguments of a generic declaring type (<c>Godot.Collections.Array&lt;Sprite2D&gt;
    /// .Add</c> — a MemberReference on a TypeSpecification). Decoding here mints
    /// nothing new: the call edge's own resolution already decoded these blobs.</summary>
    private void TrimNoteCallSite(Module module, EntityHandle handle, GenericContext ctx)
    {
        switch (handle.Kind)
        {
            case HandleKind.MethodSpecification:
            {
                var ms = module.Reader.GetMethodSpecification((MethodSpecificationHandle)handle);
                foreach (var arg in ms.DecodeSignature(SigProvider, ctx))
                    TrimNoteNamedType(arg);
                if (ms.Method.Kind == HandleKind.MemberReference)
                    TrimNoteCallSite(module, ms.Method, ctx);
                break;
            }
            case HandleKind.MemberReference:
            {
                var mr = module.Reader.GetMemberReference((MemberReferenceHandle)handle);
                if (mr.Parent.Kind == HandleKind.TypeSpecification)
                    TrimNoteNamedType(module.Reader
                        .GetTypeSpecification((TypeSpecificationHandle)mr.Parent)
                        .DecodeSignature(SigProvider, ctx));
                break;
            }
        }
    }

    /// <summary>Freezes the allowlist at the end of <see cref="Build"/> — after the
    /// last reachability fixpoint, before anything emits (both of the emitter's
    /// ComputeEmitted passes and the planning trials see one immutable table) — and
    /// computes the ROUTE: each still-deferred lambda redirects to the lambda of its
    /// wrapper's nearest released ancestor. The walk terminates by construction —
    /// the floor released GodotObject/RefCounted, and every eligible wrapper derives
    /// from GodotObject — but a registry whose floor entry never surfaced (an
    /// unexpected GodotSharp shape) fails loudly rather than emitting a dangling
    /// ldftn.
    /// <para>Reach CAN still run after the freeze — the emitter's drains
    /// (SyncSharedGenerics, the intrinsic bridges' DrainReachability) reach and scan
    /// methods — but a canonical owner body or an rgctx fill scans the same tokens
    /// the grouped member's own scan already walked, so an engine wrapper cannot
    /// make its FIRST appearance there; a post-freeze release that would have
    /// changed the table is therefore a bug guard (TrimRelease throws), and a
    /// post-freeze first scan of the registry cctor itself falls back to reaching
    /// its lambdas (TrimTryDeferRegistryLambda declines).</para></summary>
    private void TrimFreeze()
    {
        if (_godotClassTrim is null)
            return;
        _trimFrozen = true;
        foreach (var (lambda, cls) in _trimPendingLambdas)
        {
            MethodInfo? target = null;
            for (var b = cls.BaseClass; b is not null; b = b.BaseClass)
                if (_trimReleased.Contains(b) && _trimClassLambda.TryGetValue(b, out var anc)
                    && !_trimPendingLambdas.ContainsKey(anc))
                {
                    target = anc;
                    break;
                }
            _trimLdftnRedirect[lambda] = target ?? throw new NotSupportedException(
                $"--trim-godot-classes: no released ancestor lambda for {cls.FullName} — "
                + "the GodotObject floor entry never surfaced in the registry cctor "
                + "(unexpected GodotSharp shape)");
        }
        TrimWarnUnreleasedInternalCasts();
    }

    /// <summary>Diagnostic sweep behind the signature-trigger rationale: the claim
    /// that every wrapper a reached GodotSharp body CASTS to also appears in some
    /// reached signature (and is therefore released) is empirical, and its first
    /// counterexample in a future GodotSharp would otherwise surface only as a
    /// silent run-time InvalidCastException. Decode every reached registry-module
    /// body once more and WARN — never fail, the cast may sit on a path this
    /// program never runs — for each castclass/isinst target that is an eligible
    /// engine wrapper left unreleased (i.e. one the redirect will materialize as an
    /// ancestor), naming the type and the --godot-class-root remedy. Plain
    /// TypeDef/TypeRef tokens only: a wrapper is non-generic, so it can head no
    /// TypeSpec, and skipping specs keeps this sweep from minting instantiations
    /// after the freeze. Zero hits on the shipped GodotSharp corpus (asserted by
    /// the trim gate staying warning-free in its transpile log).</summary>
    private void TrimWarnUnreleasedInternalCasts()
    {
        var warned = new HashSet<ClassInfo>();
        foreach (var m in Reachable)
        {
            if (m.Module != _trimRegistryModule || m.Rva == 0 || m.IsSynthetic)
                continue;
            List<Instruction> insns;
            try
            {
                insns = ILDecoder.Decode(m.Module.PE.GetMethodBody(m.Rva).GetILBytes()!.ToImmutableArrayCompat());
            }
            catch (Exception e) when (!IsMustEscape(e))
            {
                continue; // a body the main scan could not decode was never compiled either
            }
            foreach (var insn in insns)
            {
                if (insn.OpCode is not (ILOpCode.Castclass or ILOpCode.Isinst))
                    continue;
                var handle = SRME.EntityHandle(insn.Token);
                if (handle.Kind is not (HandleKind.TypeDefinition or HandleKind.TypeReference))
                    continue;
                if (ResolveTypeTokenForScan(m.Module, handle, m.Context)
                        is not { Kind: TypeKind.Class, Class: { } cls })
                    continue;
                // A class named ONLY by a cast token may still be an uncompleted
                // shell (the main scan never resolves plain castclass TypeDefs), and
                // eligibility walks its base chain — complete the shape first, or an
                // unreleased wrapper would read as ineligible and the warning that
                // exists for exactly that case would never fire.
                EnsureCompleted(cls);
                if (!TrimEligibleClass(cls) || _trimReleased.Contains(cls) || !warned.Add(cls))
                    continue;
                Console.WriteLine(
                    $"dn2cpp: godot-class-trim warning: reached GodotSharp code casts to {cls.FullName}, "
                    + $"which nothing in this program names — an engine object of that class would be "
                    + $"wrapped as an ancestor and the internal cast would throw at run time if hit. "
                    + $"Pass --godot-class-root {cls.FullName} to keep it. (in "
                    + $"{m.DeclaringClass.FullName}.{m.Name})");
            }
        }
    }

    /// <summary>The emit-side ROUTE of <c>--trim-godot-classes</c>: the ancestor
    /// lambda a deferred registry lambda's ldftn must name instead, or null for
    /// every other method. MethodCompiler's Ldftn case is the single consumer;
    /// the table is frozen before emission.</summary>
    internal MethodInfo? TrimLdftnRedirect(MethodInfo m)
        => _trimLdftnRedirect.TryGetValue(m, out var r) ? r : null;

    /// <summary>Decode a method's `[DllImport]` metadata into a <see cref="PInvokeInfo"/>,
    /// or null when the method is not <c>pinvokeimpl</c>. The entry point is the
    /// import's explicit name, falling back to the method's own name when unspecified.
    /// Per-parameter / return <c>[MarshalAs(UnmanagedType.*)]</c> overrides are decoded
    /// from the marshalling descriptors.</summary>
    private static PInvokeInfo? ReadPInvoke(MetadataReader reader, MethodDefinition md)
    {
        if ((md.Attributes & MethodAttributes.PinvokeImpl) == 0)
            return null;
        var import = md.GetImport();
        if (import.Module.IsNil)
            return null; // malformed / vararg pinvoke with no module ref — leave as extern
        string moduleName = reader.GetString(reader.GetModuleReference(import.Module).Name);
        string entryPoint = import.Name.IsNil ? reader.GetString(md.Name) : reader.GetString(import.Name);

        // Darwin [f]setattrlist take a `struct attrlist*` whose leading u_short
        // pair the emitted managed AttrList layout cannot reproduce (sub-int32
        // struct fields are emitted at int32 width), so a direct libc bind would
        // hand the kernel a misaligned attribute bitmap (EINVAL — FileStatus's
        // macOS creation-time restore hits it on every mtime set that predates
        // the file's birthtime). Route them to runtime shims that convert the
        // layout (runtime/core/platform/posix/dn2cpp_system_native.cpp).
        if (moduleName == "libc" && entryPoint is "setattrlist" or "fsetattrlist")
            entryPoint = "dn2cpp_" + entryPoint;

        // Per-parameter / return [MarshalAs(UnmanagedType.*)] overrides. The
        // marshalling-descriptor blob's first byte is the UnmanagedType; the Parameter's
        // SequenceNumber is 0 for the return and 1..N for the parameters (mapped here to a
        // 0-based index). A parameter with no descriptor keeps its type+CharSet default.
        System.Runtime.InteropServices.UnmanagedType? retMarshal = null;
        var paramMarshal = new System.Collections.Generic.Dictionary<int, System.Runtime.InteropServices.UnmanagedType>();
        foreach (var ph in md.GetParameters())
        {
            var p = reader.GetParameter(ph);
            var bh = p.GetMarshallingDescriptor();
            if (bh.IsNil)
                continue;
            var blob = reader.GetBlobReader(bh);
            if (blob.Length == 0)
                continue;
            var u = (System.Runtime.InteropServices.UnmanagedType)blob.ReadByte();
            if (p.SequenceNumber == 0)
                retMarshal = u;
            else
                paramMarshal[p.SequenceNumber - 1] = u;
        }

        return new PInvokeInfo(moduleName, entryPoint, import.Attributes, retMarshal, paramMarshal);
    }

    /// <summary>Distinct linker library tokens (sorted) for the app-module P/Invoke
    /// calls that lowered to native calls — a <c>[DllImport("foo")]</c> contributes
    /// <c>"foo"</c> so the build links <c>-lfoo</c>. Always-linked
    /// platform libraries (libc / libSystem / <c>__Internal</c>) contribute nothing,
    /// so a program that only calls into libc needs no extra link flags at all (its
    /// self-contained binary is preserved).</summary>
    public IReadOnlyList<string> PInvokeLinkLibraries() =>
        PInvokeCalls.Select(m => LinkLibToken(m.PInvoke!.ModuleName))
            .OfType<string>()
            .Distinct(StringComparer.Ordinal)
            .OrderBy(t => t, StringComparer.Ordinal)
            .ToList();

    /// <summary>Normalize a <c>[DllImport]</c> module name to a <c>-l</c> library token,
    /// or null for an always-linked platform library that needs no flag. Strips a
    /// directory part, a shared-library extension (<c>.so[.N]</c> / <c>.dylib</c> /
    /// <c>.dll</c>) and a leading <c>lib</c>, mirroring how the .NET loader probes names
    /// (so <c>"libz.so.1"</c>, <c>"z"</c> and <c>"libz"</c> all map to <c>z</c>).</summary>
    private static string? LinkLibToken(string module)
    {
        string name = Path.GetFileName(module);
        int so = name.IndexOf(".so", StringComparison.Ordinal);
        if (so >= 0)
            name = name[..so];
        else if (name.EndsWith(".dylib", StringComparison.OrdinalIgnoreCase))
            name = name[..^6];
        else if (name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            name = name[..^4];
        if (name.StartsWith("lib", StringComparison.Ordinal))
            name = name[3..];
        // libc / libSystem (and the in-module __Internal self-reference) are linked by
        // every executable already, so emitting a flag for them is both redundant and
        // would make a libc-only program sprout a needless manifest. libm and
        // friends are kept (harmless on macOS, required on Linux).
        // System.Native (the .NET PAL, `[DllImport("libSystem.Native")]` in the real
        // CoreLib) contributes nothing either: the dn2cpp runtime implements the
        // SystemNative_* symbols itself (runtime/core/platform/posix/), so the binary
        // stays self-contained instead of linking the dotnet install's library.
        // System.IO.Compression.Native likewise contributes nothing: the dn2cpp runtime
        // implements the CompressionNative_* symbols itself against a vendored zlib
        // (runtime/core/intrinsics/dn2cpp_zlib_native.cpp), so the binary stays
        // self-contained instead of linking the dotnet install's native shim.
        // dn2cpp_http is the same shape: the DnHttp shim's [DllImport("dn2cpp_http")]
        // resolves to the dn2cpp_http2_call_* transport entry points compiled INTO the runtime
        // (runtime/core/intrinsics/dn2cpp_http2_stream.cpp under DN2CPP_USE_CURL), not a
        // separate library — so it needs no -l token (a `-ldn2cpp_http` finds nothing).
        return name.ToLowerInvariant() switch
        {
            // macOS libproc (/usr/lib/libproc.dylib -> "proc" after the strip above) is
            // libc's shape exactly: no such file exists on disk, proc_pidinfo/proc_pidpath
            // are exported by the always-linked libSystem, and `-lproc` would at best find
            // the SDK's libproc.tbd re-export. Nothing to link, so no token.
            "" or "c" or "system" or "__internal" or "system.native" or "proc"
                or "system.io.compression.native" or "dn2cpp_http"
                or "system.security.cryptography.native.apple"
                // The Linux RID's crypto native module: dn2cpp provides the
                // CryptoNative_* digest/HMAC/RNG symbols itself
                // (runtime/core/intrinsics/dn2cpp_openssl_crypto_digest.cpp) over
                // the same portable cores, so — like the Apple flavor — it links
                // nothing and stays self-contained (no real OpenSSL dependency).
                or "system.security.cryptography.native.openssl" => null,
            // CoreFoundation (Interop.AppleCrypto's cctor P/Invokes
            // CFStringCreateWithCString by absolute framework path): resolved by
            // the real framework, linked via `-framework CoreFoundation` in
            // runtime/CMakeLists.txt — no -l token.
            "/system/library/frameworks/corefoundation.framework/corefoundation" => null,
            _ => name,
        };
    }

    /// <summary><c>--pinvoke-module</c>: the [DllImport] module names opted in to the
    /// direct-native-call lowering from any loaded assembly (see
    /// <see cref="IsOptedInPInvokeModule"/>).</summary>
    private readonly HashSet<string> _pinvokeModules;

    /// <summary>True for a <c>[DllImport]</c> module the run opted in with
    /// <c>--pinvoke-module</c>, so a P/Invoke declared in a referenced assembly (an
    /// external binding library pulled in with <c>-r</c>, e.g. CriWare's
    /// <c>cri_atom</c>) lowers to a direct native call exactly like an app-module
    /// P/Invoke, and its library token reaches the pinvoke-libs.txt link manifest.
    /// The name is matched by ordinal equality against the exact [DllImport] module
    /// string — no case folding, no lib-prefix/extension normalization — keeping the
    /// contract as simple as the declaration it names. A CLI flag rather than an
    /// environment variable on purpose: it changes the C++ a successful transpile
    /// emits (the <c>--trim-reflection</c> doctrine). The per-run counterpart of
    /// <see cref="IsRuntimeProvidedPInvokeModule"/>'s fixed platform set.</summary>
    public bool IsOptedInPInvokeModule(string module) => _pinvokeModules.Contains(module);

    /// <summary>True for a bodyless <c>[DllImport]</c> method whose call sites lower to a
    /// direct native call: an app-module import, a runtime-provided module
    /// (<see cref="IsRuntimeProvidedPInvokeModule"/>), or a per-run opt-in
    /// (<see cref="IsOptedInPInvokeModule"/>). The ONE predicate every asker shares —
    /// the call route (<c>MethodCompiler.TranslateCall</c>) and the address-taken path
    /// (the ldftn arm noting <see cref="NotePInvokeFtnTarget"/>) — so the set of imports
    /// that lower cannot drift between a call site and a delegate method group over the
    /// same import.</summary>
    public bool LowersToDirectNativeCall(MethodInfo m) =>
        m.Rva == 0 && m.PInvoke is { } pinv
        && (m.DeclaringClass.Module == AppModule
            || IsRuntimeProvidedPInvokeModule(pinv.ModuleName)
            || IsOptedInPInvokeModule(pinv.ModuleName));

    /// <summary>ldftn/delegate targets that are bodyless P/Invokes whose call sites
    /// lower to direct native calls (<see cref="LowersToDirectNativeCall"/>), e.g.
    /// CriWare's <c>new CbFunc(NativeMethods.criErr_SetCallback)</c> — a delegate over
    /// the [DllImport] method itself. A call site lowers inline, but taking the address
    /// needs a function; CppEmitter synthesizes each body as a forwarder built from the
    /// same P/Invoke lowering a call site gets
    /// (<see cref="MethodCompiler.CompilePInvokeWrapper"/>), so the delegate invoke /
    /// calli marshals identically to a direct call. The method joins
    /// <see cref="Reachable"/> scan-less (there is no IL to follow), the same protocol
    /// as <see cref="IntrinsicFtnTargets"/>.</summary>
    internal HashSet<MethodInfo> PInvokeFtnTargets { get; } = new();

    public void NotePInvokeFtnTarget(MethodInfo m)
    {
        if (PInvokeFtnTargets.Add(m))
            Reachable.Add(m.EnsureSignature()); // reached => decoded, as in Reach
    }

    /// <summary>True for a <c>[DllImport]</c> module whose symbols the dn2cpp runtime /
    /// platform provides, so a P/Invoke in a <b>referenced</b> module (the real CoreLib)
    /// may lower to a direct native call like an app-module P/Invoke: the .NET PAL
    /// (<c>libSystem.Native</c> — the <c>SystemNative_*</c> POSIX wrappers implemented in
    /// <c>runtime/core/platform/posix/</c>), plain <c>libc</c> (always-linked platform
    /// symbols, e.g. macOS <c>clonefile</c>), and <c>libSystem.IO.Compression.Native</c>
    /// (the <c>CompressionNative_*</c> zlib wrappers implemented in
    /// <c>runtime/core/intrinsics/dn2cpp_zlib_native.cpp</c>). Everything else in a
    /// referenced module stays on the intrinsic/throw boundary (runtime-internal QCalls,
    /// Windows-only libraries). <c>libSystem.Security.Cryptography.Native.Apple</c> is
    /// the <c>AppleCryptoNative_*</c> digest/HMAC/random surface implemented portably in
    /// <c>runtime/core/intrinsics/dn2cpp_apple_crypto_digest.cpp</c> (+ the posix random
    /// PAL); the CoreFoundation framework path (Interop.AppleCrypto's cctor) resolves to
    /// the real framework via <c>-framework CoreFoundation</c>.
    /// <c>libSystem.Security.Cryptography.Native.OpenSsl</c> is the same surface for the
    /// Linux RID of the assembly — the <c>CryptoNative_*</c> digest/HMAC/RNG entry points
    /// implemented over the identical portable cores in
    /// <c>runtime/core/intrinsics/dn2cpp_openssl_crypto_digest.cpp</c>, so no real OpenSSL
    /// is linked either.</summary>
    /// <para>On Windows the real BCL's Interop layer P/Invokes the OS import
    /// libraries directly with already-marshalled blittable shapes (raw UTF-16
    /// <c>char*</c>/<c>IntPtr</c>/scalar args — it does its own <c>fixed</c>-pointer
    /// marshalling managed-side), so those calls direct-link like an app-module
    /// P/Invoke with no shim: <c>kernel32.dll</c> (file/directory bodies —
    /// CreateFile/ReadFile/FindFirstFile/…), <c>ntdll.dll</c> (the real directory
    /// enumeration primitive NtQueryDirectoryFile), <c>ole32.dll</c>
    /// (Guid.NewGuid -> CoCreateGuid), and <c>ws2_32.dll</c> (winsock — the
    /// <c>gethostname</c> primitive Dns.GetHostName bottoms out in, behind
    /// Interop.Winsock's WSAStartup init; the Windows counterpart of the POSIX
    /// PAL's <c>SystemNative_GetHostName</c>). Each string is the exact
    /// <c>ModuleRef</c> spelling — matched case-sensitively, so <c>BCrypt.dll</c> is
    /// not lowercase — as it appears in the win-x64 assembly that declares the
    /// P/Invoke: CoreLib for <c>kernel32</c>/<c>ntdll</c>/<c>ole32</c>/<c>BCrypt</c>,
    /// but <c>System.Net.NameResolution.dll</c> for <c>ws2_32.dll</c> (CoreLib names
    /// no winsock import; NameResolution is where <c>gethostname</c>/<c>WSAStartup</c>/
    /// <c>WSACleanup</c> are declared). Of these, <c>ws2_32</c>, <c>ntdll</c> and
    /// <c>BCrypt</c> are outside CMake's default Windows link set (which covers
    /// <c>kernel32</c>/<c>ole32</c>), so <c>runtime/CMakeLists.txt</c> links
    /// <c>ws2_32</c> explicitly on the WIN32 arm — without that the lowered call sites
    /// resolve here and then fail at C++ link time. Nothing reaches an <c>ntdll</c> /
    /// <c>BCrypt</c> call today; one that did would need the same explicit link.
    /// <c>advapi32.dll</c> is the registry surface, and it is reached for real:
    /// <c>TimeZoneInfo.GetUtcOffset</c> has a static edge to the LOCAL zone
    /// (<c>CachedData.get_Local -> GetLocalTimeZone -> FindIdFromTimeZoneInformation
    /// -> Internal.Win32.RegistryKey.GetSubKeyNames -> Advapi32.RegEnumKeyEx</c>), so a
    /// program that only ever touches <c>TimeZoneInfo.Utc</c> still drags the whole
    /// registry subtree in — the local-zone edge is a method-level reachability fact, not
    /// something the UTC intercept can cut. Unlike <c>ws2_32</c> it needs NO explicit link:
    /// <c>advapi32</c> is already in CMake's <c>CMAKE_C_STANDARD_LIBRARIES_INIT</c>, as the
    /// WIN32 arm of <c>runtime/CMakeLists.txt</c> states.
    /// <c>System.IO.Compression.Native</c> (no <c>lib</c> prefix — the win-x64
    /// <c>ModuleRef</c> spelling, unlike POSIX's
    /// <c>libSystem.IO.Compression.Native</c>; that prefix is the only difference
    /// between the two, and stripping it is <see cref="CanonicalNativeImplModule"/>'s
    /// whole job) is CoreLib's on neither platform — no CoreLib RID names a
    /// compression import at all. It is declared by the assemblies owning the codecs,
    /// and they are the same ones on both: <c>System.IO.Compression.dll</c> for the
    /// <c>CompressionNative_*</c> ZLib surface (its sole <c>ModuleRef</c>) and
    /// <c>System.IO.Compression.Brotli.dll</c> for the <c>Brotli*</c> one
    /// (<c>System.Net.WebSockets.dll</c> names it too). Same assemblies, same
    /// entry-point names, one spelling apart — verified against the real win-x64 and
    /// osx-arm64/linux-x64 metadata — so it resolves against the identical
    /// <c>dn2cpp_zlib_native.cpp</c> shim / vendored-brotli exports, no separate
    /// Windows implementation needed. Everything else in a
    /// referenced module — <c>System.Globalization.Native</c> (ICU),
    /// runtime-internal QCalls — stays on the intrinsic/throw boundary, same posture
    /// as POSIX. This fixed set names only modules the runtime/platform itself
    /// provides; the per-run opt-in path coexists with it —
    /// <see cref="IsOptedInPInvokeModule"/> (<c>--pinvoke-module</c>) admits a named
    /// module from any loaded assembly, which is how an external binding library's
    /// imports lower without its module name ever being hardcoded here.</para></summary>
    public static bool IsRuntimeProvidedPInvokeModule(string module) =>
        module is "libSystem.Native" or "libc" or "libSystem.IO.Compression.Native"
            // macOS process self-inspection. Interop.libproc's `proc_pidinfo` /
            // `proc_pidpath` / `proc_listallpids` are what ProcessManager.OSX answers
            // Process.ProcessName / StartTime / MainModule / GetProcessesByName from, and
            // they are ALWAYS-LINKED PLATFORM SYMBOLS despite the path: there is no
            // libproc.dylib file on disk, the symbols live in libSystem, and a plain
            // `clang -o x x.c` calling them links with no flag at all — the exact posture
            // "libc" above names. So this admission provides no implementation and needs no
            // shim TU; it just stops dn2cpp substituting a zero for a call the platform
            // would have answered. Bounding these instead gave a silently empty
            // ProcessName and a throwing StartTime while the SessionId behind the same
            // proc_pidinfo call was correct, so no per-row loudness verdict could fix it.
            // Darwin-only without a host test, because the ModuleRef is: only the OSX
            // flavour of System.Diagnostics.Process declares it.
            or "/usr/lib/libproc.dylib"
            // The Objective-C runtime. Interop.libobjc's objc_getClass / sel_getUid /
            // objc_msgSend are what Environment.OSVersion bottoms out in on macOS (through
            // NSProcessInfo.operatingSystemVersion), and they are ALWAYS-LINKED PLATFORM
            // SYMBOLS in the same sense libproc's are: libobjc.dylib is part of libSystem, a
            // plain `clang -o x x.c` calling objc_getClass links with no flag. So this
            // admission provides no implementation and needs no shim TU — it just stops
            // dn2cpp refusing a call the platform would have answered, which is the whole
            // difference between reporting the host's real OS version and reporting none
            // (Grpc.Net.Client reads Environment.OSVersion in its channel ctor).
            // Darwin-only without a host test, because the ModuleRef is: only the OSX
            // flavour of CoreLib declares it.
            or "/usr/lib/libobjc.dylib"
            or "libSystem.Security.Cryptography.Native.Apple"
            or "libSystem.Security.Cryptography.Native.OpenSsl"
            or "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation"
            or "kernel32.dll" or "ntdll.dll" or "ole32.dll" or "BCrypt.dll"
            or "ws2_32.dll" or "advapi32.dll"
            or "System.IO.Compression.Native"
            // The libcurl-backed HTTP transport (runtime/core/intrinsics/dn2cpp_http2_stream.cpp,
            // under DN2CPP_USE_CURL): the DnHttp shim's [DllImport("dn2cpp_http")] direct-links
            // to those dn2cpp_http2_call_* entry points, the same posture as the zlib shim.
            or "dn2cpp_http";

    // ── Conditional default references (the shim assemblies shipped with the CLI) ──
    /// <summary>The shim assemblies dn2cpp ships beside its CLI, each paired with the
    /// BCL assembly whose presence in the load set is the <b>only</b> justification for
    /// loading it. A shim is not a general-purpose library: it exists to replace or
    /// forward to a specific piece of BCL surface, so with that surface absent it has
    /// no work to do and adding it would only change the emitted C++
    /// (see <see cref="InjectDefaultRefs"/> on why the load set, not reachability, is
    /// what the output is a function of).
    /// <list type="bullet">
    /// <item><c>DnZlib</c> / <c>System.IO.Compression</c> — carries
    /// <c>[NativeImplementation]</c> adapters for that assembly's
    /// <c>CompressionNative_*</c> imports, which REPLACE the native codec with a
    /// transpiled managed one. Note the direction: without this shim those imports
    /// already resolve, to <c>runtime/core/intrinsics/dn2cpp_zlib_native.cpp</c> over
    /// the vendored zlib, and Deflate/GZip/Zip work.</item>
    /// <item><c>DnBrotli</c> / <c>System.IO.Compression.Brotli</c> — the same for the
    /// <c>Brotli*</c> imports, which absent the shim link straight to the vendored
    /// brotli's own exports (that assembly's <c>EntryPoint</c>s are the raw brotli API
    /// names, so there is no shim TU on the native side at all).</item>
    /// <item><c>DnHttp</c> / <c>System.Net.Http</c> — the transport the intercepted
    /// <c>SocketsHttpHandler.Send</c>/<c>SendAsync</c> forwards to
    /// (<see cref="HttpShimTarget"/>); it P/Invokes the runtime's <c>dn2cpp_http_*</c>
    /// surface in place of the socket subtree the intercept cut.</item>
    /// </list>
    /// <b>The first two swap a working path for another working path; only the third is
    /// load-bearing.</b> Injecting a codec shim moves compression from the vendored C
    /// library into transpiled C#, which is a choice about which implementation ships,
    /// not the difference between compressing and not — so the gates that assert the
    /// NATIVE codec still works have to opt out by name (<c>--no-default-ref</c>), and
    /// their round-trip diffs pass either way, which is exactly why that opt-out looks
    /// removable and is not. DnHttp is the asymmetric one: an <c>HttpClient</c> program
    /// whose handler was cut with no shim to forward to fails the transpile outright
    /// (MethodCompiler.CompileHttpShimBody, naming the remedy).</summary>
    private static readonly (string Shim, string Trigger)[] s_defaultRefs =
    {
        ("DnZlib",   "System.IO.Compression"),
        ("DnBrotli", "System.IO.Compression.Brotli"),
        ("DnHttp",   "System.Net.Http"),
    };

    /// <summary>What the conditional default-reference pass decided about one shim.
    /// Recorded per shim so a later diagnostic can say *why* a shim is missing rather
    /// than only that it is.</summary>
    internal enum DefaultRefOutcome
    {
        /// <summary>The BCL assembly this shim serves is not in the load set — the
        /// shim has nothing to do, so it was not loaded. Also what
        /// <see cref="DefaultRefStatusOf"/> reports when the mechanism itself is off
        /// (<see cref="TranspileOptions.DefaultRefDir"/> null): nothing was injected
        /// and nothing was suppressed, which is the same story from a caller's
        /// side.</summary>
        TriggerAbsent,
        /// <summary>Named by <c>--no-default-ref</c>.</summary>
        Suppressed,
        /// <summary>Already in the load set — an explicit <c>-r</c> (or the reference
        /// closure) got there first, and it wins: injecting a second copy is what the
        /// simple-name dedupe in <see cref="InjectDefaultRefs"/> exists to
        /// prevent.</summary>
        AlreadyLoaded,
        /// <summary>Wanted, but no such file beside the CLI — a CLI installed without
        /// its shims.</summary>
        NotFound,
        /// <summary>Loaded by this pass.</summary>
        Injected,
    }

    private readonly Dictionary<string, DefaultRefOutcome> _defaultRefStatus =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The conditional default-reference verdict for one shim simple name
    /// (<see cref="DefaultRefOutcome.TriggerAbsent"/> when the pass never ran). Read by
    /// the diagnostics that have to explain a missing shim to the person running the
    /// transpile — MethodCompiler's HTTP shim body, whose failure is otherwise a
    /// sentence about a shim the caller never asked for.</summary>
    internal DefaultRefOutcome DefaultRefStatusOf(string shim) =>
        _defaultRefStatus.TryGetValue(shim, out var outcome)
            ? outcome : DefaultRefOutcome.TriggerAbsent;

    /// <summary>Every shim's conditional default-reference verdict, in
    /// <see cref="s_defaultRefs"/> declaration order, for the
    /// <c>--hotupdate-base</c> sidecar to record.
    ///
    /// <para>The base build goes
    /// through <see cref="TranspileDriver.Run"/> and therefore injects; the patch
    /// converter builds its own <see cref="Compilation"/> with
    /// <see cref="TranspileOptions.DefaultRefDir"/> deliberately unset (see the
    /// comment at its <see cref="Create"/> call) and therefore does not. The two
    /// load sets are consequently asymmetric by exactly the injected shims, and
    /// nothing in the emitted image says which ones those were — so the converter
    /// cannot tell an expected asymmetry from a caller who passed the wrong
    /// <c>-r</c> set. Recording the verdicts makes the difference checkable.</para></summary>
    internal IEnumerable<(string Shim, DefaultRefOutcome Outcome)> DefaultRefRecord
    {
        get
        {
            foreach (var (shim, _) in s_defaultRefs)
                yield return (shim, DefaultRefStatusOf(shim));
        }
    }

    /// <summary>Whether a name is one of the shims <c>--no-default-ref</c> accepts.
    /// The CLIs cannot ask the table directly (<c>Dn2Cpp.Cli.Console</c> is not an
    /// InternalsVisibleTo friend), so validation funnels through
    /// <see cref="TranspileDriver.Run"/> and this predicate — an unknown name is a hard
    /// error there, never a silent no-op. Case-insensitive, matching the simple-name
    /// comparison the injection itself uses.</summary>
    internal static bool IsDefaultRefName(string name)
    {
        foreach (var (shim, _) in s_defaultRefs)
        {
            if (string.Equals(shim, name, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    /// <summary>The known <c>--no-default-ref</c> names, for the error message that
    /// rejects an unknown one — the diagnostic has to carry the whole set, since a
    /// typo is exactly the case it exists to catch.</summary>
    internal static string DefaultRefNameList
    {
        get
        {
            var names = new string[s_defaultRefs.Length];
            for (int i = 0; i < s_defaultRefs.Length; i++)
                names[i] = s_defaultRefs[i].Shim;
            return string.Join(", ", names);
        }
    }

    /// <summary>Loads each shim from <paramref name="dir"/> (the directory the CLI
    /// ships in) whose trigger assembly is present, in the table's declaration order so
    /// the resulting module order is deterministic. Returns whether anything was
    /// loaded. Off entirely when <see cref="TranspileOptions.DefaultRefDir"/> is null.
    ///
    /// <para><b>The dedupe key is the assembly SIMPLE NAME, OrdinalIgnoreCase — never a
    /// path.</b> The place where a collision is fatal
    /// (<see cref="RegisterNativeImpl"/>'s "duplicate native implementation") keys on
    /// assembly identity, not on which file the identity came from: load DnZlib twice
    /// and the transpile dies in Pass 2, before reachability has an opinion about
    /// whether anything even uses it. And a second copy is the *normal* case, not an
    /// exotic one — the gates pass <c>-r internal/DnZlib/bin/.../DnZlib.dll</c>, a
    /// different file from the one beside the CLI, so a path comparison would
    /// double-load every time. OrdinalIgnoreCase is what
    /// <see cref="LoadReferenceClosure"/>'s dedupe already uses and what ECMA-335 says
    /// about comparing assembly simple names; this introduces no new convention.</para>
    ///
    /// <para><b>Append-only.</b> Injection only appends modules; no existing
    /// <see cref="Module.Index"/> moves. That is what makes a run in which nothing is
    /// injected byte-identical to a build without this feature at all —
    /// <c>ClassInfo.CompareByOrder</c> (Model.cs) takes <c>Module.Index</c> as its
    /// first key, so a renumbering would reorder the entire emission.</para>
    ///
    /// <para><b>And the output is a function of the LOAD SET, not of reachability</b>
    /// — <c>CppEmitter.EmitAssemblyRegistry</c> walks every module in
    /// <see cref="Modules"/> without consulting reachability, so one unreached extra
    /// module still changes generated.cpp byte for byte (the same fact
    /// <c>gates/selfhost-emit.sh</c>'s header states for its fixpoint). Hence a shim is
    /// injected only on its trigger, and an explicit <c>-r</c> keeps its original,
    /// lower Module.Index while this pass records
    /// <see cref="DefaultRefOutcome.AlreadyLoaded"/> — so every gate that references a
    /// shim by hand keeps today's module order and today's output.</para></summary>
    private bool InjectDefaultRefs(string dir, IReadOnlyList<string> suppressed)
    {
        var loaded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var m in Modules)
            loaded.Add(m.AssemblyName);
        var off = new HashSet<string>(suppressed, StringComparer.OrdinalIgnoreCase);
        bool any = false;
        foreach (var (shim, trigger) in s_defaultRefs)
        {
            DefaultRefOutcome outcome;
            if (!loaded.Contains(trigger))
                outcome = DefaultRefOutcome.TriggerAbsent;
            else if (off.Contains(shim))
                outcome = DefaultRefOutcome.Suppressed;
            else if (loaded.Contains(shim))
                outcome = DefaultRefOutcome.AlreadyLoaded;
            else
            {
                string path = Path.Combine(dir, shim + ".dll");
                if (!File.Exists(path))
                {
                    outcome = DefaultRefOutcome.NotFound;
                }
                else
                {
                    var module = LoadModule(path);
                    // The whole "an explicit -r wins" decision above compared the FILE
                    // name against the loaded simple names, which is sound only while
                    // the two agree. A shim whose assembly name differs from its file
                    // name would slip past that test and be loaded a second time, and
                    // the failure would land in RegisterNativeImpl with no hint of where
                    // the duplicate came from — so say it here, at the shipping mistake.
                    if (!string.Equals(module.AssemblyName, shim, StringComparison.OrdinalIgnoreCase))
                        throw new NotSupportedException(
                            $"default reference {path}: assembly name is '{module.AssemblyName}', "
                            + $"expected '{shim}' — the shim's file name and assembly name must "
                            + "agree, or the already-loaded check cannot see an explicit -r of it");
                    loaded.Add(module.AssemblyName);
                    outcome = DefaultRefOutcome.Injected;
                    any = true;
                }
            }
            _defaultRefStatus[shim] = outcome;
        }
        return any;
    }

    /// <summary>Methods of intrinsic-mapped types whose real CoreLib bodies ARE
    /// transpiled, overriding the blanket intrinsic-type cut in <see cref="Reach"/>:
    /// String's interface-dispatch impls (the interface slot needs a real function
    /// to point at — there is no call site to intercept) and intrinsic call sites
    /// that explicitly delegate to the real body (<see cref="ReachStringMethod"/>).
    /// Everything else on an intrinsic type keeps the cut.</summary>
    private readonly HashSet<MethodInfo> _intrinsicTypeTranspiled = new();

    // ── System.Net.Http transport intercept (DnHttp shim) ─────────────────────────
    // The DnHttp shim assembly (DnHttp.DnHttpBackend) is where an intercepted
    // SocketsHttpHandler.Send/SendAsync forwards. It is a CONDITIONAL DEFAULT REFERENCE
    // (see s_defaultRefs / InjectDefaultRefs), not an optional -r: it ships beside the CLI
    // and is injected whenever System.Net.Http is in the load set, which is whenever this
    // lookup can matter. An explicit -r DnHttp.dll still wins (the simple-name dedupe
    // records AlreadyLoaded), and --no-default-ref DnHttp declines it. Resolved lazily and
    // once. When it is absent HttpShimTarget returns null, the transport IL is still cut,
    // and the emit route (MethodCompiler.CompileHttpShimBody) fails loudly naming the cause
    // AND the remedy — MissingHttpShimReason asks DefaultRefStatusOf which of those three
    // things happened — so a program that reaches the handler without the shim does not
    // silently drag the socket subtree back in.
    private MethodInfo? _httpBackendSend, _httpBackendSendAsync;
    private bool _httpBackendResolved;

    private void EnsureHttpBackend()
    {
        if (_httpBackendResolved)
            return;
        _httpBackendResolved = true;
        if (FindClassByFullName("DnHttp.DnHttpBackend") is not { } cls)
            return;
        // Reading .Methods decodes the shim class's member rows (names only — a signature
        // is decoded on its own read), a targeted decode, not a walk over Classes.
        foreach (var m in cls.Methods)
        {
            if (!m.IsStatic)
                continue;
            if (m.Name == "Send")
                _httpBackendSend = m;
            else if (m.Name == "SendAsync")
                _httpBackendSendAsync = m;
        }
    }

    /// <summary>The DnHttp shim's static entry an intercepted handler method forwards to
    /// (DnHttpBackend.Send / SendAsync), or null when the shim assembly is not referenced.
    /// Consulted by MethodCompiler.CompileHttpShimBody.</summary>
    internal MethodInfo? HttpShimTarget(string handlerMethodName)
    {
        EnsureHttpBackend();
        return handlerMethodName switch
        {
            "Send" => _httpBackendSend,
            "SendAsync" => _httpBackendSendAsync,
            _ => null,
        };
    }

    private void ReachHttpShimEdges(MethodInfo m)
    {
        // Only the transport shim names a managed symbol (DnHttpBackend.Send/SendAsync). The
        // client-certificate bodies name none — the guard refuses through a runtime helper,
        // which is not a MethodInfo, and the getter answers a constant — so they have no edge
        // to reach and nothing to resolve. Asked of the same pure predicate the cut and the
        // route ask, not of a second list.
        if (!CoreIntrinsics.IsInterceptedHttpHandlerMethod(m.DeclaringClass.FullName, m.Name))
            return;
        if (HttpShimTarget(m.Name) is { } shim)
            Reach(shim);
    }

    private void PopulateMethodImpls(ClassInfo cls, TypeDefinition td, Module module, GenericContext ctx)
    {
        var reader = module.Reader;
        foreach (var mih in td.GetMethodImplementations())
        {
            // A real CoreLib has MethodImpls pointing at members we don't model.
            // Skip those for reference assemblies; the app module stays strict.
            try
            {
                var mi = reader.GetMethodImplementation(mih);
                // A C# destructor's MethodImpl row (.override
                // System.Object::Finalize): Finalize dispatch is wired by name
                // (EffectiveFinalize -> the type-info finalize slot), never
                // through the explicit-impl map, and System.Object need not be
                // a loaded TypeDef (the GDExtension pipeline transpiles with no
                // BCL assembly) — skip the row instead of resolving strictly.
                if (mi.MethodDeclaration.Kind == HandleKind.MemberReference
                    && MemberRefParentTypeName(module, (MemberReferenceHandle)mi.MethodDeclaration) == "System.Object"
                    && reader.GetString(reader.GetMemberReference((MemberReferenceHandle)mi.MethodDeclaration).Name) == "Finalize")
                    continue;
                // An explicit implementation of an interface whose declaring
                // assembly was not referenced at transpile (a hot-update patch
                // implementing a base-image interface): the declaration resolves
                // to no loaded MethodInfo, so there is nothing to record here —
                // the patch converter reads the .override rows directly against
                // the base-image ABI manifest. Skip rather than abort.
                if (mi.MethodDeclaration.Kind == HandleKind.MemberReference
                    && reader.GetMemberReference((MemberReferenceHandle)mi.MethodDeclaration).Parent
                        is { Kind: HandleKind.TypeReference } declParent
                    && ResolveTypeRef(module, (TypeReferenceHandle)declParent) is null)
                    continue;
                // A generic method's .override row (e.g. a static abstract
                // Serialize<TBufferWriter> on a generic interface): open templates are
                // not modeled as MethodInfo, so the row cannot key this map — and needs
                // to reach nobody: ReachGvmImpl and ResolveStaticVirtualImpl's generic
                // arm resolve the implementation by template lookup, dotted explicit
                // names included (FindGenericMethodTemplate). Skip, don't resolve.
                bool declIsGenericMethod = mi.MethodDeclaration.Kind switch
                {
                    HandleKind.MemberReference => reader.GetBlobReader(
                            reader.GetMemberReference((MemberReferenceHandle)mi.MethodDeclaration).Signature)
                        .ReadSignatureHeader().IsGeneric,
                    HandleKind.MethodDefinition => reader.GetMethodDefinition(
                        (MethodDefinitionHandle)mi.MethodDeclaration).GetGenericParameters().Count > 0,
                    _ => false,
                };
                if (declIsGenericMethod)
                    continue;
                var bodyMethod = ResolveMethodHandle(module, mi.MethodBody, ctx, cls);
                var declMethod = ResolveMethodHandle(module, mi.MethodDeclaration, ctx);
                if (bodyMethod != null && declMethod != null)
                {
                    cls.ExplicitInterfaceImpls[declMethod] = bodyMethod;
                }
            }
            // A canonical-world specialization (placeholder type args) can carry
            // a .override row for a generic interface method that is never
            // instantiated in the placeholder world (generic virtual methods are
            // a per-instantiation fallback under sharing) — skip such rows there
            // too; every real instantiation still resolves its rows strictly.
            catch (NotSupportedException) when (cls.Module != AppModule
                || ContainsCanonPlaceholder(cls))
            {
            }
        }
    }

    /// <summary>
    /// Vtable construction; abstract methods own slots (so overrides match by
    /// signature) but emit null implementation entries.
    /// </summary>
    private void BuildVtables()
    {
        var slotOwners = new Dictionary<ClassInfo, List<MethodInfo>>();
        foreach (var cls in TopologicalByBase())
        {
            if (cls.IsInterface || cls.IsDelegate)
            {
                slotOwners[cls] = new List<MethodInfo>();
                continue;
            }
            // A specialization builds its own vtable when its members are decoded
            // (BuildVtableForSpecialization), and only then — this eager pass has nothing
            // to contribute to one: at this point in the build its method list is empty
            // and any vtable written here would be overwritten wholesale later. Skip by
            // kind rather than by reading .Methods to find that out — the read would
            // decode them, exactly the on-demand work the member tier defers.
            if (cls.GenericArity > 0)
                continue;
            var owners = new List<MethodInfo>();
            var impls = new List<MethodInfo?>();
            if (cls.BaseClass is not null)
            {
                // A non-generic class can derive from a closed generic whose base
                // is encoded as a TypeSpecification (e.g.
                // Int64Converter : JsonPrimitiveConverter<long>). That spec's
                // vtable is built lazily by BuildVtableForSpecialization, so this
                // eager pass must complete it first; inheriting an un-built base
                // would start the derived vtable from slot 0 and misalign every
                // slot, so a callvirt to a base slot would dispatch the wrong
                // override. Read the SlotOwners/Vtable *fields* (populated by both
                // builders) so the completed spec's real layout is used.
                if (cls.BaseClass.GenericArity > 0)
                    EnsureCompleted(cls.BaseClass);
                owners.AddRange(cls.BaseClass.SlotOwners);
                impls.AddRange(cls.BaseClass.Vtable);
            }

            var classOverrides = ClassOverrideDecls(cls);
            foreach (var m in cls.Methods)
            {
                if (!m.IsVirtual)
                    continue;
                int slot = ExplicitBaseSlot(cls, m, classOverrides, owners.Count);
                if (slot < 0 && !m.IsNewSlot)
                {
                    for (int i = 0; i < owners.Count; i++)
                    {
                        if (owners[i].Name == m.Name && owners[i].SigKey == m.SigKey)
                        {
                            slot = i;
                            break;
                        }
                    }
                }
                if (slot < 0)
                {
                    slot = owners.Count;
                    owners.Add(m);
                    impls.Add(null);
                }
                owners[slot] = m;
                impls[slot] = m.IsAbstract ? null : m;
                m.VtableSlot = slot;
            }

            slotOwners[cls] = owners;
            cls.SlotOwners = owners;
            cls.Vtable.Clear();
            cls.Vtable.AddRange(impls);
        }
    }

    private IEnumerable<ClassInfo> TopologicalByBase()
    {
        var visited = new HashSet<ClassInfo>();
        var result = new List<ClassInfo>();
        void Visit(ClassInfo c)
        {
            if (!visited.Add(c))
                return;
            if (c.BaseClass is not null)
                Visit(c.BaseClass);
            result.Add(c);
        }
        foreach (var c in Classes)
            Visit(c);
        return result;
    }
}
