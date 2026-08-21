using System.Reflection.Metadata;
using SRME = System.Reflection.Metadata.Ecma335.MetadataTokens;

namespace Dn2Cpp;

internal sealed partial class Compilation
{
    // ---- canonical shared generics (layout-coinciding instantiation grouping) ----

    /// <summary>The compilation's canonicalizer (placeholders are interned per
    /// instance, but the string-keyed instantiation cache makes canonical
    /// instantiations agree across instances anyway).</summary>
    internal CanonicalGenerics Canon => _canon ??= new CanonicalGenerics(this);
    private CanonicalGenerics? _canon;
    // How far into Classes the canonical-linkage examination has run (incremental:
    // late instantiations discovered during body emission are linked too). A cursor
    // rather than a scanned-set because Classes is append-only, so "not yet examined"
    // IS the tail past this index — the same property CppEmitter's EmittedClasses
    // cache rests on.
    private int _canonLinkCursor;
    // Specializations linked to a canonical owner whose already-reachable methods have
    // not yet been given their owner counterparts. Only a link made AFTER a method was
    // reached needs that catch-up — Reach reaches the counterpart of every method it
    // admits to an already-grouped class. See SyncSharedGenerics.
    private readonly List<ClassInfo> _canonCounterpartPending = new();

    /// <summary>Groups every not-yet-examined closed specialization whose
    /// canonicalized argument vector differs from its real one under the
    /// canonical owner instantiation: instantiates the owner (a fresh
    /// specialization over the placeholder arguments, reusing the ordinary
    /// monomorphization machinery and cache) and records the
    /// <see cref="ClassInfo.SharedOwner"/> / <see cref="ClassInfo.SharedUsers"/>
    /// linkage. Then verifies each new group member's vtable slot layout against
    /// its owner's and drops the link for any class whose shape diverges (an
    /// overload pair like <c>Foo(T)</c>/<c>Foo(object)</c> can merge into one
    /// slot under canonicalization while staying distinct in a real
    /// instantiation — such a class falls back to its own bodies wholesale).
    /// Incremental: safe to call repeatedly as new instantiations appear.</summary>
    private void LinkCanonicalOwners()
    {
        var canonicalizer = Canon;
        var newlyLinked = new List<ClassInfo>();
        // Fix the end here: instantiating an owner below appends new (already-canonical)
        // specializations to Classes, and they need no linkage of their own. Anything past
        // this point is left for the next call, which the cursor picks up.
        int end = Classes.Count;
        for (int c = _canonLinkCursor; c < end; c++)
        {
            var spec = Classes[c];
            if (spec.GenericArity == 0 || spec.Context.TypeArgs.Length != spec.GenericArity
                || spec.IntrinsicCppName is not null)
                continue;
            var args = spec.Context.TypeArgs;
            if (args.Any(ContainsOpenGenerics))
                continue;
            var canon = new TypeDesc[args.Length];
            bool changed = false;
            for (int i = 0; i < args.Length; i++)
            {
                canon[i] = canonicalizer.CanonicalForm(args[i]);
                changed |= !ReferenceEquals(canon[i], args[i]);
            }
            if (!changed)
                continue;
            var owner = Instantiate(spec.Module, spec.Handle, canon);
            if (owner == spec || owner.IntrinsicCppName is not null)
                continue;
            spec.SharedOwner = owner;
            owner.AddSharedUser(spec);
            newlyLinked.Add(spec);
        }
        _canonLinkCursor = end;
        _canonCounterpartPending.AddRange(newlyLinked);
        // Freshly instantiated owners are shells; give them at least their shape, as the
        // drain would have.
        CompletePendingSpecializations();
        // Check the links whose users already have a vtable. The rest are checked when
        // they get one — see VerifyCanonicalLink for why deferring is sound, and why it
        // matters.
        foreach (var spec in newlyLinked)
            if (spec.MembersReady)
                VerifyCanonicalLink(spec);
    }

    /// <summary>Links each new generic-method instantiation to its canonical
    /// counterpart: the same template instantiated on the declaring class's
    /// canonical owner (or the class itself when ungrouped) at canonicalized
    /// method arguments. The method-dimension analog of
    /// <see cref="LinkCanonicalOwners"/>, run after it each sync round so the
    /// class dimension is settled first. GVM-shaped instantiations stay
    /// per-instantiation (their type-switch dispatchers bind real signatures),
    /// and a counterpart whose signature fails to decode leaves the
    /// instantiation unlinked — no sharing rather than wrong sharing.</summary>
    private void LinkCanonicalMethodOwners()
    {
        while (_methodInstanceLinkCursor < _methodInstanceOrder.Count)
            LinkCanonicalMethodInstance(_methodInstanceOrder[_methodInstanceLinkCursor++]);
    }

    /// <summary>Links one generic-method instantiation to its canonical
    /// counterpart (idempotent; see <see cref="LinkCanonicalMethodOwners"/>).
    /// Also called by the emission-pass rgctx fill: resolving a forwarding
    /// slot under a real user's context can instantiate a real generic method
    /// for the first time (every real caller's body was shared, so no
    /// per-instantiation compile ever resolved the spec), and the sync pass
    /// that normally links no longer runs by then.</summary>
    private void LinkCanonicalMethodInstance(MethodInfo m)
    {
        if (m.SharedOwner is not null || m.Rva == 0 || (m.IsVirtual && m.VtableSlot < 0))
            return;
        var args = m.Context.MethodArgs;
        if (args.Any(ContainsOpenGenerics))
            return;
        var canonDecl = m.DeclaringClass.SharedOwner ?? m.DeclaringClass;
        var canon = new TypeDesc[args.Length];
        bool changed = !ReferenceEquals(canonDecl, m.DeclaringClass);
        for (int i = 0; i < args.Length; i++)
        {
            canon[i] = Canon.CanonicalForm(args[i]);
            changed |= !ReferenceEquals(canon[i], args[i]);
        }
        if (!changed)
            return;
        MethodInfo counterpart;
        try
        {
            counterpart = InstantiateMethodOnClass(canonDecl, m.Module, m.Handle, canon);
            // The decode is what this catch is for. InstantiateMethodOnClass does not read
            // MethodInfo.Signature, so ask for it here — otherwise the link is made anyway
            // and the failure resurfaces somewhere that can no longer choose no-sharing
            // over wrong-sharing.
            counterpart.EnsureSignature();
        }
        catch (NotSupportedException)
        {
            return;
        }
        if (ReferenceEquals(counterpart, m))
            return;
        m.SharedOwner = counterpart;
        counterpart.AddSharedUser(m);
    }

    /// <summary>Current emit-pipeline phase (see <see cref="EmitPhase"/>). Owned here
    /// because every phase-sensitive mutation lives on the compilation; the emitter
    /// drives the transitions at its pipeline boundaries. Per-instance, because the
    /// transpiler builds more than one compilation in one process (see
    /// <see cref="Module.Owner"/>).</summary>
    internal EmitPhase Phase { get; private set; } = EmitPhase.Discovery;

    /// <summary>The only mutator of <see cref="Phase"/>. The legal edges are exactly the
    /// pipelines that exist: Emit with shared generics walks Discovery → LayoutClosure →
    /// Planning → Finalized → Emission; <c>--measure</c> skips LayoutClosure (its guarded
    /// closure runs last, inside Emission); with shared generics off both jump Discovery
    /// → Emission (no planning protocol exists to misorder). Anything else is a
    /// transpiler bug: throw <see cref="InvalidOperationException"/> and crash raw.</summary>
    internal void EnterPhase(EmitPhase next)
    {
        bool legal = next switch
        {
            EmitPhase.LayoutClosure => Phase == EmitPhase.Discovery,
            EmitPhase.Planning => Phase is EmitPhase.Discovery or EmitPhase.LayoutClosure,
            EmitPhase.Finalized => Phase == EmitPhase.Planning,
            EmitPhase.Emission => Phase is EmitPhase.Discovery or EmitPhase.Finalized,
            _ => false,
        };
        if (!legal)
            throw new InvalidOperationException(
                $"emit protocol: illegal phase transition {Phase} -> {next}");
        Phase = next;
    }

    /// <summary>Drives the canonical-linkage / reachability fixpoint: link any
    /// new specializations, reach the owner counterpart of every reachable
    /// grouped method (its body is the sharing candidate), and drain the
    /// discovery queues — until nothing new appears. Called at the end of
    /// <c>Build</c> and again each body-emission round, so instantiations first
    /// discovered while compiling bodies are grouped and their owner bodies
    /// compiled too.</summary>
    internal void SyncSharedGenerics()
    {
        if (!SharedGenericsEnabled)
            return;
        // Grouping is legal only while the verdicts are still forming: Build's own call
        // (Discovery) and every planning round. After FinalizeSharedGenerics the
        // SharedImpl assignments are final, so a later sync would link instantiations
        // the cascade/taint fixpoint never saw — quietly changing which bodies share.
        // (The emission rounds re-discover instantiations too, but those group nothing:
        // they emit as themselves, which is why the layout closure must run before
        // planning in the first place.)
        if (Phase is not (EmitPhase.Discovery or EmitPhase.Planning))
            throw new InvalidOperationException(
                $"emit protocol: SyncSharedGenerics in phase {Phase} (legal: Discovery, Planning)");
        while (true)
        {
            int classesBefore = Classes.Count;
            int reachableBefore = Reachable.Count;
            LinkCanonicalOwners();
            LinkCanonicalMethodOwners();
            // Catch-up for the one case Reach cannot cover: a specialization whose methods
            // were already reachable when the link was made. Reach reaches the counterpart
            // of every method it admits, so a class linked before its methods arrive needs
            // nothing here — which is why the delta is the newly-linked classes and not the
            // whole of Classes. A grouped specialization whose members were never decoded
            // has no reachable method to find a counterpart for (reaching a method means
            // having resolved it, and resolving it is what decodes them), so it is carried
            // to a later round rather than answered now; reading .Methods to ask anyway
            // would decode every instantiation in the program.
            int keep = 0;
            for (int i = 0; i < _canonCounterpartPending.Count; i++)
            {
                var cls = _canonCounterpartPending[i];
                if (cls.SharedOwner is null)
                    continue;   // link dropped by the vtable-shape check
                if (!cls.MembersReady)
                {
                    _canonCounterpartPending[keep++] = cls;
                    continue;
                }
                foreach (var m in cls.Methods.ToList())
                    if (Reachable.Contains(m))
                        ReachSharedCounterpart(m);
            }
            _canonCounterpartPending.RemoveRange(keep, _canonCounterpartPending.Count - keep);
            // Method-dimension counterparts: a reachable linked instantiation's
            // canonical body is the sharing candidate, exactly like the class
            // counterparts above (index loop: reaching can grow the list).
            for (int i = 0; i < _methodInstanceOrder.Count; i++)
            {
                var m = _methodInstanceOrder[i];
                if (m.SharedOwner is { } owner && Reachable.Contains(m))
                    Reach(owner);
            }
            DrainReachability();
            if (Classes.Count == classesBefore && Reachable.Count == reachableBefore)
                break;
        }
    }

    /// <summary>Reaches the canonical owner's counterpart (same template method
    /// handle) of a grouped specialization's reachable method, so the shared
    /// body candidate is scanned and compiled. Generic-method instantiations
    /// link through their own <see cref="MethodInfo.SharedOwner"/> path (the
    /// sync loop reaches those counterparts separately); static constructors
    /// never share (statics stay per real instantiation by design).</summary>
    private void ReachSharedCounterpart(MethodInfo m)
    {
        if (m.NameSuffix != "" || m.Name == ".cctor" || m.Rva == 0)
            return;
        if (m.DeclaringClass.SharedOwner is not { } owner)
            return;
        // The owner's counterpart is about to be compiled, so it has to exist. Owners are
        // one per group, not one per instantiation — this is the pull the sharing machinery
        // genuinely needs, and the only one.
        EnsureCompleted(owner);
        if (owner.MethodByTemplate.TryGetValue(m.Handle, out var counterpart))
            Reach(counterpart);
    }

    /// <summary>Whether the type is, or transitively contains, a canonical
    /// placeholder — i.e. belongs to the synthetic canonical-owner world rather
    /// than a real instantiation.</summary>
    internal static bool ContainsCanonPlaceholder(TypeDesc t) => t.IsCanonPlaceholder || t.Kind switch
    {
        TypeKind.SZArray or TypeKind.MDArray or TypeKind.ByRef or TypeKind.Pointer =>
            ContainsCanonPlaceholder(t.Element!),
        TypeKind.Class => ContainsCanonPlaceholder(t.Class!),
        _ => false,
    };

    /// <summary>Whether the class is a canonical-owner-world specialization
    /// (any of its type arguments transitively carries a placeholder).</summary>
    internal static bool ContainsCanonPlaceholder(ClassInfo c) =>
        c.Context.TypeArgs.Any(ContainsCanonPlaceholder);

    /// <summary>Whether the type is, or transitively contains, a per-index ANY
    /// placeholder ($CnAnyN) — i.e. belongs to a runtime-instantiation TEMPLATE.
    /// A $CnAny token folds nothing (it stands for value and reference arguments
    /// alike), so its consumers strip the token and run the runtime read.</summary>
    internal static bool ContainsCanonAny(TypeDesc t) => t.CanonAnyIndex >= 0 || t.Kind switch
    {
        TypeKind.SZArray or TypeKind.MDArray or TypeKind.ByRef or TypeKind.Pointer =>
            ContainsCanonAny(t.Element!),
        TypeKind.Class => t.Class!.Context.TypeArgs.Any(ContainsCanonAny),
        _ => false,
    };

    /// <summary>Whether the type transitively carries the REFERENCE placeholder
    /// (CnRef) specifically — the fold-agreement rules for a poisoned typeof are
    /// stricter for it than for the enum-width placeholders (see
    /// <c>MethodCompiler.IsPlaceholderAgnosticFold</c>).</summary>
    internal static bool ContainsRefPlaceholder(TypeDesc t) =>
        // A $CnAny is Primitive-Object-shaped but is NOT the reference
        // placeholder: it stands for value arguments too, so no CnRef fold
        // agreement holds for it.
        (t.IsCanonPlaceholder && t.IsObject && t.CanonAnyIndex < 0) || t.Kind switch
        {
            TypeKind.SZArray or TypeKind.MDArray or TypeKind.ByRef or TypeKind.Pointer =>
                ContainsRefPlaceholder(t.Element!),
            TypeKind.Class => t.Class!.Context.TypeArgs.Any(ContainsRefPlaceholder),
            _ => false,
        };

    /// <summary>Whether the method belongs to the canonical-owner world: its
    /// declaring class or its own generic method arguments carry a placeholder.
    /// Such a method compiles only as a shared-body candidate (trial compile
    /// with taint checks); it is emitted only when a real grouped method binds
    /// to it (directly or via the direct-call closure).</summary>
    internal static bool IsCanonicalMethod(MethodInfo m) =>
        ContainsCanonPlaceholder(m.DeclaringClass)
        || m.Context.MethodArgs.Any(ContainsCanonPlaceholder);

    /// <summary>The method that donates the shared body for a canonical-world
    /// target: a partially-canonical satellite (e.g.
    /// <c>Dictionary&lt;CnInt32,String&gt;</c>, instantiated while compiling an
    /// enum-canonical world whose token spells <c>Dictionary&lt;T,string&gt;</c>)
    /// is itself grouped under the fully canonical owner, and only that owner's
    /// counterpart compiles/emits — a shared body binding the satellite's own
    /// symbol would duplicate the donor and read mismatched rgctx slot indices
    /// off real receivers. Generic-method instantiations redirect the same way
    /// through their own canonical link (a satellite instantiation created
    /// inside a partially-canonical world walks to the fully canonical
    /// instantiation). Identity for real methods, fully-canonical methods and
    /// cctors.</summary>
    internal MethodInfo SharedDonor(MethodInfo m)
    {
        while (m.NameSuffix == "" && m.Name != ".cctor"
               && ContainsCanonPlaceholder(m.DeclaringClass)
               && m.DeclaringClass.SharedOwner is { } owner
               && owner.MethodByTemplate.TryGetValue(m.Handle, out var counterpart)
               && !ReferenceEquals(counterpart, m))
            m = counterpart;
        while (m.NameSuffix != "" && IsCanonicalMethod(m)
               && m.SharedOwner is { } methodOwner && !ReferenceEquals(methodOwner, m))
            m = methodOwner;
        return m;
    }

    /// <summary>Callee → the first emitted body that named its C++ symbol; null when the
    /// audit is off, which is the default. Feeds the named-symbol backstop
    /// (<c>CppEmitter.AssertCalledBodiesEmitted</c>), which exists because the method
    /// forward-declaration block — and therefore the set of symbols the linker will be
    /// asked to resolve — is spun out of the compiled bodies and nothing else. A body that
    /// names a symbol OUTSIDE that set is a transpile that SUCCEEDS and then fails in the
    /// C++ toolchain, which reports a mangled name and no cause: reachability cut an edge
    /// that emission still walks. Catching it here is the difference between a diagnostic
    /// that names the caller, the callee and the reach chain, and a link error that names
    /// none of them.
    ///
    /// Off during the planning pass on purpose — it compiles bodies and throws them away,
    /// so it can say nothing true about which symbols the OUTPUT names. <c>--measure</c>
    /// arms this half too (<see cref="BeginMeasureSymbolAudit"/>): it drops the bodies
    /// that fail, so their callers would look like they named a symbol nothing defines —
    /// but every drop leaves a gap row carrying the dropped body's symbol, and the
    /// dangling-symbol sweep (<c>CppEmitter.MeasureGaps</c>) unions those into the defined
    /// set before diffing. What survives is a real cut ⟹ route violation, reported as a
    /// <c>dangling</c> gap row rather than a failure.</summary>
    internal Dictionary<MethodInfo, MethodInfo>? NamedBodySymbols;

    /// <summary>The type-info half of the named-symbol backstop: every <c>ti_&lt;T&gt;</c> /
    /// <c>ti_arr_&lt;T&gt;</c> symbol an emitted body spells out (keyed by the symbol string,
    /// value the first body that named it) so <see cref="CppEmitter.AssertNamedTypeInfosDefined"/>
    /// can diff it against the type-info symbols the emission actually DEFINES. The method
    /// half (<see cref="NamedBodySymbols"/>) catches a cut-but-still-called body; this catches
    /// the sibling asymmetry — a lowering (castclass/isinst/box/typeof/array-covariance) that
    /// names a type's runtime type-info while nothing emits that <c>ti_</c>, which the
    /// transpile reports as success and the C++ link then fails on the mangled name with no
    /// cause attached. Armed by <see cref="BeginCallSymbolAudit"/> alone (real emission
    /// pass only; null under planning AND <c>--measure</c>, unlike the method half —
    /// measure emits no type-infos, so this half would have no defined set to diff
    /// against).</summary>
    internal Dictionary<string, MethodInfo>? NamedTypeInfoSymbols;

    /// <summary>The C++ struct-type half of the named-symbol backstop, the sibling of
    /// <see cref="NamedTypeInfoSymbols"/>: every <c>t_&lt;T&gt;</c> struct-type symbol an
    /// emitted body SPELLS OUT as a C++ type — a field-access receiver cast
    /// (<c>((t_&lt;T&gt;*)o)-&gt;f_…</c>), a dispatch function-pointer signature
    /// (<c>t_Ret (*)(t_Decl*, t_Param*)</c>), or a <c>sizeof(t_&lt;T&gt;)</c> over a value
    /// element — while nothing emits that struct. Its declaring type is reached through no
    /// emit edge (an intrinsic-modeled type a transpiled BCL body field-reads, an interface
    /// or value type named only in an interface method's dispatched signature, or a value
    /// element of an <c>Array.Empty&lt;T&gt;</c>), so <see cref="CppEmitter.EmittedClasses"/>
    /// never carries it and the C++ compile fails on an undeclared <c>t_</c> inside an
    /// all-green transpile. Armed and disarmed exactly like <see cref="NamedTypeInfoSymbols"/>
    /// (real emission pass only; null under planning / <c>--measure</c>).</summary>
    internal Dictionary<string, MethodInfo>? NamedStructSymbols;

    /// <summary>Arm the named-symbol audit. The emission pass calls this, once, before the
    /// only body compilation whose text reaches the output — so it is also Emit's
    /// Emission-phase transition point.</summary>
    internal void BeginCallSymbolAudit()
    {
        // The audit must arm exactly at the emission-pass boundary (Finalized → Emission,
        // or Discovery → Emission when shared generics are off). Armed any earlier, it
        // would record the planning pass's discarded bodies — callers that "name" symbols
        // nothing defines — and the backstop would fail correct programs.
        EnterPhase(EmitPhase.Emission);
        NamedBodySymbols = new Dictionary<MethodInfo, MethodInfo>();
        NamedTypeInfoSymbols = new Dictionary<string, MethodInfo>(StringComparer.Ordinal);
        NamedStructSymbols = new Dictionary<string, MethodInfo>(StringComparer.Ordinal);
        // The [HotPath(NoAlloc)] closure recorders arm together with the named-symbol
        // audit — same emission-pass boundary, same write-only "cannot change output"
        // contract — but ONLY when a NoAlloc method exists, so a program that uses none
        // pays nothing (null dictionaries, no body scans). NoAllocMethods is complete by
        // now: the planning pass compiled every reachable body, and MethodCompiler.Compile
        // touches IsHotPath, which decodes the bit and registers the root.
        if (NoAllocMethods.Count > 0)
        {
            HotCallEdges = new Dictionary<string, List<MethodInfo>>(StringComparer.Ordinal);
            HotBodyFacts = new Dictionary<string, HotBodyFacts>(StringComparer.Ordinal);
            HotIndirectCalls = new HashSet<string>(StringComparer.Ordinal);
        }
    }

    /// <summary>Arm the METHOD-symbol half of the audit alone, for <c>--measure</c>: the
    /// measure sweep's Emission-phase transition point, called once, after planning, before
    /// the only body pass whose recordings the dangling-symbol diff reads. The other halves
    /// stay null on purpose — measure emits no type-infos and no struct layouts, so their
    /// defined sets never exist and a recording would have nothing to diff against — and the
    /// [HotPath(NoAlloc)] recorders stay off with them (their verifier runs only in Emit).
    /// Recording is write-only and decode-free, so arming it cannot change which gap rows the
    /// body pass itself collects.</summary>
    internal void BeginMeasureSymbolAudit()
    {
        // Same boundary rule as BeginCallSymbolAudit: armed any earlier, the planning
        // pass's discarded bodies would record symbols and the sweep would report
        // dangling rows for correct programs.
        EnterPhase(EmitPhase.Emission);
        NamedBodySymbols = new Dictionary<MethodInfo, MethodInfo>();
    }

    /// <summary>Record that <paramref name="caller"/>'s body names <paramref name="callee"/>'s
    /// C++ method symbol, so some body must define it. Write-only: it cannot change what is
    /// emitted, and costs nothing at all while the audit is off. First caller wins — one
    /// witness is all a diagnostic needs, and the compile order it is picked out of is
    /// already content-derived, so the message stays a function of the input.</summary>
    internal void NoteNamedBodySymbol(MethodInfo caller, MethodInfo callee)
    {
        NamedBodySymbols?.TryAdd(callee, caller);
        // The same funnel is the direct-call closure the NoAlloc BFS walks. Keyed by
        // caller SYMBOL (not identity), because a minted twin absent from compiledMethods
        // shares its symbol with the compiled body that defines it — the reason the
        // named-symbol backstop compares strings too. The callee is the resolved mouth
        // (impl / Emittable), whose CppName is the key its body facts are recorded under.
        if (HotCallEdges is { } edges)
        {
            if (!edges.TryGetValue(caller.CppName, out var list))
                edges[caller.CppName] = list = new List<MethodInfo>();
            list.Add(callee);
        }
    }

    /// <summary>Record that <paramref name="caller"/>'s body names the type-info symbol
    /// <paramref name="expr"/> (an <c>&amp;ti_…</c> handle expression), so some type-info
    /// definition must define it. Write-only and decode-free — it reads the string a
    /// lowering already produced — so it cannot change what is emitted and costs nothing
    /// while the audit is off. A non-<c>&amp;ti_</c> expression (a runtime handle
    /// <c>&amp;dn2cpp_*_type</c>, an rgctx slot access under a shared trial, a nullptr fold)
    /// is not a per-type emitted symbol and is ignored. First witness wins, like
    /// <see cref="NoteNamedBodySymbol"/>.</summary>
    internal void NoteNamedTypeInfoSymbol(MethodInfo caller, string? expr)
    {
        if (NamedTypeInfoSymbols is not { } named
            || expr is null || !expr.StartsWith("&ti_", StringComparison.Ordinal))
            return;
        named.TryAdd(expr[1..], caller);
    }

    /// <summary>Record that <paramref name="caller"/>'s body names the C++ struct-type symbol
    /// <paramref name="sym"/> (a bare <c>t_&lt;T&gt;</c>, no <c>*</c> and no <c>&amp;</c>), so
    /// some type-layout emission must forward-declare it. Write-only and decode-free — it reads
    /// a <see cref="ClassInfo.CppStructName"/> the lowering already produced — so it cannot
    /// change what is emitted and costs nothing while the audit is off. A non-<c>t_</c> spelling
    /// (a runtime struct like <c>Dn2CppObject</c>, an <c>int32_t</c> enum lowering) is not a
    /// per-type emitted struct and is ignored. First witness wins, like
    /// <see cref="NoteNamedTypeInfoSymbol"/>.</summary>
    internal void NoteNamedStructSymbol(MethodInfo caller, string? sym)
    {
        if (NamedStructSymbols is not { } named
            || sym is null || !sym.StartsWith("t_", StringComparison.Ordinal))
            return;
        named.TryAdd(sym, caller);
    }

    // ---- [HotPath(NoAlloc)] closure verification (armed only when a NoAlloc method
    //      exists; see BeginCallSymbolAudit and CppEmitter.AssertNoAllocClosures) ----

    /// <summary>Every method carrying <c>[HotPath(NoAlloc = true)]</c>, appended once
    /// each by <see cref="MethodInfo.NoAlloc"/>'s decode (ComputeHotPathBits is cached).
    /// These are the roots whose direct-call closures the emit-time verifier walks. Not
    /// armed state — it must exist whenever a bit is decoded, which can happen before the
    /// emission pass.</summary>
    internal readonly List<MethodInfo> NoAllocMethods = new();

    /// <summary>Register a <c>[HotPath(NoAlloc)]</c> verification root.</summary>
    internal void NoteNoAllocMethod(MethodInfo m) => NoAllocMethods.Add(m);

    /// <summary>The direct-call closure edges the NoAlloc BFS follows: caller body
    /// CppName → the callee bodies it names. Null (unarmed) unless a NoAlloc method
    /// exists. Filled by <see cref="NoteNamedBodySymbol"/>, the mouth every emitted call
    /// edge already flows through (direct calls, ldftn/ldvirtftn, the async continuation,
    /// an intrinsic body delegating to a real one).</summary>
    internal Dictionary<string, List<MethodInfo>>? HotCallEdges;

    /// <summary>Per-emitted-body allocation/dispatch facts, keyed by body CppName. Filled
    /// by <see cref="RecordHotBodyFacts"/> as bodies stream through emission. Null unless
    /// a NoAlloc method exists.</summary>
    internal Dictionary<string, HotBodyFacts>? HotBodyFacts;

    /// <summary>Bodies that emit a <c>calli</c> (function-pointer) call, by CppName. That
    /// call's C++ form carries no distinctive runtime token to scan for, so
    /// <see cref="NoteHotIndirectCall"/> marks it here and RecordHotBodyFacts folds it into
    /// the body's dispatch verdict. Null unless a NoAlloc method exists.</summary>
    internal HashSet<string>? HotIndirectCalls;

    /// <summary>Mark that <paramref name="m"/>'s body makes a <c>calli</c> — a statically
    /// unprovable indirect call, i.e. dynamic dispatch. No-op while recording is off.</summary>
    internal void NoteHotIndirectCall(MethodInfo m) => HotIndirectCalls?.Add(m.CppName);

    // The tokens an emitted body text spells for a steady-state GC allocation. Curation
    // rule: a token is listed iff the helper CAN allocate a managed object, string or
    // array on some input (growth counts — a StringBuilder append that may reallocate the
    // buffer is "may allocate", which is the question NoAlloc asks); pure reads, compares
    // and scans (dn2cpp_str_compare/_indexof*, hash, length) are deliberately absent,
    // because a false failure breaks a green program. Exception construction
    // (dn2cpp_exception_new / _aggregate_exception_new) is deliberately absent too: a
    // throw ends the hot path, and flagging it would make every ThrowHelper-guarded BCL
    // body (Span.get_Item, the Math overflow guards) a violation. So is
    // dn2cpp_array_empty_* (a cached per-type singleton, no per-call allocation). A custom
    // exception type derived from a BCL exception still allocates through dn2cpp_alloc and
    // so is still flagged — accepted, not special-cased.
    private static readonly string[] HotAllocTokens =
    {
        // Objects, arrays, boxes.
        "dn2cpp_alloc(",            // objects, delegates, boxed state machines
        "dn2cpp_alloc_type_associated(", // RuntimeHelpers.AllocateTypeAssociatedMemory
        "dn2cpp_newarr_",           // every array-allocation variant (i4/ref/n, _t, _atomic_t)
        "dn2cpp_newmdarr(",         // multi-dimensional array newobj
        "dn2cpp_array_clone_",      // Array.Clone (i4/ref/n/dyn)
        "dn2cpp_array_subarray_",   // array segment materialization (i4/ref/n)
        "dn2cpp_array_create_instance", // Array.CreateInstance (+ _from_arraytype/_lengths)
        "dn2cpp_box(",              // boxing
        // String constructors and producers.
        "dn2cpp_string_from",       // every string-from-* constructor (chars/wcs/mbs/utf8/…)
        "dn2cpp_string_repeat_char",
        "dn2cpp_string_fast_allocate", // String.FastAllocateString
        "dn2cpp_string_create_buffer(", // String.Create's backing buffer
        "dn2cpp_string_concat",     // Concat family (2/3/4, _objects(_n), _objs, _spanchars)
        "dn2cpp_string_format",     // string.Format family (+ _arr/_spanobjs, _c variants)
        "dn2cpp_string_join_",      // string.Join family (ref/objs/i4/i8/u4/u8/r8/ch, _n/_range)
        "dn2cpp_string_decode_",    // utf8/utf16le decoders
        "dn2cpp_encoding_get_string", // Encoding.GetString (+ _ptr)
        "dn2cpp_encoding_decode_range(",
        "dn2cpp_string_to_chararray", // String.ToCharArray (+ _range)
        "dn2cpp_string_intern(",    // first intern of a content allocates its pool cell
        "dn2cpp_marshal_ptr_to_string_utf16(",
        // String instance operations returning a (possibly) new string. Substring
        // and Remove are family prefixes, not exact calls: the one-argument
        // overloads lower to the _to_end sibling, which allocates exactly as the
        // explicit-bound form it delegates to. Neither name has a non-allocating
        // sibling, so the prefix cannot over-match.
        "dn2cpp_str_substring",     // + _to_end
        "dn2cpp_str_replace_",      // Replace (char/str/str_cmp)
        "dn2cpp_str_split",         // Split family (+ _char/_str): the array and its parts
        "dn2cpp_str_pad(",
        "dn2cpp_str_insert(",
        "dn2cpp_str_remove",        // + _to_end
        "dn2cpp_str_trim",          // Trim/TrimStart/TrimEnd (+ _set)
        "dn2cpp_str_to_case(",      // ToUpper/ToLower
        "dn2cpp_str_normalize(",    // the non-allocating probe spells str_is_normalized
        // Value formatting: every *_to_string / format_* returns a fresh string
        // (dn2cpp_bool_to_string included — it builds "True"/"False", no cache).
        "dn2cpp_int_to_string(",
        "dn2cpp_long_to_string(",
        "dn2cpp_double_to_string",  // + _c
        "dn2cpp_float_to_string",   // + _c
        "dn2cpp_bool_to_string(",
        "dn2cpp_char_to_string(",
        "dn2cpp_decimal_to_string(",
        "dn2cpp_datetime_to_string(",
        "dn2cpp_datetimeoffset_to_string(",
        "dn2cpp_dateonly_to_string(",
        "dn2cpp_timeonly_to_string(",
        "dn2cpp_timespan_to_string(",
        "dn2cpp_enum_flags_to_string(",
        "dn2cpp_format_enum(",
        "dn2cpp_format_int",        // + _c
        "dn2cpp_format_uint",       // + _c
        "dn2cpp_format_r4",         // + _c
        "dn2cpp_format_r8",         // + _c
        "dn2cpp_convert_obj_to_string(",
        "dn2cpp_convert_to_string_base_", // i32/i64
        // Family prefix, so the concat entry point and the _virtual dispatch one
        // both match. Returns a string — and dispatches; in both tables.
        "dn2cpp_object_tostring",
        // StringBuilder: creation, ToString, and every helper that can grow the buffer.
        // The in-place ones (sb_remove, sb_clear, sb_replace_char*, sb_length,
        // sb_capacity, sb_get_char, sb_set_char) are deliberately absent.
        "dn2cpp_sb_new",            // ctor family (+ _cap/_str/_str_cap)
        "dn2cpp_sb_append_",        // every append routes through the growing ensure
        "dn2cpp_sb_insert_",
        "dn2cpp_sb_replace_str",    // + _range: a longer replacement grows the buffer
        "dn2cpp_sb_ensure_capacity(",
        "dn2cpp_sb_set_length(",    // growing past capacity reallocates
        "dn2cpp_sb_tostring(",
        "dn2cpp_sb_char_arr_str(",
        "dn2cpp_isb_",              // interpolated-string handler: new/append*/to_string all allocate or grow
        // Delegates.
        "dn2cpp_delegate_create(",
        "dn2cpp_delegate_combine(",
        "dn2cpp_delegate_remove(",  // may allocate the shortened multicast copy
        "dn2cpp_delegate_for_fnptr_",
    };

    // The tokens an emitted body text spells for a dynamic dispatch. Body text only — the
    // metadata that initializes vtables/interface tables is emitted elsewhere and never
    // reaches RecordHotBodyFacts.
    private static readonly string[] HotDispatchTokens =
    {
        "->type->vtable[",              // virtual dispatch / ldvirtftn
        "dn2cpp_resolve_interface(",    // interface dispatch
        "dn2cpp_try_resolve_interface(",// probed interface dispatch
        "dginvoke_",                    // delegate Invoke trampoline
        "dn2cpp_gvm_",                  // generic-virtual-method dispatcher (GvmDispatchName)
        "dn2cpp_object_tostring",       // runtime-internal tostring-slot dispatch (interpolation, Append(object), x.ToString())
    };

    /// <summary>Record what an emitted body allocates or dispatches, for the NoAlloc BFS.
    /// No-op while unarmed. First twin wins (string-keyed, like the named-symbol backstop):
    /// a minted twin sharing a CppName with the already-recorded body reuses its facts.
    /// A body's own <c>calli</c> mark (HotIndirectCalls) folds into its dispatch verdict.</summary>
    internal void RecordHotBodyFacts(MethodInfo m, string body)
    {
        if (HotBodyFacts is not { } facts || facts.ContainsKey(m.CppName))
            return;
        string? allocTok = null, dispTok = null;
        foreach (var t in HotAllocTokens)
            if (body.Contains(t, System.StringComparison.Ordinal)) { allocTok = t; break; }
        foreach (var t in HotDispatchTokens)
            if (body.Contains(t, System.StringComparison.Ordinal)) { dispTok = t; break; }
        if (dispTok is null && HotIndirectCalls is { } ind && ind.Contains(m.CppName))
            dispTok = "calli";
        facts[m.CppName] = new HotBodyFacts
        {
            Allocates = allocTok is not null,
            AllocToken = allocTok,
            Dispatches = dispTok is not null,
            DispatchToken = dispTok,
            Method = m,
        };
    }

    /// <summary>The canonical form of a closed generic interface — the
    /// same-template instantiation over canonicalized arguments — or null when
    /// the interface is its own canonical form (or not a closed generic). Used
    /// to append canonical alias rows to interface dispatch tables so a shared
    /// body's canonical-interface probe resolves on real receivers.</summary>
    internal ClassInfo? CanonicalInterfaceOf(ClassInfo itf)
    {
        if (!SharedGenericsEnabled || !itf.IsInterface || itf.GenericArity == 0
            || itf.Context.TypeArgs.Length != itf.GenericArity || itf.IntrinsicCppName is not null)
            return null;
        var args = itf.Context.TypeArgs;
        if (args.Any(ContainsOpenGenerics))
            return null;
        var canon = new TypeDesc[args.Length];
        bool changed = false;
        for (int i = 0; i < args.Length; i++)
        {
            canon[i] = Canon.CanonicalForm(args[i]);
            changed |= !ReferenceEquals(canon[i], args[i]);
        }
        if (!changed)
            return null;
        var canonItf = Instantiate(itf.Module, itf.Handle, canon);
        return canonItf == itf ? null : canonItf;
    }

    /// <summary>Whether a real (placeholder-free) specialization's argument
    /// vector canonicalizes to something else — i.e. the class belongs to a
    /// sharing group whether or not the link survived the shape check. Used by
    /// the debug backstop: a shared body must never name such a class's
    /// per-instantiation symbols. Cache-hitting only (every such class was
    /// examined by LinkCanonicalOwners, so its canonical instantiations
    /// already exist).</summary>
    internal bool HasCanonicalizableArgs(ClassInfo c)
    {
        if (c.GenericArity == 0 || c.Context.TypeArgs.Length != c.GenericArity
            || c.IntrinsicCppName is not null)
            return false;
        var args = c.Context.TypeArgs;
        if (args.Any(ContainsOpenGenerics))
            return false;
        foreach (var a in args)
            if (!ReferenceEquals(Canon.CanonicalForm(a), a))
                return true;
        return false;
    }

    // ---- shared-body planning state (filled by the emitter's planning pass) ----

    /// <summary>Canonical methods whose trial compile hit an
    /// instantiation-dependent site, keyed to the taint kind. Such a method has
    /// no shareable body; each grouped user compiles its own.</summary>
    internal readonly Dictionary<MethodInfo, string> SharedTaint = new();
    /// <summary>Canonical methods whose trial compile succeeded.</summary>
    internal readonly HashSet<MethodInfo> SharedTrialCompiled = new();
    /// <summary>Direct-call edges from a canonical method's body into other
    /// canonical methods (recorded during trial compile). A shared body binds
    /// these callees by symbol, so an unshareable callee cascades to the caller
    /// and a retained caller retains its callees. <c>RgctxPassable</c> records
    /// whether the site could hand the callee a runtime generic context if the
    /// callee turns out to need the hidden parameter (same canonical class — the
    /// caller's own context serves — or a call-site token that verifiably
    /// re-resolves to the callee); a needy callee behind an unpassable edge
    /// taints the caller instead.</summary>
    internal readonly Dictionary<MethodInfo, List<(MethodInfo Callee, bool RgctxPassable)>> SharedCallEdges = new();
    /// <summary>Canonical methods whose trial compile allocated at least one
    /// rgctx slot of their own (the roots of the context-use closure).</summary>
    internal readonly HashSet<MethodInfo> SharedRgctxRoots = new();
    /// <summary>Per canonical method, the canonical interfaces its trial body
    /// dispatches through (interface callvirt / constrained dispatch / the
    /// Dictionary comparer probe against a canonical handle). Resolved on real
    /// receivers via the alias rows, which are first-wins — so a canonical
    /// interface that two REAL interfaces of one allocated class collapse onto
    /// is ambiguous, and every body dispatching through it must not share
    /// (see <see cref="FinalizeSharedGenerics"/>).</summary>
    internal readonly Dictionary<MethodInfo, HashSet<ClassInfo>> SharedItfDispatches = new();
    /// <summary>Per canonical method, the (owner class, slot) keys its trial
    /// compile allocated — so a slot whose planning-pass fill failed to resolve
    /// for some real instantiation taints exactly the bodies that use it.</summary>
    internal readonly Dictionary<MethodInfo, List<(ClassInfo Owner, RgctxSlot Slot)>> SharedSlotKeys = new();
    /// <summary>Method-dimension mirror of <see cref="SharedSlotKeys"/>: per
    /// canonical generic-method instantiation body, the (owner method, slot)
    /// keys its trial compile allocated in its own registry.</summary>
    internal readonly Dictionary<MethodInfo, List<(MethodInfo Owner, RgctxSlot Slot)>> SharedMethodSlotKeys = new();
    /// <summary>Shared-generics stats for the CLI metrics line, filled by
    /// <see cref="FinalizeSharedGenerics"/>.</summary>
    internal (int Instantiations, int Groups, int SharedBodies, int EligibleBodies) SharedStats;
    /// <summary>Rgctx stats for the CLI metrics line: distinct slots across the
    /// final registries, emitted per-instantiation tables, and per-real one-line
    /// forwarders (set during final emission).</summary>
    internal int RgctxSlotCount, RgctxTableCount, RgctxForwarderCount;
    /// <summary>Root fallback reasons (taint kind -> count), before cascade.</summary>
    internal IReadOnlyDictionary<string, int> SharedTaintReasons => _sharedTaintReasons;
    private readonly Dictionary<string, int> _sharedTaintReasons = new();

    /// <summary>Turns the planning-pass trial results into final shared-body
    /// assignments:
    /// <list type="number">
    /// <item>cascade taint: a canonical body that direct-calls an unshareable
    /// (or never-compiled) canonical sibling is itself unshareable — iterated to
    /// a fixpoint;</item>
    /// <item>assignment: every reachable grouped method whose owner counterpart
    /// compiled shareable gets <see cref="MethodInfo.SharedImpl"/>; the rest
    /// keep compiling their own bodies;</item>
    /// <item>retention: the assigned owner bodies plus their direct-call
    /// closure stay reachable; every other canonical-world method is dropped
    /// from <see cref="Reachable"/> (synthetic owners exist only to donate
    /// bodies — an undonated body must not emit);</item>
    /// <item>delegate-adapter targets recorded during planning are remapped
    /// through <see cref="MethodInfo.Emittable"/> so no adapter wraps a symbol
    /// that no longer emits.</item>
    /// </list></summary>
    internal void FinalizeSharedGenerics(Func<ClassInfo, MethodInfo, bool> backendSkips)
    {
        // Planning → Finalized, asserted: the verdicts must be computed exactly once,
        // over the COMPLETED planning fixpoint. Run early, they would miss taint from
        // bodies not yet trial-compiled; run twice, the second pass would re-cascade
        // over the SharedCallEdges that ResetSharedPlanningState has since cleared —
        // an empty call graph, so nothing would taint and everything would share.
        EnterPhase(EmitPhase.Finalized);
        // 0a. Alias-row collision policy: a canonical interface two REAL closed
        // interfaces of one allocatable class collapse onto (e.g. IFoo<string>
        // + IFoo<object> on one receiver) resolves first-wins through the alias
        // rows — ambiguous for a shared body's dispatch. Taint every body that
        // dispatches through such a handle; the affected instantiations fall
        // back to per-instantiation bodies whose dispatch uses the REAL
        // interface handles and stays exact. Only allocated classes matter
        // (interface dispatch needs an instance).
        if (SharedItfDispatches.Count > 0)
        {
            var ambiguous = new HashSet<ClassInfo>();
            var rowOwner = new Dictionary<ClassInfo, ClassInfo>();
            foreach (var cls in Classes)
            {
                if (cls.IsInterface || ContainsCanonPlaceholder(cls) || !IsAllocated(cls))
                    continue;
                rowOwner.Clear();
                for (var c = cls; c is not null; c = c.BaseClass)
                    foreach (var itf in c.Interfaces)
                        if (CanonicalInterfaceOf(itf) is { } citf)
                        {
                            if (rowOwner.TryGetValue(citf, out var first)
                                && !ReferenceEquals(first, itf))
                                ambiguous.Add(citf);
                            else
                                rowOwner[citf] = itf;
                        }
            }
            if (ambiguous.Count > 0)
                foreach (var kv in SharedItfDispatches)
                    if (!SharedTaint.ContainsKey(kv.Key) && kv.Value.Overlaps(ambiguous))
                        SharedTaint[kv.Key] = "itf-collision";
        }

        // 0. A slot whose planning-pass fill failed to resolve for some real
        // instantiation is unusable group-wide: taint every body that uses it.
        foreach (var kv in SharedSlotKeys)
        {
            if (SharedTaint.ContainsKey(kv.Key))
                continue;
            foreach (var key in kv.Value)
                if (Rgctx.Classes.SlotKnownBad(key))
                {
                    SharedTaint[kv.Key] = "rgctx-fill";
                    break;
                }
        }
        foreach (var kv in SharedMethodSlotKeys)
        {
            if (SharedTaint.ContainsKey(kv.Key))
                continue;
            foreach (var key in kv.Value)
                if (Rgctx.Methods.SlotKnownBad(key))
                {
                    SharedTaint[kv.Key] = "rgctx-fill";
                    break;
                }
        }

        // 1. Joint fixpoint over the direct-call edges: cascade unshareability
        // (a caller of an unshareable/never-compiled callee is unshareable) and
        // propagate context use (a caller of a context-needing hidden-parameter
        // callee must supply the table — through its own context when the edge
        // is passable, else it taints). Both are monotone, so one loop.
        var uses = new HashSet<MethodInfo>(SharedRgctxRoots);
        bool changed = true;
        while (changed)
        {
            changed = false;
            foreach (var kv in SharedCallEdges)
            {
                var m = kv.Key;
                if (SharedTaint.ContainsKey(m))
                    continue;
                foreach (var (callee, passable) in kv.Value)
                {
                    if (SharedTaint.ContainsKey(callee) || !SharedTrialCompiled.Contains(callee))
                    {
                        SharedTaint[m] = "cascade";
                        changed = true;
                        break;
                    }
                    if (uses.Contains(callee) && WouldNeedRgctxParam(callee))
                    {
                        if (!passable)
                        {
                            SharedTaint[m] = "rgctx-pass";
                            changed = true;
                            break;
                        }
                        if (uses.Add(m))
                            changed = true;
                    }
                }
            }
        }
        foreach (var m in uses)
        {
            if (SharedTaint.ContainsKey(m))
                continue;
            m.RgctxUses = true;
            m.RgctxParam = WouldNeedRgctxParam(m);
        }

        // Root reasons only — cascade is derived, so it would drown the roots.
        foreach (var kv in SharedTaint)
            if (kv.Value != "cascade")
                _sharedTaintReasons[kv.Value] = _sharedTaintReasons.GetValueOrDefault(kv.Value) + 1;

        // 2. Assign SharedImpl for every reachable grouped method with a
        // shareable owner counterpart.
        int instantiations = 0;
        var groups = new HashSet<ClassInfo>();
        var eligible = new HashSet<MethodInfo>();
        var shared = new HashSet<MethodInfo>();
        foreach (var cls in Classes.ToList())   // snapshot: the owner pull below can append
        {
            if (cls.SharedOwner is not { } owner)
                continue;
            // A partially-canonical satellite grouped under a fuller canonical
            // owner is not a real instantiation: its methods never emit (calls
            // redirect to the donor via SharedDonor), so it neither counts nor
            // binds SharedImpl.
            if (ContainsCanonPlaceholder(cls))
                continue;
            instantiations++;
            groups.Add(owner);
            // Counted above (it IS a grouped instantiation), but it binds nothing: with no
            // members decoded it has no reachable method, and the loop below only ever binds
            // those. Reading .Methods to establish that would decode it, which is exactly
            // the decode the deferral exists to avoid.
            if (!cls.MembersReady)
                continue;
            foreach (var m in cls.Methods)
            {
                if (m.Rva == 0 || m.NameSuffix != "" || m.Name == ".cctor"
                    || !Reachable.Contains(m) || backendSkips(cls, m))
                    continue;
                if (!eligible.Add(m))
                    continue;
                if (owner.MethodByTemplate.TryGetValue(m.Handle, out var counterpart)
                    && SharedTrialCompiled.Contains(counterpart)
                    && !SharedTaint.ContainsKey(counterpart))
                {
                    m.SharedImpl = counterpart;
                    shared.Add(m);
                }
            }
        }

        // 2b. Generic-method instantiations bind their method-dimension
        // canonical counterpart the same way. They live on real and canonical
        // classes alike, so the classes loop above cannot enumerate them; the
        // creation-order list is the population. A linked instantiation that is
        // itself canonical-world is a satellite: it never emits (calls redirect
        // to the donor via SharedDonor), so it neither counts nor binds.
        foreach (var m in _methodInstanceOrder)
        {
            if (m.SharedOwner is not { } methodOwner || IsCanonicalMethod(m))
                continue;
            if (!Reachable.Contains(m) || backendSkips(m.DeclaringClass, m))
                continue;
            if (!eligible.Add(m))
                continue;
            if (SharedTrialCompiled.Contains(methodOwner)
                && !SharedTaint.ContainsKey(methodOwner))
            {
                m.SharedImpl = methodOwner;
                shared.Add(m);
            }
        }

        // 2c. Runtime-instantiation template verdicts: a template whose every
        // reachable chain-level body shared and whose every rgctx slot is a
        // bare-type-argument TypeInfo read survives to emission; its bodies join
        // the retention seed below (a template has no users to donate to, so
        // nothing else would keep them).
        var templateSeeds = JudgeRuntimeTemplates(backendSkips);

        // 3. Retention: assigned bodies + their direct-call closure. All of the
        // closure is shareable by construction (step 1 removed any caller of an
        // unshareable callee).
        var retained = new HashSet<MethodInfo>();
        var work = new Stack<MethodInfo>(
            shared.Select(m => m.SharedImpl!).Concat(templateSeeds).Distinct());
        while (work.Count > 0)
        {
            var m = work.Pop();
            if (!retained.Add(m))
                continue;
            if (SharedCallEdges.TryGetValue(m, out var callees))
                foreach (var (c, _) in callees)
                    work.Push(c);
        }
        foreach (var m in Reachable.Where(IsCanonicalMethod).ToList())
            if (!retained.Contains(m))
                Reachable.Remove(m);

        // 4. Remap delegate-adapter targets recorded while planning-pass bodies
        // still resolved to their own symbols. The shape half of the key rides
        // through untouched: it is group-invariant (see IsClosedStaticDelegate), so
        // two group members mapping onto one canonical impl agree on it, and the
        // Distinct collapses them to one adapter.
        var adapters = DelegateAdapters
            .Select(a => a with { Method = a.Method.Emittable })
            .Distinct()
            .ToList();
        DelegateAdapters.Clear();
        DelegateAdapters.AddRange(adapters);

        SharedStats = (instantiations, groups.Count, shared.Count, eligible.Count);
    }

    /// <summary>The eligible runtime-instantiation templates, in trigger order:
    /// (definition backtick name, template, its placeholder-bearing chain levels
    /// derived-first). The emitter gives each level full metadata and each entry
    /// a runtime-templates row; empty means the feature emits nothing. Filled by
    /// <see cref="FinalizeSharedGenerics"/>.</summary>
    internal readonly List<(string DefName, ClassInfo Template, List<ClassInfo> Levels)>
        EligibleRuntimeTemplates = new();

    /// <summary>The union of the eligible templates' chain levels — the one
    /// canonical-world class kind that DOES emit metadata. The emitter's
    /// canonical-exclusion predicates carve these out.</summary>
    internal readonly HashSet<ClassInfo> EligibleRuntimeTemplateLevels = new();

    /// <summary>The emitted rgctx slot descriptor of a template chain level:
    /// entry i is the type-argument index whose type-info the runtime fill
    /// stamps into slot i. The verdict already established every slot is a
    /// bare-type-argument TypeInfo read off the PLANNING registry; the emission
    /// registry holds the retained subset, so a violation here is a transpiler
    /// bug and crashes raw.</summary>
    internal int[] RuntimeTemplateSlotDescriptor(ClassInfo level)
    {
        var slots = Rgctx.Classes.SlotsOf(level);
        if (slots is null || slots.Count == 0)
            return Array.Empty<int>();
        var desc = new int[slots.Count];
        for (int i = 0; i < slots.Count; i++)
        {
            var slot = slots[i];
            if (slot.Kind != RgctxSlotKind.TypeInfo)
                throw new InvalidOperationException(
                    $"runtime template {level.FullName}: emission slot {i} is {slot.Kind}, "
                    + "which the eligibility verdict should have rejected");
            var t = RgctxResolveTypeToken(level.Module, SRME.EntityHandle(slot.Token),
                new GenericContext(level.Context.TypeArgs, Array.Empty<TypeDesc>()));
            if (t.CanonAnyIndex < 0)
                throw new InvalidOperationException(
                    $"runtime template {level.FullName}: emission slot {i} resolves to {t}, "
                    + "not a bare type argument");
            desc[i] = t.CanonAnyIndex;
        }
        return desc;
    }

    /// <summary>Judges each rooted template (see
    /// <see cref="BuildRuntimeInstantiationTemplates"/>) against what a runtime
    /// clone can actually run: every reachable method of every placeholder-bearing
    /// chain level must be an instance, non-generic body that trial-compiled
    /// shareable, and every rgctx slot its level accumulated must be a TypeInfo
    /// read whose token re-resolves (under the template's own context) to a bare
    /// per-index placeholder — the one entry a MakeGenericType fill can synthesize
    /// from its argument array. Anything else fails the WHOLE template: its
    /// bodies stay undonated and are dropped like any other canonical world's,
    /// and the runtime diagnostic keeps naming the missing instantiation.
    /// Returns the eligible templates' bodies as retention seeds.</summary>
    private List<MethodInfo> JudgeRuntimeTemplates(Func<ClassInfo, MethodInfo, bool> backendSkips)
    {
        var seeds = new List<MethodInfo>();
        if (RuntimeInstantiationTemplates.Count == 0)
            return seeds;
        // Reachable generic-METHOD instantiations by declaring class, collected
        // once: a template level that owns one is ineligible (the runtime clone
        // cannot mint per-method tables), and the class's own Methods list does
        // not enumerate instantiations.
        var genericInstanceOwners = new HashSet<ClassInfo>();
        foreach (var gm in _methodInstanceOrder)
            if (Reachable.Contains(gm))
                genericInstanceOwners.Add(gm.DeclaringClass);
        foreach (var (defName, tmpl) in RuntimeInstantiationTemplates)
        {
            var levels = new List<ClassInfo>();
            for (ClassInfo? lv = tmpl; lv is not null && ContainsCanonPlaceholder(lv); lv = lv.BaseClass)
                levels.Add(lv);
            bool ok = levels.Count > 0;
            var bodies = new List<MethodInfo>();
            foreach (var lv in levels)
            {
                if (!ok)
                    break;
                if (!lv.MembersReady || genericInstanceOwners.Contains(lv))
                {
                    ok = false;
                    break;
                }
                foreach (var m in lv.Methods)
                {
                    if (m.Rva == 0 || m.Name == ".cctor" || !Reachable.Contains(m)
                        || backendSkips(lv, m))
                        continue;
                    if (m.IsStatic || m.NameSuffix != ""
                        || !SharedTrialCompiled.Contains(m) || SharedTaint.ContainsKey(m))
                    {
                        ok = false;
                        break;
                    }
                    // A signature naming a type parameter moves T VALUES across the
                    // call boundary — the body trial cannot see that (the body just
                    // uses its argument), so the boundary is checked here.
                    bool sigAny = ContainsCanonAny(m.Signature.ReturnType);
                    foreach (var pt in m.Signature.ParameterTypes)
                        sigAny |= ContainsCanonAny(pt);
                    if (sigAny)
                    {
                        ok = false;
                        break;
                    }
                    bodies.Add(m);
                }
                if (!ok)
                    break;
                if (Rgctx.Classes.SlotsOf(lv) is { } slots)
                    foreach (var slot in slots)
                    {
                        if (slot.Kind != RgctxSlotKind.TypeInfo)
                        {
                            ok = false;
                            break;
                        }
                        try
                        {
                            var t = RgctxResolveTypeToken(lv.Module,
                                SRME.EntityHandle(slot.Token),
                                new GenericContext(lv.Context.TypeArgs, Array.Empty<TypeDesc>()));
                            if (t.CanonAnyIndex < 0)
                            {
                                ok = false;
                                break;
                            }
                        }
                        catch (Exception e) when (!IsMustEscape(e))
                        {
                            ok = false;
                            break;
                        }
                    }
            }
            if (!ok)
                continue;
            EligibleRuntimeTemplates.Add((defName, tmpl, levels));
            foreach (var lv in levels)
                EligibleRuntimeTemplateLevels.Add(lv);
            seeds.AddRange(bodies);
        }
        return seeds;
    }

    // ---- runtime generic context (rgctx): slot registries and table fill ----

    /// <summary>The rgctx subsystem's two dimensions (see
    /// <see cref="RgctxSystem"/>). Built here because both dimensions' hooks ask
    /// questions only the compilation can answer — liveness, satellite-hood, and
    /// the token re-resolution with its note/reach side effects.
    /// <para>The class dimension's user context is class arguments only, resolved
    /// in the user's own scope; the method dimension's is the user's FULL context
    /// (class + method arguments — a slot may depend on either), resolved in its
    /// declaring class's scope.</para>
    /// <para>The satellite hooks are what a canonical-world group member looks
    /// like in each dimension: a class like <c>Dictionary&lt;CnInt32,String&gt;</c>,
    /// instantiated while completing an enum-canonical class, can link under a
    /// fuller canonical owner, and a generic-method instantiation created inside a
    /// partially-canonical world does the same. Neither exists at runtime, so
    /// neither may fill.</para></summary>
    private RgctxSystem Rgctx => _rgctx ??= new RgctxSystem(
        new RgctxDimension<ClassInfo>(
            static owner => owner.SharedUsers,
            ClassInfo.CompareByOrder,
            ContainsCanonPlaceholder,
            RgctxUserLive,
            (owner, user, slot) => ResolveRgctxSlot(
                owner.Module,
                new GenericContext(user.Context.TypeArgs, Array.Empty<TypeDesc>()),
                user,
                slot)),
        new RgctxDimension<MethodInfo>(
            static owner => owner.SharedUsers,
            MethodInfo.CompareByOrder,
            IsCanonicalMethod,
            RgctxMethodUserLive,
            (owner, user, slot) => ResolveRgctxSlot(
                owner.Module, user.Context, user.DeclaringClass, slot)));
    private RgctxSystem? _rgctx;

    private readonly Dictionary<ClassInfo, string?> _rgctxAnchorSyms = new();

    /// <summary>Whether a grouped real instantiation can observe a runtime
    /// generic context at all — some method of its own is reachable (shared
    /// bodies run under its identity) or an instance can exist (receiver-derived
    /// context lookups). The dimension adds the "another live table forwards to
    /// its table" disjunct ahead of these, since it owns that record. Group
    /// membership alone is NOT enough: signature decoding instantiates many
    /// specializations that never emit (under reference collapsing, every
    /// <c>Dictionary&lt;ref,ref&gt;</c> mentioned anywhere in a loaded assembly
    /// joins one group), and filling their tables would force-emit their
    /// statics/cctors — dragging whole dead subsystems into the output.</summary>
    private bool RgctxUserLive(ClassInfo user) =>
        _allocatedRefTypes.Contains(user)
        // Short-circuit, not an optimization: the question the last disjunct asks — does
        // this instantiation have a reachable method? — is answered "no" by a class whose
        // members were never decoded, because reaching a method means having resolved it,
        // and resolving it is what decodes them. Reading .Methods to hear that would decode
        // every grouped instantiation in the program.
        || (user.MembersReady && user.Methods.Any(Reachable.Contains));

    /// <summary>Allocates (or reuses) the rgctx slot of <paramref name="owner"/>
    /// for the given kind + raw IL token, returning its table index.</summary>
    internal int RgctxSlotIndex(ClassInfo owner, RgctxSlotKind kind, int token) =>
        Rgctx.Classes.SlotIndex(owner, kind, token);

    /// <summary>Whether the slot is known unresolvable (its planning-pass fill
    /// failed for some group member) — the allocating body must taint.</summary>
    internal bool RgctxSlotKnownBad(ClassInfo owner, RgctxSlotKind kind, int token) =>
        Rgctx.Classes.SlotKnownBad(owner, kind, token);

    /// <summary>Allocates (or reuses) the rgctx slot of the canonical
    /// generic-method instantiation <paramref name="owner"/> for the given
    /// kind + raw IL token, returning its table index. The method-dimension
    /// mirror of <see cref="RgctxSlotIndex"/>.</summary>
    internal int RgctxMethodSlotIndex(MethodInfo owner, RgctxSlotKind kind, int token) =>
        Rgctx.Methods.SlotIndex(owner, kind, token);

    /// <summary>Whether the method-registry slot is known unresolvable (its
    /// planning-pass fill failed for some real instantiation).</summary>
    internal bool RgctxMethodSlotKnownBad(MethodInfo owner, RgctxSlotKind kind, int token) =>
        Rgctx.Methods.SlotKnownBad(owner, kind, token);

    /// <summary>The shape-eligible runtime-instantiation templates, in trigger
    /// (first-typeof) order: (definition backtick name, $CnAny instantiation).
    /// Grown once by <see cref="BuildRuntimeInstantiationTemplates"/>; the
    /// emitter narrows it to the definitions whose every reachable body shared
    /// and whose rgctx slots all resolved to bare type arguments.</summary>
    internal readonly List<(string DefName, ClassInfo Template)> RuntimeInstantiationTemplates = new();

    /// <summary>Builds the runtime-instantiation TEMPLATES: for each open generic
    /// definition the program both typeofs (<c>ldtoken D&lt;&gt;</c>) and could hand
    /// to <c>Type.MakeGenericType</c>, instantiate the definition over the
    /// per-index $CnAny placeholders and root it like an allocated type, so the
    /// planning pass trial-compiles its bodies. The template is NEVER anyone's
    /// SharedOwner — real instantiations keep their ordinary grouping — so this
    /// pass composes with the existing machinery instead of re-linking it. The
    /// whole pass is a no-op unless the program calls MakeGenericType AND typeofs
    /// an open definition: a corpus without the pattern is byte-identical with the
    /// pass compiled out. A shape-ineligible definition is skipped here; a body
    /// that fails the $CnAny trial merely taints, leaving an unused canonical
    /// world and no emitted template.</summary>
    internal void BuildRuntimeInstantiationTemplates()
    {
        if (!SharedGenericsEnabled || !_makeGenericTypeUsed)
            return;
        foreach (var defName in _typeofOpenGenericDefNames)
        {
            int cut = defName.LastIndexOf('.');
            var key = cut < 0 ? ("", defName) : (defName[..cut], defName[(cut + 1)..]);
            if (!TypeIndex().TryGetValue(key, out var cands) || cands.Count == 0)
                continue;
            var (module, handle) = cands[0];
            ClassInfo tmpl;
            try
            {
                int arity = module.Reader.GetTypeDefinition(handle).GetGenericParameters().Count;
                if (arity == 0)
                    continue;
                var args = new TypeDesc[arity];
                for (int i = 0; i < arity; i++)
                    args[i] = Canon.AnyPlaceholder(i);
                tmpl = Instantiate(module, handle, args);
            }
            catch (Exception e) when (!IsMustEscape(e))
            {
                // A definition that does not instantiate (unresolvable signature,
                // bound overflow would escape) is simply not a template; the
                // runtime diagnostic still names the missing instantiation.
                continue;
            }
            // A fresh specialization's IsValueType/IsAbstract land in CompleteShape,
            // so the shape check reads garbage until the pending queue drains.
            CompletePendingSpecializations();
            if (!RuntimeTemplateShapeEligible(tmpl))
                continue;
            RuntimeInstantiationTemplates.Add((defName, tmpl));
            // Root it like an allocated type: arms the used-slot × allocated-type
            // cross product, so every virtual override a base-typed caller can
            // dispatch into is trial-compiled; instance ctors are what the
            // reflection-ctor path (Activator over the synthesized Type) invokes.
            ReachAllocatedType(tmpl);
            foreach (var m in tmpl.Methods)
                if (m.Name == ".ctor" && !m.IsStatic && m.Rva != 0)
                    Reach(m);
        }
    }

    /// <summary>The v1 shape bound for a runtime-instantiation template: a
    /// concrete top-level reference definition whose placeholder-bearing chain
    /// levels carry no per-instantiation storage (no statics, no cctor), no
    /// field or directly-implemented interface naming a type parameter, and an
    /// identity base projection (<c>D2&lt;T&gt; : D1&lt;T&gt;</c>, same order) —
    /// each restriction is exactly a thing the runtime clone cannot synthesize
    /// (AOT storage symbols, per-argument layout, a permuted argument map). The
    /// non-placeholder tail of the chain is ordinary emitted code and is not
    /// restricted.</summary>
    private bool RuntimeTemplateShapeEligible(ClassInfo tmpl)
    {
        if (tmpl.IsValueType || tmpl.IsInterface || tmpl.IsAbstract)
            return false;
        try
        {
            for (ClassInfo? lv = tmpl; lv is not null; lv = lv.BaseClass)
            {
                if (!ContainsCanonPlaceholder(lv))
                    break;
                if (lv.IsValueType || RgctxAnchorSym(lv) is null)
                    return false;
                lv.EnsureMembers();
                if (lv.StaticCctor is not null)
                    return false;
                foreach (var f in lv.Fields)
                {
                    if (f.IsLiteral)
                        continue;
                    if (f.IsStatic || ContainsCanonPlaceholder(f.Type))
                        return false;
                }
                foreach (var itf in lv.Interfaces)
                    if (ContainsCanonPlaceholder(itf))
                        return false;
                if (lv.BaseClass is { } b && ContainsCanonPlaceholder(b))
                {
                    var ba = b.Context.TypeArgs;
                    for (int j = 0; j < ba.Length; j++)
                        if (!ReferenceEquals(ba[j], Canon.AnyPlaceholder(j)))
                            return false;
                }
            }
        }
        catch (Exception e) when (!IsMustEscape(e))
        {
            return false;
        }
        return true;
    }

    /// <summary>Whether a context-using shared body of <paramref name="m"/>'s
    /// shape receives the context as the hidden trailing parameter (no
    /// receiver-derivable source): generic-method instantiations (the receiver
    /// type determines class arguments only, never method arguments), statics,
    /// value-type receivers, and reference receivers of classes with no
    /// open-definition anchor (nested generics).</summary>
    internal bool WouldNeedRgctxParam(MethodInfo m) =>
        m.NameSuffix != "" || m.IsStatic || m.DeclaringClass.IsValueType
        || RgctxAnchorSym(m.DeclaringClass) is null;

    /// <summary>The open-definition type-info symbol a reference-receiver shared
    /// body anchors its rgctx walk on (<c>dn2cpp_rgctx(recv->type, &amp;SYM)</c>),
    /// or null when the class has none (non-generic, or a nested generic — the
    /// emitter's generic-definition handles cover top-level generics only).
    /// Mirrors the emitter's GenericDefInfo naming, so the symbol is the same
    /// <c>gendef_*</c> the group members' type-infos point at.</summary>
    internal string? RgctxAnchorSym(ClassInfo cls)
    {
        if (_rgctxAnchorSyms.TryGetValue(cls, out var cached))
            return cached;
        string? sym = null;
        if (cls.GenericArity > 0 && cls.Context.TypeArgs.Length > 0)
            try
            {
                var reader = cls.Module.Reader;
                var td = reader.GetTypeDefinition(cls.Handle);
                if (td.GetDeclaringType().IsNil)
                {
                    string name = reader.GetString(td.Name);
                    string ns = reader.GetString(td.Namespace);
                    string full = string.IsNullOrEmpty(ns) ? name : ns + "." + name;
                    sym = "gendef_" + CppNaming.Sanitize(full);
                }
            }
            catch (Exception e) when (!IsMustEscape(e))
            {
                sym = null;
            }
        _rgctxAnchorSyms[cls] = sym;
        return sym;
    }

    /// <summary>Resets the slot registries and fill state between the planning
    /// and emission passes: the final registries then hold exactly the retained
    /// shared bodies' slots in deterministic compile order. The bad-slot record
    /// survives (its taints were already applied).</summary>
    internal void ResetRgctxForEmission()
    {
        // Legal only in the Finalized window. Earlier, the wipe would drop the very
        // planning registries FillRgctxTables is still filling and FinalizeSharedGenerics'
        // slot-taint step still keys on; later, it would wipe the live registries the
        // emission pass has refilled — the tables the output is spun from.
        if (Phase is not EmitPhase.Finalized)
            throw new InvalidOperationException(
                $"emit protocol: ResetRgctxForEmission in phase {Phase} (legal: Finalized)");
        Rgctx.ResetForEmission();
    }

    /// <summary>Releases the shared-generics planning state once
    /// <see cref="FinalizeSharedGenerics"/> has consumed it. These are per-canonical-body
    /// structures — <see cref="SharedCallEdges"/> is the whole canonical call graph — and
    /// nothing reads them after the verdicts are in, so holding them would add to the
    /// emission pass's own peak for nothing.
    ///
    /// <para>Deliberately NOT cleared, and they must stay that way:
    /// <see cref="SharedTaint"/> (the driver prints the fallback histogram from it after
    /// Emit returns) and <see cref="SharedItfDispatches"/> (the emitter probes it while
    /// deciding which canonical interfaces to pull in). Both are small.</para>
    ///
    /// <para>Separate from <see cref="ResetRgctxForEmission"/> on purpose: that one rebuilds
    /// registries the emission pass refills, this one drops state nobody refills.</para></summary>
    internal void ResetSharedPlanningState()
    {
        // Legal only in the Finalized window: this clears exactly the state
        // FinalizeSharedGenerics consumes (SharedCallEdges IS the canonical call graph,
        // SharedTrialCompiled/SharedRgctxRoots/Shared*SlotKeys feed the taints). Cleared
        // before the verdicts, no edge would cascade and no slot would taint — every
        // body would silently share.
        if (Phase is not EmitPhase.Finalized)
            throw new InvalidOperationException(
                $"emit protocol: ResetSharedPlanningState in phase {Phase} (legal: Finalized)");
        SharedCallEdges.Clear();
        SharedTrialCompiled.Clear();
        SharedRgctxRoots.Clear();
        SharedSlotKeys.Clear();
        SharedMethodSlotKeys.Clear();
    }

    /// <summary>Total distinct slots across the final registries (metrics).</summary>
    internal int RgctxSlotTotal() => Rgctx.SlotTotal();

    /// <summary>The per-instantiation tables for the emitter, in
    /// <see cref="ClassInfo.SortKey"/> order: each grouped real class with a
    /// non-empty owner registry gets one <c>rgctx_&lt;cls&gt;</c> table (see
    /// <see cref="RgctxDimension{TOwner}.Tables"/> for why it is sorted).</summary>
    internal IEnumerable<(ClassInfo User, IReadOnlyList<string> Entries)> RgctxTables() =>
        Rgctx.Classes.Tables();

    /// <summary>The per-real generic-method-instantiation tables, in the same
    /// content-derived order (see <see cref="RgctxTables"/>): each linked real
    /// instantiation of a slot-owning canonical instantiation gets one
    /// <c>rgctx_&lt;method&gt;</c> table (the value its one-line forwarder appends).</summary>
    internal IEnumerable<(MethodInfo User, IReadOnlyList<string> Entries)> RgctxMethodTables() =>
        Rgctx.Methods.Tables();

    /// <summary>Resolves every not-yet-filled (slot, group member) pair: each
    /// slot's token is re-resolved under the member's real generic context and
    /// the referenced runtime entities are noted/reached exactly as a
    /// per-instantiation body would have. Runs once per body-compilation round
    /// (both passes — the planning fill drives the discovery fixpoint the
    /// emission pass replays), incrementally and idempotently. Returns whether
    /// anything new was filled.</summary>
    internal bool FillRgctxTables()
    {
        // The fill's failure policy IS the pass, so the arm is derived from the phase
        // rather than taken as a parameter — removing the one way the two could
        // disagree: a planning-mode fill during emission would degrade a real bug to a
        // silent nullptr slot; an emission-mode fill during planning would abort on the
        // unresolvable slots whose taint is the planning pass's product. And a fill
        // belongs to a body-compilation round only: in Discovery it would note/reach
        // entities ahead of the grouping fixpoint, in Finalized it would fill against
        // half-reset registries.
        if (Phase is not (EmitPhase.Planning or EmitPhase.Emission))
            throw new InvalidOperationException(
                $"emit protocol: FillRgctxTables in phase {Phase} (legal: Planning, Emission)");
        return Rgctx.Fill(planning: Phase == EmitPhase.Planning);
    }

    /// <summary>Whether a linked real generic-method instantiation can observe
    /// a runtime generic context at all: its forwarder runs because it is
    /// reachable of its own. The dimension adds the "another live table forwards
    /// to its table" disjunct ahead of this, as in the class dimension. The
    /// method-dimension analog of <see cref="RgctxUserLive"/> (allocation does
    /// not apply — method tables are only ever named statically, never walked off
    /// a receiver).</summary>
    private bool RgctxMethodUserLive(MethodInfo user) => Reachable.Contains(user);

    /// <summary>One slot's table entry for one group member: re-resolve the raw
    /// token under the member's real generic context (<paramref name="ctx"/> —
    /// class arguments only for a class-registry slot, the full class+method
    /// context for a method-registry slot), note/reach what the entry
    /// references (so the symbols exist), and return the C++ expression.
    /// <paramref name="scope"/> is the member's declaring class, the resolution
    /// scope for member references.</summary>
    private string ResolveRgctxSlot(Module module, GenericContext ctx, ClassInfo scope, RgctxSlot slot)
    {
        var handle = SRME.EntityHandle(slot.Token);
        switch (slot.Kind)
        {
            case RgctxSlotKind.TypeInfo:
                return RgctxTypeInfoEntry(RgctxResolveTypeToken(module, handle, ctx));
            case RgctxSlotKind.TypeArg0TypeInfo:
            {
                var t = RgctxResolveTypeToken(module, handle, ctx);
                if (t is not { Kind: TypeKind.Class, Class: { } g } || g.Context.TypeArgs.Length == 0)
                    throw new NotSupportedException($"rgctx: {t} has no type argument to project");
                return RgctxTypeInfoEntry(g.Context.TypeArgs[0]);
            }
            case RgctxSlotKind.ArrayTypeInfo:
            {
                var t = RgctxResolveTypeToken(module, handle, ctx);
                var elem = t.Kind == TypeKind.SZArray ? t.Element! : t;
                NoteArrayElementType(elem);
                return "&ti_arr_" + ArrayElemMangle(elem);
            }
            case RgctxSlotKind.ClassAlloc:
            {
                var ctor = ResolveMethodHandle(module, handle, ctx, scope)
                    ?? throw new NotSupportedException("rgctx: unresolved allocation ctor token");
                var rc = ctor.DeclaringClass;
                NoteForceEmit(rc);
                ReachAllocatedType(rc);
                return "&" + rc.CppTypeInfoName;
            }
            case RgctxSlotKind.StaticFieldAddr:
            {
                var (cls, fld) = RgctxResolveField(module, handle, ctx, scope);
                if (fld.IsThreadStatic)
                    throw new NotSupportedException("rgctx: thread-static storage has no stable address");
                NoteForceEmit(cls);
                ReachCctor(cls);
                return "&" + fld.CppStaticName;
            }
            case RgctxSlotKind.CctorEnsureFn:
            {
                var (cls, _) = RgctxResolveField(module, handle, ctx, scope);
                var cc = cls.EnsureMembers().StaticCctor   // its .cctor is a member
                    ?? throw new NotSupportedException($"rgctx: {cls.FullName} has no static constructor");
                NoteForceEmit(cls);
                NoteCctorEnsure(cc);
                Reach(cc);
                return "&" + cc.CppName + "__ensure";
            }
            case RgctxSlotKind.ComparerDefault:
            {
                var getter = ResolveMethodHandle(module, handle, ctx, scope)
                    ?? throw new NotSupportedException("rgctx: unresolved Comparer.Default token");
                var gc = GenericComparerFor(getter.DeclaringClass.Context.TypeArgs[0])
                    ?? throw new NotSupportedException("rgctx: no GenericComparer for element");
                NoteForceEmit(gc);
                ReachAllocatedType(gc);
                return "&" + gc.CppTypeInfoName;
            }
            // EqualityComparer<T> is intrinsic-OPAQUE: its members are never decoded,
            // so the entry re-resolves the get_Default member reference's PARENT TYPE
            // rather than the method.
            case RgctxSlotKind.EqualityComparerDefault:
                return RgctxTypeInfoEntry(RgctxResolveMemberParentType(module, handle, ctx));
            case RgctxSlotKind.EqualityComparerInterface:
            {
                var t = RgctxResolveMemberParentType(module, handle, ctx);
                if (t is not { Kind: TypeKind.Class, Class: { } ec } || ec.Context.TypeArgs.Length != 1)
                    throw new NotSupportedException($"rgctx: {t} is not a closed EqualityComparer<T>");
                var itf = IEqualityComparerInterfaceFor(ec.Context.TypeArgs[0])
                    ?? throw new NotSupportedException("rgctx: no IEqualityComparer<T> for element");
                return RgctxTypeInfoEntry(TypeDesc.MakeClass(itf));
            }
            case RgctxSlotKind.RgctxTable:
            {
                var callee = ResolveMethodHandle(module, handle, ctx, scope)
                    ?? throw new NotSupportedException("rgctx: unresolved cross-class callee token");
                var rc = callee.DeclaringClass;
                if (rc.SharedOwner is null)
                    throw new NotSupportedException($"rgctx: {rc.FullName} is not a grouped instantiation");
                // The referenced table must itself fill/emit, even when none of
                // rc's own methods is reachable (the callee runs as a shared
                // body under rc's identity through this very entry).
                Rgctx.Classes.RequireTable(rc);
                return "rgctx_" + rc.CppName;
            }
            case RgctxSlotKind.MethodRgctxTable:
            {
                var callee = ResolveMethodHandle(module, handle, ctx, scope)
                    ?? throw new NotSupportedException("rgctx: unresolved generic-method callee token");
                // Resolving under the real context can instantiate the real
                // generic method for the first time (all its callers shared);
                // link it on the spot — the emission pass runs no sync rounds.
                LinkCanonicalMethodInstance(callee);
                if (callee.SharedOwner is null)
                    throw new NotSupportedException(
                        $"rgctx: {callee.DeclaringClass.FullName}.{callee.Name} is not a linked "
                        + "generic-method instantiation");
                // The referenced per-method table must itself fill/emit, even
                // when the real instantiation is otherwise unreachable (the
                // shared callee runs under its identity through this entry).
                Rgctx.Methods.RequireTable(callee);
                return "rgctx_" + callee.CppName;
            }
            default:
                throw new NotSupportedException($"rgctx slot kind {slot.Kind}");
        }
    }

    /// <summary>A real type's type-info expression for an rgctx table entry,
    /// with the same emission side effects the equivalent per-instantiation body
    /// site would have: reference types and enums are noted for (at least
    /// opaque) metadata emission; a value type is force-emitted and marked
    /// allocated — its entry exists because a shared body boxes it — with the
    /// object-protocol overrides the boxed form can dispatch reached.</summary>
    private string RgctxTypeInfoEntry(TypeDesc t)
    {
        if (t is { Kind: TypeKind.Class, Class: { } c })
        {
            if (c.IsValueType)
            {
                NoteForceEmit(c);
                ReachAllocatedType(c);
                if (EffectiveToString(c) is { } ts)
                    Reach(ts);
                if (EffectiveGetHashCode(c) is { } gh)
                    Reach(gh);
                if (EffectiveEquals(c) is { } eq)
                    Reach(eq);
            }
            else
            {
                NoteReferencedType(c);
                // A cast target that is a closed SZArray collection interface
                // lets arrays of the element carry their dispatch map — mirror
                // the body-site note (see CastTargetTypeInfoExpr).
                if (c.IsInterface
                    && GenericDefFullName(c) is "System.Collections.Generic.IEnumerable"
                        or "System.Collections.Generic.ICollection"
                        or "System.Collections.Generic.IReadOnlyCollection"
                        or "System.Collections.Generic.IList"
                        or "System.Collections.Generic.IReadOnlyList"
                    && c.Context.TypeArgs is [{ } ee])
                    NoteArrayEnumerableElement(ee);
            }
        }
        return MethodCompiler.TypeInfoExprOf(t)
            ?? throw new NotSupportedException($"rgctx: no runtime type-info for {t}");
    }

    private TypeDesc RgctxResolveTypeToken(Module module, EntityHandle handle, GenericContext ctx) => handle.Kind switch
    {
        HandleKind.TypeDefinition => GetTypeDescForDefinition(module, (TypeDefinitionHandle)handle),
        HandleKind.TypeSpecification => module.Reader.GetTypeSpecification((TypeSpecificationHandle)handle)
            .DecodeSignature(SigProvider, ctx),
        _ => throw new NotSupportedException($"rgctx type token kind {handle.Kind} is not supported"),
    };

    /// <summary>The declaring type of a member-reference token, resolved under
    /// <paramref name="ctx"/>: the resolution path for a member of an
    /// intrinsic-opaque class, which has no MethodInfo to resolve to.</summary>
    private TypeDesc RgctxResolveMemberParentType(Module module, EntityHandle handle, GenericContext ctx)
    {
        if (handle.Kind != HandleKind.MemberReference)
            throw new NotSupportedException($"rgctx member-parent token kind {handle.Kind} is not supported");
        var parent = module.Reader.GetMemberReference((MemberReferenceHandle)handle).Parent;
        return RgctxResolveTypeToken(module, parent, ctx);
    }

    private (ClassInfo, FieldInfo) RgctxResolveField(Module module, EntityHandle handle, GenericContext ctx, ClassInfo user)
    {
        if (handle.Kind == HandleKind.MemberReference)
            return ResolveMemberRefField(module, (MemberReferenceHandle)handle, ctx);
        if (handle.Kind != HandleKind.FieldDefinition)
            throw new NotSupportedException($"rgctx field token kind {handle.Kind} is not supported");
        var fd = module.Reader.GetFieldDefinition((FieldDefinitionHandle)handle);
        var declType = fd.GetDeclaringType();
        string fname = module.Reader.GetString(fd.Name);
        // A FieldDef inside the shared class's own body resolves against the
        // group member (same template); any other FieldDef owner is
        // non-generic in this position.
        var cls = user.GenericArity > 0 && user.Handle == declType ? user : GetClass(module, declType);
        return (cls, cls.Fields.First(f => f.Name == fname));
    }

    /// <summary>Whether the type (recursively) mentions an unresolved generic
    /// parameter or an open template — such an argument vector is not a closed
    /// instantiation and is never canonicalized.</summary>
    private static bool ContainsOpenGenerics(TypeDesc t) => t.Kind switch
    {
        TypeKind.GenericVar or TypeKind.Template => true,
        TypeKind.SZArray or TypeKind.MDArray or TypeKind.ByRef or TypeKind.Pointer =>
            ContainsOpenGenerics(t.Element!),
        TypeKind.Class => t.Class!.Context.TypeArgs.Any(ContainsOpenGenerics),
        _ => false,
    };

    /// <summary>Checks a canonical link that was made before its user had a vtable, and
    /// drops it if the shapes diverge.
    ///
    /// <para>The check is the expensive half of canonical linkage: it reads both vtables,
    /// so asking it eagerly would decode the members of every specialization in the program,
    /// most of them for classes nothing reaches and nothing emits. So the link is made
    /// optimistically and checked here, at the first moment the answer exists: when the
    /// user's members are decoded (<see cref="CompleteMembers"/>), or right away for a user
    /// that already had them. A user that is never decoded keeps an unverified link, and
    /// that is sound because nothing reads it: the link is consumed by body sharing (a shell
    /// has no reachable body), by the struct alias (a shell is not emitted — and if it ever
    /// is, EmitAdd decodes it, which lands here), and by the rgctx tables (which filter on
    /// live users).</para></summary>
    internal void VerifyCanonicalLink(ClassInfo user)
    {
        if (user.SharedLinkVerified || user.SharedOwner is not { } owner)
            return;
        // Set first, exactly as CompleteMembers sets MembersCompleted first: EnsureCompleted
        // below re-enters here at the end of the decode, and the outer call is the one that
        // finishes the job.
        user.SharedLinkVerified = true;
        EnsureCompleted(user);    // its vtable is half the comparison
        EnsureCompleted(owner);   // few: one per group, not one per instantiation
        if (SameVtableShape(user, owner))
            return;
        owner.RemoveSharedUser(user);
        user.SharedOwner = null;
    }

    /// <summary>Whether two specializations of the same template assigned their
    /// vtable slots identically: same slot count, the same template method (by
    /// metadata handle) owning each slot, and abstract/concrete agreement. Both
    /// sides derive from one template, so any divergence means canonicalization
    /// changed the override/overload matching (e.g. two signatures merged) and
    /// the pair must not share bodies.</summary>
    private static bool SameVtableShape(ClassInfo user, ClassInfo owner)
    {
        if (user.SlotOwners.Count != owner.SlotOwners.Count)
            return false;
        for (int i = 0; i < user.SlotOwners.Count; i++)
        {
            var a = user.SlotOwners[i];
            var b = owner.SlotOwners[i];
            if (a.Module != b.Module || a.Handle != b.Handle)
                return false;
            if ((user.Vtable[i] is null) != (owner.Vtable[i] is null))
                return false;
        }
        return true;
    }
}

/// <summary>What an emitted body does that a <c>[HotPath(NoAlloc)]</c> closure forbids:
/// a steady-state allocation and/or a dynamic dispatch, each with the representative token
/// that witnessed it (for diagnostics). <see cref="Method"/> is a representative MethodInfo
/// for the body symbol — the closure-chain and message name types off it.</summary>
internal struct HotBodyFacts
{
    internal bool Allocates;
    internal string? AllocToken;
    internal bool Dispatches;
    internal string? DispatchToken;
    internal MethodInfo Method;
}
