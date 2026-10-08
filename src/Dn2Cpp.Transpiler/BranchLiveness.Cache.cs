using System.Reflection.Metadata;

namespace Dn2Cpp;

internal sealed partial class BranchLiveness
{
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

}
