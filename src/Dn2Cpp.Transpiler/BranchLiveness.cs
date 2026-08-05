using System.Reflection.Metadata;

namespace Dn2Cpp;

/// <summary>Classification of a call token for the type-identity window — raw metadata-name
/// reads only (see <c>Compilation.ClassifyTypeIdentityCall</c>), so probing every
/// call-before-branch site stays cheap.</summary>
internal enum TypeIdentityCall
{
    None,
    GetTypeFromHandle,
    OpEquality,
    OpInequality,
}

/// <summary>Branch-granular liveness for a body whose branch verdict folds at compile time.
/// Reachability is method-granular with no branch pruning, so once a guard folds to a
/// constant its dead arm would still pull its callees into the tree — where an unmapped
/// icall aborts the transpile, or a typeof-chain dispatcher instantiates its generic local
/// function against the cross product of every compared type. This pass computes, per body,
/// the live IL offsets by walking edges from the entry point and taking only the live side of
/// a folded branch. Two shapes fold:
///  - `call folded-getter; brtrue/brfalse` (<see cref="CoreIntrinsics.ConstFoldedGetter"/>);
///  - `ldtoken A; call GetTypeFromHandle; ldtoken B; call GetTypeFromHandle;
///    call Type.op_[In]Equality; brtrue/brfalse` — Roslyn's shape for
///    `if (typeof(A) ==/!= typeof(B))` — when both tokens resolve to closed types
///    (<c>Compilation.TypeEqualityVerdict</c>). The five pre-branch instructions of such a
///    window are additionally ELIDED (<see cref="ElidedAt"/>) and the branch then pops
///    nothing (<see cref="FoldedWindowBranch"/>), so no dead type-info loads are emitted.
/// Both consumers share this pass — the reachability scanner
/// (Compilation.ScanBodyForGenerics) and the emitter (MethodCompiler.Compile) skip the same
/// instructions and fold the same branches — so the emitted call graph and the reachable set
/// agree exactly. That agreement holds only because both sides compute THIS pass with the
/// same Compilation-backed callbacks; never give one side a private variant.
///
/// Conservative bail-outs (Compute returns null, callers treat the body as fully live, and
/// any dead-arm leaf falls back to the bounded-method cut):
///  - no folded call+branch pair in the body;
///  - a guard value flowing through a local instead of feeding the branch directly — the
///    peephole does not see it, so both arms stay live, which can only over-approximate;
///  - a dead offset inside any exception region, since a fully-dead try/catch would need
///    region-aware emission surgery. Not free: a body whose dead arm sits inside a `using`
///    keeps its whole subtree reachable, enough to drag an untranspilable cascade in. Answer
///    such a body with a call-site const-fold intercept instead of widening this pass.
/// A window that some branch/switch/handler enters PAST its first instruction is dropped —
/// the verdict describes operands that entry never computes; entering AT the window start is
/// an ordinary join and folds fine.</summary>
internal sealed class BranchLiveness
{
    private readonly HashSet<int> _liveOffsets;
    private readonly Dictionary<int, bool> _verdicts;
    // Offsets of the five pre-branch instructions of every folded
    // type-identity window (skipped by emission), and the branch offsets whose
    // verdict came from such a window (they pop nothing — the comparison that
    // would have fed them was elided).
    private readonly HashSet<int>? _elided;
    private readonly HashSet<int>? _windowBranches;

    private BranchLiveness(HashSet<int> liveOffsets, Dictionary<int, bool> verdicts,
        HashSet<int>? elided, HashSet<int>? windowBranches)
    {
        _liveOffsets = liveOffsets;
        _verdicts = verdicts;
        _elided = elided;
        _windowBranches = windowBranches;
    }

    public bool LiveAt(int offset) => _liveOffsets.Contains(offset);

    /// <summary>For a brtrue/brfalse whose operand is a folded constant: whether
    /// the branch is taken. Null for every dynamic branch.</summary>
    public bool? VerdictAt(int offset) => _verdicts.TryGetValue(offset, out bool taken) ? taken : null;

    /// <summary>Whether the (live) instruction at this offset belongs to a folded
    /// type-identity window: the emitter skips it entirely — nothing is pushed,
    /// no type-info is referenced.</summary>
    public bool ElidedAt(int offset) => _elided is not null && _elided.Contains(offset);

    /// <summary>Whether the folded branch at this offset closes a type-identity
    /// window: its comparison operand was never pushed, so the emitter must not
    /// pop one (a folded GETTER's constant push is real and does come off).</summary>
    public bool FoldedWindowBranch(int offset) => _windowBranches is not null && _windowBranches.Contains(offset);

    /// <summary>The value cached on a <see cref="MethodInfo"/> whose body the pass
    /// does not apply to (<see cref="Compute"/> answered null): one reference field
    /// then distinguishes "computed, nothing to prune" from "never computed".
    /// Never escapes — <see cref="ComputeCached"/> maps it back to null.</summary>
    private static readonly BranchLiveness s_notApplicable = new(new(), new(), null, null);

    /// <summary><see cref="Compute"/> memoized per method on
    /// <see cref="MethodInfo.LivenessCache"/> — the sole reader and writer of that field. The
    /// reachability scan fills it; the planning and emission compiles hit it.
    ///
    /// <para>A hit equals a recompute because the result is a pure function of inputs fixed
    /// for the run: the IL bytes at <see cref="MethodInfo.Rva"/> (immutable PE),
    /// <see cref="MethodInfo.Context"/> (fixed at construction), and the three callbacks'
    /// answers — two of which read raw metadata names only, while <c>TypeEqualityVerdict</c>
    /// resolves tokens in that same context over an assembly set closed before any body
    /// compiles. Skipping the recompute perturbs no state either: the callbacks' only side
    /// effect is minting closed generics, which is idempotent and already done by whoever
    /// filled the cache. All three sites must pass the same module-backed callbacks under the
    /// same <c>m.Context</c>, or the shared verdict stops being the one each would have
    /// computed.</para>
    ///
    /// <para>The decoded instruction list is deliberately NOT cached beside the verdict: the
    /// verdict is bare IL offsets, and only for the rare body with a folded branch, whereas
    /// instruction lists would retain every compiled body from planning through
    /// emission.</para></summary>
    public static BranchLiveness? ComputeCached(
        MethodInfo m,
        List<Instruction> insns, MethodBodyBlock body, Func<int, bool?> foldedConst,
        Func<int, TypeIdentityCall> typeIdentityCall, Func<int, int, bool?> typeEqualityVerdict)
    {
        if (m.LivenessCache is { } cached)
            return ReferenceEquals(cached, s_notApplicable) ? null : cached;
        var result = Compute(insns, body, foldedConst, typeIdentityCall, typeEqualityVerdict);
        m.LivenessCache = result ?? s_notApplicable;
        return result;
    }

    /// <summary>Compute liveness for one body, or null when the pass does not
    /// apply (see the class remarks). <paramref name="foldedConst"/> resolves a
    /// call token to its folded constant (null for a normal call);
    /// <paramref name="typeIdentityCall"/> classifies a call token for the type-identity
    /// window (raw name reads); <paramref name="typeEqualityVerdict"/> answers
    /// whether two ldtoken type tokens name the same closed type (null: no fold —
    /// open/placeholder/unresolved, or a hot-update base build).</summary>
    public static BranchLiveness? Compute(
        List<Instruction> insns, MethodBodyBlock body, Func<int, bool?> foldedConst,
        Func<int, TypeIdentityCall> typeIdentityCall, Func<int, int, bool?> typeEqualityVerdict)
    {
        // 1. Verdicts: every folded pattern whose result directly feeds the next
        // branch. The constant decides the edge; brtrue takes it when true,
        // brfalse when false.
        Dictionary<int, bool>? verdicts = null;
        List<(int Start, int Branch)>? windows = null; // instruction INDICES
        for (int i = 0; i + 1 < insns.Count; i++)
        {
            if (insns[i].OpCode is not (ILOpCode.Call or ILOpCode.Callvirt))
                continue;
            var br = insns[i + 1].OpCode;
            bool isTrue = br is ILOpCode.Brtrue or ILOpCode.Brtrue_s;
            if (!isTrue && br is not (ILOpCode.Brfalse or ILOpCode.Brfalse_s))
                continue;
            if (foldedConst(insns[i].Token) is bool c)
            {
                (verdicts ??= new())[insns[i + 1].Offset] = isTrue == c;
                continue;
            }
            // Type-identity window. Cheapest test outermost: opcode shape (free), then the
            // raw-name gates (one metadata string read each), then the resolving verdict —
            // so a body without the pattern pays only the shape test per call site.
            if (i < 4
                || insns[i].OpCode != ILOpCode.Call
                || insns[i - 1].OpCode != ILOpCode.Call || insns[i - 3].OpCode != ILOpCode.Call
                || insns[i - 2].OpCode != ILOpCode.Ldtoken || insns[i - 4].OpCode != ILOpCode.Ldtoken)
                continue;
            var op = typeIdentityCall(insns[i].Token);
            if (op is not (TypeIdentityCall.OpEquality or TypeIdentityCall.OpInequality))
                continue;
            if (typeIdentityCall(insns[i - 1].Token) != TypeIdentityCall.GetTypeFromHandle
                || typeIdentityCall(insns[i - 3].Token) != TypeIdentityCall.GetTypeFromHandle)
                continue;
            if (typeEqualityVerdict(insns[i - 4].Token, insns[i - 2].Token) is not bool eq)
                continue;
            if (op == TypeIdentityCall.OpInequality)
                eq = !eq;
            (verdicts ??= new())[insns[i + 1].Offset] = isTrue == eq;
            (windows ??= new()).Add((i - 4, i + 1));
        }

        // 1b. A branch/switch/handler entering a window PAST its first
        // instruction would reach the folded branch with an operand the verdict
        // knows nothing about — drop such a window (its comparison stays
        // dynamic on both sides). Entering AT the window start is an ordinary
        // join: the window is stack-neutral and consumes nothing beneath it, so
        // the fold holds however control arrives there.
        HashSet<int>? elided = null;
        HashSet<int>? windowBranches = null;
        if (windows is not null)
        {
            var targets = new HashSet<int>();
            foreach (var insn in insns)
            {
                if (insn.SwitchTargets is not null)
                    foreach (int t in insn.SwitchTargets)
                        targets.Add(t);
                else if (ILDecoder.IsBranch(insn.OpCode))
                    targets.Add((int)insn.Operand);
            }
            foreach (var region in body.ExceptionRegions)
            {
                targets.Add(region.HandlerOffset);
                if (region.Kind == ExceptionRegionKind.Filter)
                    targets.Add(region.FilterOffset);
            }
            foreach (var (start, branch) in windows)
            {
                bool enteredMidWindow = false;
                for (int k = start + 1; k <= branch && !enteredMidWindow; k++)
                    enteredMidWindow = targets.Contains(insns[k].Offset);
                if (enteredMidWindow)
                {
                    verdicts!.Remove(insns[branch].Offset);
                    continue;
                }
                elided ??= new();
                for (int k = start; k < branch; k++)
                    elided.Add(insns[k].Offset);
                (windowBranches ??= new()).Add(insns[branch].Offset);
            }
        }
        if (verdicts is null || verdicts.Count == 0)
            return null;

        // 2. Live-offset walk. Handlers and filters are entered exceptionally,
        // not by an IL edge, so they are roots alongside the entry point.
        var byOffset = new Dictionary<int, int>(insns.Count);
        for (int i = 0; i < insns.Count; i++)
            byOffset[insns[i].Offset] = i;
        var live = new HashSet<int>();
        var work = new Stack<int>();
        void Visit(int offset)
        {
            if (byOffset.TryGetValue(offset, out int idx) && live.Add(offset))
                work.Push(idx);
        }
        Visit(0);
        foreach (var region in body.ExceptionRegions)
        {
            Visit(region.HandlerOffset);
            if (region.Kind == ExceptionRegionKind.Filter)
                Visit(region.FilterOffset);
        }
        while (work.Count > 0)
        {
            var insn = insns[work.Pop()];
            if (insn.SwitchTargets is not null)
            {
                foreach (int t in insn.SwitchTargets)
                    Visit(t);
                Visit(insn.NextOffset);
                continue;
            }
            if (verdicts.TryGetValue(insn.Offset, out bool taken))
            {
                Visit(taken ? (int)insn.Operand : insn.NextOffset);
                continue;
            }
            if (ILDecoder.IsBranch(insn.OpCode))
            {
                Visit((int)insn.Operand);
                if (!ILDecoder.IsUnconditionalTransfer(insn.OpCode))
                    Visit(insn.NextOffset);
                continue;
            }
            if (insn.OpCode is ILOpCode.Ret or ILOpCode.Throw or ILOpCode.Rethrow
                or ILOpCode.Endfinally or ILOpCode.Endfilter or ILOpCode.Jmp)
                continue;
            Visit(insn.NextOffset);
        }
        if (live.Count == insns.Count)
            return null; // every arm reachable — nothing to prune

        // 3. Bail if any dead offset lies inside an exception region.
        foreach (var region in body.ExceptionRegions)
        {
            foreach (var insn in insns)
            {
                int o = insn.Offset;
                bool inRegion = (o >= region.TryOffset && o < region.TryOffset + region.TryLength)
                    || (o >= region.HandlerOffset && o < region.HandlerOffset + region.HandlerLength)
                    || (region.Kind == ExceptionRegionKind.Filter
                        && o >= region.FilterOffset && o < region.HandlerOffset);
                if (inRegion && !live.Contains(o))
                    return null;
            }
        }
        return new BranchLiveness(live, verdicts, elided, windowBranches);
    }
}
