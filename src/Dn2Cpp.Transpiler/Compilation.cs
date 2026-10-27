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
    Nested = 16,
    ByRefLike = 32,
    HiddenEnclosing = 64,
    Delegate = 128,
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
    public required string Path;
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
    public Dictionary<(string, int), ThrowHelperResources.Sink>? ThrowHelperSinks;
    public Dictionary<string, Dictionary<int, string>>? ExceptionEnums;

    /// <summary>One reflection summary per scanned method definition; every instantiation
    /// of the definition shares it, since a summary resolves nothing.</summary>
    public readonly Dictionary<MethodDefinitionHandle, ReflectionSummary> ReflectionSummaries = new();
    public ReflectionSummarizer? ReflectionSummarizer;
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
    /// The adapter prefixes cannot collide with a plain method name because method
    /// symbols end their readable stem with <c>_m</c> plus a numeric identity.</summary>
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
    public IReadOnlyList<string> OriginalAssemblyPaths { get; private set; } = Array.Empty<string>();
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

    /// <summary>The <see cref="DelegateInvokerUses"/> the shipped pass records. A shipped
    /// body calls each one's invoker, so every struct that invoker's prototype spells has to
    /// be declared even for a delegate type nothing constructs.</summary>
    public HashSet<ClassInfo> ShippedDelegateInvokerUses { get; } = new();

    /// <summary>Records a mouth that spells <c>dginvoke_&lt;CppName&gt;</c>.</summary>
    internal void NoteDelegateInvokerUse(ClassInfo cls)
    {
        DelegateInvokerUses.Add(cls);
        if (Phase == EmitPhase.Emission)
            ShippedDelegateInvokerUses.Add(cls);
    }

    /// <summary>Canonical shared generics (opt-in via <c>--shared-generics</c>):
    /// group generic instantiations whose C++ layout coincides under a canonical
    /// owner instantiation (see <see cref="CanonicalGenerics"/>). In this stage
    /// the grouping is pure model linkage (<see cref="ClassInfo.SharedOwner"/> /
    /// <see cref="ClassInfo.SharedUsers"/>) — emission is unchanged until shared
    /// body emission lands.</summary>
    public bool SharedGenericsEnabled { get; }

    public bool CompressMetadata { get; }
    public bool ObfuscationEnabled { get; }
    internal HashSet<MethodInfo> ObfuscationMethods { get; } = new();

    private void NoteObfuscationMethod(MethodInfo method)
    {
        if (ObfuscationEnabled && method.HasObfuscateAttribute)
            ObfuscationMethods.Add(method);
    }
    /// <summary>The CLI's requested body-compilation worker limit. Zero means the
    /// host processor count; retained separately so timing diagnostics can say
    /// whether the effective limit was automatic.</summary>
    internal int BodyCompileJobsRequested { get; }
    /// <summary>The resolved positive body-compilation worker limit.</summary>
    internal int BodyCompileJobs { get; }

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

    /// <summary>The specializations whose members <see cref="CompleteMembers"/> decoded,
    /// in that order.</summary>
    private readonly List<ClassInfo> _membersCompletedOrder = new();

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
    private readonly bool _cutMethodsValidated;
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

    /// <summary>A <c>--hotupdate-base</c> build, whose generic virtual rows a patch can
    /// call virtually (<see cref="ReachPatchCallableGvms"/>).</summary>
    private readonly bool _hotUpdateBase;

    /// <summary>The single construction path: every option is set on the record
    /// before the instance exists, so nothing can observe a half-configured
    /// Compilation — the "must be set before Build()" ordering in the constructor
    /// is structural, not disciplinary. <paramref name="assemblyPaths"/> is the
    /// load set the caller assembled (input + references + auto-referenced shims),
    /// not <see cref="TranspileOptions.Input"/>/References verbatim.</summary>
    public static Compilation Create(IReadOnlyList<string> assemblyPaths, TranspileOptions options) =>
        new(assemblyPaths, options);

    /// <summary>Set for <see cref="TranspileOptions.IsaSurfaceDump"/>: Build ends after the
    /// vtable pass, with no reachability fixpoint run.</summary>
    internal bool StopBeforeReachability { get; }

    private Compilation(IReadOnlyList<string> assemblyPaths, TranspileOptions options)
    {
        _backend = options.Backend;
        _strippingLevel = options.StrippingLevel;
        if (_strippingLevel is not (StrippingLevel.High or StrippingLevel.Medium))
            throw new NotSupportedException("Unknown stripping level: " + _strippingLevel);
        StopBeforeReachability = options.IsaSurfaceDump is not null;
        // Must be set before Build(): the reachability drain computes the
        // typeof-equality branch folds, and a hot-update world must not prune (see the field).
        // A --hotupdate-base build IS a hot-update world, hence the OR.
        _hotUpdatePosture = options.HotUpdatePosture || options.HotupdateBase;
        _hotUpdateBase = options.HotupdateBase;
        _noAdoptAsync = options.NoAdoptAsync ?? Array.Empty<string>();
        _cliCutMethods = (options.CutMethods ?? Array.Empty<string>()).Select(ParseCutSpec).ToHashSet();
        _trimReflection = options.TrimReflection;
        _reflectionRoots = new HashSet<string>(options.ReflectionRoots ?? Array.Empty<string>(), StringComparer.Ordinal);
        CompressMetadata = options.CompressMetadata;
        ConfigureReflectionMetadata(options.ReflectionMetadataFormats);
        _projectRoots = options.ProjectRoots;
        _linkXmlFiles = options.LinkXmlFiles;
        _cutMethodsValidated = options.CutMethodsValidated;
        _linkFeatures = new HashSet<string>(options.LinkFeatures, StringComparer.Ordinal);
        _noManifestResources = new HashSet<string>(
            options.NoManifestResources ?? Array.Empty<string>(), StringComparer.Ordinal);
        _bclResourceDirectory = options.BclResourceDirectory;
        _manifestResourceRoots = new HashSet<string>(
            options.ManifestResourceRoots ?? Array.Empty<string>(), StringComparer.Ordinal);
        _directPInvokes = new HashSet<string>(
            options.DirectPInvokes ?? Array.Empty<string>(), StringComparer.Ordinal);
        // Snapshot before Build(): the reachability drain consults IsBoundedMethod.
        _backendBoundedMethods = options.Backend is null
            ? new() : new(options.Backend.AdditionalBoundedMethods);
        _genericRoots = options.HotupdateRefs;
        SharedGenericsEnabled = options.SharedGenerics;
        ObfuscationEnabled = options.Obfuscate;
        BodyCompileJobsRequested = options.MaxDegreeOfParallelism;
        int processorCount = System.Math.Max(1, Environment.ProcessorCount);
        BodyCompileJobs = options.MaxDegreeOfParallelism == 0
            ? processorCount
            : System.Math.Min(options.MaxDegreeOfParallelism, processorCount);
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
            + $" fld {ModelCensus.FieldTypesDecoded} gdepth {MaxGenericArgDepth}"
            + $" virtual-pairs {_virtualCandidatePairs} boxed-visits {_boxedEqualityVisits}"
            + $" backend-attr-replays {_backendAttributeReplayCount}"
            + $" preserve-passes {_conditionalPreservePasses} preserve-visits {_conditionalPreserveVisits}"
            + $" preserve-checks {_conditionalPreserveChecks} preserve-activations {_conditionalPreserveActivations}"
            + $" array-search-value-types {_finiteArraySearchElements.Count}"
            + $" array-search-unknown {(_unknownArraySearchReceiver ? 1 : 0)}"
            + $" shared-class-visits {_sharedClassDemandVisits} shared-method-visits {_sharedMethodDemandVisits}"
            + $" virtual-index-fallbacks {_virtualIndexFallbacks}";
        var loadSet = options.ResolvedLoadSet ?? AssemblyLoadSet.Resolve(assemblyPaths, options);
        OriginalAssemblyPaths = loadSet.Paths.ToArray();
        foreach (var status in loadSet.DefaultRefStatus)
            _defaultRefStatus.Add(status.Key, status.Value);
        foreach (string path in options.ResolvedLoadSet is not null
                     ? assemblyPaths : loadSet.Paths)
            LoadModule(path);
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
        if (_cutMethodsValidated)
            return;
        foreach (var (type, method) in _cliCutMethods)
        {
            var cls = FindClassByFullName(type);
            if (cls is null)
                throw new NotSupportedException(
                    $"--cut {type}::{method}: no loaded assembly declares type {type} "
                    + $"(loaded: {string.Join(", ", Modules.Select(m => m.AssemblyName))})");
            if (!cls.EnsureMembers().Methods.Any(m => m.Name == method))
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
            Path = path,
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

    /// <summary><paramref name="cls"/> with its shape tier final: a specialization
    /// learns its base, and so whether it is a value type, only with its shape.</summary>
    private ClassInfo EnsureShape(ClassInfo cls)
    {
        if (!cls.ShapeReady)
            CompleteShape(cls);
        return cls;
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
    public static TypeDesc? WellKnownPrimitive(string fullName) =>
        CoreIntrinsicTypeMaps.WellKnownPrimitiveCode(fullName) is { } code
            ? TypeDesc.MakePrimitive(code) : null;

    /// <summary>Reference types named by an IL type token (castclass/isinst/
    /// ldtoken/…) or a bounded call's receiver, but never allocated or otherwise
    /// reached. They occur in branches the BCL leaves statically reachable but
    /// that fold away at runtime (e.g. Dictionary's string-comparer path for a
    /// value-type key). They are emitted opaquely so dead-branch C++ still
    /// compiles, never as live layouts.</summary>
    public HashSet<ClassInfo> ReferencedTypes { get; } = new();

    // Task's default-result cache observes managed storage between fields too.
    internal HashSet<ClassInfo> TaskResultCacheLayouts { get; } = new();

    public void NoteReferencedType(ClassInfo cls)
    {
        if (ReferencedTypes.Add(cls))
            NoteConditionalPreservationUse(cls);
        _identityOnlyReferences.Remove(cls);
    }

    /// <summary>The <see cref="ReferencedTypes"/> members only an identity handle names:
    /// their type-info must exist, but naming one is no sign the program reflects over
    /// it, so they seed no reflection keep. Any ordinary note removes a class, which makes
    /// the final set independent of note order.</summary>
    private readonly HashSet<ClassInfo> _identityOnlyReferences = new();

    internal bool IsIdentityOnlyReference(ClassInfo cls) => _identityOnlyReferences.Contains(cls);

    internal readonly Dictionary<string, CppEmitter.BindingSignature> ReflectionTypeTokens = new(StringComparer.Ordinal);

    internal string NoteReflectionTypeToken(MethodInfo method, int token, CppEmitter.BindingSignature signature,
        string suffix = "")
    {
        string symbol = "signature_type_" + method.CppName + "_" + token.ToString(System.Globalization.CultureInfo.InvariantCulture) + suffix;
        ReflectionTypeTokens[symbol] = signature;
        NoteSignatureArrayInterfaces(signature);
        return symbol;
    }

    private void NoteSignatureArrayInterfaces(CppEmitter.BindingSignature signature)
    {
        if (signature.Kind is 6 or 7)
        {
            NoteMdArrayUse();
            // Jagged elements are references and can use the shared SZ interface map.
            if (signature.Children[0].Kind is 6 or 7)
                NoteArrayElementType(TypeDesc.MakePrimitive(PrimitiveTypeCode.Object));
        }
        foreach (var child in signature.Children)
            NoteSignatureArrayInterfaces(child);
    }

    /// <summary>Notes every concrete handle that a constructed type's runtime identity
    /// points through, including generic arguments and array elements. Without
    /// <paramref name="keepSeed"/> the classes are referenced for their type-info
    /// alone.</summary>
    internal void NoteTypeIdentityClosure(TypeDesc type, bool keepSeed = true)
    {
        switch (type.Kind)
        {
            case TypeKind.Class:
                if (keepSeed)
                    NoteReferencedType(type.Class!);
                else if (ReferencedTypes.Add(type.Class!))
                {
                    _identityOnlyReferences.Add(type.Class!);
                    NoteConditionalPreservationUse(type.Class!);
                }
                foreach (var arg in type.Class!.Context.TypeArgs)
                    NoteTypeIdentityClosure(arg, keepSeed);
                break;
            case TypeKind.SZArray:
                NoteArrayElementType(type.Element!);
                NoteTypeIdentityClosure(type.Element!, keepSeed);
                break;
            case TypeKind.MDArray:
                NoteMdArrayType(type);
                NoteTypeIdentityClosure(type.Element!, keepSeed);
                break;
        }
    }

    /// <summary>Delegate method identities named by emitted bodies, by symbol. Planning
    /// bodies never record: their text is discarded, so only the shipped pass may name
    /// what the metadata emitter defines.</summary>
    private readonly Dictionary<string, (MethodInfo Method, bool Virtual)> _delegateIdentities =
        new(StringComparer.Ordinal);
    private readonly HashSet<string> _delegateIdentityTargets = new(StringComparer.Ordinal);
    private bool _delegateIdentitiesFrozen;
    private bool _delegateMethodRead;
    internal MethodInfo? DelegateCloneBinding { get; private set; }
    private readonly HashSet<ClassInfo> _delegateCloneBindingOwners = new();
    private readonly HashSet<MethodInfo> _delegateCloneBindingDeclarations = new();
    private readonly HashSet<ClassInfo> _runtimeDelegateCloneReceivers = new();
    internal IEnumerable<ClassInfo> DelegateCloneReceiverTypes =>
        _allocatedRefTypes.Concat(_runtimeDelegateCloneReceivers).Distinct();
    private readonly HashSet<ClassInfo> _reflectionDelegateCloneReceivers = new();
    internal bool HasDelegateCloneBindings => _delegateCloneBindingOwners.Count != 0;

    internal void NoteReflectionDelegateCloneReceiver(ClassInfo receiver) =>
        _reflectionDelegateCloneReceivers.Add(receiver);

    internal bool IsDelegateCloneBindingReceiver(ClassInfo receiver)
    {
        foreach (var owner in _delegateCloneBindingOwners)
            if (DerivesFromOrIs(receiver, owner))
                return true;
        return false;
    }

    internal void NoteDelegateCloneBinding(MethodInfo method)
    {
        // Function-token resolution keeps the callable a call-edge cut omits.
        _delegateCloneBindingOwners.Add(method.DeclaringClass);
        _delegateCloneBindingDeclarations.Add(method);
        ReachUsedVirtual(method);
        DrainReachability();
        if (Phase != EmitPhase.Emission)
            return;
        int slot = method.VtableSlot;
        for (var owner = method.DeclaringClass; owner is not null; owner = owner.BaseClass)
        {
            if (!owner.MembersReady)
                throw new InvalidOperationException("Delegate.Clone binding needs completed class slots");
            if (slot >= 0 && slot < owner.SlotOwners.Count
                && CoreIntrinsics.BrDelegateClone.Matches(owner.SlotOwners[slot].DeclaringClass.FullName,
                    owner.SlotOwners[slot].Name))
            {
                var declaration = owner.SlotOwners[slot];
                if (DelegateCloneBinding is not null && DelegateCloneBinding != declaration)
                    throw new InvalidOperationException("Delegate.Clone bindings disagree on the base class slot");
                DelegateCloneBinding = declaration;
                return;
            }
        }
        throw new InvalidOperationException("Delegate.Clone binding has no base class slot");
    }

    /// <summary>The identity symbol a delegate over <paramref name="m"/> points at; one
    /// definition per (method, binding) whichever body names it.</summary>
    internal string NoteDelegateIdentity(MethodInfo m, bool isVirtual)
    {
        string sym = (isVirtual ? "dgidv_" : "dgid_") + m.CppName;
        if (Phase != EmitPhase.Emission)
            return sym;
        if (_delegateIdentitiesFrozen)
            throw new InvalidOperationException(
                $"emit protocol: delegate identity {sym} named after the identity table was emitted");
        if (!_delegateIdentities.ContainsKey(sym))
        {
            _delegateIdentities.Add(sym, (m, isVirtual));
            _delegateIdentityTargets.Add(m.CppName);
        }
        return sym;
    }

    /// <summary>Records that a shipped body reads <c>Delegate.Method</c>: only then do
    /// its known selected declarations need their method rows.</summary>
    internal void NoteDelegateMethodRead()
    {
        if (Phase == EmitPhase.Emission)
            _delegateMethodRead = true;
    }

    /// <summary>Whether a shipped body reads <c>Delegate.Method</c>.</summary>
    internal bool ReadsDelegateMethod => _delegateMethodRead;

    /// <summary>Whether <paramref name="m"/>'s method row must survive the unreached-row
    /// trim: <c>Delegate.Method</c> answers a delegate's declaration from it even when
    /// every receiver overrides the body.</summary>
    private bool KeepsDelegateTargetRow(MethodInfo m) =>
        _delegateMethodRead && _delegateIdentityTargets.Contains(m.CppName);

    /// <summary>The recorded identities in symbol order; no body may name one
    /// afterwards.</summary>
    internal List<(string Symbol, MethodInfo Method, bool Virtual)> FreezeDelegateIdentities()
    {
        _delegateIdentitiesFrozen = true;
        var symbols = new List<string>(_delegateIdentities.Keys);
        symbols.Sort(StringComparer.Ordinal);
        var result = new List<(string, MethodInfo, bool)>(symbols.Count);
        foreach (string sym in symbols)
        {
            var (m, isVirtual) = _delegateIdentities[sym];
            result.Add((sym, m, isVirtual));
        }
        return result;
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
    /// throws (see <c>DN2CPP_TF_METADATA_STRIPPED</c>) rather than answering empty.
    /// An unpublished owner belongs to no finalized keep-set.</summary>
    public bool KeepsReflectionMetadata(ClassInfo? cls) =>
        _reflectionKeep is null || cls is not null && _reflectionKeep.Contains(cls);

    /// <summary>Whether <c>--trim-reflection</c> strips the member metadata of the classes
    /// <see cref="KeepsReflectionMetadata"/> does not keep.</summary>
    public bool TrimsReflection => _trimReflection;

    /// <summary>Materializes the reflection keep-set. Called by the emitter immediately
    /// before <c>EmitTypeInfos</c>, and it must not run earlier: <c>ComputeEmitted</c>'s
    /// delegate-invoker pass still appends to <see cref="ReferencedTypes"/>, which is a
    /// seed. Selected delegate rows survive even when metadata trimming is off.</summary>
    public void ComputeReflectionKeepSet()
    {
        var delegateMethods = _delegateMethodRead ? GetDelegateSelectedMethods().ToList() : null;
        if (delegateMethods is not null)
            foreach (var method in delegateMethods)
                _delegateIdentityTargets.Add(method.CppName);
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
            else if (ReferencedTypes.Contains(cls) && !_identityOnlyReferences.Contains(cls))
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
        // A virtual binding names its selected declaration, which can belong to
        // a receiver override or derived-interface default absent from the IL.
        if (delegateMethods is not null)
            foreach (var m in delegateMethods)
                keep.Add(m.DeclaringClass);
        // A recognized lookup, enumeration, attribute-data read or type identity names the
        // owner whose members it reads.
        foreach (var cls in _reflectionMetadataOwners)
            keep.Add(cls);
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
        // Runtime instances copy their member tables from the typeof-named definition's template.
        foreach (var (defName, template, _) in EligibleRuntimeTemplates)
            if (TypeofOpenGenericDefs.ContainsKey(defName))
                keep.Add(template);
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
    private readonly string? _bclResourceDirectory;

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
        if (cls is not null && ForceEmittedClasses.Add(cls))
            NoteConditionalPreservationUse(cls);
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

    /// <summary>Direct P/Invoke methods whose call site actually lowered to a native call.
    /// A P/Invoke method has no IL body, so it never enters the normal
    /// <see cref="Reachable"/> set (Reach cuts Rva==0); the emitter walks this set
    /// instead to declare each distinct entry-point symbol <c>extern "C"</c>.</summary>
    public HashSet<MethodInfo> PInvokeCalls { get; } = new();

    /// <summary>Program-wide lazy P/Invoke resolution cells. Direct IL calls share a
    /// MethodDef cell; the address-taken forwarder has a second cell, matching CoreCLR's
    /// distinct delegate/function-pointer stub while still sharing across all users of
    /// each route.</summary>
    public HashSet<string> LazyPInvokeCaches { get; } = new(StringComparer.Ordinal);

    public string NoteLazyPInvokeCall(MethodInfo method, bool ftnWrapper)
    {
        string name = LazyPInvokeCacheName(method, ftnWrapper);
        LazyPInvokeCaches.Add(name);
        return name;
    }

    internal void ResetLazyPInvokeCallsForEmission() => LazyPInvokeCaches.Clear();

    private static string LazyPInvokeCacheName(MethodInfo method, bool ftnWrapper) =>
        "dn2cpp_pinvoke_cache_" + method.CppName + (ftnWrapper ? "_ftn" : "");

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
    /// themselves emit unconditionally (a null row) so the runtime always links.
    /// Under shared generics the planning pass lowers every reachable body before any
    /// body renders, and the rendered rgctx prologues depend on this flag
    /// (<see cref="NullReceiverRgctx"/>), so a first binding lowered by the emission pass
    /// is a broken invariant.</summary>
    public bool NeedsReflectionDelegateBind
    {
        get => _needsReflectionDelegateBind;
        set
        {
            if (value && !NullReceiverRgctx && SharedGenericsEnabled && Phase == EmitPhase.Emission)
                throw new InvalidOperationException(
                    "emit protocol: a CreateDelegate binding was first lowered by the emission pass, "
                    + "after the shared bodies' rgctx prologues were rendered");
            _needsReflectionDelegateBind = value;
        }
    }

    private bool _needsReflectionDelegateBind;

    /// <summary>Whether a shared instance body's rgctx prologue accepts a null receiver
    /// and then reads the generic context a null-bound CreateDelegate binding supplies
    /// (<c>dn2cpp_null_receiver_rgctx</c>). Only such a binding enters a shared instance
    /// body without a receiver: one this image lowers, or one a hot-update patch makes
    /// through this base image's runtime. Every other image keeps the plain prologue.</summary>
    internal bool NullReceiverRgctx => _needsReflectionDelegateBind || _hotUpdateBase;

    /// <summary>Whether a Delegate.DynamicInvoke was lowered. It binds each emitted
    /// delegate type's Invoke row to the type's multicast invoker, as MethodInfo.Invoke use
    /// (<see cref="ReflectionInvokeUsed"/>) does, since .NET's DynamicInvoke calls the
    /// delegate through that row.</summary>
    public bool NeedsDelegateInvokeRows { get; set; }

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
    // (namespace, name) -> the TypeDefs that declare it. Built once; a real CoreLib has thousands
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
    /// located through the type index (app module wins name-only ties). Both facts ride
    /// the ONE lookup because both are facts about that one TypeDef;
    /// the degrade when no loaded module declares the name is
    /// <see cref="GenericDefKind.Unknown"/> with no bracket group.
    /// <para>The by-NAME counterpart of <see cref="ResolveOpenGenericDefKind"/> /
    /// <see cref="GenericParamNames(Module, TypeDefinitionHandle)"/>, and the arm that makes
    /// a typeof of an INTRINSIC definition answer: dn2cpp mints neither ClassInfo nor
    /// Template for one, so <c>typeof(Vector128&lt;&gt;)</c> decodes to an External name.
    /// Nested names are resolved by their entire declaring chain.</para></summary>
    /// <summary>The CLR backtick full name of <paramref name="t"/> when it is an
    /// OPEN generic definition in one of the three shapes a <c>typeof(D&lt;&gt;)</c>
    /// ldtoken decodes to (the <c>OpenGenericDefTypeInfoExpr</c> shapes), else
    /// null. Nested definitions retain their entire declaring chain.</summary>
    internal static string? OpenGenericDefBacktickNameOf(TypeDesc t) => t switch
    {
        { Kind: TypeKind.External, ExternalName: { } en }
            when en.Contains('`') => en,
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
        int nested = defName.LastIndexOf('+');
        if (nested >= 0)
        {
            var nestedKey = ("", defName[(nested + 1)..]);
            if (TypeIndex().TryGetValue(nestedKey, out var nestedCandidates))
                foreach (var (module, handle) in nestedCandidates)
                    if (MethodCompiler.OpenDefBacktickName(module, handle) == defName)
                        return (module, handle);
            return null;
        }
        int cut = defName.LastIndexOf('.');
        var key = cut < 0 ? ("", defName) : (defName[..cut], defName[(cut + 1)..]);
        return TypeIndex().TryGetValue(key, out var cands) && cands.Count > 0 ? cands[0] : null;
    }

    internal static bool HasRuntimeObjectBase(ClassInfo cls)
    {
        var handle = cls.Module.Reader.GetTypeDefinition(cls.Handle).BaseType;
        return handle.Kind == HandleKind.TypeReference
            && IsRuntimeObjectTypeReference(cls.Module, (TypeReferenceHandle)handle);
    }

    internal static bool IsRuntimeObjectTypeReference(Module module, TypeReferenceHandle handle)
    {
        var reader = module.Reader;
        var type = reader.GetTypeReference(handle);
        if (!reader.StringComparer.Equals(type.Namespace, "System") || !reader.StringComparer.Equals(type.Name, "Object")
            || type.ResolutionScope.Kind != HandleKind.AssemblyReference)
            return false;
        var reference = reader.GetAssemblyReference((AssemblyReferenceHandle)type.ResolutionScope);
        if ((reference.Flags & AssemblyFlags.PublicKey) != 0)
            return false;
        var token = reader.GetBlobReader(reference.PublicKeyOrToken);
        if (token.Length != 8)
            return false;
        ulong expected = reader.StringComparer.Equals(reference.Name, "System.Runtime", true) ? 0x3a0ad5117f5f3fb0UL
            : reader.StringComparer.Equals(reference.Name, "netstandard", true) ? 0x51dd2dcdff137bccUL
            : reader.StringComparer.Equals(reference.Name, "mscorlib", true) ? 0x89e03419565c7ab7UL
            : reader.StringComparer.Equals(reference.Name, "System.Private.CoreLib", true) ? 0x8e79a7bed785ec7cUL : 0;
        return expected != 0 && token.ReadUInt64() == expected;
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

        if (tr.ResolutionScope.Kind == HandleKind.AssemblyReference)
        {
            string assembly = from.Reader.GetString(from.Reader.GetAssemblyReference(
                (AssemblyReferenceHandle)tr.ResolutionScope).Name);
            return ResolveAssemblyType(assembly, ns, name, candidates, from,
                (AssemblyReferenceHandle)tr.ResolutionScope);
        }
        if (tr.ResolutionScope.Kind == HandleKind.ModuleDefinition || tr.ResolutionScope.IsNil)
            foreach (var (target, tdh) in candidates)
                if (target == from && target.Reader.GetTypeDefinition(tdh).GetDeclaringType().IsNil)
                    return MakeTypeDescFor(target, tdh);
        return null;
    }

    private Dictionary<(string Assembly, string Namespace, string Name), (Module Module, AssemblyReferenceHandle Scope)>? _typeForwarders;
    private Dictionary<string, string>? _loadedAssemblyNames;

    private TypeDesc? ResolveAssemblyType(string assembly, string ns, string name,
        List<(Module, TypeDefinitionHandle)> candidates, Module scopeModule, AssemblyReferenceHandle scope)
    {
        if (_typeForwarders is null)
        {
            _typeForwarders = new();
            _loadedAssemblyNames = new(StringComparer.OrdinalIgnoreCase);
            foreach (var module in Modules)
            {
                _loadedAssemblyNames.TryAdd(module.AssemblyName, module.AssemblyName);
                foreach (var handle in module.Reader.ExportedTypes)
                {
                    var type = module.Reader.GetExportedType(handle);
                    if (type.Implementation.Kind != HandleKind.AssemblyReference)
                        continue;
                    _typeForwarders.TryAdd((module.AssemblyName, module.Reader.GetString(type.Namespace),
                        module.Reader.GetString(type.Name)), (module, (AssemblyReferenceHandle)type.Implementation));
                }
            }
        }
        for (int hops = 0; hops <= Modules.Count; hops++)
        {
            if (_loadedAssemblyNames!.TryGetValue(assembly, out var canonical))
                assembly = canonical;
            foreach (var (module, handle) in candidates)
                if (module.AssemblyName.Equals(assembly, StringComparison.OrdinalIgnoreCase)
                    && module.Reader.GetTypeDefinition(handle).GetDeclaringType().IsNil)
                    return MakeTypeDescFor(module, handle);
            if (_typeForwarders.TryGetValue((assembly, ns, name), out var forwarded))
            {
                scopeModule = forwarded.Module;
                scope = forwarded.Scope;
                assembly = scopeModule.Reader.GetString(scopeModule.Reader.GetAssemblyReference(scope).Name);
                continue;
            }
            // Signed facades can be omitted when their implementation is loaded.
            if (!_loadedAssemblyNames.ContainsKey(assembly)
                && IsSignedStandardFacadeScope(scopeModule, scope, assembly))
            {
                string target = IsPrivateUriFacadeType(scopeModule, scope, assembly, ns, name)
                    ? "System.Private.Uri" : "System.Private.CoreLib";
                foreach (var (module, handle) in candidates)
                    if (module.AssemblyName == target
                        && module.Reader.GetTypeDefinition(handle).GetDeclaringType().IsNil)
                        return MakeTypeDescFor(module, handle);
            }
            return null;
        }
        return null;
    }

    private static bool IsPrivateUriFacadeType(Module module, AssemblyReferenceHandle scope,
        string assembly, string ns, string name)
    {
        bool runtime = assembly.Equals("System.Runtime", StringComparison.OrdinalIgnoreCase);
        if (ns != "System" || (!runtime
            && !assembly.Equals("netstandard", StringComparison.OrdinalIgnoreCase)))
            return false;
        var reference = module.Reader.GetAssemblyReference(scope);
        var token = module.Reader.GetBlobReader(reference.PublicKeyOrToken);
        if (token.Length != 8 || token.ReadUInt64() != (runtime
            ? 0x3a0ad5117f5f3fb0UL : 0x51dd2dcdff137bccUL))
            return false;
        // These public forwarding rows share Private.Uri; nested rows resolve through their owner.
        return name is "FileStyleUriParser" or "FtpStyleUriParser" or "GenericUriParser"
            or "GenericUriParserOptions" or "GopherStyleUriParser" or "HttpStyleUriParser"
            or "LdapStyleUriParser" or "NetPipeStyleUriParser" or "NetTcpStyleUriParser"
            or "NewsStyleUriParser" or "Uri" or "UriBuilder" or "UriComponents"
            or "UriFormat" or "UriFormatException" or "UriHostNameType" or "UriKind"
            or "UriParser" or "UriPartial" || (runtime && name == "UriCreationOptions");
    }

    private static bool IsSignedStandardFacadeScope(Module module, AssemblyReferenceHandle scope, string assembly)
    {
        if (!assembly.StartsWith("System.", StringComparison.OrdinalIgnoreCase)
            && !assembly.Equals("netstandard", StringComparison.OrdinalIgnoreCase)
            && !assembly.Equals("mscorlib", StringComparison.OrdinalIgnoreCase))
            return false;
        var reference = module.Reader.GetAssemblyReference(scope);
        var token = module.Reader.GetBlobReader(reference.PublicKeyOrToken);
        if (token.Length != 8)
            return false;
        return token.ReadUInt64() is 0x3a0ad5117f5f3fb0UL or 0x51dd2dcdff137bccUL
            or 0x8e79a7bed785ec7cUL or 0x89e03419565c7ab7UL;
    }

    internal TypeDesc ExternalTypeRef(Module module, TypeReferenceHandle handle)
    {
        var type = module.Reader.GetTypeReference(handle);
        string name = module.Reader.GetString(type.Name);
        string ns = module.Reader.GetString(type.Namespace);
        string full = ns.Length == 0 ? name : ns + "." + name;
        string assembly = module.AssemblyName;
        if (type.ResolutionScope.Kind == HandleKind.TypeReference)
        {
            var declaring = ExternalTypeRef(module, (TypeReferenceHandle)type.ResolutionScope);
            full = declaring.ExternalName + "+" + name;
            assembly = declaring.ExternalAssembly!;
        }
        else if (type.ResolutionScope.Kind == HandleKind.AssemblyReference)
            assembly = module.Reader.GetString(module.Reader.GetAssemblyReference(
                (AssemblyReferenceHandle)type.ResolutionScope).Name);
        return TypeDesc.MakeExternal(full, module, handle, assembly);
    }

    // A scoped unresolved TypeRef must never be promoted through a different assembly's name.
    internal ClassInfo? ResolveExternalClass(TypeDesc type) => type.ExternalModule is { } module
        ? ResolveTypeRef(module, type.ExternalHandle)?.Class
        : type.ExternalName is { } name ? FindClassByFullName(name) : null;

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

    /// <summary>The type a custom-attribute blob names by its serialized name (ECMA-335
    /// II.23.3): <c>Ns.Outer+Inner`1[[Arg, Asm],[Arg, Asm]][]</c>, optionally followed by
    /// <c>, Assembly, Version=…</c>. A CoreLib primitive, at any depth, is the primitive a
    /// signature decodes, so a closed generic keys the same instantiation a body names.
    /// An open generic definition, and anything unresolved, answers the External type-name
    /// prefix: for the definition that is the backtick name its open-definition handle is
    /// keyed on, otherwise a type no attribute can render. Only Discovery mints a closed
    /// generic; a later decode finds the instantiations that exist.</summary>
    internal TypeDesc ResolveSerializedTypeName(string name)
    {
        try
        {
            int pos = 0;
            if (ParseSerializedType(name, ref pos) is { } type && (pos == name.Length || name[pos] == ','))
                return type;
        }
        catch (Exception e) when (!IsMustEscape(e))
        {
            // An undecodable name falls back like an unresolved one.
        }
        return TypeDesc.MakeExternal(SerializedTypeNamePrefix(name));
    }

    /// <summary>A serialized type name without its assembly part: everything before the
    /// first comma outside brackets.</summary>
    private static string SerializedTypeNamePrefix(string name)
    {
        int depth = 0;
        for (int i = 0; i < name.Length; i++)
        {
            if (name[i] == '[')
                depth++;
            else if (name[i] == ']')
                depth--;
            else if (name[i] == ',' && depth == 0)
                return name[..i].Trim();
        }
        return name.Trim();
    }

    /// <summary>The '+' chain, generic arguments and array suffixes at
    /// <paramref name="pos"/>. Qualified arguments and the outer type resolve in
    /// their named assembly; bare arguments leave the separating comma untouched.
    /// Leaves <paramref name="pos"/> after the name; null when unresolved.</summary>
    private TypeDesc? ParseSerializedType(string s, ref int pos, bool allowAssembly = true)
    {
        while (pos < s.Length && s[pos] == ' ')
            pos++;
        int start = pos;
        while (pos < s.Length && s[pos] is not ('[' or ']' or ','))
        {
            // Escaped names, pointers and byrefs are not attribute-value shapes.
            if (s[pos] is '\\' or '*' or '&')
                return null;
            pos++;
        }
        string chain = s[start..pos].TrimEnd();
        TypeDesc[]? args = null;
        if (pos + 1 < s.Length && s[pos] == '[' && s[pos + 1] is not (']' or ',' or '*'))
        {
            var list = new List<TypeDesc>();
            do
            {
                pos++;
                while (pos < s.Length && s[pos] == ' ')
                    pos++;
                bool qualified = pos < s.Length && s[pos] == '[';
                if (qualified)
                    pos++;
                if (ParseSerializedType(s, ref pos, qualified) is not { } arg)
                    return null;
                if (qualified)
                {
                    // An assembly name carries no brackets.
                    while (pos < s.Length && s[pos] != ']')
                        pos++;
                    if (pos == s.Length)
                        return null;
                    pos++;
                }
                list.Add(arg);
                while (pos < s.Length && s[pos] == ' ')
                    pos++;
            }
            while (pos < s.Length && s[pos] == ',');
            if (pos == s.Length || s[pos] != ']')
                return null;
            pos++;
            args = list.ToArray();
        }
        var ranks = new List<int>();
        while (pos < s.Length && s[pos] == '[')
        {
            int rank = 1;
            pos++;
            while (pos < s.Length && s[pos] == ',')
            {
                rank++;
                pos++;
            }
            // "[*]" and bounded shapes are not attribute-value shapes.
            if (pos == s.Length || s[pos] != ']')
                return null;
            pos++;
            ranks.Add(rank);
        }
        string? assembly = null;
        if (allowAssembly && pos < s.Length && s[pos] == ',')
        {
            int assemblyStart = ++pos;
            while (pos < s.Length && s[pos] is not (',' or ']'))
                pos++;
            assembly = s[assemblyStart..pos].Trim();
            if (assembly.Length == 0)
                return null;
            while (pos < s.Length && s[pos] != ']')
                pos++;
        }
        if (ResolveSerializedChain(chain, args, assembly) is not { } type)
            return null;
        foreach (int rank in ranks)
            type = rank == 1 ? TypeDesc.MakeSZArray(type) : TypeDesc.MakeMDArray(type, rank);
        return type;
    }

    /// <summary>The type a '+'-separated chain names, closed over <paramref name="args"/>
    /// when it is generic; null for an open definition and for anything unresolved. Each
    /// nested link is looked up among its declaring type's nested types, since the type
    /// index keys every nested type under an empty namespace.</summary>
    private TypeDesc? ResolveSerializedChain(string chain, TypeDesc[]? args, string? assembly)
    {
        if (args is null && (assembly is null || SerializedCoreLibAssembly(assembly))
            && WellKnownPrimitive(chain) is { } primitive)
            return primitive;
        int plus = chain.IndexOf('+');
        string head = plus < 0 ? chain : chain[..plus];
        int dot = head.LastIndexOf('.');
        var key = dot < 0 ? ("", head) : (head[..dot], head[(dot + 1)..]);
        if (!TypeIndex().TryGetValue(key, out var candidates))
            return null;
        foreach (var (module, top) in candidates)
        {
            if (assembly is not null
                && !module.AssemblyName.Equals(assembly, StringComparison.OrdinalIgnoreCase)
                && !(module.AssemblyName == "System.Private.CoreLib" && SerializedCoreLibAssembly(assembly)))
                continue;
            var reader = module.Reader;
            if (!reader.GetTypeDefinition(top).GetDeclaringType().IsNil)
                continue;
            TypeDefinitionHandle? handle = top;
            for (int link = plus; link >= 0 && handle is { } declaring;)
            {
                int next = chain.IndexOf('+', link + 1);
                string name = next < 0 ? chain[(link + 1)..] : chain[(link + 1)..next];
                handle = null;
                foreach (var nested in reader.GetTypeDefinition(declaring).GetNestedTypes())
                    if (reader.StringComparer.Equals(reader.GetTypeDefinition(nested).Name, name))
                    {
                        handle = nested;
                        break;
                    }
                link = next;
            }
            if (handle is { } resolved)
                return SerializedTypeDesc(module, resolved, args);
        }
        return null;
    }

    // Standard CoreLib facades serialize their forwarded types under the facade name.
    private static bool SerializedCoreLibAssembly(string assembly) =>
        assembly.Equals("System.Private.CoreLib", StringComparison.OrdinalIgnoreCase)
        || assembly.Equals("mscorlib", StringComparison.OrdinalIgnoreCase)
        || assembly.Equals("System.Runtime", StringComparison.OrdinalIgnoreCase)
        || assembly.Equals("netstandard", StringComparison.OrdinalIgnoreCase);

    /// <summary>The TypeDesc of a resolved serialized TypeDef: the class itself, or its
    /// instantiation over <paramref name="args"/>. Only Discovery instantiates, since the
    /// emit set closes after it; a later phase answers only an instantiation that
    /// exists.</summary>
    private TypeDesc? SerializedTypeDesc(Module module, TypeDefinitionHandle handle, TypeDesc[]? args)
    {
        if (module.ClassMap.TryGetValue(handle, out var cls))
            return args is null ? TypeDesc.MakeClass(cls) : null;
        if (args is null || !module.GenericTemplates.Contains(handle)
            || module.Reader.GetTypeDefinition(handle).GetGenericParameters().Count != args.Length)
            return null;
        if (Phase == EmitPhase.Discovery)
            return TypeDesc.MakeClass(Instantiate(module, handle, args));
        return _instances.TryGetValue((module.Index, SRME.GetToken(handle)), out var byArgs)
            && byArgs.TryGetValue(args, out var existing)
                ? TypeDesc.MakeClass(existing)
                : null;
    }

    /// <summary>The width a custom-attribute blob encodes a value of enum type
    /// <paramref name="type"/> at; Int32 for a type that is not a resolved enum. A closed
    /// generic learns it is an enum with its shape, which Discovery completes here so the
    /// reach-time decode reads the width the emit-time decode reads.</summary>
    internal PrimitiveTypeCode SerializedEnumUnderlying(TypeDesc type)
    {
        if (type.Kind != TypeKind.Class)
            return PrimitiveTypeCode.Int32;
        var cls = type.Class!;
        if (cls.GenericArity > 0 && Phase == EmitPhase.Discovery)
            CompleteShape(cls);
        return cls.IsEnum ? cls.EnumUnderlying : PrimitiveTypeCode.Int32;
    }

    /// <summary>The CLR qualified name (<c>Ns.Outer+Mid+Leaf</c>) of a TypeDef whose
    /// outermost declaring type lives under <c>System.Runtime.Intrinsics.</c>; null for
    /// every other type, so the ISA table is consulted only for that namespace. A
    /// declaring-chain walk over TypeDefinition rows, like <see cref="NestedCppPrefix"/>.</summary>
    private static string? PlatformIsaQualifiedName(MetadataReader reader, TypeDefinition td)
    {
        string chain = reader.GetString(td.Name);
        var outer = td;
        for (var declHandle = td.GetDeclaringType(); !declHandle.IsNil; declHandle = outer.GetDeclaringType())
        {
            outer = reader.GetTypeDefinition(declHandle);
            chain = reader.GetString(outer.Name) + "+" + chain;
        }
        string ns = reader.GetString(outer.Namespace);
        return ns.StartsWith("System.Runtime.Intrinsics.", StringComparison.Ordinal) ? ns + "." + chain : null;
    }

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

    private static bool IsEmbeddedMetadataAttribute(MetadataReader reader, TypeDefinition td)
    {
        if ((td.Attributes & TypeAttributes.VisibilityMask) != TypeAttributes.NotPublic
            || reader.GetString(td.Namespace) != "System.Runtime.CompilerServices"
            || td.BaseType.Kind != HandleKind.TypeReference
            || TypeRefFullName(reader, (TypeReferenceHandle)td.BaseType) != "System.Attribute")
            return false;
        bool compilerGenerated = false;
        bool embedded = false;
        foreach (var handle in td.GetCustomAttributes())
        {
            string? name = AttributeTypeName(reader, reader.GetCustomAttribute(handle));
            compilerGenerated |= name == "System.Runtime.CompilerServices.CompilerGeneratedAttribute";
            embedded |= name == "Microsoft.CodeAnalysis.EmbeddedAttribute";
        }
        return compilerGenerated && embedded;
    }

    private void ValidatePrimaryCoreLibNames()
    {
        var input = Modules[0];
        var publicCoreLibNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (var module in Modules)
        {
            if (module == input || module.AssemblyName is not ("System.Private.CoreLib" or "mscorlib"))
                continue;
            var reader = module.Reader;
            foreach (var handle in reader.TypeDefinitions)
            {
                var td = reader.GetTypeDefinition(handle);
                if (!td.GetDeclaringType().IsNil
                    || (td.Attributes & TypeAttributes.VisibilityMask) != TypeAttributes.Public)
                    continue;
                string ns = reader.GetString(td.Namespace);
                string name = reader.GetString(td.Name);
                publicCoreLibNames.Add(string.IsNullOrEmpty(ns) ? name : ns + "." + name);
            }
        }
        // Full-name lookup cannot distinguish input identities owned by CoreLib or
        // the runtime. Embedded compiler attributes may predate their public CoreLib counterparts.
        foreach (var handle in input.Reader.TypeDefinitions)
        {
            var td = input.Reader.GetTypeDefinition(handle);
            if (!td.GetDeclaringType().IsNil)
                continue;
            string ns = input.Reader.GetString(td.Namespace);
            string name = input.Reader.GetString(td.Name);
            string fullName = string.IsNullOrEmpty(ns) ? name : ns + "." + name;
            int arity = fullName.IndexOf('`');
            string intrinsicName = arity < 0 ? fullName : fullName[..arity];
            bool runtimeIdentity = intrinsicName.StartsWith("System.", StringComparison.Ordinal)
                && (intrinsicName is "System.Object" or "System.ValueType" or "System.Runtime.CompilerServices.Unsafe"
                    || CoreIntrinsics.RuntimeOwnsTypeInfo(intrinsicName)
                    || CoreIntrinsics.IntrinsicGenericCppType(intrinsicName) is not null);
            if (runtimeIdentity || (publicCoreLibNames.Contains(fullName)
                    && !IsEmbeddedMetadataAttribute(input.Reader, td)))
                throw new NotSupportedException(
                    $"input type '{fullName}' in assembly '{input.AssemblyName}' conflicts with a CoreLib/runtime type identity");
        }
    }

    private void Build()
    {
        ValidatePrimaryCoreLibNames();
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
                // A CoreLib platform-ISA facade (Sse2, Lzcnt.X64, AdvSimd.Arm64, PackedSimd,
                // …) is identified by its CLR qualified name, which for a nested facade the
                // bare FullName cannot carry. A declaring-chain walk over TypeDefinition rows
                // only — no member decode — so the stamp is strict-completion safe.
                cls.PlatformIsa = PlatformIsaQualifiedName(reader, td) is { } isaName
                    ? CoreIntrinsics.PlatformIsaFamily(isaName)
                    : null;
                // An intrinsic type whose C++ mapping is a by-value struct (no trailing
                // '*') is modeled as a value type even when its metadata is a reference
                // type — e.g. the file-backed view handles lowered to
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
                    PInvoke = ReadPInvoke(module, md),
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
            if (cls.IsEnum || cls.GenericArity > 0)
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

        // The ISA-surface dump reads the model as built — every TypeDef of every loaded
        // module has its shell and stamp — and needs no reachability; stopping here keeps
        // the dump independent of what the app happens to call.
        if (StopBeforeReachability)
            return;

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
        TypeKind.Pointer when t.FunctionSignature is { } signature => 1 + MaxArgDepth(signature.Types),
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
        if (_mediumOwner is not null)
            chain += "\n  [Medium type retention: " + MediumRetentionChain(_mediumOwner) + "]";
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
        {
            if (_preserveMintedInstances.Count > 0)
                NotePreserveMint(existing, minted: false);
            return existing;
        }

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
            CppNamePrefix = GenericTypeCppPrefix(module, templateHandle),
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
        NotePreserveMint(spec, minted: true);
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

    private readonly Dictionary<(int Module, int Token), string> _genericCppPrefixes = new();

    private string GenericTypeCppPrefix(Module module, TypeDefinitionHandle handle)
    {
        var key = (module.Index, SRME.GetToken(handle));
        if (_genericCppPrefixes.TryGetValue(key, out var prefix))
            return prefix;
        var type = module.Reader.GetTypeDefinition(handle);
        var name = (module.Reader.GetString(type.Namespace), module.Reader.GetString(type.Name));
        string full = MethodCompiler.OpenDefBacktickName(module, handle)!;
        prefix = "";
        if (TypeIndex().TryGetValue(name, out var candidates))
            foreach (var (other, definition) in candidates)
                if (other != module && MethodCompiler.OpenDefBacktickName(other, definition) == full)
                {
                    prefix = "m" + module.Index + "_";
                    break;
                }
        _genericCppPrefixes.Add(key, prefix);
        return prefix;
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
            var inst = InstantiateMethodOnClass(cls, cls.Module, mh, methodArgs);
            Reach(inst);
            // A patch callvirt of a generic virtual instantiation enters the
            // dispatcher an AOT callvirt would, so the root registers it with the
            // allocated types' overrides. A final method's or a sealed class's
            // body is every receiver's.
            if (IsGvmCall(inst) && !cls.IsSealed && (inst.Attributes & MethodAttributes.Final) == 0)
                ReachUsedGvm(inst, callSite: false);
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
                    ExternalMangle(t),
                // The type-constructor arms: base + one marker per level, read back right to
                // left (CppNaming.MangleFragment states the injectivity contract). A single
                // "T" for all of them would put Shadow<int[,]> and Shadow<double[,]> in one
                // specialization — one static, one type-info — silently.
                { Kind: TypeKind.SZArray } => MangleArg(t.Element!) + "Arr",
                { Kind: TypeKind.MDArray } => MangleArg(t.Element!) + "Md" + t.Rank,
                { FunctionSignature: { } signature } => FunctionPointerMangle(signature),
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

    private static string FunctionPointerMangle(TypeDesc.FunctionPointerInfo signature)
    {
        var key = new System.Text.StringBuilder("Gfn").Append(signature.Convention).Append('_');
        foreach (var type in signature.Types)
        {
            string child = MangleArg(type);
            key.Append(child.Length).Append('_').Append(child);
        }
        return key.Append("End").ToString();
    }

    /// <summary>A closed generic used as an argument retains its definition's arity
    /// and nesting, plus each child's boundary. Its readable C++ name alone cannot
    /// distinguish a pair followed by a key from a triple.</summary>
    internal static string ClosedTypeMangle(ClassInfo type)
    {
        string assembly = GenericIdentitySymbol(type.Module.AssemblyName);
        string definition = GenericIdentitySymbol(
            MethodCompiler.OpenDefBacktickName(type.Module, type.Handle)!);
        // Metadata row numbers and module indices can change between a hot-update
        // base and patch, while the assembly and open-definition names remain stable.
        var key = new System.Text.StringBuilder("Ginst")
            .Append(assembly.Length).Append('_').Append(assembly)
            .Append(definition.Length).Append('_').Append(definition)
            .Append('_').Append(type.Context.TypeArgs.Length).Append('_');
        foreach (var arg in type.Context.TypeArgs)
        {
            string child = GenericIdentitySymbol(MangleArg(arg));
            key.Append(child.Length).Append('_').Append(child);
        }
        return key.Append("End").ToString();
    }

    private static string GenericIdentitySymbol(string identity)
    {
        bool safe = true;
        foreach (char c in identity)
            if (!char.IsAsciiLetterOrDigit(c) && c != '_')
                safe = false;
        if (safe)
            return "N" + identity;
        // Preserve punctuation before Sanitize; escape underscores as well so a
        // literal identifier cannot impersonate an encoded separator or '$' token.
        var symbol = new System.Text.StringBuilder("X");
        const string hex = "0123456789ABCDEF";
        foreach (char c in identity)
        {
            if (char.IsAsciiLetterOrDigit(c))
                symbol.Append(c);
            else
            {
                symbol.Append('_');
                for (int shift = 12; shift >= 0; shift -= 4)
                    symbol.Append(hex[(c >> shift) & 15]);
            }
        }
        return symbol.ToString();
    }

    private static string ExternalMangle(TypeDesc type)
    {
        string name = CppNaming.Sanitize(type.ExternalName!);
        // An unavailable scoped type cannot share an array or instantiation key with a loaded namesake.
        if (type.ExternalModule is { } module && CppTypes.ExternalCppName(type.ExternalName) is null
            && module.Owner.ResolveExternalClass(type) is null)
        {
            string assembly = type.ExternalAssembly!.ToUpperInvariant();
            // Gtpl's reserved leaf space keeps this scoped key apart from named types.
            return "GtplExternal_" + CppNaming.GenericDefinitionStem(
                assembly.Length + "_" + assembly + "+" + type.ExternalName);
        }
        return CppNaming.MangleFragment(name);
    }

    /// <summary>The metadata-facing type fragment shown in a generic method's
    /// readable symbol stem. Identity stays in <see cref="MangleArg"/> and the
    /// numeric suffix; this rendering deliberately omits namespaces and closed
    /// declaring-type arguments.</summary>
    private static string ReadableMethodArg(TypeDesc t)
    {
        if (t.IsCanonPlaceholder)
            return t.CanonAnyIndex >= 0 ? "CnAny" + t.CanonAnyIndex
                : t.IsObject ? "CnRef" : "Cn" + t.Primitive;
        return t.Kind switch
        {
            TypeKind.Primitive => t.Primitive.ToString(),
            TypeKind.Class => CppNaming.Il2CppClean(t.Class!.Handle.IsNil
                ? t.Class.Name
                : t.Class.Module.Reader.GetString(
                    t.Class.Module.Reader.GetTypeDefinition(t.Class.Handle).Name)),
            TypeKind.External => CppNaming.Il2CppClean(SimpleExternalTypeName(t.ExternalName!)),
            TypeKind.ExternalGeneric => CppNaming.Il2CppClean(SimpleExternalTypeName(t.ExternalName!)),
            TypeKind.SZArray => ReadableMethodArg(t.Element!) + "U5BU5D",
            TypeKind.MDArray => ReadableMethodArg(t.Element!)
                + CppNaming.Il2CppClean("[" + new string(',', t.Rank - 1) + "]"),
            TypeKind.Pointer => ReadableMethodArg(t.Element!) + "U2A",
            TypeKind.ByRef => ReadableMethodArg(t.Element!) + "U26",
            TypeKind.GenericVar => "T" + t.GenVarIndex,
            TypeKind.Template => CppNaming.Il2CppClean(MangleArg(t)),
            _ => throw new InvalidOperationException(
                $"no readable generic-method argument for type kind {t.Kind} ({t})"),
        };
    }

    private static string SimpleExternalTypeName(string name)
    {
        int dot = name.LastIndexOf('.');
        int nested = name.LastIndexOf('+');
        int slash = name.LastIndexOf('/');
        int separator = Math.Max(dot, Math.Max(nested, slash));
        return separator >= 0 ? name.Substring(separator + 1) : name;
    }

    /// <summary>Canonical mangle for an SZArray element type — the suffix of the
    /// <c>ti_arr_&lt;…&gt;</c> symbol CppEmitter emits for the array <c>element[]</c>
    /// — shared by the collection set and the emitter so they agree.</summary>
    internal static string ArrayElemMangle(TypeDesc element) => MangleArg(element);

    /// <summary>A type's instantiation-cache mangle: one spelling per type the model
    /// distinguishes, each generic parameter by position.</summary>
    internal static string IdentityMangle(TypeDesc t) => MangleArg(t);

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
        // Signature arrays use their structural signature and the runtime interner.
        if (CppEmitter.HasSignatureShape(element))
            return;
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

    /// <summary>MDArray types whose identity is named by a concrete instruction token,
    /// an SZArray element (<c>new int[2][,]</c>, <c>typeof(int[][,])</c>) or
    /// inside a constructed type's identity closure, keyed by <see cref="ArrayElemMangle"/>.
    /// CppEmitter emits one static <c>ti_md_&lt;key&gt;</c> per entry and registers it in
    /// the type registry, where the runtime interner (<c>dn2cpp_array_ti</c>) resolves
    /// every <c>new T[,]</c> of the shape to it — so the static handle IS the interned
    /// identity, not a second one. Concrete instruction tokens resolve the same
    /// static identity through the interner.</summary>
    internal Dictionary<string, TypeDesc> MdArrayTypes { get; } = new(System.StringComparer.Ordinal);

    /// <summary>See <see cref="MdArrayTypes"/>. Recurses like NoteArrayElementType so
    /// the whole GetElementType chain stays linkable, notes a referenced enum element's
    /// own ti_ (the NoteReflectedArrayType precedent), and keys the shared rank&gt;=2
    /// dispatch map — the MD identity can flow through an SZ array or constructed
    /// generic metadata without a statically visible MD token.</summary>
    internal void NoteMdArrayType(TypeDesc md)
    {
        if (CppEmitter.HasSignatureShape(md))
            return;
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

    internal ClassInfo? ArrayDispatchClass { get; private set; }

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
        if (CppEmitter.HasSignatureShape(element))
        {
            NoteMdArrayUse();
            return;
        }
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
        if (ResolveExternalClass(element) is not { } cls)
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
        if (CppEmitter.HasSignatureShape(element))
            return;
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
        if (ArrayDispatchClass is null && FindClassByFullName("System.Array") is { } arrayClass)
        {
            ArrayDispatchClass = arrayClass.EnsureMembers();
            NoteReferencedType(arrayClass);
        }
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
    /// member-type pre-note (<c>TypeMetadataEmitter.NoteReflectedMemberTypes</c>) is
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
        MappedFileDispose,
        MappedViewDispose,
        NoopDispose,
        BlockingCollectionDispose,
        TimerChange,
        TimerDisposeAsync,
        CtsDispose,
        WaitHandleDispose,
        SafeWaitHandleDispose,
        TaskDispose,
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
        new("System.IO.MemoryMappedFiles.MemoryMappedFile", "dn2cpp_mappedfile_type", "itfthunk_mappedfile_dispose",
            "System", "IDisposable", "Dispose", 0, IntrinsicInterfaceThunkKind.MappedFileDispose),
        new("System.IO.MemoryMappedFiles.MemoryMappedViewAccessor", "dn2cpp_mappedview_type", "itfthunk_mappedview_dispose",
            "System", "IDisposable", "Dispose", 0, IntrinsicInterfaceThunkKind.MappedViewDispose),
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
            IntrinsicInterfaceThunkKind.BlockingCollectionDispose),
        new("System.Threading.CancellationTokenSource", "dn2cpp_cancel_source_type", "itfthunk_cts_dispose",
            "System", "IDisposable", "Dispose", 0, IntrinsicInterfaceThunkKind.CtsDispose),
        new("System.Threading.SemaphoreSlim", "dn2cpp_semaphore_type", "itfthunk_semaphore_dispose",
            "System", "IDisposable", "Dispose", 0, IntrinsicInterfaceThunkKind.NoopDispose),
        new("System.Threading.ManualResetEventSlim", "dn2cpp_manualreseteventslim_type", "itfthunk_mres_dispose",
            "System", "IDisposable", "Dispose", 0, IntrinsicInterfaceThunkKind.NoopDispose),
        // One row serves EventWaitHandle, ManualResetEvent and AutoResetEvent: their
        // handles carry no table of their own, so the resolve walk climbs to this one.
        new("Microsoft.Win32.SafeHandles.SafeWaitHandle", "dn2cpp_safewaithandle_type", "itfthunk_safewaithandle_dispose",
            "System", "IDisposable", "Dispose", 0, IntrinsicInterfaceThunkKind.SafeWaitHandleDispose),
        new("System.Threading.Tasks.Task", "dn2cpp_task_type", "itfthunk_task_dispose",
            "System", "IDisposable", "Dispose", 0, IntrinsicInterfaceThunkKind.TaskDispose),
        new("System.Threading.WaitHandle", "dn2cpp_waithandle_type", "itfthunk_waithandle_dispose",
            "System", "IDisposable", "Dispose", 0, IntrinsicInterfaceThunkKind.WaitHandleDispose),
    ];

    /// <summary>A row whose intrinsic is instantiated by the program and whose interface
    /// resolved against the loaded CoreLib: the row, the interface's ClassInfo and the
    /// declaration whose vtable slot the thunk fills. CppEmitter turns each into one
    /// <c>Dn2CppInterfaceEntry</c> row installed by the init prologue.</summary>
    internal sealed record IntrinsicInterfaceInfo(IntrinsicInterfaceRow Row, ClassInfo Itf,
        MethodInfo SlotDecl, ClassInfo? Receiver, MethodInfo? Target);

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

    /// <summary>Notes that the intrinsic named by <paramref name="intrinsicName"/> can
    /// be allocated by a constructor, factory, or reflection, wiring its
    /// interface-dispatch info once. The emitter renders the row
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
                IntrinsicInterfaceThunkKind.TimerDispose or IntrinsicInterfaceThunkKind.MappedFileDispose
                    or IntrinsicInterfaceThunkKind.MappedViewDispose
                    or IntrinsicInterfaceThunkKind.NoopDispose or IntrinsicInterfaceThunkKind.CtsDispose
                    or IntrinsicInterfaceThunkKind.BlockingCollectionDispose
                    or IntrinsicInterfaceThunkKind.WaitHandleDispose
                    or IntrinsicInterfaceThunkKind.SafeWaitHandleDispose
                    or IntrinsicInterfaceThunkKind.TaskDispose => decl.Signature.ReturnType.IsVoid
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
            var receiver = FindClassByFullName(row.IntrinsicName);
            var target = receiver is not null ? ResolveItfImplOrNull(receiver, decl) : null;
            _intrinsicItfs[i] = new IntrinsicInterfaceInfo(row, itf, decl, receiver, target);
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

    /// <summary>The generic counterpart of <see cref="ReachStringStaticMethod"/>: resolves
    /// the closed instantiation a MethodSpec names on an intrinsic-mapped type
    /// (<see cref="CoreIntrinsics.IsRealBodyGeneric"/>) and reaches its real body for
    /// the intercepted call site to call.</summary>
    internal MethodInfo ReachIntrinsicTypeMethodSpec(Module module, MethodSpecificationHandle msh, GenericContext ctx)
    {
        var m = ResolveMethodSpec(module, msh, ctx);
        ReachIntrinsicTypeMethod(m);
        DrainReachability();
        return m;
    }

    internal readonly HashSet<FieldInfo> IntrinsicManagedStaticFields = new();

    internal MethodInfo ReachIntrinsicStaticMethod(string ownerName, string name)
    {
        var owner = FindClassByFullName(ownerName)
            ?? throw new NotSupportedException($"{ownerName} is not loaded");
        var method = owner.EnsureMembers().Methods.FirstOrDefault(m => m.IsStatic && m.Rva != 0
            && m.Name == name && m.Signature.ParameterTypes.Length == 0)
            ?? throw new NotSupportedException($"{ownerName}.{name} has no managed getter");
        foreach (var field in owner.Fields)
            if (field.IsStatic && !field.IsLiteral)
            {
                IntrinsicManagedStaticFields.Add(field);
                if (field.Type is { Kind: TypeKind.Class, Class: { } fieldClass })
                    NoteReferencedType(fieldClass);
            }
        ReachIntrinsicTypeMethod(method);
        DrainReachability();
        return method;
    }

    internal MethodInfo ReachIntrinsicStaticMethod(string ownerName, string name,
        Func<MethodInfo, bool> matches)
    {
        var owner = FindClassByFullName(ownerName)
            ?? throw new NotSupportedException($"{ownerName} is not loaded");
        var method = owner.EnsureMembers().Methods.FirstOrDefault(m => m.IsStatic && m.Rva != 0
            && m.Name == name && matches(m))
            ?? throw new NotSupportedException($"{ownerName}.{name} has no matching managed body");
        ReachIntrinsicTypeMethod(method);
        DrainReachability();
        return method;
    }

    /// <summary>The non-generic counterpart of <see cref="ReachIntrinsicTypeMethodSpec"/>:
    /// System.Array's own CoreLib method <paramref name="name"/> with exactly
    /// <paramref name="sig"/>'s parameter types
    /// (<see cref="CoreIntrinsics.IsArrayRealBodyMember"/>), reached for the intercepted
    /// call site to call. Null without a CoreLib. Only same-named overloads have their
    /// signatures decoded.</summary>
    internal MethodInfo? ReachArrayMethod(string name, MethodSignature<TypeDesc> sig)
    {
        if (FindClassByFullName("System.Array") is not { } arr)
            return null;
        var want = sig.ParameterTypes;
        foreach (var m in arr.EnsureMembers().Methods)
        {
            if (m.Name != name || m.Rva == 0 || m.IsStatic == sig.Header.IsInstance)
                continue;
            var have = m.Signature.ParameterTypes;
            if (have.Length != want.Length)
                continue;
            bool same = true;
            for (int i = 0; i < have.Length && same; i++)
                same = have[i].ToString() == want[i].ToString();
            if (!same)
                continue;
            ReachIntrinsicTypeMethod(m);
            DrainReachability();
            return m;
        }
        return null;
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
        System.Func<TypeDesc[], bool> match, bool allocates = false, bool virtualDispatch = false)
    {
        if (FindClassByFullName(typeFullName) is not { } cls)
            return null;
        return ReachManagedMethod(cls, name, match, allocates, virtualDispatch);
    }

    /// <summary>The <see cref="ClassInfo"/>-keyed form of
    /// <see cref="ReachManagedMethod(string,string,System.Func{TypeDesc[],bool},bool,bool)"/>,
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
        System.Func<TypeDesc[], bool> match, bool allocates = false, bool virtualDispatch = false)
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
        if (virtualDispatch && m.IsVirtual)
            ReachUsedVirtual(m);
        // Mandatory, not cosmetic: a method reached here without a drain would be COMPILED
        // by the emit fixpoint's next batch with its body never SCANNED, so its callees
        // would miss the reachable set and AssertCalledBodiesEmitted would fail the
        // transpile on a forwarded-to symbol. (Same note at WireArrayEnumerableMap.)
        DrainReachability();
        return m;
    }

    internal (MethodInfo Local, MethodInfo Daylight)? ReachLocalDaylightSavingTime()
    {
        var cls = FindClassByFullName("System.TimeZoneInfo");
        if (cls is null)
            return null;
        var local = cls.EnsureMembers().Methods.FirstOrDefault(m => m.Name == "get_Local"
            && m.IsStatic && m.Rva != 0 && m.Signature.ParameterTypes.Length == 0);
        var daylight = cls.Methods.FirstOrDefault(m => m.Name == "IsDaylightSavingTime"
            && !m.IsStatic && m.Rva != 0 && m.Signature.ParameterTypes is
                [{ Class.FullName: "System.DateTime" }]);
        if (local is null || daylight is null)
            return null;
        Reach(local);
        Reach(daylight);
        DrainReachability();
        return (local, daylight);
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
        if (CoreIntrinsics.BrLifetimeDispose.Matches(m.DeclaringClass.FullName, m.Name))
            Reach(m);
        if (_intrinsicTypeTranspiled.Contains(m))
            return; // real body transpiled — the symbol already exists
        // Its calls already delegate to the real body, which serves the address too.
        if (CoreIntrinsics.IsRuntimeTypeHandleCopy(m)
            || CoreIntrinsics.IsRealBodyGeneric(m.DeclaringClass.FullName, m.Name)
            || CoreIntrinsics.IsArrayRealBodyMember(m.DeclaringClass.FullName, m.Name, m.Signature))
        {
            ReachIntrinsicTypeMethod(m);
            DrainReachability();
            return;
        }
        if (IntrinsicFtnTargets.Add(m))
            AddReachable(m.EnsureSignature()); // reached => decoded, as in Reach
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
            AddReachable(m.EnsureSignature()); // reached => decoded, as in Reach
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
        // rank>=2 array, also serving element-agnostic Clone on SZ arrays.
        added |= WireMdArrayItfMap();
        added |= ReachMdArrayEnumeration();
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
        // A reflective invoke of an interface row runs the receiver's slot as a callvirt
        // would (dn2cpp_invoke_row). The wrappers are allocated, so a recognized
        // selection's used slot (MarkReflectedSlot) reaches their implementation; an
        // unselected slot holds a trap that refuses the reflective call catchably.
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

    /// <summary>Registers the dispatcher a closed generic virtual row runs through: the
    /// row has no slot, so reflection calls the dispatcher a callvirt of the same
    /// instantiation would. A row on a sealed class or a final row runs its own body,
    /// and a row a call already dispatches has its dispatcher. The dispatcher falls
    /// back to the row's own body, so an application row's body is reached; a
    /// library row with an unreached body has no invoker to enter it.
    /// <paramref name="strips"/> registers a stripping dispatcher
    /// (<see cref="GvmDispatch.Strips"/>), whose bodies outside the application need
    /// not transpile unless a preservation rule keeps them. The row's boxes are noted
    /// whether or not it gets a dispatcher here.</summary>
    private bool ReachReflectedGvm(ClassInfo cls, MethodInfo m, bool strips)
    {
        if (ContainsCanonPlaceholder(cls) || ContainsGenericVar(cls)
            || _backend?.ShouldSkipMethodBody(cls, m) == true)
            return false;
        foreach (var arg in m.Context.MethodArgs)
            if (ContainsCanonPlaceholder(arg) || ContainsGenericVar(arg))
                return false;
        bool noted = NoteReflectedRowBoxes(m);
        if (cls.IsSealed || (m.Attributes & MethodAttributes.Final) != 0
            || _usedGvms.TryGetValue(m.CppName, out var known) && (strips || !known.Strips))
            return noted;
        if (m.Rva != 0 && !Reachable.Contains(m))
        {
            if (cls.Module != AppModule)
                return noted;
            Reach(m);
        }
        ReachUsedGvm(m, callSite: false, strips);
        return true;
    }

    /// <summary>How much of <see cref="_methodInstanceOrder"/> and
    /// <see cref="ReachableSet.Order"/> <see cref="ReachPatchCallableGvms"/> has
    /// visited.</summary>
    private int _patchGvmInstanceCursor, _patchGvmReachCursor;

    /// <summary>A <c>--hotupdate-base</c> build: a patch <c>callvirt</c> can name any closed
    /// generic virtual row the loader binds, a reached body or a bodiless declaration,
    /// whether or not an AOT call site names it. <see cref="MarkPatchCallableGvm"/> gives
    /// each row it accepts the non-stripping dispatcher an AOT <c>callvirt</c> registers,
    /// so the import runs the receiver's override. The loader refuses an import whose row
    /// needs a dispatcher and has none. Driven with the other routes each round.</summary>
    public void ReachPatchCallableGvms()
    {
        if (!_hotUpdateBase)
            return;
        while (MarkPatchCallableGvms())
            DrainReachability();
    }

    // Only retained interface types have rows a patch can bind. Keep both the
    // named default body for call and the allocated receivers' callvirt targets.
    internal bool ReachPatchCallableInterfaceBodies(IReadOnlyList<ClassInfo> retained)
    {
        if (!_hotUpdateBase)
            return false;
        bool marked = false;
        foreach (var cls in retained)
        {
            if (!cls.IsInterface || IsFrameworkAssemblyName(cls.Module.AssemblyName)
                || ContainsCanonPlaceholder(cls) || ContainsGenericVar(cls))
                continue;
            foreach (var m in cls.Methods.ToList())
            {
                if (m.IsStatic || m.Rva == 0
                    || m.Context.MethodArgs.Any(a => ContainsCanonPlaceholder(a) || ContainsGenericVar(a))
                    || m.Context.MethodArgs.Length == 0
                        && m.Module.Reader.GetMethodDefinition(m.Handle).GetGenericParameters().Count > 0
                    || _backend?.ShouldSkipMethodBody(cls, m) == true)
                    continue;
                if (m.IsVirtual && !IsGvmCall(m) && !_usedVirtualDecls.Contains(m))
                {
                    ReachUsedVirtual(m);
                    marked = true;
                }
                if (!Reachable.Contains(m))
                {
                    Reach(m);
                    marked |= Reachable.Contains(m);
                }
            }
        }
        if (marked)
            DrainReachability();
        return marked;
    }

    /// <summary>Registers the dispatchers of the rows the logs gained since the last
    /// visit; returns whether any was registered.</summary>
    private bool MarkPatchCallableGvms()
    {
        bool marked = false;
        while (_patchGvmInstanceCursor < _methodInstanceOrder.Count
               || _patchGvmReachCursor < Reachable.Order.Count)
        {
            // A bodiless row is never reached; a row with a body binds once it is.
            while (_patchGvmInstanceCursor < _methodInstanceOrder.Count)
            {
                var m = _methodInstanceOrder[_patchGvmInstanceCursor++];
                if (m.Rva == 0)
                    marked |= MarkPatchCallableGvm(m);
            }
            var order = Reachable.Order;
            while (_patchGvmReachCursor < order.Count)
                marked |= MarkPatchCallableGvm(order[_patchGvmReachCursor++]);
        }
        return marked;
    }

    /// <summary>Registers <paramref name="m"/>'s dispatcher unless its own body is every
    /// receiver's (a final row, or one on a sealed class), a non-stripping one exists, its
    /// instantiation holds a canonical placeholder or a generic parameter, or the backend
    /// skips its body.</summary>
    private bool MarkPatchCallableGvm(MethodInfo m)
    {
        var cls = m.DeclaringClass;
        if (!IsGvmCall(m) || cls.IsSealed || (m.Attributes & MethodAttributes.Final) != 0
            || ContainsCanonPlaceholder(cls) || ContainsGenericVar(cls)
            || _backend?.ShouldSkipMethodBody(cls, m) == true
            || _usedGvms.TryGetValue(m.CppName, out var known) && !known.Strips)
            return false;
        foreach (var arg in m.Context.MethodArgs)
            if (ContainsCanonPlaceholder(arg) || ContainsGenericVar(arg))
                return false;
        ReachUsedGvm(m, callSite: false);
        return true;
    }

    /// <summary>How much of <see cref="_methodInstanceOrder"/> and
    /// <see cref="ReachableSet.Order"/> <see cref="ReachLateBoundGvmRows"/> has
    /// visited.</summary>
    private int _lateBoundGvmInstanceCursor, _lateBoundGvmReachCursor;

    /// <summary>A reflective call can enter any closed generic virtual row the image
    /// keeps, through a site admission recognizes or not, and without a dispatcher the
    /// runtime runs the row's own body for every receiver. Each such row gets a stripping
    /// dispatcher (<see cref="GvmDispatch.Strips"/>), so an allocated receiver that
    /// overrides the row either has a compiled case or refuses the call as a stripped
    /// body. A call site or a recognized reflective target registers the non-stripping
    /// dispatcher instead. Driven with the other routes each round.</summary>
    public void ReachLateBoundGvmRows()
    {
        if (_hotUpdateBase
            || !_reflectionInvokeUsed && !_reflectionDelegateBindScanned && !NeedsReflectionDelegateBind)
            return;
        bool marked;
        do
        {
            marked = false;
            while (_lateBoundGvmInstanceCursor < _methodInstanceOrder.Count)
            {
                var m = _methodInstanceOrder[_lateBoundGvmInstanceCursor++];
                if (m.Rva == 0)
                    marked |= MarkLateBoundGvm(m);
            }
            var order = Reachable.Order;
            while (_lateBoundGvmReachCursor < order.Count)
                marked |= MarkLateBoundGvm(order[_lateBoundGvmReachCursor++]);
            if (marked)
                DrainReachability();
        }
        while (marked);
    }

    private bool MarkLateBoundGvm(MethodInfo m)
    {
        var cls = m.DeclaringClass;
        if (!IsGvmCall(m) || cls.IsSealed || (m.Attributes & MethodAttributes.Final) != 0
            || !CarriesReflectedSlots(cls) || ContainsCanonPlaceholder(cls) || ContainsGenericVar(cls)
            || _backend?.ShouldSkipMethodBody(cls, m) == true
            || _usedGvms.ContainsKey(m.CppName))
            return false;
        foreach (var arg in m.Context.MethodArgs)
            if (ContainsCanonPlaceholder(arg) || ContainsGenericVar(arg))
                return false;
        ReachUsedGvm(m, callSite: false, strips: true);
        return true;
    }

    /// <summary>How much of <see cref="_methodInstanceOrder"/>
    /// <see cref="InstantiateGvmChainRoots"/> has visited.</summary>
    private int _gvmChainRootCursor;

    /// <summary>Each instantiation <see cref="InstantiateGvmChainRoots"/> made of a method
    /// that introduces a class generic virtual override chain, with the overrides it
    /// roots.</summary>
    private readonly Dictionary<MethodInfo, List<MethodInfo>> _gvmChainRoots = new();

    /// <summary>The root <see cref="InstantiateGvmChainRoots"/> made for each override it
    /// visited.</summary>
    private readonly Dictionary<MethodInfo, MethodInfo> _gvmChainRootOf = new();

    /// <summary>The instantiation of the new slot that introduces class generic virtual
    /// override <paramref name="m"/>'s chain, which its row names, or null.</summary>
    internal MethodInfo? GvmChainRootOf(MethodInfo m) =>
        _gvmChainRootOf.TryGetValue(m, out var root) ? root : null;

    /// <summary>A closed class generic virtual row has no slot, so an override's row names
    /// the method that introduces its chain (<see cref="GvmChainRootOf"/>): hiding ends,
    /// and GetBaseDefinition answers, at that method's row. Instantiates that method at
    /// each override's type arguments, so its row exists wherever the override's does.
    /// Driven by discovery's last fixpoint and again each round: bodies keep minting
    /// instantiations.</summary>
    public void InstantiateGvmChainRoots()
    {
        while (_gvmChainRootCursor < _methodInstanceOrder.Count)
        {
            var m = _methodInstanceOrder[_gvmChainRootCursor++];
            if (GvmChainRootOrNull(m) is not { } root)
                continue;
            if (!_gvmChainRoots.TryGetValue(root, out var overrides))
                _gvmChainRoots.Add(root, overrides = new List<MethodInfo>());
            overrides.Add(m);
            _gvmChainRootOf[m] = root;
        }
    }

    /// <summary>The instantiation at <paramref name="m"/>'s type arguments of the new slot
    /// that introduces <paramref name="m"/>'s override chain, when <paramref name="m"/>
    /// overrides a class generic virtual method; otherwise null. Each override in the
    /// chain takes the most derived base template of its name, generic arity and
    /// definition signature, spelled through its own extends chain.</summary>
    private MethodInfo? GvmChainRootOrNull(MethodInfo m)
    {
        var cls = m.DeclaringClass;
        var args = m.Context.MethodArgs;
        if (!IsGvmCall(m) || m.Handle.IsNil || (m.Attributes & MethodAttributes.NewSlot) != 0
            || cls.IsInterface || ContainsCanonPlaceholder(cls) || ContainsGenericVar(cls))
            return null;
        foreach (var arg in args)
            if (ContainsCanonPlaceholder(arg) || ContainsGenericVar(arg))
                return null;
        try
        {
            var signature = GvmDefinitionSignature(m);
            var owner = cls;
            var overrider = m.Handle;
            for (var b = cls.BaseClass; b is not null; b = b.BaseClass)
            {
                var level = b;
                var (from, own) = (owner, overrider);
                if (b.Handle.IsNil
                    || FindGvmClassTemplate(b, m.Name, args.Length, signature, matchReturn: true, slotOf: null,
                            t => SameGvmDefinition(from, own, level, t))
                        is not { } template)
                    continue;
                if ((b.Module.Reader.GetMethodDefinition(template).Attributes & MethodAttributes.NewSlot) != 0)
                    return InstantiateMethodOnClass(b, b.Module, template, args);
                (owner, overrider) = (b, template);
            }
        }
        // A signature no row can spell has no chain to complete.
        catch (NotSupportedException e) when (!IsMustEscape(e))
        {
        }
        return null;
    }

    /// <summary>Whether <paramref name="m"/>'s row must survive the unreached-row trim:
    /// <c>Delegate.Method</c> answers from it (<see cref="KeepsDelegateTargetRow"/>), a
    /// recognized lookup or enumeration lists it, Medium retains its owner, or it
    /// introduces the chain of an override whose row the image keeps.</summary>
    internal bool KeepsUnreachedRow(MethodInfo m)
    {
        if (KeepsDelegateTargetRow(m) || IsReflectionListedRow(m) || _mediumTypes.Contains(m.DeclaringClass))
            return true;
        if (!_gvmChainRoots.TryGetValue(m, out var overrides))
            return false;
        foreach (var o in overrides)
            if (o.DeclaringClass.Module == AppModule || o.Rva == 0 || Reachable.Contains(o)
                || KeepsDelegateTargetRow(o))
                return true;
        return false;
    }

    // A value type's row is sealed; an intrinsic type carries no rows.
    private static bool CarriesReflectedSlots(ClassInfo cls) =>
        !cls.IsValueType && !cls.IsDelegate && cls.IntrinsicCppName is null
        && !CoreIntrinsics.IsIntrinsicType(cls.FullName);

    /// <summary>Marks the slot of a row reflection can enter when no used declaration
    /// covers it yet, and notes the row's boxes either way. MethodBase.Invoke,
    /// PropertyInfo.GetValue/SetValue and a CreateDelegate binding run a class virtual row,
    /// or an interface row with a default body, through the receiver's slot, as a callvirt
    /// would (dn2cpp_invoke_row), so a selected row is a used slot with no call site. A
    /// class row is invocable once reached, or when abstract (its thunk is
    /// signature-only). A library's default body has a row only once reached. A bodiless
    /// interface row is marked only when a recognized site names it.</summary>
    private bool MarkReflectedSlot(MethodInfo m, bool named)
    {
        if (m.IsStatic || !m.IsVirtual || m.VtableSlot < 0)
            return false;
        var cls = m.DeclaringClass;
        if (cls.IsInterface
            ? (m.Rva == 0 ? !named : cls.Module != AppModule && !Reachable.Contains(m))
            : !m.IsAbstract && !Reachable.Contains(m))
            return false;
        bool noted = NoteReflectedRowBoxes(m);
        if (_usedVirtualDecls.Contains(m) || !cls.IsInterface && ClassSlotUsedAtOrAbove(m))
            return noted;
        ReachUsedVirtual(m);
        return true;
    }

    /// <summary>The rows <see cref="NoteReflectedRowBoxes"/> has noted.</summary>
    private readonly HashSet<MethodInfo> _reflectedBoxRows = new();

    /// <summary>Notes, once per row, the values invoking <paramref name="m"/> through
    /// reflection boxes (<see cref="NoteReflectionInvokeBoxes"/>). Whichever body the
    /// receiver's slot or the dispatcher runs, it returns and writes back the row's
    /// value types, so the row's signature covers every override and implementation.
    /// Returns whether a value type was newly allocated.</summary>
    private bool NoteReflectedRowBoxes(MethodInfo m)
    {
        if (!_reflectedBoxRows.Add(m))
            return false;
        int allocated = _allocatedRefTypes.Count;
        try
        {
            NoteReflectionInvokeBoxes(m);
        }
        // A row whose signature does not decode is not reflectively invokable either.
        catch (NotSupportedException e) when (!IsMustEscape(e))
        {
        }
        return _allocatedRefTypes.Count != allocated;
    }

    /// <summary>Set once a scanned body calls or binds Array.Initialize() through a member
    /// reference, or a compiled body lowers it to <c>dn2cpp_array_initialize</c>, which
    /// runs the element's parameterless constructor through the element type's constructor
    /// row. The scan cannot see the receiver's element type, so it arms on every such
    /// site.</summary>
    private bool _runtimeArrayInitialize;

    /// <summary>How much of <see cref="Classes"/>, which only grows,
    /// <see cref="ReachRuntimeArrayInitializeCtors"/> has visited.</summary>
    private int _runtimeArrayInitializeCursor;

    /// <summary>Arms <see cref="ReachRuntimeArrayInitializeCtors"/> from the lowering: an
    /// Array.Initialize receiver that states no element type can hold any value type at run
    /// time, in a body whose call the scan did not arm on.</summary>
    internal void NoteRuntimeArrayInitialize()
    {
        if (_runtimeArrayInitialize)
            return;
        _runtimeArrayInitialize = true;
        ReachRuntimeArrayInitializeCtors();
    }

    /// <summary>Once <see cref="NoteRuntimeArrayInitialize"/> armed it, reaches the
    /// parameterless constructor of every user-module value type that declares one, so the
    /// constructor row <c>dn2cpp_array_initialize</c> invokes carries a body. No framework
    /// value type a program can name declares one. Driven by discovery's last fixpoint and
    /// again each round, like <see cref="ApplyPendingReflectionSelections"/>: a compiled
    /// body can mint a closed generic value type.</summary>
    public void ReachRuntimeArrayInitializeCtors()
    {
        if (!_runtimeArrayInitialize)
            return;
        bool reached = false;
        while (_runtimeArrayInitializeCursor < Classes.Count)
        {
            var cls = Classes[_runtimeArrayInitializeCursor];
            // A specialization minted by this walk is still pending, and only its shape
            // says whether it is a value type.
            if (!cls.ShapeReady)
                CompletePendingSpecializations();
            if (!cls.ShapeReady)
                throw new InvalidOperationException($"{cls.FullName} is in Classes but was never queued for its shape");
            _runtimeArrayInitializeCursor++;
            if (!cls.IsValueType || cls.IsEnum || cls.IntrinsicCppName is not null || !IsUserModule(cls.Module)
                || ContainsCanonPlaceholder(cls) || ContainsGenericVar(cls))
                continue;
            var ctor = ParameterlessCtorHandle(cls);
            if (ctor.IsNil)
                continue;
            EnsureCompleted(cls);
            foreach (var m in cls.Methods)
            {
                if (m.Handle != ctor || Reachable.Contains(m) || _backend?.ShouldSkipMethodBody(cls, m) == true)
                    continue;
                Reach(m);
                reached = true;
            }
        }
        if (reached)
            DrainReachability();
    }

    /// <summary>The parameterless instance constructor with a body that
    /// <paramref name="cls"/>'s definition declares, or a nil handle. Read from metadata:
    /// a specialization's members are completed only when it has one, and no other
    /// constructor's signature is decoded, since one naming a deeper instantiation of its
    /// own type would feed this walk forever.</summary>
    private static MethodDefinitionHandle ParameterlessCtorHandle(ClassInfo cls)
    {
        if (cls.Handle.IsNil)
            return default;
        var reader = cls.Module.Reader;
        foreach (var handle in reader.GetTypeDefinition(cls.Handle).GetMethods())
        {
            var md = reader.GetMethodDefinition(handle);
            if ((md.Attributes & MethodAttributes.Static) != 0 || md.RelativeVirtualAddress == 0
                || !reader.StringComparer.Equals(md.Name, ".ctor"))
                continue;
            var blob = reader.GetBlobReader(md.Signature);
            if (blob.ReadSignatureHeader().IsGeneric)
                blob.ReadCompressedInteger();
            if (blob.ReadCompressedInteger() == 0)
                return handle;
        }
        return default;
    }

    internal static bool HasParameterlessCtorDefinition(ClassInfo cls)
    {
        if (cls.Handle.IsNil)
            return false;
        var reader = cls.Module.Reader;
        foreach (var handle in reader.GetTypeDefinition(cls.Handle).GetMethods())
        {
            var md = reader.GetMethodDefinition(handle);
            if ((md.Attributes & MethodAttributes.Static) != 0
                || !reader.StringComparer.Equals(md.Name, ".ctor"))
                continue;
            var blob = reader.GetBlobReader(md.Signature);
            if (blob.ReadSignatureHeader().IsGeneric)
                blob.ReadCompressedInteger();
            if (blob.ReadCompressedInteger() == 0)
                return true;
        }
        return false;
    }

    private bool ClassSlotUsedAtOrAbove(MethodInfo m)
    {
        int slot = m.VtableSlot;
        for (var b = m.DeclaringClass; b is not null && slot < b.SlotOwners.Count; b = b.BaseClass)
            if (_usedVirtualDecls.Contains(b.SlotOwners[slot]))
                return true;
        return false;
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

    private volatile bool _enumValuesArraysUsed;
    internal bool EnumValuesArraysUsed => _enumValuesArraysUsed;

    internal void NoteEnumValuesArrays()
    {
        _enumValuesArraysUsed = true;
        // Runtime enum arrays can lack a static E[] token. Their non-generic
        // interfaces use the existing element-agnostic Array reflection wrapper.
        NoteMdArrayUse();
    }

    private bool _mdArrayUsed;
    private bool _arrayCloneDispatchUsed;
    private bool _mdArrayEnumerationReached;

    /// <summary>Notes that the program can materialize or see a non-SZ array:
    /// an MD allocation/accessor/type token, or a runtime-rank or explicit-bound
    /// <c>Array.CreateInstance</c>.
    /// Requests the shared dispatch map and enumeration slots through the emit
    /// fixpoint; Clone-only demand requests the map separately.</summary>
    public void NoteMdArrayUse()
    {
        _mdArrayUsed = true;
        // An MD array's interned type-info carries no rows of its own, so the shared
        // relation-only set is the ONLY thing GetInterfaces() can report for it.
        NoteArrayItfRowsNeeded();
    }

    internal void NoteArrayInterfaceDispatch(ClassInfo itf)
    {
        if (itf.FullName == "System.ICloneable")
        {
            _arrayCloneDispatchUsed = true;
            NoteArrayItfRowsNeeded();
        }
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
    /// array or Clone dispatch is noted: resolves + reaches the
    /// non-generic <c>MDArrayEnumerable</c> wrapper and records the six-interface
    /// dispatch set for CppEmitter. Mirrors <see cref="WireObjectArrayFallbackMap"/> —
    /// driven each fixpoint round, a pure function of the noted flag. Without a
    /// CoreLib the collection interfaces don't exist and nothing can dispatch, so
    /// that case is a no-op (resolved FIRST, so it never demands the shim); a missing
    /// *support shim* is a hard failure — the map would silently not exist and the
    /// first MD interface dispatch would abort at run time.</summary>
    private bool WireMdArrayItfMap()
    {
        if ((!_mdArrayUsed && !_arrayCloneDispatchUsed) || MdArrayInterfaces is not null)
            return false;
        // Both halves of the no-CoreLib guard: without the non-generic collection
        // interfaces nothing can dispatch, and without a LOADED System.Array the
        // wrapper's (System.Array) ctor is inexpressible (its parameter degrades to
        // External and could never match) — either way the MD dispatch keeps the
        // loud abort it has today, and neither case may demand the shim.
        if (!TypeIndex().TryGetValue(("System.Collections", "IEnumerable"), out _)
            || !TypeIndex().TryGetValue(("System", "Array"), out _))
            return false;
        const string Need = "dispatching a non-generic array interface";
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
        if (ArrayDispatchClass is null)
        {
            ArrayDispatchClass = ctor.Signature.ParameterTypes[0].Class!.EnsureMembers();
            NoteReferencedType(ArrayDispatchClass);
        }
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
        // Wiring happens during body compilation; drain now so the emit fixpoint
        // picks up the transitive closure of the just-reached wrapper members.
        DrainReachability();
        return true;
    }

    private bool ReachMdArrayEnumeration()
    {
        if (!_mdArrayUsed || MdArrayInterfaces is null || _mdArrayEnumerationReached)
            return false;
        // Clone can install the map before an MD allocation is discovered. Only
        // MD/signature-array demand adds enumeration slots to the used-virtual cross product.
        _mdArrayEnumerationReached = true;
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

    // Set by a classified site that calls or binds MethodInfo/MethodBase.Invoke,
    // PropertyInfo.GetValue/SetValue, an event accessor or InvokeMember, or by a user
    // body's CreateDelegate. An emission switch: invoker thunks and their boxing, the rows
    // of EmitGvmRowDispatch, the binding of each delegate type's Invoke row to the type's
    // multicast invoker, ReachReflectedTemplateBodies on a selected template,
    // ReachDelegateInvokeBoxes and the array maps' rows ExpandArrayEnumerableMaps
    // reaches. It selects no body a reflection site names.
    private bool _reflectionInvokeUsed;
    internal bool ReflectionInvokeUsed => _reflectionInvokeUsed;

    // Argument-bearing activation requires constructor signatures for runtime binding,
    // including rows whose bodies are absent. The capability selects no constructor body.
    private bool _reflectionConstructorBindingUsed;
    internal bool ReflectionConstructorBindingUsed => _reflectionConstructorBindingUsed;

    // Set when any reached body, a framework one included, calls or binds CreateDelegate.
    // Arms ReachRuntimeDelegateCloneReceivers during discovery, before the lowering sets
    // NeedsReflectionDelegateBind.
    private bool _reflectionDelegateBindScanned;
    // Set when a user body binds a delegate by name (Delegate.CreateDelegate over a Type
    // and a method name). Emits the initializer rows the runtime binder runs.
    private bool _reflectionNamedDelegateBindUsed;
    internal bool NamedDelegateBindingUsed => _reflectionNamedDelegateBindUsed;
    private readonly HashSet<MethodInfo> _bindingOnlyInitializers = new();
    private readonly HashSet<MethodInfo> _bindingOnlyBodies = new();
    private bool _reachingBindingOnlyBody;
    internal bool IsBindingOnlyInitializer(MethodInfo method) => _bindingOnlyInitializers.Contains(method);

    // Set when a reached body calls or binds Delegate.DynamicInvoke, which runs a
    // delegate type's Invoke row as MethodInfo.Invoke does. It or `_reflectionInvokeUsed`
    // arms ReachDelegateInvokeBoxes.
    private bool _delegateDynamicInvokeUsed;

    // One arm of the runtime-instantiation trigger: a classified site calls
    // Type.MakeGenericType.
    private bool _makeGenericTypeUsed;
    // Set when a MakeGenericType site's receiver definition is not known: every
    // typeof'd definition then gets template metadata, and no template body.
    private bool _makeGenericTypeUnknownReceiver;

    // The other arm: the CLR backtick full names of every OPEN generic
    // definition a reached body ldtokens (typeof(D<>)), in first-seen order.
    // Recorded unconditionally, not behind _makeGenericTypeUsed — the flag can
    // be set by a body scanned after the ldtoken site, and gating the record on
    // it would make the set scan-order-dependent. Consumed once, after the scan
    // closes, by the runtime-instantiation template pass.
    private readonly List<string> _typeofOpenGenericDefNames = new();
    private readonly HashSet<string> _typeofOpenGenericDefSeen = new(StringComparer.Ordinal);

    // Set when a classified site calls GetCustomAttributes/IsDefined or reads a
    // CustomAttributeData view. An emission switch: every reflectable row keeps its
    // attribute table, and System.Attribute gets the array handle a filter names.
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
    // scan-order-dependent); duplicates are free (a set).
    private readonly HashSet<string> _userTypeofNamedFrameworkTypes = new();

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

        // The roots: the entry point, the members preservation keeps, a hot-update base's
        // export contract, module initializers, native entry points and what the backend
        // names. Every other body, in the application and its references alike, enters
        // only through a static edge or a recognized reflection site, and a .cctor only
        // through a use of its type (ReachCctor). A root runs with no IL caller, so it
        // triggers its own type's initializer (ReachRootInitializer): .NET runs a
        // precise-init entry type's .cctor before Main.
        if (EntryPoint is not null)
        {
            Reach(EntryPoint);
            ReachRootInitializer(EntryPoint);
        }
        SeedExportContract();

        // A module initializer is a side effect with NO call edge — that is the entire
        // point of the feature — so a call-graph tree-shaker can never discover it. The
        // `<Module>` .cctor the C# compiler emits to call it is therefore a root, in every
        // loaded NON-framework module: the app module and every user library pulled in
        // with -r. No other .cctor is a root (ReachCctor pulls one in on a use of its type,
        // and `<Module>` is never used, so an initializer would silently never run).
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
                {
                    Reach(m);
                    ReachRootInitializer(m);
                }
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
            {
                Reach(m);
                ReachRootInitializer(m);
            }

            foreach (var cls in _backend.AttributeReadClasses(this).ToList())
                ReachBackendAttributeRows(cls);
            foreach (var module in _backend.AttributeReadAssemblies(this).ToList())
                ReachBackendAttributes(module, module.Reader.GetAssemblyDefinition().GetCustomAttributes());
        }

        // Hot-update base build: force-emit the extra closed generic
        // instantiations a patch may bind against (hotupdate-refs.txt roots).
        if (_genericRoots is { Count: > 0 })
            foreach (var root in _genericRoots)
                SeedGenericRoot(root);

        DrainReachability();
        // The reflection selections and the routes that reach bodies no call site names
        // feed one another: a body any of them reaches can carry a site or mint an
        // instantiation a pending selection names. Alternate them to a fixpoint, so what
        // each reaches is walked, wired, noted and released like any other body before the
        // notes and the freeze below. Boxed
        // equality goes last: it reads two facts the others are still producing, which
        // structs got boxed and whether anything compares two objects, and the walk it
        // starts can produce more of both (a field's Equals override is a body, and a body
        // can box), exactly as the used×allocated cross product it stands in for does.
        int admitted, classes, selections;
        do
        {
            admitted = Reachable.Order.Count;
            classes = Classes.Count;
            selections = _reflectionSelectionCount;
            ReachReflectionRuntimeSupport();
            ApplyPendingReflectionSelections();
            NoteAttributeArrayHandle();
            InstantiateGvmChainRoots();
            ReachPatchCallableGvms();
            ReachLateBoundGvmRows();
            ReachExportContractInstances();
            ReachRuntimeArrayInitializeCtors();
            if (ReachBoxedValueEquality())
                DrainReachability();
            if (ReachNonGenericArrayElementEquality())
                DrainReachability();
        }
        while (Reachable.Order.Count != admitted
               || Classes.Count != classes
               || _reflectionSelectionCount != selections);

        // The emit side renders an attribute row whatever reached its ctor. Note what every
        // row names once ctor reachability has settled.
        NoteUnwalkedAttributeRows();

        // --trim-godot-classes: the allowlist may only grow while reachability can
        // still deliver the released lambdas' subtrees — freeze it here, after the
        // last drain and before ANY emission (including the emitter's planning
        // pass), and compute the ldftn redirect table (cut ⟹ route, sealed).
        TrimFreeze();
    }

    /// <summary>A hot-update base's export contract: a patch binds, as a friend assembly
    /// and with no call site in the base, any non-private application method through row
    /// metadata, and reads the statics every application .cctor sets, so each is a root.</summary>
    private void SeedExportContract()
    {
        if (!_hotUpdateBase)
            return;
        foreach (var cls in Classes.ToList())
        {
            // Undecoded specializations wait for ReachExportContractInstances, which covers
            // the ones the program reaches whenever they appear; decoding them here would
            // root every app generic named anywhere. Whatever IS decoded by now (a generic
            // base of a non-generic class, via BuildVtables) is seeded.
            if (cls.Module != AppModule || !cls.MembersReady)
                continue;
            foreach (var m in cls.Methods)
                if (m.Rva != 0 && (m.Name == ".cctor" || !m.IsPrivate))
                    Reach(m);
        }
    }

    private int _exportContractCursor;
    private int _exportContractDepth = -1;
    private readonly HashSet<ClassInfo> _exportContractInstances = new();

    /// <summary>The export contract over closed instantiations of application generics:
    /// the base ABI lists every instantiation the image carries, and a patch binds any
    /// non-private method of one. Covers each instantiation a reached body belongs to, up
    /// to the generic depth reached before the first walk, so a contract body that nests
    /// its own type deeper cannot feed the walk up to the instantiation bound. Generic
    /// methods stay with the hotupdate-refs roots. Driven by discovery's fixpoint and again
    /// each compile round.</summary>
    internal void ReachExportContractInstances()
    {
        if (!_hotUpdateBase)
            return;
        if (_exportContractDepth < 0)
            _exportContractDepth = MaxGenericArgDepth;
        var order = Reachable.Order;
        bool reached = false;
        while (_exportContractCursor < order.Count)
        {
            var cls = order[_exportContractCursor++].DeclaringClass;
            if (cls.Module != AppModule || cls.GenericArity == 0 || cls.GenericDepth > _exportContractDepth
                || ContainsCanonPlaceholder(cls) || ContainsGenericVar(cls) || !_exportContractInstances.Add(cls))
                continue;
            foreach (var m in cls.EnsureMembers().Methods.ToList())
            {
                if (m.Rva == 0 || m.IsPrivate || m.Name == ".cctor" || m.Context.MethodArgs.Length > 0
                    || m.Module.Reader.GetMethodDefinition(m.Handle).GetGenericParameters().Count > 0
                    || _backend?.ShouldSkipMethodBody(cls, m) == true)
                    continue;
                Reach(m);
                reached = true;
            }
        }
        if (reached)
            DrainReachability();
    }

    /// <summary>What reflection runs over delegates with no site naming a body: the Clone
    /// receivers runtime binders allocate, and the values a delegate type's Invoke row
    /// boxes for DynamicInvoke and MethodInfo.Invoke. Both are bounded to the delegates the
    /// program allocates or binds, and gated on the capability bits. Driven by discovery's
    /// fixpoint and again each compile round: a compiled body can allocate a delegate.</summary>
    internal void ReachReflectionRuntimeSupport()
    {
        ReachRuntimeDelegateCloneReceivers();
        ReachDelegateInvokeBoxes();
    }

    internal bool ReachRuntimeDelegateCloneReceivers()
    {
        if (_delegateCloneBindingOwners.Count == 0)
            return false;
        bool added = false;
        int reached = Reachable.Count;
        // Runtime binders allocate delegates without a newobj. Cross only their
        // finite receiver sets admitted by a reached class Clone declaration.
        var receivers = MarshalFnPtrDelegates.ToList();
        if (_reflectionDelegateBindScanned || NeedsReflectionDelegateBind)
            receivers.AddRange(_reflectionDelegateCloneReceivers);
        foreach (var receiver in receivers)
            if (receiver.IsDelegate && !ContainsCanonPlaceholder(receiver)
                && !ContainsGenericVar(receiver) && IsDelegateCloneBindingReceiver(receiver))
            {
                EnsureCompleted(receiver);
                added |= _runtimeDelegateCloneReceivers.Add(receiver);
            }
        // These are Clone selections, not general allocation roots: Invoke boxing
        // would recursively expand delegates whose parameter names a deeper delegate.
        var declarations = _delegateCloneBindingDeclarations.ToList();
        foreach (var decl in _usedVirtualDecls.ToList())
            if (decl.DeclaringClass.FullName == "System.ICloneable" && decl.Name == "Clone")
                declarations.Add(decl);
        foreach (var receiver in _runtimeDelegateCloneReceivers.ToList())
            foreach (var declaration in declarations)
                ReachVirtualImpl(receiver, declaration);
        DrainReachability();
        return added || Reachable.Count != reached;
    }

    /// <summary>The allocated delegate classes, in allocation order, and how many of them
    /// <see cref="ReachDelegateInvokeBoxes"/> has walked.</summary>
    private readonly List<ClassInfo> _allocatedDelegates = new();
    private int _delegateInvokeBoxCursor;

    /// <summary>Delegate.DynamicInvoke and MethodInfo.Invoke over a delegate type's Invoke
    /// row box, with no box site, the result and each by-ref argument written back
    /// (<see cref="NoteReflectionInvokeBoxes"/>). Notes them for each allocated delegate
    /// class once either is in use.</summary>
    private void ReachDelegateInvokeBoxes()
    {
        if (!_delegateDynamicInvokeUsed && !_reflectionInvokeUsed)
            return;
        int count = _allocatedDelegates.Count;
        if (_delegateInvokeBoxCursor == count)
            return;
        for (int i = _delegateInvokeBoxCursor; i < count; i++)
            if (_allocatedDelegates[i].EnsureMembers().Methods.FirstOrDefault(m => m.Name == "Invoke") is { } invoke)
            {
                // Invoker boxing prepares runtime rows; it does not invoke them.
                bool previous = _reachingBindingOnlyBody;
                _reachingBindingOnlyBody |= _strippingLevel == StrippingLevel.Medium;
                try { NoteReflectionInvokeBoxes(invoke); }
                finally { _reachingBindingOnlyBody = previous; }
            }
        _delegateInvokeBoxCursor = count;
        DrainReachability();
    }

    private void ReachBindingInitializer(MethodInfo method)
    {
        bool previous = _reachingBindingOnlyBody;
        _reachingBindingOnlyBody = true;
        try { Reach(method); }
        finally { _reachingBindingOnlyBody = previous; }
    }

    /// <summary>Marks allocated the value type of a value the runtime boxes for
    /// reflection — a returned value, a by-ref argument written back or a field's —
    /// with no box site to do it, so the box can dispatch through the type's interface
    /// map and slots. A by-ref type boxes as its referent, a Nullable&lt;T&gt; as a T.
    /// An unmanaged pointer keeps its runtime-allocated Pointer box. Value types are
    /// bounded to user structs and the modeled Decimal, DateTime and DateTimeOffset
    /// payloads; other framework structs' overrides need not transpile.</summary>
    private void NoteReflectionBoxed(TypeDesc t)
    {
        if (t.Kind == TypeKind.ByRef)
            t = t.Element!;
        if (t is { Kind: TypeKind.Pointer, IsFunctionPointer: false })
        {
            ReachPointerBox();
            return;
        }
        if ((NullableUnderlying(t) ?? t) is { Kind: TypeKind.Class, Class: { } c })
            NoteReflectionBoxed(c);
    }

    /// <summary>Runtime handle boxing has no IL box edge; retain the selected
    /// payload's dispatch through the same route as reflection-produced boxes.</summary>
    internal bool ReachRuntimeHandleBoxes()
    {
        int reached = Reachable.Count;
        int allocated = _allocatedRefTypes.Count;
        foreach (var type in SelectedRuntimeHandleBoxes().ToList())
        {
            // RuntimeTypeHandle has a native pointer payload and a supported
            // serialization rejection body, without an IL box allocation edge.
            if (type is { Kind: TypeKind.Class, Class: { FullName: "System.RuntimeTypeHandle" } handle })
                ReachAllocatedType(handle);
            else
                NoteReflectionBoxed(type);
        }
        return Reachable.Count != reached || _allocatedRefTypes.Count != allocated;
    }

    /// <summary><see cref="NoteReflectionBoxed(TypeDesc)"/> for each value invoking
    /// <paramref name="m"/> through reflection boxes: its return value and each by-ref
    /// argument it writes back.</summary>
    private void NoteReflectionInvokeBoxes(MethodInfo m)
    {
        var sig = m.Signature;
        NoteReflectionBoxed(sig.ReturnType);
        foreach (var p in sig.ParameterTypes)
            if (p.Kind == TypeKind.ByRef)
                NoteReflectionBoxed(p);
    }

    /// <summary>The System.Reflection.Pointer class reflection boxes an unmanaged pointer
    /// result as, once an invoked method or reflected field returns one; the runtime
    /// allocates it with no IL allocation site. Null otherwise, and in a load set
    /// without the class, where reflection refuses such a result.</summary>
    internal ClassInfo? PointerBoxClass { get; private set; }

    private void ReachPointerBox()
    {
        if (PointerBoxClass is not null
            || !TypeIndex().TryGetValue(("System.Reflection", "Pointer"), out var cands))
            return;
        var (mod, tdh) = cands[0];
        if (!mod.ClassMap.TryGetValue(tdh, out var cls))
            return;
        NoteReferencedType(cls);
        ReachAllocatedType(cls);
        PointerBoxClass = cls;
    }

    /// <summary><see cref="NoteReflectionBoxed(TypeDesc)"/> for a value type reflection
    /// constructs: Activator.CreateInstance boxes one whether or not it declares a
    /// constructor. A box always carries its exact type, never a shared-generic
    /// placeholder or an open shell, and never a by-ref-like type, which reflection
    /// refuses to box: a constructor's Invoke throws TargetException, every other route
    /// NotSupportedException.</summary>
    private void NoteReflectionBoxed(ClassInfo c)
    {
        if ((IsUserModule(c.Module) || c.FullName is "System.Decimal" or "System.DateTime" or "System.DateTimeOffset")
            && !ContainsCanonPlaceholder(c) && !ContainsGenericVar(c)
            && EnsureShape(c) is { IsValueType: true, IsByRefLike: false })
        {
            ReachAllocatedType(c);
            // The reflection box has the same ToString slot as an IL box.
            if (EffectiveToString(c) is { } toString)
                Reach(toString);
        }
    }

    /// <summary>Whether an assembly simple name identifies a .NET framework/BCL
    /// assembly (used to bound the [UnmanagedCallersOnly] rooting to user/library
    /// modules — a non-framework library's native callbacks are rooted, while
    /// the BCL's own host/PAL callbacks stay tree-shaken).
    ///
    /// <para><c>Dn2Cpp.Runtime</c> — dn2cpp's own managed support library, auto-loaded
    /// beside the executable — is the platform's half, not user code: its members
    /// exist to be pulled by the transpiler's dedicated routes (RequireShimType),
    /// and several are transpilable ONLY through them (Dn2CppConsoleWriter's ctor
    /// names the intrinsic-mapped TextWriter base ctor). As a framework assembly it
    /// contributes no module-initializer or <c>[UnmanagedCallersOnly]</c> root and no
    /// custom-attribute rows, its value types get no reflection boxes, and its
    /// reflection sites report as framework sites.</para>
    ///
    /// <para><b>This list is for platform assemblies, whose roots, attribute rows and
    /// boxes belong to the runtime rather than to the application — a name does not
    /// belong here merely because dn2cpp ships it.</b> The
    /// conditional default references (<c>DnZlib</c>, <c>DnBrotli</c>, <c>DnHttp</c> —
    /// see <see cref="AssemblyLoadSet"/>) are shipped and are deliberately absent: they
    /// are ordinary IL on ordinary types (<c>DnHttp.DnHttpBackend</c> is an
    /// <c>internal static class</c> with no ctor at all), and their
    /// <c>[NativeImplementation]</c> adapters already satisfy
    /// <see cref="RegisterNativeImpl"/>'s shape constraints. Adding them would take
    /// them out of <see cref="IsUserModule"/>, drop their custom-attribute metadata,
    /// and move the output of the existing <c>-r DnZlib</c> gates — with not one
    /// motivating defect behind it.</para></summary>
    internal static bool IsFrameworkAssemblyName(string name) =>
        PreservationReader.IsFrameworkAssemblyName(name);

    /// <summary>Whether a module is user/library code rather than a framework/BCL
    /// assembly — the complement of <see cref="IsFrameworkAssemblyName"/>. Used to
    /// gate the custom-attribute reflection tables, which must cover a class declared
    /// in a referenced (non-framework) library, not just the app module. Pre-existing
    /// quirk inherited from the name test: a user library literally named
    /// <c>System.*</c>/<c>Microsoft.*</c> reads as framework (its attributes go
    /// unemitted) — accepted, as no real user assembly claims a BCL name.</summary>
    internal bool IsUserModule(Module m) => !IsFrameworkAssemblyName(m.AssemblyName);

    /// <summary>How far <see cref="NoteUnwalkedAttributeRows"/> has visited
    /// <see cref="Classes"/>.</summary>
    private int _unwalkedAttributeCursor;

    /// <summary>Notes what the attribute rows of every user-module class and of every
    /// assembly name, for the attributes whose ctor is reached: those rows render, and each
    /// handle a row names must be declared. A noted Type argument mints
    /// the class it names, and the drain completes its shape, so the notes alternate with
    /// the drain until no class is minted; each class is visited once.</summary>
    private void NoteUnwalkedAttributeRows()
    {
        ReachAssemblyAttributes(reach: false);
        do
        {
            for (; _unwalkedAttributeCursor < Classes.Count; _unwalkedAttributeCursor++)
            {
                var cls = Classes[_unwalkedAttributeCursor];
                if (IsUserModule(cls.Module))
                    ReachClassAttributes(cls, reach: false, CarriesUnretainedRows(cls));
            }
            DrainReachability();
        }
        while (_unwalkedAttributeCursor != Classes.Count);
    }

    /// <summary>Whether the image carries <paramref name="cls"/>'s unretained attribute rows:
    /// every application class is emitted, a library class only once the program reaches a
    /// member of it or allocates it. A library loaded but unused therefore adds no
    /// attribute type-info, and a hot-update base's contract does not move with it.</summary>
    private bool CarriesUnretainedRows(ClassInfo cls)
    {
        if (cls.Module == AppModule || _allocatedRefTypes.Contains(cls))
            return true;
        if (!cls.MembersReady)
            return false;
        foreach (var m in cls.Methods)
            if (Reachable.Contains(m))
                return true;
        return false;
    }

    /// <summary><see cref="ReachAttributesOf"/> over a class's own attributes and those of
    /// its fields, methods, parameters and properties.</summary>
    private void ReachClassAttributes(ClassInfo cls, bool reach, bool unretained = true)
    {
        try
        {
            var reader = cls.Module.Reader;
            var td = reader.GetTypeDefinition(cls.Handle);
            ReachAttributesOf(cls.Module, td.GetCustomAttributes(), reach, unretained);
            foreach (var fh in td.GetFields())
                ReachAttributesOf(cls.Module, reader.GetFieldDefinition(fh).GetCustomAttributes(), reach, unretained);
            foreach (var mh in td.GetMethods())
            {
                var md = reader.GetMethodDefinition(mh);
                ReachAttributesOf(cls.Module, md.GetCustomAttributes(), reach, unretained);
                foreach (var pph in md.GetParameters())
                    ReachAttributesOf(cls.Module, reader.GetParameter(pph).GetCustomAttributes(), reach, unretained);
            }
            foreach (var ph in td.GetProperties())
                ReachAttributesOf(cls.Module, reader.GetPropertyDefinition(ph).GetCustomAttributes(), reach, unretained);
        }
        catch (Exception e) when (!IsMustEscape(e))
        {
            // A class without decodable metadata (e.g. a generic instance) just
            // contributes no reflected attributes.
        }
    }

    /// <summary>Whether a row whose constructor nothing reached still renders, with a null
    /// factory: a metadata read answers from it, and an instance read refuses instead of
    /// answering a set without it.</summary>
    internal bool KeepsUnretainedAttributeRow(DecodedAttribute da) => !Reachable.Contains(da.Ctor);

    /// <summary>Constructs the rows on a class a backend's runtime reads
    /// (<see cref="IEmitBackend.AttributeReadClasses"/>) and on its declared methods.</summary>
    private void ReachBackendAttributeRows(ClassInfo cls)
    {
        if (cls.Handle.IsNil)
            return;
        var reader = cls.Module.Reader;
        var td = reader.GetTypeDefinition(cls.Handle);
        ReachBackendAttributes(cls.Module, td.GetCustomAttributes());
        foreach (var mh in td.GetMethods())
            ReachBackendAttributes(cls.Module, reader.GetMethodDefinition(mh).GetCustomAttributes());
    }

    /// <summary><see cref="ReachAttributesOf"/> over every loaded module's assembly-level
    /// attributes, which follow the element rules so Assembly.GetCustomAttributes can
    /// materialize them. DecodeCustomAttributes bounds this to attribute types defined in
    /// non-framework modules (plus the user-typeof-named framework ones), so a real CoreLib
    /// pulled in with -r contributes almost nothing.</summary>
    private void ReachAssemblyAttributes(bool reach)
    {
        foreach (var module in Modules)
            try
            {
                ReachAttributesOf(module, module.Reader.GetAssemblyDefinition().GetCustomAttributes(), reach);
                ReachAttributesOf(module, module.Reader.GetModuleDefinition().GetCustomAttributes(), reach);
            }
            catch (Exception e) when (!IsMustEscape(e))
            {
                // A module without an assembly definition contributes no attributes.
            }
    }

    /// <summary>Notes what each argument's factory names (<see cref="NoteAttrArg"/>) for
    /// every reflectable custom attribute in the collection, so the argument stays
    /// renderable. With <paramref name="reach"/> it also reaches the attribute's ctor,
    /// named-property setters and allocated type; without, it notes only the rows the emit
    /// side renders: those whose ctor something else reached, and the unretained rows
    /// (<see cref="KeepsUnretainedAttributeRow"/>) when <paramref name="unretained"/>, whose
    /// row names the attribute's type-info.</summary>
    private void ReachAttributesOf(Module module, CustomAttributeHandleCollection handles, bool reach,
        bool unretained = true)
    {
        // A row only another route rendered is read by nothing unless attributes are read.
        bool keep = reach || _reflectionAttrUsed;
        foreach (var da in DecodeCustomAttributes(module, handles, reachedCtorsOnly: !reach && !unretained))
        {
            if (!reach && unretained && KeepsUnretainedAttributeRow(da))
            {
                EnsureCompleted(da.AttrClass);
                NoteTypeIdentityClosure(TypeDesc.MakeClass(da.AttrClass), keepSeed: false);
            }
            if (reach)
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
            }
            var ps = da.Ctor.Signature.ParameterTypes;
            for (int i = 0; i < da.Fixed.Length; i++)
                NoteAttrArg(da.Fixed[i].Type, da.Fixed[i].Value, i < ps.Length && ps[i].IsObject, keep);
            foreach (var na in da.Named)
            {
                // The member may be declared on an attribute BASE class — walk the
                // chain, paired with the emit-side lookup in CppEmitter.RenderNamedArg: a
                // setter emit finds but reach never reached fails its Reachable test and
                // silently drops the whole attribute row, and an object-typed member boxes
                // its value there.
                bool boxed = false;
                if (na.Kind == CustomAttributeNamedArgumentKind.Property && na.Name is { } pn
                    && da.AttrClass.InstanceMethodOnBaseChain("set_" + pn) is { } sm)
                {
                    if (reach)
                        Reach(sm);
                    boxed = sm.Signature.ParameterTypes.Length > 0 && sm.Signature.ParameterTypes[0].IsObject;
                }
                else if (na.Kind == CustomAttributeNamedArgumentKind.Field && na.Name is { } fn
                    && da.AttrClass.InstanceFieldOnBaseChain(fn) is { } f)
                    boxed = f.Type.IsObject;
                NoteAttrArg(na.Type, na.Value, boxed, keep);
            }
        }
    }

    /// <summary>Notes what the factory of one attribute value names, from its
    /// <paramref name="encoded"/> type; <paramref name="boxed"/> when it fills an
    /// object-typed slot or object[] element, which boxes it at that type. The emit-side
    /// pairing is CppEmitter.RenderAttrValue, and every note is emitted and
    /// header-declared before any attribute table renders:
    /// <list type="bullet">
    /// <item>an SZArray's element, at every nesting level, so the array allocates with its
    /// precise <c>ti_arr_&lt;elem&gt;</c> handle — one left on the shared object[] handle
    /// has no interface-dispatch map, so the first LINQ over it inside the attribute ctor
    /// (e.g. <c>inputNames.Count(...)</c>) aborts loudly — plus an enum element's own ti_,
    /// exactly as TypeMetadataEmitter.NoteReflectedType notes for member types;</item>
    /// <item>a boxed enum's ti_, which the box carries;</item>
    /// <item>a Type value's identity closure, so its handle survives tree-shaking.</item>
    /// </list>
    /// Without <paramref name="keep"/> the classes are referenced for their type-info
    /// alone: a row the program cannot read is no sign it reflects over them.</summary>
    private void NoteAttrArg(TypeDesc encoded, object? value, bool boxed, bool keep)
    {
        if (encoded.Kind == TypeKind.SZArray)
        {
            var el = encoded.Element!;
            NoteArrayElementType(el);
            if (el is { Kind: TypeKind.Class, Class: { IsEnum: true } })
                NoteTypeIdentityClosure(el, keep);
            if (value is ImmutableArray<CustomAttributeTypedArgument<TypeDesc>> items)
                foreach (var item in items)
                    NoteAttrArg(item.Type, item.Value, el.IsObject, keep);
            return;
        }
        if (value is TypeDesc type)
            NoteTypeIdentityClosure(type, keep);
        else if (boxed && value is not null && encoded is { Kind: TypeKind.Class, Class: { IsEnum: true } })
            NoteTypeIdentityClosure(encoded, keep);
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
                bool previous = _reachingBindingOnlyBody;
                _reachingBindingOnlyBody = _bindingOnlyBodies.Contains(m);
                _scanningBody = true;
                _scanningPreserveOnlyBody = _preserveOnlyBodies.Contains(m);
                try
                {
                    if (ReachabilityDiagnostics is null)
                        ScanBodyForGenerics(m);
                    else
                        try { ScanBodyForGenerics(m); }
                        catch (Exception ex) when (!IsMustEscape(ex))
                        { ReachabilityDiagnostics.Add((m, ex)); }
                }
                finally
                {
                    _reachingBindingOnlyBody = previous;
                    _scanningBody = _scanningPreserveOnlyBody = false;
                }
                if (_preserveRestored.Count > 0)
                    ApplyRestoredPreservation();
                progress = true;
            }
            if (ReachReachedStrippedGvmCases())
                progress = true;
            if (ReachPendingRuntimeRaisedHeirs())
                progress = true;
            if (ReachPendingBackendDemands())
                progress = true;
            if (ActivateConditionalPreservationPolicies())
                progress = true;
            if (RetainPendingMediumTypes())
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
    private long _virtualCandidatePairs;
    private long _boxedEqualityVisits;

    /// <summary>True if the type's boxed form is allocated somewhere (a reference
    /// type via newobj, or a value type via box). A boxed value type needs an
    /// interface dispatch table in its type-info, unlike one only ever dispatched
    /// via direct constrained calls.</summary>
    public bool IsAllocated(ClassInfo c) => _allocatedRefTypes.Contains(c);

    internal IEnumerable<ClassInfo> AllocatedRefTypes => _allocatedRefTypes;

    /// <summary>The recorded reachability path to <paramref name="m"/> (newest
    /// first), for actionable diagnostics when a reached method cannot be
    /// emitted. Empty for roots.</summary>
    public string ReachChain(MethodInfo m)
    {
        var chain = new List<string>();
        var seen = new HashSet<MethodInfo>();
        for (var p = m; p is not null && seen.Add(p);)
        {
            chain.Add(p.DeclaringClass.FullName + "." + p.Name);
            if (_predTrace.TryGetValue(p, out var predecessor) && predecessor is not null)
                p = predecessor;
            else if (_mediumMethodOwners.TryGetValue(p, out var owner))
            {
                chain.Add("Medium type retention: " + MediumRetentionChain(owner));
                p = null;
            }
            else
                p = null;
        }
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
        // Preservation is seeded before the backend installs the registry trim.
        foreach (var cls in Classes.ToList())
            if (TrimEligibleClass(cls) && (_explicitReflectionKeep.Contains(cls)
                || ReflectionRootMatching(cls) is not null))
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
    /// Rewritten entries may already target an ancestor factory; classify its
    /// allocation rather than the registration name to allow another redirect.
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
    private static PInvokeInfo? ReadPInvoke(Module module, MethodDefinition md)
    {
        var reader = module.Reader;
        if ((md.Attributes & MethodAttributes.PinvokeImpl) == 0)
            return null;
        var import = md.GetImport();
        if (import.Module.IsNil)
            return null; // malformed / vararg pinvoke with no module ref — leave as extern
        string moduleName = reader.GetString(reader.GetModuleReference(import.Module).Name);
        string rawEntryPoint = import.Name.IsNil ? reader.GetString(md.Name) : reader.GetString(import.Name);
        string entryPoint = rawEntryPoint;

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

        int? searchPath = ReadDefaultDllImportSearchPath(reader, md.GetCustomAttributes());
        if (searchPath is null && reader.IsAssembly)
            searchPath = ReadDefaultDllImportSearchPath(
                reader, reader.GetAssemblyDefinition().GetCustomAttributes());

        return new PInvokeInfo(moduleName, rawEntryPoint, entryPoint, import.Attributes,
            searchPath, retMarshal, paramMarshal);
    }

    /// <summary>The first <c>[DefaultDllImportSearchPaths]</c> value in a metadata
    /// attribute set, or null when the attribute is absent. The value blob is the ECMA-335
    /// custom-attribute prolog followed by the enum's Int32 value; decoding it directly
    /// keeps P/Invoke discovery from resolving an otherwise-unused attribute type.</summary>
    private static int? ReadDefaultDllImportSearchPath(
        MetadataReader reader, CustomAttributeHandleCollection attributes)
    {
        foreach (var cah in attributes)
        {
            var ca = reader.GetCustomAttribute(cah);
            if (AttributeTypeName(reader, ca)
                != "System.Runtime.InteropServices.DefaultDllImportSearchPathsAttribute")
                continue;
            var blob = reader.GetBlobReader(ca.Value);
            if (blob.Length < 6 || blob.ReadUInt16() != 0x0001)
                throw new NotSupportedException(
                    "DefaultDllImportSearchPathsAttribute has an invalid value blob");
            return blob.ReadInt32();
        }
        return null;
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

    /// <summary>Distinct module-token and entry-point pairs for build backends
    /// that must construct an import library from a package-native DLL. The
    /// linker manifest alone cannot name exports that the target platform omits
    /// but CoreCLR would resolve lazily only if their managed branch executes.</summary>
    public IReadOnlyList<string> PInvokeLinkSymbols() =>
        PInvokeCalls.Select(m =>
            (Token: LinkLibToken(m.PInvoke!.ModuleName), m.PInvoke.EntryPoint))
            .Where(p => p.Token is not null)
            .Select(p => p.Token + "\t" + p.EntryPoint)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(p => p, StringComparer.Ordinal)
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
            "corefoundation" => null,
            _ => name,
        };
    }

    /// <summary>The exact selectors supplied by <c>--direct-pinvoke</c>. They are
    /// intentionally not normalized: a selector names metadata, not a filename
    /// candidate produced by the platform loader.</summary>
    private readonly HashSet<string> _directPInvokes;

    public bool IsDirectPInvoke(MethodInfo method)
    {
        if (method.PInvoke is not { } pinv)
            return false;
        if (_directPInvokes.Contains("*")
            || _directPInvokes.Contains(pinv.ModuleName)
            || _directPInvokes.Contains(pinv.ModuleName + "!" + pinv.RawEntryPoint))
            return true;

        // These imports bind to symbols supplied by the runtime or the target OS
        // itself. Keep only framework/shim declarations direct; an application or
        // third-party assembly importing the same module still gets resolver-first
        // lazy binding, which is observable (notably a user "libc" resolver on Windows).
        string assembly = method.DeclaringClass.Module.AssemblyName;
        bool framework = IsFrameworkAssemblyName(assembly)
            || assembly is "DnZlib" or "DnBrotli" or "DnHttp";
        return framework && IsRuntimeProvidedPInvokeModule(pinv.ModuleName);
    }

    /// <summary>True for a bodyless <c>[DllImport]</c> method whose call sites lower to a
    /// P/Invoke lowering, either lazy, explicitly direct, or an absent-PAL refusal.
    /// The ONE predicate every asker shares —
    /// the call route (<c>MethodCompiler.TranslateCall</c>) and the address-taken path
    /// (the ldftn arm noting <see cref="NotePInvokeFtnTarget"/>) — so the set of imports
    /// that lower cannot drift between a call site and a delegate method group over the
    /// same import.</summary>
    public bool LowersToPInvoke(MethodInfo m)
    {
        if (m.Rva != 0 || m.PInvoke is null)
            return false;

        // User/library imports are the dynamic-loading surface. Framework imports
        // remain behind their intrinsic/refusal boundary; absent runtime modules
        // use a callable refusal through this same route.
        if (!IsFrameworkAssemblyName(m.DeclaringClass.Module.AssemblyName)
            || IsDirectPInvoke(m) || IsAbsentGlobalizationImport(m))
            return true;

        // A concrete cut lowering owns both calls and addresses. The intrinsic-type
        // umbrella alone does not promise a lowering for every private QCall member.
        return IsAbsentCoreClrQCall(m)
            && (!CoreIntrinsics.TryFindCutRow(m, out var row)
                || row.EmitArm == InterceptEmitArm.IntrinsicDispatch);
    }

    // Invariant globalization has no ICU PAL. Keep retained imports callable,
    // but refuse execution unless the caller explicitly supplies native binding.
    internal bool IsAbsentGlobalizationImport(MethodInfo method) =>
        CoreIntrinsics.PiAbsentGlobalizationNative.Matches(method) && !IsDirectPInvoke(method);

    internal bool IsAbsentCoreClrQCall(MethodInfo method) =>
        CoreIntrinsics.PiAbsentCoreClrQCall.Matches(method) && !IsDirectPInvoke(method);

    internal static string AbsentGlobalizationImportMessage(PInvokeInfo import) =>
        ("the native import " + import.ModuleName + "!" + import.RawEntryPoint
        + " requires the ICU globalization PAL, which a dn2cpp-transpiled binary does not provide. "
        + "Use the supported globalization APIs, a NativeImplementation shim, or an explicit --direct-pinvoke binding.")
        .Replace("\\", "\\\\").Replace("\"", "\\\"");

    internal static string AbsentCoreClrQCallMessage(PInvokeInfo import) =>
        ("the native import " + import.ModuleName + "!" + import.RawEntryPoint
        + " requires the CoreCLR runtime, which a dn2cpp-transpiled binary does not provide. "
        + "Use a supported managed API, a NativeImplementation shim, or an explicit --direct-pinvoke binding.")
        .Replace("\\", "\\\\").Replace("\"", "\\\"");

    /// <summary>ldftn/delegate targets that are bodyless P/Invokes whose call sites
    /// use the same lazy/direct lowering (<see cref="LowersToPInvoke"/>), e.g.
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
            AddReachable(m.EnsureSignature()); // reached => decoded, as in Reach
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
    /// <c>runtime/core/intrinsics/dn2cpp_apple_crypto_digest.cpp</c> plus the
    /// SystemNative-ABI-only random adapter in
    /// <c>runtime/core/platform/posix/dn2cpp_apple_crypto_random.cpp</c>. The adapter is
    /// reused on Web with the wasm entropy backend despite its source-tree location;
    /// the CoreFoundation framework path (Interop.AppleCrypto's cctor) resolves to the
    /// real framework via <c>-framework CoreFoundation</c> on Apple hosts.
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
    /// PAL's <c>SystemNative_GetHostName</c>). <c>iphlpapi.dll</c> supplies the
    /// interface-name-to-index conversion used by named IPv6 scope parsing, and
    /// <c>secur32.dll</c> supplies <c>Environment.UserName</c>'s
    /// <c>GetUserNameExW</c>. Each string
    /// is the exact
    /// <c>ModuleRef</c> spelling — matched case-sensitively, so <c>BCrypt.dll</c> is
    /// not lowercase — as it appears in the win-x64 assembly that declares the
    /// P/Invoke: CoreLib for <c>kernel32</c>/<c>ntdll</c>/<c>ole32</c>/<c>BCrypt</c>,
    /// but <c>System.Net.NameResolution.dll</c> for <c>ws2_32.dll</c> (CoreLib names
    /// no winsock import; NameResolution is where <c>gethostname</c>/<c>WSAStartup</c>/
    /// <c>WSACleanup</c> are declared). Of these, <c>ws2_32</c>, <c>iphlpapi</c>,
    /// <c>ntdll</c> and
    /// <c>BCrypt</c> are outside CMake's default Windows link set (which covers
    /// <c>kernel32</c>/<c>ole32</c>), so <c>runtime/CMakeLists.txt</c> links
    /// <c>ws2_32</c>, <c>iphlpapi</c> and <c>secur32</c> explicitly on the WIN32 arm — without that
    /// the lowered call sites resolve here and then fail at C++ link time. Nothing
    /// reaches an <c>ntdll</c> / <c>BCrypt</c> call today; one that did would need
    /// the same explicit link.
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
    /// User declarations do not use this fixed set to decide admission: all reachable
    /// imports are accepted and bind lazily unless <c>--direct-pinvoke</c> selects them.
    /// </para></summary>
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
            or "ws2_32.dll" or "advapi32.dll" or "iphlpapi.dll" or "secur32.dll"
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
    /// (see <see cref="AssemblyLoadSet"/> on why the load set, not reachability, is
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
    internal static readonly (string Shim, string Trigger)[] DefaultReferences =
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
        /// simple-name dedupe in <see cref="AssemblyLoadSet"/> exists to
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
    /// <see cref="DefaultReferences"/> declaration order, for the
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
            foreach (var (shim, _) in DefaultReferences)
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
        foreach (var (shim, _) in DefaultReferences)
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
            var names = new string[DefaultReferences.Length];
            for (int i = 0; i < DefaultReferences.Length; i++)
                names[i] = DefaultReferences[i].Shim;
            return string.Join(", ", names);
        }
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
    // (see DefaultReferences / InjectDefaultRefs), not an optional -r: it ships beside the CLI
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
            var mi = reader.GetMethodImplementation(mih);
            if (mi.MethodDeclaration.Kind == HandleKind.MemberReference
                && MemberRefParentTypeName(module, (MemberReferenceHandle)mi.MethodDeclaration) == "System.Object")
            {
                var declaration = reader.GetMemberReference((MemberReferenceHandle)mi.MethodDeclaration);
                string name = reader.GetString(declaration.Name);
                string bodyName = mi.MethodBody.Kind == HandleKind.MethodDefinition
                    ? reader.GetString(reader.GetMethodDefinition((MethodDefinitionHandle)mi.MethodBody).Name)
                    : reader.GetString(reader.GetMemberReference((MemberReferenceHandle)mi.MethodBody).Name);
                // The intrinsic Object fallback dispatches by name. It cannot
                // represent a renamed override without the real slot declaration.
                if (bodyName != name && declaration.Parent.Kind == HandleKind.TypeReference
                    && ResolveTypeRef(module, (TypeReferenceHandle)declaration.Parent) is null)
                    throw new NotSupportedException($"{cls.FullName}: renamed MethodImpl body '{bodyName}' "
                        + $"for System.Object::{name} requires a CoreLib reference");
                // A normal destructor is wired by EffectiveFinalize even when
                // Object is not loaded. Renamed bodies need the explicit map.
                if (name == "Finalize" && bodyName == name)
                    continue;
            }
            // A real CoreLib has MethodImpls pointing at members we don't model.
            // Skip those for reference assemblies; the app module stays strict.
            try
            {
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
                // not modeled as MethodInfo, so the row cannot key this map.
                // Interface GVM dispatch reads the MethodImpl row when binding
                // the template; static virtual dispatch resolves its template
                // by name. Skip this row rather than trying to resolve it here.
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
            catch (NotSupportedException e) when (!IsMustEscape(e)
                && (cls.Module != AppModule || ContainsCanonPlaceholder(cls)))
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
            if (cls.IsInterface)
            {
                slotOwners[cls] = new List<MethodInfo>();
                continue;
            }
            // Delegates omit a runtime vtable, but class binding and selected-method
            // identity still need the same completed override slots as other classes.
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

            int inherited = owners.Count;
            var classOverrides = ClassOverrideDecls(cls);
            foreach (var m in cls.Methods)
            {
                if (!m.IsVirtual)
                    continue;
                int slot = ExplicitBaseSlot(cls, m, classOverrides, owners.Count);
                if (slot < 0 && !m.IsNewSlot)
                    slot = ImplicitBaseSlot(cls, m, owners, inherited);
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
