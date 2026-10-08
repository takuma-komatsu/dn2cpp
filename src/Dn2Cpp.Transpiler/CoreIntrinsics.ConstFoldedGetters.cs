namespace Dn2Cpp;

internal static partial class CoreIntrinsics
{
    /// <summary>Boolean capability getters folded to a compile-time constant.
    /// Three mechanisms act on this table together: the call site pushes the
    /// constant (MethodCompiler.TryEmitConstFoldedGetter), the reachability
    /// edge to the getter body is cut (Compilation.ResolveCallTarget), and a
    /// `call getter; brtrue/brfalse` pair prunes the dead arm from both
    /// reachability and emission (BranchLiveness). Both the MethodDef form
    /// (within-CoreLib) and the MemberRef form (a public getter referenced
    /// cross-assembly) are intercepted on all three paths.
    ///
    /// GlobalizationMode: a dn2cpp binary runs with the invariant-globalization
    /// posture (the equivalent of building with InvariantGlobalization=true) —
    /// every `if (GlobalizationMode.Invariant)` guard in the BCL becomes
    /// constant-true, so the pure-managed invariant arms execute (ordinal
    /// casing, managed Punycode for IDN) and the ICU/NLS arms — whose leaves
    /// are libSystem.Globalization.Native P/Invokes the runtime does not
    /// provide — become dead (the getter's real body drags Settings..cctor ->
    /// LoadAppLocalIcu -> InitICUFunctions into the tree).
    ///
    /// RuntimeFeature: the dynamic-code capability probes are constant-false —
    /// the IL2CPP/NativeAOT posture: a native dn2cpp binary has no runtime
    /// codegen (their real bodies read an AppContext switch defaulting to
    /// true — wrong here). The RegexOptions.Compiled arm those flags guard
    /// degrades to the interpreter, exactly like NativeAOT; Regex.Compile
    /// itself folds to a null factory alongside them
    /// (MethodCompiler.TryEmitRegexCompileFold).
    ///
    /// RuntimeHelpers.TryEnsureSufficientExecutionStack: constant-true — the
    /// no-stack-probe posture (the native stack is the only stack; the void
    /// Ensure sibling is a no-op in the RuntimeHelpers intrinsic table, which
    /// also still serves this probe's MemberRef call sites — RuntimeHelpers is
    /// an intrinsic-mapped type, so its MemberRef form never reaches the
    /// const-fold call-site path). StackHelper.TryEnsureSufficientExecutionStack
    /// is the shared-source forwarder Regex's guards actually call
    /// (intra-assembly, so the fold must key on the wrapper too). The entries'
    /// value feeds the BranchLiveness verdict: the
    /// `if (!TryEnsure...) CallOnEmptyStack(...)` re-dispatch arm (Regex's
    /// deep-recursion escape hatch) is pruned, keeping StackHelper's
    /// Task.Run/ContinueWith plumbing out of the tree.
    ///
    /// Debugger.IsLogging: constant-false, NativeAOT's hard-coded answer — a
    /// transpiled binary has no VM to log into. BranchLiveness prunes
    /// DebugProvider.WriteToDebugger's then-arm (Debugger.Log, whose leaf is the
    /// bodyless QCall LogInternal) and leaves Interop.Sys.SysLog the live sink,
    /// where NativeAOT's Trace/Debug output goes on Unix.</summary>
    private static readonly Dictionary<(string Type, string Method), bool> s_constFoldedGetters = new()
    {
        [("System.Globalization.GlobalizationMode", "get_Invariant")] = true,
        [("System.Globalization.GlobalizationMode", "get_UseNls")] = false,
        [("System.Runtime.CompilerServices.RuntimeFeature", "get_IsDynamicCodeSupported")] = false,
        [("System.Runtime.CompilerServices.RuntimeFeature", "get_IsDynamicCodeCompiled")] = false,
        [("System.Runtime.CompilerServices.RuntimeHelpers", "TryEnsureSufficientExecutionStack")] = true,
        [("System.Threading.StackHelper", "TryEnsureSufficientExecutionStack")] = true,
        [("System.Diagnostics.Debugger", "IsLogging")] = false,
    };

    /// <summary>Method names appearing in <see cref="s_constFoldedGetters"/> —
    /// the cheap first-pass filter for per-call-token oracle lookups.</summary>
    private static readonly HashSet<string> s_constFoldedGetterNames =
        new(s_constFoldedGetters.Keys.Select(k => k.Method));

    /// <summary>The folded constant for a getter, or null when the method is not
    /// const-folded. See <see cref="s_constFoldedGetters"/>.</summary>
    public static bool? ConstFoldedGetter(string declType, string name) =>
        s_constFoldedGetters.TryGetValue((declType, name), out bool v) ? v : null;

    /// <summary>Name-only prefilter for <see cref="ConstFoldedGetter"/>, so a
    /// per-call-site probe skips declaring-type resolution for the overwhelming
    /// majority of calls.</summary>
    public static bool IsConstFoldedGetterName(string name) => s_constFoldedGetterNames.Contains(name);
}
