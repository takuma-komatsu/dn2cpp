namespace Dn2Cpp;

/// <summary>The transpile argument bundle: every lever the CLIs pass down through
/// <see cref="TranspileDriver"/> into <see cref="Compilation.Create"/>, with each
/// default declared exactly once — here, so no caller can declare a divergent one.
/// Public because the Godot-free console CLI builds one; the members whose types are
/// internal (<see cref="Backend"/>, <see cref="ExternalGenericResolver"/>) are
/// internal, and their defaults are what that caller wants.</summary>
public sealed record TranspileOptions
{
    /// <summary>The input assembly — module 0 of the load set.</summary>
    public string Input { get; init; } = "";

    /// <summary>Reference assemblies (<c>-r</c>), loaded after the input.</summary>
    public IReadOnlyList<string> References { get; init; } = Array.Empty<string>();

    /// <summary>Output directory for the generated C++ (and the <c>--measure</c>
    /// report).</summary>
    public string OutDir { get; init; } = ".";

    /// <summary>Self-hosting feasibility harness (<c>--measure</c>): collect and
    /// report every reachable transpilation gap instead of emitting C++.</summary>
    public bool Measure { get; init; }

    /// <summary>Opt-in (<c>--verbose</c>): expand the driver's end-of-run reports that
    /// print a count by default into their per-item detail. A flag rather than an env
    /// knob — those reports answer "what did this build quietly decline to do", and an
    /// answer you have to already know to ask for is no answer; a flag is in
    /// <c>--help</c>.</summary>
    public bool Verbose { get; init; }

    /// <summary>Opt-in (<c>--auto-ref</c>): eagerly load the transitive
    /// shared-framework reference closure before Build() so external generic
    /// instantiations resolve without an explicit <c>-r</c> per dependency.</summary>
    public bool AutoRef { get; init; }

    /// <summary>Canonical shared-generics grouping
    /// (<see cref="Compilation.SharedGenericsEnabled"/>). Default on; off
    /// (<c>--no-shared-generics</c>), every generic instantiation compiles
    /// monomorphically, byte-identical to the pre-sharing output.</summary>
    public bool SharedGenerics { get; init; } = true;

    /// <summary>Opt-in (<c>--shadow-stack</c>): plant a Dn2CppShadowFrame guard in
    /// every compiled body's prologue for exact throw traces
    /// (<see cref="Compilation.ShadowStackEnabled"/>).</summary>
    public bool ShadowStack { get; init; }

    /// <summary>Hot-update base build (<c>--hotupdate-base</c>): the emitter adds
    /// the ABI-contract hash constant, the driver writes the base-abi.json sidecar
    /// (docs/BPI-FORMAT.md), and the compilation takes the hot-update posture
    /// (see <see cref="HotUpdatePosture"/>).</summary>
    public bool HotupdateBase { get; init; }

    /// <summary>Hot-update base build: the <c>--hotupdate-refs</c> roots — extra
    /// patch-bindable instantiations/methods to force-emit even when the base
    /// program never uses them (HybridCLR's AOTGenericReferences).</summary>
    public IReadOnlyList<string>? HotupdateRefs { get; init; }

    /// <summary>Assembly simple names (<c>--no-adopt-async</c>, repeatable) whose
    /// [AsyncMethodBuilder] task types are NOT adopted into the intrinsic Task
    /// family — their real IL transpiles through the general pipeline.</summary>
    public IReadOnlyList<string>? NoAdoptAsync { get; init; }

    /// <summary>Method cut specs (<c>--cut "DeclType::Method"</c>, repeatable):
    /// subtree cut at reachability, call sites neutralized to the default result
    /// (the backend bounded-set semantics as a per-run lever).</summary>
    public IReadOnlyList<string>? CutMethods { get; init; }

    /// <summary>Opt-in (<c>--trim-reflection</c>): strip the reflection member
    /// metadata of every class the program cannot plausibly reflect over. A flag,
    /// never an environment variable — it changes the emitted C++.</summary>
    public bool TrimReflection { get; init; }

    /// <summary>Types whose reflection metadata is kept under
    /// <c>--trim-reflection</c> (<c>--reflection-root</c>, repeatable). A root
    /// matching no loaded type is a hard error.</summary>
    public IReadOnlyList<string>? ReflectionRoots { get; init; }

    /// <summary>Project directories recursively searched for files named exactly
    /// <c>link.xml</c>. Direct CLI use performs no search unless a root is supplied.</summary>
    public IReadOnlyList<string> ProjectRoots { get; init; } = Array.Empty<string>();

    /// <summary>Unity linker feature names enabled for conditional
    /// <c>feature=</c> directives in <c>link.xml</c>.</summary>
    public IReadOnlyList<string> LinkFeatures { get; init; } = Array.Empty<string>();

    /// <summary>Assembly simple names (<c>--no-manifest-resources</c>, repeatable)
    /// whose embedded manifest resources are DROPPED from the image: the assembly's
    /// registry row carries the <c>resourcesDropped</c> bit instead of a resource
    /// table, so a run-time query that misses throws a catchable
    /// <c>PlatformNotSupportedException</c> naming the assembly and the remedy —
    /// never the null/empty that means "no such resource" in real .NET. A name
    /// matching no loaded assembly is a hard error: a typo silently becoming a no-op
    /// drop would keep shipping the bytes the flag was written to shed. A flag and
    /// never an environment variable — it changes the C++ a *successful* transpile
    /// emits, which the self-host fixpoint forbids the environment to do.</summary>
    public IReadOnlyList<string>? NoManifestResources { get; init; }

    /// <summary>Manifest resource names kept under <c>--no-manifest-resources</c>
    /// (<c>--manifest-resource-root</c>, repeatable) — the escape hatch, matched by
    /// exact manifest name against the dropped assemblies' embedded resources. A
    /// kept resource stays in the table and answers truthfully; everything else in
    /// the dropped assembly throws. A root matching no resource of any dropped
    /// assembly is a hard error (the <see cref="ReflectionRoots"/> doctrine: a typo
    /// silently becoming a no-op root would surface as a
    /// PlatformNotSupportedException only in the shipped game).</summary>
    public IReadOnlyList<string>? ManifestResourceRoots { get; init; }

    /// <summary>[DllImport] module names (<c>--pinvoke-module</c>, repeatable) whose
    /// P/Invokes lower to direct native calls from any loaded assembly — the opt-in
    /// for an external binding library pulled in with <c>-r</c>, whose imports
    /// otherwise stay on the intrinsic/throw boundary
    /// (<see cref="Compilation.IsOptedInPInvokeModule"/>).</summary>
    public IReadOnlyList<string>? PInvokeModules { get; init; }

    /// <summary>Method-body chunk size cap per emitted C++ translation unit
    /// (overflow spills into generated_N.cpp); a non-positive value keeps
    /// everything in one generated.cpp.</summary>
    public int SplitBytes { get; init; } = 1 << 20;

    /// <summary>Shim assembly simple names (<c>--no-default-ref</c>, repeatable) NOT
    /// injected as conditional default references even when the BCL assembly they
    /// serve is loaded — one name per occurrence, from the fixed set
    /// <see cref="Compilation.IsDefaultRefName"/> validates against (an unknown name
    /// is a hard error in <see cref="TranspileDriver.Run"/>, the one place both CLIs
    /// funnel through). Deliberately per-shim with no blanket "turn them all off"
    /// form: a blanket flag would silently decline a newly added shim in every caller
    /// that already wrote it. A flag and never an environment variable — it changes
    /// the C++ a *successful* transpile emits, which the self-host fixpoint forbids
    /// the environment to do.</summary>
    public IReadOnlyList<string>? NoDefaultRefs { get; init; }

    /// <summary>Directory holding the shim assemblies that ship with this CLI, for
    /// the conditional default-reference injection (<see cref="NoDefaultRefs"/>).
    /// <para><b>Null disables the mechanism entirely</b>, and null is the default: a
    /// caller that does not set this loads exactly the paths it passed, so its output
    /// is byte-identical to a build without the feature. <see cref="TranspileDriver.Run"/>
    /// is the only setter.</para>
    /// <para>Internal on purpose — the shims a transpile injects must be the ones
    /// shipped *with this CLI*, so a caller must not be able to point the injection at
    /// some other directory. The <c>--emit-patch</c> path builds its own
    /// <see cref="Compilation.Create"/> bundle and never sets this, so the patch
    /// converter is outside the mechanism by construction.</para></summary>
    internal string? DefaultRefDir { get; init; }

    /// <summary>The emit backend. Null — the only value the Godot-free console CLI
    /// can produce, the type being internal — means the pure-.NET
    /// <see cref="ConsoleBackend"/>; the resolution lives in
    /// <see cref="TranspileDriver.Run"/>, nowhere else.</summary>
    internal IEmitBackend? Backend { get; init; }

    /// <summary>Hot-update patch converter: resolves a closed generic instantiation
    /// of a base-image type against the base manifest during signature decoding
    /// (the converter never loads the base assembly).</summary>
    internal Func<string, TypeDesc[], TypeDesc?>? ExternalGenericResolver { get; init; }

    /// <summary>Hot-update world without a base build: the patch converter's own
    /// model sets this directly. A <see cref="HotupdateBase"/> build implies the
    /// same posture — <see cref="Compilation"/> reads the OR of the two — so the
    /// driver never has to remember to copy one flag into the other.</summary>
    internal bool HotUpdatePosture { get; init; }
}
