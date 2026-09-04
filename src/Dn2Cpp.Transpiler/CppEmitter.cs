using System.Reflection.Metadata;
using System.Text;

namespace Dn2Cpp;

/// <summary>Emits the generated C++ sources: a shared <c>generated.h</c> header (managed
/// type layouts + external-linkage declarations) and one-or-more translation units
/// (<c>generated.cpp</c> with the global data + entry point, plus <c>generated_N.cpp</c>
/// method-body chunks once the bodies exceed a per-file byte budget, for parallel
/// compilation). Target-specific output (console entry point vs. Godot GDExtension tables)
/// is delegated to an <see cref="IEmitBackend"/>, keeping this core emitter free of any
/// Godot knowledge.</summary>
internal sealed partial class CppEmitter
{
    private readonly Compilation _c;
    private readonly IEmitBackend _backend;
    private readonly LiteralPool _literals = new();
    private int _parallelBodyPeak;
    private int _parallelBodyEligible;
    private int _parallelBodySerial;
    private bool _parallelBodyStatsReported;
    // Immutable body shape only. Dynamic exclusions (SharedImpl, ftn-target
    // registries and replacement bodies) are checked again in every pass.
    private Dictionary<MethodInfo, bool>? _parallelBodyPureShape;
    // Tiny scalar bodies need several work items per worker to amortize the
    // window's publish/join barrier and worker-pool bookkeeping.
    private const int ParallelBodiesPerWorker = 8;

    /// <summary>Automatic mode grows from two workers to the host processor
    /// count only when this window has enough work to amortize them. Explicit
    /// --jobs remains the exact operator-selected upper bound.</summary>
    private int BodyCompileWorkerLimit(int workCount)
    {
        int limit = System.Math.Min(_c.BodyCompileJobs, workCount);
        if (_c.BodyCompileJobsRequested != 0)
            return limit;
        int adaptive = (workCount - 1) / ParallelBodiesPerWorker + 1;
        return System.Math.Min(limit, System.Math.Max(2, adaptive));
    }

    /// <summary>One worker result slot. Workers only write their own slot; the
    /// coordinator reads and commits the slots in method order.</summary>
    private sealed class ParallelBodyResult
    {
        internal string? Body;
        internal Exception? Error;
        internal List<(MethodInfo Callee, bool RgctxPassable)>? SharedDirectCallees;
    }

    /// <summary>A resident, bounded worker set used by one body-compilation round.
    /// The coordinator publishes one fixed-size window at a time and waits for
    /// every worker before touching shared compilation state again.</summary>
    private sealed class BodyWorkerPool
    {
        private readonly object _gate = new();
        private readonly Thread[] _threads;
        private Action<int>? _work;
        private int _workCount;
        private int _nextWork;
        private int _workersRemaining;
        private int _generation;
        private int _activeWorkers;
        private int _peakActiveWorkers;
        private Exception? _workerError;
        private bool _stopping;

        internal int WorkerCount => _threads.Length;

        internal BodyWorkerPool(int workerCount)
        {
            _threads = new Thread[workerCount];
            for (int i = 0; i < _threads.Length; i++)
                _threads[i] = new Thread(WorkerLoop);
            int started = 0;
            try
            {
                for (; started < _threads.Length; started++)
                    _threads[started].Start();
            }
            catch
            {
                lock (_gate)
                {
                    _stopping = true;
                    Monitor.PulseAll(_gate);
                }
                for (int i = 0; i < started; i++)
                    _threads[i].Join();
                throw;
            }
        }

        internal int Execute(int count, Action<int> work)
        {
            lock (_gate)
            {
                _work = work;
                _workCount = count;
                _nextWork = 0;
                _workersRemaining = _threads.Length;
                _activeWorkers = 0;
                _peakActiveWorkers = 0;
                _workerError = null;
                _generation++;
                Monitor.PulseAll(_gate);
                while (_workersRemaining != 0)
                    Monitor.Wait(_gate);
                _work = null;
                if (_workerError is { } error)
                    throw error;
                return _peakActiveWorkers;
            }
        }

        private void WorkerLoop()
        {
            int observedGeneration = 0;
            while (true)
            {
                lock (_gate)
                {
                    while (!_stopping && observedGeneration == _generation)
                        Monitor.Wait(_gate);
                    if (_stopping)
                        return;
                    observedGeneration = _generation;
                }

                try
                {
                    while (true)
                    {
                        Action<int> work;
                        int index;
                        lock (_gate)
                        {
                            if (_nextWork == _workCount)
                                break;
                            index = _nextWork++;
                            work = _work!;
                            _activeWorkers++;
                            if (_activeWorkers > _peakActiveWorkers)
                                _peakActiveWorkers = _activeWorkers;
                        }
                        try
                        {
                            work(index);
                        }
                        finally
                        {
                            lock (_gate)
                                _activeWorkers--;
                        }
                    }
                }
                catch (Exception ex)
                {
                    lock (_gate)
                        _workerError ??= ex;
                }
                finally
                {
                    lock (_gate)
                    {
                        _workersRemaining--;
                        if (_workersRemaining == 0)
                            Monitor.PulseAll(_gate);
                    }
                }
            }
        }

        internal void Stop()
        {
            lock (_gate)
            {
                _stopping = true;
                Monitor.PulseAll(_gate);
            }
            foreach (var thread in _threads)
                thread.Join();
        }
    }
    // Per-enum Object.ToString function bodies. Accumulated while emitting
    // the enum type-infos (which add their member-name string literals to the pool)
    // and flushed after the literal table, since the bodies reference those literals.
    private readonly System.Text.StringBuilder _enumToStringFns = new();
    private int _attrSeq;

    /// <summary>The classes whose layout/metadata is actually emitted. With a
    /// real CoreLib pulled in via -r, <see cref="Compilation.Classes"/> holds
    /// thousands of types; only the ones the program reaches (plus the app
    /// module and the transitive closure they depend on) are materialized. Set
    /// in <see cref="Emit"/> once the reachable set is known.</summary>
    private HashSet<ClassInfo> _emit = new();
    // Reference-only types emitted as opaque empty layouts (see Emit).
    private HashSet<ClassInfo> _opaque = new();

    /// <summary>The opaque VALUE-type shells whose CLR layout extent the model could not
    /// compute (<see cref="TryStructExtent"/> null). Their shell stays empty, so
    /// <c>sizeof</c> — and the <c>instanceSize</c> stamped from it — reads 1, which is
    /// indistinguishable from a genuinely field-less struct; hence a FLAG
    /// (<c>DN2CPP_TF_LAYOUT_UNKNOWN</c>) rather than a test on the number, and every
    /// size/stride reader throws naming the type instead of answering 1 — the same
    /// "a stripped type must throw, not answer empty" shape as
    /// <c>DN2CPP_TF_METADATA_STRIPPED</c>.
    ///
    /// <para>Populated at the shell-pad site in <see cref="EmitOneStruct"/>, so the pad and
    /// the flag cannot disagree; sound because <c>EmitStructs</c> runs before
    /// <c>EmitTypeInfos</c> and every opaque type in the emit set gets a struct.</para>
    ///
    /// <para>Keyed on the C++ STRUCT NAME, never on the <see cref="ClassInfo"/>: a grouped
    /// specialization redirects <see cref="ClassInfo.CppStructName"/> to its canonical
    /// owner's, so identity would flag the owner and miss every real member. Ask the
    /// question the stamp asks.</para></summary>
    private readonly HashSet<string> _layoutUnknown = new(StringComparer.Ordinal);

    /// <summary>Every struct name the layout emission actually RENDERED — each field's
    /// storage type and each struct header's base — checked at the end of
    /// <see cref="EmitStructs"/> against the names it declared. A layout that spells an
    /// undeclared <c>t_</c> name is otherwise a C++ error with no cause attached.</summary>
    private readonly List<(ClassInfo Cls, string Member, string Cpp)> _renderedStructRefs = new();

    /// <summary>Whether <paramref name="cls"/>'s type-info must carry
    /// <c>DN2CPP_TF_LAYOUT_UNKNOWN</c> — see <see cref="_layoutUnknown"/>.</summary>
    internal bool HasUnknownLayoutExtent(ClassInfo cls) => _layoutUnknown.Contains(cls.CppStructName);

    /// <summary>Skipped-body methods that received a synthesized engine-call
    /// body (<see cref="IEmitBackend.WantsSyntheticBody"/>, hot-update base
    /// builds only): the reflection member table wires their fnPtr/invoker like
    /// any emitted method, so the hot-update interpreter can bind them. Always
    /// empty in a normal build.</summary>
    private readonly HashSet<MethodInfo> _syntheticBodies = new();

    /// <summary>Generic reflection: CLR open-definition FullName (e.g.
    /// "System.Collections.Generic.List`1") → the C++ symbol of its synthetic
    /// open-definition type-info. Populated in <see cref="EmitTypeInfos"/> so the closed
    /// instantiations and the type registry can reference the shared definition handle.</summary>
    private readonly Dictionary<string, string> _genericDefSyms = new(System.StringComparer.Ordinal);

    /// <summary>Per-element array type-infos: CLR array name (e.g.
    /// "System.Int32[]") → the <c>&amp;ti_arr_&lt;…&gt;</c> handle expression. Populated by
    /// <see cref="EmitArrayTypeInfos"/> so <see cref="EmitTypeRegistry"/> can resolve
    /// <c>Type.GetType("T[]")</c> to it.</summary>
    private readonly Dictionary<string, string> _arrayTypeSyms = new(System.StringComparer.Ordinal);

    /// <summary>The intrinsic-modeled classes given a minimal <c>ti_</c> because a lowering
    /// named their own handle and nothing else emits them
    /// (<c>TypeMetadataEmitter.ReferencedIntrinsicTypeInfos</c>, which fills this). Kept so
    /// <see cref="EmitTypeRegistry"/> can key those handles by name; ordered there.</summary>
    private IReadOnlyList<ClassInfo> _referencedIntrinsicTis = System.Array.Empty<ClassInfo>();

    /// <summary>The runtime-raised exception types this emission carries real metadata
    /// for (see <see cref="ClassInfo.CppTypeInfoName"/>): the startup bind copies each
    /// one's emitted <c>tibind_</c> object into the runtime's own handle. Collected in
    /// type-info render order (TopoOrder), so the table is deterministic.</summary>
    private readonly List<ClassInfo> _typeBinds = new();

    /// <summary>Every <c>ti_&lt;T&gt;</c> / <c>ti_arr_&lt;T&gt;</c> type-info symbol this
    /// emission DEFINES — populated as each is forward-declared / emitted (the class,
    /// enum, array and referenced-intrinsic type-info loops). The defined half of
    /// <see cref="AssertNamedTypeInfosDefined"/> and of <see cref="TypeInfoRef"/>; kept as
    /// strings, because the linker only ever sees names.</summary>
    private readonly HashSet<string> _definedTypeInfoSyms = new(System.StringComparer.Ordinal);

    /// <summary>Whether the type-info declaration block has finished, i.e. whether
    /// <see cref="_definedTypeInfoSyms"/> is complete and can be believed. Set once, at the
    /// end of that block; <see cref="TypeInfoRef"/> and <see cref="ArrayTypeInfoDeclared"/>
    /// refuse to answer before it, rather than answering "undefined" about a set that is
    /// still being filled — an emitter-side <c>&amp;ti_</c> mouth moved ahead of the
    /// declarations must fail loudly, not silently stop being checked.
    ///
    /// <para>No input reaches those two throws: every mouth of the funnel is textually below
    /// the <c>EmitTypeInfos</c> call in <see cref="Emit"/>'s straight-line body, and the
    /// branches in between select WHETHER a mouth runs, never WHEN. So the guards check an
    /// EDIT — they fire for whoever moves a mouth above <c>EmitTypeInfos</c> or writes a new
    /// one there, the one change that would silently turn
    /// <see cref="ArrayTypeInfoDeclared"/> into a System.Object degrade for every array
    /// member type in the program.</para></summary>
    private bool _typeInfoSymsFinal;

    /// <summary>Whether a class is emitted opaquely (an intrinsic reference type
    /// like Object/Exception, or a dead-branch reference-only type) — empty
    /// layout rooted at Dn2CppObject, no fields/interfaces/vtable walked.</summary>
    private bool IsOpaque(ClassInfo c) => _opaque.Contains(c) || CoreIntrinsics.IsIntrinsicType(c.FullName);

    /// <summary>The emitter-minted half of <see cref="IsOpaque"/>: a shell emitted for a
    /// type reached by a type token alone (or pulled in as one such shell's base /
    /// canonical owner), as opposed to a type whose C++ representation the runtime owns.
    /// The two halves part company wherever a reader needs the shell's CLR facts but must
    /// not touch an intrinsic's hand-written metadata — the relation-only interface rows
    /// of <c>TypeMetadataEmitter.RenderItfTables</c> are the case that forced the
    /// split.</summary>
    private bool IsOpaqueShell(ClassInfo c) => _opaque.Contains(c) && !CoreIntrinsics.IsIntrinsicType(c.FullName);

    /// <summary>The other half: a class whose C++ shape and metadata the runtime owns
    /// (String, Int32, Object, …). Emitted tables must never claim to describe one.</summary>
    private bool IsIntrinsicShaped(ClassInfo c) => CoreIntrinsics.IsIntrinsicType(c.FullName);

    /// <summary>A canonical-owner-world class (shared generics): its struct
    /// layout and any retained shared bodies emit, but its per-instantiation
    /// metadata — type-info, vtable, reflection tables, statics, registry row —
    /// never does: no runtime instance ever carries it, and the taint rules
    /// keep shared bodies from naming it. Canonical interfaces are the
    /// exception — their type-info is the group-wide dispatch handle the
    /// interface alias rows and the shared bodies' probes share — but their
    /// reflection tables and registry rows are as unreachable as any other
    /// canonical metadata (see <see cref="IsCanonicalWorld"/>).</summary>
    private bool SkipsCanonicalMetadata(ClassInfo c) =>
        _c.SharedGenericsEnabled && !c.IsInterface && Compilation.ContainsCanonPlaceholder(c)
        && !IsRuntimeTemplateLevel(c);

    /// <summary>Any canonical-owner-world class, interface or not: reflection
    /// member/field/property tables, generic-definition handles and type-name
    /// registry rows never emit for these (no runtime Type ever exposes
    /// them).</summary>
    private bool IsCanonicalWorld(ClassInfo c) =>
        _c.SharedGenericsEnabled && Compilation.ContainsCanonPlaceholder(c)
        && !IsRuntimeTemplateLevel(c);

    /// <summary>An eligible runtime-instantiation template chain level (see
    /// <c>Compilation.JudgeRuntimeTemplates</c>): the one canonical-world class
    /// kind that emits full metadata — ti_, vtable, member tables, flagged
    /// SHARED_CANON|RUNTIME_TEMPLATE — because a runtime MakeGenericType clones
    /// it. It still stays out of the static type registry, the hot-update ABI
    /// manifests and the GVM dispatchers: what runtime code observes is a
    /// synthesized clone, never the template itself. Empty until
    /// FinalizeSharedGenerics runs, which is before any metadata renders.</summary>
    private bool IsRuntimeTemplateLevel(ClassInfo c) =>
        _c.EligibleRuntimeTemplateLevels.Count > 0 && _c.EligibleRuntimeTemplateLevels.Contains(c);

    /// <summary>Hot-update base build (<c>--hotupdate-base</c>): emit the
    /// ABI-contract hash constant into the Data TU and hand the base-abi.json
    /// sidecar back through <see cref="EmittedSources.BaseAbiJson"/>. Off (the
    /// default), the output is byte-identical to a build without the flag.</summary>
    private readonly bool _hotUpdateBase;

    /// <summary>Whether this is a hot-update base build, exposed so a backend's
    /// epilogue emission can adapt — e.g. the Godot engine-virtual trampolines
    /// route their inner call through the receiver's vtable slot (where the
    /// patch loader installs interpreted overrides) instead of a direct static
    /// call, only in a build that can be patched.</summary>
    public bool HotUpdateBase => _hotUpdateBase;

    public CppEmitter(Compilation c, IEmitBackend backend, bool hotUpdateBase = false)
    {
        _c = c;
        _backend = backend;
        _hotUpdateBase = hotUpdateBase;
    }

    /// <summary>Loaded classes that are actually emitted, one per mangled C++
    /// name, in stable <see cref="Compilation.Classes"/> order.</summary>
    // Dedupe by CppName: an internal type compiled into several assemblies via shared
    // source (e.g. System.Collections.Generic.ValueListBuilder<T>) is a distinct .NET
    // type per assembly yet mangles to the same C++ name, and every type-level symbol
    // (t_/ti_/ty_/vt_/fldget_/methtab_...) derives from CppName — so emitting both would
    // redefine each one. Their layout and metadata are byte-identical shared source.
    // Method BODIES are unaffected (CompileReachableBodies iterates by MethodInfo
    // identity). First-in-Classes-order wins, keeping the self-host emit a fixpoint.
    //
    // Cached because the property is read many times per emit and each evaluation walks
    // thousands of classes. Correct because every _emit mutation goes through EmitAdd /
    // the assignment in Emit() (both null it) and _c.Classes is append-only, so a Count
    // mismatch forces a re-derive.
    private List<ClassInfo>? _emittedCache;
    private int _emittedCacheClassCount;

    /// <summary>Whether <see cref="ComputeEmitted"/>'s app-module signature closure has already
    /// run. It is a ONE-STEP closure and must stay one, but the set is now computed twice — see
    /// the comment at the loop it guards.</summary>
    private bool _sigClosureDone;

    private IEnumerable<ClassInfo> EmittedClasses
    {
        get
        {
            if (_emittedCache is null || _emittedCacheClassCount != _c.Classes.Count)
            {
                var classes = _c.Classes;
                var list = new List<ClassInfo>();
                for (int i = 0; i < classes.Count; i++)
                    if (_emit.Contains(classes[i]))
                        list.Add(classes[i]);
                // By SortKey, not by position in Classes: this list seeds the order every
                // declaration comes out in (TopoOrder walks it), and through that the
                // numbering of the per-chunk interned metadata pools. Discovery order is
                // not a property of the input, so it must not reach the output.
                list.Sort(ClassInfo.CompareByOrder);
                // Same tie-break, now content-derived: the smallest SortKey wins a
                // CppName. (The twins are shared-source internal generics linked into
                // several assemblies — see above. Which copy wins was a discovery
                // artifact; now it is the lowest module and row.)
                var seen = new HashSet<string>();
                var deduped = new List<ClassInfo>();
                foreach (var c in list)
                    if (seen.Add(c.CppName))
                        deduped.Add(c);
                _emittedCache = deduped;
                _emittedCacheClassCount = classes.Count;
            }
            return _emittedCache;
        }
    }

    /// <summary>Adds to <see cref="_emit"/>, invalidating the
    /// <see cref="EmittedClasses"/> cache. Every membership change of _emit must
    /// go through this (or reassign _emit and null the cache) so the cached list
    /// can never go stale.
    ///
    /// <para>It is also where being emitted starts meaning being completed. A class in this
    /// set gets a struct, a type-info, a vtable and reflection tables, and those are built
    /// from its members — so pull them here, at the one door into the set, rather than at
    /// each of the dozen renderers downstream, every one of which would otherwise have to
    /// remember to. Membership in _emit is the whole specification of which specializations
    /// have to pay for their methods: the rest are named by nothing the program emits, and
    /// they keep their shells.</para></summary>
    private bool EmitAdd(ClassInfo c)
    {
        _emittedCache = null;
        _c.EnsureCompleted(c);
        return _emit.Add(c);
    }

    /// <summary>Computes the set of classes whose struct/vtable/type-info must be
    /// emitted: the app module, every class that declares a reachable method or
    /// appears in a reachable method's signature, and the transitive closure of
    /// their base classes, interfaces and field types.</summary>
    private HashSet<ClassInfo> ComputeEmitted()
    {
        // The layout closure runs BEFORE the shareability verdicts (AGENTS.md): a
        // field-type decode here mints closed generics, and an instantiation nothing has
        // grouped is emitted as itself rather than through its canonical owner — so a
        // closure taken mid-Planning would make the output a function of when a field
        // happened to be read. This assert closes the hole the Emit-path phase
        // transitions leave: the call alone drifting behind EnterPhase(Planning).
        if (_c.Phase is not (EmitPhase.LayoutClosure or EmitPhase.Emission))
            throw new InvalidOperationException(
                $"emit protocol: ComputeEmitted in phase {_c.Phase} (legal: LayoutClosure, Emission)");
        var set = new HashSet<ClassInfo>();
        var queue = new Queue<ClassInfo>();
        void Add(ClassInfo? c)
        {
            // Task-family async types are provided by the runtime header, not
            // emitted (no struct/vtable/type-info/fields). They appear only as
            // field/parameter types, which CppTypes maps to the runtime struct
            // by name.
            if (c is { IntrinsicCppName: not null })
                return;
            if (c is not null && set.Add(c))
                queue.Enqueue(c);
        }
        void AddType(TypeDesc t)
        {
            switch (t.Kind)
            {
                case TypeKind.Class:
                    Add(t.Class);
                    break;
                case TypeKind.SZArray:
                case TypeKind.MDArray:
                case TypeKind.ByRef:
                case TypeKind.Pointer:
                    AddType(t.Element!);
                    break;
            }
        }

        foreach (var c in _c.Classes)
            if (c.Module == _c.AppModule)
                Add(c);
        foreach (var m in _c.Reachable)
        {
            Add(m.DeclaringClass);
            AddType(m.Signature.ReturnType);
            foreach (var p in m.Signature.ParameterTypes)
                AddType(p);
        }
        // Full-layout emit roots recorded during body compilation: a
        // static-field owner whose static-field symbol a body references (the
        // <>O method-group cache, which has no .cctor so ReachCctor missed it),
        // or a by-value struct a body accesses by field. These need their real
        // fields, so they seed the emit set as normal (non-opaque) classes.
        foreach (var c in _c.ForceEmittedClasses)
            Add(c);

        // An app-module class's reflection tables list every member it declares — only a
        // reference assembly's unreached members are trimmed (BuildMemberTable) — and every
        // row SPELLS its member's return and parameter types. The closure has to meet them
        // HERE: a signature first decoded during emission mints a specialization the set was
        // already closed without, and the row then falls back to reporting object.
        //
        // One step, not a fixpoint, and that is the termination argument: what these
        // signatures name joins the emit set but is not itself walked for members, so a type
        // whose member names a deeper instantiation of ITSELF stops instead of recursing.
        // Once per EMITTER for the same reason — Emit() computes the set twice and the seed
        // is "every app-module class", so a specialization minted on the first call would be
        // walked on the second: a fixpoint assembled out of two one-steps. The
        // transpiler-limits gate counts the Box types precisely to catch that.
        if (!_sigClosureDone)
        {
            _sigClosureDone = true;
            foreach (var c in set.Where(c => c.Module == _c.AppModule).ToList())
            {
                if (c.IsEnum || IsOpaque(c) || IsCanonicalWorld(c))
                    continue; // RenderMemberTables skips these: no member table, no types spelled
                // A typeof-only specialization arrives here still deferred — nothing
                // reached its members — and this walk IS an ask (its table's rows are
                // about to spell these signatures), so pull, as the dequeue below does
                // for every other set member. Safe against the no-pull rule above: the
                // ToList() snapshot is closed, so decode-appended classes cannot shift it.
                _c.EnsureCompleted(c);
                foreach (var m in c.Methods)
                {
                    if (m.Name == ".cctor")
                        continue; // neither a reflected method nor a reflected constructor
                    if (_c.SharedGenericsEnabled
                        && m.Context.MethodArgs.Any(Compilation.ContainsCanonPlaceholder))
                        continue; // not a real member; its parameter table would name the canonical world
                    AddType(m.Signature.ReturnType);
                    foreach (var p in m.Signature.ParameterTypes)
                        AddType(p);
                }
            }
        }

        while (queue.Count > 0)
        {
            var c = queue.Dequeue();
            // Emitting this class means emitting its vtable and its reflection tables, so
            // its members have to exist. Pull them here, once per emitted class — this and
            // EmitAdd are the two doors into the emit set, and together they are the whole
            // of "if we emit it, we decoded it". The seeding loops above must NOT pull:
            // one of them walks Compilation.Classes, and decoding appends to it.
            _c.EnsureCompleted(c);
            Add(c.BaseClass);
            // A grouped specialization's struct layout is the canonical
            // owner's (CppStructName redirects), so every emitted alias pulls
            // its owner's layout in even when no owner method survived sharing.
            if (c.SharedOwner is { } owner)
                Add(owner);
            // An intrinsic reference type (System.Object/Exception) is emitted as
            // an opaque empty struct — its methods are inlined and its real
            // fields are never touched. Walking them would drag the BCL's
            // private exception/serialization plumbing (Exception._data ->
            // Dictionary -> randomized hashing, …) into the layout closure.
            if (CoreIntrinsics.IsIntrinsicType(c.FullName))
                continue;
            foreach (var itf in c.Interfaces)
                Add(itf);
            foreach (var f in c.Fields)
                AddType(f.Type);
        }
        return set;
    }

    /// <summary>Emits the whole program. <paramref name="writeChunk"/> takes each
    /// body/metadata translation unit (fileName, text) the instant it is sealed —
    /// emission streams them out rather than holding the program's text to the end (see
    /// <see cref="CppOutput"/>) — and the returned <see cref="EmittedSources"/> carries
    /// only what is left: generated.h and generated.cpp.</summary>
    internal EmittedSources Emit(Action<string, string> writeChunk, int splitBytes = 1 << 20)
    {
        // The output sink exists before the bodies do, because bodies are routed into it
        // AS THEY COMPILE rather than collected and replayed. A body's destination is a
        // property of the method, not of the text (MethodCompiler.EmitsInline reads the
        // input assembly's IL length), so nothing has to be seen twice — and the whole
        // program's body text never sits on the heap between the two.
        var o = new CppOutput(splitBytes, writeChunk);
        // The per-signature dispatch-trap thunks are defined inline in the header
        // (SlotTrapThunk), minted lazily wherever a table needs one.
        _trapThunkHeader = o.Header;
        // Inline-promoted bodies close the header, so they alone are buffered. They are
        // bounded by construction: only bodies of at most 128 IL bytes are promoted.
        var inlineBodies = new StringBuilder();
        // Single-TU mode (DN2CPP_SPLIT_BYTES=0) cannot stream: bodies share generated.cpp
        // with the data definitions they must follow, and those are not emitted yet. It
        // keeps the old collect-then-replay path, byte-identically.
        List<string>? unsplitBodies = splitBytes <= 0 ? new List<string>() : null;
        // The one consumer that still needs body text after emission, and only in DEBUG /
        // DN2CPP_SHARED_ASSERT: its forbidden-symbol set is derived from _c.Classes, which
        // grows while bodies compile, so it cannot be checked body-by-body. It only ever
        // looks at canonical bodies, so only those are teed.
        var canonicalBodies = new List<(MethodInfo M, string Text)>();
        bool teeCanonical = _c.SharedGenericsEnabled && SharedAssertEnabled;

        void EmitBody(MethodInfo m, string body)
        {
            // Scan for [HotPath(NoAlloc)] closure verification BEFORE the inline/hot routing,
            // so an inline-promoted body is scanned too. A no-op unless a NoAlloc method
            // armed recording. The body text as-emitted is the ground truth for what it
            // allocates or dispatches.
            _c.RecordHotBodyFacts(m, body);
            if (teeCanonical && Compilation.IsCanonicalMethod(m))
                canonicalBodies.Add((m, body));
            if (MethodCompiler.EmitsInline(m))
                inlineBodies.AppendLine(body);
            else if (m.FastMath)
                // FastMath implies IsHotPath (one attribute), so this arm sits
                // ahead of the plain one: the two hot TUs partition the marked
                // set and a body lands in exactly one of them.
                o.AppendHotFastBody(body);
            else if (m.IsHotPath)
                // Routed even in single-TU mode: the dedicated TU is what the
                // build attaches the stronger per-file flags to, and that
                // contract must not silently dissolve under DN2CPP_SPLIT_BYTES=0.
                o.AppendHotBody(body);
            else if (unsplitBodies is not null)
                unsplitBodies.Add(body);
            else
                o.AppendBody(body);
        }

        // Compile bodies first: they record reference-only types (dead-branch
        // casts) via Compilation.ReferencedTypes, which feed the opaque emit set
        // below. CompileReachableBodies drives this to a fixpoint.
        var compiledMethods = new List<MethodInfo>();
        if (_c.SharedGenericsEnabled)
        {
            // Canonical shared generics: a planning pass first compiles every reachable body
            // (canonical owner bodies under taint trial) so the instantiation/reachability
            // fixpoint and the shareability verdicts are final before any text is kept.
            // FinalizeSharedGenerics then assigns MethodInfo.SharedImpl and drops undonated
            // canonical bodies from Reachable; the real pass below re-compiles the survivors
            // with every direct call bound through Emittable. Both passes traverse the same
            // structures in the same order and all side effects are idempotent set-adds, so
            // the kept output is deterministic. The pass runs for those side effects alone:
            // it keeps neither body text nor literals (a scratch LiteralPool — everything in
            // the real pool is emitted, and these bodies are about to be dropped).
            //
            // The type-layout closure runs BEFORE planning, which is what makes a lazily
            // decoded field type sound under sharing: a decode mints the closed generics the
            // type names, and an instantiation nothing has grouped is emitted as ITSELF
            // rather than through its canonical owner. Planning's every round calls
            // SyncSharedGenerics and so groups what this minted; the set itself is discarded
            // (this call is for the decode) and the real one is computed below, once
            // SharedOwner exists to redirect it. Misordered, byte-identity breaks quietly —
            // EventHandler<T> stops canonicalizing — so the phase transitions make the
            // ordering contract loud.
            _c.EnterPhase(EmitPhase.LayoutClosure);
            ComputeEmitted();
            Timing.Mark("layout-closure");
            _c.EnterPhase(EmitPhase.Planning);
            // Runtime-instantiation templates root here — after the scan and the
            // layout closure (their trigger set is final), before the planning
            // compile (their bodies must be trial-compiled like any canonical
            // owner's).
            _c.BuildRuntimeInstantiationTemplates();
            CompileReachableBodies(new LiteralPool(), null, new List<MethodInfo>(),
                diagnostics: null);
            Timing.Mark("plan-bodies");
            _c.FinalizeSharedGenerics((cls, m) => _backend.ShouldSkipMethodBody(cls, m));
            // Planning-pass slot registries include later-dropped bodies' slots;
            // rebuild them from scratch so the emitted tables hold exactly the
            // retained shared bodies' slots in deterministic compile order.
            _c.ResetRgctxForEmission();
            _c.ResetLazyPInvokeCallsForEmission();
            _c.ResetSharedPlanningState();
            Timing.Mark("finalize-shared");
        }
        // Decode every reachable method's HotPath bits before the audit arms below, so the
        // arming decision (BeginCallSymbolAudit reads NoAllocMethods.Count) sees the full
        // root set. The planning pass does NOT guarantee it: a body synthesized without
        // Compile() — a P/Invoke forwarder, an intrinsic ftn wrapper, an HTTP shim — never
        // touches IsHotPath, so a [HotPath(NoAlloc)] root among those would leave recording
        // unarmed while EmitBody's own read still registers the root, tripping
        // AssertNoAllocClosures' unarmed-recording invariant on what is user input.
        // IsHotPath reads only custom attributes, so this grows nothing and changes no
        // output.
        foreach (var m in _c.Reachable)
            _ = m.IsHotPath;
        // The real pass, and only it, fills the emitter's literal pool and the output —
        // and so it is the only one whose named symbols the linker will ever be handed,
        // which is why the audit is armed here and nowhere else.
        _c.BeginCallSymbolAudit();
        CompileReachableBodies(_literals, EmitBody, compiledMethods, diagnostics: null);
        if (_c.SharedGenericsEnabled)
            AssertSharedBodySymbols(canonicalBodies);
        // The defined set, materialized ONCE and read by two consumers: the named-symbol
        // backstop below, and RequireDefinedBodySymbol — the question a backend's epilogue
        // has to ask before it spells a method symbol into a table. Nothing appends to
        // compiledMethods after this point (EmitUnmanagedExports and EmitCctorEnsures only
        // read it), so the snapshot stays the whole truth for the rest of the emission.
        _definedBodySymbols = new HashSet<string>(compiledMethods.Count, StringComparer.Ordinal);
        foreach (var m in compiledMethods)
            _definedBodySymbols.Add(m.CppName);
        AssertCalledBodiesEmitted();
        AssertNoAllocClosures(compiledMethods);
        Timing.Mark("compile-bodies");

        _emit = ComputeEmitted();
        _emittedCache = null;
        // A delegate invoker names its Invoke signature types by value or pointer; ensure
        // any class type there is at least opaquely emitted even when it is otherwise only
        // referenced through the delegate. LOOK THROUGH A BYREF: a `ref T` / `out T`
        // parameter renders as T's own struct pointer, so the ELEMENT is the type the
        // invoker's declaration names, and skipping it emits a declaration against an
        // undeclared struct (the BCL's Regex `MatchCallback<TState>(ref TState, Match)`
        // over a ValueTuple, visible only in a shared-generics build).
        foreach (var d in _emit.Where(c => c.IsDelegate).ToList())
            if (d.Methods.FirstOrDefault(m => m.Name == "Invoke") is { } inv)
                foreach (var t0 in inv.Signature.ParameterTypes.Append(inv.Signature.ReturnType))
                {
                    var t = t0;
                    while (t is { Kind: TypeKind.ByRef, Element: { } brEl })
                        t = brEl;
                    if (t is { Kind: TypeKind.Class, Class: { IntrinsicCppName: null } tc })
                        _c.NoteReferencedType(tc);
                }
        // Reference-only types not otherwise emitted become opaque empty layouts
        // (forward-declared struct + minimal type-info), so dead-branch casts
        // compile without dragging in their real field/interface closure.
        foreach (var c in _c.ReferencedTypes)
            if (c is { IntrinsicCppName: null, IsEnum: false } && !_emit.Contains(c))
            {
                // A referenced-only DELEGATE stays non-opaque: reflection over a
                // typeof-only delegate type must still see its Invoke signature
                // (Expression.Lambda<TDelegate> validates typeof(TDelegate) via
                // GetMethod("Invoke") + GetParameters). Invoke has no IL body
                // (Rva == 0), so the method-table row survives the reachability
                // trim and its dginvoke thunk emits with the delegate class —
                // the full metadata costs only the table rows.
                if (!c.IsDelegate)
                    _opaque.Add(c);
                EmitAdd(c);
                // A grouped specialization's struct name redirects to its
                // canonical owner's; when the only group member is this
                // referenced-only type, the owner is not in the emit closure
                // yet, so pull it in (opaquely — nothing reads its fields)
                // or the type-info's sizeof would name an undefined struct.
                if (ClassInfo.ShareStructLayout && c.SharedOwner is { } owner
                    && EmitAdd(owner))
                    _opaque.Add(owner);
                // Keep the base chain: the opaque type-info still backs isinst /
                // IsAssignableFrom over a typeof-only type (Expression.Lambda
                // validates typeof(TDelegate) against MulticastDelegate), so its
                // bases come in opaquely too instead of truncating at nullptr.
                for (var bc = c.BaseClass; bc is { IntrinsicCppName: null, IsEnum: false }; bc = bc.BaseClass)
                    if (EmitAdd(bc))
                        _opaque.Add(bc);
            }
        // Canonical shared generics: interface tables grow an alias row per
        // implemented closed generic interface whose canonical form differs
        // (see EmitTypeInfos), so a shared body's canonical-interface probe
        // (dn2cpp_resolve_interface with e.g. ti_IEqualityComparer_$CnInt32)
        // resolves on real receivers. Make sure each such canonical interface
        // is at least opaquely emitted so its type-info symbol exists.
        if (_c.SharedGenericsEnabled)
        {
            // The alias row rides on the REAL interface's slot table, which only
            // materializes when that interface is itself in the emit set. A
            // receiver whose interfaces are dispatched exclusively through shared
            // canonical bodies (e.g. DoublyLinkedList<SymbolicRegexNode<BDD>>
            // enumerated only inside the shared SymbolicRegexBuilder<CnRef>.
            // CreateConcatAlreadyReversed) has no compiled site referencing the
            // real IEnumerable<...>, so pull the real interface in wherever a
            // retained shared body actually probes its canonical form — else the
            // canonical dn2cpp_resolve_interface probe finds no row and traps.
            var probedCanonItfs = new HashSet<ClassInfo>();
            foreach (var kv in _c.SharedItfDispatches)
                if (_c.Reachable.Contains(kv.Key))
                    probedCanonItfs.UnionWith(kv.Value);
            foreach (var cls in _emit.ToList())
            {
                if (cls.IsInterface || cls.IsEnum || cls.IsAbstract || IsOpaque(cls))
                    continue;
                for (var c = cls; c is not null; c = c.BaseClass)
                    foreach (var itf in c.Interfaces)
                        if (_c.CanonicalInterfaceOf(itf) is { } citf
                            && probedCanonItfs.Contains(citf) && EmitAdd(itf))
                            _opaque.Add(itf);
            }
            foreach (var cls in _emit.ToList())
                for (var c = cls; c is not null; c = c.BaseClass)
                    foreach (var itf in c.Interfaces)
                        if (_c.CanonicalInterfaceOf(itf) is { } citf && !_emit.Contains(citf))
                        {
                            _opaque.Add(citf);
                            EmitAdd(citf);
                        }
            foreach (var aei in _c.ArrayEnumerableElementTypes.Values.ToList())
                foreach (var disp in aei.Dispatches)
                    if (_c.CanonicalInterfaceOf(disp.Itf) is { } citf && !_emit.Contains(citf))
                    {
                        _opaque.Add(citf);
                        EmitAdd(citf);
                    }
            _c.CompletePendingSpecializations();
        }
        if (ClassInfo.ShareStructLayout)
            CloseTopoOnlyLayouts();

        // A `static Main(string[] args)` entry point's epilogue builds the args
        // array tagged with the precise ti_arr_string handle. That per-element array
        // type-info is only emitted (by EmitArrayTypeInfos, below) when its element is
        // noted — and a plain `args[i]`/`args.Length` body never notes it — so note it
        // here, before EmitTypeInfos runs.
        if (EntryPoint is { Signature.ParameterTypes: [{ Kind: TypeKind.SZArray, Element: { } el }] } && el.IsString)
            _c.NoteArrayElementType(el);

        // Emission is split across a shared header (type layouts + external-linkage
        // declarations) and one-or-more TUs, so a large program compiles in parallel: only
        // symbols referenced ACROSS TU boundaries are promoted to external linkage and
        // declared in the header (ti_/ty_/gendef_/rgctx_ handles, method prototypes,
        // statics, string literals); everything referenced only within one class's metadata
        // block stays file-local and co-located with it.
        string asmName = _c.Reader.GetString(_c.Reader.GetAssemblyDefinition().Name);
        o.Header.AppendLine("// Auto-generated by Dn2Cpp.Transpiler — do not edit.");
        o.Header.AppendLine($"// Source assembly: {asmName}");
        o.Header.AppendLine("#pragma once");
        o.Header.AppendLine($"#include \"{_backend.RuntimeHeader}\"");
        // The portable-SIMD vector surface is included explicitly rather than ridden
        // in through the runtime header: generated code is the only consumer of
        // dn2cpp_vec_*, and keeping dn2cpp.h vector-free spares every runtime TU
        // (they reach it through the lane headers) the ~100k preprocessed lines of
        // hwy/highway.h under the default DN2CPP_USE_HIGHWAY=ON.
        o.Header.AppendLine("#include \"dn2cpp_vectors.h\"");
        if (_c.UsesPlatformIsa)
        {
            o.Header.AppendLine("#include \"dn2cpp_cpu_features.h\"");
            foreach (string header in _c.PlatformIsaHeaders)
                o.Header.AppendLine($"#include \"{header}\"");
        }
        o.Header.AppendLine();

        o.Data.AppendLine("// Auto-generated by Dn2Cpp.Transpiler — do not edit.");
        o.Data.AppendLine($"// Source assembly: {asmName}");
        o.Data.AppendLine("#include \"generated.h\"");
        o.Data.AppendLine();

        // Managed type layouts go in the header (every TU needs the complete struct defs).
        // After EmitStructs: a P/Invoke entry point may pass/return a blittable value
        // struct by value, so its extern "C" declaration references the managed struct's
        // complete C++ layout (t_<Name>), which EmitStructs defines.
        EmitStructs(o.Header);
        Timing.Mark("emit-structs");
        // After EmitStructs (which defines every managed t_<Name> layout) and before the
        // P/Invoke extern declarations (a by-value non-blittable struct param's extern decl
        // names tn_<Name>): emit the native marshalling structs (-> header) + their
        // field-by-field marshal-in/out helpers (-> data).
        EmitPInvokeMarshalStructs(o);
        EmitPInvokeDeclarations(o.Header);
        EmitLazyPInvokeCaches(o.Header);
        EmitStaticFields(o);

        // Method prototypes go in the header (external linkage): a body in any TU may call
        // a method defined in another TU.
        o.Header.AppendLine("// ---- method forward declarations ----");
        foreach (var m in compiledMethods)
            o.Header.AppendLine($"{MethodCompiler.Signature(m)};");
        o.Header.AppendLine();

        // The reflection keep-set (--trim-reflection), materialized HERE and not earlier:
        // ComputeEmitted's delegate-invoker pass above still appends to ReferencedTypes,
        // which is one of the seeds. A no-op unless the flag was passed.
        _c.ComputeReflectionKeepSet();
        EmitTypeInfos(o);
        // Every ti_ an emitted body named is now either defined by the type-info emission
        // just run or a genuine hole; assert the former before the C++ link finds the latter.
        AssertNamedTypeInfosDefined();
        // The sibling check over C++ struct types: every t_ an emitted body named as a type
        // (a field-access cast, a dispatch fn-ptr signature, a sizeof) must be one the layout
        // emission forward-declares (EmittedClasses) — else an undeclared t_ at the C++ compile.
        AssertNamedStructsDefined();
        Timing.Mark("emit-typeinfos");
        EmitTypeRegistry(o.Data);
        EmitTypeBinds(o.Data);
        // The Console.Error managed-writer singleton accessor: after the type
        // infos (ti_ handles) and method forward decls (the ctor prototype) it references.
        EmitConsoleErrorWriterAccessor(o);
        // Hot-update base build: hash the canonical ABI contract over the emitted
        // classes (now final — bodies compiled, opaque fill done) into the base
        // binary, pre-emit the N2M vtable trampolines the patch loader installs
        // for interpreted virtual overrides, and hand the sidecar manifest
        // (hash + vtable slot layouts) back to the driver.
        string? baseAbiJson = null;
        if (_hotUpdateBase)
        {
            ulong abiHash = AbiContract.HashUtf8(AbiContract.Serialize(EmittedClasses,
                _c.SharedGenericsEnabled ? AbiContract.CanonPolicyVersion : 0));
            o.Data.AppendLine("// ---- hot-update base-image ABI hash (docs/BPI-FORMAT.md) ----");
            o.Data.AppendLine($"extern \"C\" const uint64_t dn2cpp_base_image_abi_hash = 0x{AbiContract.Hex16(abiHash)}ULL;");
            o.Data.AppendLine();
            // Canonical-world classes (placeholder type args) never carry
            // per-instantiation metadata — no type-info, no registry row — so
            // they can neither appear in the manifest nor back an N2M
            // trampoline/bridge table entry; the per-real group members do.
            var vtableClasses = EmittedClasses.Where(c =>
                !c.IsValueType && !c.IsEnum && !c.IsInterface && !c.IsDelegate && !IsOpaque(c)
                && !IsCanonicalWorld(c) && !IsRuntimeTemplateLevel(c)).ToList();
            var itfClasses = EmittedClasses.Where(c => c.IsInterface && !IsOpaque(c)
                && !IsCanonicalWorld(c)).ToList();
            var delegateClasses = EmittedClasses.Where(c => c.IsDelegate && !IsOpaque(c)
                && !IsCanonicalWorld(c)).ToList();
            // Struct names actually declared in this image: a slot signature can
            // reference a class tree-shaking dropped (e.g. a skipped-body engine
            // shim virtual's InputEvent argument in a GDExtension build), and a
            // bridge over such a slot would not compile.
            var declaredStructs = EmittedClasses.Select(c => c.CppStructName)
                .ToHashSet(StringComparer.Ordinal);
            EmitN2MTrampolines(o.Data, vtableClasses, declaredStructs);
            EmitN2MItfTrampolines(o.Data, itfClasses, declaredStructs);
            EmitDelegateInterpBridges(o.Data, delegateClasses, declaredStructs);
            // Closed generic reference-type instantiations (collections, generic
            // delegates, generic interfaces): the manifest maps each one's
            // base⇄converter key to its mangled registry name so the patch
            // converter can bind a base-image generic type it never loaded.
            var genericInstantiations = EmittedClasses
                .Where(c => c.GenericArity > 0 && c.Context.TypeArgs.Length > 0
                    && !c.IsValueType && !c.IsEnum && !IsOpaque(c) && !IsCanonicalWorld(c)
                    && !IsRuntimeTemplateLevel(c))
                .Select(c => (Key: AbiContract.InstantiationKey(_c.GenericDefFullName(c), c.Context.TypeArgs),
                    Mangled: c.FullName));
            // The conditional default-reference verdicts ride along as a build-input
            // record: the base injects and the converter does not, so the converter needs
            // to know which shims the base carried to tell that expected asymmetry from a
            // wrong -r set. Outside the hash — see AbiContract.ManifestJson.
            baseAbiJson = AbiContract.ManifestJson(abiHash, vtableClasses, itfClasses,
                genericInstantiations, _c.DefaultRefRecord);
        }
        EmitAssemblyRegistry(o.Data);
        EmitStringLiterals(o);
        // Enum Object.ToString bodies, after the literal table they reference.
        if (_enumToStringFns.Length > 0)
        {
            o.Data.AppendLine("// ---- enum Object.ToString (member-name formatting) ----");
            o.Data.Append(_enumToStringFns);
            o.Data.AppendLine();
        }
        EmitBlobs(o);

        EmitDelegateAdapters(o);
        EmitDelegateInvokers(o);
        EmitDelegateReflBinds(o);
        // After the delegate invokers (which the pool thunks call) and before the
        // method bodies (whose Marshal fn-ptr intrinsics AND delegate-typed P/Invoke
        // call sites reference the pool helpers). Every delegate → native function
        // pointer conversion — explicit or a P/Invoke parameter marshal — goes through
        // this one persistent, GC-rooted pool.
        EmitMarshalFnPtrThunks(o);

        // Generic-virtual-method type-switch dispatchers (header prototype + body).
        // Emitted before the method bodies that call them; they reference only
        // header-visible symbols (the override impls' forward decls and the ti_ externs).
        EmitGvmDispatchers(o);

        // The method bodies are already out: EmitBody routed each one as it compiled —
        // inline-promoted ones (AggressiveInlining or tiny IL) into the header buffer that
        // closes generated.h below, the rest into body chunks that streamed to disk as they
        // sealed. A body may land in any TU, so it only ever names header-visible symbols;
        // that is what makes routing it before the header exists sound.
        //
        // Single-TU mode is the exception and cannot be: with splitting off there are no
        // chunks, so the bodies share generated.cpp with the data definitions above and
        // have to follow them. Replay them here, after the data definitions.
        if (unsplitBodies is not null)
        {
            o.Data.AppendLine("// ---- method bodies ----");
            for (int i = 0; i < unsplitBodies.Count; i++)
            {
                o.Data.AppendLine(unsplitBodies[i]);
                unsplitBodies[i] = null!;   // released as it is copied
            }
        }
        Timing.Mark("emit-bodies");

        // C-symbol exports for [UnmanagedCallersOnly(EntryPoint = ...)] methods, after
        // the bodies (they only reference the header-declared method prototypes).
        EmitUnmanagedExports(o.Data, compiledMethods);

        // Static constructors run eagerly at startup (EmitInitCalls), in reach/compile
        // order; .NET runs each lazily on first use, which guarantees a type's statics are
        // set before anything reads them. The startup subset and its remaining orderings
        // have to be restored by hand.
        //
        // System.HashCode.s_seed is a .cctor fill consumed — transitively, via
        // HashCode.Combine — by the GetHashCode of every ValueTuple / record / struct key,
        // so a static-field Dictionary/HashSet keyed by such a type and populated inside its
        // own cctor buckets its entries with s_seed==0 while every later lookup recomputes
        // with the real seed and misses. HashCode..cctor is a dependency-free leaf, so
        // hoisting it ahead of every other cctor is always correct. OrderBy is a stable
        // sort, so all other cctors keep their relative order.
        //
        // The same hazard spans assemblies: a static initializer reads a static a
        // *referenced* assembly's cctor sets, transitively through ordinary calls. A static
        // dependency always points from a referencing assembly into its references, never
        // back, so ordering by assembly-reference depth (CoreLib 0, the entry assembly
        // deepest) matches .NET's first-use ordering across assemblies.
        //
        // Within one module the `<Module>` .cctor — which .NET runs before the first access
        // to ANY type in that module — is hoisted ahead of that module's type cctors: the
        // same "before its dependents" rule one level down. (The use-site guards make it
        // safe either way; the hoist is what makes the order match .NET.)
        var modDepth = ModuleInitDepths();
        var allCctors = compiledMethods
            .Where(m => m.Name == ".cctor" && m.IsStatic)
            .OrderBy(m => m.DeclaringClass.FullName == "System.HashCode" ? -1
                        : modDepth.GetValueOrDefault(m.DeclaringClass.Module, 0))
            .ThenBy(m => m.DeclaringClass.Name == "<Module>" ? 0 : 1)
            .ToList();
        // A closed generic's statics belong to that exact instantiation. Running every
        // reached instantiation here turns first-use caches into pre-Main side effects;
        // resolver caches in particular can freeze a registry before Main registers it.
        // Their static-field mouths already call the idempotent __ensure wrapper, so leave
        // closed generic cctors lazy while retaining the legacy startup pass for other types.
        var startupCctors = allCctors
            .Where(m => m.DeclaringClass.GenericArity == 0)
            .ToList();
        // Idempotent first-use wrappers for every static constructor: the eager init
        // loop (EmitInitCalls) and the use-site guards (EnsureCctorBefore) both run a
        // cctor only through its __ensure wrapper, so it runs at most once whichever
        // fires first. The use-site guard reproduces .NET's "statics set before first
        // read" ordering for the dependencies the eager reach order violates.
        EmitCctorEnsures(o, allCctors, compiledMethods);
        // Inline-promoted bodies (AggressiveInlining + tiny-IL) close the header —
        // after the cctor __ensure guards just above, the last header-declared
        // symbols a body can name.
        if (inlineBodies.Length > 0)
        {
            o.Header.AppendLine("// ---- inline-promoted method bodies ----");
            o.Header.Append(inlineBodies);
        }
        // The entry point / Godot tables are definitions, so they land in the primary
        // Data TU (generated.cpp); they reference only header-visible symbols.
        _backend.EmitEpilogue(this, o.Data, startupCctors);

        // Every mouth that can name a precise ti_arr_ handle has now run — the reflection
        // tables, the attribute rows, the assembly registry and the backend's epilogue — so
        // the degrade tally is complete and can be answered for. Kept here rather than beside
        // AssertNamedTypeInfosDefined for exactly that reason.
        AssertArrayTypeInfoDegradesWithinCap();

        // Every chunk but the last of each stream is already on disk; flush those two.
        // Header and Data stay as builders — the driver ToString()s and releases them
        // one at a time rather than holding both final strings at once.
        o.SealChunks();
        Timing.Mark("emit-rest");
        if (EmitCensus.Enabled)
            EmitCensusReport(o);
        if (_c.UsesPlatformIsa)
        {
            // The detector is app-specific: a runtime archive is shared across outputs,
            // while only this output knows whether an ISA token/helper survived reachability.
            // Emit it after every managed-input validation, and keep it out of both the
            // runtime archive and generated.h's PCH.
            writeChunk("generated_platform_isa.cpp",
                "// Auto-generated by Dn2Cpp.Transpiler — do not edit.\n"
                + "#include \"intrinsics/dn2cpp_cpu_features.cpp\"\n");
        }
        ReportParallelBodyStats();
        return new EmittedSources(o.Header, o.Data, o.BodyChunks, o.MetadataChunks, o.HotChunks,
            o.HotFastChunks, baseAbiJson);
    }

    /// <summary>Hands what emission still holds to <see cref="EmitCensus"/> — the two
    /// builders the driver has yet to write plus the pools that fed them. Gated at the
    /// call site for the reason <c>TypeMetadataEmitter.Census</c> is: the walks are real
    /// work, and this one runs at the peak.</summary>
    private void EmitCensusReport(CppOutput o)
    {
        long litChars = 0;
        foreach (string s in _literals.Literals)
            litChars += s.Length;
        long blobBytes = 0;
        foreach (byte[] b in _literals.Blobs)
            blobBytes += b.Length;
        long defBodyChars = 0;
        int defBodyCount = 0;
        if (_definedBodySymbols is { } defBody)
            foreach (string s in defBody)
            {
                defBodyChars += s.Length;
                defBodyCount++;
            }
        long defTiChars = 0;
        foreach (string s in _definedTypeInfoSyms)
            defTiChars += s.Length;
        EmitCensus.ReportEmission(
            o.Header.Length, o.Data.Length, _literals.Literals.Count, litChars,
            _literals.Blobs.Count, blobBytes, _enumToStringFns.Length,
            _c.NamedBodySymbols is { } nb ? nb.Count : 0, defBodyCount, defBodyChars,
            _definedTypeInfoSyms.Count, defTiChars, o.BodyChunks, o.MetadataChunks);
    }

    /// <summary>Debug backstop for shared generics (on in DEBUG builds or with
    /// DN2CPP_SHARED_ASSERT=1): no emitted shared body's text may name the
    /// vtable/rgctx/static-field symbols of a class that belongs to a
    /// sharing group (a linked alias, or a real specialization whose arguments
    /// canonicalize — even when the link was shape-dropped). Shared bodies may
    /// name owner-group (placeholder-bearing — including the canonical
    /// interface handles, by design) or placeholder-free type-info handles;
    /// naming a grouped instantiation's per-context storage (rgctx_/sf_) or,
    /// dormantly, its vtable means a placeholder leaked a real instantiation's
    /// identity into shared text. The type-info (ti_) arm is deliberately NOT
    /// enforced; the reasoning is at the loop below.</summary>
    /// <summary>Whether the shared-body backstop below runs. Consulted by the body sink,
    /// which tees the canonical bodies' text only when it does — the check is the only
    /// thing that still needs a body's text after emission has streamed it out, and it
    /// only ever looks at canonical bodies.</summary>
    private static bool SharedAssertEnabled
    {
        get
        {
#if DEBUG
            return true;
#else
            return EnvKnobs.BoolIsOne(EnvKnobs.SharedAssert);
#endif
        }
    }

    /// <summary>The forbidden-symbol set is derived from <c>_c.Classes</c>, which KEEPS
    /// GROWING while bodies compile (each round completes new specializations) — so this
    /// cannot be a per-body check made as each body is produced. It has to run after the
    /// fixpoint, against the set as it finally stands, which is why the sink retains the
    /// canonical bodies' text at all.</summary>
    private void AssertSharedBodySymbols(List<(MethodInfo M, string Text)> canonicalBodies)
    {
        if (!SharedAssertEnabled)
            return;
        var forbiddenSyms = new HashSet<string>(System.StringComparer.Ordinal);
        var forbiddenStaticPrefixes = new HashSet<string>(System.StringComparer.Ordinal);
        foreach (var cls in _c.Classes)
        {
            bool grouped = cls.SharedOwner is not null
                || (cls.GenericArity > 0 && !Compilation.ContainsCanonPlaceholder(cls)
                    && _c.HasCanonicalizableArgs(cls));
            if (!grouped)
                continue;
            // A grouped class's type-info symbol (ti_...) is deliberately NOT forbidden.
            // The teed bodies are exclusively FULLY-canonical owner bodies, compiled once
            // under a context where their own group type-arguments are __Canon, so a ti_
            // reaching one is either the body's own canonical identity (never in this set:
            // a canonical owner is not `grouped`) or placeholder-free — i.e. sourced from a
            // concrete IL token, resolving to the SAME type-info for every real member
            // running the one shared body, with the alias rows guaranteeing that row exists
            // on every real receiver. The other three arms stay protective: rgctx_ (a
            // canonical body takes its context as the hidden __rgctx param and must never
            // bake in a concrete instantiation's), sf_ (a concrete static-storage symbol
            // would pin one instantiation's storage into group-shared text), and vt_
            // (dormant — emitted only in metadata — kept because it is harmless).
            forbiddenSyms.Add(cls.CppVtableName);
            forbiddenSyms.Add("rgctx_" + cls.CppName);
            forbiddenStaticPrefixes.Add("sf_" + cls.CppName + "_");
        }
        // Real generic-method instantiations' per-method tables: only their
        // forwarders and fill-produced table entries may name them — a shared
        // body baking one in would pin a single instantiation's context.
        foreach (var cls in _c.Classes)
        {
            if (!cls.MembersReady)
                continue; // a specialization nothing reached has no methods to forbid —
                          // and asking it for them would decode every one
            foreach (var m in cls.Methods)
                if (m.NameSuffix != "" && m.SharedOwner is not null
                    && !Compilation.IsCanonicalMethod(m))
                    forbiddenSyms.Add("rgctx_" + m.CppName);
        }
        var sfPrefixLookup = forbiddenStaticPrefixes.GetAlternateLookup<ReadOnlySpan<char>>();
        foreach (var (m, body) in canonicalBodies)
        {
            foreach (System.Text.RegularExpressions.Match match in
                     System.Text.RegularExpressions.Regex.Matches(body, @"\b(?:ti|vt|rgctx)_[A-Za-z0-9_]+"))
                if (forbiddenSyms.Contains(match.Value))
                    throw new InvalidOperationException(
                        $"shared-generics backstop: shared body {m.CppName} names "
                        + $"the instantiation-specific symbol {match.Value}");
            // One linear scan per body instead of one Contains pass per forbidden prefix:
            // every position a prefix could match starts with "sf_", and a prefix is all
            // identifier characters ending in '_', so probing every '_'-terminated cut of
            // the identifier run at each "sf_" occurrence visits exactly the positions the
            // per-prefix scans matched at.
            for (int at = body.IndexOf("sf_", StringComparison.Ordinal); at >= 0;
                 at = body.IndexOf("sf_", at + 1, StringComparison.Ordinal))
            {
                int end = at + 3;
                while (end < body.Length && (char.IsAsciiLetterOrDigit(body[end]) || body[end] == '_'))
                    end++;
                for (int cut = at + 4; cut <= end; cut++)
                    if (body[cut - 1] == '_' && sfPrefixLookup.Contains(body.AsSpan(at, cut - at)))
                        throw new InvalidOperationException(
                            $"shared-generics backstop: shared body {m.CppName} names "
                            + $"a static field of the instantiation-specific group '{body[at..cut]}*'");
            }
        }
    }

    /// <summary>The C++ method symbols this emission DEFINES: <c>{m.CppName : m ∈
    /// compiledMethods}</c>, materialized once in <see cref="Emit"/> right after the
    /// emission body pass. The header's forward-declaration block is spun out of that same
    /// list and nothing else, so this set IS what the linker will be handed — which is why
    /// both readers ask it rather than approximating with <c>Reachable</c> (a superset: a
    /// reachable method bound to a shared canonical body, or one the backend skips, defines
    /// no symbol of its own). Null until that pass has run.</summary>
    private HashSet<string>? _definedBodySymbols;

    /// <summary>The symbol a backend's epilogue may spell for <paramref name="m"/>, or a
    /// loud failure. A ClassDB/registration table is emitted text that is NOT a method body,
    /// so <see cref="AssertCalledBodiesEmitted"/> structurally cannot see it — it diffs what
    /// bodies name — and the epilogue's own tables would otherwise reach the C++ compiler
    /// naming an undeclared identifier. It asks a stronger question than the
    /// <c>_c.Reachable.Contains(...)</c> the other emitter-side address sites ask: those
    /// legitimately fall back to a null slot when nothing reaches the method, whereas an
    /// engine entry point has no C# caller by construction — degrading it to
    /// <c>nullptr</c> would ship a game whose <c>_Ready</c> never runs.
    ///
    /// <para><paramref name="site"/> names the table row for the message; it is the only
    /// thing the caller knows that this cannot re-derive.</para></summary>
    internal string RequireDefinedBodySymbol(MethodInfo m, string site)
    {
        if (_definedBodySymbols is not { } defined)
            throw new InvalidOperationException(
                $"{site}: the defined-body-symbol set is not materialized — an epilogue "
                + "asked for a method symbol before the emission body pass ran.");
        if (defined.Contains(m.CppName))
            return m.CppName;
        string what = $"{m.DeclaringClass.FullName}::{m.Name}  ->  {m.CppName}";
        // Bad input, not a transpiler bug: somebody cut a method the backend has to name.
        // NotSupportedException, so the driver renders it as one `error:` line (exit 2)
        // instead of a raw stack — the exception contract in AGENTS.md.
        if (_c.IsBoundedMethod(m.DeclaringClass.FullName, m.Name))
            throw new NotSupportedException(
                $"{site} must name {what}, but that method is CUT — this emission defines "
                + "no body for it, so the generated C++ would name an undeclared symbol. "
                + "The slot is an ENGINE entry point: nothing in the C# call graph reaches "
                + "it, so a cut here cannot be neutralized at a call site the way an "
                + "ordinary call is. Drop the --cut spec for this method, or stop "
                + "overriding/exporting it in C#.");
        throw new InvalidOperationException(
            $"{site} must name {what}, but this emission defines no body for it. The "
            + "backend rooted the method and emission dropped it — an emitted table would "
            + "name a symbol the header never declares.");
    }

    /// <summary>The named-symbol backstop: every method symbol an emitted body spells out
    /// must be a symbol some emitted body DEFINES (<see cref="_definedBodySymbols"/> — the
    /// forward-declaration block's own source, exactly, with no other). A body naming a
    /// symbol outside it is the asymmetry this exists to catch: reachability cut an edge
    /// that emission still walks, and the transpile SUCCEEDS, so the failure surfaces one
    /// stage later as a C++ link error against a mangled name with no caller, callee or
    /// reason attached. Here all three are still in hand.
    ///
    /// Compared by SYMBOL, never by MethodInfo identity: a minted structural-equality body
    /// whose CppName collides with a twin's is deliberately left out of the compiled list —
    /// one definition serves both — yet the symbol IS defined, by the twin. The linker only
    /// ever sees names, so this asks the question the linker asks.
    ///
    /// Unconditional, with no build-configuration or environment escape hatch (a gated
    /// assert is one nobody runs), and decode-free: CppName reads neither Signature nor
    /// Methods, so recording and checking grows no closed generic.</summary>
    private void AssertCalledBodiesEmitted()
    {
        // Off during planning, whose bodies never reach the output. (--measure never runs
        // Emit at all; MeasureGaps runs this same diff itself, with the dropped bodies'
        // symbols unioned into `defined`, and reports survivors as `dangling` gap rows.)
        if (_c.NamedBodySymbols is not { Count: > 0 } named)
            return;
        var defined = _definedBodySymbols!;
        var missing = named.Where(kv => !defined.Contains(kv.Key.CppName))
            .OrderBy(kv => kv.Key.CppName, StringComparer.Ordinal)
            .ToList();
        if (missing.Count == 0)
            return;

        var (callee, caller) = (missing[0].Key, missing[0].Value);
        var sb = new StringBuilder();
        sb.AppendLine($"named-symbol backstop: {missing.Count} method symbol(s) named by an emitted "
                      + "body, defined by none — the C++ link would fail on the mangled name.");
        sb.AppendLine("Either reachability must keep the callee's body, or the call site must lower "
                      + "to something that does not name it.");
        sb.AppendLine($"  callee: {callee.DeclaringClass.FullName}::{callee.Name}  ->  {callee.CppName}");
        sb.AppendLine($"  caller: {caller.DeclaringClass.FullName}::{caller.Name}  ->  {caller.CppName}");
        sb.AppendLine($"  reach:  {_c.ReachChain(caller)}");
        const int more = 8;
        foreach (var (m, by) in missing.Skip(1).Take(more))
            sb.AppendLine($"  also:   {m.DeclaringClass.FullName}::{m.Name}  ({m.CppName})"
                          + $"  named by {by.DeclaringClass.FullName}::{by.Name}");
        if (missing.Count > more + 1)
            sb.AppendLine($"  ... and {missing.Count - more - 1} more");
        throw new InvalidOperationException(sb.ToString());
    }

    /// <summary>The type-info half of the named-symbol backstop, the sibling of
    /// <see cref="AssertCalledBodiesEmitted"/>: every <c>ti_&lt;T&gt;</c> / <c>ti_arr_&lt;T&gt;</c>
    /// symbol an emitted body SPELLS OUT (recorded through the lowering wrappers into
    /// <see cref="Compilation.NamedTypeInfoSymbols"/>) must be one this emission DEFINES
    /// (<see cref="_definedTypeInfoSyms"/>, populated as every class/enum/array/referenced-
    /// intrinsic type-info is forward-declared). A body naming one nothing defines is the
    /// asymmetry this catches: a <c>typeof</c>/<c>isinst</c>/<c>castclass</c>/<c>box</c>/array-
    /// covariance lowering emits <c>&amp;ti_&lt;T&gt;</c> for a type whose runtime type-info no
    /// pass emits (the intrinsic-modeled classes the opaque referenced-type pass skips) — the
    /// transpile SUCCEEDS and the C++ link then fails on the undeclared mangled name with no
    /// cause attached. Here the symbol and a naming body are still in hand.
    ///
    /// Compared by SYMBOL STRING, decode-free (recording read a lowering's own output, never a
    /// Signature or Members), and off during planning / <c>--measure</c> — the recording
    /// dictionary is null there, so this returns at once, because neither can say anything true
    /// about what the OUTPUT names. Runs after <see cref="EmitTypeInfos"/>, so the defined set
    /// is complete.</summary>
    private void AssertNamedTypeInfosDefined()
    {
        if (_c.NamedTypeInfoSymbols is not { Count: > 0 } named)
            return;
        var missing = named.Where(kv => !_definedTypeInfoSyms.Contains(kv.Key))
            .OrderBy(kv => kv.Key, StringComparer.Ordinal)
            .ToList();
        if (missing.Count == 0)
            return;

        var (sym, by) = (missing[0].Key, missing[0].Value);
        var sb = new StringBuilder();
        sb.AppendLine($"named-type-info backstop: {missing.Count} type-info symbol(s) named by an "
                      + "emitted body, defined by none — the C++ link would fail on the mangled name.");
        sb.AppendLine("A typeof/isinst/castclass/box/array-covariance lowering names a type's ti_ "
                      + "while nothing emits it (an intrinsic-modeled type the opaque referenced-type "
                      + "pass skips). Emit its type-info, or lower the site to a symbol that IS emitted.");
        sb.AppendLine($"  symbol: {sym}");
        sb.AppendLine($"  named by: {by.DeclaringClass.FullName}::{by.Name}  ->  {by.CppName}");
        sb.AppendLine($"  reach:  {_c.ReachChain(by)}");
        const int more = 8;
        foreach (var (s, b) in missing.Skip(1).Take(more))
            sb.AppendLine($"  also:   {s}  named by {b.DeclaringClass.FullName}::{b.Name}");
        if (missing.Count > more + 1)
            sb.AppendLine($"  ... and {missing.Count - more - 1} more");
        throw new InvalidOperationException(sb.ToString());
    }

    /// <summary>THE definedness question — "does this emission DEFINE the type-info symbol
    /// <paramref name="sym"/>?" — and the only answer any emitter-side <c>&amp;ti_</c> mouth
    /// is allowed to consult (they all render through <see cref="TypeInfoRef"/>).
    ///
    /// <para><b>It is not the same question as <c>_emit.Contains</c>.</b> A canonical group
    /// owner (<see cref="SkipsCanonicalMetadata"/>) is in the emit set — its struct layout and
    /// its retained shared bodies emit — while its per-instantiation type-info never does, so
    /// membership says "yes" where the linker says no; enums are a second gap in the same
    /// direction (their type-infos come from <c>ReferencedTypes</c> alone), and
    /// <see cref="TopoOrder"/> pulling in a tree-shaken base is the reverse. A set of CLASSES
    /// cannot answer a question about SYMBOLS; only the set of symbols the declaration block
    /// actually wrote can (<see cref="_definedTypeInfoSyms"/>).</para>
    ///
    /// <para>Not every handle is a <c>ti_</c>: a runtime-raised exception type's
    /// <see cref="ClassInfo.CppTypeInfoName"/> is the mutable handle <c>dn2cpp.h</c> declares,
    /// which this emission never defines and never has to — the same filter
    /// <c>TypeMetadataEmitter.NoteDefinedTypeInfo</c> applies to the defined half.</para>
    ///
    /// <para>The <c>ti_arr_&lt;elem&gt;</c> handles are in scope too, through
    /// <see cref="ArrayTypeInfoDeclared"/>: the recorded set is exactly the DECLARED one (the
    /// declaration loop notes the entries present when the header externs are written), so a
    /// late-noted element is correctly answered "no" — it gets a definition but no extern, and
    /// a metadata chunk TU naming it would fail the C++ compile.</para></summary>
    private bool TypeInfoSymbolDefined(string sym) =>
        !sym.StartsWith("ti_", StringComparison.Ordinal)
        || _definedTypeInfoSyms.Contains(sym);

    /// <summary>The array arm of <see cref="TypeInfoSymbolDefined"/>: does this emission
    /// declare the precise <c>ti_arr_&lt;element&gt;</c> handle? The two mouths that can name
    /// one — a reflected member type (<see cref="FieldTypeInfoExpr"/>) and a custom-attribute
    /// array argument's allocation tag — ask HERE rather than each holding its own snapshot.
    ///
    /// <para>Unlike <see cref="TypeInfoRef"/> this answers rather than throws AT THE MOUTH,
    /// because both mouths have a syntactically sound degrade (the <c>System.Object</c>
    /// handle / the untagged <c>object[]</c> allocation) and the emission that follows is
    /// well-formed C++ either way. A "no" is nevertheless RECORDED
    /// (<see cref="_arrayTiDegrades"/>) and answered for at the end of the emission by
    /// <see cref="AssertArrayTypeInfoDegradesWithinCap"/> — see that method for why a degrade
    /// nobody counts is the one outcome of this funnel a reader of the output cannot
    /// detect.</para></summary>
    private bool ArrayTypeInfoDeclared(TypeDesc element, string site)
    {
        // The early-call guard. Its throw is unreachable from any input, and the argument
        // for that — plus what it does catch — is at _typeInfoSymsFinal.
        if (!_typeInfoSymsFinal)
            throw new InvalidOperationException(
                $"named-type-info backstop ({site}): a precise ti_arr_ handle was asked about "
                + "BEFORE the type-info declaration block ran, so nothing can answer whether it "
                + "is declared — and the answer would silently degrade the site to an untyped "
                + "handle. Move the mouth back to at or after TypeMetadataEmitter.Emit's "
                + "declaration loops.");
        string sym = "ti_arr_" + Compilation.ArrayElemMangle(element);
        if (TypeInfoSymbolDefined(sym))
            return true;
        string key = site + " -> " + sym;
        _arrayTiDegrades[key] = _arrayTiDegrades.TryGetValue(key, out int n) ? n + 1 : 1;
        return false;
    }

    /// <summary>Every "no" <see cref="ArrayTypeInfoDeclared"/> has answered, keyed
    /// <c>"&lt;site&gt; -&gt; &lt;ti_arr_ symbol&gt;"</c> and counted — the site because the two
    /// mouths degrade differently, the symbol because it is what a fix has to make declared.
    /// Read by <see cref="AssertArrayTypeInfoDegradesWithinCap"/>.</summary>
    private readonly Dictionary<string, int> _arrayTiDegrades = new(System.StringComparer.Ordinal);

    /// <summary>The ceiling on <see cref="ArrayTypeInfoDeclared"/> degrades, default
    /// <b>zero</b>: past it the transpile fails naming every one of them. It is the answer to
    /// the one degradation in the type-info funnel that a reader of the emitted C++ cannot
    /// see — the emission is well-formed either way, and what is lost is a fact about a type.
    ///
    /// <para>Zero holds because for both mouths the declared set is filled by a PAIRING that
    /// mirrors the mouth: <c>TypeMetadataEmitter.NoteReflectedMemberArrayElements</c>
    /// pre-notes exactly the member types the reflection tables go on to render, and
    /// <c>Compilation.NoteAttrArgArrayType</c> notes an attribute argument's element in the
    /// same loop that reaches the attribute's ctor. A "no" means a pairing has come
    /// apart.</para>
    ///
    /// <para>Neither degrade is benign, which is why the default fails rather than warns. A
    /// degraded member type reports <c>object</c> where .NET reports <c>Elem[]</c> — a JSON
    /// contract resolver classifying a member by its reflected type then deserializes an
    /// untyped JArray and the setter thunk's blind cast stores it. A degraded
    /// attribute-argument allocation carries the shared <c>object[]</c> handle, whose missing
    /// interface-dispatch map aborts the first collection-interface dispatch inside the
    /// attribute ctor. Both are latent wrong answers in the shipped binary, reported by
    /// nothing. <see cref="EnvKnobs.MaxArrayTypeInfoDegrades"/> raises the ceiling — a cap, so
    /// it can only turn a run into an abort or back, never change the C++ a successful
    /// transpile emits — and the run then prints the tally as a warning.</para></summary>
    private void AssertArrayTypeInfoDegradesWithinCap()
    {
        if (_arrayTiDegrades.Count == 0)
            return;
        int total = 0;
        foreach (var kv in _arrayTiDegrades)
            total += kv.Value;
        var sb = new StringBuilder();
        sb.AppendLine($"array type-info backstop: {total} reference(s) to a precise ti_arr_ handle "
                      + $"this emission never declared, across {_arrayTiDegrades.Count} (site, element) "
                      + "pair(s) — each one silently degraded to System.Object / an untagged object[]:");
        // A List + Sort rather than an OrderBy pipeline: this file is transpiler input
        // (self-host), and the plain shapes are the ones that pipeline cleanly there.
        var keys = new List<string>(_arrayTiDegrades.Keys);
        keys.Sort(StringComparer.Ordinal);
        foreach (string key in keys)
            sb.AppendLine($"  {_arrayTiDegrades[key]}x  {key}");
        int cap = EnvKnobs.Int(EnvKnobs.MaxArrayTypeInfoDegrades) is { } v && v > 0 ? v : 0;
        if (total <= cap)
        {
            Console.WriteLine("dn2cpp: " + sb.ToString().TrimEnd());
            return;
        }
        sb.AppendLine("Note the element where the mouth's pairing does: a reflected member type "
                      + "through TypeMetadataEmitter.NoteReflectedMemberArrayElements, an attribute "
                      + "argument through Compilation.NoteAttrArgArrayType. Both run before the "
                      + "declaration loops, which is what makes the handle exist.");
        sb.Append($"Raise {EnvKnobs.MaxArrayTypeInfoDegrades}=<n> to accept them (cap is {cap}) — the "
                  + "degrade is then reported instead of thrown.");
        throw new InvalidOperationException(sb.ToString());
    }

    /// <summary>The single mouth through which every emitter-side <c>&amp;ti_&lt;T&gt;</c>
    /// reference is spelled: renders the reference AND asserts, first, that this emission
    /// defines the symbol (<see cref="TypeInfoSymbolDefined"/>).
    ///
    /// <para>This is the emitter-side sibling of <see cref="AssertNamedTypeInfosDefined"/>,
    /// which cannot see any of these references: that one diffs what the compiled BODIES named,
    /// recorded through the lowering wrappers, and the emitter's own tables, dispatchers,
    /// thunks and singleton accessors are text no body ever produced. A reference nothing
    /// defines makes the transpile report success and the C++ build fail on an undeclared
    /// identifier — in a toolchain that knows nothing about IL, reachability or the emit set.
    /// Here the symbol, the type, the reason and the site are all still in hand.</para>
    ///
    /// <para>The check is a NET, not a proof: it catches a mouth on a corpus that walks it.
    /// The point of one funnel is that the mouth set is greppable and a new one cannot be
    /// written without asking the question.
    /// <paramref name="site"/> is a string literal at every caller, and
    /// <paramref name="detail"/> — the site-specific half of a diagnosis, e.g. a dispatcher's
    /// reach chain — is a thunk evaluated only on failure, so the funnel allocates nothing on
    /// the per-field and per-member paths it sits in.</para></summary>
    internal string TypeInfoRef(ClassInfo c, string site, Func<string>? detail = null)
    {
        string sym = c.CppTypeInfoName;
        if (!_typeInfoSymsFinal)
            throw new InvalidOperationException(
                $"named-type-info backstop ({site}): a &{sym} reference was rendered BEFORE the "
                + "type-info declaration block ran, so nothing can answer whether it is defined. "
                + "An emitter-side &ti_ mouth must emit at or after EmitTypeInfos' declaration "
                + "loops; move it back, or record the symbol and diff it the way "
                + nameof(AssertNamedTypeInfosDefined) + " does for compiled bodies.");
        if (!TypeInfoSymbolDefined(sym))
        {
            var sb = new StringBuilder();
            sb.AppendLine($"named-type-info backstop ({site}): an emitted reference names a "
                          + "type-info symbol this emission defines nowhere — the C++ build would "
                          + "fail on the undeclared identifier with no cause attached.");
            sb.AppendLine($"  symbol: {sym}");
            sb.AppendLine($"  type:   {c.FullName}");
            sb.AppendLine($"  why:    {TypeInfoUndefinedReason(c)}");
            if (detail is not null)
                sb.AppendLine($"  site:   {detail()}");
            sb.AppendLine("Emit the type's type-info, or drop the reference — a handle nothing "
                          + "defines is a handle no object can ever carry.");
            throw new InvalidOperationException(sb.ToString());
        }
        return "&" + sym;
    }

    /// <summary>The string-form arm of the same question, for the one emitter-side family whose
    /// <c>&amp;ti_</c> references are not rendered from a <see cref="ClassInfo"/> at the point of
    /// emission: the shared-generics rgctx tables. Their slot expressions are built by
    /// <c>Compilation.RgctxSlotExpr</c> during BODY compilation, and that fill site cannot host
    /// the check for two independent reasons — the defined set does not exist yet, and the
    /// fill's callers catch <see cref="InvalidOperationException"/> next to
    /// <see cref="NotSupportedException"/> and DEGRADE the slot to <c>nullptr</c>, so a throw
    /// there would be swallowed and quietly change which bodies get shared. The rows are
    /// RENDERED after the declaration block, which is where the question can be both answered
    /// and escaped from, so that is where it is asked.
    ///
    /// <para>Only a bare <c>&amp;ti_…</c> expression is a type-info reference; a slot may just
    /// as well hold a static-field address, a cctor-ensure thunk or <c>nullptr</c>, and those
    /// name symbols this check knows nothing about.</para></summary>
    private void RequireRenderedTypeInfoDefined(string expr, string site, ClassInfo owner)
    {
        if (!expr.StartsWith("&ti_", StringComparison.Ordinal) || TypeInfoSymbolDefined(expr[1..]))
            return;
        var sb = new StringBuilder();
        sb.AppendLine($"named-type-info backstop ({site}): a rendered row names a type-info "
                      + "symbol this emission defines nowhere — the C++ build would fail on the "
                      + "undeclared identifier with no cause attached.");
        sb.AppendLine($"  symbol: {expr[1..]}");
        sb.AppendLine($"  table:  rgctx_{owner.CppName}  ({owner.FullName})");
        sb.AppendLine("The slot's fill (Compilation.RgctxSlotExpr) force-emits the type it names; "
                      + "if that stopped reaching the type-info emission, the fill is what to fix.");
        throw new InvalidOperationException(sb.ToString());
    }

    /// <summary>Why <see cref="TypeInfoRef"/> found no definition — the half of the diagnosis
    /// the symbol name cannot carry. Ordered most-specific first: the canonical-owner case is
    /// the one seen in the wild, and it is precisely the case an <c>_emit.Contains</c> guard
    /// reports as fine.</summary>
    private string TypeInfoUndefinedReason(ClassInfo c) =>
        SkipsCanonicalMetadata(c)
            ? "a canonical group owner — in the emit set (struct layout + retained shared "
              + "bodies), but its per-instantiation metadata, type-info included, never emits"
            : c.IsEnum && !_c.ReferencedTypes.Contains(c)
                ? "an enum, and enum type-infos are emitted from ReferencedTypes alone — "
                  + "nothing referenced this one as a type token"
                : !_emit.Contains(c)
                    ? "not in the emit set, and not pulled into TopoOrder as an emitted class's "
                      + "base or canonical owner either"
                    : "in the emit set, yet the declaration loop wrote no ti_ for it";

    /// <summary>The C++ struct-type half of the named-symbol backstop, the sibling of
    /// <see cref="AssertNamedTypeInfosDefined"/>: every <c>t_&lt;T&gt;</c> struct-type symbol an
    /// emitted body SPELLS OUT (recorded into <see cref="Compilation.NamedStructSymbols"/> at the
    /// three funnels that name a bare struct type — a field-access receiver cast, a dispatch
    /// function-pointer signature, a <c>sizeof</c> over a value element) must be one the layout
    /// emission FORWARD-DECLARES — i.e. one of <see cref="EmittedClasses"/>'s non-enum struct
    /// names, the exact set <see cref="EmitStructs"/> writes <c>struct t_…;</c> for. A body
    /// naming one nothing declares is the asymmetry this catches: the naming type is reached
    /// through no emit edge (an intrinsic-modeled type a transpiled BCL body field-reads, an
    /// interface or value type named only in a dispatched interface method's signature, an
    /// <c>Array.Empty&lt;T&gt;</c> value element) — the transpile SUCCEEDS and the C++ compile
    /// then fails on the undeclared <c>t_</c> with no cause attached.
    ///
    /// Compared by SYMBOL STRING, decode-free (recording read a lowering's own
    /// <see cref="ClassInfo.CppStructName"/> output, never a Signature or Members), and off
    /// during planning / <c>--measure</c> (the recording dictionary is null there). Runs after
    /// the emit set is final (<see cref="_emit"/> plus the opaque / referenced-intrinsic passes),
    /// so the defined set is complete.</summary>
    private void AssertNamedStructsDefined()
    {
        if (_c.NamedStructSymbols is not { Count: > 0 } named)
            return;
        var defined = EmittedClasses.Where(c => !c.IsEnum)
            .Select(c => c.CppStructName)
            .ToHashSet(StringComparer.Ordinal);
        var missing = named.Where(kv => !defined.Contains(kv.Key))
            .OrderBy(kv => kv.Key, StringComparer.Ordinal)
            .ToList();
        if (missing.Count == 0)
            return;

        var (sym, by) = (missing[0].Key, missing[0].Value);
        var sb = new StringBuilder();
        sb.AppendLine($"named-struct backstop: {missing.Count} C++ struct type(s) named by an "
                      + "emitted body, forward-declared by none — the C++ compile would fail on the type name.");
        sb.AppendLine("A field-access cast, a dispatch fn-ptr signature, or a sizeof names a type's "
                      + "t_ struct while nothing emits it (a type reached through no emit edge — an "
                      + "intrinsic-modeled type a BCL body field-reads, an interface/value type named "
                      + "only in a dispatched signature, an Array.Empty<T> value element). Emit its "
                      + "layout (NoteReferencedType / NoteForceEmit), or cut the body that names it.");
        sb.AppendLine($"  symbol: {sym}");
        sb.AppendLine($"  named by: {by.DeclaringClass.FullName}::{by.Name}  ->  {by.CppName}");
        sb.AppendLine($"  reach:  {_c.ReachChain(by)}");
        const int more = 8;
        foreach (var (s, b) in missing.Skip(1).Take(more))
            sb.AppendLine($"  also:   {s}  named by {b.DeclaringClass.FullName}::{b.Name}");
        if (missing.Count > more + 1)
            sb.AppendLine($"  ... and {missing.Count - more - 1} more");
        throw new InvalidOperationException(sb.ToString());
    }

    /// <summary>The [HotPath(NoAlloc)] verifier: each NoAlloc method's direct-call closure
    /// must allocate nothing and dispatch nothing dynamically. Runs right after
    /// <see cref="AssertCalledBodiesEmitted"/>, so every symbol a body names is known to be
    /// defined — every edge target therefore has recorded body facts. It consumes only what
    /// emission recorded (HotCallEdges + HotBodyFacts); it re-resolves nothing and reads no
    /// signature. A hit fails the transpile with a NotSupportedException the driver renders
    /// as a single <c>error:</c> line (exit 2), naming the method, the offending construct
    /// and the reach chain that carried it in.</summary>
    private void AssertNoAllocClosures(List<MethodInfo> compiledMethods)
    {
        if (_c.NoAllocMethods.Count == 0)
            return;
        // Recording arms in BeginCallSymbolAudit iff a NoAlloc method exists. If one reached
        // compilation with recording off, the arming ordering broke — a transpiler bug, so
        // crash raw (InvalidOperationException) per the exception contract, never a silent
        // pass. Off during planning / --measure is not this case: this runs only after the
        // real emission pass, whose armed dictionaries are non-null here.
        if (_c.HotBodyFacts is not { } facts || _c.HotCallEdges is not { } edges)
        {
            var compiledSet = new HashSet<string>(compiledMethods.Select(m => m.CppName), StringComparer.Ordinal);
            if (_c.NoAllocMethods.Any(r => compiledSet.Contains(r.CppName)))
                throw new InvalidOperationException(
                    "[HotPath(NoAlloc)] recording was never armed although a NoAlloc method was "
                    + "compiled — Compilation.NoAllocMethods must be complete before "
                    + "BeginCallSymbolAudit arms the closure recorders.");
            return;
        }

        var violations = new List<(MethodInfo Root, MethodInfo Offender, string Kind, string Token, List<MethodInfo> Chain)>();
        var seen = new HashSet<(string, string)>();
        foreach (var root in _c.NoAllocMethods)
        {
            if (!facts.ContainsKey(root.CppName))
                continue; // never emitted — unreachable code verifies nothing
            // BFS over body symbols; the predecessor map reconstructs the chain root→offender.
            var visited = new HashSet<string>(StringComparer.Ordinal) { root.CppName };
            var pred = new Dictionary<string, string>(StringComparer.Ordinal);
            var queue = new Queue<string>();
            queue.Enqueue(root.CppName);
            while (queue.Count > 0)
            {
                string sym = queue.Dequeue();
                if (!facts.TryGetValue(sym, out var f))
                    // Every named symbol is defined (AssertCalledBodiesEmitted just passed) and
                    // every defined body routed through EmitBody → RecordHotBodyFacts, so a
                    // missing entry means the recording missed a routed body: a bug, not a skip.
                    throw new InvalidOperationException(
                        $"[HotPath(NoAlloc)] closure of {root.DeclaringClass.FullName}::{root.Name} "
                        + $"reached body symbol '{sym}' with no recorded facts, though the named-symbol "
                        + "backstop passed — RecordHotBodyFacts missed a routed body.");
                if ((f.Allocates || f.Dispatches) && seen.Add((root.CppName, sym)))
                {
                    string kind = f.Allocates ? "allocates" : "dispatches dynamically";
                    string token = f.Allocates ? f.AllocToken! : f.DispatchToken!;
                    violations.Add((root, f.Method, kind, token, ChainTo(pred, facts, root, sym)));
                }
                if (edges.TryGetValue(sym, out var callees))
                    foreach (var callee in callees)
                        if (visited.Add(callee.CppName))
                        {
                            pred[callee.CppName] = sym;
                            queue.Enqueue(callee.CppName);
                        }
            }
        }
        if (violations.Count == 0)
            return;

        var ordered = violations
            .OrderBy(v => v.Root.CppName, StringComparer.Ordinal)
            .ThenBy(v => v.Offender.CppName, StringComparer.Ordinal)
            .ToList();
        var sb = new StringBuilder();
        const int more = 8;
        foreach (var v in ordered.Take(more))
        {
            sb.AppendLine($"[HotPath(NoAlloc)] closure of {v.Root.DeclaringClass.FullName}::{v.Root.Name} "
                          + $"{v.Kind}: {v.Offender.DeclaringClass.FullName}::{v.Offender.Name} emits {v.Token}");
            sb.AppendLine($"  call chain: {string.Join(" -> ", v.Chain.Select(m => m.DeclaringClass.FullName + "." + m.Name))}");
        }
        if (ordered.Count > more)
            sb.AppendLine($"  ... and {ordered.Count - more} more");
        sb.AppendLine("Remove the offending construct from the closure, or drop NoAlloc from the [HotPath] attribute.");
        throw new NotSupportedException(sb.ToString());
    }

    /// <summary>Walk the BFS predecessor map from an offending symbol back to the root and
    /// return the chain root→offender as representative methods.</summary>
    private static List<MethodInfo> ChainTo(Dictionary<string, string> pred,
        Dictionary<string, HotBodyFacts> facts, MethodInfo root, string offenderSym)
    {
        var syms = new List<string>();
        for (string cur = offenderSym; ; )
        {
            syms.Add(cur);
            if (!pred.TryGetValue(cur, out var p))
                break; // reached the root (it has no predecessor)
            cur = p;
        }
        syms.Reverse();
        var chain = new List<MethodInfo>(syms.Count);
        foreach (var s in syms)
            chain.Add(s == root.CppName ? root : facts[s].Method);
        return chain;
    }

    /// <summary>The one-line body a grouped method bound to a hidden-parameter
    /// shared implementation emits under its own symbol: forward every argument
    /// plus this instantiation's rgctx table. Keeps a per-instantiation function
    /// with the original signature at every indirection boundary.</summary>
    private static string RgctxForwarderBody(MethodInfo m, MethodInfo impl)
    {
        // The shared implementation spells erased parameter/return types
        // (CnRef -> Dn2CppObject*), the forwarder keeps this instantiation's real
        // signature — every position whose spelling differs converts through the SAME
        // funnel the direct-call binder uses (MethodCompiler.ErasedBoundaryCast → Cast), so
        // a headerless position (the NFI trio, Assembly/Module) wraps into the erased slot
        // and unwraps out of it instead of riding a plain C cast, and the verdict is never
        // maintained in a second place. Every other differing spelling keeps its C cast.
        var args = new List<string>();
        int i = 0;
        if (!m.IsStatic)
        {
            // Both receivers name the group's shared struct layout
            // (CppStructName redirects to the owner), so no cast is needed.
            args.Add($"a{i++}");
        }
        for (int p = 0; p < m.Signature.ParameterTypes.Length; p++, i++)
        {
            var realT = m.Signature.ParameterTypes[p];
            string real = CppTypes.Of(realT);
            string erased = CppTypes.Of(impl.Signature.ParameterTypes[p]);
            args.Add(real == erased ? $"a{i}"
                : MethodCompiler.ErasedBoundaryCast($"a{i}", real, realT, erased));
        }
        // A generic-method instantiation appends its own per-method table (its
        // method arguments are not derivable from the class); a class-grouped
        // method appends its class's table.
        args.Add("rgctx_" + (m.NameSuffix != "" ? m.CppName : m.DeclaringClass.CppName));
        string call = $"{impl.CppName}({string.Join(", ", args)})";
        if (!m.Signature.ReturnType.IsVoid)
        {
            string realRet = CppTypes.Of(m.Signature.ReturnType);
            string erasedRet = CppTypes.Of(impl.Signature.ReturnType);
            if (realRet != erasedRet)
                // Same funnel, reverse direction: an erased Dn2CppObject* return
                // carrying a headerless position unwraps tolerantly to the
                // instantiation's real spelling.
                call = MethodCompiler.ErasedBoundaryCast(call, erasedRet, m.Signature.ReturnType, realRet);
        }
        var sb = new StringBuilder();
        sb.AppendLine($"// {m.DeclaringClass.FullName}::{m.Name} -> shared body + this instantiation's rgctx table");
        sb.AppendLine(MethodCompiler.Signature(m));
        sb.AppendLine("{");
        sb.AppendLine(m.Signature.ReturnType.IsVoid ? $"    {call};" : $"    return {call};");
        sb.AppendLine("}");
        return sb.ToString();
    }

    /// <summary>Per-module assembly-reference depth, used to order static constructors so a
    /// module's cctors run after the cctors of every assembly it references (the direction a
    /// static dependency points). A module with no loaded references (CoreLib) is depth 0; a
    /// referencing module is one deeper than its deepest loaded reference; the entry assembly
    /// is deepest. Reference cycles (rare across assemblies) are broken by treating a back-edge
    /// as depth 0.</summary>
    private Dictionary<Module, int> ModuleInitDepths()
    {
        var byName = new Dictionary<string, Module>();
        foreach (var m in _c.Modules)
            byName[m.AssemblyName] = m;
        var depth = new Dictionary<Module, int>();
        int Depth(Module m, HashSet<Module> path)
        {
            if (depth.TryGetValue(m, out var cached))
                return cached;
            if (!path.Add(m))
                return 0; // break a reference cycle
            int max = -1;
            foreach (var arh in m.Reader.AssemblyReferences)
            {
                string name = m.Reader.GetString(m.Reader.GetAssemblyReference(arh).Name);
                if (byName.TryGetValue(name, out var dep) && dep != m)
                    max = System.Math.Max(max, Depth(dep, path));
            }
            path.Remove(m);
            return depth[m] = max + 1;
        }
        foreach (var m in _c.Modules)
            Depth(m, new HashSet<Module>());
        return depth;
    }

    /// <summary>Whether a decoded IL instruction is translated without consulting
    /// or mutating <see cref="Compilation"/>. This is deliberately a whitelist:
    /// a new opcode stays on the established sequential path until it is proved
    /// pure here.</summary>
    private static bool IsParallelPureOpcode(ILOpCode op) => op switch
    {
        ILOpCode.Nop
            or ILOpCode.Ldc_i4_m1 or ILOpCode.Ldc_i4_0 or ILOpCode.Ldc_i4_1
            or ILOpCode.Ldc_i4_2 or ILOpCode.Ldc_i4_3 or ILOpCode.Ldc_i4_4
            or ILOpCode.Ldc_i4_5 or ILOpCode.Ldc_i4_6 or ILOpCode.Ldc_i4_7
            or ILOpCode.Ldc_i4_8 or ILOpCode.Ldc_i4_s or ILOpCode.Ldc_i4
            or ILOpCode.Ldc_i8 or ILOpCode.Ldc_r4 or ILOpCode.Ldc_r8
            or ILOpCode.Ldnull
            or ILOpCode.Ldarg_0 or ILOpCode.Ldarg_1 or ILOpCode.Ldarg_2
            or ILOpCode.Ldarg_3 or ILOpCode.Ldarg_s or ILOpCode.Ldarg
            or ILOpCode.Starg_s or ILOpCode.Starg
            or ILOpCode.Ldloc_0 or ILOpCode.Ldloc_1 or ILOpCode.Ldloc_2
            or ILOpCode.Ldloc_3 or ILOpCode.Ldloc_s or ILOpCode.Ldloc
            or ILOpCode.Stloc_0 or ILOpCode.Stloc_1 or ILOpCode.Stloc_2
            or ILOpCode.Stloc_3 or ILOpCode.Stloc_s or ILOpCode.Stloc
            or ILOpCode.Dup or ILOpCode.Pop
            or ILOpCode.Add or ILOpCode.Sub or ILOpCode.Mul
            or ILOpCode.Div or ILOpCode.Div_un or ILOpCode.Rem or ILOpCode.Rem_un
            or ILOpCode.And or ILOpCode.Or or ILOpCode.Xor
            or ILOpCode.Shl or ILOpCode.Shr or ILOpCode.Shr_un
            or ILOpCode.Neg or ILOpCode.Not
            or ILOpCode.Ceq or ILOpCode.Cgt or ILOpCode.Cgt_un
            or ILOpCode.Clt or ILOpCode.Clt_un
            or ILOpCode.Conv_i1 or ILOpCode.Conv_u1
            or ILOpCode.Conv_i2 or ILOpCode.Conv_u2
            or ILOpCode.Conv_i4 or ILOpCode.Conv_u4
            or ILOpCode.Conv_i8 or ILOpCode.Conv_u8
            or ILOpCode.Conv_r4 or ILOpCode.Conv_r8
            or ILOpCode.Conv_i or ILOpCode.Conv_u or ILOpCode.Conv_r_un
            or ILOpCode.Add_ovf or ILOpCode.Add_ovf_un
            or ILOpCode.Sub_ovf or ILOpCode.Sub_ovf_un
            or ILOpCode.Mul_ovf or ILOpCode.Mul_ovf_un
            or ILOpCode.Conv_ovf_i1 or ILOpCode.Conv_ovf_i1_un
            or ILOpCode.Conv_ovf_u1 or ILOpCode.Conv_ovf_u1_un
            or ILOpCode.Conv_ovf_i2 or ILOpCode.Conv_ovf_i2_un
            or ILOpCode.Conv_ovf_u2 or ILOpCode.Conv_ovf_u2_un
            or ILOpCode.Conv_ovf_i4 or ILOpCode.Conv_ovf_i4_un
            or ILOpCode.Conv_ovf_u4 or ILOpCode.Conv_ovf_u4_un
            or ILOpCode.Conv_ovf_i8 or ILOpCode.Conv_ovf_i8_un
            or ILOpCode.Conv_ovf_u8 or ILOpCode.Conv_ovf_u8_un
            or ILOpCode.Conv_ovf_i or ILOpCode.Conv_ovf_i_un
            or ILOpCode.Conv_ovf_u or ILOpCode.Conv_ovf_u_un
            or ILOpCode.Ckfinite
            or ILOpCode.Br or ILOpCode.Br_s
            or ILOpCode.Brtrue or ILOpCode.Brtrue_s
            or ILOpCode.Brfalse or ILOpCode.Brfalse_s
            or ILOpCode.Beq or ILOpCode.Beq_s
            or ILOpCode.Bne_un or ILOpCode.Bne_un_s
            or ILOpCode.Bge or ILOpCode.Bge_s
            or ILOpCode.Bgt or ILOpCode.Bgt_s
            or ILOpCode.Ble or ILOpCode.Ble_s
            or ILOpCode.Blt or ILOpCode.Blt_s
            or ILOpCode.Bge_un or ILOpCode.Bge_un_s
            or ILOpCode.Bgt_un or ILOpCode.Bgt_un_s
            or ILOpCode.Ble_un or ILOpCode.Ble_un_s
            or ILOpCode.Blt_un or ILOpCode.Blt_un_s
            or ILOpCode.Switch or ILOpCode.Ret => true,
        _ => false,
    };

    private static bool IsParallelScalarPrimitive(TypeDesc type) =>
        type is
        {
            Kind: TypeKind.Primitive,
            IsCanonPlaceholder: false,
            Primitive: PrimitiveTypeCode.Boolean or PrimitiveTypeCode.Char
                or PrimitiveTypeCode.SByte or PrimitiveTypeCode.Byte
                or PrimitiveTypeCode.Int16 or PrimitiveTypeCode.UInt16
                or PrimitiveTypeCode.Int32 or PrimitiveTypeCode.UInt32
                or PrimitiveTypeCode.Int64 or PrimitiveTypeCode.UInt64
                or PrimitiveTypeCode.IntPtr or PrimitiveTypeCode.UIntPtr
                or PrimitiveTypeCode.Single or PrimitiveTypeCode.Double,
        };

    /// <summary>Reads a LOCAL_SIG without invoking the signature provider. A
    /// worker may decode direct scalar primitives safely: BuildArgsAndLocals'
    /// reference/layout notes and CanonAny taint are all no-ops for these kinds.
    /// Every prefix, token and composite type code stays sequential.</summary>
    private static bool HasOnlyParallelScalarPrimitiveLocals(
        Module module, StandaloneSignatureHandle handle)
    {
        if (handle.IsNil)
            return true;
        var signature = module.Reader.GetStandaloneSignature(handle);
        var blob = module.Reader.GetBlobReader(signature.Signature);
        if (blob.RemainingBytes == 0 || blob.ReadByte() != 0x07) // LOCAL_SIG
            return false;
        int count = blob.ReadCompressedInteger();
        if (count < 0)
            return false;
        for (int i = 0; i < count; i++)
        {
            if (blob.RemainingBytes == 0)
                return false;
            // ECMA-335 ELEMENT_TYPE_* direct primitive codes. Void and every
            // prefix/composite/token-bearing code are intentionally absent.
            if (blob.ReadByte() is not (0x02 or 0x03 or 0x04 or 0x05
                    or 0x06 or 0x07 or 0x08 or 0x09 or 0x0a or 0x0b
                    or 0x0c or 0x0d or 0x18 or 0x19))
                return false;
        }
        return blob.RemainingBytes == 0;
    }

    private bool HasParallelPureShape(MethodInfo m)
    {
        _parallelBodyPureShape ??= new Dictionary<MethodInfo, bool>();
        if (_parallelBodyPureShape.TryGetValue(m, out bool cached))
            return cached;
        bool pure = false;
        try
        {
            var signature = m.Signature;
            if (!signature.ReturnType.IsVoid
                && !IsParallelScalarPrimitive(signature.ReturnType))
                return _parallelBodyPureShape[m] = false;
            foreach (var parameter in signature.ParameterTypes)
                if (!IsParallelScalarPrimitive(parameter))
                    return _parallelBodyPureShape[m] = false;
            var body = m.Module.PE.GetMethodBody(m.Rva);
            if (body.ExceptionRegions.Length != 0
                || !HasOnlyParallelScalarPrimitiveLocals(m.Module, body.LocalSignature))
                return _parallelBodyPureShape[m] = false;
            var insns = ILDecoder.Decode(body.GetILBytes()!.ToImmutableArrayCompat());
            foreach (var insn in insns)
                if (!IsParallelPureOpcode(insn.OpCode))
                    return _parallelBodyPureShape[m] = false;
            // Materialize every lazy rendering the worker reads.
            _ = MethodCompiler.Signature(m);
            pure = true;
        }
        catch (Exception ex) when (!Compilation.IsMustEscape(ex))
        {
            // The ordinary compile reproduces the input failure at its established
            // position; structural classification never owns a diagnostic.
        }
        _parallelBodyPureShape[m] = pure;
        return pure;
    }

    /// <summary>Conservative proof that compiling one static scalar IL body is a
    /// read-only operation on the completed model. Reference-bearing signatures,
    /// non-scalar locals, token-bearing instructions, exception regions, special
    /// replacement bodies and lazily unprepared caches stay sequential.</summary>
    private bool CanCompileBodyInParallel(ClassInfo cls, MethodInfo m)
    {
        // MemoryGuard's sampled tick and first over-budget exception are ordered
        // observables. Preserve its exact method boundary by staying sequential
        // when the operator enables the heap ceiling.
        if (_c.BodyCompileJobs <= 1 || MemoryGuard.LimitBytes > 0
            || m.Rva == 0 || m.IsSynthetic || m.SharedImpl is not null
            || !m.IsStatic || !m.SignatureReady
            || m.IsSynchronized || m.IsUnmanagedCallersOnly || m.IsHotPath
            || CoreIntrinsics.BrHttpShim.Matches(cls.FullName, m.Name)
            || CoreIntrinsics.BrEnumInstanceFormat.Matches(cls.FullName, m.Name)
            || _c.PInvokeFtnTargets.Contains(m) || _c.IntrinsicFtnTargets.Contains(m)
            || _c.InterceptFtnTargets.Contains(m) || CoreIntrinsics.MdComparerCompare.Matches(m)
            || _c.IsUnorderableComparerCompareBody(m)
            || CoreIntrinsics.IsIntrinsicType(cls.FullName))
            return false;
        return HasParallelPureShape(m);
    }

    private ParallelBodyResult CompileParallelBody(
        MethodInfo m, LiteralPool literals, bool planning)
    {
        var result = new ParallelBodyResult();
        try
        {
            var mc = new MethodCompiler(_c, m, literals, _backend);
            if (_c.SharedGenericsEnabled && Compilation.IsCanonicalMethod(m))
            {
                mc.SharedTrial = true;
                if (planning)
                    mc.SharedDirectCallees = new List<(MethodInfo, bool)>();
            }
            string body = mc.Compile();
            if (!planning)
                result.Body = body;
            result.SharedDirectCallees = mc.SharedDirectCallees;
        }
        catch (Exception ex)
        {
            result.Error = ex;
        }
        return result;
    }

    private void CommitParallelBody(
        MethodInfo m, ParallelBodyResult result, bool planning,
        Action<MethodInfo, string>? emitBody, List<MethodInfo> compiledMethods,
        List<MeasureGap>? diagnostics)
    {
        bool canonical = _c.SharedGenericsEnabled && Compilation.IsCanonicalMethod(m);
        if (result.Error is { } error)
        {
            if (canonical && planning)
            {
                if (error is SharedBodyTaintException taint)
                {
                    _c.SharedTaint[m] = taint.Kind;
                    return;
                }
                if (error is NotSupportedException unsupported
                    && !Compilation.IsMustEscape(unsupported))
                {
                    _c.SharedTaint[m] = "unsupported";
                    if (EnvKnobs.BoolIsOne(EnvKnobs.SharedDump))
                        Console.WriteLine(
                            $"dn2cpp: shared-generics unsupported {m.CppName}: {unsupported.Message}");
                    return;
                }
                throw error;
            }
            if (diagnostics is not null && !Compilation.IsMustEscape(error))
            {
                diagnostics.Add(MeasureGap.From("compile", m, error));
                return;
            }
            throw error;
        }

        if (canonical && planning)
        {
            _c.SharedTrialCompiled.Add(m);
            _c.SharedCallEdges[m] = result.SharedDirectCallees!;
            return;
        }
        if (planning && diagnostics is null)
            return;
        if (!planning)
            emitBody?.Invoke(m, result.Body!);
        compiledMethods.Add(m);
    }

    private void ReportParallelBodyStats()
    {
        if (_parallelBodyStatsReported)
            return;
        _parallelBodyStatsReported = true;
        Timing.ParallelBodies(_c.BodyCompileJobsRequested, _c.BodyCompileJobs,
            _parallelBodyPeak, 0, _parallelBodyEligible, _parallelBodySerial);
    }

    /// <summary>Compiles every reachable, non-skipped method body to a fixpoint.
    /// Compiling a body can instantiate a generic the discovery scan never saw (a
    /// local/cast type), appending to <see cref="Compilation.Classes"/> and queuing it
    /// for completion — so collect a not-yet-compiled snapshot, compile it (mutations
    /// land in _c.Classes, not the list we iterate), drain the new specializations,
    /// and repeat until no new body appears.
    /// <para>When <paramref name="diagnostics"/> is non-null (self-hosting
    /// feasibility harness, see <see cref="MeasureGaps"/>), a body that throws
    /// <em>any</em> exception is recorded (by exception type) and skipped instead of
    /// aborting the whole run, so a single pass enumerates every gap — including an
    /// unexpected throw (InvalidCast, ArgumentException, …) that would otherwise stop
    /// the measurement before later gaps are seen. In the normal (diagnostics == null)
    /// path any compile error propagates.</para>
    /// <para><paramref name="emitBody"/> takes each body the instant it is compiled, and a
    /// null one discards it: compiling also allocates rgctx slots, discovers instantiations,
    /// interns literals and records shareability taint, and those side effects are the whole
    /// point of the passes that keep no text (the shared-generics planning pass, both of
    /// <see cref="MeasureGaps"/>'s). So every call site below computes the body into a local
    /// FIRST and only then offers it to the sink — an <c>emitBody?.Invoke(m, Compile())</c>
    /// would skip the compile outright when the sink is null, because C# does not evaluate
    /// the argument of a null-conditional call.</para></summary>
    private void CompileReachableBodies(
        LiteralPool literals,
        Action<MethodInfo, string>? emitBody, List<MethodInfo> compiledMethods,
        List<MeasureGap>? diagnostics)
    {
        // Which pass this is is not an argument — it is the compilation's phase,
        // captured once here: the caller advances the phase at the pass
        // boundary, so a call site cannot claim to be the planning pass while the
        // verdicts are already final (or vice versa), which is exactly the lie a
        // caller-passed boolean would make possible.
        bool planning = _c.Phase == EmitPhase.Planning;
        var compiled = new HashSet<MethodInfo>();
        // Per-pass, not per-emitter: this method runs twice (planning, then emission) and
        // the emission pass has to define the same bodies the planning pass discarded.
        var mintedSymbols = new HashSet<string>(StringComparer.Ordinal);
        // The candidate queue and its cursor into the reachable set's admission log. Every
        // body this loop compiles is reachable, so the log IS the source of candidates:
        // draining it per round replaces a walk over every class's every method, whose cost
        // was the whole model rather than the round's delta. Per-pass like `compiled` — the
        // emission pass has to re-see what the planning pass already saw.
        int reachCursor = 0;
        var pending = new List<MethodInfo>();
        var pendingSet = new HashSet<MethodInfo>();
        // The filters a candidate may be dropped on, because their answer cannot turn back:
        // an eviction from Reachable is either immediately re-admitted or happens between
        // the two passes (FinalizeSharedGenerics), and either way the re-admission appends a
        // fresh log entry that brings the method back through the queue; SharedImpl is
        // assigned between the passes too; and the backend's skip is a pure function of the
        // pair. Shared with the hot-update collector below so the two cannot drift.
        bool Wanted(ClassInfo cls, MethodInfo m)
            => _c.Reachable.Contains(m)                 // tree-shaken: unreachable from the roots
               // bound to a shared canonical body — nothing of its own to emit
               && (m.SharedImpl is not { } si || (!planning && si.RgctxParam));
        while (true)
        {
            // Rgctx table fill: resolve every new (slot, group member) pair —
            // noting/reaching the referenced runtime entities — before the map
            // expansion below, so entities a table entry pulls in are wired
            // this same round. Runs in both passes: the planning fill drives
            // the discovery fixpoint the emission pass replays.
            bool rgctxFilled = _c.SharedGenericsEnabled && _c.FillRgctxTables();
            if (rgctxFilled && !planning)
                _c.DrainReachability();
            // Covariant array→IEnumerable expansion: wire a per-element map for
            // every noted array whose element is assignable to a requested IEnumerable<E>,
            // reaching the wrapper support so the next batch compiles it. Runs each round
            // because the noted-array / cast-target sets keep growing as bodies compile.
            _c.ExpandArrayEnumerableMaps();
            // Shared-generics planning: instantiations discovered by the bodies
            // just compiled are linked to canonical owners and their grouped
            // methods' owner counterparts reached, so the next batch trial-
            // compiles the new shared-body candidates too.
            if (planning)
                _c.SyncSharedGenerics();
            var batch = new List<(ClassInfo Cls, MethodInfo M)>();
            // Everything admitted to the reachable set since the last round is a candidate.
            // A minted structural body is deliberately absent from its class's Methods — the
            // walk this replaces could not see it either — and the mint list below owns it.
            var order = _c.Reachable.Order;
            while (reachCursor < order.Count)
            {
                var m = order[reachCursor++];
                if (!m.IsSynthetic && pendingSet.Add(m))
                    pending.Add(m);
            }
            int keep = 0;
            for (int i = 0; i < pending.Count; i++)
            {
                var m = pending[i];
                var cls = m.DeclaringClass;
                // Two tests can still change their answer, so failing either keeps the
                // candidate for a later round rather than dropping it. A specialization
                // whose members were never decoded cannot own a reachable method (reaching
                // one means having resolved it, and resolving it is what decodes them), and
                // a bodyless method has nothing to compile — except an address-taken
                // P/Invoke, whose forwarder body is synthesized below, and whose noting
                // ldftn may not have run yet.
                if (!cls.MembersReady || (m.Rva == 0 && !_c.PInvokeFtnTargets.Contains(m)))
                {
                    pending[keep++] = m;
                    continue;
                }
                pendingSet.Remove(m);
                if (!Wanted(cls, m) || _backend.ShouldSkipMethodBody(cls, m)
                    || compiled.Contains(m))
                    continue;
                batch.Add((cls, m));
            }
            pending.RemoveRange(keep, pending.Count - keep);
            // Hot-update base: a skipped engine-shim method still needs a real,
            // invoker-compatible function — the interpreter binds a patch's method imports
            // through reflection metadata, which a body-less method leaves null. Collected
            // by a walk over Classes rather than off the queue above because this is the one
            // arm whose emission order is discovery order: the batch is sorted before it
            // compiles, these are not. A normal build never enters the walk.
            var synthetic = new List<MethodInfo>();
            if (_hotUpdateBase)
            {
                foreach (var cls in _c.Classes)
                {
                    if (!cls.MembersReady)
                        continue;
                    foreach (var m in cls.Methods)
                        if ((m.Rva != 0 || _c.PInvokeFtnTargets.Contains(m))
                            && Wanted(cls, m) && _backend.ShouldSkipMethodBody(cls, m)
                            && _backend.WantsSyntheticBody(cls, m))
                            synthetic.Add(m);
                }
            }
            // Synthesize the collected bodies from the call intrinsic's own lowering
            // (identical marshalling to an AOT call site); a shape it cannot lower stays
            // bodyless, the pre-existing missing-AOT boundary. This runs after the walk
            // rather than inside it because compiling a body resolves its tokens, and
            // resolving a token can instantiate a generic — which appends to _c.Classes,
            // the list the walk is reading. A collector collects; it does not compile.
            // (Ordering invariant: every synthetic body precedes every batch body,
            // so the literal pool and the cctor order numbering stay deterministic.)
            foreach (var m in synthetic)
            {
                if (compiled.Add(m)
                    && new MethodCompiler(_c, m, literals, _backend)
                        .CompileIntrinsicCallWrapper() is { } wrapper)
                {
                    emitBody?.Invoke(m, wrapper);
                    compiledMethods.Add(m);
                    _syntheticBodies.Add(m);
                }
            }
            // The minted structural equality/hash bodies of value types that override
            // neither (Compilation.SynthesizedValueBodies). They cannot ride the walk
            // above: a minted body is deliberately absent from its class's Methods — that
            // absence is what keeps it out of the vtable, the reflection member table and
            // the ABI contract — so the only place it can be seen is the mint list.
            // Reachability decided the set before either pass began (a mint reads field
            // types, and that is a decode), which is why there is no registry to reset
            // between the planning and emission passes: `Reachable` IS the registry, and it
            // is the same set both times.
            var minted = _c.SynthesizedValueBodies
                .Where(m => _c.Reachable.Contains(m) && !compiled.Contains(m)
                    && !_backend.ShouldSkipMethodBody(m.DeclaringClass, m))
                .ToList();
            minted.Sort(MethodInfo.CompareByOrder);
            foreach (var m in minted)
            {
                compiled.Add(m);
                // A CppName is not unique across ClassInfos: shared-source internal generics
                // linked into several assemblies mangle alike, and a minted body carries row
                // 0, so twins would collide on the symbol where real methods are told apart
                // by their rows. One definition serves both — their layouts are identical,
                // which is the premise of the emit set's own CppName dedupe.
                if (!mintedSymbols.Add(m.CppName))
                    continue;
                emitBody?.Invoke(m, new MethodCompiler(_c, m, literals, _backend)
                    .CompileSynthesizedValueBody());
                compiledMethods.Add(m);
            }
            if (batch.Count == 0 && minted.Count == 0 && !rgctxFilled)
                break;
            // Compile in a content-derived order: three numberings are handed out by first
            // use during body compilation and baked into the emitted text (the literal
            // pool's str_N / blob_N, the rgctx registry's slot indices, and — through
            // compiledMethods — the order cctors run in within a module depth), and all
            // three would otherwise read discovery order. Round boundaries are already
            // order-independent (both sets are least fixpoints of monotone operators), so
            // sorting inside the round makes the whole sequence a function of the input.
            batch.Sort(static (x, y) => MethodInfo.CompareByOrder(x.M, y.M));
            BodyWorkerPool? workers = null;
            try
            {
                // Amortize the publish/join barrier across several tiny scalar
                // bodies per worker while keeping retained text O(jobs).
                int windowSize = batch.Count;
                if (_c.BodyCompileJobs > 1
                    && _c.BodyCompileJobs <= int.MaxValue / ParallelBodiesPerWorker)
                    windowSize = System.Math.Min(batch.Count,
                        _c.BodyCompileJobs * ParallelBodiesPerWorker);
                for (int windowStart = 0; windowStart < batch.Count;)
                {
                    int remaining = batch.Count - windowStart;
                    int windowEnd = windowStart + System.Math.Min(windowSize, remaining);
                    ParallelBodyResult?[]? parallel = null;
                    List<int>? eligible = null;
                    if (_c.BodyCompileJobs > 1)
                    {
                        eligible = new List<int>();
                        parallel = new ParallelBodyResult?[windowEnd - windowStart];
                        for (int bi = windowStart; bi < windowEnd; bi++)
                        {
                            var candidate = batch[bi];
                            if (CanCompileBodyInParallel(candidate.Cls, candidate.M))
                                eligible.Add(bi);
                        }
                        _parallelBodyEligible += eligible.Count;
                        _parallelBodySerial += windowEnd - windowStart - eligible.Count;
                        if (eligible.Count < 2)
                        {
                            // One body cannot use more than one core; keep it on the
                            // established path rather than paying a thread handoff.
                            _parallelBodySerial += eligible.Count;
                            _parallelBodyEligible -= eligible.Count;
                            eligible.Clear();
                        }
                        else
                        {
                            // MethodCompiler's constructor reads this property. Some
                            // backends initialize the intrinsics object lazily, so the
                            // coordinator must publish it before constructors race.
                            _ = _backend.CallIntrinsics;
                            int usefulWorkers = BodyCompileWorkerLimit(eligible.Count);
                            // Keep the largest pool this round has actually needed.
                            // Execute publishes the smaller work count under the same lock,
                            // so surplus workers claim no index and only join the barrier;
                            // retaining them avoids stop/start churn across uneven windows.
                            if (workers is null || workers.WorkerCount < usefulWorkers)
                            {
                                workers?.Stop();
                                workers = new BodyWorkerPool(usefulWorkers);
                            }
                            var resultSlots = parallel;
                            var workItems = eligible;
                            int activePeak = workers.Execute(workItems.Count, wi =>
                            {
                                int bi = workItems[wi];
                                resultSlots[bi - windowStart] = CompileParallelBody(
                                    batch[bi].M, literals, planning);
                            });
                            _parallelBodyPeak = System.Math.Max(
                                _parallelBodyPeak, activePeak);
                        }
                    }
                    else
                    {
                        _parallelBodySerial += windowEnd - windowStart;
                    }

                    for (int bi = windowStart; bi < windowEnd; bi++)
                    {
                        var (cls, m) = batch[bi];
                        // Covers the planning pass, the emission pass and --measure alike.
                        MemoryGuard.Check("compile-bodies");
                        compiled.Add(m);
                        if (parallel is not null && parallel[bi - windowStart] is { } parallelResult)
                        {
                            CommitParallelBody(m, parallelResult, planning,
                                emitBody, compiledMethods, diagnostics);
                            continue;
                        }
                        // A System.Net.Http method whose body dn2cpp replaces (the transport overrides,
                        // the ClientCertificateOptions accessors — the BodyReplace row CoreIntrinsics.
                        // BrHttpShim, whose other asker is Compilation.Reach): emit the synthesized body
                        // (CompileHttpShimBody) in place of the real IL that reachability cut. Like
                        // the minted value-body arm above, the planning pass leaves emitBody null,
                        // so the null-conditional skips the compile — the shim edge was already
                        // reached in Compilation.Reach, so no discovery is lost.
                        if (CoreIntrinsics.BrHttpShim.Matches(cls.FullName, m.Name))
                        {
                            emitBody?.Invoke(m, new MethodCompiler(_c, m, literals, _backend)
                                .CompileHttpShimBody());
                            compiledMethods.Add(m);
                            continue;
                        }
                        // System.Enum's instance ToString/GetTypeCode/value-trio/GetValue — the
                        // BodyReplace row CoreIntrinsics.BrEnumInstanceFormat, whose other asker is Compilation.Reach:
                        // emit the synthesized enum-format body in place of the real IL that
                        // reachability cut. Like the HTTP shim above, the planning pass leaves emitBody
                        // null so the null-conditional skips the compile.
                        if (CoreIntrinsics.BrEnumInstanceFormat.Matches(cls.FullName, m.Name))
                        {
                            emitBody?.Invoke(m, new MethodCompiler(_c, m, literals, _backend)
                                .CompileEnumInstanceFormatBody());
                            compiledMethods.Add(m);
                            continue;
                        }
                        // An address-taken bodyless P/Invoke (ldftn / delegate method group
                        // over a [DllImport] that lowers to a direct native call): synthesize
                        // a forwarder from the same P/Invoke lowering a call site gets.
                        if (_c.PInvokeFtnTargets.Contains(m))
                        {
                            var forwarder = new MethodCompiler(_c, m, literals, _backend)
                                    .CompilePInvokeWrapper()
                                ?? throw new NotSupportedException(
                                    $"taking the address of {m.DeclaringClass.FullName}::{m.Name} " +
                                    "(delegate method group / function pointer over a [DllImport]): " +
                                    "the import's shape is not marshallable, so no forwarder body " +
                                    "can be synthesized");
                            emitBody?.Invoke(m, forwarder);
                            compiledMethods.Add(m);
                            continue;
                        }
                        // An address-taken method of an intrinsic-mapped type (ldftn /
                        // delegate method group, e.g. Char.IsDigit): no real body is ever
                        // transpiled — synthesize one from the call intrinsic's lowering.
                        if (_c.IntrinsicFtnTargets.Contains(m))
                        {
                            var wrapper = new MethodCompiler(_c, m, literals, _backend)
                                    .CompileCoreIntrinsicWrapper()
                                ?? throw new NotSupportedException(
                                    $"taking the address of {m.DeclaringClass.FullName}::{m.Name} " +
                                    "(delegate method group / function pointer): the method has no " +
                                    "intrinsic lowering to synthesize a function body from");
                            emitBody?.Invoke(m, wrapper);
                            compiledMethods.Add(m);
                            continue;
                        }
                        // System.Collections.Comparer.Compare is body-intercepted — the THIRD mouth
                        // of the row whose call-site mouths are CoreIntrinsics.MdComparerCompare /
                        // MrComparerCompare: synthesize from the intrinsic lowering
                        // (-> dn2cpp_object_compare) instead of transpiling the real IL, so the
                        // IComparer.Compare slot — how ArrayList.Sort()/SortedList and the
                        // non-generic boxed sort/search reach Comparer.Default — points at the
                        // boxed-object order, which handles a boxed primitive (the real body's
                        // `x as IComparable` cannot). The synthesized body carries Comparer.Compare's
                        // own symbol, so the slot, any ldftn and a direct call all resolve to it.
                        if (CoreIntrinsics.MdComparerCompare.Matches(m))
                        {
                            var wrapper = new MethodCompiler(_c, m, literals, _backend)
                                    .CompileCoreIntrinsicWrapper()
                                ?? throw new NotSupportedException(
                                    $"{m.DeclaringClass.FullName}::{m.Name}: non-generic boxed-ordering body intercept has no intrinsic lowering to synthesize from");
                            emitBody?.Invoke(m, wrapper);
                            compiledMethods.Add(m);
                            continue;
                        }
                        // An address-taken member whose CALLS an intercept row cut (ldftn /
                        // delegate method group over e.g. ExecutionContext.Run): reachability
                        // deleted the edge to the real body, so replay that row's own emit arm
                        // at the body position — cut ⟹ route holds through the body too. Behind
                        // the Comparer.Compare arm above: that row already owns its own body, and
                        // an address taken of it must keep getting that one.
                        if (_c.InterceptFtnTargets.Contains(m))
                        {
                            var wrapper = new MethodCompiler(_c, m, literals, _backend)
                                    .CompileInterceptWrapper()
                                ?? throw new NotSupportedException(
                                    $"taking the address of {m.DeclaringClass.FullName}::{m.Name} " +
                                    "(delegate method group / function pointer): the intercept that " +
                                    "lowers its calls cannot be replayed as a function body");
                            emitBody?.Invoke(m, wrapper);
                            compiledMethods.Add(m);
                            continue;
                        }
                        // A synthesized GenericComparer<T>.Compare over a VALUE TYPE real .NET's
                        // default comparer cannot order (see
                        // Compilation.IsUnorderableComparerCompareBody): its real IL boxes the value
                        // type for the null checks (an intrinsic value type has no ti_ to box
                        // through) and dispatches a CompareTo that does not exist. Real .NET throws
                        // ArgumentException here; body-replace with a catchable
                        // PlatformNotSupportedException so the comparer object stays real and only
                        // its Compare faults. The trailing `return 0;` is unreachable (the throw is
                        // [[noreturn]]) and only keeps the int32 return type-checking.
                        if (_c.IsUnorderableComparerCompareBody(m))
                        {
                            string elem = _c.GenericDefFullName(m.DeclaringClass.Context.TypeArgs[0].Class!);
                            string body = $"// {m.DeclaringClass.FullName}::{m.Name} (element not orderable — throws on call)\n"
                                + $"{MethodCompiler.Signature(m)}\n{{\n"
                                + $"    dn2cpp_throw_platform_not_supported(\"Comparer<{elem}>.Default.Compare: "
                                + $"{elem} does not implement IComparable and is not orderable "
                                + "(real .NET throws ArgumentException here)\");\n"
                                + "    return 0;\n}\n";
                            emitBody?.Invoke(m, body);
                            compiledMethods.Add(m);
                            continue;
                        }
                        // An intrinsic-mapped type's method whose body IS transpiled (String's
                        // interface impls): prefer synthesizing from the intrinsic lowering
                        // when one exists — String.Equals(String) becomes the same
                        // dn2cpp_string_equals its call sites use, instead of dragging the
                        // real body's private helpers (EqualsHelper's SIMD loops) into the
                        // tree. No lowering (GetEnumerator/Clone/the explicit interface
                        // impls) compiles the real IL; a real body that hits an unmapped
                        // intrinsic (an IConvertible impl over an unmodeled Convert form)
                        // degrades to a trap-on-call body instead of failing the build —
                        // the slot is only reached because SOME type dispatches the decl,
                        // and a string may never flow there.
                        if (CoreIntrinsics.IsIntrinsicType(m.DeclaringClass.FullName))
                        {
                            string body;
                            if (new MethodCompiler(_c, m, literals, _backend)
                                    .CompileCoreIntrinsicWrapper() is { } iw)
                                body = iw;
                            else
                                try
                                {
                                    body = new MethodCompiler(_c, m, literals, _backend).Compile();
                                }
                                // Must-escape (Compilation.IsMustEscape): a bound overrun is a
                                // NotSupportedException, so the type alone does not filter it —
                                // and baking it into a trap body would report the run as a
                                // success while the model kept growing.
                                catch (NotSupportedException ex) when (!Compilation.IsMustEscape(ex))
                                {
                                    string msg = $"{m.DeclaringClass.FullName}.{m.Name} is not transpilable: {ex.Message}"
                                        .Replace("\\", "\\\\").Replace("\"", "\\\"");
                                    body = $"// {m.DeclaringClass.FullName}::{m.Name} (untranspilable — traps on call)\n"
                                        + $"{MethodCompiler.Signature(m)}\n{{\n    dn2cpp_fail(\"NotSupportedException ({msg})\");\n}}\n";
                                }
                            emitBody?.Invoke(m, body);
                            compiledMethods.Add(m);
                            continue;
                        }
                        // A grouped method bound to a hidden-parameter shared body keeps
                        // its own symbol as a one-line forwarder appending its class's
                        // rgctx table — the per-instantiation function identity every
                        // boundary (vtable, interface table, ldftn, reflection) uses.
                        if (m.SharedImpl is { } fwdImpl)
                        {
                            string fwd = RgctxForwarderBody(m, fwdImpl);
                            emitBody?.Invoke(m, fwd);
                            compiledMethods.Add(m);
                            _c.RgctxForwarderCount++;
                            continue;
                        }
                        // A canonical-owner-world method is only ever a shared-body
                        // candidate: compile it with the taint checks armed. In the
                        // planning pass a taint (or an unsupported placeholder shape)
                        // just marks it unshareable — each grouped user then compiles
                        // its own body; in the final pass only shareable candidates
                        // survive in Reachable, so a throw here propagates as a real
                        // error.
                        if (_c.SharedGenericsEnabled && Compilation.IsCanonicalMethod(m))
                        {
                            var mc = new MethodCompiler(_c, m, literals, _backend)
                            {
                                SharedTrial = true,
                                SharedDirectCallees = planning ? new List<(MethodInfo, bool)>() : null,
                            };
                            if (planning)
                            {
                                try
                                {
                                    mc.Compile();
                                    _c.SharedTrialCompiled.Add(m);
                                    _c.SharedCallEdges[m] = mc.SharedDirectCallees!;
                                }
                                catch (SharedBodyTaintException ex)
                                {
                                    _c.SharedTaint[m] = ex.Kind;
                                }
                                // Must-escape (Compilation.IsMustEscape): never a taint, never a
                                // fallback — the same filter the measure-mode arm below carries.
                                // A canonical trial DOES mint instantiations nothing else in the
                                // run names (the emitter-invented SZArrayEnumerable<E> wrapper of
                                // an array-to-collection boundary, a delegate ctor resolved under
                                // the canonical context), so the bound can genuinely trip in here;
                                // recorded as a taint it would silently change which bodies get
                                // shared and keep feeding the overrun it bounds.
                                catch (NotSupportedException ex) when (!Compilation.IsMustEscape(ex))
                                {
                                    // A placeholder-only gap (e.g. an intrinsic keyed on
                                    // the concrete type): unshareable, users fall back.
                                    _c.SharedTaint[m] = "unsupported";
                                    if (EnvKnobs.BoolIsOne(EnvKnobs.SharedDump))
                                        Console.WriteLine(
                                            $"dn2cpp: shared-generics unsupported {m.CppName}: {ex.Message}");
                                }
                            }
                            else if (diagnostics is null)
                            {
                                string canon = mc.Compile();
                                emitBody?.Invoke(m, canon);
                                compiledMethods.Add(m);
                            }
                            else
                            {
                                // Measure mode: a canonical survivor's throw becomes a gap
                                // row instead of aborting the run (the normal-emit branch
                                // above keeps the invariant that a throw is a real error).
                                try
                                {
                                    string canon = mc.Compile();
                                    emitBody?.Invoke(m, canon);
                                    compiledMethods.Add(m);
                                }
                                // Must-escape (Compilation.IsMustEscape): never a gap row, never a fallback.
                                catch (Exception ex) when (!Compilation.IsMustEscape(ex))
                                {
                                    diagnostics.Add(MeasureGap.From("compile", m, ex));
                                }
                            }
                            continue;
                        }
                        if (diagnostics is null)
                        {
                            var body = new MethodCompiler(_c, m, literals, _backend).Compile();
                            if (!planning)
                            {
                                emitBody?.Invoke(m, body);
                                compiledMethods.Add(m);
                            }
                            continue;
                        }
                        try
                        {
                            string body = new MethodCompiler(_c, m, literals, _backend).Compile();
                            emitBody?.Invoke(m, body);
                            compiledMethods.Add(m);
                        }
                        // Measure mode only (diagnostics != null): record any exception, by type,
                        // and keep going — an unexpected throw must not truncate the gap inventory.
                        // The must-escape pair (Compilation.IsMustEscape) is the exception: recording
                        // the monomorphization bound and compiling on would feed the very growth it
                        // exists to stop.
                        catch (Exception ex) when (!Compilation.IsMustEscape(ex))
                        {
                            diagnostics.Add(MeasureGap.From("compile", m, ex));
                        }
                    }
                    windowStart = windowEnd;
                }
            }
            finally
            {
                workers?.Stop();
            }
            // Complete any generics the just-compiled bodies instantiated, so their
            // layouts are ready for the struct/type-info emit below and the next
            // round can pick up any of their reachable methods. In measure mode a
            // partially-discovered generic can leave the pending queue inconsistent,
            // so tolerate a throw here too and end the pass.
            if (diagnostics is null)
                _c.CompletePendingSpecializations();
            else
                try { _c.CompletePendingSpecializations(); }
                catch (Exception ex) when (!Compilation.IsMustEscape(ex))
                { diagnostics.Add(MeasureGap.From("compile-spec", null, ex)); break; }
        }
    }

    /// <summary>Self-hosting feasibility harness: compile every reachable method body,
    /// collecting per-method failures instead of stopping at the first, and return them. No
    /// C++ is emitted and no backend epilogue runs — so an entry point taking
    /// <c>string[] args</c> (which the console epilogue does not yet support) does not stop
    /// the measurement. Driven by <c>gates/selfhost-measure.sh</c> through the CLI
    /// <c>--measure</c> flag.</summary>
    public MeasureResult MeasureGaps()
    {
        var diagnostics = new List<MeasureGap>();
        // Reachability-phase gaps were collected during the Compilation ctor (before
        // this emitter existed); fold them in first.
        if (_c.ReachabilityDiagnostics is { } reach)
            foreach (var (m, ex) in reach)
                diagnostics.Add(MeasureGap.From("reach", m, ex));
        int reachGaps = diagnostics.Count;
        var compiledMethods = new List<MethodInfo>();
        if (_c.SharedGenericsEnabled)
        {
            // Same two-pass canonical-shared-generics protocol as Emit(): without the
            // planning pass, any canonical body that reads its rgctx trips the "planning
            // pass did not record it" assert and aborts the whole measurement instead of
            // recording a gap row. The planning pass gets a throwaway diagnostics list (not
            // null): with null, a non-canonical method's gap would abort the planning pass
            // itself; gap rows are collected only from the real pass below.
            //
            // Discovery → Planning directly: --measure runs no LayoutClosure up front — its
            // guarded closure runs LAST, where a layout failure is a gap row rather than the
            // run failing. The grouping consequence the up-front closure exists for does not
            // apply, since measure emits nothing.
            _c.EnterPhase(EmitPhase.Planning);
            CompileReachableBodies(new LiteralPool(), null, new List<MethodInfo>(),
                diagnostics: new List<MeasureGap>());
            Timing.Mark("measure-plan-bodies");
            _c.FinalizeSharedGenerics((cls, m) => _backend.ShouldSkipMethodBody(cls, m));
            _c.ResetRgctxForEmission();
            _c.ResetSharedPlanningState();
            Timing.Mark("measure-finalize-shared");
        }
        // The real pass runs under the Emission phase even though no C++ is emitted: it
        // replays the emission-pass protocol (FillRgctxTables derives its failure-policy arm
        // from the phase). The METHOD-symbol half of the call-symbol audit arms here too —
        // a mode that drops failing bodies cannot take AssertCalledBodiesEmitted's diff
        // as-is, but the dangling sweep below unions the dropped bodies' symbols into the
        // defined set first, and what survives IS a cut => route violation. The full
        // BeginCallSymbolAudit stays uncalled: its other halves diff against emissions
        // (type-infos, struct layouts) this mode never produces.
        _c.BeginMeasureSymbolAudit();
        // Neither pass keeps body text or literals: --measure emits no C++, so every
        // byte of both would be built, retained for the length of the pass, and dropped
        // unread.
        CompileReachableBodies(new LiteralPool(), null, compiledMethods, diagnostics);
        Timing.Mark("measure-compile-bodies");
        // The type-layout closure. Field types are decoded on demand and what demands them
        // is the emit set, so a mode that reports whether a program can be transpiled has to
        // walk it — otherwise it answers for the bodies and stays silent about the types
        // they are written in, reporting a program as transpilable that emission cannot
        // transpile: the one lie a feasibility harness must not tell.
        //
        // Guarded, because ComputeEmitted is not: on the emit path a decode failure is the
        // run failing, here it is a row. The two must-escape exceptions still escape — a
        // strict-completion violation is a transpiler bug, and recording a bound overrun
        // while continuing would feed the very growth the bound exists to stop.
        try
        {
            ComputeEmitted();
        }
        catch (Exception ex) when (!Compilation.IsMustEscape(ex))
        {
            diagnostics.Add(MeasureGap.From("layout", null, ex));
        }
        Timing.Mark("measure-layout");
        // "Attempted" counts compile-phase bodies only (reach gaps never got that far, and
        // the dangling rows below are not bodies at all) — snapshot it before the sweep.
        int attempted = compiledMethods.Count + (diagnostics.Count - reachGaps);
        // The dangling-symbol sweep: AssertCalledBodiesEmitted's diff, adapted to a mode
        // that DROPS failing bodies. Every drop left a gap row carrying the dropped body's
        // C++ symbol, so unioning those into the defined set removes exactly the false
        // positives; a symbol that still survives is a real cut => route violation, reported
        // as a `dangling` gap row rather than a failure, because --measure's contract is to
        // keep draining. Compared by SYMBOL STRING like the assert, and decode-free. A
        // `compile-spec` row means the body pass was truncated mid-fixpoint — bodies dropped
        // with no row to union — so the sweep is skipped as unsound rather than reported
        // wrong (NamedSymbols null tells the driver to say so).
        int? namedCount = null;
        if (_c.NamedBodySymbols is { } named
            && !diagnostics.Any(g => g.Phase == "compile-spec"))
        {
            namedCount = named.Count;
            var defined = new HashSet<string>(StringComparer.Ordinal);
            foreach (var m in compiledMethods)
                defined.Add(m.CppName);
            foreach (var g in diagnostics)
                if (g.CppSymbol is not null)
                    defined.Add(g.CppSymbol);
            // Sorted by callee symbol so the row order is a function of the input, like
            // every other numbering the mode hands out.
            foreach (var (callee, caller) in named
                         .Where(kv => !defined.Contains(kv.Key.CppName))
                         .OrderBy(kv => kv.Key.CppName, StringComparer.Ordinal))
                diagnostics.Add(MeasureGap.Dangling(callee, caller));
            Timing.Mark("measure-dangling");
        }
        ReportParallelBodyStats();
        return new MeasureResult(attempted, compiledMethods.Count, diagnostics, namedCount);
    }

    /// <summary>Emits an <c>extern "C"</c> wrapper with default visibility for every
    /// compiled <c>[UnmanagedCallersOnly(EntryPoint = ...)]</c> method, so a native
    /// host can <c>dlsym</c>/link the entry point from a shared-library build. The CLR
    /// restricts such methods to blittable parameter/return types, so the wrapper is a
    /// direct passthrough of the method's C++ signature. Two methods exporting the
    /// same entry-point name are a hard error here (the alternative is a linker
    /// duplicate-symbol failure with a far less actionable message).</summary>
    private void EmitUnmanagedExports(StringBuilder sb, List<MethodInfo> compiledMethods)
    {
        var exports = compiledMethods
            .Where(m => m is { IsUnmanagedCallersOnly: true, UnmanagedEntryPoint: not null }
                && !_backend.ShouldSkipMethodBody(m.DeclaringClass, m))
            .OrderBy(m => m.UnmanagedEntryPoint, StringComparer.Ordinal)
            .ToList();
        if (exports.Count == 0)
            return;

        var byName = new Dictionary<string, MethodInfo>();
        foreach (var m in exports)
        {
            if (!m.IsStatic)
                throw new NotSupportedException(
                    $"{m.DeclaringClass.FullName}.{m.Name}: [UnmanagedCallersOnly] on an instance method is invalid");
            if (!byName.TryAdd(m.UnmanagedEntryPoint!, m))
            {
                var first = byName[m.UnmanagedEntryPoint!];
                throw new NotSupportedException(
                    $"[UnmanagedCallersOnly] entry point '{m.UnmanagedEntryPoint}' is exported by both "
                    + $"{first.DeclaringClass.FullName}.{first.Name} and {m.DeclaringClass.FullName}.{m.Name}");
            }
        }

        sb.AppendLine("// ---- [UnmanagedCallersOnly] exports ----");
        // Default visibility keeps the symbol exported even under a host build that
        // compiles with -fvisibility=hidden; the Windows arm uses dllexport instead
        // (same split as DN2CPP_RT_EXPORT, runtime/core/dn2cpp.h: _WIN32, not
        // _MSC_VER — MinGW-w64 defines _WIN32 without _MSC_VER, and the visibility
        // attribute does not put a symbol in a Windows DLL's export table).
        sb.AppendLine("#if defined(_WIN32) && !defined(__CYGWIN__)");
        sb.AppendLine("#define DN2CPP_UCO_EXPORT extern \"C\" __declspec(dllexport)");
        sb.AppendLine("#else");
        sb.AppendLine("#define DN2CPP_UCO_EXPORT extern \"C\" __attribute__((visibility(\"default\")))");
        sb.AppendLine("#endif");
        foreach (var m in exports)
        {
            var ps = m.Signature.ParameterTypes.Select((p, i) => $"{CppTypes.Of(p)} a{i}").ToList();
            string ret = m.Signature.ReturnType.IsVoid ? "void" : CppTypes.Of(m.Signature.ReturnType);
            string args = string.Join(", ", Enumerable.Range(0, ps.Count).Select(i => $"a{i}"));
            string call = $"{m.CppName}({args})";
            sb.AppendLine($"DN2CPP_UCO_EXPORT {ret} {m.UnmanagedEntryPoint}({string.Join(", ", ps)}) "
                + (m.Signature.ReturnType.IsVoid ? $"{{ {call}; }}" : $"{{ return {call}; }}"));
        }
        sb.AppendLine();
    }

    /// <summary>Emits the runtime-init prologue shared by backend epilogues
    /// (runtime init, string literal table init, then static constructors).</summary>
    internal void EmitInitCalls(StringBuilder sb, IReadOnlyList<MethodInfo> cctors)
    {
        sb.AppendLine(_c.UsesPlatformIsa
            ? "    dn2cpp_runtime_init(&dn2cpp_cpu_features_resolve);"
            : "    dn2cpp_runtime_init();");
        // The image's per-signature vtable trap thunks, so dn2cpp_exception_message can
        // recognise a trapped get_Message slot without calling it. Before any managed
        // code, like every install below.
        if (_vcallTrapThunks.Count > 0)
        {
            sb.AppendLine($"    static const void* const vtrapfns[] = {{ {string.Join(", ", _vcallTrapThunks.Select(t => $"(const void*)&{t}"))} }};");
            sb.AppendLine($"    dn2cpp_register_vcall_traps(vtrapfns, {_vcallTrapThunks.Count});");
        }
        // A --shadow-stack image tells the flag-independent runtime that AOT bodies
        // carry frame guards (the hot-update interpreter's reader); gated, so
        // flag-off output stays byte-identical.
        if (_c.ShadowStackEnabled)
            sb.AppendLine("    dn2cpp_shadow_stack_mark_enabled();");
        // String's interface rows go in before any managed code (cctors included)
        // can dispatch a string through an interface.
        if (_stringItfMap is { } sim)
            sb.AppendLine($"    dn2cpp_string_set_interfaces({sim.Sym}, {sim.Count});");
        // The boxed-enum rows the same way — a cctor can already dispatch
        // IConvertible/IComparable/IFormattable on a boxed enum.
        if (_enumItfMap is { } eim)
            sb.AppendLine($"    dn2cpp_enum_set_interfaces({eim.Sym}, {eim.Count});");
        // The intrinsic types' IDisposable rows go in the same way, before any managed
        // code can `using`-dispose one through the interface.
        foreach (var (tiSym, sym, count) in _intrinsicItfMaps)
            sb.AppendLine($"    dn2cpp_intrinsic_set_interfaces(&{tiSym}, {sym}, {count});");
        // The shared reference-element SZArray fallback table goes in before any managed
        // code too: a cctor can already dispatch a collection interface on an array it
        // reached through `object` (or on a runtime-built attribute array).
        if (_arrayRefFallbackItfMap is { } afm)
            sb.AppendLine($"    dn2cpp_array_set_ref_fallback_interfaces({afm.Sym}, {afm.Count});");
        // The multi-dimensional array dispatch table the same way: a cctor can already
        // dispatch a collection interface on a rank>=2 array, and MD type-infos are
        // runtime-interned so this table is their ONLY dispatch route.
        if (_mdArrayItfMap is { } mdm)
            sb.AppendLine($"    dn2cpp_array_set_md_fallback_interfaces({mdm.Sym}, {mdm.Count});");
        // The relation-only rows the two tables above cannot carry — the enumeration half.
        // Installed here rather than lazily for the same reason: a cctor can already call
        // Type.GetInterfaces() on an array type.
        if (_arrayNongenericItfRows is { } anr)
            sb.AppendLine($"    dn2cpp_array_set_nongeneric_interfaces({anr.Sym}, {anr.Count});");
        sb.AppendLine("    dn2cpp_init_strings();");
        // Route the eager startup pass through the same idempotent wrappers the use-site
        // guards call, so a cctor already run on first use is not run a second time here.
        // dn2cpp_cctor_run_startup swallows a managed failure: this prologue sits AHEAD of
        // the entry point's own try/catch, so an escaping exception reaches no handler and
        // std::terminates the process — over a type real .NET, initializing lazily, might
        // never have touched at all. The runtime remembers the failure and the first real
        // use re-raises it, which is exactly where .NET raises it. The declaring type's
        // reflection name rides beside the pointer because the swallowed failure is reported
        // through the host-boundary sink, and a report that cannot name the type is not one
        // anyone can act on.
        foreach (var cctor in cctors)
            sb.AppendLine($"    dn2cpp_cctor_run_startup(&{cctor.CppName}__ensure, "
                + $"\"{Compilation.ReflectionTypeName(cctor.DeclaringClass)}\");");
    }

    /// <summary>Emits an idempotent first-use wrapper (<c>inline void X__ensure()</c>
    /// over an <c>extern std::atomic&lt;int8_t&gt; X__done</c> flag) for every static
    /// constructor that is either run at startup (<paramref name="cctors"/>) or guarded
    /// at a use site (<see cref="Compilation.CctorEnsureTargets"/>). The wrapper
    /// fast-paths on an acquire load of the flag and funnels first use through
    /// <c>dn2cpp_cctor_run_once</c>, which gives .NET's multithreaded cctor semantics:
    /// exactly one body run with a happens-before edge to every later reader, a thread
    /// racing an in-flight init blocks until it completes, and a re-entrant access from
    /// the initializing thread itself returns to see the partially-initialized state
    /// (recursive-cctor semantics). A body that THROWS does not latch the flag — the
    /// failure is remembered and re-raised by every later guard, so no use site ever
    /// reads a half-initialized type as ready. A target whose cctor body was tree-shaken (e.g. a
    /// ReachCctor skip) gets a nullptr body so any straggler use site still links. The
    /// wrapper is <c>inline</c> in the header so its fast path inlines into every
    /// calling TU (avoiding an opaque cross-TU call that would also force a reload of
    /// the extern statics), while plain <c>inline</c> keeps one consistent address for
    /// the rgctx CctorEnsureFn slot; the flag's single definition lives in the primary
    /// Data TU.</summary>
    private void EmitCctorEnsures(CppOutput o, IReadOnlyList<MethodInfo> cctors, List<MethodInfo> compiledMethods)
    {
        var compiledSet = new HashSet<MethodInfo>(compiledMethods);
        // Deterministic union: the ordered startup cctors first, then any extra use-site
        // targets not among them (sorted by C++ name for stable output).
        var targets = new List<MethodInfo>(cctors);
        var seen = new HashSet<MethodInfo>(cctors);
        foreach (var cc in _c.CctorEnsureTargets.Where(cc => seen.Add(cc)).OrderBy(cc => cc.CppName, StringComparer.Ordinal))
            targets.Add(cc);
        if (targets.Count == 0)
            return;

        o.Header.AppendLine("// ---- static-constructor first-use guards ----");
        foreach (var cc in targets)
        {
            bool compiled = compiledSet.Contains(cc);
            o.Header.AppendLine($"extern std::atomic<int8_t> {cc.CppName}__done;");
            // Forward-declare the cctor body so the inline wrapper can name it wherever
            // the method's own prototype lands in the header (a redundant declaration is
            // harmless); a tree-shaken cctor has no body to declare or run.
            if (compiled)
                o.Header.AppendLine($"void {cc.CppName}();");
            string body = compiled ? $"&{cc.CppName}" : "nullptr";
            o.Header.AppendLine(
                $"inline void {cc.CppName}__ensure() {{ if ({cc.CppName}__done.load(std::memory_order_acquire)) return; "
                + $"dn2cpp_cctor_run_once(&{cc.CppName}__done, {body}); }}");
        }
        o.Header.AppendLine();

        o.Data.AppendLine("// ---- static-constructor first-use guards ----");
        foreach (var cc in targets)
            o.Data.AppendLine($"std::atomic<int8_t> {cc.CppName}__done{{0}};");
        o.Data.AppendLine();
    }

    /// <summary>The program entry point, exposed so a backend epilogue (e.g. the
    /// console backend's <c>main</c>) can reference it.</summary>
    internal MethodInfo? EntryPoint => _c.EntryPoint;

    /// <summary>All loaded types, exposed so a backend epilogue can discover the
    /// classes it needs to register (e.g. the Godot backend's export tables).</summary>
    internal IReadOnlyList<ClassInfo> Classes => _c.Classes;

    /// <summary>Whether <paramref name="cls"/> comes from the app module (the
    /// assembly being transpiled) rather than a reference. The public-surface
    /// root rule only guarantees emission for the app module's public methods —
    /// a reference assembly (e.g. the real CoreLib) is tree-shaken, so most of
    /// its public methods have IL but no emitted body.</summary>
    internal bool IsAppModuleClass(ClassInfo cls) => cls.Module == _c.AppModule;

    /// <summary>The compilation's shared signature decoder, exposed for a backend
    /// epilogue that reads raw metadata rows itself (e.g. the Godot backend
    /// decoding a [Signal] delegate's Invoke signature for the signal tables).</summary>
    internal SignatureProvider SigProvider => _c.SigProvider;

    /// <summary>Whether <paramref name="cls"/> is actually emitted in this
    /// compilation (its <c>ti_…</c> type-info symbol exists), so a backend
    /// epilogue references type-infos only for types that are really there.</summary>
    internal bool IsEmitted(ClassInfo cls) => _emit.Contains(cls);

    /// <summary>The noted SZArray element types, keyed by the mangle that names
    /// their emitted <c>ti_arr_&lt;key&gt;</c> symbol — exposed so a backend
    /// epilogue can register the precise array type-infos of this compilation.</summary>
    internal IReadOnlyDictionary<string, TypeDesc> ArrayElementTypes => _c.ArrayElementTypes;

    /// <summary>The emitted String interface-map symbol + row count, or null when the
    /// program never uses a string through an interface. Consumed by
    /// <see cref="EmitInitCalls"/>, which installs the rows onto the runtime-owned
    /// <c>dn2cpp_string_type</c> before any managed code runs.</summary>
    private (string Sym, int Count)? _stringItfMap;

    /// <summary>The emitted <c>object</c>-element SZArray interface-map symbol + row
    /// count, or null when no reference-element array was noted (then no array can need
    /// the fallback). Consumed by <see cref="EmitInitCalls"/>, which registers it as the
    /// runtime's shared reference-element fallback dispatch table
    /// (<c>dn2cpp_array_set_ref_fallback_interfaces</c>) before any managed code
    /// runs. Set by <see cref="EmitArrayTypeInfos"/>, which emits into the same primary
    /// Data TU the init prologue lands in — the symbol is file-local, like the string
    /// map's.</summary>
    private (string Sym, int Count)? _arrayRefFallbackItfMap;

    /// <summary>The emitted multi-dimensional array dispatch-map symbol + row count, or
    /// null when the program mints no MD array (then no rank&gt;=2 receiver can exist and
    /// nothing is installed — an MD-free program's output stays byte-identical).
    /// Consumed by <see cref="EmitInitCalls"/>, which registers it as the runtime's
    /// shared rank&gt;=2 dispatch table (<c>dn2cpp_array_set_md_fallback_interfaces</c>)
    /// before any managed code runs. Set by
    /// <see cref="EmitMdArrayItfMap"/>, which emits into the same primary Data TU the
    /// init prologue lands in — the symbol is file-local, like the string map's.</summary>
    private (string Sym, int Count)? _mdArrayItfMap;

    /// <summary>The emitted relation-only row set for the six non-generic array
    /// interfaces + its row count, or null when the load set does not carry all six
    /// (then nothing is installed and every reader keeps today's answer). Consumed by
    /// <see cref="EmitInitCalls"/>, which registers it with
    /// <c>dn2cpp_array_set_nongeneric_interfaces</c> before any managed code
    /// runs. Set by <see cref="EmitArrayNongenericItfRows"/>, which emits into the same
    /// primary Data TU the init prologue lands in — the symbol is file-local, like the
    /// two dispatch maps'.</summary>
    private (string Sym, int Count)? _arrayNongenericItfRows;

    /// <summary>Emits the relation-only row set for the six non-generic interfaces every
    /// CLR array implements — <c>{ &amp;ti_X, nullptr }</c> per interface, the same shape
    /// <c>TypeMetadataEmitter.RenderItfTables</c> gives an interface, an abstract class or a
    /// never-boxed value type.
    ///
    /// <para>A sibling of the two dispatch maps, not a replacement: a dispatch map answers a
    /// CALL and its rows carry slot tables, this answers <c>Type.GetInterfaces()</c> and its
    /// rows carry nullptr, which is what keeps it out of every dispatch reader (they all
    /// guard on <c>slots != nullptr</c>). Without it a per-element SZArray map reports only
    /// the interfaces <c>SZArrayEnumerable&lt;T&gt;</c> implements and structurally cannot
    /// report <c>ICloneable</c>/<c>IStructuralComparable</c>/<c>IStructuralEquatable</c>,
    /// and an MD array — runtime-interned with no rows at all — reports none.</para>
    ///
    /// <para>Nothing here is per-element, which is why ONE table serves every array in
    /// the program: all six are non-generic, so they say nothing about the element.</para>
    /// </summary>
    private void EmitArrayNongenericItfRows(StringBuilder sb)
    {
        if (_c.ArrayNongenericInterfaces is not { Count: > 0 } itfs)
            return;
        sb.AppendLine("// ---- non-generic array interface rows (relation-only, runtime-installed) ----");
        var entries = itfs
            .Select(itf => $"{{ {TypeInfoRef(itf, "non-generic array interface relation row")}, nullptr }}")
            .ToList();
        sb.AppendLine($"static const Dn2CppInterfaceEntry arr_nongeneric_itfs[] = {{ {string.Join(", ", entries)} }};");
        _arrayNongenericItfRows = ("arr_nongeneric_itfs", entries.Count);
    }

    /// <summary>Emits the multi-dimensional array dispatch table:
    /// one <c>Dn2CppInterfaceEntry</c> per non-generic interface a CLR rank&gt;=2 array
    /// implements (the six of <see cref="Compilation.IsMdArrayMapInterface"/>), each
    /// reached method's slot a thunk that wraps the receiver (passed as <c>this</c>)
    /// into a fresh non-generic <c>MDArrayEnumerable</c> and forwards to the wrapper's
    /// concrete impl. An MD type-info is runtime-interned and carries no rows of its
    /// own, so this table — registered with the runtime by <see cref="EmitInitCalls"/>
    /// — is every rank&gt;=2 array's ONLY dispatch route; one element-agnostic table is
    /// sound because the wrapper reads/writes through the System.Array reflection
    /// surface. An unreached method gets a trap stub that aborts naming the
    /// (receiver, interface, method) triple — NEVER a nullptr slot. The
    /// interfaces are all non-generic, so there are no canonical alias rows (contrast
    /// <see cref="EmitArrayEnumerableMap"/>) and no ref-erasure concern for the wrapper
    /// the thunks hand back: the cursor is dispatched as the non-generic
    /// <c>IEnumerator</c>, ordinary class dispatch on the wrapper's own emitted
    /// type-info.</summary>
    private void EmitMdArrayItfMap(StringBuilder sb)
    {
        if (_c.MdArrayInterfaces is not { } mai)
            return;
        // Defensive, as EmitArrayEnumerableMap: a tree-shaken wrapper/ctor edge must
        // degrade to "no map" (the runtime keeps its loud abort), never to a dangle.
        if (!_emit.Contains(mai.Wrapper) || !_c.Reachable.Contains(mai.Ctor) || mai.Dispatches.Count == 0)
            return;
        sb.AppendLine("// ---- multi-dimensional array interface-dispatch map (runtime-installed) ----");
        string arrCpp = CppTypes.Of(mai.Ctor.Signature.ParameterTypes[0]); // the System.Array C++ type
        // Named trap stubs for unreached slots whose return is an indirectly-returned
        // struct (argument 0 may be the caller's hidden result buffer — same split as
        // the class itables); pooled per emission, all rows live in this one TU.
        var stubs = new Dictionary<string, string>(System.StringComparer.Ordinal);
        string SlotMissStub(string desc, MethodInfo decl)
        {
            if (!stubs.TryGetValue(desc, out var name))
            {
                name = $"mdarrslotmiss_{stubs.Count}";
                sb.AppendLine(NamedSlotMissStubDef(name, "dn2cpp_itf_slot_missing_named", desc, decl));
                stubs[desc] = name;
            }
            return name;
        }
        var entries = new List<string>();
        int i = 0;
        foreach (var disp in mai.Dispatches)
        {
            // Slot table sized to cover every declared method's vtable slot; the
            // default (a row the wiring resolved no impl for) is the argument-free
            // anonymous trap — with no decl there is no return type to decide whether
            // argument 0 is the receiver or a struct-return buffer, so read nothing.
            int slotCount = disp.Methods.Max(m => m.Decl.VtableSlot) + 1;
            var slots = new string[slotCount];
            for (int s = 0; s < slotCount; s++)
                slots[s] = "(const void*)&dn2cpp_itf_slot_missing_anon";
            foreach (var (decl, impl) in disp.Methods)
            {
                if (decl.VtableSlot < 0)
                    continue;
                if (!_c.Reachable.Contains(impl))
                {
                    slots[decl.VtableSlot] = ReceiverIsFirstArg(decl.Signature.ReturnType)
                        ? $"(const void*)&{SlotTrapThunk(decl, vcall: false) ?? "dn2cpp_itf_slot_missing"}"
                        : $"(const void*)&{SlotMissStub($"(rank>=2 array)::{disp.Itf.FullName}.{decl.Name}", decl)}";
                    continue;
                }
                string retCpp = CppTypes.Of(decl.Signature.ReturnType);
                var ps = decl.Signature.ParameterTypes;
                // The forwarding target's DEFINITION parameter types, as
                // EmitArrayEnumerableMap: the wrapper is non-generic, so decl and impl
                // types agree today and the cast collapses to identity — kept so a
                // widened impl shape can never silently mis-type a forward.
                var implPs = impl.Emittable.Signature.ParameterTypes;
                var pdecls = new List<string> { $"{disp.Itf.CppStructName}* self" };
                var fargs = new List<string> { "w" };
                for (int p = 0; p < ps.Length; p++)
                {
                    string pCpp = CppTypes.Of(ps[p]);
                    pdecls.Add($"{pCpp} p{p}");
                    string implCpp = p < implPs.Length ? CppTypes.Of(implPs[p]) : pCpp;
                    fargs.Add(pCpp == implCpp ? $"p{p}" : $"({implCpp})p{p}");
                }
                string thunk = $"mdarrthunk_{i}_{decl.VtableSlot}";
                sb.AppendLine($"static {retCpp} {thunk}({string.Join(", ", pdecls)}) {{");
                sb.AppendLine($"    {mai.Wrapper.CppStructName}* w = ({mai.Wrapper.CppStructName}*)dn2cpp_alloc(sizeof({mai.Wrapper.CppStructName}));");
                sb.AppendLine($"    ((Dn2CppObject*)w)->type = {TypeInfoRef(mai.Wrapper, "MDArray-enumerable wrapper allocation")};");
                sb.AppendLine($"    {mai.Ctor.Emittable.CppName}(w, ({arrCpp})self);");
                string call = $"{impl.Emittable.CppName}({string.Join(", ", fargs)})";
                sb.AppendLine(retCpp == "void" ? $"    {call};" : $"    return ({retCpp}){call};");
                sb.AppendLine("}");
                slots[decl.VtableSlot] = $"(const void*)&{thunk}";
            }
            sb.AppendLine($"static const void* mdarr_itf_{i}[] = {{ {string.Join(", ", slots)} }};");
            entries.Add($"{{ {TypeInfoRef(disp.Itf, "MDArray interface-dispatch map")}, mdarr_itf_{i} }}");
            i++;
        }
        sb.AppendLine($"static const Dn2CppInterfaceEntry mdarr_itfs[] = {{ {string.Join(", ", entries)} }};");
        _mdArrayItfMap = ("mdarr_itfs", entries.Count);
    }

    /// <summary>Emits String's interface-dispatch rows (one slot table per interface
    /// String implements, wired by <see cref="Compilation.NoteStringInterfaces"/>).
    /// Unlike every other reference type, String has no emitted type-info to carry
    /// them — the generated init prologue registers the rows onto the runtime's
    /// <c>dn2cpp_string_type</c> instead. Slot conventions match the per-class
    /// tables: a reachable impl's function pointer (String impls take the
    /// <c>Dn2CppString*</c> receiver directly), nullptr for a never-dispatched slot —
    /// the row's presence alone makes <c>is</c>/<c>castclass</c> succeed. Canonical
    /// alias rows mirror the per-class/array maps so a shared body's resolve against
    /// the canonical handle dispatches on a string receiver too.</summary>
    private void EmitStringInterfaceMap(StringBuilder sb)
    {
        if (_c.StringInterfaces is not { } si || si.Dispatches.Count == 0)
            return;
        sb.AppendLine("// ---- String interface-dispatch map (installed onto dn2cpp_string_type at init) ----");
        var entries = new List<string>();
        int i = 0;
        foreach (var disp in si.Dispatches)
        {
            if (!_emit.Contains(disp.Itf))
            {
                i++;
                continue;
            }
            int slotCount = disp.Methods.Max(m => m.Decl.VtableSlot) + 1;
            var slots = new string[slotCount];
            for (int s = 0; s < slotCount; s++)
                slots[s] = "nullptr";
            foreach (var (decl, impl) in disp.Methods)
            {
                if (!_c.Reachable.Contains(impl))
                    continue;
                slots[decl.VtableSlot] = $"(const void*)&{impl.Emittable.CppName}";
            }
            sb.AppendLine($"static const void* str_itf_{i}[] = {{ {string.Join(", ", slots)} }};");
            entries.Add($"{{ {TypeInfoRef(disp.Itf, "String interface-dispatch map")}, str_itf_{i} }}");
            if (_c.SharedGenericsEnabled && _c.CanonicalInterfaceOf(disp.Itf) is { } citf)
                entries.Add($"{{ {TypeInfoRef(citf, "String interface-dispatch map (canonical alias row)")}, str_itf_{i} }}");
            i++;
        }
        if (entries.Count == 0)
            return;
        sb.AppendLine($"static const Dn2CppInterfaceEntry str_itfs[] = {{ {string.Join(", ", entries)} }};");
        _stringItfMap = ("str_itfs", entries.Count);
    }

    /// <summary>The emitted boxed-enum interface-map symbol + row count, or null when
    /// the program never coerces/casts an enum toward System.Enum's CLR interfaces
    /// (then no boxed enum can be interface-dispatched). Consumed by
    /// <see cref="EmitInitCalls"/>, which installs it onto the runtime-owned
    /// <c>dn2cpp_enum_type</c> (<c>dn2cpp_enum_set_interfaces</c>) before any
    /// managed code runs. Set by <see cref="EmitEnumInterfaceMap"/>.</summary>
    private (string Sym, int Count)? _enumItfMap;

    /// <summary>Emits the boxed-enum interface-dispatch rows (one slot table per
    /// interface System.Enum implements, wired by
    /// <see cref="Compilation.NoteEnumInterfaces"/>). A per-enum emitted type-info
    /// deliberately carries no rows — its base IS the runtime-owned
    /// <c>dn2cpp_enum_type</c> and <c>dn2cpp_resolve_interface_walk</c> consults the
    /// base chain — so ONE map installed there by the generated init prologue serves
    /// every enum, the same wiring as <see cref="EmitStringInterfaceMap"/> and at the
    /// same program-constant cost. Slot conventions: a reachable impl's function
    /// pointer (System.Enum is modeled as a reference type, so its impls take the box
    /// pointer directly — no unboxing thunks); an unreached impl degrades to the
    /// dispatch TRAP, never nullptr — this table is handed to dispatch sites, and a
    /// hole must abort by name rather than jump through 0. The one
    /// slot that is never an impl is ISpanFormattable.TryFormat, cut by
    /// CvEnumTryFormat and routed here to the runtime's own boxed-enum formatter. The four
    /// interfaces are non-generic, so there are no canonical alias rows (contrast
    /// the string map).</summary>
    private void EmitEnumInterfaceMap(StringBuilder sb)
    {
        if (_c.EnumInterfaces is not { } ei || ei.Dispatches.Count == 0)
            return;
        sb.AppendLine("// ---- boxed-enum interface-dispatch map (installed onto dn2cpp_enum_type at init) ----");
        // Named trap stubs for unreached slots whose return is an indirectly-returned
        // struct (argument 0 may be the caller's hidden result buffer — same split as
        // the class itables); pooled per emission, all rows live in this one TU.
        var stubs = new Dictionary<string, string>(System.StringComparer.Ordinal);
        string SlotMissStub(string desc, MethodInfo decl)
        {
            if (!stubs.TryGetValue(desc, out var name))
            {
                name = $"enumslotmiss_{stubs.Count}";
                sb.AppendLine(NamedSlotMissStubDef(name, "dn2cpp_itf_slot_missing_named", desc, decl));
                stubs[desc] = name;
            }
            return name;
        }
        var entries = new List<string>();
        int i = 0;
        foreach (var disp in ei.Dispatches)
        {
            if (!_emit.Contains(disp.Itf))
            {
                i++;
                continue;
            }
            int slotCount = disp.Methods.Max(m => m.Decl.VtableSlot) + 1;
            var slots = new string[slotCount];
            for (int s = 0; s < slotCount; s++)
                slots[s] = "(const void*)&dn2cpp_itf_slot_missing_anon";
            foreach (var (decl, impl) in disp.Methods)
            {
                // The boxed mouth of CoreIntrinsics.CvEnumTryFormat's cut: the impl's real
                // IL reaches an InternalCall, so it is never in the tree, and the trap below
                // aborted the process where .NET prints "Monday". The runtime slot formats
                // through the same dn2cpp_enum_format the IFormattable mouth reaches, so the
                // two cannot answer differently.
                if (CoreIntrinsics.CvEnumTryFormat.Matches(impl.DeclaringClass.FullName, impl.Name))
                {
                    slots[decl.VtableSlot] = "(const void*)&dn2cpp_enum_box_try_format";
                    continue;
                }
                if (!_c.Reachable.Contains(impl))
                {
                    slots[decl.VtableSlot] = ReceiverIsFirstArg(decl.Signature.ReturnType)
                        ? $"(const void*)&{SlotTrapThunk(decl, vcall: false) ?? "dn2cpp_itf_slot_missing"}"
                        : $"(const void*)&{SlotMissStub($"System.Enum::{disp.Itf.FullName}.{decl.Name}", decl)}";
                    continue;
                }
                slots[decl.VtableSlot] = $"(const void*)&{impl.Emittable.CppName}";
            }
            sb.AppendLine($"static const void* enum_itf_{i}[] = {{ {string.Join(", ", slots)} }};");
            entries.Add($"{{ {TypeInfoRef(disp.Itf, "Enum interface-dispatch map")}, enum_itf_{i} }}");
            i++;
        }
        if (entries.Count == 0)
            return;
        sb.AppendLine($"static const Dn2CppInterfaceEntry enum_itfs[] = {{ {string.Join(", ", entries)} }};");
        _enumItfMap = ("enum_itfs", entries.Count);
    }

    /// <summary>The emitted intrinsic-type interface maps, in
    /// <see cref="Compilation.IntrinsicInterfaceRows"/> order: the runtime type-info to
    /// install onto, the map symbol and its row count. Empty when the program mints none
    /// of the intrinsics, or emits none of their interfaces' type-infos. Consumed by
    /// <see cref="EmitInitCalls"/>, which installs each map
    /// (<c>dn2cpp_intrinsic_set_interfaces</c>) before any managed code runs.</summary>
    private readonly List<(string TypeInfoSym, string Sym, int Count)> _intrinsicItfMaps = [];

    /// <summary>Emits one interface-dispatch map per instantiated intrinsic
    /// (<see cref="Compilation.NoteIntrinsicInterfaces"/>): the row's thunk at the
    /// interface declaration's slot, so
    /// the interface mouth — <c>using (…)</c>, an interface-typed local, a cast — reaches
    /// the same disposal the direct intrinsic call takes. The thunk is a function of the
    /// interface method's exact shape rather than a pointer to the runtime helper: the
    /// dispatch site casts the slot to <c>void Dispose()</c> and e.g.
    /// <c>dn2cpp_timer_dispose</c> returns int32_t, which through a <c>void</c> fnptr is
    /// a wasm signature trap. A row is emitted only when its interface's type-info is in
    /// the emit set — without one, nothing can dispatch through the interface and no map
    /// is installed. The interfaces here are non-generic, so there is no canonical alias
    /// row (contrast <see cref="EmitStringInterfaceMap"/>).</summary>
    private void EmitIntrinsicInterfaceMaps(StringBuilder sb)
    {
        // A row whose slot the loaded framework spells differently is fatal only here,
        // where the emit set is known: minting the intrinsic without ever dispatching
        // through the interface installs nothing, so nothing can trap.
        foreach (var mismatch in _c.IntrinsicInterfaceMismatches)
            if (_emit.Contains(mismatch.Itf))
                throw new NotSupportedException(mismatch.Message);
        var infos = new List<Compilation.IntrinsicInterfaceInfo>();
        foreach (var info in _c.IntrinsicInterfaces)
        {
            if (_emit.Contains(info.Itf))
                infos.Add(info);
        }
        if (infos.Count == 0)
            return;
        sb.AppendLine("// ---- intrinsic-type interface-dispatch maps (installed at init) ----");
        int thunk = 0;
        // dn2cpp_intrinsic_set_interfaces REPLACES the type-info's interface list, so one
        // type-info may get exactly one map: the run-length scan below requires the rows of
        // one intrinsic to be adjacent, and a second run for the same type-info would drop
        // the first run's interfaces with no error anywhere.
        var mapped = new HashSet<string>(StringComparer.Ordinal);
        for (int begin = 0; begin < infos.Count;)
        {
            string typeInfoSym = infos[begin].Row.TypeInfoSym;
            if (!mapped.Add(typeInfoSym))
                throw new InvalidOperationException(
                    $"intrinsic interface rows for {typeInfoSym} are not adjacent in "
                    + "Compilation.IntrinsicInterfaceRows; the second group's map would "
                    + "replace the first's.");
            var entries = new List<string>();
            int end = begin;
            while (end < infos.Count && infos[end].Row.TypeInfoSym == typeInfoSym)
            {
                var info = infos[end];
                var row = info.Row;
                sb.AppendLine($"// {row.IntrinsicName} : {row.ItfNamespace}.{row.ItfName}");
                string thunkBody = row.ThunkKind switch
                {
                    Compilation.IntrinsicInterfaceThunkKind.TimerDispose =>
                        $"static void {row.ThunkSym}(Dn2CppObject* o) {{ dn2cpp_timer_dispose(o); }}",
                    Compilation.IntrinsicInterfaceThunkKind.NoopDispose =>
                        $"static void {row.ThunkSym}(Dn2CppObject* o) {{ (void)o; }}",
                    Compilation.IntrinsicInterfaceThunkKind.TimerChange =>
                        $"static int32_t {row.ThunkSym}(Dn2CppObject* o, Dn2CppTimeSpan due, Dn2CppTimeSpan period) " +
                        "{ return dn2cpp_timer_change(o, due.ticks / 10000LL, period.ticks / 10000LL); }",
                    Compilation.IntrinsicInterfaceThunkKind.TimerDisposeAsync =>
                        $"static Dn2CppTaskAwaiter {row.ThunkSym}(Dn2CppObject* o) " +
                        "{ dn2cpp_timer_dispose(o); return Dn2CppTaskAwaiter{ nullptr }; }",
                    _ => throw new InvalidOperationException(
                        $"unknown intrinsic interface thunk kind {row.ThunkKind}"),
                };
                sb.AppendLine(thunkBody);
                int slot = info.SlotDecl.VtableSlot >= 0 ? info.SlotDecl.VtableSlot : 0;
                var slots = new string[slot + 1];
                for (int s = 0; s < slot; s++)
                    slots[s] = "nullptr";
                slots[slot] = $"(const void*)&{row.ThunkSym}";
                sb.AppendLine($"static const void* intr_itf_{thunk}[] = {{ {string.Join(", ", slots)} }};");
                entries.Add($"{{ {TypeInfoRef(info.Itf, "intrinsic-type interface-dispatch map")}, intr_itf_{thunk} }}");
                thunk++;
                end++;
            }
            string mapSym = $"intr_itfs_{_intrinsicItfMaps.Count}";
            sb.AppendLine($"static const Dn2CppInterfaceEntry {mapSym}[] = {{ {string.Join(", ", entries)} }};");
            _intrinsicItfMaps.Add((typeInfoSym, mapSym, entries.Count));
            begin = end;
        }
    }

    /// <summary>Per-element array type-infos: one <c>ti_arr_&lt;T&gt;</c> per
    /// SZArray element type reached via <c>newarr</c>/<c>typeof(T[])</c>, carrying
    /// DN2CPP_TF_ARRAY, the element's type-info (elementType — backs GetElementType) and
    /// rank 1. Each is registered under its CLR array name (e.g. "System.Int32[]") so
    /// Type.GetType("T[]") resolves to it. When an array of T is cast to
    /// <c>IEnumerable&lt;T&gt;</c> at runtime, the type-info also gets a real
    /// interface-dispatch map whose <c>GetEnumerator</c> wraps the array into the
    /// <c>SZArrayEnumerable&lt;T&gt;</c> enumerator — so the cast/foreach resolve on the
    /// array itself (the precise per-element identity makes this sound).</summary>
    private void EmitArrayTypeInfos(StringBuilder sb, HashSet<ClassInfo> emittedEnums)
    {
        if (_c.ArrayElementTypes.Count == 0 && _c.MdArrayTypes.Count == 0)
            return;
        sb.AppendLine("// ---- per-element array type-infos ----");
        // Content-intern pool for the SZArray dispatch tables (arritfpool_):
        // every map below is emitted into this single TU, so unlike the
        // class-metadata pools it never rolls. Address-sharing is safe for the
        // same reason as itfpool_ (dispatch compares .itf, only indexes .slots).
        var arrItfPools = new Dictionary<string, string>(System.StringComparer.Ordinal);
        // Named trap stubs for unreached dispatch-table slots (see EmitArrayEnumerableMap);
        // pooled alongside for the same single-TU reason.
        var arrSlotStubs = new Dictionary<string, string>(System.StringComparer.Ordinal);
        // elementType for an entry representing element[]: the element's own ti_, or — for
        // a jagged element (itself an array) — the inner array's ti_arr_/ti_md_ handle
        // (the recursion in NoteArrayElementType guarantees it was emitted).
        string ElemTi(TypeDesc element) =>
            element.Kind == TypeKind.SZArray
                ? "&ti_arr_" + Compilation.ArrayElemMangle(element.Element!)
                : element.Kind == TypeKind.MDArray
                ? "&ti_md_" + Compilation.ArrayElemMangle(element)
                : FieldTypeInfoExpr(element, emittedEnums);
        // Static MD-array type-infos: one ti_md_<T> per noted MD identity, the linkable
        // constant that constructed generic arguments and nested array elements name. Shape matches
        // dn2cpp_array_ti's fabricated identity (ARRAY|SEALED, real elementType and rank, no
        // rows — MD dispatch is the shared rank-keyed table); the registry row
        // EmitTypeRegistry takes from _arrayTypeSyms is what makes the interner answer THIS
        // handle for every `new T[,]` of the shape, so the static symbol and the interned
        // identity stay one.
        foreach (var (key, md) in _c.MdArrayTypes.OrderBy(kv => kv.Key, System.StringComparer.Ordinal))
        {
            string clr = ArrayClrName(md);
            sb.AppendLine($"const Dn2CppTypeInfo ti_md_{key} = {{ \"{clr}\", nullptr, 0, nullptr, nullptr, 0, nullptr, nullptr, nullptr, (DN2CPP_TF_ARRAY | DN2CPP_TF_SEALED), nullptr, 0, nullptr, 0, nullptr, 0, nullptr, 0, nullptr, 0, nullptr, nullptr, 0, nullptr, nullptr, 0, nullptr, 0, {ElemTi(md.Element!)}, {md.Rank}, nullptr, nullptr, nullptr, &ty_md_{key} }};");
            sb.AppendLine($"const Dn2CppType ty_md_{key} = {{ {{ &dn2cpp_type_type }}, &ti_md_{key} }};");
            _arrayTypeSyms[clr] = "&ti_md_" + key;
        }
        foreach (var (key, element) in _c.ArrayElementTypes.OrderBy(kv => kv.Key, System.StringComparer.Ordinal))
        {
            string sym = "ti_arr_" + key;
            string clrName = ArrayClrName(element) + "[]";
            // SZArray interface dispatch map: wired only when an array of this
            // element is actually used as a collection interface at runtime AND the wrapper
            // resolved. Carries the full CLR SZArray interface set (IEnumerable<T>/ICollection
            // <T>/IList<T>/IReadOnly{List,Collection}<T> + non-generic IEnumerable/ICollection/
            // IList), each forwarding through a fresh SZArrayEnumerable<T>.
            string itfsExpr = "nullptr", itfCount = "0";
            if (_c.ArrayEnumerableElementTypes.TryGetValue(key, out var aei)
                && EmitArrayEnumerableMap(sb, key, aei, arrItfPools, arrSlotStubs) is { } map)
            {
                itfsExpr = map.Sym;
                itfCount = map.Count.ToString();
                // The object-element map doubles as the runtime's shared reference-element
                // fallback dispatch table: EmitInitCalls registers it
                // (dn2cpp_array_set_ref_fallback_interfaces) so ANY reference-element
                // rank-1 array can service a collection-interface call it was reached
                // into through `object`, with no per-element map of its own.
                if (aei.Elem is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Object })
                    _arrayRefFallbackItfMap = (map.Sym, map.Count);
            }
            else if (_c.ArrayGenericItfRowElements.TryGetValue(key, out var rowItfs))
            {
                // Relation-only rows for an element the eager value loop skipped: nullptr
                // slot tables, so every dispatch reader skips them and the loud dispatch
                // abort stays; only the enumerating reflection readers report them.
                var rows = rowItfs.Select(itf =>
                    $"{{ {TypeInfoRef(itf, "SZArray generic-interface relation row")}, nullptr }}");
                sb.AppendLine($"static const Dn2CppInterfaceEntry arrgenitf_{key}[] = {{ {string.Join(", ", rows)} }};");
                itfsExpr = "arrgenitf_" + key;
                itfCount = rowItfs.Count.ToString();
            }
            // name, base, instanceSize, vtable, interfaces, interfaceCount, tostring,
            // gethashcode, equals, flags(ARRAY), then the 18 metadata table slots (0),
            // then the trailing elementType / arrayRank.
            sb.AppendLine($"const Dn2CppTypeInfo {sym} = {{ \"{clrName}\", nullptr, 0, nullptr, {itfsExpr}, {itfCount}, nullptr, nullptr, nullptr, (DN2CPP_TF_ARRAY | DN2CPP_TF_SEALED), nullptr, 0, nullptr, 0, nullptr, 0, nullptr, 0, nullptr, 0, nullptr, nullptr, 0, nullptr, nullptr, 0, nullptr, 0, {ElemTi(element)}, 1, nullptr, nullptr, nullptr, &ty_arr_{key} }};");
            sb.AppendLine($"const Dn2CppType ty_arr_{key} = {{ {{ &dn2cpp_type_type }}, &{sym} }};");
            _arrayTypeSyms[clrName] = "&" + sym;
        }
    }

    /// <summary>Emits an array's SZArray interface-dispatch table: one
    /// <c>Dn2CppInterfaceEntry</c> per interface the array implements (IEnumerable&lt;T&gt;/
    /// ICollection&lt;T&gt;/IList&lt;T&gt;/IReadOnly{List,Collection}&lt;T&gt; + non-generic
    /// IEnumerable/ICollection/IList). Each reached interface method gets a thunk that wraps
    /// the array (passed as <c>this</c>) into a fresh <c>SZArrayEnumerable&lt;T&gt;</c> and
    /// forwards to the wrapper's concrete impl (size-changing members throw there, matching a
    /// fixed-size array). An unreached method gets a trap stub that aborts naming the
    /// (array, interface, method) triple — NEVER a nullptr slot: these tables are served
    /// beyond their own element by the fallback/canonical-alias dispatch, so a hole the
    /// reach closure missed must fail loudly at the call, not jump through null.
    /// The entry's presence still makes <c>is</c>/<c>castclass</c> succeed.
    /// Returns the entry-array symbol + count, or null if the wrapper/ctor were not
    /// emitted (defensive — then the array keeps no map).
    /// </summary>
    private (string Sym, int Count)? EmitArrayEnumerableMap(StringBuilder sb, string key, Compilation.ArrayEnumerableInfo aei,
        Dictionary<string, string> arrItfPools, Dictionary<string, string> arrSlotStubs)
    {
        if (!_emit.Contains(aei.Szae) || !_c.Reachable.Contains(aei.Ctor) || aei.Dispatches.Count == 0)
            return null;
        string arrCpp = CppTypes.Of(aei.Ctor.Signature.ParameterTypes[0]); // T[] C++ type
        string arrClr = ArrayClrName(aei.Elem) + "[]";
        // A per-slot named trap: aborts via dn2cpp_itf_slot_missing_named with the
        // (array, interface, method) descriptor baked in. Same shape as
        // TypeMetadataEmitter.SlotMissStub, but pooled per EmitArrayTypeInfos run —
        // these tables all live in the one metadata section that calls this.
        string SlotMissStub(string desc, MethodInfo decl)
        {
            if (!arrSlotStubs.TryGetValue(desc, out var name))
            {
                name = $"arrslotmiss_{arrSlotStubs.Count}";
                sb.AppendLine(NamedSlotMissStubDef(name, "dn2cpp_itf_slot_missing_named", desc, decl));
                arrSlotStubs[desc] = name;
            }
            return name;
        }
        var entries = new List<string>();
        int i = 0;
        foreach (var disp in aei.Dispatches)
        {
            // Slot table sized to cover every declared method's vtable slot (so any dispatch
            // index is valid); reached methods get a wrap-and-forward thunk, the rest a trap.
            // The default (a row BuildArrayItfDispatches resolved no impl for) is the
            // argument-free anonymous trap — with no decl there is no return type to
            // decide whether argument 0 is the receiver or a struct-return buffer, so
            // read nothing (same reasoning as the class-itable named stub).
            int slotCount = disp.Methods.Max(m => m.Decl.VtableSlot) + 1;
            var slots = new string[slotCount];
            for (int s = 0; s < slotCount; s++)
                slots[s] = "(const void*)&dn2cpp_itf_slot_missing_anon";
            foreach (var (decl, impl) in disp.Methods)
            {
                if (decl.VtableSlot < 0)
                    continue;
                if (!_c.Reachable.Contains(impl))
                {
                    // Same receiver-readable / struct-return split as the class itables
                    // (TypeMetadataEmitter): a receiver-readable slot enters the shared
                    // receiver-naming trap through a per-signature thunk; a
                    // struct-returning slot's descriptor is baked into a named stub.
                    slots[decl.VtableSlot] = ReceiverIsFirstArg(decl.Signature.ReturnType)
                        ? $"(const void*)&{SlotTrapThunk(decl, vcall: false) ?? "dn2cpp_itf_slot_missing"}"
                        : $"(const void*)&{SlotMissStub($"{arrClr}::{disp.Itf.FullName}.{decl.Name}", decl)}";
                    continue;
                }
                string retCpp = CppTypes.Of(decl.Signature.ReturnType);
                var ps = decl.Signature.ParameterTypes;
                // The forwarding target's DEFINITION parameter types — i.e. the C++
                // prototype MethodCompiler.Signature spins out of impl.Emittable. When
                // impl is a grouped shared-generics member, Emittable is the __CnRef
                // canonical body, whose element parameters render `Dn2CppObject*` — NOT
                // the concrete element that impl.Signature carries. Cast to those.
                var implPs = impl.Emittable.Signature.ParameterTypes;
                var pdecls = new List<string> { $"{disp.Itf.CppStructName}* self" };
                var fargs = new List<string> { "w" };
                for (int p = 0; p < ps.Length; p++)
                {
                    string pCpp = CppTypes.Of(ps[p]);
                    pdecls.Add($"{pCpp} p{p}");
                    // The thunk parameter is typed at the concrete interface element while
                    // the canonical impl body's is `Dn2CppObject*`, so the forward casts
                    // (dropping const, widening the reference); a matching type casts to
                    // itself. A headerless element (the NFI trio, Assembly/Module) must be
                    // WRAPPED rather than punned: the ref-array SLOTS hold the interned
                    // wrapper, and IndexOf/Contains inside the canonical body compare the
                    // argument against those slots.
                    string implCpp = p < implPs.Length ? CppTypes.Of(implPs[p]) : pCpp;
                    fargs.Add(pCpp == implCpp ? $"p{p}"
                        : MethodCompiler.IsHeaderlessWrapCpp(pCpp) && implCpp == "Dn2CppObject*"
                            ? MethodCompiler.HeaderlessWrapExpr($"p{p}", pCpp, ps[p])
                            : $"({implCpp})p{p}");
                }
                string thunk = $"arrthunk_{key}_{i}_{decl.VtableSlot}";
                sb.AppendLine($"static {retCpp} {thunk}({string.Join(", ", pdecls)}) {{");
                sb.AppendLine($"    {aei.Szae.CppStructName}* w = ({aei.Szae.CppStructName}*)dn2cpp_alloc(sizeof({aei.Szae.CppStructName}));");
                sb.AppendLine($"    ((Dn2CppObject*)w)->type = {TypeInfoRef(aei.Szae, "SZArray-enumerable wrapper allocation")};");
                sb.AppendLine($"    {aei.Ctor.Emittable.CppName}(w, ({arrCpp})self);");
                string call = $"{impl.Emittable.CppName}({string.Join(", ", fargs)})";
                // The symmetric return conversion: get_Item/Current on a
                // headerless-element array come back as the wrapped slot
                // (Dn2CppObject*) from the canonical body — unwrap to the declared
                // element type.
                sb.AppendLine(retCpp == "void" ? $"    {call};"
                    : MethodCompiler.IsHeaderlessWrapCpp(retCpp)
                        ? $"    return {MethodCompiler.HeaderlessUnwrapExpr(call, retCpp)};"
                        : $"    return ({retCpp}){call};");
                sb.AppendLine("}");
                slots[decl.VtableSlot] = $"(const void*)&{thunk}";
            }
            // Intern byte-identical dispatch tables (the pool spans this whole
            // single-TU section; a table carrying reached thunks embeds this
            // key's thunk symbols and stays unique by construction). Pool size
            // doubles as the name counter — entries are never removed, so it is
            // monotonic and deterministic.
            string slotsInit = string.Join(", ", slots);
            if (!arrItfPools.TryGetValue(slotsInit, out var slotSym))
            {
                slotSym = $"arritfpool_{arrItfPools.Count}";
                sb.AppendLine($"static const void* {slotSym}[] = {{ {slotsInit} }};");
                arrItfPools[slotsInit] = slotSym;
            }
            entries.Add($"{{ {TypeInfoRef(disp.Itf, "SZArray interface-dispatch map")}, {slotSym} }}");
            // Canonical alias row (shared generics): a shared body enumerating
            // an enum-element array through the canonical interface handle must
            // resolve on the array's own dispatch map too.
            if (_c.SharedGenericsEnabled && _c.CanonicalInterfaceOf(disp.Itf) is { } citf)
                entries.Add($"{{ {TypeInfoRef(citf, "SZArray interface-dispatch map (canonical alias row)")}, {slotSym} }}");
            i++;
        }
        sb.AppendLine($"static const Dn2CppInterfaceEntry arr_itfs_{key}[] = {{ {string.Join(", ", entries)} }};");
        return ("arr_itfs_" + key, entries.Count);
    }

    /// <summary>Emits the lazy, cached <c>Console.Error</c> managed-writer singleton
    /// accessor — the "generated lazy static" the get_Error intrinsic pushes. It
    /// allocates one <c>Dn2CppConsoleWriter</c>, stamps its type header (so a callvirt
    /// dispatches through the real vtable), runs its ctor once, and caches it in a
    /// function-local <c>static</c> (a GC root, so the singleton lives for the process),
    /// giving <c>Console.Error</c> reference identity. The forward decl goes in the header
    /// (external linkage) so a body in any TU can call it; the definition into the primary
    /// TU. No-op unless a Console.Error use reached the writer.</summary>
    private void EmitConsoleErrorWriterAccessor(CppOutput o)
    {
        if (_c.ConsoleErrorWriterInfo is not { } cew)
            return;
        var (cls, ctor) = cew;
        // Reached during body compilation, so it is emitted + its ctor reachable; guard
        // defensively so a tree-shaken edge can never dangle the accessor's references.
        if (!_emit.Contains(cls) || !_c.Reachable.Contains(ctor))
            return;
        string st = cls.CppStructName;
        o.Header.AppendLine($"{st}* dn2cpp_console_error_writer();");
        o.Data.AppendLine("// ---- managed Console.Error writer singleton ----");
        o.Data.AppendLine($"{st}* dn2cpp_console_error_writer()");
        o.Data.AppendLine("{");
        o.Data.AppendLine($"    static {st}* w = nullptr;");
        o.Data.AppendLine("    if (w == nullptr)");
        o.Data.AppendLine("    {");
        o.Data.AppendLine($"        w = ({st}*)dn2cpp_alloc(sizeof({st}));");
        o.Data.AppendLine($"        ((Dn2CppObject*)w)->type = {TypeInfoRef(cls, "Console.Error managed-writer singleton")};");
        o.Data.AppendLine($"        {ctor.Emittable.CppName}(w);");
        o.Data.AppendLine("    }");
        o.Data.AppendLine("    return w;");
        o.Data.AppendLine("}");
        o.Data.AppendLine();
    }

    /// <summary>The CLR FullName of an SZArray element type — what
    /// <c>typeof(T[]).FullName</c> prefixes before "[]" (e.g. "System.Int32", a user
    /// type's reflection name — '+'-qualified when nested, see
    /// <see cref="Compilation.ReflectionTypeName"/> — or a nested array's name
    /// recursively).</summary>
    private static string ArrayClrName(TypeDesc t) => t.Kind switch
    {
        TypeKind.Primitive => PrimitiveClrName(t.Primitive),
        TypeKind.Class => Compilation.ReflectionTypeName(t.Class!),
        TypeKind.External => t.ExternalName!,
        TypeKind.SZArray => ArrayClrName(t.Element!) + "[]",
        // "Elem[,]" — the CLR MD suffix, matching dn2cpp_array_ti's fabricated names.
        TypeKind.MDArray => ArrayClrName(t.Element!) + "[" + new string(',', t.Rank - 1) + "]",
        _ => t.ToString(),
    };

    private static string PrimitiveClrName(PrimitiveTypeCode p) => p switch
    {
        PrimitiveTypeCode.Boolean => "System.Boolean",
        PrimitiveTypeCode.Char => "System.Char",
        PrimitiveTypeCode.SByte => "System.SByte",
        PrimitiveTypeCode.Byte => "System.Byte",
        PrimitiveTypeCode.Int16 => "System.Int16",
        PrimitiveTypeCode.UInt16 => "System.UInt16",
        PrimitiveTypeCode.Int32 => "System.Int32",
        PrimitiveTypeCode.UInt32 => "System.UInt32",
        PrimitiveTypeCode.Int64 => "System.Int64",
        PrimitiveTypeCode.UInt64 => "System.UInt64",
        PrimitiveTypeCode.Single => "System.Single",
        PrimitiveTypeCode.Double => "System.Double",
        PrimitiveTypeCode.IntPtr => "System.IntPtr",
        PrimitiveTypeCode.UIntPtr => "System.UIntPtr",
        PrimitiveTypeCode.String => "System.String",
        PrimitiveTypeCode.Object => "System.Object",
        _ => "System.Object",
    };

    /// <summary>Whether the class is declared inside another type (metadata
    /// DeclaringType present) — the DN2CPP_TF_NESTED bit backing Type.IsNested.
    /// Synthetic classes with no metadata handle are top-level.</summary>
    private static bool IsNestedType(ClassInfo cls)
    {
        if (cls.Handle.IsNil)
            return false;
        return !cls.Module.Reader.GetTypeDefinition(cls.Handle).GetDeclaringType().IsNil;
    }

    /// <summary>Whether an enum carries [FlagsAttribute] — the DN2CPP_TF_FLAGS bit
    /// backing dn2cpp_enum_format's "G" specifier (a flags enum decomposes into flag
    /// names, a plain enum reports a single member name / decimal). [Flags] is a
    /// custom attribute, invisible to the CLR TypeAttributes bits, so it must be read
    /// straight off the metadata blob and stamped onto the type-info here.</summary>
    private static bool EnumHasFlagsAttribute(ClassInfo en)
    {
        if (en.Handle.IsNil)
            return false;
        var reader = en.Module.Reader;
        foreach (var cah in reader.GetTypeDefinition(en.Handle).GetCustomAttributes())
            if (Compilation.AttributeTypeName(reader, reader.GetCustomAttribute(cah)) == "System.FlagsAttribute")
                return true;
        return false;
    }

    /// <summary>The member name a type's [DefaultMember] attribute declares
    /// ("Item" for a type with an indexer — the C# compiler emits the attribute),
    /// or null when the type carries none. Read straight off the metadata blob:
    /// DefaultMemberAttribute is a framework attribute, so it is outside the
    /// reflected attribute tables (the IL2CPP-managed-stripping bound) and its
    /// name must ride the type-info for Type.GetDefaultMembers. Only ASCII
    /// identifier names are stamped (a non-ASCII name cannot be rendered as a
    /// plain C literal here and CLR member names in practice are ASCII).</summary>
    private static string? DefaultMemberName(ClassInfo cls)
    {
        if (cls.Handle.IsNil)
            return null;
        try
        {
            var reader = cls.Module.Reader;
            foreach (var cah in reader.GetTypeDefinition(cls.Handle).GetCustomAttributes())
            {
                var ca = reader.GetCustomAttribute(cah);
                string? attrType = ca.Constructor.Kind switch
                {
                    HandleKind.MemberReference
                        when reader.GetMemberReference((MemberReferenceHandle)ca.Constructor).Parent
                            is { Kind: HandleKind.TypeReference } p =>
                        reader.GetString(reader.GetTypeReference((TypeReferenceHandle)p).Namespace)
                            + "." + reader.GetString(reader.GetTypeReference((TypeReferenceHandle)p).Name),
                    HandleKind.MethodDefinition =>
                        reader.GetString(reader.GetTypeDefinition(
                            reader.GetMethodDefinition((MethodDefinitionHandle)ca.Constructor).GetDeclaringType()).Namespace)
                            + "." + reader.GetString(reader.GetTypeDefinition(
                                reader.GetMethodDefinition((MethodDefinitionHandle)ca.Constructor).GetDeclaringType()).Name),
                    _ => null,
                };
                if (attrType != "System.Reflection.DefaultMemberAttribute")
                    continue;
                // Blob: prolog (0x0001) + one SerString positional argument.
                var blob = reader.GetBlobReader(ca.Value);
                if (blob.ReadUInt16() != 1)
                    continue;
                string? name = blob.ReadSerializedString();
                if (name is not null && name.All(c => c is >= ' ' and <= '~' && c != '"' && c != '\\'))
                    return name;
            }
        }
        catch (Exception e) when (!Compilation.IsMustEscape(e))
        {
            // A type without decodable attribute metadata has no default member.
        }
        return null;
    }

    /// <summary>The EventSource provider identity a class declares, or null when the
    /// class is not an EventSource provider: the display name and, when the type declares
    /// one explicitly, the canonical Guid. Stamped on the type-info
    /// (<c>eventSourceName</c> / <c>eventSourceGuid</c>) and read back by
    /// <c>dn2cpp_eventsource_name</c> / <c>_guid</c>.
    /// <para>The rule is .NET's own <c>EventSource.GetName(Type)</c> /
    /// <c>GetGuid(Type)</c>, measured rather than assumed:
    /// <c>[EventSource(Name=…)]</c> wins, otherwise the type's SIMPLE name (a nested
    /// provider answers "InnerLog", not "Outer+InnerLog", and a namespaced one drops the
    /// namespace); <c>[EventSource(Guid=…)]</c> wins for the Guid, otherwise the runtime
    /// derives it from the name. The attribute is read with inherit:false, exactly as
    /// <c>GetCustomAttributeHelper</c> does, so nothing walks the base chain here.</para>
    /// <para>It has to ride the type-info rather than the reflected attribute tables
    /// because EventSourceAttribute is a FRAMEWORK attribute — the same reason
    /// <see cref="DefaultMemberName"/> does — and the same C-literal restriction applies:
    /// a name outside plain printable ASCII is left unstamped, and get_Name then throws
    /// PlatformNotSupportedException.</para></summary>
    private static (string Name, string? Guid)? EventSourceIdentity(ClassInfo cls)
    {
        if (cls.Handle.IsNil || !Compilation.InheritsFromEventSource(cls))
            return null;
        string? attrName = null;
        string? attrGuid = null;
        try
        {
            var reader = cls.Module.Reader;
            foreach (var cah in reader.GetTypeDefinition(cls.Handle).GetCustomAttributes())
            {
                var ca = reader.GetCustomAttribute(cah);
                if (Compilation.AttributeTypeName(reader, ca)
                        != "System.Diagnostics.Tracing.EventSourceAttribute")
                    continue;
                foreach (var na in ca.DecodeValue(cls.Module.Owner.AttrProvider).NamedArguments)
                {
                    if (na is { Name: "Name", Value: string n })
                        attrName = n;
                    else if (na is { Name: "Guid", Value: string g })
                        attrGuid = g;
                }
                break;
            }
        }
        catch (Exception e) when (!Compilation.IsMustEscape(e))
        {
            // An undecodable blob degrades to the type-name default — the same answer
            // .NET gives a provider that carries no attribute at all.
        }
        string name = attrName ?? cls.Name;
        if (!IsCLiteralSafe(name))
            return null;
        // The runtime parses the canonical 36-character form and nothing else; anything
        // else is left to the name-derived default rather than mis-parsed. Validated by
        // hand rather than through Guid.TryParseExact because this file is transpiler
        // input as well as transpiler source (the self-host rule in AGENTS.md), and the
        // shape is four hyphens and thirty-two hex digits.
        if (attrGuid is not null && !IsCanonicalGuidText(attrGuid))
            attrGuid = null;
        return (name, attrGuid);
    }

    /// <summary>Whether the text is exactly "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx" with
    /// lower- or upper-case hex digits — the one spelling
    /// <c>dn2cpp_eventsource.cpp</c>'s reader accepts.</summary>
    private static bool IsCanonicalGuidText(string s)
    {
        if (s.Length != 36)
            return false;
        for (int i = 0; i < 36; i++)
        {
            char c = s[i];
            bool hyphen = i is 8 or 13 or 18 or 23;
            if (hyphen != (c == '-'))
                return false;
            if (!hyphen && !(c is >= '0' and <= '9' or >= 'a' and <= 'f' or >= 'A' and <= 'F'))
                return false;
        }
        return true;
    }

    /// <summary>Whether a string can be stamped into a C string literal verbatim
    /// (plain printable ASCII, no quote, no backslash) — the restriction
    /// <see cref="DefaultMemberName"/> applies to the member name it stamps.</summary>
    private static bool IsCLiteralSafe(string s) =>
        s.Length > 0 && s.All(c => c is >= ' ' and <= '~' && c != '"' && c != '\\');

    /// <summary>The raw ECMA TypeAttributes word + metadata token of a class,
    /// backing Type.Attributes (and the IsPublic/IsVisible family) and
    /// MemberInfo.MetadataToken. (0, 0) for a synthetic class with no metadata
    /// handle — the runtime then synthesizes a best-effort word from the flags.</summary>
    private static (int Attrs, int Token) TypeIlMeta(ClassInfo cls)
    {
        if (cls.Handle.IsNil)
            return (0, 0);
        try
        {
            var td = cls.Module.Reader.GetTypeDefinition(cls.Handle);
            return ((int)td.Attributes,
                System.Reflection.Metadata.Ecma335.MetadataTokens.GetToken(cls.Handle));
        }
        catch (Exception e) when (!Compilation.IsMustEscape(e))
        {
            return (0, 0);
        }
    }

    /// <summary>An enum's (value, name) members sorted by the constants' *unsigned*
    /// underlying magnitude, matching .NET's Enum.GetNames/GetValues ordering —
    /// the same key the generic Enum.*&lt;T&gt; inline lowering uses. The key
    /// reinterprets each value at the underlying width so e.g. a short -5 sorts as
    /// 65531. A 64-bit underlying reads its members at full width
    /// (<c>EnumMembers64</c>): the int32 model width is what the payload carries for
    /// every narrower enum, but a member past int32 range truncates there, and the
    /// table it lands in is what <c>dn2cpp_enum_format</c> / GetName / IsDefined
    /// answer from.</summary>
    private static List<(long value, string name)> SortedEnumMembers(ClassInfo en)
    {
        return MethodCompiler.EnumMembersModel(en).OrderBy(MethodCompiler.EnumSortKey(en)).ToList();
    }

    /// <summary>A type's public nested types that are emitted and non-generic —
    /// the default <c>Type.GetNestedTypes()</c> (BindingFlags.Public) set. Non-public /
    /// generic / unreached nested types are carved out (their type-info isn't emitted, or
    /// the open generic nested def isn't a type here). A generic enclosing type is also
    /// skipped: its template's nested handles don't name the closed nested instances.</summary>
    private List<ClassInfo> PublicNestedTypes(ClassInfo cls, HashSet<ClassInfo> emittedEnums)
    {
        var result = new List<ClassInfo>();
        if (cls.GenericArity > 0)
            return result;
        try
        {
            var reader = cls.Module.Reader;
            var td = reader.GetTypeDefinition(cls.Handle);
            foreach (var nh in td.GetNestedTypes())
            {
                var ntd = reader.GetTypeDefinition(nh);
                if ((ntd.Attributes & System.Reflection.TypeAttributes.VisibilityMask)
                    != System.Reflection.TypeAttributes.NestedPublic)
                    continue;
                // Include a nested type only if its type-info is actually emitted.
                // A nested enum's ti_ is emitted from ReferencedTypes (emittedEnums), not
                // the class loop — and an enum can land in _emit without being referenced
                // as a type token, in which case no ti_ is emitted and listing it here
                // would dangle. So gate enums on emittedEnums and classes on _emit.
                if (cls.Module.ClassMap.TryGetValue(nh, out var nested)
                    && nested.GenericArity == 0
                    && (nested.IsEnum ? emittedEnums.Contains(nested) : _emit.Contains(nested)))
                    result.Add(nested);
            }
        }
        catch (Exception e) when (!Compilation.IsMustEscape(e))
        { /* unreadable metadata -> no nested types */ }
        return result;
    }

    /// <summary>For a closed generic instantiation, the CLR open-definition
    /// FullName (with the backtick arity marker, e.g. "System.Collections.Generic.List`1")
    /// and the type-argument count. Null for a non-generic type or a nested generic
    /// (a nested type's FullName isn't the simple namespace-qualified name — carved out
    /// like Type.GetType's nested-name limitation).</summary>
    private (string DefName, int Arity)? GenericDefInfo(ClassInfo cls)
    {
        if (cls.GenericArity == 0 || cls.Context.TypeArgs.Length == 0)
            return null;
        try
        {
            var reader = cls.Module.Reader;
            var td = reader.GetTypeDefinition(cls.Handle);
            if (!td.GetDeclaringType().IsNil)
                return null;
            string name = reader.GetString(td.Name);          // "List`1" (keeps the backtick)
            string ns = reader.GetString(td.Namespace);
            string full = string.IsNullOrEmpty(ns) ? name : ns + "." + name;
            return (full, cls.Context.TypeArgs.Length);
        }
        catch (Exception e) when (!Compilation.IsMustEscape(e))
        {
            return null;
        }
    }

    /// <summary>Whether <paramref name="cls"/>'s generic definition has a single
    /// covariant (<c>out</c>) type parameter — the exact shape DN2CPP_TF_COVARIANT has
    /// always meant. The general answer now lives in the definition's variance MASK
    /// (<see cref="Compilation.GenericVarianceMask"/>, DN2CPP_TF_VARIANT); this bit stays
    /// beside it as the compatibility contract, so a type-info emitted before the mask
    /// existed — a hot-update base image — still matches covariantly.</summary>
    private bool IsCovariantGenericDef(ClassInfo cls) =>
        cls.Context.TypeArgs.Length == 1
        && _c.GenericVarianceMask(cls) == Compilation.VarianceCovariant;

    /// <summary>A field's FieldType as a Dn2CppTypeInfo* expression. References
    /// a per-type ti_ only when that type is actually emitted; an un-emitted/opaque BCL
    /// field type falls back to System.Object so the field table always links (its
    /// FieldType then best-effort reports object). Primitives/string/decimal/exception
    /// route to their stable runtime handles. An SZArray member type names the precise
    /// per-element <c>ti_arr_&lt;elem&gt;</c> handle — the same identity <c>typeof(T[])</c>
    /// and a <c>newarr T</c>'s GetType carry — so a reflected FieldType/PropertyType/
    /// ParameterType reports "Elem[]" with a real element, like .NET (a JSON contract
    /// resolver classifying a member by its reflected type is the load-bearing consumer:
    /// an object-degraded array member deserializes as an untyped JArray and the setter
    /// thunk's blind cast stores it — silent corruption). Gated on
    /// <see cref="ArrayTypeInfoDeclared"/> so only a forward-declared handle is ever named;
    /// <c>TypeMetadataEmitter.NoteReflectedMemberArrayElements</c> is what puts reflected
    /// member elements in that set — a mirror of this emitter's own filters, so a "no" here
    /// means the mirror came apart, which is why it is counted and fails the transpile
    /// (<see cref="AssertArrayTypeInfoDegradesWithinCap"/>) rather than degrading in
    /// silence. An MDArray type whose identity closure was noted names the same static
    /// <c>ti_md_</c> handle the runtime interner registers for that element/rank shape.</summary>
    private string FieldTypeInfoExpr(TypeDesc t, HashSet<ClassInfo> emittedEnums)
    {
        if (t is { Kind: TypeKind.SZArray, Element: { Kind: TypeKind.Primitive or TypeKind.Class or TypeKind.External or TypeKind.SZArray or TypeKind.MDArray } el }
            && ArrayTypeInfoDeclared(el, "reflected member type (array)"))
            return MethodCompiler.PreciseArrayTypeInfoExprOf(el);
        if (t.Kind == TypeKind.MDArray)
        {
            string sym = "ti_md_" + Compilation.ArrayElemMangle(t);
            if (TypeInfoSymbolDefined(sym))
                return "&" + sym;
        }
        // A cross-assembly type can arrive in ResolveTypeToken's degraded External spelling
        // (an array element's first noter wins) while typeof answers the loaded class's own
        // handle — promote so both mouths answer one identity, else an IDisposable[]
        // elementType reads object and GetElementType()/Array.Copy cannot tell it from
        // object[].
        if (t is { Kind: TypeKind.External, ExternalName: { } xn } && _c.FindClassByFullName(xn) is { } xc)
            t = TypeDesc.MakeClass(xc);
        if (t.Kind == TypeKind.Class && t.Class is { } fc)
        {
            // A type whose type-info the C++ runtime defines answers with THAT handle,
            // whatever the emit set holds — the same answer typeof gives, asked through the
            // one identity table rather than a second copy of it. It also subsumes the
            // "not emitted ⟹ degrade to System.Object" fallback for these types, which was
            // itself a wrong answer.
            if (CoreIntrinsics.RuntimeTypeInfoSymbol(fc) is { } rt) return rt;
            if (_referencedIntrinsicTis.Contains(fc))
                return TypeInfoRef(fc, "reflected signature custom modifier");
            if (fc.IsEnum) return emittedEnums.Contains(fc) ? TypeInfoRef(fc, "reflected member type (enum)") : "&dn2cpp_object_type";
            return _emit.Contains(fc) ? TypeInfoRef(fc, "reflected member type") : "&dn2cpp_object_type";
        }
        return MethodCompiler.TypeInfoExprOf(t) ?? "&dn2cpp_object_type";
    }

    /// <summary>A method's return/parameter type as a Dn2CppTypeInfo* expression.
    /// Like <see cref="FieldTypeInfoExpr"/> but maps <c>void</c> to the
    /// System.Void handle so a void method's ReturnType.Name reports "Void".</summary>
    private string MemberTypeInfoExpr(TypeDesc t, HashSet<ClassInfo> emittedEnums) =>
        t.IsVoid ? "&dn2cpp_void_type" : FieldTypeInfoExpr(t, emittedEnums);

    // Runtime reflection handles intentionally retain only compact type-info
    // pointers. Render the signature spelling here, while byref and generic
    // substitution detail is still present in TypeDesc, and carry it on the row.
    private string ReflectionSignatureType(TypeDesc t, bool qualifyPrimitive = false,
        IReadOnlyList<string>? methodGenericNames = null)
    {
        if (t.IsVoid)
            return "Void";
        if (t.Kind == TypeKind.ByRef)
            return ReflectionSignatureType(t.Element!, qualifyPrimitive, methodGenericNames) + "&";
        if (t.Kind == TypeKind.Pointer)
            return ReflectionSignatureType(t.Element!, qualifyPrimitive, methodGenericNames) + "*";
        if (t.Kind == TypeKind.SZArray)
            return ReflectionSignatureType(t.Element!, qualifyPrimitive, methodGenericNames) + "[]";
        if (t.Kind == TypeKind.MDArray)
            return ReflectionSignatureType(t.Element!, qualifyPrimitive, methodGenericNames)
                + "[" + new string(',', t.Rank - 1) + "]";
        if (t.Kind == TypeKind.Primitive)
        {
            string simple = t.Primitive.ToString();
            if (t.Primitive is PrimitiveTypeCode.String or PrimitiveTypeCode.Object)
                return "System." + simple;
            return qualifyPrimitive ? "System." + simple : simple;
        }
        if (t.Kind == TypeKind.Class && t.Class is { } cls)
        {
            if (cls.Context.TypeArgs.Length == 0)
                return Compilation.ReflectionTypeName(cls);
            string def = MethodCompiler.OpenDefBacktickName(cls.Module, cls.Handle)
                ?? Compilation.ReflectionTypeName(cls);
            return def + "[" + string.Join(",", cls.Context.TypeArgs.Select(
                a => ReflectionSignatureType(a, true, methodGenericNames))) + "]";
        }
        if (t.Kind == TypeKind.GenericVar && t.GenVarIsMethod
            && methodGenericNames is not null && (uint)t.GenVarIndex < (uint)methodGenericNames.Count)
            return methodGenericNames[t.GenVarIndex];
        if (t.Kind is TypeKind.External or TypeKind.ExternalGeneric)
            return t.ExternalName!;
        return t.ToString();
    }

    /// <summary>The exact CLR display of a closed nested type. Nested generic
    /// instantiations deliberately have no synthetic generic-definition handle, so their
    /// type-info cannot compose this spelling from <c>genericDef</c>/<c>genericArgs</c>.
    /// Task-family awaiters still need a precise per-instantiation ToString identity.</summary>
    private string ClosedNestedTypeDisplay(ClassInfo cls)
    {
        var reader = cls.Module.Reader;
        var handle = cls.Handle;
        var parts = new List<string>();
        while (!handle.IsNil)
        {
            var td = reader.GetTypeDefinition(handle);
            string name = reader.GetString(td.Name);
            string ns = reader.GetString(td.Namespace);
            parts.Insert(0, string.IsNullOrEmpty(ns) ? name : ns + "." + name);
            handle = td.GetDeclaringType();
        }
        return string.Join("+", parts) + "["
            + string.Join(",", cls.Context.TypeArgs.Select(a => ReflectionSignatureType(a, true))) + "]";
    }

    private string ReflectionMethodDisplay(MethodInfo m)
    {
        string ret = ReflectionSignatureType(m.Signature.ReturnType);
        string name = m.Name;
        if (m.Context.MethodArgs.Length > 0)
            name += "[" + string.Join(",", m.Context.MethodArgs.Select(
                a => ReflectionSignatureType(a))) + "]";
        string ps = string.Join(", ", m.Signature.ParameterTypes.Select(
            p => p.Kind == TypeKind.ByRef
                ? ReflectionSignatureType(p.Element!) + " ByRef"
                : ReflectionSignatureType(p)));
        return ret + " " + name + "(" + ps + ")";
    }

    private (string Method, string Return, string[] Parameters)?
        ReflectionGenericMethodDefinitionDisplays(MethodInfo m,
            IReadOnlyList<string?> parameterNames)
    {
        if (m.Context.MethodArgs.Length == 0 || m.Handle.IsNil)
            return null;
        try
        {
            var reader = m.Module.Reader;
            var md = reader.GetMethodDefinition(m.Handle);
            var names = new string[md.GetGenericParameters().Count];
            foreach (var gph in md.GetGenericParameters())
            {
                var gp = reader.GetGenericParameter(gph);
                names[gp.Index] = reader.GetString(gp.Name);
            }
            var open = md.DecodeSignature(_c.SigProvider,
                new GenericContext(m.DeclaringClass.Context.TypeArgs, Array.Empty<TypeDesc>()));
            string ret = ReflectionSignatureType(open.ReturnType, false, names);
            string ps = string.Join(", ", open.ParameterTypes.Select(p =>
                p.Kind == TypeKind.ByRef
                    ? ReflectionSignatureType(p.Element!, false, names) + " ByRef"
                    : ReflectionSignatureType(p, false, names)));
            string[] parameters = new string[open.ParameterTypes.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                parameters[i] = ReflectionSignatureType(
                    open.ParameterTypes[i], false, names);
                if (parameterNames[i] is { Length: > 0 } parameterName)
                    parameters[i] += " " + parameterName;
            }
            return (ret + " " + m.Name + "[" + string.Join(",", names) + "](" + ps + ")",
                ret, parameters);
        }
        catch (Exception e) when (!Compilation.IsMustEscape(e))
        {
            return null;
        }
    }

    private string ReflectionPropertyDisplay(string name, TypeDesc type,
        IReadOnlyList<TypeDesc> indexParameters)
    {
        string result = ReflectionSignatureType(type) + " " + name;
        if (indexParameters.Count > 0)
            result += " [" + string.Join(", ", indexParameters.Select(
                p => ReflectionSignatureType(p))) + "]";
        return result;
    }

    // ---- custom attributes ----

    private static readonly System.Globalization.CultureInfo AttrCI = System.Globalization.CultureInfo.InvariantCulture;

    /// <summary>Emits the Dn2CppAttrInfo[] table (plus its create-function forward decls)
    /// for a reflected element's custom attributes, returning (tableExpr, count) —
    /// or ("nullptr", 0) when there is no reflectable attribute. `key` is a unique C++ name
    /// fragment. Only attributes whose ctor body is emitted and whose argument shape is
    /// renderable are included; others are silently dropped (the IL2CPP-stripping bound).</summary>
    private (string Expr, int Count) BuildAttrTable(StringBuilder sb, string key, Module module, CustomAttributeHandleCollection handles)
    {
        var rows = new List<string>();
        foreach (var da in _c.DecodeCustomAttributes(module, handles))
            if (RenderAttrCreate(sb, da) is { } createName)
                rows.Add($"{{ {TypeInfoRef(da.AttrClass, "custom-attribute table row")}, &{createName}, \"{CLiteral(RenderAttrDisplay(da))}\" }}");
        if (rows.Count == 0)
            return ("nullptr", 0);
        string tab = $"attrtab_{key}";
        sb.AppendLine($"static const Dn2CppAttrInfo {tab}[] = {{ {string.Join(", ", rows)} }};");
        return (tab, rows.Count);
    }

    private string RenderAttrDisplay(Compilation.DecodedAttribute da)
    {
        var args = new List<string>();
        for (int i = 0; i < da.Fixed.Length; i++)
            args.Add(RenderAttrDisplayValue(da.Fixed[i].Type, da.Fixed[i].Value, true));
        foreach (var named in da.Named)
            args.Add((named.Name ?? "") + " = "
                + RenderAttrDisplayValue(named.Type, named.Value, false));
        string type = ReflectionSignatureType(TypeDesc.MakeClass(da.AttrClass), true);
        return "[" + type + "(" + string.Join(", ", args) + ")]";
    }

    private string RenderAttrDisplayValue(TypeDesc type, object? value, bool typed)
    {
        if (type.Kind == TypeKind.SZArray)
        {
            if (value is null)
                return typed ? "(" + ReflectionAttributeType(type) + ")null" : "null";
            if (value is not System.Collections.Immutable.ImmutableArray<CustomAttributeTypedArgument<TypeDesc>> items)
                return "null";
            string elem = ReflectionAttributeType(type.Element!);
            string values = string.Join(", ", items.Select(
                x => RenderAttrDisplayValue(type.Element!, x.Value, false)));
            return "new " + elem + "[" + items.Length + "] { " + values + " }";
        }
        if (IsTypeTarget(type))
        {
            if (value is null)
                return typed ? "(Type)null" : "null";
            return value is TypeDesc td
                ? "typeof(" + ReflectionSignatureType(td, true) + ")"
                : "null";
        }
        string rendered;
        if (value is null)
            rendered = "null";
        else if (type.IsString)
            rendered = "\"" + value + "\"";
        else if (type.Kind == TypeKind.Class && type.Class!.IsEnum)
            rendered = System.Convert.ToInt64(value, AttrCI).ToString(AttrCI);
        else if (type.Kind == TypeKind.Primitive && type.Primitive == PrimitiveTypeCode.Boolean)
            rendered = System.Convert.ToBoolean(value, AttrCI) ? "True" : "False";
        else if (type.Kind == TypeKind.Primitive && type.Primitive == PrimitiveTypeCode.Char)
            rendered = "'" + value.ToString()!.Replace("'", "\\'") + "'";
        else
            rendered = System.Convert.ToString(value, AttrCI) ?? "null";
        // CustomAttributeData keeps the explicit type cast for scalar numeric/
        // enum arguments, but a non-null string is already self-describing.
        // A null string does keep its cast to disambiguate the blob type.
        if (!typed || (type.IsString && value is not null))
            return rendered;
        return "(" + ReflectionAttributeType(type) + ")" + rendered;
    }

    private string ReflectionAttributeType(TypeDesc type) => type.Kind switch
    {
        TypeKind.Primitive => type.Primitive.ToString(),
        TypeKind.SZArray => ReflectionAttributeType(type.Element!) + "[]",
        _ => ReflectionSignatureType(type),
    };

    /// <summary>Emits a create-function definition (into <paramref name="sb"/>, right
    /// before the attribute table that names it — the same TU, so the file-local
    /// function always resolves) for one decoded attribute, returning the
    /// function name — or null when the attribute can't be materialized (ctor body not
    /// emitted, or an unsupported argument shape). The function allocates the instance,
    /// runs the ctor with the constant positional args, then sets the named fields/
    /// properties, returning a fresh instance like .NET. The body may reference string
    /// literals (a string-valued argument) — safe anywhere in any TU, since every
    /// str_N is declared extern in the header.</summary>
    private string? RenderAttrCreate(StringBuilder sb, Compilation.DecodedAttribute da)
    {
        var cls = da.AttrClass;
        var ctor = da.Ctor;
        if (!_c.Reachable.Contains(ctor) || _backend.ShouldSkipMethodBody(cls, ctor))
            return null;
        var ps = ctor.Signature.ParameterTypes;
        if (ps.Length != da.Fixed.Length)
            return null;
        // Array-valued arguments (e.g. a Type[] positional arg) materialize through
        // pre-statements building the array before the ctor call / setters run.
        var pre = new List<string>();
        int arrSeq = 0;
        var argExprs = new List<string>();
        for (int i = 0; i < ps.Length; i++)
        {
            if (RenderAttrValue(ps[i], da.Fixed[i].Value, pre, ref arrSeq) is not { } e)
                return null;
            // A pointer-typed parameter (Type/string/array) gets an explicit cast to
            // the ctor's exact C++ parameter type (e.g. Dn2CppType* -> t_System_Type*
            // when the real CoreLib's System.Type is a loaded class), mirroring the
            // named-field assignment below.
            string cppT = CppTypes.Of(ps[i]);
            argExprs.Add(cppT.EndsWith("*") ? $"({cppT})({e})" : e);
        }
        var setLines = new List<string>();
        foreach (var na in da.Named)
        {
            if (RenderNamedArg(cls, na, pre, ref arrSeq) is not { } line)
                return null;
            setLines.Add(line);
        }
        string fn = $"attrcreate_{_attrSeq++}";
        var body = sb;
        body.AppendLine($"static Dn2CppObject* {fn}()");
        body.AppendLine("{");
        body.AppendLine($"    {cls.CppStructName}* o = ({cls.CppStructName}*)dn2cpp_alloc(sizeof({cls.CppStructName}));");
        body.AppendLine($"    ((Dn2CppObject*)o)->type = {TypeInfoRef(cls, "custom-attribute create function")};");
        foreach (var line in pre)
            body.AppendLine($"    {line}");
        string callArgs = argExprs.Count == 0 ? "o" : "o, " + string.Join(", ", argExprs);
        body.AppendLine($"    {ctor.Emittable.CppName}({callArgs});");
        foreach (var line in setLines)
            body.AppendLine($"    {line}");
        body.AppendLine("    return (Dn2CppObject*)o;");
        body.AppendLine("}");
        return fn;
    }

    /// <summary>A named attribute argument as a C++ statement: a direct field assignment
    /// (kind Field) or a setter call (kind Property, whose accessor must be emitted). Null
    /// when the member or its value shape is unsupported.</summary>
    private string? RenderNamedArg(ClassInfo cls, CustomAttributeNamedArgument<TypeDesc> na,
        List<string> pre, ref int arrSeq)
    {
        if (na.Name is not { } name)
            return null;
        if (RenderAttrValue(na.Type, na.Value, pre, ref arrSeq) is not { } val)
            return null;
        // The named member may be declared on an attribute BASE class — walk the chain,
        // most-derived first. The setter half is paired with the reach-side walk in
        // Compilation.ReachAttributesOf: both sides must agree, or the Reachable test below
        // drops the whole attribute row.
        if (na.Kind == CustomAttributeNamedArgumentKind.Field)
        {
            // The C++ struct chains through its base, so o-> reaches an inherited
            // field directly; no reach pairing exists (or is needed) for fields.
            var f = cls.InstanceFieldOnBaseChain(name);
            if (f is null)
                return null;
            string cppT = CppTypes.Of(f.Type);
            string store = cppT.EndsWith("*")
                ? $"o->{f.CppName} = ({cppT})({val});"
                : $"o->{f.CppName} = {val};";
            return f.Type.ContainsGcReferences()
                ? store + " dn2cpp_gc_write_barrier((void*)o);"
                : store;
        }
        var setter = cls.InstanceMethodOnBaseChain("set_" + name);
        if (setter is null || !_c.Reachable.Contains(setter)
            || _backend.ShouldSkipMethodBody(setter.DeclaringClass, setter))
            return null;
        return $"{setter.Emittable.CppName}(o, {val});";
    }

    /// <summary>An attribute argument value (positional or named) as an unboxed C++
    /// expression of the target type's C++ representation. Supports string, the
    /// integer/floating primitives, bool/char, enums (their underlying integer),
    /// System.Type (a typeof handle to an emitted type) and single-dimensional arrays
    /// of those same element shapes (Type[]/string[]/primitive[]/enum[] — built via
    /// pre-statements appended to <paramref name="pre"/>). Returns null for an
    /// unsupported shape (object-typed args, an unemitted Type) so the attribute is
    /// dropped.</summary>
    private string? RenderAttrValue(TypeDesc target, object? value, List<string> pre, ref int arrSeq)
    {
        if (target.Kind == TypeKind.SZArray)
            return RenderAttrArray(target.Element!, value, pre, ref arrSeq);
        if (IsTypeTarget(target))
        {
            if (value is null)
                return "(Dn2CppType*)nullptr";
            if (value is TypeDesc td)
            {
                // A closed, emitted type: its own reflectable ti_ handle.
                if (td.Kind == TypeKind.Class && _emit.Contains(td.Class!))
                    return $"dn2cpp_get_type_from_handle({TypeInfoRef(td.Class!, "custom-attribute Type argument")})";
                // An OPEN generic definition — typeof(Foo<>) — decodes to an External
                // TypeDesc carrying the CLR backtick name (no ClassInfo is minted for an
                // open definition, so GetTypeFromSerializedName falls back to MakeExternal).
                // It is never emitted as a bare ti_, so the _emit test above cannot see it
                // and the whole attribute would drop (the array build short-circuits on the
                // first null element) — which is how Godot's [AssemblyHasScripts] over
                // abstract generic Node bases reached zero registered scripts. Route it to
                // the shared open-definition handle (DN2CPP_TF_GENERICDEF), emitted by
                // EmitTypeInfos for every definition with at least one emitted close;
                // EmitTypeInfos precedes both attribute passes, so _genericDefSyms is
                // complete here.
                if (td.Kind == TypeKind.External
                    && _genericDefSyms.TryGetValue(td.ExternalName!, out var defSym))
                    return $"dn2cpp_get_type_from_handle(&{defSym})";
                // An open definition with no emitted instantiation has no handle, so it
                // stays unrenderable — and dropping it drops the WHOLE attribute. Keep
                // that all-or-nothing contract (packing a shorter Type[] would silently
                // change what a consumer like LookupScriptsInAssembly iterates), but make
                // the drop visible rather than repeat the original silent failure. Scoped
                // to backtick (generic) names so an ordinary tree-shaken typeof stays the
                // quiet IL2CPP-strip it has always been.
                if (td.Kind == TypeKind.External && td.ExternalName!.Contains('`'))
                    Console.Error.WriteLine(
                        $"warning: dropping a custom attribute whose typeof({td.ExternalName}) argument " +
                        "names an open generic definition with no emitted instantiation (no reflectable type handle)");
            }
            return null;
        }
        if (target.IsString)
            return value is null ? "(Dn2CppString*)nullptr" : value is string s ? _literals.GetOrAdd(s) : null;
        if (target.Kind == TypeKind.Class && target.Class!.IsEnum)
            return value is null ? null : $"(int32_t)({System.Convert.ToInt64(value, AttrCI)})";
        if (target.Kind == TypeKind.Primitive)
            return RenderPrimitiveAttrLiteral(target.Primitive, value);
        return null;
    }

    /// <summary>An array-valued attribute argument (e.g. <c>new[] { typeof(A), ... }</c>)
    /// as a fresh array local built by pre-statements. Element kinds mirror the scalar
    /// support of <see cref="RenderAttrValue"/>: Type / string (a Dn2CppArrayRef), and
    /// the primitives / enums (the i4 or packed element-width rep, matching what every
    /// ldelem/stelem in user code addresses via CppTypes.ArrayCppType). A
    /// null array renders as a typed null. Returns the local's name, or null when the
    /// element shape (or any element) is unsupported so the attribute is dropped.</summary>
    private string? RenderAttrArray(TypeDesc element, object? value, List<string> pre, ref int arrSeq)
    {
        bool refElem = IsTypeTarget(element) || element.IsString;
        if (!refElem && element.Kind != TypeKind.Primitive
            && !(element.Kind == TypeKind.Class && element.Class!.IsEnum))
            return null;
        // "Dn2CppArrayRef*" / "Dn2CppArrayI4*" / "Dn2CppArrayN*" — the rep the
        // element's ldelem/stelem lowering addresses.
        string hdr = CppTypes.Of(TypeDesc.MakeSZArray(element));
        if (value is null)
            return $"({hdr})nullptr";
        if (value is not System.Collections.Immutable.ImmutableArray<CustomAttributeTypedArgument<TypeDesc>> items)
            return null;
        var elemExprs = new List<string>();
        foreach (var item in items)
        {
            if (RenderAttrValue(element, item.Value, pre, ref arrSeq) is not { } e)
                return null;
            elemExprs.Add(e);
        }
        string name = $"attrarr{arrSeq++}";
        // Allocate with the precise per-element handle whenever it is header-declared (the
        // reach-side pairing, Compilation.NoteAttrArgArrayType, notes every SZArray-typed
        // attribute argument's element, so for a reached row it always is). An untagged
        // allocation carries a shared imprecise handle with no interface-dispatch map, so
        // the first IEnumerable<T> dispatch over the array inside the attribute ctor aborts
        // loudly. The gate is ArrayTypeInfoDeclared — the same one FieldTypeInfoExpr's
        // SZArray arm asks — so an element noted by nothing else degrades to the untagged
        // allocation rather than naming a symbol with no header extern; that arm keeps this
        // emission well-formed while AssertArrayTypeInfoDegradesWithinCap decides, at the
        // end of the run, whether the degrade is reported or fatal (default: fatal).
        bool tagged = ArrayTypeInfoDeclared(element, "custom-attribute array argument");
        string ti = MethodCompiler.PreciseArrayTypeInfoExprOf(element);
        if (refElem)
        {
            pre.Add(tagged
                ? $"Dn2CppArrayRef* {name} = dn2cpp_newarr_ref_t({elemExprs.Count}, {ti});"
                : $"Dn2CppArrayRef* {name} = dn2cpp_newarr_ref({elemExprs.Count});");
            for (int i = 0; i < elemExprs.Count; i++)
                pre.Add($"{name}->data[{i}] = (Dn2CppObject*)({elemExprs[i]});");
            if (elemExprs.Count > 0)
                pre.Add($"dn2cpp_gc_write_barrier((void*){name});");
        }
        else if (hdr == "Dn2CppArrayI4*")
        {
            pre.Add(tagged
                ? $"Dn2CppArrayI4* {name} = dn2cpp_newarr_i4_t({elemExprs.Count}, {ti});"
                : $"Dn2CppArrayI4* {name} = dn2cpp_newarr_i4({elemExprs.Count});");
            for (int i = 0; i < elemExprs.Count; i++)
                pre.Add($"{name}->data[{i}] = (int32_t)({elemExprs[i]});");
        }
        else
        {
            // Element-width packed storage (byte/short/char/bool/long/float/double,
            // sub-word or 64-bit enums) — plain builtin storage types, so the sizeof
            // never names a t_ struct, and every element is ref-free (the atomic
            // allocator, like EmitNewarr's refFree arm). A Single literal is formatted
            // as a double (RenderPrimitiveAttrLiteral) and narrows back losslessly here.
            string st = CppTypes.StorageOf(element);
            pre.Add(tagged
                ? $"Dn2CppArrayN* {name} = dn2cpp_newarr_n_atomic_t({elemExprs.Count}, (int32_t)sizeof({st}), {ti});"
                : $"Dn2CppArrayN* {name} = dn2cpp_newarr_n({elemExprs.Count}, (int32_t)sizeof({st}));");
            for (int i = 0; i < elemExprs.Count; i++)
                pre.Add($"(({st}*){name}->data)[{i}] = ({st})({elemExprs[i]});");
        }
        return name;
    }

    private static bool IsTypeTarget(TypeDesc t) =>
        (t.Kind == TypeKind.External && t.ExternalName == "System.Type")
        || (t.Kind == TypeKind.Class && t.Class!.FullName == "System.Type");

    /// <summary>A primitive attribute-argument value as a C++ literal of the matching
    /// width; null for an unsupported/unconvertible value.</summary>
    private static string? RenderPrimitiveAttrLiteral(PrimitiveTypeCode code, object? value)
    {
        if (value is null)
            return null;
        try
        {
            return code switch
            {
                PrimitiveTypeCode.Boolean => System.Convert.ToBoolean(value, AttrCI) ? "1" : "0",
                PrimitiveTypeCode.Char => $"(int32_t){(int)System.Convert.ToChar(value, AttrCI)}",
                PrimitiveTypeCode.SByte or PrimitiveTypeCode.Int16 or PrimitiveTypeCode.Int32
                    or PrimitiveTypeCode.Byte or PrimitiveTypeCode.UInt16
                    => System.Convert.ToInt64(value, AttrCI).ToString(AttrCI),
                PrimitiveTypeCode.UInt32 => System.Convert.ToUInt32(value, AttrCI).ToString(AttrCI) + "U",
                PrimitiveTypeCode.Int64 => System.Convert.ToInt64(value, AttrCI).ToString(AttrCI) + "LL",
                PrimitiveTypeCode.UInt64 => System.Convert.ToUInt64(value, AttrCI).ToString(AttrCI) + "ULL",
                // Route both floating widths through the same formatter the body path
                // uses (CppTypes.FloatLiteral): the old "R"-plus-"f" form emitted an
                // invalid C++ token for an integral value (`2f`) and mishandled the
                // special values. A Single widens to double for formatting and narrows
                // back losslessly at the C++ ctor's float parameter.
                PrimitiveTypeCode.Single => CppTypes.FloatLiteral(System.Convert.ToSingle(value, AttrCI)),
                PrimitiveTypeCode.Double => CppTypes.FloatLiteral(System.Convert.ToDouble(value, AttrCI)),
                _ => null,
            };
        }
        catch (Exception e) when (!Compilation.IsMustEscape(e))
        {
            return null;
        }
    }

    /// <summary>The C++ struct type that blocks materializing <paramref name="m"/>'s
    /// invoker thunk, or null when the thunk compiles. The classification mirrors
    /// <see cref="EmitInvokerThunk"/> exactly: pointer-rendered shapes collapse to a
    /// uniform <c>void*</c> in the thunk, so only a BY-VALUE type spells its exact C++
    /// name — and that name must be one the emit set actually declares
    /// (<see cref="StructDeclared"/> over the emitted classes' struct names, the set
    /// <see cref="EmitStructs"/> defines; runtime-provided types always pass). A row a
    /// <c>--hotupdate-base</c> build keeps regardless of reachability — chiefly an
    /// interface's signature-only rows — can name a struct nothing else in the image
    /// ever laid out (an uninstantiated specialization, an intrinsic-represented value
    /// type such as <c>Span&lt;char&gt;</c>), and a thunk over it is C++ that does not
    /// compile. A REACHED method can never be blocked: its emitted body's own
    /// prototype names the same <see cref="CppTypes.Of"/> renderings, so they are
    /// declared or the output never compiled at all — which is why this check cannot
    /// change the bytes of any output that compiles today. Decode-free beyond what the
    /// caller already paid: the member-table row read <c>m.Signature</c> before asking.
    /// Throws the same <see cref="NotSupportedException"/> as the thunk emitter for a
    /// shape <see cref="CppTypes"/> cannot render — callers treat both alike.</summary>
    private static string? InvokerThunkBlocker(MethodInfo m, IReadOnlySet<string> declaredStructs)
    {
        static string? Blocked(TypeDesc t, IReadOnlySet<string> declared)
        {
            string cpp = CppTypes.Of(t);
            return !cpp.EndsWith("*", StringComparison.Ordinal) && !StructDeclared(cpp, declared)
                ? cpp
                : null;
        }
        var ret = m.Signature.ReturnType;
        if (!ret.IsVoid && Blocked(ret, declaredStructs) is { } blockedRet)
            return blockedRet;
        foreach (var p in m.Signature.ParameterTypes)
            if (Blocked(p, declaredStructs) is { } blockedParam)
                return blockedParam;
        return null;
    }

    /// <summary>Emits (once per distinct C++ ABI shape) the signature-deduplicated
    /// invoker thunk for a reachable method and returns its name. The thunk
    /// unboxes/casts each boxed arg to the parameter's C++ type, calls the method's
    /// fn pointer with the correct signature, and boxes the result. Reference-typed
    /// params/return collapse to a uniform pointer (so all-ref signatures share one
    /// thunk); value types keep their exact C++ type. A value-type receiver's payload
    /// adjustment is done by the runtime dispatcher, so the receiver is a plain
    /// pointer here. ref/out/pointer params are treated as pointers — invoking such a
    /// method via reflection is out of scope (the thunk still links). Callers gate on
    /// <see cref="InvokerThunkBlocker"/> first: a signature naming a by-value struct
    /// the emit set never declared must get a trapping stub instead
    /// (TypeMetadataEmitter.InvokerMissStub), never this thunk.</summary>
    private string EmitInvokerThunk(StringBuilder sb, MethodInfo m, HashSet<string> seen)
    {
        var ps = m.Signature.ParameterTypes;
        bool instance = !m.IsStatic;
        var ret = m.Signature.ReturnType;

        static (bool Ptr, string Cpp) Classify(TypeDesc t)
        {
            string cpp = CppTypes.Of(t);
            return (cpp.EndsWith("*"), cpp);
        }

        // Build the shape key + the C++ pieces.
        var key = new StringBuilder(instance ? "i" : "s");
        var sigParams = new List<string>();   // function-pointer parameter types
        var callArgs = new List<string>();    // argument expressions
        if (instance)
        {
            sigParams.Add("void*");
            callArgs.Add("self");
        }
        for (int i = 0; i < ps.Length; i++)
        {
            var (ptr, cpp) = Classify(ps[i]);
            if (MethodCompiler.IsNfiCppType(cpp))
            {
                // A headerless intrinsic parameter (CultureInfo/NumberFormatInfo/
                // TextInfo — the const Dn2CppNumberFormatInfo* lowering). The boxed
                // argument array holds the MANAGED form: an `object[]` element got
                // the interned wrapper at its own escape, so handing args[i] to a
                // parameter that means the raw struct pointer punts a wrapper into
                // it and the callee misreads the box's header as culture fields.
                // Unwrap — tolerantly, so a raw pointer that reached the array
                // through an erased path passes through unchanged. Its own shape
                // letter, because collapsing it onto _P is exactly what made one
                // thunk serve both meanings.
                key.Append("_N");
                sigParams.Add("void*");
                callArgs.Add($"(void*)dn2cpp_nfi_unwrap(args[{i}])");
            }
            else if (MethodCompiler.IsAsmCppType(cpp))
            {
                // The other wrappable headerless parameter — Assembly/Module's
                // const char*. Same argument, own funnel, own shape letter.
                key.Append("_A");
                sigParams.Add("void*");
                callArgs.Add($"(void*)dn2cpp_asm_unwrap(args[{i}])");
            }
            else if (ptr)
            {
                key.Append("_P");
                sigParams.Add("void*");
                callArgs.Add($"args[{i}]");
            }
            else
            {
                key.Append('_').Append(CppNaming.Sanitize(cpp));
                sigParams.Add(cpp);
                callArgs.Add($"*({cpp}*)((char*)args[{i}] + sizeof(Dn2CppObject))");
            }
        }
        // Return shape.
        string retSig, retToken;
        // 0 void, 1 pointer, 2 value, 3 NFI headerless (nfi-wrapped), 4 Assembly/
        // Module headerless (asm-wrapped).
        int retKind;
        string nfiRetKind = ""; // the DN2CPP_NFI_KIND_* / DN2CPP_ASM_KIND_* macro of retKind 3/4
        if (ret.IsVoid) { retSig = "void"; retToken = "v"; retKind = 0; }
        else
        {
            var (ptr, cpp) = Classify(ret);
            if (MethodCompiler.IsNfiCppType(cpp))
            {
                // The mirror of the parameter arm: the method hands back the raw
                // headerless pointer, and Invoke's contract is to hand back an
                // `object`. Wrap it, with the DECLARED static kind rather than
                // the runtime PROVIDER probe — the kind is a property of this
                // shape, so it rides the dedup key and the thunk stays exact for
                // a TextInfo return, which the isNfi probe cannot tell from a
                // culture.
                nfiRetKind = MethodCompiler.NfiKindOf(ret);
                retSig = "const Dn2CppNumberFormatInfo*";
                retToken = "N" + nfiRetKind["DN2CPP_NFI_KIND_".Length..];
                retKind = 3;
            }
            else if (MethodCompiler.IsAsmCppType(cpp))
            {
                // The Assembly/Module mirror: wrap the raw handle with the DECLARED
                // kind, which rides the dedup key like the NFI one.
                nfiRetKind = MethodCompiler.AsmKindOf(ret);
                retSig = "const char*";
                retToken = "A" + nfiRetKind["DN2CPP_ASM_KIND_".Length..];
                retKind = 4;
            }
            else if (ptr) { retSig = "Dn2CppObject*"; retToken = "P"; retKind = 1; }
            else { retSig = cpp; retToken = CppNaming.Sanitize(cpp); retKind = 2; }
        }
        key.Append("__r_").Append(retToken);

        string name = "inv_" + key;
        if (seen.Add(name))
        {
            string fnType = $"{retSig}(*)({string.Join(", ", sigParams)})";
            string call = $"f({string.Join(", ", callArgs)})";
            string body = retKind switch
            {
                0 => $"{call}; return nullptr;",
                1 => $"return (Dn2CppObject*){call};",
                3 => $"return dn2cpp_nfi_wrap({call}, {nfiRetKind});",
                4 => $"return dn2cpp_asm_wrap({call}, {nfiRetKind});",
                _ => $"{retSig} r = {call}; return dn2cpp_box(retType, &r, sizeof(r));",
            };
            sb.AppendLine($"static Dn2CppObject* {name}(void* fn, [[maybe_unused]] Dn2CppObject* self, " +
                          $"[[maybe_unused]] Dn2CppObject** args, [[maybe_unused]] const Dn2CppTypeInfo* retType) " +
                          $"{{ auto f = ({fnType})fn; {body} }}");
        }
        return name;
    }

    /// <summary>The source parameter names of a method (0-based, indexed by position),
    /// read from the Param metadata table. A parameter with no Param row (or
    /// no name) yields null → ParameterInfo.Name reports null. The handle of a generic
    /// instantiation still resolves the template's Param rows, whose names are stable.</summary>
    private static string?[] ParamNames(MethodInfo m, int count)
    {
        var names = new string?[count];
        try
        {
            var reader = m.Module.Reader;
            var md = reader.GetMethodDefinition(m.Handle);
            foreach (var ph in md.GetParameters())
            {
                var p = reader.GetParameter(ph);
                int seq = p.SequenceNumber; // 0 == return value
                if (seq >= 1 && seq <= count)
                    names[seq - 1] = reader.GetString(p.Name);
            }
        }
        catch (Exception e) when (!Compilation.IsMustEscape(e))
        {
            // Best-effort: a metadata shape we can't read just leaves names null.
        }
        return names;
    }

    /// <summary>The startup type-info binds: for every exception type the runtime raises
    /// itself, the emitted metadata object that has to be copied into the runtime's own
    /// handle before anything runs. The runtime declares those handles with a name, a base
    /// and nothing else — no vtable (so a <c>callvirt</c> of a virtual member declared on
    /// one, ArgumentException.get_ParamName, loaded slot k off a null table), no instance
    /// size (so an object the runtime allocated had no storage for the type's own fields)
    /// and no reflection tables. It cannot know any of them: they are a property of the
    /// transpiled program. The generated code does, so it hands them over here, into the
    /// SAME handle catch/isinst/typeof already name — the alternative, an emitted ti_ next
    /// to the runtime's, is two type-infos for one managed type, and every pointer-identity
    /// test in the runtime (catch matching above all) would then depend on who allocated
    /// the object.</summary>
    private void EmitTypeBinds(StringBuilder sb)
    {
        sb.AppendLine("// ---- runtime type-info binds (exception handles the runtime raises) ----");
        var rows = _typeBinds
            .Select(c => $"{{ {TypeInfoRef(c, "runtime type-info bind")}, &{c.CppTypeInfoDefName} }}")
            .ToList();
        sb.AppendLine(rows.Count == 0
            ? "const Dn2CppTypeBind dn2cpp_type_binds[] = { { nullptr, nullptr } };"
            : $"const Dn2CppTypeBind dn2cpp_type_binds[] = {{ {string.Join(", ", rows)} }};");
        sb.AppendLine($"const int32_t dn2cpp_type_bind_count = {rows.Count};");
        // The vtable slot of System.Exception::get_Message, a program property the runtime
        // cannot know: dn2cpp_exception_message dispatches through it so a derived exception's
        // get_Message override is honored on a base-typed (System.Exception) receiver. -1 in
        // a corelib-less build (no Exception type to slot). Defined only in generated output,
        // like dn2cpp_type_binds above — every consumer of the runtime links generated code.
        sb.AppendLine($"const int32_t dn2cpp_exception_get_message_slot = {_c.ExceptionGetMessageSlot()};");
        sb.AppendLine();
        EmitBclMessages(sb);
    }

    /// <summary>The SR texts the C++ runtime's own throw sites raise, folded in
    /// here for the reason every other SR read is: at run time the table they live in may
    /// have been dropped (<c>--no-manifest-resources</c>), and a fault that faults building
    /// its own message is uncatchable. Keyed by resource NAME, so a runtime asking for a key
    /// this table lacks reads null and falls back to Exception.Message's type-name text
    /// rather than reading a neighbour's sentence. <see cref="BclMessages"/> owns the
    /// list.</summary>
    private void EmitBclMessages(StringBuilder sb)
    {
        sb.AppendLine("// ---- BCL exception message texts (runtime throw sites) ----");
        var rows = new List<string>();
        foreach (string key in BclMessages.Keys)
        {
            string? text = _c.CoreLibSrText(key);
            rows.Add($"{{ \"{key}\", {(text is null ? "nullptr" : "\"" + CLiteral(text) + "\"")} }}");
        }
        sb.AppendLine($"const Dn2CppBclMessage dn2cpp_bcl_messages[] = {{ {string.Join(", ", rows)} }};");
        sb.AppendLine($"const int32_t dn2cpp_bcl_message_count = {rows.Count};");
        sb.AppendLine();
    }

    /// <summary>Body of a C++ narrow string literal for arbitrary resource text (the
    /// message table's only producer): SR strings carry quotes and newlines.</summary>
    private static string CLiteral(string s) => s
        .Replace("\\", "\\\\").Replace("\"", "\\\"")
        .Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", "\\t");

    /// <summary>Type name registry: a CLR-reflection-name → Dn2CppTypeInfo* table
    /// backing Type.GetType(string). Seeds every runtime-owned/bound/raised handle from
    /// <see cref="CoreIntrinsics.RuntimeTypeInfoRows"/> — the same table typeof asks — then
    /// every emitted user type/enum keyed by <see cref="Compilation.ReflectionTypeName"/>
    /// — the same string baked into its type-info's <c>name</c>, so a nested type
    /// resolves under the CLR '+' syntax (<c>Type.GetType("Ns.Outer+Inner")</c>) and
    /// two same-named nested types get distinct keys instead of the first silently
    /// swallowing the second. Deduped by name (the runtime seed wins). A closed generic's
    /// key stays the MANGLED instantiation name; the CLR <c>Ns.List`1[[…]]</c>
    /// spelling resolves through the runtime's structural fallback instead
    /// (<c>dn2cpp_resolve_type_name</c>), never through a key here. Still out
    /// of scope, like IL2CPP's metadata resolving only built-in types: any type this
    /// emission defines no type-info for.</summary>
    private void EmitTypeRegistry(StringBuilder sb)
    {
        sb.AppendLine("// ---- type name registry (Type.GetType) ----");
        var seen = new HashSet<string>(System.StringComparer.Ordinal);
        var rows = new List<string>();
        void Add(string name, string handle)
        {
            if (seen.Add(name))
                rows.Add($"{{ \"{name}\", {handle} }}");
        }
        // The seed is DERIVED from the three runtime type-info tables, never listed here:
        // typeof asks them through ClassInfo.CppTypeInfoName, so a second list is a second
        // answer, and the direction it fails in is silent — Type.GetType answering null
        // about a type typeof hands back a live handle for. It also gives the hot-update
        // loader every runtime-raised exception name: a BPI catch clause is a type import
        // resolved through this table, an unresolved one binds to null WITHOUT failing the
        // load, and eh_catch_matches aborts the process the first time it is tested.
        foreach (var (n, h) in CoreIntrinsics.RuntimeTypeInfoRows())
            Add(n, h);
        // A runtime-template level's ti_ exists but is NOT a CLR type — the
        // synthesized clones register their closed names on the dynamic
        // side-chain at MakeGenericType time.
        foreach (var cls in EmittedClasses.Where(c => !c.IsEnum && !IsCanonicalWorld(c)
                     && !IsRuntimeTemplateLevel(c)))
            Add(Compilation.ReflectionTypeName(cls), TypeInfoRef(cls, "type-name registry row"));
        foreach (var en in _c.ReferencedTypes.Where(c => c.IsEnum))
            Add(Compilation.ReflectionTypeName(en), TypeInfoRef(en, "type-name registry row (enum)"));
        // The minimal type-infos of the referenced intrinsics (StringBuilder, CultureInfo,
        // the SIMD vectors, …): a lowering already names each handle, so leaving them out
        // made Type.GetType answer null about a type typeof answers about — and, for a
        // closed intrinsic generic, made MakeGenericType and the composed-name resolution
        // find no candidate at all (both scan this table). Identity is safe because the row
        // names an EXISTING handle rather than minting one: ReferencedIntrinsicTypeInfos
        // selects exactly the classes whose body-path type-info expression IS their own ti_.
        // Placed after the well-known and emitted rows, so this loop can only fill a gap.
        foreach (var c in _referencedIntrinsicTis)
            Add(Compilation.ReflectionTypeName(c), TypeInfoRef(c, "type-name registry row (referenced intrinsic)"));
        // Generic open-definition handles, keyed by the backtick CLR name so the
        // registry covers every closed instantiation's genericDef. Lets MakeGenericType
        // scan them and (parity with .NET) GetType("Namespace.Type`N") resolve the
        // definition. Populated by EmitTypeInfos, which runs before this method.
        foreach (var (defName, sym) in _genericDefSyms)
            Add(defName, "&" + sym);
        // Per-element array type names: Type.GetType("System.Int32[]") etc.
        // resolve to the emitted ti_arr_ handle. Populated by EmitArrayTypeInfos (run by
        // EmitTypeInfos, which precedes this method).
        foreach (var (name, handle) in _arrayTypeSyms)
            Add(name, handle);
        sb.AppendLine($"const Dn2CppTypeRegEntry dn2cpp_type_registry[] = {{ {string.Join(", ", rows)} }};");
        sb.AppendLine($"const int32_t dn2cpp_type_registry_count = {rows.Count};");
        sb.AppendLine();
        EmitRuntimeTemplates(sb);
    }

    /// <summary>Runtime-instantiation template rows: one per eligible template
    /// chain level, keyed by the level's open-definition handle so
    /// <c>dn2cpp_type_make_generic</c> can clone the level's emitted metadata for
    /// an instantiation the AOT image lacks (base levels are looked up by their
    /// template ti when the clone synthesizes its base chain). The two symbols
    /// define unconditionally — the runtime references them in every build.</summary>
    private void EmitRuntimeTemplates(StringBuilder sb)
    {
        var rows = new List<string>();
        var seen = new HashSet<ClassInfo>();
        foreach (var (defName, _, levels) in _c.EligibleRuntimeTemplates)
            foreach (var lv in levels)
            {
                if (!seen.Add(lv))
                    continue;
                string levelDef = GenericDefInfo(lv) is { } gdi
                        && _genericDefSyms.TryGetValue(gdi.DefName, out var sym)
                    ? "&" + sym
                    : throw new InvalidOperationException(
                        $"runtime template {lv.FullName}: no gendef symbol (def {defName})");
                var desc = _c.RuntimeTemplateSlotDescriptor(lv);
                string descExpr = "nullptr";
                if (desc.Length > 0)
                {
                    string descSym = "rgctxdesc_" + lv.CppName;
                    sb.AppendLine($"static const int32_t {descSym}[] = {{ {string.Join(", ", desc)} }};");
                    descExpr = descSym;
                }
                rows.Add($"{{ {levelDef}, {TypeInfoRef(lv, "runtime template row")}, {descExpr}, "
                    + $"{desc.Length}, {lv.Context.TypeArgs.Length} }}");
            }
        sb.AppendLine("// ---- runtime-instantiation templates (MakeGenericType clone sources) ----");
        if (rows.Count == 0)
        {
            sb.AppendLine("const Dn2CppRuntimeTemplate* const dn2cpp_runtime_templates = nullptr;");
        }
        else
        {
            sb.AppendLine($"static const Dn2CppRuntimeTemplate dn2cpp_runtime_template_rows[] = {{ {string.Join(", ", rows)} }};");
            sb.AppendLine("const Dn2CppRuntimeTemplate* const dn2cpp_runtime_templates = dn2cpp_runtime_template_rows;");
        }
        sb.AppendLine($"const int32_t dn2cpp_runtime_template_count = {rows.Count};");
        sb.AppendLine();
    }

    /// <summary>Assembly registry: one entry per loaded module (app + -r references),
    /// keyed by the assembly simple name — the <c>const char*</c> handle Assembly is
    /// modeled as — with the module's reflectable assembly-level custom attributes.
    /// Backs Assembly.GetCustomAttributes/IsDefined on any Assembly handle
    /// (Type.Assembly / GetEntryAssembly). The attribute rows follow the same
    /// conditional bound as the member tables: an attribute renders only when its ctor
    /// was reached (the reflection-attribute route), so a program that never reflects
    /// attributes gets name-only rows.</summary>
    private void EmitAssemblyRegistry(StringBuilder sb)
    {
        sb.AppendLine("// ---- assembly registry (Assembly identity + assembly-level custom attributes) ----");
        var rows = new List<string>();
        var seen = new HashSet<string>(System.StringComparer.Ordinal);
        foreach (var module in _c.Modules)
        {
            string name = module.AssemblyName;
            if (name.Length == 0 || !seen.Add(name))
                continue;
            (string Expr, int Count) ca = ("nullptr", 0);
            // Identity pieces for the .NET display name (Assembly.FullName /
            // Type.AssemblyQualifiedName / AssemblyName): the metadata version,
            // culture ("neutral" when empty, as displayed) and the public-key
            // token derived from the assembly's public key (null when unsigned —
            // the runtime prints "null", matching real .NET).
            string version = "nullptr", culture = "nullptr", pkt = "nullptr";
            try
            {
                var def = module.Reader.GetAssemblyDefinition();
                ca = BuildAttrTable(sb, $"assembly{module.Index}", module,
                    def.GetCustomAttributes());
                version = $"\"{def.Version.Major}.{def.Version.Minor}.{def.Version.Build}.{def.Version.Revision}\"";
                string cultureName = module.Reader.GetString(def.Culture);
                culture = cultureName.Length == 0 ? "\"neutral\"" : $"\"{cultureName}\"";
                if (PublicKeyTokenOf(module.Reader, def) is { } token)
                    pkt = $"\"{token}\"";
            }
            catch (Exception e) when (!Compilation.IsMustEscape(e))
            {
                // A module without a decodable assembly definition keeps a name-only row.
            }
            var res = BuildManifestResourceTable(sb, module);
            // [assembly: NeutralResourcesLanguage(...)], read only when the program reads
            // manifest resources at all: EVERY BCL assembly declares this attribute, so an
            // unconditional read would lengthen the registry row of every program to carry
            // a fact none of them can ask about.
            (string? Culture, bool Satellite) nrl = res.Count != 0 || res.Dropped
                ? NeutralResourcesLanguageOf(module) : (null, false);
            // The resource members are appended only when there is something to say — a
            // table to name, or the resourcesDropped bit: a shorter row zero-fills the
            // rest (the struct's trailing-member 0-fill convention), so a program that
            // reads no manifest resource emits exactly the text it always did. A dropped
            // assembly whose roots kept nothing carries `nullptr, 0, 1` — the bit is what
            // makes a run-time miss throw instead of lying with null. The initializers are
            // POSITIONAL, so a neutral-language column obliges the three before it.
            string tail = nrl.Culture is not null
                    ? $", {res.Expr}, {res.Count}, {(res.Dropped ? 1 : 0)}, "
                      + $"{CppUtf8Literal(nrl.Culture)}, {(nrl.Satellite ? 1 : 0)}"
                : res.Dropped ? $", {res.Expr}, {res.Count}, 1"
                : res.Count == 0 ? "" : $", {res.Expr}, {res.Count}";
            rows.Add($"{{ \"{name}\", {ca.Expr}, {ca.Count}, {version}, {culture}, {pkt}{tail} }}");
        }
        sb.AppendLine($"const Dn2CppAssemblyRegEntry dn2cpp_assembly_registry[] = {{ {string.Join(", ", rows)} }};");
        sb.AppendLine($"const int32_t dn2cpp_assembly_registry_count = {rows.Count};");
        sb.AppendLine();
    }

    /// <summary>Emits one module's embedded manifest-resource blobs as file-local
    /// <c>.rodata</c> byte arrays plus the <c>Dn2CppManifestResource</c> table that
    /// names them (into <paramref name="sb"/>, right before the registry row that
    /// points at it — same TU, so the file-local symbols always resolve), returning
    /// the table expression and its length. Serves
    /// <c>Assembly.GetManifestResourceStream</c> / <c>GetManifestResourceNames</c> /
    /// <c>GetManifestResourceInfo</c>.
    ///
    /// <para>Nothing is emitted unless some emitted body actually lowered one of
    /// those reads (<see cref="Compilation.ManifestResourcesUsed"/>) — the bodies are
    /// all compiled by the time the registry is emitted, so the test is exact rather
    /// than an over-approximation, and a program that reads no resource is
    /// byte-identical. Order is metadata order, which is what real .NET's
    /// <c>GetManifestResourceNames</c> reports, and it is deterministic.</para>
    ///
    /// <para><c>--no-manifest-resources</c> narrows a named assembly's table
    /// to its <c>--manifest-resource-root</c> rows and reports Dropped, which the
    /// caller turns into the registry row's <c>resourcesDropped</c> bit — the honesty
    /// bit that lets the runtime throw on a miss instead of answering the null that
    /// means "no such resource". Dropped is reported only when resources were
    /// actually shed: an assembly that carries none answers every question the same
    /// with or without the flag, so its null/empty stays truthful and needs no
    /// throw.</para></summary>
    private (string Expr, int Count, bool Dropped) BuildManifestResourceTable(
        StringBuilder sb, Module module)
    {
        if (!_c.ManifestResourcesUsed)
            return ("nullptr", 0, false);
        List<(string Name, byte[] Bytes)> blobs;
        try
        {
            blobs = ResourceStrings.ReadBlobs(module.PE, module.Reader);
        }
        catch (Exception e) when (!Compilation.IsMustEscape(e))
        {
            // Same posture as the identity columns above: an undecodable resource
            // directory costs the module its resources, never the transpile.
            return ("nullptr", 0, false);
        }
        if (blobs.Count == 0)
            return ("nullptr", 0, false);
        bool dropped = _c.DropsManifestResources(module);
        if (dropped)
        {
            // The keep-set: rooted names stay (exact manifest-name match, validated
            // non-empty against the dropped assemblies at load time), everything else
            // is shed. Order within the survivors stays metadata order.
            blobs = blobs.Where(b => _c.IsManifestResourceRoot(b.Name)).ToList();
            if (blobs.Count == 0)
                return ("nullptr", 0, true);
        }
        var rows = new List<string>();
        for (int i = 0; i < blobs.Count; i++)
        {
            var (name, bytes) = blobs[i];
            string data = "nullptr";
            if (bytes.Length > 0)
            {
                // A zero-length array is ill-formed C++, hence the nullptr above for an
                // empty resource (the length column carries the truth either way).
                data = $"resdata_{module.Index}_{i}";
                sb.Append("static const uint8_t ").Append(data).Append("[] = {");
                for (int b = 0; b < bytes.Length; b++)
                {
                    // Wrapped rather than one enormous line: a multi-hundred-KB resource
                    // is a realistic input and a single token run that long is a needless
                    // burden on the C++ front end.
                    sb.Append(b % 32 == 0 ? "\n    " : " ");
                    sb.Append("0x").Append(bytes[b].ToString("x2")).Append(',');
                }
                sb.AppendLine("\n};");
            }
            rows.Add($"{{ {CppUtf8Literal(name)}, {data}, {bytes.Length} }}");
        }
        string tab = $"restab_assembly{module.Index}";
        sb.AppendLine($"static const Dn2CppManifestResource {tab}[] = {{ {string.Join(", ", rows)} }};");
        return (tab, rows.Count, dropped);
    }

    /// <summary>The module's <c>[assembly: NeutralResourcesLanguage(culture[, location])]</c>
    /// declaration — the culture name and whether the ultimate fallback lives in a
    /// SATELLITE — or <c>(null, false)</c> when it declares none.
    ///
    /// <para>Read straight off the attribute's value blob rather than through
    /// <c>CustomAttribute.DecodeValue</c>, and that is not a micro-optimization: the
    /// second fixed argument is enum-typed, so decoding it resolves
    /// <c>UltimateResourceFallbackLocation</c> — a type nothing else in the program
    /// names — and a resolve is a decode that grows <c>Compilation.Classes</c>. This
    /// runs during emission, after the emit set is closed, which is the one place a new
    /// class cannot be emitted. The blob shape is ECMA-335 II.23.3 and needs nothing
    /// resolved: prolog, a SerString, then the optional int32.</para>
    ///
    /// <para>Whether that int32 is there is read from the constructor's ARITY, never from
    /// how many bytes are left: a byte-count guess would fail as a wrong fallback location —
    /// a silently wrong resource answer, not a parse error. The arity comes from the
    /// signature blob's own parameter count, one compressed integer in, resolving
    /// nothing.</para></summary>
    private static (string? Culture, bool Satellite) NeutralResourcesLanguageOf(Module module)
    {
        try
        {
            var reader = module.Reader;
            foreach (var cah in reader.GetAssemblyDefinition().GetCustomAttributes())
            {
                var ca = reader.GetCustomAttribute(cah);
                if (Compilation.AttributeTypeName(reader, ca)
                    != "System.Resources.NeutralResourcesLanguageAttribute")
                    continue;
                var blob = reader.GetBlobReader(ca.Value);
                if (blob.ReadUInt16() != 1)
                    return (null, false);       // not the ECMA prolog — leave it alone
                string? culture = blob.ReadSerializedString();
                if (culture is null)
                    return (null, false);       // a null culture declares nothing
                bool satellite = false;
                if (AttributeCtorParamCount(reader, ca) >= 2 && blob.RemainingBytes >= 4)
                    satellite = blob.ReadInt32() == 1;   // UltimateResourceFallbackLocation
                return (culture, satellite);
            }
        }
        catch (Exception e) when (!Compilation.IsMustEscape(e))
        {
            // Same posture as the identity columns: an undecodable attribute costs the
            // module its declared neutral culture (the lookup then refuses every
            // culture-specific ask), never the transpile.
        }
        return (null, false);
    }

    /// <summary>The parameter count of a custom attribute's constructor, read out of the
    /// signature blob's own ParamCount field (ECMA-335 II.23.2.1: a calling convention
    /// byte, then the compressed parameter count). No signature is decoded, so nothing
    /// is resolved and no class is minted.</summary>
    private static int AttributeCtorParamCount(MetadataReader reader, CustomAttribute ca)
    {
        BlobHandle sig = ca.Constructor.Kind switch
        {
            HandleKind.MethodDefinition =>
                reader.GetMethodDefinition((MethodDefinitionHandle)ca.Constructor).Signature,
            HandleKind.MemberReference =>
                reader.GetMemberReference((MemberReferenceHandle)ca.Constructor).Signature,
            _ => default,
        };
        if (sig.IsNil)
            return 0;
        var blob = reader.GetBlobReader(sig);
        blob.ReadSignatureHeader();
        return blob.ReadCompressedInteger();
    }

    /// <summary>A C++ narrow string literal holding <paramref name="s"/>'s UTF-8 bytes.
    /// Everything outside printable ASCII — and the two characters a literal cannot
    /// carry raw — goes out as a three-digit OCTAL escape: <c>\xNN</c> is greedy in
    /// C++ (a following hex digit would join the escape), three-digit octal is not.</summary>
    private static string CppUtf8Literal(string s)
    {
        var sb = new StringBuilder(s.Length + 2);
        sb.Append('"');
        foreach (byte b in Encoding.UTF8.GetBytes(s))
        {
            if (b is >= 0x20 and < 0x7F && b != (byte)'"' && b != (byte)'\\')
                sb.Append((char)b);
            else
                // Spelled out rather than through Convert.ToString(b, 8).PadLeft(3, '0'):
                // the transpiler is its own input, and this keeps the site on BCL surface
                // the self-host corpus already carries.
                sb.Append('\\').Append((char)('0' + (b >> 6)))
                    .Append((char)('0' + ((b >> 3) & 7))).Append((char)('0' + (b & 7)));
        }
        sb.Append('"');
        return sb.ToString();
    }

    /// <summary>The assembly's public-key token as 16 lowercase hex chars — the
    /// last 8 bytes of the public key's SHA-1, reversed (the ECMA-335 token
    /// derivation real .NET displays) — or null for an unsigned assembly.</summary>
    private static string? PublicKeyTokenOf(MetadataReader reader, AssemblyDefinition def)
    {
        if (def.PublicKey.IsNil)
            return null;
        byte[] publicKey = reader.GetBlobBytes(def.PublicKey);
        if (publicKey.Length == 0)
            return null;
        byte[] hash = System.Security.Cryptography.SHA1.HashData(publicKey);
        var sb = new StringBuilder(16);
        for (int i = hash.Length - 1; i >= hash.Length - 8; i--)
            sb.Append(hash[i].ToString("x2"));
        return sb.ToString();
    }

    /// <summary>Adapters that let delegates over static methods be invoked uniformly
    /// through the target argument every <c>f_method</c> is called with — in the two
    /// shapes that argument can have (see <see cref="DelegateAdapter"/>): an OPEN
    /// static delegate ignores it, a CLOSED one takes the static's bound first
    /// argument out of it and forwards the delegate's own arguments to the rest.</summary>
    private void EmitDelegateAdapters(CppOutput o)
    {
        if (_c.DelegateAdapters.Count == 0)
            return;
        // Referenced by method bodies (delegate construction over a static target), so the
        // adapter gets external linkage + a header prototype; the body stays in the data TU.
        o.Header.AppendLine("// ---- delegate adapters for static targets ----");
        o.Data.AppendLine("// ---- delegate adapters for static targets ----");
        foreach (var adapter in _c.DelegateAdapters)
        {
            var m = adapter.Method;
            var ps = m.Signature.ParameterTypes;
            var sigParams = new List<string>();
            var callArgs = new List<string>();
            if (adapter.NfiErased)
            {
                EmitNfiErasedDelegateAdapter(o, adapter);
                continue;
            }
            if (adapter.Closed)
            {
                // The bound first argument rides the target slot; the delegate's own
                // arguments start at the static's SECOND parameter. A reference type by
                // construction (MethodCompiler.IsClosedStaticDelegate rejects any
                // other), and every reference-typed struct derives from Dn2CppObject,
                // so the recovery is a plain downcast.
                sigParams.Add("Dn2CppObject* __target");
                callArgs.Add($"({CppTypes.Of(ps[0])})__target");
            }
            else
            {
                sigParams.Add("[[maybe_unused]] Dn2CppObject* __target");
            }
            for (int i = adapter.Closed ? 1 : 0; i < ps.Length; i++)
            {
                sigParams.Add($"{CppTypes.Of(ps[i])} a{i}");
                callArgs.Add($"a{i}");
            }
            string ret = m.Signature.ReturnType.IsVoid ? "void" : CppTypes.Of(m.Signature.ReturnType);
            string call = $"{m.CppName}({string.Join(", ", callArgs)})";
            string head = $"{ret} {adapter.CppName}({string.Join(", ", sigParams)})";
            o.Header.AppendLine($"{head};");
            o.Data.AppendLine(head);
            o.Data.AppendLine("{");
            o.Data.AppendLine(m.Signature.ReturnType.IsVoid ? $"    {call};" : $"    return {call};");
            o.Data.AppendLine("}");
        }
        o.Header.AppendLine();
        o.Data.AppendLine();
    }

    /// <summary>The adapter that presents a method group to <c>f_method</c> under the
    /// canonical NFI-erased ABI (see <see cref="DelegateAdapter"/>): every headerless
    /// intrinsic position is spelled <c>Dn2CppObject*</c> and unwrapped on the way in /
    /// wrapped on the way out, so one delegate instance invoked through two delegate
    /// types — the variance case, which emits no IL to hook — reads the same bytes
    /// either way. Unlike the open/closed pair this shape also serves an INSTANCE
    /// target, whose receiver comes out of the same target slot.</summary>
    private void EmitNfiErasedDelegateAdapter(CppOutput o, DelegateAdapter adapter)
    {
        var m = adapter.Method;
        var ps = m.Signature.ParameterTypes;
        var sigParams = new List<string> { "Dn2CppObject* __target" };
        var callArgs = new List<string>();
        int start = 0;
        if (!m.IsStatic)
            callArgs.Add($"({m.DeclaringClass.CppStructName}*)__target");
        else if (adapter.Closed)
        {
            // The bound first argument rides the target slot, which is always a
            // Dn2CppObject*: a headerless-typed one arrived there as the wrapper.
            string t0 = CppTypes.Of(ps[0]);
            callArgs.Add(MethodCompiler.IsHeaderlessWrapCpp(t0)
                ? MethodCompiler.HeaderlessUnwrapExpr("__target", t0)
                : $"({t0})__target");
            start = 1;
        }
        else
        {
            sigParams[0] = "[[maybe_unused]] Dn2CppObject* __target";
        }
        for (int i = start; i < ps.Length; i++)
        {
            string real = CppTypes.Of(ps[i]);
            sigParams.Add($"{MethodCompiler.NfiErasedAbi(real)} a{i}");
            callArgs.Add(MethodCompiler.IsHeaderlessWrapCpp(real)
                ? MethodCompiler.HeaderlessUnwrapExpr($"a{i}", real)
                : $"a{i}");
        }
        var rt = m.Signature.ReturnType;
        string realRet = rt.IsVoid ? "void" : CppTypes.Of(rt);
        string ret = rt.IsVoid ? "void" : MethodCompiler.NfiErasedAbi(realRet);
        string call = $"{m.CppName}({string.Join(", ", callArgs)})";
        if (!rt.IsVoid && MethodCompiler.IsHeaderlessWrapCpp(realRet))
            call = MethodCompiler.HeaderlessWrapExpr(call, realRet, rt);
        string head = $"{ret} {adapter.CppName}({string.Join(", ", sigParams)})";
        o.Header.AppendLine($"{head};");
        o.Data.AppendLine(head);
        o.Data.AppendLine("{");
        o.Data.AppendLine(rt.IsVoid ? $"    {call};" : $"    return {call};");
        o.Data.AppendLine("}");
    }

    /// <summary>Per-delegate-type invoker walking the multicast chain
    /// (earlier entries first; the last call's result is returned).</summary>
    private void EmitDelegateInvokers(CppOutput o)
    {
        // The emit-set delegates, then every delegate some emitted text NAMED an invoker
        // for that the emit set does not carry (a type reached only as a VARIANCE view —
        // `Func<object> fo = fs;` constructs nothing, so the class is not emitted, while
        // its Invoke call site spells dginvoke_* all the same). The extras are sorted by
        // the symbol they define, because recording order is compile order and compile
        // order moves under DN2CPP_SPEC_DRAIN. Dedup is by DEFINED SYMBOL, the linker's own
        // key, so a recorded class whose emitted twin already defines dginvoke_<CppName> is
        // not defined twice.
        //
        // An extra is emitted only when EVERY struct its prototype would spell is declared
        // in this image. That test is exact, not defensive: a compiled body naming
        // dginvoke_<CppName> spells the same receiver cast and the same argument/return
        // renderings itself, so a record whose spellings are undeclared can have NO emitted
        // caller — it is planning-pass residue, and emitting it would introduce exactly the
        // undeclared-identifier failure this pass exists to remove.
        var delegates = EmittedClasses.Where(c => c.IsDelegate).ToList();
        var declaredStructs = EmittedClasses.Select(c => c.CppStructName)
            .ToHashSet(StringComparer.Ordinal);
        bool ExtraSpellable(ClassInfo c)
        {
            if (!StructDeclared(c.CppStructName, declaredStructs))
                return false;
            var inv = c.Methods.FirstOrDefault(m => m.Name == "Invoke");
            if (inv is null)
                return false;
            if (!inv.Signature.ReturnType.IsVoid
                && !StructDeclared(CppTypes.Of(inv.Signature.ReturnType), declaredStructs))
                return false;
            foreach (var p in inv.Signature.ParameterTypes)
                if (!StructDeclared(CppTypes.Of(p), declaredStructs))
                    return false;
            return true;
        }
        var seen = new HashSet<string>(delegates.Select(c => c.CppName), StringComparer.Ordinal);
        delegates.AddRange(_c.DelegateInvokerUses
            .Where(ExtraSpellable)
            .Where(c => seen.Add(c.CppName))
            .OrderBy(c => c.CppName, StringComparer.Ordinal));
        if (delegates.Count == 0)
            return;
        var sb = o.Data;
        // Invokers are called from method bodies (delegate Invoke) in any TU, so each gets
        // external linkage + a header prototype; the (recursive) body stays in the data TU.
        o.Header.AppendLine("// ---- delegate invokers ----");
        sb.AppendLine("// ---- delegate invokers ----");
        foreach (var cls in delegates)
        {
            var invoke = cls.Methods.FirstOrDefault(m => m.Name == "Invoke");
            if (invoke is null)
                continue;
            var ps = invoke.Signature.ParameterTypes;
            string ret = invoke.Signature.ReturnType.IsVoid ? "void" : CppTypes.Of(invoke.Signature.ReturnType);
            var sigParams = new List<string> { $"{cls.CppStructName}* dg" };
            var fnParamTypes = new List<string> { "Dn2CppObject*" };
            var callArgs = new List<string> { "dg->f_target" };
            var fwdArgs = new List<string>();
            for (int i = 0; i < ps.Length; i++)
            {
                string t = CppTypes.Of(ps[i]);
                sigParams.Add($"{t} a{i}");
                // f_method's ABI erases every wrappable headerless position (the
                // NFI trio, Assembly/Module) to Dn2CppObject* (DelegateAdapter):
                // the delegate's own spelling is converted here, the target's in
                // the adapter, and the two derive the erasure independently — the
                // only way they can differ is a headerless spelling against
                // Dn2CppObject*, and both erase to the latter. Without this, a
                // Func<CultureInfo> assigned to a Func<object> — an implicit
                // reference conversion that emits no IL to hook — would read one
                // f_method under two incompatible ABIs.
                fnParamTypes.Add(MethodCompiler.NfiErasedAbi(t));
                callArgs.Add(MethodCompiler.IsHeaderlessWrapCpp(t)
                    ? MethodCompiler.HeaderlessWrapExpr($"a{i}", t, ps[i])
                    : $"a{i}");
                fwdArgs.Add($"a{i}");
            }
            string name = $"dginvoke_{cls.CppName}";
            bool nfiRet = !invoke.Signature.ReturnType.IsVoid && MethodCompiler.IsHeaderlessWrapCpp(ret);
            string fnRet = nfiRet ? "Dn2CppObject*" : ret;
            string fnPtr = $"({fnRet} (*)({string.Join(", ", fnParamTypes)}))dg->f_method";
            var fwd = new List<string> { $"({cls.CppStructName}*)dg->f_prev" };
            fwd.AddRange(fwdArgs);
            o.Header.AppendLine($"{ret} {name}({string.Join(", ", sigParams)});");
            sb.AppendLine($"{ret} {name}({string.Join(", ", sigParams)})");
            sb.AppendLine("{");
            sb.AppendLine($"    if (dg->f_prev != nullptr)");
            sb.AppendLine($"        {name}({string.Join(", ", fwd)});");
            string call = $"({fnPtr})({string.Join(", ", callArgs)})";
            if (nfiRet)
                call = MethodCompiler.HeaderlessUnwrapExpr(call, ret);
            sb.AppendLine(invoke.Signature.ReturnType.IsVoid ? $"    {call};" : $"    return {call};");
            sb.AppendLine("}");
        }
        o.Header.AppendLine();
        sb.AppendLine();
    }

    /// <summary>Reflection delegate binding (MethodInfo.CreateDelegate /
    /// Delegate.CreateDelegate): one signature-deduplicated <c>dgrefl_*</c> trampoline
    /// per distinct delegate-Invoke C++ ABI shape, plus the
    /// {delegate type-info → trampoline} registry the runtime binder scans
    /// (<c>dn2cpp_delegate_create</c>). The trampoline is the bound delegate's
    /// <c>f_method</c>: the multicast invoker calls it with the parked
    /// <c>Dn2CppReflBind</c> node (<c>f_target</c>) first, then the delegate's
    /// concrete arguments; it boxes them per the bound methtab row's parameter
    /// type-infos (mode-shifted for open-instance/closed-static bindings),
    /// dispatches through the row's boxed invoker (<c>dn2cpp_reflbind_invoke</c>)
    /// and converts the boxed result back — the boxing-per-call degradation an
    /// IL2CPP-style runtime accepts for reflection-created delegates. Real rows
    /// emit only when a CreateDelegate intrinsic was reached; the registry symbols
    /// themselves always emit (one null row) so the runtime links.
    /// <para>Emitted after the delegate invokers, in the same TU region as the
    /// other delegate trampolines.</para></summary>
    private void EmitDelegateReflBinds(CppOutput o)
    {
        var sb = o.Data;
        var rows = new List<string>();
        if (_c.NeedsReflectionDelegateBind)
        {
            sb.AppendLine("// ---- reflection delegate-bind trampolines + registry (CreateDelegate) ----");
            var seen = new HashSet<string>(System.StringComparer.Ordinal);
            foreach (var cls in EmittedClasses)
            {
                if (!cls.IsDelegate || IsCanonicalWorld(cls) || IsOpaque(cls))
                    continue;
                var invoke = cls.Methods.FirstOrDefault(m => m.Name == "Invoke");
                if (invoke is null)
                    continue;
                // Decode outside the try, for the reason spelled out at the interface
                // trampolines below: the catch is for the RENDER failing ("this shape
                // cannot be marshalled"), and a signature that will not DECODE is a
                // different failure that must escape.
                invoke.EnsureSignature();
                try
                {
                    rows.Add($"{{ {TypeInfoRef(cls, "reflection delegate-bind registry row")}, (void*)&{EmitDelegateReflTramp(sb, invoke, seen)} }}");
                }
                catch (NotSupportedException e) when (!Compilation.IsMustEscape(e))
                {
                    // An Invoke signature CppTypes cannot render stays unbindable:
                    // no registry row, so CreateDelegate over this type throws the
                    // catchable PlatformNotSupportedException at run time.
                }
            }
        }
        sb.AppendLine(rows.Count == 0
            ? "const Dn2CppDelegateReflEntry dn2cpp_delegate_refl_registry[] = { { nullptr, nullptr } };"
            : $"const Dn2CppDelegateReflEntry dn2cpp_delegate_refl_registry[] = {{ {string.Join(", ", rows)} }};");
        sb.AppendLine($"const int32_t dn2cpp_delegate_refl_registry_count = {rows.Count};");
        sb.AppendLine();
    }

    /// <summary>Emits (once per distinct delegate-Invoke C++ ABI shape) the
    /// reflection-bind trampoline and returns its name. Same shape classification
    /// as <see cref="EmitInvokerThunk"/>: reference-typed parameters/returns
    /// collapse to a uniform pointer, value types keep their exact C++ type. The
    /// per-argument box reads the bound method row's parameter type-info at run
    /// time (<c>ctx->method->parameters[k]</c>), so one shape serves every
    /// delegate class and binding mode sharing it.</summary>
    private string EmitDelegateReflTramp(StringBuilder sb, MethodInfo invoke, HashSet<string> seen)
    {
        var ps = invoke.Signature.ParameterTypes;
        var ret = invoke.Signature.ReturnType;

        // The trampoline IS an f_method, so its signature is spelled in f_method's
        // canonical erased ABI (MethodCompiler.NfiErasedAbi): every wrappable
        // headerless position (the NFI trio, Assembly/Module's const char*) is a
        // Dn2CppObject* here, which is exactly what the delegate invoker's
        // function-pointer cast says and exactly what the boxed reflbind world
        // underneath speaks — the invoker wraps such an argument before the call
        // and unwraps such a return after it, so argv and the returned box need no
        // conversion in between. The erasure makes the two sides one spelling
        // rather than two raw headerless spellings that happen to pun alike.
        static (bool Ptr, string Cpp) Classify(TypeDesc t)
        {
            string cpp = MethodCompiler.NfiErasedAbi(CppTypes.Of(t));
            return (cpp.EndsWith("*"), cpp);
        }

        var key = new StringBuilder();
        var sigParams = new List<string> { "Dn2CppObject* ctx0" };
        for (int i = 0; i < ps.Length; i++)
        {
            var (ptr, cpp) = Classify(ps[i]);
            key.Append(ptr ? "_P" : "_" + CppNaming.Sanitize(cpp));
            sigParams.Add($"{cpp} a{i}");
        }
        string retSig;
        int retKind; // 0 void, 1 pointer, 2 value
        if (ret.IsVoid) { retSig = "void"; key.Append("__r_v"); retKind = 0; }
        else
        {
            var (ptr, cpp) = Classify(ret);
            if (ptr) { retSig = cpp; key.Append("__r_P"); retKind = 1; }
            else { retSig = cpp; key.Append("__r_").Append(CppNaming.Sanitize(cpp)); retKind = 2; }
        }
        string name = "dgrefl" + key;
        if (!seen.Add(name))
        {
            return name;
        }

        sb.AppendLine($"static {retSig} {name}({string.Join(", ", sigParams)})");
        sb.AppendLine("{");
        sb.AppendLine("    auto* ctx = reinterpret_cast<Dn2CppReflBind*>(ctx0);");
        sb.AppendLine("    [[maybe_unused]] const Dn2CppParamInfo* mp = ctx->method->parameters;");
        sb.AppendLine($"    Dn2CppObject* argv[{System.Math.Max(ps.Length + 1, 1)}];");
        sb.AppendLine("    int32_t k = 0;");
        sb.AppendLine("    Dn2CppObject* self = ctx->target;");
        sb.AppendLine("    // Closed-static: the bound first argument leads, no receiver.");
        sb.AppendLine($"    if (ctx->mode == DN2CPP_DGBIND_CLOSED_STATIC) {{ argv[k] = ctx->target; k++; self = nullptr; }}");
        for (int i = 0; i < ps.Length; i++)
        {
            var (ptr, _) = Classify(ps[i]);
            string box = ptr
                ? $"argv[k] = (Dn2CppObject*)a{i}; k++;"
                : $"argv[k] = dn2cpp_box(mp[k].paramType, &a{i}, sizeof(a{i})); k++;";
            if (i == 0)
            {
                // Open-instance: the first delegate argument is the receiver. The
                // binder rejects value-typed receiver shapes, so a value-classified
                // first argument never sees that mode.
                sb.AppendLine(ptr
                    ? $"    if (ctx->mode == DN2CPP_DGBIND_OPEN_INSTANCE) self = (Dn2CppObject*)a0; else {{ {box} }}"
                    : $"    {box}");
            }
            else
            {
                sb.AppendLine($"    {box}");
            }
        }
        switch (retKind)
        {
            case 0:
                sb.AppendLine("    dn2cpp_reflbind_invoke(ctx, self, argv);");
                break;
            case 1:
                sb.AppendLine($"    return ({retSig})dn2cpp_reflbind_invoke(ctx, self, argv);");
                break;
            default:
                sb.AppendLine("    Dn2CppObject* r = dn2cpp_reflbind_invoke(ctx, self, argv);");
                sb.AppendLine($"    {retSig} rv{{}};");
                sb.AppendLine($"    if (r != nullptr) rv = *reinterpret_cast<{retSig}*>(reinterpret_cast<char*>(r) + sizeof(Dn2CppObject));");
                sb.AppendLine("    return rv;");
                break;
        }
        sb.AppendLine("}");
        return name;
    }

    /// <summary>Per-delegate-type machinery for every managed delegate ⇔ raw native
    /// function-pointer conversion: the explicit
    /// <c>Marshal.GetFunctionPointerForDelegate&lt;T&gt;</c> intrinsic AND the implicit
    /// marshal of a delegate-typed P/Invoke parameter (<c>MethodCompiler.Newobj.cs</c>)
    /// both route here. The pointer has no bounded lifetime — the native may store it and
    /// call back much later (a stored collision-filter / error callback) — and the
    /// transpiler cannot statically prove otherwise, so this is the ONLY delegate-to-fnptr
    /// path (a synchronous thread-local slot, valid only while the native call is on the
    /// stack, would be silent UB for a stored pointer). Each delegate type gets a fixed
    /// pool of delegate slots in a file-scope static array (data segment = a Boehm static
    /// root, keeping the parked delegates alive) plus one C-ABI thunk per slot. Every thunk
    /// registers a foreign calling thread with the collector before it reads managed state,
    /// then dispatches through <c>dginvoke_&lt;T&gt;</c> with the native-width ⇔ managed-width
    /// casts the invoker expects. A managed exception is reported and terminated at that C
    /// boundary rather than unwinding through native code. The same delegate instance always
    /// maps to the same thunk (the .NET identity guarantee); a full pool traps loudly rather
    /// than corrupting dispatch. The reverse direction
    /// (<c>Marshal.GetDelegateForFunctionPointer&lt;T&gt;</c>) first checks the pool — a
    /// thunk pointer round-trips to its original parked delegate — and otherwise wraps the
    /// raw native pointer in a fresh delegate whose target boxes the pointer and whose
    /// method is a managed-ABI forwarder casting each argument down to its native width.
    /// <para>Emitted after the delegate invokers the thunks call and before the method
    /// bodies whose intrinsic call sites reference the pool helpers (same TU; the
    /// helpers get header prototypes, the pool + thunks stay internal).</para></summary>
    private void EmitMarshalFnPtrThunks(CppOutput o)
    {
        if (_c.MarshalFnPtrDelegates.Count == 0)
            return;
        const int poolSize = 8;
        var sb = o.Data;
        o.Header.AppendLine("// ---- Marshal delegate <-> function-pointer thunk pools ----");
        sb.AppendLine("// ---- Marshal delegate <-> function-pointer thunk pools ----");
        foreach (var cls in _c.MarshalFnPtrDelegates.OrderBy(c => c.CppName, System.StringComparer.Ordinal))
        {
            var invoke = cls.Methods.FirstOrDefault(m => m.Name == "Invoke");
            if (invoke is null)
                continue;
            var ps = invoke.Signature.ParameterTypes;
            var rt = invoke.Signature.ReturnType;
            // The native-ABI type of each Invoke parameter/return — the emit half of
            // CppTypes.IsCallbackMarshalableType (classifier ⇔ thunk must stay in lockstep):
            // a byref-to-blittable marshals as the same pointer the delegate invoker expects
            // (Of), so the callArg cast below is identity and the native address forwards
            // straight through; a bool as the 4-byte Win32 BOOL (matching the forward-P/Invoke
            // bool policy — the thunk returns a clean 0/1, correct for a native reading a
            // 1-byte bool); a blittable struct by its standard C++ layout; otherwise the
            // precise native width (sub-word integers keep their real size).
            string NativeOf(TypeDesc t, bool isRet) =>
                t.Kind == TypeKind.ByRef ? CppTypes.Of(t)
                : t.Kind == TypeKind.Primitive && t.Primitive == PrimitiveTypeCode.Boolean ? "int32_t"
                : CppTypes.IsBlittableStruct(t) ? t.Class!.CppStructName
                : CppTypes.NativeAbiType(t, isRet)!;
            string nativeRet = rt.IsVoid ? "void" : NativeOf(rt, isRet: true);
            var sigParams = new List<string>();
            var callArgs = new List<string> { "dg" };
            for (int i = 0; i < ps.Length; i++)
            {
                sigParams.Add($"{NativeOf(ps[i], isRet: false)} a{i}");
                callArgs.Add($"({CppTypes.Of(ps[i])})a{i}");
            }
            string pool = $"g_dn2cpp_fnptr_dgs_{cls.CppName}";
            string thunks = $"g_dn2cpp_fnptr_thunks_{cls.CppName}";
            string poolLock = $"g_dn2cpp_fnptr_lock_{cls.CppName}";
            sb.AppendLine($"// {cls.FullName}: {poolSize}-slot delegate pool + per-slot C-ABI thunks.");
            sb.AppendLine($"static DN2CPP_GC_STATIC_ROOT Dn2CppObject* {pool}[{poolSize}];");
            sb.AppendLine($"static std::atomic_flag {poolLock} = ATOMIC_FLAG_INIT;");
            var thunkAddrs = new List<string>();
            for (int i = 0; i < poolSize; i++)
            {
                string invokeCall = $"dginvoke_{cls.CppName}({string.Join(", ", callArgs)})";
                sb.AppendLine($"static {nativeRet} dn2cpp_fnptrtramp_{cls.CppName}_{i}({string.Join(", ", sigParams)})");
                sb.AppendLine("{");
                sb.AppendLine("    dn2cpp_native_callback_prologue();");
                sb.AppendLine($"    auto* dg = ({cls.CppStructName}*){pool}[{i}];");
                sb.AppendLine("    try");
                sb.AppendLine("    {");
                sb.AppendLine(rt.IsVoid ? $"        {invokeCall};" : $"        return ({nativeRet}){invokeCall};");
                sb.AppendLine("    }");
                sb.AppendLine("    catch (Dn2CppException& __ex)");
                sb.AppendLine("    {");
                sb.AppendLine("        dn2cpp_report_boundary_exception(__ex.obj, \"a native delegate callback\");");
                sb.AppendLine("        dn2cpp_fail(\"native delegate callback: unhandled managed exception\");");
                sb.AppendLine("    }");
                sb.AppendLine("}");
                thunkAddrs.Add($"(void*)&dn2cpp_fnptrtramp_{cls.CppName}_{i}");
            }
            sb.AppendLine($"static void* const {thunks}[{poolSize}] = {{ {string.Join(", ", thunkAddrs)} }};");
            o.Header.AppendLine($"void* dn2cpp_fnptr_for_delegate_{cls.CppName}(Dn2CppObject* dg);");
            sb.AppendLine($"void* dn2cpp_fnptr_for_delegate_{cls.CppName}(Dn2CppObject* dg)");
            sb.AppendLine("{");
            sb.AppendLine("    if (dg == nullptr)");
            sb.AppendLine("        dn2cpp_throw_argument_null();");
            sb.AppendLine("    // A callback can arrive on a native executor thread as soon as the pointer");
            sb.AppendLine("    // is published. Enable its collector-registration prologue before publishing.");
            sb.AppendLine("    dn2cpp_enable_native_delegate_callback_gc_registration();");
            sb.AppendLine($"    while ({poolLock}.test_and_set(std::memory_order_acquire)) {{ }}");
            sb.AppendLine("    // Already parked: the same instance returns the same pointer (.NET identity).");
            sb.AppendLine($"    for (int32_t i = 0; i < {poolSize}; i++)");
            sb.AppendLine($"        if ({pool}[i] == dg)");
            sb.AppendLine("        {");
            sb.AppendLine($"            {poolLock}.clear(std::memory_order_release);");
            sb.AppendLine($"            return {thunks}[i];");
            sb.AppendLine("        }");
            sb.AppendLine("    // Serialize scan-and-claim so concurrent marshalling cannot alias two delegates");
            sb.AppendLine("    // onto one thunk. Static roots are rescanned by incremental GC, and each slot");
            sb.AppendLine("    // is immutable after publication, so its store and thunk reads stay raw.");
            sb.AppendLine($"    for (int32_t i = 0; i < {poolSize}; i++)");
            sb.AppendLine($"        if ({pool}[i] == nullptr)");
            sb.AppendLine("        {");
            sb.AppendLine($"            {pool}[i] = dg;");
            sb.AppendLine($"            {poolLock}.clear(std::memory_order_release);");
            sb.AppendLine($"            return {thunks}[i];");
            sb.AppendLine("        }");
            sb.AppendLine($"    {poolLock}.clear(std::memory_order_release);");
            sb.AppendLine($"    dn2cpp_fail(\"Marshal.GetFunctionPointerForDelegate: thunk pool exhausted for "
                + $"{cls.FullName} ({poolSize} distinct function pointers per delegate type per process)\");");
            sb.AppendLine("}");

            // The reverse direction (Marshal.GetDelegateForFunctionPointer<T>): a
            // managed-ABI forwarder over a raw native pointer, plus the wrapper factory.
            string managedRet = rt.IsVoid ? "void" : CppTypes.Of(rt);
            var fwdSigParams = new List<string> { "Dn2CppObject* __target" };
            var fwdNativeParamTypes = new List<string>();
            var fwdCallArgs = new List<string>();
            for (int i = 0; i < ps.Length; i++)
            {
                fwdSigParams.Add($"{CppTypes.Of(ps[i])} a{i}");
                fwdNativeParamTypes.Add(NativeOf(ps[i], isRet: false));
                fwdCallArgs.Add($"({NativeOf(ps[i], isRet: false)})a{i}");
            }
            string fpType = $"{nativeRet} (*)({string.Join(", ", fwdNativeParamTypes)})";
            string fwdCall = $"fp({string.Join(", ", fwdCallArgs)})";
            sb.AppendLine($"static {managedRet} dn2cpp_fpfwd_{cls.CppName}({string.Join(", ", fwdSigParams)})");
            sb.AppendLine("{");
            sb.AppendLine("    // __target is the boxed IntPtr the factory below built; the raw native");
            sb.AppendLine("    // function pointer is its payload (right after the object header).");
            sb.AppendLine($"    auto fp = ({fpType})*(intptr_t*)(__target + 1);");
            sb.AppendLine(rt.IsVoid ? $"    {fwdCall};" : $"    return ({managedRet}){fwdCall};");
            sb.AppendLine("}");
            o.Header.AppendLine($"Dn2CppObject* dn2cpp_delegate_for_fnptr_{cls.CppName}(void* p);");
            sb.AppendLine($"Dn2CppObject* dn2cpp_delegate_for_fnptr_{cls.CppName}(void* p)");
            sb.AppendLine("{");
            sb.AppendLine("    if (p == nullptr)");
            sb.AppendLine("        dn2cpp_throw_argument_null();");
            sb.AppendLine("    // A pointer minted by GetFunctionPointerForDelegate round-trips to the");
            sb.AppendLine("    // ORIGINAL parked delegate (the .NET managed round-trip identity).");
            sb.AppendLine($"    for (int32_t i = 0; i < {poolSize}; i++)");
            sb.AppendLine($"        if ({thunks}[i] == p && {pool}[i] != nullptr)");
            sb.AppendLine($"            return {pool}[i];");
            sb.AppendLine("    // A raw native pointer: wrap it in a fresh delegate over the managed-ABI");
            sb.AppendLine("    // forwarder above. Calls into it from a thread the runtime never registered");
            sb.AppendLine("    // carry the same constraints as any other reverse-pinvoke here.");
            sb.AppendLine("    intptr_t ip = (intptr_t)p;");
            sb.AppendLine($"    auto* dg = ({cls.CppStructName}*)dn2cpp_alloc(sizeof({cls.CppStructName}));");
            sb.AppendLine($"    ((Dn2CppObject*)dg)->type = {TypeInfoRef(cls, "Marshal function-pointer delegate wrapper")};");
            sb.AppendLine("    dg->f_target = dn2cpp_box(&dn2cpp_intptr_type, &ip, sizeof(intptr_t));");
            sb.AppendLine($"    dg->f_method = (void*)&dn2cpp_fpfwd_{cls.CppName};");
            sb.AppendLine("    dg->f_prev = nullptr;");
            sb.AppendLine("    dn2cpp_gc_write_barrier((void*)dg);");
            sb.AppendLine("    return (Dn2CppObject*)dg;");
            sb.AppendLine("}");
        }
        o.Header.AppendLine();
        sb.AppendLine();
    }

    /// <summary>Emits an unboxing thunk for a boxed value type's interface slot:
    /// it takes the object pointer (matching <see cref="FnPtrType"/> for the
    /// interface method, so the dispatch-site cast is identity), offsets past the
    /// box header to the unboxed payload, and forwards to the impl, which expects a
    /// pointer to the bare value layout.</summary>
    /// <summary>Emits one type-switch dispatcher (<c>dn2cpp_gvm_*</c>) per used generic
    /// virtual method instantiation. A generic virtual has no vtable slot, so the call
    /// site routes through this function: it branches on the receiver's concrete type to
    /// the matching override and falls back to the base default. The prototype goes in the
    /// header (callable from any TU); the body goes in the data TU.
    ///
    /// The cases are the ALLOCATED types (<c>Compilation.ReachAllocatedType</c> crosses
    /// every newly allocated type with every registered dispatcher), and under shared
    /// generics the allocated set contains canonical group owners: a shared body's
    /// <c>newobj</c> resolves to the canonical instantiation, so
    /// <c>Enumerable.OrderedIterator&lt;TElement,$CnRef&gt;</c> is allocated exactly as its
    /// real members are. A canonical owner has no <c>ti_</c> — <see cref="SkipsCanonicalMetadata"/>
    /// is the rule, and its reason is precisely why the case is dead: no runtime instance
    /// ever carries a canonical type-info (the shared ctor stamps the REAL one, handed to it
    /// through the rgctx <c>ClassAlloc</c> slot, whose fill marks that real class allocated
    /// and so gives it its own case here). Emitting the branch anyway names a symbol nothing
    /// defines — a GREEN transpile whose C++ compile fails on an undeclared identifier. So
    /// the canonical case is dropped, which loses no dispatch: every receiver that can exist
    /// at run time is a real member, and a real member that can exist is allocated.</summary>
    private void EmitGvmDispatchers(CppOutput o)
    {
        var dispatchers = _c.UsedGvms.ToList();
        if (dispatchers.Count == 0)
            return;
        o.Header.AppendLine("// ---- generic virtual method dispatchers ----");
        o.Data.AppendLine("// ---- generic virtual method dispatchers ----");
        foreach (var disp in dispatchers)
        {
            var gvm = disp.Gvm;
            string ret = gvm.Signature.ReturnType.IsVoid ? "void" : CppTypes.Of(gvm.Signature.ReturnType);
            string name = Compilation.GvmDispatchName(gvm);
            var decl = new List<string> { $"{disp.Decl.CppStructName}* a0" };
            for (int k = 0; k < gvm.Signature.ParameterTypes.Length; k++)
                decl.Add($"{CppTypes.Of(gvm.Signature.ParameterTypes[k])} a{k + 1}");
            string sig = $"{ret} {name}({string.Join(", ", decl)})";
            o.Header.AppendLine($"{sig};");

            // Argument forwarding: cast a0 to the target receiver, pass a1.. cast to the
            // target's parameter types. The slot signature and the target's may disagree in
            // BY-REF-NESS, not just in the underlying type — a by-value generic-virtual slot
            // (a default interface method doing `var temp = obj; WriteObject(ref temp);`)
            // resolves for a concrete type to a by-ref override. Passing the address of our
            // own by-value parameter is exactly what the DIM's `temp` is (a fresh copy whose
            // write-back is discarded), so `&a{k}` bridges the ABI with identical semantics;
            // the mirror case dereferences. Decide on the TypeDesc KIND, never on the
            // rendered `*`: a class-typed value is a C++ pointer too but is NOT a managed
            // by-ref, and must keep the plain cast.
            string ForwardCall(MethodInfo target)
            {
                var ca = new List<string> { $"({target.DeclaringClass.CppStructName}*)a0" };
                for (int k = 0; k < target.Signature.ParameterTypes.Length; k++)
                {
                    var tp = target.Signature.ParameterTypes[k];
                    var sp = gvm.Signature.ParameterTypes[k];
                    string tc = CppTypes.Of(tp);
                    bool targetByRef = tp.Kind == TypeKind.ByRef;
                    bool slotByRef = sp.Kind == TypeKind.ByRef;
                    if (targetByRef && !slotByRef)
                        ca.Add($"({tc})&a{k + 1}");
                    else if (!targetByRef && slotByRef)
                        ca.Add($"({tc})*a{k + 1}");
                    else
                        ca.Add($"({tc})a{k + 1}");
                }
                return $"{target.Emittable.CppName}({string.Join(", ", ca)})";
            }
            string Stmt(MethodInfo target) =>
                ret == "void" ? $"{ForwardCall(target)}; return;" : $"return ({ret}){ForwardCall(target)};";

            o.Data.AppendLine(sig);
            o.Data.AppendLine("{");
            o.Data.AppendLine("    const Dn2CppTypeInfo* __t = ((Dn2CppObject*)a0)->type;");
            // The dispatcher's identity and reach chain — the half of a TypeInfoRef diagnosis
            // the case type cannot carry. A thunk, built once per dispatcher and evaluated
            // only if a case ever names an undefined handle.
            Func<string> caseDetail = () =>
                $"{Compilation.GvmDispatchName(gvm)}, reach {_c.ReachChain(gvm)}";
            // One branch per concrete type whose override differs from the base default;
            // types that don't override fall through to the shared base case.
            foreach (var (type, impl) in disp.Cases
                         .Where(kv => kv.Value != gvm && _c.Reachable.Contains(kv.Value))
                         .OrderBy(kv => kv.Key.CppName, StringComparer.Ordinal))
            {
                // A canonical group owner is allocated but has no type-info and can never
                // BE a receiver's type-info (see this method's doc): drop the dead branch
                // rather than name a symbol nothing defines.
                if (SkipsCanonicalMetadata(type) || IsRuntimeTemplateLevel(type))
                    continue;
                o.Data.AppendLine($"    if (__t == {TypeInfoRef(type, "generic-virtual dispatcher case", caseDetail)}) {{ {Stmt(impl)} }}");
            }
            // Base default: the GVM's own (declaring-type) implementation. When it has no
            // body (an abstract generic virtual), every concrete type must have overridden
            // it, so the fallback is unreachable — trap rather than link a missing symbol.
            if (gvm.Rva != 0 && _c.Reachable.Contains(gvm))
                o.Data.AppendLine($"    {Stmt(gvm)}");
            else
            {
                o.Data.AppendLine("    __builtin_trap();");
                if (ret != "void")
                    // A by-value struct return cannot take a C-style cast from 0
                    // ((t_Foo)0 has no matching conversion); value-init it via the
                    // shared helper. Pointer/scalar returns keep the (T)0 form
                    // byte-for-byte (ZeroInit yields "{}" only for the aggregate
                    // case — Of never renders a sub-word scalar as a return type).
                    o.Data.AppendLine(CppTypes.ZeroInit(ret) == "{}"
                        ? $"    return {CppTypes.ZeroInitExpr(ret)};"
                        : $"    return ({ret})0;");
            }
            o.Data.AppendLine("}");
        }
        o.Header.AppendLine();
        o.Data.AppendLine();
    }

    /// <summary>The C++ return types that are handed back in registers, so the emitted
    /// signature's first parameter really is its first argument. Everything else is a
    /// struct, and a struct MAY be returned indirectly — which on x86-64 SysV and Win64
    /// means a hidden result pointer occupying the first integer argument register.</summary>
    private static readonly HashSet<string> ScalarReturns = new(System.StringComparer.Ordinal)
    {
        "void", "bool", "int32_t", "uint32_t", "int64_t", "uint64_t",
        "float", "double", "intptr_t", "uintptr_t",
    };

    /// <summary>Whether a slot with this return type is entered with the RECEIVER in
    /// argument 0 — the precondition for a trap that reads the receiver's type name.
    ///
    /// <para>True for void, a scalar, or any pointer (every managed reference lowers to
    /// one). False for a by-value struct return, conservatively and on every target: the
    /// struct may be returned indirectly, and where it is, argument 0 holds the caller's
    /// result buffer rather than the receiver. Over-classifying a small register-returned
    /// struct as unsafe costs a name in an abort message; under-classifying a large one
    /// costs a second crash inside the first, with the diagnosis lost. So: pointers and
    /// scalars only.</para></summary>
    private static bool ReceiverIsFirstArg(TypeDesc ret)
    {
        string c = ret.IsVoid ? "void" : CppTypes.Of(ret);
        return c.EndsWith("*", System.StringComparison.Ordinal) || ScalarReturns.Contains(c);
    }

    // ---- per-signature dispatch-trap thunks -------------------------------------
    //
    // A trap in a dispatch slot must carry the slot's exact C++ signature: a wasm
    // call_indirect checks the callee's type immediate before entering it, so the shared
    // void()-shaped trap symbols die there as an anonymous "function signature mismatch"
    // where the same miss natively reaches the named abort. Each thunk enters through
    // the slot's own signature and forwards the receiver — argument 0 at the C++ level
    // for every return shape, the compiler owning any hidden struct-return buffer — to
    // the receiver-reading reporter. Deduplicated on ABI shape across the whole emission
    // (pointers collapse to void*, value types stay exact — the invoker-thunk
    // classification) and defined inline in generated.h, so every TU shares one
    // definition and one address. A vtable thunk passes its own address for the
    // candidate scan, and the set is registered at init (EmitInitCalls) so
    // dn2cpp_exception_message can recognise a trap without calling it.
    private readonly HashSet<string> _slotTrapThunks = new(StringComparer.Ordinal);
    private readonly List<string> _vcallTrapThunks = new();
    private StringBuilder? _trapThunkHeader;
    private HashSet<string>? _declaredStructNames;
    private IReadOnlySet<string> DeclaredStructNames =>
        _declaredStructNames ??= EmittedClasses.Select(c => c.CppStructName)
            .ToHashSet(StringComparer.Ordinal);

    /// <summary>The dispatch signature of the slot <paramref name="decl"/> declares,
    /// rendered for a trap: the shape key that dedups it, the return type, and the
    /// parameter types with the receiver as leading <c>void*</c>. Null when the slot
    /// cannot carry an exact-signature trap — a static (no receiver), a shape
    /// <see cref="CppTypes"/> cannot render, or a by-value type this image never
    /// declared — and the caller degrades to the signature-less historical form.</summary>
    private (string Key, string Ret, List<string> ParamTypes)? SlotTrapShape(MethodInfo decl)
    {
        if (decl.IsStatic)
            return null;
        try
        {
            var key = new StringBuilder();
            var ps = new List<string> { "void*" };
            foreach (var p in decl.Signature.ParameterTypes)
            {
                string cpp = CppTypes.Of(p);
                if (cpp.EndsWith("*", StringComparison.Ordinal))
                {
                    key.Append("_P");
                    ps.Add("void*");
                }
                else
                {
                    if (!StructDeclared(cpp, DeclaredStructNames))
                        return null;
                    key.Append('_').Append(CppNaming.Sanitize(cpp));
                    ps.Add(cpp);
                }
            }
            string ret;
            var rt = decl.Signature.ReturnType;
            if (rt.IsVoid)
            {
                ret = "void";
                key.Append("__r_v");
            }
            else
            {
                string cpp = CppTypes.Of(rt);
                if (cpp.EndsWith("*", StringComparison.Ordinal))
                {
                    ret = "void*";
                    key.Append("__r_P");
                }
                else
                {
                    if (!StructDeclared(cpp, DeclaredStructNames))
                        return null;
                    ret = cpp;
                    key.Append("__r_").Append(CppNaming.Sanitize(cpp));
                }
            }
            return (key.ToString(), ret, ps);
        }
        catch (NotSupportedException e) when (!Compilation.IsMustEscape(e))
        {
            return null;
        }
    }

    /// <summary>The per-signature trap thunk for the unreached dispatch slot
    /// <paramref name="decl"/> declares — <c>itftrap_*</c> for an interface slot,
    /// <c>vtrap_*</c> for a vtable slot — or null when the shape cannot be rendered
    /// (see <see cref="SlotTrapShape"/>). The thunk body never returns: the reporter
    /// aborts, so a non-void return type needs no value.</summary>
    internal string? SlotTrapThunk(MethodInfo decl, bool vcall)
    {
        if (SlotTrapShape(decl) is not { } shape)
            return null;
        string name = (vcall ? "vtrap" : "itftrap") + shape.Key;
        if (_slotTrapThunks.Add(name))
        {
            var ps = new List<string> { "void* self" };
            ps.AddRange(shape.ParamTypes.Skip(1));
            string body = vcall
                ? $"dn2cpp_vcall_unimplemented_at((Dn2CppObject*)self, (const void*)&{name});"
                : "dn2cpp_itf_slot_missing(self);";
            _trapThunkHeader!.AppendLine($"inline {shape.Ret} {name}({string.Join(", ", ps)}) {{ {body} }}");
            if (vcall)
                _vcallTrapThunks.Add(name);
        }
        return name;
    }

    /// <summary>Renders the definition of the named trap stub <paramref name="name"/>
    /// for an unreached dispatch slot: <paramref name="reporter"/> (a <c>*_named</c>
    /// runtime abort) with <paramref name="desc"/> baked in. Carries the slot's exact
    /// C++ signature when it renders — the wasm type-immediate rule above — and
    /// degrades to the historical <c>void()</c> form when it cannot.</summary>
    internal string NamedSlotMissStubDef(string name, string reporter, string desc, MethodInfo decl)
    {
        string lit = desc.Replace("\\", "\\\\").Replace("\"", "\\\"");
        string sig = SlotTrapShape(decl) is { } s
            ? $"{s.Ret} {name}({string.Join(", ", s.ParamTypes)})"
            : $"void {name}()";
        return $"[[maybe_unused]] static {sig} {{ {reporter}(\"{lit}\"); }}";
    }

    private static void EmitUnboxingThunk(StringBuilder sb, string thunk, ClassInfo cls, MethodInfo im, MethodInfo impl)
    {
        string ret = im.Signature.ReturnType.IsVoid ? "void" : CppTypes.Of(im.Signature.ReturnType);
        var ps = im.Signature.ParameterTypes;
        var decl = new List<string> { $"{im.DeclaringClass.CppStructName}* o" };
        for (int k = 0; k < ps.Length; k++)
            decl.Add($"{CppTypes.Of(ps[k])} a{k}");
        var implPs = impl.Emittable.Signature.ParameterTypes;
        // A static-abstract interface member (generic-math/SIMD operators like
        // IAdditionOperators.op_Addition, INumber.Min/Max, ISimdVector.get_Zero) is
        // implemented by a *static* method with no receiver, so the unboxed `this`
        // pointer must not be prepended — only the real operands are forwarded. The
        // slot stays populated (index alignment is preserved) but the boxed receiver
        // `o` is ignored, which is correct: a static member never reads the instance.
        var callArgs = new List<string>();
        if (!impl.Emittable.IsStatic)
            callArgs.Add($"({cls.CppStructName}*)((Dn2CppObject*)o + 1)");
        for (int k = 0; k < ps.Length; k++)
            callArgs.Add(NfiSlotArg(implPs[k], $"a{k}"));
        string body = $"{impl.Emittable.CppName}({string.Join(", ", callArgs)})";
        string call = im.Signature.ReturnType.IsVoid ? $"{body};" : $"return ({ret}){body};";
        sb.AppendLine($"static {ret} {thunk}({string.Join(", ", decl)}) {{ {call} }}");
    }

    /// <summary>One forwarded argument of an interface-slot thunk, cast to the
    /// implementation's spelling. A headerless intrinsic parameter
    /// (CultureInfo/NumberFormatInfo/TextInfo — the <c>const Dn2CppNumberFormatInfo*</c>
    /// lowering) is UNWRAPPED rather than cast: the caller of a slot whose canonical
    /// interface erases the position hands over the interned wrapper
    /// (<see cref="MethodCompiler.NfiWrapErasedCallArgs"/>), and a concrete
    /// implementation body means the raw struct pointer. The unwrap is tolerant, so a
    /// caller that passed the raw pointer is unaffected — which is what makes it safe on
    /// a slot table shared between the closed interface row and its canonical alias
    /// row.</summary>
    private static string NfiSlotArg(TypeDesc implParam, string arg)
    {
        string cpp = CppTypes.Of(implParam);
        return MethodCompiler.IsHeaderlessWrapCpp(cpp)
            ? MethodCompiler.HeaderlessUnwrapExpr(arg, cpp)
            : $"({cpp}){arg}";
    }

    /// <summary>The interface-slot thunk a REFERENCE-typed class needs when its
    /// concrete implementation spells a headerless intrinsic parameter that the
    /// interface's CANONICAL form erases — or null when the slot can hold the
    /// implementation symbol directly (the overwhelmingly common case, and the
    /// case whose emitted bytes must not move).
    /// <para>The asymmetry this closes: a call through
    /// <c>IHolder&lt;CultureInfo&gt;</c> wraps its NFI arguments because the canonical
    /// <c>IHolder&lt;__Canon&gt;</c> reads the position as a managed object — right for
    /// a grouped generic implementer, whose bound body IS the shared canonical one, and
    /// wrong for a NON-generic implementer, whose body takes the raw
    /// <c>const Dn2CppNumberFormatInfo*</c> and reads the wrapper's header as culture
    /// fields. That failure is silent: no crash, just an empty
    /// <c>CultureInfo.Name</c>. The test is on the ABI actually in the slot
    /// (<c>impl.Emittable</c>), not on the interface signature, so an implementer bound
    /// to a shared donor — which already expects the wrapper — keeps its bare
    /// symbol.</para></summary>
    private string? NfiErasedSlotThunk(StringBuilder sb, ClassInfo cls, ClassInfo itf,
                                       MethodInfo im, MethodInfo impl, int slotIndex)
    {
        var citf = _c.CanonicalInterfaceOf(itf);
        if (citf is null)
            return null;
        var cim = citf.Methods.FirstOrDefault(m => m.Name == im.Name && m.VtableSlot == im.VtableSlot);
        if (cim is null)
            return null;
        var ps = im.Signature.ParameterTypes;
        var cps = cim.Signature.ParameterTypes;
        var implPs = impl.Emittable.Signature.ParameterTypes;
        if (ps.Length != cps.Length || ps.Length != implPs.Length)
            return null;
        bool any = false;
        for (int k = 0; k < ps.Length && !any; k++)
            any = MethodCompiler.IsHeaderlessWrapCpp(CppTypes.Of(implPs[k]))
                  && CppTypes.Of(cps[k]) == "Dn2CppObject*";
        if (!any)
            return null;
        string thunk = $"nfithunk_{cls.CppName}_{itf.CppName}_{slotIndex}";
        string ret = im.Signature.ReturnType.IsVoid ? "void" : CppTypes.Of(im.Signature.ReturnType);
        var decl = new List<string> { $"{im.DeclaringClass.CppStructName}* o" };
        for (int k = 0; k < ps.Length; k++)
            decl.Add($"{CppTypes.Of(ps[k])} a{k}");
        var callArgs = new List<string>();
        if (!impl.Emittable.IsStatic)
            callArgs.Add($"({cls.CppStructName}*)o");
        for (int k = 0; k < ps.Length; k++)
            callArgs.Add(NfiSlotArg(implPs[k], $"a{k}"));
        string body = $"{impl.Emittable.CppName}({string.Join(", ", callArgs)})";
        string call = im.Signature.ReturnType.IsVoid ? $"{body};" : $"return ({ret}){body};";
        sb.AppendLine($"static {ret} {thunk}({string.Join(", ", decl)}) {{ {call} }}");
        return thunk;
    }

    /// <summary>Emit the native-layout marshalling struct (<c>tn_&lt;Name&gt;</c>) and the
    /// field-by-field marshal-in / marshal-out helpers for every non-blittable but
    /// marshalable value struct a P/Invoke passes by value or by ref. The managed
    /// <c>t_&lt;Name&gt;</c> layout (already emitted by EmitStructs) and the native
    /// <c>tn_&lt;Name&gt;</c> differ field for field, so the helpers translate between them:
    /// a string field is encoded to / decoded from a NUL-terminated buffer (UTF-8 or UTF-16
    /// per the struct's <c>[StructLayout].CharSet</c>, fixed per struct type), a bool to/from
    /// the 4-byte Win32 BOOL, a sub-word integer to/from its real native width, and a nested
    /// blittable struct copied as-is. The marshal-out string write-back frees a pointer only
    /// when the native REPLACED our [In] buffer (which is GC memory) — the same discipline as
    /// a byref string, keyed off the IN snapshot.</summary>
    internal void EmitPInvokeMarshalStructs(CppOutput o)
    {
        if (_c.PInvokeMarshalStructs.Count == 0)
            return;
        // The native-layout struct (tn_<Name>) and the marshal-in/out helpers are
        // referenced by method bodies at P/Invoke call sites in any TU: the struct type and
        // the helper prototypes go in the header, the helper bodies in the data TU.
        o.Header.AppendLine("// ---- P/Invoke non-blittable struct marshalling ----");
        o.Data.AppendLine("// ---- P/Invoke non-blittable struct marshalling ----");
        foreach (var cls in _c.PInvokeMarshalStructs.OrderBy(c => c.CppName, StringComparer.Ordinal))
        {
            bool unicode = StructCharSetUnicode(cls);
            var fields = cls.Fields.Where(f => !f.IsStatic && !f.IsLiteral).ToList();
            string tn = CppTypes.MarshalStructName(cls);
            string tm = cls.CppStructName;

            // The native-layout struct: each field at its native-ABI width. A
            // [MarshalAs(ByValArray, SizeConst=N)] field embeds its elements inline as a
            // fixed-length C array (<elem>[N]) — exactly the native struct's layout.
            // [StructLayout(Pack)] caps every field's alignment here exactly as it does in
            // the managed layout: this struct IS the unmanaged one, so a packed struct whose
            // native mirror was laid out at natural alignment would have marshalled into the
            // wrong offsets — silently, since nothing but the native library could tell.
            bool tnPacked = cls.LayoutPack > 0;
            if (tnPacked)
                o.Header.AppendLine($"#pragma pack(push, {cls.LayoutPack})");
            o.Header.AppendLine($"struct {tn}");
            o.Header.AppendLine("{");
            foreach (var f in fields)
                o.Header.AppendLine(CppTypes.IsByValArrayField(f)
                    ? $"    {CppTypes.ByValArrayElemNative(f)} {f.CppName}[{f.ByValArraySize}];"
                    : $"    {CppTypes.PInvokeStructFieldNativeType(f.Type)!} {f.CppName};");
            o.Header.AppendLine("};");
            if (tnPacked)
                o.Header.AppendLine("#pragma pack(pop)");
            // ---- the marshalled-layout model's cross-check ----
            //
            // tn_<Name> IS the marshalled layout, laid out by the C++ compiler;
            // Compilation.MarshalLayout.cs computes the same layout by arithmetic over the
            // metadata rows, and Marshal.SizeOf / Marshal.OffsetOf answer from THAT. The
            // two must agree, and the toolchain says so for free at build time, naming the
            // type — a drift would otherwise surface as a wrong NUMBER, which is worse than
            // a refusal. Asserted only where the model HAS an answer: a shape it declines is
            // a declared divergence, visible as the absence of the assert.
            //
            // A pointer-bearing marshalled struct is legitimately smaller on a 32-bit target,
            // so each number is written in sizeof(void*) and the assert is UNGUARDED: it IS
            // the check at 32-bit width, re-evaluated by the C++ compiler against the real
            // pointer size, which is what makes every wasm32 link a proof of the model over
            // the whole corpus. Only an assert the model could pin at 64 bits alone states
            // that premise (`sizeof(void*) != 8 ||`) — per assert, never shared.
            if (_c.MarshalMemberLayoutText(cls) is { } mt)
            {
                o.Header.AppendLine(MarshalLayoutAssert(
                    mt.Size, $"sizeof({tn})", $"marshalled size of {cls.FullName}"));
                foreach (var f in fields)
                    if (mt.Offsets.TryGetValue(f, out var mo))
                        o.Header.AppendLine(MarshalLayoutAssert(
                            mo, $"offsetof({tn}, {f.CppName})",
                            $"marshalled offset of {cls.FullName}.{f.Name}"));
            }
            o.Header.AppendLine($"void dn2cpp_marshalin_{cls.CppName}({tm}* src, {tn}* dst);");
            o.Header.AppendLine($"void dn2cpp_marshalout_{cls.CppName}({tn}* src, {tm}* dst, {tn}* insnap);");

            // marshal-in: managed -> native (string -> GC-allocated NUL-terminated buffer).
            o.Data.AppendLine($"void dn2cpp_marshalin_{cls.CppName}({tm}* src, {tn}* dst)");
            o.Data.AppendLine("{");
            foreach (var f in fields)
                o.Data.AppendLine("    " + MarshalInField(f, unicode));
            o.Data.AppendLine("}");

            // marshal-out: native -> managed (string decoded; a native-replaced pointer freed).
            o.Data.AppendLine($"void dn2cpp_marshalout_{cls.CppName}({tn}* src, {tm}* dst, {tn}* insnap)");
            o.Data.AppendLine("{");
            foreach (var f in fields)
            {
                o.Data.AppendLine("    " + MarshalOutField(f, unicode));
                if (f.Type.ContainsGcReferences())
                    o.Data.AppendLine("    dn2cpp_gc_write_barrier_if_heap((void*)dst);");
            }
            o.Data.AppendLine("}");
        }
        o.Header.AppendLine();
        o.Data.AppendLine();
    }

    /// <summary>One cross-check of the marshalled-layout model against the C++ compiler's own
    /// layout of <c>tn_&lt;Name&gt;</c>.</summary>
    private static string MarshalLayoutAssert(ModeledSize m, string measured, string what) =>
        $"static_assert({(m.Guarded ? "sizeof(void*) != 8 || " : "")}{measured} == {m.Text}, "
        + $"\"{what} disagrees with Compilation.MarshalLayout\");";

    /// <summary>The marshal-in statement for one struct field (managed src -> native dst).</summary>
    private static string MarshalInField(FieldInfo f, bool unicode)
    {
        string fn = f.CppName;
        var ft = f.Type;
        if (CppTypes.IsByValArrayField(f))
            // Inline fixed array (managed array ref -> native <elem>[N]): copy the managed
            // elements into the inline slots (truncating a longer array, throwing on a
            // shorter one); a null array zeroes the slots — exactly real .NET's
            // ByValArray copy-in. The managed array's packed stride equals the native
            // element width for a blittable element, so the copy is a memcpy.
            return $"dn2cpp_pinvoke_byvalarr_in(dst->{fn}, {f.ByValArraySize}, "
                + $"sizeof({CppTypes.ByValArrayElemNative(f)}), "
                + $"src->{fn} == nullptr ? nullptr : (const void*)src->{fn}->data, "
                + $"src->{fn} == nullptr ? 0 : src->{fn}->length);";
        if (ft.IsString)
            // The encoder allocates a GC buffer; it stays rooted across the [In] call via
            // the native struct local on the conservatively scanned stack. A struct's
            // CharSet is binary (Unicode = UTF-16, else the default Ansi = the host code
            // page on Windows / UTF-8 on Unix); struct fields have no LPUTF8Str form.
            return $"dst->{fn} = (void*)({(unicode ? "dn2cpp_pinvoke_str_to_utf16" : "dn2cpp_pinvoke_str_to_ansi")}(src->{fn}));";
        if (ft.Kind == TypeKind.Primitive && ft.Primitive == PrimitiveTypeCode.Boolean)
            return $"dst->{fn} = (src->{fn} != 0) ? 1 : 0;";
        if (CppTypes.IsBlittableStruct(ft))
            return $"dst->{fn} = src->{fn};";
        return $"dst->{fn} = ({CppTypes.PInvokeStructFieldNativeType(ft)!})(src->{fn});";
    }

    /// <summary>The marshal-out statement for one struct field (native src -> managed dst).</summary>
    private static string MarshalOutField(FieldInfo f, bool unicode)
    {
        string fn = f.CppName;
        var ft = f.Type;
        if (CppTypes.IsByValArrayField(f))
        {
            // Inline fixed array (native <elem>[N] -> managed array ref): real .NET
            // allocates a FRESH managed array of N elements on copy-back (never reusing the
            // input), so allocate one in the field's rep and memcpy the inline slots into it.
            // The array is a genuine T[] to the caller, so it carries the element's precise
            // handle rather than the untyped allocator's System.Object[]. The symbol
            // is guaranteed by MethodCompiler.NotePInvokeMarshalStruct, which notes every
            // ByValArray element when the struct joins PInvokeMarshalStructs — this emitter
            // runs before EmitTypeInfos, so it can neither ask nor note here.
            string elemTi = MethodCompiler.PreciseArrayTypeInfoExprOf(f.Type.Element!);
            return CppTypes.ByValArrayRepIsI4(f)
                ? $"dst->{fn} = dn2cpp_pinvoke_byvalarr_out_i4((const void*)src->{fn}, {f.ByValArraySize}, {elemTi});"
                : $"dst->{fn} = dn2cpp_pinvoke_byvalarr_out_n((const void*)src->{fn}, {f.ByValArraySize}, "
                    + $"sizeof({CppTypes.ByValArrayElemNative(f)}), {elemTi});";
        }
        if (ft.IsString)
            // Decode the current native pointer; free it only when the native replaced our
            // [In] buffer (result != insnap) — our buffer is GC memory, never free()d.
            return $"dst->{fn} = dn2cpp_pinvoke_byref_str_result(src->{fn}, insnap->{fn}, {(unicode ? 1 : 0)});";
        if (ft.Kind == TypeKind.Primitive && ft.Primitive == PrimitiveTypeCode.Boolean)
            return $"dst->{fn} = (src->{fn} != 0) ? 1 : 0;";
        if (CppTypes.IsBlittableStruct(ft))
            return $"dst->{fn} = src->{fn};";
        return $"dst->{fn} = ({CppTypes.Of(ft)})(src->{fn});";
    }

    /// <summary>Whether a struct's <c>[StructLayout].CharSet</c> selects UTF-16 (Unicode)
    /// rather than UTF-8 (Ansi/default/Auto) for its char and string fields — the metadata
    /// StringFormat on the TypeDef, which is what real .NET uses to marshal those fields
    /// inside a struct (independent of the importing method's CharSet), defaulting to
    /// Ansi.
    ///
    /// <para>Reads the bit decoded at shape time (<see cref="ClassInfo.LayoutCharSetUnicode"/>)
    /// rather than re-reading the TypeDef here: the marshalled-size model
    /// (<c>Compilation.MarshalLayout.cs</c>) answers <c>Marshal.SizeOf</c> for the same
    /// structs this emitter lays out, and a size that disagrees with the bytes
    /// <c>tn_&lt;Name&gt;</c> actually uses is a silent wrong number. Two readers of one
    /// metadata bit is how that disagreement arrives, so there is one.</para></summary>
    private static bool StructCharSetUnicode(ClassInfo cls) => cls.LayoutCharSetUnicode;

    /// <summary>Declare each native entry point a P/Invoke call lowered to.
    /// One <c>extern "C"</c> declaration per distinct entry-point symbol, named with a
    /// generated alias bound to the real symbol either through a platform-aware <c>asm</c>
    /// label (<c>DN2CPP_PINVOKE_ASM</c>, GNU/Clang — including clang-cl, which still defines
    /// <c>__clang__</c>) or, under real <c>cl.exe</c> (which cannot compile an <c>asm</c>-label
    /// declaration at all), by declaring only the alias and binding it to the real symbol at
    /// link time via an <c>/alternatename</c> linker directive — see
    /// <see cref="EmitPInvokeDecl"/>. Deterministic order (sorted by alias).</summary>
    private void EmitPInvokeDeclarations(StringBuilder sb)
    {
        if (_c.PInvokeCalls.Count == 0)
            return;
        // Dedupe by alias (one declaration per native symbol). Two P/Invoke methods
        // binding the same entry point share one extern; their blittable signatures
        // are expected to agree (a mismatch is a user error → C++ redeclaration).
        // A plain Dictionary, not SortedDictionary: dn2cpp cannot transpile
        // SortedDictionary's ValueCollection.CopyTo when building itself (it delegates to
        // InOrderTreeWalk via an ldftn of an external method). Output is byte-identical —
        // same last-write-wins dedupe, then the Ordinal-ascending OrderBy below.
        var decls = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var m in _c.PInvokeCalls)
        {
            var pinv = m.PInvoke!;
            // A char's native width follows the importing method's CharSet (1-byte Ansi /
            // 2-byte Unicode); EmitPInvokeCall reads the same flag so the extern decl and
            // the call agree.
            bool charUnicode = (pinv.ImportAttributes
                    & System.Reflection.MethodImportAttributes.CharSetMask)
                == System.Reflection.MethodImportAttributes.CharSetUnicode;
            // A per-parameter / return [MarshalAs(UnmanagedType.*)] override can
            // change the native width / encoding; the decl reads the same overrides as the
            // call site (EmitPInvokeCall) so the extern type and the call agree.
            string ret = CppTypes.PInvokeNativeType(
                m.Signature.ReturnType, isReturn: true, charUnicode, pinv.ReturnMarshalAs)!;
            var ps = m.Signature.ParameterTypes.Select((p, i) => CppTypes.PInvokeNativeType(
                p, charUnicode: charUnicode,
                marshalAs: pinv.ParamMarshalAs.TryGetValue(i, out var u)
                    ? u
                    : (System.Runtime.InteropServices.UnmanagedType?)null)!);
            string alias = pinv.CppAlias(m.Signature);
            decls[alias] = EmitPInvokeDecl(pinv, alias, ret, string.Join(", ", ps));
        }
        sb.AppendLine("// ---- P/Invoke native entry points ----");
        foreach (var kv in decls.OrderBy(kv => kv.Key, StringComparer.Ordinal))
            sb.AppendLine(kv.Value);
        sb.AppendLine();
    }

    /// <summary>Program-wide fixup cells for lazy imported MethodDefs and their optional
    /// address-taken forwarders. Inline variables keep the header usable from split body
    /// TUs while stable names and a sorted walk make the emitted bytes independent of
    /// call-site discovery order.</summary>
    private void EmitLazyPInvokeCaches(StringBuilder sb)
    {
        if (_c.LazyPInvokeCaches.Count == 0)
            return;
        sb.AppendLine("// ---- lazy P/Invoke method caches ----");
        foreach (string cache in _c.LazyPInvokeCaches.OrderBy(n => n, StringComparer.Ordinal))
            sb.AppendLine($"inline std::atomic<void*> {cache}{{nullptr}};");
        sb.AppendLine();
    }

    /// <summary>Whether <paramref name="s"/> is a bare C identifier (ASCII letter/underscore
    /// first, then letters/digits/underscores) — real Win32/libc P/Invoke entry points always
    /// are. A .NET ordinal import (<c>EntryPoint = "#42"</c>) is the one documented shape
    /// that is not, and is out of scope under either compiler: it would need
    /// <c>GetProcAddress</c>-by-ordinal dynamic binding, not a link-time
    /// declaration.</summary>
    private static bool IsBareCIdentifier(string s)
    {
        // Indexed loop, not string LINQ — this body runs natively in the
        // self-hosted transpiler and per P/Invoke decl, so it stays
        // allocation-free (string LINQ itself dispatches fine these days).
        if (s.Length == 0 || !(char.IsAsciiLetter(s[0]) || s[0] == '_'))
            return false;
        for (int i = 1; i < s.Length; i++)
        {
            if (!char.IsAsciiLetterOrDigit(s[i]) && s[i] != '_')
                return false;
        }
        return true;
    }

    /// <summary>Emit one P/Invoke <c>extern "C"</c> declaration, dual-arm on target compiler.
    /// The GNU/Clang arm (also clang-cl, which defines <c>__clang__</c>) is an <c>asm</c>-label
    /// alias. Real <c>cl.exe</c> has no asm-label extension, so its arm declares <em>only</em>
    /// the generated alias (with the lowered/marshalled signature) and binds it to the real
    /// <paramref name="pinv"/>.EntryPoint symbol at link time via an
    /// <c>/alternatename:alias=EntryPoint</c> directive. x64 <c>extern "C"</c> carries no name
    /// decoration, so both names in the directive are exactly their link-time spelling.
    /// Because the real EntryPoint is never redeclared under its own name, it cannot collide
    /// with a CRT header's own prototype of the same symbol, and two DllImports sharing an
    /// EntryPoint with different marshalled signatures cannot collide either (aliases are
    /// deduped by <c>CppAlias</c>, and each gets its own declaration + directive).</summary>
    private static string EmitPInvokeDecl(PInvokeInfo pinv, string alias, string ret, string paramList)
    {
        string clangDecl =
            $"extern \"C\" {ret} {alias}({paramList}) DN2CPP_PINVOKE_ASM(\"{pinv.EntryPoint}\");";
        if (!IsBareCIdentifier(pinv.EntryPoint))
            // Out-of-scope shape (see IsBareCIdentifier) — fail loud at MSVC compile time
            // instead of emitting an invalid declaration token; the GNU/Clang arm is left
            // exactly as it always was (may or may not actually link, unchanged either way).
            return "#if defined(_MSC_VER) && !defined(__clang__)\n"
                + $"#error \"P/Invoke entry point '{pinv.EntryPoint}' is not a plain C "
                + "identifier (e.g. an ordinal import); unsupported under MSVC direct-"
                + "declaration binding — see the Windows P/Invoke notes in docs/STATUS.md.\"\n"
                + "#else\n"
                + clangDecl + "\n"
                + "#endif";
        // MSVC arm: declare ONLY the alias — never redeclare the real EntryPoint symbol
        // under its own name (that collides both with a CRT header's own prototype of the
        // same symbol and with a second DllImport marshalling it differently). The
        // /alternatename directive resolves the otherwise-undefined alias to whatever
        // exports EntryPoint; `#pragma comment(linker, ...)` appends it to this TU's
        // .drectve section, so no CMake change is needed.
        return "#if defined(_MSC_VER) && !defined(__clang__)\n"
            + $"extern \"C\" {ret} {alias}({paramList});\n"
            + $"#pragma comment(linker, \"/alternatename:{alias}={pinv.EntryPoint}\")\n"
            + "#else\n"
            + clangDecl + "\n"
            + "#endif";
    }

    private void EmitStructs(StringBuilder sb)
    {
        sb.AppendLine("// ---- managed type layouts ----");
        _renderedStructRefs.Clear();
        // A grouped specialization's layout IS its canonical owner's
        // (CppStructName redirects), so the alias defines no struct of its own —
        // the owner's single definition serves the whole group.
        static bool IsStructAlias(ClassInfo c) =>
            ClassInfo.ShareStructLayout && c.SharedOwner is not null;
        // The class whose emitted struct definition carries c's layout.
        static ClassInfo StructCarrier(ClassInfo c) => IsStructAlias(c) ? c.SharedOwner! : c;
        static bool InheritsFromWaitHandle(ClassInfo c)
        {
            for (var b = c; b is not null; b = b.BaseClass)
            {
                if (b.FullName == "System.Threading.WaitHandle")
                    return true;
                if (b.BaseClass is null
                    && b.ExternalBaseName is "System.Threading.WaitHandle"
                        or "System.Threading.EventWaitHandle")
                    return true;
            }
            return false;
        }
        // Interfaces are emitted as empty structs (below) and can be pointer-field
        // types (e.g. Dictionary._comparer : IEqualityComparer<TKey>), so they need
        // forward declarations too, ahead of any struct that references them.
        // Deduped by name: a whole alias group forward-declares one struct.
        var fwdDeclared = new HashSet<string>(System.StringComparer.Ordinal);
        foreach (var cls in EmittedClasses)
        {
            if (!cls.IsEnum && fwdDeclared.Add(cls.CppStructName))
                sb.AppendLine($"struct {cls.CppStructName};");
        }
        sb.AppendLine();

        // Value types first (they appear by value inside other layouts),
        // ordered so struct fields are complete before use.
        var valueTypes = EmittedClasses.Where(c => c.IsValueType && !IsStructAlias(c)).ToList();
        // Track emitted layouts by C++ struct name, not ClassInfo identity: a
        // by-value field can reference a name-twin of a type EmittedClasses
        // deduped out (the same shared-source struct from another assembly —
        // e.g. RegexParser._optionsStack : the RegularExpressions copy of
        // ValueListBuilder<int>, while only the CoreLib copy is emitted). Both
        // resolve to one struct name, so readiness keys on that name.
        var emitted = new HashSet<string>(System.StringComparer.Ordinal);
        while (valueTypes.Count > 0)
        {
            var ready = valueTypes.Where(v =>
                // An opaque value type (a reference-only struct from a dead-branch
                // cast) is emitted field-less by EmitOneStruct, so it imposes no
                // layout ordering — its by-value field types were never pulled into
                // the emit closure and must not be waited on (e.g. EventSource's
                // PropertyValue, whose nested explicit-layout Scalar union is never
                // emitted).
                IsOpaque(v) || v.Fields
                // Intrinsic value-type fields (Task-family async structs) are
                // provided by the runtime header, so they impose no emission
                // ordering.
                .Where(f => !f.IsStatic && f.Type.Kind == TypeKind.Class
                            && f.Type.Class!.IsValueType && f.Type.Class!.IntrinsicCppName is null)
                .All(f => emitted.Contains(StructCarrier(f.Type.Class!).CppStructName))).ToList();
            if (ready.Count == 0)
            {
                // A genuine cycle (only possible among full-layout value types):
                // name each stuck struct and the unemitted by-value field holding
                // it up, so the bare message is debuggable.
                var stuck = string.Join("; ", valueTypes.Select(v =>
                    v.FullName + " -> [" + string.Join(", ", v.Fields
                        .Where(f => !f.IsStatic && f.Type.Kind == TypeKind.Class
                                    && f.Type.Class!.IsValueType && f.Type.Class!.IntrinsicCppName is null
                                    && !emitted.Contains(StructCarrier(f.Type.Class!).CppStructName))
                        .Select(f => f.Name + ":" + f.Type.Class!.FullName)) + "]"));
                throw new NotSupportedException("Cyclic struct layout dependency: " + stuck);
            }
            foreach (var cls in ready)
            {
                EmitOneStruct(sb, cls, baseName: null);
                emitted.Add(cls.CppStructName);
                valueTypes.Remove(cls);
            }
        }

        foreach (var cls in TopoOrder())
        {
            if (cls.IsValueType || cls.IsEnum || IsStructAlias(cls))
                continue;
            // Emit one struct per C++ name: a tree-shaken base pulled into the
            // topo order via a subclass chain can be a name-twin of an already-
            // emitted type (shared-source classes duplicated across assemblies).
            if (!emitted.Add(cls.CppStructName))
                continue;
            if (cls.IsDelegate)
            {
                // Must mirror Dn2CppDelegate's layout (combine/remove rely on it).
                sb.AppendLine($"struct {cls.CppStructName} : Dn2CppObject");
                sb.AppendLine("{");
                sb.AppendLine("    Dn2CppObject* f_target;");
                sb.AppendLine("    void* f_method;");
                sb.AppendLine("    Dn2CppObject* f_prev;");
                sb.AppendLine("};");
                continue;
            }
            // A non-opaque class chains through its non-opaque base. When the base
            // is opaque (typically System.Exception or System.Object) the layout
            // roots at Dn2CppObject — except for runtime-owned prefixes whose hidden
            // fields must precede every user-defined instance field.
            string baseName;
            if (!IsOpaque(cls) && cls.BaseClass is { } bc && !IsOpaque(bc))
                baseName = bc.CppStructName;
            else if (!IsOpaque(cls) && Compilation.InheritsFromException(cls))
                baseName = "Dn2CppExceptionObject";
            else if (!IsOpaque(cls) && InheritsFromWaitHandle(cls))
                baseName = "Dn2CppWaitHandle";
            else
                baseName = "Dn2CppObject";
            _renderedStructRefs.Add((cls, "<base>", baseName));
            EmitOneStruct(sb, cls, baseName);
        }
        sb.AppendLine();

        // Every t_ name a layout spelled must be one this emission declared. The C++
        // compiler catches the miss too, but only as "unknown type name" with nothing
        // saying which class, which field, or why the type never reached the emit set.
        var declared = new HashSet<string>(fwdDeclared, System.StringComparer.Ordinal);
        declared.UnionWith(emitted);
        foreach (var (cls, member, cpp) in _renderedStructRefs)
            if (!StructDeclared(cpp, declared))
                throw new InvalidOperationException(
                    $"{cls.FullName}.{member} is laid out as {cpp}, which no emitted struct "
                    + "declares — the type was never closed into the emit set");
    }

    /// <summary>A field's rendered storage type, recorded for the declared-name check at
    /// the end of <see cref="EmitStructs"/>.</summary>
    private string FieldStorage(ClassInfo cls, FieldInfo f)
    {
        string t = CppTypes.FieldOf(f);
        _renderedStructRefs.Add((cls, f.Name, t));
        return t;
    }

    private void EmitOneStruct(StringBuilder sb, ClassInfo cls, string? baseName)
    {
        (ModeledSize Size, ModeledSize Align)? opaqueLayout =
            IsOpaque(cls) && cls.IsValueType ? TryModeledStructLayout(cls) : null;
        // [StructLayout(Pack = N)] caps every field's alignment at N bytes; the
        // pragma makes the C++ compiler apply the same cap so the layout matches the
        // CLR's packed layout (value types only — a class's Pack has no emitted
        // layout contract here).
        bool packed = !IsOpaque(cls) && cls.IsValueType && cls.LayoutPack > 0;
        if (packed)
            sb.AppendLine($"#pragma pack(push, {cls.LayoutPack})");
        string align = "";
        if (opaqueLayout is { } ol && ol.Align.Text != "1")
            align = $" alignas({ol.Align.Text})";
        sb.AppendLine(baseName is null
            ? $"struct{align} {cls.CppStructName}"
            : $"struct{align} {cls.CppStructName} : {baseName}");
        sb.AppendLine("{");
        ModeledSize? explicitSize = null;
        // An opaque VALUE type is field-less, but its type-info still stamps instanceSize
        // as sizeof(this shell), and reflection's Unsafe.SizeOf<T>, Marshal.SizeOf(Type)
        // and the reflection array element stride all read that stamp — an empty shell's
        // sizeof (1) is a silently wrong answer for every one of them. Pad the shell to the
        // modeled CLR layout extent so sizeof tells the truth. When the extent is unknown
        // the shell stays empty and the type-info is stamped DN2CPP_TF_LAYOUT_UNKNOWN, so
        // the size readers throw instead of answering that 1 (see _layoutUnknown).
        // The shell IS its size, so a total the model cannot write width-independently —
        // the narrow reading refused the type — takes the same unknown channel a missing
        // extent takes, rather than baking a 64-bit width a wasm32 build would then carry.
        // An opaque type is by definition one this program never lays out itself, so a
        // reader throwing when asked is the proportionate answer; no corpus type reaches
        // it today. A one-byte extent cannot: it holds no pointer, so both readings exist.
        if (IsOpaque(cls) && cls.IsValueType)
        {
            if (opaqueLayout is { } modeled)
            {
                if (modeled.Size.Text != "1")
                    sb.AppendLine($"    uint8_t __opaque_pad[{modeled.Size.Text}];");
            }
            else if (!cls.IsEnum)
                _layoutUnknown.Add(cls.CppStructName);
        }
        // Opaque reference types (Object/Exception, dead-branch refs): members are
        // inlined and real fields never materialized.
        if (!IsOpaque(cls))
        {
            var instanceFields = cls.Fields.Where(f => !f.IsStatic).ToList();
            // An [InlineArray(N)] struct's single field provides storage for N
            // contiguous elements — lay it out as that field repeated N times so the
            // ldflda + pointer-stride element access (and the span built over it) stays
            // in bounds (params-ReadOnlySpan lowering).
            if (cls.InlineArrayLength > 0 && instanceFields.Count == 1)
                // Storage-width elements (FieldOf; InlineArray owners are
                // exact-layout) — span windows over the buffer use the element's
                // real stride.
                sb.AppendLine($"    {FieldStorage(cls, instanceFields[0])} {instanceFields[0].CppName}[{cls.InlineArrayLength}];");
            else if (cls.IsExplicitLayout
                     && (instanceFields.Count > 0 || cls.LayoutSize > 0))
                // Both value types and reference types: a class's explicit region
                // follows the Dn2CppObject header (baseName), and FieldOffset is
                // relative to that region's start, exactly as the CLR places fields
                // after the method-table pointer.
                explicitSize = EmitExplicitLayoutBody(sb, cls, instanceFields);
            else if (NeedsDeclaredSizeArm(cls, instanceFields))
            {
                // A declared [StructLayout(Size = N)] reaching past the fields is stated as
                // N, in the same union arm the explicit layout uses, and never as a trailing
                // pad: for a struct whose fields end on a pointer that pad is 0 at 64-bit
                // width and positive at 32, which no array length written after the fields
                // can express. N is metadata and so target-independent; the union's own max
                // supplies the field end at whichever width the C++ compiler is building for.
                if (instanceFields.Count == 0)
                    sb.AppendLine($"    uint8_t __struct_size_pad[{cls.LayoutSize}];");
                else
                {
                    sb.AppendLine("    union");
                    sb.AppendLine("    {");
                    sb.AppendLine($"        uint8_t __struct_size_pad[{cls.LayoutSize}];");
                    sb.AppendLine("        struct");
                    sb.AppendLine("        {");
                    foreach (var f in instanceFields)
                        sb.AppendLine($"            {FieldStorage(cls, f)} {f.CppName};");
                    sb.AppendLine("        };");
                    sb.AppendLine("    };");
                }
            }
            else
            {
                foreach (var f in instanceFields)
                    sb.AppendLine($"    {FieldStorage(cls, f)} {f.CppName};");
                // A C# `fixed E buf[N]` lowers to a compiler-generated <buf>e__FixedBuffer
                // value type with a single element field of type E and an explicit
                // ClassLayout size of N*sizeof(E). The buffer is addressed as
                // `&FixedElementField + i`, so the struct must occupy the full N elements
                // even though it declares one — pad the lone scalar up to the declared size
                // (kept scalar so `&field` stays an E*, matching the ldflda call site).
                int pad = FixedBufferPadding(cls, instanceFields);
                if (pad > 0)
                    sb.AppendLine($"    uint8_t __fixedbuffer_pad[{pad}];");
            }
        }
        sb.AppendLine("};");
        if (packed)
            sb.AppendLine("#pragma pack(pop)");
        // The explicit-layout emission computed the exact byte size the CLR gives
        // the struct; pin the C++ compiler to it so any divergence (a mis-modeled
        // field width/alignment) is a compile error, not silent ABI corruption.
        // Only for a value type: a reference type's sizeof also carries the
        // Dn2CppObject header, so the union total is not its whole sizeof — the
        // field offsets are pinned by the emitted union arms regardless.
        // Unguarded at every width: the body emission refuses a total it cannot write in
        // sizeof(void*), so a guarded size never reaches here.
        if (explicitSize is { } esz && cls.IsValueType)
            sb.AppendLine($"static_assert(sizeof({cls.CppStructName}) == {esz.Text}, \"explicit layout size mismatch: {cls.FullName}\");");
        // Safety net: every value type lays out at real storage width, and the
        // extent model (TryStructExtent) mirrors the emitted body — pin the C++
        // compiler to the modeled size so a divergence (an alignment rule the
        // model missed) is a loud compile error, not a silently wrong sizeof.
        // A pointer-bearing struct is read at both widths and the pair rendered
        // as target-independent text, so the 32-bit build checks it too; only a
        // type the narrow reading refuses falls back to stating the premise.
        // Unlike the explicit and marshalled models, no narrow-reading wrapper is
        // needed here: TryStructExtent already swallows a NotSupportedException.
        else if (!IsOpaque(cls) && cls.IsValueType && TryStructExtent(cls, PointerWidth.Bytes64) is { } ext)
        {
            var sz = PointerWidth.Model(ext.Size, TryStructExtent(cls, PointerWidth.Bytes32)?.Size);
            sb.AppendLine($"static_assert({(sz.Guarded ? "sizeof(void*) != 8 || " : "")}sizeof({cls.CppStructName}) == {sz.Text}, \"sequential layout size mismatch: {cls.FullName}\");");
        }
    }

    /// <summary>Emits the body of a <c>[StructLayout(LayoutKind.Explicit)]</c> value
    /// type OR reference type as one anonymous union: a leading
    /// <c>uint8_t __explicit_pad[total]</c> arm (fixing the total size and guaranteeing
    /// <c>{}</c> zero-initialization of the whole layout), then one arm per field — the
    /// field itself for offset 0, or an anonymous struct
    /// <c>{ uint8_t __pad_&lt;field&gt;[offset]; T field; }</c> placing it at its exact
    /// byte offset. For a reference type the union follows the Dn2CppObject base
    /// subobject, so the offsets are relative to the field region exactly as the CLR
    /// places them after the method-table pointer. Fields use their real storage width
    /// (<see cref="CppTypes.FieldOf"/>, as every field does), so overlapping arms pun
    /// bytes exactly like the CLR layout.
    /// Returns the rendered total size (the value-type caller pins sizeof to it).
    /// Unrepresentable shapes (a GC reference field overlapping another field, an
    /// unsized field type, an offset misaligned for the field's C++ alignment, a total
    /// only one pointer width can express) throw with the type named.</summary>
    private ModeledSize EmitExplicitLayoutBody(StringBuilder sb, ClassInfo cls, List<FieldInfo> fields)
    {
        // Unlike every other emitted body, this one FIXES the total size — the pad arm is
        // what the union measures. So a size that leans on the pointer width must be
        // written in sizeof(void*), or the struct is a 64-bit size on a 32-bit target and
        // a copy of it walks off the end of the ABI's storage.
        var size = PointerWidth.Model(ExplicitLayoutExtent(cls, fields, PointerWidth.Bytes64).Size,
                                      TryExplicitLayoutSize(cls, fields, PointerWidth.Bytes32));
        // The narrow reading refused the shape, so there is no second number to select
        // between and nothing here to degrade to — a field-bearing type must get a body.
        // Refusing is also what every other unrepresentable explicit shape does.
        if (size.Guarded)
            throw new NotSupportedException(
                $"{cls.FullName}: the LayoutKind.Explicit total cannot be written in sizeof(void*) — "
                + "the 32-bit reading of this layout is refused, and the emitted union FIXES the "
                + "total, so the 64-bit size would become the type's size on a 32-bit target");
        if (fields.Count == 0)
        {
            sb.AppendLine($"    uint8_t __explicit_pad[{size.Text}];");
            return size;
        }
        sb.AppendLine("    union");
        sb.AppendLine("    {");
        sb.AppendLine($"        uint8_t __explicit_pad[{size.Text}];");
        foreach (var f in fields)
        {
            string t = FieldStorage(cls, f);
            sb.AppendLine(f.ExplicitOffset == 0
                ? $"        {t} {f.CppName};"
                : $"        struct {{ uint8_t __pad_{f.CppName}[{f.ExplicitOffset}]; {t} {f.CppName}; }};");
        }
        sb.AppendLine("    };");
        return size;
    }

    /// <summary>The explicit-layout total at a pointer width other than the model's own —
    /// a second reading, never a diagnosis: a shape the layout model refuses is refused
    /// loudly by the 64-bit reading alone, and a width that refuses where 64 bits did not
    /// just means the size cannot be written in <c>sizeof(void*)</c>.</summary>
    private int? TryExplicitLayoutSize(ClassInfo cls, List<FieldInfo> fields, int ptr)
    {
        try
        {
            return ExplicitLayoutExtent(cls, fields, ptr).Size;
        }
        catch (NotSupportedException e) when (!Compilation.IsMustEscape(e))
        {
            return null;
        }
    }

    /// <summary>The exact byte size and alignment of an explicit-layout value type (or
    /// the explicit field region of a reference type) with pointers <paramref name="ptr"/>
    /// bytes wide: fields end at max(offset + field size), grown to any declared
    /// <c>[StructLayout(Size = N)]</c> and rounded up to the max (pack-capped) field
    /// alignment — exactly what the emitted union produces in C++ and what the CLR
    /// computes. Throws (naming the type and field) for every shape the union
    /// emission cannot represent; see <see cref="EmitExplicitLayoutBody"/>. A declared
    /// size makes max(Size, field end) the byte count that must be representable, so
    /// the refusal names that total rather than the Size that may not be the offender.</summary>
    private (int Size, int Align) ExplicitLayoutExtent(ClassInfo cls, List<FieldInfo> fields, int ptr)
    {
        int pack = cls.LayoutPack;
        int align = 1, end = 0;
        var placed = new List<(FieldInfo F, int Off, int Size, bool Gc)>();
        foreach (var f in fields)
        {
            if (f.ExplicitOffset < 0)
                throw new NotSupportedException(
                    $"{cls.FullName}.{f.Name}: a LayoutKind.Explicit field without [FieldOffset] is not supported");
            if (TryFieldExtent(f.Type, ptr) is not { } e)
                throw new NotSupportedException(
                    $"{cls.FullName}.{f.Name}: cannot compute the size of field type {f.Type} inside a LayoutKind.Explicit struct");
            int a = pack > 0 && e.Align > pack ? pack : e.Align;
            if (f.ExplicitOffset % a != 0)
                throw new NotSupportedException(
                    $"{cls.FullName}.{f.Name}: [FieldOffset({f.ExplicitOffset})] is not a multiple of the field's {a}-byte alignment, which the emitted C++ layout cannot represent");
            if (a > align) align = a;
            if (f.ExplicitOffset + e.Size > end) end = f.ExplicitOffset + e.Size;
            placed.Add((f, f.ExplicitOffset, e.Size, f.Type.ContainsGcReferences()));
        }
        // A GC-reference field may sit at its own offset but may NOT overlap another
        // field. Boehm scans the object conservatively, so a non-overlapping
        // reference is found and kept alive wherever it lands (the common interop /
        // tagged-union-with-a-reference shape); but a reference sharing bytes with
        // another arm has byte-exact overlap semantics this union emission does not
        // model — and the CLR type loader itself rejects overlapping object
        // references, so any type that loaded has none. Reject the overlap loudly.
        foreach (var (f, off, size, gc) in placed)
        {
            if (!gc) continue;
            foreach (var (g, goff, gsize, _) in placed)
            {
                if (ReferenceEquals(f, g)) continue;
                if (off < goff + gsize && goff < off + size)
                    throw new NotSupportedException(
                        $"{cls.FullName}.{f.Name}: a GC-reference field overlapping another field in a LayoutKind.Explicit type is not supported");
            }
        }
        int content = Math.Max(end, cls.LayoutSize);
        if (content == 0) content = 1;
        int total = RoundUp(content, align);
        // What real .NET measures is max(Size, end) with the alignment intact — sizeof,
        // Unsafe.SizeOf, Marshal.SizeOf and the array stride alike, so Explicit Size=6 over
        // an int is 6 and Size=4 over {IntPtr@0, byte@8} is 9. C++ cannot express a size
        // that is not a multiple of alignof, so the offender is that total and not always
        // the declared Size. Asserted by build-and-run-marshal-pinning.sh's negative arms.
        if (cls.LayoutSize > 0 && content % align != 0)
            throw new NotSupportedException(
                $"{cls.FullName}: explicit Size={cls.LayoutSize} over fields ending at byte {end} gives a {content}-byte struct, not a multiple of its {align}-byte alignment, which the emitted C++ layout cannot represent");
        return (total, align);
    }

    /// <summary>Whether an emitted sequential value-type body must FIX its total with a
    /// declared-size arm because a <c>[StructLayout(Size = N)]</c> reaches past its fields;
    /// false for the fixed-buffer shape, which the caller pads scalar-first for its own
    /// reason. Asked at BOTH pointer widths because the padding is what moves: a struct
    /// whose fields end on a pointer needs none at 64 bits and 8 bytes at 32, and the arm
    /// is the only shape that can state either.</summary>
    private bool NeedsDeclaredSizeArm(ClassInfo cls, List<FieldInfo> fields)
    {
        if (FixedBufferPadding(cls, fields) > 0)
            return false;
        // Unguarded at the model's own width: a declared size the emitted layout cannot
        // represent must fail the transpile, and that verdict is the 64-bit reading's.
        return SequentialSizePadding(cls, fields, PointerWidth.Bytes64) > 0
               || TrySequentialSizePadding(cls, fields, PointerWidth.Bytes32) > 0;
    }

    /// <summary>The declared-size padding at a pointer width other than the model's own — a
    /// second reading, never a diagnosis, exactly as
    /// <see cref="TryExplicitLayoutSize"/> is: a shape the layout model refuses is refused
    /// loudly by the 64-bit reading alone.</summary>
    private int TrySequentialSizePadding(ClassInfo cls, List<FieldInfo> fields, int ptr)
    {
        try
        {
            return SequentialSizePadding(cls, fields, ptr);
        }
        catch (NotSupportedException e) when (!Compilation.IsMustEscape(e))
        {
            return 0;
        }
    }

    /// <summary>The trailing padding a sequential value type with a declared
    /// <c>[StructLayout(Size = N)]</c> needs so its C++ <c>sizeof</c> reaches N —
    /// the general form of the fixed-buffer special case (which the caller checks
    /// first). The extent model adds it to the field end; the emitted body states the
    /// resulting total instead (see <see cref="NeedsDeclaredSizeArm"/>).
    /// 0 when there is no declared size, the natural size already covers it
    /// (e.g. every empty struct declares Size=1, and C++ gives it 1 anyway), or the
    /// field extents cannot be computed (previous behavior: no pad).
    ///
    /// <para>A declared size is a FLOOR that suppresses the rounding: real .NET measures
    /// <c>max(Size, field end)</c> with the alignment intact — sizeof, Unsafe.SizeOf,
    /// Marshal.SizeOf and the array stride alike — so Size=6 over an int is 6, and
    /// Size=4 over {IntPtr,byte} is 9. C++ cannot express a size that is not a multiple
    /// of alignof, so that total is refused loudly, ahead of the early return: the shape
    /// is unrepresentable whether or not it needs a pad. Past the refusal the natural
    /// size already equals that total, which is why the pad below stays
    /// <c>Size - end</c> — recomputing it would move types whose declared size merely
    /// equals their natural one onto the declared-size union arm for nothing. Asserted
    /// by build-and-run-marshal-pinning.sh's negative arms.</para></summary>
    private int SequentialSizePadding(ClassInfo cls, List<FieldInfo> fields, int ptr)
    {
        if (!cls.IsValueType || cls.LayoutSize <= 0)
            return 0;
        if (TrySequentialFieldsEnd(cls, fields, ptr) is not { } e)
            return 0;
        int total = Math.Max(cls.LayoutSize, e.End);
        if (total % e.Align != 0)
            throw new NotSupportedException(
                $"{cls.FullName}: Size={cls.LayoutSize} over fields ending at byte {e.End} gives a {total}-byte struct, not a multiple of its {e.Align}-byte alignment, which the emitted C++ layout cannot represent");
        int natural = Math.Max(RoundUp(e.End, e.Align), 1);
        if (cls.LayoutSize <= natural)
            return 0;
        return cls.LayoutSize - e.End;
    }

    /// <summary>Where a sequential value type's emitted members end (before any
    /// trailing size padding) and its alignment, following the C layout rules the
    /// C++ compiler applies to the emitted body: each member at
    /// <see cref="CppTypes.FieldOf"/> width, aligned to its (pack-capped) natural
    /// alignment. Includes the fixed-buffer trailing pad. Null when a member's
    /// extent is unknown (an intrinsic/opaque/external field type).</summary>
    private (int End, int Align)? TrySequentialFieldsEnd(ClassInfo cls, List<FieldInfo> fields, int ptr)
    {
        int pack = cls.LayoutPack;
        int cursor = 0, align = 1;
        foreach (var f in fields)
        {
            if (TryFieldExtent(f.Type, ptr) is not { } e)
                return null;
            int a = pack > 0 && e.Align > pack ? pack : e.Align;
            if (a > align) align = a;
            cursor = RoundUp(cursor, a) + e.Size;
        }
        cursor += FixedBufferPadding(cls, fields);
        return (cursor, align);
    }

    /// <summary>The byte size and alignment of the intrinsic-modeled value-type C++
    /// structs — the date/time/decimal family that <see cref="CppTypes"/> maps to a
    /// hand-written runtime struct (<c>runtime/core/dn2cpp_core.h</c>) rather than a
    /// transpiled <c>t_*</c> body, so <see cref="TryStructExtent"/> has no field rows to
    /// sum. Values mirror those C++ definitions; a divergence (a runtime struct changing
    /// width) is caught loudly by the layout <c>static_assert</c> the caller emits — the
    /// enclosing struct's <c>sizeof</c> is pinned to the total these produce — never
    /// silently. Only the pointer-free blittable value types are listed: a pointer-bearing
    /// intrinsic value type (e.g. GCHandle) or a SIMD vector is left unknown, so an
    /// explicit-layout field of one still aborts loudly rather than guessing a width.
    ///
    /// <para>These are REPRESENTATION sizes, and each equals real .NET's
    /// <c>Unsafe.SizeOf</c>. Keep it that way by changing the C++ STRUCT, never by adding a
    /// per-type "CLR layout size" metadata override: the readers do not share code — the
    /// statically lowered <c>Unsafe.SizeOf&lt;T&gt;()</c> and IL <c>sizeof</c> are a C++
    /// <c>sizeof</c> at the call site, which an override at a runtime reader cannot reach,
    /// while the reflected <c>Unsafe.SizeOf</c>, the box payload, the array element stride
    /// and the <c>Unsafe.Add</c>/<c>ByteOffset</c> arithmetic read <c>instanceSize</c>. One
    /// spelling would then answer 8 where the other answered 16, and a caller sizing a
    /// buffer from it would step 16-byte elements through 8-byte slots. <c>Marshal.SizeOf</c>
    /// is deliberately outside the agreement: real .NET REFUSES it for these AutoLayout
    /// types, so dn2cpp refuses it too and the question never arises on that spelling.</para>
    /// <para>Asserted by <c>ReflectIntrinsicSizeOfSubset</c>: the per-type
    /// "static == reflected == stride" agreement, and the sizes themselves live-diffed
    /// against real .NET.</para></summary>
    private static readonly Dictionary<string, (int Size, int Align)> s_intrinsicStructExtents = new()
    {
        ["Dn2CppTimeSpan"] = (8, 8),          // int64 ticks
        ["Dn2CppDateTime"] = (8, 8),          // int64 _dateData (ticks:62 | kind:2)
        ["Dn2CppDateTimeOffset"] = (16, 8),   // int64 ticks + int32 offsetMinutes
        ["Dn2CppDateOnly"] = (4, 4),          // int32 dayNumber
        ["Dn2CppTimeOnly"] = (8, 8),          // int64 ticks
        ["Dn2CppDecimal"] = (16, 8),          // int32 flags + uint32 hi32 + uint64 lo64
    };

    /// <summary>The byte size and alignment a field of type <paramref name="t"/>
    /// occupies in an emitted layout — derived from the C++ member type
    /// (<see cref="CppTypes.FieldOf"/> — every field at its real storage
    /// width): any pointer is <paramref name="ptr"/> wide and so aligned, scalars their
    /// width, an intrinsic-modeled value type its fixed runtime-struct extent
    /// (<see cref="s_intrinsicStructExtents"/>), a transpiled by-value struct its
    /// computed extent. Null when unknown (an unlisted intrinsic or opaque struct, an
    /// unmapped external type). An extent leaning on the pointer width is not flagged:
    /// the caller reads it at both widths and hands the pair to
    /// <see cref="PointerWidth.Model"/>, which is what makes the emitted text
    /// target-independent.</summary>
    private (int Size, int Align)? TryFieldExtent(TypeDesc t, int ptr)
    {
        string ct;
        try
        {
            ct = CppTypes.StorageOf(t);
        }
        catch (NotSupportedException e) when (!Compilation.IsMustEscape(e))
        {
            return null;
        }
        if (ct.EndsWith('*'))
            return (ptr, ptr);
        switch (ct)
        {
            case "int8_t" or "uint8_t": return (1, 1);
            case "int16_t" or "uint16_t" or "char16_t": return (2, 2);
            case "int32_t" or "uint32_t" or "float": return (4, 4);
            case "int64_t" or "uint64_t" or "double": return (8, 8);
            case "intptr_t" or "uintptr_t": return (ptr, ptr);
        }
        // An intrinsic value type (DateTime, Decimal, …) lowers to a hand-written runtime
        // struct rather than a transpiled body, so its extent is that C++ struct's, not a
        // field-row sum. Keyed on the resolved C++ type so a loaded Class and an External
        // TypeRef of the same type answer identically.
        if (s_intrinsicStructExtents.TryGetValue(ct, out var ise))
            return ise;
        // Opaque structs are NOT excluded here: their extent feeds the opaque-shell pad
        // (EmitOneStruct), which is what makes the shell's sizeof — and the instanceSize
        // stamped from it — truthful. Recursing into an opaque field type cannot
        // desynchronize a layout static_assert: a non-opaque struct never holds an opaque
        // type by value (the emit closure pulls every by-value field type in non-opaquely),
        // and opaque containers emit no assert. MembersReady-guarded so an uncompleted
        // specialization's field rows are never pulled just to size a shell.
        if (t is { Kind: TypeKind.Class, Class: { IsValueType: true, IsEnum: false, IntrinsicCppName: null } fc }
            && fc.MembersReady)
            return TryStructExtent(fc, ptr);
        return null;
    }

    // Memoized per-struct extents, keyed on the pointer width the reading assumed (a
    // value type cannot contain itself by value, so the null pre-seed below only guards
    // a malformed cycle, not a real layout).
    private readonly Dictionary<(ClassInfo Cls, int Ptr), (int Size, int Align)?> _structExtents = new();

    /// <summary>The C++ <c>sizeof</c>/<c>alignof</c> of an emitted value-type struct,
    /// mirroring <see cref="EmitOneStruct"/>'s body exactly (inline-array repetition,
    /// explicit-layout union, fixed-buffer and declared-size trailing pads, pack
    /// capping), with pointers <paramref name="ptr"/> bytes wide. Null when a member's
    /// extent is unknown.</summary>
    private (int Size, int Align)? TryStructExtent(ClassInfo cls, int ptr)
    {
        if (_structExtents.TryGetValue((cls, ptr), out var cached))
            return cached;
        _structExtents[(cls, ptr)] = null;
        (int Size, int Align)? r;
        try
        {
            r = ComputeStructExtent(cls, ptr);
        }
        catch (NotSupportedException e) when (!Compilation.IsMustEscape(e))
        {
            // The opaque-shell pad recurses into shapes the layout model rejects
            // LOUDLY on the emission path (an explicit-layout GC-reference overlap,
            // a declared Size misaligned for the struct's alignment). For the
            // model, null — "extent unknown" — is the honest answer: the opaque
            // shell stays empty and no static_assert is emitted. Nothing real is
            // muted, because every struct this model describes non-opaquely also
            // runs the throwing calls themselves during its body emission
            // (EmitExplicitLayoutBody / SequentialSizePadding), and those keep
            // failing the transpile.
            r = null;
        }
        _structExtents[(cls, ptr)] = r;
        return r;
    }

    /// <summary>The target-independent size and alignment an opaque value shell can
    /// preserve. A missing narrow reading cannot be baked into shared generated text.</summary>
    private (ModeledSize Size, ModeledSize Align)? TryModeledStructLayout(ClassInfo cls)
    {
        if (TryStructExtent(cls, PointerWidth.Bytes64) is not { } wide)
            return null;
        var narrow = TryStructExtent(cls, PointerWidth.Bytes32);
        var size = PointerWidth.Model(wide.Size, narrow?.Size);
        var align = PointerWidth.Model(wide.Align, narrow?.Align);
        return size.Guarded || align.Guarded ? null : (size, align);
    }

    private (int Size, int Align)? ComputeStructExtent(ClassInfo cls, int ptr)
    {
        var fields = cls.Fields.Where(f => !f.IsStatic).ToList();
        if (cls.InlineArrayLength > 0 && fields.Count == 1)
        {
            // realWidth: the buffer is emitted at storage width (see the struct
            // body emission), so the extent must use the same stride.
            if (TryFieldExtent(fields[0].Type, ptr) is not { } el)
                return null;
            return (el.Size * cls.InlineArrayLength, el.Align);
        }
        if (cls.IsExplicitLayout)
        {
            var (size, align) = ExplicitLayoutExtent(cls, fields, ptr);
            // The pack cap applies to the arms, already folded into the extent; the
            // union's own alignment is the max arm alignment.
            return (size, align);
        }
        if (TrySequentialFieldsEnd(cls, fields, ptr) is not { } e)
            return null;
        int end = e.End + SequentialSizePadding(cls, fields, ptr);
        return (Math.Max(RoundUp(end, e.Align), 1), e.Align);
    }

    private static int RoundUp(int value, int align) => (value + align - 1) / align * align;

    /// <summary>Trailing padding (in bytes) needed to grow a fixed-buffer struct
    /// (<c>&lt;buf&gt;e__FixedBuffer</c>, one primitive field + an explicit metadata
    /// layout size of N*sizeof(field)) to its declared size. Returns 0 for an ordinary
    /// struct (no explicit size, or a size that already equals the field width).</summary>
    private static int FixedBufferPadding(ClassInfo cls, List<FieldInfo> instanceFields)
    {
        if (!cls.IsValueType || instanceFields.Count != 1)
            return 0;
        if (instanceFields[0].Type is not { Kind: TypeKind.Primitive, Primitive: var p })
            return 0;
        int width = PrimitiveByteWidth(p);
        if (width <= 0)
            return 0;
        int size = cls.Module.Reader.GetTypeDefinition(cls.Handle).GetLayout().Size;
        return size > width && size % width == 0 ? size - width : 0;
    }

    private static int PrimitiveByteWidth(PrimitiveTypeCode p) => p switch
    {
        PrimitiveTypeCode.Boolean or PrimitiveTypeCode.SByte or PrimitiveTypeCode.Byte => 1,
        PrimitiveTypeCode.Char or PrimitiveTypeCode.Int16 or PrimitiveTypeCode.UInt16 => 2,
        PrimitiveTypeCode.Int32 or PrimitiveTypeCode.UInt32 or PrimitiveTypeCode.Single => 4,
        PrimitiveTypeCode.Int64 or PrimitiveTypeCode.UInt64 or PrimitiveTypeCode.Double => 8,
        _ => 0,
    };

    private void EmitStaticFields(CppOutput o)
    {
        var statics = EmittedClasses
            .Where(c => !IsOpaque(c) && !SkipsCanonicalMetadata(c))
            .SelectMany(c => c.Fields.Where(f => f.IsStatic && !f.IsLiteral)).ToList();
        if (statics.Count == 0)
            return;
        o.Header.AppendLine("// ---- static fields ----");
        o.Data.AppendLine("// ---- static fields ----");
        var gcThreadStatics = new List<FieldInfo>();
        foreach (var f in statics)
        {
            // A [ThreadStatic] whose type is (or contains) a GC reference cannot use
            // raw thread_local storage — TLV blocks are not scanned by the collector
            // on every platform (Darwin's are malloc-backed), so the field would not
            // be a GC root. Those fields live in the per-thread GC-visible block
            // emitted below; every access goes through FieldInfo.CppStaticAccess.
            if (f.IsGcRootedThreadStatic)
            {
                gcThreadStatics.Add(f);
                continue;
            }
            // At the field's real storage width, like every other field (CppTypes
            // .FieldOf): a `static ushort` is a uint16_t slot, not an int32_t one, so
            // the `ref ushort` an ldsflda hands out addresses the whole storage.
            string t = CppTypes.FieldOf(f);
            // [ThreadStatic] -> thread_local: each thread gets its own zero-initialized
            // copy. The field initializer runs inside the .cctor, which executes only on
            // the main thread, so only main's copy is initialized and every other thread
            // sees default(T) — exactly .NET's [ThreadStatic] semantics.
            // External linkage (declare in the header, define once here) so a body in any
            // translation unit can read/write it. [[maybe_unused]]: a static of an emitted
            // class may be untouched by reachable code (e.g. RuntimeType's cached
            // singletons pulled in by typeof machinery) — expected for generated code.
            string storage = f.IsThreadStatic ? "thread_local " : "";
            // A non-thread-static field whose type is (or contains) a GC reference is a
            // data-segment root the collector cannot see on wasm (bdwgc's EMSCRIPTEN arm
            // registers an empty static-data range) — DN2CPP_GC_STATIC_ROOT corrals it
            // into the dn2cpp_roots section the runtime registers at init. Empty off-wasm.
            // GC-ref [ThreadStatic] fields never reach here (routed to the per-thread
            // GC-visible block above); non-GC statics stay unmarked so they waste no
            // scanned bytes.
            string root = !f.IsThreadStatic && f.Type.ContainsGcReferences() ? "DN2CPP_GC_STATIC_ROOT " : "";
            o.Header.AppendLine($"extern {storage}{t} {f.CppStaticName};");
            o.Data.AppendLine($"{root}[[maybe_unused]] {storage}{t} {f.CppStaticName} = {CppTypes.ZeroInit(t)};");
        }
        if (gcThreadStatics.Count > 0)
        {
            // GC-reference-carrying [ThreadStatic] fields: one slot each inside a
            // per-thread block the runtime allocates GC-visibly (uncollectable =>
            // scanned as a root, zero-filled => a fresh thread sees default(T),
            // released when a runtime-spawned thread exits). The accessor is a plain
            // call each time: the runtime owns the thread_local block pointer, so
            // thread exit can free and clear it without a stale cache anywhere.
            o.Header.AppendLine("struct Dn2CppThreadStatics");
            o.Header.AppendLine("{");
            foreach (var f in gcThreadStatics)
                o.Header.AppendLine($"    {CppTypes.FieldOf(f)} {f.CppStaticName};");
            o.Header.AppendLine("};");
            o.Header.AppendLine("inline Dn2CppThreadStatics* dn2cpp_threadstatics()");
            o.Header.AppendLine("{");
            o.Header.AppendLine("    return (Dn2CppThreadStatics*)dn2cpp_threadstatic_block((int32_t)sizeof(Dn2CppThreadStatics));");
            o.Header.AppendLine("}");
        }
        o.Header.AppendLine();
        o.Data.AppendLine();
    }

    /// <summary>True when a rendered C++ type is declared in this image: any
    /// runtime-provided type, or a <c>t_*</c> struct whose class was actually
    /// emitted. A vtable/interface/delegate slot signature can reference a
    /// class tree-shaking dropped — e.g. a Godot engine shim virtual
    /// (<c>_Input(InputEvent)</c>) whose body is skipped in a GDExtension
    /// build, leaving the parameter class unreferenced — and a bridge over
    /// such a slot would name an undeclared struct. The slot is outside the
    /// bridged surface exactly like an unrenderable shape: the patch
    /// converter's fence rejects overrides of it at bake time.</summary>
    private static bool StructDeclared(string cppType, IReadOnlySet<string> declaredStructs)
    {
        if (!cppType.StartsWith("t_", StringComparison.Ordinal))
            return true;
        // Strip the pointer suffix (t_Foo* / t_Foo** -> t_Foo). Stars are always
        // trailing in a cpp type name, so cutting at the first '*' equals
        // TrimEnd('*') and keeps this emitter inside the self-host subset.
        int star = cppType.IndexOf('*');
        string bare = star >= 0 ? cppType.Substring(0, star) : cppType;
        return declaredStructs.Contains(bare);
    }

    /// <summary>Hot-update base build: pre-emits the N2M vtable trampolines —
    /// one native function per distinct (vtable slot, signature shape) pair
    /// over the emitted classes' vtables — plus the registration table the
    /// patch loader scans when it rewrites a patch type's copied vtable
    /// (docs/BPI-FORMAT.md "N2M trampolines"). An AOT callvirt site loads the
    /// slot's raw function pointer and calls it with the slot's C++ ABI, so
    /// each bridge is emitted with exactly that ABI (the receiver as a base
    /// object pointer — the call site casts the pointer anyway), marshals the
    /// arguments into interpreter slots, and forwards to dn2cpp_interp_vcall
    /// with its baked slot number; the slot bake is safe because every slot's
    /// signature is a V line of the hashed ABI contract (slot freeze). Slots
    /// whose shape falls outside the bridged surface (by-value structs, byrefs,
    /// unsigned/native ints, …) get no bridge — the patch converter's method
    /// fence rejects overrides of those shapes at bake time, and a mismatched
    /// BPI fails loudly at load.</summary>
    private static void EmitN2MTrampolines(StringBuilder sb, IReadOnlyList<ClassInfo> vtableClasses,
        IReadOnlySet<string> declaredStructs)
    {
        // The interpreter-slot member carrying a value of the given emitted C++
        // type, or null when the type is outside the bridged marshalling shapes
        // (mirrors the interpreter's Slot invariants: i32 sign-extended in `i`,
        // f32 widened in `f`, any pointer in `ref`).
        static string? SlotMember(string cppType) => cppType switch
        {
            "int32_t" or "int64_t" => "i",
            "float" or "double" => "f",
            _ => cppType.EndsWith("*", StringComparison.Ordinal) ? "ref" : null,
        };

        var bodies = new StringBuilder();
        var rows = new List<string>();
        var seenRows = new HashSet<string>(StringComparer.Ordinal);
        var fnBySig = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var cls in vtableClasses)
        {
            for (int slot = 0; slot < cls.SlotOwners.Count; slot++)
            {
                var owner = cls.SlotOwners[slot];
                string sigKey = owner.SigKey;
                string shape = sigKey.Substring(sigKey.IndexOf('('));
                if (!seenRows.Add(slot + "|" + shape))
                    continue;
                // The slot's C++ ABI; a shape CppTypes cannot render (open
                // generics, exotic externals) is outside the bridged surface.
                // Decode outside the try (see the interface trampolines below): the
                // catch is for the render, and a signature that will not decode must
                // fail the transpile rather than silently drop a slot the patch
                // loader is going to look for.
                owner.EnsureSignature();
                string retC;
                var paramCs = new List<string>();
                try
                {
                    retC = owner.Signature.ReturnType.IsVoid
                        ? "void"
                        : CppTypes.Of(owner.Signature.ReturnType);
                    foreach (var p in owner.Signature.ParameterTypes)
                        paramCs.Add(CppTypes.Of(p));
                }
                catch (NotSupportedException e) when (!Compilation.IsMustEscape(e))
                {
                    continue;
                }
                if (retC != "void" && (SlotMember(retC) is null || !StructDeclared(retC, declaredStructs)))
                    continue;
                if (paramCs.Any(p => SlotMember(p) is null || !StructDeclared(p, declaredStructs)))
                    continue;

                string fnKey = slot + "|" + retC + "|" + string.Join(",", paramCs);
                if (!fnBySig.TryGetValue(fnKey, out var fn))
                {
                    fn = $"dn2cpp_n2m_{fnBySig.Count}";
                    fnBySig[fnKey] = fn;
                    var ps = new List<string> { "Dn2CppObject* a0" };
                    for (int i = 0; i < paramCs.Count; i++)
                        ps.Add($"{paramCs[i]} a{i + 1}");
                    bodies.AppendLine($"static {retC} {fn}({string.Join(", ", ps)})");
                    bodies.AppendLine("{");
                    bodies.AppendLine($"    Dn2CppInterpSlot s[{paramCs.Count + 1}];");
                    bodies.AppendLine("    s[0].ref = a0;");
                    for (int i = 0; i < paramCs.Count; i++)
                    {
                        string m = SlotMember(paramCs[i])!;
                        bodies.AppendLine(m == "ref"
                            ? $"    s[{i + 1}].ref = (void*)a{i + 1};"
                            : $"    s[{i + 1}].{m} = a{i + 1};");
                    }
                    string call = $"dn2cpp_interp_vcall({slot}, s, {paramCs.Count + 1})";
                    if (retC == "void")
                        bodies.AppendLine($"    {call};");
                    else
                        bodies.AppendLine($"    return ({retC}){call}.{SlotMember(retC)};");
                    bodies.AppendLine("}");
                }
                rows.Add($"{{ {slot}, \"{shape.Replace("\\", "\\\\").Replace("\"", "\\\"")}\", (const void*)&{fn} }}");
            }
        }

        sb.AppendLine("// ---- hot-update N2M vtable trampolines (docs/BPI-FORMAT.md) ----");
        sb.Append(bodies);
        int count = rows.Count;
        if (rows.Count == 0)
            rows.Add("{ -1, nullptr, nullptr }"); // an empty array is ill-formed C++
        sb.AppendLine($"extern \"C\" const Dn2CppN2MTrampoline dn2cpp_n2m_trampolines[] = {{ {string.Join(", ", rows)} }};");
        sb.AppendLine($"extern \"C\" const int32_t dn2cpp_n2m_trampoline_count = {count};");
        sb.AppendLine();
    }

    /// <summary>Hot-update base build: pre-emits the N2M interface trampolines —
    /// one native function per (emitted interface, method slot) pair with a
    /// bridgeable signature — plus the registration table the patch loader scans
    /// when it builds a patch type's interface-dispatch map. Mirrors the vtable
    /// trampolines (docs/BPI-FORMAT.md "N2M trampolines"), but an interface
    /// method's identity is (interface, slot) rather than a bare vtable slot, so
    /// both are baked into the body's dn2cpp_interp_itfcall call and matched in
    /// the registration table (the interface pointer disambiguates two
    /// interfaces sharing a slot number). A slot whose signature falls outside
    /// the bridged marshalling shapes gets no bridge — a patch type that
    /// implements such a method with an interpreted body fails the load loudly,
    /// and the converter's method fence rejects the shape at bake time.</summary>
    private void EmitN2MItfTrampolines(StringBuilder sb, IReadOnlyList<ClassInfo> itfClasses,
        IReadOnlySet<string> declaredStructs)
    {
        static string? SlotMember(string cppType) => cppType switch
        {
            "int32_t" or "int64_t" => "i",
            "float" or "double" => "f",
            _ => cppType.EndsWith("*", StringComparison.Ordinal) ? "ref" : null,
        };

        var bodies = new StringBuilder();
        var rows = new List<string>();
        int fnCount = 0;
        foreach (var itf in itfClasses)
        {
            for (int slot = 0; slot < itf.Methods.Count; slot++)
            {
                var im = itf.Methods[slot];
                // Decode outside the try. BuildVtables skips interfaces, so this is a genuine
                // first read of the signature — and the catch below is for CppTypes.Of, whose
                // failure means "this shape cannot be marshalled, so the loader does without
                // the trampoline". A signature that will not DECODE is a different failure
                // entirely — one that must fail the transpile; swallowed
                // here it would silently drop a slot the patch loader is going to look for.
                im.EnsureSignature();
                string retC;
                var paramCs = new List<string>();
                try
                {
                    retC = im.Signature.ReturnType.IsVoid ? "void" : CppTypes.Of(im.Signature.ReturnType);
                    foreach (var p in im.Signature.ParameterTypes)
                        paramCs.Add(CppTypes.Of(p));
                }
                catch (NotSupportedException e) when (!Compilation.IsMustEscape(e))
                {
                    continue;
                }
                if (retC != "void" && (SlotMember(retC) is null || !StructDeclared(retC, declaredStructs)))
                    continue;
                if (paramCs.Any(p => SlotMember(p) is null || !StructDeclared(p, declaredStructs)))
                    continue;

                string fn = $"dn2cpp_n2m_itf_{fnCount++}";
                var ps = new List<string> { "Dn2CppObject* a0" };
                for (int i = 0; i < paramCs.Count; i++)
                    ps.Add($"{paramCs[i]} a{i + 1}");
                bodies.AppendLine($"static {retC} {fn}({string.Join(", ", ps)})");
                bodies.AppendLine("{");
                bodies.AppendLine($"    Dn2CppInterpSlot s[{paramCs.Count + 1}];");
                bodies.AppendLine("    s[0].ref = a0;");
                for (int i = 0; i < paramCs.Count; i++)
                {
                    string mem = SlotMember(paramCs[i])!;
                    bodies.AppendLine(mem == "ref"
                        ? $"    s[{i + 1}].ref = (void*)a{i + 1};"
                        : $"    s[{i + 1}].{mem} = a{i + 1};");
                }
                string call = $"dn2cpp_interp_itfcall({TypeInfoRef(itf, "hot-update N2M interface trampoline")}, {slot}, s, {paramCs.Count + 1})";
                if (retC == "void")
                    bodies.AppendLine($"    {call};");
                else
                    bodies.AppendLine($"    return ({retC}){call}.{SlotMember(retC)};");
                bodies.AppendLine("}");
                rows.Add($"{{ {TypeInfoRef(itf, "hot-update N2M interface trampoline row")}, {slot}, (const void*)&{fn} }}");
            }
        }

        sb.AppendLine("// ---- hot-update N2M interface trampolines (docs/BPI-FORMAT.md) ----");
        sb.Append(bodies);
        int count = rows.Count;
        if (rows.Count == 0)
            rows.Add("{ nullptr, -1, nullptr }"); // an empty array is ill-formed C++
        sb.AppendLine($"extern \"C\" const Dn2CppN2MItfTrampoline dn2cpp_n2m_itf_trampolines[] = {{ {string.Join(", ", rows)} }};");
        sb.AppendLine($"extern \"C\" const int32_t dn2cpp_n2m_itf_trampoline_count = {count};");
        sb.AppendLine();
    }

    /// <summary>Pre-emits the delegate interpreter bridges for a
    /// <c>--hotupdate-base</c> build. For each bridgeable emitted delegate type
    /// (its Invoke return + parameters all inside the interpreter-slot
    /// marshalling surface), two functions plus a registration row in each of
    /// two type-info-keyed tables (a delegate's Invoke signature is neither a
    /// vtable nor an interface slot):
    /// <list type="bullet">
    /// <item><c>dn2cpp_dgthunk_&lt;T&gt;</c> — the <c>f_method</c> a patch method
    /// bound into the delegate carries. Its ABI is the delegate's Invoke C++ ABI
    /// with a leading <c>Dn2CppObject*</c> target (the interpreter closure); it
    /// packs the invoke arguments into interpreter slots and forwards into
    /// <c>dn2cpp_interp_dgcall</c>. Registered in
    /// <c>dn2cpp_n2m_delegate_thunks</c>.</item>
    /// <item><c>dn2cpp_dgbridge_&lt;T&gt;</c> — the reverse: an interpreted
    /// <c>Invoke</c> hands the delegate + its argument slots here; the bridge
    /// unpacks the slots into the Invoke C++ ABI and calls the delegate's own
    /// emitted multicast invoker <c>dginvoke_&lt;T&gt;</c> (so a delegate wrapping
    /// either an interpreted or an AOT method invokes correctly). Registered in
    /// <c>dn2cpp_dg_invoke_bridges</c>.</item>
    /// </list>
    /// Gated on <c>--hotupdate-base</c>, so normal builds stay byte-identical.
    /// The bridged surface is the same as the N2M trampolines': signed 32/64-bit
    /// ints, float/double, and any pointer (mirroring the <c>Dn2CppInterpSlot</c>
    /// members). A delegate whose Invoke falls outside it is skipped, and a patch
    /// that constructs or invokes it fails loudly at load.</summary>
    private void EmitDelegateInterpBridges(StringBuilder sb, IReadOnlyList<ClassInfo> delegateClasses,
        IReadOnlySet<string> declaredStructs)
    {
        static string? SlotMember(string cppType) => cppType switch
        {
            "int32_t" or "int64_t" => "i",
            "float" or "double" => "f",
            _ => cppType.EndsWith("*", System.StringComparison.Ordinal) ? "ref" : null,
        };

        var bodies = new StringBuilder();
        var thunkRows = new List<string>();
        var bridgeRows = new List<string>();
        foreach (var cls in delegateClasses)
        {
            var invoke = cls.Methods.FirstOrDefault(m => m.Name == "Invoke");
            if (invoke is null)
                continue;
            // Decode outside the try (see the interface trampolines above): the catch
            // is for the render, and a signature that will not decode must fail the
            // transpile rather than silently drop a bridge the patch loader needs.
            invoke.EnsureSignature();
            string retC;
            var paramCs = new List<string>();
            try
            {
                retC = invoke.Signature.ReturnType.IsVoid ? "void" : CppTypes.Of(invoke.Signature.ReturnType);
                foreach (var p in invoke.Signature.ParameterTypes)
                    paramCs.Add(CppTypes.Of(p));
            }
            catch (NotSupportedException e) when (!Compilation.IsMustEscape(e))
            {
                continue;
            }
            if (retC != "void" && (SlotMember(retC) is null || !StructDeclared(retC, declaredStructs)))
                continue;
            if (paramCs.Any(p => SlotMember(p) is null || !StructDeclared(p, declaredStructs)))
                continue;
            int n = paramCs.Count;

            // The thunk: the delegate's f_method for a patch-bound target. Packs
            // the invoke parameters into slots (i32 sign-extended into .i, f32
            // widened into .f, pointers into .ref) and forwards into the
            // interpreter, then narrows the result slot back to the AOT type.
            string thunk = $"dn2cpp_dgthunk_{cls.CppName}";
            var tps = new List<string> { "Dn2CppObject* __closure" };
            for (int i = 0; i < n; i++)
                tps.Add($"{paramCs[i]} a{i}");
            bodies.AppendLine($"static {retC} {thunk}({string.Join(", ", tps)})");
            bodies.AppendLine("{");
            if (n > 0)
            {
                bodies.AppendLine($"    Dn2CppInterpSlot s[{n}];");
                for (int i = 0; i < n; i++)
                {
                    string m = SlotMember(paramCs[i])!;
                    bodies.AppendLine(m == "ref"
                        ? $"    s[{i}].ref = (void*)a{i};"
                        : $"    s[{i}].{m} = a{i};");
                }
            }
            string sArg = n > 0 ? "s" : "nullptr";
            string tcall = $"dn2cpp_interp_dgcall(__closure, {sArg}, {n})";
            if (retC == "void")
                bodies.AppendLine($"    {tcall};");
            else if (SlotMember(retC) == "ref")
                bodies.AppendLine($"    return ({retC}){tcall}.ref;");
            else
                bodies.AppendLine($"    return ({retC}){tcall}.{SlotMember(retC)};");
            bodies.AppendLine("}");

            // The reverse invoke bridge: unpacks the argument slots into the
            // Invoke C++ ABI and dispatches through the delegate's own multicast
            // invoker, then packs the result back into a slot.
            string bridge = $"dn2cpp_dgbridge_{cls.CppName}";
            bodies.AppendLine($"static Dn2CppInterpSlot {bridge}(Dn2CppObject* __dg, const Dn2CppInterpSlot* __a, int32_t __n)");
            bodies.AppendLine("{");
            bodies.AppendLine("    (void)__a; (void)__n;");
            var callArgs = new List<string> { $"({cls.CppStructName}*)__dg" };
            for (int i = 0; i < n; i++)
            {
                string m = SlotMember(paramCs[i])!;
                callArgs.Add(m == "ref" ? $"({paramCs[i]})__a[{i}].ref" : $"({paramCs[i]})__a[{i}].{m}");
            }
            string invokeCall = $"dginvoke_{cls.CppName}({string.Join(", ", callArgs)})";
            if (retC == "void")
            {
                bodies.AppendLine($"    {invokeCall};");
                bodies.AppendLine("    return Dn2CppInterpSlot{};");
            }
            else
            {
                bodies.AppendLine("    Dn2CppInterpSlot __r{};");
                bodies.AppendLine(SlotMember(retC) == "ref"
                    ? $"    __r.ref = (void*){invokeCall};"
                    : $"    __r.{SlotMember(retC)} = {invokeCall};");
                bodies.AppendLine("    return __r;");
            }
            bodies.AppendLine("}");

            thunkRows.Add($"{{ {TypeInfoRef(cls, "hot-update delegate interpreter thunk row")}, (const void*)&{thunk} }}");
            bridgeRows.Add($"{{ {TypeInfoRef(cls, "hot-update delegate interpreter bridge row")}, (const void*)&{bridge} }}");
        }

        sb.AppendLine("// ---- hot-update delegate interpreter bridges (docs/BPI-FORMAT.md) ----");
        sb.Append(bodies);
        int thunkCount = thunkRows.Count;
        int bridgeCount = bridgeRows.Count;
        if (thunkRows.Count == 0)
            thunkRows.Add("{ nullptr, nullptr }"); // an empty array is ill-formed C++
        if (bridgeRows.Count == 0)
            bridgeRows.Add("{ nullptr, nullptr }");
        sb.AppendLine($"extern \"C\" const Dn2CppN2MDelegate dn2cpp_n2m_delegate_thunks[] = {{ {string.Join(", ", thunkRows)} }};");
        sb.AppendLine($"extern \"C\" const int32_t dn2cpp_n2m_delegate_thunk_count = {thunkCount};");
        sb.AppendLine($"extern \"C\" const Dn2CppN2MDelegate dn2cpp_dg_invoke_bridges[] = {{ {string.Join(", ", bridgeRows)} }};");
        sb.AppendLine($"extern \"C\" const int32_t dn2cpp_dg_invoke_bridge_count = {bridgeCount};");
        sb.AppendLine();
    }

    private void EmitBlobs(CppOutput o)
    {
        if (_literals.Blobs.Count == 0)
            return;
        // Array-init blobs are referenced by method bodies in any TU (array initializers),
        // so they get external linkage: a header `extern const` declaration makes the
        // definition external (a namespace-scope const is internal-linkage by default).
        o.Header.AppendLine("// ---- static array-init blobs (RVA field data) ----");
        o.Data.AppendLine("// ---- static array-init blobs (RVA field data) ----");
        for (int i = 0; i < _literals.Blobs.Count; i++)
        {
            o.Header.AppendLine($"extern const uint8_t blob_{i}[];");
            o.Data.AppendLine($"const uint8_t blob_{i}[] = {{ {string.Join(", ", _literals.Blobs[i].Select(b => "0x" + b.ToString("x2")))} }};");
        }
        o.Header.AppendLine();
        o.Data.AppendLine();
    }

    private void EmitStringLiterals(CppOutput o)
    {
        var sb = o.Data;
        // .NET strings are UTF-16; emit each literal as a static char16_t array
        // of code units (a numeric initializer sidesteps escaping concerns). The raw
        // str_data_N arrays and the init function are referenced only within this data TU,
        // so they stay file-local; the resulting str_N pointers are read by method bodies
        // in any TU, so they get external linkage + a header declaration.
        o.Header.AppendLine("// ---- string literals ----");
        for (int i = 0; i < _literals.Literals.Count; i++)
            o.Header.AppendLine($"extern Dn2CppString* str_{i};");
        o.Header.AppendLine();
        sb.AppendLine("// ---- string literals (UTF-16 code units) ----");
        for (int i = 0; i < _literals.Literals.Count; i++)
        {
            string lit = _literals.Literals[i];
            if (IsPlainAsciiLiteral(lit))
                sb.AppendLine($"static const char16_t str_data_{i}[] = u\"{lit}\";");
            else
                sb.AppendLine($"static const char16_t str_data_{i}[] = {{ {EncodeUtf16(lit)} }};");
        }
        for (int i = 0; i < _literals.Literals.Count; i++)
            sb.AppendLine($"Dn2CppString* str_{i};");
        sb.AppendLine();
        sb.AppendLine("static void dn2cpp_init_strings()");
        sb.AppendLine("{");
        for (int i = 0; i < _literals.Literals.Count; i++)
            sb.AppendLine($"    str_{i} = dn2cpp_string_literal(str_data_{i}, {_literals.Literals[i].Length});");
        sb.AppendLine("}");
        sb.AppendLine();
    }


    /// <summary>Closes the field types of a class the STRUCT ORDERING reaches and
    /// nothing else does. TopoOrder visits a SharedOwner unconditionally, so a canonical
    /// owner pulled in through an opaque referenced-only type's base chain lands in
    /// neither <see cref="_emit"/> nor <see cref="_opaque"/> — and EmitStructs still
    /// writes its FULL layout, spelling every field type by name while the only field
    /// closure (ComputeEmitted's walk) never saw it. Those names must be declared.
    ///
    /// <para>The owner itself stays non-opaque: a grouped alias's type-info stamps
    /// <c>sizeof(t_owner)</c> as its instance size, so shelling the owner instead would
    /// silently shrink every member of its group.</para></summary>
    private void CloseTopoOnlyLayouts()
    {
        // An opaque shell for a by-value field type IS the owner's layout, so a shell of
        // unknown extent would silently shrink the owner rather than only itself.
        bool Stub(ClassInfo owner, FieldInfo f, ClassInfo t, bool directByValue)
        {
            bool added = EmitAdd(t);
            if (directByValue && (added || _opaque.Contains(t))
                && TryModeledStructLayout(t) is null)
                throw new NotSupportedException(
                    $"{owner.FullName}.{f.Name}: by-value field type {t.FullName} is reached "
                    + "only by the struct ordering, and its size or alignment cannot be "
                    + "modeled at both pointer widths, so no opaque stub can preserve its layout");
            if (!added)
                return false;
            _opaque.Add(t);
            return true;
        }

        bool grew = true;
        while (grew)
        {
            grew = false;
            // A stub can float a further SharedOwner into the order, so this iterates
            // to a fixpoint over fresh snapshots.
            foreach (var c in TopoOrder().ToList())
            {
                if (_emit.Contains(c) || _opaque.Contains(c)
                    || c.IsEnum || c.IsDelegate || c.SharedOwner is not null)
                    continue;
                if (!c.ShapeReady)
                    throw new InvalidOperationException(
                        $"emit protocol: topo-only layout owner {c.FullName} has incomplete shape");
                foreach (var f in c.Fields)
                {
                    if (f.IsStatic)
                        continue;
                    var t = f.Type;
                    bool directByValue = t is
                        { Kind: TypeKind.Class, Class: { IsValueType: true } };
                    while (t is { Kind: TypeKind.ByRef or TypeKind.Pointer, Element: { } el })
                        t = el;
                    if (t is not { Kind: TypeKind.Class, Class: { IntrinsicCppName: null, IsEnum: false } ft })
                        continue;
                    grew |= Stub(c, f, ft, directByValue);
                    // Mirrors the referenced-only path: a stub's own struct name may
                    // redirect to a canonical owner, and its bases back isinst.
                    if (ft.SharedOwner is { } owner && EmitAdd(owner))
                    {
                        _opaque.Add(owner);
                        grew = true;
                    }
                    for (var bc = ft.BaseClass; bc is { IntrinsicCppName: null, IsEnum: false }; bc = bc.BaseClass)
                        if (EmitAdd(bc))
                        {
                            _opaque.Add(bc);
                            grew = true;
                        }
                }
            }
        }
        if (_c.SharedGenericsEnabled)
            _c.CompletePendingSpecializations();
    }

    private IEnumerable<ClassInfo> TopoOrder()
    {
        var visited = new HashSet<ClassInfo>();
        var result = new List<ClassInfo>();
        void Visit(ClassInfo c)
        {
            if (!visited.Add(c))
                return;
            // Opaque types root at Dn2CppObject, so don't pull their real base
            // (which may be an unemitted/intrinsic-generic type) into the order.
            if (c.BaseClass is not null && !IsOpaque(c))
                Visit(c.BaseClass);
            // A grouped specialization's struct definition is its canonical
            // owner's — order the owner ahead of the alias's dependents.
            if (ClassInfo.ShareStructLayout && c.SharedOwner is not null)
                Visit(c.SharedOwner);
            result.Add(c);
        }
        foreach (var c in EmittedClasses)
            Visit(c);
        return result;
    }

    // True when the string can be emitted verbatim inside a u"..." literal with
    // no escaping: every char printable ASCII (0x20-0x7E) excluding '"' and '\',
    // and short enough to dodge MSVC's ~16 KB single-literal-chunk limit. Both
    // renderings yield the same array layout (the numeric form appends an
    // explicit 0 terminator; u"..." an implicit one), and the runtime is always
    // passed the explicit length, so the choice is pure surface syntax.
    private static bool IsPlainAsciiLiteral(string s)
    {
        if (s.Length >= 4096)
            return false;
        foreach (char c in s)
        {
            if (c < 0x20 || c > 0x7E || c == '"' || c == '\\')
                return false;
        }
        return true;
    }

    // Renders a .NET string's UTF-16 code units as a C++ char16_t array body
    // (comma-separated hex), with a trailing NUL terminator.
    private static string EncodeUtf16(string s)
    {
        var sb = new StringBuilder(s.Length * 8 + 4);
        foreach (char c in s)
            sb.Append("0x").Append(((int)c).ToString("X4")).Append(", ");
        sb.Append('0');
        return sb.ToString();
    }
}

/// <summary>One unsupported-IL / unmapped-intrinsic hit recorded by the
/// self-hosting feasibility harness (<see cref="CppEmitter.MeasureGaps"/>).
/// <paramref name="Phase"/> is <c>reach</c> (reachability scan, in the Compilation
/// ctor), <c>compile</c> (method-body emit), <c>compile-spec</c> / <c>layout</c>
/// (specialization completion / layout closure), or <c>dangling</c> (the
/// dangling-symbol sweep). <paramref name="CppSymbol"/> is the gapped method's
/// own C++ symbol when the row has one — not written to the TSV; it feeds the sweep's
/// defined-set union, so a dropped body's callers are not reported as dangling.
/// Decode-free like the sweep itself (CppName reads neither Signature nor Methods).</summary>
internal readonly record struct MeasureGap(string Phase, string Namespace, string Method, string ExceptionType, string Message, string? CppSymbol = null)
{
    internal static MeasureGap From(string phase, MethodInfo? m, Exception ex)
    {
        if (m is null)
            return new MeasureGap(phase, "<specialization>", "<specialization>", ex.GetType().Name, ex.Message);
        string full = m.DeclaringClass.FullName;
        int dot = full.LastIndexOf('.');
        string ns = dot > 0 ? full.Substring(0, dot) : "<global>";
        return new MeasureGap(phase, ns, $"{full}.{m.Name}", ex.GetType().Name, ex.Message, m.CppName);
    }

    /// <summary>A dangling-symbol row: some body named <paramref name="callee"/>'s
    /// C++ symbol, and neither a compiled nor a dropped body defines it — the asymmetry
    /// AssertCalledBodiesEmitted fails a real transpile on, reported here as a row because
    /// <c>--measure</c>'s contract is to keep draining. Keyed on the CALLEE (the row names
    /// the cut body, like the assert's diagnostic); the message carries the witness caller.</summary>
    internal static MeasureGap Dangling(MethodInfo callee, MethodInfo caller)
    {
        string full = callee.DeclaringClass.FullName;
        int dot = full.LastIndexOf('.');
        string ns = dot > 0 ? full.Substring(0, dot) : "<global>";
        return new MeasureGap("dangling", ns, $"{full}.{callee.Name}", "DanglingSymbol",
            $"cut => route violation: {caller.DeclaringClass.FullName}.{caller.Name} names "
            + $"{callee.CppName}, defined by no compiled or dropped body — the C++ link "
            + "would fail on the mangled name");
    }
}

/// <summary>Result of <see cref="CppEmitter.MeasureGaps"/>: how many reachable method
/// bodies were attempted / compiled cleanly, the per-method gaps for the rest, and how
/// many named body symbols the dangling-symbol sweep diffed — null when the sweep was
/// skipped because a <c>compile-spec</c> gap truncated the body pass (bodies dropped
/// with no row to union make the diff unsound).</summary>
internal sealed record MeasureResult(int Attempted, int Compiled, List<MeasureGap> Gaps, int? NamedSymbols);

/// <summary>Sink for the multi-file C++ emission. Splits the program across:
/// <see cref="Header"/> (generated.h — managed type layouts plus external-linkage
/// declarations of every symbol method bodies reference across translation units),
/// the primary <see cref="Data"/> TU (generated.cpp — the global data definitions, the
/// first slice of method bodies and the backend entry point), and zero or more overflow
/// TUs. Method bodies are appended via <see cref="AppendBody"/>, which peels the
/// overflow past a byte budget into body chunks; per-class metadata blocks are appended
/// via <see cref="AppendMetadata"/>, which peels into its own chunk stream — so a large
/// program compiles both its bodies and its reflection metadata as parallel translation
/// units.
///
/// <para>A chunk is WRITTEN AND RELEASED the moment it is sealed — as soon as the next
/// one opens, since nothing ever appends to a chunk again. Nearly all of a large
/// program's text lives in chunks, so holding them to the end would put the whole
/// generated program on the heap at once, in UTF-16, at twice its byte size. At most one
/// chunk of each stream is live, whatever the size of the program.</para>
///
/// <para>That is what forces the naming: a single shared number sequence cannot be
/// assigned until BOTH streams are finished, while metadata is emitted first in time. Two
/// disjoint name spaces (<c>generated_b{k}.cpp</c>, <c>generated_m{j}.cpp</c>) name a
/// chunk at the instant it is sealed, in any emission order. Both still match the build's
/// <c>generated*.cpp</c> glob.</para>
///
/// <para>The roll boundaries are a pure function of deterministic text sizes, so the
/// managed and self-hosted transpilers still peel chunks identically — the self-host
/// byte-for-byte fixpoint is unaffected.</para></summary>
internal sealed class CppOutput
{
    // A block that crosses this budget stays whole; the next block opens a fresh
    // TU. A non-positive budget disables splitting (everything stays in Data).
    private readonly int _splitBytes;
    // (fileName, text) -> disk. Injected so CppOutput owns no I/O policy and the
    // emitter stays testable; TranspileDriver supplies the real writer.
    private readonly Action<string, string> _write;

    public StringBuilder Header { get; } = new();
    public StringBuilder Data { get; } = new();

    // The one open chunk of each stream; null until that stream's first block.
    private StringBuilder? _bodyCur;
    private int _bodyBytes;
    private StringBuilder? _metaCur;
    private int _metaBytes;
    // The two [HotPath] TUs; buffered whole until SealChunks (see AppendHotBody).
    // They partition the marked set — FastMath routes here, everything else to
    // _hotCur — so no body is ever defined in both.
    private StringBuilder? _hotCur;
    private StringBuilder? _hotFastCur;

    public CppOutput(int splitBytes, Action<string, string> write)
    {
        _splitBytes = splitBytes;
        _write = write;
    }

    /// <summary>Body-only translation units peeled so far (generated_b1..K.cpp).</summary>
    public int BodyChunks { get; private set; }

    /// <summary>Metadata-only translation units peeled so far (generated_m1..J.cpp).</summary>
    public int MetadataChunks { get; private set; }

    /// <summary>1 once a [HotPath] TU (generated_hot.cpp) has been opened, else 0.</summary>
    public int HotChunks { get; private set; }

    /// <summary>1 once a [HotPath(FastMath)] TU (generated_hot_fast.cpp) has been
    /// opened, else 0.</summary>
    public int HotFastChunks { get; private set; }

    private static string BodyName(int k) => $"generated_b{k}.cpp";
    private static string MetadataName(int j) => $"generated_m{j}.cpp";

    /// <summary>Append one method body, opening a fresh body-only TU when the current
    /// chunk has crossed the byte budget — and sealing (writing out and dropping) the one
    /// it replaces.
    /// <para>Bodies never share generated.cpp. They are routed here as they compile,
    /// before the data definitions generated.cpp must place ahead of them even exist —
    /// and they need nothing from it: a body can land in any TU, so it only ever names
    /// symbols the header declares. Only single-TU mode
    /// (<c>DN2CPP_SPLIT_BYTES=0</c>) puts bodies in generated.cpp, and it does not come
    /// through here at all; the emitter replays them into Data itself.</para></summary>
    public void AppendBody(string body)
    {
        if (_bodyCur is null || _bodyBytes > _splitBytes)
        {
            if (_bodyCur is not null)   // the chunk being replaced is final
                _write(BodyName(BodyChunks), _bodyCur.ToString());
            var next = new StringBuilder();
            next.AppendLine("#include \"generated.h\"");
            _bodyCur = next;
            _bodyBytes = 0;
            BodyChunks++;
        }
        _bodyCur.AppendLine(body);
        _bodyBytes += body.Length + 1;
    }

    /// <summary>Append one [HotPath] method body to the dedicated hot TU
    /// (<c>generated_hot.cpp</c>) — the fixed-name unit runtime/CMakeLists.txt
    /// attaches stronger per-file optimization flags to, and one TU on purpose:
    /// marked kernels calling each other stay visible to the optimizer. Buffered
    /// whole until <see cref="SealChunks"/> rather than streamed — like the
    /// inline-promoted bodies, the marked set is a user-selected handful of
    /// kernels, never the whole program's text, so the no-full-program-buffer
    /// streaming rule is not at stake.</summary>
    public void AppendHotBody(string body)
    {
        if (_hotCur is null)
        {
            _hotCur = new StringBuilder();
            _hotCur.AppendLine("#include \"generated.h\"");
            HotChunks = 1;
        }
        _hotCur.AppendLine(body);
    }

    /// <summary>Append one <c>[HotPath(FastMath = true)]</c> method body to the
    /// second hot TU (<c>generated_hot_fast.cpp</c>), which carries the plain hot
    /// TU's flags plus relaxed floating-point semantics. It is a separate unit
    /// rather than a flag on the first because <c>-ffast-math</c> / <c>/fp:fast</c>
    /// is a translation-unit property: a body that opts out of IEEE-exact
    /// arithmetic must not drag every other marked kernel with it. Buffered whole
    /// for the reason given at <see cref="AppendHotBody"/>.</summary>
    public void AppendHotFastBody(string body)
    {
        if (_hotFastCur is null)
        {
            _hotFastCur = new StringBuilder();
            _hotFastCur.AppendLine("#include \"generated.h\"");
            HotFastChunks = 1;
        }
        _hotFastCur.AppendLine(body);
    }

    /// <summary>Whether the next <see cref="AppendMetadata"/> call will open a fresh
    /// metadata chunk. The roll predicate deliberately ignores the incoming block (it
    /// looks only at bytes already appended), so a caller can consult this BEFORE
    /// rendering a block and reset any per-chunk state (the emitter's per-chunk
    /// static-thunk dedup set) knowing the verdict cannot change by append time —
    /// only AppendMetadata itself mutates the counters.</summary>
    public bool MetadataWillRoll => _splitBytes > 0 && (_metaCur is null || _metaBytes > _splitBytes);

    /// <summary>Append one self-contained metadata block (a class's complete metadata:
    /// vtable, interface/field/member tables, thunks, ti_/ty_ definitions), opening a
    /// fresh metadata-only TU first when the current chunk has already crossed the byte
    /// budget (see <see cref="MetadataWillRoll"/>) and sealing the one it replaces. A
    /// block is never split across chunks. With splitting disabled the block lands in
    /// <see cref="Data"/>, exactly like the pre-split single-TU layout.</summary>
    public void AppendMetadata(string block)
    {
        if (_splitBytes <= 0)
        {
            Data.AppendLine(block);
            return;
        }
        if (MetadataWillRoll)
        {
            if (_metaCur is not null)
                _write(MetadataName(MetadataChunks), _metaCur.ToString());
            var next = new StringBuilder();
            next.AppendLine("#include \"generated.h\"");
            _metaCur = next;
            _metaBytes = 0;
            MetadataChunks++;
        }
        _metaCur!.AppendLine(block);
        _metaBytes += block.Length + 1;
    }

    /// <summary>Writes out the still-open chunk of each stream. Called once, at the end
    /// of emission; Header and Data are the driver's to write.</summary>
    public void SealChunks()
    {
        if (_bodyCur is not null)
        {
            _write(BodyName(BodyChunks), _bodyCur.ToString());
            _bodyCur = null;   // idempotent: a second SealChunks writes nothing
        }
        if (_metaCur is not null)
        {
            _write(MetadataName(MetadataChunks), _metaCur.ToString());
            _metaCur = null;
        }
        if (_hotCur is not null)
        {
            _write("generated_hot.cpp", _hotCur.ToString());
            _hotCur = null;
        }
        if (_hotFastCur is not null)
        {
            _write("generated_hot_fast.cpp", _hotFastCur.ToString());
            _hotFastCur = null;
        }
    }
}

/// <summary>What <see cref="CppEmitter.Emit"/> leaves for the driver to write: the
/// shared <c>generated.h</c> and the primary <c>generated.cpp</c>, both still
/// unmaterialized builders (the driver ToString()s and releases one at a time), plus how
/// many chunks Emit already streamed out — the driver needs the counts only to sweep any
/// stale chunk a previous, larger run left behind.
/// <para><paramref name="HotChunks"/> is 1 when a <c>[HotPath]</c> TU
/// (generated_hot.cpp) was written, <paramref name="HotFastChunks"/> 1 when the
/// <c>[HotPath(FastMath)]</c> TU (generated_hot_fast.cpp) was. Both sweeps are
/// rewrite-or-remove on a fixed name (the driver deletes them up front), so the
/// counts only feed the console chunk total.</para>
/// <para><paramref name="BaseAbiJson"/> is the base-abi.json sidecar body of a
/// <c>--hotupdate-base</c> build (null otherwise).</para></summary>
internal sealed record EmittedSources(
    StringBuilder Header, StringBuilder Data, int BodyChunks, int MetadataChunks,
    int HotChunks = 0, int HotFastChunks = 0, string? BaseAbiJson = null);
